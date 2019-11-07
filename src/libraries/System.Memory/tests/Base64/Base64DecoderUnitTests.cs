// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64DecoderUnitTests
    {
        [Fact]
        public void BasicDecoding()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                while (numBytes % 4 != 0)
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                }
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitalizeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                Assert.Equal(OperationStatus.Done,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(source.Length, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Fact]
        public void DecodeEmptySpan()
        {
            Span<byte> source = Span<byte>.Empty;
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done,
                Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(source.Length, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
        }

        [Fact]
        public void DecodeGuid()
        {
            Span<byte> source = new byte[24];
            Span<byte> decodedBytes = Guid.NewGuid().ToByteArray();
            Base64.EncodeToUtf8(decodedBytes, source, out int _, out int _);

            Assert.Equal(OperationStatus.Done,
                Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(24, consumed);
            Assert.Equal(16, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, decodedBytes.Length, source, decodedBytes));
        }

        [Fact]
        public void BasicDecodingWithFinalBlockFalse()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                while (numBytes % 4 != 0)
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                }
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitalizeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                int expectedConsumed = source.Length / 4 * 4; // only consume closest multiple of four since isFinalBlock is false

                Assert.Equal(OperationStatus.NeedMoreData,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount, isFinalBlock: false));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }
        }

        [Theory]
        [InlineData("A", 0, 0)]
        [InlineData("AQ", 0, 0)]
        [InlineData("AQI", 0, 0)]
        [InlineData("AQIDBA", 4, 3)]
        [InlineData("AQIDBAU", 4, 3)]
        [InlineData("AQID", 4, 3)]
        [InlineData("AQIDBAUG", 8, 6)]
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
        [InlineData("AQ==", 4, 1)]
        [InlineData("AQI=", 4, 2)]
        [InlineData("AQID", 4, 3)]
        [InlineData("AQIDBA==", 8, 4)]
        [InlineData("AQIDBAU=", 8, 5)]
        [InlineData("AQIDBAUG", 8, 6)]
        public void BasicDecodingWithFinalBlockTrueKnownInputDone(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<byte> source = Encoding.ASCII.GetBytes(inputString);
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, expectedWritten, source, decodedBytes));
        }

        [Fact]
        public void DecodingInvalidBytes()
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
                Span<byte> source = new byte[8] { 50, 50, 50, 50, 80, 80, 80, 80 }; // valid input - "2222PPPP"
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];

                for (int i = 0; i < invalidBytes.Length; i++)
                {
                    // Don't test padding (byte 61 i.e. '='), which is tested in DecodingInvalidBytesPadding
                    if (invalidBytes[i] == Base64TestHelper.s_encodingPad)
                        continue;

                    // replace one byte with an invalid input
                    source[j] = invalidBytes[i];

                    Assert.Equal(OperationStatus.InvalidData,
                        Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));

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

            // Input that is not a multiple of 4 is considered invalid
            {
                Span<byte> source = new byte[7] { 50, 50, 50, 50, 80, 80, 80 }; // incomplete input - "2222PPP"
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                Assert.Equal(OperationStatus.InvalidData,
                        Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
            }
        }

        [Fact]
        public void DecodingInvalidBytesPadding()
        {
            // Only last 2 bytes can be padding, all other occurrence of padding is invalid
            for (int j = 0; j < 7; j++)
            {
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 }; // valid input - "2222PPPP"
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[j] = Base64TestHelper.s_encodingPad;
                Assert.Equal(OperationStatus.InvalidData,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));

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
                source[6] = Base64TestHelper.s_encodingPad;
                source[7] = Base64TestHelper.s_encodingPad; // invalid input - "2222P*=="
                Assert.Equal(OperationStatus.InvalidData,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[7] = Base64TestHelper.s_encodingPad; // invalid input - "2222PP**="
                Assert.Equal(OperationStatus.InvalidData,
                    Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount));

                Assert.Equal(4, consumed);
                Assert.Equal(3, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(4, 3, source, decodedBytes));
            }

            // The last byte or the last 2 bytes being the padding character is valid
            {
                Span<byte> source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[6] = Base64TestHelper.s_encodingPad;
                source[7] = Base64TestHelper.s_encodingPad; // valid input - "2222PP=="
                Assert.Equal(OperationStatus.Done,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));

                Assert.Equal(source.Length, consumed);
                Assert.Equal(4, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, 4, source, decodedBytes));

                source = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                decodedBytes = new byte[Base64.GetMaxDecodedFromUtf8Length(source.Length)];
                source[7] = Base64TestHelper.s_encodingPad; // valid input - "2222PPP="
                Assert.Equal(OperationStatus.Done,
                    Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount));

                Assert.Equal(source.Length, consumed);
                Assert.Equal(5, decodedByteCount);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(source.Length, 5, source, decodedBytes));
            }
        }

        [Fact]
        public void DecodingOutputTooSmall()
        {
            for (int numBytes = 5; numBytes < 20; numBytes++)
            {
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitalizeDecodableBytes(source, numBytes);

                Span<byte> decodedBytes = new byte[3];
                int consumed, written;
                if (numBytes % 4 != 0)
                {
                    Assert.True(OperationStatus.InvalidData ==
                        Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out written), "Number of Input Bytes: " + numBytes);
                }
                else
                {
                    Assert.True(OperationStatus.DestinationTooSmall ==
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
                Base64TestHelper.InitalizeDecodableBytes(source);
                source[10] = Base64TestHelper.s_encodingPad;
                source[11] = Base64TestHelper.s_encodingPad;

                Span<byte> decodedBytes = new byte[6];
                Assert.Equal(OperationStatus.DestinationTooSmall,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(decodedBytes.Length, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
            }

            {
                Span<byte> source = new byte[12];
                Base64TestHelper.InitalizeDecodableBytes(source);
                source[11] = Base64TestHelper.s_encodingPad;

                Span<byte> decodedBytes = new byte[7];
                Assert.Equal(OperationStatus.DestinationTooSmall,
                    Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int written));
                int expectedConsumed = 8;
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(6, written);
                Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, 6, source, decodedBytes));
            }
        }

        [Fact]
        public void DecodingOutputTooSmallRetry()
        {
            Span<byte> source = new byte[1000];
            Base64TestHelper.InitalizeDecodableBytes(source);

            int outputSize = 240;
            int requiredSize = Base64.GetMaxDecodedFromUtf8Length(source.Length);

            Span<byte> decodedBytes = new byte[outputSize];
            Assert.Equal(OperationStatus.DestinationTooSmall,
                Base64.DecodeFromUtf8(source, decodedBytes, out int consumed, out int decodedByteCount));
            int expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));

            decodedBytes = new byte[requiredSize - outputSize];
            source = source.Slice(consumed);
            Assert.Equal(OperationStatus.Done,
                Base64.DecodeFromUtf8(source, decodedBytes, out consumed, out decodedByteCount));
            expectedConsumed = decodedBytes.Length / 3 * 4;
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(decodedBytes.Length, decodedByteCount);
            Assert.True(Base64TestHelper.VerifyDecodingCorrectness(expectedConsumed, decodedBytes.Length, source, decodedBytes));
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
                Base64TestHelper.InitalizeDecodableBytes(testBytes);
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
                    Span<byte> buffer = new byte[8] { 50, 50, 50, 50, 80, 80, 80, 80 }; // valid input - "2222PPPP"

                    // Don't test padding (byte 61 i.e. '='), which is tested in DecodeInPlaceInvalidBytesPadding
                    if (invalidBytes[i] == Base64TestHelper.s_encodingPad)
                        continue;

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
                Span<byte> buffer = new byte[7] { 50, 50, 50, 50, 80, 80, 80 }; // incomplete input - "2222PPP"
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(0, bytesWritten);
            }
        }

        [Fact]
        public void DecodeInPlaceInvalidBytesPadding()
        {
            // Only last 2 bytes can be padding, all other occurrence of padding is invalid
            for (int j = 0; j < 7; j++)
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 }; // valid input - "2222PPPP"
                buffer[j] = Base64TestHelper.s_encodingPad;
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
                buffer[6] = Base64TestHelper.s_encodingPad;
                buffer[7] = Base64TestHelper.s_encodingPad; // invalid input - "2222P*=="
                string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(3, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 42, 42, 42 };
                buffer[7] = Base64TestHelper.s_encodingPad; // invalid input - "2222P**="
                string sourceString = Encoding.ASCII.GetString(buffer.Slice(0, 4).ToArray());
                Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(3, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            // The last byte or the last 2 bytes being the padding character is valid
            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[6] = Base64TestHelper.s_encodingPad;
                buffer[7] = Base64TestHelper.s_encodingPad; // valid input - "2222PP=="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(4, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }

            {
                Span<byte> buffer = new byte[] { 50, 50, 50, 50, 80, 80, 80, 80 };
                buffer[7] = Base64TestHelper.s_encodingPad; // valid input - "2222PPP="
                string sourceString = Encoding.ASCII.GetString(buffer.ToArray());
                Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8InPlace(buffer, out int bytesWritten));
                Assert.Equal(5, bytesWritten);
                Span<byte> expectedBytes = Convert.FromBase64String(sourceString);
                Assert.True(expectedBytes.SequenceEqual(buffer.Slice(0, bytesWritten)));
            }
        }

    }
}
