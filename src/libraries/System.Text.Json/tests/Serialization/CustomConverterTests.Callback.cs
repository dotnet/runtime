// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// A converter that calls back in the serializer.
        /// </summary>
        private class CustomerCallbackConverter : JsonConverter<Customer>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(Customer).IsAssignableFrom(typeToConvert);
            }

            public override Customer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // The options are not passed here as that would cause an infinite loop.
                Customer value = JsonSerializer.Deserialize<Customer>(ref reader);

                value.Name = value.Name + "Hello!";
                return value;
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                // todo: there is no WriteValue yet.
                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void ConverterWithCallback()
        {
            const string json = @"{""Name"":""MyName""}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new CustomerCallbackConverter());

            Customer customer = JsonSerializer.Deserialize<Customer>(json, options);
            Assert.Equal("MyNameHello!", customer.Name);
        }
    }
}
