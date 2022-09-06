// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class CharConverter : JsonConverter<char>
    {
        private const int MaxEscapedCharacterLength = JsonConstants.MaxExpansionFactorWhileEscaping;

        public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
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
#if NETCOREAPP
                MemoryMarshal.CreateSpan(ref value, 1)
#else
                value.ToString()
#endif
                );
        }

        internal override char ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Read(ref reader, typeToConvert, options);

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, char value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(
#if NETCOREAPP
                MemoryMarshal.CreateSpan(ref value, 1)
#else
                value.ToString()
#endif
                );
        }
    }
}
