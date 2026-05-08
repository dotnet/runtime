// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    listedobject.hpp

Abstract:
    Shared memory based object



--*/

#ifndef _PAL_SHMOBJECT_HPP
#define _PAL_SHMOBJECT_HPP

#include "palobjbase.hpp"

extern "C"
{
#include "pal/list.h"
}

namespace CorUnix
{
    class CListedObject : public CPalObjectBase
    {
    protected:

        //
        // Entry on the process's named or anonymous object list
        //

        LIST_ENTRY m_le;

        //
        // The lock that guards access to that list
        //

        minipal_mutex *m_pcsObjListLock;

        virtual
        void
        AcquireObjectDestructionLock(
            CPalThread *pthr
            );

        virtual
        void
        ReleaseObjectDestructionLock(
            CPalThread *pthr,
            bool fDestructionPending
            );

        virtual ~CListedObject();

    public:

        //
        // Constructor used for new object
        //

        CListedObject(
            CObjectType *pot,
            minipal_mutex *pcsObjListLock
            )
            :
            CPalObjectBase(pot),
            m_pcsObjListLock(pcsObjListLock)
        {
            InitializeListHead(&m_le);
        };

        virtual
        PAL_ERROR
        Initialize(
            CPalThread *pthr,
            CObjectAttributes *poa
            );

        void
        CleanupForProcessShutdown(
            CPalThread *pthr
            );

        PLIST_ENTRY
        GetObjectListLink(
            void
            )
        {
            return &m_le;
        }

        //
        // Clients of this object -- in particular, CListedObjectManager
        // -- can't use CONTAINING_RECORD directly, since they don't have
        // access to m_Link.
        //

        static
        CListedObject*
        GetObjectFromListLink(PLIST_ENTRY pLink);

        //
        // IPalObject routines
        //

        virtual
        PAL_ERROR
        GetSynchStateController(
            CPalThread *pthr,
            ISynchStateController **ppStateController
            );

        virtual
        PAL_ERROR
        GetSynchWaitController(
            CPalThread *pthr,
            ISynchWaitController **ppWaitController
            );

        virtual
        PAL_ERROR
        GetObjectSynchData(
            VOID **ppvSynchData
            );

    };

    class CSharedMemoryWaitableObject : public CListedObject
    {
    protected:

        VOID *m_pvSynchData;

        virtual ~CSharedMemoryWaitableObject();

    public:

        CSharedMemoryWaitableObject(
            CObjectType *pot,
            minipal_mutex *pcsObjListLock
            )
            :
            CListedObject(pot, pcsObjListLock)
        {
        };

        virtual
        PAL_ERROR
        Initialize(
            CPalThread *pthr,
            CObjectAttributes *poa
            );

        //
        // IPalObject routines
        //

        virtual
        PAL_ERROR
        GetSynchStateController(
            CPalThread *pthr,
            ISynchStateController **ppStateController
            );

        virtual
        PAL_ERROR
        GetSynchWaitController(
            CPalThread *pthr,
            ISynchWaitController **ppWaitController
            );

        virtual
        PAL_ERROR
        GetObjectSynchData(
            VOID **ppvSynchData
            );
    };

}

#endif // _PAL_SHMOBJECT_HPP

