// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.SourceGeneration.Tests
{
    public interface ITestContext
    {
        public JsonTypeInfo<Location> Location { get; }
        public JsonTypeInfo<NumberTypes> NumberTypes { get; }
        public JsonTypeInfo<RepeatedTypes.Location> RepeatedLocation { get; }
        public JsonTypeInfo<ActiveOrUpcomingEvent> ActiveOrUpcomingEvent { get; }
        public JsonTypeInfo<CampaignSummaryViewModel> CampaignSummaryViewModel { get; }
        public JsonTypeInfo<IndexViewModel> IndexViewModel { get; }
        public JsonTypeInfo<WeatherForecastWithPOCOs> WeatherForecastWithPOCOs { get; }
        public JsonTypeInfo<EmptyPoco> EmptyPoco { get; }
        public JsonTypeInfo<HighLowTemps> HighLowTemps { get; }
        public JsonTypeInfo<MyType> MyType { get; }
        public JsonTypeInfo<MyType2> MyType2 { get; }
        public JsonTypeInfo<MyTypeWithCallbacks> MyTypeWithCallbacks { get; }
        public JsonTypeInfo<MyTypeWithPropertyOrdering> MyTypeWithPropertyOrdering { get; }
        public JsonTypeInfo<MyIntermediateType> MyIntermediateType { get; }
        public JsonTypeInfo<HighLowTempsImmutable> HighLowTempsImmutable { get; }
        public JsonTypeInfo<RealWorldContextTests.MyNestedClass> MyNestedClass { get; }
        public JsonTypeInfo<RealWorldContextTests.MyNestedClass.MyNestedNestedClass> MyNestedNestedClass { get; }
        public JsonTypeInfo<object[]> ObjectArray { get; }
        public JsonTypeInfo<string> String { get; }
        public JsonTypeInfo<RealWorldContextTests.ClassWithEnumAndNullable> ClassWithEnumAndNullable { get; }
    }

    internal partial class JsonContext : JsonSerializerContext
    {
        private static JsonSerializerOptions s_defaultOptions { get; } = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static JsonContext s_defaultContext;
        public static JsonContext Default => s_defaultContext ??= new JsonContext(new JsonSerializerOptions(s_defaultOptions));

        public JsonContext() : base(null, s_defaultOptions)
        {
        }

        public JsonContext(JsonSerializerOptions options) : base(options, s_defaultOptions)
        {
        }

        public override JsonTypeInfo GetTypeInfo(global::System.Type type)
        {
            if (type == typeof(JsonMessage))
            {
                return JsonMessage;
            }

            return null!;
        }

        private JsonTypeInfo<JsonMessage> _JsonMessage;
        public JsonTypeInfo<JsonMessage> JsonMessage
        {
            get
            {
                if (_JsonMessage == null)
                {
                    JsonTypeInfo<JsonMessage> objectInfo = JsonMetadataServices.CreateObjectInfo<JsonMessage>(
                        Options,
                        createObjectFunc: static () => new JsonMessage(),
                        propInitFunc: null,
                        default,
                        serializeFunc: JsonMessageSerialize);

                    _JsonMessage = objectInfo;
                }

                return _JsonMessage;
            }
        }

        private static void JsonMessageSerialize(Utf8JsonWriter writer, JsonMessage value) => throw new NotImplementedException();
    }

    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<int, string>))]
    [JsonSerializable(typeof(Dictionary<string, JsonMessage>))]
    internal partial class DictionaryTypeContext : JsonSerializerContext { }

    [JsonSerializable(typeof(JsonMessage))]
    public partial class PublicContext : JsonSerializerContext { }
}
