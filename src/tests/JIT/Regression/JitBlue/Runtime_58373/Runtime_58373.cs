// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public unsafe class Runtime_58373
{
    public static int Main()
    {
        FillStack(0, 0, 0, 0, 0, 0, 0xdeadbeef);
        short val1 = HalfToInt16Bits(0, 0, 0, 0, 0, 0, (Half)42f);
        FillStack(0, 0, 0, 0, 0, 0, 0xf000baaa);
        short val2 = HalfToInt16Bits(0, 0, 0, 0, 0, 0, (Half)42f);

        return val1 == val2 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FillStack(int a0, int a1, int a2, int a3, int a4, int a5, uint onStack)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short HalfToInt16Bits(int a0, int a1, int a2, int a3, int a4, int a5, Half h)
    {
        return *(short*)&h;
    }
} 