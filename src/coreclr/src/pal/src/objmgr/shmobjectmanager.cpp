// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    shmobjectmgr.cpp

Abstract:
    Shared memory based object manager



--*/

#include "shmobjectmanager.hpp"
#include "shmobject.hpp"
#include "pal/cs.hpp"
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
    CAllowedObjectTypes *paot,
    DWORD dwRightsGranted,
    DWORD dwRightsRequired
    );

/*++
Function:
  CSharedMemoryObjectManager::Initialize

  Performs (possibly failing) startup tasks for the object manager

Parameters:
  None
--*/

PAL_ERROR
CSharedMemoryObjectManager::Initialize(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;

    ENTRY("CSharedMemoryObjectManager::Initialize (this=%p)\n", this);
    
    InitializeListHead(&m_leNamedObjects);
    InitializeListHead(&m_leAnonymousObjects);

    InternalInitializeCriticalSection(&m_csListLock);
    m_fListLockInitialized = TRUE;

    palError = m_HandleManager.Initialize();

    LOGEXIT("CSharedMemoryObjectManager::Initialize returns %d", palError);
    
    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::Shutdown

  Cleans up the object manager. This routine will call cleanup routines
  for all objects referenced by this process. After this routine is called
  no attempt should be made to access an IPalObject.

Parameters:
  pthr -- thread data for calling thread
--*/

PAL_ERROR
CSharedMemoryObjectManager::Shutdown(
    CPalThread *pthr
    )
{
    PLIST_ENTRY ple;
    CSharedMemoryObject *pshmobj;

    _ASSERTE(NULL != pthr);
    
    ENTRY("CSharedMemoryObjectManager::Shutdown (this=%p, pthr=%p)\n",
        this,
        pthr
        );

    InternalEnterCriticalSection(pthr, &m_csListLock);
    SHMLock();

    while (!IsListEmpty(&m_leAnonymousObjects))
    {
        ple = RemoveTailList(&m_leAnonymousObjects);
        pshmobj = CSharedMemoryObject::GetObjectFromListLink(ple);
        pshmobj->CleanupForProcessShutdown(pthr);
    }

    while (!IsListEmpty(&m_leNamedObjects))
    {
        ple = RemoveTailList(&m_leNamedObjects);
        pshmobj = CSharedMemoryObject::GetObjectFromListLink(ple);
        pshmobj->CleanupForProcessShutdown(pthr);
    }

    SHMRelease();
    InternalLeaveCriticalSection(pthr, &m_csListLock);

    LOGEXIT("CSharedMemoryObjectManager::Shutdown returns %d\n", NO_ERROR);
    
    return NO_ERROR;
}

/*++
Function:
  CSharedMemoryObjectManager::AllocateObject

  Allocates a new object instance of the specified type.

Parameters:
  pthr -- thread data for calling thread
  pot -- type of object to allocate
  poa -- attributes (name and SD) of object to allocate
  ppobjNew -- on success, receives a reference to the new object
--*/

PAL_ERROR
CSharedMemoryObjectManager::AllocateObject(
    CPalThread *pthr,
    CObjectType *pot,
    CObjectAttributes *poa,
    IPalObject **ppobjNew            // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    CSharedMemoryObject *pshmobj = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pot);
    _ASSERTE(NULL != poa);
    _ASSERTE(NULL != ppobjNew);

    ENTRY("CSharedMemoryObjectManager::AllocateObject "
        "(this=%p, pthr=%p, pot=%p, poa=%p, ppobjNew=%p)\n",
        this,
        pthr,
        pot,
        poa,
        ppobjNew
        );

    if (CObjectType::WaitableObject == pot->GetSynchronizationSupport())
    {
        pshmobj = InternalNew<CSharedMemoryWaitableObject>(pot, &m_csListLock);
    }
    else
    {
        pshmobj = InternalNew<CSharedMemoryObject>(pot, &m_csListLock);
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

    LOGEXIT("CSharedMemoryObjectManager::AllocateObject returns %d\n", palError);
    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::RegisterObject

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
  dwRightsRequested -- requested access rights for the returned handle (ignored)
  pHandle -- on success, receives a handle to the registered object
  ppobjRegistered -- on success, receives a reference to the registered object
    instance.
--*/

PAL_ERROR
CSharedMemoryObjectManager::RegisterObject(
    CPalThread *pthr,
    IPalObject *pobjToRegister,
    CAllowedObjectTypes *paot,
    DWORD dwRightsRequested,
    HANDLE *pHandle,                 // OUT
    IPalObject **ppobjRegistered     // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    CSharedMemoryObject *pshmobj = static_cast<CSharedMemoryObject*>(pobjToRegister);
    SHMObjData *psmodNew = NULL;
    CObjectAttributes *poa;
    CObjectType *potObj;
    IPalObject *pobjExisting;
    BOOL fInherit = FALSE;
    BOOL fShared = FALSE;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pobjToRegister);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != pHandle);
    _ASSERTE(NULL != ppobjRegistered);

    ENTRY("CSharedMemoryObjectManager::RegisterObject "
        "(this=%p, pthr=%p, pobjToRegister=%p, paot=%p, "
        "dwRightsRequested=%d, pHandle=%p, ppobjRegistered=%p)\n",
        this,
        pthr,
        pobjToRegister,
        paot,
        dwRightsRequested,
        pHandle,
        ppobjRegistered
        );

    poa = pobjToRegister->GetObjectAttributes();
    _ASSERTE(NULL != poa);

    if (NULL != poa->pSecurityAttributes)
    {
        fInherit = poa->pSecurityAttributes->bInheritHandle;
    }

    potObj = pobjToRegister->GetObjectType();
    fShared = (SharedObject == pshmobj->GetObjectDomain());
    
    InternalEnterCriticalSection(pthr, &m_csListLock);

    if (fShared)
    {
        //
        // We only need to acquire the shared memory lock if this
        // object is actually shared.
        //
        
        SHMLock();
    }

    if (0 != poa->sObjectName.GetStringLength())
    {
        SHMPTR shmObjectListHead = NULL;

        //
        // The object must be shared
        //

        _ASSERTE(fShared);
        
        //
        // Check if an object by this name alredy exists
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
                dwRightsRequested,
                fInherit,
                NULL, 
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

        psmodNew = SHMPTR_TO_TYPED_PTR(SHMObjData, pshmobj->GetShmObjData());
        if (NULL == psmodNew)
        {
            ASSERT("Failure to map shared object data\n");
            palError = ERROR_INTERNAL_ERROR;
            goto RegisterObjectExit;
        }

        shmObjectListHead = SHMGetInfo(SIID_NAMED_OBJECTS);
        if (NULL != shmObjectListHead)
        {
            SHMObjData *psmodListHead;
            
            psmodListHead = SHMPTR_TO_TYPED_PTR(SHMObjData, shmObjectListHead);
            if (NULL != psmodListHead)
            {
                psmodNew->shmNextObj = shmObjectListHead;
                psmodListHead->shmPrevObj = pshmobj->GetShmObjData();
            }
            else
            {
                ASSERT("Failure to map shared object data\n");
                palError = ERROR_INTERNAL_ERROR;
                goto RegisterObjectExit;
            }
        }

        psmodNew->fAddedToList = TRUE;

        if (!SHMSetInfo(SIID_NAMED_OBJECTS, pshmobj->GetShmObjData()))
        {
            ASSERT("Failed to set shared named object list head\n");
            palError = ERROR_INTERNAL_ERROR;
            goto RegisterObjectExit;
        }
    }
    else
    {
        //
        // Place the object on the anonymous object list
        //

        InsertTailList(&m_leAnonymousObjects, pshmobj->GetObjectListLink());
    }

    //
    // Hoist the object's immutable data (if any) into shared memory if
    // the object is shared
    //

    if (fShared && 0 != potObj->GetImmutableDataSize())
    {
        VOID *pvImmutableData;
        SHMObjData *psmod;

        palError = pobjToRegister->GetImmutableData(&pvImmutableData);
        if (NO_ERROR != palError)
        {
            ASSERT("Failure to obtain object immutable data\n");
            goto RegisterObjectExit;
        }

        psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, pshmobj->GetShmObjData());
        if (NULL != psmod)
        {
            VOID *pvSharedImmutableData =
                SHMPTR_TO_TYPED_PTR(VOID, psmod->shmObjImmutableData);
            
            if (NULL != pvSharedImmutableData)
            {
                CopyMemory(
                    pvSharedImmutableData,
                    pvImmutableData,
                    potObj->GetImmutableDataSize()
                    );

                if (NULL != potObj->GetImmutableDataCopyRoutine())
                {
                    (*potObj->GetImmutableDataCopyRoutine())(pvImmutableData, pvSharedImmutableData);
                }

                psmod->pCopyRoutine = potObj->GetImmutableDataCopyRoutine();
                psmod->pCleanupRoutine = potObj->GetImmutableDataCleanupRoutine();
            }
            else
            {
                ASSERT("Failure to map psmod->shmObjImmutableData\n");
                palError = ERROR_INTERNAL_ERROR;
                goto RegisterObjectExit;
            }
        }
        else
        {
            ASSERT("Failure to map pshmobj->GetShmObjData()\n");
            palError = ERROR_INTERNAL_ERROR;
            goto RegisterObjectExit;
        }
    }

    //
    // Obtain a handle for the new object
    //

    palError = ObtainHandleForObject(
        pthr,
        pobjToRegister,
        dwRightsRequested,
        fInherit,
        NULL, 
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

    if (fShared)
    {
        SHMRelease();
    }
    
    InternalLeaveCriticalSection(pthr, &m_csListLock);

    if (NULL != pobjToRegister)
    {
        pobjToRegister->ReleaseReference(pthr);
    }

    LOGEXIT("CSharedMemoryObjectManager::RegisterObject return %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::LocateObject

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
CSharedMemoryObjectManager::LocateObject(
    CPalThread *pthr,
    CPalString *psObjectToLocate,
    CAllowedObjectTypes *paot,
    IPalObject **ppobj               // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjExisting = NULL;
    SHMPTR shmSharedObjectData = NULL;
    SHMPTR shmObjectListEntry = NULL;
    SHMObjData *psmod = NULL;
    LPWSTR pwsz = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != psObjectToLocate);
    _ASSERTE(NULL != psObjectToLocate->GetString());
    _ASSERTE(PAL_wcslen(psObjectToLocate->GetString()) == psObjectToLocate->GetStringLength());
    _ASSERTE(NULL != ppobj);

    ENTRY("CSharedMemoryObjectManager::LocateObject "
        "(this=%p, pthr=%p, psObjectToLocate=%p, paot=%p, "
        "ppobj=%p)\n",
        this,
        pthr,
        psObjectToLocate,
        paot,
        ppobj
        );

    TRACE("Searching for object name %S\n", psObjectToLocate->GetString());

    InternalEnterCriticalSection(pthr, &m_csListLock);

    //
    // Search the local named object list for this object
    //

    for (PLIST_ENTRY ple = m_leNamedObjects.Flink;
         ple != &m_leNamedObjects;
         ple = ple->Flink)
    {
        CObjectAttributes *poa;
        CSharedMemoryObject *pshmobj =
            CSharedMemoryObject::GetObjectFromListLink(ple);

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

    //
    // Search the shared memory named object list for a matching object
    //
    
    SHMLock();
    
    shmObjectListEntry = SHMGetInfo(SIID_NAMED_OBJECTS);
    while (NULL != shmObjectListEntry)
    {
        psmod = SHMPTR_TO_TYPED_PTR(SHMObjData, shmObjectListEntry);
        if (NULL != psmod)
        {
            if (psmod->dwNameLength == psObjectToLocate->GetStringLength())
            {
                pwsz = SHMPTR_TO_TYPED_PTR(WCHAR, psmod->shmObjName);
                if (NULL != pwsz)
                {
                    if (0 == PAL_wcscmp(pwsz, psObjectToLocate->GetString()))
                    {
                        //
                        // This is the object we were looking for.
                        //

                        shmSharedObjectData = shmObjectListEntry;
                        break;
                    }
                }
                else
                {
                    ASSERT("Unable to map psmod->shmObjName\n");
                    break;
                }                
            }

            shmObjectListEntry = psmod->shmNextObj;
        }
        else
        {
            ASSERT("Unable to map shmObjectListEntry\n");
            break;
        }
    }

    if (NULL != shmSharedObjectData)
    {
        CSharedMemoryObject *pshmobj = NULL;
        CObjectAttributes oa(pwsz, NULL);

        //
        // Check if the type is allowed
        //

        if (!paot->IsTypeAllowed(psmod->eTypeId))
        {
            TRACE("Remote object exists w/ incompatible type\n");
            palError = ERROR_INVALID_HANDLE;
            goto LocateObjectExitSHMRelease;
        }
        
        //
        // Get the local instance of the CObjectType
        //
        
        CObjectType *pot = CObjectType::GetObjectTypeById(psmod->eTypeId);
        if (NULL == pot)
        {
            ASSERT("Invalid object type ID in shared memory info\n");
            goto LocateObjectExitSHMRelease;
        }

        TRACE("Remote object exists compatible type -- importing\n");
        
        //
        // Create the local state for the shared object
        //

        palError = ImportSharedObjectIntoProcess(
            pthr,
            pot,
            &oa,
            shmSharedObjectData,
            psmod,
            TRUE,
            &pshmobj
            );

        if (NO_ERROR == palError)
        {   
            *ppobj = static_cast<IPalObject*>(pshmobj);
        }
        else
        {
            ERROR("Failure initializing object from shared data\n");
            goto LocateObjectExitSHMRelease;
        }
        
    }
    else
    {
        //
        // The object was not found
        //

        palError = ERROR_INVALID_NAME;
    }

LocateObjectExitSHMRelease:

    SHMRelease();

LocateObjectExit:

    InternalLeaveCriticalSection(pthr, &m_csListLock);

    LOGEXIT("CSharedMemoryObjectManager::LocateObject returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::ObtainHandleForObject

  Allocated a new handle for an object

Parameters:
  pthr -- thread data for calling thread
  pobj -- the object to allocate a handle for
  dwRightsRequired -- the access rights to grant the handle; currently ignored
  fInheritHandle -- true if the handle is inheritable; ignored for all but file
    objects that represent pipes
  pProcessForHandle -- the process the handle is to be used from; currently
    must be NULL
  pNewHandle -- on success, receives the newly allocated handle
--*/

PAL_ERROR   
CSharedMemoryObjectManager::ObtainHandleForObject(
    CPalThread *pthr,
    IPalObject *pobj,
    DWORD dwRightsRequested,
    bool fInheritHandle,
    IPalProcess *pProcessForHandle,     // IN, OPTIONAL
    HANDLE *pNewHandle                  // OUT
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pobj);
    _ASSERTE(NULL != pNewHandle);

    ENTRY("CSharedMemoryObjectManager::ObtainHandleForObject "
        "(this=%p, pthr=%p, pobj=%p, dwRightsRequested=%d, "
        "fInheritHandle=%p, pProcessForHandle=%p, pNewHandle=%p)\n",
        this,
        pthr,
        pobj,
        dwRightsRequested,
        fInheritHandle,
        pProcessForHandle,
        pNewHandle
        );

    if (NULL != pProcessForHandle)
    {
        //
        // Not yet supported
        //

        ASSERT("Caller to ObtainHandleForObject provided a process\n");
        return ERROR_CALL_NOT_IMPLEMENTED;
    }

    palError = m_HandleManager.AllocateHandle(
        pthr,
        pobj,
        dwRightsRequested,
        fInheritHandle,
        pNewHandle
        );

    LOGEXIT("CSharedMemoryObjectManager::ObtainHandleForObject return %d\n", palError);

    return palError;    
}

/*++
Function:
  CSharedMemoryObjectManager::RevokeHandle

  Removes a handle from the process's handle table, which in turn releases
  the handle's reference on the object instance it refers to

Parameters:
  pthr -- thread data for calling thread
  hHandleToRevoke -- the handle to revoke
--*/

PAL_ERROR
CSharedMemoryObjectManager::RevokeHandle(
    CPalThread *pthr,
    HANDLE hHandleToRevoke
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pthr);

    ENTRY("CSharedMemoryObjectManager::RevokeHandle "
        "(this=%p, pthr=%p, hHandleToRevoke=%p)\n",
        this,
        pthr,
        hHandleToRevoke
        );
    
    palError = m_HandleManager.FreeHandle(pthr, hHandleToRevoke);

    LOGEXIT("CSharedMemoryObjectManager::RevokeHandle returns %d\n", palError);

    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::ReferenceObjectByHandle

  Returns a referenced object instance that a handle refers to

Parameters:
  pthr -- thread data for calling thread
  hHandleToReference -- the handle to reference
  paot -- acceptable types for the underlying object
  dwRightsRequired -- the access rights that the handle must have been
    granted; currently ignored
  ppobj -- on success, receives a reference to the object instance
--*/

PAL_ERROR
CSharedMemoryObjectManager::ReferenceObjectByHandle(
    CPalThread *pthr,
    HANDLE hHandleToReference,
    CAllowedObjectTypes *paot,
    DWORD dwRightsRequired,
    IPalObject **ppobj               // OUT
    )
{
    PAL_ERROR palError;
    DWORD dwRightsGranted;
    IPalObject *pobj;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != ppobj);

    ENTRY("CSharedMemoryObjectManager::ReferenceObjectByHandle "
        "(this=%p, pthr=%p, hHandleToReference=%p, paot=%p, "
        "dwRightsRequired=%d, ppobj=%p)\n",
        this,
        pthr,
        hHandleToReference,
        paot,
        dwRightsRequired,
        ppobj
        );

    palError = m_HandleManager.GetObjectFromHandle(
        pthr,
        hHandleToReference,
        &dwRightsGranted,
        &pobj
        );

    if (NO_ERROR == palError)
    {
        palError = CheckObjectTypeAndRights(
            pobj,
            paot,
            dwRightsGranted,
            dwRightsRequired
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

    LOGEXIT("CSharedMemoryObjectManager::ReferenceObjectByHandle returns %d\n",
        palError
        );

    return palError;    
}

/*++
Function:
  CSharedMemoryObjectManager::ReferenceObjectByHandleArray

  Returns the referenced object instances that an array of handles
  refer to.

Parameters:
  pthr -- thread data for calling thread
  rgHandlesToReference -- the array of handles to reference
  dwHandleCount -- the number of handles in the arrayu
  paot -- acceptable types for the underlying objects
  dwRightsRequired -- the access rights that the handles must have been
    granted; currently ignored
  rgpobjs -- on success, receives references to the object instances; will
    be empty on failures
--*/

PAL_ERROR
CSharedMemoryObjectManager::ReferenceMultipleObjectsByHandleArray(
    CPalThread *pthr,
    HANDLE rghHandlesToReference[],
    DWORD dwHandleCount,
    CAllowedObjectTypes *paot,
    DWORD dwRightsRequired,
    IPalObject *rgpobjs[]            // OUT (caller allocated)
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobj = NULL;
    DWORD dwRightsGranted;
    DWORD dw;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != rghHandlesToReference);
    _ASSERTE(0 < dwHandleCount);
    _ASSERTE(NULL != paot);
    _ASSERTE(NULL != rgpobjs);

    ENTRY("CSharedMemoryObjectManager::ReferenceMultipleObjectsByHandleArray "
        "(this=%p, pthr=%p, rghHandlesToReference=%p, dwHandleCount=%d, "
        "pAllowedTyped=%d, dwRightsRequired=%d, rgpobjs=%p)\n",
        this,
        pthr,
        rghHandlesToReference,
        dwHandleCount,
        paot,
        dwRightsRequired,
        rgpobjs
        );

    m_HandleManager.Lock(pthr);

    for (dw = 0; dw < dwHandleCount; dw += 1)
    {        
        palError = m_HandleManager.GetObjectFromHandle(
            pthr,
            rghHandlesToReference[dw],
            &dwRightsGranted,
            &pobj
            );

        if (NO_ERROR == palError)
        {
            palError = CheckObjectTypeAndRights(
                pobj,
                paot,
                dwRightsGranted,
                dwRightsRequired
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

    LOGEXIT("CSharedMemoryObjectManager::ReferenceMultipleObjectsByHandleArray"
        " returns %d\n",
        palError
        );

    return palError;
}

/*++
Function:
  CSharedMemoryObjectManager::ReferenceObjectByForeignHandle

  Returns a referenced object instance that a handle belongin to
  another process refers to; currently unimplemented

Parameters:
  pthr -- thread data for calling thread
  hForeignHandle -- the handle to reference
  pForeignProcess -- the process that hForeignHandle belongs to
  paot -- acceptable types for the underlying object
  dwRightsRequired -- the access rights that the handle must have been
    granted; currently ignored
  ppobj -- on success, receives a reference to the object instance
--*/

PAL_ERROR
CSharedMemoryObjectManager::ReferenceObjectByForeignHandle(
    CPalThread *pthr,
    HANDLE hForeignHandle,
    IPalProcess *pForeignProcess,
    CAllowedObjectTypes *paot,
    DWORD dwRightsRequired,
    IPalObject **ppobj               // OUT
    )
{
    //
    // Not implemented for basic shared memory object manager --
    // requires an IPC channel. (For the shared memory object manager
    // PAL_LocalHandleToRemote and PAL_RemoteHandleToLocal must still
    // be used...)
    //

    ASSERT("ReferenceObjectByForeignHandle not yet supported\n");
    return ERROR_CALL_NOT_IMPLEMENTED;
}

/*++
Function:
  CSharedMemoryObjectManager::ImportSharedObjectIntoProcess

  Takes an object's shared memory data and from it creates the
  necessary in-process structures for the object

Parameters:
  pthr -- thread data for calling thread
  pot -- the object's type
  poa -- attributes for the object
  shmSharedObjectData -- the shared memory pointer for the object's shared
    data
  psmod -- the shared memory data for the object, mapped into this process's
    address space
  fAddRefSharedData -- if TRUE, we need to add to the shared data reference
    count
  ppshmobj -- on success, receives a pointer to the newly created local
    object instance
--*/

PAL_ERROR
CSharedMemoryObjectManager::ImportSharedObjectIntoProcess(
    CPalThread *pthr,
    CObjectType *pot,
    CObjectAttributes *poa,
    SHMPTR shmSharedObjectData,
    SHMObjData *psmod,
    bool fAddRefSharedData,
    CSharedMemoryObject **ppshmobj
    )
{
    PAL_ERROR palError = NO_ERROR;
    CSharedMemoryObject *pshmobj;
    PLIST_ENTRY pleObjectList;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != pot);
    _ASSERTE(NULL != poa);
    _ASSERTE(NULL != shmSharedObjectData);
    _ASSERTE(NULL != psmod);
    _ASSERTE(NULL != ppshmobj);

    ENTRY("CSharedMemoryObjectManager::ImportSharedObjectIntoProcess(pthr=%p, "
        "pot=%p, poa=%p, shmSharedObjectData=%p, psmod=%p, fAddRefSharedData=%d, "
        "ppshmobj=%p)\n",
        pthr,
        pot,
        poa,
        shmSharedObjectData,
        psmod,
        fAddRefSharedData,
        ppshmobj
        );
    
    if (CObjectType::WaitableObject == pot->GetSynchronizationSupport())
    {
        pshmobj = InternalNew<CSharedMemoryWaitableObject>(pot,
                                                           &m_csListLock,
                                                           shmSharedObjectData,
                                                           psmod,
                                                           fAddRefSharedData);
    }
    else
    {
        pshmobj = InternalNew<CSharedMemoryObject>(pot,
                                                   &m_csListLock,
                                                   shmSharedObjectData,
                                                   psmod,
                                                   fAddRefSharedData);
    }

    if (NULL != pshmobj)
    {
        palError = pshmobj->InitializeFromExistingSharedData(pthr, poa);
        if (NO_ERROR == palError)
        {
            if (0 != psmod->dwNameLength)
            {
                pleObjectList = &m_leNamedObjects;
            }
            else
            {
                pleObjectList = &m_leAnonymousObjects;
            }
            
            InsertTailList(pleObjectList, pshmobj->GetObjectListLink());
        }
        else
        {
            goto ImportSharedObjectIntoProcessExit;
        }
    }
    else
    {
        ERROR("Unable to alllocate new object\n");
        palError = ERROR_OUTOFMEMORY;
        goto ImportSharedObjectIntoProcessExit;
    }

    *ppshmobj = pshmobj;

ImportSharedObjectIntoProcessExit:

    LOGEXIT("CSharedMemoryObjectManager::ImportSharedObjectIntoProcess returns %d\n", palError);

    return palError;
}

static PalObjectTypeId RemotableObjectTypes[] =
    {otiManualResetEvent, otiAutoResetEvent, otiMutex, otiProcess};
    
static CAllowedObjectTypes aotRemotable(
    RemotableObjectTypes,
    sizeof(RemotableObjectTypes) / sizeof(RemotableObjectTypes[0])
    );

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
  dwRightsGranted -- the granted access rights (ignored)
  dwRightsRequired -- the required access rights (ignored)
--*/

static
PAL_ERROR
CheckObjectTypeAndRights(
    IPalObject *pobj,
    CAllowedObjectTypes *paot,
    DWORD dwRightsGranted,
    DWORD dwRightsRequired
    )
{
    PAL_ERROR palError = NO_ERROR;

    _ASSERTE(NULL != pobj);
    _ASSERTE(NULL != paot);

    ENTRY("CheckObjectTypeAndRights (pobj=%p, paot=%p, "
        "dwRightsGranted=%d, dwRightsRequired=%d)\n",
        pobj,
        paot,
        dwRightsGranted,
        dwRightsRequired
        );

    if (paot->IsTypeAllowed(pobj->GetObjectType()->GetId()))
    {
#ifdef ENFORCE_OBJECT_ACCESS_RIGHTS

        //
        // This is where the access right check would occur if Win32 object
        // security were supported.
        //
        
        if ((dwRightsRequired & dwRightsGranted) != dwRightsRequired)
        {
            palError = ERROR_ACCESS_DENIED;
        }
#endif
    }
    else
    {
        palError = ERROR_INVALID_HANDLE;
    }

    LOGEXIT("CheckObjectTypeAndRights returns %d\n", palError);

    return palError;
}
    

