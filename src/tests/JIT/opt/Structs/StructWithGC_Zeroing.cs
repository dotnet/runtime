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
        LargeStructWithGC ls1 = default;
        ls1.str = "hello1";
        ls1.z2 = long.MaxValue;
        ZeroIt(ref ls1);

        LargeStructWithGC2 ls2 = default;
        ls2.str = "hello2";
        ls2.z1 = long.MinValue;
        ZeroIt2(ref ls2);

        if (ls1.str != null || ls2.str != null || ls1.z2 != 0 || ls2.z1 != 0)
            throw new InvalidOperationException("should be zeroed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ZeroIt(ref LargeStructWithGC s)
    {
        // X64-NOT: CORINFO_HELP_MEMSET
        // ARM64-NOT: CORINFO_HELP_MEMSET
        s = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ZeroIt2(ref LargeStructWithGC2 s)
    {
        // X64-NOT: CORINFO_HELP_MEMSET
        // ARM64-NOT: CORINFO_HELP_MEMSET
        s = default;
    }

    struct LargeStructWithGC // 360 bytes (64-bit)
    {
        public string str;
        public long b1, c1, d1, e1, f1, g1, h1, i1, j1, k1, l1, m1, n1, o1, p1, r1, s1, t1, u1, v1, w1, z1;
        public long b2, c2, d2, e2, f2, g2, h2, i2, j2, k2, l2, m2, n2, o2, p2, r2, s2, t2, u2, v2, w2, z2;
    }

    unsafe struct LargeStructWithGC2 // 4184 bytes (64-bit)
    {
        public fixed byte data[4000];
        public string str;
        public long b1, c1, d1, e1, f1, g1, h1, i1, j1, k1, l1, m1, n1, o1, p1, r1, s1, t1, u1, v1, w1, z1;
    }
}
