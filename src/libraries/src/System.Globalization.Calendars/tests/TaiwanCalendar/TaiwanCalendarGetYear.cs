// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetYear
    {
        public static IEnumerable<object[]> GetYear_TestData()
        {
            yield return new object[] { new TaiwanCalendar().MinSupportedDateTime };
            yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime };
            yield return new object[] { TaiwanCalendarUtilities.RandomDateTime() };
        }

        [Theory]
        [MemberData(nameof(GetYear_TestData))]
        public void GetYear(DateTime time)
        {
            Assert.Equal(time.Year - 1911, new TaiwanCalendar().GetYear(time));
        }
    }
}
