// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    handlemgr.hpp

Abstract:

    Simple handle table manager class



--*/

#ifndef _PAL_HANDLEMGR_H_
#define _PAL_HANDLEMGR_H_


#include "corunix.hpp"
#include "cs.hpp"
#include "pal/thread.hpp"
#include "pal/malloc.hpp"


/* Pseudo handles constant for current thread and process */
extern const HANDLE hPseudoCurrentProcess;
extern const HANDLE hPseudoCurrentThread;
extern const HANDLE hPseudoGlobalIOCP;

namespace CorUnix
{
    class CSimpleHandleManager
    {
    private:
        enum { c_BasicGrowthRate = 1024 };
        enum { c_MaxIndex = 0x3FFFFFFE };

        typedef UINT_PTR HANDLE_INDEX;
        static const HANDLE_INDEX c_hiInvalid = (HANDLE_INDEX) -1;

        HANDLE
        HandleIndexToHandle(HANDLE_INDEX hi)
        {
            return (HANDLE) ((hi + 1) << 2);
        };

        HANDLE_INDEX
        HandleToHandleIndex(HANDLE h)
        {
            return (HANDLE_INDEX) (((UINT_PTR) h) >> 2) - 1;
        };

        typedef struct _HANDLE_TABLE_ENTRY
        {
            union
            {
                IPalObject *pObject;
                HANDLE_INDEX hiNextIndex;
            } u;

            bool fEntryAllocated;
        } HANDLE_TABLE_ENTRY;

        HANDLE_INDEX m_hiFreeListStart;
        HANDLE_INDEX m_hiFreeListEnd;

        DWORD m_dwTableSize;
        DWORD m_dwTableGrowthRate;
        HANDLE_TABLE_ENTRY* m_rghteHandleTable;

        CRITICAL_SECTION m_csLock;
        bool m_fLockInitialized;

        bool ValidateHandle(HANDLE h);

    public:

        CSimpleHandleManager()
            :
            m_hiFreeListStart(c_hiInvalid),
            m_hiFreeListEnd(c_hiInvalid),
            m_dwTableSize(0),
            m_dwTableGrowthRate(c_BasicGrowthRate),
            m_rghteHandleTable(NULL),
            m_fLockInitialized(FALSE)
        {
        };

        virtual
        ~CSimpleHandleManager()
        {
            if (m_fLockInitialized)
            {
                DeleteCriticalSection(&m_csLock);
            }

            if (NULL != m_rghteHandleTable)
            {
                free(m_rghteHandleTable);
            }
        }

        PAL_ERROR
        Initialize(
            void
            );

        PAL_ERROR
        AllocateHandle(
            CPalThread *pThread,
            IPalObject *pObject,
            HANDLE *ph
            );

        //
        // On success this will add a reference to the returned object.
        //

        PAL_ERROR
        GetObjectFromHandle(
            CPalThread *pThread,
            HANDLE h,
            IPalObject **ppObject
            );

        PAL_ERROR
        FreeHandle(
            CPalThread *pThread,
            HANDLE h
            );

        void
        Lock(
            CPalThread *pThread
            )
        {
            InternalEnterCriticalSection(pThread, &m_csLock);
        };

        void
        Unlock(
            CPalThread *pThread
            )
        {
            InternalLeaveCriticalSection(pThread, &m_csLock);
        };
    };

    bool
    HandleIsSpecial(
        HANDLE h
        );
}

#endif // _PAL_HANDLEMGR_H_













