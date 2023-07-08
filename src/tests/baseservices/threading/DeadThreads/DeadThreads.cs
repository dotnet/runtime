// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class DeadThreads
{
    /// <summary>
    /// A sanity test that exercises code paths relevant to the heuristic that triggers GCs based on dead thread count and time
    /// elapsed since a previous GC. See https://github.com/dotnet/coreclr/pull/10413.
    /// 
    /// This test suite runs with the following environment variables relevant to this test (see .csproj):
    ///     set DOTNET_Thread_DeadThreadCountThresholdForGCTrigger=8
    ///     set DOTNET_Thread_DeadThreadGCTriggerPeriodMilliseconds=3e8 // 1000
    /// </summary>
    private static void GCTriggerSanityTest()
    {
        var testDuration = TimeSpan.FromSeconds(8);
        var startTime = DateTime.UtcNow;
        do
        {
            StartNoOpThread();
            Thread.Sleep(1);
        } while (DateTime.UtcNow - startTime < testDuration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StartNoOpThread()
    {
        var t = new Thread(() => { });
        t.IsBackground = true;
        t.Start();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        GCTriggerSanityTest();
        return 100;
    }
}
