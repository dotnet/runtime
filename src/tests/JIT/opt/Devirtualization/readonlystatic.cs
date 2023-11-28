// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

class B
{
    public virtual int F() => -1;
}

class D : B
{
    public override int F() => 100;
}

public class X 
{
    static readonly B S;
    static int R;

    static X()
    {
        S = new B();
        R = During();
        S = new D();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int During()
    {
        // Jit should not be able to devirtualize here
        return S.F();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int After()
    {
        // Jit should be able to devirtualize here
        return S.F();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var p = S;
        int a = After();
        int d = During();
        return a + d + R - 99;
    }
}
