// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarAddYears
    {
        public static IEnumerable<object[]> AddYears_TestData()
        {
            Random rand = new Random(-55);
            TaiwanCalendar tc = new TaiwanCalendar();

            DateTime randomDateTime = TaiwanCalendarUtilities.RandomDateTime();
            yield return new object[] { randomDateTime, rand.Next(1, (tc.MaxSupportedDateTime.Year - randomDateTime.Year - 1911)) };

            yield return new object[] { randomDateTime, rand.Next((tc.MinSupportedDateTime.Year - randomDateTime.Year), 0) };

            yield return new object[] { tc.MinSupportedDateTime, 0 };
            yield return new object[] { tc.MaxSupportedDateTime, 0 };
            yield return new object[] { randomDateTime, 0 };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears(DateTime time, int years)
        {
            Assert.Equal(time.AddYears(years), new TaiwanCalendar().AddYears(time, years));
        }
    }
}
