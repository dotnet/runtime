// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//; ==++==
//;

//;
//; ==--==
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

.endmacro

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
