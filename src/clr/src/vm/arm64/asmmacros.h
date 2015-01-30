//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

;; ==++==
;;

;;
;; ==--==

;-----------------------------------------------------------------------------
; Basic extension of Assembler Macros- For Consistency

    MACRO
        EPILOG_BRANCH_REG $reg

        EPILOG_NOP br $reg
    MEND
;-----------------------------------------------------------------------------

    MACRO
        EPILOG_BRANCH $_target

        EPILOG_NOP b $_target
    MEND
;-----------------------------------------------------------------------------
; The following group of macros assist in implementing prologs and epilogs for methods that set up some
; subclass of TransitionFrame. They ensure that the SP is 16-byte aligned at the conclusion of the prolog 

;-----------------------------------------------------------------------------
; Define the prolog for a TransitionFrame-based method. This macro should be called first in the method and
; comprises the entire prolog (i.e. don't modify SP after calling this).The locals must be 8 byte aligned 
;
    MACRO
        PROLOG_WITH_TRANSITION_BLOCK $extraLocals, $SaveFPArgs

        GBLA __PWTB_FloatArgumentRegisters
        GBLA __PWTB_ArgumentRegisters 
        GBLA __PWTB_StackAlloc
        GBLA __PWTB_TransitionBlock
        GBLL __PWTB_SaveFPArgs

        IF "$SaveFPArgs" != ""
__PWTB_SaveFPArgs SETL $SaveFPArgs
        ELSE
__PWTB_SaveFPArgs SETL {true}
        ENDIF

        IF "$extraLocals" != ""
__PWTB_FloatArgumentRegisters SETA $extraLocals
        ELSE
__PWTB_FloatArgumentRegisters SETA 0
        ENDIF

        IF __PWTB_FloatArgumentRegisters:MOD:16 != 0
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatArgumentRegisters + 8
        ENDIF

        IF __PWTB_SaveFPArgs
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters + SIZEOF__FloatArgumentRegisters  
        ELSE
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters
        ENDIF

__PWTB_StackAlloc SETA __PWTB_TransitionBlock
__PWTB_ArgumentRegisters SETA __PWTB_StackAlloc + 96 

        PROLOG_SAVE_REG_PAIR   fp, lr, #-160!
        ; Spill callee saved registers 
        PROLOG_SAVE_REG_PAIR   x19, x20, #16
        PROLOG_SAVE_REG_PAIR   x21, x22, #32
        PROLOG_SAVE_REG_PAIR   x23, x24, #48
        PROLOG_SAVE_REG_PAIR   x25, x26, #64
        PROLOG_SAVE_REG_PAIR   x27, x28, #80
       
        ; Allocate space for the rest of the frame
        PROLOG_STACK_ALLOC  __PWTB_StackAlloc
        
        ; Spill argument registers.
        SAVE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

        IF __PWTB_SaveFPArgs
        SAVE_FLOAT_ARGUMENT_REGISTERS  sp, __PWTB_FloatArgumentRegisters 
        ENDIF

    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and ends by preparing for tail-calling.
; Since this is a tail call argument registers are restored.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        IF __PWTB_SaveFPArgs 
        RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, __PWTB_FloatArgumentRegisters
        ENDIF

        RESTORE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters
        EPILOG_STACK_FREE                 __PWTB_StackAlloc
        
        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #160!
    MEND

;-----------------------------------------------------------------------------
; Macro used to end a function with explicit _End label
    MACRO
    LEAF_END_MARKED $FuncName

    LCLS __EndLabelName
__EndLabelName SETS "$FuncName":CC:"_End"
    EXPORT $__EndLabelName
$__EndLabelName

    LEAF_END $FuncName

    MEND
;-----------------------------------------------------------------------------
; Macro use for enabling C++ to know where to patch code at runtime.
    MACRO
    PATCH_LABEL $FuncName
$FuncName
    EXPORT $FuncName

    MEND

;-----------------------------------------------------------------------------
; The Following sets of SAVE_*_REGISTERS expect the memory to be reserved and 
; base address to be passed in $reg
;

; Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
    MACRO
       SAVE_ARGUMENT_REGISTERS $reg, $offset 

       GBLA __PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        stp                    x0, x1, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET)]
        stp                    x2, x3, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 16)]
        stp                    x4, x5, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 32)]
        stp                    x6, x7, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 48)]
    MEND

; Reserve 64 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
    MACRO
       SAVE_FLOAT_ARGUMENT_REGISTERS $reg, $offset 

       GBLA __PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        stp                    d0, d1, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET)]
        stp                    d2, d3, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 16)]
        stp                    d4, d5, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 32)]
        stp                    d6, d7, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 48)]
    MEND

    MACRO
       RESTORE_ARGUMENT_REGISTERS $reg, $offset 

       GBLA __PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        ldp                    x0, x1, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET)]
        ldp                    x2, x3, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 16)]
        ldp                    x4, x5, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 32)]
        ldp                    x6, x7, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 48)]
    MEND

    MACRO
       RESTORE_FLOAT_ARGUMENT_REGISTERS $reg, $offset 

       GBLA __PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        ldp                    d0, d1, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET)]
        ldp                    d2, d3, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 16)]
        ldp                    d4, d5, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 32)]
        ldp                    d6, d7, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 48)]
    MEND

; ------------------------------------------------------------------
; Macro to generate Redirection Stubs
;
; $reason : reason for redirection
;                     Eg. GCThreadControl
; NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
; This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
        MACRO
        GenerateRedirectedHandledJITCaseStub $reason

        GBLS __RedirectionStubFuncName
        GBLS __RedirectionStubEndFuncName
        GBLS __RedirectionFuncName
__RedirectionStubFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_Stub"
__RedirectionStubEndFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_StubEnd"
__RedirectionFuncName SETS "|?RedirectedHandledJITCaseFor":CC:"$reason":CC:"@Thread@@CAXXZ|"

        IMPORT $__RedirectionFuncName

        NESTED_ENTRY $__RedirectionStubFuncName
        PROLOG_SAVE_REG_PAIR    fp, lr, #-16!    
        sub sp, sp, #16                          ; stack slot for CONTEXT * and padding 

        ;REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h and is used in GetCONTEXTFromRedirectedStubStackFrame
        ;If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        ; Stack alignment. This check is necessary as this function can be
        ; entered before complete execution of the prolog of another function.
        and x8, fp, #15
        sub sp, sp, x8


        ;
        ; Save a copy of the redirect CONTEXT*.
        ; This is needed for the debugger to unwind the stack.
        ;
        bl GetCurrentSavedRedirectContext
        str x0, [sp]

        ;
        ; Fetch the interrupted pc and save it as our return address.
        ;
        ldr x1, [x0, #CONTEXT_Pc]
        str x1, [fp, #8]

        ;
        ; Call target, which will do whatever we needed to do in the context
        ; of the target thread, and will RtlRestoreContext when it is done.
        ;
        bl $__RedirectionFuncName

        EMIT_BREAKPOINT ; Unreachable

; Put a label here to tell the debugger where the end of this function is.
$__RedirectionStubEndFuncName
        EXPORT $__RedirectionStubEndFuncName

        NESTED_END

        MEND
    
