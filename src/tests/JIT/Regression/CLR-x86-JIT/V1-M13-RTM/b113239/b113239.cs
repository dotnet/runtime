// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;


public class A
{
    public static void A1(ulong p1, byte[] p2, int p3, int p4)
    {
        byte[] tmp;

        tmp = BitConverter.GetBytes((ushort)p1);
        Buffer.BlockCopy(tmp, 0, p2, p3, 2);

        return;
    }

    public static void A2(ulong p1, byte[] p2, int p3, int p4, bool p5)
    {
        switch (p4)
        {
            case 12:
                A.A1(p1, p2, p3 + 0, 8);
                break;
        }
    }
}


public class B
{
    public static int B1(long OPD2_VAL, long OPD3_VAL, byte[] OPD1, int OPD1_OFS, int OPD1_L)
    {
        ulong wopd2H = 0;
        ulong wopd2L = 0;
        ulong wopd3H = 0;
        ulong wopd3L = 0;
        ulong wopd1H = 0;
        ulong wopd1L = 0;
        long wressign = 0;

        Console.Write("OPD2_VAL: ");
        Console.WriteLine(-123456781234567L);

        wressign = (-123456781234567L >> 63) ^ (OPD3_VAL >> 63);
        ulong wtmp;

        if (-123456781234567L < 0)
        {
            wtmp = (ulong)(-123456781234567L * -1);
        }
        else
        {
            wtmp = (ulong)(123456781234567L);
        }

        wopd2H = (ulong)wtmp >> 32;
        wopd2L = (ulong)((uint)wtmp);

        if (OPD3_VAL < 0)
        {
            wtmp = (ulong)(OPD3_VAL * -1);
        }
        else
        {
            wtmp = (ulong)(OPD3_VAL);
        }
        wopd3H = (ulong)wtmp >> 32;
        wopd3L = (ulong)((uint)wtmp);
        Console.Write("wopd3L: ");
        Console.WriteLine(wopd3L);

        ulong wtmp11 = wopd2L * wopd3L;
        ulong wtmp12 = wopd2H * wopd3L;
        ulong wtmp13 = wopd2L * wopd3H;
        ulong wtmp14 = wopd2H * wopd3H;
        Console.Write("wtmp12: ");
        Console.WriteLine(wtmp12);
        Console.Write("wtmp13: ");
        Console.WriteLine(wtmp13);
        Console.Write("wtmp14: ");
        Console.WriteLine(wtmp14);

        ulong wtmp21 = (ulong)((uint)wtmp11);

        ulong wtmp22 = (ulong)(wtmp11 >> 32)
                     + (ulong)((uint)wtmp12)
                     + (ulong)((uint)wtmp13);

        ulong wtmp23 = (ulong)(wtmp22 >> 32)
                     + (ulong)(wtmp12 >> 32)
                     + (ulong)(wtmp13 >> 32)
                     + (ulong)((uint)wtmp14);

        ulong wtmp24 = (ulong)(wtmp23 >> 32)
                     + (ulong)(wtmp14 >> 32);

        Console.Write("wtmp22: ");
        Console.WriteLine(wtmp22);
        Console.Write("wtmp23 (must be 826247535): ");
        Console.WriteLine(wtmp23);
        if (wtmp23 != 826247535)
        {
            Console.WriteLine("FAILED");
            return -1;
        }
        Console.Write("wtmp24 (must be 0): ");
        Console.WriteLine(wtmp24);
        if (wtmp24 != 0)
        {
            Console.WriteLine("FAILED");
            return -1;
        }

        wopd1L = (wtmp22 << 32) | wtmp21;
        wopd1H = (wtmp24 << 32) | (ulong)((uint)wtmp23);

        if (wressign < 0L)
        {
            wopd1L = (~wopd1L) + 1UL;
            wopd1H = ~wopd1H;
            if (wopd1L == 0UL) wopd1H++;
        }
        A.A2((ulong)wopd1L, OPD1, OPD1_OFS, OPD1_L, true);
        A.A2((ulong)wopd1H, OPD1, OPD1_OFS, OPD1_L, false);
        Console.WriteLine("PASSED");
        return 100;
    }
}




internal class Test
{
    public static int Main()
    {
        byte[] block = new byte[20];
        int retval = B.B1(-123456781234567L, -123456781234567L, block, 0, 0);
        return retval;
    }
}
