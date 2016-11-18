// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;

namespace PerfLabTests
{
    public static class StackWalk
    {
        [Benchmark(InnerIterationCount = 1000)]
        public static void Walk()
        {
            A(5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int A(int a) { return B(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int B(int a) { return C(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int C(int a) { return D(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int D(int a) { return E(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int E(int a) { return F(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int F(int a) { return G(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int G(int a) { return H(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int H(int a) { return I(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int I(int a) { return J(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int J(int a) { return K(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int K(int a) { return L(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int L(int a) { return M(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int M(int a) { return N(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int N(int a) { return O(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int O(int a) { return P(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int P(int a) { return Q(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Q(int a) { return R(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int R(int a) { return S(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int S(int a) { return T(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int T(int a) { return U(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int U(int a) { return V(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int V(int a) { return W(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int W(int a) { return X(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int X(int a) { return Y(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Y(int a) { return Z(a + 5); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Z(int a)
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        GC.Collect(0);

            return 55;
        }
    }
}