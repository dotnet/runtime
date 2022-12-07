// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: rspriv.inl
//

//
// Inline functions for rspriv.h
//
//*****************************************************************************

#ifndef RSPRIV_INL_
#define RSPRIV_INL_

#include "rspriv.h"

// Get the native pipeline object, which resides on the Win32EventThread.
inline
INativeEventPipeline * CordbWin32EventThread::GetNativePipeline()
{
    return m_pNativePipeline;
}


// True if we're interop-debugging, else false.
// Note, we include this even in Non-interop builds because there are runtime checks throughout the APIs
// that certain operations only succeed/fail in interop-debugging.
inline
bool CordbProcess::IsInteropDebugging()
{
#ifdef FEATURE_INTEROP_DEBUGGING
    return (m_state & PS_WIN32_ATTACHED) != 0;
#else
    return false;
#endif // FEATURE_INTEROP_DEBUGGING
}


//-----------------------------------------------------------------------------
// Get the ShimProcess object.
//
// Returns:
//    ShimProcess object if available; else NULL.
//
// Notes:
//    This shim has V2 emulation logic.
//    If we have no ShimProcess object, then we're in a V3 codepath.
//    @dbgtodo - eventually, remove all emulation and this function.
//-----------------------------------------------------------------------------
inline
ShimProcess * CordbProcess::GetShim()
{
    return m_pShim;
};



//---------------------------------------------------------------------------------------
// Helper to read a structure from the target
//
// Arguments:
//    T - type of structure to read.
//    pRemotePtr - remote pointer into target (src).
//    pLocalBuffer - local buffer to copy into (Dest).
//
// Return Value:
//    Returns S_OK on success, in the event of a short read returns ERROR_PARTIAL_COPY
//
// Notes:
//    This just does a raw Byte copy, but does not do any Marshalling.
//    This fails if any part of the buffer can't be read.
//
//---------------------------------------------------------------------------------------
template<typename T>
HRESULT CordbProcess::SafeReadStruct(CORDB_ADDRESS pRemotePtr, T * pLocalBuffer)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        TargetBuffer tb(pRemotePtr, sizeof(T));
        SafeReadBuffer(tb, (PBYTE) pLocalBuffer);
    }
    EX_CATCH_HRESULT(hr) ;
    return hr;
}

//---------------------------------------------------------------------------------------
// Destructor for RSInitHolder. Will safely neuter and release the object.
template<class T> inline
RSInitHolder<T>::~RSInitHolder()
{
    if (m_pObject != NULL)
    {
        CordbProcess * pProcess = m_pObject->GetProcess();
        RSLockHolder lockHolder(pProcess->GetProcessLock());

        m_pObject->Neuter();

        // Can't explicitly call 'delete' because somebody may have taken a reference.
        m_pObject.Clear();
    }
}

//---------------------------------------------------------------------------------------
// Helper to write a structure to the target
//
// Arguments:
//    T - type of structure to read.
//    pRemotePtr - remote pointer into target (dest).
//    pLocalBuffer - local buffer to write (Src).
//
// Return Value:
//    Returns S_OK on success, in the event of a short write returns ERROR_PARTIAL_COPY
//
// Notes:
//    This just does a raw Byte copy into the Target, but does not do any Marshalling.
//    This fails if any part of the buffer can't be written.
//
//---------------------------------------------------------------------------------------
template<typename T> inline
HRESULT CordbProcess::SafeWriteStruct(CORDB_ADDRESS pRemotePtr, const T* pLocalBuffer)
{
    HRESULT hr= S_OK;
    EX_TRY
    {
        TargetBuffer tb(pRemotePtr, sizeof(T));
        SafeWriteBuffer(tb, (BYTE *) (pLocalBuffer));
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

inline
CordbModule *CordbJITILFrame::GetModule()
{
    return (m_ilCode->GetModule());
}

inline
CordbAppDomain *CordbJITILFrame::GetCurrentAppDomain()
{
    return (m_nativeFrame->GetCurrentAppDomain());
}

//-----------------------------------------------------------------------------
// Called to notify that we must flush DAC
//-----------------------------------------------------------------------------
inline
void CordbProcess::ForceDacFlush()
{
    // We need to take the process lock here because otherwise we could race with the Arrowhead stackwalking
    // APIs.  The Arrowhead stackwalking APIs check the flush counter and refresh all the state if necessary.
    // However, while one thread is refreshing the state of the stackwalker, another thread may come in
    // and force a flush.  That's why we need to take a process lock before we flush.  We need to synchronize
    // with other threads which are using DAC memory.
    RSLockHolder lockHolder(GetProcessLock());

    // For Mac debugging, it is not safe to call into the DAC once code:INativeEventPipeline::TerminateProcess
    // is called.  Also, we must check m_exiting under the process lock.
    if (!m_exiting)
    {
        if (m_pDacPrimitives != NULL)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000, "Flush() - old counter: %d\n", m_flushCounter);
            m_flushCounter++;
            HRESULT hr = S_OK;
            EX_TRY
            {
                m_pDacPrimitives->FlushCache();
            }
            EX_CATCH_HRESULT(hr);
            SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
        }
    }
}


inline
CordbFunction *CordbJITILFrame::GetFunction()
{
    return m_nativeFrame->m_nativeCode->GetFunction();
}

//-----------------------------------------------------------------------------
// Helpers to assert threading semantics.
//-----------------------------------------------------------------------------
inline bool IsWin32EventThread(CordbProcess * p)
{
    _ASSERTE(p!= NULL);
    return p->IsWin32EventThread();
}

inline bool IsRCEventThread(Cordb* p)
{
    _ASSERTE(p!= NULL);
    return (p->m_rcEventThread != NULL) && p->m_rcEventThread->IsRCEventThread();
}



//-----------------------------------------------------------------------------
// StopContinueHolder. Ensure that we're synced during a certain region.
//-----------------------------------------------------------------------------
inline HRESULT StopContinueHolder::Init(CordbProcess * p)
{
    _ASSERTE(p != NULL);
    LOG((LF_CORDB, LL_INFO100000, "Doing RS internal Stop\n"));
    HRESULT hr = p->StopInternal(INFINITE, VMPTR_AppDomain::NullPtr());
    if ((hr == CORDBG_E_PROCESS_TERMINATED) || SUCCEEDED(hr))
    {
        // Better be synced after calling Stop!
        _ASSERTE(p->GetSynchronized());
        m_p = p;
    }

    return hr;
};

inline StopContinueHolder::~StopContinueHolder()
{
    // If Init() failed to call Stop, then don't call continue
    if (m_p == NULL)
        return;

    HRESULT hr;
    LOG((LF_CORDB, LL_INFO100000, "Doing RS internal Continue\n"));
    hr = m_p->ContinueInternal(false);
    SIMPLIFYING_ASSUMPTION(
        (hr == CORDBG_E_PROCESS_TERMINATED) ||
        (hr == CORDBG_E_PROCESS_DETACHED) ||
        (hr == CORDBG_E_OBJECT_NEUTERED) ||
        (hr == E_ACCESSDENIED) || //Sadly in rare cases we leak this error code instead of PROCESS_TERMINATED
                                  //See Dev10 bug 872621
        SUCCEEDED(hr));
}

//-----------------------------------------------------------------------------
// Neutering on the base object
//-----------------------------------------------------------------------------
inline
void CordbCommonBase::Neuter()
{
    LOG((LF_CORDB, LL_EVERYTHING, "Memory: CordbBase object neutered: this=%p, id=%p\n", this, m_id));
    m_fIsNeutered = 1;
}

// Unsafe neuter for an object that's already dead. Only use this if you know exactly what you're doing.
// The point here is that we can mark the object neutered even though we may not hold the stop-go lock.
inline
void CordbCommonBase::UnsafeNeuterDeadObject()
{
    LOG((LF_CORDB, LL_EVERYTHING, "Memory: CordbBase object neutered: this=%p, id=%p\n", this, m_id));
    m_fIsNeutered = 1;
}


//-----------------------------------------------------------------------------
// Reference Counting
//-----------------------------------------------------------------------------
inline
void CordbCommonBase::InternalAddRef()
{
    CONSISTENCY_CHECK_MSGF((m_RefCount & CordbBase_InternalRefCountMask) != (CordbBase_InternalRefCountMax),
        ("Internal AddRef overlow, External Count = %d,\n'%s' @ 0x%p",
        (m_RefCount >> CordbBase_ExternalRefCountShift), this->DbgGetName(), this));

    // Since the internal ref-count is the lower bits, and we know we'll never overflow ;)
    // we can just do an interlocked increment on the whole 32 bits.
#ifdef TRACK_OUTSTANDING_OBJECTS
    MixedRefCountUnsigned Count =
#endif

    InterlockedIncrement64((MixedRefCountSigned*) &m_RefCount);


#ifdef _DEBUG_IMPL

    // For leak detection in debug builds, track all internal references.
    InterlockedIncrement(&Cordb::s_DbgMemTotalOutstandingInternalRefs);
#endif

#ifdef TRACK_OUTSTANDING_OBJECTS
    if ((Count & CordbBase_InternalRefCountMask) != 1)
    {
        return;
    }

    LONG i;

    for (i = 0; i < Cordb::s_DbgMemOutstandingObjectMax; i++)
    {
        if (Cordb::s_DbgMemOutstandingObjects[i] == NULL)
        {
            if (InterlockedCompareExchangeT(&(Cordb::s_DbgMemOutstandingObjects[i]), (LPVOID) this, NULL) == NULL)
            {
                return;
            }
        }
    }

    do
    {
        i = Cordb::s_DbgMemOutstandingObjectMax + 1;
    }
    while ((i < MAX_TRACKED_OUTSTANDING_OBJECTS) &&
           (InterlockedCompareExchange(&Cordb::s_DbgMemOutstandingObjectMax, i, i - 1) != (i - 1)));

    if (i < MAX_TRACKED_OUTSTANDING_OBJECTS)
    {
        Cordb::s_DbgMemOutstandingObjects[i] = this;
    }
#endif

}

// Derived versions of AddRef / Release will call these.
// External AddRef.
inline
ULONG CordbCommonBase::BaseAddRef()
{
    Volatile<MixedRefCountUnsigned> ref;
    MixedRefCountUnsigned refNew;
    ExternalRefCount cExternalCount;

    // Compute what refNew ought to look like; and then If m_RefCount hasn't changed on us
    // (via another thread), then stash the new one in.
    do
    {
        ref = m_RefCount;

        cExternalCount = (ExternalRefCount) (ref >> CordbBase_ExternalRefCountShift);

        if (cExternalCount == CordbBase_InternalRefCountMax)
        {
            CONSISTENCY_CHECK_MSGF(false, ("Overflow in External AddRef. Internal Count =%d,\n'%s' @ 0x%p",
                (ref & CordbBase_InternalRefCountMask), this->DbgGetName(), this));

            // Ignore any AddRefs beyond this... This will screw up Release(), but we're
            // probably already so screwed it wouldn't matter.
            return cExternalCount;
        }

        cExternalCount++;

        refNew = (((MixedRefCountUnsigned)cExternalCount) << CordbBase_ExternalRefCountShift) | (ref & CordbBase_InternalRefCountMask);
    }
    while ((MixedRefCountUnsigned)InterlockedCompareExchange64((MixedRefCountSigned*)&m_RefCount, refNew, ref) != ref);

    return cExternalCount;
}

// Do an AddRef against the External count. This is a semantics issue.
// We use this when an internal component Addrefs out-parameters (which Cordbg will call Release on).
inline
void CordbCommonBase::ExternalAddRef()
{
    // Call on BaseAddRef() to avoid any asserts that prevent stuff from inside the RS from bumping
    // up the external ref count.
    BaseAddRef();
}

inline
void CordbCommonBase::InternalRelease()
{
    CONSISTENCY_CHECK_MSGF((m_RefCount & CordbBase_InternalRefCountMask) != 0,
        ("Internal Release underflow, External Count = %d,\n'%s' @ 0x%p",
        (m_RefCount >> CordbBase_ExternalRefCountShift), this->DbgGetName(), this));

#ifdef _DEBUG_IMPL
    // For leak detection in debug builds, track all internal references.
    InterlockedDecrement(&Cordb::s_DbgMemTotalOutstandingInternalRefs);
#endif



    // The internal count is in the low 16 bits, and we know that we'll never underflow the internal
    // release. ;)
    // Furthermore we know that ExternalRelease  will prevent us from underflowing the external release count.
    // Thus we can just do an simple decrement here, and compare against 0x00000000 (which is the value
    // when both the Internal + External counts are at 0)
    MixedRefCountSigned cRefCount = InterlockedDecrement64((MixedRefCountSigned*) &m_RefCount);

#ifdef TRACK_OUTSTANDING_OBJECTS
    if ((cRefCount & CordbBase_InternalRefCountMask) == 0)
    {
        for (LONG i = 0; i < Cordb::s_DbgMemOutstandingObjectMax; i++)
        {
            if (Cordb::s_DbgMemOutstandingObjects[i] == this)
            {
                Cordb::s_DbgMemOutstandingObjects[i] = NULL;
                break;
            }
        }
    }
#endif


    if (cRefCount == 0x00000000)
    {
        delete this;
    }
}

// Do an external release.
inline
ULONG CordbCommonBase::BaseRelease()
{
    Volatile<MixedRefCountUnsigned> ref;
    MixedRefCountUnsigned refNew;
    ExternalRefCount cExternalCount;

    // Compute what refNew ought to look like; and then If m_RefCount hasn't changed on us
    // (via another thread), then stash the new one in.
    do
    {
        ref = m_RefCount;

        cExternalCount = (ExternalRefCount) (ref >> CordbBase_ExternalRefCountShift);

        if (cExternalCount == 0)
        {
            CONSISTENCY_CHECK_MSGF(false, ("Underflow in External Release. Internal Count = %d\n'%s' @ 0x%p",
                (ref & CordbBase_InternalRefCountMask), this->DbgGetName(), this));

            // Ignore any Releases beyond this... This will screw up Release(), but we're
            // probably already so screwed it wouldn't matter.
            // It's very important that we don't let the release count go negative (both
            // Releases assumes this when deciding whether to delete)
            return 0;
        }

        cExternalCount--;

        refNew = (((MixedRefCountUnsigned) cExternalCount) << CordbBase_ExternalRefCountShift) | (ref & CordbBase_InternalRefCountMask);
    }
    while ((MixedRefCountUnsigned)InterlockedCompareExchange64((MixedRefCountSigned*)&m_RefCount, refNew, ref) != ref);

    // If the external count just dropped to 0, then this object can be neutered.
    if (cExternalCount == 0)
    {
        m_fNeuterAtWill = 1;
    }

    if (refNew == 0)
    {
        delete this;
        return 0;
    }
    return cExternalCount;

}


inline ULONG CordbCommonBase::BaseAddRefEnforceExternal()
{
    // External refs shouldn't be called while in the RS
#ifdef RSCONTRACTS
    DbgRSThread * pThread = DbgRSThread::GetThread();
    CONSISTENCY_CHECK_MSGF(!pThread->IsInRS(),
        ("External addref for pThis=0x%p, name='%s' called from within RS",
            this, this->DbgGetName()
        ));
#endif
    return (BaseAddRef());

}

inline ULONG CordbCommonBase::BaseReleaseEnforceExternal()
{
#ifdef RSCONTRACTS
    DbgRSThread * pThread = DbgRSThread::GetThread();

    CONSISTENCY_CHECK_MSGF(!pThread->IsInRS(),
        ("External release for pThis=0x%p, name='%s' called from within RS",
            this, this->DbgGetName()
        ));
#endif

    return (BaseRelease());
}



//-----------------------------------------------------------------------------
// Locks
//-----------------------------------------------------------------------------

// Base class
#ifdef _DEBUG
inline bool RSLock::HasLock()
{
    CONSISTENCY_CHECK_MSGF(IsInit(), ("RSLock '%s' not inited", m_szTag));
    return m_tidOwner == ::GetCurrentThreadId();
}
#endif

#ifdef _DEBUG
// Ctor+  Dtor are only used for asserts.
inline RSLock::RSLock()
{
    m_eAttr = cLockUninit;
    m_tidOwner = (DWORD)-1;
};

inline RSLock::~RSLock()
{
    // If this lock is still ininitialized, then no body ever deleted the critical section
    // for it and we're leaking.
    CONSISTENCY_CHECK_MSGF(!IsInit(), ("Leaking Critical section for RS Lock '%s'", m_szTag));
}
#endif


// Initialize a lock.
inline void RSLock::Init(const char * szTag, int eAttr, ERSLockLevel level)
{
    CONSISTENCY_CHECK_MSGF(!IsInit(), ("RSLock '%s' already inited", szTag));
#ifdef _DEBUG
    m_szTag = szTag;
    m_eAttr = eAttr;
    m_count = 0;
    m_level = level;

    // Must be either re-entrant xor flat. (not neither; not both)
    _ASSERTE(IsReentrant() ^ ((m_eAttr & cLockFlat) == cLockFlat));
#endif
    _ASSERTE((level >= 0) && (level <= RSLock::LL_MAX));

    _ASSERTE(IsInit());

    InitializeCriticalSection(&m_lock);
}

// Cleanup a lock.
inline void RSLock::Destroy()
{
    CONSISTENCY_CHECK_MSGF(IsInit(), ("RSLock '%s' not inited", m_szTag));
    DeleteCriticalSection(&m_lock);

#ifdef _DEBUG
    m_eAttr = cLockUninit; // No longer initialized.
    _ASSERTE(!IsInit());
#endif
}

inline void RSLock::Lock()
{
    CONSISTENCY_CHECK_MSGF(IsInit(), ("RSLock '%s' not inited", m_szTag));

#ifdef RSCONTRACTS
    DbgRSThread * pThread = DbgRSThread::GetThread();
    pThread->NotifyTakeLock(this);
#endif

    EnterCriticalSection(&m_lock);
#ifdef _DEBUG
    m_tidOwner = ::GetCurrentThreadId();
    m_count++;

    // Either count == 1 or we're re-entrant.
    _ASSERTE((m_count == 1) || (m_eAttr == cLockReentrant));
#endif
}

inline void RSLock::Unlock()
{
    CONSISTENCY_CHECK_MSGF(IsInit(), ("RSLock '%s' not inited", m_szTag));

#ifdef _DEBUG
    _ASSERTE(HasLock());
    m_count--;
    _ASSERTE(m_count >= 0);
    if (m_count == 0)
    {
        m_tidOwner = (DWORD)-1;
    }
#endif

#ifdef RSCONTRACTS
    // NotifyReleaseLock needs to be called before we release the lock.
    // Note that HasLock()==false at this point. NotifyReleaseLock relies on that.
    DbgRSThread * pThread = DbgRSThread::GetThread();
    pThread->NotifyReleaseLock(this);
#endif

    LeaveCriticalSection(&m_lock);
}

template <class T>
inline T* CordbSafeHashTable<T>::GetBase(ULONG_PTR id, BOOL fFab)
{
    return static_cast<T*>(UnsafeGetBase(id, fFab));
}

template <class T>
inline T* CordbSafeHashTable<T>::GetBaseOrThrow(ULONG_PTR id, BOOL fFab)
{
    T* pResult = GetBase(id, fFab);
    if (pResult == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }
    else
    {
        return pResult;
    }
}

// Copy the contents of the hash to an strong-ref array
//
// Arguments:
//    pArray - array to allocate storage and copy to
//
// Assumptions:
//    Caller locks.
//
// Notes:
//    Array takes strong internal references.
//    This can be useful for dancing around locks; eg: If we want to iterate on a hash
//    and do an operation that requires a lock that can't be held when iterating.
//    (Example: Neuter needs Big stop-go lock; Hash is protected by little Process-lock).
//
template <class T>
inline void CordbSafeHashTable<T>::CopyToArray(RSPtrArray<T> * pArray)
{
    // Assumes caller has necessary locks to iterate
    UINT32 count = GetCount();
    pArray->AllocOrThrow(count);


    HASHFIND find;
    UINT32 idx = 0;

    T * pCordbBase = FindFirst(&find);
    while(idx < count)
    {
        pArray->Assign(idx, pCordbBase);
        idx++;
        pCordbBase = FindNext(&find);
    }

    // Assert is at end.
    _ASSERTE(pCordbBase == NULL);
}

// Empty the contents of the hash to an array. Array gets ownersship.
//
// Arguments:
//    pArray - array to allocate and get ownership
//
// Assumptions:
//    Caller locks.
//
// Notes:
//    Hashtable will be empty after this.
template <class T>
inline void CordbSafeHashTable<T>::TransferToArray(RSPtrArray<T> * pArray)
{
    // Assumes caller has necessary locks

    HASHFIND find;
    UINT32 count = GetCount();
    UINT32 idx = 0;

    pArray->AllocOrThrow(count);

    while(idx < count)
    {
        T * pCordbBase = FindFirst(&find);
        _ASSERTE(pCordbBase != NULL);
        pArray->Assign(idx, pCordbBase);

        idx++;
        // We're removing while iterating the collection.
        // But we reset the iteration each time by calling FindFirst.
        RemoveBase((ULONG_PTR)pCordbBase->m_id); // this will call release, adjust GetCount()
    }

    // Assert is at end.
    _ASSERTE(GetCount() == 0);
}

//
// Neuter all elements in the hash table and empty the hash.
//
// Arguments:
//    pLock - lock required to iterate through hash.
//
// Assumptions:
//    Caller ensured it's safe to Neuter.
//    Caller has locked the hash.
//
template <class T>
inline void CordbSafeHashTable<T>::NeuterAndClear(RSLock * pLock)
{
    _ASSERTE(pLock->HasLock());

    HASHFIND find;
    UINT32 count = GetCount();
    UINT32 idx = 0;

    while(idx < count)
    {
        T * pCordbBase = FindFirst(&find);
        _ASSERTE(pCordbBase != NULL);
        pCordbBase->Neuter();
        idx++;

        // We're removing while iterating the collection.
        // But we reset the iteration each time by calling FindFirst.
        RemoveBase((ULONG_PTR)pCordbBase->m_id); // this will call release, adjust GetCount()
    }

    // Assert is at end.
    _ASSERTE(GetCount() == 0);
}


#endif  // RSPRIV_INL_
