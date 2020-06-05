using System.Text;
using Xunit;

namespace System.Tests
{
    public class ConvertFromHexStringTests
    {
        [Fact]
        public static void KnownByteSequence()
        {
            string inputString = "000102FDFEFF";
            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0xFD, 0xFE, 0xFF }, Convert.FromHexString(inputString));
        }

        [Fact]
        public static void CompleteValueRange()
        {
            byte[] values = new byte[256];
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (byte)i;
                sb.Append(i.ToString("X2"));
            }

            Assert.Equal(values, Convert.FromHexString(sb.ToString()));
        }

        [Fact]
        public static void InvalidInputString_Null()
        {
            Assert.Throws<ArgumentNullException>(() => Convert.FromHexString(null));
        }

        [Fact]
        public static void InvalidInputString_HalfByte()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("ABC"));
        }

        [Fact]
        public static void InvalidInputString_BadFirstCharacter()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("x0"));
        }

        [Fact]
        public static void InvalidInputString_BadSecondCharacter()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("0x"));
        }

        [Fact]
        public static void InvalidInputString_NonAsciiCharacter()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("0\u0308"));
        }

        [Fact]
        public static void InvalidInputString_Lowercase()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("010a"));
        }

        [Fact]
        public static void InvalidInputString_ZeroWidthSpace()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("\u200B 000102FDFEFF"));
        }

        [Fact]
        public static void InvalidInputString_LeadingWhiteSpace()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("  000102FDFEFF"));
        }

        [Fact]
        public static void InvalidInputString_TrailingWhiteSpace()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("000102FDFEFF  "));
        }

        [Fact]
        public static void InvalidInputString_WhiteSpace()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("00 01 02FD FE FF"));
        }

        [Fact]
        public static void InvalidInputString_Dash()
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString("01-02-FD-FE-FF"));
        }

        [Fact]
        public static void ZeroLength()
        {
            Assert.Same(Array.Empty<byte>(), Convert.FromHexString(string.Empty));
        }
    }
}
