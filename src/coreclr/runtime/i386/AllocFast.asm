;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros_Shared.inc

;; Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
;; allocation context then automatically fallback to the slow allocation path.
;;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFast, 4

        ;; edx = ee_alloc_context pointer, TRASHES eax
        INLINE_GET_ALLOC_CONTEXT edx, eax

        ;;
        ;; ecx contains MethodTable pointer
        ;;
        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]

        ;;
        ;; eax: base size
        ;; ecx: MethodTable pointer
        ;; edx: ee_alloc_context pointer
        ;;

        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed

        ;; set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ;; calc the new object pointer
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]

        ;; set the new object's MethodTable pointer
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        ret

AllocFailed:

        xor         edx, edx                                        ; Flags
        jmp         @RhpNewObject@8

FASTCALL_ENDFUNC

;; Allocate non-array object with finalizer.
;;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFinalizable, 4

        mov         edx, GC_ALLOC_FINALIZE                          ; Flags
        jmp         @RhpNewObject@8

FASTCALL_ENDFUNC

;; Allocate non-array object
;;  ECX == MethodTable
;;  EDX == alloc flags
FASTCALL_FUNC   RhpNewObject, 8

        PUSH_COOP_PINVOKE_FRAME eax

        push        eax                                             ; transition frame
        push        0                                               ; numElements

        ;; Call the rest of the allocation helper.
        ;; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        POP_COOP_PINVOKE_FRAME

        test        eax, eax
        jz          NewOutOfMemory

        ret

NewOutOfMemory:
        ;; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ;; an out of memory exception that the caller of this allocator understands.

        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation
FASTCALL_ENDFUNC

;; Allocate a new string.
;;  ECX == MethodTable
;;  EDX == element count
FASTCALL_FUNC   RhNewString, 8

        ;; Make sure computing the aligned overall allocation size won't overflow
        cmp         edx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        push        ecx
        push        edx        

        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        lea         eax, [(edx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 3)]
        and         eax, -4

        ; ECX == MethodTable
        ; EAX == allocation size
        ; EDX == scratch

        INLINE_GET_ALLOC_CONTEXT    edx, ecx        ; edx = ee_alloc_context pointer, TRASHES ecx

        ; ECX == scratch
        ; EAX == allocation size
        ; EDX == ee_alloc_context pointer

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          StringAllocContextOverflow
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          StringAllocContextOverflow

        ; ECX == allocation size
        ; EAX == new alloc ptr
        ; EDX == ee_alloc_context pointer

        ; set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer
        sub         eax, ecx

        pop         edx
        pop         ecx

        ; set the new object's MethodTable pointer and element count
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        mov         [eax + OFFSETOF__String__m_Length], edx
        ret

StringAllocContextOverflow:
        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx
        pop         ecx

        jmp         @RhpNewArray@8

StringSizeOverflow:
        ;; We get here if the size of the final string object can't be represented as an unsigned
        ;; 32-bit value. We're going to tail-call to a managed helper that will throw
        ;; an OOM exception that the caller of this allocator understands.

        ;; ecx holds MethodTable pointer already
        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

FASTCALL_ENDFUNC


;; Allocate one dimensional, zero based array (SZARRAY).
;;  ECX == MethodTable
;;  EDX == element count
FASTCALL_FUNC   RhpNewArrayFast, 8

        push        ecx
        push        edx

        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        ; if the element count is <= 0x10000, no overflow is possible because the component size is
        ; <= 0xffff, and thus the product is <= 0xffff0000, and the base size for the worst case
        ; (32 dimensional MdArray) is less than 0xffff.
        movzx       eax, word ptr [ecx + OFFSETOF__MethodTable__m_usComponentSize]
        cmp         edx,010000h
        ja          ArraySizeBig
        mul         edx
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, 3
ArrayAlignSize:
        and         eax, -4

        ; ECX == MethodTable
        ; EAX == array size
        ; EDX == scratch

        INLINE_GET_ALLOC_CONTEXT    edx, ecx        ; edx = ee_alloc_context pointer, TRASHES ecx

        ; ECX == scratch
        ; EAX == array size
        ; EDX == ee_alloc_context pointer

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          ArrayAllocContextOverflow
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          ArrayAllocContextOverflow

        ; ECX == array size
        ; EAX == new alloc ptr
        ; EDX == ee_alloc_context pointer

        ; set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer
        sub         eax, ecx

        pop         edx
        pop         ecx

        ; set the new object's MethodTable pointer and element count
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        mov         [eax + OFFSETOF__Array__m_Length], edx
        ret

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

ArrayAllocContextOverflow:
        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx.
        pop         ecx

        jmp         @RhpNewArray@8

ArrayOutOfMemoryNoFrame:
        add         esp, 8          ; pop ecx / edx

        ; ecx holds MethodTable pointer already
        xor         edx, edx        ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

ArraySizeOverflow:
        ; We get here if the size of the final array object can't be represented as an unsigned
        ; 32-bit value. We're going to tail-call to a managed helper that will throw
        ; an overflow exception that the caller of this allocator understands.

        add         esp, 8          ; pop ecx / edx

        ; ecx holds MethodTable pointer already
        mov         edx, 1          ; Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation

FASTCALL_ENDFUNC

;;
;; Object* RhpNewArray(MethodTable *pMT, INT_PTR size)
;;
;; ecx == MethodTable
;; edx == element count
;;
FASTCALL_FUNC RhpNewArray, 8

        PUSH_COOP_PINVOKE_FRAME eax

        ; Push alloc helper arguments (transition frame, size, flags, MethodTable).
        push        eax                                             ; transition frame
        push        edx                                             ; numElements
        xor         edx, edx                                        ; Flags
        ; Passing MethodTable in ecx

        ; void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        POP_COOP_PINVOKE_FRAME

        test        eax, eax
        jz          ArrayOutOfMemory

        ret

ArrayOutOfMemory:
        ; This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        ; an out of memory exception that the caller of this allocator understands.

        xor         edx, edx        ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

FASTCALL_ENDFUNC

        end
