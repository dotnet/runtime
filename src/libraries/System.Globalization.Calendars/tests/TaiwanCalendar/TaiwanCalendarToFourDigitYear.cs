// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarToFourDigitYear
    {
        public static IEnumerable<object[]> ToFourDigitYear_TestData()
        {
            yield return new object[] { new Random(-55).Next(1, 8088) };
            yield return new object[] { 1 };
            yield return new object[] { 8088 };
        }

        [Theory]
        [MemberData(nameof(ToFourDigitYear_TestData))]
        public void ToFourDigitYear(int year)
        {
            Assert.Equal(year, new TaiwanCalendar().ToFourDigitYear(year));
        }
    }
}
