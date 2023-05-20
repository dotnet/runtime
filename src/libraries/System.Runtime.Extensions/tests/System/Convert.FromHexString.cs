using System.Text;
using Xunit;

namespace System.Tests
{
    public class ConvertFromHexStringTests
    {
        [Theory]
        [InlineData("000102FDFEFF")]
        [InlineData("000102fdfeff")]
        [InlineData("000102fDfEfF")]
        [InlineData("000102FdFeFf")]
        [InlineData("000102FDfeFF")]
        public static void KnownByteSequence(string value)
        {
            byte[] knownSequence = {0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF};
            TestSequence(knownSequence, value);
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

            TestSequence(values, sb.ToString());
            TestSequence(values, sb.ToString().ToLower());
        }

        private static void TestSequence(byte[] expected, string actual)
        {
            byte[] fromResult = Convert.FromHexString(actual);
            Assert.Equal(expected, fromResult);

            Span<byte> tryResult = stackalloc byte[actual.Length / 2];
            Assert.True(Convert.TryFromHexString(actual, tryResult, out int written));
            Assert.Equal(fromResult.Length, written);
            AssertExtensions.SequenceEqual(expected, tryResult);

        }

        [Fact]
        public static void InvalidInputString_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => Convert.FromHexString(null));
            Assert.False(Convert.TryFromHexString(null, default, out _));
        }

        [Theory]
        [InlineData("01-02-FD-FE-FF")]
        [InlineData("00 01 02FD FE FF")]
        [InlineData("000102FDFEFF  ")]
        [InlineData("  000102FDFEFF")]
        [InlineData("\u200B 000102FDFEFF")]
        [InlineData("0\u0308")]
        [InlineData("0x")]
        [InlineData("x0")]
        [InlineData("ABC")] // HalfByte
        public static void InvalidInputString_FormatException_Or_FalseResult(string invalidInput)
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString(invalidInput));

            Span<byte> buffer = stackalloc byte[invalidInput.Length / 2];
            Assert.False(Convert.TryFromHexString(invalidInput.AsSpan(), buffer, out _));
        }

        [Fact]
        public static void ZeroLength()
        {
            Assert.Same(Array.Empty<byte>(), Convert.FromHexString(string.Empty));

            bool tryResult = Convert.TryFromHexString(string.Empty, Span<byte>.Empty, out int written);

            Assert.True(tryResult);
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ToHexFromHexRoundtrip()
        {
            for (int i = 1; i < 50; i++)
            {
                byte[] data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(i);
                string hex = Convert.ToHexString(data);
                Assert.Equal(data, Convert.FromHexString(hex.ToLowerInvariant()));
                Assert.Equal(data, Convert.FromHexString(hex.ToUpperInvariant()));
                string mixedCase1 = hex.Substring(0, hex.Length / 2).ToUpperInvariant() +
                                    hex.Substring(hex.Length / 2).ToLowerInvariant();
                string mixedCase2 = hex.Substring(0, hex.Length / 2).ToLowerInvariant() +
                                    hex.Substring(hex.Length / 2).ToUpperInvariant();
                Assert.Equal(data, Convert.FromHexString(mixedCase1));
                Assert.Equal(data, Convert.FromHexString(mixedCase2));
                Assert.Throws<FormatException>(() => Convert.FromHexString(hex + "  "));
                Assert.Throws<FormatException>(() => Convert.FromHexString("\uAAAA" + hex));
            }
        }
    }
}
