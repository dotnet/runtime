// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

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
#if OP_DIV
			return (sbyte)(a / 0.5);
#elif OP_ADD
			return (sbyte)(a + a);
#elif OP_SUB
            return (sbyte)(-1 - a - a);
#else
			return (sbyte)(a * 2);
#endif
        }
    }

    private static byte Test_byte()
    {
        if (!rtv) return 0;
        byte a = 1 + byte.MaxValue / 2;
        checked
        {
#if OP_DIV
			return (byte)(a / 0.5);
#elif OP_ADD
			return (byte)(a + a);
#elif OP_SUB
            return (byte)(0 - a - a);
#else
			return (byte)(a * 2);
#endif
        }
    }

    private static short Test_short()
    {
        if (!rtv) return 0;
        short a = 1 + short.MaxValue / 2;
        checked
        {
#if OP_DIV
			return (short)(a / 0.5);
#elif OP_ADD
			return (short)(a + a);
#elif OP_SUB
            return (short)(-1 - a - a);
#else
			return (short)(a * 2);
#endif
        }
    }

    private static ushort Test_ushort()
    {
        if (!rtv) return 0;
        ushort a = 1 + ushort.MaxValue / 2;
        checked
        {
#if OP_DIV
			return (ushort)(a / 0.5);
#elif OP_ADD
			return (ushort)(a + a);
#elif OP_SUB
            return (ushort)(0 - a - a);
#else
			return (ushort)(a * 2);
#endif
        }
    }

    private static int Test_int()
    {
        if (!rtv) return 0;
        int a = 1 + int.MaxValue / 2;
        checked
        {
#if OP_DIV
			return (int)(a / 0.5);
#elif OP_ADD
			return a + a;
#elif OP_SUB
            return -1 - a - a;
#else
			return a * 2;
#endif
        }
    }

    private static uint Test_uint()
    {
        if (!rtv) return 0;
        uint a = 1U + uint.MaxValue / 2U;
        checked
        {
#if OP_DIV
			return (uint)(a / 0.5);
#elif OP_ADD
			return a + a;
#elif OP_SUB
            return 0U - a - a;
#else
			return a * 2;
#endif
        }
    }

    private static long Test_long()
    {
        if (!rtv) return 0;
        long a = 1L + long.MaxValue / 2L;
        checked
        {
#if OP_DIV
			return (long)(a / 0.5);
#elif OP_ADD
			return a + a;
#elif OP_SUB
            return -1L - a - a;
#else
			return a * 2;
#endif
        }
    }

    private static ulong Test_ulong()
    {
        if (!rtv) return 0;
        ulong a = 1UL + ulong.MaxValue / 2UL;
        checked
        {
#if OP_DIV
			return (ulong)(a / 0.5);
#elif OP_ADD
			return a + a;
#elif OP_SUB
            return 0UL - a - a;
#else
			return a * 2;
#endif
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
#if OP_DIV
		const string op = "div.ovf";
#elif OP_ADD
		const string op = "add.ovf";
#elif OP_SUB
        const string op = "sub.ovf";
#else
		const string op = "mul.ovf";
#endif

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
