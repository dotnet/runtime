//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


/* ----------------------------------------------------------------------------

---------------------------------------------------------------------------- */
#ifndef __UTSEM_H__
#define __UTSEM_H__


// -------------------------------------------------------------
//              INCLUDES
// -------------------------------------------------------------
#include "utilcode.h"

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
    Semaphore * GetReadWaiterSemaphore()
    {
        return m_pReadWaiterSemaphore;
    }
    Event * GetWriteWaiterEvent()
    {
        return m_pWriteWaiterEvent;
    }
    
    Volatile<ULONG> m_dwFlag;               // internal state, see implementation
    Semaphore *     m_pReadWaiterSemaphore; // semaphore for awakening read waiters
    Event *         m_pWriteWaiterEvent;    // event for awakening write waiters
};  // class UTSemReadWrite

#endif // __UTSEM_H__
