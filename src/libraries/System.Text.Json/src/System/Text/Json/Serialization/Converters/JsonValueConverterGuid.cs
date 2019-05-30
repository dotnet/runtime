// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterGuid : JsonValueConverter<Guid>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out Guid value)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                value = default;
                return false;
            }

            return reader.TryGetGuid(out value);
        }

        public override void Write(Guid value, Utf8JsonWriter writer)
        {
            writer.WriteStringValue(value);
        }

        public override void Write(JsonEncodedText propertyName, Guid value, Utf8JsonWriter writer)
        {
            writer.WriteString(propertyName, value);
        }
    }
}
