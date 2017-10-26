// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Management.Tests
{
    public class ManagementDateTimeConverterTests
    {
        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        public void DateTime_RoundTrip()
        {
            var date = new DateTime(2002, 4, 8, 14, 18, 35, 978, DateTimeKind.Utc);
            var dmtfDate = "20020408141835.978000+000";

            DateTime convertedDate = ManagementDateTimeConverter.ToDateTime(dmtfDate).ToUniversalTime();
            Assert.Equal(date, convertedDate);

            // Converting System.DateTime to DMTF datetime
            string convertedDmtfDate = ManagementDateTimeConverter.ToDmtfDateTime(date);
            Assert.Equal(dmtfDate, convertedDmtfDate);
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        public void TimeSpan_RoundTrip()
        {
            var timeSpan = new TimeSpan(10, 12, 25, 32, 123);
            var dmtfTimeInterval = "00000010122532.123000:000";

            // Converting DMTF timeinterval to System.TimeSpan
            TimeSpan convertedTimeSpan = ManagementDateTimeConverter.ToTimeSpan(dmtfTimeInterval);
            Assert.Equal(timeSpan, convertedTimeSpan);

            // Converting System.TimeSpan to DMTF time interval format
            string convertedDmtfTimeInterval = ManagementDateTimeConverter.ToDmtfTimeInterval(timeSpan);
            Assert.Equal(dmtfTimeInterval, convertedDmtfTimeInterval);
        }
    }
}
