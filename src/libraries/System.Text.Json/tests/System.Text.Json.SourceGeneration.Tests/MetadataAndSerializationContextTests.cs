// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable(typeof(Location))]
    [JsonSerializable(typeof(RepeatedTypes.Location), TypeInfoPropertyName = "RepeatedLocation")]
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
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass))]
    [JsonSerializable(typeof(RealWorldContextTests.MyNestedClass.MyNestedNestedClass))]
    [JsonSerializable(typeof(object[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(RealWorldContextTests.ClassWithEnumAndNullable))]
    internal partial class MetadataAndSerializationContext : JsonSerializerContext, ITestContext
    {
    }

    public sealed class MetadataAndSerializationContextTests : RealWorldContextTests
    {
        public MetadataAndSerializationContextTests() : base(MetadataAndSerializationContext.Default, (options) => new MetadataAndSerializationContext(options)) { }

        [Fact]
        public override void EnsureFastPathGeneratedAsExpected()
        {
            Assert.NotNull(MetadataAndSerializationContext.Default.Location.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.RepeatedLocation.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.ActiveOrUpcomingEvent.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.CampaignSummaryViewModel.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.IndexViewModel.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.WeatherForecastWithPOCOs.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.EmptyPoco.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.HighLowTemps.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyType.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyType2.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyTypeWithCallbacks.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyTypeWithPropertyOrdering.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyIntermediateType.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.HighLowTempsImmutable.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyNestedClass.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.MyNestedNestedClass.Serialize);
            Assert.Null(MetadataAndSerializationContext.Default.ObjectArray.Serialize);
            Assert.Null(MetadataAndSerializationContext.Default.String.Serialize);
            Assert.NotNull(MetadataAndSerializationContext.Default.ClassWithEnumAndNullable.Serialize);
        }
    }
}
