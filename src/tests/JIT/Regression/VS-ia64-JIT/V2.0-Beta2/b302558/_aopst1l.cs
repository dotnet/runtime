// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}

[StructLayout(LayoutKind.Sequential)]
public struct AA
{
    public bool[, ,] m_abField1;

    public String Method1(ushort param1, short[,] param2, bool param3)
    {
        return ((String)(((object)((param1 /= param1)))));
    }

    public static Array[][, ,][][, , ,] Static8()
    {
        bool[] local39 = new bool[5] { true, true, true, false, false };
        {
            uint local40 = 65u;
#pragma warning disable 253
            for (App.m_sbyFwd10 += 49; (new AA().Method1(((ushort)(60u)), (new
                short[local40, local40]), false) != ((object)(((short)(62.0f))))); App.
#pragma warning disable 1717,0162
m_dblFwd11 = App.m_dblFwd11)
#pragma warning restore 1717,0162
#pragma warning restore 253
            {
#pragma warning disable 219
                long local41 = ((long)(109.0f));
#pragma warning restore  219
                return new Array[][, ,][][,,,]{(new Array[local40, local40, local40][][,,,])
					 };
            }
            local39[23] = true;
#pragma warning disable 162
            throw new InvalidOperationException();
        }
        return ((Array[][, ,][][, , ,])(((Array)(null))));
#pragma warning restore 162
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing AA::Static8");
            AA.Static8();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static char m_chFwd1;
    public static short m_shFwd2;
    public static String[,][][] m_axFwd3;
    public static String m_xFwd4;
    public static int m_iFwd5;
    public static double[, , ,] m_adblFwd6;
    public static uint m_uFwd7;
    public static ulong m_ulFwd8;
    public static short[,][, ,][] m_ashFwd9;
    public static sbyte m_sbyFwd10;
    public static double m_dblFwd11;
    public static bool m_bFwd12;
    public static ushort[] m_aushFwd13;
    public static byte m_byFwd14;
    public static float m_fFwd15;
    public static ushort m_ushFwd16;
    public static long m_lFwd17;
    public static ulong[] m_aulFwd18;
    public static ushort[,][,][][] m_aushFwd19;
    public static char[] m_achFwd20;
}
