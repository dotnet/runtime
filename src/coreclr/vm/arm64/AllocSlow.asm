; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT RhpNewObject
    IMPORT RhpGcAllocMaybeFrozen
    IMPORT RhExceptionHandling_FailedAllocation_Helper

    TEXTAREA

;
; Object* RhpNew(MethodTable *pMT)
;
; Allocate non-array object, slow path.
;
    LEAF_ENTRY RhpNew

        mov         x1, #0
        b           RhpNewObject

    LEAF_END

;
; Object* RhpNewMaybeFrozen(MethodTable *pMT)
;
; Allocate non-array object, may be on frozen heap.
;
    NESTED_ENTRY RhpNewMaybeFrozen

        PUSH_COOP_PINVOKE_FRAME x2

        mov         x1, 0
        bl          RhpGcAllocMaybeFrozen

        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

    NESTED_END

;
; Object* RhpNewMaybeFrozen(MethodTable *pMT, INT_PTR size)
;
; Allocate array object, may be on frozen heap.
;
    NESTED_ENTRY RhpNewArrayMaybeFrozen

        PUSH_COOP_PINVOKE_FRAME x2

        bl          RhpGcAllocMaybeFrozen

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