// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================

#ifndef _FINALIZER_THREAD_H_
#define _FINALIZER_THREAD_H_

class FinalizerThread
{
    static BOOL fRunFinalizersOnUnload;
    static BOOL fQuitFinalizer;
    static AppDomain *UnloadingAppDomain;

    static CLREvent *hEventFinalizer;
    static CLREvent *hEventFinalizerDone;
    static CLREvent *hEventShutDownToFinalizer;
    static CLREvent *hEventFinalizerToShutDown;

    // Note: This enum makes it easier to read much of the code that deals with the
    // array of events that the finalizer thread waits on.  However, the ordering
    // is important.
    // See code:SVR::WaitForFinalizerEvent#MHandleTypeValues for more info
    enum MHandleType
    {
        kLowMemoryNotification  = 0,
        kFinalizer              = 1,

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
        kProfilingAPIAttach     = 2,
#endif // FEATURE_PROFAPI_ATTACH_DETACH 

        kHandleCount,
    };

    static HANDLE MHandles[kHandleCount];

    static void WaitForFinalizerEvent (CLREvent *event);

    static BOOL FinalizerThreadWatchDogHelper();

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    static void ProcessProfilerAttachIfNecessary(ULONGLONG * pui64TimestampLastCheckedEventMs);
#endif // FEATURE_PROFAPI_ATTACH_DETACH

    static Object * DoOneFinalization(Object* fobj, Thread* pThread, int bitToCheck, bool *pbTerminate);

    static void FinalizeAllObjects_Wrapper(void *ptr);
    static Object * FinalizeAllObjects(Object* fobj, int bitToCheck);

public:
    static Thread* GetFinalizerThread() 
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_pFinalizerThread != 0);
        return g_pFinalizerThread;
    }

    // Start unloading app domain
    static void UnloadAppDomain(AppDomain *pDomain, BOOL fRunFinalizers)
    {
        LIMITED_METHOD_CONTRACT;
        UnloadingAppDomain = pDomain; 
        fRunFinalizersOnUnload = fRunFinalizers;
    }

    static AppDomain*  GetUnloadingAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        return UnloadingAppDomain;
    }

    static BOOL IsCurrentThreadFinalizer();

    static void EnableFinalization();

    static BOOL HaveExtraWorkForFinalizer();

    static void FinalizerThreadWait(DWORD timeout = INFINITE);

    // We wake up a wait for finaliation for two reasons:
    // if fFinalizer=TRUE, we have finished finalization.
    // if fFinalizer=FALSE, the timeout for finalization is changed, and AD unload helper thread is notified.
    static void SignalFinalizationDone(BOOL fFinalizer);

    static VOID FinalizerThreadWorker(void *args);
    static void FinalizeObjectsOnShutdown(LPVOID args);
    static DWORD __stdcall FinalizerThreadStart(void *args);

    static DWORD FinalizerThreadCreate();
    static BOOL FinalizerThreadWatchDog();
};

#endif // _FINALIZER_THREAD_H_
