// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Add_SmallType_Correctness()
    {
        for (int i = 0; i < ushort.MaxValue + 1; i++)
        {
            for (int j = 0; j < ushort.MaxValue + 1; j++)
            {
                if ((byte)(i + j) != (byte)((byte)i + (byte)j))
                {
                    throw new Exception();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Sub_SmallType_Correctness()
    {
        for (int i = 0; i < ushort.MaxValue + 1; i++)
        {
            for (int j = 0; j < ushort.MaxValue + 1; j++)
            {
                if ((byte)(i - j) != (byte)((byte)i - (byte)j))
                {
                    throw new Exception();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Mul_SmallType_Correctness()
    {
        for (int i = 0; i < ushort.MaxValue + 1; i++)
        {
            for (int j = 0; j < ushort.MaxValue + 1; j++)
            {
                if ((byte)(i * j) != (byte)((byte)i * (byte)j))
                {
                    throw new Exception();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Div_SmallType_Correctness(int i, int j)
    {
        if ((byte)(i / j) != (byte)((byte)i / (byte)j))
        {
            throw new Exception();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Add_SmallType_Correctness();
        Sub_SmallType_Correctness();
        Mul_SmallType_Correctness();

        try
        {
            Div_SmallType_Correctness(2, 256);
        }
        catch(DivideByZeroException) {}

        return 100;
    }
}
