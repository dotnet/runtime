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

            public override void Write(Utf8JsonWriter writer, object value, JsonEncodedText propertyName, JsonSerializerOptions options)
            {
                writer.WriteString(propertyName, value.GetType().ToString());
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

            public override void Write(Utf8JsonWriter writer, object value, JsonEncodedText propertyName, JsonSerializerOptions options)
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
    }
}
