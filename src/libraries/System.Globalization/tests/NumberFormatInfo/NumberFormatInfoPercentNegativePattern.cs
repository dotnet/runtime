// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentNegativePattern
    {
        public static IEnumerable<object[]> PercentNegativePattern_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 0, 0 };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, 0, 1 };
            yield return new object[] { new CultureInfo("en-MY").NumberFormat, 1, 1 };
            yield return new object[] { new CultureInfo("tr").NumberFormat, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(PercentNegativePattern_TestData))]
        public void PercentNegativePattern_Get(NumberFormatInfo format, int expectedWindows, int expectedIcu)
        {
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;
            Assert.Equal(expected, format.PercentNegativePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public void PercentNegativePattern_Set(int newPercentNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentNegativePattern = newPercentNegativePattern;
            Assert.Equal(newPercentNegativePattern, format.PercentNegativePattern);
        }

        [Fact]
        public void PercentNegativePattern_Set_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("PercentNegativePattern", () => new NumberFormatInfo().PercentNegativePattern = -1);
            Assert.Throws<ArgumentOutOfRangeException>("PercentNegativePattern", () => new NumberFormatInfo().PercentNegativePattern = 12);

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentNegativePattern = 1);
        }
    }
}
