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
    [JsonSerializable(typeof(HighLowTempsRecord), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(byte[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof((string Label1, int Label2, bool)), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithNullableProperties), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterFactory), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithCustomConverterFactoryProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithCustomConverterFactoryProperty), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ClassWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(StructWithBadCustomConverter), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(PersonStruct?), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(TypeWithValidationAttributes), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(TypeWithDerivedAttribute), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(PolymorphicClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
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
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.Location.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.RepeatedLocation.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.NumberTypes.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ActiveOrUpcomingEvent.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.CampaignSummaryViewModel.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.IndexViewModel.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.WeatherForecastWithPOCOs.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.EmptyPoco.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.HighLowTemps.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyType.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyType2.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyIntermediateType.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.HighLowTempsImmutable.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.HighLowTempsRecord.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyNestedClass.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.MyNestedNestedClass.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ObjectArray.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ByteArray.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.SampleEnum.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.String.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ValueTupleStringInt32Boolean.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithEnumAndNullable.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithNullableProperties.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverter.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverter.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.StructWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MetadataWithPerTypeAttributeContext.Default.ClassWithBadCustomConverter.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MetadataWithPerTypeAttributeContext.Default.StructWithBadCustomConverter.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.NullablePersonStruct.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.PersonStruct.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.TypeWithValidationAttributes.SerializeHandler);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.TypeWithDerivedAttribute.SerializeHandler);
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
    [JsonSerializable(typeof(HighLowTempsRecord))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof((string Label1, int Label2, bool)))]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable))]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithNullableProperties))]
    [JsonSerializable(typeof(ClassWithCustomConverter))]
    [JsonSerializable(typeof(StructWithCustomConverter))]
    [JsonSerializable(typeof(ClassWithCustomConverterFactory))]
    [JsonSerializable(typeof(StructWithCustomConverterFactory))]
    [JsonSerializable(typeof(ClassWithCustomConverterProperty))]
    [JsonSerializable(typeof(StructWithCustomConverterProperty))]
    [JsonSerializable(typeof(ClassWithCustomConverterFactoryProperty))]
    [JsonSerializable(typeof(StructWithCustomConverterFactoryProperty))]
    [JsonSerializable(typeof(ClassWithBadCustomConverter))]
    [JsonSerializable(typeof(StructWithBadCustomConverter))]
    [JsonSerializable(typeof(PersonStruct?))]
    [JsonSerializable(typeof(TypeWithValidationAttributes))]
    [JsonSerializable(typeof(TypeWithDerivedAttribute))]
    [JsonSerializable(typeof(PolymorphicClass))]
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
            Assert.Null(MetadataContext.Default.Location.SerializeHandler);
            Assert.Null(MetadataContext.Default.RepeatedLocation.SerializeHandler);
            Assert.Null(MetadataContext.Default.NumberTypes.SerializeHandler);
            Assert.Null(MetadataContext.Default.ActiveOrUpcomingEvent.SerializeHandler);
            Assert.Null(MetadataContext.Default.CampaignSummaryViewModel.SerializeHandler);
            Assert.Null(MetadataContext.Default.IndexViewModel.SerializeHandler);
            Assert.Null(MetadataContext.Default.WeatherForecastWithPOCOs.SerializeHandler);
            Assert.Null(MetadataContext.Default.EmptyPoco.SerializeHandler);
            Assert.Null(MetadataContext.Default.HighLowTemps.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyType.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyType2.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyTypeWithCallbacks.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyTypeWithPropertyOrdering.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyIntermediateType.SerializeHandler);
            Assert.Null(MetadataContext.Default.HighLowTempsImmutable.SerializeHandler);
            Assert.Null(MetadataContext.Default.HighLowTempsRecord.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyNestedClass.SerializeHandler);
            Assert.Null(MetadataContext.Default.MyNestedNestedClass.SerializeHandler);
            Assert.Null(MetadataContext.Default.ObjectArray.SerializeHandler);
            Assert.Null(MetadataContext.Default.ByteArray.SerializeHandler);
            Assert.Null(MetadataContext.Default.SampleEnum.SerializeHandler);
            Assert.Null(MetadataContext.Default.String.SerializeHandler);
            Assert.Null(MetadataContext.Default.ValueTupleStringInt32Boolean.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithEnumAndNullable.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithNullableProperties.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverter.SerializeHandler);
            Assert.Null(MetadataContext.Default.StructWithCustomConverter.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterFactory.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterProperty.SerializeHandler);
            Assert.Null(MetadataContext.Default.ClassWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Null(MetadataContext.Default.StructWithCustomConverterFactoryProperty.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MetadataContext.Default.ClassWithBadCustomConverter.SerializeHandler);
            Assert.Throws<InvalidOperationException>(() => MetadataContext.Default.StructWithBadCustomConverter.SerializeHandler);
            Assert.Null(MetadataContext.Default.NullablePersonStruct.SerializeHandler);
            Assert.Null(MetadataContext.Default.PersonStruct.SerializeHandler);
            Assert.Null(MetadataContext.Default.TypeWithValidationAttributes.SerializeHandler);
            Assert.Null(MetadataContext.Default.TypeWithDerivedAttribute.SerializeHandler);
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
