// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void IndexOfSequenceMatchAtStart()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 5, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5, 1, 77 });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 77, 77, 88 });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 77, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 100, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(Array.Empty<int>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(Array.Empty<int>());
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 1, 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 2 });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<int> value = new ReadOnlySpan<int>(new int[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtStart_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "5", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5", "1", "77" });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "1", "2", "3", "1", "2", "3", "1", "2", "3" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "2", "3" });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "77", "77", "88" });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "77", "77", "88", "99" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "100", "77", "88", "99" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "3", "4", "5" });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "3", "4", "5" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "77", "2", "3", "77", "77", "4", "5", "77", "77", "77", "88", "6", "6", "77", "77", "88", "9" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(Array.Empty<string>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(Array.Empty<string>());
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "1", "2", "3" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "2" });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" });
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5" });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_String()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(new string[] { "0", "1", "2", "3", "4", "5" }, 0, 5);
            ReadOnlySpan<string> value = new ReadOnlySpan<string>(new string[] { "5" });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }
    }
}
