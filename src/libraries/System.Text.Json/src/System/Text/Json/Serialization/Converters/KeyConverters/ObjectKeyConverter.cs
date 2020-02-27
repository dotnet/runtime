// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ObjectKeyConverter : KeyConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Can't use ParseValue as ObjectConverter does since it does not parse the property name but the value token next to it.
            return ReadKeyFromBytes(reader.GetSpan());
        }

        public override object ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            int idx = bytes.IndexOf(JsonConstants.BackSlash);
            ReadOnlySpan<byte> unescapedBytes = idx > -1 ? JsonReaderHelper.GetUnescapedSpan(bytes, idx) : bytes;

            // Always wrap property name in quotes since reader.GetSpan() removes it from property names.
            // The side effect of this is that any boxed number will be a JsonElement of JsonValueKind.String.
            byte[] propertyNameArray = new byte[unescapedBytes.Length + 2];
            Span<byte> span = propertyNameArray;
            span[0] = (byte)'"';
            unescapedBytes.CopyTo(span.Slice(1));
            span[span.Length - 1] = (byte)'"';

            using (JsonDocument document = JsonDocument.Parse(propertyNameArray))
            {
                return document.RootElement.Clone();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException();
        }
    }
}
