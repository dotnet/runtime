// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(Location), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation", GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(NumberTypes), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(CampaignSummaryViewModel), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(IndexViewModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EmptyPoco), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTemps), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyType), GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(MyType2), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyTypeWithCallbacks), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyTypeWithPropertyOrdering), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyIntermediateType), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTempsImmutable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(byte[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof((string Label1, int Label2, bool)), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithNullableProperties), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(ClassWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(StructWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(ClassWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(StructWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(ClassWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(StructWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(ClassWithCustomConverterFactoryProperty), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(StructWithCustomConverterFactoryProperty), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(ClassWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(StructWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(PersonStruct?), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(TypeWithValidationAttributes), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(TypeWithDerivedAttribute), GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization)]
    internal partial class MixedModeContext : JsonSerializerContext, ITestContext
    {
        public JsonSourceGenerationMode JsonSourceGenerationMode => JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization;
    }

    public sealed class MixedModeContextTests : RealWorldContextTests
    {
        public MixedModeContextTests() : base(MixedModeContext.Default, (options) => new MixedModeContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.Null(MixedModeContext.Default.Location.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.RepeatedLocation.SerializeHandler);
            Assert.Null(MixedModeContext.Default.NumberTypes.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.CampaignSummaryViewModel.SerializeHandler);
            Assert.Null(MixedModeContext.Default.IndexViewModel.SerializeHandler);
            Assert.Null(MixedModeContext.Default.WeatherForecastWithPOCOs.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.EmptyPoco.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.HighLowTemps.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyType.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyType2.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyTypeWithCallbacks.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyTypeWithPropertyOrdering.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyIntermediateType.SerializeHandler);
            Assert.Null(MixedModeContext.Default.HighLowTempsImmutable.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyNestedClass.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.MyNestedNestedClass.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ObjectArray.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ByteArray.SerializeHandler);
            Assert.Null(MixedModeContext.Default.SampleEnum.SerializeHandler);
            Assert.Null(MixedModeContext.Default.String.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.ValueTupleStringInt32Boolean.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.ClassWithEnumAndNullable.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.ClassWithNullableProperties.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ClassWithCustomConverter.SerializeHandler);
            Assert.Null(MixedModeContext.Default.StructWithCustomConverter.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ClassWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MixedModeContext.Default.StructWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ClassWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MixedModeContext.Default.StructWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MixedModeContext.Default.ClassWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Null(MixedModeContext.Default.StructWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MixedModeContext.Default.ClassWithBadCustomConverter.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MixedModeContext.Default.StructWithBadCustomConverter.SerializeHandler);
            Assert.Null(MixedModeContext.Default.NullablePersonStruct.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.PersonStruct.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.TypeWithValidationAttributes.SerializeHandler);
            Assert.NotNull(MixedModeContext.Default.TypeWithDerivedAttribute.SerializeHandler);
        }

        [Fact]
        public override void RoundTripIndexViewModel()
        {
            IndexViewModel expected = CreateIndexViewModel();

            string json = JsonSerializer.Serialize(expected, DefaultContext.IndexViewModel);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.IndexViewModel), typeof(CampaignSummaryViewModel));

            IndexViewModel obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).IndexViewModel);
            VerifyIndexViewModel(expected, obj);
        }

        [Fact]
        public override void RoundTripCampaignSummaryViewModel()
        {
            CampaignSummaryViewModel expected = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(expected, DefaultContext.CampaignSummaryViewModel);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.CampaignSummaryViewModel), typeof(CampaignSummaryViewModel));

            CampaignSummaryViewModel obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).CampaignSummaryViewModel);
            VerifyCampaignSummaryViewModel(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.CampaignSummaryViewModel);
        }

        [Fact]
        public override void RoundTripCollectionsDictionary()
        {
            WeatherForecastWithPOCOs expected = CreateWeatherForecastWithPOCOs();

            string json = JsonSerializer.Serialize(expected, DefaultContext.WeatherForecastWithPOCOs);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.WeatherForecastWithPOCOs), typeof(HighLowTemps));

            WeatherForecastWithPOCOs obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).WeatherForecastWithPOCOs);
            VerifyWeatherForecastWithPOCOs(expected, obj);
        }

        [Fact]
        public override void RoundTripEmptyPoco()
        {
            EmptyPoco expected = CreateEmptyPoco();

            string json = JsonSerializer.Serialize(expected, DefaultContext.EmptyPoco);
            // This would have thrown if we tried to lookup any properties but since there are no properties this is able to complete.
            EmptyPoco obj = JsonSerializer.Deserialize(json, DefaultContext.EmptyPoco);
            VerifyEmptyPoco(expected, obj);

            obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).EmptyPoco);
            VerifyEmptyPoco(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.EmptyPoco);
        }

        [Fact]
        public override void RoundTripTypeNameClash()
        {
            RepeatedTypes.Location expected = CreateRepeatedLocation();

            string json = JsonSerializer.Serialize(expected, DefaultContext.RepeatedLocation);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.RepeatedLocation), typeof(RepeatedTypes.Location));

            RepeatedTypes.Location obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).RepeatedLocation);
            VerifyRepeatedLocation(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.RepeatedLocation);
        }

        [Fact]
        public override void HandlesNestedTypes()
        {
            string json = @"{""MyInt"":5}";
            MyNestedClass obj = JsonSerializer.Deserialize<MyNestedClass>(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).MyNestedClass);
            Assert.Equal(5, obj.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj, DefaultContext.MyNestedClass));

            MyNestedClass.MyNestedNestedClass obj2 = JsonSerializer.Deserialize<MyNestedClass.MyNestedNestedClass>(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).MyNestedNestedClass);
            Assert.Equal(5, obj2.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj2, DefaultContext.MyNestedNestedClass));
        }

        [Fact]
        public override void SerializeObjectArray()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, DefaultContext.ObjectArray);
            object[] arr = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), ((ITestContext)MetadataWithPerTypeAttributeContext.Default).IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), ((ITestContext)MetadataWithPerTypeAttributeContext.Default).CampaignSummaryViewModel));
        }

        [Fact]
        public override void SerializeObjectArray_WithCustomOptions()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            ITestContext context = SerializationContextWithCamelCase.Default;
            Assert.Same(JsonNamingPolicy.CamelCase, ((JsonSerializerContext)context).Options.PropertyNamingPolicy);

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, context.ObjectArray);
            object[] arr = JsonSerializer.Deserialize(json, ((ITestContext)MetadataWithPerTypeAttributeContext.Default).ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];

            ITestContext metadataContext = new MetadataContext(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), metadataContext.IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), metadataContext.CampaignSummaryViewModel));
        }

        [Fact]
        public void OnSerializeCallbacks_WithCustomOptions()
        {
            MyTypeWithCallbacks obj = new();
            Assert.Null(obj.MyProperty);

            ITestContext context = SerializationContextWithCamelCase.Default;
            Assert.Same(JsonNamingPolicy.CamelCase, ((JsonSerializerContext)context).Options.PropertyNamingPolicy);

            string json = JsonSerializer.Serialize(obj, context.MyTypeWithCallbacks);
            Assert.Equal("{\"myProperty\":\"Before\"}", json);
            Assert.Equal("After", obj.MyProperty);

            context = new MetadataContext(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            json = JsonSerializer.Serialize(obj, context.MyTypeWithCallbacks);
            Assert.Equal("{\"myProperty\":\"Before\"}", json);
            Assert.Equal("After", obj.MyProperty);
        }
    }
}
