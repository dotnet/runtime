// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_Shift
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shl64(ulong shift, int count)
    {
        return shift << count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shl64_32_inplace(ulong shift, ulong addit)
    {
        ulong x = shift + addit;
        x = x << 32;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shl64_33_inplace(ulong shift, ulong addit)
    {
        ulong x = shift + addit;
        x = x << 33;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shr64(ulong shift, int count)
    {
        return shift >> count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shr64_32_inplace(ulong shift, ulong addit)
    {
        ulong x = shift + addit;
        x = x >> 32;
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong shr1_32_add(ulong shift, ulong addit)
    {
        ulong x = (addit + (shift >> 1)) >> 31;
        return x;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (shl64_32_inplace(0x123456789abcdef, 0) != shl64(0x123456789abcdef, 32))
        {
            Console.WriteLine("shl64_32");
            return Fail;
        }

        if (shl64_33_inplace(0x123456789abcdef, 0) != shl64(0x123456789abcdef, 33))
        {
            Console.WriteLine("shl64_33");
            return Fail;
        }

        if (shr64_32_inplace(0x123456789abcdef, 0) != shr64(0x123456789abcdef, 32))
        {
            Console.WriteLine("shr64_32 {0:X} {1:X}", shr64_32_inplace(0x123456789abcdef, 0), shr64(0x123456789abcdef, 32));
            return Fail;
        }

        if (shr1_32_add(0x123456789abcdef, 0) != shr64(0x123456789abcdef, 32))
        {
            Console.WriteLine("HAHAHAHAHAHAHA {0:X}", shr1_32_add(0x123456789abcdef, 0));
            return Fail;
        }

        return Pass;
    }
}
