// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//testing float narrowing upon conv.r4 explicit cast

using System;
using Xunit;

public struct VT
{
    public float f1;
    public float delta1;
    public int a1;
    public float b1;
    public float temp;
}

public class CL
{
    //used for add and sub
    public float f1 = 1.0F;
    public float delta1 = 1.0E-10F;
    //used for mul and div
    public int a1 = 3;
    public float b1 = (1.0F / 3.0F);
    //used as temp variable
    public float temp;
}

public class ConvR4test
{
    //static field of a1 class
    private static float s_f1 = 1.0F;
    private static float s_delta1 = 1.0E-10F;
    private static int s_a1 = 3;
    private static float s_b1 = (1.0F / 3.0F);

    private static void disableInline(ref int x) { }

    //f1 and delta1 are static filed of a1 class
    private static float floatadd()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 + s_delta1;
    }

    private static float floatsub()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 - s_delta1;
    }

    private static float floatmul()
    {
        int i = 0;
        disableInline(ref i);
        return s_a1 * s_b1;
    }

    private static float floatdiv()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 / s_a1;
    }

    private static float floatadd_inline()
    {
        return s_f1 + s_delta1;
    }

    private static float floatsub_inline()
    {
        return s_f1 - s_delta1;
    }

    private static float floatmul_inline()
    {
        return s_a1 * s_b1;
    }

    private static float floatdiv_inline()
    {
        return s_f1 / s_a1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        float temp;
        float[] arr = new float[3];
        VT vt1;
        CL cl1 = new CL();

        //*** add ***
        Console.WriteLine();
        Console.WriteLine("***add***");

        //local, in-line
        if (((float)(s_f1 + s_delta1)) != s_f1)
        {
            Console.WriteLine("((float)(f1+delta1))!=f1");
            pass = false;
        }

        //local
        temp = s_f1 + s_delta1;
        if (((float)temp) != s_f1)
        {
            Console.WriteLine("((float)temp)!=f1, temp=f1+delta1");
            pass = false;
        }

        //method call
        if (((float)floatadd()) != s_f1)
        {
            Console.WriteLine("((float)floatadd())!=f1");
            pass = false;
        }

        //inline method call
        if (((float)floatadd_inline()) != s_f1)
        {
            Console.WriteLine("((float)floatadd_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_delta1;
        arr[2] = arr[0] + arr[1];
        if (((float)arr[2]) != s_f1)
        {
            Console.WriteLine("((float)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0F;
        vt1.delta1 = 1.0E-10F;
        vt1.temp = vt1.f1 + vt1.delta1;
        if (((float)vt1.temp) != s_f1)
        {
            Console.WriteLine("((float)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 + cl1.delta1;
        if (((float)cl1.temp) != s_f1)
        {
            Console.WriteLine("((float)cl1.temp)!=f1");
            pass = false;
        }

        //*** minus ***
        Console.WriteLine();
        Console.WriteLine("***sub***");

        //local, in-line
        if (((float)(s_f1 - s_delta1)) != s_f1)
        {
            Console.WriteLine("((float)(f1-delta1))!=f1");
            pass = false;
        }

        //local
        temp = s_f1 - s_delta1;
        if (((float)temp) != s_f1)
        {
            Console.WriteLine("((float)temp)!=f1, temp=f1-delta1");
            pass = false;
        }

        //method call
        if (((float)floatsub()) != s_f1)
        {
            Console.WriteLine("((float)floatsub())!=f1");
            pass = false;
        }

        //inline method call
        if (((float)floatsub_inline()) != s_f1)
        {
            Console.WriteLine("((float)floatsub_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_delta1;
        arr[2] = arr[0] - arr[1];
        if (((float)arr[2]) != s_f1)
        {
            Console.WriteLine("((float)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0F;
        vt1.delta1 = 1.0E-10F;
        vt1.temp = vt1.f1 - vt1.delta1;
        if (((float)vt1.temp) != s_f1)
        {
            Console.WriteLine("((float)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 - cl1.delta1;
        if (((float)cl1.temp) != s_f1)
        {
            Console.WriteLine("((float)cl1.temp)!=f1");
            pass = false;
        }

        //*** multiply ***
        Console.WriteLine();
        Console.WriteLine("***mul***");

        //local, in-line
        if (((float)(s_a1 * s_b1)) != s_f1)
        {
            Console.WriteLine("((float)(a1*b1))!=f1");
            pass = false;
        }

        //local
        temp = s_a1 * s_b1;
        if (((float)temp) != s_f1)
        {
            Console.WriteLine("((float)temp)!=f1, temp=a1*b1");
            pass = false;
        }

        //method call
        if (((float)floatmul()) != s_f1)
        {
            Console.WriteLine("((float)floatmul())!=f1");
            pass = false;
        }

        //inline method call
        if (((float)floatmul_inline()) != s_f1)
        {
            Console.WriteLine("((float)floatmul_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_a1;
        arr[1] = s_b1;
        arr[2] = arr[0] * arr[1];
        if (((float)arr[2]) != s_f1)
        {
            Console.WriteLine("((float)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.a1 = 3;
        vt1.b1 = 1.0F / 3.0F;
        vt1.temp = vt1.a1 * vt1.b1;
        if (((float)vt1.temp) != s_f1)
        {
            Console.WriteLine("((float)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.a1 * cl1.b1;
        if (((float)cl1.temp) != s_f1)
        {
            Console.WriteLine("((float)cl1.temp)!=f1");
            pass = false;
        }

        //*** divide ***
        Console.WriteLine();
        Console.WriteLine("***div***");

        //local, in-line
        if (((float)(s_f1 / s_a1)) != s_b1)
        {
            Console.WriteLine("((float)(f1/a1))!=b1");
            pass = false;
        }

        //local
        temp = s_f1 / s_a1;
        if (((float)temp) != s_b1)
        {
            Console.WriteLine("((float)temp)!=f1, temp=f1/a1");
            pass = false;
        }

        //method call
        if (((float)floatdiv()) != s_b1)
        {
            Console.WriteLine("((float)floatdivl())!=b1");
            pass = false;
        }

        //method call
        if (((float)floatdiv_inline()) != s_b1)
        {
            Console.WriteLine("((float)floatdiv_inline())!=b1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_a1;
        arr[2] = arr[0] / arr[1];
        if (((float)arr[2]) != s_b1)
        {
            Console.WriteLine("((float)arr[2])!=b1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0F;
        vt1.a1 = 3;
        vt1.temp = vt1.f1 / vt1.a1;
        if (((float)vt1.temp) != s_b1)
        {
            Console.WriteLine("((float)vt1.temp)!=b1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 / cl1.a1;
        if (((float)cl1.temp) != s_b1)
        {
            Console.WriteLine("((float)cl1.temp)!=b1");
            pass = false;
        }

        Console.WriteLine();
        if (pass)
        {
            Console.WriteLine("SUCCESS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILURE: float not truncated properly");
            return 1;
        }
    }
}
