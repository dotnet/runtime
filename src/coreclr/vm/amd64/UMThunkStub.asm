; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

extern TheUMEntryPrestubWorker:proc
extern UMEntryPrestubUnwindFrameChainHandler:proc

;
; METHODDESC_REGISTER: UMEntryThunk*
;
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UMEntryPrestubUnwindFrameChainHandler

TheUMEntryPrestub_STACK_FRAME_SIZE = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES

; XMM save area
TheUMEntryPrestub_XMM_SAVE_OFFSET = TheUMEntryPrestub_STACK_FRAME_SIZE
TheUMEntryPrestub_STACK_FRAME_SIZE = TheUMEntryPrestub_STACK_FRAME_SIZE + SIZEOF_MAX_FP_ARG_SPILL

; Ensure that the new rsp will be 16-byte aligned.  Note that the caller has
; already pushed the return address.
if ((TheUMEntryPrestub_STACK_FRAME_SIZE + 8) MOD 16) ne 0
TheUMEntryPrestub_STACK_FRAME_SIZE = TheUMEntryPrestub_STACK_FRAME_SIZE + 8
endif

        alloc_stack     TheUMEntryPrestub_STACK_FRAME_SIZE

        save_reg_postrsp    rcx, TheUMEntryPrestub_STACK_FRAME_SIZE + 8h
        save_reg_postrsp    rdx, TheUMEntryPrestub_STACK_FRAME_SIZE + 10h
        save_reg_postrsp    r8,  TheUMEntryPrestub_STACK_FRAME_SIZE + 18h
        save_reg_postrsp    r9,  TheUMEntryPrestub_STACK_FRAME_SIZE + 20h

        save_xmm128_postrsp xmm0, TheUMEntryPrestub_XMM_SAVE_OFFSET
        save_xmm128_postrsp xmm1, TheUMEntryPrestub_XMM_SAVE_OFFSET + 10h
        save_xmm128_postrsp xmm2, TheUMEntryPrestub_XMM_SAVE_OFFSET + 20h
        save_xmm128_postrsp xmm3, TheUMEntryPrestub_XMM_SAVE_OFFSET + 30h

        END_PROLOGUE

        ;
        ; Do prestub-specific stuff
        ;
        mov             rcx, METHODDESC_REGISTER
        call            TheUMEntryPrestubWorker

        ;
        ; we're going to tail call to the exec stub that we just setup
        ;

        mov             rcx, [rsp + TheUMEntryPrestub_STACK_FRAME_SIZE + 8h]
        mov             rdx, [rsp + TheUMEntryPrestub_STACK_FRAME_SIZE + 10h]
        mov             r8,  [rsp + TheUMEntryPrestub_STACK_FRAME_SIZE + 18h]
        mov             r9,  [rsp + TheUMEntryPrestub_STACK_FRAME_SIZE + 20h]

        movdqa          xmm0, xmmword ptr [rsp + TheUMEntryPrestub_XMM_SAVE_OFFSET]
        movdqa          xmm1, xmmword ptr [rsp + TheUMEntryPrestub_XMM_SAVE_OFFSET + 10h]
        movdqa          xmm2, xmmword ptr [rsp + TheUMEntryPrestub_XMM_SAVE_OFFSET + 20h]
        movdqa          xmm3, xmmword ptr [rsp + TheUMEntryPrestub_XMM_SAVE_OFFSET + 30h]

        ;
        ; epilogue
        ;
        add             rsp, TheUMEntryPrestub_STACK_FRAME_SIZE
        TAILJMP_RAX

NESTED_END TheUMEntryPrestub, _TEXT

        end
