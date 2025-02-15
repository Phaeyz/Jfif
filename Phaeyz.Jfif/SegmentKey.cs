using System.Diagnostics.CodeAnalysis;

namespace Phaeyz.Jfif;

/// <summary>
/// Uniquely identifies a segment type.
/// </summary>
public readonly struct SegmentKey(Marker marker, string? identifier) : IEquatable<SegmentKey>
{
    /// <summary>
    /// The marker of the segment.
    /// </summary>
    public Marker Marker { get; } = marker;

    /// <summary>
    /// The identifier of the segment.
    /// </summary>
    public string? Identifier { get; } = identifier;

    /// <summary>
    /// Tests two segment keys for equality.
    /// </summary>
    /// <param name="obj">
    /// The other segment key to compare.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment keys are the same; <c>false</c> otherwise.
    /// </returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is SegmentKey segmentKey && Equals(segmentKey);

    /// <summary>
    /// Tests two segment keys for equality.
    /// </summary>
    /// <param name="other">
    /// The other segment key to compare.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment keys are the same; <c>false</c> otherwise.
    /// </returns>
    public readonly bool Equals(SegmentKey other) => Marker == other.Marker && Identifier == other.Identifier;

    /// <summary>
    /// Gets a segment key for a segment class type.
    /// </summary>
    /// <typeparam name="T">
    /// The segment class type.
    /// </typeparam>
    /// <returns>
    /// The segment key for a segment class type.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// The segment class type does not implement <see cref="Phaeyz.Jfif.Segment"/>,
    /// or the type does not have a public default parameterless constructor,
    /// or the type does not have a <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </exception>
    public static SegmentKey Get<T>() where T : Segment, new() => Get(typeof(T));

    /// <summary>
    /// Gets a segment key for a segment class type.
    /// </summary>
    /// <param name="segmentClassType">
    /// The segment class type.
    /// </param>
    /// <returns>
    /// Gets the segment key for a segment class type.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// The type associated with <paramref name="segmentClassType"/> does not implement <see cref="Phaeyz.Jfif.Segment"/>,
    /// or the type does not have a public default parameterless constructor,
    /// or the type does not have a <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </exception>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="segmentClassType"/> is <c>null</c>.
    /// </exception>
    public static SegmentKey Get(Type segmentClassType) => SegmentAttribute.Get(segmentClassType).Key;

    /// <summary>
    /// Gets a hash code for the current instance.
    /// </summary>
    /// <returns>
    /// A hash code for the current instance.
    /// </returns>
    public override int GetHashCode() => HashCode.Combine(Marker, Identifier);

    /// <summary>
    /// Gets a friendly representation of the segment key.
    /// </summary>
    /// <returns>
    /// A friendly representation of the segment key.
    /// </returns>
    public override readonly string ToString() => Identifier is null ? Marker.ToString() : $"{Marker} [{Identifier}]";

    /// <summary>
    /// Tests two segment keys for equality.
    /// </summary>
    /// <param name="left">
    /// The first segment key.
    /// </param>
    /// <param name="right">
    /// The second segment key.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment keys are equal; <c>false</c> otherwise.
    /// </returns>
    public static bool operator ==(SegmentKey left, SegmentKey right) => left.Equals(right);

    /// <summary>
    /// Tests two segment keys for inequality.
    /// </summary>
    /// <param name="left">
    /// The first segment key.
    /// </param>
    /// <param name="right">
    /// The second segment key.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment keys are not equal; <c>false</c> otherwise.
    /// </returns>
    public static bool operator !=(SegmentKey left, SegmentKey right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts the segment key to a tuple.
    /// </summary>
    /// <param name="segmentKey">
    /// The segment key to convert.
    /// </param>
    public static implicit operator (Marker marker, string? identifier)(SegmentKey segmentKey) =>
        (segmentKey.Marker, segmentKey.Identifier);

    /// <summary>
    /// Implicitly converts a tuple to a segment key.
    /// </summary>
    /// <param name="segmentKey">
    /// The tuple to convert to a segment key.
    /// </param>
    public static implicit operator SegmentKey((Marker marker, string? identifier) segmentKey) =>
        new(segmentKey.marker, segmentKey.identifier);
}
