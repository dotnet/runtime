// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from spectral-norm C# .NET Core #3 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=spectralnorm&lang=csharpcore&id=3
// aka (as of 2017-09-01) rev 1.1 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/spectralnorm/spectralnorm.csharp-3.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   contributed by Isaac Gouy 
   modified by Josh Goldfoot, based on the Java version by The Anh Tran
*/

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarksGame
{
    public class SpectralNorm_3
    {
        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int n = arg ?? 100;

            double norm = Bench(n);
            Console.WriteLine("{0:f9}", norm);

            double expected = 1.274219991;
            bool result = Math.Abs(norm - expected) < 1e-4;
            return (result ? 100 : -1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double Bench(int n)
        {
            double[] u = new double[n];
            double[] v = new double[n];
            double[] tmp = new double[n];

            // create unit vector
            for (int i = 0; i < n; i++)
                u[i] = 1.0;

            int nthread = Environment.ProcessorCount;
            int chunk = n / nthread;
            var barrier = new Barrier(nthread);
            Approximate[] ap = new Approximate[nthread];

            for (int i = 0; i < nthread; i++)
            {
                int r1 = i * chunk;
                int r2 = (i < (nthread - 1)) ? r1 + chunk : n;
                ap[i] = new Approximate(u, v, tmp, r1, r2, barrier);
            }

            double vBv = 0, vv = 0;
            for (int i = 0; i < nthread; i++)
            {
                ap[i].t.Wait();
                vBv += ap[i].m_vBv;
                vv += ap[i].m_vv;
            }

            return Math.Sqrt(vBv / vv);
        }

    }

    public class Approximate
    {
        private Barrier barrier;
        public Task t;

        private double[] _u;
        private double[] _v;
        private double[] _tmp;

        private int range_begin, range_end;
        public double m_vBv, m_vv;

        public Approximate(double[] u, double[] v, double[] tmp, int rbegin, int rend, Barrier b)
        {
            m_vBv = 0;
            m_vv = 0;
            _u = u;
            _v = v;
            _tmp = tmp;
            range_begin = rbegin;
            range_end = rend;
            barrier = b;
            t = Task.Run(() => run());
        }

        private void run()
        {
            // 20 steps of the power method
            for (int i = 0; i < 10; i++)
            {
                MultiplyAtAv(_u, _tmp, _v);
                MultiplyAtAv(_v, _tmp, _u);
            }

            for (int i = range_begin; i < range_end; i++)
            {
                m_vBv += _u[i] * _v[i];
                m_vv += _v[i] * _v[i];
            }
        }

        /* return element i,j of infinite matrix A */
        private double eval_A(int i, int j)
        {
            return 1.0 / ((i + j) * (i + j + 1) / 2 + i + 1);
        }

        /* multiply vector v by matrix A, each thread evaluate its range only */
        private void MultiplyAv(double[] v, double[] Av)
        {
            for (int i = range_begin; i < range_end; i++)
            {
                double sum = 0;
                for (int j = 0; j < v.Length; j++)
                    sum += eval_A(i, j) * v[j];

                Av[i] = sum;
            }
        }

        /* multiply vector v by matrix A transposed */
        private void MultiplyAtv(double[] v, double[] Atv)
        {
            for (int i = range_begin; i < range_end; i++)
            {
                double sum = 0;
                for (int j = 0; j < v.Length; j++)
                    sum += eval_A(j, i) * v[j];

                Atv[i] = sum;
            }
        }

        /* multiply vector v by matrix A and then by matrix A transposed */
        private void MultiplyAtAv(double[] v, double[] tmp, double[] AtAv)
        {

            MultiplyAv(v, tmp);
            // all thread must syn at completion
            barrier.SignalAndWait();
            MultiplyAtv(tmp, AtAv);
            // all thread must syn at completion
            barrier.SignalAndWait();
        }

    }
}
