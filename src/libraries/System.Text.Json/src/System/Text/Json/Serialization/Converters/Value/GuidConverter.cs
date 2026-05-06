// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidConverter : JsonPrimitiveConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetGuid();
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        internal override Guid ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetGuidNoValidation();
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            writer.WritePropertyName(value);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling numberHandling) =>
            new() { Type = JsonSchemaType.String, Format = "uuid" };
    }
}
