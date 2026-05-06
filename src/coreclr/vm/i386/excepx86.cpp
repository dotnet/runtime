// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*  EXCEP.CPP:
 *
 */
#include "common.h"

#include "frames.h"
#include "excep.h"
#include "object.h"
#include "field.h"
#include "dbginterface.h"
#include "cgensys.h"
#include "comutilnative.h"
#include "sigformat.h"
#include "siginfo.hpp"
#include "gcheaputilities.h"
#include "eedbginterfaceimpl.h" //so we can clearexception in COMPlusThrow
#include "eventtrace.h"
#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"
#include "dllimportcallback.h"
#include "threads.h"
#include "eeconfig.h"
#include "vars.hpp"
#include "generics.h"
#include "corinfo.h"

#include "asmconstants.h"
#include "virtualcallstub.h"

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    UINT_PTR stackSlot = pContext->Ebp + REDIRECTSTUB_EBP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

#ifndef DACCESS_COMPILE
LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
    return EXCEPTION_CONTINUE_SEARCH;
}

// Returns TRUE if caller should resume execution.
BOOL
AdjustContextForVirtualStub(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThreadNULLOk();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    PCODE f_IP = GetIP(pContext);

    StubCodeBlockKind sk;
    VirtualCallStubManager *pMgr = VirtualCallStubManager::FindStubManager(f_IP, &sk);

    if (sk == STUB_CODE_BLOCK_VSD_DISPATCH_STUB)
    {
        if (*PTR_WORD(f_IP) != X86_INSTR_CMP_IND_ECX_IMM32)
        {
            _ASSERTE(!"AV in DispatchStub at unknown instruction");
            return FALSE;
        }
    }
    else
    if (sk == STUB_CODE_BLOCK_VSD_RESOLVE_STUB)
    {
        if (*PTR_WORD(f_IP) != X86_INSTR_MOV_EAX_ECX_IND)
        {
            _ASSERTE(!"AV in ResolveStub at unknown instruction");
            return FALSE;
        }

        SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(GetSP(pContext)) + sizeof(void*))); // rollback push eax
    }
    else
    {
        return FALSE;
    }

    PCODE callsite = *dac_cast<PTR_PCODE>(GetSP(pContext));
    if (pExceptionRecord != NULL)
    {
        pExceptionRecord->ExceptionAddress = (PVOID)callsite;
    }

    SetIP(pContext, callsite);

#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)
    // Modify LastAVAddress saved in thread to distinguish between fake & real AV
    // See comments in IsGcMarker in file excep.cpp for more details
    pThread->SetLastAVAddress((LPVOID)GetIP(pContext));
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)

    // put ESP back to what it was before the call.
    TADDR sp = GetSP(pContext) + sizeof(void*);

#ifndef UNIX_X86_ABI
    // set the ESP to what it would be after the call (remove pushed arguments)

    size_t stackArgumentsSize;
    if (sk == STUB_CODE_BLOCK_VSD_DISPATCH_STUB)
    {
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        DispatchHolder *holder = DispatchHolder::FromDispatchEntry(f_IP);
        MethodTable *pMT = (MethodTable*)holder->stub()->expectedMT();
        DispatchToken token(VirtualCallStubManager::GetTokenFromStubQuick(pMgr, f_IP, sk));
        MethodDesc* pMD = VirtualCallStubManager::GetRepresentativeMethodDescFromToken(token, pMT);
        stackArgumentsSize = pMD->SizeOfArgStack();
    }
    else
    {
        // Compute the stub entry address from the address of failure (location of dereferencing of "this" pointer)
        ResolveHolder *holder = ResolveHolder::FromResolveEntry(f_IP - ResolveStub::offsetOfThisDeref());
        stackArgumentsSize = holder->stub()->stackArgumentsSize();
    }

    sp += stackArgumentsSize;
#endif // UNIX_X86_ABI

    SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(sp)));

    return TRUE;
}

#ifdef TARGET_WINDOWS

PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord()
{
    WRAPPER_NO_CONTRACT;

    return (PEXCEPTION_REGISTRATION_RECORD)__readfsdword(0);
}

VOID SetCurrentSEHRecord(EXCEPTION_REGISTRATION_RECORD *pSEH)
{
    WRAPPER_NO_CONTRACT;

    __writefsdword(0, (DWORD)pSEH);
}

VOID PopSEHRecords(LPVOID pTargetSP)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    PEXCEPTION_REGISTRATION_RECORD currentContext = GetCurrentSEHRecord();
    // The last record in the chain is EXCEPTION_CHAIN_END which is defined as maxiumum
    // pointer value so it cannot satisfy the loop condition.
    while (currentContext < pTargetSP)
    {
        currentContext = currentContext->Next;
    }
    SetCurrentSEHRecord(currentContext);
}

#ifdef FEATURE_HIJACK
void
CPFH_AdjustContextForThreadSuspensionRace(CONTEXT *pContext, Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    PCODE f_IP = GetIP(pContext);
    if (Thread::IsAddrOfRedirectFunc((PVOID)f_IP)) {

        // This is a very rare case where we tried to redirect a thread that was
        // just about to dispatch an exception, and our update of EIP took, but
        // the thread continued dispatching the exception.
        //
        // If this should happen (very rare) then we fix it up here.
        //
        _ASSERTE(pThread->GetSavedRedirectContext());
        SetIP(pContext, GetIP(pThread->GetSavedRedirectContext()));
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 1 setting IP = %x\n", pContext->Eip);
    }

    if (f_IP == GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION)) {

        // This is a very rare case where we tried to redirect a thread that was
        // just about to dispatch an exception, and our update of EIP took, but
        // the thread continued dispatching the exception.
        //
        // If this should happen (very rare) then we fix it up here.
        //
        SetIP(pContext, GetIP(pThread->m_OSContext));
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 2 setting IP = %x\n", pContext->Eip);
    }

// We have another even rarer race condition:
// - A) On thread A, Debugger puts an int 3 in the code stream at address X
// - A) We hit it and the begin an exception. The eip will be X + 1 (int3 is special)
// - B) Meanwhile, thread B redirects A's eip to Y. (Although A is really somewhere
// in the kernel, it looks like it's still in user code, so it can fall under the
// HandledJitCase and can be redirected)
// - A) The OS, trying to be nice, expects we have a breakpoint exception at X+1,
// but does -1 on the address since it knows int3 will leave the eip +1.
// So the context structure it will pass to the Handler is ideally (X+1)-1 = X
//
// ** Here's the race: Since thread B redirected A, the eip is actually Y (not X+1),
// but the kernel still touches it up to Y-1. So there's a window between when we hit a
// bp and when the handler gets called that this can happen.
// This causes an unhandled BP (since the debugger doesn't recognize the bp at Y-1)
//
// So what to do: If we land at Y-1 (ie, if f_IP+1 is the addr of a Redirected Func),
// then restore the EIP back to X. This will skip the redirection.
// Fortunately, this only occurs in cases where it's ok
// to skip. The debugger will recognize the patch and handle it.

    if (Thread::IsAddrOfRedirectFunc((PVOID)(f_IP + 1))) {
        _ASSERTE(pThread->GetSavedRedirectContext());
        SetIP(pContext, GetIP(pThread->GetSavedRedirectContext()) - 1);
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 3 setting IP = %x\n", pContext->Eip);
    }

    if (f_IP + 1 == GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION)) {
        SetIP(pContext, GetIP(pThread->m_OSContext) - 1);
        STRESS_LOG1(LF_EH, LL_INFO100, "CPFH_AdjustContextForThreadSuspensionRace: Case 4 setting IP = %x\n", pContext->Eip);
    }
}
#endif // FEATURE_HIJACK
#endif // TARGET_WINDOWS
#endif // !DACCESS_COMPILE
