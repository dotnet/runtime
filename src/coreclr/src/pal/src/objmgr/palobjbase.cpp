// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    palobjbase.cpp

Abstract:
    PAL object base class



--*/

#include "palobjbase.hpp"
#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(PAL);

using namespace CorUnix;

CObjectType* CObjectType::s_rgotIdMapping[ObjectTypeIdCount];

/*++
Function:
  CPalObjectBase::Initialize

  Performs possibly-failing initialization for a newly-constructed
  object

Parameters:
  pthr -- thread data for calling thread
  poa -- the object attributes (e.g., name) for the object
--*/

PAL_ERROR
CPalObjectBase::Initialize(
    CPalThread *pthr,
    CObjectAttributes *poa
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != poa);

    ENTRY("CPalObjectBase::Initialize"
        "(this = %p, pthr = %p, poa = %p)\n",
        this,
        pthr,
        poa
        );

    if (0 != m_pot->GetImmutableDataSize())
    {
        m_pvImmutableData = InternalMalloc(m_pot->GetImmutableDataSize());
        if (NULL != m_pvImmutableData)
        {
            ZeroMemory(m_pvImmutableData, m_pot->GetImmutableDataSize());
        }
        else
        {
            ERROR("Unable to allocate immutable data\n");
            palError = ERROR_OUTOFMEMORY;
            goto IntializeExit;
        }
    }

    if (0 != m_pot->GetProcessLocalDataSize())
    {
        palError = m_sdlLocalData.Initialize();
        if (NO_ERROR != palError)
        {
            ERROR("Unable to initialize local data lock!\n");
            goto IntializeExit;
        }
        
        m_pvLocalData = InternalMalloc(m_pot->GetProcessLocalDataSize());
        if (NULL != m_pvLocalData)
        {
            ZeroMemory(m_pvLocalData, m_pot->GetProcessLocalDataSize());
        }
        else
        {
            ERROR("Unable to allocate local data\n");
            palError = ERROR_OUTOFMEMORY;
            goto IntializeExit;
        }
    }

    if (0 != poa->sObjectName.GetStringLength())
    {
        palError = m_oa.sObjectName.CopyString(&poa->sObjectName);
    }

IntializeExit:

    LOGEXIT("CPalObjectBase::Initialize returns %d\n", palError);

    return palError;
}

/*++
Function:
  CPalObjectBase::GetObjectType

  Returns the type of the object
--*/

CObjectType *
CPalObjectBase::GetObjectType(
    VOID
    )
{
    ENTRY("CPalObjectBase::GetObjectType(this = %p)\n", this);
    LOGEXIT("CPalObjectBase::GetObjectType returns %p\n", m_pot);
    
    return m_pot;
}

/*++
Function:
  CPalObjectBase::GetObjectAttributes

  Returns the attributes of the object
--*/

CObjectAttributes *
CPalObjectBase::GetObjectAttributes(
    VOID
    )
{
    ENTRY("CPalObjectBase::GetObjectAttributes(this = %p)\n", this);
    LOGEXIT("CPalObjectBase::GetObjectAttributes returns %p\n", &m_oa);
    
    return &m_oa;
}

/*++
Function:
  CPalObjectBase::GetImmutableData

  Provides the caller access to the object's immutable data (if any)

Parameters:
  ppvImmutableData -- on success, receives a pointer to the object's
    immutable data
--*/

PAL_ERROR
CPalObjectBase::GetImmutableData(
    void **ppvImmutableData             // OUT
    )
{
    _ASSERTE(NULL != ppvImmutableData);
    
    ENTRY("CPalObjectBase::GetImmutableData"
        "(this = %p, ppvImmutableData = %p)\n",
        this,
        ppvImmutableData
        );
    
    _ASSERTE(0 < m_pot->GetImmutableDataSize());

    *ppvImmutableData = m_pvImmutableData;

    LOGEXIT("CPalObjectBase::GetImmutableData returns %d\n", NO_ERROR);

    return NO_ERROR;
}

/*++
Function:
  CPalObjectBase::GetProcessLocalData

  Provides the caller access to the object's local data (if any)

Parameters:
  pthr -- thread data for calling thread
  eLockRequest -- specifies if the caller desires a read lock or a
    write lock on the data (currently ignored)
  ppDataLock -- on success, receives a pointer to the data lock instance
    for the local data
  ppvProcssLocalData -- on success, receives a pointer to the local data
--*/

PAL_ERROR
CPalObjectBase::GetProcessLocalData(
    CPalThread *pthr,
    LockType eLockRequest,
    IDataLock **ppDataLock,             // OUT
    void **ppvProcessLocalData          // OUT
    )
{
    _ASSERTE(NULL != pthr);
    _ASSERTE(ReadLock == eLockRequest || WriteLock == eLockRequest);
    _ASSERTE(NULL != ppDataLock);
    _ASSERTE(NULL != ppvProcessLocalData);
    
    ENTRY("CPalObjectBase::GetProcessLocalData"
        "(this = %p, pthr = %p, eLockRequest = %d, ppDataLock = %p,"
        " ppvProcessLocalData = %p)\n",
        this,
        pthr,
        eLockRequest,
        ppDataLock,
        ppvProcessLocalData
        );
    
    _ASSERTE(0 < m_pot->GetProcessLocalDataSize());

    m_sdlLocalData.AcquireLock(pthr, ppDataLock);
    *ppvProcessLocalData = m_pvLocalData;

    LOGEXIT("CPalObjectBase::GetProcessLocalData returns %d\n", NO_ERROR);
    
    return NO_ERROR;
}

/*++
Function:
  CPalObjectBase::AddReference

  Increments the object's reference count. The updated count is returned
  for diagnostic purposes only
--*/

DWORD
CPalObjectBase::AddReference(
    void
    )
{
    LONG lRefCount;

    ENTRY("CPalObjectBase::AddReference(this = %p)\n", this);

    _ASSERTE(m_lRefCount > 0);
    lRefCount = InterlockedIncrement(&m_lRefCount);

    LOGEXIT("CPalObjectBase::AddReference returns %d\n", lRefCount);

    return lRefCount;
}

/*++
Function:
  CPalObjectBase::ReleaseReference

  Decrements the object's reference count. The updated count is returned
  for diagnostic purposes only

Parameters:
  pthr -- thread data for calling thread
--*/

DWORD
CPalObjectBase::ReleaseReference(
    CPalThread *pthr
    )
{
    LONG lRefCount;

    _ASSERTE(NULL != pthr);

    ENTRY("CPalObjectBase::ReleaseReference"
        "(this = %p, pthr = %p)\n",
        this,
        pthr
        );

    AcquireObjectDestructionLock(pthr);

    _ASSERTE(m_lRefCount > 0); 

    //
    // Even though object destruction takes place under a lock
    // we still need to use an interlocked decrement, as AddRef
    // operates lock free
    //

    lRefCount = InterlockedDecrement(&m_lRefCount);

    if (0 == lRefCount)
    {
        bool fCleanupSharedState = ReleaseObjectDestructionLock(pthr, TRUE);

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

        if (NULL != m_pot->GetObjectCleanupRoutine())
        {
            (*m_pot->GetObjectCleanupRoutine())(
                pthr,
                static_cast<IPalObject*>(this),
                FALSE,
                fCleanupSharedState
                );
        }

        if (NULL != m_pot->GetImmutableDataCleanupRoutine())
        {
            (*m_pot->GetImmutableDataCleanupRoutine())(m_pvImmutableData);
        }

        if (NULL != m_pot->GetProcessLocalDataCleanupRoutine())
        {
            (*m_pot->GetProcessLocalDataCleanupRoutine())(pthr, static_cast<IPalObject*>(this));
        }

        InternalDelete(this);

        pthr->ReleaseThreadReference();
    }
    else
    {       
        ReleaseObjectDestructionLock(pthr, FALSE);
    }

    LOGEXIT("CPalObjectBase::ReleaseReference returns %d\n", lRefCount);

    return lRefCount;
}

/*++
Function:
  CPalObjectBase::~CPalObjectBase

  Object destructor
--*/

CPalObjectBase::~CPalObjectBase()
{
    ENTRY("CPalObjectBase::~CPalObjectBase(this = %p)\n", this);

    if (NULL != m_pvImmutableData)
    {
        free(m_pvImmutableData);
    }

    if (NULL != m_pvLocalData)
    {
        free(m_pvLocalData);
    }

    if (NULL != m_oa.sObjectName.GetString())
    {
        m_oa.sObjectName.FreeBuffer();
    }

    LOGEXIT("CPalObjectBase::~CPalObjectBase\n");
}

