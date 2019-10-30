// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class WriteValueTests
    {
        [Fact]
        public static void NullWriterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1, typeof(int)));
        }

        [Fact]
        public static void CanWriteValueToJsonArray()
        {
            using MemoryStream memoryStream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream);

            writer.WriteStartObject();
            writer.WriteStartArray("test");
            JsonSerializer.Serialize<int>(writer, 1);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            string json = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Equal("{\"test\":[1]}", json);
        }

        public class CustomClassWithEscapedProperty
        {
            public int pizza { get; set; }
            public int hello\u6C49\u5B57 { get; set; }
            public int normal { get; set; }
        }

        [Fact]
        public static void SerializeToWriterRoundTripEscaping()
        {
            const string jsonIn = " { \"p\\u0069zza\": 1, \"hello\\u6C49\\u5B57\": 2, \"normal\": 3 }";

            CustomClassWithEscapedProperty input = JsonSerializer.Deserialize<CustomClassWithEscapedProperty>(jsonIn);

            Assert.Equal(1, input.pizza);
            Assert.Equal(2, input.hello\u6C49\u5B57);
            Assert.Equal(3, input.normal);

            string normalizedString = JsonSerializer.Serialize(input);
            Assert.Equal("{\"pizza\":1,\"hello\\u6C49\\u5B57\":2,\"normal\":3}", normalizedString);

            CustomClassWithEscapedProperty inputNormalized = JsonSerializer.Deserialize<CustomClassWithEscapedProperty>(normalizedString);
            Assert.Equal(1, inputNormalized.pizza);
            Assert.Equal(2, inputNormalized.hello\u6C49\u5B57);
            Assert.Equal(3, inputNormalized.normal);

            using MemoryStream memoryStream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream);
            JsonSerializer.Serialize(writer, inputNormalized);
            writer.Flush();

            string json = Encoding.UTF8.GetString(memoryStream.ToArray());

            Assert.Equal(normalizedString, json);
        }
    }
}
