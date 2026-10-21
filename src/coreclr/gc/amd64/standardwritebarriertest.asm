; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

CLOBBER_VOLATILE_INTEGER_STATE MACRO
        xor     eax, eax
        xor     ecx, ecx
        xor     edx, edx
        xor     r8d, r8d
        xor     r9d, r9d
        xor     r10d, r10d
        xor     r11d, r11d
ENDM

.code

PUBLIC WriteBarrierTestGetClobberMask
WriteBarrierTestGetClobberMask PROC FRAME
        push    rbx
        .pushreg rbx
        .endprolog

        mov     r8d, 1
        xor     r11d, r11d
        xor     eax, eax
        cpuid
        mov     r10d, eax

        mov     eax, 1
        cpuid
        test    ecx, 08000000h
        jz      DetectApx
        mov     r11d, 1
        test    ecx, 10000000h
        jz      DetectApx

        xor     ecx, ecx
        xgetbv
        mov     r9d, eax
        and     eax, 6
        cmp     eax, 6
        jne     DetectApx
        mov     r8d, 2

        mov     eax, 7
        xor     ecx, ecx
        cpuid
        test    ebx, 00010000h
        jz      DetectApx

        mov     eax, r9d
        and     eax, 0e6h
        cmp     eax, 0e6h
        jne     DetectApx
        mov     r8d, 4

DetectApx:
        test    r11d, r11d
        jz      DetectionComplete
        cmp     r10d, 29h
        jb      DetectionComplete

        mov     eax, 7
        mov     ecx, 1
        cpuid
        test    edx, 00200000h
        jz      DetectionComplete

        xor     ecx, ecx
        xgetbv
        test    eax, 00080000h
        jz      DetectionComplete

        mov     eax, 29h
        xor     ecx, ecx
        cpuid
        test    ebx, 1
        jz      DetectionComplete
        or      r8d, 8

DetectionComplete:
        mov     eax, r8d
        pop     rbx
        ret
WriteBarrierTestGetClobberMask ENDP

PUBLIC WriteBarrierTestClobberSse
WriteBarrierTestClobberSse PROC
        pxor    xmm0, xmm0
        pxor    xmm1, xmm1
        pxor    xmm2, xmm2
        pxor    xmm3, xmm3
        pxor    xmm4, xmm4
        pxor    xmm5, xmm5
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
WriteBarrierTestClobberSse ENDP

PUBLIC WriteBarrierTestClobberAvx
WriteBarrierTestClobberAvx PROC
        vxorps  ymm0, ymm0, ymm0
        vxorps  ymm1, ymm1, ymm1
        vxorps  ymm2, ymm2, ymm2
        vxorps  ymm3, ymm3, ymm3
        vxorps  ymm4, ymm4, ymm4
        vxorps  ymm5, ymm5, ymm5
        vzeroupper
        CLOBBER_VOLATILE_INTEGER_STATE
        ret
WriteBarrierTestClobberAvx ENDP

PUBLIC WriteBarrierTestClobberAvx512
WriteBarrierTestClobberAvx512 PROC
        vpxord  zmm0, zmm0, zmm0
        vpxord  zmm1, zmm1, zmm1
        vpxord  zmm2, zmm2, zmm2
        vpxord  zmm3, zmm3, zmm3
        vpxord  zmm4, zmm4, zmm4
        vpxord  zmm5, zmm5, zmm5
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
WriteBarrierTestClobberAvx512 ENDP

PUBLIC WriteBarrierTestClobberApx
WriteBarrierTestClobberApx PROC
        ; xor r16d-r23d with themselves (REX2.R4/B4)
        db      0d5h, 050h, 031h, 0c0h
        db      0d5h, 050h, 031h, 0c9h
        db      0d5h, 050h, 031h, 0d2h
        db      0d5h, 050h, 031h, 0dbh
        db      0d5h, 050h, 031h, 0e4h
        db      0d5h, 050h, 031h, 0edh
        db      0d5h, 050h, 031h, 0f6h
        db      0d5h, 050h, 031h, 0ffh

        ; xor r24d-r31d with themselves (REX2.R4/R3/B4/B3)
        db      0d5h, 055h, 031h, 0c0h
        db      0d5h, 055h, 031h, 0c9h
        db      0d5h, 055h, 031h, 0d2h
        db      0d5h, 055h, 031h, 0dbh
        db      0d5h, 055h, 031h, 0e4h
        db      0d5h, 055h, 031h, 0edh
        db      0d5h, 055h, 031h, 0f6h
        db      0d5h, 055h, 031h, 0ffh

        CLOBBER_VOLATILE_INTEGER_STATE
        ret
WriteBarrierTestClobberApx ENDP

END
