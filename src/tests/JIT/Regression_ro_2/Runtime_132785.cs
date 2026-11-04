// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_132785;

public class Runtime_132785
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long F(long p0, byte p1)
    {
        byte v69 = 0;
        long v3 = 0, v7 = 0, v56 = 0, v79 = 0, v106 = 0;
        int v6 = 0, v8 = 0, v16 = 0, v21 = 0, v36 = 0, v52 = 0;

        v6 = p0 > v3 ? 1 : 0;
        v7 = v6;
        v8 = v7 != 0 ? 1 : 0;
        if (v8 != 0) goto b1;
        goto b4;
    b1:
        v16 = p0 >= v3 ? 1 : 0;
        if (v16 != 0) goto b2;
        goto b4;
    b2:
        if (v21 != 0) goto b6;
        goto b9;
    b4:
        if (v36 != 0) goto b9;
        goto b10;
    b6:
        if (v52 != 0) goto b11;
        goto b14;
    b9:
        v69 = (byte)(v69 + p1);
        goto b11;
    b10:
        v79 = -101479 ^ v56;
        return v79;
    b11:
        goto b15;
    b14:
        return v106;
    b15:
        goto b2;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(-101479L, F(0, 0));
    }
}
