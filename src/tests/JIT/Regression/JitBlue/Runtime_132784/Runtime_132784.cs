// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_132784;

public class Runtime_132784
{
    // The "p1 == 0xFFFE008000000000" assertion is a TYP_LONG constant. Range inference used to
    // truncate it to its low 32 bits (0), conclude that "0 - p1" is 0, and fold "v4 != v2" to
    // false - sending control down the b5 path that can never be reached at runtime.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong Test(ulong p1)
    {
        int v6 = 0, v13 = 0, v21 = 0, count = 0;
        ulong v2 = 0, v4 = 0, v42 = 0;

    b0:
        v4 = 0 - p1;
        v6 = p1 == 0xFFFE008000000000UL ? 1 : 0;
        if (v6 != 0) goto b1;
        goto b4;
    b1:
        v13 = v4 != v2 ? 1 : 0;
        if (v13 != 0) goto b2;
        goto b5;
    b2:
        if (v21 != 0) goto b7;
        goto b6;
    b4:
        goto b0;
    b5:
        // Only reachable if the JIT mis-folded the branch above; bail out so we don't spin forever.
        count++;
        if (count > 10) return 0xBAD;
        v42 = 0xBAD;
        goto b1;
    b6:
        return 0;
    b7:
        return v42;
    }

    [Fact]
    public static void LongAssertionConstantIsNotTruncated()
    {
        Assert.Equal(0UL, Test(0xFFFE008000000000UL));
    }
}
