; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

    extern  ExternalMethodFixupWorker:proc
    extern  ProcessCLRException:proc

ifdef FEATURE_READYTORUN
    extern DynamicHelperWorker:proc
endif

ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 0, 10h, r8, r9

        lea     rcx, [rsp + __PWTB_TransitionBlock] ; pTransitionBlock
        mov     rdx, rax                            ; pIndirection

        call            ExternalMethodFixupWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

PATCH_LABEL ExternalMethodFixupPatchLabel
        TAILJMP_RAX

NESTED_END DelayLoad_MethodCall, _TEXT

;============================================================================================

DYNAMICHELPER macro frameFlags, suffix

NESTED_ENTRY DelayLoad_Helper&suffix, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 8h, 10h, r8, r9

        mov     qword ptr [rsp + SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES], frameFlags
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

        end
