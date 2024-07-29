// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This program solves the "lorenz" equations using Runge-Kutta 4

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Lorenz
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 8000000;
#endif

    private static double s_t = 0.0;
    private static double s_x = 5.0;
    private static double s_y = 2.0;
    private static double s_z = 27.0;

    private static int s_nsteps = Iterations;
    private static double s_h = -1.0;
    private static int s_printDerivative = -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double k1, k2, k3, k4;
        double l1, l2, l3, l4;
        double m1, m2, m3, m4;
        double hdiv2, hdiv6;
        int i;

        if (s_h < 0.0)
        {
            s_h = 20.0 / (double)s_nsteps;
        }
        if (s_printDerivative < 0)
        {
            s_printDerivative = s_nsteps;
        }

        hdiv2 = s_h / 2.0;
        hdiv6 = s_h / 6.0;

        for (i = 0; i < s_nsteps; ++i)
        {
            double t_arg, x_arg, y_arg, z_arg;

            k1 = F(s_t, s_x, s_y, s_z);
            l1 = G(s_t, s_x, s_y, s_z);
            m1 = H(s_t, s_x, s_y, s_z);

            t_arg = s_t + hdiv2;
            x_arg = s_x + hdiv2 * k1;
            y_arg = s_y + hdiv2 * l1;
            z_arg = s_z + hdiv2 * m1;

            k2 = F(t_arg, x_arg, y_arg, z_arg);
            l2 = G(t_arg, x_arg, y_arg, z_arg);
            m2 = H(t_arg, x_arg, y_arg, z_arg);

            x_arg = s_x + hdiv2 * k2;
            y_arg = s_y + hdiv2 * l2;
            z_arg = s_z + hdiv2 * m2;

            k3 = F(t_arg, x_arg, y_arg, z_arg);
            l3 = G(t_arg, x_arg, y_arg, z_arg);
            m3 = H(t_arg, x_arg, y_arg, z_arg);

            t_arg = s_t + s_h;
            x_arg = s_x + s_h * k3;
            y_arg = s_y + s_h * l3;
            z_arg = s_z + s_h * m3;

            k4 = F(t_arg, x_arg, y_arg, z_arg);
            l4 = G(t_arg, x_arg, y_arg, z_arg);
            m4 = H(t_arg, x_arg, y_arg, z_arg);

            s_x = s_x + hdiv6 * (k1 + 2.0 * k2 + 2.0 * k3 + k4);
            s_y = s_y + hdiv6 * (l1 + 2.0 * l2 + 2.0 * l3 + l4);
            s_z = s_z + hdiv6 * (m1 + 2.0 * m2 + 2.0 * m3 + m4);
            s_t = t_arg;
        }

        return true;
    }

    private static double F(double t, double x, double y, double z)
    {
        return (10.0 * (y - x));
    }

    private static double G(double t, double x, double y, double z)
    {
        return (x * (28.0 - z) - y);
    }

    private static double H(double t, double x, double y, double z)
    {
        return (x * y - (8.0 * z) / 3.0);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
