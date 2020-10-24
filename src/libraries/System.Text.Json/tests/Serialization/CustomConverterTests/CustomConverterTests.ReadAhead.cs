// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private static void VerifyClassWithStringProperties(ClassWithStringProperties obj, int stringSize)
        {
            // The 10 properties will cause buffer boundary cases where the converter requires read-ahead.
            Assert.Equal(new string('0', stringSize), obj.MyString0);
            Assert.Equal(new string('1', stringSize), obj.MyString1);
            Assert.Equal(new string('2', stringSize), obj.MyString2);
            Assert.Equal(new string('3', stringSize), obj.MyString3);
            Assert.Equal(new string('4', stringSize), obj.MyString4);
            Assert.Equal(new string('5', stringSize), obj.MyString5);
            Assert.Equal(new string('6', stringSize), obj.MyString6);
            Assert.Equal(new string('7', stringSize), obj.MyString7);
            Assert.Equal(new string('8', stringSize), obj.MyString8);
            Assert.Equal(new string('9', stringSize), obj.MyString9);
        }

        private static void VerifyExtensionDataStringProperties(ClassWithExtensionData obj, int stringSize)
        {
            // The 10 properties will cause buffer boundary cases where the converter requires read-ahead.
            Assert.Equal(new string('0', stringSize), ((JsonElement)obj.MyOverflow["MyString0"]).ToString());
            Assert.Equal(new string('1', stringSize), ((JsonElement)obj.MyOverflow["MyString1"]).ToString());
            Assert.Equal(new string('2', stringSize), ((JsonElement)obj.MyOverflow["MyString2"]).ToString());
            Assert.Equal(new string('3', stringSize), ((JsonElement)obj.MyOverflow["MyString3"]).ToString());
            Assert.Equal(new string('4', stringSize), ((JsonElement)obj.MyOverflow["MyString4"]).ToString());
            Assert.Equal(new string('5', stringSize), ((JsonElement)obj.MyOverflow["MyString5"]).ToString());
            Assert.Equal(new string('6', stringSize), ((JsonElement)obj.MyOverflow["MyString6"]).ToString());
            Assert.Equal(new string('7', stringSize), ((JsonElement)obj.MyOverflow["MyString7"]).ToString());
            Assert.Equal(new string('8', stringSize), ((JsonElement)obj.MyOverflow["MyString8"]).ToString());
            Assert.Equal(new string('9', stringSize), ((JsonElement)obj.MyOverflow["MyString9"]).ToString());
        }

        private static string CreateTestStringProperty(int stringSize)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("{");

            for (int i = 0; i <= 9; i++)
            {
                builder.Append(@"""MyString");
                builder.Append(i.ToString());
                builder.Append(@""":");
                AppendTestString(i, stringSize, builder);

                if (i != 9)
                {
                    builder.Append(@",");
                }
            }

            builder.Append("}");

            return builder.ToString();
        }

        private static void AppendTestString(int i, int stringSize, StringBuilder builder)
        {
            builder.Append(@"""");
            builder.Append(new string(i.ToString()[0], stringSize));
            builder.Append(@"""");
        }

        [Theory,
            InlineData(1),
            InlineData(10),
            InlineData(100),
            InlineData(1000),
            InlineData(10000),
            InlineData(25000)]
        public static void ReadAheadFromRoot(int stringSize)
        {
            string json = CreateTestStringProperty(stringSize);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ClassWithStringPropertyConverter());

            // Ensure buffer size is small as possible for read-ahead.
            options.DefaultBufferSize = 1;

            byte[] data = Encoding.UTF8.GetBytes(json);

            {
                MemoryStream stream = new MemoryStream(data);
                ClassWithStringProperties obj = JsonSerializer.DeserializeAsync<ClassWithStringProperties>(stream, options).Result;

                VerifyClassWithStringProperties(obj, stringSize);

                string jsonSerialized = JsonSerializer.Serialize(obj, options);
                Assert.Equal(json, jsonSerialized);
            }

            {
                // Verify extension data works with read-ahead. Extension data stored on JsonElement which has a custom converter.
                MemoryStream stream = new MemoryStream(data);
                ClassWithExtensionData obj = JsonSerializer.DeserializeAsync<ClassWithExtensionData>(stream, options).Result;

                VerifyExtensionDataStringProperties(obj, stringSize);

                string jsonSerialized = JsonSerializer.Serialize(obj, options);
                Assert.Equal(json, jsonSerialized);
            }
        }

        [Theory,
            InlineData(1),
            InlineData(10),
            InlineData(100),
            InlineData(1000),
            InlineData(10000)]
        public static void ReadAheadFromProperties(int stringSize)
        {
            string jsonProperties = CreateTestStringProperty(stringSize);

            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            builder.Append(@"""Property1"":");
            builder.Append(jsonProperties);
            builder.Append(@",""Property2"":");
            builder.Append(jsonProperties);
            builder.Append(@",""Property3"":");
            builder.Append(jsonProperties);
            builder.Append("}");

            string json = builder.ToString();

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ClassWithStringPropertyConverter());

            // Ensure buffer size is small as possible for read-ahead.
            options.DefaultBufferSize = 1;

            byte[] data = Encoding.UTF8.GetBytes(json);

            {
                MemoryStream stream = new MemoryStream(data);
                ClassWithNoConverter obj = JsonSerializer.DeserializeAsync<ClassWithNoConverter>(stream, options).Result;

                VerifyClassWithStringProperties(obj.Property1, stringSize);
                VerifyClassWithStringProperties(obj.Property2, stringSize);
                VerifyClassWithStringProperties(obj.Property3, stringSize);

                string jsonSerialized = JsonSerializer.Serialize(obj, options);
                Assert.Equal(json, jsonSerialized);
            }

            {
                // Verify extension data works with read-ahead. Extension data stored on JsonElement which has a custom converter.
                MemoryStream stream = new MemoryStream(data);
                ClassWithExtensionData obj = JsonSerializer.DeserializeAsync<ClassWithExtensionData>(stream, options).Result;
                Assert.NotNull(obj.MyOverflow["Property1"]);
                Assert.NotNull(obj.MyOverflow["Property2"]);
                Assert.NotNull(obj.MyOverflow["Property3"]);

                string jsonSerialized = JsonSerializer.Serialize(obj, options);
                Assert.Equal(json, jsonSerialized);
            }
        }

        [Theory,
            InlineData(1),
            InlineData(10),
            InlineData(100),
            InlineData(1000),
            InlineData(10000)]
        public static void ReadAheadFromArray(int stringSize)
        {
            StringBuilder builder = new StringBuilder("[");
            for (int i = 0; i < 10; i++)
            {
                AppendTestString(i, stringSize, builder);

                if (i != 9)
                {
                    builder.Append(@",");
                }
            }

            builder.Append(@"]");

            string json = builder.ToString();

            var options = new JsonSerializerOptions();
            // No customer converters registered; the built-in string converter converter will be used.

            // Ensure buffer size is small as possible for read-ahead.
            options.DefaultBufferSize = 1;

            byte[] data = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(data);
            string[] arr = JsonSerializer.DeserializeAsync<string[]>(stream, options).Result;

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(new string(i.ToString()[0], stringSize), arr[i]);
            }

            string jsonSerialized = JsonSerializer.Serialize(arr, options);
            Assert.Equal(json, jsonSerialized);
        }

        private class ClassWithStringProperties
        {
            public string MyString0 { get; set; }
            public string MyString1 { get; set; }
            public string MyString2 { get; set; }
            public string MyString3 { get; set; }
            public string MyString4 { get; set; }
            public string MyString5 { get; set; }
            public string MyString6 { get; set; }
            public string MyString7 { get; set; }
            public string MyString8 { get; set; }
            public string MyString9 { get; set; }
        }

        private class ClassWithExtensionData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        /// <summary>
        /// Class without a converter that has properties with a custom converter.
        /// </summary>
        private class ClassWithNoConverter
        {
            public ClassWithStringProperties Property1 { get; set; }
            public ClassWithStringProperties Property2 { get; set; }
            public ClassWithStringProperties Property3 { get; set; }
        }

        /// <summary>
        /// Converter for POCO type with 10 string properties.
        /// </summary>
        private class ClassWithStringPropertyConverter : JsonConverter<ClassWithStringProperties>
        {
            public override ClassWithStringProperties Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                ClassWithStringProperties obj = new ClassWithStringProperties();

                for (int i = 0; i <= 9; i++)
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string propertyName = reader.GetString();
                    reader.Read();
                    string val = reader.GetString();

                    switch (propertyName)
                    {
                        case "MyString0":
                            obj.MyString0 = val;
                            break;
                        case "MyString1":
                            obj.MyString1 = val;
                            break;
                        case "MyString2":
                            obj.MyString2 = val;
                            break;
                        case "MyString3":
                            obj.MyString3 = val;
                            break;
                        case "MyString4":
                            obj.MyString4 = val;
                            break;
                        case "MyString5":
                            obj.MyString5 = val;
                            break;
                        case "MyString6":
                            obj.MyString6 = val;
                            break;
                        case "MyString7":
                            obj.MyString7 = val;
                            break;
                        case "MyString8":
                            obj.MyString8 = val;
                            break;
                        case "MyString9":
                            obj.MyString9 = val;
                            break;
                        default:
                            throw new JsonException();
                    }
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }

                return obj;
            }

            public override void Write(Utf8JsonWriter writer, ClassWithStringProperties value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("MyString0", value.MyString0);
                writer.WriteString("MyString1", value.MyString1);
                writer.WriteString("MyString2", value.MyString2);
                writer.WriteString("MyString3", value.MyString3);
                writer.WriteString("MyString4", value.MyString4);
                writer.WriteString("MyString5", value.MyString5);
                writer.WriteString("MyString6", value.MyString6);
                writer.WriteString("MyString7", value.MyString7);
                writer.WriteString("MyString8", value.MyString8);
                writer.WriteString("MyString9", value.MyString9);

                writer.WriteEndObject();
            }
        }
    }
}
