// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    handlemgr.cpp

Abstract:

    Implementation of a basic handle table



--*/

#include "pal/thread.hpp"
#include "pal/handlemgr.hpp"
#include "pal/cs.hpp"
#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(HANDLE);

/* Constants */
/* Special handles */
/* Pseudo handles constant for current thread and process */
const HANDLE hPseudoCurrentProcess = (HANDLE) 0xFFFFFF01;
const HANDLE hPseudoCurrentThread  = (HANDLE) 0xFFFFFF03;
/* Pseudo handle constant for the global IO Completion port */
const HANDLE hPseudoGlobalIOCP  = (HANDLE) 0xFFFFFF05;

PAL_ERROR
CSimpleHandleManager::Initialize(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;

    InternalInitializeCriticalSection(&m_csLock);
    m_fLockInitialized = TRUE;

    m_dwTableGrowthRate = c_BasicGrowthRate;

    /* initialize the handle table - the free list is stored in the 'object'
       field, with the head in the global 'm_hiFreeListStart'. */
    m_dwTableSize = m_dwTableGrowthRate;

    m_rghteHandleTable = reinterpret_cast<HANDLE_TABLE_ENTRY*>(InternalMalloc((m_dwTableSize * sizeof(HANDLE_TABLE_ENTRY))));
    if(NULL == m_rghteHandleTable)
    {
        ERROR("Unable to create initial handle table array");
        palError = ERROR_OUTOFMEMORY;
        goto InitializeExit;
    }

    for (DWORD i = 0; i < m_dwTableSize; i++)
    {
        m_rghteHandleTable[i].u.hiNextIndex = i + 1;
        m_rghteHandleTable[i].fEntryAllocated = FALSE;
    }

    m_rghteHandleTable[m_dwTableSize - 1].u.hiNextIndex = (HANDLE_INDEX)-1;

    m_hiFreeListStart = 0;
    m_hiFreeListEnd = m_dwTableSize - 1;

    TRACE("Handle Manager initialization complete.\n");

InitializeExit:

    return palError;
}

PAL_ERROR
CSimpleHandleManager::AllocateHandle(
    CPalThread *pThread,
    IPalObject *pObject,
    HANDLE *ph
    )
{
    PAL_ERROR palError = NO_ERROR;
    DWORD dwIndex;

    Lock(pThread);

    /* if no free handles are available, we need to grow the handle table and
       add new handles to the pool */
    if (m_hiFreeListStart == c_hiInvalid)
    {
        HANDLE_TABLE_ENTRY* rghteTempTable;

        TRACE("Handle pool empty (%d handles allocated), growing handle table "
              "by %d entries.\n", m_dwTableSize, m_dwTableGrowthRate );

        /* make sure handle values don't overflow */
        if (m_dwTableSize + m_dwTableGrowthRate >= c_MaxIndex)
        {
            WARN("Unable to allocate handle : maximum (%d) reached!\n",
                 m_dwTableSize);
            palError = ERROR_OUTOFMEMORY;
            goto AllocateHandleExit;
        }

        /* grow handle table */
        rghteTempTable = reinterpret_cast<HANDLE_TABLE_ENTRY*>(InternalRealloc(
            m_rghteHandleTable,
            (m_dwTableSize + m_dwTableGrowthRate) * sizeof(HANDLE_TABLE_ENTRY)));

        if (NULL == rghteTempTable)
        {
            WARN("not enough memory to grow handle table!\n");
            palError = ERROR_OUTOFMEMORY;
            goto AllocateHandleExit;
        }
        m_rghteHandleTable = rghteTempTable;

        /* update handle table and handle pool */
        for (DWORD dw = m_dwTableSize; dw < m_dwTableSize + m_dwTableGrowthRate; dw += 1)
        {
            /* new handles are initially invalid */
            /* the last "old" handle was m_dwTableSize-1, so the new
               handles range from m_dwTableSize to
               m_dwTableSize+m_dwTableGrowthRate-1 */
            m_rghteHandleTable[dw].u.hiNextIndex = dw + 1;
            m_rghteHandleTable[dw].fEntryAllocated = FALSE;
        }

        m_hiFreeListStart = m_dwTableSize;
        m_dwTableSize += m_dwTableGrowthRate;
        m_rghteHandleTable[m_dwTableSize - 1].u.hiNextIndex = (HANDLE_INDEX)-1;
        m_hiFreeListEnd = m_dwTableSize - 1;

    }

    /* take the next free handle */
    dwIndex = m_hiFreeListStart;

    /* remove the handle from the pool */
    m_hiFreeListStart = m_rghteHandleTable[dwIndex].u.hiNextIndex;

    /* clear the tail record if this is the last handle slot available */
    if(m_hiFreeListStart == c_hiInvalid)
    {
        m_hiFreeListEnd = c_hiInvalid;
    }

    /* save the data associated with the new handle */
    *ph = HandleIndexToHandle(dwIndex);

    pObject->AddReference();
    m_rghteHandleTable[dwIndex].u.pObject = pObject;
    m_rghteHandleTable[dwIndex].fEntryAllocated = TRUE;

AllocateHandleExit:

    Unlock(pThread);

    return palError;
}

PAL_ERROR
CSimpleHandleManager::GetObjectFromHandle(
    CPalThread *pThread,
    HANDLE h,
    IPalObject **ppObject
    )
{
    PAL_ERROR palError = NO_ERROR;
    HANDLE_INDEX hi;

    Lock(pThread);

    if (!ValidateHandle(h))
    {
        ERROR("Tried to dereference an invalid handle %p\n", h);
        palError = ERROR_INVALID_HANDLE;
        goto GetObjectFromHandleExit;
    }

    hi = HandleToHandleIndex(h);

    *ppObject = m_rghteHandleTable[hi].u.pObject;
    (*ppObject)->AddReference();

GetObjectFromHandleExit:

    Unlock(pThread);

    return palError;
}

PAL_ERROR
CSimpleHandleManager::FreeHandle(
    CPalThread *pThread,
    HANDLE h
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobj = NULL;
    HANDLE_INDEX hi = HandleToHandleIndex(h);

    Lock(pThread);

    if (!ValidateHandle(h))
    {
        ERROR("Trying to free invalid handle %p.\n", h);
        palError = ERROR_INVALID_HANDLE;
        goto FreeHandleExit;
    }

    if (HandleIsSpecial(h))
    {
        ASSERT("Trying to free Special Handle %p.\n", h);
        palError = ERROR_INVALID_HANDLE;
        goto FreeHandleExit;
    }

    pobj = m_rghteHandleTable[hi].u.pObject;
    m_rghteHandleTable[hi].fEntryAllocated = FALSE;

    /* add handle to the free pool */
    if(m_hiFreeListEnd != c_hiInvalid)
    {
        m_rghteHandleTable[m_hiFreeListEnd].u.hiNextIndex = hi;
    }
    else
    {
        m_hiFreeListStart = hi;
    }

    m_rghteHandleTable[hi].u.hiNextIndex = c_hiInvalid;
    m_hiFreeListEnd = hi;

FreeHandleExit:

    Unlock(pThread);

    if (NULL != pobj)
    {
        pobj->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function :
    ValidateHandle

    Check if a handle was allocated by this handle manager

Parameters :
    HANDLE handle : handle to check.

Return Value :
    TRUE if valid, FALSE if invalid.
--*/
bool CSimpleHandleManager::ValidateHandle(HANDLE handle)
{
    DWORD dwIndex;

    if (NULL == m_rghteHandleTable)
    {
        ASSERT("Handle Manager is not initialized!\n");
        return FALSE;
    }

    if (handle == INVALID_HANDLE_VALUE || handle == 0)
    {
        TRACE( "INVALID_HANDLE_VALUE or NULL value is not a valid handle.\n" );
        return FALSE;
    }

    if (HandleIsSpecial(handle))
    {
        //
        // Special handles are valid in the general sense. They are not valid
        // in this context, though, as they were not allocated by the handle
        // manager. Hitting this case indicates a logic error within the PAL
        // (since clients of the handle manager should have already dealt with
        // the specialness of the handle) so we assert here.
        //

        ASSERT ("Handle %p is a special handle, returning FALSE.\n", handle);
        return FALSE;
    }

    dwIndex = HandleToHandleIndex(handle);

    if (dwIndex >= m_dwTableSize)
    {
        WARN( "The handle value(%p) is out of the bounds for the handle table.\n", handle );
        return FALSE;
    }

    if (!m_rghteHandleTable[dwIndex].fEntryAllocated)
    {
        WARN("The handle value (%p) has not been allocated\n", handle);
        return FALSE;
    }

    return TRUE;
}

bool
CorUnix::HandleIsSpecial(
    HANDLE h
    )
{
    return (hPseudoCurrentProcess == h ||
            hPseudoCurrentThread == h ||
            hPseudoGlobalIOCP == h);
}

