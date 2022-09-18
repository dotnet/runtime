// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class DebuggerDisplayTests
    {
        [Theory]
        [InlineData(new uint[] { 0, 0, 1 }, "1.84467441e+19")]
        [InlineData(new uint[] { 0, 0, 0, 1 }, "7.92281625e+28")]
        [InlineData(new uint[] { 0, 0xCC00CC00, 0x80808080 }, "3.97694306e+28")]
        public void TestDebuggerDisplay(uint[] bits, string displayString)
        {
            BigInteger positiveValue = new BigInteger(1, bits);
            Assert.Equal(displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(positiveValue));

            BigInteger negativeValue = new BigInteger(-1, bits);
            Assert.Equal("-" + displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(negativeValue));
        }
    }
}
