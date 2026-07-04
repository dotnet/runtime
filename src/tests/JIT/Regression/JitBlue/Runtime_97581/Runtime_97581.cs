// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression tests for https://github.com/dotnet/runtime/issues/97581:
// Redundant Branch Opts should recognize when a dominating `is` check tells
// it something about a nested `is` check on the same object via class-handle
// relationships (compareTypesForCast returning Must or MustNot).

namespace Runtime_97581;

using System.Runtime.CompilerServices;
using Xunit;

file class Program1 {}
file class Program2 {}
file class Derived1 : Program1 {}

public class Runtime_97581
{
    // Unrelated types: (o is Program1) -> (o is Program2) is always false.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustNotEqTree(object o)
    {
        if (o is Program1)
        {
            if (o is Program2) return 1;
            return 2;
        }
        return 3;
    }

    // Same pattern, opposite tree relop (NE instead of EQ).
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustNotNeTree(object o)
    {
        if (o is Program1)
        {
            if (!(o is Program2)) return 1;
            return 2;
        }
        return 3;
    }

    // Derived -> Base always holds: (o is Derived1) implies (o is Program1) true.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustEqTree(object o)
    {
        if (o is Derived1)
        {
            if (o is Program1) return 1;
            return 2;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustNeTree(object o)
    {
        if (o is Derived1)
        {
            if (!(o is Program1)) return 1;
            return 2;
        }
        return 3;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var p1  = new Program1();
        var p2  = new Program2();
        var d1  = new Derived1();
        object str = "hello";

        if (MustNotEqTree(p1)  != 2) return 101;
        if (MustNotEqTree(p2)  != 3) return 102;
        if (MustNotEqTree(str) != 3) return 103;
        if (MustNotEqTree(d1)  != 2) return 104;

        if (MustNotNeTree(p1)  != 1) return 111;
        if (MustNotNeTree(p2)  != 3) return 112;
        if (MustNotNeTree(d1)  != 1) return 114;

        if (MustEqTree(d1)  != 1) return 121;
        if (MustEqTree(p1)  != 3) return 122;
        if (MustEqTree(str) != 3) return 123;

        if (MustNeTree(d1)  != 2) return 131;
        if (MustNeTree(p1)  != 3) return 132;

        return 100;
    }
}
