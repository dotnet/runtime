;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros_Shared.inc

; Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
; allocation context then automatically fallback to the slow allocation path.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFast, 4
        ; edx = ee_alloc_context pointer, TRASHES eax
        INLINE_GET_ALLOC_CONTEXT_BASE edx, eax

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          AllocFailed
        cmp         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed
        mov         [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ret

AllocFailed:
        xor         edx, edx
        jmp         @RhpNewObject@8
FASTCALL_ENDFUNC

; Allocate non-array object with finalizer.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFinalizable, 4
        mov         edx, GC_ALLOC_FINALIZE                          ; Flags
        jmp         @RhpNewObject@8
FASTCALL_ENDFUNC

; Allocate non-array object
;  ECX == MethodTable
;  EDX == alloc flags
FASTCALL_FUNC   RhpNewObject, 8
        PUSH_COOP_PINVOKE_FRAME eax

        ; Preserve MethodTable in ESI.
        mov         esi, ecx

        push        eax                                             ; transition frame
        push        0                                               ; numElements
        push        edx
        push        ecx

        ;; Call the rest of the allocation helper.
        ;; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        test        eax, eax
        jz          NewOutOfMemory

        POP_COOP_PINVOKE_FRAME

        ret

NewOutOfMemory:
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        mov         ecx, esi        ; Restore MethodTable pointer
        xor         edx, edx        ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation
FASTCALL_ENDFUNC

; Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
;  EAX == string/array size
;  ECX == MethodTable
;  EDX == character/element count
NEW_ARRAY_FAST_PROLOG MACRO
        push        ecx
        push        edx
ENDM

NEW_ARRAY_FAST MACRO
        LOCAL AllocContextOverflow

        ; EDX = ee_alloc_context pointer, trashes ECX 
        INLINE_GET_ALLOC_CONTEXT_BASE    edx, ecx

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          AllocContextOverflow
        cmp         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocContextOverflow

        ; ECX == allocation size
        ; EAX == new alloc ptr
        ; EDX == ee_alloc_context pointer

        ; set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer
        sub         eax, ecx

        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx
        pop         ecx

        ; set the new object's MethodTable pointer and element count
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        mov         [eax + OFFSETOF__Array__m_Length], edx
        ret

AllocContextOverflow:
        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx
        pop         ecx

        jmp         @RhpNewVariableSizeObject@8
ENDM

; Allocate a new string.
;  ECX == MethodTable
;  EDX == element count
FASTCALL_FUNC   RhNewString, 8
        ; Make sure computing the aligned overall allocation size won't overflow
        cmp         edx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        lea         eax, [(edx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 3)]
        and         eax, -4

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST
        
StringSizeOverflow:
        ; We get here if the size of the final string object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an OOM exception that the caller of this allocator understands.

        ; ecx holds MethodTable pointer already
        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation
FASTCALL_ENDFUNC

; Allocate one dimensional, zero based array (SZARRAY).
;  ECX == MethodTable
;  EDX == element count
FASTCALL_FUNC   RhpNewArrayFast, 8
        NEW_ARRAY_FAST_PROLOG

        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        ; if the element count is <= 0x10000, no overflow is possible because the component size is
        ; <= 0xffff, and thus the product is <= 0xffff0000, and the base size for the worst case
        ; (32 dimensional MdArray) is less than 0xffff.
        movzx       eax, word ptr [ecx + OFFSETOF__MethodTable__m_usComponentSize]
        cmp         edx, 010000h
        ja          ArraySizeBig
        mul         edx
        lea         eax, [eax + SZARRAY_BASE_SIZE + 3]
ArrayAlignSize:
        and         eax, -4

        NEW_ARRAY_FAST

ArraySizeBig:
        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        ; if the element count is negative, it's an overflow, otherwise it's out of memory
        cmp         edx, 0
        jl          ArraySizeOverflow
        mul         edx
        jc          ArrayOutOfMemoryNoFrame
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        jc          ArrayOutOfMemoryNoFrame
        add         eax, 3
        jc          ArrayOutOfMemoryNoFrame
        jmp         ArrayAlignSize

ArrayOutOfMemoryNoFrame:
        add         esp, 8

        ; ecx holds MethodTable pointer already
        xor         edx, edx        ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

ArraySizeOverflow:
        add         esp, 8

        ; We get here if the size of the final array object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an overflow exception that the caller of this allocator understands.

        ; ecx holds MethodTable pointer already
        mov         edx, 1          ; Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation
FASTCALL_ENDFUNC

; Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
;  ECX == MethodTable
;  EDX == element count
FASTCALL_FUNC   RhpNewPtrArrayFast, 8
        ; Delegate overflow handling to the generic helper conservatively

        cmp         edx, (40000000h / 4) ; sizeof(void*)
        jae         @RhpNewArrayFast@8

        ; In this case we know the element size is sizeof(void *), or 4 for x86
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        lea         eax, [edx * 4 + SZARRAY_BASE_SIZE]

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST
FASTCALL_ENDFUNC

;
; Object* RhpNewVariableSizeObject(MethodTable *pMT, INT_PTR size)
;
; ecx == MethodTable
; edx == element count
;
FASTCALL_FUNC RhpNewVariableSizeObject, 8
        PUSH_COOP_PINVOKE_FRAME eax

        ; Preserve MethodTable in ESI.
        mov         esi, ecx

        ; Push alloc helper arguments (transition frame, size, flags, MethodTable).
        push        eax                                             ; transition frame
        push        edx                                             ; numElements
        push        0                                               ; Flags
        push        ecx                                             ; MethodTable

        ; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        test        eax, eax
        jz          RhpNewVariableSizeObject_OutOfMemory

        POP_COOP_PINVOKE_FRAME

        ret

RhpNewVariableSizeObject_OutOfMemory:
        ; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ; an out of memory exception that the caller of this allocator understands.

        mov         ecx, esi        ; Restore MethodTable pointer
        xor         edx, edx        ; Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation
FASTCALL_ENDFUNC

        end
