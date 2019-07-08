// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.Tests.Serialization
{
    /// <summary>
    /// Catch-all location for combined read scenarios (user reported and otherwise) that aren't
    /// specific to one serialization feature.
    /// </summary>
    public class ReadScenarioTests
    {
        [Fact]
        public void StringEnumUriAndCustomDateTimeConverter()
        {
            // Validating a scenario reported with https://github.com/dotnet/corefx/issues/38568.
            // Our DateTime parsing is ISO 8601 strict, more flexible parsing is possible by
            // writing a simple converter. String based enum parsing is handled by registering
            // a custom built-in parser (JsonStringEnumConverter). Uri is handled implicitly.

            string json =
                @"{" +
                    @"""picture"": ""http://placehold.it/32x32""," +
                    @"""eyeColor"": ""Brown""," +
                    @"""registered"": ""2015-05-30T01:50:21 -01:00""" +
                @"}";

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new Types38568.MyDateTimeConverter()
                }
            };

            Types38568.Model model = JsonSerializer.Deserialize<Types38568.Model>(json, options);
            Assert.Equal(Types38568.Color.Brown, model.EyeColor);
            Assert.Equal(@"http://placehold.it/32x32", model.Picture.OriginalString);
            Assert.Equal(DateTime.Parse("2015-05-30T01:50:21 -01:00"), model.Registered);
        }

        public class Types38568
        {
            // The built-in DateTime parser is stricter than DateTime.Parse.
            public class MyDateTimeConverter : JsonConverter<DateTime>
            {
                public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                    => DateTime.Parse(reader.GetString());

                public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
                    => writer.WriteStringValue(value.ToString("O"));
            }

            public sealed class Model
            {
                public Color EyeColor { get; set; }
                public Uri Picture { get; set; }
                public DateTime Registered { get; set; }
            }

            public enum Color
            {
                Blue,
                Green,
                Brown
            }
        }
    }
}
