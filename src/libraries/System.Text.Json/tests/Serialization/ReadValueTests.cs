// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ReadValueTests
    {
        [Fact]
        public static void NullReturnTypeThrows()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.Deserialize(ref reader, returnType: null);
            });

            Assert.Contains("returnType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize("", returnType: null));
            Assert.Contains("returnType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(new char[] { '1' }, returnType: null));
            Assert.Contains("returnType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(new byte[] { 1 }, returnType: null));
            Assert.Contains("returnType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializeAsync(new MemoryStream(), returnType: null));
            Assert.Contains("returnType", ex.ToString());
        }

        [Fact]
        public static void NullJsonThrows()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(json: (string)null, returnType: typeof(string)));
            Assert.Contains("json", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializeAsync(utf8Json: null, returnType: null));
            Assert.Contains("utf8Json", ex.ToString());
        }

        [Fact]
        public static void SerializerOptionsStillApply()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            byte[] utf8 = Encoding.UTF8.GetBytes(@"{""myint16"":1}");
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);

            SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(ref reader, options);
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

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                JsonSerializer.Deserialize(ref reader, typeof(int[][]), serializerOptions);
            });

            var state = new JsonReaderState(readerOptions);

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                JsonSerializer.Deserialize(ref reader, typeof(int[][]), serializerOptions);
            });


            readerOptions = new JsonReaderOptions
            {
                MaxDepth = 5,
            };

            serializerOptions = new JsonSerializerOptions
            {
                MaxDepth = 1,
            };

            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                int[][] result = JsonSerializer.Deserialize<int[][]>(ref reader);
                Assert.Equal(1, result.Length);
            }

            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                int[][] result = JsonSerializer.Deserialize<int[][]>(ref reader, serializerOptions);
                Assert.Equal(1, result.Length);
            }
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
                var reader = new Utf8JsonReader(utf8);
                JsonSerializer.Deserialize(ref reader, typeof(int[]), serializerOptions);
            });

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state: default);
                JsonSerializer.Deserialize(ref reader, typeof(int[]), serializerOptions);
            });

            var readerOptions = new JsonReaderOptions { AllowTrailingCommas = true };

            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                int[] result = JsonSerializer.Deserialize<int[]>(ref reader);
                Assert.Equal(3, result.Length);
            }

            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                int[] result = JsonSerializer.Deserialize<int[]>(ref reader, serializerOptions);
                Assert.Equal(3, result.Length);
            }
        }

        [Fact]
        public static void ReaderOptionsWinComments()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, /* some comment */ 3]");

            var serializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8);
                JsonSerializer.Deserialize(ref reader, typeof(int[]), serializerOptions);
            });

            Assert.Throws<JsonException>(() =>
            {
                var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state: default);
                JsonSerializer.Deserialize(ref reader, typeof(int[]), serializerOptions);
            });

            var readerOptions = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip };

            ReadAndVerify(utf8, serializerOptions: null, readerOptions, utf8.Length);
            ReadAndVerify(utf8, serializerOptions, readerOptions, utf8.Length);

            byte[] utf8_CommentsAfter = Encoding.UTF8.GetBytes("[1, 2, 3]/* some comment */");

            ReadAndVerify(utf8_CommentsAfter, serializerOptions, readerOptions: default, expectedLength: "[1, 2, 3]".Length);
            ReadAndVerify(utf8_CommentsAfter, serializerOptions, readerOptions, expectedLength: "[1, 2, 3]".Length);

            static void ReadAndVerify(byte[] utf8, JsonSerializerOptions serializerOptions, JsonReaderOptions readerOptions, int expectedLength)
            {
                var reader = new Utf8JsonReader(utf8, readerOptions);
                int[] result = JsonSerializer.Deserialize<int[]>(ref reader, serializerOptions);
                Assert.Equal(3, result.Length);
                Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
                Assert.Equal(expectedLength, reader.BytesConsumed);
            }
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
                JsonSerializer.Deserialize(ref reader, typeof(int[]));
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

            SimpleType instance = JsonSerializer.Deserialize<SimpleType>(ref reader);
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
                JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);
                Assert.True(false, "Expected ReadValue to throw JsonException for type mismatch.");
            }
            catch (JsonException) { }

            Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);

            int[] instance = JsonSerializer.Deserialize<int[]>(ref reader);
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

            SimpleTypeWithArray instance = JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);

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
                    JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);
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
                    JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);
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
                JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);
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
                JsonSerializer.Deserialize<SimpleTypeWithArray>(ref reader);
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

            object obj = JsonSerializer.Deserialize(ref reader, type);
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

            object obj = JsonSerializer.Deserialize(ref reader, type);
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
                    var reader = new Utf8JsonReader(utf8, options);
                    JsonSerializer.Deserialize(ref reader, typeof(int));
                });

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                    JsonSerializer.Deserialize(ref reader, typeof(int));
                });

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                    JsonSerializer.Deserialize(ref reader, typeof(int));
                });

            AssertExtensions.Throws<ArgumentException>(
               "reader",
               () =>
               {
                   var state = new JsonReaderState(options);
                   var reader = new Utf8JsonReader(utf8, isFinalBlock: false, state);
                   JsonSerializer.Deserialize<int>(ref reader);
               });

            AssertExtensions.Throws<ArgumentException>(
                "reader",
                () =>
                {
                    var state = new JsonReaderState(options);
                    var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                    JsonSerializer.Deserialize<int>(ref reader);
                });
        }

        [Fact]
        public static void ReadDefaultReader()
        {
            Assert.ThrowsAny<JsonException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.Deserialize(ref reader, typeof(int));
            });

            Assert.ThrowsAny<JsonException>(() =>
            {
                Utf8JsonReader reader = default;
                JsonSerializer.Deserialize<int>(ref reader);
            });
        }

        [Fact]
        public static void ReadSimpleStruct()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(SimpleTestStruct.s_json);
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            SimpleTestStruct testStruct = JsonSerializer.Deserialize<SimpleTestStruct>(ref reader);
            testStruct.Verify();

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            object obj = JsonSerializer.Deserialize(ref reader, typeof(SimpleTestStruct));
            ((SimpleTestStruct)obj).Verify();
        }

        [Fact]
        public static void ReadClasses()
        {
            {
                byte[] utf8 = Encoding.UTF8.GetBytes(TestClassWithNestedObjectInner.s_json);
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
                TestClassWithNestedObjectInner testStruct = JsonSerializer.Deserialize<TestClassWithNestedObjectInner>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithNestedObjectInner));
                ((TestClassWithNestedObjectInner)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithNestedObjectOuter.s_data, isFinalBlock: true, state: default);
                TestClassWithNestedObjectOuter testStruct = JsonSerializer.Deserialize<TestClassWithNestedObjectOuter>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithNestedObjectOuter.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithNestedObjectOuter));
                ((TestClassWithNestedObjectOuter)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectList.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectList testStruct = JsonSerializer.Deserialize<TestClassWithObjectList>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectList.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithObjectList));
                ((TestClassWithObjectList)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectArray.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectArray testStruct = JsonSerializer.Deserialize<TestClassWithObjectArray>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectArray.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithObjectArray));
                ((TestClassWithObjectArray)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithObjectIEnumerableT.s_data, isFinalBlock: true, state: default);
                TestClassWithObjectIEnumerableT testStruct = JsonSerializer.Deserialize<TestClassWithObjectIEnumerableT>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithObjectIEnumerableT.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithObjectIEnumerableT));
                ((TestClassWithObjectIEnumerableT)obj).Verify();
            }

            {
                var reader = new Utf8JsonReader(TestClassWithStringToPrimitiveDictionary.s_data, isFinalBlock: true, state: default);
                TestClassWithStringToPrimitiveDictionary testStruct = JsonSerializer.Deserialize<TestClassWithStringToPrimitiveDictionary>(ref reader);
                testStruct.Verify();

                reader = new Utf8JsonReader(TestClassWithStringToPrimitiveDictionary.s_data, isFinalBlock: true, state: default);
                object obj = JsonSerializer.Deserialize(ref reader, typeof(TestClassWithStringToPrimitiveDictionary));
                ((TestClassWithStringToPrimitiveDictionary)obj).Verify();
            }
        }

        [Fact]
        public static void ReadPartial()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1, 2, 3]");
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            int[] array = JsonSerializer.Deserialize<int[]>(ref reader);
            var expected = new int[3] { 1, 2, 3 };
            Assert.Equal(expected, array);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            object obj = JsonSerializer.Deserialize(ref reader, typeof(int[]));
            Assert.Equal(expected, obj);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            int number = JsonSerializer.Deserialize<int>(ref reader);
            Assert.Equal(1, number);

            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state: default);
            reader.Read();
            reader.Read();
            obj = JsonSerializer.Deserialize(ref reader, typeof(int));
            Assert.Equal(1, obj);
        }

        [Theory]
        [InlineData("0,1")]
        [InlineData("0 1")]
        [InlineData("0 {}")]
        public static void TooMuchJson(string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(jsonBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<int>(new MemoryStream(jsonBytes)).Result);

            // Using a reader directly doesn't throw.
            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes);
            JsonSerializer.Deserialize<int>(ref reader);
            Assert.Equal(1, reader.BytesConsumed);
        }

        [Theory]
        [InlineData("0/**/")]
        [InlineData("0 /**/")]
        public static void TooMuchJsonWithComments(string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(jsonBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<int>(new MemoryStream(jsonBytes)).Result);

            // Using a reader directly doesn't throw.
            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes);
            JsonSerializer.Deserialize<int>(ref reader);
            Assert.Equal(1, reader.BytesConsumed);

            // Use JsonCommentHandling.Skip

            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            JsonSerializer.Deserialize<int>(json, options);
            JsonSerializer.Deserialize<int>(jsonBytes, options);
            int result = JsonSerializer.DeserializeAsync<int>(new MemoryStream(jsonBytes), options).Result;

            // Using a reader directly doesn't throw.
            reader = new Utf8JsonReader(jsonBytes);
            JsonSerializer.Deserialize<int>(ref reader, options);
            Assert.Equal(1, reader.BytesConsumed);
        }

        [Theory]
        [InlineData("[")]
        [InlineData("[0")]
        [InlineData("[0,")]
        public static void TooLittleJsonForIntArray(string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(jsonBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<int[]>(new MemoryStream(jsonBytes)).Result);

            // Using a reader directly throws since it can't read full int[].
            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes);
            try
            {
                JsonSerializer.Deserialize<int[]>(ref reader);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }

            Assert.Equal(0, reader.BytesConsumed);
        }

        // From https://github.com/dotnet/runtime/issues/882
        [Fact]
        public static void OptionsFollowToConverter()
        {
            var builder = new StringBuilder();
            builder.Append("{\"type\": \"array\", \"array\": ");

            for (int i = 0; i < 128; i++)
            {
                builder.Append("[");
            }
            builder.Append("1");
            for (int i = 0; i < 128; i++)
            {
                builder.Append("]");
            }
            builder.Append("}");

            string json = builder.ToString();
            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new CustomConverter());

                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DeepArray>(json, options));
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IContent>(json, options));
            }

            {
                var options = new JsonSerializerOptions { MaxDepth = 256 };
                options.Converters.Add(new CustomConverter());

                DeepArray direct = JsonSerializer.Deserialize<DeepArray>(json, options);
                Assert.Equal(1, direct.array.GetArrayLength());

                IContent custom = JsonSerializer.Deserialize<IContent>(json, options);
                Assert.True(custom is DeepArray);
                Assert.Equal(1, ((DeepArray)custom).array.GetArrayLength());
            }

            {
                var options = new JsonSerializerOptions();
                ReadAndValidate(utf8, options, new JsonReaderOptions { MaxDepth = 65 }, expectedThrow: true);
            }

            {
                var options = new JsonSerializerOptions { MaxDepth = 256 };
                ReadAndValidate(utf8, options, new JsonReaderOptions { MaxDepth = 65 }, expectedThrow: true);
            }

            {
                var options = new JsonSerializerOptions();
                ReadAndValidate(utf8, options, new JsonReaderOptions { MaxDepth = 256 }, expectedThrow: false);
            }

            static void ReadAndValidate(byte[] utf8, JsonSerializerOptions options, JsonReaderOptions readerOptions, bool expectedThrow)
            {
                options.Converters.Add(new CustomConverter());

                if (expectedThrow)
                {
                    Assert.Throws<JsonException>(() =>
                    {
                        var reader = new Utf8JsonReader(utf8, readerOptions);
                        JsonSerializer.Deserialize<DeepArray>(ref reader, options);
                    });

                    Assert.Throws<JsonException>(() =>
                    {
                        var reader = new Utf8JsonReader(utf8, readerOptions);
                        JsonSerializer.Deserialize<IContent>(ref reader, options);
                    });
                }
                else
                {
                    var reader = new Utf8JsonReader(utf8, readerOptions);
                    DeepArray direct = JsonSerializer.Deserialize<DeepArray>(ref reader, options);
                    Assert.Equal(1, direct.array.GetArrayLength());

                    reader = new Utf8JsonReader(utf8, readerOptions);
                    IContent custom = JsonSerializer.Deserialize<IContent>(ref reader, options);
                    Assert.True(custom is DeepArray);
                    Assert.Equal(1, ((DeepArray)custom).array.GetArrayLength());
                }
            }
        }
    }

    // From https://github.com/dotnet/runtime/issues/882
    public interface IContent { }

    public class DeepArray : IContent
    {
        public JsonElement array { get; set; }
    }

    public class CustomConverter : JsonConverter<IContent>
    {
        public override IContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader sideReader = reader;

            sideReader.Read();
            sideReader.Read();
            string type = sideReader.GetString();

            return type switch
            {
                "array" => JsonSerializer.Deserialize<DeepArray>(ref reader, options),
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, IContent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "array");
            writer.WritePropertyName("array");
            JsonSerializer.Serialize(writer, ((DeepArray)value).array, options);
            writer.WriteEndObject();
        }
    }
}
