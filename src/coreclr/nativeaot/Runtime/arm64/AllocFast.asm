;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

;; Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
;; allocation context then automatically fallback to the slow allocation path.
;;  x0 == MethodTable
    LEAF_ENTRY RhpNewFast

        ;; x1 = GetThread(), TRASHES x2
        INLINE_GETTHREAD x1, x2

        ;;
        ;; x0 contains MethodTable pointer
        ;;
        ldr         w2, [x0, #OFFSETOF__MethodTable__m_uBaseSize]

        ;;
        ;; x0: MethodTable pointer
        ;; x1: Thread pointer
        ;; x2: base size
        ;;

        ;; Load potential new object address into x12.
        ldr         x12, [x1, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Determine whether the end of the object would lie outside of the current allocation context. If so,
        ;; we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add         x2, x2, x12
        ldr         x13, [x1, #OFFSETOF__Thread__m_alloc_context__alloc_limit]
        cmp         x2, x13
        bhi         RhpNewFast_RarePath

        ;; Update the alloc pointer to account for the allocation.
        str         x2, [x1, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Set the new object's MethodTable pointer
        str         x0, [x12, #OFFSETOF__Object__m_pEEType]

        mov         x0, x12
        ret

RhpNewFast_RarePath
        mov         x1, #0
        b           RhpNewObject
    LEAF_END RhpNewFast

    INLINE_GETTHREAD_CONSTANT_POOL

;; Allocate non-array object with finalizer.
;;  x0 == MethodTable
    LEAF_ENTRY RhpNewFinalizable
        mov         x1, #GC_ALLOC_FINALIZE
        b           RhpNewObject
    LEAF_END RhpNewFinalizable

;; Allocate non-array object.
;;  x0 == MethodTable
;;  x1 == alloc flags
    NESTED_ENTRY RhpNewObject

        PUSH_COOP_PINVOKE_FRAME x3

        ;; x3: transition frame

        ;; Preserve the MethodTable in x19
        mov         x19, x0

        mov         w2, #0              ; numElements

        ;; Call the rest of the allocation helper.
        ;; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl          RhpGcAlloc

        cbz         x0, NewOutOfMemory

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

NewOutOfMemory
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        mov         x0, x19             ; MethodTable pointer
        mov         x1, #0              ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        EPILOG_NOP b RhExceptionHandling_FailedAllocation

    NESTED_END RhpNewObject

;; Allocate a string.
;;  x0 == MethodTable
;;  x1 == element/character count
    LEAF_ENTRY RhNewString
        ;; Make sure computing the overall allocation size won't overflow
        movz        x2, #(MAX_STRING_LENGTH & 0xFFFF)
        movk        x2, #(MAX_STRING_LENGTH >> 16), lsl #16
        cmp         x1, x2
        bhi         StringSizeOverflow

        ;; Compute overall allocation size (align(base size + (element size * elements), 8)).
        mov         w2, #STRING_COMPONENT_SIZE
        mov         x3, #(STRING_BASE_SIZE + 7)
        umaddl      x2, w1, w2, x3          ; x2 = w1 * w2 + x3
        and         x2, x2, #-8

        ; x0 == MethodTable
        ; x1 == element count
        ; x2 == string size

        INLINE_GETTHREAD x3, x5

        ;; Load potential new object address into x12.
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Determine whether the end of the object would lie outside of the current allocation context. If so,
        ;; we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add         x2, x2, x12
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_limit]
        cmp         x2, x12
        bhi         RhpNewArrayRare

        ;; Reload new object address into r12.
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Update the alloc pointer to account for the allocation.
        str         x2, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Set the new object's MethodTable pointer and element count.
        str         x0, [x12, #OFFSETOF__Object__m_pEEType]
        str         x1, [x12, #OFFSETOF__Array__m_Length]

        ;; Return the object allocated in x0.
        mov         x0, x12

        ret

StringSizeOverflow
        ; We get here if the length of the final string object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an OOM exception that the caller of this allocator understands.

        ; x0 holds MethodTable pointer already
        mov         x1, #1                  ; Indicate that we should throw OverflowException
        b           RhExceptionHandling_FailedAllocation
    LEAF_END    RhNewString

    INLINE_GETTHREAD_CONSTANT_POOL


;; Allocate one dimensional, zero based array (SZARRAY).
;;  x0 == MethodTable
;;  x1 == element count
    LEAF_ENTRY RhpNewArray

        ;; We want to limit the element count to the non-negative 32-bit int range.
        ;; If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        ;; size is <= 0xffff (it's an unsigned 16-bit value), and the base size for the worst
        ;; case (32 dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        mov         x2, #0x7FFFFFFF
        cmp         x1, x2
        bhi         ArraySizeOverflow

        ldrh        w2, [x0, #OFFSETOF__MethodTable__m_usComponentSize]
        umull       x2, w1, w2
        ldr         w3, [x0, #OFFSETOF__MethodTable__m_uBaseSize]
        add         x2, x2, x3
        add         x2, x2, #7
        and         x2, x2, #-8

        ; x0 == MethodTable
        ; x1 == element count
        ; x2 == array size

        INLINE_GETTHREAD x3, x5

        ;; Load potential new object address into x12.
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Determine whether the end of the object would lie outside of the current allocation context. If so,
        ;; we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add         x2, x2, x12
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_limit]
        cmp         x2, x12
        bhi         RhpNewArrayRare

        ;; Reload new object address into x12.
        ldr         x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Update the alloc pointer to account for the allocation.
        str         x2, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]

        ;; Set the new object's MethodTable pointer and element count.
        str         x0, [x12, #OFFSETOF__Object__m_pEEType]
        str         x1, [x12, #OFFSETOF__Array__m_Length]

        ;; Return the object allocated in r0.
        mov         x0, x12

        ret

ArraySizeOverflow
        ; We get here if the size of the final array object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an overflow exception that the caller of this allocator understands.

        ; x0 holds MethodTable pointer already
        mov         x1, #1                  ; Indicate that we should throw OverflowException
        b           RhExceptionHandling_FailedAllocation
    LEAF_END    RhpNewArray

    INLINE_GETTHREAD_CONSTANT_POOL

;; Allocate one dimensional, zero based array (SZARRAY) using the slow path that calls a runtime helper.
;;  x0 == MethodTable
;;  x1 == element count
;;  x2 == array size + Thread::m_alloc_context::alloc_ptr
;;  x3 == Thread
    NESTED_ENTRY RhpNewArrayRare

        ; Recover array size by subtracting the alloc_ptr from x2.
        PROLOG_NOP ldr x12, [x3, #OFFSETOF__Thread__m_alloc_context__alloc_ptr]
        PROLOG_NOP sub x2, x2, x12

        PUSH_COOP_PINVOKE_FRAME x3

        ; Preserve data we'll need later into the callee saved registers
        mov         x19, x0             ; Preserve MethodTable

        mov         x2, x1              ; numElements
        mov         x1, #0              ; uFlags

        ;; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl          RhpGcAlloc

        cbz         x0, ArrayOutOfMemory

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

ArrayOutOfMemory
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        mov         x0, x19             ; MethodTable Pointer
        mov         x1, #0              ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        EPILOG_NOP b RhExceptionHandling_FailedAllocation

    NESTED_END RhpNewArrayRare

    END
