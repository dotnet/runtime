// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/72808.
// When inlining a callee that takes a struct argument but never uses it,
// the JIT was dropping the struct load (GT_BLK) from the call site entirely,
// swallowing a NullReferenceException that should have been thrown.

namespace Runtime_72808;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_72808
{
    struct S { }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            unsafe { Problem(null); }
        }
        catch (NullReferenceException)
        {
            return 100;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void Problem(S* pA) { Use(*pA); }

    private static void Use(S s) { }
}
