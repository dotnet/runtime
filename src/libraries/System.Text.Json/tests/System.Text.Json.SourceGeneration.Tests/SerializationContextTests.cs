// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable(typeof(Location), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RepeatedTypes.Location), GenerationMode = JsonSourceGenerationMode.Serialization, TypeInfoPropertyName = "RepeatedLocation")]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(CampaignSummaryViewModel), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(IndexViewModel), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(EmptyPoco), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTemps), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyType), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyType2), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyIntermediateType), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTempsImmutable), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Serialization)]
    internal partial class SerializationContext : JsonSerializerContext, ITestContext
    {
    }

    [JsonSerializerOptions(NamingPolicy = JsonKnownNamingPolicy.BuiltInCamelCase)]
    [JsonSerializable(typeof(Location), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RepeatedTypes.Location), GenerationMode = JsonSourceGenerationMode.Serialization, TypeInfoPropertyName = "RepeatedLocation")]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(CampaignSummaryViewModel), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(IndexViewModel), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(EmptyPoco), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTemps), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyType), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyType2), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(MyIntermediateType), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(HighLowTempsImmutable), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Serialization)]
    internal partial class SerializationContextWithCamelCase : JsonSerializerContext, ITestContext
    {
    }

    public sealed class SerializationContextTests : RealWorldContextTests
    {
        public SerializationContextTests() : base(SerializationContext.Default, (options) => new SerializationContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.NotNull(SerializationContext.Default.Location.Serialize);
            Assert.NotNull(SerializationContext.Default.RepeatedLocation.Serialize);
            Assert.NotNull(SerializationContext.Default.ActiveOrUpcomingEvent.Serialize);
            Assert.NotNull(SerializationContext.Default.CampaignSummaryViewModel.Serialize);
            Assert.NotNull(SerializationContext.Default.IndexViewModel.Serialize);
            Assert.NotNull(SerializationContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.NotNull(SerializationContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.NotNull(SerializationContext.Default.HighLowTemps.Serialize);
            Assert.NotNull(SerializationContext.Default.MyType.Serialize);
            Assert.NotNull(SerializationContext.Default.MyType2.Serialize);
            Assert.NotNull(SerializationContext.Default.MyIntermediateType.Serialize);
            Assert.NotNull(SerializationContext.Default.HighLowTempsImmutable.Serialize);
            Assert.NotNull(SerializationContext.Default.MyNestedClass.Serialize);
            Assert.NotNull(SerializationContext.Default.MyNestedNestedClass.Serialize);
            Assert.Null(SerializationContext.Default.ObjectArray.Serialize);
            Assert.Null(SerializationContext.Default.String.Serialize);
            Assert.NotNull(SerializationContext.Default.ClassWithEnumAndNullable.Serialize);
        }

        [Fact]
        public override void RoundTripLocation()
        {
            Location expected = CreateLocation();

            string json = JsonSerializer.Serialize(expected, DefaultContext.Location);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.Location), typeof(Location));

            Location obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).Location);
            VerifyLocation(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.Location);
        }

        [Fact]
        public override void RoundTripIndexViewModel()
        {
            IndexViewModel expected = CreateIndexViewModel();

            string json = JsonSerializer.Serialize(expected, DefaultContext.IndexViewModel);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.IndexViewModel), typeof(IndexViewModel));

            IndexViewModel obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).IndexViewModel);
            VerifyIndexViewModel(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.IndexViewModel);
        }

        [Fact]
        public override void RoundTripCampaignSummaryViewModel()
        {
            CampaignSummaryViewModel expected = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(expected, DefaultContext.CampaignSummaryViewModel);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.CampaignSummaryViewModel), typeof(CampaignSummaryViewModel));

            CampaignSummaryViewModel obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).CampaignSummaryViewModel);
            VerifyCampaignSummaryViewModel(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.CampaignSummaryViewModel);
        }

        [Fact]
        public override void RoundTripActiveOrUpcomingEvent()
        {
            ActiveOrUpcomingEvent expected = CreateActiveOrUpcomingEvent();

            string json = JsonSerializer.Serialize(expected, DefaultContext.ActiveOrUpcomingEvent);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.ActiveOrUpcomingEvent), typeof(ActiveOrUpcomingEvent));

            ActiveOrUpcomingEvent obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).ActiveOrUpcomingEvent);
            VerifyActiveOrUpcomingEvent(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.ActiveOrUpcomingEvent);
        }

        [Fact]
        public override void RoundTripCollectionsDictionary()
        {
            WeatherForecastWithPOCOs expected = CreateWeatherForecastWithPOCOs();

            string json = JsonSerializer.Serialize(expected, DefaultContext.WeatherForecastWithPOCOs);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.WeatherForecastWithPOCOs), typeof(WeatherForecastWithPOCOs));

            WeatherForecastWithPOCOs obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).WeatherForecastWithPOCOs);
            VerifyWeatherForecastWithPOCOs(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.WeatherForecastWithPOCOs);
        }

        [Fact]
        public override void RoundTripEmptyPoco()
        {
            EmptyPoco expected = CreateEmptyPoco();

            string json = JsonSerializer.Serialize(expected, DefaultContext.EmptyPoco);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.EmptyPoco), typeof(EmptyPoco));

            EmptyPoco obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).EmptyPoco);
            VerifyEmptyPoco(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.EmptyPoco);
        }

        [Fact]
        public override void RoundTripTypeNameClash()
        {
            RepeatedTypes.Location expected = CreateRepeatedLocation();

            string json = JsonSerializer.Serialize(expected, DefaultContext.RepeatedLocation);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.RepeatedLocation), typeof(RepeatedTypes.Location));

            RepeatedTypes.Location obj = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).RepeatedLocation);
            VerifyRepeatedLocation(expected, obj);

            AssertFastPathLogicCorrect(json, obj, DefaultContext.RepeatedLocation);
        }

        [Fact]
        public override void NestedSameTypeWorks()
        {
            MyType myType = new() { Type = new() };
            string json = JsonSerializer.Serialize(myType, DefaultContext.MyType);
            myType = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).MyType);
            AssertFastPathLogicCorrect(json, myType, DefaultContext.MyType);

            MyType2 myType2 = new() { Type = new MyIntermediateType() { Type = myType } };
            json = JsonSerializer.Serialize(myType2, DefaultContext.MyType2);
            myType2 = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).MyType2);
            AssertFastPathLogicCorrect(json, myType2, DefaultContext.MyType2);
        }

        [Fact]
        public override void SerializeObjectArray()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, DefaultContext.ObjectArray);
            object[] arr = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), ((ITestContext)MetadataContext.Default).IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), ((ITestContext)MetadataContext.Default).CampaignSummaryViewModel));
        }

        [Fact]
        public override void SerializeObjectArray_WithCustomOptions()
        {
            IndexViewModel index = CreateIndexViewModel();
            CampaignSummaryViewModel campaignSummary = CreateCampaignSummaryViewModel();

            ITestContext context = SerializationContextWithCamelCase.Default;
            Assert.Same(JsonNamingPolicy.CamelCase, ((JsonSerializerContext)context).Options.PropertyNamingPolicy);

            string json = JsonSerializer.Serialize(new object[] { index, campaignSummary }, context.ObjectArray);
            // Verify JSON was written with camel casing.
            Assert.Contains("activeOrUpcomingEvents", json);
            Assert.Contains("featuredCampaign", json);
            Assert.Contains("description", json);
            Assert.Contains("organizationName", json);

            object[] arr = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).ObjectArray);

            JsonElement indexAsJsonElement = (JsonElement)arr[0];
            JsonElement campaignSummeryAsJsonElement = (JsonElement)arr[1];

            ITestContext metadataContext = new MetadataContext(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            VerifyIndexViewModel(index, JsonSerializer.Deserialize(indexAsJsonElement.GetRawText(), metadataContext.IndexViewModel));
            VerifyCampaignSummaryViewModel(campaignSummary, JsonSerializer.Deserialize(campaignSummeryAsJsonElement.GetRawText(), metadataContext.CampaignSummaryViewModel));
        }

        [Fact]
        public override void SerializeObjectArray_SimpleTypes_WithCustomOptions()
        {
            JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            ITestContext context = new SerializationContext(options);

            string json = JsonSerializer.Serialize(new object[] { "Hello", "World" }, typeof(object[]), (JsonSerializerContext)context);
            object[] arr = (object[])JsonSerializer.Deserialize(json, typeof(object[]), (JsonSerializerContext)((ITestContext)MetadataContext.Default));

            JsonElement hello = (JsonElement)arr[0];
            JsonElement world = (JsonElement)arr[1];
            Assert.Equal("\"Hello\"", hello.GetRawText());
            Assert.Equal("\"World\"", world.GetRawText());
        }

        [Fact]
        public override void HandlesNestedTypes()
        {
            string json = @"{""MyInt"":5}";
            MyNestedClass obj = JsonSerializer.Deserialize<MyNestedClass>(json, ((ITestContext)MetadataContext.Default).MyNestedClass);
            Assert.Equal(5, obj.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj, DefaultContext.MyNestedClass));

            MyNestedClass.MyNestedNestedClass obj2 = JsonSerializer.Deserialize<MyNestedClass.MyNestedNestedClass>(json, ((ITestContext)MetadataContext.Default).MyNestedNestedClass);
            Assert.Equal(5, obj2.MyInt);
            Assert.Equal(json, JsonSerializer.Serialize(obj2, DefaultContext.MyNestedNestedClass));
        }

        [Fact]
        public override void EnumAndNullable()
        {
            RunTest(new ClassWithEnumAndNullable() { Day = DayOfWeek.Monday, NullableDay = DayOfWeek.Tuesday });
            RunTest(new ClassWithEnumAndNullable());

            void RunTest(ClassWithEnumAndNullable expected)
            {
                string json = JsonSerializer.Serialize(expected, DefaultContext.ClassWithEnumAndNullable);
                ClassWithEnumAndNullable actual = JsonSerializer.Deserialize(json, ((ITestContext)MetadataContext.Default).ClassWithEnumAndNullable);
                Assert.Equal(expected.Day, actual.Day);
                Assert.Equal(expected.NullableDay, actual.NullableDay);
            }
        }

        [Fact]
        public override void ParameterizedConstructor()
        {
            string json = JsonSerializer.Serialize(new HighLowTempsImmutable(1, 2), DefaultContext.HighLowTempsImmutable);
            Assert.Contains(@"""High"":1", json);
            Assert.Contains(@"""Low"":2", json);

            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Deserialize(json, DefaultContext.HighLowTempsImmutable), typeof(HighLowTempsImmutable));
        }
    }
}
