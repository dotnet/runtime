// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

//
// Prologue of the funclet calling helpers. They save all callee-saved registers since the funclets do not
// preserve them. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the resulting frame layout.
//
// The helpers set fp to the frame pointer of the parent frame before calling the funclet, so the frame is
// SP based rather than using PROLOG_SAVE_REG_PAIR_INDEXED fp, ra, which would set fp up as the frame pointer.
//
#define FUNCLET_CALL_FRAME_SIZE 0xE0
#define FUNCLET_CALL_LOCALS     0xC8

.macro FUNCLET_SAVE_FP_REG reg, ofs
        fsd     \reg, \ofs(sp)
        .cfi_rel_offset \reg, \ofs
.endm

.macro FUNCLET_RESTORE_FP_REG reg, ofs
        fld     \reg, \ofs(sp)
        .cfi_restore \reg
.endm

.macro FUNCLET_CALL_PROLOGUE
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED fp, ra, FUNCLET_CALL_FRAME_SIZE
        PROLOG_SAVE_REG_PAIR    s1, s2, 0x10
        PROLOG_SAVE_REG_PAIR    s3, s4, 0x20
        PROLOG_SAVE_REG_PAIR    s5, s6, 0x30
        PROLOG_SAVE_REG_PAIR    s7, s8, 0x40
        PROLOG_SAVE_REG_PAIR    s9, s10, 0x50
        PROLOG_SAVE_REG         s11, 0x60
        FUNCLET_SAVE_FP_REG     fs0, 0x68
        FUNCLET_SAVE_FP_REG     fs1, 0x70
        FUNCLET_SAVE_FP_REG     fs2, 0x78
        FUNCLET_SAVE_FP_REG     fs3, 0x80
        FUNCLET_SAVE_FP_REG     fs4, 0x88
        FUNCLET_SAVE_FP_REG     fs5, 0x90
        FUNCLET_SAVE_FP_REG     fs6, 0x98
        FUNCLET_SAVE_FP_REG     fs7, 0xA0
        FUNCLET_SAVE_FP_REG     fs8, 0xA8
        FUNCLET_SAVE_FP_REG     fs9, 0xB0
        FUNCLET_SAVE_FP_REG     fs10, 0xB8
        FUNCLET_SAVE_FP_REG     fs11, 0xC0
.endm

//
// Epilogue of the funclet calling helpers
//
.macro FUNCLET_CALL_EPILOGUE
        FUNCLET_RESTORE_FP_REG  fs11, 0xC0
        FUNCLET_RESTORE_FP_REG  fs10, 0xB8
        FUNCLET_RESTORE_FP_REG  fs9, 0xB0
        FUNCLET_RESTORE_FP_REG  fs8, 0xA8
        FUNCLET_RESTORE_FP_REG  fs7, 0xA0
        FUNCLET_RESTORE_FP_REG  fs6, 0x98
        FUNCLET_RESTORE_FP_REG  fs5, 0x90
        FUNCLET_RESTORE_FP_REG  fs4, 0x88
        FUNCLET_RESTORE_FP_REG  fs3, 0x80
        FUNCLET_RESTORE_FP_REG  fs2, 0x78
        FUNCLET_RESTORE_FP_REG  fs1, 0x70
        FUNCLET_RESTORE_FP_REG  fs0, 0x68
        EPILOG_RESTORE_REG      s11, 0x60
        EPILOG_RESTORE_REG_PAIR s9, s10, 0x50
        EPILOG_RESTORE_REG_PAIR s7, s8, 0x40
        EPILOG_RESTORE_REG_PAIR s5, s6, 0x30
        EPILOG_RESTORE_REG_PAIR s3, s4, 0x20
        EPILOG_RESTORE_REG_PAIR s1, s2, 0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, ra, FUNCLET_CALL_FRAME_SIZE
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
// INPUT:  a0:  exception object (null for finally funclets)
//         a1:  funclet address
//         a2:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         a3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: a0:  resume address for catch funclets
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

#define locThread   (FUNCLET_CALL_LOCALS)
#define locRegs     (FUNCLET_CALL_LOCALS + 8)

        sd      sp, 0(a3)

#ifdef FEATURE_NATIVEAOT
        sd      a2, locRegs(sp)

        // INLINE_GETTHREAD trashes a0.
        mv      s1, a0
        INLINE_GETTHREAD a4
        mv      a0, s1
        sd      a4, locThread(sp)

        // Clear the DoNotTriggerGc state before calling out to the managed funclet.
        addi    t3, a4, OFFSETOF__Thread__m_ThreadStateFlags
        addiw   t4, zero, ~TSF_DoNotTriggerGc
        amoand.w zero, t4, (t3)
#endif

        LOAD_FUNCLET_REGISTER a2, 8, t3
        LOAD_FUNCLET_REGISTER a2, 9, t3
        LOAD_FUNCLET_REGISTER a2, 18, t3
        LOAD_FUNCLET_REGISTER a2, 19, t3
        LOAD_FUNCLET_REGISTER a2, 20, t3
        LOAD_FUNCLET_REGISTER a2, 21, t3
        LOAD_FUNCLET_REGISTER a2, 22, t3
        LOAD_FUNCLET_REGISTER a2, 23, t3
        LOAD_FUNCLET_REGISTER a2, 24, t3
        LOAD_FUNCLET_REGISTER a2, 25, t3
        LOAD_FUNCLET_REGISTER a2, 26, t3
        LOAD_FUNCLET_REGISTER a2, 27, t3

        LOAD_FUNCLET_FP_REGISTER a2, 8
        LOAD_FUNCLET_FP_REGISTER a2, 9
        LOAD_FUNCLET_FP_REGISTER a2, 18
        LOAD_FUNCLET_FP_REGISTER a2, 19
        LOAD_FUNCLET_FP_REGISTER a2, 20
        LOAD_FUNCLET_FP_REGISTER a2, 21
        LOAD_FUNCLET_FP_REGISTER a2, 22
        LOAD_FUNCLET_FP_REGISTER a2, 23
        LOAD_FUNCLET_FP_REGISTER a2, 24
        LOAD_FUNCLET_FP_REGISTER a2, 25
        LOAD_FUNCLET_FP_REGISTER a2, 26
        LOAD_FUNCLET_FP_REGISTER a2, 27

        jalr    a1

ALTERNATE_ENTRY CallEHFunclet2

#ifdef FEATURE_NATIVEAOT
        ld      a2, locRegs(sp)
        STORE_FUNCLET_REGISTER a2, 8, t3
        STORE_FUNCLET_REGISTER a2, 9, t3
        STORE_FUNCLET_REGISTER a2, 18, t3
        STORE_FUNCLET_REGISTER a2, 19, t3
        STORE_FUNCLET_REGISTER a2, 20, t3
        STORE_FUNCLET_REGISTER a2, 21, t3
        STORE_FUNCLET_REGISTER a2, 22, t3
        STORE_FUNCLET_REGISTER a2, 23, t3
        STORE_FUNCLET_REGISTER a2, 24, t3
        STORE_FUNCLET_REGISTER a2, 25, t3
        STORE_FUNCLET_REGISTER a2, 26, t3
        STORE_FUNCLET_REGISTER a2, 27, t3

        STORE_FUNCLET_FP_REGISTER a2, 8
        STORE_FUNCLET_FP_REGISTER a2, 9
        STORE_FUNCLET_FP_REGISTER a2, 18
        STORE_FUNCLET_FP_REGISTER a2, 19
        STORE_FUNCLET_FP_REGISTER a2, 20
        STORE_FUNCLET_FP_REGISTER a2, 21
        STORE_FUNCLET_FP_REGISTER a2, 22
        STORE_FUNCLET_FP_REGISTER a2, 23
        STORE_FUNCLET_FP_REGISTER a2, 24
        STORE_FUNCLET_FP_REGISTER a2, 25
        STORE_FUNCLET_FP_REGISTER a2, 26
        STORE_FUNCLET_FP_REGISTER a2, 27

        // Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        ld      t3, locThread(sp)
        addi    t3, t3, OFFSETOF__Thread__m_ThreadStateFlags
        addiw   t4, zero, TSF_DoNotTriggerGc
        amoor.w zero, t4, (t3)
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
// INPUT:  a0:  exception object
//         a1:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         a2:  filter funclet address
//         a3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: a0:  result of the filter execution
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

        sd      sp, 0(a3)

        LOAD_FUNCLET_REGISTER a1, 8, t3

        jalr    a2

ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT
