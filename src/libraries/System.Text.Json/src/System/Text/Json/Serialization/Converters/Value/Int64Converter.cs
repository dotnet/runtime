// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetInt64();
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override long ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetInt64Core(out long value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Int64);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, long value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
