// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#ifndef DACCESS_COMPILE
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhassert.h"
#include "slist.h"
#include "GcEnum.h"
#include "shash.h"
#include "TypeManager.h"
#include "varint.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "holder.h"
#include "Crst.h"
#include "RuntimeInstance.h"
#include "event.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "stressLog.h"
#include "rhbinder.h"
#include "MethodTable.h"
#include "MethodTable.inl"
#include "CommonMacros.inl"

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhpEHEnumInitFromStackFrameIterator, (
    StackFrameIterator* pFrameIter, void ** pMethodStartAddressOut, EHEnum* pEHEnum))
{
    ICodeManager * pCodeManager = pFrameIter->GetCodeManager();
    pEHEnum->m_pCodeManager = pCodeManager;

    FC_RETURN_BOOL(pCodeManager->EHEnumInit(pFrameIter->GetMethodInfo(), pMethodStartAddressOut, &pEHEnum->m_state));
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhpEHEnumNext, (EHEnum* pEHEnum, EHClause* pEHClause))
{
    FC_RETURN_BOOL(pEHEnum->m_pCodeManager->EHEnumNext(&pEHEnum->m_state, pEHClause));
}

// Unmanaged helper to locate one of two classlib-provided functions that the runtime needs to
// implement throwing of exceptions out of Rtm, and fail-fast. This may return NULL if the classlib
// found via the provided address does not have the necessary exports.
COOP_PINVOKE_HELPER(void *, RhpGetClasslibFunctionFromCodeAddress, (void * address, ClasslibFunctionId functionId))
{
    return GetRuntimeInstance()->GetClasslibFunctionFromCodeAddress(address, functionId);
}

// Unmanaged helper to locate one of two classlib-provided functions that the runtime needs to
// implement throwing of exceptions out of Rtm, and fail-fast. This may return NULL if the classlib
// found via the provided address does not have the necessary exports.
COOP_PINVOKE_HELPER(void *, RhpGetClasslibFunctionFromEEType, (MethodTable * pEEType, ClasslibFunctionId functionId))
{
    return pEEType->GetTypeManagerPtr()->AsTypeManager()->GetClasslibFunction(functionId);
}

COOP_PINVOKE_HELPER(void, RhpValidateExInfoStack, ())
{
    Thread * pThisThread = ThreadStore::GetCurrentThread();
    pThisThread->ValidateExInfoStack();
}

COOP_PINVOKE_HELPER(void, RhpClearThreadDoNotTriggerGC, ())
{
    Thread * pThisThread = ThreadStore::GetCurrentThread();

    if (!pThisThread->IsDoNotTriggerGcSet())
        RhFailFast();

    pThisThread->ClearDoNotTriggerGc();
}

COOP_PINVOKE_HELPER(void, RhpSetThreadDoNotTriggerGC, ())
{
    Thread * pThisThread = ThreadStore::GetCurrentThread();

    if (pThisThread->IsDoNotTriggerGcSet())
        RhFailFast();

    pThisThread->SetDoNotTriggerGc();
}

COOP_PINVOKE_HELPER(int32_t, RhGetModuleFileName, (HANDLE moduleHandle, _Out_ const TCHAR** pModuleNameOut))
{
    return PalGetModuleFileName(pModuleNameOut, moduleHandle);
}

COOP_PINVOKE_HELPER(void, RhpCopyContextFromExInfo, (void * pOSContext, int32_t cbOSContext, PAL_LIMITED_CONTEXT * pPalContext))
{
    ASSERT((size_t)cbOSContext >= sizeof(CONTEXT));
    CONTEXT* pContext = (CONTEXT *)pOSContext;

#ifndef HOST_WASM

    memset(pOSContext, 0, cbOSContext);
    pContext->ContextFlags = CONTEXT_CONTROL | CONTEXT_INTEGER;

    // Fill in CONTEXT_CONTROL registers that were not captured in PAL_LIMITED_CONTEXT.
    PopulateControlSegmentRegisters(pContext);

#endif // !HOST_WASM

#if defined(UNIX_AMD64_ABI)
    pContext->Rip = pPalContext->IP;
    pContext->Rsp = pPalContext->Rsp;
    pContext->Rbp = pPalContext->Rbp;
    pContext->Rdx = pPalContext->Rdx;
    pContext->Rax = pPalContext->Rax;
    pContext->Rbx = pPalContext->Rbx;
    pContext->R12 = pPalContext->R12;
    pContext->R13 = pPalContext->R13;
    pContext->R14 = pPalContext->R14;
    pContext->R15 = pPalContext->R15;
#elif defined(HOST_AMD64)
    pContext->Rip = pPalContext->IP;
    pContext->Rsp = pPalContext->Rsp;
    pContext->Rbp = pPalContext->Rbp;
    pContext->Rdi = pPalContext->Rdi;
    pContext->Rsi = pPalContext->Rsi;
    pContext->Rax = pPalContext->Rax;
    pContext->Rbx = pPalContext->Rbx;
    pContext->R12 = pPalContext->R12;
    pContext->R13 = pPalContext->R13;
    pContext->R14 = pPalContext->R14;
    pContext->R15 = pPalContext->R15;
#elif defined(HOST_X86)
    pContext->Eip = pPalContext->IP;
    pContext->Esp = pPalContext->Rsp;
    pContext->Ebp = pPalContext->Rbp;
    pContext->Edi = pPalContext->Rdi;
    pContext->Esi = pPalContext->Rsi;
    pContext->Eax = pPalContext->Rax;
    pContext->Ebx = pPalContext->Rbx;
#elif defined(HOST_ARM)
    pContext->R0  = pPalContext->R0;
    pContext->R4  = pPalContext->R4;
    pContext->R5  = pPalContext->R5;
    pContext->R6  = pPalContext->R6;
    pContext->R7  = pPalContext->R7;
    pContext->R8  = pPalContext->R8;
    pContext->R9  = pPalContext->R9;
    pContext->R10 = pPalContext->R10;
    pContext->R11 = pPalContext->R11;
    pContext->Sp  = pPalContext->SP;
    pContext->Lr  = pPalContext->LR;
    pContext->Pc  = pPalContext->IP;
#elif defined(HOST_ARM64)
    pContext->X0 = pPalContext->X0;
    pContext->X1 = pPalContext->X1;
    // TODO: Copy registers X2-X7 when we start supporting HVA's
    pContext->X19 = pPalContext->X19;
    pContext->X20 = pPalContext->X20;
    pContext->X21 = pPalContext->X21;
    pContext->X22 = pPalContext->X22;
    pContext->X23 = pPalContext->X23;
    pContext->X24 = pPalContext->X24;
    pContext->X25 = pPalContext->X25;
    pContext->X26 = pPalContext->X26;
    pContext->X27 = pPalContext->X27;
    pContext->X28 = pPalContext->X28;
    pContext->Fp = pPalContext->FP;
    pContext->Sp = pPalContext->SP;
    pContext->Lr = pPalContext->LR;
    pContext->Pc = pPalContext->IP;
#elif defined(HOST_WASM)
    // No registers, no work to do yet
#else
#error Not Implemented for this architecture -- RhpCopyContextFromExInfo
#endif
}

#if defined(HOST_AMD64) || defined(HOST_ARM) || defined(HOST_X86) || defined(HOST_ARM64)
struct DISPATCHER_CONTEXT
{
    uintptr_t  ControlPc;
    // N.B. There is more here (so this struct isn't the right size), but we ignore everything else
};

#ifdef HOST_X86
struct EXCEPTION_REGISTRATION_RECORD
{
    uintptr_t Next;
    uintptr_t Handler;
};
#endif // HOST_X86

EXTERN_C void __cdecl RhpFailFastForPInvokeExceptionPreemp(intptr_t PInvokeCallsiteReturnAddr,
                                                           void* pExceptionRecord, void* pContextRecord);
EXTERN_C void REDHAWK_CALLCONV RhpFailFastForPInvokeExceptionCoop(intptr_t PInvokeCallsiteReturnAddr,
                                                                  void* pExceptionRecord, void* pContextRecord);
int32_t __stdcall RhpVectoredExceptionHandler(PEXCEPTION_POINTERS pExPtrs);

EXTERN_C int32_t __stdcall RhpPInvokeExceptionGuard(PEXCEPTION_RECORD       pExceptionRecord,
                                                  uintptr_t              EstablisherFrame,
                                                  PCONTEXT                pContextRecord,
                                                  DISPATCHER_CONTEXT *    pDispatcherContext)
{
    UNREFERENCED_PARAMETER(EstablisherFrame);

    Thread * pThread = ThreadStore::GetCurrentThread();

    // A thread in DoNotTriggerGc mode has many restrictions that will become increasingly likely to be violated as
    // exception dispatch kicks off. So we just address this as early as possible with a FailFast.
    // The most likely case where this occurs is in GC-callouts -- in that case, we have
    // managed code that runs on behalf of GC, which might have a bug that causes an AV.
    if (pThread->IsDoNotTriggerGcSet())
        RhFailFast();

    // We promote exceptions that were not converted to managed exceptions to a FailFast.  However, we have to
    // be careful because we got here via OS SEH infrastructure and, therefore, don't know what GC mode we're
    // currently in.  As a result, since we're calling back into managed code to handle the FailFast, we must
    // correctly call either a UnmanagedCallersOnly or a RuntimeExport version of the same method.
    if (pThread->IsCurrentThreadInCooperativeMode())
    {
        // Cooperative mode -- Typically, RhpVectoredExceptionHandler will handle this because the faulting IP will be
        // in managed code.  But sometimes we AV on a bad call indirect or something similar.  In that situation, we can
        // use the dispatcher context or exception registration record to find the relevant classlib.
#ifdef HOST_X86
        intptr_t classlibBreadcrumb = ((EXCEPTION_REGISTRATION_RECORD*)EstablisherFrame)->Handler;
#else
        intptr_t classlibBreadcrumb = pDispatcherContext->ControlPc;
#endif
        RhpFailFastForPInvokeExceptionCoop(classlibBreadcrumb, pExceptionRecord, pContextRecord);
    }
    else
    {
        // Preemptive mode -- the classlib associated with the last pinvoke owns the fail fast behavior.
        intptr_t pinvokeCallsiteReturnAddr = (intptr_t)pThread->GetCurrentThreadPInvokeReturnAddress();
        RhpFailFastForPInvokeExceptionPreemp(pinvokeCallsiteReturnAddr, pExceptionRecord, pContextRecord);
    }

    return 0;
}
#else
EXTERN_C int32_t RhpPInvokeExceptionGuard()
{
    ASSERT_UNCONDITIONALLY("RhpPInvokeExceptionGuard NYI for this architecture!");
    RhFailFast();
    return 0;
}
#endif

#if defined(HOST_AMD64) || defined(HOST_ARM) || defined(HOST_X86) || defined(HOST_ARM64) || defined(HOST_WASM)
EXTERN_C NATIVEAOT_API void REDHAWK_CALLCONV RhpThrowHwEx();
#else
COOP_PINVOKE_HELPER(void, RhpThrowHwEx, ())
{
    ASSERT_UNCONDITIONALLY("RhpThrowHwEx NYI for this architecture!");
}
COOP_PINVOKE_HELPER(void, RhpThrowEx, ())
{
    ASSERT_UNCONDITIONALLY("RhpThrowEx NYI for this architecture!");
}
COOP_PINVOKE_HELPER(void, RhpCallCatchFunclet, ())
{
    ASSERT_UNCONDITIONALLY("RhpCallCatchFunclet NYI for this architecture!");
}
COOP_PINVOKE_HELPER(void, RhpCallFinallyFunclet, ())
{
    ASSERT_UNCONDITIONALLY("RhpCallFinallyFunclet NYI for this architecture!");
}
COOP_PINVOKE_HELPER(void, RhpCallFilterFunclet, ())
{
    ASSERT_UNCONDITIONALLY("RhpCallFilterFunclet NYI for this architecture!");
}
COOP_PINVOKE_HELPER(void, RhpRethrow, ())
{
    ASSERT_UNCONDITIONALLY("RhpRethrow NYI for this architecture!");
}

EXTERN_C void* RhpCallCatchFunclet2 = NULL;
EXTERN_C void* RhpCallFinallyFunclet2 = NULL;
EXTERN_C void* RhpCallFilterFunclet2 = NULL;
EXTERN_C void* RhpThrowEx2   = NULL;
EXTERN_C void* RhpThrowHwEx2 = NULL;
EXTERN_C void* RhpRethrow2   = NULL;
#endif

EXTERN_C void * RhpAssignRefAVLocation;
EXTERN_C void * RhpCheckedAssignRefAVLocation;
EXTERN_C void * RhpCheckedLockCmpXchgAVLocation;
EXTERN_C void * RhpCheckedXchgAVLocation;
#if !defined(HOST_AMD64) && !defined(HOST_ARM64)
#if !defined(HOST_X86) && !defined(HOST_ARM)
EXTERN_C void * RhpLockCmpXchg8AVLocation;
EXTERN_C void * RhpLockCmpXchg16AVLocation;
EXTERN_C void * RhpLockCmpXchg32AVLocation;
#endif
EXTERN_C void * RhpLockCmpXchg64AVLocation;
#endif
EXTERN_C void * RhpByRefAssignRefAVLocation1;

#if !defined(HOST_ARM64)
EXTERN_C void * RhpByRefAssignRefAVLocation2;
#endif

#if defined(HOST_ARM64) && !defined(LSE_INSTRUCTIONS_ENABLED_BY_DEFAULT)
EXTERN_C void* RhpCheckedLockCmpXchgAVLocation2;
EXTERN_C void* RhpCheckedXchgAVLocation2;
#endif

static bool InWriteBarrierHelper(uintptr_t faultingIP)
{
#ifndef USE_PORTABLE_HELPERS
    static uintptr_t writeBarrierAVLocations[] =
    {
        (uintptr_t)&RhpAssignRefAVLocation,
        (uintptr_t)&RhpCheckedAssignRefAVLocation,
        (uintptr_t)&RhpCheckedLockCmpXchgAVLocation,
        (uintptr_t)&RhpCheckedXchgAVLocation,
#if !defined(HOST_AMD64) && !defined(HOST_ARM64)
#if !defined(HOST_X86) && !defined(HOST_ARM)
        (uintptr_t)&RhpLockCmpXchg8AVLocation,
        (uintptr_t)&RhpLockCmpXchg16AVLocation,
        (uintptr_t)&RhpLockCmpXchg32AVLocation,
#endif
        (uintptr_t)&RhpLockCmpXchg64AVLocation,
#endif
        (uintptr_t)&RhpByRefAssignRefAVLocation1,
#if !defined(HOST_ARM64)
        (uintptr_t)&RhpByRefAssignRefAVLocation2,
#endif
#if defined(HOST_ARM64) && !defined(LSE_INSTRUCTIONS_ENABLED_BY_DEFAULT)
        (uintptr_t)&RhpCheckedLockCmpXchgAVLocation2,
        (uintptr_t)&RhpCheckedXchgAVLocation2,
#endif
    };

    // compare the IP against the list of known possible AV locations in the write barrier helpers
    for (size_t i = 0; i < sizeof(writeBarrierAVLocations)/sizeof(writeBarrierAVLocations[0]); i++)
    {
#if defined(HOST_AMD64) || defined(HOST_X86)
        // Verify that the runtime is not linked with incremental linking enabled. Incremental linking
        // wraps every method symbol with a jump stub that breaks the following check.
        ASSERT(*(uint8_t*)writeBarrierAVLocations[i] != 0xE9); // jmp XXXXXXXX
#endif

        if (PCODEToPINSTR(writeBarrierAVLocations[i]) == faultingIP)
            return true;
    }
#endif // USE_PORTABLE_HELPERS

    return false;
}

EXTERN_C void* RhpInitialInterfaceDispatch;
EXTERN_C void* RhpInterfaceDispatchAVLocation1;
EXTERN_C void* RhpInterfaceDispatchAVLocation2;
EXTERN_C void* RhpInterfaceDispatchAVLocation4;
EXTERN_C void* RhpInterfaceDispatchAVLocation8;
EXTERN_C void* RhpInterfaceDispatchAVLocation16;
EXTERN_C void* RhpInterfaceDispatchAVLocation32;
EXTERN_C void* RhpInterfaceDispatchAVLocation64;

static bool InInterfaceDispatchHelper(uintptr_t faultingIP)
{
#ifndef USE_PORTABLE_HELPERS
    static uintptr_t interfaceDispatchAVLocations[] =
    {
        (uintptr_t)&RhpInitialInterfaceDispatch,
        (uintptr_t)&RhpInterfaceDispatchAVLocation1,
        (uintptr_t)&RhpInterfaceDispatchAVLocation2,
        (uintptr_t)&RhpInterfaceDispatchAVLocation4,
        (uintptr_t)&RhpInterfaceDispatchAVLocation8,
        (uintptr_t)&RhpInterfaceDispatchAVLocation16,
        (uintptr_t)&RhpInterfaceDispatchAVLocation32,
        (uintptr_t)&RhpInterfaceDispatchAVLocation64,
    };

    // compare the IP against the list of known possible AV locations in the interface dispatch helpers
    for (size_t i = 0; i < sizeof(interfaceDispatchAVLocations) / sizeof(interfaceDispatchAVLocations[0]); i++)
    {
#if defined(HOST_AMD64) || defined(HOST_X86)
        // Verify that the runtime is not linked with incremental linking enabled. Incremental linking
        // wraps every method symbol with a jump stub that breaks the following check.
        ASSERT(*(uint8_t*)interfaceDispatchAVLocations[i] != 0xE9); // jmp XXXXXXXX
#endif

        if (PCODEToPINSTR(interfaceDispatchAVLocations[i]) == faultingIP)
            return true;
    }
#endif // USE_PORTABLE_HELPERS

    return false;
}

static uintptr_t UnwindSimpleHelperToCaller(
#ifdef TARGET_UNIX
    PAL_LIMITED_CONTEXT * pContext
#else
    _CONTEXT * pContext
#endif
    )
{
#if defined(_DEBUG)
    uintptr_t faultingIP = pContext->GetIp();
    ASSERT(InWriteBarrierHelper(faultingIP) || InInterfaceDispatchHelper(faultingIP));
#endif
#if defined(HOST_AMD64) || defined(HOST_X86)
    // simulate a ret instruction
    uintptr_t sp = pContext->GetSp();
    uintptr_t adjustedFaultingIP = *(uintptr_t *)sp;
    pContext->SetSp(sp+sizeof(uintptr_t)); // pop the stack
#elif defined(HOST_ARM) || defined(HOST_ARM64)
    uintptr_t adjustedFaultingIP = pContext->GetLr();
#else
    uintptr_t adjustedFaultingIP = 0; // initializing to make the compiler happy
    PORTABILITY_ASSERT("UnwindSimpleHelperToCaller");
#endif
    return adjustedFaultingIP;
}

#ifdef TARGET_UNIX

int32_t __stdcall RhpHardwareExceptionHandler(uintptr_t faultCode, uintptr_t faultAddress,
    PAL_LIMITED_CONTEXT* palContext, uintptr_t* arg0Reg, uintptr_t* arg1Reg)
{
    uintptr_t faultingIP = palContext->GetIp();

    ICodeManager * pCodeManager = GetRuntimeInstance()->GetCodeManagerForAddress((PTR_VOID)faultingIP);
    bool translateToManagedException = false;
    if (pCodeManager != NULL)
    {
        // Make sure that the OS does not use our internal fault codes
        ASSERT(faultCode != STATUS_REDHAWK_NULL_REFERENCE && faultCode != STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE);

        if (faultCode == STATUS_ACCESS_VIOLATION)
        {
            if (faultAddress < NULL_AREA_SIZE)
            {
                faultCode = STATUS_REDHAWK_NULL_REFERENCE;
            }
        }
        else if (faultCode == STATUS_STACK_OVERFLOW)
        {
            // Do not use ASSERT_UNCONDITIONALLY here. It will crash because of it consumes too much stack.

            PalPrintFatalError("\nProcess is terminating due to StackOverflowException.\n");
            RhFailFast();
        }

        translateToManagedException = true;
    }
    else if (faultCode == STATUS_ACCESS_VIOLATION)
    {
        // If this was an AV and code manager is null, this was an AV in unmanaged code.
        // Could still be an AV in one of our assembly helpers that we know how to handle.
        bool inWriteBarrierHelper = InWriteBarrierHelper(faultingIP);
        bool inInterfaceDispatchHelper = InInterfaceDispatchHelper(faultingIP);

        if (inWriteBarrierHelper || inInterfaceDispatchHelper)
        {
            if (faultAddress < NULL_AREA_SIZE)
            {
                faultCode = STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE;
            }

            // we were AV-ing in a helper - unwind our way to our caller
            faultingIP = UnwindSimpleHelperToCaller(palContext);

            translateToManagedException = true;
        }
    }

    if (translateToManagedException)
    {
        *arg0Reg = faultCode;
        *arg1Reg = faultingIP;
        palContext->SetIp(PCODEToPINSTR((PCODE)&RhpThrowHwEx));

        return EXCEPTION_CONTINUE_EXECUTION;
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

#else // TARGET_UNIX

static bool g_ContinueOnFatalErrors = false;

// Set the runtime to continue search when encountering an unhandled runtime exception. Once done it is forever.
// Continuing the search allows any vectored exception handlers or SEH installed by the client to take effect.
// Any client that does so is expected to handle stack overflows.
EXTERN_C void RhpContinueOnFatalErrors()
{
    g_ContinueOnFatalErrors = true;
}

int32_t __stdcall RhpVectoredExceptionHandler(PEXCEPTION_POINTERS pExPtrs)
{
    uintptr_t faultCode = pExPtrs->ExceptionRecord->ExceptionCode;

    // Do not interfere with debugger exceptions
    if (faultCode == STATUS_BREAKPOINT || faultCode == STATUS_SINGLE_STEP)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    uintptr_t faultingIP = pExPtrs->ContextRecord->GetIp();

    ICodeManager * pCodeManager = GetRuntimeInstance()->GetCodeManagerForAddress((PTR_VOID)faultingIP);
    bool translateToManagedException = false;
    if (pCodeManager != NULL)
    {
        // Make sure that the OS does not use our internal fault codes
        ASSERT(faultCode != STATUS_REDHAWK_NULL_REFERENCE && faultCode != STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE);

        if (faultCode == STATUS_ACCESS_VIOLATION)
        {
            if (pExPtrs->ExceptionRecord->ExceptionInformation[1] < NULL_AREA_SIZE)
            {
                faultCode = STATUS_REDHAWK_NULL_REFERENCE;
            }
        }
        else if (faultCode == STATUS_STACK_OVERFLOW)
        {
            if (g_ContinueOnFatalErrors)
            {
                // The client is responsible for the handling.
                return EXCEPTION_CONTINUE_SEARCH;
            }

            // Do not use ASSERT_UNCONDITIONALLY here. It will crash because of it consumes too much stack.
            PalPrintFatalError("\nProcess is terminating due to StackOverflowException.\n");
            PalRaiseFailFastException(pExPtrs->ExceptionRecord, pExPtrs->ContextRecord, 0);
        }

        translateToManagedException = true;
    }
    else if (faultCode == STATUS_ACCESS_VIOLATION)
    {
        // If this was an AV and code manager is null, this was an AV in unmanaged code.
        // Could still be an AV in one of our assembly helpers that we know how to handle.
        bool inWriteBarrierHelper = InWriteBarrierHelper(faultingIP);
        bool inInterfaceDispatchHelper = InInterfaceDispatchHelper(faultingIP);

        if (inWriteBarrierHelper || inInterfaceDispatchHelper)
        {
            if (pExPtrs->ExceptionRecord->ExceptionInformation[1] < NULL_AREA_SIZE)
            {
                faultCode = STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE;
            }

            // we were AV-ing in a helper - unwind our way to our caller
            faultingIP = UnwindSimpleHelperToCaller(pExPtrs->ContextRecord);

            translateToManagedException = true;
        }
    }

    if (translateToManagedException)
    {
        pExPtrs->ContextRecord->SetIp(PCODEToPINSTR((PCODE)&RhpThrowHwEx));
        pExPtrs->ContextRecord->SetArg0Reg(faultCode);
        pExPtrs->ContextRecord->SetArg1Reg(faultingIP);

        return EXCEPTION_CONTINUE_EXECUTION;
    }

    // The client may have told us to continue to search for custom handlers,
    // but in general we consider any form of hardware exception within the runtime itself a fatal error.
    // Note this includes the managed code within the runtime.
    if (!g_ContinueOnFatalErrors)
    {
        static uint8_t *s_pbRuntimeModuleLower = NULL;
        static uint8_t *s_pbRuntimeModuleUpper = NULL;

        // If this is the first time through this path then calculate the upper and lower bounds of the
        // runtime module. Note we could be racing to calculate this but it doesn't matter since the results
        // should always agree.
        if ((s_pbRuntimeModuleLower == NULL) || (s_pbRuntimeModuleUpper == NULL))
        {
            // Get the module handle for this runtime. Do this by passing an address definitely within the
            // module (the address of this function) to GetModuleHandleEx with the "from address" flag.
            HANDLE hRuntimeModule = PalGetModuleHandleFromPointer(reinterpret_cast<void*>(RhpVectoredExceptionHandler));
            if (!hRuntimeModule)
            {
                ASSERT_UNCONDITIONALLY("Failed to locate our own module handle");
                RhFailFast();
            }

            PalGetModuleBounds(hRuntimeModule, &s_pbRuntimeModuleLower, &s_pbRuntimeModuleUpper);
        }

        if (((uint8_t*)faultingIP >= s_pbRuntimeModuleLower) && ((uint8_t*)faultingIP < s_pbRuntimeModuleUpper))
        {
            ASSERT_UNCONDITIONALLY("Hardware exception raised inside the runtime.");
            PalRaiseFailFastException(pExPtrs->ExceptionRecord, pExPtrs->ContextRecord, 0);
        }
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

#endif // TARGET_UNIX

COOP_PINVOKE_HELPER(void, RhpFallbackFailFast, ())
{
    RhFailFast();
}

#endif // !DACCESS_COMPILE
