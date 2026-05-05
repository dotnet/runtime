;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

; void* PacStripPtr(void *);
; This function strips the pointer of PAC info that is passed as an argument.
    LEAF_ENTRY PacStripPtr
        DCD     0xDAC143E0  ; xpaci x0 instruction in binary to avoid requiring PAC-enabled assemblers
        ret
    LEAF_END PacStripPtr

; void* PacSignPtr(void *, void *);
; This function sign the input pointer using zero as salt.
; Thus we need to move input in lr, sign it and then copy it back to the result register.
    LEAF_ENTRY PacSignPtr
        mov x17, x0
        mov x16, x1
        DCD 0xD503211F  ; pacia1716 instruction in binary to avoid error while compiling with non-PAC enabled compilers
        mov x0, x17
        ret
    LEAF_END PacSignPtr

    end
