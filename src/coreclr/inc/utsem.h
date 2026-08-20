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
#include <minipal/rwlock.h>

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

#ifdef _DEBUG
    BOOL Debug_IsLockedForRead();
    BOOL Debug_IsLockedForWrite();
#endif //_DEBUG

private:
    minipal_rwlock m_lock;
    bool m_initialized;
#ifdef _DEBUG
    Volatile<LONG> m_readers;
    Volatile<LONG> m_writers;
#endif // _DEBUG
};  // class UTSemReadWrite

#endif // __UTSEM_H__
