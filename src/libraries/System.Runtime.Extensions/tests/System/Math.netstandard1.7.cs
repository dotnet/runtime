// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static partial class MathTests
    {
        [Fact]
        public static void BigMul()
        {
            Assert.Equal(4611686014132420609L, Math.BigMul(2147483647, 2147483647));
            Assert.Equal(0L, Math.BigMul(0, 0));
        }

        [Theory]
        [InlineData(1073741, 2147483647, 2000, 1647)]
        [InlineData(6, 13952, 2000, 1952)]
        [InlineData(0, 0, 2000, 0)]
        [InlineData(-7, -14032, 2000, -32)]
        [InlineData(-1073741, -2147483648, 2000, -1648)]
        [InlineData(-1073741, 2147483647, -2000, 1647)]
        [InlineData(-6, 13952, -2000, 1952)]
        public static void DivRem(int quotient, int dividend, int divisor, int expectedRemainder)
        {
            int remainder;
            Assert.Equal(quotient, Math.DivRem(dividend, divisor, out remainder));
            Assert.Equal(expectedRemainder, remainder);
        }

        [Theory]
        [InlineData(4611686018427387L, 9223372036854775807L, 2000L, 1807L)]
        [InlineData(4611686018427387L, -9223372036854775808L, -2000L, -1808L)]
        [InlineData(-4611686018427387L, 9223372036854775807L, -2000L, 1807L)]
        [InlineData(-4611686018427387L, -9223372036854775808L, 2000L, -1808L)]
        [InlineData(6L, 13952L, 2000L, 1952L)]
        [InlineData(0L, 0L, 2000L, 0L)]
        [InlineData(-7L, -14032L, 2000L, -32L)]
        [InlineData(-6L, 13952L, -2000L, 1952L)]
        public static void DivRemLong(long quotient, long dividend, long divisor, long expectedRemainder)
        {
            long remainder;
            Assert.Equal(quotient, Math.DivRem(dividend, divisor, out remainder));
            Assert.Equal(expectedRemainder, remainder);
        }
    }
}