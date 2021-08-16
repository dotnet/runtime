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

    enum
    {
        ApartmentSTA = 0,
        ApartmentMTA = 1,
        ApartmentUnknown = 2
    };

    static void QCALLTYPE Start(QCall::ThreadHandle thread, int threadStackSize, int priority, PCWSTR pThreadName);
    static FCDECL1(INT32,   GetPriority,       ThreadBaseObject* pThisUNSAFE);
    static FCDECL2(void,    SetPriority,       ThreadBaseObject* pThisUNSAFE, INT32 iPriority);
    static FCDECL1(void,    Interrupt,         ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsAlive,       ThreadBaseObject* pThisUNSAFE);
    static FCDECL2(FC_BOOL_RET, Join,          ThreadBaseObject* pThisUNSAFE, INT32 Timeout);
#undef Sleep
    static FCDECL1(void,    Sleep,             INT32 iTime);
#define Sleep(a) Dont_Use_Sleep(a)
    static void QCALLTYPE   UninterruptibleSleep0();
    static FCDECL1(void,    Initialize,        ThreadBaseObject* pThisUNSAFE);
    static FCDECL2(void,    SetBackground,     ThreadBaseObject* pThisUNSAFE, CLR_BOOL isBackground);
    static FCDECL1(FC_BOOL_RET, IsBackground,  ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(INT32,   GetThreadState,    ThreadBaseObject* pThisUNSAFE);
    static FCDECL1(INT32,   GetThreadContext,  ThreadBaseObject* pThisUNSAFE);
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    static FCDECL1(INT32,   GetApartmentState, ThreadBaseObject* pThis);
    static FCDECL2(INT32,   SetApartmentState, ThreadBaseObject* pThisUNSAFE, INT32 iState);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    static
    void QCALLTYPE InformThreadNameChange(QCall::ThreadHandle thread, LPCWSTR name, INT32 len);

    static
    UINT64 QCALLTYPE GetProcessDefaultStackSize();

    static FCDECL1(INT32,   GetManagedThreadId, ThreadBaseObject* th);
    static FCDECL0(INT32,   GetOptimalMaxSpinWaitsPerSpinIteration);
    static FCDECL1(void,    SpinWait,                       int iterations);
    static BOOL QCALLTYPE YieldThread();
    static FCDECL0(Object*, GetCurrentThread);
    static UINT64 QCALLTYPE GetCurrentOSThreadId();
    static FCDECL1(void,    Finalize,                       ThreadBaseObject* pThis);
#ifdef FEATURE_COMINTEROP
    static FCDECL1(void,    DisableComObjectEagerCleanup,   ThreadBaseObject* pThis);
#endif //FEATURE_COMINTEROP
    static FCDECL1(FC_BOOL_RET,IsThreadpoolThread,          ThreadBaseObject* thread);
    static FCDECL1(void,    SetIsThreadpoolThread,          ThreadBaseObject* thread);

    static FCDECL0(INT32,   GetCurrentProcessorNumber);

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


#endif // _COMSYNCHRONIZABLE_H

