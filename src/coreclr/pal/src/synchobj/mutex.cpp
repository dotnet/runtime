// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    mutex.ccpp

Abstract:

    Implementation of mutex synchroniztion object as described in
    the WIN32 API

Revision History:

--*/

#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(SYNC); // some headers have code with asserts, so do this first

#include "pal/mutex.hpp"
#include "pal/file.hpp"
#include "pal/thread.hpp"
#include "pal/utils.h"

#include "../synchmgr/synchmanager.hpp"

#include <sys/file.h>
#include <sys/types.h>

#include <errno.h>
#include <time.h>
#include <unistd.h>
#include "minipal/time.h"

using namespace CorUnix;

/* ------------------- Definitions ------------------------------*/

CObjectType CorUnix::otMutex(
                otiMutex,
                NULL,   // No cleanup routine
                0,      // No immutable data
                NULL,   // No immutable data copy routine
                NULL,   // No immutable data cleanup routine
                0,      // No process local data
                NULL,   // No process local data cleanup routine
                CObjectType::WaitableObject,
                CObjectType::ObjectCanBeUnsignaled,
                CObjectType::ThreadReleaseAltersSignalCount,
                CObjectType::OwnershipTracked
                );

static CAllowedObjectTypes aotMutex(otiMutex);

/*++
Function:
  CreateMutexW

Note:
  lpMutexAttributes currently ignored:
  -- Win32 object security not supported
  -- handles to mutex objects are not inheritable

  See MSDN docs on CreateMutexW for all other parameters.
--*/

HANDLE
PALAPI
CreateMutexW(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN BOOL bInitialOwner,
    IN LPCWSTR lpName)
{
    _ASSERTE(lpName == nullptr);
    HANDLE hMutex = NULL;
    PAL_ERROR palError;
    CPalThread *pthr = NULL;

    PERF_ENTRY(CreateMutexW);
    ENTRY("CreateMutexW(lpMutexAttributes=%p, bInitialOwner=%d\n",
        lpMutexAttributes,
        bInitialOwner
        );

    pthr = InternalGetCurrentThread();

    {
        palError = InternalCreateMutex(
            pthr,
            nullptr, // lpMutexAttributes currently ignored
            bInitialOwner,
            &hMutex
            );
    }

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

    pthr->SetLastError(palError);

    LOGEXIT("CreateMutexW returns HANDLE %p\n", hMutex);
    PERF_EXIT(CreateMutexW);
    return hMutex;
}

/*++
Function:
CreateMutexExW

Note:
lpMutexAttributes currently ignored:
-- Win32 object security not supported
-- handles to mutex objects are not inheritable

Parameters:
See MSDN doc.
--*/

HANDLE
PALAPI
CreateMutexExW(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN LPCWSTR lpName,
    IN DWORD dwFlags,
    IN DWORD dwDesiredAccess)
{
    return CreateMutexW(lpMutexAttributes, (dwFlags & CREATE_MUTEX_INITIAL_OWNER) != 0, lpName);
}

/*++
Function:
  InternalCreateMutex

Note:
  lpMutexAttributes currently ignored:
  -- Win32 object security not supported
  -- handles to mutex objects are not inheritable

Parameters:
  errors -- An optional wrapper for system call errors, for more detailed error information.
  pthr -- thread data for calling thread
  phEvent -- on success, receives the allocated mutex handle

  See MSDN docs on CreateMutex for all other parameters.
--*/

PAL_ERROR
CorUnix::InternalCreateMutex(
    CPalThread *pthr,
    LPSECURITY_ATTRIBUTES lpMutexAttributes,
    BOOL bInitialOwner,
    HANDLE *phMutex
    )
{
    CObjectAttributes oa(nullptr, lpMutexAttributes);
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjMutex = NULL;
    IPalObject *pobjRegisteredMutex = NULL;
    ISynchStateController *pssc = NULL;
    HANDLE hMutex = nullptr;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != phMutex);

    ENTRY("InternalCreateMutex(pthr=%p, lpMutexAttributes=%p, bInitialOwner=%d"
        ", phMutex=%p)\n",
        pthr,
        lpMutexAttributes,
        bInitialOwner,
        phMutex
        );

    palError = g_pObjectManager->AllocateObject(
        pthr,
        &otMutex,
        &oa,
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreateMutexExit;
    }

    palError = pobjMutex->GetSynchStateController(
        pthr,
        &pssc
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to create state controller (%d)\n", palError);
        goto InternalCreateMutexExit;
    }

    if (bInitialOwner)
    {
        palError = pssc->SetOwner(pthr);
    }
    else
    {
        palError = pssc->SetSignalCount(1);
    }

    pssc->ReleaseController();

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to set initial mutex state (%d)\n", palError);
        goto InternalCreateMutexExit;
    }

    palError = g_pObjectManager->RegisterObject(
        pthr,
        pobjMutex,
        &aotMutex,
        &hMutex,
        &pobjRegisteredMutex
        );
    _ASSERTE(palError != ERROR_ALREADY_EXISTS); // Mutexes can't have names
    _ASSERTE(palError != NO_ERROR || pobjRegisteredMutex == pobjMutex);
    _ASSERTE((palError == NO_ERROR) == (hMutex != nullptr));

    // When RegisterObject succeeds, the object would have an additional reference from the handle, and one reference is
    // released below through pobjRegisteredMutex. When RegisterObject fails, it releases the initial reference to the object.
    // Either way, pobjMutex is invalidated by the above call to RegisterObject.
    pobjMutex = nullptr;

    if (palError != NO_ERROR)
    {
        goto InternalCreateMutexExit;
    }

    pobjRegisteredMutex->ReleaseReference(pthr);
    pobjRegisteredMutex = nullptr;

    *phMutex = hMutex;
    hMutex = nullptr;

InternalCreateMutexExit:

    _ASSERTE(pobjRegisteredMutex == nullptr);
    _ASSERTE(hMutex == nullptr);

    if (pobjMutex != nullptr)
    {
        pobjMutex->ReleaseReference(pthr);
    }

    LOGEXIT("InternalCreateMutex returns %i\n", palError);

    return palError;
}

/*++
Function:
  ReleaseMutex

Parameters:
  See MSDN doc.
--*/

BOOL
PALAPI
ReleaseMutex( IN HANDLE hMutex )
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = NULL;

    PERF_ENTRY(ReleaseMutex);
    ENTRY("ReleaseMutex(hMutex=%p)\n", hMutex);

    pthr = InternalGetCurrentThread();

    palError = InternalReleaseMutex(pthr, hMutex);

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }

    LOGEXIT("ReleaseMutex returns BOOL %d\n", (NO_ERROR == palError));
    PERF_EXIT(ReleaseMutex);
    return (NO_ERROR == palError);
}

/*++
Function:
  InternalReleaseMutex

Parameters:
  pthr -- thread data for calling thread

  See MSDN docs on ReleaseMutex for all other parameters
--*/

PAL_ERROR
CorUnix::InternalReleaseMutex(
    CPalThread *pthr,
    HANDLE hMutex
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjMutex = NULL;
    ISynchStateController *pssc = NULL;
    PalObjectTypeId objectTypeId;

    _ASSERTE(NULL != pthr);

    ENTRY("InternalReleaseMutex(pthr=%p, hMutex=%p)\n",
        pthr,
        hMutex
        );

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pthr,
        hMutex,
        &aotMutex,
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        ERROR("Unable to obtain object for handle %p (error %d)!\n", hMutex, palError);
        goto InternalReleaseMutexExit;
    }

    palError = pobjMutex->GetSynchStateController(
        pthr,
        &pssc
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Error %d obtaining synch state controller\n", palError);
        goto InternalReleaseMutexExit;
    }

    palError = pssc->DecrementOwnershipCount();

    if (NO_ERROR != palError)
    {
        ERROR("Error %d decrementing mutex ownership count\n", palError);
        goto InternalReleaseMutexExit;
    }

InternalReleaseMutexExit:

    if (NULL != pssc)
    {
        pssc->ReleaseController();
    }

    if (NULL != pobjMutex)
    {
        pobjMutex->ReleaseReference(pthr);
    }

    LOGEXIT("InternalReleaseMutex returns %i\n", palError);

    return palError;
}

/* Basic spinlock implementation */
void SPINLOCKAcquire (LONG * lock, unsigned int flags)
{
    size_t loop_seed = 1, loop_count = 0;

    if (flags & SYNCSPINLOCK_F_ASYMMETRIC)
    {
        loop_seed = ((size_t)pthread_self() % 10) + 1;
    }
    while (InterlockedCompareExchange(lock, 1, 0))
    {
        if (!(flags & SYNCSPINLOCK_F_ASYMMETRIC) || (++loop_count % loop_seed))
        {
#if PAL_IGNORE_NORMAL_THREAD_PRIORITY
            struct timespec tsSleepTime;
            tsSleepTime.tv_sec = 0;
            tsSleepTime.tv_nsec = 1;
            nanosleep(&tsSleepTime, NULL);
#else
            sched_yield();
#endif
        }
    }

}

void SPINLOCKRelease (LONG * lock)
{
    VolatileStore(lock, 0);
}

DWORD SPINLOCKTryAcquire (LONG * lock)
{
    return InterlockedCompareExchange(lock, 1, 0);
    // only returns 0 or 1.
}
