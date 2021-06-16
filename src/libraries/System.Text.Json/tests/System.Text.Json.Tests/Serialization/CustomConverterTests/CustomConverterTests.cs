// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void MultipleConvertersInObjectArray()
        {
            const string expectedJson = @"[""?"",{""TypeDiscriminator"":1,""CreditLimit"":100,""Name"":""C""},null]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyBoolEnumConverter());
            options.Converters.Add(new PersonConverterWithTypeDiscriminator());

            Customer customer = new Customer();
            customer.CreditLimit = 100;
            customer.Name = "C";

            MyBoolEnum myBoolEnum = MyBoolEnum.Unknown;
            MyBoolEnum? myNullBoolEnum = null;

            string json = JsonSerializer.Serialize(new object[] { myBoolEnum, customer, myNullBoolEnum }, options);
            Assert.Equal(expectedJson, json);

            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json, options);
            string jsonElementString = jsonElement.ToString();
            Assert.Equal(expectedJson, jsonElementString);
        }

        [Fact]
        public static void OptionsArePassedToCreateConverter()
        {
            TestFactory factory = new TestFactory();
            JsonSerializerOptions options = new JsonSerializerOptions { Converters = { factory } };
            string json = JsonSerializer.Serialize("Test", options);
            Assert.Equal(@"""Test""", json);
            Assert.Same(options, factory.Options);
        }

        public class TestFactory : JsonConverterFactory
        {
            public JsonSerializerOptions Options { get; private set; }

            public override bool CanConvert(Type typeToConvert) => true;

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                Options = options;
                return new SimpleConverter();
            }

            public class SimpleConverter : JsonConverter<string>
            {
                public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    throw new NotImplementedException();
                }

                public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                    => writer.WriteStringValue(value);
            }
        }

        private class ConverterReturningNull : JsonConverter<Customer>
        {
            public override Customer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

                bool rc = reader.Read();
                Assert.True(rc);

                Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

                return null;
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void VerifyConverterWithTrailingWhitespace()
        {
            string json = "{}   ";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());

            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            // The serializer will finish reading the whitespace and no exception will be thrown.
            Customer c = JsonSerializer.Deserialize<Customer>(utf8, options);

            Assert.Null(c);
        }

        [Fact]
        public static void VerifyConverterWithTrailingComments()
        {
            string json = "{}  //";
            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            // Disallow comments
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Customer>(utf8, options));

            // Skip comments
            options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterReturningNull());
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            Customer c = JsonSerializer.Deserialize<Customer>(utf8, options);
            Assert.Null(c);
        }

        public class ObjectBoolConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.True)
                {
                    return true;
                }

                if (reader.TokenType == JsonTokenType.False)
                {
                    return false;
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void VerifyObjectConverterWithPreservedReferences()
        {
            var json = "true";
            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            var options = new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.Preserve,
            };
            options.Converters.Add(new ObjectBoolConverter());

            object obj = (JsonSerializer.Deserialize<object>(utf8, options));

            Assert.IsType<bool>(obj);
            Assert.Equal(true, obj);
        }

        [Fact]
        public static void GetConverterRootsBuiltInConverters()
        {
            JsonSerializerOptions options = new();
            RunTest<DateTime>();
            RunTest<Point_2D>();

            void RunTest<TConverterReturn>()
            {
                JsonConverter converter = options.GetConverter(typeof(TConverterReturn));
                Assert.NotNull(converter);
                Assert.True(converter is JsonConverter<TConverterReturn>);
            }
        }

        [Fact]
        public static void GetConverterTypeToConvertNull()
        {
            Assert.Throws<ArgumentNullException>(() => (new JsonSerializerOptions()).GetConverter(typeToConvert: null!));
        }
    }
}
