// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "../../pal/inc/unixasmmacros.inc"

.macro CLOBBER_VOLATILE_INTEGER_STATE
        xor     eax, eax
        xor     ecx, ecx
        xor     edx, edx
        xor     esi, esi
        xor     edi, edi
        xor     r8d, r8d
        xor     r9d, r9d
        xor     r10d, r10d
        xor     r11d, r11d
.endm

NESTED_ENTRY WriteBarrierTestGetClobberMask, _TEXT, NoHandler
        push_nonvol_reg rbx
        END_PROLOGUE

        mov     r8d, 1
        xor     r11d, r11d
        xor     eax, eax
        cpuid
        mov     r10d, eax

        mov     eax, 1
        cpuid
        test    ecx, 0x08000000
        jz      LOCAL_LABEL(DetectApx)
        mov     r11d, 1
        test    ecx, 0x10000000
        jz      LOCAL_LABEL(DetectApx)

        xor     ecx, ecx
        xgetbv
        mov     r9d, eax
        and     eax, 6
        cmp     eax, 6
        jne     LOCAL_LABEL(DetectApx)
        mov     r8d, 2

        mov     eax, 7
        xor     ecx, ecx
        cpuid
        test    ebx, 0x00010000
        jz      LOCAL_LABEL(DetectApx)

        mov     eax, r9d
        and     eax, 0xE6
        cmp     eax, 0xE6
        jne     LOCAL_LABEL(DetectApx)
        mov     r8d, 4

    LOCAL_LABEL(DetectApx):
        test    r11d, r11d
        jz      LOCAL_LABEL(DetectionComplete)
        cmp     r10d, 0x29
        jb      LOCAL_LABEL(DetectionComplete)

        mov     eax, 7
        mov     ecx, 1
        cpuid
        test    edx, 0x00200000
        jz      LOCAL_LABEL(DetectionComplete)

        xor     ecx, ecx
        xgetbv
        test    eax, 0x00080000
        jz      LOCAL_LABEL(DetectionComplete)

        mov     eax, 0x29
        xor     ecx, ecx
        cpuid
        test    ebx, 1
        jz      LOCAL_LABEL(DetectionComplete)
        or      r8d, 8

    LOCAL_LABEL(DetectionComplete):
        mov     eax, r8d
        pop_nonvol_reg rbx
        ret
NESTED_END WriteBarrierTestGetClobberMask, _TEXT

LEAF_ENTRY WriteBarrierTestClobberSse, _TEXT
        pxor    xmm0, xmm0
        pxor    xmm1, xmm1
        pxor    xmm2, xmm2
        pxor    xmm3, xmm3
        pxor    xmm4, xmm4
        pxor    xmm5, xmm5
        pxor    xmm6, xmm6
        pxor    xmm7, xmm7
        pxor    xmm8, xmm8
        pxor    xmm9, xmm9
        pxor    xmm10, xmm10
        pxor    xmm11, xmm11
        pxor    xmm12, xmm12
        pxor    xmm13, xmm13
        pxor    xmm14, xmm14
        pxor    xmm15, xmm15
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
LEAF_END WriteBarrierTestClobberSse, _TEXT

LEAF_ENTRY WriteBarrierTestClobberAvx, _TEXT
        vxorps  ymm0, ymm0, ymm0
        vxorps  ymm1, ymm1, ymm1
        vxorps  ymm2, ymm2, ymm2
        vxorps  ymm3, ymm3, ymm3
        vxorps  ymm4, ymm4, ymm4
        vxorps  ymm5, ymm5, ymm5
        vxorps  ymm6, ymm6, ymm6
        vxorps  ymm7, ymm7, ymm7
        vxorps  ymm8, ymm8, ymm8
        vxorps  ymm9, ymm9, ymm9
        vxorps  ymm10, ymm10, ymm10
        vxorps  ymm11, ymm11, ymm11
        vxorps  ymm12, ymm12, ymm12
        vxorps  ymm13, ymm13, ymm13
        vxorps  ymm14, ymm14, ymm14
        vxorps  ymm15, ymm15, ymm15
        vzeroupper
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
LEAF_END WriteBarrierTestClobberAvx, _TEXT

LEAF_ENTRY WriteBarrierTestClobberAvx512, _TEXT
        vpxord  zmm0, zmm0, zmm0
        vpxord  zmm1, zmm1, zmm1
        vpxord  zmm2, zmm2, zmm2
        vpxord  zmm3, zmm3, zmm3
        vpxord  zmm4, zmm4, zmm4
        vpxord  zmm5, zmm5, zmm5
        vpxord  zmm6, zmm6, zmm6
        vpxord  zmm7, zmm7, zmm7
        vpxord  zmm8, zmm8, zmm8
        vpxord  zmm9, zmm9, zmm9
        vpxord  zmm10, zmm10, zmm10
        vpxord  zmm11, zmm11, zmm11
        vpxord  zmm12, zmm12, zmm12
        vpxord  zmm13, zmm13, zmm13
        vpxord  zmm14, zmm14, zmm14
        vpxord  zmm15, zmm15, zmm15
        vpxord  zmm16, zmm16, zmm16
        vpxord  zmm17, zmm17, zmm17
        vpxord  zmm18, zmm18, zmm18
        vpxord  zmm19, zmm19, zmm19
        vpxord  zmm20, zmm20, zmm20
        vpxord  zmm21, zmm21, zmm21
        vpxord  zmm22, zmm22, zmm22
        vpxord  zmm23, zmm23, zmm23
        vpxord  zmm24, zmm24, zmm24
        vpxord  zmm25, zmm25, zmm25
        vpxord  zmm26, zmm26, zmm26
        vpxord  zmm27, zmm27, zmm27
        vpxord  zmm28, zmm28, zmm28
        vpxord  zmm29, zmm29, zmm29
        vpxord  zmm30, zmm30, zmm30
        vpxord  zmm31, zmm31, zmm31
        kxnorw  k1, k1, k1
        kxnorw  k2, k2, k2
        kxnorw  k3, k3, k3
        kxnorw  k4, k4, k4
        kxnorw  k5, k5, k5
        kxnorw  k6, k6, k6
        kxnorw  k7, k7, k7
        vzeroupper
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
LEAF_END WriteBarrierTestClobberAvx512, _TEXT

LEAF_ENTRY WriteBarrierTestClobberApx, _TEXT
        .byte   0xD5, 0x50, 0x31, 0xC0
        .byte   0xD5, 0x50, 0x31, 0xC9
        .byte   0xD5, 0x50, 0x31, 0xD2
        .byte   0xD5, 0x50, 0x31, 0xDB
        .byte   0xD5, 0x50, 0x31, 0xE4
        .byte   0xD5, 0x50, 0x31, 0xED
        .byte   0xD5, 0x50, 0x31, 0xF6
        .byte   0xD5, 0x50, 0x31, 0xFF
        .byte   0xD5, 0x55, 0x31, 0xC0
        .byte   0xD5, 0x55, 0x31, 0xC9
        .byte   0xD5, 0x55, 0x31, 0xD2
        .byte   0xD5, 0x55, 0x31, 0xDB
        .byte   0xD5, 0x55, 0x31, 0xE4
        .byte   0xD5, 0x55, 0x31, 0xED
        .byte   0xD5, 0x55, 0x31, 0xF6
        .byte   0xD5, 0x55, 0x31, 0xFF
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
LEAF_END WriteBarrierTestClobberApx, _TEXT
