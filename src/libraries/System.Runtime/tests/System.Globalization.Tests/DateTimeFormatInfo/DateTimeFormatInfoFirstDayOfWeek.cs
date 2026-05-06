// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFirstDayOfWeek
    {
        public static IEnumerable<object[]> FirstDayOfWeek_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, DayOfWeek.Sunday, "invariant" };
            yield return new object[] { new CultureInfo("en-US", false).DateTimeFormat, DayOfWeek.Sunday, "en-US" };
            yield return new object[] { new CultureInfo("fr-FR", false).DateTimeFormat, DayOfWeek.Monday, "fr-FR" };
        }

        [Theory]
        [MemberData(nameof(FirstDayOfWeek_Get_TestData))]
        public void FirstDayOfWeek(DateTimeFormatInfo format, DayOfWeek expected, string cultureName)
        {
            Assert.True(expected == format.FirstDayOfWeek, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.FirstDayOfWeek}");
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday)]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Tuesday)]
        [InlineData(DayOfWeek.Wednesday)]
        [InlineData(DayOfWeek.Thursday)]
        [InlineData(DayOfWeek.Friday)]
        [InlineData(DayOfWeek.Saturday)]
        public void FirstDayOfWeek_Set_GetReturnsExpected(DayOfWeek value)
        {
            var format = new DateTimeFormatInfo();
            format.FirstDayOfWeek = value;
            Assert.Equal(value, format.FirstDayOfWeek);
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday - 1)]
        [InlineData(DayOfWeek.Saturday + 1)]
        public void FirstDayOfWeek_SetInvalid_ThrowsArgumentOutOfRangeException(DayOfWeek value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => format.FirstDayOfWeek = value);
        }

        [Fact]
        public void FirstDayOfWeek_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.FirstDayOfWeek = DayOfWeek.Wednesday);
        }
    }
}
