// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//testing double narrowing

using System;
using Xunit;

public struct VT
{
    public double f1;
    public double delta1;
    public int a1;
    public double b1;
    public double temp;
}

public class CL
{
    //used for add and sub
    public double f1 = 1.0;
    public double delta1 = 1.0E-18;
    //used for mul and div
    public int a1 = 3;
    public double b1 = (1.0 / 3.0);
    //used as temp variable
    public double temp;
}

public class ConvR8test
{
    //static field of a1 class
    private static double s_f1 = 1.0;
    private static double s_delta1 = 1.0E-18;
    private static int s_a1 = 3;
    private static double s_b1 = (1.0 / 3.0);

    private static void disableInline(ref int x) { }

    //f1 and delta1 are static filed of a1 class
    private static double doubleadd()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 + s_delta1;
    }

    private static double doublesub()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 - s_delta1;
    }

    private static double doublemul()
    {
        int i = 0;
        disableInline(ref i);
        return s_a1 * s_b1;
    }

    private static double doublediv()
    {
        int i = 0;
        disableInline(ref i);
        return s_f1 / s_a1;
    }

    private static double doubleadd_inline()
    {
        return s_f1 + s_delta1;
    }

    private static double doublesub_inline()
    {
        return s_f1 - s_delta1;
    }

    private static double doublemul_inline()
    {
        return s_a1 * s_b1;
    }

    private static double doublediv_inline()
    {
        return s_f1 / s_a1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        double temp;
        double[] arr = new double[3];
        VT vt1;
        CL cl1 = new CL();

        //*** add ***
        Console.WriteLine();
        Console.WriteLine("***add***");

        //local, in-line
        if (((double)(s_f1 + s_delta1)) != s_f1)
        {
            Console.WriteLine("((double)(f1+delta1))!=f1");
            pass = false;
        }

        //local
        temp = s_f1 + s_delta1;
        if (((double)temp) != s_f1)
        {
            Console.WriteLine("((double)temp)!=f1, temp=f1+delta1");
            pass = false;
        }

        //method call
        if (((double)doubleadd()) != s_f1)
        {
            Console.WriteLine("((double)doubleadd())!=f1");
            pass = false;
        }

        //inline method call
        if (((double)doubleadd_inline()) != s_f1)
        {
            Console.WriteLine("((double)doubleadd_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_delta1;
        arr[2] = arr[0] + arr[1];
        if (((double)arr[2]) != s_f1)
        {
            Console.WriteLine("((double)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0;
        vt1.delta1 = 1.0E-18;
        vt1.temp = vt1.f1 + vt1.delta1;
        if (((double)vt1.temp) != s_f1)
        {
            Console.WriteLine("((double)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 + cl1.delta1;
        if (((double)cl1.temp) != s_f1)
        {
            Console.WriteLine("((double)cl1.temp)!=f1");
            pass = false;
        }

        //*** minus ***
        Console.WriteLine();
        Console.WriteLine("***sub***");

        //local, in-line
        if (((double)(s_f1 - s_delta1)) != s_f1)
        {
            Console.WriteLine("((double)(f1-delta1))!=f1");
            pass = false;
        }

        //local
        temp = s_f1 - s_delta1;
        if (((double)temp) != s_f1)
        {
            Console.WriteLine("((double)temp)!=f1, temp=f1-delta1");
            pass = false;
        }

        //method call
        if (((double)doublesub()) != s_f1)
        {
            Console.WriteLine("((double)doublesub())!=f1");
            pass = false;
        }

        //inline method call
        if (((double)doublesub_inline()) != s_f1)
        {
            Console.WriteLine("((double)doublesub_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_delta1;
        arr[2] = arr[0] - arr[1];
        if (((double)arr[2]) != s_f1)
        {
            Console.WriteLine("((double)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0;
        vt1.delta1 = 1.0E-18;
        vt1.temp = vt1.f1 - vt1.delta1;
        if (((double)vt1.temp) != s_f1)
        {
            Console.WriteLine("((double)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 - cl1.delta1;
        if (((double)cl1.temp) != s_f1)
        {
            Console.WriteLine("((double)cl1.temp)!=f1");
            pass = false;
        }

        //*** multiply ***
        Console.WriteLine();
        Console.WriteLine("***mul***");

        //local, in-line
        if (((double)(s_a1 * s_b1)) != s_f1)
        {
            Console.WriteLine("((double)(a1*b1))!=f1");
            pass = false;
        }

        //local
        temp = s_a1 * s_b1;
        if (((double)temp) != s_f1)
        {
            Console.WriteLine("((double)temp)!=f1, temp=a1*b1");
            pass = false;
        }

        //method call
        if (((double)doublemul()) != s_f1)
        {
            Console.WriteLine("((double)doublemul())!=f1");
            pass = false;
        }

        //inline method call
        if (((double)doublemul_inline()) != s_f1)
        {
            Console.WriteLine("((double)doublemul_inline())!=f1");
            pass = false;
        }

        //array element
        arr[0] = s_a1;
        arr[1] = s_b1;
        arr[2] = arr[0] * arr[1];
        if (((double)arr[2]) != s_f1)
        {
            Console.WriteLine("((double)arr[2])!=f1");
            pass = false;
        }

        //struct
        vt1.a1 = 3;
        vt1.b1 = 1.0 / 3.0;
        vt1.temp = vt1.a1 * vt1.b1;
        if (((double)vt1.temp) != s_f1)
        {
            Console.WriteLine("((double)vt1.temp)!=f1");
            pass = false;
        }

        //class
        cl1.temp = cl1.a1 * cl1.b1;
        if (((double)cl1.temp) != s_f1)
        {
            Console.WriteLine("((double)cl1.temp)!=f1");
            pass = false;
        }

        //*** divide ***
        Console.WriteLine();
        Console.WriteLine("***div***");

        //local, in-line
        if (((double)(s_f1 / s_a1)) != s_b1)
        {
            Console.WriteLine("((double)(f1/a1))!=b1");
            pass = false;
        }

        //local
        temp = s_f1 / s_a1;
        if (((double)temp) != s_b1)
        {
            Console.WriteLine("((double)temp)!=f1, temp=f1/a1");
            pass = false;
        }

        //method call
        if (((double)doublediv()) != s_b1)
        {
            Console.WriteLine("((double)doubledivl())!=b1");
            pass = false;
        }

        //method call
        if (((double)doublediv_inline()) != s_b1)
        {
            Console.WriteLine("((double)doublediv_inline())!=b1");
            pass = false;
        }

        //array element
        arr[0] = s_f1;
        arr[1] = s_a1;
        arr[2] = arr[0] / arr[1];
        if (((double)arr[2]) != s_b1)
        {
            Console.WriteLine("((double)arr[2])!=b1");
            pass = false;
        }

        //struct
        vt1.f1 = 1.0;
        vt1.a1 = 3;
        vt1.temp = vt1.f1 / vt1.a1;
        if (((double)vt1.temp) != s_b1)
        {
            Console.WriteLine("((double)vt1.temp)!=b1");
            pass = false;
        }

        //class
        cl1.temp = cl1.f1 / cl1.a1;
        if (((double)cl1.temp) != s_b1)
        {
            Console.WriteLine("((double)cl1.temp)!=b1");
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
            Console.WriteLine("FAILURE: double not truncated properly");
            return 1;
        }
    }
}
