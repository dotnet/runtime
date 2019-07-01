// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Allow a conversion of "null" to a 0 value.
        /// </summary>
        private class Int32NullConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return 0;
                }

                return reader.GetInt32();
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        [Fact]
        public static void ValueTypeConverterForNull()
        {
            // Baseline
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>("null"));
            Assert.Equal(1, JsonSerializer.Deserialize<int>("1"));

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32NullConverter());

            Assert.Equal(0, JsonSerializer.Deserialize<int>("null", options));
            Assert.Equal(1, JsonSerializer.Deserialize<int>("1", options));
        }

        [Fact]
        public static void ValueTypeConverterForNullWithArray()
        {
            const string json = "[null, 1, null]";

            // Baseline
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(json));

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32NullConverter());

            int[] arr = JsonSerializer.Deserialize<int[]>(json, options);
            Assert.Equal(0, arr[0]);
            Assert.Equal(1, arr[1]);
            Assert.Equal(0, arr[2]);
        }
    }
}
