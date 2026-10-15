// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    //
    // When the third parameter (fShutdown) is TRUE the process is in
    // the act of exiting. The cleanup routine should not perform any
    // unnecessary cleanup operations (e.g., closing file descriptors,
    // since the OS will automatically close them when the process exits)
    // in this situation.
    //

    typedef void (*OBJECTCLEANUPROUTINE) (
        CPalThread *,   // pThread
        IPalObject *,   // pObjectToCleanup
        bool            // fShutdown
        );

    typedef void (*OBJECT_IMMUTABLE_DATA_COPY_ROUTINE) (
        void *,
        void *);
    typedef void (*OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE) (
        void *);
    typedef void (*OBJECT_PROCESS_LOCAL_DATA_CLEANUP_ROUTINE) (
        CPalThread *,   // pThread
        IPalObject *);

    enum PalObjectTypeId
    {
        otiFile = 0,
        otiFileMapping,
        otiSocket,
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
    class CObjectType
    {
    private:

        //
        // Array that maps object type IDs to the corresponding
        // CObjectType instance
        //

        static CObjectType* s_rgotIdMapping[];

        PalObjectTypeId m_eTypeId;
        OBJECTCLEANUPROUTINE m_pCleanupRoutine;
        DWORD m_dwImmutableDataSize;
        OBJECT_IMMUTABLE_DATA_COPY_ROUTINE m_pImmutableDataCopyRoutine;
        OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE m_pImmutableDataCleanupRoutine;
        DWORD m_dwProcessLocalDataSize;
        OBJECT_PROCESS_LOCAL_DATA_CLEANUP_ROUTINE m_pProcessLocalDataCleanupRoutine;

    public:

        CObjectType(
            PalObjectTypeId eTypeId,
            OBJECTCLEANUPROUTINE pCleanupRoutine,
            DWORD dwImmutableDataSize,
            OBJECT_IMMUTABLE_DATA_COPY_ROUTINE pImmutableDataCopyRoutine,
            OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE pImmutableDataCleanupRoutine,
            DWORD dwProcessLocalDataSize,
            OBJECT_PROCESS_LOCAL_DATA_CLEANUP_ROUTINE pProcessLocalDataCleanupRoutine
            )
            :
            m_eTypeId(eTypeId),
            m_pCleanupRoutine(pCleanupRoutine),
            m_dwImmutableDataSize(dwImmutableDataSize),
            m_pImmutableDataCopyRoutine(pImmutableDataCopyRoutine),
            m_pImmutableDataCleanupRoutine(pImmutableDataCleanupRoutine),
            m_dwProcessLocalDataSize(dwProcessLocalDataSize),
            m_pProcessLocalDataCleanupRoutine(pProcessLocalDataCleanupRoutine)
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

        DWORD
        GetImmutableDataSize(
            void
            )
        {
            return  m_dwImmutableDataSize;
        };

        void
        SetImmutableDataCopyRoutine(
            OBJECT_IMMUTABLE_DATA_COPY_ROUTINE ptr
            )
        {
            m_pImmutableDataCopyRoutine = ptr;
        };

        OBJECT_IMMUTABLE_DATA_COPY_ROUTINE
        GetImmutableDataCopyRoutine(
            void
            )
        {
            return m_pImmutableDataCopyRoutine;
        };

        void
        SetImmutableDataCleanupRoutine(
            OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE ptr
            )
        {
            m_pImmutableDataCleanupRoutine = ptr;
        };

        OBJECT_IMMUTABLE_DATA_CLEANUP_ROUTINE
        GetImmutableDataCleanupRoutine(
            void
            )
        {
            return m_pImmutableDataCleanupRoutine;
        }

        DWORD
        GetProcessLocalDataSize(
            void
            )
        {
            return m_dwProcessLocalDataSize;
        };

        OBJECT_PROCESS_LOCAL_DATA_CLEANUP_ROUTINE
        GetProcessLocalDataCleanupRoutine(
            void
            )
        {
            return m_pProcessLocalDataCleanupRoutine;
        }

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
        DWORD
        AddReference(
            void
            ) = 0;

        virtual
        DWORD
        ReleaseReference(
            CPalThread *pThread
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
        // Object creation is a two step
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
        // For a named object pAllowedTypes specifies what type of existing
        // objects can be returned in ppRegisteredObjects. pAllowedTypes must
        // include the type of pObjectToRegister.
        //

        virtual
        PAL_ERROR
        RegisterObject(
            CPalThread *pThread,                // IN, OPTIONAL
            IPalObject *pObjectToRegister,
            CAllowedObjectTypes *pAllowedTypes,
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
            IPalObject **ppObject               // OUT
            ) = 0;

    };

    extern IPalObjectManager *g_pObjectManager;

}

#endif // _CORUNIX_H
