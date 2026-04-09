// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    listedobject.hpp

Abstract:
    Shared memory based object



--*/

#include "listedobject.hpp"
#include "pal/dbgmsg.h"

#include <stddef.h>

SET_DEFAULT_DEBUG_CHANNEL(PAL);

using namespace CorUnix;

/*++
Function:
  CListedObject::Initialize

  Performs possibly-failing initialization for a newly-constructed
  object

Parameters:
  pthr -- thread data for calling thread
  poa -- the object attributes (e.g., name) for the object
--*/

PAL_ERROR
CListedObject::Initialize(
    CPalThread *pthr,
    CObjectAttributes *poa
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != poa);

    ENTRY("CListedObject::Initialize"
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


InitializeExit:

    LOGEXIT("CListedObject::Initialize returns %d\n", palError);

    return palError;
}

/*++
Function:
  CListedObject::CleanupForProcessShutdown

  Cleanup routine called by the object manager when shutting down

Parameters:
  pthr -- thread data for the calling thread
--*/

void
CListedObject::CleanupForProcessShutdown(
    CPalThread *pthr
    )
{
    _ASSERTE(NULL != pthr);

    ENTRY("CListedObject::CleanupForProcessShutdown"
        "(this = %p, pthr = %p)\n",
        this,
        pthr
        );

    if (NULL != m_pot->GetObjectCleanupRoutine())
    {
        (*m_pot->GetObjectCleanupRoutine())(
            pthr,
            static_cast<IPalObject*>(this),
            TRUE
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

    delete this;

    pthr->ReleaseThreadReference();

    LOGEXIT("CListedObject::CleanupForProcessShutdown\n");
}

/*++
Function:
  CListedObject::AcquireObjectDestructionLock

  Acquires the lock that must be held when decrementing the object's
  reference count (and, if the count drops to 0, while removing the
  object from the object manager's lists).

Parameters:
  pthr -- thread data for the calling thread
--*/

void
CListedObject::AcquireObjectDestructionLock(
    CPalThread *pthr
    )
{
    _ASSERTE(NULL != pthr);

    ENTRY("CListedObject::AcquireObjectDestructionLock"
        "(this = %p, pthr = $p)\n",
        this,
        pthr
        );

    minipal_mutex_enter(m_pcsObjListLock);

    LOGEXIT("CListedObject::AcquireObjectDestructionLock\n");
}

/*++
Function:
  CListedObject::ReleaseObjectDestructionLock

  Releases the lock acquired by AcquireObjectDestructionLock

Parameters:
  pthr -- thread data for the calling thread
  fDestructionPending -- if TRUE, the reference count for this
    object has dropped to 0; the object will be destroyed after
    this routine returns
--*/

void
CListedObject::ReleaseObjectDestructionLock(
    CPalThread *pthr,
    bool fDestructionPending
    )
{
    _ASSERTE(NULL != pthr);

    ENTRY("CListedObject::ReleaseObjectDestructionLock"
        "(this = %p, pthr = %p, fDestructionPending = %d\n",
        this,
        pthr,
        fDestructionPending
        );

    if (fDestructionPending)
    {
        RemoveEntryList(&m_le);
    }

    minipal_mutex_leave(m_pcsObjListLock);
}

/*++
Function:
  CListedObject::~CListedObject

  Destructor; should only be called from ReleaseReference
--*/

CListedObject::~CListedObject()
{
    ENTRY("CListedObject::~CListedObject(this = %p)\n", this);
    LOGEXIT("CListedObject::~CListedObject\n");
}

/*++
Function:
  CListedObject::GetObjectFromListLink

  Given a list link returns the object that contains it. Since m_le is
  protected the caller cannot perform this computation directly

Parameters:
  ple -- the list entry to obtain the object for
--*/

// static
CListedObject*
CListedObject::GetObjectFromListLink(PLIST_ENTRY ple)
{
    CListedObject *plo;

    _ASSERTE(NULL != ple);

    ENTRY("CListedObject::GetObjectFromListLink(ple = %p)\n", ple);

    //
    // Ideally we'd use CONTAINING_RECORD here, but it uses offsetof (see above
    // comment
    //

    plo = reinterpret_cast<CListedObject*>(
        reinterpret_cast<size_t>(ple) - offsetof(CListedObject, m_le)
        );

    _ASSERTE(ple == &plo->m_le);

    LOGEXIT("CListedObject::GetObjectFromListLink returns %p\n", plo);

    return plo;
}

/*++
Function:
  CListedObject::GetSynchStateController

  Obtain a synchronization state controller for this object. Should
  never be called.

Parameters:
  pthr -- thread data for calling thread
  ppStateController -- on success, receives a pointer to the state controller
    instance
--*/

PAL_ERROR
CListedObject::GetSynchStateController(
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
  CListedObject::GetSynchWaitController

  Obtain a synchronization wait controller for this object. Should
  never be called.

Parameters:
  pthr -- thread data for calling thread
  ppWaitController -- on success, receives a pointer to the wait controller
    instance
--*/

PAL_ERROR
CListedObject::GetSynchWaitController(
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
  CListedObject::GetObjectSynchData

  Obtain the synchronization data for this object. Should
  never be called.

Parameters:
  ppvSynchData -- on success, receives a pointer to the object's synch data
--*/

PAL_ERROR
CListedObject::GetObjectSynchData(
    VOID **ppvSynchData             // OUT
    )
{
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

    palError = CListedObject::Initialize(pthr, poa);
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
        &m_pvSynchData
        );

InitializeExit:

    LOGEXIT("CSharedMemoryWaitableObject::Initialize returns %d\n", palError);

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

    if (NULL != m_pvSynchData)
    {
        g_pSynchronizationManager->FreeObjectSynchData(
            m_pot,
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

