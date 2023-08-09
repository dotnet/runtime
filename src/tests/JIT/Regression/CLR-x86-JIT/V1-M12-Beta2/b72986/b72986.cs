// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public unsafe class testout1
{
    public struct VT_0_2
    {
        public int a0_0_2;
#pragma warning disable 0414
        public int a5_0_2;
#pragma warning restore 0414
    }

    public class CL_0_2
    {
        public long a1_0_2 = 4L;
    }

    public static VT_0_2 vtstatic_0_2 = new VT_0_2();

    internal static void Func_0_2(CL_0_2 cl_0_2, double* a3_0_2)
    {
        VT_0_2 vt_0_2 = new VT_0_2();
        vt_0_2.a0_0_2 = 10;
        vt_0_2.a5_0_2 = 9;

        vtstatic_0_2.a0_0_2 = 20;
        vtstatic_0_2.a5_0_2 = 1;
        Console.WriteLine(cl_0_2.a1_0_2 - ((long)(Convert.ToInt32(vtstatic_0_2.a0_0_2) - (long)((cl_0_2.a1_0_2 - ((long)(Convert.ToInt32(vtstatic_0_2.a0_0_2) - (long)((long)(Convert.ToInt32(vtstatic_0_2.a0_0_2) + (long)(39L))))))))));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        CL_0_2 cl_0_2 = new CL_0_2();
        double* a3_0_2 = stackalloc double[1];
        *a3_0_2 = 8;
        Func_0_2(cl_0_2, a3_0_2);
        return 100;
    }
}
