//
// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Geoff Norton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
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


//============================================================================================
// EXTERN_C VOID __stdcall VirtualMethodFixupStub()

NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, rsi, 0, 0

        lea             rdi, [rsp + __PWTB_TransitionBlock] // pTransitionBlock
        sub             rsi, 5                              // pThunk
        call            C_FUNC(VirtualMethodFixupWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
PATCH_LABEL VirtualMethodFixupPatchLabel
        TAILJMP_RAX

NESTED_END VirtualMethodFixupStub, _TEXT
