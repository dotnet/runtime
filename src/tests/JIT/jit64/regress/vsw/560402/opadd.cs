// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Test
{
    private static uint GetIndex(int v)
    {
        uint i = 0;
        try
        {
            i = 100 / (uint)(v + 1);
        }
        catch (Exception)
        {
        }
        finally
        {
            i = UInt32.MaxValue / 2 + 1U + (uint)v;
        }
        return i;
    }

    [Fact]
    public static int TestEntryPoint() => Run(new string[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(string[] args)
    {
        byte* table = stackalloc byte[257];

        int index = 50;
        uint base1 = GetIndex(args.Length);
        uint base2 = GetIndex(args.Length + index);

        table[index] = 0;
        table[base2 + base1] = 53;

        bool passed = table[index] == 53;

        Console.WriteLine(passed ? "PASS" : "FAIL");
        return passed ? 100 : 1;
    }
}
