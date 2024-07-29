// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetDayName
    {
        public static IEnumerable<object[]> GetDayName_TestData()
        {
            string[] englishDayNames = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, englishDayNames };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, englishDayNames };
            yield return new object[] { new DateTimeFormatInfo(), englishDayNames };

            if (!PlatformDetection.IsUbuntu)
            {
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, DateTimeFormatInfoData.FrFRDayNames() };
            }
        }

        [Theory]
        [MemberData(nameof(GetDayName_TestData))]
        public void GetDayName_Invoke_ReturnsExpected(DateTimeFormatInfo format, string[] expected)
        {
            DayOfWeek[] values = new DayOfWeek[]
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            for (int i = 0; i < values.Length; ++i)
            {
                Assert.Equal(expected[i], format.GetDayName(values[i]));
            }
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday - 1)]
        [InlineData(DayOfWeek.Saturday + 1)]
        public void GetDayName_InvalidDayOfWeek_ThrowsArgumentOutOfRangeException(DayOfWeek dayofweek)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dayofweek", () => format.GetDayName(dayofweek));
        }
    }
}
