// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    seh-unwind.cpp

Abstract:

    Implementation of exception API functions based on
    the Unwind API.



--*/

#ifdef HOST_UNIX
#include "pal/context.h"
#include "pal.h"
#include <dlfcn.h>

#define UNW_LOCAL_ONLY
// Sub-headers included from the libunwind.h contain an empty struct
// and clang issues a warning. Until the libunwind is fixed, disable
// the warning.
#ifdef __llvm__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wextern-c-compat"
#endif
#include <libunwind.h>
#ifdef __llvm__
#pragma clang diagnostic pop
#endif
#else // HOST_UNIX

#include <windows.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <libunwind.h>
#include "debugmacros.h"
#include "crosscomp.h"

#define KNONVOLATILE_CONTEXT_POINTERS T_KNONVOLATILE_CONTEXT_POINTERS
#define CONTEXT T_CONTEXT

#define ASSERT(x, ...)
#define TRACE(x, ...)

#define PALAPI

#endif // HOST_UNIX

#if defined(TARGET_OSX) && defined(HOST_ARM64) && !defined(HAVE_UNW_AARCH64_X19)
// MacOS uses ARM64 instead of AARCH64 to describe these registers
// Create aliases to reuse more code
enum
{
    UNW_AARCH64_X19 = UNW_ARM64_X19,
    UNW_AARCH64_X20 = UNW_ARM64_X20,
    UNW_AARCH64_X21 = UNW_ARM64_X21,
    UNW_AARCH64_X22 = UNW_ARM64_X22,
    UNW_AARCH64_X23 = UNW_ARM64_X23,
    UNW_AARCH64_X24 = UNW_ARM64_X24,
    UNW_AARCH64_X25 = UNW_ARM64_X25,
    UNW_AARCH64_X26 = UNW_ARM64_X26,
    UNW_AARCH64_X27 = UNW_ARM64_X27,
    UNW_AARCH64_X28 = UNW_ARM64_X28,
    UNW_AARCH64_X29 = UNW_ARM64_X29,
    UNW_AARCH64_X30 = UNW_ARM64_X30,
    UNW_AARCH64_V8 = UNW_ARM64_D8,
    UNW_AARCH64_V9 = UNW_ARM64_D9,
    UNW_AARCH64_V10 = UNW_ARM64_D10,
    UNW_AARCH64_V11 = UNW_ARM64_D11,
    UNW_AARCH64_V12 = UNW_ARM64_D12,
    UNW_AARCH64_V13 = UNW_ARM64_D13,
    UNW_AARCH64_V14 = UNW_ARM64_D14,
    UNW_AARCH64_V15 = UNW_ARM64_D15,
    UNW_AARCH64_V16 = UNW_ARM64_D16,
    UNW_AARCH64_V17 = UNW_ARM64_D17,
    UNW_AARCH64_V18 = UNW_ARM64_D18,
    UNW_AARCH64_V19 = UNW_ARM64_D19,
    UNW_AARCH64_V20 = UNW_ARM64_D20,
    UNW_AARCH64_V21 = UNW_ARM64_D21,
    UNW_AARCH64_V22 = UNW_ARM64_D22,
    UNW_AARCH64_V23 = UNW_ARM64_D23,
    UNW_AARCH64_V24 = UNW_ARM64_D24,
    UNW_AARCH64_V25 = UNW_ARM64_D25,
    UNW_AARCH64_V26 = UNW_ARM64_D26,
    UNW_AARCH64_V27 = UNW_ARM64_D27,
    UNW_AARCH64_V28 = UNW_ARM64_D28,
    UNW_AARCH64_V29 = UNW_ARM64_D29,
    UNW_AARCH64_V30 = UNW_ARM64_D30,
    UNW_AARCH64_V31 = UNW_ARM64_D31
};
#endif // defined(TARGET_OSX) && defined(HOST_ARM64)


//----------------------------------------------------------------------
// Virtual Unwinding
//----------------------------------------------------------------------

#if UNWIND_CONTEXT_IS_UCONTEXT_T

#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Rip)        \
    ASSIGN_REG(Rsp)        \
    ASSIGN_REG(Rbp)        \
    ASSIGN_REG(Rbx)        \
    ASSIGN_REG(R12)        \
    ASSIGN_REG(R13)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Eip)        \
    ASSIGN_REG(Esp)        \
    ASSIGN_REG(Ebp)        \
    ASSIGN_REG(Ebx)        \
    ASSIGN_REG(Esi)        \
    ASSIGN_REG(Edi)
#elif (defined(HOST_UNIX) && defined(HOST_S390X))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(PSWAddr)    \
    ASSIGN_REG(R6)         \
    ASSIGN_REG(R7)         \
    ASSIGN_REG(R8)         \
    ASSIGN_REG(R9)         \
    ASSIGN_REG(R10)        \
    ASSIGN_REG(R11)        \
    ASSIGN_REG(R12)        \
    ASSIGN_REG(R13)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)
#elif (defined(HOST_UNIX) && defined(HOST_LOONGARCH64))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Pc)         \
    ASSIGN_REG(Tp)         \
    ASSIGN_REG(Sp)         \
    ASSIGN_REG(Fp)         \
    ASSIGN_REG(Ra)         \
    ASSIGN_REG(S0)         \
    ASSIGN_REG(S1)         \
    ASSIGN_REG(S2)         \
    ASSIGN_REG(S3)         \
    ASSIGN_REG(S4)         \
    ASSIGN_REG(S5)         \
    ASSIGN_REG(S6)         \
    ASSIGN_REG(S7)         \
    ASSIGN_REG(S8)
#elif (defined(HOST_UNIX) && defined(HOST_RISCV64))

// https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/2d865a2964fe06bfc569ab00c74e152b582ed764/riscv-cc.adoc

#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Ra)         \
    ASSIGN_REG(Sp)         \
    ASSIGN_REG(Gp)         \
    ASSIGN_REG(Tp)         \
    ASSIGN_REG(Pc)         \
    ASSIGN_REG(Fp)         \
    ASSIGN_REG(S1)         \
    ASSIGN_REG(S2)         \
    ASSIGN_REG(S3)         \
    ASSIGN_REG(S4)         \
    ASSIGN_REG(S5)         \
    ASSIGN_REG(S6)         \
    ASSIGN_REG(S7)         \
    ASSIGN_REG(S8)         \
    ASSIGN_REG(S9)         \
    ASSIGN_REG(S10)        \
    ASSIGN_REG(S11)
#elif (defined(HOST_UNIX) && defined(HOST_POWERPC64))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(Nip)        \
    ASSIGN_REG(R14)        \
    ASSIGN_REG(R15)        \
    ASSIGN_REG(R16)        \
    ASSIGN_REG(R17)        \
    ASSIGN_REG(R18)        \
    ASSIGN_REG(R19)        \
    ASSIGN_REG(R20)        \
    ASSIGN_REG(R21)        \
    ASSIGN_REG(R22)        \
    ASSIGN_REG(R23)        \
    ASSIGN_REG(R24)        \
    ASSIGN_REG(R25)        \
    ASSIGN_REG(R26)        \
    ASSIGN_REG(R27)        \
    ASSIGN_REG(R28)        \
    ASSIGN_REG(R29)        \
    ASSIGN_REG(R30)        \
    ASSIGN_REG(R31)
#elif (defined(HOST_ARM64) && defined(TARGET_FREEBSD))
#define ASSIGN_UNWIND_REGS \
    ASSIGN_REG(X0) \
    ASSIGN_REG(X1) \
    ASSIGN_REG(X2) \
    ASSIGN_REG(X3) \
    ASSIGN_REG(X4) \
    ASSIGN_REG(X5) \
    ASSIGN_REG(X6) \
    ASSIGN_REG(X7) \
    ASSIGN_REG(X8) \
    ASSIGN_REG(X9) \
    ASSIGN_REG(X10) \
    ASSIGN_REG(X11) \
    ASSIGN_REG(X12) \
    ASSIGN_REG(X13) \
    ASSIGN_REG(X14) \
    ASSIGN_REG(X15) \
    ASSIGN_REG(X16) \
    ASSIGN_REG(X17) \
    ASSIGN_REG(X18) \
    ASSIGN_REG(X19) \
    ASSIGN_REG(X20) \
    ASSIGN_REG(X21) \
    ASSIGN_REG(X22) \
    ASSIGN_REG(X23) \
    ASSIGN_REG(X24) \
    ASSIGN_REG(X25) \
    ASSIGN_REG(X26) \
    ASSIGN_REG(X27) \
    ASSIGN_REG(X28) \
    ASSIGN_REG(Lr) \
    ASSIGN_REG(Sp) \
    ASSIGN_REG(Pc) \
    ASSIGN_REG(Fp) \
    ASSIGN_REG(Cpsr)
#else
#error unsupported architecture
#endif

static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#define ASSIGN_REG(reg) MCREG_##reg(unwContext->uc_mcontext) = winContext->reg;
    ASSIGN_UNWIND_REGS
#undef ASSIGN_REG
}
#else // UNWIND_CONTEXT_IS_UCONTEXT_T
static void WinContextToUnwindContext(CONTEXT *winContext, unw_context_t *unwContext)
{
#if (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    // Assuming that unw_set_reg() on cursor will point the cursor to the
    // supposed stack frame is dangerous for libunwind-arm in Linux.
    // It is because libunwind's unw_cursor_t has other data structure
    // initialized by unw_init_local(), which are not updated by
    // unw_set_reg().
    unwContext->regs[0] = 0;
    unwContext->regs[1] = 0;
    unwContext->regs[2] = 0;
    unwContext->regs[3] = 0;
    unwContext->regs[4] = winContext->R4;
    unwContext->regs[5] = winContext->R5;
    unwContext->regs[6] = winContext->R6;
    unwContext->regs[7] = winContext->R7;
    unwContext->regs[8] = winContext->R8;
    unwContext->regs[9] = winContext->R9;
    unwContext->regs[10] = winContext->R10;
    unwContext->regs[11] = winContext->R11;
    unwContext->regs[12] = 0;
    unwContext->regs[13] = winContext->Sp;
    unwContext->regs[14] = winContext->Lr;
    unwContext->regs[15] = winContext->Pc;
    for (int i = 0; i < 16; i++)
    {
        unwContext->fpregs[i] = winContext->D[i];
    }
#elif defined(HOST_ARM64) && !defined(TARGET_OSX)
    unwContext->uc_mcontext.pc       = winContext->Pc;
    unwContext->uc_mcontext.sp       = winContext->Sp;
    unwContext->uc_mcontext.regs[29] = winContext->Fp;
    unwContext->uc_mcontext.regs[30] = winContext->Lr;

    unwContext->uc_mcontext.regs[19] = winContext->X19;
    unwContext->uc_mcontext.regs[20] = winContext->X20;
    unwContext->uc_mcontext.regs[21] = winContext->X21;
    unwContext->uc_mcontext.regs[22] = winContext->X22;
    unwContext->uc_mcontext.regs[23] = winContext->X23;
    unwContext->uc_mcontext.regs[24] = winContext->X24;
    unwContext->uc_mcontext.regs[25] = winContext->X25;
    unwContext->uc_mcontext.regs[26] = winContext->X26;
    unwContext->uc_mcontext.regs[27] = winContext->X27;
    unwContext->uc_mcontext.regs[28] = winContext->X28;
    unw_fpsimd_context_t *fp = (unw_fpsimd_context_t *)&unwContext->uc_mcontext.__reserved;
    for (int i = 0; i < 32; i++)
    {
        *(NEON128*) &fp->vregs[i] = winContext->V[i];
    }
#endif
}

static void WinContextToUnwindCursor(CONTEXT *winContext, unw_cursor_t *cursor)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    unw_set_reg(cursor, UNW_REG_IP, winContext->Rip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Rsp);
    unw_set_reg(cursor, UNW_X86_64_RBP, winContext->Rbp);
    unw_set_reg(cursor, UNW_X86_64_RBX, winContext->Rbx);
    unw_set_reg(cursor, UNW_X86_64_R12, winContext->R12);
    unw_set_reg(cursor, UNW_X86_64_R13, winContext->R13);
    unw_set_reg(cursor, UNW_X86_64_R14, winContext->R14);
    unw_set_reg(cursor, UNW_X86_64_R15, winContext->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    unw_set_reg(cursor, UNW_REG_IP, winContext->Eip);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Esp);
    unw_set_reg(cursor, UNW_X86_EBP, winContext->Ebp);
    unw_set_reg(cursor, UNW_X86_EBX, winContext->Ebx);
    unw_set_reg(cursor, UNW_X86_ESI, winContext->Esi);
    unw_set_reg(cursor, UNW_X86_EDI, winContext->Edi);
#elif defined(HOST_ARM64) && defined(TARGET_OSX)
    // unw_cursor_t is an opaque data structure on macOS
    // As noted in WinContextToUnwindContext this didn't work for Linux
    // TBD whether this will work for macOS.
    unw_set_reg(cursor, UNW_REG_IP, winContext->Pc);
    unw_set_reg(cursor, UNW_REG_SP, winContext->Sp);
    unw_set_reg(cursor, UNW_AARCH64_X29, winContext->Fp);
    unw_set_reg(cursor, UNW_AARCH64_X30, winContext->Lr);
    unw_set_reg(cursor, UNW_AARCH64_X19, winContext->X19);
    unw_set_reg(cursor, UNW_AARCH64_X20, winContext->X20);
    unw_set_reg(cursor, UNW_AARCH64_X21, winContext->X21);
    unw_set_reg(cursor, UNW_AARCH64_X22, winContext->X22);
    unw_set_reg(cursor, UNW_AARCH64_X23, winContext->X23);
    unw_set_reg(cursor, UNW_AARCH64_X24, winContext->X24);
    unw_set_reg(cursor, UNW_AARCH64_X25, winContext->X25);
    unw_set_reg(cursor, UNW_AARCH64_X26, winContext->X26);
    unw_set_reg(cursor, UNW_AARCH64_X27, winContext->X27);
    unw_set_reg(cursor, UNW_AARCH64_X28, winContext->X28);
    unw_set_fpreg(cursor, UNW_AARCH64_V8, *(unw_fpreg_t *)&winContext->V[8].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V9, *(unw_fpreg_t *)&winContext->V[9].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V10, *(unw_fpreg_t *)&winContext->V[10].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V11, *(unw_fpreg_t *)&winContext->V[11].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V12, *(unw_fpreg_t *)&winContext->V[12].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V13, *(unw_fpreg_t *)&winContext->V[13].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V14, *(unw_fpreg_t *)&winContext->V[14].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V15, *(unw_fpreg_t *)&winContext->V[15].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V16, *(unw_fpreg_t *)&winContext->V[16].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V17, *(unw_fpreg_t *)&winContext->V[17].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V18, *(unw_fpreg_t *)&winContext->V[18].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V19, *(unw_fpreg_t *)&winContext->V[19].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V20, *(unw_fpreg_t *)&winContext->V[20].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V21, *(unw_fpreg_t *)&winContext->V[21].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V22, *(unw_fpreg_t *)&winContext->V[22].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V23, *(unw_fpreg_t *)&winContext->V[23].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V24, *(unw_fpreg_t *)&winContext->V[24].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V25, *(unw_fpreg_t *)&winContext->V[25].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V26, *(unw_fpreg_t *)&winContext->V[26].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V27, *(unw_fpreg_t *)&winContext->V[27].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V28, *(unw_fpreg_t *)&winContext->V[28].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V29, *(unw_fpreg_t *)&winContext->V[29].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V30, *(unw_fpreg_t *)&winContext->V[30].Low);
    unw_set_fpreg(cursor, UNW_AARCH64_V31, *(unw_fpreg_t *)&winContext->V[31].Low);
#endif
}
#endif // UNWIND_CONTEXT_IS_UCONTEXT_T

void UnwindContextToWinContext(unw_cursor_t *cursor, CONTEXT *winContext)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Rip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Rsp);
    unw_get_reg(cursor, UNW_X86_64_RBP, (unw_word_t *) &winContext->Rbp);
    unw_get_reg(cursor, UNW_X86_64_RBX, (unw_word_t *) &winContext->Rbx);
    unw_get_reg(cursor, UNW_X86_64_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_X86_64_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_X86_64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_X86_64_R15, (unw_word_t *) &winContext->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Eip);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Esp);
    unw_get_reg(cursor, UNW_X86_EBP, (unw_word_t *) &winContext->Ebp);
    unw_get_reg(cursor, UNW_X86_EBX, (unw_word_t *) &winContext->Ebx);
    unw_get_reg(cursor, UNW_X86_ESI, (unw_word_t *) &winContext->Esi);
    unw_get_reg(cursor, UNW_X86_EDI, (unw_word_t *) &winContext->Edi);
#elif (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_ARM_R14, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_ARM_R4, (unw_word_t *) &winContext->R4);
    unw_get_reg(cursor, UNW_ARM_R5, (unw_word_t *) &winContext->R5);
    unw_get_reg(cursor, UNW_ARM_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_ARM_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_ARM_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_ARM_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_ARM_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_ARM_R11, (unw_word_t *) &winContext->R11);
    unw_get_fpreg(cursor, UNW_ARM_D8, (unw_fpreg_t *)&winContext->D[8]);
    unw_get_fpreg(cursor, UNW_ARM_D9, (unw_fpreg_t *)&winContext->D[9]);
    unw_get_fpreg(cursor, UNW_ARM_D10, (unw_fpreg_t *)&winContext->D[10]);
    unw_get_fpreg(cursor, UNW_ARM_D11, (unw_fpreg_t *)&winContext->D[11]);
    unw_get_fpreg(cursor, UNW_ARM_D12, (unw_fpreg_t *)&winContext->D[12]);
    unw_get_fpreg(cursor, UNW_ARM_D13, (unw_fpreg_t *)&winContext->D[13]);
    unw_get_fpreg(cursor, UNW_ARM_D14, (unw_fpreg_t *)&winContext->D[14]);
    unw_get_fpreg(cursor, UNW_ARM_D15, (unw_fpreg_t *)&winContext->D[15]);
#elif (defined(HOST_UNIX) && defined(HOST_ARM64)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM64))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_AARCH64_X29, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_AARCH64_X30, (unw_word_t *) &winContext->Lr);
    unw_get_reg(cursor, UNW_AARCH64_X19, (unw_word_t *) &winContext->X19);
    unw_get_reg(cursor, UNW_AARCH64_X20, (unw_word_t *) &winContext->X20);
    unw_get_reg(cursor, UNW_AARCH64_X21, (unw_word_t *) &winContext->X21);
    unw_get_reg(cursor, UNW_AARCH64_X22, (unw_word_t *) &winContext->X22);
    unw_get_reg(cursor, UNW_AARCH64_X23, (unw_word_t *) &winContext->X23);
    unw_get_reg(cursor, UNW_AARCH64_X24, (unw_word_t *) &winContext->X24);
    unw_get_reg(cursor, UNW_AARCH64_X25, (unw_word_t *) &winContext->X25);
    unw_get_reg(cursor, UNW_AARCH64_X26, (unw_word_t *) &winContext->X26);
    unw_get_reg(cursor, UNW_AARCH64_X27, (unw_word_t *) &winContext->X27);
    unw_get_reg(cursor, UNW_AARCH64_X28, (unw_word_t *) &winContext->X28);
    unw_get_fpreg(cursor, UNW_AARCH64_V8, (unw_fpreg_t*)&winContext->V[8].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V9, (unw_fpreg_t*)&winContext->V[9].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V10, (unw_fpreg_t*)&winContext->V[10].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V11, (unw_fpreg_t*)&winContext->V[11].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V12, (unw_fpreg_t*)&winContext->V[12].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V13, (unw_fpreg_t*)&winContext->V[13].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V14, (unw_fpreg_t*)&winContext->V[14].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V15, (unw_fpreg_t*)&winContext->V[15].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V16, (unw_fpreg_t*)&winContext->V[16].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V17, (unw_fpreg_t*)&winContext->V[17].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V18, (unw_fpreg_t*)&winContext->V[18].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V19, (unw_fpreg_t*)&winContext->V[19].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V20, (unw_fpreg_t*)&winContext->V[20].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V21, (unw_fpreg_t*)&winContext->V[21].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V22, (unw_fpreg_t*)&winContext->V[22].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V23, (unw_fpreg_t*)&winContext->V[23].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V24, (unw_fpreg_t*)&winContext->V[24].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V25, (unw_fpreg_t*)&winContext->V[25].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V26, (unw_fpreg_t*)&winContext->V[26].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V27, (unw_fpreg_t*)&winContext->V[27].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V28, (unw_fpreg_t*)&winContext->V[28].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V29, (unw_fpreg_t*)&winContext->V[29].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V30, (unw_fpreg_t*)&winContext->V[30].Low);
    unw_get_fpreg(cursor, UNW_AARCH64_V31, (unw_fpreg_t*)&winContext->V[31].Low);

#if defined(TARGET_OSX) && defined(TARGET_ARM64)
    // Strip pointer authentication bits which seem to be leaking out of libunwind
    // Seems like ptrauth_strip() / __builtin_ptrauth_strip() should work, but currently
    // errors with "this target does not support pointer authentication"
    winContext->Pc = winContext->Pc & 0x7fffffffffffull;
#endif // defined(TARGET_OSX) && defined(TARGET_ARM64)
#elif (defined(HOST_UNIX) && defined(HOST_S390X))
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->R15);
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->PSWAddr);
    unw_get_reg(cursor, UNW_S390X_R6, (unw_word_t *) &winContext->R6);
    unw_get_reg(cursor, UNW_S390X_R7, (unw_word_t *) &winContext->R7);
    unw_get_reg(cursor, UNW_S390X_R8, (unw_word_t *) &winContext->R8);
    unw_get_reg(cursor, UNW_S390X_R9, (unw_word_t *) &winContext->R9);
    unw_get_reg(cursor, UNW_S390X_R10, (unw_word_t *) &winContext->R10);
    unw_get_reg(cursor, UNW_S390X_R11, (unw_word_t *) &winContext->R11);
    unw_get_reg(cursor, UNW_S390X_R12, (unw_word_t *) &winContext->R12);
    unw_get_reg(cursor, UNW_S390X_R13, (unw_word_t *) &winContext->R13);
    unw_get_reg(cursor, UNW_S390X_R14, (unw_word_t *) &winContext->R14);
#elif (defined(HOST_UNIX) && defined(HOST_LOONGARCH64))
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R1, (unw_word_t *) &winContext->Ra);
    unw_get_reg(cursor, UNW_LOONGARCH64_R2, (unw_word_t *) &winContext->Tp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R22, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_LOONGARCH64_R23, (unw_word_t *) &winContext->S0);
    unw_get_reg(cursor, UNW_LOONGARCH64_R24, (unw_word_t *) &winContext->S1);
    unw_get_reg(cursor, UNW_LOONGARCH64_R25, (unw_word_t *) &winContext->S2);
    unw_get_reg(cursor, UNW_LOONGARCH64_R26, (unw_word_t *) &winContext->S3);
    unw_get_reg(cursor, UNW_LOONGARCH64_R27, (unw_word_t *) &winContext->S4);
    unw_get_reg(cursor, UNW_LOONGARCH64_R28, (unw_word_t *) &winContext->S5);
    unw_get_reg(cursor, UNW_LOONGARCH64_R29, (unw_word_t *) &winContext->S6);
    unw_get_reg(cursor, UNW_LOONGARCH64_R30, (unw_word_t *) &winContext->S7);
    unw_get_reg(cursor, UNW_LOONGARCH64_R31, (unw_word_t *) &winContext->S8);
#elif (defined(HOST_UNIX) && defined(HOST_RISCV64))
    // https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/2d865a2964fe06bfc569ab00c74e152b582ed764/riscv-cc.adoc

    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Pc);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->Sp);
    unw_get_reg(cursor, UNW_RISCV_X1, (unw_word_t *) &winContext->Ra);
    unw_get_reg(cursor, UNW_RISCV_X3, (unw_word_t *) &winContext->Gp);
    unw_get_reg(cursor, UNW_RISCV_X4, (unw_word_t *) &winContext->Tp);
    unw_get_reg(cursor, UNW_RISCV_X8, (unw_word_t *) &winContext->Fp);
    unw_get_reg(cursor, UNW_RISCV_X9, (unw_word_t *) &winContext->S1);
    unw_get_reg(cursor, UNW_RISCV_X18, (unw_word_t *) &winContext->S2);
    unw_get_reg(cursor, UNW_RISCV_X19, (unw_word_t *) &winContext->S3);
    unw_get_reg(cursor, UNW_RISCV_X20, (unw_word_t *) &winContext->S4);
    unw_get_reg(cursor, UNW_RISCV_X21, (unw_word_t *) &winContext->S5);
    unw_get_reg(cursor, UNW_RISCV_X22, (unw_word_t *) &winContext->S6);
    unw_get_reg(cursor, UNW_RISCV_X23, (unw_word_t *) &winContext->S7);
    unw_get_reg(cursor, UNW_RISCV_X24, (unw_word_t *) &winContext->S8);
    unw_get_reg(cursor, UNW_RISCV_X25, (unw_word_t *) &winContext->S9);
    unw_get_reg(cursor, UNW_RISCV_X26, (unw_word_t *) &winContext->S10);
    unw_get_reg(cursor, UNW_RISCV_X27, (unw_word_t *) &winContext->S11);
#elif (defined(HOST_UNIX) && defined(HOST_POWERPC64))
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &winContext->R31);
    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &winContext->Nip);
    unw_get_reg(cursor, UNW_PPC64_R14, (unw_word_t *) &winContext->R14);
    unw_get_reg(cursor, UNW_PPC64_R15, (unw_word_t *) &winContext->R15);
    unw_get_reg(cursor, UNW_PPC64_R16, (unw_word_t *) &winContext->R16);
    unw_get_reg(cursor, UNW_PPC64_R17, (unw_word_t *) &winContext->R17);
    unw_get_reg(cursor, UNW_PPC64_R18, (unw_word_t *) &winContext->R18);
    unw_get_reg(cursor, UNW_PPC64_R19, (unw_word_t *) &winContext->R19);
    unw_get_reg(cursor, UNW_PPC64_R20, (unw_word_t *) &winContext->R20);
    unw_get_reg(cursor, UNW_PPC64_R21, (unw_word_t *) &winContext->R21);
    unw_get_reg(cursor, UNW_PPC64_R22, (unw_word_t *) &winContext->R22);
    unw_get_reg(cursor, UNW_PPC64_R23, (unw_word_t *) &winContext->R23);
    unw_get_reg(cursor, UNW_PPC64_R24, (unw_word_t *) &winContext->R24);
    unw_get_reg(cursor, UNW_PPC64_R25, (unw_word_t *) &winContext->R25);
    unw_get_reg(cursor, UNW_PPC64_R26, (unw_word_t *) &winContext->R26);
    unw_get_reg(cursor, UNW_PPC64_R27, (unw_word_t *) &winContext->R27);
    unw_get_reg(cursor, UNW_PPC64_R28, (unw_word_t *) &winContext->R28);
    unw_get_reg(cursor, UNW_PPC64_R29, (unw_word_t *) &winContext->R29);
    unw_get_reg(cursor, UNW_PPC64_R30, (unw_word_t *) &winContext->R30);
#else
#error unsupported architecture
#endif
}

static void GetContextPointer(unw_cursor_t *cursor, unw_context_t *unwContext, int reg, SIZE_T **contextPointer)
{
#if defined(HAVE_UNW_GET_SAVE_LOC)
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        SIZE_T *pLoc = (SIZE_T *)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext
        if (unwContext == NULL || (pLoc < (SIZE_T *)unwContext) || ((SIZE_T *)(unwContext + 1) <= pLoc))
            *contextPointer = (SIZE_T *)saveLoc.u.addr;
    }
#else
    // Returning NULL indicates that we don't have context pointers available
    *contextPointer = NULL;
#endif
}

void GetContextPointers(unw_cursor_t *cursor, unw_context_t *unwContext, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
#if (defined(HOST_UNIX) && defined(HOST_AMD64)) || (defined(HOST_WINDOWS) && defined(TARGET_AMD64))
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBP, (SIZE_T **)&contextPointers->Rbp);
    GetContextPointer(cursor, unwContext, UNW_X86_64_RBX, (SIZE_T **)&contextPointers->Rbx);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R12, (SIZE_T **)&contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R13, (SIZE_T **)&contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R14, (SIZE_T **)&contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_X86_64_R15, (SIZE_T **)&contextPointers->R15);
#elif (defined(HOST_UNIX) && defined(HOST_X86)) || (defined(HOST_WINDOWS) && defined(TARGET_X86))
    GetContextPointer(cursor, unwContext, UNW_X86_EBX, &contextPointers->Ebx);
    GetContextPointer(cursor, unwContext, UNW_X86_EBP, &contextPointers->Ebp);
    GetContextPointer(cursor, unwContext, UNW_X86_ESI, &contextPointers->Esi);
    GetContextPointer(cursor, unwContext, UNW_X86_EDI, &contextPointers->Edi);
#elif (defined(HOST_UNIX) && defined(HOST_ARM)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM))
    GetContextPointer(cursor, unwContext, UNW_ARM_R4, &contextPointers->R4);
    GetContextPointer(cursor, unwContext, UNW_ARM_R5, &contextPointers->R5);
    GetContextPointer(cursor, unwContext, UNW_ARM_R6, &contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_ARM_R7, &contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_ARM_R8, &contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_ARM_R9, &contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_ARM_R10, &contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_ARM_R11, &contextPointers->R11);
    GetContextPointer(cursor, unwContext, UNW_ARM_D8, (SIZE_T **)&contextPointers->D8);
    GetContextPointer(cursor, unwContext, UNW_ARM_D9, (SIZE_T **)&contextPointers->D9);
    GetContextPointer(cursor, unwContext, UNW_ARM_D10, (SIZE_T **)&contextPointers->D10);
    GetContextPointer(cursor, unwContext, UNW_ARM_D11, (SIZE_T **)&contextPointers->D11);
    GetContextPointer(cursor, unwContext, UNW_ARM_D12, (SIZE_T **)&contextPointers->D12);
    GetContextPointer(cursor, unwContext, UNW_ARM_D13, (SIZE_T **)&contextPointers->D13);
    GetContextPointer(cursor, unwContext, UNW_ARM_D14, (SIZE_T **)&contextPointers->D14);
    GetContextPointer(cursor, unwContext, UNW_ARM_D15, (SIZE_T **)&contextPointers->D15);
#elif (defined(HOST_UNIX) && defined(HOST_ARM64)) || (defined(HOST_WINDOWS) && defined(TARGET_ARM64))
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X19, (SIZE_T**)&contextPointers->X19);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X20, (SIZE_T**)&contextPointers->X20);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X21, (SIZE_T**)&contextPointers->X21);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X22, (SIZE_T**)&contextPointers->X22);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X23, (SIZE_T**)&contextPointers->X23);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X24, (SIZE_T**)&contextPointers->X24);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X25, (SIZE_T**)&contextPointers->X25);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X26, (SIZE_T**)&contextPointers->X26);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X27, (SIZE_T**)&contextPointers->X27);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X28, (SIZE_T**)&contextPointers->X28);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_X29, (SIZE_T**)&contextPointers->Fp);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V8, (SIZE_T**)&contextPointers->D8);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V9, (SIZE_T**)&contextPointers->D9);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V10, (SIZE_T**)&contextPointers->D10);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V11, (SIZE_T**)&contextPointers->D11);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V12, (SIZE_T**)&contextPointers->D12);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V13, (SIZE_T**)&contextPointers->D13);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V14, (SIZE_T**)&contextPointers->D14);
    GetContextPointer(cursor, unwContext, UNW_AARCH64_V15, (SIZE_T**)&contextPointers->D15);
#elif (defined(HOST_UNIX) && defined(HOST_S390X))
    GetContextPointer(cursor, unwContext, UNW_S390X_R6, (SIZE_T **)&contextPointers->R6);
    GetContextPointer(cursor, unwContext, UNW_S390X_R7, (SIZE_T **)&contextPointers->R7);
    GetContextPointer(cursor, unwContext, UNW_S390X_R8, (SIZE_T **)&contextPointers->R8);
    GetContextPointer(cursor, unwContext, UNW_S390X_R9, (SIZE_T **)&contextPointers->R9);
    GetContextPointer(cursor, unwContext, UNW_S390X_R10, (SIZE_T **)&contextPointers->R10);
    GetContextPointer(cursor, unwContext, UNW_S390X_R11, (SIZE_T **)&contextPointers->R11);
    GetContextPointer(cursor, unwContext, UNW_S390X_R12, (SIZE_T **)&contextPointers->R12);
    GetContextPointer(cursor, unwContext, UNW_S390X_R13, (SIZE_T **)&contextPointers->R13);
    GetContextPointer(cursor, unwContext, UNW_S390X_R14, (SIZE_T **)&contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_S390X_R15, (SIZE_T **)&contextPointers->R15);
#elif (defined(HOST_UNIX) && defined(HOST_LOONGARCH64))
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R1, (SIZE_T **)&contextPointers->Ra);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R2, (SIZE_T **)&contextPointers->Tp);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R22, (SIZE_T **)&contextPointers->Fp);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R23, (SIZE_T **)&contextPointers->S0);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R24, (SIZE_T **)&contextPointers->S1);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R25, (SIZE_T **)&contextPointers->S2);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R26, (SIZE_T **)&contextPointers->S3);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R27, (SIZE_T **)&contextPointers->S4);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R28, (SIZE_T **)&contextPointers->S5);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R29, (SIZE_T **)&contextPointers->S6);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R30, (SIZE_T **)&contextPointers->S7);
    GetContextPointer(cursor, unwContext, UNW_LOONGARCH64_R31, (SIZE_T **)&contextPointers->S8);
#elif (defined(HOST_UNIX) && defined(HOST_RISCV64))
    // https://github.com/riscv-non-isa/riscv-elf-psabi-doc/blob/2d865a2964fe06bfc569ab00c74e152b582ed764/riscv-cc.adoc

    GetContextPointer(cursor, unwContext, UNW_RISCV_X1, (SIZE_T **)&contextPointers->Ra);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X3, (SIZE_T **)&contextPointers->Gp);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X4, (SIZE_T **)&contextPointers->Tp);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X8, (SIZE_T **)&contextPointers->Fp);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X9, (SIZE_T **)&contextPointers->S1);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X18, (SIZE_T **)&contextPointers->S2);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X19, (SIZE_T **)&contextPointers->S3);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X20, (SIZE_T **)&contextPointers->S4);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X21, (SIZE_T **)&contextPointers->S5);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X22, (SIZE_T **)&contextPointers->S6);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X23, (SIZE_T **)&contextPointers->S7);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X24, (SIZE_T **)&contextPointers->S8);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X25, (SIZE_T **)&contextPointers->S9);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X26, (SIZE_T **)&contextPointers->S10);
    GetContextPointer(cursor, unwContext, UNW_RISCV_X27, (SIZE_T **)&contextPointers->S11);
#elif (defined(HOST_UNIX) && defined(HOST_POWERPC64))
    GetContextPointer(cursor, unwContext, UNW_PPC64_R14, (SIZE_T **)&contextPointers->R14);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R15, (SIZE_T **)&contextPointers->R15);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R16, (SIZE_T **)&contextPointers->R16);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R17, (SIZE_T **)&contextPointers->R17);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R18, (SIZE_T **)&contextPointers->R18);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R19, (SIZE_T **)&contextPointers->R19);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R20, (SIZE_T **)&contextPointers->R20);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R21, (SIZE_T **)&contextPointers->R21);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R22, (SIZE_T **)&contextPointers->R22);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R23, (SIZE_T **)&contextPointers->R23);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R24, (SIZE_T **)&contextPointers->R24);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R25, (SIZE_T **)&contextPointers->R25);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R26, (SIZE_T **)&contextPointers->R26);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R27, (SIZE_T **)&contextPointers->R27);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R28, (SIZE_T **)&contextPointers->R28);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R29, (SIZE_T **)&contextPointers->R29);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R30, (SIZE_T **)&contextPointers->R30);
    GetContextPointer(cursor, unwContext, UNW_PPC64_R31, (SIZE_T **)&contextPointers->R31);
#else
#error unsupported architecture
#endif
}

#ifndef HOST_WINDOWS

// Frame pointer relative offset of a local containing a pointer to the windows style context of a location
// where a hardware exception occurred.
int g_hardware_exception_context_locvar_offset = 0;
// Frame pointer relative offset of a local containing a pointer to the windows style context of a location
// where an activation signal interrupted the thread.
int g_inject_activation_context_locvar_offset = 0;

BOOL PAL_VirtualUnwind(CONTEXT *context, KNONVOLATILE_CONTEXT_POINTERS *contextPointers)
{
    int st;
    unw_context_t unwContext;
    unw_cursor_t cursor;

    DWORD64 curPc = CONTEXTGetPC(context);

    // Check if the PC is the return address from the SEHProcessException.
    // If that's the case, extract its local variable containing a pointer to the windows style context of the hardware
    // exception and return that. This skips the hardware signal handler trampoline that the libunwind
    // cannot cross on some systems. On macOS, it skips a similar trampoline we create in HijackFaultingThread.
    if ((void*)curPc == g_SEHProcessExceptionReturnAddress)
    {
        CONTEXT* exceptionContext = *(CONTEXT**)(CONTEXTGetFP(context) + g_hardware_exception_context_locvar_offset);
        memcpy_s(context, sizeof(CONTEXT), exceptionContext, sizeof(CONTEXT));

        return TRUE;
    }

    // Check if the PC is the return address from the InvokeActivationHandler.
    // If that's the case, extract its local variable containing a pointer to the windows style context of the activation
    // injection location and return that. This skips the signal handler trampoline that the libunwind
    // cannot cross on some systems.
    if ((void*)curPc == g_InvokeActivationHandlerReturnAddress)
    {
        CONTEXT** activationContext = (CONTEXT**)(CONTEXTGetFP(context) + g_inject_activation_context_locvar_offset);
        memcpy_s(context, sizeof(CONTEXT), *activationContext, sizeof(CONTEXT));

        return TRUE;
    }

    if ((context->ContextFlags & CONTEXT_EXCEPTION_ACTIVE) != 0)
    {
        // The current frame is a source of hardware exception. Due to the fact that
        // we use the low level unwinder to unwind just one frame a time, the
        // unwinder doesn't have the signal_frame flag set. So it doesn't
        // know that it should not decrement the PC before looking up the unwind info.
        // So we compensate it by incrementing the PC before passing it to the unwinder.
        // Without it, the unwinder would not find unwind info if the hardware exception
        // happened in the first instruction of a function.
        CONTEXTSetPC(context, curPc + 1);
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
// The unw_getcontext is defined in the libunwind headers for ARM as inline assembly with
// stmia instruction storing SP and PC, which clang complains about as deprecated.
// However, it is required for atomic restoration of the context, so disable that warning.
#if defined(__llvm__) && defined(TARGET_ARM)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winline-asm"
#endif
    st = unw_getcontext(&unwContext);
#if defined(__llvm__) && defined(TARGET_ARM)
#pragma clang diagnostic pop
#endif

    if (st < 0)
    {
        return FALSE;
    }
#endif

    WinContextToUnwindContext(context, &unwContext);

    st = unw_init_local(&cursor, &unwContext);
    if (st < 0)
    {
        return FALSE;
    }

#if !UNWIND_CONTEXT_IS_UCONTEXT_T
    // Set the unwind context to the specified windows context
    WinContextToUnwindCursor(context, &cursor);
#endif

    st = unw_step(&cursor);
    if (st < 0)
    {
        return FALSE;
    }

    // Check if the frame we have unwound to is a frame that caused
    // synchronous signal, like a hardware exception and record it
    // in the context flags.
    if ((st != 0) && (unw_is_signal_frame(&cursor) > 0))
    {
        context->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
#if defined(CONTEXT_UNWOUND_TO_CALL)
        context->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
#endif // CONTEXT_UNWOUND_TO_CALL
    }
    else
    {
        context->ContextFlags &= ~CONTEXT_EXCEPTION_ACTIVE;
#if defined(CONTEXT_UNWOUND_TO_CALL)
        context->ContextFlags |= CONTEXT_UNWOUND_TO_CALL;
#endif // CONTEXT_UNWOUND_TO_CALL
    }

    // Update the passed in windows context to reflect the unwind
    //
    UnwindContextToWinContext(&cursor, context);

    // On some OSes / architectures if it unwound all the way to _start
    // (__libc_start_main on arm64 Linux with glibc older than 2.27).
    // >= 0 is returned from the step, but $pc will stay the same.
    // So we detect that here and set the $pc to NULL in that case.
    // This is the default behavior of the libunwind on x64 Linux.
    //
    if (st >= 0 && CONTEXTGetPC(context) == curPc)
    {
        CONTEXTSetPC(context, 0);
    }

    if (contextPointers != NULL)
    {
        GetContextPointers(&cursor, &unwContext, contextPointers);
    }
    return TRUE;
}

struct ExceptionRecords
{
    CONTEXT ContextRecord;
    EXCEPTION_RECORD ExceptionRecord;
};

// Max number of fallback contexts that are used when malloc fails to allocate ExceptionRecords structure
static const int MaxFallbackContexts = sizeof(size_t) * 8;
// Array of fallback contexts
static ExceptionRecords s_fallbackContexts[MaxFallbackContexts];
// Bitmap used for allocating fallback contexts - bits set to 1 represent already allocated context.
static volatile size_t s_allocatedContextsBitmap = 0;

/*++
Function:
    AllocateExceptionRecords

    Allocate EXCEPTION_RECORD and CONTEXT structures for an exception.
Parameters:
    exceptionRecord - output pointer to the allocated exception record
    contextRecord - output pointer to the allocated context record
--*/
VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord)
{
    ExceptionRecords* records;
    if (posix_memalign((void**)&records, alignof(ExceptionRecords), sizeof(ExceptionRecords)) != 0)
    {
        size_t bitmap;
        size_t newBitmap;
        int index;

        do
        {
            bitmap = s_allocatedContextsBitmap;
            index = __builtin_ffsl(~bitmap) - 1;
            if (index < 0)
            {
                PROCAbort();
            }

            newBitmap = bitmap | ((size_t)1 << index);
        }
        while (__sync_val_compare_and_swap(&s_allocatedContextsBitmap, bitmap, newBitmap) != bitmap);

        records = &s_fallbackContexts[index];
    }

    *contextRecord = &records->ContextRecord;
    *exceptionRecord = &records->ExceptionRecord;
}

/*++
Function:
    PAL_FreeExceptionRecords

    Free EXCEPTION_RECORD and CONTEXT structures of an exception that were allocated by the
    AllocateExceptionRecords.
Parameters:
    exceptionRecord - exception record
    contextRecord - context record
--*/
VOID
PALAPI
PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    // Both records are allocated at once and the allocated memory starts at the contextRecord
    ExceptionRecords* records = (ExceptionRecords*)contextRecord;
    if ((records >= &s_fallbackContexts[0]) && (records < &s_fallbackContexts[MaxFallbackContexts]))
    {
        int index = records - &s_fallbackContexts[0];
        __sync_fetch_and_and(&s_allocatedContextsBitmap, ~((size_t)1 << index));
    }
    else
    {
        free(contextRecord);
    }
}

/*++
Function:
    RtlpRaiseException

Parameters:
    ExceptionRecord - the Windows exception record to throw

Note:
    The name of this function and the name of the ExceptionRecord
    parameter is used in the sos lldb plugin code to read the exception
    record. See coreclr\tools\SOS\lldbplugin\services.cpp.

    This function must not be inlined or optimized so the below PAL_VirtualUnwind
    calls end up with RaiseException caller's context and so the above debugger
    code finds the function and ExceptionRecord parameter.
--*/
PAL_NORETURN
__attribute__((noinline))
__attribute__((NOOPT_ATTRIBUTE))
static void
RtlpRaiseException(EXCEPTION_RECORD *ExceptionRecord, CONTEXT *ContextRecord)
{
    throw PAL_SEHException(ExceptionRecord, ContextRecord);
}

/*++
Function:
  RaiseException

See MSDN doc.
--*/
// no PAL_NORETURN, as callers must assume this can return for continuable exceptions.
__attribute__((noinline))
VOID
PALAPI
RaiseException(IN DWORD dwExceptionCode,
               IN DWORD dwExceptionFlags,
               IN DWORD nNumberOfArguments,
               IN CONST ULONG_PTR *lpArguments)
{
    // PERF_ENTRY_ONLY is used here because RaiseException may or may not
    // return. We can not get latency data without PERF_EXIT. For this reason,
    // PERF_ENTRY_ONLY is used to profile frequency only.
    PERF_ENTRY_ONLY(RaiseException);
    ENTRY("RaiseException(dwCode=%#x, dwFlags=%#x, nArgs=%u, lpArguments=%p)\n",
          dwExceptionCode, dwExceptionFlags, nNumberOfArguments, lpArguments);

    /* Validate parameters */
    if (dwExceptionCode & RESERVED_SEH_BIT)
    {
        WARN("Exception code %08x has bit 28 set; clearing it.\n", dwExceptionCode);
        dwExceptionCode ^= RESERVED_SEH_BIT;
    }

    if (nNumberOfArguments > EXCEPTION_MAXIMUM_PARAMETERS)
    {
        WARN("Number of arguments (%d) exceeds the limit "
            "EXCEPTION_MAXIMUM_PARAMETERS (%d); ignoring extra parameters.\n",
            nNumberOfArguments, EXCEPTION_MAXIMUM_PARAMETERS);
        nNumberOfArguments = EXCEPTION_MAXIMUM_PARAMETERS;
    }

    CONTEXT *contextRecord;
    EXCEPTION_RECORD *exceptionRecord;
    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    ZeroMemory(exceptionRecord, sizeof(EXCEPTION_RECORD));

    exceptionRecord->ExceptionCode = dwExceptionCode;
    exceptionRecord->ExceptionFlags = dwExceptionFlags;
    exceptionRecord->ExceptionRecord = NULL;
    exceptionRecord->ExceptionAddress = NULL; // will be set by RtlpRaiseException
    exceptionRecord->NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        CopyMemory(exceptionRecord->ExceptionInformation, lpArguments,
                   nNumberOfArguments * sizeof(ULONG_PTR));
    }

    // Capture the context of RaiseException.
    ZeroMemory(contextRecord, sizeof(CONTEXT));
    contextRecord->ContextFlags = CONTEXT_FULL;
    CONTEXT_CaptureContext(contextRecord);

    // We have to unwind one level to get the actual context user code could be resumed at.
    PAL_VirtualUnwind(contextRecord, NULL);

    exceptionRecord->ExceptionAddress = (void *)CONTEXTGetPC(contextRecord);

    RtlpRaiseException(exceptionRecord, contextRecord);

    LOGEXIT("RaiseException returns\n");
}

#endif // !HOST_WINDOWS
