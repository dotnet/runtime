//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifndef GCENV_H_
#define GCENV_H_

//
// Extra VM headers required to compile GC-related files
//

#include "finalizerthread.h"

#include "threadsuspend.h"

#ifdef FEATURE_COMINTEROP
#include <windows.ui.xaml.h>
#endif

#include "stubhelpers.h"

#include "eeprofinterfaces.inl"

#ifdef GC_PROFILING
#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "profilepriv.h"
#endif

#ifdef DEBUGGING_SUPPORTED
#include "dbginterface.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif 

#ifdef FEATURE_UEF_CHAINMANAGER
// This is required to register our UEF callback with the UEF chain manager
#include <mscoruefwrapper.h>
#endif // FEATURE_UEF_CHAINMANAGER


struct ScanContext;
class CrawlFrame;

typedef void promote_func(PTR_PTR_Object, ScanContext*, DWORD);

typedef struct
{
    promote_func*  f;
    ScanContext*   sc;
    CrawlFrame *   cf;
} GCCONTEXT;


class GCToEEInterface
{
public:
    //
    // Suspend/Resume callbacks
    //
    typedef enum
    {
        SUSPEND_FOR_GC = 1,
        SUSPEND_FOR_GC_PREP = 6
    } SUSPEND_REASON;

    static void SuspendEE(SUSPEND_REASON reason);
    static void RestartEE(BOOL bFinishedGC); //resume threads.

    // 
    // The GC roots enumeration callback
    //
    static void ScanStackRoots(Thread * pThread, promote_func* fn, ScanContext* sc);

    // Optional static GC refs scanning for better parallelization of server GC marking
    static void ScanStaticGCRefsOpportunistically(promote_func* fn, ScanContext* sc);

    // 
    // Callbacks issues during GC that the execution engine can do its own bookeeping
    //

    // start of GC call back - single threaded
    static void GcStartWork(int condemned, int max_gen); 

    //EE can perform post stack scanning action, while the 
    // user threads are still suspended 
    static void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);

    // Called before BGC starts sweeping, the heap is walkable
    static void GcBeforeBGCSweepWork();

    // post-gc callback.
    static void GcDone(int condemned);

    // Promote refcounted handle callback
    static bool RefCountedHandleCallbacks(Object * pObject);

    // Sync block cache management
    static void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, LPARAM lp1, LPARAM lp2);
    static void SyncBlockCacheDemote(int max_gen);
    static void SyncBlockCachePromotionsGranted(int max_gen);

    // Thread functions
    static bool IsPreemptiveGCDisabled(Thread * pThread)
    {
        WRAPPER_NO_CONTRACT;
        return !!pThread->PreemptiveGCDisabled();
    }

    static void EnablePreemptiveGC(Thread * pThread)
    {
        WRAPPER_NO_CONTRACT;
        pThread->EnablePreemptiveGC();
    }

    static void DisablePreemptiveGC(Thread * pThread)
    {
        WRAPPER_NO_CONTRACT;
        pThread->DisablePreemptiveGC();
    }

    static void SetGCSpecial(Thread * pThread);
    static alloc_context * GetAllocContext(Thread * pThread);
    static bool CatchAtSafePoint(Thread * pThread);

    static Thread * GetThreadList(Thread * pThread);
};

#endif // GCENV_H_