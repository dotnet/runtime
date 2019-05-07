// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Fact]
        public static void DefaultUtf8JsonReader()
        {
            Utf8JsonReader json = default;

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.Equal(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(0, json.CurrentState.BytesConsumed);
            Assert.Equal(default, json.CurrentState.Position);
            Assert.Equal(0, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.False(json.Read());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TextEquals("".AsSpan()));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TextEquals(default(ReadOnlySpan<char>)));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TextEquals(default(ReadOnlySpan<byte>)));

            TestGetMethodsOnDefault();
        }

        private static void TestGetMethodsOnDefault()
        {
            Utf8JsonReader json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetDateTime(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetDateTime());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetDateTimeOffset(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetDateTimeOffset());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetDecimal(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetDecimal());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetDouble(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetDouble());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetInt32(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetInt32());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetInt64(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetInt64());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetSingle(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetSingle());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetUInt32(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetUInt32());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetUInt64(out _));
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetUInt64());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetString());

            json = default;
            JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetBoolean());
        }

        [Fact]
        public static void InitialState()
        {
            var json = new Utf8JsonReader(Encoding.UTF8.GetBytes("1"), isFinalBlock: true, state: default);

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.Equal(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(0, json.CurrentState.BytesConsumed);
            Assert.Equal(default, json.CurrentState.Position);
            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.False(json.Read());
        }

        [Fact]
        public static void StateRecovery()
        {
            byte[] utf8 = Encoding.UTF8.GetBytes("[1]");
            var json = new Utf8JsonReader(utf8, isFinalBlock: false, state: default);

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.Equal(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(0, json.CurrentState.BytesConsumed);
            Assert.Equal(default, json.CurrentState.Position);
            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.True(json.Read());

            Assert.Equal(2, json.BytesConsumed);
            Assert.Equal(1, json.TokenStartIndex);
            Assert.Equal(1, json.CurrentDepth);
            Assert.Equal(JsonTokenType.Number, json.TokenType);
            Assert.Equal(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(new byte[] { (byte)'1'}));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(2, json.CurrentState.BytesConsumed);
            Assert.Equal(default, json.CurrentState.Position);
            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            JsonReaderState state = json.CurrentState;

            json = new Utf8JsonReader(utf8.AsSpan((int)json.BytesConsumed), isFinalBlock: true, state);

            Assert.Equal(0, json.BytesConsumed);    // Not retained
            Assert.Equal(0, json.TokenStartIndex);  // Not retained
            Assert.Equal(1, json.CurrentDepth);
            Assert.Equal(JsonTokenType.Number, json.TokenType);
            Assert.Equal(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(0, json.CurrentState.BytesConsumed);
            Assert.Equal(default, json.CurrentState.Position);
            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.False(json.Read());
        }

        // TestCaseType is only used to give the json strings a descriptive name.
        [Theory]
        [MemberData(nameof(TestCases))]
        public static void TestJsonReaderUtf8(bool compactData, TestCaseType type, string jsonString)
        {
            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            SpanSequenceStatesAreEqual(dataUtf8);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int length);
            string actualStr = Encoding.UTF8.GetString(result, 0, length);

            byte[] resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length);
            string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

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
            actualStr = Encoding.UTF8.GetString(result, 0, length);
            resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Skip);
            actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

            Assert.Equal(expectedStr, actualStr);
            Assert.Equal(expectedStr, actualStrSequence);

            result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Allow);
            actualStr = Encoding.UTF8.GetString(result, 0, length);
            resultSequence = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, JsonCommentHandling.Allow);
            actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

            Assert.Equal(expectedStr, actualStr);
            Assert.Equal(expectedStr, actualStrSequence);
        }

        [Theory]
        [MemberData(nameof(LargeTestCases))]
        public static void TestPartialLargeJsonReader(bool compactData, TestCaseType type, string jsonString)
        {
            // Skipping really large JSON since slicing them (O(n^2)) is too slow.
            if (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson)
            {
                return;
            }

            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength);
            var outputArray = new byte[outputLength];
            Span<byte> outputSpan = outputArray;

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
                Assert.Equal(default, json.Position);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), isFinalBlock: true, json.CurrentState);
                output = JsonTestHelper.ReaderLoop(outputSpan.Length - written, out int length, ref json);
                output.AsSpan(0, length).CopyTo(outputSpan.Slice(written));
                written += length;
                Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                Assert.Equal(default, json.Position);
                Assert.Equal(default, json.CurrentState.Position);

                Assert.Equal(outputSpan.Length, written);
                string actualStr = Encoding.UTF8.GetString(outputArray);
                Assert.Equal(expectedStr, actualStr);
            }
        }

        [Theory]
        // Skipping large JSON since slicing them (O(n^2)) is too slow.
        [MemberData(nameof(SmallTestCases))]
        public static void TestPartialJsonReader(bool compactData, TestCaseType type, string jsonString)
        {
            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength);
            var outputArray = new byte[outputLength];
            Span<byte> outputSpan = outputArray;

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
                Assert.Equal(default, json.Position);

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
                    Assert.Equal(default, json.Position);
                    Assert.Equal(default, json.CurrentState.Position);

                    Assert.Equal(outputSpan.Length, written);
                    string actualStr = Encoding.UTF8.GetString(outputArray);
                    Assert.Equal(expectedStr, actualStr);
                }
            }
        }

        [Fact]
        public static void TestSingleStrings()
        {
            string jsonString = "\"Hello, \\u0041hson!\"";
            string expectedString = "Hello, \\u0041hson!, ";

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength);
            var outputArray = new byte[outputLength];
            Span<byte> outputSpan = outputArray;

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                JsonReaderState state = default;
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                byte[] output = JsonTestHelper.ReaderLoop(outputSpan.Length, out int firstLength, ref json);
                output.AsSpan(0, firstLength).CopyTo(outputSpan);
                int written = firstLength;

                long consumed = json.BytesConsumed;
                Assert.Equal(consumed, json.CurrentState.BytesConsumed);
                Assert.Equal(default, json.Position);
                Assert.Equal(0, json.TokenStartIndex);

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
                    Assert.Equal(default, json.Position);
                    Assert.Equal(default, json.CurrentState.Position);
                    Assert.Equal(0, json.TokenStartIndex);

                    Assert.Equal(outputSpan.Length, written);
                    string actualStr = Encoding.UTF8.GetString(outputArray);
                    Assert.Equal(expectedString, actualStr);
                }
            }
        }

        [Fact]
        public static void CurrentDepthArrayTest()
        {
            string jsonString =
@"[
    [
        1,
        2,
        3
    ]
]";

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            Assert.Equal(0, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(0, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(1, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(1, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(0, json.CurrentDepth);
            Assert.False(json.Read());
            Assert.Equal(0, json.CurrentDepth);
        }

        [Fact]
        public static void CurrentDepthObjectTest()
        {
            string jsonString =
@"{
    ""array"": [
        1,
        2,
        3,
        {}
    ]
}";

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            Assert.Equal(0, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(0, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(1, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(1, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(2, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(1, json.CurrentDepth);
            Assert.True(json.Read());
            Assert.Equal(0, json.CurrentDepth);
            Assert.False(json.Read());
            Assert.Equal(0, json.CurrentDepth);
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
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            // Set the max depth sufficiently large to account for the depth padding.
            byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int outputLength, maxDepth: 256);
            Span<byte> outputSpan = new byte[outputLength];
            string actualStr = Encoding.UTF8.GetString(result, 0, outputLength);

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
                }
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SpecialNumTestCases))]
        public static void TestPartialJsonReaderSlicesSpecialNumbers(TestCaseType type, string jsonString)
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

                    var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = depth });
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

                    var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = depth + 1 });
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
        [InlineData("[123,//hi\n456//bye\n]", "123456", "123//hi\n456//bye\n")]
        [InlineData("[123,//hi\r456//bye\r]", "123456", "123//hi\r456//bye\r")]
        [InlineData("[123,//hi\r\n456\r\n]", "123456", "123//hi\r\n456")]
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
                        Assert.True(json.ValueSequence.IsEmpty);
                        if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                            builder.Append(Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
                    }

                    long consumed = json.BytesConsumed;
                    Assert.Equal(consumed, json.CurrentState.BytesConsumed);
                    json = new Utf8JsonReader(data.AsSpan((int)consumed), true, json.CurrentState);
                    while (json.Read())
                    {
                        Assert.True(json.ValueSequence.IsEmpty);
                        if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                            builder.Append(Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
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

            var state = new JsonReaderState(new JsonReaderOptions { MaxDepth = depth - 1 });
            var json = new Utf8JsonReader(data, isFinalBlock: true, state);

            try
            {
                int maxDepth = 0;
                while (json.Read())
                {
                    if (maxDepth < json.CurrentDepth)
                        maxDepth = json.CurrentDepth;
                }
                Assert.True(false, $"Expected JsonException was not thrown. Max depth allowed = {json.CurrentState.Options.MaxDepth} | Max depth reached = {maxDepth}");
            }
            catch (JsonException)
            { }

            jsonStr = JsonTestHelper.WriteDepthArray(depth - 1);
            data = Encoding.UTF8.GetBytes(jsonStr);

            state = new JsonReaderState(new JsonReaderOptions { MaxDepth = depth - 1 });
            json = new Utf8JsonReader(data, isFinalBlock: true, state);

            try
            {
                int maxDepth = 0;
                while (json.Read())
                {
                    if (maxDepth < json.CurrentDepth)
                        maxDepth = json.CurrentDepth;
                }
                Assert.True(false, $"Expected JsonException was not thrown. Max depth allowed = {json.CurrentState.Options.MaxDepth} | Max depth reached = {maxDepth}");
            }
            catch (JsonException)
            { }
        }

        [Theory]
        [InlineData("{\"nam\\\"e\":\"ah\\\"son\"}", "nam\\\"e, ah\\\"son, ", "nam\"e, ah\"son, ")]
        [InlineData("{\"Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str\":\"\\\"\\\"\"}",
            "Here is a string: \\\"\\\", Here is a, Here is a back slash\\\\, Multiline\\r\\n String\\r\\n, \\tMul\\r\\ntiline String, \\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\, str, \\\"\\\", ",
            "Here is a string: \"\", Here is a, Here is a back slash\\, Multiline\r\n String\r\n, \tMul\r\ntiline String, \"somequote\"\tMu\"\"l\r\ntiline\"another\" String\\, str, \"\", ")]
        public static void TestJsonReaderUtf8SpecialString(string jsonString, string expectedStr, string expectedEscapedStr)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
                byte[] result = JsonTestHelper.ReturnBytesHelper(dataUtf8, out int length, commentHandling);
                string actualStr = Encoding.UTF8.GetString(result, 0, length);

                Assert.Equal(expectedStr, actualStr);

                result = JsonTestHelper.SequenceReturnBytesHelper(dataUtf8, out length, commentHandling);
                actualStr = Encoding.UTF8.GetString(result, 0, length);

                Assert.Equal(expectedStr, actualStr);

                object jsonValues = JsonTestHelper.ReturnObjectHelper(dataUtf8, commentHandling);
                string str = JsonTestHelper.ObjectToString(jsonValues);
                Assert.Equal(expectedEscapedStr, str);

                Stream stream = new MemoryStream(dataUtf8);
                TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                expectedEscapedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);
                Assert.Equal(expectedEscapedStr, str);
            }
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
                        Assert.True(json.ValueSequence.IsEmpty);
                        // Check if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
                        Assert.True(json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null);
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
                        Assert.Equal(2, json.TokenStartIndex);
                    }

                    long consumed = json.BytesConsumed;
                    Assert.Equal(consumed, json.CurrentState.BytesConsumed);
                    json = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), true, json.CurrentState);
                    while (json.Read())
                    {
                        Assert.True(json.ValueSequence.IsEmpty);
                        // Check if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
                        Assert.True(json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null);
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
                        if (consumed <= 2)
                        {
                            Assert.Equal(2 - consumed, json.TokenStartIndex);
                        }
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
                Assert.Equal(default, json.Position);
                Assert.Equal(default, json.CurrentState.Position);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)json.BytesConsumed), true, json.CurrentState);
                while (json.Read())
                    ;
                Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                Assert.Equal(default, json.Position);
                Assert.Equal(default, json.CurrentState.Position);
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
                    Assert.True(false, "Expected JsonException was not thrown.");
                }
                catch (JsonException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Theory]
        [InlineData("{]", 0, 1, 64)]
        [InlineData("[}", 0, 1, 64)]
        [InlineData("nulz", 0, 3, 64)]
        [InlineData("truz", 0, 3, 64)]
        [InlineData("falsz", 0, 4, 64)]
        [InlineData("\"a\u6F22\u5B57ge\":", 0, 11, 64)]
        [InlineData("12345.1.", 0, 7, 64)]
        [InlineData("-f", 0, 1, 64)]
        [InlineData("1.f", 0, 2, 64)]
        [InlineData("0.1f", 0, 3, 64)]
        [InlineData("0.1e1f", 0, 5, 64)]
        [InlineData("123,", 0, 3, 64)]
        [InlineData("01", 0, 1, 64)]
        [InlineData("-01", 0, 2, 64)]
        [InlineData("10.5e-0.2", 0, 7, 64)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \"ints\":[1, 2, 3, 4, 5.1e7.3]}", 0, 42, 64)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \r\n \"num\":-0.e, \r\n \"ints\":[1, 2, 3, 4, 5]}", 1, 10, 64)]
        [InlineData("{{}}", 0, 1, 64)]
        [InlineData("[[{{}}]]", 0, 3, 64)]
        [InlineData("[1, 2, 3, ]", 0, 10, 64)]
        [InlineData("{\"a\u6F22\u5B57ge\":30, \"ints\":[1, 2, 3, 4, 5}}", 0, 38, 64)]
        [InlineData("{\r\n\"isActive\": false \"\r\n}", 1, 18, 64)]
        [InlineData("[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[}]]]]}]]]]", 1, 28, 64)]
        [InlineData("[[[[{\r\n\"t\u6F22\u5B57emp1\":[[[[{\"temp2\":[]},[}]]]]}]]]]", 1, 32, 64)]
        [InlineData("{\r\n\t\"isActive\": false,\r\n\t\"array\": [\r\n\t\t[{\r\n\t\t\t\"id\": 1\r\n\t\t}]\r\n\t]\r\n}", 3, 3, 3)]
        [InlineData("{\"Here is a \u6F22\u5B57string: \\\"\\\"\":\"Here is \u6F22\u5B57a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str:\"\\\"\\\"\"}", 4, 35, 64)]
        public static void InvalidJsonWhenPartial(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                var json = new Utf8JsonReader(dataUtf8, false, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonException was not thrown.");
                }
                catch (JsonException ex)
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
                    Assert.True(false, "Expected JsonException was not thrown.");
                }
                catch (JsonException ex)
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
                var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
                }
                catch (JsonException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                    Assert.Equal(default, json.Position);
                    Assert.Equal(default, json.CurrentState.Position);
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
                var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
                }
                catch (JsonException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                    Assert.Equal(default, json.Position);
                }

                for (int i = 0; i < dataUtf8.Length; i++)
                {
                    try
                    {
                        var stateInner = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                        var jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, stateInner);
                        while (jsonSlice.Read())
                            ;

                        long consumed = jsonSlice.BytesConsumed;
                        Assert.Equal(consumed, jsonSlice.CurrentState.BytesConsumed);
                        Assert.Equal(default, json.Position);
                        Assert.Equal(default, json.CurrentState.Position);

                        JsonReaderState jsonState = jsonSlice.CurrentState;

                        jsonSlice = new Utf8JsonReader(dataUtf8.AsSpan((int)consumed), isFinalBlock: true, jsonState);
                        while (jsonSlice.Read())
                            ;

                        Assert.True(false, "Expected JsonException was not thrown with multi-segment data.");
                    }
                    catch (JsonException ex)
                    {
                        string errorMessage = $"expectedLineNumber: {expectedlineNumber} | actual: {ex.LineNumber} | index: {i} | option: {commentHandling}";
                        string firstSegmentString = Encoding.UTF8.GetString(dataUtf8, 0, i);
                        string secondSegmentString = Encoding.UTF8.GetString(dataUtf8, i, dataUtf8.Length - i);
                        errorMessage += " | " + firstSegmentString + " | " + secondSegmentString;
                        Assert.True(expectedlineNumber == ex.LineNumber, errorMessage);
                        errorMessage = $"expectedBytePosition: {expectedBytePosition} | actual: {ex.BytePositionInLine} | index: {i} | option: {commentHandling}";
                        errorMessage += " | " + firstSegmentString + " | " + secondSegmentString;
                        Assert.True(expectedBytePosition == ex.BytePositionInLine, errorMessage);
                        Assert.Equal(default, json.Position);
                        Assert.Equal(default, json.CurrentState.Position);
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
                Assert.True(json.ValueSequence.IsEmpty);
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        if (foundComment)
                            break;
                        foundComment = true;
                        indexAfterFirstComment = json.BytesConsumed;
                        Assert.Equal(indexAfterFirstComment, json.CurrentState.BytesConsumed);
                        string actualComment = Encoding.UTF8.GetString(json.ValueSpan.ToArray()); // TODO: https://github.com/dotnet/corefx/issues/33347
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
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return\r//Another single-line comment", "//This is a comment with a carriage return\r", 59)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a line break\n//Another single-line comment", "//This is a comment with a line break\n", 54)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return and line break\r\n//Another single-line comment", "//This is a comment with a carriage return and line break\r\n", 75)]

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
                Assert.True(json.ValueSequence.IsEmpty);
                JsonTokenType tokenType = json.TokenType;
                switch (tokenType)
                {
                    case JsonTokenType.Comment:
                        if (foundComment)
                            break;
                        foundComment = true;
                        indexAfterFirstComment = json.BytesConsumed;
                        Assert.Equal(indexAfterFirstComment, json.CurrentState.BytesConsumed);
                        string actualComment = Encoding.UTF8.GetString(json.ValueSpan.ToArray());
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
                    Assert.True(json.ValueSequence.IsEmpty);
                    JsonTokenType tokenType = jsonSlice.TokenType;
                    switch (tokenType)
                    {
                        case JsonTokenType.Comment:
                            if (foundComment)
                                break;
                            foundComment = true;
                            indexAfterFirstComment = jsonSlice.BytesConsumed;
                            Assert.Equal(indexAfterFirstComment, jsonSlice.CurrentState.BytesConsumed);
                            string actualComment = Encoding.UTF8.GetString(jsonSlice.ValueSpan.ToArray());
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
                        Assert.True(json.ValueSequence.IsEmpty);
                        JsonTokenType tokenType = jsonSlice.TokenType;
                        switch (tokenType)
                        {
                            case JsonTokenType.Comment:
                                if (foundComment)
                                    break;
                                foundComment = true;
                                indexAfterFirstComment = jsonSlice.BytesConsumed;
                                Assert.Equal(indexAfterFirstComment, jsonSlice.CurrentState.BytesConsumed);
                                string actualComment = Encoding.UTF8.GetString(jsonSlice.ValueSpan.ToArray());
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
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return\r//Another single-line comment", 59)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a line break\n//Another single-line comment", 54)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return and line break\r\n//Another single-line comment", 75)]

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
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return\r//Another single-line comment", 59)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a line break\n//Another single-line comment", 54)]
        [InlineData("\"a\u6F22\u5B57lpha\" \r\n//This is a comment with a carriage return and line break\r\n//Another single-line comment", 75)]

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
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
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
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
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

                    Assert.True(false, "Expected JsonException was not thrown with multi-segment data.");
                }
                catch (JsonException ex)
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
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
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
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
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
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(expectedlineNumber, ex.LineNumber);
                Assert.Equal(expectedPosition, ex.BytePositionInLine);
            }
        }

        [Fact]
        public static void EmptyJsonIsInvalid()
        {
            var dataUtf8 = ReadOnlySpan<byte>.Empty;
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(0, ex.BytePositionInLine);
            }
        }

        [Fact]
        public static void JsonContainingOnlyWhitespaceIsInvalid()
        {
            var dataUtf8 = new byte[] { 0x20 };
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(1, ex.BytePositionInLine);
            }
        }

        [Theory]
        [InlineData("//", 2, 0)]
        [InlineData("//\n", 0, 1)]
        [InlineData("/**/", 4, 0)]
        [InlineData("/*/*/", 5, 0)]
        [InlineData("// just a comment", 17, 0)]
        [InlineData(" /* comment and whitespace */ ", 30, 0)]
        public static void JsonContainingOnlyCommentsIsInvalid(string jsonString, int expectedConsumed, int expectedLineNumber)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(expectedLineNumber, ex.LineNumber);
                Assert.Equal(expectedConsumed, ex.BytePositionInLine);
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
                Assert.Equal(0, json.TokenStartIndex);
            }

            if (json.Read())
            {
                Assert.True(json.ValueSequence.IsEmpty);
                bool isTokenPrimitive = json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null;
                Assert.True(isTokenPrimitive);
                switch (json.TokenType)
                {
                    case JsonTokenType.Null:
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
                        break;
                    case JsonTokenType.Number:
                        if (json.ValueSpan.IndexOf((byte)'.') != -1)
                        {
                            Assert.True(json.TryGetDouble(out double numberValue));
                            // Use InvariantCulture to format the numbers to make sure they retain the decimal point '.'
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Assert.True(json.TryGetInt32(out int numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        break;
                    case JsonTokenType.String:
                        string stringValue = json.GetString();
                        Assert.Equal(expectedString, stringValue);
                        break;
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        bool boolValue = json.GetBoolean();
                        Assert.Equal(expectedString, boolValue.ToString(CultureInfo.InvariantCulture));
                        break;
                }
                Assert.Equal(insideArray ? 1688894 : 1688894 - 1, json.TokenStartIndex);
            }

            if (insideArray)
            {
                Assert.True(json.Read());
                Assert.True(json.TokenType == JsonTokenType.EndArray);
                Assert.Equal(dataUtf8.Length - 1, json.TokenStartIndex);
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
                Assert.True(json.ValueSequence.IsEmpty);
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
                        Assert.Equal(expectedString, Encoding.UTF8.GetString(json.ValueSpan.ToArray()));
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.Number:
                        if (json.ValueSpan.IndexOf((byte)'.') != -1)
                        {
                            Assert.True(json.TryGetDouble(out double numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Assert.True(json.TryGetInt32(out int numberValue));
                            Assert.Equal(expectedString, numberValue.ToString(CultureInfo.InvariantCulture));
                        }
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.String:
                        string stringValue = json.GetString();
                        Assert.Equal(expectedString, stringValue);
                        foundPrimitiveValue = true;
                        break;
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        bool boolValue = json.GetBoolean();
                        Assert.Equal(expectedString, boolValue.ToString(CultureInfo.InvariantCulture));
                        foundPrimitiveValue = true;
                        break;
                }
                if (isTokenPrimitive)
                {
                    Assert.Equal(insideArray ? 1688894 : 1688894 - 1, json.TokenStartIndex);
                }
            }
            Assert.True(foundPrimitiveValue);
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);

            if (insideArray)
            {
                Assert.True(json.TokenType == JsonTokenType.EndArray);
                Assert.Equal(dataUtf8.Length - 1, json.TokenStartIndex);
            }
        }

        private static void VerifyReadLoop(ref Utf8JsonReader json, string expected)
        {
            while (json.Read())
            {
                switch (json.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.EndObject:
                        break;
                    case JsonTokenType.Comment:
                        if (expected != null)
                        {
                            byte[] data = json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan.ToArray();
                            Assert.Equal(expected, Encoding.UTF8.GetString(data));
                        }
                        else
                        {
                            Assert.True(false);
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Theory]
        [MemberData(nameof(SingleLineCommentData))]
        public static void ConsumeSingleLineCommentSingleSpanTest(string expected)
        {
            var jsonData = "{" + expected + "}";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);

            for (int i = 0; i < jsonData.Length; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                VerifyReadLoop(ref json, expected);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)state.BytesConsumed), isFinalBlock: true, state);
                VerifyReadLoop(ref json, expected);
            }
        }

        [Theory]
        [MemberData(nameof(SingleLineCommentData))]
        public static void SkipSingleLineCommentSingleSpanTest(string expected)
        {
            var jsonData = "{" + expected + "}";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);

            for (int i = 0; i < jsonData.Length; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
                var json = new Utf8JsonReader(dataUtf8.AsSpan(0, i), isFinalBlock: false, state);
                VerifyReadLoop(ref json, null);

                json = new Utf8JsonReader(dataUtf8.AsSpan((int)state.BytesConsumed), isFinalBlock: true, state);
                VerifyReadLoop(ref json, null);
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValue))]
        public static void ReadJsonTokenWithExtraValue(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: false);
                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValueAndComments))]
        public static void ReadJsonTokenWithExtraValueAndComments(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: false);
                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValueAndComments))]
        public static void ReadJsonTokenWithExtraValueAndCommentsAppended(string jsonString)
        {
            jsonString = "  /* comment */  /* comment */  " + jsonString;
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: false, commentsAppended: true);
                TestReadTokenWithExtra(utf8, commentHandling, isFinalBlock: true, commentsAppended: true);
            }
        }

        private static void TestReadTokenWithExtra(byte[] utf8, JsonCommentHandling commentHandling, bool isFinalBlock, bool commentsAppended = false)
        {
            JsonReaderState state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
            Utf8JsonReader reader = new Utf8JsonReader(utf8, isFinalBlock, state);

            if (commentsAppended && commentHandling == JsonCommentHandling.Allow)
            {
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.Comment, reader.TokenType);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.Comment, reader.TokenType);
            }

            Assert.True(reader.Read());
            if (reader.TokenType == JsonTokenType.StartArray || reader.TokenType == JsonTokenType.StartObject)
            {
                Assert.True(reader.Read());
                Assert.Contains(reader.TokenType, new[] { JsonTokenType.EndArray, JsonTokenType.EndObject });
            }

            JsonTestHelper.AssertThrows<JsonException>(reader, (jsonReader) =>
            {
                jsonReader.Read();
                if (commentHandling == JsonCommentHandling.Allow && jsonReader.TokenType == JsonTokenType.Comment)
                {
                    jsonReader.Read();
                }
            });
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommas))]
        public static void JsonWithTrailingCommas_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            {
                JsonReaderState state = default;
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);
            }

            {
                var state = new JsonReaderState(options: default);
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);
            }

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(utf8, state, allowTrailingCommas, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommas))]
        public static void JsonWithTrailingCommas_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(utf8, state, allowTrailingCommas, expectThrow: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommasAndComments))]
        public static void JsonWithTrailingCommasAndComments_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(utf8, state, allowTrailingCommas, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommasAndComments))]
        public static void JsonWithTrailingCommasAndComments_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(utf8, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(utf8, state, allowTrailingCommas, expectThrow: true);
            }
        }

        private static void TrailingCommasHelper(byte[] utf8, JsonReaderState state, bool allow, bool expectThrow)
        {
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);

            Assert.Equal(allow, state.Options.AllowTrailingCommas);
            Assert.Equal(allow, reader.CurrentState.Options.AllowTrailingCommas);

            if (expectThrow)
            {
                JsonTestHelper.AssertThrows<JsonException>(reader, (jsonReader) =>
                {
                    while (jsonReader.Read())
                        ;
                });
            }
            else
            {
                while (reader.Read())
                    ;
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommas))]
        public static void PartialJsonWithTrailingCommas_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            {
                JsonReaderState state = default;
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);
            }

            {
                var state = new JsonReaderState(options: default);
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);
            }

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                TrailingCommasHelperPartial(utf8, state, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommasAndComments))]
        public static void PartialJsonWithTrailingCommasAndComments_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                TrailingCommasHelperPartial(utf8, state, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommas))]
        public static void PartialJsonWithTrailingCommas_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommasAndComments))]
        public static void PartialJsonWithTrailingCommasAndComments_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                TrailingCommasHelperPartial(utf8, state, expectThrow: true);
            }
        }

        private static void TrailingCommasHelperPartial(byte[] utf8, JsonReaderState state, bool expectThrow)
        {
            if (expectThrow)
            {
                Assert.ThrowsAny<JsonException>(() => PartialReaderLoop(utf8, state));
            }
            else
            {
                PartialReaderLoop(utf8, state);
            }
        }

        private static void PartialReaderLoop(byte[] utf8, JsonReaderState state)
        {
            for (int i = 0; i < utf8.Length; i++)
            {
                JsonReaderState stateCopy = state;
                PartialReaderLoop(utf8, stateCopy, i);
            }
        }

        private static void PartialReaderLoop(byte[] utf8, JsonReaderState state, int splitLocation)
        {
            var reader = new Utf8JsonReader(utf8.AsSpan(0, splitLocation), isFinalBlock: false, state);
            while (reader.Read())
                ;

            long consumed = reader.BytesConsumed;
            reader = new Utf8JsonReader(utf8.AsSpan((int)consumed), isFinalBlock: true, reader.CurrentState);
            while (reader.Read())
                ;
        }

        [Theory]
        [MemberData(nameof(SingleJsonTokenStartIndex))]
        public static void TestTokenStartIndex_SingleValue(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(SingleJsonWithCommentsAllowTokenStartIndex))]
        public static void TestTokenStartIndex_SingleValueCommentsAllow(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow, AllowTrailingCommas = false });
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
            Assert.True(reader.Read());
            Assert.Equal(expectedIndex, reader.TokenStartIndex);

            state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow, AllowTrailingCommas = true });
            reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
            Assert.True(reader.Read());
            Assert.Equal(expectedIndex, reader.TokenStartIndex);
        }

        [Theory]
        [MemberData(nameof(SingleJsonWithCommentsTokenStartIndex))]
        public static void TestTokenStartIndex_SingleValueWithComments(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                if (commentHandling == JsonCommentHandling.Allow)
                {
                    Assert.Equal(JsonTokenType.Comment, reader.TokenType);
                    Assert.True(reader.Read());
                }
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                if (commentHandling == JsonCommentHandling.Allow)
                {
                    Assert.Equal(JsonTokenType.Comment, reader.TokenType);
                    Assert.True(reader.Read());
                }
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(ComplexArrayJsonTokenStartIndex))]
        public static void TestTokenStartIndex_ComplexArrayValue(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(ComplexObjectJsonTokenStartIndex))]
        public static void TestTokenStartIndex_ComplexObjectValue(string jsonString, int expectedIndexProperty, int expectedIndexValue)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(ComplexObjectSeveralJsonTokenStartIndex))]
        public static void TestTokenStartIndex_ComplexObjectManyValues(string jsonString, int expectedIndexProperty, int expectedIndexValue)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommas))]
        public static void TestTokenStartIndex_WithTrailingCommas(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                while (reader.Read())
                { }

                Assert.Equal(utf8.Length - 1, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommasAndComments))]
        public static void TestTokenStartIndex_WithTrailingCommasAndComments(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);
                while (reader.Read())
                { }

                Assert.Equal(utf8.Length - 1, reader.TokenStartIndex);
            }
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

        public static IEnumerable<object[]> SmallTestCases
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { true, TestCaseType.Basic, SR.BasicJson},
                    new object[] { true, TestCaseType.BasicLargeNum, SR.BasicJsonWithLargeNum}, // Json.NET treats numbers starting with 0 as octal (0425 becomes 277)
                    new object[] { true, TestCaseType.FullSchema1, SR.FullJsonSchema1},
                    new object[] { true, TestCaseType.HelloWorld, SR.HelloWorld},
                    new object[] { true, TestCaseType.Json400B, SR.Json400B},

                    new object[] { false, TestCaseType.Basic, SR.BasicJson},
                    new object[] { false, TestCaseType.BasicLargeNum, SR.BasicJsonWithLargeNum}, // Json.NET treats numbers starting with 0 as octal (0425 becomes 277)
                    new object[] { false, TestCaseType.FullSchema1, SR.FullJsonSchema1},
                    new object[] { false, TestCaseType.HelloWorld, SR.HelloWorld},
                    new object[] { false, TestCaseType.Json400B, SR.Json400B},
                };
            }
        }

        public static IEnumerable<object[]> LargeTestCases
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { true, TestCaseType.BroadTree, SR.BroadTree}, // \r\n behavior is different between Json.NET and System.Text.Json
                    new object[] { true, TestCaseType.DeepTree, SR.DeepTree},
                    new object[] { true, TestCaseType.LotsOfNumbers, SR.LotsOfNumbers},
                    new object[] { true, TestCaseType.LotsOfStrings, SR.LotsOfStrings},
                    new object[] { true, TestCaseType.ProjectLockJson, SR.ProjectLockJson},
                    new object[] { true, TestCaseType.Json400B, SR.Json400B},
                    new object[] { true, TestCaseType.Json40KB, SR.Json40KB},
                    new object[] { true, TestCaseType.Json400KB, SR.Json400KB},

                    new object[] { false, TestCaseType.BroadTree, SR.BroadTree}, // \r\n behavior is different between Json.NET and System.Text.Json
                    new object[] { false, TestCaseType.DeepTree, SR.DeepTree},
                    new object[] { false, TestCaseType.LotsOfNumbers, SR.LotsOfNumbers},
                    new object[] { false, TestCaseType.LotsOfStrings, SR.LotsOfStrings},
                    new object[] { false, TestCaseType.ProjectLockJson, SR.ProjectLockJson},
                    new object[] { false, TestCaseType.Json400B, SR.Json400B},
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
                    new object[] {"   \" Test string with \\\"nested quotes \\\" and hex: \\uABCD values! \"   ", true, " Test string with \"nested quotes \" and hex: \uABCD values! "},

                    new object[] {"   12345   ", false, "12345"},
                    new object[] {"   12345.67890e-12   ", false, "1.23456789E-08"},
                    new object[] {"   true  ", false, "True"},
                    new object[] {"   false   ", false, "False"},
                    new object[] {"   null   ", false, "null"},
                    new object[] {"   \" Test string with \\\"nested quotes \\\" and hex: \\uABCD values! \"   ", false, " Test string with \"nested quotes \" and hex: \uABCD values! "},
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

        public static IEnumerable<object[]> JsonTokenWithExtraValue
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"  true  5 "},
                    new object[] {"  false  5 "},
                    new object[] {"  null  5 "},
                    new object[] {"  5  5 "},
                    new object[] {"  5.1234e-4  5 "},
                    new object[] {"  \"hello\"  5 "},
                    new object[] {"  \"hello\"  \"hello\" "},
                    new object[] {"  [  ]  5 "},
                    new object[] {"  [  ]  [] "},
                    new object[] {"  [  ]  {} "},
                    new object[] {"  { }  5 "},
                    new object[] {"  { }  [] "},
                    new object[] {"  { }  {} "},
                    new object[] {"  [  ]5 "},
                    new object[] {"  [  ][] "},
                    new object[] {"  [  ]{} "},
                    new object[] {"  { }5 "},
                    new object[] {"  { }[] "},
                    new object[] {"  { }{} "},
                    new object[] {"  { }  5.1234e-4"},
                    new object[] {"  { }  null "},
                    new object[] {"  { }  false "},
                    new object[] {"  { }  true "},
                    new object[] {"  { }  \"hello\" " },
                    new object[] {"  { },  5 "},
                    new object[] {"  { },  [] "},
                    new object[] {"  { },  {} "},
                    new object[] {"  { },  5.1234e-4"},
                    new object[] {"  { },  null "},
                    new object[] {"  { },  false "},
                    new object[] {"  { },  true "},
                    new object[] {"  { },  \"hello\" " },
                };
            }
        }

        public static IEnumerable<object[]> JsonTokenWithExtraValueAndComments
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"  true  /* comment */ 5 "},
                    new object[] {"  false  /* comment */ 5 "},
                    new object[] {"  null  /* comment */ 5 "},
                    new object[] {"  5  /* comment */ 5 "},
                    new object[] {"  5.1234e-4  /* comment */ 5 "},
                    new object[] {"  \"hello\"  /* comment */ 5 "},
                    new object[] {"  \"hello\"  /* comment */ \"hello\" "},
                    new object[] {"  \"hello\"  // comment \n \"hello\" "},
                    new object[] {"  [  ]  /* comment */ 5 "},
                    new object[] {"  [  ]  /* comment */ [ ]"},
                    new object[] {"  [  ]  /* comment */ { }"},
                    new object[] {"  [  ]  // comment \n 5 "},
                    new object[] {"  { }  /* comment */ 5 "},
                    new object[] {"  { }  /* comment */ [] "},
                    new object[] {"  { }  /* comment */ {} "},
                    new object[] {"  [  ]/* comment */5 "},
                    new object[] {"  [  ]/* comment */[ ]"},
                    new object[] {"  [  ]/* comment */{ }"},
                    new object[] {"  [  ]// comment \n5 "},
                    new object[] {"  { }/* comment */5 "},
                    new object[] {"  { }/* comment */[] "},
                    new object[] {"  { }/* comment */{} "},
                    new object[] {"  { }  /* comment */ 5.1234e-4"},
                    new object[] {"  { }  /* comment */ null "},
                    new object[] {"  { }  /* comment */ false "},
                    new object[] {"  { }  /* comment */ true "},
                    new object[] {"  { }  /* comment */ \"hello\" "},
                    new object[] {"  { }  // comment \n \"hello\" "},
                    new object[] {"  { },  /* comment */ 5 "},
                    new object[] {"  { },  /* comment */ [] "},
                    new object[] {"  { },  /* comment */ {} "},
                    new object[] {"  { },  /* comment */ 5.1234e-4"},
                    new object[] {"  { },  /* comment */ null "},
                    new object[] {"  { },  /* comment */ false "},
                    new object[] {"  { },  /* comment */ true "},
                    new object[] {"  { },  /* comment */ \"hello\" "},
                    new object[] {"  { },  // comment \n \"hello\" "},
                };
            }
        }

        public static IEnumerable<object[]> JsonWithValidTrailingCommas
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"{\"name\": \"value\",}"},
                    new object[] {"{\"name\": [],}"},
                    new object[] {"{\"name\": 1,}"},
                    new object[] {"{\"name\": true,}"},
                    new object[] {"{\"name\": false,}"},
                    new object[] {"{\"name\": null,}"},
                    new object[] {"{\"name\": [{},],}"},
                    new object[] {"{\"first\" : \"value\", \"name\": [{},], \"last\":2 ,}"},
                    new object[] {"{\"prop\":{\"name\": 1,\"last\":2,},}"},
                    new object[] {"{\"prop\":[1,2,],}"},
                    new object[] {"[\"value\",]"},
                    new object[] {"[1,]"},
                    new object[] {"[true,]"},
                    new object[] {"[false,]"},
                    new object[] {"[null,]"},
                    new object[] {"[{},]"},
                    new object[] {"[{\"name\": [],},]"},
                    new object[] {"[1, {\"name\": [],},2 , ]"},
                    new object[] {"[[1,2,],]"},
                    new object[] {"[{\"name\": 1,\"last\":2,},]"},
                };
            }
        }

        public static IEnumerable<object[]> JsonWithValidTrailingCommasAndComments
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"{\"name\": \"value\"/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": []/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": 1/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": true/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": false/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": null/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": [{},]/*comment*/,/*comment*/}"},
                    new object[] {"{\"first\" : \"value\", \"name\": [{},], \"last\":2 /*comment*/,/*comment*/}"},
                    new object[] {"{\"prop\":{\"name\": 1,\"last\":2,}/*comment*/,}"},
                    new object[] {"{\"prop\":[1,2,]/*comment*/,}"},
                    new object[] {"{\"prop\":1,/*comment*/}"},
                    new object[] {"[\"value\"/*comment*/,/*comment*/]"},
                    new object[] {"[1/*comment*/,/*comment*/]"},
                    new object[] {"[true/*comment*/,/*comment*/]"},
                    new object[] {"[false/*comment*/,/*comment*/]"},
                    new object[] {"[null/*comment*/,/*comment*/]"},
                    new object[] {"[{}/*comment*/,/*comment*/]"},
                    new object[] {"[{\"name\": [],}/*comment*/,/*comment*/]"},
                    new object[] {"[1, {\"name\": [],},2 /*comment*/,/*comment*/ ]"},
                    new object[] {"[[1,2,]/*comment*/,]"},
                    new object[] {"[{\"name\": 1,\"last\":2,}/*comment*/,]"},
                    new object[] {"[1,/*comment*/]"},
                };
            }
        }

        public static IEnumerable<object[]> JsonWithInvalidTrailingCommas
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {","},
                    new object[] {"   ,   "},
                    new object[] {"{},"},
                    new object[] {"[],"},
                    new object[] {"1,"},
                    new object[] {"true,"},
                    new object[] {"false,"},
                    new object[] {"null,"},
                    new object[] {"{,}"},
                    new object[] {"{\"name\": 1,,}"},
                    new object[] {"{\"name\": 1,,\"last\":2,}"},
                    new object[] {"[,]"},
                    new object[] {"[1,,]"},
                    new object[] {"[1,,2,]"},
                };
            }
        }

        public static IEnumerable<object[]> JsonWithInvalidTrailingCommasAndComments
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"/*comment*/ ,/*comment*/"},
                    new object[] {"   /*comment*/ ,  /*comment*/ "},
                    new object[] {"{}/*comment*/,/*comment*/"},
                    new object[] {"[]/*comment*/,/*comment*/"},
                    new object[] {"1/*comment*/,/*comment*/"},
                    new object[] {"true/*comment*/,/*comment*/"},
                    new object[] {"false/*comment*/,/*comment*/"},
                    new object[] {"null/*comment*/,/*comment*/"},
                    new object[] {"{/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": 1/*comment*/,/*comment*/,/*comment*/}"},
                    new object[] {"{\"name\": 1,/*comment*/,\"last\":2,}"},
                    new object[] {"[/*comment*/,/*comment*/]"},
                    new object[] {"[1/*comment*/,/*comment*/,/*comment*/]"},
                    new object[] {"[1,/*comment*/,2,]"},
                };
            }
        }

        public static IEnumerable<object[]> SingleJsonTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"[]", 0},
                    new object[] {"{}", 0},
                    new object[] {"12345", 0},
                    new object[] {"1", 0},
                    new object[] {"true", 0},
                    new object[] {"false", 0},
                    new object[] {"null", 0},
                    new object[] {"\"hello\"", 0},
                    new object[] {"\"\"", 0},

                    new object[] {"  []", 2},
                    new object[] {"  {}", 2},
                    new object[] {"  12345", 2},
                    new object[] {"  1", 2},
                    new object[] {"  true", 2},
                    new object[] {"  false", 2},
                    new object[] {"  null", 2},
                    new object[] {"  \"hello\"", 2},
                    new object[] {"  \"\"", 2},

                    new object[] {"  []  ", 2},
                    new object[] {"  {}  ", 2},
                    new object[] {"  12345  ", 2},
                    new object[] {"  1  ", 2},
                    new object[] {"  true  ", 2},
                    new object[] {"  false  ", 2},
                    new object[] {"  null  ", 2},
                    new object[] {"  \"hello\"  ", 2},
                    new object[] {"  \"\"  ", 2},
                };
            }
        }

        public static IEnumerable<object[]> SingleJsonWithCommentsAllowTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"/*comment*/", 0},
                    new object[] {"//comment\n", 0},
                    new object[] {"/*comment*//*comment*/", 0},
                    new object[] {"/*comment*///comment\n", 0},
                    new object[] {"//comment\n/*comment*/", 0},
                    new object[] {"//comment\n//comment\n", 0},

                    new object[] {"  /*comment*/", 2},
                    new object[] {"  //comment\n", 2},
                    new object[] {"  /*comment*//*comment*/", 2},
                    new object[] {"  /*comment*///comment\n", 2},
                    new object[] {"  //comment\n/*comment*/", 2},
                    new object[] {"  //comment\n//comment\n", 2},

                    new object[] {"  /*comment*/  ", 2},
                    new object[] {"  //comment\n  ", 2},
                    new object[] {"  /*comment*//*comment*/  ", 2},
                    new object[] {"  /*comment*///comment\n  ", 2},
                    new object[] {"  //comment\n/*comment*/  ", 2},
                    new object[] {"  //comment\n//comment\n  ", 2},
                };
            }
        }

        public static IEnumerable<object[]> SingleJsonWithCommentsTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"/*comment*/[]", 11},
                    new object[] {"/*comment*/{}", 11},
                    new object[] {"/*comment*/12345", 11},
                    new object[] {"/*comment*/12345  ", 11},
                    new object[] {"/*comment*/12345/*comment*/", 11},
                    new object[] {"/*comment*/12345  /*comment*/", 11},
                    new object[] {"/*comment*/12345  /*comment*/  ", 11},
                    new object[] {"/*comment*/1", 11},
                    new object[] {"/*comment*/true", 11},
                    new object[] {"/*comment*/false", 11},
                    new object[] {"/*comment*/null", 11},
                    new object[] {"/*comment*/\"hello\"", 11},
                    new object[] {"/*comment*/\"\"", 11},

                    new object[] {"  /*comment*/  []", 15},
                    new object[] {"  /*comment*/  {}", 15},
                    new object[] {"  /*comment*/  12345", 15},
                    new object[] { "  /*comment*/  12345  ", 15},
                    new object[] { "  /*comment*/  12345/*comment*/", 15},
                    new object[] { "  /*comment*/  12345  /*comment*/", 15},
                    new object[] { "  /*comment*/  12345  /*comment*/  ", 15},
                    new object[] {"  /*comment*/  1", 15},
                    new object[] {"  /*comment*/  true", 15},
                    new object[] {"  /*comment*/  false", 15},
                    new object[] {"  /*comment*/  null", 15},
                    new object[] {"  /*comment*/  \"hello\"", 15},
                    new object[] {"  /*comment*/  \"\"", 15},
                };
            }
        }

        public static IEnumerable<object[]> ComplexArrayJsonTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"[1,2]", 3},
                    new object[] {"[1,  2]", 5},
                    new object[] {"[1  ,2]", 5},
                    new object[] {"[1  ,  2]", 7},
                    new object[] {"[1  ,  2  ]", 7},

                    new object[] {"[1,\"string\"]", 3},
                    new object[] {"[1,  \"string\"]", 5},
                    new object[] {"[1  ,\"string\"]", 5},
                    new object[] {"[1  ,  \"string\"]", 7},
                    new object[] {"[1  ,  \"string\"  ]", 7},

                    new object[] {"[{}]", 2},
                    new object[] {"[[]]", 2},
                    new object[] {"[123,{}]", 5},
                    new object[] {"[123,[]]", 5},
                    new object[] {"[  {}]", 4},
                    new object[] {"[  []]", 4},
                    new object[] {"[123,  {}]", 7},
                    new object[] {"[123,  []]", 7},
                };
            }
        }

        public static IEnumerable<object[]> ComplexObjectJsonTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"{\"propertyName\":\"value\"}", 1, 16},
                    new object[] {"{  \"propertyName\":\"value\"}", 3, 18},
                    new object[] {"{\"propertyName\"  :\"value\"}", 1, 18},
                    new object[] {"{\"propertyName\":  \"value\"}", 1, 18},
                    new object[] {"{\"propertyName\":\"value\"  }", 1, 16},
                    new object[] {"  {\"propertyName\":\"value\"}", 3, 18},
                    new object[] {"{\"propertyName\":\"value\"}  ", 1, 16},

                    new object[] {"{  \"propertyName\"  :\"value\"}", 3, 20},
                    new object[] {"{  \"propertyName\":  \"value\"}", 3, 20},
                    new object[] {"{  \"propertyName\":\"value\"  }", 3, 18},
                    new object[] {"  {  \"propertyName\":\"value\"}", 5, 20},
                    new object[] {"{  \"propertyName\":\"value\"}   ", 3, 18},

                    new object[] {"{\"propertyName\"  :  \"value\"}", 1, 20},
                    new object[] {"{\"propertyName\":  \"value\"  }", 1, 18},
                    new object[] {"  {\"propertyName\":  \"value\"}", 3, 20},
                    new object[] {"{\"propertyName\":  \"value\"}  ", 1, 18},

                    new object[] {"{\"propertyName\"  :\"value\"  }", 1, 18},
                    new object[] {"  {\"propertyName\"  :\"value\"}", 3, 20},
                    new object[] {"{\"propertyName\"  :\"value\"}  ", 1, 18},

                    new object[] {"  {\"propertyName\":\"value\"  }", 3, 18},
                    new object[] {"{\"propertyName\":\"value\"  }  ", 1, 16},

                    new object[] {"{\"propertyName\":123}", 1, 16},
                    new object[] {"{  \"propertyName\":123}", 3, 18},
                    new object[] {"{\"propertyName\"  :123}", 1, 18},
                    new object[] {"{\"propertyName\":  123}", 1, 18},
                    new object[] {"{\"propertyName\":123  }", 1, 16},
                    new object[] {"  {\"propertyName\":123}", 3, 18},
                    new object[] {"{\"propertyName\":123}   ", 1, 16},

                    new object[] {"{  \"propertyName\"  :123}", 3, 20},
                    new object[] {"{  \"propertyName\":  123}", 3, 20},
                    new object[] {"{  \"propertyName\":123  }", 3, 18},
                    new object[] {"  {  \"propertyName\":123}", 5, 20},
                    new object[] {"{  \"propertyName\":123}  ", 3, 18},

                    new object[] {"{\"propertyName\"  :  123}", 1, 20},
                    new object[] {"{\"propertyName\":  123  }", 1, 18},
                    new object[] {"  {\"propertyName\":  123}", 3, 20},
                    new object[] {"{\"propertyName\":  123}  ", 1, 18},

                    new object[] {"{\"propertyName\"  :123  }", 1, 18},
                    new object[] {"  {\"propertyName\"  :123}", 3, 20},
                    new object[] {"{\"propertyName\"  :123}  ", 1, 18},

                    new object[] {"  {\"propertyName\":123  }", 3, 18},
                    new object[] {"{\"propertyName\":123  }  ", 1, 16},

                    new object[] {"{\"propertyName\":[]}", 1, 16},
                    new object[] {"{  \"propertyName\":[]}", 3, 18},
                    new object[] {"{\"propertyName\"  :[]}", 1, 18},
                    new object[] {"{\"propertyName\":  []}", 1, 18},
                    new object[] {"{\"propertyName\":[]  }", 1, 16},
                    new object[] {"  {\"propertyName\":[]}", 3, 18},
                    new object[] {"{\"propertyName\":[]}  ", 1, 16},

                    new object[] {"{\"propertyName\":{}}", 1, 16},
                    new object[] {"{  \"propertyName\":{}}", 3, 18},
                    new object[] {"{\"propertyName\"  :{}}", 1, 18},
                    new object[] {"{\"propertyName\":  {}}", 1, 18},
                    new object[] {"{\"propertyName\":{}  }", 1, 16},
                    new object[] {"  {\"propertyName\":{}}", 3, 18},
                    new object[] {"{\"propertyName\":{}}  ", 1, 16},
                };
            }
        }

        public static IEnumerable<object[]> ComplexObjectSeveralJsonTokenStartIndex
        {
            get
            {
                return new List<object[]>
                {
                    new object[] {"{\"\":\"\", \"propertyName\":[]}", 8, 23},
                    new object[] {"{\"\":\"\",   \"propertyName\":[]}", 10, 25},
                    new object[] {"{\"\":\"\", \"propertyName\"  :[]}", 8, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":  []}", 8, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":[]  }", 8, 23},
                    new object[] {"  {\"\":\"\", \"propertyName\":[]}", 10, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":[]}  ", 8, 23},

                    new object[] {"{\"\":\"\", \"propertyName\":{}}", 8, 23},
                    new object[] {"{\"\":\"\",   \"propertyName\":{}}", 10, 25},
                    new object[] {"{\"\":\"\", \"propertyName\"  :{}}", 8, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":  {}}", 8, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":{}  }", 8, 23},
                    new object[] {"{  \"\":\"\", \"propertyName\":{}}", 10, 25},
                    new object[] {"{\"\":\"\", \"propertyName\":{}}  ", 8, 23},
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

        public static IEnumerable<object[]> InvalidUTF8Strings
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { new byte[] { 34, 97, 0xc3, 0x28, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xa0, 0xa1, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xe2, 0x28, 0xa1, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xe2, 0x82, 0x28, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x28, 0x8c, 0xbc, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x90, 0x28, 0xbc, 98, 34 } },
                    new object[] { new byte[] { 34, 97, 0xf0, 0x28, 0x8c, 0x28, 98, 34 } },
                };
            }
        }

        public static IEnumerable<object[]> SingleLineCommentData
        {
            get
            {
                return new List<object[]>
                {
                    // \r as the line separator
                    new object [] {"//Comment\r" },
                    new object [] {"//Comment\r" },
                    new object [] {"//Comment\r" },

                    // \r\n as line separator
                    new object [] {"//Comment\r\n" },
                    new object [] {"//Comment\r\n" },
                    new object [] {"//Comment\r\n" },
                    new object [] {"//Comment\r\n" },

                    // \n as line separator
                    new object [] {"//Comment\n" },
                    new object [] {"//Comment\n" },
                    new object [] {"//Comment\n" }
                };
            }
        }
    }
}
