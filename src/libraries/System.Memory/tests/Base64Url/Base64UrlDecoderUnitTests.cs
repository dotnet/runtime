// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UrlDecoderUnitTests : Base64TestBase
    {
        [Fact]
        public void BasicDecoding()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 == 1); // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(source.Length, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Fact]
        public void BasicDecodingByteArrayReturnOverload()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 == 1); // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = Base64Url.DecodeFromUtf8(source);
                Assert.Equal(decodedBytes.Length, Base64Url.GetMaxDecodedLength(source.Length));
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Fact]
        public void BasicDecodingInvalidInputLength()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 != 1);    // ensure we have a invalid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                int expectedConsumed = numBytes / 4 * 4;    // decode input up to the closest multiple of 4
                int expectedDecoded = expectedConsumed / 4 * 3;

                Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedDecoded, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, expectedDecoded, source, decodedBytes));
            }
        }

        [Fact]
        public void BasicDecodingInvalidInputWithOneByteData()
        {
            // Only 1 byte of data is invalid, 2 - 3 bytes of data are valid as padding is optional
            ReadOnlySpan<byte> source = stackalloc byte[] { (byte)'A' };
            Span<byte> decodedBytes = stackalloc byte[128];

            Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(0, consumed);
            Assert.Equal(0, decodedByteCount);
        }

        [Fact]
        public void BasicDecodingWithFinalBlockFalse()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 != 0);    // ensure we have a complete length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                int expectedConsumed = source.Length / 4 * 4; // only consume closest multiple of four since isFinalBlock is false

                Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Fact]
        public void BasicDecodingWithFinalBlockFalseInvalidInputLength()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 == 0);    // ensure we have a incomplete length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                int expectedConsumed = source.Length / 4 * 4; // only consume closest multiple of four since isFinalBlock is false
                int expectedDecoded = expectedConsumed / 4 * 3;

                Assert.Equal(OperationStatus.NeedMoreData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedDecoded, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedByteCount, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodeEmptySpan(bool isFinalBlock)
        {
            Span<byte> source = Span<byte>.Empty;
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
            Assert.Equal(0, consumed);
            Assert.Equal(0, decodedByteCount);
        }

        [Fact]
        public void DecodeGuid()
        {
            Span<byte> source = new byte[22]; // For Base64Url padding ignored
            Span<byte> providedBytes = Guid.NewGuid().ToByteArray();
            Base64Url.EncodeToUtf8(providedBytes, source);

            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
            Assert.Equal(16, Base64Url.DecodeFromUtf8(source, decodedBytes));
            Assert.True(providedBytes.SequenceEqual(decodedBytes));
        }

        [Fact]
        public void DecodingOutputTooSmall()
        {
            for (int numBytes = 5; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[3];
                int consumed, written;
                if (numBytes >= 6)
                {
                    Assert.True(OperationStatus.DestinationTooSmall ==
                        Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out written), "Number of Input Bytes: " + numBytes);
                }
                else
                {
                    Assert.True(OperationStatus.InvalidData ==
                        Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out written), "Number of Input Bytes: " + numBytes);
                }
                int expectedConsumed = 4;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }

            // Output too small even with padding characters in the input
            {
                Span<byte> source = new byte[12];
                Base64TestHelper.InitializeUrlDecodableBytes(source);
                source[10] = Base64TestHelper.EncodingPad;
                source[11] = Base64TestHelper.EncodingPad;

                Span<byte> decodedBytes = new byte[6];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }

            {
                Span<byte> source = new byte[12];
                Base64TestHelper.InitializeUrlDecodableBytes(source);
                source[11] = Base64TestHelper.EncodingPad;

                Span<byte> decodedBytes = new byte[7];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(6, written);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, 6, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodingOutputTooSmallWithFinalBlockTrueFalse(bool isFinalBlock)
        {
            for (int numBytes = 8; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[4];
                int consumed, written;
                Assert.True(OperationStatus.DestinationTooSmall ==
                    Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out written, isFinalBlock: isFinalBlock), "Number of Input Bytes: " + numBytes);
                int expectedConsumed = 4;
                int expectedWritten = 3;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, written);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodingOutputTooSmallRetry(bool isFinalBlock)
        {
            Span<byte> source = new byte[1000];
            Base64TestHelper.InitializeUrlDecodableBytes(source);

            int outputSize = 240;
            int requiredSize = Base64Url.GetMaxDecodedLength(source.Length);

            Span<byte> decodedBytes = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
            int expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));

            decodedBytes = new byte[requiredSize - outputSize];
            source = source.Slice(consumed);
            Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));
            expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("AQ==", 1)]
        [InlineData("AQI=", 2)]
        [InlineData("AQID", 3)]
        [InlineData("AQIDBA%%", 4)]
        [InlineData("AQIDBAU=", 5)]
        [InlineData("AQIDBAUG", 6)]
        public void BasicDecodingWithFinalBlockTrueKnownInputDone(string inputString, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(expectedWritten, Base64Url.DecodeFromUtf8(source, decodedBytes));
            Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(inputString.Length, expectedWritten, source, decodedBytes));
        }

        [Theory]
        [InlineData("A", 0, 0, OperationStatus.InvalidData)]
        [InlineData("A===", 0, 0, OperationStatus.InvalidData)]
        [InlineData("A==", 0, 0, OperationStatus.InvalidData)]
        [InlineData("A=", 0, 0, OperationStatus.InvalidData)]
        [InlineData("AQ", 2, 1, OperationStatus.Done)]  // Padding is optional
        [InlineData("AQI", 3, 2, OperationStatus.Done)]
        [InlineData("AQIDBA", 6, 4, OperationStatus.Done)]
        [InlineData("AQIDBAU", 7, 5, OperationStatus.Done)]
        public void BasicDecodingWithFinalBlockTrueInputWithoutPaddingOrInvalidData(string inputString, int expectedConsumed, int expectedWritten, OperationStatus expectedStatus)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(expectedStatus, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Theory]
        [InlineData("A", 0, false)]
        [InlineData("A===", 0, false)]
        [InlineData("A==", 0, false)]
        [InlineData("A=", 0, false)]
        [InlineData("AQ", 1, true)]  // Padding is optional
        [InlineData("AQI", 2, true)]
        [InlineData("AQID", 3, true)]
        [InlineData("AQIDB", 3, false)]
        [InlineData("AQIDBA", 4, true)]
        [InlineData("AQIDBAU", 5, true)]
        [InlineData("AQ==", 1, true)]
        [InlineData("AQI%", 2, true)]
        [InlineData("AQIDBA%%", 4, true)]
        [InlineData("z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo\u5948==", 33, false)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789==", 0, false)]
        public void TryDecodeFromUtf8VariousInput(string inputString, int expectedWritten, bool succeeds)
        {
            byte[] source = Encoding.ASCII.GetBytes(inputString);
            byte[] decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            if (succeeds)
            {
                Assert.True(Base64Url.TryDecodeFromUtf8(source, decodedBytes, out int bytesWritten));
                Assert.Equal(expectedWritten, bytesWritten);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(inputString.Length, expectedWritten, source, decodedBytes));
            }
            else
            {
                Assert.Throws<FormatException>(() => Base64Url.TryDecodeFromUtf8(source, decodedBytes, out _));
            }
        }

        [Theory]
        [InlineData("\u5948cz_T", 0, 0)]                                              // scalar code-path
        [InlineData("z_Ta123\u5948", 4, 3)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw==", 0, 0)]                          // Vector128 code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw\u5948==", 20, 15)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo==", 0, 0)]  // Vector256 / AVX code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo\u5948==", 44, 33)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789==", 0, 0)]  // Vector512 / Avx512Vbmi code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789\u5948==", 92, 69)]
        public void BasicDecodingNonAsciiInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.UTF8.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
        }

        [Theory]
        [InlineData("AQID", 3)]
        [InlineData("AQIDBAUG", 6)]
        public void BasicDecodingWithFinalBlockFalseKnownInputDone(string inputString, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            int expectedConsumed = inputString.Length;
            Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("A", 0, 0)]
        [InlineData("AQ", 0, 0)] // when FinalBlock: false incomplete bytes ignored
        [InlineData("AQI", 0, 0)] 
        [InlineData("AQIDB", 4, 3)]
        [InlineData("AQIDBA", 4, 3)]
        [InlineData("AQIDBAU", 4, 3)]
        public void BasicDecodingWithFinalBlockFalseKnownInputNeedMoreData(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.NeedMoreData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, decodedByteCount, source, decodedBytes));
        }

        [Theory]
        [InlineData("AQ==", 0, 0)]
        [InlineData("AQI%", 0, 0)]
        [InlineData("AQIDBA==", 4, 3)]
        [InlineData("AQIDBAU=", 4, 3)]
        public void BasicDecodingWithFinalBlockFalseKnownInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodingInvalidBytes(bool isFinalBlock)
        {
            // Invalid Bytes:
            // 0-44
            // 46=47
            // 58-64
            // 91-94, 96
            // 123-255
            byte[] invalidBytes = Base64TestHelper.UrlInvalidBytes;
            Assert.Equal(byte.MaxValue + 1 - 64, invalidBytes.Length); // 192

            for (int j = 0; j < 8; j++)
            {
                Span<byte> source = "2222PPPP"u8.ToArray(); // valid input
                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

                for (int i = 0; i < invalidBytes.Length; i++)
                {
                    // Don't test padding (byte 61 i.e. '=' or '%'), which is tested in DecodingInvalidBytesPadding
                    // Don't test chars to be ignored (spaces: 9, 10, 13, 32 i.e. '\n', '\t', '\r', ' ')
                    if (invalidBytes[i] == Base64TestHelper.EncodingPad ||
                        invalidBytes[i] == Base64TestHelper.UrlEncodingPad ||
                        Base64TestHelper.IsByteToBeIgnored(invalidBytes[i]))
                    {
                        continue;
                    }

                    // replace one byte with an invalid input
                    source[j] = invalidBytes[i];

                    Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

                    if (j < 4)
                    {
                        Assert.Equal(0, consumed);
                        Assert.Equal(0, decodedByteCount);
                    }
                    else
                    {
                        Assert.Equal(4, consumed);
                        Assert.Equal(3, decodedByteCount);
                        Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
                    }
                }
            }

            // When isFinalBlock = true input that is not a multiple of 4 is invalid for Base64, but valid for Base64Url
            if (isFinalBlock)
            {
                Span<byte> source = "2222PPP"u8.ToArray(); // incomplete input
                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                Assert.Equal(5, Base64Url.DecodeFromUtf8(source, decodedBytes));
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(7, 5, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodingInvalidBytesPadding(bool isFinalBlock)
        {
            // Only last 2 bytes can be padding, all other occurrence of padding is invalid
            for (int j = 0; j < 7; j++)
            {
                Span<byte> source = "2222PPPP"u8.ToArray(); // valid input
                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                source[j] = Base64TestHelper.EncodingPad;
                Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

                if (j < 4)
                {
                    Assert.Equal(0, consumed);
                    Assert.Equal(0, decodedByteCount);
                }
                else
                {
                    Assert.Equal(4, consumed);
                    Assert.Equal(3, decodedByteCount);
                    Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
                }
            }

            // Invalid input with valid padding
            {
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                source[6] = Base64TestHelper.EncodingPad;
                source[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P*=="
                Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                source[7] = Base64TestHelper.EncodingPad; // invalid input - "2222PP**="
                Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
            }

            // The last byte or the last 2 bytes being the padding character is valid, if isFinalBlock = true
            {
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                source[6] = Base64TestHelper.EncodingPad;
                source[7] = Base64TestHelper.EncodingPad; // valid input - "2222PP=="

                OperationStatus expectedStatus = isFinalBlock ? OperationStatus.Done : OperationStatus.InvalidData;
                int expectedConsumed = isFinalBlock ? source.Length : 4;
                int expectedWritten = isFinalBlock ? 4 : 3;

                Assert.Equal(expectedStatus, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                source[7] = Base64TestHelper.UrlEncodingPad; // valid input - "2222PPP="

                expectedConsumed = isFinalBlock ? source.Length : 4;
                expectedWritten = isFinalBlock ? 5 : 3;
                Assert.Equal(expectedStatus, Base64Url.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyUrlDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
            }
        }

        [Fact]
        public void GetMaxDecodedLength()
        {
            Span<byte> sourceEmpty = Span<byte>.Empty;
            Assert.Equal(0, Base64Url.GetMaxDecodedLength(0));

            // int.MaxValue - (int.MaxValue % 4) => 2147483644, largest multiple of 4 less than int.MaxValue
            int[] input = { 0, 4, 8, 12, 16, 20, 2000000000, 2147483640, 2147483644 };
            int[] expected = { 0, 3, 6, 9, 12, 15, 1500000000, 1610612730, 1610612733 };

            for (int i = 0; i < input.Length; i++)
            {
                Assert.Equal(expected[i], Base64Url.GetMaxDecodedLength(input[i]));
            }

            // Lengths that are not a multiple of 4.
            int[] lengthsNotMultipleOfFour = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 1001, 1002, 1003, 2147483645, 2147483646, 2147483647 };
            int[] expectedOutput =           { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 750, 751, 752, 1610612733, 1610612734, 1610612735 };
            for (int i = 0; i < lengthsNotMultipleOfFour.Length; i++)
            {
                Assert.Equal(expectedOutput[i], Base64Url.GetMaxDecodedLength(lengthsNotMultipleOfFour[i]));
            }

            // negative input
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetMaxDecodedLength(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64Url.GetMaxDecodedLength(int.MinValue));
        }

        private static bool VerifyUrlDecodingCorrectness(string sourceString, Span<byte> decodedBytes)
        {
            string padded = sourceString.Length % 4 == 0 ? sourceString :
                sourceString.PadRight(sourceString.Length + (4 - sourceString.Length % 4), '=');
            byte[] expectedBytes = Convert.FromBase64String(padded.Replace('_', '/').Replace('-', '+').Replace('%', '='));
            return expectedBytes.AsSpan().SequenceEqual(decodedBytes);
        }

        [Fact]
        public void DecodeInPlace()
        {
            const int numberOfBytes = 15;

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest += 4)
            {
                Span<byte> testBytes = new byte[numberOfBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(testBytes);
                string sourceString = Encoding.ASCII.GetString(testBytes.Slice(0, numberOfBytesToTest).ToArray());
                int bytesWritten = Base64Url.DecodeFromUtf8InPlace(testBytes.Slice(0, numberOfBytesToTest));

                Assert.Equal(Base64Url.GetMaxDecodedLength(numberOfBytesToTest), bytesWritten);
                Assert.True(VerifyUrlDecodingCorrectness(sourceString, testBytes.Slice(0, bytesWritten)));
            }
        }

        [Fact]
        public void EncodeAndDecodeInPlace()
        {
            byte[] testBytes = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                testBytes[i] = (byte)i;
            }

            for (int value = 0; value < 256; value++)
            {
                Span<byte> sourceBytes = testBytes.AsSpan(0, value + 1);
                Span<byte> buffer = new byte[Base64Url.GetEncodedLength(sourceBytes.Length)];

                Assert.Equal(OperationStatus.Done, Base64Url.EncodeToUtf8(sourceBytes, buffer, out int consumed, out int written));
                Assert.True(Base64TestHelper.VerifyUrlEncodingCorrectness(consumed, written, sourceBytes, buffer));

                int bytesWritten = Base64Url.DecodeFromUtf8InPlace(buffer);

                Assert.Equal(sourceBytes.Length, bytesWritten);
                Assert.True(sourceBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }
        }

        [Fact]
        public void DecodeInPlaceInvalidBytesThrowsFormatException()
        {
            byte[] invalidBytes = Base64TestHelper.UrlInvalidBytes;

            for (int j = 0; j < 8; j++)
            {
                for (int i = 0; i < invalidBytes.Length; i++)
                {
                    byte[] buffer = "2222PPPP"u8.ToArray(); // valid input

                    // Don't test padding (byte 61 i.e. '='), which is tested in DecodeInPlaceInvalidBytesPadding
                    // Don't test chars to be ignored (spaces: 9, 10, 13, 32 i.e. '\n', '\t', '\r', ' ')
                    if (invalidBytes[i] == Base64TestHelper.EncodingPad ||
                        invalidBytes[i] == Base64TestHelper.UrlEncodingPad ||
                        Base64TestHelper.IsByteToBeIgnored(invalidBytes[i]))
                    {
                        continue;
                    }

                    // replace one byte with an invalid input
                    buffer[j] = invalidBytes[i];

                    Assert.Throws<FormatException>(() => Base64Url.DecodeFromUtf8InPlace(buffer));
                }
            }

            // Input that is not a multiple of 4 is valid for remainder 2-3, but invalid for 1
            {
                byte[] buffer = "2222P"u8.ToArray(); // incomplete input
                Assert.Throws<FormatException>(() => Base64Url.DecodeFromUtf8InPlace(buffer));
            }
        }

        [Fact]
        public void DecodeInPlaceInvalidBytesPaddingThrowsFormatException()
        {
            // Only last 2 bytes can be padding, all other occurrence of padding is invalid
            for (int j = 0; j < 7; j++)
            {
                byte[] buffer = "2222PPPP"u8.ToArray(); // valid input
                buffer[j] = Base64TestHelper.EncodingPad;

                Assert.Throws<FormatException>(() => Base64Url.DecodeFromUtf8InPlace(buffer));
            }

            // Invalid input with valid padding
            {
                byte[] buffer = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                buffer[6] = Base64TestHelper.EncodingPad;
                buffer[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P*=="

                Assert.Throws<FormatException>(() => Base64Url.DecodeFromUtf8InPlace(buffer));
            }

            {
                byte[] buffer = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                buffer[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P**="

                Assert.Throws<FormatException>(() => Base64Url.DecodeFromUtf8InPlace(buffer));
            }

            // The last byte or the last 2 bytes being the padding character is valid
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[6] = Base64TestHelper.UrlEncodingPad;
                buffer[7] = Base64TestHelper.EncodingPad; // valid input - "2222PP=="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                int bytesWritten = Base64Url.DecodeFromUtf8InPlace(buffer);

                Assert.Equal(4, bytesWritten);
                Assert.True(VerifyUrlDecodingCorrectness(sourceString, buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[7] = Base64TestHelper.EncodingPad; // valid input - "2222PPP="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                int bytesWritten = Base64Url.DecodeFromUtf8InPlace(buffer);

                Assert.Equal(5, bytesWritten);
                Assert.True(VerifyUrlDecodingCorrectness(sourceString, buffer.Slice(0, bytesWritten)));
            }

            // The last byte or the last 2 bytes being the padding character is valid
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80 }; // valid input without padding "2222PP"

                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                int bytesWritten = Base64Url.DecodeFromUtf8InPlace(buffer);

                Assert.Equal(4, bytesWritten);
                Assert.True(VerifyUrlDecodingCorrectness(sourceString, buffer.Slice(0, bytesWritten)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidBase64Strings_WithCharsThatMustBeIgnored))]
        public void BasicDecodingIgnoresCharsToBeIgnoredAsConvertToBase64Does(string utf8WithCharsToBeIgnored, byte[] expectedBytes)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            byte[] resultBytes = new byte[5];
            OperationStatus result = Base64Url.DecodeFromUtf8(utf8BytesWithByteToBeIgnored, resultBytes, out int bytesConsumed, out int bytesWritten);

            // Control value from Convert.FromBase64String
            byte[] stringBytes = Convert.FromBase64String(utf8WithCharsToBeIgnored);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(utf8WithCharsToBeIgnored.Length, bytesConsumed);
            Assert.Equal(expectedBytes.Length, bytesWritten);
            Assert.True(expectedBytes.SequenceEqual(resultBytes));
            Assert.True(stringBytes.SequenceEqual(resultBytes));
        }

        [Theory]
        [MemberData(nameof(ValidBase64Strings_WithCharsThatMustBeIgnored))]
        public void DecodeInPlaceIgnoresCharsToBeIgnoredAsConvertToBase64Does(string utf8WithCharsToBeIgnored, byte[] expectedBytes)
        {
            Span<byte> utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            int bytesWritten = Base64Url.DecodeFromUtf8InPlace(utf8BytesWithByteToBeIgnored);
            Span<byte> bytesOverwritten = utf8BytesWithByteToBeIgnored.Slice(0, bytesWritten);
            byte[] resultBytesArray = bytesOverwritten.ToArray();

            // Control value from Convert.FromBase64String
            byte[] stringBytes = Convert.FromBase64String(utf8WithCharsToBeIgnored);

            Assert.Equal(expectedBytes.Length, bytesWritten);
            Assert.True(expectedBytes.SequenceEqual(resultBytesArray));
            Assert.True(stringBytes.SequenceEqual(resultBytesArray));
        }

        [Theory]
        [MemberData(nameof(StringsOnlyWithCharsToBeIgnored))]
        public void BasicDecodingWithOnlyCharsToBeIgnored(string utf8WithCharsToBeIgnored)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            byte[] resultBytes = new byte[5];
            OperationStatus result = Base64Url.DecodeFromUtf8(utf8BytesWithByteToBeIgnored, resultBytes, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(StringsOnlyWithCharsToBeIgnored))]
        public void DecodingInPlaceWithOnlyCharsToBeIgnored(string utf8WithCharsToBeIgnored)
        {
            Span<byte> utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            int bytesWritten = Base64Url.DecodeFromUtf8InPlace(utf8BytesWithByteToBeIgnored);

            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(new byte[] { 0xa, 0xa, 0x2d, 0x2d }, 251)]
        [InlineData(new byte[] { 0xa, 0x5f, 0xa, 0x2d }, 255)]
        [InlineData(new byte[] { 0x5f, 0x5f, 0xa, 0xa }, 255)]
        [InlineData(new byte[] { 0x70, 0xa, 0x61, 0xa }, 165)]
        [InlineData(new byte[] { 0xa, 0x70, 0xa, 0x61, 0xa }, 165)]
        [InlineData(new byte[] { 0x70, 0xa, 0x61, 0xa, 0x3d, 0x3d }, 165)]
        public void DecodingLessThan4BytesWithWhiteSpaces(byte[] utf8Bytes, byte decoded)
        {
            Assert.True(Base64Url.IsValid(utf8Bytes, out int decodedLength));
            Assert.Equal(1, decodedLength);
            Span<byte> decodedSpan = new byte[decodedLength];
            OperationStatus status = Base64Url.DecodeFromUtf8(utf8Bytes, decodedSpan, out int bytesRead, out int bytesDecoded);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(utf8Bytes.Length, bytesRead);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.Equal(decoded, decodedSpan[0]);
            decodedSpan.Clear();
            Assert.True(Base64Url.TryDecodeFromUtf8(utf8Bytes, decodedSpan, out bytesDecoded));
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.Equal(decoded, decodedSpan[0]);

            bytesDecoded = Base64Url.DecodeFromUtf8InPlace(utf8Bytes);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.Equal(decoded, utf8Bytes[0]);
        }

        [Theory]
        [InlineData(new char[] { '\r', '\r', '-', '-' }, 251)]
        [InlineData(new char[] { '\r', '_', '\r', '-' }, 255)]
        [InlineData(new char[] { '_', '_', '\r', '\r' }, 255)]
        [InlineData(new char[] { 'p', '\r', 'a', '\r' }, 165)]
        [InlineData(new char[] { '\r', 'p', '\r', 'a', '\r' }, 165)]
        [InlineData(new char[] { 'p', '\r', 'a', '\r', '=', '=' }, 165)]
        public void DecodingLessThan4CharsWithWhiteSpaces(char[] utf8Bytes, byte decoded)
        {
            Assert.True(Base64Url.IsValid(utf8Bytes, out int decodedLength));
            Assert.Equal(1, decodedLength);
            Span<byte> decodedSpan = new byte[decodedLength];
            OperationStatus status = Base64Url.DecodeFromChars(utf8Bytes, decodedSpan, out int bytesRead, out int bytesDecoded);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(utf8Bytes.Length, bytesRead);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.Equal(decoded, decodedSpan[0]);
            decodedSpan.Clear();
            Assert.True(Base64Url.TryDecodeFromChars(utf8Bytes, decodedSpan, out bytesDecoded));
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.Equal(decoded, decodedSpan[0]);
        }

        [Theory]
        [InlineData(new byte[] { 0x4a, 0x74, 0xa, 0x4a, 0x4a, 0x74, 0xa, 0x4a }, new byte[] { 38, 210, 73, 180 })]
        [InlineData(new byte[] { 0xa, 0x2d, 0x56, 0xa, 0xa, 0xa, 0x2d, 0x4a, 0x4a, 0x4a, }, new byte[] { 249, 95, 137, 36 })]
        public void DecodingNotMultipleOf4WithWhiteSpace(byte[] utf8Bytes, byte[] decoded)
        {
            Assert.True(Base64Url.IsValid(utf8Bytes, out int decodedLength));
            Assert.Equal(4, decodedLength);
            Span<byte> decodedSpan = new byte[decodedLength];
            OperationStatus status = Base64Url.DecodeFromUtf8(utf8Bytes, decodedSpan, out int bytesRead, out int bytesDecoded);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(utf8Bytes.Length, bytesRead);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.True(decodedSpan.SequenceEqual(decoded));
            decodedSpan.Clear();
            Assert.True(Base64Url.TryDecodeFromUtf8(utf8Bytes, decodedSpan, out bytesDecoded));
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.True(decodedSpan.SequenceEqual(decoded));
            bytesDecoded = Base64Url.DecodeFromUtf8InPlace(utf8Bytes);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.True(utf8Bytes.AsSpan().Slice(0, bytesDecoded).SequenceEqual(decoded));
        }

        [Theory]
        [InlineData(new char[] { 'J', 't', '\r', 'J', 'J', 't', '\r', 'J' }, new byte[] { 38, 210, 73, 180 })]
        [InlineData(new char[] { '\r', '-', 'V', '\r', '\r', '\r', '-', 'J', 'J', 'J', }, new byte[] { 249, 95, 137, 36 })]
        public void DecodingNotMultipleOf4CharsWithWhiteSpace(char[] utf8Bytes, byte[] decoded)
        {
            Assert.True(Base64Url.IsValid(utf8Bytes, out int decodedLength));
            Assert.Equal(4, decodedLength);
            Span<byte> decodedSpan = new byte[decodedLength];
            OperationStatus status = Base64Url.DecodeFromChars(utf8Bytes, decodedSpan, out int bytesRead, out int bytesDecoded);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(utf8Bytes.Length, bytesRead);
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.True(decodedSpan.SequenceEqual(decoded));
            decodedSpan.Clear();
            Assert.True(Base64Url.TryDecodeFromChars(utf8Bytes, decodedSpan, out bytesDecoded));
            Assert.Equal(decodedLength, bytesDecoded);
            Assert.True(decodedSpan.SequenceEqual(decoded));
        }

        [Theory]
        [MemberData(nameof(BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes_MemberData))]
        public void BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }
    }
}
