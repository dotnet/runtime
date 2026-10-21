// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

NESTED_ENTRY JIT_WriteBarrier_SlotLog_Slow, _TEXT, NoHandler
        alloc_stack 0x18
        END_PROLOGUE

        mov     [rsp + 0x10], rdi
        mov     [rsp + 0x08], rsi
        mov     rdx, rsi
        mov     rsi, r10
        PREPARE_EXTERNAL_VAR g_SlotLogWriteBarrierSlowPath, rax
        call    qword ptr [rax]
        mov     rdi, [rsp + 0x10]
        mov     rsi, [rsp + 0x08]
        mov     [rdi], rsi
        free_stack 0x18
        ret
ALTERNATE_ENTRY JIT_WriteBarrier_SlotLog_Slow_End
NESTED_END JIT_WriteBarrier_SlotLog_Slow, _TEXT

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

NESTED_ENTRY JIT_WriteBarrier_WriteWatch_SlotLog_Slow, _TEXT, NoHandler
        alloc_stack 0x18
        END_PROLOGUE

        mov     [rsp + 0x10], rdi
        mov     [rsp + 0x08], rsi
        mov     rdx, rsi
        mov     rsi, r10
        PREPARE_EXTERNAL_VAR g_SlotLogWriteBarrierSlowPath, rax
        call    qword ptr [rax]
        mov     rdi, [rsp + 0x10]
        mov     rsi, [rsp + 0x08]
        mov     [rdi], rsi

        mov     r10, rdi
        shr     r10, 0xC
        PREPARE_EXTERNAL_VAR g_write_watch_table, r11
        add     r10, qword ptr [r11]
        cmp     byte ptr [r10], 0
        jne     LOCAL_LABEL(Done_WriteWatch_SlotLog_Slow)
        mov     byte ptr [r10], 0xFF
    LOCAL_LABEL(Done_WriteWatch_SlotLog_Slow):
        free_stack 0x18
        ret
ALTERNATE_ENTRY JIT_WriteBarrier_WriteWatch_SlotLog_Slow_End
NESTED_END JIT_WriteBarrier_WriteWatch_SlotLog_Slow, _TEXT

#endif
