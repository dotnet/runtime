// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Some comments about the test:
 * Expected: The code runs and completes successfully.
 * Actual: The application hangs when it is run.
 * The csharp compiler seems to be generating the same IL in both the cases.
 *
 * Some comments about the bug:
 * The problem is that the way we use ComputePreferredZapModule grows exponentially in this test case.  The thing with the inliner is a red herring.  If you increase the constant from 5 to 15 it also hangs.  The only reason that it's worse with the inliner is because of the way we handle the unbounded recursion.  Once you get about 5 deep you go all the way infinitely (because the call site size is always smaller than the estimated function body).
 *
 */

using System;
using Xunit;

public struct Tuple<T0, T1>
{
    public readonly T0 Field0;
    public readonly T1 Field1;
    public Tuple(T0 Field0, T1 Field1)
    {
        this.Field0 = Field0;
        this.Field1 = Field1;
    }
}

public static class M
{
    [Fact]
    public static int TestEntryPoint()
    {
        return meth<int>(8, 100);
        //Console.Write(meth<int>(8, 100));
        //Console.Write(meth<int>(5, 1)); Increasing levels to 8
    }

    private static T meth<T>(int v, T x)
    {
        //Recursive generic
        return ((v >= 0) ? meth<Tuple<T, T>>(v - 1, new Tuple<T, T>(x, x)).Field0 : x);
    }
}
