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
        public void PercentGroupSizes_Get_ReturnsExpected(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.PercentGroupSizes);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void PercentGroupSizes_Set_GetReturnsExpected(int[] newPercentGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentGroupSizes = newPercentGroupSizes;
            Assert.Equal(newPercentGroupSizes, format.PercentGroupSizes);
        }

        [Fact]
        public void PercentGroupSizes_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PercentGroupSizes", () => format.PercentGroupSizes = null);
        }

        [Theory]
        [InlineData(new int[] { -1, 1, 2 })]
        [InlineData(new int[] { 98, 99, 100 })]
        [InlineData(new int[] { 0, 1, 2 })]
        public void PercentGroupSizes_SetInvalid_ThrowsArgumentException(int[] value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", "PercentGroupSizes", () => format.PercentGroupSizes = value);
        }

        [Fact]
        public void PercentGroupSizes_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
