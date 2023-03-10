// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public unsafe class Runtime_61359
{
    public static int Main()
    {
        return HalfToInt16Bits((Half)(-1)) == -17408 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short HalfToInt16Bits(Half h)
    {
        return *(short*)&h;
    }
}  