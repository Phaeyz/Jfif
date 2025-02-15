using System.Text;
using Phaeyz.Marshalling;

namespace Phaeyz.Jfif;

/// <summary>
/// A writer for JFIF segments.
/// </summary>
public class SegmentWriter
{
    private readonly MarshalStream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentWriter"/> class.
    /// </summary>
    /// <param name="stream">
    /// The stream to which JFIF segments will be written.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// The stream is not read-only.
    /// </exception>
    public SegmentWriter(MarshalStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanWrite)
        {
            throw new ArgumentException("The stream is read-only.", nameof(stream));
        }

        _stream = stream;
    }

    /// <summary>
    /// Writes a JFIF segment to the stream.
    /// </summary>
    /// <param name="segment">
    /// The segment to write.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the operation completes.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// The length of the segment identifier and body are too big.
    /// </exception>
    public async ValueTask WriteAsync(Segment segment, CancellationToken cancellationToken = default)
    {
        // If this is a segment without length, simply write the marker and any optional out-of-band data.
        if (!segment.HasLength)
        {
            await _stream.WriteUInt8Async(SegmentReader.MarkerIndicator, cancellationToken).ConfigureAwait(false);
            await _stream.WriteUInt8Async((byte)segment.Key.Marker, cancellationToken).ConfigureAwait(false);
            await segment.WriteOutOfBandToStreamAsync(_stream, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Otherwise a segment body is going to be written. Compute it's length so the length field may be written.
        // Do this before writing to the stream in the case validation fails.
        int segmentBodyLength = segment.ValidateAndComputeLength();
        int segmentIdentifierLength = segment.Key.Identifier?.Length + 1 ?? 0;
        int segmentTotalLength = checked(segmentBodyLength + segmentIdentifierLength + SegmentLength.FieldByteCount);
        if (segmentTotalLength > ushort.MaxValue)
        {
            throw new JfifException("The length of the segment identifier and body are too big.");
        }

        // Write the marker and segment length.
        await _stream.WriteUInt8Async(SegmentReader.MarkerIndicator, cancellationToken).ConfigureAwait(false);
        await _stream.WriteUInt8Async((byte)segment.Key.Marker, cancellationToken).ConfigureAwait(false);
        await _stream.WriteUInt16Async((ushort)segmentTotalLength, ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);

        // Write the optional segment identifier.
        if (segment.Key.Identifier is not null)
        {
            await _stream.WriteStringAsync(Encoding.ASCII, segment.Key.Identifier.AsMemory(), true, cancellationToken).ConfigureAwait(false);
        }

        // Finally serialize the segment body and write any optional out-of-band data.
        await segment.WriteToStreamAsync(_stream, cancellationToken).ConfigureAwait(false);
        await segment.WriteOutOfBandToStreamAsync(_stream, cancellationToken).ConfigureAwait(false);
    }
}
