// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverter : JsonConverter<JsonValue?>
    {
        public override void Write(Utf8JsonWriter writer, JsonValue? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

#pragma warning disable SYSLIB0060 // Type or member is obsolete
            value.WriteTo(writer, options);
#pragma warning restore SYSLIB0060 // Type or member is obsolete
        }

        public override JsonValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            JsonElement element = JsonElement.ParseValue(ref reader);
            return JsonValue.CreateFromElement(ref element, options.GetNodeOptions());
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => JsonSchema.CreateTrueSchema();
    }
}
