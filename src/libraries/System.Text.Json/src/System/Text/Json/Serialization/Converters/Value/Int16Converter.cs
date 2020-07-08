// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int16Converter : JsonConverter<short>
    {
        public override short Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt16();
        }

        public override void Write(Utf8JsonWriter writer, short value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override short ReadWithQuotes(ref Utf8JsonReader reader)
        {
            return reader.GetInt16WithQuotes();
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, short value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }
    }
}
