// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_r8_cs
{
public class test
{
    private static double f00(double x, double y)
    {
        x = x + y;
        return x;
    }

    private static double f01(double x, double y)
    {
        x = x - y;
        return x;
    }

    private static double f02(double x, double y)
    {
        x = x * y;
        return x;
    }

    private static double f03(double x, double y)
    {
        x = x / y;
        return x;
    }

    private static double f04(double x, double y)
    {
        x = x % y;
        return x;
    }

    private static double f10(double x, double y)
    {
        x += x + y;
        return x;
    }

    private static double f11(double x, double y)
    {
        x += x - y;
        return x;
    }

    private static double f12(double x, double y)
    {
        x += x * y;
        return x;
    }

    private static double f13(double x, double y)
    {
        x += x / y;
        return x;
    }

    private static double f14(double x, double y)
    {
        x += x % y;
        return x;
    }

    private static double f20(double x, double y)
    {
        x -= x + y;
        return x;
    }

    private static double f21(double x, double y)
    {
        x -= x - y;
        return x;
    }

    private static double f22(double x, double y)
    {
        x -= x * y;
        return x;
    }

    private static double f23(double x, double y)
    {
        x -= x / y;
        return x;
    }

    private static double f24(double x, double y)
    {
        x -= x % y;
        return x;
    }

    private static double f30(double x, double y)
    {
        x *= x + y;
        return x;
    }

    private static double f31(double x, double y)
    {
        x *= x - y;
        return x;
    }

    private static double f32(double x, double y)
    {
        x *= x * y;
        return x;
    }

    private static double f33(double x, double y)
    {
        x *= x / y;
        return x;
    }

    private static double f34(double x, double y)
    {
        x *= x % y;
        return x;
    }

    private static double f40(double x, double y)
    {
        x /= x + y;
        return x;
    }

    private static double f41(double x, double y)
    {
        x /= x - y;
        return x;
    }

    private static double f42(double x, double y)
    {
        x /= x * y;
        return x;
    }

    private static double f43(double x, double y)
    {
        x /= x / y;
        return x;
    }

    private static double f44(double x, double y)
    {
        x /= x % y;
        return x;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        double x;
        bool pass = true;

        x = f00(-10.0, 4.0);
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f00	x =	x + y;	failed.\nx: {0} \texpected: -6\n", x);
            pass = false;
        }

        x = f01(-10.0, 4.0);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f01	x =	x - y;	failed.\nx: {0} \texpected: -14\n", x);
            pass = false;
        }

        x = f02(-10.0, 4.0);
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f02	x =	x * y;	failed.\nx: {0} \texpected: -40\n", x);
            pass = false;
        }

        x = f03(-10.0, 4.0);
        if (x != -2.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f03	x =	x / y;	failed.\nx: {0} \texpected: -2.5\n", x);
            pass = false;
        }

        x = f04(-10.0, 4.0);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f04	x =	x % y;	failed.\nx: {0} \texpected: -2\n", x);
            pass = false;
        }

        x = f10(-10.0, 4.0);
        if (x != -16)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f10	x +=	x + y;	failed.\nx: {0} \texpected: -16\n", x);
            pass = false;
        }

        x = f11(-10.0, 4.0);
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f11	x +=	x - y;	failed.\nx: {0} \texpected: -24\n", x);
            pass = false;
        }

        x = f12(-10.0, 4.0);
        if (x != -50)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f12	x +=	x * y;	failed.\nx: {0} \texpected: -50\n", x);
            pass = false;
        }

        x = f13(-10.0, 4.0);
        if (x != -12.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f13	x +=	x / y;	failed.\nx: {0} \texpected: -12.5\n", x);
            pass = false;
        }

        x = f14(-10.0, 4.0);
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f14	x +=	x % y;	failed.\nx: {0} \texpected: -12\n", x);
            pass = false;
        }

        x = f20(-10.0, 4.0);
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f20	x -=	x + y;	failed.\nx: {0} \texpected: -4\n", x);
            pass = false;
        }

        x = f21(-10.0, 4.0);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f21	x -=	x - y;	failed.\nx: {0} \texpected: 4\n", x);
            pass = false;
        }

        x = f22(-10.0, 4.0);
        if (x != 30)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f22	x -=	x * y;	failed.\nx: {0} \texpected: 30\n", x);
            pass = false;
        }

        x = f23(-10.0, 4.0);
        if (x != -7.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f23	x -=	x / y;	failed.\nx: {0} \texpected: -7.5\n", x);
            pass = false;
        }

        x = f24(-10.0, 4.0);
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f24	x -=	x % y;	failed.\nx: {0} \texpected: -8\n", x);
            pass = false;
        }

        x = f30(-10.0, 4.0);
        if (x != 60)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f30	x *=	x + y;	failed.\nx: {0} \texpected: 60\n", x);
            pass = false;
        }

        x = f31(-10.0, 4.0);
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f31	x *=	x - y;	failed.\nx: {0} \texpected: 140\n", x);
            pass = false;
        }

        x = f32(-10.0, 4.0);
        if (x != 400)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f32	x *=	x * y;	failed.\nx: {0} \texpected: 400\n", x);
            pass = false;
        }

        x = f33(-10.0, 4.0);
        if (x != 25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f33	x *=	x / y;	failed.\nx: {0} \texpected: 25\n", x);
            pass = false;
        }

        x = f34(-10.0, 4.0);
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f34	x *=	x % y;	failed.\nx: {0} \texpected: 20\n", x);
            pass = false;
        }

        x = f40(-10.0, 4.0);
        if (!x.Equals(1.6666666666666667D))
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f40	x /=	x + y;	failed.\nx: {0} \texpected: 1.6666666666666667\n", x);
            pass = false;
        }

        x = f41(-10.0, 4.0);
        if (!x.Equals(0.7142857142857143))
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f41	x /=	x - y;	failed.\nx: {0} \texpected: 0.7142857142857143\n", x);
            pass = false;
        }

        x = f42(-10.0, 4.0);
        if (x != 0.25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f42	x /=	x * y;	failed.\nx: {0} \texpected: 0.25\n", x);
            pass = false;
        }

        x = f43(-10.0, 4.0);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f43	x /=	x / y;	failed.\nx: {0} \texpected: 4\n", x);
            pass = false;
        }

        x = f44(-10.0, 4.0);
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0 and y is 4.0");
            Console.WriteLine("f44	x /=	x % y;	failed.\nx: {0} \texpected: 5\n", x);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
            return 1;
    }
}
}
