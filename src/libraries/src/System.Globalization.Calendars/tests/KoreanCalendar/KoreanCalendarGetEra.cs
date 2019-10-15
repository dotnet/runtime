// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarGetEra
    {
        public static IEnumerable<object[]> GetEra_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { new DateTime(2004, 2, 29) };
        }

        [Theory]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra(DateTime time)
        {
            Assert.Equal(1, new KoreanCalendar().GetEra(time));
        }
    }
}
