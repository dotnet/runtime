// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class UmAlQuraCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new UmAlQuraCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1900, 04, 30);

        public override DateTime MaxSupportedDateTime => new DateTime(2077, 11, 16, 23, 59, 59).AddTicks(9999999);

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;
    }
}
