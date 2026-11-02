// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_136041
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, Test(new int[] { 1 }, 0));
        Assert.Equal(1, Test(new int[] { 1 }, 1));
    }

    // Loop inversion splits the catch handler entry to create the loop preheader,
    // then the loop condition folds to false, leaving an unreachable block that
    // inherited the handler entry's "don't remove" flag.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(int[] p, int d)
    {
        int result = 0;
        try
        {
            result = 1 / d;
            GC.KeepAlive(p);
        }
        catch (DivideByZeroException)
        {
            while (checked(p[0] > p[0]))
            {
                try
                {
                    GC.Collect();
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }

        return result;
    }
}
