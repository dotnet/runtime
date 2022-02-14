// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "regdisplay.h"
#include "config.h"

#include <libunwind.h>

#if HAVE_UCONTEXT_T
#include <ucontext.h>
#endif  // HAVE_UCONTEXT_T

#include "UnixContext.h"
#include "UnwindHelpers.h"

// WebAssembly has a slightly different version of LibUnwind that doesn't define unw_get_save_loc
#if defined(HOST_WASM)
enum unw_save_loc_type_t
{
    UNW_SLT_NONE,       /* register is not saved ("not an l-value") */
    UNW_SLT_MEMORY,     /* register has been saved in memory */
    UNW_SLT_REG         /* register has been saved in (another) register */
};
typedef enum unw_save_loc_type_t unw_save_loc_type_t;

struct unw_save_loc_t
{
    unw_save_loc_type_t type;
    union
    {
        unw_word_t addr;        /* valid if type==UNW_SLT_MEMORY */
        unw_regnum_t regnum;    /* valid if type==UNW_SLT_REG */
    }
    u;
};
typedef struct unw_save_loc_t unw_save_loc_t;

int unw_get_save_loc(unw_cursor_t*, int, unw_save_loc_t*)
{
    return -1;
}
#endif // _WASM

#ifdef __APPLE__

#ifdef HOST_ARM64

#define MCREG_Pc(mc)      ((mc)->__ss.__pc)
#define MCREG_Sp(mc)      ((mc)->__ss.__sp)
#define MCREG_Lr(mc)      ((mc)->__ss.__lr)
#define MCREG_X0(mc)      ((mc)->__ss.__x[0])
#define MCREG_X1(mc)      ((mc)->__ss.__x[1])
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

#if defined(HOST_ARM64)

#define MCREG_Pc(mc)      ((mc).pc)
#define MCREG_Sp(mc)      ((mc).sp)
#define MCREG_Lr(mc)      ((mc).regs[30])
#define MCREG_X0(mc)      ((mc).regs[0])
#define MCREG_X1(mc)      ((mc).regs[1])
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
#define MCREG_R4(mc)        ((mc).arm_r4)
#define MCREG_R5(mc)        ((mc).arm_r5)
#define MCREG_R6(mc)        ((mc).arm_r6)
#define MCREG_R7(mc)        ((mc).arm_r7)
#define MCREG_R8(mc)        ((mc).arm_r8)
#define MCREG_R9(mc)        ((mc).arm_r9)
#define MCREG_R10(mc)       ((mc).arm_r10)
#define MCREG_R11(mc)       ((mc).arm_fp)

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

// Update unw_cursor_t from REGDISPLAY.
// NOTE: We don't set the IP here since the current use cases for this function
// don't require it.
static void RegDisplayToUnwindCursor(REGDISPLAY* regDisplay, unw_cursor_t *cursor)
{
#define ASSIGN_REG(regName1, regName2) \
    unw_set_reg(cursor, regName1, regDisplay->regName2, 0);

#define ASSIGN_REG_PTR(regName1, regName2) \
    if (regDisplay->p##regName2 != NULL) \
        unw_set_reg(cursor, regName1, *(regDisplay->p##regName2), 0);

#if defined(HOST_AMD64)
    ASSIGN_REG(UNW_REG_SP, SP)
    ASSIGN_REG_PTR(UNW_X86_64_RBP, Rbp)
    ASSIGN_REG_PTR(UNW_X86_64_RBX, Rbx)
    ASSIGN_REG_PTR(UNW_X86_64_R12, R12)
    ASSIGN_REG_PTR(UNW_X86_64_R13, R13)
    ASSIGN_REG_PTR(UNW_X86_64_R14, R14)
    ASSIGN_REG_PTR(UNW_X86_64_R15, R15)
#elif HOST_ARM
    ASSIGN_REG(UNW_ARM_SP, SP)
    ASSIGN_REG_PTR(UNW_ARM_R4, R4)
    ASSIGN_REG_PTR(UNW_ARM_R5, R5)
    ASSIGN_REG_PTR(UNW_ARM_R6, R6)
    ASSIGN_REG_PTR(UNW_ARM_R7, R7)
    ASSIGN_REG_PTR(UNW_ARM_R8, R8)
    ASSIGN_REG_PTR(UNW_ARM_R9, R9)
    ASSIGN_REG_PTR(UNW_ARM_R10, R10)
    ASSIGN_REG_PTR(UNW_ARM_R11, R11)
    ASSIGN_REG_PTR(UNW_ARM_R14, LR)
#elif HOST_ARM64
    ASSIGN_REG(UNW_ARM64_SP, SP)
    ASSIGN_REG_PTR(UNW_ARM64_FP, FP)
    ASSIGN_REG_PTR(UNW_ARM64_X19, X19)
    ASSIGN_REG_PTR(UNW_ARM64_X20, X20)
    ASSIGN_REG_PTR(UNW_ARM64_X21, X21)
    ASSIGN_REG_PTR(UNW_ARM64_X22, X22)
    ASSIGN_REG_PTR(UNW_ARM64_X23, X23)
    ASSIGN_REG_PTR(UNW_ARM64_X24, X24)
    ASSIGN_REG_PTR(UNW_ARM64_X25, X25)
    ASSIGN_REG_PTR(UNW_ARM64_X26, X26)
    ASSIGN_REG_PTR(UNW_ARM64_X27, X27)
    ASSIGN_REG_PTR(UNW_ARM64_X28, X28)
#elif defined(HOST_X86)
    ASSIGN_REG(UNW_REG_SP, SP)
    ASSIGN_REG_PTR(UNW_X86_EBP, Rbp)
    ASSIGN_REG_PTR(UNW_X86_EBX, Rbx)
#endif

#undef ASSIGN_REG
#undef ASSIGN_REG_PTR
}

// Returns the unw_proc_info_t for a given IP.
bool GetUnwindProcInfo(PCODE ip, unw_proc_info_t *procInfo)
{
    int st;

    unw_context_t unwContext;
    unw_cursor_t cursor;

    st = unw_getcontext(&unwContext);
    if (st < 0)
    {
        return false;
    }

#ifdef HOST_AMD64
    // We manually index into the unw_context_t's internals for now because there's
    // no better way to modify it. This will go away in the future when we locate the
    // LSDA and other information without initializing an unwind cursor.
    unwContext.data[16] = ip;
#elif HOST_ARM
    ((uint32_t*)(unwContext.data))[15] = ip;
#elif HOST_ARM64
    unwContext.data[32] = ip;
#elif HOST_WASM
    ASSERT(false);
#elif HOST_X86
    ASSERT(false);
#else
    #error "GetUnwindProcInfo is not supported on this arch yet."
#endif

    st = unw_init_local(&cursor, &unwContext);
    if (st < 0)
    {
        return false;
    }

    st = unw_get_proc_info(&cursor, procInfo);
    if (st < 0)
    {
        return false;
    }

    return true;
}

// Initialize unw_cursor_t and unw_context_t from REGDISPLAY
bool InitializeUnwindContextAndCursor(REGDISPLAY* regDisplay, unw_cursor_t* cursor, unw_context_t* unwContext)
{
    int st;

    st = unw_getcontext(unwContext);
    if (st < 0)
    {
        return false;
    }

    // Set the IP here instead of after unwinder initialization. unw_init_local
    // will do some initialization of internal structures based on the IP value.
    // We manually index into the unw_context_t's internals for now because there's
    // no better way to modify it. This whole function will go away in the future
    // when we are able to read unwind info without initializing an unwind cursor.
#ifdef HOST_AMD64
    unwContext->data[16] = regDisplay->IP;
#elif HOST_ARM
    ((uint32_t*)(unwContext->data))[15] = regDisplay->IP;
#elif HOST_ARM64
    ((uint32_t*)(unwContext->data))[32] = regDisplay->IP;
#elif HOST_X86
    ASSERT(false);
#else
    #error "InitializeUnwindContextAndCursor is not supported on this arch yet."
#endif

    st = unw_init_local(cursor, unwContext);
    if (st < 0)
    {
        return false;
    }

    // Set the unwind context to the specified Windows context.
    RegDisplayToUnwindCursor(regDisplay, cursor);

    return true;
}

// Update context pointer for a register from the unw_cursor_t.
static void GetContextPointer(unw_cursor_t *cursor, unw_context_t *unwContext, int reg, PTR_UIntNative *contextPointer)
{
    unw_save_loc_t saveLoc;
    unw_get_save_loc(cursor, reg, &saveLoc);
    if (saveLoc.type == UNW_SLT_MEMORY)
    {
        PTR_UIntNative pLoc = (PTR_UIntNative)saveLoc.u.addr;
        // Filter out fake save locations that point to unwContext
        if (unwContext == NULL || (pLoc < (PTR_UIntNative)unwContext) || ((PTR_UIntNative)(unwContext + 1) <= pLoc))
            *contextPointer = (PTR_UIntNative)saveLoc.u.addr;
    }
}

#if defined(HOST_AMD64)
#define GET_CONTEXT_POINTERS                    \
    GET_CONTEXT_POINTER(UNW_X86_64_RBP, Rbp)	\
    GET_CONTEXT_POINTER(UNW_X86_64_RBX, Rbx)    \
    GET_CONTEXT_POINTER(UNW_X86_64_R12, R12)    \
    GET_CONTEXT_POINTER(UNW_X86_64_R13, R13)    \
    GET_CONTEXT_POINTER(UNW_X86_64_R14, R14)    \
    GET_CONTEXT_POINTER(UNW_X86_64_R15, R15)
#elif defined(HOST_ARM)
#define GET_CONTEXT_POINTERS                    \
    GET_CONTEXT_POINTER(UNW_ARM_R4, R4)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R5, R5)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R6, R6)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R7, R7)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R8, R8)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R9, R9)	        \
    GET_CONTEXT_POINTER(UNW_ARM_R10, R10)       \
    GET_CONTEXT_POINTER(UNW_ARM_R11, R11)
#elif defined(HOST_ARM64)
#define GET_CONTEXT_POINTERS                    \
    GET_CONTEXT_POINTER(UNW_ARM64_X19, X19)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X20, X20)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X21, X21)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X22, X22)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X23, X23)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X24, X24)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X25, X25)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X26, X26)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X27, X27)	\
    GET_CONTEXT_POINTER(UNW_ARM64_X28, X28)	\
    GET_CONTEXT_POINTER(UNW_ARM64_FP, FP)
#elif defined(HOST_X86)
#define GET_CONTEXT_POINTERS                    \
    GET_CONTEXT_POINTER(UNW_X86_EBP, Rbp)       \
    GET_CONTEXT_POINTER(UNW_X86_EBX, Rbx)
#elif defined (HOST_WASM)
// No registers
#define GET_CONTEXT_POINTERS
#else
#error unsupported architecture
#endif

// Update REGDISPLAY from the unw_cursor_t and unw_context_t
void UnwindCursorToRegDisplay(unw_cursor_t *cursor, unw_context_t *unwContext, REGDISPLAY *regDisplay)
{
#define GET_CONTEXT_POINTER(unwReg, rdReg) GetContextPointer(cursor, unwContext, unwReg, &regDisplay->p##rdReg);
    GET_CONTEXT_POINTERS
#undef GET_CONTEXT_POINTER

    unw_get_reg(cursor, UNW_REG_IP, (unw_word_t *) &regDisplay->IP);
    unw_get_reg(cursor, UNW_REG_SP, (unw_word_t *) &regDisplay->SP);

#if defined(HOST_AMD64)
    regDisplay->pIP = PTR_PCODE(regDisplay->SP - sizeof(TADDR));
#endif

#if defined(HOST_ARM) || defined(HOST_ARM64)
    regDisplay->IP |= 1;
#endif
}

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

// Find LSDA and start address for a function at address controlPC
bool FindProcInfo(uintptr_t controlPC, uintptr_t* startAddress, uintptr_t* lsda)
{
    unw_proc_info_t procInfo;

    if (!GetUnwindProcInfo((PCODE)controlPC, &procInfo))
    {
        return false;
    }

    assert((procInfo.start_ip <= controlPC) && (controlPC < procInfo.end_ip));

#if defined(HOST_ARM)
    // libunwind fills by reference not by value for ARM
    *lsda = *((uintptr_t *)procInfo.lsda);
#else
    *lsda = procInfo.lsda;
#endif
    *startAddress = procInfo.start_ip;

    return true;
}

// Virtually unwind stack to the caller of the context specified by the REGDISPLAY
bool VirtualUnwind(REGDISPLAY* pRegisterSet)
{
    return UnwindHelpers::StepFrame(pRegisterSet);
}
