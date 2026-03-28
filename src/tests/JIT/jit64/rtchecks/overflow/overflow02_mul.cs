// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace overflow02_mul;

public class OVFTest
{
    static public volatile bool rtv;

    static OVFTest()
    {
        rtv = Environment.TickCount != 0;
    }

    private static sbyte Test_sbyte()
    {
        if (!rtv) return 0;
        sbyte a = 1 + sbyte.MaxValue / 2;
        checked
        {
			return (sbyte)(a * 2);
        }
    }

    private static byte Test_byte()
    {
        if (!rtv) return 0;
        byte a = 1 + byte.MaxValue / 2;
        checked
        {
			return (byte)(a * 2);
        }
    }

    private static short Test_short()
    {
        if (!rtv) return 0;
        short a = 1 + short.MaxValue / 2;
        checked
        {
			return (short)(a * 2);
        }
    }

    private static ushort Test_ushort()
    {
        if (!rtv) return 0;
        ushort a = 1 + ushort.MaxValue / 2;
        checked
        {
			return (ushort)(a * 2);
        }
    }

    private static int Test_int()
    {
        if (!rtv) return 0;
        int a = 1 + int.MaxValue / 2;
        checked
        {
			return a * 2;
        }
    }

    private static uint Test_uint()
    {
        if (!rtv) return 0;
        uint a = 1U + uint.MaxValue / 2U;
        checked
        {
			return a * 2;
        }
    }

    private static long Test_long()
    {
        if (!rtv) return 0;
        long a = 1L + long.MaxValue / 2L;
        checked
        {
			return a * 2;
        }
    }

    private static ulong Test_ulong()
    {
        if (!rtv) return 0;
        ulong a = 1UL + ulong.MaxValue / 2UL;
        checked
        {
			return a * 2;
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
            var a = Test_byte();
            Console.WriteLine(a);
        });

        Console.Write("Type 'sbyte'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_sbyte();
            Console.WriteLine(a);
        });

        Console.Write("Type 'short'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_short();
            Console.WriteLine(a);
        });

        Console.Write("Type 'ushort' . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_ushort();
            Console.WriteLine(a);
        });

        Console.Write("Type 'int'. . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_int();
            Console.WriteLine(a);
        });

        Console.Write("Type 'uint' . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_uint();
            Console.WriteLine(a);
        });

        Console.Write("Type 'long' . . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_long();
            Console.WriteLine(a);
        });

        Console.Write("Type 'ulong'. . : ");
        Assert.Throws<OverflowException>(() =>
        {
            var a = Test_ulong();
            Console.WriteLine(a);
        });

        return;
    }
}
