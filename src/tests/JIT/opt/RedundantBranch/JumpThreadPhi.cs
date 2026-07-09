// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class JumpThreadPhi
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Phi_00(int value)
    {
        int idx = BitOperations.TrailingZeroCount(value);
        idx = (idx != 32) ? idx : -1;

        if (idx != -1)
        {
            return idx + 100;
        }

        return -1;
    }

    [Fact]
    public static void TestPhi00()
    {
        Assert.Equal(-1, Phi_00(0));
        Assert.Equal(100, Phi_00(1));
        Assert.Equal(103, Phi_00(8));
    }
}
