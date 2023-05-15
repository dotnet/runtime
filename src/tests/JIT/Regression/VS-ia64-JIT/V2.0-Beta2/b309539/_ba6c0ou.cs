// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
public struct AA
{
    public double m_dblField1;
    public static uint[][][] Static1(ref char param1)
    {
        AA[, , ,] local1 = (new AA[66u, 67u, ((uint)(78.0f)), 114u]);
        int[, ,] local2 = (new int[((uint)(72.0f)), 5u, 64u]);
        return (new uint[31u][][]);
    }
    public static double[] Static3(long[,][,] param1, ref Array[,][, ,] param2, ref 
		object param3, ref String param4, ref bool param5, ref object param6)
    {
#pragma warning disable 1717,0162
        for (App.m_axFwd2 = App.m_axFwd2; ((bool)(param3)); App.m_iFwd3 = 68)
#pragma warning restore 1717,0162
        {
            return (new double[28u]);
        }
        return ((double[])(((Array)(param3))));
    }
    public static char[] Static4()
    {
        char local10 = '\x36';
        float[] local11 = new float[] { 79.0f, 33.0f, 96.0f, 109.0f, 86.0f };
        for (App.m_uFwd4 /= 23u; Convert.ToBoolean(Convert.ToByte(local10)); App.m_iFwd3
            = 19)
        {
            AA.Static1(ref local10);
            while (App.m_bFwd6)
            {
#pragma warning disable 1717
                local11 = (local11 = (local11 = local11));
#pragma warning restore 1717
            }
            local10 = '\0';
        }
        if (((bool)(((object)(new AA())))))
            local11[79] *= ((float)(83.0));

        return ((char[])(((Array)(null))));
    }
}

[StructLayout(LayoutKind.Sequential)]
public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        App.m_bFwd6 = false;

        try
        {
            Console.WriteLine("Testing AA::Static3");
            AA.Static3(
                ((long[,][,])(((Array)(null)))),
                ref App.m_axFwd8,
                ref App.m_objFwd9,
                ref App.m_xFwd10,
                ref App.m_bFwd6,
                ref App.m_objFwd9);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static4");
            AA.Static4();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static char m_chFwd1;
    public static Array[][] m_axFwd2;
    public static int m_iFwd3;
    public static uint m_uFwd4;
    public static byte m_byFwd5;
    public static bool m_bFwd6;
    public static ulong m_ulFwd7;
    public static Array[,][, ,] m_axFwd8;
    public static object m_objFwd9;
    public static String m_xFwd10;
}
