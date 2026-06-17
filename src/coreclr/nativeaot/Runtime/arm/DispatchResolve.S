// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod

NESTED_ENTRY RhpInterfaceDispatch, _TEXT, NoHandler

        // r12 currently contains the indirection cell address. We need scratch registers.
        // Allocate stack frame for register spills.
        PROLOG_PUSH "{r1,r2}"

        // Load the MethodTable from the object instance in r0.
        // The label marks the location of a potential nullref for the unwinder.
        GLOBAL_LABEL RhpInterfaceDispatchAVLocation
        ldr         r1, [r0]

        // r12 contains the indirection cell address.
        ldr         r2, [r12]               // load cached MethodTable
        cmp         r2, r1                  // is this the monomorphic MethodTable?
        bne         LOCAL_LABEL(Hashtable)

        // Fast path: restore r1/r2 before jumping to cached code.
        EPILOG_POP "{r1,r2}"
        // dmb ensures that the Code load below sees the value written before
        // MethodTable. ARM32 has no load-acquire instruction (LDAR is ARMv8 only).
        dmb
        ldr         r12, [r12, #4]          // load the cached monomorphic resolved code address
        EPILOG_BRANCH_REG r12

LOCAL_LABEL(Hashtable):

        // r1 = MethodTable, r12 = indirection cell address
        // Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
        PROLOG_PUSH  "{r3,r4,r5,r6,r8}"

        // Load the _table field (Entry[]) from the cache struct.
        PREPARE_EXTERNAL_VAR_INDIRECT g_pDispatchCache, r2
        ldr         r2, [r2]

        // Compute 32-bit hash from Key.GetHashCode():
        // hash = (RotateLeft(dispatchCell, 16) ^ objectType) * GoldenRatio
        ror         r3, r12, #16
        eor         r3, r3, r1
        movw        r8, #0x79B9
        movt        r8, #0x9E37             // r8 = 0x9E3779B9
        mul         r3, r3, r8

        // HashToBucket: bucket = hash >> hashShift
        ldrb        r8, [r2, #8]
        lsr         r3, r3, r8

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
        ldr         r8, [r5, #4]
        cmp         r12, r8
        bne         LOCAL_LABEL(ProbeMiss)
        ldr         r8, [r5, #8]
        cmp         r1, r8
        bne         LOCAL_LABEL(ProbeMiss)

        // Read the cached code pointer, then re-verify the version has not changed.
        ldr         r8, [r5, #12]
        dmb

        // Verify: (original version & ~1) == re-read version.
        // Masking makes the check fail for both odd (write-in-progress) and changed versions.
        bic         r6, r6, #1
        ldr         r5, [r5]
        cmp         r6, r5
        bne         LOCAL_LABEL(CacheMiss)

        // Dispatch to cached target.
        mov         r12, r8

        EPILOG_POP   "{r3,r4,r5,r6,r8}"
        EPILOG_POP "{r1,r2}"

        EPILOG_BRANCH_REG r12

LOCAL_LABEL(ProbeMiss):
        // If version is zero the rest of the bucket is unclaimed — stop probing.
        cmp         r6, #0
        beq         LOCAL_LABEL(CacheMiss)

        // Quadratic reprobe: i++; index = (index + i) & tableMask
        add         r4, r4, #1
        add         r3, r3, r4
        ldr         r8, [r2, #4]
        sub         r8, r8, #2
        and         r3, r3, r8
        cmp         r4, #8
        blt         LOCAL_LABEL(ProbeLoop)

LOCAL_LABEL(CacheMiss):
        EPILOG_POP  "{r3,r4,r5,r6,r8}"

        // restore original value of r1, r2
        EPILOG_POP "{r1,r2}"

        // Push args for RhpUniversalTransitionTailCall:
        // [sp+0] = extra arg (indirection cell), [sp+4] = target fn (RhpCidResolve)
        sub         sp, sp, #8
        str         r12, [sp, #0]
        PREPARE_EXTERNAL_VAR RhpCidResolve, r12
        str         r12, [sp, #4]

        b           C_FUNC(RhpUniversalTransitionTailCall)

NESTED_END RhpInterfaceDispatch, _TEXT
