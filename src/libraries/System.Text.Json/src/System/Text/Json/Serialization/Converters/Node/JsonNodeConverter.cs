// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Node;

namespace System.Text.Json.Serialization.Converters
{
    internal class JsonNodeConverter : JsonConverter<object>
    {
        public static JsonNodeConverter Instance { get; } = new JsonNodeConverter();
        public JsonArrayConverter ArrayConverter { get; } = new JsonArrayConverter();
        public JsonObjectConverter ObjectConverter { get; } = new JsonObjectConverter();
        public JsonValueConverter ValueConverter { get; } = new JsonValueConverter();

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                if (value is JsonObject jsonObject)
                {
                    ObjectConverter.Write(writer, jsonObject, options);
                }
                else if (value is JsonArray jsonArray)
                {
                    ArrayConverter.Write(writer, (JsonArray)value, options);
                }
                else
                {
                    Debug.Assert(value is JsonValue);
                    ValueConverter.Write(writer, (JsonValue)value, options);
                }
            }
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return ValueConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartArray:
                    return ArrayConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartObject:
                    return ObjectConverter.Read(ref reader, typeToConvert, options);
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
                    node = new JsonValue<JsonElement>(element, options);
                    break;
            }

            return node;
        }
    }
}
