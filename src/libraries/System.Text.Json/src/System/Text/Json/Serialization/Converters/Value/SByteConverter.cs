// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class SByteConverter : JsonConverter<sbyte>
    {
        public override sbyte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetSByte();
        }

        public override void Write(Utf8JsonWriter writer, sbyte value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        internal override sbyte ReadWithQuotes(ref Utf8JsonReader reader)
        {
            if (!reader.TryGetSByteCore(out sbyte value))
            {
                throw ThrowHelper.GetFormatException(NumericType.SByte);
            }

            return value;
        }

        internal override void WriteWithQuotes(Utf8JsonWriter writer, [DisallowNull] sbyte value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WritePropertyName(value);
        }

        internal override bool CanBeDictionaryKey => true;
    }
}
