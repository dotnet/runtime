; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_FastWriteBarriers.asm
;
; Notes: these are the fast write barriers which are copied in to the
;        JIT_WriteBarrier buffer (found in JitHelpers_Fast.asm).
;        This code should never be executed at runtime and should end
;        up effectively being treated as data.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc


; Two super fast helpers that together do the work of JIT_WriteBarrier.  These
; use inlined ephemeral region bounds and an inlined pointer to the card table.
;
; Until the GC does some major reshuffling, the ephemeral region will always be
; at the top of the heap, so given that we know the reference is inside the
; heap, we don't have to check against the upper bound of the ephemeral region
; (PreGrow version).  Once the GC moves the ephemeral region, this will no longer
; be valid, so we use the PostGrow version to check both the upper and lower
; bounds. The inlined bounds and card table pointers have to be patched
; whenever they change.
;
; At anyone time, the memory pointed to by JIT_WriteBarrier will contain one
; of these functions.  See StompWriteBarrierResize and StompWriteBarrierEphemeral
; in VM\AMD64\JITInterfaceAMD64.cpp and InitJITHelpers1 in VM\JITInterfaceGen.cpp
; for more info.
;
; READ THIS!!!!!!
; it is imperative that the addresses of the values that we overwrite
; (card table, ephemeral region ranges, etc) are naturally aligned since
; there are codepaths that will overwrite these values while the EE is running.
;
LEAF_ENTRY JIT_WriteBarrier_PreGrow64, _TEXT
        align 8
        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        NOP_3_BYTE ; padding for alignment of constant

        ; Can't compare a 64 bit immediate, so we have to move it into a
        ; register.  Value of this immediate will be patched at runtime.
PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_Lower
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check the lower ephemeral region bound.
        cmp     rdx, rax
        jb      Exit

        nop ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     rcx, 0Ah
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_PreGrow64, _TEXT


; See comments for JIT_WriteBarrier_PreGrow (above).
LEAF_ENTRY JIT_WriteBarrier_PostGrow64, _TEXT
        align 8
        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        NOP_3_BYTE ; padding for alignment of constant

        ; Can't compare a 64 bit immediate, so we have to move them into a
        ; register.  Values of these immediates will be patched at runtime.
        ; By using two registers we can pipeline better.  Should we decide to use
        ; a special non-volatile calling convention, this should be changed to
        ; just one.
PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_Lower
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check the lower and upper ephemeral region bounds
        cmp     rdx, rax
        jb      Exit

        nop ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_Upper
        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     rcx, 0Ah
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_PostGrow64, _TEXT


ifdef FEATURE_SVR_GC

LEAF_ENTRY JIT_WriteBarrier_SVR64, _TEXT
        align 8
        ;
        ; SVR GC has multiple heaps, so it cannot provide one single
        ; ephemeral region to bounds check against, so we just skip the
        ; bounds checking all together and do our card table update
        ; unconditionally.
        ;

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        NOP_3_BYTE ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_SVR64_PatchLabel_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        shr     rcx, 0Bh

        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     rcx, 0Ah
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_SVR64, _TEXT

LEAF_ENTRY JIT_WriteBarrier_Byte_Region64, _TEXT
        align 8

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        mov     r8, rcx

PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0F0F0F0F0F0F0F0F0h

PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest
        shr     rcx, 16h ; compute region index

        ; Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [rcx + rax], 0
        jne     NotGen0
        REPRET

        NOP_2_BYTE ; padding for alignment of constant

    NotGen0:
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jae     NotLow
        ret
    NotLow:
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jb      NotHigh
        REPRET
    NotHigh:
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc
        shr     rdx, 16h ; compute region index
        mov     dl, [rdx + rax]
        cmp     dl, [rcx + rax]
        jb      isOldToYoung
        REPRET
        nop

    IsOldToYoung:
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        shr     r8, 0Bh
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [r8 + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     r8, 0Ah
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [r8 + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_Byte_Region64, _TEXT

LEAF_ENTRY JIT_WriteBarrier_Bit_Region64, _TEXT
        align 8

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        mov     r8, rcx

PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0F0F0F0F0F0F0F0F0h

PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrDest
        shr     rcx, 16h ; compute region index

        ; Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [rcx + rax], 0
        jne     NotGen0
        REPRET

        NOP_2_BYTE ; padding for alignment of constant

    NotGen0:
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jae     NotLow
        ret
    NotLow:
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_Upper
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jb      NotHigh
        REPRET
    NotHigh:
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrSrc
        shr     rdx, 16h ; compute region index
        mov     dl, [rdx + rax]
        cmp     dl, [rcx + rax]
        jb      isOldToYoung
        REPRET
        nop

    IsOldToYoung:
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        mov     ecx, r8d
        shr     r8, 0Bh
        shr     ecx, 8
        and     ecx, 7
        mov     dl, 1
        shl     dl, cl
        test    byte ptr [r8 + rax], dl
        je      UpdateCardTable
        REPRET

    UpdateCardTable:
        lock or byte ptr [r8 + rax], dl

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     r8, 0Ah
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [r8 + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_Bit_Region64, _TEXT

endif


ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

LEAF_ENTRY JIT_WriteBarrier_WriteWatch_PreGrow64, _TEXT
        align 8

        ; Regarding patchable constants:
        ; - 64-bit constants have to be loaded into a register
        ; - The constants have to be aligned to 8 bytes so that they can be patched easily
        ; - The constant loads have been located to minimize NOP padding required to align the constants
        ; - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        ;   non-volatile calling convention, this should be changed to use just one register.

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        add     rax, r8
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh

        ; Check the lower ephemeral region bound.
    CheckCardTable:
        cmp     rdx, r9
        jb      Exit

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     rcx, 0Ah
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_PreGrow64, _TEXT


LEAF_ENTRY JIT_WriteBarrier_WriteWatch_PostGrow64, _TEXT
        align 8

        ; Regarding patchable constants:
        ; - 64-bit constants have to be loaded into a register
        ; - The constants have to be aligned to 8 bytes so that they can be patched easily
        ; - The constant loads have been located to minimize NOP padding required to align the constants
        ; - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        ;   non-volatile calling convention, this should be changed to use just one register.

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        add     rax, r8
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh

        NOP_3_BYTE ; padding for alignment of constant

        ; Check the lower and upper ephemeral region bounds
    CheckCardTable:
        cmp     rdx, r9
        jb      Exit

        NOP_3_BYTE ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper
        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     rcx, 0Ah
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_PostGrow64, _TEXT


ifdef FEATURE_SVR_GC

LEAF_ENTRY JIT_WriteBarrier_WriteWatch_SVR64, _TEXT
        align 8

        ; Regarding patchable constants:
        ; - 64-bit constants have to be loaded into a register
        ; - The constants have to be aligned to 8 bytes so that they can be patched easily
        ; - The constant loads have been located to minimize NOP padding required to align the constants
        ; - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        ;   non-volatile calling convention, this should be changed to use just one register.

        ;
        ; SVR GC has multiple heaps, so it cannot provide one single
        ; ephemeral region to bounds check against, so we just skip the
        ; bounds checking all together and do our card table update
        ; unconditionally.
        ;

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable
        mov     r9, 0F0F0F0F0F0F0F0F0h
        add     rax, r8
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh

    CheckCardTable:
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + r9], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + r9], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        nop ; padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     rcx, 0Ah
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_SVR64, _TEXT

endif

LEAF_ENTRY JIT_WriteBarrier_WriteWatch_Byte_Region64, _TEXT
        align 8

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_WriteWatchTable
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, r8
        mov     r8, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrDest
        shr     rcx, 16h ; compute region index
        cmp     byte ptr [rax], 0h
        jne     JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionToGeneration
        mov     byte ptr [rax], 0FFh

PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [rcx + rax], 0
        jne     NotGen0
        REPRET

        NOP_2_BYTE ; padding for alignment of constant
        NOP_2_BYTE ; padding for alignment of constant
        NOP_2_BYTE ; padding for alignment of constant

    NotGen0:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jae     NotLow
        ret
    NotLow:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Upper
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jb      NotHigh
        REPRET
    NotHigh:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrSrc
        shr     rdx, 16h ; compute region index
        mov     dl, [rdx + rax]
        cmp     dl, [rcx + rax]
        jb      isOldToYoung
        REPRET
        nop

    IsOldToYoung:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        shr     r8, 0Bh
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [r8 + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     r8, 0Ah
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [r8 + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_Byte_Region64, _TEXT

LEAF_ENTRY JIT_WriteBarrier_WriteWatch_Bit_Region64, _TEXT
        align 8

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_WriteWatchTable
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, r8
        mov     r8, rcx
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrDest
        shr     rcx, 16h ; compute region index
        cmp     byte ptr [rax], 0h
        jne     JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration
        mov     byte ptr [rax], 0FFh

PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [rcx + rax], 0
        jne     NotGen0
        REPRET

        NOP_2_BYTE ; padding for alignment of constant
        NOP_2_BYTE ; padding for alignment of constant
        NOP_2_BYTE ; padding for alignment of constant

    NotGen0:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Lower
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jae     NotLow
        ret
    NotLow:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Upper
        mov     r9, 0F0F0F0F0F0F0F0F0h
        cmp     rdx, r9
        jb      NotHigh
        REPRET
    NotHigh:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrSrc
        shr     rdx, 16h ; compute region index
        mov     dl, [rdx + rax]
        cmp     dl, [rcx + rax]
        jb      isOldToYoung
        REPRET
        nop

    IsOldToYoung:
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardTable
        mov     rax, 0F0F0F0F0F0F0F0F0h

        mov     ecx, r8d
        shr     r8, 0Bh
        shr     ecx, 8
        and     ecx, 7
        mov     dl, 1
        shl     dl, cl
        test    byte ptr [r8 + rax], dl
        je      UpdateCardTable
        REPRET

    UpdateCardTable:
        lock or byte ptr [r8 + rax], dl
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardBundleTable
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     r8, 0Ah
        cmp     byte ptr [r8 + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [r8 + rax], 0FFh
endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_Bit_Region64, _TEXT

endif


        end
