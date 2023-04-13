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
    public String[][][] m_axField1;
    public static double[,][] Static1(TypedReference param1)
    {
        object[,,][,,,][][,,,] local2 = (new object[((uint)(30.0f)), 99u, 71u]
            [,,,][][,,,]);
        return (new double[((uint)(90.0f)), ((uint)(((byte)((0.0f)))))][]);
    }
    public static void Static2(int param1, ref uint[,] param2, ref short param3)
    {
        uint local12 = 55u;

        AA.Static1(((/*2 REFS*/((byte)(local12)) != /*2 REFS*/((byte)(local12))) ?
            __refvalue(__makeref(param1), TypedReference) : __makeref(param3)));
        ;
    }
}

[StructLayout(LayoutKind.Sequential)]
public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            AA.Static2(
                8,
                ref App.m_auFwd8,
                ref App.m_shFwd1);
        }
        catch (Exception)
        {
        }
        return 100;
    }
    public static short m_shFwd1;
    public static uint[,] m_auFwd8;
}
