// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using Xunit;

namespace System.Text.Tests
{
    public static class IsValidCharTests
    {
        private static char GetNextValidAsciiChar() => (char)Random.Shared.Next(0, 127 + 1);
        private static char GetNextInvalidAsciiChar() => (char)Random.Shared.Next(128, ushort.MaxValue + 1);

        [Fact]
        public static void EmptyInput_ReturnsTrue()
        {
            Assert.True(Ascii.IsValid(ReadOnlySpan<char>.Empty));
        }

        private static int[] BufferLengths = new[] {
            1,
            Vector128<short>.Count - 1,
            Vector128<short>.Count,
            Vector128<short>.Count + 1,
            Vector256<short>.Count - 1,
            Vector256<short>.Count,
            Vector256<short>.Count + 1 };

        public static IEnumerable<object[]> AsciiOnlyBuffers
        {
            get
            {
                yield return new object[] { new char[] { GetNextValidAsciiChar() } };

                foreach (int length in BufferLengths)
                {
                    yield return new object[] { Enumerable.Repeat(GetNextValidAsciiChar(), length).ToArray() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AsciiOnlyBuffers))]
        public static void AllAscii_ReturnsTrue(char[] buffer)
        {
            Assert.True(Ascii.IsValid(buffer));
            Assert.All(buffer, character => Assert.True(Ascii.IsValid(character)));
        }

        public static IEnumerable<object[]> ContainingNonAsciiCharactersBuffers
        {
            get
            {
                foreach (int length in BufferLengths)
                {
                    for (int index = 0; index < length; index++)
                    {
                        yield return new object[] { index, Create(length, index) };
                    }
                }

                static char[] Create(int length, int index)
                {
                    char[] buffer = Enumerable.Repeat(GetNextValidAsciiChar(), length).ToArray();
                    buffer[index] = GetNextInvalidAsciiChar();
                    return buffer;
                }
            }
        }

        [Theory]
        [MemberData(nameof(ContainingNonAsciiCharactersBuffers))]
        public static void NonAsciiAtGivenIndex(int nonAsciiIndex, char[] buffer)
        {
            Assert.False(Ascii.IsValid(buffer));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(i != nonAsciiIndex, Ascii.IsValid(buffer[i]));
            }
        }

        [Fact]
        public static void Vector128InnerLoop()
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

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * Vector128<byte>.Count - 1; i >= 0; i--)
                {
                    chars[100 + i * 13] = '\u0123'; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.False(Ascii.IsValid(chars));
                }
            }
        }

        [Fact]
        public static void Boundaries()
        {
            // The purpose of this test is to make sure we're hitting all of the vectorized
            // and draining logic correctly both in the SSE2 and in the non-SSE2 enlightened
            // code paths. We shouldn't be reading beyond the boundaries we were given.
            //
            // The 5 * Vector test should make sure that we're exercising all possible
            // code paths across both implementations. The sizeof(char) is because we're
            // specifying element count, but underlying implementation reinterpret casts to bytes.
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
                    Assert.True(Ascii.IsValid(chars.Slice(0, i)));
                }

                // Then, try it with non-ASCII bytes.

                for (int i = chars.Length; i >= 1; i--)
                {
                    chars[i - 1] = '\u0123'; // set non-ASCII
                    Assert.False(Ascii.IsValid(chars.Slice(0, i)));
                }
            }
        }
    }
}
