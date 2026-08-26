// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class HotColdSplitting
{
    private static int s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MethodWithColdThrow(int value)
    {
        if (value != 0)
        {
            return value + 1;
        }

        int coldValue;
        if ((value & 2) == 0)
        {
            coldValue = RareTransform(value + 1);
        }
        else
        {
            coldValue = RareTransform(value - 1);
        }

        s_sink = coldValue;

        throw new InvalidOperationException(coldValue.ToString());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RareTransform(int value) => (value * 3) ^ 0x5A5A5A5A;
}
