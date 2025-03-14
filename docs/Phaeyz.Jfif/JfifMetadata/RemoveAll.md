# JfifMetadata.RemoveAll method (1 of 2)

Removes all segments with the specified key from the JFIF metadata.

```csharp
public int RemoveAll(SegmentKey segmentKey)
```

| parameter | description |
| --- | --- |
| segmentKey | The key of the segment to remove. |

## Return Value

The number of matching segments removed.

## See Also

* struct [SegmentKey](../SegmentKey.md)
* class [JfifMetadata](../JfifMetadata.md)
* namespace [Phaeyz.Jfif](../../Phaeyz.Jfif.md)

---

# JfifMetadata.RemoveAll&lt;T&gt; method (2 of 2)

Removes all segments of the specified type from the JFIF metadata.

```csharp
public int RemoveAll<T>()
    where T : Segment, new()
```

| parameter | description |
| --- | --- |
| T | The type of segment to remove. |

## Return Value

The number of matching segments removed.

## See Also

* class [Segment](../Segment.md)
* class [JfifMetadata](../JfifMetadata.md)
* namespace [Phaeyz.Jfif](../../Phaeyz.Jfif.md)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
