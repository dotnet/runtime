; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc
include asmconstants.inc

EXTERN RhpNewObject : PROC
EXTERN RhpNewVariableSizeObject : PROC
EXTERN RhpGcAllocMaybeFrozen : PROC
EXTERN RhExceptionHandling_FailedAllocation_Helper : PROC

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

    end
