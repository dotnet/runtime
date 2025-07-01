// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** COMWaitHandle.cpp
**
** Purpose: Native methods on System.WaitHandle
**
**
===========================================================*/
#include "common.h"
#include "comwaithandle.h"

extern "C" INT32 QCALLTYPE WaitHandle_WaitOneCore(HANDLE handle, INT32 timeout, BOOL useTrivialWaits)
{
    QCALL_CONTRACT;

    INT32 retVal = 0;

    BEGIN_QCALL;

    _ASSERTE(handle != 0);
    _ASSERTE(handle != INVALID_HANDLE_VALUE);

    Thread* pThread = GET_THREAD();
    WaitMode waitMode = (WaitMode)((!useTrivialWaits ? WaitMode_Alertable : WaitMode_None) | WaitMode_IgnoreSyncCtx);
    retVal = pThread->DoAppropriateWait(1, &handle, TRUE, timeout, waitMode);

    END_QCALL;
    return retVal;
}

extern "C" INT32 QCALLTYPE WaitHandle_WaitMultipleIgnoringSyncContext(HANDLE *handleArray, INT32 numHandles, BOOL waitForAll, INT32 timeout)
{
    QCALL_CONTRACT;

    INT32 ret = 0;
    BEGIN_QCALL;

    Thread * pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // There are some issues with wait-all from an STA thread
    // - https://github.com/dotnet/runtime/issues/10243#issuecomment-385117537
    if (waitForAll && numHandles > 1 && pThread->GetApartment() == Thread::AS_InSTA)
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_WaitAllSTAThread"));
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    ret = pThread->DoAppropriateWait(numHandles, handleArray, waitForAll, timeout, (WaitMode)(WaitMode_Alertable | WaitMode_IgnoreSyncCtx));

    END_QCALL;
    return ret;
}

extern "C" INT32 QCALLTYPE WaitHandle_SignalAndWait(HANDLE waitHandleSignal, HANDLE waitHandleWait, INT32 timeout)
{
    QCALL_CONTRACT;

    INT32 retVal = (DWORD)-1;

    BEGIN_QCALL;

    _ASSERTE(waitHandleSignal != 0);
    _ASSERTE(waitHandleWait != 0);

    Thread* pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    if (pThread->GetApartment() == Thread::AS_InSTA)
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_SignalAndWaitSTAThread"));
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    HANDLE handles[] = { waitHandleSignal, waitHandleWait };
    retVal = pThread->DoSignalAndWait(handles, timeout, TRUE /*alertable*/);

    END_QCALL;
    return retVal;
}

#ifdef TARGET_UNIX
extern "C" INT32 QCALLTYPE WaitHandle_WaitOnePrioritized(HANDLE handle, INT32 timeoutMs)
{
    QCALL_CONTRACT;

    DWORD result = WAIT_FAILED;

    BEGIN_QCALL;

    _ASSERTE(handle != NULL);
    _ASSERTE(handle != INVALID_HANDLE_VALUE);

    result = PAL_WaitForSingleObjectPrioritized(handle, timeoutMs);

    END_QCALL;
    return (INT32)result;
}
#endif // TARGET_UNIX
