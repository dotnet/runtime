// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    semaphore.hpp

Abstract:

    Semaphore object structure definition.



--*/

#ifndef _PAL_SEMAPHORE_H_
#define _PAL_SEMAPHORE_H_

#include "corunix.hpp"

namespace CorUnix
{
    extern CObjectType otSemaphore;

    typedef struct
    {
        LONG lMaximumCount;
    } SemaphoreImmutableData;

    PAL_ERROR
    InternalCreateSemaphore(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpSemaphoreAttributes,
        LONG lInitialCount,
        LONG lMaximumCount,
        LPCWSTR lpName,
        HANDLE *phSemaphore
        );

    PAL_ERROR
    InternalReleaseSemaphore(
        CPalThread *pThread,
        HANDLE hSemaphore,
        LONG lReleaseCount,
        LPLONG lpPreviousCount
        );

    PAL_ERROR
    InternalOpenSemaphore(
        CPalThread *pThread,
        DWORD dwDesiredAccess,
        BOOL bInheritHandle,
        LPCWSTR lpName,
        HANDLE *phSemaphore
        );
        
}

#endif //_PAL_SEMAPHORE_H_










