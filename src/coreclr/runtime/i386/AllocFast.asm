;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros_Shared.inc

; Shared code for RhpNewFast, RhpNewFastAlign8 and RhpNewFastMisalign
;  ECX == MethodTable
NEW_FAST MACRO Variation

        LOCAL AlreadyAligned
        LOCAL AllocFailed

        ; edx = ee_alloc_context pointer, TRASHES eax
        INLINE_GET_ALLOC_CONTEXT edx, eax

        ; When doing aligned or misaligned allocation we first check
        ; the alignment and skip to the regular path if it's already
        ; matching the expectation.
        ; Otherwise, we try to allocate size + ASM_MIN_OBJECT_SIZE and
        ; then prepend a dummy free object at the beginning of the
        ; allocation.
IFDIF <&Variation>, <>
        mov         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        test        eax, 7
IFIDN <&Variation>, <Align8>
        jz          AlreadyAligned
ELSE ; Variation == <Misalign>
        jnz         AlreadyAligned
ENDIF

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, ASM_MIN_OBJECT_SIZE
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ; initialize the padding object preceeding the new object
        mov         edx, [G_FREE_OBJECT_METHOD_TABLE]
        mov         [eax + OFFSETOF__Object__m_pEEType - ASM_MIN_OBJECT_SIZE], edx
        mov         dword ptr [eax + OFFSETOF__Array__m_Length - ASM_MIN_OBJECT_SIZE], 0

        ret
ENDIF ; Variation != ""

AlreadyAligned:
        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ret

AllocFailed:
IFIDN <&Variation>, <Align8>
        mov         edx, GC_ALLOC_ALIGN8
ELSEIFIDN <&Variation>, <Misalign>
        mov         edx, GC_ALLOC_ALIGN8 + GC_ALLOC_ALIGN8_BIAS
ELSE
        xor         edx, edx
ENDIF
        jmp         @RhpNewObject@8

ENDM

; Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
; allocation context then automatically fallback to the slow allocation path.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFast, 4
        NEW_FAST <>
FASTCALL_ENDFUNC

; Allocate simple object (not finalizable, array or value type) on an 8 byte boundary.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFastAlign8, 4
        NEW_FAST <Align8>
FASTCALL_ENDFUNC

; Allocate a value type object (i.e. box it) on an 8 byte boundary + 4 (so that the value type payload
; itself is 8 byte aligned).
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFastMisalign, 4
        NEW_FAST <Misalign>
FASTCALL_ENDFUNC

; Allocate non-array object with finalizer.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFinalizable, 4
        mov         edx, GC_ALLOC_FINALIZE                          ; Flags
        jmp         @RhpNewObject@8
FASTCALL_ENDFUNC

; Allocate non-array object with finalizer on an 8 byte boundary.
;  ECX == MethodTable
FASTCALL_FUNC   RhpNewFinalizableAlign8, 4
        mov         edx, GC_ALLOC_FINALIZE + GC_ALLOC_ALIGN8        ; Flags
        jmp         @RhpNewObject@8
FASTCALL_ENDFUNC

; Allocate non-array object
;  ECX == MethodTable
;  EDX == alloc flags
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

; Shared code for RhNewString, RhpNewArrayFast and RhpNewObjectArray
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
        INLINE_GET_ALLOC_CONTEXT    edx, ecx

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          AllocContextOverflow
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocContextOverflow

        ; ECX == allocation size
        ; EAX == new alloc ptr
        ; EDX == ee_alloc_context pointer

        ; set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

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

        jmp         @RhpNewArray@8

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
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, 3
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

IFNDEF FEATURE_NATIVEAOT
; Allocate one dimensional, zero based array (SZARRAY) of objects (pointer sized elements).
;  ECX == MethodTable
;  EDX == element count
FASTCALL_FUNC   RhpNewObjectArrayFast, 8

        cmp         edx, (ASM_LARGE_OBJECT_SIZE - 256)/4 ; sizeof(void*)
        jae         @RhpNewArray@8

        ; In this case we know the element size is sizeof(void *), or 4 for x86
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov         eax, dword ptr [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        lea         eax, [eax + edx * 4]

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST

FASTCALL_ENDFUNC
ENDIF

; Shared code for RhpNewArray and RhpNewArrayFastAlign8
NEW_ARRAY MACRO Flags
        LOCAL ArrayOutOfMemory

        PUSH_COOP_PINVOKE_FRAME eax

        ; Push alloc helper arguments (transition frame, size, flags, MethodTable).
        push        eax                                             ; transition frame
        push        edx                                             ; numElements
IF Flags EQ 0
        xor         edx, edx                                        ; Flags
ELSE
        mov         edx, Flags
ENDIF
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
ENDM

;
; Object* RhpNewArray(MethodTable *pMT, INT_PTR size)
;
; ecx == MethodTable
; edx == element count
;
FASTCALL_FUNC RhpNewArray, 8
        NEW_ARRAY 0
FASTCALL_ENDFUNC

;
; Object* RhpNewArrayFastAlign8(MethodTable *pMT, INT_PTR size)
;
; ecx == MethodTable
; edx == element count
;
FASTCALL_FUNC   RhpNewArrayFastAlign8, 8
        ; We don't really provide a fast path here. CoreCLR has a configurable threshold
        ; for array size to go to large object heap and it's not worth the extra effort
        ; to check for it.
        NEW_ARRAY GC_ALLOC_ALIGN8
FASTCALL_ENDFUNC

        end
