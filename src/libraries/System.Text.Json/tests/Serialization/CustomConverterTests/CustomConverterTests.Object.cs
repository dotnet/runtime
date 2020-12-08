// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        /// <summary>
        /// A converter that converts System.Object similar to Newtonsoft's JSON.Net.
        /// Only primitives are the same; arrays and objects do not result in the same types.
        /// </summary>
        private class SystemObjectNewtonsoftCompatibleConverter : JsonConverter<object>
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

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt64(out long l))
                    {
                        return l;
                    }

                    return reader.GetDouble();
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    if (reader.TryGetDateTime(out DateTime datetime))
                    {
                        return datetime;
                    }

                    return reader.GetString();
                }

                // Use JsonElement as fallback.
                // Newtonsoft uses JArray or JObject.
                using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.Clone();
                }
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new InvalidOperationException("Should not get here.");
            }
        }

        private class PrimitiveConverter : JsonConverter<object>
        {
            public int ReadCallCount { get; private set; }
            public int WriteCallCount { get; private set; }

            public override bool CanConvert(Type typeToConvert)
                => typeToConvert != typeof(ClassWithPrimitives)
                && typeToConvert != typeof(ClassWithNullablePrimitives);

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                ReadCallCount++;

                if (reader.TokenType == JsonTokenType.True)
                {
                    return true;
                }

                if (reader.TokenType == JsonTokenType.False)
                {
                    return false;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int i))
                    {
                        return i;
                    }

                    return reader.GetDouble();
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    if (reader.TryGetDateTime(out DateTime datetime))
                    {
                        return datetime;
                    }

                    return reader.GetString();
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                WriteCallCount++;

                if (value is int i)
                {
                    writer.WriteNumberValue(i);
                }
                else if (value is bool b)
                {
                    writer.WriteBooleanValue(b);
                }
                else if (value is string s)
                {
                    writer.WriteStringValue(s);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private class ClassWithPrimitives
        {
            public int MyIntProperty { get; set; }
            public bool MyBoolProperty { get; set; }
            public string MyStringProperty { get; set; }
#pragma warning disable 0649
            public int MyIntField;
            public bool MyBoolField;
            public string MyStringField;
#pragma warning restore
        }

        [Fact]
        public static void ClassWithPrimitivesObjectConverter()
        {
            string expected = @"{
""MyIntProperty"":123,
""MyBoolProperty"":true,
""MyStringProperty"":""Hello"",
""MyIntField"":321,
""MyBoolField"":true,
""MyStringField"":""World""
}";

            string json;
            var converter = new PrimitiveConverter();
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            options.Converters.Add(converter);

            {
                var obj = new ClassWithPrimitives
                {
                    MyIntProperty = 123,
                    MyBoolProperty = true,
                    MyStringProperty = "Hello",
                    MyIntField = 321,
                    MyBoolField = true,
                    MyStringField = "World",
                };

                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(6, converter.WriteCallCount);
                JsonTestHelper.AssertJsonEqual(expected, json);
            }
            {
                var obj = JsonSerializer.Deserialize<ClassWithPrimitives>(json, options);

                Assert.Equal(6, converter.ReadCallCount);

                Assert.Equal(123, obj.MyIntProperty);
                Assert.True(obj.MyBoolProperty);
                Assert.Equal("Hello", obj.MyStringProperty);
                Assert.Equal(321, obj.MyIntField);
                Assert.True(obj.MyBoolField);
                Assert.Equal("World", obj.MyStringField);
            }
        }

        private class ClassWithNullablePrimitives
        {
            public int? MyIntProperty { get; set; }
            public bool? MyBoolProperty { get; set; }
            public string MyStringProperty { get; set; }
#pragma warning disable 0649
            public int? MyIntField;
            public bool? MyBoolField;
            public string MyStringField;
#pragma warning restore
        }

        [Fact]
        public static void ClassWithNullablePrimitivesObjectConverter()
        {
            string expected = @"{
""MyIntProperty"":123,
""MyBoolProperty"":true,
""MyStringProperty"":""Hello"",
""MyIntField"":321,
""MyBoolField"":true,
""MyStringField"":""World""
}";

            string json;
            var converter = new PrimitiveConverter();
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            options.Converters.Add(converter);

            {
                var obj = new ClassWithNullablePrimitives
                {
                    MyIntProperty = 123,
                    MyBoolProperty = true,
                    MyStringProperty = "Hello",
                    MyIntField = 321,
                    MyBoolField = true,
                    MyStringField = "World",
                };

                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(6, converter.WriteCallCount);
                JsonTestHelper.AssertJsonEqual(expected, json);
            }
            {
                var obj = JsonSerializer.Deserialize<ClassWithNullablePrimitives>(json, options);

                Assert.Equal(123, obj.MyIntProperty);
                Assert.True(obj.MyBoolProperty);
                Assert.Equal("Hello", obj.MyStringProperty);
                Assert.Equal(321, obj.MyIntField);
                Assert.True(obj.MyBoolField);
                Assert.Equal("World", obj.MyStringField);
            }
        }

        [Fact]
        public static void SystemObjectNewtonsoftCompatibleConverterDeserialize()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new SystemObjectNewtonsoftCompatibleConverter());

            {
                const string Value = @"null";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.Null(obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.Null(newtonsoftObj);
            }

            {
                const string Value = @"""mystring""";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<string>(obj);
                Assert.Equal("mystring", obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<string>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = "true";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<bool>(obj);
                Assert.True((bool)obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<bool>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = "false";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<bool>(obj);
                Assert.False((bool)obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<bool>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = "123";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<long>(obj);
                Assert.Equal((long)123, obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<long>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = "123.45";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<double>(obj);
                Assert.Equal(123.45d, obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<double>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = @"""2019-01-30T12:01:02Z""";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<DateTime>(obj);
                Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<DateTime>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = @"""2019-01-30T12:01:02+01:00""";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<DateTime>(obj);

                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<DateTime>(newtonsoftObj);
                Assert.Equal(newtonsoftObj, obj);
            }

            {
                const string Value = "{}";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<JsonElement>(obj);

                // Types are different.
                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<Newtonsoft.Json.Linq.JObject>(newtonsoftObj);
            }

            {
                const string Value = "[]";

                object obj = JsonSerializer.Deserialize<object>(Value, options);
                Assert.IsType<JsonElement>(obj);

                // Types are different.
                object newtonsoftObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(Value);
                Assert.IsType<Newtonsoft.Json.Linq.JArray>(newtonsoftObj);
            }
        }

        [Fact]
        public static void SystemObjectNewtonsoftCompatibleConverterSerialize()
        {
            static void Verify(JsonSerializerOptions options)
            {
                {
                    string json = JsonSerializer.Serialize<object>(null, options);
                    Assert.Equal("null", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(null);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    const string Value = "mystring";

                    string json = JsonSerializer.Serialize<object>(Value, options);
                    Assert.Equal(@"""mystring""", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(Value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    string json = JsonSerializer.Serialize<object>(true, options);
                    Assert.Equal("true", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(true);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    string json = JsonSerializer.Serialize<object>(false, options);
                    Assert.Equal("false", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(false);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    const long Value = 123;

                    object json = JsonSerializer.Serialize<object>(123, options);
                    Assert.Equal("123", json);

                    object newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(Value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    const double Value = 123.45;

                    object json = JsonSerializer.Serialize<object>(Value, options);
                    Assert.Equal("123.45", json);

                    object newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(Value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    var value = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);

                    string json = JsonSerializer.Serialize<object>(value, options);
                    Assert.Equal(@"""2019-01-30T12:01:02Z""", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    var value = new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0));

                    string json = JsonSerializer.Serialize<object>(value, options);
                    Assert.Equal(@"""2019-01-30T12:01:02+01:00""", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    var value = new object();

                    string json = JsonSerializer.Serialize<object>(new object(), options);
                    Assert.Equal("{}", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    Assert.Equal(newtonsoftJson, json);
                }

                {
                    var value = new int[] { };

                    string json = JsonSerializer.Serialize<object>(value, options);
                    Assert.Equal("[]", json);

                    string newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    Assert.Equal(newtonsoftJson, json);
                }
            }

            // Results should be the same with or without the custom converter since the
            // serializer calls value.GetType() for every property value declared as System.Object.
            Verify(new JsonSerializerOptions());

            var options = new JsonSerializerOptions();
            options.Converters.Add(new SystemObjectNewtonsoftCompatibleConverter());
            Verify(options);
        }
    }
}
