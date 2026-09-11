// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Redundant branch opts walks up the dominator tree and may simplify an
// intermediate dominating relop (e.g. rewrite `x < 0` and `x != 0` into `x > 0`).
// While doing so it rewrote the start block's relop but kept reasoning about a
// stale value number, which let it unsoundly fold a higher dominating guard
// `if (x == -1)`. As a result `Validate(-1)` threw instead of returning.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_128062
{
    // Mirrors StreamReader.ValidateArgsAndOpenPath's buffer-size check.
    // AggressiveOptimization forces FullOpts so the buggy phase (RBO) runs.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Validate(int bufferSize)
    {
        if (bufferSize != -1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        }

        return bufferSize;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // -1 is the sentinel and must be accepted without throwing.
        Assert.Equal(-1, Validate(-1));

        // Positive values are accepted.
        Assert.Equal(1024, Validate(1024));

        // Zero and negatives (other than -1) still throw.
        Assert.Throws<ArgumentOutOfRangeException>(() => { Validate(0); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { Validate(-2); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { Validate(int.MinValue); });
    }
}
