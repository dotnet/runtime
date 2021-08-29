// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class CharConverter : JsonConverter<char>
    {
        public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str) || str.Length > 1)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedChar(reader.TokenType);
            }
            return str[0];
        }

        public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(
#if BUILDING_INBOX_LIBRARY
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
#if BUILDING_INBOX_LIBRARY
                MemoryMarshal.CreateSpan(ref value, 1)
#else
                value.ToString()
#endif
                );
        }
    }
}
