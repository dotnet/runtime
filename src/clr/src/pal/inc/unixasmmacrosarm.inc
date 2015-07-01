//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

.macro PATCH_LABEL Name
        .thumb_func
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_ENTRY Name, Section
        .thumb_func
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END_MARKED Name, Section
        .thumb_func
        .global C_FUNC(\Name\()_End)
C_FUNC(\Name\()_End):
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        ldr \HelperReg, [pc, #C_FUNC(\Name)@GOTPCREL]
.endm

.macro push_nonvol_reg Register
        push {\Register}
        .cfi_adjust_cfa_offset 4
        .cfi_rel_offset \Register, 0
.endm

.macro pop_nonvol_reg Register
        pop {\Register}
        .cfi_adjust_cfa_offset -4
        .cfi_restore \Register
.endm

.macro alloc_stack Size
        sub sp, sp, \Size
        .cfi_adjust_cfa_offset \Size
.endm

.macro free_stack Size
        add sp, sp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro set_cfa_register Reg, Offset
        .cfi_def_cfa_register \Reg
        .cfi_def_cfa_offset \Offset
.endm

.macro save_reg_postsp Reg, Offset
        str \Reg, [sp, #\Offset]
        .cfi_rel_offset \Reg, __Offset
.endm

.macro restore_reg Reg, Offset
        ldr \Reg, [sp, #\Offset]
        .cfi_restore \Reg
.endm

.macro POP_CALLEE_SAVED_REGISTERS
        pop {r4-r11, lr}
        .cfi_adjust_cfa_offset -(4*9)
        .cfi_restore r4
        .cfi_restore r5
        .cfi_restore r6
        .cfi_restore r7
        .cfi_restore r8
        .cfi_restore r9
        .cfi_restore r10
        .cfi_restore r11
        .cfi_restore lr
.endm

.macro PUSH_CALLEE_SAVED_REGISTERS
        push {r4-r11, lr}
        .cfi_adjust_cfa_offset (4*9)
.endm

.macro push_register Reg
        push            {\Reg}
        .cfi_adjust_cfa_offset 4
.endm

.macro push_argument_register Reg
        push_register \Reg
.endm

.macro PUSH_ARGUMENT_REGISTERS
        push {r0-r3}
        .cfi_adjust_cfa_offset (4*4)
.endm

.macro pop_register Reg
        pop            {\Reg}
        .cfi_adjust_cfa_offset -4
.endm

.macro pop_argument_register Reg
        pop_register \Reg
.endm

.macro POP_ARGUMENT_REGISTERS
        pop {r0-r3}
        .cfi_adjust_cfa_offset -(4*4)
.endm

// Stack layout:
//
// (stack parameters)
// ...
// ArgumentRegisters::r3
// ArgumentRegisters::r2
// ArgumentRegisters::r1
// ArgumentRegisters::r0
// CalleeSavedRegisters::lr
// CalleeSavedRegisters::r11
// CalleeSavedRegisters::r10
// CalleeSavedRegisters::r9
// CalleeSavedRegisters::r8
// CalleeSavedRegisters::r7
// CalleeSavedRegisters::r6
// CalleeSavedRegisters::r5
// CalleeSavedRegisters::r4    <- __PWTB_StackAlloc, __PWTB_TransitionBlock
// padding to align float save area
// d7
// d6
// d5
// d4
// d3
// d2
// d1
// d0                          <- __PWTB_FloatArgumentRegisters
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, saveFpArgs = 0, pushArgRegs = 0

        __PWTB_FloatArgumentRegisters = \extraLocals
        __PWTB_SaveFPArgs = \saveFpArgs

        .if (__PWTB_SaveFPArgs == 1)
                .if ((__PWTB_FloatArgumentRegisters % 8) != 0)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 4
                .endif
                
                __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters + 8 * 8 + 8 // 8 floating point registers
        .else                
                .if ((__PWTB_FloatArgumentRegisters % 8) == 0)
                __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 4
                .endif
                
                __PWTB_TransitionBlock = __PWTB_FloatArgumentRegisters
        .endif

        __PWTB_StackAlloc = __PWTB_TransitionBlock

        .ifnc \pushArgRegs, DoNotPushArgRegs
                PUSH_ARGUMENT_REGISTERS
        .endif

        PUSH_CALLEE_SAVED_REGISTERS
        
        alloc_stack     __PWTB_StackAlloc
        
        .if (__PWTB_SaveFPArgs == 1)
                add r6, sp, #(__PWTB_FloatArgumentRegisters)
                vstm r6, {d0-d7}
        .endif
        
        CHECK_STACK_ALIGNMENT
        
        END_PROLOGUE

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        free_stack __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS
        free_stack 16
        bx lr
                
.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        .if (__PWTB_SaveFPArgs == 1)
                add r6, sp, #(__PWTB_FloatArgumentRegisters)
                vldm r6, {d0-d7}
        .endif
        
        free_stack __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS
        POP_ARGUMENT_REGISTERS        
        
.endm

.macro EMIT_BREAKPOINT
        .inst.w 0xde01
.endm

//-----------------------------------------------------------------------------
// Macro used to check (in debug builds only) whether the stack is 64-bit aligned (a requirement before calling
// out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
// before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
// is misaligned.
//
.macro CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        push {r0}
        add r0, sp, #4
        tst r0, #7
        pop {r0}
        beq 0f
        EMIT_BREAKPOINT
0:
#endif
.endm