; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include asmconstants.inc
include asmmacros.inc

EXTERN _RhpGcAllocMaybeFrozen@12 : PROC
EXTERN _RhExceptionHandling_FailedAllocation_Helper@12 : PROC
EXTERN @RhpNewObject@8 : PROC
EXTERN @RhpNewVariableSizeObject@8 : PROC

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
        push        0
        push        ecx
        call        _RhpGcAllocMaybeFrozen@12

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
        push        edx
        push        ecx
        call        _RhpGcAllocMaybeFrozen@12

        POP_COOP_PINVOKE_FRAME
        ret
FASTCALL_ENDFUNC

;
; void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
;
RhExceptionHandling_FailedAllocation PROC PUBLIC
        PUSH_COOP_PINVOKE_FRAME eax

        push        eax
        push        edx
        push        ecx
        call        _RhExceptionHandling_FailedAllocation_Helper@12

        POP_COOP_PINVOKE_FRAME
        ret
RhExceptionHandling_FailedAllocation ENDP

;
; void RhpNewFast_UP(MethodTable *pMT)
;
; Allocate non-array object, uniprocessor version
;
FASTCALL_FUNC   RhpNewFast_UP, 4
        inc         [g_global_alloc_lock]
        jnz         AllocFailed

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [g_global_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          AllocFailed_Unlock
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
        xor         edx, edx
        jmp         @RhpNewObject@8
FASTCALL_ENDFUNC

;
; Shared code for RhNewString_UP, RhpNewArrayFast_UP and RhpNewPtrArrayFast_UP
;  EAX == string/array size
;  ECX == MethodTable
;  EDX == character/element count
;
NEW_ARRAY_FAST_PROLOG_UP MACRO
        inc         [g_global_alloc_lock]
        jnz         @RhpNewVariableSizeObject@8

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
        jmp         @RhpNewVariableSizeObject@8
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
        lea         eax, [eax + SZARRAY_BASE_SIZE + 3]
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
; Object* RhpNewPtrArrayFast_UP(MethodTable *pMT, INT_PTR elementCount)
; 
; Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements,
; uniprocessor version
;
FASTCALL_FUNC   RhpNewPtrArrayFast_UP, 8
        ; Delegate overflow handling to the generic helper conservatively

        cmp         edx, (40000000h / 4) ; sizeof(void*)
        jae         @RhpNewVariableSizeObject@8

        ; In this case we know the element size is sizeof(void *), or 4 for x86
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        lea         eax, [edx * 4 + SZARRAY_BASE_SIZE]

        NEW_ARRAY_FAST_PROLOG_UP
        NEW_ARRAY_FAST_UP
FASTCALL_ENDFUNC


    end
