// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
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
