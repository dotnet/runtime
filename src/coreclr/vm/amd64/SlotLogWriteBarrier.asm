; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN g_SlotLogWriteBarrierSlowPath:QWORD

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN g_write_watch_table:QWORD
endif

NESTED_ENTRY JIT_WriteBarrier_SlotLog_Slow, _TEXT
        alloc_stack 38h
        END_PROLOGUE

        mov     [rsp + 30h], rcx
        mov     [rsp + 28h], rdx
        mov     r8, rdx
        mov     rdx, r10
        call    qword ptr [g_SlotLogWriteBarrierSlowPath]
        mov     rcx, [rsp + 30h]
        mov     rdx, [rsp + 28h]
        mov     [rcx], rdx
        add     rsp, 38h
        ret
NESTED_END_MARKED JIT_WriteBarrier_SlotLog_Slow, _TEXT

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

NESTED_ENTRY JIT_WriteBarrier_WriteWatch_SlotLog_Slow, _TEXT
        alloc_stack 38h
        END_PROLOGUE

        mov     [rsp + 30h], rcx
        mov     [rsp + 28h], rdx
        mov     r8, rdx
        mov     rdx, r10
        call    qword ptr [g_SlotLogWriteBarrierSlowPath]
        mov     rcx, [rsp + 30h]
        mov     rdx, [rsp + 28h]
        mov     [rcx], rdx

        mov     r10, rcx
        shr     r10, 0Ch
        add     r10, qword ptr [g_write_watch_table]
        cmp     byte ptr [r10], 0
        jne     Done
        mov     byte ptr [r10], 0FFh
    Done:
        add     rsp, 38h
        ret
NESTED_END_MARKED JIT_WriteBarrier_WriteWatch_SlotLog_Slow, _TEXT

endif

        end
