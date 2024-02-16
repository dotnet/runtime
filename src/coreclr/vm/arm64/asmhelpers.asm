; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT ExternalMethodFixupWorker
    IMPORT PreStubWorker
    IMPORT NDirectImportWorker
    IMPORT VSD_ResolveWorker
    IMPORT JIT_InternalThrow
    IMPORT ComPreStubWorker
    IMPORT COMToCLRWorker
    IMPORT CallDescrWorkerUnwindFrameChainHandler
    IMPORT UMEntryPrestubUnwindFrameChainHandler
    IMPORT TheUMEntryPrestubWorker
    IMPORT GetCurrentSavedRedirectContext
    IMPORT OnHijackWorker
#ifdef FEATURE_READYTORUN
    IMPORT DynamicHelperWorker
#endif
    IMPORT HijackHandler
    IMPORT ThrowControlForThread

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    IMPORT  g_sw_ww_table
#endif

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    IMPORT g_card_bundle_table
#endif

    IMPORT  g_ephemeral_low
    IMPORT  g_ephemeral_high
    IMPORT  g_lowest_address
    IMPORT  g_highest_address
    IMPORT  g_card_table
    IMPORT  g_dispatch_cache_chain_success_counter
#ifdef WRITE_BARRIER_CHECK
    SETALIAS g_GCShadow, ?g_GCShadow@@3PEAEEA
    SETALIAS g_GCShadowEnd, ?g_GCShadowEnd@@3PEAEEA

    IMPORT g_lowest_address
    IMPORT $g_GCShadow
    IMPORT $g_GCShadowEnd
#endif // WRITE_BARRIER_CHECK

    IMPORT JIT_GetSharedNonGCStaticBase_Helper
    IMPORT JIT_GetSharedGCStaticBase_Helper

#ifdef FEATURE_COMINTEROP
    IMPORT CLRToCOMWorker
#endif // FEATURE_COMINTEROP

    IMPORT JIT_WriteBarrier_Table_Loc
    IMPORT JIT_WriteBarrier_Loc

    ;;like TEXTAREA, but with 64 byte alignment so that we can align the patchable pool below to 64 without warning
    AREA    |.text|,ALIGN=6,CODE,READONLY

;; LPVOID __stdcall GetCurrentIP(void);
    LEAF_ENTRY GetCurrentIP
        mov     x0, lr
        ret     lr
    LEAF_END

;; LPVOID __stdcall GetCurrentSP(void);
    LEAF_ENTRY GetCurrentSP
        mov     x0, sp
        ret     lr
    LEAF_END

;; DWORD64 __stdcall GetDataCacheZeroIDReg(void);
    LEAF_ENTRY GetDataCacheZeroIDReg
        mrs     x0, dczid_el0
        and     x0, x0, 31
        ret     lr
    LEAF_END

;;-----------------------------------------------------------------------------
;; This routine captures the machine state. It is used by helper method frame
;;-----------------------------------------------------------------------------
;;void LazyMachStateCaptureState(struct LazyMachState *pState);
        LEAF_ENTRY LazyMachStateCaptureState
        ;; marks that this is not yet valid
        mov     w1, #0
        str     w1, [x0, #MachState__isValid]

        str     lr, [x0, #LazyMachState_captureIp]

        ;; str instruction does not save sp register directly so move to temp register
        mov     x1, sp
        str     x1, [x0, #LazyMachState_captureSp]

        ;; save non-volatile registers that can contain object references
        add     x1, x0, #LazyMachState_captureX19_X29
        stp     x19, x20, [x1, #(16*0)]
        stp     x21, x22, [x1, #(16*1)]
        stp     x23, x24, [x1, #(16*2)]
        stp     x25, x26, [x1, #(16*3)]
        stp     x27, x28, [x1, #(16*4)]
        str     x29, [x1, #(16*5)]

        ret     lr
        LEAF_END

        ;
        ; If a preserved register were pushed onto the stack between
        ; the managed caller and the H_M_F, ptrX19_X29 will point to its
        ; location on the stack and it would have been updated on the
        ; stack by the GC already and it will be popped back into the
        ; appropriate register when the appropriate epilog is run.
        ;
        ; Otherwise, the register is preserved across all the code
        ; in this HCALL or FCALL, so we need to update those registers
        ; here because the GC will have updated our copies in the
        ; frame.
        ;
        ; So, if ptrX19_X29 points into the MachState, we need to update
        ; the register here.  That's what this macro does.
        ;

        MACRO
            RestoreRegMS $regIndex, $reg

        ; Incoming:
        ;
        ; x0 = address of MachState
        ;
        ; $regIndex: Index of the register (x19-x29). For x19, index is 19.
        ;            For x20, index is 20, and so on.
        ;
        ; $reg: Register name (e.g. x19, x20, etc)
        ;
        ; Get the address of the specified captured register from machine state
        add     x2, x0, #(MachState__captureX19_X29 + (($regIndex-19)*8))

        ; Get the content of specified preserved register pointer from machine state
        ldr     x3, [x0, #(MachState__ptrX19_X29 + (($regIndex-19)*8))]

        cmp     x2, x3
        bne     %FT0
        ldr     $reg, [x2]
0

        MEND

; EXTERN_C int __fastcall HelperMethodFrameRestoreState(
;         INDEBUG_COMMA(HelperMethodFrame *pFrame)
;         MachState *pState
;         )
        LEAF_ENTRY HelperMethodFrameRestoreState

#ifdef _DEBUG
        mov x0, x1
#endif

        ; If machine state is invalid, then simply exit
        ldr w1, [x0, #MachState__isValid]
        cmp w1, #0
        beq Done

        RestoreRegMS 19, X19
        RestoreRegMS 20, X20
        RestoreRegMS 21, X21
        RestoreRegMS 22, X22
        RestoreRegMS 23, X23
        RestoreRegMS 24, X24
        RestoreRegMS 25, X25
        RestoreRegMS 26, X26
        RestoreRegMS 27, X27
        RestoreRegMS 28, X28
        RestoreRegMS 29, X29

Done
        ; Its imperative that the return value of HelperMethodFrameRestoreState is zero
        ; as it is used in the state machine to loop until it becomes zero.
        ; Refer to HELPER_METHOD_FRAME_END macro for details.
        mov x0,#0
        ret lr

        LEAF_END

; ------------------------------------------------------------------
; The call in ndirect import precode points to this function.
        NESTED_ENTRY NDirectImportThunk

        PROLOG_SAVE_REG_PAIR           fp, lr, #-224!
        SAVE_ARGUMENT_REGISTERS        sp, 16
        SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 96

        mov     x0, x12
        bl      NDirectImportWorker
        mov     x12, x0

        ; pop the stack and restore original register state
        RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 96
        RESTORE_ARGUMENT_REGISTERS        sp, 16
        EPILOG_RESTORE_REG_PAIR           fp, lr, #224!

        ; If we got back from NDirectImportWorker, the MD has been successfully
        ; linked. Proceed to execute the original DLL call.
        EPILOG_BRANCH_REG x12

        NESTED_END

; ------------------------------------------------------------------

        NESTED_ENTRY ThePreStub

        PROLOG_WITH_TRANSITION_BLOCK

        add         x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         x1, METHODDESC_REGISTER         ; pMethodDesc

        bl          PreStubWorker

        mov         x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x9

        NESTED_END

;; ------------------------------------------------------------------
;; ThePreStubPatch()

        LEAF_ENTRY ThePreStubPatch
        nop
ThePreStubPatchLabel
        EXPORT          ThePreStubPatchLabel
        ret             lr
        LEAF_END

;-----------------------------------------------------------------------------
; void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck, size_t writeableOffset)
;
; Update shadow copies of the various state info required for barrier
;
; State info is contained in a literal pool at the end of the function
; Placed in text section so that it is close enough to use ldr literal and still
; be relocatable. Eliminates need for PREPARE_EXTERNAL_VAR in hot code.
;
; Align and group state info together so it fits in a single cache line
; and each entry can be written atomically
;
    LEAF_ENTRY JIT_UpdateWriteBarrierState
        PROLOG_SAVE_REG_PAIR   fp, lr, #-16!

        ; x0-x7, x10 will contain intended new state
        ; x8 will preserve skipEphemeralCheck
        ; x12 will be used for pointers

        mov      x8, x0
        mov      x9, x1

        adrp     x12, g_card_table
        ldr      x0, [x12, g_card_table]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        adrp     x12, g_card_bundle_table
        ldr      x1, [x12, g_card_bundle_table]
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        adrp     x12, g_sw_ww_table
        ldr      x2, [x12, g_sw_ww_table]
#endif

        adrp     x12, g_ephemeral_low
        ldr      x3, [x12, g_ephemeral_low]

        adrp     x12, g_ephemeral_high
        ldr      x4, [x12, g_ephemeral_high]

        ; Check skipEphemeralCheck
        cbz      x8, EphemeralCheckEnabled
        movz     x3, #0
        movn     x4, #0

EphemeralCheckEnabled
        adrp     x12, g_lowest_address
        ldr      x5, [x12, g_lowest_address]

        adrp     x12, g_highest_address
        ldr      x6, [x12, g_highest_address]

#ifdef WRITE_BARRIER_CHECK
        adrp     x12, $g_GCShadow
        ldr      x7, [x12, $g_GCShadow]

        adrp     x12, $g_GCShadowEnd
        ldr      x10, [x12, $g_GCShadowEnd]
#endif

        ; Update wbs state
        adrp     x12, JIT_WriteBarrier_Table_Loc
        ldr      x12, [x12, JIT_WriteBarrier_Table_Loc]
        add      x12, x12, x9
        stp      x0, x1, [x12], 16
        stp      x2, x3, [x12], 16
        stp      x4, x5, [x12], 16
        str      x6, [x12], 8
#ifdef WRITE_BARRIER_CHECK
        stp     x7, x10, [x12], 16
#endif

        EPILOG_RESTORE_REG_PAIR fp, lr, #16!
        EPILOG_RETURN

    LEAF_END JIT_UpdateWriteBarrierState

; void SinglecastDelegateInvokeStub(Delegate *pThis)
    LEAF_ENTRY SinglecastDelegateInvokeStub
        cmp     x0, #0
        beq     LNullThis

        ldr     x16, [x0, #DelegateObject___methodPtr]
        ldr     x0, [x0, #DelegateObject___target]

        br      x16

LNullThis
        mov     x0, #CORINFO_NullReferenceException_ASM
        b       JIT_InternalThrow

    LEAF_END

#ifdef FEATURE_COMINTEROP

; ------------------------------------------------------------------
; setStubReturnValue
; w0 - size of floating point return value (MetaSig::GetFPReturnSize())
; x1 - pointer to the return buffer in the stub frame
    LEAF_ENTRY setStubReturnValue

        cbz     w0, NoFloatingPointRetVal

        ;; Float return case
        cmp     x0, #4
        bne     LNoFloatRetVal
        ldr     s0, [x1]
        ret
LNoFloatRetVal

        ;; Double return case
        cmp     w0, #8
        bne     LNoDoubleRetVal
        ldr     d0, [x1]
        ret
LNoDoubleRetVal

        ;; Float HFA return case
        cmp     w0, #16
        bne     LNoFloatHFARetVal
        ldp     s0, s1, [x1]
        ldp     s2, s3, [x1, #8]
        ret
LNoFloatHFARetVal

        ;;Double HFA return case
        cmp     w0, #32
        bne     LNoDoubleHFARetVal
        ldp     d0, d1, [x1]
        ldp     d2, d3, [x1, #16]
        ret
LNoDoubleHFARetVal

        ;;Vector HVA return case
        cmp     w3, #64
        bne     LNoVectorHVARetVal
        ldp     q0, q1, [x1]
        ldp     q2, q3, [x1, #32]
        ret
LNoVectorHVARetVal

        EMIT_BREAKPOINT ; Unreachable

NoFloatingPointRetVal

        ;; Restore the return value from retbuf
        ldr     x0, [x1]
        ldr     x1, [x1, #8]
        ret

    LEAF_END

; ------------------------------------------------------------------
; GenericComPlusCallStub that erects a ComPlusMethodFrame and calls into the runtime
; (CLRToCOMWorker) to dispatch rare cases of the interface call.
;
; On entry:
;   x0          : 'this' object
;   x12         : Interface MethodDesc*
;   plus user arguments in registers and on the stack
;
; On exit:
;   x0/x1/s0-s3/d0-d3 set to return value of the call as appropriate
;
    NESTED_ENTRY GenericComPlusCallStub

        PROLOG_WITH_TRANSITION_BLOCK ASM_ENREGISTERED_RETURNTYPE_MAXSIZE

        add         x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         x1, x12                         ; pMethodDesc

        ; Call CLRToCOMWorker(TransitionBlock *, ComPlusCallMethodDesc *).
        ; This call will set up the rest of the frame (including the vfptr, the GS cookie and
        ; linking to the thread), make the client call and return with correct registers set
        ; (x0/x1/s0-s3/d0-d3 as appropriate).

        bl          CLRToCOMWorker

        ; x0 = fpRetSize

        ; The return value is stored before float argument registers
        add         x1, sp, #(__PWTB_FloatArgumentRegisters - ASM_ENREGISTERED_RETURNTYPE_MAXSIZE)
        bl          setStubReturnValue

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

    NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub called the first time a particular method is invoked.
;
; On entry:
;   x12         : ComCallMethodDesc* provided by prepad thunk
;   plus user arguments in registers and on the stack
;
; On exit:
;   tail calls to real method
;
    NESTED_ENTRY ComCallPreStub

    GBLA ComCallPreStub_FrameSize
    GBLA ComCallPreStub_StackAlloc
    GBLA ComCallPreStub_FrameOffset
    GBLA ComCallPreStub_ErrorReturnOffset
    GBLA ComCallPreStub_FirstStackAdjust

ComCallPreStub_FrameSize         SETA (SIZEOF__GSCookie + SIZEOF__ComMethodFrame)
ComCallPreStub_FirstStackAdjust  SETA (8 + SIZEOF__ArgumentRegisters + 2 * 8) ; x8, reg args , fp & lr already pushed
ComCallPreStub_StackAlloc        SETA ComCallPreStub_FrameSize - ComCallPreStub_FirstStackAdjust
ComCallPreStub_StackAlloc        SETA ComCallPreStub_StackAlloc + SIZEOF__FloatArgumentRegisters + 8; 8 for ErrorReturn
    IF ComCallPreStub_StackAlloc:MOD:16 != 0
ComCallPreStub_StackAlloc     SETA ComCallPreStub_StackAlloc + 8
    ENDIF

ComCallPreStub_FrameOffset       SETA (ComCallPreStub_StackAlloc - (SIZEOF__ComMethodFrame - ComCallPreStub_FirstStackAdjust))
ComCallPreStub_ErrorReturnOffset SETA SIZEOF__FloatArgumentRegisters

    IF (ComCallPreStub_FirstStackAdjust):MOD:16 != 0
ComCallPreStub_FirstStackAdjust     SETA ComCallPreStub_FirstStackAdjust + 8
    ENDIF

    ; Save arguments and return address
    PROLOG_SAVE_REG_PAIR           fp, lr, #-ComCallPreStub_FirstStackAdjust!
    PROLOG_STACK_ALLOC  ComCallPreStub_StackAlloc

    SAVE_ARGUMENT_REGISTERS        sp, (16+ComCallPreStub_StackAlloc)

    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 0

    str x12, [sp, #(ComCallPreStub_FrameOffset + UnmanagedToManagedFrame__m_pvDatum)]
    add x0, sp, #(ComCallPreStub_FrameOffset)
    add x1, sp, #(ComCallPreStub_ErrorReturnOffset)
    bl ComPreStubWorker

    cbz x0, ComCallPreStub_ErrorExit

    mov x12, x0

    ; pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 0
    RESTORE_ARGUMENT_REGISTERS        sp, (16+ComCallPreStub_StackAlloc)

    EPILOG_STACK_FREE ComCallPreStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, #ComCallPreStub_FirstStackAdjust!

    ; and tailcall to the actual method
    EPILOG_BRANCH_REG x12

ComCallPreStub_ErrorExit
    ldr x0, [sp, #(ComCallPreStub_ErrorReturnOffset)] ; ErrorReturn

    ; pop the stack
    EPILOG_STACK_FREE ComCallPreStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, #ComCallPreStub_FirstStackAdjust!

    EPILOG_RETURN

    NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub which sets up a ComMethodFrame and calls COMToCLRWorker.
;
; On entry:
;   x12         : ComCallMethodDesc*  provided by prepad thunk
;   plus user arguments in registers and on the stack
;
; On exit:
;   Result in x0/d0 as per the real method being called
;
    NESTED_ENTRY GenericComCallStub

    GBLA GenericComCallStub_FrameSize
    GBLA GenericComCallStub_StackAlloc
    GBLA GenericComCallStub_FrameOffset
    GBLA GenericComCallStub_FirstStackAdjust

GenericComCallStub_FrameSize         SETA (SIZEOF__GSCookie + SIZEOF__ComMethodFrame)
GenericComCallStub_FirstStackAdjust  SETA (8 + SIZEOF__ArgumentRegisters + 2 * 8)
GenericComCallStub_StackAlloc        SETA GenericComCallStub_FrameSize - GenericComCallStub_FirstStackAdjust
GenericComCallStub_StackAlloc        SETA GenericComCallStub_StackAlloc + SIZEOF__FloatArgumentRegisters

    IF (GenericComCallStub_StackAlloc):MOD:16 != 0
GenericComCallStub_StackAlloc     SETA GenericComCallStub_StackAlloc + 8
    ENDIF

GenericComCallStub_FrameOffset       SETA (GenericComCallStub_StackAlloc - (SIZEOF__ComMethodFrame - GenericComCallStub_FirstStackAdjust))

    IF (GenericComCallStub_FirstStackAdjust):MOD:16 != 0
GenericComCallStub_FirstStackAdjust     SETA GenericComCallStub_FirstStackAdjust + 8
    ENDIF


    ; Save arguments and return address
    PROLOG_SAVE_REG_PAIR           fp, lr, #-GenericComCallStub_FirstStackAdjust!
    PROLOG_STACK_ALLOC  GenericComCallStub_StackAlloc

    SAVE_ARGUMENT_REGISTERS        sp, (16+GenericComCallStub_StackAlloc)
    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 0

    str x12, [sp, #(GenericComCallStub_FrameOffset + UnmanagedToManagedFrame__m_pvDatum)]
    add x1, sp, #GenericComCallStub_FrameOffset
    bl COMToCLRWorker

    ; pop the stack
    EPILOG_STACK_FREE GenericComCallStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, #GenericComCallStub_FirstStackAdjust!

    EPILOG_RETURN

    NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub called from COMToCLRWorker that actually dispatches to the real managed method.
;
; On entry:
;   x0          : dwStackSlots, count of argument stack slots to copy
;   x1          : pFrame, ComMethodFrame pushed by GenericComCallStub above
;   x2          : pTarget, address of code to call
;   x3          : pSecretArg, hidden argument passed to target above in x12
;   x4          : pDangerousThis, managed 'this' reference
;
; On exit:
;   Result in x0/d0 as per the real method being called
;
    NESTED_ENTRY COMToCLRDispatchHelper,,CallDescrWorkerUnwindFrameChainHandler

    PROLOG_SAVE_REG_PAIR           fp, lr, #-16!

    cbz x0, COMToCLRDispatchHelper_RegSetup

    add x9, x1, #SIZEOF__ComMethodFrame

    ; Compute number of 8 bytes slots to copy. This is done by rounding up the
    ; dwStackSlots value to the nearest even value
    add x0, x0, #1
    bic x0, x0, #1

    ; Compute how many slots to adjust the address to copy from. Since we
    ; are copying 16 bytes at a time, adjust by -1 from the rounded value
    sub x6, x0, #1
    add x9, x9, x6, LSL #3

COMToCLRDispatchHelper_StackLoop
    ldp     x7, x8, [x9], #-16  ; post-index
    stp     x7, x8, [sp, #-16]! ; pre-index
    subs    x0, x0, #2
    bne     COMToCLRDispatchHelper_StackLoop

COMToCLRDispatchHelper_RegSetup

    ; We need an aligned offset for restoring float args, so do the subtraction into
    ; a scratch register
    sub     x5, x1, GenericComCallStub_FrameOffset
    RESTORE_FLOAT_ARGUMENT_REGISTERS x5, 0

    mov lr, x2
    mov x12, x3

    mov x0, x4

    ldp x2, x3, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 16)]
    ldp x4, x5, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 32)]
    ldp x6, x7, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 48)]
    ldr x8, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters - 8)]

    ldr x1, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 8)]

    blr lr

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR           fp, lr, #16!
    EPILOG_RETURN

    NESTED_END

#endif ; FEATURE_COMINTEROP

;
; x12 = UMEntryThunk*
;
    NESTED_ENTRY TheUMEntryPrestub,,UMEntryPrestubUnwindFrameChainHandler

    ; Save arguments and return address
    PROLOG_SAVE_REG_PAIR           fp, lr, #-224!
    SAVE_ARGUMENT_REGISTERS        sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 96

    mov x0, x12
    bl  TheUMEntryPrestubWorker

    ; save real target address in x12.
    mov x12, x0

    ; pop the stack and restore original register state
    RESTORE_ARGUMENT_REGISTERS        sp, 16
    RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 96
    EPILOG_RESTORE_REG_PAIR           fp, lr, #224!

    ; and tailcall to the actual method
    EPILOG_BRANCH_REG x12

    NESTED_END

#ifdef FEATURE_HIJACK
; ------------------------------------------------------------------
; Hijack function for functions which return a scalar type or a struct (value type)
    NESTED_ENTRY OnHijackTripThread
    PROLOG_SAVE_REG_PAIR   fp, lr, #-176!
    ; Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, #16
    PROLOG_SAVE_REG_PAIR   x21, x22, #32
    PROLOG_SAVE_REG_PAIR   x23, x24, #48
    PROLOG_SAVE_REG_PAIR   x25, x26, #64
    PROLOG_SAVE_REG_PAIR   x27, x28, #80

    ; save any integral return value(s)
    stp x0, x1, [sp, #96]

    ; save any FP/HFA/HVA return value(s)
    stp q0, q1, [sp, #112]
    stp q2, q3, [sp, #144]

    mov x0, sp
    bl OnHijackWorker

    ; restore any integral return value(s)
    ldp x0, x1, [sp, #96]

    ; restore any FP/HFA/HVA return value(s)
    ldp q0, q1, [sp, #112]
    ldp q2, q3, [sp, #144]

    EPILOG_RESTORE_REG_PAIR   x19, x20, #16
    EPILOG_RESTORE_REG_PAIR   x21, x22, #32
    EPILOG_RESTORE_REG_PAIR   x23, x24, #48
    EPILOG_RESTORE_REG_PAIR   x25, x26, #64
    EPILOG_RESTORE_REG_PAIR   x27, x28, #80
    EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
    EPILOG_RETURN
    NESTED_END

#endif ; FEATURE_HIJACK

;; ------------------------------------------------------------------
;; Redirection Stub for GC in fully interruptible method
        GenerateRedirectedHandledJITCaseStub GCThreadControl
;; ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub DbgThreadControl
;; ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub UserSuspend

#ifdef _DEBUG
; ------------------------------------------------------------------
; Redirection Stub for GC Stress
        GenerateRedirectedHandledJITCaseStub GCStress
#endif


; ------------------------------------------------------------------

        ; This helper enables us to call into a funclet after restoring Fp register
        NESTED_ENTRY CallEHFunclet
        ; On entry:
        ;
        ; X0 = throwable
        ; X1 = PC to invoke
        ; X2 = address of X19 register in CONTEXT record; used to restore the non-volatile registers of CrawlFrame
        ; X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        ;

        ; Using below prolog instead of PROLOG_SAVE_REG_PAIR fp,lr, #-16!
        ; is intentional. Above statement would also emit instruction to save
        ; sp in fp. If sp is saved in fp in prolog then it is not expected that fp can change in the body
        ; of method. However, this method needs to be able to change fp before calling funclet.
        ; This is required to access locals in funclet.
        PROLOG_SAVE_REG_PAIR_NO_FP fp,lr, #-96!

        ; Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, 16
        PROLOG_SAVE_REG_PAIR   x21, x22, 32
        PROLOG_SAVE_REG_PAIR   x23, x24, 48
        PROLOG_SAVE_REG_PAIR   x25, x26, 64
        PROLOG_SAVE_REG_PAIR   x27, x28, 80

        ; Save the SP of this function. We cannot store SP directly.
        mov fp, sp
        str fp, [x3]

        ldp x19, x20, [x2, #0]
        ldp x21, x22, [x2, #16]
        ldp x23, x24, [x2, #32]
        ldp x25, x26, [x2, #48]
        ldp x27, x28, [x2, #64]
        ldr fp, [x2, #80] ; offset of fp in CONTEXT relative to X19

        ; Invoke the funclet
        blr x1
        nop

        EPILOG_RESTORE_REG_PAIR   x19, x20, 16
        EPILOG_RESTORE_REG_PAIR   x21, x22, 32
        EPILOG_RESTORE_REG_PAIR   x23, x24, 48
        EPILOG_RESTORE_REG_PAIR   x25, x26, 64
        EPILOG_RESTORE_REG_PAIR   x27, x28, 80
        EPILOG_RESTORE_REG_PAIR   fp, lr, #96!
        EPILOG_RETURN

        NESTED_END CallEHFunclet

        ; This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
        ; frame pointer for accessing the locals in the parent method.
        NESTED_ENTRY CallEHFilterFunclet

        PROLOG_SAVE_REG_PAIR   fp, lr, #-16!

        ; On entry:
        ;
        ; X0 = throwable
        ; X1 = SP of the caller of the method/funclet containing the filter
        ; X2 = PC to invoke
        ; X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        ;
        ; Save the SP of this function
        str fp, [x3]
        ; Invoke the filter funclet
        blr x2

        EPILOG_RESTORE_REG_PAIR   fp, lr,   #16!
        EPILOG_RETURN

        NESTED_END CallEHFilterFunclet


        GBLA FaultingExceptionFrame_StackAlloc
        GBLA FaultingExceptionFrame_FrameOffset

FaultingExceptionFrame_StackAlloc         SETA (SIZEOF__GSCookie + SIZEOF__FaultingExceptionFrame)
FaultingExceptionFrame_FrameOffset        SETA  SIZEOF__GSCookie

        MACRO
        GenerateRedirectedStubWithFrame $STUB, $TARGET

        ;
        ; This is the primary function to which execution will be redirected to.
        ;
        NESTED_ENTRY $STUB

        ;
        ; IN: lr: original IP before redirect
        ;

        PROLOG_SAVE_REG_PAIR    fp, lr, #-16!
        PROLOG_STACK_ALLOC  FaultingExceptionFrame_StackAlloc

        ; At this point, the stack maybe misaligned if the thread abort was asynchronously
        ; triggered in the prolog or epilog of the managed method. For such a case, we must
        ; align the stack before calling into the VM.
        ;
        ; Runtime check for 16-byte alignment.
        mov x0, sp
        and x0, x0, #15
        sub sp, sp, x0

        ; Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        add x19, sp, #FaultingExceptionFrame_FrameOffset

        ; Prepare to initialize to NULL
        mov x1,#0
        str x1, [x19]                                                        ; Initialize vtbl (it is not strictly necessary)
        str x1, [x19, #FaultingExceptionFrame__m_fFilterExecuted]            ; Initialize BOOL for personality routine

        mov x0, x19       ; move the ptr to FEF in X0

        bl            $TARGET

        ; Target should not return.
        EMIT_BREAKPOINT

        NESTED_END $STUB

        MEND


; ------------------------------------------------------------------
;
; Helpers for ThreadAbort exceptions
;

        NESTED_ENTRY RedirectForThreadAbort2,,HijackHandler
        PROLOG_SAVE_REG_PAIR fp,lr, #-16!

        ; stack must be 16 byte aligned
        CHECK_STACK_ALIGNMENT

        ; On entry:
        ;
        ; x0 = address of FaultingExceptionFrame
        ;
        ; Invoke the helper to setup the FaultingExceptionFrame and raise the exception
        bl              ThrowControlForThread

        ; ThrowControlForThread doesn't return.
        EMIT_BREAKPOINT

        NESTED_END RedirectForThreadAbort2

        GenerateRedirectedStubWithFrame RedirectForThreadAbort, RedirectForThreadAbort2

; ------------------------------------------------------------------
; ResolveWorkerChainLookupAsmStub
;
; This method will perform a quick chained lookup of the entry if the
;  initial cache lookup fails.
;
; On Entry:
;   x9        contains the pointer to the current ResolveCacheElem
;   x11       contains the address of the indirection (and the flags in the low two bits)
;   x12       contains our contract the DispatchToken
; Must be preserved:
;   x0        contains the instance object ref that we are making an interface call on
;   x9        Must point to a ResolveCacheElem [For Sanity]
;  [x1-x7]    contains any additional register arguments for the interface method
;
; Loaded from x0
;   x13       contains our type     the MethodTable  (from object ref in x0)
;
; On Exit:
;   x0, [x1-x7] arguments for the interface implementation target
;
; On Exit (to ResolveWorkerAsmStub):
;   x11       contains the address of the indirection and the flags in the low two bits.
;   x12       contains our contract (DispatchToken)
;   x16,x17   will be trashed
;
    GBLA BACKPATCH_FLAG      ; two low bit flags used by ResolveWorkerAsmStub
    GBLA PROMOTE_CHAIN_FLAG  ; two low bit flags used by ResolveWorkerAsmStub
BACKPATCH_FLAG      SETA  1
PROMOTE_CHAIN_FLAG  SETA  2

    NESTED_ENTRY ResolveWorkerChainLookupAsmStub

        tst     x11, #BACKPATCH_FLAG    ; First we check if x11 has the BACKPATCH_FLAG set
        bne     Fail                    ; If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

        ldr     x13, [x0]         ; retrieve the MethodTable from the object ref in x0
MainLoop
        ldr     x9, [x9, #ResolveCacheElem__pNext]     ; x9 <= the next entry in the chain
        cmp     x9, #0
        beq     Fail

        ldp     x16, x17, [x9]
        cmp     x16, x13          ; compare our MT with the one in the ResolveCacheElem
        bne     MainLoop

        cmp     x17, x12          ; compare our DispatchToken with one in the ResolveCacheElem
        bne     MainLoop

Success
        ldr     x13, =g_dispatch_cache_chain_success_counter
        ldr     x16, [x13]
        subs    x16, x16, #1
        str     x16, [x13]
        blt     Promote

        ldr     x16, [x9, #ResolveCacheElem__target]    ; get the ImplTarget
        br      x16               ; branch to interface implementation target

Promote
                                  ; Move this entry to head position of the chain
        mov     x16, #256
        str     x16, [x13]        ; be quick to reset the counter so we don't get a bunch of contending threads
        orr     x11, x11, #PROMOTE_CHAIN_FLAG   ; set PROMOTE_CHAIN_FLAG
        mov     x12, x9           ; We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

Fail
        b       ResolveWorkerAsmStub ; call the ResolveWorkerAsmStub method to transition into the VM

    NESTED_END ResolveWorkerChainLookupAsmStub

;; ------------------------------------------------------------------
;; void ResolveWorkerAsmStub(args in regs x0-x7 & stack and possibly retbuf arg in x8, x11:IndirectionCellAndFlags, x12:DispatchToken)
;;
;; The stub dispatch thunk which transfers control to VSD_ResolveWorker.
        NESTED_ENTRY ResolveWorkerAsmStub

        PROLOG_WITH_TRANSITION_BLOCK

        add x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        and x1, x11, #-4 ; Indirection cell
        mov x2, x12 ; DispatchToken
        and x3, x11, #3 ; flag
        bl VSD_ResolveWorker
        mov x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH_REG  x9

        NESTED_END

#ifdef FEATURE_READYTORUN

    NESTED_ENTRY DelayLoad_MethodCall
    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
    mov x1, x11 ; Indirection cell
    mov x2, x9 ; sectionIndex
    mov x3, x10 ; Module*
    bl ExternalMethodFixupWorker
    mov x12, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL ExternalMethodFixupPatchLabel
    EPILOG_BRANCH_REG   x12

    NESTED_END

    MACRO
        DynamicHelper $frameFlags, $suffix

        NESTED_ENTRY DelayLoad_Helper$suffix

        PROLOG_WITH_TRANSITION_BLOCK

        add x0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov x1, x11 ; Indirection cell
        mov x2, x9 ; sectionIndex
        mov x3, x10 ; Module*
        mov x4, $frameFlags
        bl DynamicHelperWorker
        cbnz x0, %FT0
        ldr x0, [sp, #__PWTB_ArgumentRegister_FirstArg]
        EPILOG_WITH_TRANSITION_BLOCK_RETURN
0
        mov x12, x0
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG  x12
        NESTED_END
    MEND

    DynamicHelper DynamicHelperFrameFlags_Default
    DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
    DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif // FEATURE_READYTORUN

#ifdef FEATURE_COMINTEROP
; ------------------------------------------------------------------
; Function used by COM interop to get floating point return value (since it's not in the same
; register(s) as non-floating point values).
;
; On entry;
;   x0          : size of the FP result (4 or 8 bytes)
;   x1          : pointer to 64-bit buffer to receive result
;
; On exit:
;   buffer pointed to by x1 on entry contains the float or double argument as appropriate
;
    LEAF_ENTRY getFPReturn
    str d0, [x1]
    LEAF_END

; ------------------------------------------------------------------
; Function used by COM interop to set floating point return value (since it's not in the same
; register(s) as non-floating point values).
;
; On entry:
;   x0          : size of the FP result (4 or 8 bytes)
;   x1          : 32-bit or 64-bit FP result
;
; On exit:
;   s0          : float result if x0 == 4
;   d0          : double result if x0 == 8
;
    LEAF_ENTRY setFPReturn
    fmov d0, x1
    LEAF_END
#endif

;
; JIT Static access helpers when coreclr host specifies single appdomain flag
;

; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain
    ; If class is not initialized, bail to C++ helper
    add x2, x0, #DomainLocalModule__m_pDataBlob
    ldrb w2, [x2, w1]
    tst w2, #1
    beq CallHelper1

    ret lr

CallHelper1
    ; Tail call JIT_GetSharedNonGCStaticBase_Helper
    b JIT_GetSharedNonGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain
    ret lr
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain
    ; If class is not initialized, bail to C++ helper
    add x2, x0, #DomainLocalModule__m_pDataBlob
    ldrb w2, [x2, w1]
    tst w2, #1
    beq CallHelper2

    ldr x0, [x0, #DomainLocalModule__m_pGCStatics]
    ret lr

CallHelper2
    ; Tail call Jit_GetSharedGCStaticBase_Helper
    b JIT_GetSharedGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain
    ldr x0, [x0, #DomainLocalModule__m_pGCStatics]
    ret lr
    LEAF_END


; ------------------------------------------------------------------
; __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
    LEAF_ENTRY  JIT_WriteBarrier_Callable

    ; Setup args for JIT_WriteBarrier. x14 = dst ; x15 = val
    mov     x14, x0                     ; x14 = dst
    mov     x15, x1                     ; x15 = val

    ; Branch to the write barrier
    adrp    x17, JIT_WriteBarrier_Loc
    ldr     x17, [x17, JIT_WriteBarrier_Loc]
    br      x17

    LEAF_END

#ifdef PROFILING_SUPPORTED

; ------------------------------------------------------------------
; void JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
   LEAF_ENTRY  JIT_ProfilerEnterLeaveTailcallStub
   ret      lr
   LEAF_END

 #define PROFILE_ENTER    1
 #define PROFILE_LEAVE    2
 #define PROFILE_TAILCALL 4
 #define SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA 320

; ------------------------------------------------------------------
    MACRO
    GenerateProfileHelper $helper, $flags

    LCLS __HelperNakedFuncName
__HelperNakedFuncName SETS "$helper":CC:"Naked"
    IMPORT $helper

    NESTED_ENTRY $__HelperNakedFuncName
        ; On entry:
        ;   x10 = functionIDOrClientID
        ;   x11 = profiledSp
        ;   x12 = throwable
        ;
        ; On exit:
        ;   Values of x0-x8, q0-q7, fp are preserved.
        ;   Values of other volatile registers are not preserved.

        PROLOG_SAVE_REG_PAIR fp, lr, -SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA! ; Allocate space and save Fp, Pc.
        SAVE_ARGUMENT_REGISTERS sp, 16          ; Save x8 and argument registers (x0-x7).
        str     xzr, [sp, #88]                  ; Clear functionId.
        SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96    ; Save floating-point/SIMD registers (q0-q7).
        add     x12, fp, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA ; Compute probeSp - initial value of Sp on entry to the helper.
        stp     x12, x11, [sp, #224]            ; Save probeSp, profiledSp.
        str     xzr, [sp, #240]                 ; Clear hiddenArg.
        mov     w12, $flags
        stp     w12, wzr, [sp, #248]            ; Save flags and clear unused field.

        mov     x0, x10
        mov     x1, sp
        bl $helper

        RESTORE_ARGUMENT_REGISTERS sp, 16       ; Restore x8 and argument registers.
        RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 96 ; Restore floating-point/SIMD registers.

        EPILOG_RESTORE_REG_PAIR fp, lr, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA!
        EPILOG_RETURN

    NESTED_END
0

    MEND

    GenerateProfileHelper ProfileEnter, PROFILE_ENTER
    GenerateProfileHelper ProfileLeave, PROFILE_LEAVE
    GenerateProfileHelper ProfileTailcall, PROFILE_TAILCALL

#endif

#ifdef FEATURE_TIERED_COMPILATION

    IMPORT OnCallCountThresholdReached

    NESTED_ENTRY OnCallCountThresholdReachedStub
        PROLOG_WITH_TRANSITION_BLOCK

        add     x0, sp, #__PWTB_TransitionBlock ; TransitionBlock *
        mov     x1, x9 ; stub-identifying token
        bl      OnCallCountThresholdReached
        mov     x9, x0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG x9
    NESTED_END

#endif ; FEATURE_TIERED_COMPILATION

    LEAF_ENTRY  JIT_ValidateIndirectCall
        ret lr
    LEAF_END

    LEAF_ENTRY  JIT_DispatchIndirectCall
        br x9
    LEAF_END

; Must be at very end of file
    END
