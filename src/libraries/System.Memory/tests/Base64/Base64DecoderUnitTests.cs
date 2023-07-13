// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
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
        [InlineData("A", 0, 0)]
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
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
        }

        [Theory]
        [InlineData("\u00ecz/T", 0, 0)]                                              // scalar code-path
        [InlineData("z/Ta123\u00ec", 4, 3)]
        [InlineData("\u00ecz/TpH7sqEkerqMweH1uSw==", 0, 0)]                          // Vector128 code-path
        [InlineData("z/TpH7sqEkerqMweH1uSw\u00ec==", 20, 15)]
        [InlineData("\u00ecz/TpH7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo==", 0, 0)]  // Vector256 / AVX code-path
        [InlineData("z/TpH7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo\u00ec==", 44, 33)]
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
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[6] = Base64TestHelper.EncodingPad;
                source[7] = Base64TestHelper.EncodingPad; // valid input - "2222PP=="

                OperationStatus expectedStatus = isFinalBlock ? OperationStatus.Done : OperationStatus.InvalidData;
                int expectedConsumed = isFinalBlock ? source.Length : 4;
                int expectedWritten = isFinalBlock ? 4 : 3;

                Assert.Equal(expectedStatus, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(expectedWritten, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[7] = Base64TestHelper.EncodingPad; // valid input - "2222PPP="

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
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[6] = Base64TestHelper.EncodingPad;
                buffer[7] = Base64TestHelper.EncodingPad; // valid input - "2222PP=="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(4, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[7] = Base64TestHelper.EncodingPad; // valid input - "2222PPP="
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

        public static IEnumerable<object[]> BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes_MemberData()
        {
            var r = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                yield return new object[] { "AQ==" + new string(r.GetItems<char>(" \n\t\r", i)), 4 + i, 1 };
            }

            foreach (string s in new[] { "MTIz", "M TIz", "MT Iz", "MTI z", "MTIz ", "M    TI   z", "M T I Z " })
            {
                yield return new object[] { s + s + s + s, s.Length * 4, 12 };
            }
        }
    }
}
