// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_133588
{
    private struct Buf
    {
        public int E0, E1, E2, E3, E4, E5, E6, E7;
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        Assert.Equal(2, Test(1).GetAwaiter().GetResult());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Sink(ref int x) => x += 1;

    // The address of 'buf' computed via Unsafe.As + Unsafe.Add is native-int typed.
    // It must not be reused after resumption, since locals live in a new frame then.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static async Task<int> Test(int index)
    {
        Buf buf = default;
        Sink(ref Unsafe.Add(ref Unsafe.As<Buf, int>(ref buf), index));
        await Task.Yield();
        Sink(ref Unsafe.Add(ref Unsafe.As<Buf, int>(ref buf), index));
        return buf.E0 + buf.E1 + buf.E2 + buf.E3 + buf.E4 + buf.E5 + buf.E6 + buf.E7;
    }
}
