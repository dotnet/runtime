// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TestLibrary;
using Xunit;

public static class UnresolvedPInvokeTests
{
    private static bool s_enteredMissingPInvokeMethod;
    private static bool s_initializedResolvedPInvokeType;
    private static bool s_initializedUnreachedPInvokeType;

    [Fact]
    public static void MissingPInvokeThrowsWhenCalled()
    {
        s_enteredMissingPInvokeMethod = false;

        Assert.Throws<DllNotFoundException>(CallMissingPInvoke);
        Assert.True(s_enteredMissingPInvokeMethod);
    }

    [Fact]
    public static void UnreachedMissingPInvokeDoesNotRunStaticConstructor()
    {
        CallMissingPInvokeIf(false);

        Assert.False(s_initializedUnreachedPInvokeType);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public static void ResolvedPInvokeRunsStaticConstructor()
    {
        ResolvedPInvokeWithStaticConstructor.Invoke();

        Assert.True(s_initializedResolvedPInvokeType);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallMissingPInvokeIf(bool shouldCall)
    {
        if (shouldCall)
        {
            MissingPInvokeWithStaticConstructor.Invoke();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallMissingPInvoke()
    {
        s_enteredMissingPInvokeMethod = true;
        MissingPInvoke();
    }

    [DllImport("UnresolvedPInvokeTests_MissingNativeLibrary")]
    private static extern void MissingPInvoke();

    private static class MissingPInvokeWithStaticConstructor
    {
        static MissingPInvokeWithStaticConstructor()
        {
            s_initializedUnreachedPInvokeType = true;
        }

        [DllImport("UnresolvedPInvokeTests_MissingNativeLibrary")]
        internal static extern void Invoke();
    }

    private static class ResolvedPInvokeWithStaticConstructor
    {
        static ResolvedPInvokeWithStaticConstructor()
        {
            s_initializedResolvedPInvokeType = true;
        }

        [DllImport("libSystem.Native", EntryPoint = "SystemNative_GetLowResolutionTimestamp")]
        internal static extern long Invoke();
    }
}
