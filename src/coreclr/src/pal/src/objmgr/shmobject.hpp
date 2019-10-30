// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    shmobject.hpp

Abstract:
    Shared memory based object



--*/

#ifndef _PAL_SHMOBJECT_HPP
#define _PAL_SHMOBJECT_HPP

#include "palobjbase.hpp"
#include "pal/shm.hpp"

extern "C"
{
#include "pal/list.h"
}

namespace CorUnix
{
    class CSimpleSharedMemoryLock : public IDataLock
    {
    public:

        void
        AcquireLock(
            CPalThread *pthr,
            IDataLock **ppDataLock
            )
        {
            SHMLock();
            *ppDataLock = static_cast<IDataLock*>(this);
        };

        virtual
        void
        ReleaseLock(
            CPalThread *pthr,
            bool fDataChanged
            )
        {
            SHMRelease();
        };
    };

    typedef struct _SHMObjData
    {
        SHMPTR shmPrevObj;
        SHMPTR shmNextObj;
        BOOL fAddedToList;
        
        SHMPTR shmObjName;
        SHMPTR shmObjImmutableData;
        SHMPTR shmObjSharedData;

        OBJECT_IMMUTABLE_DATA_COPY_ROUTINE pCopyRoutine;
        OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE pCleanupRoutine;

        LONG lProcessRefCount;
        DWORD dwNameLength;

        PalObjectTypeId eTypeId;

        PVOID pvSynchData;
    } SHMObjData;

    class CSharedMemoryObject : public CPalObjectBase
    {
        template <class T> friend void InternalDelete(T *p);
        
    protected:

        //
        // Entry on the process's named or anonymous object list
        //

        LIST_ENTRY m_le;

        //
        // The lock that guards access to that list
        //

        CRITICAL_SECTION *m_pcsObjListLock;

        //
        // The SHMObjData for this object, protected by the
        // shared memory lock.
        //
        
        SHMPTR m_shmod;

        //
        // The shared data (i.e., m_shmObjData->shmObjSharedData)
        // for this object, mapped into this process. This will be
        // NULL if m_pot->dwSharedDataSize is 0. Access to this data
        // is controlled by m_ssmlSharedData when m_ObjectDomain is
        // SharedObject, and m_sdlSharedData when it is ProcessLocalObject.
        //

        VOID *m_pvSharedData;
        
        CSimpleSharedMemoryLock m_ssmlSharedData;
        CSimpleDataLock m_sdlSharedData;

        //
        // Is this object process local or shared?
        //
        
        ObjectDomain m_ObjectDomain;

        //
        // m_fSharedDataDereferenced will be TRUE if DereferenceSharedData
        // has already been called. (N.B. -- this is a LONG instead of a bool
        // because it is passed to InterlockedExchange). If the shared data blob
        // should be freed in the object's destructor DereferenceSharedData will
        // set m_fDeleteSharedData to TRUE.
        //

        LONG m_fSharedDataDereferenced;
        LONG m_fDeleteSharedData;

        PAL_ERROR
        AllocateSharedDataItems(
            SHMPTR *pshmObjData,
            SHMObjData **ppsmod
            );

        static
        void
        FreeSharedDataAreas(
            SHMPTR shmObjData
            );

        bool
        DereferenceSharedData();

        virtual
        void
        AcquireObjectDestructionLock(
            CPalThread *pthr
            );

        virtual
        bool
        ReleaseObjectDestructionLock(
            CPalThread *pthr,
            bool fDestructionPending
            );
        
        virtual ~CSharedMemoryObject();

    public:

        //
        // Constructor used for new object
        //

        CSharedMemoryObject(
            CObjectType *pot,
            CRITICAL_SECTION *pcsObjListLock
            )
            :
            CPalObjectBase(pot),
            m_pcsObjListLock(pcsObjListLock),
            m_shmod(NULL),
            m_pvSharedData(NULL),
            m_ObjectDomain(ProcessLocalObject),
            m_fSharedDataDereferenced(FALSE),
            m_fDeleteSharedData(FALSE)
        {
            InitializeListHead(&m_le);
        };

        //
        // Constructor used to import a shared object into this process. The
        // shared memory lock must be held when calling this contstructor
        //

        CSharedMemoryObject(
            CObjectType *pot,
            CRITICAL_SECTION *pcsObjListLock,
            SHMPTR shmSharedObjectData,
            SHMObjData *psmod,
            bool fAddRefSharedData
            )
            :
            CPalObjectBase(pot),
            m_pcsObjListLock(pcsObjListLock),
            m_shmod(shmSharedObjectData),
            m_pvSharedData(NULL),
            m_ObjectDomain(SharedObject),
            m_fSharedDataDereferenced(FALSE),
            m_fDeleteSharedData(FALSE)
        {
            InitializeListHead(&m_le);
            if (fAddRefSharedData)
            {
                psmod->lProcessRefCount += 1;
            }
        };

        virtual
        PAL_ERROR
        Initialize(
            CPalThread *pthr,
            CObjectAttributes *poa
            );

        virtual
        PAL_ERROR
        InitializeFromExistingSharedData(
            CPalThread *pthr,
            CObjectAttributes *poa
            );

        void
        CleanupForProcessShutdown(
            CPalThread *pthr
            );

        SHMPTR
        GetShmObjData(
            void
            )
        {
            return m_shmod;
        };

        PLIST_ENTRY
        GetObjectListLink(
            void
            )
        {
            return &m_le;
        }

        //
        // Clients of this object -- in particular, CSharedMemoryObjectManager
        // -- can't use CONTAINING_RECORD directly, since they don't have
        // access to m_Link.
        //

        static
        CSharedMemoryObject*
        GetObjectFromListLink(PLIST_ENTRY pLink);

        //
        // IPalObject routines
        //
        
        virtual
        PAL_ERROR
        GetSharedData(
            CPalThread *pthr,
            LockType eLockRequest,
            IDataLock **ppDataLock,
            void **ppvSharedData
            );

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
        ObjectDomain
        GetObjectDomain(
            void
            );

        virtual
        PAL_ERROR
        GetObjectSynchData(
            VOID **ppvSynchData
            );

    };

    class CSharedMemoryWaitableObject : public CSharedMemoryObject
    {
        template <class T> friend void InternalDelete(T *p);
        
    protected:

        VOID *m_pvSynchData;

        virtual ~CSharedMemoryWaitableObject();
        
    public:

        CSharedMemoryWaitableObject(
            CObjectType *pot,
            CRITICAL_SECTION *pcsObjListLock
            )
            :
            CSharedMemoryObject(pot, pcsObjListLock),
            m_pvSynchData(NULL)
        {
        };

        //
        // Constructor used to import a shared object into this process. The
        // shared memory lock must be held when calling this contstructor
        //

        CSharedMemoryWaitableObject(
            CObjectType *pot,
            CRITICAL_SECTION *pcsObjListLock,
            SHMPTR shmSharedObjectData,
            SHMObjData *psmod,
            bool fAddRefSharedData
            )
            :
            CSharedMemoryObject(pot, pcsObjListLock, shmSharedObjectData, psmod, fAddRefSharedData),
            m_pvSynchData(psmod->pvSynchData)
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

