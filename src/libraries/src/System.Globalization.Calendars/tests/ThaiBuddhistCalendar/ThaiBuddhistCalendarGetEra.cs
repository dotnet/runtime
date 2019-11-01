// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarGetEra
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetEra_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { new DateTime(2000, 2, 29) };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55) };
        }

        [Theory]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra(DateTime time)
        {
            Assert.Equal(1, new ThaiBuddhistCalendar().GetEra(time));
        }
    }
}
