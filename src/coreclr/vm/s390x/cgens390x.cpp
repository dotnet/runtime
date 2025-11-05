// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Various helper routines for generating S390X assembly code.
//

// Precompiled Header

#include "common.h"

#include "stublink.h"
#include "cgensys.h"
#include "siginfo.hpp"
#include "excep.h"
#include "ecall.h"
#include "dllimport.h"
#include "dllimportcallback.h"
#include "dbginterface.h"
#include "fcall.h"
#include "array.h"
#include "virtualcallstub.h"
#include "jitinterface.h"

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pRegs)
{
    LIMITED_METHOD_CONTRACT;

    T_CONTEXT * pContext = pRD->pCurrentContext;
#define CALLEE_SAVED_REGISTER(regname) pContext->regname = pRegs->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
#define CALLEE_SAVED_REGISTER(regname) pContextPointers->regname = (PULONG64)&pRegs->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
}

void ClearRegDisplayArgumentAndScratchRegisters(REGDISPLAY * pRD)
{
    LIMITED_METHOD_CONTRACT;

    KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
//    pContextPointers->R0 = NULL;
//    pContextPointers->R1 = NULL;
//    pContextPointers->R2 = NULL;
//    pContextPointers->R3 = NULL;
//    pContextPointers->R4 = NULL;
//    pContextPointers->R5 = NULL;
}

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
        _ASSERTE(pRD->pCurrentContext->PSWAddr == GetReturnAddress());
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->PSWAddr = GetReturnAddress();
    pRD->pCurrentContext->R15 = GetSP();

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, GetCalleeSavedRegisters());
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
#ifdef PROFILING_SUPPORTED
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->PSWAddr = *(DWORD64 *)&m_pCallerReturnAddress;
    pRD->pCurrentContext->R15 = *(DWORD64 *)&m_pCallSiteSP;
    pRD->pCurrentContext->R11 = *(DWORD64 *)&m_pCalleeSavedFP;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->pCurrentContextPointers->R11 = (DWORD64 *)&m_pCalleeSavedFP;

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    InlinedCallFrame::UpdateRegDisplay(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
}

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_MachState._pRetAddr == PTR_TADDR(&m_MachState.m_Rip));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
        _ASSERTE(pRD->pCurrentContext->PSWAddr == m_MachState.m_Rip);
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    //
    // Copy the saved state from the frame to the current context.
    //

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HelperMethodFrame::UpdateRegDisplay cached ip:%p, sp:%p\n", m_MachState.m_Rip, m_MachState.m_Rsp));

#if defined(DACCESS_COMPILE)
    // For DAC, we may get here when the HMF is still uninitialized.
    // So we may need to unwind here.
    if (!m_MachState.isValid())
    {
        // This allocation throws on OOM.
        MachState* pUnwoundState = (MachState*)DacAllocHostOnlyInstance(sizeof(*pUnwoundState), true);

        InsureInit(pUnwoundState);

        pRD->pCurrentContext->PSWAddr = pRD->ControlPC = pUnwoundState->m_Rip;
        pRD->pCurrentContext->R15 = pRD->SP = pUnwoundState->m_Rsp;

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = pUnwoundState->m_Capture.regname;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = pUnwoundState->m_Ptrs.p##regname;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

        ClearRegDisplayArgumentAndScratchRegisters(pRD);

        return;
    }
#endif // DACCESS_COMPILE

    pRD->pCurrentContext->PSWAddr = pRD->ControlPC = m_MachState.m_Rip;
    pRD->pCurrentContext->R15 = pRD->SP = m_MachState.m_Rsp;

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = (m_MachState.m_Ptrs.p##regname != NULL) ? \
        *m_MachState.m_Ptrs.p##regname : m_MachState.m_Unwound.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = m_MachState.m_Ptrs.p##regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    //
    // Clear all knowledge of scratch registers.  We're skipping to any
    // arbitrary point on the stack, and frames aren't required to preserve or
    // keep track of these anyways.
    //

    ClearRegDisplayArgumentAndScratchRegisters(pRD);
}

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    LIMITED_METHOD_DAC_CONTRACT;

    memcpy(pRD->pCurrentContext, &m_ctx, sizeof(CONTEXT));

    pRD->ControlPC = m_ctx.PSWAddr;

    pRD->SP = m_ctx.R15;

//    pRD->pCurrentContextPointers->R0  = &m_ctx.R0;
//    pRD->pCurrentContextPointers->R1  = &m_ctx.R1;
//    pRD->pCurrentContextPointers->R2  = &m_ctx.R2;
//    pRD->pCurrentContextPointers->R3  = &m_ctx.R3;
//    pRD->pCurrentContextPointers->R4  = &m_ctx.R4;
//    pRD->pCurrentContextPointers->R5  = &m_ctx.R5;
    pRD->pCurrentContextPointers->R6  = &m_ctx.R6;
    pRD->pCurrentContextPointers->R7  = &m_ctx.R7;
    pRD->pCurrentContextPointers->R8  = &m_ctx.R8;
    pRD->pCurrentContextPointers->R9  = &m_ctx.R9;
    pRD->pCurrentContextPointers->R10 = &m_ctx.R10;
    pRD->pCurrentContextPointers->R11 = &m_ctx.R11;
    pRD->pCurrentContextPointers->R12 = &m_ctx.R12;
    pRD->pCurrentContextPointers->R13 = &m_ctx.R13;
    pRD->pCurrentContextPointers->R14 = &m_ctx.R14;
    //pRD->pCurrentContextPointers->R15 = &m_ctx.R15;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, PSWAddr);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CopyMemory(pRD->pCurrentContext, m_Regs, sizeof(CONTEXT));

    pRD->ControlPC = m_Regs->PSWAddr;

    pRD->SP = m_Regs->R15;

//    pRD->pCurrentContextPointers->R0  = &m_Regs->R0;
//    pRD->pCurrentContextPointers->R1  = &m_Regs->R1;
//    pRD->pCurrentContextPointers->R2  = &m_Regs->R2;
//    pRD->pCurrentContextPointers->R3  = &m_Regs->R3;
//    pRD->pCurrentContextPointers->R4  = &m_Regs->R4;
//    pRD->pCurrentContextPointers->R5  = &m_Regs->R5;
    pRD->pCurrentContextPointers->R6  = &m_Regs->R6;
    pRD->pCurrentContextPointers->R7  = &m_Regs->R7;
    pRD->pCurrentContextPointers->R8  = &m_Regs->R8;
    pRD->pCurrentContextPointers->R9  = &m_Regs->R9;
    pRD->pCurrentContextPointers->R10 = &m_Regs->R10;
    pRD->pCurrentContextPointers->R11 = &m_Regs->R11;
    pRD->pCurrentContextPointers->R12 = &m_Regs->R12;
    pRD->pCurrentContextPointers->R13 = &m_Regs->R13;
    pRD->pCurrentContextPointers->R14 = &m_Regs->R14;
    //pRD->pCurrentContextPointers->R15 = &m_Regs->R15;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    RETURN;
}

// The HijackFrame has to know the registers that are pushed by OnHijackTripThread
void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->PSWAddr = m_ReturnAddress;
    pRD->pCurrentContext->R15 = PTR_TO_MEMBER_TADDR(HijackArgs, m_Args, Rip) + sizeof(void *);   // FIXME!!!

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &(m_Args->Regs));

//    pRD->pCurrentContextPointers->R0 = NULL;
//    pRD->pCurrentContextPointers->R1 = NULL;
//    pRD->pCurrentContextPointers->R2 = NULL;
//    pRD->pCurrentContextPointers->R3 = NULL;
//    pRD->pCurrentContextPointers->R4 = NULL;
//    pRD->pCurrentContextPointers->R5 = NULL;

//    pRD->pCurrentContextPointers->R6 = NULL;

//    pRD->pCurrentContextPointers->Rax = (PULONG64)&m_Args->Rax;   // FIXME!!!

    SyncRegDisplayToCurrentContext(pRD);

/*
    // This only describes the top-most frame
    pRD->pContext = NULL;


    pRD->PCTAddr = dac_cast<TADDR>(m_Args) + offsetof(HijackArgs, Rip);
    //pRD->pPC  = PTR_SLOT(pRD->PCTAddr);
    pRD->SP   = (ULONG64)(pRD->PCTAddr + sizeof(TADDR));
*/
}
#endif // FEATURE_HIJACK

#ifndef DACCESS_COMPILE

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    int n = 0;

    // lgrl %r0, m_pvSecretParam
    _ASSERTE((UINT16*)&m_pvSecretParam == &m_code[n + 12]);
    m_code[n++] = 0xc408;
    m_code[n++] = 0;
    m_code[n++] = 12;
    // lgrl %r1, m_pTargetCode
    _ASSERTE((UINT16*)&m_pTargetCode == &m_code[n + 5]);
    m_code[n++] = 0xc418;
    m_code[n++] = 0;
    m_code[n++] = 5;
    // br %r1
    m_code[n++] = 0x07f1;
    // 2 bytes padding
    m_code[n++] = 0x0707;
    _ASSERTE(n == ARRAY_SIZE(m_code));

    m_pTargetCode = (TADDR)pTargetCode;
    m_pvSecretParam = (TADDR)pvSecretParam;
    FlushInstructionCache(GetCurrentProcess(),&pEntryThunkCodeRX->m_code,sizeof(m_code));
}

void UMEntryThunkCode::Poison()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ExecutableWriterHolder<UMEntryThunkCode> thunkWriterHolder(this, sizeof(UMEntryThunkCode));
    UMEntryThunkCode *pThisRW = thunkWriterHolder.GetRW();

    pThisRW->m_pTargetCode = (TADDR)UMEntryThunk::ReportViolation;

    // lgrl %r2, m_pvSecretParam
    pThisRW->m_code[0] = 0xc428;

    ClrFlushInstructionCache(&m_code,sizeof(m_code));
}

UMEntryThunk* UMEntryThunk::Decode(LPVOID pCallback)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(offsetof(UMEntryThunkCode, m_code) == 0);
    UMEntryThunkCode * pCode = (UMEntryThunkCode*)pCallback;

    return (UMEntryThunk*)pCode->m_pvSecretParam;
}

#ifdef FEATURE_READYTORUN

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    _ASSERTE(!"S390X:NYI");
    return NULL;
}

#endif // FEATURE_READYTORUN

#endif // DACCESS_COMPILE
