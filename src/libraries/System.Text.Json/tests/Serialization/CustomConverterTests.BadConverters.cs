// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class PocoWithNoBaseClass { }
        private class DerivedCustomer : Customer { }
        private class SuccessException : Exception { }

        private class BadCustomerConverter : JsonConverter<Customer>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                // Say this converter supports all types even though we specify "Customer".
                return true;
            }

            public override Customer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new SuccessException();
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                throw new SuccessException();
            }
        }

        [Fact]
        public static void ContraVariantConverterFail()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new BadCustomerConverter());

            // Incompatible types.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<int>("0", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(0, options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<PocoWithNoBaseClass>("{}", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new PocoWithNoBaseClass(), options));

            // Contravariant to Customer.
            Assert.Throws<SuccessException>(() => JsonSerializer.Deserialize<DerivedCustomer>("{}", options));
            Assert.Throws<SuccessException>(() => JsonSerializer.Serialize(new DerivedCustomer(), options));

            // Covariant to Customer.
            Assert.Throws<SuccessException>(() => JsonSerializer.Deserialize<Customer>("{}", options));
            Assert.Throws<SuccessException>(() => JsonSerializer.Serialize(new Customer(), options));
            Assert.Throws<SuccessException>(() => JsonSerializer.Serialize<Customer>(new DerivedCustomer(), options));

            Assert.Throws<SuccessException>(() => JsonSerializer.Deserialize<Person>("{}", options));
            Assert.Throws<SuccessException>(() => JsonSerializer.Serialize<Person>(new Customer(), options));
            Assert.Throws<SuccessException>(() => JsonSerializer.Serialize<Person>(new DerivedCustomer(), options));
        }

        private class CanConvertNullConverterAttribute : JsonConverterAttribute
        {
            public CanConvertNullConverterAttribute() : base(typeof(int)) { }

            public override JsonConverter CreateConverter(Type typeToConvert)
            {
                return null;
            }
        }

        private class PocoWithNullConverter
        {
            [CanConvertNullConverter]
            public int MyInt { get; set; }
        }

        [Fact]
        public static void AttributeCreateConverterFail()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new PocoWithNullConverter()));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<PocoWithNullConverter>("{}"));
        }

        private class ConverterThatReturnsNull : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return true;
            }

            protected override JsonConverter CreateConverter(Type typeToConvert)
            {
                return null;
            }
        }

        [Fact]
        public static void ConverterThatReturnsNullFail()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ConverterThatReturnsNull());

            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(0, options));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<int>("0", options));
        }

        private class Level1
        {
            public Level1()
            {
                Level2 = new Level2();
                Level2.Level3s = new Level3[] { new Level3() };
            }

            public Level2 Level2 { get; set; }
        }

        private class Level2
        {
            public Level3[] Level3s {get; set; }
        }

        private class Level3
        {
            // If true, read\write too much instead of too little.
            public bool ReadWriteTooMuch { get; set; }
        }

        private class Level3ConverterThatsBad: JsonConverter<Level3>
        {
            public override Level3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

                reader.Read();
                Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);

                reader.Read();
                Assert.True(reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False);

                // Determine if we should read too much.
                if (reader.TokenType == JsonTokenType.True)
                {
                    // Do the correct read.
                    reader.Read();
                    Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

                    // Do an extra read.
                    reader.Read();
                    Assert.Equal(JsonTokenType.EndArray, reader.TokenType);

                    // End on EndObject token so it looks good, but wrong depth.
                    reader.Read();
                    Assert.Equal(JsonTokenType.EndObject, reader.TokenType);
                }

                return new Level3();
            }

            public override void Write(Utf8JsonWriter writer, Level3 value, JsonSerializerOptions options)
            {
                if (value.ReadWriteTooMuch)
                {
                    writer.WriteStartObject();
                }
            }
        }

        [Fact]
        public static void ConverterReadTooLittle()
        {
            const string json = @"{""Level2"":{""Level3s"":[{""ReadWriteTooMuch"":false}]}}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Level3ConverterThatsBad());

            try
            {
                JsonSerializer.Deserialize<Level1>(json, options);
                Assert.True(false, "Expected exception");
            }
            catch (JsonException ex)
            {
                Assert.Contains("$.Level2.Level3s[1]", ex.ToString());
                Assert.Equal(ex.Path, "$.Level2.Level3s[1]");
            }
        }

        [Fact]
        public static void ConverterReadTooMuch()
        {
            const string json = @"{""Level2"":{""Level3s"":[{""ReadWriteTooMuch"":true}]}}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new Level3ConverterThatsBad ());

            try
            {
                JsonSerializer.Deserialize<Level1>(json, options);
                Assert.True(false, "Expected exception");
            }
            catch (JsonException ex)
            {
                Assert.Contains("$.Level2.Level3s[1]", ex.ToString());
                Assert.Equal(ex.Path, "$.Level2.Level3s[1]");
            }
        }

        [Fact]
        public static void ConverterWroteNothing()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Level3ConverterThatsBad());

            // Not writing is allowed.
            string str = JsonSerializer.Serialize(new Level1(), options);
            Assert.False(string.IsNullOrEmpty(str));
        }

        [Fact]
        public static void ConverterWroteTooMuch()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Level3ConverterThatsBad());

            try
            {
                var l1 = new Level1();
                l1.Level2.Level3s[0].ReadWriteTooMuch = true;

                JsonSerializer.Serialize(l1, options);
                Assert.True(false, "Expected exception");
            }
            catch (JsonException ex)
            {
                Assert.Contains("$.Level2.Level3s", ex.ToString());
                Assert.Equal(ex.Path, "$.Level2.Level3s");
            }
        }

        private class PocoWithTwoConvertersOnProperty
        {
            [CanConvertNullConverter]
            [PointConverter]
            public int MyInt { get; set; }
        }

        [Fact]
        public static void PropertyHasMoreThanOneConverter()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new PocoWithTwoConvertersOnProperty()));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<PocoWithTwoConvertersOnProperty>("{}"));
        }

        [CanConvertNullConverter]
        [PointConverter]
        private class PocoWithTwoConverters
        {
            public int MyInt { get; set; }
        }

        [Fact]
        public static void TypeHasMoreThanOneConverter()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new PocoWithTwoConverters()));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<PocoWithTwoConverters>("{}"));
        }
    }
}
