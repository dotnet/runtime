// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    shmemory/shmemory.c

Abstract:

    Implementation of shared memory infrastructure for IPC



--*/

#include "pal/dbgmsg.h"
#include "pal/shmemory.h"
#include "pal/critsect.h"
#include "pal/process.h"

#if HAVE_YIELD_SYSCALL
#include <sys/syscall.h>
#endif  /* HAVE_YIELD_SYSCALL */

SET_DEFAULT_DEBUG_CHANNEL(SHMEM);

/* Type definitions ***********************************************************/

/*
SHM_HEADER
Global information about the shared memory system

The spinlock is used to ensure that only one process accesses shared memory at
the same time. A process can only take the spinlock if its contents is 0, and
it takes the spinlock by placing its PID in it. (this allows a process to catch
the special case where it tries to take a spinlock it already owns.
*/

typedef struct
{
    Volatile<pid_t> spinlock;
    Volatile<SHMPTR> shm_info[SIID_LAST]; /* basic blocks of shared information.*/
} SHM_HEADER;

static SHM_HEADER shm_header;

/* Static variables ***********************************************************/

/* Critical section to ensure that only one thread at a time accesses shared
memory. Rationale :
-Using a spinlock means that processes must busy-wait for the lock to be
 available. The critical section ensures taht only one thread will busy-wait,
 while the rest are put to sleep.
-Since the spinlock only contains a PID, it isn't possible to make a difference
 between threads of the same process. This could be resolved by using 2
 spinlocks, but this would introduce more busy-wait.
*/
static CRITICAL_SECTION shm_critsec;

/* number of locks the process currently holds (SHMLock calls without matching
SHMRelease). Because we take the critical section while inside a
SHMLock/SHMRelease pair, this is actually the number of locks held by a single
thread. */
static Volatile<LONG> lock_count;

/* thread ID of thread holding the SHM lock. used for debugging purposes :
   SHMGet/SetInfo will verify that the calling thread holds the lock */
static Volatile<HANDLE> locking_thread;

/* Public function implementations ********************************************/

/*++
SHMInitialize

Hook this process into the PAL shared memory system; initialize the shared
memory if no other process has done it.

--*/
BOOL SHMInitialize(void)
{
    InternalInitializeCriticalSection(&shm_critsec);

        TRACE("Now initializing global shared memory system\n");

        InterlockedExchange((LONG *)&shm_header.spinlock, 0);

        /* SHM information array starts with NULLs */
        memset((void *)shm_header.shm_info, 0, SIID_LAST*sizeof(SHMPTR));

        TRACE("Global shared memory initialization complete.\n");

    lock_count = 0;
    locking_thread = 0;

    return TRUE;
}

/*++
SHMCleanup

Release all shared memory resources held; remove ourselves from the list of
registered processes, and remove all shared memory files if no process remains

Note that this function does not use thread suspension wrapper for unlink and free
because all thread objects are deleted before this function is called
in PALCommonCleanup.

--*/
void SHMCleanup(void)
{
    pid_t my_pid;

    TRACE("Starting shared memory cleanup\n");

    SHMLock();
    SHMRelease();

    /* We should not be holding the spinlock at this point. If we are, release
       the spinlock. by setting it to 0 */
    my_pid = gPID;

    _ASSERT_MSG(shm_header.spinlock != my_pid,
            "SHMCleanup called while the current process still owns the lock "
            "[owner thread=%u, current thread: %u]\n",
            locking_thread.Load(), THREADSilentGetCurrentThreadId());

    /* Now for the interprocess stuff. */
    DeleteCriticalSection(&shm_critsec);

    TRACE("SHMCleanup complete!\n");
}

/*++
SHMLock

Restrict shared memory access to the current thread of the current process

(no parameters)

Return value :
    New lock count

Notes :
see comments at the declaration of shm_critsec for rationale of critical
section usage
--*/
int SHMLock(void)
{
    /* Hold the critical section until the lock is released */
    PALCEnterCriticalSection(&shm_critsec);

    _ASSERTE((0 == lock_count && 0 == locking_thread) ||
             (0 < lock_count && reinterpret_cast<HANDLE>(pthread_self()) == locking_thread));

    if(lock_count == 0)
    {
        pid_t my_pid, tmp_pid;
        int spincount = 1;

        TRACE("First-level SHM lock : taking spinlock\n");

        // Store the id of the current thread as the (only) one that is
        // trying to grab the spinlock from the current process
        locking_thread = reinterpret_cast<HANDLE>(pthread_self());

        my_pid = gPID;

        while(TRUE)
        {
            //
            // Try to grab the spinlock
            //
            tmp_pid = InterlockedCompareExchange((LONG *) &shm_header.spinlock, my_pid,0);

            if (0 == tmp_pid)
            {
                // Spinlock acquired: break out of the loop
                break;
            }

            /* Check if lock holder is alive. If it isn't, we can reset the
               spinlock and try to take it again. If it is, we have to wait.
               We use "spincount" to do this check only every 8th time through
               the loop, for performance reasons.*/
            if( (0 == (spincount&0x7)) &&
                (-1 == kill(tmp_pid,0)) &&
                (errno == ESRCH))
            {
                TRACE("SHM spinlock owner (%08x) is dead; releasing its lock\n",
                      tmp_pid);

                InterlockedCompareExchange((LONG *) &shm_header.spinlock, 0, tmp_pid);
            }
            else
            {
                /* another process is holding the lock... we want to yield and
                   give the holder a chance to release the lock
                   The function sched_yield() only yields to a thread in the
                   current process; this doesn't help us much, and doesn't help
                   at all if there's only 1 thread. There doesn't seem to be
                   any clean way to force a yield to another process, but the
                   FreeBSD syscall "yield" does the job. We alternate between
                   both methods to give other threads of this process a chance
                   to run while we wait.
                 */
#if HAVE_YIELD_SYSCALL
                if(spincount&1)
                {
#endif  /* HAVE_YIELD_SYSCALL */
                    sched_yield();
#if HAVE_YIELD_SYSCALL
                }
                else
                {
                    /* use the syscall first, since we know we'l need to yield
                       to another process eventually - the lock can't be held
                       by the current process, thanks to the critical section */
                    syscall(SYS_yield, 0);
                }
#endif  /* HAVE_YIELD_SYSCALL */
            }

            // Increment spincount
            spincount++;
        }

        _ASSERT_MSG(my_pid == shm_header.spinlock,
            "\n(my_pid = %u) != (header->spinlock = %u)\n"
            "tmp_pid         = %u\n"
            "spincount       = %d\n"
            "locking_thread  = %u\n",
            (DWORD)my_pid, (DWORD)shm_header.spinlock,
            (DWORD)tmp_pid,
            (int)spincount,
            (HANDLE)locking_thread);
    }

    lock_count++;
    TRACE("SHM lock level is now %d\n", lock_count.Load());
    return lock_count;
}

/*++
SHMRelease

Release a lock on shared memory taken with SHMLock.

(no parameters)

Return value :
    New lock count

--*/
int SHMRelease(void)
{
    /* prevent a thread from releasing another thread's lock */
    PALCEnterCriticalSection(&shm_critsec);

    if(lock_count==0)
    {
        ASSERT("SHMRelease called without matching SHMLock!\n");
        PALCLeaveCriticalSection(&shm_critsec);
        return 0;
    }

    lock_count--;

    _ASSERTE(lock_count >= 0);

    /* If lock count is 0, this call matches the first Lock call; it's time to
       set the spinlock back to 0. */
    if(lock_count == 0)
    {
        pid_t my_pid, tmp_pid;

        TRACE("Releasing first-level SHM lock : resetting spinlock\n");

        my_pid = gPID;

        /* Make sure we don't touch the spinlock if we don't own it. We're
           supposed to own it if we get here, but just in case... */
        tmp_pid = InterlockedCompareExchange((LONG *) &shm_header.spinlock, 0, my_pid);

        if (tmp_pid != my_pid)
        {
            ASSERT("Process 0x%08x tried to release spinlock owned by process "
                   "0x%08x! \n", my_pid, tmp_pid);
            PALCLeaveCriticalSection(&shm_critsec);
            return 0;
        }

        /* indicate no thread (in this process) holds the SHM lock */
        locking_thread = 0;
    }

    TRACE("SHM lock level is now %d\n", lock_count.Load());

    /* This matches the EnterCriticalSection from SHMRelease */
    PALCLeaveCriticalSection(&shm_critsec);

    /* This matches the EnterCriticalSection from SHMLock */
    PALCLeaveCriticalSection(&shm_critsec);

    return lock_count;
}

/*++
Function :
    SHMGetInfo

    Retrieve some information from shared memory

Parameters :
    SHM_INFO_ID element : identifier of element to retrieve

Return value :
    Value of specified element

Notes :
    The SHM lock should be held while manipulating shared memory
--*/
SHMPTR SHMGetInfo(SHM_INFO_ID element)
{
    SHMPTR retval = 0;

    if(element < 0 || element >= SIID_LAST)
    {
        ASSERT("Invalid SHM info element %d\n", element);
        return 0;
    }

    /* verify that this thread holds the SHM lock. No race condition: if the
       current thread is here, it can't be in SHMLock or SHMUnlock */
    if( reinterpret_cast<HANDLE>(pthread_self()) != locking_thread )
    {
        ASSERT("SHMGetInfo called while thread does not hold the SHM lock!\n");
    }

    retval = shm_header.shm_info[element];

    TRACE("SHM info element %d is %08x\n", element, retval );
    return retval;
}


/*++
Function :
    SHMSetInfo

    Place some information into shared memory

Parameters :
    SHM_INFO_ID element : identifier of element to save
    SHMPTR value : new value of element

Return value :
    TRUE if successful, FALSE otherwise.

Notes :
    The SHM lock should be held while manipulating shared memory
--*/
BOOL SHMSetInfo(SHM_INFO_ID element, SHMPTR value)
{
    if(element < 0 || element >= SIID_LAST)
    {
        ASSERT("Invalid SHM info element %d\n", element);
        return FALSE;
    }

    /* verify that this thread holds the SHM lock. No race condition: if the
       current thread is here, it can't be in SHMLock or SHMUnlock */
    if( reinterpret_cast<HANDLE>(pthread_self()) != locking_thread )
    {
        ASSERT("SHMGetInfo called while thread does not hold the SHM lock!\n");
    }

    TRACE("Setting SHM info element %d to %08x; used to be %08x\n",
          element, value, shm_header.shm_info[element].Load() );

    shm_header.shm_info[element] = value;

    return TRUE;
}
