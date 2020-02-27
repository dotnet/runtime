// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.TryGetGuidAfterValidation(out Guid keyValue))
            {
                throw ThrowHelper.GetFormatException(DataType.Guid);
            }

            return keyValue;
        }

        public override Guid ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            int idx = bytes.IndexOf(JsonConstants.BackSlash);
            ReadOnlySpan<byte> unescapedBytes = idx > -1 ? JsonReaderHelper.GetUnescapedSpan(bytes, idx) : bytes;

            if (Utf8Parser.TryParse(unescapedBytes, out Guid keyValue, out int bytesConsumed) && bytesConsumed == unescapedBytes.Length)
            {
                return keyValue;
            }

            throw ThrowHelper.GetFormatException(DataType.Guid);
        }

        public override void Write(Utf8JsonWriter writer, Guid key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
