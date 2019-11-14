// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal class testout1
{
    private static double[,,] s_arr3d_0 = new double[5, 6, 4];
    private static double[,] s_arr2d_0 = new double[3, 6];


    public static int Func_0()
    {
        s_arr3d_0[4, 0, 3] = 1177305879;
        s_arr2d_0[2, 1] = 1177305779D;
        if ((s_arr2d_0[2, 1]) <= ((int)(s_arr3d_0[4, 0, 3]) % (int)s_arr2d_0[2, 1]))
            Console.WriteLine("Func_0: <= true");
        int retval_0 = (int)(s_arr3d_0[4, 0, 3]) % (int)s_arr2d_0[2, 1];
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
