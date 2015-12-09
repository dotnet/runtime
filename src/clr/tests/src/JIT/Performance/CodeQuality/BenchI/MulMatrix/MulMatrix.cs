// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class MulMatrix
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100;
#endif
    
    const int Size = 75;
    static volatile object VolatileObject;

    static void Escape(object obj) {
        VolatileObject = obj;
    }

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static void Inner(int[][] a, int[][] b, int[][] c) {

        int i, j, k, l;

        // setup
        for (j = 0; j < Size; j++) {
            for (i = 0; i < Size; i++) {
                a[i][j] = i;
                b[i][j] = 2 * j;
                c[i][j] = a[i][j] + b[i][j];
            }
        }

        // jkl
        for (j = 0; j < Size; j++) {
            for (k = 0; k < Size; k++) {
                for (l = 0; l < Size; l++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        // jlk
        for (j = 0; j < Size; j++) {
            for (l = 0; l < Size; l++) {
                for (k = 0; k < Size; k++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        // kjl
        for (k = 0; k < Size; k++) {
            for (j = 0; j < Size; j++) {
                for (l = 0; l < Size; l++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        // klj
        for (k = 0; k < Size; k++) {
            for (l = 0; l < Size; l++) {
                for (j = 0; j < Size; j++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        // ljk
        for (l = 0; l < Size; l++) {
            for (j = 0; j < Size; j++) {
                for (k = 0; k < Size; k++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        // lkj
        for (l = 0; l < Size; l++) {
            for (k = 0; k < Size; k++) {
                for (j = 0; j < Size; j++) {
                    c[j][k] += a[j][l] * b[l][k];
                }
            }
        }

        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[][] a = AllocArray<int>(Size, Size);
        int[][] b = AllocArray<int>(Size, Size);
        int[][] c = AllocArray<int>(Size, Size);

        for (int i = 0; i < Iterations; ++i) {
            Inner(a, b, c);
        }

        Escape(c);
        return true;
    }

    [Benchmark]
    public static void Test() {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                Bench();
            }
        }
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }
    
    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
