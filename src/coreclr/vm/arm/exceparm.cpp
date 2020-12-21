// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ExcepArm.cpp

#include "common.h"
#include "asmconstants.h"
#include "virtualcallstub.h"

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_DISPATCHER_CONTEXT * pDispatcherContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    UINT_PTR stackSlot = pDispatcherContext->EstablisherFrame + REDIRECTSTUB_SP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_CONTEXT * pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    UINT_PTR stackSlot = pContext->Sp + REDIRECTSTUB_SP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

#if !defined(DACCESS_COMPILE)

// The next two functions help retrieve data kept relative to FaultingExceptionFrame that is setup
// for handling async exceptions (e.g. AV, NullRef, ThreadAbort, etc).
//
// FEF (and related data) is available relative to R4 - the thing to be kept in mind is that the
// DispatcherContext->ContextRecord:
//
// 1) represents the caller context in the first pass.
// 2) represents the current context in the second pass.
//
// Since R4 is a non-volatile register, this works for us since we setup the value of R4
// in the redirection helpers (e.g. NakedThrowHelper or RedirectForThreadAbort) but do not
// change it in their respective callee functions (e.g. NakedThrowHelper2 or RedirectForThreadAbort2)
// that have the personality routines associated with them (which perform the collided unwind and also
// invoke the two functions below).
//
// Thus, when our personality routine gets called in either passes, DC->ContextRecord->R4 will
// have the same value.

// Returns the pointer to the FEF
FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame (T_DISPATCHER_CONTEXT *pDispatcherContext)
{
    LIMITED_METHOD_CONTRACT;

    return (FaultingExceptionFrame*)((TADDR)pDispatcherContext->ContextRecord->R4);
}

// Returns TRUE if caller should resume execution.
BOOL
AdjustContextForVirtualStub(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThread();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    PCODE f_IP = GetIP(pContext);
    TADDR pInstr = PCODEToPINSTR(f_IP);

    VirtualCallStubManager::StubKind sk;
    VirtualCallStubManager::FindStubManager(f_IP, &sk);

    if (sk == VirtualCallStubManager::SK_DISPATCH)
    {
        if (*PTR_WORD(pInstr) != DISPATCH_STUB_FIRST_WORD)
        {
            _ASSERTE(!"AV in DispatchStub at unknown instruction");
            return FALSE;
        }
    }
    else
    if (sk == VirtualCallStubManager::SK_RESOLVE)
    {
        if (*PTR_WORD(pInstr) != RESOLVE_STUB_FIRST_WORD)
        {
            _ASSERTE(!"AV in ResolveStub at unknown instruction");
            return FALSE;
        }
    }
    else
    {
        return FALSE;
    }

    PCODE callsite = GetAdjustedCallAddress(GetLR(pContext));

    // Lr must already have been saved before calling so it should not be necessary to restore Lr
    if (pExceptionRecord != NULL)
    {
        pExceptionRecord->ExceptionAddress = (PVOID)callsite;
    }
    SetIP(pContext, callsite);

    return TRUE;
}
#endif // !DACCESS_COMPILE

