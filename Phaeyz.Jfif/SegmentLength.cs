namespace Phaeyz.Jfif;

/// <summary>
/// Tracks the deserialized length of a segment.
/// </summary>
public struct SegmentLength
{
    /// <summary>
    /// The number of bytes in the length field.
    /// </summary>
    public const ushort FieldByteCount = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentLength"/> struct.
    /// </summary>
    /// <param name="marker">
    /// The marker of the segment currently being deserialized.
    /// </param>
    /// <param name="totalLength">
    /// The total length of the segment, which does include the length field.
    /// </param>
    /// <param name="remainingLength">
    /// The remaining length of the segment.
    /// </param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The total length is either shorter than the minimum field byte count, or less than <paramref name="remainingLength"/>.
    /// </exception>
    public SegmentLength(Marker marker, ushort totalLength, ushort remainingLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLength, FieldByteCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLength, remainingLength);

        Marker = marker;
        Total = totalLength;
        Remaining = remainingLength;
    }

    /// <summary>
    /// The marker of the segment currently being deserialized.
    /// </summary>
    public readonly Marker Marker { get; }

    /// <summary>
    /// The remaining length of the segment.
    /// </summary>
    public readonly ushort Remaining { get; }

    /// <summary>
    /// The total length of the segment, which does include the length field.
    /// </summary>
    public readonly ushort Total { get; }

    /// <summary>
    /// Subtracts a field length from the remaining length.
    /// </summary>
    /// <param name="fieldLength">
    /// The length of the field to subtract from the remaining length.
    /// </param>
    /// <returns>
    /// The new <see cref="Phaeyz.Jfif.SegmentLength"/> with the field length subtracted.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// Attempted to subtract more length than is available.
    /// </exception>
    public readonly SegmentLength Subtract(int fieldLength)
    {
        if (Remaining < fieldLength)
        {
            throw new JfifException($"Expected more header length in segment \"{Marker}\".");
        }

        return new SegmentLength(Marker, Total, (ushort)(Remaining - fieldLength));
    }

    /// <summary>
    /// Subtracts a field length from the remaining length.
    /// </summary>
    /// <param name="fieldLength">
    /// The length of the field to subtract from the remaining length.
    /// </param>
    /// <returns>
    /// The new <see cref="Phaeyz.Jfif.SegmentLength"/> with the field length subtracted.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// Attempted to subtract more length than is available.
    /// </exception>
    public readonly SegmentLength Subtract(long fieldLength)
    {
        if (Remaining < fieldLength)
        {
            throw new Exception($"Expected more header length in segment \"{Marker}\".");
        }

        return new SegmentLength(Marker, Total, (ushort)(Remaining - fieldLength));
    }

    /// <summary>
    /// Subtracts a field length from the remaining length.
    /// </summary>
    /// <param name="length">
    /// The segment length to subtract from.
    /// </param>
    /// <param name="fieldLength">
    /// The length of the field to subtract from the remaining length.
    /// </param>
    /// <returns>
    /// The new <see cref="Phaeyz.Jfif.SegmentLength"/> with the field length subtracted.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// Attempted to subtract more length than is available.
    /// </exception>
    public static SegmentLength operator -(SegmentLength length, int fieldLength) => length.Subtract(fieldLength);

    /// <summary>
    /// Subtracts a field length from the remaining length.
    /// </summary>
    /// <param name="length">
    /// The segment length to subtract from.
    /// </param>
    /// <param name="fieldLength">
    /// The length of the field to subtract from the remaining length.
    /// </param>
    /// <returns>
    /// The new <see cref="Phaeyz.Jfif.SegmentLength"/> with the field length subtracted.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// Attempted to subtract more length than is available.
    /// </exception>
    public static SegmentLength operator -(SegmentLength length, long fieldLength) => length.Subtract(fieldLength);

    /// <summary>
    /// Subtracts a single byte from the remaining length.
    /// </summary>
    /// <param name="length">
    /// The segment length to subtract from.
    /// </param>
    /// <returns>
    /// The new <see cref="Phaeyz.Jfif.SegmentLength"/> with one byte subtracted.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// The segment length already has zero remaining bytes.
    /// </exception>
    public static SegmentLength operator --(SegmentLength length) => length.Subtract(1);

    /// <summary>
    /// Creates a friendly string for the segment length.
    /// </summary>
    /// <returns>
    /// A friendly string for the segment length.
    /// </returns>
    public override readonly string ToString() => $"Total={Total}, Remaining={Remaining}, Marker={Marker}";
}
