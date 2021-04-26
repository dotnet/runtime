// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private static JsonContext s_default;
        public static JsonContext Default => s_default ??= new JsonContext(new JsonSerializerOptions());

        public JsonContext() : base(null)
        {
        }

        public JsonContext(JsonSerializerOptions options) : base(options)
        {
        }

        private JsonConverter GetRuntimeProvidedCustomConverter(System.Type type)
        {
            IList<JsonConverter> converters = Options.Converters;

            for (int i = 0; i < converters.Count; i++)
            {
                JsonConverter converter = converters[i];

                if (converter.CanConvert(type))
                {
                    if (converter is JsonConverterFactory factory)
                    {
                        converter = factory.CreateConverter(type, Options);
                        if (converter == null || converter is JsonConverterFactory)
                        {
                            throw new System.InvalidOperationException($"The converter '{factory.GetType()}' cannot return null or a JsonConverterFactory instance.");
                        }
                    }

                    return converter;
                }
            }

            return null;
        }
    }
}
