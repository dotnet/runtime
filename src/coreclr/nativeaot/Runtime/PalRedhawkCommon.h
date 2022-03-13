// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Provide common definitions between the Redhawk and the Redhawk PAL implementation. This header file is used
// (rather than PalRedhawk.h) since the PAL implementation is built in a different environment than Redhawk
// code. For instance both environments may provide a definition of various common macros such as NULL.
//
// This header contains only environment neutral definitions (i.e. using only base C++ types and compositions
// of those types) and can thus be included from either environment without issue.
//

#ifndef __PAL_REDHAWK_COMMON_INCLUDED
#define __PAL_REDHAWK_COMMON_INCLUDED

#include "rhassert.h"

#ifndef DECLSPEC_ALIGN
#ifdef _MSC_VER
#define DECLSPEC_ALIGN(x)   __declspec(align(x))
#else
#define DECLSPEC_ALIGN(x)   __attribute__((aligned(x)))
#endif
#endif // DECLSPEC_ALIGN

#ifdef HOST_AMD64
#define AMD64_ALIGN_16 DECLSPEC_ALIGN(16)
#else // HOST_AMD64
#define AMD64_ALIGN_16
#endif // HOST_AMD64

struct AMD64_ALIGN_16 Fp128 {
    uint64_t Low;
    int64_t High;
};


struct PAL_LIMITED_CONTEXT
{
    // Includes special registers, callee saved registers and general purpose registers used to return values from functions (not floating point return registers)
#ifdef TARGET_ARM
    uintptr_t  R0;
    uintptr_t  R4;
    uintptr_t  R5;
    uintptr_t  R6;
    uintptr_t  R7;
    uintptr_t  R8;
    uintptr_t  R9;
    uintptr_t  R10;
    uintptr_t  R11;

    uintptr_t  IP;
    uintptr_t  SP;
    uintptr_t  LR;

    uint64_t      D[16-8]; // D8 .. D15 registers (D16 .. D31 are volatile according to the ABI spec)

    uintptr_t GetIp() const { return IP; }
    uintptr_t GetSp() const { return SP; }
    uintptr_t GetFp() const { return R7; }
    uintptr_t GetLr() const { return LR; }
    void SetIp(uintptr_t ip) { IP = ip; }
    void SetSp(uintptr_t sp) { SP = sp; }
#elif defined(TARGET_ARM64)
    uintptr_t  FP;
    uintptr_t  LR;

    uintptr_t  X0;
    uintptr_t  X1;
    uintptr_t  X19;
    uintptr_t  X20;
    uintptr_t  X21;
    uintptr_t  X22;
    uintptr_t  X23;
    uintptr_t  X24;
    uintptr_t  X25;
    uintptr_t  X26;
    uintptr_t  X27;
    uintptr_t  X28;

    uintptr_t  SP;
    uintptr_t  IP;

    uint64_t      D[16 - 8];  // Only the bottom 64-bit value of the V registers V8..V15 needs to be preserved
                            // (V0-V7 and V16-V31 are not preserved according to the ABI spec).


    uintptr_t GetIp() const { return IP; }
    uintptr_t GetSp() const { return SP; }
    uintptr_t GetFp() const { return FP; }
    uintptr_t GetLr() const { return LR; }
    void SetIp(uintptr_t ip) { IP = ip; }
    void SetSp(uintptr_t sp) { SP = sp; }
#elif defined(UNIX_AMD64_ABI)
    // Param regs: rdi, rsi, rdx, rcx, r8, r9, scratch: rax, rdx (both return val), preserved: rbp, rbx, r12-r15
    uintptr_t  IP;
    uintptr_t  Rsp;
    uintptr_t  Rbp;
    uintptr_t  Rax;
    uintptr_t  Rbx;
    uintptr_t  Rdx;
    uintptr_t  R12;
    uintptr_t  R13;
    uintptr_t  R14;
    uintptr_t  R15;

    uintptr_t GetIp() const { return IP; }
    uintptr_t GetSp() const { return Rsp; }
    void SetIp(uintptr_t ip) { IP = ip; }
    void SetSp(uintptr_t sp) { Rsp = sp; }
    uintptr_t GetFp() const { return Rbp; }
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
    uintptr_t  IP;
    uintptr_t  Rsp;
    uintptr_t  Rbp;
    uintptr_t  Rdi;
    uintptr_t  Rsi;
    uintptr_t  Rax;
    uintptr_t  Rbx;
#ifdef TARGET_AMD64
    uintptr_t  R12;
    uintptr_t  R13;
    uintptr_t  R14;
    uintptr_t  R15;
    uintptr_t  __explicit_padding__;
    Fp128       Xmm6;
    Fp128       Xmm7;
    Fp128       Xmm8;
    Fp128       Xmm9;
    Fp128       Xmm10;
    Fp128       Xmm11;
    Fp128       Xmm12;
    Fp128       Xmm13;
    Fp128       Xmm14;
    Fp128       Xmm15;
#endif // TARGET_AMD64

    uintptr_t GetIp() const { return IP; }
    uintptr_t GetSp() const { return Rsp; }
    uintptr_t GetFp() const { return Rbp; }
    void SetIp(uintptr_t ip) { IP = ip; }
    void SetSp(uintptr_t sp) { Rsp = sp; }
#else // TARGET_ARM
    uintptr_t  IP;

    uintptr_t GetIp() const { PORTABILITY_ASSERT("GetIp");  return 0; }
    uintptr_t GetSp() const { PORTABILITY_ASSERT("GetSp"); return 0; }
    uintptr_t GetFp() const { PORTABILITY_ASSERT("GetFp"); return 0; }
    void SetIp(uintptr_t ip) { PORTABILITY_ASSERT("SetIp"); }
    void SetSp(uintptr_t sp) { PORTABILITY_ASSERT("GetSp"); }
#endif // TARGET_ARM
};

void RuntimeThreadShutdown(void* thread);

typedef void (*ThreadExitCallback)();

extern ThreadExitCallback g_threadExitCallback;

#ifdef TARGET_UNIX
typedef int32_t (*PHARDWARE_EXCEPTION_HANDLER)(uintptr_t faultCode, uintptr_t faultAddress, PAL_LIMITED_CONTEXT* palContext, uintptr_t* arg0Reg, uintptr_t* arg1Reg);
#endif

#endif // __PAL_REDHAWK_COMMON_INCLUDED
