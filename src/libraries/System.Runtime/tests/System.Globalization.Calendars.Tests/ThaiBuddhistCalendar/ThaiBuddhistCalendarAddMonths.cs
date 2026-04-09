// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarAddMonths
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { DateTime.MinValue, 1 };
            yield return new object[] { DateTime.MaxValue, -2 };
            yield return new object[] { DateTime.MinValue, 0 };
            yield return new object[] { DateTime.MaxValue, 0 };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55), 0 };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months)
        {
            Assert.Equal(time.AddMonths(months), new ThaiBuddhistCalendar().AddMonths(time, months));
        }
    }
}
