// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<HighLowTemps> _HighLowTemps;
        public JsonTypeInfo<HighLowTemps> HighLowTemps
        {
            get
            {
                if (_HighLowTemps == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(HighLowTemps))) != null)
                    {
                        _HighLowTemps = JsonMetadataServices.CreateValueInfo<HighLowTemps>(Options, customConverter);
                    }
                    else
                    {
                        JsonTypeInfo<HighLowTemps> objectInfo = JsonMetadataServices.CreateObjectInfo<HighLowTemps>(
                            Options,
                            createObjectFunc: static () => new HighLowTemps(),
                            HighLowTempsPropInitFunc,
                            default,
                            serializeFunc: null);

                        _HighLowTemps = objectInfo;
                    }
                }

                return _HighLowTemps;
            }
        }

        private static JsonPropertyInfo[] HighLowTempsPropInitFunc(JsonSerializerContext context)
        {
            JsonContext jsonContext = (JsonContext)context;
            JsonSerializerOptions options = context.Options;

            JsonPropertyInfo[] properties = new JsonPropertyInfo[2];

            properties[0] = JsonMetadataServices.CreatePropertyInfo<int>(
                options,
                isProperty: true,
                declaringType: typeof(HighLowTemps),
                propertyTypeInfo: jsonContext.Int32,
                converter: null,
                getter: static (obj) => { return ((HighLowTemps)obj).High; },
                setter: static (obj, value) => { ((HighLowTemps)obj).High = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.HighLowTemps.High),
                jsonPropertyName: null);
            
            properties[1] = JsonMetadataServices.CreatePropertyInfo<int>(
                options,
                isProperty: true,
                declaringType: typeof(HighLowTemps),
                propertyTypeInfo: jsonContext.Int32,
                converter: null,
                getter: static (obj) => { return ((HighLowTemps)obj).Low; },
                setter: static (obj, value) => { ((HighLowTemps)obj).Low = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.HighLowTemps.Low),
                jsonPropertyName: null);
            
            return properties;
        }
    }
}
