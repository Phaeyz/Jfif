# JfifMetadata class

Encapsulates a series of JFIF segments which make up a valid JFIF file.

```csharp
public class JfifMetadata
```

## Public Members

| name | description |
| --- | --- |
| [JfifMetadata](JfifMetadata/JfifMetadata.md)() | The default constructor. |
| [Segments](JfifMetadata/Segments.md) { get; set; } | The segments in the JFIF metadata. It is up to the caller to ensure that the correct segments exist and that they are in the correct order. |
| [FindAll&lt;T&gt;](JfifMetadata/FindAll.md)() | Finds all segments of the specified type in the JFIF metadata. |
| [FindFirst&lt;T&gt;](JfifMetadata/FindFirst.md)() | Finds the first segment of the specified type in the JFIF metadata. |
| [FindFirst&lt;T&gt;](JfifMetadata/FindFirst.md)(…) | Finds the first segment of the specified type in the JFIF metadata. |
| [FindFirstIndex](JfifMetadata/FindFirstIndex.md)(…) | Finds the first segment with the specified key in the JFIF metadata. |
| [GetFirstOrCreate&lt;T&gt;](JfifMetadata/GetFirstOrCreate.md)(…) | Gets the first segment of the specified type in the JFIF metadata, or creates a new one if it does not exist. (2 methods) |
| [GetIndexAfter](JfifMetadata/GetIndexAfter.md)(…) | Gets the index after the last segment with any of the specified keys. |
| [Insert&lt;T&gt;](JfifMetadata/Insert.md)(…) | Inserts a segment after the last segment with any of the specified keys. |
| [RemoveAll](JfifMetadata/RemoveAll.md)(…) | Removes all segments with the specified key from the JFIF metadata. |
| [RemoveAll&lt;T&gt;](JfifMetadata/RemoveAll.md)() | Removes all segments of the specified type from the JFIF metadata. |
| [RemoveFirst](JfifMetadata/RemoveFirst.md)(…) | Removes the first segment with the specified key from the JFIF metadata. |
| [RemoveFirst&lt;T&gt;](JfifMetadata/RemoveFirst.md)() | Removes the first segment of the specified type from the JFIF metadata. |
| [WriteToStreamAsync](JfifMetadata/WriteToStreamAsync.md)(…) | Writes the JFIF metadata to the stream. |
| static [ReadAllFromStreamAsync](JfifMetadata/ReadAllFromStreamAsync.md)(…) | Reads all JFIF metadatas from the stream. |
| static [ReadFromStreamAsync](JfifMetadata/ReadFromStreamAsync.md)(…) | Read a JFIF metadata from the stream. |
| static [WriteAllToStreamAsync](JfifMetadata/WriteAllToStreamAsync.md)(…) | Writes all JFIF metadatas to the stream. |

## See Also

* namespace [Phaeyz.Jfif](../Phaeyz.Jfif.md)
* [JfifMetadata.cs](https://github.com/Phaeyz/Jfif/blob/main/Phaeyz.Jfif/JfifMetadata.cs)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
