// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToBase64StringTests
    {
        [Fact]
        public static void KnownByteSequence()
        {
            var inputBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                inputBytes[i] = (byte)(i + 5);

            // The sequence of bits for this byte array is
            // 00000101000001100000011100001000
            // Encoding adds 16 bits of trailing bits to make this a multiple of 24 bits.
            // |        +         +         +         +    
            // 000001010000011000000111000010000000000000000000
            // which is, (Interesting, how do we distinguish between '=' and 'A'?)
            // 000001 010000 011000 000111 000010 000000 000000 000000
            // B      Q      Y      H      C      A      =      =

            Assert.Equal("BQYHCA==", Convert.ToBase64String(inputBytes));
        }

        [Fact]
        public static void ZeroLength()
        {
            byte[] inputBytes = Convert.FromBase64String("test");
            Assert.Equal(string.Empty, Convert.ToBase64String(inputBytes, 0, 0));
        }

        [Fact]
        public static void InvalidInputBuffer()
        {
            Assert.Throws<ArgumentNullException>(() => Convert.ToBase64String(null));
            Assert.Throws<ArgumentNullException>(() => Convert.ToBase64String(null, 0, 0));
        }

        [Fact]
        public static void InvalidOffset()
        {
            byte[] inputBytes = Convert.FromBase64String("test");
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64String(inputBytes, -1, inputBytes.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64String(inputBytes, inputBytes.Length, inputBytes.Length));
        }

        [Fact]
        public static void InvalidLength()
        {
            byte[] inputBytes = Convert.FromBase64String("test");
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64String(inputBytes, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64String(inputBytes, 0, inputBytes.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64String(inputBytes, 1, inputBytes.Length));
        }
    }
}
