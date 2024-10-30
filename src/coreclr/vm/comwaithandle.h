// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header: COMWaitHandle.h
**
** Purpose: Native methods on System.WaitHandle
**
**
===========================================================*/

#ifndef _COM_WAITABLE_HANDLE_H
#define _COM_WAITABLE_HANDLE_H

extern "C" INT32 QCALLTYPE WaitHandle_WaitOneCore(HANDLE handle, INT32 timeout, BOOL useTrivialWaits);
extern "C" INT32 QCALLTYPE WaitHandle_WaitMultipleIgnoringSyncContext(HANDLE *handleArray, INT32 numHandles, BOOL waitForAll, INT32 timeout);
extern "C" INT32 QCALLTYPE WaitHandle_SignalAndWait(HANDLE waitHandleSignal, HANDLE waitHandleWait, INT32 timeout);

#ifdef TARGET_UNIX
extern "C" INT32 QCALLTYPE WaitHandle_WaitOnePrioritized(HANDLE handle, INT32 timeoutMs);
#endif // TARGET_UNIX

#endif // _COM_WAITABLE_HANDLE_H
