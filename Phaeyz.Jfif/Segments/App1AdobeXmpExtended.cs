using Phaeyz.Marshalling;
using System.Security.Cryptography;
using System.Text;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// An App1 segment for an extended XMP packet.
/// </summary>
[Segment(Marker.App1, Namespace)]
public class App1AdobeXmpExtended : Segment
{
    /// <summary>
    /// The namespace which is used as the identifier for the extended XMP packet.
    /// </summary>
    private const string Namespace = "http://ns.adobe.com/xmp/extension/";

    /// <summary>
    /// The XMP spec implies an alignment padding of 2 bytes. This padding is not explicitly
    /// specified in XMP documentation, but is inferred when it mentions the maximum length
    /// of UTF-8 encoded bytes is 65502, when the computed maximum should be 65504. Apparently
    /// some older encoder and decoders used this as maximum constant. Therefore, our code
    /// will only use it during an encoding process.
    /// </summary>
    private const int c_alignmentPadding = 2;

    /// <summary>
    /// The maximum number of bytes which can be stored in a single extended XMP portion.
    /// </summary>
    public static readonly int MaxXmpUtf8BytesPerPortion =
        ushort.MaxValue - SegmentLength.FieldByteCount - (Namespace.Length + 1) - c_fullMd5DigestGuidByteLength - 8 - c_alignmentPadding;

    /// <summary>
    /// The number of bytes which make up the full MD5 digest guid.
    /// </summary>
    private const int c_fullMd5DigestGuidByteLength = 32;

    /// <summary>
    /// A guid which unique identifies a set of extended XMP portions.
    /// </summary>
    /// <remarks>
    /// This guid may be created by passing in all concatenated XMP portions to the
    /// <see cref="Phaeyz.Jfif.Segments.App1AdobeXmpExtended.CreateMd5DigestGuid"/> method.
    /// </remarks>
    public Guid FullMd5DigestGuid { get; set; }

    /// <summary>
    /// The length of all combined extended XMP portions in bytes.
    /// </summary>
    public uint FullXmpUtf8Length { get; set; }

    /// <summary>
    /// The zero-based byte offset of this XMP portion within the full extended XMP.
    /// </summary>
    public uint StartingOffsetInFullXmpUtf8 { get; set; }

    /// <summary>
    /// The UTF-8 encoded XMP portion.
    /// </summary>
    public byte[] XmpPortionUtf8 { get; set; } = [];

    /// <summary>
    /// Creates an MD5 digest guid from the full extended XMP.
    /// </summary>
    /// <param name="fullExtendedXmpUtf8">
    /// The full extended XMP in UTF-8 encoding.
    /// </param>
    /// <returns>
    /// An MD5 digest guid from the full extended XMP.
    /// </returns>
    public static Guid CreateMd5DigestGuid(
        ReadOnlySpan<byte> fullExtendedXmpUtf8)
    {
        Span<byte> hashBytes = stackalloc byte[16];
        MD5.HashData(fullExtendedXmpUtf8, hashBytes);
        return new(
            ByteConverter.BigEndian.ToUInt32(hashBytes[..4]),
            ByteConverter.BigEndian.ToUInt16(hashBytes[4..6]),
            ByteConverter.BigEndian.ToUInt16(hashBytes[6..8]),
            hashBytes[8],
            hashBytes[9],
            hashBytes[10],
            hashBytes[11],
            hashBytes[12],
            hashBytes[13],
            hashBytes[14],
            hashBytes[15]);
    }

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
        segmentLength -= c_fullMd5DigestGuidByteLength + 8;

        // From the spec: A 128-bit GUID stored as a 32-byte ASCII hex string, capital A-F, no null termination. The GUID is a 128-bit
        //                MD5 digest of the full ExtendedXMP serialization.
        MarshalStreamReadStringResult fullMd5DigestGuidReadResult = await stream.ReadStringAsync(
            Encoding.ASCII,
            c_fullMd5DigestGuidByteLength,
            MarshalStreamNullTerminatorBehavior.TrimTrailing,
            cancellationToken).ConfigureAwait(false);
        if (fullMd5DigestGuidReadResult.BytesRead != c_fullMd5DigestGuidByteLength ||
            !Guid.TryParseExact(fullMd5DigestGuidReadResult.Value, "N", out Guid fullMd5DigestGuid))
        {
            throw new JfifException($"An invalid Full MD5 Digest guid was encountered: \"{fullMd5DigestGuidReadResult.Value}\"");
        }

        FullMd5DigestGuid = fullMd5DigestGuid;
        FullXmpUtf8Length = await stream.ReadUInt32Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        StartingOffsetInFullXmpUtf8 = await stream.ReadUInt32Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        XmpPortionUtf8 = new byte[segmentLength.Remaining];
        await stream.ReadExactlyAsync(XmpPortionUtf8, 0, XmpPortionUtf8.Length, cancellationToken).ConfigureAwait(false);
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
        const int fixedLengthPart = c_fullMd5DigestGuidByteLength + 8;
        return checked(fixedLengthPart + XmpPortionUtf8.Length);
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
        await stream.WriteStringAsync(Encoding.ASCII, FullMd5DigestGuid.ToString("N").ToUpper().AsMemory(), false, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt32Async(FullXmpUtf8Length, ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        await stream.WriteUInt32Async(StartingOffsetInFullXmpUtf8, ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(XmpPortionUtf8.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
