// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetEra
    {
        public static IEnumerable<object[]> GetEra_TestData()
        {
            yield return new object[] { new TaiwanCalendar().MinSupportedDateTime };
            yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime };
            yield return new object[] { TaiwanCalendarUtilities.RandomDateTime() };
        }

        [Theory]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra(DateTime time)
        {
            Assert.Equal(1, new TaiwanCalendar().GetEra(time));
        }
    }
}
