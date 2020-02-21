// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

                value.Name += "Hello!";
                return value;
            }

            public override void Write(Utf8JsonWriter writer, Customer value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                long bytesWrittenSoFar = writer.BytesCommitted + writer.BytesPending;

                JsonSerializer.Serialize(writer, value);

                Debug.Assert(writer.BytesPending == 0);
                long payloadLength =  writer.BytesCommitted - bytesWrittenSoFar;
                writer.WriteNumberValue(payloadLength);
                writer.WriteEndArray();
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

            string result = JsonSerializer.Serialize(customer, options);
            int expectedLength = JsonSerializer.Serialize(customer).Length;
            Assert.Equal(@"[{""CreditLimit"":0,""Name"":""MyNameHello!"",""Address"":{""City"":null}}," + $"{expectedLength}]", result);
        }
    }
}
