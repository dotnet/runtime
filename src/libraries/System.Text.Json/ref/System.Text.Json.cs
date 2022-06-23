// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json
{
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
        public static System.Text.Json.JsonDocument Parse([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlyMemory<char> json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.JsonDocument Parse([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Threading.Tasks.Task<System.Text.Json.JsonDocument> ParseAsync(System.IO.Stream utf8Json, System.Text.Json.JsonDocumentOptions options = default(System.Text.Json.JsonDocumentOptions), System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Text.Json.JsonDocument ParseValue(ref System.Text.Json.Utf8JsonReader reader) { throw null; }
        public static bool TryParseValue(ref System.Text.Json.Utf8JsonReader reader, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Text.Json.JsonDocument? document) { throw null; }
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
        public string? GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public static System.Text.Json.JsonElement ParseValue(ref System.Text.Json.Utf8JsonReader reader) { throw null; }
        public override string ToString() { throw null; }
        public bool TryGetByte(out byte value) { throw null; }
        public bool TryGetBytesFromBase64([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out byte[]? value) { throw null; }
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
        public static bool TryParseValue(ref System.Text.Json.Utf8JsonReader reader, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Text.Json.JsonElement? element) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool ValueEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool ValueEquals(string? text) { throw null; }
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
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<byte> utf8Value, System.Text.Encodings.Web.JavaScriptEncoder? encoder = null) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(System.ReadOnlySpan<char> value, System.Text.Encodings.Web.JavaScriptEncoder? encoder = null) { throw null; }
        public static System.Text.Json.JsonEncodedText Encode(string value, System.Text.Encodings.Web.JavaScriptEncoder? encoder = null) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Text.Json.JsonEncodedText other) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class JsonException : System.Exception
    {
        public JsonException() { }
        protected JsonException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public JsonException(string? message) { }
        public JsonException(string? message, System.Exception? innerException) { }
        public JsonException(string? message, string? path, long? lineNumber, long? bytePositionInLine) { }
        public JsonException(string? message, string? path, long? lineNumber, long? bytePositionInLine, System.Exception? innerException) { }
        public long? BytePositionInLine { get { throw null; } }
        public long? LineNumber { get { throw null; } }
        public override string Message { get { throw null; } }
        public string? Path { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class JsonNamingPolicy
    {
        protected JsonNamingPolicy() { }
        public static System.Text.Json.JsonNamingPolicy CamelCase { get { throw null; } }
        public abstract string ConvertName(string name);
    }
    public readonly partial struct JsonProperty
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Name { get { throw null; } }
        public System.Text.Json.JsonElement Value { get { throw null; } }
        public bool NameEquals(System.ReadOnlySpan<byte> utf8Text) { throw null; }
        public bool NameEquals(System.ReadOnlySpan<char> text) { throw null; }
        public bool NameEquals(string? text) { throw null; }
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
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(System.ReadOnlySpan<byte> utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(System.ReadOnlySpan<byte> utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlySpan<char> json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlySpan<char> json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(this System.Text.Json.JsonDocument document, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(this System.Text.Json.JsonDocument document, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(this System.Text.Json.JsonElement element, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(this System.Text.Json.JsonElement element, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(this System.Text.Json.Nodes.JsonNode? node, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(this System.Text.Json.Nodes.JsonNode? node, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static object? Deserialize(ref System.Text.Json.Utf8JsonReader reader, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static object? Deserialize(ref System.Text.Json.Utf8JsonReader reader, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Threading.Tasks.ValueTask<object?> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<object?> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Collections.Generic.IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Collections.Generic.IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Threading.Tasks.ValueTask<TValue?> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue?> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlySpan<char> json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlySpan<char> json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(this System.Text.Json.JsonDocument document, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(this System.Text.Json.JsonDocument document, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(this System.Text.Json.JsonElement element, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(this System.Text.Json.JsonElement element, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(this System.Text.Json.Nodes.JsonNode? node, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(this System.Text.Json.Nodes.JsonNode? node, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static TValue? Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static TValue? Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static void Serialize(System.IO.Stream utf8Json, object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static void Serialize(System.IO.Stream utf8Json, object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static string Serialize(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static string Serialize(object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static void Serialize(System.Text.Json.Utf8JsonWriter writer, object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static void Serialize(System.Text.Json.Utf8JsonWriter writer, object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.JsonDocument SerializeToDocument(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.JsonDocument SerializeToDocument(object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.JsonDocument SerializeToDocument<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.JsonDocument SerializeToDocument<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.JsonElement SerializeToElement(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.JsonElement SerializeToElement(object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.JsonElement SerializeToElement<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.JsonElement SerializeToElement<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.Nodes.JsonNode? SerializeToNode(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? SerializeToNode(object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static System.Text.Json.Nodes.JsonNode? SerializeToNode<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? SerializeToNode<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static byte[] SerializeToUtf8Bytes(object? value, System.Type inputType, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static byte[] SerializeToUtf8Bytes(object? value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static void Serialize<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static void Serialize<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.JsonSerializerOptions? options = null) { }
        public static void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
        public static string Serialize<TValue>(TValue value, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static string Serialize<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo) { throw null; }
    }
    public enum JsonSerializerDefaults
    {
        General = 0,
        Web = 1,
    }
    public sealed partial class JsonSerializerOptions
    {
        public JsonSerializerOptions() { }
        public JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults defaults) { }
        public JsonSerializerOptions(System.Text.Json.JsonSerializerOptions options) { }
        public bool AllowTrailingCommas { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Text.Json.Serialization.JsonConverter> Converters { get { throw null; } }
        public static System.Text.Json.JsonSerializerOptions Default { get { throw null; } }
        public int DefaultBufferSize { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonIgnoreCondition DefaultIgnoreCondition { get { throw null; } set { } }
        public System.Text.Json.JsonNamingPolicy? DictionaryKeyPolicy { get { throw null; } set { } }
        public System.Text.Encodings.Web.JavaScriptEncoder? Encoder { get { throw null; } set { } }
        [System.ObsoleteAttribute("JsonSerializerOptions.IgnoreNullValues is obsolete. To ignore null values when serializing, set DefaultIgnoreCondition to JsonIgnoreCondition.WhenWritingNull.", DiagnosticId = "SYSLIB0020", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool IgnoreNullValues { get { throw null; } set { } }
        public bool IgnoreReadOnlyFields { get { throw null; } set { } }
        public bool IgnoreReadOnlyProperties { get { throw null; } set { } }
        public bool IncludeFields { get { throw null; } set { } }
        public int MaxDepth { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonNumberHandling NumberHandling { get { throw null; } set { } }
        public bool PropertyNameCaseInsensitive { get { throw null; } set { } }
        public System.Text.Json.JsonNamingPolicy? PropertyNamingPolicy { get { throw null; } set { } }
        public System.Text.Json.JsonCommentHandling ReadCommentHandling { get { throw null; } set { } }
        public System.Text.Json.Serialization.ReferenceHandler? ReferenceHandler { get { throw null; } set { } }
        public System.Collections.Generic.IList<System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration> PolymorphicTypeConfigurations { get { throw null; } }
        public System.Text.Json.Serialization.JsonUnknownTypeHandling UnknownTypeHandling { get { throw null; } set { } }
        public bool WriteIndented { get { throw null; } set { } }
        public void AddContext<TContext>() where TContext : System.Text.Json.Serialization.JsonSerializerContext, new() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Getting a converter for a type may require reflection which depends on unreferenced code.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Getting a converter for a type may require reflection which depends on runtime code generation.")]
        public System.Text.Json.Serialization.JsonConverter GetConverter(System.Type typeToConvert) { throw null; }
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
        public System.Text.Encodings.Web.JavaScriptEncoder? Encoder { readonly get { throw null; } set { } }
        public bool Indented { get { throw null; } set { } }
        public int MaxDepth { readonly get { throw null; } set { } }
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
        public readonly bool ValueIsEscaped { get { throw null; } }
        public readonly System.Buffers.ReadOnlySequence<byte> ValueSequence { get { throw null; } }
        public readonly System.ReadOnlySpan<byte> ValueSpan { get { throw null; } }
        public readonly int CopyString(Span<byte> utf8Destination) { throw null; }
        public readonly int CopyString(Span<char> destination) { throw null; }
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
        public string? GetString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64() { throw null; }
        public bool Read() { throw null; }
        public void Skip() { }
        public bool TryGetByte(out byte value) { throw null; }
        public bool TryGetBytesFromBase64([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out byte[]? value) { throw null; }
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
        public bool ValueTextEquals(string? text) { throw null; }
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
        public void WriteRawValue([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, bool skipInputValidation = false) { }
        public void WriteRawValue(System.ReadOnlySpan<byte> utf8Json, bool skipInputValidation = false) { }
        public void WriteRawValue([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] System.ReadOnlySpan<char> json, bool skipInputValidation = false) { }
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
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, string? value) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8PropertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.DateTime value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.DateTimeOffset value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.Guid value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, string? value) { }
        public void WriteString(System.ReadOnlySpan<char> propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(string propertyName, System.DateTime value) { }
        public void WriteString(string propertyName, System.DateTimeOffset value) { }
        public void WriteString(string propertyName, System.Guid value) { }
        public void WriteString(string propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(string propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(string propertyName, string? value) { }
        public void WriteString(string propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.DateTime value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.DateTimeOffset value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.Guid value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.ReadOnlySpan<char> value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, string? value) { }
        public void WriteString(System.Text.Json.JsonEncodedText propertyName, System.Text.Json.JsonEncodedText value) { }
        public void WriteStringValue(System.DateTime value) { }
        public void WriteStringValue(System.DateTimeOffset value) { }
        public void WriteStringValue(System.Guid value) { }
        public void WriteStringValue(System.ReadOnlySpan<byte> utf8Value) { }
        public void WriteStringValue(System.ReadOnlySpan<char> value) { }
        public void WriteStringValue(string? value) { }
        public void WriteStringValue(System.Text.Json.JsonEncodedText value) { }
    }
}
namespace System.Text.Json.Nodes
{
    public sealed partial class JsonArray : System.Text.Json.Nodes.JsonNode, System.Collections.Generic.ICollection<System.Text.Json.Nodes.JsonNode?>, System.Collections.Generic.IEnumerable<System.Text.Json.Nodes.JsonNode?>, System.Collections.Generic.IList<System.Text.Json.Nodes.JsonNode?>, System.Collections.IEnumerable
    {
        public JsonArray(System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { }
        public JsonArray(System.Text.Json.Nodes.JsonNodeOptions options, params System.Text.Json.Nodes.JsonNode?[] items) { }
        public JsonArray(params System.Text.Json.Nodes.JsonNode?[] items) { }
        public int Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Text.Json.Nodes.JsonNode?>.IsReadOnly { get { throw null; } }
        public void Add(System.Text.Json.Nodes.JsonNode? item) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("Creating JsonValue instances with non-primitive types requires generating code at runtime.")]
        public void Add<T>(T? value) { }
        public void Clear() { }
        public bool Contains(System.Text.Json.Nodes.JsonNode? item) { throw null; }
        public static System.Text.Json.Nodes.JsonArray? Create(System.Text.Json.JsonElement element, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Text.Json.Nodes.JsonNode?> GetEnumerator() { throw null; }
        public int IndexOf(System.Text.Json.Nodes.JsonNode? item) { throw null; }
        public void Insert(int index, System.Text.Json.Nodes.JsonNode? item) { }
        public bool Remove(System.Text.Json.Nodes.JsonNode? item) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.Generic.ICollection<System.Text.Json.Nodes.JsonNode?>.CopyTo(System.Text.Json.Nodes.JsonNode?[]? array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions? options = null) { }
    }
    public abstract partial class JsonNode
    {
        internal JsonNode() { }
        public System.Text.Json.Nodes.JsonNode? this[int index] { get { throw null; } set { } }
        public System.Text.Json.Nodes.JsonNode? this[string propertyName] { get { throw null; } set { } }
        public System.Text.Json.Nodes.JsonNodeOptions? Options { get { throw null; } }
        public System.Text.Json.Nodes.JsonNode? Parent { get { throw null; } }
        public System.Text.Json.Nodes.JsonNode Root { get { throw null; } }
        public System.Text.Json.Nodes.JsonArray AsArray() { throw null; }
        public System.Text.Json.Nodes.JsonObject AsObject() { throw null; }
        public System.Text.Json.Nodes.JsonValue AsValue() { throw null; }
        public string GetPath() { throw null; }
        public virtual T GetValue<T>() { throw null; }
        public static explicit operator bool(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator byte(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator char(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator System.DateTime(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator System.DateTimeOffset(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator decimal(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator double(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator System.Guid(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator short(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator int(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator long(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator bool?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator byte?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator char?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator System.DateTimeOffset?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator System.DateTime?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator decimal?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator double?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator System.Guid?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator short?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator int?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator long?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static explicit operator float?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator float(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator string?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort(System.Text.Json.Nodes.JsonNode value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint(System.Text.Json.Nodes.JsonNode value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(bool value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(byte value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(char value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(System.DateTime value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(System.DateTimeOffset value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(decimal value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(double value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(System.Guid value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(short value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(int value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(long value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(bool? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(byte? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(char? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(System.DateTimeOffset? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(System.DateTime? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(decimal? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(double? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(System.Guid? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(short? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(int? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(long? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode?(sbyte? value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(float? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode?(ushort? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode?(uint? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode?(ulong? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode(sbyte value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode(float value) { throw null; }
        public static implicit operator System.Text.Json.Nodes.JsonNode?(string? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Text.Json.Nodes.JsonNode(ulong value) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? Parse(System.IO.Stream utf8Json, System.Text.Json.Nodes.JsonNodeOptions? nodeOptions = default(System.Text.Json.Nodes.JsonNodeOptions?), System.Text.Json.JsonDocumentOptions documentOptions = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? Parse(System.ReadOnlySpan<byte> utf8Json, System.Text.Json.Nodes.JsonNodeOptions? nodeOptions = default(System.Text.Json.Nodes.JsonNodeOptions?), System.Text.Json.JsonDocumentOptions documentOptions = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? Parse([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Json")] string json, System.Text.Json.Nodes.JsonNodeOptions? nodeOptions = default(System.Text.Json.Nodes.JsonNodeOptions?), System.Text.Json.JsonDocumentOptions documentOptions = default(System.Text.Json.JsonDocumentOptions)) { throw null; }
        public static System.Text.Json.Nodes.JsonNode? Parse(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.Nodes.JsonNodeOptions? nodeOptions = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public string ToJsonString(System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public override string ToString() { throw null; }
        public abstract void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions? options = null);
    }
    public partial struct JsonNodeOptions
    {
        private int _dummyPrimitive;
        public bool PropertyNameCaseInsensitive { readonly get { throw null; } set { } }
    }
    public sealed partial class JsonObject : System.Text.Json.Nodes.JsonNode, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>, System.Collections.Generic.IDictionary<string, System.Text.Json.Nodes.JsonNode?>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>, System.Collections.IEnumerable
    {
        public JsonObject(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>> properties, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { }
        public JsonObject(System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { }
        public int Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>.IsReadOnly { get { throw null; } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<string, System.Text.Json.Nodes.JsonNode?>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<System.Text.Json.Nodes.JsonNode?> System.Collections.Generic.IDictionary<string, System.Text.Json.Nodes.JsonNode?>.Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> property) { }
        public void Add(string propertyName, System.Text.Json.Nodes.JsonNode? value) { }
        public void Clear() { }
        public bool ContainsKey(string propertyName) { throw null; }
        public static System.Text.Json.Nodes.JsonObject? Create(System.Text.Json.JsonElement element, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>> GetEnumerator() { throw null; }
        public bool Remove(string propertyName) { throw null; }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>.Contains(System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>.CopyTo(System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode?>>.Remove(System.Collections.Generic.KeyValuePair<string, System.Text.Json.Nodes.JsonNode> item) { throw null; }
        bool System.Collections.Generic.IDictionary<string, System.Text.Json.Nodes.JsonNode?>.TryGetValue(string propertyName, out System.Text.Json.Nodes.JsonNode? jsonNode) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetPropertyValue(string propertyName, out System.Text.Json.Nodes.JsonNode? jsonNode) { throw null; }
        public override void WriteTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions? options = null) { }
    }
    public abstract partial class JsonValue : System.Text.Json.Nodes.JsonNode
    {
        private protected JsonValue(System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(bool value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(byte value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(char value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(System.DateTime value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(System.DateTimeOffset value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(decimal value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(double value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(System.Guid value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(short value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(int value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(long value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(bool? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(byte? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(char? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.DateTimeOffset? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.DateTime? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(decimal? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(double? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.Guid? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(short? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(int? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(long? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue? Create(sbyte? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(float? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.Text.Json.JsonElement? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue? Create(ushort? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue? Create(uint? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue? Create(ulong? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue Create(sbyte value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue Create(float value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(string? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.Text.Json.JsonElement value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue Create(ushort value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue Create(uint value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue Create(ulong value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create<T>(T? value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("Creating JsonValue instances with non-primitive types requires generating code at runtime.")]
        public static System.Text.Json.Nodes.JsonValue? Create<T>(T? value, System.Text.Json.Nodes.JsonNodeOptions? options = default(System.Text.Json.Nodes.JsonNodeOptions?)) { throw null; }
        public abstract bool TryGetValue<T>([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out T? value);
    }
}
namespace System.Text.Json.Serialization
{
    public partial interface IJsonOnDeserialized
    {
        void OnDeserialized();
    }
    public partial interface IJsonOnDeserializing
    {
        void OnDeserializing();
    }
    public partial interface IJsonOnSerialized
    {
        void OnSerialized();
    }
    public partial interface IJsonOnSerializing
    {
        void OnSerializing();
    }
    public abstract partial class JsonAttribute : System.Attribute
    {
        protected JsonAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed partial class JsonConstructorAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonConstructorAttribute() { }
    }
    public abstract partial class JsonConverter
    {
        internal JsonConverter() { }
        public abstract bool CanConvert(System.Type typeToConvert);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Enum | System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple = false)]
    public partial class JsonConverterAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        protected JsonConverterAttribute() { }
        public JsonConverterAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type converterType) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public System.Type? ConverterType { get { throw null; } }
        public virtual System.Text.Json.Serialization.JsonConverter? CreateConverter(System.Type typeToConvert) { throw null; }
    }
    public abstract partial class JsonConverterFactory : System.Text.Json.Serialization.JsonConverter
    {
        protected JsonConverterFactory() { }
        public abstract System.Text.Json.Serialization.JsonConverter? CreateConverter(System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);
    }
    public abstract partial class JsonConverter<T> : System.Text.Json.Serialization.JsonConverter
    {
        protected internal JsonConverter() { }
        public virtual bool HandleNull { get { throw null; } }
        public override bool CanConvert(System.Type typeToConvert) { throw null; }
        public abstract T? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);
        public virtual T ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
        public abstract void Write(
            System.Text.Json.Utf8JsonWriter writer,
#nullable disable
            T value,
#nullable restore
            System.Text.Json.JsonSerializerOptions options);
        public virtual void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public partial class JsonDerivedTypeAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonDerivedTypeAttribute(System.Type derivedType) { }
        public JsonDerivedTypeAttribute(System.Type derivedType, int typeDiscriminator) { }
        public JsonDerivedTypeAttribute(System.Type derivedType, string typeDiscriminator) { }
        public System.Type DerivedType { get { throw null; } }
        public object? TypeDiscriminator { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed partial class JsonExtensionDataAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonExtensionDataAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed partial class JsonIgnoreAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonIgnoreAttribute() { }
        public System.Text.Json.Serialization.JsonIgnoreCondition Condition { get { throw null; } set { } }
    }
    public enum JsonIgnoreCondition
    {
        Never = 0,
        Always = 1,
        WhenWritingDefault = 2,
        WhenWritingNull = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed partial class JsonIncludeAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonIncludeAttribute() { }
    }
    public enum JsonKnownNamingPolicy
    {
        Unspecified = 0,
        CamelCase = 1,
    }
    [System.FlagsAttribute]
    public enum JsonNumberHandling
    {
        Strict = 0,
        AllowReadingFromString = 1,
        WriteAsString = 2,
        AllowNamedFloatingPointLiterals = 4,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple = false)]
    public sealed partial class JsonNumberHandlingAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonNumberHandlingAttribute(System.Text.Json.Serialization.JsonNumberHandling handling) { }
        public System.Text.Json.Serialization.JsonNumberHandling Handling { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed partial class JsonPolymorphicAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPolymorphicAttribute() { }
        public string? TypeDiscriminatorPropertyName { get { throw null; } set { } }
        public bool IgnoreUnrecognizedTypeDiscriminators { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed partial class JsonPropertyNameAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPropertyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed partial class JsonPropertyOrderAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPropertyOrderAttribute(int order) { }
        public int Order { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed partial class JsonSerializableAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonSerializableAttribute(System.Type type) { }
        public string? TypeInfoPropertyName { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonSourceGenerationMode GenerationMode { get { throw null; } set { } }
    }
    public abstract partial class JsonSerializerContext
    {
        protected JsonSerializerContext(System.Text.Json.JsonSerializerOptions? options) { }
        protected abstract System.Text.Json.JsonSerializerOptions? GeneratedSerializerOptions { get; }
        public System.Text.Json.JsonSerializerOptions Options { get { throw null; } }
        public abstract System.Text.Json.Serialization.Metadata.JsonTypeInfo? GetTypeInfo(System.Type type);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple = false)]
    public sealed partial class JsonSourceGenerationOptionsAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonSourceGenerationOptionsAttribute() { }
        public System.Text.Json.Serialization.JsonIgnoreCondition DefaultIgnoreCondition { get { throw null; } set { } }
        public bool IgnoreReadOnlyFields { get { throw null; } set { } }
        public bool IgnoreReadOnlyProperties { get { throw null; } set { } }
        public bool IncludeFields { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonKnownNamingPolicy PropertyNamingPolicy { get { throw null; } set { } }
        public bool WriteIndented { get { throw null; } set { } }
        public System.Text.Json.Serialization.JsonSourceGenerationMode GenerationMode { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum JsonSourceGenerationMode
    {
        Default = 0,
        Metadata = 1,
        Serialization = 2,
    }
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    public partial class JsonStringEnumConverter : System.Text.Json.Serialization.JsonConverterFactory
    {
        public JsonStringEnumConverter() { }
        public JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true) { }
        public sealed override bool CanConvert(System.Type typeToConvert) { throw null; }
        public sealed override System.Text.Json.Serialization.JsonConverter CreateConverter(System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
    }
    public enum JsonUnknownDerivedTypeHandling
    {
        FailSerialization = 0,
        FallBackToBaseType = 1,
        FallBackToNearestAncestor = 2
    }
    public enum JsonUnknownTypeHandling
    {
        JsonElement = 0,
        JsonNode = 1,
    }
    public partial class JsonPolymorphicTypeConfiguration : System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>, System.Collections.Generic.IEnumerable<(System.Type DerivedType, object? TypeDiscriminator)>, System.Collections.IEnumerable
    {
        public JsonPolymorphicTypeConfiguration(System.Type baseType) { }
        public System.Type BaseType { get { throw null; } }
        public string? TypeDiscriminatorPropertyName { get { throw null; } set { } }
        public bool IgnoreUnrecognizedTypeDiscriminators { get { throw null; } set { } }
        int System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.Count { get { throw null; } }
        public System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.IsReadOnly { get { throw null; } }
        void System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.Add((System.Type DerivedType, object? TypeDiscriminator) item) { }
        void System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.Clear() { }
        bool System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.Contains((System.Type DerivedType, object? TypeDiscriminator) item) { throw null; }
        void System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.CopyTo((System.Type DerivedType, object? TypeDiscriminator)[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<(System.Type DerivedType, object? TypeDiscriminator)>.Remove((System.Type DerivedType, object? TypeDiscriminator) item) { throw null; }
        System.Collections.Generic.IEnumerator<(System.Type DerivedType, object? TypeDiscriminator)> System.Collections.Generic.IEnumerable<(System.Type DerivedType, object? TypeDiscriminator)>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration WithDerivedType(System.Type derivedType) { throw null; }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration WithDerivedType(System.Type derivedType, int typeDiscriminator) { throw null; }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration WithDerivedType(System.Type derivedType, string typeDiscriminator) { throw null; }
    }
    public partial class JsonPolymorphicTypeConfiguration<TBaseType> : System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration where TBaseType : class
    {
        public JsonPolymorphicTypeConfiguration() : base(default(System.Type)) { }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>() where TDerivedType : TBaseType { throw null; }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>(int typeDiscriminator) where TDerivedType : TBaseType { throw null; }
        public System.Text.Json.Serialization.JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>(string typeDiscriminator) where TDerivedType : TBaseType { throw null; }
    }
    public abstract partial class ReferenceHandler
    {
        protected ReferenceHandler() { }
        public static System.Text.Json.Serialization.ReferenceHandler IgnoreCycles { get { throw null; } }
        public static System.Text.Json.Serialization.ReferenceHandler Preserve { get { throw null; } }
        public abstract System.Text.Json.Serialization.ReferenceResolver CreateResolver();
    }
    public sealed partial class ReferenceHandler<T> : System.Text.Json.Serialization.ReferenceHandler where T : System.Text.Json.Serialization.ReferenceResolver, new()
    {
        public ReferenceHandler() { }
        public override System.Text.Json.Serialization.ReferenceResolver CreateResolver() { throw null; }
    }
    public abstract partial class ReferenceResolver
    {
        protected ReferenceResolver() { }
        public abstract void AddReference(string referenceId, object value);
        public abstract string GetReference(object value, out bool alreadyExists);
        public abstract object ResolveReference(string referenceId);
    }
}
namespace System.Text.Json.Serialization.Metadata
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class JsonCollectionInfoValues<TCollection>
    {
        public JsonCollectionInfoValues() { }
        public System.Func<TCollection>? ObjectCreator { get { throw null; } init { } }
        public System.Text.Json.Serialization.Metadata.JsonTypeInfo ElementInfo { get { throw null; } init { } }
        public System.Text.Json.Serialization.Metadata.JsonTypeInfo? KeyInfo { get { throw null; } init { } }
        public System.Text.Json.Serialization.JsonNumberHandling NumberHandling { get { throw null; } init { } }
        public System.Action<System.Text.Json.Utf8JsonWriter, TCollection>? SerializeHandler { get { throw null; } init { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class JsonMetadataServices
    {
        public static System.Text.Json.Serialization.JsonConverter<bool> BooleanConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<byte[]> ByteArrayConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<byte> ByteConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<char> CharConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.DateTime> DateTimeConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.DateTimeOffset> DateTimeOffsetConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<decimal> DecimalConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<double> DoubleConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Guid> GuidConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<short> Int16Converter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<int> Int32Converter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<long> Int64Converter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.Nodes.JsonArray> JsonArrayConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.JsonElement> JsonElementConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.Nodes.JsonNode> JsonNodeConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.Nodes.JsonObject> JsonObjectConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.Nodes.JsonValue> JsonValueConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Text.Json.JsonDocument> JsonDocumentConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<object> ObjectConverter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<sbyte> SByteConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<float> SingleConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<string> StringConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.TimeSpan> TimeSpanConverter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<ushort> UInt16Converter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<uint> UInt32Converter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<ulong> UInt64Converter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Uri> UriConverter { get { throw null; } }
        public static System.Text.Json.Serialization.JsonConverter<System.Version> VersionConverter { get { throw null; } }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TElement[]> collectionInfo) { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateConcurrentQueueInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Concurrent.ConcurrentQueue<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateConcurrentStackInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Concurrent.ConcurrentStack<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.Dictionary<TKey, TValue> where TKey : notnull { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIAsyncEnumerableInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.IAsyncEnumerable<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateICollectionInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.ICollection<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.IDictionary { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection, TKey, TValue>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.IDictionary<TKey, TValue> where TKey : notnull { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.IEnumerable { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.IEnumerable<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIListInfo<TCollection>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.IList { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIListInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.IList<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateImmutableDictionaryInfo<TCollection, TKey, TValue>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo, System.Func<System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, TCollection> createRangeFunc) where TCollection : System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> where TKey : notnull { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateImmutableEnumerableInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo, System.Func<System.Collections.Generic.IEnumerable<TElement>, TCollection> createRangeFunc) where TCollection : System.Collections.Generic.IEnumerable<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateIReadOnlyDictionaryInfo<TCollection, TKey, TValue>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> where TKey : notnull { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateISetInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.ISet<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.List<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> CreateObjectInfo<T>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonObjectInfoValues<T> objectInfo) where T : notnull { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonPropertyInfo CreatePropertyInfo<T>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues<T> propertyInfo) { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateQueueInfo<TCollection>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo, System.Action<TCollection, object?> addFunc) where TCollection : System.Collections.IEnumerable { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateQueueInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.Queue<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateStackInfo<TCollection>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo, System.Action<TCollection, object?> addFunc) where TCollection : System.Collections.IEnumerable { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<TCollection> CreateStackInfo<TCollection, TElement>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : System.Collections.Generic.Stack<TElement> { throw null; }
        public static System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> CreateValueInfo<T>(System.Text.Json.JsonSerializerOptions options, System.Text.Json.Serialization.JsonConverter converter) { throw null; }
        public static System.Text.Json.Serialization.JsonConverter<T> GetUnsupportedTypeConverter<T>() { throw null; }
        public static System.Text.Json.Serialization.JsonConverter<T> GetEnumConverter<T>(System.Text.Json.JsonSerializerOptions options) where T : struct { throw null; }
        public static System.Text.Json.Serialization.JsonConverter<T?> GetNullableConverter<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> underlyingTypeInfo) where T : struct { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class JsonObjectInfoValues<T>
    {
        public JsonObjectInfoValues() { }
        public System.Func<System.Text.Json.Serialization.Metadata.JsonParameterInfoValues[]>? ConstructorParameterMetadataInitializer { get { throw null; } init { } }
        public System.Text.Json.Serialization.JsonNumberHandling NumberHandling { get { throw null; } init { } }
        public System.Func<T>? ObjectCreator { get { throw null; } init { } }
        public System.Func<object[], T>? ObjectWithParameterizedConstructorCreator { get { throw null; } init { } }
        public System.Func<System.Text.Json.Serialization.JsonSerializerContext, System.Text.Json.Serialization.Metadata.JsonPropertyInfo[]>? PropertyMetadataInitializer { get { throw null; } init { } }
        public System.Action<System.Text.Json.Utf8JsonWriter, T>? SerializeHandler { get { throw null; } init { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class JsonParameterInfoValues
    {
        public JsonParameterInfoValues() { }
        public object? DefaultValue { get { throw null; } init { } }
        public bool HasDefaultValue { get { throw null; } init { } }
        public string Name { get { throw null; } init { } }
        public System.Type ParameterType { get { throw null; } init { } }
        public int Position { get { throw null; } init { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class JsonPropertyInfo
    {
        internal JsonPropertyInfo() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class JsonPropertyInfoValues<T>
    {
        public JsonPropertyInfoValues() { }
        public System.Text.Json.Serialization.JsonConverter<T>? Converter { get { throw null; } init { } }
        public System.Type DeclaringType { get { throw null; } init { } }
        public System.Func<object, T?>? Getter { get { throw null; } init { } }
        public System.Text.Json.Serialization.JsonIgnoreCondition? IgnoreCondition { get { throw null; } init { } }
        public bool HasJsonInclude { get { throw null; } init { } }
        public bool IsExtensionData { get { throw null; } init { } }
        public bool IsProperty { get { throw null; } init { } }
        public bool IsPublic { get { throw null; } init { } }
        public bool IsVirtual { get { throw null; } init { } }
        public string? JsonPropertyName { get { throw null; } init { } }
        public System.Text.Json.Serialization.JsonNumberHandling? NumberHandling { get { throw null; } init { } }
        public string PropertyName { get { throw null; } init { } }
        public System.Text.Json.Serialization.Metadata.JsonTypeInfo PropertyTypeInfo { get { throw null; } init { } }
        public System.Action<object, T?>? Setter { get { throw null; } init { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class JsonTypeInfo
    {
        internal JsonTypeInfo() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public abstract partial class JsonTypeInfo<T> : System.Text.Json.Serialization.Metadata.JsonTypeInfo
    {
        internal JsonTypeInfo() { }
        public System.Action<System.Text.Json.Utf8JsonWriter, T>? SerializeHandler { get { throw null; } }
    }
}
