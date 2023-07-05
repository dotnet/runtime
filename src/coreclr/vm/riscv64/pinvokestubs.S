// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// ------------------------------------------------------------------
// Macro to generate PInvoke Stubs.
// __PInvokeStubFuncName : function which calls the actual stub obtained from VASigCookie
// __PInvokeGenStubFuncName : function which generates the IL stubs for PInvoke
//
// Params :-
// FuncPrefix : prefix of the function name for the stub
//                     Eg. VarargPinvoke, GenericPInvokeCalli
// VASigCookieReg : register which contains the VASigCookie
// SaveFPArgs : "Yes" or "No" . For varidic functions FP Args are not present in FP regs
//                        So need not save FP Args registers for vararg Pinvoke
.macro PINVOKE_STUB __PInvokeStubFuncName,__PInvokeGenStubFuncName,__PInvokeStubWorkerName,VASigCookieReg,HiddenArg,SaveFPArgs,ShiftLeftAndOrSecret=0


        NESTED_ENTRY \__PInvokeStubFuncName, _TEXT, NoHandler

        // get the stub
        ld  t0, (VASigCookie__pNDirectILStub)(\VASigCookieReg)

        // if null goto stub generation
        beq  t0, zero, \__PInvokeGenStubFuncName

        .if (\ShiftLeftAndOrSecret == 1)
            //
            // We need to distinguish between a MethodDesc* and an unmanaged target.
            // The way we do this is to shift the managed target to the left by one bit and then set the
            // least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
            //
            slli  \HiddenArg, \HiddenArg, 1
            ori  \HiddenArg, \HiddenArg, 1
        .endif

        jr  t0
        NESTED_END \__PInvokeStubFuncName, _TEXT

        NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 0, \SaveFPArgs
        .cfi_adjust_cfa_offset __PWTB_StackAlloc

        // a2 = Umanaged Target\MethodDesc
        addi  a2, \HiddenArg, 0

        // a1 = VaSigCookie
        addi  a1, \VASigCookieReg, 0

        // a0 = pTransitionBlock
        addi  a0, sp, __PWTB_TransitionBlock

        // save hidden arg
        addi  s1, \HiddenArg, 0

        // save VASigCookieReg
        addi  s2, \VASigCookieReg, 0

        call  C_FUNC(\__PInvokeStubWorkerName)

        // restore VASigCookieReg
        addi  \VASigCookieReg, s2, 0

        // restore hidden arg (method desc or unmanaged target)
        addi  \HiddenArg, s1, 0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        .cfi_adjust_cfa_offset -__PWTB_StackAlloc

        EPILOG_BRANCH       C_FUNC(\__PInvokeStubFuncName)
        NESTED_END \__PInvokeGenStubFuncName, _TEXT
.endm

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
    NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 32
    PROLOG_SAVE_REG    s1, 16           // the stack slot at sp+24 is empty for 16 byte alignment

    PREPARE_EXTERNAL_VAR  s_gsCookie, t0
    ld  t4, 0(t0)
    sd  t4, 0(a0)
    addi  s1, a0, SIZEOF__GSCookie

    // s1 = pFrame
    // set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
    PREPARE_EXTERNAL_VAR  _ZTV16InlinedCallFrame, t0
    addi  t4, t0, 16
    sd  t4, 0(s1)

    sd  zero, (InlinedCallFrame__m_Datum)(s1)

    addi  t0, sp, 32
    sd  t0, (InlinedCallFrame__m_pCallSiteSP)(s1)
    sd  ra, (InlinedCallFrame__m_pCallerReturnAddress)(s1)

    ld  t4, 0(sp)
    sd  t4, (InlinedCallFrame__m_pCalleeSavedFP)(s1)

    // v0 = GetThread()
    call  GetThreadHelper

    sd  a0, (InlinedCallFrame__m_pThread)(s1)

    // pFrame->m_Next = pThread->m_pFrame;
    ld  t4, Thread_m_pFrame(a0)
    sd  t4, Frame__m_Next(s1)

    // pThread->m_pFrame = pFrame;
    sd  s1, (Thread_m_pFrame)(a0)

    // pThread->m_fPreemptiveGCDisabled = 0
    sw  zero, (Thread_m_fPreemptiveGCDisabled)(a0)

    EPILOG_RESTORE_REG      s1, 16    //the stack slot at sp+24 is empty for 16 byte alignment
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, ra, 32
    EPILOG_RETURN

   NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

    addi  a0, a0, SIZEOF__GSCookie
    ld  a1, (InlinedCallFrame__m_pThread)(a0)
    // a0 = pFrame
    // a1 = pThread

    // pThread->m_fPreemptiveGCDisabled = 1
    ori  t4, x0, 1
    sw   t4, (Thread_m_fPreemptiveGCDisabled)(a1)

    // Check return trap
    PREPARE_EXTERNAL_VAR  g_TrapReturningThreads, t0
    lw  t4, 0(t0)
    bne  t4, zero, LOCAL_LABEL(RarePath)

    // pThread->m_pFrame = pFrame->m_Next
    ld  t4, (Frame__m_Next)(a0)
    sd  t4, (Thread_m_pFrame)(a1)

    jr  ra

LOCAL_LABEL(RarePath):
    tail  JIT_PInvokeEndRarePath

LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
// There is a separate stub when the method has a hidden return buffer arg.
//
// in:
// a0 = VASigCookie*
// t2 = MethodDesc *
//
PINVOKE_STUB VarargPInvokeStub, VarargPInvokeGenILStub, VarargPInvokeStubWorker, a0, t2, 0


// ------------------------------------------------------------------
// GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
// Helper for generic pinvoke calli instruction
//
// in:
// t3 = VASigCookie*
// t2 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker, t3, t2, 1, 1

//// ------------------------------------------------------------------
//// VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
//// Vararg PInvoke Stub when the method has a hidden return buffer arg
////
//// in:
//// a1 = VASigCookie*          //not used ???
//// t2 = MethodDesc*
////
//PINVOKE_STUB VarargPInvokeStub_RetBuffArg, VarargPInvokeGenILStub_RetBuffArg, VarargPInvokeStubWorker, a1, t8, 0
