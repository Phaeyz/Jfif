using Phaeyz.Marshalling;

namespace Phaeyz.Jfif;

/// <summary>
/// A segment without length. This class is provided just for convenience to prevent having to implement unused methods.
/// </summary>
/// <remarks>
/// The derived class must use <see cref="Phaeyz.Jfif.SegmentAttribute"/> to indicate the segment has no length.
/// </remarks>
public abstract class SegmentWithoutLength : Segment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentWithoutLength"/> class.
    /// </summary>
    protected SegmentWithoutLength() : base() { }

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
    public override ValueTask ReadFromStreamAsync(MarshalStream stream, SegmentLength segmentLength, CancellationToken cancellationToken)
    {
        // This won't be called on segments without length.
        throw new NotImplementedException();
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
        // This won't be called on segments without length.
        throw new NotImplementedException();
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
    public override ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
    {
        // This won't be called on segments without length.
        throw new NotImplementedException();
    }
}