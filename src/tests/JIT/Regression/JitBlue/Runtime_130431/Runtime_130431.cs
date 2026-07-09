// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_130431
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<OverflowException>(() => Add(1, uint.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint Add(uint x, uint y)
    {
        uint result = checked(y + unchecked((byte)x));

        if (x == 0)
        {
            return result + 1;
        }

        return result;
    }
}
