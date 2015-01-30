;
; Copyright (c) Microsoft. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information. 
;

include <AsmMacros.inc>
include AsmConstants.inc

    extern  ExternalMethodFixupWorker:proc
    extern  ProcessCLRException:proc
    extern  VirtualMethodFixupWorker:proc

ifdef FEATURE_READYTORUN
    extern DynamicHelperWorker:proc
endif

;============================================================================================
;; EXTERN_C VOID __stdcall ExternalMethodFixupStub()

NESTED_ENTRY ExternalMethodFixupStub, _TEXT, ProcessCLRException

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, rdx

        lea             rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        sub             rdx, 5                              ; pThunk
        mov             r8, 0                               ; sectionIndex
        mov             r9, 0                               ; pModule

        call            ExternalMethodFixupWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
PATCH_LABEL ExternalMethodFixupPatchLabel
        TAILJMP_RAX

NESTED_END ExternalMethodFixupStub, _TEXT


ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 0, 10h, r8, r9

        lea     rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov     rdx, rax                            ; pIndirection

        call            ExternalMethodFixupWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        ; Share the patch label
        jmp ExternalMethodFixupPatchLabel

NESTED_END DelayLoad_MethodCall, _TEXT

;============================================================================================

DYNAMICHELPER macro frameFlags, suffix

NESTED_ENTRY DelayLoad_Helper&suffix, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 8h, 10h, r8, r9

        mov     rcx, frameFlags
        mov     [rsp], rcx
        lea     rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov     rdx, rax                            ; pIndirection

        call    DynamicHelperWorker

        test    rax,rax
        jnz     @F

        mov     rax, [rsp + __PWTB_ArgumentRegisters] ; The result is stored in the argument area of the transition block

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

@@:
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END DelayLoad_Helper&suffix, _TEXT

    endm

DYNAMICHELPER DynamicHelperFrameFlags_Default
DYNAMICHELPER DynamicHelperFrameFlags_ObjectArg, _Obj
DYNAMICHELPER <DynamicHelperFrameFlags_ObjectArg OR DynamicHelperFrameFlags_ObjectArg2>, _ObjObj

endif ; FEATURE_READYTORUN

;============================================================================================
;; EXTERN_C VOID __stdcall VirtualMethodFixupStub()

NESTED_ENTRY VirtualMethodFixupStub, _TEXT, ProcessCLRException

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, rdx

        lea             rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        sub             rdx, 5                              ; pThunk
        call            VirtualMethodFixupWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
PATCH_LABEL VirtualMethodFixupPatchLabel
        TAILJMP_RAX

NESTED_END VirtualMethodFixupStub, _TEXT

        end
