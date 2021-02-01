// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
#include <unistd.h>
#include <errno.h>
#include <stddef.h>
#include <sys/stat.h>
#include <limits.h>
#include <debugmacrosext.h>

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

/*++
Function:
  InternalSuspendNewThreadFromData

  On platforms where we use pipes for starting threads suspended, this
  function sets the blocking pipe for the thread and blocks until the
  wakeup code is written to the pipe by ResumeThread.

--*/
PAL_ERROR
CThreadSuspensionInfo::InternalSuspendNewThreadFromData(
    CPalThread *pThread
    )
{
    PAL_ERROR palError = NO_ERROR;

    AcquireSuspensionLock(pThread);
    pThread->suspensionInfo.SetSelfSusp(TRUE);
    ReleaseSuspensionLock(pThread);

    int pipe_descs[2];
    int pipeRv =
#if HAVE_PIPE2
        pipe2(pipe_descs, O_CLOEXEC);
#else
        pipe(pipe_descs);
#endif // HAVE_PIPE2
    if (pipeRv == -1)
    {
        ERROR("pipe() failed! error is %d (%s)\n", errno, strerror(errno));
        return ERROR_NOT_ENOUGH_MEMORY;
    }
#if !HAVE_PIPE2
    fcntl(pipe_descs[0], F_SETFD, FD_CLOEXEC); // make pipe non-inheritable, if possible
    fcntl(pipe_descs[1], F_SETFD, FD_CLOEXEC);
#endif // !HAVE_PIPE2

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

    if (palError == NO_ERROR)
    {
        AcquireSuspensionLock(pThread);
        pThread->suspensionInfo.SetSelfSusp(FALSE);
        ReleaseSuspensionLock(pThread);
    }

    // Close the pipes regardless of whether we were successful.
    close(pipe_descs[0]);
    close(pipe_descs[1]);

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

    int nWrittenBytes = -1;

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
    else
    {
        *pdwSuspendCount = 0;
        palError = ERROR_BAD_COMMAND;
    }

InternalResumeThreadFromDataExit:

    if (NO_ERROR == palError)
    {
        *pdwSuspendCount = 1;
    }

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
        INDEBUG(int iPthreadError = )
        pthread_mutex_lock(&pthrCurrent->suspensionInfo.m_ptmSuspmutex);
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
        INDEBUG(int iPthreadError = )
        pthread_mutex_unlock(&pthrCurrent->suspensionInfo.m_ptmSuspmutex);
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
    // TRUE first (which the suspender will take as an indication that no wait is required).
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
    while (!m_fSuspended)
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
    // TRUE first (which the resumer will take as an indication that no wait is required).
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
    while (!m_fResumed)
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
        INDEBUG(int iError = )
        pthread_mutex_destroy(&m_ptmSuspmutex);
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
