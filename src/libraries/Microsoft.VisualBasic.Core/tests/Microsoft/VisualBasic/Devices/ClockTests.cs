// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Devices.Tests
{
    public class ClockTests
    {
        [Fact]
        public void LocalTime()
        {
            var clock = new Clock();

            var before = clock.LocalTime;
            System.Threading.Thread.Sleep(10);

            var now = DateTime.Now;
            System.Threading.Thread.Sleep(10);

            var after = clock.LocalTime;

            Assert.True(before <= now);
            Assert.True(now <= after);
        }

        [Fact]
        public void GmtTime()
        {
            var clock = new Clock();

            var before = clock.GmtTime;
            System.Threading.Thread.Sleep(10);

            var now = DateTime.UtcNow;
            System.Threading.Thread.Sleep(10);

            var after = clock.GmtTime;

            Assert.True(before <= now);
            Assert.True(now <= after);
        }

        [Fact]
        public void TickCount()
        {
            var clock = new Clock();

            var before = clock.TickCount;
            System.Threading.Thread.Sleep(10);

            var after = clock.TickCount;
            Assert.True(before <=  after);
        }
    }
}
