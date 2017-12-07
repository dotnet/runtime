// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void LastIndexOfSequenceMatchAtStart()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 5, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5, 1, 77 });
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMultipleMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 1 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 2, 3 });
            int index = span.LastIndexOf(value);
            Assert.Equal(7, index);
        }

        [Fact]
        public static void LastIndexOfSequenceRestart()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 8, 9, 77, 0, 1 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 77, 77, 88 });
            int index = span.LastIndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNoMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 77, 77, 88, 99 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNotEvenAHeadMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 100, 77, 88, 99 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtVeryEnd()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 3, 4, 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void LastIndexOfSequenceJustPastVeryEnd()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 3, 4, 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthValue()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(Array.Empty<int>());
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthSpan()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(Array.Empty<int>());
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 1, 2, 3 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValue()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 2 });
            int index = span.LastIndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueAtVeryEnd()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueMultipleTimes()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 5, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueJustPasttVeryEnd()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtStart_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "5", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5", "1", "77" });
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMultipleMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "1", "2", "3", "1", "2", "3", "1", "2", "3" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "2", "3" });
            int index = span.LastIndexOf(value);
            Assert.Equal(7, index);
        }

        [Fact]
        public static void LastIndexOfSequenceRestart_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "8", "9", "77", "0", "1" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "77", "77", "88" });
            int index = span.LastIndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNoMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "77", "77", "88", "99" });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNotEvenAHeadMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "100", "77", "88", "99" });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtVeryEnd_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "3", "4", "5" });
            int index = span.LastIndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void LastIndexOfSequenceJustPastVeryEnd_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "3", "4", "5" });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthValue_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(Array.Empty<string>());
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthSpan_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(Array.Empty<string>());
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "1", "2", "3" });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValue_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "2" });
            int index = span.LastIndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueAtVeryEnd_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5" });
            int index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueJustPasttVeryEnd_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5" });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }
    }
}
