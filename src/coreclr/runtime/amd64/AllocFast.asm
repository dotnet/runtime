;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc


;; Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
;; allocation context then automatically fallback to the slow allocation path.
;;  RCX == MethodTable
LEAF_ENTRY RhpNewFast, _TEXT

        ;; rdx = ee_alloc_context pointer, TRASHES rax
        INLINE_GET_ALLOC_CONTEXT_BASE rdx, rax

        ;;
        ;; rcx contains MethodTable pointer
        ;;
        mov         r8d, [rcx + OFFSETOF__MethodTable__m_uBaseSize]

        ;;
        ;; eax: base size
        ;; rcx: MethodTable pointer
        ;; rdx: ee_alloc_context pointer
        ;;

        mov         rax, [rdx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         r9, [rdx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         r9, rax
        cmp         r8, r9
        ja          RhpNewFast_RarePath

        ;; Calculate the new alloc pointer to account for the allocation.
        add         r8, rax

        ;; Set the new object's MethodTable pointer
        mov         [rax + OFFSETOF__Object__m_pEEType], rcx

        ;; Set the new alloc pointer
        mov         [rdx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8

        ret

RhpNewFast_RarePath:
        xor         edx, edx
        jmp         RhpNewObject

LEAF_END RhpNewFast, _TEXT


;; Allocate non-array object with finalizer
;;  RCX == MethodTable
LEAF_ENTRY RhpNewFinalizable, _TEXT

        mov         edx, GC_ALLOC_FINALIZE
        jmp         RhpNewObject

LEAF_END RhpNewFinalizable, _TEXT


;; Allocate non-array object
;;  RCX == MethodTable
;;  EDX == alloc flags
NESTED_ENTRY RhpNewObject, _TEXT

        PUSH_COOP_PINVOKE_FRAME r9

        ; R9: transition frame

        ;; Preserve the MethodTable in RSI
        mov         rsi, rcx

        xor         r8d, r8d        ; numElements

        ;; Call the rest of the allocation helper.
        ;; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        test        rax, rax
        jz          NewOutOfMemory

        POP_COOP_PINVOKE_FRAME
        ret

NewOutOfMemory:
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        mov         rcx, rsi            ; MethodTable pointer
        xor         edx, edx            ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation

NESTED_END RhpNewObject, _TEXT


;; Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
;;  RAX == string/array size
;;  RCX == MethodTable
;;  RDX == character/element count
NEW_ARRAY_FAST MACRO

        ; r10 = ee_alloc_context pointer, TRASHES r8
        INLINE_GET_ALLOC_CONTEXT_BASE r10, r8

        mov         r8, rax
        mov         rax, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         r9, [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         r9, rax

        ; rax == new object ptr
        ; rcx == MethodTable
        ; rdx == element count
        ; r8 == array size
        ; r10 == ee_alloc_context pointer
        cmp         r8, r9
        ja          RhpNewVariableSizeObject

        add         r8, rax
        mov         [rax + OFFSETOF__Object__m_pEEType], rcx
        mov         [rax + OFFSETOF__Array__m_Length], edx
        mov         [r10 + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8
        ret

ENDM ; NEW_ARRAY_FAST


;; Allocate a string.
;;  RCX == MethodTable
;;  EDX == character/element count
LEAF_ENTRY RhNewString, _TEXT

        ; we want to limit the element count to the non-negative 32-bit int range
        cmp         rdx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        ; Compute overall allocation size (align(base size + (element size * elements), 8)).
        lea         rax, [(rdx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 7)]
        and         rax, -8

        NEW_ARRAY_FAST

StringSizeOverflow:
        ; We get here if the size of the final string object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an OOM exception that the caller of this allocator understands.

        ; rcx holds MethodTable pointer already
        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhNewString, _TEXT


;; Allocate one dimensional, zero based array (SZARRAY).
;;  RCX == MethodTable
;;  EDX == element count
LEAF_ENTRY RhpNewArrayFast, _TEXT

        ; we want to limit the element count to the non-negative 32-bit int range
        cmp         rdx, 07fffffffh
        ja          ArraySizeOverflow

        ; Compute overall allocation size (align(base size + (element size * elements), 8)).
        movzx       eax, word ptr [rcx + OFFSETOF__MethodTable__m_usComponentSize]
        imul        rax, rdx
        lea         rax, [rax + SZARRAY_BASE_SIZE + 7]
        and         rax, -8

        NEW_ARRAY_FAST

ArraySizeOverflow:
        ; We get here if the size of the final array object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an overflow exception that the caller of this allocator understands.

        ; rcx holds MethodTable pointer already
        mov         edx, 1              ; Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhpNewArrayFast, _TEXT


;; Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
;;  RCX == MethodTable
;;  EDX == element count
LEAF_ENTRY RhpNewPtrArrayFast, _TEXT

        ; Delegate overflow handling to the generic helper conservatively

        cmp         rdx, (40000000h / 8) ; sizeof(void*)
        jae         RhpNewArrayFast

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        lea         eax, [edx * 8 + SZARRAY_BASE_SIZE]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        NEW_ARRAY_FAST

LEAF_END RhpNewPtrArrayFast, _TEXT


NESTED_ENTRY RhpNewVariableSizeObject, _TEXT

        ; rcx == MethodTable
        ; rdx == element count

        PUSH_COOP_PINVOKE_FRAME r9

        ; r9: transition frame

        ; Preserve the MethodTable in RSI
        mov         rsi, rcx

        ; passing MethodTable in rcx
        mov         r8, rdx         ; numElements
        xor         rdx, rdx        ; uFlags
        ; passing pTransitionFrame in r9

        ; Call the rest of the allocation helper.
        ; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        test        rax, rax
        jz          RhpNewVariableSizeObject_OutOfMemory

        POP_COOP_PINVOKE_FRAME
        ret

RhpNewVariableSizeObject_OutOfMemory:
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        mov         rcx, rsi            ; MethodTable pointer
        xor         edx, edx            ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation

NESTED_END RhpNewVariableSizeObject, _TEXT

        END
