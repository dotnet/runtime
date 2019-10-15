// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarGetDayOfMonth
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetDayOfMonth_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            for (int i = 1; i <= 29; i++)
            {
                yield return new object[] { new DateTime(2008, 2, i) };
            }
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55) };
        }

        [Theory]
        [MemberData(nameof(GetDayOfMonth_TestData))]
        public void GetDayOfMonth(DateTime time)
        {
            Assert.Equal(time.Day, new ThaiBuddhistCalendar().GetDayOfMonth(time));
        }
    }
}
