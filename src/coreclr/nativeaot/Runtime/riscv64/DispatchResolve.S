// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
.macro INTERFACE_DISPATCH DispatchName, ReturnTarget, DispatchCellReg

LEAF_ENTRY \DispatchName, _TEXT

        // Load the MethodTable from the object instance in a0.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        ld      t0, 0(a0)

        // DispatchCellReg currently contains the indirection cell address.
        // The cell is { cached MethodTable, cached code }; the writer stores Code
        // first and publishes the entry with a release store of MethodTable. RVWMO
        // allows two plain loads to be reordered, so "fence r, r" after the
        // MethodTable load gives it acquire semantics: the load of Code below cannot
        // be hoisted above it. This is what the ldar in the arm64 helper does.
        ld      t6, 0(\DispatchCellReg)
        fence   r, r
        bne     t6, t0, LOCAL_LABEL(Hashtable\@) // not the monomorphic MethodTable?

        ld      t6, 8(\DispatchCellReg)         // cached monomorphic resolved code address
.if \ReturnTarget
        mv      a0, t6
        ret
.else
        jr      t6
.endif

LOCAL_LABEL(Hashtable\@):

        // t0 = MethodTable, DispatchCellReg = indirection cell address
        // Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
        // Only the temporaries t0-t4 and t6 are used, so no argument register
        // spilling is needed.
        //
        // Register use in the probe loop:
        //   t0 = MethodTable, t1 = Entry[] table, t3 = current entry,
        //   t4 = version snapshot, t6 = scratch.
        //   t2 = bucket index (bits 0..31) | probe count i (bits 40..47). The two
        //   share a register because at the key compare below t6 is the only
        //   temporary still free, so the probe count has nowhere else to live.
        //   arm64 has the registers to keep it separate, in w15.

        // Load the _table field (Entry[]) from the cache struct: the first load
        // dereferences the address of g_pDispatchCache, the second reads _table.
        PREPARE_EXTERNAL_VAR g_pDispatchCache, t1
        ld      t1, 0(t1)
        ld      t1, 0(t1)

        // Compute 32-bit hash from Key.GetHashCode():
        // hash = IntPtr.GetHashCode(RotateLeft(dispatchCell, 16) ^ objectType)
        slli    t2, \DispatchCellReg, 16
        srli    t3, \DispatchCellReg, 48
        or      t2, t2, t3              // RotateLeft(cell, 16)
        xor     t2, t2, t0              // combined = RotateLeft(cell, 16) ^ MethodTable
        srai    t3, t2, 32              // upper32 = combined >> 32 (arithmetic)
        xor     t2, t2, t3              // hash = (int)lower32 ^ (int)upper32
        sext.w  t2, t2                  // sign-extend hash to 64-bit

        // HashToBucket: bucket = (hash * 0x9E3779B97F4A7C15) >> hashShift
        li      t3, -7046029254386353131 // 0x9E3779B97F4A7C15
        mul     t2, t2, t3
        lbu     t3, 0x10(t1)            // hashShift from Element[0]._info
        srl     t2, t2, t3              // bucket index, i = 0

LOCAL_LABEL(ProbeLoop\@):
        // Compute entry address: Element(table, index) = table + 0x10 + (index + 1) * 0x20.
        // The shift pair does two things at once: it drops the packed probe count and
        // zero-extends the 32-bit index, and it scales that index by 0x20.
        slli    t3, t2, 32              // index in the upper half, probe count gone
        srli    t3, t3, 27              // back down, times 0x20
        add     t3, t3, t1
        addi    t3, t3, 0x30

        // Read the version snapshot (seqlock protocol). "fence r, r" makes the load
        // acquire, so the key and value loads below cannot be hoisted above it and
        // are guaranteed to see the data written before this version was published.
        lw      t4, 0(t3)
        fence   r, r
        andi    t6, t4, 1
        bnez    t6, LOCAL_LABEL(ProbeMiss\@) // odd: entry being written

        // Compare key (dispatchCell, objectType)
        ld      t6, 8(t3)
        bne     t6, \DispatchCellReg, LOCAL_LABEL(ProbeMiss\@)
        ld      t6, 16(t3)
        bne     t6, t0, LOCAL_LABEL(ProbeMiss\@)

        // Read the cached code pointer.
        ld      t6, 24(t3)

        // Order the value load before the version re-read, so that a writer which
        // touched this entry in between is guaranteed to show up as a version change.
        fence   r, r

        // Re-verify version has not changed.
        lw      t3, 0(t3)
        bne     t3, t4, LOCAL_LABEL(CacheMiss\@)

        // Dispatch to cached target.
.if \ReturnTarget
        mv      a0, t6
        ret
.else
        jr      t6
.endif

LOCAL_LABEL(ProbeMiss\@):
        // If version is zero the rest of the bucket is unclaimed - stop probing.
        beqz    t4, LOCAL_LABEL(CacheMiss\@)

        // Quadratic reprobe: i++; index = (index + i) & tableMask. Give up after
        // eight probes, matching arm64.
        srli    t6, t2, 40              // i
        addi    t6, t6, 1
        li      t3, 8
        bge     t6, t3, LOCAL_LABEL(CacheMiss\@)
        slli    t3, t2, 32
        srli    t3, t3, 32              // index
        add     t3, t3, t6              // index + i
        lw      t4, 8(t1)               // table.Length
        addi    t4, t4, -2              // TableMask = Length - 2
        and     t3, t3, t4
        slli    t6, t6, 40
        or      t2, t3, t6              // repack index | i << 40
        j       LOCAL_LABEL(ProbeLoop\@)

LOCAL_LABEL(CacheMiss\@):
.if \ReturnTarget
        tail    C_FUNC(RhpCidResolve_Worker)
.else
        // Calling convention of the universal thunk is:
        //  t0: target address for the thunk to call
        //  t1: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, t0
        mv      t1, \DispatchCellReg
        // The tail call is written out by hand because the "tail" pseudo-instruction
        // expands through t1, which is carrying the thunk parameter set just above.
1:
        auipc   t2, %pcrel_hi(C_FUNC(RhpUniversalTransitionTailCall))
        jalr    x0, t2, %pcrel_lo(1b)
.endif

LEAF_END \DispatchName, _TEXT

.endm

        INTERFACE_DISPATCH RhpInterfaceDispatch, 0, t5
        INTERFACE_DISPATCH RhpDispatchResolve, 1, a1
