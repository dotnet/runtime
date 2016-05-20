// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        DWORD dwDesiredAccess,
        BOOL bInheritHandle,
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

#if HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
class MutexHelpers
{
public:
    static void InitializeProcessSharedRobustRecursiveMutex(pthread_mutex_t *mutex);
    static void DestroyMutex(pthread_mutex_t *mutex);

    static MutexTryAcquireLockResult TryAcquireLock(pthread_mutex_t *mutex, DWORD timeoutMilliseconds);
    static void ReleaseLock(pthread_mutex_t *mutex);
};
#endif // HAVE_FULLY_FEATURED_PTHREAD_MUTEXES

class NamedMutexSharedData
{
private:
#if HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    pthread_mutex_t m_lock;
#else // !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    UINT32 m_timedWaiterCount;
#endif // HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    UINT32 m_lockOwnerProcessId;
    UINT64 m_lockOwnerThreadId;
    bool m_isAbandoned;

public:
    NamedMutexSharedData();
    ~NamedMutexSharedData();

#if HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
public:
    pthread_mutex_t *GetLock();
#else // !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
public:
    bool HasAnyTimedWaiters() const;
    void IncTimedWaiterCount();
    void DecTimedWaiterCount();
#endif // HAVE_FULLY_FEATURED_PTHREAD_MUTEXES

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
    NamedMutexSharedData *m_sharedData;
    SIZE_T m_lockCount;
#if !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    HANDLE m_processLockHandle;
    int m_sharedLockFileDescriptor;
#endif // !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    CorUnix::CPalThread *m_lockOwnerThread;
    NamedMutexProcessData *m_nextInThreadOwnedNamedMutexList;

public:
    static SharedMemoryProcessDataHeader *CreateOrOpen(LPCSTR name, bool acquireLockIfCreated, bool *createdRef);
    static SharedMemoryProcessDataHeader *Open(LPCSTR name);
private:
    static SharedMemoryProcessDataHeader *CreateOrOpen(LPCSTR name, bool createIfNotExist, bool acquireLockIfCreated, bool *createdRef);

public:
    NamedMutexProcessData(
        SharedMemoryProcessDataHeader *processDataHeader
    #if !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
        ,
        int sharedLockFileDescriptor
    #endif // !HAVE_FULLY_FEATURED_PTHREAD_MUTEXES
    );
    virtual void Close(bool isAbruptShutdown, bool releaseSharedData) override;

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
