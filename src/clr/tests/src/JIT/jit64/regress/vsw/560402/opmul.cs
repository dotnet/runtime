// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal unsafe class Test
{
    public static int Main(string[] args)
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
