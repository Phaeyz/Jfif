using Phaeyz.Marshalling;

namespace Phaeyz.Jfif.Segments;

/// <summary>
/// An App0 segment with "JFXX" as the identifier.
/// </summary>
[Segment(Marker.App0, "JFXX")]
public class App0Jfxx : Segment
{
    /// <summary>
    /// The format of the thumbnail.
    /// </summary>
    public ThumbnailFormat ThumbnailFormat { get; set; }

    /// <summary>
    /// The thumbnail image in JPEG format.
    /// </summary>
    /// <remarks>
    /// This must only be non-<c>null</c> if <see cref="Phaeyz.Jfif.Segments.App0Jfxx.ThumbnailFormat"/> is
    /// <see cref="Phaeyz.Jfif.Segments.ThumbnailFormat.Jpeg"/>.
    /// </remarks>
    public ThumbnailImageJpeg? ThumbnailJpeg { get; set; }

    /// <summary>
    /// The thumbnail image in 1-byte-per-pixel palettized format.
    /// </summary>
    /// <remarks>
    /// This must only be non-<c>null</c> if <see cref="Phaeyz.Jfif.Segments.App0Jfxx.ThumbnailFormat"/> is
    /// <see cref="Phaeyz.Jfif.Segments.ThumbnailFormat.OneBytePerPixelPalettized"/>.
    /// </remarks>
    public ThumbnailImage1bppPalettized? Thumbnail1bppPalettized { get; set; }

    /// <summary>
    /// The thumbnail image in 3-bytes-per-pixel RGB format.
    /// </summary>
    /// <remarks>
    /// This must only be non-<c>null</c> if <see cref="Phaeyz.Jfif.Segments.ThumbnailFormat.ThreeBytesPerPixelRgb"/> is
    /// <see cref="Phaeyz.Jfif.Segments.ThumbnailFormat.ThreeBytesPerPixelRgb"/>.
    /// </remarks>
    public ThumbnailImage3bppRgb? Thumbnail3bppRgb { get; set; }

    /// <summary>
    /// Reads the segment from the stream, hydrating the properties of the segment.
    /// </summary>
    /// <param name="stream">
    /// The stream to read from.
    /// </param>
    /// <param name="segmentLength">
    /// The remaining length of the segment on the stream. The caller may deduct from this object as it reads
    /// to prevent from reading too much.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the segment has been read.
    /// </returns>
    /// <remarks>
    /// It is expected that the method reads <paramref name="segmentLength"/> bytes from the stream.
    /// If less is read, it will lead to corruption. More is allowed depending on the circumstance, such as
    /// handling out-of-band data.
    /// </remarks>
    public override async ValueTask ReadFromStreamAsync(MarshalStream stream, SegmentLength segmentLength, CancellationToken cancellationToken)
    {
        segmentLength--;
        ThumbnailFormat = (ThumbnailFormat)await stream.ReadByteAsync(cancellationToken).ConfigureAwait(false);

        switch (ThumbnailFormat)
        {
            case ThumbnailFormat.Jpeg:
                ThumbnailJpeg = await ThumbnailImageJpeg.ReadFromStreamAsync(stream, segmentLength, cancellationToken).ConfigureAwait(false);
                break;
            case ThumbnailFormat.OneBytePerPixelPalettized:
                Thumbnail1bppPalettized = await ThumbnailImage1bppPalettized.ReadFromStreamAsync(stream, segmentLength, cancellationToken).ConfigureAwait(false);
                break;
            case ThumbnailFormat.ThreeBytesPerPixelRgb:
                Thumbnail3bppRgb = await ThumbnailImage3bppRgb.ReadFromStreamAsync(stream, segmentLength, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new JfifException($"Unrecognized thumbnail format \"{ThumbnailFormat}\".");
        };
    }

    /// <summary>
    /// Validates all properties of the segment such that the segment is ready for serialization,
    /// and computes the body length of the segment.
    /// </summary>
    /// <returns>
    /// The body length of the segment (total length of segment after the optional identifier, and excluding
    /// any out-of-band data).
    /// </returns>
    /// <remarks>
    /// This method is not called if the segment does not have length.
    /// </remarks>
    public override int ValidateAndComputeLength()
    {
        switch (ThumbnailFormat)
        {
            case ThumbnailFormat.Jpeg:
                if (ThumbnailJpeg is null)
                {
                    throw new JfifException("The Jpeg thumbnail is null.");
                }
                return ThumbnailJpeg.ValidateAndComputeLength();
            case ThumbnailFormat.OneBytePerPixelPalettized:
                if (Thumbnail1bppPalettized is null)
                {
                    throw new JfifException("The 1 byte-per-pixel thumbnail is null.");
                }
                return Thumbnail1bppPalettized.ValidateAndComputeLength();
            case ThumbnailFormat.ThreeBytesPerPixelRgb:
                if (Thumbnail3bppRgb is null)
                {
                    throw new JfifException("The 3 bytes-per-pixel RGB thumbnail is null.");
                }
                return Thumbnail3bppRgb.ValidateAndComputeLength();
            default:
                throw new JfifException($"Unrecognized thumbnail format \"{ThumbnailFormat}\".");
        }
    }

    /// <summary>
    /// Writes the segment to the stream.
    /// </summary>
    /// <param name="stream">
    /// The stream to write to.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token which may be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task which is completed when the segment has been written.
    /// </returns>
    /// <remarks>
    /// This method is not called if the segment does not have length.
    /// </remarks>
    public override async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
    {
        await stream.WriteUInt8Async((byte)ThumbnailFormat, cancellationToken).ConfigureAwait(false);

        switch (ThumbnailFormat)
        {
            case ThumbnailFormat.Jpeg:
                await ThumbnailJpeg!.WriteToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                break;
            case ThumbnailFormat.OneBytePerPixelPalettized:
                await Thumbnail1bppPalettized!.WriteToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                break;
            case ThumbnailFormat.ThreeBytesPerPixelRgb:
                await Thumbnail3bppRgb!.WriteToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// The thumbnail image in JPEG format.
    /// </summary>
    public class ThumbnailImageJpeg
    {
        /// <summary>
        /// The JPEG data for the thumbnail.
        /// </summary>
        public byte[] ThumbnailJpegData { get; set; } = [];

        /// <summary>
        /// Reads the thumbnail image from the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="length">
        /// The segment remaining length.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which yields the thumbnail image.
        /// </returns>
        /// <exception cref="Phaeyz.Jfif.JfifException">
        /// Could not parse the thumbnail image from the stream.
        /// </exception>
        internal static async ValueTask<ThumbnailImageJpeg> ReadFromStreamAsync(MarshalStream stream, SegmentLength length, CancellationToken cancellationToken)
        {
            ThumbnailImageJpeg thumbnail = new();

            length -= SegmentReader.MarkerAndIndicatorLength;
            Marker thumbnailMarker = await SegmentReader.ReadMarkerAsync(stream, cancellationToken).ConfigureAwait(false);
            if (thumbnailMarker != Marker.StartOfImage)
            {
                throw new JfifException($"Expected SOI marker to start the thumbnail.");
            }
            static int ScanBuffer(ReadOnlyMemory<byte> scanBuffer)
            {
                ReadOnlySpan<byte> scanSpan = scanBuffer.Span;
                for (int i = 0; i < scanSpan.Length - 1; i++)
                {
                    if (scanSpan[i] == SegmentReader.MarkerIndicator && scanSpan[i + 1] == (byte)Marker.EndOfImage)
                    {
                        return i;
                    }
                }
                return scanSpan.Length - 1;
            }
            MemoryStream thumbnailData = new();
            MarshalStreamScanResult result = await stream.ScanAsync(
                thumbnailData,
                SegmentReader.MarkerAndIndicatorLength,
                length.Remaining - SegmentReader.MarkerAndIndicatorLength,
                ScanBuffer,
                cancellationToken).ConfigureAwait(false);
            if (!result.IsPositiveMatch)
            {
                throw new JfifException("Could not find the EOI marker after the thumbnail.");
            }
            thumbnail.ThumbnailJpegData = thumbnailData.ToArray();
            length -= result.BytesRead;

            // Skip past the EOI and any other padding
            await stream.SkipAsync(length.Remaining, cancellationToken).ConfigureAwait(false);
            return thumbnail;
        }

        /// <summary>
        /// Validates all properties of the segment such that the segment is ready for serialization,
        /// and computes the body length of the segment.
        /// </summary>
        /// <returns>
        /// The body length of the segment (total length of segment after the optional identifier, and excluding
        /// any out-of-band data).
        /// </returns>
        /// <remarks>
        /// This method is not called if the segment does not have length.
        /// </remarks>
        internal int ValidateAndComputeLength()
        {
            const int fixedLengthPart = 4 + 1; // Add 1 for ThumbnailFormat in the parent
            return checked(fixedLengthPart + ThumbnailJpegData.Length);
        }

        /// <summary>
        /// Writes the thumbnail image to the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which is completed when the thumbnail image has been written.
        /// </returns>
        internal async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
        {
            await stream.WriteUInt8Async(SegmentReader.MarkerIndicator, cancellationToken).ConfigureAwait(false);
            await stream.WriteUInt8Async((byte)Marker.StartOfImage, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(ThumbnailJpegData, cancellationToken).ConfigureAwait(false);
            await stream.WriteUInt8Async(SegmentReader.MarkerIndicator, cancellationToken).ConfigureAwait(false);
            await stream.WriteUInt8Async((byte)Marker.EndOfImage, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The thumbnail image in 1-byte-per-pixel palettized format.
    /// </summary>
    public class ThumbnailImage1bppPalettized
    {
        /// <summary>
        /// The fixed byte length of the thumbnail palette.
        /// </summary>
        public const int ThumbnailPaletteLength = 768;

        /// <summary>
        /// The palette for the thumbnail.
        /// </summary>
        public byte[] ThumbnailPalette { get; set; } = [];

        /// <summary>
        /// The horizontal pixel width of the thumbnail.
        /// </summary>
        public byte ThumbnailHorizontalPixelWidth { get; set; }

        /// <summary>
        /// The vertical pixel height of the thumbnail.
        /// </summary>
        public byte ThumbnailVerticalPixelHeight { get; set; }

        /// <summary>
        /// The palettized data for the thumbnail.
        /// </summary>
        public byte[] ThumbnailPalettizedData { get; set; } = [];

        /// <summary>
        /// Reads the thumbnail image from the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="length">
        /// The segment remaining length.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which yields the thumbnail image.
        /// </returns>
        internal static async ValueTask<ThumbnailImage1bppPalettized> ReadFromStreamAsync(MarshalStream stream, SegmentLength length, CancellationToken cancellationToken)
        {
            ThumbnailImage1bppPalettized thumbnail = new();

            length -= ThumbnailPaletteLength + 2;
            thumbnail.ThumbnailHorizontalPixelWidth = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
            thumbnail.ThumbnailVerticalPixelHeight = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
            thumbnail.ThumbnailPalette = new byte[ThumbnailPaletteLength];
            await stream.ReadExactlyAsync(thumbnail.ThumbnailPalette, 0, ThumbnailPaletteLength, cancellationToken).ConfigureAwait(false);

            int thumbnailLength = thumbnail.ThumbnailHorizontalPixelWidth * thumbnail.ThumbnailVerticalPixelHeight;
            length -= thumbnailLength;
            thumbnail.ThumbnailPalettizedData = new byte[thumbnailLength];
            await stream.ReadExactlyAsync(thumbnail.ThumbnailPalettizedData, 0, thumbnailLength, cancellationToken).ConfigureAwait(false);

            // Skip past any other padding
            await stream.SkipAsync(length.Remaining, cancellationToken).ConfigureAwait(false);
            return thumbnail;
        }

        /// <summary>
        /// Validates all properties of the segment such that the segment is ready for serialization,
        /// and computes the body length of the segment.
        /// </summary>
        /// <returns>
        /// The body length of the segment (total length of segment after the optional identifier, and excluding
        /// any out-of-band data).
        /// </returns>
        /// <remarks>
        /// This method is not called if the segment does not have length.
        /// </remarks>
        internal int ValidateAndComputeLength()
        {
            int thumbnailLength = ThumbnailHorizontalPixelWidth * ThumbnailVerticalPixelHeight;

            if (ThumbnailPaletteLength != ThumbnailPalette.Length)
            {
                throw new JfifException("The length of the thumbnail palette data does not match the expected length.");
            }

            if (thumbnailLength != ThumbnailPalettizedData.Length)
            {
                throw new JfifException("The length of the thumbnail palettized data does not match the expected length.");
            }

            const int fixedLengthPart = 2 + 1; // Add 1 for ThumbnailFormat in the parent
            return checked(fixedLengthPart + ThumbnailPalette.Length + ThumbnailPalettizedData.Length);
        }

        /// <summary>
        /// Writes the thumbnail image to the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which is completed when the thumbnail image has been written.
        /// </returns>
        internal async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
        {
            await stream.WriteUInt8Async(ThumbnailHorizontalPixelWidth, cancellationToken).ConfigureAwait(false);
            await stream.WriteUInt8Async(ThumbnailVerticalPixelHeight, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(ThumbnailPalette, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(ThumbnailPalettizedData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The thumbnail image in 3-bytes-per-pixel RGB format.
    /// </summary>
    public class ThumbnailImage3bppRgb
    {
        /// <summary>
        /// The horizontal pixel width of the thumbnail.
        /// </summary>
        public byte ThumbnailHorizontalPixelWidth { get; set; }

        /// <summary>
        /// The vertical pixel height of the thumbnail.
        /// </summary>
        public byte ThumbnailVerticalPixelHeight { get; set; }

        /// <summary>
        /// The RGB data for the thumbnail.
        /// </summary>
        /// <remarks>
        /// Each pixel is 3-bytes (RGB) and the data is stored in row-major order.
        /// </remarks>
        public byte[] ThumbnailRgbData { get; set; } = [];

        /// <summary>
        /// Reads the thumbnail image from the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="length">
        /// The segment remaining length.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which yields the thumbnail image.
        /// </returns>
        internal static async ValueTask<ThumbnailImage3bppRgb> ReadFromStreamAsync(MarshalStream stream, SegmentLength length, CancellationToken cancellationToken)
        {
            ThumbnailImage3bppRgb thumbnail = new();

            length -= 2;
            thumbnail.ThumbnailHorizontalPixelWidth = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);
            thumbnail.ThumbnailVerticalPixelHeight = await stream.ReadUInt8Async(cancellationToken).ConfigureAwait(false);

            ushort thumbnailLength = (ushort)(3 * thumbnail.ThumbnailHorizontalPixelWidth * thumbnail.ThumbnailVerticalPixelHeight);
            length -= thumbnailLength;
            thumbnail.ThumbnailRgbData = new byte[thumbnailLength];
            await stream.ReadExactlyAsync(thumbnail.ThumbnailRgbData, 0, thumbnailLength, cancellationToken).ConfigureAwait(false);

            // Skip past any other padding
            await stream.SkipAsync(length.Remaining, cancellationToken).ConfigureAwait(false);
            return thumbnail;
        }

        /// <summary>
        /// Validates all properties of the segment such that the segment is ready for serialization,
        /// and computes the body length of the segment.
        /// </summary>
        /// <returns>
        /// The body length of the segment (total length of segment after the optional identifier, and excluding
        /// any out-of-band data).
        /// </returns>
        /// <remarks>
        /// This method is not called if the segment does not have length.
        /// </remarks>
        internal int ValidateAndComputeLength()
        {
            int thumbnailLength = 3 * ThumbnailHorizontalPixelWidth * ThumbnailVerticalPixelHeight;

            if (thumbnailLength != ThumbnailRgbData.Length)
            {
                throw new JfifException("The length of the thumbnail RGB data does not match the expected length.");
            }

            const int fixedLengthPart = 2 + 1; // Add 1 for ThumbnailFormat in the parent
            return checked(fixedLengthPart + ThumbnailRgbData.Length);
        }

        /// <summary>
        /// Writes the thumbnail image to the stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token which may be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task which is completed when the thumbnail image has been written.
        /// </returns>
        internal async ValueTask WriteToStreamAsync(MarshalStream stream, CancellationToken cancellationToken)
        {
            await stream.WriteUInt8Async(ThumbnailHorizontalPixelWidth, cancellationToken).ConfigureAwait(false);
            await stream.WriteUInt8Async(ThumbnailVerticalPixelHeight, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(ThumbnailRgbData, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// The format of the thumbnail.
/// </summary>
public enum ThumbnailFormat
{
    /// <summary>
    /// The thumbnail image is in JPEG format.
    /// </summary>
    Jpeg                            = 16,

    /// <summary>
    /// The thumbnail image is in 1-byte-per-pixel palettized format.
    /// </summary>
    OneBytePerPixelPalettized       = 17,

    /// <summary>
    /// The thumbnail image is in 3-bytes-per-pixel RGB format.
    /// </summary>
    ThreeBytesPerPixelRgb           = 19,
}