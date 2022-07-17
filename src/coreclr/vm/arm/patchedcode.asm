; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"


    TEXTAREA


; ------------------------------------------------------------------
; Start of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeStart
        bx      lr
    LEAF_END

; ------------------------------------------------------------------
; GC write barrier support.
;
; GC Write barriers are defined in asmhelpers.asm. The following functions are used to define
; patchable location where the write-barriers are copied over at runtime

    LEAF_ENTRY JIT_PatchedWriteBarrierStart
        ; Cannot be empty function to prevent LNK1223
        bx lr
    LEAF_END

    ; These write barriers are overwritten on the fly
    ; See ValidateWriteBarriers on how the sizes of these should be calculated
        ALIGN 4
    LEAF_ENTRY JIT_WriteBarrier
    SPACE (0x84)
    LEAF_END_MARKED JIT_WriteBarrier

        ALIGN 4
    LEAF_ENTRY JIT_CheckedWriteBarrier
    SPACE (0x9C)
    LEAF_END_MARKED JIT_CheckedWriteBarrier

        ALIGN 4
    LEAF_ENTRY JIT_ByRefWriteBarrier
    SPACE (0xA0)
    LEAF_END_MARKED JIT_ByRefWriteBarrier

    LEAF_ENTRY JIT_PatchedWriteBarrierLast
        ; Cannot be empty function to prevent LNK1223
        bx lr
    LEAF_END

; ------------------------------------------------------------------
; End of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeLast
        bx      lr
    LEAF_END


; Must be at very end of file
        END
