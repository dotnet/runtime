// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
class bug1
{
    public struct VT
    {
        public double a1;
        public long a2;
        public long a3;
    }

    public class CL
    {
        public double[] arr1d = new double[11];
    }

    public static VT vtstatic = new VT();

    public static int Main()
    {
        double a5 = -0.5;

        VT vt = new VT();
        vt.a1 = 6;
        vt.a2 = 3L;
        vt.a3 = 1L;

        CL cl = new CL();
        cl.arr1d[0] = 0.2;

        vtstatic.a1 = 9;
        vtstatic.a2 = 0L;
        vtstatic.a3 = 1L;
        long retval = (long)(Convert.ToInt32((Convert.ToInt32(((double)(vtstatic.a3 * (vt.a1 - cl.arr1d[0]))) - (vt.a1 - (a5))))) - (long)(((vtstatic.a3 + vtstatic.a2) + (vtstatic.a3 + 5L))));
        Console.WriteLine("The correct value is -8");
        Console.WriteLine("The actual value is {0}", retval);
        return 100;
    }

}
