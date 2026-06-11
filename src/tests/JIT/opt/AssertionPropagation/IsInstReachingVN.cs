// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.

// Regression test for assertion-prop / VN folding of `isinst` across reaching
// definitions (PHIs of typed allocations).
//
// These tests exercise the path in optAssertionVNIsSubtype where the cast
// target type is proven from reaching VNs (VNF_JitNew / VNF_JitNewArr) for
// PHI inputs. The JIT must:
//   * Fold `isinst BaseClass` to true when every reaching VN is a subtype.
//   * NOT fold to true when any reaching VN can be null or an unrelated type.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class IsInstReachingVN
{
    public class BaseClass { }
    public class DerivedClass1 : BaseClass { }
    public class DerivedClass2 : BaseClass { }
    public class Unrelated { }

    public static object s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PhiOfDerived(int len)
    {
        object bc;
        if (len > 100)
            bc = new DerivedClass1();
        else
            bc = new DerivedClass2();

        // Keep `bc` live so DCE cannot remove the allocations / cast.
        s_sink = bc;
        return bc is BaseClass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PhiWithNull(int len)
    {
        object bc;
        if (len > 100)
            bc = new DerivedClass1();
        else
            bc = null;

        s_sink = bc;
        // Must NOT be folded to true: null reaches here.
        return bc is BaseClass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool DirectAlloc()
    {
        object bc = new DerivedClass1();
        s_sink = bc;
        return bc is BaseClass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PhiWithUnrelated(int len)
    {
        object bc;
        if (len > 100)
            bc = new DerivedClass1();
        else
            bc = new Unrelated();

        s_sink = bc;
        // Must NOT be folded to true: Unrelated is not a BaseClass.
        return bc is BaseClass;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (!PhiOfDerived(200)) return 101;
        if (!PhiOfDerived(50))  return 102;

        if (PhiWithNull(50))    return 103;
        if (!PhiWithNull(200))  return 104;

        if (!DirectAlloc())     return 105;

        if (!PhiWithUnrelated(200)) return 106;
        if (PhiWithUnrelated(50))   return 107;

        return 100;
    }
}
