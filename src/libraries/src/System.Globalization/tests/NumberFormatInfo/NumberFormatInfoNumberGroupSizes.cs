// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberGroupSizes
    {
        public static IEnumerable<object[]> NumberGroupSizes_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 } };
            yield return new object[] { new CultureInfo("en-US").NumberFormat, new int[] { 3 } };

            // TODO: when dotnet/corefx#2103 is addressed, we should also check fr-FR
            if (!PlatformDetection.IsUbuntu1510 && !PlatformDetection.IsUbuntu1604 && !PlatformDetection.IsUbuntu1610
                && !PlatformDetection.IsWindows7 && !PlatformDetection.IsWindows8x && !PlatformDetection.IsFedora24)
            {
                yield return new object[] { new CultureInfo("ur-IN").NumberFormat, NumberFormatInfoData.UrINNumberGroupSizes() };
            }
        }

        [Theory]
        [MemberData(nameof(NumberGroupSizes_TestData))]
        public void NumberGroupSizes_Get(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.NumberGroupSizes);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void NumberGroupSizes_Set(int[] newNumberGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberGroupSizes = newNumberGroupSizes;
            Assert.Equal(newNumberGroupSizes, format.NumberGroupSizes);
        }

        [Fact]
        public void NumberGroupSizes_Set_Invalid()
        {
            Assert.Throws<ArgumentNullException>("NumberGroupSizes", () => new NumberFormatInfo().NumberGroupSizes = null);

            Assert.Throws<ArgumentException>("NumberGroupSizes", () => new NumberFormatInfo().NumberGroupSizes = new int[] { -1, 1, 2 });
            Assert.Throws<ArgumentException>("NumberGroupSizes", () => new NumberFormatInfo().NumberGroupSizes = new int[] { 98, 99, 100 });
            Assert.Throws<ArgumentException>("NumberGroupSizes", () => new NumberFormatInfo().NumberGroupSizes = new int[] { 0, 1, 2 });

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
