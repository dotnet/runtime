; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

ifdef FEATURE_COMINTEROP

include AsmMacros.inc
include asmconstants.inc

; extern "C" const BYTE* ComPreStubWorker(UMEntryThunkData* pEntryThunkInfo);
extern ComPreStubWorker:proc
extern UMEntryPrestubUnwindFrameChainHandler:proc

NESTED_ENTRY ComCallPreStub, _TEXT, UMEntryPrestubUnwindFrameChainHandler

ComCallPreStub_STACK_FRAME_SIZE = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES

; XMM save area
ComCallPreStub_XMM_SAVE_OFFSET = ComCallPreStub_STACK_FRAME_SIZE
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + SIZEOF_MAX_FP_ARG_SPILL

ComCallPreStub_NONVOL_REG_SAVE_OFFSET = ComCallPreStub_STACK_FRAME_SIZE
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 8 ; for one non-volatile register to preserve the secret argument

; Ensure that the new rsp will be 16-byte aligned.  Note that the caller has
; already pushed the return address.
if ((ComCallPreStub_STACK_FRAME_SIZE + 8) MOD 16) ne 0
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 8
endif

        alloc_stack     ComCallPreStub_STACK_FRAME_SIZE

        save_reg_postrsp    rcx, ComCallPreStub_STACK_FRAME_SIZE + 8h
        save_reg_postrsp    rdx, ComCallPreStub_STACK_FRAME_SIZE + 10h
        save_reg_postrsp    r8,  ComCallPreStub_STACK_FRAME_SIZE + 18h
        save_reg_postrsp    r9,  ComCallPreStub_STACK_FRAME_SIZE + 20h

        save_xmm128_postrsp xmm0, ComCallPreStub_XMM_SAVE_OFFSET
        save_xmm128_postrsp xmm1, ComCallPreStub_XMM_SAVE_OFFSET + 10h
        save_xmm128_postrsp xmm2, ComCallPreStub_XMM_SAVE_OFFSET + 20h
        save_xmm128_postrsp xmm3, ComCallPreStub_XMM_SAVE_OFFSET + 30h

        save_reg_postrsp    r12, ComCallPreStub_NONVOL_REG_SAVE_OFFSET

        END_PROLOGUE

        ;
        ; Do prestub-specific stuff
        ;
        mov             rcx, METHODDESC_REGISTER
        mov             r12, METHODDESC_REGISTER
        call            ComPreStubWorker

        ; Restore the secret argument
        mov             METHODDESC_REGISTER, r12

        ;
        ; we're going to tail call to the exec stub that we just setup
        ; or return the specified error
        ;

        mov             rcx, [rsp + ComCallPreStub_STACK_FRAME_SIZE + 8h]
        mov             rdx, [rsp + ComCallPreStub_STACK_FRAME_SIZE + 10h]
        mov             r8,  [rsp + ComCallPreStub_STACK_FRAME_SIZE + 18h]
        mov             r9,  [rsp + ComCallPreStub_STACK_FRAME_SIZE + 20h]

        movdqa          xmm0, xmmword ptr [rsp + ComCallPreStub_XMM_SAVE_OFFSET]
        movdqa          xmm1, xmmword ptr [rsp + ComCallPreStub_XMM_SAVE_OFFSET + 10h]
        movdqa          xmm2, xmmword ptr [rsp + ComCallPreStub_XMM_SAVE_OFFSET + 20h]
        movdqa          xmm3, xmmword ptr [rsp + ComCallPreStub_XMM_SAVE_OFFSET + 30h]

        mov             r12, [rsp + ComCallPreStub_NONVOL_REG_SAVE_OFFSET]

        ;
        ; epilogue
        ;
        add             rsp, ComCallPreStub_STACK_FRAME_SIZE
        TAILJMP_RAX

NESTED_END ComCallPreStub, _TEXT

endif ; FEATURE_COMINTEROP

        end

