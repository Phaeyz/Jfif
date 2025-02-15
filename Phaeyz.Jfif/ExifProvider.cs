using Phaeyz.Jfif.Segments;

namespace Phaeyz.Jfif;

/// <summary>
/// Provides access to the EXIF within JFIF metadata.
/// </summary>
public static class ExifProvider
{
    /// <summary>
    /// Deserializes EXIF out of JFIF metadata.
    /// </summary>
    /// <param name="jfifMetadata">
    /// The JFIF which may or may not contain EXIF segments.
    /// </param>
    /// <returns>
    /// Returns the EXIF deserialized from the JFIF metadata.
    /// </returns>
    public static byte[]? DeserializeFromJfif(JfifMetadata jfifMetadata)
    {
        ArgumentNullException.ThrowIfNull(jfifMetadata);

        // Get all the App1 EXIF segments with content.
        List<App1Exif> app1ExifList = jfifMetadata.FindAll<App1Exif>().Where(o => o.Exif?.Length > 0).ToList();

        // If nothing found, return null.
        if (app1ExifList.Count == 0)
        {
            return null;
        }

        // If there is only one (usually the case) just return the embedded buffer.
        if (app1ExifList.Count == 1)
        {
            return app1ExifList[0].Exif;
        }

        // This is an unusual circumstance where EXIF was likely too big and distributed over multiple App1 EXIF segments.
        // First get the size of the complete EXIF buffer.
        byte[] exif = new byte[app1ExifList.Sum(o => o.Exif.Length)];

        // Copy each EXIF segment into the complete buffer.
        int bytesCopied = 0;
        foreach (App1Exif app1Exif in app1ExifList)
        {
            app1Exif.Exif.CopyTo(exif, bytesCopied);
            bytesCopied += app1Exif.Exif.Length;
        }
        return exif;
    }

    /// <summary>
    /// Serializes EXIF into the JFIF metadata.
    /// </summary>
    /// <param name="exif">
    /// The EXIF to serialize into the JFIF metadata. If this is <c>null</c> or empty, all EXIF segments are removed.
    /// </param>
    /// <param name="jfifMetadata">
    /// The JFIF metadata to receive the Adobe XMP metadata.
    /// </param>
    /// <param name="maxBytesPerSegment">
    /// The maximum number of bytes to use per segment. If <c>null</c>, the <c>App1Exif.MaxExifBytes</c> is used.
    /// The default is <c>null</c> and this parameter is typically used for testing.
    /// </param>
    public static void SerializeToJfif(byte[]? exif, JfifMetadata jfifMetadata, int? maxBytesPerSegment = null)
    {
        ArgumentNullException.ThrowIfNull(jfifMetadata);

        // Validate maxBytesPerSegment
        if (maxBytesPerSegment is null)
        {
            maxBytesPerSegment = App1Exif.MaxExifBytes;
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                maxBytesPerSegment.Value,
                App1Exif.MaxExifBytes,
                nameof(maxBytesPerSegment));
        }

        // If there is EXIF, just remove any segments corresponding to it.
        if (exif is null || exif.Length == 0)
        {
            jfifMetadata.RemoveAll<App1Exif>();
            return;
        }

        // Write EXIF buffers until all EXIF is written.
        int index = 0;
        for (int totalBytesWritten = 0; totalBytesWritten < exif.Length;)
        {
            App1Exif exifSegment;
            // If this is the first segment in the EXIF series, find the existing
            // segment, or create a new one at the appropriate location.
            if (totalBytesWritten == 0)
            {
                exifSegment = jfifMetadata.GetFirstOrCreate<App1Exif>(
                    false,
                    out _,
                    out index,
                    SegmentKey.Get<App0Jfif>(),
                    SegmentKey.Get<App0Jfxx>());
            }
            // If this is a subsequent one, see if the next location is an EXIF segment already.
            else if (++index < jfifMetadata.Segments.Count &&
                jfifMetadata.Segments[index] is App1Exif existingExifSegment)
            {
                exifSegment = existingExifSegment;
            }
            // Otherwise, create and insert a new one.
            else
            {
                exifSegment = new App1Exif();
                jfifMetadata.Segments.Insert(index, exifSegment);
            }

            // Compute the number of bytes to write this segment.
            int bytesToWrite = Math.Min(exif.Length - totalBytesWritten, maxBytesPerSegment.Value);

            // If the buffer is not the right size, resize it.
            if ((exifSegment.Exif?.Length ?? 0) != bytesToWrite)
            {
                exifSegment.Exif = new byte[bytesToWrite];
            }

            // Copy the bytes into the buffer.
            Buffer.BlockCopy(exif, totalBytesWritten, exifSegment.Exif!, 0, bytesToWrite);
            totalBytesWritten += bytesToWrite;
        }

        // Remove any remaining EXIF segments.
        for (int i = jfifMetadata.Segments.Count - 1; i >= index + 1; i--)
        {
            if (jfifMetadata.Segments[i] is App1Exif)
            {
                jfifMetadata.Segments.RemoveAt(i);
            }
        }
    }
}
