using Phaeyz.Marshalling;
using System.Text;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// An App1 segment for a standard XMP packet.
/// </summary>
[Segment(Marker.App1, Namespace)]
public class App1AdobeXmp : Segment
{
    /// <summary>
    /// The namespace which is used as the identifier for the XMP packet.
    /// </summary>
    private const string Namespace = "http://ns.adobe.com/xap/1.0/";

    /// <summary>
    /// The XMP spec implies an alignment padding of 2 bytes. This padding is not explicitly
    /// specified in XMP documentation, but is inferred when it mentions the maximum length
    /// of UTF-8 encoded bytes is 65502, when the computed maximum should be 65504. Apparently
    /// some older encoder and decoders used this as maximum constant. Therefore, our code
    /// will only use it during an encoding process.
    /// </summary>
    private const int c_alignmentPadding = 2;

    /// <summary>
    /// The maximum number of UTF-8 encoded bytes which can be stored in the XMP packet.
    /// </summary>
    public static readonly int MaxXmpUtf8Bytes =
        ushort.MaxValue - SegmentLength.FieldByteCount - (Namespace.Length + 1) - c_alignmentPadding;

    /// <summary>
    /// The XMP packet.
    /// </summary>
    public string XmpPacket { get; set; } = string.Empty;

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
        MarshalStreamReadStringResult readXmpResult = await stream.ReadStringAsync(
            Encoding.UTF8,
            segmentLength.Remaining,
            MarshalStreamNullTerminatorBehavior.Stop,
            cancellationToken).ConfigureAwait(false);
        XmpPacket = readXmpResult.Value;
        segmentLength -= readXmpResult.BytesRead;

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
        if (XmpPacket is null)
        {
            throw new JfifException("The XMP packet is null.");
        }

        return Encoding.UTF8.GetByteCount(XmpPacket);
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
        await stream.WriteStringAsync(Encoding.UTF8, XmpPacket.AsMemory(), false, cancellationToken).ConfigureAwait(false);
    }
}
