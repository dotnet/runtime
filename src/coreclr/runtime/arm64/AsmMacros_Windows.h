;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;; -----------------------------------------------------------------------------
;; Macro used to assign an alternate name to a symbol containing characters normally disallowed in a symbol
;; name (e.g. C++ decorated names).
    MACRO
      SETALIAS   $name, $symbol
        GBLS    $name
$name   SETS    "|$symbol|"
    MEND

;; -----------------------------------------------------------------------------
;;
;; Macro to get a pointer to a threadlocal symbol for the currently executing thread
;;

__tls_array     equ 0x58    ;; offsetof(TEB, ThreadLocalStoragePointer)

    MACRO
        INLINE_GET_TLS_VAR $destReg, $trashReg, $variable

        EXTERN _tls_index
        EXTERN $variable

        ;; The following macro variables are just some assembler magic to get the name of the 32-bit version
        ;; of $trashReg. It does it by string manipulation. Replaces something like x3 with w3.
        LCLS TrashRegister32Bit
TrashRegister32Bit SETS "$trashReg"
TrashRegister32Bit SETS "w":CC:("$TrashRegister32Bit":RIGHT:((:LEN:TrashRegister32Bit) - 1))

        adrp        $destReg, _tls_index
        ldr         $TrashRegister32Bit, [$destReg, _tls_index]
        ldr         $destReg, [xpr, #__tls_array]
        ldr         $destReg, [$destReg, $TrashRegister32Bit uxtw #3]
        add         $destReg, $destReg, #0, lsl #0xC
        RELOC       0xA, $variable                          ;; IMAGE_REL_ARM64_SECREL_HIGH12A
        add         $destReg, $destReg, #0, lsl #0
        RELOC       0x9, $variable                          ;; IMAGE_REL_ARM64_SECREL_LOW12A
    MEND

;;-----------------------------------------------------------------------------
;; Macro for loading a 64-bit constant by a minimal number of instructions
;; Since the asssembles doesn't support 64 bit arithmetics in expressions,
;; the value is passed in as lo, hi pair.
    MACRO
        MOVL64 $Reg, $ConstantLo, $ConstantHi

        LCLS MovInstr
MovInstr SETS "movz"

         IF ((($ConstantHi):SHR:16):AND:0xffff) != 0
         $MovInstr $Reg, #((($Constant):SHR:16):AND:0xffff), lsl #48
MovInstr SETS "movk"
         ENDIF

         IF (($ConstantHi):AND:0xffff) != 0
         $MovInstr $Reg, #(($ConstantHi):AND:0xffff), lsl #32
MovInstr SETS "movk"
         ENDIF

        IF ((($ConstantLo):SHR:16):AND:0xffff) != 0
        $MovInstr $Reg, #((($ConstantLo):SHR:16):AND:0xffff), lsl #16
MovInstr SETS "movk"
        ENDIF

        $MovInstr $Reg, #(($ConstantLo):AND:0xffff)
    MEND

;;-----------------------------------------------------------------------------
;; Macro for loading a 64bit value of a global variable into a register
    MACRO
        PREPARE_EXTERNAL_VAR_INDIRECT $Name, $Reg

        adrp $Reg, $Name
        ldr  $Reg, [$Reg, $Name]
    MEND

;; ---------------------------------------------------------------------------- -
;; Macro for loading a 32bit value of a global variable into a register
    MACRO
        PREPARE_EXTERNAL_VAR_INDIRECT_W $Name, $RegNum

        adrp x$RegNum, $Name
        ldr  w$RegNum, [x$RegNum, $Name]
    MEND

;; ---------------------------------------------------------------------------- -
;;
;; Macro to add a memory barrier. Equal to __sync_synchronize().
;;

    MACRO
        InterlockedOperationBarrier

        dmb         ish
    MEND

;; -----------------------------------------------------------------------------
;;
;; Macro for indicating an alternate entry point into a function.
;;

    MACRO
        LABELED_RETURN_ADDRESS $ReturnAddressName

        ; export the return address name, but do not perturb the code by forcing alignment
$ReturnAddressName
        EXPORT $ReturnAddressName

        ; flush any pending literal pool stuff
        ROUT

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
; Macro used to check (in debug builds only) whether the stack is 16-bytes aligned (a requirement before calling
; out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
; before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
; is misaligned.
;
    MACRO
        CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        add     x9, sp, xzr
        tst     x9, #15
        beq     %F0
        EMIT_BREAKPOINT
0
#endif
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

;; GC type flags
GC_ALLOC_FINALIZE               equ 1
GC_ALLOC_ALIGN8_BIAS            equ 4
GC_ALLOC_ALIGN8                 equ 8

;-----------------------------------------------------------------------------
; The following group of macros assist in implementing prologs and epilogs for methods that set up some
; subclass of TransitionFrame. They ensure that the SP is 16-byte aligned at the conclusion of the prolog

;-----------------------------------------------------------------------------
; Define the prolog for a TransitionFrame-based method. This macro should be called first in the method and
; comprises the entire prolog (i.e. don't modify SP after calling this).The locals must be 8 byte aligned
;
; $pushCalleeSavedFloatRegs - Optional parameter. If set to PushCalleeSavedFloatRegs, the macro will also save
;                             the callee-saved floating point registers (d8-d15) to the stack. These registers
;                             are NOT restored by the EPILOG_WITH_TRANSITION_BLOCK variants.
;
    MACRO
        PROLOG_WITH_TRANSITION_BLOCK $extraLocals, $SaveFPArgs, $pushCalleeSavedFloatRegs

        GBLA __PWTB_FloatArgumentRegisters
        GBLA __PWTB_FloatCalleeSavedRegisters
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
__PWTB_FloatCalleeSavedRegisters SETA $extraLocals
        ELSE
__PWTB_FloatCalleeSavedRegisters SETA 0
        ENDIF

        IF __PWTB_FloatCalleeSavedRegisters:MOD:16 != 0
__PWTB_FloatCalleeSavedRegisters SETA __PWTB_FloatCalleeSavedRegisters + 8
        ENDIF

        ; If PushCalleeSavedFloatRegs is specified, reserve space for d8-d15 (8 registers * 8 bytes = 64 bytes)
        IF "$pushCalleeSavedFloatRegs" == "PushCalleeSavedFloatRegs"
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatCalleeSavedRegisters + 64
        ELSE
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatCalleeSavedRegisters
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

        ; Save callee-saved floating point registers if requested
        IF "$pushCalleeSavedFloatRegs" == "PushCalleeSavedFloatRegs"
        stp     d8, d9, [sp, #__PWTB_FloatCalleeSavedRegisters]
        stp     d10, d11, [sp, #(__PWTB_FloatCalleeSavedRegisters + 16)]
        stp     d12, d13, [sp, #(__PWTB_FloatCalleeSavedRegisters + 32)]
        stp     d14, d15, [sp, #(__PWTB_FloatCalleeSavedRegisters + 48)]
        ENDIF

    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and returns to caller.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        EPILOG_WITH_TRANSITION_BLOCK
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
