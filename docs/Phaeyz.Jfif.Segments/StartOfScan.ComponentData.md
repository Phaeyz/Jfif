# StartOfScan.ComponentData structure

The component data is used to describe the image's color components and their respective Huffman tables.

```csharp
public struct ComponentData
```

## Public Members

| name | description |
| --- | --- |
| [AlternatingCurrentHuffmanTable](StartOfScan.ComponentData/AlternatingCurrentHuffmanTable.md) { get; set; } | The alternating current Huffman table used for this component. |
| [ComponentId](StartOfScan.ComponentData/ComponentId.md) { get; set; } | The id of a component. For example, often Y=1, Cb=2, and Cr=3. |
| [DirectCurrentHuffmanTable](StartOfScan.ComponentData/DirectCurrentHuffmanTable.md) { get; set; } | The direct current Huffman table number used for this component. |

## See Also

* class [StartOfScan](./StartOfScan.md)
* namespace [Phaeyz.Jfif.Segments](../Phaeyz.Jfif.md)

<!-- DO NOT EDIT: generated by xmldocmd for Phaeyz.Jfif.dll -->
