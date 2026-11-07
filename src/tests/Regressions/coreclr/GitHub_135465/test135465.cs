// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

public class Runtime_135465
{
    private static volatile bool s_loopStarted;

    // An empty infinite loop is a branch to itself
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpinInBranchToSelf()
    {
        s_loopStarted = true;
        while (true) { }
    }

    // The loop is closed by the leave at the end of the try block
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpinInBackwardLeave()
    {
        s_loopStarted = true;
        int i = 0;
        while (true)
        {
            try
            {
                i++;
            }
            catch (ArgumentException)
            {
                i--;
            }
        }
    }

    // The same, with a finally to call on the way back to the head of the loop
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpinInBackwardLeaveThroughFinally()
    {
        s_loopStarted = true;
        int i = 0;
        while (true)
        {
            try
            {
                i++;
            }
            finally
            {
                i--;
            }
        }
    }

    // The loop is closed by the switch: the C# compiler makes the switch target of an empty `goto` case the label itself
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SpinInBackwardSwitch(int selector)
    {
        s_loopStarted = true;
    Loop:
        switch (selector)
        {
            case 0: goto Loop;
            case 1: return 1;
            case 2: return 2;
            case 3: return 3;
            case 4: return 4;
            default: return -1;
        }
    }

    // Runs a loop that never exits and calls nothing on another thread, then lets a GC suspend that thread
    private static void CollectWhileSpinning(string name, ThreadStart loop)
    {
        Console.WriteLine(name);
        s_loopStarted = false;
        // The loop never exits, so its thread has to be one that ends with the process
        Thread thread = new Thread(loop) { IsBackground = true };
        thread.Start();
        while (!s_loopStarted)
            Thread.Sleep(1);

        // This does not return if the loop has no safepoint
        GC.Collect();
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        CollectWhileSpinning(nameof(SpinInBranchToSelf), SpinInBranchToSelf);
        CollectWhileSpinning(nameof(SpinInBackwardLeave), SpinInBackwardLeave);
        CollectWhileSpinning(nameof(SpinInBackwardLeaveThroughFinally), SpinInBackwardLeaveThroughFinally);
        CollectWhileSpinning(nameof(SpinInBackwardSwitch), () => SpinInBackwardSwitch(0));
    }
}
