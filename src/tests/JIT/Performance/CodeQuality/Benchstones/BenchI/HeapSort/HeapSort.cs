// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class HeapSort
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 2500;
#endif

    const int ArraySize = 5500;

    static void Inner(int[] x, int n) {
        int i, j, k, m;

        // pass1 -- put vector in heap form
        // that is to say, guarantee that x(i)>=x(2*i) and x(i)>=x(2*i+1).
        // after pass 1, the largest item will be at x(1).
        for (i = 2; i <= n; i++) {
            j = i;
            k = j / 2;
            m = x[i];

            // 0 < k <= (n / 2)
            // 1 <= j <= n
            while (k > 0) {
                if (m <= x[k]) {
                    break;
                }
                x[j] = x[k];
                j = k;
                k = k / 2;
            }
            x[j] = m;
        }

        // pass 2 --  swap first and last items.  now with the last
        // item correctly placed, consider the list shorter by one item.
        // restore the shortened list to heap sort, and repeat
        // process until list is only two items long.
        i = n;
        do {
            // do i = n to 2 by -1;
            m = x[i];
            x[i] = x[1];  // last item, i.e. item(i) now correct.
            j = 1;        // we now find the appropriate resting point for m
            k = 2;

            // 2 <= k < i ==> 2 <= k < n
            // 1 <= j < n
            while (k < i) {
                if ((k + 1) < i) {
                    if (x[k + 1] > x[k]) {
                        k = k + 1;
                    }
                }
                if (x[k] <= m) {
                    break;
                }

                x[j] = x[k];
                j = k;
                k = k + k;
            }

            x[j] = m;
            i = i - 1;
        } while (i >= 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[] x = new int[ArraySize + 1];
        for (int i = 1; i <= ArraySize; i++) {
            x[i] = ArraySize - i + 1;
        }
        Inner(x, ArraySize);
        for (int j = 1; j <= ArraySize; j++) {
            if (x[j] != j) {
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
