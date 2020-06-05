using System.Text;
using Xunit;

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
        public static void CompleteValueRange()
        {
            byte[] values = new byte[256];
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (byte)i;
                sb.Append(i.ToString("X2"));
            }

            Assert.Equal(sb.ToString(), Convert.ToHexString(values));
        }

        [Fact]
        public static void ZeroLength()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            Assert.Same(string.Empty, Convert.ToHexString(inputBytes, 0, 0));
        }

        [Fact]
        public static void InvalidInputBuffer()
        {
            Assert.Throws<ArgumentNullException>(() => Convert.ToHexString(null));
            Assert.Throws<ArgumentNullException>(() => Convert.ToHexString(null, 0, 0));
        }

        [Fact]
        public static void InvalidOffset()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToHexString(inputBytes, -1, inputBytes.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToHexString(inputBytes, inputBytes.Length, inputBytes.Length));
        }

        [Fact]
        public static void InvalidLength()
        {
            byte[] inputBytes = Convert.FromHexString("000102FDFEFF");
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToHexString(inputBytes, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToHexString(inputBytes, 0, inputBytes.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert.ToHexString(inputBytes, 1, inputBytes.Length));
        }

        [Fact]
        public static unsafe void InputTooLarge()
        {
            Assert.Throws<OutOfMemoryException>(() => Convert.ToHexString(new ReadOnlySpan<byte>((void*)0, Int32.MaxValue)));
        }
    }
}
