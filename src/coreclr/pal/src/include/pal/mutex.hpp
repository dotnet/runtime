// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    mutex.hpp

Abstract:

    Mutex object structure definition.



--*/

#ifndef _PAL_MUTEX_H_
#define _PAL_MUTEX_H_

#include "corunix.hpp"
#include "sharedmemory.h"

#include <pthread.h>

namespace CorUnix
{
    extern CObjectType otMutex;
    extern CObjectType otNamedMutex;

    PAL_ERROR
    InternalCreateMutex(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpMutexAttributes,
        BOOL bInitialOwner,
        LPCSTR lpName,
        HANDLE *phMutex
        );

    PAL_ERROR
    InternalReleaseMutex(
        CPalThread *pThread,
        HANDLE hMutex
        );

    PAL_ERROR
    InternalOpenMutex(
        CPalThread *pThread,
        LPCSTR lpName,
        HANDLE *phMutex
        );

}

#define SYNCSPINLOCK_F_ASYMMETRIC  1

#define SPINLOCKInit(lock) (*(lock) = 0)
#define SPINLOCKDestroy SPINLOCKInit

void SPINLOCKAcquire (LONG * lock, unsigned int flags);
void SPINLOCKRelease (LONG * lock);
DWORD SPINLOCKTryAcquire (LONG * lock);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Named mutex

/*
Design

- On systems that support pthread process-shared robust recursive mutexes, they will be used
- On other systems, file locks are used. File locks unfortunately don't have a timeout in the blocking wait call, and I didn't
  find any other sync object with a timed wait with the necessary properties, so polling is done for timed waits.

Shared memory files
- Session-scoped mutexes (name not prefixed, or prefixed with Local) go in /tmp/.dotnet/shm/session<sessionId>/<mutexName>
- Globally-scoped mutexes (name prefixed with Global) go in /tmp/.dotnet/shm/global/<mutexName>
- Contains shared state, and is mmap'ped into the process, see SharedMemorySharedDataHeader and NamedMutexSharedData for data
  stored
- Creation and deletion is synchronized using an exclusive file lock on the shm directory
- Any process using the shared memory file holds a shared file lock on the shared memory file
- Upon creation, if the shared memory file already exists, an exclusive file lock is attempted on it, to see if the file data is
  valid. If no other processes have the mutex open, the file is reinitialized.
- Upon releasing the last reference to a mutex in a process, it will try to get an exclusive lock on the shared memory file to
  see if any other processes have the mutex opened. If not, the file is deleted, along with the session directory if it's empty.
  The .dotnet and shm directories are not deleted.
- This allows managing the lifetime of mutex state based on active processes that have the mutex open. Depending on how the
  process terminated, the file may still be left over in the tmp directory, I haven't found anything that can be done about
  that.

Lock files when using file locks:
- In addition to the shared memory file, we need another file for the actual synchronization file lock, since a file lock on the
  shared memory file is used for lifetime purposes.
- These files go in /tmp/.dotnet/lockfiles/session<sessionId>|global/<mutexName>
- The file is empty, and is only used for file locks

Process data
- See SharedMemoryProcessDataHeader and NamedMutexProcessData for data stored
- Per mutex name, there is only one instance of process data that is ref-counted. They are currently stored in a linked list in
  SharedMemoryManager. It should use a hash table, but of the many hash table implementations that are already there, none seem
  to be easily usable in the PAL. I'll look into that and will fix later.
- Refers to the associated shared memory, and knows how to clean up both the process data and shared data
- When using file locks for synchronization, a process-local mutex is also created for synchronizing threads, since file locks
  are owned at the file descriptor level and there is only one open file descriptor in the process per mutex name. The
  process-local mutex is locked around the file lock, so that only one thread per process is ever trying to flock on a given
  file descriptor.

Abandon detection
- When a lock is acquired, the process data is added to a linked list on the owning thread
- When a thread exits, the list is walked, each mutex is flagged as abandoned and released
- For detecting process abruptly terminating, pthread robust mutexes give us that. When using file locks, the file lock is
  automatically released by the system. Upon acquiring a lock, the lock owner info in the shared memory is checked to see if the
  mutex was abandoned.

Miscellaneous
- CreateMutex and OpenMutex both create new handles for each mutex opened. Each handle just refers to the process data header
  for the mutex name.
- Some of the above features are already available in the PAL, but not quite in a way that I can use for this purpose. The
  existing shared memory, naming, and waiting infrastructure is not suitable for this purpose, and is not used.
*/

// - On FreeBSD, pthread process-shared robust mutexes cannot be placed in shared memory mapped independently by the processes
//   involved. See https://github.com/dotnet/runtime/issues/10519.
// - On OSX, pthread robust mutexes were/are not available at the time of this writing. In case they are made available in the
//   future, their use is disabled for compatibility.
#if HAVE_FULLY_FEATURED_PTHREAD_MUTEXES && \
    HAVE_FUNCTIONAL_PTHREAD_ROBUST_MUTEXES && \
    !(defined(__FreeBSD__) || defined(TARGET_OSX))

    #define NAMED_MUTEX_USE_PTHREAD_MUTEX 1
#else
    #define NAMED_MUTEX_USE_PTHREAD_MUTEX 0
#endif

enum class NamedMutexError : DWORD
{
    MaximumRecursiveLocksReached = ERROR_NOT_ENOUGH_MEMORY,
    ThreadHasNotAcquiredMutex = ERROR_NOT_OWNER,
    Unknown = ERROR_NOT_ENOUGH_MEMORY
};

enum class MutexTryAcquireLockResult
{
    AcquiredLock,
    AcquiredLockButMutexWasAbandoned,
    TimedOut
};

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
class MutexHelpers
{
public:
    static void InitializeProcessSharedRobustRecursiveMutex(pthread_mutex_t *mutex);
    static void DestroyMutex(pthread_mutex_t *mutex);

    static MutexTryAcquireLockResult TryAcquireLock(pthread_mutex_t *mutex, DWORD timeoutMilliseconds);
    static void ReleaseLock(pthread_mutex_t *mutex);
};
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

class NamedMutexSharedData
{
private:
#if NAMED_MUTEX_USE_PTHREAD_MUTEX
    pthread_mutex_t m_lock;
#else // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    UINT32 m_timedWaiterCount;
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX
    UINT32 m_lockOwnerProcessId;
    UINT64 m_lockOwnerThreadId;
    bool m_isAbandoned;

public:
    NamedMutexSharedData();
    ~NamedMutexSharedData();

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
public:
    pthread_mutex_t *GetLock();
#else // !NAMED_MUTEX_USE_PTHREAD_MUTEX
public:
    bool HasAnyTimedWaiters() const;
    void IncTimedWaiterCount();
    void DecTimedWaiterCount();
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

public:
    bool IsAbandoned() const;
    void SetIsAbandoned(bool isAbandoned);

public:
    bool IsLockOwnedByAnyThread() const;
    bool IsLockOwnedByCurrentThread() const;
    void SetLockOwnerToCurrentThread();
    void ClearLockOwner();
};

class NamedMutexProcessData : public SharedMemoryProcessDataBase
{
private:
    static const UINT8 SyncSystemVersion;
    static const DWORD PollLoopMaximumSleepMilliseconds;

private:
    SharedMemoryProcessDataHeader *m_processDataHeader;
    SIZE_T m_lockCount;
#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    HANDLE m_processLockHandle;
    int m_sharedLockFileDescriptor;
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    CorUnix::CPalThread *m_lockOwnerThread;
    NamedMutexProcessData *m_nextInThreadOwnedNamedMutexList;
    bool m_hasRefFromLockOwnerThread;

public:
    static SharedMemoryProcessDataHeader *CreateOrOpen(LPCSTR name, bool acquireLockIfCreated, bool *createdRef);
    static SharedMemoryProcessDataHeader *Open(LPCSTR name);
private:
    static SharedMemoryProcessDataHeader *CreateOrOpen(LPCSTR name, bool createIfNotExist, bool acquireLockIfCreated, bool *createdRef);

public:
    NamedMutexProcessData(
        SharedMemoryProcessDataHeader *processDataHeader
    #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
        ,
        int sharedLockFileDescriptor
    #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    );

public:
    virtual bool CanClose() const override;
    virtual bool HasImplicitRef() const override;
    virtual void SetHasImplicitRef(bool value) override;
    virtual void Close(bool isAbruptShutdown, bool releaseSharedData) override;

public:
    bool IsLockOwnedByCurrentThread() const
    {
        return GetSharedData()->IsLockOwnedByCurrentThread();
    }

private:
    NamedMutexSharedData *GetSharedData() const;
    void SetLockOwnerThread(CorUnix::CPalThread *lockOwnerThread);
public:
    NamedMutexProcessData *GetNextInThreadOwnedNamedMutexList() const;
    void SetNextInThreadOwnedNamedMutexList(NamedMutexProcessData *next);

public:
    MutexTryAcquireLockResult TryAcquireLock(DWORD timeoutMilliseconds);
    void ReleaseLock();
    void Abandon();
private:
    void ActuallyReleaseLock();
};

#endif //_PAL_MUTEX_H_
