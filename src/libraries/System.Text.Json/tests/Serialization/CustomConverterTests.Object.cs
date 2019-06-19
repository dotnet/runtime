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
        private class ObjectConverter : JsonConverter<object>
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
            options.Converters.Add(new ObjectConverter());

            {
                var customer = new Customer();
                string json = JsonSerializer.ToString<object>(customer, options);
                Assert.Contains(typeof(Customer).ToString(), json);

                json = JsonSerializer.ToString(customer, options);
                Assert.Contains(typeof(Customer).ToString(), json);
            }

            {
                string json = JsonSerializer.ToString(42, options);
                Assert.Contains(typeof(int).ToString(), json);
            }

            {
                Customer obj = JsonSerializer.Parse<Customer>("{}", options);
                Assert.Equal("HelloWorld", obj.Name);
            }

            {
                int obj = JsonSerializer.Parse<int>("0", options);
                Assert.Equal(42, obj);
            }
        }
    }
}
