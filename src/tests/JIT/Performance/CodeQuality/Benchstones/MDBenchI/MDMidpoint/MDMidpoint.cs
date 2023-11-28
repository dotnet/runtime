// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchI
{
public static class MDMidpoint
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 70000;
#endif

    static int Inner(ref int x, ref int y, ref int z) {
        int mid;

        if (x < y) {
            if (y < z) {
                mid = y;
            }
            else {
                if (x < z) {
                    mid = z;
                }
                else {
                    mid = x;
                }
            }
        }
        else {
            if (x < z) {
                mid = x;
            }
            else {
                if (y < z) {
                    mid = z;
                }
                else {
                    mid = y;
                }
            }
        }

        return (mid);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[,] a = new int[2001, 4];
        int[] mid = new int[2001];
        int j = 99999;

        for (int i = 1; i <= 2000; i++) {
            a[i,1] = j & 32767;
            a[i,2] = (j + 11111) & 32767;
            a[i,3] = (j + 22222) & 32767;
            j = j + 33333;
        }

        for (int k = 1; k <= Iterations; k++) {
            for (int l = 1; l <= 2000; l++) {
                mid[l] = Inner(ref a[l,1], ref a[l,2], ref a[l,3]);
            }
        }

        return (mid[2000] == 17018);
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
