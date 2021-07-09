// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<WeatherForecastWithPOCOs> _WeatherForecastWithPOCOs;
        public JsonTypeInfo<WeatherForecastWithPOCOs> WeatherForecastWithPOCOs
        {
            get
            {
                if (_WeatherForecastWithPOCOs == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(WeatherForecastWithPOCOs))) != null)
                    {
                        _WeatherForecastWithPOCOs = JsonMetadataServices.CreateValueInfo<WeatherForecastWithPOCOs>(Options, customConverter);
                    }
                    else
                    {
                        JsonTypeInfo<WeatherForecastWithPOCOs> objectInfo = JsonMetadataServices.CreateObjectInfo<WeatherForecastWithPOCOs>(
                            Options,
                            createObjectFunc: static () => new WeatherForecastWithPOCOs(),
                            WeatherForecastWithPOCOsPropInitFunc,
                            default,
                            serializeFunc: null);

                        _WeatherForecastWithPOCOs = objectInfo;
                    }
                }

                return _WeatherForecastWithPOCOs;
            }
        }

        private static JsonPropertyInfo[] WeatherForecastWithPOCOsPropInitFunc(JsonSerializerContext context)
        {
            JsonContext jsonContext = (JsonContext)context;
            JsonSerializerOptions options = context.Options;

            JsonPropertyInfo[] properties = new JsonPropertyInfo[7];

            properties[0] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.DateTimeOffset,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).Date; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).Date = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.Date),
                jsonPropertyName: null);
            
            properties[1] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.Int32,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).TemperatureCelsius; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).TemperatureCelsius = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.TemperatureCelsius),
                jsonPropertyName: null);
            
            properties[2] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.String,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).Summary; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).Summary = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.Summary),
                jsonPropertyName: null);
            
            properties[3] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.ListSystemDateTimeOffset,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).DatesAvailable; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).DatesAvailable = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.DatesAvailable),
                jsonPropertyName: null);
            
            properties[4] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.Dictionary,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).TemperatureRanges; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).TemperatureRanges = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.TemperatureRanges),
                jsonPropertyName: null);
            
            properties[5] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.StringArray,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).SummaryWords; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).SummaryWords = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.SummaryWords),
                jsonPropertyName: null);
            
            properties[6] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: false,
                declaringType: typeof(WeatherForecastWithPOCOs),
                propertyTypeInfo: jsonContext.String,
                converter: null,
                getter: static (obj) => { return ((WeatherForecastWithPOCOs)obj).SummaryField; },
                setter: static (obj, value) => { ((WeatherForecastWithPOCOs)obj).SummaryField = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Serialization.WeatherForecastWithPOCOs.SummaryField),
                jsonPropertyName: null);
            
            return properties;
        }
    }
}
