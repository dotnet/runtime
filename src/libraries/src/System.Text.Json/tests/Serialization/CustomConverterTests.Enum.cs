// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        public enum MyBoolEnum
        {
            True = 1,   // JSON is "TRUE"
            False = 2,  // JSON is "FALSE"
            Unknown = 3 // JSON is "?"
        }

        // A converter for a specific Enum.
        private class MyBoolEnumConverter : JsonConverter<MyBoolEnum>
        {
            // CanConvert does not need to be implemented here since we only convert MyBoolEnum.

            public override MyBoolEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string enumValue = reader.GetString();
                if (enumValue == "TRUE")
                {
                    return MyBoolEnum.True;
                }

                if (enumValue == "FALSE")
                {
                    return MyBoolEnum.False;
                }

                if (enumValue == "?")
                {
                    return MyBoolEnum.Unknown;
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, MyBoolEnum value, JsonSerializerOptions options)
            {
                if (value is MyBoolEnum.True)
                {
                    writer.WriteStringValue("TRUE");
                }
                else if (value is MyBoolEnum.False)
                {
                    writer.WriteStringValue("FALSE");
                }
                else
                {
                    writer.WriteStringValue("?");
                }
            }
        }

        [Fact]
        public static void CustomEnumConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyBoolEnumConverter());

            {
                MyBoolEnum value = JsonSerializer.Deserialize<MyBoolEnum>(@"""TRUE""", options);
                Assert.Equal(MyBoolEnum.True, value);
                Assert.Equal(@"""TRUE""", JsonSerializer.Serialize(value, options));
            }

            {
                MyBoolEnum value = JsonSerializer.Deserialize<MyBoolEnum>(@"""FALSE""", options);
                Assert.Equal(MyBoolEnum.False, value);
                Assert.Equal(@"""FALSE""", JsonSerializer.Serialize(value, options));
            }

            {
                MyBoolEnum value = JsonSerializer.Deserialize<MyBoolEnum>(@"""?""", options);
                Assert.Equal(MyBoolEnum.Unknown, value);
                Assert.Equal(@"""?""", JsonSerializer.Serialize(value, options));
            }
        }

        [Fact]
        public static void NullableCustomEnumConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyBoolEnumConverter());

            {
                MyBoolEnum? value = JsonSerializer.Deserialize<MyBoolEnum?>(@"null", options);
                Assert.Null(value);
            }

            {
                MyBoolEnum? value = JsonSerializer.Deserialize<MyBoolEnum?>(@"""TRUE""", options);
                Assert.Equal(MyBoolEnum.True, value);
                Assert.Equal(@"""TRUE""", JsonSerializer.Serialize(value, options));
            }

            {
                MyBoolEnum? value = JsonSerializer.Deserialize<MyBoolEnum?>(@"""FALSE""", options);
                Assert.Equal(MyBoolEnum.False, value);
                Assert.Equal(@"""FALSE""", JsonSerializer.Serialize(value, options));
            }

            {
                MyBoolEnum? value = JsonSerializer.Deserialize<MyBoolEnum?>(@"""?""", options);
                Assert.Equal(MyBoolEnum.Unknown, value);
                Assert.Equal(@"""?""", JsonSerializer.Serialize(value, options));
            }
        }
    }
}
