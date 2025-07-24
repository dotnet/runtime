; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc
include asmconstants.inc

EXTERN RhpNewObject : PROC
EXTERN RhpNewVariableSizeObject : PROC
EXTERN RhpGcAllocMaybeFrozen : PROC
EXTERN RhExceptionHandling_FailedAllocation_Helper : PROC

EXTERN g_global_alloc_lock : DWORD
EXTERN g_global_alloc_context : QWORD

;
; Object* RhpNew(MethodTable *pMT)
;
; Allocate non-array object, slow path.
;
LEAF_ENTRY RhpNew, _TEXT

        mov         rdx, 0
        jmp         RhpNewObject

LEAF_END RhpNew, _TEXT

;
; Object* RhpNewMaybeFrozen(MethodTable *pMT)
;
; Allocate non-array object, may be on frozen heap.
;
NESTED_ENTRY RhpNewMaybeFrozen, _TEXT

        PUSH_COOP_PINVOKE_FRAME r8

        mov         rdx, 0
        call        RhpGcAllocMaybeFrozen

        POP_COOP_PINVOKE_FRAME
        ret

NESTED_END RhpNewMaybeFrozen, _TEXT

;
; Object* RhpNewArrayMaybeFrozen(MethodTable *pMT, INT_PTR size)
;
; Allocate array object, may be on frozen heap.
;
NESTED_ENTRY RhpNewArrayMaybeFrozen, _TEXT

        PUSH_COOP_PINVOKE_FRAME r8

        call        RhpGcAllocMaybeFrozen

        POP_COOP_PINVOKE_FRAME
        ret

NESTED_END RhpNewArrayMaybeFrozen, _TEXT

;
; void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
;
NESTED_ENTRY RhExceptionHandling_FailedAllocation, _TEXT

        PUSH_COOP_PINVOKE_FRAME r8

        call        RhExceptionHandling_FailedAllocation_Helper

        POP_COOP_PINVOKE_FRAME
        ret

NESTED_END RhExceptionHandling_FailedAllocation, _TEXT

;
; void RhpNewFast_UP(MethodTable *pMT)
;
; Allocate non-array object, uniprocessor version
;
LEAF_ENTRY RhpNewFast_UP, _TEXT

        inc         [g_global_alloc_lock]
        jnz         RhpNewFast_UP_RarePath

        ;;
        ;; rcx contains MethodTable pointer
        ;;
        mov         r8d, [rcx + OFFSETOF__MethodTable__m_uBaseSize]

        ;;
        ;; eax: base size
        ;; rcx: MethodTable pointer
        ;; rdx: ee_alloc_context pointer
        ;;

        mov         rax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        add         r8, rax
        cmp         r8, [g_global_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          RhpNewFast_UP_RarePath_Unlock

        ;; set the new alloc pointer
        mov         [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8

        ;; set the new object's MethodTable pointer
        mov         [rax], rcx
        mov         [g_global_alloc_lock], -1
        ret

RhpNewFast_UP_RarePath_Unlock:
        mov         [g_global_alloc_lock], -1

RhpNewFast_UP_RarePath:
        xor         edx, edx
        jmp         RhpNewObject

LEAF_END RhpNewFast_UP, _TEXT

;
; Shared code for RhNewString_UP, RhpNewArrayFast_UP and RhpNewPtrArrayFast_UP
;  RAX == string/array size
;  RCX == MethodTable
;  RDX == character/element count
;
NEW_ARRAY_FAST_UP MACRO

        inc         [g_global_alloc_lock]
        jnz         RhpNewVariableSizeObject

        mov         r8, rax
        add         rax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          NewArrayFast_RarePath

        ; rax == new alloc ptr
        ; rcx == MethodTable
        ; rdx == element count
        ; r8 == array size
        cmp         rax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          NewArrayFast_RarePath

        mov         [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], rax

        ; calc the new object pointer
        sub         rax, r8

        mov         [rax + OFFSETOF__Object__m_pEEType], rcx
        mov         [rax + OFFSETOF__Array__m_Length], edx
        mov         [g_global_alloc_lock], -1
        ret

NewArrayFast_RarePath:
        mov         [g_global_alloc_lock], -1
        jmp         RhpNewVariableSizeObject

ENDM

;
; Object* RhNewString_UP(MethodTable *pMT, DWORD stringLength)
;
; Allocate a string, uniprocessor version
;
LEAF_ENTRY RhNewString_UP, _TEXT

        ; we want to limit the element count to the non-negative 32-bit int range
        cmp         rdx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        ; Compute overall allocation size (align(base size + (element size * elements), 8)).
        lea         rax, [(rdx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 7)]
        and         rax, -8

        NEW_ARRAY_FAST_UP

StringSizeOverflow:
        ; We get here if the size of the final string object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an OOM exception that the caller of this allocator understands.

        ; rcx holds MethodTable pointer already
        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhNewString_UP, _TEXT

;
; Object* RhpNewArrayFast_UP(MethodTable *pMT, INT_PTR elementCount)
; Object* RhpNewArrayFast_UP_OBJ(MethodTable *pMT, INT_PTR elementCount)
; 
; Allocate one dimensional, zero based array (SZARRAY), uniprocessor version
;
LEAF_ENTRY RhpNewArrayFast_UP, _TEXT

        ; we want to limit the element count to the non-negative 32-bit int range
        cmp         rdx, 07fffffffh
        ja          ArraySizeOverflow

        ; save element count
        mov         r8, rdx

        ; Compute overall allocation size (align(base size + (element size * elements), 8)).
        movzx       eax, word ptr [rcx + OFFSETOF__MethodTable__m_usComponentSize]
        imul        rax, rdx
        lea         rax, [rax + SZARRAY_BASE_SIZE + 7]
        and         rax, -8

        mov         rdx, r8

        NEW_ARRAY_FAST_UP

ArraySizeOverflow:
        ; We get here if the size of the final array object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an overflow exception that the caller of this allocator understands.

        ; rcx holds MethodTable pointer already
        mov         edx, 1              ; Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhpNewArrayFast_UP, _TEXT

;
; Object* RhpNewPtrArrayFast_UP(MethodTable *pMT, INT_PTR elementCount)
; 
; Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements,
; uniprocessor version
;
LEAF_ENTRY RhpNewPtrArrayFast_UP, _TEXT

        ; Delegate overflow handling to the generic helper conservatively

        cmp         rdx, (40000000h / 8) ; sizeof(void*)
        jae         RhpNewVariableSizeObject

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        lea         eax, [edx * 8 + SZARRAY_BASE_SIZE]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        NEW_ARRAY_FAST_UP

LEAF_END RhpNewPtrArrayFast_UP, _TEXT

    end
