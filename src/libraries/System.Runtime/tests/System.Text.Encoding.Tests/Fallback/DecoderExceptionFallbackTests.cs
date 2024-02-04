// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class DecoderExceptionFallbackTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            DecoderExceptionFallback fallback = new DecoderExceptionFallback();
            Assert.Equal(0, fallback.MaxCharCount);
            Assert.Equal(879, fallback.GetHashCode());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DecoderExceptionFallback(), new DecoderExceptionFallback(), true };
            yield return new object[] { new DecoderExceptionFallback(), new object(), false };
            yield return new object[] { new DecoderExceptionFallback(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(DecoderExceptionFallback fallback, object value, bool expected)
        {
            Assert.Equal(expected, fallback.Equals(value));
        }

        [Fact]
        public void CreateFallbackBuffer()
        {
            DecoderFallbackBuffer buffer = new DecoderExceptionFallback().CreateFallbackBuffer();

            Assert.Equal((char)0, buffer.GetNextChar());
            Assert.False(buffer.MovePrevious());
            Assert.Equal(0, buffer.Remaining);

            Assert.Throws<DecoderFallbackException>(() => buffer.Fallback(new byte[0], 0));
            Assert.Throws<DecoderFallbackException>(() => buffer.Fallback(new byte[25], 0));
        }
    }
}
