# SegmentDefinitions class

Used by segment readers to determine which classes to use for markers.

```csharp
public class SegmentDefinitions
```

## Public Members

| name | description |
| --- | --- |
| [SegmentDefinitions](SegmentDefinitions/SegmentDefinitions.md)() | Initializes a new instance of the [`SegmentDefinitions`](./SegmentDefinitions.md) class. |
| [SegmentDefinitions](SegmentDefinitions/SegmentDefinitions.md)(…) | Initializes a new instance of the [`SegmentDefinitions`](./SegmentDefinitions.md) class, and clones a preexisting instance. |
| static [Default](SegmentDefinitions/Default.md) { get; } | Gets the default segment definitions. |
| [IsReadOnly](SegmentDefinitions/IsReadOnly.md) { get; } | Gets a value indicating whether the current object is read-only. |
| [Add](SegmentDefinitions/Add.md)(…) | Adds a mappings for a marker and identifier to a segment class. |
| [Add&lt;T&gt;](SegmentDefinitions/Add.md)(…) | Adds a mappings for a marker and identifier to a segment class. |
| [AddAllInAssembly](SegmentDefinitions/AddAllInAssembly.md)(…) | Adds all eligible mappings for markers and identifiers to a segment classes in an assembly. |
| [GetIdentifierMapping](SegmentDefinitions/GetIdentifierMapping.md)(…) | Gets the segment class type for a marker without an identifier. |
| [GetNoIdentifierMapping](SegmentDefinitions/GetNoIdentifierMapping.md)(…) | Gets the segment class type for a marker without an identifier. |
| [HasIdentifier](SegmentDefinitions/HasIdentifier.md)(…) | Indicates whether or not the marker is mapped to a segment class with an identifier. |
| [MakeReadOnly](SegmentDefinitions/MakeReadOnly.md)() | Prevents further changes to the current segment definitions object. |

## See Also

* namespace [Phaeyz.Jfif](../Phaeyz.Jfif.md)
* [SegmentDefinitions.cs](https://github.com/Phaeyz/Jfif/blob/main/Phaeyz.Jfif/SegmentDefinitions.cs)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
