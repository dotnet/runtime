// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
internal struct VT
{
    public float one;
    public double delta;
    public double temp;
}
public class loop6
{
    public static int cnt;

    public static float sone;
    public static double sdelta;
    public static double stemp;

    internal static void f1()
    {
        float one = 1.0F;
        double delta = 1.0D;
        double temp = 0.0D;
        while (temp != one)
        {
            temp = one + delta;
            delta = delta / 2.0F;
        }
        if ((delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f1 passed");
        }
        else
            System.Console.WriteLine("f1 failed");
    }

    internal static void f2()
    {
        float one = 1.0F;
        double delta = 1.0D;
        double temp = 0.0D;
        while (temp != one)
        {
            temp = one + delta;
            delta = delta / 2.0F;
        }
        if ((delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f2 passed");
        }
        else
            System.Console.WriteLine("f2 failed");
    }

    internal static void f3()
    {
        double temp = 0.0D;
        float one = 1.0F;
        double delta = 1.0D;
        while (temp != one)
        {
            temp = one + delta;
            delta = delta / 2.0F;
        }
        if ((delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f3 passed");
        }
        else
            System.Console.WriteLine("f3 failed");
    }

    internal static void f4()
    {
        float one = 1.0F;
        double delta = 1.0D;
        double temp = 0.0D;
        temp = one + delta;
        while (temp > one)
        {
            temp = one + delta;
            delta = delta / 2.0F;
        }
        if ((delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f4 passed");
        }
        else
            System.Console.WriteLine("f4 failed");
    }

    internal static void f5()
    {
        sone = 1.0F;
        sdelta = 1.0D;
        stemp = 0.0D;
        while (stemp != sone)
        {
            stemp = sone + sdelta;
            sdelta = sdelta / 2.0F;
        }
        if ((sdelta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f5 passed");
        }
        else
            System.Console.WriteLine("f5 failed");
    }
    internal static void f6()
    {
        VT vt;
        vt.one = 1.0F;
        vt.delta = 1.0D;
        vt.temp = 0.0D;
        while (vt.temp != vt.one)
        {
            vt.temp = vt.one + vt.delta;
            vt.delta = vt.delta / 2.0F;
        }
        if ((vt.delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f6 passed");
        }
        else
            System.Console.WriteLine("f6 failed");
    }

    internal static void f7()
    {
        float one = 1.0F;
        double delta = 1.0D;
        double temp = 0.0D;
        temp = one + delta;
        while (-temp < -one)
        {
            temp = one + delta;
            delta = delta * 0.5F;
        }
        if ((delta - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f7 passed");
        }
        else
            System.Console.WriteLine("f7 failed");
    }

    internal static void f8()
    {
        float one = 1.0F;
        double delta = 1.0D;
        double temp = 0.0D;

        TypedReference one_ref = __makeref(one);
        TypedReference delta_ref = __makeref(delta);
        TypedReference temp_ref = __makeref(temp);

        while (__refvalue(temp_ref, double) != __refvalue(one_ref, float))
        {
            __refvalue(temp_ref, double) = __refvalue(one_ref, float) + __refvalue(delta_ref, double);
            __refvalue(delta_ref, double) = __refvalue(delta_ref, double) / 2.0F;
        }
        if ((__refvalue(delta_ref, double) - 5.551115E-17) < 1.2E-10)
        {
            cnt++;
            System.Console.WriteLine("f8 passed");
        }
        else
            System.Console.WriteLine("f8 failed");
    }

    [Fact]
    public static int TestEntryPoint()
    {
        cnt = 0;
        f1();
        f2();
        f3();
        f4();
        f5();
        f6();
        f7();
        f8();
        if (cnt == 8)
            return 100;
        else
            return 1;
    }
}
