// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class BubbleSort
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 55000;
#endif

    static void SortArray(int[] tab, int last) {
        bool swap;
        int temp;
        do {
            swap = false;
            for (int i = 0; i < last; i++) {
                if (tab[i] > tab[i + 1]) {
                    temp = tab[i];
                    tab[i] = tab[i + 1];
                    tab[i + 1] = temp;
                    swap = true;
                }
            }
        }
        while (swap);
    }

    static bool VerifySort(int[] tab, int last) {
        for (int i = 0; i < last; i++) {
            if (tab[i] > tab[i + 1]) {
                return false;
            }
        }
        
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static public bool Bench() {
        int[] tab = new int[100];
        int k = 0;
        for (int i = 9; i >= 0; i--) {
            for (int j = i * 10; j < (i + 1) * 10; j++) {
                tab[k++] = ((j & 1) == 1) ? j + 1 : j - 1;
            }
        }
        SortArray(tab, 99);
        bool result = VerifySort(tab, 99);
        return result;
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
