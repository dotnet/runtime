// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class ConvertToBase64CharArrayTests
    {
        [Fact]
        public static void ValidOffsetIn()
        {
            string input = "test";
            byte[] inputBytes = Convert.FromBase64String(input);
            char[] resultChars = new char[4];
            int fillCharCount = Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length - 1, resultChars, 0);
            Assert.Equal(input.Length, fillCharCount);
        }

        [Fact]
        public static void ShortInputArray()
        {
            // Regression test for bug where a short input array caused an exception to be thrown
            byte[] inputBuffer = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            char[] ouputBuffer = new char[4];
            Convert.ToBase64CharArray(inputBuffer, 0, 3, ouputBuffer, 0);
            Convert.ToBase64CharArray(inputBuffer, 0, 2, ouputBuffer, 0);
        }

        [Fact]
        public static void ValidOffsetOut()
        {
            // Regression test for bug where offsetOut parameter was ignored
            char[] outputBuffer = "........".ToCharArray();
            byte[] inputBuffer = new byte[6];
            for (int i = 0; i < inputBuffer.Length; inputBuffer[i] = (byte)i++) ;

            // Convert the first half of the byte array, write to the first half of the char array
            int c = Convert.ToBase64CharArray(inputBuffer, 0, 3, outputBuffer, 0);
            Assert.Equal(4, c);
            Assert.Equal("AAEC....", new String(outputBuffer));

            // Convert the second half of the byte array, write to the second half of the char array
            c = Convert.ToBase64CharArray(inputBuffer, 3, 3, outputBuffer, 4);
            Assert.Equal(4, c);
            Assert.Equal("AAECAwQF", new String(outputBuffer));
        }

        [Fact]
        public static void InvalidInputBuffer()
        {
            Assert.Throws<ArgumentNullException>(() => Convert.ToBase64CharArray(null, 0, 1, new char[1], 0));
        }

        [Fact]
        public static void InvalidOutputBuffer()
        {
            char[] inputChars = "test".ToCharArray();
            byte[] inputBytes = Convert.FromBase64CharArray(inputChars, 0, inputChars.Length);
            Assert.Throws<ArgumentNullException>(() => Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length, null, 0));
        }

        [Fact]
        public static void InvalidOffsetIn()
        {
            char[] inputChars = "test".ToCharArray();
            byte[] inputBytes = Convert.FromBase64CharArray(inputChars, 0, inputChars.Length);
            char[] outputBuffer = new char[4];

            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, -1, inputBytes.Length, outputBuffer, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, inputBytes.Length, inputBytes.Length, outputBuffer, 0));
        }

        [Fact]
        public static void InvalidOffsetOut()
        {
            char[] inputChars = "test".ToCharArray();
            byte[] inputBytes = Convert.FromBase64CharArray(inputChars, 0, inputChars.Length);
            char[] outputBuffer = new char[4];

            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length, outputBuffer, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length, outputBuffer, 1));
        }

        [Fact]
        public static void InvalidInputLength()
        {
            char[] inputChars = "test".ToCharArray();
            byte[] inputBytes = Convert.FromBase64CharArray(inputChars, 0, inputChars.Length);
            char[] outputBuffer = new char[4];

            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, 0, -1, outputBuffer, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length + 1, outputBuffer, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToBase64CharArray(inputBytes, 1, inputBytes.Length, outputBuffer, 0));
        }
    }
}
