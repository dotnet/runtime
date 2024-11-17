// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class SpanUtilityTests
    {
        public static IEnumerable<object[]> IsValidIndexTestData()
        {
            yield return new object[] { "", -1, false };
            yield return new object[] { "", 0, false };
            yield return new object[] { "", 1, false };
            yield return new object[] { "x", -1, false };
            yield return new object[] { "x", 0, true };
            yield return new object[] { "x", 1, false };
            yield return new object[] { "Hello", -1, false };
            yield return new object[] { "Hello", 0, true };
            yield return new object[] { "Hello", 4, true };
            yield return new object[] { "Hello", 5, false };
        }

        [Theory]
        [MemberData(nameof(IsValidIndexTestData))]
        public void IsValidIndex_ReadOnlySpan(string inputData, int index, bool expectedValue)
        {
            ReadOnlySpan<char> span = inputData.AsSpan();
            Assert.Equal(expectedValue, SpanUtility.IsValidIndex(span, index));
        }

        [Theory]
        [MemberData(nameof(IsValidIndexTestData))]
        public void IsValidIndex_Span(string inputData, int index, bool expectedValue)
        {
            Span<char> span = inputData.ToCharArray();
            Assert.Equal(expectedValue, SpanUtility.IsValidIndex(span, index));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, -1)]
        [InlineData(7, 0)]
        [InlineData(7, 1)]
        [InlineData(7, -1)]
        [InlineData(8, 1)]
        [InlineData(8, 8)]
        [InlineData(8, -1)]
        [InlineData(8, int.MaxValue)]
        [InlineData(8, int.MaxValue - 8)]
        [InlineData(int.MaxValue, int.MaxValue - 7)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MaxValue, -1)]
        [InlineData(int.MaxValue, int.MinValue)]
        public unsafe void TryWriteUInt64LittleEndian_FailureCases(int spanLength, int offset)
        {
            // fabricate a span of the correct length - we can't deref it because it'll AV
            Span<byte> span = new Span<byte>((byte*)null, spanLength);
            Assert.False(SpanUtility.TryWriteUInt64LittleEndian(span, offset, 0xdeadbeef_deadbeef));
        }

        [Fact]
        public void TryWriteUInt64LittleEndian_SuccessCases()
        {
            Span<byte> span = stackalloc byte[10] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
            Assert.True(SpanUtility.TryWriteUInt64LittleEndian(span, 0, 0x10203040_50607080));
            Assert.Equal(new byte[] { 0x80, 0x70, 0x60, 0x50, 0x40, 0x30, 0x20, 0x10, 0x08, 0x09 }, span.ToArray());

            Assert.True(SpanUtility.TryWriteUInt64LittleEndian(span, 1, 0x1a2a3a4a_5a6a7a8a));
            Assert.Equal(new byte[] { 0x80, 0x8a, 0x7a, 0x6a, 0x5a, 0x4a, 0x3a, 0x2a, 0x1a, 0x09 }, span.ToArray());

            Assert.True(SpanUtility.TryWriteUInt64LittleEndian(span, 2, 0x1f2f3f4f_5f6f7f8f));
            Assert.Equal(new byte[] { 0x80, 0x8a, 0x8f, 0x7f, 0x6f, 0x5f, 0x4f, 0x3f, 0x2f, 0x1f }, span.ToArray());
        }
    }
}
