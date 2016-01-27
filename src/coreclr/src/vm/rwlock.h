// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 
//+-------------------------------------------------------------------
//
//  File:       RWLock.h
//
//  Contents:   Reader writer lock implementation that supports the
//              following features
//                  1. Cheap enough to be used in large numbers
//                     such as per object synchronization.
//                  2. Supports timeout. This is a valuable feature
//                     to detect deadlocks
//                  3. Supports caching of events. The allows
//                     the events to be moved from least contentious
//                     regions to the most contentious regions.
//                     In other words, the number of events needed by
//                     Reader-Writer lockls is bounded by the number
//                     of threads in the process.
//                  4. Supports nested locks by readers and writers
//                  5. Supports spin counts for avoiding context switches
//                     on  multi processor machines.
//                  6. Supports functionality for upgrading to a writer
//                     lock with a return argument that indicates
//                     intermediate writes. Downgrading from a writer
//                     lock restores the state of the lock.
//                  7. Supports functionality to Release Lock for calling
//                     app code. RestoreLock restores the lock state and
//                     indicates intermediate writes.
//                  8. Recovers from most common failures such as creation of
//                     events. In other words, the lock mainitains consistent
//                     internal state and remains usable
//
//  Classes:    CRWLock,
//              CStaticRWLock
// 
//--------------------------------------------------------------------

#ifdef FEATURE_RWLOCK
#ifndef _RWLOCK_H_
#define _RWLOCK_H_
#include "common.h"
#include "threads.h"

// If you do define this, make sure you define this in managed as well.
//#define RWLOCK_STATISTICS     0

extern DWORD gdwDefaultTimeout;
extern DWORD gdwDefaultSpinCount;


//+-------------------------------------------------------------------
//
//  Struct:     LockCookie
//
//  Synopsis:   Lock cookies returned to the client
//
//+-------------------------------------------------------------------
typedef struct {
    DWORD dwFlags;
    DWORD dwWriterSeqNum;
    WORD wReaderLevel;
    WORD wWriterLevel;
    DWORD dwThreadID;
} LockCookie;

//+-------------------------------------------------------------------
//
//  Class:      CRWLock
//
//  Synopsis:   Class the implements the reader writer locks. 
// 
//+-------------------------------------------------------------------
class CRWLock : public Object
{
    friend class MscorlibBinder;

public:
    // Constuctor
    CRWLock();

    // Cleanup
    void Cleanup();

    OBJECTHANDLE GetObjectHandle();
    HRESULT CreateOwnerIterator(SIZE_T *pIterator);
    static void GetNextOwner(SIZE_T Iterator, IHostTask **ppOwnerHostTask);
    static void DeleteOwnerIterator(SIZE_T Iterator);

    // Statics that do the core work
    static FCDECL1 (void, StaticPrivateInitialize, CRWLock *pRWLock);
    static FCDECL1 (void, StaticPrivateDestruct, CRWLock *pRWLock);
    static FCDECL2 (void, StaticAcquireReaderLockPublic, CRWLock *pRWLock, DWORD dwDesiredTimeout);
    static FCDECL2 (void, StaticAcquireWriterLockPublic, CRWLock *pRWLock, DWORD dwDesiredTimeout);
    static FCDECL1 (void, StaticReleaseReaderLockPublic, CRWLock *pRWLock);
    static FCDECL1 (void, StaticReleaseWriterLockPublic, CRWLock *pRWLock);
    static FCDECL3 (void, StaticDoUpgradeToWriterLockPublic, CRWLock *pRWLock, LockCookie * pLockCookie, DWORD dwDesiredTimeout);
    static FCDECL2 (void, StaticDowngradeFromWriterLock, CRWLock *pRWLock, LockCookie* pLockCookie);
    static FCDECL2 (void, StaticDoReleaseLock, CRWLock *pRWLock, LockCookie * pLockCookie);
    static FCDECL2 (void, StaticRestoreLockPublic, CRWLock *pRWLock, LockCookie* pLockCookie);
    static FCDECL1 (FC_BOOL_RET, StaticIsReaderLockHeld, CRWLock *pRWLock);
    static FCDECL1 (FC_BOOL_RET, StaticIsWriterLockHeld, CRWLock *pRWLock);
    static FCDECL1 (INT32, StaticGetWriterSeqNum, CRWLock *pRWLock);
    static FCDECL2 (FC_BOOL_RET, StaticAnyWritersSince, CRWLock *pRWLock, DWORD dwSeqNum);
private:
    static void StaticAcquireReaderLock(CRWLock **ppRWLock, DWORD dwDesiredTimeout);
    static void StaticAcquireWriterLock(CRWLock **ppRWLock, DWORD dwDesiredTimeout);
    static void StaticReleaseReaderLock(CRWLock **ppRWLock);
    static void StaticReleaseWriterLock(CRWLock **ppRWLock);
    static void StaticRecoverLock(CRWLock **ppRWLock, LockCookie *pLockCookie, DWORD dwFlags);
    static void StaticRestoreLock(CRWLock **ppRWLock, LockCookie *pLockCookie);
    static void StaticUpgradeToWriterLock(CRWLock **ppRWLock, LockCookie *pLockCookie, DWORD dwDesiredTimeout);
public:
    // Assert functions
#ifdef _DEBUG
    BOOL AssertWriterLockHeld();
    BOOL AssertWriterLockNotHeld();
    BOOL AssertReaderLockHeld();
    BOOL AssertReaderLockNotHeld();
    BOOL AssertReaderOrWriterLockHeld();
    void AssertHeld()
    { 
        WRAPPER_NO_CONTRACT; 
        AssertWriterLockHeld(); 
    }
    void AssertNotHeld()
    { 
        WRAPPER_NO_CONTRACT;
        AssertWriterLockNotHeld();
        AssertReaderLockNotHeld(); 
    }
#else
    void AssertWriterLockHeld()                  { LIMITED_METHOD_CONTRACT; }
    void AssertWriterLockNotHeld()               { LIMITED_METHOD_CONTRACT; }
    void AssertReaderLockHeld()                  { LIMITED_METHOD_CONTRACT; }
    void AssertReaderLockNotHeld()               { LIMITED_METHOD_CONTRACT; }
    void AssertReaderOrWriterLockHeld()          { LIMITED_METHOD_CONTRACT; }
    void AssertHeld()                            { LIMITED_METHOD_CONTRACT; }
    void AssertNotHeld()                         { LIMITED_METHOD_CONTRACT; }
#endif

    // Helper functions
#ifdef RWLOCK_STATISTICS
    DWORD GetReaderEntryCount()                  
    { 
        LIMITED_METHOD_CONTRACT;
        return(_dwReaderEntryCount); 
    }
    DWORD GetReaderContentionCount()             { LIMITED_METHOD_CONTRACT; return(_dwReaderContentionCount); }
    DWORD GetWriterEntryCount()                  { LIMITED_METHOD_CONTRACT; return(_dwWriterEntryCount); }
    DWORD GetWriterContentionCount()             { LIMITED_METHOD_CONTRACT; return(_dwWriterContentionCount); }
#endif
    // Static functions
    static void *operator new(size_t size)       
    { 
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        return ::operator new(size); 
    }
    static void ProcessInit();

    static void SetTimeout(DWORD dwTimeout)      
    { 
        LIMITED_METHOD_CONTRACT;

        gdwDefaultTimeout = dwTimeout; 
    }
    static DWORD GetTimeout()                    
    { 
        LIMITED_METHOD_CONTRACT; 
        return(gdwDefaultTimeout); 
    }
    static void SetSpinCount(DWORD dwSpinCount)  
    { 
        LIMITED_METHOD_CONTRACT;

        gdwDefaultSpinCount = g_SystemInfo.dwNumberOfProcessors > 1
                                                    ? dwSpinCount
                                                    : 0; 
    }
    static DWORD GetSpinCount()                  { LIMITED_METHOD_CONTRACT; return(gdwDefaultSpinCount); }

private:
    // Private helpers
    static void ChainEntry(Thread *pThread, LockEntry *pLockEntry);
    LockEntry *GetLockEntry(Thread *pThread = NULL);
    LockEntry *FastGetOrCreateLockEntry();
    LockEntry *SlowGetOrCreateLockEntry(Thread *pThread);
    void FastRecycleLockEntry(LockEntry *pLockEntry);
    static void RecycleLockEntry(LockEntry *pLockEntry);

    CLREvent* GetReaderEvent(HRESULT *pHR);
    CLREvent* GetWriterEvent(HRESULT *pHR);
    void ReleaseEvents();

    static LONG RWInterlockedCompareExchange(LONG RAW_KEYWORD(volatile) *pvDestination,
                                              LONG dwExchange,
                                              LONG dwComperand);
    static void* RWInterlockedCompareExchangePointer(PVOID RAW_KEYWORD(volatile) *pvDestination,
                                                   PVOID pExchange,
                                                   PVOID pComparand);
    static LONG RWInterlockedExchangeAdd(LONG RAW_KEYWORD(volatile) *pvDestination, LONG dwAddState);
    static LONG RWInterlockedIncrement(LONG RAW_KEYWORD(volatile) *pdwState);

    static DWORD RWWaitForSingleObject(CLREvent* event, DWORD dwTimeout);
    static void RWSetEvent(CLREvent* event);
    static void RWResetEvent(CLREvent* event);
    static void RWSleep(DWORD dwTime);

#if defined(ENABLE_CONTRACTS_IMPL)
    // The LOCK_TAKEN/RELEASED macros need a "pointer" to the lock object to do
    // comparisons between takes & releases (and to provide debugging info to the
    // developer).  We can't use "this" (*ppRWLock), because CRWLock is an Object and thus
    // can move.  So we use _dwLLockID instead.  It's not exactly unique, but it's
    // good enough--worst that can happen is if a thread takes RWLock A and erroneously
    // releases RWLock B (instead of A), we'll fail to catch that if their _dwLLockID's
    // are the same.  On 64 bits, we can use both _dwULockID & _dwLLockID and be unique
    static void * GetPtrForLockContract(CRWLock ** ppRWLock)
    {
#if defined(_WIN64)
        return (void *)
            (
                (
                    ((__int64) ((*ppRWLock)->_dwULockID)) << 32
                )
                |
                (
                    (__int64) ((*ppRWLock)->_dwLLockID)
                )
            );
#else //defined(_WIN64)
            return LongToPtr((*ppRWLock)->_dwLLockID);
#endif //defined(_WIN64)
    }
#endif //defined(ENABLE_CONTRACTS_IMPL)

    // private new
    static void *operator new(size_t size, void *pv)   { LIMITED_METHOD_CONTRACT;  return(pv); }

    // Private data
    CLREvent *_hWriterEvent;
    CLREvent *_hReaderEvent;
    OBJECTHANDLE _hObjectHandle;
    Volatile<LONG> _dwState;
    LONG _dwULockID;
    LONG _dwLLockID;
    DWORD _dwWriterID;
    DWORD _dwWriterSeqNum;
    WORD _wWriterLevel;
#ifdef RWLOCK_STATISTICS
    // WARNING: You must explicitly #define RWLOCK_STATISTICS when you build
    // in both the VM and BCL directories, as the managed class must also 
    // contain these fields!
    Volatile<LONG> _dwReaderEntryCount;
    Volatile<LONG> _dwReaderContentionCount;
    Volatile<LONG> _dwWriterEntryCount;
    Volatile<LONG> _dwWriterContentionCount;
    Volatile<LONG> _dwEventsReleasedCount;
#endif

    // Static data
    static Volatile<LONG> s_mostRecentULockID;
    static Volatile<LONG> s_mostRecentLLockID;
    static CrstStatic       s_RWLockCrst;
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<CRWLock> RWLOCKREF;

#else
typedef CRWLock*     RWLOCKREF;
#endif

#endif // _RWLOCK_H_

#endif // FEATURE_RWLOCK

