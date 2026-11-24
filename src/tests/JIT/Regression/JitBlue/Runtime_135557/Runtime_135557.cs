// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_135557
{
    private class Base
    {
        public virtual int M() => 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test()
    {
        Base value = new Base();
        return value.M();
    }

    // Calls Test until the JIT has been idle for a while, i.e. until tiered compilation has
    // finished promoting Test (and everything else that is hot) to its final tier.
    private static void CallUntilJitIsIdle()
    {
        long compiledMethodCount = JitInfo.GetCompiledMethodCount();
        int idleIterations = 0;

        for (int i = 0; (i < 2_000) && (idleIterations < 20); i++)
        {
            for (int j = 0; j < 100; j++)
            {
                Test();
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
        // The final tier of Test is optimized and, because of DOTNET_JitInstrumentIfOptimizing in the
        // project file, also instrumented. The JIT knows the exact type of the object, so it has to
        // devirtualize and inline M, instead of probing the call, for the allocation to go away.
        CallUntilJitIsIdle();

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
