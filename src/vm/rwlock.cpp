// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 
//+-------------------------------------------------------------------
//
//  File:       RWLock.cpp
//
//  Contents:   Reader writer lock implementation that supports the
//              following features
//                  1. Cheap enough to be used in large numbers
//                     such as per object synchronization.
//                  2. Supports timeout. This is a valuable feature
//                     to detect deadlocks
//                  3. Supports caching of events. This allows
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
//
//  Classes:    CRWLock
// 
//--------------------------------------------------------------------


#include "common.h"
#include "rwlock.h"
#include "corhost.h"

#ifdef FEATURE_RWLOCK

// Reader increment
#define READER                 0x00000001
// Max number of readers
#define READERS_MASK           0x000003FF
// Reader being signaled
#define READER_SIGNALED        0x00000400
// Writer being signaled
#define WRITER_SIGNALED        0x00000800
#define WRITER                 0x00001000
// Waiting reader increment
#define WAITING_READER         0x00002000
// Note size of waiting readers must be less
// than or equal to size of readers
#define WAITING_READERS_MASK   0x007FE000
#define WAITING_READERS_SHIFT  13
// Waiting writer increment
#define WAITING_WRITER         0x00800000
// Max number of waiting writers
#define WAITING_WRITERS_MASK   0xFF800000
// Events are being cached
#define CACHING_EVENTS         (READER_SIGNALED | WRITER_SIGNALED)

// Cookie flags
#define UPGRADE_COOKIE         0x02000
#define RELEASE_COOKIE         0x04000
#define COOKIE_NONE            0x10000
#define COOKIE_WRITER          0x20000
#define COOKIE_READER          0x40000
#define INVALID_COOKIE         (~(UPGRADE_COOKIE | RELEASE_COOKIE |            \
                                  COOKIE_NONE | COOKIE_WRITER | COOKIE_READER))
#define RWLOCK_MAX_ACQUIRE_COUNT 0xFFFF

// globals
Volatile<LONG> CRWLock::s_mostRecentLLockID = 0;
Volatile<LONG> CRWLock::s_mostRecentULockID = -1;
CrstStatic CRWLock::s_RWLockCrst;

// Default values
#ifdef _DEBUG
DWORD gdwDefaultTimeout = 120000;
#else //!_DEBUG
DWORD gdwDefaultTimeout = INFINITE;
#endif //_DEBUG
const DWORD gdwReasonableTimeout = 120000;
DWORD gdwDefaultSpinCount = 0;
BOOL fBreakOnErrors = FALSE; // Temporarily break on errors

// <REVISIT_TODO> REVISIT_TODO: Bad practise</REVISIT_TODO>
#define HEAP_SERIALIZE                   0
#define RWLOCK_RECOVERY_FAILURE          (0xC0000227L)

// Catch GC holes
#if _DEBUG
#define VALIDATE_LOCK(pRWLock)                ((Object *) (pRWLock))->Validate();
#else // !_DEBUG
#define VALIDATE_LOCK(pRWLock)
#endif // _DEBUG


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::ProcessInit     public
//
//  Synopsis:   Reads default values from registry and intializes 
//              process wide data structures
// 
//+-------------------------------------------------------------------
void CRWLock::ProcessInit()
{
    CONTRACTL
    {
        THROWS; // From Crst.Init()
        GC_NOTRIGGER;
        PRECONDITION((g_SystemInfo.dwNumberOfProcessors != 0));
    }
    CONTRACTL_END;

    gdwDefaultSpinCount = (g_SystemInfo.dwNumberOfProcessors != 1) ? 500 : 0;

    PPEB peb = (PPEB) ClrTeb::GetProcessEnvironmentBlock();
    DWORD dwTimeout = (DWORD)(peb->CriticalSectionTimeout.QuadPart/-10000000);
    if (dwTimeout)
    {
        gdwDefaultTimeout = dwTimeout;
    }

    // Initialize the critical section used by the lock
    // Can throw out of memory here.
    s_RWLockCrst.Init(CrstRWLock, CRST_UNSAFE_ANYMODE);
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::CRWLock     public
//
//  Synopsis:   Constructor
// 
//+-------------------------------------------------------------------
CRWLock::CRWLock()
:   _hWriterEvent(NULL),
    _hReaderEvent(NULL),
    _dwState(0),
    _dwWriterID(0),
    _dwWriterSeqNum(1),
    _wWriterLevel(0)
#ifdef RWLOCK_STATISTICS
    ,
    _dwReaderEntryCount(0),
    _dwReaderContentionCount(0),
    _dwWriterEntryCount(0),
    _dwWriterContentionCount(0),
    _dwEventsReleasedCount(0)
#endif
{

    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION((_dwLLockID > 0));
    }
    CONTRACT_END;

    LONG dwKnownLLockID;
    LONG dwULockID = s_mostRecentULockID;
    LONG dwLLockID = s_mostRecentLLockID;
    do
    {
        dwKnownLLockID = dwLLockID;
        if(dwKnownLLockID != 0)
        {
            dwLLockID = RWInterlockedCompareExchange(&s_mostRecentLLockID, dwKnownLLockID+1, dwKnownLLockID);
        }
        else
        {
            CrstHolder ch(&s_RWLockCrst);
            
            if(s_mostRecentLLockID == 0)
            {
                dwULockID = ++s_mostRecentULockID;
                dwLLockID = s_mostRecentLLockID++;
                dwKnownLLockID = dwLLockID;
            }
            else
            {
                dwULockID = s_mostRecentULockID;
                dwLLockID = s_mostRecentLLockID;
            }
        }
    } while(dwKnownLLockID != dwLLockID);

    _dwLLockID = ++dwLLockID;
    _dwULockID = dwULockID;

    RETURN;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::Cleanup    public
//
//  Synopsis:   Cleansup state
// 
//+-------------------------------------------------------------------
void CRWLock::Cleanup()
{

    CONTRACTL
    {
        NOTHROW;           
        GC_NOTRIGGER;
        PRECONDITION((_dwState == 0));        // sanity checks
        PRECONDITION((_dwWriterID == 0));
        PRECONDITION((_wWriterLevel == 0));
    }
    CONTRACTL_END;

    if(_hWriterEvent) {
        delete _hWriterEvent;
        _hWriterEvent = NULL;
    }
    if(_hReaderEvent) {
        delete _hReaderEvent;
        _hReaderEvent = NULL;
    }

    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::ChainEntry     private
//
//  Synopsis:   Chains the given lock entry into the chain
// 
//+-------------------------------------------------------------------
inline void CRWLock::ChainEntry(Thread *pThread, LockEntry *pLockEntry)
{
    CONTRACTL
    {
        NOTHROW;           
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // This is to synchronize with finalizer thread and deadlock detection.
    CrstHolder rwl(&s_RWLockCrst);
    LockEntry *pHeadEntry = pThread->m_pHead;
    pLockEntry->pNext = pHeadEntry;
    pLockEntry->pPrev = pHeadEntry->pPrev;
    pLockEntry->pPrev->pNext = pLockEntry;
    pHeadEntry->pPrev = pLockEntry;

    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::GetLockEntry     private
//
//  Synopsis:   Gets lock entry from TLS
// 
//+-------------------------------------------------------------------
inline LockEntry *CRWLock::GetLockEntry(Thread* pThread)
{
    CONTRACTL
    {
        NOTHROW;           
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if (pThread == NULL) {
        pThread = GetThread();
    }
    LockEntry *pHeadEntry = pThread->m_pHead;
    LockEntry *pLockEntry = pHeadEntry;
    do
    {
        if((pLockEntry->dwLLockID == _dwLLockID) && (pLockEntry->dwULockID == _dwULockID))
            return(pLockEntry);
        pLockEntry = pLockEntry->pNext;
    } while(pLockEntry != pHeadEntry);

    return(NULL);
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::FastGetOrCreateLockEntry     private
//
//  Synopsis:   The fast path for getting a lock entry from TLS
// 
//+-------------------------------------------------------------------
inline LockEntry *CRWLock::FastGetOrCreateLockEntry()
{

    CONTRACTL
    {
        THROWS;               //   SlowGetOrCreateLockEntry can throw out of memory exception       
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);
    LockEntry *pLockEntry = pThread->m_pHead;
    if(pLockEntry->dwLLockID == 0)
    {
        _ASSERTE(pLockEntry->wReaderLevel == 0);
        pLockEntry->dwLLockID = _dwLLockID;
        pLockEntry->dwULockID = _dwULockID;
        return(pLockEntry);
    }
    else if((pLockEntry->dwLLockID == _dwLLockID) && (pLockEntry->dwULockID == _dwULockID))
    {
        // Note, StaticAcquireReaderLock can have reentry via pumping while it's blocking
        // so no assertions about pLockEntry->wReaderLevel's state
        return(pLockEntry);
    }

    return(SlowGetOrCreateLockEntry(pThread));
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::SlowGetorCreateLockEntry     private
//
//  Synopsis:   The slow path for getting a lock entry from TLS
// 
//+-------------------------------------------------------------------
LockEntry *CRWLock::SlowGetOrCreateLockEntry(Thread *pThread)
{

    CONTRACTL
    {
        THROWS;               // memory allocation can throw out of memory exception       
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LockEntry *pFreeEntry = NULL;
    LockEntry *pHeadEntry = pThread->m_pHead;

    // Search for an empty entry or an entry belonging to this lock
    LockEntry *pLockEntry = pHeadEntry->pNext;
    while(pLockEntry != pHeadEntry)
    {
         if(pLockEntry->dwLLockID && 
            ((pLockEntry->dwLLockID != _dwLLockID) || (pLockEntry->dwULockID != _dwULockID)))
         {
             // Move to the next entry
             pLockEntry = pLockEntry->pNext;
         }
         else
         {
             // Prepare to move it to the head
             pFreeEntry = pLockEntry;
             pLockEntry->pPrev->pNext = pLockEntry->pNext;
             pLockEntry->pNext->pPrev = pLockEntry->pPrev;

             break;
         }
    }

    if(pFreeEntry == NULL)
    {
        pFreeEntry = new LockEntry;
        pFreeEntry->wReaderLevel = 0;
    }

    if(pFreeEntry)
    {
        _ASSERTE((pFreeEntry->dwLLockID != 0) || (pFreeEntry->wReaderLevel == 0));
        _ASSERTE((pFreeEntry->wReaderLevel == 0) || 
                 ((pFreeEntry->dwLLockID == _dwLLockID) && (pFreeEntry->dwULockID == _dwULockID)));

        // Chain back the entry
        ChainEntry(pThread, pFreeEntry);

        // Move this entry to the head
        pThread->m_pHead = pFreeEntry;

        // Mark the entry as belonging to this lock
        pFreeEntry->dwLLockID = _dwLLockID;
        pFreeEntry->dwULockID = _dwULockID;
    }

    return pFreeEntry;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::FastRecycleLockEntry     private
//
//  Synopsis:   Fast path for recycling the lock entry that is used
//              when the thread is the next few instructions is going
//              to call FastGetOrCreateLockEntry again
// 
//+-------------------------------------------------------------------
inline void CRWLock::FastRecycleLockEntry(LockEntry *pLockEntry)
{

    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;

        // Sanity checks
        PRECONDITION(pLockEntry->wReaderLevel == 0);
        PRECONDITION((pLockEntry->dwLLockID == _dwLLockID) && (pLockEntry->dwULockID == _dwULockID));
        PRECONDITION(pLockEntry == GetThread()->m_pHead);
    }
    CONTRACTL_END;


    pLockEntry->dwLLockID = 0;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RecycleLockEntry     private
//
//  Synopsis:   Fast path for recycling the lock entry
// 
//+-------------------------------------------------------------------
inline void CRWLock::RecycleLockEntry(LockEntry *pLockEntry)
{

    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;

        // Sanity check
        PRECONDITION(pLockEntry->wReaderLevel == 0);
    }
    CONTRACTL_END;

    // Move the entry to tail
    Thread *pThread = GetThread();
    LockEntry *pHeadEntry = pThread->m_pHead;
    if(pLockEntry == pHeadEntry)
    {
        pThread->m_pHead = pHeadEntry->pNext;
    }
    else if(pLockEntry->pNext->dwLLockID)
    {
        // Prepare to move the entry to tail
        pLockEntry->pPrev->pNext = pLockEntry->pNext;
        pLockEntry->pNext->pPrev = pLockEntry->pPrev;

        // Chain back the entry
        ChainEntry(pThread, pLockEntry);
    }

    // The entry does not belong to this lock anymore
    pLockEntry->dwLLockID = 0;
    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticIsWriterLockHeld    public
//
//  Synopsis:   Return TRUE if writer lock is held
// 
//+-------------------------------------------------------------------
FCIMPL1(FC_BOOL_RET, CRWLock::StaticIsWriterLockHeld, CRWLock *pRWLock)
{
    FCALL_CONTRACT;

    if (pRWLock == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    if(pRWLock->_dwWriterID == GetThread()->GetThreadId())
        FC_RETURN_BOOL(TRUE);

    FC_RETURN_BOOL(FALSE);
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticIsReaderLockHeld    public
//
//  Synopsis:   Return TRUE if reader lock is held
// 
//+-------------------------------------------------------------------
FCIMPL1(FC_BOOL_RET, CRWLock::StaticIsReaderLockHeld, CRWLock *pRWLock)
{
    FCALL_CONTRACT;

    if (pRWLock == NULL)
    {
        FCThrow(kNullReferenceException);
    }
    
    LockEntry *pLockEntry = pRWLock->GetLockEntry();
    if(pLockEntry)
    {
        FC_RETURN_BOOL(pLockEntry->wReaderLevel > 0);
    }

    FC_RETURN_BOOL(FALSE);
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::AssertWriterLockHeld    public
//
//  Synopsis:   Asserts that writer lock is held
// 
//+-------------------------------------------------------------------
#ifdef _DEBUG
BOOL CRWLock::AssertWriterLockHeld()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if(_dwWriterID == GetThread()->GetThreadId())
        return(TRUE);

    _ASSERTE(!"Writer lock not held by the current thread");
    return(FALSE);
}
#endif


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::AssertWriterLockNotHeld    public
//
//  Synopsis:   Asserts that writer lock is not held
// 
//+-------------------------------------------------------------------
#ifdef _DEBUG
BOOL CRWLock::AssertWriterLockNotHeld()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if(_dwWriterID != GetThread()->GetThreadId())
        return(TRUE);

    _ASSERTE(!"Writer lock held by the current thread");
    return(FALSE);
}
#endif


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::AssertReaderLockHeld    public
//
//  Synopsis:   Asserts that reader lock is held
// 
//+-------------------------------------------------------------------
#ifdef _DEBUG
BOOL CRWLock::AssertReaderLockHeld()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    LockEntry *pLockEntry = GetLockEntry();
    if(pLockEntry)
    {
        _ASSERTE(pLockEntry->wReaderLevel);
        return(TRUE);
    }

    _ASSERTE(!"Reader lock not held by the current thread");
    return(FALSE);
}
#endif


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::AssertReaderLockNotHeld    public
//
//  Synopsis:   Asserts that writer lock is not held
// 
//+-------------------------------------------------------------------
#ifdef _DEBUG
BOOL CRWLock::AssertReaderLockNotHeld()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    LockEntry *pLockEntry = GetLockEntry();
    if(pLockEntry == NULL)
        return(TRUE);

    _ASSERTE(pLockEntry->wReaderLevel);
    _ASSERTE(!"Reader lock held by the current thread");

    return(FALSE);
}
#endif


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::AssertReaderOrWriterLockHeld   public
//
//  Synopsis:   Asserts that writer lock is not held
// 
//+-------------------------------------------------------------------
#ifdef _DEBUG
BOOL CRWLock::AssertReaderOrWriterLockHeld()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    if(_dwWriterID == GetThread()->GetThreadId())
    {
        return(TRUE);
    }
    else
    {
        LockEntry *pLockEntry = GetLockEntry();
        if(pLockEntry)
        {
            _ASSERTE(pLockEntry->wReaderLevel);
            return(TRUE);
        }
    }

    _ASSERTE(!"Neither Reader nor Writer lock held");
    return(FALSE);
}
#endif


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWSetEvent    private
//
//  Synopsis:   Helper function for setting an event
// 
//+-------------------------------------------------------------------
inline void CRWLock::RWSetEvent(CLREvent* event)
{
    CONTRACTL
    {
        THROWS;         
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if(!event->Set())
    {
        _ASSERTE(!"SetEvent failed");
        if(fBreakOnErrors) // fBreakOnErrors == FALSE so will be optimized out.
            DebugBreak();
        COMPlusThrowWin32(E_UNEXPECTED);
    }
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWResetEvent    private
//
//  Synopsis:   Helper function for resetting an event
// 
//+-------------------------------------------------------------------
inline void CRWLock::RWResetEvent(CLREvent* event)
{
    CONTRACTL
    {
      THROWS;         
      GC_TRIGGERS;
    }
    CONTRACTL_END;

    if(!event->Reset())
    {
        _ASSERTE(!"ResetEvent failed");
        if(fBreakOnErrors) // fBreakOnErrors == FALSE so will be optimized out.
            DebugBreak();
        COMPlusThrowWin32(E_UNEXPECTED);
    }
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWWaitForSingleObject    public
//
//  Synopsis:   Helper function for waiting on an event
// 
//+-------------------------------------------------------------------
inline DWORD CRWLock::RWWaitForSingleObject(CLREvent* event, DWORD dwTimeout)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DWORD status = WAIT_FAILED;
    EX_TRY
    {
        status = event->Wait(dwTimeout,TRUE);
    }
    EX_CATCH
    {
        status = GET_EXCEPTION()->GetHR();
        if (status == S_OK)
        {
            status = WAIT_FAILED;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);  // The caller will rethrow the exception

    return status;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWSleep    public
//
//  Synopsis:   Helper function for calling Sleep
// 
//+-------------------------------------------------------------------
inline void CRWLock::RWSleep(DWORD dwTime)
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ClrSleepEx(dwTime, TRUE);
}


#undef volatile

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWInterlockedCompareExchange    public
//
//  Synopsis:   Helper function for calling intelockedCompareExchange
// 
//+-------------------------------------------------------------------
inline LONG CRWLock::RWInterlockedCompareExchange(LONG volatile *pvDestination,
                                                   LONG dwExchange,
                                                   LONG dwComparand)
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return  FastInterlockCompareExchange(pvDestination, 
                                         dwExchange, 
                                         dwComparand);
}

inline void* CRWLock::RWInterlockedCompareExchangePointer(PVOID volatile *pvDestination,
                                                   void* pExchange,
                                                   void* pComparand)
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return  FastInterlockCompareExchangePointer(pvDestination, 
                                            pExchange, 
                                            pComparand);
}

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWInterlockedExchangeAdd    public
//
//  Synopsis:   Helper function for adding state
// 
//+-------------------------------------------------------------------
inline LONG CRWLock::RWInterlockedExchangeAdd(LONG volatile *pvDestination,
                                               LONG dwAddToState)
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return FastInterlockExchangeAdd(pvDestination, dwAddToState);
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::RWInterlockedIncrement    public
//
//  Synopsis:   Helper function for incrementing a pointer
// 
//+-------------------------------------------------------------------
inline LONG CRWLock::RWInterlockedIncrement(LONG volatile *pdwState)
{
    CONTRACTL
    {
        NOTHROW;         
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return FastInterlockIncrement(pdwState);
}

#define volatile DoNotUserVolatileKeyword


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::ReleaseEvents    public
//
//  Synopsis:   Helper function for caching events
// 
//+-------------------------------------------------------------------
void CRWLock::ReleaseEvents()
{
    CONTRACTL
    {
        NOTHROW;         
        GC_TRIGGERS;
        PRECONDITION(((_dwState & CACHING_EVENTS) == CACHING_EVENTS));        // Ensure that reader and writers have been stalled

    }
    CONTRACTL_END;

    // Save writer event
    CLREvent *hWriterEvent = _hWriterEvent;
    _hWriterEvent = NULL;

    // Save reader event
    CLREvent *hReaderEvent = _hReaderEvent;
    _hReaderEvent = NULL;

    // Allow readers and writers to continue
    RWInterlockedExchangeAdd(&_dwState, -(CACHING_EVENTS));

    // Cache events
    // <REVISIT_TODO>: 
    //         I am closing events for now. What is needed
    //         is an event cache to which the events are
    //         released using InterlockedCompareExchange64</REVISIT_TODO>
    if(hWriterEvent)
    {
        LOG((LF_SYNC, LL_INFO10, "Releasing writer event\n"));
        delete hWriterEvent;
    }
    if(hReaderEvent)
    {
        LOG((LF_SYNC, LL_INFO10, "Releasing reader event\n"));
        delete hReaderEvent;
    }
#ifdef RWLOCK_STATISTICS
    RWInterlockedIncrement(&_dwEventsReleasedCount);
#endif

    return;
}

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::GetWriterEvent    public
//
//  Synopsis:   Helper function for obtaining a auto reset event used
//              for serializing writers. It utilizes event cache
// 
//+-------------------------------------------------------------------
CLREvent* CRWLock::GetWriterEvent(HRESULT *pHR)
{
    CONTRACTL
    {
        NOTHROW;   
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    *pHR = S_OK;
    //GC could happen in ~CLREvent or EH. "this" is a GC object so it could be moved
    //during GC. So we need to cache the field before GC could happen
    CLREvent * result = _hWriterEvent;
    
    if(_hWriterEvent == NULL)
    {
        EX_TRY
        {
            CLREvent *pEvent = new CLREvent();
            NewHolder<CLREvent> hWriterEvent (pEvent);
            hWriterEvent->CreateRWLockWriterEvent(FALSE,this);
            if(hWriterEvent)
            {
                if(RWInterlockedCompareExchangePointer((PVOID*) &_hWriterEvent,
                                                hWriterEvent.GetValue(),        
                                                NULL) == NULL)
                {
                    hWriterEvent.SuppressRelease();
                }
                //GC could happen in ~CLREvent or EH. "this" is a GC object so it could be moved
                //during GC. So we need to cache the field before GC could happen.
                result = _hWriterEvent;                
            }
        }
        EX_CATCH
        {
            *pHR = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return(result);
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::GetReaderEvent    public
//
//  Synopsis:   Helper function for obtaining a manula reset event used
//              by readers to wait when a writer holds the lock.
//              It utilizes event cache
// 
//+-------------------------------------------------------------------
CLREvent* CRWLock::GetReaderEvent(HRESULT *pHR)
{
    CONTRACTL
    {
        NOTHROW;   
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    *pHR = S_OK;
    //GC could happen in ~CLREvent or EH. "this" is a GC object so it could be moved
    //during GC. So we need to cache the field before GC could happen
    CLREvent * result = _hReaderEvent;

    if(_hReaderEvent == NULL)
    {
        EX_TRY
        {
            CLREvent *pEvent = new CLREvent();
            NewHolder<CLREvent> hReaderEvent (pEvent);
            hReaderEvent->CreateRWLockReaderEvent(FALSE, this);
            if(hReaderEvent)
            {
                if(RWInterlockedCompareExchangePointer((PVOID*) &_hReaderEvent,
                                                hReaderEvent.GetValue(),
                                                NULL) == NULL)
                {
                    hReaderEvent.SuppressRelease();
                }                
                //GC could happen in ~CLREvent or EH. "this" is a GC object so it could be moved
                //during GC. So we need to cache the field before GC could happen
                result = _hReaderEvent;                
            }
        }
        EX_CATCH
        {
            *pHR = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return(result);
}

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticRecoverLock    public
//
//  Synopsis:   Helper function to restore the lock to 
//              the original state
//

//
//+-------------------------------------------------------------------
void CRWLock::StaticRecoverLock(
    CRWLock **ppRWLock, 
    LockCookie *pLockCookie,
    DWORD dwFlags)
{
    CONTRACTL
    {
      THROWS;               // StaticAcquireWriterLock can throw exception     
      GC_TRIGGERS;
      CAN_TAKE_LOCK;
    }
    CONTRACTL_END;
        
    DWORD dwTimeout = (gdwDefaultTimeout > gdwReasonableTimeout)
                        ? gdwDefaultTimeout
                        : gdwReasonableTimeout;

    Thread *pThread = GetThread();
    _ASSERTE (pThread);

    EX_TRY
    {
        // Check if the thread was a writer
        if(dwFlags & COOKIE_WRITER)
        {
            // Acquire writer lock
            StaticAcquireWriterLock(ppRWLock, dwTimeout);
            _ASSERTE (pThread->m_dwLockCount >= (*ppRWLock)->_wWriterLevel);
            ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= (*ppRWLock)->_wWriterLevel);
            pThread->m_dwLockCount -= (*ppRWLock)->_wWriterLevel;
            USER_LOCK_RELEASED_MULTIPLE((*ppRWLock)->_wWriterLevel, GetPtrForLockContract(ppRWLock));
            (*ppRWLock)->_wWriterLevel = pLockCookie->wWriterLevel;
            pThread->m_dwLockCount += (*ppRWLock)->_wWriterLevel;
            USER_LOCK_TAKEN_MULTIPLE((*ppRWLock)->_wWriterLevel, GetPtrForLockContract(ppRWLock));
        }
        // Check if the thread was a reader
        else if(dwFlags & COOKIE_READER)
        {
            StaticAcquireReaderLock(ppRWLock, dwTimeout);
            LockEntry *pLockEntry = (*ppRWLock)->GetLockEntry();
            _ASSERTE(pLockEntry);
            _ASSERTE (pThread->m_dwLockCount >= pLockEntry->wReaderLevel);
            ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= pLockEntry->wReaderLevel);
            pThread->m_dwLockCount -= pLockEntry->wReaderLevel;
            USER_LOCK_RELEASED_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract(ppRWLock));
            pLockEntry->wReaderLevel = pLockCookie->wReaderLevel;
            pThread->m_dwLockCount += pLockEntry->wReaderLevel;
            USER_LOCK_TAKEN_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract(ppRWLock));
        }
    }
    EX_CATCH
    {
        // Removed an assert here. This error is expected in case of
        // ThreadAbort.
        COMPlusThrowWin32(RWLOCK_RECOVERY_FAILURE);
    }
    EX_END_CATCH_UNREACHABLE
}

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticAcquireReaderLockPublic    public
//
//  Synopsis:   Public access to StaticAcquireReaderLock
//
//+-------------------------------------------------------------------
FCIMPL2(void, CRWLock::StaticAcquireReaderLockPublic, CRWLock *pRWLockUNSAFE, DWORD dwDesiredTimeout)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);

    StaticAcquireReaderLock((CRWLock**)&pRWLock, dwDesiredTimeout);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticAcquireReaderLock    private
//
//  Synopsis:   Makes the thread a reader. Supports nested reader locks.
// 
//+-------------------------------------------------------------------

void CRWLock::StaticAcquireReaderLock(
    CRWLock **ppRWLock, 
    DWORD dwDesiredTimeout)
{

    CONTRACTL
    {
        THROWS;               
        GC_TRIGGERS;            // CLREvent::Wait is GC_TRIGGERS
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(ppRWLock));
        PRECONDITION(CheckPointer(*ppRWLock));
    }
    CONTRACTL_END;

    TESTHOOKCALL(AppDomainCanBeUnloaded(GetThread()->GetDomain()->GetId().m_dwId,FALSE));    

    if (GetThread()->IsAbortRequested()) {
        GetThread()->HandleThreadAbort();
    }

    LockEntry *pLockEntry = (*ppRWLock)->FastGetOrCreateLockEntry();
    if (pLockEntry == NULL)
    {
        COMPlusThrowWin32(STATUS_NO_MEMORY);
    }
    
    DWORD dwStatus = WAIT_OBJECT_0;
    // Check for the fast path
    if(RWInterlockedCompareExchange(&(*ppRWLock)->_dwState, READER, 0) == 0)
    {
        _ASSERTE(pLockEntry->wReaderLevel == 0);
    }
    // Check for nested reader
    else if(pLockEntry->wReaderLevel != 0)
    {
        _ASSERTE((*ppRWLock)->_dwState & READERS_MASK);

        if (pLockEntry->wReaderLevel == RWLOCK_MAX_ACQUIRE_COUNT) {
            COMPlusThrow(kOverflowException, W("Overflow_UInt16"));        
        }
        ++pLockEntry->wReaderLevel;
        INCTHREADLOCKCOUNT();
        USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
        return;
    }
    // Check if the thread already has writer lock
    else if((*ppRWLock)->_dwWriterID == GetThread()->GetThreadId())
    {
        StaticAcquireWriterLock(ppRWLock, dwDesiredTimeout);
        (*ppRWLock)->FastRecycleLockEntry(pLockEntry);
        return;
    }
    else
    {
        DWORD dwSpinCount;
        DWORD dwCurrentState, dwKnownState;
        
        // Initialize
        dwSpinCount = 0;
        dwCurrentState = (*ppRWLock)->_dwState;
        do
        {
            dwKnownState = dwCurrentState;

            // Reader need not wait if there are only readers and no writer
            if((dwKnownState < READERS_MASK) ||
                (((dwKnownState & READER_SIGNALED) && ((dwKnownState & WRITER) == 0)) &&
                 (((dwKnownState & READERS_MASK) +
                   ((dwKnownState & WAITING_READERS_MASK) >> WAITING_READERS_SHIFT)) <=
                  (READERS_MASK - 2))))
            {
                // Add to readers
                dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              (dwKnownState + READER),
                                                              dwKnownState);
                if(dwCurrentState == dwKnownState)
                {
                    // One more reader
                    break;
                }
            }
            // Check for too many Readers or waiting readers or signaling in progress
            else if(((dwKnownState & READERS_MASK) == READERS_MASK) ||
                    ((dwKnownState & WAITING_READERS_MASK) == WAITING_READERS_MASK) ||
                    ((dwKnownState & CACHING_EVENTS) == READER_SIGNALED))
            {
                //  Sleep
                GetThread()->UserSleep(1000);
                
                // Update to latest state
                dwSpinCount = 0;
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            // Check if events are being cached
            else if((dwKnownState & CACHING_EVENTS) == CACHING_EVENTS)
            {
                if(++dwSpinCount > gdwDefaultSpinCount)
                {
                    RWSleep(1);
                    dwSpinCount = 0;
                }
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            // Check spin count
            else if(++dwSpinCount <= gdwDefaultSpinCount)
            {
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            else
            {
                // Add to waiting readers
                dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              (dwKnownState + WAITING_READER),
                                                              dwKnownState);
                if(dwCurrentState == dwKnownState)
                {
                    CLREvent* hReaderEvent;
                    DWORD dwModifyState;

                    // One more waiting reader
#ifdef RWLOCK_STATISTICS
                    RWInterlockedIncrement(&(*ppRWLock)->_dwReaderContentionCount);
#endif
                    HRESULT hr;
                    hReaderEvent = (*ppRWLock)->GetReaderEvent(&hr);
                    if(hReaderEvent)
                    {
                        dwStatus = RWWaitForSingleObject(hReaderEvent, dwDesiredTimeout);
                        VALIDATE_LOCK(*ppRWLock);

                        // StaticAcquireReaderLock can have reentry via pumping while waiting for 
                        // hReaderEvent, which may change pLockEntry's state from underneath us.
                        if ((pLockEntry->dwLLockID != (*ppRWLock)->_dwLLockID) || 
                            (pLockEntry->dwULockID != (*ppRWLock)->_dwULockID))
                        {
                            pLockEntry = (*ppRWLock)->FastGetOrCreateLockEntry();
                            if (pLockEntry == NULL)
                            {
                                COMPlusThrowWin32(STATUS_NO_MEMORY);
                            }
                        }
                    }
                    else
                    {
                        LOG((LF_SYNC, LL_WARNING,
                            "AcquireReaderLock failed to create reader "
                            "event for RWLock 0x%x\n", *ppRWLock));
                        dwStatus = E_FAIL;
                    }

                    if(dwStatus == WAIT_OBJECT_0)
                    {
                        _ASSERTE((*ppRWLock)->_dwState & READER_SIGNALED);
                        _ASSERTE(((*ppRWLock)->_dwState & READERS_MASK) < READERS_MASK);
                        dwModifyState = READER - WAITING_READER;
                    }
                    else
                    {
                        dwModifyState = (DWORD) -WAITING_READER;
                        if(dwStatus == WAIT_TIMEOUT)
                        {
                            LOG((LF_SYNC, LL_WARNING,
                                "Timed out trying to acquire reader lock "
                                "for RWLock 0x%x\n", *ppRWLock));
                            hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
                        }
                        else if(dwStatus == WAIT_IO_COMPLETION)
                        {
                            LOG((LF_SYNC, LL_WARNING,
                                "Thread interrupted while trying to acquire reader lock "
                                "for RWLock 0x%x\n", *ppRWLock));
                            hr = COR_E_THREADINTERRUPTED;
                        }
                        else if (dwStatus == WAIT_FAILED)
                        {
                            if (SUCCEEDED(hr))
                            {
                                dwStatus = GetLastError();
                                if (dwStatus == WAIT_OBJECT_0)
                                {
                                    dwStatus = WAIT_FAILED;
                                }
                                hr = HRESULT_FROM_WIN32(dwStatus);
                                LOG((LF_SYNC, LL_WARNING,
                                    "WaitForSingleObject on Event 0x%x failed for "
                                    "RWLock 0x%x with status code 0x%x\n",
                                    hReaderEvent, *ppRWLock, dwStatus));
                            }
                        }
                    }

                    // One less waiting reader and he may have become a reader
                    dwKnownState = RWInterlockedExchangeAdd(&(*ppRWLock)->_dwState, dwModifyState);

                    // Check for last signaled waiting reader
                    if(dwStatus == WAIT_OBJECT_0)
                    {
                        _ASSERTE(dwKnownState & READER_SIGNALED);
                        _ASSERTE((dwKnownState & READERS_MASK) < READERS_MASK);
                        if((dwKnownState & WAITING_READERS_MASK) == WAITING_READER)
                        {
                            // Reset the event and lower reader signaled flag
                            RWResetEvent(hReaderEvent);
                            RWInterlockedExchangeAdd(&(*ppRWLock)->_dwState, -READER_SIGNALED);
                        }
                    }
                    else
                    {
                        if(((dwKnownState & WAITING_READERS_MASK) == WAITING_READER) &&
                           (dwKnownState & READER_SIGNALED))
                        {
                            HRESULT hr1;
                            if(hReaderEvent == NULL)
                                hReaderEvent = (*ppRWLock)->GetReaderEvent(&hr1);
                            _ASSERTE(hReaderEvent);

                            // Ensure the event is signalled before resetting it.
                            DWORD dwTemp;
                            dwTemp = hReaderEvent->Wait(INFINITE, FALSE);
                            _ASSERTE(dwTemp == WAIT_OBJECT_0);
                            _ASSERTE(((*ppRWLock)->_dwState & READERS_MASK) < READERS_MASK);
                            
                            // Reset the event and lower reader signaled flag
                            RWResetEvent(hReaderEvent);
                            RWInterlockedExchangeAdd(&(*ppRWLock)->_dwState, (READER - READER_SIGNALED));

                            // Honor the orginal status
                            ++pLockEntry->wReaderLevel;
                            INCTHREADLOCKCOUNT();
                            USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
                            StaticReleaseReaderLock(ppRWLock);
                        }
                        else
                        {
                            (*ppRWLock)->FastRecycleLockEntry(pLockEntry);
                        }
                        
                        _ASSERTE((pLockEntry == NULL) ||
                                 ((pLockEntry->dwLLockID == 0) &&
                                  (pLockEntry->wReaderLevel == 0)));
                        if(fBreakOnErrors)  // fBreakOnErrors == FALSE so will be optimized out.
                        {
                            _ASSERTE(!"Failed to acquire reader lock");
                            DebugBreak();
                        }
                        
                        // Prepare the frame for throwing an exception
                        if ((DWORD)HOST_E_DEADLOCK == dwStatus)
                        {
                            // So that the error message is in the exception.
                            RaiseDeadLockException();
                        } else if ((DWORD)COR_E_THREADINTERRUPTED == dwStatus) {
                            COMPlusThrow(kThreadInterruptedException);
                        }
                        else
                        {
                            COMPlusThrowWin32 (hr);
                        }
                    }

                    // Sanity check
                    _ASSERTE(dwStatus == WAIT_OBJECT_0);
                    break;                        
                }
            }
            YieldProcessor();           // Indicate to the processor that we are spining
        } while(TRUE);
    }

    // Success
    _ASSERTE(dwStatus == WAIT_OBJECT_0);
    _ASSERTE(((*ppRWLock)->_dwState & WRITER) == 0);
    _ASSERTE((*ppRWLock)->_dwState & READERS_MASK);
    ++pLockEntry->wReaderLevel;
    INCTHREADLOCKCOUNT();
    USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
#ifdef RWLOCK_STATISTICS
    RWInterlockedIncrement(&(*ppRWLock)->_dwReaderEntryCount);
#endif
    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticAcquireWriterLockPublic    public
//
//  Synopsis:   Public access to StaticAcquireWriterLock
//
//+-------------------------------------------------------------------
FCIMPL2(void, CRWLock::StaticAcquireWriterLockPublic, CRWLock *pRWLockUNSAFE, DWORD dwDesiredTimeout)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);

    StaticAcquireWriterLock((CRWLock**)&pRWLock, dwDesiredTimeout);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticAcquireWriterLock    private
//
//  Synopsis:   Makes the thread a writer. Supports nested writer
//              locks
// 
//+-------------------------------------------------------------------

void CRWLock::StaticAcquireWriterLock(
    CRWLock **ppRWLock, 
    DWORD dwDesiredTimeout)
{
    CONTRACTL
    {
        THROWS;               
        GC_TRIGGERS;            // CLREvent::Wait can trigger GC
        CAN_TAKE_LOCK;
        PRECONDITION((CheckPointer(ppRWLock)));
        PRECONDITION((CheckPointer(*ppRWLock)));
    }
    CONTRACTL_END;

    TESTHOOKCALL(AppDomainCanBeUnloaded(GetThread()->GetDomain()->GetId().m_dwId,FALSE));    
    if (GetThread()->IsAbortRequested()) {
        GetThread()->HandleThreadAbort();
    }

    // Declare locals needed for setting up frame
    DWORD dwThreadID = GetThread()->GetThreadId();
    DWORD dwStatus;

    // Check for the fast path
    if(RWInterlockedCompareExchange(&(*ppRWLock)->_dwState, WRITER, 0) == 0)
    {
        _ASSERTE(((*ppRWLock)->_dwState & READERS_MASK) == 0);
    }
    // Check if the thread already has writer lock
    else if((*ppRWLock)->_dwWriterID == dwThreadID)
    {
        if ((*ppRWLock)->_wWriterLevel == RWLOCK_MAX_ACQUIRE_COUNT) {
            COMPlusThrow(kOverflowException, W("Overflow_UInt16"));        
        }
        ++(*ppRWLock)->_wWriterLevel;
        INCTHREADLOCKCOUNT();
        USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
        return;
    }
    else
    {
        DWORD dwCurrentState, dwKnownState;
        DWORD dwSpinCount;

        // Initialize
        dwSpinCount = 0;
        dwCurrentState = (*ppRWLock)->_dwState;
        do
        {
            dwKnownState = dwCurrentState;

            // Writer need not wait if there are no readers and writer
            if((dwKnownState == 0) || (dwKnownState == CACHING_EVENTS))
            {
                // Can be a writer
                dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              (dwKnownState + WRITER),
                                                              dwKnownState);
                if(dwCurrentState == dwKnownState)
                {
                    // Only writer
                    break;
                }
            }
            // Check for too many waiting writers
            else if(((dwKnownState & WAITING_WRITERS_MASK) == WAITING_WRITERS_MASK))
            {
                // Sleep
                GetThread()->UserSleep(1000);
                
                // Update to latest state
                dwSpinCount = 0;
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            // Check if events are being cached
            else if((dwKnownState & CACHING_EVENTS) == CACHING_EVENTS)
            {
                if(++dwSpinCount > gdwDefaultSpinCount)
                {
                    RWSleep(1);
                    dwSpinCount = 0;
                }
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            // Check spin count
            else if(++dwSpinCount <= gdwDefaultSpinCount)
            {
                dwCurrentState = (*ppRWLock)->_dwState;
            }
            else
            {
                // Add to waiting writers
                dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              (dwKnownState + WAITING_WRITER),
                                                              dwKnownState);
                if(dwCurrentState == dwKnownState)
                {
                    CLREvent* hWriterEvent;
                    DWORD dwModifyState;

                    // One more waiting writer
#ifdef RWLOCK_STATISTICS
                    RWInterlockedIncrement(&(*ppRWLock)->_dwWriterContentionCount);
#endif
                    HRESULT hr;
                    hWriterEvent = (*ppRWLock)->GetWriterEvent(&hr);
                    if(hWriterEvent)
                    {
                        dwStatus = RWWaitForSingleObject(hWriterEvent, dwDesiredTimeout);
                        VALIDATE_LOCK(*ppRWLock);
                    }
                    else
                    {
                        LOG((LF_SYNC, LL_WARNING,
                            "AcquireWriterLock failed to create writer "
                            "event for RWLock 0x%x\n", *ppRWLock));
                        dwStatus = WAIT_FAILED;
                    }

                    if(dwStatus == WAIT_OBJECT_0)
                    {
                        _ASSERTE((*ppRWLock)->_dwState & WRITER_SIGNALED);
                        dwModifyState = WRITER - WAITING_WRITER - WRITER_SIGNALED;
                    }
                    else
                    {
                        dwModifyState = (DWORD) -WAITING_WRITER;
                        if(dwStatus == WAIT_TIMEOUT)
                        {
                            LOG((LF_SYNC, LL_WARNING,
                                "Timed out trying to acquire writer "
                                "lock for RWLock 0x%x\n", *ppRWLock));
                            hr = HRESULT_FROM_WIN32 (ERROR_TIMEOUT);
                        }
                        else if(dwStatus == WAIT_IO_COMPLETION)
                        {
                            LOG((LF_SYNC, LL_WARNING,
                                "Thread interrupted while trying to acquire writer lock "
                                "for RWLock 0x%x\n", *ppRWLock));
                            hr = COR_E_THREADINTERRUPTED;
                        }
                        else if (dwStatus == WAIT_FAILED)
                        {
                            if (SUCCEEDED(hr))
                            {
                                dwStatus = GetLastError();
                                if (dwStatus == WAIT_OBJECT_0)
                                {
                                    dwStatus = WAIT_FAILED;
                                }
                                hr = HRESULT_FROM_WIN32(dwStatus);
                                LOG((LF_SYNC, LL_WARNING,
                                    "WaitForSingleObject on Event 0x%x failed for "
                                    "RWLock 0x%x with status code 0x%x",
                                    hWriterEvent, *ppRWLock, dwStatus));
                            }
                        }
                    }

                    // One less waiting writer and he may have become a writer
                    dwKnownState = RWInterlockedExchangeAdd(&(*ppRWLock)->_dwState, dwModifyState);

                    // Check for last timing out signaled waiting writer
                    if(dwStatus == WAIT_OBJECT_0)
                    {
                        // Common case
                    }
                    else
                    {
                        if((dwKnownState & WRITER_SIGNALED) &&
                           ((dwKnownState & WAITING_WRITERS_MASK) == WAITING_WRITER))
                        {
                            HRESULT hr1;
                            if(hWriterEvent == NULL)
                                hWriterEvent = (*ppRWLock)->GetWriterEvent(&hr1);
                            _ASSERTE(hWriterEvent);
                            do
                            {
                                dwKnownState = (*ppRWLock)->_dwState;
                                if((dwKnownState & WRITER_SIGNALED) &&
                                   ((dwKnownState & WAITING_WRITERS_MASK) == 0))
                                {
                                    DWORD dwTemp = hWriterEvent->Wait(10, FALSE);
                                    if(dwTemp == WAIT_OBJECT_0)
                                    {
                                        dwKnownState = RWInterlockedExchangeAdd(&(*ppRWLock)->_dwState, (WRITER - WRITER_SIGNALED));
                                        _ASSERTE(dwKnownState & WRITER_SIGNALED);
                                        _ASSERTE((dwKnownState & WRITER) == 0);

                                        // Honor the orginal status
                                        (*ppRWLock)->_dwWriterID = dwThreadID;
                                        Thread *pThread = GetThread();
                                        _ASSERTE (pThread);
                                        _ASSERTE ((*ppRWLock)->_wWriterLevel == 0);
                                        pThread->m_dwLockCount ++;
                                        USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
                                        (*ppRWLock)->_wWriterLevel = 1;
                                        StaticReleaseWriterLock(ppRWLock);
                                        break;
                                    }
                                    // else continue;
                                }
                                else
                                    break;
                            }while(TRUE);
                        }

                        if(fBreakOnErrors) // fBreakOnErrors == FALSE so will be optimized out.
                        {
                            _ASSERTE(!"Failed to acquire writer lock");
                            DebugBreak();
                        }
                        
                        // Prepare the frame for throwing an exception
                        if ((DWORD)HOST_E_DEADLOCK == dwStatus)
                        {
                            // So that the error message is in the exception.
                            RaiseDeadLockException();
                        } else if ((DWORD)COR_E_THREADINTERRUPTED == dwStatus) {
                            COMPlusThrow(kThreadInterruptedException);
                        }
                        else
                        {
                            COMPlusThrowWin32(hr);
                        }
                    }

                    // Sanity check
                    _ASSERTE(dwStatus == WAIT_OBJECT_0);
                    break;
                }
            }
            YieldProcessor();       // indicate to the processor that we are spinning 
        } while(TRUE);
    }

    // Success
    _ASSERTE((*ppRWLock)->_dwState & WRITER);
    _ASSERTE(((*ppRWLock)->_dwState & READERS_MASK) == 0);
    _ASSERTE((*ppRWLock)->_dwWriterID == 0);

    // Save threadid of the writer
    (*ppRWLock)->_dwWriterID = dwThreadID;
    (*ppRWLock)->_wWriterLevel = 1;
    INCTHREADLOCKCOUNT();
    USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
    ++(*ppRWLock)->_dwWriterSeqNum;
#ifdef RWLOCK_STATISTICS
    ++(*ppRWLock)->_dwWriterEntryCount;
#endif
    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticReleaseWriterLockPublic    public
//
//  Synopsis:   Public access to StaticReleaseWriterLock
//
//+-------------------------------------------------------------------
FCIMPL1(void, CRWLock::StaticReleaseWriterLockPublic, CRWLock *pRWLockUNSAFE)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT, pRWLock);

    // We don't want to block thread abort when we need to construct exception in
    // unwind-continue handler.
    // note that we cannot use this holder in FCALLs outside our HMF since it breaks the epilog walker on x86!
    ThreadPreventAbortHolder preventAbortIn;

    StaticReleaseWriterLock((CRWLock**)&pRWLock);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticReleaseWriterLock    private
//
//  Synopsis:   Removes the thread as a writer if not a nested
//              call to release the lock
// 
//+-------------------------------------------------------------------
void CRWLock::StaticReleaseWriterLock(
    CRWLock **ppRWLock)
{

    CONTRACTL
    {
        THROWS;               
        GC_TRIGGERS;
        PRECONDITION((CheckPointer(ppRWLock)));
        PRECONDITION((CheckPointer(*ppRWLock)));
    }
    CONTRACTL_END;

    DWORD dwThreadID = GetThread()->GetThreadId();

    // Check validity of caller
    if((*ppRWLock)->_dwWriterID == dwThreadID)
    {
        DECTHREADLOCKCOUNT();
        USER_LOCK_RELEASED(GetPtrForLockContract(ppRWLock));
        // Check for nested release
        if(--(*ppRWLock)->_wWriterLevel == 0)
        {
            DWORD dwCurrentState, dwKnownState, dwModifyState;
            BOOL fCacheEvents;
            CLREvent* hReaderEvent = NULL, *hWriterEvent = NULL;

            // Not a writer any more
            (*ppRWLock)->_dwWriterID = 0;
            dwCurrentState = (*ppRWLock)->_dwState;
            do
            {
                dwKnownState = dwCurrentState;
                dwModifyState = (DWORD) -WRITER;
                fCacheEvents = FALSE;
                if(dwKnownState & WAITING_READERS_MASK)
                {
                    HRESULT hr;
                    hReaderEvent = (*ppRWLock)->GetReaderEvent(&hr);
                    if(hReaderEvent == NULL)
                    {
                        LOG((LF_SYNC, LL_WARNING,
                            "ReleaseWriterLock failed to create "
                            "reader event for RWLock 0x%x\n", *ppRWLock));
                        RWSleep(100);
                        dwCurrentState = (*ppRWLock)->_dwState;
                        dwKnownState = 0;
                        _ASSERTE(dwCurrentState != dwKnownState);
                        continue;
                    }
                    dwModifyState += READER_SIGNALED;
                }
                else if(dwKnownState & WAITING_WRITERS_MASK)
                {
                    HRESULT hr;
                    hWriterEvent = (*ppRWLock)->GetWriterEvent(&hr);
                    if(hWriterEvent == NULL)
                    {
                        LOG((LF_SYNC, LL_WARNING,
                            "ReleaseWriterLock failed to create "
                            "writer event for RWLock 0x%x\n", *ppRWLock));
                        RWSleep(100);
                        dwCurrentState = (*ppRWLock)->_dwState;
                        dwKnownState = 0;
                        _ASSERTE(dwCurrentState != dwKnownState);
                        continue;
                    }
                    dwModifyState += WRITER_SIGNALED;
                }
                else if(((*ppRWLock)->_hReaderEvent || (*ppRWLock)->_hWriterEvent) &&
                        (dwKnownState == WRITER))
                {
                    fCacheEvents = TRUE;
                    dwModifyState += CACHING_EVENTS;
                }

                // Sanity checks
                _ASSERTE((dwKnownState & READERS_MASK) == 0);

                dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              (dwKnownState + dwModifyState),
                                                              dwKnownState);
            } while(dwCurrentState != dwKnownState);

            // Check for waiting readers
            if(dwKnownState & WAITING_READERS_MASK)
            {
                _ASSERTE((*ppRWLock)->_dwState & READER_SIGNALED);
                _ASSERTE(hReaderEvent);
                RWSetEvent(hReaderEvent);
            }
            // Check for waiting writers
            else if(dwKnownState & WAITING_WRITERS_MASK)
            {
                _ASSERTE((*ppRWLock)->_dwState & WRITER_SIGNALED);
                _ASSERTE(hWriterEvent);
                RWSetEvent(hWriterEvent);
            }
            // Check for the need to release events
            else if(fCacheEvents)
            {
                (*ppRWLock)->ReleaseEvents();
            }
            
            Thread *pThread = GetThread();
            TESTHOOKCALL(AppDomainCanBeUnloaded(pThread->GetDomain()->GetId().m_dwId,FALSE));    
            if (pThread->IsAbortRequested()) {
                pThread->HandleThreadAbort();
            }

        }
    }
    else
    {
        if(fBreakOnErrors) // fBreakOnErrors == FALSE so will be optimized out.
        {
            _ASSERTE(!"Attempt to release writer lock on a wrong thread");
            DebugBreak();
        }
        COMPlusThrowWin32(ERROR_NOT_OWNER);
    }

    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticReleaseReaderLockPublic    public
//
//  Synopsis:   Public access to StaticReleaseReaderLock
//
//+-------------------------------------------------------------------
FCIMPL1(void, CRWLock::StaticReleaseReaderLockPublic, CRWLock *pRWLockUNSAFE)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT, pRWLock);

    // note that we cannot use this holder in FCALLs outside our HMF since it breaks the epilog walker on x86!
    ThreadPreventAbortHolder preventAbortIn;

    StaticReleaseReaderLock((CRWLock**)&pRWLock);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticReleaseReaderLock    private
//
//  Synopsis:   Removes the thread as a reader
// 
//+-------------------------------------------------------------------

void CRWLock::StaticReleaseReaderLock(
    CRWLock **ppRWLock)
{
    CONTRACTL
    {
        THROWS;               
        GC_TRIGGERS;
        PRECONDITION((CheckPointer(ppRWLock)));
        PRECONDITION((CheckPointer(*ppRWLock)));
    }
    CONTRACTL_END;

    // Check if the thread has writer lock
    if((*ppRWLock)->_dwWriterID == GetThread()->GetThreadId())
    {
        StaticReleaseWriterLock(ppRWLock);
    }
    else
    {
        LockEntry *pLockEntry = (*ppRWLock)->GetLockEntry();
        if(pLockEntry)
        {
            --pLockEntry->wReaderLevel;
            DECTHREADLOCKCOUNT();
            USER_LOCK_RELEASED(GetPtrForLockContract(ppRWLock));
            if(pLockEntry->wReaderLevel == 0)
            {
                DWORD dwCurrentState, dwKnownState, dwModifyState;
                BOOL fLastReader, fCacheEvents = FALSE;
                CLREvent* hReaderEvent = NULL, *hWriterEvent = NULL;

                // Sanity checks
                _ASSERTE(((*ppRWLock)->_dwState & WRITER) == 0);
                _ASSERTE((*ppRWLock)->_dwState & READERS_MASK);

                // Not a reader any more
                dwCurrentState = (*ppRWLock)->_dwState;
                do
                {
                    dwKnownState = dwCurrentState;
                    dwModifyState = (DWORD) -READER;
                    if((dwKnownState & (READERS_MASK | READER_SIGNALED)) == READER)
                    {
                        fLastReader = TRUE;
                        fCacheEvents = FALSE;
                        if(dwKnownState & WAITING_WRITERS_MASK)
                        {
                            HRESULT hr;
                            hWriterEvent = (*ppRWLock)->GetWriterEvent(&hr);
                            if(hWriterEvent == NULL)
                            {
                                LOG((LF_SYNC, LL_WARNING,
                                    "ReleaseReaderLock failed to create "
                                    "writer event for RWLock 0x%x\n", *ppRWLock));
                                RWSleep(100);
                                dwCurrentState = (*ppRWLock)->_dwState;
                                dwKnownState = 0;
                                _ASSERTE(dwCurrentState != dwKnownState);
                                continue;
                            }
                            dwModifyState += WRITER_SIGNALED;
                        }
                        else if(dwKnownState & WAITING_READERS_MASK)
                        {
                            HRESULT hr;
                            hReaderEvent = (*ppRWLock)->GetReaderEvent(&hr);
                            if(hReaderEvent == NULL)
                            {
                                LOG((LF_SYNC, LL_WARNING,
                                    "ReleaseReaderLock failed to create "
                                    "reader event\n", *ppRWLock));
                                RWSleep(100);
                                dwCurrentState = (*ppRWLock)->_dwState;
                                dwKnownState = 0;
                                _ASSERTE(dwCurrentState != dwKnownState);
                                continue;
                            }
                            dwModifyState += READER_SIGNALED;
                        }
                        else if(((*ppRWLock)->_hReaderEvent || (*ppRWLock)->_hWriterEvent) &&
                                (dwKnownState == READER))
                        {
                            fCacheEvents = TRUE;
                            dwModifyState += CACHING_EVENTS;
                        }
                    }
                    else
                    {
                        fLastReader = FALSE;
                    }

                    // Sanity checks
                    _ASSERTE((dwKnownState & WRITER) == 0);
                    _ASSERTE(dwKnownState & READERS_MASK);

                    dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                                  (dwKnownState + dwModifyState),
                                                                  dwKnownState);
                } while(dwCurrentState != dwKnownState);

                // Check for last reader
                if(fLastReader)
                {
                    // Check for waiting writers
                    if(dwKnownState & WAITING_WRITERS_MASK)
                    {
                        _ASSERTE((*ppRWLock)->_dwState & WRITER_SIGNALED);
                        _ASSERTE(hWriterEvent);
                        RWSetEvent(hWriterEvent);
                    }
                    // Check for waiting readers
                    else if(dwKnownState & WAITING_READERS_MASK)
                    {
                        _ASSERTE((*ppRWLock)->_dwState & READER_SIGNALED);
                        _ASSERTE(hReaderEvent);
                        RWSetEvent(hReaderEvent);
                    }
                    // Check for the need to release events
                    else if(fCacheEvents)
                    {
                        (*ppRWLock)->ReleaseEvents();
                    }
                }

                // Recycle lock entry
                RecycleLockEntry(pLockEntry);
                
                Thread *pThread = GetThread();
                TESTHOOKCALL(AppDomainCanBeUnloaded(pThread->GetDomain()->GetId().m_dwId,FALSE));    
                
                if (pThread->IsAbortRequested()) {
                    pThread->HandleThreadAbort();
                }
            }
        }
        else
        {
            if(fBreakOnErrors)  // fBreakOnErrors == FALSE so will be optimized out.
            {
                _ASSERTE(!"Attempt to release reader lock on a wrong thread");
                DebugBreak();
            }
            COMPlusThrowWin32(ERROR_NOT_OWNER);
        }
    }

    return;
}


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticDoUpgradeToWriterLockPublic    private
//
//  Synopsis:   Public Access to StaticUpgradeToWriterLockPublic
//
// 
//+-------------------------------------------------------------------
FCIMPL3(void, CRWLock::StaticDoUpgradeToWriterLockPublic, CRWLock *pRWLockUNSAFE, LockCookie * pLockCookie, DWORD dwDesiredTimeout)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);
    GCPROTECT_BEGININTERIOR (pLockCookie)

    StaticUpgradeToWriterLock((CRWLock**)&pRWLock, pLockCookie, dwDesiredTimeout);

    GCPROTECT_END ();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticUpgradeToWriterLock    Private
//
//  Synopsis:   Upgrades to a writer lock. It returns a BOOL that
//              indicates intervening writes.
//

//
//+-------------------------------------------------------------------

void CRWLock::StaticUpgradeToWriterLock(
    CRWLock **ppRWLock, 
    LockCookie *pLockCookie, 
    DWORD dwDesiredTimeout)

{
    CONTRACTL
    {
        THROWS;               
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD dwThreadID = GetThread()->GetThreadId();

    // Check if the thread is already a writer
    if((*ppRWLock)->_dwWriterID == dwThreadID)
    {
        // Update cookie state
        pLockCookie->dwFlags = UPGRADE_COOKIE | COOKIE_WRITER;
        pLockCookie->wWriterLevel = (*ppRWLock)->_wWriterLevel;

        // Acquire the writer lock again
        StaticAcquireWriterLock(ppRWLock, dwDesiredTimeout);
    }
    else
    {
        BOOL fAcquireWriterLock;
        LockEntry *pLockEntry = (*ppRWLock)->GetLockEntry();
        if(pLockEntry == NULL)
        {
            fAcquireWriterLock = TRUE;
            pLockCookie->dwFlags = UPGRADE_COOKIE | COOKIE_NONE;
        }
        else
        {
            // Sanity check
            _ASSERTE((*ppRWLock)->_dwState & READERS_MASK);
            _ASSERTE(pLockEntry->wReaderLevel);

            // Save lock state in the cookie
            pLockCookie->dwFlags = UPGRADE_COOKIE | COOKIE_READER;
            pLockCookie->wReaderLevel = pLockEntry->wReaderLevel;
            pLockCookie->dwWriterSeqNum = (*ppRWLock)->_dwWriterSeqNum;

            // If there is only one reader, try to convert reader to a writer
            DWORD dwKnownState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                              WRITER,
                                                              READER);
            if(dwKnownState == READER)
            {
                // Thread is no longer a reader
                Thread* pThread = GetThread();
                _ASSERTE (pThread);
                _ASSERTE (pThread->m_dwLockCount >= pLockEntry->wReaderLevel);
                ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= pLockEntry->wReaderLevel);
                pThread->m_dwLockCount -= pLockEntry->wReaderLevel;
                USER_LOCK_RELEASED_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract(ppRWLock));
                pLockEntry->wReaderLevel = 0;
                RecycleLockEntry(pLockEntry);

                // Thread is a writer
                (*ppRWLock)->_dwWriterID = dwThreadID;
                (*ppRWLock)->_wWriterLevel = 1;
                INCTHREADLOCKCOUNT();
                USER_LOCK_TAKEN(GetPtrForLockContract(ppRWLock));
                ++(*ppRWLock)->_dwWriterSeqNum;
                fAcquireWriterLock = FALSE;

                // No intevening writes
#if RWLOCK_STATISTICS
                ++(*ppRWLock)->_dwWriterEntryCount;
#endif
            }
            else
            {
                // Release the reader lock
                Thread *pThread = GetThread();
                _ASSERTE (pThread);
                _ASSERTE (pThread->m_dwLockCount >= (DWORD)(pLockEntry->wReaderLevel - 1));
                ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= 
                                             (DWORD)(pLockEntry->wReaderLevel - 1));
                pThread->m_dwLockCount -= (pLockEntry->wReaderLevel - 1);
                USER_LOCK_RELEASED_MULTIPLE(pLockEntry->wReaderLevel - 1, GetPtrForLockContract(ppRWLock));
                pLockEntry->wReaderLevel = 1;
                StaticReleaseReaderLock(ppRWLock);
                fAcquireWriterLock = TRUE;
            }
        }

        // Check for the need to acquire the writer lock
        if(fAcquireWriterLock)
        {

            // Declare and Setup the frame as we are aware of the contention
            // on the lock and the thread will most probably block
            // to acquire writer lock

            EX_TRY
            {
                StaticAcquireWriterLock(ppRWLock, dwDesiredTimeout);
            }
            EX_CATCH
            {
                // Invalidate cookie
                DWORD dwFlags = pLockCookie->dwFlags; 
                pLockCookie->dwFlags = INVALID_COOKIE;

                StaticRecoverLock(ppRWLock, pLockCookie, dwFlags & COOKIE_READER);

                EX_RETHROW;
            }
            EX_END_CATCH_UNREACHABLE
        }
    }


    // Update the validation fields of the cookie 
    pLockCookie->dwThreadID = dwThreadID;

    return;
}

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticDowngradeFromWriterLock   public
//
//  Synopsis:   Downgrades from a writer lock.
// 
//+-------------------------------------------------------------------

inline CRWLock* GetLock(OBJECTREF orLock)
{
    CONTRACTL
    {
        NOTHROW;               
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return (CRWLock*)OBJECTREFToObject(orLock);
}

FCIMPL2(void, CRWLock::StaticDowngradeFromWriterLock, CRWLock *pRWLockUNSAFE, LockCookie* pLockCookie)
{
    FCALL_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    DWORD dwThreadID = GetThread()->GetThreadId();

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    if( NULL == pLockCookie) {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);

    if (GetLock(pRWLock)->_dwWriterID != dwThreadID)
    {
        COMPlusThrowWin32(ERROR_NOT_OWNER);
    }

    // Validate cookie
    DWORD dwStatus;
    if(((pLockCookie->dwFlags & INVALID_COOKIE) == 0) && 
       (pLockCookie->dwThreadID == dwThreadID))
    {
        DWORD dwFlags = pLockCookie->dwFlags;
        pLockCookie->dwFlags = INVALID_COOKIE;
        
        // Check if the thread was a reader
        if(dwFlags & COOKIE_READER)
        {
            // Sanity checks
            _ASSERTE(GetLock(pRWLock)->_wWriterLevel == 1);
    
            LockEntry *pLockEntry = GetLock(pRWLock)->FastGetOrCreateLockEntry();
            if(pLockEntry)
            {
                DWORD dwCurrentState, dwKnownState, dwModifyState;
                CLREvent* hReaderEvent = NULL;
    
                // Downgrade to a reader
                GetLock(pRWLock)->_dwWriterID = 0;
                GetLock(pRWLock)->_wWriterLevel = 0;
                DECTHREADLOCKCOUNT ();
                USER_LOCK_RELEASED(GetPtrForLockContract((CRWLock**)&pRWLock));
                dwCurrentState = GetLock(pRWLock)->_dwState;
                do
                {
                    dwKnownState = dwCurrentState;
                    dwModifyState = READER - WRITER;
                    if(dwKnownState & WAITING_READERS_MASK)
                    {
                        HRESULT hr;
                        hReaderEvent = GetLock(pRWLock)->GetReaderEvent(&hr);
                        if(hReaderEvent == NULL)
                        {
                            LOG((LF_SYNC, LL_WARNING,
                                "DowngradeFromWriterLock failed to create "
                                "reader event for RWLock 0x%x\n", GetLock(pRWLock)));
                            RWSleep(100);
                            dwCurrentState = GetLock(pRWLock)->_dwState;
                            dwKnownState = 0;
                            _ASSERTE(dwCurrentState != dwKnownState);
                            continue;
                        }
                        dwModifyState += READER_SIGNALED;
                    }
    
                    // Sanity checks
                    _ASSERTE((dwKnownState & READERS_MASK) == 0);
    
                    dwCurrentState = RWInterlockedCompareExchange(&GetLock(pRWLock)->_dwState,
                                                                  (dwKnownState + dwModifyState),
                                                                  dwKnownState);
                } while(dwCurrentState != dwKnownState);
    
                // Check for waiting readers
                if(dwKnownState & WAITING_READERS_MASK)
                {
                    _ASSERTE(GetLock(pRWLock)->_dwState & READER_SIGNALED);
                    _ASSERTE(hReaderEvent);
                    RWSetEvent(hReaderEvent);
                }
    
                // Restore reader nesting level
                Thread *pThread = GetThread();
                _ASSERTE (pThread);
                _ASSERTE (pThread->m_dwLockCount >= pLockEntry->wReaderLevel);
                ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= 
                                             pLockEntry->wReaderLevel);
                pThread->m_dwLockCount -= pLockEntry->wReaderLevel;
                USER_LOCK_RELEASED_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract((CRWLock**)&pRWLock));
                pLockEntry->wReaderLevel = pLockCookie->wReaderLevel;
                pThread->m_dwLockCount += pLockEntry->wReaderLevel;
                USER_LOCK_TAKEN_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract((CRWLock**)&pRWLock));
    #ifdef RWLOCK_STATISTICS
                RWInterlockedIncrement(&GetLock(pRWLock)->_dwReaderEntryCount);
    #endif
            }
            else
            {
                // Removed assert, as thread abort can occur normally
                dwStatus = RWLOCK_RECOVERY_FAILURE;
                goto ThrowException;
            }
        }
        else if(dwFlags & (COOKIE_WRITER | COOKIE_NONE))
        {
            // Release the writer lock
            StaticReleaseWriterLock((CRWLock**)&pRWLock);
            _ASSERTE((GetLock(pRWLock)->_dwWriterID != GetThread()->GetThreadId()) ||
                     (dwFlags & COOKIE_WRITER));
        }
    }
    else
    {
        dwStatus = E_INVALIDARG;
ThrowException:        
        COMPlusThrowWin32(dwStatus);
    }

    HELPER_METHOD_FRAME_END();

    // Update the validation fields of the cookie 
    pLockCookie->dwThreadID = dwThreadID;
    
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticDoReleaseLock    private
//
//  Synopsis:   Releases the lock held by the current thread
// 
//+-------------------------------------------------------------------

FCIMPL2(void, CRWLock::StaticDoReleaseLock, CRWLock *pRWLockUNSAFE, LockCookie * pLockCookie)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    DWORD dwThreadID = GetThread()->GetThreadId();

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT, pRWLock);

    // note that we cannot use this holder in FCALLs outside our HMF since it breaks the epilog walker on x86!
    ThreadPreventAbortHolder preventAbortIn;

    GCPROTECT_BEGININTERIOR (pLockCookie)

    // Check if the thread is a writer
    if(GetLock(pRWLock)->_dwWriterID == dwThreadID)
    {
        // Save lock state in the cookie
        pLockCookie->dwFlags = RELEASE_COOKIE | COOKIE_WRITER;
        pLockCookie->dwWriterSeqNum = GetLock(pRWLock)->_dwWriterSeqNum;
        pLockCookie->wWriterLevel = GetLock(pRWLock)->_wWriterLevel;

        // Release the writer lock
        Thread *pThread = GetThread();
        _ASSERTE (pThread);
        _ASSERTE (pThread->m_dwLockCount >= (DWORD)(GetLock(pRWLock)->_wWriterLevel - 1));
        ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >=
                                     (DWORD)(GetLock(pRWLock)->_wWriterLevel - 1));
        pThread->m_dwLockCount -= (GetLock(pRWLock)->_wWriterLevel - 1);
        USER_LOCK_RELEASED_MULTIPLE(GetLock(pRWLock)->_wWriterLevel - 1, GetPtrForLockContract((CRWLock**)&pRWLock));
        GetLock(pRWLock)->_wWriterLevel = 1;
        StaticReleaseWriterLock((CRWLock**)&pRWLock);
    }
    else
    {
        LockEntry *pLockEntry = GetLock(pRWLock)->GetLockEntry();
        if(pLockEntry)
        {
            // Sanity check
            _ASSERTE(GetLock(pRWLock)->_dwState & READERS_MASK);
            _ASSERTE(pLockEntry->wReaderLevel);

            // Save lock state in the cookie
            pLockCookie->dwFlags = RELEASE_COOKIE | COOKIE_READER;
            pLockCookie->wReaderLevel = pLockEntry->wReaderLevel;
            pLockCookie->dwWriterSeqNum = GetLock(pRWLock)->_dwWriterSeqNum;

            // Release the reader lock
            Thread *pThread = GetThread();
            _ASSERTE (pThread);
            _ASSERTE (pThread->m_dwLockCount >= (DWORD)(pLockEntry->wReaderLevel - 1));
            ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >= 
                                         (DWORD)(pLockEntry->wReaderLevel - 1));
            pThread->m_dwLockCount -= (pLockEntry->wReaderLevel - 1);
            USER_LOCK_RELEASED_MULTIPLE(pLockEntry->wReaderLevel - 1, GetPtrForLockContract((CRWLock**)&pRWLock));
            pLockEntry->wReaderLevel = 1;
            StaticReleaseReaderLock((CRWLock**)&pRWLock);
        }
        else
        {
            pLockCookie->dwFlags = RELEASE_COOKIE | COOKIE_NONE;
        }
    }

    GCPROTECT_END ();

    HELPER_METHOD_FRAME_END();

    // Update the validation fields of the cookie 
    pLockCookie->dwThreadID = dwThreadID;
}
FCIMPLEND

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticRestoreLockPublic    public
//
//  Synopsis:   Public Access to StaticRestoreLock
//
// 
//+-------------------------------------------------------------------

FCIMPL2(void, CRWLock::StaticRestoreLockPublic, CRWLock *pRWLockUNSAFE, LockCookie* pLockCookie)
{
    FCALL_CONTRACT;

    if (pRWLockUNSAFE == NULL) {
        FCThrowVoid(kNullReferenceException);
    }

    if( NULL == pLockCookie) {
        FCThrowVoid(kNullReferenceException);
    }

    OBJECTREF pRWLock = ObjectToOBJECTREF((Object*)pRWLockUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);

    StaticRestoreLock((CRWLock**)&pRWLock, pLockCookie);
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//+-------------------------------------------------------------------
//
//  Method:     CRWLock::StaticRestoreLock  Private
//
//  Synopsis:   Restore the lock held by the current thread
//

//
//+-------------------------------------------------------------------

void CRWLock::StaticRestoreLock(
    CRWLock **ppRWLock, 
    LockCookie *pLockCookie)
{
    CONTRACTL
    {
        THROWS;     
        CAN_TAKE_LOCK;
        GC_TRIGGERS;           // CRWLock::StaticAquireWriterLock can trigger GC
    }
    CONTRACTL_END;

    // Validate cookie
    DWORD dwThreadID = GetThread()->GetThreadId();
    DWORD dwFlags = pLockCookie->dwFlags;
    if(pLockCookie->dwThreadID == dwThreadID)
    {
        if (((*ppRWLock)->_dwWriterID == dwThreadID) || ((*ppRWLock)->GetLockEntry() != NULL))
        {
            COMPlusThrow(kSynchronizationLockException, W("Arg_RWLockRestoreException"));
        }
    
        // Check for the no contention case
        pLockCookie->dwFlags = INVALID_COOKIE;
        if(dwFlags & COOKIE_WRITER)
        {
            if(RWInterlockedCompareExchange(&(*ppRWLock)->_dwState, WRITER, 0) == 0)
            {
                // Restore writer nesting level
                (*ppRWLock)->_dwWriterID = dwThreadID;
                Thread *pThread = GetThread();
                _ASSERTE (pThread);
                _ASSERTE (pThread->m_dwLockCount >= (*ppRWLock)->_wWriterLevel);
                ASSERT_UNLESS_NO_DEBUG_STATE(__pClrDebugState->GetLockCount(kDbgStateLockType_User) >=
                                             (*ppRWLock)->_wWriterLevel);
                pThread->m_dwLockCount -= (*ppRWLock)->_wWriterLevel;
                USER_LOCK_RELEASED_MULTIPLE((*ppRWLock)->_wWriterLevel, GetPtrForLockContract(ppRWLock));
                (*ppRWLock)->_wWriterLevel = pLockCookie->wWriterLevel;
                pThread->m_dwLockCount += (*ppRWLock)->_wWriterLevel;
                USER_LOCK_TAKEN_MULTIPLE((*ppRWLock)->_wWriterLevel, GetPtrForLockContract(ppRWLock));
                ++(*ppRWLock)->_dwWriterSeqNum;
#ifdef RWLOCK_STATISTICS
                ++(*ppRWLock)->_dwWriterEntryCount;
#endif
                goto LNormalReturn;
            }
        }
        else if(dwFlags & COOKIE_READER)
        {
            LockEntry *pLockEntry = (*ppRWLock)->FastGetOrCreateLockEntry();
            if(pLockEntry)
            {
                // This thread should not already be a reader
                // else bad things can happen
                _ASSERTE(pLockEntry->wReaderLevel == 0);
                DWORD dwKnownState = (*ppRWLock)->_dwState;
                if(dwKnownState < READERS_MASK)
                {
                    DWORD dwCurrentState = RWInterlockedCompareExchange(&(*ppRWLock)->_dwState,
                                                                        (dwKnownState + READER),
                                                                        dwKnownState);
                    if(dwCurrentState == dwKnownState)
                    {
                        // Restore reader nesting level
                        Thread *pThread = GetThread();
                        _ASSERTE (pThread);
                        pLockEntry->wReaderLevel = pLockCookie->wReaderLevel;
                        pThread->m_dwLockCount += pLockEntry->wReaderLevel;
                        USER_LOCK_TAKEN_MULTIPLE(pLockEntry->wReaderLevel, GetPtrForLockContract(ppRWLock));
#ifdef RWLOCK_STATISTICS
                        RWInterlockedIncrement(&(*ppRWLock)->_dwReaderEntryCount);
#endif
                        goto LNormalReturn;
                    }
                }
    
                // Recycle the lock entry for the slow case
                (*ppRWLock)->FastRecycleLockEntry(pLockEntry);
            }
            else
            {
                // Ignore the error and try again below. May be thread will luck
                // out the second time
            }
        }
        else if(dwFlags & COOKIE_NONE) 
        {
            goto LNormalReturn;
        }

        // Declare and Setup the frame as we are aware of the contention
        // on the lock and the thread will most probably block
        // to acquire lock below
ThrowException:        
        if((dwFlags & INVALID_COOKIE) == 0)
        {
            StaticRecoverLock(ppRWLock, pLockCookie, dwFlags);
        }
        else
        {
            COMPlusThrowWin32(E_INVALIDARG);
        }

        goto LNormalReturn;
    }
    else
    {
        dwFlags = INVALID_COOKIE;
        goto ThrowException;
    }

LNormalReturn:
    return;
}


//+-------------------------------------------------------------------
//
//  Class:      CRWLock::StaticPrivateInitialize
//
//  Synopsis:   Initialize lock
// 
//+-------------------------------------------------------------------
FCIMPL1(void, CRWLock::StaticPrivateInitialize, CRWLock *pRWLock)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_1(pRWLock);

    // Run the constructor on the GC allocated space
    // CRWLock's constructor can throw exception
#ifndef _PREFAST_
    // Prefast falsely complains of memory leak.
    CRWLock *pTemp;
    pTemp = new (pRWLock) CRWLock();
    _ASSERTE(pTemp == pRWLock);
#endif

    // Catch GC holes
    VALIDATE_LOCK(pRWLock);

    HELPER_METHOD_FRAME_END();
    return;
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Class:      CRWLock::StaticPrivateDestruct
//
//  Synopsis:   Destruct lock
//+-------------------------------------------------------------------
FCIMPL1(void, CRWLock::StaticPrivateDestruct, CRWLock *pRWLock)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT, pRWLock);

    // Fixing one handle recycling security hole by
    // ensuring we don't delete the events more than once.
    // After deletion (for now, assuming ONE FINALIZER THREAD)
    // make the object essentially unusable by setting handle to 
    // INVALID_HANDLE_VALUE (unusable) versus NULL (uninitialized)
    
    if ((pRWLock->_hWriterEvent != INVALID_HANDLE_VALUE) && (pRWLock->_hReaderEvent != INVALID_HANDLE_VALUE))
    {
        // Note, this still allows concurrent event consumers (such as StaticAcquireReaderLock)
        // to Set and/or Wait on non-events.  There still exists a security hole here.
        if(pRWLock->_hWriterEvent)
        {
            CLREvent *h = (CLREvent *) FastInterlockExchangePointer((PVOID *)&(pRWLock->_hWriterEvent), INVALID_HANDLE_VALUE);
            delete h;
        }
        if(pRWLock->_hReaderEvent)
        {
            CLREvent *h = (CLREvent *) FastInterlockExchangePointer((PVOID *)&(pRWLock->_hReaderEvent), INVALID_HANDLE_VALUE);
            delete h;
        }

        // There is no LockEntry for this lock.
        if (pRWLock->_dwState != 0)
        {
            // Recycle LockEntry on threads
            ThreadStoreLockHolder tsl;

            // Take ThreadStore lock and walk over every thread in the process
            Thread *thread = NULL;
            while ((thread = ThreadStore::s_pThreadStore->GetAllThreadList(thread, 
                            Thread::TS_Unstarted|Thread::TS_Dead|Thread::TS_Detached, 0))
                != NULL) 
            {
                LockEntry *pLockEntry;
                {
                    CrstHolder rwl(&s_RWLockCrst);
                    pLockEntry = pRWLock->GetLockEntry(thread);
                }
                if (pLockEntry)
                {
                    // The entry does not belong to this lock anymore
                    pLockEntry->dwLLockID = 0;
                    pLockEntry->wReaderLevel = 0;
                }
            }
        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Class:      CRWLock::StaticGetWriterSeqNum
//
//  Synopsis:   Returns the current sequence number
// 
//+-------------------------------------------------------------------
FCIMPL1(INT32, CRWLock::StaticGetWriterSeqNum, CRWLock *pRWLock)
{
    FCALL_CONTRACT;

    if (pRWLock == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    return(pRWLock->_dwWriterSeqNum);
}    
FCIMPLEND


//+-------------------------------------------------------------------
//
//  Class:      CRWLock::StaticAnyWritersSince
//
//  Synopsis:   Returns TRUE if there were writers since the given
//              sequence number
// 
//+-------------------------------------------------------------------
FCIMPL2(FC_BOOL_RET, CRWLock::StaticAnyWritersSince, CRWLock *pRWLock, DWORD dwSeqNum)
{
    FCALL_CONTRACT;

    if (pRWLock == NULL)
    {
        FCThrow(kNullReferenceException);
    }
    

    if(pRWLock->_dwWriterID == GetThread()->GetThreadId())
        ++dwSeqNum;

    FC_RETURN_BOOL(pRWLock->_dwWriterSeqNum > dwSeqNum);
}
FCIMPLEND

struct RWLockIterator
{
    IHostTask **m_Owner;
    DWORD  m_Capacity;
    DWORD  m_index;
};

OBJECTHANDLE CRWLock::GetObjectHandle()
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (_hObjectHandle == NULL)
    {
        OBJECTREF obj = ObjectToOBJECTREF((Object*)this);
        OBJECTHANDLE handle = GetAppDomain()->CreateLongWeakHandle(obj);
        if (RWInterlockedCompareExchangePointer((PVOID*)&_hObjectHandle, handle, NULL) != NULL)
        {
            DestroyLongWeakHandle(handle);
        }
    }
    return _hObjectHandle;
}

// CRWLock::CreateOwnerIterator can return E_OUTOFMEMORY
//
HRESULT CRWLock::CreateOwnerIterator(SIZE_T *pIterator)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_NOTRIGGER;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    *pIterator = 0;
    if (_dwState == 0) {
        return S_OK;
    }
    NewHolder<RWLockIterator> IteratorHolder(new (nothrow) RWLockIterator);
    RWLockIterator *pRWLockIterator = IteratorHolder;
    if (pRWLockIterator == NULL) {
        return E_OUTOFMEMORY;
    }
    // Writer can be handled fast
    if (_dwState & WRITER) {
        DWORD writerID = _dwWriterID;
        if (writerID != 0)
        {
            pRWLockIterator->m_Capacity = 1;
            pRWLockIterator->m_index = 0;
            pRWLockIterator->m_Owner = new (nothrow) IHostTask*[1];
            if (pRWLockIterator->m_Owner == NULL) {
                return E_OUTOFMEMORY;
            }
            Thread *pThread = g_pThinLockThreadIdDispenser->IdToThreadWithValidation(writerID);
            if (pThread == NULL)
            {
                return S_OK;
            }
            IteratorHolder.SuppressRelease();
            pRWLockIterator->m_Owner[0] = pThread->GetHostTaskWithAddRef();
            *pIterator = (SIZE_T)pRWLockIterator;
            return S_OK;
        }
    }
    if (_dwState == 0) {
        return S_OK;
    }
    pRWLockIterator->m_Capacity = 4;
    pRWLockIterator->m_index = 0;
    pRWLockIterator->m_Owner = new (nothrow) IHostTask*[pRWLockIterator->m_Capacity];
    if (pRWLockIterator->m_Owner == NULL) {
        return E_OUTOFMEMORY;
    }

    HRESULT hr = S_OK;
    
    NewArrayHolder<IHostTask*> OwnerHolder(pRWLockIterator->m_Owner);

    // Take ThreadStore lock and walk over every thread in the process
    Thread *thread = NULL;
    while ((thread = ThreadStore::s_pThreadStore->GetAllThreadList(thread, 
                                                      Thread::TS_Unstarted|Thread::TS_Dead|Thread::TS_Detached, 0))
           != NULL) 
    {
        LockEntry *pLockEntry;
        {
            CrstHolder rwl(&s_RWLockCrst);
            pLockEntry = GetLockEntry(thread);
        }
        if (pLockEntry && pLockEntry->wReaderLevel >= 1) {
            if (pRWLockIterator->m_index == pRWLockIterator->m_Capacity) {
                IHostTask** newArray = new (nothrow) IHostTask*[2*pRWLockIterator->m_Capacity];
                if (newArray == NULL) {
                    hr = E_OUTOFMEMORY;
                    break;
                }
                memcpy (newArray,pRWLockIterator->m_Owner,pRWLockIterator->m_Capacity*sizeof(IHostTask*));
                pRWLockIterator->m_Owner = newArray;
                pRWLockIterator->m_Capacity *= 2;
                OwnerHolder = pRWLockIterator->m_Owner;
            }
            IHostTask *pHostTask = thread->GetHostTaskWithAddRef();
            if (pHostTask)
            {
                pRWLockIterator->m_Owner[pRWLockIterator->m_index++] = pHostTask;
            }
        }
    }
    if (FAILED(hr))
    {
        for (DWORD i = 0; i < pRWLockIterator->m_index; i ++)
        {
            if (pRWLockIterator->m_Owner[i])
            {
                pRWLockIterator->m_Owner[i]->Release();
            }
        }
    }
    if (SUCCEEDED(hr)) {
        IteratorHolder.SuppressRelease();
        OwnerHolder.SuppressRelease();
        pRWLockIterator->m_Capacity = pRWLockIterator->m_index;
        pRWLockIterator->m_index = 0;
        *pIterator = (SIZE_T)pRWLockIterator;
    }
    
    return hr;
}

void CRWLock::GetNextOwner(SIZE_T Iterator, IHostTask **ppOwnerHostTask)
{
    CONTRACTL
    {
        NOTHROW;    
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    *ppOwnerHostTask = NULL;
    if (Iterator) {
        RWLockIterator* tmp = (RWLockIterator*)Iterator;
        if (tmp->m_index < tmp->m_Capacity) {
            *ppOwnerHostTask = tmp->m_Owner[tmp->m_index];
            tmp->m_index ++;
        }
    }
}

void CRWLock::DeleteOwnerIterator(SIZE_T Iterator)
{
    CONTRACTL
    {
        NOTHROW;    
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;


    if (Iterator) {
        RWLockIterator* pIterator = (RWLockIterator*)Iterator;
        while (pIterator->m_index < pIterator->m_Capacity) {
            IHostTask *pHostTask = pIterator->m_Owner[pIterator->m_index];
            if (pHostTask)
            {
                pHostTask->Release();
            }
            pIterator->m_index ++;
        }
        delete[] pIterator->m_Owner;
        delete pIterator;
    }
}
#endif // FEATURE_RWLOCK
