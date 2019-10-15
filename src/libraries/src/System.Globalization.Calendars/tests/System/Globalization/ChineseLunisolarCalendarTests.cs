// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class ChineseLunisolarCalendarTests : EastAsianLunisolarCalendarTestBase
    {
        public override Calendar Calendar => new ChineseLunisolarCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1901, 02, 19);

        public override DateTime MaxSupportedDateTime => new DateTime(2101, 01, 28, 23, 59, 59).AddTicks(9999999);
    }
}
