// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;

public class Test
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

    public static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (shl64_32_inplace(0x123456789abcdef, 0) != shl64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        if (shl64_33_inplace(0x123456789abcdef, 0) != shl64(0x123456789abcdef, 33))
        {
            return Fail;
        }

        if (shr64_32_inplace(0x123456789abcdef, 0) != shr64(0x123456789abcdef, 32))
        {
            return Fail;
        }

        return Pass;
    }
}
