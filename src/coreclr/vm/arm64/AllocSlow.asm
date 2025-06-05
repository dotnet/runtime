; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT NewObject
    IMPORT GcAllocMaybeFrozen
    IMPORT RhExceptionHandling_FailedAllocation_Helper

    TEXTAREA

;
; Object* New(MethodTable *pMT)
;
; Allocate non-array object, slow path.
;
    LEAF_ENTRY New

        mov         x1, #0
        b           NewObject

    LEAF_END

;
; Object* NewMaybeFrozen(MethodTable *pMT)
;
; Allocate non-array object, may be on frozen heap.
;
    NESTED_ENTRY NewMaybeFrozen

        PUSH_COOP_PINVOKE_FRAME x2

        mov         x1, 0
        bl          GcAllocMaybeFrozen

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

    NESTED_END

;
; Object* NewMaybeFrozen(MethodTable *pMT, INT_PTR size)
;
; Allocate array object, may be on frozen heap.
;
    NESTED_ENTRY NewArrayMaybeFrozen

        PUSH_COOP_PINVOKE_FRAME x2

        bl          GcAllocMaybeFrozen

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

    NESTED_END

;
; void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
;
    NESTED_ENTRY RhExceptionHandling_FailedAllocation

        PUSH_COOP_PINVOKE_FRAME x2

        bl          RhExceptionHandling_FailedAllocation_Helper

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

    NESTED_END RhExceptionHandling_FailedAllocation

    END