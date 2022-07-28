// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header: COMThreadPool.h
**
** Purpose: Native methods on System.ThreadPool
**          and its inner classes
**
**
===========================================================*/

#ifndef _COMTHREADPOOL_H
#define _COMTHREADPOOL_H

#include "delegateinfo.h"
#include "nativeoverlapped.h"

class ThreadPoolNative
{

public:
    static FCDECL4(INT32, GetNextConfigUInt32Value,
        INT32 configVariableIndex,
        UINT32 *configValueRef,
        BOOL *isBooleanRef,
        LPCWSTR *appContextConfigNameRef);
    static FCDECL1(FC_BOOL_RET, CorCanSetMinIOCompletionThreads, DWORD ioCompletionThreads);
    static FCDECL1(FC_BOOL_RET, CorCanSetMaxIOCompletionThreads, DWORD ioCompletionThreads);
    static FCDECL2(FC_BOOL_RET, CorSetMaxThreads, DWORD workerThreads, DWORD completionPortThreads);
    static FCDECL2(VOID, CorGetMaxThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL2(FC_BOOL_RET, CorSetMinThreads, DWORD workerThreads, DWORD completionPortThreads);
    static FCDECL2(VOID, CorGetMinThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL2(VOID, CorGetAvailableThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL0(INT32, GetThreadCount);
    static FCDECL0(INT64, GetPendingUnmanagedWorkItemCount);

    static FCDECL0(VOID, NotifyRequestProgress);
    static FCDECL0(FC_BOOL_RET, NotifyRequestComplete);

    static FCDECL0(FC_BOOL_RET, GetEnableWorkerTracking);

    static FCDECL1(void, ReportThreadStatus, CLR_BOOL isWorking);

    static FCDECL5(LPVOID, CorRegisterWaitForSingleObject,
                                Object* waitObjectUNSAFE,
                                Object* stateUNSAFE,
                                UINT32 timeout,
                                CLR_BOOL executeOnlyOnce,
                                Object* registeredWaitObjectUNSAFE);

    static FCDECL1(FC_BOOL_RET, CorPostQueuedCompletionStatus, LPOVERLAPPED lpOverlapped);
    static FCDECL2(FC_BOOL_RET, CorUnregisterWait, LPVOID WaitHandle, Object * objectToNotify);
    static FCDECL1(void, CorWaitHandleCleanupNative, LPVOID WaitHandle);
    static FCDECL1(FC_BOOL_RET, CorBindIoCompletionCallback, HANDLE fileHandle);
};

extern "C" INT64 QCALLTYPE ThreadPool_GetCompletedWorkItemCount();
extern "C" BOOL QCALLTYPE ThreadPool_RequestWorkerThread();
extern "C" BOOL QCALLTYPE ThreadPool_PerformGateActivities(INT32 cpuUtilization);

VOID QueueUserWorkItemManagedCallback(PVOID pArg);
void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                         DWORD numBytesTransferred,
                                         LPOVERLAPPED lpOverlapped);
void SetAsyncResultProperties(
    OVERLAPPEDDATAREF overlapped,
    DWORD dwErrorCode,
    DWORD dwNumBytes);

#endif
