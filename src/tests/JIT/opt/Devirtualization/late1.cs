// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class B
{
    public virtual int F(int x) { return x + 33; }
}

public class D : B
{
    public override int F(int x) { return x + 44; }
}

public class Q
{
    static int V(int x)
    {
        return x;
    }

    // calls to B will use a return spill temp since 
    // B has multiple return sites.
    //
    // Jit will initially type this temp as 'B' but then
    // sharpen type type to 'B exact' or 'D exact' if the
    // 'b' is a constant at the call site.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static B Choose(bool b)
    {
        return b ? new B() : new D();
    }

    // The calls to F should be devirtualized late
    [Fact]
    public static int TestEntryPoint()
    {
        int v0 = Choose(false).F(V(67));
        B b = Choose(true);
        int v1 = b.F(V(56));
        return v0 + v1 - 100;
    }
}
