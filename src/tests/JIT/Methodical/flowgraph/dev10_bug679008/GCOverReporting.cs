// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*
 * If using a value type/struct that contains only a single reference type field, under certain situations the x64 JIT reports the stack location as a live GC pointer before zero-initializing it.
 * The only workaround would be to disable optimizations via MethodImplOptions.NoOptimization.  This GC hole is sort of an existing one and sort of a regression.
 * In one sense, it has been in the JIT since we turned on candidates and made worse with OPT_WITH_EH (since it adds a lot of cases where we try and then undo worthless register candidates), which was done between beta2 and RTM of Whidbey/2.0.
 * Previously bugs in this area caused the stack's lifetime to not get reported at all.
 * Earlier this year 2 bugs were fixed, and recently I ported those fixes to arrowhead, so that we now correctly report the untracked stack lifetime of these value types.
 * This bug is a manifestation of the opposite problem where the reference pointer is reported, but not initialized.
 * Thus depending upon the previous stack contents, can cause an inverse-GCHole (reporting of a non-GC pointer as a GC pointer).
 * This is not as serious because it is *not* the normal GC hole that leads to type system hole that leads to security exploit.
 * The worst this could cause is an AV in the runtime which would trigger an ExecutionException which I believe cannot be caught, so it would just tear down the process and hopefully invoke Watson.
 *
 * Expected output:
 * 2
 * 3
 * 5
 * 6
 * 8
 * 9
 *
 * Actual output:
 * 2
 * 3
 * and then a crash!
 * 
 */

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_GCOverReporting_cs
{
public struct MB8
{
    public object foo;
}

public class Repro
{
    private static int s_counter;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MB8 MakeNewMB8()
    {
        GC.Collect();
        MB8 mb;
        mb.foo = ++s_counter;
        return mb;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MB8 MakeAndUseMB8(MB8 mb)
    {
        mb.foo = ++s_counter;
        return mb;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Method()
    {
        MB8 mb = MakeNewMB8();
        for (int i = 0; i < 5; i += 3)
        {
            try
            {
                mb = MakeAndUseMB8(mb);
            }
            finally
            {
                Console.WriteLine(s_counter);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void TrashStack()
    {
        ulong* stack = stackalloc ulong[256];
        for (int i = 0; i < 256; i++)
            stack[i] = 0xCCCCCCCCCCCCCCCCL;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ConsumeStack(int i)
    {
        if (i > 0)
            ConsumeStack(i - 1);
        else
            Method();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void DoTest()
    {
        TrashStack();
        ConsumeStack(3);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Method();
        TrashStack();
        ConsumeStack(0);
        DoTest();
        return 100;
    }
}
}
