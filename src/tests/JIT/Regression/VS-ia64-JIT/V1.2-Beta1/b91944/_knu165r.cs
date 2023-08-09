// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public object m_objField1;
    public static long Static1(Array param1, ushort[,] param2, ref int[,,][,,]
        param3, ulong[,] param4, float[][,,] param5)
    {
        long[,] local1 = (new long[100u, 30u]);
#pragma warning disable 1718
        while ((param1 != param1))
#pragma warning restore 1718
        {
            for (App.m_lFwd1 = ((long)(6.0)); App.m_bFwd2; App.m_ulFwd3 -= ((ulong)(8u)))
            {
                byte local2 = App.m_byFwd4;
                do
                {
                    long[,,,] local3 = (new long[4u, 68u, 35u, 15u]);
                    while (App.m_bFwd2)
                    {
#pragma warning disable 1717
                        param1 = (param1 = param1);
#pragma warning restore 1717
                        param3 = ((int[,,][,,])(param1));
                        do
                        {
                            ulong local4 = ((ulong)(108.0f));
                            String[][][,,,,] local5 = (new String[14u][][,,,,]);
#pragma warning disable 1718
                            for (App.m_sbyFwd5 *= App.m_sbyFwd5; (local4 != local4); App.m_fFwd6 = 28.0f
                                )
#pragma warning restore 1718
                            {
                                bool[][][,,,] local6 = new bool[][][,,,] { (new bool[53u][,,,]) };
                                char local7 = '\x6f';
                            }
                            param4[((int)(108.0f)), 27] /= local4;
                        }
                        while ((param1 == null));
                        try
                        {
                        }
                        catch (Exception)
                        {
                            bool local8 = true;
                            TypedReference local9 = __makeref(App.m_chFwd7);
                        }
                        return ((long)(91.0f));
                    }
                }
                while (App.m_bFwd2);
                if ((null != param1))
                    if (App.m_bFwd2)
                        for (App.m_uFwd8++; Convert.ToBoolean(local2); App.m_shFwd9 -= ((short)(61))
                            )
                        {
                            float[,] local10 = (new float[16u, 15u]);
                        }
            }
            if ((param1 != null))
                for (App.m_fFwd6 *= 55.0f; App.m_bFwd2; App.m_sbyFwd5 -= ((sbyte)(122)))
                {
                    object local11 = new AA().m_objField1;
                    sbyte[,,][] local12 = (new sbyte[108u, 78u, 18u][]);
                }
            try
            {
                bool local13 = true;
            }
            catch (IndexOutOfRangeException)
            {
                AA[,,][,][] local14 = (new AA[71u, 60u, 84u][,][]);
            }
        }
        return App.m_lFwd1;
    }
    public static TestEnum[] Static2()
    {
        AA[,] local15 = (new AA[47u, 85u]);
        uint local16 = 106u;
        try
        {
            short[,,][,][] local17 = (new short[91u, 93u, 91u][,][]);
            try
            {
                ulong[] local18 = (new ulong[102u]);
                do
                {
                    ushort[,,,,] local19 = (new ushort[29u, 107u, 46u, 25u, 125u]);
                    char local20 = '\x3f';
                    if (App.m_bFwd2)
#pragma warning disable 1718
                        if ((local16 != local16))
#pragma warning restore 1718
                            do
                            {
                                ushort[,] local21 = (new ushort[119u, 19u]);
                            }
                            while (App.m_bFwd2);
                    local19[28, 103, ((int)(local20)), ((int)(60u)), 79] -= App.m_ushFwd10;
                    try
                    {
                        float local22 = 29.0f;
                        long local23 = App.m_lFwd1;
                    }
                    catch (Exception)
                    {
                        byte[,,][,,][][,] local24 = (new byte[27u, 16u, 23u][,,][][,]);
                        TypedReference local25 = __makeref(App.m_axFwd11);
                    }
                }
                while (App.m_bFwd2);
                local17[2, 27, 54][107, 25][3] = ((short)(local16));
                local17[107, ((int)('\x11')), 115][97, ((int)(local16))][11] += ((short)(4.0
                    ));
                return new TestEnum[] { 0, TestEnum.blue };
            }
            catch (IndexOutOfRangeException)
            {
                sbyte local26 = ((sbyte)(45));
                short local27 = ((short)(80.0));
            }
            AA.Static1(
                ((Array)(null)),
                (new ushort[79u, local16]),
                ref App.m_aiFwd12,
                (new ulong[30u, local16]),
                (new float[local16][,,]));
        }
        catch (InvalidOperationException)
        {
        }
        if (App.m_bFwd2)
            do
            {
                sbyte local28 = App.m_sbyFwd5;
            }
            while (App.m_bFwd2);
        else
        {
        }
        local15[47, ((int)(124.0f))] = new AA();
        try
        {
        }
        catch (InvalidOperationException)
        {
        }
        if (App.m_bFwd2)
            local15[88, 123].m_objField1 = ((object)(true));
        return (new TestEnum[local16]);
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            AA.Static1(
                ((Array)(null)),
                (new ushort[1u, 44u]),
                ref App.m_aiFwd12,
                (new ulong[107u, 41u]),
                new float[][,,] { (new float[87u, 93u, 100u]), (new float[27u, 39u, 9u]) });
        }
        catch (Exception)
        {
        }
        try
        {
            AA.Static2();
        }
        catch (Exception)
        {
        }
        return 100;
    }
    public static long m_lFwd1;
    public static bool m_bFwd2;
    public static ulong m_ulFwd3;
    public static byte m_byFwd4;
    public static sbyte m_sbyFwd5;
    public static float m_fFwd6;
    public static char m_chFwd7;
    public static uint m_uFwd8;
    public static short m_shFwd9;
    public static ushort m_ushFwd10;
    public static TestEnum[,] m_axFwd11;
    public static int[,,][,,] m_aiFwd12;
}
