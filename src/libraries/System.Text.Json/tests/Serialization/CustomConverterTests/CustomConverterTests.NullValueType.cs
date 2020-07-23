// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>
        /// Allow a conversion of empty string to a null DateTimeOffset?.
        /// </summary>
        public class JsonNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
        {
            public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return default;
                }

                string value = reader.GetString();
                if (value == string.Empty)
                {
                    return default;
                }

                return DateTimeOffset.ParseExact(value, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
            {
                if (!value.HasValue)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(value.Value.ToString("yyyy/MM/dd HH:mm:ss"));
                }
            }
        }

        private class ClassWithNullableAndJsonConverterAttribute
        {
            [JsonConverter(typeof(JsonNullableDateTimeOffsetConverter))]
            public DateTimeOffset? NullableValue { get; set; }
        }

        [Fact]
        public static void ValueConverterForNullableWithJsonConverterAttribute()
        {
            ClassWithNullableAndJsonConverterAttribute obj;

            const string BaselineJson = @"{""NullableValue"":""1989/01/01 11:22:33""}";
            obj = JsonSerializer.Deserialize<ClassWithNullableAndJsonConverterAttribute>(BaselineJson);
            Assert.NotNull(obj.NullableValue);

            const string Json = @"{""NullableValue"":""""}";
            obj = JsonSerializer.Deserialize<ClassWithNullableAndJsonConverterAttribute>(Json);
            Assert.Null(obj.NullableValue);

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""NullableValue"":null", json);
        }

        private class ClassWithNullableAndWithoutJsonConverterAttribute
        {
            public DateTimeOffset? NullableValue { get; set; }
            public List<DateTimeOffset?> NullableValues { get; set; }
        }

        [Fact]
        public static void ValueConverterForNullableWithoutJsonConverterAttribute()
        {
            const string Json = @"{""NullableValue"":"""", ""NullableValues"":[""""]}";
            ClassWithNullableAndWithoutJsonConverterAttribute obj;

            // The json is not valid with the default converter.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithNullableAndWithoutJsonConverterAttribute>(Json));

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new JsonNullableDateTimeOffsetConverter());

            obj = JsonSerializer.Deserialize<ClassWithNullableAndWithoutJsonConverterAttribute>(Json, options);
            Assert.Null(obj.NullableValue);
            Assert.Null(obj.NullableValues[0]);

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""NullableValue"":null", json);
            Assert.Contains(@"""NullableValues"":[null]", json);
        }

        [JsonConverter(typeof(ClassThatCanBeNullDependingOnContentConverter))]
        private class ClassThatCanBeNullDependingOnContent
        {
            public int MyInt { get; set; }
        }

        /// <summary>
        /// Allow a conversion of ClassThatCanBeNullDependingOnContent to null when its MyInt property is 0.
        /// </summary>
        private class ClassThatCanBeNullDependingOnContentConverter : JsonConverter<ClassThatCanBeNullDependingOnContent>
        {
            public override ClassThatCanBeNullDependingOnContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                // Assume a single property.

                reader.Read();
                Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);

                reader.Read();
                int myInt = reader.GetInt16();

                reader.Read();
                Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

                if (myInt == 0)
                {
                    return null;
                }

                return new ClassThatCanBeNullDependingOnContent
                {
                    MyInt = myInt
                };
            }

            public override void Write(Utf8JsonWriter writer, ClassThatCanBeNullDependingOnContent value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                if (value.MyInt == 0)
                {
                    writer.WriteNull("MyInt");
                }
                else
                {
                    writer.WriteNumber("MyInt", value.MyInt);
                }

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void ConverterForClassThatCanBeNullDependingOnContent()
        {
            ClassThatCanBeNullDependingOnContent obj;

            obj = JsonSerializer.Deserialize<ClassThatCanBeNullDependingOnContent>(@"{""MyInt"":5}");
            Assert.Equal(5, obj.MyInt);

            string json;
            json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyInt"":5", json);

            obj.MyInt = 0;
            json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyInt"":null", json);

            obj = JsonSerializer.Deserialize<ClassThatCanBeNullDependingOnContent>(@"{""MyInt"":0}");
            Assert.Null(obj);
        }
    }
}
