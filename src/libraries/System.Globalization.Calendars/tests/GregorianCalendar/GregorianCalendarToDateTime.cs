// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarToDateTime
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static int RandomEra() => s_randomDataGenerator.GetInt32(-55) & 1;

        public static IEnumerable<object[]> ToDateTime_Valid_TestData()
        {
            // Random
            int randomYear = RandomYear();
            int randomMonth = RandomMonth();
            int randomDay = RandomDay(randomYear, randomMonth);
            int randomHour = s_randomDataGenerator.GetInt32(-55) % 24; // 0-23
            int randomMinute = s_randomDataGenerator.GetInt32(-55) % 60; // 0-59
            int randomSecond = s_randomDataGenerator.GetInt32(-55) % 60; // 0-59
            int randomMillisecond = s_randomDataGenerator.GetInt32(-55) % 1000; // 0-999
            yield return new object[] { randomYear, randomMonth, randomDay, randomHour, randomMinute, randomSecond, randomMillisecond, RandomEra() };

            // Minimum supported DateTime
            yield return new object[] { 1, 1, 1, 0, 0, 0, 0, RandomEra() };

            // Maximum supported DateTime
            yield return new object[] { 9999, 12, 31, 23, 59, 59, 999, RandomEra() };
        }
        
        [Theory]
        [MemberData(nameof(ToDateTime_Valid_TestData))]
        public void ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            DateTime expected = new DateTime(year, month, day, hour, minute, second, millisecond);
            DateTime actual = new GregorianCalendar().ToDateTime(year, month, day, hour, minute, second, millisecond, era);
            Assert.Equal(expected, actual);
        }
    }
}
