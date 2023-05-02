// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_73559
{
    [Fact]
    public static int TestEntryPoint()
    {
        long value = 0x1234567891011121;
        return Verify(*(S8*)&value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Verify(S8 val)
    {
        long asLong = *(long*)&val;
        if (asLong == 0x1234567891011121)
        {
            Console.WriteLine("PASS");
            return 100;
        }

        Console.WriteLine("FAIL: Value is {0:X}", asLong);
        return -1;
    }

    private struct S8
    {
        public int A, B;
    }
}