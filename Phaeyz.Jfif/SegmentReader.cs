using Phaeyz.Marshalling;
using System.Text;

namespace Phaeyz.Jfif;

/// <summary>
/// A reader for JFIF segments.
/// </summary>
public class SegmentReader
{
    /// <summary>
    /// All segments beginning with this byte, and it always precedes a marker.
    /// </summary>
    public const byte MarkerIndicator = 0xFF;

    /// <summary>
    /// The length of the marker and indicator in bytes.
    /// </summary>
    public const ushort MarkerAndIndicatorLength = 2;

    private readonly SegmentDefinitions _segmentDefinitions;
    private readonly MarshalStream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentReader"/> class.
    /// </summary>
    /// <param name="stream">
    /// The stream containing JFIF segments.
    /// </param>
    /// <param name="segmentDefinitions">
    /// A set of segment definitions used for mapping to segment classes. If not provided, the default set will be used.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// The stream is not readable, or the buffer size is smaller than <see cref="Phaeyz.Jfif.SegmentReader.MarkerAndIndicatorLength"/>.
    /// </exception>
    public SegmentReader(MarshalStream stream, SegmentDefinitions? segmentDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("The stream is not readable.", nameof(stream));
        }

        if (!stream.IsFixedBuffer && stream.TotalBufferSize < SegmentReader.MarkerAndIndicatorLength)
        {
            throw new ArgumentException("The stream buffer size is too small for parsing.", nameof(stream));
        }

        _segmentDefinitions = segmentDefinitions ?? SegmentDefinitions.Default;
        _stream = stream;
    }

    /// <summary>
    /// Reads the next segment from the stream.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task yielding the next segment from the stream.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A valid JFIF marker indicator was not found.
    /// </exception>
    public async ValueTask<Segment> ReadAsync(CancellationToken cancellationToken = default)
    {
        // First read the marker at the current position on the stream.
        Marker marker = await ReadMarkerAsync(_stream, cancellationToken).ConfigureAwait(false);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        // There seems to be a bug in either C# compiler or VS that it cannot detect at the bottom of this method
        // when ReadFromStreamAsync is called, that segment is not null. The editor is complaining that with a
        // warning that segment is not initialized. To work around this bug, initialize it to null.
        Segment segment = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        // See if the marker can be mapped to a class type without an identifier. If this fails the marker
        // is either unknown or requires an identifier. In either case we will need to read a length field.
        Type? segmentClassType = _segmentDefinitions.GetNoIdentifierMapping(marker);
        if (segmentClassType is not null)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            segment = (Segment)Activator.CreateInstance(segmentClassType);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            // If the mapping has no length, we can short circuit and yield the segment now.
            if (!segment!.HasLength)
            {
                return segment;
            }
        }

        // For all other instances we read the segment length.
        ushort totalLength = await _stream.ReadUInt16Async(ByteConverter.BigEndian, cancellationToken).ConfigureAwait(false);
        SegmentLength length = new(marker, totalLength, (ushort)(totalLength - SegmentLength.FieldByteCount));

        if (segmentClassType is null)
        {
            // If this is a segment known to have an identifier, read it.
            if (_segmentDefinitions.HasIdentifier(marker))
            {
                MarshalStreamReadStringResult stringResult = await _stream.ReadStringAsync(
                    Encoding.ASCII,
                    length.Remaining,
                    MarshalStreamNullTerminatorBehavior.Stop,
                    cancellationToken).ConfigureAwait(false);
                length -= stringResult.BytesRead;

                // Is this specific identifier found for the marker?
                segmentClassType = _segmentDefinitions.GetIdentifierMapping(marker, stringResult.Value);
                if (segmentClassType is null)
                {
                    segment = new SegmentWithLength();
                    segment.InitializeGenericSegment((marker, stringResult.Value), true);
                }
                else
                {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    segment = (Segment)Activator.CreateInstance(segmentClassType);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                }
            }
            else
            {
                // The segment is not known, so create a generic segment object.
                segment = new SegmentWithLength();
                segment.InitializeGenericSegment((marker, null), true);
            }
        }

        // Now deserialize the segment from the stream.
        await segment!.ReadFromStreamAsync(_stream, length, cancellationToken).ConfigureAwait(false);
        return segment;
    }

    /// <summary>
    /// Probes the stream to see if the next bytes on the stream are the start of a JFIF image.
    /// Some files may have a series of JFIF segments one after the other.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task yielding <c>true</c> if the stream has the start of a JFIF image; otherwise <c>false</c>.
    /// </returns>
    public async ValueTask<bool> ProbeForStartOfImageAsync(CancellationToken cancellationToken = default)
    {
        if (_stream.IsFixedBuffer && _stream.TotalBufferSize < SegmentReader.MarkerAndIndicatorLength)
        {
            return false;
        }

        return await _stream.EnsureByteCountAvailableInBufferAsync(SegmentReader.MarkerAndIndicatorLength, cancellationToken).ConfigureAwait(false) &&
            _stream.BufferedReadableBytes.Span[0] == MarkerIndicator &&
            _stream.BufferedReadableBytes.Span[1] == (byte)Marker.StartOfImage;
    }

    /// <summary>
    /// Reads a JFIF marker indicator and marker from the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to read from.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task yielding the marker read from the stream.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A valid JFIF marker indicator was not found.
    /// </exception>
    public static async ValueTask<Marker> ReadMarkerAsync(MarshalStream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte indicator = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
        if (indicator != MarkerIndicator)
        {
            throw new JfifException("Expected JFIF marker indicator.");
        }

        return (Marker)await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
    }
}