// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace Test_5w1d_03
{
public unsafe class testout1
{
    public class CL_0
    {
        public Decimal[,,] arr3d_0 = new Decimal[5, 6, 4];
        public Decimal a1_0 = -3.0476190476190476190476190476M;
        public ulong a2_0 = 4096UL;
    }

    public static CL_0 clstatic_0 = new CL_0();

    public static int Func_0()
    {
        CL_0 cl_0 = new CL_0();

        cl_0.arr3d_0[4, 0, 3] = 16M;
        int retval_0 = Convert.ToInt32(Convert.ToInt32(cl_0.arr3d_0[4, 0, 3] - ((Convert.ToDecimal((Convert.ToUInt64(clstatic_0.a2_0 / 16UL))) / clstatic_0.a1_0))));
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
