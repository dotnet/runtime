// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using TestLibrary;
using Xunit;

public class CanonicalInterfaceReadyToRun
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmUp() { }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReadyToRunCompiled))]
    [SkipOnMono("ReadyToRun validation is specific to CoreCLR.")]
    [SkipOnCoreClr("Requires precompiled code to remain enabled.", RuntimeTestModes.AnyJitOptimizationStress)]
    public static void TestEntryPoint()
    {
        if (TestLibrary.CoreClrConfigurationDetection.IsDisableR2R)
            return;

        // Warm the measurement helpers before preparing the still-cold shared method.
        RuntimeHelpers.PrepareMethod(typeof(CanonicalInterfaceReadyToRun)
            .GetMethod(nameof(WarmUp), BindingFlags.NonPublic | BindingFlags.Static).MethodHandle);
        JitInfo.GetCompiledMethodCount(currentThread: true);

        // Shared agreement and hierarchy refinements must keep their ReadyToRun bodies.
        Check("ReadConstant", typeof(object));
        Check("ReadDefault", typeof(object));
        Check("ReadMostSpecific", typeof(object));
        Check("CheckGeneric", typeof(object), typeof(string), typeof(object));

        // Keep accepting the single-interface and exact-interface cases the runtime supports.
        Check("ReadContravariantDefault", typeof(object));
        Check("ReadVariant", typeof(int));
    }

    private static void Check(string methodName, params Type[] arguments)
    {
        RuntimeMethodHandle target = typeof(InheritedCanonicalInterface)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(arguments).MethodHandle;

        long before = JitInfo.GetCompiledMethodCount(currentThread: true);
        RuntimeHelpers.PrepareMethod(target);
        long after = JitInfo.GetCompiledMethodCount(currentThread: true);

        // A correct result alone would also pass after rejecting the ReadyToRun body.
        Assert.True(before == after, $"{methodName} was JIT-compiled instead of using its ReadyToRun body.");
    }
}
