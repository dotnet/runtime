// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override double ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetDoubleWithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, double value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
