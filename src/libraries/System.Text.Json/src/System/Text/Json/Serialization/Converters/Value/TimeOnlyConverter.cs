// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json.Extensions;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class TimeOnlyConverter : JsonPrimitiveConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            return reader.GetTimeOnly();
        }

        internal override TimeOnly ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetTimeOnly();
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            Span<byte> output = stackalloc byte[JsonConstants.MaximumTimeOnlyFormatLength];

            bool result = Utf8Formatter.TryFormat(value.ToTimeSpan(), output, out int bytesWritten, 'c');
            Debug.Assert(result);

            writer.WriteStringValue(output.Slice(0, bytesWritten));
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            Span<byte> output = stackalloc byte[JsonConstants.MaximumTimeOnlyFormatLength];

            bool result = Utf8Formatter.TryFormat(value.ToTimeSpan(), output, out int bytesWritten, 'c');
            Debug.Assert(result);

            writer.WritePropertyName(output.Slice(0, bytesWritten));
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.String, Format = "time" };
    }
}
