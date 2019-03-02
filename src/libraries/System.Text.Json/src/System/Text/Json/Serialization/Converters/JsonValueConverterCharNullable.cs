// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterCharNullable : JsonValueConverter<char?>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out char? value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            value = reader.GetString()[0];
            return true;
        }

        public override void Write(char? value, ref Utf8JsonWriter writer)
        {
            Debug.Assert(value.HasValue); // nulls are filtered before calling here
#if BUILDING_INBOX_LIBRARY
            char tempChar = value.Value;
            Span<char> tempSpan = MemoryMarshal.CreateSpan<char>(ref tempChar, 1);
            writer.WriteStringValue(tempSpan);
#else
            writer.WriteStringValue(value.ToString());
#endif
        }

        public override void Write(Span<byte> escapedPropertyName, char? value, ref Utf8JsonWriter writer)
        {
            Debug.Assert(value.HasValue); // nulls are filtered before calling here
#if BUILDING_INBOX_LIBRARY
            char tempChar = value.Value;
            Span<char> tempSpan = MemoryMarshal.CreateSpan<char>(ref tempChar, 1);
            writer.WriteString(escapedPropertyName, tempSpan);
#else
            writer.WriteString(escapedPropertyName, value.ToString());
#endif
        }
    }
}
