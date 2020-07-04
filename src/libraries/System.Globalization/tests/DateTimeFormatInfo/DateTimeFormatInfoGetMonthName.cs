// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetMonthName
    {
        private const int MinMonth = 1;
        private const int MaxMonth = 13;

        public static IEnumerable<object[]> GetMonthName_TestData()
        {
            string[] englishMonthNames = new string[] { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, englishMonthNames };
            yield return new object[] { new DateTimeFormatInfo(), englishMonthNames };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, englishMonthNames };

            if (!PlatformDetection.IsUbuntu)
            {
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, new string[] { "", "janvier", "f\u00E9vrier", "mars", "avril", "mai", "juin", "juillet", "ao\u00FBt", "septembre", "octobre", "novembre", "d\u00E9cembre", "" } };
            }
        }

        [Theory]
        [MemberData(nameof(GetMonthName_TestData))]
        public void GetMonthName_Invoke_ReturnsExpected(DateTimeFormatInfo format, string[] expected)
        {
            for (int i = MinMonth; i <= MaxMonth; ++i)
            {
                Assert.Equal(expected[i], format.GetMonthName(i));
            }
        }

        [Theory]
        [InlineData(MinMonth - 1)]
        [InlineData(MaxMonth + 1)]
        public void GetMonthName_InvalidMonth_ThrowsArgumentOutOfRangeException(int month)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("month", () => format.GetMonthName(month));
        }
    }
}
