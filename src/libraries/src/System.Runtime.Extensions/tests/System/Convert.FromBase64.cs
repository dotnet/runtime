// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Tests
{
    public class ConvertFromBase64Tests
    {
        [Fact]
        public static void Roundtrip1()
        {
            string input = "test";
            Verify(input, result =>
            {
            // See Freed, N. and N. Borenstein, RFC2045, Section 6.8 for a description of why this check is necessary.
            Assert.Equal(3, result.Length);

                uint triplet = (uint)((result[0] << 16) | (result[1] << 8) | result[2]);
                Assert.Equal<uint>(45, triplet >> 18); // 't'
            Assert.Equal<uint>(30, (triplet << 14) >> 26); // 'e'
            Assert.Equal<uint>(44, (triplet << 20) >> 26); // 's'
            Assert.Equal<uint>(45, (triplet << 26) >> 26); // 't'

            Assert.Equal(input, Convert.ToBase64String(result));
            });
        }

        [Fact]
        public static void Roundtrip2()
        {
            VerifyRoundtrip("AAAA");
        }

        [Fact]
        public static void Roundtrip3()
        {
            VerifyRoundtrip("AAAAAAAA");
        }

        [Fact]
        public static void EmptyString()
        {
            string input = string.Empty;
            Verify(input, result =>
            {
                Assert.NotNull(result);
                Assert.Equal(0, result.Length);
            });
        }

        [Fact]
        public static void ZeroLengthArray()
        {
            string input = "test";
            char[] inputChars = input.ToCharArray();
            byte[] result = Convert.FromBase64CharArray(inputChars, 0, 0);

            Assert.NotNull(result);
            Assert.Equal(0, result.Length);
        }

        [Fact]
        public static void RoundtripWithPadding1()
        {
            VerifyRoundtrip("abc=");
        }

        [Fact]
        public static void RoundtripWithPadding2()
        {
            VerifyRoundtrip("BQYHCA==");
        }

        [Fact]
        public static void PartialRoundtripWithPadding1()
        {
            string input = "ab==";
            Verify(input, result =>
            {
                Assert.Equal(1, result.Length);

                string roundtrippedString = Convert.ToBase64String(result);
                Assert.NotEqual(input, roundtrippedString);
                Assert.Equal(input[0], roundtrippedString[0]);
            });
        }

        [Fact]
        public static void PartialRoundtripWithPadding2()
        {
            string input = "789=";
            Verify(input, result =>
            {
                Assert.Equal(2, result.Length);

                string roundtrippedString = Convert.ToBase64String(result);
                Assert.NotEqual(input, roundtrippedString);
                Assert.Equal(input[0], roundtrippedString[0]);
                Assert.Equal(input[1], roundtrippedString[1]);
            });
        }

        [Fact]
        public static void ParseWithWhitespace()
        {
            Verify("abc= \t \r\n =");
        }

        [Fact]
        public static void RoundtripWithWhitespace2()
        {
            string input = "abc=  \t\n\t\r ";
            VerifyRoundtrip(input, input.Trim());
        }

        [Fact]
        public static void RoundtripWithWhitespace3()
        {
            string input = "abc \r\n\t   =  \t\n\t\r ";
            VerifyRoundtrip(input, "abc=");
        }

        [Fact]
        public static void RoundtripWithWhitespace4()
        {
            string expected = "test";
            string input = expected.Insert(1, new string(' ', 17)).PadLeft(31, ' ').PadRight(12, ' ');
            VerifyRoundtrip(input, expected, expectedLengthBytes: 3);
        }

        [Fact]
        public static void RoundtripWithWhitespace5()
        {
            string expected = "test";
            string input = expected.Insert(2, new string('\t', 9)).PadLeft(37, '\t').PadRight(8, '\t');
            VerifyRoundtrip(input, expected, expectedLengthBytes: 3);
        }

        [Fact]
        public static void RoundtripWithWhitespace6()
        {
            string expected = "test";
            string input = expected.Insert(2, new string('\r', 13)).PadLeft(7, '\r').PadRight(29, '\r');
            VerifyRoundtrip(input, expected, expectedLengthBytes: 3);
        }

        [Fact]
        public static void RoundtripWithWhitespace7()
        {
            string expected = "test";
            string input = expected.Insert(2, new string('\n', 23)).PadLeft(17, '\n').PadRight(34, '\n');
            VerifyRoundtrip(input, expected, expectedLengthBytes: 3);
        }

        [Fact]
        public static void RoundtripLargeString()
        {
            string input = new string('a', 10000);
            VerifyRoundtrip(input, input);
        }

        [Fact]
        public static void InvalidOffset()
        {
            string input = "test";
            char[] inputChars = input.ToCharArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.FromBase64CharArray(inputChars, -1, inputChars.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.FromBase64CharArray(inputChars, inputChars.Length, inputChars.Length));
        }

        [Fact]
        public static void InvalidLength()
        {
            string input = "test";
            char[] inputChars = input.ToCharArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.FromBase64CharArray(inputChars, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.FromBase64CharArray(inputChars, 0, inputChars.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.FromBase64CharArray(inputChars, 1, inputChars.Length));
        }

        [Fact]
        public static void InvalidInput()
        {
            Assert.Throws<ArgumentNullException>(() => Convert.FromBase64CharArray(null, 0, 3));

            // Input must be at least 4 characters long
            VerifyInvalidInput("No");

            // Length of input must be a multiple of 4
            VerifyInvalidInput("NoMore");

            // Input must not contain invalid characters
            VerifyInvalidInput("2-34");

            // Input must not contain 3 or more padding characters in a row
            VerifyInvalidInput("a===");
            VerifyInvalidInput("abc=====");
            VerifyInvalidInput("a===\r  \t  \n");

            // Input must not contain padding characters in the middle of the string
            VerifyInvalidInput("No=n");
            VerifyInvalidInput("abcdabc=abcd");
            VerifyInvalidInput("abcdab==abcd");
            VerifyInvalidInput("abcda===abcd");
            VerifyInvalidInput("abcd====abcd");

            // Input must not contain extra trailing padding characters
            VerifyInvalidInput("=");
            VerifyInvalidInput("abc===");
        }

        [Fact]
        public static void ExtraPaddingCharacter()
        {
            VerifyInvalidInput("abcdxyz=" + "=");
        }

        [Fact]
        public static void InvalidCharactersInInput()
        {
            ushort[] invalidChars = { 30122, 62608, 13917, 19498, 2473, 40845, 35988, 2281, 51246, 36372 };

            foreach (char ch in invalidChars)
            {
                var builder = new StringBuilder("abc");
                builder.Insert(1, ch);
                VerifyInvalidInput(builder.ToString());
            }
        }

        private static void VerifyRoundtrip(string input, string expected = null, int? expectedLengthBytes = null)
        {
            if (expected == null)
            {
                expected = input;
            }

            Verify(input, result =>
            {
                if (expectedLengthBytes.HasValue)
                {
                    Assert.Equal(expectedLengthBytes.Value, result.Length);
                }
                Assert.Equal(expected, Convert.ToBase64String(result));
                Assert.Equal(expected, Convert.ToBase64String(result, 0, result.Length));
            });
        }

        private static void VerifyInvalidInput(string input)
        {
            char[] inputChars = input.ToCharArray();
            Assert.Throws<FormatException>(() => Convert.FromBase64CharArray(inputChars, 0, inputChars.Length));
            Assert.Throws<FormatException>(() => Convert.FromBase64String(input));
        }

        private static void Verify(string input, Action<byte[]> action = null)
        {
            if (action != null)
            {
                action(Convert.FromBase64CharArray(input.ToCharArray(), 0, input.Length));
                action(Convert.FromBase64String(input));
            }
        }
    }
}
