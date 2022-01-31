// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void IndexOfSequenceMatchAtStart_Char()
        {
            Span<char> span = new Span<char>(new char[] { '5', '1', '7', '2', '3', '7', '7', '4', '5', '7', '7', '7', '8', '6', '6', '7', '7', '8', '9' });
            Span<char> value = new Span<char>(new char[] { '5', '1', '7' });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch_Char()
        {
            Span<char> span = new Span<char>(new char[] { '1', '2', '3', '1', '2', '3', '1', '2', '3' });
            Span<char> value = new Span<char>(new char[] { '2', '3' });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart_Char()
        {
            Span<char> span = new Span<char>(new char[] { '5', '1', '7', '2', '3', '7', '7', '4', '5', '7', '7', '7', '8', '6', '6', '7', '7', '8', '9' });
            Span<char> value = new Span<char>(new char[] { '7', '7', '8' });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '7', '2', '3', '7', '7', '4', '5', '7', '7', '7', '8', '6', '6', '7', '7', '8', '9' });
            Span<char> value = new Span<char>(new char[] { '7', '7', '8', 'X' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '7', '2', '3', '7', '7', '4', '5', '7', '7', '7', '8', '6', '6', '7', '7', '8', '9' });
            Span<char> value = new Span<char>(new char[] { 'X', '7', '8', '9' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' });
            Span<char> value = new Span<char>(new char[] { '3', '4', '5' });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            Span<char> value = new Span<char>(new char[] { '3', '4', '5' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue_Char()
        {
            // A zero-length value is always "found" at the start of the span.
            Span<char> span = new Span<char>(new char[] { '0', '1', '7', '2', '3', '7', '7', '4', '5', '7', '7', '7', '8', '6', '6', '7', '7', '8', '9' });
            Span<char> value = new Span<char>(Array.Empty<char>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan_Char()
        {
            Span<char> span = new Span<char>(Array.Empty<char>());
            Span<char> value = new Span<char>(new char[] { '1', '2', '3' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' });
            Span<char> value = new Span<char>(new char[] { '2' });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' });
            Span<char> value = new Span<char>(new char[] { '5' });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_Char()
        {
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            Span<char> value = new Span<char>(new char[] { '5' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        public static IEnumerable<object[]> IndexOfSubSeqData_Char()
        {
            // searchSpace, value, expected IndexOf value, expected LastIndexOf value
            yield return new object[] { "11111", "111", 0, 2 };
            yield return new object[] { "1111111111", "1x1", -1, -1 };
            yield return new object[] { "1111111111", "111", 0, 7 };
            yield return new object[] { "11111111111x12111", "1x121", 10, 10 };
            yield return new object[] { "11111111111x12111", "11121", -1, -1 };
            yield return new object[] { "1111111111x121111", "11121", -1, -1 };
            yield return new object[] { "11111x12111111111", "11121", -1, -1 };
            yield return new object[] { "11111111111x12111", "1x211", -1, -1 };
            yield return new object[] { "11111111111x12111", "11211", -1, -1 };
            yield return new object[] { "1111111111x121111", "11211", -1, -1 };
            yield return new object[] { "11111x12111111111", "11211", -1, -1 };
            yield return new object[] { "11111111111x12111", "12111", 12, 12 };
            yield return new object[] { "1111111111x121111", "12111", 11, 11 };
            yield return new object[] { "11111x12111111111", "12111", 6, 6 };
            yield return new object[] { "1111x1211111111111x12", "11121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "11121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "111121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "1111121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "1111121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "1111121", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "1211211", -1, -1 };
            yield return new object[] { "1111x1211111111111x12", "1211111", 5, 5 };
            yield return new object[] { "1111x1211111111111x12", "1211111", 5, 5 };
            yield return new object[] { "1111x1211111111111x12", "1211111", 5, 5 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111", 0, 44 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111", 0, 43 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111", 7, 42 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111", 7, 41 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111111", 7, 11 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111111", 7, 10 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111111", 7, 9 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111111111", 7, 7 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1211", 5, 48 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11121", 44, 44 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "121111", 5, 19 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "12111211", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111111", 7, 11 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1121121111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111211111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111111211111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1121111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11122111112111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "1111111211111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "111211111111111111", -1, -1 };
            yield return new object[] { "1111x1211111111111x12111122131221221211221111112121121", "11111211111121111111", -1, -1 };
            yield return new object[] { "жжжжжжжжжжжжжж", "жжж", 0, 11 };
            yield return new object[] { "жжжжжжжжжжжжжжжжжжжжжжжжжжжж", "ж0ж", -1, -1 };
            yield return new object[] { "жжжжжаааааааааааааааччччс", "ччччс", 20, 20 };
            yield return new object[] { "жжжжжаааааааааааааааччччсссссссчччч", "чччч", 20, 31 };
            yield return new object[] { "жжжжжжжжжжжжжжжжжжжжжжжжжжжж", "1112", -1, -1 };
            yield return new object[] { "0уза0оцущ0оаз0щцуоазщцуо0азщцуоазщоц0узозцуоазуоцз0щауцз0оазцо", "0оаз0", 9, 9 };
            yield return new object[] { "abababababababababababababababbc", "bb", 29, 29 };
            yield return new object[] { "abababababababababababababababb", "bb", 29, 29 };
            yield return new object[] { "abababababababababababababababbc", "bb", 29, 29 };
            yield return new object[] { "babababababababababababababababc", "bb", -1, -1 };
            yield return new object[] { "abababababababababababababababbb", "bbb", 29, 29 };
            yield return new object[] { "abababababababababababababababbbc", "bbb", 29, 29 };
            yield return new object[] { "bbbbabababababababababababababababc", "bbb", 0, 1 };
            yield return new object[] { "abababababababababababababababbc", "aa", -1, -1 };
            yield return new object[] { "abababababababababababababababb", "aa", -1, -1 };
            yield return new object[] { "abababababababababababababababbc", "aa", -1, -1 };
            yield return new object[] { "babababababababababababababababc", "aa", -1, -1 };
            yield return new object[] { "abababababababababababababababbb", "aaa", -1, -1 };
            yield return new object[] { "abababababababababababababababbbc", "aaa", -1, -1 };
            yield return new object[] { "bbbbabababababababababababababababc", "aaa", -1, -1 };
            yield return new object[] { "ababababababababababababababababbc", "abaa", -1, -1 };
            yield return new object[] { "babbbabababababababababababababababc", "babb", 0, 0 };
            yield return new object[] { "babbbabababababababababababababababc", "сaсс", -1, -1 };
            yield return new object[] { "babbbbbbbbbbbbb", "babbbbbbbbbbbb", 0, 0 };
            yield return new object[] { "babbbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbbbbbbb", 0, 15 };
            yield return new object[] { "babbbbbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbbbbbbb", 0, 17 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbbbbbbb", 18, 32 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "bbbbbbbbbbbbb", 20, 20 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", 0, 0 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbb", 0, 0 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbbb", -1, -1 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", 0, 0 };
            yield return new object[] { "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbb", "babbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbb", 0, 0 };
            yield return new object[] { "xxxxxxxxxxxxxxbabbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbbxxxxxxxxxxxxxxx", "xxxxxxxxxxxxxxx", 60, 60 };
            yield return new object[] { "xxxxxxxxxxxxxxxbabbbbbbxbbbbbbbbbbabbbbbbbbbbbbbabbbbbbbbbbbbxxxxxxxxxxxxxx", "xxxxxxxxxxxxxxx", 0, 0 };
        }

        [Theory]
        [MemberData(nameof(IndexOfSubSeqData_Char))]
        public static void ValueStartsAndEndsWithTheSameChars(string searchSpace, string value, int expectedIndexOfValue, int expectedLastIndexOfValue)
        {
            Assert.Equal(expectedIndexOfValue, searchSpace.AsSpan().IndexOf(value));
            Assert.Equal(expectedLastIndexOfValue, searchSpace.AsSpan().LastIndexOf(value));
        }
    }
}
