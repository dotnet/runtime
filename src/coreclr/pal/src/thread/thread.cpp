// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    thread.cpp

Abstract:

    Thread object and core APIs



--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(THREAD); // some headers have code with asserts, so do this first

#include "pal/corunix.hpp"
#include "pal/context.h"
#include "pal/thread.hpp"
#include "pal/mutex.hpp"
#include "pal/handlemgr.hpp"
#include "pal/cs.hpp"
#include "pal/seh.hpp"
#include "pal/signal.hpp"

#include "procprivate.hpp"
#include "pal/process.h"
#include "pal/module.h"
#include "pal/environ.h"
#include "pal/init.h"
#include "pal/utils.h"
#include "pal/virtual.h"

#if defined(__NetBSD__) && !HAVE_PTHREAD_GETCPUCLOCKID
#include <sys/cdefs.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#elif defined(__sun)
#ifndef _KERNEL
#define _KERNEL
#define UNDEF_KERNEL
#endif
#include <sys/procfs.h>
#ifdef UNDEF_KERNEL
#undef _KERNEL
#endif
#endif

#include <signal.h>
#include <pthread.h>
#if HAVE_PTHREAD_NP_H
#include <pthread_np.h>
#endif
#include <unistd.h>
#include <errno.h>
#include <stddef.h>
#include <sys/stat.h>
#include <sys/mman.h>
#if HAVE_MACH_THREADS
#include <mach/mach.h>
#endif // HAVE_MACH_THREADS
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL
#include <limits.h>
#include <algorithm>

#if HAVE_SYS_LWP_H
#include <sys/lwp.h>
#endif
#if HAVE_LWP_H
#include <lwp.h>
#endif
// If we don't have sys/lwp.h but do expect to use _lwp_self, declare it to silence compiler warnings
#if HAVE__LWP_SELF && !HAVE_SYS_LWP_H && !HAVE_LWP_H
extern "C" int _lwp_self ();
#endif

#if HAVE_CPUSET_T
typedef cpuset_t cpu_set_t;
#endif

using namespace CorUnix;

#ifdef __APPLE__
#define MAX_THREAD_NAME_SIZE 63
#elif defined(__FreeBSD__)
#define MAX_THREAD_NAME_SIZE MAXCOMLEN
#else
#define MAX_THREAD_NAME_SIZE 15
#endif

/* ------------------- Definitions ------------------------------*/

// Activation function that gets called when an activation is injected into a thread.
PAL_ActivationFunction g_activationFunction = NULL;
// Function to check if an activation can be safely injected at a specified context
PAL_SafeActivationCheckFunction g_safeActivationCheckFunction = NULL;

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

CObjectType CorUnix::otThread(
                otiThread,
                ThreadCleanupRoutine,
                ThreadInitializationRoutine,
                0,      // sizeof(CThreadImmutableData),
                NULL,   // No immutable data copy routine
                NULL,   // No immutable data cleanup routine
                sizeof(CThreadProcessLocalData),
                NULL,   // No process local data cleanup routine
                0,      // sizeof(CThreadSharedData),
                0,      // THREAD_ALL_ACCESS,
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

    /* Call entry point functions of every attached modules to
       indicate the thread is exiting */
    /* note : no need to enter a critical section for serialization, the loader
       will lock its own critical section */
    LOADCallDllMain(DLL_THREAD_DETACH, NULL);

#if !HAVE_MACH_EXCEPTIONS
    pThread->FreeSignalAlternateStack();
#endif // !HAVE_MACH_EXCEPTIONS

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

    return TRUE;
}

/*++
Function:
    TLSCleanup

    Shutdown the TLS subsystem
--*/
VOID TLSCleanup()
{
    pthread_key_delete(thObjKey);
}

/*++
Function:
    AllocTHREAD

Abstract:
    Allocate CPalThread instance

Return:
    The fresh thread structure, NULL otherwise
--*/
CPalThread* AllocTHREAD()
{
    return InternalNew<CPalThread>();
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

    free(pThread);
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
        ERROR("Couldn't retrieve the hThread:%p pid owner !\n", hThread);
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

    dwThreadId = (DWORD)THREADSilentGetCurrentThreadId();

    LOGEXIT("GetCurrentThreadId returns DWORD %#x\n", dwThreadId);
    PERF_EXIT(GetCurrentThreadId);

    return dwThreadId;
}

/*++
Function:
  PAL_GetCurrentOSThreadId

Returns the current thread's OS thread ID.
This API is functionally equivalent to GetCurrentThreadId, but does not truncate the return value to 32-bits.
This is needed to ensure that we can provide the correct OS thread ID on platforms such as OSX that have a 64-bit thread ID.
--*/
size_t
PALAPI
PAL_GetCurrentOSThreadId(
            VOID)
{
    size_t threadId;

    PERF_ENTRY(PAL_GetCurrentOSThreadId);
    ENTRY("PAL_GetCurrentOSThreadId()\n");

    threadId = THREADSilentGetCurrentThreadId();

    LOGEXIT("PAL_GetCurrentOSThreadId returns %p\n", threadId);
    PERF_EXIT(GetCurrentThreadId);

    return threadId;
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
    SIZE_T osThreadId = 0;
    palError = InternalCreateThread(
        pThread,
        lpThreadAttributes,
        dwStackSize,
        lpStartAddress,
        lpParameter,
        dwCreationFlags,
        UserCreatedThread,
        &osThreadId,
        &hNewThread
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }
    if(lpThreadId != nullptr)
    {
        *lpThreadId = (DWORD)osThreadId;
    }
    LOGEXIT("CreateThread returns HANDLE %p\n", hNewThread);
    PERF_EXIT(CreateThread);

    return hNewThread;
}

/*++
Function:
  PAL_CreateThread64
  Similar to CreateThread but passes out a 64 bit thread id on platforms which use one.

Note:
  lpThreadAttributes could be ignored.

See MSDN doc.

--*/
HANDLE
PALAPI
PAL_CreateThread64(
    IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
    IN DWORD dwStackSize,
    IN LPTHREAD_START_ROUTINE lpStartAddress,
    IN LPVOID lpParameter,
    IN DWORD dwCreationFlags,
    OUT SIZE_T* pThreadId)
{
    PAL_ERROR palError;
    CPalThread *pThread;
    HANDLE hNewThread = NULL;

    PERF_ENTRY(PAL_CreateThread64);
    ENTRY("PAL_CreateThread64(lpThreadAttr=%p, dwStackSize=%u, lpStartAddress=%p, "
        "lpParameter=%p, dwFlags=%#x, pThreadId=%p)\n",
        lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter,
        dwCreationFlags, pThreadId);

    pThread = InternalGetCurrentThread();
    palError = InternalCreateThread(
        pThread,
        lpThreadAttributes,
        dwStackSize,
        lpStartAddress,
        lpParameter,
        dwCreationFlags,
        UserCreatedThread,
        pThreadId,
        &hNewThread
    );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("PAL_CreateThread64 returns HANDLE %p\n", hNewThread);
    PERF_EXIT(PAL_CreateThread64);

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
    SIZE_T* pThreadId,
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
#if PTHREAD_CREATE_MODIFIES_ERRNO
    int storedErrno;
#endif  // PTHREAD_CREATE_MODIFIES_ERRNO
    BOOL fHoldingProcessLock = FALSE;
    int iError = 0;
    size_t alignedStackSize;

    /* Validate parameters */

    if (lpThreadAttributes != NULL)
    {
        ASSERT("lpThreadAttributes parameter must be NULL (%p)\n",
               lpThreadAttributes);
        palError = ERROR_INVALID_PARAMETER;
        goto EXIT;
    }

    alignedStackSize = dwStackSize;
    if (alignedStackSize != 0)
    {
        // Some systems require the stack size to be aligned to the page size
        if (sizeof(alignedStackSize) <= sizeof(dwStackSize) && alignedStackSize + (GetVirtualPageSize() - 1) < alignedStackSize)
        {
            // When coming here from the public API surface, the incoming value is originally a nonnegative signed int32, so
            // this shouldn't happen
            ASSERT(
                "Couldn't align the requested stack size (%zu) to the page size because the stack size was too large\n",
                alignedStackSize);
            palError = ERROR_INVALID_PARAMETER;
            goto EXIT;
        }
        alignedStackSize = ALIGN_UP(alignedStackSize, GetVirtualPageSize());
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

    pNewThread = AllocTHREAD();
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

    if (alignedStackSize == 0)
    {
        // The thread is to be created with default stack size. Use the default stack size
        // override that was determined during the PAL initialization.
        alignedStackSize = g_defaultStackSize;
    }

    /* adjust the stack size if necessary */
    if (alignedStackSize != 0)
    {
#ifdef PTHREAD_STACK_MIN
        size_t MinStackSize = ALIGN_UP(PTHREAD_STACK_MIN, GetVirtualPageSize());
#else // !PTHREAD_STACK_MIN
        size_t MinStackSize = 64 * 1024; // this value is typically accepted by pthread_attr_setstacksize()
#endif // PTHREAD_STACK_MIN
        if (alignedStackSize < MinStackSize)
        {
            // Adjust the stack size to a minimum value that is likely to be accepted by pthread_attr_setstacksize(). If this
            // function fails, typically the caller will end up throwing OutOfMemoryException under the assumption that the
            // requested stack size is too large or the system does not have sufficient memory to create a thread. Try to
            // prevent failing just just because the stack size value is too low.
            alignedStackSize = MinStackSize;
        }

        TRACE("setting thread stack size to %zu\n", alignedStackSize);
        if (0 != pthread_attr_setstacksize(&pthreadAttr, alignedStackSize))
        {
            ERROR("couldn't set pthread stack size to %zu\n", alignedStackSize);
            palError = ERROR_INTERNAL_ERROR;
            goto EXIT;
        }
    }
    else
    {
        TRACE("using the system default thread stack size\n");
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

        if (NULL != pThreadId)
        {
            *pThreadId = pNewThread->GetThreadId();
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

    /* kill the thread (itself), resulting in a call to InternalEndCurrentThread */
    pthread_exit(NULL);

    ASSERT("pthread_exit should not return!\n");
    while (true);
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
    // TODO: Is this still needed after removing InternalSuspendThreadFromData?
    //

    pThread->suspensionInfo.AcquireSuspensionLock(pThread);
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

    // Ensure that EH is disabled on the current thread
    SEHDisable(pThread);

    //
    // Now release our reference to the thread data. We cannot touch
    // it after this point
    //

    pThread->ReleaseThreadReference();
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

    int st;
    int policy;
    struct sched_param schedParam;
    int max_priority;
    int min_priority;
    float posix_priority;


    palError = InternalGetThreadDataFromHandle(
        pThread,
        hTargetThread,
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
            pTargetThread->GetPThreadSelf(),
            &policy,
            &schedParam
            ) != 0)
    {
        ASSERT("Unable to get current thread scheduling information\n");
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadPriorityExit;
    }

#if !HAVE_SCHED_OTHER_ASSIGNABLE
    /* Defining thread priority for SCHED_OTHER is implementation defined.
       Some platforms like NetBSD cannot reassign it as they are dynamic.
    */
    if (policy == SCHED_OTHER)
    {
        TRACE("Pthread priority levels for SCHED_OTHER cannot be reassigned on this platform\n");
        goto InternalSetThreadPriorityExit;
    }
#endif

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
       -subtracting the minimal PAL priority from the desired priority. this
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
    st = pthread_setschedparam(pTargetThread->GetPThreadSelf(), policy, &schedParam);
    if (st != 0)
    {
#if SET_SCHEDPARAM_NEEDS_PRIVS
        if (EPERM == st)
        {
            // UNIXTODO: Should log a warning to the event log
            TRACE("Caller does not have OS privileges to call pthread_setschedparam\n");
            pTargetThread->m_iThreadPriority = iNewPriority;
            goto InternalSetThreadPriorityExit;
        }
#endif

        ASSERT("Unable to set thread priority to %d (error %d)\n", (int)posix_priority, st);
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

#define SECS_TO_NS 1000000000 /* 10^9 */
#define USECS_TO_NS 1000 /* 10^3 */

BOOL
CorUnix::GetThreadTimesInternal(
    IN HANDLE hThread,
    OUT LPFILETIME lpKernelTime,
    OUT LPFILETIME lpUserTime)
{
    __int64 calcTime;
    BOOL retval = FALSE;

#if HAVE_MACH_THREADS
    thread_basic_info resUsage;
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthrCurrent = NULL;
    CPalThread *pthrTarget = NULL;
    IPalObject *pobjThread = NULL;
    mach_msg_type_number_t resUsage_count = THREAD_BASIC_INFO_COUNT;

    pthrCurrent = InternalGetCurrentThread();
    palError = InternalGetThreadDataFromHandle(
        pthrCurrent,
        hThread,
        &pthrTarget,
        &pobjThread
        );

    if (palError != NO_ERROR)
    {
        ASSERT("Unable to get thread data from handle %p"
              "thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto SetTimesToZero;
    }

    pthrTarget->Lock(pthrCurrent);

    mach_port_t mhThread;
    mhThread = pthread_mach_thread_np(pthrTarget->GetPThreadSelf());

    kern_return_t status;
    status = thread_info(
        mhThread,
        THREAD_BASIC_INFO,
        (thread_info_t)&resUsage,
        &resUsage_count);

    pthrTarget->Unlock(pthrCurrent);

    if (status != KERN_SUCCESS)
    {
        ASSERT("Unable to get resource usage information for the current "
              "thread\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto SetTimesToZero;
    }

    /* Get the time of user mode execution, in nanoseconds */
    calcTime = (__int64)resUsage.user_time.seconds * SECS_TO_NS;
    calcTime += (__int64)resUsage.user_time.microseconds * USECS_TO_NS;
    /* Assign the time into lpUserTime */
    lpUserTime->dwLowDateTime = (DWORD)calcTime;
    lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);

    /* Get the time of kernel mode execution, in nanoseconds */
    calcTime = (__int64)resUsage.system_time.seconds * SECS_TO_NS;
    calcTime += (__int64)resUsage.system_time.microseconds * USECS_TO_NS;
    /* Assign the time into lpKernelTime */
    lpKernelTime->dwLowDateTime = (DWORD)calcTime;
    lpKernelTime->dwHighDateTime = (DWORD)(calcTime >> 32);

    retval = TRUE;

    goto GetThreadTimesInternalExit;

#elif defined(__NetBSD__) && !HAVE_PTHREAD_GETCPUCLOCKID /* Currently unimplemented */

    PAL_ERROR palError;
    CPalThread *pThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
    kvm_t *kd;
    int cnt, nlwps;
    struct kinfo_lwp *klwp;
    int i;
    bool found = false;

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        &pTargetThread,
        &pobjThread
        );
    if (palError != NO_ERROR)
    {
        ASSERT("Unable to get thread data from handle %p"
              "thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto SetTimesToZero;
    }

    kd = kvm_open(NULL, NULL, NULL, KVM_NO_FILES, "kvm_open");
    if (kd == NULL)
    {
        ASSERT("kvm_open(3) error");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto SetTimesToZero;
    }

    pTargetThread->Lock(pThread);

    klwp = kvm_getlwps(kd, getpid(), 0, sizeof(struct kinfo_lwp), &nlwps);
    if (klwp == NULL || nlwps < 1)
    {
        kvm_close(kd);
        ASSERT("Unable to get clock from %p thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        pTargetThread->Unlock(pThread);
        goto SetTimesToZero;
    }

    for (i = 0; i < nlwps; i++)
    {
        if (klwp[i].l_lid == THREADSilentGetCurrentThreadId())
        {
            found = true;
            break;
        }
    }

    if (!found)
    {
        kvm_close(kd);
        ASSERT("Unable to get clock from %p thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        pTargetThread->Unlock(pThread);
        goto SetTimesToZero;
    }

    pTargetThread->Unlock(pThread);

    kvm_close(kd);

    calcTime = (__int64) klwp[i].l_rtime_sec * SECS_TO_NS;
    calcTime += (__int64) klwp[i].l_rtime_usec * USECS_TO_NS;
    lpUserTime->dwLowDateTime = (DWORD)calcTime;
    lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);

    /* NetBSD as of (7.0) doesn't differentiate used time in user/kernel for lwp */
    lpKernelTime->dwLowDateTime = 0;
    lpKernelTime->dwHighDateTime = 0;

    retval = TRUE;
    goto GetThreadTimesInternalExit;

#else //HAVE_MACH_THREADS

    PAL_ERROR palError;
    CPalThread *pThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
#ifdef __sun
    int fd;
#else // __sun
    clockid_t cid;
#endif // __sun

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        &pTargetThread,
        &pobjThread
        );
    if (palError != NO_ERROR)
    {
        ASSERT("Unable to get thread data from handle %p"
              "thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        goto SetTimesToZero;
    }

    pTargetThread->Lock(pThread);

#if HAVE_PTHREAD_GETCPUCLOCKID
    if (pthread_getcpuclockid(pTargetThread->GetPThreadSelf(), &cid) != 0)
    {
        ASSERT("Unable to get clock from thread\n", hThread);
        SetLastError(ERROR_INTERNAL_ERROR);
        pTargetThread->Unlock(pThread);
        goto SetTimesToZero;
    }

    struct timespec ts;
    if (clock_gettime(cid, &ts) != 0)
    {
        ASSERT("clock_gettime() failed; errno is %d (%s)\n", errno, strerror(errno));
        SetLastError(ERROR_INTERNAL_ERROR);
        pTargetThread->Unlock(pThread);
        goto SetTimesToZero;
    }
#elif defined(__sun)
    timestruc_t ts;
    int readResult;
    char statusFilename[64];
    snprintf(statusFilename, sizeof(statusFilename), "/proc/%d/lwp/%d/lwpstatus", getpid(), pTargetThread->GetLwpId());
    fd = open(statusFilename, O_RDONLY);
    if (fd == -1)
    {
       ASSERT("open(%s) failed; errno is %d (%s)\n", statusFilename, errno, strerror(errno));
       SetLastError(ERROR_INTERNAL_ERROR);
       pTargetThread->Unlock(pThread);
       goto SetTimesToZero;
    }

    lwpstatus_t status;
    do
    {
        readResult = read(fd, &status, sizeof(status));
    }
    while ((readResult == -1) && (errno == EINTR));

    close(fd);

    ts = status.pr_utime;
#else // HAVE_PTHREAD_GETCPUCLOCKID
#error "Don't know how to obtain user cpu time on this platform."
#endif // HAVE_PTHREAD_GETCPUCLOCKID

    pTargetThread->Unlock(pThread);

    /* Calculate time in nanoseconds and assign to user time */
    calcTime = (__int64) ts.tv_sec * SECS_TO_NS;
    calcTime += (__int64) ts.tv_nsec;
    lpUserTime->dwLowDateTime = (DWORD)calcTime;
    lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);

    /* Set kernel time to zero, for now */
    lpKernelTime->dwLowDateTime = 0;
    lpKernelTime->dwHighDateTime = 0;

    retval = TRUE;
    goto GetThreadTimesInternalExit;

#endif //HAVE_MACH_THREADS

SetTimesToZero:

    lpUserTime->dwLowDateTime = 0;
    lpUserTime->dwHighDateTime = 0;
    lpKernelTime->dwLowDateTime = 0;
    lpKernelTime->dwHighDateTime = 0;
    goto GetThreadTimesInternalExit;

GetThreadTimesInternalExit:
    return retval;
}

HRESULT
PALAPI
SetThreadDescription(
    IN HANDLE hThread,
    IN PCWSTR lpThreadDescription)
{
    CPalThread *pThread;
    PAL_ERROR palError;

    PERF_ENTRY(SetThreadDescription);
    ENTRY("SetThreadDescription(hThread=%p,lpThreadDescription=%p)\n", hThread, lpThreadDescription);

    pThread = InternalGetCurrentThread();

    palError = InternalSetThreadDescription(
        pThread,
        hThread,
        lpThreadDescription
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("SetThreadDescription");
    PERF_EXIT(SetThreadDescription);

    return HRESULT_FROM_WIN32(palError);
}

PAL_ERROR
CorUnix::InternalSetThreadDescription(
    CPalThread *pThread,
    HANDLE hTargetThread,
    PCWSTR lpThreadDescription
)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pTargetThread = NULL;
    IPalObject *pobjThread = NULL;
    int error = 0;
    int maxNameSize = 0;
    int nameSize;
    char *nameBuf = NULL;

// The exact API of pthread_setname_np varies very wildly depending on OS.
// For now, only Linux, macOS and FreeBSD are implemented.
#if defined(__linux__) || defined(__APPLE__) || defined(__FreeBSD__)

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hTargetThread,
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetThreadDescriptionExit;
    }

    pTargetThread->Lock(pThread);

    // Ignore requests to set the main thread name because
    // it causes the value returned by Process.ProcessName to change.
    if ((pid_t)pTargetThread->GetThreadId() == getpid())
    {
        goto InternalSetThreadDescriptionExit;
    }

    /* translate the wide char lpThreadDescription string to multibyte string */
    nameSize = WideCharToMultiByte(CP_ACP, 0, lpThreadDescription, -1, NULL, 0, NULL, NULL);

    if (0 == nameSize)
    {
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadDescriptionExit;
    }

    nameBuf = (char *)PAL_malloc(nameSize);
    if (nameBuf == NULL)
    {
        palError = ERROR_OUTOFMEMORY;
        goto InternalSetThreadDescriptionExit;
    }

    if (WideCharToMultiByte(CP_ACP, 0, lpThreadDescription, -1, nameBuf, nameSize, NULL,
                            NULL) != nameSize)
    {
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSetThreadDescriptionExit;
    }

    // Null terminate early.
    // pthread_setname_np only accepts up to 16 chars on Linux,
    // 64 chars on macOS and 20 chars on FreeBSD.
    if (nameSize > MAX_THREAD_NAME_SIZE)
    {
        nameBuf[MAX_THREAD_NAME_SIZE] = '\0';
    }

    #if defined(__linux__) || defined(__FreeBSD__)
    error = pthread_setname_np(pTargetThread->GetPThreadSelf(), nameBuf);
    #endif

    #if defined(__APPLE__)
    // on macOS, pthread_setname_np only works for the calling thread.
    if (PlatformGetCurrentThreadId() == pTargetThread->GetThreadId())
    {
        error = pthread_setname_np(nameBuf);
    }
    #endif

    if (error != 0)
    {
        palError = ERROR_INTERNAL_ERROR;
    }

InternalSetThreadDescriptionExit:

    if (NULL != pTargetThread)
    {
        pTargetThread->Unlock(pThread);
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    if (NULL != nameBuf) {
        free(nameBuf);
    }

#endif //defined(__linux__) || defined(__APPLE__) || defined(__FreeBSD__)

    return palError;
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
#if HAVE_SCHED_GETAFFINITY && HAVE_SCHED_SETAFFINITY
    cpu_set_t cpuSet;
    int st;
#endif

    pThread = reinterpret_cast<CPalThread*>(pvParam);

    if (NULL == pThread)
    {
        ASSERT("THREAD pointer is NULL!\n");
        goto fail;
    }

#if HAVE_SCHED_GETAFFINITY && HAVE_SCHED_SETAFFINITY
    // Threads inherit their parent's affinity mask on Linux. This is not desired, so we reset
    // the current thread's affinity mask to the mask of the current process.
    //
    // Typically, we would use pthread_attr_setaffinity_np() and have pthread_create() create the thread with the specified
    // affinity. At least one implementation of pthread_create() following a pthread_attr_setaffinity_np() calls
    // sched_setaffinity(<newThreadPid>, ...), which is not allowed under Snap's default strict confinement without manually
    // connecting the process-control plug. To work around that, have the thread set the affinity after it starts.
    // sched_setaffinity(<currentThreadPid>, ...) is also currently not allowed, only sched_setaffinity(0, ...).
    // pthread_setaffinity_np(pthread_self(), ...) seems to call sched_setaffinity(<currentThreadPid>, ...) in at least one
    // implementation, and does not work. Use sched_setaffinity(0, ...) instead. See the following for more information:
    // - https://github.com/dotnet/runtime/pull/38795
    // - https://github.com/dotnet/runtime/issues/1634
    // - https://forum.snapcraft.io/t/requesting-autoconnect-for-interfaces-in-pigmeat-process-control-home/17987/13

    CPU_ZERO(&cpuSet);

    st = sched_getaffinity(gPID, sizeof(cpu_set_t), &cpuSet);
    if (st != 0)
    {
        ASSERT("sched_getaffinity failed!\n");
        // The sched_getaffinity should never fail for getting affinity of the current process
        palError = ERROR_INTERNAL_ERROR;
        goto fail;
    }

    st = sched_setaffinity(0, sizeof(cpu_set_t), &cpuSet);
    if (st != 0)
    {
        ASSERT("sched_setaffinity failed!\n");
        // The sched_setaffinity should never fail when passed the mask extracted using sched_getaffinity
        palError = ERROR_INTERNAL_ERROR;
        goto fail;
    }
#endif // HAVE_SCHED_GETAFFINITY && HAVE_SCHED_SETAFFINITY

#if !HAVE_MACH_EXCEPTIONS
    if (!pThread->EnsureSignalAlternateStack())
    {
        ASSERT("Cannot allocate alternate stack for SIGSEGV!\n");
        goto fail;
    }
#endif // !HAVE_MACH_EXCEPTIONS

    pThread->m_threadId = THREADSilentGetCurrentThreadId();
    pThread->m_pthreadSelf = pthread_self();
#if HAVE_MACH_THREADS
    pThread->m_machPortSelf = pthread_mach_thread_np(pThread->m_pthreadSelf);
#endif
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
        palError = pThread->suspensionInfo.InternalSuspendNewThreadFromData(pThread);
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
    pThread->SetExitCode(retValue);

    return NULL;

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
    pThread = AllocTHREAD();

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

    pThread->SetLastError(0);

    pThread->m_threadId = THREADSilentGetCurrentThreadId();
    pThread->m_pthreadSelf = pthread_self();
#if HAVE_MACH_THREADS
    pThread->m_machPortSelf = pthread_mach_thread_np(pThread->m_pthreadSelf);
#endif
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
    CreateThreadData

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

    pDummyThread = AllocTHREAD();
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

    palError = apcInfo.InitializePreCreate();
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

    if (pthread_setspecific(thObjKey, reinterpret_cast<void*>(this)))
    {
        ASSERT("Unable to set the thread object key's value\n");
        palError = ERROR_INTERNAL_ERROR;
        goto RunPostCreateInitializersExit;
    }

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

    palError = apcInfo.InitializePostCreate(this, m_threadId, m_dwLwpId);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

    palError = SEHEnable(this);
    if (NO_ERROR != palError)
    {
        goto RunPostCreateInitializersExit;
    }

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
    // This routine may get called from CPalThread::ThreadEntry
    //
    // If we've reached this point there are no further thread
    // suspensions that happen at creation time, so reset
    // m_bCreateSuspended
    //

    m_bCreateSuspended = FALSE;

    iError = pthread_mutex_lock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primitive failure\n");
        // bugcheck?
    }

    m_fStartStatus = fStartSucceeded;
    m_fStartStatusSet = TRUE;

    iError = pthread_cond_signal(&m_startCond);
    if (0 != iError)
    {
        ASSERT("pthread primitive failure\n");
        // bugcheck?
    }

    iError = pthread_mutex_unlock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primitive failure\n");
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
        ASSERT("pthread primitive failure\n");
        // bugcheck?
    }

    while (!m_fStartStatusSet)
    {
        iError = pthread_cond_wait(&m_startCond, &m_startMutex);
        if (0 != iError)
        {
            ASSERT("pthread primitive failure\n");
            // bugcheck?
        }
    }

    iError = pthread_mutex_unlock(&m_startMutex);
    if (0 != iError)
    {
        ASSERT("pthread primitive failure\n");
        // bugcheck?
    }

    return m_fStartStatus;
}

#if !HAVE_MACH_EXCEPTIONS
/*++
Function :
    EnsureSignalAlternateStack

    Ensure that alternate stack for signal handling is allocated for the current thread

Parameters :
    None

Return :
    TRUE in case of a success, FALSE otherwise
--*/
BOOL
CPalThread::EnsureSignalAlternateStack()
{
    int st = 0;

    if (g_registered_signal_handlers)
    {
        stack_t oss;

        // Query the current alternate signal stack
        st = sigaltstack(NULL, &oss);
        if ((st == 0) && (oss.ss_flags == SS_DISABLE))
        {
            // There is no alternate stack for SIGSEGV handling installed yet so allocate one

            // We include the size of the SignalHandlerWorkerReturnPoint in the alternate stack size since the
            // context contained in it is large and the SIGSTKSZ was not sufficient on ARM64 during testing.
            int altStackSize = SIGSTKSZ + ALIGN_UP(sizeof(SignalHandlerWorkerReturnPoint), 16) + GetVirtualPageSize();
#ifdef HAS_ADDRESS_SANITIZER
            // Asan also uses alternate stack so we increase its size on the SIGSTKSZ * 4 that enough for asan
            // (see kAltStackSize in compiler-rt/lib/sanitizer_common/sanitizer_posix_libcdep.cc)
            altStackSize += SIGSTKSZ * 4;
#endif
            altStackSize = ALIGN_UP(altStackSize, GetVirtualPageSize());
            int flags = MAP_ANONYMOUS | MAP_PRIVATE;
#ifdef MAP_STACK
            flags |= MAP_STACK;
#endif
            void* altStack = mmap(NULL, altStackSize, PROT_READ | PROT_WRITE, flags, -1, 0);
            if (altStack != MAP_FAILED)
            {
                // create a guard page for the alternate stack
                st = mprotect(altStack, GetVirtualPageSize(), PROT_NONE);
                if (st == 0)
                {
                    stack_t ss;
                    ss.ss_sp = (char*)altStack;
                    ss.ss_size = altStackSize;
                    ss.ss_flags = 0;
                    st = sigaltstack(&ss, NULL);
                }

                if (st == 0)
                {
                    m_alternateStack = altStack;
                }
                else
                {
                    int st2 = munmap(altStack, altStackSize);
                    _ASSERTE(st2 == 0);
                }
            }
        }
    }

    return (st == 0);
}

/*++
Function :
    FreeSignalAlternateStack

    Free alternate stack for signal handling

Parameters :
    None

Return :
    None
--*/
void
CPalThread::FreeSignalAlternateStack()
{
    void *altstack = m_alternateStack;
    m_alternateStack = nullptr;

    if (altstack != nullptr)
    {
        stack_t ss, oss;
        // The man page for sigaltstack says that when the ss.ss_flags is set to SS_DISABLE,
        // all other ss fields are ignored. However, MUSL implementation checks that the
        // ss_size is >= MINSIGSTKSZ even in this case.
        ss.ss_size = MINSIGSTKSZ;
        ss.ss_flags = SS_DISABLE;
        ss.ss_sp = NULL;
        int st = sigaltstack(&ss, &oss);
        if ((st == 0) && (oss.ss_flags != SS_DISABLE))
        {
            // Make sure this altstack is this PAL's before freeing.
            if (oss.ss_sp == altstack)
            {
                int st = munmap(oss.ss_sp, oss.ss_size);
                _ASSERTE(st == 0);
            }
        }
    }
}

#endif // !HAVE_MACH_EXCEPTIONS

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

// Get base address of the current thread's stack
void *
CPalThread::GetStackBase()
{
    void* stackBase;
#ifdef TARGET_OSX
    // This is a Mac specific method
    stackBase = pthread_get_stackaddr_np(pthread_self());
#else
    pthread_attr_t attr;
    void* stackAddr;
    size_t stackSize;
    int status;

    pthread_t thread = pthread_self();

    status = pthread_attr_init(&attr);
    _ASSERT_MSG(status == 0, "pthread_attr_init call failed");

#if HAVE_PTHREAD_ATTR_GET_NP
    status = pthread_attr_get_np(thread, &attr);
#elif HAVE_PTHREAD_GETATTR_NP
    status = pthread_getattr_np(thread, &attr);
#else
#error Dont know how to get thread attributes on this platform!
#endif
    _ASSERT_MSG(status == 0, "pthread_getattr_np call failed");

    status = pthread_attr_getstack(&attr, &stackAddr, &stackSize);
    _ASSERT_MSG(status == 0, "pthread_attr_getstack call failed");

    status = pthread_attr_destroy(&attr);
    _ASSERT_MSG(status == 0, "pthread_attr_destroy call failed");

    stackBase = (void*)((size_t)stackAddr + stackSize);
#endif

    return stackBase;
}

// Get limit address of the current thread's stack
void *
CPalThread::GetStackLimit()
{
    void* stackLimit;
#ifdef TARGET_OSX
    // This is a Mac specific method
    stackLimit = ((BYTE *)pthread_get_stackaddr_np(pthread_self()) -
                   pthread_get_stacksize_np(pthread_self()));
#else
    pthread_attr_t attr;
    size_t stackSize;
    int status;

    pthread_t thread = pthread_self();

    status = pthread_attr_init(&attr);
    _ASSERT_MSG(status == 0, "pthread_attr_init call failed");

#if HAVE_PTHREAD_ATTR_GET_NP
    status = pthread_attr_get_np(thread, &attr);
#elif HAVE_PTHREAD_GETATTR_NP
    status = pthread_getattr_np(thread, &attr);
#else
#error Dont know how to get thread attributes on this platform!
#endif
    _ASSERT_MSG(status == 0, "pthread_getattr_np call failed");

    status = pthread_attr_getstack(&attr, &stackLimit, &stackSize);
    _ASSERT_MSG(status == 0, "pthread_attr_getstack call failed");

    status = pthread_attr_destroy(&attr);
    _ASSERT_MSG(status == 0, "pthread_attr_destroy call failed");
#endif

    return stackLimit;
}

// Get cached base address of this thread's stack
// Can be called only for the current thread.
void *
CPalThread::GetCachedStackBase()
{
    _ASSERT_MSG(this == InternalGetCurrentThread(), "CPalThread::GetStackBase called from foreign thread");

    if (m_stackBase == NULL)
    {
        m_stackBase = GetStackBase();
    }

    return m_stackBase;
}

// Get cached limit address of this thread's stack.
// Can be called only for the current thread.
void *
CPalThread::GetCachedStackLimit()
{
    _ASSERT_MSG(this == InternalGetCurrentThread(), "CPalThread::GetCachedStackLimit called from foreign thread");

    if (m_stackLimit == NULL)
    {
        m_stackLimit = GetStackLimit();
    }

    return m_stackLimit;
}

PVOID
PALAPI
PAL_GetStackBase()
{
    CPalThread* thread = InternalGetCurrentThread();
    return thread->GetCachedStackBase();
}

PVOID
PALAPI
PAL_GetStackLimit()
{
    CPalThread* thread = InternalGetCurrentThread();
    return thread->GetCachedStackLimit();
}

PAL_ERROR InjectActivationInternal(CorUnix::CPalThread* pThread);

/*++
Function:
    PAL_SetActivationFunction

    Register an activation function that gets called when an activation is injected
    into a thread.

Parameters:
    pActivationFunction - activation function
    pSafeActivationCheckFunction - function to check if an activation can be safely
                                   injected at a specified context
Return value:
    None
--*/
PALIMPORT
VOID
PALAPI
PAL_SetActivationFunction(
    IN PAL_ActivationFunction pActivationFunction,
    IN PAL_SafeActivationCheckFunction pSafeActivationCheckFunction)
{
    g_activationFunction = pActivationFunction;
    g_safeActivationCheckFunction = pSafeActivationCheckFunction;
}

/*++
Function:
PAL_InjectActivation

Interrupt the specified thread and have it call an activation function registered
using the PAL_SetActivationFunction

Parameters:
hThread            - handle of the target thread

Return:
TRUE if it succeeded, FALSE otherwise.
--*/
BOOL
PALAPI
PAL_InjectActivation(
    IN HANDLE hThread)
{
    PERF_ENTRY(PAL_InjectActivation);
    ENTRY("PAL_InjectActivation(hThread=%p)\n", hThread);

    CPalThread *pCurrentThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;

    pCurrentThread = InternalGetCurrentThread();

    PAL_ERROR palError = InternalGetThreadDataFromHandle(
        pCurrentThread,
        hThread,
        &pTargetThread,
        &pobjThread
        );

    if (palError == NO_ERROR)
    {
        palError = InjectActivationInternal(pTargetThread);
    }

    if (palError != NO_ERROR)
    {
        pCurrentThread->SetLastError(palError);
    }

    if (pobjThread != NULL)
    {
        pobjThread->ReleaseReference(pCurrentThread);
    }

    BOOL success = (palError == NO_ERROR);
    LOGEXIT("PAL_InjectActivation returns:d\n", success);
    PERF_EXIT(PAL_InjectActivation);

    return success;
}

#if HAVE_MACH_EXCEPTIONS

extern mach_port_t s_ExceptionPort;

// Get handler details for a given type of exception. If successful the structure pointed at by pHandler is
// filled in and true is returned. Otherwise false is returned.
bool CorUnix::CThreadMachExceptionHandlers::GetHandler(exception_type_t eException, CorUnix::MachExceptionHandler *pHandler)
{
    exception_mask_t bmExceptionMask = (1 << eException);
    int idxHandler = GetIndexOfHandler(bmExceptionMask);

    // Did we find a handler?
    if (idxHandler == -1)
        return false;

    // Found one, so initialize the output structure with the details.
    pHandler->m_mask = m_masks[idxHandler];
    pHandler->m_handler = m_handlers[idxHandler];
    pHandler->m_behavior = m_behaviors[idxHandler];
    pHandler->m_flavor = m_flavors[idxHandler];

    return true;
}

// Look for a handler for the given exception within the given handler node. Return its index if successful or
// -1 otherwise.
int CorUnix::CThreadMachExceptionHandlers::GetIndexOfHandler(exception_mask_t bmExceptionMask)
{
    // Check all handler entries for one handling the exception mask.
    for (mach_msg_type_number_t i = 0; i < m_nPorts; i++)
    {
        // Entry covers this exception type and the handler isn't null
        if (m_masks[i] & bmExceptionMask && m_handlers[i] != MACH_PORT_NULL)
        {
            _ASSERTE(m_handlers[i] != s_ExceptionPort);

            // One more check; has the target handler port become dead?
            mach_port_type_t ePortType;
            if (mach_port_type(mach_task_self(), m_handlers[i], &ePortType) == KERN_SUCCESS && !(ePortType & MACH_PORT_TYPE_DEAD_NAME))
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

/*++
Function:
  PAL_SetCurrentThreadAffinity

Abstract
  Set affinity of the current thread to the specified processor.

Parameters:
  procNo - number of the processor to affinitize the current thread to

Return value:
  TRUE if the function was able to set the affinity, FALSE if it has failed.
--*/
BOOL
PALAPI
PAL_SetCurrentThreadAffinity(WORD procNo)
{
#if HAVE_SCHED_SETAFFINITY || HAVE_PTHREAD_SETAFFINITY_NP
    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);
    CPU_SET(procNo, &cpuSet);

    // Snap's default strict confinement does not allow sched_setaffinity(<nonzeroPid>, ...) without manually connecting the
    // process-control plug. sched_setaffinity(<currentThreadPid>, ...) is also currently not allowed, only
    // sched_setaffinity(0, ...). pthread_setaffinity_np(pthread_self(), ...) seems to call
    // sched_setaffinity(<currentThreadPid>, ...) in at least one implementation, and does not work. To work around those
    // issues, use sched_setaffinity(0, ...) if available and only otherwise fall back to pthread_setaffinity_np(). See the
    // following for more information:
    // - https://github.com/dotnet/runtime/pull/38795
    // - https://github.com/dotnet/runtime/issues/1634
    // - https://forum.snapcraft.io/t/requesting-autoconnect-for-interfaces-in-pigmeat-process-control-home/17987/13
#if HAVE_SCHED_SETAFFINITY
    int st = sched_setaffinity(0, sizeof(cpu_set_t), &cpuSet);
#else
    int st = pthread_setaffinity_np(pthread_self(), sizeof(cpu_set_t), &cpuSet);
#endif

    return st == 0;
#else  // !(HAVE_SCHED_SETAFFINITY || HAVE_PTHREAD_SETAFFINITY_NP)
    // There is no API to manage thread affinity, so let's ignore the request
    return FALSE;
#endif // HAVE_SCHED_SETAFFINITY || HAVE_PTHREAD_SETAFFINITY_NP
}

/*++
Function:
  PAL_SetCurrentThreadAffinity

Abstract
  Get affinity set of the current thread. The set is represented by an array of "size" entries of UINT_PTR type.

Parameters:
  size - number of entries in the "data" array
  data - pointer to the data of the resulting set, the LSB of the first entry in the array represents processor 0

Return value:
  TRUE if the function was able to get the affinity set, FALSE if it has failed.
--*/
BOOL
PALAPI
PAL_GetCurrentThreadAffinitySet(SIZE_T size, UINT_PTR* data)
{
#if HAVE_PTHREAD_GETAFFINITY_NP
    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);

    int st = pthread_getaffinity_np(pthread_self(), sizeof(cpu_set_t), &cpuSet);

    if (st == 0)
    {
        const SIZE_T BitsPerBitsetEntry = 8 * sizeof(UINT_PTR);

        // Get info for as much processors as it is possible to fit into the resulting set
        SIZE_T remainingCount = std::min(size * BitsPerBitsetEntry, (SIZE_T)CPU_SETSIZE);
        SIZE_T i = 0;
        while (remainingCount != 0)
        {
            UINT_PTR entry = 0;
            SIZE_T bitsToCopy = std::min(remainingCount, BitsPerBitsetEntry);
            SIZE_T cpuSetOffset = i * BitsPerBitsetEntry;
            for (SIZE_T j = 0; j < bitsToCopy; j++)
            {
                if (CPU_ISSET(cpuSetOffset + j, &cpuSet))
                {
                    entry |= (UINT_PTR)1 << j;
                }
            }
            remainingCount -= bitsToCopy;
            data[i++] = entry;
        }
    }

    return st == 0;
#else  // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's ignore the request
    return FALSE;
#endif // HAVE_PTHREAD_GETAFFINITY_NP
}
