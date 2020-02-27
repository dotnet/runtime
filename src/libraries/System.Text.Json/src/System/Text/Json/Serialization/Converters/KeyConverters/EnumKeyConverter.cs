// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Unicode;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumKeyConverter<TEnum> : KeyConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? enumValue = reader.GetString();
            if (!Enum.TryParse(enumValue, out TEnum value)
                    && !Enum.TryParse(enumValue, ignoreCase: true, out value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        public override TEnum ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            int idx = bytes.IndexOf(JsonConstants.BackSlash);
            // if no escaping, just parse the bytes to string using TranscodeHelper.
            string unescapedKeyName = idx > -1 ? JsonReaderHelper.GetUnescapedString(bytes, idx) : JsonReaderHelper.TranscodeHelper(bytes);

            if (!Enum.TryParse(unescapedKeyName, out TEnum value)
                    && !Enum.TryParse(unescapedKeyName, ignoreCase: true, out value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            string keyName = value.ToString();
            // Unlike EnumConverter we don't do any validation here since PropertyName can only be string.
            writer.WritePropertyName(keyName);
        }
    }
}
