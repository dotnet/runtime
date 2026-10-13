// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

public class Runtime_134477
{
    public static volatile byte s_state;
    private static int s_data;

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        Thread t = new Thread(Test) { IsBackground = true };
        t.Start();
        Thread.Sleep(100);
        s_data = 1;
        Assert.True(t.Join(30000));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test()
    {
        while (s_state >= 0)
        {
            if (s_data != 0)
                break;
        }
    }
}
