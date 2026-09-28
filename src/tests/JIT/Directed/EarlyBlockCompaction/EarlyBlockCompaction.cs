// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class EarlyBlockCompaction
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Select(bool condition)
    {
        if (condition)
        {
            return 100;
        }

        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Compact(bool condition)
    {
        // Inlining Select introduces blocks that are compacted before the first
        // liveness analysis. Copying their uninitialized live-out sets must not
        // read an uninitialized tracked-local bitset size.
        return Select(condition);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return (Compact(true) == 100) && (Compact(false) == 101) ? 100 : -1;
    }
}
