// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class X
{
    // a -> rdi
    // b -> rsi
    // c -> rdx
    //   -> rcx
    // d -> r8
    // e -> r9
    // f -> s[0]
    // g -> s[1]
    public static int F(int a, int b, Guid c, int d, int e, int f, int g)
    {
        Guid[] z = new Guid[] { c };
        // Bug is here passing params to G: f gets trashed by g.
        return G(a, b, z, d, e, f, g);
    }
    
    // loop here is just to make this method too big to inline
    // if we set [noinline] to effect this, we won't tail call it either.
    //
    // a -> rdi
    // b -> rsi
    // c -> rdx
    // d -> rcx
    // e -> r8
    // f -> r9
    // g -> s[0]
    public static int G(int a, int b, Guid[] c, int d, int e, int f, int g)
    {
        int r = 0;
        for (int i = 0; i < 10; i++)
        {
            r += f + g;
        }
        return r / 10;
    }

    // No-opt to stop F from being inlined without marking it noinline
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        int result = F(0, 1, Guid.Empty, 3, 4, 33, 67);
        Console.WriteLine($"Result={result}");
        return result;
    }
}
