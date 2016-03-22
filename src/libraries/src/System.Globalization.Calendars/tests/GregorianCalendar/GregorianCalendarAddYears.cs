// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarAddYears
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { DateTime.Now, 0 };
            yield return new object[] { DateTime.MinValue, 100 };
            yield return new object[] { DateTime.MaxValue, -99 };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55), 1 };

            // February in a leap year
            yield return new object[] { new DateTime(2000, 2, 29, 10, 30, 24), 13 };
            yield return new object[] { new DateTime(1996, 2, 29, 10, 30, 24), 4 };

            // Month other than February in a leap year
            yield return new object[] { new DateTime(1996, 3, 29, 10, 30, 24), 48 };

            // February in a common year
            yield return new object[] { new DateTime(1999, 2, 28, 10, 30, 24), 48 };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears(DateTime time, int years)
        {
            Assert.Equal(time.AddYears(years), new GregorianCalendar().AddYears(time, years));
        }
    }
}
