// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DownwardsLoopMultiplyExecutedExiting
{
    [Fact]
    public static int ExitFromNestedLoop()
    {
        int n = Get10();
        int i = 0;
        int result = 0;
        if (n < 0)
            return -1;

        do
        {
            int j = 0;
            do
            {
                if (i >= n)
                    return result;

                j++;
                result++;
            } while (j < 10);

            i++;
        } while (true);
    }

    [Fact]
    public static int ExitFromNestedIrreducibleLoop()
    {
        int n = Get10();
        int i = 0;
        int result = -1;
        if (n < 0)
            return -1;

        do
        {
            int j = 0;
            if (AlwaysFalse())
                goto InsideLoop;

            LoopHeader:
            j++;
            result++;

            InsideLoop:;
            if (i >= n)
                return result;

            if (j < 10)
                goto LoopHeader;

            i++;
        } while (true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AlwaysFalse() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Get10() => 10;
}
