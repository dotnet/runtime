// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonNodeConverterFactory : JsonConverterFactory
    {
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
            {
                return JsonNodeConverter.ValueConverter;
            }

            if (typeof(JsonObject) == typeToConvert)
            {
                return JsonNodeConverter.ObjectConverter;
            }

            if (typeof(JsonArray) == typeToConvert)
            {
                return JsonNodeConverter.ArrayConverter;
            }

            Debug.Assert(typeof(JsonNode) == typeToConvert);
            return JsonNodeConverter.Instance;
        }

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert != JsonTypeInfo.ObjectType &&
            typeof(JsonNode).IsAssignableFrom(typeToConvert);
    }
}
