// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header: COMSynchronizable.h
**
** Purpose: Native methods on System.SynchronizableObject
**          and its subclasses.
**
**
===========================================================*/

#ifndef _COMSYNCHRONIZABLE_H
#define _COMSYNCHRONIZABLE_H

class ThreadNative
{
public:
    enum
    {
        PRIORITY_LOWEST = 0,
        PRIORITY_BELOW_NORMAL = 1,
        PRIORITY_NORMAL = 2,
        PRIORITY_ABOVE_NORMAL = 3,
        PRIORITY_HIGHEST = 4,
    };

    enum
    {
        ThreadStopRequested = 1,
        ThreadSuspendRequested = 2,
        ThreadBackground = 4,
        ThreadUnstarted = 8,
        ThreadStopped = 16,
        ThreadWaitSleepJoin = 32,
        ThreadSuspended = 64,
        ThreadAbortRequested = 128,
    };

    FCDECL0(static INT32,       GetOptimalMaxSpinWaitsPerSpinIteration);
#ifdef TARGET_WASM
    FCDECL0(static void*,       GetThreadStaticsBaseNative);
#endif
    FCDECL1(static void,        Finalize, ThreadBaseObject* pThis);
    FCDECL0(static FC_BOOL_RET, CatchAtSafePoint);
    FCDECL0(static FC_BOOL_RET, CurrentThreadIsFinalizerThread);
};

extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_GetQCallSpecialException(INT_PTR status, QCall::ObjectHandleOnStack exception);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_Start(QCall::ThreadHandle thread, int threadStackSize, int priority, BOOL isThreadPool, PCWSTR pThreadName, QCall::ObjectHandleOnStack exception, BOOL* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_SetPriority(QCall::ObjectHandleOnStack thread, INT32 iPriority);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_GetCurrentThread(QCall::ObjectHandleOnStack thread);
extern "C" BOOL QCALLTYPE ThreadNative_GetIsBackground(QCall::ThreadHandle thread);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_SetIsBackground(QCall::ThreadHandle thread, BOOL value);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_InformThreadNameChange(QCall::ThreadHandle thread, LPCWSTR name, INT32 len);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_YieldThread(BOOL* pReturnValue);
extern "C" void QCALLTYPE ThreadNative_PollGC();
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_GetCurrentOSThreadId(UINT64* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_Initialize(QCall::ObjectHandleOnStack t);
extern "C" INT32 QCALLTYPE ThreadNative_GetThreadState(QCall::ThreadHandle thread);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_ReentrantWaitAny(BOOL alertable, INT32 timeout, INT32 count, HANDLE *handles, INT32* pReturnValue);
#ifdef TARGET_WINDOWS
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_Interrupt(QCall::ThreadHandle thread);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_CheckForPendingInterrupt();
#endif // TARGET_WINDOWS

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_GetApartmentState(QCall::ObjectHandleOnStack t, INT32* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_SetApartmentState(QCall::ObjectHandleOnStack t, INT32 iState, INT32* pReturnValue);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef TARGET_WINDOWS
extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_GetOSHandle(QCall::ThreadHandle t, HANDLE* pReturnValue);
#endif // TARGET_WINDOWS

extern "C" QCallExceptionStatus QCALLTYPE ThreadNative_Abort(QCall::ThreadHandle thread);
extern "C" void QCALLTYPE ThreadNative_ResetAbort();
extern "C" void QCALLTYPE ThreadNative_SpinWait(INT32 iterations);
#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE ThreadNative_DisableComObjectEagerCleanup(QCall::ThreadHandle thread);
#endif // FEATURE_COMINTEROP

extern "C" QCallExceptionStatus QCALLTYPE ObjectHeader_GetOrCreateLockObject(QCall::ObjectHandleOnStack obj, QCall::ObjectHandleOnStack lockObj);

FCDECL1(OBJECTHANDLE, ObjectHeader_GetLockHandleIfExists, Object* obj);
#endif // _COMSYNCHRONIZABLE_H
