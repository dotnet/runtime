// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Various helper routines for generating AMD64 assembly code.
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

#ifdef FEATURE_COMINTEROP
#include "clrtocomcall.h"
#endif // FEATURE_COMINTEROP

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
    pContextPointers->Rax = NULL;
#ifdef UNIX_AMD64_ABI
    pContextPointers->Rsi = NULL;
    pContextPointers->Rdi = NULL;
#endif
    pContextPointers->Rcx = NULL;
    pContextPointers->Rdx = NULL;
    pContextPointers->R8  = NULL;
    pContextPointers->R9  = NULL;
    pContextPointers->R10 = NULL;
    pContextPointers->R11 = NULL;
}

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD, GetSP());
        _ASSERTE(pRD->pCurrentContext->Rip == GetReturnAddress());
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Rip = GetReturnAddress();
    pRD->pCurrentContext->Rsp = GetSP();

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, GetCalleeSavedRegisters());
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
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
        HOST_NOCALLS;
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
        UpdateFloatingPointRegisters(pRD, *(DWORD64 *)&m_pCallSiteSP);
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Rip = *(DWORD64 *)&m_pCallerReturnAddress;
    pRD->pCurrentContext->Rsp = *(DWORD64 *)&m_pCallSiteSP;
    pRD->pCurrentContext->Rbp = *(DWORD64 *)&m_pCalleeSavedFP;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    pRD->pCurrentContextPointers->Rbp = (DWORD64 *)&m_pCalleeSavedFP;

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    InlinedCallFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
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
        UpdateFloatingPointRegisters(pRD,  m_MachState.m_Rsp);
        _ASSERTE(pRD->pCurrentContext->Rip == m_MachState.m_Rip);
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

        InsureInit(false, pUnwoundState);

        pRD->pCurrentContext->Rip = pRD->ControlPC = pUnwoundState->m_Rip;
        pRD->pCurrentContext->Rsp = pRD->SP        = pUnwoundState->m_Rsp;

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = pUnwoundState->m_Capture.regname;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = &pRD->pCurrentContext->regname;
        ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

        ClearRegDisplayArgumentAndScratchRegisters(pRD);

        return;
    }
#endif // DACCESS_COMPILE

    pRD->pCurrentContext->Rip = pRD->ControlPC = m_MachState.m_Rip;
    pRD->pCurrentContext->Rsp = pRD->SP = m_MachState.m_Rsp;

#ifdef TARGET_UNIX

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = (m_MachState.m_Ptrs.p##regname != NULL) ? \
        *m_MachState.m_Ptrs.p##regname : m_MachState.m_Unwound.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#else // TARGET_UNIX

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = *m_MachState.m_Ptrs.p##regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#endif // TARGET_UNIX

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

    pRD->ControlPC = m_ctx.Rip;

    pRD->SP = m_ctx.Rsp;

    pRD->pCurrentContextPointers->Rax = &m_ctx.Rax;
    pRD->pCurrentContextPointers->Rcx = &m_ctx.Rcx;
    pRD->pCurrentContextPointers->Rdx = &m_ctx.Rdx;
    pRD->pCurrentContextPointers->Rbx = &m_ctx.Rbx;
    pRD->pCurrentContextPointers->Rbp = &m_ctx.Rbp;
    pRD->pCurrentContextPointers->Rsi = &m_ctx.Rsi;
    pRD->pCurrentContextPointers->Rdi = &m_ctx.Rdi;
    pRD->pCurrentContextPointers->R8  = &m_ctx.R8;
    pRD->pCurrentContextPointers->R9  = &m_ctx.R9;
    pRD->pCurrentContextPointers->R10 = &m_ctx.R10;
    pRD->pCurrentContextPointers->R11 = &m_ctx.R11;
    pRD->pCurrentContextPointers->R12 = &m_ctx.R12;
    pRD->pCurrentContextPointers->R13 = &m_ctx.R13;
    pRD->pCurrentContextPointers->R14 = &m_ctx.R14;
    pRD->pCurrentContextPointers->R15 = &m_ctx.R15;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Rip);
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

    pRD->ControlPC = m_Regs->Rip;

    pRD->SP = m_Regs->Rsp;

    pRD->pCurrentContextPointers->Rax = &m_Regs->Rax;
    pRD->pCurrentContextPointers->Rcx = &m_Regs->Rcx;
    pRD->pCurrentContextPointers->Rdx = &m_Regs->Rdx;
    pRD->pCurrentContextPointers->Rbx = &m_Regs->Rbx;
    pRD->pCurrentContextPointers->Rbp = &m_Regs->Rbp;
    pRD->pCurrentContextPointers->Rsi = &m_Regs->Rsi;
    pRD->pCurrentContextPointers->Rdi = &m_Regs->Rdi;
    pRD->pCurrentContextPointers->R8  = &m_Regs->R8;
    pRD->pCurrentContextPointers->R9  = &m_Regs->R9;
    pRD->pCurrentContextPointers->R10 = &m_Regs->R10;
    pRD->pCurrentContextPointers->R11 = &m_Regs->R11;
    pRD->pCurrentContextPointers->R12 = &m_Regs->R12;
    pRD->pCurrentContextPointers->R13 = &m_Regs->R13;
    pRD->pCurrentContextPointers->R14 = &m_Regs->R14;
    pRD->pCurrentContextPointers->R15 = &m_Regs->R15;

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

    pRD->pCurrentContext->Rip = m_ReturnAddress;
#ifdef TARGET_WINDOWS
    pRD->pCurrentContext->Rsp = m_Args->Rsp;
#else
    pRD->pCurrentContext->Rsp = PTR_TO_MEMBER_TADDR(HijackArgs, m_Args, Rip) + sizeof(void *);
#endif

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &(m_Args->Regs));

#ifdef UNIX_AMD64_ABI
    pRD->pCurrentContextPointers->Rsi = NULL;
    pRD->pCurrentContextPointers->Rdi = NULL;
#endif
    pRD->pCurrentContextPointers->Rcx = NULL;
#ifdef UNIX_AMD64_ABI
    pRD->pCurrentContextPointers->Rdx = (PULONG64)&m_Args->Rdx;
#else // UNIX_AMD64_ABI
    pRD->pCurrentContextPointers->Rdx = NULL;
#endif // UNIX_AMD64_ABI
    pRD->pCurrentContextPointers->R8  = NULL;
    pRD->pCurrentContextPointers->R9  = NULL;
    pRD->pCurrentContextPointers->R10 = NULL;
    pRD->pCurrentContextPointers->R11 = NULL;

    pRD->pCurrentContextPointers->Rax = (PULONG64)&m_Args->Rax;

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

BOOL isJumpRel32(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    PTR_BYTE pbCode = PTR_BYTE(pCode);

    return 0xE9 == pbCode[0];
}

//
//  Given the same pBuffer that was used by emitJump this
//  method decodes the instructions and returns the jump target
//
PCODE decodeJump32(PCODE pBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // jmp rel32
    _ASSERTE(isJumpRel32(pBuffer));

    return rel32Decode(pBuffer+1);
}

BOOL isJumpRel64(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    PTR_BYTE pbCode = PTR_BYTE(pCode);

    return 0x48 == pbCode[0]  &&
           0xB8 == pbCode[1]  &&
           0xFF == pbCode[10] &&
           0xE0 == pbCode[11];
}

PCODE decodeJump64(PCODE pBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // mov rax, xxx
    // jmp rax
    _ASSERTE(isJumpRel64(pBuffer));

    return *PTR_UINT64(pBuffer+2);
}

#ifdef DACCESS_COMPILE
BOOL GetAnyThunkTarget (CONTEXT *pctx, TADDR *pTarget, TADDR *pTargetMethodDesc)
{
    TADDR pThunk = GetIP(pctx);

    *pTargetMethodDesc = NULL;

    //
    // Check for something generated by emitJump.
    //
    if (isJumpRel64(pThunk))
    {
        *pTarget = decodeJump64(pThunk);
        return TRUE;
    }

    return FALSE;
}
#endif // DACCESS_COMPILE


#ifndef DACCESS_COMPILE

// Note: This is only used on server GC on Windows.
//
// This function returns the number of logical processors on a given physical chip.  If it cannot
// determine the number of logical cpus, or the machine is not populated uniformly with the same
// type of processors, this function returns 1.

void EncodeLoadAndJumpThunk (LPBYTE pBuffer, LPVOID pv, LPVOID pTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pBuffer));
    }
    CONTRACTL_END;

    // mov r10, pv                      49 ba xx xx xx xx xx xx xx xx

    pBuffer[0]  = 0x49;
    pBuffer[1]  = 0xBA;

    *((UINT64 UNALIGNED *)&pBuffer[2])  = (UINT64)pv;

    // mov rax, pTarget                 48 b8 xx xx xx xx xx xx xx xx

    pBuffer[10] = 0x48;
    pBuffer[11] = 0xB8;

    *((UINT64 UNALIGNED *)&pBuffer[12]) = (UINT64)pTarget;

    // jmp rax                          ff e0

    pBuffer[20] = 0xFF;
    pBuffer[21] = 0xE0;

    _ASSERTE(DbgIsExecutable(pBuffer, 22));
}

void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    BYTE *pBufferRX = (BYTE*)pCOMMethodRX - COMMETHOD_CALL_PRESTUB_SIZE;
    BYTE *pBufferRW = (BYTE*)pCOMMethodRW - COMMETHOD_CALL_PRESTUB_SIZE;

    // We need the target to be in a 64-bit aligned memory location and the call instruction
    // to immediately precede the ComCallMethodDesc. We'll generate an indirect call to avoid
    // consuming 3 qwords for this (mov rax, | target | nops & call rax).

    // dq 123456789abcdef0h
    // nop                              90
    // nop                              90
    // call [$ - 10]                    ff 15 f0 ff ff ff

    *((UINT64 *)&pBufferRW[COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET]) = (UINT64)target;

    pBufferRW[-2]  = 0x90;
    pBufferRW[-1]  = 0x90;

    pBufferRW[0] = 0xFF;
    pBufferRW[1] = 0x15;
    *((UINT32 UNALIGNED *)&pBufferRW[2]) = (UINT32)(COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET - COMMETHOD_CALL_PRESTUB_SIZE);

    _ASSERTE(DbgIsExecutable(pBufferRX, COMMETHOD_CALL_PRESTUB_SIZE));

    RETURN;
}

void emitJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pBufferRX));
    }
    CONTRACTL_END;

    // mov rax, 123456789abcdef0h       48 b8 xx xx xx xx xx xx xx xx
    // jmp rax                          ff e0

    pBufferRW[0]  = 0x48;
    pBufferRW[1]  = 0xB8;

    *((UINT64 UNALIGNED *)&pBufferRW[2]) = (UINT64)target;

    pBufferRW[10] = 0xFF;
    pBufferRW[11] = 0xE0;

    _ASSERTE(DbgIsExecutable(pBufferRX, 12));
}

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // padding                  // CC CC CC CC
    // mov r10, pUMEntryThunk   // 49 ba xx xx xx xx xx xx xx xx    // METHODDESC_REGISTER
    // mov rax, pJmpDest        // 48 b8 xx xx xx xx xx xx xx xx    // need to ensure this imm64 is qword aligned
    // TAILJMP_RAX              // 48 FF E0

#ifdef _DEBUG
    m_padding[0] = X86_INSTR_INT3;
    m_padding[1] = X86_INSTR_INT3;
    m_padding[2] = X86_INSTR_INT3;
    m_padding[3] = X86_INSTR_INT3;
#endif // _DEBUG
    m_movR10[0]  = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT | REX_OPCODE_REG_EXT;
    m_movR10[1]  = 0xBA;
    m_uet        = pvSecretParam;
    m_movRAX[0]  = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
    m_movRAX[1]  = 0xB8;
    m_execstub   = pTargetCode;
    m_jmpRAX[0]  = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
    m_jmpRAX[1]  = 0xFF;
    m_jmpRAX[2]  = 0xE0;

    _ASSERTE(DbgIsExecutable(&pEntryThunkCodeRX->m_movR10[0], &pEntryThunkCodeRX->m_jmpRAX[3]-&pEntryThunkCodeRX->m_movR10[0]));
    FlushInstructionCache(GetCurrentProcess(),pEntryThunkCodeRX,sizeof(UMEntryThunkCode));
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

    pThisRW->m_execstub    = (BYTE *)UMEntryThunk::ReportViolation;

    pThisRW->m_movR10[0]  = REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT;
#ifdef _WIN32
    // mov rcx, pUMEntryThunk // 48 b9 xx xx xx xx xx xx xx xx
    pThisRW->m_movR10[1]  = 0xB9;
#else
    // mov rdi, pUMEntryThunk // 48 bf xx xx xx xx xx xx xx xx
    pThisRW->m_movR10[1]  = 0xBF;
#endif

    ClrFlushInstructionCache(&m_movR10[0], &m_jmpRAX[3]-&m_movR10[0], /* hasCodeExecutedBefore */ true);
}

UMEntryThunk* UMEntryThunk::Decode(LPVOID pCallback)
{
    LIMITED_METHOD_CONTRACT;

    UMEntryThunkCode *pThunkCode = (UMEntryThunkCode*)((BYTE*)pCallback - UMEntryThunkCode::GetEntryPointOffset());

    return (UMEntryThunk*)pThunkCode->m_uet;
}

INT32 rel32UsingJumpStub(INT32 UNALIGNED * pRel32, PCODE target, MethodDesc *pMethod,
    LoaderAllocator *pLoaderAllocator /* = NULL */, bool throwOnOutOfMemoryWithinRange /*= true*/)
{
    CONTRACTL
    {
        THROWS;         // Creating a JumpStub could throw OutOfMemory
        GC_NOTRIGGER;

        PRECONDITION(pMethod != NULL || pLoaderAllocator != NULL);
        // If a loader allocator isn't explicitly provided, we must be able to get one via the MethodDesc.
        PRECONDITION(pLoaderAllocator != NULL || pMethod->GetLoaderAllocator() != NULL);
        // If a domain is provided, the MethodDesc mustn't yet be set up to have one, or it must match the MethodDesc's domain,
        // unless we're in a compilation domain (NGen loads assemblies as domain-bound but compiles them as domain neutral).
        PRECONDITION(!pLoaderAllocator || !pMethod || pMethod->GetMethodDescChunk()->GetMethodTable() == NULL ||
            pLoaderAllocator == pMethod->GetMethodDescChunk()->GetFirstMethodDesc()->GetLoaderAllocator());
    }
    CONTRACTL_END;

    TADDR baseAddr = (TADDR)pRel32 + 4;

    INT_PTR offset = target - baseAddr;

    if (!FitsInI4(offset) INDEBUG(|| PEDecoder::GetForceRelocs()))
    {
        TADDR loAddr = baseAddr + INT32_MIN;
        if (loAddr > baseAddr) loAddr = UINT64_MIN; // overflow

        TADDR hiAddr = baseAddr + INT32_MAX;
        if (hiAddr < baseAddr) hiAddr = UINT64_MAX; // overflow

        // Always try to allocate with throwOnOutOfMemoryWithinRange:false first to conserve reserveForJumpStubs until when
        // it is really needed. LoaderCodeHeap::CreateCodeHeap and EEJitManager::CanUseCodeHeap won't use the reserved
        // space when throwOnOutOfMemoryWithinRange is false.
        //
        // The reserved space should be only used by jump stubs for precodes and other similar code fragments. It should
        // not be used by JITed code. And since the accounting of the reserved space is not precise, we are conservative
        // and try to save the reserved space until it is really needed to avoid throwing out of memory within range exception.
        PCODE jumpStubAddr = ExecutionManager::jumpStub(pMethod,
                                                        target,
                                                        (BYTE *)loAddr,
                                                        (BYTE *)hiAddr,
                                                        pLoaderAllocator,
                                                        /* throwOnOutOfMemoryWithinRange */ false);
        if (jumpStubAddr == NULL)
        {
            if (!throwOnOutOfMemoryWithinRange)
                return 0;

            jumpStubAddr = ExecutionManager::jumpStub(pMethod,
                target,
                (BYTE *)loAddr,
                (BYTE *)hiAddr,
                pLoaderAllocator,
                /* throwOnOutOfMemoryWithinRange */ true);
        }

        offset = jumpStubAddr - baseAddr;

        if (!FitsInI4(offset))
        {
            _ASSERTE(!"jump stub was not in expected range");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
    }

    _ASSERTE(FitsInI4(offset));
    return static_cast<INT32>(offset);
}

INT32 rel32UsingPreallocatedJumpStub(INT32 UNALIGNED * pRel32, PCODE target, PCODE jumpStubAddrRX, PCODE jumpStubAddrRW, bool emitJump)
{
    CONTRACTL
    {
        THROWS; // emitBackToBackJump may throw (see emitJump)
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    TADDR baseAddr = (TADDR)pRel32 + 4;
    _ASSERTE(FitsInI4(jumpStubAddrRX - baseAddr));

    INT_PTR offset = target - baseAddr;
    if (!FitsInI4(offset) INDEBUG(|| PEDecoder::GetForceRelocs()))
    {
        offset = jumpStubAddrRX - baseAddr;
        if (!FitsInI4(offset))
        {
            _ASSERTE(!"jump stub was not in expected range");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }

        if (emitJump)
        {
            emitBackToBackJump((LPBYTE)jumpStubAddrRX, (LPBYTE)jumpStubAddrRW, (LPVOID)target);
        }
        else
        {
            _ASSERTE(decodeBackToBackJump(jumpStubAddrRX) == target);
        }
    }

    _ASSERTE(FitsInI4(offset));
    return static_cast<INT32>(offset);
}
//
// Some AMD64 assembly functions have one or more DWORDS at the end of the function
//  that specify the offsets where significant instructions are
//  we use this function to get at these offsets
//
DWORD GetOffsetAtEndOfFunction(ULONGLONG           uImageBase,
                               PT_RUNTIME_FUNCTION pFunctionEntry,
                               int                 offsetNum /* = 1*/)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION((offsetNum > 0) && (offsetNum < 20));  /* we only allow reasonable offsetNums 1..19 */
    }
    CONTRACTL_END;

    DWORD  functionSize     = pFunctionEntry->EndAddress - pFunctionEntry->BeginAddress;
    BYTE*  pEndOfFunction   = (BYTE*)  (uImageBase + pFunctionEntry->EndAddress);
    DWORD* pOffset          = (DWORD*) (pEndOfFunction)  - offsetNum;
    DWORD  offsetInFunc     = *pOffset;

    _ASSERTE_ALL_BUILDS((offsetInFunc >= 0) && (offsetInFunc < functionSize));

    return offsetInFunc;
}

#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStartRX = (BYTE *)(void*)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    ExecutableWriterHolder<BYTE> startWriterHolder(pStartRX, cbAligned); \
    BYTE * pStart = startWriterHolder.GetRW(); \
    size_t rxOffset = pStartRX - pStart; \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) *p++ = X86_INSTR_INT3; \
    ClrFlushInstructionCache(pStartRX, cbAligned); \
    return (PCODE)pStartRX

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(15);

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBF48; // mov rdi, XXXXXX
#else
    *(UINT16 *)p = 0xB948; // mov rcx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
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

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBE48; // mov rsi, XXXXXX
#else
    *(UINT16 *)p = 0xBA48; // mov rdx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
    p += 4;
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(15);

    EmitHelperWithArg(p, rxOffset, pAllocator, arg, target);

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(25);

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBF48; // mov rdi, XXXXXX
#else
    *(UINT16 *)p = 0xB948; // mov rcx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBE48; // mov rsi, XXXXXX
#else
    *(UINT16 *)p = 0xBA48; // mov rdx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg2;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(18);

#ifdef UNIX_AMD64_ABI
    *p++ = 0x48; // mov rsi, rdi
    *(UINT16 *)p = 0xF78B;
#else
    *p++ = 0x48; // mov rdx, rcx
    *(UINT16 *)p = 0xD18B;
#endif
    p += 2;

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBF48; // mov rdi, XXXXXX
#else
    *(UINT16 *)p = 0xB948; // mov rcx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
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
    BEGIN_DYNAMIC_HELPER_EMIT(11);

    *(UINT16 *)p = 0xB848; // mov rax, XXXXXX
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    BEGIN_DYNAMIC_HELPER_EMIT((offset != 0) ? 15 : 11);

    *(UINT16 *)p = 0xA148; // mov rax, [XXXXXX]
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    if (offset != 0)
    {
        // add rax, <offset>
        *p++ = 0x48;
        *p++ = 0x83;
        *p++ = 0xC0;
        *p++ = offset;
    }

    *p++ = 0xC3; // ret

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(15);

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBA48; // mov rdx, XXXXXX
#else
    *(UINT16 *)p = 0xB849; // mov r8, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(25);

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBA48; // mov rdx, XXXXXX
#else
    *(UINT16 *)p = 0xB849; // mov r8, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xB948; // mov rcx, XXXXXX
#else
    *(UINT16 *)p = 0xB949; // mov r9, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg2;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)(p + rxOffset), target, NULL, pAllocator);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    PCODE helperAddress = (pLookup->helper == CORINFO_HELP_RUNTIMEHANDLE_METHOD ?
        GetEEFuncEntryPoint(JIT_GenericHandleMethodWithSlotAndModule) :
        GetEEFuncEntryPoint(JIT_GenericHandleClassWithSlotAndModule));

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    ExecutableWriterHolder<GenericHandleArgs> argsWriterHolder(pArgs, sizeof(GenericHandleArgs));
    argsWriterHolder.GetRW()->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    argsWriterHolder.GetRW()->signature = pLookup->signature;
    argsWriterHolder.GetRW()->module = (CORINFO_MODULE_HANDLE)pModule;

    WORD slotOffset = (WORD)(dictionaryIndexAndSlot & 0xFFFF) * sizeof(Dictionary*);

    // It's available only via the run-time helper function
    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(15);

        // rcx/rdi contains the generic context parameter
        // mov rdx/rsi,pArgs
        // jmp helperAddress
        EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int indirectionsSize = 0;
        for (WORD i = 0; i < pLookup->indirections; i++)
            indirectionsSize += (pLookup->offsets[i] >= 0x80 ? 7 : 4);

        int codeSize = indirectionsSize + (pLookup->testForNull ? 21 : 1) + (pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK ? 13 : 0);

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        BYTE* pJLECall = NULL;

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                _ASSERTE(pLookup->testForNull && i > 0);

                // cmp qword ptr[rax + sizeOffset],slotOffset
                *(UINT32*)p = 0x00b88148; p += 3;
                *(UINT32*)p = (UINT32)pLookup->sizeOffset; p += 4;
                *(UINT32*)p = (UINT32)slotOffset; p += 4;

                // jle 'HELPER CALL'
                *p++ = 0x7e;
                pJLECall = p++;     // Offset filled later
            }

            if (i == 0)
            {
                // Move from rcx|rdi if it's the first indirection, otherwise from rax
#ifdef UNIX_AMD64_ABI
                // mov rax,qword ptr [rdi+offset]
                if (pLookup->offsets[i] >= 0x80)
                {
                    *(UINT32*)p = 0x00878b48; p += 3;
                    *(UINT32*)p = (UINT32)pLookup->offsets[i]; p += 4;
                }
                else
                {
                    *(UINT32*)p = 0x00478b48; p += 3;
                    *p++ = (BYTE)pLookup->offsets[i];
                }
#else
                // mov rax,qword ptr [rcx+offset]
                if (pLookup->offsets[i] >= 0x80)
                {
                    *(UINT32*)p = 0x00818b48; p += 3;
                    *(UINT32*)p = (UINT32)pLookup->offsets[i]; p += 4;
                }
                else
                {
                    *(UINT32*)p = 0x00418b48; p += 3;
                    *p++ = (BYTE)pLookup->offsets[i];
                }
#endif
            }
            else
            {
                // mov rax,qword ptr [rax+offset]
                if (pLookup->offsets[i] >= 0x80)
                {
                    *(UINT32*)p = 0x00808b48; p += 3;
                    *(UINT32*)p = (UINT32)pLookup->offsets[i]; p += 4;
                }
                else
                {
                    *(UINT32*)p = 0x00408b48; p += 3;
                    *p++ = (BYTE)pLookup->offsets[i];
                }
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
            // rcx/rdi contains the value of the dictionary slot entry

            _ASSERTE(pLookup->indirections != 0);

            *(UINT32*)p = 0x00c08548; p += 3;       // test rax,rax

            // je 'HELPER_CALL' (a jump of 1 byte)
            *(UINT16*)p = 0x0174; p += 2;

            *p++ = 0xC3;    // ret

            // 'HELPER_CALL'
            {
                if (pJLECall != NULL)
                    *pJLECall = (BYTE)(p - pJLECall - 1);

                // rcx|rdi already contains the generic context parameter

                // mov rdx|rsi,pArgs
                // jmp helperAddress
                EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);
            }
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}

#endif // FEATURE_READYTORUN

#endif // DACCESS_COMPILE
