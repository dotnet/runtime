// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.TryGetInt32AfterValidation(out int keyValue))
            {
                throw ThrowHelper.GetFormatException(NumericType.Int32);
            }

            return keyValue;
        }

        public override int ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            int idx = bytes.IndexOf(JsonConstants.BackSlash);
            ReadOnlySpan<byte> unescapedBytes = idx > -1 ? JsonReaderHelper.GetUnescapedSpan(bytes, idx) : bytes;

            if (Utf8Parser.TryParse(unescapedBytes, out int keyValue, out int bytesConsumed) && bytesConsumed == unescapedBytes.Length)
            {
               return keyValue;
            }

            throw ThrowHelper.GetFormatException(NumericType.Int32);
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WritePropertyName(value);
    }
}
