using Phaeyz.Marshalling;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// A segment for the start-of-scan.
/// </summary>
[Segment(Marker.StartOfScan)]
public class StartOfScan : Segment
{
    /// <summary>
    /// The component data is used to describe the image's color components and their respective Huffman tables.
    /// </summary>
    public ComponentData[] ComponentDatas { get; set; } = [];

    /// <summary>
    /// The starting DCT coefficient. For a baseline sequential JPEG, this value is typically set to <c>0</c>,
    /// which means that the scan will start from the direct current (DC) coefficient.
    /// </summary>
    public byte StartingDctCoefficient { get; set; }

    /// <summary>
    /// The ending DCT coefficient. For a baseline sequential JPEG, this is usually set to <c>63</c>,
    /// covering all alternating current (AC) coefficients.
    /// </summary>
    public byte EndingDctCoefficient { get; set; }

    /// <summary>
    /// Specifies the point at which successive approximation begins, indicating the bit position for the first scan
    /// of a particular set of coefficients. Used in progressive JPEGs and is set to <c>0</c> in baseline JPEGs.
    /// </summary>
    public byte SuccessiveApproximationBitPositionHigh { get; set; }

    /// <summary>
    /// Specifies the point at which successive approximation ends, indicating the bit position for the final scan
    /// of a particular set of coefficients. Used in progressive JPEGs and is set to <c>0</c> in baseline JPEGs.
    /// </summary>
    public byte SuccessiveApproximationBitPositionLow { get; set; }

    /// <summary>
    /// The scanned image content immediately following the segment.
    /// </summary>
    public byte[] ImageContent { get; set; } = [];

    /// <summary>
    /// Reads the segment from the stream, hydrating the properties of the segment.
    /// </summary>
    /// <param name="stream">
    /// The stream to read from.
    /// </param>
    /// <param name="segmentLength">
    /// The remaining length of the segment on the stream. The caller may deduct from this object as it reads
    /// to prevent from reading too much.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the segment has been read.
    /// </returns>
    /// <remarks>
    /// It is expected that the method reads <paramref name="segmentLength"/> bytes from the stream.
    /// If less is read, it will lead to corruption. More is allowed depending on the circumstance, such as
    /// handling out-of-band data.
    /// </remarks>
    public override async ValueTask ReadFromStreamAsync(MarshalStream stream, SegmentLength segmentLength, CancellationToken cancellationToken)
    {
        segmentLength--;
        byte componentCount = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);

        segmentLength -= componentCount * 2;
        ComponentDatas = new ComponentData[componentCount];
        for (int i = 0; i < componentCount; i++)
        {
            ComponentDatas[i].ComponentId = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
            byte huffmanTable = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
            ComponentDatas[i].DirectCurrentHuffmanTable = (byte)(huffmanTable >> 4);
            ComponentDatas[i].AlternatingCurrentHuffmanTable = (byte)(huffmanTable & 0x0F);
        }

        segmentLength -= 3;

        ushort specialSelection = await stream.ReadUInt16Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        StartingDctCoefficient = (byte)(specialSelection >> 8);
        EndingDctCoefficient = (byte)(specialSelection);

        byte successiveApproximation = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        SuccessiveApproximationBitPositionHigh = (byte)(successiveApproximation >> 4);
        SuccessiveApproximationBitPositionLow = (byte)(successiveApproximation & 0x0F);

        // Skip past any padding.
        await stream.SkipAsync(segmentLength.Remaining, cancellationToken).ConfigureAwait(false);

        // Now read the payload.
        // https://stackoverflow.com/a/53062155/5988923
        static int ScanBuffer(ReadOnlyMemory<byte> scanBuffer)
        {
            ReadOnlySpan<byte> scanSpan = scanBuffer.Span;
            for (int i = 0; i < scanSpan.Length - 1; i++)
            {
                if (scanSpan[i] == SegmentReader.MarkerIndicator)
                {
                    byte potentialMarker = scanSpan[i + 1];
                    if (potentialMarker != 0 && (potentialMarker < (byte)Marker.Restart0 || potentialMarker > (byte)Marker.Restart7))
                    {
                        return i;
                    }
                }
            }
            return scanSpan.Length - 1;
        }
        using MemoryStream imageStream = new();
        MarshalStreamScanResult result = await stream.ScanAsync(imageStream, 2, -1, ScanBuffer, cancellationToken).ConfigureAwait(false);
        if (!result.IsPositiveMatch)
        {
            throw new JfifException("Could not find the next JFIF marker.");
        }

        ImageContent = imageStream.ToArray();
    }

    /// <summary>
    /// Validates all properties of the segment such that the segment is ready for serialization,
    /// and computes the body length of the segment.
    /// </summary>
    /// <returns>
    /// The body length of the segment (total length of segment after the optional identifier, and excluding
    /// any out-of-band data).
    /// </returns>
    /// <remarks>
    /// This method is not called if the segment does not have length.
    /// </remarks>
    public override int ValidateAndComputeLength()
    {
        if ((SuccessiveApproximationBitPositionHigh & 0xF0) != 0)
        {
            throw new JfifException($"Invalid value for 'SuccessiveApproximationHigh': ({SuccessiveApproximationBitPositionHigh})");
        }
        if ((SuccessiveApproximationBitPositionLow & 0xF0) != 0)
        {
            throw new JfifException($"Invalid value for 'SuccessiveApproximationLow': ({SuccessiveApproximationBitPositionLow})");
        }

        // Ensure the out of band payload does not contain detection markers.
        for (int i = 0; i < ImageContent.Length - 1; i++)
        {
            if (ImageContent[i] == SegmentReader.MarkerIndicator)
            {
                byte potentialMarker = ImageContent[i + 1];
                if (potentialMarker == 0 || (potentialMarker >= (byte)Marker.Restart0 && potentialMarker <= (byte)Marker.Restart7))
                {
                    i++;
                }
                else
                {
                    throw new JfifException("The out of band payload contains a JFIF marker.");
                }
            }
        }

        // Compute the length and return it.
        const int fixedLengthPart = 4;
        return checked(fixedLengthPart + (ComponentDatas.Length * 2));
    }

    /// <summary>
    /// Writes the segment to the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to write to.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the segment has been written.
    /// </returns>
    /// <remarks>
    /// This method is not called if the segment does not have length.
    /// </remarks>
    public override async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
    {
        ValidateAndComputeLength();
        await stream.WriteUInt8Async((byte)ComponentDatas.Length, cancellationToken).ConfigureAwait(false);
        foreach (ComponentData componentData in ComponentDatas)
        {
            await stream.WriteUInt8Async(componentData.ComponentId, cancellationToken).ConfigureAwait(false);
            byte huffmanTable = (byte)((componentData.DirectCurrentHuffmanTable << 4) | componentData.AlternatingCurrentHuffmanTable);
            await stream.WriteUInt8Async(huffmanTable, cancellationToken).ConfigureAwait(false);
        }
        await stream.WriteUInt16Async(
            (ushort)((StartingDctCoefficient << 8) | EndingDctCoefficient),
            ByteConverter.BigEndian,
            cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt8Async(
            (byte)((SuccessiveApproximationBitPositionHigh << 4) | SuccessiveApproximationBitPositionLow),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override ValueTask WriteOutOfBandToStreamAsync(MarshalStream stream, CancellationToken cancellationToken) =>
        stream.WriteAsync(ImageContent.AsMemory(), cancellationToken);

    /// <summary>
    /// The component data is used to describe the image's color components and their respective Huffman tables.
    /// </summary>
    public struct ComponentData
    {
        /// <summary>
        /// The id of a component. For example, often Y=1, Cb=2, and Cr=3.
        /// </summary>
        public byte ComponentId { get; set; }

        /// <summary>
        /// The direct current Huffman table number used for this component.
        /// </summary>
        public byte DirectCurrentHuffmanTable { get; set; }

        /// <summary>
        /// The alternating current Huffman table used for this component.
        /// </summary>
        public byte AlternatingCurrentHuffmanTable { get; set; }
    }
}
