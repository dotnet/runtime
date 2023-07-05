// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_3w1d_02
{
public unsafe class testout1
{
    public struct VT_0
    {
        public ulong[,,] arr3d_0;
        public VT_0(int i)
        {
            arr3d_0 = new ulong[5, 4, 4];
        }
    }
    private static double s_a1_0 = -0.035714285714285712;

    public static VT_0 vtstatic_0 = new VT_0(1);

    public static double Func_0()
    {
        vtstatic_0.arr3d_0[4, 0, 3] = 1UL;
        if ((s_a1_0) == ((vtstatic_0.arr3d_0[4, 0, 3] / s_a1_0)))
        {
            if ((128.0) <= ((128.0 + (vtstatic_0.arr3d_0[4, 0, 3] / s_a1_0))))
                Console.WriteLine("Func_0: <= true");
            else
            {
                return Convert.ToDouble((128.0 + (vtstatic_0.arr3d_0[4, 0, 3] / s_a1_0)));
            }
        }
        double retval_0 = Convert.ToDouble((128.0 + (vtstatic_0.arr3d_0[4, 0, 3] / s_a1_0)));
        return retval_0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        vtstatic_0.arr3d_0[4, 0, 3] = 1UL;

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
