// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal class bug1
{
    public struct VT
    {
        public int a3;
    }
    public class CL
    {
        public float a2 = 4.0F;
        public long a5 = 2L;
    }
    public static VT vtstatic = new VT();

    public static int Main()
    {
        CL cl = new CL();
        float[] arr1d = new float[11];
        double a4 = 3;
        arr1d[0] = 4F;
        long a1 = 6L;
        vtstatic.a3 = 13;

        Console.WriteLine("The correct result is 1");
        Console.Write("The actual result is ");
        int retval = Convert.ToInt32(Convert.ToInt32((long)(Convert.ToInt32(vtstatic.a3) + (long)(18L / cl.a5 / 3)) / (cl.a5 * a1 / a4) + (cl.a2 / arr1d[0] - cl.a2)));
        Console.WriteLine(retval);
        return retval + 99;
    }
}
