// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    mutex.hpp

Abstract:

    Mutex object structure definition.



--*/

#ifndef _PAL_MUTEX_H_
#define _PAL_MUTEX_H_

#include "corunix.hpp"

#include <pthread.h>

namespace CorUnix
{
    extern CObjectType otMutex;

    PAL_ERROR
    InternalCreateMutex(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpMutexAttributes,
        BOOL bInitialOwner,
        HANDLE *phMutex
        );

    PAL_ERROR
    InternalReleaseMutex(
        CPalThread *pThread,
        HANDLE hMutex
        );
}

#define SYNCSPINLOCK_F_ASYMMETRIC  1

#define SPINLOCKInit(lock) (*(lock) = 0)
#define SPINLOCKDestroy SPINLOCKInit

void SPINLOCKAcquire (LONG * lock, unsigned int flags);
void SPINLOCKRelease (LONG * lock);
DWORD SPINLOCKTryAcquire (LONG * lock);
#endif //_PAL_MUTEX_H_
