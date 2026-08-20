// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/******************************************************************************
    FILE : UTSEM.CPP



    Purpose: Part of the utilities library for the VIPER project

    Abstract : Implements the UTSemReadWrite class.
-------------------------------------------------------------------------------
Revision History:


*******************************************************************************/
#include "stdafx.h"
#include "clrhost.h"
#include "ex.h"

#include <utsem.h>
#include "contract.h"

// Consider replacing this with a #ifdef INTEROP_DEBUGGING
#if !defined(SELF_NO_HOST) && defined(TARGET_X86) && !defined(TARGET_UNIX)
// For Interop debugging, the UTSemReadWrite class must inform the debugger
// that this thread can't be suspended currently.  See vm\util.hpp for the
// implementation of these methods.
void IncCantStopCount();
void DecCantStopCount();
#else
#define IncCantStopCount()
#define DecCantStopCount()
#endif  // !SELF_NO_HOST && TARGET_X86

/******************************************************************************
Function : UTSemReadWrite::UTSemReadWrite

Abstract: Constructor.
******************************************************************************/
UTSemReadWrite::UTSemReadWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_initialized = false;

#ifdef _DEBUG
    m_readers = 0;
    m_writers = 0;
#endif // _DEBUG
}


/******************************************************************************
Function : UTSemReadWrite::~UTSemReadWrite

Abstract: Destructor
******************************************************************************/
UTSemReadWrite::~UTSemReadWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    _ASSERTE_MSG((m_readers == 0), "Destroying a UTSemReadWrite while a reader lock is held");
    _ASSERTE_MSG((m_writers == 0), "Destroying a UTSemReadWrite while a writer lock is held");
#endif // _DEBUG

    if (m_initialized)
    {
        minipal_rwlock_destroy(&m_lock);
        m_initialized = false;
    }
}

//=======================================================================================
//
// Initialize the lock.
//
HRESULT UTSemReadWrite::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!m_initialized);

    if (!minipal_rwlock_init(&m_lock))
    {
        return E_FAIL;
    }

    m_initialized = true;
    return S_OK;
} // UTSemReadWrite::Init

/******************************************************************************
Function : UTSemReadWrite::LockRead

Abstract: Obtain a shared lock
******************************************************************************/
HRESULT UTSemReadWrite::LockRead()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

    // Inform CLR that the debugger shouldn't suspend this thread while
    // holding this lock.
    IncCantStopCount();

    minipal_rwlock_acquire_read(&m_lock);

#ifdef _DEBUG
    LONG readers = InterlockedIncrement(&m_readers);
    _ASSERTE(readers > 0);
    _ASSERTE(m_writers == 0);
#endif // _DEBUG

    EE_LOCK_TAKEN(this);

    return S_OK;
} // UTSemReadWrite::LockRead



/******************************************************************************
Function : UTSemReadWrite::LockWrite

Abstract: Obtain an exclusive lock
******************************************************************************/
HRESULT UTSemReadWrite::LockWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

    // Inform CLR that the debugger shouldn't suspend this thread while
    // holding this lock.
    IncCantStopCount();

    minipal_rwlock_acquire_write(&m_lock);

#ifdef _DEBUG
    LONG writers = InterlockedIncrement(&m_writers);
    _ASSERTE(writers == 1);
    _ASSERTE(m_readers == 0);
#endif // _DEBUG

    EE_LOCK_TAKEN(this);

    return S_OK;
} // UTSemReadWrite::LockWrite



/******************************************************************************
Function : UTSemReadWrite::UnlockRead

Abstract: Release a shared lock
******************************************************************************/
void UTSemReadWrite::UnlockRead()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

#ifdef _DEBUG
    _ASSERTE(m_readers > 0);
    _ASSERTE(m_writers == 0);

    LONG readers = InterlockedDecrement(&m_readers);
    _ASSERTE(readers >= 0);
#endif // _DEBUG

    minipal_rwlock_release_read(&m_lock);

    DecCantStopCount();
    EE_LOCK_RELEASED(this);
} // UTSemReadWrite::UnlockRead


/******************************************************************************
Function : UTSemReadWrite::UnlockWrite

Abstract: Release an exclusive lock
******************************************************************************/
void UTSemReadWrite::UnlockWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

#ifdef _DEBUG
    _ASSERTE(m_readers == 0);
    _ASSERTE(m_writers == 1);

    LONG writers = InterlockedDecrement(&m_writers);
    _ASSERTE(writers == 0);
#endif // _DEBUG

    minipal_rwlock_release_write(&m_lock);

    DecCantStopCount();
    EE_LOCK_RELEASED(this);
} // UTSemReadWrite::UnlockWrite

#ifdef _DEBUG

//=======================================================================================
BOOL UTSemReadWrite::Debug_IsLockedForRead()
{
    return (m_readers != 0);
}

//=======================================================================================
BOOL UTSemReadWrite::Debug_IsLockedForWrite()
{
    return (m_writers != 0);
}

#endif //_DEBUG
