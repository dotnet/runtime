// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        // TestCaseType is only used to give the json strings a descriptive name.
        [Theory]
        [MemberData(nameof(TestCases))]
        public static void TestJsonReaderUtf8(bool compactData, TestCaseType type, string jsonString)
        {
            // Remove all formatting/indendation
            if (compactData)
            {
                using (JsonTextReader jsonReader = new JsonTextReader(new StringReader(jsonString)))
                {
                    jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                    JToken jtoken = JToken.ReadFrom(jsonReader);
                    var stringWriter = new StringWriter();
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter))
                    {
                        jtoken.WriteTo(jsonWriter);
                        jsonString = stringWriter.ToString();
                    }
                }
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            SpanSequenceStatesAreEqual(dataUtf8);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int length);
            string actualStr = Encoding.UTF8.GetString(result.AsSpan(0, length));

            byte[] resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length);
            string actualStrSequence = Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            Assert.Equal(expectedStr, actualStr);
            Assert.Equal(expectedStr, actualStrSequence);

            // Json payload contains numbers that are too large for .NET (need BigInteger+)
            if (type != TestCaseType.FullSchema1 && type != TestCaseType.BasicLargeNum)
            {
                object jsonValues = JsonTestHelper.ReturnObjectHelper(dataUtf8);
                string str = JsonTestHelper.ObjectToString(jsonValues);
                ReadOnlySpan<char> expectedSpan = expectedStr.AsSpan(0, expectedStr.Length - 2);
                ReadOnlySpan<char> actualSpan = str.AsSpan(0, str.Length - 2);
                Assert.True(expectedSpan.SequenceEqual(actualSpan));
            }

            result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Skip);
            actualStr = Encoding.UTF8.GetString(result.AsSpan(0, length));
            resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Skip);
            actualStrSequence = Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));

            Assert.Equal(expectedStr, actualStr);
            Assert.Equal(expectedStr, actualStrSequence);

            result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Allow);
            actualStr = Encoding.UTF8.GetString(result.AsSpan(0, length));
            resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Allow);
            actualStrSequence = Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));

            Assert.Equal(expectedStr, actualStr);
            Assert.Equal(expectedStr, actualStrSequence);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public static void TestPartialJsonReader(bool compactData, TestCaseType type, string jsonString)
        {
            // Skipping really large JSON since slicing them (O(n^2)) is too slow.
            if (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson)
            {
                return;
            }

            // Remove all formatting/indendation
            if (compactData)
            {
                using (JsonTextReader jsonReader = new JsonTextReader(new StringReader(jsonString)))
                {
                    jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                    JToken jtoken = JToken.ReadFrom(jsonReader);
                    var stringWriter = new StringWriter();
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter))
                    {
                        jtoken.WriteTo(jsonWriter);
                        jsonString = stringWriter.ToString();
                    }
                }
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength);
            Span<byte> outputSpan = new byte[outputLength];

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                JsonReaderState state = default;
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                byte[] output = JsonTestHelper.ReaderLoop(outputSpan.Length, out int firstLength, ref json);
                output.AsSpan(0, firstLength).CopyTo(outputSpan);
                int written = firstLength;

                long consumed = json.BytesConsumed;
                Assert.Equal(consumed, json.CurrentState.BytesConsumed);

                // Skipping large JSON since slicing them (O(n^3)) is too slow.
                if (type == TestCaseType.DeepTree || type == TestCaseType.BroadTree || type == TestCaseType.LotsOfNumbers
                    || type == TestCaseType.LotsOfStrings || type == TestCaseType.Json4KB)
                {
                    json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), isFinalBlock: true, json.CurrentState);
                    output = JsonTestHelper.ReaderLoop(outputSpan.Length - written, out int length, ref json);
                    output.AsSpan(0, length).CopyTo(outputSpan.Slice(written));
                    written += length;
                    Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                    Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);

                    Assert.Equal(outputSpan.Length, written);
                    string actualStr = Encoding.UTF8.GetString(outputSpan);
                    Assert.Equal(expectedStr, actualStr);
                }
                else
                {
                    for (long j = consumed; j < dataUtf8.Length - consumed; j++)
                    {
                        // Need to re-initialize the state and reader to avoid using the previous state stack.
                        state = default;
                        json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                        while (json.Read())
                            ;

                        JsonReaderState jsonState = json.CurrentState;

                        written = firstLength;
                        json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed, (int)j), isFinalBlock: false, jsonState);
                        output = JsonTestHelper.ReaderLoop(outputSpan.Length - written, out int length, ref json);
                        output.AsSpan(0, length).CopyTo(outputSpan.Slice(written));
                        written += length;

                        long consumedInner = json.BytesConsumed;
                        Assert.Equal(consumedInner, json.CurrentState.BytesConsumed);
                        json = new Utf8JsonReader(dataUtf8.AsSpan((int)(consumed + consumedInner)), isFinalBlock: true, json.CurrentState);
                        output = JsonTestHelper.ReaderLoop(outputSpan.Length - written, out length, ref json);
                        output.AsSpan(0, length).CopyTo(outputSpan.Slice(written));
                        written += length;
                        Assert.Equal(dataUtf8.Length - consumedInner - consumed, json.BytesConsumed);
                        Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);

                        Assert.Equal(outputSpan.Length, written);
                        string actualStr = Encoding.UTF8.GetString(outputSpan);
                        Assert.Equal(expectedStr, actualStr);
                    }
                }
            }
        }

        [Theory]
        // Pad depth by nested objects, but minimize the text
        [InlineData(1, true, true)]
        [InlineData(2, true, true)]
        [InlineData(3, true, true)]
        [InlineData(16, true, true)]
        [InlineData(32, true, true)]
        [InlineData(60, true, true)]
        [InlineData(61, true, true)]
        [InlineData(62, true, true)]
        [InlineData(63, true, true)]
        [InlineData(64, true, true)]
        [InlineData(65, true, true)]
        [InlineData(66, true, true)]
        [InlineData(67, true, true)]
        [InlineData(68, true, true)]
        [InlineData(123, true, true)]
        [InlineData(124, true, true)]
        [InlineData(125, true, true)]

        // Pad depth by nested arrays, but minimize the text
        [InlineData(1, false, true)]
        [InlineData(2, false, true)]
        [InlineData(3, false, true)]
        [InlineData(16, false, true)]
        [InlineData(32, false, true)]
        [InlineData(60, false, true)]
        [InlineData(61, false, true)]
        [InlineData(62, false, true)]
        [InlineData(63, false, true)]
        [InlineData(64, false, true)]
        [InlineData(65, false, true)]
        [InlineData(66, false, true)]
        [InlineData(67, false, true)]
        [InlineData(68, false, true)]
        [InlineData(123, false, true)]
        [InlineData(124, false, true)]
        [InlineData(125, false, true)]

        // Pad depth by nested arrays, but keep the text formatted
        [InlineData(1, false, false)]
        [InlineData(2, false, false)]
        [InlineData(3, false, false)]
        [InlineData(16, false, false)]
        [InlineData(32, false, false)]
        [InlineData(60, false, false)]
        [InlineData(61, false, false)]
        [InlineData(62, false, false)]
        [InlineData(63, false, false)]
        [InlineData(64, false, false)]
        [InlineData(65, false, false)]
        [InlineData(66, false, false)]
        [InlineData(67, false, false)]
        [InlineData(68, false, false)]
        [InlineData(123, false, false)]
        [InlineData(124, false, false)]
        [InlineData(125, false, false)]

        // Pad depth by nested objects, but keep the text formatted
        [InlineData(1, true, false)]
        [InlineData(2, true, false)]
        [InlineData(3, true, false)]
        [InlineData(16, true, false)]
        [InlineData(32, true, false)]
        [InlineData(60, true, false)]
        [InlineData(61, true, false)]
        [InlineData(62, true, false)]
        [InlineData(63, true, false)]
        [InlineData(64, true, false)]
        [InlineData(65, true, false)]
        [InlineData(66, true, false)]
        [InlineData(67, true, false)]
        [InlineData(68, true, false)]
        [InlineData(123, true, false)]
        [InlineData(124, true, false)]
        [InlineData(125, true, false)]
        public static void TestPartialJsonReaderWithDepthPadding(int depthPadding, bool padByObject, bool compactData)
        {
            var builderPrefix = new StringBuilder();
            var builderSuffix = new StringBuilder();

            if (padByObject)
            {
                for (int i = 0; i < depthPadding; i++)
                {
                    builderPrefix.Append($"{{\n \"property{i}\": ");
                    builderSuffix.Append("\n}");
                }
            }
            else
            {
                for (int i = 0; i < depthPadding; i++)
                {
                    builderPrefix.Append("[\n");
                    builderSuffix.Append("\n]");
                }
            }

            string jsonString = builderPrefix.ToString() + SR.FullJsonSchema1 + builderSuffix.ToString();

            // Remove all formatting/indendation
            if (compactData)
            {
                using (JsonTextReader jsonReader = new JsonTextReader(new StringReader(jsonString)))
                {
                    jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                    JToken jtoken = JToken.ReadFrom(jsonReader);
                    var stringWriter = new StringWriter();
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(stringWriter))
                    {
                        jtoken.WriteTo(jsonWriter);
                        jsonString = stringWriter.ToString();
                    }
                }
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            // Set the max depth sufficiently large to account for the depth padding.
            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength, maxDepth: 256);
            Span<byte> outputSpan = new byte[outputLength];
            string actualStr = Encoding.UTF8.GetString(result.AsSpan(0, outputLength));

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            Assert.Equal(expectedStr, actualStr);
        }

        [Theory]
        [MemberData(nameof(SpecialNumTestCases))]
        public static void TestPartialJsonReaderSpecialNumbers(TestCaseType type, string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                for (int i = 0; i < dataUtf8.Length; i++)
                {
                    var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                    var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                    while (json.Read())
                        ;

                    long consumed = json.BytesConsumed;
                    Assert.Equal(consumed, json.CurrentState.BytesConsumed);

                    for (long j = consumed; j < dataUtf8.Length - consumed; j++)
                    {
                        // Need to re-initialize the state and reader to avoid using the previous state stack.
                        state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                        json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                        while (json.Read())
                            ;

                        JsonReaderState jsonState = json.CurrentState;

                        json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed, (int)j), isFinalBlock: false, jsonState);
                        while (json.Read())
                            ;

                        long consumedInner = json.BytesConsumed;
                        Assert.Equal(consumedInner, json.CurrentState.BytesConsumed);
                        json = new Utf8JsonReader(dataUtf8.AsSpan((int)(consumed + consumedInner)), isFinalBlock: true, json.CurrentState);
                        while (json.Read())
                            ;
                        Assert.Equal(dataUtf8.Length - consumedInner - consumed, json.BytesConsumed);
                        Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                    }
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(62)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(66)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public static void TestDepth(int depth)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                for (int i = 0; i < depth; i++)
                {
                    string jsonStr = JsonTestHelper.WriteDepthObject(i, commentHandling == JsonCommentHandling.Allow);
                    Span<byte> data = Encoding.UTF8.GetBytes(jsonStr);

                    var state = new JsonReaderState(maxDepth: depth, options: new JsonReaderOptions { CommentHandling = commentHandling });
                    var json = new Utf8JsonReader(data, isFinalBlock: true, state);

                    int actualDepth = 0;
                    while (json.Read())
                    {
                        if (json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null)
                            actualDepth = json.CurrentDepth;
                    }

                    int expectedDepth = 0;
                    var newtonJson = new JsonTextReader(new StringReader(jsonStr))
                    {
                        MaxDepth = depth
                    };
                    while (newtonJson.Read())
                    {
                        if (newtonJson.TokenType == JsonToken.String)
                        {
                            expectedDepth = newtonJson.Depth;
                        }
                    }

                    Assert.Equal(expectedDepth, actualDepth);
                    Assert.Equal(i + 1, actualDepth);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(62)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(66)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public static void TestDepthWithObjectArrayMismatch(int depth)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                for (int i = 0; i < depth; i++)
                {
                    string jsonStr = JsonTestHelper.WriteDepthObjectWithArray(i, commentHandling == JsonCommentHandling.Allow);
                    Span<byte> data = Encoding.UTF8.GetBytes(jsonStr);

                    var state = new JsonReaderState(maxDepth: depth + 1, options: new JsonReaderOptions { CommentHandling = commentHandling });
                    var json = new Utf8JsonReader(data, isFinalBlock: true, state);

                    int actualDepth = 0;
                    while (json.Read())
                    {
                        if (json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null)
                            actualDepth = json.CurrentDepth;
                    }

                    int expectedDepth = 0;
                    var newtonJson = new JsonTextReader(new StringReader(jsonStr))
                    {
                        MaxDepth = depth + 1
                    };
                    while (newtonJson.Read())
                    {
                        if (newtonJson.TokenType == JsonToken.String)
                        {
                            expectedDepth = newtonJson.Depth;
                        }
                    }

                    Assert.Equal(expectedDepth, actualDepth);
                    Assert.Equal(i + 2, actualDepth);
                }
            }
        }

        [Theory]
        [InlineData("[123, 456]", "123456", "123456")]
        [InlineData("/*a*/[{\"testA\":[{\"testB\":[{\"testC\":123}]}]}]", "testAtestBtestC123", "/*a*/testAtestBtestC123")]
        [InlineData("{\"testA\":[1/*hi*//*bye*/, 2, 3], \"testB\": 4}", "testA123testB4", "testA1/*hi*//*bye*/23testB4")]
        [InlineData("{\"test\":[[[123,456]]]}", "test123456", "test123456")]
        [InlineData("/*a*//*z*/[/*b*//*z*/123/*c*//*z*/,/*d*//*z*/456/*e*//*z*/]/*f*//*z*/", "123456", "/*a*//*z*//*b*//*z*/123/*c*//*z*//*d*//*z*/456/*e*//*z*//*f*//*z*/")]
        [InlineData("[123,/*hi*/456/*bye*/]", "123456", "123/*hi*/456/*bye*/")]
        [InlineData("/*a*//*z*/{/*b*//*z*/\"test\":/*c*//*z*/[/*d*//*z*/[/*e*//*z*/[/*f*//*z*/123/*g*//*z*/,/*h*//*z*/456/*i*//*z*/]/*j*//*z*/]/*k*//*z*/]/*l*//*z*/}/*m*//*z*/",
    "test123456", "/*a*//*z*//*b*//*z*/test/*c*//*z*//*d*//*z*//*e*//*z*//*f*//*z*/123/*g*//*z*//*h*//*z*/456/*i*//*z*//*j*//*z*//*k*//*z*//*l*//*z*//*m*//*z*/")]
        [InlineData("//a\n//z\n{//b\n//z\n\"test\"://c\n//z\n[//d\n//z\n[//e\n//z\n[//f\n//z\n123//g\n//z\n,//h\n//z\n456//i\n//z\n]//j\n//z\n]//k\n//z\n]//l\n//z\n}//m\n//z\n",
    "test123456", "//a\n//z\n//b\n//z\ntest//c\n//z\n//d\n//z\n//e\n//z\n//f\n//z\n123//g\n//z\n//h\n//z\n456//i\n//z\n//j\n//z\n//k\n//z\n//l\n//z\n//m\n//z\n")]
        public static void AllowCommentStackMismatch(string jsonString, string expectedWithoutComments, string expectedWithComments)
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                    var json = new Utf8JsonReader(data.AsSpan(0, i), false, state);

                    var builder = new StringBuilder();
                    while (json.Read())
                    {
                        if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                            builder.Append(Encoding.UTF8.GetString(json.ValueSpan));
                    }

                    long consumed = json.BytesConsumed;
                    Assert.Equal(consumed, json.CurrentState.BytesConsumed);
                    json = new Utf8JsonReader(data.AsSpan((int)consumed), true, json.CurrentState);
                    while (json.Read())
                    {
                        if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                            builder.Append(Encoding.UTF8.GetString(json.ValueSpan));
                    }
                    Assert.Equal(data.Length - consumed, json.BytesConsumed);
                    Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);

                    Assert.Equal(commentHandling == JsonCommentHandling.Allow ? expectedWithComments : expectedWithoutComments, builder.ToString());
                }
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(62)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(66)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public static void TestDepthBeyondLimit(int depth)
        {
            string jsonStr = JsonTestHelper.WriteDepthObject(depth - 1);
            Span<byte> data = Encoding.UTF8.GetBytes(jsonStr);

            var state = new JsonReaderState(maxDepth: depth - 1);
            var json = new Utf8JsonReader(data, isFinalBlock: true, state);

            try
            {
                int maxDepth = 0;
                while (json.Read())
                {
                    if (maxDepth < json.CurrentDepth)
                        maxDepth = json.CurrentDepth;
                }
                Assert.True(false, $"Expected JsonReaderException was not thrown. Max depth allowed = {json.CurrentState.MaxDepth} | Max depth reached = {maxDepth}");
            }
            catch (JsonReaderException)
            { }

            jsonStr = JsonTestHelper.WriteDepthArray(depth - 1);
            data = Encoding.UTF8.GetBytes(jsonStr);

            state = new JsonReaderState(maxDepth: depth - 1);
            json = new Utf8JsonReader(data, isFinalBlock: true, state);

            try
            {
                int maxDepth = 0;
                while (json.Read())
                {
                    if (maxDepth < json.CurrentDepth)
                        maxDepth = json.CurrentDepth;
                }
                Assert.True(false, $"Expected JsonReaderException was not thrown. Max depth allowed = {json.CurrentState.MaxDepth} | Max depth reached = {maxDepth}");
            }
            catch (JsonReaderException)
            { }
        }

        [Theory]
        [InlineData("{\"nam\\\"e\":\"ah\\\"son\"}", JsonCommentHandling.Disallow, "nam\\\"e, ah\\\"son, ")]
        [InlineData("{\"Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str\":\"\\\"\\\"\"}",
            JsonCommentHandling.Disallow,
            "Here is a string: \\\"\\\", Here is a, Here is a back slash\\\\, Multiline\\r\\n String\\r\\n, \\tMul\\r\\ntiline String, \\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\, str, \\\"\\\", ")]

        [InlineData("{\"nam\\\"e\":\"ah\\\"son\"}", JsonCommentHandling.Allow, "nam\\\"e, ah\\\"son, ")]
        [InlineData("{\"Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str\":\"\\\"\\\"\"}",
            JsonCommentHandling.Allow,
            "Here is a string: \\\"\\\", Here is a, Here is a back slash\\\\, Multiline\\r\\n String\\r\\n, \\tMul\\r\\ntiline String, \\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\, str, \\\"\\\", ")]

        [InlineData("{\"nam\\\"e\":\"ah\\\"son\"}", JsonCommentHandling.Skip, "nam\\\"e, ah\\\"son, ")]
        [InlineData("{\"Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str\":\"\\\"\\\"\"}",
            JsonCommentHandling.Skip,
            "Here is a string: \\\"\\\", Here is a, Here is a back slash\\\\, Multiline\\r\\n String\\r\\n, \\tMul\\r\\ntiline String, \\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\, str, \\\"\\\", ")]
        public static void TestJsonReaderUtf8SpecialString(string jsonString, JsonCommentHandling commentHandling, string expectedStr)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int length, commentHandling);
            string actualStr = Encoding.UTF8.GetString(result.AsSpan(0, length));

            Assert.Equal(expectedStr, actualStr);

            result = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, commentHandling);
            actualStr = Encoding.UTF8.GetString(result.AsSpan(0, length));

            Assert.Equal(expectedStr, actualStr);

            object jsonValues = JsonTestHelper.ReturnObjectHelper(dataUtf8, commentHandling);
            string str = JsonTestHelper.ObjectToString(jsonValues);
            ReadOnlySpan<char> expectedSpan = expectedStr.AsSpan(0, expectedStr.Length - 2);
            ReadOnlySpan<char> actualSpan = str.AsSpan(0, str.Length - 2);
            Assert.True(expectedSpan.SequenceEqual(actualSpan));
        }

        [Theory]
        [MemberData(nameof(SingleValueJson))]
        public static void SingleJsonValue(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                for (int i = 0; i < dataUtf8.Length; i++)
                {
                    var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                    var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), false, state);
                    while (json.Read())
                    {
                        // Check if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
                        Assert.True(json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null);
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan));
                    }

                    long consumed = json.BytesConsumed;
                    Assert.Equal(consumed, json.CurrentState.BytesConsumed);
                    json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), true, json.CurrentState);
                    while (json.Read())
                    {
                        // Check if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
                        Assert.True(json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null);
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan));
                    }
                    Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                    Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                }
            }
        }

        [Theory]
        [InlineData("\"h\u6F22\u5B57ello\"", 1, 0)] // "\""
        [InlineData("12345", 3, 0)]   // "123"
        [InlineData("null", 3, 0)]   // "nul"
        [InlineData("true", 3, 0)]   // "tru"
        [InlineData("false", 4, 0)]  // "fals"
        [InlineData("   {\"a\u6F22\u5B57ge\":30}   ", 16, 16)] // "   {\"a\u6F22\u5B57ge\":"
        [InlineData("{\"n\u6F22\u5B57ame\":\"A\u6F22\u5B57hson\"}", 15, 14)]  // "{\"n\u6F22\u5B57ame\":\"A\u6F22\u5B57hso"
        [InlineData("-123456789", 1, 0)] // "-"
        [InlineData("0.5", 2, 0)]    // "0."
        [InlineData("10.5e+3", 5, 0)] // "10.5e"
        [InlineData("10.5e-1", 6, 0)]    // "10.5e-"
        [InlineData("{\"i\u6F22\u5B57nts\":[1, 2, 3, 4, 5]}", 27, 25)]    // "{\"i\u6F22\u5B57nts\":[1, 2, 3, 4, "
        [InlineData("{\"s\u6F22\u5B57trings\":[\"a\u6F22\u5B57bc\", \"def\"], \"ints\":[1, 2, 3, 4, 5]}", 36, 36)]  // "{\"s\u6F22\u5B57trings\":[\"a\u6F22\u5B57bc\", \"def\""
        [InlineData("{\"a\u6F22\u5B57ge\":30, \"name\":\"test}:[]\", \"another \u6F22\u5B57string\" : \"tests\"}", 25, 24)]   // "{\"a\u6F22\u5B57ge\":30, \"name\":\"test}"
        [InlineData("   [[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"t\u6F22\u5B57emp2:[]}]]]]}]]]]\":[]}]]]]}]]]]   ", 54, 29)] // "   [[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"t\u6F22\u5B57emp2:[]}]]]]}]]]]"
        [InlineData("{\r\n\"is\u6F22\u5B57Active\": false, \"in\u6F22\u5B57valid\"\r\n : \"now its \u6F22\u5B57valid\"}", 26, 26)]  // "{\r\n\"is\u6F22\u5B57Active\": false, \"in\u6F22\u5B57valid\"\r\n}"
        [InlineData("{\"property\\u1234Name\": \"String value with hex: \\uABCD in the middle.\"}", 51, 23)]  // "{\"property\\u1234Name\": \"String value with hex: \\uAB"
        [InlineData("{ \"number\": 0}", 13, 12)]    // "{ \"number\": 0"
        public static void PartialJson(string jsonString, int splitLocation, int consumed)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, splitLocation), false, state);
                while (json.Read())
                    ;
                Assert.Equal(consumed, json.BytesConsumed);
                Assert.Equal(consumed, json.CurrentState.BytesConsumed);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)json.BytesConsumed), true, json.CurrentState);
                while (json.Read())
                    ;
                Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
            }
        }

        [Theory]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false \"in\u6F22\u5B57valid\"\r\n}", 30, 30, 2, 21)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false \"in\u6F22\u5B57valid\"\r\n}", 31, 31, 2, 21)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, \"in\u6F22\u5B57valid\"\r\n}", 30, 30, 3, 0)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, \"in\u6F22\u5B57valid\"\r\n}", 31, 30, 3, 0)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, \"in\u6F22\u5B57valid\"\r\n}", 32, 30, 3, 0)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, 5\r\n}", 30, 30, 2, 22)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, 5\r\n}", 31, 30, 2, 22)]
        [InlineData("{\r\n\"is\\r\\nAct\u6F22\u5B57ive\": false, 5\r\n}", 32, 30, 2, 22)]
        public static void InvalidJsonSplitRemainsInvalid(string jsonString, int splitLocation, int consumed, int expectedlineNumber, int expectedBytePosition)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, splitLocation), false, state);
                while (json.Read())
                    ;
                Assert.Equal(consumed, json.BytesConsumed);
                Assert.Equal(consumed, json.CurrentState.BytesConsumed);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)json.BytesConsumed), true, json.CurrentState);
                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException was not thrown.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [InlineData("{]", 0, 1)]
        [InlineData("[}", 0, 1)]
        [InlineData("nulz", 0, 3)]
        [InlineData("truz", 0, 3)]
        [InlineData("falsz", 0, 4)]
        [InlineData("\"a\u6F22\u5B57ge\":", 0, 11)]
        [InlineData("12345.1.", 0, 7)]
        [InlineData("-f", 0, 1)]
        [InlineData("1.f", 0, 2)]
        [InlineData("0.1f", 0, 3)]
        [InlineData("0.1e1f", 0, 5)]
        [InlineData("123,", 0, 3)]
        [InlineData("01", 0, 1)]
        [InlineData("-01", 0, 2)]
        [InlineData("10.5e-0.2", 0, 7)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \"ints\":[1, 2, 3, 4, 5.1e7.3]}", 0, 42)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \r\n \"num\":-0.e, \r\n \"ints\":[1, 2, 3, 4, 5]}", 1, 10)]
        [InlineData("{{}}", 0, 1)]
        [InlineData("[[{{}}]]", 0, 3)]
        [InlineData("[1, 2, 3, ]", 0, 10)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \"ints\":[1, 2, 3, 4, 5}}", 0, 38)]
        [InlineData("{\r\n\"isActive\": false \"\r\n}", 1, 18)]
        [InlineData("[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[}]]]]}]]]]", 1, 28)]
        [InlineData("[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[]},[}]]]]}]]]]", 1, 32)]
        [InlineData("{\r\n\t\"isActive\": false,\r\n\t\"array\": [\r\n\t\t[{\r\n\t\t\t\"id\": 1\r\n\t\t}]\r\n\t]\r\n}", 3, 3, 3)]
        [InlineData("{\"Here is a \u6F22\u5B57string: \\\"\\\"\":\"Here is \u6F22\u5B57a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str:\"\\\"\\\"\"}", 4, 35)]
        public static void InvalidJsonWhenPartial(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth = 64)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, false, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException was not thrown.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [InlineData("{\"text\": \"\u0E4F\u0020\u0E2A\u0E27\u0E31\u0E2A\u0E14\u0E35\\uABCZ \u0E42\u0E25\u0E01\"}", 0, 37)]   // * Hello\\uABCZ World in thai
        [InlineData("{\"text\": \"\u0E4F\u0020\u0E2A\u0E39\u0E07\\n\u0E15\u0E48\u0E33\\uABCZ \u0E42\u0E25\u0E01\"}", 1, 14)]    // * High\\nlow\\uABCZ World in thai
        public static void PositionInCodeUnits(string jsonString, int expectedlineNumber, int expectedBytePosition)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, false, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException was not thrown.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidJsonStrings))]
        public static void InvalidJson(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth = 64)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidJsonStrings))]
        public static void InvalidJsonSingleSegment(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth = 64)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }

                for (int i = 0; i < dataUtf8.Length; i++)
                {
                    try
                    {
                        var stateInner = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
                        var jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, stateInner);
                        while (jsonSlice.Read())
                            ;

                        long consumed = jsonSlice.BytesConsumed;
                        Assert.Equal(consumed, jsonSlice.CurrentState.BytesConsumed);
                        JsonReaderState jsonState = jsonSlice.CurrentState;

                        jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), isFinalBlock: true, jsonState);
                        while (jsonSlice.Read())
                            ;

                        Assert.True(false, "Expected JsonReaderException was not thrown with multi-segment data.");
                    }
                    catch (JsonReaderException ex)
                    {
                        string errorMessage = $"expectedLineNumber: {expectedlineNumber} | actual: {ex.LineNumber} | index: {i} | option: {commentHandling}";
                        string firstSegmentString = Encoding.UTF8.GetString(dataUtf8.AsSpan(0, i));
                        string secondSegmentString = Encoding.UTF8.GetString(dataUtf8.AsSpan(i));
                        errorMessage += " | " + firstSegmentString + " | " + secondSegmentString;
                        Assert.True(expectedlineNumber == ex.LineNumber, errorMessage);
                        errorMessage = $"expectedBytePosition: {expectedBytePosition} | actual: {ex.BytePositionInLine} | index: {i} | option: {commentHandling}";
                        errorMessage += " | " + firstSegmentString + " | " + secondSegmentString;
                        Assert.True(expectedBytePosition == ex.BytePositionInLine, errorMessage);
                    }
                }
            }
        }

        [Theory]
        [InlineData("//", "//", 2)]
        [InlineData("//\n", "//\n", 3)]
        [InlineData("/**/", "/**/", 4)]
        [InlineData("/*/*/", "/*/*/", 5)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", "//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n", 44)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", "//This is a \u6F22\u5B57comment after json", 49)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", "//This is a \u6F22\u5B57comment after json\n", 50)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "//This is a \u6F22\u5B57comment after json\n", 52)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", "//This is a \u6F22\u5B57comment after json with new line\n", 64)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", "//This is a \u6F22\u5B57comment between key-value pairs\n", 66)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", "//This is a \u6F22\u5B57comment between key-value pairs on the same line\n", 84)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", "/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/", 56)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 62)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 64)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", "/*This is a \u6F22\u5B57comment between key-value pairs*/", 67)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", "/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/", 85)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", "/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/", 63)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 69)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 71)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", "/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/", 85)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", "/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/", 103)]
        public static void Allow(string jsonString, string expectedComment, int expectedIndex)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            bool foundComment = false;
            long indexAfterFirstComment = 0;
            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        if (foundComment)
                            break;
                        foundComment = true;
                        indexAfterFirstComment = json.BytesConsumed;
                        Assert.Equal(indexAfterFirstComment, json.CurrentState.BytesConsumed);
                        string actualComment = Encoding.UTF8.GetString(json.ValueSpan); // TODO: https://github.com/dotnet/corefx/issues/33347
                        Assert.Equal(expectedComment, actualComment);
                        break;
                }
            }
            Assert.True(foundComment);
            Assert.Equal(expectedIndex, indexAfterFirstComment);
        }

        [Theory]
        [InlineData("//", "//", 2)]
        [InlineData("//\n", "//\n", 3)]
        [InlineData("/**/", "/**/", 4)]
        [InlineData("/*/*/", "/*/*/", 5)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", "//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n", 44)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", "//This is a \u6F22\u5B57comment after json", 49)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", "//This is a \u6F22\u5B57comment after json\n", 50)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "//This is a \u6F22\u5B57comment after json\n", 52)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "//This is a \u6F22\u5B57comment after json\n", 53)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", "//This is a \u6F22\u5B57comment after json with new line\n", 64)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", "//This is a \u6F22\u5B57comment between key-value pairs\n", 66)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", "//This is a \u6F22\u5B57comment between key-value pairs on the same line\n", 84)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", "/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/", 56)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 62)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 64)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a multi-line \u6F22\u5B57comment after json*/", 65)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", "/*This is a \u6F22\u5B57comment between key-value pairs*/", 67)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", "/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/", 85)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", "/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/", 63)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 69)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 71)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", "/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 72)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", "/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/", 85)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", "/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/", 103)]

        [InlineData("{\r\n   \"value\": 11,\r\n   /* yes, it's mis-spelled */\r\n   \"deelay\": 3\r\n}", "/* yes, it's mis-spelled */", 50)]
        [InlineData("[\r\n   12,\r\n   87,\r\n   /* Isn't it \"nice\" that JSON provides no limits on the length of numbers? */\r\n   123456789012345678901234567890123456789.01234567890123456789e+9876543218976543219876543210\r\n]",
            "/* Isn't it \"nice\" that JSON provides no limits on the length of numbers? */", 98)]
        public static void AllowSingleSegment(string jsonString, string expectedComment, int expectedIndex)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            bool foundComment = false;
            long indexAfterFirstComment = 0;
            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        if (foundComment)
                            break;
                        foundComment = true;
                        indexAfterFirstComment = json.BytesConsumed;
                        Assert.Equal(indexAfterFirstComment, json.CurrentState.BytesConsumed);
                        string actualComment = Encoding.UTF8.GetString(json.ValueSpan);
                        Assert.Equal(expectedComment, actualComment);
                        break;
                }
            }
            Assert.True(foundComment);
            Assert.Equal(expectedIndex, indexAfterFirstComment);

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                var stateInner = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
                var jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, stateInner);

                foundComment = false;
                indexAfterFirstComment = 0;
                while (jsonSlice.Read())
                {
                    JsonTokenType tokenType = jsonSlice.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            if (foundComment)
                                break;
                            foundComment = true;
                            indexAfterFirstComment = jsonSlice.BytesConsumed;
                            Assert.Equal(indexAfterFirstComment, jsonSlice.CurrentState.BytesConsumed);
                            string actualComment = Encoding.UTF8.GetString(jsonSlice.ValueSpan);
                            Assert.Equal(expectedComment, actualComment);
                            break;
                    }
                }

                int consumed = (int)jsonSlice.BytesConsumed;
                Assert.Equal(consumed, jsonSlice.CurrentState.BytesConsumed);
                jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(consumed), isFinalBlock: true, jsonSlice.CurrentState);

                if (!foundComment)
                {
                    while (jsonSlice.Read())
                    {
                        JsonTokenType tokenType = jsonSlice.TokenType;
                        switch (tokenType)
                        {
                            case JsonTokenType.Comment:
                                if (foundComment)
                                    break;
                                foundComment = true;
                                indexAfterFirstComment = jsonSlice.BytesConsumed;
                                Assert.Equal(indexAfterFirstComment, jsonSlice.CurrentState.BytesConsumed);
                                string actualComment = Encoding.UTF8.GetString(jsonSlice.ValueSpan);
                                Assert.Equal(expectedComment, actualComment);
                                break;
                        }
                    }
                    indexAfterFirstComment += consumed;
                }

                Assert.True(foundComment);
                Assert.Equal(expectedIndex, indexAfterFirstComment);
            }
        }

        [Theory]
        [InlineData("//", 2)]
        [InlineData("//\n", 3)]
        [InlineData("/**/", 4)]
        [InlineData("/*/*/", 5)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", 32)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", 37)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", 38)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", 41)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 40)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", 41)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 41)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", 52)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", 54)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", 72)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", 44)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", 50)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 53)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 52)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 53)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 53)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", 55)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", 73)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", 51)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 57)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 60)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 59)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 60)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 60)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", 73)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", 91)]
        public static void Skip(string jsonString, int expectedConsumed)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            JsonTokenType prevTokenType = JsonTokenType.None;
            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                        break;
                }
                Assert.NotEqual(tokenType, prevTokenType);
                prevTokenType = tokenType;
            }
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(dataUtf8.Length, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("//", 2)]
        [InlineData("//\n", 3)]
        [InlineData("/**/", 4)]
        [InlineData("/*/*/", 5)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", 32)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", 37)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", 38)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", 41)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 40)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", 41)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 41)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", 52)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", 54)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", 72)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", 44)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", 50)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 53)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 52)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 53)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 53)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", 55)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", 73)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", 51)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 57)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 60)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 59)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 60)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 60)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", 73)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", 91)]

        [InlineData("{\r\n   \"value\": 11,\r\n   /* yes, it's mis-spelled */\r\n   \"deelay\": 3\r\n}", 50)]
        [InlineData("[\r\n   12,\r\n   87,\r\n   /* Isn't it \"nice\" that JSON provides no limits on the length of numbers? */\r\n   123456789012345678901234567890123456789.01234567890123456789e+9876543218976543219876543210\r\n]", 98)]
        public static void SkipSingleSegment(string jsonString, int expectedConsumed)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                        break;
                }
            }
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(dataUtf8.Length, json.CurrentState.BytesConsumed);

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                var stateInner = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
                var jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, stateInner);

                while (jsonSlice.Read())
                {
                    JsonTokenType tokenType = jsonSlice.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                            break;
                    }
                }

                int prevConsumed = (int)jsonSlice.BytesConsumed;
                Assert.Equal(prevConsumed, jsonSlice.CurrentState.BytesConsumed);
                jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(prevConsumed), isFinalBlock: true, jsonSlice.CurrentState);

                while (jsonSlice.Read())
                {
                    JsonTokenType tokenType = jsonSlice.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                            break;
                    }
                }

                Assert.Equal(dataUtf8.Length - prevConsumed, jsonSlice.BytesConsumed);
                Assert.Equal(jsonSlice.BytesConsumed, jsonSlice.CurrentState.BytesConsumed);
            }
        }

        [Theory]
        [InlineData("//", 0, 0)]
        [InlineData("//\n", 0, 0)]
        [InlineData("/**/", 0, 0)]
        [InlineData("/*/*/", 0, 0)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", 0, 13)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", 0, 13)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", 0, 17)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", 0, 17)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", 0, 17)]
        public static void CommentsAreInvalidByDefault(string jsonString, int expectedlineNumber, int expectedPosition)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, default);

            try
            {
                while (json.Read())
                {
                    JsonTokenType tokenType = json.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                            break;
                    }
                }
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }
        }

        [Theory]
        [InlineData("//", 0, 0)]
        [InlineData("//\n", 0, 0)]
        [InlineData("/**/", 0, 0)]
        [InlineData("/*/*/", 0, 0)]

        [InlineData("//T\u6F22\u5B57his is a \u6F22\u5B57comment before json\n\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json", 0, 13)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json\n", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a \u6F22\u5B57comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a \u6F22\u5B57comment after json with new line\n", 0, 13)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a \u6F22\u5B57comment between key-value pairs\n 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a \u6F22\u5B57comment between key-value pairs on the same line\n}", 0, 17)]

        [InlineData("/*T\u6F22\u5B57his is a multi-line \u6F22\u5B57comment before json*/\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line \u6F22\u5B57comment after json*/", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line \u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a \u6F22\u5B57comment between key-value pairs*/ 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a \u6F22\u5B57comment between key-value pairs on the same line*/}", 0, 17)]

        [InlineData("/*T\u6F22\u5B57his is a split multi-line \n\u6F22\u5B57comment before json*/\"hello\"", 0, 0)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \n\u6F22\u5B57comment after json*/", 0, 13)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"b\u6F22\u5B57eta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment", 1, 0)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \n\u6F22\u5B57comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs*/ 30}", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \n\u6F22\u5B57comment between key-value pairs on the same line*/}", 0, 17)]
        public static void CommentsAreInvalidByDefaultSingleSegment(string jsonString, int expectedlineNumber, int expectedPosition)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, default);

            try
            {
                while (json.Read())
                {
                    JsonTokenType tokenType = json.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                            break;
                    }
                }
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                var jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, default);
                try
                {
                    while (jsonSlice.Read())
                    {
                        JsonTokenType tokenType = jsonSlice.TokenType;
                        switch (tokenType)
                        {
                            case JsonTokenType.Comment:
                                Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                                break;
                        }
                    }

                    Assert.Equal(jsonSlice.BytesConsumed, jsonSlice.CurrentState.BytesConsumed);
                    jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan((int)jsonSlice.BytesConsumed), isFinalBlock: true, jsonSlice.CurrentState);
                    while (jsonSlice.Read())
                    {
                        JsonTokenType tokenType = jsonSlice.TokenType;
                        switch (tokenType)
                        {
                            case JsonTokenType.Comment:
                                Assert.True(false, "TokenType should never be 'Comment' when we are skipping them.");
                                break;
                        }
                    }

                    Assert.True(false, "Expected JsonReaderException was not thrown with multi-segment data.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedPosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [InlineData("//\n}", 1, 0)]
        [InlineData("//comment\n}", 1, 0)]
        [InlineData("/**/}", 0, 4)]
        [InlineData("/*\n*/}", 1, 2)]
        [InlineData("/*comment\n*/}", 1, 2)]
        [InlineData("/*/*/}", 0, 5)]
        [InlineData("//This is a comment before json\n\"hello\"{", 1, 7)]
        [InlineData("\"hello\"//This is a comment after json\n{", 1, 0)]
        [InlineData("\"gamma\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 3, 28)]
        [InlineData("\"delta\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 4, 18)]
        [InlineData("\"hello\"//This is a comment after json with new line\n{", 1, 0)]
        [InlineData("{\"age\" : \n//This is a comment between key-value pairs\n 30}{", 2, 4)]
        [InlineData("{\"age\" : 30//This is a comment between key-value pairs on the same line\n}{", 1, 1)]
        [InlineData("/*This is a multi-line comment before json*/\"hello\"{", 0, 51)]
        [InlineData("\"hello\"/*This is a multi-line comment after json*/{", 0, 50)]
        [InlineData("\"gamma\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 2, 28)]
        [InlineData("\"delta\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 3, 18)]
        [InlineData("{\"age\" : \n/*This is a comment between key-value pairs*/ 30}{", 1, 49)]
        [InlineData("{\"age\" : 30/*This is a comment between key-value pairs on the same line*/}{", 0, 74)]
        [InlineData("/*This is a split multi-line \ncomment before json*/\"hello\"{", 1, 28)]
        [InlineData("\"hello\"/*This is a split multi-line \ncomment after json*/{", 1, 20)]
        [InlineData("\"gamma\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 3, 28)]
        [InlineData("\"delta\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 4, 18)]
        [InlineData("{\"age\" : \n/*This is a split multi-line \ncomment between key-value pairs*/ 30}{", 2, 37)]
        [InlineData("{\"age\" : 30/*This is a split multi-line \ncomment between key-value pairs on the same line*/}{", 1, 51)]

        [InlineData("//\n\u6F22\u5B57}", 1, 0)]
        [InlineData("//c\u6F22\u5B57omment\n\u6F22\u5B57}", 1, 0)]
        [InlineData("/**/\u6F22\u5B57}", 0, 4)]
        [InlineData("/*\n*/\u6F22\u5B57}", 1, 2)]
        [InlineData("/*c\u6F22\u5B57omment\n*/\u6F22\u5B57}", 1, 2)]
        [InlineData("/*/*/\u6F22\u5B57}", 0, 5)]
        [InlineData("//T\u6F22\u5B57his is a comment before json\n\"hello\"\u6F22\u5B57{", 1, 7)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a comment after json\n\u6F22\u5B57{", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 3, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 4, 18)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a comment after json with new line\n\u6F22\u5B57{", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a comment between key-value pairs\n 30}\u6F22\u5B57{", 2, 4)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a comment between key-value pairs on the same line\n}\u6F22\u5B57{", 1, 1)]
        [InlineData("/*T\u6F22\u5B57his is a multi-line comment before json*/\"hello\"\u6F22\u5B57{", 0, 57)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line comment after json*/\u6F22\u5B57{", 0, 56)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 2, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 3, 18)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a comment between key-value pairs*/ 30}\u6F22\u5B57{", 1, 49)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a comment between key-value pairs on the same line*/}\u6F22\u5B57{", 0, 80)]
        [InlineData("/*T\u6F22\u5B57his is a split multi-line \ncomment before json*/\"hello\"\u6F22\u5B57{", 1, 28)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \ncomment after json*/\u6F22\u5B57{", 1, 20)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 3, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 4, 18)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \ncomment between key-value pairs*/ 30}\u6F22\u5B57{", 2, 37)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \ncomment between key-value pairs on the same line*/}\u6F22\u5B57{", 1, 51)]

        [InlineData("{   // comment \n   ]", 1, 3)]
        [InlineData("[   // comment \n   }", 1, 3)]
        [InlineData("{   /* comment */   ]", 0, 20)]
        [InlineData("[   /* comment */   }", 0, 20)]
        public static void InvalidJsonWithComments(string jsonString, int expectedlineNumber, int expectedPosition)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }

            state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }
        }

        [Theory]
        [InlineData("//\n}", 1, 0)]
        [InlineData("//comment\n}", 1, 0)]
        [InlineData("/**/}", 0, 4)]
        [InlineData("/*\n*/}", 1, 2)]
        [InlineData("/*comment\n*/}", 1, 2)]
        [InlineData("/*/*/}", 0, 5)]
        [InlineData("//This is a comment before json\n\"hello\"{", 1, 7)]
        [InlineData("\"hello\"//This is a comment after json\n{", 1, 0)]
        [InlineData("\"gamma\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 3, 28)]
        [InlineData("\"delta\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 4, 18)]
        [InlineData("\"hello\"//This is a comment after json with new line\n{", 1, 0)]
        [InlineData("{\"age\" : \n//This is a comment between key-value pairs\n 30}{", 2, 4)]
        [InlineData("{\"age\" : 30//This is a comment between key-value pairs on the same line\n}{", 1, 1)]
        [InlineData("/*This is a multi-line comment before json*/\"hello\"{", 0, 51)]
        [InlineData("\"hello\"/*This is a multi-line comment after json*/{", 0, 50)]
        [InlineData("\"gamma\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 2, 28)]
        [InlineData("\"delta\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 3, 18)]
        [InlineData("{\"age\" : \n/*This is a comment between key-value pairs*/ 30}{", 1, 49)]
        [InlineData("{\"age\" : 30/*This is a comment between key-value pairs on the same line*/}{", 0, 74)]
        [InlineData("/*This is a split multi-line \ncomment before json*/\"hello\"{", 1, 28)]
        [InlineData("\"hello\"/*This is a split multi-line \ncomment after json*/{", 1, 20)]
        [InlineData("\"gamma\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*/{//Another single-line comment", 3, 28)]
        [InlineData("\"delta\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/{", 4, 18)]
        [InlineData("{\"age\" : \n/*This is a split multi-line \ncomment between key-value pairs*/ 30}{", 2, 37)]
        [InlineData("{\"age\" : 30/*This is a split multi-line \ncomment between key-value pairs on the same line*/}{", 1, 51)]

        [InlineData("//\n\u6F22\u5B57}", 1, 0)]
        [InlineData("//c\u6F22\u5B57omment\n\u6F22\u5B57}", 1, 0)]
        [InlineData("/**/\u6F22\u5B57}", 0, 4)]
        [InlineData("/*\n*/\u6F22\u5B57}", 1, 2)]
        [InlineData("/*c\u6F22\u5B57omment\n*/\u6F22\u5B57}", 1, 2)]
        [InlineData("/*/*/\u6F22\u5B57}", 0, 5)]
        [InlineData("//T\u6F22\u5B57his is a comment before json\n\"hello\"\u6F22\u5B57{", 1, 7)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a comment after json\n\u6F22\u5B57{", 1, 0)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 3, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n//This is a comment after json\n//Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 4, 18)]
        [InlineData("\"h\u6F22\u5B57ello\"//This is a comment after json with new line\n\u6F22\u5B57{", 1, 0)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n//This is a comment between key-value pairs\n 30}\u6F22\u5B57{", 2, 4)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30//This is a comment between key-value pairs on the same line\n}\u6F22\u5B57{", 1, 1)]
        [InlineData("/*T\u6F22\u5B57his is a multi-line comment before json*/\"hello\"\u6F22\u5B57{", 0, 57)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a multi-line comment after json*/\u6F22\u5B57{", 0, 56)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 2, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a multi-line comment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 3, 18)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a comment between key-value pairs*/ 30}\u6F22\u5B57{", 1, 49)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a comment between key-value pairs on the same line*/}\u6F22\u5B57{", 0, 80)]
        [InlineData("/*T\u6F22\u5B57his is a split multi-line \ncomment before json*/\"hello\"\u6F22\u5B57{", 1, 28)]
        [InlineData("\"h\u6F22\u5B57ello\"/*This is a split multi-line \ncomment after json*/\u6F22\u5B57{", 1, 20)]
        [InlineData("\"g\u6F22\u5B57amma\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*/\u6F22\u5B57{//Another single-line comment", 3, 28)]
        [InlineData("\"d\u6F22\u5B57elta\" \r\n/*This is a split multi-line \ncomment after json*///Here is another comment\n/*and a multi-line comment*///Another single-line comment\n\t  /*blah * blah*/\u6F22\u5B57{", 4, 18)]
        [InlineData("{\"a\u6F22\u5B57ge\" : \n/*This is a split multi-line \ncomment between key-value pairs*/ 30}\u6F22\u5B57{", 2, 37)]
        [InlineData("{\"a\u6F22\u5B57ge\" : 30/*This is a split multi-line \ncomment between key-value pairs on the same line*/}\u6F22\u5B57{", 1, 51)]
        public static void InvalidJsonWithCommentsSingleSegment(string jsonString, int expectedlineNumber, int expectedPosition)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }
        }

        [Theory]
        [MemberData(nameof(LotsOfCommentsTests))]
        public static void SkipLotsOfComments(string valueString, bool insideArray, string expectedString)
        {
            var builder = new StringBuilder();
            if (insideArray)
            {
                builder.Append("[");
            }
            for (int i = 0; i < 100_000; i++)
            {
                builder.Append("// comment ").Append(i).Append("\n");
            }
            builder.Append(valueString);
            if (insideArray)
            {
                builder.Append("]");
            }
            string jsonString = builder.ToString();
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            if (insideArray)
            {
                Assert.True(json.Read());
                Assert.True(json.TokenType == JsonTokenType.StartArray);
            }

            if (json.Read())
            {
                bool isTokenPrimitive = json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null;
                Assert.True(isTokenPrimitive);
                switch (json.TokenType)
                {
                    case JsonTokenType.Null:
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan));
                        break;
                    case JsonTokenType.Number:
                        if (json.ValueSpan.Contains((byte)'.'))
                        {
                            Assert.True(json.TryGetDoubleValue(out double numberValue));
                            // Use InvariantCulture to format the numbers to make sure they retain the decimal point '.'
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Assert.True(json.TryGetInt32Value(out int numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        break;
                    case JsonTokenType.String:
                        string stringValue = json.GetStringValue();
                        Assert.Equal(expectedString, stringValue);
                        break;
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        bool boolValue = json.GetBooleanValue();
                        Assert.Equal(expectedString, boolValue.ToString(CultureInfo.InvariantCulture));
                        break;
                }
            }

            if (insideArray)
            {
                Assert.True(json.Read());
                Assert.True(json.TokenType == JsonTokenType.EndArray);
            }

            Assert.False(json.Read());
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(LotsOfCommentsTests))]
        public static void ConsumeLotsOfComments(string valueString, bool insideArray, string expectedString)
        {
            var builder = new StringBuilder();
            if (insideArray)
            {
                builder.Append("[");
            }
            for (int i = 0; i < 100_000; i++)
            {
                builder.Append("// comment ").Append(i).Append("\n");
            }
            builder.Append(valueString);
            if (insideArray)
            {
                builder.Append("]");
            }
            string jsonString = builder.ToString();
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            bool foundPrimitiveValue = false;
            while (json.Read())
            {
                bool isTokenPrimitive = json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null;

                if (insideArray)
                {
                    Assert.True(isTokenPrimitive || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.StartArray || json.TokenType == JsonTokenType.EndArray);
                }
                else
                {
                    Assert.True(isTokenPrimitive || json.TokenType == JsonTokenType.Comment);
                }

                switch (json.TokenType)
                {
                    case JsonTokenType.Null:
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan));
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.Number:
                        if (json.ValueSpan.Contains((byte)'.'))
                        {
                            Assert.True(json.TryGetDoubleValue(out double numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Assert.True(json.TryGetInt32Value(out int numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.String:
                        string stringValue = json.GetStringValue();
                        Assert.Equal(expectedString, stringValue);
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        bool boolValue = json.GetBooleanValue();
                        Assert.Equal(expectedString, boolValue.ToString(CultureInfo.InvariantCulture));
                        foundPrimitiveValue = true;
                        break;
                }
            }
            Assert.True(foundPrimitiveValue);
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { true, TestCaseType.Basic, SR.BasicJson},
                    new object[] { true, TestCaseType.BasicLargeNum, SR.BasicJsonWithLargeNum}, // Json.NET treats numbers starting with 0 as octal (0425 becomes 277)
                    new object[] { true, TestCaseType.BroadTree, SR.BroadTree}, // \r\n behavior is different between Json.NET and System.Text.Json
                    new object[] { true, TestCaseType.DeepTree, SR.DeepTree},
                    new object[] { true, TestCaseType.FullSchema1, SR.FullJsonSchema1},
                    new object[] { true, TestCaseType.HelloWorld, SR.HelloWorld},
                    new object[] { true, TestCaseType.LotsOfNumbers, SR.LotsOfNumbers},
                    new object[] { true, TestCaseType.LotsOfStrings, SR.LotsOfStrings},
                    new object[] { true, TestCaseType.ProjectLockJson, SR.ProjectLockJson},
                    new object[] { true, TestCaseType.Json400B, SR.Json400B},
                    new object[] { true, TestCaseType.Json4KB, SR.Json4KB},
                    new object[] { true, TestCaseType.Json40KB, SR.Json40KB},
                    new object[] { true, TestCaseType.Json400KB, SR.Json400KB},

                    new object[] { false, TestCaseType.Basic, SR.BasicJson},
                    new object[] { false, TestCaseType.BasicLargeNum, SR.BasicJsonWithLargeNum}, // Json.NET treats numbers starting with 0 as octal (0425 becomes 277)
                    new object[] { false, TestCaseType.BroadTree, SR.BroadTree}, // \r\n behavior is different between Json.NET and System.Text.Json
                    new object[] { false, TestCaseType.DeepTree, SR.DeepTree},
                    new object[] { false, TestCaseType.FullSchema1, SR.FullJsonSchema1},
                    new object[] { false, TestCaseType.HelloWorld, SR.HelloWorld},
                    new object[] { false, TestCaseType.LotsOfNumbers, SR.LotsOfNumbers},
                    new object[] { false, TestCaseType.LotsOfStrings, SR.LotsOfStrings},
                    new object[] { false, TestCaseType.ProjectLockJson, SR.ProjectLockJson},
                    new object[] { false, TestCaseType.Json400B, SR.Json400B},
                    new object[] { false, TestCaseType.Json4KB, SR.Json4KB},
                    new object[] { false, TestCaseType.Json40KB, SR.Json40KB},
                    new object[] { false, TestCaseType.Json400KB, SR.Json400KB}
                };
            }
        }

        public static IEnumerable<object[]> SpecialNumTestCases
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { TestCaseType.FullSchema2, SR.FullJsonSchema2},
                    new object[] { TestCaseType.SpecialNumForm, SR.JsonWithSpecialNumFormat},
                };
            }
        }

        public static IEnumerable<object[]> LotsOfCommentsTests
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"   12345   ", true, "12345"},
                    new object[] {"   12345.67890e-12   ", true, "1.23456789E-08"},
                    new object[] {"   true  ", true, "True"},
                    new object[] {"   false   ", true, "False"},
                    new object[] {"   null   ", true, "null"},
                    new object[] {"   \" Test string with \\\"nested quotes \\\" and hex: \\uABCD values! \"   ", true, " Test string with \\\"nested quotes \\\" and hex: \\uABCD values! "},

                    new object[] {"   12345   ", false, "12345"},
                    new object[] {"   12345.67890e-12   ", false, "1.23456789E-08"},
                    new object[] {"   true  ", false, "True"},
                    new object[] {"   false   ", false, "False"},
                    new object[] {"   null   ", false, "null"},
                    new object[] {"   \" Test string with \\\"nested quotes \\\" and hex: \\uABCD values! \"   ", false, " Test string with \\\"nested quotes \\\" and hex: \\uABCD values! "},
                };
            }
        }

        public static IEnumerable<object[]> SingleValueJson
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"  \"h\u6F22\u5B57ello\"  ", "h\u6F22\u5B57ello"},    // "\u6F22\u5B57" is Chinese for "Chinese character" (from the Han script)
                    new object[] {"  \"he\\r\\n\\\"l\\\\\\\"lo\\\\\"  ", "he\\r\\n\\\"l\\\\\\\"lo\\\\"},
                    new object[] {"  12345  ", "12345"},
                    new object[] {"  null  ", "null"},
                    new object[] {"  true  ", "true"},
                    new object[] {"  false  ", "false"},
                };
            }
        }

        public static IEnumerable<object[]> InvalidJsonStrings
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"\"", 0, 0},
                    new object[] {"{]", 0, 1},
                    new object[] {"[}", 0, 1},
                    new object[] {"nul", 0, 3},
                    new object[] {"tru", 0, 3},
                    new object[] {"fals", 0, 4},
                    new object[] {"\"a\u6F22\u5B57ge\":", 0, 11},
                    new object[] {"{\"a\u6F22\u5B57ge\":", 0, 13},
                    new object[] {"{\"name\":\"A\u6F22\u5B57hso", 0, 8},
                    new object[] {"12345.1.", 0, 7},
                    new object[] {"-", 0, 1},
                    new object[] {"-f", 0, 1},
                    new object[] {"1.f", 0, 2},
                    new object[] {"0.", 0, 2},
                    new object[] {"0.1f", 0, 3},
                    new object[] {"0.1e1f", 0, 5},
                    new object[] {"123,", 0, 3},
                    new object[] {"false,", 0, 5},
                    new object[] {"true,", 0, 4},
                    new object[] {"null,", 0, 4},
                    new object[] {"trUe,", 0, 2},
                    new object[] {"\"h\u6F22\u5B57ello\",", 0, 13},
                    new object[] {"\"\\u12z3\"", 0, 5},
                    new object[] {"\"\\u12]3\"", 0, 5},
                    new object[] {"\"\\u12=3\"", 0, 5},
                    new object[] {"\"\\u12$3\"", 0, 5},
                    new object[] {"\"\\u12\"", 0, 5},
                    new object[] {"\"\\u120\"", 0, 6},
                    new object[] {"01", 0, 1},
                    new object[] {"1a", 0, 1},
                    new object[] {"-01", 0, 2},
                    new object[] {"10.5e", 0, 5},
                    new object[] {"10.5e-", 0, 6},
                    new object[] {"10.5e-0.2", 0, 7},
                    new object[] {"{\"age\":30, \"ints\":[1, 2, 3, 4, 5.1e7.3]}", 0, 36},
                    new object[] {"{\"age\":30, \r\n \"num\":-0.e, \r\n \"ints\":[1, 2, 3, 4, 5]}", 1, 10},
                    new object[] {"{ \"number\": 00", 0, 13},
                    new object[] {"{{}}", 0, 1},
                    new object[] {"[[]", 0, 3},
                    new object[] {"[[{{}}]]", 0, 3},
                    new object[] {"[1, 2, 3, ]", 0, 10},
                    new object[] {"{\"ints\":[1, 2, 3, 4, 5", 0, 22},
                    new object[] {"{\"s\u6F22\u5B57trings\":[\"a\u6F22\u5B57bc\", \"def\"", 0, 36},
                    new object[] {"{\"age\":30, \"ints\":[1, 2, 3, 4, 5}}", 0, 32},
                    new object[] {"{\"age\":30, \"name\":\"test}", 0, 18},
                    new object[] {"{\r\n\"isActive\": false \"\r\n}", 1, 18},
                    new object[] {"[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[}]]]]}]]]]", 1, 28},
                    new object[] {"[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2:[]}]]]]}]]]]", 1, 19},
                    new object[] {"[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[]},[}]]]]}]]]]", 1, 32},
                    new object[] {"{\r\n\t\"isActive\": false,\r\n\t\"array\": [\r\n\t\t[{\r\n\t\t\t\"id\": 1\r\n\t\t}]\r\n\t]\r\n}", 3, 3, 3},
                    new object[] {"{\"Here is a \u6F22\u5B57string: \\\"\\\"\":\"Here is \u6F22\u5B57a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str:\"\\\"\\\"\"}", 4, 35},
                    new object[] {"\"hel\rlo\"", 0, 4},
                    new object[] {"\"hel\nlo\"", 0, 4},
                    new object[] {"\"hel\\uABCXlo\"", 0, 9},
                    new object[] {"\"hel\\\tlo\"", 0, 5},
                    new object[] {"\"hel\rlo\\\"\"", 0, 4},
                    new object[] {"\"hel\nlo\\\"\"", 0, 4},
                    new object[] {"\"hel\\uABCXlo\\\"\"", 0, 9},
                    new object[] {"\"hel\\\tlo\\\"\"", 0, 5},
                    new object[] {"\"he\\nl\rlo\\\"\"", 1, 1},
                    new object[] {"\"he\\nl\nlo\\\"\"", 1, 1},
                    new object[] {"\"he\\nl\\uABCXlo\\\"\"", 1, 6},
                    new object[] {"\"he\\nl\\\tlo\\\"\"", 1, 2},
                    new object[] {"\"he\\nl\rlo", 1, 1},
                    new object[] {"\"he\\nl\nlo", 1, 1},
                    new object[] {"\"he\\nl\\uABCXlo", 1, 6},
                    new object[] {"\"he\\nl\\\tlo", 1, 2},
                };
            }
        }
    }
}
