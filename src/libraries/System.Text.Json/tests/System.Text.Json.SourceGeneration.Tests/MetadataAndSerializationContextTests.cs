// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable(typeof(Location))]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation")]
    [JsonSerializable(typeof(NumberTypes))]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent))]
    [JsonSerializable(typeof(CampaignSummaryViewModel))]
    [JsonSerializable(typeof(IndexViewModel))]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs))]
    [JsonSerializable(typeof(EmptyPoco))]
    // Ensure no errors when type of member in previously specified object graph is passed as input type to generator.
    [JsonSerializable(typeof(HighLowTemps))]
    [JsonSerializable(typeof(MyType))]
    [JsonSerializable(typeof(MyType2))]
    [JsonSerializable(typeof(MyTypeWithCallbacks))]
    [JsonSerializable(typeof(MyTypeWithPropertyOrdering))]
    [JsonSerializable(typeof(MyIntermediateType))]
    [JsonSerializable(typeof(HighLowTempsImmutable))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass))]
    [JsonSerializable(typeof(object[]))]
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
    internal partial class MetadataAndSerializationContext : JsonSerializerContext, ITestContext
    {
        public JsonSourceGenerationMode JsonSourceGenerationMode => JsonSourceGenerationMode.Default;
    }

    public sealed class MetadataAndSerializationContextTests : RealWorldContextTests
    {
        public MetadataAndSerializationContextTests() : base(MetadataAndSerializationContext.Default, (options) => new MetadataAndSerializationContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.NotNull(MetadataAndSerializationContext.Default.Location.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NumberTypes.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.RepeatedLocation.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.ActiveOrUpcomingEvent.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.CampaignSummaryViewModel.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IndexViewModel.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.WeatherForecastWithPOCOs.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.EmptyPoco.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.HighLowTemps.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyType.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyType2.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyTypeWithCallbacks.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyTypeWithPropertyOrdering.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyIntermediateType.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.HighLowTempsImmutable.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyNestedClass.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyNestedNestedClass.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.ObjectArray.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.SampleEnum.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.String.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.ValueTupleStringInt32Boolean.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithEnumAndNullable.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithNullableProperties.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithCustomConverter);
            Assert.NotNull(MetadataAndSerializationContext.Default.StructWithCustomConverter);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithCustomConverterFactory);
            Assert.NotNull(MetadataAndSerializationContext.Default.StructWithCustomConverterFactory);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithCustomConverterProperty);
            Assert.NotNull(MetadataAndSerializationContext.Default.StructWithCustomConverterProperty);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithCustomConverterFactoryProperty);
            Assert.NotNull(MetadataAndSerializationContext.Default.StructWithCustomConverterFactoryProperty);
            Assert.Throws<InvalidOperationException>(() => MetadataAndSerializationContext.Default.ClassWithBadCustomConverter);
            Assert.Throws<InvalidOperationException>(() => MetadataAndSerializationContext.Default.StructWithBadCustomConverter);
            Assert.Null(MetadataAndSerializationContext.Default.NullablePersonStruct.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.PersonStruct.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.TypeWithValidationAttributes.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.TypeWithDerivedAttribute.SerializeHandler);
        }
    }
}
