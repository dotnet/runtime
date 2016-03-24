// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    mutex.ccpp

Abstract:

    Implementation of mutex synchroniztion object as described in 
    the WIN32 API

Revision History:



--*/

#include "pal/mutex.hpp"
#include "pal/thread.hpp"
#include "pal/dbgmsg.h"

using namespace CorUnix;

/* ------------------- Definitions ------------------------------*/
SET_DEFAULT_DEBUG_CHANNEL(SYNC);

CObjectType CorUnix::otMutex(
                otiMutex,
                NULL,   // No cleanup routine
                NULL,   // No initialization routine
                0,      // No immutable data
                0,      // No process local data
                0,      // No shared data
                0,      // Should be MUTEX_ALL_ACCESS; currently ignored (no Win32 security)
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::ObjectCanHaveName,
                CObjectType::CrossProcessDuplicationAllowed,
                CObjectType::WaitableObject,
                CObjectType::ObjectCanBeUnsignaled,
                CObjectType::ThreadReleaseAltersSignalCount,
                CObjectType::OwnershipTracked
                );

CAllowedObjectTypes aotMutex(otiMutex);

/*++
Function:
  CreateMutexA

Note:
  lpMutexAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to mutex objects are not inheritable

Parameters:
  See MSDN doc.
--*/

HANDLE
PALAPI
CreateMutexA(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN BOOL bInitialOwner,
    IN LPCSTR lpName)
{
    HANDLE hMutex = NULL;
    CPalThread *pthr = NULL;
    PAL_ERROR palError;
    
    PERF_ENTRY(CreateMutexA);
    ENTRY("CreateMutexA(lpMutexAttr=%p, bInitialOwner=%d, lpName=%p (%s)\n",
          lpMutexAttributes, bInitialOwner, lpName, lpName?lpName:"NULL");

    pthr = InternalGetCurrentThread();
    
    if (lpName != nullptr)
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
    }
    else
    {
        palError = InternalCreateMutex(
            pthr,
            lpMutexAttributes,
            bInitialOwner,
            NULL,
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
    
    LOGEXIT("CreateMutexA returns HANDLE %p\n", hMutex);
    PERF_EXIT(CreateMutexA);
    return hMutex;
}


/*++
Function:
  CreateMutexW

Note:
  lpMutexAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to mutex objects are not inheritable

Parameters:
  See MSDN doc.
--*/

HANDLE
PALAPI
CreateMutexW(
    IN LPSECURITY_ATTRIBUTES lpMutexAttributes,
    IN BOOL bInitialOwner,
    IN LPCWSTR lpName)
{
    HANDLE hMutex = NULL;
    PAL_ERROR palError;
    CPalThread *pthr = NULL;

    PERF_ENTRY(CreateMutexW);
    ENTRY("CreateMutexW(lpMutexAttr=%p, bInitialOwner=%d, lpName=%p (%S)\n",
          lpMutexAttributes, bInitialOwner, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    palError = InternalCreateMutex(
        pthr,
        lpMutexAttributes,
        bInitialOwner,
        lpName,
        &hMutex
        );

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
  InternalCreateMutex

Note:
  lpMutexAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to mutex objects are not inheritable

Parameters:
  pthr -- thread data for calling thread
  phEvent -- on success, receives the allocated mutex handle

  See MSDN docs on CreateMutex for all other parameters
--*/

PAL_ERROR
CorUnix::InternalCreateMutex(
    CPalThread *pthr,
    LPSECURITY_ATTRIBUTES lpMutexAttributes,
    BOOL bInitialOwner,
    LPCWSTR lpName,
    HANDLE *phMutex
    )
{
    CObjectAttributes oa(lpName, lpMutexAttributes);
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjMutex = NULL;
    IPalObject *pobjRegisteredMutex = NULL;
    ISynchStateController *pssc = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != phMutex);

    ENTRY("InternalCreateMutex(pthr=%p, lpMutexAttributes=%p, bInitialOwner=%d"
        ", lpName=%p, phMutex=%p)\n",
        pthr,
        lpMutexAttributes,
        bInitialOwner,
        lpName,
        phMutex
        );

    if (lpName != nullptr)
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
        goto InternalCreateMutexExit;
    }

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
        0, // should be MUTEX_ALL_ACCESS -- currently ignored (no Win32 security)
        phMutex,
        &pobjRegisteredMutex
        );

    //
    // pobjMutex is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //
    
    pobjMutex = NULL;

InternalCreateMutexExit:

    if (NULL != pobjMutex)
    {
        pobjMutex->ReleaseReference(pthr);
    }

    if (NULL != pobjRegisteredMutex)
    {
        pobjRegisteredMutex->ReleaseReference(pthr);
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

    _ASSERTE(NULL != pthr);

    ENTRY("InternalReleaseMutex(pthr=%p, hMutex=%p)\n",
        pthr,
        hMutex
        );

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pthr,
        hMutex,
        &aotMutex,
        0, // should be MUTEX_MODIFY_STATE -- current ignored (no Win32 security)
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

/*++
Function:
  OpenMutexA

Note:
  dwDesiredAccess is currently ignored (no Win32 object security support)
  bInheritHandle is currently ignored (handles to mutexes are not inheritable)

See MSDN doc.
--*/

HANDLE
PALAPI
OpenMutexA (
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCSTR lpName)
{
    HANDLE hMutex = NULL;
    CPalThread *pthr = NULL;
    PAL_ERROR palError;
    
    PERF_ENTRY(OpenMutexA);
    ENTRY("OpenMutexA(dwDesiredAccess=%#x, bInheritHandle=%d, lpName=%p (%s))\n", 
          dwDesiredAccess, bInheritHandle, lpName, lpName?lpName:"NULL");

    pthr = InternalGetCurrentThread();

    /* validate parameters */
    if (lpName == nullptr)
    {
        ERROR("name is NULL\n");
        palError = ERROR_INVALID_PARAMETER;
    }
    else
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
    }

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }
        
    LOGEXIT("OpenMutexA returns HANDLE %p\n", hMutex);
    PERF_EXIT(OpenMutexA);
    return hMutex;
}

/*++
Function:
  OpenMutexW

Note:
  dwDesiredAccess is currently ignored (no Win32 object security support)
  bInheritHandle is currently ignored (handles to mutexes are not inheritable)

See MSDN doc.
--*/

HANDLE
PALAPI
OpenMutexW(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCWSTR lpName)
{
    HANDLE hMutex = NULL;
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = NULL;

    PERF_ENTRY(OpenMutexW);
    ENTRY("OpenMutexW(dwDesiredAccess=%#x, bInheritHandle=%d, lpName=%p (%S))\n", 
          dwDesiredAccess, bInheritHandle, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    /* validate parameters */
    if (lpName == nullptr)
    {
        ERROR("name is NULL\n");
        palError = ERROR_INVALID_PARAMETER;
    }
    else
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
    }

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }

    LOGEXIT("OpenMutexW returns HANDLE %p\n", hMutex);
    PERF_EXIT(OpenMutexW);

    return hMutex;
}

/*++
Function:
  InternalOpenMutex

Note:
  dwDesiredAccess is currently ignored (no Win32 object security support)
  bInheritHandle is currently ignored (handles to mutexes are not inheritable)

Parameters:
  pthr -- thread data for calling thread
  phEvent -- on success, receives the allocated mutex handle
  
  See MSDN docs on OpenMutex for all other parameters.
--*/

PAL_ERROR
CorUnix::InternalOpenMutex(
    CPalThread *pthr,
    DWORD dwDesiredAccess,
    BOOL bInheritHandle,
    LPCWSTR lpName,
    HANDLE *phMutex
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjMutex = NULL;
    CPalString sObjectName(lpName);

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != lpName);
    _ASSERTE(NULL != phMutex);

    ENTRY("InternalOpenMutex(pthr=%p, dwDesiredAcces=%d, bInheritHandle=%d, "
        "lpName=%p, phMutex=%p)\n",
        pthr,
        dwDesiredAccess,
        bInheritHandle,
        lpName,
        phMutex
        );

    palError = g_pObjectManager->LocateObject(
        pthr,
        &sObjectName,
        &aotMutex,
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        goto InternalOpenMutexExit;
    }

    palError = g_pObjectManager->ObtainHandleForObject(
        pthr,
        pobjMutex,
        dwDesiredAccess,
        bInheritHandle,
        NULL,
        phMutex
        );

    if (NO_ERROR != palError)
    {
        goto InternalOpenMutexExit;
    }

InternalOpenMutexExit:

    if (NULL != pobjMutex)
    {
        pobjMutex->ReleaseReference(pthr);
    }

    LOGEXIT("InternalOpenMutex returns %d\n", palError);
    
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
    *lock = 0;
}

DWORD SPINLOCKTryAcquire (LONG * lock)
{
    return InterlockedCompareExchange(lock, 1, 0);
    // only returns 0 or 1.
}
