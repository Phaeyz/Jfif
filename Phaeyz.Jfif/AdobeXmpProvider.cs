using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Phaeyz.Jfif.Segments;
using Phaeyz.Marshalling;
using Phaeyz.Xml;

namespace Phaeyz.Jfif;

/// <summary>
/// Serializes and deserializes XMP from JFIF metadata.
/// </summary>
public static partial class AdobeXmpProvider
{
    // Used for stripping xpacket wrappers.
    [GeneratedRegex(@"\A\s*<\?xpacket(\s*((?<begin>begin)|([^\?\<\>""'\s=]+))(?=[\s=\?])\s*(=\s*((""[^""]*"")|('[^']*')|(?<nq>(?![""'])((?!\?>)[^\s])+)))?)*\s*\?\>\s*(?<xmp>.*?(?<!\s))\s*(?(begin)(<\?xpacket(\s*((?<end>end)|([^\?\<\>""'\s=]+))(?=[\s=\?])\s*(=\s*((""[^""]*"")|('[^']*')|((?![""'])((?!\?>)[^\s])+)))?)*\s*\?\>.*)|\A)(?(end)\z|\A)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex XmpPacketWrapperRegex();

    // The toolkit name (includes version).
    private static readonly Lazy<string> s_toolkitName = new Lazy<string>(() =>
    {
        string version = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
            Assembly.GetExecutingAssembly(),
            typeof(AssemblyFileVersionAttribute), false)!).Version;

        return $"Phaeyz XMP Toolkit ({version})";
    });

    /// <summary>
    /// The toolkit name serialized with XMP.
    /// </summary>
    public static readonly string ToolkitName = s_toolkitName.Value;

    /// <summary>
    /// Deserializes XMP out of JFIF metadata.
    /// </summary>
    /// <param name="jfifMetadata">
    /// The JFIF which may or may not contain Adobe XMP segments.
    /// </param>
    /// <param name="throwOnInvalidSamples">
    /// If <c>true</c>, when XMP segments are being deserialized an exception will be thrown if invalid data
    /// is encountered. If <c>false</c> the XMP data will be ignored and no exception thrown.
    /// </param>
    /// <returns>
    /// Returns the XMP deserialized from the JFIF metadata.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// The offsets in extended XMP do not align to the actual data, there is too little or too many UTF8 bytes
    /// for the extended data, or the UTF8 bytes failed verification against the MD5 digest guid.
    /// </exception>
    public static string? DeserializeFromJfif(JfifMetadata jfifMetadata, bool throwOnInvalidSamples = true)
    {
        ArgumentNullException.ThrowIfNull(jfifMetadata);

        Dictionary<string, string> extendedXmp = [];
        string? baseXmp = ExtractXmpPortionsFromJfif(jfifMetadata, extendedXmp, throwOnInvalidSamples);

        if (baseXmp is null || extendedXmp.Count == 0)
        {
            return baseXmp;
        }

        return MergeExtendedXmpIntoBaseXmp(baseXmp, extendedXmp);
    }

    /// <summary>
    /// Extracts the base XMP and XMP portions from the JFIF metadata and stores them in the
    /// extended XMP dictionary.
    /// </summary>
    /// <param name="jfifMetadata">
    /// The JFIF metadata to extract XMP data from.
    /// </param>
    /// <param name="extendedXmp">
    /// A dictionary which receives the extended XMP data. The key is the MD5 digest of the extended XMP.
    /// </param>
    /// <param name="throwOnInvalidSamples">
    /// If <c>true</c>, when XMP segments are being deserialized an exception will be thrown if invalid data
    /// is encountered. If <c>false</c> the XMP data will be ignored and no exception thrown.
    /// </param>
    /// <returns>
    /// Returns the base XMP data extracted from the JFIF metadata, or <c>null</c> if there is no XMP data.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// The offsets in extended XMP do not align to the actual data, there is too little or too many UTF8 bytes
    /// for the extended data, or the UTF8 bytes failed verification against the MD5 digest guid.
    /// </exception>
    private static string? ExtractXmpPortionsFromJfif(
        JfifMetadata jfifMetadata,
        Dictionary<string, string> extendedXmp,
        bool throwOnInvalidSamples = true)
    {
        // First get the base XMP.
        string? baseXmp = StripXmpPacketWrapper(jfifMetadata.FindFirst<App1AdobeXmp>()?.XmpPacket);
        if (baseXmp is null)
        {
            return null;
        }

        // For the extended XMP, we need to group them by their MD5 digest. Within each group, order the segments
        // by their starting offset to make it easier to merge them into a contiguous byte array.
        foreach (IGrouping<Guid, App1AdobeXmpExtended> xmpPortions in jfifMetadata
            .Segments
            .OfType<App1AdobeXmpExtended>()
            .OrderBy(o => o.StartingOffsetInFullXmpUtf8)
            .GroupBy(o => o.FullMd5DigestGuid))
        {
            // First merge all portions into a contiguous byte array
            byte[]? extendedXmpUtf8 = null;
            int nextStartingOffset = 0;
            bool isInvalid = false;

            // Iterate each portion in the current group.
            foreach (var xmpPortion in xmpPortions)
            {
                // Ensure a buffer is allocated for this this extended XMP
                extendedXmpUtf8 ??= new byte[xmpPortion.FullXmpUtf8Length];

                // Ensure the next starting offset aligns to what we are expecting. Also make sure the portion
                // size is not bigger than the target buffer.
                if (xmpPortion.StartingOffsetInFullXmpUtf8 != nextStartingOffset ||
                    checked(xmpPortion.StartingOffsetInFullXmpUtf8 + xmpPortion.XmpPortionUtf8.Length) > extendedXmpUtf8.Length)
                {
                    if (throwOnInvalidSamples)
                    {
                        throw new JfifException("Extended XMP portions are not properly aligned.");
                    }
                    isInvalid = true;
                    break;
                }

                // Copy the portion bytes into the buffer.
                Buffer.BlockCopy(
                    xmpPortion.XmpPortionUtf8,
                    0,
                    extendedXmpUtf8,
                    nextStartingOffset,
                    xmpPortion.XmpPortionUtf8.Length);

                // Track the next expected position in the target buffer.
                nextStartingOffset += xmpPortion.XmpPortionUtf8.Length;
            }

            // After copying everything over, the next starting offset should be the end of the buffer.
            if (isInvalid || nextStartingOffset != extendedXmpUtf8!.Length)
            {
                if (throwOnInvalidSamples)
                {
                    throw new JfifException("Extended XMP portions do not match the expected full size.");
                }
                continue;
            }

            // Verify the MD5 digest
            Guid md5DigestGuid = App1AdobeXmpExtended.CreateMd5DigestGuid(extendedXmpUtf8);
            if (xmpPortions.Key != md5DigestGuid)
            {
                if (throwOnInvalidSamples)
                {
                    throw new JfifException("Extended XMP portions do not match the expected MD5 digest.");
                }
                continue;
            }

            // Store the extended XMP. The spec says it must not have the xpacket wrapper,
            // but for resilience lets strip it just in case.
            string extendedXmpString = StripXmpPacketWrapper(Encoding.UTF8.GetString(extendedXmpUtf8))!;
            extendedXmp.Add(md5DigestGuid.ToString("N").ToUpper(), extendedXmpString);
        }

        return baseXmp;
    }

    /// <summary>
    /// Merges the extended XMP data into the base XMP data.
    /// </summary>
    /// <param name="baseXmp">
    /// The base XMP.
    /// </param>
    /// <param name="extendedXmp">
    /// A dictionary of extended XMP portions.
    /// </param>
    /// <returns>
    /// Returns an XMP string with the extended XMP merged into the base XMP.
    /// </returns>
    private static string MergeExtendedXmpIntoBaseXmp(string baseXmp, Dictionary<string, string> extendedXmp)
    {
        if (string.IsNullOrEmpty(baseXmp) || extendedXmp.Count == 0)
        {
            return baseXmp;
        }

        // Parse the base XMP.
        XmlDocument baseDoc = new();
        baseDoc.LoadXml(baseXmp);

        // Iterate all <rdf:Description> elements in the base XMP.
        foreach (var description in baseDoc
            .DocumentElement?
            .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
            .FirstOrDefault()?
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description") ?? [])
        {
            // Look for the xmpNote:HasExtendedXMP attribute.
            XmlAttribute? hasExtendedXmpAttr = description.SelectAttributes(XmpNamespaces.XmpNote.Uri, "HasExtendedXMP").FirstOrDefault();
            if (hasExtendedXmpAttr is null)
            {
                continue;
            }

            // Get the MD5 digest GUID from the attribute value.
            string md5DigestGuid = hasExtendedXmpAttr.Value;
            if (extendedXmp.TryGetValue(md5DigestGuid, out string? extendedXmpValue))
            {
                // Parse the extended XMP.
                XmlDocument extendedDoc = new();
                extendedDoc.LoadXml(extendedXmpValue);

                // Find the <rdf:Description> in the extended XMP.
                XmlElement? extendedDescription = extendedDoc
                    .DocumentElement?
                    .SelectChildren(XmpNamespaces.Rdf.Uri, "RDF")
                    .FirstOrDefault()?
                    .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
                    .FirstOrDefault();
                if (extendedDescription is null)
                {
                    continue;
                }

                // Move all attributes from the extended description to the base description.
                foreach (var attribute in extendedDescription.SelectAttributes().Where(attr => !attr.IsNamespaceDeclaration()).ToList())
                {
                    XmlNodeMover.MoveAttribute(attribute, description);
                }

                // Move all child elements from the extended description to the base description.
                foreach (var element in extendedDescription.SelectChildren().ToList())
                {
                    XmlNodeMover.MoveElement(element, description);
                }

                // Remove the xmpNote:HasExtendedXMP attribute after merging.
                description.Attributes.Remove(hasExtendedXmpAttr);
            }
        }

        // Optimize namespaces. This will remove things like the namespace declarations for HasExtendedXMP.
        baseDoc.OptimizeNamespaces();

        // Return the regenerated XMP as a string.
        return XmlElementSerializer.WriteToString(baseDoc.DocumentElement!);
    }

    /// <summary>
    /// Serializes XMP into the JFIF metadata.
    /// </summary>
    /// <param name="xmp">
    /// The XMP to serialize into the JFIF metadata. If this is <c>null</c> or empty, any Adobe XMP segments are removed.
    /// </param>
    /// <param name="jfifMetadata">
    /// The JFIF metadata to receive the Adobe XMP metadata.
    /// </param>
    /// <param name="maxBaseXmpUtf8Bytes">
    /// The maximum number of bytes to use for the base XMP. If <c>null</c>, the <c>App1AdobeXmp.MaxXmpUtf8Bytes</c> is used.
    /// The default is <c>null</c> and this parameter is typically used for testing.
    /// </param>
    public static void SerializeToJfif(string? xmp, JfifMetadata jfifMetadata, int? maxBaseXmpUtf8Bytes = null)
    {
        ArgumentNullException.ThrowIfNull(jfifMetadata);

        // Will recreate these if necessary.
        jfifMetadata.RemoveAll<App1AdobeXmpExtended>();

        // If there is no XMP packet, just remove any segments corresponding to it.
        if (string.IsNullOrEmpty(xmp))
        {
            jfifMetadata.RemoveAll<App1AdobeXmp>();
            return;
        }

        string baseXmp = PrepareXmpForJfif(xmp, out Dictionary<Guid, byte[]> extendedXmp, maxBaseXmpUtf8Bytes);

        // Ensure there is an Adobe XMP segment.
        App1AdobeXmp xmpSegment = jfifMetadata.GetFirstOrCreate<App1AdobeXmp>(
            false,
            out _,
            out int index,
            SegmentKey.Get<App0Jfif>(),
            SegmentKey.Get<App0Jfxx>(),
            SegmentKey.Get<App1Exif>());

        xmpSegment.XmpPacket = baseXmp;

        foreach (KeyValuePair<Guid, byte[]> entry in extendedXmp)
        {
            // Split the extended XMP into multiple portions of maximum size, and create segments for each portion.
            for (int bytesStored = 0; bytesStored < entry.Value.Length;)
            {
                int bytesThisSegment = Math.Min(entry.Value.Length - bytesStored, App1AdobeXmpExtended.MaxXmpUtf8BytesPerPortion);

                App1AdobeXmpExtended extendedSegment = new()
                {
                    FullMd5DigestGuid = entry.Key,
                    FullXmpUtf8Length = (uint)entry.Value.Length,
                    StartingOffsetInFullXmpUtf8 = (uint)bytesStored,
                    XmpPortionUtf8 = entry.Value.AsSpan(bytesStored, bytesThisSegment).ToArray(),
                };

                jfifMetadata.Segments.Insert(++index, extendedSegment);
                bytesStored += bytesThisSegment;
            }
        }
    }

    /// <summary>
    /// Prepares the XMP for serialization into JFIF metadata by creating UTF-8 chunks of extended XMP.
    /// </summary>
    /// <param name="xmp">
    /// The input XMP to prepare for serialization.
    /// </param>
    /// <param name="extendedXmp">
    /// On output, receives a dictionary of extended XMP chunks. The key is the MD5 digest of the extended XMP.
    /// </param>
    /// <param name="maxBaseXmpUtf8Bytes">
    /// The maximum number of bytes to use for the base XMP. If <c>null</c>, the <c>App1AdobeXmp.MaxXmpUtf8Bytes</c> is used.
    /// The default is <c>null</c> and this parameter is typically used for testing.
    /// </param>
    /// <returns>
    /// The new base XMP which excludes the extended XMP.
    /// </returns>
    private static string PrepareXmpForJfif(string xmp, out Dictionary<Guid, byte[]> extendedXmp, int? maxBaseXmpUtf8Bytes = null)
    {
        ExtendedXmpBuilder extendedXmpBuilder = new(xmp, maxBaseXmpUtf8Bytes);
        if (extendedXmpBuilder.IsBaseDocumentSmallEnough())
        {
            return extendedXmpBuilder.Persist(out extendedXmp);
        }

        while (
            // Move xmp:Thumbnails
            !extendedXmpBuilder.MoveElements(XmpNamespaces.Images.Uri, "Thumbnails") &&
            // Move Camera Raw properties
            !extendedXmpBuilder.MoveElements(XmpNamespaces.CameraRaw.Uri) &&
            // Move photoshop:History
            !extendedXmpBuilder.MoveElements(XmpNamespaces.Photoshop.Uri, "History") &&
            // Move other top-level properties incrementally by size
            !extendedXmpBuilder.MoveLargestAttributesOrElements()) break;

        return extendedXmpBuilder.Persist(out extendedXmp);
    }

    /// <summary>
    /// Removes the xpacket wrapper from the an XMP block if it exists.
    /// </summary>
    /// <param name="xmp">
    /// Any XMP block, regardless if it contains an xpacket wrapper.
    /// </param>
    /// <returns>
    /// Returns the trimmed XMP block excluding an xpacket wrapper and excess whitespace.
    /// If the XMP block is empty, null is returned.
    /// </returns>
    /// <remarks>
    /// The XMP wrapper is required by most handlers, and Adobe's XMP Toolkit includes it in the response.
    /// https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart1.pdf
    /// Some handlers (i.e. PNG) are not very smart and if you write a block smaller than the previously persisted block,
    /// it overwrites the previous block without updating length information. Then when you go read the block again,
    /// the Adobe XMP Toolkit will return data which have remnants of the previous block after the xpacket header.
    /// This method is smart enough to yield only the parts within the xpacket wrapper.
    /// </remarks>
    private static string? StripXmpPacketWrapper(string? xmp)
    {
        if (string.IsNullOrWhiteSpace(xmp))
        {
            return null;
        }

        Match match = XmpPacketWrapperRegex().Match(xmp);
        xmp = match.Success ? match.Groups["xmp"].Value : xmp.Trim();
        return xmp.Length == 0 ? null : xmp;
    }
}

/// <summary>
/// Builds extended XMP chunks from a base XMP document.
/// </summary>
file class ExtendedXmpBuilder
{
    private readonly XmlDocument _baseDocument = new();
    private readonly XmlElement? _rdf;
    private readonly Dictionary<int, ExtendedXmp> _extendedXmpTable = [];
    private readonly int _maxBaseXmpUtf8Bytes;

    /// <summary>
    /// Creates a new instance of the <see cref="Phaeyz.Jfif.ExtendedXmpBuilder"/> class.
    /// </summary>
    /// <param name="xmp">
    /// The input XMP which extended XMP will be built from.
    /// </param>
    /// <param name="maxBaseXmpUtf8Bytes">
    /// The maximum number of bytes to use for the base XMP. If <c>null</c>, the <c>App1AdobeXmp.MaxXmpUtf8Bytes</c> is used.
    /// The default is <c>null</c> and this parameter is typically used for testing.
    /// </param>
    public ExtendedXmpBuilder(string xmp, int? maxBaseXmpUtf8Bytes = null)
    {
        _maxBaseXmpUtf8Bytes = maxBaseXmpUtf8Bytes ?? App1AdobeXmp.MaxXmpUtf8Bytes;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            _maxBaseXmpUtf8Bytes,
            App1AdobeXmp.MaxXmpUtf8Bytes,
            nameof(maxBaseXmpUtf8Bytes));
        _baseDocument.LoadXml(xmp);
        _rdf = ValidateAndCleanupBaseDocument();
    }

    /// <summary>
    /// Gets the base description from the input XMP at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the description to get from the RDF.
    /// </param>
    /// <returns>
    /// The base description from the input XMP at the specified index.
    /// </returns>
    private XmlElement GetBaseDescription(int index) =>
        _rdf!
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .ElementAt(index);

    /// <summary>
    /// Gets a list of all base descriptions in the input XMP's RDF.
    /// </summary>
    /// <returns>
    /// A list of all base descriptions in the input XMP's RDF.
    /// </returns>
    private List<(XmlElement description, int index)> GetBaseDescriptions(XmlElement? rdf = null) =>
        (rdf ?? _rdf!)
            .SelectChildren(XmpNamespaces.Rdf.Uri, "Description")
            .Select((XmlElement description, int index) => (description, index))
            .ToList();

    /// <summary>
    /// Gets the extended XMP for the input XMP's description at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the description to in the RDF.
    /// </param>
    /// <returns>
    /// The extended XMP for the input XMP's description at the specified index.
    /// </returns>
    private ExtendedXmp GetExtendedXmp(int index)
    {
        if (!_extendedXmpTable.TryGetValue(index, out var extendedXmp))
        {
            var description = GetBaseDescription(index);
            description.EnsureNamespaceDeclared(true, XmpNamespaces.XmpNote);
            description.AddAttribute(XmpNamespaces.XmpNote, "HasExtendedXMP", Guid.Empty.ToString("N").ToUpper());
            extendedXmp = new ExtendedXmp();
            _extendedXmpTable[index] = extendedXmp;
        }

        return extendedXmp;
    }

    /// <summary>
    /// Computes whether or not the base XMP document is small enough to be serialized.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if the base XMP document is small enough to be serialized; <c>false</c> otherwise.
    /// </returns>
    public bool IsBaseDocumentSmallEnough()
    {
        return XmlElementSerializer.GetUtf8ByteCount(_baseDocument.DocumentElement!) <= _maxBaseXmpUtf8Bytes;
    }

    /// <summary>
    /// Moves elements in each input RDF description containing the specified namespace (and optionally elementName) to an extended XMP.
    /// </summary>
    /// <param name="namespaceUri">
    /// The namespace of the elements to move to the extended XMP.
    /// </param>
    /// <param name="elementName">
    /// Optionally, the name of the elements to move to the extended XMP. If <c>null</c>, all elements in the namespace will be moved.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the base XMP document is small enough to be serialized; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// As an optimization, this method does not initially check if the base XMP document is small enough to be
    /// serialized before moving data. The caller should only call this if it knows is needs to.
    /// </remarks>
    public bool MoveElements(string namespaceUri, string? elementName = null)
    {
        // This action happens over all description elements.
        foreach ((XmlElement description, int index) in GetBaseDescriptions())
        {
            // Find elements to move.
            List<XmlElement> elements = description.SelectChildren(namespaceUri, elementName).ToList();
            if (elements.Count > 0)
            {
                // Move the elements.
                foreach (XmlElement element in elements)
                {
                    XmlNodeMover.MoveElement(element, GetExtendedXmp(index).Description);
                }

                // Optimizing namespaces can reduce the size of the document.
                _baseDocument.OptimizeNamespaces();

                // Now test if moving this element was sufficient.
                if (IsBaseDocumentSmallEnough())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Moves the largest attributes or elements from each input RDF description to an extended XMP.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if the base XMP document is small enough to be serialized; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// As an optimization, this method does not initially check if the base XMP document is small enough to be
    /// serialized before moving data. The caller should only call this if it knows is needs to.
    /// </remarks>
    public bool MoveLargestAttributesOrElements()
    {
        // This action happens over all description elements.
        foreach ((XmlElement description, int index) in GetBaseDescriptions())
        {
            // Get and measure the size of all attributes.
            var attributesWithSize = description
                .SelectAttributes()
                .Where(attr => !attr.IsNamespaceDeclaration())
                .Where(attr => attr.NamespaceURI != XmpNamespaces.XmpNote.Uri || attr.LocalName != "HasExtendedXMP")
                .Select(attr => new
            {
                Element = (XmlElement?)null,
                Attribute = (XmlAttribute?)attr,
                Size = XmlElementSerializer.GetUtf8ByteCount(attr),
            });

            // Get and measure the size of all elements.
            var elementsWithSize = description
                .SelectChildren()
                .OfType<XmlElement>()
                .Select(element => new
            {
                Element = (XmlElement?)element,
                Attribute = (XmlAttribute?)null,
                Size = XmlElementSerializer.GetUtf8ByteCount(element),
            });

            // Combine the list of attributes and elements, and order them by size so we can take the biggest ones first.
            var dataWithSize = attributesWithSize.Concat(elementsWithSize).OrderByDescending(o => o.Size).ToList();

            // Iterate over all attributes and elements, processing the biggest ones first.
            foreach (var data in dataWithSize)
            {
                // Move the attribute or element.
                if (data.Attribute is not null)
                {
                    XmlNodeMover.MoveAttribute(data.Attribute, GetExtendedXmp(index).Description);
                }
                else if (data.Element is not null)
                {
                    XmlNodeMover.MoveElement(data.Element, GetExtendedXmp(index).Description);
                }

                // Optimizing namespaces can reduce the size of the document.
                _baseDocument.OptimizeNamespaces();

                // Now test if moving this attribute or element was sufficient.
                if (IsBaseDocumentSmallEnough())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Processes the base XMP into a string, and extended XMP into a dictionary of UTF-8 byte array
    /// chunks which may be partitioned and written to JFIF metadata.
    /// </summary>
    /// <param name="extendedXmp">
    /// On output, receives a dictionary of UTF-8 byte array extended XMP chunks.
    /// </param>
    /// <returns>
    /// The new base XMP as a string.
    /// </returns>
    public string Persist(out Dictionary<Guid, byte[]> extendedXmp)
    {
        extendedXmp = [];

        // Iterate all descriptions in the base XMP, to potentially create a UTF-8 byte array
        // extended XMP chunk.
        foreach ((XmlElement description, int index) in GetBaseDescriptions())
        {
            if (_extendedXmpTable.TryGetValue(index, out ExtendedXmp? extendedXmpElement))
            {
                // Convert this extended XMP to a string and then a UTF-8 byte array chunk.
                byte[] extendedXmpBytes =
                    XmlElementSerializer.WriteToUtf8Stream(extendedXmpElement.Document.DocumentElement!).ToArray();

                // Create a new MD5 digest for this full extended XMP chunk.
                Guid md5DigestGuid = App1AdobeXmpExtended.CreateMd5DigestGuid(extendedXmpBytes);
                extendedXmp[md5DigestGuid] = extendedXmpBytes;

                // Update the base XMP to reference this extended XMP.
                description.EnsureNamespaceDeclared(true, XmpNamespaces.XmpNote);
                description.SetAttribute("HasExtendedXMP", XmpNamespaces.XmpNote.Uri, md5DigestGuid.ToString("N").ToUpper());
            }
        }

        // Finally return the base XMP as a string.
        return XmlElementSerializer.WriteToString(_baseDocument.DocumentElement!);
    }

    /// <summary>
    /// Validates and cleans up the base XMP document so that it may be processed.
    /// </summary>
    /// <returns>
    /// The discovered RDF element.
    /// </returns>
    /// <exception cref="Phaeyz.Jfif.JfifException">
    /// The root element is not a valid &lt;x:xmpmeta&gt; element, or there is not only a single &lt;rdf:RDF&gt; element under the root.
    /// </exception>
    private XmlElement ValidateAndCleanupBaseDocument()
    {
        // First ensure the root elements exist.
        if (_baseDocument.DocumentElement is null ||
            _baseDocument.DocumentElement.LocalName != "xmpmeta" ||
            _baseDocument.DocumentElement.NamespaceURI != XmpNamespaces.AdobeMeta.Uri)
        {
            throw new JfifException(
                "The XMP document is not valid XMP: The root element is not a valid <x:xmpmeta> element.");
        }

        List<XmlElement> rootChildren = _baseDocument.DocumentElement.SelectChildren().ToList();
        if (rootChildren.Count != 1 ||
            rootChildren[0].LocalName != "RDF" ||
            rootChildren[0].NamespaceURI != XmpNamespaces.Rdf.Uri)
        {
            throw new JfifException(
                "The XMP document is not valid XMP: There must be a single <rdf:RDF> element under the root.");
        }

        // Ensure common prefixes to the document exist.
        _baseDocument.DocumentElement.EnsureNamespaceDeclared(true, XmpNamespaces.AdobeMeta);
        rootChildren[0].EnsureNamespaceDeclared(true, XmpNamespaces.Rdf);

        // Remove any existing toolkit attributes and add a new one. SetAttribute() normally
        // overrides existing attributes, but we want to be super clean and make sure there
        // are not multiple of these attributes set.
        foreach (XmlAttribute attr in _baseDocument.DocumentElement.SelectAttributes(XmpNamespaces.AdobeMeta.Uri, "xmptk").ToList())
        {
            _baseDocument.DocumentElement.Attributes.Remove(attr);
        }

        _baseDocument.DocumentElement.AddAttribute(XmpNamespaces.AdobeMeta, "xmptk", AdobeXmpProvider.ToolkitName);

        // Remove any existing "HasExtendedXMP" attributes. They may have been left behind by a bad deserializer, for example.
        // These will be recreated as necessary.
        foreach ((XmlElement description, _) in GetBaseDescriptions(rootChildren[0]))
        {
            foreach (XmlAttribute attr in description.SelectAttributes(XmpNamespaces.XmpNote.Uri, "HasExtendedXMP").ToList())
            {
                description.Attributes.Remove(attr);
            }
        }

        // Now optimize the namespace usage.
        _baseDocument.OptimizeNamespaces();

        // Return the <rdf:RDF> element.
        return rootChildren[0];
    }
}

/// <summary>
/// Internally used to track extended XMP chunks during serialization.
/// </summary>
file class ExtendedXmp
{
    /// <summary>
    /// Creates a new instance of the <see cref="Phaeyz.Jfif.ExtendedXmp"/> class.
    /// </summary>
    public ExtendedXmp()
    {
        // Create the extended XMP document.
        Document = new XmlDocument();
        var elXmpMeta = Document.AddElement(XmpNamespaces.AdobeMeta, "xmpmeta");
        elXmpMeta.AddNamespaceDeclaration(XmpNamespaces.AdobeMeta);
        elXmpMeta.AddAttribute(XmpNamespaces.AdobeMeta, "xmptk", AdobeXmpProvider.ToolkitName);
        var elRdf = elXmpMeta.AddChildElement(XmpNamespaces.Rdf, "RDF");
        elRdf.AddNamespaceDeclaration(XmpNamespaces.Rdf);
        Description = elRdf.AddChildElement(XmpNamespaces.Rdf, "Description");
    }

    /// <summary>
    /// The description element for this extended XMP.
    /// </summary>
    public XmlElement Description { get; private set; }

    /// <summary>
    /// The extended XMP document.
    /// </summary>
    public XmlDocument Document { get; private set; }
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

/// <summary>
/// Internally used to serialize and deserialize XML elements for usage in XMP.
/// </summary>
file static class XmlElementSerializer
{
    private static readonly Encoding s_utf8WithoutBom = new UTF8Encoding(false);
    private static readonly Encoding s_utf16WithoutBom = new UnicodeEncoding(false, false);

    /// <summary>
    /// Writes an XML element to a string.
    /// </summary>
    /// <param name="xmlElement">
    /// The XML element to write to a string.
    /// </param>
    /// <returns>
    /// The string form of the XML element.
    /// </returns>
    public static string WriteToString(XmlElement xmlElement)
    {
        // Create XmlWriterSettings with UTF-16 encoding (because serializing to String).
        XmlWriterSettings settings = new()
        {
            Encoding = s_utf16WithoutBom,
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = true,
        };

        // Create an XmlWriter with the specified settings
        StringBuilder sb = new();
        using (XmlWriter xmlWriter = XmlWriter.Create(sb, settings))
        {
            // Write the document to the XmlWriter
            xmlElement.WriteTo(xmlWriter);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes an XML element to a UTF-8 byte stream.
    /// </summary>
    /// <param name="xmlElement">
    /// The XML element to write to a UTF-8 byte stream.
    /// </param>
    /// <returns>
    /// The UTF-8 byte stream of the XML element.
    /// </returns>
    public static MemoryStream WriteToUtf8Stream(XmlElement xmlElement)
    {
        // Create a MemoryStream to hold the byte stream
        MemoryStream memoryStream = new();

        // Create XmlWriterSettings with UTF-8 encoding.
        XmlWriterSettings settings = new()
        {
            Encoding = s_utf8WithoutBom,
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = true,
        };

        // Create an XmlWriter with the specified settings
        using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, settings))
        {
            // Write the document to the XmlWriter
            xmlElement.WriteTo(xmlWriter);
        }

        return memoryStream;
    }

    /// <summary>
    /// Gets the number of bytes that would be written to a UTF-8 byte stream for the XML attribute.
    /// </summary>
    /// <param name="xmlAttribute">
    /// The XML attribute to measure.
    /// </param>
    /// <returns>
    /// The number of bytes that would be written to a UTF-8 byte stream for the XML attribute.
    /// </returns>
    public static int GetUtf8ByteCount(XmlAttribute xmlAttribute) =>
        Encoding.UTF8.GetByteCount(xmlAttribute.Value)
        + xmlAttribute.Prefix.Length
        + (xmlAttribute.Prefix.Length > 0 ? 1 : 0)
        + xmlAttribute.LocalName.Length
        + 3; // equal sign and two quotations.

    /// <summary>
    /// Gets the number of bytes that would be written to a UTF-8 byte stream for the XML element.
    /// </summary>
    /// <param name="xmlElement">
    /// The XML element to measure.
    /// </param>
    /// <returns>
    /// The number of bytes that would be written to a UTF-8 byte stream for the XML element.
    /// </returns>
    public static int GetUtf8ByteCount(XmlElement xmlElement)
    {
        // Create a MemoryStream to hold the byte stream
        ByteCountingStream byteCountingStream = new();

        // Create XmlWriterSettings with UTF-8 encoding.
        XmlWriterSettings settings = new()
        {
            Encoding = s_utf8WithoutBom,
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = true,
        };

        // Create an XmlWriter with the specified settings
        using (XmlWriter xmlWriter = XmlWriter.Create(byteCountingStream, settings))
        {
            // Write the document to the XmlWriter
            xmlElement.WriteTo(xmlWriter);
        }

        return (int)byteCountingStream.BytesWritten;
    }
}

/// <summary>
/// Helper methods for moving elements and attributes around.
/// </summary>
file static class XmlNodeMover
{
    /// <summary>
    /// Moves an attribute to a new parent element, ensuring it's namespace is declared at the target.
    /// </summary>
    /// <param name="attribute">
    /// The attribute to move.
    /// </param>
    /// <param name="targetParent">
    /// The target parent receiving the attribute.
    /// </param>
    /// <returns>
    /// The moved attribute, which may be a new instance if it had to be imported into a new document.
    /// </returns>
    public static XmlAttribute MoveAttribute(XmlAttribute attribute, XmlElement targetParent)
    {
        // Create a duplicate attribute for the new document.
        XmlAttribute newAttribute = targetParent.OwnerDocument != attribute.OwnerDocument
            ? (XmlAttribute)targetParent.OwnerDocument.ImportNode(attribute, true)
            : attribute;

        // Ensure the target parent has the attribute's namespace declared.
        targetParent.EnsureNamespaceDeclared(false, attribute.GetPrefixedNamespace());

        // Remove the old attribute from it's parent, and add the new one to the new parent.
        attribute.OwnerElement?.Attributes!.Remove(attribute);
        targetParent.Attributes!.Append(newAttribute);
        return newAttribute;
    }

    /// <summary>
    /// Moves an element to a new parent element, ensuring all undeclared namespaces are declared.
    /// </summary>
    /// <param name="element">
    /// The element to move.
    /// </param>
    /// <param name="targetParent">
    /// The target parent receiving the element.
    /// </param>
    /// <returns>
    /// The moved element, which may be a new instance if it had to be imported into a new document.
    /// </returns>
    public static XmlElement MoveElement(XmlElement element, XmlElement targetParent)
    {
        // First make sure all namespaces are declared at the immediate element.
        HashSet<XmlPrefixedNamespace> undeclaredNamespaces = element.GetNamespacesNotDeclaredInProgeny();
        foreach (XmlPrefixedNamespace undeclaredNamespace in undeclaredNamespaces)
        {
            element.EnsureNamespaceDeclared(false, undeclaredNamespace);
        }

        // Create a duplicate element for the new document.
        XmlElement newElement = targetParent.OwnerDocument != element.OwnerDocument
            ? (XmlElement)targetParent.OwnerDocument.ImportNode(element, true)
            : element;

        // Remove the old element from it's document, and add the new one to the new document.
        element.ParentNode?.RemoveChild(element);
        targetParent.AppendChild(newElement);
        return newElement;
    }
}
