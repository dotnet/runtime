// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public unsafe class testout1
{
    public struct VT2
    {
        public int a4;
        public int a5;
    }

    public struct VT1
    {
#pragma warning disable 0414
        public double a2;
        public double a3;
#pragma warning restore 0414
    }

    private static int s_a1 = 1;

    public static int Func1(double* arg1, double* arg2)
    {
        VT2 vt2 = new VT2();
        vt2.a4 = 10;
        vt2.a5 = 18;

        int retval1 = Convert.ToInt32((*arg1) + (vt2.a5 * 12)) % Convert.ToInt32((Convert.ToInt32(4 - (-9)) % (17 % vt2.a4)) / (s_a1 * (*arg2)));
        Console.WriteLine("The correct result is 1");
        Console.WriteLine("The actual result is {0}", retval1);
        return retval1;
    }

    public static double Func_0_1(VT1 vt1)
    {
        double* arg1 = stackalloc double[1];
        *arg1 = 4.0;
        double* arg2 = stackalloc double[1];
        *arg2 = 2;
        int val1 = Func1(arg1, arg2);
        double retval0 = Convert.ToDouble(Convert.ToInt32(val1));
        return 1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        VT1 vt1 = new VT1();
        vt1.a2 = 9;
        vt1.a3 = 4;
        double val_0_1 = Func_0_1(vt1);
        int retval_0 = 100;
        return retval_0;
    }
}
