// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_134067
{
    private static int s_loopValue;

    [SkipOnCoreClr("JitStress can force Test to MinOpts, bypassing the optimized code path.", RuntimeTestModes.JitStress)]
    [Fact]
    public static void TestEntryPoint()
    {
        bool stopWorkers = false;
        Thread gcThread = new(() =>
        {
            while (!Volatile.Read(ref stopWorkers))
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            }
        })
        {
            IsBackground = true
        };
        Thread allocationThread = new(() =>
        {
            while (!Volatile.Read(ref stopWorkers))
            {
                GC.KeepAlive(new byte[Random.Shared.Next(1_000, 20_000)]);
            }
        })
        {
            IsBackground = true
        };

        try
        {
            gcThread.Start();
            allocationThread.Start();

            Stopwatch duration = Stopwatch.StartNew();
            while (duration.Elapsed < TimeSpan.FromSeconds(5))
            {
                C c = new();
                Assert.True(Test(c, 20_000), "A block initialization wrote through a stale object address.");

                SmallC small = new();
                Assert.True(TestSmall(small, 20_000), "A block initialization wrote through a stale object address.");

                ByrefOwner owner = new();
                Assert.True(TestByref(ref owner.Value, 20_000), "A block initialization wrote through a stale byref.");
                Assert.Equal(0, owner.Value.T.H);
            }
        }
        finally
        {
            Volatile.Write(ref stopWorkers, true);
            if (gcThread.IsAlive)
            {
                gcThread.Join();
            }
            if (allocationThread.IsAlive)
            {
                allocationThread.Join();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Test(C c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Volatile.Write(ref c.T.H, 1);
            c.T = default;
            if (Volatile.Read(ref c.T.H) != 0)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitSmall(SmallC c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Volatile.Write(ref s_loopValue, i);
        }

        c.T = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestSmall(SmallC c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Volatile.Write(ref c.T.H, 1);
            InitSmall(c, 1);
            if (Volatile.Read(ref c.T.H) != 0)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool TestByref(ref PaddedPayload value, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Volatile.Write(ref value.T.H, 1);
            value.T = default;
            if (Volatile.Read(ref value.T.H) != 0)
            {
                return false;
            }
        }

        Volatile.Write(ref value.T.H, 1);
        value.T = default;
        return true;
    }

    [InlineArray(40_000)]
    private struct Pad
    {
        private byte _element0;
    }

    [InlineArray(1_024)]
    private struct SmallPad
    {
        private byte _element0;
    }

    private struct Payload
    {
        public long A, B, C, D, E, F, G, H;
    }

    private struct PaddedPayload
    {
        public Pad P;
        public Payload T;
    }

    private sealed class C
    {
        public Pad P;
        public Payload T;
    }

    private sealed class SmallC
    {
        public SmallPad P;
        public Payload T;
    }

    private sealed class ByrefOwner
    {
        public PaddedPayload Value;
    }
}
