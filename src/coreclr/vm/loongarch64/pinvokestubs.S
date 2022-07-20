// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// ------------------------------------------------------------------
// Macro to generate PInvoke Stubs.
// $__PInvokeStubFuncName : function which calls the actual stub obtained from VASigCookie
// $__PInvokeGenStubFuncName : function which generates the IL stubs for PInvoke
//
// Params :-
// $FuncPrefix : prefix of the function name for the stub
//                     Eg. VarargPinvoke, GenericPInvokeCalli
// $VASigCookieReg : register which contains the VASigCookie
// $SaveFPArgs : "Yes" or "No" . For varidic functions FP Args are not present in FP regs
//                        So need not save FP Args registers for vararg Pinvoke

.macro PINVOKE_STUB __PInvokeStubFuncName,__PInvokeGenStubFuncName,__PInvokeStubWorkerName,VASigCookieReg,HiddenArg,SaveFPArgs,ShiftLeftAndOrSecret=0

        NESTED_ENTRY \__PInvokeStubFuncName, _TEXT, NoHandler

        // get the stub
        ld.d   $t0, \VASigCookieReg, VASigCookie__pNDirectILStub

        // if null goto stub generation
        beq  $t0, $zero, \__PInvokeGenStubFuncName

        .if (\ShiftLeftAndOrSecret == 1)
            //
            // We need to distinguish between a MethodDesc* and an unmanaged target.
            // The way we do this is to shift the managed target to the left by one bit and then set the
            // least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
            //
            slli.d    \HiddenArg, \HiddenArg, 1
            ori       \HiddenArg, \HiddenArg, 1
        .endif

        jirl  $r0, $t0, 0

        NESTED_END \__PInvokeStubFuncName, _TEXT

        NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 0, \SaveFPArgs

        // $a2 = Umanaged Target\MethodDesc
        ori  $a2, \HiddenArg, 0

        // $a1 = VaSigCookie
        ori  $a1, \VASigCookieReg, 0

        // $a0 = pTransitionBlock
        addi.d  $a0, $sp, __PWTB_TransitionBlock

        // save hidden arg
        ori  $s0, \HiddenArg, 0

        // save VASigCookieReg
        ori  $s1, \VASigCookieReg, 0

        bl  C_FUNC(\__PInvokeStubWorkerName)

        // restore VASigCookieReg
        ori  \VASigCookieReg, $s1, 0

        // restore hidden arg (method desc or unmanaged target)
        ori  \HiddenArg, $s0, 0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH       C_FUNC(\__PInvokeStubFuncName)

        NESTED_END \__PInvokeGenStubFuncName, _TEXT
.endm

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame ($a0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 32
    //                $s0=23.
    PROLOG_SAVE_REG    23, 16           //the stack slot at $sp+24 is empty for 16 byte alignment

    PREPARE_EXTERNAL_VAR  s_gsCookie, $t0
    ld.d  $t4, $t0, 0
    st.d  $t4, $a0, 0
    addi.d  $s0, $a0, SIZEOF__GSCookie

    // s0 = pFrame
    // set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
    PREPARE_EXTERNAL_VAR  _ZTV16InlinedCallFrame, $t0
    addi.d  $t4, $t0, 16
    st.d  $t4, $s0, 0

    st.d  $zero, $s0, InlinedCallFrame__m_Datum

    addi.d  $t0, $sp, 32
    st.d  $t0, $s0, InlinedCallFrame__m_pCallSiteSP
    st.d  $ra, $s0, InlinedCallFrame__m_pCallerReturnAddress

    ld.d  $t4, $sp, 0
    st.d  $t4, $s0, InlinedCallFrame__m_pCalleeSavedFP

    // v0 = GetThread()
    bl  GetThreadHelper

    st.d  $v0, $s0, InlinedCallFrame__m_pThread

    // pFrame->m_Next = pThread->m_pFrame;
    ld.d  $t4, $v0, Thread_m_pFrame
    st.d  $t4, $s0, Frame__m_Next

    // pThread->m_pFrame = pFrame;
    st.d  $s0, $v0, Thread_m_pFrame

    // pThread->m_fPreemptiveGCDisabled = 0
    st.w     $zero, $v0, Thread_m_fPreemptiveGCDisabled

    EPILOG_RESTORE_REG      23, 16    //the stack slot at $sp+24 is empty for 16 byte alignment
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 32
    EPILOG_RETURN

NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame ($a0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

    addi.d  $a0, $a0, SIZEOF__GSCookie
    ld.d      $a1, $a0, InlinedCallFrame__m_pThread
    // $a0 = pFrame
    // $a1 = pThread

    // pThread->m_fPreemptiveGCDisabled = 1
    ori  $t4, $r0, 1
    st.w   $t4, $a1, Thread_m_fPreemptiveGCDisabled

    // Check return trap
    PREPARE_EXTERNAL_VAR  g_TrapReturningThreads, $t0
    ld.w  $t4, $t0, 0
    bne  $t4, $zero, LOCAL_LABEL(RarePath)

    // pThread->m_pFrame = pFrame->m_Next
    ld.d  $t4, $a0, Frame__m_Next
    st.d  $t4, $a1, Thread_m_pFrame

    jirl  $r0, $ra, 0

LOCAL_LABEL(RarePath):
    b  JIT_PInvokeEndRarePath

LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
// There is a separate stub when the method has a hidden return buffer arg.
//
// in:
// $a0 = VASigCookie*
// $t2 = MethodDesc *
//
PINVOKE_STUB VarargPInvokeStub, VarargPInvokeGenILStub, VarargPInvokeStubWorker, $a0, $t2, 0


// ------------------------------------------------------------------
// GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
// Helper for generic pinvoke calli instruction
//
// in:
// $t3 = VASigCookie*
// $t2 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker, $t3, $t2, 1, 1

//// ------------------------------------------------------------------
//// VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
//// Vararg PInvoke Stub when the method has a hidden return buffer arg
////
//// in:
//// $a1 = VASigCookie*          //not used ???
//// $t2 = MethodDesc*
////
//PINVOKE_STUB VarargPInvokeStub_RetBuffArg, VarargPInvokeGenILStub_RetBuffArg, VarargPInvokeStubWorker, $a1, t8, 0
