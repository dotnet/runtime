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
    [JsonSerializable(typeof(CovariantBaseNotIgnored_CovariantDerivedNotIgnored))]
    [JsonSerializable(typeof(CovariantBaseNotIgnored_CovariantDerivedGenericNotIgnored<string>))]
    [JsonSerializable(typeof(CovariantBaseNotIgnored_CovariantDerivedIgnored))]
    [JsonSerializable(typeof(CovariantBaseNotIgnored_CovariantDerivedGenericIgnored<string>))]
    [JsonSerializable(typeof(CovariantBaseIgnored_CovariantDerivedNotIgnored))]
    [JsonSerializable(typeof(CovariantBaseIgnored_CovariantDerivedGenericNotIgnored<string>))]
    [JsonSerializable(typeof(CovariantBaseIgnored_CovariantDerivedIgnored))]
    [JsonSerializable(typeof(CovariantBaseIgnored_CovariantDerivedGenericIgnored<string>))]
    [JsonSerializable(typeof(IgnoredPropertyBase))]
    [JsonSerializable(typeof(NotIgnoredPropertyBase))]
    [JsonSerializable(typeof(IgnoredPropertyBase_NotIgnoredPropertyDerived))]
    [JsonSerializable(typeof(NotIgnoredPropertyBase_IgnoredPropertyDerived))]
    [JsonSerializable(typeof(NotIgnoredPropertyBase_NotIgnoredPropertyDerived))]
    [JsonSerializable(typeof(IgnoredPropertyBase_IgnoredPropertyDerived))]
    [JsonSerializable(typeof(IgnoredVirtualPropertyBase))]
    [JsonSerializable(typeof(NotIgnoredVirtualPropertyBase))]
    [JsonSerializable(typeof(IgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived))]
    [JsonSerializable(typeof(NotIgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived))]
    [JsonSerializable(typeof(NotIgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived))]
    [JsonSerializable(typeof(IgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived))]
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
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseNotIgnored_CovariantDerivedNotIgnored.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseNotIgnored_CovariantDerivedGenericNotIgnoredString.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseNotIgnored_CovariantDerivedIgnored.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseNotIgnored_CovariantDerivedGenericIgnoredString.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseIgnored_CovariantDerivedNotIgnored.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseIgnored_CovariantDerivedGenericNotIgnoredString.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseIgnored_CovariantDerivedIgnored.SerializeHandler);
            Assert.Null(MetadataAndSerializationContext.Default.CovariantBaseIgnored_CovariantDerivedGenericIgnoredString.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredPropertyBase.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredPropertyBase.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredPropertyBase_NotIgnoredPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredPropertyBase_IgnoredPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredPropertyBase_NotIgnoredPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredPropertyBase_IgnoredPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredVirtualPropertyBase.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredVirtualPropertyBase.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.IgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredVirtualPropertyBase_NotIgnoredOverriddenPropertyDerived.SerializeHandler);
            Assert.NotNull(MetadataAndSerializationContext.Default.NotIgnoredVirtualPropertyBase_IgnoredOverriddenPropertyDerived.SerializeHandler);
        }
    }
}
