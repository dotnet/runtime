// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonNodeConverterFactory : JsonConverterFactory
    {
        private static JsonArrayConverter ArrayConverter => field ??= new JsonArrayConverter();
        private static JsonObjectConverter ObjectConverter => field ??= new JsonObjectConverter();
        private static JsonValueConverter ValueConverter => field ??= new JsonValueConverter();

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
            {
                return ValueConverter;
            }

            if (typeof(JsonObject) == typeToConvert)
            {
                return ObjectConverter;
            }

            if (typeof(JsonArray) == typeToConvert)
            {
                return ArrayConverter;
            }

            Debug.Assert(typeof(JsonNode) == typeToConvert);
            return JsonNodeConverter.Instance;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(JsonNode).IsAssignableFrom(typeToConvert);
    }
}
