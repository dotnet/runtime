// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json
{
    public enum DuplicatePropertyNameHandlingStrategy
    {
        Replace = 0,
        Ignore = 1,
        Error = 2,
    }
    public sealed partial class JsonArray : System.Text.Json.JsonNode, System.Collections.Generic.ICollection<System.Text.Json.JsonNode>, System.Collections.Generic.IEnumerable<System.Text.Json.JsonNode>, System.Collections.Generic.IList<System.Text.Json.JsonNode>, System.Collections.Generic.IReadOnlyCollection<System.Text.Json.JsonNode>, System.Collections.Generic.IReadOnlyList<System.Text.Json.JsonNode>, System.Collections.IEnumerable
    {
        public JsonArray() { }
        public JsonArray(System.Collections.Generic.IEnumerable<bool> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<byte> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<decimal> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<double> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<short> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<int> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<long> values) { }
        [System.CLSCompliantAttribute(false)]
        public JsonArray(System.Collections.Generic.IEnumerable<sbyte> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<float> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<string> values) { }
        public JsonArray(System.Collections.Generic.IEnumerable<System.Text.Json.JsonNode> values) { }
        [System.CLSCompliantAttribute(false)]
        public JsonArray(System.Collections.Generic.IEnumerable<ushort> values) { }
        [System.CLSCompliantAttribute(false)]
        public JsonArray(System.Collections.Generic.IEnumerable<uint> values) { }
        [System.CLSCompliantAttribute(false)]
        public JsonArray(System.Collections.Generic.IEnumerable<ulong> values) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public System.Text.Json.JsonNode this[int idx] { get { throw null; } set { } }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public void Add(System.Text.Json.JsonNode value) { }
        public void Clear() { }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public bool Contains(System.Text.Json.JsonNode value) { throw null; }
        public System.Text.Json.JsonArrayEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Text.Json.JsonNode item) { throw null; }
        public void Insert(int index, System.Text.Json.JsonNode item) { }
        public int LastIndexOf(System.Text.Json.JsonNode item) { throw null; }
        public bool Remove(System.Text.Json.JsonNode item) { throw null; }
        public int RemoveAll(System.Predicate<System.Text.Json.JsonNode> match) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.Generic.ICollection<System.Text.Json.JsonNode>.CopyTo(System.Text.Json.JsonNode[] array, int arrayIndex) { }
        System.Collections.Generic.IEnumerator<System.Text.Json.JsonNode> System.Collections.Generic.IEnumerable<System.Text.Json.JsonNode>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial struct JsonArrayEnumerator : System.Collections.Generic.IEnumerator<System.Text.Json.JsonNode>, System.Collections.IEnumerator, System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public JsonArrayEnumerator(System.Text.Json.JsonArray jsonArray) { throw null; }
        public System.Text.Json.JsonNode Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public void Dispose() { }
        public bool MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public sealed partial class JsonBoolean : System.Text.Json.JsonNode, System.IEquatable<System.Text.Json.JsonBoolean>
    {
        public JsonBoolean() { }
        public JsonBoolean(bool value) { }
        public bool Value { get { throw null; } set { } }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonBoolean other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Text.Json.JsonBoolean left, System.Text.Json.JsonBoolean right) { throw null; }
        public static bool operator !=(System.Text.Json.JsonBoolean left, System.Text.Json.JsonBoolean right) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum JsonCommentHandling : byte
    {
        Disallow = (byte)0,
        Skip = (byte)1,
        Allow = (byte)2,
    }
    public sealed partial class JsonDocument : System.IDisposable
    {
        internal JsonDocument() { }
        public System.Text.Json.JsonElement RootElement { get { throw null; } }
        public void Dispose() { }
        public static System.Text.Json.JsonDocument Parse(System.Buffers.ReadOnlySequence<byte> utf8Json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.IO.Stream utf8Json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.ReadOnlyMemory<byte> utf8Json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(System.ReadOnlyMemory<char> json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse(string json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Threading.Tasks.Task<System.Text.Json.JsonDocument> ParseAsync(System.IO.Stream utf8Json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Text.Json.JsonDocument ParseValue(ref System.Text.Json.Utf8JsonReader reader) { throw null; }
        public static bool TryParseValue(ref System.Text.Json.Utf8JsonReader reader, out System.Text.Json.JsonDocument document) { throw null; }
        public void WriteTo(System.Text.Json.Utf8JsonWriter writer) { }
    }
    public partial struct JsonDocumentOptions
    {
        private int _dummyPrimitive;
        public bool AllowTrailingCommas { readonly get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling CommentHandling { readonly get { throw null; } set { } }
        public int MaxDepth { readonly get { throw null; } set { } }
    }
    public readonly partial struct JsonElement
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool IsImmutable { get { throw null; } }
        public System.Text.Json.JsonElement this[int index] { get { throw null; } }
        public System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public System.Text.Json.JsonElement Clone() { throw null; }
        public System.Text.Json.JsonElement.ArrayEnumerator EnumerateArray() { throw null; }
        public System.Text.Json.JsonElement.ObjectEnumerator EnumerateObject() { throw null; }
        public int GetArrayLength() { throw null; }
        public bool GetBoolean() { throw null; }
        public byte GetByte() { throw null; }
        public byte[] GetBytesFromBase64() { throw null; }
        public System.DateTime GetDateTime() { throw null; }
        public System.DateTimeOffset GetDateTimeOffset() { throw null; }
        public decimal GetDecimal() { throw null; }
        public double GetDouble() { throw null; }
        public System.Guid GetGuid() { throw null; }
        public short GetInt16() { throw null; }
        public int GetInt32() { throw null; }
        public long GetInt64() { throw null; }
        public System.Text.Json.JsonElement GetProperty(System.ReadOnlySpan<byte> utf8PropertyName) { throw null; }
        public System.Text.Json.JsonElement GetProperty(System.ReadOnlySpan<char> propertyName) { throw null; }
        public System.Text.Json.JsonElement GetProperty(string propertyName) { throw null; }
        public string GetRawText() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte GetSByte() { throw null; }
        public float GetSingle() { throw null; }
        public string GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public override string ToString() { throw null; }
        public bool TryGetByte(out byte value) { throw null; }
        public bool TryGetBytesFromBase64(out byte[] value) { throw null; }
        public bool TryGetDateTime(out System.DateTime value) { throw null; }
        public bool TryGetDateTimeOffset(out System.DateTimeOffset value) { throw null; }
        public bool TryGetDecimal(out decimal value) { throw null; }
        public bool TryGetDouble(out double value) { throw null; }
        public bool TryGetGuid(out System.Guid value) { throw null; }
        public bool TryGetInt16(out short value) { throw null; }
        public bool TryGetInt32(out int value) { throw null; }
        public bool TryGetInt64(out long value) { throw null; }
        public bool TryGetProperty(System.ReadOnlySpan<byte> utf8PropertyName, out System.Text.Json.JsonElement value) { throw null; }
        public bool TryGetProperty(System.ReadOnlySpan<char> propertyName, out System.Text.Json.JsonElement value) { throw null; }
        public bool TryGetProperty(string propertyName, out System.Text.Json.JsonElement value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetSByte(out sbyte value) { throw null; }
        public bool TryGetSingle(out float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt16(out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool ValueEquals(string text) { throw null; }
        public void WriteTo(System.Text.Json.Utf8JsonWriter writer) { }
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
        private readonly int _dummyPrimitive;
        public System.ReadOnlySpan<byte> EncodedUtf8Bytes { get { throw null; } }
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<byte> utf8Value, System.Text.Encodings.Web.JavaScriptEncoder encoder = null) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<char> value, System.Text.Encodings.Web.JavaScriptEncoder encoder = null) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(string value, System.Text.Encodings.Web.JavaScriptEncoder encoder = null) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonEncodedText other) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class JsonException : System.Exception
    {
        public JsonException() { }
        protected JsonException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public JsonException(string message) { }
        public JsonException(string message, System.Exception innerException) { }
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine) { }
        public JsonException(string message, string path, long? lineNumber, long? bytePositionInLine, System.Exception innerException) { }
        public long? BytePositionInLine { get { throw null; } }
        public long? LineNumber { get { throw null; } }
        public override string Message { get { throw null; } }
        public string Path { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class JsonNamingPolicy
    {
        protected JsonNamingPolicy() { }
        public static System.Text.Json.JsonNamingPolicy CamelCase { get { throw null; } }
        public abstract string ConvertName(string name);
    }
    public abstract partial class JsonNode
    {
        internal JsonNode() { }
        public abstract System.Text.Json.JsonValueKind ValueKind { get; }
        public System.Text.Json.JsonElement AsJsonElement() { throw null; }
        public abstract System.Text.Json.JsonNode Clone();
        public static System.Text.Json.JsonNode DeepCopy(System.Text.Json.JsonElement jsonElement) { throw null; }
        public static System.Text.Json.JsonNode GetNode(System.Text.Json.JsonElement jsonElement) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (bool value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (byte value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (System.DateTime value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (System.DateTimeOffset value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (decimal value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (double value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (System.Guid value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (short value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (int value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.JsonNode (sbyte value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (float value) { throw null; }
        public static implicit operator System.Text.Json.JsonNode (string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.JsonNode (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.JsonNode (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.JsonNode (ulong value) { throw null; }
        public static System.Text.Json.JsonNode Parse(string json, System.Text.Json.JsonNodeOptions options = default(System.Text.Json.JsonNodeOptions)) { throw null; }
        public string ToJsonString() { throw null; }
        public static bool TryGetNode(System.Text.Json.JsonElement jsonElement, out System.Text.Json.JsonNode jsonNode) { throw null; }
        public void WriteTo(System.Text.Json.Utf8JsonWriter writer) { }
    }
    public partial struct JsonNodeOptions
    {
        private int _dummyPrimitive;
        public bool AllowTrailingCommas { readonly get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling CommentHandling { readonly get { throw null; } set { } }
        public System.Text.Json.DuplicatePropertyNameHandlingStrategy DuplicatePropertyNameHandling { readonly get { throw null; } set { } }
        public int MaxDepth { readonly get { throw null; } set { } }
    }
    public sealed partial class JsonNull : System.Text.Json.JsonNode, System.IEquatable<System.Text.Json.JsonNull>
    {
        public JsonNull() { }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonNull other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Text.Json.JsonNull left, System.Text.Json.JsonNull right) { throw null; }
        public static bool operator !=(System.Text.Json.JsonNull left, System.Text.Json.JsonNull right) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class JsonNumber : System.Text.Json.JsonNode, System.IEquatable<System.Text.Json.JsonNumber>
    {
        public JsonNumber() { }
        public JsonNumber(byte value) { }
        public JsonNumber(decimal value) { }
        public JsonNumber(double value) { }
        public JsonNumber(short value) { }
        public JsonNumber(int value) { }
        public JsonNumber(long value) { }
        [System.CLSCompliantAttribute(false)]
        public JsonNumber(sbyte value) { }
        public JsonNumber(float value) { }
        public JsonNumber(string value) { }
        [System.CLSCompliantAttribute(false)]
        public JsonNumber(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public JsonNumber(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public JsonNumber(ulong value) { }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonNumber other) { throw null; }
        public byte GetByte() { throw null; }
        public decimal GetDecimal() { throw null; }
        public double GetDouble() { throw null; }
        public override int GetHashCode() { throw null; }
        public short GetInt16() { throw null; }
        public int GetInt32() { throw null; }
        public long GetInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte GetSByte() { throw null; }
        public float GetSingle() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public static bool operator ==(System.Text.Json.JsonNumber left, System.Text.Json.JsonNumber right) { throw null; }
        public static bool operator !=(System.Text.Json.JsonNumber left, System.Text.Json.JsonNumber right) { throw null; }
        public void SetByte(byte value) { }
        public void SetDecimal(decimal value) { }
        public void SetDouble(double value) { }
        public void SetFormattedValue(string value) { }
        public void SetInt16(short value) { }
        public void SetInt32(int value) { }
        public void SetInt64(long value) { }
        [System.CLSCompliantAttribute(false)]
        public void SetSByte(sbyte value) { }
        public void SetSingle(float value) { }
        [System.CLSCompliantAttribute(false)]
        public void SetUInt16(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public void SetUInt32(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void SetUInt64(ulong value) { }
        public override string ToString() { throw null; }
        public bool TryGetByte(out byte value) { throw null; }
        public bool TryGetDecimal(out decimal value) { throw null; }
        public bool TryGetDouble(out double value) { throw null; }
        public bool TryGetInt16(out short value) { throw null; }
        public bool TryGetInt32(out int value) { throw null; }
        public bool TryGetInt64(out long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetSByte(out sbyte value) { throw null; }
        public bool TryGetSingle(out float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt16(out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value) { throw null; }
    }
    public sealed partial class JsonObject : System.Text.Json.JsonNode, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode>>, System.Collections.IEnumerable
    {
        public JsonObject() { }
        public JsonObject(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode>> jsonProperties) { }
        public System.Text.Json.JsonNode this[string propertyName] { get { throw null; } set { } }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode> jsonProperty) { }
        public void Add(string propertyName, System.Text.Json.JsonNode propertyValue) { }
        public void AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode>> jsonProperties) { }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public bool ContainsProperty(string propertyName) { throw null; }
        public bool ContainsProperty(string propertyName, System.StringComparison stringComparison) { throw null; }
        public System.Text.Json.JsonObjectEnumerator GetEnumerator() { throw null; }
        public System.Text.Json.JsonArray GetJsonArrayPropertyValue(string propertyName) { throw null; }
        public System.Text.Json.JsonArray GetJsonArrayPropertyValue(string propertyName, System.StringComparison stringComparison) { throw null; }
        public System.Text.Json.JsonObject GetJsonObjectPropertyValue(string propertyName) { throw null; }
        public System.Text.Json.JsonObject GetJsonObjectPropertyValue(string propertyName, System.StringComparison stringComparison) { throw null; }
        public System.Collections.Generic.IReadOnlyCollection<string> GetPropertyNames() { throw null; }
        public System.Text.Json.JsonNode GetPropertyValue(string propertyName) { throw null; }
        public System.Text.Json.JsonNode GetPropertyValue(string propertyName, System.StringComparison stringComparison) { throw null; }
        public System.Collections.Generic.IReadOnlyCollection<System.Text.Json.JsonNode> GetPropertyValues() { throw null; }
        public bool Remove(string propertyName) { throw null; }
        public bool Remove(string propertyName, System.StringComparison stringComparison) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.Text.Json.JsonNode>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetJsonArrayPropertyValue(string propertyName, System.StringComparison stringComparison, out System.Text.Json.JsonArray jsonArray) { throw null; }
        public bool TryGetJsonArrayPropertyValue(string propertyName, out System.Text.Json.JsonArray jsonArray) { throw null; }
        public bool TryGetJsonObjectPropertyValue(string propertyName, System.StringComparison stringComparison, out System.Text.Json.JsonObject jsonObject) { throw null; }
        public bool TryGetJsonObjectPropertyValue(string propertyName, out System.Text.Json.JsonObject jsonObject) { throw null; }
        public bool TryGetPropertyValue(string propertyName, System.StringComparison stringComparison, out System.Text.Json.JsonNode jsonNode) { throw null; }
        public bool TryGetPropertyValue(string propertyName, out System.Text.Json.JsonNode jsonNode) { throw null; }
    }
    public partial struct JsonObjectEnumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode>>, System.Collections.IEnumerator, System.IDisposable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public JsonObjectEnumerator(System.Text.Json.JsonObject jsonObject) { throw null; }
        public System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonNode> Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public void Dispose() { }
        public bool MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public readonly partial struct JsonProperty
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Name { get { throw null; } }
        public System.Text.Json.JsonElement Value { get { throw null; } }
        public bool NameEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool NameEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool NameEquals(string text) { throw null; }
        public override string ToString() { throw null; }
        public void WriteTo(System.Text.Json.Utf8JsonWriter writer) { }
    }
    public partial struct JsonReaderOptions
    {
        private int _dummyPrimitive;
        public bool AllowTrailingCommas { readonly get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling CommentHandling { readonly get { throw null; } set { } }
        public int MaxDepth { readonly get { throw null; } set { } }
    }
    public partial struct JsonReaderState
    {
        private object _dummy;
        private int _dummyPrimitive;
        public JsonReaderState(System.Text.Json.JsonReaderOptions options = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public System.Text.Json.JsonReaderOptions Options { get { throw null; } }
    }
    public static partial class JsonSerializer
    {
        public static object Deserialize(System.ReadOnlySpan<byte> utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static object Deserialize(string json, System.Type returnType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static object Deserialize(ref System.Text.Json.Utf8JsonReader reader, System.Type returnType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static System.Threading.Tasks.ValueTask<object> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static TValue Deserialize<TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static TValue Deserialize<TValue>(string json, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static TValue Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static string Serialize(object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static void Serialize(System.Text.Json.Utf8JsonWriter writer, object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options = null) { }
        public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static byte[] SerializeToUtf8Bytes(object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
        public static void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.JsonSerializerOptions options = null) { }
        public static string Serialize<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options = null) { throw null; }
    }
    public sealed partial class JsonSerializerOptions
    {
        public JsonSerializerOptions() { }
        public bool AllowTrailingCommas { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Text.Json.Serialization.JsonConverter> Converters { get { throw null; } }
        public int DefaultBufferSize { get { throw null; } set { } }
        public System.Text.Json.JsonNamingPolicy DictionaryKeyPolicy { get { throw null; } set { } }
        public System.Text.Encodings.Web.JavaScriptEncoder Encoder { get { throw null; } set { } }
        public bool IgnoreNullValues { get { throw null; } set { } }
        public bool IgnoreReadOnlyProperties { get { throw null; } set { } }
        public int MaxDepth { get { throw null; } set { } }
        public bool PropertyNameCaseInsensitive { get { throw null; } set { } }
        public System.Text.Json.JsonNamingPolicy PropertyNamingPolicy { get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling ReadCommentHandling { get { throw null; } set { } }
        public bool WriteIndented { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonConverter GetConverter(System.Type typeToConvert) { throw null; }
    }
    public sealed partial class JsonString : System.Text.Json.JsonNode, System.IEquatable<System.Text.Json.JsonString>
    {
        public JsonString() { }
        public JsonString(System.DateTime value) { }
        public JsonString(System.DateTimeOffset value) { }
        public JsonString(System.Guid value) { }
        public JsonString(System.ReadOnlySpan<char> value) { }
        public JsonString(string value) { }
        public string Value { get { throw null; } set { } }
        public override System.Text.Json.JsonValueKind ValueKind { get { throw null; } }
        public override System.Text.Json.JsonNode Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Text.Json.JsonString other) { throw null; }
        public System.DateTime GetDateTime() { throw null; }
        public System.DateTimeOffset GetDateTimeOffset() { throw null; }
        public System.Guid GetGuid() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Text.Json.JsonString left, System.Text.Json.JsonString right) { throw null; }
        public static bool operator !=(System.Text.Json.JsonString left, System.Text.Json.JsonString right) { throw null; }
        public override string ToString() { throw null; }
        public bool TryGetDateTime(out System.DateTime value) { throw null; }
        public bool TryGetDateTimeOffset(out System.DateTimeOffset value) { throw null; }
        public bool TryGetGuid(out System.Guid value) { throw null; }
    }
    public enum JsonTokenType : byte
    {
        None = (byte)0,
        StartObject = (byte)1,
        EndObject = (byte)2,
        StartArray = (byte)3,
        EndArray = (byte)4,
        PropertyName = (byte)5,
        Comment = (byte)6,
        String = (byte)7,
        Number = (byte)8,
        True = (byte)9,
        False = (byte)10,
        Null = (byte)11,
    }
    public enum JsonValueKind : byte
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
        private object _dummy;
        private int _dummyPrimitive;
        public System.Text.Encodings.Web.JavaScriptEncoder Encoder { readonly get { throw null; } set { } }
        public bool Indented { get { throw null; } set { } }
        public bool SkipValidation { get { throw null; } set { } }
    }
    public ref partial struct Utf8JsonReader
    {
        private object _dummy;
        private int _dummyPrimitive;
        public Utf8JsonReader(System.Buffers.ReadOnlySequence<byte> jsonData, bool isFinalBlock, System.Text.Json.JsonReaderState state) { throw null; }
        public Utf8JsonReader(System.Buffers.ReadOnlySequence<byte> jsonData, System.Text.Json.JsonReaderOptions options = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public Utf8JsonReader(System.ReadOnlySpan<byte> jsonData, bool isFinalBlock, System.Text.Json.JsonReaderState state) { throw null; }
        public Utf8JsonReader(System.ReadOnlySpan<byte> jsonData, System.Text.Json.JsonReaderOptions options = default(System.Text.Json.JsonReaderOptions)) { throw null; }
        public long BytesConsumed { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public System.Text.Json.JsonReaderState CurrentState { get { throw null; } }
        public readonly bool HasValueSequence { get { throw null; } }
        public bool IsFinalBlock { get { throw null; } }
        public System.SequencePosition Position { get { throw null; } }
        public readonly long TokenStartIndex { get { throw null; } }
        public System.Text.Json.JsonTokenType TokenType { get { throw null; } }
        public readonly System.Buffers.ReadOnlySequence<byte> ValueSequence { get { throw null; } }
        public readonly System.ReadOnlySpan<byte> ValueSpan { get { throw null; } }
        public bool GetBoolean() { throw null; }
        public byte GetByte() { throw null; }
        public byte[] GetBytesFromBase64() { throw null; }
        public string GetComment() { throw null; }
        public System.DateTime GetDateTime() { throw null; }
        public System.DateTimeOffset GetDateTimeOffset() { throw null; }
        public decimal GetDecimal() { throw null; }
        public double GetDouble() { throw null; }
        public System.Guid GetGuid() { throw null; }
        public short GetInt16() { throw null; }
        public int GetInt32() { throw null; }
        public long GetInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte GetSByte() { throw null; }
        public float GetSingle() { throw null; }
        public string GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public bool Read() { throw null; }
        public void Skip() { }
        public bool TryGetByte(out byte value) { throw null; }
        public bool TryGetBytesFromBase64(out byte[] value) { throw null; }
        public bool TryGetDateTime(out System.DateTime value) { throw null; }
        public bool TryGetDateTimeOffset(out System.DateTimeOffset value) { throw null; }
        public bool TryGetDecimal(out decimal value) { throw null; }
        public bool TryGetDouble(out double value) { throw null; }
        public bool TryGetGuid(out System.Guid value) { throw null; }
        public bool TryGetInt16(out short value) { throw null; }
        public bool TryGetInt32(out int value) { throw null; }
        public bool TryGetInt64(out long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetSByte(out sbyte value) { throw null; }
        public bool TryGetSingle(out float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt16(out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value) { throw null; }
        public bool TrySkip() { throw null; }
        public bool ValueTextEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool ValueTextEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool ValueTextEquals(string text) { throw null; }
    }
    public sealed partial class Utf8JsonWriter : System.IAsyncDisposable, System.IDisposable
    {
        public Utf8JsonWriter(System.Buffers.IBufferWriter<byte> bufferWriter, System.Text.Json.JsonWriterOptions options = default(System.Text.Json.JsonWriterOptions)) { }
        public Utf8JsonWriter(System.IO.Stream utf8Json, System.Text.Json.JsonWriterOptions options = default(System.Text.Json.JsonWriterOptions)) { }
        public long BytesCommitted { get { throw null; } }
        public int BytesPending { get { throw null; } }
        public int CurrentDepth { get { throw null; } }
        public System.Text.Json.JsonWriterOptions Options { get { throw null; } }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
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
        public void WritePropertyName(System.ReadOnlySpan<byte> utf8PropertyName) { }
        public void WritePropertyName(System.ReadOnlySpan<char> propertyName) { }
        public void WritePropertyName(string propertyName) { }
        public void WritePropertyName(System.Text.Json.JsonEncodedText propertyName) { }
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
    public abstract partial class JsonConverter
    {
        internal JsonConverter() { }
        public abstract bool CanConvert(System.Type typeToConvert);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple=false)]
    public partial class JsonConverterAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        protected JsonConverterAttribute() { }
        public JsonConverterAttribute(System.Type converterType) { }
        public System.Type ConverterType { get { throw null; } }
        public virtual System.Text.Json.Serialization.JsonConverter CreateConverter(System.Type typeToConvert) { throw null; }
    }
    public abstract partial class JsonConverterFactory : System.Text.Json.Serialization.JsonConverter
    {
        protected JsonConverterFactory() { }
        public abstract System.Text.Json.Serialization.JsonConverter CreateConverter(System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);
    }
    public abstract partial class JsonConverter<T> : System.Text.Json.Serialization.JsonConverter
    {
        protected internal JsonConverter() { }
        public override bool CanConvert(System.Type typeToConvert) { throw null; }
        public abstract T Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);
        public abstract void Write(System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options);
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
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class JsonPropertyNameAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPropertyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    public sealed partial class JsonStringEnumConverter : System.Text.Json.Serialization.JsonConverterFactory
    {
        public JsonStringEnumConverter() { }
        public JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy namingPolicy = null, bool allowIntegerValues = true) { }
        public override bool CanConvert(System.Type typeToConvert) { throw null; }
        public override System.Text.Json.Serialization.JsonConverter CreateConverter(System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
    }
}
