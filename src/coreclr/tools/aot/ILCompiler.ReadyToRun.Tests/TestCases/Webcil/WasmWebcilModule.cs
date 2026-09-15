// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Webcil;

public static class WasmWebcilModule
{
    private static readonly int[] s_primes = new int[] { 3, 5, 7, 11, 13 };
    private static int s_counter;

    private sealed class GcMarker
    {
        public readonly int Value;

        public GcMarker(int value)
        {
            Value = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AddIntegers(int left, int right)
    {
        return left + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int OptimizedTrackedVariables(int left, int right)
    {
        int sum = left + right;
        if (sum < 0)
        {
            return AddIntegers(sum, right);
        }

        int difference = left - right;
        return AddIntegers(sum, difference);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LeafFrameLocal(int value)
    {
        int local = value + 1;
        int* address = &local;
        return *address * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LocallocFrameLocal(int value)
    {
        int length = (value & 3) + 1;
        int* storage = stackalloc int[length];
        *storage = value;
        return *storage;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static double AddDoubles(double left, double right)
    {
        double result = left + right;
        return result;
    }

    // Reads static data, which forces the JIT to materialize the imageBase address via a
    // 'global.get' of the wasm imageBase well-known global.
    public static int SumStaticData(int index)
    {
        s_counter += 1;
        return AddIntegers(s_primes[index], s_counter);
    }

    // An unoptimized try/finally makes the JIT emit a call to the 'finally'
    // funclet (genCallFinally), which computes the funclet's address from the
    // wasm table-base well-known global via a 'global.get'. Like the image
    // base, that table-base global is referenced through a
    // WASM_GLOBAL_INDEX_LEB relocation the R2R object writer must self-resolve
    // back to the fixed table-base global index; if that resolution regresses,
    // the emitted 'global.get' encoding changes (or crossgen2 throws while
    // emitting this method).
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int SumWithFinally(int index)
    {
        int total = 0;
        try
        {
            total = s_primes[index];
        }
        finally
        {
            s_counter++;
        }
        return total + s_counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int GcLocalAcrossFinally(int value)
    {
        GcMarker marker = new(value);
        try
        {
            return marker.Value;
        }
        finally
        {
            // Keep the newly allocated object live in the parent frame across an actual GC
            // reached through the finally funclet.
            CollectAtGcSafepoint();
            GC.KeepAlive(marker);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CollectAtGcSafepoint()
    {
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LocallocAcrossFinally(int value)
    {
        int length = (value & 3) + 1;
        int* storage = stackalloc int[length];
        *storage = value;
        try
        {
            return *storage;
        }
        finally
        {
            CollectAtGcSafepoint();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int GcSlotIdentity()
    {
        GcMarker first = new(17);
        if (first.Value == 0)
        {
            return 0;
        }

        GcMarker second = new(29);
        if (second.Value == 0)
        {
            return 0;
        }

        GcMarker? empty = null;
        TouchMarkerSlot(ref empty);

        CollectAtGcSafepoint();
        int result = (first.Value * 100) + second.Value;
        GC.KeepAlive(first);
        GC.KeepAlive(second);
        GC.KeepAlive(empty);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TouchMarkerSlot(ref GcMarker? marker)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CatchException(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }
}
