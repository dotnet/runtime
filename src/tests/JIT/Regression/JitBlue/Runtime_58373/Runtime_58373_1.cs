// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public unsafe class Runtime_58373
{
    public static int Main()
    {
        short halfValue = HalfToInt16Bits(MakeHalf());
        int x = halfValue;
        short val2 = HalfToInt16Bits(*(Half*)&x);

        return halfValue == val2 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Half MakeHalf()
    {
        return (Half)(-1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short HalfToInt16Bits(Half h)
    {
        return *(short*)&h;
    }
}  