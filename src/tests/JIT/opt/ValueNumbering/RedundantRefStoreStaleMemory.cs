// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The unchanged reference store elimination reads the current memory state
// during value numbering. Memory SSA phis are pruned where no reads follow,
// so in a store-only join block the block entry state is the dominator path
// state rather than the true reaching state. These tests store a previously
// loaded reference back over a join whose other path overwrote the slot,
// which a proof consulting the pruned state would wrongly remove.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class RedundantRefStoreStaleMemory
{
    public struct RefSlot
    {
        public object A;
        public int X;
    }

    public class Holder
    {
        public RefSlot S;
    }

    [Fact]
    public static void FieldRestoreAfterBranchStoreIsKept()
    {
        Holder h = new Holder();
        object orig = new object();
        h.S.A = orig;
        FieldRestore(h, new object(), true);
        Assert.Same(orig, h.S.A);
    }

    [Fact]
    public static void ByrefRestoreAfterBranchStoreIsKept()
    {
        Holder h = new Holder();
        object orig = new object();
        h.S.A = orig;
        ByrefRestore(ref h.S, new object(), true);
        Assert.Same(orig, h.S.A);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FieldRestore(Holder h, object x, bool cond)
    {
        object orig = h.S.A;
        if (cond)
        {
            h.S.A = x;
        }

        h.S.A = orig;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ByrefRestore(ref RefSlot s, object x, bool cond)
    {
        object orig = s.A;
        if (cond)
        {
            s.A = x;
        }

        s.A = orig;
    }
}
