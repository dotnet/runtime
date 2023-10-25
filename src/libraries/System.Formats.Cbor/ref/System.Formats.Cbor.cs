// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Formats.Cbor
{
    public enum CborConformanceMode
    {
        Lax = 0,
        Strict = 1,
        Canonical = 2,
        Ctap2Canonical = 3,
    }
    public partial class CborContentException : System.Exception
    {
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected CborContentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CborContentException(string? message) { }
        public CborContentException(string? message, System.Exception? inner) { }
    }
    public partial class CborReader
    {
        public CborReader(System.ReadOnlyMemory<byte> data, System.Formats.Cbor.CborConformanceMode conformanceMode = System.Formats.Cbor.CborConformanceMode.Strict, bool allowMultipleRootLevelValues = false) { }
        public bool AllowMultipleRootLevelValues { get { throw null; } }
        public int BytesRemaining { get { throw null; } }
        public System.Formats.Cbor.CborConformanceMode ConformanceMode { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public System.Formats.Cbor.CborReaderState PeekState() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Formats.Cbor.CborTag PeekTag() { throw null; }
        public System.Numerics.BigInteger ReadBigInteger() { throw null; }
        public bool ReadBoolean() { throw null; }
        public byte[] ReadByteString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong ReadCborNegativeIntegerRepresentation() { throw null; }
        public System.DateTimeOffset ReadDateTimeOffset() { throw null; }
        public decimal ReadDecimal() { throw null; }
        public System.ReadOnlyMemory<byte> ReadDefiniteLengthByteString() { throw null; }
        public System.ReadOnlyMemory<byte> ReadDefiniteLengthTextStringBytes() { throw null; }
        public double ReadDouble() { throw null; }
        public System.ReadOnlyMemory<byte> ReadEncodedValue(bool disableConformanceModeChecks = false) { throw null; }
        public void ReadEndArray() { }
        public void ReadEndIndefiniteLengthByteString() { }
        public void ReadEndIndefiniteLengthTextString() { }
        public void ReadEndMap() { }
        public int ReadInt32() { throw null; }
        public long ReadInt64() { throw null; }
        public void ReadNull() { }
        public System.Formats.Cbor.CborSimpleValue ReadSimpleValue() { throw null; }
        public float ReadSingle() { throw null; }
        public int? ReadStartArray() { throw null; }
        public void ReadStartIndefiniteLengthByteString() { }
        public void ReadStartIndefiniteLengthTextString() { }
        public int? ReadStartMap() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Formats.Cbor.CborTag ReadTag() { throw null; }
        public string ReadTextString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint ReadUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong ReadUInt64() { throw null; }
        public System.DateTimeOffset ReadUnixTimeSeconds() { throw null; }
        public void Reset(System.ReadOnlyMemory<byte> data) { }
        public void SkipToParent(bool disableConformanceModeChecks = false) { }
        public void SkipValue(bool disableConformanceModeChecks = false) { }
        public bool TryReadByteString(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public bool TryReadTextString(System.Span<char> destination, out int charsWritten) { throw null; }
    }
    public enum CborReaderState
    {
        Undefined = 0,
        UnsignedInteger = 1,
        NegativeInteger = 2,
        ByteString = 3,
        StartIndefiniteLengthByteString = 4,
        EndIndefiniteLengthByteString = 5,
        TextString = 6,
        StartIndefiniteLengthTextString = 7,
        EndIndefiniteLengthTextString = 8,
        StartArray = 9,
        EndArray = 10,
        StartMap = 11,
        EndMap = 12,
        Tag = 13,
        SimpleValue = 14,
        HalfPrecisionFloat = 15,
        SinglePrecisionFloat = 16,
        DoublePrecisionFloat = 17,
        Null = 18,
        Boolean = 19,
        Finished = 20,
    }
    public enum CborSimpleValue : byte
    {
        False = (byte)20,
        True = (byte)21,
        Null = (byte)22,
        Undefined = (byte)23,
    }
    [System.CLSCompliantAttribute(false)]
    public enum CborTag : ulong
    {
        DateTimeString = (ulong)0,
        UnixTimeSeconds = (ulong)1,
        UnsignedBigNum = (ulong)2,
        NegativeBigNum = (ulong)3,
        DecimalFraction = (ulong)4,
        BigFloat = (ulong)5,
        Base64UrlLaterEncoding = (ulong)21,
        Base64StringLaterEncoding = (ulong)22,
        Base16StringLaterEncoding = (ulong)23,
        EncodedCborDataItem = (ulong)24,
        Uri = (ulong)32,
        Base64Url = (ulong)33,
        Base64 = (ulong)34,
        Regex = (ulong)35,
        MimeMessage = (ulong)36,
        SelfDescribeCbor = (ulong)55799,
    }
    public partial class CborWriter
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public CborWriter(System.Formats.Cbor.CborConformanceMode conformanceMode, bool convertIndefiniteLengthEncodings, bool allowMultipleRootLevelValues) { }
        public CborWriter(System.Formats.Cbor.CborConformanceMode conformanceMode = System.Formats.Cbor.CborConformanceMode.Strict, bool convertIndefiniteLengthEncodings = false, bool allowMultipleRootLevelValues = false, int initialCapacity = -1) { }
        public bool AllowMultipleRootLevelValues { get { throw null; } }
        public int BytesWritten { get { throw null; } }
        public System.Formats.Cbor.CborConformanceMode ConformanceMode { get { throw null; } }
        public bool ConvertIndefiniteLengthEncodings { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public bool IsWriteCompleted { get { throw null; } }
        public byte[] Encode() { throw null; }
        public int Encode(System.Span<byte> destination) { throw null; }
        public void Reset() { }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public void WriteBigInteger(System.Numerics.BigInteger value) { }
        public void WriteBoolean(bool value) { }
        public void WriteByteString(byte[] value) { }
        public void WriteByteString(System.ReadOnlySpan<byte> value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteCborNegativeIntegerRepresentation(ulong value) { }
        public void WriteDateTimeOffset(System.DateTimeOffset value) { }
        public void WriteDecimal(decimal value) { }
        public void WriteDouble(double value) { }
        public void WriteEncodedValue(System.ReadOnlySpan<byte> encodedValue) { }
        public void WriteEndArray() { }
        public void WriteEndIndefiniteLengthByteString() { }
        public void WriteEndIndefiniteLengthTextString() { }
        public void WriteEndMap() { }
        public void WriteInt32(int value) { }
        public void WriteInt64(long value) { }
        public void WriteNull() { }
        public void WriteSimpleValue(System.Formats.Cbor.CborSimpleValue value) { }
        public void WriteSingle(float value) { }
        public void WriteStartArray(int? definiteLength) { }
        public void WriteStartIndefiniteLengthByteString() { }
        public void WriteStartIndefiniteLengthTextString() { }
        public void WriteStartMap(int? definiteLength) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteTag(System.Formats.Cbor.CborTag tag) { }
        public void WriteTextString(System.ReadOnlySpan<char> value) { }
        public void WriteTextString(string value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteUInt32(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteUInt64(ulong value) { }
        public void WriteUnixTimeSeconds(double seconds) { }
        public void WriteUnixTimeSeconds(long seconds) { }
    }
}
