// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// #error "TODO-RISCV64: missing implementation"

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
        // TODO RISCV64 NYI
            sw ra, 0(zero)
        NESTED_END \__PInvokeStubFuncName, _TEXT


        NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler
        // TODO RISCV64 NYI
            sw ra, 0(zero)
        NESTED_END \__PInvokeGenStubFuncName, _TEXT
.endm

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
    NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler
    // TODO RISCV64 NYI
        sw ra, 0(zero)
    NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT
    // TODO RISCV64 NYI
        sw ra, 0(zero)
    LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
//
PINVOKE_STUB VarargPInvokeStub, VarargPInvokeGenILStub, VarargPInvokeStubWorker, a0, t2, 1, 1 


// ------------------------------------------------------------------
// GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
// Helper for generic pinvoke calli instruction
//
// in:
// x15 = VASigCookie*
// x12 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker, t3, t2, 1, 1
