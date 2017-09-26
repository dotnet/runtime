// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static partial class GuidTests
    {
        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void Ctor_ReadOnlySpan(byte[] b, Guid expected)
        {
            Assert.Equal(expected, new Guid(new ReadOnlySpan<byte>(b)));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public static void CtorSpan_InvalidLengthByteArray_ThrowsArgumentException(int length)
        {
            AssertExtensions.Throws<ArgumentException>("b", null, () => new Guid(new ReadOnlySpan<byte>(new byte[length])));
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void TryWriteBytes_ValidLength_ReturnsTrue(byte[] b, Guid guid)
        {
            var bytes = new byte[16];
            Assert.True(guid.TryWriteBytes(new Span<byte>(bytes)));
            Assert.Equal(b, bytes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        public static void TryWriteBytes_LengthTooShort_ReturnsFalse(int length)
        {
            Assert.False(s_testGuid.TryWriteBytes(new Span<byte>(new byte[length])));
        }

        [Theory]
        [InlineData("Y")]
        [InlineData("XX")]
        public static void TryFormat_InvalidFormat_ThrowsFormatException(string format)
        {
            Assert.Throws<FormatException>(() => s_testGuid.TryFormat(new Span<char>(), out int charsWritten, format));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_LengthTooSmall_ReturnsFalse(Guid guid, string format, string expected)
        {
            Assert.False(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out int charsWritten, format));
            Assert.Equal(0, charsWritten);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_CharsWritten_EqualsZero_WhenSpanTooSmall(Guid guid, string format, string expected)
        {
            Assert.False(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out int charsWritten, format));
            Assert.Equal(0, charsWritten);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_ValidLength_ReturnsTrue(Guid guid, string format, string expected)
        {
            char[] chars = new char[guid.ToString(format).Length];
            Assert.True(guid.TryFormat(new Span<char>(chars), out int charsWritten, format));
            Assert.Equal(chars, expected.ToCharArray());
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Valid_TestData))]
        public static void Parse_Span_ValidInput_Success(string input, string format, Guid expected)
        {
            Assert.Equal(expected, Guid.Parse(input.AsReadOnlySpan()));
            Assert.Equal(expected, Guid.ParseExact(input.AsReadOnlySpan(), format.ToUpperInvariant()));
            Assert.Equal(expected, Guid.ParseExact(input.AsReadOnlySpan(), format.ToLowerInvariant())); // Format should be case insensitive

            Guid result;

            Assert.True(Guid.TryParse(input.AsReadOnlySpan(), out result));
            Assert.Equal(expected, result);

            Assert.True(Guid.TryParseExact(input.AsReadOnlySpan(), format.ToUpperInvariant(), out result));
            Assert.Equal(expected, result);

            Assert.True(Guid.TryParseExact(input.AsReadOnlySpan(), format.ToLowerInvariant(), out result)); // Format should be case insensitive
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Invalid_TestData))]
        public static void Parse_Span_InvalidInput_Fails(string input, Type exceptionType)
        {
            if (input == null)
            {
                return;
            }

            // Overflow exceptions throw as format exceptions in Parse
            if (exceptionType.Equals(typeof(OverflowException)))
            {
                exceptionType = typeof(FormatException);
            }
            Assert.Throws(exceptionType, () => Guid.Parse(input.AsReadOnlySpan()));

            Assert.False(Guid.TryParse(input.AsReadOnlySpan(), out Guid result));
            Assert.Equal(Guid.Empty, result);

            foreach (string format in new[] { "N", "D", "B", "P", "X" })
            {
                Assert.Throws(exceptionType, () => Guid.ParseExact(input.AsReadOnlySpan(), format));

                Assert.False(Guid.TryParseExact(input.AsReadOnlySpan(), format, out result));
                Assert.Equal(Guid.Empty, result);
            }
        }
    }
}
