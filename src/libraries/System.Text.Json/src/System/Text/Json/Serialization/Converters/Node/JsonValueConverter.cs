// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverter : JsonConverter<JsonValue?>
    {
        public override void Write(Utf8JsonWriter writer, JsonValue? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer, options);
        }

        public override JsonValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            // For Object or Array types, return a JsonObject or JsonArray instance
            // when deserializing a JsonValue. This maintains compatibility with .NET 8.0.
            if (reader.TokenType is JsonTokenType.StartObject)
            {
                return JsonNodeConverter.ObjectConverter.Read(ref reader, typeof(JsonObject), options) as JsonValue;
            }
            
            if (reader.TokenType is JsonTokenType.StartArray)
            {
                return JsonNodeConverter.ArrayConverter.Read(ref reader, typeof(JsonArray), options) as JsonValue;
            }

            JsonElement element = JsonElement.ParseValue(ref reader);
            return JsonValue.CreateFromElement(ref element, options.GetNodeOptions());
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
