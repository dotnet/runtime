// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentGroupSizes
    {
        public static IEnumerable<object[]> PercentGroupSizes_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 } };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, new int[] { 3 } };
        }

        [Theory]
        [MemberData(nameof(PercentGroupSizes_TestData))]
        public void PercentGroupSizes_Get(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.PercentGroupSizes);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void PercentGroupSizes_Set(int[] newPercentGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentGroupSizes = newPercentGroupSizes;
            Assert.Equal(newPercentGroupSizes, format.PercentGroupSizes);
        }

        [Fact]
        public void PercentGroupSizes_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PercentGroupSizes", () => new NumberFormatInfo().PercentGroupSizes = null);

            AssertExtensions.Throws<ArgumentException>("PercentGroupSizes", () => new NumberFormatInfo().PercentGroupSizes = new int[] { -1, 1, 2 });
            AssertExtensions.Throws<ArgumentException>("PercentGroupSizes", () => new NumberFormatInfo().PercentGroupSizes = new int[] { 98, 99, 100 });
            AssertExtensions.Throws<ArgumentException>("PercentGroupSizes", () => new NumberFormatInfo().PercentGroupSizes = new int[] { 0, 1, 2 });

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
