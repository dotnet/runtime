;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc

EXTERN RhpNewObject : PROC
EXTERN RhpNewVariableSizeObject : PROC
EXTERN RhpAllocationComplete : PROC

;;
;; Successful notification helpers enter the epilogue with:
;;   RAX = object
;;   R9  = aligned object size
;;   R10 = allocation context
;;   R11 = new allocation pointer
;;
NESTED_ENTRY RhpAllocationCompleteEpilogue, _TEXT
        alloc_stack 28h
        END_PROLOGUE

        mov         [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r11
        mov         [rsp + 20h], rax

        mov         rcx, rax
        mov         rdx, r9
        call        RhpAllocationComplete

        mov         rax, [rsp + 20h]

        add         rsp, 28h
        ret
NESTED_END RhpAllocationCompleteEpilogue, _TEXT

;; Allocate a non-array, non-finalizable object and notify on fast success.
;; RCX = MethodTable
LEAF_ENTRY RhpNewFast_Notify, _TEXT
        ;; RDX = ee_alloc_context; trashes RAX
        INLINE_GET_ALLOC_CONTEXT_BASE rdx, rax

        mov         r9d, [rcx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         rax, [rdx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         r11, [rdx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         r11, rax
        cmp         r9, r11
        ja          RhpNewFast_Notify_RarePath

        lea         r11, [rax + r9]
        mov         [rax + OFFSETOF__Object__m_pEEType], rcx
        mov         r10, rdx
        jmp         RhpAllocationCompleteEpilogue

RhpNewFast_Notify_RarePath:
        xor         edx, edx
        jmp         RhpNewObject
LEAF_END RhpNewFast_Notify, _TEXT

;; Shared fast path for strings and SZ arrays.
;; RAX = aligned size, RCX = MethodTable, RDX = component count.
NEW_ARRAY_FAST_NOTIFY MACRO
        ;; R10 = ee_alloc_context; trashes R8
        INLINE_GET_ALLOC_CONTEXT_BASE r10, r8

        mov         r8, rax
        mov         rax, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         r11, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         r11, rax
        cmp         r8, r11
        ja          RhpNewVariableSizeObject

        lea         r11, [rax + r8]
        mov         [rax + OFFSETOF__Object__m_pEEType], rcx
        mov         [rax + OFFSETOF__Array__m_Length], edx
        mov         r9, r8
        jmp         RhpAllocationCompleteEpilogue
ENDM

;; RCX = string MethodTable, RDX = string length.
LEAF_ENTRY RhNewString_Notify, _TEXT
        cmp         rdx, MAX_STRING_LENGTH
        ja          RhNewString_Notify_Overflow

        lea         rax, [(rdx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 7)]
        and         rax, -8
        NEW_ARRAY_FAST_NOTIFY

RhNewString_Notify_Overflow:
        xor         edx, edx
        jmp         RhExceptionHandling_FailedAllocation
LEAF_END RhNewString_Notify, _TEXT

;; RCX = array MethodTable, RDX = element count.
LEAF_ENTRY RhpNewArrayFast_Notify, _TEXT
        cmp         rdx, 07fffffffh
        ja          RhpNewArrayFast_Notify_Overflow

        movzx       eax, word ptr [rcx + OFFSETOF__MethodTable__m_usComponentSize]
        imul        rax, rdx
        lea         rax, [rax + SZARRAY_BASE_SIZE + 7]
        and         rax, -8
        NEW_ARRAY_FAST_NOTIFY

RhpNewArrayFast_Notify_Overflow:
        mov         edx, 1
        jmp         RhExceptionHandling_FailedAllocation
LEAF_END RhpNewArrayFast_Notify, _TEXT

;; RCX = pointer-array MethodTable, RDX = element count.
LEAF_ENTRY RhpNewPtrArrayFast_Notify, _TEXT
        cmp         rdx, (40000000h / 8)
        jae         RhpNewArrayFast_Notify

        lea         eax, [edx * 8 + SZARRAY_BASE_SIZE]
        NEW_ARRAY_FAST_NOTIFY
LEAF_END RhpNewPtrArrayFast_Notify, _TEXT

        END
