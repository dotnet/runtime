// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // r12 currently contains the indirection cell address. But we need more scratch registers and
        // we may A/V on a null this. Store r1 and r2 in red zone.
        str         r1, [sp, #-8]
        str         r2, [sp, #-4]

        // Load the MethodTable from the object instance in r0.
        // The label marks the location of a potential nullref for the unwinder.
        GLOBAL_LABEL RhpInterfaceDispatchAVLocation
        ldr         r1, [r0]

        // r12 contains the indirection cell address.
        ldr         r2, [r12]               // load cached MethodTable
        cmp         r2, r1                  // is this the monomorphic MethodTable?
        bne         LOCAL_LABEL(Hashtable)

        // Fast path: restore r1/r2 before jumping to cached code.
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]
        // dmb ensures that the Code load below sees the value written before
        // MethodTable. ARM32 has no load-acquire instruction (LDAR is ARMv8 only).
        dmb
        ldr         r12, [r12, #4]          // load the cached monomorphic resolved code address
        bx          r12

LOCAL_LABEL(Hashtable):

        // r1 = MethodTable, r12 = indirection cell address
        // Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
        // Spill additional registers to the red zone below sp
        // so we don't modify sp (this is a LEAF_ENTRY with no unwind info).
        str         r3,  [sp, #-12]
        str         lr,  [sp, #-16]
        str         r4,  [sp, #-20]
        str         r5,  [sp, #-24]
        str         r6,  [sp, #-28]

        // Load the _table field (Entry[]) from the cache struct.
        PREPARE_EXTERNAL_VAR_INDIRECT g_pDispatchCache, r2
        ldr         r2, [r2]

        // Compute 32-bit hash from Key.GetHashCode():
        // hash = (RotateLeft(dispatchCell, 16) ^ objectType) * GoldenRatio
        ror         r3, r12, #16
        eor         r3, r3, r1
        movw        lr, #0x79B9
        movt        lr, #0x9E37             // lr = 0x9E3779B9
        mul         r3, r3, lr

        // HashToBucket: bucket = hash >> hashShift
        ldrb        lr, [r2, #8]
        lsr         r3, r3, lr

        mov         r4, #0                  // i = 0

LOCAL_LABEL(ProbeLoop):
        // Compute entry address: Element(table, index) = table + 8 + (index + 1) * 16
        add         r5, r3, #1
        lsl         r5, r5, #4
        add         r5, r5, #8
        add         r5, r2, r5

        // Read version snapshot before key comparison (seqlock protocol).
        ldr         r6, [r5]
        dmb

        // Compare key (dispatchCell, objectType)
        ldr         lr, [r5, #4]
        cmp         r12, lr
        bne         LOCAL_LABEL(ProbeMiss)
        ldr         lr, [r5, #8]
        cmp         r1, lr
        bne         LOCAL_LABEL(ProbeMiss)

        // Read the cached code pointer, then re-verify the version has not changed.
        ldr         lr, [r5, #12]
        dmb

        // Verify: (original version & ~1) == re-read version.
        // Masking makes the check fail for both odd (write-in-progress) and changed versions.
        bic         r6, r6, #1
        ldr         r5, [r5]
        cmp         r6, r5
        bne         LOCAL_LABEL(CacheMiss)

        // Dispatch to cached target.
        mov         r12, lr

        ldr         r6, [sp, #-28]
        ldr         r5, [sp, #-24]
        ldr         r4, [sp, #-20]
        ldr         r3, [sp, #-12]
        ldr         lr, [sp, #-16]
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]

        bx          r12

LOCAL_LABEL(ProbeMiss):
        // If version is zero the rest of the bucket is unclaimed — stop probing.
        cmp         r6, #0
        beq         LOCAL_LABEL(CacheMiss)

        // Quadratic reprobe: i++; index = (index + i) & tableMask
        add         r4, r4, #1
        add         r3, r3, r4
        ldr         lr, [r2, #4]
        sub         lr, lr, #2
        and         r3, r3, lr
        cmp         r4, #8
        blt         LOCAL_LABEL(ProbeLoop)

LOCAL_LABEL(CacheMiss):
        ldr         r6, [sp, #-28]
        ldr         r5, [sp, #-24]
        ldr         r4, [sp, #-20]

LOCAL_LABEL(SlowPath):
        // restore original value of r1, r2, r3, lr
        ldr         r3, [sp, #-12]
        ldr         lr, [sp, #-16]
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]

        str         r12, [sp, #-8]
        PREPARE_EXTERNAL_VAR RhpCidResolve, r12
        str         r12, [sp, #-4]

        b           C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
