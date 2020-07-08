// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void IndexOfSequenceMatchAtStart()
        {
            Span<int> span = new Span<int>(new int[] { 5, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            Span<int> value = new Span<int>(new int[] { 5, 1, 77 });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch()
        {
            Span<int> span = new Span<int>(new int[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 });
            Span<int> value = new Span<int>(new int[] { 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            Span<int> value = new Span<int>(new int[] { 77, 77, 88 });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            Span<int> value = new Span<int>(new int[] { 77, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            Span<int> value = new Span<int>(new int[] { 100, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            Span<int> value = new Span<int>(new int[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            Span<int> value = new Span<int>(new int[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue()
        {
            // A zero-length value is always "found" at the start of the span.
            Span<int> span = new Span<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            Span<int> value = new Span<int>(Array.Empty<int>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan()
        {
            Span<int> span = new Span<int>(Array.Empty<int>());
            Span<int> value = new Span<int>(new int[] { 1, 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            Span<int> value = new Span<int>(new int[] { 2 });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            Span<int> value = new Span<int>(new int[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd()
        {
            Span<int> span = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            Span<int> value = new Span<int>(new int[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtStart_String()
        {
            Span<string> span = new Span<string>(new string[] { "5", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            Span<string> value = new Span<string>(new string[] { "5", "1", "77" });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch_String()
        {
            Span<string> span = new Span<string>(new string[] { "1", "2", "3", "1", "2", "3", "1", "2", "3" });
            Span<string> value = new Span<string>(new string[] { "2", "3" });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            Span<string> value = new Span<string>(new string[] { "77", "77", "88" });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            Span<string> value = new Span<string>(new string[] { "77", "77", "88", "99" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            Span<string> value = new Span<string>(new string[] { "100", "77", "88", "99" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            Span<string> value = new Span<string>(new string[] { "3", "4", "5" });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            Span<string> value = new Span<string>(new string[] { "3", "4", "5" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue_String()
        {
            // A zero-length value is always "found" at the start of the span.
            Span<string> span = new Span<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            Span<string> value = new Span<string>(Array.Empty<string>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan_String()
        {
            Span<string> span = new Span<string>(Array.Empty<string>());
            Span<string> value = new Span<string>(new string[] { "1", "2", "3" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            Span<string> value = new Span<string>(new string[] { "2" });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            Span<string> value = new Span<string>(new string[] { "5" });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_String()
        {
            Span<string> span = new Span<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            Span<string> value = new Span<string>(new string[] { "5" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Theory]
        [MemberData(nameof(TestHelpers.IndexOfNullSequenceData), MemberType = typeof(TestHelpers))]
        public static void IndexOfNullSequence_String(string[] spanInput, string[] searchInput, int expected)
        {
            Span<string> theStrings = spanInput;
            Assert.Equal(expected, theStrings.IndexOf(searchInput));
            Assert.Equal(expected, theStrings.IndexOf((ReadOnlySpan<string>)searchInput));
        }
    }
}
