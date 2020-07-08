// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    palobjbase.hpp

Abstract:
    PAL object base class



--*/

#ifndef _PALOBJBASE_HPP_
#define _PALOBJBASE_HPP_

#include "pal/corunix.hpp"
#include "pal/cs.hpp"
#include "pal/thread.hpp"

namespace CorUnix
{
    class CSimpleDataLock : IDataLock
    {
    private:

        CRITICAL_SECTION m_cs;
        bool m_fInitialized;

    public:

        CSimpleDataLock()
            :
            m_fInitialized(FALSE)
        {
        };

        virtual ~CSimpleDataLock()
        {
            if (m_fInitialized)
            {
                InternalDeleteCriticalSection(&m_cs);
            }
        };

        PAL_ERROR
        Initialize(
            void
            )
        {
            PAL_ERROR palError = NO_ERROR;

            InternalInitializeCriticalSection(&m_cs);
            m_fInitialized = TRUE;

            return palError;
        };

        void
        AcquireLock(
            CPalThread *pthr,
            IDataLock **pDataLock
            )
        {
            InternalEnterCriticalSection(pthr, &m_cs);
            *pDataLock = static_cast<IDataLock*>(this);
        };

        virtual
        void
        ReleaseLock(
            CPalThread *pthr,
            bool fDataChanged
            )
        {
            InternalLeaveCriticalSection(pthr, &m_cs);
        };

    };

    class CPalObjectBase : public IPalObject
    {
        template <class T> friend void InternalDelete(T *p);

    protected:

        LONG m_lRefCount;

        VOID *m_pvImmutableData;
        VOID *m_pvLocalData;

        CObjectType *m_pot;
        CObjectAttributes m_oa;

        CSimpleDataLock m_sdlLocalData;

        //
        // The thread that initiated object cleanup
        //

        CPalThread *m_pthrCleanup;

        virtual ~CPalObjectBase();

        virtual
        void
        AcquireObjectDestructionLock(
            CPalThread *pthr
            ) = 0;

        virtual
        bool
        ReleaseObjectDestructionLock(
            CPalThread *pthr,
            bool fDestructionPending
            ) = 0;

    public:

        CPalObjectBase(
            CObjectType *pot
            )
            :
            m_lRefCount(1),
            m_pvImmutableData(NULL),
            m_pvLocalData(NULL),
            m_pot(pot),
            m_pthrCleanup(NULL)
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
        CObjectType *
        GetObjectType(
            VOID
            );

        virtual
        CObjectAttributes *
        GetObjectAttributes(
            VOID
            );

        virtual
        PAL_ERROR
        GetImmutableData(
            void **ppvImmutableData             // OUT
            );

        virtual
        PAL_ERROR
        GetProcessLocalData(
            CPalThread *pthr,                // IN, OPTIONAL
            LockType eLockRequest,
            IDataLock **ppDataLock,             // OUT
            void **ppvProcessLocalData          // OUT
            );

        virtual
        DWORD
        AddReference(
            void
            );

        virtual
        DWORD
        ReleaseReference(
            CPalThread *pthr
            );
    };
}

#endif // _PALOBJBASE_HPP_

