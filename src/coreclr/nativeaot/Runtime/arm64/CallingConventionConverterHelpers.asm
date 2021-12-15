;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; CallingConventionCoverter Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

POINTER_SIZE                        equ 0x08

;;
;; Note: The "__jmpstub__" prefix is used to indicate to debugger
;; that it must step-through this stub when it encounters it while
;; stepping.
;;

    ;;
    ;; void CallingConventionConverter_ReturnThunk()
    ;;
    LEAF_ENTRY CallingConventionConverter_ReturnThunk
        ret
    LEAF_END CallingConventionConverter_ReturnThunk

    ;;
    ;; __jmpstub__CallingConventionConverter_CommonCallingStub
    ;;
    ;; struct CallingConventionConverter_CommonCallingStub_PointerData
    ;; {
    ;;     void *ManagedCallConverterThunk;
    ;;     void *UniversalThunk;
    ;; }
    ;;
    ;; struct CommonCallingStubInputData
    ;; {
    ;;     ULONG_PTR CallingConventionId;
    ;;     CallingConventionConverter_CommonCallingStub_PointerData *commonData; // Only the ManagedCallConverterThunk field is used
    ;;                                                                           // However, it is specified just like other platforms, so the behavior of the common
    ;;                                                                           // calling stub is easier to debug
    ;; }
    ;;
    ;; xip0 - Points at CommonCallingStubInputData
    ;;
    ;;
    LEAF_ENTRY __jmpstub__CallingConventionConverter_CommonCallingStub
        ldr     xip1, [xip0]                ; put CallingConventionId into xip1 as "parameter" to universal transition thunk
        ldr     xip0, [xip0, #POINTER_SIZE] ; get pointer to CallingConventionConverter_CommonCallingStub_PointerData into xip0
        ldr     x12, [xip0, #POINTER_SIZE]  ; get address of UniversalTransitionThunk (which we'll tailcall to later)
        ldr     xip0, [xip0]                ; get address of ManagedCallConverterThunk (target for universal thunk to call)
        br      x12
    LEAF_END __jmpstub__CallingConventionConverter_CommonCallingStub

    ;;
    ;; void CallingConventionConverter_GetStubs(IntPtr *returnVoidStub, IntPtr *returnIntegerStub, IntPtr *commonCallingStub)
    ;;
    LEAF_ENTRY CallingConventionConverter_GetStubs
        ldr     x12, =CallingConventionConverter_ReturnThunk
        str     x12, [x0] ;; ARM doesn't need different return thunks.
        str     x12, [x1]
        ldr     x12, =__jmpstub__CallingConventionConverter_CommonCallingStub
        str     x12, [x2]
        ret
    LEAF_END CallingConventionConverter_GetStubs

    END
