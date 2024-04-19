// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Fact]
        public static void InitialStateMultiSegment()
        {
            byte[] utf8 = "1"u8.ToArray();
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.NotEqual(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.False(json.Read());
        }

        [Fact]
        public static void EmptyJsonMultiSegmentIsInvalid()
        {
            ReadOnlyMemory<byte> dataMemory = Array.Empty<byte>();

            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, 0));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(0, 0);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);

            var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);

            Assert.ThrowsAny<JsonException>(() =>
            {
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);

                Assert.Equal(0, json.BytesConsumed);
                Assert.Equal(0, json.TokenStartIndex);
                Assert.Equal(0, json.CurrentDepth);
                Assert.Equal(JsonTokenType.None, json.TokenType);
                Assert.NotEqual(default, json.Position);
                Assert.False(json.HasValueSequence);
                Assert.False(json.ValueIsEscaped);
                Assert.True(json.ValueSpan.SequenceEqual(default));
                Assert.True(json.ValueSequence.IsEmpty);

                Assert.Equal(64, json.CurrentState.Options.MaxDepth);
                Assert.False(json.CurrentState.Options.AllowTrailingCommas);
                Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

                json.Read(); // this should throw
            });
        }

        [Theory]
        [InlineData("2e2", 200, 3)]
        [InlineData("2e+2", 200, 4)]
        [InlineData("123e-01", 12.3, 7)]
        [InlineData("0", 0, 1)]
        [InlineData("0.1", 0.1, 3)]
        [InlineData("-0", 0, 2)]
        [InlineData("-0.1", -0.1, 4)]
        [InlineData("   2e2   ", 200, 3)]
        [InlineData("   2e+2   ", 200, 4)]
        [InlineData("   123e-01   ", 12.3, 7)]
        [InlineData("   0   ", 0, 1)]
        [InlineData("   0.1   ", 0.1, 3)]
        [InlineData("   -0   ", 0, 2)]
        [InlineData("   -0.1   ", -0.1, 4)]
        [InlineData("[2e2]", 200, 3)]
        [InlineData("[2e+2]", 200, 4)]
        [InlineData("[123e-01]", 12.3, 7)]
        [InlineData("[0]", 0, 1)]
        [InlineData("[0.1]", 0.1, 3)]
        [InlineData("[-0]", 0, 2)]
        [InlineData("[-0.1]", -0.1, 4)]
        [InlineData("{\"foo\": 2e2}", 200, 3)]
        [InlineData("{\"foo\": 2e+2}", 200, 4)]
        [InlineData("{\"foo\": 123e-01}", 12.3, 7)]
        [InlineData("{\"foo\": 0}", 0, 1)]
        [InlineData("{\"foo\": 0.1}", 0.1, 3)]
        [InlineData("{\"foo\": -0}", 0, 2)]
        [InlineData("{\"foo\": -0.1}", -0.1, 4)]
        public static void ReadJsonWithNumberVariousSegmentSizes(string input, double expectedValue, long expectedTokenLength)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(input);

            var jsonReader = new Utf8JsonReader(utf8);
            ReadDoubleHelper(ref jsonReader, utf8.Length, expectedValue, expectedTokenLength);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            jsonReader = new Utf8JsonReader(sequence);
            ReadDoubleHelper(ref jsonReader, utf8.Length, expectedValue, expectedTokenLength);

            for (int splitLocation = 0; splitLocation < utf8.Length; splitLocation++)
            {
                sequence = JsonTestHelper.CreateSegments(utf8, splitLocation);
                jsonReader = new Utf8JsonReader(sequence);
                ReadDoubleHelper(ref jsonReader, utf8.Length, expectedValue, expectedTokenLength);
            }

            for (int firstSplit = 0; firstSplit < utf8.Length; firstSplit++)
            {
                for (int secondSplit = firstSplit; secondSplit < utf8.Length; secondSplit++)
                {
                    sequence = JsonTestHelper.CreateSegments(utf8, firstSplit, secondSplit);
                    jsonReader = new Utf8JsonReader(sequence);
                    ReadDoubleHelper(ref jsonReader, utf8.Length, expectedValue, expectedTokenLength);
                }
            }
        }

        private static void ReadDoubleHelper(ref Utf8JsonReader jsonReader, int expectedBytesConsumed, double expectedValue, long expectedTokenLength)
        {
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonTokenType.Number)
                {
                    long tokenLength = jsonReader.HasValueSequence ? jsonReader.ValueSequence.Length : jsonReader.ValueSpan.Length;
                    Assert.Equal(expectedTokenLength, tokenLength);
                    Assert.Equal(tokenLength, jsonReader.BytesConsumed - jsonReader.TokenStartIndex);
                    Assert.Equal(expectedValue, jsonReader.GetDouble());
                }
            }
            Assert.Equal(expectedBytesConsumed, jsonReader.BytesConsumed);
        }

        [Theory]
        [InlineData("0{", 1, false, 0, '{')]
        [InlineData("0e+1b", 4, false, 0, 'b')]
        [InlineData("1e+0}", 4, true, 4, '}')]
        [InlineData("12345.1. ", 7, false, 0, '.')]
        [InlineData("- ", 1, false, 0, ' ')]
        [InlineData("-f ", 1, false, 0, 'f')]
        [InlineData("-} ", 1, false, 0, '}')]
        [InlineData("1.f ", 2, false, 0, 'f')]
        [InlineData("1.} ", 2, false, 0, '}')]
        [InlineData("0. ", 2, false, 0, ' ')]
        [InlineData("0.1f ", 3, false, 0, 'f')]
        [InlineData("0.1} ", 3, true, 3, '}')]
        [InlineData("0.1e1f ", 5, false, 0, 'f')]
        [InlineData("+0 ", 0, false, 0, '+')]
        [InlineData("+1 ", 0, false, 0, '+')]
        [InlineData("0e ", 2, false, 0, ' ')]
        [InlineData("0.1e ", 4, false, 0, ' ')]
        [InlineData("01 ", 1, false, 0, '1')]
        [InlineData("1a ", 1, false, 0, 'a')]
        [InlineData("-01 ", 2, false, 0, '1')]
        [InlineData("10.5e ", 5, false, 0, ' ')]
        [InlineData("10.5e- ", 6, false, 0, ' ')]
        [InlineData("10.5e+ ", 6, false, 0, ' ')]
        [InlineData("10.5e-0.2 ", 7, false, 0, '.')]
        public static void InvalidJsonNumberVariousSegmentSizes(string input, int expectedBytePositionInLine, bool firstReadSuccessful, int expectedConsumed, char invalidChar)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(input);

            var jsonReader = new Utf8JsonReader(utf8);
            InvalidReadNumberHelper(ref jsonReader, expectedBytePositionInLine, firstReadSuccessful, expectedConsumed, invalidChar);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            jsonReader = new Utf8JsonReader(sequence);
            InvalidReadNumberHelper(ref jsonReader, expectedBytePositionInLine, firstReadSuccessful, expectedConsumed, invalidChar);

            for (int splitLocation = 0; splitLocation < utf8.Length; splitLocation++)
            {
                sequence = JsonTestHelper.CreateSegments(utf8, splitLocation);
                jsonReader = new Utf8JsonReader(sequence);
                InvalidReadNumberHelper(ref jsonReader, expectedBytePositionInLine, firstReadSuccessful, expectedConsumed, invalidChar);
            }

            for (int firstSplit = 0; firstSplit < utf8.Length; firstSplit++)
            {
                for (int secondSplit = firstSplit; secondSplit < utf8.Length; secondSplit++)
                {
                    sequence = JsonTestHelper.CreateSegments(utf8, firstSplit, secondSplit);
                    jsonReader = new Utf8JsonReader(sequence);
                    InvalidReadNumberHelper(ref jsonReader, expectedBytePositionInLine, firstReadSuccessful, expectedConsumed, invalidChar);
                }
            }
        }

        private static void InvalidReadNumberHelper(ref Utf8JsonReader jsonReader, int expectedBytePositionInLine, bool firstReadSuccessful, int expectedConsumed, char invalidChar)
        {
            if (firstReadSuccessful)
            {
                Assert.True(jsonReader.Read());

                long tokenLength = jsonReader.HasValueSequence ? jsonReader.ValueSequence.Length : jsonReader.ValueSpan.Length;
                Assert.Equal(expectedConsumed, tokenLength);
                Assert.Equal(0, jsonReader.TokenStartIndex);
                Assert.Equal(expectedConsumed, jsonReader.BytesConsumed - jsonReader.TokenStartIndex);
            }

            try
            {
                jsonReader.Read();
                Assert.Fail("Expected JsonException was not thrown for incomplete/invalid JSON payload reading.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(expectedBytePositionInLine, ex.BytePositionInLine);
                Assert.Equal(expectedConsumed, jsonReader.BytesConsumed);
                Assert.Contains($"'{invalidChar}'", ex.Message);
            }
        }

        [Theory]
        [InlineData("\"\\\"\"}", 4, 4, "\"", 2)]
        [InlineData("\"\\\"\"5", 4, 4, "\"", 2)]
        [InlineData("\"\\\"\":", 4, 4, "\"", 2)]
        [InlineData("\"\\\"\",", 4, 4, "\"", 2)]
        [InlineData("\"abc\\t\"}", 7, 7, "abc\t", 5)]
        [InlineData("\"abc\\u0061\"}", 11, 11, "abca", 9)]
        [InlineData("\"abc\\u0061abc\"}", 14, 14, "abcaabc", 12)]
        [InlineData("\"abc\\t\\\"\"}", 9, 9, "abc\t\"", 7)]
        [InlineData("\"abc\\n\"}", 7, 7, "abc\n", 5)]
        [InlineData("\"abc\\na\"}", 8, 8, "abc\na", 6)]
        [InlineData("\"abc\\u0061X\"}", 12, 12, "abcaX", 10)]
        [InlineData("\"\\u0061\"}", 8, 8, "a", 6)]
        public static void ValidStringWithinInvalidJsonVariousSegmentSizes(string input, int expectedBytePositionInLine, int expectedConsumed, string expectedStr, int expectedTokenLength)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(input);

            var jsonReader = new Utf8JsonReader(utf8);
            ValidReadStringHelper(ref jsonReader, expectedBytePositionInLine, expectedConsumed, expectedStr, expectedTokenLength);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            jsonReader = new Utf8JsonReader(sequence);
            ValidReadStringHelper(ref jsonReader, expectedBytePositionInLine, expectedConsumed, expectedStr, expectedTokenLength);

            for (int splitLocation = 0; splitLocation < utf8.Length; splitLocation++)
            {
                sequence = JsonTestHelper.CreateSegments(utf8, splitLocation);
                jsonReader = new Utf8JsonReader(sequence);
                ValidReadStringHelper(ref jsonReader, expectedBytePositionInLine, expectedConsumed, expectedStr, expectedTokenLength);
            }

            for (int firstSplit = 0; firstSplit < utf8.Length; firstSplit++)
            {
                for (int secondSplit = firstSplit; secondSplit < utf8.Length; secondSplit++)
                {
                    sequence = JsonTestHelper.CreateSegments(utf8, firstSplit, secondSplit);
                    jsonReader = new Utf8JsonReader(sequence);
                    ValidReadStringHelper(ref jsonReader, expectedBytePositionInLine, expectedConsumed, expectedStr, expectedTokenLength);
                }
            }
        }

        private static void ValidReadStringHelper(ref Utf8JsonReader jsonReader, int expectedBytePositionInLine, int expectedConsumed, string expectedStr, int expectedTokenLength)
        {
            Assert.True(jsonReader.Read());

            Assert.Equal(JsonTokenType.String, jsonReader.TokenType);
            long tokenLength = jsonReader.HasValueSequence ? jsonReader.ValueSequence.Length : jsonReader.ValueSpan.Length;
            Assert.Equal(expectedTokenLength, tokenLength);
            Assert.Equal(expectedTokenLength + 2, jsonReader.BytesConsumed - jsonReader.TokenStartIndex);
            Assert.Equal(expectedStr, jsonReader.GetString());

            try
            {
                jsonReader.Read();
                Assert.Fail("Expected JsonException was not thrown for incomplete/invalid JSON payload reading.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(expectedBytePositionInLine, ex.BytePositionInLine);
                Assert.Equal(expectedConsumed, jsonReader.BytesConsumed);
            }
        }

        [Theory]
        [InlineData("\"abc\\t", 6)]
        [InlineData("\"abc\\t\\\"", 8)]
        [InlineData("\"abc\\n", 6)]
        [InlineData("\"abc\\nabc", 9)]
        [InlineData("\"abc\\n\\\"", 8)]
        [InlineData("\"hi\\n\\n\\n\\q\"", 10)]
        [InlineData("\"abc\u0010\"", 4)]
        [InlineData("\"abc\u0010abc\"", 4)]
        [InlineData("\"abc\\p\"", 5)]
        [InlineData("\"abc\\p", 5)]
        [InlineData("\"abc\\u\"", 6)]
        [InlineData("\"abc\\uX\"", 6)]
        [InlineData("\"abc\\u", 6)]
        [InlineData("\"abc\\u1\"", 7)]
        [InlineData("\"abc\\u1X\"", 7)]
        [InlineData("\"abc\\u1", 7)]
        [InlineData("\"abc\\u12\"", 8)]
        [InlineData("\"abc\\u12X\"", 8)]
        [InlineData("\"abc\\u12", 8)]
        [InlineData("\"abc\\u123\"", 9)]
        [InlineData("\"abc\\u123X\"", 9)]
        [InlineData("\"abc\\u123", 9)]
        [InlineData("\"abc\\u1234\\\"", 12)]
        [InlineData("\"abc\\u1234X\\\"", 13)]
        [InlineData("\"abc\\u1234", 10)]
        [InlineData("\"abcdefg", 8)]
        [InlineData("\"\\u1234", 7)]
        [InlineData("{\"name\": \"abc\\t", 6 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\t\\\"", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\n", 6 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\nabc", 9 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\n\\\"", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"hi\\n\\n\\n\\q\"", 10 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\u0010\"", 4 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\u0010abc\"", 4 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\p\"", 5 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\p", 5 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u\"", 6 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\uX\"", 6 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u", 6 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1\"", 7 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1X\"", 7 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1", 7 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u12\"", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u12X\"", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u12", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u123\"", 9 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u123X\"", 9 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u123", 9 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1234\\\"", 12 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1234X\\\"", 13 + 9, 2, 9)]
        [InlineData("{\"name\": \"abc\\u1234", 10 + 9, 2, 9)]
        [InlineData("{\"name\": \"abcdefg", 8 + 9, 2, 9)]
        [InlineData("{\"name\": \"\\u1234", 7 + 9, 2, 9)]
        public static void InvalidJsonStringVariousSegmentSizes(string input, int expectedBytePositionInLine, int numberOfSuccessfulReads = 0, int expectedConsumed = 0)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(input);

            var jsonReader = new Utf8JsonReader(utf8);
            InvalidReadStringHelper(ref jsonReader, expectedBytePositionInLine, numberOfSuccessfulReads, expectedConsumed);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            jsonReader = new Utf8JsonReader(sequence);
            InvalidReadStringHelper(ref jsonReader, expectedBytePositionInLine, numberOfSuccessfulReads, expectedConsumed);

            for (int splitLocation = 0; splitLocation < utf8.Length; splitLocation++)
            {
                sequence = JsonTestHelper.CreateSegments(utf8, splitLocation);
                jsonReader = new Utf8JsonReader(sequence);
                InvalidReadStringHelper(ref jsonReader, expectedBytePositionInLine, numberOfSuccessfulReads, expectedConsumed);
            }

            for (int firstSplit = 0; firstSplit < utf8.Length; firstSplit++)
            {
                for (int secondSplit = firstSplit; secondSplit < utf8.Length; secondSplit++)
                {
                    sequence = JsonTestHelper.CreateSegments(utf8, firstSplit, secondSplit);
                    jsonReader = new Utf8JsonReader(sequence);
                    InvalidReadStringHelper(ref jsonReader, expectedBytePositionInLine, numberOfSuccessfulReads, expectedConsumed);
                }
            }
        }

        private static void InvalidReadStringHelper(ref Utf8JsonReader jsonReader, int expectedBytePositionInLine, int numberOfSuccessfulReads, int expectedConsumed)
        {
            for (int i = 0; i < numberOfSuccessfulReads; i++)
            {
                Assert.True(jsonReader.Read());
            }

            try
            {
                jsonReader.Read();
                Assert.Fail("Expected JsonException was not thrown for incomplete/invalid JSON payload reading.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(expectedBytePositionInLine, ex.BytePositionInLine);
                Assert.Equal(expectedConsumed, jsonReader.BytesConsumed);
            }
        }

        [Fact]
        public static void InitialStateSimpleCtorMultiSegment()
        {
            byte[] utf8 = "1"u8.ToArray();
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            var json = new Utf8JsonReader(sequence);

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.NotEqual(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.False(json.Read());
        }

        [Fact]
        public static void StateRecoveryMultiSegment()
        {
            byte[] utf8 = "[1]"u8.ToArray();
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);
            var json = new Utf8JsonReader(sequence, isFinalBlock: false, state: default);

            Assert.Equal(0, json.BytesConsumed);
            Assert.Equal(0, json.TokenStartIndex);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.NotEqual(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.True(json.Read());

            Assert.Equal(2, json.BytesConsumed);
            Assert.Equal(1, json.TokenStartIndex);
            Assert.Equal(1, json.CurrentDepth);
            Assert.Equal(JsonTokenType.Number, json.TokenType);
            Assert.NotEqual(default, json.Position);
            Assert.True(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.ToArray().AsSpan().SequenceEqual(new byte[] { (byte)'1' }));

            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            JsonReaderState state = json.CurrentState;

            json = new Utf8JsonReader(sequence.Slice(json.Position), isFinalBlock: true, state);

            Assert.Equal(0, json.BytesConsumed);    // Not retained
            Assert.Equal(0, json.TokenStartIndex);  // Not retained
            Assert.Equal(1, json.CurrentDepth);
            Assert.Equal(JsonTokenType.Number, json.TokenType);
            Assert.NotEqual(default, json.Position);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.Equal(64, json.CurrentState.Options.MaxDepth);
            Assert.False(json.CurrentState.Options.AllowTrailingCommas);
            Assert.Equal(JsonCommentHandling.Disallow, json.CurrentState.Options.CommentHandling);

            Assert.True(json.Read());
            Assert.False(json.Read());
        }

        // TestCaseType is only used to give the json strings a descriptive name.
        [Theory]
        // Skipping large JSON since slicing them (O(n^2)) is too slow.
        [MemberData(nameof(SmallTestCases))]
        public static void TestJsonReaderUtf8SegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
        {
            ReadPartialSegmentSizeOne(compactData, type, jsonString);
        }

        // TestCaseType is only used to give the json strings a descriptive name.
        [Theory]
        [MemberData(nameof(LargeTestCases))]
        public static void TestJsonReaderLargeUtf8SegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
        {
            // Skipping really large JSON on Browser to prevent OOM
            if (PlatformDetection.IsBrowser && (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson))
            {
                return;
            }

            ReadFullySegmentSizeOne(compactData, type, jsonString);
        }

        // TestCaseType is only used to give the json strings a descriptive name.
        [Theory]
        [OuterLoop]
        [MemberData(nameof(LargeTestCases))]
        public static void TestJsonReaderLargestUtf8SegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
        {
            // Skipping really large JSON since slicing them (O(n^2)) is too slow.
            if (type == TestCaseType.Json40KB || type == TestCaseType.Json400KB || type == TestCaseType.ProjectLockJson)
            {
                return;
            }

            ReadPartialSegmentSizeOne(compactData, type, jsonString);
        }

        private static void ReadPartialSegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
        {
            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            for (int j = 0; j < dataUtf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

                long consumed = utf8JsonReader.BytesConsumed;
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out length, ref utf8JsonReader);
                actualStrSequence += Encoding.UTF8.GetString(resultSequence, 0, length);
                string message = $"Expected consumed: {dataUtf8.Length - consumed}, Actual consumed: {utf8JsonReader.BytesConsumed}, Index: {j}";
                Assert.True(dataUtf8.Length - consumed == utf8JsonReader.BytesConsumed, message);
                Assert.Equal(expectedStr, actualStrSequence);
            }
        }

        private static void ReadFullySegmentSizeOne(bool compactData, TestCaseType type, string jsonString)
        {
            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            Stream stream = new MemoryStream(dataUtf8);
            TextReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            string expectedStr = JsonTestHelper.NewtonsoftReturnStringHelper(reader);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            var utf8JsonReader = new Utf8JsonReader(sequence, isFinalBlock: true, default);
            byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
            string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);
            Assert.Equal(expectedStr, actualStrSequence);
        }

        [Theory]
        [MemberData(nameof(SmallTestCases))]
        public static void TestPartialJsonReaderMultiSegment(bool compactData, TestCaseType type, string jsonString)
        {
            _ = type;

            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlyMemory<byte> dataMemory = dataUtf8;

            List<ReadOnlySequence<byte>> sequences = JsonTestHelper.GetSequences(dataMemory);

            for (int i = 0; i < sequences.Count; i++)
            {
                ReadOnlySequence<byte> sequence = sequences[i];
                var json = new Utf8JsonReader(sequence, isFinalBlock: true, default);
                while (json.Read())
                    ;
                Assert.Equal(sequence.Length, json.BytesConsumed);

                Assert.True(sequence.Slice(json.Position).IsEmpty);
            }

            for (int i = 0; i < sequences.Count; i++)
            {
                ReadOnlySequence<byte> sequence = sequences[i];
                var json = new Utf8JsonReader(sequence);
                while (json.Read())
                    ;
                Assert.Equal(sequence.Length, json.BytesConsumed);

                Assert.True(sequence.Slice(json.Position).IsEmpty);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SmallTestCases))]
        public static void TestPartialJsonReaderSlicesMultiSegment(bool compactData, TestCaseType type, string jsonString)
        {
            _ = type;

            // Remove all formatting/indendation
            if (compactData)
            {
                jsonString = JsonTestHelper.GetCompactString(jsonString);
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlyMemory<byte> dataMemory = dataUtf8;

            List<ReadOnlySequence<byte>> sequences = JsonTestHelper.GetSequences(dataMemory);

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
                    json = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, jsonState);
                    while (json.Read())
                        ;
                    Assert.Equal(dataUtf8.Length - consumed, json.BytesConsumed);
                }
            }
        }

        [Theory]
        [MemberData(nameof(TrySkipValues))]
        public static void TestTrySkipMultiSegment(string jsonString, JsonTokenType lastToken)
        {
            List<JsonTokenType> expectedTokenTypes = JsonTestHelper.GetTokenTypes(jsonString);
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(dataUtf8);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Disallow);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Skip);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Allow);

            sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Disallow);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Skip);
            TrySkipHelper(sequence, lastToken, expectedTokenTypes, JsonCommentHandling.Allow);
        }

        [Theory]
        [MemberData(nameof(TrySkipValues))]
        public static void TestTrySkipWithCommentsMultiSegment(string jsonString, JsonTokenType lastToken)
        {
            List<JsonTokenType> expectedTokenTypesWithoutComments = JsonTestHelper.GetTokenTypes(jsonString);

            jsonString = JsonTestHelper.InsertCommentsEverywhere(jsonString);

            List<JsonTokenType> expectedTokenTypes = JsonTestHelper.GetTokenTypes(jsonString);

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(dataUtf8);
            TrySkipHelper(sequence, JsonTokenType.Comment, expectedTokenTypes, JsonCommentHandling.Allow);
            TrySkipHelper(sequence, lastToken, expectedTokenTypesWithoutComments, JsonCommentHandling.Skip);

            sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
            TrySkipHelper(sequence, JsonTokenType.Comment, expectedTokenTypes, JsonCommentHandling.Allow);
            TrySkipHelper(sequence, lastToken, expectedTokenTypesWithoutComments, JsonCommentHandling.Skip);
        }

        private static void TrySkipHelper(ReadOnlySequence<byte> dataUtf8, JsonTokenType lastToken, List<JsonTokenType> expectedTokenTypes, JsonCommentHandling commentHandling)
        {
            var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            JsonReaderState previous = json.CurrentState;
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(0, json.BytesConsumed);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.True(json.TrySkip());

            JsonReaderState current = json.CurrentState;
            Assert.Equal(JsonTokenType.None, json.TokenType);
            Assert.Equal(previous, current);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(0, json.BytesConsumed);
            Assert.False(json.HasValueSequence);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            int totalReads = 0;
            while (json.Read())
            {
                totalReads++;
            }

            Assert.Equal(expectedTokenTypes.Count, totalReads);

            previous = json.CurrentState;
            Assert.Equal(lastToken, json.TokenType);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            Assert.True(json.TrySkip());

            current = json.CurrentState;
            Assert.Equal(previous, current);
            Assert.Equal(lastToken, json.TokenType);
            Assert.Equal(0, json.CurrentDepth);
            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.False(json.HasValueSequence);
            Assert.False(json.ValueIsEscaped);
            Assert.True(json.ValueSpan.SequenceEqual(default));
            Assert.True(json.ValueSequence.IsEmpty);

            for (int i = 0; i < totalReads; i++)
            {
                state = new JsonReaderState(new JsonReaderOptions { CommentHandling = commentHandling });
                json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
                for (int j = 0; j < i; j++)
                {
                    Assert.True(json.Read());
                }
                Assert.True(json.TrySkip());
                Assert.True(expectedTokenTypes[i] == json.TokenType, $"Expected: {expectedTokenTypes[i]}, Actual: {json.TokenType}, , Index: {i}, BytesConsumed: {json.BytesConsumed}");
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
                    Assert.Fail("Expected JsonException for multi-segment data was not thrown.");
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
                    Assert.Fail("Expected JsonException for multi-segment data was not thrown.");
                }
                catch (JsonException ex)
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
                Assert.Fail("Expected JsonException was not thrown with single-segment data.");
            }
            catch (JsonException ex)
            {
                Assert.Equal(0, ex.LineNumber);
                Assert.Equal(0, ex.BytePositionInLine);
            }
        }

        [Theory]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, true, 1)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, true, 2)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, true, 3)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, false, 1)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, false, 2)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'1' }, false, 3)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, true, 1)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, true, 2)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, true, 3)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, false, 1)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, false, 2)]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF }, false, 3)]
        public static void TestBOMWithSingleJsonValueMultiSegment(byte[] utf8BomAndValue, bool isFinalBlock, int segmentSize)
        {
            Assert.ThrowsAny<JsonException>(() =>
            {
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8BomAndValue, segmentSize);
                var json = new Utf8JsonReader(sequence, isFinalBlock: isFinalBlock, state: default);
                json.Read();
            });
        }

        [Fact]
        public static void TestSingleStringsMultiSegmentByOne()
        {
            string jsonString = "\"Hello, \\u0041hson!\"";
            string expectedString = "Hello, \\u0041hson!, ";

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            for (int j = 0; j < dataUtf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

                long consumed = utf8JsonReader.BytesConsumed;
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out length, ref utf8JsonReader);
                actualStrSequence += Encoding.UTF8.GetString(resultSequence, 0, length);
                string message = $"Expected consumed: {dataUtf8.Length - consumed}, Actual consumed: {utf8JsonReader.BytesConsumed}, Index: {j}";
                Assert.True(dataUtf8.Length - consumed == utf8JsonReader.BytesConsumed, message);
                Assert.Equal(expectedString, actualStrSequence);
            }
        }

        [Fact]
        public static void TestSingleStringsMultiSegment()
        {
            string jsonString = "\"Hello, \\u0041hson!\"";
            string expectedString = "Hello, \\u0041hson!, ";

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(dataUtf8);

            for (int j = 0; j < dataUtf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                byte[] resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out int length, ref utf8JsonReader);
                string actualStrSequence = Encoding.UTF8.GetString(resultSequence, 0, length);

                Assert.Equal(0, utf8JsonReader.TokenStartIndex);

                long consumed = utf8JsonReader.BytesConsumed;
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                resultSequence = JsonTestHelper.ReaderLoop(dataUtf8.Length, out length, ref utf8JsonReader);
                actualStrSequence += Encoding.UTF8.GetString(resultSequence, 0, length);
                string message = $"Expected consumed: {dataUtf8.Length - consumed}, Actual consumed: {utf8JsonReader.BytesConsumed}, Index: {j}";
                Assert.True(dataUtf8.Length - consumed == utf8JsonReader.BytesConsumed, message);
                Assert.Equal(expectedString, actualStrSequence);

                Assert.Equal(0, utf8JsonReader.TokenStartIndex);
            }
        }

        [Fact]
        public static void TestMultiSegmentStringConversionToDateTime()
        {
            string jsonString = "\"1997-07-16\"";
            string expectedString = "1997-07-16";
            int expectedTokenLength = 10;
            DateTime expectedDateTime = DateTime.Parse(expectedString);

            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8);

            for (int j = 0; j < utf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                ReadDateTimeHelper(ref utf8JsonReader, expectedDateTime, expectedTokenLength);

                Assert.Equal(0, utf8JsonReader.TokenStartIndex);

                long consumed = utf8JsonReader.BytesConsumed;
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                ReadDateTimeHelper(ref utf8JsonReader, expectedDateTime, expectedTokenLength);
            }
        }

        private static void ReadDateTimeHelper(ref Utf8JsonReader jsonReader, DateTime expectedValue, long expectedTokenLength)
        {
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonTokenType.String)
                {
                    long tokenLength = jsonReader.HasValueSequence ? jsonReader.ValueSequence.Length : jsonReader.ValueSpan.Length;
                    Assert.Equal(expectedTokenLength, tokenLength);
                    Assert.Equal(expectedValue, jsonReader.GetDateTime());
                }
            }
        }

        [Fact]
        public static void TestMultiSegmentStringConversionToDateTimeOffset()
        {
            string jsonString = "\"1997-07-16\"";
            string expectedString = "1997-07-16";
            int expectedTokenLength = 10;
            DateTimeOffset expectedDateTimeOffset = DateTimeOffset.Parse(expectedString);

            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8);

            for (int j = 0; j < utf8.Length; j++)
            {
                var utf8JsonReader = new Utf8JsonReader(sequence.Slice(0, j), isFinalBlock: false, default);
                ReadDateTimeOffsetHelper(ref utf8JsonReader, expectedDateTimeOffset, expectedTokenLength);

                Assert.Equal(0, utf8JsonReader.TokenStartIndex);

                long consumed = utf8JsonReader.BytesConsumed;
                utf8JsonReader = new Utf8JsonReader(sequence.Slice(consumed), isFinalBlock: true, utf8JsonReader.CurrentState);
                ReadDateTimeOffsetHelper(ref utf8JsonReader, expectedDateTimeOffset, expectedTokenLength);
            }
        }

        private static void ReadDateTimeOffsetHelper(ref Utf8JsonReader jsonReader, DateTimeOffset expectedValue, long expectedTokenLength)
        {
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonTokenType.String)
                {
                    long tokenLength = jsonReader.HasValueSequence ? jsonReader.ValueSequence.Length : jsonReader.ValueSpan.Length;
                    Assert.Equal(expectedTokenLength, tokenLength);
                    Assert.Equal(expectedValue, jsonReader.GetDateTimeOffset());
                }
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
                Assert.Fail("Expected JsonException due to invalid JSON.");
            }
            catch (JsonException)
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
        [InlineData("/*a*/[{\"testA\":[{\"testB\":[{\"testC\":123}]}]}]", "testAtestBtestC123", "atestAtestBtestC123")]
        [InlineData("{\"testA\":[1/*hi*//*bye*/, 2, 3], \"testB\": 4}", "testA123testB4", "testA1hibye23testB4")]
        [InlineData("{\"test\":[[[123,456]]]}", "test123456", "test123456")]
        [InlineData("/*a*//*z*/[/*b*//*z*/123/*c*//*z*/,/*d*//*z*/456/*e*//*z*/]/*f*//*z*/", "123456", "azbz123czdz456ezfz")]
        [InlineData("[123,/*hi*/456/*bye*/]", "123456", "123hi456bye")]
        [InlineData("[123,//hi\n456//bye\n]", "123456", "123hi456bye")]
        [InlineData("[123,//hi\r456//bye\r]", "123456", "123hi456bye")]
        [InlineData("[123,//hi\r\n456]", "123456", "123hi456")]
        [InlineData("/*a*//*z*/{/*b*//*z*/\"test\":/*c*//*z*/[/*d*//*z*/[/*e*//*z*/[/*f*//*z*/123/*g*//*z*/,/*h*//*z*/456/*i*//*z*/]/*j*//*z*/]/*k*//*z*/]/*l*//*z*/}/*m*//*z*/",
        "test123456", "azbztestczdzezfz123gzhz456izjzkzlzmz")]
        [InlineData("//a\n//z\n{//b\n//z\n\"test\"://c\n//z\n[//d\n//z\n[//e\n//z\n[//f\n//z\n123//g\n//z\n,//h\n//z\n456//i\n//z\n]//j\n//z\n]//k\n//z\n]//l\n//z\n}//m\n//z\n",
        "test123456", "azbztestczdzezfz123gzhz456izjzkzlzmz")]
        public static void AllowCommentStackMismatchMultiSegment(string jsonString, string expectedWithoutComments, string expectedWithComments)
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            var sequence = new ReadOnlySequence<byte>(data);
            TestReadingJsonWithComments(data, sequence, expectedWithoutComments, expectedWithComments);

            sequence = JsonTestHelper.GetSequence(data, 1);
            TestReadingJsonWithComments(data, sequence, expectedWithoutComments, expectedWithComments);

            sequence = JsonTestHelper.GetSequence(data, 6);
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
                    Assert.Equal(2, json.TokenStartIndex);
                }

                Assert.Equal(inputData, sequence.Slice(0, json.Position).ToArray());
            }
        }

        // For first line in each case:
        //     . represents contiguous characters in input.
        //     | represents end of a segment in the sequence. Character below | is last character of particular segment.
        //
        //     Note: \ for escape sequence has neither a . nor a |
        //
        // Second line in each case represents whether the resulting token after parsing has a value sequence or not.
        //     T(rue) indicates presence of value sequence and F(alse) indicates otherwise. T or F is written above first
        //       character of partiular token.
        //     - indicates that token that begins at character right below it has been skipped during parsing and hence there
        //       is no truth value representation of the same in the expectedValueSequence* in each case.
        [Theory]
        //              . .........|.... .... ........ ... ........|............... ...............|......................|||
        //              F T                 F F            T                           F      T           F     F      F   FF
        [InlineData(0, "{\"property name\": [\"value 1\", \"value 2 across sequence\", 12345, 1234567890, true, false, null]}")]

        //              .. .............. .... ........ ... .......|................ .....|.................|......|......|..||
        //              FF F                 F F            T                           T      F           T     T      T   FFF
        [InlineData(1, "[{\"property name\": [\"value 1\", \"value 2 across sequence\", 12345, 1234567890, true, false, null]}]")]

        //              . .............. .................... | . ...........|............ ..................|...... . |..
        // Skip:        F F                 F-                    T                         -                           FF
        // Allow:       F F                 FF                    T                         T                           FF
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
                    expectedHasValueSequence = new bool[] { false, true, false, false, true, false, true, false, false, false, false, false };
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
                    expectedHasValueSequenceSkip = new bool[] { false, false, false, true, false, false };
                    expectedHasValueSequenceAllow = new bool[] { false, false, false, false, true, true, false, false };
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

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var json = new Utf8JsonReader(sequence, new JsonReaderOptions { CommentHandling = commentHandling });

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

        [Theory]
        [MemberData(nameof(CommentTestLineSeparators))]
        public static void ConsumeSingleLineCommentMultiSpanTest(string lineSeparator)
        {
            string expected = "Comment";
            string jsonData = "{//" + expected + lineSeparator + "}";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            for (int i = 0; i < jsonData.Length; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });

                var json = new Utf8JsonReader(sequence.Slice(0, i), isFinalBlock: false, state);
                VerifyReadLoop(ref json, expected);

                json = new Utf8JsonReader(sequence.Slice(json.BytesConsumed), isFinalBlock: true, json.CurrentState);
                VerifyReadLoop(ref json, expected);
            }
        }

        [Theory]
        [MemberData(nameof(CommentTestLineSeparators))]
        public static void SkipSingleLineCommentMultiSpanTest(string lineSeparator)
        {
            string expected = "Comment";
            string jsonData = "{//" + expected + lineSeparator + "}";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);

            for (int i = 0; i < jsonData.Length; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });

                var json = new Utf8JsonReader(sequence.Slice(0, i), isFinalBlock: false, state);
                VerifyReadLoop(ref json, null);

                json = new Utf8JsonReader(sequence.Slice(json.BytesConsumed), isFinalBlock: true, json.CurrentState);
                VerifyReadLoop(ref json, null);
            }
        }

        [Theory]
        [InlineData("//\u2028", 1)]
        [InlineData("//\u2028", 2)]
        [InlineData("//\u2028", 3)]
        [InlineData("//\u2028", 100)]
        [InlineData("//\u2029", 1)]
        [InlineData("//\u2029", 2)]
        [InlineData("//\u2029", 3)]
        [InlineData("//\u2029", 100)]
        [InlineData("// \u2028", 1)]
        [InlineData("// \u2028", 2)]
        [InlineData("// \u2028", 3)]
        [InlineData("// \u2028", 100)]
        [InlineData("//   \u2028", 1)]
        [InlineData("//   \u2028", 2)]
        [InlineData("//   \u2028", 3)]
        [InlineData("//   \u2028", 100)]
        [InlineData("//  \u2029 ", 1)]
        [InlineData("//  \u2029 ", 2)]
        [InlineData("//  \u2029 ", 3)]
        [InlineData("//  \u2029 ", 100)]
        [InlineData("//  \u2029  ", 1)]
        [InlineData("//  \u2029  ", 2)]
        [InlineData("//  \u2029  ", 3)]
        [InlineData("//  \u2029  ", 100)]
        public static void JsonWithSingleLineCommentEndingWithNonStandardLineEndingMultiSegment(string jsonString, int segmentSize)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);

            foreach (JsonCommentHandling jsonCommentHandling in typeof(JsonCommentHandling).GetEnumValues())
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = jsonCommentHandling });

                foreach (bool isFinalBlock in new bool[] { false, true })
                {
                    var json = new Utf8JsonReader(sequence, isFinalBlock, state);

                    try
                    {
                        json.Read();
                        Assert.Fail($"Expected JsonException was not thrown. CommentHandling = {jsonCommentHandling}");
                    }
                    catch (JsonException) { }
                }
            }
        }

        [Theory]
        [InlineData("{ \"foo\" : \"bar\" //\u2028\n}", 1)]
        [InlineData("{ \"foo\" : \"bar\" //\u2028\n}", 2)]
        [InlineData("{ \"foo\" : \"bar\" //\u2028\n}", 3)]
        [InlineData("{ \"foo\" : \"bar\" //\u2028\n}", 100)]
        public static void JsonWithSingleLineCommentInTheMiddleOfThePayloadEndingWithNonStandardLineEndingMultiSegment(string jsonString, int segmentSize)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);

            foreach (JsonCommentHandling jsonCommentHandling in typeof(JsonCommentHandling).GetEnumValues())
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = jsonCommentHandling });

                foreach (bool isFinalBlock in new bool[] { false, true })
                {
                    var json = new Utf8JsonReader(sequence, isFinalBlock, state);

                    try
                    {
                        Assert.True(json.Read()); // {
                        Assert.True(json.Read()); // "foo"
                        Assert.True(json.Read()); // "bar"
                        json.Read(); // bad comment
                        Assert.Fail($"Expected JsonException was not thrown. CommentHandling = {jsonCommentHandling}");
                    }
                    catch (JsonException) { }
                }
            }
        }

        [Theory]
        [InlineData("//", "", 1, 2)]
        [InlineData("//", "", 2, 2)]
        [InlineData("//", "", 3, 2)]
        [InlineData("//", "", 100, 2)]
        [InlineData("//a", "a", 1, 3)]
        [InlineData("//a", "a", 2, 3)]
        [InlineData("//a", "a", 3, 3)]
        [InlineData("//a", "a", 100, 3)]
        [InlineData("//abc", "abc", 1, 5)]
        [InlineData("//abc", "abc", 2, 5)]
        [InlineData("//abc", "abc", 3, 5)]
        [InlineData("//abc", "abc", 100, 5)]
        public static void JsonWithSingleLineCommentWithNoLineEndingsFinalBlockMultiSegment(string jsonString, string expectedComment, int segmentSize, int expectedBytesConsumed)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);
            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.Comment, json.TokenType);
            Assert.Equal(expectedComment, json.GetComment());
            Assert.False(json.Read());
            Assert.Equal(expectedBytesConsumed, json.BytesConsumed);
        }

        [Theory]
        [InlineData("//", 1)]
        [InlineData("//", 2)]
        [InlineData("//", 3)]
        [InlineData("//", 100)]
        [InlineData("//a", 1)]
        [InlineData("//a", 2)]
        [InlineData("//a", 3)]
        [InlineData("//a", 100)]
        [InlineData("//abc", 1)]
        [InlineData("//abc", 2)]
        [InlineData("//abc", 3)]
        [InlineData("//abc", 100)]
        public static void JsonWithSingleLineCommentWithNoLineEndingsNonFinalBlockMultiSegment(string jsonString, int segmentSize)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);
            var json = new Utf8JsonReader(sequence, isFinalBlock: false, state);

            Assert.False(json.Read());
            Assert.Equal(0, json.BytesConsumed);
        }

        [Theory]
        [InlineData("/**/", "", 1, 4)]
        [InlineData("/**/", "", 2, 4)]
        [InlineData("/**/", "", 3, 4)]
        [InlineData("/**/", "", 100, 4)]
        [InlineData("/*a*/", "a", 1, 5)]
        [InlineData("/*a*/", "a", 2, 5)]
        [InlineData("/*a*/", "a", 3, 5)]
        [InlineData("/*a*/", "a", 100, 5)]
        [InlineData("/*abc*/", "abc", 1, 7)]
        [InlineData("/*abc*/", "abc", 2, 7)]
        [InlineData("/*abc*/", "abc", 3, 7)]
        [InlineData("/*abc*/", "abc", 100, 7)]
        [InlineData("/*\u2028*/", "\u2028", 1, 7)]
        [InlineData("/*\u2028*/", "\u2028", 2, 7)]
        [InlineData("/*\u2028*/", "\u2028", 3, 7)]
        [InlineData("/*\u2028*/", "\u2028", 100, 7)]
        [InlineData("/*\u2029*/", "\u2029", 1, 7)]
        [InlineData("/*\u2029*/", "\u2029", 2, 7)]
        [InlineData("/*\u2029*/", "\u2029", 3, 7)]
        [InlineData("/*\u2029*/", "\u2029", 100, 7)]
        public static void JsonWithMultiLineCommentWithNoLineEndingsFinalBlockMultiSegment(string jsonString, string expectedComment, int segmentSize, int expectedBytesConsumed)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);
            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state);

            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.Comment, json.TokenType);
            Assert.Equal(expectedComment, json.GetComment());
            Assert.False(json.Read());
            Assert.Equal(expectedBytesConsumed, json.BytesConsumed);
        }

        [Theory]
        [InlineData("/*", 1)]
        [InlineData("/*", 2)]
        [InlineData("/*", 100)]
        [InlineData("/*  ", 1)]
        [InlineData("/*  ", 2)]
        [InlineData("/*  ", 3)]
        [InlineData("/*  ", 100)]
        [InlineData("/**", 1)]
        [InlineData("/**", 2)]
        [InlineData("/**", 3)]
        [InlineData("/**", 100)]
        [InlineData("/*  *", 1)]
        [InlineData("/*  *", 2)]
        [InlineData("/*  *", 3)]
        [InlineData("/*  *", 100)]
        public static void JsonWithUnfinishedMultiLineCommentNonFinalBlockMultiSegment(string jsonString, int segmentSize)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);
            var json = new Utf8JsonReader(sequence, isFinalBlock: false, state);

            Assert.False(json.Read());
            Assert.Equal(0, json.BytesConsumed);
        }

        [Theory]
        [InlineData("//", "", 1)]
        [InlineData("//", "", 2)]
        [InlineData("//", "", 3)]
        [InlineData("//", "", 100)]
        [InlineData("//a", "a", 1)]
        [InlineData("//a", "a", 2)]
        [InlineData("//a", "a", 3)]
        [InlineData("//a", "a", 100)]
        [InlineData("//abc", "abc", 1)]
        [InlineData("//abc", "abc", 2)]
        [InlineData("//abc", "abc", 3)]
        [InlineData("//abc", "abc", 100)]
        public static void JsonWithSingleLineCommentWithRegularLineEndingMultiSegment(string jsonStringWithoutLineEnding, string expectedComment, int segmentSize)
        {
            foreach (string lineEnding in new string[] { "\r", "\r ", "\r\n", "\r\n ", "\n", "\n ", "" })
            {
                foreach (bool isFinalBlock in new bool[] { false, true })
                {
                    if (!isFinalBlock && (lineEnding == "\r" || lineEnding == ""))
                    {
                        // In this case parser would return false on the first Read (and check for \n on the next segment)
                        // which is not the purpose of this test and is covered separately
                        continue;
                    }

                    byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonStringWithoutLineEnding + lineEnding);
                    var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
                    ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, segmentSize);

                    var json = new Utf8JsonReader(sequence, isFinalBlock, state);

                    Assert.True(json.Read(), $"Expected read to return true. IsFinalBlock = {isFinalBlock}; LineEnding = {string.Join("", lineEnding.Select((c) => ((byte)c).ToString("X2")))}");
                    Assert.Equal(JsonTokenType.Comment, json.TokenType);
                    Assert.Equal(expectedComment, json.GetComment());
                    Assert.False(json.Read());
                }
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValue))]
        public static void ReadJsonTokenWithExtraValueMultiSegment(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: false);
                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValueAndComments))]
        public static void ReadJsonTokenWithExtraValueAndCommentsMultiSegment(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: false);
                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonTokenWithExtraValueAndComments))]
        public static void ReadJsonTokenWithExtraValueAndCommentsAppendedMultiSegment(string jsonString)
        {
            jsonString = "  /* comment */  /* comment */  " + jsonString;
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: false, commentsAppended: true);
                TestReadTokenWithExtra(sequence, commentHandling, isFinalBlock: true, commentsAppended: true);
            }
        }

        private static void TestReadTokenWithExtra(ReadOnlySequence<byte> sequence, JsonCommentHandling commentHandling, bool isFinalBlock, bool commentsAppended = false)
        {
            JsonReaderState state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
            Utf8JsonReader reader = new Utf8JsonReader(sequence, isFinalBlock, state);

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

            JsonTestHelper.AssertThrows<JsonException>(ref reader, (ref Utf8JsonReader jsonReader) =>
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
        public static void JsonWithTrailingCommasMultiSegment_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            {
                JsonReaderState state = default;
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);
            }

            {
                var state = new JsonReaderState(options: default);
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);
            }

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(sequence, state, allowTrailingCommas, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommas))]
        public static void JsonWithTrailingCommasMultiSegment_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(sequence, state, allowTrailingCommas, expectThrow: true);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommasAndComments))]
        public static void JsonWithTrailingCommasAndCommentsMultiSegment_Valid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(sequence, state, allowTrailingCommas, expectThrow: false);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithInvalidTrailingCommasAndComments))]
        public static void JsonWithTrailingCommasAndCommentsMultiSegment_Invalid(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                TrailingCommasHelper(sequence, state, allow: false, expectThrow: true);

                bool allowTrailingCommas = true;
                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = allowTrailingCommas });
                TrailingCommasHelper(sequence, state, allowTrailingCommas, expectThrow: true);
            }
        }

        private static void TrailingCommasHelper(ReadOnlySequence<byte> utf8, JsonReaderState state, bool allow, bool expectThrow)
        {
            var reader = new Utf8JsonReader(utf8, isFinalBlock: true, state);

            Assert.Equal(allow, state.Options.AllowTrailingCommas);
            Assert.Equal(allow, reader.CurrentState.Options.AllowTrailingCommas);

            if (expectThrow)
            {
                JsonTestHelper.AssertThrows<JsonException>(ref reader, (ref Utf8JsonReader jsonReader) =>
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
        [InlineData("{]")]
        [InlineData("[}")]
        [InlineData("{// comment\n]")]
        [InlineData("[// comment\n}")]
        [InlineData("{,}")]
        [InlineData("[,]")]
        [InlineData("{// comment\n,}")]
        [InlineData("[// comment\n,]")]
        [InlineData("123, ")]
        [InlineData("\"abc\", ")]
        [InlineData("123, // comment\n")]
        [InlineData("\"abc\", // comment\n")]
        [InlineData("123 // comment\n,")]
        [InlineData("\"abc\" // comment\n,")]
        [InlineData("{\"Property1\": [ 42], 5}")]
        [InlineData("{\"Property1\": [ 42], // comment\n5}")]
        [InlineData("{\"Property1\": [ 42], // comment\r5}")]
        [InlineData("{\"Property1\": [ 42], // comment\r\n5}")]
        [InlineData("{\"Property1\": [ 42], /*comment*/5}")]
        [InlineData("{\"Property1\": [ 42] // comment\n,5}")]
        [InlineData("{\"Property1\": [ 42], // comment\n // comment\n5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, 5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r\n5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, /*comment*/5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42} // comment\n,5}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n // comment\n5}")]
        [InlineData("{// comment\n5}")]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", ~RuntimeConfiguration.Release)]
        public static void ReadInvalidJsonStringsWithComments(string jsonString)
        {
            byte[] input = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var options = new JsonReaderOptions() { CommentHandling = commentHandling };
                var optionsWithTrailing = new JsonReaderOptions() { CommentHandling = commentHandling, AllowTrailingCommas = true };

                var reader = new Utf8JsonReader(input, options);
                ValidateThrows(ref reader);

                reader = new Utf8JsonReader(input, optionsWithTrailing);
                ValidateThrows(ref reader);

                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(input, 1);
                reader = new Utf8JsonReader(sequence, options);
                ValidateThrows(ref reader);

                reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                ValidateThrows(ref reader);

                for (int splitLocation = 0; splitLocation < input.Length; splitLocation++)
                {
                    sequence = JsonTestHelper.CreateSegments(input, splitLocation);
                    reader = new Utf8JsonReader(sequence, options);
                    ValidateThrows(ref reader);

                    reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                    ValidateThrows(ref reader);
                }

                for (int firstSplit = 0; firstSplit < input.Length; firstSplit++)
                {
                    for (int secondSplit = firstSplit; secondSplit < input.Length; secondSplit++)
                    {
                        sequence = JsonTestHelper.CreateSegments(input, firstSplit, secondSplit);
                        reader = new Utf8JsonReader(sequence, options);
                        ValidateThrows(ref reader);

                        reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                        ValidateThrows(ref reader);
                    }
                }
            }
        }

        private static void ValidateThrows(ref Utf8JsonReader reader)
        {
            JsonTestHelper.AssertThrows<JsonException>(ref reader, (ref Utf8JsonReader jsonReader) =>
            {
                while (jsonReader.Read())
                    ;
            });
        }

        [Theory]
        [InlineData("123 ")]
        [InlineData("\"abc\" ")]
        [InlineData("123 // comment\n")]
        [InlineData("\"abc\" // comment\n")]
        [InlineData("{// comment\n}")]
        [InlineData("[// comment\n]")]
        [InlineData("{\"Property1\": [ 42], \"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42], // comment\n\"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42], // comment\r\"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42], // comment\r\n\"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42], /*comment*/\"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42] // comment\n,\"Property2\": 42}")]
        [InlineData("{\"Property1\": [ 42], // comment\n // comment\n\"Property2\": 42}")]
        [InlineData("[[ 42], // comment\n 42]")]
        [InlineData("[[ 42] // comment\n, 42]")]
        [InlineData("[[ 42, // comment\n 43], // comment\n 42]")]
        [InlineData("[[ \"42\", // comment\n 43], // comment\n 42]")]
        [InlineData("[[ true, // comment\n 43], // comment\n 42]")]
        [InlineData("[[ false, // comment\n 43], // comment\n 42]")]
        [InlineData("[[ null, // comment\n 43], // comment\n 42]")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, \"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n\"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r\"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r\n\"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, /*comment*/\"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42} // comment\n,\"Property2\": 42}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n // comment\n\"Property2\": 42}")]
        [InlineData("[{\"Property1\": 42}, // comment\n 42]")]
        [InlineData("[{\"Property1\": 42} // comment\n, 42]")]
        [InlineData("[{\"Property1\": 42, // comment\n \"prop\": 43 }, // comment\n 42]")]
        [InlineData("[{\"Property1\": \"42\", // comment\n \"prop\": 43 }, // comment\n 42]")]
        [InlineData("[{\"Property1\": true, // comment\n \"prop\": 43 }, // comment\n 42]")]
        [InlineData("[{\"Property1\": false, // comment\n \"prop\": 43 }, // comment\n 42]")]
        [InlineData("[{\"Property1\": null, // comment\n \"prop\": 43 }, // comment\n 42]")]
        public static void ReadJsonStringsWithComments(string jsonString)
        {
            byte[] input = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var options = new JsonReaderOptions() { CommentHandling = commentHandling };
                var optionsWithTrailing = new JsonReaderOptions() { CommentHandling = commentHandling, AllowTrailingCommas = true };

                var reader = new Utf8JsonReader(input, options);
                ReadWithCommentsHelper(ref reader, input.Length);

                reader = new Utf8JsonReader(input, optionsWithTrailing);
                ReadWithCommentsHelper(ref reader, input.Length);

                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(input, 1);
                reader = new Utf8JsonReader(sequence, options);
                ReadWithCommentsHelper(ref reader, input.Length);

                reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                ReadWithCommentsHelper(ref reader, input.Length);

                for (int splitLocation = 0; splitLocation < input.Length; splitLocation++)
                {
                    sequence = JsonTestHelper.CreateSegments(input, splitLocation);
                    reader = new Utf8JsonReader(sequence, options);
                    ReadWithCommentsHelper(ref reader, input.Length);

                    reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                    ReadWithCommentsHelper(ref reader, input.Length);
                }

                for (int firstSplit = 0; firstSplit < input.Length; firstSplit++)
                {
                    for (int secondSplit = firstSplit; secondSplit < input.Length; secondSplit++)
                    {
                        sequence = JsonTestHelper.CreateSegments(input, firstSplit, secondSplit);
                        reader = new Utf8JsonReader(sequence, options);
                        ReadWithCommentsHelper(ref reader, input.Length);

                        reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                        ReadWithCommentsHelper(ref reader, input.Length);
                    }
                }
            }
        }

        [Theory]
        [InlineData("{\"Property1\": [ 42], }")]
        [InlineData("{\"Property1\": [ 42], // comment\n}")]
        [InlineData("{\"Property1\": [ 42], // comment\r}")]
        [InlineData("{\"Property1\": [ 42], // comment\r\n}")]
        [InlineData("{\"Property1\": [ 42], /*comment*/}")]
        [InlineData("{\"Property1\": [ 42] // comment\n,}")]
        [InlineData("{\"Property1\": [ 42], // comment\n // comment\n}")]
        [InlineData("[[ 42], // comment\n ]")]
        [InlineData("[[ 42] // comment\n, ]")]
        [InlineData("[[ 42, // comment\n ], // comment\n ]")]
        [InlineData("[[ \"42\", // comment\n ], // comment\n ]")]
        [InlineData("[[ true, // comment\n ], // comment\n ]")]
        [InlineData("[[ false, // comment\n ], // comment\n ]")]
        [InlineData("[[ null, // comment\n ], // comment\n ]")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, }")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\r\n}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, /*comment*/}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42} // comment\n,}")]
        [InlineData("{\"Property1\": {\"Property1.1\": 42}, // comment\n // comment\n}")]
        [InlineData("[{\"Property1\": 42}, // comment\n ]")]
        [InlineData("[{\"Property1\": 42} // comment\n, ]")]
        [InlineData("[{\"Property1\": 42, // comment\n }, // comment\n ]")]
        [InlineData("[{\"Property1\": \"42\", // comment\n }, // comment\n ]")]
        [InlineData("[{\"Property1\": true, // comment\n }, // comment\n ]")]
        [InlineData("[{\"Property1\": false, // comment\n }, // comment\n ]")]
        [InlineData("[{\"Property1\": null, // comment\n }, // comment\n ]")]
        public static void ReadJsonStringsWithCommentsAndTrailingCommas(string jsonString)
        {
            byte[] input = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var options = new JsonReaderOptions() { CommentHandling = commentHandling };
                var optionsWithTrailing = new JsonReaderOptions() { CommentHandling = commentHandling, AllowTrailingCommas = true };

                var reader = new Utf8JsonReader(input, options);
                ReadWithCommentsHelper(ref reader, -1, validateThrows: true);

                reader = new Utf8JsonReader(input, optionsWithTrailing);
                ReadWithCommentsHelper(ref reader, input.Length);

                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(input, 1);
                reader = new Utf8JsonReader(sequence, options);
                ReadWithCommentsHelper(ref reader, -1, validateThrows: true);

                reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                ReadWithCommentsHelper(ref reader, input.Length);

                for (int splitLocation = 0; splitLocation < input.Length; splitLocation++)
                {
                    sequence = JsonTestHelper.CreateSegments(input, splitLocation);
                    reader = new Utf8JsonReader(sequence, options);
                    ReadWithCommentsHelper(ref reader, input.Length, validateThrows: true);

                    reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                    ReadWithCommentsHelper(ref reader, input.Length);
                }

                for (int firstSplit = 0; firstSplit < input.Length; firstSplit++)
                {
                    for (int secondSplit = firstSplit; secondSplit < input.Length; secondSplit++)
                    {
                        sequence = JsonTestHelper.CreateSegments(input, firstSplit, secondSplit);
                        reader = new Utf8JsonReader(sequence, options);
                        ReadWithCommentsHelper(ref reader, input.Length, validateThrows: true);

                        reader = new Utf8JsonReader(sequence, optionsWithTrailing);
                        ReadWithCommentsHelper(ref reader, input.Length);
                    }
                }
            }
        }

        private static void ReadWithCommentsHelper(ref Utf8JsonReader reader, int expectedConsumed, bool validateThrows = false)
        {
            if (validateThrows)
            {
                try
                {
                    while (reader.Read())
                        ;
                    Assert.Fail("Expected JsonException was not thrown when reading JSON with trailing commas.");
                }
                catch (JsonException ex)
                {
                    Assert.Contains("trailing", ex.Message);
                }
            }
            else
            {
                while (reader.Read())
                    ;
                Assert.Equal(expectedConsumed, reader.BytesConsumed);
            }
        }

        [Theory]
        [MemberData(nameof(SingleJsonTokenStartIndex))]
        public static void TestTokenStartIndexMultiSegment_SingleValue(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var reader = new Utf8JsonReader(sequence, new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                reader = new Utf8JsonReader(sequence, new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(SingleJsonWithCommentsAllowTokenStartIndex))]
        public static void TestTokenStartIndexMultiSegment_SingleValueCommentsAllow(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow, AllowTrailingCommas = false });
            var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.True(reader.Read());
            Assert.Equal(expectedIndex, reader.TokenStartIndex);

            state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow, AllowTrailingCommas = true });
            reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.True(reader.Read());
            Assert.Equal(expectedIndex, reader.TokenStartIndex);
        }

        [Theory]
        [MemberData(nameof(SingleJsonWithCommentsTokenStartIndex))]
        public static void TestTokenStartIndexMultiSegment_SingleValueWithComments(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                if (commentHandling == JsonCommentHandling.Allow)
                {
                    Assert.Equal(JsonTokenType.Comment, reader.TokenType);
                    Assert.True(reader.Read());
                }
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
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
        public static void TestTokenStartIndexMultiSegment_ComplexArrayValue(string jsonString, int expectedIndex)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndex, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(ComplexObjectJsonTokenStartIndex))]
        public static void TestTokenStartIndexMultiSegment_ComplexObjectValue(string jsonString, int expectedIndexProperty, int expectedIndexValue)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
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
        public static void TestTokenStartIndexMultiSegment_ComplexObjectManyValues(string jsonString, int expectedIndexProperty, int expectedIndexValue)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = false });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                Assert.True(reader.Read());
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexProperty, reader.TokenStartIndex);
                Assert.True(reader.Read());
                Assert.Equal(expectedIndexValue, reader.TokenStartIndex);

                state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
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
        public static void TestTokenStartIndexMultiSegment_WithTrailingCommas(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                while (reader.Read())
                { }

                Assert.Equal(utf8.Length - 1, reader.TokenStartIndex);
            }
        }

        [Theory]
        [MemberData(nameof(JsonWithValidTrailingCommasAndComments))]
        public static void TestTokenStartIndexMultiSegment_WithTrailingCommasAndComments(string jsonString)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(jsonString);
            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8, 1);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                if (commentHandling == JsonCommentHandling.Disallow)
                {
                    continue;
                }

                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling, AllowTrailingCommas = true });
                var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
                while (reader.Read())
                { }

                Assert.Equal(utf8.Length - 1, reader.TokenStartIndex);
            }
        }
    }
}
