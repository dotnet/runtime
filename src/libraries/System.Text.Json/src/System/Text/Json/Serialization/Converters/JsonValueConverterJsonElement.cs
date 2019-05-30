// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonValueConverterJsonElement : JsonValueConverter<JsonElement>
    {
        public override bool TryRead(Type valueType, ref Utf8JsonReader reader, out JsonElement value)
        {
            if (JsonDocument.TryParseValue(ref reader, out JsonDocument document))
            {
                value = document.RootElement.Clone();
                document.Dispose();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public override void Write(JsonElement value, Utf8JsonWriter writer)
        {
            value.WriteAsValue(writer);
        }

        public override void Write(JsonEncodedText propertyName, JsonElement value, Utf8JsonWriter writer)
        {
            value.WriteAsProperty(propertyName.ToString(), writer);
        }
    }
}
