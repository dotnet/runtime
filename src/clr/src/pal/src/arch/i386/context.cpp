//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    context.c

Abstract:

    Implementation of GetThreadContext/SetThreadContext/DebugBreak functions for
    the Intel x86 platform. These functions are processor dependent.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/context.h"
#include "pal/debug.h"
#include "pal/thread.hpp"

#include <sys/ptrace.h> 
#include <errno.h>
#include <unistd.h>

SET_DEFAULT_DEBUG_CHANNEL(DEBUG);

// in context2.S
extern void CONTEXT_CaptureContext(LPCONTEXT lpContext);

#ifdef _X86_
#define CONTEXT_ALL_FLOATING (CONTEXT_FLOATING_POINT | CONTEXT_EXTENDED_REGISTERS)
#elif defined(_AMD64_)
#define CONTEXT_ALL_FLOATING CONTEXT_FLOATING_POINT
#else
#error Unexpected architecture.
#endif

#if !HAVE_MACH_EXCEPTIONS

#if HAVE_BSD_REGS_T
#include <machine/reg.h>
#include <machine/npx.h>
#endif  // HAVE_BSD_REGS_T
#if HAVE_PT_REGS
#include <asm/ptrace.h>
#endif  // HAVE_PT_REGS

#if HAVE_GREGSET_T

#ifdef BIT64
#define MCREG_Rbx(mc)       ((mc).gregs[REG_RBX])
#define MCREG_Rcx(mc)       ((mc).gregs[REG_RCX])
#define MCREG_Rdx(mc)       ((mc).gregs[REG_RDX])
#define MCREG_Rsi(mc)       ((mc).gregs[REG_RSI])
#define MCREG_Rdi(mc)       ((mc).gregs[REG_RDI])
#define MCREG_Rbp(mc)       ((mc).gregs[REG_RBP])
#define MCREG_Rax(mc)       ((mc).gregs[REG_RAX])
#define MCREG_Rip(mc)       ((mc).gregs[REG_RIP])
#define MCREG_Rsp(mc)       ((mc).gregs[REG_RSP])
#define MCREG_SegCs(mc)     ((mc).gregs[REG_CSGSFS])
#define MCREG_R8(mc)        ((mc).gregs[REG_R8])
#define MCREG_R9(mc)        ((mc).gregs[REG_R9])
#define MCREG_R10(mc)       ((mc).gregs[REG_R10])
#define MCREG_R11(mc)       ((mc).gregs[REG_R11])
#define MCREG_R12(mc)       ((mc).gregs[REG_R12])
#define MCREG_R13(mc)       ((mc).gregs[REG_R13])
#define MCREG_R14(mc)       ((mc).gregs[REG_R14])
#define MCREG_R15(mc)       ((mc).gregs[REG_R15])

#define FPREG_Xmm(uc, index) *(M128A*)&((uc)->__fpregs_mem._xmm[index])

#define FPREG_St(uc, index) *(M128A*)&((uc)->__fpregs_mem._st[index])

#define FPREG_ControlWord(uc) ((uc)->__fpregs_mem.cwd)
#define FPREG_StatusWord(uc) ((uc)->__fpregs_mem.swd)
#define FPREG_TagWord(uc) ((uc)->__fpregs_mem.ftw)
#define FPREG_ErrorOffset(uc) *(DWORD*)&((uc)->__fpregs_mem.rip)
#define FPREG_ErrorSelector(uc) *(((WORD*)&((uc)->__fpregs_mem.rip)) + 2)
#define FPREG_DataOffset(uc) *(DWORD*)&((uc)->__fpregs_mem.rdp)
#define FPREG_DataSelector(uc) *(((WORD*)&((uc)->__fpregs_mem.rdp)) + 2)
#define FPREG_MxCsr(uc) ((uc)->__fpregs_mem.mxcsr)
#define FPREG_MxCsr_Mask(uc) ((uc)->__fpregs_mem.mxcr_mask)

#else // BIT64

#define MCREG_Ebx(mc)       ((mc).gregs[REG_EBX])
#define MCREG_Ecx(mc)       ((mc).gregs[REG_ECX])
#define MCREG_Edx(mc)       ((mc).gregs[REG_EDX])
#define MCREG_Esi(mc)       ((mc).gregs[REG_ESI])
#define MCREG_Edi(mc)       ((mc).gregs[REG_EDI])
#define MCREG_Ebp(mc)       ((mc).gregs[REG_EBP])
#define MCREG_Eax(mc)       ((mc).gregs[REG_EAX])
#define MCREG_Eip(mc)       ((mc).gregs[REG_EIP])
#define MCREG_Esp(mc)       ((mc).gregs[REG_ESP])
#define MCREG_SegCs(mc)     ((mc).gregs[REG_CS])
#define MCREG_SegSs(mc)     ((mc).gregs[REG_SS])

#endif // BIT64

#define MCREG_EFlags(mc)    ((mc).gregs[REG_EFL])

#else // HAVE_GREGSET_T

#define MCREG_Ebx(mc)       ((mc).mc_ebx)
#define MCREG_Ecx(mc)       ((mc).mc_ecx)
#define MCREG_Edx(mc)       ((mc).mc_edx)
#define MCREG_Esi(mc)       ((mc).mc_esi)
#define MCREG_Edi(mc)       ((mc).mc_edi)
#define MCREG_Ebp(mc)       ((mc).mc_ebp)
#define MCREG_Eax(mc)       ((mc).mc_eax)
#define MCREG_Eip(mc)       ((mc).mc_eip)
#define MCREG_SegCs(mc)     ((mc).mc_cs)
#define MCREG_EFlags(mc)    ((mc).mc_eflags)
#define MCREG_Esp(mc)       ((mc).mc_esp)
#define MCREG_SegSs(mc)     ((mc).mc_ss)

#endif // HAVE_GREGSET_T


#if HAVE_PT_REGS

#ifdef BIT64
#define PTREG_Rbx(ptreg)    ((ptreg).rbx)
#define PTREG_Rcx(ptreg)    ((ptreg).rcx)
#define PTREG_Rdx(ptreg)    ((ptreg).rdx)
#define PTREG_Rsi(ptreg)    ((ptreg).rsi)
#define PTREG_Rdi(ptreg)    ((ptreg).rdi)
#define PTREG_Rbp(ptreg)    ((ptreg).rbp)
#define PTREG_Rax(ptreg)    ((ptreg).rax)
#define PTREG_Rip(ptreg)    ((ptreg).rip)
#define PTREG_SegCs(ptreg)  ((ptreg).cs)
#define PTREG_SegSs(ptreg)  ((ptreg).ss)
#define PTREG_Rsp(ptreg)    ((ptreg).rsp)
#define PTREG_R8(ptreg)     ((ptreg).r8)
#define PTREG_R9(ptreg)     ((ptreg).r9)
#define PTREG_R10(ptreg)    ((ptreg).r10)
#define PTREG_R11(ptreg)    ((ptreg).r11)
#define PTREG_R12(ptreg)    ((ptreg).r12)
#define PTREG_R13(ptreg)    ((ptreg).r13)
#define PTREG_R14(ptreg)    ((ptreg).r14)
#define PTREG_R15(ptreg)    ((ptreg).r15)

#else // BIT64

#define PTREG_Ebx(ptreg)    ((ptreg).ebx)
#define PTREG_Ecx(ptreg)    ((ptreg).ecx)
#define PTREG_Edx(ptreg)    ((ptreg).edx)
#define PTREG_Esi(ptreg)    ((ptreg).esi)
#define PTREG_Edi(ptreg)    ((ptreg).edi)
#define PTREG_Ebp(ptreg)    ((ptreg).ebp)
#define PTREG_Eax(ptreg)    ((ptreg).eax)
#define PTREG_Eip(ptreg)    ((ptreg).eip)
#define PTREG_SegCs(ptreg)  ((ptreg).xcs)
#define PTREG_SegSs(ptreg)  ((ptreg).xss)
#define PTREG_Esp(ptreg)    ((ptreg).esp)

#endif // BIT64


#define PTREG_EFlags(ptreg) ((ptreg).eflags)

#endif // HAVE_PT_REGS


#if HAVE_BSD_REGS_T

#define BSDREG_Ebx(reg)     ((reg).r_ebx)
#define BSDREG_Ecx(reg)     ((reg).r_ecx)
#define BSDREG_Edx(reg)     ((reg).r_edx)
#define BSDREG_Esi(reg)     ((reg).r_esi)
#define BSDREG_Edi(reg)     ((reg).r_edi)
#define BSDREG_Ebp(reg)     ((reg).r_ebp)
#define BSDREG_Eax(reg)     ((reg).r_eax)
#define BSDREG_Eip(reg)     ((reg).r_eip)
#define BSDREG_SegCs(reg)   ((reg).r_cs)
#define BSDREG_EFlags(reg)  ((reg).r_eflags)
#define BSDREG_Esp(reg)     ((reg).r_esp)
#define BSDREG_SegSs(reg)   ((reg).r_ss)

#endif // HAVE_BSD_REGS_T

#ifdef BIT64
#define ASSIGN_CONTROL_REGS \
        ASSIGN_REG(Rbp)     \
        ASSIGN_REG(Rip)     \
        ASSIGN_REG(SegCs)   \
        ASSIGN_REG(EFlags)  \
        ASSIGN_REG(Rsp)     \

#define ASSIGN_INTEGER_REGS \
        ASSIGN_REG(Rdi)     \
        ASSIGN_REG(Rsi)     \
        ASSIGN_REG(Rbx)     \
        ASSIGN_REG(Rdx)     \
        ASSIGN_REG(Rcx)     \
        ASSIGN_REG(Rax)     \
        ASSIGN_REG(R8)     \
        ASSIGN_REG(R9)     \
        ASSIGN_REG(R10)     \
        ASSIGN_REG(R11)     \
        ASSIGN_REG(R12)     \
        ASSIGN_REG(R13)     \
        ASSIGN_REG(R14)     \
        ASSIGN_REG(R15)     \

#else // BIT64
#define ASSIGN_CONTROL_REGS \
        ASSIGN_REG(Ebp)     \
        ASSIGN_REG(Eip)     \
        ASSIGN_REG(SegCs)   \
        ASSIGN_REG(EFlags)  \
        ASSIGN_REG(Esp)     \
        ASSIGN_REG(SegSs)   \

#define ASSIGN_INTEGER_REGS \
        ASSIGN_REG(Edi)     \
        ASSIGN_REG(Esi)     \
        ASSIGN_REG(Ebx)     \
        ASSIGN_REG(Edx)     \
        ASSIGN_REG(Ecx)     \
        ASSIGN_REG(Eax)     \

#endif //BIT64

#define ASSIGN_ALL_REGS     \
        ASSIGN_CONTROL_REGS \
        ASSIGN_INTEGER_REGS \

/*++
Function:
  CONTEXT_GetRegisters

Abstract
  retrieve the machine registers value of the indicated process.

Parameter
  processId: process ID
  registers: reg structure in which the machine registers value will be returned.
Return
 returns TRUE if it succeeds, FALSE otherwise
--*/
BOOL CONTEXT_GetRegisters(DWORD processId, ucontext_t *registers)
{
#if HAVE_BSD_REGS_T
    int regFd = -1;
#endif  // HAVE_BSD_REGS_T
    BOOL bRet = FALSE;

    if (processId == GetCurrentProcessId()) 
    {
#if HAVE_GETCONTEXT
        if (getcontext(registers) != 0)
        {
            ASSERT("getcontext() failed %d (%s)\n", errno, strerror(errno));
            return FALSE;
        }
#elif HAVE_BSD_REGS_T
        char buf[MAX_PATH];
        struct reg bsd_registers;

        sprintf_s(buf, sizeof(buf), "/proc/%d/regs", processId);

        if ((regFd = PAL__open(buf, O_RDONLY)) == -1) 
        {
          ASSERT("PAL__open() failed %d (%s) \n", errno, strerror(errno));
          return FALSE;
        }

        if (lseek(regFd, 0, 0) == -1)
        {
            ASSERT("lseek() failed %d (%s)\n", errno, strerror(errno));
            goto EXIT;
        }

        if (read(regFd, &bsd_registers, sizeof(bsd_registers)) != sizeof(bsd_registers))
        {
            ASSERT("read() failed %d (%s)\n", errno, strerror(errno));
            goto EXIT;
        }

#define ASSIGN_REG(reg) MCREG_##reg(registers->uc_mcontext) = BSDREG_##reg(bsd_registers);
        ASSIGN_ALL_REGS
#undef ASSIGN_REG

#else
#error "Don't know how to get current context on this platform!"
#endif
    }
    else
    {
#if HAVE_PT_REGS
        struct pt_regs ptrace_registers;
#elif HAVE_BSD_REGS_T
        struct reg ptrace_registers;
#endif

        if (ptrace((__ptrace_request)PT_GETREGS, processId, (caddr_t) &ptrace_registers, 0) == -1)
        {
            ASSERT("Failed ptrace(PT_GETREGS, processId:%d) errno:%d (%s)\n",
                   processId, errno, strerror(errno));
        }

#if HAVE_PT_REGS
#define ASSIGN_REG(reg) MCREG_##reg(registers->uc_mcontext) = PTREG_##reg(ptrace_registers);
#elif HAVE_BSD_REGS_T
#define ASSIGN_REG(reg) MCREG_##reg(registers->uc_mcontext) = BSDREG_##reg(ptrace_registers);
#endif
        ASSIGN_ALL_REGS
#undef ASSIGN_REG
    }
    
    bRet = TRUE;
#if HAVE_BSD_REGS_T
EXIT :
    if (regFd != -1)
    {
        close(regFd);
    }
#endif  // HAVE_BSD_REGS_T
    return bRet;
}

/*++
Function:
  GetThreadContext

See MSDN doc.
--*/
BOOL
CONTEXT_GetThreadContext(
         DWORD dwProcessId,
#if !defined(_AMD64_)
         DWORD dwThreadId,
#else // defined(_AMD64_)
         DWORD64 dwThreadId,
#endif // !defined(_AMD64_)
         DWORD dwLwpId,
         LPCONTEXT lpContext)
{    
    BOOL ret = FALSE;
    ucontext_t registers;

    if (lpContext == NULL)
    {
        ERROR("Invalid lpContext parameter value\n");
        SetLastError(ERROR_NOACCESS);
        goto EXIT;
    }
    
    /* How to consider the case when dwThreadId is different from the current
       thread of its owner process. Machine registers values could be retreived
       by a ptrace(pid, ...) call or from the "/proc/%pid/reg" file content. 
       Unfortunately, these two methods only depend on process ID, not on 
       thread ID. */

    if (dwProcessId == GetCurrentProcessId())
    {
        if (dwThreadId != THREADSilentGetCurrentThreadId())
        {
            DWORD flags;
            // There aren't any APIs for this. We can potentially get the
            // context of another thread by using per-thread signals, but
            // on FreeBSD signal handlers that are called as a result
            // of signals raised via pthread_kill don't get a valid
            // sigcontext or ucontext_t. But we need this to return TRUE
            // to avoid an assertion in the CLR in code that manages to
            // cope reasonably well without a valid thread context.
            // Given that, we'll zero out our structure and return TRUE.
            ERROR("GetThreadContext on a thread other than the current "
                  "thread is returning TRUE\n");
            flags = lpContext->ContextFlags;
            memset(lpContext, 0, sizeof(*lpContext));
            lpContext->ContextFlags = flags;
            ret = TRUE;
            goto EXIT;
        }

    }

    if (lpContext->ContextFlags & 
        (CONTEXT_CONTROL | CONTEXT_INTEGER))
    {        
        if (CONTEXT_GetRegisters(dwProcessId, &registers) == FALSE)
        {
            SetLastError(ERROR_INTERNAL_ERROR);
            goto EXIT;
        }

        CONTEXTFromNativeContext(&registers, lpContext, lpContext->ContextFlags);        
    }

    ret = TRUE;

EXIT:
    return ret;
}

/*++
Function:
  SetThreadContext

See MSDN doc.
--*/
BOOL
CONTEXT_SetThreadContext(
           DWORD dwProcessId,
#if !defined(_AMD64_)
           DWORD dwThreadId,
#else // defined(_AMD64_)
           DWORD64 dwThreadId,
#endif // !defined(_AMD64_)
           DWORD dwLwpId,
           CONST CONTEXT *lpContext)
{
    BOOL ret = FALSE;

#if HAVE_PT_REGS
    struct pt_regs ptrace_registers;
#elif HAVE_BSD_REGS_T
    struct reg ptrace_registers;
#endif

    if (lpContext == NULL)
    {
        ERROR("Invalid lpContext parameter value\n");
        SetLastError(ERROR_NOACCESS);
        goto EXIT;
    }
    
    /* How to consider the case when dwThreadId is different from the current
       thread of its owner process. Machine registers values could be retreived
       by a ptrace(pid, ...) call or from the "/proc/%pid/reg" file content. 
       Unfortunately, these two methods only depend on process ID, not on 
       thread ID. */
        
    if (dwProcessId == GetCurrentProcessId())
    {
#ifdef FEATURE_PAL_SXS
        // Need to implement SetThreadContext(current thread) for the IX architecture; look at common_signal_handler.
        _ASSERT(FALSE);
#endif // FEATURE_PAL_SXS
        ASSERT("SetThreadContext should be called for cross-process only.\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    
    if (lpContext->ContextFlags  & 
        (CONTEXT_CONTROL | CONTEXT_INTEGER))
    {   
        if (ptrace((__ptrace_request)PT_GETREGS, dwProcessId, (caddr_t)&ptrace_registers, 0) == -1)
        {
            ASSERT("Failed ptrace(PT_GETREGS, processId:%d) errno:%d (%s)\n",
                   dwProcessId, errno, strerror(errno));
             SetLastError(ERROR_INTERNAL_ERROR);
             goto EXIT;
        }

#if HAVE_PT_REGS
#define ASSIGN_REG(reg) PTREG_##reg(ptrace_registers) = lpContext->reg;
#elif HAVE_BSD_REGS_T
#define ASSIGN_REG(reg) BSDREG_##reg(ptrace_registers) = lpContext->reg;
#endif
        if (lpContext->ContextFlags & CONTEXT_CONTROL)
        {
            ASSIGN_CONTROL_REGS
        }
        if (lpContext->ContextFlags & CONTEXT_INTEGER)
        {
            ASSIGN_INTEGER_REGS
        }
#undef ASSIGN_REG
        
        if (ptrace((__ptrace_request)PT_SETREGS, dwProcessId, (caddr_t)&ptrace_registers, 0) == -1)
        {
            ASSERT("Failed ptrace(PT_SETREGS, processId:%d) errno:%d (%s)\n",
                   dwProcessId, errno, strerror(errno));
            SetLastError(ERROR_INTERNAL_ERROR);
            goto EXIT;
        }
    }

    ret = TRUE;
   EXIT:
     return ret;
}

/*++
Function :
    CONTEXTToNativeContext
    
    Converts a CONTEXT record to a native context.

Parameters :
    CONST CONTEXT *lpContext : CONTEXT to convert
    native_context_t *native : native context to fill in
    ULONG contextFlags : flags that determine which registers are valid in
                         lpContext and which ones to set in native

Return value :
    None

--*/
void CONTEXTToNativeContext(CONST CONTEXT *lpContext, native_context_t *native)
{
#define ASSIGN_REG(reg) MCREG_##reg(native->uc_mcontext) = lpContext->reg;
    if ((lpContext->ContextFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        ASSIGN_CONTROL_REGS
    }

    if ((lpContext->ContextFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        ASSIGN_INTEGER_REGS
    }
#undef ASSIGN_REG

    if ((lpContext->ContextFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        FPREG_ControlWord(native) = lpContext->FltSave.ControlWord;
        FPREG_StatusWord(native) = lpContext->FltSave.StatusWord;
        FPREG_TagWord(native) = lpContext->FltSave.TagWord;
        FPREG_ErrorOffset(native) = lpContext->FltSave.ErrorOffset;
        FPREG_ErrorSelector(native) = lpContext->FltSave.ErrorSelector;
        FPREG_DataOffset(native) = lpContext->FltSave.DataOffset;
        FPREG_DataSelector(native) = lpContext->FltSave.DataSelector;
        FPREG_MxCsr(native) = lpContext->FltSave.MxCsr;
        FPREG_MxCsr_Mask(native) = lpContext->FltSave.MxCsr_Mask;

        for (int i = 0; i < 8; i++)
        {
            FPREG_St(native, i) = lpContext->FltSave.FloatRegisters[i];
        }

        for (int i = 0; i < 16; i++)
        {
            FPREG_Xmm(native, i) = lpContext->FltSave.XmmRegisters[i];
        }        
    }
}

/*++
Function :
    CONTEXTFromNativeContext
    
    Converts a native context to a CONTEXT record.

Parameters :
    const native_context_t *native : native context to convert
    LPCONTEXT lpContext : CONTEXT to fill in
    ULONG contextFlags : flags that determine which registers are valid in
                         native and which ones to set in lpContext

Return value :
    None

--*/
void CONTEXTFromNativeContext(const native_context_t *native, LPCONTEXT lpContext,
                              ULONG contextFlags)
{
    lpContext->ContextFlags = contextFlags;

#define ASSIGN_REG(reg) lpContext->reg = MCREG_##reg(native->uc_mcontext);
    if ((contextFlags & CONTEXT_CONTROL) == CONTEXT_CONTROL)
    {
        ASSIGN_CONTROL_REGS
    }

    if ((contextFlags & CONTEXT_INTEGER) == CONTEXT_INTEGER)
    {
        ASSIGN_INTEGER_REGS
    }
#undef ASSIGN_REG
    
    if ((contextFlags & CONTEXT_FLOATING_POINT) == CONTEXT_FLOATING_POINT)
    {
        lpContext->FltSave.ControlWord = FPREG_ControlWord(native);
        lpContext->FltSave.StatusWord = FPREG_StatusWord(native);
        lpContext->FltSave.TagWord = FPREG_TagWord(native);
        lpContext->FltSave.ErrorOffset = FPREG_ErrorOffset(native);
        lpContext->FltSave.ErrorSelector = FPREG_ErrorSelector(native);
        lpContext->FltSave.DataOffset = FPREG_DataOffset(native);
        lpContext->FltSave.DataSelector = FPREG_DataSelector(native);
        lpContext->FltSave.MxCsr = FPREG_MxCsr(native);
        lpContext->FltSave.MxCsr_Mask = FPREG_MxCsr_Mask(native);

        for (int i = 0; i < 8; i++)
        {
            lpContext->FltSave.FloatRegisters[i] = FPREG_St(native, i);
        }

        for (int i = 0; i < 16; i++)
        {
            lpContext->FltSave.XmmRegisters[i] = FPREG_Xmm(native, i);
        }        
    }
}

/*++
Function :
    CONTEXTGetPC
    
    Returns the program counter from the native context.

Parameters :
    const native_context_t *native : native context

Return value :
    The program counter from the native context.

--*/
LPVOID CONTEXTGetPC(const native_context_t *context)
{
#ifdef BIT64
    return (LPVOID)MCREG_Rip(context->uc_mcontext);
#else
    return (LPVOID) MCREG_Eip(context->uc_mcontext);
#endif // BIT64
}

/*++
Function :
    CONTEXTGetExceptionCodeForSignal
    
    Translates signal and context information to a Win32 exception code.

Parameters :
    const siginfo_t *siginfo : signal information from a signal handler
    const native_context_t *context : context information

Return value :
    The Win32 exception code that corresponds to the signal and context
    information.

--*/
#ifdef ILL_ILLOPC
// If si_code values are available for all signals, use those.
DWORD CONTEXTGetExceptionCodeForSignal(const siginfo_t *siginfo,
                                       const native_context_t *context)
{
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
        case SIGTRAP:
            switch (siginfo->si_code)
            {
                case SI_KERNEL:
                case SI_USER:
                case TRAP_BRKPT:    // Process breakpoint
                    return EXCEPTION_BREAKPOINT;
                case TRAP_TRACE:    // Process trace trap
                    return EXCEPTION_SINGLE_STEP;
                default:
                    // We don't want to use ASSERT here since it raises SIGTRAP and we
                    // might again end up here resulting in an infinite loop! 
                    // so, we print out an error message and return 
                    DBG_PRINTF(DLI_ASSERT, defdbgchan, TRUE) 
                    ("Got unknown SIGTRAP signal (%d) with code %d\n", SIGTRAP, siginfo->si_code);

                    return EXCEPTION_ILLEGAL_INSTRUCTION;
            }
        default:
            break;
    }
    ASSERT("Got unknown signal number %d with code %d\n",
           siginfo->si_signo, siginfo->si_code);
    return EXCEPTION_ILLEGAL_INSTRUCTION;
}
#else   // ILL_ILLOPC
DWORD CONTEXTGetExceptionCodeForSignal(const siginfo_t *siginfo,
                                       const native_context_t *context)
{
    int trap;

    if (siginfo->si_signo == SIGFPE)
    {
        // Floating point exceptions are mapped by their si_code.
        switch (siginfo->si_code)
        {
            case FPE_INTDIV :
                TRACE("Got signal SIGFPE:FPE_INTDIV; raising "
                      "EXCEPTION_INT_DIVIDE_BY_ZERO\n");
                return EXCEPTION_INT_DIVIDE_BY_ZERO;
                break;
            case FPE_INTOVF :
                TRACE("Got signal SIGFPE:FPE_INTOVF; raising "
                      "EXCEPTION_INT_OVERFLOW\n");
                return EXCEPTION_INT_OVERFLOW;
                break;
            case FPE_FLTDIV :
                TRACE("Got signal SIGFPE:FPE_FLTDIV; raising "
                      "EXCEPTION_FLT_DIVIDE_BY_ZERO\n");
                return EXCEPTION_FLT_DIVIDE_BY_ZERO;
                break;
            case FPE_FLTOVF :
                TRACE("Got signal SIGFPE:FPE_FLTOVF; raising "
                      "EXCEPTION_FLT_OVERFLOW\n");
                return EXCEPTION_FLT_OVERFLOW;
                break;
            case FPE_FLTUND :
                TRACE("Got signal SIGFPE:FPE_FLTUND; raising "
                      "EXCEPTION_FLT_UNDERFLOW\n");
                return EXCEPTION_FLT_UNDERFLOW;
                break;
            case FPE_FLTRES :
                TRACE("Got signal SIGFPE:FPE_FLTRES; raising "
                      "EXCEPTION_FLT_INEXACT_RESULT\n");
                return EXCEPTION_FLT_INEXACT_RESULT;
                break;
            case FPE_FLTINV :
                TRACE("Got signal SIGFPE:FPE_FLTINV; raising "
                      "EXCEPTION_FLT_INVALID_OPERATION\n");
                return EXCEPTION_FLT_INVALID_OPERATION;
                break;
            case FPE_FLTSUB :/* subscript out of range */
                TRACE("Got signal SIGFPE:FPE_FLTSUB; raising "
                      "EXCEPTION_FLT_INVALID_OPERATION\n");
                return EXCEPTION_FLT_INVALID_OPERATION;
                break;
            default:
                ASSERT("Got unknown signal code %d\n", siginfo->si_code);
                break;
        }
    }

    trap = context->uc_mcontext.mc_trapno;
    switch (trap)
    {
        case T_PRIVINFLT : /* privileged instruction */
            TRACE("Trap code T_PRIVINFLT mapped to EXCEPTION_PRIV_INSTRUCTION\n");
            return EXCEPTION_PRIV_INSTRUCTION; 
        case T_BPTFLT :    /* breakpoint instruction */
            TRACE("Trap code T_BPTFLT mapped to EXCEPTION_BREAKPOINT\n");
            return EXCEPTION_BREAKPOINT;
        case T_ARITHTRAP : /* arithmetic trap */
            TRACE("Trap code T_ARITHTRAP maps to floating point exception...\n");
            return 0;      /* let the caller pick an exception code */
#ifdef T_ASTFLT
        case T_ASTFLT :    /* system forced exception : ^C, ^\. SIGINT signal 
                              handler shouldn't be calling this function, since
                              it doesn't need an exception code */
            ASSERT("Trap code T_ASTFLT received, shouldn't get here\n");
            return 0;
#endif  // T_ASTFLT
        case T_PROTFLT :   /* protection fault */
            TRACE("Trap code T_PROTFLT mapped to EXCEPTION_ACCESS_VIOLATION\n");
            return EXCEPTION_ACCESS_VIOLATION; 
        case T_TRCTRAP :   /* debug exception (sic) */
            TRACE("Trap code T_TRCTRAP mapped to EXCEPTION_SINGLE_STEP\n");
            return EXCEPTION_SINGLE_STEP;
        case T_PAGEFLT :   /* page fault */
            TRACE("Trap code T_PAGEFLT mapped to EXCEPTION_ACCESS_VIOLATION\n");
            return EXCEPTION_ACCESS_VIOLATION;
        case T_ALIGNFLT :  /* alignment fault */
            TRACE("Trap code T_ALIGNFLT mapped to EXCEPTION_DATATYPE_MISALIGNMENT\n");
            return EXCEPTION_DATATYPE_MISALIGNMENT;
        case T_DIVIDE :
            TRACE("Trap code T_DIVIDE mapped to EXCEPTION_INT_DIVIDE_BY_ZERO\n");
            return EXCEPTION_INT_DIVIDE_BY_ZERO;
        case T_NMI :       /* non-maskable trap */
            TRACE("Trap code T_NMI mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION;
        case T_OFLOW :
            TRACE("Trap code T_OFLOW mapped to EXCEPTION_INT_OVERFLOW\n");
            return EXCEPTION_INT_OVERFLOW;
        case T_BOUND :     /* bound instruction fault */
            TRACE("Trap code T_BOUND mapped to EXCEPTION_ARRAY_BOUNDS_EXCEEDED\n");
            return EXCEPTION_ARRAY_BOUNDS_EXCEEDED; 
        case T_DNA :       /* device not available fault */
            TRACE("Trap code T_DNA mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        case T_DOUBLEFLT : /* double fault */
            TRACE("Trap code T_DOUBLEFLT mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        case T_FPOPFLT :   /* fp coprocessor operand fetch fault */
            TRACE("Trap code T_FPOPFLT mapped to EXCEPTION_FLT_INVALID_OPERATION\n");
            return EXCEPTION_FLT_INVALID_OPERATION; 
        case T_TSSFLT :    /* invalid tss fault */
            TRACE("Trap code T_TSSFLT mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        case T_SEGNPFLT :  /* segment not present fault */
            TRACE("Trap code T_SEGNPFLT mapped to EXCEPTION_ACCESS_VIOLATION\n");
            return EXCEPTION_ACCESS_VIOLATION; 
        case T_STKFLT :    /* stack fault */
            TRACE("Trap code T_STKFLT mapped to EXCEPTION_STACK_OVERFLOW\n");
            return EXCEPTION_STACK_OVERFLOW; 
        case T_MCHK :      /* machine check trap */
            TRACE("Trap code T_MCHK mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        case T_RESERVED :  /* reserved (unknown) */
            TRACE("Trap code T_RESERVED mapped to EXCEPTION_ILLEGAL_INSTRUCTION\n");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        default:
            ASSERT("Got unknown trap code %d\n", trap);
            break;
    }
    return EXCEPTION_ILLEGAL_INSTRUCTION;
}
#endif  // ILL_ILLOPC

#else // !HAVE_MACH_EXCEPTIONS

#include <mach/message.h>
#include <mach/thread_act.h>
#include "../../exception/machexception.h"

/*++
Function:
  CONTEXT_GetThreadContextFromPort

  Helper for GetThreadContext that uses a mach_port
--*/
kern_return_t
CONTEXT_GetThreadContextFromPort(
        mach_port_t Port,
        LPCONTEXT lpContext)
{
    // Extract the CONTEXT from the Mach thread.
    
    kern_return_t MachRet = KERN_SUCCESS;
    mach_msg_type_number_t StateCount;
    thread_state_flavor_t StateFlavor;
  
    if (lpContext->ContextFlags & (CONTEXT_CONTROL|CONTEXT_INTEGER))
    {
#ifdef _X86_  
        x86_thread_state32_t State;
        StateFlavor = x86_THREAD_STATE32;
#elif defined(_AMD64_)
        x86_thread_state64_t State;
        StateFlavor = x86_THREAD_STATE64;
#else
#error Unexpected architecture.
#endif

        StateCount = sizeof(State) / sizeof(natural_t);

        MachRet = thread_get_state(Port,
           StateFlavor,
           (thread_state_t)&State,
           &StateCount);
        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("thread_get_state(THREAD_STATE) failed: %d\n", MachRet);
            goto EXIT;
        }

        // Copy in the GPRs and the various other control registers
#ifdef _X86_
        lpContext->Eax = State.eax;
        lpContext->Ebx = State.ebx;
        lpContext->Ecx = State.ecx;
        lpContext->Edx = State.edx;
        lpContext->Edi = State.edi;
        lpContext->Esi = State.esi;
        lpContext->Ebp = State.ebp;
        lpContext->Esp = State.esp;
        lpContext->SegSs = State.ss;
        lpContext->EFlags = State.eflags;
        lpContext->Eip = State.eip;
        lpContext->SegCs = State.cs;
        lpContext->SegDs_PAL_Undefined = State.ds;
        lpContext->SegEs_PAL_Undefined = State.es;
        lpContext->SegFs_PAL_Undefined = State.fs;
        lpContext->SegGs_PAL_Undefined = State.gs;
#elif defined(_AMD64_)
        lpContext->Rax = State.__rax;
        lpContext->Rbx = State.__rbx;
        lpContext->Rcx = State.__rcx;
        lpContext->Rdx = State.__rdx;
        lpContext->Rdi = State.__rdi;
        lpContext->Rsi = State.__rsi;
        lpContext->Rbp = State.__rbp;
        lpContext->Rsp = State.__rsp;
        lpContext->R8 = State.__r8;
        lpContext->R9 = State.__r9;
        lpContext->R10 = State.__r10;
        lpContext->R11 = State.__r11;
        lpContext->R12 = State.__r12;
        lpContext->R13 = State.__r13;
        lpContext->R14 = State.__r14;
        lpContext->R15 = State.__r15;
//        lpContext->SegSs = State.ss; // no such state?
        lpContext->EFlags = State.__rflags;
        lpContext->Rip = State.__rip;
        lpContext->SegCs = State.__cs;
//        lpContext->SegDs_PAL_Undefined = State.ds; // no such state?
//        lpContext->SegEs_PAL_Undefined = State.es; // no such state?
        lpContext->SegFs = State.__fs;
        lpContext->SegGs = State.__gs;
#else
#error Unexpected architecture.
#endif
    }
    
    if (lpContext->ContextFlags & CONTEXT_ALL_FLOATING) {
#ifdef _X86_
        x86_float_state32_t State;
        StateFlavor = x86_FLOAT_STATE32;
#elif defined(_AMD64_)
        x86_float_state64_t State;
        StateFlavor = x86_FLOAT_STATE64;
#else
#error Unexpected architecture.
#endif
        StateCount = sizeof(State) / sizeof(natural_t);
        
        MachRet = thread_get_state(Port,
            StateFlavor,
            (thread_state_t)&State,
            &StateCount);
        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("thread_get_state(FLOAT_STATE) failed: %d\n", MachRet);
            goto EXIT;
        }
        
        if (lpContext->ContextFlags & CONTEXT_FLOATING_POINT)
        {
            // Copy the FPRs
#ifdef _X86_
            lpContext->FloatSave.ControlWord = *(DWORD*)&State.fpu_fcw;
            lpContext->FloatSave.StatusWord = *(DWORD*)&State.fpu_fsw;
            lpContext->FloatSave.TagWord = State.fpu_ftw;
            lpContext->FloatSave.ErrorOffset = State.fpu_ip;
            lpContext->FloatSave.ErrorSelector = State.fpu_cs;
            lpContext->FloatSave.DataOffset = State.fpu_dp;
            lpContext->FloatSave.DataSelector = State.fpu_ds;
            lpContext->FloatSave.Cr0NpxState = State.fpu_mxcsr;
            
            // Windows stores the floating point registers in a packed layout (each 10-byte register end to end
            // for a total of 80 bytes). But Mach returns each register in an 16-bit structure (presumably for
            // alignment purposes). So we can't just memcpy the registers over in a single block, we need to copy
            // them individually.
            for (int i = 0; i < 8; i++)
                memcpy(&lpContext->FloatSave.RegisterArea[i * 10], (&State.fpu_stmm0)[i].mmst_reg, 10);
#elif defined(_AMD64_)
            lpContext->FltSave.ControlWord = *(DWORD*)&State.__fpu_fcw;
            lpContext->FltSave.StatusWord = *(DWORD*)&State.__fpu_fsw;
            lpContext->FltSave.TagWord = State.__fpu_ftw;
            lpContext->FltSave.ErrorOffset = State.__fpu_ip;
            lpContext->FltSave.ErrorSelector = State.__fpu_cs;
            lpContext->FltSave.DataOffset = State.__fpu_dp;
            lpContext->FltSave.DataSelector = State.__fpu_ds;
            lpContext->FltSave.MxCsr = State.__fpu_mxcsr;
            lpContext->FltSave.MxCsr_Mask = State.__fpu_mxcsrmask; // note: we don't save the mask for x86
            
            // Windows stores the floating point registers in a packed layout (each 10-byte register end to end
            // for a total of 80 bytes). But Mach returns each register in an 16-bit structure (presumably for
            // alignment purposes). So we can't just memcpy the registers over in a single block, we need to copy
            // them individually.
            for (int i = 0; i < 8; i++)
                memcpy(&lpContext->FltSave.FloatRegisters[i], (&State.__fpu_stmm0)[i].__mmst_reg, 10);
            
            // AMD64's FLOATING_POINT includes the xmm registers.
            memcpy(&lpContext->Xmm0, &State.__fpu_xmm0, 8 * 16);
#else
#error Unexpected architecture.
#endif
        }

#ifdef _X86_
        if (lpContext->ContextFlags & CONTEXT_EXTENDED_REGISTERS) {
            // The only extended register information that Mach will tell us about are the xmm register values.
            // Both Windows and Mach store the registers in a packed layout (each of the 8 registers is 16 bytes)
            // so we can simply memcpy them across.
            memcpy(lpContext->ExtendedRegisters + CONTEXT_EXREG_XMM_OFFSET, &State.fpu_xmm0, 8 * 16);
        }
#endif
    }

EXIT:
    return MachRet;
}

/*++
Function:
  GetThreadContext

See MSDN doc.
--*/
BOOL
CONTEXT_GetThreadContext(
         DWORD dwProcessId,
#if !defined(_AMD64_)
         DWORD dwThreadId,
#else // defined(_AMD64_)
         DWORD64 dwThreadId,
#endif // !defined(_AMD64_)
         DWORD dwLwpId,
         LPCONTEXT lpContext)
{
    BOOL ret = FALSE;

    if (lpContext == NULL)
    {
        ERROR("Invalid lpContext parameter value\n");
        SetLastError(ERROR_NOACCESS);
        goto EXIT;
    }
    
    if (GetCurrentProcessId() == dwProcessId)
    {
        if (dwThreadId != THREADSilentGetCurrentThreadId())
        {
            // the target thread is in the current process, but isn't 
            // the current one: extract the CONTEXT from the Mach thread.            
            mach_port_t mptPort;
            mptPort = pthread_mach_thread_np((pthread_t)dwThreadId);
   
            ret = (CONTEXT_GetThreadContextFromPort(mptPort, lpContext) == KERN_SUCCESS);
        }
        else
        {
            CONTEXT_CaptureContext(lpContext);
            ret = TRUE;
        }
    }
    else
    {
        ASSERT("Cross-process GetThreadContext() is not supported on this platform\n");
        SetLastError(ERROR_NOACCESS);
    }

EXIT:
    return ret;
}

/*++
Function:
  SetThreadContextOnPort

  Helper for CONTEXT_SetThreadContext
--*/
kern_return_t
CONTEXT_SetThreadContextOnPort(
           mach_port_t Port,
           IN CONST CONTEXT *lpContext)
{
    kern_return_t MachRet = KERN_SUCCESS;
    mach_msg_type_number_t StateCount;
    thread_state_flavor_t StateFlavor;

    if (lpContext->ContextFlags & (CONTEXT_CONTROL|CONTEXT_INTEGER)) 
    {
#ifdef _X86_
        x86_thread_state32_t State;
        StateFlavor = x86_THREAD_STATE32;
        
        State.eax = lpContext->Eax;
        State.ebx = lpContext->Ebx;
        State.ecx = lpContext->Ecx;
        State.edx = lpContext->Edx;
        State.edi = lpContext->Edi;
        State.esi = lpContext->Esi;
        State.ebp = lpContext->Ebp;
        State.esp = lpContext->Esp;
        State.ss = lpContext->SegSs;
        State.eflags = lpContext->EFlags;
        State.eip = lpContext->Eip;
        State.cs = lpContext->SegCs;
        State.ds = lpContext->SegDs_PAL_Undefined;
        State.es = lpContext->SegEs_PAL_Undefined;
        State.fs = lpContext->SegFs_PAL_Undefined;
        State.gs = lpContext->SegGs_PAL_Undefined;
#elif defined(_AMD64_)
        x86_thread_state64_t State;
        StateFlavor = x86_THREAD_STATE64;

        State.__rax = lpContext->Rax;
        State.__rbx = lpContext->Rbx;
        State.__rcx = lpContext->Rcx;
        State.__rdx = lpContext->Rdx;
        State.__rdi = lpContext->Rdi;
        State.__rsi = lpContext->Rsi;
        State.__rbp = lpContext->Rbp;
        State.__rsp = lpContext->Rsp;
        State.__r8 = lpContext->R8;
        State.__r9 = lpContext->R9;
        State.__r10 = lpContext->R10;
        State.__r11 = lpContext->R11;
        State.__r12 = lpContext->R12;
        State.__r13 = lpContext->R13;
        State.__r14 = lpContext->R14;
        State.__r15 = lpContext->R15;
//        State.ss = lpContext->SegSs;
        State.__rflags = lpContext->EFlags;
        State.__rip = lpContext->Rip;
        State.__cs = lpContext->SegCs;
//        State.ds = lpContext->SegDs_PAL_Undefined;
//        State.es = lpContext->SegEs_PAL_Undefined;
        State.__fs = lpContext->SegFs;
        State.__gs = lpContext->SegGs;
#else
#error Unexpected architecture.
#endif

        StateCount = sizeof(State) / sizeof(natural_t);

        MachRet = thread_set_state(Port,
                                   StateFlavor,
                                   (thread_state_t)&State,
                                   StateCount);
        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("thread_set_state(THREAD_STATE) failed: %d\n", MachRet);
            goto EXIT;
        }
    }

    if (lpContext->ContextFlags & CONTEXT_ALL_FLOATING)
    {
        
#ifdef _X86_
        x86_float_state32_t State;
        StateFlavor = x86_FLOAT_STATE32;
#elif defined(_AMD64_)
        x86_float_state64_t State;
        StateFlavor = x86_FLOAT_STATE64;
#else
#error Unexpected architecture.
#endif

        StateCount = sizeof(State) / sizeof(natural_t);

        // If we're setting only one of the floating point or extended registers (of which Mach supports only
        // the xmm values) then we don't have values for the other set. This is a problem since Mach only
        // supports setting both groups as a single unit. So in this case we'll need to fetch the current
        // values first.
        if ((lpContext->ContextFlags & CONTEXT_ALL_FLOATING) !=
            CONTEXT_ALL_FLOATING)
        {
            mach_msg_type_number_t StateCountGet = StateCount;
            MachRet = thread_get_state(Port,
                                       StateFlavor,
                                       (thread_state_t)&State,
                                       &StateCountGet);
            if (MachRet != KERN_SUCCESS)
            {
                ASSERT("thread_get_state(FLOAT_STATE) failed: %d\n", MachRet);
                goto EXIT;
            }
            _ASSERTE(StateCountGet == StateCount);
        }

        if (lpContext->ContextFlags & CONTEXT_FLOATING_POINT)
        {
#ifdef _X86_
            *(DWORD*)&State.fpu_fcw = lpContext->FloatSave.ControlWord;
            *(DWORD*)&State.fpu_fsw = lpContext->FloatSave.StatusWord;
            State.fpu_ftw = lpContext->FloatSave.TagWord;
            State.fpu_ip = lpContext->FloatSave.ErrorOffset;
            State.fpu_cs = lpContext->FloatSave.ErrorSelector;
            State.fpu_dp = lpContext->FloatSave.DataOffset;
            State.fpu_ds = lpContext->FloatSave.DataSelector;
            State.fpu_mxcsr = lpContext->FloatSave.Cr0NpxState;

            // Windows stores the floating point registers in a packed layout (each 10-byte register end to
            // end for a total of 80 bytes). But Mach returns each register in an 16-bit structure (presumably
            // for alignment purposes). So we can't just memcpy the registers over in a single block, we need
            // to copy them individually.
            for (int i = 0; i < 8; i++)
                memcpy((&State.fpu_stmm0)[i].mmst_reg, &lpContext->FloatSave.RegisterArea[i * 10], 10);
#elif defined(_AMD64_)
            *(DWORD*)&State.__fpu_fcw = lpContext->FltSave.ControlWord;
            *(DWORD*)&State.__fpu_fsw = lpContext->FltSave.StatusWord;
            State.__fpu_ftw = lpContext->FltSave.TagWord;
            State.__fpu_ip = lpContext->FltSave.ErrorOffset;
            State.__fpu_cs = lpContext->FltSave.ErrorSelector;
            State.__fpu_dp = lpContext->FltSave.DataOffset;
            State.__fpu_ds = lpContext->FltSave.DataSelector;
            State.__fpu_mxcsr = lpContext->FltSave.MxCsr;
            State.__fpu_mxcsrmask = lpContext->FltSave.MxCsr_Mask; // note: we don't save the mask for x86

            // Windows stores the floating point registers in a packed layout (each 10-byte register end to
            // end for a total of 80 bytes). But Mach returns each register in an 16-bit structure (presumably
            // for alignment purposes). So we can't just memcpy the registers over in a single block, we need
            // to copy them individually.
            for (int i = 0; i < 8; i++)
                memcpy((&State.__fpu_stmm0)[i].__mmst_reg, &lpContext->FltSave.FloatRegisters[i], 10);

            memcpy(&State.__fpu_xmm0, &lpContext->Xmm0, 8 * 16);
#else
#error Unexpected architecture.
#endif
        }

#ifdef _X86_
        if (lpContext->ContextFlags & CONTEXT_EXTENDED_REGISTERS)
        {
            // The only extended register information that Mach will tell us about are the xmm register
            // values. Both Windows and Mach store the registers in a packed layout (each of the 8 registers
            // is 16 bytes) so we can simply memcpy them across.
            memcpy(&State.fpu_xmm0, lpContext->ExtendedRegisters + CONTEXT_EXREG_XMM_OFFSET, 8 * 16);
        }
#endif // _X86_

        MachRet = thread_set_state(Port,
                                   StateFlavor,
                                   (thread_state_t)&State,
                                   StateCount);
        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("thread_set_state(FLOAT_STATE) failed: %d\n", MachRet);
            goto EXIT;
        }
    }    

EXIT:
    return MachRet;
}

/*++
Function:
  SetThreadContext

See MSDN doc.
--*/
BOOL
CONTEXT_SetThreadContext(
           DWORD dwProcessId,
#if !defined(_AMD64_)
           DWORD dwThreadId,
#else // defined(_AMD64_)
           DWORD64 dwThreadId,
#endif // !defined(_AMD64_)
           DWORD dwLwpId,
           CONST CONTEXT *lpContext)
{
    BOOL ret = FALSE;

    if (lpContext == NULL) 
    {
        ERROR("Invalid lpContext parameter value\n");
        SetLastError(ERROR_NOACCESS);
        goto EXIT;
    }

    if (dwProcessId != GetCurrentProcessId()) 
    {
        // GetThreadContext() of a thread in another process
        ASSERT("Cross-process GetThreadContext() is not supported\n");
        SetLastError(ERROR_NOACCESS);
        goto EXIT;
    }

    if (dwThreadId != THREADSilentGetCurrentThreadId())
    {
        // hThread is in the current process, but isn't the current
        // thread.  Extract the CONTEXT from the Mach thread.

        mach_port_t mptPort;

        mptPort = pthread_mach_thread_np((pthread_t)dwThreadId);
    
        ret = (CONTEXT_SetThreadContextOnPort(mptPort, lpContext) == KERN_SUCCESS);
    } 
    else 
    {
        MachSetThreadContext(const_cast<CONTEXT *>(lpContext));
        ASSERT("MachSetThreadContext should never return\n");
    }

EXIT:
    return ret;
}

#endif // !HAVE_MACH_EXCEPTIONS

/*++
Function:
  DBG_DebugBreak: same as DebugBreak

See MSDN doc.
--*/
VOID
DBG_DebugBreak()
{
    __asm__ __volatile__("int $3");
}


/*++
Function:
  DBG_FlushInstructionCache: processor-specific portion of 
  FlushInstructionCache

See MSDN doc.
--*/
BOOL
DBG_FlushInstructionCache(
                          IN LPCVOID lpBaseAddress,
                          IN SIZE_T dwSize)
{
    // Intel x86 hardware has cache coherency, so nothing needs to be done.
    return TRUE;
}
