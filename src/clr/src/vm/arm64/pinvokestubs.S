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
