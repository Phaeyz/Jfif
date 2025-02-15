namespace Phaeyz.Jfif.Segments;

/// <summary>
/// A segment which marks the beginning of the JFIF metadata.
/// </summary>
[Segment(Marker.StartOfImage, false)]
public class StartOfImage : SegmentWithoutLength { }
