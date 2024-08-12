// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Inherited by built-in converters serializing types as JSON primitives that support property name serialization.
    /// </summary>
    internal abstract class JsonPrimitiveConverter<T> : JsonConverter<T>
    {
        public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
        }

        public sealed override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedPropertyName(reader.TokenType);
            }

            return ReadAsPropertyNameCore(ref reader, typeToConvert, options);
        }

        private protected static JsonSchema GetSchemaForNumericType(JsonSchemaType schemaType, JsonNumberHandling numberHandling, bool isIeeeFloatingPoint = false)
        {
            Debug.Assert(schemaType is JsonSchemaType.Integer or JsonSchemaType.Number);
            Debug.Assert(!isIeeeFloatingPoint || schemaType is JsonSchemaType.Number);
#if NET
            Debug.Assert(isIeeeFloatingPoint == (typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(Half)));
#endif
            string? pattern = null;

            if ((numberHandling & (JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)) != 0)
            {
                pattern = schemaType is JsonSchemaType.Integer
                    ? @"^-?(?:0|[1-9]\d*)$"
                    : isIeeeFloatingPoint
                        ? @"^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$"
                        : @"^-?(?:0|[1-9]\d*)(?:\.\d+)?$";

                schemaType |= JsonSchemaType.String;
            }

            if (isIeeeFloatingPoint && (numberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0)
            {
                return new JsonSchema
                {
                    AnyOf =
                    [
                        new JsonSchema { Type = schemaType, Pattern = pattern },
                        new JsonSchema { Enum = [(JsonNode)"NaN", (JsonNode)"Infinity", (JsonNode)"-Infinity"] },
                    ]
                };
            }

            return new JsonSchema { Type = schemaType, Pattern = pattern };
        }
    }
}
