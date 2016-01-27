// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
class test1
{
    public struct VT
    {
        public short a1;
        public double a5;
    }
    static float[, ,] arr3d = new float[5, 20, 4];
    public static double Func(VT vt, short a4, double a6)
    {
        arr3d[4, 0, 3] = 16.0F;
        double retval = Convert.ToDouble(Convert.ToInt16(Convert.ToInt16(a4 / Convert.ToSingle(-1.582702F)) % Convert.ToInt16(vt.a1 * Convert.ToSingle(Convert.ToInt64(16L / 4L) * 0.12312290072441101))) / ((Convert.ToInt32(arr3d[4, 0, 3] + Convert.ToSingle(4UL * vt.a5)) / (vt.a5 + Convert.ToDouble(Convert.ToUInt64(4UL - 0UL) * -62.99951171875))) + (a6 - (a6 + arr3d[4, 0, 3] / -2.68274906263726E-07))));
        return retval;
    }
    public static int Main()
    {
        VT vt = new VT();
        vt.a1 = 23840;
        vt.a5 = 252.0;
        short a4 = 31548;
        double a6 = 0.001953125;
        double val = Func(vt, a4, a6);
        Console.WriteLine("The expected result is -0.000136159794114324");
        Console.WriteLine("The actual result is {0}", val);
        return 100;
    }
}



