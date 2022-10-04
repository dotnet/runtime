// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Text.Json.Tests.SourceGenRegressionTests.Net60;
using System.Threading.Tasks;
using Xunit;
using HighLowTemps = System.Text.Json.Tests.SourceGenRegressionTests.Net60.HighLowTemps;
using WeatherForecastWithPOCOs = System.Text.Json.Tests.SourceGenRegressionTests.Net60.WeatherForecastWithPOCOs;

namespace System.Text.Json.Tests.SourceGenRegressionTests
{
    public static class Net60RegressionTests
    {
        [Theory]
        [MemberData(nameof(GetSupportedTypeRoundtripData))]
        public static void SupportedTypeRoundtrip<T>(JsonTypeInfo<T> jsonTypeInfo, T value, string expectedJson)
        {
            string json = JsonSerializer.Serialize(value, jsonTypeInfo);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            T deserializedValue = JsonSerializer.Deserialize(json, jsonTypeInfo);
            json = JsonSerializer.Serialize(deserializedValue, jsonTypeInfo);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);
        }

        public static IEnumerable<object[]> GetSupportedTypeRoundtripData()
        {
            var ctx = Net60GeneratedContext.Default;
            yield return Wrap(ctx.Int32, 42, "42");
            yield return Wrap(ctx.DateTimeOffset, DateTimeOffset.MinValue, "\"0001-01-01T00:00:00+00:00\"");
            yield return Wrap(ctx.String, "I am a string", "\"I am a string\"");
            yield return Wrap(ctx.HighLowTemps, new HighLowTemps { Low = 0, High = 5 }, """{"Low":0,"High":5}""");
            yield return Wrap(ctx.ListDateTimeOffset, new List<DateTimeOffset> { DateTimeOffset.MinValue }, "[\"0001-01-01T00:00:00+00:00\"]");
            yield return Wrap(ctx.ClassWithCustomConverter, new ClassWithCustomConverter { Value = 41 }, "42");
            yield return Wrap(ctx.WeatherForecastWithPOCOs, new WeatherForecastWithPOCOs
            {
                Date = DateTimeOffset.MinValue,
                TemperatureCelsius = 10,
                Summary = "I am a string",
                DatesAvailable = new List<DateTimeOffset> { DateTimeOffset.MinValue },
                TemperatureRanges = new Dictionary<string, HighLowTemps>
                {
                    ["key"] = new HighLowTemps { Low = 0, High = 5}
                },
                SummaryWords = new[] { "word1", "word2" },
            },
            """
            {
                "Date" : "0001-01-01T00:00:00+00:00",
                "TemperatureCelsius" : 10,
                "Summary" : "I am a string",
                "DatesAvailable" : [ "0001-01-01T00:00:00+00:00" ],
                "TemperatureRanges" :
                {
                    "key" : { "Low" : 0, "High" : 5 }
                },
                "SummaryWords" : [ "word1", "word2" ]
            }
            """);

            static object[] Wrap<T>(JsonTypeInfo<T> jsonTypeInfo, T value, string expectedJson) => new object[] { jsonTypeInfo, value, expectedJson };
        }

        [Theory]
        [MemberData(nameof(GetSupportedTypeRoundtripData_OptionsBased))]
        public static void SupportedTypeRoundtrip_OptionsBased<T>(T value, string expectedJson)
        {
            string json = JsonSerializer.Serialize(value, Net60GeneratedContext.Default.Options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            T deserializedValue = JsonSerializer.Deserialize<T>(json, Net60GeneratedContext.Default.Options);
            json = JsonSerializer.Serialize(deserializedValue, Net60GeneratedContext.Default.Options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);
        }

        [Fact]
        public static void UnsupportedType_ThrowsInvalidOperationException()
        {
            DateTime value = DateTime.MinValue;
            string json = JsonSerializer.Serialize(value);

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(value, value.GetType(), Net60GeneratedContext.Default));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize(json, value.GetType(), Net60GeneratedContext.Default));
        }

        public static IEnumerable<object[]> GetSupportedTypeRoundtripData_OptionsBased()
        {
            yield return Wrap(new ClassWithCustomConverter { Value = 41 }, "42");
            yield return Wrap(new WeatherForecastWithPOCOs
            {
                Date = DateTimeOffset.MinValue,
                TemperatureCelsius = 10,
                Summary = "I am a string",
                DatesAvailable = new List<DateTimeOffset> { DateTimeOffset.MinValue },
                TemperatureRanges = new Dictionary<string, HighLowTemps>
                {
                    ["key"] = new HighLowTemps { Low = 0, High = 5 }
                },
                SummaryWords = new[] { "word1", "word2" },
            },
            """
            {
                "Date" : "0001-01-01T00:00:00+00:00",
                "TemperatureCelsius" : 10,
                "Summary" : "I am a string",
                "DatesAvailable" : [ "0001-01-01T00:00:00+00:00" ],
                "TemperatureRanges" :
                {
                    "key" : { "Low" : 0, "High" : 5 }
                },
                "SummaryWords" : [ "word1", "word2" ]
            }
            """);

            static object[] Wrap<T>(T value, string expectedJson) => new object[] { value, expectedJson };
        }

        [Fact]
        public static void HighLowTemps_ContextReportsCorrectMetadata()
        {
            JsonTypeInfo<HighLowTemps> jsonTypeInfo = Net60GeneratedContext.Default.HighLowTemps;

            HighLowTemps instance = jsonTypeInfo.CreateObject();
            Assert.Equal(0, instance.Low);
            Assert.Equal(0, instance.High);

            Assert.Equal(2, jsonTypeInfo.Properties.Count);

            JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.Properties[0];
            Assert.Equal("High", jsonPropertyInfo.Name);
            jsonPropertyInfo.Set(instance, 1);
            Assert.Equal(1, instance.High);
            Assert.Equal(1, jsonPropertyInfo.Get(instance));

            jsonPropertyInfo = jsonTypeInfo.Properties[1];
            Assert.Equal("Low", jsonPropertyInfo.Name);
            jsonPropertyInfo.Set(instance, 2);
            Assert.Equal(2, instance.Low);
            Assert.Equal(2, jsonPropertyInfo.Get(instance));
        }

        [Fact]
        public static void CombinedContexts_ThrowsInvalidOperationException()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(Net60GeneratedContext.Default, new DefaultJsonTypeInfoResolver())
            };

            // v6 Contexts do not implement IJsonTypeInfoResolver so combined resolvers will throw by default.
            // We're fine with this since it doesn't introduce any regressions to existing code.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new HighLowTemps(), options));
        }
    }
}
