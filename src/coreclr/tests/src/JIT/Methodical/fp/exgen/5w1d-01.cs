// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
{
    public struct VT_0
    {
        public double[,] arr2d_0;
        public VT_0(int i)
        {
            arr2d_0 = new double[3, 6];
        }
    }

    public static VT_0 vtstatic_0 = new VT_0(1);

    public static int Func_0()
    {
        vtstatic_0.arr2d_0[2, 0] = -2.125;
        vtstatic_0.arr2d_0[2, 2] = -68.0;
        if ((vtstatic_0.arr2d_0[2, 2]) != (vtstatic_0.arr2d_0[2, 0]))
        {
            if (((583855800 * -1.1646711396889438E-07)) != (-1.1646711396889438E-07))
            {
                int if1_0retval_0 = Convert.ToInt32((Convert.ToInt32(((583855800 * -1.1646711396889438E-07) / vtstatic_0.arr2d_0[2, 0]) - (vtstatic_0.arr2d_0[2, 2]))));
                return if1_0retval_0;
            }
        }
        else
        {
            if (((583855800 * -1.1646711396889438E-07)) == (-1.1646711396889438E-07))
            {
                if ((vtstatic_0.arr2d_0[2, 2]) < (vtstatic_0.arr2d_0[2, 0]))
                {
                    return Convert.ToInt32((Convert.ToInt32(((583855800 * -1.1646711396889438E-07) / vtstatic_0.arr2d_0[2, 0]) - (vtstatic_0.arr2d_0[2, 2]))));
                }
                else
                    Console.WriteLine("Func_0: < false");
            }
            else
            {
                if (((583855800 * -1.1646711396889438E-07)) < (-1.1646711396889438E-07))
                    Console.WriteLine("Func_0: < true");
            }
        }
        return Convert.ToInt32((Convert.ToInt32(((583855800 * -1.1646711396889438E-07) / vtstatic_0.arr2d_0[2, 0]) - (vtstatic_0.arr2d_0[2, 2]))));
    }

    public static int Main()
    {
        vtstatic_0.arr2d_0[2, 0] = -2.125;
        vtstatic_0.arr2d_0[2, 2] = -68.0;

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
