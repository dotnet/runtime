// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseLunisolarTests : EastAsianLunisolarCalendarTestBase
    {
        public override Calendar Calendar => new JapaneseLunisolarCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1960, 01, 28);

        public override DateTime MaxSupportedDateTime => new DateTime(2050, 01, 22, 23, 59, 59).AddTicks(9999999);

        public override bool SkipErasTest => true;

        [Fact]
        public void Eras_Get_ReturnsAtLeastTwo()
        {
            int[] eras = Calendar.Eras;
            Assert.True(eras.Length >= 2);
        }
    }
}
