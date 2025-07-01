// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

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

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return ReadNonNullPrimitiveValue(ref reader, options.GetNodeOptions());
                default:
                    JsonElement element = JsonElement.ParseValue(ref reader, options.AllowDuplicateProperties);
                    return JsonValue.CreateFromElement(ref element, options.GetNodeOptions());
            }
        }

        internal static JsonValue ReadNonNullPrimitiveValue(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.False or JsonTokenType.True or JsonTokenType.Number);

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return JsonValue.Create(reader.GetString()!, options);
                case JsonTokenType.False:
                    return JsonValue.Create(false, options);
                case JsonTokenType.True:
                    return JsonValue.Create(true, options);
                case JsonTokenType.Number:
                    // We can't infer CLR type for the number, so we parse it as a JsonElement.
                    JsonElement element = JsonElement.ParseValue(ref reader);
                    return JsonValue.CreateFromElement(ref element, options)!;
                default:
                    Debug.Fail("Unexpected token type for primitive value.");
                    throw new JsonException();
            }
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
