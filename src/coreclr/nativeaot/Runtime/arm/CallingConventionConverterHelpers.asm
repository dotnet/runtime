;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "kxarm.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    DATAAREA
UniversalThunkPointer % 4
    TEXTAREA

OFFSETOF_CallingConventionId EQU 0
OFFSETOF_commonData EQU 4
OFFSETOF_ManagedCallConverterThunk EQU 0
OFFSETOF_UniversalThunk EQU 4

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; CallingConventionCoverter Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;
;; Note: The "__jmpstub__" prefix is used to indicate to debugger
;; that it must step-through this stub when it encounters it while
;; stepping.
;;

    ;;
    ;; void CallingConventionConverter_ReturnThunk()
    ;;
    LEAF_ENTRY CallingConventionConverter_ReturnThunk
        bx          lr
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
    ;; sp-4 - Points at CommonCallingStubInputData
    ;;
    ;;
    LEAF_ENTRY __jmpstub__CallingConventionConverter_CommonCallingStub
        ldr     r12, [sp, #-4]
        ldr     r12, [r12, #OFFSETOF_CallingConventionId] ; Get CallingConventionId into r12
        str     r12, [sp, #-8] ; Put calling convention id into red zone
        ldr     r12, [sp, #-4]
        ldr     r12, [r12, #OFFSETOF_commonData] ; Get pointer to common data
        ldr     r12, [r12, #OFFSETOF_ManagedCallConverterThunk] ; Get pointer to managed call converter thunk
        str     r12, [sp, #-4] ; Put managed calling convention thunk pointer into red zone (overwrites pointer to CommonCallingStubInputData)
        ldr     r12, =UniversalThunkPointer
        ldr     r12, [r12]
        bx      r12
    LEAF_END __jmpstub__CallingConventionConverter_CommonCallingStub

    ;;
    ;; void CallingConventionConverter_SpecifyCommonStubData(CallingConventionConverter_CommonCallingStub_PointerData *commonData);
    ;;
    LEAF_ENTRY CallingConventionConverter_SpecifyCommonStubData
        ldr     r1, [r0, #OFFSETOF_ManagedCallConverterThunk]     ; Load ManagedCallConverterThunk into r1 {r1 = (CallingConventionConverter_CommonCallingStub_PointerData*)r0->ManagedCallConverterThunk }
        ldr     r2, [r0, #OFFSETOF_UniversalThunk]                ; Load UniversalThunk into r2 {r2 = (CallingConventionConverter_CommonCallingStub_PointerData*)r0->UniversalThunk }
        ldr     r12, =UniversalThunkPointer
        str     r2, [r12]
        bx      lr
    LEAF_END CallingConventionConverter_SpecifyCommonStubData

    ;;
    ;; void CallingConventionConverter_GetStubs(IntPtr *returnVoidStub, IntPtr *returnIntegerStub, IntPtr *commonCallingStub)
    ;;
    LEAF_ENTRY CallingConventionConverter_GetStubs
        ldr     r12, =CallingConventionConverter_ReturnThunk
        str     r12, [r0] ;; ARM doesn't need different return thunks.
        str     r12, [r1]
        ldr     r12, =__jmpstub__CallingConventionConverter_CommonCallingStub
        str     r12, [r2]
        bx      lr
    LEAF_END CallingConventionConverter_GetStubs

    END
