// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_108969
{
    [Fact]
    public static int TestEntryPoint() => Foo(null);
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(object o)
    {
        S v = default;
        try
        {
            v = Bar();

            // "(int?)o" creates a QMARK with a branch that may throw; we would
            // end up reading back v.A inside the QMARK
            Use((int?)o);
        }
        catch (Exception)
        {
        }

        // Induce promotion of v.A field
        Use(v.A);
        Use(v.A);
        Use(v.A);
        Use(v.A);
        Use(v.A);
        Use(v.A);
        return v.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Bar()
    {
        return new S { A = 100 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use<T>(T x)
    {
    }

    private struct S
    {
        public int A, B, C, D;
    }
}
