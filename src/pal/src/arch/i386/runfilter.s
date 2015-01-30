//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// Implementation of the PAL_RunFilter primitive that allows
// to run a filter guarded by a personality routine that can
// deal with nested exceptions.
//

#define ALIGN_UP(x) ((x + 15) & ~15)

#ifdef BIT64

#define SIZEOF_ARG_REGISTERS 32
#define FRAME_SIZE ALIGN_UP(SIZEOF_ARG_REGISTERS)

    .text
    .globl _PAL_RunFilter
_PAL_RunFilter:
LFB7:
    push    %rbp
LCFI0:
    mov     %rsp, %rbp
LCFI1:
    sub     $FRAME_SIZE, %rsp
    mov     %rdi, (%rsp)            // ExceptionPointers
    mov     %rsi, 8(%rsp)           // DispatcherContext
    mov     %rdx, 16(%rsp)          // pvParam
    mov     %rcx, 24(%rsp)          // pfnFilter
    
    // Filters need to be passed ExceptionPointers and pvParam arguments, in that order.
    // ExceptionPointers is already in the right register (RDI), so setup pvParam to be
    // in RSI
    mov     %rdx, %rsi     
LEHB0:
    call    *%rcx // Invoke the filter
LEHE0:
    leave
    ret
LFE7:

#else // BIT64

#define SIZEOF_ARG_REGISTERS 12
#define FRAME_SIZE ALIGN_UP(8 + SIZEOF_ARG_REGISTERS) - 8

    .text
    .globl _PAL_RunFilter
_PAL_RunFilter:
LFB7:
    pushl   %ebp
LCFI0:
    movl    %esp, %ebp
LCFI1:
    subl    $FRAME_SIZE, %esp
    movl    8(%ebp), %eax // exception pointers
    movl    %eax, (%esp)
    movl    12(%ebp), %eax // dispatcher context
    movl    %eax, 4(%esp)
    movl    16(%ebp), %eax // param
    movl    %eax, 8(%esp)
LEHB0:
    call    *20(%ebp) // filter
LEHE0:
    leave
    ret
LFE7:

#endif // BIT64 else

    .section __TEXT,__eh_frame,coalesced,no_toc+strip_static_syms+live_support
CIE_SEHFilterPersonality:
    .long   LECIE1-LSCIE1
LSCIE1:
    .long   0x0
    .byte   0x1
    .ascii "zPLR\0"
    .byte   0x1
#ifdef BIT64
    .byte   0x78        // data_align: -8
    .byte   16          // return address register: rip
#else // BIT64
    .byte   0x7c        // data_align: -4
    .byte   0x8         // return address register: eip
#endif // BIT64 else
    .byte   0x7
    .byte   0x9b
#ifdef BIT64
    .long   _PAL_SEHFilterPersonalityRoutine+4@GOTPCREL
#else // BIT64
    .long   L_PAL_SEHFilterPersonalityRoutine$non_lazy_ptr-.
#endif // BIT64 else
    .byte   0x10
    .byte   0x10
    .byte   0xc         // DW_CFA_def_cfa
#ifdef BIT64
    .byte   0x7         //   operand1 = rsp
    .byte   0x8         //   operand2 = offset 8
    .byte   0x80 | 16   // DW_CFA_offset of return address register
#else // BIT64
    .byte   0x5         //   operand1 = esp
    .byte   0x4         //   operand2 = offset 4
    .byte   0x80 | 8    // DW_CFA_offset of return address register
#endif // BIT64 else
    .byte   0x1         //   operand1 = 1 word
    .align 2
LECIE1:

    .globl _PAL_RunFilter.eh
_PAL_RunFilter.eh:
LSFDE1:
    .set    LLFDE1,LEFDE1-LASFDE1
    .set    LFL7,LFE7-LFB7
    .long   LLFDE1
LASFDE1:
    .long   LASFDE1-CIE_SEHFilterPersonality
#ifdef BIT64
    .quad   LFB7-.
    .quad   LFL7
    .byte   0x8
    .quad   0x0
#else // BIT64
    .long   LFB7-.
    .long   LFL7
    .byte   0x4
    .long   0x0
#endif // BIT64 else
    .byte   0x4         // DW_CFA_advance_loc4
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
#else // BIT64
    .byte   4           //   operand1 = ebp
#endif // BIT64
    .align 2
LEFDE1:

#ifndef BIT64

    .section __IMPORT,__pointers,non_lazy_symbol_pointers
L_PAL_SEHFilterPersonalityRoutine$non_lazy_ptr:
    .indirect_symbol _PAL_SEHFilterPersonalityRoutine
    .long   0

#endif // BIT64
