// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
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
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));
                Assert.Equal(expectedStr, actualStrSequence);
                return;
            }

            for (int j = 0; j < dataUtf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));

                long consumed = utf8JsonReader.BytesConsumed;
                Assert.Equal(consumed, utf8JsonReader.CurrentState.BytesConsumed);
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out length, ref utf8JsonReader);
                actualStrSequence += Encoding.UTF8.GetString(resultSequence.AsSpan(0, length));
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

            var sequences = new List<ReadOnlySequence<byte>>();

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

                var state = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
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

                var state = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
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

        private static void SpanSequenceStatesAreEqualInvalidJson(byte[] dataUtf8, ReadOnlySequence<byte> sequence, int maxDepth, JsonCommentHandling commentHandling)
        {
            var stateSpan = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
            var jsonSpan = new Utf8JsonReader(dataUtf8, isFinalBlock: true, stateSpan);

            var stateSequence = new JsonReaderState(maxDepth: maxDepth, options: new JsonReaderOptions { CommentHandling = commentHandling });
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
    }
}
