// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "regdisplay.h"
#include "config.h"

#include "NativeContext.h"

#include <unix/NativeContextRegs.h>
#include <unix/NativeContextXstate.h>

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
    MCREG_Rdi(MCONTEXT_FROM_NATIVE(nativeContext)) = arg0Reg;  \
    MCREG_Rsi(MCONTEXT_FROM_NATIVE(nativeContext)) = arg1Reg;

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

#elif defined(HOST_LOONGARCH64)

#define ASSIGN_CONTROL_REGS  \
    ASSIGN_REG(Pc, IP)    \
    ASSIGN_REG(Sp, SP)    \
    ASSIGN_REG(Fp, FP)    \
    ASSIGN_REG(Ra, RA)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(S0, R23)   \
    ASSIGN_REG(S1, R24)   \
    ASSIGN_REG(S2, R25)   \
    ASSIGN_REG(S3, R26)   \
    ASSIGN_REG(S4, R27)   \
    ASSIGN_REG(S5, R28)   \
    ASSIGN_REG(S6, R29)   \
    ASSIGN_REG(S7, R30)   \
    ASSIGN_REG(S8, R31)

#define ASSIGN_TWO_ARGUMENT_REGS \
    MCREG_A0(nativeContext->uc_mcontext) = arg0Reg;       \
    MCREG_A1(nativeContext->uc_mcontext) = arg1Reg;

#elif defined(HOST_RISCV64)

#define ASSIGN_CONTROL_REGS  \
    ASSIGN_REG(Pc, IP)    \
    ASSIGN_REG(Sp, SP)    \
    ASSIGN_REG(Fp, FP)    \
    ASSIGN_REG(Ra, RA)

#define ASSIGN_INTEGER_REGS  \
    ASSIGN_REG(S1, S1)     \
    ASSIGN_REG(S2, S2)     \
    ASSIGN_REG(S3, S3)     \
    ASSIGN_REG(S4, S4)     \
    ASSIGN_REG(S5, S5)     \
    ASSIGN_REG(S6, S6)     \
    ASSIGN_REG(S7, S7)     \
    ASSIGN_REG(S8, S8)     \
    ASSIGN_REG(S9, S9)     \
    ASSIGN_REG(S10, S10)   \
    ASSIGN_REG(S11, S11)

#define ASSIGN_TWO_ARGUMENT_REGS \
    MCREG_A0(nativeContext->uc_mcontext) = arg0Reg;       \
    MCREG_A1(nativeContext->uc_mcontext) = arg1Reg;

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
#define ASSIGN_REG(regNative, regPal) palContext->regPal = MCREG_##regNative(MCONTEXT_FROM_NATIVE(nativeContext));
    ASSIGN_CONTROL_REGS
    ASSIGN_INTEGER_REGS
#undef ASSIGN_REG
}

// Redirect Unix native context to the PAL_LIMITED_CONTEXT and also set the first two argument registers
void RedirectNativeContext(void* context, const PAL_LIMITED_CONTEXT* palContext, uintptr_t arg0Reg, uintptr_t arg1Reg)
{
    ucontext_t *nativeContext = (ucontext_t*)context;

#define ASSIGN_REG(regNative, regPal) MCREG_##regNative(MCONTEXT_FROM_NATIVE(nativeContext)) = palContext->regPal;
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
//                   R16..R31 = 16..31 when the context contains APX state
uint64_t GetRegisterValueByIndex(void* context, uint32_t index)
{
    ucontext_t *nativeContext = (ucontext_t*)context;
    switch (index)
    {
        case 0:
            return MCREG_Rax(MCONTEXT_FROM_NATIVE(nativeContext));
        case 1:
            return MCREG_Rcx(MCONTEXT_FROM_NATIVE(nativeContext));
        case 2:
            return MCREG_Rdx(MCONTEXT_FROM_NATIVE(nativeContext));
        case 3:
            return MCREG_Rbx(MCONTEXT_FROM_NATIVE(nativeContext));
        case 4:
            return MCREG_Rsp(MCONTEXT_FROM_NATIVE(nativeContext));
        case 5:
            return MCREG_Rbp(MCONTEXT_FROM_NATIVE(nativeContext));
        case 6:
            return MCREG_Rsi(MCONTEXT_FROM_NATIVE(nativeContext));
        case 7:
            return MCREG_Rdi(MCONTEXT_FROM_NATIVE(nativeContext));
        case 8:
            return MCREG_R8(MCONTEXT_FROM_NATIVE(nativeContext));
        case 9:
            return MCREG_R9(MCONTEXT_FROM_NATIVE(nativeContext));
        case 10:
            return MCREG_R10(MCONTEXT_FROM_NATIVE(nativeContext));
        case 11:
            return MCREG_R11(MCONTEXT_FROM_NATIVE(nativeContext));
        case 12:
            return MCREG_R12(MCONTEXT_FROM_NATIVE(nativeContext));
        case 13:
            return MCREG_R13(MCONTEXT_FROM_NATIVE(nativeContext));
        case 14:
            return MCREG_R14(MCONTEXT_FROM_NATIVE(nativeContext));
        case 15:
            return MCREG_R15(MCONTEXT_FROM_NATIVE(nativeContext));
    }

#ifdef XSTATE_SUPPORTED
    if ((index < 32) && FPREG_HasApxRegisters(nativeContext))
    {
        uint32_t size;
        uint64_t* egpr = (uint64_t*)FPREG_Xstate_Egpr(nativeContext, &size);
        ASSERT(size >= 16 * sizeof(uint64_t));
        return egpr[index - 16];
    }
#endif // XSTATE_SUPPORTED

    ASSERT(false);
    return 0;
}

// Get value of the program counter from the native context
uint64_t GetPC(void* context)
{
    ucontext_t *nativeContext = (ucontext_t*)context;
    return MCREG_InstructionPointer(MCONTEXT_FROM_NATIVE(nativeContext));
}

#endif // HOST_AMD64

#ifdef TARGET_ARM64

    uint64_t& NATIVE_CONTEXT::X0() { return (uint64_t&)MCREG_X0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X1() { return (uint64_t&)MCREG_X1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X2() { return (uint64_t&)MCREG_X2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X3() { return (uint64_t&)MCREG_X3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X4() { return (uint64_t&)MCREG_X4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X5() { return (uint64_t&)MCREG_X5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X6() { return (uint64_t&)MCREG_X6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X7() { return (uint64_t&)MCREG_X7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X8() { return (uint64_t&)MCREG_X8(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X9() { return (uint64_t&)MCREG_X9(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X10() { return (uint64_t&)MCREG_X10(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X11() { return (uint64_t&)MCREG_X11(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X12() { return (uint64_t&)MCREG_X12(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X13() { return (uint64_t&)MCREG_X13(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X14() { return (uint64_t&)MCREG_X14(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X15() { return (uint64_t&)MCREG_X15(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X16() { return (uint64_t&)MCREG_X16(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X17() { return (uint64_t&)MCREG_X17(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X18() { return (uint64_t&)MCREG_X18(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X19() { return (uint64_t&)MCREG_X19(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X20() { return (uint64_t&)MCREG_X20(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X21() { return (uint64_t&)MCREG_X21(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X22() { return (uint64_t&)MCREG_X22(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X23() { return (uint64_t&)MCREG_X23(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X24() { return (uint64_t&)MCREG_X24(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X25() { return (uint64_t&)MCREG_X25(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X26() { return (uint64_t&)MCREG_X26(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X27() { return (uint64_t&)MCREG_X27(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::X28() { return (uint64_t&)MCREG_X28(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Fp() { return (uint64_t&)MCREG_Fp(ctx.uc_mcontext); } // X29
    uint64_t& NATIVE_CONTEXT::Lr() { return (uint64_t&)MCREG_Lr(ctx.uc_mcontext); } // X30
    uint64_t& NATIVE_CONTEXT::Sp() { return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Pc() { return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }

#elif defined(TARGET_AMD64)
    uint64_t& NATIVE_CONTEXT::Rax(){ return (uint64_t&)MCREG_Rax(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rcx(){ return (uint64_t&)MCREG_Rcx(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rdx(){ return (uint64_t&)MCREG_Rdx(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rbx(){ return (uint64_t&)MCREG_Rbx(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rsp(){ return (uint64_t&)MCREG_Rsp(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rbp(){ return (uint64_t&)MCREG_Rbp(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rsi(){ return (uint64_t&)MCREG_Rsi(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rdi(){ return (uint64_t&)MCREG_Rdi(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R8(){ return (uint64_t&)MCREG_R8(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R9(){ return (uint64_t&)MCREG_R9(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R10(){ return (uint64_t&)MCREG_R10(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R11(){ return (uint64_t&)MCREG_R11(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R12(){ return (uint64_t&)MCREG_R12(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R13(){ return (uint64_t&)MCREG_R13(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R14(){ return (uint64_t&)MCREG_R14(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::R15(){ return (uint64_t&)MCREG_R15(MCONTEXT_FROM_NATIVE(&ctx)); }
    uint64_t& NATIVE_CONTEXT::Rip(){ return (uint64_t&)MCREG_Rip(MCONTEXT_FROM_NATIVE(&ctx)); }

#elif defined(TARGET_ARM)
    uint64_t& NATIVE_CONTEXT::Pc(){ return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Sp(){ return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Lr(){ return (uint64_t&)MCREG_Lr(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R0(){ return (uint64_t&)MCREG_R0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R1(){ return (uint64_t&)MCREG_R1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R2(){ return (uint64_t&)MCREG_R2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R3(){ return (uint64_t&)MCREG_R3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R4(){ return (uint64_t&)MCREG_R4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R5(){ return (uint64_t&)MCREG_R5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R6(){ return (uint64_t&)MCREG_R6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R7(){ return (uint64_t&)MCREG_R7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R8(){ return (uint64_t&)MCREG_R8(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R9(){ return (uint64_t&)MCREG_R9(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R10(){ return (uint64_t&)MCREG_R10(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R11(){ return (uint64_t&)MCREG_R11(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R12(){ return (uint64_t&)MCREG_R12(ctx.uc_mcontext); }

#elif TARGET_LOONGARCH64

    uint64_t& NATIVE_CONTEXT::R0() { return (uint64_t&)MCREG_R0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Ra() { return (uint64_t&)MCREG_Ra(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R2() { return (uint64_t&)MCREG_Tp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Sp() { return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R4() { return (uint64_t&)MCREG_A0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R5() { return (uint64_t&)MCREG_A1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R6() { return (uint64_t&)MCREG_A2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R7() { return (uint64_t&)MCREG_A3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R8() { return (uint64_t&)MCREG_A4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R9() { return (uint64_t&)MCREG_A5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R10() { return (uint64_t&)MCREG_A6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R11() { return (uint64_t&)MCREG_A7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R12() { return (uint64_t&)MCREG_T0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R13() { return (uint64_t&)MCREG_T1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R14() { return (uint64_t&)MCREG_T2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R15() { return (uint64_t&)MCREG_T3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R16() { return (uint64_t&)MCREG_T4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R17() { return (uint64_t&)MCREG_T5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R18() { return (uint64_t&)MCREG_T6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R19() { return (uint64_t&)MCREG_T7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R20() { return (uint64_t&)MCREG_T8(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R21() { return (uint64_t&)MCREG_X0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Fp() { return (uint64_t&)MCREG_Fp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R23() { return (uint64_t&)MCREG_S0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R24() { return (uint64_t&)MCREG_S1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R25() { return (uint64_t&)MCREG_S2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R26() { return (uint64_t&)MCREG_S3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R27() { return (uint64_t&)MCREG_S4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R28() { return (uint64_t&)MCREG_S5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R29() { return (uint64_t&)MCREG_S6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R30() { return (uint64_t&)MCREG_S7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::R31() { return (uint64_t&)MCREG_S8(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Pc() { return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }

#elif TARGET_RISCV64

    uint64_t& NATIVE_CONTEXT::R0() { return (uint64_t&)MCREG_R0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Ra() { return (uint64_t&)MCREG_Ra(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Sp() { return (uint64_t&)MCREG_Sp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Gp() { return (uint64_t&)MCREG_Gp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Tp() { return (uint64_t&)MCREG_Tp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T0() { return (uint64_t&)MCREG_T0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T1() { return (uint64_t&)MCREG_T1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T2() { return (uint64_t&)MCREG_T2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Fp() { return (uint64_t&)MCREG_Fp(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S1() { return (uint64_t&)MCREG_S1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A0() { return (uint64_t&)MCREG_A0(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A1() { return (uint64_t&)MCREG_A1(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A2() { return (uint64_t&)MCREG_A2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A3() { return (uint64_t&)MCREG_A3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A4() { return (uint64_t&)MCREG_A4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A5() { return (uint64_t&)MCREG_A5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A6() { return (uint64_t&)MCREG_A6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::A7() { return (uint64_t&)MCREG_A7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S2() { return (uint64_t&)MCREG_S2(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S3() { return (uint64_t&)MCREG_S3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S4() { return (uint64_t&)MCREG_S4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S5() { return (uint64_t&)MCREG_S5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S6() { return (uint64_t&)MCREG_S6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S7() { return (uint64_t&)MCREG_S7(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S8() { return (uint64_t&)MCREG_S8(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S9() { return (uint64_t&)MCREG_S9(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S10() { return (uint64_t&)MCREG_S10(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::S11() { return (uint64_t&)MCREG_S11(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T3() { return (uint64_t&)MCREG_T3(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T4() { return (uint64_t&)MCREG_T4(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T5() { return (uint64_t&)MCREG_T5(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::T6() { return (uint64_t&)MCREG_T6(ctx.uc_mcontext); }
    uint64_t& NATIVE_CONTEXT::Pc() { return (uint64_t&)MCREG_Pc(ctx.uc_mcontext); }

#else
    PORTABILITY_ASSERT("NATIVE_CONTEXT");
#endif // TARGET_ARM

