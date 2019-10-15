// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
    static FCDECL2(FC_BOOL_RET, CorSetMaxThreads, DWORD workerThreads, DWORD completionPortThreads);
    static FCDECL2(VOID, CorGetMaxThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL2(FC_BOOL_RET, CorSetMinThreads, DWORD workerThreads, DWORD completionPortThreads);
    static FCDECL2(VOID, CorGetMinThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL2(VOID, CorGetAvailableThreads, DWORD* workerThreads, DWORD* completionPortThreads);
    static FCDECL0(INT32, GetThreadCount);
    static INT64 QCALLTYPE GetCompletedWorkItemCount();
    static FCDECL0(INT64, GetPendingUnmanagedWorkItemCount);

    static FCDECL0(VOID, NotifyRequestProgress);
    static FCDECL0(FC_BOOL_RET, NotifyRequestComplete);

    static void QCALLTYPE InitializeVMTp(CLR_BOOL* pEnableWorkerTracking);

    static FCDECL1(void, ReportThreadStatus, CLR_BOOL isWorking);


    static FCDECL5(LPVOID, CorRegisterWaitForSingleObject,
                                Object* waitObjectUNSAFE,
                                Object* stateUNSAFE,
                                UINT32 timeout,
                                CLR_BOOL executeOnlyOnce,
                                Object* registeredWaitObjectUNSAFE);

    static BOOL QCALLTYPE RequestWorkerThread();

    static FCDECL1(FC_BOOL_RET, CorPostQueuedCompletionStatus, LPOVERLAPPED lpOverlapped);    
    static FCDECL2(FC_BOOL_RET, CorUnregisterWait, LPVOID WaitHandle, Object * objectToNotify);
    static FCDECL1(void, CorWaitHandleCleanupNative, LPVOID WaitHandle);
    static FCDECL1(FC_BOOL_RET, CorBindIoCompletionCallback, HANDLE fileHandle);
};

class AppDomainTimerNative
{
public:
    static HANDLE QCALLTYPE CreateAppDomainTimer(INT32 dueTime, INT32 timerId);
    static BOOL QCALLTYPE ChangeAppDomainTimer(HANDLE hTimer, INT32 dueTime);
    static BOOL QCALLTYPE DeleteAppDomainTimer(HANDLE hTimer);
};

VOID QueueUserWorkItemManagedCallback(PVOID pArg);
void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                         DWORD numBytesTransferred,
                                         LPOVERLAPPED lpOverlapped);
void SetAsyncResultProperties(
    OVERLAPPEDDATAREF overlapped,
    DWORD dwErrorCode, 
    DWORD dwNumBytes);

#endif
