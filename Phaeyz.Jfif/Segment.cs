using Phaeyz.Marshalling;

namespace Phaeyz.Jfif;

/// <summary>
/// The base class for all segments.
/// </summary>
public abstract class Segment
{
    /// <summary>
    /// Called by base classes when initializing a segment.
    /// </summary>
    protected Segment()
    {
        Type segmentClassType = GetType();

        // Don't check by doing "this is SegmentWithLength" because SegmentWithLength is a special type not
        // decorated by SegmentAttribute. It is the default SegmentReader uses when a mapping could not be found.
        // Yet there may be derived instances of SegmentWithLength, and those instances require SegmentAttribute.
        if (segmentClassType != typeof(SegmentWithLength))
        {
            SegmentAttribute segmentAttribute = SegmentAttribute.Get(segmentClassType);
            HasLength = segmentAttribute.HasLength;
            Key = segmentAttribute.Key;
        }
    }

    /// <summary>
    /// Determines whether or not if the segment has a length field with potential data after it.
    /// </summary>
    public bool HasLength { get; private set; }

    /// <summary>
    /// A segment key which uniquely identifies a mapping to a segment.
    /// </summary>
    public SegmentKey Key { get; private set; }

    /// <summary>
    /// Internally used by <see cref="Phaeyz.Jfif.SegmentReader"/> to initialize fixed values for generic segments
    /// which have no class representation.
    /// </summary>
    /// <param name="segmentKey">
    /// The segment key which uniquely identifies the segment.
    /// </param>
    /// <param name="hasLength">
    /// Determines whether or not if the segment has a length field with potential data after it.
    /// </param>
    internal void InitializeGenericSegment(SegmentKey segmentKey, bool hasLength)
    {
        HasLength = hasLength;
        Key = segmentKey;
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
    public abstract ValueTask ReadFromStreamAsync(
        MarshalStream stream,
        SegmentLength segmentLength,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a friendly string representation of the segment.
    /// </summary>
    /// <returns>
    /// A friendly string representation of the segment.
    /// </returns>
    public override string ToString() => string.IsNullOrEmpty(Key.Identifier)
        ? $"{Key.Marker} => {GetType().Name}"
        : $"{Key.Marker} [{Key.Identifier}] => {GetType().Name}";

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
    public abstract int ValidateAndComputeLength();

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
    public abstract ValueTask WriteToStreamAsync(
        MarshalStream stream,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes out-of-band data to the stream. It is optional for segments to implement this method.
    /// </summary>
    /// <param name="stream">
    /// The stream to write to.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A task which is completed when the segment has been written.
    /// </returns>
    /// <remarks>
    /// Out-of-band content is content which is not part of the segment itself.
    /// An example of this is content which appears after a start-of-scan segment.
    /// </remarks>
    public virtual ValueTask WriteOutOfBandToStreamAsync(
        MarshalStream stream,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}