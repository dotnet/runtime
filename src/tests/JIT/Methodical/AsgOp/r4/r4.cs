// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_r4_cs
{
public class test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f00(float x, float y)
    {
        x = x + y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f01(float x, float y)
    {
        x = x - y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f02(float x, float y)
    {
        x = x * y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f03(float x, float y)
    {
        x = x / y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f04(float x, float y)
    {
        x = x % y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f10(float x, float y)
    {
        x += x + y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f11(float x, float y)
    {
        x += x - y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f12(float x, float y)
    {
        x += x * y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f13(float x, float y)
    {
        x += x / y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f14(float x, float y)
    {
        x += x % y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f20(float x, float y)
    {
        x -= x + y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f21(float x, float y)
    {
        x -= x - y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f22(float x, float y)
    {
        x -= x * y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f23(float x, float y)
    {
        x -= x / y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f24(float x, float y)
    {
        x -= x % y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f30(float x, float y)
    {
        x *= x + y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f31(float x, float y)
    {
        x *= x - y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f32(float x, float y)
    {
        x *= x * y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f33(float x, float y)
    {
        x *= x / y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f34(float x, float y)
    {
        x *= x % y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f40(float x, float y)
    {
        x /= x + y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f41(float x, float y)
    {
        x /= x - y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f42(float x, float y)
    {
        x /= x * y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f43(float x, float y)
    {
        x /= x / y;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float f44(float x, float y)
    {
        x /= x % y;
        return x;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        float x;
        bool pass = true;

        x = f00(-10.0F, 4.0F);
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f00	x =	x + y;	failed.\nx: {0} \texpected: -6\n", x);
            pass = false;
        }

        x = f01(-10.0F, 4.0F);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f01	x =	x - y;	failed.\nx: {0} \texpected: -14\n", x);
            pass = false;
        }

        x = f02(-10.0F, 4.0F);
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f02	x =	x * y;	failed.\nx: {0} \texpected: -40\n", x);
            pass = false;
        }

        x = f03(-10.0F, 4.0F);
        if (x != -2.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f03	x =	x / y;	failed.\nx: {0} \texpected: -2.5\n", x);
            pass = false;
        }

        x = f04(-10.0F, 4.0F);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f04	x =	x % y;	failed.\nx: {0} \texpected: -2\n", x);
            pass = false;
        }

        x = f10(-10.0F, 4.0F);
        if (x != -16)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f10	x +=	x + y;	failed.\nx: {0} \texpected: -16\n", x);
            pass = false;
        }

        x = f11(-10.0F, 4.0F);
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f11	x +=	x - y;	failed.\nx: {0} \texpected: -24\n", x);
            pass = false;
        }

        x = f12(-10.0F, 4.0F);
        if (x != -50)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f12	x +=	x * y;	failed.\nx: {0} \texpected: -50\n", x);
            pass = false;
        }

        x = f13(-10.0F, 4.0F);
        if (x != -12.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f13	x +=	x / y;	failed.\nx: {0} \texpected: -12.5\n", x);
            pass = false;
        }

        x = f14(-10.0F, 4.0F);
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f14	x +=	x % y;	failed.\nx: {0} \texpected: -12\n", x);
            pass = false;
        }

        x = f20(-10.0F, 4.0F);
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f20	x -=	x + y;	failed.\nx: {0} \texpected: -4\n", x);
            pass = false;
        }

        x = f21(-10.0F, 4.0F);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f21	x -=	x - y;	failed.\nx: {0} \texpected: 4\n", x);
            pass = false;
        }

        x = f22(-10.0F, 4.0F);
        if (x != 30)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f22	x -=	x * y;	failed.\nx: {0} \texpected: 30\n", x);
            pass = false;
        }

        x = f23(-10.0F, 4.0F);
        if (x != -7.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f23	x -=	x / y;	failed.\nx: {0} \texpected: -7.5\n", x);
            pass = false;
        }

        x = f24(-10.0F, 4.0F);
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f24	x -=	x % y;	failed.\nx: {0} \texpected: -8\n", x);
            pass = false;
        }

        x = f30(-10.0F, 4.0F);
        if (x != 60)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f30	x *=	x + y;	failed.\nx: {0} \texpected: 60\n", x);
            pass = false;
        }

        x = f31(-10.0F, 4.0F);
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f31	x *=	x - y;	failed.\nx: {0} \texpected: 140\n", x);
            pass = false;
        }

        x = f32(-10.0F, 4.0F);
        if (x != 400)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f32	x *=	x * y;	failed.\nx: {0} \texpected: 400\n", x);
            pass = false;
        }

        x = f33(-10.0F, 4.0F);
        if (x != 25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f33	x *=	x / y;	failed.\nx: {0} \texpected: 25\n", x);
            pass = false;
        }

        x = f34(-10.0F, 4.0F);
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f34	x *=	x % y;	failed.\nx: {0} \texpected: 20\n", x);
            pass = false;
        }

        x = f40(-10.0F, 4.0F);
        if ((x - 1.66666663F) > Single.Epsilon)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f40	x /=	x + y;	failed.\nx: {0} \texpected: 1.66666663F\n", x);
            pass = false;
        }

        x = f41(-10.0F, 4.0F);
        if (!x.Equals(0.714285731F))
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f41	x /=	x - y;	failed.\nx: {0} \texpected: 0.714285731F\n", x);
            pass = false;
        }

        x = f42(-10.0F, 4.0F);
        if (x != 0.25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f42	x /=	x * y;	failed.\nx: {0} \texpected: 0.25\n", x);
            pass = false;
        }

        x = f43(-10.0F, 4.0F);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("f43	x /=	x / y;	failed.\nx: {0} \texpected: 4\n", x);
            pass = false;
        }

        x = f44(-10.0F, 4.0F);
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
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
