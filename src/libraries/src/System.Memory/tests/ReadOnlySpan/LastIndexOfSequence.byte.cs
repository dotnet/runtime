// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void LastIndexOfSequenceMatchAtStart_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 5, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5, 1, 77 });
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMultipleMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 1 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 2, 3 });
            int index = span.LastIndexOf(value);
            Assert.Equal(7, index);
        }

        [Fact]
        public static void LastIndexOfSequenceRestart_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 8, 9, 77, 0, 1 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 77, 77, 88 });
            int index = span.LastIndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNoMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 77, 77, 88, 99 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceNotEvenAHeadMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 100, 77, 88, 99 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceMatchAtVeryEnd_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 3, 4, 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void LastIndexOfSequenceJustPastVeryEnd_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 3, 4, 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthValue_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(Array.Empty<byte>());
            int index = span.LastIndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void LastIndexOfSequenceZeroLengthSpan_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValue_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 2 });
            int index = span.LastIndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueAtVeryEnd_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueMultipleTimes_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 5, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void LastIndexOfSequenceLengthOneValueJustPasttVeryEnd_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5 });
            int index = span.LastIndexOf(value);
            Assert.Equal(-1, index);
        }
    }
}
