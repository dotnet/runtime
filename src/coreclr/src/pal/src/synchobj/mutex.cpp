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

#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(SYNC); // some headers have code with asserts, so do this first

#include "pal/mutex.hpp"
#include "pal/file.hpp"
#include "pal/thread.hpp"

#include "../synchmgr/synchmanager.hpp"

#include <sys/file.h>
#include <sys/types.h>

#include <errno.h>
#include <time.h>
#include <unistd.h>

#include "pal/sharedmemory.inl"

using namespace CorUnix;

/* ------------------- Definitions ------------------------------*/

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
                CObjectType::UnnamedObject,
                CObjectType::LocalDuplicationOnly,
                CObjectType::WaitableObject,
                CObjectType::ObjectCanBeUnsignaled,
                CObjectType::ThreadReleaseAltersSignalCount,
                CObjectType::OwnershipTracked
                );

static CAllowedObjectTypes aotMutex(otiMutex);

CObjectType CorUnix::otNamedMutex(
                otiNamedMutex,
                &SharedMemoryProcessDataHeader::PalObject_Close, // Cleanup routine
                NULL,   // No initialization routine
                sizeof(SharedMemoryProcessDataHeader *), // Immutable data
                0,      // No process local data
                0,      // No shared data
                0,      // Should be MUTEX_ALL_ACCESS; currently ignored (no Win32 security)
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::UnnamedObject, // PAL's naming infrastructure is not used
                CObjectType::LocalDuplicationOnly,
                CObjectType::UnwaitableObject, // PAL's waiting infrastructure is not used
                CObjectType::SignalingNotApplicable, // PAL's signaling infrastructure is not used
                CObjectType::ThreadReleaseNotApplicable, // PAL's signaling infrastructure is not used
                CObjectType::OwnershipNotApplicable // PAL's ownership infrastructure is not used
                );

static CAllowedObjectTypes aotNamedMutex(otiNamedMutex);

static PalObjectTypeId anyMutexTypeIds[] = {otiMutex, otiNamedMutex};
static CAllowedObjectTypes aotAnyMutex(anyMutexTypeIds, _countof(anyMutexTypeIds));

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
    char utf8Name[SHARED_MEMORY_MAX_NAME_CHAR_COUNT + 1];

    PERF_ENTRY(CreateMutexW);
    ENTRY("CreateMutexW(lpMutexAttr=%p, bInitialOwner=%d, lpName=%p (%S)\n",
          lpMutexAttributes, bInitialOwner, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    if (lpName != nullptr)
    {
        int bytesWritten = WideCharToMultiByte(CP_ACP, 0, lpName, -1, utf8Name, _countof(utf8Name), nullptr, nullptr);
        if (bytesWritten == 0)
        {
            DWORD errorCode = GetLastError();
            if (errorCode == ERROR_INSUFFICIENT_BUFFER)
            {
                palError = static_cast<DWORD>(SharedMemoryError::NameTooLong);
            }
            else
            {
                ASSERT("WideCharToMultiByte failed (%u)\n", errorCode);
                palError = errorCode;
            }
            goto CreateMutexWExit;
        }
    }

    palError = InternalCreateMutex(
        pthr,
        lpMutexAttributes,
        bInitialOwner,
        lpName == nullptr ? nullptr : utf8Name,
        &hMutex
        );

CreateMutexWExit:
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
    LPCSTR lpName,
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
        ", lpName=%p, phMutex=%p)\n",
        pthr,
        lpMutexAttributes,
        bInitialOwner,
        lpName,
        phMutex
        );

    if (lpName != nullptr && lpName[0] == '\0')
    {
        // Empty name is treated as a request for an unnamed process-local mutex
        lpName = nullptr;
    }

    CObjectType *ot = lpName == nullptr ? &otMutex : &otNamedMutex;
    CAllowedObjectTypes *aot = lpName == nullptr ? &aotMutex : &aotNamedMutex;

    palError = g_pObjectManager->AllocateObject(
        pthr,
        ot,
        &oa,
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreateMutexExit;
    }

    if (lpName == nullptr)
    {
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
    }

    palError = g_pObjectManager->RegisterObject(
        pthr,
        pobjMutex,
        aot,
        0, // should be MUTEX_ALL_ACCESS -- currently ignored (no Win32 security)
        &hMutex,
        &pobjRegisteredMutex
        );

    if (palError != NO_ERROR)
    {
        _ASSERTE(palError != ERROR_ALREADY_EXISTS); // PAL's naming infrastructure is not used for named mutexes
        _ASSERTE(pobjRegisteredMutex == nullptr);
        _ASSERTE(hMutex == nullptr);
        goto InternalCreateMutexExit;
    }

    // Now that the object has been registered successfully, it would have a reference associated with the handle, so release
    // the initial reference. Any errors from now on need to revoke the handle.
    _ASSERTE(pobjRegisteredMutex == pobjMutex);
    _ASSERTE(hMutex != nullptr);
    pobjMutex->ReleaseReference(pthr);
    pobjRegisteredMutex = nullptr;

    if (lpName != nullptr)
    {
        SharedMemoryProcessDataHeader *processDataHeader;
        bool created = false;
        try
        {
            processDataHeader = NamedMutexProcessData::CreateOrOpen(lpName, !!bInitialOwner, &created);
        }
        catch (SharedMemoryException ex)
        {
            palError = ex.GetErrorCode();
            goto InternalCreateMutexExit;
        }
        SharedMemoryProcessDataHeader::PalObject_SetProcessDataHeader(pobjMutex, processDataHeader);

        if (!created)
        {
            // Indicate to the caller that an existing mutex was opened, and hence the caller will not have initial ownership
            // of the mutex if requested through bInitialOwner
            palError = ERROR_ALREADY_EXISTS;
        }
    }

    *phMutex = hMutex;
    hMutex = nullptr;
    pobjMutex = nullptr;

InternalCreateMutexExit:

    _ASSERTE(pobjRegisteredMutex == nullptr);
    if (hMutex != nullptr)
    {
        g_pObjectManager->RevokeHandle(pthr, hMutex);
    }
    else if (NULL != pobjMutex)
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
        &aotAnyMutex,
        0, // should be MUTEX_MODIFY_STATE -- current ignored (no Win32 security)
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        ERROR("Unable to obtain object for handle %p (error %d)!\n", hMutex, palError);
        goto InternalReleaseMutexExit;
    }

    objectTypeId = pobjMutex->GetObjectType()->GetId();
    if (objectTypeId == otiMutex)
    {
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
    }
    else
    {
        _ASSERTE(objectTypeId == otiNamedMutex);

        SharedMemoryProcessDataHeader *processDataHeader =
            SharedMemoryProcessDataHeader::PalObject_GetProcessDataHeader(pobjMutex);
        _ASSERTE(processDataHeader != nullptr);
        try
        {
            static_cast<NamedMutexProcessData *>(processDataHeader->GetData())->ReleaseLock();
        }
        catch (SharedMemoryException ex)
        {
            palError = ex.GetErrorCode();
            goto InternalReleaseMutexExit;
        }
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
        goto OpenMutexAExit;
    }

    palError = InternalOpenMutex(pthr, dwDesiredAccess, bInheritHandle, lpName, &hMutex);

OpenMutexAExit:
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
    char utf8Name[SHARED_MEMORY_MAX_NAME_CHAR_COUNT + 1];

    PERF_ENTRY(OpenMutexW);
    ENTRY("OpenMutexW(dwDesiredAccess=%#x, bInheritHandle=%d, lpName=%p (%S))\n", 
          dwDesiredAccess, bInheritHandle, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    /* validate parameters */
    if (lpName == nullptr)
    {
        ERROR("name is NULL\n");
        palError = ERROR_INVALID_PARAMETER;
        goto OpenMutexWExit;
    }

    {
        int bytesWritten = WideCharToMultiByte(CP_ACP, 0, lpName, -1, utf8Name, _countof(utf8Name), nullptr, nullptr);
        if (bytesWritten == 0)
        {
            DWORD errorCode = GetLastError();
            if (errorCode == ERROR_INSUFFICIENT_BUFFER)
            {
                palError = static_cast<DWORD>(SharedMemoryError::NameTooLong);
            }
            else
            {
                ASSERT("WideCharToMultiByte failed (%u)\n", errorCode);
                palError = errorCode;
            }
            goto OpenMutexWExit;
        }
    }

    palError = InternalOpenMutex(pthr, dwDesiredAccess, bInheritHandle, lpName == nullptr ? nullptr : utf8Name, &hMutex);

OpenMutexWExit:
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
    LPCSTR lpName,
    HANDLE *phMutex
    )
{
    CObjectAttributes oa;
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjMutex = NULL;
    IPalObject *pobjRegisteredMutex = NULL;
    HANDLE hMutex = nullptr;

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

    palError = g_pObjectManager->AllocateObject(
        pthr,
        &otNamedMutex,
        &oa,
        &pobjMutex
        );

    if (NO_ERROR != palError)
    {
        goto InternalOpenMutexExit;
    }

    palError = g_pObjectManager->RegisterObject(
        pthr,
        pobjMutex,
        &aotNamedMutex,
        dwDesiredAccess,
        &hMutex,
        &pobjRegisteredMutex
        );

    if (palError != NO_ERROR)
    {
        _ASSERTE(palError != ERROR_ALREADY_EXISTS); // PAL's naming infrastructure is not used for named mutexes
        _ASSERTE(pobjRegisteredMutex == nullptr);
        _ASSERTE(hMutex == nullptr);
        goto InternalOpenMutexExit;
    }

    // Now that the object has been registered successfully, it would have a reference associated with the handle, so release
    // the initial reference. Any errors from now on need to revoke the handle.
    _ASSERTE(pobjRegisteredMutex == pobjMutex);
    _ASSERTE(hMutex != nullptr);
    pobjMutex->ReleaseReference(pthr);
    pobjRegisteredMutex = nullptr;

    {
        SharedMemoryProcessDataHeader *processDataHeader;
        try
        {
            processDataHeader = NamedMutexProcessData::Open(lpName);
        }
        catch (SharedMemoryException ex)
        {
            palError = ex.GetErrorCode();
            goto InternalOpenMutexExit;
        }
        if (processDataHeader == nullptr)
        {
            palError = ERROR_FILE_NOT_FOUND;
            goto InternalOpenMutexExit;
        }
        SharedMemoryProcessDataHeader::PalObject_SetProcessDataHeader(pobjMutex, processDataHeader);
    }

    *phMutex = hMutex;
    hMutex = nullptr;
    pobjMutex = nullptr;

InternalOpenMutexExit:

    _ASSERTE(pobjRegisteredMutex == nullptr);
    if (hMutex != nullptr)
    {
        g_pObjectManager->RevokeHandle(pthr, hMutex);
    }
    else if (NULL != pobjMutex)
    {
        pobjMutex->ReleaseReference(pthr);
    }

    LOGEXIT("InternalCreateMutex returns %i\n", palError);

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

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MutexHelpers

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
void MutexHelpers::InitializeProcessSharedRobustRecursiveMutex(pthread_mutex_t *mutex)
{
    _ASSERTE(mutex != nullptr);

    struct AutoCleanup
    {
        pthread_mutexattr_t *m_mutexAttributes;

        AutoCleanup() : m_mutexAttributes(nullptr)
        {
        }

        ~AutoCleanup()
        {
            if (m_mutexAttributes != nullptr)
            {
                int error = pthread_mutexattr_destroy(m_mutexAttributes);
                _ASSERTE(error == 0);
            }
        }
    } autoCleanup;

    pthread_mutexattr_t mutexAttributes;
    int error = pthread_mutexattr_init(&mutexAttributes);
    if (error != 0)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
    }
    autoCleanup.m_mutexAttributes = &mutexAttributes;

    error = pthread_mutexattr_setpshared(&mutexAttributes, PTHREAD_PROCESS_SHARED);
    _ASSERTE(error == 0);

    error = pthread_mutexattr_setrobust(&mutexAttributes, PTHREAD_MUTEX_ROBUST);
    _ASSERTE(error == 0);

    error = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    _ASSERTE(error == 0);

    error = pthread_mutex_init(mutex, &mutexAttributes);
    if (error != 0)
    {
        throw SharedMemoryException(static_cast<DWORD>(error == EPERM ? SharedMemoryError::IO : SharedMemoryError::OutOfMemory));
    }
}

void MutexHelpers::DestroyMutex(pthread_mutex_t *mutex)
{
    _ASSERTE(mutex != nullptr);

    int error = pthread_mutex_destroy(mutex);
    _ASSERTE(error == 0 || error == EBUSY); // the error will be EBUSY if the mutex is locked
}

MutexTryAcquireLockResult MutexHelpers::TryAcquireLock(pthread_mutex_t *mutex, DWORD timeoutMilliseconds)
{
    _ASSERTE(mutex != nullptr);

    int lockResult;
    switch (timeoutMilliseconds)
    {
        case static_cast<DWORD>(-1):
            lockResult = pthread_mutex_lock(mutex);
            break;

        case 0:
            lockResult = pthread_mutex_trylock(mutex);
            break;

        default:
        {
            struct timespec timeoutTime;
            PAL_ERROR palError = CPalSynchronizationManager::GetAbsoluteTimeout(timeoutMilliseconds, &timeoutTime, /*fPreferMonotonicClock*/ FALSE);
            _ASSERTE(palError == NO_ERROR);
            lockResult = pthread_mutex_timedlock(mutex, &timeoutTime);
            break;
        }
    }

    switch (lockResult)
    {
        case 0:
            return MutexTryAcquireLockResult::AcquiredLock;

        case EBUSY:
            _ASSERTE(timeoutMilliseconds == 0);
            return MutexTryAcquireLockResult::TimedOut;

        case ETIMEDOUT:
            _ASSERTE(timeoutMilliseconds != static_cast<DWORD>(-1));
            _ASSERTE(timeoutMilliseconds != 0);
            return MutexTryAcquireLockResult::TimedOut;

        case EOWNERDEAD:
        {
            int setConsistentResult = pthread_mutex_consistent(mutex);
            _ASSERTE(setConsistentResult == 0);
            return MutexTryAcquireLockResult::AcquiredLockButMutexWasAbandoned;
        }

        case EAGAIN:
            throw SharedMemoryException(static_cast<DWORD>(NamedMutexError::MaximumRecursiveLocksReached));

        default:
            throw SharedMemoryException(static_cast<DWORD>(NamedMutexError::Unknown));
    }
}

void MutexHelpers::ReleaseLock(pthread_mutex_t *mutex)
{
    _ASSERTE(mutex != nullptr);

    int unlockResult = pthread_mutex_unlock(mutex);
    _ASSERTE(unlockResult == 0);
}
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// NamedMutexSharedData

NamedMutexSharedData::NamedMutexSharedData()
    :
#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    m_timedWaiterCount(0),
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    m_lockOwnerProcessId(SharedMemoryHelpers::InvalidProcessId),
    m_lockOwnerThreadId(SharedMemoryHelpers::InvalidSharedThreadId),
    m_isAbandoned(false)
{
#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    static_assert_no_msg(sizeof(m_timedWaiterCount) == sizeof(LONG)); // for interlocked operations
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(SharedMemoryManager::IsCreationDeletionFileLockAcquired());

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
    MutexHelpers::InitializeProcessSharedRobustRecursiveMutex(&m_lock);
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX
}

NamedMutexSharedData::~NamedMutexSharedData()
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(SharedMemoryManager::IsCreationDeletionFileLockAcquired());

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
    MutexHelpers::DestroyMutex(&m_lock);
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX
}

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
pthread_mutex_t *NamedMutexSharedData::GetLock()
{
    return &m_lock;
}
#else // !NAMED_MUTEX_USE_PTHREAD_MUTEX
bool NamedMutexSharedData::HasAnyTimedWaiters() const
{
    return
        InterlockedCompareExchange(
            const_cast<LONG *>(reinterpret_cast<const LONG *>(&m_timedWaiterCount)),
            -1 /* Exchange */,
            -1 /* Comparand */) != 0;
}

void NamedMutexSharedData::IncTimedWaiterCount()
{
    ULONG newValue = InterlockedIncrement(reinterpret_cast<LONG *>(&m_timedWaiterCount));
    if (newValue == 0)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
    }
}

void NamedMutexSharedData::DecTimedWaiterCount()
{
    ULONG newValue = InterlockedDecrement(reinterpret_cast<LONG *>(&m_timedWaiterCount));
    _ASSERTE(newValue + 1 != 0);
}
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

bool NamedMutexSharedData::IsAbandoned() const
{
    _ASSERTE(IsLockOwnedByCurrentThread());
    return m_isAbandoned;
}

void NamedMutexSharedData::SetIsAbandoned(bool isAbandoned)
{
    _ASSERTE(IsLockOwnedByCurrentThread());
    _ASSERTE(m_isAbandoned != isAbandoned);

    m_isAbandoned = isAbandoned;
}

bool NamedMutexSharedData::IsLockOwnedByAnyThread() const
{
    return
        m_lockOwnerProcessId != SharedMemoryHelpers::InvalidProcessId ||
        m_lockOwnerThreadId != SharedMemoryHelpers::InvalidSharedThreadId;
}

bool NamedMutexSharedData::IsLockOwnedByCurrentThread() const
{
    return m_lockOwnerProcessId == GetCurrentProcessId() && m_lockOwnerThreadId == THREADSilentGetCurrentThreadId();
}

void NamedMutexSharedData::SetLockOwnerToCurrentThread()
{
    m_lockOwnerProcessId = GetCurrentProcessId();
    _ASSERTE(m_lockOwnerProcessId != SharedMemoryHelpers::InvalidProcessId);
    m_lockOwnerThreadId = THREADSilentGetCurrentThreadId();
    _ASSERTE(m_lockOwnerThreadId != SharedMemoryHelpers::InvalidSharedThreadId);
}

void NamedMutexSharedData::ClearLockOwner()
{
    _ASSERTE(IsLockOwnedByCurrentThread());

    m_lockOwnerProcessId = SharedMemoryHelpers::InvalidProcessId;
    m_lockOwnerThreadId = SharedMemoryHelpers::InvalidSharedThreadId;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// NamedMutexProcessData

// This value should only be incremented if a non-backward-compatible change to the sync system is made. A process would fail to
// open a mutex created with a different sync system version.
const UINT8 NamedMutexProcessData::SyncSystemVersion = 1;

const DWORD NamedMutexProcessData::PollLoopMaximumSleepMilliseconds = 100;

SharedMemoryProcessDataHeader *NamedMutexProcessData::CreateOrOpen(LPCSTR name, bool acquireLockIfCreated, bool *createdRef)
{
    return CreateOrOpen(name, true /* createIfNotExist */, acquireLockIfCreated, createdRef);
}

SharedMemoryProcessDataHeader *NamedMutexProcessData::Open(LPCSTR name)
{
    return CreateOrOpen(name, false /* createIfNotExist */, false /* acquireLockIfCreated */, nullptr /* createdRef */);
}

SharedMemoryProcessDataHeader *NamedMutexProcessData::CreateOrOpen(
    LPCSTR name,
    bool createIfNotExist,
    bool acquireLockIfCreated,
    bool *createdRef)
{
    _ASSERTE(name != nullptr);
    _ASSERTE(createIfNotExist || !acquireLockIfCreated);

    struct AutoCleanup
    {
        bool m_acquiredCreationDeletionProcessLock;
        bool m_acquiredCreationDeletionFileLock;
        SharedMemoryProcessDataHeader *m_processDataHeader;
    #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
        char *m_lockFilePath;
        SIZE_T m_sessionDirectoryPathCharCount;
        bool m_createdLockFile;
        int m_lockFileDescriptor;
    #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
        bool m_cancel;

        AutoCleanup()
            : m_acquiredCreationDeletionProcessLock(false),
            m_acquiredCreationDeletionFileLock(false),
            m_processDataHeader(nullptr),
        #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
            m_lockFilePath(nullptr),
            m_sessionDirectoryPathCharCount(0),
            m_createdLockFile(false),
            m_lockFileDescriptor(-1),
        #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
            m_cancel(false)
        {
        }

        ~AutoCleanup()
        {
        #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
            if (!m_cancel)
            {
                if (m_lockFileDescriptor != -1)
                {
                    SharedMemoryHelpers::CloseFile(m_lockFileDescriptor);
                }

                if (m_createdLockFile)
                {
                    _ASSERTE(m_lockFilePath != nullptr);
                    unlink(m_lockFilePath);
                }

                if (m_sessionDirectoryPathCharCount != 0)
                {
                    _ASSERTE(m_lockFilePath != nullptr);
                    m_lockFilePath[m_sessionDirectoryPathCharCount] = '\0';
                    rmdir(m_lockFilePath);
                }
            }
        #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX

            if (m_acquiredCreationDeletionFileLock)
            {
                SharedMemoryManager::ReleaseCreationDeletionFileLock();
            }

            if (!m_cancel && m_processDataHeader != nullptr)
            {
                _ASSERTE(m_acquiredCreationDeletionProcessLock);
                m_processDataHeader->DecRefCount();
            }

            if (m_acquiredCreationDeletionProcessLock)
            {
                SharedMemoryManager::ReleaseCreationDeletionProcessLock();
            }
        }
    } autoCleanup;

    SharedMemoryManager::AcquireCreationDeletionProcessLock();
    autoCleanup.m_acquiredCreationDeletionProcessLock = true;

    // Create or open the shared memory
    bool created;
    SharedMemoryProcessDataHeader *processDataHeader =
        SharedMemoryProcessDataHeader::CreateOrOpen(
            name,
            SharedMemorySharedDataHeader(SharedMemoryType::Mutex, SyncSystemVersion),
            sizeof(NamedMutexSharedData),
            createIfNotExist,
            &created);
    if (createdRef != nullptr)
    {
        *createdRef = created;
    }
    if (created)
    {
        // If the shared memory file was created, the creation/deletion file lock would have been acquired so that we can
        // initialize the shared data
        autoCleanup.m_acquiredCreationDeletionFileLock = true;
    }
    if (processDataHeader == nullptr)
    {
        _ASSERTE(!createIfNotExist);
        return nullptr;
    }
    autoCleanup.m_processDataHeader = processDataHeader;

    if (created)
    {
        // Initialize the shared data
        new(processDataHeader->GetSharedDataHeader()->GetData()) NamedMutexSharedData;
    }

    if (processDataHeader->GetData() == nullptr)
    {
    #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
        // Create the lock files directory
        char lockFilePath[SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT + 1];
        SIZE_T lockFilePathCharCount =
            SharedMemoryHelpers::CopyString(lockFilePath, 0, SHARED_MEMORY_LOCK_FILES_DIRECTORY_PATH);
        if (created)
        {
            SharedMemoryHelpers::EnsureDirectoryExists(lockFilePath, true /* isGlobalLockAcquired */);
        }

        // Create the session directory
        lockFilePath[lockFilePathCharCount++] = '/';
        SharedMemoryId *id = processDataHeader->GetId();
        lockFilePathCharCount = id->AppendSessionDirectoryName(lockFilePath, lockFilePathCharCount);
        if (created)
        {
            SharedMemoryHelpers::EnsureDirectoryExists(lockFilePath, true /* isGlobalLockAcquired */);
            autoCleanup.m_lockFilePath = lockFilePath;
            autoCleanup.m_sessionDirectoryPathCharCount = lockFilePathCharCount;
        }

        // Create or open the lock file
        lockFilePath[lockFilePathCharCount++] = '/';
        lockFilePathCharCount =
            SharedMemoryHelpers::CopyString(lockFilePath, lockFilePathCharCount, id->GetName(), id->GetNameCharCount());
        int lockFileDescriptor = SharedMemoryHelpers::CreateOrOpenFile(lockFilePath, created);
        if (lockFileDescriptor == -1)
        {
            _ASSERTE(!created);
            if (createIfNotExist)
            {
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }
            return nullptr;
        }
        autoCleanup.m_createdLockFile = created;
        autoCleanup.m_lockFileDescriptor = lockFileDescriptor;
    #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX

        // Create the process data
        void *processDataBuffer = SharedMemoryHelpers::Alloc(sizeof(NamedMutexProcessData));
        AutoFreeBuffer autoFreeProcessDataBuffer(processDataBuffer);
        NamedMutexProcessData *processData =
            new(processDataBuffer)
            NamedMutexProcessData(
                processDataHeader
            #if !NAMED_MUTEX_USE_PTHREAD_MUTEX
                ,
                lockFileDescriptor
            #endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
            );
        autoFreeProcessDataBuffer.Cancel();
        processDataHeader->SetData(processData);

        // If the mutex was created and if requested, acquire the lock initially while holding the creation/deletion locks
        if (created && acquireLockIfCreated)
        {
            MutexTryAcquireLockResult tryAcquireLockResult = processData->TryAcquireLock(0);
            _ASSERTE(tryAcquireLockResult == MutexTryAcquireLockResult::AcquiredLock);
        }
    }

    autoCleanup.m_cancel = true;
    return processDataHeader;
}

NamedMutexProcessData::NamedMutexProcessData(
    SharedMemoryProcessDataHeader *processDataHeader
#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    ,
    int sharedLockFileDescriptor
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
)
    :
    m_processDataHeader(processDataHeader),
    m_lockCount(0),
#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    m_sharedLockFileDescriptor(sharedLockFileDescriptor),
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    m_lockOwnerThread(nullptr),
    m_nextInThreadOwnedNamedMutexList(nullptr)
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(processDataHeader != nullptr);

#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    _ASSERTE(sharedLockFileDescriptor != -1);

    m_processLockHandle = CreateMutex(nullptr /* lpMutexAttributes */, false /* bInitialOwner */, nullptr /* lpName */);
    if (m_processLockHandle == nullptr)
    {
        throw SharedMemoryException(GetLastError());
    }
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
}

void NamedMutexProcessData::Close(bool isAbruptShutdown, bool releaseSharedData)
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!releaseSharedData || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    // If the process is shutting down abruptly without having closed some mutexes, there could still be threads running with
    // active references to the mutex. So when shutting down abruptly, don't clean up any object or global process-local state.
    if (!isAbruptShutdown)
    {
        CPalThread *lockOwnerThread = m_lockOwnerThread;
        if (lockOwnerThread != nullptr)
        {
            // The mutex was not released before it was closed. If the lock is owned by the current thread, abandon the mutex.
            // In both cases, clean up the owner thread's list of owned mutexes.
            lockOwnerThread->synchronizationInfo.RemoveOwnedNamedMutex(this);
            if (lockOwnerThread == GetCurrentPalThread())
            {
                Abandon();
            }
            else
            {
                m_lockOwnerThread = nullptr;
            }
        }

        if (releaseSharedData)
        {
            GetSharedData()->~NamedMutexSharedData();
        }
    }

#if !NAMED_MUTEX_USE_PTHREAD_MUTEX
    if (!isAbruptShutdown)
    {
        CloseHandle(m_processLockHandle);
        SharedMemoryHelpers::CloseFile(m_sharedLockFileDescriptor);
    }

    if (!releaseSharedData)
    {
        return;
    }

    // Delete the lock file, and the session directory if it's not empty
    char path[SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT + 1];
    SIZE_T sessionDirectoryPathCharCount = SharedMemoryHelpers::CopyString(path, 0, SHARED_MEMORY_LOCK_FILES_DIRECTORY_PATH);
    path[sessionDirectoryPathCharCount++] = '/';
    SharedMemoryId *id = m_processDataHeader->GetId();
    sessionDirectoryPathCharCount = id->AppendSessionDirectoryName(path, sessionDirectoryPathCharCount);
    path[sessionDirectoryPathCharCount++] = '/';
    SharedMemoryHelpers::CopyString(path, sessionDirectoryPathCharCount, id->GetName(), id->GetNameCharCount());
    unlink(path);
    path[sessionDirectoryPathCharCount] = '\0';
    rmdir(path);
#endif // !NAMED_MUTEX_USE_PTHREAD_MUTEX
}

NamedMutexSharedData *NamedMutexProcessData::GetSharedData() const
{
    return reinterpret_cast<NamedMutexSharedData *>(m_processDataHeader->GetSharedDataHeader()->GetData());
}

void NamedMutexProcessData::SetLockOwnerThread(CorUnix::CPalThread *lockOwnerThread)
{
    _ASSERTE(lockOwnerThread == nullptr || lockOwnerThread == GetCurrentPalThread());
    _ASSERTE(GetSharedData()->IsLockOwnedByCurrentThread());

    m_lockOwnerThread = lockOwnerThread;
}

NamedMutexProcessData *NamedMutexProcessData::GetNextInThreadOwnedNamedMutexList() const
{
    return m_nextInThreadOwnedNamedMutexList;
}

void NamedMutexProcessData::SetNextInThreadOwnedNamedMutexList(NamedMutexProcessData *next)
{
    m_nextInThreadOwnedNamedMutexList = next;
}

MutexTryAcquireLockResult NamedMutexProcessData::TryAcquireLock(DWORD timeoutMilliseconds)
{
    NamedMutexSharedData *sharedData = GetSharedData();

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
    MutexTryAcquireLockResult result = MutexHelpers::TryAcquireLock(sharedData->GetLock(), timeoutMilliseconds);
    if (result == MutexTryAcquireLockResult::TimedOut)
    {
        return result;
    }

    // Check if a recursive lock was just taken. The recursion level is tracked manually so that the lock owner can be cleared
    // at the appropriate time, see ReleaseLock().
    if (m_lockCount != 0)
    {
        _ASSERTE(sharedData->IsLockOwnedByCurrentThread()); // otherwise, this thread would not have acquired the lock
        _ASSERTE(GetCurrentPalThread()->synchronizationInfo.OwnsNamedMutex(this));

        if (m_lockCount + 1 < m_lockCount)
        {
            MutexHelpers::ReleaseLock(sharedData->GetLock());
            throw SharedMemoryException(static_cast<DWORD>(NamedMutexError::MaximumRecursiveLocksReached));
        }
        ++m_lockCount;

        // The lock is released upon acquiring a recursive lock from the thread that already owns the lock
        MutexHelpers::ReleaseLock(sharedData->GetLock());

        _ASSERTE(result != MutexTryAcquireLockResult::AcquiredLockButMutexWasAbandoned);
        _ASSERTE(!sharedData->IsAbandoned());
        return result;
    }

    // The non-recursive case is handled below (skip the #else and see below that)
#else // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    // If a timeout is specified, determine the start time
    DWORD startTime = 0;
    if (timeoutMilliseconds != static_cast<DWORD>(-1) && timeoutMilliseconds != 0)
    {
        startTime = GetTickCount();
    }

    // Acquire the process lock. A file lock can only be acquired once per file descriptor, so to synchronize the threads of
    // this process, the process lock is used.
    while (true)
    {
        DWORD waitResult = WaitForSingleObject(m_processLockHandle, timeoutMilliseconds);
        switch (waitResult)
        {
            case WAIT_OBJECT_0:
            case WAIT_ABANDONED: // abandoned state for the process lock is irrelevant, the shared lock will also have been abandoned
                break;

            case WAIT_TIMEOUT:
                return MutexTryAcquireLockResult::TimedOut;

            case WAIT_IO_COMPLETION:
                continue;

            case WAIT_FAILED:
                throw SharedMemoryException(GetLastError());

            default:
                _ASSERTE(false);
                break;
        }
        break;
    }

    struct AutoReleaseProcessLock
    {
        HANDLE m_processLockHandle;
        bool m_cancel;

        AutoReleaseProcessLock(HANDLE processLockHandle) : m_processLockHandle(processLockHandle), m_cancel(false)
        {
        }

        ~AutoReleaseProcessLock()
        {
            if (!m_cancel)
            {
                ReleaseMutex(m_processLockHandle);
            }
        }
    } autoReleaseProcessLock(m_processLockHandle);

    // Check if it's a recursive lock attempt
    if (m_lockCount != 0)
    {
        _ASSERTE(sharedData->IsLockOwnedByCurrentThread()); // otherwise, this thread would not have acquired the process lock
        _ASSERTE(GetCurrentPalThread()->synchronizationInfo.OwnsNamedMutex(this));

        if (m_lockCount + 1 < m_lockCount)
        {
            throw SharedMemoryException(static_cast<DWORD>(NamedMutexError::MaximumRecursiveLocksReached));
        }
        ++m_lockCount;

        // The process lock is released upon acquiring a recursive lock from the thread that already owns the lock
        return MutexTryAcquireLockResult::AcquiredLock;
    }

    switch (timeoutMilliseconds)
    {
        case static_cast<DWORD>(-1):
        {
            // The file lock API does not have a timeout on the wait, so timed waiters will poll the file lock in a loop,
            // sleeping for a short duration in-between. Due to the polling nature of a timed wait, timed waiters will almost
            // never acquire the file lock as long as there are also untimed waiters. So, in order to make the file lock
            // acquisition reasonable, when there are timed waiters, have untimed waiters also use polling.
            bool acquiredFileLock = false;
            while (sharedData->HasAnyTimedWaiters())
            {
                if (SharedMemoryHelpers::TryAcquireFileLock(m_sharedLockFileDescriptor, LOCK_EX | LOCK_NB))
                {
                    acquiredFileLock = true;
                    break;
                }
                Sleep(PollLoopMaximumSleepMilliseconds);
            }
            if (acquiredFileLock)
            {
                break;
            }

            acquiredFileLock = SharedMemoryHelpers::TryAcquireFileLock(m_sharedLockFileDescriptor, LOCK_EX);
            _ASSERTE(acquiredFileLock);
            break;
        }

        case 0:
            if (!SharedMemoryHelpers::TryAcquireFileLock(m_sharedLockFileDescriptor, LOCK_EX | LOCK_NB))
            {
                return MutexTryAcquireLockResult::TimedOut;
            }
            break;

        default:
        {
            // Try to acquire the file lock without waiting
            if (SharedMemoryHelpers::TryAcquireFileLock(m_sharedLockFileDescriptor, LOCK_EX | LOCK_NB))
            {
                break;
            }

            // The file lock API does not have a timeout on the wait, so timed waiters need to poll the file lock in a loop,
            // sleeping for a short duration in-between. Due to the polling nature of a timed wait, timed waiters will almost
            // never acquire the file lock as long as there are also untimed waiters. So, in order to make the file lock
            // acquisition reasonable, record that there is a timed waiter, to have untimed waiters also use polling.
            sharedData->IncTimedWaiterCount();
            struct AutoDecTimedWaiterCount
            {
                NamedMutexSharedData *m_sharedData;

                AutoDecTimedWaiterCount(NamedMutexSharedData *sharedData) : m_sharedData(sharedData)
                {
                }

                ~AutoDecTimedWaiterCount()
                {
                    m_sharedData->DecTimedWaiterCount();
                }
            } autoDecTimedWaiterCount(sharedData);

            // Poll for the file lock
            do
            {
                DWORD elapsedMilliseconds = GetTickCount() - startTime;
                if (elapsedMilliseconds >= timeoutMilliseconds)
                {
                    return MutexTryAcquireLockResult::TimedOut;
                }

                DWORD remainingMilliseconds = timeoutMilliseconds - elapsedMilliseconds;
                DWORD sleepMilliseconds =
                    remainingMilliseconds < PollLoopMaximumSleepMilliseconds
                        ? remainingMilliseconds
                        : PollLoopMaximumSleepMilliseconds;
                Sleep(sleepMilliseconds);
            } while (!SharedMemoryHelpers::TryAcquireFileLock(m_sharedLockFileDescriptor, LOCK_EX | LOCK_NB));
            break;
        }
    }

    // There cannot be any exceptions after this
    autoReleaseProcessLock.m_cancel = true;

    // After acquiring the file lock, if we find that a lock owner is already designated, the process that previously owned the
    // lock must have terminated while holding the lock.
    MutexTryAcquireLockResult result =
        sharedData->IsLockOwnedByAnyThread()
            ? MutexTryAcquireLockResult::AcquiredLockButMutexWasAbandoned
            : MutexTryAcquireLockResult::AcquiredLock;
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX

    sharedData->SetLockOwnerToCurrentThread();
    m_lockCount = 1;
    CPalThread *currentThread = GetCurrentPalThread();
    SetLockOwnerThread(currentThread);
    currentThread->synchronizationInfo.AddOwnedNamedMutex(this);

    if (sharedData->IsAbandoned())
    {
        // The thread that previously owned the lock did not release it before exiting
        sharedData->SetIsAbandoned(false);
        result = MutexTryAcquireLockResult::AcquiredLockButMutexWasAbandoned;
    }
    return result;
}

void NamedMutexProcessData::ReleaseLock()
{
    if (!GetSharedData()->IsLockOwnedByCurrentThread())
    {
        throw SharedMemoryException(static_cast<DWORD>(NamedMutexError::ThreadHasNotAcquiredMutex));
    }

    _ASSERTE(GetCurrentPalThread()->synchronizationInfo.OwnsNamedMutex(this));

    _ASSERTE(m_lockCount != 0);
    --m_lockCount;
    if (m_lockCount != 0)
    {
        return;
    }

    GetCurrentPalThread()->synchronizationInfo.RemoveOwnedNamedMutex(this);
    SetLockOwnerThread(nullptr);
    ActuallyReleaseLock();
}

void NamedMutexProcessData::Abandon()
{
    NamedMutexSharedData *sharedData = GetSharedData();
    _ASSERTE(sharedData->IsLockOwnedByCurrentThread());
    _ASSERTE(m_lockCount != 0);

    sharedData->SetIsAbandoned(true);
    m_lockCount = 0;
    SetLockOwnerThread(nullptr);
    ActuallyReleaseLock();
}

void NamedMutexProcessData::ActuallyReleaseLock()
{
    NamedMutexSharedData *sharedData = GetSharedData();
    _ASSERTE(sharedData->IsLockOwnedByCurrentThread());
    _ASSERTE(!GetCurrentPalThread()->synchronizationInfo.OwnsNamedMutex(this));
    _ASSERTE(m_lockCount == 0);

    sharedData->ClearLockOwner();

#if NAMED_MUTEX_USE_PTHREAD_MUTEX
    MutexHelpers::ReleaseLock(sharedData->GetLock());
#else // !NAMED_MUTEX_USE_PTHREAD_MUTEX
    SharedMemoryHelpers::ReleaseFileLock(m_sharedLockFileDescriptor);
    ReleaseMutex(m_processLockHandle);
#endif // NAMED_MUTEX_USE_PTHREAD_MUTEX
}
