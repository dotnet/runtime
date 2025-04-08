// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __NATIVE_CONTEXT_H__
#define __NATIVE_CONTEXT_H__

#include <windows.h>

struct NATIVE_CONTEXT
{
    CONTEXT ctx;

#if defined(TARGET_AMD64)
    uintptr_t GetIp() { return ctx.Rip; }
    uintptr_t GetSp() { return ctx.Rsp; }
    void SetIp(uintptr_t val) { ctx.Rip = val; }
    void SetSp(uintptr_t val) { ctx.Rsp = val; }
    void SetArg0Reg(uintptr_t val) { ctx.Rcx = val; }
    void SetArg1Reg(uintptr_t val) { ctx.Rdx = val; }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        for (uint64_t* pReg = &ctx.Rax; pReg < &ctx.Rip; pReg++)
            lambda((size_t*)pReg);
    }
#elif defined(TARGET_X86)
    uintptr_t GetIp() { return ctx.Eip; }
    uintptr_t GetSp() { return ctx.Esp; }
    void SetIp(uintptr_t val) { ctx.Eip = val; }
    void SetSp(uintptr_t val) { ctx.Esp = val; }
    void SetArg0Reg(uintptr_t val) { ctx.Ecx = val; }
    void SetArg1Reg(uintptr_t val) { ctx.Edx = val; }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        for (uint32_t* pReg = (uint32_t*)&ctx.Eax; pReg < (uint32_t*)&ctx.Eip; pReg++)
            lambda((size_t*)pReg);
    }
#elif defined(TARGET_ARM64)
    uintptr_t GetIp() { return ctx.Pc; }
    uintptr_t GetSp() { return ctx.Sp; }
    uintptr_t GetLr() { return ctx.Lr; }
    void SetIp(uintptr_t val) { ctx.Pc = val; }
    void SetSp(uintptr_t val) { ctx.Sp = val; }
    void SetArg0Reg(uintptr_t val) { ctx.X0 = val; }
    void SetArg1Reg(uintptr_t val) { ctx.X1 = val; }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        for (uint64_t* pReg = &ctx.X0; pReg <= &ctx.X28; pReg++)
            lambda((size_t*)pReg);

        // Lr can be used as a scratch register
        lambda((size_t*)&ctx.Lr);
    }
#endif
};

#endif // __NATIVE_CONTEXT_H__
