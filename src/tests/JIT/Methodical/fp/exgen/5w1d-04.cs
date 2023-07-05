// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_5w1d_04
{
public unsafe class testout1
{
    public struct VT_0
    {
        public double a1_0;
        public VT_0(int i)
        {
            a1_0 = 1;
        }
    }
    public class CL_0
    {
        public double a0_0 = -1013.76;
    }


    public static double Func_0()
    {
        VT_0 vt_0 = new VT_0(1);
        vt_0.a1_0 = 10.24;
        CL_0 cl_0 = new CL_0();

        double asgop0 = vt_0.a1_0;
        asgop0 -= ((cl_0.a0_0));
        double asgop1 = cl_0.a0_0;
        asgop1 /= (-99.0);
        return Convert.ToDouble((asgop0 / asgop1));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int retval;
        retval = Convert.ToInt32(Func_0());
        if ((retval >= 99) && (retval < 100))
            retval = 100;
        if ((retval > 100) && (retval <= 101))
            retval = 100;
        Console.WriteLine(retval);
        return retval;
    }
}
}
