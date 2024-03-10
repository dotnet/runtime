// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "regdisplay.h"
#include "config.h"

#include "UnixContext.h"

#ifdef __APPLE__

#ifdef HOST_ARM64

#define MCREG_X0(mc)      ((mc)->__ss.__x[0])
#define MCREG_X1(mc)      ((mc)->__ss.__x[1])
#define MCREG_X2(mc)      ((mc)->__ss.__x[2])
#define MCREG_X3(mc)      ((mc)->__ss.__x[3])
#define MCREG_X4(mc)      ((mc)->__ss.__x[4])
#define MCREG_X5(mc)      ((mc)->__ss.__x[5])
#define MCREG_X6(mc)      ((mc)->__ss.__x[6])
#define MCREG_X7(mc)      ((mc)->__ss.__x[7])
#define MCREG_X8(mc)      ((mc)->__ss.__x[8])
#define MCREG_X9(mc)      ((mc)->__ss.__x[9])
#define MCREG_X10(mc)     ((mc)->__ss.__x[10])
#define MCREG_X11(mc)     ((mc)->__ss.__x[11])
#define MCREG_X12(mc)     ((mc)->__ss.__x[12])
#define MCREG_X13(mc)     ((mc)->__ss.__x[13])
#define MCREG_X14(mc)     ((mc)->__ss.__x[14])
#define MCREG_X15(mc)     ((mc)->__ss.__x[15])
#define MCREG_X16(mc)     ((mc)->__ss.__x[16])
#define MCREG_X17(mc)     ((mc)->__ss.__x[17])
#define MCREG_X18(mc)     ((mc)->__ss.__x[18])
#define MCREG_X19(mc)     ((mc)->__ss.__x[19])
#define MCREG_X20(mc)     ((mc)->__ss.__x[20])
#define MCREG_X21(mc)     ((mc)->__ss.__x[21])
#define MCREG_X22(mc)     ((mc)->__ss.__x[22])
#define MCREG_X23(mc)     ((mc)->__ss.__x[23])
#define MCREG_X24(mc)     ((mc)->__ss.__x[24])
#define MCREG_X25(mc)     ((mc)->__ss.__x[25])
#define MCREG_X26(mc)     ((mc)->__ss.__x[26])
#define MCREG_X27(mc)     ((mc)->__ss.__x[27])
#define MCREG_X28(mc)     ((mc)->__ss.__x[28])
#define MCREG_Fp(mc)      ((mc)->__ss.__fp)
#define MCREG_Lr(mc)      ((mc)->__ss.__lr)
#define MCREG_Sp(mc)      ((mc)->__ss.__sp)
#define MCREG_Pc(mc)      ((mc)->__ss.__pc)

#elif HOST_AMD64 // HOST_ARM64

#define MCREG_Rip(mc)       ((mc)->__ss.__rip)
#define MCREG_Rsp(mc)       ((mc)->__ss.__rsp)
#define MCREG_Rax(mc)       ((mc)->__ss.__rax)
#define MCREG_Rbx(mc)       ((mc)->__ss.__rbx)
#define MCREG_Rcx(mc)       ((mc)->__ss.__rcx)
#define MCREG_Rdx(mc)       ((mc)->__ss.__rdx)
#define MCREG_Rsi(mc)       ((mc)->__ss.__rsi)
#define MCREG_Rdi(mc)       ((mc)->__ss.__rdi)
#define MCREG_Rbp(mc)       ((mc)->__ss.__rbp)
#define MCREG_R8(mc)        ((mc)->__ss.__r8)
#define MCREG_R9(mc)        ((mc)->__ss.__r9)
#define MCREG_R10(mc)       ((mc)->__ss.__r10)
#define MCREG_R11(mc)       ((mc)->__ss.__r11)
#define MCREG_R12(mc)       ((mc)->__ss.__r12)
#define MCREG_R13(mc)       ((mc)->__ss.__r13)
#define MCREG_R14(mc)       ((mc)->__ss.__r14)
#define MCREG_R15(mc)       ((mc)->__ss.__r15)

#else // HOST_ARM64

#error "Unsupported arch"

#endif // HOST_ARM64

#else

#if HAVE___GREGSET_T

#ifdef HOST_64BIT
#define MCREG_Rip(mc)       ((mc).__gregs[_REG_RIP])
#define MCREG_Rsp(mc)       ((mc).__gregs[_REG_RSP])
#define MCREG_Rax(mc)       ((mc).__gregs[_REG_RAX])
#define MCREG_Rbx(mc)       ((mc).__gregs[_REG_RBX])
#define MCREG_Rcx(mc)       ((mc).__gregs[_REG_RCX])
#define MCREG_Rdx(mc)       ((mc).__gregs[_REG_RDX])
#define MCREG_Rsi(mc)       ((mc).__gregs[_REG_RSI])
#define MCREG_Rdi(mc)       ((mc).__gregs[_REG_RDI])
#define MCREG_Rbp(mc)       ((mc).__gregs[_REG_RBP])
#define MCREG_R8(mc)        ((mc).__gregs[_REG_R8])
#define MCREG_R9(mc)        ((mc).__gregs[_REG_R9])
#define MCREG_R10(mc)       ((mc).__gregs[_REG_R10])
#define MCREG_R11(mc)       ((mc).__gregs[_REG_R11])
#define MCREG_R12(mc)       ((mc).__gregs[_REG_R12])
#define MCREG_R13(mc)       ((mc).__gregs[_REG_R13])
#define MCREG_R14(mc)       ((mc).__gregs[_REG_R14])
#define MCREG_R15(mc)       ((mc).__gregs[_REG_R15])

#else // HOST_64BIT

#define MCREG_Eip(mc)       ((mc).__gregs[_REG_EIP])
#define MCREG_Esp(mc)       ((mc).__gregs[_REG_ESP])
#define MCREG_Eax(mc)       ((mc).__gregs[_REG_EAX])
#define MCREG_Ebx(mc)       ((mc).__gregs[_REG_EBX])
#define MCREG_Ecx(mc)       ((mc).__gregs[_REG_ECX])
#define MCREG_Edx(mc)       ((mc).__gregs[_REG_EDX])
#define MCREG_Esi(mc)       ((mc).__gregs[_REG_ESI])
#define MCREG_Edi(mc)       ((mc).__gregs[_REG_EDI])
#define MCREG_Ebp(mc)       ((mc).__gregs[_REG_EBP])

#endif // HOST_64BIT

#elif HAVE_GREGSET_T

#ifdef HOST_64BIT
#define MCREG_Rip(mc)       ((mc).gregs[REG_RIP])
#define MCREG_Rsp(mc)       ((mc).gregs[REG_RSP])
#define MCREG_Rax(mc)       ((mc).gregs[REG_RAX])
#define MCREG_Rbx(mc)       ((mc).gregs[REG_RBX])
#define MCREG_Rcx(mc)       ((mc).gregs[REG_RCX])
#define MCREG_Rdx(mc)       ((mc).gregs[REG_RDX])
#define MCREG_Rsi(mc)       ((mc).gregs[REG_RSI])
#define MCREG_Rdi(mc)       ((mc).gregs[REG_RDI])
#define MCREG_Rbp(mc)       ((mc).gregs[REG_RBP])
#define MCREG_R8(mc)        ((mc).gregs[REG_R8])
#define MCREG_R9(mc)        ((mc).gregs[REG_R9])
#define MCREG_R10(mc)       ((mc).gregs[REG_R10])
#define MCREG_R11(mc)       ((mc).gregs[REG_R11])
#define MCREG_R12(mc)       ((mc).gregs[REG_R12])
#define MCREG_R13(mc)       ((mc).gregs[REG_R13])
#define MCREG_R14(mc)       ((mc).gregs[REG_R14])
#define MCREG_R15(mc)       ((mc).gregs[REG_R15])

#else // HOST_64BIT

#define MCREG_Eip(mc)       ((mc).gregs[REG_EIP])
#define MCREG_Esp(mc)       ((mc).gregs[REG_ESP])
#define MCREG_Eax(mc)       ((mc).gregs[REG_EAX])
#define MCREG_Ebx(mc)       ((mc).gregs[REG_EBX])
#define MCREG_Ecx(mc)       ((mc).gregs[REG_ECX])
#define MCREG_Edx(mc)       ((mc).gregs[REG_EDX])
#define MCREG_Esi(mc)       ((mc).gregs[REG_ESI])
#define MCREG_Edi(mc)       ((mc).gregs[REG_EDI])
#define MCREG_Ebp(mc)       ((mc).gregs[REG_EBP])

#endif // HOST_64BIT

#else // HAVE_GREGSET_T

#ifdef HOST_64BIT

#if defined(HOST_ARM64) && defined(TARGET_FREEBSD)

#define MCREG_X0(mc)   (mc.mc_gpregs.gp_x[0])
#define MCREG_X1(mc)   (mc.mc_gpregs.gp_x[1])
#define MCREG_X2(mc)   (mc.mc_gpregs.gp_x[2])
#define MCREG_X3(mc)   (mc.mc_gpregs.gp_x[3])
#define MCREG_X4(mc)   (mc.mc_gpregs.gp_x[4])
#define MCREG_X5(mc)   (mc.mc_gpregs.gp_x[5])
#define MCREG_X6(mc)   (mc.mc_gpregs.gp_x[6])
#define MCREG_X7(mc)   (mc.mc_gpregs.gp_x[7])
#define MCREG_X8(mc)   (mc.mc_gpregs.gp_x[8])
#define MCREG_X9(mc)   (mc.mc_gpregs.gp_x[9])
#define MCREG_X10(mc)  (mc.mc_gpregs.gp_x[10])
#define MCREG_X11(mc)  (mc.mc_gpregs.gp_x[11])
#define MCREG_X12(mc)  (mc.mc_gpregs.gp_x[12])
#define MCREG_X13(mc)  (mc.mc_gpregs.gp_x[13])
#define MCREG_X14(mc)  (mc.mc_gpregs.gp_x[14])
#define MCREG_X15(mc)  (mc.mc_gpregs.gp_x[15])
#define MCREG_X16(mc)  (mc.mc_gpregs.gp_x[16])
#define MCREG_X17(mc)  (mc.mc_gpregs.gp_x[17])
#define MCREG_X18(mc)  (mc.mc_gpregs.gp_x[18])
#define MCREG_X19(mc)  (mc.mc_gpregs.gp_x[19])
#define MCREG_X20(mc)  (mc.mc_gpregs.gp_x[20])
#define MCREG_X21(mc)  (mc.mc_gpregs.gp_x[21])
#define MCREG_X22(mc)  (mc.mc_gpregs.gp_x[22])
#define MCREG_X23(mc)  (mc.mc_gpregs.gp_x[23])
#define MCREG_X24(mc)  (mc.mc_gpregs.gp_x[24])
#define MCREG_X25(mc)  (mc.mc_gpregs.gp_x[25])
#define MCREG_X26(mc)  (mc.mc_gpregs.gp_x[26])
#define MCREG_X27(mc)  (mc.mc_gpregs.gp_x[27])
#define MCREG_X28(mc)  (mc.mc_gpregs.gp_x[28])
#define MCREG_Lr(mc)   (mc.mc_gpregs.gp_lr)
#define MCREG_Sp(mc)   (mc.mc_gpregs.gp_sp)
#define MCREG_Pc(mc)   (mc.mc_gpregs.gp_elr)
#define MCREG_Fp(mc)   (mc.mc_gpregs.gp_x[29])

#elif defined(HOST_ARM64)

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

#else

// For FreeBSD, as found in x86/ucontext.h
#define MCREG_Rip(mc)       ((mc).mc_rip)
#define MCREG_Rsp(mc)       ((mc).mc_rsp)
#define MCREG_Rax(mc)       ((mc).mc_rax)
#define MCREG_Rbx(mc)       ((mc).mc_rbx)
#define MCREG_Rcx(mc)       ((mc).mc_rcx)
#define MCREG_Rdx(mc)       ((mc).mc_rdx)
#define MCREG_Rsi(mc)       ((mc).mc_rsi)
#define MCREG_Rdi(mc)       ((mc).mc_rdi)
#define MCREG_Rbp(mc)       ((mc).mc_rbp)
#define MCREG_R8(mc)        ((mc).mc_r8)
#define MCREG_R9(mc)        ((mc).mc_r9)
#define MCREG_R10(mc)       ((mc).mc_r10)
#define MCREG_R11(mc)       ((mc).mc_r11)
#define MCREG_R12(mc)       ((mc).mc_r12)
#define MCREG_R13(mc)       ((mc).mc_r13)
#define MCREG_R14(mc)       ((mc).mc_r14)
#define MCREG_R15(mc)       ((mc).mc_r15)

#endif

#else // HOST_64BIT

#if defined(HOST_ARM)

#define MCREG_Pc(mc)        ((mc).arm_pc)
#define MCREG_Sp(mc)        ((mc).arm_sp)
#define MCREG_Lr(mc)        ((mc).arm_lr)
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

#elif defined(HOST_X86)

#define MCREG_Eip(mc)       ((mc).mc_eip)
#define MCREG_Esp(mc)       ((mc).mc_esp)
#define MCREG_Eax(mc)       ((mc).mc_eax)
#define MCREG_Ebx(mc)       ((mc).mc_ebx)
#define MCREG_Ecx(mc)       ((mc).mc_ecx)
#define MCREG_Edx(mc)       ((mc).mc_edx)
#define MCREG_Esi(mc)       ((mc).mc_esi)
#define MCREG_Edi(mc)       ((mc).mc_edi)
#define MCREG_Ebp(mc)       ((mc).mc_ebp)

#else
#error "Unsupported arch"
#endif

#endif // HOST_64BIT

#endif // HAVE_GREGSET_T

#endif // __APPLE__

#if defined(HOST_AMD64)
#define ASSIGN_CONTROL_REGS \
    ASSIGN_REG(Rip, IP)     \
    ASSIGN_REG(Rsp, Rsp)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(Rbx, Rbx)     \
    ASSIGN_REG(Rbp, Rbp)     \
    ASSIGN_REG(R12, R12)     \
    ASSIGN_REG(R13, R13)     \
    ASSIGN_REG(R14, R14)     \
    ASSIGN_REG(R15, R15)

#define ASSIGN_TWO_ARGUMENT_REGS(arg0Reg, arg1Reg)    \
    MCREG_Rdi(nativeContext->uc_mcontext) = arg0Reg;  \
    MCREG_Rsi(nativeContext->uc_mcontext) = arg1Reg;

#elif defined(HOST_X86)
#define ASSIGN_CONTROL_REGS \
    ASSIGN_REG(Eip, IP)     \
    ASSIGN_REG(Esp, Rsp)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(Ebx, Rbx)     \
    ASSIGN_REG(Ebp, Rbp)

#define ASSIGN_TWO_ARGUMENT_REGS(arg0Reg, arg1Reg)    \
    MCREG_Ecx(nativeContext->uc_mcontext) = arg0Reg;  \
    MCREG_Edx(nativeContext->uc_mcontext) = arg1Reg;

#elif defined(HOST_ARM)

#define ASSIGN_CONTROL_REGS  \
    ASSIGN_REG(Pc, IP)       \
    ASSIGN_REG(Sp, SP)       \
    ASSIGN_REG(Lr, LR)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(R4, R4)       \
    ASSIGN_REG(R5, R5)       \
    ASSIGN_REG(R6, R6)       \
    ASSIGN_REG(R7, R7)       \
    ASSIGN_REG(R8, R8)       \
    ASSIGN_REG(R9, R9)       \
    ASSIGN_REG(R10, R10)     \
    ASSIGN_REG(R11, R11)

#define ASSIGN_TWO_ARGUMENT_REGS(arg0Reg, arg1Reg) \
    MCREG_R0(nativeContext->uc_mcontext) = arg0Reg;       \
    MCREG_R1(nativeContext->uc_mcontext) = arg1Reg;

#elif defined(HOST_ARM64)

#define ASSIGN_CONTROL_REGS  \
    ASSIGN_REG(Pc, IP)    \
    ASSIGN_REG(Sp, SP)    \
    ASSIGN_REG(Fp, FP)    \
    ASSIGN_REG(Lr, LR)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(X19, X19)   \
    ASSIGN_REG(X20, X20)   \
    ASSIGN_REG(X21, X21)   \
    ASSIGN_REG(X22, X22)   \
    ASSIGN_REG(X23, X23)   \
    ASSIGN_REG(X24, X24)   \
    ASSIGN_REG(X25, X25)   \
    ASSIGN_REG(X26, X26)   \
    ASSIGN_REG(X27, X27)   \
    ASSIGN_REG(X28, X28)

#define ASSIGN_TWO_ARGUMENT_REGS \
    MCREG_X0(nativeContext->uc_mcontext) = arg0Reg;       \
    MCREG_X1(nativeContext->uc_mcontext) = arg1Reg;

#elif defined(HOST_WASM)
    // TODO: determine how unwinding will work on WebAssembly
#define ASSIGN_CONTROL_REGS
#define ASSIGN_INTEGER_REGS
#define ASSIGN_TWO_ARGUMENT_REGS
#else
#error unsupported architecture
#endif

// Convert Unix native context to PAL_LIMITED_CONTEXT
void NativeContextToPalContext(const void* context, PAL_LIMITED_CONTEXT* palContext)
{
    ucontext_t *nativeContext = (ucontext_t*)context;
#define ASSIGN_REG(regNative, regPal) palContext->regPal = MCREG_##regNative(nativeContext->uc_mcontext);
    ASSIGN_CONTROL_REGS
    ASSIGN_INTEGER_REGS
#undef ASSIGN_REG
}

// Redirect Unix native context to the PAL_LIMITED_CONTEXT and also set the first two argument registers
void RedirectNativeContext(void* context, const PAL_LIMITED_CONTEXT* palContext, uintptr_t arg0Reg, uintptr_t arg1Reg)
{
    ucontext_t *nativeContext = (ucontext_t*)context;

#define ASSIGN_REG(regNative, regPal) MCREG_##regNative(nativeContext->uc_mcontext) = palContext->regPal;
    ASSIGN_CONTROL_REGS
#undef ASSIGN_REG
    ASSIGN_TWO_ARGUMENT_REGS(arg0Reg, arg1Reg);
}

#ifdef HOST_AMD64
// Get value of a register from the native context
// Parameters:
//  void* context  - context containing the registers
//  uint32_t index - index of the register
//                   Rax = 0, Rcx = 1, Rdx = 2, Rbx = 3
//                   Rsp = 4, Rbp = 5, Rsi = 6, Rdi = 7
//                   R8  = 8, R9  = 9, R10 = 10, R11 = 11
//                   R12 = 12, R13 = 13, R14 = 14, R15 = 15
uint64_t GetRegisterValueByIndex(void* context, uint32_t index)
{
    ucontext_t *nativeContext = (ucontext_t*)context;
    switch (index)
    {
        case 0:
            return MCREG_Rax(nativeContext->uc_mcontext);
        case 1:
            return MCREG_Rcx(nativeContext->uc_mcontext);
        case 2:
            return MCREG_Rdx(nativeContext->uc_mcontext);
        case 3:
            return MCREG_Rbx(nativeContext->uc_mcontext);
        case 4:
            return MCREG_Rsp(nativeContext->uc_mcontext);
        case 5:
            return MCREG_Rbp(nativeContext->uc_mcontext);
        case 6:
            return MCREG_Rsi(nativeContext->uc_mcontext);
        case 7:
            return MCREG_Rdi(nativeContext->uc_mcontext);
        case 8:
            return MCREG_R8(nativeContext->uc_mcontext);
        case 9:
            return MCREG_R9(nativeContext->uc_mcontext);
        case 10:
            return MCREG_R10(nativeContext->uc_mcontext);
        case 11:
            return MCREG_R11(nativeContext->uc_mcontext);
        case 12:
            return MCREG_R12(nativeContext->uc_mcontext);
        case 13:
            return MCREG_R13(nativeContext->uc_mcontext);
        case 14:
            return MCREG_R14(nativeContext->uc_mcontext);
        case 15:
            return MCREG_R15(nativeContext->uc_mcontext);
    }

    ASSERT(false);
    return 0;
}

// Get value of the program counter from the native context
uint64_t GetPC(void* context)
{
    ucontext_t *nativeContext = (ucontext_t*)context;
    return MCREG_Rip(nativeContext->uc_mcontext);
}

#endif // HOST_AMD64

#ifdef TARGET_ARM64

    uint64_t& UNIX_CONTEXT::X0() { return (uint64_t&)MCREG_X0(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X1() { return (uint64_t&)MCREG_X1(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X2() { return (uint64_t&)MCREG_X2(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X3() { return (uint64_t&)MCREG_X3(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X4() { return (uint64_t&)MCREG_X4(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X5() { return (uint64_t&)MCREG_X5(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X6() { return (uint64_t&)MCREG_X6(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X7() { return (uint64_t&)MCREG_X7(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X8() { return (uint64_t&)MCREG_X8(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X9() { return (uint64_t&)MCREG_X9(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X10() { return (uint64_t&)MCREG_X10(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X11() { return (uint64_t&)MCREG_X11(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X12() { return (uint64_t&)MCREG_X12(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X13() { return (uint64_t&)MCREG_X13(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X14() { return (uint64_t&)MCREG_X14(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X15() { return (uint64_t&)MCREG_X15(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X16() { return (uint64_t&)MCREG_X16(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X17() { return (uint64_t&)MCREG_X17(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X18() { return (uint64_t&)MCREG_X18(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X19() { return (uint64_t&)MCREG_X19(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X20() { return (uint64_t&)MCREG_X20(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X21() { return (uint64_t&)MCREG_X21(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X22() { return (uint64_t&)MCREG_X22(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X23() { return (uint64_t&)MCREG_X23(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X24() { return (uint64_t&)MCREG_X24(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X25() { return (uint64_t&)MCREG_X25(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X26() { return (uint64_t&)MCREG_X26(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X27() { return (uint64_t&)MCREG_X27(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::X28() { return (uint64_t&)MCREG_X28(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Fp() { return (uint64_t&)MCREG_Fp(ctx.uc_mcontext); } // X29
    uint64_t& UNIX_CONTEXT::Lr() { return (uint64_t&)MCREG_Lr(ctx.uc_mcontext); } // X30
    uint64_t& UNIX_CONTEXT::Sp() { return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Pc() { return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }

#elif defined(TARGET_AMD64)
    uint64_t& UNIX_CONTEXT::Rax(){ return (uint64_t&)MCREG_Rax(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rcx(){ return (uint64_t&)MCREG_Rcx(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rdx(){ return (uint64_t&)MCREG_Rdx(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rbx(){ return (uint64_t&)MCREG_Rbx(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rsp(){ return (uint64_t&)MCREG_Rsp(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rbp(){ return (uint64_t&)MCREG_Rbp(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rsi(){ return (uint64_t&)MCREG_Rsi(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rdi(){ return (uint64_t&)MCREG_Rdi(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R8(){ return (uint64_t&)MCREG_R8(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R9(){ return (uint64_t&)MCREG_R9(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R10(){ return (uint64_t&)MCREG_R10(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R11(){ return (uint64_t&)MCREG_R11(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R12(){ return (uint64_t&)MCREG_R12(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R13(){ return (uint64_t&)MCREG_R13(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R14(){ return (uint64_t&)MCREG_R14(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R15(){ return (uint64_t&)MCREG_R15(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Rip(){ return (uint64_t&)MCREG_Rip(ctx.uc_mcontext); }

#elif defined(TARGET_ARM)
    uint64_t& UNIX_CONTEXT::Pc(){ return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Sp(){ return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::Lr(){ return (uint64_t&)MCREG_Lr(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R0(){ return (uint64_t&)MCREG_R0(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R1(){ return (uint64_t&)MCREG_R1(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R2(){ return (uint64_t&)MCREG_R2(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R3(){ return (uint64_t&)MCREG_R3(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R4(){ return (uint64_t&)MCREG_R4(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R5(){ return (uint64_t&)MCREG_R5(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R6(){ return (uint64_t&)MCREG_R6(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R7(){ return (uint64_t&)MCREG_R7(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R8(){ return (uint64_t&)MCREG_R8(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R9(){ return (uint64_t&)MCREG_R9(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R10(){ return (uint64_t&)MCREG_R10(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R11(){ return (uint64_t&)MCREG_R11(ctx.uc_mcontext); }
    uint64_t& UNIX_CONTEXT::R12(){ return (uint64_t&)MCREG_R12(ctx.uc_mcontext); }

#else
    PORTABILITY_ASSERT("UNIX_CONTEXT");
#endif // TARGET_ARM

