// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_132841;

public class Runtime_132841
{
    // Math.Abs(x1) here is only ever reached with x1 == 1.
    private static int H1(int x0, int x1)
    {
        int r0 = x0 - x1;
        int r1 = Math.Abs(x1);
        int r2 = unchecked((int)0xBFFEE70F) ^ r0;
        int r3 = Math.Clamp(r1, r2, int.MaxValue);
        return 0 ^ r3;
    }

    // Range inference derived "v3 >= 0x91960 + 2" for the wrapping ADD below (a range that only
    // holds if the ADD does not overflow) and then negated it for "Math.Abs(v3)". The overflow
    // check that is supposed to reject such ranges stopped at the NEG instead of walking into the
    // ADD, so "-v3 < 0" was folded to always-true and Math.Abs unconditionally threw.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int F(int p0, int p1)
    {
        unchecked
        {
            // Establishes range assertions on p0 (requires min=2 <= max=p0).
            int v2 = Math.Clamp(0, 2, p0);

            // Wraps for large p0: 0x91960 + 2147168000 -> -2147202976, which is strictly greater
            // than int.MinValue, so Math.Abs(v3) is well defined.
            int v3 = unchecked((int)0x00091960) + p0;
            if (v3 >= p0) goto b10;

            int v8 = Math.Abs(v3);
            int v9 = Math.Abs(-1);

            if (0 >= p1) goto b10;

            return H1(v8, v9);

        b10:
            return v2 & 0;
        }
    }

    [Fact]
    public static void WrappingAddDoesNotProveAbsOverflows()
    {
        Assert.Equal(1, F(2147168000, 290901));
    }
}
