// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// A converter that uses Object as it's type.
        /// </summary>
        private class ObjectToCustomerOrIntConverter : JsonConverter<object>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return (typeToConvert == typeof(Customer) ||
                    typeToConvert == typeof(int));
            }

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (typeToConvert == typeof(Customer))
                {
                    reader.Skip();
                    Customer c = new Customer();
                    c.Name = "HelloWorld";
                    return c;
                }

                if (typeToConvert == typeof(int))
                {
                    return 42;
                }

                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                // Write the name of the type.
                writer.WriteStringValue(value.GetType().ToString());
            }
        }

        [Fact]
        public static void CustomObjectConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectToCustomerOrIntConverter());

            {
                var customer = new Customer();
                string json = JsonSerializer.Serialize<object>(customer, options);
                Assert.Contains(typeof(Customer).ToString(), json);

                json = JsonSerializer.Serialize(customer, options);
                Assert.Contains(typeof(Customer).ToString(), json);
            }

            {
                string json = JsonSerializer.Serialize(42, options);
                Assert.Contains(typeof(int).ToString(), json);
            }

            {
                object obj = JsonSerializer.Deserialize<Customer>("{}", options);
                Assert.IsType<Customer>(obj);
                Assert.Equal("HelloWorld", ((Customer)obj).Name);
            }

            {
                // The converter doesn't handle object.
                object obj = JsonSerializer.Deserialize<object>("{}", options);
                Assert.IsType<JsonElement>(obj);
            }

            {
                int obj = JsonSerializer.Deserialize<int>("0", options);
                Assert.Equal(42, obj);
            }
        }

        /// <summary>
        /// A converter that converts "true" and "false" tokens to a bool.
        /// </summary>
        private class ObjectToBoolConverter : JsonConverter<object>
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

                // Use JsonElement as fallback.
                var converter = options.GetConverter(typeof(JsonElement)) as JsonConverter<JsonElement>;
                if (converter != null)
                {
                    return converter.Read(ref reader, typeToConvert, options);
                }

                // Shouldn't get here.
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new InvalidOperationException("Directly writing object not supported");
            }
        }

        [Fact]
        public static void CustomObjectBoolConverter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectToBoolConverter());

            {
                object obj = JsonSerializer.Deserialize<object>("true", options);
                Assert.IsType<bool>(obj);
                Assert.True((bool)obj);
            }

            {
                object obj = JsonSerializer.Deserialize<object>("false", options);
                Assert.IsType<bool>(obj);
                Assert.False((bool)obj);
            }

            {
                object obj = JsonSerializer.Deserialize<object>("{}", options);
                Assert.IsType<JsonElement>(obj);
            }
        }

        private class ObjectToCustomerConverter : JsonConverter<Customer>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return (typeToConvert == typeof(Customer) || typeToConvert == typeof(DerivedCustomer));
            }

            public override Customer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Customer customer = null;
                if (typeToConvert == typeof(Customer))
                {
                    customer = new Customer();
                }

                if (typeToConvert == typeof(DerivedCustomer))
                {
                    customer = new DerivedCustomer();
                }

                if (customer != null)
                { 
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            return customer;
                        }

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string propertyName = reader.GetString();
                            reader.Read();
                            switch (propertyName)
                            {
                                case "CreditLimit":
                                    decimal creditLimit = reader.GetDecimal();
                                    customer.CreditLimit = creditLimit - 1;
                                    break;
                                case "Address":
                                    string city = reader.GetString();
                                    customer.Address.City = string.IsNullOrEmpty(city) ? "NA" : city;
                                    break;
                                case "Name":
                                    string name = reader.GetString().ToUpper();
                                    customer.Name = name;
                                    break;
                            }
                        }
                    }
                    return customer;
                }

                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", value.Name);
                writer.WriteNumber("CreditLimit", value.CreditLimit);
                writer.WriteString("Address", value.Address.City);
                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void ClassWithFieldHavingCustomConverterTest()
        {
            TestClassWithFieldsHavingCustomConverter testObject = new TestClassWithFieldsHavingCustomConverter
            {
                IntValue = 32,
                Name = "John Doe",
                Customer = new Customer
                {
                    Name = "Customer Doe",
                    CreditLimit = 1000
                },
                DerivedCustomer = new DerivedCustomer
                {
                    Name = "Derived Doe",
                    CreditLimit = 2000,
                    Address = new Address { City = "UB" }
                }
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectToCustomerConverter());

            string json = JsonSerializer.Serialize(testObject, options);

            Assert.Equal("{\"Name\":\"John Doe\"," +
                "\"Customer\":{\"Name\":\"Customer Doe\",\"CreditLimit\":1000,\"Address\":null}," +
                "\"DerivedCustomer\":{\"Name\":\"Derived Doe\",\"CreditLimit\":2000,\"Address\":\"UB\"}," +
                "\"NullDerivedCustomer\":null," +
                "\"IntValue\":32," +
                "\"Message\":null}", json);

            TestClassWithFieldsHavingCustomConverter testObj = JsonSerializer.Deserialize<TestClassWithFieldsHavingCustomConverter>(json, options);

            Assert.Equal(32, testObj.IntValue);
            Assert.Equal("John Doe", testObj.Name);
            Assert.Equal("CUSTOMER DOE", testObj.Customer.Name);
            Assert.Equal("NA", testObj.Customer.Address.City);
            Assert.Equal("DERIVED DOE", testObj.DerivedCustomer.Name);
            Assert.Equal(1999, testObj.DerivedCustomer.CreditLimit);
            Assert.Equal("UB", testObj.DerivedCustomer.Address.City);
            Assert.Null(testObj.NullDerivedCustomer);
        }

        private class TestClassWithFieldsHavingCustomConverter
        {
            public string Name { get; set; }
            public Customer Customer { get; set; }
            public DerivedCustomer DerivedCustomer { get; set; }
            public DerivedCustomer NullDerivedCustomer { get; set; }
            public int IntValue { get; set; }
            public string Message { get; set; }
        }
    }
}
