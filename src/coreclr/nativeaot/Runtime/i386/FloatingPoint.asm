;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .xmm
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

FASTCALL_FUNC   RhpFltRemRev, 8     ; float dividend, float divisor

        fld         dword ptr [esp+8]       ; divisor
        fld         dword ptr [esp+4]       ; dividend

fremloop:
        fprem
        wait
        fnstsw      ax
        wait
        sahf
        jp          fremloop    ; Continue while the FPU status bit C2 is set

        fxch        st(1)       ; swap, so divisor is on top and result is in st(1)
        fstp        st(0)       ; Pop the divisor from the FP stack

        ret         8

FASTCALL_ENDFUNC

FASTCALL_FUNC   RhpDblRemRev, 16    ; double dividend, double divisor

        fld         qword ptr [esp+0Ch]
        fld         qword ptr [esp+4]

fremloopd:
        fprem
        wait
        fnstsw      ax
        wait
        sahf
        jp          fremloopd   ; Continue while the FPU status bit C2 is set

        fxch        st(1)       ; swap, so divisor is on top and result is in st(1)
        fstp        st(0)       ; Pop the divisor from the FP stack

        ret         10h

FASTCALL_ENDFUNC


FASTCALL_FUNC   RhpFltRemRev_SSE2, 0     ; float dividend, float divisor
        sub        esp, 12             ;; 4 bytes of our stack, 8 bytes args
        movd       dword ptr [esp], xmm0
        movd       dword ptr [esp+4], xmm1
        call       @RhpFltRemRev@8     ;; pops 8 bytes of stack
        fstp       dword ptr [esp]
        movd       xmm0, dword ptr [esp]
        add        esp, 4
        ret
FASTCALL_ENDFUNC

FASTCALL_FUNC   RhpDblRemRev_SSE2, 0     ; float dividend, float divisor
        sub        esp, 24               ;; 8 bytes of our stack, 16 bytes args
        movq       qword ptr [esp], xmm0
        movq       qword ptr [esp+8], xmm1
        call       @RhpDblRemRev@16      ;; pops 16 bytes of stack
        fstp       qword ptr [esp]
        movq       xmm0, qword ptr [esp]
        add        esp, 8
        ret
FASTCALL_ENDFUNC


        end
