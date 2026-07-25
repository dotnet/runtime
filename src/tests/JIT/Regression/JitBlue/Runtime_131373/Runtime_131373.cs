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
// The shape below is `field = Allocate()`, which lowers to
// STOREIND(LCL_VAR obj, CALL alloc) -- obj is pushed, the allocating call is the
// safepoint, and the store writes through the stale pointer. Gen0 collections are
// driven so the objects actually move.
//
// Reproduces only under crossgen wasm R2R (TargetOS=browser). Passes trivially on
// all other targets.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_131373
{
    private sealed class Node
    {
        public string Payload;
        public int Id;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // Enough iterations, and enough garbage per iteration, that gen0 fills and
        // compacts repeatedly while a Node ref is held across the allocating call.
        for (int i = 0; i < 2000; i++)
        {
            Node node = Allocate(i);
            Assert.Equal(i, node.Id);
            Assert.Equal(i.ToString(), node.Payload);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Node Allocate(int id)
    {
        Node node = new Node();
        node.Id = id;

        // `node` is loaded before MakePayload runs, so the unfixed JIT holds a stale
        // copy across that call and stores the payload through a dangling reference.
        node.Payload = MakePayload(id);

        return node;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string MakePayload(int id)
    {
        // Allocate short-lived garbage so this call is likely to be the safepoint
        // that triggers a compacting gen0 collection.
        byte[] garbage = new byte[4096];
        GC.KeepAlive(garbage);

        return id.ToString();
    }
}
