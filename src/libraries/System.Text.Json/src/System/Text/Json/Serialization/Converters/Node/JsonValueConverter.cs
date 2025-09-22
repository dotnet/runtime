// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverter<T> : JsonConverter<T?>
        where T : JsonValue
    {
        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer, options);
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonValue? rawValue;

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
                    rawValue = ReadNonNullPrimitiveValue(ref reader, options.GetNodeOptions());
                    break;
                default:
                    JsonElement element = JsonElement.ParseValue(ref reader, options.AllowDuplicateProperties);
                    rawValue = JsonValue.CreateFromElement(ref element, options.GetNodeOptions());
                    break;
            }

            switch (rawValue)
            {
                case null:
                {
                    return null;
                }
                case T valueOfT:
                {
                    return valueOfT;
                }
                default:
                {
                    ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(typeOfValue: rawValue.GetType(), typeof(T));
                    return default;
                }
            }
        }

        internal static JsonValue ReadNonNullPrimitiveValue(ref Utf8JsonReader reader, JsonNodeOptions options)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.False or JsonTokenType.True or JsonTokenType.Number);
            return JsonValueOfJsonPrimitive.CreatePrimitiveValue(ref reader, options);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
