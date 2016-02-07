// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/context.h

Abstract:

    Header file for thread context utility functions.



--*/

#ifndef _PAL_CONTEXT_H_
#define _PAL_CONTEXT_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#include <signal.h>
#include <pthread.h>

#if !HAVE_MACH_EXCEPTIONS
/* A type to wrap the native context type, which is ucontext_t on some
 * platforms and another type elsewhere. */
#if HAVE_UCONTEXT_T
#include <ucontext.h>

typedef ucontext_t native_context_t;
#else   // HAVE_UCONTEXT_T
#error Native context type is not known on this platform!
#endif  // HAVE_UCONTEXT_T
#else // !HAVE_MACH_EXCEPTIONS
#include <mach/kern_return.h>
#include <mach/mach_port.h>
#endif // !HAVE_MACH_EXCEPTIONS else

#if HAVE___GREGSET_T

#ifdef BIT64
#define MCREG_Rbx(mc)       ((mc).__gregs[_REG_RBX])
#define MCREG_Rcx(mc)       ((mc).__gregs[_REG_RCX])
#define MCREG_Rdx(mc)       ((mc).__gregs[_REG_RDX])
#define MCREG_Rsi(mc)       ((mc).__gregs[_REG_RSI])
#define MCREG_Rdi(mc)       ((mc).__gregs[_REG_RDI])
#define MCREG_Rbp(mc)       ((mc).__gregs[_REG_RBP])
#define MCREG_Rax(mc)       ((mc).__gregs[_REG_RAX])
#define MCREG_Rip(mc)       ((mc).__gregs[_REG_RIP])
#define MCREG_Rsp(mc)       ((mc).__gregs[_REG_RSP])
#define MCREG_SegCs(mc)     ((mc).__gregs[_REG_CS])
#define MCREG_SegSs(mc)     ((mc).__gregs[_REG_SS])
#define MCREG_R8(mc)        ((mc).__gregs[_REG_R8])
#define MCREG_R9(mc)        ((mc).__gregs[_REG_R9])
#define MCREG_R10(mc)       ((mc).__gregs[_REG_R10])
#define MCREG_R11(mc)       ((mc).__gregs[_REG_R11])
#define MCREG_R12(mc)       ((mc).__gregs[_REG_R12])
#define MCREG_R13(mc)       ((mc).__gregs[_REG_R13])
#define MCREG_R14(mc)       ((mc).__gregs[_REG_R14])
#define MCREG_R15(mc)       ((mc).__gregs[_REG_R15])
#define MCREG_EFlags(mc)    ((mc).__gregs[_REG_RFLAGS])

#else // BIT64

#define MCREG_Ebx(mc)       ((mc).__gregs[_REG_EBX])
#define MCREG_Ecx(mc)       ((mc).__gregs[_REG_ECX])
#define MCREG_Edx(mc)       ((mc).__gregs[_REG_EDX])
#define MCREG_Esi(mc)       ((mc).__gregs[_REG_ESI])
#define MCREG_Edi(mc)       ((mc).__gregs[_REG_EDI])
#define MCREG_Ebp(mc)       ((mc).__gregs[_REG_EBP])
#define MCREG_Eax(mc)       ((mc).__gregs[_REG_EAX])
#define MCREG_Eip(mc)       ((mc).__gregs[_REG_EIP])
#define MCREG_Esp(mc)       ((mc).__gregs[_REG_ESP])
#define MCREG_SegCs(mc)     ((mc).__gregs[_REG_CS])
#define MCREG_SegSs(mc)     ((mc).__gregs[_REG_SS])
#define MCREG_EFlags(mc)    ((mc).__gregs[_REG_RFLAGS])

#endif // BIT64

#elif HAVE_GREGSET_T

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

#define FPREG_Xmm(uc, index) *(M128A*)&((uc)->uc_mcontext.fpregs->_xmm[index])

#define FPREG_St(uc, index) *(M128A*)&((uc)->uc_mcontext.fpregs->_st[index])

#define FPREG_ControlWord(uc) ((uc)->uc_mcontext.fpregs->cwd)
#define FPREG_StatusWord(uc) ((uc)->uc_mcontext.fpregs->swd)
#define FPREG_TagWord(uc) ((uc)->uc_mcontext.fpregs->ftw)
#define FPREG_ErrorOffset(uc) *(DWORD*)&((uc)->uc_mcontext.fpregs->rip)
#define FPREG_ErrorSelector(uc) *(((WORD*)&((uc)->uc_mcontext.fpregs->rip)) + 2)
#define FPREG_DataOffset(uc) *(DWORD*)&((uc)->uc_mcontext.fpregs->rdp)
#define FPREG_DataSelector(uc) *(((WORD*)&((uc)->uc_mcontext.fpregs->rdp)) + 2)
#define FPREG_MxCsr(uc) ((uc)->uc_mcontext.fpregs->mxcsr)
#define FPREG_MxCsr_Mask(uc) ((uc)->uc_mcontext.fpregs->mxcr_mask)

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

#ifdef BIT64

#if defined(_ARM64_)
#define MCREG_X0(mc)      ((mc).regs[0])
#define MCREG_X1(mc)      ((mc).regs[1])
#define MCREG_X2(mc)      ((mc).regs[2])
#define MCREG_X3(mc)      ((mc).regs[3])
#define MCREG_X4(mc)      ((mc).regs[4])
#define MCREG_X5(mc)      ((mc).regs[5])
#define MCREG_X6(mc)      ((mc).regs[6])
#define MCREG_X7(mc)      ((mc).regs[7])
#define MCREG_X8(mc)      ((mc).regs[8])
#define MCREG_X9(mc)      ((mc).regs[9])
#define MCREG_X10(mc)     ((mc).regs[10])
#define MCREG_X11(mc)     ((mc).regs[11])
#define MCREG_X12(mc)     ((mc).regs[12])
#define MCREG_X13(mc)     ((mc).regs[13])
#define MCREG_X14(mc)     ((mc).regs[14])
#define MCREG_X15(mc)     ((mc).regs[15])
#define MCREG_X16(mc)     ((mc).regs[16])
#define MCREG_X17(mc)     ((mc).regs[17])
#define MCREG_X18(mc)     ((mc).regs[18])
#define MCREG_X19(mc)     ((mc).regs[19])
#define MCREG_X20(mc)     ((mc).regs[20])
#define MCREG_X21(mc)     ((mc).regs[21])
#define MCREG_X22(mc)     ((mc).regs[22])
#define MCREG_X23(mc)     ((mc).regs[23])
#define MCREG_X24(mc)     ((mc).regs[24])
#define MCREG_X25(mc)     ((mc).regs[25])
#define MCREG_X26(mc)     ((mc).regs[26])
#define MCREG_X27(mc)     ((mc).regs[27])
#define MCREG_X28(mc)     ((mc).regs[28])
#define MCREG_Fp(mc)      ((mc).regs[29])
#define MCREG_Lr(mc)      ((mc).regs[30])

#define MCREG_Sp(mc)      ((mc).sp)
#define MCREG_Pc(mc)      ((mc).pc)
#define MCREG_PState(mc)  ((mc).pstate)
#define MCREG_Cpsr(mc)    ((mc).cpsr)
#else
    // For FreeBSD, as found in x86/ucontext.h
#define MCREG_Rbp(mc)	    ((mc).mc_rbp)
#define MCREG_Rip(mc)	    ((mc).mc_rip)
#define MCREG_Rsp(mc)	    ((mc).mc_rsp)
#define MCREG_Rsi(mc)       ((mc).mc_rsi)
#define MCREG_Rdi(mc)	    ((mc).mc_rdi)
#define MCREG_Rbx(mc)	    ((mc).mc_rbx)
#define MCREG_Rdx(mc)	    ((mc).mc_rdx)
#define MCREG_Rcx(mc)	    ((mc).mc_rcx)
#define MCREG_Rax(mc)	    ((mc).mc_rax)
#define MCREG_R8(mc)	    ((mc).mc_r8)
#define MCREG_R9(mc)	    ((mc).mc_r9)
#define MCREG_R10(mc)	    ((mc).mc_r10)
#define MCREG_R11(mc)	    ((mc).mc_r11)
#define MCREG_R12(mc)	    ((mc).mc_r12)
#define MCREG_R13(mc)	    ((mc).mc_r13)
#define MCREG_R14(mc)	    ((mc).mc_r14)
#define MCREG_R15(mc)	    ((mc).mc_r15)
#define MCREG_EFlags(mc)    ((mc).mc_rflags)
#define MCREG_SegCs(mc)     ((mc).mc_cs)

  // from x86/fpu.h: struct __envxmm64
#define FPSTATE(uc)             ((savefpu*)((uc)->uc_mcontext.mc_fpstate))
#define FPREG_ControlWord(uc)   FPSTATE(uc)->sv_env.en_cw
#define FPREG_StatusWord(uc)    FPSTATE(uc)->sv_env.en_sw
#define FPREG_TagWord(uc)       FPSTATE(uc)->sv_env.en_tw
#define FPREG_MxCsr(uc)         FPSTATE(uc)->sv_env.en_mxcsr
#define FPREG_MxCsr_Mask(uc)    FPSTATE(uc)->sv_env.en_mxcsr_mask
#define FPREG_ErrorOffset(uc)   *(DWORD*) &(FPSTATE(uc)->sv_env.en_rip)
#define FPREG_ErrorSelector(uc) *((WORD*) &(FPSTATE(uc)->sv_env.en_rip) + 2)
#define FPREG_DataOffset(uc)    *(DWORD*) &(FPSTATE(uc)->sv_env.en_rdp)
#define FPREG_DataSelector(uc)  *((WORD*) &(FPSTATE(uc)->sv_env.en_rdp) + 2)

#define FPREG_Xmm(uc, index)    *(M128A*) &(FPSTATE(uc)->sv_xmm[index])
#define FPREG_St(uc, index)     *(M128A*) &(FPSTATE(uc)->sv_fp[index].fp_acc)
#endif

#else // BIT64

#if defined(_ARM_)

#define MCREG_R0(mc)        ((mc).arm_r0)
#define MCREG_R1(mc)        ((mc).arm_r1)
#define MCREG_R2(mc)        ((mc).arm_r2)
#define MCREG_R3(mc)        ((mc).arm_r3)
#define MCREG_R4(mc)        ((mc).arm_r4)
#define MCREG_R5(mc)        ((mc).arm_r5)
#define MCREG_R6(mc)        ((mc).arm_r6)
#define MCREG_R7(mc)        ((mc).arm_r7)
#define MCREG_R8(mc)        ((mc).arm_r8)
#define MCREG_R9(mc)        ((mc).arm_r9)
#define MCREG_R10(mc)       ((mc).arm_r10)
#define MCREG_R11(mc)       ((mc).arm_fp)
#define MCREG_R12(mc)       ((mc).arm_ip)
#define MCREG_Sp(mc)        ((mc).arm_sp)
#define MCREG_Lr(mc)        ((mc).arm_lr)
#define MCREG_Pc(mc)        ((mc).arm_pc)
#define MCREG_Cpsr(mc)      ((mc).arm_cpsr)

#elif defined(_X86_)

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

#else
#error "Unsupported arch"
#endif

#endif // BIT64

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

#if defined(_ARM_)
#define PTREG_R0(ptreg)        ((ptreg).uregs[0])
#define PTREG_R1(ptreg)        ((ptreg).uregs[1])
#define PTREG_R2(ptreg)        ((ptreg).uregs[2])
#define PTREG_R3(ptreg)        ((ptreg).uregs[3])
#define PTREG_R4(ptreg)        ((ptreg).uregs[4])
#define PTREG_R5(ptreg)        ((ptreg).uregs[5])
#define PTREG_R6(ptreg)        ((ptreg).uregs[6])
#define PTREG_R7(ptreg)        ((ptreg).uregs[7])
#define PTREG_R8(ptreg)        ((ptreg).uregs[8])
#define PTREG_R9(ptreg)        ((ptreg).uregs[9])
#define PTREG_R10(ptreg)       ((ptreg).uregs[10])
#define PTREG_R11(ptreg)       ((ptreg).uregs[11])
#define PTREG_R12(ptreg)       ((ptreg).uregs[12])
#define PTREG_Sp(ptreg)        ((ptreg).uregs[13])
#define PTREG_Lr(ptreg)        ((ptreg).uregs[14])
#define PTREG_Pc(ptreg)        ((ptreg).uregs[15])
#define PTREG_Cpsr(ptreg)      ((ptreg).uregs[16])
#elif defined(_X86_)
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
#else
#error "Unsupported arch"
#endif

#endif // BIT64


#define PTREG_EFlags(ptreg) ((ptreg).eflags)

#endif // HAVE_PT_REGS



#if HAVE_BSD_REGS_T

#ifndef BSD_REGS_STYLE
#error "struct reg" has unrecognized format
#endif

#ifdef BIT64

#define BSDREG_Rbx(reg)     BSD_REGS_STYLE(reg,RBX,rbx)
#define BSDREG_Rcx(reg)     BSD_REGS_STYLE(reg,RCX,rcx)
#define BSDREG_Rdx(reg)     BSD_REGS_STYLE(reg,RDX,rdx)
#define BSDREG_Rsi(reg)     BSD_REGS_STYLE(reg,RSI,rsi)
#define BSDREG_Rdi(reg)     BSD_REGS_STYLE(reg,RDI,rdi)
#define BSDREG_Rbp(reg)     BSD_REGS_STYLE(reg,RBP,rbp)
#define BSDREG_Rax(reg)     BSD_REGS_STYLE(reg,RAX,rax)
#define BSDREG_Rip(reg)     BSD_REGS_STYLE(reg,RIP,rip)
#define BSDREG_SegCs(reg)   BSD_REGS_STYLE(reg,CS,cs)
#define BSDREG_SegSs(reg)   BSD_REGS_STYLE(reg,SS,ss)
#define BSDREG_Rsp(reg)     BSD_REGS_STYLE(reg,RSP,rsp)
#define BSDREG_R8(reg)      BSD_REGS_STYLE(reg,R8,r8)
#define BSDREG_R9(reg)      BSD_REGS_STYLE(reg,R9,r9)
#define BSDREG_R10(reg)     BSD_REGS_STYLE(reg,R10,r10)
#define BSDREG_R11(reg)     BSD_REGS_STYLE(reg,R11,r11)
#define BSDREG_R12(reg)     BSD_REGS_STYLE(reg,R12,r12)
#define BSDREG_R13(reg)     BSD_REGS_STYLE(reg,R13,r13)
#define BSDREG_R14(reg)     BSD_REGS_STYLE(reg,R14,r14)
#define BSDREG_R15(reg)     BSD_REGS_STYLE(reg,R15,r15)
#define BSDREG_EFlags(reg)  BSD_REGS_STYLE(reg,RFLAGS,rflags)

#else // BIT64

#define BSDREG_Ebx(reg)     BSD_REGS_STYLE(reg,EBX,ebx)
#define BSDREG_Ecx(reg)     BSD_REGS_STYLE(reg,ECX,ecx)
#define BSDREG_Edx(reg)     BSD_REGS_STYLE(reg,EDX,edx)
#define BSDREG_Esi(reg)     BSD_REGS_STYLE(reg,ESI,esi)
#define BSDREG_Edi(reg)     BSD_REGS_STYLE(reg,EDI,edi)
#define BSDREG_Ebp(reg)     BSD_REGS_STYLE(reg,EDP,ebp)
#define BSDREG_Eax(reg)     BSD_REGS_STYLE(reg,EAX,eax)
#define BSDREG_Eip(reg)     BSD_REGS_STYLE(reg,EIP,eip)
#define BSDREG_SegCs(reg)   BSD_REGS_STYLE(reg,CS,cs)
#define BSDREG_EFlags(reg)  BSD_REGS_STYLE(reg,EFLAGS,eflags)
#define BSDREG_Esp(reg)     BSD_REGS_STYLE(reg,ESP,esp)
#define BSDREG_SegSs(reg)   BSD_REGS_STYLE(reg,SS,ss)

#endif // BIT64

#endif // HAVE_BSD_REGS_T

inline static DWORD64 CONTEXTGetPC(LPCONTEXT pContext)
{
#if defined(_AMD64_)
    return pContext->Rip;
#elif defined(_ARM64_) || defined(_ARM_)
    return pContext->Pc;
#else
#error don't know how to get the program counter for this architecture
#endif
}

inline static void CONTEXTSetPC(LPCONTEXT pContext, DWORD64 pc)
{
#if defined(_AMD64_)
    pContext->Rip = pc;
#elif defined(_ARM64_) || defined(_ARM_)
    pContext->Pc = pc;
#else
#error don't know how to set the program counter for this architecture
#endif
}

/*++
Function :
    CONTEXT_CaptureContext

    Captures the context of the caller.
    The returned context is suitable for performing
    a virtual unwind.

Parameters :
    LPCONTEXT lpContext : new context

--*/
void
CONTEXT_CaptureContext(
    LPCONTEXT lpContext
    );

/*++
Function :
    CONTEXT_SetThreadContext

    Processor-dependent implementation of SetThreadContext

Parameters :
    HANDLE hThread : thread whose context is to be set
    CONTEXT *lpContext : new context

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL
CONTEXT_SetThreadContext(
    DWORD dwProcessId,
    pthread_t self,
    CONST CONTEXT *lpContext
    );

/*++
Function :
    CONTEXT_GetThreadContext

    Processor-dependent implementation of GetThreadContext

Parameters :
    HANDLE hThread : thread whose context is to retrieved
    LPCONTEXT lpContext  : destination for thread's context

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL
CONTEXT_GetThreadContext(
         DWORD dwProcessId,
         pthread_t self,
         LPCONTEXT lpContext);

#if HAVE_MACH_EXCEPTIONS
/*++
Function:
  CONTEXT_GetThreadContextFromPort

  Helper for GetThreadContext that uses a mach_port
--*/
kern_return_t
CONTEXT_GetThreadContextFromPort(
    mach_port_t Port,
    LPCONTEXT lpContext);

/*++
Function:
  SetThreadContextOnPort

  Helper for CONTEXT_SetThreadContext
--*/
kern_return_t
CONTEXT_SetThreadContextOnPort(
   mach_port_t Port,
   IN CONST CONTEXT *lpContext);

/*++
Function:
  GetThreadContextFromThreadState

  Helper for mach exception support
--*/
void
CONTEXT_GetThreadContextFromThreadState(
    thread_state_flavor_t stateFlavor,
    thread_state_t threadState,
    LPCONTEXT lpContext);

#else // HAVE_MACH_EXCEPTIONS
/*++
Function :
    CONTEXTToNativeContext
    
    Converts a CONTEXT record to a native context.

Parameters :
    CONST CONTEXT *lpContext : CONTEXT to convert, including 
                               flags that determine which registers are valid in
                               lpContext and which ones to set in native
    native_context_t *native : native context to fill in

Return value :
    None

--*/
void CONTEXTToNativeContext(CONST CONTEXT *lpContext, native_context_t *native);

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
                              ULONG contextFlags);

/*++
Function :
    GetNativeContextPC
    
    Returns the program counter from the native context.

Parameters :
    const native_context_t *context : native context

Return value :
    The program counter from the native context.

--*/
LPVOID GetNativeContextPC(const native_context_t *context);

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
DWORD CONTEXTGetExceptionCodeForSignal(const siginfo_t *siginfo,
                                       const native_context_t *context);

#endif  // HAVE_MACH_EXCEPTIONS else

#ifdef __cplusplus
}
#endif // __cplusplus

#endif  // _PAL_CONTEXT_H_
