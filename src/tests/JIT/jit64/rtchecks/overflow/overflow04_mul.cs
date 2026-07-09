// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace overflow04_mul;

public class OVFTest
{
    static public volatile bool rtv;

    static OVFTest()
    {
        rtv = Environment.TickCount != 0;
    }

    private static sbyte Test_sbyte(sbyte a)
    {
        checked
        {
			return Test_sbyte((sbyte)(a * 2));
        }
    }

    private static byte Test_byte(byte a)
    {
        checked
        {
			return Test_byte((byte)(a * 2));
        }
    }

    private static short Test_short(short a)
    {
        checked
        {
			return Test_short((short)(a * 2));
        }
    }

    private static ushort Test_ushort(ushort a)
    {
        checked
        {
			return Test_ushort((ushort)(a * 2));
        }
    }

    private static int Test_int(int a)
    {
        checked
        {
			return Test_int(a * 2);
        }
    }

    private static uint Test_uint(uint a)
    {
        checked
        {
			return Test_uint(a * 2U);
        }
    }

    private static long Test_long(long a)
    {
        checked
        {
			return Test_long(a * 2L);
        }
    }

    private static ulong Test_ulong(ulong a)
    {
        checked
        {
			return Test_ulong(a * 2UL);
        }
    }


    [Fact]
    public static void TestEntryPoint()
    {
		const string op = "mul.ovf";

        Console.WriteLine("Runtime Checks [OP: {0}]", op);



        Console.Write("Type 'byte' . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_byte((byte)(OVFTest.rtv ? 1 + byte.MaxValue / 2 : 0));
            Console.WriteLine(a);
        });

        Console.Write("Type 'sbyte'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_sbyte((sbyte)(OVFTest.rtv ? 1 + sbyte.MaxValue / 2 : 0));
            Console.WriteLine(a);
        });

        Console.Write("Type 'short'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_short((short)(OVFTest.rtv ? 1 + short.MaxValue / 2 : 0));
            Console.WriteLine(a);
        });

        Console.Write("Type 'ushort' . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_ushort((ushort)(OVFTest.rtv ? 1 + ushort.MaxValue / 2 : 0));
            Console.WriteLine(a);
        });

        Console.Write("Type 'int'. . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_int((int)(OVFTest.rtv ? 1 + int.MaxValue / 2 : 0));
            Console.WriteLine(a);
        });

        Console.Write("Type 'uint' . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_uint((uint)(OVFTest.rtv ? 1U + uint.MaxValue / 2U : 0U));
            Console.WriteLine(a);
        });

        Console.Write("Type 'long' . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_long((long)(OVFTest.rtv ? 1L + long.MaxValue / 2L : 0L));
            Console.WriteLine(a);
        });

        Console.Write("Type 'ulong'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_ulong((ulong)(OVFTest.rtv ? 1UL + ulong.MaxValue / 2UL : 0UL));
            Console.WriteLine(a);
        });

        return;
    }
}
