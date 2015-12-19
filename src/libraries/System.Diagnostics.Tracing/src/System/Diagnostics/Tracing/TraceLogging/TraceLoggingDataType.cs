using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Used when implementing a custom TraceLoggingTypeInfo.
    /// These are passed to metadataCollector.Add to specify the low-level
    /// type of a field in the event payload. Note that a "formatted"
    /// TraceLoggingDataType consists of a core TraceLoggingDataType value
    /// (a TraceLoggingDataType with a value less than 32) plus an OutType.
    /// Any combination of TraceLoggingDataType + OutType is valid, but not
    /// all are useful. In particular, combinations not explicitly listed
    /// below are unlikely to be recognized by decoders, and will typically
    /// be decoded as the corresponding core type (i.e. the decoder will
    /// mask off any unrecognized OutType value).
    /// </summary>
    internal enum TraceLoggingDataType
    {
        /// <summary>
        /// Core type.
        /// Data type with no value (0-length payload).
        /// NOTE: arrays of Nil are illegal.
        /// NOTE: a fixed-length array of Nil is interpreted by the decoder as
        /// a struct (obsolete but retained for backwards-compatibility).
        /// </summary>
        Nil = 0,

        /// <summary>
        /// Core type.
        /// Encoding assumes null-terminated Char16 string.
        /// Decoding treats as UTF-16LE string.
        /// NOTE: arrays of Utf16String will not be supported until M3.
        /// </summary>
        Utf16String = 1,

        /// <summary>
        /// Core type.
        /// Encoding assumes null-terminated Char8 string.
        /// Decoding treats as MBCS string.
        /// NOTE: arrays of MbcsString will not be supported until M3.
        /// </summary>
        MbcsString = 2,

        /// <summary>
        /// Core type.
        /// Encoding assumes 8-bit value.
        /// Decoding treats as signed integer.
        /// </summary>
        Int8 = 3,

        /// <summary>
        /// Core type.
        /// Encoding assumes 8-bit value.
        /// Decoding treats as unsigned integer.
        /// </summary>
        UInt8 = 4,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-bit value.
        /// Decoding treats as signed integer.
        /// </summary>
        Int16 = 5,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-bit value.
        /// Decoding treats as unsigned integer.
        /// </summary>
        UInt16 = 6,

        /// <summary>
        /// Core type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as signed integer.
        /// </summary>
        Int32 = 7,

        /// <summary>
        /// Core type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as unsigned integer.
        /// </summary>
        UInt32 = 8,

        /// <summary>
        /// Core type.
        /// Encoding assumes 64-bit value.
        /// Decoding treats as signed integer.
        /// </summary>
        Int64 = 9,

        /// <summary>
        /// Core type.
        /// Encoding assumes 64-bit value.
        /// Decoding treats as unsigned integer.
        /// </summary>
        UInt64 = 10,

        /// <summary>
        /// Core type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as Float.
        /// </summary>
        Float = 11,

        /// <summary>
        /// Core type.
        /// Encoding assumes 64-bit value.
        /// Decoding treats as Double.
        /// </summary>
        Double = 12,

        /// <summary>
        /// Core type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as Boolean.
        /// </summary>
        Boolean32 = 13,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-bit bytecount followed by binary data.
        /// Decoding treats as binary data.
        /// NOTE: arrays of Binary will not be supported until M3.
        /// </summary>
        Binary = 14,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-byte value.
        /// Decoding treats as GUID.
        /// </summary>
        Guid = 15,

        /// <summary>
        /// Core type.
        /// Encoding assumes 64-bit value.
        /// Decoding treats as FILETIME.
        /// </summary>
        FileTime = 17,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-byte value.
        /// Decoding treats as SYSTEMTIME.
        /// </summary>
        SystemTime = 18,

        /// <summary>
        /// Core type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as hexadecimal unsigned integer.
        /// </summary>
        HexInt32 = 20,

        /// <summary>
        /// Core type.
        /// Encoding assumes 64-bit value.
        /// Decoding treats as hexadecimal unsigned integer.
        /// </summary>
        HexInt64 = 21,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-bit bytecount followed by Char16 data.
        /// Decoding treats as UTF-16LE string.
        /// </summary>
        CountedUtf16String = 22,

        /// <summary>
        /// Core type.
        /// Encoding assumes 16-bit bytecount followed by Char8 data.
        /// Decoding treats as MBCS string.
        /// </summary>
        CountedMbcsString = 23,

        /// <summary>
        /// Core type.
        /// Special case: Struct indicates that this field plus the the
        /// subsequent N logical fields are to be considered as one logical
        /// field (i.e. a nested structure). The OutType is used to encode N.
        /// The maximum value for N is 127. This field has no payload by
        /// itself, but logically contains the payload of the following N
        /// fields. It is legal to have an array of Struct.
        /// </summary>
        Struct = 24,

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit value.
        /// Decoding treats as UTF-16LE character.
        /// </summary>
        Char16 = UInt16 + (EventFieldFormat.String << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 8-bit value.
        /// Decoding treats as character.
        /// </summary>
        Char8 = UInt8 + (EventFieldFormat.String << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 8-bit value.
        /// Decoding treats as Boolean.
        /// </summary>
        Boolean8 = UInt8 + (EventFieldFormat.Boolean << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 8-bit value.
        /// Decoding treats as hexadecimal unsigned integer.
        /// </summary>
        HexInt8 = UInt8 + (EventFieldFormat.Hexadecimal << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit value.
        /// Decoding treats as hexadecimal unsigned integer.
        /// </summary>
        HexInt16 = UInt16 + (EventFieldFormat.Hexadecimal << 8),

#if false 
        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as process identifier.
        /// </summary>
        ProcessId = UInt32 + (EventSourceFieldFormat.ProcessId << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as thread identifier.
        /// </summary>
        ThreadId = UInt32 + (EventSourceFieldFormat.ThreadId << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit value.
        /// Decoding treats as IP port.
        /// </summary>
        Port = UInt16 + (EventSourceFieldFormat.Port << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as IPv4 address.
        /// </summary>
        Ipv4Address = UInt32 + (EventSourceFieldFormat.Ipv4Address << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by binary data.
        /// Decoding treats as IPv6 address.
        /// NOTE: arrays of Ipv6Address will not be supported until M3.
        /// </summary>
        Ipv6Address = Binary + (EventSourceFieldFormat.Ipv6Address << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by binary data.
        /// Decoding treats as SOCKADDR.
        /// NOTE: arrays of SocketAddress will not be supported until M3.
        /// </summary>
        SocketAddress = Binary + (EventSourceFieldFormat.SocketAddress << 8),
#endif
        /// <summary>
        /// Formatted type.
        /// Encoding assumes null-terminated Char16 string.
        /// Decoding treats as UTF-16LE XML string.
        /// NOTE: arrays of Utf16Xml will not be supported until M3.
        /// </summary>
        Utf16Xml = Utf16String + (EventFieldFormat.Xml << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes null-terminated Char8 string.
        /// Decoding treats as MBCS XML string.
        /// NOTE: arrays of MbcsXml will not be supported until M3.
        /// </summary>
        MbcsXml = MbcsString + (EventFieldFormat.Xml << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by Char16 data.
        /// Decoding treats as UTF-16LE XML.
        /// </summary>
        CountedUtf16Xml = CountedUtf16String + (EventFieldFormat.Xml << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by Char8 data.
        /// Decoding treats as MBCS XML.
        /// </summary>
        CountedMbcsXml = CountedMbcsString + (EventFieldFormat.Xml << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes null-terminated Char16 string.
        /// Decoding treats as UTF-16LE JSON string.
        /// NOTE: arrays of Utf16Json will not be supported until M3.
        /// </summary>
        Utf16Json = Utf16String + (EventFieldFormat.Json << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes null-terminated Char8 string.
        /// Decoding treats as MBCS JSON string.
        /// NOTE: arrays of MbcsJson will not be supported until M3.
        /// </summary>
        MbcsJson = MbcsString + (EventFieldFormat.Json << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by Char16 data.
        /// Decoding treats as UTF-16LE JSON.
        /// </summary>
        CountedUtf16Json = CountedUtf16String + (EventFieldFormat.Json << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 16-bit bytecount followed by Char8 data.
        /// Decoding treats as MBCS JSON.
        /// </summary>
        CountedMbcsJson = CountedMbcsString + (EventFieldFormat.Json << 8),
#if false 
        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as Win32 error.
        /// </summary>
        Win32Error = UInt32 + (EventSourceFieldFormat.Win32Error << 8),

        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as NTSTATUS.
        /// </summary>
        NTStatus = UInt32 + (EventSourceFieldFormat.NTStatus << 8),
#endif
        /// <summary>
        /// Formatted type.
        /// Encoding assumes 32-bit value.
        /// Decoding treats as HRESULT.
        /// </summary>
        HResult = Int32 + (EventFieldFormat.HResult << 8)
    }
}
