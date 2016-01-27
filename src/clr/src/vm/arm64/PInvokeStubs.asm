; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;;

;;
;; ==--==
#include "ksarm64.h"

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

        PINVOKE_STUB $FuncPrefix,$VASigCookieReg,$HiddenArg,$SaveFPArgs

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

       IF "$VASigCookieReg" == "x1"
__PInvokeStubFuncName SETS "$__PInvokeStubFuncName":CC:"_RetBuffArg"
__PInvokeGenStubFuncName SETS "$__PInvokeGenStubFuncName":CC:"_RetBuffArg"
        ENDIF

        NESTED_ENTRY $__PInvokeStubFuncName

        ; get the stub
        ldr                 x9, [$VASigCookieReg, #VASigCookie__pNDirectILStub]

        ; if null goto stub generation
        cbz                 x9, %0


        EPILOG_BRANCH_REG   x9 

0
        EPILOG_BRANCH       $__PInvokeGenStubFuncName

        NESTED_END

        
        NESTED_ENTRY $__PInvokeGenStubFuncName

        PROLOG_WITH_TRANSITION_BLOCK 0, $SaveFPArgs

        ; x2 = Umanaged Target\MethodDesc
        mov                 x2, $HiddenArg 

        ; x1 = VaSigCookie
        IF "$VASigCookieReg" != "x1"
        mov                 x1, $VASigCookieReg
        ENDIF

        ; x0 = pTransitionBlock
        add                 x0, sp, #__PWTB_TransitionBlock      

        ; save hidden arg
        mov                 x19, $HiddenArg 

        bl                  $__PInvokeStubWorkerName

        ; restore hidden arg (method desc or unmanaged target)
        mov                 $HiddenArg , x19


        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        EPILOG_BRANCH       $__PInvokeStubFuncName
     
        NESTED_END
        
        MEND

    TEXTAREA

; ------------------------------------------------------------------
; VarargPInvokeStub & VarargPInvokeGenILStub
; There is a separate stub when the method has a hidden return buffer arg.
;
; in:
; x0 = VASigCookie*
; x12 = MethodDesc *       
;
        PINVOKE_STUB VarargPInvoke, x0, x12, {false}


; ------------------------------------------------------------------
; GenericPInvokeCalliHelper & GenericPInvokeCalliGenILStub
; Helper for generic pinvoke calli instruction 
;
; in:
; x15 = VASigCookie*
; x14 = Unmanaged target
;
        PINVOKE_STUB GenericPInvokeCalli, x15, x14, {true}

; ------------------------------------------------------------------
; VarargPInvokeStub_RetBuffArg & VarargPInvokeGenILStub_RetBuffArg
; Vararg PInvoke Stub when the method has a hidden return buffer arg
;
; in:
; x1 = VASigCookie*
; x12 = MethodDesc*       
; 
        PINVOKE_STUB VarargPInvoke, x1, x12, {false}


; Must be at very end of file 
        END
