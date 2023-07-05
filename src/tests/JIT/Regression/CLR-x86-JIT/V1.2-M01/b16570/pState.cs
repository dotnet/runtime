// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public class test
{
    public struct VT
    {
        public double a1;
        public float a4;
        public VT(int i)
        {
            a1 = 1;
            a4 = 1;
        }
    }

    public class CL
    {
        public double[,,] arr3d = new double[5, 11, 4];
        public float a5 = -0.001953125F;
    }

    public static VT vtstatic = new VT(1);
    public static CL clstatic = new CL();

    public static double Func()
    {
        VT vt = new VT(1);
        vt.a1 = -2.0386427882503781E-07;
        vt.a4 = 0.5F;
        CL cl = new CL();

        vtstatic.a1 = -2.0386427882503781E-07;
        vtstatic.a4 = 0.5F;
        cl.arr3d[4, 0, 3] = 1.1920928955078125E-07;
        float asgop0 = vtstatic.a4;
        asgop0 -= (0.484375F);
        if (((vtstatic.a4 * clstatic.a5)) <= (vtstatic.a4))
        {
            return Convert.ToDouble((((vtstatic.a4 * clstatic.a5) + (asgop0 - (0.25F - 0.235290527F))) / (cl.arr3d[4, 0, 3] - (vt.a1))));
        }
        else
        {
            if ((cl.arr3d[4, 0, 3]) < ((((vtstatic.a4 * clstatic.a5) + (asgop0 - (0.25F - 0.235290527F))) / (cl.arr3d[4, 0, 3] - (vt.a1)))))
            {
                double if0_1retval = Convert.ToDouble((((vtstatic.a4 * clstatic.a5) + (asgop0 - (0.25F - 0.235290527F))) / (cl.arr3d[4, 0, 3] - (vt.a1))));
                return if0_1retval;
            }
        }
        return Convert.ToDouble((((vtstatic.a4 * clstatic.a5) + (asgop0 - (0.25F - 0.235290527F))) / (cl.arr3d[4, 0, 3] - (vt.a1))));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        double retval = Func();
        if ((retval > -191) && (retval < -188))
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            Console.WriteLine(retval);
            return 1;
        }
    }
}
