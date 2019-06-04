// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json
{
    public enum JsonCommentHandling : byte
    {
        Disallow = (byte)0,
        Allow = (byte)1,
        Skip = (byte)2,
    }
    public sealed partial class JsonDocument : System.IDisposable
    {
        internal JsonDocument() { }
        public System.Text.Json.JsonElement RootElement { get { throw null; } }
        public void Dispose() { }
        public static System.Text.Json.JsonDocument Parse(System.Buffers.ReadOnlySequence<byte> utf8Json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.IO.Stream utf8Json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.ReadOnlyMemory<byte> utf8Json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.ReadOnlyMemory<char> json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(string json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public static System.Threading.Tasks.Task<System.Text.Json.JsonDocument> ParseAsync(System.IO.Stream utf8Json, System.Text.Json.JsonReaderOptions readerOptions = default(System.Text.Json.JsonReaderOptions), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Text.Json.JsonDocument ParseValue(ref System.Text.Json.Utf8JsonReader reader) { throw null; }
        public static bool TryParseValue(ref System.Text.Json.Utf8JsonReader reader, out System.Text.Json.JsonDocument document) { throw null; }
    }
    public readonly partial struct JsonElement
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Text.Json.JsonElement this[int index] { get { throw null; } }
        public System.Text.Json.JsonValueType Type { get { throw null; } }
        public System.Text.Json.JsonElement Clone() { throw null; }
        public System.Text.Json.JsonElement.ArrayEnumerator EnumerateArray() { throw null; }
        public System.Text.Json.JsonElement.ObjectEnumerator EnumerateObject() { throw null; }
        public int GetArrayLength() { throw null; }
        public bool GetBoolean() { throw null; }
        public byte[] GetBytesFromBase64() { throw null; }
        public System.DateTime GetDateTime() { throw null; }
        public System.DateTimeOffset GetDateTimeOffset() { throw null; }
        public decimal GetDecimal() { throw null; }
        public double GetDouble() { throw null; }
        public System.Guid GetGuid() { throw null; }
        public int GetInt32() { throw null; }
        public long GetInt64() { throw null; }
        public System.Text.Json.JsonElement GetProperty(System.ReadOnlySpan<byte> utf8PropertyName) { throw null; }
        public System.Text.Json.JsonElement GetProperty(System.ReadOnlySpan<char> propertyName) { throw null; }
        public System.Text.Json.JsonElement GetProperty(string propertyName) { throw null; }
        public string GetRawText() { throw null; }
        public float GetSingle() { throw null; }
        public string GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public override string ToString() { throw null; }
        public bool TryGetBytesFromBase64(out byte[] value) { throw null; }
        public bool TryGetDateTime(out System.DateTime value) { throw null; }
        public bool TryGetDateTimeOffset(out System.DateTimeOffset value) { throw null; }
        public bool TryGetDecimal(out decimal value) { throw null; }
        public bool TryGetDouble(out double value) { throw null; }
        public bool TryGetGuid(out System.Guid value) { throw null; }
        public bool TryGetInt32(out int value) { throw null; }
        public bool TryGetInt64(out long value) { throw null; }
        public bool TryGetProperty(System.ReadOnlySpan<byte> utf8PropertyName, out System.Text.Json.JsonElement value) { throw null; }
        public bool TryGetProperty(System.ReadOnlySpan<char> propertyName, out System.Text.Json.JsonElement value) { throw null; }
        public bool TryGetProperty(string propertyName, out System.Text.Json.JsonElement value) { throw null; }
        public bool TryGetSingle(out float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool ValueEquals(string text) { throw null; }
        public void WriteAsProperty(System.ReadOnlySpan<byte> utf8PropertyName, System.Text.Json.Utf8JsonWriter writer) { }
        public void WriteAsProperty(System.ReadOnlySpan<char> propertyName, System.Text.Json.Utf8JsonWriter writer) { }
        public void WriteAsProperty(string propertyName, System.Text.Json.Utf8JsonWriter writer) { }
        public void WriteAsValue(System.Text.Json.Utf8JsonWriter writer) { }
        public partial struct ArrayEnumerator : System.Collections.Generic.IEnumerable<System.Text.Json.JsonElement>, System.Collections.Generic.IEnumerator<System.Text.Json.JsonElement>, System.Collections.IEnumerable, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Text.Json.JsonElement Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public System.Text.Json.JsonElement.ArrayEnumerator GetEnumerator() { throw null; }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            System.Collections.Generic.IEnumerator<System.Text.Json.JsonElement> System.Collections.Generic.IEnumerable<System.Text.Json.JsonElement>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        public partial struct ObjectEnumerator : System.Collections.Generic.IEnumerable<System.Text.Json.JsonProperty>, System.Collections.Generic.IEnumerator<System.Text.Json.JsonProperty>, System.Collections.IEnumerable, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Text.Json.JsonProperty Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public System.Text.Json.JsonElement.ObjectEnumerator GetEnumerator() { throw null; }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            System.Collections.Generic.IEnumerator<System.Text.Json.JsonProperty> System.Collections.Generic.IEnumerable<System.Text.Json.JsonProperty>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
    }
    public readonly partial struct JsonEncodedText : System.IEquatable<System.Text.Json.JsonEncodedText>
    {
        private readonly object _dummy;
        public System.ReadOnlySpan<byte> EncodedUtf8Bytes { get { throw null; } }
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<byte> utf8Value) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<char> value) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(string value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonEncodedText other) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class JsonException : System.Exception
    {
        protected JsonException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine) { }
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine, System.Exception innerException) { }
        public long? BytePositionInLine { get { throw null; } }
        public long? LineNumber { get { throw null; } }
        public string Path { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public readonly partial struct JsonProperty
    {
        private readonly object _dummy;
        public string Name { get { throw null; } }
        public System.Text.Json.JsonElement Value { get { throw null; } }
        public bool NameEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool NameEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool NameEquals(string text) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct JsonReaderOptions
    {
        private int _dummyPrimitive;
        public bool AllowTrailingCommas { get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling CommentHandling { get { throw null; } set { } }
        public int MaxDepth { get { throw null; } set { } }
    }
    public partial struct JsonReaderState
    {
        private object _dummy;
        private int _dummyPrimitive;
        public JsonReaderState(System.Text.Json.JsonReaderOptions options = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public long BytesConsumed { get { throw null; } }
        public System.Text.Json.JsonReaderOptions Options { get { throw null; } }
        public System.SequencePosition Position { get { throw null; } }
    }
    public enum JsonTokenType : byte
    {
        None = (byte)0,
        StartObject = (byte)1,
        EndObject = (byte)2,
        StartArray = (byte)3,
        EndArray = (byte)4,
        PropertyName = (byte)5,
        String = (byte)6,
        Number = (byte)7,
        True = (byte)8,
        False = (byte)9,
        Null = (byte)10,
        Comment = (byte)11,
    }
    public enum JsonValueType : byte
    {
        Undefined = (byte)0,
        Object = (byte)1,
        Array = (byte)2,
        String = (byte)3,
        Number = (byte)4,
        True = (byte)5,
        False = (byte)6,
        Null = (byte)7,
    }
    public partial struct JsonWriterOptions
    {
        private int _dummyPrimitive;
        public bool Indented { get { throw null; } set { } }
        public bool SkipValidation { get { throw null; } set { } }
    }
    public ref partial struct Utf8JsonReader
    {
        private object _dummy;
        private int _dummyPrimitive;
        public Utf8JsonReader(in System.Buffers.ReadOnlySequence<byte> jsonData, bool isFinalBlock, System.Text.Json.JsonReaderState state) { throw null; }
        public Utf8JsonReader(System.ReadOnlySpan<byte> jsonData, bool isFinalBlock, System.Text.Json.JsonReaderState state) { throw null; }
        public long BytesConsumed { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public System.Text.Json.JsonReaderState CurrentState { get { throw null; } }
        public bool HasValueSequence { get { throw null; } }
        public bool IsFinalBlock { get { throw null; } }
        public System.SequencePosition Position { get { throw null; } }
        public long TokenStartIndex { get { throw null; } }
        public System.Text.Json.JsonTokenType TokenType { get { throw null; } }
        public System.Buffers.ReadOnlySequence<byte> ValueSequence { get { throw null; } }
        public System.ReadOnlySpan<byte> ValueSpan { get { throw null; } }
        public bool GetBoolean() { throw null; }
        public byte[] GetBytesFromBase64() { throw null; }
        public string GetComment() { throw null; }
        public System.DateTime GetDateTime() { throw null; }
        public System.DateTimeOffset GetDateTimeOffset() { throw null; }
        public decimal GetDecimal() { throw null; }
        public double GetDouble() { throw null; }
        public System.Guid GetGuid() { throw null; }
        public int GetInt32() { throw null; }
        public long GetInt64() { throw null; }
        public float GetSingle() { throw null; }
        public string GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public bool Read() { throw null; }
        public void Skip() { }
        public bool TryGetBytesFromBase64(out byte[] value) { throw null; }
        public bool TryGetDateTime(out System.DateTime value) { throw null; }
        public bool TryGetDateTimeOffset(out System.DateTimeOffset value) { throw null; }
        public bool TryGetDecimal(out decimal value) { throw null; }
        public bool TryGetDouble(out double value) { throw null; }
        public bool TryGetGuid(out System.Guid value) { throw null; }
        public bool TryGetInt32(out int value) { throw null; }
        public bool TryGetInt64(out long value) { throw null; }
        public bool TryGetSingle(out float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value) { throw null; }
        public bool TrySkip() { throw null; }
        public bool ValueTextEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool ValueTextEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool ValueTextEquals(string text) { throw null; }
    }
    public sealed partial class Utf8JsonWriter : System.IDisposable
    {
        public Utf8JsonWriter(System.Buffers.IBufferWriter<byte> bufferWriter, System.Text.Json.JsonWriterOptions options = default(System.Text.Json.JsonWriterOptions)) { }
        public Utf8JsonWriter(System.IO.Stream utf8Json, System.Text.Json.JsonWriterOptions options = default(System.Text.Json.JsonWriterOptions)) { }
        public long BytesCommitted { get { throw null; } }
        public int BytesPending { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public System.Text.Json.JsonWriterOptions Options { get { throw null; } }
        public void Dispose() { }
        public void Flush() { }
        public System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Reset() { }
        public void Reset(System.Buffers.IBufferWriter<byte> bufferWriter) { }
        public void Reset(System.IO.Stream utf8Json) { }
        public void WriteBase64String(System.ReadOnlySpan<byte> utf8PropertyName, System.ReadOnlySpan<byte> bytes) { }
        public void WriteBase64String(System.ReadOnlySpan<char> propertyName, System.ReadOnlySpan<byte> bytes) { }
        public void WriteBase64String(string propertyName, System.ReadOnlySpan<byte> bytes) { }
        public void WriteBase64String(System.Text.Json.JsonEncodedText propertyName, System.ReadOnlySpan<byte> bytes) { }
        public void WriteBase64StringValue(System.ReadOnlySpan<byte> bytes) { }
        public void WriteBoolean(System.ReadOnlySpan<byte> utf8PropertyName, bool value) { }
        public void WriteBoolean(System.ReadOnlySpan<char> propertyName, bool value) { }
        public void WriteBoolean(string propertyName, bool value) { }
        public void WriteBoolean(System.Text.Json.JsonEncodedText propertyName, bool value) { }
        public void WriteBooleanValue(bool value) { }
        public void WriteCommentValue(System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteCommentValue(System.ReadOnlySpan<char> value) { }
        public void WriteCommentValue(string value) { }
        public void WriteEndArray() { }
        public void WriteEndObject() { }
        public void WriteNull(System.ReadOnlySpan<byte> utf8PropertyName) { }
        public void WriteNull(System.ReadOnlySpan<char> propertyName) { }
        public void WriteNull(string propertyName) { }
        public void WriteNull(System.Text.Json.JsonEncodedText propertyName) { }
        public void WriteNullValue() { }
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, decimal value) { }
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, double value) { }
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, int value) { }
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, long value) { }
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.ReadOnlySpan<byte> utf8PropertyName, ulong value) { }
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, decimal value) { }
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, double value) { }
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, int value) { }
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, long value) { }
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.ReadOnlySpan<char> propertyName, ulong value) { }
        public void WriteNumber(string propertyName, decimal value) { }
        public void WriteNumber(string propertyName, double value) { }
        public void WriteNumber(string propertyName, int value) { }
        public void WriteNumber(string propertyName, long value) { }
        public void WriteNumber(string propertyName, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(string propertyName, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(string propertyName, ulong value) { }
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, decimal value) { }
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, double value) { }
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, int value) { }
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, long value) { }
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumber(System.Text.Json.JsonEncodedText propertyName, ulong value) { }
        public void WriteNumberValue(decimal value) { }
        public void WriteNumberValue(double value) { }
        public void WriteNumberValue(int value) { }
        public void WriteNumberValue(long value) { }
        public void WriteNumberValue(float value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumberValue(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumberValue(ulong value) { }
        public void WriteStartArray() { }
        public void WriteStartArray(System.ReadOnlySpan<byte> utf8PropertyName) { }
        public void WriteStartArray(System.ReadOnlySpan<char> propertyName) { }
        public void WriteStartArray(string propertyName) { }
        public void WriteStartArray(System.Text.Json.JsonEncodedText propertyName) { }
        public void WriteStartObject() { }
        public void WriteStartObject(System.ReadOnlySpan<byte> utf8PropertyName) { }
        public void WriteStartObject(System.ReadOnlySpan<char> propertyName) { }
        public void WriteStartObject(string propertyName) { }
        public void WriteStartObject(System.Text.Json.JsonEncodedText propertyName) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.DateTime value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.DateTimeOffset value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.Guid value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, string value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.DateTime value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.DateTimeOffset value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.Guid value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, string value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(string propertyName, System.DateTime value) { }
        public void WriteString(string propertyName, System.DateTimeOffset value) { }
        public void WriteString(string propertyName, System.Guid value) { }
        public void WriteString(string propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(string propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(string propertyName, string value) { }
        public void WriteString(string propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.DateTime value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.DateTimeOffset value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.Guid value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, string value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteStringValue(System.DateTime value) { }
        public void WriteStringValue(System.DateTimeOffset value) { }
        public void WriteStringValue(System.Guid value) { }
        public void WriteStringValue(System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteStringValue(System.ReadOnlySpan<char> value) { }
        public void WriteStringValue(string value) { }
        public void WriteStringValue(System.Text.Json.JsonEncodedText value) { }
    }
}
namespace System.Text.Json.Serialization
{
    public abstract partial class JsonAttribute : System.Attribute
    {
        protected JsonAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class JsonExtensionDataAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonExtensionDataAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class JsonIgnoreAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonIgnoreAttribute() { }
    }
    public abstract partial class JsonNamingPolicy
    {
        protected JsonNamingPolicy() { }
        public static System.Text.Json.Serialization.JsonNamingPolicy CamelCase { get { throw null; } }
        public abstract string ConvertName(string name);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class JsonPropertyNameAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPropertyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    public static partial class JsonSerializer
    {
        public static object ReadValue(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions options = null) { throw null; }
        public static TValue ReadValue<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions options = null) { throw null; }
        public static object Parse(System.ReadOnlySpan<byte> utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static object Parse(string json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static TValue Parse<TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static TValue Parse<TValue>(string json, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static System.Threading.Tasks.ValueTask<object> ReadAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue> ReadAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static string ToString(object value, System.Type type, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static string ToString<TValue>(TValue value, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static byte[] ToUtf8Bytes(object value, System.Type type, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static byte[] ToUtf8Bytes<TValue>(TValue value, System.Text.Json.Serialization.JsonSerializerOptions options = null) { throw null; }
        public static System.Threading.Tasks.Task WriteAsync(object value, System.Type type, System.IO.Stream utf8Json, System.Text.Json.Serialization.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task WriteAsync<TValue>(TValue value, System.IO.Stream utf8Json, System.Text.Json.Serialization.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class JsonSerializerOptions
    {
        public JsonSerializerOptions() { }
        public bool AllowTrailingCommas { get { throw null; } set { } }
        public int DefaultBufferSize { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonNamingPolicy DictionaryKeyPolicy { get { throw null; } set { } }
        public bool IgnoreNullValues { get { throw null; } set { } }
        public bool IgnoreReadOnlyProperties { get { throw null; } set { } }
        public int MaxDepth { get { throw null; } set { } }
        public bool PropertyNameCaseInsensitive { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonNamingPolicy PropertyNamingPolicy { get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling ReadCommentHandling { get { throw null; } set { } }
        public bool WriteIndented { get { throw null; } set { } }
    }
}
