// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
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
