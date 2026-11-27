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
