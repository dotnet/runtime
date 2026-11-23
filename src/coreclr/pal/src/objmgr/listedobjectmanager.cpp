// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    shmobjectmgr.cpp

Abstract:
    Shared memory based object manager



--*/

#include "listedobjectmanager.hpp"
#include "listedobject.hpp"
#include "pal/thread.hpp"
#include "pal/procobj.hpp"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(PAL);

#include "pal/corunix.inl"

using namespace CorUnix;

IPalObjectManager * CorUnix::g_pObjectManager;

static
PAL_ERROR
CheckObjectTypeAndRights(
    IPalObject *pobj,
    CAllowedObjectTypes *paot
    );

/*++
Function:
  CListedObjectManager::Initialize

  Performs (possibly failing) startup tasks for the object manager

Parameters:
  None
--*/

PAL_ERROR
CListedObjectManager::Initialize(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;

    ENTRY("CListedObjectManager::Initialize (this=%p)\n", this);

    InitializeListHead(&m_leAnonymousObjects);

    minipal_mutex_init(&m_csListLock);
    m_fListLockInitialized = TRUE;

    palError = m_HandleManager.Initialize();

    LOGEXIT("CListedObjectManager::Initialize returns %d", palError);

    return palError;
}

/*++
Function:
  CListedObjectManager::Shutdown

  Cleans up the object manager. This routine will call cleanup routines
  for all objects referenced by this process. After this routine is called
  no attempt should be made to access an IPalObject.

Parameters:
  pthr -- thread data for calling thread
--*/

PAL_ERROR
CListedObjectManager::Shutdown(
    CPalThread *pthr
    )
{
    PLIST_ENTRY ple;
    CListedObject *pshmobj;

    _ASSERTE(NULL != pthr);

    ENTRY("CListedObjectManager::Shutdown (this=%p, pthr=%p)\n",
        this,
        pthr
        );

    minipal_mutex_enter(&m_csListLock);

    while (!IsListEmpty(&m_leAnonymousObjects))
    {
        ple = RemoveTailList(&m_leAnonymousObjects);
        pshmobj = CListedObject::GetObjectFromListLink(ple);
        pshmobj->CleanupForProcessShutdown(pthr);
    }

    minipal_mutex_leave(&m_csListLock);

    LOGEXIT("CListedObjectManager::Shutdown returns %d\n", NO_ERROR);

    return NO_ERROR;
}

/*++
Function:
  CListedObjectManager::AllocateObject

  Allocates a new object instance of the specified type.

Parameters:
  pthr -- thread data for calling thread
  pot -- type of object to allocate
  ppobjNew -- on success, receives a reference to the new object
--*/

PAL_ERROR
CListedObjectManager::AllocateObject(
    CPalThread *pthr,
    CObjectType *pot,
    IPalObject **ppobjNew            // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    CListedObject *pshmobj = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pot);
    _ASSERTE(NULL != ppobjNew);

    ENTRY("CListedObjectManager::AllocateObject "
        "(this=%p, pthr=%p, pot=%p, ppobjNew=%p)\n",
        this,
        pthr,
        pot,
        ppobjNew
        );

    pshmobj = new(std::nothrow) CListedObject(pot, &m_csListLock);

    if (NULL != pshmobj)
    {
        palError = pshmobj->Initialize(pthr);
        if (NO_ERROR == palError)
        {
            *ppobjNew = static_cast<IPalObject*>(pshmobj);
        }
    }
    else
    {
        ERROR("Unable to allocate pshmobj\n");
        palError = ERROR_OUTOFMEMORY;
    }

    LOGEXIT("CListedObjectManager::AllocateObject returns %d\n", palError);
    return palError;
}

/*++
Function:
  CListedObjectManager::RegisterObject

  Registers a newly-allocated object instance.

Parameters:
  pthr -- thread data for calling thread
  pobjToRegister -- the object instance to register. This routine will always
    call ReleaseReference on this instance
  paot -- object types that are compatible with the new object instance
  pHandle -- on success, receives a handle to the registered object
  ppobjRegistered -- on success, receives a reference to the registered object
    instance.
--*/

PAL_ERROR
CListedObjectManager::RegisterObject(
    CPalThread *pthr,
    IPalObject *pobjToRegister,
    CAllowedObjectTypes *paot,
    HANDLE *pHandle,                 // OUT
    IPalObject **ppobjRegistered     // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    CListedObject *pshmobj = static_cast<CListedObject*>(pobjToRegister);

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pobjToRegister);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != pHandle);
    _ASSERTE(NULL != ppobjRegistered);

    ENTRY("CListedObjectManager::RegisterObject "
        "(this=%p, pthr=%p, pobjToRegister=%p, paot=%p, "
        "pHandle=%p, ppobjRegistered=%p)\n",
        this,
        pthr,
        pobjToRegister,
        paot,
        pHandle,
        ppobjRegistered
        );

    minipal_mutex_enter(&m_csListLock);

    //
    // Place the object on the anonymous object list
    //

    InsertTailList(&m_leAnonymousObjects, pshmobj->GetObjectListLink());

    //
    // Obtain a handle for the new object
    //

    palError = ObtainHandleForObject(
        pthr,
        pobjToRegister,
        pHandle
        );

    if (NO_ERROR == palError)
    {
        //
        // Transfer pobjToRegister reference to out param
        //

        *ppobjRegistered = pobjToRegister;
        pobjToRegister = NULL;
    }

    minipal_mutex_leave(&m_csListLock);

    if (NULL != pobjToRegister)
    {
        pobjToRegister->ReleaseReference(pthr);
    }

    LOGEXIT("CListedObjectManager::RegisterObject return %d\n", palError);

    return palError;
}

/*++
Function:
  CListedObjectManager::ObtainHandleForObject

  Allocated a new handle for an object

Parameters:
  pthr -- thread data for calling thread
  pobj -- the object to allocate a handle for
  pNewHandle -- on success, receives the newly allocated handle
--*/

PAL_ERROR
CListedObjectManager::ObtainHandleForObject(
    CPalThread *pthr,
    IPalObject *pobj,
    HANDLE *pNewHandle                  // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pobj);
    _ASSERTE(NULL != pNewHandle);

    ENTRY("CListedObjectManager::ObtainHandleForObject "
        "(this=%p, pthr=%p, pobj=%p, "
        "pNewHandle=%p)\n",
        this,
        pthr,
        pobj,
        pNewHandle
        );

    palError = m_HandleManager.AllocateHandle(
        pthr,
        pobj,
        pNewHandle
        );

    LOGEXIT("CListedObjectManager::ObtainHandleForObject return %d\n", palError);

    return palError;
}

/*++
Function:
  CListedObjectManager::RevokeHandle

  Removes a handle from the process's handle table, which in turn releases
  the handle's reference on the object instance it refers to

Parameters:
  pthr -- thread data for calling thread
  hHandleToRevoke -- the handle to revoke
--*/

PAL_ERROR
CListedObjectManager::RevokeHandle(
    CPalThread *pthr,
    HANDLE hHandleToRevoke
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);

    ENTRY("CListedObjectManager::RevokeHandle "
        "(this=%p, pthr=%p, hHandleToRevoke=%p)\n",
        this,
        pthr,
        hHandleToRevoke
        );

    palError = m_HandleManager.FreeHandle(pthr, hHandleToRevoke);

    LOGEXIT("CListedObjectManager::RevokeHandle returns %d\n", palError);

    return palError;
}

/*++
Function:
  CListedObjectManager::ReferenceObjectByHandle

  Returns a referenced object instance that a handle refers to

Parameters:
  pthr -- thread data for calling thread
  hHandleToReference -- the handle to reference
  paot -- acceptable types for the underlying object
    granted; currently ignored
  ppobj -- on success, receives a reference to the object instance
--*/

PAL_ERROR
CListedObjectManager::ReferenceObjectByHandle(
    CPalThread *pthr,
    HANDLE hHandleToReference,
    CAllowedObjectTypes *paot,
    IPalObject **ppobj               // OUT
    )
{
    PAL_ERROR palError;
    IPalObject *pobj;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != ppobj);

    ENTRY("CListedObjectManager::ReferenceObjectByHandle "
        "(this=%p, pthr=%p, hHandleToReference=%p, paot=%p, ppobj=%p)\n",
        this,
        pthr,
        hHandleToReference,
        paot,
        ppobj
        );

    palError = m_HandleManager.GetObjectFromHandle(
        pthr,
        hHandleToReference,
        &pobj
        );

    if (NO_ERROR == palError)
    {
        palError = CheckObjectTypeAndRights(
            pobj,
            paot
            );

        if (NO_ERROR == palError)
        {
            //
            // Transfer object reference to out parameter
            //

            *ppobj = pobj;
        }
        else
        {
            pobj->ReleaseReference(pthr);
        }
    }

    LOGEXIT("CListedObjectManager::ReferenceObjectByHandle returns %d\n",
        palError
        );

    return palError;
}

/*++
Function:
  CheckObjectTypeAndRights

  Helper routine that determines if:
  1) An object instance is of a specified type
  2) A set of granted access rights satisfies the required access rights
     (currently ignored)

Parameters:
  pobj -- the object instance whose type is to be checked
  paot -- the acceptable type for the object instance
--*/

static
PAL_ERROR
CheckObjectTypeAndRights(
    IPalObject *pobj,
    CAllowedObjectTypes *paot
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pobj);
    _ASSERTE(NULL != paot);

    ENTRY("CheckObjectTypeAndRights (pobj=%p, paot=%p)\n",
        pobj,
        paot
        );

    if (!paot->IsTypeAllowed(pobj->GetObjectType()->GetId()))
    {
        palError = ERROR_INVALID_HANDLE;
    }

    LOGEXIT("CheckObjectTypeAndRights returns %d\n", palError);

    return palError;
}
