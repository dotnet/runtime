// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarGetYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetYear_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55) };
        }

        [Theory]
        [MemberData(nameof(GetYear_TestData))]
        public void GetYear(DateTime time)
        {
            Assert.Equal(time.Year + 543, new ThaiBuddhistCalendar().GetYear(time));
        }
    }
}
