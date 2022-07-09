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
    static FCDECL0(INT64, GetPendingUnmanagedWorkItemCount);
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
