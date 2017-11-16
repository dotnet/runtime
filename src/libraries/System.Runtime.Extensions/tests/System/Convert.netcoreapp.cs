// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Tests
{
    public partial class ConvertTests
    {
        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[] { 5, 6, 7, 8 }, "BQYHCA==")]
        public void ToBase64String_Span_ProducesExpectedOutput(byte[] input, string expected)
        {
            Assert.Equal(expected, Convert.ToBase64String(input.AsReadOnlySpan()));
            Assert.Equal(expected, Convert.ToBase64String(input.AsReadOnlySpan(), Base64FormattingOptions.None));
            Assert.Equal(expected, Convert.ToBase64String(input.AsReadOnlySpan(), Base64FormattingOptions.InsertLineBreaks));
        }

        [Fact]
        public void ToBase64String_Span_LongWithOptions_ProducesExpectedOutput()
        {
            byte[] input = Enumerable.Range(0, 120).Select(i => (byte)i).ToArray();

            Assert.Equal(
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4" +
                "OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3Bx" +
                "cnN0dXZ3",
                Convert.ToBase64String(input));

            Assert.Equal(
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4" +
                "OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3Bx" +
                "cnN0dXZ3",
                Convert.ToBase64String(input, Base64FormattingOptions.None));

            Assert.Equal(
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4\r\n" +
                "OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3Bx\r\n" +
                "cnN0dXZ3",
                Convert.ToBase64String(input, Base64FormattingOptions.InsertLineBreaks));
        }

        [Theory]
        [InlineData((Base64FormattingOptions)(-1))]
        [InlineData((Base64FormattingOptions)(2))]
        public void ToBase64String_Span_InvalidOptions_Throws(Base64FormattingOptions invalidOption)
        {
            AssertExtensions.Throws<ArgumentException>("options", () => Convert.ToBase64String(new byte[0].AsReadOnlySpan(), invalidOption));
        }

        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[] { 5, 6, 7, 8 }, "BQYHCA==")]
        public void TryToBase64Chars_ProducesExpectedOutput(byte[] input, string expected)
        {
            Span<char> dest;

            // Just right
            dest = new char[expected.Length];
            Assert.True(Convert.TryToBase64Chars(input.AsReadOnlySpan(), dest, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.ToArray());

            // Too short
            if (expected.Length > 0)
            {
                dest = new char[expected.Length - 1];
                Assert.False(Convert.TryToBase64Chars(input.AsReadOnlySpan(), dest, out charsWritten));
                Assert.Equal(0, charsWritten);
            }

            // Longer than needed
            dest = new char[expected.Length + 1];
            Assert.True(Convert.TryToBase64Chars(input.AsReadOnlySpan(), dest, out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.Slice(0, expected.Length).ToArray());
            Assert.Equal(0, dest[dest.Length - 1]);
        }

        [Theory]
        [InlineData((Base64FormattingOptions)(-1))]
        [InlineData((Base64FormattingOptions)(2))]
        public void TryToBase64Chars_InvalidOptions_Throws(Base64FormattingOptions invalidOption)
        {
            AssertExtensions.Throws<ArgumentException>("options",
                () => Convert.TryToBase64Chars(new byte[0].AsReadOnlySpan(), new char[0].AsSpan(), out int charsWritten, invalidOption));
        }

        [Theory]
        [InlineData("")]
        [InlineData("BQYHCA==")]
        [InlineData(
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4\r\n" +
            "OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3Bx\r\n" +
            "cnN0dXZ3")]
        public void TryFromBase64String_MatchesFromBase64String(string stringInput)
        {
            byte[] expected = Convert.FromBase64String(stringInput);
            Span<byte> dest;

            // Just the right length
            dest = new byte[expected.Length];
            Assert.True(Convert.TryFromBase64String(stringInput, dest, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal<byte>(expected, dest.ToArray());

            // Too short
            if (expected.Length > 0)
            {
                dest = new byte[expected.Length - 1];
                Assert.False(Convert.TryFromBase64String(stringInput, dest, out bytesWritten));
                Assert.Equal(0, bytesWritten);
            }

            // Longer than needed
            dest = new byte[expected.Length + 1];
            Assert.True(Convert.TryFromBase64String(stringInput, dest, out bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal<byte>(expected, dest.Slice(0, expected.Length).ToArray());
            Assert.Equal(0, dest[dest.Length - 1]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("BQYHCA==")]
        [InlineData(
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4\r\n" +
            "OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3Bx\r\n" +
            "cnN0dXZ3")]
        public void TryFromBase64Chars_MatchesFromBase64CharArray(string stringInput)
        {
            char[] charArrayInput = stringInput.ToCharArray();
            byte[] expected = Convert.FromBase64CharArray(charArrayInput, 0, charArrayInput.Length);
            Span<byte> dest;

            // Just the right length
            dest = new byte[expected.Length];
            Assert.True(Convert.TryFromBase64Chars(charArrayInput.AsReadOnlySpan(), dest, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal<byte>(expected, dest.ToArray());

            // Too short
            if (expected.Length > 0)
            {
                dest = new byte[expected.Length - 1];
                Assert.False(Convert.TryFromBase64Chars(charArrayInput.AsReadOnlySpan(), dest, out bytesWritten));
                Assert.Equal(0, bytesWritten);
            }

            // Longer than needed
            dest = new byte[expected.Length + 1];
            Assert.True(Convert.TryFromBase64Chars(charArrayInput.AsReadOnlySpan(), dest, out bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal<byte>(expected, dest.Slice(0, dest.Length - 1).ToArray());
            Assert.Equal(0, dest[dest.Length - 1]);
        }
    }
}
