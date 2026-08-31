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

            Span<char> output = stackalloc char[12];
            Assert.True(Convert.TryToHexString(inputBytes, output, out int charsWritten));
            Assert.Equal(12, charsWritten);
            Assert.Equal("000102FDFEFF", output.ToString());

            Span<byte> outputUtf8 = stackalloc byte[12];
            Assert.True(Convert.TryToHexString(inputBytes, outputUtf8, out int bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal("000102FDFEFF", Encoding.UTF8.GetString(outputUtf8));
        }

        [Fact]
        public static void KnownByteSequenceLower()
        {
            byte[] inputBytes = new byte[] { 0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF };
            Assert.Equal("000102fdfeff", Convert.ToHexStringLower(inputBytes));

            Span<char> output = stackalloc char[12];
            Assert.True(Convert.TryToHexStringLower(inputBytes, output, out int charsWritten));
            Assert.Equal(12, charsWritten);
            Assert.Equal("000102fdfeff", output.ToString());

            Span<byte> outputUtf8 = stackalloc byte[12];
            Assert.True(Convert.TryToHexStringLower(inputBytes, outputUtf8, out int bytesWritten));
            Assert.Equal(12, bytesWritten);
            Assert.Equal("000102fdfeff", Encoding.UTF8.GetString(outputUtf8));
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

            string excepted = sb.ToString();
            Assert.Equal(excepted, Convert.ToHexString(values));

            Span<char> output = stackalloc char[512];
            Assert.True(Convert.TryToHexString(values, output, out int charsWritten));
            Assert.Equal(512, charsWritten);
            Assert.Equal(excepted, output.ToString());

            Span<byte> outputUtf8 = stackalloc byte[512];
            Assert.True(Convert.TryToHexString(values, outputUtf8, out int bytesWritten));
            Assert.Equal(512, bytesWritten);
            Assert.Equal(excepted, Encoding.UTF8.GetString(outputUtf8));
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

            string excepted = sb.ToString();
            Assert.Equal(excepted, Convert.ToHexStringLower(values));

            Span<char> output = stackalloc char[512];
            Assert.True(Convert.TryToHexStringLower(values, output, out int charsWritten));
            Assert.Equal(512, charsWritten);
            Assert.Equal(excepted, output.ToString());

            Span<byte> outputUtf8 = stackalloc byte[512];
            Assert.True(Convert.TryToHexStringLower(values, outputUtf8, out int bytesWritten));
            Assert.Equal(512, bytesWritten);
            Assert.Equal(excepted, Encoding.UTF8.GetString(outputUtf8));
        }

        [Fact]
        public static void ZeroLength()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            Assert.Same(string.Empty, Convert.ToHexString(inputBytes, 0, 0));
            Assert.Same(string.Empty, Convert.ToHexStringLower(inputBytes, 0, 0));

            Span<char> output = stackalloc char[12];
            Assert.True(Convert.TryToHexString(default, output, out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.True(Convert.TryToHexStringLower(default, output, out charsWritten));
            Assert.Equal(0, charsWritten);

            Span<byte> outputUtf8 = stackalloc byte[12];
            Assert.True(Convert.TryToHexString(default, outputUtf8, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.True(Convert.TryToHexStringLower(default, outputUtf8, out bytesWritten));
            Assert.Equal(0, bytesWritten);
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
        public static void InvalidOutputBuffer()
        {
            byte[] inputBytes = new byte[] { 0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF };

            Span<char> output = stackalloc char[11];
            Assert.False(Convert.TryToHexString(inputBytes, Span<char>.Empty, out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.False(Convert.TryToHexString(inputBytes, output, out charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.False(Convert.TryToHexStringLower(inputBytes, Span<char>.Empty, out charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.False(Convert.TryToHexStringLower(inputBytes, output, out charsWritten));
            Assert.Equal(0, charsWritten);

            Span<byte> outputUtf8 = stackalloc byte[11];
            Assert.False(Convert.TryToHexString(inputBytes, Span<byte>.Empty, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.False(Convert.TryToHexString(inputBytes, outputUtf8, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.False(Convert.TryToHexStringLower(inputBytes, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.False(Convert.TryToHexStringLower(inputBytes, outputUtf8, out bytesWritten));
            Assert.Equal(0, bytesWritten);
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Convert.ToHexString(new ReadOnlySpan<byte>((void*)0, int.MaxValue)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () => Convert.ToHexStringLower(new ReadOnlySpan<byte>((void*)0, int.MaxValue)));

            Span<char> output = new Span<char>((void*)0, int.MaxValue);
            Assert.False(Convert.TryToHexString(new ReadOnlySpan<byte>((void*)0, int.MaxValue), output, out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.False(Convert.TryToHexStringLower(new ReadOnlySpan<byte>((void*)0, int.MaxValue), output, out charsWritten));
            Assert.Equal(0, charsWritten);

            Span<byte> outputUtf8 = new Span<byte>((void*)0, int.MaxValue);
            Assert.False(Convert.TryToHexString(new ReadOnlySpan<byte>((void*)0, int.MaxValue), outputUtf8, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.False(Convert.TryToHexStringLower(new ReadOnlySpan<byte>((void*)0, int.MaxValue), outputUtf8, out bytesWritten));
            Assert.Equal(0, bytesWritten);
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
        public static unsafe void TryToHexString(byte[] input, string expected)
        {
            Span<char> output = new char[expected.Length];
            Assert.True(Convert.TryToHexString(input, output, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, output.ToString());

            Span<byte> outputUtf8 = new byte[expected.Length];
            Assert.True(Convert.TryToHexString(input, outputUtf8, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal(expected, Encoding.UTF8.GetString(outputUtf8));
        }


        [Theory]
        [MemberData(nameof(ToHexStringTestData))]
        public static unsafe void ToHexStringLower(byte[] input, string expected)
        {
            string actual = Convert.ToHexStringLower(input);
            Assert.Equal(expected.ToLower(), actual);
        }

        [Theory]
        [MemberData(nameof(ToHexStringTestData))]
        public static unsafe void TryToHexStringLower(byte[] input, string expected)
        {
            Span<char> output = new char[expected.Length];
            Assert.True(Convert.TryToHexStringLower(input, output, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected.ToLower(), output.ToString());

            Span<byte> outputUtf8 = new byte[expected.Length];
            Assert.True(Convert.TryToHexStringLower(input, outputUtf8, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal(expected.ToLower(), Encoding.UTF8.GetString(outputUtf8));
        }
    }
}
