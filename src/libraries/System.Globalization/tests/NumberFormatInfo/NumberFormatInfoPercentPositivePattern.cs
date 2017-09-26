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
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-MY").NumberFormat, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("tr").NumberFormat, 2 };
        }

        /// <summary>
        /// Not testing for Windows as the culture data can change
        /// https://blogs.msdn.microsoft.com/shawnste/2005/04/05/culture-data-shouldnt-be-considered-stable-except-for-invariant/
        /// In the CultureInfoAll test class we are testing the expected behavior 
        /// for Windows by enumerating all locales on the system and then test them. 
        /// </summary>
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [MemberData(nameof(PercentPositivePattern_TestData))]
        public void PercentPositivePattern_Get(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.PercentPositivePattern);
        }

        [Fact]
        public void PercentPositivePattern_Invariant_Get()
        {
            Assert.Equal(0, NumberFormatInfo.InvariantInfo.PercentPositivePattern);
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("PercentPositivePattern", () => new NumberFormatInfo().PercentPositivePattern = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("PercentPositivePattern", () => new NumberFormatInfo().PercentPositivePattern = 4);

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentPositivePattern = 1);
        }
    }
}
