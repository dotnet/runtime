// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

typedef void(WINAPI *pfnGetSystemTimeAsFileTime)(LPFILETIME lpSystemTimeAsFileTime);
extern pfnGetSystemTimeAsFileTime g_pfnGetSystemTimeAsFileTime;

void WINAPI InitializeGetSystemTimeAsFileTime(LPFILETIME lpSystemTimeAsFileTime)
{
    pfnGetSystemTimeAsFileTime func = NULL;

#ifndef FEATURE_PAL
    HMODULE hKernel32 = WszLoadLibrary(W("kernel32.dll"));
    if (hKernel32 != NULL)
    {
        func = (pfnGetSystemTimeAsFileTime)GetProcAddress(hKernel32, "GetSystemTimePreciseAsFileTime");
        if (func != NULL)
        {
            // GetSystemTimePreciseAsFileTime exists and we'd like to use it.  However, on
            // misconfigured systems, it's possible for the "precise" time to be inaccurate:
            //     https://github.com/dotnet/coreclr/issues/14187
            // If it's inaccurate, though, we expect it to be wildly inaccurate, so as a
            // workaround/heuristic, we get both the "normal" and "precise" times, and as
            // long as they're close, we use the precise one. This workaround can be removed
            // when we better understand what's causing the drift and the issue is no longer
            // a problem or can be better worked around on all targeted OSes.

            FILETIME systemTimeResult;
            ::GetSystemTimeAsFileTime(&systemTimeResult);

            FILETIME preciseSystemTimeResult;
            func(&preciseSystemTimeResult);

            LONG64 systemTimeLong100ns = (LONG64)((((ULONG64)systemTimeResult.dwHighDateTime) << 32) | (ULONG64)systemTimeResult.dwLowDateTime);
            LONG64 preciseSystemTimeLong100ns = (LONG64)((((ULONG64)preciseSystemTimeResult.dwHighDateTime) << 32) | (ULONG64)preciseSystemTimeResult.dwLowDateTime);

            const INT32 THRESHOLD_100NS = 1000000; // 100ms
            if (abs(preciseSystemTimeLong100ns - systemTimeLong100ns) > THRESHOLD_100NS)
            {
                // Too much difference.  Don't use GetSystemTimePreciseAsFileTime.
                func = NULL;
            }
        }
    }
    if (func == NULL)
#endif
    {
        func = &::GetSystemTimeAsFileTime;
    }

    g_pfnGetSystemTimeAsFileTime = func;
    func(lpSystemTimeAsFileTime);
}

pfnGetSystemTimeAsFileTime g_pfnGetSystemTimeAsFileTime = &InitializeGetSystemTimeAsFileTime;

FCIMPL0(INT64, SystemNative::__GetSystemTimeAsFileTime)
{
    FCALL_CONTRACT;

    INT64 timestamp;
    g_pfnGetSystemTimeAsFileTime((FILETIME*)&timestamp);

#if BIGENDIAN
    timestamp = (INT64)(((UINT64)timestamp >> 32) | ((UINT64)timestamp << 32));
#endif

    return timestamp;
}
FCIMPLEND;


#ifndef FEATURE_PAL

FCIMPL1(VOID, SystemNative::GetSystemTimeWithLeapSecondsHandling, FullSystemTime *time)
{
    FCALL_CONTRACT;
    INT64 timestamp;

    g_pfnGetSystemTimeAsFileTime((FILETIME*)&timestamp);

    if (::FileTimeToSystemTime((FILETIME*)&timestamp, &(time->systemTime)))
    {
        // to keep the time precision
        time->hundredNanoSecond = timestamp % 10000; // 10000 is the number of 100-nano seconds per Millisecond
    }
    else
    {
        ::GetSystemTime(&(time->systemTime));
        time->hundredNanoSecond = 0;
    }

    if (time->systemTime.wSecond > 59)
    {
        // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
        // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
        time->systemTime.wSecond = 59;
        time->systemTime.wMilliseconds = 999;
        time->hundredNanoSecond = 9999;
    }
}
FCIMPLEND;

FCIMPL2(FC_BOOL_RET, SystemNative::FileTimeToSystemTime, INT64 fileTime, FullSystemTime *time)
{
    FCALL_CONTRACT;
    if (::FileTimeToSystemTime((FILETIME*)&fileTime, (LPSYSTEMTIME) time))
    {
        // to keep the time precision
        time->hundredNanoSecond = fileTime % 10000; // 10000 is the number of 100-nano seconds per Millisecond
        if (time->systemTime.wSecond > 59)
        {
            // we have a leap second, force it to last second in the minute as DateTime doesn't account for leap seconds in its calculation.
            // we use the maxvalue from the milliseconds and the 100-nano seconds to avoid reporting two out of order 59 seconds
            time->systemTime.wSecond = 59;
            time->systemTime.wMilliseconds = 999;
            time->hundredNanoSecond = 9999;
        }
        FC_RETURN_BOOL(TRUE);
    }
    FC_RETURN_BOOL(FALSE);
}
FCIMPLEND;

FCIMPL2(FC_BOOL_RET, SystemNative::ValidateSystemTime, SYSTEMTIME *time, CLR_BOOL localTime)
{
    FCALL_CONTRACT;

    if (localTime)
    {
        SYSTEMTIME st;
        FC_RETURN_BOOL(::TzSpecificLocalTimeToSystemTime(NULL, time, &st));
    }
    else
    {
        FILETIME timestamp;
        FC_RETURN_BOOL(::SystemTimeToFileTime(time, &timestamp));
    }
}
FCIMPLEND;

FCIMPL2(FC_BOOL_RET, SystemNative::SystemTimeToFileTime, SYSTEMTIME *time, INT64 *pFileTime)
{
    FCALL_CONTRACT;

    BOOL ret = ::SystemTimeToFileTime(time, (LPFILETIME) pFileTime);
    FC_RETURN_BOOL(ret);
}
FCIMPLEND;
#endif // FEATURE_PAL


FCIMPL0(UINT32, SystemNative::GetTickCount)
{
    FCALL_CONTRACT;

    return ::GetTickCount();
}
FCIMPLEND;




VOID QCALLTYPE SystemNative::Exit(INT32 exitcode)
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

void QCALLTYPE SystemNative::_GetCommandLine(QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    LPCWSTR commandLine;

    commandLine = WszGetCommandLine();
    if (commandLine==NULL)
        COMPlusThrowOM();

    retString.Set(commandLine);

    END_QCALL;
}

FCIMPL0(Object*, SystemNative::GetCommandLineArgs)
{
    FCALL_CONTRACT;

    PTRARRAYREF strArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(strArray);

    LPWSTR commandLine;

    commandLine = WszGetCommandLine();
    if (commandLine==NULL)
        COMPlusThrowOM();

    DWORD numArgs = 0;
    LPWSTR* argv = SegmentCommandLine(commandLine, &numArgs);
    if (!argv)
        COMPlusThrowOM();

    _ASSERTE(numArgs > 0);

    strArray = (PTRARRAYREF) AllocateObjectArray(numArgs, g_pStringClass);
    // Copy each argument into new Strings.
    for(unsigned int i=0; i<numArgs; i++)
    {
        STRINGREF str = StringObject::NewString(argv[i]);
        STRINGREF * destData = ((STRINGREF*)(strArray->GetDataPtr())) + i;
        SetObjectReference((OBJECTREF*)destData, (OBJECTREF)str, strArray->GetAppDomain());
    }
    delete [] argv;

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(strArray);
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

INT32 QCALLTYPE SystemNative::GetProcessorCount()
{
    QCALL_CONTRACT;

    INT32 processorCount = 0;

    BEGIN_QCALL;

    CPUGroupInfo::EnsureInitialized();

    if(CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
    {
        processorCount = CPUGroupInfo::GetNumActiveProcessors();
    }

    // Processor count will be 0 if CPU groups are disabled/not supported
    if(processorCount == 0)
    {
        SYSTEM_INFO systemInfo;
        ZeroMemory(&systemInfo, sizeof(systemInfo));

        GetSystemInfo(&systemInfo);

        processorCount = systemInfo.dwNumberOfProcessors;
    }

#ifdef FEATURE_PAL
    uint32_t cpuLimit;

    if (PAL_GetCpuLimit(&cpuLimit) && cpuLimit < processorCount)
        processorCount = cpuLimit;
#endif

    END_QCALL;

    return processorCount;
}

FCIMPL0(FC_BOOL_RET, SystemNative::HasShutdownStarted)
{
    FCALL_CONTRACT;

    // Return true if the EE has started to shutdown and is now going to
    // aggressively finalize objects referred to by static variables OR
    // if someone is unloading the current AppDomain AND we have started
    // finalizing objects referred to by static variables.
    FC_RETURN_BOOL(g_fEEShutDown & ShutDown_Finalize2);
}
FCIMPLEND

// FailFast is supported in BCL.small as internal to support failing fast in places where EEE used to be thrown.
//
// Static message buffer used by SystemNative::FailFast to avoid reliance on a
// managed string object buffer. This buffer is not always used, see comments in
// the method below.
WCHAR g_szFailFastBuffer[256];
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
        SO_TOLERANT;
    }CONTRACTL_END;

    struct
    {
        STRINGREF refMesgString;
        EXCEPTIONREF refExceptionForWatsonBucketing;
        STRINGREF refErrorSourceString;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.refMesgString = refMesgString;
    gc.refExceptionForWatsonBucketing = refExceptionForWatsonBucketing;
    gc.refErrorSourceString = refErrorSourceString;

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
    WCHAR  *pszMessage = NULL;
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
        pszMessage = g_szFailFastBuffer;
    }
    else
    {
        // We can fail here, but we can handle the fault.
        CONTRACT_VIOLATION(FaultViolation);
        pszMessage = new (nothrow) WCHAR[cchMessage + 1];
        if (pszMessage == NULL)
        {
            // Truncate the message to what will fit in the static buffer.
            cchMessage = FAIL_FAST_STATIC_BUFFER_LENGTH - 1;
            pszMessage = g_szFailFastBuffer;
        }
    }

    if (cchMessage > 0)
        memcpyNoGCRefs(pszMessage, gc.refMesgString->GetBuffer(), cchMessage * sizeof(WCHAR));
    pszMessage[cchMessage] = W('\0');

    if (cchMessage == 0) {
        WszOutputDebugString(W("CLR: Managed code called FailFast without specifying a reason.\r\n"));
    }
    else {
        WszOutputDebugString(W("CLR: Managed code called FailFast, saying \""));
        WszOutputDebugString(pszMessage);
        WszOutputDebugString(W("\"\r\n"));
    }

    LPCWSTR argExceptionString = NULL;
    StackSString msg;
    if (gc.refExceptionForWatsonBucketing != NULL)
    {
        GetExceptionMessage(gc.refExceptionForWatsonBucketing, msg);
        argExceptionString = msg.GetUnicode();
    }

    Thread *pThread = GetThread();

#ifndef FEATURE_PAL
    // If we have the exception object, then try to setup
    // the watson bucket if it has any details.
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this, if required.
    if (IsWatsonEnabled())
    {
        BEGIN_SO_INTOLERANT_CODE(pThread);
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
        END_SO_INTOLERANT_CODE;
    }
#endif // !FEATURE_PAL

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

#ifdef FEATURE_COMINTEROP

BOOL QCALLTYPE SystemNative::WinRTSupported()
{
    QCALL_CONTRACT;

    BOOL hasWinRT = FALSE;

    BEGIN_QCALL;
    hasWinRT = ::WinRTSupported();
    END_QCALL;

    return hasWinRT;
}

#endif // FEATURE_COMINTEROP







