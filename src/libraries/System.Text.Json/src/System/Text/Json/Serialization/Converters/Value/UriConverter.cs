// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UriConverter : JsonPrimitiveConverter<Uri>
    {
        public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? uriString = reader.GetString();
            if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out Uri? value))
            {
                return value;
            }

            ThrowHelper.ThrowJsonException();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.OriginalString);
        }

        internal override Uri ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName);
            return Read(ref reader, typeToConvert, options);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value.OriginalString);
        }
    }
}
