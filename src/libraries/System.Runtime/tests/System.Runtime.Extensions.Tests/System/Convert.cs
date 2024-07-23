// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Test.Cryptography;

namespace System.Tests
{
    public class ConvertTests
    {
        [Fact]
        public static void ChangeTypeTest()
        {
            var testValue = 0;
            bool expectedValue = false;
            Assert.Equal(expectedValue, Convert.ChangeType(testValue, TypeCode.Boolean));
        }

        [Fact]
        public static void ToBase64CharArrayTest()
        {
            byte[] barray = new byte[256];
            char[] carray = new char[352];
            int length = Convert.ToBase64CharArray(barray, 0, barray.Length, carray, 0, Base64FormattingOptions.InsertLineBreaks);
            int length2 = Convert.ToBase64CharArray(barray, 0, barray.Length, carray, 0, Base64FormattingOptions.None);
            Assert.Equal(352, length);
            Assert.Equal(344, length2);
        }

        [Fact]
        public static void ToBase64CharArray_NonEmptyInput_ZeroLength_Test()
        {
            Assert.Equal(0, Convert.ToBase64CharArray(new byte[1], 0, 0, new char[0], 0, Base64FormattingOptions.None));
        }

        [Fact]
        public static void ToBase64StringTest()
        {
            byte[] barray = new byte[] { 1, 2, 3 };
            byte[] subset = new byte[] { 2, 3 };
            string s1 = Convert.ToBase64String(barray, Base64FormattingOptions.InsertLineBreaks);
            string s2 = Convert.ToBase64String(barray, Base64FormattingOptions.None);
            string s3 = Convert.ToBase64String(barray, 1, 2, Base64FormattingOptions.None);
            Assert.Equal(barray, Convert.FromBase64String(s1));
            Assert.True(!s2.Contains("\n"));
            Assert.Equal(barray, Convert.FromBase64String(s2));
            Assert.Equal(subset, Convert.FromBase64String(s3));
        }

        [Fact]
        public static void Base64_AllMethodsRoundtripConsistently()
        {
            var r = new Random(42);
            for (int length = 0; length < 128; length++)
            {
                var original = new byte[length];
                r.NextBytes(original);

                string encodedString = Convert.ToBase64String(original);

                char[] encodedArray = new char[encodedString.Length];
                int charsWritten = Convert.ToBase64CharArray(original, 0, original.Length, encodedArray, 0);
                Assert.Equal(encodedArray.Length, charsWritten);
                AssertExtensions.SequenceEqual<char>(encodedString, encodedArray);

                char[] encodedSpan = new char[encodedString.Length];
                Assert.True(Convert.TryToBase64Chars(original, encodedSpan, out charsWritten));
                Assert.Equal(encodedSpan.Length, charsWritten);
                AssertExtensions.SequenceEqual<char>(encodedString, encodedSpan);

                AssertExtensions.SequenceEqual(original, Convert.FromBase64String(encodedString));
                AssertExtensions.SequenceEqual(original, Convert.FromBase64CharArray(encodedArray, 0, encodedArray.Length));

                byte[] actualBytes = new byte[original.Length];
                Assert.True(Convert.TryFromBase64Chars(encodedSpan, actualBytes, out int bytesWritten));
                Assert.Equal(original.Length, bytesWritten);
                AssertExtensions.SequenceEqual(original, actualBytes);
            }
        }

        [Fact]
        public static void FromBase64CharArray_NonEmptyInputZeroLength_ReturnsEmptyArray()
        {
            Assert.Same(Array.Empty<byte>(), Convert.FromBase64CharArray(new char[42], 0, 0));
        }

        [Fact]
        public void ToBooleanTests()
        {
            char testValue = char.MinValue;
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(testValue));
            DateTime testValue2 = DateTime.MinValue;
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(testValue2));
        }

        [Fact]
        public void ToByteTest()
        {
            DateTime testValue = DateTime.MaxValue;
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(testValue));
        }

        [Fact]
        public void ToCharTests()
        {
            char testValue = char.MinValue;
            Assert.Equal(testValue, Convert.ToChar(testValue));
            DateTime testValue2 = DateTime.MinValue;
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(testValue2));
        }

        [Fact]
        public void ToDateTimeTests()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(byte.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(sbyte.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(float.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(ushort.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(uint.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(ulong.MinValue));
        }

        [Fact]
        public void ToDecimalTests()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(char.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(DateTime.MinValue));
        }

        [Fact]
        public void ToDoubleTests()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(char.MinValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(DateTime.MinValue));
        }

        [Fact]
        public void ToInt16Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(DateTime.MaxValue));
        }

        [Fact]
        public void ToInt32Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(DateTime.MaxValue));
        }

        [Fact]
        public void ToInt64Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(DateTime.MaxValue));
        }

        [Fact]
        public static void IsDBNullTest()
        {
            Assert.True(Convert.IsDBNull(Convert.DBNull));
            Assert.False(Convert.IsDBNull(4));
            Assert.False(Convert.IsDBNull(true));
            Assert.False(Convert.IsDBNull('x'));
            Assert.False(Convert.IsDBNull(1.1));
            Assert.False(Convert.IsDBNull(null));
        }

        [Fact]
        public void ToSByteTest()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(DateTime.MaxValue));
        }

        [Fact]
        public void ToSingleTests()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(DateTime.MaxValue));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(char.MinValue));
        }

        [Fact]
        public static void ToStringTests()
        {
            string testValue = "Hello World!";
            Assert.Equal(testValue, Convert.ToString(testValue));
            Assert.Equal(testValue, Convert.ToString(testValue, NumberFormatInfo.CurrentInfo));
        }

        [Fact]
        public void ToUInt16Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(DateTime.MaxValue));
        }

        [Fact]
        public void ToUInt32Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(DateTime.MaxValue));
        }

        [Fact]
        public void ToUInt64Test()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(DateTime.MaxValue));
        }

        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[] { 5, 6, 7, 8 }, "BQYHCA==")]
        public void ToBase64String_Span_ProducesExpectedOutput(byte[] input, string expected)
        {
            Assert.Equal(expected, Convert.ToBase64String(input.AsSpan()));
            Assert.Equal(expected, Convert.ToBase64String(input.AsSpan(), Base64FormattingOptions.None));
            Assert.Equal(expected, Convert.ToBase64String(input.AsSpan(), Base64FormattingOptions.InsertLineBreaks));
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
            AssertExtensions.Throws<ArgumentException>("options", () => Convert.ToBase64String(new byte[0].AsSpan(), invalidOption));
        }

        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[] { 5, 6, 7, 8 }, "BQYHCA==")]
        public void TryToBase64Chars_ProducesExpectedOutput(byte[] input, string expected)
        {
            Span<char> dest;

            // Just right
            dest = new char[expected.Length];
            Assert.True(Convert.TryToBase64Chars(input.AsSpan(), dest, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal<char>(expected.ToCharArray(), dest.ToArray());

            // Too short
            if (expected.Length > 0)
            {
                dest = new char[expected.Length - 1];
                Assert.False(Convert.TryToBase64Chars(input.AsSpan(), dest, out charsWritten));
                Assert.Equal(0, charsWritten);
            }

            // Longer than needed
            dest = new char[expected.Length + 1];
            Assert.True(Convert.TryToBase64Chars(input.AsSpan(), dest, out charsWritten));
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
                () => Convert.TryToBase64Chars(new byte[0].AsSpan(), new char[0].AsSpan(), out int charsWritten, invalidOption));
        }

        [Theory]
        [MemberData(nameof(Base64TestData))]
        public static void TryFromBase64String(string encoded, byte[] expected)
        {
            if (expected == null)
            {
                Span<byte> actual = new byte[1000];
                bool success = Convert.TryFromBase64String(encoded, actual, out int bytesWritten);
                Assert.False(success);
                Assert.Equal(0, bytesWritten);
            }
            else
            {
                // Exact-sized buffer
                {
                    byte[] actual = new byte[expected.Length];
                    bool success = Convert.TryFromBase64String(encoded, actual, out int bytesWritten);
                    Assert.True(success);
                    Assert.Equal<byte>(expected, actual);
                    Assert.Equal(expected.Length, bytesWritten);
                }

                // Buffer too short
                if (expected.Length != 0)
                {
                    byte[] actual = new byte[expected.Length - 1];
                    bool success = Convert.TryFromBase64String(encoded, actual, out int bytesWritten);
                    Assert.False(success);
                    Assert.Equal(0, bytesWritten);
                }

                // Buffer larger than needed
                {
                    byte[] actual = new byte[expected.Length + 1];
                    actual[expected.Length] = 99;
                    bool success = Convert.TryFromBase64String(encoded, actual, out int bytesWritten);
                    Assert.True(success);
                    Assert.Equal(99, actual[expected.Length]);
                    Assert.Equal<byte>(expected, actual.Take(expected.Length));
                    Assert.Equal(expected.Length, bytesWritten);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Base64TestData))]
        public static void TryFromBase64Chars(string encodedAsString, byte[] expected)
        {
            ReadOnlySpan<char> encoded = encodedAsString;  // Executing the conversion to ROS here so people debugging don't have to step through it at the api callsite.
            if (expected == null)
            {
                Span<byte> actual = new byte[1000];
                bool success = Convert.TryFromBase64Chars(encoded, actual, out int bytesWritten);
                Assert.False(success);
                Assert.Equal(0, bytesWritten);
            }
            else
            {
                // Exact-sized buffer
                {
                    byte[] actual = new byte[expected.Length];
                    bool success = Convert.TryFromBase64Chars(encoded, actual, out int bytesWritten);
                    Assert.True(success);
                    Assert.Equal<byte>(expected, actual);
                    Assert.Equal(expected.Length, bytesWritten);
                }

                // Buffer too short
                if (expected.Length != 0)
                {
                    byte[] actual = new byte[expected.Length - 1];
                    bool success = Convert.TryFromBase64Chars(encoded, actual, out int bytesWritten);
                    Assert.False(success);
                    Assert.Equal(0, bytesWritten);
                }

                // Buffer larger than needed
                {
                    byte[] actual = new byte[expected.Length + 1];
                    actual[expected.Length] = 99;
                    bool success = Convert.TryFromBase64Chars(encoded, actual, out int bytesWritten);
                    Assert.True(success);
                    Assert.Equal(99, actual[expected.Length]);
                    Assert.Equal<byte>(expected, actual.Take(expected.Length));
                    Assert.Equal(expected.Length, bytesWritten);
                }
            }
        }

        public static IEnumerable<object[]> Base64TestData
        {
            get
            {
                foreach (Tuple<string, byte[]> tuple in Base64TestDataSeed)
                {
                    yield return new object[] { tuple.Item1, tuple.Item2 };
                    yield return new object[] { InsertSpaces(tuple.Item1, 1), tuple.Item2 };
                    yield return new object[] { InsertSpaces(tuple.Item1, 4), tuple.Item2 };
                }
            }
        }

        public static IEnumerable<Tuple<string, byte[]>> Base64TestDataSeed
        {
            get
            {
                // Empty
                yield return Tuple.Create<string, byte[]>("", Array.Empty<byte>());

                // All whitespace characters.
                yield return Tuple.Create<string, byte[]>(" \t\r\n", Array.Empty<byte>());

                // Pad characters
                yield return Tuple.Create<string, byte[]>("BQYHCAZ=", "0506070806".HexToByteArray());
                yield return Tuple.Create<string, byte[]>("BQYHCA==", "05060708".HexToByteArray());

                // Typical
                yield return Tuple.Create<string, byte[]>(
                    "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0" +
                    "BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3",

                    ("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F202122232425262728292A2B2C2D2E2F303132333435363738393A3B3C3D3E" +
                     "3F404142434445464748494A4B4C4D4E4F505152535455565758595A5B5C5D5E5F606162636465666768696A6B6C6D6E6F7071727374757677").HexToByteArray()
                );

                // Input length not multiple of 4
                yield return Tuple.Create<string, byte[]>("A", null);
                yield return Tuple.Create<string, byte[]>("AA", null);
                yield return Tuple.Create<string, byte[]>("AAA", null);
                yield return Tuple.Create<string, byte[]>("AAAAA", null);
                yield return Tuple.Create<string, byte[]>("AAAAAA", null);
                yield return Tuple.Create<string, byte[]>("AAAAAAA", null);

                // Cannot continue past end pad
                yield return Tuple.Create<string, byte[]>("AAA=BBBB", null);
                yield return Tuple.Create<string, byte[]>("AA==BBBB", null);

                // Cannot have more than two end pads
                yield return Tuple.Create<string, byte[]>("A===", null);
                yield return Tuple.Create<string, byte[]>("====", null);

                // Verify negative entries of charmap.
                for (int i = 0; i < 256; i++)
                {
                    char c = (char)i;
                    if (!IsValidBase64Char(c))
                    {
                        string text = new string(c, 1) + "AAA";
                        yield return Tuple.Create<string, byte[]>(text, null);
                    }
                }

                // Verify >255 character handling.
                string largerThanByte = new string((char)256, 1);
                yield return Tuple.Create<string, byte[]>(largerThanByte + "AAA", null);
                yield return Tuple.Create<string, byte[]>("A" + largerThanByte + "AA", null);
                yield return Tuple.Create<string, byte[]>("AA" + largerThanByte + "A", null);
                yield return Tuple.Create<string, byte[]>("AAA" + largerThanByte, null);
                yield return Tuple.Create<string, byte[]>("AAAA" + largerThanByte + "AAA", null);
                yield return Tuple.Create<string, byte[]>("AAAA" + "A" + largerThanByte + "AA", null);
                yield return Tuple.Create<string, byte[]>("AAAA" + "AA" + largerThanByte + "A", null);
                yield return Tuple.Create<string, byte[]>("AAAA" + "AAA" + largerThanByte, null);

                // Verify positive entries of charmap.
                yield return Tuple.Create<string, byte[]>("+A==", new byte[] { 0xf8 });
                yield return Tuple.Create<string, byte[]>("/A==", new byte[] { 0xfc });
                yield return Tuple.Create<string, byte[]>("0A==", new byte[] { 0xd0 });
                yield return Tuple.Create<string, byte[]>("1A==", new byte[] { 0xd4 });
                yield return Tuple.Create<string, byte[]>("2A==", new byte[] { 0xd8 });
                yield return Tuple.Create<string, byte[]>("3A==", new byte[] { 0xdc });
                yield return Tuple.Create<string, byte[]>("4A==", new byte[] { 0xe0 });
                yield return Tuple.Create<string, byte[]>("5A==", new byte[] { 0xe4 });
                yield return Tuple.Create<string, byte[]>("6A==", new byte[] { 0xe8 });
                yield return Tuple.Create<string, byte[]>("7A==", new byte[] { 0xec });
                yield return Tuple.Create<string, byte[]>("8A==", new byte[] { 0xf0 });
                yield return Tuple.Create<string, byte[]>("9A==", new byte[] { 0xf4 });
                yield return Tuple.Create<string, byte[]>("AA==", new byte[] { 0x00 });
                yield return Tuple.Create<string, byte[]>("BA==", new byte[] { 0x04 });
                yield return Tuple.Create<string, byte[]>("CA==", new byte[] { 0x08 });
                yield return Tuple.Create<string, byte[]>("DA==", new byte[] { 0x0c });
                yield return Tuple.Create<string, byte[]>("EA==", new byte[] { 0x10 });
                yield return Tuple.Create<string, byte[]>("FA==", new byte[] { 0x14 });
                yield return Tuple.Create<string, byte[]>("GA==", new byte[] { 0x18 });
                yield return Tuple.Create<string, byte[]>("HA==", new byte[] { 0x1c });
                yield return Tuple.Create<string, byte[]>("IA==", new byte[] { 0x20 });
                yield return Tuple.Create<string, byte[]>("JA==", new byte[] { 0x24 });
                yield return Tuple.Create<string, byte[]>("KA==", new byte[] { 0x28 });
                yield return Tuple.Create<string, byte[]>("LA==", new byte[] { 0x2c });
                yield return Tuple.Create<string, byte[]>("MA==", new byte[] { 0x30 });
                yield return Tuple.Create<string, byte[]>("NA==", new byte[] { 0x34 });
                yield return Tuple.Create<string, byte[]>("OA==", new byte[] { 0x38 });
                yield return Tuple.Create<string, byte[]>("PA==", new byte[] { 0x3c });
                yield return Tuple.Create<string, byte[]>("QA==", new byte[] { 0x40 });
                yield return Tuple.Create<string, byte[]>("RA==", new byte[] { 0x44 });
                yield return Tuple.Create<string, byte[]>("SA==", new byte[] { 0x48 });
                yield return Tuple.Create<string, byte[]>("TA==", new byte[] { 0x4c });
                yield return Tuple.Create<string, byte[]>("UA==", new byte[] { 0x50 });
                yield return Tuple.Create<string, byte[]>("VA==", new byte[] { 0x54 });
                yield return Tuple.Create<string, byte[]>("WA==", new byte[] { 0x58 });
                yield return Tuple.Create<string, byte[]>("XA==", new byte[] { 0x5c });
                yield return Tuple.Create<string, byte[]>("YA==", new byte[] { 0x60 });
                yield return Tuple.Create<string, byte[]>("ZA==", new byte[] { 0x64 });
                yield return Tuple.Create<string, byte[]>("aA==", new byte[] { 0x68 });
                yield return Tuple.Create<string, byte[]>("bA==", new byte[] { 0x6c });
                yield return Tuple.Create<string, byte[]>("cA==", new byte[] { 0x70 });
                yield return Tuple.Create<string, byte[]>("dA==", new byte[] { 0x74 });
                yield return Tuple.Create<string, byte[]>("eA==", new byte[] { 0x78 });
                yield return Tuple.Create<string, byte[]>("fA==", new byte[] { 0x7c });
                yield return Tuple.Create<string, byte[]>("gA==", new byte[] { 0x80 });
                yield return Tuple.Create<string, byte[]>("hA==", new byte[] { 0x84 });
                yield return Tuple.Create<string, byte[]>("iA==", new byte[] { 0x88 });
                yield return Tuple.Create<string, byte[]>("jA==", new byte[] { 0x8c });
                yield return Tuple.Create<string, byte[]>("kA==", new byte[] { 0x90 });
                yield return Tuple.Create<string, byte[]>("lA==", new byte[] { 0x94 });
                yield return Tuple.Create<string, byte[]>("mA==", new byte[] { 0x98 });
                yield return Tuple.Create<string, byte[]>("nA==", new byte[] { 0x9c });
                yield return Tuple.Create<string, byte[]>("oA==", new byte[] { 0xa0 });
                yield return Tuple.Create<string, byte[]>("pA==", new byte[] { 0xa4 });
                yield return Tuple.Create<string, byte[]>("qA==", new byte[] { 0xa8 });
                yield return Tuple.Create<string, byte[]>("rA==", new byte[] { 0xac });
                yield return Tuple.Create<string, byte[]>("sA==", new byte[] { 0xb0 });
                yield return Tuple.Create<string, byte[]>("tA==", new byte[] { 0xb4 });
                yield return Tuple.Create<string, byte[]>("uA==", new byte[] { 0xb8 });
                yield return Tuple.Create<string, byte[]>("vA==", new byte[] { 0xbc });
                yield return Tuple.Create<string, byte[]>("wA==", new byte[] { 0xc0 });
                yield return Tuple.Create<string, byte[]>("xA==", new byte[] { 0xc4 });
                yield return Tuple.Create<string, byte[]>("yA==", new byte[] { 0xc8 });
                yield return Tuple.Create<string, byte[]>("zA==", new byte[] { 0xcc });
            }
        }

        private static string InsertSpaces(string text, int period)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if ((i % period) == 0)
                {
                    sb.Append("  ");
                }
                sb.Append(text[i]);
            }
            sb.Append("  ");
            return sb.ToString();
        }

        private static bool IsValidBase64Char(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c is '+' or '/';
        }
    }
}
