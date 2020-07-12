// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class SingleConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                options != null &&
                ((JsonConstants.ReadNumberOrFloatingConstantFromString) & options.NumberHandling) != 0)
            {
                return reader.GetSingleWithQuotes();
            }

            return reader.GetSingle();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (options != null &&
                ((JsonConstants.WriteNumberOrFloatingConstantAsString) & options.NumberHandling) != 0)
            {
                writer.WriteNumberValueAsString(value);
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }

        internal override float ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetSingleWithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, float value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
