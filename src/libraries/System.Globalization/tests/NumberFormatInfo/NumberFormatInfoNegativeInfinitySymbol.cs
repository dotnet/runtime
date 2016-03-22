// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeInfinitySymbol
    {
        public static IEnumerable<object[]> NegativeInfinitySymbol_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "-Infinity" };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, NumberFormatInfoData.GetNegativeInfinitySymbol("en-US") };
            yield return new object[] { new CultureInfo("fr-FR").NumberFormat, NumberFormatInfoData.GetNegativeInfinitySymbol("fr-FR") };
        }

        [Theory]
        [MemberData(nameof(NegativeInfinitySymbol_TestData))]
        public void NegativeInfinitySymbol_Get(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NegativeInfinitySymbol);
        }
    }
}
