// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UrlValidationUnitTests : Base64TestBase
    {
        public static readonly byte[] s_encodingMap = {
            65, 66, 67, 68, 69, 70, 71, 72,         //A..H
            73, 74, 75, 76, 77, 78, 79, 80,         //I..P
            81, 82, 83, 84, 85, 86, 87, 88,         //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,      //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110, //g..n
            111, 112, 113, 114, 115, 116, 117, 118, //o..v
            119, 120, 121, 122, 48, 49, 50, 51,     //w..z, 0..3
            52, 53, 54, 55, 56, 57, 45, 95          //4..9, -, _
        };

        private static void InitializeDecodableBytes(Span<byte> bytes, int seed = 100)
        {
            var rnd = new Random(seed);
            for (int i = 0; i < bytes.Length; i++)
            {
                int index = (byte)rnd.Next(0, s_encodingMap.Length);
                bytes[i] = s_encodingMap[index];
            }
        }

        [Fact]
        public void BasicValidationBytes()
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
                InitializeDecodableBytes(source, numBytes);

                Assert.True(Base64Url.IsValid(source));
                Assert.True(Base64Url.IsValid(source, out int decodedLength));
                Assert.True(decodedLength > 0);
            }
        }

        [Fact]
        public void BasicValidationChars()
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
                InitializeDecodableBytes(source, numBytes);
                Span<char> chars = source
                    .ToArray()
                    .Select(Convert.ToChar)
                    .ToArray()
                    .AsSpan();

                Assert.True(Base64Url.IsValid(chars));
                Assert.True(Base64Url.IsValid(chars, out int decodedLength));
                Assert.True(decodedLength > 0);
            }
        }

        [Fact]
        public void BasicValidationInvalidInputLengthBytes()
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

                Assert.False(Base64Url.IsValid(source));
                Assert.False(Base64Url.IsValid(source, out int decodedLength));
                Assert.Equal(0, decodedLength);
            }
        }

        [Fact]
        public void BasicValidationInvalidInputLengthChars()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 10; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 == 0);    // ensure we have a invalid length

                Span<char> source = new char[numBytes];

                Assert.False(Base64Url.IsValid(source));
                Assert.False(Base64Url.IsValid(source, out int decodedLength));
                Assert.Equal(0, decodedLength);
            }
        }

        [Fact]
        public void ValidateEmptySpanBytes()
        {
            Span<byte> source = Span<byte>.Empty;

            Assert.True(Base64Url.IsValid(source));
            Assert.True(Base64Url.IsValid(source, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }

        [Fact]
        public void ValidateEmptySpanChars()
        {
            Span<char> source = Span<char>.Empty;

            Assert.True(Base64Url.IsValid(source));
            Assert.True(Base64Url.IsValid(source, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }

        [Fact]
        public void ValidateGuidBytes()
        {
            Span<byte> source = new byte[22];
            Span<byte> decodedBytes = Guid.NewGuid().ToByteArray();
            Base64Url.EncodeToUtf8(decodedBytes, source, out int _, out int _);

            Assert.True(Base64Url.IsValid(source));
            Assert.True(Base64Url.IsValid(source, out int decodedLength));
            Assert.True(decodedLength > 0);
        }

        [Fact]
        public void ValidateGuidChars()
        {
            Span<byte> source = new byte[22];
            Span<byte> decodedBytes = Guid.NewGuid().ToByteArray();
            Base64Url.EncodeToUtf8(decodedBytes, source, out int _, out int _);
            Span<char> chars = source
                .ToArray()
                .Select(Convert.ToChar)
                .ToArray()
                .AsSpan();

            Assert.True(Base64Url.IsValid(chars));
            Assert.True(Base64Url.IsValid(chars, out int decodedLength));
            Assert.True(decodedLength > 0);
        }

        [Theory]
        [MemberData(nameof(ValidBase64Strings_WithCharsThatMustBeIgnored))]
        public void ValidateBytesIgnoresCharsToBeIgnoredBytes(string utf8WithByteToBeIgnored, byte[] expectedBytes)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithByteToBeIgnored);

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedBytes.Length, decodedLength);
        }

        [Theory]
        [MemberData(nameof(ValidBase64Strings_WithCharsThatMustBeIgnored))]
        public void ValidateBytesIgnoresCharsToBeIgnoredChars(string utf8WithByteToBeIgnored, byte[] expectedBytes)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedBytes.Length, decodedLength);
        }

        [Theory]
        [MemberData(nameof(StringsOnlyWithCharsToBeIgnored))]
        public void ValidateWithOnlyCharsToBeIgnoredBytes(string utf8WithByteToBeIgnored)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithByteToBeIgnored);

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }

        [Theory]
        [MemberData(nameof(StringsOnlyWithCharsToBeIgnored))]
        public void ValidateWithOnlyCharsToBeIgnoredChars(string utf8WithByteToBeIgnored)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }

        [Theory]
        [InlineData("YQ==", 1)]
        [InlineData("YWI=", 2)]
        [InlineData("YWJj", 3)]
        [InlineData(" YWI=", 2)]
        [InlineData("Y WI=", 2)]
        [InlineData("YW I=", 2)]
        [InlineData("YWI =", 2)]
        [InlineData("YWI= ", 2)]
        [InlineData(" YQ==", 1)]
        [InlineData("Y Q==", 1)]
        [InlineData("YQ ==", 1)]
        [InlineData("YQ= =", 1)]
        [InlineData("YQ== ", 1)]
        [InlineData("YQ%%", 1)]
        [InlineData("YWI%", 2)]
        [InlineData("YW% ", 1)]
        public void ValidateWithPaddingReturnsCorrectCountBytes(string utf8WithByteToBeIgnored, int expectedLength)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithByteToBeIgnored);

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedLength, decodedLength);
        }

        [Theory]
        [InlineData("YQ==", 1)]
        [InlineData("YWI=", 2)]
        [InlineData("YWJj", 3)]
        [InlineData(" YWI=", 2)]
        [InlineData("Y WI=", 2)]
        [InlineData("YW I=", 2)]
        [InlineData("YWI =", 2)]
        [InlineData("YWI= ", 2)]
        [InlineData(" YQ==", 1)]
        [InlineData("Y Q==", 1)]
        [InlineData("YQ ==", 1)]
        [InlineData("YQ= =", 1)]
        [InlineData("YQ== ", 1)]
        [InlineData("YQ%%", 1)]
        [InlineData("YWI%", 2)]
        [InlineData("YW% ", 1)]
        public void ValidateWithPaddingReturnsCorrectCountChars(string utf8WithByteToBeIgnored, int expectedLength)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedLength, decodedLength);
        }

        [Theory]
        [InlineData("YQ==", 1)]
        [InlineData("YWI=", 2)]
        [InlineData("YWJj", 3)]
        public void DecodeEmptySpan(string utf8WithByteToBeIgnored, int expectedLength)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedLength, decodedLength);
        }

        [Theory]
        [InlineData("YWJ", true, 2)]
        [InlineData("YW", true, 1)]
        [InlineData("Y", false, 0)]
        public void SmallSizeBytes(string utf8Text, bool isValid, int expectedDecodedLength)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8Text);

            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedDecodedLength, decodedLength);
        }

        [Theory]
        [InlineData("YWJ", true, 2)]
        [InlineData("YW", true, 1)]
        [InlineData("Y", false, 0)]
        public void SmallSizeChars(string utf8Text, bool isValid, int expectedDecodedLength)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8Text;

            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedDecodedLength, decodedLength);
        }

        [Theory]
        [InlineData("YQ===")]
        [InlineData("YQ=a=")]
        [InlineData("YWI=a")]
        [InlineData(" aYWI=a")]
        [InlineData("a YWI=a")]
        [InlineData("aY WI=a")]
        [InlineData("aYW I=a")]
        [InlineData("aYWI =a")]
        [InlineData("aYWI= a")]
        [InlineData("a YQ==a")]
        [InlineData("aY Q==a")]
        [InlineData("aYQ ==a")]
        [InlineData("aYQ= =a")]
        [InlineData("aYQ== a")]
        [InlineData("aYQ==a ")]
        [InlineData("YQ+a")] // plus invalid
        [InlineData("/Qab")] // slash invalid
        public void InvalidBase64UrlBytes(string utf8WithByteToBeIgnored)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithByteToBeIgnored);

            Assert.False(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.False(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }

        [Theory]
        [InlineData("YQ===")]
        [InlineData("YQ=a=")]
        [InlineData("YWI=a")]
        [InlineData("a YWI=a")]
        [InlineData("aY WI=a")]
        [InlineData("aYW I=a")]
        [InlineData("aYWI =a")]
        [InlineData("aYWI= a")]
        [InlineData("a YQ==a")]
        [InlineData("aY Q==a")]
        [InlineData("aYQ ==a")]
        [InlineData("aYQ= =a")]
        [InlineData("aYQ== a")]
        [InlineData("aYQ==a ")]
        [InlineData("a")]
        [InlineData(" a")]
        [InlineData("  a")]
        [InlineData("   a")]
        [InlineData("    a")]
        [InlineData("a ")]
        [InlineData("a  ")]
        [InlineData("a   ")]
        [InlineData("a    ")]
        [InlineData(" a ")]
        [InlineData("  a  ")]
        [InlineData("   a   ")]
        [InlineData("    a    ")]
        public void InvalidBase64UrlChars(string utf8WithByteToBeIgnored)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8WithByteToBeIgnored);

            Assert.False(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.False(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }
    }
}
