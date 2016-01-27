; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

;; ==++==
;;

;;
;; ==--==
#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"


    IMPORT VarargPInvokeStubWorker
    IMPORT GenericPInvokeCalliStubWorker


; ------------------------------------------------------------------
; Macro to generate PInvoke Stubs.
; $__PInvokeStubFuncName : function which calls the actual stub obtained from VASigCookie
; $__PInvokeGenStubFuncName : function which generates the IL stubs for PInvoke
; 
; Params :-
; $FuncPrefix : prefix of the function name for the stub
;                     Eg. VarargPinvoke, GenericPInvokeCalli
; $VASigCookieReg : register which contains the VASigCookie
; $SaveFPArgs : "Yes" or "No" . For varidic functions FP Args are not present in FP regs 
;                        So need not save FP Args registers for vararg Pinvoke
        MACRO

        PINVOKE_STUB $FuncPrefix,$VASigCookieReg,$SaveFPArgs

        GBLS __PInvokeStubFuncName
        GBLS __PInvokeGenStubFuncName
        GBLS __PInvokeStubWorkerName

        IF "$FuncPrefix" == "GenericPInvokeCalli"
__PInvokeStubFuncName SETS "$FuncPrefix":CC:"Helper"
        ELSE
__PInvokeStubFuncName SETS "$FuncPrefix":CC:"Stub"
        ENDIF
__PInvokeGenStubFuncName SETS "$FuncPrefix":CC:"GenILStub"
__PInvokeStubWorkerName SETS "$FuncPrefix":CC:"StubWorker"

       IF "$VASigCookieReg" == "r1"
__PInvokeStubFuncName SETS "$__PInvokeStubFuncName":CC:"_RetBuffArg"
__PInvokeGenStubFuncName SETS "$__PInvokeGenStubFuncName":CC:"_RetBuffArg"
        ENDIF

        NESTED_ENTRY $__PInvokeStubFuncName

        ; save reg value before using the reg
        PROLOG_PUSH         {$VASigCookieReg}

        ; get the stub
        ldr                 $VASigCookieReg, [$VASigCookieReg,#VASigCookie__pNDirectILStub]

        ; if null goto stub generation
        cbz                 $VASigCookieReg, %0

        EPILOG_STACK_FREE   4

        EPILOG_BRANCH_REG   $VASigCookieReg

0

        EPILOG_POP          {$VASigCookieReg}
        EPILOG_BRANCH       $__PInvokeGenStubFuncName

        NESTED_END

        
        NESTED_ENTRY $__PInvokeGenStubFuncName

        PROLOG_WITH_TRANSITION_BLOCK 0, $SaveFPArgs

        ; r2 = UnmanagedTarget\ MethodDesc
        mov                 r2, r12

        ; r1 = VaSigCookie
        IF "$VASigCookieReg" != "r1"
        mov                 r1, $VASigCookieReg
        ENDIF

        ; r0 =  pTransitionBlock
        add                 r0, sp, #__PWTB_TransitionBlock     

        ; save hidden arg
        mov                 r4, r12

        bl                  $__PInvokeStubWorkerName

        ; restore hidden arg (method desc or unmanaged target)
        mov                 r12, r4

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH   $__PInvokeStubFuncName
     
        NESTED_END
        
        MEND


    TEXTAREA
; ------------------------------------------------------------------
; VarargPInvokeStub & VarargPInvokeGenILStub
; There is a separate stub when the method has a hidden return buffer arg.
;
; in:
; r0 = VASigCookie*
; r12 = MethodDesc *       
;
        PINVOKE_STUB VarargPInvoke, r0, {false}


; ------------------------------------------------------------------
; GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
; Helper for generic pinvoke calli instruction 
;
; in:
; r4 = VASigCookie*
; r12 = Unmanaged target
;
        PINVOKE_STUB GenericPInvokeCalli, r4, {true}

; ------------------------------------------------------------------
; VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
; Vararg PInvoke Stub when the method has a hidden return buffer arg
;
; in:
; r1 = VASigCookie*
; r12 = MethodDesc*       
; 
        PINVOKE_STUB VarargPInvoke, r1, {false}


; Must be at very end of file 
        END
