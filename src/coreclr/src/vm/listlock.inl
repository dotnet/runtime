// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
//

// 
// File: ListLock.inl
//
// ===========================================================================
// This file decribes the list lock and deadlock aware list lock functions
// that are inlined but can't go in the header.
// ===========================================================================
#ifndef LISTLOCK_INL
#define LISTLOCK_INL

#include "listlock.h"
#include "dbginterface.h"
// Must own the lock before calling this or is ok if the debugger has
// all threads stopped

inline ListLockEntry *ListLock::Find(void *pData)
{
	CONTRACTL
	{
		NOTHROW;
		GC_NOTRIGGER;
		PRECONDITION(CheckPointer(this));
#ifdef DEBUGGING_SUPPORTED
		PRECONDITION(m_Crst.OwnedByCurrentThread() || 
             CORDebuggerAttached() && g_pDebugInterface->IsStopped());
#else
		PRECONDITION(m_Crst.OwnedByCurrentThread());
#endif // DEBUGGING_SUPPORTED

	}
	CONTRACTL_END;

    ListLockEntry *pSearch;

    for (pSearch = m_pHead; pSearch != NULL; pSearch = pSearch->m_pNext)
    {
        if (pSearch->m_pData == pData)
            return pSearch;
    }

    return NULL;
}


#endif // LISTLOCK_I
