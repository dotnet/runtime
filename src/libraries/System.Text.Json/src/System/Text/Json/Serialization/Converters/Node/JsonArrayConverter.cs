// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonArrayConverter : JsonConverter<JsonArray?>
    {
        public override void Write(Utf8JsonWriter writer, JsonArray? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer, options);
        }

        public override JsonArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    return options.AllowDuplicateProperties
                        ? ReadLazy(ref reader, options.GetNodeOptions())
                        : ReadEager(ref reader, options.GetNodeOptions());
                case JsonTokenType.Null:
                    return null;
                default:
                    Debug.Assert(false);
                    throw ThrowHelper.GetInvalidOperationException_ExpectedArray(reader.TokenType);
            }
        }

        internal static JsonArray ReadLazy(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            return new JsonArray(jElement, options);
        }

        internal static JsonArray ReadEager(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.StartArray);

            JsonArray jArray = new JsonArray(options);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return jArray;
                }

                JsonNode? item = JsonNodeConverter.ReadEager(ref reader, options);
                jArray.Add(item);
            }

            // JSON is invalid so reader would have already thrown.
            Debug.Fail("End array token not found.");
            throw new JsonException();
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.Array };
    }
}
