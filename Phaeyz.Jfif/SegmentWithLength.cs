using Phaeyz.Marshalling;

namespace Phaeyz.Jfif;

/// <summary>
/// A segment with a length. The JFIF reader will use this type for unknown segment types.
/// </summary>
public class SegmentWithLength : Segment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentWithLength"/> class.
    /// </summary>
    protected internal SegmentWithLength() : base() { }

    /// <summary>
    /// The data of the segment body.
    /// </summary>
    public byte[] Data { get; set; } = [];

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
        Data = new byte[segmentLength.Remaining];
        await stream.ReadExactlyAsync(Data, 0, Data.Length, cancellationToken).ConfigureAwait(false);
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
    public override int ValidateAndComputeLength() => Data is null ? 0 : Data.Length;

    /// <summary>
    /// Creates a friendly string for the current instance.
    /// </summary>
    /// <returns>
    /// A friendly string for the current instance.
    /// </returns>
    public override string ToString() => $"{Key.Marker} => {GetType().Name}, Length={Data.Length}";

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
        if (Data is not null)
        {
            await stream.WriteAsync(Data.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
