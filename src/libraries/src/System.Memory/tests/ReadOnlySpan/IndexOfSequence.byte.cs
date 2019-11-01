// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void IndexOfSequenceMatchAtStart_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 5, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5, 1, 77 });
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceMultipleMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(1, index);
        }

        [Fact]
        public static void IndexOfSequenceRestart_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 77, 77, 88 });
            int index = span.IndexOf(value);
            Assert.Equal(10, index);
        }

        [Fact]
        public static void IndexOfSequenceNoMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 77, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceNotEvenAHeadMatch_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 100, 77, 88, 99 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceMatchAtVeryEnd_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(3, index);
        }

        [Fact]
        public static void IndexOfSequenceJustPastVeryEnd_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 3, 4, 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthValue_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 77, 2, 3, 77, 77, 4, 5, 77, 77, 77, 88, 6, 6, 77, 77, 88, 9 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(Array.Empty<byte>());
            int index = span.IndexOf(value);
            Assert.Equal(0, index);
        }

        [Fact]
        public static void IndexOfSequenceZeroLengthSpan_Byte()
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(Array.Empty<byte>());
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValue_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 2 });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 });
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_Byte()
        {
            // A zero-length value is always "found" at the start of the span.
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0, 1, 2, 3, 4, 5 }, 0, 5);
            ReadOnlySpan<byte> value = new ReadOnlySpan<byte>(new byte[] { 5 });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }
    }
}
