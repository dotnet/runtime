;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS
#endif

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
    EXTERN RhpIntegerTrashValues
    EXTERN RhpFpTrashValues
#endif ;; TRASH_SAVED_ARGUMENT_REGISTERS

;; Padding to account for the odd number of saved integer registers
#define ALIGNMENT_PADDING_SIZE (8)

#define COUNT_ARG_REGISTERS (9)
#define INTEGER_REGISTER_SIZE (8)
#define ARGUMENT_REGISTERS_SIZE (COUNT_ARG_REGISTERS * INTEGER_REGISTER_SIZE)

;; Largest return block is 4 doubles
#define RETURN_BLOCK_SIZE (32)

#define COUNT_FLOAT_ARG_REGISTERS (8)
#define FLOAT_REGISTER_SIZE (16)
#define FLOAT_ARG_REGISTERS_SIZE (COUNT_FLOAT_ARG_REGISTERS * FLOAT_REGISTER_SIZE)

#define PUSHED_LR_SIZE (8)
#define PUSHED_FP_SIZE (8)

;;
;; From CallerSP to ChildSP, the stack frame is composed of the following adjacent regions:
;;
;;      ALIGNMENT_PADDING_SIZE
;;      ARGUMENT_REGISTERS_SIZE
;;      RETURN_BLOCK_SIZE
;;      FLOAT_ARG_REGISTERS_SIZE
;;      PUSHED_LR_SIZE
;;      PUSHED_FP_SIZE
;;

#define DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK (PUSHED_FP_SIZE + PUSHED_LR_SIZE + FLOAT_ARG_REGISTERS_SIZE)

#define STACK_SIZE (ALIGNMENT_PADDING_SIZE + ARGUMENT_REGISTERS_SIZE + RETURN_BLOCK_SIZE + FLOAT_ARG_REGISTERS_SIZE + \
    PUSHED_LR_SIZE + PUSHED_FP_SIZE)

#define FLOAT_ARG_OFFSET (PUSHED_FP_SIZE + PUSHED_LR_SIZE)
#define ARGUMENT_REGISTERS_OFFSET (FLOAT_ARG_OFFSET + FLOAT_ARG_REGISTERS_SIZE + RETURN_BLOCK_SIZE)

;;
;; RhpUniversalTransition
;;
;; At input to this function, x0-8, q0-7 and the stack may contain any number of arguments.
;;
;; In addition, there are 2 extra arguments passed in the intra-procedure-call scratch register:
;;  xip0 will contain the managed function that is to be called by this transition function
;;  xip1 will contain the pointer sized extra argument to the managed function
;;
;; When invoking the callee:
;;
;;  x0 shall contain a pointer to the TransitionBlock
;;  x1 shall contain the value that was in xip1 at entry to this function
;;
;; Frame layout is:
;;
;;  {StackPassedArgs}                           ChildSP+100     CallerSP+000
;;  {AlignmentPad (0x8 bytes)}                  ChildSP+0F8     CallerSP-008
;;  {IntArgRegs (x0-x8) (0x48 bytes)}           ChildSP+0A0     CallerSP-050
;;  {ReturnBlock (0x20 bytes)}                  ChildSP+090     CallerSP-070
;;   -- The base address of the Return block is the TransitionBlock pointer, the floating point args are
;;      in the neg space of the TransitionBlock pointer.  Note that the callee has knowledge of the exact
;;      layout of all pieces of the frame that lie at or above the pushed floating point registers.
;;  {FpArgRegs (q0-q7) (0x80 bytes)}            ChildSP+010     CallerSP-0F0
;;  {PushedLR}                                  ChildSP+008     CallerSP-0F8
;;  {PushedFP}                                  ChildSP+000     CallerSP-100
;;
;; NOTE: If the frame layout ever changes, the C++ UniversalTransitionStackFrame structure
;; must be updated as well.
;;
;; NOTE: The callee receives a pointer to the base of the ReturnBlock, and the callee has
;; knowledge of the exact layout of all pieces of the frame that lie at or above the pushed
;; FpArgRegs.
;;
;; NOTE: The stack walker guarantees that conservative GC reporting will be applied to
;; everything between the base of the ReturnBlock and the top of the StackPassedArgs.
;;

    TEXTAREA

    MACRO
        UNIVERSAL_TRANSITION $FunctionName

    NESTED_ENTRY Rhp$FunctionName

        ;; FP and LR registers
        PROLOG_SAVE_REG_PAIR   fp, lr, #-STACK_SIZE!            ;; Push down stack pointer and store FP and LR

        ;; Floating point registers
        stp         q0, q1, [sp, #(FLOAT_ARG_OFFSET       )]
        stp         q2, q3, [sp, #(FLOAT_ARG_OFFSET + 0x20)]
        stp         q4, q5, [sp, #(FLOAT_ARG_OFFSET + 0x40)]
        stp         q6, q7, [sp, #(FLOAT_ARG_OFFSET + 0x60)]

        ;; Space for return buffer data (0x40 bytes)

        ;; Save argument registers
        stp         x0, x1,  [sp, #(ARGUMENT_REGISTERS_OFFSET       )]
        stp         x2, x3,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x10)]
        stp         x4, x5,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x20)]
        stp         x6, x7,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x30)]
        stp         x8, xzr, [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x40)]

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS
        ;; ARM64TODO
#endif // TRASH_SAVED_ARGUMENT_REGISTERS

        add         x0, sp, #DISTANCE_FROM_CHILDSP_TO_RETURN_BLOCK  ;; First parameter to target function is a pointer to the return block
        mov         x8, x0                                          ;; Arm64 calling convention: Address of return block shall be passed in x8
        mov         x1, xip1                                        ;; Second parameter to target function
        blr         xip0

        ;; We cannot make the label public as that tricks DIA stackwalker into thinking
        ;; it's the beginning of a method. For this reason we export an auxiliary variable
        ;; holding the address instead.
    ALTERNATE_ENTRY ReturnFrom$FunctionName

        ;; Move the result (the target address) to x12 so it doesn't get overridden when we restore the
        ;; argument registers.
        mov         x12, x0

        ;; Restore floating point registers
        ldp         q0, q1, [sp, #(FLOAT_ARG_OFFSET       )]
        ldp         q2, q3, [sp, #(FLOAT_ARG_OFFSET + 0x20)]
        ldp         q4, q5, [sp, #(FLOAT_ARG_OFFSET + 0x40)]
        ldp         q6, q7, [sp, #(FLOAT_ARG_OFFSET + 0x60)]

        ;; Restore the argument registers
        ldp         x0, x1,  [sp, #(ARGUMENT_REGISTERS_OFFSET       )]
        ldp         x2, x3,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x10)]
        ldp         x4, x5,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x20)]
        ldp         x6, x7,  [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x30)]
        ldr         x8,      [sp, #(ARGUMENT_REGISTERS_OFFSET + 0x40)]

        ;; Restore FP and LR registers, and free the allocated stack block
        EPILOG_RESTORE_REG_PAIR   fp, lr, #STACK_SIZE!

        ;; Tailcall to the target address.
        EPILOG_NOP br x12

    NESTED_END Rhp$FunctionName

    MEND

    ; To enable proper step-in behavior in the debugger, we need to have two instances
    ; of the thunk. For the first one, the debugger steps into the call in the function,
    ; for the other, it steps over it.
    UNIVERSAL_TRANSITION UniversalTransition
    UNIVERSAL_TRANSITION UniversalTransition_DebugStepTailCall

    END
