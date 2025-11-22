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
#include "pal/semaphore.hpp"
#include "pal/dbgmsg.h"
#include <new>

SET_DEFAULT_DEBUG_CHANNEL(SYNC);

#define MAXIMUM_STACK_WAITOBJ_ARRAY_SIZE (MAXIMUM_WAIT_OBJECTS / 4)

using namespace CorUnix;

static PalObjectTypeId sg_rgWaitObjectsIds[] =
    {
        otiAutoResetEvent,
        otiManualResetEvent,
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
                                             dwMilliseconds);

    LOGEXIT("WaitForSingleObject returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForSingleObject);
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
                                             dwMilliseconds);

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
                                             bWaitAll, dwMilliseconds);

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
                                             dwMilliseconds);

    LOGEXIT("WaitForMultipleObjectsEx returns DWORD %u\n", dwRet);
    PERF_EXIT(WaitForMultipleObjectsEx);
    return dwRet;
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

    DWORD internalSleepRet = InternalSleepEx(pThread, dwMilliseconds);

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

    dwRet = InternalSleepEx(pThread, dwMilliseconds);

    LOGEXIT("SleepEx returns DWORD %u\n", dwRet);
    PERF_EXIT(SleepEx);

    return dwRet;
}

DWORD CorUnix::InternalWaitForMultipleObjectsEx(
    CPalThread * pThread,
    DWORD nCount,
    CONST HANDLE *lpHandles,
    BOOL bWaitAll,
    DWORD dwMilliseconds)
{
    DWORD dwRet = WAIT_FAILED;
    PAL_ERROR palErr = NO_ERROR;
    int i, iSignaledObjCount, iSignaledObjIndex = -1;
    bool fWAll = (bool)bWaitAll, fNeedToBlock  = false;
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
            ppIPalObjs = new(std::nothrow) IPalObject*[nCount];
            ppISyncWaitCtrlrs = new(std::nothrow) ISynchWaitController*[nCount];
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

    iSignaledObjCount = 0;
    iSignaledObjIndex = -1;
    for (i=0;i<(int)nCount;i++)
    {
        bool fValue;
        palErr = ppISyncWaitCtrlrs[i]->CanThreadWaitWithoutBlocking(&fValue);
        if (NO_ERROR != palErr)
        {
            ERROR("ISynchWaitController::CanThreadWaitWithoutBlocking() failed for "
                  "%d-th object [handle=%p error=%u]\n", i, lpHandles[i], palErr);
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
            goto WFMOExIntReleaseControllers;
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

        dwRet = WAIT_OBJECT_0;
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
                                                        i);
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
        case WaitTimeout:
            dwRet = WAIT_TIMEOUT;
            break;
        case WaitFailed:
        default:
            ERROR("Thread %p awakened with some failure\n", pThread);
            dwRet = WAIT_FAILED;
            break;
        }
    }

    if (!fWAll && (WAIT_OBJECT_0 == dwRet))
    {
        _ASSERT_MSG(0 <= iSignaledObjIndex,
                    "Failed to identify signaled object\n");
        _ASSERT_MSG(iSignaledObjIndex >= 0 && nCount > static_cast<DWORD>(iSignaledObjIndex),
                    "SignaledObjIndex object out of range "
                    "[index=%d obj_count=%u\n",
                    iSignaledObjIndex, nCount);

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
        delete[] ppIPalObjs;
        delete[] ppISyncWaitCtrlrs;
    }

    return dwRet;
}

DWORD CorUnix::InternalSleepEx (
    CPalThread * pThread,
    DWORD dwMilliseconds)
{
    PAL_ERROR palErr = NO_ERROR;
    DWORD dwRet = WAIT_FAILED;
    int iSignaledObjIndex;

    TRACE("Sleeping %u ms", dwMilliseconds);

    if (dwMilliseconds > 0)
    {
        ThreadWakeupReason twrWakeupReason;
        palErr = g_pSynchronizationManager->BlockThread(pThread,
                                                        dwMilliseconds,
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

    TRACE("Done sleeping %u ms", dwMilliseconds);
    return dwRet;
}

