// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The original repro for this test was an assertion after code generation that the actual maximum depth of the stack
// was less than or identical to the estimated depth of the stack as calculated during morph. The calculation was
// incorrect when a math intrinsic was used as an argument to a function with on-stack parameters (e.g. the call to
// `M` on line 18).

public static class C
{
    struct S
    {
        int a, b, c, d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int N(S s, float d)
    {
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(double d)
    {
        N(new S(), (float)(Math.Atan2(d, 2.0) * 180 / Math.PI));
        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return M(2.0);
    }
}
