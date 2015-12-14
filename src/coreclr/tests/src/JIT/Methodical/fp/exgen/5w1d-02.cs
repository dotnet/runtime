// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
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

    public static int Main()
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
