// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFirstDayOfWeek
    {
        public static IEnumerable<object[]> FirstDayOfWeek_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, DayOfWeek.Sunday };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, DayOfWeek.Monday };
        }

        [Theory]
        [MemberData(nameof(FirstDayOfWeek_TestData))]
        public void FirstDayOfWeek(DateTimeFormatInfo format, DayOfWeek expected)
        {
            Assert.Equal(expected, format.FirstDayOfWeek);
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Tuesday)]
        [InlineData(DayOfWeek.Wednesday)]
        [InlineData(DayOfWeek.Thursday)]
        [InlineData(DayOfWeek.Friday)]
        [InlineData(DayOfWeek.Saturday)]
        public void FirstDayOfWeek_Set(DayOfWeek newFirstDayOfWeek)
        {
            var format = new DateTimeFormatInfo();
            format.FirstDayOfWeek = newFirstDayOfWeek;
            Assert.Equal(newFirstDayOfWeek, format.FirstDayOfWeek);
        }

        [Fact]
        public void FirstDayOfWeek_Set_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new DateTimeFormatInfo().FirstDayOfWeek = DayOfWeek.Sunday - 1); // Value is invalid
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new DateTimeFormatInfo().FirstDayOfWeek = DayOfWeek.Saturday + 1); // Value is invalid
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FirstDayOfWeek = DayOfWeek.Wednesday); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
