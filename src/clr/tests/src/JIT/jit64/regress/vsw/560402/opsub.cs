// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal unsafe class Test
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
            i = (uint)v + 0x7FFFFFE8;
        }
        return i;
    }

    public static int Main(string[] args)
    {
        byte* table = stackalloc byte[257];

        int index = 50;
        uint base1 = GetIndex(args.Length);
        uint base2 = GetIndex(args.Length + index);

        table[index] = 0;
        table[base2 - base1] = 53;

        bool passed = table[index] == 53;

        Console.WriteLine(passed ? "PASS" : "FAIL");
        return passed ? 100 : 1;
    }
}
