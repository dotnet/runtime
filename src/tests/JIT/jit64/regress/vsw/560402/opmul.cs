// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Test
{
    [Fact]
    public static int TestEntryPoint() => Run(new string[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(string[] args)
    {
        byte* table = stackalloc byte[257];

        uint i = (uint)args.Length + 0x80000030;
        uint index = i % 257;

        table[index] = 0;
        table[i - (i / 257) * 257] = 53;

        bool passed = table[index] == 53;

        Console.WriteLine(passed ? "PASS" : "FAIL");
        return passed ? 100 : 1;
    }
}
