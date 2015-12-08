// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
{
    private static float[] s_arr1d_0 = new float[6];


    public static int Func_0()
    {
        float* a2_0 = stackalloc float[1];
        *a2_0 = 0.0F;

        s_arr1d_0[0] = -3996.0F;
        float asgop0 = 2048.0F;
        asgop0 += (s_arr1d_0[0]);
        return Convert.ToInt32((2048.0F - ((*a2_0))) + asgop0);
    }

    public static int Main()
    {
        s_arr1d_0[0] = -3996.0F;

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
