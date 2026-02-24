// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Verify that ifConversion does not speculatively hoist potentially invalid
// byrefs, as those may lead to GC crashes.

public class ByrefConversion
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte Problem(ref byte x, int len)
    {
        // X64-NOT: cmov
        // ARM64-NOT: csel
        ref byte t = ref (len == 0 ?
            ref Unsafe.NullRef<byte>() :
            ref Unsafe.Add(ref x, 100));
        return t;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ref byte ProblemRef(ref byte x, int len)
    {
        // X64-NOT: cmov
        // ARM64-NOT: csel
        return ref (len == 0 ?
            ref Unsafe.NullRef<byte>() :
            ref Unsafe.Add(ref x, 100));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        byte[] arr = new byte[256];
        arr[100] = 42;

        if (Problem(ref arr[0], 1) != 42)
            return 0;

        if (Unsafe.IsNullRef(ref ProblemRef(ref arr[0], 0)))
            return 100;

        return 0;
    }
}
