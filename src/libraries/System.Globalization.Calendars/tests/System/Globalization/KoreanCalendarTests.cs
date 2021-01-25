// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new KoreanCalendar();
    }
}
