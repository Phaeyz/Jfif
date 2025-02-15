namespace Phaeyz.Jfif;

/// <summary>
/// Every segment in JFIF starts with 1-byte <see cref="Phaeyz.Jfif.SegmentReader.MarkerIndicator"/>
/// (0xFF) followed by a 1-byte marker defined in this enum.
/// </summary>
public enum Marker : byte
{
    /// <summary>
    /// StartOfFrameBaselineDct (SOF0)
    /// </summary>
    StartOfFrameBaselineDct                                 = 0xC0,
    /// <summary>
    /// StartOfFrameExtendedSequentialDct (SOF1)
    /// </summary>
    StartOfFrameExtendedSequentialDct                       = 0xC1,
    /// <summary>
    /// StartOfFrameProgressiveDct (SOF2)
    /// </summary>
    StartOfFrameProgressiveDct                              = 0xC2,
    /// <summary>
    /// StartOfFrameLossless (SOF3)
    /// </summary>
    StartOfFrameLossless                                    = 0xC3,
    /// <summary>
    /// DefineHuffmanTable (DHT)
    /// </summary>
    DefineHuffmanTable                                      = 0xC4,
    /// <summary>
    /// StartOfFrameDifferentialSequentialDct (SOF5)
    /// </summary>
    StartOfFrameDifferentialSequentialDct                   = 0xC5,
    /// <summary>
    /// StartOfFrameDifferentialProgressiveDct (SOF6)
    /// </summary>
    StartOfFrameDifferentialProgressiveDct                  = 0xC6,
    /// <summary>
    /// StartOfFrameDifferentialLossless (SOF7)
    /// </summary>
    StartOfFrameDifferentialLossless                        = 0xC7,
    /// <summary>
    /// StartOfFrameExtendedSequentialDctArithmeticCoding (SOF9)
    /// </summary>
    StartOfFrameExtendedSequentialDctArithmeticCoding       = 0xC9,
    /// <summary>
    /// StartOfFrameProgressiveDctArithmeticCoding (SOF10)
    /// </summary>
    StartOfFrameProgressiveDctArithmeticCoding              = 0xCA,
    /// <summary>
    /// StartOfFrameLosslessArithmeticCoding (SOF11)
    /// </summary>
    StartOfFrameLosslessArithmeticCoding                    = 0xCB,
    /// <summary>
    /// DefineArithmeticCoding (DAC)
    /// </summary>
    DefineArithmeticCoding                                  = 0xCC,
    /// <summary>
    /// StartOfFrameDifferentialSequentialDctArithmeticCoding (SOF13)
    /// </summary>
    StartOfFrameDifferentialSequentialDctArithmeticCoding   = 0xCD,
    /// <summary>
    /// StartOfFrameDifferentialProgressiveDctArithmeticCoding (SOF14)
    /// </summary>
    StartOfFrameDifferentialProgressiveDctArithmeticCoding  = 0xCE,
    /// <summary>
    /// StartOfFrameDifferentialLosslessArithmeticCoding (SOF15)
    /// </summary>
    StartOfFrameDifferentialLosslessArithmeticCoding        = 0xCF,
    /// <summary>
    /// StartOfImage (SOI)
    /// </summary>
    StartOfImage                                            = 0xD8,
    /// <summary>
    /// EndOfImage (EOI)
    /// </summary>
    EndOfImage                                              = 0xD9,
    /// <summary>
    /// StartOfScan (SOS)
    /// </summary>
    StartOfScan                                             = 0xDA,
    /// <summary>
    /// DefineQuantizationTable (DQT)
    /// </summary>
    DefineQuantizationTable                                 = 0xDB,
    /// <summary>
    /// Restart0 (RST0, only used within image data)
    /// </summary>
    Restart0                                                = 0xD0,
    /// <summary>
    /// Restart1 (RST1, only used within image data)
    /// </summary>
    Restart1                                                = 0xD1,
    /// <summary>
    /// Restart2 (RST2, only used within image data)
    /// </summary>
    Restart2                                                = 0xD2,
    /// <summary>
    /// Restart3 (RST3, only used within image data)
    /// </summary>
    Restart3                                                = 0xD3,
    /// <summary>
    /// Restart4 (RST4, only used within image data)
    /// </summary>
    Restart4                                                = 0xD4,
    /// <summary>
    /// Restart5 (RST5, only used within image data)
    /// </summary>
    Restart5                                                = 0xD5,
    /// <summary>
    /// Restart6 (RST6, only used within image data)
    /// </summary>
    Restart6                                                = 0xD6,
    /// <summary>
    /// Restart7 (RST7, only used within image data)
    /// </summary>
    Restart7                                                = 0xD7,
    /// <summary>
    /// DefineNumberOfLines (DNL)
    /// </summary>
    DefineNumberOfLines                                     = 0xDC,
    /// <summary>
    /// DefineRestartInterval (DRI)
    /// </summary>
    DefineRestartInterval                                   = 0xDD,
    /// <summary>
    /// App0 (APP0)
    /// </summary>
    App0                                                    = 0xE0,
    /// <summary>
    /// App1 (APP1)
    /// </summary>
    App1                                                    = 0xE1,
    /// <summary>
    /// AppIccProfile (APP2)
    /// </summary>
    AppIccProfile                                           = 0xE2,
    /// <summary>
    /// AppNitf (APP6)
    /// </summary>
    AppNitf                                                 = 0xE6,
    /// <summary>
    /// AppHdr (APP11)
    /// </summary>
    AppHdr                                                  = 0xEB,
    /// <summary>
    /// AppPictureTransportProtocol (APP12)
    /// </summary>
    AppPictureTransportProtocol                             = 0xEC,
    /// <summary>
    /// AppIptc (APP13)
    /// </summary>
    AppIptc                                                 = 0xED,
    /// <summary>
    /// AppAdobe (APP14)
    /// </summary>
    AppAdobe                                                = 0xEE,
    /// <summary>
    /// Comment (COM)
    /// </summary>
    Comment                                                 = 0xFE,
}