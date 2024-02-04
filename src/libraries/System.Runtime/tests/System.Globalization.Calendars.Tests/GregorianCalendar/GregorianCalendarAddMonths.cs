// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarAddMonths
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        private const int MaxMonths = 120000;

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { DateTime.Now, 0 };
            yield return new object[] { DateTime.MinValue, s_randomDataGenerator.GetInt32(-55) % MaxMonths + 1 };
            yield return new object[] { DateTime.MaxValue, -1 * s_randomDataGenerator.GetInt32(-55) % MaxMonths - 1 };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55).AddMonths(1), -1 };
            yield return new object[] { new DateTime(2000, 2, 29, 10, 30, 24), 13 }; // February in a leap year
            yield return new object[] { new DateTime(1996, 2, 29, 10, 30, 24), 48 }; // February in a leap year
            yield return new object[] { new DateTime(1996, 3, 29, 10, 30, 24), 48 }; // Other month in a leap year
            yield return new object[] { new DateTime(1999, 2, 28, 10, 30, 24), 48 }; // February in a common year
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months)
        {
            Assert.Equal(time.AddMonths(months), new GregorianCalendar().AddMonths(time, months));
        }
    }
}
