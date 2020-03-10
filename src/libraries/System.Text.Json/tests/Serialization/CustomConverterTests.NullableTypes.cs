// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Diagnostics;
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

        /// <summary>
        /// Used to verify a converter for Nullable is called for null values. Converters are passed
        /// null when {T} is a value type including when {T}=Nullable{int?}.
        /// </summary>
        private class NullIntTo42Converter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    // Use literal value to differ from the default converter's behavior.
                    return 42;
                }

                Debug.Assert(false);
                throw new Exception("not expected");
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    // Use literal value to differ from the default converter's behavior.
                    writer.WriteNumberValue(42);
                }
                else
                {
                    Debug.Assert(false);
                    throw new Exception("not expected");
                }
            }
        }

        [Fact]
        public static void NullableConverterIsPassedNull()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NullIntTo42Converter());

            {
                int? myInt = JsonSerializer.Deserialize<int?>("null", options);
                Assert.True(myInt.HasValue);
                Assert.Equal(42, myInt.Value);
            }

            {
                string json = JsonSerializer.Serialize<int?>(null, options);
                Assert.Equal("42", json);
            }

            {
                int?[] ints = JsonSerializer.Deserialize<int?[]>("[null, null]", options);
                Assert.Equal(2, ints.Length);
                Assert.Equal(42, ints[0]);
                Assert.Equal(42, ints[1]);
            }

            {
                string json = JsonSerializer.Serialize<int?[]>(new int?[] { null, null }, options);
                Assert.Equal("[42,42]", json);
            }
        }

        private class PocoSingleInt
        {
            public int MyInt;
        }

        /// <summary>
        /// Used to verify a converter for a reference type is not called for null values.
        /// </summary>
        private class PocoFailOnNullConverter : JsonConverter<PocoSingleInt>
        {
            public override PocoSingleInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    Debug.Assert(false);
                    throw new Exception();
                }

                reader.Skip();

                return new PocoSingleInt()
                {
                    // Use literal value to differ from the default converter's behavior.
                    MyInt = 42
                };
            }

            public override void Write(Utf8JsonWriter writer, PocoSingleInt value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    Debug.Assert(false);
                    throw new Exception();
                }

                writer.WriteStartObject();

                // Use literal value to differ from the default converter's behavior.
                writer.WriteNumber("MyInt", 42);

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void ReferenceTypeConverterDoesntGetPassedNull()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PocoFailOnNullConverter());

            {
                PocoSingleInt poco = JsonSerializer.Deserialize<PocoSingleInt>("null", options);
                Assert.Null(poco);

                poco = JsonSerializer.Deserialize<PocoSingleInt>("{}", options);
                Assert.Equal(42, poco.MyInt);
            }

            {
                PocoSingleInt[] pocos = JsonSerializer.Deserialize<PocoSingleInt[]>("[null, null]", options);
                Assert.Equal(2, pocos.Length);
                Assert.Null(pocos[0]);
                Assert.Null(pocos[1]);

                pocos = JsonSerializer.Deserialize<PocoSingleInt[]>("[{}, {}]", options);
                Assert.Equal(2, pocos.Length);
                Assert.Equal(42, pocos[0].MyInt);
                Assert.Equal(42, pocos[1].MyInt);
            }

            {
                string json = JsonSerializer.Serialize<PocoSingleInt>(null, options);
                Assert.Equal(@"null", json);

                PocoSingleInt poco = new PocoSingleInt();
                json = JsonSerializer.Serialize<PocoSingleInt>(poco, options);
                Assert.Equal(@"{""MyInt"":42}", json);
            }

            {
                string json = JsonSerializer.Serialize<PocoSingleInt[]>(new PocoSingleInt[] { null, null }, options);
                Assert.Equal(@"[null,null]", json);

                PocoSingleInt poco = new PocoSingleInt();
                json = JsonSerializer.Serialize<PocoSingleInt[]>(new PocoSingleInt[] { poco, poco }, options);
                Assert.Equal(@"[{""MyInt"":42},{""MyInt"":42}]", json);
            }
        }
    }
}
