// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//
// ==--==
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
        ldr                 x9, [\VASigCookieReg, #VASigCookie__pNDirectILStub]

        // if null goto stub generation
        cbz                 x9, LOCAL_LABEL(\__PInvokeStubFuncName\()_0)

        .if (\ShiftLeftAndOrSecret == 1)
            //
            // We need to distinguish between a MethodDesc* and an unmanaged target.
            // The way we do this is to shift the managed target to the left by one bit and then set the
            // least significant bit to 1.  This works because MethodDesc* are always 8-byte aligned.
            //
            lsl             \HiddenArg, \HiddenArg, #1
            orr             \HiddenArg, \HiddenArg, #1
        .endif

        EPILOG_BRANCH_REG   x9 

LOCAL_LABEL(\__PInvokeStubFuncName\()_0):

        EPILOG_BRANCH       \__PInvokeGenStubFuncName

        NESTED_END \__PInvokeStubFuncName, _TEXT

        
        NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, \SaveFPArgs

        // x2 = Umanaged Target\MethodDesc
        mov                 x2, \HiddenArg 

        // x1 = VaSigCookie
        mov                 x1, \VASigCookieReg

        // x0 = pTransitionBlock
        add                 x0, sp, #__PWTB_TransitionBlock      

        // save hidden arg
        mov                 x19, \HiddenArg 

        // save VASigCookieReg
        mov                 x20, \VASigCookieReg

        bl                  \__PInvokeStubWorkerName

        // restore VASigCookieReg
        mov                 \VASigCookieReg, x20

        // restore hidden arg (method desc or unmanaged target)
        mov                 \HiddenArg , x19


        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH       \__PInvokeStubFuncName
     
        NESTED_END \__PInvokeGenStubFuncName, _TEXT
.endm

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right 
//                         before actual InlinedCallFrame data)
//
//
    NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler
    
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
        PROLOG_SAVE_REG    x19, 16           //the stack slot at sp+24 is empty for 16 byte alignment

        PREPARE_EXTERNAL_VAR s_gsCookie, x9
        ldr     x9, [x9]
        str     x9, [x0]
        add     x19, x0, SIZEOF__GSCookie

        // x19 = pFrame
        
        // set first slot to the value of InlinedCallFrame::`vftable' (checked by runtime code)
        PREPARE_EXTERNAL_VAR _ZTV16InlinedCallFrame, x9
        add     x9, x9, 16
        str     x9, [x19]

        str     xzr, [x19, #InlinedCallFrame__m_Datum]
    
        add     x9, sp, 32
        str     x9, [x19, #InlinedCallFrame__m_pCallSiteSP]
        str     lr, [x19, #InlinedCallFrame__m_pCallerReturnAddress]

        ldr     x9, [sp,#0]
        str     x9, [x19, #InlinedCallFrame__m_pCalleeSavedFP]

        // x0 = GetThread()
        bl      C_FUNC(GetThread)
        str     x0, [x19, #InlinedCallFrame__m_pThread]

        // pFrame->m_Next = pThread->m_pFrame;
        ldr     x9, [x0, #Thread_m_pFrame]
        str     x9, [x19, #Frame__m_Next]

        // pThread->m_pFrame = pFrame;
        str     x19, [x0, #Thread_m_pFrame]

        // pThread->m_fPreemptiveGCDisabled = 0
        str     wzr, [x0, #Thread_m_fPreemptiveGCDisabled]

        EPILOG_RESTORE_REG      x19, 16    //the stack slot at sp+24 is empty for 16 byte alignment
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
        EPILOG_RETURN

    NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right 
//                         before actual InlinedCallFrame data)
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        add     x0, x0, SIZEOF__GSCookie
        ldr     x1, [x0, #InlinedCallFrame__m_pThread]

        // x0 = pFrame
        // x1 = pThread
            
        // pThread->m_fPreemptiveGCDisabled = 1
        mov     x9, 1
        str     w9, [x1, #Thread_m_fPreemptiveGCDisabled]

        // Check return trap
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, x9
        ldr     x9, [x9]
        cbnz    x9, LOCAL_LABEL(RarePath)

        // pThread->m_pFrame = pFrame->m_Next
        ldr     x9, [x0, #Frame__m_Next]
        str     x9, [x1, #Thread_m_pFrame]

        ret
            
LOCAL_LABEL(RarePath):
        b       C_FUNC(JIT_PInvokeEndRarePath)

    LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
//
// in:
// x0 = VASigCookie*
// x12 = MethodDesc *       
//
PINVOKE_STUB VarargPInvokeStub, VarargPInvokeGenILStub, VarargPInvokeStubWorker, x0, x12, 0


// ------------------------------------------------------------------
// GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
// Helper for generic pinvoke calli instruction 
//
// in:
// x15 = VASigCookie*
// x12 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker, x15, x12, 1, 1
