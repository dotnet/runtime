// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

/// <summary>Checks multi-register local definitions across runtime-async suspensions.</summary>
public class AsyncStoreLclVars
{
    [StructLayout(LayoutKind.Explicit)]
    private struct Pair
    {
        [FieldOffset(0)] public long First;
        [FieldOffset(8)] public long Second;
        [FieldOffset(0)] public (long, long) Overlap;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Pair MakePair(long value) => new Pair { First = value, Second = value + 1 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Pair> MakePairAsync(long value)
    {
        await Task.Yield();
        return MakePair(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task CheckLocalDefinitions()
    {
        Pair pair = MakePair(3);
        await Task.Yield();
        Assert.Equal(62, pair.First * 17 + pair.Second + (pair.First ^ pair.Second));

        // Each resumption changes both fields before suspending again. Continuation
        // reuse must save the new definitions rather than keeping the previous values.
        for (int i = 4; i < 8; i++)
        {
            pair = MakePair(i);
            await Task.Yield();
            Assert.Equal(i * 17 + i + 1 + (i ^ (i + 1)),
                         pair.First * 17 + pair.Second + (pair.First ^ pair.Second));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task CheckAsyncCall()
    {
        Pair pair = await MakePairAsync(3);
        Assert.Equal(62, pair.First * 17 + pair.Second + (pair.First ^ pair.Second));
        await Task.Yield();
        Assert.Equal(62, pair.First * 17 + pair.Second + (pair.First ^ pair.Second));
    }

    /// <summary>Checks newly allocated and reused continuations and async struct returns.</summary>
    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        CheckAsyncCall().GetAwaiter().GetResult();
        CheckLocalDefinitions().GetAwaiter().GetResult();
    }
}
