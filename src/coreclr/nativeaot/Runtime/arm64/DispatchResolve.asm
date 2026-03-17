;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionTailCall
    EXTERN RhpUniversalTransitionGuardedTailCall

    EXTERN __guard_dispatch_icall_fptr
    EXTERN g_pDispatchCache

;; Macro that generates an interface dispatch stub.
;; DispatchName: the name of the dispatch entry point
;; Guarded: if non-empty, validate indirect call targets using Control Flow Guard
    MACRO
        INTERFACE_DISPATCH $DispatchName, $Guarded

    LEAF_ENTRY $DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in x0.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     x12, [x0]

        ;; x11 currently contains the indirection cell address.
        ;; Load-acquire ensures that if we observe the cached MethodTable,
        ;; the subsequent load of Code will see the value written before it.
        ldar    x13, [x11]              ;; load-acquire cached MethodTable
        cmp     x13, x12                ;; is this the monomorphic MethodTable?
        bne     %ft1

        ldr     x9, [x11, #8]           ;; load the cached monomorphic resolved code address
    IF "$Guarded" != ""
        adrp    x16, __guard_dispatch_icall_fptr
        ldr     x16, [x16, __guard_dispatch_icall_fptr]
        br      x16
    ELSE
        br      x9
    ENDIF

1
        ;; x12 = MethodTable, x11 = indirection cell address
        ;; Look up the target in the dispatch cache hashtable (GenericCache<Key, nint>).
        ;; Only volatile scratch registers (x9-x17) are used, so no argument
        ;; register spilling is needed.

        ;; Load the _table field (Entry[]) from the cache struct.
        adrp    x13, g_pDispatchCache
        ldr     x13, [x13, g_pDispatchCache]
        ldr     x13, [x13]

        ;; Compute 32-bit hash from Key.GetHashCode():
        ;; hash = IntPtr.GetHashCode(RotateLeft(dispatchCell, 16) ^ objectType)
        eor     x14, x12, x11, ror #48 ;; combined = MethodTable ^ RotateLeft(cell, 16)
        asr     x16, x14, #32          ;; upper32 = combined >> 32 (arithmetic)
        eor     w14, w14, w16          ;; hash = (int)lower32 ^ (int)upper32
        sxtw    x14, w14               ;; sign-extend hash to 64-bit

        ;; HashToBucket: bucket = (hash * 0x9E3779B97F4A7C15) >> hashShift
        movz    x16, #0x7C15
        movk    x16, #0x7F4A, lsl #16
        movk    x16, #0x79B9, lsl #32
        movk    x16, #0x9E37, lsl #48
        mul     x14, x14, x16
        ldrb    w16, [x13, #0x10]       ;; hashShift from Element[0]._info
        lsr     x14, x14, x16           ;; bucket index

        ;; Precompute loop-invariant base: table + 0x10 (start of Element data).
        add     x13, x13, #0x10
        mov     w15, wzr                ;; loop counter i = 0

2       ;; ProbeLoop
        ;; Compute entry address: Element(table, index) = base + (index + 1) * 0x20
        add     w9, w14, #1
        sbfiz   x9, x9, #5, #32        ;; sign-extend and shift left by 5 in one instruction
        add     x9, x13, x9

        ;; Read version snapshot with load-acquire (seqlock protocol, ARM64 memory model).
        ;; This ensures subsequent loads of key and value see data written before this version.
        ldar    w10, [x9]
        tbnz    w10, #0, %ft3           ;; skip if odd (entry being written)

        ;; Compare key (dispatchCell, objectType) — load both fields with a single ldp.
        ldp     x16, x17, [x9, #8]
        cmp     x16, x11
        bne     %ft3
        cmp     x17, x12
        bne     %ft3

        ;; Read the cached code pointer.
        ldr     x16, [x9, #0x18]

        ;; Ensure value read completes before version re-read (ARM64 needs explicit barrier).
        dmb     ishld

        ;; Re-verify version has not changed.
        ldr     w17, [x9]
        cmp     w10, w17
        bne     %ft4

        ;; Dispatch to cached target.
        mov     x9, x16

    IF "$Guarded" != ""
        adrp    x16, __guard_dispatch_icall_fptr
        ldr     x16, [x16, __guard_dispatch_icall_fptr]
        br      x16
    ELSE
        br      x9
    ENDIF

3       ;; ProbeMiss
        ;; If version is zero the rest of the bucket is unclaimed — stop probing.
        cbz     w10, %ft4

        ;; Quadratic reprobe: i++; index = (index + i) & tableMask
        add     w15, w15, #1
        add     w14, w14, w15
        ldr     w9, [x13, #-8]          ;; table.Length (at base - 0x10 + 0x08 = base - 0x08)
        sub     w9, w9, #2              ;; TableMask = Length - 2
        and     w14, w14, w9
        cmp     w15, #8
        blt     %bt2

4       ;; CacheMiss / SlowPath
        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
    IF "$Guarded" != ""
        b       RhpUniversalTransitionGuardedTailCall
    ELSE
        b       RhpUniversalTransitionTailCall
    ENDIF

    LEAF_END $DispatchName

    MEND

    INTERFACE_DISPATCH RhpInterfaceDispatch
    INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, Guarded

    END
