// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static class GetIndexOfFirstNonAsciiByteTests
    {
        private static byte GetNextValidAsciiByte() => (byte)Random.Shared.Next(0, 127 + 1);
        private static byte GetNextInvalidAsciiByte() => (byte)Random.Shared.Next(128, 255 + 1);

        [Fact]
        public static void EmptyInput_IndexNotFound()
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte>.Empty));
            Assert.True(Ascii.IsAscii(ReadOnlySpan<byte>.Empty));
        }

        private static int[] BufferLengths = new[] {
            1,
            Vector128<byte>.Count - 1,
            Vector128<byte>.Count,
            Vector128<byte>.Count + 1,
            Vector256<byte>.Count - 1,
            Vector256<byte>.Count,
            Vector256<byte>.Count + 1 };

        public static IEnumerable<object[]> AsciiOnlyBuffers
        {
            get
            {
                yield return new object[] { new byte[] { GetNextValidAsciiByte() } };

                foreach (int length in BufferLengths)
                {
                    yield return new object[] { Enumerable.Repeat(GetNextValidAsciiByte(), length).ToArray() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AsciiOnlyBuffers))]
        public static void AllAscii_IndexNotFound(byte[] buffer)
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(buffer));
            Assert.True(Ascii.IsAscii(buffer));
            Assert.All(buffer, character => Assert.True(Ascii.IsAscii(character)));
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

                static byte[] Create(int length, int index)
                {
                    byte[] buffer = Enumerable.Repeat(GetNextValidAsciiByte(), length).ToArray();
                    buffer[index] = GetNextInvalidAsciiByte();
                    return buffer;
                }
            }
        }

        [Theory]
        [MemberData(nameof(ContainingNonAsciiCharactersBuffers))]
        public static void NonAscii_IndexFound(int expectedIndex, byte[] buffer)
        {
            Assert.Equal(expectedIndex, Ascii.GetIndexOfFirstNonAsciiByte(buffer));
            Assert.False(Ascii.IsAscii(buffer));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(i != expectedIndex, Ascii.IsAscii(buffer[i]));
            }
        }

        [Fact]
        public static void Vector128InnerLoop()
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

                // Two vectors have offsets 0 .. 31. We'll go backward to avoid having to
                // re-clear the vector every time.

                for (int i = 2 * Vector128<byte>.Count - 1; i >= 0; i--)
                {
                    bytes[100 + i * 13] = 0x80; // 13 is relatively prime to 32, so it ensures all possible positions are hit
                    Assert.Equal(100 + i * 13, Ascii.GetIndexOfFirstNonAsciiByte(bytes));
                }
            }
        }

        [Fact]
        public static void Boundaries()
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
                }

                // Then, try it with non-ASCII bytes.

                for (int i = bytes.Length; i >= 1; i--)
                {
                    bytes[i - 1] = 0x80; // set non-ASCII
                    Assert.Equal(i - 1, Ascii.GetIndexOfFirstNonAsciiByte(bytes.Slice(0, i)));
                }
            }
        }
    }
}
