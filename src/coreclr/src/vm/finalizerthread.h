// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================

#ifndef _FINALIZER_THREAD_H_
#define _FINALIZER_THREAD_H_

class FinalizerThread
{
    static BOOL fQuitFinalizer;

#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
    static ULONGLONG LastHeapDumpTime;
#endif

    static CLREvent *hEventFinalizer;
    static CLREvent *hEventFinalizerDone;
    static CLREvent *hEventFinalizerToShutDown;

    // Note: This enum makes it easier to read much of the code that deals with the
    // array of events that the finalizer thread waits on.  However, the ordering
    // is important.
    // See code:SVR::WaitForFinalizerEvent#MHandleTypeValues for more info
    enum MHandleType
    {
        kLowMemoryNotification  = 0,
        kFinalizer              = 1,
        kHandleCount,
    };

    static HANDLE MHandles[kHandleCount];

    static void WaitForFinalizerEvent (CLREvent *event);

    static void DoOneFinalization(Object* fobj, Thread* pThread);

    static void FinalizeAllObjects(int bitToCheck);

public:
    static Thread* GetFinalizerThread() 
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_pFinalizerThread != 0);
        return g_pFinalizerThread;
    }

    static BOOL IsCurrentThreadFinalizer();

    static void EnableFinalization();

    static BOOL HaveExtraWorkForFinalizer();

    static void RaiseShutdownEvents()
    {
        WRAPPER_NO_CONTRACT;
        fQuitFinalizer = TRUE;
        EnableFinalization();

        // Do not wait for FinalizerThread if the current one is FinalizerThread.
        if (GetThread() != GetFinalizerThread())
        {
            // This wait must be alertable to handle cases where the current
            // thread's context is needed (i.e. RCW cleanup)
            hEventFinalizerToShutDown->Wait(INFINITE, /*alertable*/ TRUE);
        }
    }

    static void FinalizerThreadWait(DWORD timeout = INFINITE);

    // We wake up a wait for finaliation for two reasons:
    // if fFinalizer=TRUE, we have finished finalization.
    // if fFinalizer=FALSE, the timeout for finalization is changed, and AD unload helper thread is notified.
    static void SignalFinalizationDone(BOOL fFinalizer);

    static VOID FinalizerThreadWorker(void *args);
    static DWORD WINAPI FinalizerThreadStart(void *args);

    static void FinalizerThreadCreate();
};

#endif // _FINALIZER_THREAD_H_
