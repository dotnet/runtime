// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Translation of hardware exception signals to Win32 exception codes, shared by the CoreCLR PAL and NativeAOT.

#ifndef _EXCEPTION_CODE_FOR_SIGNAL_H_
#define _EXCEPTION_CODE_FOR_SIGNAL_H_

#include <signal.h>
#include <stdint.h>

#include "ExceptionCodes.h"

// Returns the exception code for a SIGSEGV with SI_KERNEL code, or 0 to report an access violation.
typedef uint32_t (*SiKernelSegvExceptionCodeCallback)(const void* context);

// Translates signal and context information to a Win32 exception code.
inline uint32_t GetExceptionCodeForSignal(const siginfo_t *siginfo, const void *context,
                                          SiKernelSegvExceptionCodeCallback getSiKernelSegvExceptionCode = nullptr)
{
    // IMPORTANT NOTE: This function must not call any signal unsafe functions
    // since it is called from signal handlers.
    // That includes ASSERT and TRACE macros.
#ifdef ILL_ILLOPC
    switch (siginfo->si_signo)
    {
        case SIGILL:
            switch (siginfo->si_code)
            {
                case ILL_ILLOPC:    // Illegal opcode
                case ILL_ILLOPN:    // Illegal operand
                case ILL_ILLADR:    // Illegal addressing mode
                case ILL_ILLTRP:    // Illegal trap
                case ILL_COPROC:    // Co-processor error
                    return EXCEPTION_ILLEGAL_INSTRUCTION;
                case ILL_PRVOPC:    // Privileged opcode
                case ILL_PRVREG:    // Privileged register
                    return EXCEPTION_PRIV_INSTRUCTION;
                case ILL_BADSTK:    // Internal stack error
                    return EXCEPTION_STACK_OVERFLOW;
                default:
                    break;
            }
            break;
        case SIGFPE:
            switch (siginfo->si_code)
            {
                case FPE_INTDIV:
                    return EXCEPTION_INT_DIVIDE_BY_ZERO;
                case FPE_INTOVF:
                    return EXCEPTION_INT_OVERFLOW;
                case FPE_FLTDIV:
                    return EXCEPTION_FLT_DIVIDE_BY_ZERO;
                case FPE_FLTOVF:
                    return EXCEPTION_FLT_OVERFLOW;
                case FPE_FLTUND:
                    return EXCEPTION_FLT_UNDERFLOW;
                case FPE_FLTRES:
                    return EXCEPTION_FLT_INEXACT_RESULT;
                case FPE_FLTINV:
                    return EXCEPTION_FLT_INVALID_OPERATION;
                case FPE_FLTSUB:
                    return EXCEPTION_FLT_INVALID_OPERATION;
                default:
                    break;
            }
            break;
        case SIGSEGV:
            switch (siginfo->si_code)
            {
                case SI_USER:       // User-generated signal, sometimes sent
                                    // for SIGSEGV under normal circumstances
                case SEGV_MAPERR:   // Address not mapped to object
                case SEGV_ACCERR:   // Invalid permissions for mapped object
                    return EXCEPTION_ACCESS_VIOLATION;

#ifdef SI_KERNEL
                case SI_KERNEL:
                {
                    // Identify privileged instructions that are not identified as such by the system
                    if (getSiKernelSegvExceptionCode != nullptr)
                    {
                        uint32_t exceptionCode = getSiKernelSegvExceptionCode(context);
                        if (exceptionCode != 0)
                        {
                            return exceptionCode;
                        }
                    }
                    return EXCEPTION_ACCESS_VIOLATION;
                }
#endif
                default:
                    break;
            }
            break;
        case SIGBUS:
            switch (siginfo->si_code)
            {
                case BUS_ADRALN:    // Invalid address alignment
                    return EXCEPTION_DATATYPE_MISALIGNMENT;
                case BUS_ADRERR:    // Non-existent physical address
                    return EXCEPTION_ACCESS_VIOLATION;
                case BUS_OBJERR:    // Object-specific hardware error
                default:
                    break;
            }
            break;
        case SIGTRAP:
            switch (siginfo->si_code)
            {
#ifdef SI_KERNEL
                case SI_KERNEL:
#endif
                case SI_USER:
                case TRAP_BRKPT:    // Process breakpoint
                    return EXCEPTION_BREAKPOINT;
                case TRAP_TRACE:    // Process trace trap
                    return EXCEPTION_SINGLE_STEP;
                default:
                    // Got unknown SIGTRAP signal with code siginfo->si_code;
                    return EXCEPTION_ILLEGAL_INSTRUCTION;
            }
        default:
            break;
    }

    // Got unknown signal number siginfo->si_signo with code siginfo->si_code;
    return EXCEPTION_ILLEGAL_INSTRUCTION;
#else   // ILL_ILLOPC
    int trap;

    if (siginfo->si_signo == SIGFPE)
    {
        // Floating point exceptions are mapped by their si_code.
        switch (siginfo->si_code)
        {
            case FPE_INTDIV :
                return EXCEPTION_INT_DIVIDE_BY_ZERO;
            case FPE_INTOVF :
                return EXCEPTION_INT_OVERFLOW;
            case FPE_FLTDIV :
                return EXCEPTION_FLT_DIVIDE_BY_ZERO;
            case FPE_FLTOVF :
                return EXCEPTION_FLT_OVERFLOW;
            case FPE_FLTUND :
                return EXCEPTION_FLT_UNDERFLOW;
            case FPE_FLTRES :
                return EXCEPTION_FLT_INEXACT_RESULT;
            case FPE_FLTINV :
                return EXCEPTION_FLT_INVALID_OPERATION;
            case FPE_FLTSUB :/* subscript out of range */
                return EXCEPTION_FLT_INVALID_OPERATION;
            default:
                // Got unknown signal code siginfo->si_code;
                return 0;
        }
    }

    trap = ((const ucontext_t*)context)->uc_mcontext.mc_trapno;
    switch (trap)
    {
        case T_PRIVINFLT : /* privileged instruction */
            return EXCEPTION_PRIV_INSTRUCTION;
        case T_BPTFLT :    /* breakpoint instruction */
            return EXCEPTION_BREAKPOINT;
        case T_ARITHTRAP : /* arithmetic trap */
            return 0;      /* let the caller pick an exception code */
#ifdef T_ASTFLT
        case T_ASTFLT :    /* system forced exception : ^C, ^\. SIGINT signal
                              handler shouldn't be calling this function, since
                              it doesn't need an exception code */
            // Trap code T_ASTFLT received, shouldn't get here;
            return 0;
#endif  // T_ASTFLT
        case T_PROTFLT :   /* protection fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_TRCTRAP :   /* debug exception (sic) */
            return EXCEPTION_SINGLE_STEP;
        case T_PAGEFLT :   /* page fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_ALIGNFLT :  /* alignment fault */
            return EXCEPTION_DATATYPE_MISALIGNMENT;
        case T_DIVIDE :
            return EXCEPTION_INT_DIVIDE_BY_ZERO;
        case T_NMI :       /* non-maskable trap */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_OFLOW :
            return EXCEPTION_INT_OVERFLOW;
        case T_BOUND :     /* bound instruction fault */
            return EXCEPTION_ARRAY_BOUNDS_EXCEEDED;
        case T_DNA :       /* device not available fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_DOUBLEFLT : /* double fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_FPOPFLT :   /* fp coprocessor operand fetch fault */
            return EXCEPTION_FLT_INVALID_OPERATION;
        case T_TSSFLT :    /* invalid tss fault */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_SEGNPFLT :  /* segment not present fault */
            return EXCEPTION_ACCESS_VIOLATION;
        case T_STKFLT :    /* stack fault */
            return EXCEPTION_STACK_OVERFLOW;
        case T_MCHK :      /* machine check trap */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_RESERVED :  /* reserved (unknown) */
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        default:
            // Got unknown trap code trap;
            break;
    }
    return EXCEPTION_ILLEGAL_INSTRUCTION;
#endif  // ILL_ILLOPC
}

#endif // _EXCEPTION_CODE_FOR_SIGNAL_H_
