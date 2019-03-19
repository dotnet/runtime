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
        [MemberData(nameof(PercentNegativePattern_TestData))]
        public void PercentNegativePattern_Get_ReturnsExpected(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.PercentNegativePattern);
        }

        [Fact]
        public void PercentNegativePattern_GetInvariant_ReturnsExpected()
        {
            Assert.Equal(0, NumberFormatInfo.InvariantInfo.PercentNegativePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public void PercentNegativePattern_Set_GetReturnsExpected(int newPercentNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentNegativePattern = newPercentNegativePattern;
            Assert.Equal(newPercentNegativePattern, format.PercentNegativePattern);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(12)]
        public void PercentNegativePattern_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "PercentNegativePattern", () => format.PercentNegativePattern = value);
        }

        [Fact]
        public void PercentNegativePattern_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentNegativePattern = 1);
        }
    }
}
