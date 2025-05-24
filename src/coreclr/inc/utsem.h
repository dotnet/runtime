// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/* ----------------------------------------------------------------------------

---------------------------------------------------------------------------- */
#ifndef __UTSEM_H__
#define __UTSEM_H__


// -------------------------------------------------------------
//              INCLUDES
// -------------------------------------------------------------
#include "utilcode.h"
#ifdef HOST_UNIX
#include <pthread.h>
#endif // TARGET_UNIX

/* ----------------------------------------------------------------------------
@class UTSemReadWrite

    An instance of class UTSemReadWrite provides multi-read XOR single-write
    (a.k.a. shared vs. exclusive) lock capabilities, with protection against
    writer starvation.

    A thread MUST NOT call any of the Lock methods if it already holds a Lock.
    (Doing so may result in a deadlock.)


---------------------------------------------------------------------------- */
class UTSemReadWrite
{
public:
    UTSemReadWrite();   // Constructor
	~UTSemReadWrite();  // Destructor

    HRESULT Init();

    HRESULT LockRead();     // Lock the object for reading
    HRESULT LockWrite();    // Lock the object for writing
    void UnlockRead();      // Unlock the object for reading
    void UnlockWrite();     // Unlock the object for writing

#if defined(_DEBUG) && defined(HOST_WINDOWS)
    BOOL Debug_IsLockedForRead();
    BOOL Debug_IsLockedForWrite();
#endif // defined(_DEBUG) && defined(HOST_WINDOWS)

private:
#ifdef HOST_WINDOWS
    Volatile<ULONG> m_dwFlag;               // internal state, see implementation
    HANDLE          m_hReadWaiterSemaphore; // semaphore for awakening read waiters
    HANDLE          m_hWriteWaiterEvent;    // event for awakening write waiters
#else // HOST_WINDOWS
    bool m_initialized;
    pthread_rwlock_t m_rwLock;
#endif // HOST_WINDOWS
};  // class UTSemReadWrite

#endif // __UTSEM_H__
