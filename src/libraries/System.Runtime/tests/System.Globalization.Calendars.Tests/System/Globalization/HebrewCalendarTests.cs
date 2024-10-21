// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class HebrewCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new HebrewCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1583, 01, 01);

        public override DateTime MaxSupportedDateTime => new DateTime(2239, 09, 29, 23, 59, 59).AddTicks(9999999);

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunisolarCalendar;
    }
}
