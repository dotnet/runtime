// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System;
using System.Numerics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

[assembly: OptimizeForBenchmarks]

public static class SeekUnroll
{

    // The purpose of this micro-benchmark is to measure the effect of unrolling
    // on this loop (taken from https://github.com/aspnet/KestrelHttpServer/pull/1138)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int FindByte(ref Vector<byte> byteEquals)
    {
        var vector64 = Vector.AsVectorInt64(byteEquals);
        long longValue = 0;
        var i = 0;
        for (; i < Vector<long>.Count; i++)
        {
            longValue = vector64[i];
            if (longValue == 0) continue;
            break;
        }

        // Flag least significant power of two bit
        var powerOfTwoFlag = (ulong)(longValue ^ (longValue - 1));
        // Shift all powers of two into the high byte and extract
        var foundByteIndex = (int)((powerOfTwoFlag * _xorPowerOfTwoToHighByte) >> 57);
        // Single LEA instruction with jitted const (using function result)
        return i * 8 + foundByteIndex;
    }

    // Magic constant used in FindByte
    const ulong _xorPowerOfTwoToHighByte = (0x07ul |
                                            0x06ul << 8 |
                                            0x05ul << 16 |
                                            0x04ul << 24 |
                                            0x03ul << 32 |
                                            0x02ul << 40 |
                                            0x01ul << 48) + 1;

    // Inner loop to repeatedly call FindByte
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void InnerLoop(ref int foundIndex, ref Vector<Byte> vector)
    {
        for (int i = 0; i < InnerIterations; i++)
        {
            foundIndex = FindByte(ref vector);
        }
    }

    // Iteration counts for inner loop set to have each call take 1 or
    // 2 seconds or so in release, finish quickly in debug.
#if DEBUG
    static int InnerIterations = 1;
#else
    static int InnerIterations = 1000000000;
#endif

    // Function to meaure InnerLoop using the xunit-perf benchmark measurement facilities
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void XunitBenchmarkLoop(ref int foundIndex, ref Vector<Byte> vector)
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                InnerLoop(ref foundIndex, ref vector);
            }
        }
    }

    // Function to measure InnerLoop with manual use of a stopwatch timer
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ManualTimerLoop(ref int foundIndex, ref Vector<Byte> vector)
    {
        for (int iteration = 0; iteration < ManualLoopTimes.Length; ++iteration)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            InnerLoop(ref foundIndex, ref vector);
            timer.Stop();
            ManualLoopTimes[iteration] = timer.ElapsedMilliseconds;
        }
    }
    static long[] ManualLoopTimes;

    // Function that tests one input, dispatching to either the xunit-perf
    // loop or the manual timer loop
    static bool Test(int index, bool isXunitBenchmark)
    {
        if (index >= Vector<Byte>.Count)
        {
            // FindByte assumes index is in range
            index = 0;
        }
        var bytes = new Byte[Vector<Byte>.Count];
        bytes[index] = 255;
        Vector<Byte> vector = new Vector<Byte>(bytes);

        int foundIndex = -1;

        if (isXunitBenchmark)
        {
            XunitBenchmarkLoop(ref foundIndex, ref vector);
        }
        else
        {
            ManualTimerLoop(ref foundIndex, ref vector);
        }

        Assert.Equal(index, foundIndex);
        return (index == foundIndex);
    }

    // Set of indices to pass to Test(int, bool)
    static int[] IndicesToTest = new int[] { 1, 3, 11, 19, 27 };

    // Entrypoint for xunit-perf to call the benchmark
    [Benchmark]
    [MemberData(nameof(ArrayedBoxedIndicesToTest))]
    public static bool TestWithXunit(object boxedIndex)
    {
        return Test((int)boxedIndex, true);
    }

    // IndicesToTest wrapped up in arrays of boxes since that's
    // what xunit-perf needs
    public static IEnumerable<object[]> ArrayedBoxedIndicesToTest =
        IndicesToTest.Select((int index) => new object[] { index });


    // Main method entrypoint runs the manual timer loop
    public static int Main(string[] args)
    {
        int failures = 0;

        // On non-hardware accelerated platforms, the test times out because it runs for too long.
        // In those cases, we decrease InnerIterations so the test doesn't time out.
        if (!Vector.IsHardwareAccelerated)
        {
            InnerIterations = 100000;
        }

        int manualLoopCount = 1;
        if (args == null || args.Length == 0)
        {
            Console.WriteLine("Warning: no iteration count specified; defaulting to 1 iteration per case");
            Console.WriteLine("To use multiple iterations per case, pass the desired number of iterations as the first command-line argument to this test");
        }
        else
        {
            manualLoopCount = int.Parse(args[0]);
        }

        foreach(int index in IndicesToTest)
        {
            ManualLoopTimes = new long[manualLoopCount];
            bool passed = Test(index, false);
            if (!passed)
            {
                ++failures;
            }
            Console.WriteLine("Index {0}, times (ms) [{1}]", index, String.Join(", ", ManualLoopTimes));
        }

        return 100 + failures;
    }
}
