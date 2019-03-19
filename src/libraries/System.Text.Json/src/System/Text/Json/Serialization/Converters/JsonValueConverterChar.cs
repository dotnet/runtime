// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterChar : JsonValueConverter<char>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out char value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            value = reader.GetString()[0];
            return true;
        }

        public override void Write(char value, ref Utf8JsonWriter writer)
        {
#if BUILDING_INBOX_LIBRARY
            Span<char> temp = MemoryMarshal.CreateSpan<char>(ref value, 1);
            writer.WriteStringValue(temp);
#else
            writer.WriteStringValue(value.ToString());
#endif
        }

        public override void Write(Span<byte> escapedPropertyName, char value, ref Utf8JsonWriter writer)
        {
#if BUILDING_INBOX_LIBRARY
            Span<char> temp = MemoryMarshal.CreateSpan<char>(ref value, 1);
            writer.WriteString(escapedPropertyName, temp);
#else
            writer.WriteString(escapedPropertyName, value.ToString());
#endif
        }
    }
}
