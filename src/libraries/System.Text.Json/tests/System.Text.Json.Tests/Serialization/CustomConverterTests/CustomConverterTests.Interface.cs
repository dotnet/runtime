// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [JsonConverter(typeof(MyInterfaceConverter))]
        private interface IMyInterface
        {
            int IntValue { get; set; }
            string StringValue { get; set; }
        }

        // A custom converter that writes and reads the string property as a top-level value
        private class MyInterfaceConverter : JsonConverter<IMyInterface>
        {
            public override IMyInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new MyClass
                {
                    IntValue = 42,
                    StringValue = reader.GetString()
                };

            public override void Write(Utf8JsonWriter writer, IMyInterface value, JsonSerializerOptions options) => writer.WriteStringValue(value.StringValue);
        }

        private class MyClass : IMyInterface
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        [Fact]
        public static void CustomInterfaceConverter_Serialization()
        {
            IMyInterface value = new MyClass { IntValue = 11, StringValue = "myString" };

            string expectedJson = "\"myString\"";
            string actualJson = JsonSerializer.Serialize(value);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public static void CustomInterfaceConverter_Deserialization()
        {
            string json = "\"myString\"";

            IMyInterface result = JsonSerializer.Deserialize<IMyInterface>(json);

            Assert.IsType<MyClass>(result);
            Assert.Equal("myString", result.StringValue);
            Assert.Equal(42, result.IntValue);
        }
    }
}
