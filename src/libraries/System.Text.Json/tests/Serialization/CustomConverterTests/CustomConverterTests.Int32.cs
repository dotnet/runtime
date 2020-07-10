// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Allow both string and number values on deserialize.
        /// </summary>
        private class Int32Converter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string stringValue = reader.GetString();
                    if (int.TryParse(stringValue, out int value))
                    {
                        return value;
                    }
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    return reader.GetInt32();
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        [Fact]
        public static void OverrideDefaultConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32Converter());

            {
                int myInt = JsonSerializer.Deserialize<int>("1", options);
                Assert.Equal(1, myInt);
            }

            {
                int myInt = JsonSerializer.Deserialize<int>(@"""1""", options);
                Assert.Equal(1, myInt);
            }
        }
    }
}
