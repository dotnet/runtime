; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==

include AsmMacros.inc
include AsmConstants.inc

PInvokeStubForHostWorker        equ ?PInvokeStubForHostWorker@@YAXKPEAX0@Z
extern PInvokeStubForHostWorker:proc

extern GenericPInvokeCalliStubWorker:proc
extern VarargPInvokeStubWorker:proc

PInvokeStubForHost_CALLEE_SCRATCH_SIZE = 20h

PInvokeStubForHost_STACK_FRAME_SIZE = PInvokeStubForHost_CALLEE_SCRATCH_SIZE

; 4 FP parameter registers
PInvokeStubForHost_XMM_SAVE_OFFSET = PInvokeStubForHost_STACK_FRAME_SIZE
PInvokeStubForHost_STACK_FRAME_SIZE = PInvokeStubForHost_STACK_FRAME_SIZE + 40h

; Ensure that the new rsp will be 16-byte aligned.
if ((PInvokeStubForHost_STACK_FRAME_SIZE + 8) MOD 16) ne 0
PInvokeStubForHost_STACK_FRAME_SIZE = PInvokeStubForHost_STACK_FRAME_SIZE + 8
endif

; Return address is immediately above the local variables.
PInvokeStubForHost_RETURN_ADDRESS_OFFSET = PInvokeStubForHost_STACK_FRAME_SIZE
PInvokeStubForHost_PARAM_REGISTERS_OFFSET = PInvokeStubForHost_RETURN_ADDRESS_OFFSET + 8

NESTED_ENTRY PInvokeStubForHost, _TEXT
        alloc_stack     PInvokeStubForHost_STACK_FRAME_SIZE
        END_PROLOGUE

        ; spill args
        mov             [rsp + PInvokeStubForHost_PARAM_REGISTERS_OFFSET +  0h], rcx
        mov             [rsp + PInvokeStubForHost_PARAM_REGISTERS_OFFSET +  8h], rdx
        mov             [rsp + PInvokeStubForHost_PARAM_REGISTERS_OFFSET + 10h], r8
        mov             [rsp + PInvokeStubForHost_PARAM_REGISTERS_OFFSET + 18h], r9
        movdqa          [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET +  0h], xmm0
        movdqa          [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET + 10h], xmm1
        movdqa          [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET + 20h], xmm2
        movdqa          [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET + 30h], xmm3
        
        ; PInvokeStubForHostWorker(#stack args, stack frame, this)
        mov             r8, rcx
        mov             rcx, r11
        mov             rdx, rsp
        call            PInvokeStubForHostWorker
        
        ; unspill return value
        mov             rax,  [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET +  0h]
        movdqa          xmm0, [rsp + PInvokeStubForHost_XMM_SAVE_OFFSET + 10h]
        
        add             rsp, PInvokeStubForHost_STACK_FRAME_SIZE
        ret
NESTED_END PInvokeStubForHost, _TEXT


PInvokeStubForHostInner_STACK_FRAME_SIZE = 0

; integer registers saved in prologue
PInvokeStubForHostInner_NUM_REG_PUSHES = 2
PInvokeStubForHostInner_STACK_FRAME_SIZE = PInvokeStubForHostInner_STACK_FRAME_SIZE + PInvokeStubForHostInner_NUM_REG_PUSHES*8

; Ensure that the new rsp will be 16-byte aligned.
if ((PInvokeStubForHostInner_STACK_FRAME_SIZE + 8) MOD 16) ne 0
PInvokeStubForHostInner_STACK_FRAME_SIZE = PInvokeStubForHostInner_STACK_FRAME_SIZE + 8
endif

; Return address is immediately above the local variables.
PInvokeStubForHostInner_RETURN_ADDRESS_OFFSET = PInvokeStubForHostInner_STACK_FRAME_SIZE
PInvokeStubForHostInner_PARAM_REGISTERS_OFFSET = PInvokeStubForHostInner_RETURN_ADDRESS_OFFSET + 8

PInvokeStubForHostInner_FRAME_OFFSET = PInvokeStubForHost_CALLEE_SCRATCH_SIZE

; RCX - #stack args
; RDX - PInvokeStubForHost's stack frame
; R8  - target address
NESTED_ENTRY PInvokeStubForHostInner, _TEXT

        push_nonvol_reg rbp
        push_nonvol_reg r12
        alloc_stack     PInvokeStubForHostInner_FRAME_OFFSET + PInvokeStubForHostInner_STACK_FRAME_SIZE - PInvokeStubForHostInner_NUM_REG_PUSHES*8
        set_frame       rbp, PInvokeStubForHostInner_FRAME_OFFSET
        END_PROLOGUE

        mov             r10, r8
        mov             r12, rdx

        test            rcx, rcx
        jnz             HandleStackArgs

        ;
        ; Allocate space for scratch area if there are no stack args.
        ;
        sub             rsp, PInvokeStubForHost_CALLEE_SCRATCH_SIZE

DoneStackArgs:
        ; unspill args
        mov             rcx, [r12 + PInvokeStubForHost_PARAM_REGISTERS_OFFSET +  0h]
        mov             rdx, [r12 + PInvokeStubForHost_PARAM_REGISTERS_OFFSET +  8h]
        mov             r8,  [r12 + PInvokeStubForHost_PARAM_REGISTERS_OFFSET + 10h]
        mov             r9,  [r12 + PInvokeStubForHost_PARAM_REGISTERS_OFFSET + 18h]
        movdqa          xmm0, [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET +  0h]
        movdqa          xmm1, [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET + 10h]
        movdqa          xmm2, [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET + 20h]
        movdqa          xmm3, [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET + 30h]

        call            r10
        
        ; spill return value
        mov             [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET +  0h], rax                                                
        movdqa          [r12 + PInvokeStubForHost_XMM_SAVE_OFFSET + 10h], xmm0

        ; epilogue
        lea             rsp, [rbp + PInvokeStubForHostInner_RETURN_ADDRESS_OFFSET - PInvokeStubForHostInner_NUM_REG_PUSHES*8]
        pop             r12
        pop             rbp
        ret
        
; INPUTS: 
;       RDX - number of stack bytes
;       R12 - the outer method's frame pointer
;       RSP -
;       RBP - 
;
HandleStackArgs:
        ;
        ; Allocate space for stack parameters  + scratch area.
        ;
        sub             rsp, rcx
        and             rsp, -16
        sub             rsp, PInvokeStubForHost_CALLEE_SCRATCH_SIZE

        ;
        ; Copy stack parameters
        ;
        shr             rcx, 3          ; setup count
       
        mov             r8, rdi
        mov             r9, rsi
       
        lea             rdi, [rsp + PInvokeStubForHost_CALLEE_SCRATCH_SIZE]      ; rdi -> above callee scratch area
        lea             rsi, [r12 + PInvokeStubForHost_PARAM_REGISTERS_OFFSET + PInvokeStubForHost_CALLEE_SCRATCH_SIZE]
        rep             movsq
        
        mov             rsi, r9             ; restore rsi
        mov             rdi, r8             ; restore rdi
        jmp             DoneStackArgs
NESTED_END PInvokeStubForHostInner, _TEXT


;
; in:
; PINVOKE_CALLI_TARGET_REGISTER (r10) = unmanaged target
; PINVOKE_CALLI_SIGTOKEN_REGNUM (r11) = sig token       
;
; out:
; METHODDESC_REGISTER           (r10) = unmanaged target
;
LEAF_ENTRY GenericPInvokeCalliHelper, _TEXT
        
        ;
        ; check for existing IL stub
        ;
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              GenericPInvokeCalliGenILStub

        ;
        ; We need to distinguish between a MethodDesc* and an unmanaged target in PInvokeStubForHost().  
        ; The way we do this is to shift the managed target to the left by one bit and then set the 
        ; least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
        ;
        shl             PINVOKE_CALLI_TARGET_REGISTER, 1
        or              PINVOKE_CALLI_TARGET_REGISTER, 1

        ;
        ; jump to existing IL stub
        ;
        jmp             rax

LEAF_END GenericPInvokeCalliHelper, _TEXT

NESTED_ENTRY GenericPInvokeCalliGenILStub, _TEXT
        
        PROLOG_WITH_TRANSITION_BLOCK

        ;
        ; save target
        ;
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        ;
        ; GenericPInvokeCalliStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget)
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock]     ; pTransitionBlock*
        mov             rdx, PINVOKE_CALLI_SIGTOKEN_REGISTER    ; pVASigCookie
        mov             r8, METHODDESC_REGISTER                 ; pUnmanagedTarget
        call            GenericPInvokeCalliStubWorker

        ;
        ; restore target
        ;
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             GenericPInvokeCalliHelper

NESTED_END GenericPInvokeCalliGenILStub, _TEXT

LEAF_ENTRY VarargPInvokeStub, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rcx
        jmp             VarargPInvokeStubHelper
LEAF_END VarargPInvokeStub, _TEXT
        
LEAF_ENTRY VarargPInvokeStub_RetBuffArg, _TEXT
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, rdx
        jmp             VarargPInvokeStubHelper
LEAF_END VarargPInvokeStub_RetBuffArg, _TEXT

LEAF_ENTRY VarargPInvokeStubHelper, _TEXT
        ;
        ; check for existing IL stub
        ;
        mov             rax, [PINVOKE_CALLI_SIGTOKEN_REGISTER + OFFSETOF__VASigCookie__pNDirectILStub]
        test            rax, rax
        jz              VarargPInvokeGenILStub

        ;
        ; jump to existing IL stub
        ;
        jmp             rax
        
LEAF_END VarargPInvokeStubHelper, _TEXT

;
; IN: METHODDESC_REGISTER (R10) stub secret param
;     PINVOKE_CALLI_SIGTOKEN_REGISTER (R11) VASigCookie*
;
; ASSUMES: we already checked for an existing stub to use
;
NESTED_ENTRY VarargPInvokeGenILStub, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 

        ;
        ; save target
        ;
        mov             r12, METHODDESC_REGISTER
        mov             r13, PINVOKE_CALLI_SIGTOKEN_REGISTER

        ;
        ; VarargPInvokeStubWorker(TransitionBlock * pTransitionBlock, VASigCookie *pVASigCookie, MethodDesc *pMD)
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock]     ; pTransitionBlock*
        mov             rdx, PINVOKE_CALLI_SIGTOKEN_REGISTER    ; pVASigCookie
        mov             r8, METHODDESC_REGISTER                 ; pMD
        call            VarargPInvokeStubWorker

        ;
        ; restore target
        ;
        mov             METHODDESC_REGISTER, r12
        mov             PINVOKE_CALLI_SIGTOKEN_REGISTER, r13

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        jmp             VarargPInvokeStubHelper

NESTED_END VarargPInvokeGenILStub, _TEXT

        end
