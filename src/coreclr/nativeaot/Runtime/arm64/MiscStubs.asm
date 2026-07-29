;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

; void* PacStripPtr(void *);
; This function strips the pointer of PAC info that is passed as an argument.
; We prefer to strip a pointer where it's not going to be used to branch execution to.
; It is a no-op on non-PAC enabled machines.
    NESTED_ENTRY PacStripPtr
        PROLOG_SAVE_REG_PAIR fp, lr, #-16!
        mov lr, x0
        DCD     0xD50320FF  ; xpaclri instruction in binary to avoid requiring PAC-enabled assemblers
        mov x0, lr
        EPILOG_RESTORE_REG_PAIR fp, lr, #16!
        EPILOG_RETURN
    NESTED_END

; void* PacSignPtr(void *, void *);
; This function signs the input pointer using x1 as salt. It is a no-op on non-PAC enabled machines.
    LEAF_ENTRY PacSignPtr
        mov x17, x0
        mov x16, x1
        DCD 0xD503215F  ; pacib1716 instruction in binary to avoid error while compiling with non-PAC enabled compilers
        mov x0, x17
        ret
    LEAF_END PacSignPtr

; void* PacAuthPtr(void *, void *);
; This function authenticates the input signed-pointer using x1 as salt. It is a no-op on non-PAC enabled machines.
    LEAF_ENTRY PacAuthPtr
        mov x17, x0
        mov x16, x1
        DCD 0xD50321DF  ; autib1716 instruction in binary to avoid error while compiling with non-PAC enabled compilers
        mov x0, x17
        ret
    LEAF_END PacAuthPtr

    end
