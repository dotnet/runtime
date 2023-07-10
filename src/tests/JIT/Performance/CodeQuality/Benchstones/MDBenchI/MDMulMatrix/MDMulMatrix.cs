// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchI
{
public static class MDMulMatrix
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

    static void Inner(int[,] a, int[,] b, int[,] c) {

        int i, j, k, l;

        // setup
        for (j = 0; j < Size; j++) {
            for (i = 0; i < Size; i++) {
                a[i,j] = i;
                b[i,j] = 2 * j;
                c[i,j] = a[i,j] + b[i,j];
            }
        }

        // jkl
        for (j = 0; j < Size; j++) {
            for (k = 0; k < Size; k++) {
                for (l = 0; l < Size; l++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        // jlk
        for (j = 0; j < Size; j++) {
            for (l = 0; l < Size; l++) {
                for (k = 0; k < Size; k++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        // kjl
        for (k = 0; k < Size; k++) {
            for (j = 0; j < Size; j++) {
                for (l = 0; l < Size; l++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        // klj
        for (k = 0; k < Size; k++) {
            for (l = 0; l < Size; l++) {
                for (j = 0; j < Size; j++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        // ljk
        for (l = 0; l < Size; l++) {
            for (j = 0; j < Size; j++) {
                for (k = 0; k < Size; k++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        // lkj
        for (l = 0; l < Size; l++) {
            for (k = 0; k < Size; k++) {
                for (j = 0; j < Size; j++) {
                    c[j,k] += a[j,l] * b[l,k];
                }
            }
        }

        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[,] a = new int[Size, Size];
        int[,] b = new int[Size, Size];
        int[,] c = new int[Size, Size];

        for (int i = 0; i < Iterations; ++i) {
            Inner(a, b, c);
        }

        Escape(c);
        return true;
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
