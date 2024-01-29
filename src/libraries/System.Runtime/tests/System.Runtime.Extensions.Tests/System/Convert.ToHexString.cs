// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Text;
using System.Collections.Generic;

namespace System.Tests
{
    public class ConvertToHexStringTests
    {
        [Fact]
        public static void KnownByteSequence()
        {
            byte[] inputBytes = new byte[] { 0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF };
            Assert.Equal("000102FDFEFF", Convert.ToHexString(inputBytes));
        }

        [Fact]
        public static void KnownByteSequenceLower()
        {
            byte[] inputBytes = new byte[] { 0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF };
            Assert.Equal("000102fdfeff", Convert.ToHexStringLower(inputBytes));
        }

        [Fact]
        public static void CompleteValueRange()
        {
            byte[] values = new byte[256];
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (byte)i;
                sb.Append($"{i:X2}");
            }

            Assert.Equal(sb.ToString(), Convert.ToHexString(values));
        }

        [Fact]
        public static void CompleteValueRangeLower()
        {
            byte[] values = new byte[256];
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (byte)i;
                sb.Append($"{i:x2}");
            }

            Assert.Equal(sb.ToString(), Convert.ToHexStringLower(values));
        }

        [Fact]
        public static void ZeroLength()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            Assert.Same(string.Empty, Convert.ToHexString(inputBytes, 0, 0));
            Assert.Same(string.Empty, Convert.ToHexStringLower(inputBytes, 0, 0));
        }

        [Fact]
        public static void InvalidInputBuffer()
        {
            AssertExtensions.Throws<ArgumentNullException>("inArray", () => Convert.ToHexString(null));
            AssertExtensions.Throws<ArgumentNullException>("inArray", () => Convert.ToHexString(null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("inArray", () => Convert.ToHexStringLower(null));
            AssertExtensions.Throws<ArgumentNullException>("inArray", () => Convert.ToHexStringLower(null, 0, 0));
        }

        [Fact]
        public static void InvalidOffset()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexString(inputBytes, -1, inputBytes.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexString(inputBytes, inputBytes.Length, inputBytes.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexStringLower(inputBytes, -1, inputBytes.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexStringLower(inputBytes, inputBytes.Length, inputBytes.Length));
        }

        [Fact]
        public static void InvalidLength()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => Convert.ToHexString(inputBytes, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexString(inputBytes, 0, inputBytes.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexString(inputBytes, 1, inputBytes.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => Convert.ToHexStringLower(inputBytes, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexStringLower(inputBytes, 0, inputBytes.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => Convert.ToHexStringLower(inputBytes, 1, inputBytes.Length));
        }

        [Fact]
        public static unsafe void InputTooLarge()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Convert.ToHexString(new ReadOnlySpan<byte>((void*)0, Int32.MaxValue)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Convert.ToHexStringLower(new ReadOnlySpan<byte>((void*)0, Int32.MaxValue)));
        }

        public static IEnumerable<object[]> ToHexStringTestData()
        {
            yield return new object[] { new byte[0], "" };
            yield return new object[] { new byte[] { 0x00 }, "00" };
            yield return new object[] { new byte[] { 0x01 }, "01" };
            yield return new object[] { new byte[] { 0xFF }, "FF" };
            yield return new object[] { new byte[] { 0x00, 0x00 }, "0000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD }, "ABCD" };
            yield return new object[] { new byte[] { 0xFF, 0xFF }, "FFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00 }, "000000" };
            yield return new object[] { new byte[] { 0x01, 0x02, 0x03 }, "010203" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF }, "FFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00 }, "00000000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD, 0xEF, 0x12 }, "ABCDEF12" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }, "0000000000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34 }, "ABCDEF1234" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "000000000000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56 }, "ABCDEF123456" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "00000000000000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78 }, "ABCDEF12345678" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFFFFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "0000000000000000" };
            yield return new object[] { new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x90 }, "ABCDEF1234567890" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFFFFFFFFFF" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "000000000000000000" };
            yield return new object[] { new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, "010203040506070809" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, "FFFFFFFFFFFFFFFFFF" };
        }

        [Theory]
        [MemberData(nameof(ToHexStringTestData))]
        public static unsafe void ToHexString(byte[] input, string expected)
        {
            string actual = Convert.ToHexString(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ToHexStringTestData))]
        public static unsafe void ToHexStringLower(byte[] input, string expected)
        {
            string actual = Convert.ToHexStringLower(input);
            Assert.Equal(expected.ToLower(), actual);
        }
    }
}
