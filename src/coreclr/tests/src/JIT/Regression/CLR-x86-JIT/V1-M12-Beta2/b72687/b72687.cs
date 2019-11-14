// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal unsafe class CSE2
{
    public static int sa = 2;
    public static int sb = 1;

    public struct VT
    {
        public double a0;
    }

    public class CL_5
    {
        public int[,] arr2d_5 = new int[3, 11];
    }

    public static double Func_5(CL_5 cl_5)
    {
        double retval_5 = -24;
        return retval_5;
    }

    public static long Func_4(long* a0_4)
    {
        long retval_4 = 6;
        return retval_4;
    }

    public static int Func(VT vt)
    {
        CL_5 cl_5 = new CL_5();
        cl_5.arr2d_5[2, 0] = sa * sb;
        double val_5 = Func_5(cl_5);

        long* a0_4 = stackalloc long[1];
        *a0_4 = sa + sb;
        long val_4 = Func_4(a0_4);

        int retval = Convert.ToInt32((val_5 - 8 * vt.a0) + (((sa + sb) - 5) / 2 + val_4));
        Console.WriteLine("retval is {0}", retval);
        return retval;
    }

    public static int Main()
    {
        VT vt = new VT();
        vt.a0 = -(sa + sb);
        int val = Func(vt);
        return val + 95;
    }
}
