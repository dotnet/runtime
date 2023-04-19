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

    private static sbyte Test_sbyte(sbyte a)
    {
        checked
        {
#if OP_DIV
			return Test_sbyte((sbyte)(a / 0.5));
#elif OP_ADD
			return Test_sbyte((sbyte)(a + a));
#elif OP_SUB
            return Test_sbyte((sbyte)(-1 - a - a));
#else
			return Test_sbyte((sbyte)(a * 2));
#endif
        }
    }

    private static byte Test_byte(byte a)
    {
        checked
        {
#if OP_DIV
			return Test_byte((byte)(a / 0.5));
#elif OP_ADD
			return Test_byte((byte)(a + a));
#elif OP_SUB
            return Test_byte((byte)(0 - a - a));
#else
			return Test_byte((byte)(a * 2));
#endif
        }
    }

    private static short Test_short(short a)
    {
        checked
        {
#if OP_DIV
			return Test_short((short)(a / 0.5));
#elif OP_ADD
			return Test_short((short)(a + a));
#elif OP_SUB
            return Test_short((short)(-1 - a - a));
#else
			return Test_short((short)(a * 2));
#endif
        }
    }

    private static ushort Test_ushort(ushort a)
    {
        checked
        {
#if OP_DIV
			return Test_ushort((ushort)(a / 0.5));
#elif OP_ADD
			return Test_ushort((ushort)(a + a));
#elif OP_SUB
            return Test_ushort((ushort)(0 - a - a));
#else
			return Test_ushort((ushort)(a * 2));
#endif
        }
    }

    private static int Test_int(int a)
    {
        checked
        {
#if OP_DIV
			return Test_int((int)(a / 0.5));
#elif OP_ADD
			return Test_int(a + a);
#elif OP_SUB
            return Test_int(-1 - a - a);
#else
			return Test_int(a * 2);
#endif
        }
    }

    private static uint Test_uint(uint a)
    {
        checked
        {
#if OP_DIV
			return Test_uint((uint)(a / 0.5));
#elif OP_ADD
			return Test_uint(a + a);
#elif OP_SUB
            return Test_uint(0U - a - a);
#else
			return Test_uint(a * 2U);
#endif
        }
    }

    private static long Test_long(long a)
    {
        checked
        {
#if OP_DIV
			return Test_long((long)(a / 0.5));
#elif OP_ADD
			return Test_long(a + a);
#elif OP_SUB
            return Test_long(-1L - a - a);
#else
			return Test_long(a * 2L);
#endif
        }
    }

    private static ulong Test_ulong(ulong a)
    {
        checked
        {
#if OP_DIV
			return Test_ulong((ulong)(a / 0.5));
#elif OP_ADD
			return Test_ulong(a + a);
#elif OP_SUB
            return Test_ulong(0UL - a - a);
#else
			return Test_ulong(a * 2UL);
#endif
        }
    }


    [Fact]
    public static int TestEntryPoint()
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

        int check = 8;

        try
        {
            Console.Write("Type 'byte' . . : ");
            byte a = Test_byte((byte)(OVFTest.rtv ? 1 + byte.MaxValue / 2 : 0));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'sbyte'. . : ");
            sbyte a = Test_sbyte((sbyte)(OVFTest.rtv ? 1 + sbyte.MaxValue / 2 : 0));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'short'. . : ");
            short a = Test_short((short)(OVFTest.rtv ? 1 + short.MaxValue / 2 : 0));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'ushort' . : ");
            ushort a = Test_ushort((ushort)(OVFTest.rtv ? 1 + ushort.MaxValue / 2 : 0));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'int'. . . : ");
            int a = Test_int((int)(OVFTest.rtv ? 1 + int.MaxValue / 2 : 0));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'uint' . . : ");
            uint a = Test_uint((uint)(OVFTest.rtv ? 1U + uint.MaxValue / 2U : 0U));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'long' . . : ");
            long a = Test_long((long)(OVFTest.rtv ? 1L + long.MaxValue / 2L : 0L));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        try
        {
            Console.Write("Type 'ulong'. . : ");
            ulong a = Test_ulong((ulong)(OVFTest.rtv ? 1UL + ulong.MaxValue / 2UL : 0UL));
            Console.WriteLine("failed! - a = " + a);
        }
        catch (System.OverflowException)
        {
            Console.WriteLine("passed");
            check--;
        }

        return check == 0 ? 100 : 1;
    }
}
