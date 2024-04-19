// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarAddYears
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { DateTime.MinValue, s_randomDataGenerator.GetInt16(-55) % 9999 };
            yield return new object[] { DateTime.MaxValue, -(s_randomDataGenerator.GetInt16(-55) % 9999) };

            yield return new object[] { DateTime.MinValue, 0 };
            yield return new object[] { DateTime.MaxValue, 0 };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55), 0 };

            yield return new object[] { new DateTime(2000, 1, 1), s_randomDataGenerator.GetInt16(-55) % 7999 };
            yield return new object[] { new DateTime(2000, 1, 1), -(s_randomDataGenerator.GetInt16(-55) % 2000) };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears(DateTime time, int years)
        {
            Assert.Equal(time.AddYears(years), new ThaiBuddhistCalendar().AddYears(time, years));
        }
    }
}
