// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

;-----------------------------------------------------------------------------
; Macro used to assign an alternate name to a symbol containing characters normally disallowed in a symbol
; name (e.g. C++ decorated names).
    MACRO
      SETALIAS   $name, $symbol
        GBLS    $name
$name   SETS    "|$symbol|"
    MEND

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
        GBLA __PWTB_ArgumentRegister_FirstArg ; We save the x8 register ahead of the first argument, so this
                                              ; is different from the start of the argument register save area.
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
__PWTB_ArgumentRegisters SETA __PWTB_StackAlloc + 104
__PWTB_ArgumentRegister_FirstArg SETA __PWTB_ArgumentRegisters + 8

        PROLOG_SAVE_REG_PAIR   fp, lr, #-176!
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
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and returns to caller.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
		EPILOG_RETURN
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
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
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

    ; make sure this symbol gets its own address
    nop

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

; Reserve 72 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
    MACRO
       SAVE_ARGUMENT_REGISTERS $reg, $offset

       GBLA __PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        str                    x8, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET)]
        stp                    x0, x1, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 8)]
        stp                    x2, x3, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 24)]
        stp                    x4, x5, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 40)]
        stp                    x6, x7, [$reg, #(__PWTB_SAVE_ARGUMENT_REGISTERS_OFFSET + 56)]

    MEND

; Reserve 128 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
    MACRO
       SAVE_FLOAT_ARGUMENT_REGISTERS $reg, $offset

       GBLA __PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        stp                    q0, q1, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET)]
        stp                    q2, q3, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 32)]
        stp                    q4, q5, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 64)]
        stp                    q6, q7, [$reg, #(__PWTB_SAVE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 96)]
    MEND

    MACRO
       RESTORE_ARGUMENT_REGISTERS $reg, $offset

       GBLA __PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        ldr                    x8, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET)]
        ldp                    x0, x1, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 8)]
        ldp                    x2, x3, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 24)]
        ldp                    x4, x5, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 40)]
        ldp                    x6, x7, [$reg, #(__PWTB_RESTORE_ARGUMENT_REGISTERS_OFFSET + 56)]

    MEND

    MACRO
       RESTORE_FLOAT_ARGUMENT_REGISTERS $reg, $offset

       GBLA __PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET

       IF "$offset" != ""
__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA $offset
       ELSE
__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET SETA 0
       ENDIF

        ldp                    q0, q1, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET)]
        ldp                    q2, q3, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 32)]
        ldp                    q4, q5, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 64)]
        ldp                    q6, q7, [$reg, #(__PWTB_RESTORE_FLOAT_ARGUMENT_REGISTERS_OFFSET + 96)]
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

;-----------------------------------------------------------------------------
; Macro to get a pointer to the Thread* object for the currently executing thread
;
__tls_array     equ 0x58    ;; offsetof(TEB, ThreadLocalStoragePointer)

    EXTERN _tls_index

    GBLS __SECTIONREL_gCurrentThreadInfo
__SECTIONREL_gCurrentThreadInfo SETS "SECTIONREL_gCurrentThreadInfo"

    MACRO
        INLINE_GETTHREAD $destReg, $trashReg

        ;; The following macro variables are just some assembler magic to get the name of the 32-bit version
        ;; of $trashReg. It does it by string manipulation. Replaces something like x3 with w3.
        LCLS TrashRegister32Bit
TrashRegister32Bit SETS "$trashReg"
TrashRegister32Bit SETS "w":CC:("$TrashRegister32Bit":RIGHT:((:LEN:TrashRegister32Bit) - 1))

        ldr         $trashReg, =_tls_index
        ldr         $TrashRegister32Bit, [$trashReg]
        ldr         $destReg, [xpr, #__tls_array]
        ldr         $destReg, [$destReg, $trashReg lsl #3]
        ldr         $trashReg, =$__SECTIONREL_gCurrentThreadInfo
        ldr         $trashReg, [$trashReg]
        ldr         $destReg, [$destReg, $trashReg]        ; return gCurrentThreadInfo.m_pThread
    MEND

;-----------------------------------------------------------------------------
; INLINE_GETTHREAD_CONSTANT_POOL macro has to be used after the last function in the .asm file that used
; INLINE_GETTHREAD. Optionally, it can be also used after any function that used INLINE_GETTHREAD
; to improve density, or to reduce distance betweeen the constant pool and its use.
;
    SETALIAS gCurrentThreadInfo, ?gCurrentThreadInfo@@3UThreadLocalInfo@@A

    MACRO
        INLINE_GETTHREAD_CONSTANT_POOL

        EXTERN $gCurrentThreadInfo

    ;; Section relocs are 32 bits. Using an extra DCD initialized to zero for 8-byte alignment.
$__SECTIONREL_gCurrentThreadInfo
        DCD $gCurrentThreadInfo
        RELOC 8, $gCurrentThreadInfo    ;; SECREL
        DCD 0

__SECTIONREL_gCurrentThreadInfo SETS "$__SECTIONREL_gCurrentThreadInfo":CC:"_"

    MEND
