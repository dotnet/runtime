// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarAddMonths
    {
        public static IEnumerable<object[]> AddMonths_TestData()
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            Random random = new Random(-55);

            DateTime randomDateTime = TaiwanCalendarUtilities.RandomDateTime();
            if ((calendar.MaxSupportedDateTime.Year - randomDateTime.Year - 1911) > 1000)
            {
                yield return new object[] { randomDateTime, random.Next(1, 1000 * 12) };
            }
            else
            {
                yield return new object[] { randomDateTime, random.Next(1, (calendar.MaxSupportedDateTime.Year - randomDateTime.Year - 1911) * 12) };
            }

            if ((calendar.MinSupportedDateTime.Year - randomDateTime.Year) < -1000)
            {
                yield return new object[] { randomDateTime, random.Next(-1000 * 12, 0) };
            }
            else
            {
                yield return new object[] { randomDateTime, random.Next((calendar.MinSupportedDateTime.Year - randomDateTime.Year) * 12, 0) };
            }

            yield return new object[] { calendar.MaxSupportedDateTime, 0 };
            yield return new object[] { calendar.MinSupportedDateTime, 0 };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months)
        {
            Assert.Equal(time.AddMonths(months), new TaiwanCalendar().AddMonths(time, months));
        }
    }
}
