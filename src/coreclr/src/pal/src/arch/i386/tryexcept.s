//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// ==++==
//

// ==--==
//
// Implementation of the PAL_TryExcept primitive for MSVC-style
// exception handling.
//

#define ALIGN_UP(x) ((x + 15) & ~15)

#ifdef BIT64

// GCC follows the AMD64 ABI calling convention which is considerably different from the Microsoft AMD64 calling convention.
//
// With MSVC, the first four arguments are passed in RCX, RDX, R8, R9 and the remaining on the stack.
// With GCC, the first six arguments are passed in RDI, RSI, RDX, RCX, R8, R9 and the remaining on the stack.

// => Size of the total number of arguments PAL_TryExcept takes (32 bytes) + 
// => 1 stack slot (8 bytes) to preserve "actions" flags when PAL_SEHPersonalityRoutine is invoked during unwind copies
// data to the stack location before fixing the context to invoke PAL_CallRunHandler +
// => 1 stack slot (8 bytes) to ensure stack is 16bytes aligned.
//
// Hence, the 48 bytes frame size.
#define SIZEOF_ARG_REGISTERS 48
#define FRAME_SIZE ALIGN_UP(SIZEOF_ARG_REGISTERS)

    .text
    .globl _PAL_TryExcept
_PAL_TryExcept:
LFB7:
    push    %rbp
LCFI0:
    mov     %rsp, %rbp
LCFI1:
    sub     $FRAME_SIZE, %rsp
    mov     %rdi, (%rsp) // Move the Body address to the stack
    mov     %rsi, 8(%rsp) // Move the Filter address to the stack
    mov     %rdx, 16(%rsp) // Move pvParam (i.e. HandlerData) to the stack
    mov     %rcx, 24(%rsp) // Move pfExecuteHandler value to the stack
    mov     %rdi, %r9     // Move the body address to r9
    mov     %rdx, %rdi   // Move the HandlerData argument to RDI - this will serve as the first (and only) argument passed to the __try block body below
LEHB0:
    call    *%r9 // ..and invoke the body of the __try block
LEHE0:
    xor     %rax, %rax // NULL, meaning "do not run handler"
    jmp     Lepilog
    .globl  _PAL_CallRunHandler
_PAL_CallRunHandler:
    // Note: First two args (actions and exceptionObject) have already been 
    //       setup by PAL_SEHPersonalityRoutine's cleanup phase handling. They are at
    //       RSP+32 and RSP+48 respectively.
    // 
    // Prepare the arguments to be passed to PAL_RunHandler.
    mov     32(%rsp), %rdi // actions
    mov     40(%rsp), %rsi // exceptionObject
    mov     8(%rsp), %rdx // filter
    mov     16(%rsp), %rcx // param
    mov     24(%rsp), %r8 // pfExecuteHandler
    call    _PAL_RunHandler
Lepilog:
    leave
    ret
LFE7:

#else // BIT64

#define SIZEOF_ARG_REGISTERS 20
#define FRAME_SIZE ALIGN_UP(8 + SIZEOF_ARG_REGISTERS) - 8

    .text
    .globl _PAL_TryExcept
_PAL_TryExcept:
LFB7:
    pushl   %ebp
LCFI0:
    movl    %esp, %ebp
LCFI1:
    subl    $FRAME_SIZE, %esp
    movl    16(%ebp), %eax // param
    movl    %eax, (%esp)
LEHB0:
    call    *8(%ebp) // body
LEHE0:
    xor     %eax, %eax // NULL, meaning "do not run handler"
    jmp     Lepilog
    .globl  _PAL_CallRunHandler
_PAL_CallRunHandler:
    // note: first two args already set when we get here
    mov     12(%ebp), %eax // filter
    mov     %eax, 8(%esp)
    mov     16(%ebp), %eax // param
    mov     %eax, 12(%esp)
    mov     20(%ebp), %eax // pfExecuteHandler
    mov     %eax, 16(%esp)
    call    L_PAL_RunHandler$stub
Lepilog:
    leave
    ret
LFE7:

#endif // BIT64 else

    .section __TEXT,__eh_frame,coalesced,no_toc+strip_static_syms+live_support
CIE_SEHPersonality:
    .long   LECIE1-LSCIE1
LSCIE1:
    .long   0x0
    .byte   0x1
 #ifdef BIT64
    .ascii "zPLR\0"
    .byte   0x1
    .byte   0x78        // data_align: -8
    .byte   16          // return address register: rip
    .byte   0x7
    .byte   0x9b
    .long   _PAL_SEHPersonalityRoutine+4@GOTPCREL
    .byte   0x10
    .byte   0x10
    .byte   0xc         // DW_CFA_def_cfa
    .byte   0x7         //   operand1 = rsp
    .byte   0x8         //   operand2 = offset 8
    .byte   0x80 | 16   // DW_CFA_offset of return address register
    .byte   0x1         //   operand1 = 1 word
    .align 2
#else // BIT64
    .ascii "zPLR\0"
    .byte   0x1
    .byte   0x7c        // data_align: -4
    .byte   0x8         // return address register: eip
    .byte   0x7
    .byte   0x9b
   .long   L_PAL_SEHPersonalityRoutine$non_lazy_ptr-.
    .byte   0x10
    .byte   0x10
    .byte   0xc         // DW_CFA_def_cfa
    .byte   0x5         //   operand1 = esp
    .byte   0x4         //   operand2 = offset 4
    .byte   0x80 | 8    // DW_CFA_offset of return address register
    .byte   0x1         //   operand1 = 1 word
    .align 2
#endif // BIT64 else
LECIE1:

    .globl _PAL_TryExcept.eh
_PAL_TryExcept.eh:
LSFDE1:
    .set    LLFDE1,LEFDE1-LASFDE1
    .set    LFL7,LFE7-LFB7
    .long   LLFDE1
LASFDE1:
    .long   LASFDE1-CIE_SEHPersonality
#ifdef BIT64
    .quad   LFB7-.
    .quad   LFL7
    .byte   0x8
    .quad   0x0
    .byte   0x4         // DW_CFA_advance_loc4
#else // BIT64
    .long   LFB7-.
    .long   LFL7
    .byte   0x4
    .long   0x0
    .byte   0x4         // DW_CFA_advance_loc4
#endif // BIT64 else
    .long   LCFI0-LFB7
    .byte   0xe         // DW_CFA_def_cfa_offset
#ifdef BIT64
    .byte   0x10
    .byte   0x80 | 6    // DW_CFA_offset rbp
#else // BIT64
    .byte   0x8
    .byte   0x80 | 4    // DW_CFA_offset ebp
#endif // BIT64 else
    .byte   0x2
    .byte   0x4         // DW_CFA_advance_loc4
    .long   LCFI1-LCFI0
    .byte   0xd         // DW_CFA_def_cfa_register
#ifdef BIT64
    .byte   6           //   operand1 = rbp
    .align 2
#else // BIT64
    .byte   4           //   operand1 = ebp
    .align 2
#endif // BIT64

LEFDE1:

#ifndef BIT64

    .section __IMPORT,__jump_table,symbol_stubs,self_modifying_code+pure_instructions,5
L_PAL_RunHandler$stub:
    .indirect_symbol _PAL_RunHandler
    hlt ; hlt ; hlt ; hlt ; hlt

    .section __IMPORT,__pointers,non_lazy_symbol_pointers
L_PAL_SEHPersonalityRoutine$non_lazy_ptr:
    .indirect_symbol _PAL_SEHPersonalityRoutine
    .long   0

#endif // !BIT64 

