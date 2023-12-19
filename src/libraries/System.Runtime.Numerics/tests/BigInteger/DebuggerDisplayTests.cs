// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Numerics.Tests
{
    public class DebuggerDisplayTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming))]
        [InlineData(new uint[] { 0, 0, 1 }, "18446744073709551616")]
        [InlineData(new uint[] { 0, 0, 0, 0, 1 }, "3.40282367e+38")]
        [InlineData(new uint[] { 0, 0x12345678, 0, 0xCC00CC00, 0x80808080 }, "7.33616508e+47")]
        public void TestDebuggerDisplay(uint[] bits, string displayString)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                BigInteger positiveValue = new BigInteger(1, bits);
                Assert.Equal(displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(positiveValue));

                BigInteger negativeValue = new BigInteger(-1, bits);
                Assert.Equal("-" + displayString, DebuggerAttributes.ValidateDebuggerDisplayReferences(negativeValue));
            }
        }
    }
}
