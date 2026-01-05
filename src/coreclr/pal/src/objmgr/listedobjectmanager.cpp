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

    InitializeListHead(&m_leNamedObjects);
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

    while (!IsListEmpty(&m_leNamedObjects))
    {
        ple = RemoveTailList(&m_leNamedObjects);
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
  poa -- attributes (name and SD) of object to allocate
  ppobjNew -- on success, receives a reference to the new object
--*/

PAL_ERROR
CListedObjectManager::AllocateObject(
    CPalThread *pthr,
    CObjectType *pot,
    CObjectAttributes *poa,
    IPalObject **ppobjNew            // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    CListedObject *pshmobj = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pot);
    _ASSERTE(NULL != poa);
    _ASSERTE(NULL != ppobjNew);

    ENTRY("CListedObjectManager::AllocateObject "
        "(this=%p, pthr=%p, pot=%p, poa=%p, ppobjNew=%p)\n",
        this,
        pthr,
        pot,
        poa,
        ppobjNew
        );

    if (CObjectType::WaitableObject == pot->GetSynchronizationSupport())
    {
        pshmobj = new(std::nothrow) CSharedMemoryWaitableObject(pot, &m_csListLock);
    }
    else
    {
        pshmobj = new(std::nothrow) CListedObject(pot, &m_csListLock);
    }

    if (NULL != pshmobj)
    {
        palError = pshmobj->Initialize(pthr, poa);
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

  Registers a newly-allocated object instance. If the object to be registered
  has a name, and a previously registered object has the same name the new
  object will not be registered.

Distinguished return values:
  ERROR_ALREADY_EXISTS -- an object of a compatible type was already registered
    with the specified name
  ERROR_INVALID_HANDLE -- an object of an incompatible type was already
    registered with the specified name

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
    CObjectAttributes *poa;
    CObjectType *potObj;
    IPalObject *pobjExisting;

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

    poa = pobjToRegister->GetObjectAttributes();
    _ASSERTE(NULL != poa);

    potObj = pobjToRegister->GetObjectType();

    minipal_mutex_enter(&m_csListLock);

    if (0 != poa->sObjectName.GetStringLength())
    {
        //
        // Check if an object by this name already exists
        //

        palError = LocateObject(
            pthr,
            &poa->sObjectName,
            paot,
            &pobjExisting
            );

        if (NO_ERROR == palError)
        {
            //
            // Obtain a new handle to the existing object
            //

            palError = ObtainHandleForObject(
                pthr,
                pobjExisting,
                pHandle
                );

            if (NO_ERROR == palError)
            {
                //
                // Transfer object reference to out param
                //

                *ppobjRegistered = pobjExisting;
                palError = ERROR_ALREADY_EXISTS;
            }
            else
            {
                pobjExisting->ReleaseReference(pthr);
            }

            goto RegisterObjectExit;
        }
        else if (ERROR_INVALID_NAME != palError)
        {
            //
            // Something different than an object not found error
            // occurred. This is most likely due to a type conflict.
            //

            goto RegisterObjectExit;
        }

        //
        // Insert the object on the named object lists
        //

        InsertTailList(&m_leNamedObjects, pshmobj->GetObjectListLink());
    }
    else
    {
        //
        // Place the object on the anonymous object list
        //

        InsertTailList(&m_leAnonymousObjects, pshmobj->GetObjectListLink());
    }

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

RegisterObjectExit:

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
  CListedObjectManager::LocateObject

  Search for a previously registered object with a give name and type

Distinguished return values:
  ERROR_INVALID_NAME -- no object with the specified name was previously
    registered
  ERROR_INVALID_HANDLE -- an object with the specified name was previously
    registered, but its type is not compatible

Parameters:
  pthr -- thread data for calling thread
  psObjectToLocate -- the name of the object to locate
  paot -- acceptable types for the object
  ppobj -- on success, receives a reference to the object instance
--*/

PAL_ERROR
CListedObjectManager::LocateObject(
    CPalThread *pthr,
    CPalString *psObjectToLocate,
    CAllowedObjectTypes *paot,
    IPalObject **ppobj               // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjExisting = NULL;
    LPWSTR pwsz = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != psObjectToLocate);
    _ASSERTE(NULL != psObjectToLocate->GetString());
    _ASSERTE(PAL_wcslen(psObjectToLocate->GetString()) == psObjectToLocate->GetStringLength());
    _ASSERTE(NULL != ppobj);

    ENTRY("CListedObjectManager::LocateObject "
        "(this=%p, pthr=%p, psObjectToLocate=%p, paot=%p, "
        "ppobj=%p)\n",
        this,
        pthr,
        psObjectToLocate,
        paot,
        ppobj
        );

    TRACE("Searching for object name %S\n", psObjectToLocate->GetString());

    minipal_mutex_enter(&m_csListLock);

    //
    // Search the local named object list for this object
    //

    for (PLIST_ENTRY ple = m_leNamedObjects.Flink;
         ple != &m_leNamedObjects;
         ple = ple->Flink)
    {
        CObjectAttributes *poa;
        CListedObject *pshmobj =
            CListedObject::GetObjectFromListLink(ple);

        poa = pshmobj->GetObjectAttributes();
        _ASSERTE(NULL != poa);

        if (poa->sObjectName.GetStringLength() != psObjectToLocate->GetStringLength())
        {
            continue;
        }

        if (0 != PAL_wcscmp(poa->sObjectName.GetString(), psObjectToLocate->GetString()))
        {
            continue;
        }

        //
        // This object has the name we're looking for
        //

        pobjExisting = static_cast<IPalObject*>(pshmobj);
        break;
    }

    if (NULL != pobjExisting)
    {
        //
        //  Validate the located object's type
        //

        if (paot->IsTypeAllowed(
                pobjExisting->GetObjectType()->GetId()
                ))
        {
            TRACE("Local object exists with compatible type\n");

            //
            // Add a reference to the found object
            //

            pobjExisting->AddReference();
            *ppobj = pobjExisting;
        }
        else
        {
            TRACE("Local object exists w/ incompatible type\n");
            palError = ERROR_INVALID_HANDLE;
        }

        goto LocateObjectExit;
    }

LocateObjectExit:

    minipal_mutex_leave(&m_csListLock);

    LOGEXIT("CListedObjectManager::LocateObject returns %d\n", palError);

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
  CListedObjectManager::ReferenceObjectByHandleArray

  Returns the referenced object instances that an array of handles
  refer to.

Parameters:
  pthr -- thread data for calling thread
  rgHandlesToReference -- the array of handles to reference
  dwHandleCount -- the number of handles in the arrayu
  paot -- acceptable types for the underlying objects
  rgpobjs -- on success, receives references to the object instances; will
    be empty on failures
--*/

PAL_ERROR
CListedObjectManager::ReferenceMultipleObjectsByHandleArray(
    CPalThread *pthr,
    HANDLE rghHandlesToReference[],
    DWORD dwHandleCount,
    CAllowedObjectTypes *paot,
    IPalObject *rgpobjs[]            // OUT (caller allocated)
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobj = NULL;
    DWORD dw;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != rghHandlesToReference);
    _ASSERTE(0 < dwHandleCount);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != rgpobjs);

    ENTRY("CListedObjectManager::ReferenceMultipleObjectsByHandleArray "
        "(this=%p, pthr=%p, rghHandlesToReference=%p, dwHandleCount=%d, "
        "pAllowedTyped=%d, rgpobjs=%p)\n",
        this,
        pthr,
        rghHandlesToReference,
        dwHandleCount,
        paot,
        rgpobjs
        );

    m_HandleManager.Lock(pthr);

    for (dw = 0; dw < dwHandleCount; dw += 1)
    {
        palError = m_HandleManager.GetObjectFromHandle(
            pthr,
            rghHandlesToReference[dw],
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
                // Transfer reference to out array
                //

                rgpobjs[dw] = pobj;
                pobj = NULL;
            }
        }

        if (NO_ERROR != palError)
        {
            break;
        }
    }

    //
    // The handle manager lock must be released before releasing
    // any object references, as ReleaseReference will acquire
    // the object manager list lock (which needs to be acquired before
    // the handle manager lock)
    //

    m_HandleManager.Unlock(pthr);

    if (NO_ERROR != palError)
    {
        //
        // dw's current value is the failing index, so we want
        // to free from dw - 1.
        //

        while (dw > 0)
        {
            rgpobjs[--dw]->ReleaseReference(pthr);
        }

        if (NULL != pobj)
        {
            pobj->ReleaseReference(pthr);
        }
    }

    LOGEXIT("CListedObjectManager::ReferenceMultipleObjectsByHandleArray"
        " returns %d\n",
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


