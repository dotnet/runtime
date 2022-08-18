// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json
{
    public ref partial struct Utf8JsonReader
    {
        public System.Int128 GetInt128() { throw null; }
        public bool TryGetInt128(out System.Int128 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.UInt128 GetUInt128() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt128(out System.UInt128 value) { throw null; }
    }
    public sealed partial class Utf8JsonWriter : System.IAsyncDisposable, System.IDisposable
    {
        public void WriteNumberValue(System.Int128 value) { }
        [System.CLSCompliantAttribute(false)]
        public void WriteNumberValue(System.UInt128 value) { }
    }
    public readonly partial struct JsonElement
    {
        public System.Int128 GetInt128() { throw null; }
        public bool TryGetInt128(out System.Int128 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.UInt128 GetUInt128() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt128(out System.UInt128 value) { throw null; }
    }
}
namespace System.Text.Json.Nodes
{
    public abstract partial class JsonNode
    {
        public static explicit operator System.Int128(System.Text.Json.Nodes.JsonNode value) { throw null; }
        public static explicit operator System.Int128?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.UInt128(System.Text.Json.Nodes.JsonNode value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.UInt128?(System.Text.Json.Nodes.JsonNode? value) { throw null; }
        public static implicit operator JsonNode(System.Int128 value) { throw null; }
        public static implicit operator JsonNode?(System.Int128? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(System.UInt128 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode?(System.UInt128? value) { throw null; }
    }
    public abstract partial class JsonValue
    {
        public static System.Text.Json.Nodes.JsonValue Create(System.Int128 value, System.Text.Json.Nodes.JsonNodeOptions? options = null) { throw null; }
        public static System.Text.Json.Nodes.JsonValue? Create(System.Int128? value, System.Text.Json.Nodes.JsonNodeOptions? options = null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue Create(System.UInt128 value, System.Text.Json.Nodes.JsonNodeOptions? options = null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Nodes.JsonValue? Create(System.UInt128? value, System.Text.Json.Nodes.JsonNodeOptions? options = null) { throw null; }
    }
}
namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        public static System.Text.Json.Serialization.JsonConverter<System.Int128> Int128Converter { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public static System.Text.Json.Serialization.JsonConverter<System.UInt128> UInt128Converter { get { throw null; } }
    }
}
