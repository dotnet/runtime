// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for a WebAssembly R2R (crossgen) GC hole. Values on the wasm
// operand stack are not reported to the GC, so fgWasmSpillRefs spills refs that
// are live across a call into pinned shadow-stack slots. It used to skip refs
// sourced from a non-address-exposed GT_LCL_VAR, on the theory that such a local
// can't be mutated between its def and its use. That ignores the GC: once the
// local is loaded onto the operand stack the pushed value is a copy, so a
// compacting GC at a nested call moves the object and fixes up the local's slot
// while the stale copy is what the consumer dereferences.
//
// The shape that reproduces it is a store through a byref parameter. A class
// field store does NOT work: the field sits at a non-zero offset (the method
// table pointer is at 0), so the address is ADD(LCL_VAR obj, offset) -- a byref
// ADD that the unfixed code already spilled. Storing through a `ref` parameter
// has no ADD, so the store address is the bare GT_LCL_VAR, pushed before the
// call that produces the value and consumed after it.
//
// The allocation loops are what make this deterministic. A moving GC relocates
// when an allocation forces a collection, so it is the churn -- not the forced
// GC.Collect on its own -- that guarantees `box` actually moves: a compacting
// collect on a small unfragmented heap has nothing to relocate, and the stale
// operand-stack copy stays accidentally valid. With the churn the failure hits
// on the very first iteration.
//
// Reproduces only under crossgen wasm R2R (TargetOS=browser), where the unfixed
// JIT stores through the stale byref and leaves box.Slot null. Passes trivially
// on all other targets.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_131373
{
    public sealed class Box
    {
        public object Slot;
        public int Tag;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 400; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                byte[] garbage = new byte[8192];
                GC.KeepAlive(garbage);
            }

            Box box = new Box { Tag = i };
            StoreThroughByref(ref box.Slot, i);

            // Null under the unfixed JIT: the store went to box's pre-move address.
            Assert.NotNull(box.Slot);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreThroughByref(ref object slot, int i)
    {
        // STOREIND(LCL_VAR slot, CALL Allocate). `slot` is pushed before the call,
        // which is the safepoint, and is not an operand of it.
        slot = Allocate(i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Allocate(int i)
    {
        for (int j = 0; j < 48; j++)
        {
            byte[] garbage = new byte[8192];
            GC.KeepAlive(garbage);
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        return new string('x', 1 + (i & 7));
    }
}
