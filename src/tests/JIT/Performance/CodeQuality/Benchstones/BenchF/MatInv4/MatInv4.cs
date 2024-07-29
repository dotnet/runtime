// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class MatInv4
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 60;
#endif

    private static float s_det;

    private struct X
    {
        public float[] A;
        public X(int size)
        {
            A = new float[size];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        X a = new X(Iterations * Iterations);
        float[] b = new float[Iterations * Iterations];
        float[] c = new float[Iterations * Iterations];
        float[] d = new float[Iterations * Iterations];
        float[] l1 = new float[Iterations];
        float[] l2 = new float[Iterations];

        int i, k, n, nsq;

        n = Iterations;
        nsq = n * n;
        for (i = 0; i < n; ++i)
        {
            for (k = 0; k < n; ++k)
            {
                if (i == k)
                {
                    a.A[i * n + k] = 40.0F;
                }
                else
                {
                    a.A[i * n + k] = 0.0F;
                }
            }
        }

        for (i = 0; i < n; ++i)
        {
            for (k = i; k < nsq; k += n)
            {
                b[k] = a.A[k];
            }
        }

        /*** second(&t1); ***/

        MinV1(b, ref n, out s_det, l1, l2);

        if (s_det == 0.0F)
        {
            goto L990;
        }

        /*** second(&tx); ***/

        MProd(b, a.A, c, ref n);
        for (k = 1; k <= nsq; ++k)
        {
            b[k - 1] = a.A[k - 1];
        }

        /*** second(&tx); ***/

        MinV2(b, ref n, out s_det, l1, l2);

        if (s_det == 0.0F)
        {
            goto L990;
        }

        /*** second(&ty); ***/

        MProd(b, a.A, d, ref n);
        CompM(c, d, ref n);

        /*** second(&t2); ***/

        return true;

    L990:
        {
        }

        return true;
    }

    private static void MinV1(float[] a, ref int n, out float d, float[] l, float[] m)
    {
        float biga, hold;
        int i, j, k, ij, ik, ji, jk, nk, ki, kj, kk, iz, jp, jq, jr;

        d = 1.0F;
        ji = 0;
        hold = 0.0F;
        nk = -n;
        for (k = 1; k <= n; ++k)
        {
            nk = nk + n;
            l[k - 1] = k;
            m[k - 1] = k;
            kk = nk + k;
            biga = a[kk - 1];
            for (j = k; j <= n; ++j)
            {
                // j <= n, so iz <= n^2 - n
                iz = n * (j - 1);
                for (i = k; i <= n; ++i)
                {
                    // iz <= n^2 - n and i <= n, so ij <= n^2
                    ij = iz + i;
                    if (System.Math.Abs(biga) >= System.Math.Abs(a[ij - 1]))
                    {
                        continue;
                    }
                    // accessing up to n^2 - 1
                    biga = a[ij - 1];
                    l[k - 1] = i;
                    m[k - 1] = j;
                }
            }

            j = (int)l[k - 1];

            if (j <= k)
            {
                goto L35;
            }

            // -n < ki <= 0
            ki = k - n;
            for (i = 1; i <= n; ++i)
            {
                // i <= n, ki <= n + n + ... + n (n times) i.e. k <= n * n (when ki = 0 initially)
                ki = ki + n;
                // Accessing upto n^2 -1
                hold = -a[ki - 1];
                // ji <= n^2 - n + n (for ki = 0 initially when k = n and 0 < j <= n)
                // Therefore ji <= n^2
                ji = ki - k + j;
                a[ki - 1] = a[ji - 1];
                a[ji - 1] = hold;
            }
        L35:
            i = (int)m[k - 1];
            if (i <= k)
            {
                goto L45;
            }

            // 0 <= jp <= n^2 - n
            jp = n * (i - 1);
            for (j = 1; j <= n; ++j)
            {
                // 0 < nk <= n * (n-1)
                // jk <= n^2 - n + n
                // jk <= n^2
                jk = nk + j;
                // jp <= n^2 - n
                // ji <= n^2 - n + n or ji <= n^2 (since 0 < j <= n)
                ji = jp + j;
                hold = -a[jk - 1];
                a[jk - 1] = a[ji - 1];
                a[ji - 1] = hold;
            }
        L45:
            if (biga != 0.0F)
            {
                goto L48;
            }
            d = 0.0F;
            return;

        L48:
            for (i = 1; i <= n; ++i)
            {
                if (i == k)
                {
                    break;
                }
                // 0 < nk <= n * (n-1)
                // 0 < ik <= n^2
                ik = nk + i;
                a[ik - 1] = a[ik - 1] / (-biga);
            }

            for (i = 1; i <= n; ++i)
            {
                if (i == k)
                {
                    continue;
                }
                // 0 < nk <= n * (n-1)
                // 0 < ik <= n^2
                ik = nk + i;
                hold = a[ik - 1];
                // -n < ij <= 0
                ij = i - n;
                for (j = 1; j <= n; ++j)
                {
                    // i <= n, ij <= n + n + ... + n (n times) or ij <= n * n
                    ij = ij + n;
                    if (j == k)
                    {
                        continue;
                    }
                    // if i=1, kj = (1 + (n-1) * n) - 1 + n ==> ij = n^2
                    // if i=n, kj = (n * n) - n + n ==> ij = n ^2
                    // So j <= n^2
                    kj = ij - i + k;
                    a[ij - 1] = hold * a[kj - 1] + a[ij - 1];
                }
            }
            kj = k - n;
            for (j = 1; j <= n; ++j)
            {
                // k <= n, kj <= n + n + ... + n (n times) or kj <= n * n
                kj = kj + n;
                if (j == k)
                {
                    continue;
                }
                // Accessing upto n^2 - 1
                a[kj - 1] = a[kj - 1] / biga;
            }
            d = d * biga;
            a[kk - 1] = 1.0F / biga;
        }
        k = n;
    L100:
        k = k - 1;
        if (k < 1)
        {
            return;
        }
        i = (int)l[k - 1];
        if (i <= k)
        {
            goto L120;
        }

        // 0 <= jq <= n^2 - n
        // 0 <= jr <= n^2 - n
        jq = n * (k - 1);
        jr = n * (i - 1);
        for (j = 1; j <= n; ++j)
        {
            // jk <= n^2 - n + n
            // jk <= n^2
            jk = jq + j;
            hold = a[jk - 1];
            // ji <= n^2 - n + n
            // ji <= n^2
            ji = jr + j;
            a[jk - 1] = -a[ji - 1];
            a[ji - 1] = hold;
        }
    L120:
        j = (int)m[k - 1];
        if (j <= k)
        {
            goto L100;
        }
        // 0 <= jr <= n^2 - n
        ki = k - n;
        for (i = 1; i <= n; ++i)
        {
            // ki <= n + n + ... + n (n times) or ki <= n * n
            ki = ki + n;
            hold = a[ki - 1];
            // if i=1, ji = (1 + (n-1) * n) - 1 + n ==> ij = n^2
            // if i=n, ji = (n * n) - n + n ==> ij = n ^2
            // Therefore ji <= n^2
            ji = ki - k + j;
            a[ki - 1] = -a[ji - 1];
        }
        a[ji - 1] = hold;
        goto L100;
    }

    private static void MinV2(float[] a, ref int n, out float d, float[] l, float[] m)
    {
        float biga, hold;
        int i, j, k;

        d = 1.0F;
        for (k = 1; k <= n; ++k)
        {
            l[k - 1] = k;
            m[k - 1] = k;
            biga = a[(k - 1) * n + (k - 1)];
            for (j = k; j <= n; ++j)
            {
                for (i = k; i <= n; ++i)
                {
                    // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                    if (System.Math.Abs(biga) >= System.Math.Abs(a[(i - 1) * n + (j - 1)]))
                    {
                        continue;
                    }
                    biga = a[(i - 1) * n + (j - 1)];
                    l[k - 1] = i;
                    m[k - 1] = j;
                }
            }
            j = (int)l[k - 1];
            if (l[k - 1] <= k)
            {
                goto L200;
            }
            for (i = 1; i <= n; ++i)
            {
                // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                hold = -a[(k - 1) * n + (i - 1)];
                a[(k - 1) * n + (i - 1)] = a[(j - 1) * n + (i - 1)];
                a[(j - 1) * n + (i - 1)] = hold;
            }
        L200:
            i = (int)m[k - 1];
            if (m[k - 1] <= k)
            {
                goto L250;
            }
            for (j = 1; j <= n; ++j)
            {
                // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                hold = -a[(j - 1) * n + (k - 1)];
                a[(j - 1) * n + (k - 1)] = a[(j - 1) * n + (i - 1)];
                a[(j - 1) * n + (i - 1)] = hold;
            }
        L250:
            if (biga != 0.0F)
            {
                goto L300;
            }
            d = 0.0F;
            return;

        L300:
            for (i = 1; i <= n; ++i)
            {
                if (i != k)
                {
                    // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                    a[(i - 1) * n + (k - 1)] = a[(i - 1) * n + (k - 1)] / (-biga);
                }
            }
            for (i = 1; i <= n; ++i)
            {
                if (i == k)
                {
                    continue;
                }
                for (j = 1; j <= n; ++j)
                {
                    if (j != k)
                    {
                        // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                        a[(i - 1) * n + (j - 1)] = a[(i - 1) * n + (k - 1)] * a[(k - 1) * n + (j - 1)] + a[(i - 1) * n + (j - 1)];
                    }
                }
            }
            for (j = 1; j < n; ++j)
            {
                if (j != k)
                {
                    // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                    a[(k - 1) * n + (j - 1)] = a[(k - 1) * n + (j - 1)] / biga;
                }
            }
            d = d * biga;
            a[(k - 1) * n + (k - 1)] = 1.0F / biga;
        }
        k = n;
    L400:
        k = k - 1;
        if (k < 1)
        {
            return;
        }
        i = (int)l[k - 1];
        if (i <= k)
        {
            goto L450;
        }
        for (j = 1; j <= n; ++j)
        {
            // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
            hold = a[(j - 1) * n + (k - 1)];
            a[(j - 1) * n + (k - 1)] = -a[(j - 1) * n + (i - 1)];
            a[(j - 1) * n + (i - 1)] = hold;
        }
    L450:
        j = (int)m[k - 1];
        if (j <= k)
        {
            goto L400;
        }
        for (i = 1; i <= n; ++i)
        {
            // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
            hold = a[(k - 1) * n + (i - 1)];
            a[(k - 1) * n + (i - 1)] = -a[(j - 1) * n + (i - 1)];
            a[(j - 1) * n + (i - 1)] = hold;
        }
        goto L400;
    }

    private static void MProd(float[] a, float[] b, float[] c, ref int n)
    {
        int i, j, k;

        for (i = 1; i <= n; ++i)
        {
            for (j = 1; j <= n; ++j)
            {
                // Accessing upto n^2 - n + n - 1 ==> n^2 - 1
                c[(i - 1) * n + (j - 1)] = 0.0F;
                for (k = 1; k <= n; ++k)
                {
                    c[(i - 1) * n + (j - 1)] = c[(i - 1) * n + (j - 1)] + a[(i - 1) * n + (k - 1)] * b[(k - 1) * n + (j - 1)];
                }
            }
        }
        return;
    }

    private static void CompM(float[] a, float[] b, ref int n)
    {
        int i, j;
        float x, sum = 0.0F;

        //(starting compare.)
        for (i = 1; i <= n; ++i)
        {
            for (j = 1; j <= n; ++j)
            {
                x = 0.0F;
                if (i == j)
                {
                    x = 1.0F;
                }
                sum = sum + System.Math.Abs(System.Math.Abs(a[(i - 1) * n + (j - 1)]) - x);
            }
        }
        return;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
