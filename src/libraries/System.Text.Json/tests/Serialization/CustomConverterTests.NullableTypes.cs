// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class JsonTestStructConverter : JsonConverter<TestStruct>
        {
            public override TestStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new TestStruct
                {
                    InnerValue = reader.GetInt32()
                };
            }

            public override void Write(Utf8JsonWriter writer, TestStruct value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.InnerValue);
            }
        }

        private class JsonTestStructThrowingConverter : JsonConverter<TestStruct>
        {
            public override TestStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, TestStruct value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        private struct TestStruct
        {
            public int InnerValue { get; set; }
        }

        private class TestStructClass
        {
            [JsonConverter(typeof(JsonTestStructConverter))]
            public TestStruct? MyStruct { get; set; }
        }

        private class TestStructInvalidClass
        {
            // Note: JsonTestStructConverter does not convert int, this is for negative testing.
            [JsonConverter(typeof(JsonTestStructConverter))]
            public int? MyInt { get; set; }
        }

        [Fact]
        public static void NullableCustomValueTypeUsingOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonTestStructConverter());

            {
                TestStruct myStruct = JsonSerializer.Deserialize<TestStruct>("1", options);
                Assert.Equal(1, myStruct.InnerValue);
            }

            {
                TestStruct? myStruct = JsonSerializer.Deserialize<TestStruct?>("null", options);
                Assert.False(myStruct.HasValue);
            }

            {
                TestStruct? myStruct = JsonSerializer.Deserialize<TestStruct?>("1", options);
                Assert.Equal(1, myStruct.Value.InnerValue);
            }
        }

        [Fact]
        public static void NullableCustomValueTypeUsingAttributes()
        {
            {
                TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":null}");
                Assert.False(myStructClass.MyStruct.HasValue);
            }

            {
                TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":1}");
                Assert.True(myStructClass.MyStruct.HasValue);
                Assert.Equal(1, myStructClass.MyStruct.Value.InnerValue);
            }
        }

        [Fact]
        public static void NullableCustomValueTypeChoosesAttributeOverOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonTestStructThrowingConverter());

            // Chooses JsonTestStructThrowingConverter on options, which will throw.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<TestStruct?>("1", options));

            // Chooses JsonTestStructConverter on attribute, which will not throw.
            TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":null}", options);
            Assert.False(myStructClass.MyStruct.HasValue);
        }

        [Fact]
        public static void NullableCustomValueTypeNegativeTest()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<TestStructInvalidClass>(@"{""MyInt"":null}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<TestStructInvalidClass>(@"{""MyInt"":1}"));
        }

        [Fact]
        public static void NullableStandardValueTypeTest()
        {
            {
                int? myInt = JsonSerializer.Deserialize<int?>("null");
                Assert.False(myInt.HasValue);
            }

            {
                int? myInt = JsonSerializer.Deserialize<int?>("1");
                Assert.Equal(1, myInt.Value);
            }
        }
    }
}
