//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// ==++==
//

// ==--==
//
// Implementation of the PAL_DispatchExceptionWrapper that is
// interposed between a function that caused a hardware fault
// and PAL_DispatchException that throws an SEH exception for
// the fault, to make the stack unwindable.
//
// On Mac OS X 10.6, the unwinder fails to operate correctly
// on our original int3; int3 body. The workaround is to
// increase the size of the function to include a call statement,
// even though it will never be executed.

#if defined(__x86_64__)
#define PAL_DISPATCHEXCEPTION __Z21PAL_DispatchExceptionmmmmmmP8_CONTEXTP17_EXCEPTION_RECORD
#else //!defined(_AMD64_)
#define PAL_DISPATCHEXCEPTION __Z21PAL_DispatchExceptionP8_CONTEXTP17_EXCEPTION_RECORD
#endif // defined(_AMD64_)

    .text
    .globl __Z21PAL_DispatchExceptionP8_CONTEXTP17_EXCEPTION_RECORD
    .globl _PAL_DispatchExceptionWrapper
_PAL_DispatchExceptionWrapper:
LBegin:
    int3
    call PAL_DISPATCHEXCEPTION
    int3
LEnd:

//
// PAL_DispatchExceptionWrapper will never be called; it only serves
// to be referenced from a stack frame on the faulting thread.  Its
// unwinding behavior is equivalent to any standard function having
// an ebp frame.  The FDE below is analogous to the one generated
// by "g++ -S" for the following source file.
//
// --- snip ---
// struct CONTEXT
// {
//     char reserved[716];
// };
// 
// struct EXCEPTION_RECORD
// {
//     char reserved[80];
// };
// 
// void PAL_DispatchException(CONTEXT *pContext, EXCEPTION_RECORD *pExceptionRecord);
// 
// extern "C" void PAL_DispatchExceptionWrapper()
// {
//     CONTEXT Context;
//     EXCEPTION_RECORD ExceptionRecord;
//     PAL_DispatchException(&Context, &ExceptionRecord);
// }
// --- snip ---
//

    .section __TEXT,__eh_frame,coalesced,no_toc+strip_static_syms+live_support
CIE_DispatchExceptionPersonality:
    .long   LECIE1-LSCIE1
LSCIE1:
    .long   0x0
    .byte   0x1
    .ascii "zLR\0"
    .byte   0x1
#ifdef BIT64
    .byte   0x78        // data_align: -8
    .byte   16          // return address register: rip
#else // BIT64
    .byte   0x7c        // data_align: -4
    .byte   0x8         // return address register: eip
#endif // BIT64 else
    .byte   0x2
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

    .globl _PAL_DispatchExceptionWrapper.eh
_PAL_DispatchExceptionWrapper.eh:
LSFDE1:
    .set    LLFDE1,LEFDE1-LASFDE1
    .set    LLength,LEnd-LBegin
    .long   LLFDE1
LASFDE1:
    .long   LASFDE1-CIE_DispatchExceptionPersonality
#ifdef BIT64
    .quad   LBegin-.
    .quad   LLength
    .byte   0x8
    .quad   0x0
#else // BIT64
    .long   LBegin-.
    .long   LLength
    .byte   0x4
    .long   0x0
#endif // BIT64 else
    .byte   0xe         // DW_CFA_def_cfa_offset
#ifdef BIT64
    .byte   0x10
    .byte   0x80 | 6    // DW_CFA_offset rbp
#else // BIT64
    .byte   0x8
    .byte   0x80 | 4    // DW_CFA_offset ebp
#endif // BIT64 else
    .byte   0x2
    .byte   0xd         // DW_CFA_def_cfa_register
#ifdef BIT64
    .byte   6           //   operand1 = rbp
#else // BIT64
    .byte   4           //   operand1 = ebp
#endif // BIT64
    .align 2
LEFDE1:
