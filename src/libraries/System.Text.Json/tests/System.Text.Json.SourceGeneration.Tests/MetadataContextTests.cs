// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable(typeof(Location), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation", GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(NumberTypes), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(CampaignSummaryViewModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(IndexViewModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EmptyPoco), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HighLowTemps), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyType), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyType2), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyTypeWithCallbacks), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyTypeWithPropertyOrdering), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyIntermediateType), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HighLowTempsImmutable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof((string Label1, int Label2, bool)), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterPropertyFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterPropertyFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class MetadataWithPerTypeAttributeContext : JsonSerializerContext, ITestContext
    {
        public JsonSourceGenerationMode JsonSourceGenerationMode => JsonSourceGenerationMode.Metadata;
    }

    public sealed class MetadataWithPerTypeAttributeContextTests : RealWorldContextTests
    {
        public MetadataWithPerTypeAttributeContextTests() : base(MetadataWithPerTypeAttributeContext.Default, (options) => new MetadataWithPerTypeAttributeContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.Location.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.RepeatedLocation.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.NumberTypes.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ActiveOrUpcomingEvent.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.CampaignSummaryViewModel.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.IndexViewModel.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.EmptyPoco.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.HighLowTemps.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyType.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyType2.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyIntermediateType.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.HighLowTempsImmutable.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyNestedClass.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyNestedNestedClass.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ObjectArray.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.SampleEnum.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.String.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ValueTupleStringInt32Boolean.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithEnumAndNullable.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverter.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverter.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterFactory.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterFactory.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterProperty.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterProperty.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterPropertyFactory.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterPropertyFactory.Serialize);
            Assert.Throws<InvalidOperationException>(() => MetadataWithPerTypeAttributeContext.Default.ClassWithBadCustomConverter.Serialize);
            Assert.Throws<InvalidOperationException>(() => MetadataWithPerTypeAttributeContext.Default.StructWithBadCustomConverter.Serialize);
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
    [JsonSerializable(typeof(Location))]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation")]
    [JsonSerializable(typeof(NumberTypes))]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent))]
    [JsonSerializable(typeof(CampaignSummaryViewModel))]
    [JsonSerializable(typeof(IndexViewModel))]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs))]
    [JsonSerializable(typeof(EmptyPoco))]
    [JsonSerializable(typeof(HighLowTemps))]
    [JsonSerializable(typeof(MyType))]
    [JsonSerializable(typeof(MyType2))]
    [JsonSerializable(typeof(MyTypeWithCallbacks))]
    [JsonSerializable(typeof(MyTypeWithPropertyOrdering))]
    [JsonSerializable(typeof(MyIntermediateType))]
    [JsonSerializable(typeof(HighLowTempsImmutable))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof((string Label1, int Label2, bool)))]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable))]
    [JsonSerializable(typeof(ClassWithCustomConverter))]
    [JsonSerializable(typeof(StructWithCustomConverter))]
    [JsonSerializable(typeof(ClassWithCustomConverterFactory))]
    [JsonSerializable(typeof(StructWithCustomConverterFactory))]
    [JsonSerializable(typeof(ClassWithCustomConverterProperty))]
    [JsonSerializable(typeof(StructWithCustomConverterProperty))]
    [JsonSerializable(typeof(ClassWithCustomConverterPropertyFactory))]
    [JsonSerializable(typeof(StructWithCustomConverterPropertyFactory))]
    [JsonSerializable(typeof(ClassWithBadCustomConverter))]
    [JsonSerializable(typeof(StructWithBadCustomConverter))]
    internal partial class MetadataContext : JsonSerializerContext, ITestContext
    {
        public JsonSourceGenerationMode JsonSourceGenerationMode => JsonSourceGenerationMode.Metadata;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnumWrittenAsString
    {
        A = 1
    }

    [JsonSerializable(typeof(EnumWrittenAsString))]
    public partial class ContextWithExplicitStringEnum : JsonSerializerContext
    {
    }

    public class PocoWithEnum
    {
        public EnumWrittenAsString MyEnum { get; set; }
    }

    [JsonSerializable(typeof(PocoWithEnum))]
    public partial class ContextWithImplicitStringEnum : JsonSerializerContext
    {
    }

    public sealed class MetadataContextTests : RealWorldContextTests
    {
        public MetadataContextTests() : base(MetadataContext.Default, (options) => new MetadataContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.Null(MetadataContext.Default.Location.Serialize);
            Assert.Null(MetadataContext.Default.RepeatedLocation.Serialize);
            Assert.Null(MetadataContext.Default.NumberTypes.Serialize);
            Assert.Null(MetadataContext.Default.ActiveOrUpcomingEvent.Serialize);
            Assert.Null(MetadataContext.Default.CampaignSummaryViewModel.Serialize);
            Assert.Null(MetadataContext.Default.IndexViewModel.Serialize);
            Assert.Null(MetadataContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.Null(MetadataContext.Default.EmptyPoco.Serialize);
            Assert.Null(MetadataContext.Default.HighLowTemps.Serialize);
            Assert.Null(MetadataContext.Default.MyType.Serialize);
            Assert.Null(MetadataContext.Default.MyType2.Serialize);
            Assert.Null(MetadataContext.Default.MyTypeWithCallbacks.Serialize);
            Assert.Null(MetadataContext.Default.MyTypeWithPropertyOrdering.Serialize);
            Assert.Null(MetadataContext.Default.MyIntermediateType.Serialize);
            Assert.Null(MetadataContext.Default.HighLowTempsImmutable.Serialize);
            Assert.Null(MetadataContext.Default.MyNestedClass.Serialize);
            Assert.Null(MetadataContext.Default.MyNestedNestedClass.Serialize);
            Assert.Null(MetadataContext.Default.ObjectArray.Serialize);
            Assert.Null(MetadataContext.Default.SampleEnum.Serialize);
            Assert.Null(MetadataContext.Default.String.Serialize);
            Assert.Null(MetadataContext.Default.ValueTupleStringInt32Boolean.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithEnumAndNullable.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverter.Serialize);
            Assert.Null(MetadataContext.Default.StructWithCustomConverter.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterFactory.Serialize);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterFactory.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterProperty.Serialize);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterProperty.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterPropertyFactory.Serialize);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterPropertyFactory.Serialize);
            Assert.Throws<InvalidOperationException>(() => MetadataContext.Default.ClassWithBadCustomConverter.Serialize);
            Assert.Throws<InvalidOperationException>(() => MetadataContext.Default.StructWithBadCustomConverter.Serialize);
        }

        [Fact]
        public void EnsureHelperMethodGenerated_TypeFactory()
        {
            // There are 2 helper methods generated for obtaining a converter from a factory:
            // - JsonConverter<T> version that is property-based (that calls the one below)
            // - JsonConverter version that is Type-based
            // and this test verifies the latter one is generated. Other tests also have property-level
            // factories and thus verify both are created.

            const string Json = "\"A\"";

            EnumWrittenAsString obj = EnumWrittenAsString.A;

            string json = JsonSerializer.Serialize(obj, ContextWithExplicitStringEnum.Default.EnumWrittenAsString);
            Assert.Equal(Json, json);

            obj = JsonSerializer.Deserialize(Json, ContextWithExplicitStringEnum.Default.EnumWrittenAsString);
            Assert.Equal(EnumWrittenAsString.A, obj);
        }

        [Fact]
        public void EnsureHelperMethodGenerated_ImplicitPropertyFactory()
        {
            // ContextWithImplicitStringEnum does not have an entry for EnumWrittenAsString since it is
            // implictly added by PocoWithEnum. Verify helper methods are still being created properly.

            const string Json = "{\"MyEnum\":\"A\"}";

            PocoWithEnum obj = new() { MyEnum = EnumWrittenAsString.A };

            string json = JsonSerializer.Serialize(obj, ContextWithImplicitStringEnum.Default.PocoWithEnum);
            Assert.Equal(Json, json);

            obj = JsonSerializer.Deserialize(Json, ContextWithImplicitStringEnum.Default.PocoWithEnum);
            Assert.Equal(EnumWrittenAsString.A, obj.MyEnum);
        }
    }
}
