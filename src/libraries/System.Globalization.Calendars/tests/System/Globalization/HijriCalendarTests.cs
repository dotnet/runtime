// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class HijriCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new HijriCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(0622, 07, 18);

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;
    }
}
