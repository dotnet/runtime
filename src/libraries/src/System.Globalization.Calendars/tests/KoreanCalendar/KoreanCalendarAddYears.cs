// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarAddYears
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { DateTime.MinValue, s_randomDataGenerator.GetInt16(-55) % 9999 };
            yield return new object[] { DateTime.MaxValue, -(s_randomDataGenerator.GetInt16(-55) % 9999) };

            yield return new object[] { new DateTime(2000, 1, 1), s_randomDataGenerator.GetInt16(-55) % 7999 };
            yield return new object[] { new DateTime(2000, 1, 1), -(s_randomDataGenerator.GetInt16(-55) % 2000) };

            // Leap year
            yield return new object[] { new DateTime(2004, 2, 9), 2 };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears(DateTime time, int years)
        {
            Assert.Equal(time.AddYears(years), new KoreanCalendar().AddYears(time, years));
        }
    }
}
