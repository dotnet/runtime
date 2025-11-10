// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Various helper routines for generating ppc64le assembly code.
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
	_ASSERTE(pRD->pCurrentContext->Nip == GetReturnAddress());
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Nip = GetReturnAddress();
    pRD->pCurrentContext->R1 = GetSP();

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

    pRD->pCurrentContext->Nip = *(DWORD64 *)&m_pCallerReturnAddress;
    pRD->pCurrentContext->R1 = *(DWORD64 *)&m_pCallSiteSP;
    pRD->pCurrentContext->R31 = *(DWORD64 *)&m_pCalleeSavedFP;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->pCurrentContextPointers->R31 = (DWORD64 *)&m_pCalleeSavedFP;

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
	PRECONDITION(m_MachState._pRetAddr == PTR_TADDR(&m_MachState.m_nip));
	SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
	_ASSERTE(pRD->pCurrentContext->Nip == m_MachState.m_nip);
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    //
    // Copy the saved state from the frame to the current context.
    //

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HelperMethodFrame::UpdateRegDisplay cached ip:%p, sp:%p\n", m_MachState.m_nip, m_MachState.m_sp));

#if defined(DACCESS_COMPILE)
    // For DAC, we may get here when the HMF is still uninitialized.
    // So we may need to unwind here.
    if (!m_MachState.isValid())
    {
        // This allocation throws on OOM.
	MachState* pUnwoundState = (MachState*)DacAllocHostOnlyInstance(sizeof(*pUnwoundState), true);

	InsureInit(pUnwoundState);

	pRD->pCurrentContext->Nip = pRD->ControlPC = pUnwoundState->m_nip;
	pRD->pCurrentContext->R1 = pRD->SP = pUnwoundState->m_sp;

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

    pRD->pCurrentContext->Nip = pRD->ControlPC = m_MachState.m_nip;
    pRD->pCurrentContext->R1 = pRD->SP = m_MachState.m_sp;

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

    pRD->ControlPC = m_ctx.Nip;

    pRD->SP = m_ctx.R1;

    pRD->pCurrentContextPointers->R14  = &m_ctx.R14;
    pRD->pCurrentContextPointers->R15  = &m_ctx.R15;
    pRD->pCurrentContextPointers->R16  = &m_ctx.R16;
    pRD->pCurrentContextPointers->R17  = &m_ctx.R17;
    pRD->pCurrentContextPointers->R18  = &m_ctx.R18;
    pRD->pCurrentContextPointers->R19  = &m_ctx.R19;
    pRD->pCurrentContextPointers->R20  = &m_ctx.R20;
    pRD->pCurrentContextPointers->R21  = &m_ctx.R21;
    pRD->pCurrentContextPointers->R22  = &m_ctx.R22;
    pRD->pCurrentContextPointers->R23  = &m_ctx.R23;
    pRD->pCurrentContextPointers->R24  = &m_ctx.R24;
    pRD->pCurrentContextPointers->R25  = &m_ctx.R25;
    pRD->pCurrentContextPointers->R26  = &m_ctx.R26;
    pRD->pCurrentContextPointers->R27  = &m_ctx.R27;
    pRD->pCurrentContextPointers->R28  = &m_ctx.R28;
    pRD->pCurrentContextPointers->R29  = &m_ctx.R29;
    pRD->pCurrentContextPointers->R30  = &m_ctx.R30;
    pRD->pCurrentContextPointers->R31  = &m_ctx.R31;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Nip);
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

    pRD->ControlPC = m_Regs->Nip;

    pRD->SP = m_Regs->R1;

    pRD->pCurrentContextPointers->R14  = &m_Regs->R14;
    pRD->pCurrentContextPointers->R15  = &m_Regs->R15;
    pRD->pCurrentContextPointers->R16  = &m_Regs->R16;
    pRD->pCurrentContextPointers->R17  = &m_Regs->R17;
    pRD->pCurrentContextPointers->R18  = &m_Regs->R18;
    pRD->pCurrentContextPointers->R19  = &m_Regs->R19;
    pRD->pCurrentContextPointers->R20  = &m_Regs->R20;
    pRD->pCurrentContextPointers->R21  = &m_Regs->R21;
    pRD->pCurrentContextPointers->R22  = &m_Regs->R22;
    pRD->pCurrentContextPointers->R23  = &m_Regs->R23;
    pRD->pCurrentContextPointers->R24  = &m_Regs->R24;
    pRD->pCurrentContextPointers->R25  = &m_Regs->R25;
    pRD->pCurrentContextPointers->R26  = &m_Regs->R26;
    pRD->pCurrentContextPointers->R27  = &m_Regs->R27;
    pRD->pCurrentContextPointers->R28  = &m_Regs->R28;
    pRD->pCurrentContextPointers->R29  = &m_Regs->R29;
    pRD->pCurrentContextPointers->R30  = &m_Regs->R30;
    pRD->pCurrentContextPointers->R31  = &m_Regs->R31;

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

    pRD->pCurrentContext->Nip = m_ReturnAddress;
    pRD->pCurrentContext->R1 = PTR_TO_MEMBER_TADDR(HijackArgs, m_Args, Nip) + sizeof(void *);   // FIXME TARGET_POWERPC64!!!
    
    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &(m_Args->Regs));

//    pRD->pCurrentContextPointers->R0 = NULL;
//    pRD->pCurrentContextPointers->R1 = NULL;
//    pRD->pCurrentContextPointers->R2 = NULL;
//    pRD->pCurrentContextPointers->R3 = NULL;
//    pRD->pCurrentContextPointers->R4 = NULL;
//    pRD->pCurrentContextPointers->R5 = NULL;
//    pRD->pCurrentContextPointers->R6 = NULL;
//    pRD->pCurrentContextPointers->Rax = (PULONG64)&m_Args->Rax;   // FIXME TARGET_POWERPC64!!!

    SyncRegDisplayToCurrentContext(pRD);

/*
   // This only describes the top-most frame
   pRD->pContext = NULL;

   pRD->PCTAddr = dac_cast<TADDR>(m_Args) + offsetof(HijackArgs, Nip);
   //pRD->pPC  = PTR_SLOT(pRD->PCTAddr);
   pRD->SP   = (ULONG64)(pRD->PCTAddr + sizeof(TADDR));
*/
}
#endif // FEATURE_HIJACK

#ifndef DACCESS_COMPILE
void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    _ASSERTE("TARGET_POWERPC64: NYI");
    //TODO TARGET_POWERPC64
}

void UMEntryThunkCode::Poison()
{
    //TODO TARGET_POWERPC64
    _ASSERTE("TARGET_POWERPC64: NYI");
}

UMEntryThunk* UMEntryThunk::Decode(LPVOID pCallback)
{
    //TODO TARGET_POWERPC64
    _ASSERTE("TARGET_POWERPC64: NYI");
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(offsetof(UMEntryThunkCode, m_code) == 0);
    UMEntryThunkCode * pCode = (UMEntryThunkCode*)pCallback;

    return (UMEntryThunk*)pCode->m_pvSecretParam;
}
#ifdef FEATURE_READYTORUN

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelper");
    return NULL;
}

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI EmitHelperWithArg");
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelperWithArg");
    return NULL;
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelper");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelperArgMove");
    return NULL;
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    _ASSERTE(!"PPC64LE:NYI CreateReturn");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    _ASSERTE(!"PPC64LE:NYI CreateReturnConst");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    _ASSERTE(!"PPC64LE:NYI CreateReturnIndirConst");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelperWithTwoArgs");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"PPC64LE:NYI CreateHelperWithTwoArgs");
    return NULL;
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    _ASSERTE(!"PPC64LE:NYI CreateDictionaryLookupHelper");
    return NULL;
}

#endif // FEATURE_READYTORUN

#endif // DACCESS_COMPILE
