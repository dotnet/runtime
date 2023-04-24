// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Formats.Asn1
{
    public readonly partial struct Asn1Tag : System.IEquatable<System.Formats.Asn1.Asn1Tag>
    {
        private readonly int _dummyPrimitive;
        public static readonly System.Formats.Asn1.Asn1Tag Boolean;
        public static readonly System.Formats.Asn1.Asn1Tag ConstructedBitString;
        public static readonly System.Formats.Asn1.Asn1Tag ConstructedOctetString;
        public static readonly System.Formats.Asn1.Asn1Tag Enumerated;
        public static readonly System.Formats.Asn1.Asn1Tag GeneralizedTime;
        public static readonly System.Formats.Asn1.Asn1Tag Integer;
        public static readonly System.Formats.Asn1.Asn1Tag Null;
        public static readonly System.Formats.Asn1.Asn1Tag ObjectIdentifier;
        public static readonly System.Formats.Asn1.Asn1Tag PrimitiveBitString;
        public static readonly System.Formats.Asn1.Asn1Tag PrimitiveOctetString;
        public static readonly System.Formats.Asn1.Asn1Tag Sequence;
        public static readonly System.Formats.Asn1.Asn1Tag SetOf;
        public static readonly System.Formats.Asn1.Asn1Tag UtcTime;
        public Asn1Tag(System.Formats.Asn1.TagClass tagClass, int tagValue, bool isConstructed = false) { throw null; }
        public Asn1Tag(System.Formats.Asn1.UniversalTagNumber universalTagNumber, bool isConstructed = false) { throw null; }
        public bool IsConstructed { get { throw null; } }
        public System.Formats.Asn1.TagClass TagClass { get { throw null; } }
        public int TagValue { get { throw null; } }
        public System.Formats.Asn1.Asn1Tag AsConstructed() { throw null; }
        public System.Formats.Asn1.Asn1Tag AsPrimitive() { throw null; }
        public int CalculateEncodedSize() { throw null; }
        public static System.Formats.Asn1.Asn1Tag Decode(System.ReadOnlySpan<byte> source, out int bytesConsumed) { throw null; }
        public int Encode(System.Span<byte> destination) { throw null; }
        public bool Equals(System.Formats.Asn1.Asn1Tag other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public bool HasSameClassAndValue(System.Formats.Asn1.Asn1Tag other) { throw null; }
        public static bool operator ==(System.Formats.Asn1.Asn1Tag left, System.Formats.Asn1.Asn1Tag right) { throw null; }
        public static bool operator !=(System.Formats.Asn1.Asn1Tag left, System.Formats.Asn1.Asn1Tag right) { throw null; }
        public override string ToString() { throw null; }
        public static bool TryDecode(System.ReadOnlySpan<byte> source, out System.Formats.Asn1.Asn1Tag tag, out int bytesConsumed) { throw null; }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
    }
    public partial class AsnContentException : System.Exception
    {
        public AsnContentException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected AsnContentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AsnContentException(string? message) { }
        public AsnContentException(string? message, System.Exception? inner) { }
    }
    public static partial class AsnDecoder
    {
        public static byte[] ReadBitString(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int unusedBitCount, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool ReadBoolean(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static string ReadCharacterString(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Formats.Asn1.UniversalTagNumber encodingType, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.Formats.Asn1.Asn1Tag ReadEncodedValue(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed) { throw null; }
        public static System.ReadOnlySpan<byte> ReadEnumeratedBytes(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.Enum ReadEnumeratedValue(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Type enumType, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static TEnum ReadEnumeratedValue<TEnum>(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) where TEnum : System.Enum { throw null; }
        public static System.DateTimeOffset ReadGeneralizedTime(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.Numerics.BigInteger ReadInteger(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.ReadOnlySpan<byte> ReadIntegerBytes(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.Collections.BitArray ReadNamedBitList(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.Enum ReadNamedBitListValue(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Type flagsEnumType, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) where TFlagsEnum : System.Enum { throw null; }
        public static void ReadNull(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static string ReadObjectIdentifier(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static byte[] ReadOctetString(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static void ReadSequence(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static void ReadSetOf(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed, bool skipSortOrderValidation = false, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static System.DateTimeOffset ReadUtcTime(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, int twoDigitYearMax = 2049, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadBitString(System.ReadOnlySpan<byte> source, System.Span<byte> destination, System.Formats.Asn1.AsnEncodingRules ruleSet, out int unusedBitCount, out int bytesConsumed, out int bytesWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadCharacterString(System.ReadOnlySpan<byte> source, System.Span<char> destination, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Formats.Asn1.UniversalTagNumber encodingType, out int bytesConsumed, out int charsWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadCharacterStringBytes(System.ReadOnlySpan<byte> source, System.Span<byte> destination, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Formats.Asn1.Asn1Tag expectedTag, out int bytesConsumed, out int bytesWritten) { throw null; }
        public static bool TryReadEncodedValue(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out System.Formats.Asn1.Asn1Tag tag, out int contentOffset, out int contentLength, out int bytesConsumed) { throw null; }
        public static bool TryReadInt32(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadInt64(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out long value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadOctetString(System.ReadOnlySpan<byte> source, System.Span<byte> destination, System.Formats.Asn1.AsnEncodingRules ruleSet, out int bytesConsumed, out int bytesWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadPrimitiveBitString(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out int unusedBitCount, out System.ReadOnlySpan<byte> value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public static bool TryReadPrimitiveCharacterStringBytes(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Formats.Asn1.Asn1Tag expectedTag, out System.ReadOnlySpan<byte> value, out int bytesConsumed) { throw null; }
        public static bool TryReadPrimitiveOctetString(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out System.ReadOnlySpan<byte> value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt32(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out uint value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt64(System.ReadOnlySpan<byte> source, System.Formats.Asn1.AsnEncodingRules ruleSet, out ulong value, out int bytesConsumed, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
    }
    public enum AsnEncodingRules
    {
        BER = 0,
        CER = 1,
        DER = 2,
    }
    public partial class AsnReader
    {
        public AsnReader(System.ReadOnlyMemory<byte> data, System.Formats.Asn1.AsnEncodingRules ruleSet, System.Formats.Asn1.AsnReaderOptions options = default(System.Formats.Asn1.AsnReaderOptions)) { }
        public bool HasData { get { throw null; } }
        public System.Formats.Asn1.AsnEncodingRules RuleSet { get { throw null; } }
        public System.ReadOnlyMemory<byte> PeekContentBytes() { throw null; }
        public System.ReadOnlyMemory<byte> PeekEncodedValue() { throw null; }
        public System.Formats.Asn1.Asn1Tag PeekTag() { throw null; }
        public byte[] ReadBitString(out int unusedBitCount, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool ReadBoolean(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public string ReadCharacterString(System.Formats.Asn1.UniversalTagNumber encodingType, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.ReadOnlyMemory<byte> ReadEncodedValue() { throw null; }
        public System.ReadOnlyMemory<byte> ReadEnumeratedBytes(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Enum ReadEnumeratedValue(System.Type enumType, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public TEnum ReadEnumeratedValue<TEnum>(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) where TEnum : System.Enum { throw null; }
        public System.DateTimeOffset ReadGeneralizedTime(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Numerics.BigInteger ReadInteger(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.ReadOnlyMemory<byte> ReadIntegerBytes(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Collections.BitArray ReadNamedBitList(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Enum ReadNamedBitListValue(System.Type flagsEnumType, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) where TFlagsEnum : System.Enum { throw null; }
        public void ReadNull(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public string ReadObjectIdentifier(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public byte[] ReadOctetString(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Formats.Asn1.AsnReader ReadSequence(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Formats.Asn1.AsnReader ReadSetOf(bool skipSortOrderValidation, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Formats.Asn1.AsnReader ReadSetOf(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.DateTimeOffset ReadUtcTime(int twoDigitYearMax, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.DateTimeOffset ReadUtcTime(System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public void ThrowIfNotEmpty() { }
        public bool TryReadBitString(System.Span<byte> destination, out int unusedBitCount, out int bytesWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadCharacterString(System.Span<char> destination, System.Formats.Asn1.UniversalTagNumber encodingType, out int charsWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadCharacterStringBytes(System.Span<byte> destination, System.Formats.Asn1.Asn1Tag expectedTag, out int bytesWritten) { throw null; }
        public bool TryReadInt32(out int value, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadInt64(out long value, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadOctetString(System.Span<byte> destination, out int bytesWritten, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadPrimitiveBitString(out int unusedBitCount, out System.ReadOnlyMemory<byte> value, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public bool TryReadPrimitiveCharacterStringBytes(System.Formats.Asn1.Asn1Tag expectedTag, out System.ReadOnlyMemory<byte> contents) { throw null; }
        public bool TryReadPrimitiveOctetString(out System.ReadOnlyMemory<byte> contents, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryReadUInt32(out uint value, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryReadUInt64(out ulong value, System.Formats.Asn1.Asn1Tag? expectedTag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
    }
    public partial struct AsnReaderOptions
    {
        private int _dummyPrimitive;
        public bool SkipSetSortOrderVerification { readonly get { throw null; } set { } }
        public int UtcTimeTwoDigitYearMax { get { throw null; } set { } }
    }
    public sealed partial class AsnWriter
    {
        public AsnWriter(System.Formats.Asn1.AsnEncodingRules ruleSet) { }
        public AsnWriter(System.Formats.Asn1.AsnEncodingRules ruleSet, int initialCapacity) { }
        public System.Formats.Asn1.AsnEncodingRules RuleSet { get { throw null; } }
        public void CopyTo(System.Formats.Asn1.AsnWriter destination) { }
        public byte[] Encode() { throw null; }
        public int Encode(System.Span<byte> destination) { throw null; }
        public bool EncodedValueEquals(System.Formats.Asn1.AsnWriter other) { throw null; }
        public bool EncodedValueEquals(System.ReadOnlySpan<byte> other) { throw null; }
        public int GetEncodedLength() { throw null; }
        public void PopOctetString(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void PopSequence(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void PopSetOf(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public System.Formats.Asn1.AsnWriter.Scope PushOctetString(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Formats.Asn1.AsnWriter.Scope PushSequence(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public System.Formats.Asn1.AsnWriter.Scope PushSetOf(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { throw null; }
        public void Reset() { }
        public bool TryEncode(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public void WriteBitString(System.ReadOnlySpan<byte> value, int unusedBitCount = 0, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteBoolean(bool value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteCharacterString(System.Formats.Asn1.UniversalTagNumber encodingType, System.ReadOnlySpan<char> str, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteCharacterString(System.Formats.Asn1.UniversalTagNumber encodingType, string value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteEncodedValue(System.ReadOnlySpan<byte> value) { }
        public void WriteEnumeratedValue(System.Enum value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteEnumeratedValue<TEnum>(TEnum value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) where TEnum : System.Enum { }
        public void WriteGeneralizedTime(System.DateTimeOffset value, bool omitFractionalSeconds = false, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteInteger(long value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteInteger(System.Numerics.BigInteger value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteInteger(System.ReadOnlySpan<byte> value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteInteger(ulong value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteIntegerUnsigned(System.ReadOnlySpan<byte> value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteNamedBitList(System.Collections.BitArray value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteNamedBitList(System.Enum value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteNamedBitList<TEnum>(TEnum value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) where TEnum : System.Enum { }
        public void WriteNull(System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteObjectIdentifier(System.ReadOnlySpan<char> oidValue, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteObjectIdentifier(string oidValue, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteOctetString(System.ReadOnlySpan<byte> value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteUtcTime(System.DateTimeOffset value, int twoDigitYearMax, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public void WriteUtcTime(System.DateTimeOffset value, System.Formats.Asn1.Asn1Tag? tag = default(System.Formats.Asn1.Asn1Tag?)) { }
        public readonly partial struct Scope : System.IDisposable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public void Dispose() { }
        }
    }
    public enum TagClass
    {
        Universal = 0,
        Application = 64,
        ContextSpecific = 128,
        Private = 192,
    }
    public enum UniversalTagNumber
    {
        EndOfContents = 0,
        Boolean = 1,
        Integer = 2,
        BitString = 3,
        OctetString = 4,
        Null = 5,
        ObjectIdentifier = 6,
        ObjectDescriptor = 7,
        External = 8,
        InstanceOf = 8,
        Real = 9,
        Enumerated = 10,
        Embedded = 11,
        UTF8String = 12,
        RelativeObjectIdentifier = 13,
        Time = 14,
        Sequence = 16,
        SequenceOf = 16,
        Set = 17,
        SetOf = 17,
        NumericString = 18,
        PrintableString = 19,
        T61String = 20,
        TeletexString = 20,
        VideotexString = 21,
        IA5String = 22,
        UtcTime = 23,
        GeneralizedTime = 24,
        GraphicString = 25,
        ISO646String = 26,
        VisibleString = 26,
        GeneralString = 27,
        UniversalString = 28,
        UnrestrictedCharacterString = 29,
        BMPString = 30,
        Date = 31,
        TimeOfDay = 32,
        DateTime = 33,
        Duration = 34,
        ObjectIdentifierIRI = 35,
        RelativeObjectIdentifierIRI = 36,
    }
}
