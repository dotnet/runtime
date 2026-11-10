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

    end
