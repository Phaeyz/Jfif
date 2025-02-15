using System.Reflection;

namespace Phaeyz.Jfif;

/// <summary>
/// Used by segment readers to determine which classes to use for markers.
/// </summary>
public class SegmentDefinitions
{
    /// <summary>
    /// A dictionary of classes for segments without an identifier.
    /// </summary>
    private readonly Dictionary<Marker, Type> _noIdentifierClasses = [];

    /// <summary>
    /// A dictionary of dictionaries of classes for segments with an identifier.
    /// </summary>
    private readonly Dictionary<Marker, Dictionary<string, Type>> _identifierClasses = [];

    /// <summary>
    /// Initializes a value containing the default segment definitions.
    /// </summary>
    private static readonly Lazy<SegmentDefinitions> s_default = new(() => CreateDefault().MakeReadOnly());

    /// <summary>
    /// Gets the default segment definitions.
    /// </summary>
    public static SegmentDefinitions Default => s_default.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentDefinitions"/> class.
    /// </summary>
    public SegmentDefinitions()
    {
        _noIdentifierClasses = [];
        _identifierClasses = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Phaeyz.Jfif.SegmentDefinitions"/> class,
    /// and clones a preexisting instance.
    /// </summary>
    /// <param name="segmentDefinitions">
    /// The segment definitions to clone into the new instance.
    /// For example, this could be the default segment definitions.
    /// </param>
    public SegmentDefinitions(SegmentDefinitions segmentDefinitions)
    {
        _noIdentifierClasses = new Dictionary<Marker, Type>(segmentDefinitions._noIdentifierClasses);
        _identifierClasses = new Dictionary<Marker, Dictionary<string, Type>>(segmentDefinitions._identifierClasses.Count);
        foreach (KeyValuePair<Marker, Dictionary<string, Type>> kvp in segmentDefinitions._identifierClasses)
        {
            _identifierClasses.Add(kvp.Key, new Dictionary<string, Type>(kvp.Value));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current object is read-only.
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Adds a mappings for a marker and identifier to a segment class.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment class to add mappings for.
    /// </typeparam>
    /// <param name="overrideExistingMappings">
    /// If <c>true</c> and there are matching markers and/or identifiers, they will be removed
    /// and replaced. If <c>false</c>, an exception will be thrown. The default value is <c>false</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The current object is read-only.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// A configuration issue occurred with the segment class,
    /// or a mapping for matching markers and/or identifiers already exists.
    /// </exception>
    /// <remarks>
    /// The segment class must implement <see cref="Phaeyz.Jfif.Segment"/> and be decorated
    /// with <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </remarks>
    public void Add<T>(bool overrideExistingMappings = false) where T : Segment, new() =>
        Add(typeof(T), overrideExistingMappings);

    /// <summary>
    /// Adds a mappings for a marker and identifier to a segment class.
    /// </summary>
    /// <param name="segmentClassType">
    /// The type of segment class to add mappings for.
    /// </param>
    /// <param name="overrideExistingMappings">
    /// If <c>true</c> and there are matching markers and/or identifiers, they will be removed
    /// and replaced. If <c>false</c>, an exception will be thrown. The default value is <c>false</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The current object is read-only.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// A configuration issue occurred with the segment class,
    /// or a mapping for matching markers and/or identifiers already exists.
    /// </exception>
    /// <remarks>
    /// The segment class must implement <see cref="Phaeyz.Jfif.Segment"/> and be decorated
    /// with <see cref="Phaeyz.Jfif.SegmentAttribute"/>.
    /// </remarks>
    public void Add(Type segmentClassType, bool overrideExistingMappings = false)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The current object is read-only.");
        }

        SegmentAttribute attribute = SegmentAttribute.Get(segmentClassType);

        if (string.IsNullOrEmpty(attribute.Key.Identifier))
        {
            if (_identifierClasses.ContainsKey(attribute.Key.Marker))
            {
                if (overrideExistingMappings)
                {
                    _identifierClasses.Remove(attribute.Key.Marker);
                }
                else
                {
                    throw new ArgumentException($"A mapping for marker '{attribute.Key.Marker}' with an identifier already exists.", nameof(segmentClassType));
                }
            }

            if (_noIdentifierClasses.TryGetValue(attribute.Key.Marker, out Type? existingSegmentClassType))
            {
                if (overrideExistingMappings)
                {
                    _noIdentifierClasses[attribute.Key.Marker] = segmentClassType;
                }
                else
                {
                    if (segmentClassType != existingSegmentClassType)
                    {
                        throw new ArgumentException($"A mapping for marker '{attribute.Key.Marker}' without an identifier already exists.", nameof(segmentClassType));
                    }
                }
            }
            else
            {
                _noIdentifierClasses.Add(attribute.Key.Marker, segmentClassType);
            }
        }
        else
        {
            if (_noIdentifierClasses.ContainsKey(attribute.Key.Marker))
            {
                if (overrideExistingMappings)
                {
                    _noIdentifierClasses.Remove(attribute.Key.Marker);
                }
                else
                {
                    throw new ArgumentException($"A mapping for marker '{attribute.Key.Marker}' without an identifier already exists.", nameof(segmentClassType));
                }
            }

            if (!_identifierClasses.TryGetValue(attribute.Key.Marker, out Dictionary<string, Type>? mappings))
            {
                _identifierClasses.Add(attribute.Key.Marker, mappings = []);
            }

            if (mappings.TryGetValue(attribute.Key.Identifier, out Type? classType))
            {
                if (overrideExistingMappings)
                {
                    mappings[attribute.Key.Identifier] = segmentClassType;
                }
                else if (segmentClassType != classType)
                {
                    throw new ArgumentException($"A mapping for marker '{attribute.Key.Marker}' with identifier '{attribute.Key.Identifier}' already exists.", nameof(segmentClassType));
                }
            }
            else
            {
                mappings.Add(attribute.Key.Identifier, segmentClassType);
            }
        }
    }

    /// <summary>
    /// Adds all eligible mappings for markers and identifiers to a segment classes in an assembly.
    /// </summary>
    /// <param name="assembly">
    /// The assembly containing the segment classes to add mappings for.
    /// </param>
    /// <param name="overrideExistingMappings">
    /// If <c>true</c> and there are matching markers and/or identifiers, they will be removed
    /// and replaced. If <c>false</c>, an exception will be thrown. The default value is <c>false</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The current object is read-only.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// A mapping for matching markers and/or identifiers already exists.
    /// </exception>
    public void AddAllInAssembly(Assembly assembly, bool overrideExistingMappings = false)
    {
        var segmentType = typeof(Segment);
        var segmentAttributeType = typeof(SegmentAttribute);

        foreach (Type type in assembly
            .GetTypes()
            .Where(t =>
                segmentType.IsAssignableFrom(t) &&
                t.GetConstructor(Type.EmptyTypes) is not null &&
                t.GetCustomAttributes(segmentAttributeType, false).Length != 0))
        {
            Add(type, overrideExistingMappings);
        }
    }

    /// <summary>
    /// Used internally to create the default segment definitions.
    /// </summary>
    /// <returns>
    /// The default segment definitions.
    /// </returns>
    private static SegmentDefinitions CreateDefault()
    {
        SegmentDefinitions segmentDefinitions = new();
        segmentDefinitions.AddAllInAssembly(Assembly.GetExecutingAssembly());
        return segmentDefinitions;
    }

    /// <summary>
    /// Gets the segment class type for a marker without an identifier.
    /// </summary>
    /// <param name="marker">
    /// The marker to map to a segment class type.
    /// </param>
    /// <returns>
    /// The segment class type, or <c>null</c> if a mapping is not found.
    /// </returns>
    public Type? GetNoIdentifierMapping(Marker marker) =>
        _noIdentifierClasses.TryGetValue(marker, out Type? segmentClassType) ? segmentClassType : null;

    /// <summary>
    /// Gets the segment class type for a marker without an identifier.
    /// </summary>
    /// <param name="marker">
    /// The marker to map to a segment class type.
    /// </param>
    /// <param name="identifier">
    /// The identifier to map to a segment class type.
    /// </param>
    /// <returns>
    /// The segment class type, or <c>null</c> if a mapping is not found.
    /// </returns>
    public Type? GetIdentifierMapping(Marker marker, string identifier) =>
        _identifierClasses.TryGetValue(marker, out Dictionary<string, Type>? mappings) &&
            mappings.TryGetValue(identifier, out Type? mapping) ? mapping : null;

    /// <summary>
    /// Indicates whether or not the marker is mapped to a segment class with an identifier.
    /// </summary>
    /// <param name="marker">
    /// The segment marker.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the marker is mapped to a segment class with an identifier, <c>false</c> otherwise.
    /// </returns>
    public bool HasIdentifier(Marker marker) => _identifierClasses.ContainsKey(marker);

    /// <summary>
    /// Prevents further changes to the current segment definitions object.
    /// </summary>
    /// <returns>
    /// The current segment definitions object.
    /// </returns>
    public SegmentDefinitions MakeReadOnly()
    {
        IsReadOnly = true;
        return this;
    }
}
