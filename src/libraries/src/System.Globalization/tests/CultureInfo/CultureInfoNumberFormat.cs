// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNumberFormat
    {
        public static IEnumerable<object[]> NumberFormatInfo_Set_TestData()
        {
            NumberFormatInfo customNumberFormatInfo1 = new NumberFormatInfo();
            customNumberFormatInfo1.NegativeInfinitySymbol = "a";
            yield return new object[] { "en-US", customNumberFormatInfo1 };

            NumberFormatInfo customNumberFormatInfo2 = new NumberFormatInfo();
            customNumberFormatInfo2.PositiveSign = "b";
            yield return new object[] { "fi-FI", customNumberFormatInfo2 };
        }

        [Theory]
        [MemberData(nameof(NumberFormatInfo_Set_TestData))]
        public void NumberFormatInfo_Set(string name, NumberFormatInfo newNumberFormatInfo)
        {
            CultureInfo culture = new CultureInfo(name);
            culture.NumberFormat = newNumberFormatInfo;
            Assert.Equal(newNumberFormatInfo, culture.NumberFormat);
        }

        [Fact]
        public void NumberFormat_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CultureInfo("en-US").NumberFormat = null);
            Assert.Throws<InvalidOperationException>(() => CultureInfo.InvariantCulture.NumberFormat = new NumberFormatInfo());
        }
    }
}
