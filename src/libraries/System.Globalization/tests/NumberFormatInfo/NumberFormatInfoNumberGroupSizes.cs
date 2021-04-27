// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberGroupSizes
    {
        public static IEnumerable<object[]> NumberGroupSizes_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, new int[] { 3 } };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, new int[] { 3 } };

            // Culture does not exist on Windows 7 and in Browser's ICU
            if (!PlatformDetection.IsWindows7 && PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ur-IN").NumberFormat, NumberFormatInfoData.UrINNumberGroupSizes() };
            }
        }

        [Theory]
        [MemberData(nameof(NumberGroupSizes_TestData))]
        public void NumberGroupSizes_Get_ReturnsExpected(NumberFormatInfo format, int[] expected)
        {
            Assert.Equal(expected, format.NumberGroupSizes);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 2, 3, 4 })]
        [InlineData(new int[] { 2, 3, 4, 0 })]
        [InlineData(new int[] { 0 })]
        public void NumberGroupSizes_Set_GetReturnsExpected(int[] newNumberGroupSizes)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberGroupSizes = newNumberGroupSizes;
            Assert.Equal(newNumberGroupSizes, format.NumberGroupSizes);
        }

        [Fact]
        public void NumberGroupSizes_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NumberGroupSizes", () => format.NumberGroupSizes = null);
        }

        [Theory]
        [InlineData(new int[] { -1, 1, 2 })]
        [InlineData(new int[] { 98, 99, 100 })]
        [InlineData(new int[] { 0, 1, 2 })]
        public void NumberGroupSizes_SetInvalid_ThrowsArgumentException(int[] value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", "NumberGroupSizes", () => format.NumberGroupSizes = value);
        }

        [Fact]
        public void NumberGroupSizes_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberGroupSizes = new int[] { 1, 2, 3 });
        }
    }
}
