// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_111352
{
    [Fact]
    public static int Test1() => Problem1(0x1_0000_0001L, 0x2_0000_0001L);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Problem1(long x, long y)
    {
        if ((uint)x == (uint)y)
        {
            if (x == y)
            {
                return -1;
            }
        }

        return 100;
    }

    [Fact]
    public static int Test2() => Problem2(0x1_0000_0000L);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Problem2(long x)
    {
        if ((uint)x == 0)
        {
            if (x == 0)
            {
                return -1;
            }
        }

        return 100;
    }
}
