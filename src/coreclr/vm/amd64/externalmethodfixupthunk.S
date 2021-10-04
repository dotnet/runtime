// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
;
.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//============================================================================================
// EXTERN_C VOID __stdcall ExternalMethodFixupStub()

NESTED_ENTRY ExternalMethodFixupStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, rsi, 0, 0

        lea             rdi, [rsp + __PWTB_TransitionBlock] // pTransitionBlock
        sub             rsi, 5                              // pThunk
        mov             rdx, 0                              // sectionIndex
        mov             rcx, 0                              // pModule

        call            C_FUNC(ExternalMethodFixupWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
PATCH_LABEL ExternalMethodFixupPatchLabel
        TAILJMP_RAX

NESTED_END ExternalMethodFixupStub, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 0x10, rdx, rcx, 0

        lea     rdi, [rsp + __PWTB_TransitionBlock] // pTransitionBlock
        mov     rsi, rax                            // pIndirection

        call            C_FUNC(ExternalMethodFixupWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        // Share the patch label
        jmp C_FUNC(ExternalMethodFixupPatchLabel)

NESTED_END DelayLoad_MethodCall, _TEXT

//============================================================================================

.macro DYNAMICHELPER frameFlags, suffix

NESTED_ENTRY DelayLoad_Helper\suffix, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 0x10, rdx, rcx, 0

        mov     r8, \frameFlags
        lea     rdi, [rsp + __PWTB_TransitionBlock] // pTransitionBlock
        mov     rsi, rax                            // pIndirection

        call    C_FUNC(DynamicHelperWorker)

        test    rax,rax
        jnz     LOCAL_LABEL(TailCallDelayLoad_Helper\suffix)

        // The result is stored in the argument area of the transition block
        mov     rax, [rsp + __PWTB_TransitionBlock + OFFSETOF__TransitionBlock__m_argumentRegisters]

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

LOCAL_LABEL(TailCallDelayLoad_Helper\suffix):
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END DelayLoad_Helper\suffix, _TEXT

    .endm

DYNAMICHELPER DynamicHelperFrameFlags_Default
DYNAMICHELPER DynamicHelperFrameFlags_ObjectArg, _Obj
DYNAMICHELPER (DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2), _ObjObj

#endif // FEATURE_READYTORUN

