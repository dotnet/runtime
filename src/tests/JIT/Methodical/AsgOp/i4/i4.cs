// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_i4_cs
{
public class test
{
    private static int f00(int x, int y)
    {
        x = x + y;
        return x;
    }

    private static int f01(int x, int y)
    {
        x = x - y;
        return x;
    }

    private static int f02(int x, int y)
    {
        x = x * y;
        return x;
    }

    private static int f03(int x, int y)
    {
        x = x / y;
        return x;
    }

    private static int f04(int x, int y)
    {
        x = x % y;
        return x;
    }

    private static int f05(int x, int y)
    {
        x = x << y;
        return x;
    }

    private static int f06(int x, int y)
    {
        x = x >> y;
        return x;
    }

    private static int f07(int x, int y)
    {
        x = x & y;
        return x;
    }

    private static int f08(int x, int y)
    {
        x = x ^ y;
        return x;
    }

    private static int f09(int x, int y)
    {
        x = x | y;
        return x;
    }

    private static int f10(int x, int y)
    {
        x += x + y;
        return x;
    }

    private static int f11(int x, int y)
    {
        x += x - y;
        return x;
    }

    private static int f12(int x, int y)
    {
        x += x * y;
        return x;
    }

    private static int f13(int x, int y)
    {
        x += x / y;
        return x;
    }

    private static int f14(int x, int y)
    {
        x += x % y;
        return x;
    }

    private static int f15(int x, int y)
    {
        x += x << y;
        return x;
    }

    private static int f16(int x, int y)
    {
        x += x >> y;
        return x;
    }

    private static int f17(int x, int y)
    {
        x += x & y;
        return x;
    }

    private static int f18(int x, int y)
    {
        x += x ^ y;
        return x;
    }

    private static int f19(int x, int y)
    {
        x += x | y;
        return x;
    }

    private static int f20(int x, int y)
    {
        x -= x + y;
        return x;
    }

    private static int f21(int x, int y)
    {
        x -= x - y;
        return x;
    }

    private static int f22(int x, int y)
    {
        x -= x * y;
        return x;
    }

    private static int f23(int x, int y)
    {
        x -= x / y;
        return x;
    }

    private static int f24(int x, int y)
    {
        x -= x % y;
        return x;
    }

    private static int f25(int x, int y)
    {
        x -= x << y;
        return x;
    }

    private static int f26(int x, int y)
    {
        x -= x >> y;
        return x;
    }

    private static int f27(int x, int y)
    {
        x -= x & y;
        return x;
    }

    private static int f28(int x, int y)
    {
        x -= x ^ y;
        return x;
    }

    private static int f29(int x, int y)
    {
        x -= x | y;
        return x;
    }

    private static int f30(int x, int y)
    {
        x *= x + y;
        return x;
    }

    private static int f31(int x, int y)
    {
        x *= x - y;
        return x;
    }

    private static int f32(int x, int y)
    {
        x *= x * y;
        return x;
    }

    private static int f33(int x, int y)
    {
        x *= x / y;
        return x;
    }

    private static int f34(int x, int y)
    {
        x *= x % y;
        return x;
    }

    private static int f35(int x, int y)
    {
        x *= x << y;
        return x;
    }

    private static int f36(int x, int y)
    {
        x *= x >> y;
        return x;
    }

    private static int f37(int x, int y)
    {
        x *= x & y;
        return x;
    }

    private static int f38(int x, int y)
    {
        x *= x ^ y;
        return x;
    }

    private static int f39(int x, int y)
    {
        x *= x | y;
        return x;
    }

    private static int f40(int x, int y)
    {
        x /= x + y;
        return x;
    }

    private static int f41(int x, int y)
    {
        x /= x - y;
        return x;
    }

    private static int f42(int x, int y)
    {
        x /= x * y;
        return x;
    }

    private static int f43(int x, int y)
    {
        x /= x / y;
        return x;
    }

    private static int f44(int x, int y)
    {
        x /= x % y;
        return x;
    }

    private static int f45(int x, int y)
    {
        x /= x << y;
        return x;
    }

    private static int f46(int x, int y)
    {
        x /= x >> y;
        return x;
    }

    private static int f47(int x, int y)
    {
        x /= x & y;
        return x;
    }

    private static int f48(int x, int y)
    {
        x /= x ^ y;
        return x;
    }

    private static int f49(int x, int y)
    {
        x /= x | y;
        return x;
    }

    private static int f50(int x, int y)
    {
        x %= x + y;
        return x;
    }

    private static int f51(int x, int y)
    {
        x %= x - y;
        return x;
    }

    private static int f52(int x, int y)
    {
        x %= x * y;
        return x;
    }

    private static int f53(int x, int y)
    {
        x %= x / y;
        return x;
    }

    private static int f54(int x, int y)
    {
        x %= x % y;
        return x;
    }

    private static int f55(int x, int y)
    {
        x %= x << y;
        return x;
    }

    private static int f56(int x, int y)
    {
        x %= x >> y;
        return x;
    }

    private static int f57(int x, int y)
    {
        x %= x & y;
        return x;
    }

    private static int f58(int x, int y)
    {
        x %= x ^ y;
        return x;
    }

    private static int f59(int x, int y)
    {
        x %= x | y;
        return x;
    }

    private static int f60(int x, int y)
    {
        x <<= x + y;
        return x;
    }

    private static int f61(int x, int y)
    {
        x <<= x - y;
        return x;
    }

    private static int f62(int x, int y)
    {
        x <<= x * y;
        return x;
    }

    private static int f63(int x, int y)
    {
        x <<= x / y;
        return x;
    }

    private static int f64(int x, int y)
    {
        x <<= x % y;
        return x;
    }

    private static int f65(int x, int y)
    {
        x <<= x << y;
        return x;
    }

    private static int f66(int x, int y)
    {
        x <<= x >> y;
        return x;
    }

    private static int f67(int x, int y)
    {
        x <<= x & y;
        return x;
    }

    private static int f68(int x, int y)
    {
        x <<= x ^ y;
        return x;
    }

    private static int f69(int x, int y)
    {
        x <<= x | y;
        return x;
    }

    private static int f70(int x, int y)
    {
        x >>= x + y;
        return x;
    }

    private static int f71(int x, int y)
    {
        x >>= x - y;
        return x;
    }

    private static int f72(int x, int y)
    {
        x >>= x * y;
        return x;
    }

    private static int f73(int x, int y)
    {
        x >>= x / y;
        return x;
    }

    private static int f74(int x, int y)
    {
        x >>= x % y;
        return x;
    }

    private static int f75(int x, int y)
    {
        x >>= x << y;
        return x;
    }

    private static int f76(int x, int y)
    {
        x >>= x >> y;
        return x;
    }

    private static int f77(int x, int y)
    {
        x >>= x & y;
        return x;
    }

    private static int f78(int x, int y)
    {
        x >>= x ^ y;
        return x;
    }

    private static int f79(int x, int y)
    {
        x >>= x | y;
        return x;
    }

    private static int f80(int x, int y)
    {
        x &= x + y;
        return x;
    }

    private static int f81(int x, int y)
    {
        x &= x - y;
        return x;
    }

    private static int f82(int x, int y)
    {
        x &= x * y;
        return x;
    }

    private static int f83(int x, int y)
    {
        x &= x / y;
        return x;
    }

    private static int f84(int x, int y)
    {
        x &= x % y;
        return x;
    }

    private static int f85(int x, int y)
    {
        x &= x << y;
        return x;
    }

    private static int f86(int x, int y)
    {
        x &= x >> y;
        return x;
    }

    private static int f87(int x, int y)
    {
        x &= x & y;
        return x;
    }

    private static int f88(int x, int y)
    {
        x &= x ^ y;
        return x;
    }

    private static int f89(int x, int y)
    {
        x &= x | y;
        return x;
    }

    private static int f90(int x, int y)
    {
        x ^= x + y;
        return x;
    }

    private static int f91(int x, int y)
    {
        x ^= x - y;
        return x;
    }

    private static int f92(int x, int y)
    {
        x ^= x * y;
        return x;
    }

    private static int f93(int x, int y)
    {
        x ^= x / y;
        return x;
    }

    private static int f94(int x, int y)
    {
        x ^= x % y;
        return x;
    }

    private static int f95(int x, int y)
    {
        x ^= x << y;
        return x;
    }

    private static int f96(int x, int y)
    {
        x ^= x >> y;
        return x;
    }

    private static int f97(int x, int y)
    {
        x ^= x & y;
        return x;
    }

    private static int f98(int x, int y)
    {
        x ^= x ^ y;
        return x;
    }

    private static int f99(int x, int y)
    {
        x ^= x | y;
        return x;
    }

    private static int f100(int x, int y)
    {
        x |= x + y;
        return x;
    }

    private static int f101(int x, int y)
    {
        x |= x - y;
        return x;
    }

    private static int f102(int x, int y)
    {
        x |= x * y;
        return x;
    }

    private static int f103(int x, int y)
    {
        x |= x / y;
        return x;
    }

    private static int f104(int x, int y)
    {
        x |= x % y;
        return x;
    }

    private static int f105(int x, int y)
    {
        x |= x << y;
        return x;
    }

    private static int f106(int x, int y)
    {
        x |= x >> y;
        return x;
    }

    private static int f107(int x, int y)
    {
        x |= x & y;
        return x;
    }

    private static int f108(int x, int y)
    {
        x |= x ^ y;
        return x;
    }

    private static int f109(int x, int y)
    {
        x |= x | y;
        return x;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        int x;
        bool pass = true;

        x = f00(-10, 4);
        if (x != -6)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f00	x = x + y failed.	x: {0}, \texpected: -6\n", x);
            pass = false;
        }

        x = f01(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f01	x = x - y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f02(-10, 4);
        if (x != -40)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f02	x = x * y failed.	x: {0}, \texpected: -40\n", x);
            pass = false;
        }

        x = f03(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f03	x = x / y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f04(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f04	x = x % y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f05(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f05	x = x << y failed.	x: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = f06(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f06	x = x >> y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f07(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f07	x = x & y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f08(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f08	x = x ^ y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f09(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f09	x = x | y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f10(-10, 4);
        if (x != -16)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f10	x += x + y failed.	x: {0}, \texpected: -16\n", x);
            pass = false;
        }

        x = f11(-10, 4);
        if (x != -24)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f11	x += x - y failed.	x: {0}, \texpected: -24\n", x);
            pass = false;
        }

        x = f12(-10, 4);
        if (x != -50)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f12	x += x * y failed.	x: {0}, \texpected: -50\n", x);
            pass = false;
        }

        x = f13(-10, 4);
        if (x != -12)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f13	x += x / y failed.	x: {0}, \texpected: -12\n", x);
            pass = false;
        }

        x = f14(-10, 4);
        if (x != -12)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f14	x += x % y failed.	x: {0}, \texpected: -12\n", x);
            pass = false;
        }

        x = f15(-10, 4);
        if (x != -170)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f15	x += x << y failed.	x: {0}, \texpected: -170\n", x);
            pass = false;
        }

        x = f16(-10, 4);
        if (x != -11)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f16	x += x >> y failed.	x: {0}, \texpected: -11\n", x);
            pass = false;
        }

        x = f17(-10, 4);
        if (x != -6)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f17	x += x & y failed.	x: {0}, \texpected: -6\n", x);
            pass = false;
        }

        x = f18(-10, 4);
        if (x != -24)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f18	x += x ^ y failed.	x: {0}, \texpected: -24\n", x);
            pass = false;
        }

        x = f19(-10, 4);
        if (x != -20)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f19	x += x | y failed.	x: {0}, \texpected: -20\n", x);
            pass = false;
        }

        x = f20(-10, 4);
        if (x != -4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f20	x -= x + y failed.	x: {0}, \texpected: -4\n", x);
            pass = false;
        }

        x = f21(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f21	x -= x - y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f22(-10, 4);
        if (x != 30)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f22	x -= x * y failed.	x: {0}, \texpected: 30\n", x);
            pass = false;
        }

        x = f23(-10, 4);
        if (x != -8)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f23	x -= x / y failed.	x: {0}, \texpected: -8\n", x);
            pass = false;
        }

        x = f24(-10, 4);
        if (x != -8)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f24	x -= x % y failed.	x: {0}, \texpected: -8\n", x);
            pass = false;
        }

        x = f25(-10, 4);
        if (x != 150)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f25	x -= x << y failed.	x: {0}, \texpected: 150\n", x);
            pass = false;
        }

        x = f26(-10, 4);
        if (x != -9)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f26	x -= x >> y failed.	x: {0}, \texpected: -9\n", x);
            pass = false;
        }

        x = f27(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f27	x -= x & y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f28(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f28	x -= x ^ y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f29(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f29	x -= x | y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f30(-10, 4);
        if (x != 60)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f30	x *= x + y failed.	x: {0}, \texpected: 60\n", x);
            pass = false;
        }

        x = f31(-10, 4);
        if (x != 140)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f31	x *= x - y failed.	x: {0}, \texpected: 140\n", x);
            pass = false;
        }

        x = f32(-10, 4);
        if (x != 400)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f32	x *= x * y failed.	x: {0}, \texpected: 400\n", x);
            pass = false;
        }

        x = f33(-10, 4);
        if (x != 20)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f33	x *= x / y failed.	x: {0}, \texpected: 20\n", x);
            pass = false;
        }

        x = f34(-10, 4);
        if (x != 20)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f34	x *= x % y failed.	x: {0}, \texpected: 20\n", x);
            pass = false;
        }

        x = f35(-10, 4);
        if (x != 1600)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f35	x *= x << y failed.	x: {0}, \texpected: 1600\n", x);
            pass = false;
        }

        x = f36(-10, 4);
        if (x != 10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f36	x *= x >> y failed.	x: {0}, \texpected: 10\n", x);
            pass = false;
        }

        x = f37(-10, 4);
        if (x != -40)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f37	x *= x & y failed.	x: {0}, \texpected: -40\n", x);
            pass = false;
        }

        x = f38(-10, 4);
        if (x != 140)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f38	x *= x ^ y failed.	x: {0}, \texpected: 140\n", x);
            pass = false;
        }

        x = f39(-10, 4);
        if (x != 100)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f39	x *= x | y failed.	x: {0}, \texpected: 100\n", x);
            pass = false;
        }

        x = f40(-10, 4);
        if (x != 1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f40	x /= x + y failed.	x: {0}, \texpected: 1\n", x);
            pass = false;
        }

        x = f41(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f41	x /= x - y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f42(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f42	x /= x * y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f43(-10, 4);
        if (x != 5)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f43	x /= x / y failed.	x: {0}, \texpected: 5\n", x);
            pass = false;
        }

        x = f44(-10, 4);
        if (x != 5)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f44	x /= x % y failed.	x: {0}, \texpected: 5\n", x);
            pass = false;
        }

        x = f45(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f45	x /= x << y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f46(-10, 4);
        if (x != 10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f46	x /= x >> y failed.	x: {0}, \texpected: 10\n", x);
            pass = false;
        }

        x = f47(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f47	x /= x & y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f48(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f48	x /= x ^ y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f49(-10, 4);
        if (x != 1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f49	x /= x | y failed.	x: {0}, \texpected: 1\n", x);
            pass = false;
        }

        x = f50(-10, 4);
        if (x != -4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f50	x %= x + y failed.	x: {0}, \texpected: -4\n", x);
            pass = false;
        }

        x = f51(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f51	x %= x - y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f52(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f52	x %= x * y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f53(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f53	x %= x / y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f54(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f54	x %= x % y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f55(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f55	x %= x << y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f56(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f56	x %= x >> y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f57(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f57	x %= x & y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f58(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f58	x %= x ^ y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f59(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f59	x %= x | y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f60(-10, 4);
        if (x != -671088640)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f60	x <<= x + y failed.	x: {0}, \texpected: -671088640\n", x);
            pass = false;
        }

        x = f61(-10, 4);
        if (x != -2621440)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f61	x <<= x - y failed.	x: {0}, \texpected: -2621440\n", x);
            pass = false;
        }

        x = f62(-10, 4);
        if (x != -167772160)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f62	x <<= x * y failed.	x: {0}, \texpected: -167772160\n", x);
            pass = false;
        }

        x = f63(-10, 4);
        if (x != -2147483648)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f63	x <<= x / y failed.	x: {0}, \texpected: -2147483648\n", x);
            pass = false;
        }

        x = f64(-10, 4);
        if (x != -2147483648)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f64	x <<= x % y failed.	x: {0}, \texpected: -2147483648\n", x);
            pass = false;
        }

        x = f65(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f65	x <<= x << y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f66(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f66	x <<= x >> y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f67(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f67	x <<= x & y failed.	x: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = f68(-10, 4);
        if (x != -2621440)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f68	x <<= x ^ y failed.	x: {0}, \texpected: -2621440\n", x);
            pass = false;
        }

        x = f69(-10, 4);
        if (x != -41943040)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f69	x <<= x | y failed.	x: {0}, \texpected: -41943040\n", x);
            pass = false;
        }

        x = f70(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f70	x >>= x + y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f71(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f71	x >>= x - y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f72(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f72	x >>= x * y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f73(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f73	x >>= x / y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f74(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f74	x >>= x % y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f75(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f75	x >>= x << y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f76(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f76	x >>= x >> y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f77(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f77	x >>= x & y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f78(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f78	x >>= x ^ y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f79(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f79	x >>= x | y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f80(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f80	x &= x + y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f81(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f81	x &= x - y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f82(-10, 4);
        if (x != -48)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f82	x &= x * y failed.	x: {0}, \texpected: -48\n", x);
            pass = false;
        }

        x = f83(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f83	x &= x / y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f84(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f84	x &= x % y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f85(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f85	x &= x << y failed.	x: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = f86(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f86	x &= x >> y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f87(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f87	x &= x & y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f88(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f88	x &= x ^ y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f89(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f89	x &= x | y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f90(-10, 4);
        if (x != 12)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f90	x ^= x + y failed.	x: {0}, \texpected: 12\n", x);
            pass = false;
        }

        x = f91(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f91	x ^= x - y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f92(-10, 4);
        if (x != 46)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f92	x ^= x * y failed.	x: {0}, \texpected: 46\n", x);
            pass = false;
        }

        x = f93(-10, 4);
        if (x != 8)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f93	x ^= x / y failed.	x: {0}, \texpected: 8\n", x);
            pass = false;
        }

        x = f94(-10, 4);
        if (x != 8)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f94	x ^= x % y failed.	x: {0}, \texpected: 8\n", x);
            pass = false;
        }

        x = f95(-10, 4);
        if (x != 150)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f95	x ^= x << y failed.	x: {0}, \texpected: 150\n", x);
            pass = false;
        }

        x = f96(-10, 4);
        if (x != 9)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f96	x ^= x >> y failed.	x: {0}, \texpected: 9\n", x);
            pass = false;
        }

        x = f97(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f97	x ^= x & y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = f98(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f98	x ^= x ^ y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = f99(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f99	x ^= x | y failed.	x: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = f100(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f100	x |= x + y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f101(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f101	x |= x - y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f102(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f102	x |= x * y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f103(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f103	x |= x / y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f104(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f104	x |= x % y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = f105(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f105	x |= x << y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f106(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f106	x |= x >> y failed.	x: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = f107(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f107	x |= x & y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f108(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f108	x |= x ^ y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = f109(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("Initial parameters: x is -10 and y is 4.");
            Console.WriteLine("f109	x |= x | y failed.	x: {0}, \texpected: -10\n", x);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED.");
            return 100;
        }
        else
            return 1;
    }
}
}
