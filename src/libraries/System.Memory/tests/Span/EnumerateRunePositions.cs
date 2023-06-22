// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        private static string RunePositionString(RunePosition runePosition) =>
            $$"""RunePosition { Rune = {{runePosition.Rune}}, StartIndex = {{runePosition.StartIndex}}, Length = {{runePosition.Length}}, WasReplaced = {{runePosition.WasReplaced}} }""";

        private static void RunePosition_TestProps(Rune rune, int startIndex, int length, bool wasReplaced, RunePosition runePosition)
        {
            Assert.Equal(rune, runePosition.Rune);
            Assert.Equal(startIndex, runePosition.StartIndex);
            Assert.Equal(length, runePosition.Length);
            Assert.Equal(wasReplaced, runePosition.WasReplaced);
        }
        private static void RunePosition_TestEquals(RunePosition expected, RunePosition runePosition)
        {
            if (expected.Rune == runePosition.Rune && expected.StartIndex == runePosition.StartIndex &&
                expected.Length == runePosition.Length && expected.WasReplaced == runePosition.WasReplaced)
            {
                Assert.Equal(expected, runePosition);
                Assert.Equal(runePosition, expected);

                Assert.True(expected.Equals(runePosition));
                Assert.True(runePosition.Equals(expected));

                Assert.True(((object)expected).Equals(runePosition));
                Assert.True(((object)runePosition).Equals(expected));

                Assert.True(expected == runePosition);
                Assert.True(runePosition == expected);

                Assert.False(expected != runePosition);
                Assert.False(runePosition != expected);

                Assert.Equal(expected.GetHashCode(), runePosition.GetHashCode());
            }
            else
            {
                Assert.NotEqual(expected, runePosition);
                Assert.NotEqual(runePosition, expected);

                Assert.False(expected.Equals(runePosition));
                Assert.False(runePosition.Equals(expected));

                Assert.False(((object)expected).Equals(runePosition));
                Assert.False(((object)runePosition).Equals(expected));

                Assert.False(expected == runePosition);
                Assert.False(runePosition == expected);

                Assert.True(expected != runePosition);
                Assert.True(runePosition != expected);

                Assert.NotEqual(expected.GetHashCode(), runePosition.GetHashCode());
            }
        }

        private static void RunePosition_TestDeconstruct(RunePosition runePosition)
        {
            {
                (Rune rune, int startIndex) = runePosition;
                Assert.Equal(runePosition.Rune, rune);
                Assert.Equal(runePosition.StartIndex, startIndex);
            }
            {
                (Rune rune, int startIndex, int length) = runePosition;
                Assert.Equal(runePosition.Rune, rune);
                Assert.Equal(runePosition.StartIndex, startIndex);
                Assert.Equal(runePosition.Length, length);
            }
            {
                (Rune rune, int startIndex, int length, bool wasReplaced) = runePosition;
                Assert.Equal(runePosition.Rune, rune);
                Assert.Equal(runePosition.StartIndex, startIndex);
                Assert.Equal(runePosition.Length, length);
                Assert.Equal(runePosition.WasReplaced, wasReplaced);
            }
        }

        [Fact]
        public static void RunePosition_DefaultTest()
        {
            RunePosition runePosition = default;
            RunePosition_TestProps(default, 0, 0, false, runePosition);
            RunePosition_TestEquals(default, runePosition);
            Assert.Equal(RunePositionString(default), runePosition.ToString());
            RunePosition_TestDeconstruct(runePosition);

            runePosition = new RunePosition();
            RunePosition_TestProps(default, 0, 0, false, runePosition);
            RunePosition_TestEquals(default, runePosition);
            Assert.Equal(RunePositionString(runePosition), runePosition.ToString());
            RunePosition_TestDeconstruct(runePosition);

            runePosition = runePosition with { Rune = new Rune('y') };
            RunePosition_TestProps(new Rune('y'), 0, 0, false, runePosition);
            runePosition = runePosition with { StartIndex = 54 };
            RunePosition_TestProps(new Rune('y'), 54, 0, false, runePosition);
            runePosition = runePosition with { Length = 3 };
            RunePosition_TestProps(new Rune('y'), 54, 3, false, runePosition);
            runePosition = runePosition with { WasReplaced = true };
            RunePosition_TestProps(new Rune('y'), 54, 3, true, runePosition);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("\uD86D\uDF54")] // valid surrogate pair
        [InlineData("\uFFFD")] // literal U+FFFD
        [InlineData("\0")]
        public static void RunePosition_UnitTest(string runeStr)
        {
            Rune.DecodeFromUtf16(runeStr, out Rune rune, out _);

            RunePosition runePosition1 = new RunePosition(rune, 0, rune.Utf16SequenceLength, false);
            RunePosition runePosition2 = new RunePosition(rune, 5, rune.Utf16SequenceLength, true);
            RunePosition runePosition3 = new RunePosition(rune, 100, 5, true);

            foreach (var startIndex in new [] {0, 5, 100})
            {
                foreach (var wasReplaced in new[] { true, false })
                {
                    RunePosition runePosition = new RunePosition(rune, startIndex, rune.Utf16SequenceLength, wasReplaced);
                    RunePosition_TestProps(rune, startIndex, rune.Utf16SequenceLength, wasReplaced, runePosition);

                    RunePosition_TestEquals(default, runePosition);
                    RunePosition_TestEquals(runePosition1, runePosition);
                    RunePosition_TestEquals(runePosition2, runePosition);
                    RunePosition_TestEquals(runePosition3, runePosition);

                    Assert.Equal(RunePositionString(runePosition), runePosition.ToString());
                    RunePosition_TestDeconstruct(runePosition);

                    runePosition = runePosition with { Rune = new Rune('y') };
                    RunePosition_TestProps(new Rune('y'), startIndex, rune.Utf16SequenceLength, wasReplaced, runePosition);
                    runePosition = runePosition with { StartIndex = 54 };
                    RunePosition_TestProps(new Rune('y'), 54, rune.Utf16SequenceLength, wasReplaced, runePosition);
                    runePosition = runePosition with { Length = 3 };
                    RunePosition_TestProps(new Rune('y'), 54, 3, wasReplaced, runePosition);
                    runePosition = runePosition with { WasReplaced = !wasReplaced };
                    RunePosition_TestProps(new Rune('y'), 54, 3, !wasReplaced, runePosition);
                }
            }
        }

        [Fact]
        public static void EnumerateRunePositions_Empty()
        {
            {
                RunePosition.Utf16Enumerator enumerator = RunePosition.EnumerateUtf16(ReadOnlySpan<char>.Empty).GetEnumerator();
                Assert.Equal(default, enumerator.Current);
                Assert.False(RunePosition.EnumerateUtf16(ReadOnlySpan<char>.Empty).GetEnumerator().MoveNext());
                Assert.Equal(default, enumerator.Current);
            }
            //Assert.False(RunePosition.EnumerateUtf8(ReadOnlySpan<char>.Empty).GetEnumerator().MoveNext());
        }

        [Theory]
        [InlineData(new char[0], new int[0])] // empty
        [InlineData(new char[] { 'x', 'y', 'z' }, new int[] { 'x', 'y', 'z' })]
        [InlineData(new char[] { 'x', '\uD86D', '\uDF54', 'y' }, new int[] { 'x', 0x2B754, 'y' })] // valid surrogate pair
        [InlineData(new char[] { 'x', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone high surrogate
        [InlineData(new char[] { 'x', '\uDF54', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone low surrogate
        [InlineData(new char[] { 'x', '\uD86D' }, new int[] { 'x', 0xFFFD })] // standalone high surrogate at end of string
        [InlineData(new char[] { 'x', '\uDF54' }, new int[] { 'x', 0xFFFD })] // standalone low surrogate at end of string
        [InlineData(new char[] { 'x', '\uD86D', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 0xFFFD, 'y' })] // two high surrogates should be two replacement chars
        [InlineData(new char[] { 'x', '\uFFFD', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // literal U+FFFD
        public static void EnumerateRunePositions_Battery16(char[] chars, int[] expected)
        {
            // Test data is smuggled as char[] instead of straight-up string since the test framework
            // doesn't like invalid UTF-16 literals.

            List<int> enumeratedValues = new List<int>();

            RunePosition.Utf16Enumerator enumerator = RunePosition.EnumerateUtf16(chars).GetEnumerator();
            Assert.Equal(default, enumerator.Current);

            int expectedIndex = 0;
            while (enumerator.MoveNext())
            {
                bool wasReplaced = Rune.DecodeFromUtf16(chars.AsSpan(expectedIndex), out Rune expectedRune, out int charsConsumed) != OperationStatus.Done;
                RunePosition runePosition = enumerator.Current;

                RunePosition_TestProps(expectedRune, expectedIndex, charsConsumed, wasReplaced, runePosition);

                expectedIndex += charsConsumed;
                enumeratedValues.Add(runePosition.Rune.Value);
            }
            Assert.Equal(default, enumerator.Current);

            Assert.Equal(expected, enumeratedValues.ToArray());
        }

        [Theory]
        [InlineData(new byte[0], new int[0])] // empty
        [InlineData(new byte[] { 0x30, 0x40, 0x50 }, new int[] { 0x30, 0x40, 0x50 })]
        [InlineData(new byte[] { 0x31, 0x80, 0x41 }, new int[] { 0x31, 0xFFFD, 0x41, })] // standalone continuation byte
        [InlineData(new byte[] { 0x32, 0xC1, 0x42 }, new int[] { 0x32, 0xFFFD, 0x42, })] // C1 is never a valid UTF-8 byte
        [InlineData(new byte[] { 0x33, 0xF5, 0x43 }, new int[] { 0x33, 0xFFFD, 0x43, })] // F5 is never a valid UTF-8 byte
        [InlineData(new byte[] { 0x34, 0xC2, 0x44 }, new int[] { 0x34, 0xFFFD, 0x44, })] // C2 is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0x35, 0xED, 0x45 }, new int[] { 0x35, 0xFFFD, 0x45, })] // ED is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0x36, 0xF4, 0x46 }, new int[] { 0x36, 0xFFFD, 0x46, })] // F4 is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0x37, 0xC2, 0xC2, 0x47 }, new int[] { 0x37, 0xFFFD, 0xFFFD, 0x47, })] // C2 not followed by continuation byte
        [InlineData(new byte[] { 0x38, 0xC3, 0x90, 0x48 }, new int[] { 0x38, 0x00D0, 0x48, })] // [ C3 90 ] is U+00D0 LATIN CAPITAL LETTER ETH
        [InlineData(new byte[] { 0x39, 0xC1, 0xBF, 0x49 }, new int[] { 0x39, 0xFFFD, 0xFFFD, 0x49, })] // [ C1 BF ] is overlong 2-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0x40, 0xE0, 0x9F, 0x50 }, new int[] { 0x40, 0xFFFD, 0xFFFD, 0x50, })] // [ E0 9F ] is overlong 3-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0x41, 0xE0, 0xA0, 0x51 }, new int[] { 0x41, 0xFFFD, 0x51, })] // [ E0 A0 ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0x42, 0xED, 0x9F, 0x52 }, new int[] { 0x42, 0xFFFD, 0x52, })] // [ ED 9F ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0x43, 0xED, 0xBF, 0x53 }, new int[] { 0x43, 0xFFFD, 0xFFFD, 0x53, })] // [ ED BF ] would place us in UTF-16 surrogate range, all surrogate sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0x44, 0xEE, 0x80, 0x54 }, new int[] { 0x44, 0xFFFD, 0x54, })] // [ EE 80 ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0x45, 0xF0, 0x8F, 0x55 }, new int[] { 0x45, 0xFFFD, 0xFFFD, 0x55, })] // [ F0 8F ] is overlong 4-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0x46, 0xF0, 0x90, 0x56 }, new int[] { 0x46, 0xFFFD, 0x56, })] // [ F0 90 ] is valid 2-byte start of 4-byte sequence
        [InlineData(new byte[] { 0x47, 0xF4, 0x90, 0x57 }, new int[] { 0x47, 0xFFFD, 0xFFFD, 0x57, })] // [ F4 90 ] would place us beyond U+10FFFF, all such sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0x48, 0xE2, 0x88, 0xB4, 0x58 }, new int[] { 0x48, 0x2234, 0x58, })] // [ E2 88 B4 ] is U+2234 THEREFORE
        [InlineData(new byte[] { 0x49, 0xE2, 0x88, 0xC0, 0x59 }, new int[] { 0x49, 0xFFFD, 0xFFFD, 0x59, })] // [ E2 88 ] followed by non-continuation byte, maximal invalid subsequence length 2
        [InlineData(new byte[] { 0x50, 0xF0, 0x9F, 0x98, 0x60 }, new int[] { 0x50, 0xFFFD, 0x60, })] // [ F0 9F 98 ] is valid 3-byte start of 4-byte sequence
        [InlineData(new byte[] { 0x51, 0xF0, 0x9F, 0x98, 0x20, 0x61 }, new int[] { 0x51, 0xFFFD, 0x20, 0x61, })] // [ F0 9F 98 ] followed by non-continuation byte, maximal invalid subsequence length 3
        [InlineData(new byte[] { 0x52, 0xF0, 0x9F, 0x98, 0xB2, 0x62 }, new int[] { 0x52, 0x1F632, 0x62, })] // [ F0 9F 98 B2 ] is U+1F632 ASTONISHED FACE
        public static void EnumerateRunePositions_Battery8(byte[] bytes, int[] expected)
        {
            List<int> enumeratedValues = new List<int>();

            RunePosition.Utf8Enumerator enumerator = RunePosition.EnumerateUtf8(bytes).GetEnumerator();
            Assert.Equal(default, enumerator.Current);

            int expectedIndex = 0;
            while (enumerator.MoveNext())
            {
                bool wasReplaced = Rune.DecodeFromUtf8(bytes.AsSpan(expectedIndex), out Rune expectedRune, out int charsConsumed) != OperationStatus.Done;
                RunePosition runePosition = enumerator.Current;

                RunePosition_TestProps(expectedRune, expectedIndex, charsConsumed, wasReplaced, runePosition);

                expectedIndex += charsConsumed;
                enumeratedValues.Add(runePosition.Rune.Value);
            }
            Assert.Equal(default, enumerator.Current);

            Assert.Equal(expected, enumeratedValues.ToArray());
        }

        [Fact]
        public static void EnumerateRunePositions_DoesNotReadPastEndOfSpan()
        {
            // As an optimization, reading scalars from a string *may* read past the end of the string
            // to the terminating null. This optimization is invalid for arbitrary spans, so this test
            // ensures that we're not performing this optimization here.

            {
                ReadOnlySpan<char> span = "xy\U0002B754z".AsSpan(1, 2); // well-formed string, but span splits surrogate pair

                List<int> enumeratedValues = new List<int>();
                foreach (RunePosition runePosition in RunePosition.EnumerateUtf16(span))
                {
                    enumeratedValues.Add(runePosition.Rune.Value);
                }
                Assert.Equal(new int[] { 'y', '\uFFFD' }, enumeratedValues.ToArray());
            }

            {
                ReadOnlySpan<byte> span = "xy\U0002B754z"u8.Slice(1, 2); // well-formed string, but span splits surrogate pair

                List<int> enumeratedValues = new List<int>();
                foreach (RunePosition runePosition in RunePosition.EnumerateUtf8(span))
                {
                    enumeratedValues.Add(runePosition.Rune.Value);
                }
                Assert.Equal(new int[] { 'y', '\uFFFD' }, enumeratedValues.ToArray());
            }
        }
    }
}
