// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134818
{
    private static sbyte s_sink;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Compute(sbyte sb, sbyte d)
    {
        // The compound assignment leaves behind a bare `sb = tmp` local copy, and the
        // next statement reads `sb` twice. On 32 bit targets TYP_I_IMPL is TYP_INT, so
        // forward substitution treats that copy as a cheap reorderable address tree and
        // selects its multi-use path, which requires the substituted tree to still be a
        // local. Normalizing `sb` on store then wraps the tree in a cast.
        s_sink = (sbyte)((sbyte)(sb -= d) * (sbyte)(sb * sb));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Compute(10, 3);
        return s_sink == 87 ? 100 : -1;
    }
}
