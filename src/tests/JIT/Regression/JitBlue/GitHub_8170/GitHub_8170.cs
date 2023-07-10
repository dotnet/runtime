// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// This test checks for proper behavior w.r.t. overflow for expressions of the form `val % constant` where `val` is an
// unsigned long and `constant` is a 32- or 64-bit integer constant in the range [2,0x3fffffff]. These expressions
// should never produce an overflow exception.

public static class C
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint M(ulong l)
    {
        return (uint)(l % 10000000);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return M(ulong.MaxValue) == 9551615 ? 100 : 101;
    }
}
