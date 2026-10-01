// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

public class Runtime_134894
{
    private static volatile bool s_started;
    private static volatile bool s_stop;
    private static int s_x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void F6() { s_x++; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void F5() { F6(); F6(); F6(); F6(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void F4() { F5(); F5(); F5(); F5(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void F3() { F4(); F4(); F4(); F4(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void F2() { F3(); F3(); F3(); F3(); }

    // Exhausts the inline budget, so StelemRef can't be inlined
    private static void F1() => F2();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(object[] arr, int i)
    {
        F1();
        s_started = true;
        while (!s_stop)
        {
            object o = null;
            arr[i] = o;
        }
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        var t = new Thread(() => Test(new string[10], 1)) { IsBackground = true };
        t.Start();
        while (!s_started) { }
        Thread.Sleep(100);

        // Hangs if the loop in Test has no GC safe point
        GC.Collect();
        s_stop = true;
        t.Join();
    }
}
