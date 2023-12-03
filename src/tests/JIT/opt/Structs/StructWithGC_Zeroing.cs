// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class StructWithGC_Zeroing
{
    [Fact]
    public static void StructZeroingShouldNotUseMemset()
    {
        var largeStructWithGc = new LargeStructWithGC();
        Test(ref largeStructWithGc);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(ref LargeStructWithGC s)
    {
        // X64-NOT: CORINFO_HELP_MEMSET
        s = default;
    }

    struct LargeStructWithGC
    {
        public byte x;
        public string a;
        public long b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, r, s, t, u, v, w, z;
    }
}
