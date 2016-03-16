// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarEras
    {
        [Fact]
        public void Eras()
        {
            Assert.Equal(new int[] { 1 }, new ThaiBuddhistCalendar().Eras);
        }
    }
}
