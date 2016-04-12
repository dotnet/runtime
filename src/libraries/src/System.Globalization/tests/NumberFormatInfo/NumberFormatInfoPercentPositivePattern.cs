// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentPositivePattern
    {
        public static IEnumerable<object[]> PercentPositivePattern_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 0, 0 };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, 0, 1 };
            yield return new object[] { new CultureInfo("en-MY").NumberFormat, 1, 1 };
            yield return new object[] { new CultureInfo("tr").NumberFormat, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(PercentPositivePattern_TestData))]
        public void PercentPositivePattern_Get(NumberFormatInfo format, int expectedWindows, int expectedIcu)
        {
            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedIcu;
            Assert.Equal(expected, format.PercentPositivePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void PercentPositivePattern_Set(int newPercentPositivePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentPositivePattern = newPercentPositivePattern;
            Assert.Equal(newPercentPositivePattern, format.PercentPositivePattern);
        }

        [Fact]
        public void PercentPositivePattern_Set_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("PercentPositivePattern", () => new NumberFormatInfo().PercentPositivePattern = -1);
            Assert.Throws<ArgumentOutOfRangeException>("PercentPositivePattern", () => new NumberFormatInfo().PercentPositivePattern = 4);

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentPositivePattern = 1);
        }
    }
}
