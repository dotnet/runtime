// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable 472

public class Bug7907
{
    int _position = 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int G(int z, ref int r)
    {
        r -= z;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int F0(int count)
    {
        int initialCount = count;

        _position += G(_position, ref count);

        if (initialCount == count)
        {
            count--;
        }

        return initialCount - count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int F1(int count)
    {
        // " != null" is known to be true - just to remove control flow
        // since that by itself may force spilling and mask the bug
        count -= (_position += G(_position, ref count)) != null ? count : 1;

        return count;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result0 = new Bug7907().F0(10);
        int result1 = new Bug7907().F1(10);
        Console.WriteLine("R0={0} R1={1}", result0, result1);
        return (result0 == 10 && result1 == 10 ? 100 : -1);
    }
}
