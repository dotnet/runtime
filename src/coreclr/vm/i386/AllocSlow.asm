; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include asmconstants.inc
include asmmacros.inc

EXTERN @RhpGcAlloc@16 : PROC
EXTERN @RhpGcAllocMaybeFrozen@12 : PROC
EXTERN @RhExceptionHandling_FailedAllocation_Helper@12 : PROC
EXTERN @RhpNewObject@8 : PROC
EXTERN @RhpNewArray@8 : PROC

g_global_alloc_lock EQU _g_global_alloc_lock
g_global_alloc_context EQU _g_global_alloc_context

EXTERN g_global_alloc_lock : DWORD
EXTERN g_global_alloc_context : DWORD

;
; Object* RhpNew(MethodTable *pMT)
;
; Allocate non-array object, slow path.
;
FASTCALL_FUNC RhpNew, 4

        xor         edx, edx
        jmp         @RhpNewObject@8

FASTCALL_ENDFUNC

;
; Object* RhpNewMaybeFrozen(MethodTable *pMT)
;
; Allocate non-array object, may be on frozen heap.
;
FASTCALL_FUNC RhpNewMaybeFrozen, 4

        PUSH_COOP_PINVOKE_FRAME eax

        push        eax
        mov         edx, 0
        call        @RhpGcAllocMaybeFrozen@12

        POP_COOP_PINVOKE_FRAME
        ret

FASTCALL_ENDFUNC

;
; Object* RhpNewMaybeFrozen(MethodTable *pMT, INT_PTR size)
;
; Allocate array object, may be on frozen heap.
;
FASTCALL_FUNC RhpNewArrayMaybeFrozen, 8

        PUSH_COOP_PINVOKE_FRAME eax

        push        eax
        call        @RhpGcAllocMaybeFrozen@12

        POP_COOP_PINVOKE_FRAME
        ret

FASTCALL_ENDFUNC

;
; void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
;
RhExceptionHandling_FailedAllocation PROC PUBLIC

        PUSH_COOP_PINVOKE_FRAME eax

        push        eax
        call        @RhExceptionHandling_FailedAllocation_Helper@12

        POP_COOP_PINVOKE_FRAME
        ret

RhExceptionHandling_FailedAllocation ENDP

;
; Shared code for RhpNewFast_UP, RhpNewFastAlign8_UP and RhpNewFastMisalign_UP
;  ECX == MethodTable
;
NEW_FAST_UP MACRO Variation

        LOCAL AlreadyAligned
        LOCAL AllocFailed
        LOCAL AllocFailed_Unlock

        inc         [g_global_alloc_lock]
        jnz         AllocFailed

        ;; When doing aligned or misaligned allocation we first check
        ;; the alignment and skip to the regular path if it's already
        ;; matching the expectation.
        ;; Otherwise, we try to allocate size + ASM_MIN_OBJECT_SIZE and
        ;; then prepend a dummy free object at the beginning of the
        ;; allocation.
IFDIF <&Variation>, <>
        mov         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        test        eax, 7
IFIDN <&Variation>, <Align8>
        jz          AlreadyAligned
ELSE ; Variation == <Misalign>
        jnz         AlreadyAligned
ENDIF

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, ASM_MIN_OBJECT_SIZE
        add         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed_Unlock
        mov         [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ; initialize the padding object preceeding the new object
        mov         edx, [G_FREE_OBJECT_METHOD_TABLE]
        mov         [eax + OFFSETOF__Object__m_pEEType - ASM_MIN_OBJECT_SIZE], edx
        mov         dword ptr [eax + OFFSETOF__Array__m_Length - ASM_MIN_OBJECT_SIZE], 0

        mov         [g_global_alloc_lock], -1
        ret
ENDIF ; Variation != ""

AlreadyAligned:
        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocFailed_Unlock
        mov         [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        mov         [g_global_alloc_lock], -1
        ret

AllocFailed_Unlock:
        mov         [g_global_alloc_lock], -1

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

;
; void RhpNewFast_UP(MethodTable *pMT)
;
; Allocate non-array object, uniprocessor version
;
FASTCALL_FUNC   RhpNewFast_UP, 4
        NEW_FAST_UP
FASTCALL_ENDFUNC

;
; void RhpNewFastAlign8_UP(MethodTable *pMT)
;
; Allocate simple object (not finalizable, array or value type) on an 8 byte boundary,
; uniprocessor version
;
FASTCALL_FUNC   RhpNewFastAlign8_UP, 4
        NEW_FAST_UP <Align8>
FASTCALL_ENDFUNC

;
; void RhpNewFastMisalign_UP(MethodTable *pMT)
;
; Allocate a value type object (i.e. box it) on an 8 byte boundary + 4 (so that the value type payload
; itself is 8 byte aligned), uniprocessor version
;
FASTCALL_FUNC   RhpNewFastMisalign_UP, 4
        NEW_FAST_UP <Misalign>
FASTCALL_ENDFUNC

;
; Shared code for RhNewString_UP, RhpNewArrayFast_UP and RhpNewObjectArray_UP
;  EAX == string/array size
;  ECX == MethodTable
;  EDX == character/element count
;
NEW_ARRAY_FAST_PROLOG_UP MACRO
        inc         [g_global_alloc_lock]
        jnz         @RhpNewArray@8

        push        ecx
        push        edx
ENDM

NEW_ARRAY_FAST_UP MACRO

        LOCAL AllocContextOverflow

        ; ECX == MethodTable
        ; EAX == allocation size
        ; EDX == string length

        mov         ecx, eax
        add         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          AllocContextOverflow
        cmp         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          AllocContextOverflow

        ; ECX == allocation size
        ; EAX == new alloc ptr

        ; set the new alloc pointer
        mov         [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        ; calc the new object pointer
        sub         eax, ecx

        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx
        pop         ecx

        ; set the new object's MethodTable pointer and element count
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        mov         [eax + OFFSETOF__Array__m_Length], edx
        mov         [g_global_alloc_lock], -1
        ret

AllocContextOverflow:
        ; Restore the element count and put it in edx
        pop         edx
        ; Restore the MethodTable and put it in ecx
        pop         ecx

        mov         [g_global_alloc_lock], -1
        jmp         @RhpNewArray@8

ENDM

;
; Object* RhNewString_UP(MethodTable *pMT, DWORD stringLength)
;
; Allocate a string, uniprocessor version
;
FASTCALL_FUNC   RhNewString_UP, 8

        ;; Make sure computing the aligned overall allocation size won't overflow
        cmp         edx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        ; Compute overall allocation size (align(base size + (element size * elements), 4)).
        lea         eax, [(edx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 3)]
        and         eax, -4

        NEW_ARRAY_FAST_PROLOG_UP
        NEW_ARRAY_FAST_UP

StringSizeOverflow:
        ;; We get here if the size of the final string object can't be represented as an unsigned
        ;; 32-bit value. We're going to tail-call to a managed helper that will throw
        ;; an OOM exception that the caller of this allocator understands.

        ;; ecx holds MethodTable pointer already
        xor         edx, edx            ; Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

FASTCALL_ENDFUNC

;
; Object* RhpNewArrayFast_UP(MethodTable *pMT, INT_PTR elementCount)
; 
; Allocate one dimensional, zero based array (SZARRAY), uniprocessor version
;
FASTCALL_FUNC   RhpNewArrayFast_UP, 8

        NEW_ARRAY_FAST_PROLOG_UP

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

        NEW_ARRAY_FAST_UP

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

;
; Object* RhpNewObjectArrayFast_UP(MethodTable *pMT, INT_PTR elementCount)
; 
; Allocate one dimensional, zero based array (SZARRAY) of objects (pointer sized elements),
; uniprocessor version
;
FASTCALL_FUNC   RhpNewObjectArrayFast_UP, 8

        cmp         edx, (ASM_LARGE_OBJECT_SIZE - 256)/4 ; sizeof(void*)
        jae         @RhpNewArray@8

        ; In this case we know the element size is sizeof(void *), or 4 for x86
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov         eax, dword ptr [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        lea         eax, [eax + edx * 8]

        NEW_ARRAY_FAST_PROLOG_UP
        NEW_ARRAY_FAST_UP

FASTCALL_ENDFUNC


    end
