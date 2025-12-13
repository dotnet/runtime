// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Extensions;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonPrimitiveConverter<Version?>
    {
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            return reader.GetVersion();
        }

        public override void Write(Utf8JsonWriter writer, Version? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

#if NET
            Span<byte> span = stackalloc byte[JsonConstants.MaximumVersionLength];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully && charsWritten >= JsonConstants.MinimumVersionLength);
            writer.WriteStringValue(span.Slice(0, charsWritten));
#else
            writer.WriteStringValue(value.ToString());
#endif
        }

        internal override Version ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetVersion();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Version value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            ArgumentNullException.ThrowIfNull(value);

#if NET
            Span<byte> span = stackalloc byte[JsonConstants.MaximumVersionLength];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully && charsWritten >= JsonConstants.MinimumVersionLength);
            writer.WritePropertyName(span.Slice(0, charsWritten));
#else
            writer.WritePropertyName(value.ToString());
#endif
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) =>
            new()
            {
                Type = JsonSchemaType.String,
                Comment = "Represents a version string.",
                Pattern = @"^\d+(\.\d+){1,3}$",
            };
    }
}
