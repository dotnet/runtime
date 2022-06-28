// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.SpanTests;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64EncoderUnitTests
    {
        [Fact]
        public void BasicEncodingAndDecoding()
        {
            var bytes = new byte[byte.MaxValue + 1];
            for (int i = 0; i < byte.MaxValue + 1; i++)
            {
                bytes[i] = (byte)i;
            }

            for (int value = 0; value < 256; value++)
            {
                Span<byte> sourceBytes = bytes.AsSpan(0, value + 1);
                Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(sourceBytes.Length)];
                Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(sourceBytes, encodedBytes, out int consumed, out int encodedBytesCount));
                Assert.Equal(sourceBytes.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);

                string encodedText = Encoding.ASCII.GetString(encodedBytes.ToArray());
                string expectedText = Convert.ToBase64String(bytes, 0, value + 1);
                Assert.Equal(expectedText, encodedText);

                Assert.Equal(0, encodedBytes.Length % 4);
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(encodedBytes.Length)];
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(encodedBytes, decodedBytes, out consumed, out int decodedByteCount));
                Assert.Equal(encodedBytes.Length, consumed);
                Assert.Equal(sourceBytes.Length, decodedByteCount);
                Assert.True(sourceBytes.SequenceEqual(decodedBytes.Slice(0, decodedByteCount)));
            }
        }

        [Fact]
        public void BasicEncoding()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];
                Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount));
                Assert.Equal(source.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);
                Assert.True(Base64TestHelper.VerifyEncodingCorrectness(source.Length, encodedBytes.Length, source, encodedBytes));
            }
        }

        [Fact]
        public void BasicEncodingWithFinalBlockFalse()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];
                int expectedConsumed = source.Length / 3 * 3; // only consume closest multiple of three since isFinalBlock is false
                int expectedWritten = source.Length / 3 * 4;

                // The constant random seed guarantees that both states are tested.
                OperationStatus expectedStatus = numBytes % 3 == 0 ? OperationStatus.Done : OperationStatus.NeedMoreData;
                Assert.Equal(expectedStatus, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, encodedBytesCount);
                Assert.True(Base64TestHelper.VerifyEncodingCorrectness(expectedConsumed, expectedWritten, source, encodedBytes));
            }
        }

        [Theory]
        [InlineData(1, "AQ==")]
        [InlineData(2, "AQI=")]
        [InlineData(3, "AQID")]
        [InlineData(4, "AQIDBA==")]
        [InlineData(5, "AQIDBAU=")]
        [InlineData(6, "AQIDBAUG")]
        [InlineData(7, "AQIDBAUGBw==")]
        public void BasicEncodingWithFinalBlockTrueKnownInput(int numBytes, string expectedText)
        {
            int expectedConsumed = numBytes;
            int expectedWritten = expectedText.Length;

            Span<byte> source = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                source[i] = (byte)(i + 1);
            }
            Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock: true));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, encodedBytesCount);

            string encodedText = Encoding.ASCII.GetString(encodedBytes.Slice(0, expectedWritten).ToArray());
            Assert.Equal(expectedText, encodedText);
        }

        [Theory]
        [InlineData(1, "", 0, 0)]
        [InlineData(2, "", 0, 0)]
        [InlineData(3, "AQID", 3, 4)]
        [InlineData(4, "AQID", 3, 4)]
        [InlineData(5, "AQID", 3, 4)]
        [InlineData(6, "AQIDBAUG", 6, 8)]
        [InlineData(7, "AQIDBAUG", 6, 8)]
        public void BasicEncodingWithFinalBlockFalseKnownInput(int numBytes, string expectedText, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                source[i] = (byte)(i + 1);
            }
            Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];

            OperationStatus expectedStatus = numBytes % 3 == 0 ? OperationStatus.Done : OperationStatus.NeedMoreData;
            Assert.Equal(expectedStatus, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, encodedBytesCount);

            string encodedText = Encoding.ASCII.GetString(encodedBytes.Slice(0, expectedWritten).ToArray());
            Assert.Equal(expectedText, encodedText);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeEmptySpan(bool isFinalBlock)
        {
            Span<byte> source = Span<byte>.Empty;
            Span<byte> encodedBytes = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock));
            Assert.Equal(0, consumed);
            Assert.Equal(0, encodedBytesCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodingOutputTooSmall(bool isFinalBlock)
        {
            for (int numBytes = 4; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                Span<byte> encodedBytes = new byte[4];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int written, isFinalBlock));
                int expectedConsumed = 3;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(encodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodingOutputTooSmallRetry(bool isFinalBlock)
        {
            Span<byte> source = new byte[750];
            Base64TestHelper.InitializeBytes(source);

            int outputSize = 320;
            int requiredSize = Base64.GetMaxEncodedToUtf8Length(source.Length);

            Span<byte> encodedBytes = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int written, isFinalBlock));
            int expectedConsumed = encodedBytes.Length / 4 * 3;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(encodedBytes.Length, written);
            Assert.True(Base64TestHelper.VerifyEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));

            encodedBytes = new byte[requiredSize - outputSize];
            source = source.Slice(consumed);
            Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(source, encodedBytes, out consumed, out written, isFinalBlock));
            expectedConsumed = encodedBytes.Length / 4 * 3;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(encodedBytes.Length, written);
            Assert.True(Base64TestHelper.VerifyEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop]
        public void EncodeTooLargeSpan(bool isFinalBlock)
        {
            if (!Environment.Is64BitProcess)
                return;

            bool allocatedFirst = false;
            bool allocatedSecond = false;
            IntPtr memBlockFirst = IntPtr.Zero;
            IntPtr memBlockSecond = IntPtr.Zero;

            // int.MaxValue - (int.MaxValue % 4) => 2147483644, largest multiple of 4 less than int.MaxValue
            // CLR default limit of 2 gigabytes (GB).
            // 1610612734, larger than MaximumEncodeLength, requires output buffer of size 2147483648 (which is > int.MaxValue)
            const int sourceCount = (int.MaxValue >> 2) * 3 + 1;
            const int encodedCount = 2000000000;

            try
            {
                allocatedFirst = AllocationHelper.TryAllocNative((IntPtr)sourceCount, out memBlockFirst);
                allocatedSecond = AllocationHelper.TryAllocNative((IntPtr)encodedCount, out memBlockSecond);
                if (allocatedFirst && allocatedSecond)
                {
                    unsafe
                    {
                        var source = new Span<byte>(memBlockFirst.ToPointer(), sourceCount);
                        var encodedBytes = new Span<byte>(memBlockSecond.ToPointer(), encodedCount);

                        Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock));
                        Assert.Equal((encodedBytes.Length >> 2) * 3, consumed); // encoding 1500000000 bytes fits into buffer of 2000000000 bytes
                        Assert.Equal(encodedBytes.Length, encodedBytesCount);
                    }
                }
            }
            finally
            {
                if (allocatedFirst)
                    AllocationHelper.ReleaseNative(ref memBlockFirst);
                if (allocatedSecond)
                    AllocationHelper.ReleaseNative(ref memBlockSecond);
            }
        }

        [Fact]
        public void GetMaxEncodedLength()
        {
            // (int.MaxValue - 4)/(4/3) => 1610612733, otherwise integer overflow
            int[] input = { 0, 1, 2, 3, 4, 5, 6, 1610612728, 1610612729, 1610612730, 1610612731, 1610612732, 1610612733 };
            int[] expected = { 0, 4, 4, 4, 8, 8, 8, 2147483640, 2147483640, 2147483640, 2147483644, 2147483644, 2147483644 };
            for (int i = 0; i < input.Length; i++)
            {
                Assert.Equal(expected[i], Base64.GetMaxEncodedToUtf8Length(input[i]));
            }

            // integer overflow
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxEncodedToUtf8Length(1610612734));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxEncodedToUtf8Length(int.MaxValue));

            // negative input
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxEncodedToUtf8Length(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxEncodedToUtf8Length(int.MinValue));
        }

        [Fact]
        public void EncodeInPlace()
        {
            const int numberOfBytes = 15;
            Span<byte> testBytes = new byte[numberOfBytes / 3 * 4]; // slack since encoding inflates the data
            Base64TestHelper.InitializeBytes(testBytes);

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest++)
            {
                var expectedText = Convert.ToBase64String(testBytes.Slice(0, numberOfBytesToTest).ToArray());

                Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8InPlace(testBytes, numberOfBytesToTest, out int bytesWritten));
                Assert.Equal(Base64.GetMaxEncodedToUtf8Length(numberOfBytesToTest), bytesWritten);

                var encodedText = Encoding.ASCII.GetString(testBytes.Slice(0, bytesWritten).ToArray());
                Assert.Equal(expectedText, encodedText);
            }
        }

        [Fact]
        public void EncodeInPlaceOutputTooSmall()
        {
            byte[] testBytes = { 1, 2, 3 };

            for (int numberOfBytesToTest = 1; numberOfBytesToTest <= testBytes.Length; numberOfBytesToTest++)
            {
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToUtf8InPlace(testBytes, numberOfBytesToTest, out int bytesWritten));
                Assert.Equal(0, bytesWritten);
            }
        }

        [Fact]
        public void EncodeInPlaceDataLengthTooLarge()
        {
            byte[] testBytes = { 1, 2, 3 };
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToUtf8InPlace(testBytes, testBytes.Length + 1, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }
    }
}
