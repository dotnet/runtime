// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    corunix.hpp

Abstract:

    Internal interface and object definitions



--*/

#ifndef _CORUNIX_H
#define _CORUNIX_H

#include "palinternal.h"

namespace CorUnix
{
    typedef DWORD PAL_ERROR;

    //
    // Forward declarations for classes defined in other headers
    //

    class CPalThread;

    //
    // Forward declarations for items in this header
    //

    class CObjectType;
    class IPalObject;

    //
    // A simple counted string class. Using counted strings
    // allows for some optimizations when searching for a matching string.
    //

    class CPalString
    {
    protected:

        const WCHAR *m_pwsz;    // NULL terminated

        //
        // Length of string, not including terminating NULL
        //
        
        DWORD m_dwStringLength; 

        //
        // Length of buffer backing string; must be at least 1+dwStringLength
        //

        DWORD m_dwMaxLength;

    public:

        CPalString()
            :
            m_pwsz(NULL),
            m_dwStringLength(0),
            m_dwMaxLength(0)
        {
        };
            
        CPalString(
            const WCHAR *pwsz
            )
        {
            SetString(pwsz);
        };

        void
        SetString(
            const WCHAR *pwsz
            )
        {
            SetStringWithLength(pwsz, PAL_wcslen(pwsz));
        };

        void
        SetStringWithLength(
            const WCHAR *pwsz,
            DWORD dwStringLength
            )
        {
            m_pwsz = pwsz;
            m_dwStringLength = dwStringLength;
            m_dwMaxLength = m_dwStringLength + 1;

        };

        PAL_ERROR
        CopyString(
            CPalString *psSource
            );

        void
        FreeBuffer();

        const WCHAR *
        GetString()
        {
            return m_pwsz;
        };

        DWORD
        GetStringLength()
        {
            return m_dwStringLength;
        };

        DWORD
        GetMaxLength()
        {
            return m_dwMaxLength;
        };
        
    };

    //
    // Signature of the cleanup routine that is to be called for an object
    // type when:
    // 1) The object's refcount drops to 0
    // 2) A process is shutting down
    // 3) A process has released all local references to the object
    //
    // The cleanup routine must only cleanup the object's shared state
    // when the last parameter (fCleanupSharedSate) is TRUE. When
    // fCleanupSharedState is FALSE the cleanup routine must not attempt
    // to access the shared data for the object, as another process may
    // have already deleted it. ($$REIVEW -- would someone ever need access
    // to the shared data in order to cleanup process local state?)
    //
    // When the third paramter (fShutdown) is TRUE the process is in
    // the act of exiting. The cleanup routine should not perform any
    // unnecessary cleanup operations (e.g., closing file descriptors,
    // since the OS will automatically close them when the process exits)
    // in this situation.
    //

    typedef void (*OBJECTCLEANUPROUTINE) (
        CPalThread *,   // pThread
        IPalObject *,   // pObjectToCleanup
        bool,           // fShutdown
        bool            // fCleanupSharedState
        );

    //
    // Signature of the initialization routine that is to be called
    // when the first reference within a process to an existing
    // object comes into existence. This routine is responsible for
    // initializing the object's process local data, based on the
    // immutable and shared data. The thread that this routine is
    // called on holds an implicit read lock on the shared data.
    //

    typedef PAL_ERROR (*OBJECTINITROUTINE) (
        CPalThread *,   // pThread
        CObjectType *,  // pObjectType
        void *,         // pImmutableData
        void *,         // pSharedData
        void *          // pProcessLocalData
        );

    enum PalObjectTypeId
    {
        otiAutoResetEvent = 0,
        otiManualResetEvent,
        otiMutex,
        otiNamedMutex,
        otiSemaphore,
        otiFile,
        otiFileMapping,
        otiSocket,
        otiProcess,
        otiThread,
        otiIOCompletionPort,
        ObjectTypeIdCount    // This entry must come last in the enumeration
    };

    //
    // There should be one instance of CObjectType for each supported
    // type in a process; this allows for pointer equality tests
    // to be used (though in general it's probably better to use
    // checks based on the type ID). All members of this structure are
    // immutable.
    //
    // The data size members control how much space will be allocated for
    // instances of this object. Any or all of those members may be 0.
    //
    // dwSupportedAccessRights is the mask of valid access bits for this
    // object type. Supported generic rights should not be included in
    // this member.
    //
    // The generic access rights mapping (structure TBD) defines how the
    // supported generic access rights (e.g., GENERIC_READ) map to the
    // specific access rights for this object type.
    //
    // If instances of this object may have a security descriptor set on
    // them eSecuritySupport should be set to SecuritySupported. If the OS can
    // persist security information for the object type (as would be the case
    // for, say, files) eSecurityPersistence should be set to
    // OSPersistedSecurityInfo.
    // 
    // If the object may have a name eObjectNameSupport should be
    // ObjectCanHaveName. A named object can be opened in more than one
    // process.
    //
    // If it is possible to duplicate a handle to an object across process
    // boundaries then eHandleDuplicationSupport should be set to
    // CrossProcessDuplicationAllowed. Note that it is possible to have
    // an object type where eObjectNameSupport is ObjectCanHaveName and
    // eHandleDuplicationSupport is LocalDuplicationOnly. For these object
    // types an unnamed object instance will only have references from
    // the creating process.
    //
    // If the object may be waited on eSynchronizationSupport should be
    // WaitableObject. (Note that this implies that object type supports
    // the SYNCHRONIZE access right.)
    //
    // The remaining members describe the wait-object semantics for the
    // object type when eSynchronizationSupport is WaitableObject:
    //
    // * eSignalingSemantics: SingleTransitionObject for objects that, once
    //   they transition to the signaled state, can never transition back to
    //   the unsignaled state (e.g., processes and threads)
    //
    // * eThreadReleaseSemantics: if ThreadReleaseAltersSignalCount the object's
    //   signal count is decremented when a waiting thread is released; otherwise,
    //   the signal count is not modified (as is desired for a manual reset event).
    //   Must be ThreadReleaseHasNoSideEffects if eSignalingSemantics is
    //   SingleTransitionObject
    //
    // * eOwnershipSemantics: OwnershipTracked only for mutexes, for which the
    //   previous two items must also ObjectCanBeUnsignaled and
    //   ThreadReleaseAltersSignalCount.
    //

    class CObjectType
    {
    public:

        enum SecuritySupport
        {
            SecuritySupported,
            SecurityNotSupported
        };

        enum SecurityPersistence
        {
            OSPersistedSecurityInfo,
            SecurityInfoNotPersisted
        };

        enum ObjectNameSupport
        {
            ObjectCanHaveName,
            UnnamedObject
        };

        enum HandleDuplicationSupport
        {
            CrossProcessDuplicationAllowed,
            LocalDuplicationOnly
        };

        enum SynchronizationSupport
        {
            WaitableObject,
            UnwaitableObject
        };

        enum SignalingSemantics
        {
            ObjectCanBeUnsignaled,
            SingleTransitionObject,
            SignalingNotApplicable
        };

        enum ThreadReleaseSemantics
        {
            ThreadReleaseAltersSignalCount,
            ThreadReleaseHasNoSideEffects,
            ThreadReleaseNotApplicable
        };

        enum OwnershipSemantics
        {
            OwnershipTracked,
            NoOwner,
            OwnershipNotApplicable
        };
        
    private:

        //
        // Array that maps object type IDs to the corresponding
        // CObjectType instance
        //
        
        static CObjectType* s_rgotIdMapping[];

        PalObjectTypeId m_eTypeId;
        OBJECTCLEANUPROUTINE m_pCleanupRoutine;
        OBJECTINITROUTINE m_pInitRoutine;
        DWORD m_dwImmutableDataSize;
        DWORD m_dwProcessLocalDataSize;
        DWORD m_dwSharedDataSize;
        DWORD m_dwSupportedAccessRights;
        // Generic access rights mapping
        SecuritySupport m_eSecuritySupport;
        SecurityPersistence m_eSecurityPersistence;
        ObjectNameSupport m_eObjectNameSupport;
        HandleDuplicationSupport m_eHandleDuplicationSupport;
        SynchronizationSupport m_eSynchronizationSupport;
        SignalingSemantics m_eSignalingSemantics;
        ThreadReleaseSemantics m_eThreadReleaseSemantics;
        OwnershipSemantics m_eOwnershipSemantics;

    public:

        CObjectType(
            PalObjectTypeId eTypeId,
            OBJECTCLEANUPROUTINE pCleanupRoutine,
            OBJECTINITROUTINE pInitRoutine,
            DWORD dwImmutableDataSize,
            DWORD dwProcessLocalDataSize,
            DWORD dwSharedDataSize,
            DWORD dwSupportedAccessRights,
            SecuritySupport eSecuritySupport,
            SecurityPersistence eSecurityPersistence,
            ObjectNameSupport eObjectNameSupport,
            HandleDuplicationSupport eHandleDuplicationSupport,
            SynchronizationSupport eSynchronizationSupport,
            SignalingSemantics eSignalingSemantics,
            ThreadReleaseSemantics eThreadReleaseSemantics,
            OwnershipSemantics eOwnershipSemantics
            )
            :
            m_eTypeId(eTypeId),
            m_pCleanupRoutine(pCleanupRoutine),
            m_pInitRoutine(pInitRoutine),
            m_dwImmutableDataSize(dwImmutableDataSize),
            m_dwProcessLocalDataSize(dwProcessLocalDataSize),
            m_dwSharedDataSize(dwSharedDataSize),
            m_dwSupportedAccessRights(dwSupportedAccessRights),
            m_eSecuritySupport(eSecuritySupport),
            m_eSecurityPersistence(eSecurityPersistence),
            m_eObjectNameSupport(eObjectNameSupport),
            m_eHandleDuplicationSupport(eHandleDuplicationSupport),
            m_eSynchronizationSupport(eSynchronizationSupport),
            m_eSignalingSemantics(eSignalingSemantics),
            m_eThreadReleaseSemantics(eThreadReleaseSemantics),
            m_eOwnershipSemantics(eOwnershipSemantics)
        {
            s_rgotIdMapping[eTypeId] = this;
        };

        static
        CObjectType *
        GetObjectTypeById(
            PalObjectTypeId otid
            )
        {
            return s_rgotIdMapping[otid];
        };

        PalObjectTypeId
        GetId(
            void
            )
        {
            return m_eTypeId;
        };
        
        OBJECTCLEANUPROUTINE
        GetObjectCleanupRoutine(
            void
            )
        {
            return m_pCleanupRoutine;
        };
        
        OBJECTINITROUTINE
        GetObjectInitRoutine(
            void
            )
        {
            return  m_pInitRoutine;
        };
        
        DWORD
        GetImmutableDataSize(
            void
            )
        {
            return  m_dwImmutableDataSize;
        };
        
        DWORD
        GetProcessLocalDataSize(
            void
            )
        {
            return m_dwProcessLocalDataSize;
        };
        
        DWORD
        GetSharedDataSize(
            void
            )
        {
            return m_dwSharedDataSize;
        };
        
        DWORD
        GetSupportedAccessRights(
            void
            )
        {
            return m_dwSupportedAccessRights;
        };
        
        // Generic access rights mapping

        SecuritySupport
        GetSecuritySupport(
            void
            )
        {
            return  m_eSecuritySupport;
        };
        
        SecurityPersistence
        GetSecurityPersistence(
            void
            )
        {
            return  m_eSecurityPersistence;
        };
        
        ObjectNameSupport
        GetObjectNameSupport(
            void
            )
        {
            return  m_eObjectNameSupport;
        };
        
        HandleDuplicationSupport
        GetHandleDuplicationSupport(
            void
            )
        {
            return  m_eHandleDuplicationSupport;
        };
        
        SynchronizationSupport
        GetSynchronizationSupport(
            void
            )
        {
            return  m_eSynchronizationSupport;
        };
        
        SignalingSemantics
        GetSignalingSemantics(
            void
            )
        {
            return  m_eSignalingSemantics;
        };
        
        ThreadReleaseSemantics
        GetThreadReleaseSemantics(
            void
            )
        {
            return  m_eThreadReleaseSemantics;
        };
        
        OwnershipSemantics
        GetOwnershipSemantics(
            void
            )
        {
            return  m_eOwnershipSemantics;
        };
    };

    class CAllowedObjectTypes
    {
    private:

        bool m_rgfAllowedTypes[ObjectTypeIdCount];

    public:

        bool
        IsTypeAllowed(PalObjectTypeId eTypeId);

        //
        // Constructor for multiple allowed types
        //

        CAllowedObjectTypes(
            PalObjectTypeId rgAllowedTypes[],
            DWORD dwAllowedTypeCount
            );

        //
        // Single allowed type constructor
        //

        CAllowedObjectTypes(
            PalObjectTypeId eAllowedType
            );

        //
        // Allow all types or no types constructor
        //

        CAllowedObjectTypes(
            bool fAllowAllObjectTypes
            )
        {
            for (DWORD dw = 0; dw < ObjectTypeIdCount; dw += 1)
            {
                m_rgfAllowedTypes[dw] = fAllowAllObjectTypes;
            }
        };

        ~CAllowedObjectTypes()
        {
        };
    };

    //
    // Attributes for a given object instance. If the object does not have
    // a name the sObjectName member should be zero'd out. If the default
    // security attributes are desired then pSecurityAttributes should
    // be NULL.
    //

    class CObjectAttributes
    {
    public:
        
        CPalString sObjectName;
        LPSECURITY_ATTRIBUTES pSecurityAttributes;

        CObjectAttributes(
            const WCHAR *pwszObjectName,
            LPSECURITY_ATTRIBUTES pSecurityAttributes_
            )
            :
            pSecurityAttributes(pSecurityAttributes_)
        {
            if (NULL != pwszObjectName)
            {
                sObjectName.SetString(pwszObjectName);
            }
        };

        CObjectAttributes()
            :
            pSecurityAttributes(NULL)
        {
        };
    };

    //
    // ISynchStateController is used to modify any object's synchronization
    // state. It is intended to be used from within the APIs exposed for
    // various objects (e.g., SetEvent, ReleaseMutex, etc.).
    //
    // Each ISynchStateController instance implicitly holds what should be
    // viewed as the global dispatcher lock, and as such should be released
    // as quickly as possible. An ISynchStateController instance is bound to
    // the thread that requested it; it may not be passed to a different
    // thread.
    //

    class ISynchStateController
    {
    public:

        virtual
        PAL_ERROR
        GetSignalCount(
            LONG *plSignalCount
            ) = 0;

        virtual
        PAL_ERROR
        SetSignalCount(
            LONG lNewCount
            ) = 0;

        virtual
        PAL_ERROR
        IncrementSignalCount(
            LONG lAmountToIncrement
            ) = 0;

        virtual
        PAL_ERROR
        DecrementSignalCount(
            LONG lAmountToDecrement
            ) = 0;

        //
        // The following two routines may only be used for object types
        // where eOwnershipSemantics is OwnershipTracked (i.e., mutexes).
        //

        //
        // SetOwner is intended to be used in the implementation of
        // CreateMutex when bInitialOwner is TRUE. It must be called
        // before the new object instance is registered with the
        // handle manager. Any other call to this method is an error.
        //

        virtual
        PAL_ERROR
        SetOwner(
            CPalThread *pNewOwningThread
            ) = 0;

        //
        // DecrementOwnershipCount returns an error if the object
        // is unowned, or if the thread this controller is bound to
        // is not the owner of the object.
        //

        virtual
        PAL_ERROR
        DecrementOwnershipCount(
            void
            ) = 0;

        virtual
        void
        ReleaseController(
            void
            ) = 0;
    };

    //
    // ISynchWaitController is used to indicate a thread's desire to wait for
    // an object (which possibly includes detecting instances where the wait
    // can be satisfied without blocking). It is intended to be used by object
    // wait function (WaitForSingleObject, etc.).
    //
    // Each ISynchWaitController instance implicitly holds what should be
    // viewed as the global dispatcher lock, and as such should be released
    // as quickly as possible. An ISynchWaitController instance is bound to
    // the thread that requested it; it may not be passed to a different
    // thread.
    //
    // A thread may hold multiple ISynchWaitController instances
    // simultaneously.
    //

    enum WaitType
    {
        SingleObject,
        MultipleObjectsWaitOne,
        MultipleObjectsWaitAll
    };

    class ISynchWaitController
    {
    public:

        //
        // CanThreadWaitWithoutBlocking informs the caller if a wait
        // operation may succeed immediately, but does not actually
        // alter any object state. ReleaseWaitingThreadWithoutBlocking
        // alters the object state, and will return an error if it is
        // not possible for the wait to be immediately satisfied.
        //

        virtual        
        PAL_ERROR
        CanThreadWaitWithoutBlocking(
            bool *pfCanWaitWithoutBlocking,     // OUT
            bool *pfAbandoned
            ) = 0;

        virtual
        PAL_ERROR
        ReleaseWaitingThreadWithoutBlocking(
            ) = 0;

        //
        // dwIndex is intended for MultipleObjectsWaitOne situations. The
        // index for the object that becomes signaled and satisfies the
        // wait will be returned in the call to BlockThread.
        //

        virtual
        PAL_ERROR
        RegisterWaitingThread(
            WaitType eWaitType,
            DWORD dwIndex,
            bool fAltertable
            ) = 0;

        //
        // Why is there no unregister waiting thread routine? Unregistration
        // is the responsibility of the synchronization provider, not the
        // implementation of the wait object routines. (I can be convinced
        // that this isn't the best approach, though...)
        //

        virtual
        void
        ReleaseController(
            void
            ) = 0;
    };

    enum LockType
    {
        ReadLock,
        WriteLock
    };

    class IDataLock
    {
    public:

        //
        // If a thread obtains a write lock but does not actually
        // modify any data it should set fDataChanged to FALSE. If
        // a thread obtain a read lock and does actually modify any
        // data it should be taken out back and shot.
        //

        virtual
        void
        ReleaseLock(
            CPalThread *pThread,                // IN, OPTIONAL
            bool fDataChanged
            ) = 0;
    };

    //
    // The following two enums are part of the local object
    // optimizations 
    //

    enum ObjectDomain
    {
        ProcessLocalObject,
        SharedObject
    };

    enum WaitDomain
    {
        LocalWait,      // All objects in the wait set are local to this process
        MixedWait,      // Some objects are local; some are shared
        SharedWait      // All objects in the wait set are shared
    };

    class IPalObject
    {
    public:

        virtual
        CObjectType *
        GetObjectType(
            VOID
            ) = 0;

        virtual
        CObjectAttributes *
        GetObjectAttributes(
            VOID
            ) = 0;

        virtual
        PAL_ERROR
        GetImmutableData(
            void **ppvImmutableData             // OUT
            ) = 0;

        //
        // The following two routines obtain either a read or write
        // lock on the data in question. If a thread needs to examine
        // both process-local and shared data simultaneously it must obtain
        // the shared data first. A thread may not hold data locks
        // on two different objects at the same time.
        //

        virtual
        PAL_ERROR
        GetProcessLocalData(
            CPalThread *pThread,                // IN, OPTIONAL
            LockType eLockRequest,
            IDataLock **ppDataLock,             // OUT
            void **ppvProcessLocalData          // OUT
            ) = 0;

        virtual
        PAL_ERROR
        GetSharedData(
            CPalThread *pThread,                // IN, OPTIONAL
            LockType eLockRequest,
            IDataLock **ppDataLock,             // OUT
            void **ppvSharedData                // OUT
            ) = 0;

        //
        // The following two routines obtain the global dispatcher lock.
        // If a thread needs to make use of a synchronization interface
        // and examine object data it must obtain the synchronization
        // interface first. A thread is allowed to hold synchronization
        // interfaces for multiple objects at the same time if it obtains
        // all of the interfaces through a single call (see IPalSynchronizationManager
        // below).
        //
        // The single-call restriction allows the underlying implementation
        // to possibly segement the global dispatcher lock. If this restriction
        // were not in place (i.e., if a single thread were allowed to call
        // GetSynchXXXController for multiple objects) no such segmentation
        // would be possible as there would be no way know in what order a
        // thread would choose to obtain the controllers.
        //
        // Note: this design precludes simultaneous acquisition of both
        // the state and wait controller for an object but there are
        // currently no places where doing so would be necessary.
        //

        virtual
        PAL_ERROR
        GetSynchStateController(
            CPalThread *pThread,                // IN, OPTIONAL
            ISynchStateController **ppStateController   // OUT
            ) = 0;

        virtual
        PAL_ERROR
        GetSynchWaitController(
            CPalThread *pThread,                // IN, OPTIONAL
            ISynchWaitController **ppWaitController   // OUT
            ) = 0;

        virtual
        DWORD
        AddReference(
            void
            ) = 0;

        virtual
        DWORD
        ReleaseReference(
            CPalThread *pThread
            ) = 0;

        //
        // This routine is mainly intended for the synchronization
        // manager. The promotion / process synch lock must be held
        // before calling this routine.
        //

        virtual
        ObjectDomain
        GetObjectDomain(
            void
            ) = 0;

        //
        // This routine is only for use by the synchronization manager
        // (specifically, for GetSynch*ControllersForObjects). The
        // caller must have acquired the appropriate lock before
        // (whatever exactly that must be) before calling this routine.
        //

        virtual
        PAL_ERROR
        GetObjectSynchData(
            VOID **ppvSynchData             // OUT
            ) = 0;
        
    };

    class IPalProcess
    {
    public:
        virtual
        DWORD
        GetProcessID(
            void
            ) = 0;
    };
    
    class IPalObjectManager
    {
    public:

        //
        // Object creation (e.g., what is done by CreateEvent) is a two step
        // process. First, the new object is allocated and the initial
        // properties set (e.g., initially signaled). Next, the object is
        // registered, yielding a handle. If an object of the same name
        // and appropriate type already existed the returned handle will refer
        // to the previously existing object, and the newly allocated object
        // will have been thrown away.
        //
        // (The two phase process minimizes the amount of time that any
        // namespace locks need to be held. While some wasted work may be
        // done in the existing object case that work only impacts the calling
        // thread. Checking first for existence and then allocating and
        // initializing on failure requires any namespace lock to be held for
        // a much longer period of time, impacting the entire system.)
        //

        virtual
        PAL_ERROR
        AllocateObject(
            CPalThread *pThread,                // IN, OPTIONAL
            CObjectType *pType,
            CObjectAttributes *pAttributes,
            IPalObject **ppNewObject            // OUT
            ) = 0;

        //
        // After calling RegisterObject pObjectToRegister is no
        // longer valid. If successful there are two references
        // on the returned object -- one for the handle, and one
        // for the instance returned in ppRegisteredObject. The
        // caller, therefore, is responsible for releasing the
        // latter.
        //
        // For named object pAllowedTypes specifies what type of
        // existing objects can be returned in ppRegisteredObjects.
        // This is primarily intended for CreateEvent, so that
        // a ManualResetEvent can be returned when attempting to
        // register an AutoResetEvent (and vice-versa). pAllowedTypes
        // must include the type of pObjectToRegister.
        //

        virtual
        PAL_ERROR
        RegisterObject(
            CPalThread *pThread,                // IN, OPTIONAL
            IPalObject *pObjectToRegister,
            CAllowedObjectTypes *pAllowedTypes,
            DWORD dwRightsRequested,
            HANDLE *pHandle,                    // OUT
            IPalObject **ppRegisteredObject     // OUT
            ) = 0;

        //
        // LocateObject is used for OpenXXX routines. ObtainHandleForObject
        // is needed for the OpenXXX routines and DuplicateHandle.
        //

        virtual            
        PAL_ERROR
        LocateObject(
            CPalThread *pThread,                // IN, OPTIONAL
            CPalString *psObjectToLocate,
            CAllowedObjectTypes *pAllowedTypes,
            IPalObject **ppObject               // OUT
            ) = 0;

        //
        // pProcessForHandle is to support cross-process handle
        // duplication. It only needs to be specified when acquiring
        // a handle meant for use in a different process; it should
        // be left NULL when acquiring a handle for the current
        // process.
        //

        virtual
        PAL_ERROR   
        ObtainHandleForObject(
            CPalThread *pThread,                // IN, OPTIONAL
            IPalObject *pObject,
            DWORD dwRightsRequested,
            bool fInheritHandle,
            IPalProcess *pProcessForHandle,     // IN, OPTIONAL
            HANDLE *pNewHandle                  // OUT
            ) = 0;

        virtual
        PAL_ERROR
        RevokeHandle(
            CPalThread *pThread,                // IN, OPTIONAL
            HANDLE hHandleToRevoke
            ) = 0;

        //
        // The Reference routines are called to obtain the
        // object that a handle refers to. The caller must
        // specify the rights that the handle must hold for
        // the operation that it is about to perform. The caller
        // is responsible for converting generic rights to specific
        // rights. The caller must also specify what object types
        // are permissible for the object.
        //
        // The returned object[s], on success, are referenced,
        // and the caller is responsible for releasing those references
        // when appropriate.
        //

        virtual
        PAL_ERROR
        ReferenceObjectByHandle(
            CPalThread *pThread,                // IN, OPTIONAL
            HANDLE hHandleToReference,
            CAllowedObjectTypes *pAllowedTypes,
            DWORD dwRightsRequired,
            IPalObject **ppObject               // OUT
            ) = 0;

        //
        // This routine is intended for WaitForMultipleObjects[Ex]
        //

        virtual
        PAL_ERROR
        ReferenceMultipleObjectsByHandleArray(
            CPalThread *pThread,                // IN, OPTIONAL
            HANDLE rghHandlesToReference[],
            DWORD dwHandleCount,
            CAllowedObjectTypes *pAllowedTypes,
            DWORD dwRightsRequired,
            IPalObject *rgpObjects[]            // OUT
            ) = 0;

        //
        // This routine is for cross-process handle duplication.
        //

        virtual
        PAL_ERROR
        ReferenceObjectByForeignHandle(
            CPalThread *pThread,                // IN, OPTIONAL
            HANDLE hForeignHandle,
            IPalProcess *pForeignProcess,
            CAllowedObjectTypes *pAllowedTypes,
            DWORD dwRightsRequired,
            IPalObject **ppObject               // OUT
            ) = 0;
        
    };

    extern IPalObjectManager *g_pObjectManager;

    enum ThreadWakeupReason
    {
        WaitSucceeded,
        Alerted,
        MutexAbondoned,
        WaitTimeout,
        WaitFailed
    };

    class IPalSynchronizationManager
    {
    public:

        //
        // A thread calls BlockThread to put itself to sleep after it has
        // registered itself with the objects it is to wait on. A thread
        // need not have registered with any objects, as would occur in
        // the implementation of Sleep[Ex].
        //
        // Needless to say a thread must not be holding any PAL locks
        // directly or implicitly (e.g., by holding a reference to a
        // synchronization controller) when it calls this method.
        //

        virtual
        PAL_ERROR
        BlockThread(
            CPalThread *pCurrentThread,
            DWORD dwTimeout,
            bool fAlertable,
            bool fIsSleep,
            ThreadWakeupReason *peWakeupReason, // OUT
            DWORD *pdwSignaledObject       // OUT
            ) = 0;

        virtual
        PAL_ERROR
        AbandonObjectsOwnedByThread(
            CPalThread *pCallingThread,
            CPalThread *pTargetThread
            ) = 0;

        virtual
        PAL_ERROR
        QueueUserAPC(
            CPalThread *pThread,
            CPalThread *pTargetThread,
            PAPCFUNC pfnAPC,
            ULONG_PTR dwData
            ) = 0;

        virtual
        bool
        AreAPCsPending(
            CPalThread *pThread
            ) = 0;

        virtual
        PAL_ERROR
        DispatchPendingAPCs(
            CPalThread *pThread
            ) = 0;

        virtual
        PAL_ERROR
        SendTerminationRequestToWorkerThread() = 0;

        //
        // This routine is primarily meant for use by WaitForMultipleObjects[Ex].
        // The caller must individually release each of the returned controller
        // interfaces.
        //

        virtual
        PAL_ERROR
        GetSynchWaitControllersForObjects(
            CPalThread *pThread,
            IPalObject *rgObjects[],
            DWORD dwObjectCount,
            ISynchWaitController *rgControllers[]
            ) = 0;

        virtual
        PAL_ERROR
        GetSynchStateControllersForObjects(
            CPalThread *pThread,
            IPalObject *rgObjects[],
            DWORD dwObjectCount,
            ISynchStateController *rgControllers[]
            ) = 0;

        //
        // These following routines are meant for use only by IPalObject
        // implementations. The first two routines are used to
        // allocate and free an object's synchronization state; the third
        // is called during object promotion.
        //

        virtual
        PAL_ERROR
        AllocateObjectSynchData(
            CObjectType *pObjectType,
            ObjectDomain eObjectDomain,
            VOID **ppvSynchData                 // OUT
            ) = 0;

        virtual
        void
        FreeObjectSynchData(
            CObjectType *pObjectType,
            ObjectDomain eObjectDomain,
            VOID *pvSynchData
            ) = 0;

        virtual
        PAL_ERROR
        PromoteObjectSynchData(
            CPalThread *pThread,
            VOID *pvLocalSynchData,
            VOID **ppvSharedSynchData           // OUT
            ) = 0;

        //
        // The next two routines provide access to the process-wide
        // synchronization lock
        //

        virtual
        void
        AcquireProcessLock(
            CPalThread *pThread
            ) = 0;

        virtual
        void
        ReleaseProcessLock(
            CPalThread *pThread
            ) = 0;

        //
        // The final routines are used by IPalObject::GetSynchStateController
        // and IPalObject::GetSynchWaitController
        //

        virtual
        PAL_ERROR
        CreateSynchStateController(
            CPalThread *pThread,                // IN, OPTIONAL
            CObjectType *pObjectType,
            VOID *pvSynchData,
            ObjectDomain eObjectDomain,
            ISynchStateController **ppStateController       // OUT
            ) = 0;

        virtual
        PAL_ERROR
        CreateSynchWaitController(
            CPalThread *pThread,                // IN, OPTIONAL
            CObjectType *pObjectType,
            VOID *pvSynchData,
            ObjectDomain eObjectDomain,
            ISynchWaitController **ppWaitController       // OUT
            ) = 0;
    };

    extern IPalSynchronizationManager *g_pSynchronizationManager;

    class IFileTransactionLock
    {
    public:

        //
        // Called when the transaction completes (which includes
        // error completions, or the outright failure to queue
        // the transaction).
        //

        virtual
        void
        ReleaseLock() = 0;
    };

    class IFileLockController
    {
    public:

        //
        // A transaction lock is acquired before a read or write
        // operation, and released when that operation completes.
        // The lock is not tied to the calling thread, since w/
        // asynch file IO the completion may occur on a different
        // thread.
        //

        enum FileTransactionLockType
        {
            ReadLock,
            WriteLock
        };

        virtual
        PAL_ERROR
        GetTransactionLock(
            CPalThread *pThread,                // IN, OPTIONAL
            FileTransactionLockType eLockType,
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToLockLow,
            DWORD nNumberOfBytesToLockHigh,
            IFileTransactionLock **ppTransactionLock    // OUT
            ) = 0;

        enum FileLockExclusivity
        {
            ExclusiveFileLock,
            SharedFileLock
        };

        enum FileLockWaitMode
        {
            FailImmediately,
            WaitForLockAcquisition
        };

        virtual
        PAL_ERROR
        CreateFileLock(
            CPalThread *pThread,                // IN, OPTIONAL
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToLockLow,
            DWORD nNumberOfBytesToLockHigh,
            FileLockExclusivity eFileLockExclusivity,
            FileLockWaitMode eFileLockWaitMode
            ) = 0;

        virtual
        PAL_ERROR
        ReleaseFileLock(
            CPalThread *pThread,                // IN, OPTIONAL
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToUnlockLow,
            DWORD nNumberOfBytesToUnlockHigh
            ) = 0;

        //
        // ReleaseController should be called from the file object's
        // cleanup routine. It must always be called, even if fShutdown is
        // TRUE or fCleanupSharedState is FALSE.
        //

        virtual
        void
        ReleaseController() = 0;
    };

    class IFileLockManager
    {
    public:

        //
        // GetLockControllerForFile should be called by CreateFile.
        // It will fail if the requested access rights and share
        // mode are not compatible with existing lock controllers
        // for the file.
        //

        virtual
        PAL_ERROR
        GetLockControllerForFile(
            CPalThread *pThread,                // IN, OPTIONAL
            LPCSTR szFileName,
            DWORD dwAccessRights,
            DWORD dwShareMode,
            IFileLockController **ppLockController  // OUT
            ) = 0;

        // 
        // Gets the share mode for the file
        // (returns SHARE_MODE_NOT_INITIALIZED if file lock controller 
        // not found)
        // 
        virtual
        PAL_ERROR
        GetFileShareModeForFile(
            LPCSTR szFileName,
            DWORD* pdwShareMode) = 0;
    };

    extern IFileLockManager *g_pFileLockManager;
}

#endif // _CORUNIX_H

