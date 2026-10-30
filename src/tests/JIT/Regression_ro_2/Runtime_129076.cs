// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VN was simplifying NOT(relop(x,y)) to Reverse(relop)(x,y). GT_NOT is
// bitwise complement, not logical negation: ~(x relop y) produces -1 or
// -2 for a 0/1 relop result, while Reverse(relop)(x,y) produces 0 or 1,
// so downstream uses comparing the value arithmetically (here `~v3 >= -1`)
// folded the wrong way.

namespace Runtime_129076;

using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_129076
{
    private static volatile int Input_p0 = unchecked((int)0x800335C5);
    private static volatile int Input_p1 = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Func(int p0, int p1)
    {
        unchecked
        {
            int v1 = unchecked((int)0x000335C5) & p1;
            int v3 = (v1 != p0) ? 1 : 0;
            if (v3 == 0) return 99;
            int v4 = ~v3;
            return (v4 >= -1) ? 1 : 0;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Func(Input_p0, Input_p1) == 0 ? 100 : 1;
    }
}
