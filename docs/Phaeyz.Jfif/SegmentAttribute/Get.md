# SegmentAttribute.Get method (1 of 2)

Gets the [`SegmentAttribute`](../SegmentAttribute.md) of a segment class type.

```csharp
public static SegmentAttribute Get(Type segmentClassType)
```

| parameter | description |
| --- | --- |
| segmentClassType | The segment class type. |

## Return Value

The [`SegmentAttribute`](../SegmentAttribute.md) of a segment class type.

## Exceptions

| exception | condition |
| --- | --- |
| ArgumentException | The type associated with *segmentClassType* does not implement [`Segment`](../Segment.md), or the type does not have a public default parameterless constructor, or the type does not have a [`SegmentAttribute`](../SegmentAttribute.md). |
| ArgumentNullException | *segmentClassType* is `null`. |

## See Also

* class [SegmentAttribute](../SegmentAttribute.md)
* namespace [Phaeyz.Jfif](../../Phaeyz.Jfif.md)

---

# SegmentAttribute.Get&lt;T&gt; method (2 of 2)

Gets the [`SegmentAttribute`](../SegmentAttribute.md) of a segment class type.

```csharp
public static SegmentAttribute Get<T>()
    where T : Segment, new()
```

| parameter | description |
| --- | --- |
| T | The segment class type. |

## Return Value

The [`SegmentAttribute`](../SegmentAttribute.md) of a segment class type.

## Exceptions

| exception | condition |
| --- | --- |
| ArgumentException | The segment class type does not implement [`Segment`](../Segment.md), or the type does not have a public default parameterless constructor, or the type does not have a [`SegmentAttribute`](../SegmentAttribute.md). |

## See Also

* class [Segment](../Segment.md)
* class [SegmentAttribute](../SegmentAttribute.md)
* namespace [Phaeyz.Jfif](../../Phaeyz.Jfif.md)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
