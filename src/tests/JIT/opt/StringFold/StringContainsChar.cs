// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class StringContainsChar
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ContainsABCDEF(char c)
    {
        // "ABCDEF" is a continuous range, so the call is folded into the range
        // check `c >= 'A' && c <= 'F'` and the String.Contains call is elided.
        // X64-NOT: call
        // X64: cmp
        return "ABCDEF".Contains(c);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (!ContainsABCDEF('A') || !ContainsABCDEF('C') || !ContainsABCDEF('F'))
            return 0;

        if (ContainsABCDEF('@') || ContainsABCDEF('G') || ContainsABCDEF('a'))
            return 0;

        return 100;
    }
}
