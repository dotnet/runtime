// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class GetIndexOfFirstNonAsciiCharTests
    {
        private static char GetNextValidAsciiChar() => (char)Random.Shared.Next(0, 127 + 1);
        private static char GetNextInvalidAsciiChar() => (char)Random.Shared.Next(128, ushort.MaxValue + 1);

        [Fact]
        public void EmptyInput_IndexNotFound()
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(ReadOnlySpan<char>.Empty));
            Assert.True(Ascii.IsAscii(ReadOnlySpan<char>.Empty));
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
        public void AllAscii_IndexNotFound(char[] buffer)
        {
            Assert.Equal(-1, Ascii.GetIndexOfFirstNonAsciiChar(buffer));
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
        public void NonAscii_IndexFound(int expectedIndex, char[] buffer)
        {
            Assert.Equal(expectedIndex, Ascii.GetIndexOfFirstNonAsciiChar(buffer));
            Assert.False(Ascii.IsAscii(buffer));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(i != expectedIndex, Ascii.IsAscii(buffer[i]));
            }
        }
    }
}
