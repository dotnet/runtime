// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    wait.cpp

Abstract:

    Implementation of waiting functions as described in
    the WIN32 API

Revision History:



--*/

#include "pal/thread.hpp"
#include "pal/synchobjects.hpp"
#include "pal/handlemgr.hpp"
#include "pal/event.hpp"
#include "pal/mutex.hpp"
#include "pal/semaphore.hpp"
#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(SYNC);

#define MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE (MAXIMUM_WAIT_OBJECTS / 4)

using namespace CorUnix;

static PalObjectTypeId sg_rgWaitObjectsIds[] =
    {
        otiAutoResetEvent,
        otiManualResetEvent,
        otiMutex,
        otiNamedMutex,
        otiSemaphore,
        otiProcess,
        otiThread
    };
static CAllowedObjectTypes sg_aotWaitObject(sg_rgWaitObjectsIds,
    sizeof(sg_rgWaitObjectsIds)/sizeof(sg_rgWaitObjectsIds[0]));

static PalObjectTypeId sg_rgSignalableObjectIds[] =
{
    otiAutoResetEvent,
    otiManualResetEvent,
    otiMutex,
    otiNamedMutex,
    otiSemaphore
};
static CAllowedObjectTypes sg_aotSignalableObject(sg_rgSignalableObjectIds, ARRAY_SIZE(sg_rgSignalableObjectIds));

/*++
Function:
  WaitForSingleObject

See MSDN doc.
--*/
DWORD
PALAPI
WaitForSingleObject(IN HANDLE hHandle,
                    IN DWORD dwMilliseconds)
{
    DWORD dwRet;

    PERF_ENTRY(WaitForSingleObject);
    ENTRY("WaitForSingleObject(hHandle=%p, dwMilliseconds=%u)\n",
          hHandle, dwMilliseconds);

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalWaitForMultipleObjectsEx(pThread, 1, &hHandle, FALSE,
                                             dwMilliseconds, FALSE);

    LOGEXIT("WaitForSingleObject returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForSingleObject);
    return dwRet;
}


/*++
Function:
  WaitForSingleObjectPrioritized

Similar to WaitForSingleObject, except uses a LIFO release policy for waiting threads by prioritizing new waiters (registering
them at the beginning of the wait queue rather than at the end).
--*/
DWORD
PALAPI
PAL_WaitForSingleObjectPrioritized(IN HANDLE hHandle,
                                   IN DWORD dwMilliseconds)
{
    DWORD dwRet;

    PERF_ENTRY(PAL_WaitForSingleObjectPrioritized);
    ENTRY("PAL_WaitForSingleObjectPrioritized(hHandle=%p, dwMilliseconds=%u)\n",
          hHandle, dwMilliseconds);

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalWaitForMultipleObjectsEx(pThread, 1, &hHandle, FALSE,
                                             dwMilliseconds, FALSE, TRUE /* bPrioritize */);

    LOGEXIT("PAL_WaitForSingleObjectPrioritized returns DWORD %u\n", dwRet);
    PERF_EXIT(PAL_WaitForSingleObjectPrioritized);
    return dwRet;
}


/*++
Function:
  WaitForSingleObjectEx

See MSDN doc.
--*/
DWORD
PALAPI
WaitForSingleObjectEx(IN HANDLE hHandle,
                      IN DWORD dwMilliseconds,
                      IN BOOL bAlertable)
{
    DWORD dwRet;

    PERF_ENTRY(WaitForSingleObjectEx);
    ENTRY("WaitForSingleObjectEx(hHandle=%p, dwMilliseconds=%u, bAlertable=%s)\n",
          hHandle, dwMilliseconds, bAlertable ? "TRUE" : "FALSE");

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalWaitForMultipleObjectsEx(pThread, 1, &hHandle, FALSE,
                                             dwMilliseconds, bAlertable);

    LOGEXIT("WaitForSingleObjectEx returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForSingleObjectEx);
    return dwRet;
}


/*++
Function:
  WaitForMultipleObjects

See MSDN doc.

--*/
DWORD
PALAPI
WaitForMultipleObjects(IN DWORD nCount,
                       IN CONST HANDLE *lpHandles,
                       IN BOOL bWaitAll,
                       IN DWORD dwMilliseconds)
{
    DWORD dwRet;

    PERF_ENTRY(WaitForMultipleObjects);
    ENTRY("WaitForMultipleObjects(nCount=%d, lpHandles=%p,"
          " bWaitAll=%d, dwMilliseconds=%u)\n",
          nCount, lpHandles, bWaitAll, dwMilliseconds);

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalWaitForMultipleObjectsEx(pThread, nCount, lpHandles,
                                             bWaitAll, dwMilliseconds, FALSE);

    LOGEXIT("WaitForMultipleObjects returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForMultipleObjects);
    return dwRet;
}

/*++
Function:
  WaitForMultipleObjectsEx

See MSDN doc for info about this function.
--*/
DWORD
PALAPI
WaitForMultipleObjectsEx(IN DWORD nCount,
                         IN CONST HANDLE *lpHandles,
                         IN BOOL bWaitAll,
                         IN DWORD dwMilliseconds,
                         IN BOOL bAlertable)
{
    DWORD dwRet;

    PERF_ENTRY(WaitForMultipleObjectsEx);
    ENTRY("WaitForMultipleObjectsEx(nCount=%d, lpHandles=%p,"
          " bWaitAll=%d, dwMilliseconds=%u, bAlertable=%s)\n",
          nCount, lpHandles, bWaitAll, dwMilliseconds, bAlertable ? "TRUE" : "FALSE");

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalWaitForMultipleObjectsEx(pThread, nCount, lpHandles, bWaitAll,
                                             dwMilliseconds, bAlertable);

    LOGEXIT("WaitForMultipleObjectsEx returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForMultipleObjectsEx);
    return dwRet;
}

/*++
Function:
  SignalObjectAndWait

See MSDN doc for info about this function.
--*/
DWORD
PALAPI
SignalObjectAndWait(
    IN HANDLE hObjectToSignal,
    IN HANDLE hObjectToWaitOn,
    IN DWORD dwMilliseconds,
    IN BOOL bAlertable)
{
    PERF_ENTRY(SignalObjectAndWait);
    ENTRY(
        "SignalObjectAndWait(hObjectToSignal=%p, hObjectToWaitOn=%p, dwMilliseconds=%u, bAlertable=%s)\n",
        hObjectToSignal,
        hObjectToWaitOn,
        dwMilliseconds,
        bAlertable ? "TRUE" : "FALSE");

    CPalThread *thread = InternalGetCurrentThread();
    DWORD result = InternalSignalObjectAndWait(thread, hObjectToSignal, hObjectToWaitOn, dwMilliseconds, bAlertable);

    LOGEXIT("SignalObjectAndWait returns DWORD %u\n", result);
    PERF_EXIT(SignalObjectAndWait);
    return result;
}

/*++
Function:
  Sleep

See MSDN doc.
--*/
VOID
PALAPI
Sleep(IN DWORD dwMilliseconds)
{
    PERF_ENTRY(Sleep);
    ENTRY("Sleep(dwMilliseconds=%u)\n", dwMilliseconds);

    CPalThread * pThread = InternalGetCurrentThread();

    DWORD internalSleepRet = InternalSleepEx(pThread, dwMilliseconds, FALSE);

    if (internalSleepRet != 0)
    {
        ERROR("Sleep(dwMilliseconds=%u) failed [error=%u]\n", dwMilliseconds, internalSleepRet);
        pThread->SetLastError(internalSleepRet);
    }

    LOGEXIT("Sleep returns VOID\n");
    PERF_EXIT(Sleep);
}


/*++
Function:
  SleepEx

See MSDN doc.
--*/
DWORD
PALAPI
SleepEx(IN DWORD dwMilliseconds,
        IN BOOL bAlertable)
{
    DWORD dwRet;

    PERF_ENTRY(SleepEx);
    ENTRY("SleepEx(dwMilliseconds=%u, bAlertable=%d)\n", dwMilliseconds, bAlertable);

    CPalThread * pThread = InternalGetCurrentThread();

    dwRet = InternalSleepEx(pThread, dwMilliseconds, bAlertable);

    LOGEXIT("SleepEx returns DWORD %u\n", dwRet);
    PERF_EXIT(SleepEx);

    return dwRet;
}

/*++
Function:
  QueueUserAPC

See MSDN doc.
--*/
DWORD
PALAPI
QueueUserAPC(
    PAPCFUNC pfnAPC,
    HANDLE hThread,
    ULONG_PTR dwData)
{
    CPalThread * pCurrentThread = NULL;
    CPalThread * pTargetThread = NULL;
    IPalObject * pTargetThreadObject = NULL;
    PAL_ERROR palErr;
    DWORD dwRet;

    PERF_ENTRY(QueueUserAPC);
    ENTRY("QueueUserAPC(pfnAPC=%p, hThread=%p, dwData=%#x)\n",
          pfnAPC, hThread, dwData);

    /* NOTE: Windows does not check the validity of pfnAPC, even if it is
       NULL.  It just does an access violation later on when the APC call
       is attempted */

    pCurrentThread = InternalGetCurrentThread();

    palErr = InternalGetThreadDataFromHandle(
        pCurrentThread,
        hThread,
        &pTargetThread,
        &pTargetThreadObject
        );

    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain thread data for handle %p (error %x)!\n",
                hThread, palErr);
        goto QueueUserAPC_exit;
    }


    palErr = g_pSynchronizationManager->QueueUserAPC(pCurrentThread, pTargetThread,
                                                     pfnAPC, dwData);

QueueUserAPC_exit:
    if (pTargetThreadObject)
    {
        pTargetThreadObject->ReleaseReference(pCurrentThread);
    }

    dwRet = (NO_ERROR == palErr) ? 1 : 0;

    LOGEXIT("QueueUserAPC returns DWORD %d\n", dwRet);
    PERF_EXIT(QueueUserAPC);
    return dwRet;
}

DWORD CorUnix::InternalWaitForMultipleObjectsEx(
    CPalThread * pThread,
    DWORD nCount,
    CONST HANDLE *lpHandles,
    BOOL bWaitAll,
    DWORD dwMilliseconds,
    BOOL bAlertable,
    BOOL bPrioritize)
{
    DWORD dwRet = WAIT_FAILED;
    PAL_ERROR palErr = NO_ERROR;
    int i, iSignaledObjCount, iSignaledObjIndex = -1;
    bool fWAll = (bool)bWaitAll, fNeedToBlock  = false;
    bool fAbandoned = false;
    WaitType wtWaitType;

    IPalObject            * pIPalObjStackArray[MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE] = { NULL };
    ISynchWaitController  * pISyncStackArray[MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE] = { NULL };
    IPalObject           ** ppIPalObjs = pIPalObjStackArray;
    ISynchWaitController ** ppISyncWaitCtrlrs = pISyncStackArray;

    if ((nCount == 0) || (nCount > MAXIMUM_WAIT_OBJECTS))
    {
        ppIPalObjs = NULL;        // make delete at the end safe
        ppISyncWaitCtrlrs = NULL; // make delete at the end safe
        ERROR("Invalid object count=%d [range: 1 to %d]\n",
               nCount, MAXIMUM_WAIT_OBJECTS)
        pThread->SetLastError(ERROR_INVALID_PARAMETER);
        goto WFMOExIntExit;
    }
    else if (nCount == 1)
    {
        fWAll = false;  // makes no difference when nCount is 1
        wtWaitType = SingleObject;
    }
    else
    {
        wtWaitType = fWAll ? MultipleObjectsWaitAll : MultipleObjectsWaitOne;
        if (nCount > MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE)
        {
            ppIPalObjs = InternalNewArray<IPalObject*>(nCount);
            ppISyncWaitCtrlrs = InternalNewArray<ISynchWaitController*>(nCount);
            if ((NULL == ppIPalObjs) || (NULL == ppISyncWaitCtrlrs))
            {
                ERROR("Out of memory allocating internal structures\n");
                pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto WFMOExIntExit;
            }
        }
    }

    palErr = g_pObjectManager->ReferenceMultipleObjectsByHandleArray(pThread,
                                                                     (VOID **)lpHandles,
                                                                     nCount,
                                                                     &sg_aotWaitObject,
                                                                     ppIPalObjs);
    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain object for some or all of the handles [error=%u]\n",
              palErr);
        if (palErr == ERROR_INVALID_HANDLE)
            pThread->SetLastError(ERROR_INVALID_HANDLE);
        else
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
        goto WFMOExIntExit;
    }

    if (nCount > 1)
    {
        // Check for any cross-process sync objects. "Wait for any" and "wait for all" operations are not supported on
        // cross-process sync objects in the PAL.
        for (DWORD i = 0; i < nCount; ++i)
        {
            if (ppIPalObjs[i]->GetObjectType()->GetId() == otiNamedMutex)
            {
                ERROR("Attempt to wait for any or all handles including a cross-process sync object", ERROR_NOT_SUPPORTED);
                pThread->SetLastError(ERROR_NOT_SUPPORTED);
                goto WFMOExIntCleanup;
            }
        }
    }
    else if (ppIPalObjs[0]->GetObjectType()->GetId() == otiNamedMutex)
    {
        SharedMemoryProcessDataHeader *processDataHeader =
            SharedMemoryProcessDataHeader::PalObject_GetProcessDataHeader(ppIPalObjs[0]);
        _ASSERTE(processDataHeader != nullptr);
        try
        {
            MutexTryAcquireLockResult tryAcquireLockResult =
                static_cast<NamedMutexProcessData *>(processDataHeader->GetData())->TryAcquireLock(nullptr, dwMilliseconds);
            switch (tryAcquireLockResult)
            {
                case MutexTryAcquireLockResult::AcquiredLock:
                    dwRet = WAIT_OBJECT_0;
                    break;

                case MutexTryAcquireLockResult::AcquiredLockButMutexWasAbandoned:
                    dwRet = WAIT_ABANDONED_0;
                    break;

                case MutexTryAcquireLockResult::TimedOut:
                    dwRet = WAIT_TIMEOUT;
                    break;

                default:
                    _ASSERTE(false);
                    break;
            }
        }
        catch (SharedMemoryException ex)
        {
            pThread->SetLastError(ex.GetErrorCode());
        }
        goto WFMOExIntCleanup;
    }

    if (fWAll)
    {
        // For a wait-all operation, check for duplicate wait objects in the array. This just uses a brute-force O(n^2)
        // algorithm, but since MAXIMUM_WAIT_OBJECTS is small, the worst case is not so bad, and the average case would involve
        // significantly fewer items.
        for (DWORD i = 0; i < nCount - 1; ++i)
        {
            IPalObject *const objectToCheck = ppIPalObjs[i];
            for (DWORD j = i + 1; j < nCount; ++j)
            {
                if (ppIPalObjs[j] == objectToCheck)
                {
                    ERROR("Duplicate handle provided for a wait-all operation [error=%u]\n", ERROR_INVALID_PARAMETER);
                    pThread->SetLastError(ERROR_INVALID_PARAMETER);
                    goto WFMOExIntCleanup;
                }
            }
        }
    }

    palErr = g_pSynchronizationManager->GetSynchWaitControllersForObjects(
        pThread, ppIPalObjs, nCount, ppISyncWaitCtrlrs);
    if (NO_ERROR != palErr)
    {
        ERROR("Unable to obtain ISynchWaitController interface for some or all "
              "of the objects [error=%u]\n", palErr);
        pThread->SetLastError(ERROR_INTERNAL_ERROR);
        goto WFMOExIntCleanup;
    }

    if (bAlertable)
    {
        // First check for pending APC. We need to do that while holding the global
        // synch lock implicitely grabbed by GetSynchWaitControllersForObjects
        if (g_pSynchronizationManager->AreAPCsPending(pThread))
        {
            // If there is any pending APC we need to release the
            // implicit global synch lock before calling into it
            for (i = 0; (i < (int)nCount) && (NULL != ppISyncWaitCtrlrs[i]); i++)
            {
                ppISyncWaitCtrlrs[i]->ReleaseController();
                ppISyncWaitCtrlrs[i] = NULL;
            }
            palErr = g_pSynchronizationManager->DispatchPendingAPCs(pThread);
            if (NO_ERROR == palErr)
            {
                dwRet = WAIT_IO_COMPLETION;
            }
            else
            {
                ASSERT("Awakened for APC, but no APC is pending\n");
                pThread->SetLastError(ERROR_INTERNAL_ERROR);
                dwRet = WAIT_FAILED;
            }
            goto WFMOExIntCleanup;
        }
    }

    iSignaledObjCount = 0;
    iSignaledObjIndex = -1;
    for (i=0;i<(int)nCount;i++)
    {
        bool fValue, fWaitObjectAbandoned = false;
        palErr = ppISyncWaitCtrlrs[i]->CanThreadWaitWithoutBlocking(&fValue, &fWaitObjectAbandoned);
        if (NO_ERROR != palErr)
        {
            ERROR("ISynchWaitController::CanThreadWaitWithoutBlocking() failed for "
                  "%d-th object [handle=%p error=%u]\n", i, lpHandles[i], palErr);
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
            goto WFMOExIntReleaseControllers;
        }
        if (fWaitObjectAbandoned)
        {
            fAbandoned = true;
        }
        if (fValue)
        {
            iSignaledObjCount++;
            iSignaledObjIndex = i;
            if (!fWAll)
                break;
        }
    }

    fNeedToBlock = (iSignaledObjCount == 0) || (fWAll && (iSignaledObjCount < (int)nCount));
    if (!fNeedToBlock)
    {
        // At least one object signaled, or bWaitAll==TRUE and all object signaled.
        // No need to wait, let's unsignal the object(s) and return without blocking
        int iStartIdx, iEndIdx;

        if (fWAll)
        {
            iStartIdx = 0;
            iEndIdx = nCount;
        }
        else
        {
            iStartIdx = iSignaledObjIndex;
            iEndIdx = iStartIdx + 1;
        }

        // Unsignal objects
        if( iStartIdx < 0 )
        {
            ERROR("Buffer underflow due to iStartIdx < 0");
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
            dwRet = WAIT_FAILED;
            goto WFMOExIntCleanup;
        }
        for (i = iStartIdx; i < iEndIdx; i++)
        {
            palErr = ppISyncWaitCtrlrs[i]->ReleaseWaitingThreadWithoutBlocking();
            if (NO_ERROR != palErr)
            {
                ERROR("ReleaseWaitingThreadWithoutBlocking() failed for %d-th "
                      "object [handle=%p error=%u]\n",
                      i, lpHandles[i], palErr);
                pThread->SetLastError(palErr);
                goto WFMOExIntReleaseControllers;
            }
        }

        dwRet = (fAbandoned ? WAIT_ABANDONED_0 : WAIT_OBJECT_0);
    }
    else if (0 == dwMilliseconds)
    {
        // Not enough objects signaled, but timeout is zero: no actual wait
        dwRet = WAIT_TIMEOUT;
        fNeedToBlock = false;
    }
    else
    {
        // Register the thread for waiting on all objects
        for (i=0;i<(int)nCount;i++)
        {
            palErr = ppISyncWaitCtrlrs[i]->RegisterWaitingThread(
                                                        wtWaitType,
                                                        i,
                                                        (TRUE == bAlertable),
                                                        bPrioritize != FALSE);
            if (NO_ERROR != palErr)
            {
                ERROR("RegisterWaitingThread() failed for %d-th object "
                      "[handle=%p error=%u]\n", i, lpHandles[i], palErr);
                pThread->SetLastError(palErr);
                goto WFMOExIntReleaseControllers;
            }
        }
    }

WFMOExIntReleaseControllers:
    // Release all controllers before going to sleep
    for (i = 0; i < (int)nCount; i++)
    {
        ppISyncWaitCtrlrs[i]->ReleaseController();
        ppISyncWaitCtrlrs[i] = NULL;
    }
    if (NO_ERROR != palErr)
        goto WFMOExIntCleanup;

    if (fNeedToBlock)
    {
        ThreadWakeupReason twrWakeupReason;

        //
        // Going to sleep
        //
        palErr = g_pSynchronizationManager->BlockThread(pThread,
                                                        dwMilliseconds,
                                                        (TRUE == bAlertable),
                                                        false,
                                                        &twrWakeupReason,
                                                        (DWORD *)&iSignaledObjIndex);
        //
        // Awakened
        //
        if (NO_ERROR != palErr)
        {
            ERROR("IPalSynchronizationManager::BlockThread failed for thread "
                  "pThread=%p [error=%u]\n", pThread, palErr);
            pThread->SetLastError(palErr);
            goto WFMOExIntCleanup;
        }
        switch (twrWakeupReason)
        {
        case WaitSucceeded:
            dwRet = WAIT_OBJECT_0; // offset added later
            break;
        case MutexAbandoned:
            dwRet =  WAIT_ABANDONED_0; // offset added later
            break;
        case WaitTimeout:
            dwRet = WAIT_TIMEOUT;
            break;
        case Alerted:
            _ASSERT_MSG(bAlertable,
                        "Awakened for APC from a non-alertable wait\n");

            dwRet = WAIT_IO_COMPLETION;
            palErr = g_pSynchronizationManager->DispatchPendingAPCs(pThread);

            _ASSERT_MSG(NO_ERROR == palErr,
                        "Awakened for APC, but no APC is pending\n");
            break;
        case WaitFailed:
        default:
            ERROR("Thread %p awakened with some failure\n", pThread);
            dwRet = WAIT_FAILED;
            break;
        }
    }

    if (!fWAll && ((WAIT_OBJECT_0 == dwRet) || (WAIT_ABANDONED_0 == dwRet)))
    {
        _ASSERT_MSG(0 <= iSignaledObjIndex,
                    "Failed to identify signaled/abandoned object\n");
        _ASSERT_MSG(iSignaledObjIndex >= 0 && nCount > static_cast<DWORD>(iSignaledObjIndex),
                    "SignaledObjIndex object out of range "
                    "[index=%d obj_count=%u\n",
                    iSignaledObjCount, nCount);

        if (iSignaledObjIndex < 0)
        {
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
            dwRet = WAIT_FAILED;
            goto WFMOExIntCleanup;
        }
        dwRet += iSignaledObjIndex;
    }

WFMOExIntCleanup:
    for (i = 0; i < (int)nCount; i++)
    {
        ppIPalObjs[i]->ReleaseReference(pThread);
        ppIPalObjs[i] = NULL;
    }

WFMOExIntExit:
    if (nCount > MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE)
    {
        InternalDeleteArray(ppIPalObjs);
        InternalDeleteArray(ppISyncWaitCtrlrs);
    }

    return dwRet;
}

DWORD CorUnix::InternalSignalObjectAndWait(
    CPalThread *thread,
    HANDLE hObjectToSignal,
    HANDLE hObjectToWaitOn,
    DWORD dwMilliseconds,
    BOOL bAlertable)
{
    DWORD result = WAIT_FAILED;
    PAL_ERROR palError = NO_ERROR;
    IPalObject *objectToSignal = nullptr;
    IPalObject *objectToWaitOn = nullptr;

    // Validate and add a reference to the object to signal
    palError =
        g_pObjectManager->ReferenceObjectByHandle(
            thread,
            hObjectToSignal,
            &sg_aotSignalableObject,
            &objectToSignal);
    if (palError != NO_ERROR)
    {
        ERROR("Unable to obtain object for handle %p (error %u)!\n", hObjectToSignal, palError);
        goto InternalSignalObjectAndWait_Error;
    }

    // Validate and add a reference to the object to wait on. Error checking is done before signaling.
    palError =
        g_pObjectManager->ReferenceObjectByHandle(
            thread,
            hObjectToWaitOn,
            &sg_aotWaitObject,
            &objectToWaitOn);
    if (palError != NO_ERROR)
    {
        ERROR("Unable to obtain object for handle %p (error %u)!\n", hObjectToWaitOn, palError);
        goto InternalSignalObjectAndWait_Error;
    }

    // Signal
    switch (objectToSignal->GetObjectType()->GetId())
    {
        case otiAutoResetEvent:
        case otiManualResetEvent:
            palError = InternalSetEvent(thread, hObjectToSignal, true /* fSetEvent */);
            break;

        case otiMutex:
        case otiNamedMutex:
            palError = InternalReleaseMutex(thread, hObjectToSignal);
            break;

        case otiSemaphore:
            palError = InternalReleaseSemaphore(thread, hObjectToSignal, 1 /* lReleaseCount */, nullptr /* lpPreviousCount */);
            break;

        default:
            palError = ERROR_INVALID_HANDLE;
            break;
    }
    if (palError != NO_ERROR)
    {
        ERROR("Unable to signal object for handle %p (error %u)!\n", hObjectToSignal, palError);
        goto InternalSignalObjectAndWait_Error;
    }
    objectToSignal->ReleaseReference(thread);
    objectToSignal = nullptr;

    // Wait
    result =
        InternalWaitForMultipleObjectsEx(
            thread,
            1 /* nCount */,
            &hObjectToWaitOn,
            false /* bWaitAll */,
            dwMilliseconds,
            bAlertable);
    if (result == WAIT_FAILED)
    {
        ERROR("Unable to wait on object for handle %p (error %u)!\n", hObjectToWaitOn, palError);
        goto InternalSignalObjectAndWait_Error;
    }
    objectToWaitOn->ReleaseReference(thread);
    objectToWaitOn = nullptr;

    goto InternalSignalObjectAndWait_Exit;

InternalSignalObjectAndWait_Error:
    if (objectToSignal != nullptr)
    {
        objectToSignal->ReleaseReference(thread);
    }
    if (objectToWaitOn != nullptr)
    {
        objectToWaitOn->ReleaseReference(thread);
    }

    if (palError != NO_ERROR)
    {
        _ASSERTE(result == WAIT_FAILED);
        thread->SetLastError(palError);
    }

InternalSignalObjectAndWait_Exit:
    LOGEXIT("InternalSignalObjectAndWait returns %u\n", result);
    return result;
}

DWORD CorUnix::InternalSleepEx (
    CPalThread * pThread,
    DWORD dwMilliseconds,
    BOOL bAlertable)
{
    PAL_ERROR palErr = NO_ERROR;
    DWORD dwRet = WAIT_FAILED;
    int iSignaledObjIndex;

    TRACE("Sleeping %u ms [bAlertable=%d]", dwMilliseconds, (int)bAlertable);

    if (bAlertable)
    {
        // In this case do not use AreAPCsPending. In fact, since we are
        // not holding the synch lock(s) an APC posting may race with
        // AreAPCsPending.
        palErr = g_pSynchronizationManager->DispatchPendingAPCs(pThread);
        if (NO_ERROR == palErr)
        {
            return WAIT_IO_COMPLETION;
        }
    }

    if (dwMilliseconds > 0)
    {
        ThreadWakeupReason twrWakeupReason;
        palErr = g_pSynchronizationManager->BlockThread(pThread,
                                                        dwMilliseconds,
                                                        (TRUE == bAlertable),
                                                        true,
                                                        &twrWakeupReason,
                                                        (DWORD *)&iSignaledObjIndex);
        if (NO_ERROR != palErr)
        {
            ERROR("IPalSynchronizationManager::BlockThread failed for thread "
                  "pThread=%p [error=%u]\n", pThread, palErr);
            return dwRet;
        }

        switch (twrWakeupReason)
        {
        case WaitSucceeded:
        case WaitTimeout:
            dwRet = 0;
            break;
        case Alerted:
            _ASSERT_MSG(bAlertable, "Awakened for APC from a non-alertable wait\n");

            dwRet = WAIT_IO_COMPLETION;
            palErr = g_pSynchronizationManager->DispatchPendingAPCs(pThread);
            _ASSERT_MSG(NO_ERROR == palErr, "Awakened for APC, but no APC is pending\n");

            break;
        case MutexAbandoned:
            ASSERT("Thread %p awakened with reason=MutexAbandoned from a SleepEx\n", pThread);
            break;
        case WaitFailed:
        default:
            ERROR("Thread %p awakened with some failure\n", pThread);
            break;
        }
    }
    else
    {
        sched_yield();
        dwRet = 0;
    }

    TRACE("Done sleeping %u ms [bAlertable=%d]", dwMilliseconds, (int)bAlertable);
    return dwRet;
}

