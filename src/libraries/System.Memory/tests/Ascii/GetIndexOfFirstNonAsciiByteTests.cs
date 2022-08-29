// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class GetIndexOfFirstNonAsciiByteTests
    {
        private static byte GetNextValidAsciiByte() => (byte)Random.Shared.Next(0, 127 + 1);
        private static byte GetNextInvalidAsciiByte() => (byte)Random.Shared.Next(128, 255 + 1);

        [Fact]
        public void EmptyInput_IndexNotFound() => Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte>.Empty));

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
        public void AllAscii_IndexNotFound(byte[] buffer) => Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiByte(buffer));

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
        public void NonAscii_IndexFound(int expectedIndex, byte[] buffer) => Assert.Equal(expectedIndex, Ascii.GetIndexOfFirstNonAsciiByte(buffer));
    }
}
