// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_3w1d_01
{
public unsafe class testout1
{
    public class CL_0
    {
        public double[] arr1d_0 = new double[4];
    }
    private static double s_a1_0 = -268862297D;

    public static CL_0 clstatic_0 = new CL_0();

    public static int Func_0()
    {
        clstatic_0.arr1d_0[0] = 16190D;
        int retval_0 = 1075514048 % (int)(clstatic_0.arr1d_0[0] - s_a1_0);
        return retval_0;
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
