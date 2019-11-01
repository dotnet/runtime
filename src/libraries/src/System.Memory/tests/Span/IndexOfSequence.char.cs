// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

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
            // A zero-length value is always "found" at the start of the span.
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' });
            Span<char> value = new Span<char>(new char[] { '2' });
            int index = span.IndexOf(value);
            Assert.Equal(2, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueAtVeryEnd_Char()
        {
            // A zero-length value is always "found" at the start of the span.
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' });
            Span<char> value = new Span<char>(new char[] { '5' });
            int index = span.IndexOf(value);
            Assert.Equal(5, index);
        }

        [Fact]
        public static void IndexOfSequenceLengthOneValueJustPasttVeryEnd_Char()
        {
            // A zero-length value is always "found" at the start of the span.
            Span<char> span = new Span<char>(new char[] { '0', '1', '2', '3', '4', '5' }, 0, 5);
            Span<char> value = new Span<char>(new char[] { '5' });
            int index = span.IndexOf(value);
            Assert.Equal(-1, index);
        }
    }
}
