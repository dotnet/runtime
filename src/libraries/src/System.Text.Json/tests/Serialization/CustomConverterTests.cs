// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
