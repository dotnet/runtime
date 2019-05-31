// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace System.Tests
{
    public class EnvironmentTickCount
    {
        [Fact]
        public void TickCountTest()
        {
            int start = Environment.TickCount;
            var times = new HashSet<int>();
            Func<bool> test = () =>
            {
                int time = Environment.TickCount;
                times.Add(time);
                return time - start > 0;
            };
            Assert.True(
                SpinWait.SpinUntil(test, TimeSpan.FromSeconds(1)) || test(),
                $"TickCount did not increase after one second. start: {start}, values tested: {string.Join(", ", times.ToArray())}.");
        }

        [Fact]
        public void TickCount64Test()
        {
            long start = Environment.TickCount64;
            var times = new HashSet<long>();
            Func<bool> test = () =>
            {
                long time = Environment.TickCount64;
                times.Add(time);
                return time - start > 0;
            };
            Assert.True(
                SpinWait.SpinUntil(test, TimeSpan.FromSeconds(1)) || test(),
                $"TickCount did not increase after one second. start: {start}, values tested: {string.Join(", ", times.ToArray())}.");
        }
    }
}
