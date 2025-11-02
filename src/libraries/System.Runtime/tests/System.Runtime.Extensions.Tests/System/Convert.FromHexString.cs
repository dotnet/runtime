using System.Buffers;
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
            TestSequenceString(expected, actual);
            TestSequenceUtf16(expected, actual);
            TestSequenceUtf8(expected, Encoding.UTF8.GetBytes(actual));

            static void TestSequenceString(byte[] expected, string actual)
            {
                byte[] fromResult = Convert.FromHexString(actual);
                Assert.Equal(expected, fromResult);

                Span<byte> tryResult = new byte[actual.Length / 2];
                Assert.Equal(OperationStatus.Done, Convert.FromHexString(actual, tryResult, out int consumed, out int written));
                Assert.Equal(fromResult.Length, written);
                Assert.Equal(actual.Length, consumed);
                AssertExtensions.SequenceEqual(expected.AsSpan(), tryResult);
            }

            static void TestSequenceUtf16(byte[] expected, ReadOnlySpan<char> actual)
            {
                byte[] fromResult = Convert.FromHexString(actual);
                Assert.Equal(expected, fromResult);

                Span<byte> tryResult = new byte[actual.Length / 2];
                Assert.Equal(OperationStatus.Done, Convert.FromHexString(actual, tryResult, out int consumed, out int written));
                Assert.Equal(fromResult.Length, written);
                Assert.Equal(actual.Length, consumed);
                AssertExtensions.SequenceEqual(expected.AsSpan(), tryResult);
            }

            static void TestSequenceUtf8(byte[] expected, ReadOnlySpan<byte> actual)
            {
                byte[] fromResult = Convert.FromHexString(actual);
                Assert.Equal(expected, fromResult);

                Span<byte> tryResult = new byte[actual.Length / 2];
                Assert.Equal(OperationStatus.Done, Convert.FromHexString(actual, tryResult, out int consumed, out int written));
                Assert.Equal(fromResult.Length, written);
                Assert.Equal(actual.Length, consumed);
                AssertExtensions.SequenceEqual(expected.AsSpan(), tryResult);
            }
        }

        [Fact]
        public static void InvalidInputString_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => Convert.FromHexString((string)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => Convert.FromHexString((string)null, default, out _, out _));
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
        public static void InvalidInputString_FormatException_Or_FalseResult(string invalidInput)
        {
            Assert.Throws<FormatException>(() => Convert.FromHexString(invalidInput));

            Span<byte> buffer = stackalloc byte[invalidInput.Length / 2];
            Assert.Equal(OperationStatus.InvalidData, Convert.FromHexString(invalidInput.AsSpan(), buffer, out _, out _));
            Assert.Equal(OperationStatus.InvalidData, Convert.FromHexString(Encoding.UTF8.GetBytes(invalidInput), buffer, out _, out _));
        }

        [Fact]
        public static void ZeroLength()
        {
            Assert.Same(Array.Empty<byte>(), Convert.FromHexString(string.Empty));
            Assert.Same(Array.Empty<byte>(), Convert.FromHexString(ReadOnlySpan<char>.Empty));
            Assert.Same(Array.Empty<byte>(), Convert.FromHexString(ReadOnlySpan<byte>.Empty));

            OperationStatus convertResult = Convert.FromHexString(string.Empty, Span<byte>.Empty, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, convertResult);
            Assert.Equal(0, written);
            Assert.Equal(0, consumed);

            convertResult = Convert.FromHexString(ReadOnlySpan<char>.Empty, Span<byte>.Empty, out consumed, out written);

            Assert.Equal(OperationStatus.Done, convertResult);
            Assert.Equal(0, written);
            Assert.Equal(0, consumed);

            convertResult = Convert.FromHexString(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out consumed, out written);

            Assert.Equal(OperationStatus.Done, convertResult);
            Assert.Equal(0, written);
            Assert.Equal(0, consumed);
        }

        [Fact]
        public static void ToHexFromHexRoundtrip()
        {
            const int loopCount = 50;

            Span<char> buffer = stackalloc char[loopCount * 2];
            Span<char> bufferLower = stackalloc char[loopCount * 2];

            Span<byte> bufferUtf8 = stackalloc byte[loopCount * 2];
            Span<byte> bufferUtf8Lower = stackalloc byte[loopCount * 2];

            for (int i = 1; i < loopCount; i++)
            {
                byte[] data = Security.Cryptography.RandomNumberGenerator.GetBytes(i);

                string hex = Convert.ToHexString(data);
                string hexLower = hex.ToLowerInvariant();

                byte[] hexUtf8 = Encoding.UTF8.GetBytes(hex);
                byte[] hexLowerUtf8 = Encoding.UTF8.GetBytes(hexLower);

                Span<char> currentBuffer = buffer.Slice(0, i * 2);
                bool tryHex = Convert.TryToHexString(data, currentBuffer, out int written);
                Assert.True(tryHex);
                AssertExtensions.SequenceEqual(hex.AsSpan(), currentBuffer);
                Assert.Equal(hex.Length, written);

                Span<char> currentBufferLower = bufferLower.Slice(0, i * 2);
                tryHex = Convert.TryToHexStringLower(data, currentBufferLower, out written);
                Assert.True(tryHex);
                AssertExtensions.SequenceEqual(hexLower.AsSpan(), currentBufferLower);
                Assert.Equal(hexLower.Length, written);

                Span<byte> currentBufferUtf8 = bufferUtf8.Slice(0, i * 2);
                tryHex = Convert.TryToHexString(data, currentBufferUtf8, out written);
                Assert.True(tryHex);
                AssertExtensions.SequenceEqual(hexUtf8.AsSpan(), currentBufferUtf8);
                Assert.Equal(hexUtf8.Length, written);

                Span<byte> currentBufferLowerUtf8 = bufferUtf8Lower.Slice(0, i * 2);
                tryHex = Convert.TryToHexStringLower(data, currentBufferLowerUtf8, out written);
                Assert.True(tryHex);
                AssertExtensions.SequenceEqual(hexLowerUtf8.AsSpan(), currentBufferLowerUtf8);
                Assert.Equal(hexLowerUtf8.Length, written);

                TestSequence(data, hex);
                TestSequence(data, hex.ToLowerInvariant());
                TestSequence(data, hex.ToUpperInvariant());

                string mixedCase1 = hex.Substring(0, hex.Length / 2).ToUpperInvariant() +
                                    hex.Substring(hex.Length / 2).ToLowerInvariant();
                string mixedCase2 = hex.Substring(0, hex.Length / 2).ToLowerInvariant() +
                                    hex.Substring(hex.Length / 2).ToUpperInvariant();

                TestSequence(data, mixedCase1);
                TestSequence(data, mixedCase2);

                Assert.Throws<FormatException>(() => Convert.FromHexString(hex + "  "));
                Assert.Throws<FormatException>(() => Convert.FromHexString("\uAAAA" + hex));
            }
        }

        [Fact]
        public static void TooShortDestination()
        {
            const int destinationSize = 10;
            Span<byte> destination = stackalloc byte[destinationSize];
            byte[] data = Security.Cryptography.RandomNumberGenerator.GetBytes(destinationSize * 2 + 1);
            string hex = Convert.ToHexString(data);

            OperationStatus result = Convert.FromHexString(hex, destination, out int charsConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(destinationSize * 2, charsConsumed);
            Assert.Equal(destinationSize, bytesWritten);

            result = Convert.FromHexString(hex.AsSpan(), destination, out charsConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(destinationSize * 2, charsConsumed);
            Assert.Equal(destinationSize, bytesWritten);

            result = Convert.FromHexString(Encoding.UTF8.GetBytes(hex), destination, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(destinationSize * 2, bytesConsumed);
            Assert.Equal(destinationSize, bytesWritten);
        }

        [Fact]
        public static void TooLongDestination()
        {
            string hex = Convert.ToHexString([255, 255, 255]);
            byte[] buffer = new byte[100];
            OperationStatus status = Convert.FromHexString(hex, buffer, out int charsConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(hex.AsSpan(), buffer, out charsConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(Encoding.UTF8.GetBytes(hex), buffer, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, bytesConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);
        }

        [Fact]
        public static void ExactDestination()
        {
            string hex = "ffffff";
            byte[] buffer = new byte[3];
            OperationStatus status = Convert.FromHexString(hex, buffer, out int charsConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(hex.AsSpan(), buffer, out charsConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(Encoding.UTF8.GetBytes(hex), buffer, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(hex.Length, bytesConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);
        }

        [Fact]
        public static void ExactDestination_TrailingCharacter()
        {
            string hex = "fffff";
            byte[] buffer = new byte[2];
            OperationStatus status = Convert.FromHexString(hex, buffer, out int charsConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, status);
            Assert.Equal(hex.Length - 1, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(hex.AsSpan(), buffer, out charsConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, status);
            Assert.Equal(hex.Length - 1, charsConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);

            status = Convert.FromHexString(Encoding.UTF8.GetBytes(hex), buffer, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, status);
            Assert.Equal(hex.Length - 1, bytesConsumed);
            Assert.Equal(hex.Length / 2, bytesWritten);
        }

        [Fact]
        public static void NeedMoreData_OrFormatException()
        {
            const int destinationSize = 10;
            byte[] data = Security.Cryptography.RandomNumberGenerator.GetBytes(destinationSize);
            Span<byte> destination = stackalloc byte[destinationSize];
            var hex = Convert.ToHexString(data);

            var spanHex = hex.AsSpan(0, 1);
            var singeResult = Convert.FromHexString(spanHex, destination, out int consumed, out int written);

            Assert.Throws<FormatException>(() => Convert.FromHexString(hex.Substring(0, 1)));
            Assert.Equal(OperationStatus.NeedMoreData, singeResult);
            Assert.Equal(0, consumed);
            Assert.Equal(0, written);

            // Odd length
            spanHex = hex.AsSpan(0, hex.Length - 1);

            var oneOffResult = Convert.FromHexString(spanHex, destination, out consumed, out written);

            Assert.Throws<FormatException>(() => Convert.FromHexString(hex.Substring(0, hex.Length - 1)));
            Assert.Equal(OperationStatus.NeedMoreData, oneOffResult);
            Assert.Equal(spanHex.Length - 1, consumed);
            Assert.Equal((spanHex.Length - 1) / 2, written);

            // Even length
            spanHex = hex.AsSpan(0, hex.Length - 2);

            var twoOffResult = Convert.FromHexString(spanHex, destination, out consumed, out written);

            Assert.Equal(OperationStatus.Done, twoOffResult);
            Assert.Equal(spanHex.Length, consumed);
            Assert.Equal(spanHex.Length / 2, written);
        }
    }
}
