// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro LEAF_ENTRY Name, Section
        .thumb_func
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .fnstart
.endm

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .personality C_FUNC(\Handler)
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
.endm

.macro PATCH_LABEL Name
        .thumb_func
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .fnend
.endm

.macro LEAF_END_MARKED Name, Section
        .thumb_func
        .global C_FUNC(\Name\()_End)
C_FUNC(\Name\()_End):
        LEAF_END \Name, \Section
        // make sure this symbol gets its own address
        nop
.endm

.macro NESTED_END_MARKED Name, Section
        LEAF_END_MARKED \Name, \Section
.endm

.macro GLOBAL_LABEL Name
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro ALTERNATE_ENTRY Name
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        movw \HelperReg, #:lower16:C_FUNC(\Name) - (. + 12)
        movt \HelperReg, #:upper16:C_FUNC(\Name) - (. + 8)
        add \HelperReg, pc
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
        movw \HelperReg, #:lower16:C_FUNC(\Name) - (. + 12)
        movt \HelperReg, #:upper16:C_FUNC(\Name) - (. + 8)
        add \HelperReg, pc
        ldr \HelperReg, [\HelperReg]
.endm

.macro push_nonvol_reg Register
        push \Register
        .save \Register
.endm

.macro pop_nonvol_reg Register
        pop \Register
.endm

.macro vpush_nonvol_reg Register
        vpush \Register
        .vsave \Register
.endm

.macro vpop_nonvol_reg Register
        vpop \Register
.endm

.macro alloc_stack Size
        sub sp, sp, (\Size)
        .pad #(\Size)
.endm

.macro free_stack Size
        add sp, sp, (\Size)
.endm

.macro POP_CALLEE_SAVED_REGISTERS
        pop_nonvol_reg "{r4-r11, lr}"
.endm

.macro PUSH_CALLEE_SAVED_REGISTERS
        push_nonvol_reg "{r4-r11, lr}"
.endm

.macro push_register Reg
        push \Reg
.endm

.macro push_argument_register Reg
        push_register \Reg
.endm

.macro PUSH_ARGUMENT_REGISTERS
        push {r0-r3}
        .save {r0-r3}
.endm

.macro pop_register Reg
        pop \Reg
.endm

.macro pop_argument_register Reg
        pop_register \Reg
.endm

.macro POP_ARGUMENT_REGISTERS
        pop {r0-r3}
.endm

.macro EMIT_BREAKPOINT
#ifdef __armv6__
        .inst 0xe7f001f0
#else
        .inst.w 0xde01
#endif
.endm

.macro PROLOG_PUSH RegList
        push_nonvol_reg "\RegList"
.endm

.macro PROLOG_VPUSH RegList
        vpush_nonvol_reg "\RegList"
.endm

.macro PROLOG_STACK_ALLOC Size
        sub sp, sp, #\Size
        .pad #\Size
.endm

.macro PROLOG_STACK_SAVE Register
        .setfp \Register, sp
        mov \Register, sp
.endm

.macro PROLOG_STACK_SAVE_OFFSET Register, Offset
        .setfp \Register, sp, \Offset
        add \Register, sp, \Offset
.endm

.macro EPILOG_STACK_RESTORE Register
        mov sp, \Register
.endm

.macro EPILOG_STACK_RESTORE_OFFSET Register, Offset
        sub \Register, \Offset
        mov sp, \Register
.endm

.macro EPILOG_STACK_FREE Size
        add sp, sp, #\Size
        .pad #-\Size
.endm

.macro EPILOG_BRANCH Target
        b \Target
.endm

.macro EPILOG_BRANCH_REG reg
        bx \reg
.endm

.macro EPILOG_POP RegList
        pop_nonvol_reg "\RegList"
.endm

.macro EPILOG_VPOP RegList
        vpop_nonvol_reg "\RegList"
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

// Loads a 32bit constant into destination register
.macro MOV32 DestReg, Constant
        movw \DestReg, #((\Constant) & 0xFFFF)
        movt \DestReg, #((\Constant) >> 16)
.endm

// thumb with PIC version
.macro INLINE_GET_TLS_VAR Var
        ldr     r0, 2f
1:
        add     r0, pc, r0
        bl      __tls_get_addr
        b       3f

        // Inline data
        // LLVM assembler has no concept of subsections and this is not expressible as
        // cross-section relocation.
        .p2align 2
2:
        .extern \Var
        .type \Var, tls_object
        .long  \Var(TLSGD) + (2b - 1b - 4)
3:
.endm
