// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_i8_cs
{
public class test
{
    private static Int64 f00(Int64 x, Int64 y)
    {
        x = x + y;
        return x;
    }

    private static Int64 f01(Int64 x, Int64 y)
    {
        x = x - y;
        return x;
    }

    private static Int64 f02(Int64 x, Int64 y)
    {
        x = x * y;
        return x;
    }

    private static Int64 f03(Int64 x, Int64 y)
    {
        x = x / y;
        return x;
    }

    private static Int64 f04(Int64 x, Int64 y)
    {
        x = x % y;
        return x;
    }

    private static Int64 f05(Int64 x, Int64 y)
    {
        x = x << (int)y;
        return x;
    }

    private static Int64 f06(Int64 x, Int64 y)
    {
        x = x >> (int)y;
        return x;
    }

    private static Int64 f07(Int64 x, Int64 y)
    {
        x = x & y;
        return x;
    }

    private static Int64 f08(Int64 x, Int64 y)
    {
        x = x ^ y;
        return x;
    }

    private static Int64 f09(Int64 x, Int64 y)
    {
        x = x | y;
        return x;
    }

    private static Int64 f10(Int64 x, Int64 y)
    {
        x += x + y;
        return x;
    }

    private static Int64 f11(Int64 x, Int64 y)
    {
        x += x - y;
        return x;
    }

    private static Int64 f12(Int64 x, Int64 y)
    {
        x += x * y;
        return x;
    }

    private static Int64 f13(Int64 x, Int64 y)
    {
        x += x / y;
        return x;
    }

    private static Int64 f14(Int64 x, Int64 y)
    {
        x += x % y;
        return x;
    }

    private static Int64 f15(Int64 x, Int64 y)
    {
        x += x << (int)y;
        return x;
    }

    private static Int64 f16(Int64 x, Int64 y)
    {
        x += x >> (int)y;
        return x;
    }

    private static Int64 f17(Int64 x, Int64 y)
    {
        x += x & y;
        return x;
    }

    private static Int64 f18(Int64 x, Int64 y)
    {
        x += x ^ y;
        return x;
    }

    private static Int64 f19(Int64 x, Int64 y)
    {
        x += x | y;
        return x;
    }

    private static Int64 f20(Int64 x, Int64 y)
    {
        x -= x + y;
        return x;
    }

    private static Int64 f21(Int64 x, Int64 y)
    {
        x -= x - y;
        return x;
    }

    private static Int64 f22(Int64 x, Int64 y)
    {
        x -= x * y;
        return x;
    }

    private static Int64 f23(Int64 x, Int64 y)
    {
        x -= x / y;
        return x;
    }

    private static Int64 f24(Int64 x, Int64 y)
    {
        x -= x % y;
        return x;
    }

    private static Int64 f25(Int64 x, Int64 y)
    {
        x -= x << (int)y;
        return x;
    }

    private static Int64 f26(Int64 x, Int64 y)
    {
        x -= x >> (int)y;
        return x;
    }

    private static Int64 f27(Int64 x, Int64 y)
    {
        x -= x & y;
        return x;
    }

    private static Int64 f28(Int64 x, Int64 y)
    {
        x -= x ^ y;
        return x;
    }

    private static Int64 f29(Int64 x, Int64 y)
    {
        x -= x | y;
        return x;
    }

    private static Int64 f30(Int64 x, Int64 y)
    {
        x *= x + y;
        return x;
    }

    private static Int64 f31(Int64 x, Int64 y)
    {
        x *= x - y;
        return x;
    }

    private static Int64 f32(Int64 x, Int64 y)
    {
        x *= x * y;
        return x;
    }

    private static Int64 f33(Int64 x, Int64 y)
    {
        x *= x / y;
        return x;
    }

    private static Int64 f34(Int64 x, Int64 y)
    {
        x *= x % y;
        return x;
    }

    private static Int64 f35(Int64 x, Int64 y)
    {
        x *= x << (int)y;
        return x;
    }

    private static Int64 f36(Int64 x, Int64 y)
    {
        x *= x >> (int)y;
        return x;
    }

    private static Int64 f37(Int64 x, Int64 y)
    {
        x *= x & y;
        return x;
    }

    private static Int64 f38(Int64 x, Int64 y)
    {
        x *= x ^ y;
        return x;
    }

    private static Int64 f39(Int64 x, Int64 y)
    {
        x *= x | y;
        return x;
    }

    private static Int64 f40(Int64 x, Int64 y)
    {
        x /= x + y;
        return x;
    }

    private static Int64 f41(Int64 x, Int64 y)
    {
        x /= x - y;
        return x;
    }

    private static Int64 f42(Int64 x, Int64 y)
    {
        x /= x * y;
        return x;
    }

    private static Int64 f43(Int64 x, Int64 y)
    {
        x /= x / y;
        return x;
    }

    private static Int64 f44(Int64 x, Int64 y)
    {
        x /= x % y;
        return x;
    }

    private static Int64 f45(Int64 x, Int64 y)
    {
        x /= x << (int)y;
        return x;
    }

    private static Int64 f46(Int64 x, Int64 y)
    {
        x /= x >> (int)y;
        return x;
    }

    private static Int64 f47(Int64 x, Int64 y)
    {
        x /= x & y;
        return x;
    }

    private static Int64 f48(Int64 x, Int64 y)
    {
        x /= x ^ y;
        return x;
    }

    private static Int64 f49(Int64 x, Int64 y)
    {
        x /= x | y;
        return x;
    }

    private static Int64 f50(Int64 x, Int64 y)
    {
        x %= x + y;
        return x;
    }

    private static Int64 f51(Int64 x, Int64 y)
    {
        x %= x - y;
        return x;
    }

    private static Int64 f52(Int64 x, Int64 y)
    {
        x %= x * y;
        return x;
    }

    private static Int64 f53(Int64 x, Int64 y)
    {
        x %= x / y;
        return x;
    }

    private static Int64 f54(Int64 x, Int64 y)
    {
        x %= x % y;
        return x;
    }

    private static Int64 f55(Int64 x, Int64 y)
    {
        x %= x << (int)y;
        return x;
    }

    private static Int64 f56(Int64 x, Int64 y)
    {
        x %= x >> (int)y;
        return x;
    }

    private static Int64 f57(Int64 x, Int64 y)
    {
        x %= x & y;
        return x;
    }

    private static Int64 f58(Int64 x, Int64 y)
    {
        x %= x ^ y;
        return x;
    }

    private static Int64 f59(Int64 x, Int64 y)
    {
        x %= x | y;
        return x;
    }

    private static Int64 f60(Int64 x, Int64 y)
    {
        x <<= (int)(x + y);
        return x;
    }

    private static Int64 f61(Int64 x, Int64 y)
    {
        x <<= (int)(x - y);
        return x;
    }

    private static Int64 f62(Int64 x, Int64 y)
    {
        x <<= (int)(x * y);
        return x;
    }

    private static Int64 f63(Int64 x, Int64 y)
    {
        x <<= (int)(x / y);
        return x;
    }

    private static Int64 f64(Int64 x, Int64 y)
    {
        x <<= (int)(x % y);
        return x;
    }

    private static Int64 f65(Int64 x, Int64 y)
    {
        x <<= (int)(x << (int)y);
        return x;
    }

    private static Int64 f66(Int64 x, Int64 y)
    {
        x <<= (int)(x >> (int)y);
        return x;
    }

    private static Int64 f67(Int64 x, Int64 y)
    {
        x <<= (int)(x & y);
        return x;
    }

    private static Int64 f68(Int64 x, Int64 y)
    {
        x <<= (int)(x ^ y);
        return x;
    }

    private static Int64 f69(Int64 x, Int64 y)
    {
        x <<= (int)(x | y);
        return x;
    }

    private static Int64 f70(Int64 x, Int64 y)
    {
        x >>= (int)(x + y);
        return x;
    }

    private static Int64 f71(Int64 x, Int64 y)
    {
        x >>= (int)(x - y);
        return x;
    }

    private static Int64 f72(Int64 x, Int64 y)
    {
        x >>= (int)(x * y);
        return x;
    }

    private static Int64 f73(Int64 x, Int64 y)
    {
        x >>= (int)(x / y);
        return x;
    }

    private static Int64 f74(Int64 x, Int64 y)
    {
        x >>= (int)(x % y);
        return x;
    }

    private static Int64 f75(Int64 x, Int64 y)
    {
        x >>= (int)(x << (int)y);
        return x;
    }

    private static Int64 f76(Int64 x, Int64 y)
    {
        x >>= (int)(x >> (int)y);
        return x;
    }

    private static Int64 f77(Int64 x, Int64 y)
    {
        x >>= (int)(x & y);
        return x;
    }

    private static Int64 f78(Int64 x, Int64 y)
    {
        x >>= (int)(x ^ y);
        return x;
    }

    private static Int64 f79(Int64 x, Int64 y)
    {
        x >>= (int)(x | y);
        return x;
    }

    private static Int64 f80(Int64 x, Int64 y)
    {
        x &= x + y;
        return x;
    }

    private static Int64 f81(Int64 x, Int64 y)
    {
        x &= x - y;
        return x;
    }

    private static Int64 f82(Int64 x, Int64 y)
    {
        x &= x * y;
        return x;
    }

    private static Int64 f83(Int64 x, Int64 y)
    {
        x &= x / y;
        return x;
    }

    private static Int64 f84(Int64 x, Int64 y)
    {
        x &= x % y;
        return x;
    }

    private static Int64 f85(Int64 x, Int64 y)
    {
        x &= x << (int)y;
        return x;
    }

    private static Int64 f86(Int64 x, Int64 y)
    {
        x &= x >> (int)y;
        return x;
    }

    private static Int64 f87(Int64 x, Int64 y)
    {
        x &= x & y;
        return x;
    }

    private static Int64 f88(Int64 x, Int64 y)
    {
        x &= x ^ y;
        return x;
    }

    private static Int64 f89(Int64 x, Int64 y)
    {
        x &= x | y;
        return x;
    }

    private static Int64 f90(Int64 x, Int64 y)
    {
        x ^= x + y;
        return x;
    }

    private static Int64 f91(Int64 x, Int64 y)
    {
        x ^= x - y;
        return x;
    }

    private static Int64 f92(Int64 x, Int64 y)
    {
        x ^= x * y;
        return x;
    }

    private static Int64 f93(Int64 x, Int64 y)
    {
        x ^= x / y;
        return x;
    }

    private static Int64 f94(Int64 x, Int64 y)
    {
        x ^= x % y;
        return x;
    }

    private static Int64 f95(Int64 x, Int64 y)
    {
        x ^= x << (int)y;
        return x;
    }

    private static Int64 f96(Int64 x, Int64 y)
    {
        x ^= x >> (int)y;
        return x;
    }

    private static Int64 f97(Int64 x, Int64 y)
    {
        x ^= x & y;
        return x;
    }

    private static Int64 f98(Int64 x, Int64 y)
    {
        x ^= x ^ y;
        return x;
    }

    private static Int64 f99(Int64 x, Int64 y)
    {
        x ^= x | y;
        return x;
    }

    private static Int64 f100(Int64 x, Int64 y)
    {
        x |= x + y;
        return x;
    }

    private static Int64 f101(Int64 x, Int64 y)
    {
        x |= x - y;
        return x;
    }

    private static Int64 f102(Int64 x, Int64 y)
    {
        x |= x * y;
        return x;
    }

    private static Int64 f103(Int64 x, Int64 y)
    {
        x |= x / y;
        return x;
    }

    private static Int64 f104(Int64 x, Int64 y)
    {
        x |= x % y;
        return x;
    }

    private static Int64 f105(Int64 x, Int64 y)
    {
        x |= x << (int)y;
        return x;
    }

    private static Int64 f106(Int64 x, Int64 y)
    {
        x |= x >> (int)y;
        return x;
    }

    private static Int64 f107(Int64 x, Int64 y)
    {
        x |= x & y;
        return x;
    }

    private static Int64 f108(Int64 x, Int64 y)
    {
        x |= x ^ y;
        return x;
    }

    private static Int64 f109(Int64 x, Int64 y)
    {
        x |= x | y;
        return x;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        Int64 x;
        bool pass = true;

        x = f00(-10, 4);
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f00	x = x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -6", x);
            pass = false;
        }

        x = f01(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f01	x = x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f02(-10, 4);
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f02	x = x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -40", x);
            pass = false;
        }

        x = f03(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f03	x = x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f04(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f04	x = x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f05(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f05	x = x << y failed.");
            Console.WriteLine("x: {0}, \texpected: -160", x);
            pass = false;
        }

        x = f06(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f06	x = x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f07(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f07	x = x & y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f08(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f08	x = x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f09(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f09	x = x | y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f10(-10, 4);
        if (x != -16)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f10	x += x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -16", x);
            pass = false;
        }

        x = f11(-10, 4);
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f11	x += x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -24", x);
            pass = false;
        }

        x = f12(-10, 4);
        if (x != -50)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f12	x += x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -50", x);
            pass = false;
        }

        x = f13(-10, 4);
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f13	x += x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -12", x);
            pass = false;
        }

        x = f14(-10, 4);
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f14	x += x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -12", x);
            pass = false;
        }

        x = f15(-10, 4);
        if (x != -170)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f15	x += x << y failed.");
            Console.WriteLine("x: {0}, \texpected: -170", x);
            pass = false;
        }

        x = f16(-10, 4);
        if (x != -11)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f16	x += x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -11", x);
            pass = false;
        }

        x = f17(-10, 4);
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f17	x += x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -6", x);
            pass = false;
        }

        x = f18(-10, 4);
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f18	x += x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -24", x);
            pass = false;
        }

        x = f19(-10, 4);
        if (x != -20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f19	x += x | y failed.");
            Console.WriteLine("x: {0}, \texpected: -20", x);
            pass = false;
        }

        x = f20(-10, 4);
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f20	x -= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -4", x);
            pass = false;
        }

        x = f21(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f21	x -= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f22(-10, 4);
        if (x != 30)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f22	x -= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: 30", x);
            pass = false;
        }

        x = f23(-10, 4);
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f23	x -= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -8", x);
            pass = false;
        }

        x = f24(-10, 4);
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f24	x -= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -8", x);
            pass = false;
        }

        x = f25(-10, 4);
        if (x != 150)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f25	x -= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: 150", x);
            pass = false;
        }

        x = f26(-10, 4);
        if (x != -9)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f26	x -= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -9", x);
            pass = false;
        }

        x = f27(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f27	x -= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f28(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f28	x -= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f29(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f29	x -= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f30(-10, 4);
        if (x != 60)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f30	x *= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: 60", x);
            pass = false;
        }

        x = f31(-10, 4);
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f31	x *= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: 140", x);
            pass = false;
        }

        x = f32(-10, 4);
        if (x != 400)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f32	x *= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: 400", x);
            pass = false;
        }

        x = f33(-10, 4);
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f33	x *= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: 20", x);
            pass = false;
        }

        x = f34(-10, 4);
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f34	x *= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: 20", x);
            pass = false;
        }

        x = f35(-10, 4);
        if (x != 1600)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f35	x *= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: 1600", x);
            pass = false;
        }

        x = f36(-10, 4);
        if (x != 10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f36	x *= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: 10", x);
            pass = false;
        }

        x = f37(-10, 4);
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f37	x *= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -40", x);
            pass = false;
        }

        x = f38(-10, 4);
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f38	x *= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: 140", x);
            pass = false;
        }

        x = f39(-10, 4);
        if (x != 100)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f39	x *= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: 100", x);
            pass = false;
        }

        x = f40(-10, 4);
        if (x != 1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f40	x /= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: 1", x);
            pass = false;
        }

        x = f41(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f41	x /= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f42(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f42	x /= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f43(-10, 4);
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f43	x /= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: 5", x);
            pass = false;
        }

        x = f44(-10, 4);
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f44	x /= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: 5", x);
            pass = false;
        }

        x = f45(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f45	x /= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f46(-10, 4);
        if (x != 10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f46	x /= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: 10", x);
            pass = false;
        }

        x = f47(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f47	x /= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f48(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f48	x /= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f49(-10, 4);
        if (x != 1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f49	x /= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: 1", x);
            pass = false;
        }

        x = f50(-10, 4);
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f50	x %= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -4", x);
            pass = false;
        }

        x = f51(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f51	x %= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f52(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f52	x %= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f53(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f53	x %= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f54(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f54	x %= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f55(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f55	x %= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f56(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f56	x %= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f57(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f57	x %= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f58(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f58	x %= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f59(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f59	x %= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        /*
		x = f60(-10, 4);
		if (x != -671088640)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f60	x <<= x + y failed.");
			Console.WriteLine("x: {0}, \texpected: -671088640", x);
			pass = false;
		}

		x = f61(-10, 4);
		if (x != -2621440)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f61	x <<= x - y failed.");
			Console.WriteLine("x: {0}, \texpected: -2621440", x);
			pass = false;
		}

		x = f62(-10, 4);
		if (x != -167772160)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f62	x <<= x * y failed.");
			Console.WriteLine("x: {0}, \texpected: -167772160", x);
			pass = false;
		}

		x = f63(-10, 4);
		if (x != -2147483648)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f63	x <<= x / y failed.");
			Console.WriteLine("x: {0}, \texpected: -2147483648", x);
			pass = false;
		}

		x = f64(-10, 4);
		if (x != -2147483648)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f64	x <<= x % y failed.");
			Console.WriteLine("x: {0}, \texpected: -2147483648", x);
			pass = false;
		}

		x = f65(-10, 4);
		if (x != -10)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f65	x <<= x << y failed.");
			Console.WriteLine("x: {0}, \texpected: -10", x);
			pass = false;
		}
		*/

        x = f66(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f66	x <<= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f67(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f67	x <<= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -160", x);
            pass = false;
        }

        /*
		x = f68(-10, 4);
		if (x != -2621440)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f68	x <<= x ^ y failed.");
			Console.WriteLine("x: {0}, \texpected: -2621440", x);
			pass = false;
		}

		x = f69(-10, 4);
		if (x != -41943040)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f69	x <<= x | y failed.");
			Console.WriteLine("x: {0}, \texpected: -41943040", x);
			pass = false;
		}
		*/

        x = f70(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f70	x >>= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f71(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f71	x >>= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f72(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f72	x >>= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f73(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f73	x >>= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f74(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f74	x >>= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        /*
		x = f75(-10, 4);
		if (x != -10)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
			Console.WriteLine("f75	x >>= x << y failed.");
			Console.WriteLine("x: {0}, \texpected: -10", x);
			pass = false;
		}
		*/

        x = f76(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f76	x >>= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f77(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f77	x >>= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f78(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f78	x >>= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f79(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f79	x >>= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f80(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f80	x &= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f81(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f81	x &= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f82(-10, 4);
        if (x != -48)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f82	x &= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -48", x);
            pass = false;
        }

        x = f83(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f83	x &= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f84(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f84	x &= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f85(-10, 4);
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f85	x &= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: -160", x);
            pass = false;
        }

        x = f86(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f86	x &= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f87(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f87	x &= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f88(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f88	x &= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f89(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f89	x &= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f90(-10, 4);
        if (x != 12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f90	x ^= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: 12", x);
            pass = false;
        }

        x = f91(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f91	x ^= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f92(-10, 4);
        if (x != 46)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f92	x ^= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: 46", x);
            pass = false;
        }

        x = f93(-10, 4);
        if (x != 8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f93	x ^= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: 8", x);
            pass = false;
        }

        x = f94(-10, 4);
        if (x != 8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f94	x ^= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: 8", x);
            pass = false;
        }

        x = f95(-10, 4);
        if (x != 150)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f95	x ^= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: 150", x);
            pass = false;
        }

        x = f96(-10, 4);
        if (x != 9)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f96	x ^= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: 9", x);
            pass = false;
        }

        x = f97(-10, 4);
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f97	x ^= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -14", x);
            pass = false;
        }

        x = f98(-10, 4);
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f98	x ^= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: 4", x);
            pass = false;
        }

        x = f99(-10, 4);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f99	x ^= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: 0", x);
            pass = false;
        }

        x = f100(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f100	x |= x + y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f101(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f101	x |= x - y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f102(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f102	x |= x * y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f103(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f103	x |= x / y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f104(-10, 4);
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f104	x |= x % y failed.");
            Console.WriteLine("x: {0}, \texpected: -2", x);
            pass = false;
        }

        x = f105(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f105	x |= x << y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f106(-10, 4);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f106	x |= x >> y failed.");
            Console.WriteLine("x: {0}, \texpected: -1", x);
            pass = false;
        }

        x = f107(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f107	x |= x & y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f108(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f108	x |= x ^ y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
            pass = false;
        }

        x = f109(-10, 4);
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4.");
            Console.WriteLine("f109	x |= x | y failed.");
            Console.WriteLine("x: {0}, \texpected: -10", x);
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
