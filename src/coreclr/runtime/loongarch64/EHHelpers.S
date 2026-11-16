// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

//
// Prologue of the funclet calling helpers. They save all callee-saved registers since the funclets do not
// preserve them. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the resulting frame layout.
//
// The helpers set $fp to the frame pointer of the parent frame before calling the funclet, so the frame is
// SP based rather than using PROLOG_SAVE_REG_PAIR_INDEXED $fp, $ra, which would set $fp up as the frame pointer.
//
#define FUNCLET_CALL_FRAME_SIZE 0xB0
#define FUNCLET_CALL_LOCALS     0x98

.macro FUNCLET_SAVE_FP_REG reg, ofs
        fst.d   $f\reg, $sp, \ofs
        .cfi_rel_offset \reg+32, \ofs
.endm

.macro FUNCLET_RESTORE_FP_REG reg, ofs
        fld.d   $f\reg, $sp, \ofs
        .cfi_restore \reg+32
.endm

.macro FUNCLET_CALL_PROLOGUE
        //                                 $fp,$ra
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED 22, 1, FUNCLET_CALL_FRAME_SIZE
        //                      $s0,$s1
        PROLOG_SAVE_REG_PAIR    23, 24, 0x10
        PROLOG_SAVE_REG_PAIR    25, 26, 0x20
        PROLOG_SAVE_REG_PAIR    27, 28, 0x30
        PROLOG_SAVE_REG_PAIR    29, 30, 0x40
        PROLOG_SAVE_REG         31, 0x50
        FUNCLET_SAVE_FP_REG     24, 0x58
        FUNCLET_SAVE_FP_REG     25, 0x60
        FUNCLET_SAVE_FP_REG     26, 0x68
        FUNCLET_SAVE_FP_REG     27, 0x70
        FUNCLET_SAVE_FP_REG     28, 0x78
        FUNCLET_SAVE_FP_REG     29, 0x80
        FUNCLET_SAVE_FP_REG     30, 0x88
        FUNCLET_SAVE_FP_REG     31, 0x90
.endm

//
// Epilogue of the funclet calling helpers
//
.macro FUNCLET_CALL_EPILOGUE
        FUNCLET_RESTORE_FP_REG  31, 0x90
        FUNCLET_RESTORE_FP_REG  30, 0x88
        FUNCLET_RESTORE_FP_REG  29, 0x80
        FUNCLET_RESTORE_FP_REG  28, 0x78
        FUNCLET_RESTORE_FP_REG  27, 0x70
        FUNCLET_RESTORE_FP_REG  26, 0x68
        FUNCLET_RESTORE_FP_REG  25, 0x60
        FUNCLET_RESTORE_FP_REG  24, 0x58
        EPILOG_RESTORE_REG      31, 0x50
        EPILOG_RESTORE_REG_PAIR 29, 30, 0x40
        EPILOG_RESTORE_REG_PAIR 27, 28, 0x30
        EPILOG_RESTORE_REG_PAIR 25, 26, 0x20
        EPILOG_RESTORE_REG_PAIR 23, 24, 0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, FUNCLET_CALL_FRAME_SIZE
.endm

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// UINT_PTR CallEHFunclet(Object* pThrowable, PCODE pFunclet, void* pRegs, UINT_PTR* pFuncletCallerSP)
//
// Calls a catch or finally funclet with the preserved registers of the parent frame loaded from pRegs
// (the CONTEXT on CoreCLR, the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER
// and LOAD_FUNCLET_FP_REGISTER.
//
// On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved registers are written back
// through the REGDISPLAY after it returns since a GC during the funclet may have updated them.
//
// INPUT:  $a0:  exception object (null for finally funclets)
//         $a1:  funclet address
//         $a2:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         $a3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: $a0:  resume address for catch funclets
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

#define locThread   (FUNCLET_CALL_LOCALS)
#define locRegs     (FUNCLET_CALL_LOCALS + 8)

        st.d    $sp, $a3, 0

#ifdef FEATURE_NATIVEAOT
        st.d    $a2, $sp, locRegs

        INLINE_GETTHREAD $a4
        st.d    $a4, $sp, locThread

        // Clear the DoNotTriggerGc state before calling out to the managed funclet.
        addi.d  $t3, $a4, OFFSETOF__Thread__m_ThreadStateFlags
        addi.w  $t4, $zero, ~TSF_DoNotTriggerGc
        amand_db.w $t5, $t4, $t3
#endif

        LOAD_FUNCLET_REGISTER $a2, 22, $t3
        LOAD_FUNCLET_REGISTER $a2, 23, $t3
        LOAD_FUNCLET_REGISTER $a2, 24, $t3
        LOAD_FUNCLET_REGISTER $a2, 25, $t3
        LOAD_FUNCLET_REGISTER $a2, 26, $t3
        LOAD_FUNCLET_REGISTER $a2, 27, $t3
        LOAD_FUNCLET_REGISTER $a2, 28, $t3
        LOAD_FUNCLET_REGISTER $a2, 29, $t3
        LOAD_FUNCLET_REGISTER $a2, 30, $t3
        LOAD_FUNCLET_REGISTER $a2, 31, $t3

        LOAD_FUNCLET_FP_REGISTER $a2, 24
        LOAD_FUNCLET_FP_REGISTER $a2, 25
        LOAD_FUNCLET_FP_REGISTER $a2, 26
        LOAD_FUNCLET_FP_REGISTER $a2, 27
        LOAD_FUNCLET_FP_REGISTER $a2, 28
        LOAD_FUNCLET_FP_REGISTER $a2, 29
        LOAD_FUNCLET_FP_REGISTER $a2, 30
        LOAD_FUNCLET_FP_REGISTER $a2, 31

        jirl    $ra, $a1, 0

ALTERNATE_ENTRY CallEHFunclet2

#ifdef FEATURE_NATIVEAOT
        ld.d    $a2, $sp, locRegs
        STORE_FUNCLET_REGISTER $a2, 22, $t3
        STORE_FUNCLET_REGISTER $a2, 23, $t3
        STORE_FUNCLET_REGISTER $a2, 24, $t3
        STORE_FUNCLET_REGISTER $a2, 25, $t3
        STORE_FUNCLET_REGISTER $a2, 26, $t3
        STORE_FUNCLET_REGISTER $a2, 27, $t3
        STORE_FUNCLET_REGISTER $a2, 28, $t3
        STORE_FUNCLET_REGISTER $a2, 29, $t3
        STORE_FUNCLET_REGISTER $a2, 30, $t3
        STORE_FUNCLET_REGISTER $a2, 31, $t3

        STORE_FUNCLET_FP_REGISTER $a2, 24
        STORE_FUNCLET_FP_REGISTER $a2, 25
        STORE_FUNCLET_FP_REGISTER $a2, 26
        STORE_FUNCLET_FP_REGISTER $a2, 27
        STORE_FUNCLET_FP_REGISTER $a2, 28
        STORE_FUNCLET_FP_REGISTER $a2, 29
        STORE_FUNCLET_FP_REGISTER $a2, 30
        STORE_FUNCLET_FP_REGISTER $a2, 31

        // Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        ld.d    $t3, $sp, locThread
        addi.d  $t3, $t3, OFFSETOF__Thread__m_ThreadStateFlags
        addi.w  $t4, $zero, TSF_DoNotTriggerGc
        amor_db.w $t5, $t4, $t3
#endif

        FUNCLET_CALL_EPILOGUE
        EPILOG_RETURN

#undef locThread
#undef locRegs

NESTED_END CallEHFunclet, _TEXT

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// int CallEHFilterFunclet(Object* pThrowable, void* pRegs, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
//
// Calls a filter funclet with the frame pointer of the parent frame loaded from pRegs (the CONTEXT on CoreCLR,
// the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER.
//
// INPUT:  $a0:  exception object
//         $a1:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         $a2:  filter funclet address
//         $a3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: $a0:  result of the filter execution
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

        st.d    $sp, $a3, 0

        LOAD_FUNCLET_REGISTER $a1, 22, $t3

        jirl    $ra, $a2, 0

ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT
