// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// CGENX86.CPP -
// Various helper routines for generating x86 assembly code.

// Precompiled Header

#include "common.h"

#include "field.h"
#include "stublink.h"
#include "cgensys.h"
#include "frames.h"
#include "excep.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "log.h"
#include "comdelegate.h"
#include "array.h"
#include "jitinterface.h"
#include "codeman.h"
#include "dbginterface.h"
#include "eeprofinterfaces.h"
#include "eeconfig.h"
#include "asmconstants.h"
#include "class.h"
#include "virtualcallstub.h"
#include "jitinterface.h"

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "comcache.h"
#include "olevariant.h"
#endif // FEATURE_COMINTEROP

#include "stublink.inl"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * regs)
{
    LIMITED_METHOD_CONTRACT;

    T_CONTEXT * pContext = pRD->pCurrentContext;
#define CALLEE_SAVED_REGISTER(regname) pContext->regname = regs->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
#define CALLEE_SAVED_REGISTER(regname) pContextPointers->regname = (DWORD*)&regs->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
}

void ClearRegDisplayArgumentAndScratchRegisters(REGDISPLAY * pRD)
{
    LIMITED_METHOD_CONTRACT;

#define ARGUMENT_AND_SCRATCH_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_ARGUMENT_AND_SCRATCH_REGISTERS();
#undef ARGUMENT_AND_SCRATCH_REGISTER
}

void TransitionFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    MethodDesc * pFunc = GetFunction();
    _ASSERTE(pFunc != NULL);

    UpdateRegDisplayHelper(pRD, pFunc->CbStackPop());

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

void TransitionFrame::UpdateRegDisplayHelper(const PREGDISPLAY pRD, UINT cbStackPop)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CalleeSavedRegisters* regs = GetCalleeSavedRegisters();

    SetRegdisplayPCTAddr(pRD, GetReturnAddressPtr());

    DWORD CallerSP = (DWORD)(GetReturnAddressPtr() + sizeof(TADDR) + cbStackPop);

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;

    pRD->pCurrentContext->Esp = CallerSP;

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, regs);
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    RETURN;
}

void ExternalMethodFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    UpdateRegDisplayHelper(pRD, CbStackPopUsingGCRefMap(GetGCRefMap()));

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    ExternalMethodFrane::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}


void StubDispatchFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PTR_BYTE pGCRefMap = GetGCRefMap();
    if (pGCRefMap != NULL)
    {
        UpdateRegDisplayHelper(pRD, CbStackPopUsingGCRefMap(pGCRefMap));
    }
    else
    if (GetFunction() != NULL)
    {
        FramedMethodFrame::UpdateRegDisplay_Impl(pRD);
    }
    else
    {
        UpdateRegDisplayHelper(pRD, 0);

        // If we do not have owning MethodDesc, we need to pretend that
        // the call happened on the call instruction to get the ESP unwound properly.
        //
        // This path is hit when we are throwing null reference exception from
        // code:VSD_ResolveWorker or code:StubDispatchFixupWorker
        pRD->ControlPC = GetAdjustedCallAddress(pRD->ControlPC);

        // We need to set EIP to match ControlPC to ensure Thread::VirtualUnwindCallFrame
        // doesn't fail assertion on GetControlPC(pRD) == GetIP(pRD->pCurrentContext)
        // precondition.
        pRD->pCurrentContext->Eip = pRD->ControlPC;
    }

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    StubDispatchFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

PCODE StubDispatchFrame::GetReturnAddress_Impl()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCODE retAddress = FramedMethodFrame::GetReturnAddress_Impl();
    if (GetFunction() == NULL && GetGCRefMap() == NULL)
    {
        // See comment in code:StubDispatchFrame::UpdateRegDisplay
        retAddress = GetAdjustedCallAddress(retAddress);
    }
    return retAddress;
}

void FaultingExceptionFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    SetRegdisplayPCTAddr(pRD, GetReturnAddressPtr());

    memcpy(pRD->pCurrentContext, &m_ctx, sizeof(CONTEXT));

    // Clear the CONTEXT_XSTATE, since the REGDISPLAY contains just plain CONTEXT structure
    // that cannot contain any extended state.
    pRD->pCurrentContext->ContextFlags &= ~(CONTEXT_XSTATE & CONTEXT_AREA_MASK);

    pRD->SP = m_ctx.Esp;
    pRD->ControlPC = m_ctx.Eip;

#define ARGUMENT_AND_SCRATCH_REGISTER(regname) pRD->pCurrentContextPointers->regname = &m_ctx.regname;
    ENUM_ARGUMENT_AND_SCRATCH_REGISTERS();
#undef ARGUMENT_AND_SCRATCH_REGISTER

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = &m_ctx.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid = FALSE;        // Don't add usage of this field.  This is only temporary.

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    FaultingExceptionFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

void InlinedCallFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        // We should skip over InlinedCallFrame if it is not active.
        // It will be part of a JITed method's frame, and the stack-walker
        // can handle such a case.
#ifdef PROFILING_SUPPORTED
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // @TODO: Remove this after the debugger is fixed to avoid stack-walks from bad places
    // @TODO: This may be still needed for sampling profilers
    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }

    DWORD stackArgSize = 0;

#if !defined(UNIX_X86_ABI)
    TADDR datum = dac_cast<TADDR>(m_Datum);

    datum &= ~(TADDR)InlinedCallFrameMarker::Mask;

    stackArgSize = (DWORD)datum;

    if (stackArgSize & ~0xFFFF)
    {
        PInvokeMethodDesc * pMD = PTR_PInvokeMethodDesc(datum);

        /* if this is not an PInvoke frame, something is really wrong */

        _ASSERTE(pMD->SanityCheck() && pMD->IsPInvoke());

        stackArgSize = pMD->GetStackArgumentSize();
    }
#endif

    /* The return address is just above the "ESP" */
    SetRegdisplayPCTAddr(pRD, PTR_HOST_MEMBER_TADDR(InlinedCallFrame, this, m_pCallerReturnAddress));

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Esp = (DWORD) dac_cast<TADDR>(m_pCallSiteSP);
    pRD->pCurrentContext->Ebp = (DWORD) m_pCalleeSavedFP;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->pCurrentContextPointers->Ebp = (DWORD*) &m_pCalleeSavedFP;

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    InlinedCallFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

#ifdef FEATURE_HIJACK
//==========================
// Resumable Exception Frame
//
TADDR ResumableFrame::GetReturnAddressPtr_Impl()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Eip);
}

void ResumableFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    SetRegdisplayPCTAddr(pRD, dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Eip));

    CopyMemory(pRD->pCurrentContext, m_Regs, sizeof(T_CONTEXT));

    pRD->SP = m_Regs->Esp;

#define ARGUMENT_AND_SCRATCH_REGISTER(reg) pRD->pCurrentContextPointers->reg = &m_Regs->reg;
    ENUM_ARGUMENT_AND_SCRATCH_REGISTERS();
#undef ARGUMENT_AND_SCRATCH_REGISTER

#define CALLEE_SAVED_REGISTER(reg) pRD->pCurrentContextPointers->reg = &m_Regs->reg;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    ResumableFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

// The HijackFrame has to know the registers that are pushed by OnHijackTripThread
//  -> HijackFrame::UpdateRegDisplay should restore all the registers pushed by OnHijackTripThread
void HijackFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    SetRegdisplayPCTAddr(pRD, dac_cast<TADDR>(m_Args) + offsetof(HijackArgs, Eip));

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Esp = (DWORD)(GetRegdisplayPCTAddr(pRD) + sizeof(TADDR));

#define RESTORE_REG(reg) { pRD->pCurrentContext->reg = m_Args->reg; pRD->pCurrentContextPointers->reg = &m_Args->reg; }
#define CALLEE_SAVED_REGISTER(reg) RESTORE_REG(reg)
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#define ARGUMENT_AND_SCRATCH_REGISTER(reg) RESTORE_REG(reg)
    ENUM_ARGUMENT_AND_SCRATCH_REGISTERS();
#undef ARGUMENT_AND_SCRATCH_REGISTER
#undef RESTORE_REG

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HijackFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
}

#endif  // FEATURE_HIJACK

void PInvokeCalliFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    VASigCookie *pVASigCookie = GetVASigCookie();
    UpdateRegDisplayHelper(pRD, pVASigCookie->sizeOfArgs);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    PInvokeCalliFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

#ifndef UNIX_X86_ABI
void TailCallFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    SetRegdisplayPCTAddr(pRD, GetReturnAddressPtr());

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Esp = (DWORD)(GetRegdisplayPCTAddr(pRD) + sizeof(TADDR));

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &m_regs);
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TailCallFrame::UpdateRegDisplay_Impl(ip:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}
#endif // !UNIX_X86_ABI

void DynamicHelperFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    WRAPPER_NO_CONTRACT;
    UpdateRegDisplayHelper(pRD, 0);
}

//------------------------------------------------------------------------
// This is declared as returning WORD instead of PRD_TYPE because of
// header issues with cgencpu.h including dbginterface.h.
WORD GetUnpatchedCodeData(LPCBYTE pAddr)
{
#ifndef TARGET_X86
#error Make sure this works before porting to platforms other than x86.
#endif
    CONTRACT(WORD) {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CORDebuggerAttached());
        PRECONDITION(CheckPointer(pAddr));
    } CONTRACT_END;

    // Ordering is because x86 is little-endien.
    BYTE bLow  = pAddr[0];
    BYTE bHigh = pAddr[1];

#ifndef DACCESS_COMPILE
    // Need to make sure that the code we're reading is free of breakpoint patches.
    PRD_TYPE unpatchedOpcode;
    if (g_pDebugInterface->CheckGetPatchedOpcode((CORDB_ADDRESS_TYPE *)pAddr,
                                                 &unpatchedOpcode))
    {
        // PRD_TYPE is supposed to be an opaque debugger structure representing data to remove a patch.
        // Although PRD_TYPE is currently typedef'ed to be a DWORD_PTR, it's actually semantically just a BYTE.
        // (since a patch on x86 is just an 0xCC instruction).
        // Ideally, the debugger subsystem would expose a patch-code stripper that returns BYTE/WORD/etc, and
        // not force us to crack it ourselves here.
        bLow = (BYTE) unpatchedOpcode;
    }
    //
#endif

    WORD w = bLow + (bHigh << 8);
    RETURN w;
}


#ifndef DACCESS_COMPILE

//////////////////////////////////////////////////////////////////////////////
//
// JITInterface
//
//////////////////////////////////////////////////////////////////////////////

/*********************************************************************/
#ifdef FEATURE_METADATA_UPDATER
#pragma warning (disable : 4731)
void ResumeAtJit(PCONTEXT pContext, LPVOID oldESP)
{
    // No CONTRACT here, because we can't run the risk of it pushing any SEH into the
    // current method.

    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
    DWORD curESP;
    __asm mov curESP, esp
#endif

    if (oldESP)
    {
        _ASSERTE(curESP < (DWORD)(size_t)oldESP);
        // should have popped the SEH records by now as stack has been overwritten
        _ASSERTE(GetCurrentSEHRecord() > oldESP);
    }

    // For the "push Eip, ..., ret"
    _ASSERTE(curESP < pContext->Esp - sizeof(DWORD));
    pContext->Esp -= sizeof(DWORD);

    __asm {
        mov     ebp, pContext

        // Push Eip onto the targetESP, so that the final "ret" will consume it
        mov     ecx, [ebp]CONTEXT.Esp
        mov     edx, [ebp]CONTEXT.Eip
        mov     [ecx], edx

        // Restore all registers except Esp, Ebp, Eip
        mov     eax, [ebp]CONTEXT.Eax
        mov     ebx, [ebp]CONTEXT.Ebx
        mov     ecx, [ebp]CONTEXT.Ecx
        mov     edx, [ebp]CONTEXT.Edx
        mov     esi, [ebp]CONTEXT.Esi
        mov     edi, [ebp]CONTEXT.Edi

        push    [ebp]CONTEXT.Esp  // pContext->Esp is (targetESP-sizeof(DWORD))
        push    [ebp]CONTEXT.Ebp
        pop     ebp
        pop     esp

        // esp is (targetESP-sizeof(DWORD)), and [esp] is the targetEIP.
        // The ret will set eip to targetEIP and esp will be automatically
        // incremented to targetESP

        ret
    }
}
#pragma warning (default : 4731)
#endif // !FEATURE_METADATA_UPDATER

#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT_WORKER(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStartRX = (BYTE *)(void*)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    ExecutableWriterHolder<BYTE> startWriterHolder(pStartRX, cbAligned); \
    BYTE * pStart = startWriterHolder.GetRW(); \
    size_t rxOffset = pStartRX - pStart; \
    BYTE * p = pStart;

#ifdef FEATURE_PERFMAP
#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    BEGIN_DYNAMIC_HELPER_EMIT_WORKER(size) \
    PerfMap::LogStubs(__FUNCTION__, "DynamicHelper", (PCODE)p, size, PerfMapStubType::Individual);
#else
#define BEGIN_DYNAMIC_HELPER_EMIT(size) BEGIN_DYNAMIC_HELPER_EMIT_WORKER(size)
#endif

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) *p++ = X86_INSTR_INT3; \
    ClrFlushInstructionCache(pStartRX, cbAligned); \
    return (PCODE)pStartRX

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(10);

    *p++ = 0xB9; // mov ecx, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        PRECONDITION(p != NULL && target != NULL);
    }
    CONTRACTL_END;

    // Move an argument into the second argument register and jump to a target function.

    *p++ = 0xBA; // mov edx, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
    p += 4;
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(10);

    EmitHelperWithArg(p, rxOffset, pAllocator, arg, target);

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(15);

    *p++ = 0xB9; // mov ecx, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    *p++ = 0xBA; // mov edx, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg2);
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(12);

    SET_UNALIGNED_16(p, 0xD18B); // mov edx, ecx
    p += 2;

    *p++ = 0xB9; // mov ecx, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    BEGIN_DYNAMIC_HELPER_EMIT(1);

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    BEGIN_DYNAMIC_HELPER_EMIT(6);

    *p++ = 0xB8; // mov eax, XXXXXX
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    BEGIN_DYNAMIC_HELPER_EMIT((offset != 0) ? 9 : 6);

    *p++ = 0xA1; // mov eax, [XXXXXX]
    SET_UNALIGNED_32(p, (INT32)arg);
    p += 4;

    if (offset != 0)
    {
        // add eax, <offset>
        *p++ = 0x83;
        *p++ = 0xC0;
        *p++ = offset;
    }

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

EXTERN_C VOID DynamicHelperArgsStub();

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
#ifdef UNIX_X86_ABI
    BEGIN_DYNAMIC_HELPER_EMIT(18);
#else
    BEGIN_DYNAMIC_HELPER_EMIT(12);
#endif

#ifdef UNIX_X86_ABI
	// sub esp, 8
	*p++ = 0x83;
	*p++ = 0xec;
	*p++ = 0x8;
#else
    // pop eax
    *p++ = 0x58;
#endif

    // push arg
    *p++ = 0x68;
    SET_UNALIGNED_32(p, arg);
    p += 4;

#ifdef UNIX_X86_ABI
    // mov eax, target
    *p++ = 0xB8;
    SET_UNALIGNED_32(p, target);
    p += 4;
#else
    // push eax
    *p++ = 0x50;
#endif

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
#ifdef UNIX_X86_ABI
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), (PCODE)DynamicHelperArgsStub));
#else
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
#endif
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
#ifdef UNIX_X86_ABI
    BEGIN_DYNAMIC_HELPER_EMIT(23);
#else
    BEGIN_DYNAMIC_HELPER_EMIT(17);
#endif

#ifdef UNIX_X86_ABI
	// sub esp, 4
	*p++ = 0x83;
	*p++ = 0xec;
	*p++ = 0x4;
#else
    // pop eax
    *p++ = 0x58;
#endif

    // push arg
    *p++ = 0x68;
    SET_UNALIGNED_32(p, arg);
    p += 4;

    // push arg2
    *p++ = 0x68;
    SET_UNALIGNED_32(p, arg2);
    p += 4;

#ifdef UNIX_X86_ABI
    // mov eax, target
    *p++ = 0xB8;
    SET_UNALIGNED_32(p, target);
    p += 4;
#else
    // push eax
    *p++ = 0x50;
#endif

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
#ifdef UNIX_X86_ABI
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), (PCODE)DynamicHelperArgsStub));
#else
    SET_UNALIGNED_32(p, rel32UsingJumpStub((INT32 *)(p + rxOffset), target));
#endif
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    PCODE helperAddress = GetDictionaryLookupHelper(pLookup->helper);

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    ExecutableWriterHolder<GenericHandleArgs> argsWriterHolder(pArgs, sizeof(GenericHandleArgs));
    argsWriterHolder.GetRW()->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    argsWriterHolder.GetRW()->signature = pLookup->signature;
    argsWriterHolder.GetRW()->module = (CORINFO_MODULE_HANDLE)pModule;

    WORD slotOffset = (WORD)(dictionaryIndexAndSlot & 0xFFFF) * sizeof(Dictionary*);

    // It's available only via the run-time helper function
    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(10);

        // ecx contains the generic context parameter
        // mov edx,pArgs
        // jmp helperAddress
        EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int indirectionsSize = 0;
        for (WORD i = 0; i < pLookup->indirections; i++)
            indirectionsSize += (pLookup->offsets[i] >= 0x80 ? 6 : 3);

        int codeSize = indirectionsSize + (pLookup->testForNull ? 15 : 1) + (pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK ? 12 : 0);

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        BYTE* pJLECall = NULL;

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                _ASSERTE(pLookup->testForNull && i > 0);

                // cmp dword ptr[eax + sizeOffset],slotOffset
                SET_UNALIGNED_16(p, 0xb881); p += 2;
                SET_UNALIGNED_32(p, (UINT32)pLookup->sizeOffset); p += 4;
                SET_UNALIGNED_32(p, (UINT32)slotOffset); p += 4;

                // jle 'HELPER CALL'
                *p++ = 0x7e;
                pJLECall = p++;     // Offset filled later
            }

            // Move from ecx if it's the first indirection, otherwise from eax
            // mov eax,dword ptr [ecx|eax + offset]
            if (pLookup->offsets[i] >= 0x80)
            {
                SET_UNALIGNED_16(p, (i == 0 ? 0x818b : 0x808b)); p += 2;
                SET_UNALIGNED_32(p, (UINT32)pLookup->offsets[i]); p += 4;
            }
            else
            {
                SET_UNALIGNED_16(p, (i == 0 ? 0x418b : 0x408b)); p += 2;
                *p++ = (BYTE)pLookup->offsets[i];
            }
        }

        // No null test required
        if (!pLookup->testForNull)
        {
            _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);

            // No fixups needed for R2R
            *p++ = 0xC3;    // ret
        }
        else
        {
            // ecx contains the value of the dictionary slot entry

            _ASSERTE(pLookup->indirections != 0);

            // test eax,eax
            SET_UNALIGNED_16(p, 0xc085); p += 2;

            // je 'HELPER_CALL' (a jump of 1 byte)
            SET_UNALIGNED_16(p, 0x0174); p += 2;

            *p++ = 0xC3;    // ret

            // 'HELPER_CALL'
            {
                if (pJLECall != NULL)
                    *pJLECall = (BYTE)(p - pJLECall - 1);

                // ecx already contains the generic context parameter

                // mov edx,pArgs
                // jmp helperAddress
                EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);
            }
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}

#endif // FEATURE_READYTORUN


#endif // DACCESS_COMPILE
