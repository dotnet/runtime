// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <i386/AsmMacros_Unix.inc>

.macro PUSH_COOP_PINVOKE_FRAME Target
    // push ebp-frame
    PROLOG_BEG

    // save CalleeSavedRegisters
    PROLOG_PUSH ebx
    PROLOG_PUSH esi
    PROLOG_PUSH edi

    // make space for ArgumentRegisters (8) + alignment (4)
    PROLOG_ALLOC 12

    // set frame pointer
    PROLOG_END

    lea \Target, [esp + 4]
.endm

// Inlined version of GetThreadEEAllocContext.
.macro INLINE_GET_ALLOC_CONTEXT_BASE
    push  ecx
    push  eax
    call  C_FUNC(GetThreadEEAllocContext)
    pop   eax
    pop   ecx
.endm

.macro POP_COOP_PINVOKE_FRAME
    // restore stack pointer
    EPILOG_BEG

    // skip over alignment (4) + ArgumentRegisters (8)
    EPILOG_FREE 12

    // pop CalleeSavedRegisters
    EPILOG_POP edi
    EPILOG_POP esi
    EPILOG_POP ebx

    // pop ebp-frame
    EPILOG_END
.endm
