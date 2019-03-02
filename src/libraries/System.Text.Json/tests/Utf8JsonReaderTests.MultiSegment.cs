// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static void TestJsonReaderUtf8SegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
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

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            // Skipping really large JSON since slicing them (O(n^2)) is too slow.
            if (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence, isFinalBlock: true, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);
                Assert.Equal(expectedStr, actualStrSequence);
                return;
            }

            for (int j = 0; j < dataUtf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

                long consumed = utf8JsonReader.BytesConsumed;
                Assert.Equal(consumed, utf8JsonReader.CurrentState.BytesConsumed);
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out length, ref utf8JsonReader);
                actualStrSequence += Encoding.UTF8.GetString(resultSequence, 0, length);
                string message = $"Expected consumed: {dataUtf8.Length - consumed}, Actual consumed: {utf8JsonReader.BytesConsumed}, Index: {j}";
                Assert.Equal(utf8JsonReader.BytesConsumed, utf8JsonReader.CurrentState.BytesConsumed);
                Assert.True(dataUtf8.Length - consumed == utf8JsonReader.BytesConsumed, message);
                Assert.Equal(expectedStr, actualStrSequence);
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public static void TestPartialJsonReaderMultiSegment(bool compactData, TestCaseType type, string jsonString)
        {
            // Skipping really large JSON since slicing them (O(n^2)) is too slow.
            if (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson
                || type == TestCaseType.DeepTree || type == TestCaseType.BroadTree || type == TestCaseType.LotsOfNumbers
                || type == TestCaseType.LotsOfStrings || type == TestCaseType.Json4KB)
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
            ReadOnlyMemory<byte> dataMemory = dataUtf8;

            var sequences = new List<ReadOnlySequence<byte>>
            {
                new ReadOnlySequence<byte>(dataMemory)
            };

            for (int i = 0; i < dataUtf8.Length; i++)
            {
                var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, i));
                ReadOnlyMemory<byte> secondMem = dataMemory.Slice(i);
                BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
                var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
                sequences.Add(sequence);
            }

            for (int i = 0; i < sequences.Count; i++)
            {
                var json = new Utf8JsonReader(sequences[i], isFinalBlock: true, default);
                while (json.Read())
                    ;
                Assert.Equal(sequences[i].Length, json.BytesConsumed);
                Assert.Equal(sequences[i].Length, json.CurrentState.BytesConsumed);

                Assert.True(sequences[i].Slice(json.Position).IsEmpty);
                Assert.True(sequences[i].Slice(json.CurrentState.Position).IsEmpty);
            }

            for (int i = 0; i < sequences.Count; i++)
            {
                ReadOnlySequence<byte> sequence = sequences[i];
                for (int j = 0; j < dataUtf8.Length; j++)
                {
                    var json = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                    while (json.Read())
                        ;

                    long consumed = json.BytesConsumed;
                    JsonReaderState jsonState = json.CurrentState;
                    byte[] consumedArray = sequence.Slice(0, consumed).ToArray();
                    Assert.Equal(consumedArray, sequence.Slice(0, json.Position).ToArray());
                    Assert.Equal(consumedArray, sequence.Slice(0, jsonState.Position).ToArray());
                    json = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, json.CurrentState);
                    while (json.Read())
                        ;
                    Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                    Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidJsonStrings))]
        public static void InvalidJsonMultiSegmentWithEmptyFirst(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth = 64)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

                ReadOnlyMemory<byte> dataMemory = dataUtf8;
                var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, 0));
                ReadOnlyMemory<byte> secondMem = dataMemory;
                BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
                var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);

                SpanSequenceStatesAreEqualInvalidJson(dataUtf8, sequence, maxDepth, commentHandling);

                var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException for multi-segment data was not thrown.");
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
        public static void InvalidJsonMultiSegmentByOne(string jsonString, int expectedlineNumber, int expectedBytePosition, int maxDepth = 64)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

                SpanSequenceStatesAreEqualInvalidJson(dataUtf8, sequence, maxDepth, commentHandling);

                var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

                try
                {
                    while (json.Read())
                        ;
                    Assert.True(false, "Expected JsonReaderException for multi-segment data was not thrown.");
                }
                catch (JsonReaderException ex)
                {
                    Assert.Equal(expectedlineNumber, ex.LineNumber);
                    Assert.Equal(expectedBytePosition, ex.BytePositionInLine);
                }
            }
        }

        [Fact]
        public static void EmptyJsonWithinSequenceIsInvalid()
        {
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(new byte[0], 1);
            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);

            try
            {
                while (json.Read())
                    ;
                Assert.True(false, "Expected JsonReaderException was not thrown with single-segment data.");
            }
            catch (JsonReaderException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(0, ex.BytePositionInLine);
            }
        }

        private static void SpanSequenceStatesAreEqualInvalidJson(byte[] dataUtf8, ReadOnlySequence<byte> sequence, int maxDepth, JsonCommentHandling commentHandling)
        {
            var stateSpan = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
            var jsonSpan = new Utf8JsonReader(dataUtf8, isFinalBlock: true, stateSpan);

            var stateSequence = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling, MaxDepth = maxDepth });
            var jsonSequence = new Utf8JsonReader(sequence, isFinalBlock: true, stateSequence);

            try
            {
                while (true)
                {
                    bool spanResult = jsonSpan.Read();
                    bool sequenceResult = jsonSequence.Read();

                    Assert.Equal(spanResult, sequenceResult);
                    Assert.Equal(jsonSpan.CurrentDepth, jsonSequence.CurrentDepth);
                    Assert.Equal(jsonSpan.BytesConsumed, jsonSequence.BytesConsumed);
                    Assert.Equal(jsonSpan.TokenType, jsonSequence.TokenType);

                    if (!spanResult)
                    {
                        break;
                    }
                }
                Assert.True(false, "Expected JsonReaderException due to invalid JSON.");
            }
            catch (JsonReaderException)
            { }
        }

        private static void SpanSequenceStatesAreEqual(byte[] dataUtf8)
        {
            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(dataUtf8);

            var jsonSpan = new Utf8JsonReader(dataUtf8, isFinalBlock: true, default);
            var jsonSequence = new Utf8JsonReader(sequence, isFinalBlock: true, default);

            while (true)
            {
                bool spanResult = jsonSpan.Read();
                bool sequenceResult = jsonSequence.Read();

                Assert.Equal(spanResult, sequenceResult);
                Assert.Equal(jsonSpan.CurrentDepth, jsonSequence.CurrentDepth);
                Assert.Equal(jsonSpan.BytesConsumed, jsonSequence.BytesConsumed);
                Assert.Equal(jsonSpan.TokenType, jsonSequence.TokenType);

                if (!spanResult)
                {
                    break;
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
        public static void AllowCommentStackMismatchMultiSegment(string jsonString, string expectedWithoutComments, string expectedWithComments)
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            var sequence = new ReadOnlySequence<byte>(data);
            TestReadingJsonWithComments(data, sequence, expectedWithoutComments, expectedWithComments);

            sequence = JsonTestHelper.GetSequence(data, 1);
            TestReadingJsonWithComments(data, sequence, expectedWithoutComments, expectedWithComments);

            var firstSegment = new BufferSegment<byte>(ReadOnlyMemory<byte>.Empty);
            ReadOnlyMemory<byte> secondMem = data;
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
            TestReadingJsonWithComments(data, sequence, expectedWithoutComments, expectedWithComments);
        }

        private static void TestReadingJsonWithComments(byte[] inputData, ReadOnlySequence<byte> sequence, string expectedWithoutComments, string expectedWithComments)
        {
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

            var builder = new StringBuilder();
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                {
                    builder.Append(Encoding.UTF8.GetString(json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan.ToArray()));
                    if (json.HasValueSequence)
                    {
                        Assert.True(json.ValueSpan == default);
                    }
                    else
                    {
                        Assert.True(json.ValueSequence.IsEmpty);
                    }
                }
            }

            Assert.Equal(expectedWithComments, builder.ToString());
            Assert.Equal(inputData, sequence.Slice(0, json.Position).ToArray());
            Assert.Equal(inputData, sequence.Slice(0, json.CurrentState.Position).ToArray());

            state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
            json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

            builder = new StringBuilder();
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number || json.TokenType == JsonTokenType.Comment || json.TokenType == JsonTokenType.PropertyName)
                {
                    builder.Append(Encoding.UTF8.GetString(json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan.ToArray()));
                    if (json.HasValueSequence)
                    {
                        Assert.True(json.ValueSpan == default);
                    }
                    else
                    {
                        Assert.True(json.ValueSequence.IsEmpty);
                    }
                }
            }

            Assert.Equal(expectedWithoutComments, builder.ToString());
            Assert.Equal(inputData, sequence.Slice(0, json.Position).ToArray());
            Assert.Equal(inputData, sequence.Slice(0, json.CurrentState.Position).ToArray());
        }

        [Theory]
        [MemberData(nameof(SingleValueJson))]
        public static void SingleJsonValueMultiSegment(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var sequence = new ReadOnlySequence<byte>(dataUtf8);
            TestReadingSingleValueJson(dataUtf8, sequence, expectedString);

            sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
            TestReadingSingleValueJson(dataUtf8, sequence, expectedString);

            var firstSegment = new BufferSegment<byte>(ReadOnlyMemory<byte>.Empty);
            ReadOnlyMemory<byte> secondMem = dataUtf8;
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
            TestReadingSingleValueJson(dataUtf8, sequence, expectedString);
        }

        private static void TestReadingSingleValueJson(byte[] inputData, ReadOnlySequence<byte> sequence, string expectedString)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(sequence, false, state);

                while (json.Read())
                {
                    // Check if the TokenType is a primitive "value", i.e. String, Number, True, False, and Null
                    Assert.True(json.TokenType >= JsonTokenType.String && json.TokenType <= JsonTokenType.Null);
                    Assert.Equal(expectedString, Encoding.UTF8.GetString(json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan.ToArray()));

                    if (json.HasValueSequence)
                    {
                        Assert.True(json.ValueSpan == default);
                    }
                    else
                    {
                        Assert.True(json.ValueSequence.IsEmpty);
                    }
                }

                Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
                Assert.Equal(inputData, sequence.Slice(0, json.Position).ToArray());
                Assert.Equal(inputData, sequence.Slice(0, json.CurrentState.Position).ToArray());
            }
        }

        [Theory]
        [InlineData(0, "{\"property name\": [\"value 1\", \"value 2 across sequence\", 12345, 1234567890, true, false, null]}")]
        [InlineData(1, "[{\"property name\": [\"value 1\", \"value 2 across sequence\", 12345, 1234567890, true, false, null]}]")]
        [InlineData(2, "{\"property name\": [// comment value\r\n\"value 2 across sequence\"// another comment value\r\n]}")]
        public static void CheckOnlyOneOfValueSpanOrSequenceIsSet(int testCase, string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            bool[] expectedHasValueSequence = null;
            bool[] expectedHasValueSequenceSkip = null;
            bool[] expectedHasValueSequenceAllow = null;

            ReadOnlySequence<byte> sequence;
            switch (testCase)
            {
                case 0:
                    Debug.Assert(dataUtf8.Length == 95);
                    byte[][] buffers = new byte[6][];
                    buffers[0] = dataUtf8.AsSpan(0, 10).ToArray();
                    buffers[1] = dataUtf8.AsSpan(10, 28).ToArray();
                    buffers[2] = dataUtf8.AsSpan(38, 32).ToArray();
                    buffers[3] = dataUtf8.AsSpan(70, 23).ToArray();
                    buffers[4] = dataUtf8.AsSpan(93, 1).ToArray();
                    buffers[5] = dataUtf8.AsSpan(94, 1).ToArray();
                    sequence = BufferFactory.Create(buffers);
                    expectedHasValueSequence = new bool [] {false, true, false, false, true, false, true, false, false, false, false, false };
                    break;
                case 1:
                    Debug.Assert(dataUtf8.Length == 97);
                    buffers = new byte[7][];
                    buffers[0] = dataUtf8.AsSpan(0, 39).ToArray();
                    buffers[1] = dataUtf8.AsSpan(39, 22).ToArray();
                    buffers[2] = dataUtf8.AsSpan(61, 18).ToArray();
                    buffers[3] = dataUtf8.AsSpan(79, 7).ToArray();
                    buffers[4] = dataUtf8.AsSpan(86, 7).ToArray();
                    buffers[5] = dataUtf8.AsSpan(93, 3).ToArray();
                    buffers[6] = dataUtf8.AsSpan(96, 1).ToArray();
                    sequence = BufferFactory.Create(buffers);
                    expectedHasValueSequence = new bool[] { false, false, false, false, false, true, true, false, true, true, true, false, false, false };
                    break;
                case 2:
                    Debug.Assert(dataUtf8.Length == 90);
                    buffers = new byte[5][];
                    buffers[0] = dataUtf8.AsSpan(0, 36).ToArray();
                    buffers[1] = dataUtf8.AsSpan(36, 13).ToArray();
                    buffers[2] = dataUtf8.AsSpan(49, 30).ToArray();
                    buffers[3] = dataUtf8.AsSpan(79, 9).ToArray();
                    buffers[4] = dataUtf8.AsSpan(88, 2).ToArray();
                    sequence = BufferFactory.Create(buffers);
                    expectedHasValueSequenceSkip = new bool[] { false, false, false, true,  false, false };
                    expectedHasValueSequenceAllow = new bool[] { false, false, false, true, true, true, false, false };
                    break;
                default:
                    return;
            }

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow && testCase == 2)
                {
                    continue;
                }
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

                int index = 0;

                while (json.Read())
                {
                    if (testCase == 0 || testCase == 1)
                    {
                        Assert.True(expectedHasValueSequence[index] == json.HasValueSequence, $"{commentHandling}, {testCase}, {index}, {json.HasValueSequence}");
                    }
                    else
                    {
                        if (commentHandling == JsonCommentHandling.Skip)
                        {
                            Assert.True(expectedHasValueSequenceSkip[index] == json.HasValueSequence, $"{commentHandling}, {testCase}, {index}, {json.HasValueSequence}");
                        }
                        else
                        {
                            Assert.True(expectedHasValueSequenceAllow[index] == json.HasValueSequence, $"{commentHandling}, {testCase}, {index}, {json.HasValueSequence}");
                        }
                    }
                    if (json.HasValueSequence)
                    {
                        Assert.True(json.ValueSpan == default, $"Escaped ValueSpan to be empty when HasValueSequence is true. Test case: {testCase}");
                        Assert.False(json.ValueSequence.IsEmpty, $"Escaped ValueSequence to not be empty when HasValueSequence is true. Test case: {testCase}");
                    }
                    else
                    {
                        Assert.True(json.ValueSequence.IsEmpty, $"Escaped ValueSequence to be empty when HasValueSequence is false. Test case: {testCase}");
                        Assert.False(json.ValueSpan == default, $"Escaped ValueSpan to not be empty when HasValueSequence is false. Test case: {testCase}");
                    }

                    index++;
                }
            }
        }

        [Theory]
        [InlineData("\"abcdefg\"")]
        [InlineData("12345")]
        [InlineData("12345.0e-3")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("null")]
        public static void CheckOnlyOneOfValueSpanOrSequenceIsSetSingleValue(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

                Assert.False(json.HasValueSequence);
                Assert.True(json.ValueSpan == default);
                Assert.True(json.ValueSequence.IsEmpty);

                Assert.True(json.Read());
                Assert.True(json.HasValueSequence);
                Assert.True(json.ValueSpan == default);
                Assert.False(json.ValueSequence.IsEmpty);

                // Subsequent calls to Read clears the value properties since Read returned false.
                Assert.False(json.Read());
                Assert.False(json.HasValueSequence);
                Assert.True(json.ValueSpan == default);
                Assert.True(json.ValueSequence.IsEmpty);
            }
        }
    }
}
