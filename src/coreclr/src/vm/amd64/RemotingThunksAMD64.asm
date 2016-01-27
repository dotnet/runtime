; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

include AsmMacros.inc
include AsmConstants.inc
ifdef FEATURE_REMOTING

extern CallDescrWorkerUnwindFrameChainHandler:proc

extern TransparentProxyStubWorker:proc	

; Stack frame layout:
;
; (stack parameters)
; ...
; r9
; r8
; rdx
; rcx <- TPSCC_PARAMS_OFFSET
; return address <- TPSCC_STACK_FRAME_SIZE
; r10 <- TPSCC_R10_OFFSET
; xmm3
; xmm2
; xmm1
; xmm0 <- TPSCC_XMM_SAVE_OFFSET
; callee's r9
; callee's r8
; callee's rdx
; callee's rcx

TPSCC_XMM_SAVE_OFFSET = 20h
TPSCC_R10_OFFSET = 60h
TPSCC_STACK_FRAME_SIZE = 68h
TPSCC_PARAMS_OFFSET = 70h

TRANSPARENT_PROXY_STUB_PROLOGUE macro
        alloc_stack     TPSCC_STACK_FRAME_SIZE

        save_reg_postrsp r10, TPSCC_R10_OFFSET

        SAVE_ARGUMENT_REGISTERS TPSCC_PARAMS_OFFSET
        SAVE_FLOAT_ARGUMENT_REGISTERS TPSCC_XMM_SAVE_OFFSET

        END_PROLOGUE

        endm

NESTED_ENTRY TransparentProxyStub, _TEXT, CallDescrWorkerUnwindFrameChainHandler

        TRANSPARENT_PROXY_STUB_PROLOGUE
        
        ;; rcx: this
        ;; [rsp]: slot number

        mov             rax, [rcx + TransparentProxyObject___stub]
        mov             rcx, [rcx + TransparentProxyObject___stubData]
        call            rax

        RESTORE_ARGUMENT_REGISTERS TPSCC_PARAMS_OFFSET
        RESTORE_FLOAT_ARGUMENT_REGISTERS TPSCC_XMM_SAVE_OFFSET

        mov             r10, [rsp + TPSCC_R10_OFFSET] 

        test            rax, rax
        jnz             CrossContext

        mov             r11, [rcx + TransparentProxyObject___pMT]

        ; Convert the slot number (r10) into the code address (in rax)
        ; See MethodTable.h for details on vtable layout
        shr             r10, MethodTable_VtableSlotsPerChunkLog2
        mov             rax, [r11 + r10*8 + METHODTABLE_OFFSET_VTABLE]

        mov             r10, [rsp + TPSCC_R10_OFFSET] ; Reload the slot
        and             r10, MethodTable_VtableSlotsPerChunk-1
        mov             rax, [rax + r10*8]
        
        add             rsp, TPSCC_STACK_FRAME_SIZE
        TAILJMP_RAX

CrossContext:        
        add             rsp, TPSCC_STACK_FRAME_SIZE
        jmp             TransparentProxyStub_CrossContext

NESTED_END TransparentProxyStub, _TEXT


NESTED_ENTRY TransparentProxyStub_CrossContext, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 8

        ;
        ; Call TransparentProxyStubWorker.
        ;
        lea             rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov             rdx, r10                            ; MethodDesc *
        call            TransparentProxyStubWorker

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

NESTED_END TransparentProxyStub_CrossContext, _TEXT

LEAF_ENTRY TransparentProxyStubPatch, _TEXT
        ; make sure that the basic block is unique
        test            eax,12
PATCH_LABEL TransparentProxyStubPatchLabel
        ret
LEAF_END TransparentProxyStubPatch, _TEXT

;+----------------------------------------------------------------------------
;
;  Method:     CRemotingServices::CallFieldGetter   private
;
;  Synopsis:   Calls the field getter function (Object::__FieldGetter) in
;              managed code by setting up the stack and calling the target
;
;+----------------------------------------------------------------------------
; extern "C"
;void __stdcall CRemotingServices__CallFieldGetter(  MethodDesc *pMD,
;                                                    LPVOID pThis,
;                                                    LPVOID pFirst,
;                                                    LPVOID pSecond,
;                                                    LPVOID pThird
;                                                    )
LEAF_ENTRY CRemotingServices__CallFieldGetter, _TEXT

; +28   pThird
; +20   scratch area
; +18   scratch area
; +10   scratch area
; + 8   scratch area
; rsp   return address

        mov             METHODDESC_REGISTER, rcx
        mov             rcx, rdx
        mov             rdx, r8
        mov             r8, r9
        mov             r9, [rsp + 28h]
        jmp             TransparentProxyStub

LEAF_END CRemotingServices__CallFieldGetter, _TEXT


;+----------------------------------------------------------------------------
;
;  Method:     CRemotingServices::CallFieldSetter   private
;
;  Synopsis:   Calls the field setter function (Object::__FieldSetter) in
;              managed code by setting up the stack and calling the target
;
;+----------------------------------------------------------------------------
; extern "C"
;void __stdcall CRemotingServices__CallFieldSetter(  MethodDesc *pMD,
;                                                    LPVOID pThis,
;                                                    LPVOID pFirst,
;                                                    LPVOID pSecond,
;                                                    LPVOID pThird
;                                                    )
LEAF_ENTRY CRemotingServices__CallFieldSetter, _TEXT

; +28   pThird
; +20   scratch area
; +18   scratch area
; +10   scratch area
; + 8   scratch area
; rsp   return address

        mov             METHODDESC_REGISTER, rcx
        mov             rcx, rdx
        mov             rdx, r8
        mov             r8, r9
        mov             r9, [rsp + 28h]
        jmp             TransparentProxyStub

LEAF_END CRemotingServices__CallFieldSetter, _TEXT


;; extern "C" ARG_SLOT __stdcall CTPMethodTable__CallTargetHelper2(const void *pTarget,
;;                                                                 LPVOID pvFirst,
;;                                                                 LPVOID pvSecond);
NESTED_ENTRY CTPMethodTable__CallTargetHelper2, _TEXT, CallDescrWorkerUnwindFrameChainHandler
        alloc_stack     28h     ;; alloc callee scratch and align the stack
        END_PROLOGUE

        mov     rax, rcx    ; rax <- call target
        mov     rcx, rdx    ; rcx <- first arg
        mov     rdx, r8     ; rdx <- second arg

        call     rax
	;; It is important to have an instruction between the previous call and the epilog.
	;; If the return address is in epilog, OS won't call personality routine because
	;; it thinks personality routine does not help in this case.
	nop

        ; epilog
        add     rsp, 28h
	ret
NESTED_END CTPMethodTable__CallTargetHelper2, _TEXT

;; extern "C" ARG_SLOT __stdcall CTPMethodTable__CallTargetHelper2(const void *pTarget,
;;                                                                 LPVOID pvFirst,
;;                                                                 LPVOID pvSecond,
;;                                                                 LPVOID pvThird);
NESTED_ENTRY CTPMethodTable__CallTargetHelper3, _TEXT, CallDescrWorkerUnwindFrameChainHandler
        alloc_stack     28h     ;; alloc callee scratch and align the stack
        END_PROLOGUE

        mov     rax, rcx    ; rax <- call target
        mov     rcx, rdx    ; rcx <- first arg
        mov     rdx, r8     ; rdx <- second arg
        mov     r8,  r9     ; r8  <- third arg

        call     rax

	;; It is important to have an instruction between the previous call and the epilog.
	;; If the return address is in epilog, OS won't call personality routine because
	;; it thinks personality routine does not help in this case.
	nop

        ; epilog
        add     rsp, 28h
	ret
NESTED_END CTPMethodTable__CallTargetHelper3, _TEXT

NESTED_ENTRY CRemotingServices__DispatchInterfaceCall, _TEXT, CallDescrWorkerUnwindFrameChainHandler

        TRANSPARENT_PROXY_STUB_PROLOGUE

        ;
        ; 'this' is a TransparentProxy.  Call to stub to see if need to cross contexts.
        ;

        mov             rax, [rcx + TransparentProxyObject___stub]
        mov             rcx, [rcx + TransparentProxyObject___stubData]
        call            rax

        test            rax, rax
        jnz             CrossContext

extern VSD_GetTargetForTPWorkerQuick:proc
        mov             rcx, [rsp + TPSCC_PARAMS_OFFSET]                ; rcx <- this
        mov             rdx, [rsp + TPSCC_R10_OFFSET]                   ; rdx <- Get the MethodDesc* or slot number        
        call            VSD_GetTargetForTPWorkerQuick

        RESTORE_ARGUMENT_REGISTERS TPSCC_PARAMS_OFFSET
        RESTORE_FLOAT_ARGUMENT_REGISTERS TPSCC_XMM_SAVE_OFFSET

        mov             r10, [rsp + TPSCC_R10_OFFSET] 

        test            rax, rax                                         ; Did we find a target?
        jz              SlowDispatch

        add             rsp, TPSCC_STACK_FRAME_SIZE
        TAILJMP_RAX

SlowDispatch:
        add             rsp, TPSCC_STACK_FRAME_SIZE
        jmp             InContextTPDispatchAsmStub

CrossContext:        
        RESTORE_ARGUMENT_REGISTERS TPSCC_PARAMS_OFFSET
        RESTORE_FLOAT_ARGUMENT_REGISTERS TPSCC_XMM_SAVE_OFFSET

        mov             r10, [rsp + TPSCC_R10_OFFSET] 

        add             rsp, TPSCC_STACK_FRAME_SIZE
        jmp             TransparentProxyStub_CrossContext

NESTED_END CRemotingServices__DispatchInterfaceCall, _TEXT

NESTED_ENTRY InContextTPDispatchAsmStub, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK

extern VSD_GetTargetForTPWorker:proc
        lea             rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov             rdx, r10                            ; token
        call            VSD_GetTargetForTPWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END InContextTPDispatchAsmStub, _TEXT

endif ; FEATURE_REMOTING

        end
 
