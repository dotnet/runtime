; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: patchedcode.asm
;
; Notes: routinues which are patched at runtime and need to be linked in
;        their declared order.
; ***********************************************************************


include AsmMacros.inc
include asmconstants.inc

ifdef _DEBUG
extern JIT_WriteBarrier_Debug:proc
endif


; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT


; WriteBarrierManager copies the selected write barrier into this buffer and
; verifies that every implementation fits during initialization.
LEAF_ENTRY JIT_WriteBarrier, _TEXT
        align 16

ifdef _DEBUG
        ; In debug builds, this just contains jump to the debug version of the write barrier by default
        mov     rax, JIT_WriteBarrier_Debug
        jmp     rax
endif

        db (JIT_WRITE_BARRIER_BUFFER_SIZE - ($ - JIT_WriteBarrier)) dup (0CCh)
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT

        end
