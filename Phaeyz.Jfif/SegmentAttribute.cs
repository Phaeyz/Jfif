using System.Diagnostics.CodeAnalysis;

namespace Phaeyz.Jfif;

/// <summary>
/// Segment classes must be decorated with this attribute to indicate the eligible marker and optional identifier.
/// </summary>
/// <remarks>
/// Classes decorated with this attribute must also have a parameterless constructor.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SegmentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentAttribute"/> class.
    /// </summary>
    /// <param name="marker">
    /// The eligible marker for the segment class.
    /// </param>
    /// <param name="identifier">
    /// The identifier to be associated with the marker and segment class.
    /// </param>
    public SegmentAttribute(Marker marker, string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException("The identifier must not be null or empty.", nameof(identifier));
        }

        HasLength = true;
        Key = new SegmentKey(marker, identifier);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentAttribute"/> class.
    /// </summary>
    /// <param name="marker">
    /// The eligible marker for the segment class.
    /// </param>
    /// <param name="hasLength">
    /// If <c>true</c>, segments with marker contain a length field immediately after the marker.
    /// If <c>false</c> there is no length field and no segment body (as with start-of-image and end-of-image markers).
    /// The default value is <c>true</c>.
    /// </param>
    public SegmentAttribute(Marker marker, bool hasLength = true)
    {
        HasLength = hasLength;
        Key = new SegmentKey(marker, null);
    }

    /// <summary>
    /// If <c>true</c>, segments with marker contain a length field immediately after the marker.
    /// If <c>false</c> there is no length field and no segment body (as with start-of-image and end-of-image markers).
    /// </summary>
    public bool HasLength { get; private set; }

    /// <summary>
    /// A segment key which uniquely identifies a mapping to a segment.
    /// </summary>
    public SegmentKey Key { get; private set; }

    /// <summary>
    /// Gets the <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </summary>
    /// <typeparam name="T">
    /// The segment class type.
    /// </typeparam>
    /// <returns>
    /// The <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// The segment class type does not implement <see cref="Phaeyz.Jfif.Segment"/>,
    /// or the type does not have a public default parameterless constructor,
    /// or the type does not have a <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </exception>
    public static SegmentAttribute Get<T>() where T : Segment, new() => Get(typeof(T));

    /// <summary>
    /// Gets the <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </summary>
    /// <param name="segmentClassType">
    /// The segment class type.
    /// </param>
    /// <returns>
    /// The <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// The type associated with <paramref name="segmentClassType"/> does not implement <see cref="Phaeyz.Jfif.Segment"/>,
    /// or the type does not have a public default parameterless constructor,
    /// or the type does not have a <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </exception>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="segmentClassType"/> is <c>null</c>.
    /// </exception>
    public static SegmentAttribute Get(Type segmentClassType)
    {
        ArgumentNullException.ThrowIfNull(segmentClassType);

        if (!typeof(Segment).IsAssignableFrom(segmentClassType))
        {
            throw new ArgumentException($"Type '{segmentClassType.Name}' does not implement Segment.", nameof(segmentClassType));
        }

        if (segmentClassType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new ArgumentException($"Type '{segmentClassType.Name}' does not have a public default parameterless constructor.", nameof(segmentClassType));
        }

        SegmentAttribute? segmentAttribute = segmentClassType
            .GetCustomAttributes(typeof(SegmentAttribute), false)
            .Cast<SegmentAttribute>()
            .FirstOrDefault();

        return segmentAttribute is null
            ? throw new ArgumentException($"Type '{segmentClassType.Name}' does not have a SegmentAttribute.", nameof(segmentClassType))
            : segmentAttribute;
    }

    /// <summary>
    /// Gets the <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </summary>
    /// <typeparam name="T">
    /// The segment class type.
    /// </typeparam>
    /// <param name="segmentAttribute">
    /// If the method returns <c>true</c> this will receive the segment attribute. Otherwise it will receive <c>null</c>.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment class type is valid and the <paramref name="segmentAttribute"/> received the
    /// attribute; <c>false</c> otherwise.
    /// </returns>
    public static bool TryGet<T>([MaybeNullWhen(false)] out SegmentAttribute segmentAttribute) where T : Segment, new() =>
        TryGet(typeof(T), out segmentAttribute);

    /// <summary>
    /// Gets the <see cref="Phaeyz.Jfif.SegmentAttribute"/> of a segment class type.
    /// </summary>
    /// <param name="segmentClassType">
    /// The segment class type.
    /// </param>
    /// <param name="segmentAttribute">
    /// If the method returns <c>true</c> this will receive the segment attribute. Otherwise it will receive <c>null</c>.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the segment class type is valid and the <paramref name="segmentAttribute"/> received the
    /// attribute; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// This function always returns <c>false</c> if the segment class type does not derive from
    /// <see cref="Phaeyz.Jfif.Segment"/> or if the segment class type does not have a public default parameterless constructor.
    /// </remarks>
    public static bool TryGet(Type segmentClassType, [MaybeNullWhen(false)] out SegmentAttribute segmentAttribute)
    {
        if (segmentClassType is null ||
            !typeof(Segment).IsAssignableFrom(segmentClassType) ||
            segmentClassType.GetConstructor(Type.EmptyTypes) is not null)
        {
            segmentAttribute = null;
            return false;
        }
        segmentAttribute = segmentClassType
            .GetCustomAttributes(typeof(SegmentAttribute), false)
            .Cast<SegmentAttribute>()
            .FirstOrDefault();
        return segmentAttribute is not null;
    }
}