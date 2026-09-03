// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133131;

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133131
{
    private static int s_v;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void TailMergePickSelfLoopAsVictim(int x)
    {
        if (x == 1)
        {
            goto P;
        }
        if (x == 2)
        {
            s_v = 77;
            goto L;
        }
        return;
    P:
        s_v = 1;
        goto L;
    L:
        s_v = 1;
        goto L;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void TailMergeDontRemoveSelfLoop(int x)
    {
        if (x >= 0)
        {
            goto P;
        }
        return;
    L:
        s_v = 1;
        goto L;
    P:
        s_v = 1;
        goto L;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        s_v = 0;
        TailMergePickSelfLoopAsVictim(-1);
        Assert.Equal(s_v, 0);

        s_v = 0;
        TailMergeDontRemoveSelfLoop(-1);
        Assert.Equal(s_v, 0);
    }
}
