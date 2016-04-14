// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarMinSupportedDateTime
    {
        [Fact]
        public void MinSupportedDateTime()
        {
            Assert.Equal(new DateTime(1912, 1, 1), new TaiwanCalendar().MinSupportedDateTime);
        }
    }
}