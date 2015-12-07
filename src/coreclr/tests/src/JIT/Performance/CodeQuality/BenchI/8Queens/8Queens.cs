// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class EightQueens
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    static int[] m_c = new int[15];
    static int[] m_x = new int[9];

    static void TryMe(int i, ref int q, int[] a, int[] b)
    {
        int j = 0;
        q = 0;
        while ((q == 0) && (j != 8)) {
            j = j + 1;
            q = 0;
            if ((b[j] == 1) && (a[i + j] == 1) && (m_c[i - j + 7] == 1)) {
                m_x[i] = j;
                b[j] = 0;
                a[i + j] = 0;
                m_c[i - j + 7] = 0;
                if (i < 8) {
                    TryMe(i + 1, ref q, a, b);
                    if (q == 0) {
                        b[j] = 1;
                        a[i + j] = 1;
                        m_c[i - j + 7] = 1;
                    }
                }
                else {
                    q = 1;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[] a = new int[9];
        int[] b = new int[17];
        int q = 0;
        int i = 0;
        while (i <= 16) {
            if ((i >= 1) && (i <= 8)) {
                a[i] = 1;
            }
            if (i >= 2) {
                b[i] = 1;
            }
            if (i <= 14) {
                m_c[i] = 1;
            }
            i = i + 1;
        }
        
        TryMe(1, ref q, b, a);

        return (q == 1);
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
