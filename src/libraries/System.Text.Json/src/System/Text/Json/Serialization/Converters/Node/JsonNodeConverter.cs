// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for JsonNode-derived types. The {T} value must be Object and not JsonNode
    /// since we allow Object-declared members\variables to deserialize as {JsonNode}.
    /// </summary>
    internal sealed class JsonNodeConverter : JsonConverter<JsonNode?>
    {
        private static JsonNodeConverter? s_nodeConverter;
        private static JsonArrayConverter? s_arrayConverter;
        private static JsonObjectConverter? s_objectConverter;
        private static JsonValueConverter? s_valueConverter;

        public static JsonNodeConverter Instance => s_nodeConverter ??= new JsonNodeConverter();
        public static JsonArrayConverter ArrayConverter => s_arrayConverter ??= new JsonArrayConverter();
        public static JsonObjectConverter ObjectConverter => s_objectConverter ??= new JsonObjectConverter();
        public static JsonValueConverter ValueConverter => s_valueConverter ??= new JsonValueConverter();

        public override void Write(Utf8JsonWriter writer, JsonNode? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                if (value is JsonValue jsonValue)
                {
                    ValueConverter.Write(writer, jsonValue, options);
                }
                else if (value is JsonObject jsonObject)
                {
                    ObjectConverter.Write(writer, jsonObject, options);
                }
                else
                {
                    Debug.Assert(value is JsonArray);
                    ArrayConverter.Write(writer, (JsonArray)value, options);
                }
            }
        }

        public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return ValueConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartObject:
                    return ObjectConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartArray:
                    return ArrayConverter.Read(ref reader, typeToConvert, options);
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
                    node = new JsonValueTrimmable<JsonElement>(element, JsonMetadataServices.JsonElementConverter, options);
                    break;
            }

            return node;
        }
    }
}
