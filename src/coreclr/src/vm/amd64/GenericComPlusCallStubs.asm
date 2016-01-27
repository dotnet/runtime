; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==

ifdef FEATURE_COMINTEROP

include AsmMacros.inc
include asmconstants.inc

CTPMethodTable__s_pThunkTable equ ?s_pThunkTable@CTPMethodTable@@0PEAVMethodTable@@EA
InstantiatedMethodDesc__IMD_GetComPlusCallInfo equ ?IMD_GetComPlusCallInfo@InstantiatedMethodDesc@@QEAAPEAUComPlusCallInfo@@XZ

ifdef FEATURE_REMOTING
extern CRemotingServices__DispatchInterfaceCall:proc
extern CTPMethodTable__s_pThunkTable:qword
extern InstantiatedMethodDesc__IMD_GetComPlusCallInfo:proc
endif

extern CLRToCOMWorker:proc
extern ProcessCLRException:proc

ifdef FEATURE_REMOTING
;
; in:
;       r10: MethodDesc*
;       rcx: 'this' object
;
; out:
; METHODDESC_REGISTER (r10) = MethodDesc* (for IL stubs)
;
LEAF_ENTRY GenericComPlusCallStub, _TEXT

        ;
        ; check for a null 'this' pointer and
        ; then see if this is a TransparentProxy
        ;

        test            rcx, rcx
        jz              do_com_call

        mov             rax, [CTPMethodTable__s_pThunkTable]
        cmp             [rcx], rax
        jne             do_com_call

        ;
        ; 'this' is a TransparentProxy
        ;
        jmp             CRemotingServices__DispatchInterfaceCall

do_com_call:

        ;
        ; Check if the call is being made on an InstantiatedMethodDesc.
        ;

        mov             ax, [r10 + OFFSETOF__MethodDesc__m_wFlags]
        and             ax, MethodDescClassification__mdcClassification
        cmp             ax, MethodDescClassification__mcInstantiated
        je              GenericComPlusCallWorkerInstantiated

        ;
        ; Check if there is an IL stub.
        ;

        mov             rax, [r10 + OFFSETOF__ComPlusCallMethodDesc__m_pComPlusCallInfo]
        mov             rax, [rax + OFFSETOF__ComPlusCallInfo__m_pILStub]
        test            rax, rax
        jz              GenericComPlusCallStubSlow

        TAILJMP_RAX
      
LEAF_END GenericComPlusCallStub, _TEXT

; We could inline IMD_GetComPlusCallInfo here but it would be ugly.
NESTED_ENTRY GenericComPlusCallWorkerInstantiated, _TEXT, ProcessCLRException
        alloc_stack     68h

        save_reg_postrsp r10, 60h

        SAVE_ARGUMENT_REGISTERS 70h

        SAVE_FLOAT_ARGUMENT_REGISTERS 20h

        END_PROLOGUE
       
        mov             rcx, r10
        call            InstantiatedMethodDesc__IMD_GetComPlusCallInfo

        RESTORE_FLOAT_ARGUMENT_REGISTERS 20h

        RESTORE_ARGUMENT_REGISTERS 70h

        mov             r10, [rsp + 60h]

        mov             rax, [rax + OFFSETOF__ComPlusCallInfo__m_pILStub]

        add             rsp, 68h
        TAILJMP_RAX
NESTED_END GenericComPlusCallWorkerInstantiated, _TEXT
endif


ifdef FEATURE_REMOTING
NESTED_ENTRY GenericComPlusCallStubSlow, _TEXT, ProcessCLRException
else
NESTED_ENTRY GenericComPlusCallStub, _TEXT, ProcessCLRException
endif

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

ifdef FEATURE_REMOTING
NESTED_END GenericComPlusCallStubSlow, _TEXT
else
NESTED_END GenericComPlusCallStub, _TEXT
endif

endif ; FEATURE_COMINTEROP

        end
