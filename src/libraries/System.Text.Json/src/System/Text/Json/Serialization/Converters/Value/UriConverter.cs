// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UriConverter : JsonPrimitiveConverter<Uri?>
    {
        public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType is JsonTokenType.Null ? null : ReadCore(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, Uri? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.OriginalString);
        }

        internal override Uri ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            return ReadCore(ref reader);
        }

        private static Uri ReadCore(ref Utf8JsonReader reader)
        {
            string? uriString = reader.GetString();

            if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out Uri? value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            writer.WritePropertyName(value.OriginalString);
        }
    }
}
