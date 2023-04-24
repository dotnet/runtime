// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: System.cpp
//

//
// Purpose: Native methods on System.Environment & Array
//

//

#include "common.h"

#include <object.h>

#include "ceeload.h"

#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "classnames.h"
#include "system.h"
#include "string.h"
#include "sstring.h"
#include "eeconfig.h"
#include "assemblynative.hpp"
#include "generics.h"
#include "invokeutil.h"
#include "array.h"
#include "eepolicy.h"




FCIMPL0(UINT32, SystemNative::GetTickCount)
{
    FCALL_CONTRACT;

    return ::GetTickCount();
}
FCIMPLEND;

FCIMPL0(UINT64, SystemNative::GetTickCount64)
{
    FCALL_CONTRACT;

    return ::GetTickCount64();
}
FCIMPLEND;




extern "C" VOID QCALLTYPE Environment_Exit(INT32 exitcode)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);

    ForceEEShutdown();

    END_QCALL;
}

FCIMPL1(VOID,SystemNative::SetExitCode,INT32 exitcode)
{
    FCALL_CONTRACT;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    SetLatchedExitCode(exitcode);
}
FCIMPLEND

FCIMPL0(INT32, SystemNative::GetExitCode)
{
    FCALL_CONTRACT;

    // Return whatever has been latched so far.  This is uninitialized to 0.
    return GetLatchedExitCode();
}
FCIMPLEND

// Return a method info for the method were the exception was thrown
FCIMPL1(ReflectMethodObject*, SystemNative::GetMethodFromStackTrace, ArrayBase* pStackTraceUNSAFE)
{
    FCALL_CONTRACT;

    I1ARRAYREF pArray(static_cast<I1Array *>(pStackTraceUNSAFE));
    StackTraceArray stackArray(pArray);

    if (!stackArray.Size())
        return NULL;

    // The managed stacktrace classes always returns typical method definition, so we don't need to bother providing exact instantiation.
    // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(pElements[0].pFunc, pElements[0].pExactGenericArgsToken, pTypeHandle, &pMD);

    MethodDesc* pFunc = stackArray[0].pFunc;

    // Strip the instantiation to make sure that the reflection never gets a bad method desc back.
    REFLECTMETHODREF refRet = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0()
    pFunc = pFunc->LoadTypicalMethodDefinition();
    refRet = pFunc->GetStubMethodInfo();
    _ASSERTE(pFunc->IsRuntimeMethodHandle());

    HELPER_METHOD_FRAME_END();

    return (ReflectMethodObject*)OBJECTREFToObject(refRet);
}
FCIMPLEND

extern "C" INT32 QCALLTYPE Environment_GetProcessorCount()
{
    QCALL_CONTRACT;

    INT32 processorCount = 0;

    BEGIN_QCALL;

    processorCount = GetCurrentProcessCpuCount();

    END_QCALL;

    return processorCount;
}

// FailFast is supported in BCL.small as internal to support failing fast in places where EEE used to be thrown.
//
// Static message buffer used by SystemNative::FailFast to avoid reliance on a
// managed string object buffer. This buffer is not always used, see comments in
// the method below.
WCHAR g_szFailFastBuffer[256];
WCHAR *g_pFailFastBuffer = g_szFailFastBuffer;

#define FAIL_FAST_STATIC_BUFFER_LENGTH (sizeof(g_szFailFastBuffer) / sizeof(WCHAR))

// This is the common code for FailFast processing that is wrapped by the two
// FailFast FCalls below.
void SystemNative::GenericFailFast(STRINGREF refMesgString, EXCEPTIONREF refExceptionForWatsonBucketing, UINT_PTR retAddress, UINT exitCode, STRINGREF refErrorSourceString)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }CONTRACTL_END;

    struct
    {
        STRINGREF refMesgString;
        EXCEPTIONREF refExceptionForWatsonBucketing;
        STRINGREF refErrorSourceString;
    } gc;
    gc.refMesgString = refMesgString;
    gc.refExceptionForWatsonBucketing = refExceptionForWatsonBucketing;
    gc.refErrorSourceString = refErrorSourceString;

    GCPROTECT_BEGIN(gc);

    // Managed code injected FailFast maps onto the unmanaged version
    // (EEPolicy::HandleFatalError) in the following manner: the exit code is
    // always set to COR_E_FAILFAST and the address passed (usually a failing
    // EIP) is in fact the address of a unicode message buffer (explaining the
    // reason for the fault).
    // The message string comes from a managed string object so we can't rely on
    // the buffer remaining in place below our feet. But equally we don't want
    // to inject failure points (by, for example, allocating a heap buffer or a
    // pinning handle) when we have a much higher chance than usual of actually
    // tripping those failure points and eradicating useful debugging info.
    // We employ various strategies to deal with this:
    //   o  If the message is small enough we copy it into a static buffer
    //      (g_szFailFastBuffer).
    //   o  Otherwise we try to allocate a buffer of the required size on the
    //      heap. This buffer will be leaked.
    //   o  If the allocation above fails we return to the static buffer and
    //      truncate the message.
    //
    // Another option would seem to be to implement a new frame type that
    // protects object references as pinned, but that seems like overkill for
    // just this problem.
    WCHAR  *pszMessageBuffer = NULL;
    DWORD   cchMessage = (gc.refMesgString == NULL) ? 0 : gc.refMesgString->GetStringLength();

    WCHAR * errorSourceString = NULL;

    if (gc.refErrorSourceString != NULL)
    {
        DWORD cchErrorSource = gc.refErrorSourceString->GetStringLength();
        errorSourceString = new (nothrow) WCHAR[cchErrorSource + 1];

        if (errorSourceString != NULL)
        {
            memcpyNoGCRefs(errorSourceString, gc.refErrorSourceString->GetBuffer(), cchErrorSource * sizeof(WCHAR));
            errorSourceString[cchErrorSource] = W('\0');
        }
    }

    if (cchMessage < FAIL_FAST_STATIC_BUFFER_LENGTH)
    {
        // The static buffer can be used only once to avoid race condition with other threads
        pszMessageBuffer = InterlockedExchangeT(&g_pFailFastBuffer, NULL);
    }

    if (pszMessageBuffer == NULL)
    {
        // We can fail here, but we can handle the fault.
        CONTRACT_VIOLATION(FaultViolation);
        pszMessageBuffer = new (nothrow) WCHAR[cchMessage + 1];
        if (pszMessageBuffer == NULL)
        {
            // Truncate the message to what will fit in the static buffer.
            cchMessage = FAIL_FAST_STATIC_BUFFER_LENGTH - 1;
            pszMessageBuffer = InterlockedExchangeT(&g_pFailFastBuffer, NULL);
        }
    }

    const WCHAR *pszMessage;
    if (pszMessageBuffer != NULL)
    {
        if (cchMessage > 0)
            memcpyNoGCRefs(pszMessageBuffer, gc.refMesgString->GetBuffer(), cchMessage * sizeof(WCHAR));
        pszMessageBuffer[cchMessage] = W('\0');
        pszMessage = pszMessageBuffer;
    }
    else
    {
        pszMessage = W("There is not enough memory to print the supplied FailFast message.");
        cchMessage = (DWORD)wcslen(pszMessage);
    }

    if (cchMessage == 0) {
        WszOutputDebugString(W("CLR: Managed code called FailFast without specifying a reason.\r\n"));
    }
    else {
        WszOutputDebugString(W("CLR: Managed code called FailFast.\r\n"));
        WszOutputDebugString(pszMessage);
        WszOutputDebugString(W("\r\n"));
    }

    LPCWSTR argExceptionString = NULL;
    StackSString msg;
    if (gc.refExceptionForWatsonBucketing != NULL)
    {
        GetExceptionMessage(gc.refExceptionForWatsonBucketing, msg);
        argExceptionString = msg.GetUnicode();
    }

    Thread *pThread = GetThread();

#ifndef TARGET_UNIX
    // If we have the exception object, then try to setup
    // the watson bucket if it has any details.
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this, if required.
    if (IsWatsonEnabled())
    {
        if ((gc.refExceptionForWatsonBucketing == NULL) || !SetupWatsonBucketsForFailFast(gc.refExceptionForWatsonBucketing))
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
            _ASSERTE(pUEWatsonBucketTracker != NULL);
            pUEWatsonBucketTracker->SaveIpForWatsonBucket(retAddress);
            pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::FatalError, pThread, NULL);
            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
    }
#endif // !TARGET_UNIX

    // stash the user-provided exception object. this will be used as
    // the inner exception object to the FatalExecutionEngineException.
    if (gc.refExceptionForWatsonBucketing != NULL)
        pThread->SetLastThrownObject(gc.refExceptionForWatsonBucketing);

    EEPolicy::HandleFatalError(exitCode, retAddress, pszMessage, NULL, errorSourceString, argExceptionString);

    GCPROTECT_END();
}

// Note: Do not merge this FCALL method with any other FailFast overloads.
// Watson uses the managed FailFast method with one String for crash dump bucketization.
FCIMPL1(VOID, SystemNative::FailFast, StringObject* refMessageUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(refMessage);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();

    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, NULL, retaddr, COR_E_FAILFAST, NULL);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(VOID, SystemNative::FailFastWithExitCode, StringObject* refMessageUNSAFE, UINT exitCode)
{
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(refMessage);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();

    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, NULL, retaddr, exitCode, NULL);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(VOID, SystemNative::FailFastWithException, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;
    EXCEPTIONREF refException = (EXCEPTIONREF)refExceptionUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(refMessage, refException);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();

    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, refException, retaddr, COR_E_FAILFAST, NULL);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(VOID, SystemNative::FailFastWithExceptionAndSource, StringObject* refMessageUNSAFE, ExceptionObject* refExceptionUNSAFE, StringObject* errorSourceUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF refMessage = (STRINGREF)refMessageUNSAFE;
    EXCEPTIONREF refException = (EXCEPTIONREF)refExceptionUNSAFE;
    STRINGREF errorSource = (STRINGREF)errorSourceUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_3(refMessage, refException, errorSource);

    // The HelperMethodFrame knows how to get the return address.
    UINT_PTR retaddr = HELPER_METHOD_FRAME_GET_RETURN_ADDRESS();

    // Call the actual worker to perform failfast
    GenericFailFast(refMessage, refException, retaddr, COR_E_FAILFAST, errorSource);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, SystemNative::IsServerGC)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(GCHeapUtilities::IsServerHeap());
}
FCIMPLEND

#if defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE X86BaseCpuId(int cpuInfo[4], int functionId, int subFunctionId)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    __cpuidex(cpuInfo, functionId, subFunctionId);

    END_QCALL;
}

#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
