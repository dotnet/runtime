// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// The test was showing silence bad codegen for a `LclFldAddr` node under HWINSTRINSIC(IND).

public class Runtime_39403
{
    struct Container
    {
        public Vector<int> Vector;
        public int Integer;
    }

    static Vector<int> DoAThingByRef(ref Vector<int> s)
    {
        return s + Vector<int>.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<int> TestLclFldAddr()
    {
        Container container = default;
        return DoAThingByRef(ref container.Vector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<int> TestLclVarAddr()
    {
        Container container = default;
        return DoAThingByRef(ref Unsafe.As<Container, Vector<int>>(ref container));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Vector<int> v1 = TestLclFldAddr();
        Vector<int> v2 = TestLclVarAddr();
        System.Diagnostics.Debug.Assert(v1 == v2);
        return 100;
    }
}
