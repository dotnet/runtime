// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ReadValueTests
    {
        [Fact]
        public static void NullTypeThrows()
        {
            Assert.ThrowsAny<ArgumentNullException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.ReadValue(ref reader, null);
            });
        }

        [Fact]
        public static void SerializerOptionsStillApply()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            byte[] utf8 = Encoding.UTF8.GetBytes(@"{""myint16"":1}");
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);

            SimpleTestClass obj = JsonSerializer.ReadValue<SimpleTestClass>(ref reader, options);
            Assert.Equal(1, obj.MyInt16);

            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);
        }

        [Fact]
        public static void ReaderOptionsWinMaxDepth()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[[]]");

            var readerOptions = new JsonReaderOptions
            {
                MaxDepth = 1,
            };

            var serializerOptions = new JsonSerializerOptions
            {
                MaxDepth = 5,
            };

            var state = new JsonReaderState(readerOptions);

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                JsonSerializer.ReadValue(ref reader, typeof(int), serializerOptions);
            });
        }

        [Fact]
        public static void ReaderOptionsWinTrailingCommas()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, 3,]");

            var serializerOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
            };

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state: default);
                JsonSerializer.ReadValue(ref reader, typeof(int), serializerOptions);
            });
        }

        [Fact]
        public static void ReaderOptionsWinComments()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, 3]/* some comment */");

            var serializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state: default);
                JsonSerializer.ReadValue(ref reader, typeof(int), serializerOptions);
            });
        }

        [Fact]
        public static void OnInvalidReaderIsRestored()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, 3}");

            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            long previous = reader.BytesConsumed;

            try
            {
                JsonSerializer.ReadValue(ref reader, typeof(int[]));
                Assert.True(false, "Expected ReadValue to throw JsonException for invalid JSON.");
            }
            catch (JsonException) { }

            Assert.Equal(previous, reader.BytesConsumed);
            Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
        }

        [Fact]
        public static void DataRemaining()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("{\"Foo\":\"abc\", \"Bar\":123}");

            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();

            SimpleType instance = JsonSerializer.ReadValue<SimpleType>(ref reader);
            Assert.Equal("abc", instance.Foo);

            Assert.Equal(utf8.Length, reader.BytesConsumed);
            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);
        }

        public class SimpleType
        {
            public string Foo { get; set; }
        }

        [Fact]
        public static void ReadPropertyName()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("{\"Foo\":[1, 2, 3]}");

            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);

            try
            {
                JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);
                Assert.True(false, "Expected ReadValue to throw JsonException for type mismatch.");
            }
            catch (JsonException) { }

            Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);

            int[] instance = JsonSerializer.ReadValue<int[]>(ref reader);
            Assert.Equal(new int[] { 1, 2, 3 }, instance);

            Assert.Equal(utf8.Length - 1, reader.BytesConsumed);
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);

            Assert.True(reader.Read());
            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ReadObjectMultiSegment(bool isFinalBlock)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, {\"Foo\":[1, 2, 3]}]");
            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8);

            var reader = new Utf8JsonReader(sequence, isFinalBlock, state: default);
            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();
            Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

            SimpleTypeWithArray instance = JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);

            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);
            Assert.Equal(new int[] { 1, 2, 3 }, instance.Foo);
            Assert.Equal(utf8.Length - 1, reader.BytesConsumed);

            Assert.True(reader.Read());
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void NotEnoughData(bool isFinalBlock)
        {
            {
                byte[] utf8 = Encoding.UTF8.GetBytes("\"start of string");

                var reader = new Utf8JsonReader(utf8, isFinalBlock, state: default);
                Assert.Equal(0, reader.BytesConsumed);
                Assert.Equal(JsonTokenType.None, reader.TokenType);

                try
                {
                    JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);
                    Assert.True(false, "Expected ReadValue to throw JsonException for not enough data.");
                }
                catch (JsonException) { }

                Assert.Equal(0, reader.BytesConsumed);
                Assert.Equal(JsonTokenType.None, reader.TokenType);
            }

            {
                byte[] utf8 = Encoding.UTF8.GetBytes("{");

                var reader = new Utf8JsonReader(utf8, isFinalBlock, state: default);
                reader.Read();

                Assert.Equal(1, reader.BytesConsumed);
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

                try
                {
                    JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);
                    Assert.True(false, "Expected ReadValue to throw JsonException for not enough data.");
                }
                catch (JsonException) { }

                Assert.Equal(1, reader.BytesConsumed);
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
            }
        }

        [Fact]
        public static void EndObjectOrArrayIsInvalid()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[{}]");

            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            reader.Read();
            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

            try
            {
                JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);
                Assert.True(false, "Expected ReadValue to throw JsonException for invalid token.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(3, ex.BytePositionInLine);
                Assert.Equal("$", ex.Path);
            }

            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

            reader.Read();
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);

            try
            {
                JsonSerializer.ReadValue<SimpleTypeWithArray>(ref reader);
                Assert.True(false, "Expected ReadValue to throw JsonException for invalid token.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(4, ex.BytePositionInLine);
                Assert.Equal("$", ex.Path);
            }

            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
        }

        public class SimpleTypeWithArray
        {
            public int[] Foo { get; set; }
        }

        [Theory]
        [InlineData("1234", typeof(int), 1234)]
        [InlineData("null", typeof(string), null)]
        [InlineData("true", typeof(bool), true)]
        [InlineData("false", typeof(bool), false)]
        [InlineData("\"my string\"", typeof(string), "my string")]
        public static void Primitives(string jsonString, Type type, object expected)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            Assert.Equal(JsonTokenType.None, reader.TokenType);

            object obj = JsonSerializer.ReadValue(ref reader, type);
            Assert.False(reader.HasValueSequence);
            Assert.Equal(utf8.Length, reader.BytesConsumed);
            Assert.Equal(expected, obj);

            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("1234", typeof(int), 1234)]
        [InlineData("null", typeof(string), null)]
        [InlineData("true", typeof(bool), true)]
        [InlineData("false", typeof(bool), false)]
        [InlineData("\"my string\"", typeof(string), "my string")]
        public static void PrimitivesMultiSegment(string jsonString, Type type, object expected)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8);

            var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            Assert.Equal(JsonTokenType.None, reader.TokenType);

            object obj = JsonSerializer.ReadValue(ref reader, type);
            Assert.True(reader.HasValueSequence);
            Assert.Equal(utf8.Length, reader.BytesConsumed);
            Assert.Equal(expected, obj);

            Assert.False(reader.Read());
        }

        [Fact]
        public static void EnableComments()
        {
            string json = "3";

            var options = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Allow,
            };

            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                    JsonSerializer.ReadValue(ref reader, typeof(int));
                });

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                    JsonSerializer.ReadValue(ref reader, typeof(int));
                });

            AssertExtensions.Throws<ArgumentException>(
               "reader",
               () =>
               {
                   var state = new JsonReaderState(options);
                   var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                   JsonSerializer.ReadValue<int>(ref reader);
               });

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                    JsonSerializer.ReadValue<int>(ref reader);
                });
        }

        [Fact]
        public static void ReadDefaultReader()
        {
            Assert.ThrowsAny<JsonException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.ReadValue(ref reader, typeof(int));
            });

            Assert.ThrowsAny<JsonException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.ReadValue<int>(ref reader);
            });
        }

        [Fact]
        public static void ReadSimpleStruct()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(SimpleTestStruct.s_json);
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            SimpleTestStruct testStruct = JsonSerializer.ReadValue<SimpleTestStruct>(ref reader);
            testStruct.Verify();

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            object obj = JsonSerializer.ReadValue(ref reader, typeof(SimpleTestStruct));
            ((SimpleTestStruct)obj).Verify();
        }

        [Fact]
        public static void ReadClasses()
        {
            {
                byte[] utf8 = Encoding.UTF8.GetBytes(TestClassWithNestedObjectInner.s_json);
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
                TestClassWithNestedObjectInner testStruct = JsonSerializer.ReadValue<TestClassWithNestedObjectInner>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithNestedObjectInner));
                ((TestClassWithNestedObjectInner)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithNestedObjectOuter.s_data, isFinalBlock: true, state: default);
                TestClassWithNestedObjectOuter testStruct = JsonSerializer.ReadValue<TestClassWithNestedObjectOuter>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithNestedObjectOuter.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithNestedObjectOuter));
                ((TestClassWithNestedObjectOuter)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectList.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectList testStruct = JsonSerializer.ReadValue<TestClassWithObjectList>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectList.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithObjectList));
                ((TestClassWithObjectList)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectArray.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectArray testStruct = JsonSerializer.ReadValue<TestClassWithObjectArray>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectArray.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithObjectArray));
                ((TestClassWithObjectArray)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectIEnumerableT.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectIEnumerableT testStruct = JsonSerializer.ReadValue<TestClassWithObjectIEnumerableT>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectIEnumerableT.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithObjectIEnumerableT));
                ((TestClassWithObjectIEnumerableT)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithStringToPrimitiveDictionary.s_data, isFinalBlock: true, state: default);
                TestClassWithStringToPrimitiveDictionary testStruct = JsonSerializer.ReadValue<TestClassWithStringToPrimitiveDictionary>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithStringToPrimitiveDictionary.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.ReadValue(ref reader, typeof(TestClassWithStringToPrimitiveDictionary));
                ((TestClassWithStringToPrimitiveDictionary)obj).Verify();
            }
        }

        [Fact]
        public static void ReadPartial()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, 3]");
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            int[] array = JsonSerializer.ReadValue<int[]>(ref reader);
            var expected = new int[3] { 1, 2, 3 };
            Assert.Equal(expected, array);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            object obj = JsonSerializer.ReadValue(ref reader, typeof(int[]));
            Assert.Equal(expected, obj);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            int number = JsonSerializer.ReadValue<int>(ref reader);
            Assert.Equal(1, number);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            obj = JsonSerializer.ReadValue(ref reader, typeof(int));
            Assert.Equal(1, obj);
        }
    }
}
