// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Integration by corrected trapezoid rule adapted from Conte and de Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

namespace Benchstone.BenchF
{
    public static class Trap
    {
#if DEBUG
        public const int Iterations = 1;
#else
    public const int Iterations = 240000;
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Bench()
        {
            int nm1, idbg;
            double t2, cortrp, trap, a, b, h;
            trap = 0.0;
            cortrp = 0.0;

            idbg = 0;
            for (int j = 1; j <= Iterations; j++)
            {
                a = 0;
                b = 1;
                if (idbg != 0)
                {
                    System.Console.WriteLine("trapazoid sum    corr.trap sum \n");
                }

                for (int n = 10; n <= 15; n++)
                {
                    h = (b - a) / n;
                    nm1 = n - 1;
                    trap = (F(a) + F(b)) / 2;
                    for (int i = 1; i <= nm1; i++)
                    {
                        t2 = a + i * h;
                        trap = trap + F(t2);
                    }
                    trap = trap * h;
                    cortrp = trap + h * h * (FPrime(a) - FPrime(b)) / 12;
                    if (idbg != 0)
                    {
                        System.Console.WriteLine("{0}, {1}, {2}\n", n, trap, cortrp);
                    }
                }
            }

            // The integral of exp(-x^2) from 0 to 1 is approximately 0.74682413
            const double ExpectedIntegral = 0.74682413;
            const double Tolerance = 1E-5;

            // Validate the corrected trapezoidal estimate (cortrp) for the final iteration (n=15)
            return System.Math.Abs(cortrp - ExpectedIntegral) < Tolerance;
        }

        private static double F(double x)
        {
            return (System.Math.Exp(-(x) * (x)));
        }

        private static double FPrime(double x)
        {
            return ((-2) * (x) * (F(x)));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/86772", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [Fact]
        public static int TestEntryPoint()
        {
            bool result = Bench();
            return (result ? 100 : -1);
        }
    }
}