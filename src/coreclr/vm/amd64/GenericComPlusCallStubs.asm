; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

ifdef FEATURE_COMINTEROP

include AsmMacros.inc
include asmconstants.inc


extern CLRToCOMWorker:proc
extern ProcessCLRException:proc


NESTED_ENTRY GenericComPlusCallStub, _TEXT, ProcessCLRException

        PROLOG_WITH_TRANSITION_BLOCK 8

        ;
        ; Call CLRToCOMWorker.
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov             rdx, r10                            ; MethodDesc *
        call            CLRToCOMWorker

        ; handle FP return values

        lea             rcx, [rsp + __PWTB_FloatArgumentRegisters - 8]
        cmp             rax, 4
        jne             @F
        movss           xmm0, real4 ptr [rcx]
@@:
        cmp             rax, 8
        jne             @F
        movsd           xmm0, real8 ptr [rcx]
@@:
        ; load return value
        mov             rax, [rcx]

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

NESTED_END GenericComPlusCallStub, _TEXT

endif ; FEATURE_COMINTEROP

        end
