// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __UNIX_CONTEXT_H__
#define __UNIX_CONTEXT_H__

#include <ucontext.h>

// Convert Unix native context to PAL_LIMITED_CONTEXT
void NativeContextToPalContext(const void* context, PAL_LIMITED_CONTEXT* palContext);
// Redirect Unix native context to the PAL_LIMITED_CONTEXT and also set the first two argument registers
void RedirectNativeContext(void* context, const PAL_LIMITED_CONTEXT* palContext, uintptr_t arg0Reg, uintptr_t arg1Reg);

#ifdef HOST_AMD64
// Get value of a register from the native context. The index is the processor specific
// register index stored in machine instructions.
uint64_t GetRegisterValueByIndex(void* context, uint32_t index);
// Get value of the program counter from the native context
uint64_t GetPC(void* context);
#endif // HOST_AMD64

struct UNIX_CONTEXT
{
    ucontext_t ctx;

#ifdef TARGET_ARM64

    uint64_t& X0();
    uint64_t& X1();
    uint64_t& X2();
    uint64_t& X3();
    uint64_t& X4();
    uint64_t& X5();
    uint64_t& X6();
    uint64_t& X7();
    uint64_t& X8();
    uint64_t& X9();
    uint64_t& X10();
    uint64_t& X11();
    uint64_t& X12();
    uint64_t& X13();
    uint64_t& X14();
    uint64_t& X15();
    uint64_t& X16();
    uint64_t& X17();
    uint64_t& X18();
    uint64_t& X19();
    uint64_t& X20();
    uint64_t& X21();
    uint64_t& X22();
    uint64_t& X23();
    uint64_t& X24();
    uint64_t& X25();
    uint64_t& X26();
    uint64_t& X27();
    uint64_t& X28();
    uint64_t& Fp(); // X29
    uint64_t& Lr(); // X30
    uint64_t& Sp();
    uint64_t& Pc();

    uintptr_t GetIp() { return (uintptr_t)Pc(); }
    uintptr_t GetSp() { return (uintptr_t)Sp(); }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        // it is doubtful anyone would implement X0-X28 not as a contiguous array
        // just in case - here are some asserts.
        ASSERT(&X0() + 1 == &X1());
        ASSERT(&X0() + 10 == &X10());
        ASSERT(&X0() + 20 == &X20());

        for (uint64_t* pReg = &X0(); pReg <= &X28(); pReg++)
            lambda((size_t*)pReg);

        // Lr can be used as a scratch register
        lambda((size_t*)&Lr());
    }

#elif defined(TARGET_AMD64)
    uint64_t& Rax();
    uint64_t& Rcx();
    uint64_t& Rdx();
    uint64_t& Rbx();
    uint64_t& Rsp();
    uint64_t& Rbp();
    uint64_t& Rsi();
    uint64_t& Rdi();
    uint64_t& R8 ();
    uint64_t& R9 ();
    uint64_t& R10();
    uint64_t& R11();
    uint64_t& R12();
    uint64_t& R13();
    uint64_t& R14();
    uint64_t& R15();
    uint64_t& Rip();

    uintptr_t GetIp() { return (uintptr_t)Rip(); }
    uintptr_t GetSp() { return (uintptr_t)Rsp(); }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        lambda((size_t*)&Rax());
        lambda((size_t*)&Rcx());
        lambda((size_t*)&Rdx());
        lambda((size_t*)&Rbx());
        lambda((size_t*)&Rsp());
        lambda((size_t*)&Rbp());
        lambda((size_t*)&Rsi());
        lambda((size_t*)&Rdi());
        lambda((size_t*)&R8());
        lambda((size_t*)&R9());
        lambda((size_t*)&R10());
        lambda((size_t*)&R11());
        lambda((size_t*)&R12());
        lambda((size_t*)&R13());
        lambda((size_t*)&R14());
        lambda((size_t*)&R15());
    }
#elif defined(TARGET_ARM)
    uint64_t& Pc();
    uint64_t& Sp();
    uint64_t& Lr();
    uint64_t& R0();
    uint64_t& R1();
    uint64_t& R2();
    uint64_t& R3();
    uint64_t& R4();
    uint64_t& R5();
    uint64_t& R6();
    uint64_t& R7();
    uint64_t& R8();
    uint64_t& R9();
    uint64_t& R10();
    uint64_t& R11();
    uint64_t& R12();

    uintptr_t GetIp() { return (uintptr_t)Pc(); }
    uintptr_t GetSp() { return (uintptr_t)Sp(); }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        lambda((size_t*)&R0());
        lambda((size_t*)&R1());
        lambda((size_t*)&R2());
        lambda((size_t*)&R3());
        lambda((size_t*)&R4());
        lambda((size_t*)&R5());
        lambda((size_t*)&R6());
        lambda((size_t*)&R7());
        lambda((size_t*)&R8());
        lambda((size_t*)&R9());
        lambda((size_t*)&R10());
        lambda((size_t*)&R11());
        lambda((size_t*)&R12());
    }
#else
    PORTABILITY_ASSERT("UNIX_CONTEXT");
#endif // TARGET_ARM
};

#endif // __UNIX_CONTEXT_H__
