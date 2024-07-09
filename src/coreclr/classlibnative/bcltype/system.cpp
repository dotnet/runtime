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

#include <minipal/cpuid.h>


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

    // The pStackTraceUNSAFE can be either I1Array or Object[]. In the latter case, the first entry is the actual stack trace I1Array,
    // the rest are pointers to the method info objects. We only care about the first entry here.
    if (pStackTraceUNSAFE->GetArrayElementType() != ELEMENT_TYPE_I1)
    {
        PtrArray *combinedArray = (PtrArray*)pStackTraceUNSAFE;
        pStackTraceUNSAFE = (ArrayBase*)OBJECTREFToObject(combinedArray->GetAt(0));
    }
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

struct FindFailFastCallerStruct {
    StackCrawlMark* pStackMark;
    UINT_PTR        retAddress;
};

// This method is called by the GetMethod function and will crawl backward
//  up the stack for integer methods.
static StackWalkAction FindFailFastCallerCallback(CrawlFrame* frame, VOID* data) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    FindFailFastCallerStruct* pFindCaller = (FindFailFastCallerStruct*) data;

    // The check here is between the address of a local variable
    // (the stack mark) and a pointer to the EIP for a frame
    // (which is actually the pointer to the return address to the
    // function from the previous frame). So we'll actually notice
    // which frame the stack mark was in one frame later. This is
    // fine since we only implement LookForMyCaller.
    _ASSERTE(*pFindCaller->pStackMark == LookForMyCaller);
    if (!frame->IsInCalleesFrames(pFindCaller->pStackMark))
        return SWA_CONTINUE;

    pFindCaller->retAddress = GetControlPC(frame->GetRegisterSet());
    return SWA_ABORT;
}

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, PCWSTR message, QCall::ObjectHandleOnStack exception, PCWSTR errorSource)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    FindFailFastCallerStruct findCallerData;
    findCallerData.pStackMark = mark;
    findCallerData.retAddress = 0;
    GetThread()->StackWalkFrames(FindFailFastCallerCallback, &findCallerData, FUNCTIONSONLY | QUICKUNWIND);

    if (message == NULL || message[0] == W('\0'))
    {
        OutputDebugString(W("CLR: Managed code called FailFast without specifying a reason.\r\n"));
    }
    else
    {
        OutputDebugString(W("CLR: Managed code called FailFast.\r\n"));
        OutputDebugString(message);
        OutputDebugString(W("\r\n"));
    }

    LPCWSTR argExceptionString = NULL;
    StackSString msg;
    if (exception.Get() != NULL)
    {
        GetExceptionMessage(exception.Get(), msg);
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
        if ((exception.Get() == NULL) || !SetupWatsonBucketsForFailFast((EXCEPTIONREF)exception.Get()))
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
            _ASSERTE(pUEWatsonBucketTracker != NULL);
            pUEWatsonBucketTracker->SaveIpForWatsonBucket(findCallerData.retAddress);
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
    if (exception.Get() != NULL)
        pThread->SetLastThrownObject(exception.Get());

    EEPolicy::HandleFatalError(COR_E_FAILFAST, findCallerData.retAddress, message, NULL, errorSource, argExceptionString);

    END_QCALL;
}

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
