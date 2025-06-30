; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"
#include "patchedcodeconstants.h"

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

;-----------------------------------------------------------------------------
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
;   x17  : trashed (ip1)
;
;   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
;         if you add more trashed registers.
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
;   x17  : trashed (ip1)
;
;   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
;         if you add more trashed registers.
;
    WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
        ldr      x12,  wbs_lowest_address
        ldr      x17,  wbs_highest_address
        cmp      x14,  x12
        ccmphs   x14,  x17, #0x2
        blo      JIT_WriteBarrier

NotInHeap
        str      x15, [x14], 8
        ret      lr
    WRITE_BARRIER_END JIT_CheckedWriteBarrier

;-----------------------------------------------------------------------------
; void JIT_WriteBarrier(Object** dst, Object* src)
;
; Empty function which at runtime is patched with one of the JIT_WriteBarrier_
; functions below.
; On entry:
;   x14  : the destination address (LHS of the assignment)
;   x15  : the object reference (RHS of the assignment)
;
; On exit:
;   x12  : trashed
;   x14  : incremented by 8
;   x15  : trashed
;   x17  : trashed (ip1)
;
;   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
;         if you add more trashed registers.
;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier
; This must be greater than the largest JIT_WriteBarrier_ function.
        space (232*4), 0
    WRITE_BARRIER_END JIT_WriteBarrier

        ; Begin patchable literal pool
        ALIGN 64  ; Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_CardTable
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_CardBundleTable
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_WriteWatchTable
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_Lower
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_Upper
            DCQ 0
wbs_lowest_address
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_LowestAddress
            DCQ 0
wbs_highest_address
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_HighestAddress
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_RegionToGeneration
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_RegionShr
            DCQ 0
#ifdef WRITE_BARRIER_CHECK
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_GCShadow
            DCQ 0
        PATCH_LABEL JIT_WriteBarrier_Patch_Label_GCShadowEnd
            DCQ 0
#endif
    WRITE_BARRIER_END JIT_WriteBarrier_Table

; ------------------------------------------------------------------
; End of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeLast
        ret      lr
    LEAF_END



;-----------------------------------------------------------------------------
; The following Macros are used by the different JIT_WriteBarrier_ functions.
;
;

    MACRO
        WRITE_BARRIER_ENTRY_STUB $name
start$name
            stlr  x15, [x14]
    MEND


    MACRO
        WRITE_BARRIER_SHADOW_UPDATE_STUB $name
        #ifdef WRITE_BARRIER_CHECK
        ; Update GC Shadow Heap

        ; Do not perform the work if g_GCShadow is 0
            ldr  x12, JIT_WriteBarrier_Offset_GCShadow + start$name
            cbz  x12, ShadowUpdateEnd$name

        ; Compute address of shadow heap location:
        ;   pShadow = g_GCShadow + (x14 - g_lowest_address)
            ldr  x17, JIT_WriteBarrier_Offset_LowestAddress + start$name
            sub  x17, x14, x17
            add  x12, x17, x12

        ; if (pShadow >= g_GCShadowEnd) goto end
            ldr  x17, JIT_WriteBarrier_Offset_GCShadowEnd + start$name
            cmp  x12, x17
            bhs  ShadowUpdateEnd$name

        ; *pShadow = x15
            str  x15, [x12]

        ; Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
        ; conditions are caught by INVALIDGCVALUE.
            dmb  ish

        ; if ([x14] == x15) goto end
            ldr  x17, [x14]
            cmp  x17, x15
            beq ShadowUpdateEnd$name

        ; *pShadow = INVALIDGCVALUE (0xcccccccd)
            movz x17, #0xcccd
            movk x17, #0xcccc, LSL #16
            str  x17, [x12]
        #endif
ShadowUpdateEnd$name
    MEND

    MACRO
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB $name
        #ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
            ldr  x12, JIT_WriteBarrier_Offset_WriteWatchTable + start$name
        ; SoftwareWriteWatch::AddressToTableByteIndexShift
            add  x12, x12, x14, lsr #0xc
            ldrb w17, [x12]
            cbnz x17, WriteWatchForGCHeapEnd$name
            mov  w17, #0xFF
            strb w17, [x12]
WriteWatchForGCHeapEnd$name
        #endif
    MEND

    MACRO
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_STUB $name
        ; Branch to Exit if the reference is not in the ephemeral generations.
            ldr  x12, JIT_WriteBarrier_Offset_Lower + start$name
            cmp  x15, x12
            blo  exit$name
    MEND

    MACRO
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB $name
        ; Branch to Exit if the reference is not in the ephemeral generations.
            ldr  x12, JIT_WriteBarrier_Offset_Lower + start$name
            ldr  x17, JIT_WriteBarrier_Offset_Upper + start$name
            cmp  x15, x12
            ccmp x15, x17, #0x2, hs
            bhs  exit$name
    MEND

    MACRO
        WRITE_BARRIER_REGION_CHECK_STUB $name
        ; Calculate region generations
            ldr  x17, JIT_WriteBarrier_Offset_RegionToGeneration + start$name
            ldr  w12, JIT_WriteBarrier_Offset_RegionShr + start$name
            lsr  x15, x15, x12
            add  x15, x15, x17 ; x15 = (RHS >> wbs_region_shr) + wbs_region_to_generation_table
            lsr  x12, x14, x12
            add  x12, x12, x17 ; x12 = (LHS >> wbs_region_shr) + wbs_region_to_generation_table

        ; Check whether the region we are storing into is gen 0 - nothing to do in this case
            ldrb w12, [x12]
            cbz  w12, exit$name

        ; Return if the new reference is not from old to young
            ldrb w15, [x15]
            cmp  w15, w12
            bhs  exit$name
    MEND

    MACRO
        WRITE_BARRIER_CHECK_BIT_REGIONS_CARD_TABLE_STUB $name
        ; Check if we need to update the card table
            lsr w17, w14, 8
            and w17, w17, 7
            movz w15, 1
            lsl w17, w15, w17  ; w17 = 1 << (LHS >> 8 && 7)
            ldr  x12, JIT_WriteBarrier_Offset_CardTable + start$name
            add  x15, x12, x14, lsr #11
            ldrb w12, [x15]  ; w12 = [(LHS >> 11) + g_card_table]
            tst  w12, w17
            bne  exit$name

        ; Atomically update the card table
        ; Requires LSE, but the code is only compiled for 8.0
        ; stsetb w17, [x15]
            DCD 0x383131FF
    MEND

    MACRO
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB $name
        ; Check if we need to update the card table
            ldr  x12, JIT_WriteBarrier_Offset_CardTable + start$name
            add  x15, x12, x14, lsr #11
        ; w12 = [(RHS >> 11) + g_card_table]
            ldrb w12, [x15]
            cmp  x12, 0xFF
            beq  exit$name

        ; Update the card table
            mov  x12, 0xFF
            strb w12, [x15]
    MEND

    MACRO
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB $name
        #ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        ; Check if we need to update the card bundle table
            ldr  x12, JIT_WriteBarrier_Offset_CardBundleTable + start$name
            add  x15, x12, x14, lsr #21
            ldrb w12, [x15]
            cmp  x12, 0xFF
            beq  exit$name

        ; Update the card bundle
            mov  x12, 0xFF
            strb w12, [x15]
        #endif
    MEND

    MACRO
        WRITE_BARRIER_RETURN_STUB $name
exit$name
        ; Increment by 8 to implement JIT_ByRefWriteBarrier contract.
        ; TODO: Consider duplicating the logic to get rid of this redundant 'add'
        ; for JIT_WriteBarrier/JIT_CheckedWriteBarrier
            add  x14, x14, 8
            ret  lr
    MEND

    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_PreGrow64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not update the write watch table
    ;      Does not check wbs_ephemeral_high
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_PreGrow64
        WRITE_BARRIER_ENTRY_STUB PreGrow64
        WRITE_BARRIER_SHADOW_UPDATE_STUB PreGrow64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_STUB PreGrow64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB PreGrow64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB PreGrow64
        WRITE_BARRIER_RETURN_STUB PreGrow64
    WRITE_BARRIER_END JIT_WriteBarrier_PreGrow64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_PostGrow64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not update the write watch table
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_PostGrow64
        WRITE_BARRIER_ENTRY_STUB PostGrow64
        WRITE_BARRIER_SHADOW_UPDATE_STUB PostGrow64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB PostGrow64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB PostGrow64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB PostGrow64
        WRITE_BARRIER_RETURN_STUB PostGrow64
    WRITE_BARRIER_END JIT_WriteBarrier_PostGrow64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_SVR64(Object** dst, Object* src)
    ;
    ; SVR GC has multiple heaps, so it cannot provide one single ephemeral region to bounds check
    ; against, so we just skip the bounds checking all together and do our card table update unconditionally.
    ;
    ; Skipped functionality:
    ;      Does not update the write watch table
    ;      Does not check wbs_ephemeral_high or wbs_ephemeral_low
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_SVR64
        WRITE_BARRIER_ENTRY_STUB SVR64
        WRITE_BARRIER_SHADOW_UPDATE_STUB SVR64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB SVR64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB SVR64
        WRITE_BARRIER_RETURN_STUB SVR64
    WRITE_BARRIER_END JIT_WriteBarrier_SVR64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_Byte_Region64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not update the write watch table
    ;      Bitwise updates for region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_Byte_Region64
        WRITE_BARRIER_ENTRY_STUB Byte_Region64
        WRITE_BARRIER_SHADOW_UPDATE_STUB Byte_Region64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB Byte_Region64
        WRITE_BARRIER_REGION_CHECK_STUB Byte_Region64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB Byte_Region64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB Byte_Region64
        WRITE_BARRIER_RETURN_STUB Byte_Region64
    WRITE_BARRIER_END JIT_WriteBarrier_Byte_Region64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_Bit_Region64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not update the write watch table
    ;      Does not call check card table stub
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_Bit_Region64
        WRITE_BARRIER_ENTRY_STUB Bit_Region64
        WRITE_BARRIER_SHADOW_UPDATE_STUB Bit_Region64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB Bit_Region64
        WRITE_BARRIER_REGION_CHECK_STUB Bit_Region64
        WRITE_BARRIER_CHECK_BIT_REGIONS_CARD_TABLE_STUB Bit_Region64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB Bit_Region64
        WRITE_BARRIER_RETURN_STUB Bit_Region64
    WRITE_BARRIER_END JIT_WriteBarrier_Bit_Region64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_WriteWatch_PreGrow64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not check wbs_ephemeral_high
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_WriteWatch_PreGrow64
        WRITE_BARRIER_ENTRY_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_SHADOW_UPDATE_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB WriteWatch_PreGrow64
        WRITE_BARRIER_RETURN_STUB WriteWatch_PreGrow64
    WRITE_BARRIER_END JIT_WriteBarrier_WriteWatch_PreGrow64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_WriteWatch_PostGrow64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_WriteWatch_PostGrow64
        WRITE_BARRIER_ENTRY_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_SHADOW_UPDATE_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB WriteWatch_PostGrow64
        WRITE_BARRIER_RETURN_STUB WriteWatch_PostGrow64
    WRITE_BARRIER_END JIT_WriteBarrier_WriteWatch_PostGrow64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_WriteWatch_SVR64(Object** dst, Object* src)
    ;
    ; SVR GC has multiple heaps, so it cannot provide one single ephemeral region to bounds check
    ; against, so we just skip the bounds checking all together and do our card table update unconditionally.
    ;
    ; Skipped functionality:
    ;      Does not check wbs_ephemeral_high or wbs_ephemeral_low
    ;      No region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_WriteWatch_SVR64
        WRITE_BARRIER_ENTRY_STUB WriteWatch_SVR64
        WRITE_BARRIER_SHADOW_UPDATE_STUB WriteWatch_SVR64
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB WriteWatch_SVR64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB WriteWatch_SVR64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB WriteWatch_SVR64
        WRITE_BARRIER_RETURN_STUB WriteWatch_SVR64
    WRITE_BARRIER_END JIT_WriteBarrier_WriteWatch_SVR64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_WriteWatch_Byte_Region64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Bitwise updates for region checks
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_WriteWatch_Byte_Region64
        WRITE_BARRIER_ENTRY_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_SHADOW_UPDATE_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_REGION_CHECK_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_CHECK_CARD_TABLE_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB WriteWatch_Byte_Region64
        WRITE_BARRIER_RETURN_STUB WriteWatch_Byte_Region64
    WRITE_BARRIER_END JIT_WriteBarrier_WriteWatch_Byte_Region64


    ;-----------------------------------------------------------------------------
    ; void JIT_WriteBarrier_WriteWatch_Bit_Region64(Object** dst, Object* src)
    ;
    ; Skipped functionality:
    ;      Does not call check card table stub
    ;
    WRITE_BARRIER_ENTRY JIT_WriteBarrier_WriteWatch_Bit_Region64
        WRITE_BARRIER_ENTRY_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_SHADOW_UPDATE_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_WRITE_WATCH_FOR_GC_HEAP_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_CHECK_EPHEMERAL_LOW_AND_HIGH_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_REGION_CHECK_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_CHECK_BIT_REGIONS_CARD_TABLE_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_CHECK_CARD_BUNDLE_TABLE_STUB WriteWatch_Bit_Region64
        WRITE_BARRIER_RETURN_STUB WriteWatch_Bit_Region64
    WRITE_BARRIER_END JIT_WriteBarrier_WriteWatch_Bit_Region64


; Must be at very end of file
    END
