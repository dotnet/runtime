// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
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

    public static int Main()
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
