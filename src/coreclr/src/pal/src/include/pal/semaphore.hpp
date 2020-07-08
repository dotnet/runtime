// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

}

#endif //_PAL_SEMAPHORE_H_










