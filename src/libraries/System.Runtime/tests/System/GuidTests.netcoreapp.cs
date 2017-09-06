// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public static void TryWriteBytes(byte[] b, Guid guid)
        {
            var bytes = new byte[16];
            Assert.True(guid.TryWriteBytes(new Span<byte>(bytes)));
            Assert.Equal(b, bytes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        public static void TryWriteBytes_ReturnsFalse(int length)
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
        public static void TryFormat_ReturnsTrue(Guid guid, string format, string expected)
        {
            Assert.True(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length]), out int charsWritten, format));
            Assert.Equal(guid.ToString(format).Length, charsWritten);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_ReturnsFalse_WhenSpanTooSmall(Guid guid, string format, string expected)
        {
            Assert.False(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out int charsWritten, format));
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
        public static void TryFormat_Valid(Guid guid, string format, string expected)
        {
            char[] chars = new char[guid.ToString(format).Length];
            Assert.True(guid.TryFormat(new Span<char>(chars), out int charsWritten, format));
            Assert.Equal(chars, expected.ToCharArray());
        }
    }
}
