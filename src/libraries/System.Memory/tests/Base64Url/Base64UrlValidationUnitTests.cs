// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UrlValidationUnitTests : Base64TestBase
    {
        [Theory]
        [InlineData("==")]
        [InlineData("-%")]
        [InlineData("A=")]
        [InlineData("A==")]
        [InlineData("4%%")]
        [InlineData(" A==")]
        [InlineData("AAAAA ==")]
        [InlineData("\tLLLL\t=\r")]
        [InlineData("6066=")]
        public void BasicValidationEdgeCaseScenario(string base64UrlText)
        {
            Assert.False(Base64Url.IsValid(base64UrlText.AsSpan(), out int decodedLength));
            Assert.Equal(0, decodedLength);
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
                } while (numBytes % 4 == 1);    // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);
                source[numBytes - 1] = 65; // make sure unused bits set 0

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
                } while (numBytes % 4 == 1);    // ensure we have a valid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);
                source[numBytes - 1] = 65; // make sure unused bits set 0
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
                } while (numBytes % 4 != 1);    // only remainder of 1 is invalid length

                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeUrlDecodableBytes(source, numBytes);
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
                } while (numBytes % 4 != 1);    // ensure we have a invalid length

                Span<char> source = new char[numBytes];
                Base64TestHelper.InitializeUrlDecodableChars(source, numBytes);

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
        [InlineData("YQ% ", 1)]
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
        [InlineData("YQ% ", 1)]
        public void ValidateWithPaddingReturnsCorrectCountChars(string utf8WithByteToBeIgnored, int expectedLength)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8WithByteToBeIgnored.ToArray();

            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.True(Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedLength, decodedLength);
        }

        [Theory]
        [InlineData("YWI", true, 2)]
        [InlineData("YQ", true, 1)]
        [InlineData("Y", false, 0)]
        public void SmallSizeBytes(string utf8Text, bool isValid, int expectedDecodedLength)
        {
            byte[] utf8BytesWithByteToBeIgnored = UTF8Encoding.UTF8.GetBytes(utf8Text);

            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored));
            Assert.Equal(isValid, Base64Url.IsValid(utf8BytesWithByteToBeIgnored, out int decodedLength));
            Assert.Equal(expectedDecodedLength, decodedLength);
        }

        [Theory]
        [InlineData("YWI", true, 2)]
        [InlineData("YQ", true, 1)]
        [InlineData("Y", false, 0)]
        public void SmallSizeChars(string utf8Text, bool isValid, int expectedDecodedLength)
        {
            ReadOnlySpan<char> utf8BytesWithByteToBeIgnored = utf8Text.AsSpan();

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
            ReadOnlySpan<char> utf8CharsWithCharToBeIgnored = utf8WithByteToBeIgnored.AsSpan();

            Assert.False(Base64Url.IsValid(utf8CharsWithCharToBeIgnored));
            Assert.False(Base64Url.IsValid(utf8CharsWithCharToBeIgnored, out int decodedLength));
            Assert.Equal(0, decodedLength);
        }
    }
}
