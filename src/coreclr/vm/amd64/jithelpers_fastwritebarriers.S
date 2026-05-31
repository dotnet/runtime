// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"


        .balign 16
LEAF_ENTRY JIT_WriteBarrier_PreGrow64, _TEXT
        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        NOP_3_BYTE // padding for alignment of constant

        // Can't compare a 64 bit immediate, so we have to move it into a
        // register.  Value of this immediate will be patched at runtime.
PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_Lower
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Check the lower ephemeral region bound.
        cmp     rsi, rax

        .byte 0x72, Exit_PreGrow64 - . - 1

        nop // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card table entry, if not already dirty.
        shr     r11, 0x0B
        cmp     byte ptr [r11 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_PreGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_PreGrow64):
        mov     byte ptr [r11 + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_6_BYTE // padding for alignment of constant

        // Extra 6-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
PATCH_LABEL JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card bundle, if not already dirty.
        // r11 is already shifted by 0xB, so shift by 0xA more
        shr     r11, 0x0A
        cmp     byte ptr [r11 + rax], 0xFF

        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_PreGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_PreGrow64):
        mov     byte ptr [r11 + rax], 0xFF
#endif

        ret

    .balign 16
PATCH_LABEL Exit_PreGrow64
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_PreGrow64, _TEXT


        .balign 16
// See comments for JIT_WriteBarrier_PreGrow (above).
LEAF_ENTRY JIT_WriteBarrier_PostGrow64, _TEXT
        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        NOP_3_BYTE // padding for alignment of constant

        // Can't compare a 64 bit immediate, so we have to move them into a
        // register.  Values of these immediates will be patched at runtime.
        // By using two registers we can pipeline better.  Should we decide to use
        // a special non-volatile calling convention, this should be changed to
        // just one.
PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_Lower
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Check the lower and upper ephemeral region bounds
        cmp     rsi, rax

        .byte 0x72, Exit_PostGrow64 - . - 1

        nop // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_Upper
        movabs  r8, 0xF0F0F0F0F0F0F0F0

        cmp     rsi, r8

        .byte 0x73, Exit_PostGrow64 - . - 1

        nop // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card table entry, if not already dirty.
        shr     r11, 0x0B
        cmp     byte ptr [r11 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_PostGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_PostGrow64):
        mov     byte ptr [r11 + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_6_BYTE // padding for alignment of constant

        // Extra 6-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
PATCH_LABEL JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card bundle, if not already dirty.
        // r11 is already shifted by 0xB, so shift by 0xA more
        shr     r11, 0x0A
        cmp     byte ptr [r11 + rax], 0xFF

        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_PostGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_PostGrow64):
        mov     byte ptr [r11 + rax], 0xFF
#endif

        ret

    .balign 16
PATCH_LABEL Exit_PostGrow64
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_PostGrow64, _TEXT


#ifdef FEATURE_SVR_GC

        .balign 16
LEAF_ENTRY JIT_WriteBarrier_SVR64, _TEXT
        //
        // SVR GC has multiple heaps, so it cannot provide one single
        // ephemeral region to bounds check against, so we just skip the
        // bounds checking all together and do our card table update
        // unconditionally.
        //

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        NOP_3_BYTE // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_SVR64_PatchLabel_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        shr     r11, 0x0B

        cmp     byte ptr [r11 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_SVR64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_SVR64):
        mov     byte ptr [r11 + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_6_BYTE // padding for alignment of constant

        // Extra 6-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
PATCH_LABEL JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Shift the address by 0xA more since already shifted by 0xB
        shr     r11, 0x0A
        cmp     byte ptr [r11 + rax], 0xFF

        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_SVR64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_SVR64):
        mov     byte ptr [r11 + rax], 0xFF
#endif

        ret

LEAF_END_MARKED JIT_WriteBarrier_SVR64, _TEXT

#endif

        .balign 16
LEAF_ENTRY JIT_WriteBarrier_Byte_Region64, _TEXT

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        mov     r8, r11

PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration
        movabs  rax, 0xF0F0F0F0F0F0F0F0

PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest
        shr     r11, 0x16 // compute region index

        // Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [r11 + rax], 0
        .byte 0x75, LOCAL_LABEL(NotGen0_Byte_Region64) - . - 1
        REPRET

        NOP_2_BYTE // padding for alignment of constant

    LOCAL_LABEL(NotGen0_Byte_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x73, LOCAL_LABEL(NotLow_Byte_Region64) - . - 1
        ret
    LOCAL_LABEL(NotLow_Byte_Region64):
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x72, LOCAL_LABEL(NotHigh_Byte_Region64) - . - 1
        REPRET
    LOCAL_LABEL(NotHigh_Byte_Region64):
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc
        shr     rsi, 0x16 // compute region index
        mov     dl, [rsi + rax]
        cmp     dl, [r11 + rax]
        .byte 0x72, LOCAL_LABEL(IsOldToYoung_Byte_Region64) - . - 1
        REPRET
        nop

    LOCAL_LABEL(IsOldToYoung_Byte_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        shr     r8, 0xB
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_Byte_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_Byte_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     r8, 0x0A
PATCH_LABEL JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardBundleTable_Byte_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundleTable_Byte_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_Byte_Region64, _TEXT
        .balign 16
LEAF_ENTRY JIT_WriteBarrier_Bit_Region64, _TEXT

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        mov     r8, r11

PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionToGeneration
        movabs  rax, 0xF0F0F0F0F0F0F0F0

PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrDest
        shr     r11, 0x16 // compute region index

        // Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [r11 + rax], 0
        .byte 0x75, LOCAL_LABEL(NotGen0_Bit_Region64) - . - 1
        REPRET

        NOP_2_BYTE // padding for alignment of constant

    LOCAL_LABEL(NotGen0_Bit_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x73, LOCAL_LABEL(NotLow_Bit_Region64) - . - 1
        ret
    LOCAL_LABEL(NotLow_Bit_Region64):
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_Upper
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x72, LOCAL_LABEL(NotHigh_Bit_Region64) - . - 1
        REPRET
    LOCAL_LABEL(NotHigh_Bit_Region64):
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrSrc
        shr     rsi, 0x16 // compute region index
        mov     dl, [rsi + rax]
        cmp     dl, [r11 + rax]
        .byte 0x72, LOCAL_LABEL(IsOldToYoung_Bit_Region64) - . - 1
        REPRET
        nop

    LOCAL_LABEL(IsOldToYoung_Bit_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        mov     ecx, r8d
        shr     r8, 0xB
        shr     ecx, 8
        and     ecx, 7
        mov     dl, 1
        shl     dl, cl
        test    byte ptr [r8 + rax], dl
        .byte 0x74, LOCAL_LABEL(UpdateCardTable_Bit_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_Bit_Region64):
        lock or byte ptr [r8 + rax], dl
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
PATCH_LABEL JIT_WriteBarrier_Bit_Region64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0
        shr     r8, 0x0A
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardBundleTable_Bit_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundleTable_Bit_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_Bit_Region64, _TEXT

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        .balign 16
LEAF_ENTRY JIT_WriteBarrier_WriteWatch_PreGrow64, _TEXT
        // Regarding patchable constants:
        // - 64-bit constants have to be loaded into a register
        // - The constants have to be aligned to 8 bytes so that they can be patched easily
        // - The constant loads have been located to minimize NOP padding required to align the constants
        // - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        //   non-volatile calling convention, this should be changed to use just one register.

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        // Update the write watch table if necessary
        mov     rax, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable
        movabs  r10, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0x0C // SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE // padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        add     rax, r10
        cmp     byte ptr [rax], 0x0
        .byte 0x75, LOCAL_LABEL(CheckCardTable_WriteWatch_PreGrow64) - . - 1
        mov     byte ptr [rax], 0xFF

    LOCAL_LABEL(CheckCardTable_WriteWatch_PreGrow64):
        // Check the lower ephemeral region bound.
        cmp     rsi, r9

        .byte 0x72, Exit_WriteWatch_PreGrow64 - . - 1

        // Touch the card table entry, if not already dirty.
        shr     r11, 0x0B
        NOP_2_BYTE // padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0
        cmp     byte ptr [r11 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_WriteWatch_PreGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_WriteWatch_PreGrow64):
        mov     byte ptr [r11 + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_2_BYTE // padding for alignment of constant
        // Extra 6-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        shr     r11, 0x0A
        cmp     byte ptr [r11 + rax], 0xFF

        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_WriteWatch_PreGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_WriteWatch_PreGrow64):
        mov     byte ptr [r11 + rax], 0xFF
#endif

        ret

    .balign 16
PATCH_LABEL Exit_WriteWatch_PreGrow64
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_PreGrow64, _TEXT


        .balign 16
LEAF_ENTRY JIT_WriteBarrier_WriteWatch_PostGrow64, _TEXT
        // Regarding patchable constants:
        // - 64-bit constants have to be loaded into a register
        // - The constants have to be aligned to 8 bytes so that they can be patched easily
        // - The constant loads have been located to minimize NOP padding required to align the constants
        // - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        //   non-volatile calling convention, this should be changed to use just one register.

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        // Update the write watch table if necessary
        mov     rax, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable
        movabs  r10, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0x0C // SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE // padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        add     rax, r10
        cmp     byte ptr [rax], 0x0
        .byte 0x75, LOCAL_LABEL(CheckCardTable_WriteWatch_PostGrow64) - . - 1
        mov     byte ptr [rax], 0xFF

        NOP_3_BYTE // padding for alignment of constant

        // Check the lower and upper ephemeral region bounds
    LOCAL_LABEL(CheckCardTable_WriteWatch_PostGrow64):
        cmp     rsi, r9

        .byte 0x72, Exit_WriteWatch_PostGrow64 - . - 1

        NOP_3_BYTE // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper
        movabs  r10, 0xF0F0F0F0F0F0F0F0

        cmp     rsi, r10

        .byte 0x73, Exit_WriteWatch_PostGrow64 - . - 1

        nop // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card table entry, if not already dirty.
        shr     r11, 0x0B
        cmp     byte ptr [r11 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_WriteWatch_PostGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_WriteWatch_PostGrow64):
        mov     byte ptr [r11 + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_2_BYTE // padding for alignment of constant
        shr     r11, 0x0A

        // Extra 6-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
PATCH_LABEL JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable
        movabs  rax, 0xF0F0F0F0F0F0F0F0
        cmp     byte ptr [r11 + rax], 0xFF

        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_WriteWatch_PostGrow64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_WriteWatch_PostGrow64):
        mov     byte ptr [r11 + rax], 0xFF
#endif

        ret
    .balign 16
PATCH_LABEL Exit_WriteWatch_PostGrow64
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_PostGrow64, _TEXT


#ifdef FEATURE_SVR_GC

        .balign 16
LEAF_ENTRY JIT_WriteBarrier_WriteWatch_SVR64, _TEXT
        // Regarding patchable constants:
        // - 64-bit constants have to be loaded into a register
        // - The constants have to be aligned to 8 bytes so that they can be patched easily
        // - The constant loads have been located to minimize NOP padding required to align the constants
        // - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        //   non-volatile calling convention, this should be changed to use just one register.

        //
        // SVR GC has multiple heaps, so it cannot provide one single
        // ephemeral region to bounds check against, so we just skip the
        // bounds checking altogether and do our card table update
        // unconditionally.
        //

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        // Update the write watch table if necessary
        mov     rax, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable
        movabs  r10, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE // padding for alignment of constant
PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        add     rax, r10
        cmp     byte ptr [rax], 0x0
        .byte 0x75, LOCAL_LABEL(CheckCardTable_WriteWatch_SVR64) - . - 1
        mov     byte ptr [rax], 0xFF

    LOCAL_LABEL(CheckCardTable_WriteWatch_SVR64):
        shr     r11, 0x0B
        cmp     byte ptr [r11 + r9], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_WriteWatch_SVR64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_WriteWatch_SVR64):
        mov     byte ptr [r11 + r9], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP // padding for alignment of constant

PATCH_LABEL JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable
        movabs  r9, 0xF0F0F0F0F0F0F0F0

        shr     r11, 0x0A
        cmp     byte ptr [r11 + r9], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardBundle_WriteWatch_SVR64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundle_WriteWatch_SVR64):
        mov     byte ptr [r11 + r9], 0xFF
#endif

        ret

LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_SVR64, _TEXT

#endif
        .balign 16
LEAF_ENTRY JIT_WriteBarrier_WriteWatch_Byte_Region64, _TEXT

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        // Update the write watch table if necessary
        mov     rax, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_WriteWatchTable
        movabs  r8, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0x0C // SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, r8
        mov     r8, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrDest
        shr     r11, 0x16 // compute region index
        cmp     byte ptr [rax], 0x0
        .byte 0x75, LOCAL_LABEL(CheckGen0_WriteWatch_Byte_Region64) - . - 1
        mov     byte ptr [rax], 0xFF
    LOCAL_LABEL(CheckGen0_WriteWatch_Byte_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0xF0F0F0F0F0F0F0F0

        // Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [r11 + rax], 0
        .byte 0x75, LOCAL_LABEL(NotGen0_WriteWatch_Byte_Region64) - . - 1
        REPRET

        NOP_2_BYTE // padding for alignment of constant
        NOP_2_BYTE // padding for alignment of constant
        NOP_2_BYTE // padding for alignment of constant

    LOCAL_LABEL(NotGen0_WriteWatch_Byte_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x73, LOCAL_LABEL(NotLow_WriteWatch_Byte_Region64) - . - 1
        ret
    LOCAL_LABEL(NotLow_WriteWatch_Byte_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Upper
        mov     r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x72, LOCAL_LABEL(NotHigh_WriteWatch_Byte_Region64) - . - 1
        REPRET
    LOCAL_LABEL(NotHigh_WriteWatch_Byte_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrSrc
        shr     rsi, 0x16 // compute region index
        mov     dl, [rsi + rax]
        cmp     dl, [r11 + rax]
        .byte 0x72, LOCAL_LABEL(IsOldToYoung_WriteWatch_Byte_Region64) - . - 1
        REPRET
        nop

    LOCAL_LABEL(IsOldToYoung_WriteWatch_Byte_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardTable
        mov     rax, 0xF0F0F0F0F0F0F0F0

        shr     r8, 0xB
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardTable_WriteWatch_Byte_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_WriteWatch_Byte_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        shr     r8, 0x0A
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardBundleTable
        mov     rax, 0xF0F0F0F0F0F0F0F0
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardBundleTable_WriteWatch_Byte_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundleTable_WriteWatch_Byte_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_Byte_Region64, _TEXT
        .balign 16
LEAF_ENTRY JIT_WriteBarrier_WriteWatch_Bit_Region64, _TEXT

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [r11], rsi

        // Update the write watch table if necessary
        mov     rax, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_WriteWatchTable
        movabs  r8, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0x0C // SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, r8
        mov     r8, r11
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrDest
        shr     r11, 0x16 // compute region index
        cmp     byte ptr [rax], 0x0
        .byte 0x75, LOCAL_LABEL(CheckGen0_WriteWatch_Bit_Region64) - . - 1
        mov     byte ptr [rax], 0xFF
    LOCAL_LABEL(CheckGen0_WriteWatch_Bit_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration
        mov     rax, 0xF0F0F0F0F0F0F0F0

        // Check whether the region we're storing into is gen 0 - nothing to do in this case
        cmp     byte ptr [r11 + rax], 0
        .byte 0x75, LOCAL_LABEL(NotGen0_WriteWatch_Bit_Region64) - . - 1
        REPRET

        NOP_2_BYTE // padding for alignment of constant
        NOP_2_BYTE // padding for alignment of constant
        NOP_2_BYTE // padding for alignment of constant

    LOCAL_LABEL(NotGen0_WriteWatch_Bit_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Lower
        movabs  r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x73, LOCAL_LABEL(NotLow_WriteWatch_Bit_Region64) - . - 1
        ret
    LOCAL_LABEL(NotLow_WriteWatch_Bit_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Upper
        mov     r9, 0xF0F0F0F0F0F0F0F0
        cmp     rsi, r9
        .byte 0x72, LOCAL_LABEL(NotHigh_WriteWatch_Bit_Region64) - . - 1
        REPRET
    LOCAL_LABEL(NotHigh_WriteWatch_Bit_Region64):
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrSrc
        shr     rsi, 0x16 // compute region index
        mov     dl, [rsi + rax]
        cmp     dl, [r11 + rax]
        .byte 0x72, LOCAL_LABEL(IsOldToYoung_WriteWatch_Bit_Region64) - . - 1
        REPRET
        nop

    LOCAL_LABEL(IsOldToYoung_WriteWatch_Bit_Region64):
        // Extra 7-byte pad: r11 REX growth on prior byte ops shifted
        // this patchable movabs immediate; restore 8-byte alignment.
        NOP_3_BYTE
        NOP_3_BYTE
        nop
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardTable
        mov     rax, 0xF0F0F0F0F0F0F0F0

        mov     ecx, r8d
        shr     r8, 0xB
        shr     ecx, 8
        and     ecx, 7
        mov     dl, 1
        shl     dl, cl
        test    byte ptr [r8 + rax], dl
        .byte 0x74, LOCAL_LABEL(UpdateCardTable_WriteWatch_Bit_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardTable_WriteWatch_Bit_Region64):
        lock or byte ptr [r8 + rax], dl
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
PATCH_LABEL JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardBundleTable
        mov     rax, 0xF0F0F0F0F0F0F0F0
        shr     r8, 0x0A
        cmp     byte ptr [r8 + rax], 0xFF
        .byte 0x75, LOCAL_LABEL(UpdateCardBundleTable_WriteWatch_Bit_Region64) - . - 1
        REPRET

    LOCAL_LABEL(UpdateCardBundleTable_WriteWatch_Bit_Region64):
        mov     byte ptr [r8 + rax], 0xFF
#endif
        ret
LEAF_END_MARKED JIT_WriteBarrier_WriteWatch_Bit_Region64, _TEXT

#endif
