//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    LIMITED_METHOD_CONTRACT;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Rip = GetReturnAddress();
    pRD->pCurrentContext->Rsp = GetSP();

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, GetCalleeSavedRegisters());
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

#ifndef DACCESS_COMPILE

extern "C" TADDR s_pStubHelperFrameVPtr;
TADDR s_pStubHelperFrameVPtr = StubHelperFrame::GetMethodFrameVPtr();

void TailCallFrame::InitFromContext(T_CONTEXT * pContext)
{
    WRAPPER_NO_CONTRACT;

#define CALLEE_SAVED_REGISTER(regname) m_calleeSavedRegisters.regname = pContext->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    m_pGCLayout = 0;
    m_ReturnAddress = pContext->Rip;
}

#endif // !DACCESS_COMPILE

void TailCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    LIMITED_METHOD_CONTRACT;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->Rip = m_ReturnAddress;
    pRD->pCurrentContext->Rsp = dac_cast<TADDR>(this) + sizeof(*this);

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &m_calleeSavedRegisters);
    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

        return;
    }
#endif // DACCESS_COMPILE

    pRD->pCurrentContext->Rip = pRD->ControlPC = m_MachState.m_Rip;
    pRD->pCurrentContext->Rsp = pRD->SP = m_MachState.m_Rsp;

#ifdef FEATURE_PAL

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = (m_MachState.m_Ptrs.p##regname != NULL) ? \
        *m_MachState.m_Ptrs.p##regname : m_MachState.m_Unwound.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#else // FEATURE_PAL

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContext->regname = *m_MachState.m_Ptrs.p##regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#endif // FEATURE_PAL

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

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

#if defined(FEATURE_HIJACK) || defined(FEATURE_UNIX_GC_REDIRECT_HIJACK)
TADDR ResumableFrame::GetReturnAddressPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(CONTEXT, Rip);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

// The HijackFrame has to know the registers that are pushed by OnHijackObjectTripThread
// and OnHijackScalarTripThread, so all three are implemented together.
void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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
    pRD->pCurrentContext->Rsp = PTR_TO_MEMBER_TADDR(HijackArgs, m_Args, Rip) + sizeof(void *);

    UpdateRegDisplayFromCalleeSavedRegisters(pRD, &(m_Args->Regs));

    pRD->pCurrentContextPointers->Rcx = NULL;
    pRD->pCurrentContextPointers->Rdx = NULL;
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
#endif // FEATURE_HIJACK || FEATURE_UNIX_GC_REDIRECT_HIJACK

BOOL isJumpRel32(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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

extern "C" DWORD __stdcall getcpuid(DWORD arg, unsigned char result[16]);

// fix this if/when AMD does multicore or SMT
DWORD GetLogicalCpuCount()
{
    // No CONTRACT possible because GetLogicalCpuCount uses SEH

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;

    static DWORD val = 0;

    // cache value for later re-use
    if (val)
    {
        return val;
    }   

    struct Param : DefaultCatchFilterParam
    {
        DWORD retVal;
    } param;
    param.pv = COMPLUS_EXCEPTION_EXECUTE_HANDLER;
    param.retVal = 1;    

    PAL_TRY(Param *, pParam, &param)
    {    

        unsigned char buffer[16];
        DWORD maxCpuId = getcpuid(0, buffer);
        DWORD* dwBuffer = (DWORD*)buffer;

        if (maxCpuId < 1)
            goto qExit;

        if (dwBuffer[1] == 'uneG') {
            if (dwBuffer[3] == 'Ieni') {
                if (dwBuffer[2] == 'letn')  {        // get SMT/multicore enumeration for Intel EM64T 

                   
                    // TODO: Currently GetLogicalCpuCountFromOS() and GetLogicalCpuCountFallback() are broken on 
                    // multi-core processor, but we never call into those two functions since we don't halve the
                    // gen0size when it's prescott and above processor. We keep the old version here for earlier
                    // generation system(Northwood based), perf data suggests on those systems, halve gen0 size 
                    // still boost the performance(ex:Biztalk boosts about 17%). So on earlier systems(Northwood) 
                    // based, we still go ahead and halve gen0 size.  The logic in GetLogicalCpuCountFromOS() 
                    // and GetLogicalCpuCountFallback() works fine for those earlier generation systems. 
                    // If it's a Prescott and above processor or Multi-core, perf data suggests not to halve gen0 
                    // size at all gives us overall better performance. 
                    // This is going to be fixed with a new version in orcas time frame. 

                    if( (maxCpuId > 3) && (maxCpuId < 0x80000000) )   
                        goto qExit;

                    val = GetLogicalCpuCountFromOS(); //try to obtain HT enumeration from OS API
                    if (val )
                    {
                        pParam->retVal = val;     // OS API HT enumeration successful, we are Done
                        goto qExit;
                    }

                    val = GetLogicalCpuCountFallback();    // Fallback to HT enumeration using CPUID
                    if( val )
                        pParam->retVal = val;
                }
            }
        }
qExit: ;
    }

    PAL_EXCEPT_FILTER(DefaultCatchFilter)
    {
    }
    PAL_ENDTRY

    if (val == 0)
    {
        val = param.retVal;  
    }

    return param.retVal;
}

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

void emitCOMStubCall (ComCallMethodDesc *pCOMMethod, PCODE target)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    BYTE *pBuffer = (BYTE*)pCOMMethod - COMMETHOD_CALL_PRESTUB_SIZE;

    // We need the target to be in a 64-bit aligned memory location and the call instruction
    // to immediately precede the ComCallMethodDesc. We'll generate an indirect call to avoid
    // consuming 3 qwords for this (mov rax, | target | nops & call rax).

    // dq 123456789abcdef0h
    // nop                              90
    // nop                              90
    // call [$ - 10]                    ff 15 f0 ff ff ff

    *((UINT64 *)&pBuffer[COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET]) = (UINT64)target;

    pBuffer[-2]  = 0x90;
    pBuffer[-1]  = 0x90;

    pBuffer[0] = 0xFF;
    pBuffer[1] = 0x15;
    *((UINT32 UNALIGNED *)&pBuffer[2]) = (UINT32)(COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET - COMMETHOD_CALL_PRESTUB_SIZE);

    _ASSERTE(DbgIsExecutable(pBuffer, COMMETHOD_CALL_PRESTUB_SIZE));

    RETURN;
}

void emitJump(LPBYTE pBuffer, LPVOID target)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pBuffer));
    }
    CONTRACTL_END;

    // mov rax, 123456789abcdef0h       48 b8 xx xx xx xx xx xx xx xx
    // jmp rax                          ff e0

    pBuffer[0]  = 0x48;
    pBuffer[1]  = 0xB8;

    *((UINT64 UNALIGNED *)&pBuffer[2]) = (UINT64)target;

    pBuffer[10] = 0xFF;
    pBuffer[11] = 0xE0;

    _ASSERTE(DbgIsExecutable(pBuffer, 12));
}

void UMEntryThunkCode::Encode(BYTE* pTargetCode, void* pvSecretParam)
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

    _ASSERTE(DbgIsExecutable(&m_movR10[0], &m_jmpRAX[3]-&m_movR10[0]));
}

UMEntryThunk* UMEntryThunk::Decode(LPVOID pCallback)
{
    LIMITED_METHOD_CONTRACT;

    UMEntryThunkCode *pThunkCode = (UMEntryThunkCode*)((BYTE*)pCallback - UMEntryThunkCode::GetEntryPointOffset());

    return (UMEntryThunk*)pThunkCode->m_uet;
}

INT32 rel32UsingJumpStub(INT32 UNALIGNED * pRel32, PCODE target, MethodDesc *pMethod, LoaderAllocator *pLoaderAllocator /* = NULL */)
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
        PRECONDITION(!pLoaderAllocator || !pMethod || pMethod->GetMethodDescChunk()->GetMethodTablePtr()->IsNull() || 
            pLoaderAllocator == pMethod->GetMethodDescChunk()->GetFirstMethodDesc()->GetLoaderAllocatorForCode() || IsCompilationProcess());
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

        PCODE jumpStubAddr = ExecutionManager::jumpStub(pMethod,
                                                        target,
                                                        (BYTE *)loAddr,
                                                        (BYTE *)hiAddr,
                                                        pLoaderAllocator);

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

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(pCode != GetPreStubEntryPoint());
    } CONTRACTL_END;

    // AMD64 has the following possible sequences for prestub logic:
    // 1. slot -> temporary entrypoint -> prestub
    // 2. slot -> precode -> prestub
    // 3. slot -> precode -> jumprel64 (jump stub) -> prestub
    // 4. slot -> precode -> jumprel64 (NGEN case) -> prestub

#ifdef HAS_COMPACT_ENTRYPOINTS
    if (MethodDescChunk::GetMethodDescFromCompactEntryPoint(pCode, TRUE) != NULL)
    {
        return TRUE;
    }
#endif

    if (!IS_ALIGNED(pCode, PRECODE_ALIGNMENT))
    {
        return FALSE;
    }

#ifdef HAS_FIXUP_PRECODE
    if (*PTR_BYTE(pCode) == X86_INSTR_CALL_REL32)
    {
        // Note that call could have been patched to jmp in the meantime
        pCode = rel32Decode(pCode+1);

#ifdef FEATURE_PREJIT
        // NGEN helper
        if (*PTR_BYTE(pCode) == X86_INSTR_JMP_REL32) {
            pCode = (TADDR)rel32Decode(pCode+1);
        }
#endif

        // JumpStub
        if (isJumpRel64(pCode)) {
            pCode = decodeJump64(pCode);
        }

        return pCode == (TADDR)PrecodeFixupThunk;
    }
#endif

    if (*PTR_USHORT(pCode) != X86_INSTR_MOV_R10_IMM64 || // mov rax,XXXX
        *PTR_BYTE(pCode+10) != X86_INSTR_NOP || // nop
        *PTR_BYTE(pCode+11) != X86_INSTR_JMP_REL32) // jmp rel32
    {
        return FALSE;
    }
    pCode = rel32Decode(pCode+12);

#ifdef FEATURE_PREJIT
    // NGEN helper
    if (*PTR_BYTE(pCode) == X86_INSTR_JMP_REL32) {
        pCode = (TADDR)rel32Decode(pCode+1);
    }
#endif

    // JumpStub
    if (isJumpRel64(pCode)) {
        pCode = decodeJump64(pCode);
    }

    return pCode == GetPreStubEntryPoint();
}

//
// Some AMD64 assembly functions have one or more DWORDS at the end of the function
//  that specify the offsets where significant instructions are
//  we use this function to get at these offsets
//
DWORD GetOffsetAtEndOfFunction(ULONGLONG           uImageBase,
                               PRUNTIME_FUNCTION   pFunctionEntry,
                               int                 offsetNum /* = 1*/)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION((offsetNum > 0) && (offsetNum < 20));  /* we only allow reasonable offsetNums 1..19 */
    }
    CONTRACTL_END;

    DWORD  functionSize     = pFunctionEntry->EndAddress - pFunctionEntry->BeginAddress;
    BYTE*  pEndOfFunction   = (BYTE*)  (uImageBase + pFunctionEntry->EndAddress);
    DWORD* pOffset          = (DWORD*) (pEndOfFunction)  - offsetNum;
    DWORD  offsetInFunc     = *pOffset;

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/cGenAMD64.cpp", (offsetInFunc >= 0) && (offsetInFunc < functionSize));

    return offsetInFunc;
}

//==========================================================================================
// In NGen image, virtual slots inherited from cross-module dependencies point to jump thunks.
// These jump thunk initially point to VirtualMethodFixupStub which transfers control here.
// This method 'VirtualMethodFixupWorker' will patch the jump thunk to point to the actual
// inherited method body after we have execute the precode and a stable entry point.
//
EXTERN_C PCODE VirtualMethodFixupWorker(TransitionBlock * pTransitionBlock, CORCOMPILE_VIRTUAL_IMPORT_THUNK * pThunk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;    // GC not allowed until we call pEMFrame->SetFunction(pMD);  

        ENTRY_POINT;
    }
    CONTRACTL_END;

    MAKE_CURRENT_THREAD_AVAILABLE();

    PCODE         pCode   = NULL;
    MethodDesc *  pMD     = NULL;   

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    BEGIN_SO_INTOLERANT_CODE(CURRENT_THREAD);

    _ASSERTE(IS_ALIGNED((size_t)pThunk, sizeof(INT64)));

    FrameWithCookie<ExternalMethodFrame> frame(pTransitionBlock);
    ExternalMethodFrame * pEMFrame = &frame;

    OBJECTREF pThisPtr = pEMFrame->GetThis();
    _ASSERTE(pThisPtr != NULL);
    VALIDATEOBJECT(pThisPtr);

    MethodTable * pMT = pThisPtr->GetTrueMethodTable();

    WORD slotNumber = pThunk->slotNum;
    _ASSERTE(slotNumber != (WORD)-1);

    pCode = pMT->GetRestoredSlot(slotNumber);

    if (!DoesSlotCallPrestub(pCode))
    {
        pMD = MethodTable::GetMethodDescForSlotAddress(pCode);

        pEMFrame->SetFunction(pMD);   //  We will use the pMD to enumerate the GC refs in the arguments 
        pEMFrame->Push(CURRENT_THREAD);

        INSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE;

        // Skip fixup precode jump for better perf
        PCODE pDirectTarget = Precode::TryToSkipFixupPrecode(pCode);
        if (pDirectTarget != NULL)
            pCode = pDirectTarget;

        INT64 oldValue = *(INT64*)pThunk;
        BYTE* pOldValue = (BYTE*)&oldValue;

        if (pOldValue[0] == X86_INSTR_CALL_REL32)
        {
            INT64 newValue = oldValue;
            BYTE* pNewValue = (BYTE*)&newValue;
            pNewValue[0] = X86_INSTR_JMP_REL32;

            *(INT32 *)(pNewValue+1) = rel32UsingJumpStub((INT32*)(&pThunk->callJmp[1]), pCode, pMD, NULL);

            _ASSERTE(IS_ALIGNED(pThunk, sizeof(INT64)));
            EnsureWritableExecutablePages(pThunk, sizeof(INT64));
            FastInterlockCompareExchangeLong((INT64*)pThunk, newValue, oldValue);

            FlushInstructionCache(GetCurrentProcess(), pThunk, 8);
        }
        
        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE;
        pEMFrame->Pop(CURRENT_THREAD);
    }

    // Ready to return

    END_SO_INTOLERANT_CODE;
   
    return pCode;
}

#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStart = (BYTE *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) *p++ = X86_INSTR_INT3; \
    ClrFlushInstructionCache(pStart, cbAligned); \
    return (PCODE)pStart

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
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    BEGIN_DYNAMIC_HELPER_EMIT(15);

#ifdef UNIX_AMD64_ABI
    *(UINT16 *)p = 0xBE48; // mov rsi, XXXXXX
#else
    *(UINT16 *)p = 0xBA48; // mov rdx, XXXXXX
#endif
    p += 2;
    *(TADDR *)p = arg;
    p += 8;

    *p++ = X86_INSTR_JMP_REL32; // jmp rel32
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
    p += 4;

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
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
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
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
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
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
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
    *(INT32 *)p = rel32UsingJumpStub((INT32 *)p, target, NULL, pAllocator);
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

#endif // FEATURE_READYTORUN

#endif // DACCESS_COMPILE
