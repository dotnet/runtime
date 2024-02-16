; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    ;;like TEXTAREA, but with 64 byte alignment so that we can align the patchable pool below to 64 without warning
    AREA    |.text|,ALIGN=6,CODE,READONLY

;-----------------------------------------------------------------------------
; The following Macros help in WRITE_BARRIER Implementations
    ; WRITE_BARRIER_ENTRY
    ;
    ; Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
    ; to declare a write barrier function.
    ;
    MACRO
      WRITE_BARRIER_ENTRY $name

      LEAF_ENTRY $name
    MEND

    ; WRITE_BARRIER_END
    ;
    ; The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
    ;
    MACRO
      WRITE_BARRIER_END $__write_barrier_name

      LEAF_END_MARKED $__write_barrier_name

    MEND

; ------------------------------------------------------------------
; Start of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeStart
        ret      lr
    LEAF_END

        ; Begin patchable literal pool
        ALIGN 64  ; Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
wbs_begin
wbs_card_table
        DCQ 0
wbs_card_bundle_table
        DCQ 0
wbs_sw_ww_table
        DCQ 0
wbs_ephemeral_low
        DCQ 0
wbs_ephemeral_high
        DCQ 0
wbs_lowest_address
        DCQ 0
wbs_highest_address
        DCQ 0
#ifdef WRITE_BARRIER_CHECK
wbs_GCShadow
        DCQ 0
wbs_GCShadowEnd
        DCQ 0
#endif
    WRITE_BARRIER_END JIT_WriteBarrier_Table

; void JIT_ByRefWriteBarrier
; On entry:
;   x13  : the source address (points to object reference to write)
;   x14  : the destination address (object reference written here)
;
; On exit:
;   x12  : trashed
;   x13  : incremented by 8
;   x14  : incremented by 8
;   x15  : trashed
;   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
;
    WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier

        ldr      x15, [x13], 8
        b        JIT_CheckedWriteBarrier

    WRITE_BARRIER_END JIT_ByRefWriteBarrier

;-----------------------------------------------------------------------------
; Simple WriteBarriers
; void JIT_CheckedWriteBarrier(Object** dst, Object* src)
; On entry:
;   x14  : the destination address (LHS of the assignment)
;   x15  : the object reference (RHS of the assignment)
;
; On exit:
;   x12  : trashed
;   x14  : incremented by 8
;   x15  : trashed
;   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
;
    WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
        ldr      x12,  wbs_lowest_address
        cmp      x14,  x12

        ldr      x12,  wbs_highest_address
        ccmphs   x14,  x12, #0x2
        blo      JIT_WriteBarrier

NotInHeap
        str      x15, [x14], 8
        ret      lr
    WRITE_BARRIER_END JIT_CheckedWriteBarrier

; void JIT_WriteBarrier(Object** dst, Object* src)
; On entry:
;   x14  : the destination address (LHS of the assignment)
;   x15  : the object reference (RHS of the assignment)
;
; On exit:
;   x12  : trashed
;   x14  : incremented by 8
;   x15  : trashed
;   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier
        stlr     x15, [x14]

#ifdef WRITE_BARRIER_CHECK
        ; Update GC Shadow Heap

        ; Do not perform the work if g_GCShadow is 0
        ldr      x12, wbs_GCShadow
        cbz      x12, ShadowUpdateDisabled

        ; need temporary register. Save before using.
        str      x13, [sp, #-16]!

        ; Compute address of shadow heap location:
        ;   pShadow = $g_GCShadow + (x14 - g_lowest_address)
        ldr      x13, wbs_lowest_address
        sub      x13, x14, x13
        add      x12, x13, x12

        ; if (pShadow >= $g_GCShadowEnd) goto end
        ldr      x13, wbs_GCShadowEnd
        cmp      x12, x13
        bhs      ShadowUpdateEnd

        ; *pShadow = x15
        str      x15, [x12]

        ; Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
        ; conditions are caught by INVALIDGCVALUE.
        dmb      ish

        ; if ([x14] == x15) goto end
        ldr      x13, [x14]
        cmp      x13, x15
        beq ShadowUpdateEnd

        ; *pShadow = INVALIDGCVALUE (0xcccccccd)
        movz     x13, #0xcccd
        movk     x13, #0xcccc, LSL #16
        str      x13, [x12]

ShadowUpdateEnd
        ldr      x13, [sp], #16
ShadowUpdateDisabled
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        ldr      x12,  wbs_sw_ww_table
        cbz      x12,  CheckCardTable
        add      x12,  x12, x14, LSR #0xC  // SoftwareWriteWatch::AddressToTableByteIndexShift
        ldrb     w17,  [x12]
        cbnz     x17,  CheckCardTable
        mov      w17,  0xFF
        strb     w17,  [x12]
#endif

CheckCardTable
        ; Branch to Exit if the reference is not in the Gen0 heap
        ;
        ldr      x12,  wbs_ephemeral_low
        cbz      x12,  SkipEphemeralCheck
        cmp      x15,  x12

        ldr      x12,  wbs_ephemeral_high

        ; Compare against the upper bound if the previous comparison indicated
        ; that the destination address is greater than or equal to the lower
        ; bound. Otherwise, set the C flag (specified by the 0x2) so that the
        ; branch to exit is taken.
        ccmp     x15,  x12, #0x2, hs

        bhs      Exit

SkipEphemeralCheck
        ; Check if we need to update the card table
        ldr      x12, wbs_card_table

        ; x15 := pointer into card table
        add      x15, x12, x14, lsr #11

        ldrb     w12, [x15]
        cmp      x12, 0xFF
        beq      Exit

UpdateCardTable
        mov      x12, 0xFF
        strb     w12, [x15]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        ; Check if we need to update the card bundle table
        ldr      x12, wbs_card_bundle_table

        ; x15 := pointer into card bundle table
        add      x15, x12, x14, lsr #21

        ldrb     w12, [x15]
        cmp      x12, 0xFF
        beq      Exit

        mov      x12, 0xFF
        strb     w12, [x15]
#endif

Exit
        add      x14, x14, 8
        ret      lr
    WRITE_BARRIER_END JIT_WriteBarrier

; ------------------------------------------------------------------
; End of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeLast
        ret      lr
    LEAF_END

; Must be at very end of file
    END
