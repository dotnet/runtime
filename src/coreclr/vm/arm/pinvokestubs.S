// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

.syntax unified
.thumb

// ------------------------------------------------------------------
// Macro to generate PInvoke Stubs.
// Params :-
// \__PInvokeStubFuncName : function which calls the actual stub obtained from VASigCookie
// \__PInvokeGenStubFuncName : function which generates the IL stubs for PInvoke
// \__PInvokeStubWorkerName : prefix of the function name for the stub
// \VASigCookieReg : register which contains the VASigCookie
// \SaveFPArgs : "1" or "0" . For varidic functions FP Args are not present in FP regs
//                        So need not save FP Args registers for vararg Pinvoke
.macro PINVOKE_STUB __PInvokeStubFuncName,__PInvokeGenStubFuncName,__PInvokeStubWorkerName,VASigCookieReg,SaveFPArgs

    NESTED_ENTRY \__PInvokeStubFuncName, _TEXT, NoHandler

        // save reg value before using the reg
        PROLOG_PUSH         {\VASigCookieReg}

        // get the stub
        ldr                 \VASigCookieReg, [\VASigCookieReg,#VASigCookie__pNDirectILStub]

        // if null goto stub generation
        cbz                 \VASigCookieReg, \__PInvokeStubFuncName\()Label

        EPILOG_STACK_FREE   4
        EPILOG_BRANCH_REG   \VASigCookieReg

\__PInvokeStubFuncName\()Label:
        EPILOG_POP          {\VASigCookieReg}
        EPILOG_BRANCH       \__PInvokeGenStubFuncName

    NESTED_END \__PInvokeStubFuncName, _TEXT


    NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, \SaveFPArgs

        // r2 = UnmanagedTarget\ MethodDesc
        mov                 r2, r12

        // r1 = VaSigCookie
        .ifnc \VASigCookieReg, r1
        mov                 r1, \VASigCookieReg
        .endif

        // r0 =  pTransitionBlock
        add                 r0, sp, #__PWTB_TransitionBlock

        // save hidden arg
        mov                 r4, r12

        bl                  \__PInvokeStubWorkerName

        // restore hidden arg (method desc or unmanaged target)
        mov                 r12, r4

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH   \__PInvokeStubFuncName

    NESTED_END \__PInvokeGenStubFuncName, _TEXT

.endm

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (r0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
    NESTED_ENTRY JIT_PInvokeBegin,_TEXT,NoHandler

        PROLOG_PUSH  "{r4, lr}"

#if defined(__clang__)
        ldr     r1, =s_gsCookie-(1f+4)
1:
        add     r1, pc
#else
        ldr     r1, =s_gsCookie
#endif
        ldr     r1, [r1]
        str     r1, [r0]
        add     r4, r0, SIZEOF__GSCookie

        // r4 = pFrame

        // set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
#if defined(__clang__)
        ldr     r1, =_ZTV16InlinedCallFrame+8-(2f+4)
2:
        add     r1, pc
#else
        ldr     r1, =_ZTV16InlinedCallFrame+8
#endif
        str     r1, [r4]

        mov     r1, 0
        str     r1, [r4, #InlinedCallFrame__m_Datum]

        add     r1, sp, 8
        str     r1, [r4, #InlinedCallFrame__m_pCallSiteSP]
        str     r11, [r4, #InlinedCallFrame__m_pCalleeSavedFP]
        str     lr, [r4, #InlinedCallFrame__m_pCallerReturnAddress]
        str     r9, [r4, #InlinedCallFrame__m_pSPAfterProlog]

        // r0 = GetThread()
        bl      C_FUNC(GetThreadHelper)
        str     r0, [r4, #InlinedCallFrame__m_pThread]

        // pFrame->m_Next = pThread->m_pFrame;
        ldr     r1, [r0, #Thread_m_pFrame]
        str     r1, [r4, #Frame__m_Next]

        // pThread->m_pFrame = pFrame;
        str     r4, [r0, #Thread_m_pFrame]

        // pThread->m_fPreemptiveGCDisabled = 0
        mov     r1, 0
        str     r1, [r0, #Thread_m_fPreemptiveGCDisabled]

        EPILOG_POP   "{r4, pc}"

    NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (r0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        add     r0, r0, SIZEOF__GSCookie
        ldr     r1, [r0, #InlinedCallFrame__m_pThread]

        // r0 = pFrame
        // r1 = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov     r2, 1
        str     r2, [r1, #Thread_m_fPreemptiveGCDisabled]

        // Check return trap
#if defined(__clang__)
        ldr     r2, =g_TrapReturningThreads-(1f+4)
1:
        add     r2, pc
#else
        ldr     r2, =g_TrapReturningThreads
#endif
        ldr     r2, [r2]
        cbnz    r2, LOCAL_LABEL(RarePath)

        // pThread->m_pFrame = pFrame->m_Next
        ldr     r2, [r0, #Frame__m_Next]
        str     r2, [r1, #Thread_m_pFrame]

        bx      lr

LOCAL_LABEL(RarePath):
        b       C_FUNC(JIT_PInvokeEndRarePath)

    LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
// There is a separate stub when the method has a hidden return buffer arg.
//
// in:
// r0 = VASigCookie*
// r12 = MethodDesc *
//
PINVOKE_STUB VarargPInvokeStub, VarargPInvokeGenILStub, VarargPInvokeStubWorker, r0, 0

// ------------------------------------------------------------------
// GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
// Helper for generic pinvoke calli instruction
//
// in:
// r4 = VASigCookie*
// r12 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker r4, 1

// ------------------------------------------------------------------
// VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
// Vararg PInvoke Stub when the method has a hidden return buffer arg
//
// in:
// r1 = VASigCookie*
// r12 = MethodDesc*
//
PINVOKE_STUB VarargPInvokeStub_RetBuffArg, VarargPInvokeGenILStub_RetBuffArg, VarargPInvokeStubWorker, r1, 0
