; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc
include AsmConstants.inc


LEAF_ENTRY GetCurrentSP, _TEXT

        mov rax, rsp
        add rax, 8
        ret

LEAF_END GetCurrentSP, _TEXT


LEAF_ENTRY GetCurrentIP, _TEXT

        mov rax, [rsp]
        ret

LEAF_END GetCurrentIP, _TEXT


LEAF_ENTRY GetRBP, _TEXT

        mov rax, rbp
        ret

LEAF_END GetRBP, _TEXT

;// this is the same implementation as the function of the same name in di\amd64\floatconversion.asm and they must
;// remain in sync.

;// @dbgtodo inspection: remove this function when we remove the ipc event to load the float state

; extern "C" double FPFillR8(void* fpContextSlot);
LEAF_ENTRY FPFillR8, _TEXT
    movdqa  xmm0, [rcx]
    ret
LEAF_END FPFillR8, _TEXT


LEAF_ENTRY get_cycle_count, _TEXT

        rdtsc                           ; time stamp count ret'd in edx:eax
        shl     rdx, 32
        mov     edx, eax
        mov     rax, rdx                ; return tsc in rax
        ret
LEAF_END get_cycle_count, _TEXT

        end
