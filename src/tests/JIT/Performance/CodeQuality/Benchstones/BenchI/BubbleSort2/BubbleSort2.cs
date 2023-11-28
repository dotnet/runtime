// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class BubbleSort2
{

#if DEBUG
    public const int Iterations = 1;
    public const int Bound = 5 * Iterations;
#else
    public const int Iterations = 15;
    public const int Bound = 500 * Iterations;
#endif

    static void Inner(int[] x) {
        int limit1 = Bound - 1;
        for (int i = 1; i <= limit1; i++) {
            for (int j = i; j <= Bound; j++) {
                if (x[i] > x[j]) {
                    int temp = x[j];
                    x[j] = x[i];
                    x[i] = temp;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[] x = new int[Bound + 1];
        int i, j;
        int limit;
        j = 99999;
        limit = Bound - 2;
        i = 1;
        do {
            x[i] = j & 32767;
            x[i + 1] = (j + 11111) & 32767;
            x[i + 2] = (j + 22222) & 32767;
            j = j + 33333;
            i = i + 3;
        } while (i <= limit);
        x[Bound - 1] = j;
        x[Bound] = j;

        Inner(x);

        for (i = 0; i < Bound - 1; i++) {
            if (x[i] > x[i + 1]) {
                return false;
            }
        }

        return true;
    }

    static bool TestBase() {
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench();
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
