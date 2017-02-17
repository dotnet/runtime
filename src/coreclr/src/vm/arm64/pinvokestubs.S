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
.macro PINVOKE_STUB __PInvokeStubFuncName,__PInvokeGenStubFuncName,__PInvokeStubWorkerName,VASigCookieReg,HiddenArg,SaveFPArgs


        NESTED_ENTRY \__PInvokeStubFuncName, _TEXT, NoHandler

        // get the stub
        ldr                 x9, [\VASigCookieReg, #VASigCookie__pNDirectILStub]

        // if null goto stub generation
        cbz                 x9, LOCAL_LABEL(\__PInvokeStubFuncName\()_0)


        EPILOG_BRANCH_REG   x9 

LOCAL_LABEL(\__PInvokeStubFuncName\()_0):

        EPILOG_BRANCH       \__PInvokeGenStubFuncName

        NESTED_END \__PInvokeStubFuncName, _TEXT

        
        NESTED_ENTRY \__PInvokeGenStubFuncName, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, \SaveFPArgs

        // x2 = Umanaged Target\MethodDesc
        mov                 x2, \HiddenArg 

        // x1 = VaSigCookie
        .ifnc \VASigCookieReg, x1
        mov                 x1, \VASigCookieReg
        .endif

        // x0 = pTransitionBlock
        add                 x0, sp, #__PWTB_TransitionBlock      

        // save hidden arg
        mov                 x19, \HiddenArg 

        bl                  \__PInvokeStubWorkerName

        // restore hidden arg (method desc or unmanaged target)
        mov                 \HiddenArg , x19


        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH       \__PInvokeStubFuncName
     
        NESTED_END \__PInvokeGenStubFuncName, _TEXT
.endm

// ------------------------------------------------------------------
// VarargPInvokeStub & VarargPInvokeGenILStub
// There is a separate stub when the method has a hidden return buffer arg.
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
// x14 = Unmanaged target
//
PINVOKE_STUB GenericPInvokeCalliHelper, GenericPInvokeCalliGenILStub, GenericPInvokeCalliStubWorker, x15, x14, 1

// ------------------------------------------------------------------
// VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
// Vararg PInvoke Stub when the method has a hidden return buffer arg
//
// in:
// x1 = VASigCookie*
// x12 = MethodDesc*       
// 
PINVOKE_STUB VarargPInvokeStub_RetBuffArg, VarargPInvokeGenILStub_RetBuffArg, VarargPInvokeStubWorker, x1, x12, 0
