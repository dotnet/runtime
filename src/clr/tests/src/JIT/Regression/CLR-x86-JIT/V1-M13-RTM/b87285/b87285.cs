// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal unsafe class test1
{
    public class CL2
    {
        public int[] arr1d2 = new int[16];
        public double a52 = 4210752.0019569546;
        public double a62 = -15.797483678888883;
    }
    public class CL
    {
        public double a0 = 1.0;
        public double a2 = 0.99999999906867743;
    }
    public static CL2 clstatic2 = new CL2();

    public static CL clstatic = new CL();

    public static double Func2(CL2 cl2)
    {
        clstatic2.arr1d2[0] = 1048576;
        double retval2 = Convert.ToDouble(((((clstatic2.arr1d2[0] - 0) / 4210752.2509803921) * ((4210752.2509803921 + 0.0 - cl2.a52) * -0.015747789311803151)) - (-0.015747789311803151 * (-0.015747789311803151 + cl2.a62))) - ((clstatic2.arr1d2[0] / (cl2.a62 + 4210768.048464071)) - clstatic2.arr1d2[0] / -2093063.9766233866));
        return retval2;
    }
    public static double Func(double* a3)
    {
        double val_3 = -1.00000000186265;
        CL2 cl2 = new CL2();
        double val2 = Func2(cl2);
        Console.WriteLine("The expected result is -1.00000000186265");
        Console.WriteLine("The actual result is {0}", val2);
        double val_1 = -1.00000000186265;
        double retval = Convert.ToDouble((-1.0000000018626452 + *a3 + val_1) * clstatic.a0 + clstatic.a0 + val2 - val_3 + 0.0 + clstatic.a2);
        return retval;
    }
    public static int Main()
    {
        double* a3 = stackalloc double[1];
        *a3 = 2.0000000018626451;
        double val = Func(a3);
        return 100;
    }
}
