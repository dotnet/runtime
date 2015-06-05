//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//


/*++



Module Name:

    threadsusp.cpp

Abstract:

    Implementation of functions related to threads.

Revision History:



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/mutex.hpp"
#include "pal/seh.hpp"
#include "pal/init.h"
#include "pal/dbgmsg.h"

#include <pthread.h>
#if !USE_SIGNALS_FOR_THREAD_SUSPENSION
#if (HAVE_PTHREAD_SUSPEND || HAVE_PTHREAD_SUSPEND_NP) && (HAVE_PTHREAD_RESUME || HAVE_PTHREAD_RESUME_NP || HAVE_PTHREAD_CONTINUE || HAVE_PTHREAD_CONTINUE_NP)
#if HAVE_PTHREAD_NP_H
#include <pthread_np.h>
#endif
#elif HAVE_MACH_THREADS
#include <mach/thread_act.h>
#include "sys/types.h"
#include "sys/mman.h"
#else
#error "Don't know how to suspend and resume threads on this platform!"
#endif
#endif // !USE_SIGNALS_FOR_THREAD_SUSPENSION
#include <unistd.h>
#include <errno.h>
#include <stddef.h>
#include <sys/stat.h>
#include <limits.h>

#if defined(_AIX)
// AIX requires explicit definition of the union semun (see semctl man page)
union semun 
{
    int val;
    struct semid_ds * buf;
    unsigned short * array;
};
#endif 

using namespace CorUnix;

/* ------------------- Definitions ------------------------------*/
SET_DEFAULT_DEBUG_CHANNEL(THREAD);

/* This code is written to the blocking pipe of a thread that was created
   in suspended state in order to resume it. */
CONST BYTE WAKEUPCODE=0x2A;

// #define USE_GLOBAL_LOCK_FOR_SUSPENSION // Uncomment this define to use the global suspension lock. 
/* The global suspension lock can be used in place of each thread having its own
suspension mutex or spinlock. The downside is that it restricts us to only
performing one suspension or resumption in the PAL at a time. */
#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
static LONG g_ssSuspensionLock = 0;
#endif

#if !HAVE_MACH_EXCEPTIONS
static sigset_t smDefaultmask; // masks signals that the PAL handles as exceptions.
#endif // !HAVE_MACH_EXCEPTIONS
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
static sigset_t smWaitmask; // used so a thread does not receive a SIGUSR1 or SIGUSR2, during a suspension retry, until it enters sigsuspend
static sigset_t smSuspmask; // used when a thread is suspended via signals; blocks all signals except SIGUSR2
static sigset_t smContmask; // used when a thread is in sigsuspend on a suspension retry, waiting to receive a SIGUSR1
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

/*++
Function:
  SuspendThread

See MSDN doc.
--*/
DWORD
PALAPI
SuspendThread(
          IN HANDLE hThread)
{
    PAL_ERROR palError;
    CPalThread *pthrSuspender;
    DWORD dwSuspendCount = (DWORD)-1;

    PERF_ENTRY(SuspendThread);
    ENTRY("SuspendThread(hThread=%p)\n", hThread);

    pthrSuspender = InternalGetCurrentThread();
    palError = InternalSuspendThread(
        pthrSuspender,
        hThread,
        &dwSuspendCount
        );

    if (NO_ERROR != palError)
    {
        pthrSuspender->SetLastError(palError);
        dwSuspendCount = (DWORD) -1;
    }
    else
    {
        _ASSERT_MSG(dwSuspendCount != static_cast<DWORD>(-1), "InternalSuspendThread returned success but dwSuspendCount did not change.\n");
    }

    LOGEXIT("SuspendThread returns DWORD %u\n", dwSuspendCount);
    PERF_EXIT(SuspendThread);
    return dwSuspendCount;
}

/*++
Function:
  InternalSuspendThread

InternalSuspendThread converts the handle of the target thread to a 
CPalThread, and passes both the suspender and target thread references
to InternalSuspendThreadFromData. A reference to the suspend count from
the suspension attempt is passed back to the caller of this function.
--*/
PAL_ERROR
CorUnix::InternalSuspendThread(
    CPalThread *pthrSuspender,
    HANDLE hTargetThread,
    DWORD *pdwSuspendCount
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthrTarget = NULL;
    IPalObject *pobjThread = NULL;

    palError = InternalGetThreadDataFromHandle(
        pthrSuspender,
        hTargetThread,
        0, // THREAD_SUSPEND_RESUME
        &pthrTarget,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        palError = pthrSuspender->suspensionInfo.InternalSuspendThreadFromData(
            pthrSuspender,
            pthrTarget,
            pdwSuspendCount
            );
    } 

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pthrSuspender);
    }

    return palError;
}

/*++
Function:
  InternalSuspendNewThreadFromData

  On platforms where we use pipes for starting threads suspended, this
  function sets the blocking pipe for the thread and blocks until the
  wakeup code is written to the pipe by ResumeThread.

  On platforms where we don't use pipes for starting threads suspended,
  this function falls back on InternalSuspendThreadFromData to perform
  the suspension.
--*/
PAL_ERROR
CThreadSuspensionInfo::InternalSuspendNewThreadFromData(
    CPalThread *pThread
    )
{
    PAL_ERROR palError = NO_ERROR;

    AcquireSuspensionLock(pThread);
    pThread->suspensionInfo.SetSelfSusp(TRUE);
    pThread->suspensionInfo.IncrSuspCount();
    ReleaseSuspensionLock(pThread);

    int pipe_descs[2];
    if (pipe(pipe_descs) == -1)
    {
        ERROR("pipe() failed! error is %d (%s)\n", errno, strerror(errno));
        return ERROR_NOT_ENOUGH_MEMORY;
    }

    // [0] is the read end of the pipe, and [1] is the write end.
    pThread->suspensionInfo.SetBlockingPipe(pipe_descs[1]);
    pThread->SetStartStatus(TRUE);

    BYTE resume_code = 0;
    ssize_t read_ret;
    
    // Block until ResumeThread writes something to the pipe
    while ((read_ret = read(pipe_descs[0], &resume_code, sizeof(resume_code))) != sizeof(resume_code))
    {
        if (read_ret != -1 || EINTR != errno)
        {
            // read might return 0 (with EAGAIN) if the other end of the pipe gets closed
            palError = ERROR_INTERNAL_ERROR;
            break;
        }
    }

    if (palError == NO_ERROR && resume_code != WAKEUPCODE)
    {
        // If we did read successfully but the byte didn't match WAKEUPCODE, we treat it as a failure.
        palError = ERROR_INTERNAL_ERROR;
    }

    // Close the pipes regardless of whether we were successful.
    close(pipe_descs[0]);
    close(pipe_descs[1]);

    return palError;
}

/*++
Function:
  InternalSuspendThreadFromData

InternalSuspendThreadFromData suspends the target thread. It first checks if 
the target thread is suspending itself. Next, it acquires the thread(s) 
suspension locks before attempting the actual suspension. Once the attempt
is completed, the locks are released. The starting suspend count of the 
target thread is passed back to the caller of this function.

Note that ReleaseSuspensionLock(s) is called before hitting ASSERTs in error
paths. Currently, this seems unnecessary since asserting within 
InternalSuspendThreadFromData will not cause cleanup to occur. However,
this may change since it would be preferable to perform cleanup in these
situations. Thus, calls to release suspension locks remain in the error paths.
--*/
PAL_ERROR
CThreadSuspensionInfo::InternalSuspendThreadFromData(
    CPalThread *pthrSuspender,
    CPalThread *pthrTarget,
    DWORD *pdwSuspendCount
    )
{
    PAL_ERROR palError = NO_ERROR;
    DWORD dwPrevSuspendCount = 0;

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
    int nPthreadRet = 0;
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

    BOOL fSelfSuspend = FALSE;

    pthrSuspender->suspensionInfo.SetPerformingSuspension(TRUE);

    if (!pthrSuspender->suspensionInfo.IsSuspensionStateSafe())
    {
        ASSERT("The suspending thread is in an unsafe region.\n");
        palError = ERROR_INTERNAL_ERROR;
        goto InternalSuspendThreadFromDataExit;
    }
    
    if (SignalHandlerThread == pthrTarget->GetThreadType())
    {
        ASSERT("Attempting to suspend the signal handling thread.\n");
        palError = ERROR_INVALID_HANDLE;
        goto InternalSuspendThreadFromDataExit;
    }

    // Check if this is a self suspension
    if (pthrSuspender->GetThreadId() == pthrTarget->GetThreadId())
    {
        fSelfSuspend = TRUE;
    }

    // Acquire suspension mutex(es)
    if (fSelfSuspend)
    {
        AcquireSuspensionLock(pthrTarget);
    }
    else
    {
        AcquireSuspensionLocks(pthrSuspender, pthrTarget);
    }

    // Check target thread's state to ensure it hasn't died. Setting a 
    // thread's state to TS_DONE is protected by the target's suspension mutex.
    if (pthrTarget->synchronizationInfo.GetThreadState() == TS_DONE)
    {
        palError = ERROR_INVALID_HANDLE;    
        if (fSelfSuspend)
        {
            ReleaseSuspensionLock(pthrTarget);
            ASSERT("Self suspension should not fail due to TS_DONE\n");
        }
        else
        {
            ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
        }
        goto InternalSuspendThreadFromDataExit;     
    }
        
    if (pthrTarget->suspensionInfo.GetSuspCount() < MAXIMUM_SUSPEND_COUNT)
    { 
#if USE_PTHREAD_CONDVARS
        // We must initialize the flag indicating the target has acknowledged the suspension
        // before they could possibly have set it.
        m_fSuspended = FALSE;
#endif

        // If this is a self suspension, the suspension count must be 0
        // so this if statement will be true
        if (pthrTarget->suspensionInfo.GetSuspCount() == 0) 
        {
            if (fSelfSuspend)
            {
                // Notice that suspension count is incremented after 
                // assigning it to dwPrevSuspendCount
                pthrTarget->suspensionInfo.SetSelfSusp(TRUE);               
                dwPrevSuspendCount = pthrTarget->suspensionInfo.GetSuspCount();
                pthrTarget->suspensionInfo.IncrSuspCount();
                ReleaseSuspensionLock(pthrTarget);
            }

            /* Within this do while loop, the actual suspension is attempted. 
            If the target thread is unsafe, the suspender thread will wait on 
            the target's suspension semaphore. Once the target posts on the 
            semaphore, it is no longer unsafe and it will wait for the suspender 
            thread to reiterate the loop and attempt to suspend it again. */
            do
            {
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
                pthrTarget->suspensionInfo.SetSuspendSignalSent(TRUE);
                // Send the SIGUSR1 to the target thread and wait for it to post, immediately prior to suspension.
                nPthreadRet = pthread_kill(pthrTarget->GetPThreadSelf(), SIGUSR1);
                if (nPthreadRet == 0)
                {
                    if (!fSelfSuspend)
                    {
                        pthrTarget->suspensionInfo.WaitOnSuspendSemaphore();                        
                    }
                }
                else
                {
                    // pthread_kill failed so bail out.
                    palError = ERROR_SIGNAL_REFUSED;
                    if (fSelfSuspend)
                    {
                         // Decrement suspension count for self suspension case since 
                         // it was already incremented but the suspension attempt failed.
                        pthrTarget->suspensionInfo.DecrSuspCount();
                        pthrTarget->suspensionInfo.SetSelfSusp(FALSE);
                        ReleaseSuspensionLock(pthrTarget);
                        ASSERT("Self suspension should not fail but pthread_kill returned %d\n", nPthreadRet);
                    }
                    else
                    {
                        ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
                        ASSERT("pthread_kill failed with error %d\n", nPthreadRet);
                    }
                    goto InternalSuspendThreadFromDataExit;
                }
#else // USE_SIGNALS_FOR_THREAD_SUSPENSION
                // Call the native suspension function. If it fails, bail out.
                if (!THREADHandleSuspendNative(pthrTarget))
                {
                    palError = ERROR_INTERNAL_ERROR;
                    if (fSelfSuspend)
                    {
                        pthrTarget->suspensionInfo.DecrSuspCount();
                        pthrTarget->suspensionInfo.SetSelfSusp(FALSE);                        
                        ReleaseSuspensionLock(pthrTarget);
                    }
                    else
                    {
                        ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
                        ASSERT("Native suspension actually failed!\n");
                    }
                    goto InternalSuspendThreadFromDataExit;
                }

                // Couldn't be a self suspension, if the thread is unsafe, 
                // since there is an assert check above for a thread 
                // self suspending in an unsafe region.
                if (!pthrTarget->suspensionInfo.IsSuspensionStateSafe())
                {
                    // The target thread is suspension unsafe so set its pending
                    // field to TRUE. Once it leaves the unsafe region, it will
                    // check this field and wait to be suspended.
                    pthrTarget->suspensionInfo.SetSuspPending(TRUE);

                    // Since the target was suspended before checking if it was
                    // suspension unsafe, resume it now so it can eventually
                    // become suspension safe (and be suspended).
                    if(!THREADHandleResumeNative(pthrTarget))
                    {
                        palError = ERROR_INTERNAL_ERROR;
                        ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
                        ASSERT("Native suspension actually failed!\n");
                        goto InternalSuspendThreadFromDataExit;
                    }       

                    // Wait for the target thread to become suspension safe.
                    pthrTarget->suspensionInfo.WaitOnSuspendSemaphore();
                }
                else
                {   
                    // The target thread is suspension safe so leave it suspended
                    // and set its pending field to FALSE since the suspension
                    // succeeded.
                    pthrTarget->suspensionInfo.SetSuspPending(FALSE);
                }
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
            } while (pthrTarget->suspensionInfo.GetSuspPending());
        }

        if (!fSelfSuspend)
        {
            // Notice that suspension count is incremented 
            // after assigning it to dwPrevSuspendCount
            dwPrevSuspendCount = pthrTarget->suspensionInfo.GetSuspCount();
            pthrTarget->suspensionInfo.IncrSuspCount();
        }
    }
    else
    {
        // A self suspending thread can't reach this code since 
        // the suspender thread would already have been suspended.
        palError = ERROR_SIGNAL_REFUSED;
        ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
        _ASSERT_MSG(!fSelfSuspend, "A self suspending thread must have a suspension count of 0 to enter SuspendThread, "
            "yet SuspendThread thinks that this thread's count has reached the maximum.");
        goto InternalSuspendThreadFromDataExit;
    }

    if (!fSelfSuspend)
    {
        ReleaseSuspensionLocks(pthrSuspender, pthrTarget);
    }      
        
    InternalSuspendThreadFromDataExit: 

    if (NO_ERROR == palError)
    {
        *pdwSuspendCount = dwPrevSuspendCount;

#ifdef _DEBUG
        // Don't increment a self suspending thread's count of threads it suspended.
        if (!fSelfSuspend)
        {
            pthrSuspender->suspensionInfo.IncrNumThreadsSuspendedByThisThread();
        }
#endif
    }

    pthrSuspender->suspensionInfo.SetPerformingSuspension(FALSE);
    
    return palError;
}


/*++
Function:
  ResumeThread

See MSDN doc.
--*/
DWORD
PALAPI
ResumeThread(
         IN HANDLE hThread
         )
{
    PAL_ERROR palError;
    CPalThread *pthrResumer;
    DWORD dwSuspendCount = (DWORD)-1;

    PERF_ENTRY(ResumeThread);
    ENTRY("ResumeThread(hThread=%p)\n", hThread);

    pthrResumer = InternalGetCurrentThread();
    palError = InternalResumeThread(
        pthrResumer,
        hThread,
        &dwSuspendCount
        );

    if (NO_ERROR != palError)
    {
        pthrResumer->SetLastError(palError);
        dwSuspendCount = (DWORD) -1;
    }
    else
    {
        _ASSERT_MSG(dwSuspendCount != static_cast<DWORD>(-1), "InternalResumeThread returned success but dwSuspendCount did not change.\n");   
    }

    LOGEXIT("ResumeThread returns DWORD %u\n", dwSuspendCount);
    PERF_EXIT(ResumeThread);
    return dwSuspendCount;
}

/*++
Function:
  InternalResumeThread

InternalResumeThread converts the handle of the target thread to a 
CPalThread, and passes both the resumer and target thread references
to InternalResumeThreadFromData. A reference to the suspend count from
the resumption attempt is passed back to the caller of this function.
--*/
PAL_ERROR
CorUnix::InternalResumeThread(
    CPalThread *pthrResumer,
    HANDLE hTargetThread,
    DWORD *pdwSuspendCount
    )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthrTarget = NULL;
    IPalObject *pobjThread = NULL;

    palError = InternalGetThreadDataFromHandle(
        pthrResumer,
        hTargetThread,
        0, // THREAD_SUSPEND_RESUME
        &pthrTarget,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        palError = pthrResumer->suspensionInfo.InternalResumeThreadFromData(
            pthrResumer,
            pthrTarget,
            pdwSuspendCount
            );
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pthrResumer);
    }

    return palError;
}

/*++
Function:
  InternalResumeThreadFromData

InternalResumeThreadFromData resumes the target thread. First, the suspension
mutexes of the threads are acquired. Next, there's a check to ensure that the
target thread was actually suspended. Finally, the resume attempt is made
and the suspension mutexes are released. The suspend count of the 
target thread is passed back to the caller of this function.

Note that ReleaseSuspensionLock(s) is called before hitting ASSERTs in error
paths. Currently, this seems unnecessary since asserting within 
InternalResumeThreadFromData will not cause cleanup to occur. However,
this may change since it would be preferable to perform cleanup. Thus, calls
to release suspension locks remain in the error paths.
--*/
PAL_ERROR
CThreadSuspensionInfo::InternalResumeThreadFromData(
    CPalThread *pthrResumer,
    CPalThread *pthrTarget,
    DWORD *pdwSuspendCount
    )
{
    PAL_ERROR palError = NO_ERROR;
    DWORD dwPrevSuspendCount = 0;

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
    DWORD dwPthreadRet = 0;
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

    int nWrittenBytes = -1;

#ifdef _DEBUG
    // This flag is used to determine if the resuming thread's count, of threads 
    // it has suspended, should be decremented after the resume attempt 
    // on the target thread.
    BOOL fDecrementSuspenderCount = FALSE;
 #endif
    
    pthrResumer->suspensionInfo.SetPerformingSuspension(TRUE);

    if (!pthrResumer->suspensionInfo.IsSuspensionStateSafe())
    {
        ASSERT("The resuming thread is in an unsafe region.\n");
        palError = ERROR_INTERNAL_ERROR;
        goto InternalResumeThreadFromDataExit;
    }

    if (SignalHandlerThread == pthrTarget->GetThreadType())
    {
        ASSERT("Attempting to resume the signal handling thread, which can never be suspended.\n");
        palError = ERROR_INVALID_HANDLE;
        goto InternalResumeThreadFromDataExit;
    }

    // Acquire suspension mutex
    AcquireSuspensionLocks(pthrResumer, pthrTarget);

    // Check target thread's state to ensure it hasn't died. 
    // Setting a thread's state to TS_DONE is protected by the 
    // target's suspension mutex.
    if (pthrTarget->synchronizationInfo.GetThreadState() == TS_DONE)
    {
        palError = ERROR_INVALID_HANDLE;
        ReleaseSuspensionLocks(pthrResumer, pthrTarget);
        goto InternalResumeThreadFromDataExit;
    }

    // If this is a dummy thread, then it represents a process that was created with CREATE_SUSPENDED
    // and it should have a blocking pipe set. If GetBlockingPipe returns -1 for a dummy thread, then
    // something is wrong - either CREATE_SUSPENDED wasn't used or the process was already resumed.
    if (pthrTarget->IsDummy() && -1 == pthrTarget->suspensionInfo.GetBlockingPipe())
    {
        palError = ERROR_INVALID_HANDLE;
        ERROR("Tried to wake up dummy thread without a blocking pipe.\n");
        ReleaseSuspensionLocks(pthrResumer, pthrTarget);
        goto InternalResumeThreadFromDataExit;            
    }

    dwPrevSuspendCount = pthrTarget->suspensionInfo.GetSuspCount();

    // If there is a blocking pipe on this thread, resume it by writing the wake up code to that pipe.
    if (-1 != pthrTarget->suspensionInfo.GetBlockingPipe())
    {
        // If write() is interrupted by a signal before writing data, 
        // it returns -1 and sets errno to EINTR. In this case, we
        // attempt the write() again.
        writeAgain:
        nWrittenBytes = write(pthrTarget->suspensionInfo.GetBlockingPipe(), &WAKEUPCODE, sizeof(WAKEUPCODE));

        // The size of WAKEUPCODE is 1 byte. If write returns 0, we'll treat it as an error.
        if (sizeof(WAKEUPCODE) != nWrittenBytes)
        {
            // If we are here during process creation, this is most likely caused by the target 
            // process dying before reaching this point and thus breaking the pipe.
            if (nWrittenBytes == -1 && EPIPE == errno)
            {
                palError = ERROR_INVALID_HANDLE;
                ReleaseSuspensionLocks(pthrResumer, pthrTarget);
                ERROR("Write failed with EPIPE\n");
                goto InternalResumeThreadFromDataExit;
            }
            else if (nWrittenBytes == 0 || (nWrittenBytes == -1 && EINTR == errno))
            {
                TRACE("write() failed with EINTR; re-attempting write\n");
                goto writeAgain;
            }
            else
            {
                // Some other error occurred; need to release suspension mutexes before leaving ResumeThread.
                palError = ERROR_INTERNAL_ERROR;
                ReleaseSuspensionLocks(pthrResumer, pthrTarget);
                ASSERT("Write() failed; error is %d (%s)\n", errno, strerror(errno));
                goto InternalResumeThreadFromDataExit;
            }
        }

        // Reset blocking pipe to -1 since we're done using it.
        pthrTarget->suspensionInfo.SetBlockingPipe(-1);
        
        ReleaseSuspensionLocks(pthrResumer, pthrTarget);
        goto InternalResumeThreadFromDataExit;
    }

    // Check if the target thread was actually suspended.
    // Note that calling ResumeThread on an executing thread
    // is not an error; palError remains NO_ERROR and
    // dwPrevSuspendCount is still 0.
    if (pthrTarget->suspensionInfo.GetSuspCount() == 0)
    {
        ReleaseSuspensionLocks(pthrResumer, pthrTarget);
        goto InternalResumeThreadFromDataExit;
    }

    // Notice that the suspension count is decremented after setting dwPrevSuspendCount above.
    pthrTarget->suspensionInfo.DecrSuspCount();

#ifdef _DEBUG
    // The selfsusp flag is checked after acquiring the suspension locks and
    // making sure the thread was suspended. This prevents the selfsusp flag
    // from being FALSE if the resume call occurs before the target sets its
    // selfsusp field to TRUE. Note that a self suspending thread resets 
    // its selfsusp field to FALSE after being resumed from suspension.

    // If the target thread was not self suspended and the resuming thread
    // has suspended other threads, then this resume attempt allows the
    // resuming thread to decrement its count of threads it suspended.
    if (!pthrTarget->suspensionInfo.GetSelfSusp())
    {
        if (pthrResumer->suspensionInfo.GetNumThreadsSuspendedByThisThread() <= 0)
        {
            // No error is set here since this is not a logic error.
            ReleaseSuspensionLocks(pthrResumer, pthrTarget);
            ASSERT("SUSPENSION DIAGNOSTIC FAILURE: Resuming thread hasn't suspended a thread\n");
            goto InternalResumeThreadFromDataExit;      
        }
        else
        {
            fDecrementSuspenderCount = TRUE;
        }
    }
#endif // _DEBUG

    // If the target's suspension count is now zero, it's ready to be resumed.
    if (pthrTarget->suspensionInfo.GetSuspCount() == 0)
    {
#if USE_PTHREAD_CONDVARS
        // We must initialize the flag indicating the target has acknowledged the resumption
        // before they could possibly have set it.
        m_fResumed = FALSE;
#endif

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
        pthrTarget->suspensionInfo.SetResumeSignalSent(TRUE);
        dwPthreadRet = pthread_kill(pthrTarget->GetPThreadSelf(), SIGUSR2);
        if (dwPthreadRet == 0)
        {
            pthrTarget->suspensionInfo.WaitOnResumeSemaphore();
        }
        else
        {
            // Resuming the thread failed so increment the suspension count.
            palError = ERROR_INVALID_HANDLE;    
            pthrTarget->suspensionInfo.IncrSuspCount();
            ReleaseSuspensionLocks(pthrResumer, pthrTarget);      
            ASSERT("pthread_kill failed with error %d\n", dwPthreadRet);    
            goto InternalResumeThreadFromDataExit;
        }   
#else //USE_SIGNALS_FOR_THREAD_SUSPENSION
        if(!THREADHandleResumeNative(pthrTarget))
        {
            palError = ERROR_INVALID_HANDLE;
            pthrTarget->suspensionInfo.IncrSuspCount();
            ReleaseSuspensionLocks(pthrResumer, pthrTarget);
            ASSERT("Native resumption actually failed!\n");
            goto InternalResumeThreadFromDataExit;
        }
#endif //USE_SIGNALS_FOR_THREAD_SUSPENSION
    }

    ReleaseSuspensionLocks(pthrResumer, pthrTarget);

    InternalResumeThreadFromDataExit:

    if (NO_ERROR == palError)
    {
        *pdwSuspendCount = dwPrevSuspendCount;

#ifdef _DEBUG
        // Decrementing the resumer thread's count of threads it suspended
        // if it's not resuming a self suspended thread.
        if (fDecrementSuspenderCount)
        {
            pthrResumer->suspensionInfo.DecrNumThreadsSuspendedByThisThread();
        }
#endif
    }

    pthrResumer->suspensionInfo.SetPerformingSuspension(FALSE);
    
    return palError;    
}
  
/*++
Function:
  TryAcquireSuspensionLock

TryAcquireSuspensionLock is a utility function that tries to acquire a thread's
suspension mutex or spinlock. If it succeeds, the function returns TRUE. 
Otherwise, it returns FALSE. This function is used in AcquireSuspensionLocks.
Note that the global lock cannot be acquired in this function since it makes
no sense to do so. A thread holding the global lock is the only thread that
can perform suspend or resume operations so it doesn't need to acquire
a second lock.
--*/
BOOL 
CThreadSuspensionInfo::TryAcquireSuspensionLock(
    CPalThread* pthrTarget
    )
{
    int iPthreadRet = 0;
#if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
{
    iPthreadRet = SPINLOCKTryAcquire(pthrTarget->suspensionInfo.GetSuspensionSpinlock());
}
#else // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
{
    iPthreadRet = pthread_mutex_trylock(pthrTarget->suspensionInfo.GetSuspensionMutex());
    _ASSERT_MSG(iPthreadRet == 0 || iPthreadRet == EBUSY, "pthread_mutex_trylock returned %d\n", iPthreadRet);
}
#endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX

    // If iPthreadRet is 0, lock acquisition was successful. Otherwise, it failed.
    return (iPthreadRet == 0);
}

/*++
Function:
  AcquireSuspensionLock

AcquireSuspensionLock acquires a thread's suspension mutex or spinlock. 
If USE_GLOBAL_LOCK_FOR_SUSPENSION is defined, it will acquire the global lock. 
A thread in this function blocks until it acquires
its lock, unlike in TryAcquireSuspensionLock.
--*/
void 
CThreadSuspensionInfo::AcquireSuspensionLock(
    CPalThread* pthrCurrent
    )
{
#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
{
    SPINLOCKAcquire(&g_ssSuspensionLock, 0);
}
#else // USE_GLOBAL_LOCK_FOR_SUSPENSION
{
    #if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
    {
        SPINLOCKAcquire(&pthrCurrent->suspensionInfo.m_nSpinlock, 0);
    }
    #else // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
    {
        int iPthreadError = pthread_mutex_lock(&pthrCurrent->suspensionInfo.m_ptmSuspmutex);
        _ASSERT_MSG(iPthreadError == 0, "pthread_mutex_lock returned %d\n", iPthreadError);
    }
    #endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
}
#endif // USE_GLOBAL_LOCK_FOR_SUSPENSION
}

/*++
Function:
  ReleaseSuspensionLock

ReleaseSuspensionLock is a function that releases a thread's suspension mutex
or spinlock. If USE_GLOBAL_LOCK_FOR_SUSPENSION is defined, 
it will release the global lock.
--*/
void 
CThreadSuspensionInfo::ReleaseSuspensionLock(
    CPalThread* pthrCurrent
    )
{
#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
{
    SPINLOCKRelease(&g_ssSuspensionLock);
}
#else // USE_GLOBAL_LOCK_FOR_SUSPENSION
{
    #if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX 
    {
        SPINLOCKRelease(&pthrCurrent->suspensionInfo.m_nSpinlock);
    }
    #else // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX 
    {
        int iPthreadError = pthread_mutex_unlock(&pthrCurrent->suspensionInfo.m_ptmSuspmutex);
        _ASSERT_MSG(iPthreadError == 0, "pthread_mutex_unlock returned %d\n", iPthreadError);
    }
    #endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX 
}
#endif // USE_GLOBAL_LOCK_FOR_SUSPENSION
}

/*++
Function:
  AcquireSuspensionLocks

AcquireSuspensionLocks is used to acquire the suspension locks
of a suspender (or resumer) and target thread. The thread will 
perform a blocking call to acquire its own suspension lock
and will then try to acquire the target thread's lock without blocking. 
If it fails to acquire the target's lock, it releases its own lock 
and the thread will try to acquire both locks again. The key 
is that both locks must be acquired together.

Originally, only blocking calls were used to acquire the suspender
and the target lock. However, this was problematic since a thread
could acquire its own lock and then block on acquiring the target
lock. In the meantime, the target could have already acquired its
own lock and be attempting to suspend the suspender thread. This 
clearly causes deadlock. A second approach used locking hierarchies,
where locks were acquired use thread id ordering. This was better but
suffered from the scenario where thread A acquires thread B's
suspension mutex first. In the meantime, thread C acquires thread A's
suspension mutex and its own. Thus, thread A is suspended while
holding thread B's mutex. This is problematic if thread C now wants
to suspend thread B. The issue here is that a thread can be
suspended while holding someone else's mutex but not holding its own.
In the end, the correct approach is to always acquire your suspension 
mutex first. This prevents you from being suspended while holding the 
target's mutex. Then, attempt to acquire the target's mutex. If the mutex 
cannot be acquired, release your own and try again. This all or nothing 
approach is the safest and avoids nasty race conditions.

If USE_GLOBAL_LOCK_FOR_SUSPENSION is defined, the calling thread 
will acquire the global lock when possible.
--*/
VOID
CThreadSuspensionInfo::AcquireSuspensionLocks(
    CPalThread *pthrSuspender,
    CPalThread *pthrTarget
    )
{
    BOOL fReacquire = FALSE;

#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
    AcquireSuspensionLock(pthrSuspender);
#else // USE_GLOBAL_LOCK_FOR_SUSPENSION
    do
    {
        fReacquire = FALSE;
        AcquireSuspensionLock(pthrSuspender);
        if (!TryAcquireSuspensionLock(pthrTarget))
        {
            // pthread_mutex_trylock returned EBUSY so release the first lock and try again.
            ReleaseSuspensionLock(pthrSuspender);           
            fReacquire = TRUE;
            sched_yield();
        }
    } while (fReacquire);
#endif // USE_GLOBAL_LOCK_FOR_SUSPENSION

    // Whenever the native implementation for the wait subsystem's thread 
    // blocking requires a lock as protection (as pthread conditions do with 
    // the associated mutex), we need to grab that lock to prevent the target 
    // thread from being suspended while holding the lock.
    // Failing to do so can lead to a multiple threads deadlocking such as the 
    // one described in VSW 363793.
    // In general, in similar scenarios, we need to grab the protecting lock 
    // every time suspension safety/unsafety is unbalanced on the two sides 
    // using the same condition (or any other native blocking support which 
    // needs an associated native lock), i.e. when either the signaling 
    // thread(s) is(are) signaling from an unsafe area and the waiting 
    // thread(s) is(are) waiting from a safe one, or vice versa (the scenario
    // described in VSW 363793 is a good example of the first type of 
    // unbalanced suspension safety/unsafety).
    // Instead, whenever signaling and waiting sides are both marked safe or 
    // unsafe, the deadlock cannot take place since either the suspending 
    // thread will suspend them anyway (regardless of the native lock), or it 
    // won't suspend any of them, since they are both marked unsafe.
    // Such a balanced scenario applies, for instance, to critical sections 
    // where depending on whether the target CS is internal or not, both the
    // signaling and the waiting side will access the mutex/condition from 
    // respectively an unsafe or safe region.
    
    pthrTarget->AcquireNativeWaitLock();
}

/*++
Function:
  ReleaseSuspensionLocks

ReleaseSuspensionLocks releases both thread's suspension mutexes.
Note that the locks are released in the opposite order they're acquired.
This prevents a suspending or resuming thread from being suspended
while holding the target's lock.
If USE_GLOBAL_LOCK_FOR_SUSPENSION is defined, it simply releases the global lock.
--*/
VOID
CThreadSuspensionInfo::ReleaseSuspensionLocks(
    CPalThread *pthrSuspender,
    CPalThread *pthrTarget
    )
{
    // See comment in AcquireSuspensionLocks    
    pthrTarget->ReleaseNativeWaitLock();

#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
    ReleaseSuspensionLock(pthrSuspender);
#else // USE_GLOBAL_LOCK_FOR_SUSPENSION
    ReleaseSuspensionLock(pthrTarget);
    ReleaseSuspensionLock(pthrSuspender);
#endif // USE_GLOBAL_LOCK_FOR_SUSPENSION
}

/*++
Function:
  PostOnSuspendSemaphore

PostOnSuspendSemaphore is a utility function for a thread
to post on its POSIX or SysV suspension semaphore.
--*/
void
CThreadSuspensionInfo::PostOnSuspendSemaphore()
{
#if USE_POSIX_SEMAPHORES
    if (sem_post(&m_semSusp) == -1)
    {
        ASSERT("sem_post returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_SYSV_SEMAPHORES
    if (semop(m_nSemsuspid, &m_sbSempost, 1) == -1)
    {
        ASSERT("semop - post returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_PTHREAD_CONDVARS
    int status;

    // The suspending thread may not have entered the wait yet, in which case the cond var
    // signal below will be a no-op. To prevent the race condition we set m_fSuspended to
    // TRUE first (which the suspender will take as am indication that no wait is required).
    // But the setting of the flag and the signal must appear atomic to the suspender (as
    // reading the flag and potentially waiting must appear to us) to avoid the race
    // condition where the suspender reads the flag as FALSE, we set it and signal and the
    // suspender then waits.

    // Acquire the suspend mutex. Once we enter the critical section the suspender has
    // either gotten there before us (and is waiting for our signal) or is yet to even
    // check the flag (so we can set it here to stop them attempting a wait).
    status = pthread_mutex_lock(&m_mutexSusp);
    if (status != 0)
    {
        ASSERT("pthread_mutex_lock returned %d (%s)\n", status, strerror(status));
    }

    m_fSuspended = TRUE;

    status = pthread_cond_signal(&m_condSusp);
    if (status != 0)
    {
        ASSERT("pthread_cond_signal returned %d (%s)\n", status, strerror(status));
    }

    status = pthread_mutex_unlock(&m_mutexSusp);
    if (status != 0)
    {
        ASSERT("pthread_mutex_unlock returned %d (%s)\n", status, strerror(status));
    }
#endif // USE_POSIX_SEMAPHORES
}

/*++
Function:
  WaitOnSuspendSemaphore

WaitOnSuspendSemaphore is a utility function for a thread
to wait on its POSIX or SysV suspension semaphore. 
--*/
void
CThreadSuspensionInfo::WaitOnSuspendSemaphore()
{
#if USE_POSIX_SEMAPHORES
    while (sem_wait(&m_semSusp) == -1)
    {
        ASSERT("sem_wait returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_SYSV_SEMAPHORES
    while (semop(m_nSemsuspid, &m_sbSemwait, 1) == -1)
    {
        ASSERT("semop wait returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_PTHREAD_CONDVARS
    int status;

    // By the time we wait the target thread may have already signalled its suspension (in
    // which case m_fSuspended will be TRUE and we shouldn't wait on the cond var). But we
    // must check the flag and potentially wait atomically to avoid the race where we read
    // the flag and the target thread sets it and signals before we have a chance to wait.

    status = pthread_mutex_lock(&m_mutexSusp);
    if (status != 0)
    {
        ASSERT("pthread_mutex_lock returned %d (%s)\n", status, strerror(status));
    }

    // If the target has already acknowledged the suspend we shouldn't wait.
    if (!m_fSuspended)
    {
        // We got here before the target could signal. Wait on them (which atomically releases
        // the mutex during the wait).
        status = pthread_cond_wait(&m_condSusp, &m_mutexSusp);
        if (status != 0)
        {
            ASSERT("pthread_cond_wait returned %d (%s)\n", status, strerror(status));
        }
    }

    status = pthread_mutex_unlock(&m_mutexSusp);
    if (status != 0)
    {
        ASSERT("pthread_mutex_unlock returned %d (%s)\n", status, strerror(status));
    }
#endif // USE_POSIX_SEMAPHORES
}

/*++
Function:
  PostOnResumeSemaphore

PostOnResumeSemaphore is a utility function for a thread
to post on its POSIX or SysV resume semaphore.
--*/
void
CThreadSuspensionInfo::PostOnResumeSemaphore()
{
#if USE_POSIX_SEMAPHORES
    if (sem_post(&m_semResume) == -1)
    {
        ASSERT("sem_post returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_SYSV_SEMAPHORES
    if (semop(m_nSemrespid, &m_sbSempost, 1) == -1)
    {
        ASSERT("semop - post returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_PTHREAD_CONDVARS
    int status;

    // The resuming thread may not have entered the wait yet, in which case the cond var
    // signal below will be a no-op. To prevent the race condition we set m_fResumed to
    // TRUE first (which the resumer will take as am indication that no wait is required).
    // But the setting of the flag and the signal must appear atomic to the resumer (as
    // reading the flag and potentially waiting must appear to us) to avoid the race
    // condition where the resumer reads the flag as FALSE, we set it and signal and the
    // resumer then waits.

    // Acquire the resume mutex. Once we enter the critical section the resumer has
    // either gotten there before us (and is waiting for our signal) or is yet to even
    // check the flag (so we can set it here to stop them attempting a wait).
    status = pthread_mutex_lock(&m_mutexResume);
    if (status != 0)
    {
        ASSERT("pthread_mutex_lock returned %d (%s)\n", status, strerror(status));
    }

    m_fResumed = TRUE;

    status = pthread_cond_signal(&m_condResume);
    if (status != 0)
    {
        ASSERT("pthread_cond_signal returned %d (%s)\n", status, strerror(status));
    }

    status = pthread_mutex_unlock(&m_mutexResume);
    if (status != 0)
    {
        ASSERT("pthread_mutex_unlock returned %d (%s)\n", status, strerror(status));
    }
#endif // USE_POSIX_SEMAPHORES
}

/*++
Function:
  WaitOnResumeSemaphore

WaitOnResumeSemaphore is a utility function for a thread
to wait on its POSIX or SysV resume semaphore.
--*/
void
CThreadSuspensionInfo::WaitOnResumeSemaphore()
{
#if USE_POSIX_SEMAPHORES
    while (sem_wait(&m_semResume) == -1)
    {
        ASSERT("sem_wait returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_SYSV_SEMAPHORES
    while (semop(m_nSemrespid, &m_sbSemwait, 1) == -1)
    {
        ASSERT("semop wait returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_PTHREAD_CONDVARS
    int status;

    // By the time we wait the target thread may have already signalled its resumption (in
    // which case m_fResumed will be TRUE and we shouldn't wait on the cond var). But we
    // must check the flag and potentially wait atomically to avoid the race where we read
    // the flag and the target thread sets it and signals before we have a chance to wait.

    status = pthread_mutex_lock(&m_mutexResume);
    if (status != 0)
    {
        ASSERT("pthread_mutex_lock returned %d (%s)\n", status, strerror(status));
    }

    // If the target has already acknowledged the resume we shouldn't wait.
    if (!m_fResumed)
    {
        // We got here before the target could signal. Wait on them (which atomically releases
        // the mutex during the wait).
        status = pthread_cond_wait(&m_condResume, &m_mutexResume);
        if (status != 0)
        {
            ASSERT("pthread_cond_wait returned %d (%s)\n", status, strerror(status));
        }
    }

    status = pthread_mutex_unlock(&m_mutexResume);
    if (status != 0)
    {
        ASSERT("pthread_mutex_unlock returned %d (%s)\n", status, strerror(status));
    }
#endif // USE_POSIX_SEMAPHORES
}
      
/*++
Function:
  LeaveUnsafeRegion
  
LeaveUnsafeRegion decrements a thread's unsafe region count.
Once the count is zero, the calling thread checks if there
is a suspension pending on it. If so, it will post on its
suspension semaphore and wait for the suspending thread to
suspend it. LeaveUnsafeRegion should only be called after
the thread called EnterUnsafeRegion. 
--*/
VOID
CThreadSuspensionInfo::LeaveUnsafeRegion()
{
    if (PALIsThreadDataInitialized())
    {
        _ASSERT_MSG(GetUnsafeRegionCount() > 0, "When entering LeaveUnsafeRegion, a thread's unsafe region count should always be greater than zero.\n");

        // Predecrement the unsafe region count
        DecrUnsafeRegionCount();
        if (GetUnsafeRegionCount() == 0) 
        {
            if (GetSuspPending())
            {
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
                pthread_sigmask(SIG_BLOCK, &smWaitmask, &this->m_smOrigmask);
                PostOnSuspendSemaphore();
                sigsuspend(&smContmask);
                // Set the signal mask that came before this sigsuspend.
                pthread_sigmask(SIG_SETMASK, &this->m_smOrigmask, NULL);
#else // USE_SIGNALS_FOR_THREAD_SUSPENSION
                PostOnSuspendSemaphore();
                while (GetSuspPending())
                {
                    sched_yield();
                }
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
            }
        }
    }
}

/*++
Function:
  EnterUnsafeRegion
  
EnterUnsafeRegion increments a thread's unsafe region count.
When a thread's unsafe region count is greater than zero,
it cannot be suspended. Thus, this function must be used
very carefully since thread suspension is required during
PAL cleanup. LeaveUnsafeRegion is used to decrement a thread's
suspension count.
--*/
VOID
CThreadSuspensionInfo::EnterUnsafeRegion()
{
    if (PALIsThreadDataInitialized())
    {
        IncrUnsafeRegionCount();
    }
}

#if !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION
/*++
Function:
  InitializeSignalSets
  
InitializeSignalSets initializes the signal masks used for thread
suspension operations. Each thread's signal mask is initially set
to smDefaultMask in InitializePreCreate. This mask blocks SIGUSR2,
and SIGUSR1 if suspension using signals is off. This mask
also blocks common signals so they will be handled by the PAL's
signal handling thread. 
--*/
VOID
CThreadSuspensionInfo::InitializeSignalSets()
{
#if !HAVE_MACH_EXCEPTIONS
    sigemptyset(&smDefaultmask);
    
#ifndef DO_NOT_USE_SIGNAL_HANDLING_THREAD
    // The default signal mask masks all common signals except those that represent 
    // synchronous exceptions in the PAL or are used by the system (e.g. SIGPROF on BSD).
    // Note that SIGPROF is used by the BSD thread scheduler and masking it caused a 
    // significant reduction in performance.
    sigaddset(&smDefaultmask, SIGHUP);  
    sigaddset(&smDefaultmask, SIGABRT); 
#ifdef SIGEMT
    sigaddset(&smDefaultmask, SIGEMT); 
#endif
    sigaddset(&smDefaultmask, SIGSYS); 
    sigaddset(&smDefaultmask, SIGALRM); 
    sigaddset(&smDefaultmask, SIGTERM);     
    sigaddset(&smDefaultmask, SIGURG); 
    sigaddset(&smDefaultmask, SIGTSTP); 
    sigaddset(&smDefaultmask, SIGCONT);   
    sigaddset(&smDefaultmask, SIGCHLD);       
    sigaddset(&smDefaultmask, SIGTTIN); 
    sigaddset(&smDefaultmask, SIGTTOU);    
    sigaddset(&smDefaultmask, SIGIO); 
    sigaddset(&smDefaultmask, SIGXCPU);    
    sigaddset(&smDefaultmask, SIGXFSZ); 
    sigaddset(&smDefaultmask, SIGVTALRM); 
    sigaddset(&smDefaultmask, SIGWINCH); 
#ifdef SIGINFO
    sigaddset(&smDefaultmask, SIGINFO); 
#endif
    sigaddset(&smDefaultmask, SIGPIPE);  
    sigaddset(&smDefaultmask, SIGUSR2);
    
    #if !USE_SIGNALS_FOR_THREAD_SUSPENSION
    {
        // Don't mask SIGUSR1 if using signal suspension since SIGUSR1 is needed
        // to suspend threads.
        sigaddset(&smDefaultmask, SIGUSR1);
    }
    #endif // !USE_SIGNALS_FOR_THREAD_SUSPENSION
#endif // DO_NOT_USE_SIGNAL_HANDLING_THREAD
#endif // !HAVE_MACH_EXCEPTIONS

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
#if !HAVE_MACH_EXCEPTIONS
    #ifdef DO_NOT_USE_SIGNAL_HANDLING_THREAD
    {
        // If the SWT is turned on, SIGUSR2 was already added to the mask. 
        // Otherwise, add it to the mask now.
        sigaddset(&smDefaultmask, SIGUSR2);
    }
    #endif
#endif // !HAVE_MACH_EXCEPTIONS

    // smContmask is used to allow a thread to accept a SIGUSR1 when in sigsuspend, 
    // after a pending suspension
    sigfillset(&smContmask);
    sigdelset(&smContmask, SIGUSR1);

    // smSuspmask is used in sigsuspend during a safe suspension attept.
    sigfillset(&smSuspmask);
    sigdelset(&smSuspmask, SIGUSR2);

    // smWaitmask forces a thread to wait for a SIGUSR1 during a suspension retry
    sigemptyset(&smWaitmask);
    sigaddset(&smWaitmask, SIGUSR1);
    sigaddset(&smWaitmask, SIGUSR2);   
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
}
#endif // !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION

/*++
Function:
  InitializeSuspensionLock

InitializeSuspensionLock initializes a thread's suspension spinlock
or suspension mutex. It is called from the CThreadSuspensionInfo
constructor.
--*/
VOID
CThreadSuspensionInfo::InitializeSuspensionLock()
{
#if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
    SPINLOCKInit(&m_nSpinlock);
#else
    int iError = pthread_mutex_init(&m_ptmSuspmutex, NULL);
    if (0 != iError )
    {
        ASSERT("pthread_mutex_init(&suspmutex) returned %d\n", iError);
        return;
    }
    m_fSuspmutexInitialized = TRUE;
#endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
}

/*++
Function:
  InitializePreCreate

InitializePreCreate initializes the semaphores and signal masks used 
for thread suspension. At the end, it sets the calling thread's 
signal mask to the default signal mask. 
--*/
PAL_ERROR
CThreadSuspensionInfo::InitializePreCreate()
{
    PAL_ERROR palError = ERROR_INTERNAL_ERROR;
    int iError = 0;
#if SEM_INIT_MODIFIES_ERRNO
    int nStoredErrno;
#endif  // SEM_INIT_MODIFIES_ERRNO

#if USE_POSIX_SEMAPHORES

#if SEM_INIT_MODIFIES_ERRNO
    nStoredErrno = errno;
#endif  // SEM_INIT_MODIFIES_ERRNO

    // initialize suspension semaphore
    iError = sem_init(&m_semSusp, 0, 0);  

#if SEM_INIT_MODIFIES_ERRNO
    if (iError == 0)
    {
        // Restore errno if sem_init succeeded.
        errno = nStoredErrno;
    }
#endif  // SEM_INIT_MODIFIES_ERRNO

    if (0 != iError )
    {
        ASSERT("sem_init(&suspsem) returned %d\n", iError);
        goto InitializePreCreateExit;
    }

#if SEM_INIT_MODIFIES_ERRNO
    nStoredErrno = errno;
#endif  // SEM_INIT_MODIFIES_ERRNO

    // initialize resume semaphore
    iError = sem_init(&m_semResume, 0, 0);

#if SEM_INIT_MODIFIES_ERRNO
    if (iError == 0)
    {
        // Restore errno if sem_init succeeded.
        errno = nStoredErrno;
    }
#endif  // SEM_INIT_MODIFIES_ERRNO

    if (0 != iError )
    {
        ASSERT("sem_init(&suspsem) returned %d\n", iError);
        sem_destroy(&m_semSusp);
        goto InitializePreCreateExit;
    }

    m_fSemaphoresInitialized = TRUE;

#elif USE_SYSV_SEMAPHORES
    // preparing to initialize the SysV semaphores.
    union semun semunData;
    m_nSemsuspid = semget(IPC_PRIVATE, 1, IPC_CREAT | 0666);
    if (m_nSemsuspid == -1)
    {
        ASSERT("semget for suspension sem id returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
        goto InitializePreCreateExit;
    }
    
    m_nSemrespid = semget(IPC_PRIVATE, 1, IPC_CREAT | 0666);
    if (m_nSemrespid == -1)
    {
        ASSERT("semget for resumption sem id returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
        goto InitializePreCreateExit;
    }

    if (m_nSemsuspid == m_nSemrespid)
    {
        ASSERT("Suspension and Resumption Semaphores have the same id\n");
        goto InitializePreCreateExit;
    }

    semunData.val = 0;
    iError = semctl(m_nSemsuspid, 0, SETVAL, semunData);
    if (iError == -1)
    {
        ASSERT("semctl for suspension sem id returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
        goto InitializePreCreateExit;
    }

    semunData.val = 0;
    iError = semctl(m_nSemrespid, 0, SETVAL, semunData);
    if (iError == -1)
    {
        ASSERT("semctl for resumption sem id returned -1 and set errno to %d (%s)\n", errno, strerror(errno));
        goto InitializePreCreateExit;
    }
    
    // initialize suspend semaphore
    m_sbSemwait.sem_num = 0;
    m_sbSemwait.sem_op = -1;
    m_sbSemwait.sem_flg = 0;

    // initialize resume semaphore
    m_sbSempost.sem_num = 0;
    m_sbSempost.sem_op = 1;
    m_sbSempost.sem_flg = 0;    
#elif USE_PTHREAD_CONDVARS
    iError = pthread_cond_init(&m_condSusp, NULL);
    if (iError != 0)
    {
        ASSERT("pthread_cond_init for suspension returned %d (%s)\n", iError, strerror(iError));
        goto InitializePreCreateExit;
    }

    iError = pthread_mutex_init(&m_mutexSusp, NULL);
    if (iError != 0)
    {
        ASSERT("pthread_mutex_init for suspension returned %d (%s)\n", iError, strerror(iError));
        goto InitializePreCreateExit;
    }

    iError = pthread_cond_init(&m_condResume, NULL);
    if (iError != 0)
    {
        ASSERT("pthread_cond_init for resume returned %d (%s)\n", iError, strerror(iError));
        goto InitializePreCreateExit;
    }

    iError = pthread_mutex_init(&m_mutexResume, NULL);
    if (iError != 0)
    {
        ASSERT("pthread_mutex_init for resume returned %d (%s)\n", iError, strerror(iError));
        goto InitializePreCreateExit;
    }

    m_fSemaphoresInitialized = TRUE;
#endif // USE_POSIX_SEMAPHORES

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
    // m_smOrigmask is used to restore a thread's signal mask. 
    // This is not needed for sigsuspend operations since sigsuspend 
    // automatically restores the original mask
    sigemptyset(&m_smOrigmask);
#endif

#if !HAVE_MACH_EXCEPTIONS
    // This signal mask blocks SIGUSR2 when signal suspension is turned on
    // (SIGUSR2 must be blocked for signal suspension), and masks other signals
    // when the signal waiting thread is turned on. We must use SIG_SETMASK 
    // so all threads start with the same signal mask. Otherwise, issues can arise.
    // For example, on BSD using suspension with signals, the control handler 
    // routine thread, spawned from the signal handling thread, inherits the 
    // signal handling thread's mask which blocks SIGUSR1. Thus, the
    // control handler routine thread cannot be suspended. Using SETMASK 
    // ensures that SIGUSR1 is not blocked.
    
    iError = pthread_sigmask(SIG_SETMASK, &smDefaultmask, NULL);
    if (iError != 0)
    {
        ASSERT("pthread sigmask(SIG_SETMASK, &smDefaultmask) returned %d\n", iError);
        goto InitializePreCreateExit;
    }
#endif // !HAVE_MACH_EXCEPTIONS

    // Initialization was successful.
    palError = NO_ERROR;
    
InitializePreCreateExit:

    if (NO_ERROR == palError && 0 != iError)
    {
        switch (iError)
        {
            case ENOMEM:
            case EAGAIN:
            {
                palError = ERROR_OUTOFMEMORY;
                break;
            }
            default:
            {
                ASSERT("A pthrSuspender init call returned %d (%s)\n", iError, strerror(iError));
                palError = ERROR_INTERNAL_ERROR;
            }
        }
    }

    return palError;
}

CThreadSuspensionInfo::~CThreadSuspensionInfo()
{
#if !DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX                
    if (m_fSuspmutexInitialized)
    {
        int iError = pthread_mutex_destroy(&m_ptmSuspmutex);
        _ASSERT_MSG(0 == iError, "pthread_mutex_destroy returned %d (%s)\n", iError, strerror(iError));
    }
#endif

#if USE_POSIX_SEMAPHORES
    if (m_fSemaphoresInitialized)
    {
        int iError;

        iError = sem_destroy(&m_semSusp);
        _ASSERT_MSG(0 == iError, "sem_destroy failed and set errno to %d (%s)\n", errno, strerror(errno));

        iError = sem_destroy(&m_semResume);
        _ASSERT_MSG(0 == iError, "sem_destroy failed and set errno to %d (%s)\n", errno, strerror(errno));
    }
#elif USE_SYSV_SEMAPHORES
    DestroySemaphoreIds();
#elif USE_PTHREAD_CONDVARS
    if (m_fSemaphoresInitialized)
    {
        int iError;

        iError = pthread_cond_destroy(&m_condSusp);
        _ASSERT_MSG(0 == iError, "pthread_cond_destroy failed with %d (%s)\n", iError, strerror(iError));

        iError = pthread_mutex_destroy(&m_mutexSusp);
        _ASSERT_MSG(0 == iError, "pthread_mutex_destroy failed with %d (%s)\n", iError, strerror(iError));

        iError = pthread_cond_destroy(&m_condResume);
        _ASSERT_MSG(0 == iError, "pthread_cond_destroy failed with %d (%s)\n", iError, strerror(iError));

        iError = pthread_mutex_destroy(&m_mutexResume);
        _ASSERT_MSG(0 == iError, "pthread_mutex_destroy failed with %d (%s)\n", iError, strerror(iError));
    }
#endif  // USE_POSIX_SEMAPHORES
}

#if USE_SYSV_SEMAPHORES
/*++
Function:
  DestroySemaphoreIds
  
DestroySemaphoreIds is called from the CThreadSuspensionInfo destructor and
from PROCCleanupThreadSemIds. If a thread exits before shutdown or is suspended
during shutdown, its destructor will be invoked and the semaphore ids destroyed. 
In assert or exceptions situations that are suspension unsafe, 
PROCCleanupThreadSemIds is called, which uses DestroySemaphoreIds.
--*/
void
CThreadSuspensionInfo::DestroySemaphoreIds()
{
    union semun semunData;
    if (m_nSemsuspid != 0)
    {
        semunData.val = 0;
        if (0 != semctl(m_nSemsuspid, 0, IPC_RMID, semunData))
        {
            ERROR("semctl(Semsuspid) failed and set errno to %d (%s)\n", errno, strerror(errno));
        }
        else
        {
            m_nSemsuspid = 0;
        }
    }
    if (this->m_nSemrespid)
    {
        semunData.val = 0;
        if (0 != semctl(m_nSemrespid, 0, IPC_RMID, semunData))
        {
            ERROR("semctl(Semrespid) failed and set errno to %d (%s)\n", errno, strerror(errno));
        }
        else
        {
            m_nSemrespid = 0;
        }
    }
}
#endif // USE_SYSV_SEMAPHORES

/*++
Function:
  IsAssertShutdownSafe
  
IsAssertShutdownSafe returns TRUE if a thread is in an unsafe region or in the
middle of a suspension attempt.
--*/
BOOL 
CThreadSuspensionInfo::IsAssertShutdownSafe()
{
    // returns TRUE if the thread is in a suspension safe region and not 
    // asserting from within InternalSuspend/ResumeThreadFromData.
    return (IsSuspensionStateSafe() && !IsPerformingSuspension());
}

/*++
Function:
  THREADMarkDiagnostic
  
THREADMarkDiagnostic is called in functions that may be suspension unsafe.
For the assert to be invoked, the calling thread must have suspended
other threads, in which case the suspended threads may be holding an internal
lock or resource required by the diagnostic function. If this assert shows
up, it at least warrants reviewing the function to decide if threads executing
in it should be marked as suspension unsafe.
--*/
#ifdef _DEBUG
void 
THREADMarkDiagnostic(const char* funcName)
{
    if (PALIsThreadDataInitialized())
    {
        CPalThread *pthrCurrent = InternalGetCurrentThread();
        _ASSERT_MSG(pthrCurrent->suspensionInfo.GetNumThreadsSuspendedByThisThread() == 0, 
            "SUSPENSION DIAGNOSTIC: %s is potentially suspension unsafe "
            "and was executed by a thread that suspended %d threads.\n", 
            funcName, pthrCurrent->suspensionInfo.GetNumThreadsSuspendedByThisThread());
    }
}
#endif // _DEBUG

/*++
Function:
  PALCIsSuspensionStateSafe

This function allows someone to check if a thread is in a suspension safe
state in legacy C code, which has no knowledge of CPalThread.
--*/
BOOL 
PALCIsSuspensionStateSafe(void)
{
    CPalThread *pthrCurrent = InternalGetCurrentThread();
    return pthrCurrent->suspensionInfo.IsSuspensionStateSafe();
}

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
/*++
Function:
  HandleSuspendSignal

Returns:
    true if the signal is expected by this PAL instance; false should 
    be chained to the next signal handler.
  
HandleSuspendSignal is called from within the SIGUSR1 handler. The thread
that invokes this function will suspend itself if it's suspension safe
or set its pending flag to TRUE and continue executing until it becomes
suspension safe.
--*/
bool
CThreadSuspensionInfo::HandleSuspendSignal(
    CPalThread *pthrTarget
    )
{
    if (!GetSuspendSignalSent())
    {
        return false;
    }

    SetSuspendSignalSent(FALSE);

    if (IsSuspensionStateSafe())
    {
        SetSuspPending(FALSE);
        if (!pthrTarget->GetCreateSuspended())
        {
            /* Note that we don't call sem_post when CreateSuspended is true. 
            This is to handle the scenario where a thread suspends itself and 
            another thread then attempts to suspend that thread. It won't wait 
            on the semaphore if the self suspending thread already posted 
            but didn't reach the matching wait. */
            PostOnSuspendSemaphore();
        }
        else 
        {
            pthrTarget->SetStartStatus(TRUE);
        }
        sigsuspend(&smSuspmask);
    }
    else
    {
        SetSuspPending(TRUE);
    }	

    return true;
}

/*++
Function:
  HandleResumeSignal

Returns:
    true if the signal is expected by this PAL instance; false should 
    be chained to the next signal handler.
  
HandleResumeSignal is called from within the SIGUSR2 handler. 
A thread suspended by sigsuspend will enter the SUGUSR2 handler
and reach this function, which checks that SIGUSR2 was sent
by InternalResumeThreadFromData and that the resumed thread
still has a positive suspend count. After these checks, the resumed
thread posts on its resume semaphore so the resuming thread can
continue execution.
--*/
bool
CThreadSuspensionInfo::HandleResumeSignal()
{
    if (!GetResumeSignalSent())
    {
        return false;
    }

    SetResumeSignalSent(FALSE);

    if (GetSuspCount() != 0)
    {
        ASSERT("Should not be resuming a thread whose suspension count is %d.\n", GetSuspCount());
        return true;
    }

    // This thread is no longer suspended - if it self suspended, 
    // then its self suspension field should now be set to FALSE.
    if (GetSelfSusp())
    {
        SetSelfSusp(FALSE);
    }

    PostOnResumeSemaphore();

    return true;
}
#else // USE_SIGNALS_FOR_THREAD_SUSPENSION

/*++
Function:
  THREADHandleSuspendNative
  
THREADHandleSuspendNative is called to suspend the target thread
using a platform's native suspension routine. This function
returns TRUE when the native suspension is successful and returns
FALSE if it fails.
--*/
BOOL 
CThreadSuspensionInfo::THREADHandleSuspendNative(CPalThread *pthrTarget)
{
    DWORD dwPthreadRet = 0;
    if (pthrTarget->GetCreateSuspended())
    {
        pthrTarget->SetStartStatus(TRUE);
    }
    
#if HAVE_PTHREAD_SUSPEND
    dwPthreadRet = pthread_suspend(pthrTarget->GetPThreadSelf());
#elif HAVE_MACH_THREADS
    dwPthreadRet = thread_suspend(pthread_mach_thread_np(pthrTarget->GetPThreadSelf()));
#elif HAVE_PTHREAD_SUSPEND_NP
#if SELF_SUSPEND_FAILS_WITH_NATIVE_SUSPENSION
    if (pthrTarget->suspensionInfo.GetSelfSusp())
    {
        pthrTarget->suspensionInfo.WaitOnSuspendSemaphore();   
    }
    else
#endif // SELF_SUSPEND_FAILS_WITH_NATIVE_SUSPENSION
    {
        dwPthreadRet = pthread_suspend_np(pthrTarget->GetPThreadSelf());
    }
#else
    #error "Don't know how to suspend threads on this platform!"
    return FALSE;
#endif

    // A self suspending thread that reaches this point would have been resumed
    // by a call to THREADHandleResumeNative. The self suspension has been 
    // completed so it can set its selfsusp flag to FALSE. Reset the selfsusp flag 
    // before checking the return value in case the suspend itself failed.
    if (pthrTarget->suspensionInfo.GetSelfSusp())
    {
        pthrTarget->suspensionInfo.SetSelfSusp(FALSE);
    }

    if (dwPthreadRet != 0)
    {
        ASSERT("[THREADHandleSuspendNative] native suspend_thread call failed [thread id=%d thread_state=%d errno=%d (%s)]\n", 
            pthrTarget->GetThreadId(), pthrTarget->synchronizationInfo.GetThreadState(), 
            dwPthreadRet, strerror(dwPthreadRet));
        return FALSE;
    }
    return TRUE;
}

/*++
Function:
  THREADHandleResumeNative
  
THREADHandleResumeNative is called to resume a thread that was
suspended using a platform's native suspension routine. This function
returns TRUE on success and FALSE on failure.
--*/
BOOL 
CThreadSuspensionInfo::THREADHandleResumeNative(CPalThread *pthrTarget)
{
    DWORD dwPthreadRet = 0; 
#if SELF_SUSPEND_FAILS_WITH_NATIVE_SUSPENSION  
    BOOL fResumedSelfSuspender = FALSE;
#endif
    
    /* The do-while loop is necessary for self suspension situations. 
    After a self suspending thread (thread A) releases its mutex, 
    there is a small window between it's handle being returned from CreateThread 
    (making it possible for other threads to perform suspension and resumption 
    operations on it) and the thread actually being suspended. 
    During the window, another thread (thread B) may call ResumeThread on A. 
    Because A's suspension count was already incremented, thread B believes 
    it was already suspended so it decrements the count and calls the native 
    continue function. Since A hadn't called the native suspend function yet, 
    the native resume function does nothing but B thinks A was resumed. 

    The workaround is to use a do while loop that checks the target thread's 
    self suspension field. The target sets its field to zero once it's resumed 
    after self suspending. The resuming thread calls continue in the loop 
    until the field is set to zero. Calling continue repeatedly is safe since 
    calling continue on an executing thread has no effect. Also, since the 
    resuming thread holds the target's suspension mutex, no other threads can 
    attempt to suspend or resume the target until the resume operation is complete. */

    do
    {
#if HAVE_PTHREAD_CONTINUE
        dwPthreadRet = pthread_continue(pthrTarget->GetPThreadSelf());
#elif HAVE_MACH_THREADS
        dwPthreadRet = thread_resume(pthread_mach_thread_np(pthrTarget->GetPThreadSelf()));
#elif HAVE_PTHREAD_CONTINUE_NP
        dwPthreadRet = pthread_continue_np((pthrTarget->GetPThreadSelf());
#elif HAVE_PTHREAD_RESUME_NP
#if SELF_SUSPEND_FAILS_WITH_NATIVE_SUSPENSION  
        if (pthrTarget->suspensionInfo.GetSelfSusp())
        {
            /* We only want to post on the target's semaphore once. We really 
            don't need to use the loop in this case since the self suspending
            thread will never suspend if the resumer already posted on its
            semaphore. Furthermore, we don't want to repeatedly post on the
            target's semaphore while waiting for it to set its selfSusp flag to
            FALSE. However, we'd prefer to enforce the behavior of the
            resumer waiting until it knows that the self suspending thread
            has continued execution. Thus, we use a flag to ensure that the
            post only occurs once. */
            if (!fResumedSelfSuspender)
            {
                pthrTarget->suspensionInfo.PostOnSuspendSemaphore();
                fResumedSelfSuspender = TRUE;
            }
        }
        else
#endif // SELF_SUSPEND_FAILS_WITH_NATIVE_SUSPENSION  
        {
            dwPthreadRet = pthread_resume_np(pthrTarget->GetPThreadSelf());
        }
#else
        #error "Don't know how to resume threads on this platform!"
        return FALSE;
#endif

        if (dwPthreadRet != 0
#if HAVE_MACH_THREADS
            && dwPthreadRet != KERN_FAILURE
            // Here, KERN_FAILURE is returned when calling thread_resume on an 
            // executing thread. This is inconsistent with other UNIX platforms, 
            // which return success when calling resume on an executing thread.
#endif // HAVE_MACH_THREADS
        )
        {
            ASSERT("[THREADHandleResumeNative] native suspend_thread call failed [lwpid=%d thread_state=%d errno=%d (%s)]\n", 
                pthrTarget->GetThreadId(), pthrTarget->synchronizationInfo.GetThreadState(), 
                dwPthreadRet, strerror(dwPthreadRet));
            return FALSE;
        }

        // Hopefully, the self suspension field will be set to zero before the while check. 
        // If not, we'll check again in the next do-while loop iteration.
        if (pthrTarget->suspensionInfo.GetSelfSusp())
        {
            sched_yield();
        }
    } while (pthrTarget->suspensionInfo.GetSelfSusp());
    return TRUE;
}
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

