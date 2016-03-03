// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetDayName
    {
        [Fact]
        public static IEnumerable<object[]> GetDayName_TestData()
        {
            string[] englishDayNames = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, englishDayNames };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, englishDayNames };
            yield return new object[] { new DateTimeFormatInfo(), englishDayNames };

            // ActiveIssue(2103)
            if (!PlatformDetection.IsUbuntu1510)
            {
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, DateTimeFormatInfoData.FrFRDayNames() };
            }
        }

        [Theory]
        [MemberData(nameof(GetDayName_TestData))]
        public void GetDayName(DateTimeFormatInfo format, string[] expected)
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

        public void GetDayName_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new DateTimeFormatInfo().GetDayName(DayOfWeek.Sunday - 1)); // DayOfWeek is invalid
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new DateTimeFormatInfo().GetDayName(DayOfWeek.Saturday + 1)); // DayOfWeek is invalid
        }
    }
}
