// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_5w1d_02
{
public unsafe class testout1
{
    private static double[] s_arr1d_0 = new double[6];
    private static double s_a2_0 = 128.0;


    public static double Func_0()
    {
        s_arr1d_0[0] = 3758096384.0;
        double asgop0 = 2.9802322387695312E-08;
        asgop0 *= (s_arr1d_0[0]);
        double asgop1 = s_arr1d_0[0];
        asgop1 -= (3758096468.0);
        return Convert.ToDouble(((s_a2_0 - asgop0) - (asgop1)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        s_arr1d_0[0] = 3758096384.0;

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
