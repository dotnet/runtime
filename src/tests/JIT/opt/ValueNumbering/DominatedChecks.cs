// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DominatedChecks
{
    private sealed class Holder
    {
        public object Value;
        public int First = 1, Second = 2;
    }

    private static int s_marker, s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe() { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void FinallyCheck(int[] values)
    {
        try
        {
            s_sink = values.Length;
        }
        finally
        {
            if (values.Length == values.Length)
                s_marker = 1;
            // Nested EH prevents finally cloning without a configuration switch.
            try { Observe(); }
            catch (InvalidOperationException) { s_marker = 2; }
        }
    }

    [Fact]
    public static void FailedTryDoesNotEstablishFinallyFact()
    {
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => FinallyCheck(null));
        Assert.Equal(0, s_marker);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NestedLoops(Holder value, int n, int m, bool enter)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            if (enter)
            {
                sum += value.First;
                for (int j = 0; j < m; j++)
                    sum += value.Second;
            }
            s_marker++;
        }
        return sum;
    }

    [Fact]
    public static void HoistingKeepsConditionalCheck()
    {
        s_marker = 0;
        Assert.Equal(0, NestedLoops(null, 2, 3, false));
        Assert.Equal(2, s_marker);
        Assert.Equal(14, NestedLoops(new Holder(), 2, 3, true));
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => NestedLoops(null, 2, 3, true));
        Assert.Equal(0, s_marker);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreNull(Holder target, object value)
    {
        if (value == null)
        {
            target.Value = value;
            s_marker = 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference StoreYoung(Holder target)
    {
        object value = new object();
        target.Value = value;
        return new WeakReference(value);
    }

    [Fact]
    public static void BarrierClassificationPreservesStores()
    {
        var target = new Holder { Value = new object() };
        StoreNull(target, null);
        Assert.Null(target.Value);
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => StoreNull(null, null));
        Assert.Equal(0, s_marker);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        WeakReference weak = StoreYoung(target);
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
        Assert.True(weak.IsAlive);
        Assert.Same(weak.Target, target.Value);
        GC.KeepAlive(target);
    }
}
