# Phaeyz

Phaeyz is a set of libraries created and polished over time for use with other projects, and made available here for convenience.

All Phaeyz libraries may be found [here](https://github.com/Phaeyz).

# Phaeyz.Jfif

API documentation for **Phaeyz.Jfif** library is [here](https://github.com/Phaeyz/Jfif/blob/main/docs/Phaeyz.Jfif.md).

This library contains classes which allow for deserializing JFIF (JPEG File Interchange Format, i.e. .JPG files), editing and adding JFIF segments, as well as serializing it back out. Additionally there is utility for deserializing and serializing EXIF and XMP metadata out of segments as it is not straightforward due to segment length limitations. The deserializer and serializer was written such that if segments types are not built-in or supported by Phaeyz.Jfif, they are stored as generic segments, and segment types can be extended to make them strongly typed.

Note this library does not protect you from creating non-standard JFIF. Some segments may be required and in certain orders in some contexts. Furthermore, this library only provides raw structural data for JFIF, and currently does not provide decoding and encoding of image data -- though image data will be readily available through this library.

For more information on JFIF, see [JFIF on Wikipedia](https://en.wikipedia.org/wiki/JPEG_File_Interchange_Format), [JFIF 1.02 Specification on W3](http://www.w3.org/Graphics/JPEG/jfif3.pdf) and/or [JFIF ECMA International Standard](https://ecma-international.org/publications-and-standards/technical-reports/ecma-tr-98/).

Here are some highlights of this library.

## JfifMetadata (deserializing)

```C#
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream);
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    StartOfScan? startOfScan = jfifMetadata.FindFirst<StartOfScan>(); // Get the SOS segment
}
```

## JfifMetadata (serializing)

```C#
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream);
// Make an output stream
using MemoryStream memoryStream = new();
using MarshalStream outputStream = new MarshalStream(memoryStream, false); // Used to efficient write file
await JfifMetadata.WriteAllToStreamAsync(outputStream, jfifMetadatas); // Can write all JFIF metadata
// Can also write independent JFIF metadata
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    await jfifMetadata.WriteToStreamAsync(outputStream);
}
```

## JfifMetadata (removing segments)

```C#
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream);
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    // JfifMetadata is basically a collection of segments, and segments may be removed
    jfifMetadata.RemoveAll<App1AdobeXmp>(); // Remove all XMP segments
    jfifMetadata.RemoveAll<App1AdobeXmpExtended>(); // Remove all XMP segments
}
```

## ExifProvider (read and write EXIF)

```C#
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream);
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    // Read EXIF from the JFIF metadata
    byte[]? exifBuffer = ExifProvider.DeserializeFromJfif(jfifMetadata);

    // Write EXIF back to JFIF metadata, and it will be split if necessary and positioned correctly.
    ExifProvider.SerializeToJfif(exifBuffer, jfifMetadata);
}
```

## AdobeXmpProvider (read and write XMP)

```C#
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream);
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    // Read XMP from the JFIF metadata
    string? xmp = AdobeXmpProvider.DeserializeFromJfif(jfifMetadata);

    // Write XMP back to JFIF metadata, and extended XMP may be created as necessary.
    AdobeXmpProvider.SerializeToJfif(xmp, jfifMetadata);
}
```

## SegmentDefinitions (custom segments)

```C#
// Define custom segment
[Segment((Marker)0xA9, "http://ns.custom-segment.contoso.com/1.0")] // Optionally, with an identifier
public class CustomSegment : Segment
{
    public int Value { get; set; }
    public override async ValueTask ReadFromStreamAsync(MarshalStream stream, SegmentLength length, CancellationToken cancellationToken)
    {
        length -= 4; // Will throw if insufficient length available.
        Value = await stream.ReadInt32Async(ByteConverter.LittleEndian);
        // Skip past any other padding
        await stream.SkipAsync(length.Remaining, cancellationToken).ConfigureAwait(false);
    }
    // If there is more complex or structured data, this method would throw if the segment is invalid
    public override int ValidateAndComputeLength() => 4;
    public override async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
    {
        await stream.WriteInt32Async(Value, ByteConverter.LittleEndian);
    }
}

// Define the segment
SegmentDefinitions segmentDefinitions = new(SegmentDefinitions.Default);
segmentDefinitions.Add<CustomSegment>();
// Now deserialize a stream with support for that segment.
using FileStream fileStream = File.OpenRead(filePath);
using MarshalStream inputStream = new MarshalStream(fileStream, false); // Used to efficiently read file
// Some files have back-to-back JFIF streams, where the second is a thumbnail or grayscale version.
// Instead of using ReadFromStreamAsync, ReadAllFromStreamAsync may extract them all into a list.
// Notice segmentDefinitions is passed in here.
List<JfifMetadata> jfifMetadatas = await JfifMetadata.ReadAllFromStreamAsync(inputStream, segmentDefinitions);
foreach (JfifMetadata jfifMetadata in jfifMetadatas)
{
    CustomSegment? customSegment = jfifMetadata.FindFirst<CustomSegment>(); // Now fetch the custom segment
}
```

# Licensing

This project is licensed under GNU General Public License v3.0, which means you can use it for personal or educational purposes for free. However, donations are always encouraged to support the ongoing development of adding new features and resolving issues.

If you plan to use this code for commercial purposes or within an organization, we kindly ask for a donation to support the project's development. Any reasonably sized donation amount which reflects the perceived value of using Phaeyz in your product or service is accepted.

## Donation Options

There are several ways to support Phaeyz with a donation. Perhaps the best way is to use Patreon so that recurring small donations continue to support the development of Phaeyz.

- **Patreon**: [https://www.patreon.com/phaeyz](https://www.patreon.com/phaeyz)
- **Bitcoin**: Send funds to address: ```bc1qdzdahz8d7jkje09fg7s7e8xedjsxm6kfhvsgsw```
- **PayPal**: Send funds to ```phaeyz@pm.me``` ([directions](https://www.paypal.com/us/cshelp/article/how-do-i-send-money-help293))

Your support is greatly appreciated and helps me continue to improve and maintain Phaeyz!