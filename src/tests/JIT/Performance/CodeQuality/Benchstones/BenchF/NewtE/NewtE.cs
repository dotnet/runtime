// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Simultaneous equations by Newton's method adapted from Conte and De Boor
// to solve F(X,Y)=0 and G(X,Y)=0

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class NewtE
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 1000000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double idgb, a, b, x, y, deltaX, deltaY;
        a = 0;
        b = 0;
        x = 0;
        y = 0;
        idgb = 0;

        if (idgb != 0)
        {
            System.Console.WriteLine("{0}, {1}, F(x,y) , G(x,y) \n", x, y);
        }

        for (int j = 1; j <= Iterations; j++)
        {
            x = 1.0;
            y = (-2.0);
            a = F(x, y);
            b = G(x, y);
            if (idgb != 0)
            {
                System.Console.WriteLine(" {0}, {1}, {2}, {3}\n", x, y, a, b);
            }

            for (int i = 1; i <= 20; i++)
            {
                deltaX = (-F(x, y) * GY(x, y) + G(x, y) * FY(x, y)) / (FX(x, y) * GY(x, y) - FY(x, y) * GX(x, y));
                deltaY = (-G(x, y) * FX(x, y) + F(x, y) * GX(x, y)) / (FX(x, y) * GY(x, y) - FY(x, y) * GX(x, y));
                x = x + deltaX;
                y = y + deltaY;
                a = F(x, y);
                b = G(x, y);
                if (idgb != 0)
                {
                    System.Console.WriteLine("{0}, {1}, {2}, {3}, {4}\n", i, x, y, a, b);
                }

                if ((System.Math.Abs(deltaX) < 0.000001) && (System.Math.Abs(deltaY) < 0.000001) &&
                   (System.Math.Abs(a) < 0.000001) && (System.Math.Abs(b) < 0.000001))
                {
                    goto L11;
                }
            }
            if (idgb != 0)
            {
                System.Console.WriteLine("FAILED TO CONVERGE IN 20 ITERATIONS\n");
            }

        L11:
            {
            }
        }

        return true;
    }

    private static double F(double x, double y)
    {
        return ((x) + 3 * System.Math.Log(x) / System.Math.Log(10.0) - (y) * (y));
    }

    private static double G(double x, double y)
    {
        return (2 * (x) * (x) - (x) * (y) - 5 * (x) + 1);
    }

    private static double FX(double x, double y)
    {
        return (1 + 3 / (System.Math.Log(10.0) * (x)));
    }

    private static double FY(double x, double y)
    {
        return ((-2) * (y));
    }

    private static double GX(double x, double y)
    {
        return (4 * (x) - (y) - 5);
    }

    private static double GY(double x, double y)
    {
        return (-(x));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
