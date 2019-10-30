// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarToDateTime
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> ToDateTime_TestData()
        {
            yield return new object[] { 1, 1, 1, 0, 0, 0, 0 };
            yield return new object[] { 9999, 12, 31, 23, 59, 59, 999 };
            yield return new object[] { 2004, 2, 29, 1, 1, 1, 0 };
            yield return new object[] { 1600, 1, 1, 0, 0, 0, 0 };

            // Random
            DateTime randomTime = s_randomDataGenerator.GetDateTime(-55);
            yield return new object[] { randomTime.Year, randomTime.Month, randomTime.Day, randomTime.Hour, randomTime.Minute, randomTime.Second, randomTime.Millisecond };
        }

        [Theory]
        [MemberData(nameof(ToDateTime_TestData))]
        public void ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            KoreanCalendar calendar = new KoreanCalendar();
            DateTime expected = new GregorianCalendar().ToDateTime(year, month, day, hour, minute, second, millisecond);
            Assert.Equal(expected, calendar.ToDateTime(year + 2333, month, day, hour, minute, second, millisecond));
            Assert.Equal(expected, calendar.ToDateTime(year + 2333, month, day, hour, minute, second, millisecond, 0));
            Assert.Equal(expected, calendar.ToDateTime(year + 2333, month, day, hour, minute, second, millisecond, 1));
        }
    }
}
