// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UnicodeAPIsUnitTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("t")]
        [InlineData("te")]
        [InlineData("tes")]
        [InlineData("test")]
        [InlineData("test/")]
        [InlineData("test/+")]
        public static void DecodeEncodeToFromCharsStringRoundTrip(string str)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(str);
            // Base64 uses padding so the expected written length is different
            int expectedEncodedLength = Base64.GetEncodedLength(inputBytes.Length);
            Span<char> resultChars = new char[expectedEncodedLength];
            OperationStatus operationStatus = Base64.EncodeToChars(inputBytes, resultChars, out int bytesConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.Done, operationStatus);
            Assert.Equal(str.Length, bytesConsumed);
            Assert.Equal(expectedEncodedLength, charsWritten);
            string result = Base64.EncodeToString(inputBytes);
            Assert.Equal(result, resultChars.ToString());
            Assert.Equal(expectedEncodedLength, Base64.EncodeToChars(inputBytes, resultChars));
            Assert.True(Base64.TryEncodeToChars(inputBytes, resultChars, out charsWritten));
            Assert.Equal(expectedEncodedLength, charsWritten);
            Assert.Equal(result, resultChars.ToString());

            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedLength(resultChars.Length)];
            operationStatus = Base64.DecodeFromChars(resultChars, decodedBytes, out bytesConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.Done, operationStatus);
            Assert.Equal(resultChars.Length, bytesConsumed);
            Assert.Equal(str.Length, bytesWritten);
            Assert.Equal(inputBytes, decodedBytes.Slice(0, bytesWritten).ToArray());
            Assert.Equal(str.Length, Base64.DecodeFromChars(resultChars, decodedBytes));
            Assert.True(Base64.TryDecodeFromChars(resultChars, decodedBytes, out int tryDecodeBytesWritten));
            Assert.Equal(str.Length, tryDecodeBytesWritten);
            Assert.Equal(inputBytes, decodedBytes.Slice(0, tryDecodeBytesWritten).ToArray());
            Assert.Equal(str, Encoding.UTF8.GetString(decodedBytes.Slice(0, tryDecodeBytesWritten)));
        }

        [Fact]
        public void EncodingWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                Span<char> encodedBytes = new char[Base64.GetEncodedLength(source.Length)];
                OperationStatus result = Base64.EncodeToChars(source, encodedBytes, out int consumed, out int encodedBytesCount);
                Assert.Equal(OperationStatus.Done, result);
                Assert.Equal(source.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);
                string expectedText = Convert.ToBase64String(source);
                Assert.Equal(expectedText, encodedBytes.ToString());
            }
        }

        [Fact]
        public void DecodeWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                // Ensure we have a valid length (multiple of 4 for standard Base64)
                numBytes = (numBytes / 4) * 4;
                if (numBytes == 0) numBytes = 4;

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

        [Fact]
        public void RoundTripWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                int expectedLength = Base64.GetEncodedLength(source.Length);
                char[] encodedBytes = Base64.EncodeToChars(source);
                Assert.Equal(expectedLength, encodedBytes.Length);
                Assert.Equal(new String(encodedBytes), Base64.EncodeToString(source));

                byte[] decoded = Base64.DecodeFromChars(encodedBytes);
                Assert.Equal(source.ToArray(), decoded);
            }
        }

        public static IEnumerable<object[]> EncodeToStringTests_TestData()
        {
            yield return new object[] { Enumerable.Range(0, 0).Select(i => (byte)i).ToArray(), "" };
            yield return new object[] { Enumerable.Range(0, 1).Select(i => (byte)i).ToArray(), "AA==" };
            yield return new object[] { Enumerable.Range(0, 2).Select(i => (byte)i).ToArray(), "AAE=" };
            yield return new object[] { Enumerable.Range(0, 3).Select(i => (byte)i).ToArray(), "AAEC" };
            yield return new object[] { Enumerable.Range(0, 4).Select(i => (byte)i).ToArray(), "AAECAw==" };
            yield return new object[] { Enumerable.Range(0, 5).Select(i => (byte)i).ToArray(), "AAECAwQ=" };
            yield return new object[] { Enumerable.Range(0, 6).Select(i => (byte)i).ToArray(), "AAECAwQF" };
        }

        [Theory]
        [MemberData(nameof(EncodeToStringTests_TestData))]
        public static void EncodeToStringTests(byte[] inputBytes, string expectedBase64)
        {
            Assert.Equal(expectedBase64, Base64.EncodeToString(inputBytes));
            Span<char> chars = new char[Base64.GetEncodedLength(inputBytes.Length)];
            Assert.Equal(OperationStatus.Done, Base64.EncodeToChars(inputBytes, chars, out int _, out int charsWritten));
            Assert.Equal(expectedBase64, chars.Slice(0, charsWritten).ToString());
        }

        [Fact]
        public void EncodingOutputTooSmall()
        {
            for (int numBytes = 4; numBytes < 20; numBytes++)
            {
                byte[] source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);
                int expectedConsumed = 3;
                char[] encodedBytes = new char[4];

                Assert.Equal(OperationStatus.DestinationTooSmall, Base64.EncodeToChars(source, encodedBytes, out int consumed, out int written));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(encodedBytes.Length, written);

                Assert.Throws<ArgumentException>("destination", () => Base64.EncodeToChars(source, encodedBytes));
            }
        }

        [Theory]
        [InlineData("\u5948cz/T", 0, 0)]                                              // scalar code-path with non-ASCII
        [InlineData("z/Ta123\u5948", 4, 3)]
        public void BasicDecodingNonAsciiInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<char> source = inputString.ToArray();
            Span<byte> decodedBytes = new byte[Base64.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromChars(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
        }

        [Fact]
        public static void Roundtrip()
        {
            string input = "dGVzdA=="; // "test" encoded
            byte[] decodedBytes = Base64.DecodeFromChars(input);
            Assert.Equal(4, decodedBytes.Length);
            Assert.Equal("test", Encoding.UTF8.GetString(decodedBytes));

            string roundtrippedString = Base64.EncodeToString(decodedBytes);
            Assert.Equal(input, roundtrippedString);
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
        public void EncodeToUtf8_ArrayOverload()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            byte[] result = Base64.EncodeToUtf8(input);
            Assert.Equal("dGVzdA==", Encoding.UTF8.GetString(result));
        }

        [Fact]
        public void EncodeToUtf8_SpanOverload()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            Span<byte> destination = new byte[20];
            int bytesWritten = Base64.EncodeToUtf8(input, destination);
            Assert.Equal(8, bytesWritten);
            Assert.Equal("dGVzdA==", Encoding.UTF8.GetString(destination.Slice(0, bytesWritten)));
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
        public void TryEncodeToUtf8_Success()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            Span<byte> destination = new byte[20];
            Assert.True(Base64.TryEncodeToUtf8(input, destination, out int bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal("dGVzdA==", Encoding.UTF8.GetString(destination.Slice(0, bytesWritten)));
        }

        [Fact]
        public void TryEncodeToUtf8_DestinationTooSmall()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            Span<byte> destination = new byte[4]; // Too small
            Assert.False(Base64.TryEncodeToUtf8(input, destination, out int bytesWritten));
        }

        [Fact]
        public void TryEncodeToUtf8InPlace_Success()
        {
            byte[] buffer = new byte[20];
            buffer[0] = (byte)'t';
            buffer[1] = (byte)'e';
            buffer[2] = (byte)'s';
            buffer[3] = (byte)'t';

            Assert.True(Base64.TryEncodeToUtf8InPlace(buffer, 4, out int bytesWritten));
            Assert.Equal(8, bytesWritten);
            Assert.Equal("dGVzdA==", Encoding.UTF8.GetString(buffer.AsSpan(0, bytesWritten)));
        }

        [Fact]
        public void TryEncodeToUtf8InPlace_DestinationTooSmall()
        {
            byte[] buffer = new byte[4]; // Same size as input, which is too small for encoded output
            buffer[0] = (byte)'t';
            buffer[1] = (byte)'e';
            buffer[2] = (byte)'s';
            buffer[3] = (byte)'t';

            Assert.False(Base64.TryEncodeToUtf8InPlace(buffer, 4, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void GetEncodedLength_MatchesExisting()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(Base64.GetMaxEncodedToUtf8Length(i), Base64.GetEncodedLength(i));
            }
        }

        [Fact]
        public void GetMaxDecodedLength_MatchesExisting()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(Base64.GetMaxDecodedFromUtf8Length(i), Base64.GetMaxDecodedLength(i));
            }
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
        public void EncodeToChars_DestinationTooSmall()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            char[] destination = new char[4]; // Too small
            Assert.Throws<ArgumentException>("destination", () => Base64.EncodeToChars(input, destination));
        }

        [Fact]
        public void TryDecodeFromChars_DestinationTooSmall()
        {
            string validInput = "dGVzdA=="; // "test" encoded
            Span<byte> destination = new byte[2]; // Too small
            Assert.False(Base64.TryDecodeFromChars(validInput, destination, out int bytesWritten));
        }

        [Fact]
        public void TryEncodeToChars_DestinationTooSmall()
        {
            byte[] input = Encoding.UTF8.GetBytes("test");
            Span<char> destination = new char[4]; // Too small
            Assert.False(Base64.TryEncodeToChars(input, destination, out int charsWritten));
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
    }
}
