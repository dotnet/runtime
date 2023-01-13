// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

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
    static void Div_SmallType_Correctness()
    {
        for (int i = 1; i < ushort.MaxValue + 1; i++)
        {
            if ((byte)(0 / i) != (byte)((byte)0 / (byte)i))
            {
                throw new Exception();
            }
        }
    }

    static int Main()
    {
        Add_SmallType_Correctness();
        Sub_SmallType_Correctness();
        Mul_SmallType_Correctness();

        try
        {
            Div_SmallType_Correctness();
        }
        catch(DivideByZeroException) {}

        return 100;
    }
}
