// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable(typeof(Location), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation", GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(CampaignSummaryViewModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(IndexViewModel), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EmptyPoco), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HighLowTemps), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyType), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyType2), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(MyIntermediateType), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HighLowTempsImmutable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(string), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class MetadataWithPerTypeAttributeContext : JsonSerializerContext, ITestContext
    {
    }

    public sealed class MetadataWithPerTypeAttributeContextTests : RealWorldContextTests
    {
        public MetadataWithPerTypeAttributeContextTests() : base(MetadataWithPerTypeAttributeContext.Default, (options) => new MetadataWithPerTypeAttributeContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.Location.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.RepeatedLocation.Serialize);
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
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.String.Serialize);
            Assert.Null(MetadataWithPerTypeAttributeContext.Default.ClassWithEnumAndNullable.Serialize);
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Location))]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation")]
    [JsonSerializable(typeof(ActiveOrUpcomingEvent))]
    [JsonSerializable(typeof(CampaignSummaryViewModel))]
    [JsonSerializable(typeof(IndexViewModel))]
    [JsonSerializable(typeof(WeatherForecastWithPOCOs))]
    [JsonSerializable(typeof(EmptyPoco))]
    [JsonSerializable(typeof(HighLowTemps))]
    [JsonSerializable(typeof(MyType))]
    [JsonSerializable(typeof(MyType2))]
    [JsonSerializable(typeof(MyIntermediateType))]
    [JsonSerializable(typeof(HighLowTempsImmutable))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable))]
    internal partial class MetadataContext : JsonSerializerContext, ITestContext
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
            Assert.Null(MetadataContext.Default.ActiveOrUpcomingEvent.Serialize);
            Assert.Null(MetadataContext.Default.CampaignSummaryViewModel.Serialize);
            Assert.Null(MetadataContext.Default.IndexViewModel.Serialize);
            Assert.Null(MetadataContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.Null(MetadataContext.Default.EmptyPoco.Serialize);
            Assert.Null(MetadataContext.Default.HighLowTemps.Serialize);
            Assert.Null(MetadataContext.Default.MyType.Serialize);
            Assert.Null(MetadataContext.Default.MyType2.Serialize);
            Assert.Null(MetadataContext.Default.MyIntermediateType.Serialize);
            Assert.Null(MetadataContext.Default.HighLowTempsImmutable.Serialize);
            Assert.Null(MetadataContext.Default.MyNestedClass.Serialize);
            Assert.Null(MetadataContext.Default.MyNestedNestedClass.Serialize);
            Assert.Null(MetadataContext.Default.ObjectArray.Serialize);
            Assert.Null(MetadataContext.Default.String.Serialize);
            Assert.Null(MetadataContext.Default.ClassWithEnumAndNullable.Serialize);
        }
    }
}
