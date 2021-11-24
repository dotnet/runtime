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


class WaitHandleNative
{
public:
    static FCDECL2(INT32, CorWaitOneNative, HANDLE handle, INT32 timeout);
    static FCDECL4(INT32, CorWaitMultipleNative, HANDLE *handleArray, INT32 numHandles, CLR_BOOL waitForAll, INT32 timeout);
    static FCDECL3(INT32, CorSignalAndWaitOneNative, HANDLE waitHandleSignalUNSAFE, HANDLE waitHandleWaitUNSAFE, INT32 timeout);
};
#ifdef TARGET_UNIX
extern "C" INT32 QCALLTYPE WaitHandle_CorWaitOnePrioritizedNative(HANDLE handle, INT32 timeoutMs);
#endif
#endif
