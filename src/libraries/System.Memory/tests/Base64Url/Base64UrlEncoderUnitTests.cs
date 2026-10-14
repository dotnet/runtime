// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.SpanTests;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UrlEncoderUnitTests
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
                Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(sourceBytes.Length)];
                Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(sourceBytes, encodedBytes, out int consumed, out int encodedBytesCount));
                Assert.Equal(sourceBytes.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);
                Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(sourceBytes.Length, encodedBytes.Length, sourceBytes, encodedBytes));

                int decodedLength = Base64Url.GetMaxDecodedLength(encodedBytes.Length);
                Assert.True(sourceBytes.Length <= decodedLength);
                Span<byte> decodedBytes = new byte[decodedLength];
                Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(encodedBytes, decodedBytes, out consumed, out int decodedByteCount));
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

                Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(source.Length)];
                OperationStatus result = Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount);
                Assert.Equal(OperationStatus.Done, result);
                Assert.Equal(source.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);
                Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(source.Length, encodedBytes.Length, source, encodedBytes));
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

                Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(source.Length)];
                int expectedConsumed = source.Length / 3 * 3; // only consume closest multiple of three since isFinalBlock is false
                int expectedWritten = source.Length / 3 * 4;

                // The constant random seed guarantees that both states are tested.
                OperationStatus expectedStatus = numBytes % 3 == 0 ? OperationStatus.Done : OperationStatus.NeedMoreData;
                Assert.Equal(expectedStatus, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, encodedBytesCount);
                Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(expectedConsumed, expectedWritten, source, encodedBytes));
            }
        }

        [Theory]
        [InlineData(1, "AQ")]
        [InlineData(2, "AQI")]
        [InlineData(3, "AQID")]
        [InlineData(4, "AQIDBA")]
        [InlineData(5, "AQIDBAU")]
        [InlineData(6, "AQIDBAUG")]
        [InlineData(7, "AQIDBAUGBw")]
        public void BasicEncodingWithFinalBlockTrueKnownInput(int numBytes, string expectedText)
        {
            int expectedConsumed = numBytes;
            int expectedWritten = expectedText.Length;

            Span<byte> source = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                source[i] = (byte)(i + 1);
            }
            Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount));
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
            Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(source.Length)];

            OperationStatus expectedStatus = numBytes % 3 == 0 ? OperationStatus.Done : OperationStatus.NeedMoreData;
            Assert.Equal(expectedStatus, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock: false));
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
            Span<byte> encodedBytes = new byte[Base64Url.GetEncodedLength(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock));
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
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int written, isFinalBlock));
                int expectedConsumed = 3;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(encodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));
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
            int requiredSize = Base64Url.GetEncodedLength(source.Length);

            Span<byte> encodedBytes = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int written, isFinalBlock));
            int expectedConsumed = encodedBytes.Length / 4 * 3;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(encodedBytes.Length, written);
            Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));

            encodedBytes = new byte[requiredSize - outputSize];
            source = source.Slice(consumed);
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source, encodedBytes, out consumed, out written, isFinalBlock));
            expectedConsumed = encodedBytes.Length / 4 * 3;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(encodedBytes.Length, written);
            Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(expectedConsumed, encodedBytes.Length, source, encodedBytes));
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

                        Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, encodedBytes, out int consumed, out int encodedBytesCount, isFinalBlock));
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
        public void GetEncodedLength()
        {
            // (int.MaxValue - 4)/(4/3) => 1610612733, otherwise integer overflow
            int[]    input = { 0, 1, 2, 3, 4, 5, 6, 1610612728, 1610612729, 1610612730, 1610612731, 1610612732, 1610612733 };
            int[] expected = { 0, 2, 3, 4, 6, 7, 8, 2147483638, 2147483639, 2147483640, 2147483642, 2147483643, 2147483644 };
            for (int i = 0; i < input.Length; i++)
            {
                Assert.Equal(expected[i], Base64Url.GetEncodedLength(input[i]));
            }

            // integer overflow
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(1610612734));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(int.MaxValue));

            // negative input
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(int.MinValue));
        }

        [Fact]
        public void TryEncodeInPlace()
        {
            const int numberOfBytes = 15;
            Span<byte> testBytes = new byte[numberOfBytes / 3 * 4]; // slack since encoding inflates the data
            Base64TestHelper.InitializeBytes(testBytes);

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest++)
            {
                var expectedText = Convert.ToBase64String(testBytes.Slice(0, numberOfBytesToTest).ToArray())
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                Assert.True(Base64Url.TryEncodeToUtf8InPlace(testBytes, numberOfBytesToTest, out int bytesWritten));
                Assert.Equal(Base64Url.GetEncodedLength(numberOfBytesToTest), bytesWritten);

                var encodedText = Encoding.ASCII.GetString(testBytes.Slice(0, bytesWritten).ToArray());
                Assert.Equal(expectedText, encodedText);
            }
        }

        [Fact]
        public void TryEncodeInPlaceOutputTooSmall()
        {
            byte[] testBytes = { 1, 2, 3 };

            Assert.False(Base64Url.TryEncodeToUtf8InPlace(testBytes, testBytes.Length, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TryEncodeToUtf8()
        {
            const int numberOfBytes = 15;
            Span<byte> testBytes = new byte[numberOfBytes / 3 * 4]; // slack since encoding inflates the data
            Base64TestHelper.InitializeBytes(testBytes);

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest++)
            {
                ReadOnlySpan<byte> source = testBytes.Slice(0, numberOfBytesToTest);
                Span<byte> destination = new byte[Base64Url.GetEncodedLength(numberOfBytesToTest)];
                Assert.True(Base64Url.TryEncodeToUtf8(source, destination, out int bytesWritten));
                Assert.Equal(destination.Length, bytesWritten);
                Assert.True(source.SequenceEqual(Base64Url.DecodeFromUtf8(destination).AsSpan()));
            }
        }

        [Theory]
        [InlineData(1, "AQ")]
        [InlineData(2, "AQI")]
        [InlineData(3, "AQID")]
        [InlineData(4, "AQIDBA")]
        [InlineData(5, "AQIDBAU")]
        [InlineData(6, "AQIDBAUG")]
        [InlineData(7, "AQIDBAUGBw")]
        public void TryEncodeToUtf8EncodeUpToDestinationSize(int numBytes, string expectedText)
        {
            int expectedWritten = expectedText.Length;

            Span<byte> source = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                source[i] = (byte)(i + 1);
            }
            Span<byte> destination = new byte[6];

            if (numBytes < 5)
            {
                Assert.True(Base64Url.TryEncodeToUtf8(source, destination, out int bytesWritten));
                Assert.Equal(expectedWritten, bytesWritten);
                string encodedText = Encoding.ASCII.GetString(destination.Slice(0, expectedWritten).ToArray());
                Assert.Equal(expectedText, encodedText);
            }
            else
            {
                Assert.False(Base64Url.TryEncodeToUtf8(source, destination, out int bytesWritten));
                Assert.Equal(4, bytesWritten);
                string encodedText = Encoding.ASCII.GetString(destination.Slice(0, 4).ToArray());
                Assert.Equal(expectedText.Substring(0, 4), encodedText);
            }
        }

#if NET
        private static readonly Base64UrlFormattingOptions[] s_invalidOptions =
        {
            (Base64UrlFormattingOptions)1, // reserved
            (Base64UrlFormattingOptions)3,
            (Base64UrlFormattingOptions)4,
            (Base64UrlFormattingOptions)(-1),
            (Base64UrlFormattingOptions)int.MinValue,
        };

        private static byte[] SequentialBytes(int numBytes)
        {
            byte[] source = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                source[i] = (byte)(i + 1);
            }

            return source;
        }

        private static string PaddedReference(byte[] source) =>
            Convert.ToBase64String(source).Replace('+', '-').Replace('/', '_');

        [Fact]
        public void GetEncodedLengthWithPadding()
        {
            int[]    input = { 0, 1, 2, 3, 4, 5, 6, 1610612728, 1610612729, 1610612730, 1610612731, 1610612732, 1610612733 };
            int[] expected = { 0, 4, 4, 4, 8, 8, 8, 2147483640, 2147483640, 2147483640, 2147483644, 2147483644, 2147483644 };
            for (int i = 0; i < input.Length; i++)
            {
                Assert.Equal(expected[i], Base64Url.GetEncodedLength(input[i], Base64UrlFormattingOptions.IncludePadding));
                Assert.Equal(Base64Url.GetEncodedLength(input[i]), Base64Url.GetEncodedLength(input[i], Base64UrlFormattingOptions.None));
            }

            // integer overflow
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(1610612734, Base64UrlFormattingOptions.IncludePadding));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(int.MaxValue, Base64UrlFormattingOptions.IncludePadding));

            // negative input
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(-1, Base64UrlFormattingOptions.IncludePadding));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetEncodedLength(int.MinValue, Base64UrlFormattingOptions.IncludePadding));
        }

        [Fact]
        public void InvalidOptionsThrow()
        {
            foreach (Base64UrlFormattingOptions options in s_invalidOptions)
            {
                AssertInvalidOptions(() => Base64Url.GetEncodedLength(3, options));
                AssertInvalidOptions(() => Base64Url.EncodeToString(new byte[3], options));
                AssertInvalidOptions(() => Base64Url.EncodeToUtf8(new byte[3], options));
                AssertInvalidOptions(() => Base64Url.EncodeToChars(new byte[3], options));
                AssertInvalidOptions(() => Base64Url.EncodeToUtf8(new byte[3], new byte[4], options));
                AssertInvalidOptions(() => Base64Url.EncodeToChars(new byte[3], new char[4], options));
                AssertInvalidOptions(() => Base64Url.TryEncodeToUtf8(new byte[3], new byte[4], out _, options));
                AssertInvalidOptions(() => Base64Url.TryEncodeToChars(new byte[3], new char[4], out _, options));
                AssertInvalidOptions(() => Base64Url.TryEncodeToUtf8InPlace(new byte[4], 3, out _, options));
                AssertInvalidOptions(() => Base64Url.EncodeToUtf8(new byte[3], new byte[4], out _, out _, options));
                AssertInvalidOptions(() => Base64Url.EncodeToChars(new byte[3], new char[4], out _, out _, options));
                AssertInvalidOptions(() => Base64Url.EncodeToUtf8(new byte[3], new byte[4], out _, out _, options, isFinalBlock: false));
                AssertInvalidOptions(() => Base64Url.EncodeToChars(new byte[3], new char[4], out _, out _, options, isFinalBlock: false));

                // Validation happens before the empty-input fast path.
                AssertInvalidOptions(() => Base64Url.EncodeToString(Array.Empty<byte>(), options));
                AssertInvalidOptions(() => Base64Url.EncodeToUtf8(Array.Empty<byte>(), Array.Empty<byte>(), out _, out _, options));
                AssertInvalidOptions(() => Base64Url.TryEncodeToUtf8InPlace(Array.Empty<byte>(), 0, out _, options));
            }

            static void AssertInvalidOptions(Action action)
            {
                ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(action);
                Assert.Equal("options", ex.ParamName);
            }
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "AQ==")]
        [InlineData(2, "AQI=")]
        [InlineData(3, "AQID")]
        [InlineData(4, "AQIDBA==")]
        [InlineData(5, "AQIDBAU=")]
        [InlineData(6, "AQIDBAUG")]
        [InlineData(7, "AQIDBAUGBw==")]
        public void EncodeWithPaddingKnownInput(int numBytes, string expectedText)
        {
            byte[] source = SequentialBytes(numBytes);
            byte[] expectedUtf8 = Encoding.ASCII.GetBytes(expectedText);
            int expectedLength = expectedText.Length;

            Assert.Equal(expectedLength, Base64Url.GetEncodedLength(numBytes, Base64UrlFormattingOptions.IncludePadding));

            Assert.Equal(expectedText, Base64Url.EncodeToString(source, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedUtf8, Base64Url.EncodeToUtf8(source, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedText.ToCharArray(), Base64Url.EncodeToChars(source, Base64UrlFormattingOptions.IncludePadding));

            byte[] utf8 = new byte[expectedLength];
            Assert.Equal(expectedLength, Base64Url.EncodeToUtf8(source, utf8, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedUtf8, utf8);

            char[] chars = new char[expectedLength];
            Assert.Equal(expectedLength, Base64Url.EncodeToChars(source, chars, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedText, new string(chars));

            utf8 = new byte[expectedLength];
            Assert.True(Base64Url.TryEncodeToUtf8(source, utf8, out int written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedLength, written);
            Assert.Equal(expectedUtf8, utf8);

            chars = new char[expectedLength];
            Assert.True(Base64Url.TryEncodeToChars(source, chars, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedLength, written);
            Assert.Equal(expectedText, new string(chars));

            utf8 = new byte[expectedLength];
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(numBytes, consumed);
            Assert.Equal(expectedLength, written);
            Assert.Equal(expectedUtf8, utf8);

            chars = new char[expectedLength];
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(numBytes, consumed);
            Assert.Equal(expectedLength, written);
            Assert.Equal(expectedText, new string(chars));

            byte[] buffer = new byte[expectedLength];
            source.CopyTo(buffer, 0);
            Assert.True(Base64Url.TryEncodeToUtf8InPlace(buffer, numBytes, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedLength, written);
            Assert.Equal(expectedUtf8, buffer);

            // Padded output decodes back to the input.
            Assert.Equal(source, Base64Url.DecodeFromUtf8(expectedUtf8));
            Assert.Equal(source, Base64Url.DecodeFromChars(expectedText.AsSpan()));
        }

        [Fact]
        public void EncodeWithPaddingSha256Digest()
        {
            // SHA-256("hello")
            byte[] hash =
            {
                0x2c, 0xf2, 0x4d, 0xba, 0x5f, 0xb0, 0xa3, 0x0e, 0x26, 0xe8, 0x3b, 0x2a, 0xc5, 0xb9, 0xe2, 0x9e,
                0x1b, 0x16, 0x1e, 0x5c, 0x1f, 0xa7, 0x42, 0x5e, 0x73, 0x04, 0x33, 0x62, 0x93, 0x8b, 0x98, 0x24,
            };

            Assert.Equal("LPJNul-wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=", Base64Url.EncodeToString(hash, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal("LPJNul-wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ", Base64Url.EncodeToString(hash, Base64UrlFormattingOptions.None));
            Assert.Equal("LPJNul-wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ", Base64Url.EncodeToString(hash));
        }

        [Fact]
        public void EncodeWithPaddingMatchesConvert()
        {
            // Lengths through 300 cover the scalar tail after every vectorized path (Vector128, AVX2, AVX-512).
            for (int numBytes = 0; numBytes <= 300; numBytes++)
            {
                byte[] source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);
                AssertMatchesConvert(source);
            }

            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                byte[] source = new byte[rnd.Next(1000, 1000 * 1000)];
                Base64TestHelper.InitializeBytes(source, i);
                AssertMatchesConvert(source);
            }

            static void AssertMatchesConvert(byte[] source)
            {
                string expected = PaddedReference(source);

                Assert.Equal(expected, Base64Url.EncodeToString(source, Base64UrlFormattingOptions.IncludePadding));
                Assert.Equal(expected, Encoding.ASCII.GetString(Base64Url.EncodeToUtf8(source, Base64UrlFormattingOptions.IncludePadding)));
                Assert.Equal(expected, new string(Base64Url.EncodeToChars(source, Base64UrlFormattingOptions.IncludePadding)));

                byte[] buffer = new byte[Base64Url.GetEncodedLength(source.Length, Base64UrlFormattingOptions.IncludePadding)];
                source.CopyTo(buffer, 0);
                Assert.True(Base64Url.TryEncodeToUtf8InPlace(buffer, source.Length, out int written, Base64UrlFormattingOptions.IncludePadding));
                Assert.Equal(buffer.Length, written);
                Assert.Equal(expected, Encoding.ASCII.GetString(buffer));

                Assert.Equal(expected.TrimEnd('='), Base64Url.EncodeToString(source, Base64UrlFormattingOptions.None));
                Assert.Equal(Base64Url.EncodeToString(source), Base64Url.EncodeToString(source, Base64UrlFormattingOptions.None));

                Assert.Equal(source, Base64Url.DecodeFromChars(expected.AsSpan()));
            }
        }

        [Theory]
        [InlineData(1, "", 0, 0)]
        [InlineData(2, "", 0, 0)]
        [InlineData(3, "AQID", 3, 4)]
        [InlineData(4, "AQID", 3, 4)]
        [InlineData(5, "AQID", 3, 4)]
        [InlineData(6, "AQIDBAUG", 6, 8)]
        [InlineData(7, "AQIDBAUG", 6, 8)]
        public void EncodeWithPaddingFinalBlockFalseWritesNoPadding(int numBytes, string expectedText, int expectedConsumed, int expectedWritten)
        {
            byte[] source = SequentialBytes(numBytes);
            OperationStatus expectedStatus = numBytes % 3 == 0 ? OperationStatus.Done : OperationStatus.NeedMoreData;

            byte[] utf8 = new byte[Base64Url.GetEncodedLength(numBytes, Base64UrlFormattingOptions.IncludePadding)];
            Assert.Equal(expectedStatus, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out int written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);
            Assert.Equal(expectedText, Encoding.ASCII.GetString(utf8, 0, written));

            char[] chars = new char[utf8.Length];
            Assert.Equal(expectedStatus, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);
            Assert.Equal(expectedText, new string(chars, 0, written));

            // Finishing with isFinalBlock: true pads the remainder.
            string expectedTail = PaddedReference(source).Substring(expectedWritten);
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source.AsSpan(consumed), utf8.AsSpan(written), out consumed, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(numBytes - expectedConsumed, consumed);
            Assert.Equal(expectedTail.Length, written);
            Assert.Equal(PaddedReference(source), Encoding.ASCII.GetString(utf8));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(100)]
        public void EncodeWithPaddingFinalBlockFalseIgnoresPaddingInSizing(int numBytes)
        {
            // With isFinalBlock: false the option has no effect, so a destination sized for the
            // unpadded output must get the same status and counts as the unpadded overload.
            byte[] source = SequentialBytes(numBytes);
            int unpaddedLength = Base64Url.GetEncodedLength(numBytes);
            Assert.True(unpaddedLength < Base64Url.GetEncodedLength(numBytes, Base64UrlFormattingOptions.IncludePadding));

            byte[] expectedUtf8 = new byte[unpaddedLength];
            OperationStatus expectedStatus = Base64Url.EncodeToUtf8(source, expectedUtf8, out int expectedConsumed, out int expectedWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.NeedMoreData, expectedStatus);

            byte[] utf8 = new byte[unpaddedLength];
            Assert.Equal(expectedStatus, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out int written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);
            Assert.Equal(expectedUtf8, utf8);

            char[] chars = new char[unpaddedLength];
            Assert.Equal(expectedStatus, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);
            Assert.Equal(Encoding.ASCII.GetString(expectedUtf8, 0, expectedWritten), new string(chars, 0, written));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        public void EncodeWithPaddingDestinationTooSmall(int numBytes)
        {
            byte[] source = SequentialBytes(numBytes);
            int paddedLength = Base64Url.GetEncodedLength(numBytes, Base64UrlFormattingOptions.IncludePadding);
            int unpaddedLength = Base64Url.GetEncodedLength(numBytes);
            Assert.True(unpaddedLength < paddedLength);

            // Enough room for the unpadded output, but not for the padding.
            int expectedConsumed = numBytes / 3 * 3;
            int expectedWritten = numBytes / 3 * 4;

            byte[] utf8 = new byte[paddedLength - 1];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out int written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);

            char[] chars = new char[paddedLength - 1];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, written);

            Assert.False(Base64Url.TryEncodeToUtf8(source, new byte[paddedLength - 1], out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedWritten, written);
            Assert.False(Base64Url.TryEncodeToChars(source, new char[paddedLength - 1], out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(expectedWritten, written);

            Assert.Throws<ArgumentException>(() => Base64Url.EncodeToUtf8(source, new byte[paddedLength - 1], Base64UrlFormattingOptions.IncludePadding));
            Assert.Throws<ArgumentException>(() => Base64Url.EncodeToChars(source, new char[paddedLength - 1], Base64UrlFormattingOptions.IncludePadding));

            byte[] buffer = new byte[paddedLength - 1];
            source.CopyTo(buffer, 0);
            Assert.False(Base64Url.TryEncodeToUtf8InPlace(buffer, numBytes, out written, Base64UrlFormattingOptions.IncludePadding));
            Assert.Equal(0, written);

            // The same destination is large enough without padding.
            Assert.True(Base64Url.TryEncodeToUtf8(source, new byte[paddedLength - 1], out written, Base64UrlFormattingOptions.None));
            Assert.Equal(unpaddedLength, written);
        }

        [Fact]
        public void TryEncodeInPlaceWithPadding()
        {
            const int numberOfBytes = 15;
            Span<byte> testBytes = new byte[numberOfBytes / 3 * 4]; // slack since encoding inflates the data
            Base64TestHelper.InitializeBytes(testBytes);

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest++)
            {
                string expectedText = PaddedReference(testBytes.Slice(0, numberOfBytesToTest).ToArray());

                Assert.True(Base64Url.TryEncodeToUtf8InPlace(testBytes, numberOfBytesToTest, out int bytesWritten, Base64UrlFormattingOptions.IncludePadding));
                Assert.Equal(Base64Url.GetEncodedLength(numberOfBytesToTest, Base64UrlFormattingOptions.IncludePadding), bytesWritten);

                string encodedText = Encoding.ASCII.GetString(testBytes.Slice(0, bytesWritten).ToArray());
                Assert.Equal(expectedText, encodedText);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodingOutputTooSmallWithPadding(bool isFinalBlock)
        {
            for (int numBytes = 4; numBytes < 20; numBytes++)
            {
                byte[] source = SequentialBytes(numBytes);
                string expectedText = PaddedReference(source.AsSpan(0, 3).ToArray());

                byte[] utf8 = new byte[4];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out int written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
                Assert.Equal(3, consumed);
                Assert.Equal(4, written);
                Assert.Equal(expectedText, Encoding.ASCII.GetString(utf8));

                char[] chars = new char[4];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
                Assert.Equal(3, consumed);
                Assert.Equal(4, written);
                Assert.Equal(expectedText, new string(chars));
            }
        }

        [Theory]
        [InlineData(750, true)]
        [InlineData(750, false)]
        [InlineData(751, true)]
        [InlineData(752, true)]
        public void EncodingOutputTooSmallRetryWithPadding(int numBytes, bool isFinalBlock)
        {
            byte[] source = SequentialBytes(numBytes);
            string expectedText = PaddedReference(source);
            int requiredSize = Base64Url.GetEncodedLength(numBytes, Base64UrlFormattingOptions.IncludePadding);
            Assert.Equal(expectedText.Length, requiredSize);

            const int outputSize = 320;
            const int firstConsumed = outputSize / 4 * 3;

            byte[] utf8 = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToUtf8(source, utf8, out int consumed, out int written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
            Assert.Equal(firstConsumed, consumed);
            Assert.Equal(outputSize, written);
            Assert.Equal(expectedText.Substring(0, outputSize), Encoding.ASCII.GetString(utf8));

            byte[] utf8Rest = new byte[requiredSize - outputSize];
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(source.AsSpan(consumed), utf8Rest, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
            Assert.Equal(numBytes - firstConsumed, consumed);
            Assert.Equal(utf8Rest.Length, written);
            Assert.Equal(expectedText.Substring(outputSize), Encoding.ASCII.GetString(utf8Rest));

            char[] chars = new char[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToChars(source, chars, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
            Assert.Equal(firstConsumed, consumed);
            Assert.Equal(outputSize, written);
            Assert.Equal(expectedText.Substring(0, outputSize), new string(chars));

            char[] charsRest = new char[requiredSize - outputSize];
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToChars(source.AsSpan(consumed), charsRest, out consumed, out written, Base64UrlFormattingOptions.IncludePadding, isFinalBlock));
            Assert.Equal(numBytes - firstConsumed, consumed);
            Assert.Equal(charsRest.Length, written);
            Assert.Equal(expectedText.Substring(outputSize), new string(charsRest));
        }
#endif
    }
}
