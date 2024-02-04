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

#include "field.h"          // For FieldDesc definition.

//
// Each function that we call through native only gets one argument,
// which is actually a pointer to its stack of arguments.  Our structs
// for accessing these are defined below.
//

struct SharedState;

class ThreadNative
{
friend class ThreadBaseObject;

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

    static FCDECL1(INT32,   GetPriority,       ThreadBaseObject* pThisUNSAFE);
    static FCDECL2(void,    SetPriority,       ThreadBaseObject* pThisUNSAFE, INT32 iPriority);
    static FCDECL1(FC_BOOL_RET, IsAlive,       ThreadBaseObject* pThisUNSAFE);
    static FCDECL2(FC_BOOL_RET, Join,          ThreadBaseObject* pThisUNSAFE, INT32 Timeout);
    static FCDECL1(void,    Initialize,        ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(FC_BOOL_RET, GetIsBackground,  ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(INT32,   GetThreadState,    ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(INT32,   GetThreadContext,  ThreadBaseObject* pThisUNSAFE);
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    static FCDECL1(INT32,   GetApartmentState, ThreadBaseObject* pThis);
    static FCDECL2(INT32,   SetApartmentState, ThreadBaseObject* pThisUNSAFE, INT32 iState);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT


    static FCDECL0(INT32,   GetOptimalMaxSpinWaitsPerSpinIteration);
    static FCDECL0(Object*, GetCurrentThread);
    static FCDECL1(void,    Finalize,                       ThreadBaseObject* pThis);
    static FCDECL1(FC_BOOL_RET,IsThreadpoolThread,          ThreadBaseObject* thread);
    static FCDECL1(void,    SetIsThreadpoolThread,          ThreadBaseObject* thread);

    static void Start(Thread* pNewThread, int threadStackSize, int priority, PCWSTR pThreadName);
    static void InformThreadNameChange(Thread* pThread, LPCWSTR name, INT32 len);
private:

    struct KickOffThread_Args {
        Thread *pThread;
        SharedState *share;
        ULONG retVal;
    };

    static void KickOffThread_Worker(LPVOID /* KickOffThread_Args* */);
    static ULONG WINAPI KickOffThread(void *pass);
    static BOOL DoJoin(THREADBASEREF DyingThread, INT32 timeout);
};

extern "C" void QCALLTYPE ThreadNative_Start(QCall::ThreadHandle thread, int threadStackSize, int priority, PCWSTR pThreadName);
extern "C" void QCALLTYPE ThreadNative_SetIsBackground(QCall::ThreadHandle thread, BOOL value);
extern "C" void QCALLTYPE ThreadNative_InformThreadNameChange(QCall::ThreadHandle thread, LPCWSTR name, INT32 len);
extern "C" UINT64 QCALLTYPE ThreadNative_GetProcessDefaultStackSize();
extern "C" BOOL QCALLTYPE ThreadNative_YieldThread();
extern "C" UINT64 QCALLTYPE ThreadNative_GetCurrentOSThreadId();
extern "C" void QCALLTYPE ThreadNative_Abort(QCall::ThreadHandle thread);
extern "C" void QCALLTYPE ThreadNative_ResetAbort();
extern "C" void QCALLTYPE ThreadNative_SpinWait(INT32 iterations);
extern "C" void QCALLTYPE ThreadNative_Interrupt(QCall::ThreadHandle thread);
extern "C" void QCALLTYPE ThreadNative_Sleep(INT32 iTime);
#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE ThreadNative_DisableComObjectEagerCleanup(QCall::ThreadHandle thread);
#endif // FEATURE_COMINTEROP

#endif // _COMSYNCHRONIZABLE_H

