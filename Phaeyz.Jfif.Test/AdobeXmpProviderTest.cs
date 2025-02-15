using System.Text;
using System.Xml;
using Phaeyz.Jfif.Segments;
using Phaeyz.Xml;

namespace Phaeyz.Jfif.Test;

internal class AdobeXmlProviderTest
{
    private static readonly Encoding s_utf8WithoutBom = new UTF8Encoding(false);
    private static readonly Encoding s_utf16WithoutBom = new UnicodeEncoding(false, false);

    private const string c_basicXmp = """
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/">
              <crs:foo></crs:foo>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        """;

    private const string c_twoDescriptionsXmp = """
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/">
              <crs:foo></crs:foo>
            </rdf:Description>
            <rdf:Description xmlns:photoshop="http://ns.adobe.com/photoshop/1.0/">
              <photoshop:bar></photoshop:bar>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        """;

    public static string ParseAndWriteToString(string xml)
    {
        XmlDocument doc = new();
        doc.LoadXml(xml);
        return WriteToString(doc.DocumentElement!);
    }

    public static string WriteToString(XmlElement xmlElement)
    {
        XmlWriterSettings settings = new()
        {
            Encoding = s_utf16WithoutBom,
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = true,
        };
        StringBuilder sb = new();
        using (XmlWriter xmlWriter = XmlWriter.Create(sb, settings))
        {
            xmlElement.WriteTo(xmlWriter);
        }
        return sb.ToString();
    }

    [Test]
    public async Task DeserializeFromJfif_BlankXmpSegment_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        string? xmp = AdobeXmpProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(() => xmp).IsNull();
    }

    [Test]
    public async Task DeserializeFromJfif_ExtendedWithoutBaseSegment_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        string? xmp = AdobeXmpProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(() => xmp).IsNull();
    }

    [Test]
    public async Task DeserializeToJfif_MultipleDescriptionsWithExtendedXmp_XmpStitchedTogether()
    {
        byte[] extended1 = s_utf8WithoutBom.GetBytes(ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <crs:foo xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/" />
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """));
        byte[] extended2 = s_utf8WithoutBom.GetBytes(ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <photoshop:bar xmlns:photoshop="http://ns.adobe.com/photoshop/1.0/" />
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """));
        Guid md5DigestGuid1 = App1AdobeXmpExtended.CreateMd5DigestGuid(extended1);
        Guid md5DigestGuid2 = App1AdobeXmpExtended.CreateMd5DigestGuid(extended2);
        string expectedBase = ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description
                  xmlns:xmpNote="http://ns.adobe.com/xmp/note/"
                  xmpNote:HasExtendedXMP="{md5DigestGuid1.ToString("N").ToUpper()}"></rdf:Description>
                <rdf:Description
                  xmlns:xmpNote="http://ns.adobe.com/xmp/note/"
                  xmpNote:HasExtendedXMP="{md5DigestGuid2.ToString("N").ToUpper()}"></rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """);
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1AdobeXmp
        {
            XmpPacket = expectedBase
        });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended
        {
            XmpPortionUtf8 = extended1,
            FullMd5DigestGuid = md5DigestGuid1,
            FullXmpUtf8Length = (uint)extended1.Length
        });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended
        {
            XmpPortionUtf8 = extended2,
            FullMd5DigestGuid = md5DigestGuid2,
            FullXmpUtf8Length = (uint)extended2.Length
        });
        string? xmp = AdobeXmpProvider.DeserializeFromJfif(jfifMetadata);
        string expected = $"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <crs:foo xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/" />
                </rdf:Description>
                <rdf:Description>
                  <photoshop:bar xmlns:photoshop="http://ns.adobe.com/photoshop/1.0/" />
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """;
        await Assert.That(() => xmp).IsEqualTo(ParseAndWriteToString(expected));
    }

    [Test]
    public async Task DeserializeFromJfif_NoXmpSegments_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        string? xmp = AdobeXmpProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(() => xmp).IsNull();
    }

    [Test]
    public async Task SerializeToJfif_InfoExistingImage_XmpAppearsAfterExpectedSegments_()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new StartOfImage());
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App0Jfxx());
        jfifMetadata.Segments.Add(new App1Exif());
        jfifMetadata.Segments.Add(new StartOfScan());
        jfifMetadata.Segments.Add(new EndOfImage());
        AdobeXmpProvider.SerializeToJfif(c_basicXmp, jfifMetadata);
        await Assert
            .That(() => jfifMetadata.Segments.Select(s => s.GetType()).ToList())
            .IsEquivalentTo(new List<Type>
            {
                typeof(StartOfImage),
                typeof(App0Jfif),
                typeof(App0Jfxx),
                typeof(App1Exif),
                typeof(App1AdobeXmp),
                typeof(StartOfScan),
                typeof(EndOfImage),
            });
    }

    [Test]
    public async Task SerializeToJfif_MultipleDescriptionsWithExtendedXmp_SerializationIsCorrect()
    {
        JfifMetadata jfifMetadata = new();
        XmlDocument doc = new();
        doc.LoadXml(c_twoDescriptionsXmp);
        XmlElement description1 = doc.DocumentElement!
            .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
            .First()
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .First();
        XmlElement description2 = doc.DocumentElement!
            .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
            .First()
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .ElementAt(1);
        string xmp = doc.Render();
        AdobeXmpProvider.SerializeToJfif(xmp, jfifMetadata, 256);
        var baseSegment = jfifMetadata.FindFirst<App1AdobeXmp>()!;
        var extendedSegments = jfifMetadata.FindAll<App1AdobeXmpExtended>().ToList();
        await Assert.That(() => extendedSegments.Count).IsEqualTo(2);
        string expectedBase = ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description
                  xmlns:xmpNote="http://ns.adobe.com/xmp/note/"
                  xmpNote:HasExtendedXMP="{extendedSegments[0].FullMd5DigestGuid.ToString("N").ToUpper()}"></rdf:Description>
                <rdf:Description
                  xmlns:xmpNote="http://ns.adobe.com/xmp/note/"
                  xmpNote:HasExtendedXMP="{extendedSegments[1].FullMd5DigestGuid.ToString("N").ToUpper()}"></rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """);
        byte[] expectedExtended1 = s_utf8WithoutBom.GetBytes(ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <crs:foo xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/" />
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """));
        byte[] expectedExtended2 = s_utf8WithoutBom.GetBytes(ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <photoshop:bar xmlns:photoshop="http://ns.adobe.com/photoshop/1.0/" />
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """));
        await Assert.That(() => baseSegment.XmpPacket).IsEquivalentTo(expectedBase);
        await Assert.That(() => extendedSegments[0].XmpPortionUtf8).IsEquivalentTo(expectedExtended1);
        await Assert.That(() => extendedSegments[1].XmpPortionUtf8).IsEquivalentTo(expectedExtended2);
    }

    [Test]
    public async Task SerializeToJfif_MultipleExtendedXMPs_XmpAppearsAfterExpectedSegments()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new StartOfImage());
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App0Jfxx());
        jfifMetadata.Segments.Add(new App1Exif());
        jfifMetadata.Segments.Add(new StartOfScan());
        jfifMetadata.Segments.Add(new EndOfImage());
        XmlDocument doc = new();
        doc.LoadXml(c_basicXmp);
        XmlElement description = doc.DocumentElement!
            .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
            .First()
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .First();
        for (int i = 0; i < 3; i++)
        {
            description.SetAttribute("foo" + i, new string((char)('0' + i), ushort.MaxValue - 100));
        }
        string xmp = doc.Render();
        AdobeXmpProvider.SerializeToJfif(xmp, jfifMetadata, 1024 * 10);
        await Assert
            .That(() => jfifMetadata.Segments.Select(s => s.GetType()).ToList())
            .IsEquivalentTo(new List<Type>
            {
                typeof(StartOfImage),
                typeof(App0Jfif),
                typeof(App0Jfxx),
                typeof(App1Exif),
                typeof(App1AdobeXmp),
                typeof(App1AdobeXmpExtended), // crs:foo should go first, according to XMP spec.
                typeof(App1AdobeXmpExtended),
                typeof(App1AdobeXmpExtended),
                typeof(App1AdobeXmpExtended),
                typeof(StartOfScan),
                typeof(EndOfImage),
            });
    }

    [Test]
    public async Task SerializeToJfif_VerifyBaseAndExtendedXmp_SerializationIsCorrect()
    {
        JfifMetadata jfifMetadata = new();
        XmlDocument doc = new();
        doc.LoadXml(c_basicXmp);
        XmlElement description = doc.DocumentElement!
            .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
            .First()
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .First();
        string xmp = doc.Render();
        AdobeXmpProvider.SerializeToJfif(xmp, jfifMetadata, 256);
        var baseSegment = jfifMetadata.FindFirst<App1AdobeXmp>()!;
        var extendedSegment = jfifMetadata.FindFirst<App1AdobeXmpExtended>()!;
        string expectedBase = ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description
                  xmlns:xmpNote="http://ns.adobe.com/xmp/note/"
                  xmpNote:HasExtendedXMP="{extendedSegment.FullMd5DigestGuid.ToString("N").ToUpper()}"></rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """);
        byte[] expectedExtended = s_utf8WithoutBom.GetBytes(ParseAndWriteToString($"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="{AdobeXmpProvider.ToolkitName}">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description>
                  <crs:foo xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/"/>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """));
        await Assert.That(() => baseSegment.XmpPacket).IsEquivalentTo(expectedBase);
        await Assert.That(() => extendedSegment.XmpPortionUtf8).IsEquivalentTo(expectedExtended);
    }
}

/// <summary>
/// Contains the XMP namespaces used by the <see cref="Phaeyz.Jfif.AdobeXmpProvider"/>.
/// </summary>
file static class XmpNamespaces
{
    public static readonly XmlPrefixedNamespace AdobeMeta = ("x", "adobe:ns:meta/");
    public static readonly XmlPrefixedNamespace Rdf = ("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
    public static readonly XmlPrefixedNamespace Xmp = ("xpm", "http://ns.adobe.com/xap/1.0/");
    public static readonly XmlPrefixedNamespace XmpNote = ("xmpNote", "http://ns.adobe.com/xmp/note/");
    public static readonly XmlPrefixedNamespace CameraRaw = ("crs", "http://ns.adobe.com/camera-raw-settings/1.0/");
    public static readonly XmlPrefixedNamespace Images = ("xmpGImg", "http://ns.adobe.com/xap/1.0/g/img/");
    public static readonly XmlPrefixedNamespace Photoshop = ("photoshop", "http://ns.adobe.com/photoshop/1.0/");
}
