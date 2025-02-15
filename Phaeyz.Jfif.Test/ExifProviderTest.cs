using Phaeyz.Jfif.Segments;

namespace Phaeyz.Jfif.Test;

internal class ExifProviderTest
{
    [Test]
    public async Task DeserializeFromJfif_ThreeExifSegments_ReturnsFullExif()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12, 0x34, 0x56] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x78] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x90, 0x12] });
        byte[]? exif = ExifProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(exif).IsNotNull();
        await Assert.That(exif!).IsEquivalentTo(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0x12 });
    }

    [Test]
    public async Task DeserializeFromJfif_EmptyExif_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [] });
        byte[]? exif = ExifProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(exif).IsNull();
    }

    [Test]
    public async Task DeserializeFromJfif_NoExifSegment_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        byte[]? exif = ExifProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(exif).IsNull();
    }

    [Test]
    public async Task DeserializeFromJfif_NoExifBuffer_ReturnsNull()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1Exif() { Exif = null! });
        byte[]? exif = ExifProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(exif).IsNull();
    }

    [Test]
    public async Task DeserializeFromJfif_OneByteExif_ReturnsOneByte()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12] });
        byte[]? exif = ExifProvider.DeserializeFromJfif(jfifMetadata);
        await Assert.That(exif).IsNotNull();
        await Assert.That(exif!).IsEquivalentTo(new byte[] { 0x12 });
    }

    [Test]
    public async Task SerializeToJfif_EmptyExif_ExistingExifSegmentsRemoved()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x34] });
        jfifMetadata.Segments.Add(new App0Jfxx());
        ExifProvider.SerializeToJfif([], jfifMetadata);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType()).ToArray()).IsEquivalentTo(
            new[] { typeof(App0Jfif), typeof(App0Jfxx) });
    }

    [Test]
    public async Task SerializeToJfif_ExifSegmentsCleanedUp_ExifSegmentsCleanedUp()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12, 0x34, 0x56] });
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x78] });
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x90, 0x12] });
        jfifMetadata.Segments.Add(new App0Jfif());
        byte[] newExifBuffer = [0x11, 0x22, 0x33, 0x44];
        ExifProvider.SerializeToJfif(newExifBuffer, jfifMetadata, 2);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(6);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType()).ToArray()).IsEquivalentTo(
            new[] { typeof(App0Jfif), typeof(App1Exif), typeof(App1Exif), typeof(App0Jfif), typeof(App0Jfif), typeof(App0Jfif) });
        await Assert.That(((App1Exif)jfifMetadata.Segments[1]).Exif).IsEquivalentTo(new byte[] { 0x11, 0x22 });
        await Assert.That(((App1Exif)jfifMetadata.Segments[2]).Exif).IsEquivalentTo(new byte[] { 0x33, 0x44 });
    }

    [Test]
    public async Task SerializeToJfif_NullExif_ExistingExifSegmentsRemoved()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x34] });
        jfifMetadata.Segments.Add(new App0Jfxx());
        ExifProvider.SerializeToJfif(null, jfifMetadata);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType()).ToArray()).IsEquivalentTo(
            new[] { typeof(App0Jfif), typeof(App0Jfxx) });
    }

    [Test]
    public async Task SerializeToJfif_OverwriteExistingAndTruncate_ExifCorrectlyPersisted()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x12, 0x34, 0x56] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x78] });
        jfifMetadata.Segments.Add(new App1Exif() { Exif = [0x90, 0x12] });
        byte[] newExifBuffer = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77];
        ExifProvider.SerializeToJfif(newExifBuffer, jfifMetadata, 4);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(2);
        await Assert.That(jfifMetadata.Segments[0] is App1Exif).IsTrue();
        await Assert.That(jfifMetadata.Segments[1] is App1Exif).IsTrue();
        await Assert.That(((App1Exif)jfifMetadata.Segments[0]).Exif).IsEquivalentTo(new byte[] { 0x11, 0x22, 0x33, 0x44 });
        await Assert.That(((App1Exif)jfifMetadata.Segments[1]).Exif).IsEquivalentTo(new byte[] { 0x55, 0x66, 0x77 });
    }

    [Test]
    public async Task SerializeToJfif_SegmentCountIncreased_ExifPersistedCorrectly()
    {
        JfifMetadata jfifMetadata = new();
        byte[] newExifBuffer = [0x11, 0x22, 0x33, 0x44];
        ExifProvider.SerializeToJfif(newExifBuffer, jfifMetadata, 2);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(2);
        await Assert.That(((App1Exif)jfifMetadata.Segments[0]).Exif).IsEquivalentTo(new byte[] { 0x11, 0x22 });
        await Assert.That(((App1Exif)jfifMetadata.Segments[1]).Exif).IsEquivalentTo(new byte[] { 0x33, 0x44 });
    }

    [Test]
    public async Task SerializeToJfif_HigherPrioritySegmentsInFront_ExifSegmentPositionedCorrectly()
    {
        JfifMetadata jfifMetadata = new();
        jfifMetadata.Segments.Add(new App0Jfif());
        jfifMetadata.Segments.Add(new App0Jfxx());
        jfifMetadata.Segments.Add(new StartOfScan());
        jfifMetadata.Segments.Add(new EndOfImage());
        byte[] newExifBuffer = [0x11, 0x22, 0x33, 0x44];
        ExifProvider.SerializeToJfif(newExifBuffer, jfifMetadata, 2);
        await Assert.That(jfifMetadata.Segments.Count).IsEqualTo(6);
        await Assert.That(jfifMetadata.Segments.Select(s => s.GetType()).ToArray()).IsEquivalentTo(
            new[] { typeof(App0Jfif), typeof(App0Jfxx), typeof(App1Exif), typeof(App1Exif), typeof(StartOfScan), typeof(EndOfImage) });
        await Assert.That(((App1Exif)jfifMetadata.Segments[2]).Exif).IsEquivalentTo(new byte[] { 0x11, 0x22 });
        await Assert.That(((App1Exif)jfifMetadata.Segments[3]).Exif).IsEquivalentTo(new byte[] { 0x33, 0x44 });
    }
}
