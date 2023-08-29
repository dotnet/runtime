// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class Array2
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 500000;
#endif

    static T[][][] AllocArray<T>(int n1, int n2, int n3) {
        T[][][] a = new T[n1][][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2][];
            for (int j = 0; j < n2; j++) {
                a[i][j] = new T[n3];
            }
        }

        return a;
    }

    static void Initialize(int[][][] s) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    s[i][j][k] = (2 * i) - (3 * j) + (5 * k);
                }
            }
        }
    }

    static bool VerifyCopy(int[][][] s, int[][][] d) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    if (s[i][j][k] != d[i][j][k]) {
                        return false;
                    }
                }
            }
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench(int loop) {

        int[][][] s = AllocArray<int>(10, 10, 10);
        int[][][] d = AllocArray<int>(10, 10, 10);

        Initialize(s);

        for (; loop != 0; loop--) {
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    for (int k = 0; k < 10; k++) {
                        d[i][j][k] = s[i][j][k];
                    }
                }
            }
        }

        bool result = VerifyCopy(s, d);

        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = Bench(Iterations);
        return (result ? 100 : -1);
    }
}
}
