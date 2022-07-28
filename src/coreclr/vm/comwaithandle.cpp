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
#include "object.h"
#include "field.h"
#include "excep.h"
#include "comwaithandle.h"

FCIMPL2(INT32, WaitHandleNative::CorWaitOneNative, HANDLE handle, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    _ASSERTE(handle != 0);
    _ASSERTE(handle != INVALID_HANDLE_VALUE);

    Thread* pThread = GET_THREAD();

    retVal = pThread->DoAppropriateWait(1, &handle, TRUE, timeout, (WaitMode)(WaitMode_Alertable | WaitMode_IgnoreSyncCtx));

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

#ifdef TARGET_UNIX
extern "C" INT32 QCALLTYPE WaitHandle_CorWaitOnePrioritizedNative(HANDLE handle, INT32 timeoutMs)
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
#endif

FCIMPL4(INT32, WaitHandleNative::CorWaitMultipleNative, HANDLE *handleArray, INT32 numHandles, CLR_BOOL waitForAll, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 ret = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

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

    HELPER_METHOD_FRAME_END();
    return ret;
}
FCIMPLEND

FCIMPL3(INT32, WaitHandleNative::CorSignalAndWaitOneNative, HANDLE waitHandleSignalUNSAFE, HANDLE waitHandleWaitUNSAFE, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    _ASSERTE(waitHandleSignalUNSAFE != 0);
    _ASSERTE(waitHandleWaitUNSAFE != 0);

    Thread* pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP
    if (pThread->GetApartment() == Thread::AS_InSTA) {
        COMPlusThrow(kNotSupportedException, W("NotSupported_SignalAndWaitSTAThread"));  //<TODO> Change this message
    }
#endif

    DWORD res = (DWORD) -1;

    HANDLE handles[2];
    handles[0] = waitHandleSignalUNSAFE;
    handles[1] = waitHandleWaitUNSAFE;
    {
        res = pThread->DoSignalAndWait(handles, timeout, TRUE /*alertable*/);
    }

    retVal = res;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND
