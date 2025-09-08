// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_58373
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Use up a lot of registers
        int a = GetVal();
        int b = GetVal();
        int c = GetVal();
        int d = GetVal();
        int e = GetVal();
        int f = GetVal();
        int g = GetVal();
        int h = GetVal();
        int i = GetVal();

        short val1 = HalfToInt16Bits(MakeHalf());
        Half half = MakeHalf();
        MakeHalf(); // This will spill lower 16 bits of 'half' to memory
        short val2 = HalfToInt16Bits(half); // This will pass 32 bits as arg with upper 16 bits undefined

        return val1 == val2 ? 100 + a + b + c + d + e + f + g + h + i : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int GetVal()
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Half MakeHalf()
    {
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short HalfToInt16Bits(Half h)
    {
        return *(short*)&h;
    }
}  