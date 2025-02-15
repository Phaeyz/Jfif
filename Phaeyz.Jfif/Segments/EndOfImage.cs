namespace Phaeyz.Jfif.Segments;

/// <summary>
/// A segment which marks the end of JFIF metadata.
/// </summary>
[Segment(Marker.EndOfImage, false)]
public class EndOfImage : SegmentWithoutLength { }
