using Phaeyz.Marshalling;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// An App1 segment for an EXIF metadata.
/// </summary>
[Segment(Marker.App1, "Exif")]
public class App1Exif : Segment
{
    /// <summary>
    /// The maximum number of bytes allowed in the EXIF buffer per segment.
    /// </summary>
    // 6 = length of "Exif" plus null terminator plus extra nil.
    public const int MaxExifBytes = ushort.MaxValue - SegmentLength.FieldByteCount - 6;

    /// <summary>
    /// The EXIF metadata.
    /// </summary>
    /// <remarks>
    /// The <c>ExifMetadata</c> class in the <c>Phaeyz.Exif</c> library may be used to serialize and deserialize this data.
    /// </remarks>
    public byte[] Exif { get; set; } = [];

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
        segmentLength -= 1;
        byte extraNil = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false); // The "Exif" identifier is followed by an extra NIL.
        if (extraNil != 0)
        {
            throw new JfifException("Expected extra NIL after the Exif identifier.");
        }

        Exif = new byte[segmentLength.Remaining];
        await stream.ReadExactlyAsync(Exif, 0, Exif.Length, cancellationToken).ConfigureAwait(false);
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
        if (Exif?.Length > MaxExifBytes)
        {
            throw new JfifException($"The EXIF buffer is too big. The maximum size is {MaxExifBytes} bytes.");
        }
        const int fixedLengthPart = 1;
        return checked(fixedLengthPart + (Exif?.Length ?? 0));
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
        await stream.WriteUInt8Async(0, cancellationToken).ConfigureAwait(false); // The "Exif" identifier is followed by an extra NIL.
        if (Exif is not null)
        {
            await stream.WriteAsync(Exif, cancellationToken).ConfigureAwait(false);
        }
    }
}