// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression tests for https://github.com/dotnet/runtime/issues/97581:
// Redundant Branch Opts should recognize when a dominating `is` check tells
// it something about a nested `is` check on the same object via class-handle
// relationships (compareTypesForCast returning Must or MustNot).

namespace Runtime_97581;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

file class Program1 {}
file class Program2 {}
file class Derived1 : Program1 {}

file interface IFoo {}

file class DcastToIFoo : IDynamicInterfaceCastable
{
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        => interfaceType.Equals(typeof(IFoo).TypeHandle);
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType) =>
        throw new InvalidOperationException();
}

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

    // Exact pattern from https://github.com/dotnet/runtime/issues/97581.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Issue97581Exact(object o)
    {
        if (o is Program1)
        {
            if (o is Program2)
            {
                System.Console.WriteLine("this branch is never taken");
            }
        }
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

    // IDynamicInterfaceCastable can make an object dynamically implement an interface
    // that its static type does not. compareTypesForCast(DcastToIFoo, IFoo) is MustNot
    // statically, but the runtime cast succeeds. The optimization must not fold
    // "o is DcastToIFoo -> o is IFoo" to false.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustNotInterfaceTree(object o)
    {
        if (o is DcastToIFoo)
        {
            if (o is IFoo) return 1; // must not be folded away
            return 2;
        }
        return 3;
    }

    // False-implies-false: !(o is Program1) implies !(o is Derived1). If the outer
    // check fails then the runtime type does not derive from Program1, so it cannot
    // be a Derived1 either.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustFalseImpliesFalse(object o)
    {
        if (!(o is Program1))
        {
            if (o is Derived1) return 1; // must be folded to false
            return 2;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MustFalseImpliesFalseNeTree(object o)
    {
        if (!(o is Program1))
        {
            if (!(o is Derived1)) return 1;
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
        object dc  = new DcastToIFoo();

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

        // The IDCC object satisfies "o is DcastToIFoo" statically and also
        // "o is IFoo" via IDynamicInterfaceCastable. Result must be 1.
        if (MustNotInterfaceTree(dc)  != 1) return 141;
        if (MustNotInterfaceTree(p1)  != 3) return 142;
        if (MustNotInterfaceTree(str) != 3) return 143;

        if (MustFalseImpliesFalse(p1)  != 3) return 151;
        if (MustFalseImpliesFalse(d1)  != 3) return 152;
        if (MustFalseImpliesFalse(str) != 2) return 153;
        if (MustFalseImpliesFalse(p2)  != 2) return 154;

        if (MustFalseImpliesFalseNeTree(p1)  != 3) return 161;
        if (MustFalseImpliesFalseNeTree(d1)  != 3) return 162;
        if (MustFalseImpliesFalseNeTree(str) != 1) return 163;
        if (MustFalseImpliesFalseNeTree(p2)  != 1) return 164;

        // Just make sure the exact-issue method doesn't crash and produces no output.
        Issue97581Exact(p1);
        Issue97581Exact(p2);
        Issue97581Exact(str);

        return 100;
    }
}
