// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarGetMonthsInYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetMonthsInYear_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 9999, 1 };
            yield return new object[] { 2004, 1 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 9999, 1 };
        }

        [Theory]
        [MemberData(nameof(GetMonthsInYear_TestData))]
        public void GetMonthsInYear(int year, int era)
        {
            int expected = new GregorianCalendar().GetMonthsInYear(year, era);
            Assert.Equal(expected, new KoreanCalendar().GetMonthsInYear(year + 2333, era));
        }
    }
}
