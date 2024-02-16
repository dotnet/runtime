// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

#pragma warning disable 1717
#pragma warning disable 0219

public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}

public class AA<TA, TB> where TA:  IComparable 
{
    public TB m_agboGeneric1;
    public static void Static1(Array param1, ref TB param2, ulong[,,] param3, ref 
        byte param4)
    {
        if (App.m_bFwd1)
            goto label1;
        label1:
        while (App.m_bFwd1)
        {
            TestEnum[,] local1 = (new TestEnum[28u, 111u]);
            long local2 = App.m_lFwd2;
            if (App.m_bFwd1)
                param3 = param3;
            else
            {
                uint local3 = 51u;
                TestEnum[,,][,][] local4 = (new TestEnum[116u, 1u, 102u][,][]);
                do
                {
                    throw new NullReferenceException();
                }
                while(App.m_bFwd1);
                param1 = param1;
                local2 -= local2;
                if (App.m_bFwd1)
                    try
                    {
                        Array[,][,][,,][][,][,][][] local5 = (new Array[83u, 103u]
                            [,][,,][][,][,][][]);
                        Array[,,] local6 = (new Array[30u, 6u, 41u]);
                        param3[34, 110, 27] = App.m_ulFwd3;
                        param1 = param1;
                        local6[80, 2, 50] = param1;
                        local1[63, 60] = TestEnum.red;
                    }
                    catch (NullReferenceException)
                    {
                        bool local7 = true;
                        if (local7)
                            continue;
                    }
                else
                    if (App.m_bFwd1)
                        if (App.m_bFwd1)
                            local3 = local3;
                        else
                            local2 -= local2;
            }
            param2 = param2;
            try
            {
                char[,,][,,,,,] local8 = (new char[35u, 100u, 38u][,,,,,]);
                char[,] local9 = (new char[97u, 33u]);
                ulong local10 = App.m_ulFwd3;
                param4 = param4;
                param3 = (new ulong[63u, 9u, 18u]);
                if (App.m_bFwd1)
                    do
                    {
                        param4 = param4;
                        local9 = (new char[125u, 118u]);
                        try
                        {
                            while (App.m_bFwd1)
                            {
                                do
                                {
                                    TestEnum local11 = 0;
                                    BB[] local12 = new BB[]{new BB() };
                                    object[][] local13 = (new object[33u][]);
                                }
                                while(App.m_bFwd1);
                                continue;
                            }
                            try
                            {
                                BB[,,][,,][][] local14 = (new BB[76u, 111u, 60u][,,][][]);
                            }
                            finally
                            {
                            }
                            while (App.m_bFwd1)
                            {
                            }
                        }
                        catch (Exception)
                        {
                        }
                        if (App.m_bFwd1)
                            do
                            {
                            }
                            while(App.m_bFwd1);
                    }
                    while(App.m_bFwd1);
                return;
            }
            catch (InvalidOperationException)
            {
            }
            try
            {
            }
            catch (Exception)
            {
            }
        }
        do
        {
        }
        while(App.m_bFwd1);
        ;
    }
    public static bool[,,,] Static2(bool[,][,,] param1, ref ulong[] param2, TA 
        param3, TB param4, TA param5, TB param6)
    {
        ulong local15 = App.m_ulFwd3;
        param3 = param5;
        local15 = local15;
        for (App.m_byFwd4-=App.m_byFwd4; App.m_bFwd1; App.m_iFwd5++)
        {
            BB[][,,][,,,] local16 = new BB[][,,][,,,]{(new BB[115u, 22u, 97u][,,,]), 
                (new BB[71u, 101u, 72u][,,,]), (new BB[94u, 124u, 8u][,,,]) };
            TestEnum[,,,,] local17 = (new TestEnum[62u, 63u, 7u, 49u, 79u]);
            for (App.m_fFwd6=50.0f; App.m_bFwd1; App.m_iFwd5*=121)
            {
                sbyte local18 = App.m_sbyFwd7;
                ushort local19 = App.m_ushFwd8;
                for (App.m_lFwd2=App.m_lFwd2; App.m_bFwd1; App.m_chFwd9-='\x09')
                {
                    continue;
                }
            }
            do
            {
                BB[,][][][,,][] local20 = (new BB[90u, 91u][][][,,][]);
                for (App.m_iFwd5--; App.m_bFwd1; App.m_byFwd4++)
                {
                    BB[,] local21 = (new BB[124u, 91u]);
                    long[,][,][][,] local22 = (new long[115u, 83u][,][][,]);
                    for (App.m_shFwd10=App.m_shFwd10; App.m_bFwd1; App.m_ushFwd8++)
                    {
                        char[][][,,] local23 = new char[][][,,]{ };
                        TestEnum[,,][,][,,,][] local24 = (new TestEnum[29u, 90u, 44u][,][,,,][]);
                        object[][] local25 = new object[][]{(new object[92u]), new object[]{null, 
                            null, null, null } };
                        try
                        {
                            ulong local26 = App.m_ulFwd3;
                            while (App.m_bFwd1)
                            {
                                ushort[,] local27 = (new ushort[35u, 110u]);
                                long[,] local28 = (new long[73u, 43u]);
                                param4 = param4;
                                throw new IndexOutOfRangeException();
                            }
                            if (App.m_bFwd1)
                                for (App.m_fFwd6+=7.0f; App.m_bFwd1; App.m_byFwd4++)
                                {
                                    throw new DivideByZeroException();
                                }
                        }
                        catch (DivideByZeroException)
                        {
                            do
                            {
                                try
                                {
                                    local21[123, 75].m_axField1[73] = new Array[][,,,]{(new Array[102u, 56u
                                        , 82u, 63u]) };
                                }
                                finally
                                {
                                }
                                local22 = local22;
                                try
                                {
                                }
                                catch (DivideByZeroException)
                                {
                                }
                                try
                                {
                                }
                                catch (IndexOutOfRangeException)
                                {
                                }
                            }
                            while(App.m_bFwd1);
                            throw new NullReferenceException();
                        }
                        while (App.m_bFwd1)
                        {
                        }
                    }
                    do
                    {
                    }
                    while(App.m_bFwd1);
                    do
                    {
                    }
                    while(App.m_bFwd1);
                    local15 = local15;
                }
                while (App.m_bFwd1)
                {
                }
                while (App.m_bFwd1)
                {
                }
                throw new InvalidOperationException();
            }
            while(App.m_bFwd1);
        }
        param5 = param5;
        return (new bool[38u, 121u, 77u, 3u]);
    }
    public static sbyte Static3(uint param1, ref short param2)
    {
        BB local29 = new BB();
        try
        {
            param1 -= 33u;
        }
        catch (NullReferenceException)
        {
            ulong[] local30 = (new ulong[122u]);
            byte local31 = App.m_byFwd4;
            for (App.m_lFwd2++; App.m_bFwd1; App.m_chFwd9-='\x6c')
            {
                TestEnum[,,][][] local32 = (new TestEnum[109u, 121u, 75u][][]);
                ushort[,,,,][][][] local33 = (new ushort[44u, 28u, 97u, 45u, 88u][][][]);
                throw new Exception();
            }
            goto label2;
        }
        label2:
        return App.m_sbyFwd7;
    }
    public static int Static4(TB param1, ref Array[][,,][,,] param2, ref TB param3
        , char param4)
    {
        ushort[,,][,][][] local34 = (new ushort[18u, 54u, 39u][,][][]);
        short local35 = App.m_shFwd10;
        return 80;
    }
}

public struct BB
{
    public Array[][][,,,] m_axField1;
    public static void Static1(double[][,,] param1, ref bool param2, TestEnum 
        param3, ref float param4)
    {
        if (param2)
            param2 = param2;
        else
        {
            for (App.m_ushFwd8+=App.m_ushFwd8; param2; App.m_ushFwd8++)
            {
                sbyte[][] local36 = new sbyte[][]{(new sbyte[14u]) };
                ushort[,,] local37 = (new ushort[113u, 103u, 65u]);
                String local38 = "108";
                param2 = param2;
            }
            while (param2)
            {
                while (param2)
                {
                    ushort local39 = App.m_ushFwd8;
                    if (param2)
                        try
                        {
                            BB local40 = new BB();
                            ushort local41 = App.m_ushFwd8;
                            local41 += local41;
                            do
                            {
                                for (App.m_byFwd4++; param2; App.m_lFwd2*=App.m_lFwd2)
                                {
                                    ulong[,][][] local42 = (new ulong[60u, 24u][][]);
                                    Array local43 = App.m_xFwd11;
                                    try
                                    {
                                        Array[] local44 = (new Array[100u]);
                                        int local45 = 17;
                                        BB[][,][,][][,][] local46 = new BB[][,][,][][,][]{ };
                                        local43 = local43;
                                    }
                                    catch (Exception)
                                    {
                                        String[,,,,] local47 = (new String[91u, 102u, 91u, 75u, 88u]);
                                        Array[,][] local48 = (new Array[125u, 114u][]);
                                        object local49 = null;
                                        for (App.m_chFwd9+='\x2b'; param2; App.m_chFwd9++)
                                        {
                                            param2 = true;
                                            local39 += local39;
                                            try
                                            {
                                                param2 = param2;
                                            }
                                            finally
                                            {
                                            }
                                        }
                                        while (param2)
                                        {
                                        }
                                    }
                                    local40 = new BB();
                                    while (param2)
                                    {
                                    }
                                    while (param2)
                                    {
                                    }
                                }
                                try
                                {
                                }
                                catch (Exception)
                                {
                                }
                                return;
                            }
                            while(param2);
                            local40.m_axField1[10][53][56, 118, 114, 54] = App.m_xFwd11;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    else
                        throw new Exception();
                    do
                    {
                    }
                    while(param2);
                }
                if (param2)
                    try
                    {
                    }
                    catch (Exception)
                    {
                    }
                try
                {
                }
                catch (Exception)
                {
                }
            }
            param3 = 0;
        }
        while (param2)
        {
        }
        if (param2)
            throw new DivideByZeroException();
        param4 /= param4;
        ;
    }
}

public class App
{
    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            AA<char, bool>.Static1(
                App.m_xFwd11,
                ref App.m_agboFwd12,
                (new ulong[50u, 40u, 98u]),
                ref App.m_byFwd4 );
        }
        catch (Exception)
        {
        }
        try
        {
            AA<char, bool>.Static2(
                (new bool[72u, 126u][,,]),
                ref App.m_aulFwd13,
                App.m_gcFwd14,
                App.m_agboFwd12,
                App.m_gcFwd14,
                App.m_agboFwd12 );
        }
        catch (Exception)
        {
        }
        try
        {
            AA<char, bool>.Static3(
                34u,
                ref App.m_shFwd10 );
        }
        catch (Exception)
        {
        }
        try
        {
            AA<char, bool>.Static4(
                App.m_agboFwd12,
                ref App.m_axFwd15,
                ref App.m_agboFwd12,
                '\x50' );
        }
        catch (Exception)
        {
        }
        try
        {
            BB.Static1(
                (new double[101u][,,]),
                ref App.m_bFwd1,
                TestEnum.red,
                ref App.m_fFwd6 );
        }
        catch (Exception)
        {
        }
    }
    public static bool m_bFwd1;
    public static long m_lFwd2;
    public static ulong m_ulFwd3;
    public static byte m_byFwd4;
    public static int m_iFwd5;
    public static float m_fFwd6;
    public static sbyte m_sbyFwd7;
    public static ushort m_ushFwd8;
    public static char m_chFwd9;
    public static short m_shFwd10;
    public static Array m_xFwd11;
    public static bool m_agboFwd12;
    public static ulong[] m_aulFwd13;
    public static char m_gcFwd14;
    public static Array[][,,][,,] m_axFwd15;
}
