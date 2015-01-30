//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    shmobject.hpp

Abstract:
    Shared memory based object



--*/

#include "shmobject.hpp"
#include "pal/malloc.hpp"
#include "pal/cs.hpp"
#include "pal/dbgmsg.h"

#include <stddef.h>

SET_DEFAULT_DEBUG_CHANNEL(PAL);

using namespace CorUnix;

/*++
Function:
  CSharedMemoryObject::Initialize

  Performs possibly-failing initialization for a newly-constructed
  object

Parameters:
  pthr -- thread data for calling thread
  poa -- the object attributes (e.g., name) for the object
--*/

PAL_ERROR
CSharedMemoryObject::Initialize(
    CPalThread *pthr,
    CObjectAttributes *poa
    )
{
    PAL_ERROR palError = NO_ERROR;
    SHMObjData *psmod = NULL;

    _ASSERTE(NULL != pthr); 
    _ASSERTE(NULL != poa);

    ENTRY("CSharedMemoryObject::Initialize"
        "(this = %p, pthr = %p, poa = %p)\n",
        this,
        pthr,
        poa
        );

    palError = CPalObjectBase::Initialize(pthr, poa);
    if (NO_ERROR != palError)
    {
        goto InitializeExit;
    }

    //
    // If this is a named object it needs to go into the shared domain;
    // otherwise it remains local
    //

    if (0 != m_oa.sObjectName.GetStringLength())
    {   
        m_ObjectDomain = SharedObject;

        palError = AllocateSharedDataItems(&m_shmod, &psmod);
        if (NO_ERROR != palError || NULL == psmod)
        {
            goto InitializeExit;
        }
    }

    if (0 != m_pot->GetSharedDataSize())
    {       
        if (SharedObject == m_ObjectDomain)
        {
            //
            // Map the shared data into our address space
            //
            if (NULL == psmod)
            {
                ASSERT("psmod should not be NULL");
                palError = ERROR_INTERNAL_ERROR;
                goto InitializeExit;
            } 

            m_pvSharedData = SHMPTR_TO_TYPED_PTR(VOID, psmod->shmObjSharedData);
            if (NULL == m_pvSharedData)
            {
                ASSERT("Unable to map shared data area\n");
                palError = ERROR_INTERNAL_ERROR;
                goto InitializeExit;
            }
        }
        else 
        {
            //
            // Initialize the local shared data lock.
            //

            palError = m_sdlSharedData.Initialize();
            if (NO_ERROR != palError)
            {
                ERROR("Failure initializing m_sdlSharedData\n");
                goto InitializeExit;
            } 
        
            //
            // Allocate local memory to hold the shared data
            //

            m_pvSharedData = InternalMalloc(pthr, m_pot->GetSharedDataSize());
            if (NULL == m_pvSharedData)
            {
                ERROR("Failure allocating m_pvSharedData (local copy)\n");
                palError = ERROR_OUTOFMEMORY;
                goto InitializeExit;
            }
        }

        ZeroMemory(m_pvSharedData, m_pot->GetSharedDataSize());
    }


InitializeExit:

    LOGEXIT("CSharedMemoryObject::Initalize returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObject::InitializeFromExistingSharedData

  Performs possibly-failing initialization for a newly-constructed
  object that is to represent an existing object (i.e., importing
  a shared object into this process)

  The shared memory lock must be held when calling this method

Parameters:
  pthr -- thread data for calling thread
  poa -- the object attributes for the object
--*/

PAL_ERROR
CSharedMemoryObject::InitializeFromExistingSharedData(
    CPalThread *pthr,
    CObjectAttributes *poa
    )
{
    PAL_ERROR palError = NO_ERROR;
    SHMObjData *psmod = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != poa);

    ENTRY("CSharedMemoryObject::InitializeFromExistingSharedData"
        "(this = %p, pthr = %p, poa = %p)\n",
        this,
        pthr,
        poa
        );

    //
    // This object is obviously shared...
    //

    m_ObjectDomain = SharedObject;

    _ASSERTE(SHMNULL != m_shmod);

    psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, m_shmod);
    if (NULL == psmod)
    {
        ASSERT("Unable to map shared object data\n");
        palError = ERROR_INTERNAL_ERROR;
        goto InitializeFromExistingSharedDataExit;
    }

    //
    // When we're being called on the duplicate handle path the passed
    // in object attributes likely won't have an object name in it.
    // If there is an object name in the shared data place that in the
    // object attributs so that the constructed object has a local copy
    // of the name
    //

    if (0 == poa->sObjectName.GetStringLength()
        && 0 != psmod->dwNameLength)
    {
        WCHAR *wsz;

        wsz = SHMPTR_TO_TYPED_PTR(WCHAR, psmod->shmObjName);
        if (NULL != wsz)
        {
            poa->sObjectName.SetStringWithLength(wsz, psmod->dwNameLength);
        }
        else
        {
            ASSERT("Unable to map object name\n");
            palError = ERROR_INTERNAL_ERROR;
            goto InitializeFromExistingSharedDataExit;
        }
    }
#if _DEBUG
    else if (0 != psmod->dwNameLength)
    {
        WCHAR *wsz;

        //
        // Verify that the names are consistent
        //

        wsz = SHMPTR_TO_TYPED_PTR(WCHAR, psmod->shmObjName);
        _ASSERTE(NULL != wsz);
        _ASSERTE(0 == PAL_wcscmp(wsz, poa->sObjectName.GetString()));
    }
#endif // debug

    palError = CPalObjectBase::Initialize(pthr, poa);
    if (NO_ERROR != palError)
    {
        goto InitializeFromExistingSharedDataExit;
    }

    if (SHMNULL != psmod->shmObjImmutableData)
    {
        VOID *pv = SHMPTR_TO_TYPED_PTR(VOID, psmod->shmObjImmutableData);
        if (NULL != pv)
        {
            memcpy(m_pvImmutableData, pv, m_pot->GetImmutableDataSize());
        }
        else
        {
            ASSERT("Unable to map object immutable data\n");
            palError = ERROR_INTERNAL_ERROR;
            goto InitializeFromExistingSharedDataExit;
        }
    }

    if (SHMNULL != psmod->shmObjSharedData)
    {
        m_pvSharedData = SHMPTR_TO_TYPED_PTR(VOID, psmod->shmObjSharedData);
        if (NULL == m_pvSharedData)
        {
            ASSERT("Unable to map object shared data\n");
            palError = ERROR_INTERNAL_ERROR;
            goto InitializeFromExistingSharedDataExit;
        }
    }

    if (NULL != m_pot->GetObjectInitRoutine())
    {
        palError = (*m_pot->GetObjectInitRoutine())(
            pthr,
            m_pot,
            m_pvImmutableData,
            m_pvSharedData,
            m_pvLocalData
            );
    }

InitializeFromExistingSharedDataExit:

    LOGEXIT("CSharedMemoryObject::InitalizeFromExistingSharedData returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObject::AllocatedSharedDataItems

  Allocates and initialiazes the shared memory structures necessary to make an
  object available to other processes

Parameters:
  pshmObjData -- on success, receives the shared memory pointer for the
    shared memory object data
  ppsmod -- on success, receives the locally-mapped pointer for the shared
    memory object data
--*/

PAL_ERROR
CSharedMemoryObject::AllocateSharedDataItems(
    SHMPTR *pshmObjData,
    SHMObjData **ppsmod
    )
{
    PAL_ERROR palError = NO_ERROR;
    SHMPTR shmod = SHMNULL;
    SHMObjData *psmod = NULL;

    _ASSERTE(NULL != pshmObjData);
    _ASSERTE(NULL != ppsmod);

    ENTRY("CSharedMemoryObject::AllocateSharedDataItems"
        "(this = %p, pshmObjData = %p, ppsmod = %p)\n",
        this,
        pshmObjData,
        ppsmod
        );
        
    //
    // We're about to make a number of shared memory allocations,
    // so grab the lock for the entirety of the routine.
    //

    SHMLock();

    shmod = SHMalloc(sizeof(SHMObjData));
    if (SHMNULL == shmod)
    {
        ERROR("Unable to allocate m_shmod for new object\n");
        palError = ERROR_OUTOFMEMORY;
        goto AllocateSharedDataItemsExit;
    }

    psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, shmod);
    _ASSERTE(NULL != psmod);

    ZeroMemory(psmod, sizeof(*psmod));

    psmod->eTypeId = m_pot->GetId();
    psmod->lProcessRefCount = 1;

    if (0 != m_oa.sObjectName.GetStringLength())
    {
        psmod->dwNameLength = m_oa.sObjectName.GetStringLength();
        psmod->shmObjName = SHMWStrDup(m_oa.sObjectName.GetString());
        if (SHMNULL == psmod->shmObjName)
        {
            ERROR("Unable to allocate psmod->shmObjName for new object\n");
            palError = ERROR_OUTOFMEMORY;
            goto AllocateSharedDataItemsExit;
        }
    }

    if (0 != m_pot->GetImmutableDataSize())
    {
        //
        // The shared copy of the object's immutable data will be initialized
        // by CSharedMemoryObjectManager::RegisterObject or PromoteSharedData
        //
        
        psmod->shmObjImmutableData = SHMalloc(m_pot->GetImmutableDataSize());
        if (SHMNULL == psmod->shmObjImmutableData)
        {
            ERROR("Unable to allocate psmod->shmObjImmutableData for new object\n");
            palError = ERROR_OUTOFMEMORY;
            goto AllocateSharedDataItemsExit;
        }
    }

    if (0 != m_pot->GetSharedDataSize())
    {
        psmod->shmObjSharedData = SHMalloc(m_pot->GetSharedDataSize());
        if (SHMNULL == psmod->shmObjSharedData)
        {
            ERROR("Unable to allocate psmod->shmObjSharedData for new object\n");
            palError = ERROR_OUTOFMEMORY;
            goto AllocateSharedDataItemsExit;
        }
    }

    *pshmObjData = shmod;
    *ppsmod = psmod;

AllocateSharedDataItemsExit:

    if (NO_ERROR != palError && SHMNULL != shmod)
    {
        FreeSharedDataAreas(shmod);
    }

    SHMRelease();

    LOGEXIT("CSharedMemoryObject::AllocateSharedDataItems returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObject::FreeSharedDataItems

  Frees the shared memory structures referenced by the provided shared
  memory pointer

Parameters:
  shmObjData -- shared memory pointer to the structures to free
--*/

// static
void
CSharedMemoryObject::FreeSharedDataAreas(
    SHMPTR shmObjData
    )
{
    SHMObjData *psmod;

    _ASSERTE(SHMNULL != shmObjData);

    ENTRY("CSharedMemoryObject::FreeSharedDataAreas"
        "(shmObjData = %p)\n",
        shmObjData
        );

    SHMLock();
        
    psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, shmObjData);
    _ASSERTE(NULL != psmod);
    
    if (SHMNULL != psmod->shmObjImmutableData)
    {
        SHMfree(psmod->shmObjImmutableData);
    }

    if (SHMNULL != psmod->shmObjSharedData)
    {
        SHMfree(psmod->shmObjSharedData);
    }

    if (SHMNULL != psmod->shmObjName)
    {
        SHMfree(psmod->shmObjName);
    }
    
    SHMfree(shmObjData);

    SHMRelease();

    LOGEXIT("CSharedMemoryObject::FreeSharedDataAreas\n");
}

/*++
Function:
  CSharedMemoryObject::PromoteShjaredData

  Copies the object's state into the passed-in shared data structures

Parameters:
  shmObjData -- shared memory pointer for the shared memory object data
  psmod -- locally-mapped pointer for the shared memory object data
--*/

void
CSharedMemoryObject::PromoteSharedData(
    SHMPTR shmObjData,
    SHMObjData *psmod
    )
{
    _ASSERTE(SHMNULL != shmObjData);
    _ASSERTE(NULL != psmod);
    
    ENTRY("CSharedMemoryObject::PromoteSharedData"
        "(this = %p, shmObjData = %p, psmod = %p)\n",
        this, 
        shmObjData,
        psmod);
    
    //
    // psmod has been zero-inited, so we don't need to worry about
    // shmPrevObj, shmNextObj, fAddedToList, shmObjName, dwNameLength,
    // or pvSynchData
    //
    
    psmod->lProcessRefCount = 1;
    psmod->eTypeId = m_pot->GetId();
    
    if (0 != m_pot->GetImmutableDataSize())
    {
        void *pvImmutableData;

        pvImmutableData = SHMPTR_TO_TYPED_PTR(void, psmod->shmObjImmutableData);
        _ASSERTE(NULL != pvImmutableData);

        CopyMemory(
            pvImmutableData,
            m_pvImmutableData,
            m_pot->GetImmutableDataSize()
            );
    }

    if (0 != m_pot->GetSharedDataSize())
    {
        CPalThread *pthr = InternalGetCurrentThread();
        void *pvSharedData;

        pvSharedData = SHMPTR_TO_TYPED_PTR(void, psmod->shmObjSharedData);
        _ASSERTE(NULL != pvSharedData);

        CopyMemory(
            pvSharedData,
            m_pvSharedData,
            m_pot->GetSharedDataSize()
            );
        
        InternalFree(pthr, m_pvSharedData);
        m_pvSharedData = pvSharedData;
    }

    m_shmod = shmObjData;

    LOGEXIT("CSharedMemoryObject::PromoteSharedData\n");
}

/*++
Function:
  CSharedMemoryObject::EnsureObjectIsShared

  If this object is not yet in the shared domain allocate the necessary
  shared memory structures for it and copy the object's data into those
  structures

Parameters:
  pthr -- thread data for the calling thread
--*/

PAL_ERROR
CSharedMemoryObject::EnsureObjectIsShared(
    CPalThread *pthr
    )
{
    PAL_ERROR palError = NO_ERROR;
    IDataLock *pDataLock = NULL;
    SHMPTR shmObjData;
    SHMObjData *psmod;

    _ASSERTE(NULL != pthr);

    ENTRY("CSharedMemoryObject::EnsureObjectIsShared"
        "(this = %p, pthr = %p)\n",
        this,
        pthr
        );

    //
    // Grab the shared memory lock and check if the object is already
    // shared
    //
    
    SHMLock();

    if (SharedObject == m_ObjectDomain)
    {
        goto EnsureObjectIsSharedExit;
    }

    //
    // Grab the local shared data lock, if necessary
    //
    
    if (0 != m_pot->GetSharedDataSize())
    {
        m_sdlSharedData.AcquireLock(pthr, &pDataLock);
    }

    //
    // Allocate the necessary shared data areas
    //

    palError = AllocateSharedDataItems(&shmObjData, &psmod);
    if (NO_ERROR != palError)
    {
        goto EnsureObjectIsSharedExit;
    }

    //
    // Promote the object's data and set the domain to shared
    //

    PromoteSharedData(shmObjData, psmod);
    m_ObjectDomain = SharedObject;

EnsureObjectIsSharedExit:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pthr, TRUE);
    }

    SHMRelease();

    LOGEXIT("CSharedMemoryObject::EnsureObjectIsShared returns %d\n", palError);

    return palError;    
}

/*++
Function:
  CSharedMemoryObject::CleanupForProcessShutdown

  Cleanup routine called by the object manager when shutting down

Parameters:
  pthr -- thread data for the calling thread
--*/

void
CSharedMemoryObject::CleanupForProcessShutdown(
    CPalThread *pthr
    )
{
    bool fCleanupSharedState;

    _ASSERTE(NULL != pthr);
    
    ENTRY("CSharedMemoryObject::CleanupForProcessShutdown"
        "(this = %p, pthr = %p)\n",
        this,
        pthr
        );

    fCleanupSharedState = DereferenceSharedData();

    if (NULL != m_pot->GetObjectCleanupRoutine())
    {
        (*m_pot->GetObjectCleanupRoutine())(
            pthr,
            static_cast<IPalObject*>(this),
            TRUE,
            fCleanupSharedState
            );
    }

    //
    // We need to do two things with the calling thread data here:
    // 1) store it in m_pthrCleanup so it is available to the destructors
    // 2) Add a reference to it before starting any cleanup, and release
    //    that reference afterwords.
    //
    // Step 2 is necessary when we're cleaning up the thread object that
    // represents the calling thread -- it ensures that the thread data
    // is available throughout the entire cleanup process.
    //

    m_pthrCleanup = pthr;
    pthr->AddThreadReference();
    
    InternalDelete(pthr, this);

    pthr->ReleaseThreadReference();

    LOGEXIT("CSharedMemoryObject::CleanupForProcessShutdown\n");
}

/*++
Function:
  CSharedMemoryObject::AcquiteObjectDestructionLock

  Acquires the lock that must be held when decrementing the object's
  reference count (and, if the count drops to 0, while removing the
  object from the object manager's lists).

Parameters:
  pthr -- thread data for the calling thread
--*/

void
CSharedMemoryObject::AcquireObjectDestructionLock(
    CPalThread *pthr
    )
{
    _ASSERTE(NULL != pthr);
    
    ENTRY("CSharedMemoryObject::AcquireObjectDestructionLock"
        "(this = %p, pthr = $p)\n",
        this,
        pthr
        );
    
    InternalEnterCriticalSection(pthr, m_pcsObjListLock);

    LOGEXIT("CSharedMemoryObject::AcquireObjectDestructionLock\n");
}

/*++
Function:
  CSharedMemoryObject::ReleaseObjectDestructionLock

  Releases the lock acquired by AcquireObjectDestructionLock

Parameters:
  pthr -- thread data for the calling thread
  fDestructionPending -- if TRUE, the reference count for this
    object has dropped to 0; the object will be destroyed after
    this routine returns
--*/

bool
CSharedMemoryObject::ReleaseObjectDestructionLock(
    CPalThread *pthr,
    bool fDestructionPending
    )
{
    bool fCleanupSharedState = FALSE;

    _ASSERTE(NULL != pthr);

    ENTRY("CSharedMemoryObject::ReleaseObjectDestructionLock"
        "(this = %p, pthr = %p, fDestructionPending = %d\n",
        this,
        pthr,
        fDestructionPending
        );
    
    if (fDestructionPending)
    {
        RemoveEntryList(&m_le);
        fCleanupSharedState = DereferenceSharedData();
    }

    InternalLeaveCriticalSection(pthr, m_pcsObjListLock);

    LOGEXIT("CSharedMemoryObject::ReleaseObjectDestructionLock returns %d\n",
        fCleanupSharedState
        );

    return fCleanupSharedState;
}

/*++
Function:
  CSharedMemoryObject::DereferenceSharedData

  Called to decrement the global refcount (i.e., the count of
  the number of processes that have reference to the object) when
  the local reference to the object is being destroyed.

Return value:
  Returns TRUE if this process needs to clean up the object's shared
  data (i.e., the global refcount has dropped to 0, or the object
  is in the local domain)
--*/

bool
CSharedMemoryObject::DereferenceSharedData()
{
    LONG fSharedDataAlreadDereferenced;

    ENTRY("CSharedMemoryObject::DereferenceSharedData(this = %p)\n", this);

    fSharedDataAlreadDereferenced = InterlockedExchange(
        &m_fSharedDataDereferenced,
        TRUE
        );

    if (!fSharedDataAlreadDereferenced)
    {   
        if (SHMNULL != m_shmod)
        {
            SHMObjData *psmod;
            
            SHMLock();

            psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, m_shmod);
            _ASSERTE(NULL != psmod);
            
            psmod->lProcessRefCount -= 1;
            if (0 == psmod->lProcessRefCount)
            {
                //
                // No other process is using this object, so remove
                // it from the shared memory named object list (if it
                // had been added to it). The final cleanup will happen
                // in the object's destructor
                //

                m_fDeleteSharedData = TRUE;

                if (psmod->fAddedToList)
                {
                    //
                    // This object better have a name...
                    //

                    _ASSERTE(0 != psmod->dwNameLength);

                    if (SHMNULL != psmod->shmPrevObj)
                    {
                        SHMObjData *psmodPrevious = SHMPTR_TO_TYPED_PTR(SHMObjData, psmod->shmPrevObj);
                        _ASSERTE(NULL != psmodPrevious);

                        psmodPrevious->shmNextObj = psmod->shmNextObj;
                    }
                    else
                    {
                        //
                        // This object is the head of the shared memory named object
                        // list -- reset that pointer now
                        //

                        if (!SHMSetInfo(SIID_NAMED_OBJECTS, psmod->shmNextObj))
                        {
                            ASSERT("Failed to set shared named object list head");
                        }
                    }

                    if (SHMNULL != psmod->shmNextObj)
                    {
                        SHMObjData *psmodNext = SHMPTR_TO_TYPED_PTR(SHMObjData, psmod->shmNextObj);
                        _ASSERTE(NULL != psmodNext);
                        
                        psmodNext->shmPrevObj = psmod->shmPrevObj;
                    }
                }
#if _DEBUG                
                else
                {
                    _ASSERTE(SHMNULL == psmod->shmPrevObj);
                    _ASSERTE(SHMNULL == psmod->shmNextObj);
                }
#endif                
            }

            SHMRelease();
        }
        else if (ProcessLocalObject == m_ObjectDomain)
        {
            //
            // If the object is local the shared data needs to be
            // deleted by definition
            //
            
            m_fDeleteSharedData = TRUE;
        }
    }
    else
    {
        ASSERT("Multiple calls to DereferenceSharedData\n");
    }

    LOGEXIT("CSharedMemoryObject::DereferenceSharedData returns %d\n",
        m_fDeleteSharedData
        );

    return m_fDeleteSharedData;
}

/*++
Function:
  CSharedMemoryObject::~CSharedMemoryObject

  Destructor; should only be called from ReleaseReference
--*/

CSharedMemoryObject::~CSharedMemoryObject()
{
    ENTRY("CSharedMemoryObject::~CSharedMemoryObject(this = %p)\n", this);
    
    if (!m_fSharedDataDereferenced)
    {
        ASSERT("DereferenceSharedData not called before object destructor -- delete called directly?\n");
        DereferenceSharedData();
    }

    if (NULL != m_pvSharedData && ProcessLocalObject == m_ObjectDomain)
    {
        InternalFree(m_pthrCleanup, m_pvSharedData);
    }
    else if (SHMNULL != m_shmod && m_fDeleteSharedData)
    {
        FreeSharedDataAreas(m_shmod);        
    }

    LOGEXIT("CSharedMemoryObject::~CSharedMemoryObject\n");
}

//
// C++ standard, 18.1.5 - offsetof requires a POD (plain old data) struct or
// union. Since offsetof is a macro, gcc doesn't actually check for improper
// use of offsetof, it keys off of the -> from NULL (which is also invalid for
// non-POD types by 18.1.5)
//
// As we have numerous examples of this behavior in our codebase,
// making an offsetof which doesn't use 0.
//
// PAL_safe_offsetof is a version of offsetof that protects against an
// overridden operator&
//

#define PAL_safe_offsetof(s,m) ((size_t)((ptrdiff_t)&(char&)(((s *)64)->m))-64)

/*++
Function:
  CSharedMemoryObject::GetObjectFromListLink

  Given a list link returns the object that contains it. Since m_le is
  protected the caller cannot perform this computation directly

Parameters:
  ple -- the list entry to obtain the object for
--*/

// static
CSharedMemoryObject*
CSharedMemoryObject::GetObjectFromListLink(PLIST_ENTRY ple)
{
    CSharedMemoryObject *pshmo;

    _ASSERTE(NULL != ple);

    ENTRY("CSharedMemoryObject::GetObjectFromListLink(ple = %p)\n", ple);
    
    //
    // Ideally we'd use CONTAINING_RECORD here, but it uses offsetof (see above
    // comment
    //
    
    pshmo = reinterpret_cast<CSharedMemoryObject*>(
        reinterpret_cast<size_t>(ple) - PAL_safe_offsetof(CSharedMemoryObject, m_le)
        );

    _ASSERTE(ple == &pshmo->m_le);

    LOGEXIT("CSharedMemoryObject::GetObjectFromListLink returns %p\n", pshmo);

    return pshmo;
}

/*++
Function:
  CSharedMemoryObject::GetSharedData

  Provides the caller access to the object's shared data (if any)

Parameters:
  pthr -- thread data for calling thread
  eLockRequest -- specifies if the caller desires a read lock or a
    write lock on the data (currently ignored)
  ppDataLock -- on success, receives a pointer to the data lock instance
    for the shared data
  ppvProcssSharedData -- on success, receives a pointer to the shared data
--*/

PAL_ERROR
CSharedMemoryObject::GetSharedData(
    CPalThread *pthr,
    LockType eLockRequest,
    IDataLock **ppDataLock,             // OUT
    void **ppvSharedData                // OUT
    )
{
    IDataLock *pDataLock;

    _ASSERTE(NULL != pthr);
    _ASSERTE(ReadLock == eLockRequest || WriteLock == eLockRequest);
    _ASSERTE(NULL != ppDataLock);
    _ASSERTE(NULL != ppvSharedData);

    ENTRY("CSharedMemoryObject::GetSharedData"
        "(this = %p, pthr = %p, eLockRequest = %d, ppDataLock = %p,"
        " ppvSharedData = %p)\n",
        this,
        pthr,
        eLockRequest,
        ppDataLock,
        ppvSharedData
        );

    _ASSERTE(0 < m_pot->GetSharedDataSize());
    
    if (ProcessLocalObject == m_ObjectDomain)
    {
        //
        // We need to grab the local shared data lock and re-check
        // the object's domain, as there's a chance the object might
        // have been promoted after we made the above check but before
        // we grabbed the lock
        //

        m_sdlSharedData.AcquireLock(pthr, &pDataLock);

        if (SharedObject == m_ObjectDomain)
        {
            pDataLock->ReleaseLock(pthr, FALSE);
            m_ssmlSharedData.AcquireLock(pthr, &pDataLock);
        }
    }
    else
    {
        //
        // A shared object can never transition back to local,
        // so there's no need to recheck the domain on this path
        //
        
        m_ssmlSharedData.AcquireLock(pthr, &pDataLock);
    }

    *ppDataLock = pDataLock;
    *ppvSharedData = m_pvSharedData;

    LOGEXIT("CSharedMemoryObject::GetSharedData returns %d\n", NO_ERROR);
    
    return NO_ERROR;
}

/*++
Function:
  CSharedMemoryObject::GetSynchStateController

  Obtain a synchronization state controller for this object. Should
  never be called.

Parameters:
  pthr -- thread data for calling thread
  ppStateController -- on success, receives a pointer to the state controller
    instance
--*/

PAL_ERROR
CSharedMemoryObject::GetSynchStateController(
    CPalThread *pthr,
    ISynchStateController **ppStateController    // OUT
    )
{
    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != ppStateController);
    
    //
    // This is not a waitable object!
    //

    ASSERT("Attempt to obtain a synch state controller on a non-waitable object\n");
    return ERROR_INVALID_HANDLE;
}

/*++
Function:
  CSharedMemoryObject::GetSynchWaitController

  Obtain a synchronization wait controller for this object. Should
  never be called.

Parameters:
  pthr -- thread data for calling thread
  ppWaitController -- on success, receives a pointer to the wait controller
    instance
--*/

PAL_ERROR
CSharedMemoryObject::GetSynchWaitController(
    CPalThread *pthr,
    ISynchWaitController **ppWaitController    // OUT
    )
{
    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != ppWaitController);
    
    //
    // This is not a waitable object!!!
    //

    ASSERT("Attempt to obtain a synch wait controller on a non-waitable object\n");
    return ERROR_INVALID_HANDLE;
}

/*++
Function:
  CSharedMemoryObject::GetObjectDomain

  Returns the object's domain (local or shared)

--*/

ObjectDomain
CSharedMemoryObject::GetObjectDomain(
    void
    )
{
    TRACE("CSharedMemoryObject::GetObjectDomain(this = %p)\n", this);
    LOGEXIT("CSharedMemoryObject::GetObjectDomain returns %d\n", m_ObjectDomain);
    
    return m_ObjectDomain;
}

/*++
Function:
  CSharedMemoryObject::GetObjectSynchData

  Obtain the synchronization data for this object. Should
  never be called.

Parameters:
  ppvSynchData -- on success, receives a pointer to the object's synch data
--*/

PAL_ERROR
CSharedMemoryObject::GetObjectSynchData(
    VOID **ppvSynchData             // OUT
    )
{
    _ASSERTE(NULL != ppvSynchData);
    
    //
    // This is not a waitable object!!!
    //

    ASSERT("Attempt to obtain a synch data for a non-waitable object\n");
    return ERROR_INVALID_HANDLE;
}

/*++
Function:
  CSharedMemoryWaitableObject::Initialize

  Performs possibly-failing initialization for a newly-constructed
  object

Parameters:
  pthr -- thread data for calling thread
  poa -- the object attributes (e.g., name) for the object
--*/

PAL_ERROR
CSharedMemoryWaitableObject::Initialize(
    CPalThread *pthr,
    CObjectAttributes *poa
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != poa);

    ENTRY("CSharedMemoryWaitableObject::Initialize"
        "(this = %p, pthr = %p, poa = %p)\n",
        this,
        pthr,
        poa
        );

    palError = CSharedMemoryObject::Initialize(pthr, poa);
    if (NO_ERROR != palError)
    {
        goto InitializeExit;
    }

    //
    // Sanity check the passed in object type
    //

    _ASSERTE(CObjectType::WaitableObject == m_pot->GetSynchronizationSupport());

    palError = g_pSynchronizationManager->AllocateObjectSynchData(
        m_pot,
        m_ObjectDomain,
        &m_pvSynchData
        );

    if (NO_ERROR == palError && SharedObject == m_ObjectDomain)
    {        
        SHMObjData *pshmod = SHMPTR_TO_TYPED_PTR(SHMObjData, m_shmod);
        _ASSERTE(NULL != pshmod);

        pshmod->pvSynchData = m_pvSynchData;
    }

InitializeExit:

    LOGEXIT("CSharedMemoryWaitableObject::Initialize returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryWaitableObject::EnsureObjectIsShared

  If this object is not yet in the shared domain allocate the necessary
  shared memory structures for it and copy the object's data into those
  structures

Parameters:
  pthr -- thread data for the calling thread
--*/

PAL_ERROR
CSharedMemoryWaitableObject::EnsureObjectIsShared(
    CPalThread *pthr
    )
{
    PAL_ERROR palError = NO_ERROR;
    IDataLock *pDataLock = NULL;
    SHMPTR shmObjData = SHMNULL;
    SHMObjData *psmod;
    VOID *pvSharedSynchData;

    _ASSERTE(NULL != pthr);

    ENTRY("CSharedMemoryWaitableObject::EnsureObjectIsShared"
        "(this = %p, pthr = %p)\n",
        this,
        pthr
        );
    
    //
    // First, grab the process synchronization lock and check
    // if the object is already shared
    //

    g_pSynchronizationManager->AcquireProcessLock(pthr);

    if (SharedObject == m_ObjectDomain)
    {
        goto EnsureObjectIsSharedExitNoSHMLockRelease;
    }

    //
    // Grab the necessary locks
    //

    SHMLock();

    if (0 != m_pot->GetSharedDataSize())
    {
        m_sdlSharedData.AcquireLock(pthr, &pDataLock);
    }

    //
    // Allocate the necessary shared data areas
    //

    palError = AllocateSharedDataItems(&shmObjData, &psmod);
    if (NO_ERROR != palError)
    {
        goto EnsureObjectIsSharedExit;
    }

    //
    // Promote the object's synchronization data
    //

    palError = g_pSynchronizationManager->PromoteObjectSynchData(
        pthr,
        m_pvSynchData,
        &pvSharedSynchData
        );

    if (NO_ERROR != palError)
    {
        goto EnsureObjectIsSharedExit;
    }

    m_pvSynchData = pvSharedSynchData;
    psmod->pvSynchData = pvSharedSynchData;

    //
    // Promote the object's data and set the domain to shared
    //

    PromoteSharedData(shmObjData, psmod);
    m_ObjectDomain = SharedObject;

EnsureObjectIsSharedExit:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pthr, TRUE);
    }

    SHMRelease();

EnsureObjectIsSharedExitNoSHMLockRelease:

    g_pSynchronizationManager->ReleaseProcessLock(pthr);

    if (NO_ERROR != palError && SHMNULL != shmObjData)
    {
        //
        // Since shmObjdData is local to this function there's no
        // need to continue to hold the promotion locks when
        // freeing the allocated data on error
        //
        
        FreeSharedDataAreas(shmObjData);
    }

    LOGEXIT("CSharedMemoryWaitableObject::EnsureObjectIsShared returns %d\n",
        palError
        );

    return palError;
}

/*++
Function:
  CSharedMemoryWaitableObject::~CSharedMemoryWaitableObject

  Destructor; should only be called from ReleaseReference
--*/

CSharedMemoryWaitableObject::~CSharedMemoryWaitableObject()
{
    ENTRY("CSharedMemoryWaitableObject::~CSharedMemoryWaitableObject"
        "(this = %p)\n",
        this
        );
    
    if (!m_fSharedDataDereferenced)
    {
        ASSERT("DereferenceSharedData not called before object destructor -- delete called directly?\n");
        DereferenceSharedData();
    }
    
    if (NULL != m_pvSynchData && m_fDeleteSharedData)
    {
        g_pSynchronizationManager->FreeObjectSynchData(
            m_pot,
            m_ObjectDomain,
            m_pvSynchData
            );
    }

    LOGEXIT("CSharedMemoryWaitableObject::~CSharedMemoryWaitableObject\n");
}

/*++
Function:
  CSharedMemoryWaitableObject::GetSynchStateController

  Obtain a synchronization state controller for this object.

Parameters:
  pthr -- thread data for calling thread
  ppStateController -- on success, receives a pointer to the state controller
    instance
--*/

PAL_ERROR
CSharedMemoryWaitableObject::GetSynchStateController(
    CPalThread *pthr,                // IN, OPTIONAL
    ISynchStateController **ppStateController    // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != ppStateController);

    ENTRY("CSharedMemoryWaitableObject::GetSynchStateController"
        "(this = %p, pthr = %p, ppStateController = %p",
        this,
        pthr,
        ppStateController
        );

    //
    // We need to grab the local synch lock before creating the controller
    // (otherwise we could get promoted after passing in our parameters)
    //

    g_pSynchronizationManager->AcquireProcessLock(pthr);
    
    palError = g_pSynchronizationManager->CreateSynchStateController(
        pthr,
        m_pot,
        m_pvSynchData,
        m_ObjectDomain,
        ppStateController
        );

    g_pSynchronizationManager->ReleaseProcessLock(pthr);

    LOGEXIT("CSharedMemoryWaitableObject::GetSynchStateController returns %d\n",
        palError
        );

    return palError;
}

/*++
Function:
  CSharedMemoryWaitableObject::GetSynchWaitController

  Obtain a synchronization wait controller for this object.

Parameters:
  pthr -- thread data for calling thread
  ppWaitController -- on success, receives a pointer to the wait controller
    instance
--*/

PAL_ERROR
CSharedMemoryWaitableObject::GetSynchWaitController(
    CPalThread *pthr,                // IN, OPTIONAL
    ISynchWaitController **ppWaitController    // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != ppWaitController);

    ENTRY("CSharedMemoryWaitableObject::GetSynchWaitController"
        "(this = %p, pthr = %p, ppWaitController = %p",
        this,
        pthr,
        ppWaitController
        );

    //
    // We need to grab the local synch lock before creating the controller
    // (otherwise we could get promoted after passing in our parameters)
    //

    g_pSynchronizationManager->AcquireProcessLock(pthr);

    palError = g_pSynchronizationManager->CreateSynchWaitController(
        pthr,
        m_pot,
        m_pvSynchData,
        m_ObjectDomain,
        ppWaitController
        );

    g_pSynchronizationManager->ReleaseProcessLock(pthr);

    LOGEXIT("CSharedMemoryWaitableObject::GetSynchWaitController returns %d\n",
        palError
        );

    return palError;
}

/*++
Function:
  CSharedMemoryWaitableObject::GetObjectSynchData

  Obtain the synchronization data for this object. This method should only
  be called by the synchronization manager

Parameters:
  ppvSynchData -- on success, receives a pointer to the object's synch data
--*/

PAL_ERROR
CSharedMemoryWaitableObject::GetObjectSynchData(
    VOID **ppvSynchData             // OUT
    )
{
    _ASSERTE(NULL != ppvSynchData);
    
    ENTRY("CSharedMemoryWaitableObject::GetObjectSynchData"
        "(this = %p, ppvSynchData = %p)\n",
        this,
        ppvSynchData
        );
    
    *ppvSynchData = m_pvSynchData;

    LOGEXIT("CSharedMemoryWaitableObject::GetObjectSynchData returns %d\n",
        NO_ERROR
        );
    
    return NO_ERROR;
}

