using Phaeyz.Marshalling;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// An App0 segment with "JFIF" as the identifier.
/// </summary>
[Segment(Marker.App0, "JFIF")]
public class App0Jfif : Segment
{
    /// <summary>
    /// The major value of the version of the latest known JFIF standard.
    /// </summary>
    /// <remarks>
    /// This value equates to the "1" in "1.02". The JFIF spec may be found at
    /// https://www.w3.org/Graphics/JPEG/jfif3.pdf and
    /// https://www.itu.int/rec/T-REC-T.871-201105-I/en.
    /// </remarks>
    public const byte LatestVersionMajor = 1;

    /// <summary>
    /// The minor value of the version of the latest known JFIF standard.
    /// </summary>
    /// <remarks>
    /// This value equates to the "02" in "1.02". The JFIF spec may be found at
    /// https://www.w3.org/Graphics/JPEG/jfif3.pdf and
    /// https://www.itu.int/rec/T-REC-T.871-201105-I/en.
    /// </remarks>
    public const byte LatestVersionMinor = 2;

    /// <summary>
    /// Major value of the JFIF version used during serialization.
    /// </summary>
    public byte VersionMajor { get; set; }

    /// <summary>
    /// Minor value of the JFIF version used during serialization.
    /// </summary>
    public byte VersionMinor { get; set; }

    /// <summary>
    /// The pixel density of the main image.
    /// </summary>
    public PixelDensityUnits PixelDensityUnits { get; set; }

    /// <summary>
    /// The horizontal pixel density of the image.
    /// </summary>
    public ushort HorizontalPixelDensity { get; set; }

    /// <summary>
    /// The vertical pixel density of the image.
    /// </summary>
    public ushort VerticalPixelDensity { get; set; }

    /// <summary>
    /// The width of the thumbnail in pixels.
    /// </summary>
    public byte ThumbnailHorizontalPixelWidth { get; set; }

    /// <summary>
    /// The height of the thumbnail in pixels.
    /// </summary>
    public byte ThumbnailVerticalPixelHeight { get; set; }

    /// <summary>
    /// The RGB data of the thumbnail.
    /// </summary>
    /// <remarks>
    /// Each pixel is 3-bytes (RGB) and the data is stored in row-major order.
    /// </remarks>
    public byte[] ThumbnailRgbData { get; set; } = [];

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
        segmentLength -= 9;
        VersionMajor = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        VersionMinor = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        PixelDensityUnits = (PixelDensityUnits)await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        HorizontalPixelDensity = await stream.ReadUInt16Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        VerticalPixelDensity = await stream.ReadUInt16Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        ThumbnailHorizontalPixelWidth = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        ThumbnailVerticalPixelHeight = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);

        ushort thumbnailLength = (ushort)(3 * ThumbnailHorizontalPixelWidth * ThumbnailVerticalPixelHeight);
        segmentLength -= thumbnailLength;
        ThumbnailRgbData = new byte[thumbnailLength];
        await stream.ReadExactlyAsync(ThumbnailRgbData, 0, thumbnailLength, cancellationToken).ConfigureAwait(false);

        // Skip past any other padding
        await stream.SkipAsync(segmentLength.Remaining, cancellationToken).ConfigureAwait(false);
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
        ushort thumbnailLength = (ushort)(3 * ThumbnailHorizontalPixelWidth * ThumbnailVerticalPixelHeight);
        if (thumbnailLength != ThumbnailRgbData.Length)
        {
            throw new JfifException("The length of the thumbnail RGB data does not match the expected length.");
        }

        const int fixedLengthPart = 9;
        return checked(fixedLengthPart + thumbnailLength);
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
        await stream.WriteUInt8Async(VersionMajor, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt8Async(VersionMinor, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt8Async((byte)PixelDensityUnits, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt16Async(HorizontalPixelDensity, ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt16Async(VerticalPixelDensity, ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt8Async(ThumbnailHorizontalPixelWidth, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt8Async(ThumbnailVerticalPixelHeight, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(ThumbnailRgbData, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// The pixel density of the image.
/// </summary>
public enum PixelDensityUnits : byte
{
    /// <summary>
    /// No pixel density is defined.
    /// </summary>
    None                = 0,

    /// <summary>
    /// The pixel density is pixels-per-inch.
    /// </summary>
    PixelsPerInch       = 1,

    /// <summary>
    /// The pixel density is pixels-per-centimeter.
    /// </summary>
    PixelsPerCentimeter = 2,
}