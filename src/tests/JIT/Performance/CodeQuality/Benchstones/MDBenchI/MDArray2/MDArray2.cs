// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchI
{
public static class MDArray2
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 500000;
#endif

    static void Initialize(int[,,] s) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    s[i,j,k] = (2 * i) - (3 * j) + (5 * k);
                }
            }
        }
    }

    static bool VerifyCopy(int[,,] s, int[,,] d) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    if (s[i,j,k] != d[i,j,k]) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench(int loop) {

        int[,,] s = new int[10, 10, 10];
        int[,,] d = new int[10, 10, 10];

        Initialize(s);

        for (; loop != 0; loop--) {
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    for (int k = 0; k < 10; k++) {
                        d[i,j,k] = s[i,j,k];
                    }
                }
            }
        }

        bool result = VerifyCopy(s, d);
        return result;
    }

    static bool TestBase() {
        bool result = Bench(Iterations);
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
