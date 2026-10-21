// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

// Successful notification helpers enter the epilogue with:
//   RAX = object
//   R9  = aligned object size
//   R10 = allocation context
//   R11 = new allocation pointer
NESTED_ENTRY RhpAllocationCompleteEpilogue, _TEXT, NoHandler
        alloc_stack 0x18
        END_PROLOGUE

        mov         [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r11
        mov         [rsp + 0x10], rax

        mov         rdi, rax
        mov         rsi, r9
        call        C_FUNC(RhpAllocationComplete)

        mov         rax, [rsp + 0x10]

        free_stack 0x18
        ret
NESTED_END RhpAllocationCompleteEpilogue, _TEXT

// Allocate a non-array, non-finalizable object and notify on fast success.
// RDI = MethodTable.
LEAF_ENTRY RhpNewFast_Notify, _TEXT
        push_nonvol_reg rbx
        mov         rbx, rdi

        // RAX = ee_alloc_context; trashes volatile registers.
        INLINE_GET_ALLOC_CONTEXT_BASE

        mov         r10, rax
        mov         r9d, [rbx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         rax, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         r11, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         r11, rax
        cmp         r9, r11
        ja          LOCAL_LABEL(RhpNewFast_Notify_RarePath)

        lea         r11, [rax + r9]
        mov         [rax + OFFSETOF__Object__m_pEEType], rbx

        .cfi_remember_state
        pop_nonvol_reg rbx
        jmp         C_FUNC(RhpAllocationCompleteEpilogue)

        .cfi_restore_state
        .cfi_def_cfa_offset 16
LOCAL_LABEL(RhpNewFast_Notify_RarePath):
        mov         rdi, rbx
        xor         esi, esi
        pop_nonvol_reg rbx
        jmp         C_FUNC(RhpNewObject)
LEAF_END RhpNewFast_Notify, _TEXT

// Shared fast path for strings and SZ arrays.
// RAX = aligned size, RDI = MethodTable, RSI = component count.
.macro NEW_ARRAY_FAST_NOTIFY
        push_nonvol_reg rbx
        push_nonvol_reg r12
        push_nonvol_reg r13

        mov         rbx, rdi
        mov         r12, rsi
        mov         r13, rax

        // RAX = ee_alloc_context; trashes volatile registers.
        INLINE_GET_ALLOC_CONTEXT_BASE

        mov         r10, rax
        mov         rax, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         rdi, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         rdi, rax
        cmp         r13, rdi
        ja          1f

        mov         r9, r13
        lea         r11, [rax + r9]
        mov         [rax + OFFSETOF__Object__m_pEEType], rbx
        mov         [rax + OFFSETOF__Array__m_Length], r12d

        .cfi_remember_state
        pop_nonvol_reg r13
        pop_nonvol_reg r12
        pop_nonvol_reg rbx
        jmp         C_FUNC(RhpAllocationCompleteEpilogue)

        .cfi_restore_state
        .cfi_def_cfa_offset 32
1:
        mov         rdi, rbx
        mov         rsi, r12
        pop_nonvol_reg r13
        pop_nonvol_reg r12
        pop_nonvol_reg rbx
        jmp         C_FUNC(RhpNewVariableSizeObject)
.endm

// RDI = string MethodTable, RSI = string length.
LEAF_ENTRY RhNewString_Notify, _TEXT
        cmp         rsi, MAX_STRING_LENGTH
        ja          LOCAL_LABEL(RhNewString_Notify_Overflow)

        lea         rax, [rsi * STRING_COMPONENT_SIZE + STRING_BASE_SIZE + 7]
        and         rax, -8
        NEW_ARRAY_FAST_NOTIFY

LOCAL_LABEL(RhNewString_Notify_Overflow):
        xor         esi, esi
        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)
LEAF_END RhNewString_Notify, _TEXT

// RDI = array MethodTable, RSI = element count.
LEAF_ENTRY RhpNewArrayFast_Notify, _TEXT
        cmp         rsi, 0x07fffffff
        ja          LOCAL_LABEL(RhpNewArrayFast_Notify_Overflow)

        movzx       eax, word ptr [rdi + OFFSETOF__MethodTable__m_usComponentSize]
        imul        rax, rsi
        lea         rax, [rax + SZARRAY_BASE_SIZE + 7]
        and         rax, -8
        NEW_ARRAY_FAST_NOTIFY

LOCAL_LABEL(RhpNewArrayFast_Notify_Overflow):
        mov         esi, 1
        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)
LEAF_END RhpNewArrayFast_Notify, _TEXT

// RDI = pointer-array MethodTable, RSI = element count.
LEAF_ENTRY RhpNewPtrArrayFast_Notify, _TEXT
        cmp         rsi, 0x8000000
        jae         C_FUNC(RhpNewArrayFast_Notify)

        lea         eax, [rsi * 8 + SZARRAY_BASE_SIZE]
        NEW_ARRAY_FAST_NOTIFY
LEAF_END RhpNewPtrArrayFast_Notify, _TEXT
