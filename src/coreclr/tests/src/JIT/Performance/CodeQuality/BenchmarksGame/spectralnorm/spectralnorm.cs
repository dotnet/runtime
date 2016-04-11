// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   contributed by Isaac Gouy 

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class SpectralNorm
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 300;
#endif

    public static int Main(String[] args)
    {
        int n = 100;
        if (args.Length > 0) n = Int32.Parse(args[0]);
        double norm = new SpectralNorm().Approximate(n);
        Console.WriteLine("Norm={0:f9}", norm);
        double expected = 1.274219991;
        bool result = Math.Abs(norm - expected) < 1e-4;
        return (result ? 100 : -1);
    }

    [Benchmark]
    public static void Bench()
    {
        int n = 100;
        foreach (var iteration in Benchmark.Iterations)
        {
            double a = 0;

            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    SpectralNorm s = new SpectralNorm();
                    a += s.Approximate(n);
                }
            }

            double norm = a / Iterations;
            double expected = 1.274219991;
            bool valid = Math.Abs(norm - expected) < 1e-4;
            if (!valid)
            {
                throw new Exception("Benchmark failed to validate");
            }
        }
    }

    private double Approximate(int n)
    {
        // create unit vector
        double[] u = new double[n];
        for (int i = 0; i < n; i++) u[i] = 1;

        // 20 steps of the power method
        double[] v = new double[n];
        for (int i = 0; i < n; i++) v[i] = 0;

        for (int i = 0; i < 10; i++)
        {
            MultiplyAtAv(n, u, v);
            MultiplyAtAv(n, v, u);
        }

        // B=AtA         A multiplied by A transposed
        // v.Bv /(v.v)   eigenvalue of v 
        double vBv = 0, vv = 0;
        for (int i = 0; i < n; i++)
        {
            vBv += u[i] * v[i];
            vv += v[i] * v[i];
        }

        return Math.Sqrt(vBv / vv);
    }


    /* return element i,j of infinite matrix A */
    private double A(int i, int j)
    {
        return 1.0 / ((i + j) * (i + j + 1) / 2 + i + 1);
    }

    /* multiply vector v by matrix A */
    private void MultiplyAv(int n, double[] v, double[] Av)
    {
        for (int i = 0; i < n; i++)
        {
            Av[i] = 0;
            for (int j = 0; j < n; j++) Av[i] += A(i, j) * v[j];
        }
    }

    /* multiply vector v by matrix A transposed */
    private void MultiplyAtv(int n, double[] v, double[] Atv)
    {
        for (int i = 0; i < n; i++)
        {
            Atv[i] = 0;
            for (int j = 0; j < n; j++) Atv[i] += A(j, i) * v[j];
        }
    }

    /* multiply vector v by matrix A and then by matrix A transposed */
    private void MultiplyAtAv(int n, double[] v, double[] AtAv)
    {
        double[] u = new double[n];
        MultiplyAv(n, v, u);
        MultiplyAtv(n, u, AtAv);
    }
}
