// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for JsonNode-derived types. The {T} value must be Object and not JsonNode
    /// since we allow Object-declared members\variables to deserialize as {JsonNode}.
    /// </summary>
    internal sealed class JsonNodeConverter : JsonConverter<JsonNode?>
    {
        internal static JsonNodeConverter Instance { get; } = new JsonNodeConverter();

        public override void Write(Utf8JsonWriter writer, JsonNode? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                value.WriteTo(writer, options);
            }
        }

        public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return options.AllowDuplicateProperties
                ? ReadAsJsonElement(ref reader, options.GetNodeOptions())
                : ReadAsJsonNode(ref reader, options.GetNodeOptions());
        }

        internal static JsonNode? ReadAsJsonElement(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return JsonValueConverter.ReadNonNullPrimitiveValue(ref reader, options);
                case JsonTokenType.StartObject:
                    return JsonObjectConverter.ReadAsJsonElement(ref reader, options);
                case JsonTokenType.StartArray:
                    return JsonArrayConverter.ReadAsJsonElement(ref reader, options);
                case JsonTokenType.Null:
                    return null;
                default:
                    Debug.Assert(false);
                    throw new JsonException();
            }
        }

        internal static JsonNode? ReadAsJsonNode(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return JsonValueConverter.ReadNonNullPrimitiveValue(ref reader, options);
                case JsonTokenType.StartObject:
                    return JsonObjectConverter.ReadAsJsonNode(ref reader, options);
                case JsonTokenType.StartArray:
                    return JsonArrayConverter.ReadAsJsonNode(ref reader, options);
                case JsonTokenType.Null:
                    return null;
                default:
                    Debug.Assert(false);
                    throw new JsonException();
            }
        }

        public static JsonNode? Create(JsonElement element, JsonNodeOptions? options)
        {
            JsonNode? node;

            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    node = null;
                    break;
                case JsonValueKind.Object:
                    node = new JsonObject(element, options);
                    break;
                case JsonValueKind.Array:
                    node = new JsonArray(element, options);
                    break;
                default:
                    node = new JsonValueOfElement(element, options);
                    break;
            }

            return node;
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
