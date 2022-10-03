; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"

#include "asmconstants.h"

#include "asmmacros.h"


    IMPORT VarargPInvokeStubWorker
    IMPORT GenericPInvokeCalliStubWorker
    IMPORT JIT_PInvokeEndRarePath

    IMPORT s_gsCookie
    IMPORT g_TrapReturningThreads

    SETALIAS InlinedCallFrame_vftable, ??_7InlinedCallFrame@@6B@
    IMPORT $InlinedCallFrame_vftable


; ------------------------------------------------------------------
; Macro to generate PInvoke Stubs.
; $__PInvokeStubFuncName : function which calls the actual stub obtained from VASigCookie
; $__PInvokeGenStubFuncName : function which generates the IL stubs for PInvoke
;
; Params :-
; $FuncPrefix : prefix of the function name for the stub
;                     Eg. VarargPinvoke, GenericPInvokeCalli
; $VASigCookieReg : register which contains the VASigCookie
; $SaveFPArgs : "Yes" or "No" . For varidic functions FP Args are not present in FP regs
;                        So need not save FP Args registers for vararg Pinvoke
        MACRO

        PINVOKE_STUB $FuncPrefix,$VASigCookieReg,$HiddenArg,$SaveFPArgs

        GBLS __PInvokeStubFuncName
        GBLS __PInvokeGenStubFuncName
        GBLS __PInvokeStubWorkerName

        IF "$FuncPrefix" == "GenericPInvokeCalli"
__PInvokeStubFuncName SETS "$FuncPrefix":CC:"Helper"
        ELSE
__PInvokeStubFuncName SETS "$FuncPrefix":CC:"Stub"
        ENDIF
__PInvokeGenStubFuncName SETS "$FuncPrefix":CC:"GenILStub"
__PInvokeStubWorkerName SETS "$FuncPrefix":CC:"StubWorker"

        NESTED_ENTRY $__PInvokeStubFuncName

        ; get the stub
        ldr                 x9, [$VASigCookieReg, #VASigCookie__pNDirectILStub]

        ; if null goto stub generation
        cbz                 x9, %0

        IF "$FuncPrefix" == "GenericPInvokeCalli"
            ;
            ; We need to distinguish between a MethodDesc* and an unmanaged target.
            ; The way we do this is to shift the managed target to the left by one bit and then set the
            ; least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
            ;
            lsl             $HiddenArg, $HiddenArg, #1
            orr             $HiddenArg, $HiddenArg, #1
        ENDIF

        EPILOG_BRANCH_REG   x9

0
        EPILOG_BRANCH       $__PInvokeGenStubFuncName

        NESTED_END


        NESTED_ENTRY $__PInvokeGenStubFuncName

        PROLOG_WITH_TRANSITION_BLOCK 0, $SaveFPArgs

        ; x2 = Umanaged Target\MethodDesc
        mov                 x2, $HiddenArg

        ; x1 = VaSigCookie
        mov                 x1, $VASigCookieReg

        ; x0 = pTransitionBlock
        add                 x0, sp, #__PWTB_TransitionBlock

        ; save hidden arg
        mov                 x19, $HiddenArg

        ; save VASigCookieReg
        mov                 x20, $VASigCookieReg

        bl                  $__PInvokeStubWorkerName

        ; restore VASigCookieReg
        mov                 $VASigCookieReg, x20

        ; restore hidden arg (method desc or unmanaged target)
        mov                 $HiddenArg, x19


        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH       $__PInvokeStubFuncName

        NESTED_END

        MEND


    TEXTAREA

; ------------------------------------------------------------------
; JIT_PInvokeBegin helper
;
; in:
; x0 = InlinedCallFrame*
;
        LEAF_ENTRY JIT_PInvokeBegin

            ldr     x9, =s_gsCookie
            ldr     x9, [x9]
            str     x9, [x0]
            add     x10, x0, SIZEOF__GSCookie

            ;; set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
            ldr     x9, =$InlinedCallFrame_vftable
            str     x9, [x10]

            str     xzr, [x10, #InlinedCallFrame__m_Datum]

            mov     x9, sp
            str     x9, [x10, #InlinedCallFrame__m_pCallSiteSP]
            str     fp, [x10, #InlinedCallFrame__m_pCalleeSavedFP]
            str     lr, [x10, #InlinedCallFrame__m_pCallerReturnAddress]

            ;; x0 = GetThread(), TRASHES x9
            INLINE_GETTHREAD x0, x9

            ;; pFrame->m_Next = pThread->m_pFrame;
            ldr     x9, [x0, #Thread_m_pFrame]
            str     x9, [x10, #Frame__m_Next]

            ;; pThread->m_pFrame = pFrame;
            str     x10, [x0, #Thread_m_pFrame]

            ;; pThread->m_fPreemptiveGCDisabled = 0
            str     wzr, [x0, #Thread_m_fPreemptiveGCDisabled]

            ret

        LEAF_END

; ------------------------------------------------------------------
; JIT_PInvokeEnd helper
;
; in:
; x0 = InlinedCallFrame*
;
        LEAF_ENTRY JIT_PInvokeEnd

            add     x0, x0, SIZEOF__GSCookie

            ;; x1 = GetThread(), TRASHES x2
            INLINE_GETTHREAD x1, x2

            ;; x0 = pFrame
            ;; x1 = pThread

            ;; pThread->m_fPreemptiveGCDisabled = 1
            mov     x9, 1
            str     w9, [x1, #Thread_m_fPreemptiveGCDisabled]

            ;; Check return trap
            ldr     x9, =g_TrapReturningThreads
            ldr     w9, [x9]
            cbnz    w9, RarePath

            ;; pThread->m_pFrame = pFrame->m_Next
            ldr     x9, [x0, #Frame__m_Next]
            str     x9, [x1, #Thread_m_pFrame]

            ret

RarePath
            b       JIT_PInvokeEndRarePath

        LEAF_END

        INLINE_GETTHREAD_CONSTANT_POOL

; ------------------------------------------------------------------
; VarargPInvokeStub & VarargPInvokeGenILStub
;
; in:
; x0 = VASigCookie*
; x12 = MethodDesc *
;
        PINVOKE_STUB VarargPInvoke, x0, x12, {false}


; ------------------------------------------------------------------
; GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
; Helper for generic pinvoke calli instruction
;
; in:
; x15 = VASigCookie*
; x12 = Unmanaged target
;
        PINVOKE_STUB GenericPInvokeCalli, x15, x12, {true}


; Must be at very end of file
        END
