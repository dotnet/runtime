//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    thread.cpp

Abstract:

    Thread object and core APIs



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/mutex.hpp"
#include "pal/handlemgr.hpp"
#include "pal/cs.hpp"
#include "pal/seh.hpp"

#include "procprivate.hpp"
#include "pal/process.h"
#include "pal/module.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"
#include "pal/init.h"

#include <signal.h>
#include <pthread.h>
#include <unistd.h>
#include <errno.h>
#include <stddef.h>
#include <sys/stat.h>
#if HAVE_MACH_THREADS
#include <mach/mach.h>
#endif // HAVE_MACH_THREADS
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL
#include <limits.h>
#if HAVE_SYS_LWP_H
#include <sys/lwp.h>
// If we don't have sys/lwp.h but do expect to use _lwp_self, declare it to silence compiler warnings
#elif HAVE__LWP_SELF
extern "C" int _lwp_self ();
#endif // HAVE_LWP_H

using namespace CorUnix;


/* ------------------- Definitions ------------------------------*/
SET_DEFAULT_DEBUG_CHANNEL(THREAD);

// The default stack size of a newly created thread (currently 256KB)
// when the dwStackSize parameter of PAL_CreateThread()
// is zero. This value can be set by setting the
// environment variable PAL_THREAD_DEFAULT_STACK_SIZE
// (the value should be in bytes and in hex).
DWORD CPalThread::s_dwDefaultThreadStackSize = 256*1024; 

/* list of free CPalThread objects */
static Volatile<CPalThread*> free_threads_list = NULL;

/* lock to access list of free THREAD structures */
/* NOTE: can't use a CRITICAL_SECTION here (see comment in FreeTHREAD) */
int free_threads_spinlock = 0;

/* lock to access iEndingThreads counter, condition variable to signal shutdown 
thread when any remaining threads have died, and count of exiting threads that
can't be suspended. */
pthread_mutex_t ptmEndThread;
pthread_cond_t ptcEndThread;
static int iEndingThreads = 0;

void
ThreadCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown,
    bool fCleanupSharedState
    );

PAL_ERROR
ThreadInitializationRoutine(
    CPalThread *pThread,
    CObjectType *pObjectType,
    void *pImmutableData,
    void *pSharedData,
    void *pProcessLocalData
    );

void 
IncrementEndingThreadCount(
    void
    );

void 
DecrementEndingThreadCount(
    void
    );

CObjectType CorUnix::otThread(
                otiThread,
                ThreadCleanupRoutine,
                ThreadInitializationRoutine,
                0, //sizeof(CThreadImmutableData),
                sizeof(CThreadProcessLocalData),
                0, //sizeof(CThreadSharedData),
                0, // THREAD_ALL_ACCESS,
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::UnnamedObject,
                CObjectType::LocalDuplicationOnly,
                CObjectType::WaitableObject,
                CObjectType::SingleTransitionObject,
                CObjectType::ThreadReleaseHasNoSideEffects,
                CObjectType::NoOwner
                );

CAllowedObjectTypes aotThread(otiThread);

/*++
Function:
  InternalEndCurrentThreadWrapper

  Destructor for the thread-specific data representing the current PAL thread.
  Called from pthread_exit.  (pthread_exit is not called from the thread on which
  main() was first invoked.  This is not a problem, though, since when main()
  returns, this results in an implicit call to exit().)

  arg: the PAL thread
*/
static void InternalEndCurrentThreadWrapper(void *arg)
{
    CPalThread *pThread = (CPalThread *) arg;

    // When pthread_exit calls us, it has already removed the PAL thread
    // from TLS.  Since InternalEndCurrentThread calls functions that assert
    // that the current thread is known to this PAL, and that pThread
    // actually is the current PAL thread, put it back in TLS temporarily.
    pthread_setspecific(thObjKey, pThread);
    (void)PAL_Enter(PAL_BoundaryTop);
    
    /* Call entry point functions of every attached modules to
       indicate the thread is exiting */
    /* note : no need to enter a critical section for serialization, the loader 
       will lock its own critical section */
    LOADCallDllMain(DLL_THREAD_DETACH, NULL);

    // PAL_Leave will be called just before we release the thread reference
    // in InternalEndCurrentThread.
    InternalEndCurrentThread(pThread);
    pthread_setspecific(thObjKey, NULL);
}

/*++
Function:
  TLSInitialize

  Initialize the TLS subsystem
--*/
BOOL TLSInitialize()
{
    /* Create the pthread key for thread objects, which we use
       for fast access to the current thread object. */
    if (pthread_key_create(&thObjKey, InternalEndCurrentThreadWrapper))
    {
        ERROR("Couldn't create the thread object key\n");
        return FALSE;
    }

    SPINLOCKInit(&free_threads_spinlock);

    return TRUE;
}

/*++
Function:
    TLSCleanup

    Shutdown the TLS subsystem
--*/
VOID TLSCleanup()
{
    SPINLOCKDestroy(&free_threads_spinlock);
}

/*++
Function:
    AllocTHREAD

Abstract:
    Allocate CPalThread instance
  
Return:
    The fresh thread structure, NULL otherwise
--*/
CPalThread* AllocTHREAD(CPalThread *pthr)
{
    CPalThread* pThread = NULL;

    /* Get the lock */
    SPINLOCKAcquire(&free_threads_spinlock, 0);

    pThread = free_threads_list;
    if (pThread != NULL)
    {
        free_threads_list = pThread->GetNext();
    }

    /* Release the lock */
    SPINLOCKRelease(&free_threads_spinlock);

    if (pThread == NULL)
    {
        if(pthr != NULL)
        {
            pThread = InternalNew<CPalThread>(pthr);
        }
        else
        {
#ifdef FEATURE_PAL_SXS
            // When we reach this point, this thread has presumably wandered in
            // and is creating a CPalThread instance for itself.  In other words,
            // the current thread is not registered in the PAL thread list, and
            // therefore, we will not try to suspend it.  This in turn means
            // that it's okay to use the system's "new", as opposed to our "new",
            // whose purpose is to disallow thread suspension while in malloc.
#else // FEATURE_PAL_SXS
            // do not use the overloaded new in malloc.cpp since thread data isn't initialized.
            _ASSERT_MSG(!PALIsThreadDataInitialized(), "Thread data was initialized but NULL was passed in as a reference to the current thread.\n");
#endif // FEATURE_PAL_SXS
            pThread = InternalNew<CPalThread>(NULL);
        }
    }
    else
    {
        pThread = new (pThread) CPalThread;
    }

    return pThread;
}

/*++
Function:
    FreeTHREAD

Abstract:
    Free THREAD structure
  
--*/
static void FreeTHREAD(CPalThread *pThread)
{
    //
    // Run the destructors for this object
    //

    pThread->~CPalThread();

#ifdef _DEBUG
    // Fill value so we can find code re-using threads after they're dead. We
    // check against pThread->dwGuard when getting the current thread's data.
    memset((void*)pThread, 0xcc, sizeof(*pThread));
#endif
    
    // We SHOULD be doing the following, but it causes massive problems. See the 
    // comment below.
    //pthread_setspecific(thObjKey, NULL); // Make sure any TLS entry is removed.

    //
    // Never actually free the THREAD structure to make the TLS lookaside cache work. 
    // THREAD* for terminated thread can be stuck in the lookaside cache code for an 
    // arbitrary amount of time. The unused THREAD* structures has to remain in a 
    // valid memory and thus can't be returned to the heap.
    //
    // TODO: is this really true? Why would the entry remain in the cache for
    // an indefinite period of time after we've flushed it?
    //

    /* NOTE: can't use a CRITICAL_SECTION here: EnterCriticalSection(&cs,TRUE) and
       LeaveCriticalSection(&cs,TRUE) need to access the thread private data 
       stored in the very THREAD structure that we just destroyed. Entering and 
       leaving the critical section with internal==FALSE leads to possible hangs
       in the PROCSuspendOtherThreads logic, at shutdown time */

    /* Get the lock */
    SPINLOCKAcquire(&free_threads_spinlock, 0);

    pThread->SetNext(free_threads_list);
    free_threads_list = pThread;

    /* Release the lock */
    SPINLOCKRelease(&free_threads_spinlock);
}


/*++
Function:
  THREADGetThreadProcessId

returns the process owner ID of the indicated hThread
--*/
DWORD 
THREADGetThreadProcessId(
    HANDLE hThread
    // UNIXTODO Should take pThread parameter here (modify callers)
    )
{
    CPalThread *pThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
    PAL_ERROR palError = NO_ERROR;
    
    DWORD dwProcessId = 0;

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        0,
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        if (!pThread->IsDummy())
        {
            dwProcessId = GetCurrentProcessId();
        }
        else
        {
            ASSERT("Dummy thread passed to THREADGetProcessId\n");
        }

        if (NULL != pobjThread)
        {
            pobjThread->ReleaseReference(pThread);
        }
    }
    else
    {
        ERROR("Couldn't retreive the hThread:%p pid owner !\n", hThread);
    }

    
    return dwProcessId;
}

/*++
Function:
  GetCurrentThreadId

See MSDN doc.
--*/
DWORD
PALAPI
GetCurrentThreadId(
            VOID)
{
    DWORD dwThreadId;

    PERF_ENTRY(GetCurrentThreadId);
    ENTRY("GetCurrentThreadId()\n");

    //
    // TODO: should do perf test to see how this compares
    // with calling InternalGetCurrentThread (i.e., is our lookaside
    // cache faster on average than pthread_self?)
    //
    
    SIZE_T threadId = THREADSilentGetCurrentThreadId();
    dwThreadId = threadId;
    
    LOGEXIT("GetCurrentThreadId returns DWORD %#x\n", dwThreadId);    
    PERF_EXIT(GetCurrentThreadId);
    
    return dwThreadId;
}



/*++
Function:
  GetCurrentThread

See MSDN doc.
--*/
HANDLE
PALAPI
PAL_GetCurrentThread(
          VOID)
{
    PERF_ENTRY(GetCurrentThread);
    ENTRY("GetCurrentThread()\n");
    
    LOGEXIT("GetCurrentThread returns HANDLE %p\n", hPseudoCurrentThread);
    PERF_EXIT(GetCurrentThread);

    /* return a pseudo handle */
    return (HANDLE) hPseudoCurrentThread;
}

/*++
Function:
  SwitchToThread

See MSDN doc.
--*/
BOOL
PALAPI
SwitchToThread(
    VOID)
{
    BOOL ret;

    PERF_ENTRY(SwitchToThread);
    ENTRY("SwitchToThread(VOID)\n");

    /* sched_yield yields to another thread in the current process. This implementation 
       won't work well for cross-process synchronization. */
    ret = (sched_yield() == 0);

    LOGEXIT("SwitchToThread returns BOOL %d\n", ret);
    PERF_EXIT(SwitchToThread);

    return ret;
}

/*++
Function:
  CreateThread

Note:
  lpThreadAttributes could be ignored.

See MSDN doc.

--*/
HANDLE
PALAPI
CreateThread(
    IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
    IN DWORD dwStackSize,
    IN LPTHREAD_START_ROUTINE lpStartAddress,
    IN LPVOID lpParameter,
    IN DWORD dwCreationFlags,
    OUT LPDWORD lpThreadId)
{
    PAL_ERROR palError;
    CPalThread *pThread;
    HANDLE hNewThread = NULL;
    
    PERF_ENTRY(CreateThread);
    ENTRY("CreateThread(lpThreadAttr=%p, dwStackSize=%u, lpStartAddress=%p, "
          "lpParameter=%p, dwFlags=%#x, lpThreadId=%#x)\n",
          lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter,
          dwCreationFlags, lpThreadId);

    pThread = InternalGetCurrentThread();

    palError = InternalCreateThread(
        pThread,
        lpThreadAttributes,
        dwStackSize,
        lpStartAddress,
        lpParameter,
        dwCreationFlags,
        UserCreatedThread,
        lpThreadId,
        &hNewThread
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }    

    LOGEXIT("CreateThread returns HANDLE %p\n", hNewThread);
    PERF_EXIT(CreateThread);

    return hNewThread;
}

PAL_ERROR
CorUnix::InternalCreateThread(
    CPalThread *pThread,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    DWORD dwStackSize,
    LPTHREAD_START_ROUTINE lpStartAddress,
    LPVOID lpParameter,
    DWORD dwCreationFlags,
    PalThreadType eThreadType,
    LPDWORD lpThreadId,
    HANDLE *phThread
    )
{
    PAL_ERROR palError;
    CPalThread *pNewThread = NULL;
    CObjectAttributes oa;
    bool fAttributesInitialized = FALSE;
    bool fThreadDataAddedToProcessList = FALSE;
    HANDLE hNewThread = NULL;
    
    pthread_t pthread;
    pthread_attr_t pthreadAttr;
    size_t pthreadStackSize;
#if PTHREAD_CREATE_MODIFIES_ERRNO
    int storedErrno;
#endif  // PTHREAD_CREATE_MODIFIES_ERRNO
    BOOL fHoldingProcessLock = FALSE;
    int iError = 0;

    if(0 != terminator)
    {
        //
        // Since the PAL is in the middle of shutting down we don't want to
        // create any new threads (since it's possible for that new thread
        // to create another thread before the shutdown thread gets around
        // to suspending it, and so on). We don't want to return an error
        // here, though, as some programs (in particular, build) do not
        // handle CreateThread errors properly -- instead, we just put
        // the calling thread to sleep (unless it is the shutdown thread,
        // which could occur if a DllMain PROCESS_DETACH handler tried to
        // create a new thread for some odd reason).
        //
        
        ERROR("process is terminating, can't create new thread.\n");

        if (pThread->GetThreadId() != static_cast<DWORD>(terminator))
        {
            while (true)
            {
                poll(NULL, 0, INFTIM);
                sched_yield();
            }
        }
        else
        {
            //
            // This is the shutdown thread, so just return an error
            //
            
            palError = ERROR_PROCESS_ABORTED;
            goto EXIT;
        }
    }

    /* Validate parameters */

    if (lpThreadAttributes != NULL)
    {
        ASSERT("lpThreadAttributes parameter must be NULL (%p)\n", 
               lpThreadAttributes);
        palError = ERROR_INVALID_PARAMETER;
        goto EXIT;
    }
    
    // Ignore the STACK_SIZE_PARAM_IS_A_RESERVATION flag
    dwCreationFlags &= ~STACK_SIZE_PARAM_IS_A_RESERVATION;
    
    if ((dwCreationFlags != 0) && (dwCreationFlags != CREATE_SUSPENDED))
    {
        ASSERT("dwCreationFlags parameter is invalid (%#x)\n", dwCreationFlags);
        palError = ERROR_INVALID_PARAMETER;
        goto EXIT;
    }

    //
    // Create the CPalThread for the thread
    //

    pNewThread = AllocTHREAD(pThread);
    if (NULL == pNewThread)
    {
        palError = ERROR_OUTOFMEMORY;
        goto EXIT;
    }

    palError = pNewThread->RunPreCreateInitializers();
    if (NO_ERROR != palError)
    {
        goto EXIT;
    }

    pNewThread->m_lpStartAddress = lpStartAddress;
    pNewThread->m_lpStartParameter = lpParameter;
    pNewThread->m_bCreateSuspended = (dwCreationFlags & CREATE_SUSPENDED) == CREATE_SUSPENDED;
    pNewThread->m_eThreadType = eThreadType;

    if (0 != pthread_attr_init(&pthreadAttr))
    {
        ERROR("couldn't initialize pthread attributes\n");
        palError = ERROR_INTERNAL_ERROR;
        goto EXIT;        
    }

    fAttributesInitialized = TRUE;

    /* adjust the stack size if necessary */
    if (0 != pthread_attr_getstacksize(&pthreadAttr, &pthreadStackSize))
    {
        ERROR("couldn't set thread stack size\n");
        palError = ERROR_INTERNAL_ERROR;
        goto EXIT;        
    }

    TRACE("default pthread stack size is %d, caller requested %d (default is %d)\n",
          pthreadStackSize, dwStackSize, CPalThread::s_dwDefaultThreadStackSize);

    if (0 == dwStackSize)
    {
        dwStackSize = CPalThread::s_dwDefaultThreadStackSize;
    }

    if (PTHREAD_STACK_MIN > pthreadStackSize)
    {
        WARN("default stack size is reported as %d, but PTHREAD_STACK_MIN is "
             "%d\n", pthreadStackSize, PTHREAD_STACK_MIN);
    }
    
    if (pthreadStackSize < dwStackSize)
    {
        TRACE("setting thread stack size to %d\n", dwStackSize);
        if (0 != pthread_attr_setstacksize(&pthreadAttr, dwStackSize))
        {
            ERROR("couldn't set pthread stack size to %d\n", dwStackSize);
            palError = ERROR_INTERNAL_ERROR;
            goto EXIT;
        }
    }
    else
    {
        TRACE("using the system default thread stack size of %d\n", pthreadStackSize);
    }

#if HAVE_THREAD_SELF || HAVE__LWP_SELF
    /* Create new threads as "bound", so each pthread is permanently bound
       to an LWP.  Get/SetThreadContext() depend on this 1:1 mapping. */
    pthread_attr_setscope(&pthreadAttr, PTHREAD_SCOPE_SYSTEM);
#endif // HAVE_THREAD_SELF || HAVE__LWP_SELF

    //
    // We never call pthread_join, so create the new thread as detached
    //

    iError = pthread_attr_setdetachstate(&pthreadAttr, PTHREAD_CREATE_DETACHED);
    _ASSERTE(0 == iError);

    //
    // Create the IPalObject for the thread and store it in the object
    //

    palError = CreateThreadObject(
        pThread,
        pNewThread,
        &hNewThread);

    if (NO_ERROR != palError)
    {
        goto EXIT;
    }

    //
    // Add the thread to the process list
    //

    // 
    // We use the process lock to ensure that we're not interrupted
    // during the creation process. After adding the CPalThread reference
    // to the process list, we want to make sure the actual thread has been 
    // started. Otherwise, there's a window where the thread can be found
    // in the process list but doesn't yet exist in the system.
    //

    PROCProcessLock();
    fHoldingProcessLock = TRUE;

    PROCAddThread(pThread, pNewThread);
    fThreadDataAddedToProcessList = TRUE;

    //
    // Spawn the new pthread
    //

#if PTHREAD_CREATE_MODIFIES_ERRNO
    storedErrno = errno;
#endif  // PTHREAD_CREATE_MODIFIES_ERRNO

#ifdef FEATURE_PAL_SXS
    _ASSERT_MSG(pNewThread->IsInPal(), "New threads we're about to spawn should always be in the PAL.\n");
#endif // FEATURE_PAL_SXS
    iError = pthread_create(&pthread, &pthreadAttr, CPalThread::ThreadEntry, pNewThread);

#if PTHREAD_CREATE_MODIFIES_ERRNO
    if (iError == 0)
    {
        // Restore errno if pthread_create succeeded.
        errno = storedErrno;
    }
#endif  // PTHREAD_CREATE_MODIFIES_ERRNO

    if (0 != iError)
    {
        ERROR("pthread_create failed, error is %d (%s)\n", iError, strerror(iError));
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto EXIT;
    }
       
    //
    // Wait for the new thread to finish its initial startup tasks
    // (i.e., the ones that might fail)
    //
    if (pNewThread->WaitForStartStatus())
    {
        //
        // Everything succeeded. Store the handle for the new thread and
        // the thread's ID in the out params
        //
        *phThread = hNewThread;
        
        if (NULL != lpThreadId)
        {
            *lpThreadId = pNewThread->GetThreadId();
        }
    }
    else
    {
        ERROR("error occurred in THREADEntry, thread creation failed.\n");
        palError = ERROR_INTERNAL_ERROR;
        goto EXIT;
    }

    //
    // If we're here, then we've locked the process list and both pthread_create
    // and WaitForStartStatus succeeded. Thus, we can now unlock the process list.
    // Since palError == NO_ERROR, we won't call this again in the exit block.
    //
    PROCProcessUnlock();
    fHoldingProcessLock = FALSE;

EXIT:

    if (fAttributesInitialized)
    {
        if (0 != pthread_attr_destroy(&pthreadAttr))
        {
            WARN("pthread_attr_destroy() failed\n");
        }
    }

    if (NO_ERROR != palError)
    {
        //
        // We either were not able to create the new thread, or a failure
        // occurred in the new thread's entry routine. Free up the associated
        // resources here
        //

        if (fThreadDataAddedToProcessList)
        {
            PROCRemoveThread(pThread, pNewThread);
        }
        // 
        // Once we remove the thread from the process list, we can call
        // PROCProcessUnlock.
        //
        if (fHoldingProcessLock)
        {
            PROCProcessUnlock();
        }
        fHoldingProcessLock = FALSE;
    }

    _ASSERT_MSG(!fHoldingProcessLock, "Exiting InternalCreateThread while still holding the process critical section.\n");

    return palError;
}



/*++
Function:
  ExitThread

See MSDN doc.
--*/
PAL_NORETURN
VOID
PALAPI
ExitThread(
       IN DWORD dwExitCode)
{
    CPalThread *pThread;
      
    ENTRY("ExitThread(dwExitCode=%u)\n", dwExitCode);
    PERF_ENTRY_ONLY(ExitThread);

    pThread = InternalGetCurrentThread();

    /* store the exit code */
    pThread->SetExitCode(dwExitCode);

    /* pthread_exit runs TLS destructors and cleanup routines,
       possibly registered by foreign code.  The right thing
       to do here is to leave the PAL.  Our own TLS destructor
       re-enters us explicitly. */
    PAL_Leave(PAL_BoundaryTop);

    /* kill the thread (itself), resulting in a call to InternalEndCurrentThread */
    pthread_exit(NULL);
    
    ASSERT("pthread_exit should not return!\n");
    for (;;);
}

/*++
Function:
  GetExitCodeThread

See MSDN doc.
--*/
BOOL
PALAPI
GetExitCodeThread(
           IN HANDLE hThread,
           IN LPDWORD lpExitCode)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthrCurrent = NULL;
    CPalThread *pthrTarget = NULL;
    IPalObject *pobjThread = NULL;
    BOOL fExitCodeSet;

    PERF_ENTRY(GetExitCodeThread);
    ENTRY("GetExitCodeThread(hThread = %p, lpExitCode = %p)\n",
          hThread, lpExitCode);

    if (NULL == lpExitCode)
    {
        WARN("Got NULL lpExitCode\n");
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    pthrCurrent = InternalGetCurrentThread();
    palError = InternalGetThreadDataFromHandle(
        pthrCurrent,
        hThread,
        0,
        &pthrTarget,
        &pobjThread
        );

    pthrTarget->Lock(pthrCurrent);

    fExitCodeSet = pthrTarget->GetExitCode(lpExitCode);
    if (!fExitCodeSet)
    {
        if (TS_DONE == pthrTarget->synchronizationInfo.GetThreadState())
        {
#ifdef FEATURE_PAL_SXS
            // The thread exited without ever calling ExitThread.
            // It must have wandered in.
            *lpExitCode = 0;
#else // FEATURE_PAL_SXS
            ASSERT("exit code not set but thread is dead\n");
#endif // FEATURE_PAL_SXS
        }
        else
        {
            *lpExitCode = STILL_ACTIVE;
        }
    }

    pthrTarget->Unlock(pthrCurrent);

done:
    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pthrCurrent);
    }

    LOGEXIT("GetExitCodeThread returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(GetExitCodeThread);
    
    return NO_ERROR == palError;
}


/*++
Function:
  InternalEndCurrentThread

Does any necessary memory clean up, signals waiting threads, and then forces
the current thread to exit.
--*/

VOID
CorUnix::InternalEndCurrentThread(
    CPalThread *pThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    ISynchStateController *pSynchStateController = NULL;
    
#ifdef PAL_PERF
    PERFDisableThreadProfile(UserCreatedThread != pThread->GetThreadType());
#endif

    //
    // Abandon any objects owned by this thread
    //

    palError = g_pSynchronizationManager->AbandonObjectsOwnedByThread(
        pThread,
        pThread
        );

    if (NO_ERROR != palError)
    {
        ERROR("Failure abandoning owned objects");
    }

    //
    // Need to synchronize setting the thread state to TS_DONE since 
    // this is checked for in InternalSuspendThreadFromData.
    //

    pThread->suspensionInfo.AcquireSuspensionLock(pThread);
    IncrementEndingThreadCount();
    pThread->synchronizationInfo.SetThreadState(TS_DONE);
    pThread->suspensionInfo.ReleaseSuspensionLock(pThread);

    //
    // Mark the thread object as signaled
    //

    palError = pThread->GetThreadObject()->GetSynchStateController(
        pThread,
        &pSynchStateController
        );

    if (NO_ERROR == palError)
    {
        palError = pSynchStateController->SetSignalCount(1);
        if (NO_ERROR != palError)
        {
            ASSERT("Unable to mark thread object as signaled");
        }

        pSynchStateController->ReleaseController();
    }
    else
    {
        ASSERT("Unable to obtain state controller for thread");
    }

#ifndef FEATURE_PAL_SXS
    // If this is the last thread then delete the process' data,
    // but don't exit because the application hosting the PAL
    // might have its own threads.
    if (PROCGetNumberOfThreads() == 1)
    {
        TRACE("Last thread is exiting\n");
        DecrementEndingThreadCount();
        TerminateCurrentProcessNoExit(FALSE);
    }
    else
#endif // !FEATURE_PAL_SXS
    {
        /* Do this ONLY if we aren't the last thread -> otherwise
           it gets done by TerminateProcess->
           PROCCleanupProcess->PALShutdown->PAL_Terminate */

        //
        // Add a reference to the thread data before releasing the
        // thread object, so we can still use it
        //

        pThread->AddThreadReference();

        //
        // Release the reference to the IPalObject for this thread
        //

        pThread->GetThreadObject()->ReleaseReference(pThread);

        /* Remove thread for the thread list of the process 
           (don't do if this is the last thread -> gets handled by
            TerminateProcess->PROCCleanupProcess->PROCTerminateOtherThreads) */
        
        PROCRemoveThread(pThread, pThread);

#ifdef FEATURE_PAL_SXS
        // Ensure that EH is disabled on the current thread
        SEHDisable(pThread);
        PAL_Leave(PAL_BoundaryTop);
#endif // FEATURE_PAL_SXS
        
        
        //
        // Now release our reference to the thread data. We cannot touch
        // it after this point
        //

        pThread->ReleaseThreadReference();
        DecrementEndingThreadCount();
        
    }
}

/*++
Function:
  GetThreadPriority

See MSDN doc.
--*/
int
PALAPI
GetThreadPriority(
          IN HANDLE hThread)
{
    CPalThread *pThread;
    PAL_ERROR palError;
    int iPriority = THREAD_PRIORITY_ERROR_RETURN;
    
    PERF_ENTRY(GetThreadPriority);
    ENTRY("GetThreadPriority(hThread=%p)\n", hThread);

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadPriority(
        pThread,
        hThread,
        &iPriority
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("GetThreadPriorityExit returns int %d\n", iPriority);
    PERF_EXIT(GetThreadPriority);
    
    return iPriority;
}

PAL_ERROR
CorUnix::InternalGetThreadPriority(
    CPalThread *pThread,
    HANDLE hThread,
    int *piPriority
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
    
    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        0,  // THREAD_QUERY_INFORMATION
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        goto InternalGetThreadPriorityExit;
    }
    
    pTargetThread->Lock(pThread);

    *piPriority = pTargetThread->GetThreadPriority();

    pTargetThread->Unlock(pThread);

InternalGetThreadPriorityExit:

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    return palError;
}


/*++
Function:
  SetThreadPriority

See MSDN doc.
--*/
BOOL
PALAPI
SetThreadPriority(
          IN HANDLE hThread,
          IN int nPriority)
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;
    
    PERF_ENTRY(SetThreadPriority);
    ENTRY("SetThreadPriority(hThread=%p, nPriority=%#x)\n", hThread, nPriority);

    pThread = InternalGetCurrentThread();

    palError = InternalSetThreadPriority(
        pThread,
        hThread,
        nPriority
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("SetThreadPriority returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(SetThreadPriority);
    
    return NO_ERROR == palError;
}

PAL_ERROR
CorUnix::InternalSetThreadPriority(
    CPalThread *pThread,
    HANDLE hTargetThread,
    int iNewPriority
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pTargetThread = NULL;
    IPalObject *pobjThread = NULL;
    
    int policy;
    struct sched_param schedParam;
    int max_priority;
    int min_priority;
    float posix_priority;


    palError = InternalGetThreadDataFromHandle(
        pThread,
        hTargetThread,
        0, // THREAD_SET_INFORMATION
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetThreadPriorityExit;
    }
        
    pTargetThread->Lock(pThread);

    /* validate the requested priority */
    switch (iNewPriority)
    {
    case THREAD_PRIORITY_TIME_CRITICAL: /* fall through */
    case THREAD_PRIORITY_IDLE:
        break;

    case THREAD_PRIORITY_HIGHEST:       /* fall through */
    case THREAD_PRIORITY_ABOVE_NORMAL:  /* fall through */
    case THREAD_PRIORITY_NORMAL:        /* fall through */
    case THREAD_PRIORITY_BELOW_NORMAL:  /* fall through */
    case THREAD_PRIORITY_LOWEST:        
#if PAL_IGNORE_NORMAL_THREAD_PRIORITY
        /* We aren't going to set the thread priority. Just record what it is,
           and exit */
        pTargetThread->m_iThreadPriority = iNewPriority;
        goto InternalSetThreadPriorityExit;        
#endif
        break;

    default:
        ASSERT("Priority %d not supported\n", iNewPriority);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalSetThreadPriorityExit;
    }  

    /* check if the thread is still running */
    if (TS_DONE == pTargetThread->synchronizationInfo.GetThreadState())
    {
        /* the thread has exited, set the priority in the thread structure 
           and exit */
        pTargetThread->m_iThreadPriority = iNewPriority;
        goto InternalSetThreadPriorityExit;        
    }

    /* get the previous thread schedule parameters.  We need to know the 
       scheduling policy to determine the priority range */
    if (pthread_getschedparam(
            (pthread_t) pTargetThread->GetThreadId(),
            &policy, 
            &schedParam
            ) != 0)
    {
        ASSERT("Unable to get current thread scheduling information\n");
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadPriorityExit;
    }

#if HAVE_SCHED_GET_PRIORITY
    max_priority = sched_get_priority_max(policy);
    min_priority = sched_get_priority_min(policy);
    if( -1 == max_priority || -1 == min_priority)
    {
        ASSERT("sched_get_priority_min/max failed; error is %d (%s)\n", 
               errno, strerror(errno));
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadPriorityExit;
    }
#else
    max_priority = PAL_THREAD_PRIORITY_MAX;
    min_priority = PAL_THREAD_PRIORITY_MIN;
#endif

    TRACE("Pthread priorities for policy %d must be in the range %d to %d\n", 
          policy, min_priority, max_priority);

    /* explanation for fancy maths below :
       POSIX doesn't specify the range of thread priorities that can be used 
       with pthread_setschedparam. Instead, one must use sched_get_priority_min
       and sched_get_priority_max to obtain the lower and upper bounds of this
       range. Since the PAL also uses a range of values (from Idle [-15] to 
       Time Critical [+15]), we have to do a mapping from a known range to an 
       unknown (at compilation) range. 
       We do this by :
       -substracting the minimal PAL priority from the desired priority. this 
        gives a value between 0 and the PAL priority range
       -dividing this value by the PAL priority range. this allows us to 
        express the desired priority as a floating-point value between 0 and 1
       -multiplying this value by the PTHREAD priority range. This gives a 
        value between 0 and the PTHREAD priority range
       -adding the minimal PTHREAD priority range. This will give us a value 
        between the minimal and maximla pthread priority, which should be 
        equivalent to the original PAL value. 
        
        example : suppose a pthread range 100 to 200, and a desired priority 
                  of 0 (halfway between PAL minimum and maximum)
            0 - (IDLE [-15]) = 15
            15 / (TIMECRITICAL[15] - IDLE[-15]) = 0.5
            0.5 * (pthreadmax[200]-pthreadmin[100]) = 50
            50 + pthreadmin[100] = 150 -> halfway between pthread min and max
    */
    posix_priority =  (iNewPriority - THREAD_PRIORITY_IDLE);
    posix_priority /= (THREAD_PRIORITY_TIME_CRITICAL - THREAD_PRIORITY_IDLE);
    posix_priority *= (max_priority-min_priority);
    posix_priority += min_priority;

    schedParam.sched_priority = (int)posix_priority;
    
    TRACE("PAL priority %d is mapped to pthread priority %d\n",
          iNewPriority, schedParam.sched_priority);

    /* Finally, set the new priority into place */
    if (pthread_setschedparam(
            (pthread_t) pTargetThread->GetThreadId(),
            policy,
            &schedParam
            ) != 0)
    {
#if SET_SCHEDPARAM_NEEDS_PRIVS
        if (EPERM == errno)
        {
            // UNIXTODO: Should log a warning to the event log
            TRACE("Caller does not have OS privileges to call pthread_setschedparam\n");
            pTargetThread->m_iThreadPriority = iNewPriority;
            goto InternalSetThreadPriorityExit;
        }
#endif
        
        ASSERT("Unable to set thread priority (errno %d)\n", errno);
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadPriorityExit;
    }
    
    pTargetThread->m_iThreadPriority = iNewPriority;

InternalSetThreadPriorityExit:

    if (NULL != pTargetThread)
    {
        pTargetThread->Unlock(pThread);
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    return palError;    
}

/*++
Function:
  GetThreadTimes

See MSDN doc.
--*/
BOOL
PALAPI
GetThreadTimes(
        IN HANDLE hThread,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpExitTime,
        OUT LPFILETIME lpKernelTime,
        OUT LPFILETIME lpUserTime)
{
    PERF_ENTRY(GetThreadTimes);
    ENTRY("GetThreadTimes(hThread=%p, lpExitTime=%p, lpKernelTime=%p,"
          "lpUserTime=%p)\n",
          hThread, lpCreationTime, lpExitTime, lpKernelTime, lpUserTime );

    BOOL retval = FALSE;
    
#if HAVE_MACH_THREADS
    PAL_ERROR palError = NO_ERROR;
	CPalThread *pthrCurrent = NULL;
    CPalThread *pthrTarget = NULL;
    IPalObject *pobjThread = NULL;
    thread_basic_info resUsage;
    mach_msg_type_number_t resUsage_count = THREAD_BASIC_INFO_COUNT;
    __int64 calcTime;
    const __int64 SECS_TO_NS = 1000000000; /* 10^9 */
    const __int64 USECS_TO_NS = 1000;      /* 10^3 */

    pthrCurrent = InternalGetCurrentThread();
    palError = InternalGetThreadDataFromHandle(
        pthrCurrent,
        hThread,
        0,
        &pthrTarget,
        &pobjThread
        );
    
	if (palError != NO_ERROR)
    {
        ASSERT("Unable to get thread data from handle %p"
              "thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto GetThreadTimesExit;
	}	

    pthrTarget->Lock(pthrCurrent);
	
    mach_port_t mhThread;
    mhThread = pthread_mach_thread_np((pthread_t)pthrTarget->GetThreadId());
	
	kern_return_t status;
	status = thread_info(
	    mhThread, 
		THREAD_BASIC_INFO, 
		(thread_info_t)&resUsage, 
		&resUsage_count);
	
    if (status != KERN_SUCCESS)
	{
        ASSERT("Unable to get resource usage information for the current "
              "thread\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto GetThreadTimesExit;
    }
    
    TRACE ("thread_info User: %ld sec,%ld microsec. Kernel: %ld sec,%ld"
           " microsec\n",
           resUsage.user_time.seconds, resUsage.user_time.microseconds,
           resUsage.system_time.seconds, resUsage.system_time.microseconds);

    if (lpUserTime)
    {
        /* Get the time of user mode execution, in 100s of nanoseconds */
        calcTime = (__int64)resUsage.user_time.seconds * SECS_TO_NS;
        calcTime += (__int64)resUsage.user_time.microseconds * USECS_TO_NS;
        calcTime /= 100; /* Produce the time in 100s of ns */
        /* Assign the time into lpUserTime */
        lpUserTime->dwLowDateTime = (DWORD)calcTime;
        lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);
    }

    if (lpKernelTime)
    {
        /* Get the time of kernel mode execution, in 100s of nanoseconds */
        calcTime = (__int64)resUsage.system_time.seconds * SECS_TO_NS;
        calcTime += (__int64)resUsage.system_time.microseconds * USECS_TO_NS;
        calcTime /= 100; /* Produce the time in 100s of ns */
        /* Assign the time into lpUserTime */
        lpKernelTime->dwLowDateTime = (DWORD)calcTime;
        lpKernelTime->dwHighDateTime = (DWORD)(calcTime >> 32);
    }
    
    pthrTarget->Unlock(pthrCurrent);

    retval = TRUE;

GetThreadTimesExit:

#else // HAVE_MACH_THREADS
    // UNIXTODO: Implement this
    lpCreationTime->dwLowDateTime = 0;
    lpCreationTime->dwHighDateTime = 0;
    
    lpExitTime->dwLowDateTime = 0;
    lpExitTime->dwHighDateTime = 0;
    
    lpUserTime->dwLowDateTime = 0;
    lpUserTime->dwHighDateTime = 0;
    
    lpKernelTime->dwLowDateTime = 0;
    lpKernelTime->dwHighDateTime = 0;
    retval = TRUE;
#endif // HAVE_MACH_THREADS
    
    LOGEXIT("GetThreadTimes returns BOOL %d\n", retval);
    PERF_EXIT(GetThreadTimes);
    return (retval);
}



void *
CPalThread::ThreadEntry(
    void *pvParam
    )
{
    PAL_ERROR palError;
    CPalThread *pThread;
    PTHREAD_START_ROUTINE pfnStartRoutine;
    LPVOID pvPar;
    DWORD retValue;

    pThread = reinterpret_cast<CPalThread*>(pvParam);

    if(NULL == pThread)
    {
        ASSERT("THREAD pointer is NULL!\n");
        goto fail;
    }

#if defined(FEATURE_PAL_SXS) && defined(_DEBUG)
    // We cannot assert yet, as we haven't set in this thread into the TLS, and so __ASSERT_ENTER
    // will fail if the assert fails and we'll crash.
    //_ASSERT_MSG(pThread->m_fInPal == 1, "New threads should always be in the PAL upon ThreadEntry.\n");
    if (g_Dbg_asserts_enabled && pThread->m_fInPal != 1)
        DebugBreak();
#endif // FEATURE_PAL_SXS && _DEBUG

    pThread->m_threadId = (SIZE_T) pthread_self();
#if HAVE_THREAD_SELF
    pThread->m_dwLwpId = (DWORD) thread_self();
#elif HAVE__LWP_SELF
    pThread->m_dwLwpId = (DWORD) _lwp_self();
#else
    pThread->m_dwLwpId = 0;
#endif

    palError = pThread->RunPostCreateInitializers();
    if (NO_ERROR != palError)
    {
        ASSERT("Error %i initializing thread data (post creation)\n", palError);
        goto fail;
    }

    // Check if the thread should be started suspended.
    if (pThread->GetCreateSuspended())
    {
        DWORD dwSuspendCount;
        
        palError = pThread->suspensionInfo.InternalSuspendThreadFromData(pThread, pThread, &dwSuspendCount);
        if (NO_ERROR != palError)
        {
            ASSERT("Error %i attempting to suspend new thread\n", palError);
            goto fail;
        }

        //
        // We need to run any APCs that have already been queued for
        // this thread.
        //

        (void) g_pSynchronizationManager->DispatchPendingAPCs(pThread);
    }
    else
    {
        //
        // All startup operations that might have failed have succeeded,
        // so thread creation is successful. Let CreateThread return.
        //

        pThread->SetStartStatus(TRUE);
    }

    pThread->synchronizationInfo.SetThreadState(TS_RUNNING);

    if (UserCreatedThread == pThread->GetThreadType())
    {
        /* Inform all loaded modules that a thread has been created */
        /* note : no need to take a critical section to serialize here; the loader 
           will take the module critical section */
        LOADCallDllMain(DLL_THREAD_ATTACH, NULL);
    }

#ifdef PAL_PERF
    PERFAllocThreadInfo();
    PERFEnableThreadProfile(UserCreatedThread != pThread->GetThreadType());
#endif

    /* call the startup routine */
    pfnStartRoutine = pThread->GetStartAddress();
    pvPar = pThread->GetStartParameter();

    retValue = (*pfnStartRoutine)(pvPar);

    TRACE("Thread exited (%u)\n", retValue);
    ExitThread(retValue);

    /* Note: never get here */ 
    ASSERT("ExitThread failed!\n");
    for (;;);

fail:

    //
    // Notify InternalCreateThread that a failure occurred
    //
    
    if (NULL != pThread)
    {
        pThread->synchronizationInfo.SetThreadState(TS_FAILED);
        pThread->SetStartStatus(FALSE);
    }

    /* do not call ExitThread : we don't want to call DllMain(), and the thread 
       isn't in a clean state (e.g. lpThread isn't in TLS). the cleanup work 
       above should release all resources */
    return NULL;
}


#define PAL_THREAD_DEFAULT_STACK_SIZE "PAL_THREAD_DEFAULT_STACK_SIZE"

PAL_ERROR
CorUnix::InitializeGlobalThreadData(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;
    char *pszStackSize = NULL;

    //
    // Read in the environment to see whether we need to change the default
    // thread stack size.
    //
    pszStackSize = MiscGetenv(PAL_THREAD_DEFAULT_STACK_SIZE);
    if (NULL != pszStackSize)
    {
        // Environment variable exists
        char *pszEnd;
        DWORD dw = PAL_strtoul(pszStackSize, &pszEnd, 16); // treat it as hex
        if ( (pszStackSize != pszEnd) && (0 != dw) )
        {
            CPalThread::s_dwDefaultThreadStackSize = dw;
        }
    }

#if !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION
    //
    // Initialize the thread suspension signal sets.
    //
    
    CThreadSuspensionInfo::InitializeSignalSets();
#endif // !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION

    return palError;
}


/*++
Function:
    CreateThreadData

Abstract:
    Create the CPalThread for the startup thread
    or another external thread entering the PAL
    for the first time
  
Parameters:
    ppThread - on success, receives the CPalThread

Return:
   PAL_ERROR
--*/

PAL_ERROR
CorUnix::CreateThreadData(
    CPalThread **ppThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread = NULL;
    
    /* Create the thread object */
    /* Passing NULL to AllocTHREAD since there is no thread reference to pass in. */
    pThread = AllocTHREAD(NULL);

    if (NULL == pThread)
    {
       palError = ERROR_OUTOFMEMORY;
       goto CreateThreadDataExit;
    }

    palError = pThread->RunPreCreateInitializers();

    if (NO_ERROR != palError)
    {
        goto CreateThreadDataExit;
    }
    
    pThread->SetLastError(StartupLastError);

    pThread->m_threadId = (SIZE_T) pthread_self();
#if HAVE_THREAD_SELF
    pThread->m_dwLwpId = (DWORD) thread_self();
#elif HAVE__LWP_SELF
    pThread->m_dwLwpId = (DWORD) _lwp_self();
#else
    pThread->m_dwLwpId = 0;
#endif

    palError = pThread->RunPostCreateInitializers();
    if (NO_ERROR != palError)
    {
        goto CreateThreadDataExit;
    }

    *ppThread = pThread;
    
CreateThreadDataExit:

    if (NO_ERROR != palError)
    {
        if (NULL != pThread)
        {
            pThread->ReleaseThreadReference();
        }
    }

    return palError;
}

/*++
Function:
    CreateThreadObject

Abstract:
    Creates the IPalObject for a thread, storing
    the reference in the CPalThread
  
Parameters:
    pThread - the thread data for the creating thread
    pNewThread - the thread data for the thread being initialized

Return:
   PAL_ERROR
--*/

PAL_ERROR
CorUnix::CreateThreadObject(
    CPalThread *pThread,
    CPalThread *pNewThread,
    HANDLE *phThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjThread = NULL;
    IDataLock *pDataLock;
    HANDLE hThread = NULL;
    CThreadProcessLocalData *pLocalData = NULL;
    CObjectAttributes oa;
    BOOL fThreadDataStoredInObject = FALSE;
    IPalObject *pobjRegisteredThread = NULL;

    //
    // Create the IPalObject for the thread
    //

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otThread,
        &oa,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        goto CreateThreadObjectExit;
    }

    //
    // Store the CPalThread inside of the IPalObject
    //

    palError = pobjThread->GetProcessLocalData(
        pThread,
        WriteLock, 
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto CreateThreadObjectExit;
    }

    pLocalData->pThread = pNewThread;
    pDataLock->ReleaseLock(pThread, TRUE);
    fThreadDataStoredInObject = TRUE;

    //
    // Register the IPalObject (obtaining a handle)
    //

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pobjThread,
        &aotThread,
        0, //THREAD_ALL_ACCESS,
        &hThread,
        &pobjRegisteredThread
        );
        
    //
    // pobjThread is invalidated by the call to RegisterObject, so NULL
    // it out here to prevent it from being released
    //

    pobjThread = NULL;

    if (NO_ERROR != palError)
    {
        goto CreateThreadObjectExit;
    }

    //
    // Store the registered object inside of the thread object,
    // adding a reference for the thread itself
    //

    pNewThread->m_pThreadObject = pobjRegisteredThread;
    pNewThread->m_pThreadObject->AddReference();

    *phThread = hThread;

CreateThreadObjectExit:

    if (NO_ERROR != palError)
    {
        if (NULL != hThread)
        {
            g_pObjectManager->RevokeHandle(pThread, hThread);
        }

        if (NULL != pNewThread->m_pThreadObject)
        {
            //
            // Release the new thread's reference on the underlying thread
            // object
            //

            pNewThread->m_pThreadObject->ReleaseReference(pThread);
        }

        if (!fThreadDataStoredInObject)
        {
            //
            // The CPalThread for the new thread was never stored in 
            // an IPalObject instance, so we need to release the initial
            // reference here. (If it has been stored it will get freed in
            // the owning object's cleanup routine)
            //

            pNewThread->ReleaseThreadReference();            
        }
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    if (NULL != pobjRegisteredThread)
    {
        pobjRegisteredThread->ReleaseReference(pThread);
    }

    return palError;
}

PAL_ERROR
CorUnix::InternalCreateDummyThread(
    CPalThread *pThread,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    CPalThread **ppDummyThread,
    HANDLE *phThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pDummyThread = NULL;
    IPalObject *pobjThread = NULL;
    IPalObject *pobjThreadRegistered = NULL;
    IDataLock *pDataLock;
    CThreadProcessLocalData *pLocalData;
    CObjectAttributes oa(NULL, lpThreadAttributes);
    bool fThreadDataStoredInObject = FALSE;

    pDummyThread = AllocTHREAD(pThread);
    if (NULL == pDummyThread)
    {
        palError = ERROR_OUTOFMEMORY;
        goto InternalCreateDummyThreadExit;
    }

    pDummyThread->m_fIsDummy = TRUE;

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otThread,
        &oa,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreateDummyThreadExit;
    }

    palError = pobjThread->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreateDummyThreadExit;
    }

    pLocalData->pThread = pDummyThread;
    pDataLock->ReleaseLock(pThread, TRUE);
    fThreadDataStoredInObject = TRUE;

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pobjThread,
        &aotThread,
        0, // THREAD_ALL_ACCESS
        phThread,
        &pobjThreadRegistered
        );

    //
    // pobjThread is invalidated by the above call, so NULL
    // it out here
    //
    
    pobjThread = NULL;

    if (NO_ERROR != palError)
    {
        goto InternalCreateDummyThreadExit;
    }

    //
    // Note the we do NOT store the registered object for the
    // thread w/in pDummyThread. Since this thread is not actually
    // executing that reference would never be released (and thus
    // the thread object would never be cleaned up...)
    //

    *ppDummyThread = pDummyThread;

InternalCreateDummyThreadExit:

    if (NULL != pobjThreadRegistered)
    {
        pobjThreadRegistered->ReleaseReference(pThread);
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    if (NO_ERROR != palError
        && NULL != pDummyThread
        && !fThreadDataStoredInObject)
    {
        pDummyThread->ReleaseThreadReference();
    }

    return palError;
}

PAL_ERROR
CorUnix::InternalGetThreadDataFromHandle(
    CPalThread *pThread,
    HANDLE hThread,
    DWORD dwRightsRequired,
    CPalThread **ppTargetThread,
    IPalObject **ppobjThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobj;
    IDataLock *pLock;
    CThreadProcessLocalData *pData;

    *ppobjThread = NULL;

    if (hPseudoCurrentThread == hThread)
    {
        *ppTargetThread = pThread;
    }
    else
    {
        palError = g_pObjectManager->ReferenceObjectByHandle(
            pThread,
            hThread,
            &aotThread,
            dwRightsRequired,
            &pobj
            );

        if (NO_ERROR == palError)
        {
            palError = pobj->GetProcessLocalData(
                pThread,
                ReadLock,
                &pLock,
                reinterpret_cast<void**>(&pData)
                );

            if (NO_ERROR == palError)
            {
                *ppTargetThread = pData->pThread;
                pLock->ReleaseLock(pThread, FALSE);

                //
                // Transfer object reference to out param
                //

                *ppobjThread = pobj;                
            }
            else
            {
                pobj->ReleaseReference(pThread);
            }
        }
    }

    return palError;
}

PAL_ERROR
CPalThread::RunPreCreateInitializers(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;
    int iError;

    //
    // First, perform initialization of CPalThread private members
    //

    InternalInitializeCriticalSection(&m_csLock);
    m_fLockInitialized = TRUE;

    iError = pthread_mutex_init(&m_startMutex, NULL);
    if (0 != iError)
    {
        goto RunPreCreateInitializersExit;
    }

    iError = pthread_cond_init(&m_startCond, NULL);
    if (0 != iError)
    {
        pthread_mutex_destroy(&m_startMutex);
        goto RunPreCreateInitializersExit;
    }

    m_fStartItemsInitialized = TRUE;

    //
    // Call the pre-create initializers for embedded classes
    //

    palError = synchronizationInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

    palError = suspensionInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

    palError = sehInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

    palError = tlsInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

    palError = apcInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

    palError = crtInfo.InitializePreCreate();
    if (NO_ERROR != palError)
    {
        goto RunPreCreateInitializersExit;
    }

RunPreCreateInitializersExit:

    return palError;
}

CPalThread::~CPalThread()
{
    // @UNIXTODO: This is our last chance to unlink our Mach exception handler from the pseudo-chain we're trying
    // to maintain. Unfortunately we don't have enough data or control to do this at all well (and we can't
    // guarantee that another component hasn't chained to us, about which we can do nothing). If the kernel or
    // another component forwards an exception notification to us for this thread things will go badly (we'll
    // terminate the process when trying to look up this CPalThread in order to find forwarding information).
    // On the flip side I don't believe we'll get here currently unless the thread has been terminated (in
    // which case it's not an issue). If we start supporting unload or early disposal of CPalThread objects
    // (say when we return from an outer reverse p/invoke) then we'll need to revisit this. But hopefully by
    // then we'll have an alternative design for handling hardware exceptions.

    if (m_fLockInitialized)
    {
        InternalDeleteCriticalSection(&m_csLock);
    }

    if (m_fStartItemsInitialized)
    {
        int iError;
        
        iError = pthread_cond_destroy(&m_startCond);
        _ASSERTE(0 == iError);
        
        iError = pthread_mutex_destroy(&m_startMutex);
        _ASSERTE(0 == iError);
    }
}

void
CPalThread::AddThreadReference(
    void
    )
{
    InterlockedIncrement(&m_lRefCount);
}

void
CPalThread::ReleaseThreadReference(
    void
    )
{
    LONG lRefCount = InterlockedDecrement(&m_lRefCount);
    _ASSERT_MSG(lRefCount >= 0, "Released a thread and ended with a negative refcount (%ld)\n", lRefCount);
    if (0 == lRefCount)
    {
        FreeTHREAD(this);
    }
    
}

PAL_ERROR
CPalThread::RunPostCreateInitializers(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;

    //
    // Call the post-create initializers for embedded classes
    //

    palError = synchronizationInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = suspensionInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = sehInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = tlsInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = apcInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = crtInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

#ifdef FEATURE_PAL_SXS
    _ASSERTE(m_fInPal);
    palError = SEHEnable(this);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }
#endif // FEATURE_PAL_SXS

RunPostCreateInitializersExit:

    return palError;
}

void
CPalThread::SetStartStatus(
    bool fStartSucceeded
    )
{
    int iError;

#if _DEBUG
    if (m_fStartStatusSet)
    {
        ASSERT("Multiple calls to CPalThread::SetStartStatus\n");
    }
#endif

    //
    // This routine may get called from two spots:
    // * CPalThread::ThreadEntry
    // * InternalSuspendThreadFromData
    //
    // No matter which path we're on if we've reached this point
    // there are no further thread suspensions that happen at
    // creation time, to reset m_bCreateSuspended to prevent
    // InternalSuspendThreadFromData from calling us again
    //

    m_bCreateSuspended = FALSE;

    iError = pthread_mutex_lock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primative failure\n");
        // bugcheck?
    }

    m_fStartStatus = fStartSucceeded;
    m_fStartStatusSet = TRUE;

    iError = pthread_cond_signal(&m_startCond);
    if (0 != iError)
    {
        ASSERT("pthread primative failure\n");
        // bugcheck?
    }

    iError = pthread_mutex_unlock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primative failure\n");
        // bugcheck?
    }
}

bool
CPalThread::WaitForStartStatus(
    void
    )
{
    int iError;

    iError = pthread_mutex_lock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primative failure\n");
        // bugcheck?
    }

    while (!m_fStartStatusSet)
    {
        iError = pthread_cond_wait(&m_startCond, &m_startMutex);
        if (0 != iError)
        {
            ASSERT("pthread primative failure\n");
            // bugcheck?
        }
    }

    iError = pthread_mutex_unlock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primative failure\n");
        // bugcheck?
    }

    return m_fStartStatus;
}

/* IncrementEndingThreadCount and DecrementEndingThreadCount are used
to control a global counter that indicates if any threads are about to die.
Once a thread's state is set to TS_DONE, it cannot be suspended. However,
the dying thread can still access PAL resources, which is dangerous if the
thread dies during PAL cleanup. To avoid this, the shutdown thread calls
WaitForEndingThreads after suspending all other threads. WaitForEndingThreads 
uses a condition variable along with the global counter to wait for remaining 
PAL threads to die before proceeding with cleanup. As threads die, they 
decrement the counter and signal the condition variable. */

void 
IncrementEndingThreadCount(
    void
    )
{
    int iError;

    iError = pthread_mutex_lock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_lock returned %d\n", iError);

    iEndingThreads++;

    iError = pthread_mutex_unlock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_unlock returned %d\n", iError);
}

void 
DecrementEndingThreadCount(
    void
    )
{
    int iError;

    iError = pthread_mutex_lock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_lock returned %d\n", iError);

    iEndingThreads--;
    _ASSERTE(iEndingThreads >= 0);

    if (iEndingThreads == 0)
    {
        iError = pthread_cond_signal(&ptcEndThread);
        _ASSERT_MSG(iError == 0, "pthread_cond_signal returned %d\n", iError);
    }

    iError = pthread_mutex_unlock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_unlock returned %d\n", iError);
}

void 
WaitForEndingThreads(
    void
    )
{
    int iError;

    iError = pthread_mutex_lock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_lock returned %d\n", iError);

    while (iEndingThreads > 0)
    {
        iError = pthread_cond_wait(&ptcEndThread, &ptmEndThread);
        _ASSERT_MSG(iError == 0, "pthread_cond_wait returned %d\n", iError);  
    }

    iError = pthread_mutex_unlock(&ptmEndThread);
    _ASSERT_MSG(iError == 0, "pthread_mutex_unlock returned %d\n", iError);
}

PAL_ERROR
CorUnix::InitializeEndingThreadsData(
    void
    )
{
    PAL_ERROR palError = ERROR_INTERNAL_ERROR;
    int iError;

    iError = pthread_mutex_init(&ptmEndThread, NULL);
    if (0 != iError)
    {
        goto InitializeEndingThreadsDataExit;
    }

    iError = pthread_cond_init(&ptcEndThread, NULL);
    if (0 != iError)
    {
        //
        // Don't bother checking the return value of pthread_mutex_destroy
        // since PAL initialization will now fail.
        //
        
        pthread_mutex_destroy(&ptmEndThread);
        goto InitializeEndingThreadsDataExit;
    }

    palError = NO_ERROR;

InitializeEndingThreadsDataExit:

    return palError;
}

void
ThreadCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown,
    bool fCleanupSharedState
    )
{
    CThreadProcessLocalData *pThreadData = NULL;
    CPalThread *pThreadToCleanup = NULL;
    IDataLock *pDataLock = NULL;
    PAL_ERROR palError = NO_ERROR;
        
    //
    // Free the CPalThread data for the passed in thread
    //

    palError = pObjectToCleanup->GetProcessLocalData(
        pThread,
        WriteLock, 
        &pDataLock,
        reinterpret_cast<void**>(&pThreadData)
        );

    if (NO_ERROR == palError)
    {
        //
        // Note that we may be cleaning up the data for the calling
        // thread (i.e., pThread == pThreadToCleanup), so the release
        // of the thread reference needs to be the last thing that
        // we do (though in that case it's very likely that the person
        // calling us will be holding an extra reference to allow
        // for the thread data to be available while the rest of the
        // object cleanup takes place).
        //
        
        pThreadToCleanup = pThreadData->pThread;
        pThreadData->pThread = NULL;
        pDataLock->ReleaseLock(pThread, TRUE);
        pThreadToCleanup->ReleaseThreadReference();
    }
    else
    {
        ASSERT("Unable to obtain thread data");
    }
    
}

PAL_ERROR
ThreadInitializationRoutine(
    CPalThread *pThread,
    CObjectType *pObjectType,
    void *pImmutableData,
    void *pSharedData,
    void *pProcessLocalData
    )
{
    return NO_ERROR;
}

void *
PALAPI
PAL_GetStackBase()
{
#ifdef _TARGET_MAC64
    // This is a Mac specific method
    return pthread_get_stackaddr_np(pthread_self());
#else
    pthread_attr_t attr;
    void* stackAddr;
    size_t stackSize;
    int status;
    
    pthread_t thread = pthread_self();
    
    status = pthread_getattr_np(thread, &attr);
    _ASSERT_MSG(status == 0, "pthread_getattr_np call failed");

    status = pthread_attr_getstack(&attr, &stackAddr, &stackSize);
    _ASSERT_MSG(status == 0, "pthread_attr_getstack call failed");

    return (void*)((size_t)stackAddr + stackSize);
#endif
}

void *
PALAPI
PAL_GetStackLimit()
{
#ifdef _TARGET_MAC64
    // This is a Mac specific method
    return ((BYTE *)pthread_get_stackaddr_np(pthread_self()) -
            pthread_get_stacksize_np(pthread_self()));
#else
    pthread_attr_t attr;
    void* stackAddr;
    size_t stackSize;
    int status;
    
    pthread_t thread = pthread_self();
    
    status = pthread_getattr_np(thread, &attr);
    _ASSERT_MSG(status == 0, "pthread_getattr_np call failed");

    status = pthread_attr_getstack(&attr, &stackAddr, &stackSize);
    _ASSERT_MSG(status == 0, "pthread_attr_getstack call failed");
    
    return stackAddr;
#endif
}

#if HAVE_MACH_EXCEPTIONS
extern mach_port_t s_ExceptionPort;
extern mach_port_t s_TopExceptionPort;

// Returns a pointer to the handler node that should be initialized next. The first time this is called for a
// thread the bottom node will be returned. Thereafter the top node will be returned. Also returns the Mach
// exception port that should be registered.
CorUnix::CThreadMachExceptionHandlerNode *CorUnix::CThreadMachExceptionHandlers::GetNodeForInitialization(mach_port_t *pExceptionPort)
{
    if (m_bottom.m_nPorts == -1)
    {
        // Thread hasn't registered handlers before. Return the bottom handler node and exception port.
        *pExceptionPort = s_ExceptionPort;
        return &m_bottom;
    }
    else
    {
        // Othewise use the top handler node and register the top exception port.
        *pExceptionPort = s_TopExceptionPort;
        return &m_top;
    }
}

// Get handler details for a given type of exception. If successful the structure pointed at by pHandler is
// filled in and true is returned. Otherwise false is returned. The fTopException argument indicates whether
// the handlers found at the time of a call to ICLRRuntimeHost2::RegisterMacEHPort() should be searched (if
// not, or a handler is not found there, we'll fallback to looking at the handlers discovered at the point
// when the CLR first saw this thread).
bool CorUnix::CThreadMachExceptionHandlers::GetHandler(exception_type_t eException,
                                                       bool fTopException,
                                                       CorUnix::MachExceptionHandler *pHandler)
{
    exception_mask_t bmExceptionMask = (1 << eException);
    int idxHandler = -1;
    CThreadMachExceptionHandlerNode *pNode = NULL;

    // Check top handlers first if we've been asked to and they have been initialized.
    if (fTopException && m_top.m_nPorts != -1)
    {
        pNode = &m_top;
        idxHandler = GetIndexOfHandler(bmExceptionMask, pNode);
    }

    // If we haven't identified a handler yet continue looking with the bottom handlers.
    if (idxHandler == -1)
    {
        pNode = &m_bottom;
        idxHandler = GetIndexOfHandler(bmExceptionMask, pNode);
    }

    // Did we find a handler?
    if (idxHandler == -1)
        return false;

    // Found one, so initialize the output structure with the details.
    pHandler->m_mask = pNode->m_masks[idxHandler];
    pHandler->m_handler = pNode->m_handlers[idxHandler];
    pHandler->m_behavior = pNode->m_behaviors[idxHandler];
    pHandler->m_flavor = pNode->m_flavors[idxHandler];

    return true;
}

// Look for a handler for the given exception within the given handler node. Return its index if successful or
// -1 otherwise.
int CorUnix::CThreadMachExceptionHandlers::GetIndexOfHandler(exception_mask_t bmExceptionMask,
                                                             CorUnix::CThreadMachExceptionHandlerNode *pNode)
{
    // Check all handler entries for one handling the exception mask.
    for (int i = 0; i < pNode->m_nPorts; i++)
    {
        if (pNode->m_masks[i] & bmExceptionMask &&      // Entry covers this exception type
            pNode->m_handlers[i] != MACH_PORT_NULL &&   // And the handler isn't null
            pNode->m_handlers[i] != s_ExceptionPort)    // And the handler isn't ourselves
        {
            // One more check; has the target handler port become dead?
            mach_port_type_t ePortType;
            if (mach_port_type(mach_task_self(), pNode->m_handlers[i], &ePortType) == KERN_SUCCESS &&
                !(ePortType & MACH_PORT_TYPE_DEAD_NAME))
            {
                // Got a matching entry.
                return i;
            }
        }
    }

    // Didn't find a handler.
    return -1;
}

#endif // HAVE_MACH_EXCEPTIONS
