// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        [Theory]
        [InlineData(0x0000, true)]
        [InlineData(0x0001, true)]
        [InlineData(0x0010, true)]
        [InlineData(0x007F, true)]
        [InlineData(0x0080, false)]
        [InlineData(0x00FF, false)]
        [InlineData(0x0800, false)]
        [InlineData(0x7F00, false)]
        [InlineData(0x8000, false)]
        [InlineData(0xFFFF, false)]
        public void IsAscii_SingleValue(uint value, bool expected)
        {
            // test char
            Assert.Equal(expected, Ascii.IsAscii(checked((char)value)));

            // test byte
            if (value <= byte.MaxValue)
            {
                Assert.Equal(expected, Ascii.IsAscii((byte)value));
            }
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiByte_EmptyInput_NullReference()
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte>.Empty));
            Assert.True(Ascii.IsAscii(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiByte_EmptyInput_NonNullReference()
        {
            using BoundedMemory<byte> mem = BoundedMemory.Allocate<byte>(0);
            mem.MakeReadonly();

            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(mem.Span));
            Assert.True(Ascii.IsAscii(mem.Span));
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiByte_Vector128InnerLoop()
        {
            // The purpose of this test is to make sure we're identifying the correct
            // vector (of the two that we're reading simultaneously) when performing
            // the final ASCII drain at the end of the method once we've broken out
            // of the inner loop.

            using (BoundedMemory<byte> mem = BoundedMemory.Allocate<byte>(1024))
            {
                Span<byte> bytes = mem.Span;

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] &= 0x7F; // make sure each byte (of the pre-populated random data) is ASCII
                }

                Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(bytes));
                Assert.True(Ascii.IsAscii(bytes));

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * SizeOfVector128 - 1; i >= 0; i--)
                {
                    bytes[100 + i * 13] = 0x80; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, Ascii.GetIndexOfFirstNonAsciiByte(bytes));
                    Assert.False(Ascii.IsAscii(bytes));
                }
            }
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiByte_Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.

            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations.
            using (BoundedMemory<byte> mem = BoundedMemory.Allocate<byte>(5 * Vector<byte>.Count))
            {
                Span<byte> bytes = mem.Span;

                // First, try it with all-ASCII buffers.

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] &= 0x7F; // make sure each byte (of the pre-populated random data) is ASCII
                }

                for (int i = bytes.Length; i >= 0; i--)
                {
                    Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(bytes.Slice(0, i)));
                    Assert.True(Ascii.IsAscii(bytes));
                }

                // Then, try it with non-ASCII bytes.

                for (int i = bytes.Length; i >= 1; i--)
                {
                    bytes[i - 1] = 0x80; // set non-ASCII
                    Assert.Equal(i - 1, Ascii.GetIndexOfFirstNonAsciiByte(bytes.Slice(0, i)));
                    Assert.False(Ascii.IsAscii(bytes));
                }
            }
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiChar_EmptyInput_NullReference()
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(ReadOnlySpan<char>.Empty));
            Assert.True(Ascii.IsAscii(ReadOnlySpan<char>.Empty));
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiChar_EmptyInput_NonNullReference()
        {
            using BoundedMemory<char> mem = BoundedMemory.Allocate<char>(0);
            mem.MakeReadonly();

            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(mem.Span));
            Assert.True(Ascii.IsAscii(mem.Span));
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiChar_Vector128InnerLoop()
        {
            // The purpose of this test is to make sure we're identifying the correct
            // vector (of the two that we're reading simultaneously) when performing
            // the final ASCII drain at the end of the method once we've broken out
            // of the inner loop.
            //
            // Use U+0123 instead of U+0080 for this test because if our implementation
            // uses pminuw / pmovmskb incorrectly, U+0123 will incorrectly show up as ASCII,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(1024))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u007F'; // make sure each char (of the pre-populated random data) is ASCII
                }

                Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(chars));
                Assert.True(Ascii.IsAscii(chars));

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * SizeOfVector128 - 1; i >= 0; i--)
                {
                    chars[100 + i * 13] = '\u0123'; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, Ascii.GetIndexOfFirstNonAsciiChar(chars));
                    Assert.False(Ascii.IsAscii(chars));
                }
            }
        }

        [Fact]
        public void GetIndexOfFirstNonAsciiChar_Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.
            //
            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations. The sizeof(char) is because we're
            // specifying element count, but underlying implementation reintepret casts to bytes.
            //
            // Use U+0123 instead of U+0080 for this test because if our implementation
            // uses pminuw / pmovmskb incorrectly, U+0123 will incorrectly show up as ASCII,
            // causing our test to produce a false negative.

            using (BoundedMemory<char> mem = BoundedMemory.Allocate<char>(5 * Vector<byte>.Count / sizeof(char)))
            {
                Span<char> chars = mem.Span;

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] &= '\u007F'; // make sure each char (of the pre-populated random data) is ASCII
                }

                for (int i = chars.Length; i >= 0; i--)
                {
                    Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(chars.Slice(0, i)));
                    Assert.True(Ascii.IsAscii(chars));
                }

                // Then, try it with non-ASCII bytes.

                for (int i = chars.Length; i >= 1; i--)
                {
                    chars[i - 1] = '\u0123'; // set non-ASCII
                    Assert.Equal(i - 1, Ascii.GetIndexOfFirstNonAsciiChar(chars.Slice(0, i)));
                    Assert.False(Ascii.IsAscii(chars));
                }
            }
        }
    }
}
