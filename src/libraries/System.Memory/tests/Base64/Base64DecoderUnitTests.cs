// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64DecoderUnitTests : Base64TestBase
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
                } while (numBytes % 4 != 0);    // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(source.Length, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
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
                } while (numBytes % 4 == 0);    // ensure we have a invalid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                int expectedConsumed = numBytes / 4 * 4;    // decode input up to the closest multiple of 4
                int expectedDecoded = expectedConsumed / 4 * 3;

                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedDecoded, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedDecoded, source, decodedBytes));
            }
        }

        [Fact]
        public void BasicDecodingInvalidInputWithSlicedSource()
        {
            ReadOnlySpan<byte> source = stackalloc byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };
            Span<byte> decodedBytes = stackalloc byte[128];

            source = source[..3];   // now it's invalid as only 3 bytes are present

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
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
                } while (numBytes % 4 != 0);    // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                int expectedConsumed = source.Length / 4 * 4; // only consume closest multiple of four since isFinalBlock is false

                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
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
                } while (numBytes % 4 == 0);    // ensure we have a invalid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                int expectedConsumed = source.Length / 4 * 4; // only consume closest multiple of four since isFinalBlock is false
                int expectedDecoded = expectedConsumed / 4 * 3;

                Assert.Equal(OperationStatus.NeedMoreData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedDecoded, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodeEmptySpan(bool isFinalBlock)
        {
            Span<byte> source = Span<byte>.Empty;
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
            Assert.Equal(0, consumed);
            Assert.Equal(0, decodedByteCount);
        }

        [Fact]
        public void DecodeGuid()
        {
            Span<byte> source = new byte[24];
            Span<byte> decodedBytes = Guid.NewGuid().ToByteArray();
            Base64.EncodeToUtf8(decodedBytes, source, out int _, out int _);

            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(24, consumed);
            Assert.Equal(16, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
        }

        [Fact]
        public void DecodingOutputTooSmall()
        {
            for (int numBytes = 5; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[3];
                int consumed, written;
                if (numBytes >= 8)
                {
                    Assert.True(OperationStatus.DestinationTooSmall ==
                        Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out written), "Number of Input Bytes: " + numBytes);
                }
                else
                {
                    Assert.True(OperationStatus.InvalidData ==
                        Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out written), "Number of Input Bytes: " + numBytes);
                }
                int expectedConsumed = 4;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }

            // Output too small even with padding characters in the input
            {
                Span<byte> source = new byte[12];
                Base64TestHelper.InitializeDecodableBytes(source);
                source[9] = 65; // make sure unused bits set to 0
                source[10] = Base64TestHelper.EncodingPad;
                source[11] = Base64TestHelper.EncodingPad;

                Span<byte> decodedBytes = new byte[6];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }

            {
                Span<byte> source = new byte[12];
                Base64TestHelper.InitializeDecodableBytes(source);
                source[10] = 77; // make sure unused bits set to 0
                source[11] = Base64TestHelper.EncodingPad;

                Span<byte> decodedBytes = new byte[7];
                Assert.Equal(OperationStatus.DestinationTooSmall, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(6, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, 6, source, decodedBytes));
            }
        }

        [Fact]
        public void DecodingOutputTooSmallWithFinalBlockFalse()
        {
            for (int numBytes = 8; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[4];
                int consumed, written;
                Assert.True(OperationStatus.DestinationTooSmall ==
                    Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out written, isFinalBlock: false), "Number of Input Bytes: " + numBytes);
                int expectedConsumed = 4;
                int expectedWritten = 3;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DecodingOutputTooSmallRetry(bool isFinalBlock)
        {
            Span<byte> source = new byte[1000];
            Base64TestHelper.InitializeDecodableBytes(source);

            int outputSize = 240;
            int requiredSize = Base64.GetMaxDecodedFromUtf8Length(source.Length);

            Span<byte> decodedBytes = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
            int expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));

            decodedBytes = new byte[requiredSize - outputSize];
            source = source.Slice(consumed);
            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));
            expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("AQ==", 1)]
        [InlineData("AQI=", 2)]
        [InlineData("AQID", 3)]
        [InlineData("AQIDBA==", 4)]
        [InlineData("AQIDBAU=", 5)]
        [InlineData("AQIDBAUG", 6)]
        public void BasicDecodingWithFinalBlockTrueKnownInputDone(string inputString, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            int expectedConsumed = inputString.Length;
            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Theory]
        [InlineData("AR==")]
        [InlineData("AQJ=")]
        [InlineData("AQIDBB==")]
        [InlineData("AQIDBAV=")]
        [InlineData("AQIDBAUHCAkKCwwNDz==")]
        [InlineData("AQIDBAUHCAkKCwwNDxD=")]
        public void BasicDecodingWithNonZeroUnusedBits(string inputString)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.False(Base64.IsValid(inputString));
            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int _, out int _));
            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(source, out int _));
        }

        [Theory]
        [InlineData("A", 0, 0)]
        [InlineData("A===", 0, 0)]
        [InlineData("A==", 0, 0)]
        [InlineData("A=", 0, 0)]
        [InlineData("AQ", 0, 0)]
        [InlineData("AQI", 0, 0)]
        [InlineData("AQIDBA", 4, 3)]
        [InlineData("AQIDBAU", 4, 3)]
        public void BasicDecodingWithFinalBlockTrueKnownInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Theory]
        [InlineData("\u00ecz/T", 0, 0)]                                              // scalar code-path
        [InlineData("z/Ta123\u00ec", 4, 3)]
        [InlineData("\u00ecz/TpH7sqEkerqMweH1uSw==", 0, 0)]                          // Vector128 code-path
        [InlineData("z/TpH7sqEkerqMweH1uSw\u5948==", 20, 15)]
        [InlineData("\u5948/TpH7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo==", 0, 0)]  // Vector256 / AVX code-path
        [InlineData("z/TpH7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo\u00ec==", 44, 33)]
        [InlineData("\u5948z+T/H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789==", 0, 0)]  // Vector512 / Avx512Vbmi code-path
        [InlineData("z/T+H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789\u5948==", 92, 69)]
        public void BasicDecodingNonAsciiInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.UTF8.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
        }

        [Theory]
        [InlineData("AQID", 3)]
        [InlineData("AQIDBAUG", 6)]
        public void BasicDecodingWithFinalBlockFalseKnownInputDone(string inputString, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            int expectedConsumed = inputString.Length;
            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("A", 0, 0)]
        [InlineData("AQ", 0, 0)]
        [InlineData("AQI", 0, 0)]
        [InlineData("AQIDB", 4, 3)]
        [InlineData("AQIDBA", 4, 3)]
        [InlineData("AQIDBAU", 4, 3)]
        public void BasicDecodingWithFinalBlockFalseKnownInputNeedMoreData(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.NeedMoreData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount); // expectedWritten == decodedBytes.Length
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("AQ==", 0, 0)]
        [InlineData("AQI=", 0, 0)]
        [InlineData("AQIDBA==", 4, 3)]
        [InlineData("AQIDBAU=", 4, 3)]
        public void BasicDecodingWithFinalBlockFalseKnownInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
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
            // 0-42
            // 44-46
            // 58-64
            // 91-96
            // 123-255
            byte[] invalidBytes = Base64TestHelper.InvalidBytes;
            Assert.Equal(byte.MaxValue + 1 - 64, invalidBytes.Length); // 192

            for (int j = 0; j < 8; j++)
            {
                Span<byte> source = "2222PPPP"u8.ToArray(); // valid input
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

                for (int i = 0; i < invalidBytes.Length; i++)
                {
                    // Don't test padding (byte 61 i.e. '='), which is tested in DecodingInvalidBytesPadding
                    // Don't test chars to be ignored (spaces: 9, 10, 13, 32 i.e. '\n', '\t', '\r', ' ')
                    if (invalidBytes[i] == Base64TestHelper.EncodingPad ||
                        Base64TestHelper.IsByteToBeIgnored(invalidBytes[i]))
                    {
                        continue;
                    }

                    // replace one byte with an invalid input
                    source[j] = invalidBytes[i];

                    Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

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

            // Input that is not a multiple of 4 is considered invalid, if isFinalBlock = true
            if (isFinalBlock)
            {
                Span<byte> source = "2222PPP"u8.ToArray(); // incomplete input
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
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
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[j] = Base64TestHelper.EncodingPad;
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

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
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[6] = Base64TestHelper.EncodingPad;
                source[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P*=="
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[7] = Base64TestHelper.EncodingPad; // invalid input - "2222PP**="
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
            }

            // The last byte or the last 2 bytes being the padding character is valid, if isFinalBlock = true
            {
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 65,
                    Base64TestHelper.EncodingPad, Base64TestHelper.EncodingPad }; // valid input - "2222PA=="
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

                OperationStatus expectedStatus = isFinalBlock ? OperationStatus.Done : OperationStatus.InvalidData;
                int expectedConsumed = isFinalBlock ? source.Length : 4;
                int expectedWritten = isFinalBlock ? 4 : 3;

                Assert.Equal(expectedStatus, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 80, 77, 80 };
                decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[7] = Base64TestHelper.EncodingPad; // valid input - "2222PPM="

                expectedConsumed = isFinalBlock ? source.Length : 4;
                expectedWritten = isFinalBlock ? 5 : 3;
                Assert.Equal(expectedStatus, Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount, isFinalBlock));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
            }
        }

        [Fact]
        public void GetMaxDecodedLength()
        {
            Span<byte> sourceEmpty = Span<byte>.Empty;
            Assert.Equal(0, Base64.GetMaxDecodedFromUtf8Length(0));

            // int.MaxValue - (int.MaxValue % 4) => 2147483644, largest multiple of 4 less than int.MaxValue
            int[] input = { 0, 4, 8, 12, 16, 20, 2000000000, 2147483640, 2147483644 };
            int[] expected = { 0, 3, 6, 9, 12, 15, 1500000000, 1610612730, 1610612733 };

            for (int i = 0; i < input.Length; i++)
            {
                Assert.Equal(expected[i], Base64.GetMaxDecodedFromUtf8Length(input[i]));
            }

            // Lengths that are not a multiple of 4.
            int[] lengthsNotMultipleOfFour = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 1001, 1002, 1003, 2147483645, 2147483646, 2147483647 };
            int[] expectedOutput = { 0, 0, 0, 3, 3, 3, 6, 6, 6, 9, 9, 9, 750, 750, 750, 1610612733, 1610612733, 1610612733 };
            for (int i = 0; i < lengthsNotMultipleOfFour.Length; i++)
            {
                Assert.Equal(expectedOutput[i], Base64.GetMaxDecodedFromUtf8Length(lengthsNotMultipleOfFour[i]));
            }

            // negative input
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxDecodedFromUtf8Length(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Base64.GetMaxDecodedFromUtf8Length(int.MinValue));
        }

        [Fact]
        public void DecodeInPlace()
        {
            const int numberOfBytes = 15;

            for (int numberOfBytesToTest = 0; numberOfBytesToTest <= numberOfBytes; numberOfBytesToTest += 4)
            {
                Span<byte> testBytes = new byte[numberOfBytes];
                Base64TestHelper.InitializeDecodableBytes(testBytes);
                string sourceString = Encoding.ASCII.GetString(testBytes.Slice(0, numberOfBytesToTest).ToArray());
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);

                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(testBytes.Slice(0, numberOfBytesToTest), out int bytesWritten));
                Assert.Equal(Base64.GetMaxDecodedFromUtf8Length(numberOfBytesToTest), bytesWritten);
                Assert.True(expectedBytes.SequenceEqual(testBytes.Slice(0, bytesWritten)));
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
                Span<byte> buffer = new byte[Base64.GetMaxEncodedToUtf8Length(sourceBytes.Length)];

                Assert.Equal(OperationStatus.Done, Base64.EncodeToUtf8(sourceBytes, buffer, out int consumed, out int written));

                var encodedText = Encoding.ASCII.GetString(buffer.ToArray());
                var expectedText = Convert.ToBase64String(testBytes, 0, value + 1);
                Assert.Equal(expectedText, encodedText);

                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(sourceBytes.Length, bytesWritten);
                Assert.True(sourceBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }
        }

        [Fact]
        public void DecodeInPlaceInvalidBytes()
        {
            byte[] invalidBytes = Base64TestHelper.InvalidBytes;

            for (int j = 0; j < 8; j++)
            {
                for (int i = 0; i < invalidBytes.Length; i++)
                {
                    Span<byte> buffer = "2222PPPP"u8.ToArray(); // valid input

                    // Don't test padding (byte 61 i.e. '='), which is tested in DecodeInPlaceInvalidBytesPadding
                    // Don't test chars to be ignored (spaces: 9, 10, 13, 32 i.e. '\n', '\t', '\r', ' ')
                    if (invalidBytes[i] == Base64TestHelper.EncodingPad ||
                        Base64TestHelper.IsByteToBeIgnored(invalidBytes[i]))
                    {
                        continue;
                    }

                    // replace one byte with an invalid input
                    buffer[j] = invalidBytes[i];
                    string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());

                    Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));

                    if (j < 4)
                    {
                        Assert.Equal(0, bytesWritten);
                    }
                    else
                    {
                        Assert.Equal(3, bytesWritten);
                        Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                        Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
                    }
                }
            }

            // Input that is not a multiple of 4 is considered invalid
            {
                Span<byte> buffer = "2222PPP"u8.ToArray(); // incomplete input
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(3, bytesWritten);
            }
        }

        [Fact]
        public void DecodeInPlaceInvalidBytesPadding()
        {
            // Only last 2 bytes can be padding, all other occurrence of padding is invalid
            for (int j = 0; j < 7; j++)
            {
                Span<byte> buffer = "2222PPPP"u8.ToArray(); // valid input
                buffer[j] = Base64TestHelper.EncodingPad;
                string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());

                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));

                if (j < 4)
                {
                    Assert.Equal(0, bytesWritten);
                }
                else
                {
                    Assert.Equal(3, bytesWritten);
                    Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                    Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
                }
            }

            // Invalid input with valid padding
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                buffer[6] = Base64TestHelper.EncodingPad;
                buffer[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P*=="
                string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(3, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                buffer[7] = Base64TestHelper.EncodingPad; // invalid input - "2222P**="
                string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(3, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            // The last byte or the last 2 bytes being the padding character is valid
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 65,
                    Base64TestHelper.EncodingPad, Base64TestHelper.EncodingPad }; // valid input - "2222PA=="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(4, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 77, 80 };
                buffer[7] = Base64TestHelper.EncodingPad; // valid input - "2222PPM="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(5, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidBase64Strings_WithCharsThatMustBeIgnored))]
        public void BasicDecodingIgnoresCharsToBeIgnoredAsConvertToBase64Does(string utf8WithCharsToBeIgnored, byte[] expectedBytes)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            byte[] resultBytes = new byte[5];
            OperationStatus result = Base64.DecodeFromUtf8(utf8BytesWithByteToBeIgnored, resultBytes, out int bytesConsumed, out int bytesWritten);

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
            OperationStatus result = Base64.DecodeFromUtf8InPlace(utf8BytesWithByteToBeIgnored, out int bytesWritten);
            Span<byte> bytesOverwritten = utf8BytesWithByteToBeIgnored.Slice(0, bytesWritten);
            byte[] resultBytesArray = bytesOverwritten.ToArray();

            // Control value from Convert.FromBase64String
            byte[] stringBytes = Convert.FromBase64String(utf8WithCharsToBeIgnored);

            Assert.Equal(OperationStatus.Done, result);
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
            OperationStatus result = Base64.DecodeFromUtf8(utf8BytesWithByteToBeIgnored, resultBytes, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(StringsOnlyWithCharsToBeIgnored))]
        public void DecodingInPlaceWithOnlyCharsToBeIgnored(string utf8WithCharsToBeIgnored)
        {
            Span<byte> utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithCharsToBeIgnored);
            OperationStatus result = Base64.DecodeFromUtf8InPlace(utf8BytesWithByteToBeIgnored, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes_MemberData))]
        public void BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Fact]
        public void DecodeFromCharsWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                // Ensure we have a valid length (multiple of 4 for standard Base64)
                numBytes = (numBytes / 4) * 4;

                Span<char> source = new char[numBytes];
                Base64TestHelper.InitializeDecodableChars(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedLength(source.Length)];
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromChars(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(source.Length, consumed);

                string sourceString = source.ToString();
                byte[] expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.AsSpan().SequenceEqual(decodedBytes.Slice(0, decodedByteCount)));
            }
        }

        [Theory]
        [InlineData("\u5948cz/T", 0, 0)] // tests the scalar code-path with non-ASCII
        [InlineData("z/Ta123\u5948", 4, 3)]
        public void DecodeFromCharsNonAsciiInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<char> source = inputString.ToArray();
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromChars(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
        }

        [Fact]
        public void DecodeFromUtf8_ArrayOverload()
        {
            byte[] utf8Input = Encoding.UTF8.GetBytes("dGVzdA=="); // "test" encoded
            byte[] result = Base64.DecodeFromUtf8(utf8Input);
            Assert.Equal(4, result.Length);
            Assert.Equal("test", Encoding.UTF8.GetString(result));
        }

        [Fact]
        public void DecodeFromUtf8_SpanOverload()
        {
            byte[] utf8Input = Encoding.UTF8.GetBytes("dGVzdA=="); // "test" encoded
            Span<byte> destination = new byte[10];
            int bytesWritten = Base64.DecodeFromUtf8(utf8Input, destination);
            Assert.Equal(4, bytesWritten);
            Assert.Equal("test", Encoding.UTF8.GetString(destination.Slice(0, bytesWritten)));
        }

        [Fact]
        public void TryDecodeFromUtf8_Success()
        {
            byte[] utf8Input = Encoding.UTF8.GetBytes("dGVzdA==");
            Span<byte> destination = new byte[10];
            Assert.True(Base64.TryDecodeFromUtf8(utf8Input, destination, out int bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal("test", Encoding.UTF8.GetString(destination.Slice(0, bytesWritten)));
        }

        [Fact]
        public void TryDecodeFromUtf8_DestinationTooSmall()
        {
            byte[] utf8Input = Encoding.UTF8.GetBytes("dGVzdA==");
            Span<byte> destination = new byte[2]; // Too small
            Assert.False(Base64.TryDecodeFromUtf8(utf8Input, destination, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void DecodeFromChars_InvalidData()
        {
            string invalidInput = "@#$%";
            byte[] destination = new byte[10];
            Assert.Throws<FormatException>(() => Base64.DecodeFromChars(invalidInput, destination));
            Assert.Throws<FormatException>(() => Base64.DecodeFromChars(invalidInput.AsSpan()));
        }

        [Fact]
        public void DecodeFromChars_DestinationTooSmall()
        {
            string validInput = "dGVzdA=="; // "test" encoded
            byte[] destination = new byte[2]; // Too small
            Assert.Throws<ArgumentException>("destination", () => Base64.DecodeFromChars(validInput, destination));
        }

        [Fact]
        public void TryDecodeFromChars_DestinationTooSmall()
        {
            string validInput = "dGVzdA=="; // "test" encoded
            Span<byte> destination = new byte[2]; // Too small
            Assert.False(Base64.TryDecodeFromChars(validInput, destination, out int bytesWritten));
        }

        [Fact]
        public void DecodeFromChars_OperationStatus_DistinguishesBetweenInvalidAndDestinationTooSmall()
        {
            // This is the key use case from the issue - distinguishing between invalid data and destination too small
            string validInput = "dGVzdA=="; // "test" encoded - produces 4 bytes
            string invalidInput = "@#$%";
            Span<byte> smallDestination = new byte[2];

            // With destination too small, we should get DestinationTooSmall
            OperationStatus status1 = Base64.DecodeFromChars(validInput, smallDestination, out int consumed1, out int written1);
            Assert.Equal(OperationStatus.DestinationTooSmall, status1);
            Assert.True(consumed1 > 0 || written1 >= 0); // Some progress was made or at least we know why it failed

            // With invalid data, we should get InvalidData
            OperationStatus status2 = Base64.DecodeFromChars(invalidInput, smallDestination, out int consumed2, out int written2);
            Assert.Equal(OperationStatus.InvalidData, status2);
            Assert.Equal(0, consumed2);
            Assert.Equal(0, written2);
        }

        [Fact]
        public void GetMaxDecodedLength_Matches_GetMaxDecodedFromUtf8Length()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(Base64.GetMaxDecodedFromUtf8Length(i), Base64.GetMaxDecodedLength(i));
            }
        }

        [Fact]
        public void DecodingWithWhiteSpaceIntoSmallDestination()
        {
            // Input "  zAww  " (8 bytes) contains "zAww" which decodes to 3 bytes.
            // 'z' = 51, 'A' = 0, 'w' = 48, 'w' = 48 -> bits: 110011 000000 110000 110000 -> 0xCC 0x0C 0x30
            // With destination of 3 bytes, this should succeed, not report "Destination too short".
            byte[] input = Encoding.UTF8.GetBytes("  zAww  ");

            byte[] destination5 = new byte[5];
            OperationStatus status5 = Base64.DecodeFromUtf8(input, destination5, out int consumed5, out int written5);
            Assert.Equal(OperationStatus.Done, status5);
            Assert.Equal(input.Length, consumed5);
            Assert.Equal(3, written5);

            byte[] destination3 = new byte[3];
            OperationStatus status3 = Base64.DecodeFromUtf8(input, destination3, out int consumed3, out int written3);
            Assert.Equal(OperationStatus.Done, status3);
            Assert.Equal(input.Length, consumed3);
            Assert.Equal(3, written3);
        }

        [Fact]
        public void DecodingWithOnlyWhiteSpaceIntoSmallDestination()
        {
            // Input "        " (8 spaces) decodes to 0 bytes.
            // With destination of 1 byte, this should succeed, not report "Destination too short".
            byte[] allSpaces = Encoding.UTF8.GetBytes(new string(' ', 8));

            byte[] destination = new byte[1];
            OperationStatus status = Base64.DecodeFromUtf8(allSpaces, destination, out int consumed, out int written);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(allSpaces.Length, consumed);
            Assert.Equal(0, written);

            // Also test with empty destination buffer
            byte[] emptyDestination = Array.Empty<byte>();
            OperationStatus statusEmpty = Base64.DecodeFromUtf8(allSpaces, emptyDestination, out int consumedEmpty, out int writtenEmpty);
            Assert.Equal(OperationStatus.Done, statusEmpty);
            Assert.Equal(allSpaces.Length, consumedEmpty);
            Assert.Equal(0, writtenEmpty);
        }

        [Fact]
        public void DecodingWithWhiteSpaceIntoSmallDestination_ActualDestinationTooSmall()
        {
            // Input "  AQID" (leading whitespace only) decodes to 3 bytes.
            // With destination of 1 byte, this should correctly report "Destination too short".
            // Note: Base64 requires input length to be multiple of 4, so we use "AQIDBA==" which decodes to 4 bytes.
            byte[] input = Encoding.UTF8.GetBytes("  AQIDBA==");

            byte[] destination1 = new byte[1];
            OperationStatus status1 = Base64.DecodeFromUtf8(input, destination1, out _, out _);
            Assert.Equal(OperationStatus.DestinationTooSmall, status1);

            // With destination of 4 bytes, this should succeed.
            byte[] destination4 = new byte[4];
            OperationStatus status4 = Base64.DecodeFromUtf8(input, destination4, out int consumed4, out int written4);
            Assert.Equal(OperationStatus.Done, status4);
            Assert.Equal(input.Length, consumed4);
            Assert.Equal(4, written4);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, destination4);
        }

        [Fact]
        public void DecodingWithEmbeddedWhiteSpaceIntoSmallDestination()
        {
            // Tests DecodeWithWhiteSpaceBlockwiseWrapper path - whitespace embedded in Base64 data.
            // Input "z A w w" has whitespace in the middle. "zAww" decodes to 3 bytes.
            byte[] input = Encoding.UTF8.GetBytes("z A w w");

            byte[] destination3 = new byte[3];
            OperationStatus status3 = Base64.DecodeFromUtf8(input, destination3, out int consumed3, out int written3);
            Assert.Equal(OperationStatus.Done, status3);
            Assert.Equal(input.Length, consumed3);
            Assert.Equal(3, written3);

            // Also test with larger embedded whitespace
            byte[] input2 = Encoding.UTF8.GetBytes("z  A  w  w");
            byte[] destination2 = new byte[3];
            OperationStatus status2 = Base64.DecodeFromUtf8(input2, destination2, out int consumed2, out int written2);
            Assert.Equal(OperationStatus.Done, status2);
            Assert.Equal(input2.Length, consumed2);
            Assert.Equal(3, written2);
        }

        [Fact]
        public void DecodingWithEmbeddedWhiteSpaceIntoSmallDestination_ActualDestinationTooSmall()
        {
            // Tests DecodeWithWhiteSpaceBlockwiseWrapper path with actual destination too small.
            // Input "A Q I D B A = =" (embedded whitespace) decodes to 4 bytes.
            byte[] input = Encoding.UTF8.GetBytes("A Q I D B A = =");

            byte[] destination1 = new byte[1];
            OperationStatus status1 = Base64.DecodeFromUtf8(input, destination1, out _, out _);
            Assert.Equal(OperationStatus.DestinationTooSmall, status1);

            // With destination of 4 bytes, this should succeed.
            byte[] destination4 = new byte[4];
            OperationStatus status4 = Base64.DecodeFromUtf8(input, destination4, out int consumed4, out int written4);
            Assert.Equal(OperationStatus.Done, status4);
            Assert.Equal(input.Length, consumed4);
            Assert.Equal(4, written4);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, destination4);
        }

        [Theory]
        [InlineData("AQ\r\nQ=")]
        [InlineData("AQ\r\nQ=\r\n")]
        [InlineData("AQ Q=")]
        [InlineData("AQ\tQ=")]
        public void DecodingWithWhiteSpaceSplitFinalQuantumAndIsFinalBlockFalse(string base64String)
        {
            // When a final quantum (containing padding) is split by whitespace and isFinalBlock=false,
            // the decoder should not consume any bytes, allowing the caller to retry with isFinalBlock=true
            ReadOnlySpan<byte> base64Data = Encoding.ASCII.GetBytes(base64String);
            var output = new byte[10];

            // First call with isFinalBlock=false should consume 0 bytes
            OperationStatus status = Base64.DecodeFromUtf8(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(OperationStatus.InvalidData, status);

            // Second call with isFinalBlock=true should succeed
            status = Base64.DecodeFromUtf8(base64Data, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(base64Data.Length, bytesConsumed);
            Assert.Equal(2, bytesWritten); // "AQQ=" decodes to 2 bytes: {1, 4}
            Assert.Equal(new byte[] { 1, 4 }, output[..2]);
        }

        [Fact]
        public void DecodingCompleteQuantumWithIsFinalBlockFalse()
        {
            // Complete quantum without padding should be decoded even when isFinalBlock=false
            ReadOnlySpan<byte> base64Data = "AAAA"u8;
            var output = new byte[10];

            OperationStatus status = Base64.DecodeFromUtf8(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(4, bytesConsumed);
            Assert.Equal(3, bytesWritten);
        }

        [Fact]
        public void DecodingPaddedQuantumWithIsFinalBlockFalse()
        {
            // Quantum with padding should not be decoded when isFinalBlock=false
            ReadOnlySpan<byte> base64Data = "AAA="u8;
            var output = new byte[10];

            OperationStatus status = Base64.DecodeFromUtf8(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData("AQIDBAUG AQ\r\nQ=", 9, 6, "AQ\r\nQ=")]          // Two complete blocks, then whitespace-split final quantum
        [InlineData("AQID BAUG AQ\r\nQ=", 10, 6, "AQ\r\nQ=")]        // Two blocks with space, then whitespace-split final quantum
        [InlineData("AQIDBAUG\r\nAQID AQ\r\nQ=", 15, 9, "AQ\r\nQ=")] // Multiple blocks with various whitespace patterns
        public void DecodingWithValidDataBeforeWhiteSpaceSplitFinalQuantum(string base64String, int expectedBytesConsumedFirstCall, int expectedBytesWrittenFirstCall, string expectedRemainingAfterFirstCall)
        {
            // When there's valid data before a whitespace-split final quantum and isFinalBlock=false,
            // verify the streaming scenario works correctly
            ReadOnlySpan<byte> base64Data = Encoding.ASCII.GetBytes(base64String);
            var output = new byte[100];

            // First call with isFinalBlock=false should decode the valid complete blocks and stop before the incomplete final quantum
            OperationStatus status = Base64.DecodeFromUtf8(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(expectedBytesConsumedFirstCall, bytesConsumed);
            Assert.Equal(expectedBytesWrittenFirstCall, bytesWritten);

            // Verify that only the final block remains
            ReadOnlySpan<byte> remaining = base64Data.Slice(bytesConsumed);
            string remainingString = Encoding.ASCII.GetString(remaining);
            Assert.Equal(expectedRemainingAfterFirstCall, remainingString);

            // Verify we can complete decoding by retrying with the FULL input and isFinalBlock=true
            Array.Clear(output, 0, output.Length);
            status = Base64.DecodeFromUtf8(base64Data, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(base64Data.Length, bytesConsumed);
            Assert.True(bytesWritten > 0, "Should have decoded data");
        }

        [Fact]
        public void DecodingWithEmbeddedWhiteSpaceIntoSmallDestination_TrailingWhiteSpacesAreConsumed()
        {
            byte[] input = "        8J+N        i    f    C    f        jYk="u8.ToArray();

            // The actual decoded data is 8 bytes long.
            // If we provide a destination buffer with 6 bytes, we can decode two blocks (6 bytes) and leave 2 bytes undecoded.
            // But even though there are 2 bytes left undecoded, we should still consume as much input as possible,
            // such that all trailing whitespace are also consumed.

            byte[] destination = new byte[6];
            Assert.Equal(OperationStatus.DestinationTooSmall, Base64.DecodeFromUtf8(input, destination, out int consumed, out int written));
            Assert.Equal((byte)'j', input[consumed]); // byte right after the spaces
            Assert.Equal(destination.Length, written);
            Assert.Equal(new byte[] { 240, 159, 141, 137, 240, 159 }, destination);
        }

        [Theory]
        [InlineData("AQ\r\nQ=")]
        [InlineData("AQ\r\nQ=\r\n")]
        [InlineData("AQ Q=")]
        [InlineData("AQ\tQ=")]
        public void DecodingFromCharsWithWhiteSpaceSplitFinalQuantumAndIsFinalBlockFalse(string base64String)
        {
            // When a final quantum (containing padding) is split by whitespace and isFinalBlock=false,
            // the decoder should not consume any bytes, allowing the caller to retry with isFinalBlock=true
            ReadOnlySpan<char> base64Data = base64String.AsSpan();
            var output = new byte[10];

            // First call with isFinalBlock=false should consume 0 bytes
            OperationStatus status = Base64.DecodeFromChars(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(OperationStatus.InvalidData, status);

            // Second call with isFinalBlock=true should succeed
            status = Base64.DecodeFromChars(base64Data, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(base64Data.Length, bytesConsumed);
            Assert.Equal(2, bytesWritten); // "AQQ=" decodes to 2 bytes: {1, 4}
            Assert.Equal(new byte[] { 1, 4 }, output[..2]);
        }

        [Fact]
        public void DecodingFromCharsCompleteQuantumWithIsFinalBlockFalse()
        {
            // Complete quantum without padding should be decoded even when isFinalBlock=false
            ReadOnlySpan<char> base64Data = "AAAA".AsSpan();
            var output = new byte[10];

            OperationStatus status = Base64.DecodeFromChars(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(4, bytesConsumed);
            Assert.Equal(3, bytesWritten);
        }

        [Fact]
        public void DecodingFromCharsPaddedQuantumWithIsFinalBlockFalse()
        {
            // Quantum with padding should not be decoded when isFinalBlock=false
            ReadOnlySpan<char> base64Data = "AAA=".AsSpan();
            var output = new byte[10];

            OperationStatus status = Base64.DecodeFromChars(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData("AQIDBAUG AQ\r\nQ=", 9, 6, "AQ\r\nQ=")]          // Two complete blocks, then whitespace-split final quantum
        [InlineData("AQID BAUG AQ\r\nQ=", 10, 6, "AQ\r\nQ=")]        // Two blocks with space, then whitespace-split final quantum
        [InlineData("AQIDBAUG\r\nAQID AQ\r\nQ=", 15, 9, "AQ\r\nQ=")] // Multiple blocks with various whitespace patterns
        public void DecodingFromCharsWithValidDataBeforeWhiteSpaceSplitFinalQuantum(string base64String, int expectedBytesConsumedFirstCall, int expectedBytesWrittenFirstCall, string expectedRemainingAfterFirstCall)
        {
            // When there's valid data before a whitespace-split final quantum and isFinalBlock=false,
            // verify the streaming scenario works correctly
            ReadOnlySpan<char> base64Data = base64String.AsSpan();
            var output = new byte[100];

            // First call with isFinalBlock=false should decode the valid complete blocks and stop before the incomplete final quantum
            OperationStatus status = Base64.DecodeFromChars(base64Data, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(expectedBytesConsumedFirstCall, bytesConsumed);
            Assert.Equal(expectedBytesWrittenFirstCall, bytesWritten);

            // Verify that only the final block remains
            ReadOnlySpan<char> remaining = base64Data.Slice(bytesConsumed);
            string remainingString = new string(remaining);
            Assert.Equal(expectedRemainingAfterFirstCall, remainingString);

            // Verify we can complete decoding by retrying with the FULL input and isFinalBlock=true
            Array.Clear(output, 0, output.Length);
            status = Base64.DecodeFromChars(base64Data, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(base64Data.Length, bytesConsumed);
            Assert.True(bytesWritten > 0, "Should have decoded data");
        }
    }
}
