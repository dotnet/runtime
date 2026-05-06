// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DateTimeOffsetConverter : JsonPrimitiveConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        internal override DateTimeOffset ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetDateTimeOffsetNoValidation();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new JsonSchema { Type = JsonSchemaType.String, Format = "date-time" };
    }
}
