// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]

namespace Benchstone.BenchI
{
public static class AddArray
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 15000;
#endif

    const int Size = 6000;

    public static volatile object VolatileObject;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Escape(object obj) {
        VolatileObject = obj;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {

        int[] flags1 = new int[Size + 1];
        int[] flags2 = new int[Size + 1];
        int[] flags3 = new int[Size + 1];
        int[] flags4 = new int[Size + 1];

        int j, k, l, m;

        for (j = 0; j <= Size; j++) {
            flags1[j] = 70000 + j;
            k = j;
            flags2[k] = flags1[j] + k + k;
            l = j;
            flags3[l] = flags2[k] + l + l + l;
            m = j;
            flags4[m] = flags3[l] + m + m + m + m;
        }

        for (j = 0; j <= Size; j++) {
            k = j;
            l = j;
            m = j;
            flags1[j] = flags1[j] + flags2[k] + flags3[l] + flags4[m] - flags2[k - j + l];
        }

        // Escape each flags array so that their elements will appear live-out
        Escape(flags1);
        Escape(flags2);
        Escape(flags3);
        Escape(flags4);

        return true;
    }

    [Benchmark]
    public static void Test() {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                for (int i = 0; i < Iterations; i++) {
                    Bench();
                }
            }
        }
    }

    static bool TestBase() {
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench();
        }
        return result;
    }

    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
