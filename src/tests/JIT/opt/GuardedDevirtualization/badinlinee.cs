// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class BadInlinee
{
    private class Base
    {
        public virtual int M() => 42;
    }

    private sealed class Derived : Base
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int M() => 43;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Poison(Base value) => value.M();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test()
    {
        Base value = new Base();
        return value.M();
    }

    // Calls the method until the JIT has been idle for a while, i.e. until tiered compilation has
    // finished promoting the method (and everything else that is hot) to its final tier.
    private static void CallUntilJitIsIdle(Func<int> method)
    {
        long compiledMethodCount = JitInfo.GetCompiledMethodCount();
        int idleIterations = 0;

        for (int i = 0; (i < 2_000) && (idleIterations < 20); i++)
        {
            for (int j = 0; j < 100; j++)
            {
                method();
            }

            Thread.Sleep(15);

            long newCompiledMethodCount = JitInfo.GetCompiledMethodCount();
            idleIterations = (newCompiledMethodCount == compiledMethodCount) ? (idleIterations + 1) : 0;
            compiledMethodCount = newCompiledMethodCount;
        }
    }

    [SkipOnCoreClr("JitStress randomly switches methods such as Test to MinOpts, which keeps the allocation.", RuntimeTestModes.JitStress)]
    [Fact]
    public static void TestEntryPoint()
    {
        Base value = new Derived();

        // Tier up Poison first. Guarded devirtualization picks Derived.M, which can't be inlined
        // because it is synchronized, and that must not stop Base.M from being inlined elsewhere.
        CallUntilJitIsIdle(() => Poison(value));
        CallUntilJitIsIdle(Test);

        int result = 0;
        long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 1_000; i++)
        {
            result += Test();
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

        Assert.Equal(0, allocatedBytes);
        Assert.Equal(42_000, result);
    }
}
