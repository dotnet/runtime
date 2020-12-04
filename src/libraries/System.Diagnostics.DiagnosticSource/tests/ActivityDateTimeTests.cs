// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivityDateTimeTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21594", TargetFrameworkMonikers.NetFramework)]
        public void StartStopReturnsPreciseDuration()
        {
            var activity = new Activity("test");

            var sw = Stopwatch.StartNew();

            activity.Start();
            SpinWait.SpinUntil(() => sw.ElapsedMilliseconds > 1, 2);
            activity.Stop();

            sw.Stop();

            Assert.True(activity.Duration.TotalMilliseconds > 1 && activity.Duration <= sw.Elapsed);
        }
    }
}
