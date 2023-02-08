// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint PerformMod_1(uint i)
    {
        // X64-FULL-LINE:      mov [[REG0:[a-z]+]], [[REG1:[a-z0-9]+]]
        // X64-FULL-LINE-NEXT: and [[REG0]], 7

        // ARM64-FULL-LINE: and w0, w0, #7

        return i % 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PerformMod_2(int i)
    {
        // X64-FULL-LINE:              mov [[REG0:[a-z]+]], [[REG1:[a-z]+]]
        // X64-FULL-LINE-NEXT:         sar [[REG0]], 31
        // X64-FULL-LINE-NEXT:         and [[REG0]], 15
        // X64-FULL-LINE-NEXT:         add [[REG0]], [[REG1]]
        // X64-FULL-LINE-NEXT:         and [[REG0]], -16
        // X64-WINDOWS-FULL-LINE-NEXT: mov [[REG2:[a-z]+]], [[REG1]]
        // X64-WINDOWS-FULL-LINE-NEXT: sub [[REG2]], [[REG0]]
        // X64-WINDOWS-FULL-LINE-NEXT: mov [[REG0]], [[REG2]]
        // X64-LINUX-FULL-LINE-NEXT:   sub [[REG1]], [[REG0]]
        // X64-LINUX-FULL-LINE-NEXT:   mov [[REG0]], [[REG1]]
        // X64-OSX-FULL-LINE-NEXT:     sub [[REG1]], [[REG0]]
        // X64-OSX-FULL-LINE-NEXT:     mov [[REG0]], [[REG1]]

        // ARM64-FULL-LINE:      and w1, w0, #15
        // ARM64-FULL-LINE-NEXT: negs w0, w0
        // ARM64-FULL-LINE-NEXT: and w0, w0, #15
        // ARM64-FULL-LINE-NEXT: csneg w0, w1, w0, mi

        return i % 16;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PerformMod_3(int i, int j)
    {
        return i % j;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MSUB(int a, int b, int c)
    {
        // X64-FULL-LINE:      imul [[REG0:[a-z]+]], [[REG1:[a-z0-9]+]]
        // X64-FULL-LINE-NEXT: mov [[REG2:[a-z]+]], [[REG3:[a-z]+]]
        // X64-FULL-LINE-NEXT: sub [[REG2]], [[REG0]]

        // ARM64-FULL-LINE: msub w0, w1, w2, w0

        return a - b * c;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var result = 100;

        if (PerformMod_1(23) != 7)
        {
            result = -1;
            Console.WriteLine("Failed Mod1!");
        }

        if (PerformMod_2(-23) != -7)
        {
            result = -1;
            Console.WriteLine("Failed Mod2!");
        }

        if (PerformMod_3(23, 8) != 7)
        {
            result = -1;
            Console.WriteLine("Failed Mod3!");
        }

        if (MSUB(3, 7, 8) != -53)
        {
            result = -1;
            Console.WriteLine("Failed MSUB");
        }

        return result;
    }
}
