// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class CharConverter : JsonPrimitiveConverter<char>
    {
        private const int MaxEscapedCharacterLength = JsonConstants.MaxExpansionFactorWhileEscaping;

        public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, 1, MaxEscapedCharacterLength))
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedChar(reader.TokenType);
            }

            Span<char> buffer = stackalloc char[MaxEscapedCharacterLength];
            int charsWritten = reader.CopyString(buffer);

            if (charsWritten != 1)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedChar(reader.TokenType);
            }

            return buffer[0];
        }

        public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(
#if NET
                new ReadOnlySpan<char>(in value)
#else
                value.ToString()
#endif
                );
        }

        internal override char ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return Read(ref reader, typeToConvert, options);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, char value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(
#if NET
                new ReadOnlySpan<char>(in value)
#else
                value.ToString()
#endif
                );
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) =>
            new() { Type = JsonSchemaType.String, MinLength = 1, MaxLength = 1 };
    }
}
