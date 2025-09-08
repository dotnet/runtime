// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// .NET SIMD to solve Burgers' equation
//
// Benchmark based on
// https://github.com/taumuon/SIMD-Vectorisation-Burgers-Equation-CSharp
// http://www.taumuon.co.uk/2014/10/net-simd-to-solve-burgers-equation.html

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Burgers
{
    private static double BurgersAnalytical(double t, double x, double nu)
    {
        return -2 * nu * (-(-8 * t + 2 * x) * Math.Exp(-Math.Pow((-4 * t + x), 2) / (4 * nu * (t + 1))) / (4 * nu * (t + 1)) - (-8 * t + 2 * x - 12.5663706143592) * Math.Exp(-Math.Pow(-4 * t + x - 6.28318530717959, 2) / (4 * nu * (t + 1))) / (4 * nu * (t + 1))) / (Math.Exp(-Math.Pow(-4 * t + x - 6.28318530717959, 2) / (4 * nu * (t + 1))) + Math.Exp(-Math.Pow(-4 * t + x, 2) / (4 * nu * (t + 1)))) + 4;
    }

    private static double[] linspace(double first, double last, int num)
    {
        var step = (last - first) / (double)num;
        return Enumerable.Range(0, num).Select(v => (v * step) + first).ToArray();
    }

    private static double[] GetAnalytical(double[] x, double t, double nu)
    {
        double[] u = new double[x.Length];

        for (int i = 0; i < x.Length; ++i)
        {
            u[i] = BurgersAnalytical(t, x[i], nu);
        }

        return u;
    }

    private static double[] GetCalculated0(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        Array.Copy(initial, u, u.Length);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            double[] un = new double[nx];
            Array.Copy(u, un, u.Length);

            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);
        }

        return u;
    }

    // Reduce new array allocation and copying, ping-pong between them
    private static double[] GetCalculated1(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        double[] un = new double[nx];
        Array.Copy(initial, un, un.Length);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }

    // Pull calculation of (nu * dt / dx)^2 out into a variable
    private static double[] GetCalculated2(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        double[] un = new double[nx];
        Array.Copy(initial, un, un.Length);

        double factor = Math.Pow(nu * dt / dx, 2.0);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + factor *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + factor *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + factor *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }

    // SIMD
    private static double[] GetCalculated3(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        var nx2 = nx + (Vector<double>.Count - (nx % Vector<double>.Count));

        double[] u = new double[nx2];
        double[] un = new double[nx2];
        Array.Copy(initial, un, initial.Length);

        double factor = Math.Pow(nu * dt / dx, 2.0);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx2 - Vector<double>.Count + 1; i += Vector<double>.Count)
            {
                var vectorIn0 = new Vector<double>(un, i);
                var vectorInPrev = new Vector<double>(un, i - 1);
                var vectorInNext = new Vector<double>(un, i + 1);

                var vectorOut = vectorIn0 - vectorIn0 * (dt / dx) * (vectorIn0 - vectorInPrev) + factor *
                    (vectorInNext - 2.0 * vectorIn0 + vectorInPrev);

                vectorOut.CopyTo(u, i);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + factor *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + factor *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (!Vector.IsHardwareAccelerated)
        {
            Console.WriteLine("Not hardware accelerated!");
        }
        else
        {
            Console.WriteLine("Vector<double>.Length: " + Vector<double>.Count);
        }

        int nx = 10001;

#if DEBUG
        int nt = 10;
#else
        int nt = 10000;
#endif

        double dx = 2.0 * Math.PI / (nx - 1.0);
        double nu = 0.07;
        double dt = dx * nu;
        double[] x = linspace(0.0, 2.0 * Math.PI, nx);
        double[] initial = GetAnalytical(x, 0.0, nu);

        // Warmup

        GetCalculated0(1, nx, dx, dt, nu, initial);
        GetCalculated1(1, nx, dx, dt, nu, initial);
        GetCalculated2(1, nx, dx, dt, nu, initial);
        GetCalculated3(1, nx, dx, dt, nu, initial);

        double[][] results = new double[4][];

        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        results[0] = GetCalculated0(nt, nx, dx, dt, nu, initial);
        stopwatch.Stop();
        Console.WriteLine("Baseline: " + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        stopwatch.Start();
        results[1] = GetCalculated1(nt, nx, dx, dt, nu, initial);
        stopwatch.Stop();
        Console.WriteLine("Reduce copy: " + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        stopwatch.Start();
        results[2] = GetCalculated2(nt, nx, dx, dt, nu, initial);
        stopwatch.Stop();
        Console.WriteLine("CSE of Math.Pow: " + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        stopwatch.Start();
        results[3] = GetCalculated3(nt, nx, dx, dt, nu, initial);
        stopwatch.Stop();
        Console.WriteLine("SIMD: " + stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        for (int i = 0; i < x.Length; i += 33)
        {
            double expected = results[0][i];
            for (int j = 1; j < results.Length; j++)
            {
                bool valid = Math.Abs(expected - results[j][i]) < 1e-4;
                if (!valid)
                {
                    Console.WriteLine("Failed to validate");
                    return -1;
                }
            }
        }

        return 100;
    }
}

