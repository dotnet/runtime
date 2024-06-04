// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new JapaneseCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1868, 09, 08);

        public override bool SkipErasTest => true;

        [Fact]
        public void Eras_Get_ReturnsAtLeastFour()
        {
            int[] eras = Calendar.Eras;
            Assert.True(eras.Length >= 4);

            // eras should be [ noOfEras, noOfEras - 1, ..., 1 ]
            Assert.Equal(eras.Length, eras[0]);
            for (int i = 0; i < eras.Length; i++)
            {
                Assert.Equal(eras.Length - i, eras[i]);
            }
        }
    }
}
