// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class B
{
    public B z() => this;
    public bool T() => true;
    public virtual bool F(B b, int x) => x == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool G(B b, int x)
    {
        var bb = z();
        return bb?.T() == true && F(b, x);
    }

    public bool H(B b, int x) => G(b, x);
    public bool I(B b, int x) => F(b, x + 1);
    public bool J(B b, int x) => I(b, x);
    public bool K(B b, int x) => J(b, x);
}

public class X : B
{
    static int y = 0;

    public override bool F(B b, int x)
    {
        if (x == 0) return true;
        return b.H(b, x - 1);
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        var x = new X();

        for (int i = 0; i < 50; i++)
        {
            _ = x.K(x, i);
            Thread.Sleep(15);
        }

        return x.K(x, y) ? 100 : -1;
    }
}