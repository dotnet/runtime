// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanLunisolarCalendarTests : EastAsianLunisolarCalendarTestBase
    {
        public override Calendar Calendar => new KoreanLunisolarCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(0918, 02, 19);

        public override DateTime MaxSupportedDateTime => new DateTime(2051, 02, 10, 23, 59, 59).AddTicks(9999999);
    }
}
