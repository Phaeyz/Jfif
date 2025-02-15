using Phaeyz.Jfif.Segments;
using Phaeyz.Marshalling;
using TUnit.Assertions.AssertConditions.Throws;

namespace Phaeyz.Jfif.Test;

internal class JfifMetadataTest
{
    [Test]
    public async Task FindAll_MultipleMatching_MatchesYielded()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        List<App1AdobeXmpExtended> segments = jfifMetadata.FindAll<App1AdobeXmpExtended>().ToList();
        await Assert.That(segments.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FindAll_NoMatching_EmptyEnumerable()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        List<App0Jfxx> segments = jfifMetadata.FindAll<App0Jfxx>().ToList();
        await Assert.That(segments).IsEmpty();
    }

    [Test]
    public async Task FindFirst_MultipleMatching_FirstReturned()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 0 });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 1 });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 2 });
        App1AdobeXmpExtended? segment = jfifMetadata.FindFirst<App1AdobeXmpExtended>();
        await Assert.That(segment?.FullXmpUtf8Length).IsEqualTo((uint?)0);
    }

    [Test]
    public async Task FindFirst_MultipleMatching_FirstReturnedWithCorrectIndex()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 0 });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 1 });
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended { FullXmpUtf8Length = 2 });
        App1AdobeXmpExtended? segment = jfifMetadata.FindFirst<App1AdobeXmpExtended>(out int index);
        await Assert.That(segment?.FullXmpUtf8Length).IsEqualTo((uint?)0);
        await Assert.That(index).IsEqualTo(2);
    }

    [Test]
    public async Task FindFirst_NoMatching_NullReturned()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        App0Jfxx? segment = jfifMetadata.FindFirst<App0Jfxx>();
        await Assert.That(segment).IsNull();
    }

    [Test]
    public async Task FindFirstIndex_MultipleMatching_FirstCorrectIndexReturned()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        int index = jfifMetadata.FindFirstIndex(SegmentKey.Get<App1AdobeXmpExtended>());
        await Assert.That(index).IsEqualTo(2);
    }

    [Test]
    public async Task FindFirstIndex_NoMatching_NegativeOneReturned()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        int index = jfifMetadata.FindFirstIndex(SegmentKey.Get<App1AdobeXmpExtended>());
        await Assert.That(index).IsEqualTo(-1);
    }

    [Test]
    public async Task GetFirstOrCreate_CreateSegment_SegmentCreated()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        App1AdobeXmpExtended segment = jfifMetadata.GetFirstOrCreate<App1AdobeXmpExtended>(
            false,
            out bool created,
            out int index,
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>(),
            SegmentKey.Get<App1AdobeXmp>());
        await Assert.That(created).IsEqualTo(true);
        await Assert.That(index).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments.IndexOf(segment)).IsEqualTo(2);
    }

    [Test]
    public async Task GetFirstOrCreate_ExistingSegmentInProperLocation_SegmentNotMoved()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        App1AdobeXmpExtended segment1 = new();
        jfifMetadata.Segments.Add(segment1);
        jfifMetadata.Segments.Add(new EndOfImage());
        App1AdobeXmpExtended segment2 = jfifMetadata.GetFirstOrCreate<App1AdobeXmpExtended>(
            false,
            out bool created,
            out int index,
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>(),
            SegmentKey.Get<App1AdobeXmp>());
        await Assert.That(created).IsEqualTo(false);
        await Assert.That(index).IsEqualTo(2);

        await Assert.That(jfifMetadata.Segments.IndexOf(segment1)).IsEqualTo(2);
        await Assert.That(ReferenceEquals(segment1, segment2)).IsTrue();
    }

    [Test]
    public async Task GetFirstOrCreate_ExistingSegmentInWrongLocation_SegmentNotMoved()
    {
        JfifMetadata jfifMetadata = new();
        App1AdobeXmpExtended segment1 = new();
        jfifMetadata.Segments.Add(segment1);
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        App1AdobeXmpExtended segment2 = jfifMetadata.GetFirstOrCreate<App1AdobeXmpExtended>(
            false,
            out bool created,
            out int index,
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>(),
            SegmentKey.Get<App1AdobeXmp>());
        await Assert.That(created).IsEqualTo(false);
        await Assert.That(index).IsEqualTo(0);
        await Assert.That(jfifMetadata.Segments.IndexOf(segment1)).IsEqualTo(0);
        await Assert.That(ReferenceEquals(segment1, segment2)).IsTrue();
    }

    [Test]
    public async Task GetFirstOrCreate_ExistingSegmentInWrongLocation_SegmentMoved()
    {
        JfifMetadata jfifMetadata = new();
        App1AdobeXmpExtended segment1 = new();
        jfifMetadata.Segments.Add(segment1);
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        App1AdobeXmpExtended segment2 = jfifMetadata.GetFirstOrCreate<App1AdobeXmpExtended>(
            true,
            out bool created,
            out int index,
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>(),
            SegmentKey.Get<App1AdobeXmp>());
        await Assert.That(created).IsEqualTo(false);
        await Assert.That(index).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments.IndexOf(segment1)).IsEqualTo(2);
        await Assert.That(ReferenceEquals(segment1, segment2)).IsTrue();
    }

    [Test]
    public async Task GetIndexAfter_EmptyList_ReturnsZero()
    {
        JfifMetadata jfifMetadata = new();
        int index = jfifMetadata.GetIndexAfter(
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>());
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task GetIndexAfter_EndOfList_ReturnsListLength()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.GetIndexAfter(
            SegmentKey.Get<EndOfImage>());
        await Assert.That(index).IsEqualTo(3);
    }

    [Test]
    public async Task GetIndexAfter_Middle_ReturnsOne()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.GetIndexAfter(
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>());
        await Assert.That(index).IsEqualTo(1);
    }

    [Test]
    public async Task GetIndexAfter_NotFound_ReturnsZero()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.GetIndexAfter(
            SegmentKey.Get<App1AdobeXmpExtended>());
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task Insert_EmptyList_ReturnsZero()
    {
        JfifMetadata jfifMetadata = new();
        int index = jfifMetadata.Insert(
            new StartOfScan(),
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>());
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task Insert_EndOfList_ReturnsListLength()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.Insert(
            new StartOfScan(),
            SegmentKey.Get<EndOfImage>());
        await Assert.That(index).IsEqualTo(3);
    }

    [Test]
    public async Task Insert_Middle_ReturnsOne()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.Insert(
            new App1AdobeXmpExtended(),
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>());
        await Assert.That(index).IsEqualTo(1);
    }

    [Test]
    public async Task Insert_NotFound_ReturnsZero()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int index = jfifMetadata.Insert(
            new StartOfScan(),
            SegmentKey.Get<App1AdobeXmpExtended>());
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task ReadAllFromStreamAsync_MultipleMetadatasMultipleSegments_DeserializedCorrectly()
    {
        byte[] imageBytes =
        [
            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x07, 0x08, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI

            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x01, 0x02, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI
        ];

        MarshalStream marshalStream = new(imageBytes);
        List<JfifMetadata> jfifMetadataList = await JfifMetadata.ReadAllFromStreamAsync(marshalStream);

        await Assert.That(jfifMetadataList.Count).IsEqualTo(2);
        await Assert.That(jfifMetadataList[0].Segments.Select(s => s.GetType()).ToList()).IsEquivalentTo([
            typeof(StartOfImage),
            typeof(App0Jfif),
            typeof(EndOfImage)
            ]);
        await Assert.That(jfifMetadataList[1].Segments.Select(s => s.GetType()).ToList()).IsEquivalentTo([
            typeof(StartOfImage),
            typeof(App0Jfif),
            typeof(EndOfImage)
            ]);

        App0Jfif jfif1 = (App0Jfif)jfifMetadataList[0].Segments[1];
        App0Jfif jfif2 = (App0Jfif)jfifMetadataList[1].Segments[1];
        await Assert.That(jfif1.VersionMajor).IsEqualTo((byte)7);
        await Assert.That(jfif1.VersionMinor).IsEqualTo((byte)8);
        await Assert.That(jfif2.VersionMajor).IsEqualTo((byte)1);
        await Assert.That(jfif2.VersionMinor).IsEqualTo((byte)2);
        foreach (App0Jfif jfif in new List<App0Jfif>() { jfif1, jfif2 })
        {
            await Assert.That(jfif.PixelDensityUnits).IsEqualTo(PixelDensityUnits.PixelsPerInch);
            await Assert.That(jfif.HorizontalPixelDensity).IsEqualTo((ushort)0x1234);
            await Assert.That(jfif.VerticalPixelDensity).IsEqualTo((ushort)0x5678);
            await Assert.That(jfif.ThumbnailHorizontalPixelWidth).IsEqualTo((byte)1);
            await Assert.That(jfif.ThumbnailVerticalPixelHeight).IsEqualTo((byte)2);
            await Assert.That(jfif.ThumbnailRgbData).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 });
        }
    }

    [Test]
    public async Task ReadFromStreamAsync_BasicImageSegments_DeserializesCorrectly()
    {
        byte[] imageBytes =
        [
            0xFF, 0xD8, // SOI
            0xFF, 0xD9, // EOI
        ];
        MarshalStream marshalStream = new(imageBytes);
        JfifMetadata? jfifMetadata = await JfifMetadata.ReadFromStreamAsync(marshalStream);
        await Assert.That(jfifMetadata).IsNotNull();
        await Assert.That(jfifMetadata!.Segments.Count).IsEqualTo(2);
        await Assert.That(jfifMetadata!.Segments[0].GetType()).IsEqualTo(typeof(StartOfImage));
        await Assert.That(jfifMetadata!.Segments[1].GetType()).IsEqualTo(typeof(EndOfImage));
    }

    [Test]
    public async Task ReadFromStreamAsync_EmptyStream_DeserializedCorrectly()
    {
        byte[] imageBytes = [];
        MarshalStream marshalStream = new(imageBytes);
        JfifMetadata? jfifMetadata = await JfifMetadata.ReadFromStreamAsync(marshalStream);
        await Assert.That(jfifMetadata).IsNull();
    }

    [Test]
    public async Task ReadFromStreamAsync_MultipleMetadatasMultipleSegments_StreamReadingStopsAfterFirst()
    {
        byte[] imageBytes =
        [
            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x07, 0x08, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI

            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x01, 0x02, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI
        ];

        MarshalStream marshalStream = new(imageBytes);
        JfifMetadata? jfifMetadata = await JfifMetadata.ReadFromStreamAsync(marshalStream);

        await Assert.That(marshalStream.Position).IsEqualTo(imageBytes.Length / 2);
        await Assert.That(jfifMetadata).IsNotNull();
        await Assert.That(jfifMetadata!.Segments.Select(s => s.GetType()).ToList()).IsEquivalentTo([
            typeof(StartOfImage),
            typeof(App0Jfif),
            typeof(EndOfImage)
            ]);
        App0Jfif jfif = (App0Jfif)jfifMetadata.Segments[1];
        await Assert.That(jfif.VersionMajor).IsEqualTo((byte)7);
        await Assert.That(jfif.VersionMinor).IsEqualTo((byte)8);
        await Assert.That(jfif.PixelDensityUnits).IsEqualTo(PixelDensityUnits.PixelsPerInch);
        await Assert.That(jfif.HorizontalPixelDensity).IsEqualTo((ushort)0x1234);
        await Assert.That(jfif.VerticalPixelDensity).IsEqualTo((ushort)0x5678);
        await Assert.That(jfif.ThumbnailHorizontalPixelWidth).IsEqualTo((byte)1);
        await Assert.That(jfif.ThumbnailVerticalPixelHeight).IsEqualTo((byte)2);
        await Assert.That(jfif.ThumbnailRgbData).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 });
    }

    [Test]
    public async Task ReadFromStreamAsync_NoEndOfImageMarker_ThrowsEndOfStreamException()
    {
        byte[] imageBytes =
        [
            0xFF, 0xD8, // SOI
        ];
        MarshalStream marshalStream = new(imageBytes);
        await Assert.That(async () => await JfifMetadata.ReadFromStreamAsync(marshalStream)).Throws<EndOfStreamException>();
    }

    [Test]
    public async Task RemoveAll_NoneFound_ReturnsZero()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        int count = jfifMetadata.RemoveAll<App1AdobeXmpExtended>();
        await Assert.That(count).IsEqualTo(0);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage)]);
    }

    [Test]
    public async Task RemoveAll_OneFound_ReturnsOne()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new EndOfImage());
        int count = jfifMetadata.RemoveAll<App1AdobeXmpExtended>();
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage)]);
    }

    [Test]
    public async Task RemoveAll_TwoFound_ReturnsTwo()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new EndOfImage());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        int count = jfifMetadata.RemoveAll<App1AdobeXmpExtended>();
        await Assert.That(count).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage)]);
    }

    [Test]
    public async Task RemoveFirst_NoneFound_ReturnsFalse()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new EndOfImage());
        bool result = jfifMetadata.RemoveFirst<App1AdobeXmpExtended>();
        await Assert.That(result).IsEqualTo(false);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage)]);
    }

    [Test]
    public async Task RemoveFirst_OneFound_ReturnsTrue()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new EndOfImage());
        bool result = jfifMetadata.RemoveFirst<App1AdobeXmpExtended>();
        await Assert.That(result).IsEqualTo(true);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage)]);
    }

    [Test]
    public async Task RemoveFirst_TwoFound_ReturnsTrue()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1AdobeXmp());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        jfifMetadata.Segments.Add(new EndOfImage());
        jfifMetadata.Segments.Add(new App1AdobeXmpExtended());
        bool result = jfifMetadata.RemoveFirst<App1AdobeXmpExtended>();
        await Assert.That(result).IsEqualTo(true);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType())).IsEquivalentTo([
            typeof(App0Jfif),
            typeof(App1AdobeXmp),
            typeof(EndOfImage),
            typeof(App1AdobeXmpExtended)]);
    }

    [Test]
    public async Task WriteAllToStreamAsync_MultipleMetadataWithMultipleSegments_WritesCorrectOutput()
    {
        JfifMetadata jfifMetadata1 = new();
        jfifMetadata1.Segments.Add(new StartOfImage());
        jfifMetadata1.Segments.Add(new App0Jfif
        {
            VersionMajor = 7,
            VersionMinor = 8,
            PixelDensityUnits = PixelDensityUnits.PixelsPerInch,
            HorizontalPixelDensity = 0x1234,
            VerticalPixelDensity = 0x5678,
            ThumbnailHorizontalPixelWidth = 1,
            ThumbnailVerticalPixelHeight = 2,
            ThumbnailRgbData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
        });
        jfifMetadata1.Segments.Add(new EndOfImage());

        JfifMetadata jfifMetadata2 = new();
        jfifMetadata2.Segments.Add(new StartOfImage());
        jfifMetadata2.Segments.Add(new App0Jfif
        {
            VersionMajor = 1,
            VersionMinor = 2,
            PixelDensityUnits = PixelDensityUnits.PixelsPerInch,
            HorizontalPixelDensity = 0x1234,
            VerticalPixelDensity = 0x5678,
            ThumbnailHorizontalPixelWidth = 1,
            ThumbnailVerticalPixelHeight = 2,
            ThumbnailRgbData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
        });
        jfifMetadata2.Segments.Add(new EndOfImage());
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await JfifMetadata.WriteAllToStreamAsync(stream, [jfifMetadata1, jfifMetadata2]);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes =
        [
            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x07, 0x08, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI

            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x01, 0x02, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI
        ];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteAllToStreamAsync_NoMetadatas_WritesCorrectOutput()
    {
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await JfifMetadata.WriteAllToStreamAsync(stream, []);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes = [];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteAllToStreamAsync_SingleMetadataAndSegment_WritesCorrectOutput()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new StartOfImage());
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await JfifMetadata.WriteAllToStreamAsync(stream, [jfifMetadata]);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes =
        [
            0xFF, 0xD8, // SOI
        ];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteAllToStreamAsync_TwoEmptyMetadata_WritesCorrectOutput()
    {
        JfifMetadata jfifMetadata1 = new();
        JfifMetadata jfifMetadata2 = new();
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await JfifMetadata.WriteAllToStreamAsync(stream, [jfifMetadata1, jfifMetadata2]);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes = [];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteToStreamAsync_EndToEnd_WritesCorrectOutput()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new StartOfImage());
        jfifMetadata.Segments.Add(new App0Jfif
        {
            VersionMajor = 7,
            VersionMinor = 8,
            PixelDensityUnits = PixelDensityUnits.PixelsPerInch,
            HorizontalPixelDensity = 0x1234,
            VerticalPixelDensity = 0x5678,
            ThumbnailHorizontalPixelWidth = 1,
            ThumbnailVerticalPixelHeight = 2,
            ThumbnailRgbData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
        });
        jfifMetadata.Segments.Add(new EndOfImage());
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await jfifMetadata.WriteToStreamAsync(stream);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes =
        [
            0xFF, 0xD8, // SOI

            0xFF, 0xE0, // App0
            0x00, 0x16, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0" (Identifier)
            0x07, 0x08, // VersionMajor + VersionMinor
            0x01,       // PixelDensityUnits (PixelsPerInch)
            0x12, 0x34, // HorizontalPixelDensity
            0x56, 0x78, // VerticalPixelDensity
            0x01,       // ThumbnailHorizontalPixelWidth
            0x02,       // ThumbnailVerticalPixelHeight
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, // ThumbnailRgbData

            0xFF, 0xD9, // EOI
        ];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteToStreamAsync_NoSegments_WritesNothing()
    {
        JfifMetadata jfifMetadata = new();
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await jfifMetadata.WriteToStreamAsync(stream);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes = [];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }

    [Test]
    public async Task WriteToStreamAsync_SingleSegment_WritesCorrectOutput()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new EndOfImage());
        using MemoryStream memoryStream = new();
        using MarshalStream stream = new(memoryStream, false);
        await jfifMetadata.WriteToStreamAsync(stream);
        stream.Flush();
        byte[] bytes = memoryStream.ToArray();
        byte[] expectedBytes =
        [
            0xFF, 0xD9, // EOI
        ];
        await Assert.That(bytes).IsEquivalentTo(expectedBytes);
    }
}