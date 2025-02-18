# JfifMetadata.GetIndexAfter method

Gets the index after the last segment with any of the specified keys.

```csharp
public int GetIndexAfter(IEnumerable<SegmentKey> segmentKeys)
```

| parameter | description |
| --- | --- |
| segmentKeys | The keys of the segments to find. |

## Return Value

Returns the index after the last segment with any of the specified keys, or `0` if none were found.

## See Also

* struct [SegmentKey](../SegmentKey.md)
* class [JfifMetadata](../JfifMetadata.md)
* namespace [Phaeyz.Jfif](../../Phaeyz.Jfif.md)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
