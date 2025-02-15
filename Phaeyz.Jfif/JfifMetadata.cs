using Phaeyz.Marshalling;

namespace Phaeyz.Jfif;

/// <summary>
/// Encapsulates a series of JFIF segments which make up a valid JFIF file.
/// </summary>
public class JfifMetadata
{
    /// <summary>
    /// The segments in the JFIF metadata. It is up to the caller to ensure that the correct segments
    /// exist and that they are in the correct order.
    /// </summary>
    public List<Segment> Segments { get; set; } = [];

    /// <summary>
    /// Finds all segments of the specified type in the JFIF metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to find.
    /// </typeparam>
    /// <returns>
    /// An enumerable of segments of the specified type.
    /// </returns>
    public IEnumerable<T> FindAll<T>() where T : Segment, new()
    {
        SegmentKey key = SegmentKey.Get<T>();
        return Segments.Where(s => s.Key == key).Cast<T>();
    }

    /// <summary>
    /// Finds the first segment of the specified type in the JFIF metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to find.
    /// </typeparam>
    /// <returns></returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A segment was found, but the class type did not match the expected type.
    /// </exception>
    public T? FindFirst<T>() where T : Segment, new() => FindFirst<T>(out _);

    /// <summary>
    /// Finds the first segment of the specified type in the JFIF metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to find.
    /// </typeparam>
    /// <param name="index">
    /// Receives the zero-based index of the segment which has been found, or <c>-1</c> if it was not found.
    /// </param>
    /// <returns></returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A segment was found, but the class type did not match the expected type.
    /// </exception>
    public T? FindFirst<T>(out int index) where T : Segment, new()
    {
        index = FindFirstIndex(SegmentKey.Get<T>());
        if (index == -1)
        {
            return null;
        }

        if (Segments[index] is not T)
        {
            throw new JfifException(
                $"The existing segment which matches the segment key is of type " +
                $"'{Segments[index].GetType()}' but '{typeof(T)}' is expected.");
        }

        return (T)Segments[index];
    }

    /// <summary>
    /// Finds the first segment with the specified key in the JFIF metadata.
    /// </summary>
    /// <param name="segmentKey">
    /// The key of the segment to find.
    /// </param>
    /// <returns>
    /// Returns the index of the first segment with the specified key, or <c>-1</c> if it was not found.
    /// </returns>
    public int FindFirstIndex(SegmentKey segmentKey)
    {
        for (int i = 0; i < Segments.Count; i++)
        {
            if (Segments[i].Key == segmentKey)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the first segment of the specified type in the JFIF metadata, or creates a new one if it does not exist.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to get or create.
    /// </typeparam>
    /// <param name="repositionExistingSegment">
    /// If <c>true</c> and the segment already exists, it will be repositioned according to the
    /// <paramref name="precedingSegmentKeys"/> array. If <c>false</c>, the existing segment will not be repositioned.
    /// A start-of-scan segment is always implied to be in this array.
    /// </param>
    /// <param name="precedingSegmentKeys">
    /// When creating or repositioning, ensures the segment is placed after the last segment
    /// matching any of the specified keys.
    /// </param>
    /// <returns>
    /// A segment which was found or created of the requested type.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A segment was found, but the class type did not match the expected type.
    /// </exception>
    public T GetFirstOrCreate<T>(
        bool repositionExistingSegment,
        params IEnumerable<SegmentKey> precedingSegmentKeys) where T : Segment, new() =>
            GetFirstOrCreate<T>(repositionExistingSegment, out _, out _, precedingSegmentKeys);

    /// <summary>
    /// Gets the first segment of the specified type in the JFIF metadata, or creates a new one if it does not exist.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to get or create.
    /// </typeparam>
    /// <param name="repositionExistingSegment">
    /// If <c>true</c> and the segment already exists, it will be repositioned according to the
    /// <paramref name="precedingSegmentKeys"/> array. If <c>false</c>, the existing segment will not be repositioned.
    /// A start-of-scan segment is always implied to be in this array.
    /// </param>
    /// <param name="created">
    /// Receives a boolean indicating whether or not the return segment was created during this
    /// call because it was not found.
    /// </param>
    /// <param name="index">
    /// Receives the index of the returned segment.
    /// </param>
    /// <param name="precedingSegmentKeys">
    /// When creating or repositioning, ensures the segment is placed after the last segment
    /// matching any of the specified keys. If no keys are specified, the segment will be placed at the beginning.
    /// </param>
    /// <returns>
    /// A segment which was found or created of the requested type.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// A segment was found, but the class type did not match the expected type.
    /// </exception>
    public T GetFirstOrCreate<T>(
        bool repositionExistingSegment,
        out bool created,
        out int index,
        params IEnumerable<SegmentKey> precedingSegmentKeys) where T : Segment, new()
    {
        // Find the existing segment (if it exists).
        T? segment = FindFirst<T>(out index);
        if (segment is null)
        {
            // If the segment didn't exist, so create one.
            segment = new();
            // Get the index after all segments which match the specified keys.
            index = GetIndexAfter(
                precedingSegmentKeys is null
                ? [(Marker.StartOfImage, null)]
                : precedingSegmentKeys.Concat([(Marker.StartOfImage, null)]));
            // Insert and return.
            Segments.Insert(index, segment);
            created = true;
            return segment;
        }

        created = false;

        // Reposition an existing segment if requested.
        if (repositionExistingSegment)
        {
            // Get the index after all segments which match the specified keys.
            int targetIndex = GetIndexAfter(
                precedingSegmentKeys is null
                ? [(Marker.StartOfImage, null)]
                : precedingSegmentKeys.Concat([(Marker.StartOfImage, null)]));

            // If the index is before the target index, it means it is before one of the preceding segments.
            if (index < targetIndex)
            {
                Segments.RemoveAt(index);
                index = targetIndex - 1;
                Segments.Insert(index, segment);
            }
        }

        return segment;
    }

    /// <summary>
    /// Gets the index after the last segment with any of the specified keys.
    /// </summary>
    /// <param name="segmentKeys">
    /// The keys of the segments to find.
    /// </param>
    /// <returns>
    /// Returns the index after the last segment with any of the specified keys, or <c>0</c> if none were found.
    /// </returns>
    public int GetIndexAfter(
        params IEnumerable<SegmentKey> segmentKeys)
    {
        if (segmentKeys is not null)
        {
            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                if (segmentKeys.Contains(Segments[i].Key))
                {
                    return i + 1;
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// Inserts a segment after the last segment with any of the specified keys.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment being inserted.
    /// </typeparam>
    /// <param name="segment">
    /// The segment to insert.
    /// </param>
    /// <param name="precedingSegmentKeys">
    /// Inserts the segment after the last segment matching any of the specified keys.
    /// If no keys are specified, the segment will be placed at the beginning.
    /// A start-of-scan segment is always implied to be in this array.
    /// </param>
    /// <returns>
    /// The index which the segment was inserted at.
    /// </returns>
    public int Insert<T>(
        T segment,
        params IEnumerable<SegmentKey> precedingSegmentKeys) where T : Segment, new()
    {
        int index = GetIndexAfter(precedingSegmentKeys is null
            ? [(Marker.StartOfImage, null)]
            : precedingSegmentKeys.Concat([(Marker.StartOfImage, null)]));
        Segments.Insert(index, segment);
        return index;
    }

    /// <summary>
    /// Reads all JFIF metadatas from the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to read the JFIF metadata from.
    /// </param>
    /// <param name="segmentDefinitions">
    /// Optionally segment definitions which maps segment keys to segment class types.
    /// Specify <c>null</c> to use the default segment definitions.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// Returns a list of <see cref="Phaeyz.Jfif.JfifMetadata"/> objects based on what has been discovered
    /// in the stream.
    /// </returns>
    /// <remarks>
    /// There may be multiple sequences of sequences of JFIF metadata in the stream, where each sequence is
    /// a start-of-image segment, followed by a series of segments, ending with an end-of-image segment. It is
    /// possible that an end-of-image segment to immediately follow a start-of-image segment to begin new JFIF metadata.
    /// </remarks>
    public static async ValueTask<List<JfifMetadata>> ReadAllFromStreamAsync(
        MarshalStream stream,
        SegmentDefinitions? segmentDefinitions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Loop for each JFIF metadata sequence.
        List<JfifMetadata> jfifMetadatas = [];
        while (true)
        {
            // Read the sequence.
            JfifMetadata? jfifMetadata = await ReadFromStreamAsync(
                stream,
                segmentDefinitions,
                cancellationToken).ConfigureAwait(false);

            // If a new sequence was not found, end the loop.
            if (jfifMetadata is null)
            {
                break;
            }
            jfifMetadatas.Add(jfifMetadata);
        }

        return jfifMetadatas;
    }

    /// <summary>
    /// Read a JFIF metadata from the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to read the JFIF metadata from.
    /// </param>
    /// <param name="segmentDefinitions">
    /// Optionally segment definitions which maps segment keys to segment class types.
    /// Specify <c>null</c> to use the default segment definitions.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// Returns a <see cref="Phaeyz.Jfif.JfifMetadata"/> if the stream contains valid JFIF metadata,
    /// otherwise <c>null</c> if a start-of-image segment could not be detected.
    /// </returns>
    /// <remarks>
    /// It is expected the first segment be start-of-image and last segment be end-of-image.
    /// </remarks>
    public static async ValueTask<JfifMetadata?> ReadFromStreamAsync(
        MarshalStream stream,
        SegmentDefinitions? segmentDefinitions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Probe the stream to see if the current position has a start-of-image segment.
        SegmentReader reader = new(stream, segmentDefinitions);
        if (!await reader.ProbeForStartOfImageAsync(cancellationToken))
        {
            return null;
        }

        // Read segments until an end-of-image segment is discovered.
        JfifMetadata jfifMetadata = new();
        for (Segment? segment = null; segment?.Key.Marker != Marker.EndOfImage; jfifMetadata.Segments.Add(segment!))
        {
            segment = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        return jfifMetadata;
    }

    /// <summary>
    /// Removes all segments of the specified type from the JFIF metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to remove.
    /// </typeparam>
    /// <returns>
    /// The number of matching segments removed.
    /// </returns>
    public int RemoveAll<T>() where T : Segment, new() => RemoveAll(SegmentKey.Get<T>());

    /// <summary>
    /// Removes all segments with the specified key from the JFIF metadata.
    /// </summary>
    /// <param name="segmentKey">
    /// The key of the segment to remove.
    /// </param>
    /// <returns>
    /// The number of matching segments removed.
    /// </returns>
    public int RemoveAll(SegmentKey segmentKey)
    {
        int countRemoved = 0;
        for (int i = 0; i < Segments.Count;)
        {
            if (Segments[i].Key == segmentKey)
            {
                Segments.RemoveAt(i);
                countRemoved++;
            }
            else
            {
                i++;
            }
        }
        return countRemoved;
    }

    /// <summary>
    /// Removes the first segment of the specified type from the JFIF metadata.
    /// </summary>
    /// <typeparam name="T">
    /// The type of segment to remove.
    /// </typeparam>
    /// <returns>
    /// Returns <c>true</c> if a segment was removed, otherwise <c>false</c>.
    /// </returns>
    public bool RemoveFirst<T>() where T : Segment, new() => RemoveFirst(SegmentKey.Get<T>());

    /// <summary>
    /// Removes the first segment with the specified key from the JFIF metadata.
    /// </summary>
    /// <param name="segmentKey">
    /// The key of the segment to remove.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if a segment was removed, otherwise <c>false</c>.
    /// </returns>
    public bool RemoveFirst(SegmentKey segmentKey)
    {
        int index = FindFirstIndex(segmentKey);
        if (index >= 0)
        {
            Segments.RemoveAt(index);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Writes all JFIF metadatas to the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to write the JFIF metadata to.
    /// </param>
    /// <param name="jfifMetadatas">
    /// An enumerable of <see cref="Phaeyz.Jfif.JfifMetadata"/> objects to write to the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when all JFIF metadata has been written to the stream.
    /// </returns>
    public static async ValueTask WriteAllToStreamAsync(
        MarshalStream stream,
        IEnumerable<JfifMetadata> jfifMetadatas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(jfifMetadatas);
        foreach (JfifMetadata jfifMetadata in jfifMetadatas)
        {
            ArgumentNullException.ThrowIfNull(jfifMetadatas, nameof(jfifMetadatas));
            await jfifMetadata.WriteToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes the JFIF metadata to the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to write the JFIF metadata to.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the JFIF metadata has been written to the stream.
    /// </returns>
    public async ValueTask WriteToStreamAsync(
        MarshalStream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        SegmentWriter writer = new(stream);
        foreach (Segment segment in Segments)
        {
            await writer.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
        }
    }
}
