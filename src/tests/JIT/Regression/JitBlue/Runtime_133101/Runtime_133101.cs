// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133101;

public class Runtime_133101
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(4, LoopInTryAndFinally(3));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long LoopInTryAndFinally(long a)
    {
        long x = a;
        try
        {
        L1:
            if ((x & 8) != 0)
            {
                x += 4;
                goto L1;
            }
        }
        finally
        {
        L2:
            if ((x & 1) != 0)
            {
                x += 1;
                goto L2;
            }
        }

        return x;
    }
}
