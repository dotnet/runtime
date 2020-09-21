// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
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

        // A custom enum converter that uses an array for containing bit flags.
        private sealed class JsonStringEnumArrayConverter : JsonConverterFactory
        {
            public JsonStringEnumArrayConverter() { }

            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert.IsEnum;
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                    typeof(JsonConverterEnumArray<>).MakeGenericType(typeToConvert),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: null,
                    culture: null);

                return converter;
            }
        }

        internal class JsonConverterEnumArray<T> : JsonConverter<T>
            where T : struct, Enum
        {
            public override bool CanConvert(Type type)
            {
                return type.IsEnum;
            }

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                string enumString = default;

                bool first = true;
                while (true)
                {
                    reader.Read();

                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException();
                    }

                    string enumValue = reader.GetString();
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        enumString += ",";
                    }

                    enumString += enumValue.ToString();
                }

                if (!Enum.TryParse(enumString, out T value))
                {
                    throw new JsonException();
                }

                return value;
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                string[] enumString = value.ToString().Split(',');

                writer.WriteStartArray();

                foreach (string enumValue in enumString)
                {
                    writer.WriteStringValue(enumValue.TrimStart());
                }

                writer.WriteEndArray();
            }
        }

        [Flags]
        [JsonConverter(typeof(JsonStringEnumArrayConverter))]
        public enum eDevice : uint
        {
            Unknown = 0x0,
            Phone = 0x1,
            PC = 0x2,
            Laptop = 0x4,
            Tablet = 0x8,
            IoT = 0x10,
            Watch = 0x20,
            TV = 0x40,
            Hub = 0x80,
            Studio = 0x100,
            Book = 0x200,
            MediaCenter = 0x400,
        }

        [Theory]
        [InlineData(@"[""PC"",""Tablet""]")]
        [InlineData(@"[""Tablet"",""PC""]")]
        public static void EnumValue(string json)
        {
            eDevice obj;

            void Verify()
            {
                Assert.Equal(eDevice.PC | eDevice.Tablet, obj);
            }

            obj = JsonSerializer.Deserialize<eDevice>(json);
            Verify();

            // Round-trip and verify.
            json = JsonSerializer.Serialize(obj);
            obj = JsonSerializer.Deserialize<eDevice>(json);
            Verify();
        }

        private class ConnectionList
        {
            public List<ConnectionFile> Connections { get; set; }
        }

        private class ConnectionFile
        {
            public eDevice Device { get; set; }
        }

        [Theory]
        [InlineData(@"{""Connections"":[{""Device"":[""PC"",""Tablet""]},{""Device"":[""PC"",""Laptop""]}]}")]
        [InlineData(@"{""Connections"":[{""Device"":[""Tablet"",""PC""]},{""Device"":[""Laptop"",""PC""]}]}")]
        public static void EnumArray(string json)
        {
            ConnectionList obj;

            void Verify()
            {
                Assert.Equal(2, obj.Connections.Count);
                Assert.Equal(eDevice.PC | eDevice.Tablet, obj.Connections[0].Device);
                Assert.Equal(eDevice.PC | eDevice.Laptop, obj.Connections[1].Device);
            }

            obj = JsonSerializer.Deserialize<ConnectionList>(json);
            Verify();

            // Round-trip and verify.
            json = JsonSerializer.Serialize(obj);
            obj = JsonSerializer.Deserialize<ConnectionList>(json);
            Verify();
        }
    }
}
