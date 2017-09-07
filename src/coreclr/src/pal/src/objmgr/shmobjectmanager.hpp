// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    shmobjectmanager.hpp

Abstract:
    Shared memory based object manager



--*/

#ifndef _PAL_SHMOBJECTMANAGER_HPP_
#define _PAL_SHMOBJECTMANAGER_HPP_

#include "pal/corunix.hpp"
#include "pal/handlemgr.hpp"
#include "pal/list.h"    
#include "shmobject.hpp"

namespace CorUnix
{
    class CSharedMemoryObjectManager : public IPalObjectManager
    {
    protected:

        CRITICAL_SECTION m_csListLock;
        bool m_fListLockInitialized;
        LIST_ENTRY m_leNamedObjects;
        LIST_ENTRY m_leAnonymousObjects;
        
        CSimpleHandleManager m_HandleManager;

        PAL_ERROR
        ImportSharedObjectIntoProcess(
            CPalThread *pthr,
            CObjectType *pot,
            CObjectAttributes *poa,
            SHMPTR shmSharedObjectData,
            SHMObjData *psmod,
            bool fAddRefSharedData,
            CSharedMemoryObject **ppshmobj
            );
        
    public:

        CSharedMemoryObjectManager()
            :
            m_fListLockInitialized(FALSE)
        {
        };

        virtual ~CSharedMemoryObjectManager()
        {
        };

        PAL_ERROR
        Initialize(
            void
            );

        PAL_ERROR
        Shutdown(
            CPalThread *pthr
            );

        //
        // IPalObjectManager routines
        //
        
        virtual
        PAL_ERROR
        AllocateObject(
            CPalThread *pthr,
            CObjectType *pot,
            CObjectAttributes *poa,
            IPalObject **ppobjNew
            );

        virtual
        PAL_ERROR
        RegisterObject(
            CPalThread *pthr,
            IPalObject *pobjToRegister,
            CAllowedObjectTypes *paot,
            DWORD dwRightsRequested,
            HANDLE *pHandle,
            IPalObject **ppobjRegistered
            );

        virtual            
        PAL_ERROR
        LocateObject(
            CPalThread *pthr,
            CPalString *psObjectToLocate,
            CAllowedObjectTypes *paot,
            IPalObject **ppobj
            );

        virtual
        PAL_ERROR   
        ObtainHandleForObject(
            CPalThread *pthr,
            IPalObject *pobj,
            DWORD dwRightsRequested,
            bool fInheritHandle,
            IPalProcess *pProcessForHandle,     // IN, OPTIONAL
            HANDLE *pNewHandle
            );

        virtual
        PAL_ERROR
        RevokeHandle(
            CPalThread *pthr,
            HANDLE hHandleToRevoke
            );

        virtual
        PAL_ERROR
        ReferenceObjectByHandle(
            CPalThread *pthr,
            HANDLE hHandleToReference,
            CAllowedObjectTypes *paot,
            DWORD dwRightsRequired,
            IPalObject **ppobj
            );

        virtual
        PAL_ERROR
        ReferenceMultipleObjectsByHandleArray(
            CPalThread *pthr,
            HANDLE rghHandlesToReference[],
            DWORD dwHandleCount,
            CAllowedObjectTypes *paot,
            DWORD dwRightsRequired,
            IPalObject *rgpobjs[]
            );

        virtual
        PAL_ERROR
        ReferenceObjectByForeignHandle(
            CPalThread *pthr,
            HANDLE hForeignHandle,
            IPalProcess *pForeignProcess,
            CAllowedObjectTypes *paot,
            DWORD dwRightsRequired,
            IPalObject **ppobj
            );
    };
}

#endif // _PAL_SHMOBJECTMANAGER_HPP_

