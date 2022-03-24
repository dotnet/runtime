;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.


#include "kxarm.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

__tls_array                         equ 0x2C    ;; offsetof(TEB, ThreadLocalStoragePointer)

POINTER_SIZE                        equ 0x04

;; TLS variables
    AREA    |.tls$|, DATA
ThunkParamSlot % 0x4

    TEXTAREA

    EXTERN _tls_index


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    ;;
    ;; RhCommonStub
    ;;
    NESTED_ENTRY RhCommonStub
        ;; Custom calling convention:
        ;;      red zone has pointer to the current thunk's data block (data contains 2 pointer values: context + target pointers)
        ;;      Copy red zone value into r12 so that the PROLOG_PUSH doesn't destroy it
        PROLOG_NOP  ldr r12, [sp, #-4]
        PROLOG_PUSH {r0-r3}

        ;; Save context data into the ThunkParamSlot thread-local variable
        ;; A pointer to the delegate and function pointer for open static delegate should have been saved in the thunk's context cell during thunk allocation
        ldr         r3, =_tls_index
        ldr         r2, [r3]
        mrc         p15, #0, r3, c13, c0, #2
        ldr         r3, [r3, #__tls_array]
        ldr         r2, [r3, r2, lsl #2]    ;; r2 <- our TLS base

        ;; r2  = base address of TLS data
        ;; r12 = address of context cell in thunk's data

        ;; store thunk address in thread static
        ldr         r1, [r12]
        ldr         r3, =ThunkParamSlot
        str         r1, [r2, r3]            ;; ThunkParamSlot <- context slot data

        ;; Now load the target address and jump to it.
        ldr         r12, [r12, #POINTER_SIZE]
        EPILOG_POP  {r0-r3}
        bx          r12
    NESTED_END RhCommonStub


    ;;
    ;; IntPtr RhGetCommonStubAddress()
    ;;
    LEAF_ENTRY RhGetCommonStubAddress
        ldr     r0, =RhCommonStub
        bx      lr
    LEAF_END RhGetCommonStubAddress


    ;;
    ;; IntPtr RhGetCurrentThunkContext()
    ;;
    LEAF_ENTRY RhGetCurrentThunkContext

        ldr         r3, =_tls_index
        ldr         r2, [r3]
        mrc         p15, #0, r3, c13, c0, #2
        ldr         r3, [r3, #__tls_array]
        ldr         r2, [r3, r2, lsl #2]    ;; r2 <- our TLS base

        ldr         r3, =ThunkParamSlot
        ldr         r0, [r2, r3]            ;; r0 <- ThunkParamSlot

        bx          lr
    LEAF_END RhGetCurrentThunkContext

    END
