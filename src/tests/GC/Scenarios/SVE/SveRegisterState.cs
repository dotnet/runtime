// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using Xunit;

// This test exercises SVE intrinsics in a hot loop, with code that encourages
// lots of values to be allocated to the predicate register file. It also
// force triggers GC to encourage suspension, ensuring predicate values are
// saved and restored correctly.

public class SveRegisterState
{
    private const int IterationCount = 1_000_000;

    private static bool s_collectorReady;
    private static bool s_exercisingRegisters;
    private static bool s_stopCollecting;
    private static int s_collectionCount;

    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine($"Vector<int>.Count: {Vector<int>.Count}");
        if (!Sve.IsSupported)
        {
            return;
        }

        Volatile.Write(ref s_collectorReady, false);
        Volatile.Write(ref s_exercisingRegisters, false);
        Volatile.Write(ref s_collectionCount, 0);
        Volatile.Write(ref s_stopCollecting, false);
        Thread collector = new Thread(CollectUntilStopped);
        collector.Start();
        SpinWait.SpinUntil(() => Volatile.Read(ref s_collectorReady));

        Vector<int> result;
        int iterations;
        try
        {
            result = ExerciseRegisters(out iterations);
        }
        finally
        {
            Volatile.Write(ref s_stopCollecting, true);
            collector.Join();
        }

        Assert.True(Volatile.Read(ref s_collectionCount) > 0);
        Console.WriteLine($"Iterations: {iterations}; collections: {Volatile.Read(ref s_collectionCount)}");

        int[] resultValues = new int[Vector<int>.Count];
        result.CopyTo(resultValues);
        for (int i = 0; i < resultValues.Length; i++)
        {
            int expectedDelta = i switch
            {
                0 => 31,
                1 => 251,
                2 => -77,
                _ => -221,
            };
            Assert.Equal(iterations * expectedDelta, resultValues[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<int> ExerciseRegisters(out int iterations)
    {
        Vector<int> mask0 = Sve.CreateTrueMaskInt32();
        Vector<int> mask1 = Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount1);
        Vector<int> mask2 = Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount2);
        Vector<int> mask3 = Sve.CreateTrueMaskInt32(SveMaskPattern.VectorCount3);
        Vector<int> mask4 = Sve.BitwiseClear(mask0, mask1);
        Vector<int> mask5 = Sve.Xor(mask2, mask1);
        Vector<int> mask6 = Sve.And(mask3, mask4);
        Vector<int> mask7 = Sve.Or(mask1, mask5);
        Vector<int> result = Vector<int>.Zero;

        Volatile.Write(ref s_exercisingRegisters, true);
        iterations = 0;
        do
        {
            result = Sve.ConditionalSelect(mask0, result + Vector.Create<int>(1), result - Vector.Create<int>(1));
            result = Sve.ConditionalSelect(mask1, result + Vector.Create<int>(2), result - Vector.Create<int>(2));
            result = Sve.ConditionalSelect(mask2, result + Vector.Create<int>(4), result - Vector.Create<int>(4));
            result = Sve.ConditionalSelect(mask3, result + Vector.Create<int>(8), result - Vector.Create<int>(8));
            result = Sve.ConditionalSelect(mask4, result + Vector.Create<int>(16), result - Vector.Create<int>(16));
            result = Sve.ConditionalSelect(mask5, result + Vector.Create<int>(32), result - Vector.Create<int>(32));
            result = Sve.ConditionalSelect(mask6, result + Vector.Create<int>(64), result - Vector.Create<int>(64));
            result = Sve.ConditionalSelect(mask7, result + Vector.Create<int>(128), result - Vector.Create<int>(128));
            iterations++;
        }
        while ((iterations < IterationCount) || (Volatile.Read(ref s_collectionCount) == 0));
        Volatile.Write(ref s_exercisingRegisters, false);

        return result;
    }

    private static void CollectUntilStopped()
    {
        Volatile.Write(ref s_collectorReady, true);
        while (!Volatile.Read(ref s_stopCollecting))
        {
            if (!Volatile.Read(ref s_exercisingRegisters))
            {
                Thread.Yield();
                continue;
            }

            GC.Collect();
            Interlocked.Increment(ref s_collectionCount);
        }
    }
}
