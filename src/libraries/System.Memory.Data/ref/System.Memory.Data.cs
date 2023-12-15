// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    [System.Text.Json.Serialization.JsonConverterAttribute(typeof(System.Text.Json.Serialization.BinaryDataJsonConverter))]
    public partial class BinaryData
    {
        public BinaryData(byte[] data) { }
        public BinaryData(byte[] data, string? mediaType) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
        public BinaryData(object? jsonSerializable, System.Text.Json.JsonSerializerOptions? options = null, System.Type? type = null) { }
        public BinaryData(object? jsonSerializable, System.Text.Json.Serialization.JsonSerializerContext context, System.Type? type = null) { }
        public BinaryData(System.ReadOnlyMemory<byte> data) { }
        public BinaryData(System.ReadOnlyMemory<byte> data, string? mediaType) { }
        public BinaryData(string data) { }
        public BinaryData(string data, string? mediaType) { }
        public static System.BinaryData Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public string? MediaType { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public static System.BinaryData FromBytes(byte[] data) { throw null; }
        public static System.BinaryData FromBytes(byte[] data, string? mediaType) { throw null; }
        public static System.BinaryData FromBytes(System.ReadOnlyMemory<byte> data) { throw null; }
        public static System.BinaryData FromBytes(System.ReadOnlyMemory<byte> data, string? mediaType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
        public static System.BinaryData FromObjectAsJson<T>(T jsonSerializable, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.BinaryData FromObjectAsJson<T>(T jsonSerializable, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) { throw null; }
        public static System.BinaryData FromStream(System.IO.Stream stream) { throw null; }
        public static System.BinaryData FromStream(System.IO.Stream stream, string? mediaType) { throw null; }
        public static System.Threading.Tasks.Task<System.BinaryData> FromStreamAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.BinaryData> FromStreamAsync(System.IO.Stream stream, string? mediaType, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.BinaryData FromString(string data) { throw null; }
        public static System.BinaryData FromString(string data, string? mediaType) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.ReadOnlyMemory<byte> (System.BinaryData? data) { throw null; }
        public static implicit operator System.ReadOnlySpan<byte> (System.BinaryData? data) { throw null; }
        public byte[] ToArray() { throw null; }
        public System.ReadOnlyMemory<byte> ToMemory() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
        public T? ToObjectFromJson<T>(System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public T? ToObjectFromJson<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) { throw null; }
        public System.IO.Stream ToStream() { throw null; }
        public override string ToString() { throw null; }
        public System.BinaryData WithMediaType(string? mediaType) { throw null; }
    }
}
namespace System.Text.Json.Serialization
{
    public sealed partial class BinaryDataJsonConverter : System.Text.Json.Serialization.JsonConverter<System.BinaryData>
    {
        public BinaryDataJsonConverter() { }
        public override System.BinaryData? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
        public override void Write(System.Text.Json.Utf8JsonWriter writer, System.BinaryData value, System.Text.Json.JsonSerializerOptions options) { }
    }
}
