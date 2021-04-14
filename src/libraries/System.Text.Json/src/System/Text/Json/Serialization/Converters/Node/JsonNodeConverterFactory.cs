// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Node;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonNodeConverterFactory : JsonConverterFactory
    {
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonClassInfo.ObjectType == typeToConvert)
            {
                if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode)
                {
                    return JsonNodeConverter.Instance;
                }

                // Return the converter for System.Object which uses JsonElement.
                return JsonNodeConverter.Instance.ElementConverter;
            }

            if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
            {
                return JsonNodeConverter.Instance.ValueConverter;
            }

            if (typeof(JsonObject) == typeToConvert)
            {
                return JsonNodeConverter.Instance.ObjectConverter;
            }

            if (typeof(JsonArray) == typeToConvert)
            {
                return JsonNodeConverter.Instance.ArrayConverter;
            }

            Debug.Assert(typeof(JsonNode) == typeToConvert);
            return JsonNodeConverter.Instance;
        }

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == JsonClassInfo.ObjectType ||
            typeof(JsonNode).IsAssignableFrom(typeToConvert);
    }
}
