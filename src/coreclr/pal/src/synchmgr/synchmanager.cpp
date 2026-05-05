// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    synchmanager.cpp

Abstract:
    Implementation of Synchronization Manager and related objects



--*/

#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(SYNC); // some headers have code with asserts, so do this first

#include "synchmanager.hpp"
#include "pal/file.hpp"

#include <sys/types.h>
#include <sys/time.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <unistd.h>
#include <fcntl.h>
#include <limits.h>
#include <sched.h>
#include <signal.h>
#include <errno.h>
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif // HAVE_POLL

#include <algorithm>
#include <new>

const int CorUnix::CThreadSynchronizationInfo::PendingSignalingsArraySize;

namespace CorUnix
{
    /////////////////////////////////
    //                             //
    //   WaitingThreadsListNode    //
    //                             //
    /////////////////////////////////
#ifdef SYNCH_OBJECT_VALIDATION
    _WaitingThreadsListNode::_WaitingThreadsListNode()
    {
        ValidateEmptyObject();
        dwDebugHeadSignature = HeadSignature;
        dwDebugTailSignature = TailSignature;
    }
    _WaitingThreadsListNode::~_WaitingThreadsListNode()
    {
        ValidateObject();
        InvalidateObject();
    }
    void _WaitingThreadsListNode::ValidateObject()
    {
        TRACE("Verifying WaitingThreadsListNode @ %p\n", this);
        _ASSERT_MSG(HeadSignature == dwDebugHeadSignature,
                    "WaitingThreadsListNode header signature corruption [p=%p]",
                    this);
        _ASSERT_MSG(TailSignature == dwDebugTailSignature,
                    "WaitingThreadsListNode trailer signature corruption [p=%p]",
                    this);
    }
    void _WaitingThreadsListNode::ValidateEmptyObject()
    {
        _ASSERT_MSG(HeadSignature != dwDebugHeadSignature,
                    "WaitingThreadsListNode header previously signed [p=%p]",
                    this);
        _ASSERT_MSG(TailSignature != dwDebugTailSignature,
                    "WaitingThreadsListNode trailer previously signed [p=%p]",
                    this);
    }
    void _WaitingThreadsListNode::InvalidateObject()
    {
        TRACE("Invalidating WaitingThreadsListNode @ %p\n", this);
        dwDebugHeadSignature = EmptySignature;
        dwDebugTailSignature = EmptySignature;
    }
#endif // SYNCH_OBJECT_VALIDATION

    //////////////////////////////
    //                          //
    //  CPalSynchMgrController  //
    //                          //
    //////////////////////////////

    /*++
    Method:
      CPalSynchMgrController::CreatePalSynchronizationManager

    Creates the Synchronization Manager. It must be called once per process.
    --*/
    IPalSynchronizationManager * CPalSynchMgrController::CreatePalSynchronizationManager()
    {
        return CPalSynchronizationManager::CreatePalSynchronizationManager();
    };

    /*++
    Method:
      CPalSynchMgrController::PrepareForShutdown

    This method performs the part of Synchronization Manager's shutdown that
    needs to be carried out when core PAL subsystems are still active
    --*/
    PAL_ERROR CPalSynchMgrController::PrepareForShutdown()
    {
        return CPalSynchronizationManager::PrepareForShutdown();
    }

    //////////////////////////////////
    //                              //
    //  CPalSynchronizationManager  //
    //                              //
    //////////////////////////////////

    IPalSynchronizationManager * g_pSynchronizationManager = NULL;

    CPalSynchronizationManager * CPalSynchronizationManager::s_pObjSynchMgr = NULL;
    Volatile<LONG> CPalSynchronizationManager::s_lInitStatus = SynchMgrStatusIdle;
    minipal_mutex CPalSynchronizationManager::s_csSynchProcessLock;

    CPalSynchronizationManager::CPalSynchronizationManager()
        : m_cacheWaitCtrlrs(CtrlrsCacheMaxSize),
          m_cacheStateCtrlrs(CtrlrsCacheMaxSize),
          m_cacheSynchData(SynchDataCacheMaxSize),
          m_cacheSHRSynchData(SynchDataCacheMaxSize),
          m_cacheWTListNodes(WTListNodeCacheMaxSize),
          m_cacheSHRWTListNodes(WTListNodeCacheMaxSize)
    {
    }

    CPalSynchronizationManager::~CPalSynchronizationManager()
    {
    }

    /*++
    Method:
      CPalSynchronizationManager::BlockThread

    Called by a thread to go to sleep for a wait or a sleep

    NOTE: This method must must be called without holding any
          synchronization lock (as well as other locks)
    --*/
    PAL_ERROR CPalSynchronizationManager::BlockThread(
        CPalThread *pthrCurrent,
        DWORD dwTimeout,
        bool fIsSleep,
        ThreadWakeupReason *ptwrWakeupReason,
        DWORD * pdwSignaledObject)
    {
        PAL_ERROR palErr = NO_ERROR;
        ThreadWakeupReason twrWakeupReason = WaitFailed;
        DWORD * pdwWaitState;
        DWORD dwWaitState = 0;
        DWORD dwSigObjIdx = 0;
        bool fEarlyDeath = false;

        pdwWaitState = SharedIDToTypePointer(DWORD,
                pthrCurrent->synchronizationInfo.m_shridWaitAwakened);

        _ASSERT_MSG(NULL != pdwWaitState,
                    "Got NULL pdwWaitState from m_shridWaitAwakened=%p\n",
                    (VOID *)pthrCurrent->synchronizationInfo.m_shridWaitAwakened);

        if (fIsSleep)
        {
            // Setting the thread in wait state
            dwWaitState = TWS_WAITING;

            TRACE("Switching my wait state [%p] from TWS_ACTIVE to %u [current *pdwWaitState=%u]\n",
                    pdwWaitState, dwWaitState, *pdwWaitState);

            dwWaitState = InterlockedCompareExchange((LONG *)pdwWaitState,
                                                        dwWaitState,
                                                        TWS_ACTIVE);

            if ((DWORD)TWS_ACTIVE != dwWaitState)
            {
                if ((DWORD)TWS_EARLYDEATH == dwWaitState)
                {
                    // Process is terminating, this thread will soon be suspended (by SuspendOtherThreads).
                    WARN("Thread is about to get suspended by TerminateProcess\n");

                    fEarlyDeath = true;
                    palErr = WAIT_FAILED;
                }
                else
                {
                    ASSERT("Unexpected thread wait state %u\n", dwWaitState);
                    palErr = ERROR_INTERNAL_ERROR;
                }

                goto BT_exit;
            }
        }

        TRACE("Current thread is about to block for waiting\n");

        palErr = ThreadNativeWait(
            &pthrCurrent->synchronizationInfo.m_tnwdNativeData,
            dwTimeout,
            &twrWakeupReason,
            &dwSigObjIdx);

        if (NO_ERROR != palErr)
        {
            ERROR("ThreadNativeWait() failed [palErr=%d]\n", palErr);
            twrWakeupReason = WaitFailed;
            goto BT_exit;
        }

        TRACE("ThreadNativeWait returned {WakeupReason=%u "
                "dwSigObjIdx=%u}\n", twrWakeupReason, dwSigObjIdx);

        if (WaitTimeout == twrWakeupReason)
        {
            // timeout reached. set wait state back to 'active'
            dwWaitState = TWS_WAITING;

            TRACE("Current thread awakened for timeout: switching wait "
                  "state [%p] from %u to TWS_ACTIVE [current *pdwWaitState=%u]\n",
                   pdwWaitState, dwWaitState, *pdwWaitState);

            DWORD dwOldWaitState = InterlockedCompareExchange(
                                        (LONG *)pdwWaitState,
                                        TWS_ACTIVE, (LONG)dwWaitState);

            switch (dwOldWaitState)
            {
                case TWS_ACTIVE:
                    // We were already ACTIVE; someone decided to wake up this
                    // thread sometime between the moment the native wait
                    // timed out and here. Since the signaling side succeeded
                    // its InterlockedCompareExchange, it will signal the
                    // condition/predicate pair (we just raced overtaking it);
                    // therefore we need to clear the condition/predicate
                    // by waiting on it one more time.
                    // That will also cause this method to report a signal
                    // rather than a timeout.
                    // In the remote signaling scenario, this second wait
                    // also makes sure that the shared id passed over the
                    // process pipe is valid for the entire duration of time
                    // in which the worker thread deals with it
                    TRACE("Current thread already ACTIVE: a signaling raced "
                          "with the timeout: re-waiting natively to clear the "
                          "predicate\n");

                    palErr = ThreadNativeWait(
                        &pthrCurrent->synchronizationInfo.m_tnwdNativeData,
                        SecondNativeWaitTimeout,
                        &twrWakeupReason,
                        &dwSigObjIdx);

                    if (NO_ERROR != palErr)
                    {
                        ERROR("ThreadNativeWait() failed [palErr=%d]\n",
                              palErr);
                        twrWakeupReason = WaitFailed;
                    }

                    if (WaitTimeout == twrWakeupReason)
                    {
                        ERROR("Second native wait timed out\n");
                    }

                    break;
                case TWS_EARLYDEATH:
                    // Thread is about to be suspended by TerminateProcess.
                    // Anyway, if the wait timed out, we still want to
                    // (try to) unregister the wait (especially if it
                    // involves shared objects)
                    WARN("Thread is about to be suspended by TerminateProcess\n");
                    fEarlyDeath = true;
                    palErr = WAIT_FAILED;
                    break;
                case TWS_WAITING:
                default:
                    _ASSERT_MSG(dwOldWaitState == dwWaitState,
                                "Unexpected wait status: actual=%u, expected=%u\n",
                               dwOldWaitState, dwWaitState);
                    break;
            }
        }

        switch (twrWakeupReason)
        {
            case WaitTimeout:
            {
                // Awakened for timeout: we need to unregister the wait
                ThreadWaitInfo * ptwiWaitInfo;

                TRACE("Current thread awakened for timeout: unregistering the wait\n");

                // Local lock
                AcquireLocalSynchLock(pthrCurrent);

                ptwiWaitInfo = GetThreadWaitInfo(pthrCurrent);

                // Unregister the wait
                UnRegisterWait(pthrCurrent, ptwiWaitInfo);

                // Unlock
                ReleaseLocalSynchLock(pthrCurrent);

                break;
            }
            case WaitSucceeded:
                *pdwSignaledObject = dwSigObjIdx;
                break;
            default:
                // 'WaitFailed' goes through this case
                break;
        }

        // Set the returned wakeup reason
        *ptwrWakeupReason = twrWakeupReason;

        TRACE("Current thread is now active [WakeupReason=%u SigObjIdx=%u]\n",
              twrWakeupReason, dwSigObjIdx);

        _ASSERT_MSG(TWS_ACTIVE == VolatileLoad(pdwWaitState) ||
                    TWS_EARLYDEATH == VolatileLoad(pdwWaitState),
                    "Unexpected thread wait state %u\n", VolatileLoad(pdwWaitState));

    BT_exit:
        if (fEarlyDeath)
        {
            ThreadPrepareForShutdown();
        }

        return palErr;
    }

    PAL_ERROR CPalSynchronizationManager::ThreadNativeWait(
        ThreadNativeWaitData * ptnwdNativeWaitData,
        DWORD dwTimeout,
        ThreadWakeupReason * ptwrWakeupReason,
        DWORD * pdwSignaledObject)
    {
        PAL_ERROR palErr = NO_ERROR;
        int iRet, iWaitRet = 0;
        struct timespec tsAbsTmo;

        TRACE("ThreadNativeWait(ptnwdNativeWaitData=%p, dwTimeout=%u, ...)\n",
              ptnwdNativeWaitData, dwTimeout);

        if (dwTimeout != INFINITE)
        {
            // Calculate absolute timeout
            palErr = GetAbsoluteTimeout(dwTimeout, &tsAbsTmo);
            if (NO_ERROR != palErr)
            {
                ERROR("Failed to convert timeout to absolute timeout\n");
                goto TNW_exit;
            }
        }

        // Lock the mutex
        iRet = pthread_mutex_lock(&ptnwdNativeWaitData->mutex);
        if (0 != iRet)
        {
            ERROR("Internal Error: cannot lock mutex\n");
            palErr = ERROR_INTERNAL_ERROR;
            *ptwrWakeupReason = WaitFailed;
            goto TNW_exit;
        }

#ifndef FEATURE_MULTITHREADING
        // In single-threaded WASM, if the object is not already signaled (iPred == FALSE),
        // we cannot wait because there is no other thread to signal us - this would deadlock.
        // This is a programming error in single-threaded WASM.
        _ASSERT_MSG(FALSE != ptnwdNativeWaitData->iPred, "Cannot wait in single-threaded mode\n");
        if (FALSE == ptnwdNativeWaitData->iPred)
        {
            iRet = pthread_mutex_unlock(&ptnwdNativeWaitData->mutex);
            palErr = ERROR_NOT_SUPPORTED;
            *ptwrWakeupReason = WaitFailed;
        }
#else // !FEATURE_MULTITHREADING

        while (FALSE == ptnwdNativeWaitData->iPred)
        {
            if (INFINITE == dwTimeout)
            {
                iWaitRet = pthread_cond_wait(&ptnwdNativeWaitData->cond,
                                             &ptnwdNativeWaitData->mutex);
            }
            else
            {
                iWaitRet = pthread_cond_timedwait(&ptnwdNativeWaitData->cond,
                                                  &ptnwdNativeWaitData->mutex,
                                                  &tsAbsTmo);
            }

            if (ETIMEDOUT == iWaitRet)
            {
                _ASSERT_MSG(INFINITE != dwTimeout,
                            "Got ETIMEDOUT despite timeout being INFINITE\n");
                break;
            }
            else if (0 != iWaitRet)
            {
                ERROR("pthread_cond_%swait returned %d [errno=%d (%s)]\n",
                       (INFINITE == dwTimeout) ? "" : "timed",
                       iWaitRet, errno, strerror(errno));
                palErr = ERROR_INTERNAL_ERROR;
                break;
            }
        }

        // Reset the predicate
        if (0 == iWaitRet)
        {
            // We don't want to reset the predicate if pthread_cond_timedwait
            // timed out racing with a pthread_cond_signal. When
            // pthread_cond_timedwait times out, it needs to grab the mutex
            // before returning. At timeout time, it may happen that the
            // signaling thread just grabbed the mutex, but it hasn't called
            // pthread_cond_signal yet. In this scenario pthread_cond_timedwait
            // will have to wait for the signaling side to release the mutex.
            // As a result it will return with error timeout, but the predicate
            // will be set. Since pthread_cond_timedwait timed out, the
            // predicate value is intended for the next signal. In case of a
            // object signaling racing with a wait timeout this predicate value
            // will be picked up by the 'second native wait' (see comments in
            // BlockThread).

            ptnwdNativeWaitData->iPred = FALSE;
        }

        // Unlock the mutex
        iRet = pthread_mutex_unlock(&ptnwdNativeWaitData->mutex);
        if (0 != iRet)
        {
            ERROR("Cannot unlock mutex [err=%d]\n", iRet);
            palErr = ERROR_INTERNAL_ERROR;
            goto TNW_exit;
        }

        _ASSERT_MSG(ETIMEDOUT != iRet || INFINITE != dwTimeout, "Got timeout return code with INFINITE timeout\n");

        if (0 == iWaitRet)
        {
            *ptwrWakeupReason  = ptnwdNativeWaitData->twrWakeupReason;
            *pdwSignaledObject = ptnwdNativeWaitData->dwObjectIndex;
        }
        else if (ETIMEDOUT == iWaitRet)
        {
            *ptwrWakeupReason = WaitTimeout;
        }

#endif // !FEATURE_MULTITHREADING
    TNW_exit:
        TRACE("ThreadNativeWait: returning %u [WakeupReason=%u]\n", palErr, *ptwrWakeupReason);
        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::GetSynchWaitControllersForObjects

    Returns an array of wait controllers, one for each of the objects
    in rgObjects
    --*/
    PAL_ERROR CPalSynchronizationManager::GetSynchWaitControllersForObjects(
        CPalThread *pthrCurrent,
        IPalObject *rgObjects[],
        DWORD dwObjectCount,
        ISynchWaitController * rgControllers[])
    {
        return GetSynchControllersForObjects(pthrCurrent,
                                             rgObjects,
                                             dwObjectCount,
                                             (void **)rgControllers,
                                             CSynchControllerBase::WaitController);
    }

    /*++
    Method:
      CPalSynchronizationManager::GetSynchStateControllersForObjects

    Returns an array of state controllers, one for each of the objects
    in rgObjects
    --*/
    PAL_ERROR CPalSynchronizationManager::GetSynchStateControllersForObjects(
        CPalThread *pthrCurrent,
        IPalObject *rgObjects[],
        DWORD dwObjectCount,
        ISynchStateController *rgControllers[])
    {
        return GetSynchControllersForObjects(pthrCurrent,
                                             rgObjects,
                                             dwObjectCount,
                                             (void **)rgControllers,
                                             CSynchControllerBase::StateController);
    }

    /*++
    Method:
      CPalSynchronizationManager::GetSynchControllersForObjects

    Internal common implementation for GetSynchWaitControllersForObjects and
    GetSynchStateControllersForObjects
    --*/
    PAL_ERROR CPalSynchronizationManager::GetSynchControllersForObjects(
        CPalThread *pthrCurrent,
        IPalObject *rgObjects[],
        DWORD dwObjectCount,
        void ** ppvControllers,
        CSynchControllerBase::ControllerType ctCtrlrType)
    {
        PAL_ERROR palErr = NO_ERROR;
        unsigned int uIdx, uCount = 0, uSharedObjectCount = 0;
        CObjectType * potObjectType = NULL;
        unsigned int uErrCleanupIdxFirstNotInitializedCtrlr = 0;
        unsigned int uErrCleanupIdxLastCtrlr = 0;
        bool fLocalSynchLock = false;

        union
        {
            CSynchWaitController * pWaitCtrlrs[MAXIMUM_WAIT_OBJECTS];
            CSynchStateController * pStateCtrlrs[MAXIMUM_WAIT_OBJECTS];
        } Ctrlrs;

        if ((dwObjectCount <= 0) || (dwObjectCount > MAXIMUM_WAIT_OBJECTS))
        {
            palErr = ERROR_INVALID_PARAMETER;
            goto GSCFO_exit;
        }

        if (CSynchControllerBase::WaitController == ctCtrlrType)
        {
            uCount = (unsigned int)m_cacheWaitCtrlrs.Get(pthrCurrent,
                                                         dwObjectCount,
                                                         Ctrlrs.pWaitCtrlrs);
        }
        else
        {
            uCount = (unsigned int)m_cacheStateCtrlrs.Get(pthrCurrent,
                                                          dwObjectCount,
                                                          Ctrlrs.pStateCtrlrs);
        }

        if (uCount < dwObjectCount)
        {
            // We got less controllers (uCount) than we asked for (dwObjectCount),
            // probably because of low memory.
            // None of these controllers is initialized, so they must be all
            // returned directly to the cache
            uErrCleanupIdxLastCtrlr = uCount;

            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto GSCFO_error_cleanup;
        }

        //
        // We need to acquire the local synch lock before evaluating object domains
        //
        AcquireLocalSynchLock(pthrCurrent);
        fLocalSynchLock = true;

        for (uIdx=0;uIdx<dwObjectCount;uIdx++)
        {
            void * pvSData;
            CSynchData * psdSynchData;

            palErr = rgObjects[uIdx]->GetObjectSynchData((void **)&pvSData);
            if (NO_ERROR != palErr)
            {
                break;
            }

            psdSynchData = static_cast<CSynchData *>(pvSData);

            VALIDATEOBJECT(psdSynchData);

            potObjectType = rgObjects[uIdx]->GetObjectType();

            if (CSynchControllerBase::WaitController == ctCtrlrType)
            {
                Ctrlrs.pWaitCtrlrs[uIdx]->Init(pthrCurrent,
                                            ctCtrlrType,
                                            potObjectType,
                                            psdSynchData);
            }
            else
            {
                Ctrlrs.pStateCtrlrs[uIdx]->Init(pthrCurrent,
                                             ctCtrlrType,
                                             potObjectType,
                                             psdSynchData);
            }
        }
        if (NO_ERROR != palErr)
        {
            // An error occurred while initializing the (uIdx+1)-th controller,
            // i.e. the one at index uIdx; therefore the first uIdx controllers
            // must be Release'd, while the remaining uCount-uIdx must be returned
            // directly to the cache.
            uErrCleanupIdxFirstNotInitializedCtrlr = uIdx;
            uErrCleanupIdxLastCtrlr = dwObjectCount;

            goto GSCFO_error_cleanup;
        }

        // Succeeded
        if (CSynchControllerBase::WaitController == ctCtrlrType)
        {
            for (uIdx=0;uIdx<dwObjectCount;uIdx++)
            {
                // The multiple cast is NEEDED, though currently it does not
                // change the value ot the pointer. Anyway, if in the future
                // a virtual method should be added to the base class
                // CSynchControllerBase, both derived classes would have two
                // virtual tables, therefore a static cast from, for instance,
                // a CSynchWaitController* to a ISynchWaitController* would
                // return the given pointer incremented by the size of a
                // generic pointer on the specific platform
                ppvControllers[uIdx] = reinterpret_cast<void *>(
                    static_cast<ISynchWaitController *>(Ctrlrs.pWaitCtrlrs[uIdx]));
            }
        }
        else
        {
            for (uIdx=0;uIdx<dwObjectCount;uIdx++)
            {
                // See comment above
                ppvControllers[uIdx] = reinterpret_cast<void *>(
                    static_cast<ISynchStateController *>(Ctrlrs.pStateCtrlrs[uIdx]));
            }
        }

        // Succeeded: skip error cleanup
        goto GSCFO_exit;

    GSCFO_error_cleanup:
        if (CSynchControllerBase::WaitController == ctCtrlrType)
        {
            // Release already initialized wait controllers
            for (uIdx=0; uIdx<uErrCleanupIdxFirstNotInitializedCtrlr; uIdx++)
            {
                Ctrlrs.pWaitCtrlrs[uIdx]->Release();
            }

            // Return to the cache not yet initialized wait controllers
            for (uIdx=uErrCleanupIdxFirstNotInitializedCtrlr; uIdx<uErrCleanupIdxLastCtrlr; uIdx++)
            {
                m_cacheWaitCtrlrs.Add(pthrCurrent, Ctrlrs.pWaitCtrlrs[uIdx]);
            }
        }
        else
        {
            // Release already initialized state controllers
            for (uIdx=0; uIdx<uErrCleanupIdxFirstNotInitializedCtrlr; uIdx++)
            {
                Ctrlrs.pStateCtrlrs[uIdx]->Release();
            }

            // Return to the cache not yet initialized state controllers
            for (uIdx=uErrCleanupIdxFirstNotInitializedCtrlr; uIdx<uErrCleanupIdxLastCtrlr; uIdx++)
            {
                m_cacheStateCtrlrs.Add(pthrCurrent, Ctrlrs.pStateCtrlrs[uIdx]);
            }
        }

    GSCFO_exit:
        if (fLocalSynchLock)
        {
            ReleaseLocalSynchLock(pthrCurrent);
        }
        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::AllocateObjectSynchData

    Returns a new SynchData for an object of given type and domain
    --*/
    PAL_ERROR CPalSynchronizationManager::AllocateObjectSynchData(
        CObjectType *potObjectType,
        VOID **ppvSynchData)
    {
        PAL_ERROR palErr = NO_ERROR;
        CSynchData * psdSynchData = NULL;
        CPalThread * pthrCurrent = InternalGetCurrentThread();

        psdSynchData = m_cacheSynchData.Get(pthrCurrent);
        if (NULL == psdSynchData)
        {
            ERROR("Unable to allocate memory\n");
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        // Initialize waiting list pointers
        psdSynchData->SetWTLHeadPtr(NULL);
        psdSynchData->SetWTLTailPtr(NULL);

        *ppvSynchData = static_cast<void *>(psdSynchData);

        // Initialize object domain and object type;
        psdSynchData->SetObjectType(potObjectType);

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::FreeObjectSynchData

    Called to return a no longer used SynchData to the Synchronization Manager.
    The SynchData may actually survive this call, since it is a ref-counted
    object and at FreeObjectSynchData time it may still be used from within
    the Synchronization Manager itself (e.g. the worker thread).
    --*/
    void CPalSynchronizationManager::FreeObjectSynchData(
        CObjectType *potObjectType,
        VOID *pvSynchData)
    {
        CSynchData * psdSynchData;
        CPalThread * pthrCurrent = InternalGetCurrentThread();

        psdSynchData = static_cast<CSynchData *>(pvSynchData);

        psdSynchData->Release(pthrCurrent);
    }

    /*++
    Method:
      CPalSynchronizationManager::CreateSynchStateController

    Creates a state controller for the given object
    --*/
    PAL_ERROR CPalSynchronizationManager::CreateSynchStateController(
        CPalThread *pthrCurrent,
        CObjectType *potObjectType,
        VOID *pvSynchData,
        ISynchStateController **ppStateController)
    {
        PAL_ERROR palErr = NO_ERROR;
        CSynchStateController * pCtrlr =  NULL;
        CSynchData * psdSynchData;

        psdSynchData = static_cast<CSynchData *>(pvSynchData);

        VALIDATEOBJECT(psdSynchData);

        pCtrlr = m_cacheStateCtrlrs.Get(pthrCurrent);
        if (NULL == pCtrlr)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        pCtrlr->Init(pthrCurrent,
                     CSynchControllerBase::StateController,
                     potObjectType,
                     psdSynchData);

        // Succeeded
        *ppStateController = (ISynchStateController *)pCtrlr;

        if (NO_ERROR != palErr)
        {
            m_cacheStateCtrlrs.Add(pthrCurrent, pCtrlr);
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::CreateSynchWaitController

    Creates a wait controller for the given object
    --*/
    PAL_ERROR CPalSynchronizationManager::CreateSynchWaitController(
        CPalThread *pthrCurrent,
        CObjectType *potObjectType,
        VOID *pvSynchData,
        ISynchWaitController **ppWaitController)
    {
        CSynchWaitController * pCtrlr =  NULL;
        CSynchData * psdSynchData;

        psdSynchData = static_cast<CSynchData *>(pvSynchData);

        VALIDATEOBJECT(psdSynchData);

        pCtrlr = m_cacheWaitCtrlrs.Get(pthrCurrent);
        if (NULL == pCtrlr)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        pCtrlr->Init(pthrCurrent,
                     CSynchControllerBase::WaitController,
                     potObjectType,
                     psdSynchData);

        // Succeeded
        *ppWaitController = (ISynchWaitController *)pCtrlr;

        return NO_ERROR;
    }

    /*++
    Method:
      CPalSynchronizationManager::CreatePalSynchronizationManager

    Creates the Synchronization Manager.
    Private method, it is called only by CPalSynchMgrController.
    --*/
    IPalSynchronizationManager * CPalSynchronizationManager::CreatePalSynchronizationManager()
    {
        if (s_pObjSynchMgr != NULL)
        {
            ASSERT("Multiple PAL Synchronization manager initializations\n");
            return NULL;
        }

        Initialize();
        return static_cast<IPalSynchronizationManager *>(s_pObjSynchMgr);
    }

    /*++
    Method:
      CPalSynchronizationManager::Initialize

    Internal Synchronization Manager initialization
    --*/
    PAL_ERROR CPalSynchronizationManager::Initialize()
    {
        PAL_ERROR palErr = NO_ERROR;
        LONG lInit;
        CPalSynchronizationManager * pSynchManager = NULL;

        lInit = InterlockedCompareExchange(&s_lInitStatus,
                                           (LONG)SynchMgrStatusInitializing,
                                           (LONG)SynchMgrStatusIdle);

        if ((LONG)SynchMgrStatusIdle != lInit)
        {
            ASSERT("Synchronization Manager already being initialized");
            palErr = ERROR_INTERNAL_ERROR;
            goto I_exit;
        }

        minipal_mutex_init(&s_csSynchProcessLock);

        pSynchManager = new(std::nothrow) CPalSynchronizationManager();
        if (NULL == pSynchManager)
        {
            ERROR("Failed to allocate memory for Synchronization Manager");
            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto I_exit;
        }

        s_pObjSynchMgr = pSynchManager;

        // Initialization was successful
        g_pSynchronizationManager =
            static_cast<IPalSynchronizationManager *>(pSynchManager);
        s_lInitStatus = (LONG)SynchMgrStatusRunning;

    I_exit:
        if (NO_ERROR != palErr)
        {
            s_lInitStatus = (LONG)SynchMgrStatusError;

            s_pObjSynchMgr = NULL;
            g_pSynchronizationManager = NULL;
            delete pSynchManager;
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::PrepareForShutdown

    This method performs the part of Synchronization Manager's shutdown that
    needs to be carried out when core PAL subsystems are still active.
    Private method, it is called only by CPalSynchMgrController.
    --*/
    PAL_ERROR CPalSynchronizationManager::PrepareForShutdown()
    {
        PAL_ERROR palErr = NO_ERROR;

        LONG lInit = InterlockedCompareExchange(&s_lInitStatus,
            (LONG)SynchMgrStatusShuttingDown, (LONG)SynchMgrStatusRunning);

        if ((LONG)SynchMgrStatusRunning != lInit)
        {
            ASSERT("Unexpected initialization status found "
                   "in PrepareForShutdown [expected=%d current=%d]\n",
                   SynchMgrStatusRunning, lInit);
            // We intentionally do not set s_lInitStatus to SynchMgrStatusError
            // because this could interfere with a previous thread already
            // executing shutdown.
            return ERROR_INTERNAL_ERROR;
        }

        // Ready for process shutdown.
        s_lInitStatus = SynchMgrStatusReadyForProcessShutDown;
        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::WakeUpLocalThread

    Wakes up a local thead currently sleeping for a wait or a sleep
    --*/
    PAL_ERROR CPalSynchronizationManager::WakeUpLocalThread(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget,
        ThreadWakeupReason twrWakeupReason,
        DWORD dwObjectIndex)
    {
        PAL_ERROR palErr = NO_ERROR;
        ThreadNativeWaitData * ptnwdNativeWaitData =
            pthrTarget->synchronizationInfo.GetNativeData();

        TRACE("Waking up a local thread [WakeUpReason=%u ObjectIndex=%u "
              "ptnwdNativeWaitData=%p]\n", twrWakeupReason, dwObjectIndex,
              ptnwdNativeWaitData);

        // Set wakeup reason and signaled object index
        ptnwdNativeWaitData->twrWakeupReason = twrWakeupReason;
        ptnwdNativeWaitData->dwObjectIndex   = dwObjectIndex;

#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        if (0 < GetLocalSynchLockCount(pthrCurrent))
        {
            // Defer the actual thread signaling to right after
            // releasing the synch lock(s), so that signaling
            // can happen from a thread-suspension safe area
            palErr = DeferThreadConditionSignaling(pthrCurrent, pthrTarget);
        }
        else
        {
            // Signal the target thread's condition
            palErr = SignalThreadCondition(ptnwdNativeWaitData);
        }
#else // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        // Signal the target thread's condition
        palErr = SignalThreadCondition(ptnwdNativeWaitData);
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING

        return palErr;
    }

#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
    /*++
    Method:
      CPalSynchronizationManager::DeferThreadConditionSignaling

    Defers thread signaling to the final release of synchronization
    lock(s), so that condition signaling can happen when the signaling
    thread is marked as safe for thread suspension.
    --*/
    PAL_ERROR CPalSynchronizationManager::DeferThreadConditionSignaling(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget)
    {
        PAL_ERROR palErr = NO_ERROR;
        LONG lCount = pthrCurrent->synchronizationInfo.m_lPendingSignalingCount;

        _ASSERTE(pthrTarget != pthrCurrent);

        if (CThreadSynchronizationInfo::PendingSignalingsArraySize > lCount)
        {
            // If there is available room, add the target thread object to
            // the array of pending thread signalings.
            pthrCurrent->synchronizationInfo.m_rgpthrPendingSignalings[lCount] = pthrTarget;
        }
        else
        {
            // If the array is full, add the target thread object at the end
            // of the overflow list
            DeferredSignalingListNode * pdsln =
                new(std::nothrow) DeferredSignalingListNode();

            if (pdsln)
            {
                pdsln->pthrTarget = pthrTarget;

                // Add the note to the end of the list.
                // Note: no need to synchronize the access to this list since
                // it is meant to be accessed only by the owner thread.
                InsertTailList(&pthrCurrent->synchronizationInfo.m_lePendingSignalingsOverflowList,
                               &pdsln->Link);
            }
            else
            {
                palErr = ERROR_NOT_ENOUGH_MEMORY;
            }
        }

        if (NO_ERROR == palErr)
        {
            // Increment the count of pending signalings
            pthrCurrent->synchronizationInfo.m_lPendingSignalingCount += 1;

            // Add a reference to the target CPalThread object; this is
            // needed since deferring signaling after releasing the synch
            // locks implies accessing the target thread object without
            // holding the local synch lock. In rare circumstances, the
            // target thread may have already exited while deferred signaling
            // takes place, therefore invalidating the thread object. The
            // reference added here ensures that the thread object is still
            // good, even if the target thread has exited.
            pthrTarget->AddThreadReference();
        }

        return palErr;
    }
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING

    /*++
    Method:
      CPalSynchronizationManager::SignalThreadCondition

    Performs the actual condition signaling in to wake up the target thread
    --*/
    PAL_ERROR CPalSynchronizationManager::SignalThreadCondition(
        ThreadNativeWaitData * ptnwdNativeWaitData)
    {
        PAL_ERROR palErr = NO_ERROR;
        int iRet;

        // Lock the mutex
        iRet = pthread_mutex_lock(&ptnwdNativeWaitData->mutex);
        if (0 != iRet)
        {
            ERROR("Cannot lock mutex [err=%d]\n", iRet);
            return ERROR_INTERNAL_ERROR;
        }

        // Set the predicate
        ptnwdNativeWaitData->iPred = TRUE;

        // Signal the condition
        iRet = pthread_cond_signal(&ptnwdNativeWaitData->cond);
        if (0 != iRet)
        {
            ERROR("Failed to signal condition: pthread_cond_signal "
                  "returned %d [errno=%d (%s)]\n", iRet, errno,
                  strerror(errno));
            palErr = ERROR_INTERNAL_ERROR;
            // Continue in order to unlock the mutex anyway
        }

        // Unlock the mutex
        iRet = pthread_mutex_unlock(&ptnwdNativeWaitData->mutex);
        if (0 != iRet)
        {
            ERROR("Cannot unlock mutex [err=%d]\n", iRet);
            return ERROR_INTERNAL_ERROR;
        }

        return palErr;
    }


    /*++
    Method:
      CPalSynchronizationManager::GetThreadWaitInfo

    Returns a pointer to the WaitInfo structure for the passed CPalThread object
    --*/
    ThreadWaitInfo * CPalSynchronizationManager::GetThreadWaitInfo(
        CPalThread * pthrCurrent)
    {
        return &pthrCurrent->synchronizationInfo.m_twiWaitInfo;
    }

    /*++
    Method:
      CPalSynchronizationManager::UnRegisterWait

    Unregister the wait described by ptwiWaitInfo that in general involves
    a thread other than the current one (most of the times the deregistration
    is performed by the signaling thread)

    Note: this method must be called while holding the local process
          synchronization lock.
    --*/
    void CPalSynchronizationManager::UnRegisterWait(
        CPalThread * pthrCurrent,
        ThreadWaitInfo * ptwiWaitInfo)
    {
        int i = 0;
        CSynchData * psdSynchData = NULL;

        TRACE("Unregistering wait for thread=%u [ObjCount=%d WaitType=%u]\n",
              ptwiWaitInfo->pthrOwner->GetThreadId(),
              ptwiWaitInfo->lObjCount, ptwiWaitInfo->wtWaitType);

        for (i=0; i < ptwiWaitInfo->lObjCount; i++)
        {
            WaitingThreadsListNode * pwtlnItem = ptwiWaitInfo->rgpWTLNodes[i];

            VALIDATEOBJECT(pwtlnItem);

            psdSynchData = pwtlnItem->ptrOwnerObjSynchData.ptr;

            VALIDATEOBJECT(psdSynchData);

            if (pwtlnItem->ptrPrev.ptr)
            {
                VALIDATEOBJECT(pwtlnItem);
                pwtlnItem->ptrPrev.ptr->ptrNext.ptr = pwtlnItem->ptrNext.ptr;
            }
            else
            {
                psdSynchData->SetWTLHeadPtr(pwtlnItem->ptrNext.ptr);
            }

            if (pwtlnItem->ptrNext.ptr)
            {
                VALIDATEOBJECT(pwtlnItem);
                pwtlnItem->ptrNext.ptr->ptrPrev.ptr = pwtlnItem->ptrPrev.ptr;
            }
            else
            {
                psdSynchData->SetWTLTailPtr(pwtlnItem->ptrPrev.ptr);
            }

            m_cacheWTListNodes.Add(pthrCurrent, pwtlnItem);

            // Release the node's refcount on the synch data, and decerement
            // waiting thread count
            psdSynchData->DecrementWaitingThreadCount();
            psdSynchData->Release(pthrCurrent);
        }

        // Reset wait data in ThreadWaitInfo structure: it is enough
        // to reset lObjCount.
        ptwiWaitInfo->lObjCount       = 0;

        return;
    }

    /*++
    Method:
      CPalSynchronizationManager::UnsignalRestOfLocalAwakeningWaitAll

    Unsignals all the objects involved in a wait all, except the target
    one (i.e. psdTgtObjectSynchData)

    Note: this method must be called while holding the synchronization locks
          appropriate to all the objects involved in the wait-all. If any
          of the objects is shared, the caller must own both local and
          shared synch locks; if no shared object is involved in the wait,
          only the local synch lock is needed.
    --*/
    void CPalSynchronizationManager::UnsignalRestOfLocalAwakeningWaitAll(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget,
        WaitingThreadsListNode * pwtlnNode,
        CSynchData * psdTgtObjectSynchData)
    {
        PAL_ERROR palErr = NO_ERROR;
        CSynchData * psdSynchDataItem = NULL;

#ifdef _DEBUG
        bool bOriginatingNodeFound = false;
#endif

        VALIDATEOBJECT(psdTgtObjectSynchData);
        VALIDATEOBJECT(pwtlnNode);

        _ASSERT_MSG(0 != (WTLN_FLAG_WAIT_ALL & pwtlnNode->dwFlags),
            "UnsignalRestOfLocalAwakeningWaitAll() called on a normal (non wait all) wait");

        ThreadWaitInfo *ptwiWaitInfo = pwtlnNode->ptwiWaitInfo;

        int iObjCount = ptwiWaitInfo->lObjCount;
        for (int i = 0; i < iObjCount; i++)
        {
            WaitingThreadsListNode * pwtlnItem = ptwiWaitInfo->rgpWTLNodes[i];

            VALIDATEOBJECT(pwtlnItem);

            psdSynchDataItem = pwtlnItem->ptrOwnerObjSynchData.ptr;

            VALIDATEOBJECT(psdSynchDataItem);

            // Skip originating node
            if (psdTgtObjectSynchData == psdSynchDataItem)
            {
#ifdef _DEBUG
                bOriginatingNodeFound = true;
#endif
                continue;
            }

            palErr = psdSynchDataItem->ReleaseWaiterWithoutBlocking(pthrCurrent, pthrTarget);
            if (NO_ERROR != palErr)
            {
                ERROR("ReleaseWaiterWithoutBlocking failed on SynchData @ %p [palErr = %u]\n", psdSynchDataItem, palErr);
            }
        }

        _ASSERT_MSG(bOriginatingNodeFound, "Couldn't find originating node while unsignaling rest of the wait all\n");
    }



    /*++
    Method:
      CPalSynchronizationManager::ThreadPrepareForShutdown

    Used to hijack thread execution from known spots within the
    Synchronization Manager in case a PAL shutdown is initiated
    or the thread is being terminated by another thread.
    --*/
    void CPalSynchronizationManager::ThreadPrepareForShutdown()
    {
        TRACE("The Synchronization Manager hijacked the current thread "
              "for process shutdown or thread termination\n");
        while (true)
        {
            poll(NULL, 0, INFTIM);
            sched_yield();
        }

        ASSERT("This code should never be executed\n");
    }



    /*++
    Method:
      CPalSynchronizationManager::AcquireProcessLock

    Acquires the local Process Lock (which currently is the same as the
    the local Process Synch Lock)
    --*/
    void CPalSynchronizationManager::AcquireProcessLock(CPalThread * pthrCurrent)
    {
        AcquireLocalSynchLock(pthrCurrent);
    }

    /*++
    Method:
      CPalSynchronizationManager::ReleaseProcessLock

    Releases the local Process Lock (which currently is the same as the
    the local Process Synch Lock)
    --*/
    void CPalSynchronizationManager::ReleaseProcessLock(CPalThread * pthrCurrent)
    {
        ReleaseLocalSynchLock(pthrCurrent);
    }

    /////////////////////////////
    //                         //
    //  _ThreadNativeWaitData  //
    //                         //
    /////////////////////////////

    _ThreadNativeWaitData::~_ThreadNativeWaitData()
    {
        if (fInitialized)
        {
            fInitialized = false;
            pthread_cond_destroy(&cond);
            pthread_mutex_destroy(&mutex);
        }
    }


    //////////////////////////////////
    //                              //
    //  CThreadSynchronizationInfo  //
    //                              //
    //////////////////////////////////

    CThreadSynchronizationInfo::CThreadSynchronizationInfo() :
            m_tsThreadState(TS_IDLE),
            m_shridWaitAwakened(NULL),
            m_lLocalSynchLockCount(0)
    {
        InitializeListHead(&m_leOwnedObjsList);

#ifdef SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        m_lPendingSignalingCount = 0;
        InitializeListHead(&m_lePendingSignalingsOverflowList);
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
    }

    CThreadSynchronizationInfo::~CThreadSynchronizationInfo()
    {
        if (NULL != m_shridWaitAwakened)
        {
            free(m_shridWaitAwakened);
        }
    }

    void CThreadSynchronizationInfo::AcquireNativeWaitLock()
    {
#if !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        int iRet;
        iRet = pthread_mutex_lock(&m_tnwdNativeData.mutex);
        _ASSERT_MSG(0 == iRet, "pthread_mutex_lock failed with error=%d\n", iRet);
#endif // !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
    }

    void CThreadSynchronizationInfo::ReleaseNativeWaitLock()
    {
#if !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        int iRet;
        iRet = pthread_mutex_unlock(&m_tnwdNativeData.mutex);
        _ASSERT_MSG(0 == iRet, "pthread_mutex_unlock failed with error=%d\n", iRet);
#endif // !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
    }

    bool CThreadSynchronizationInfo::TryAcquireNativeWaitLock()
    {
        bool fRet = true;
#if !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        int iRet;
        iRet = pthread_mutex_trylock(&m_tnwdNativeData.mutex);
        _ASSERT_MSG(0 == iRet || EBUSY == iRet,
                    "pthread_mutex_trylock failed with error=%d\n", iRet);
        fRet = (0 == iRet);
#endif // !SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        return fRet;
    }

    /*++
    Method:
      CThreadSynchronizationInfo::InitializePreCreate

    Part of CThreadSynchronizationInfo's initialization to be carried out
    before actual thread creation
    --*/
    PAL_ERROR CThreadSynchronizationInfo::InitializePreCreate(void)
    {
        PAL_ERROR palErr = NO_ERROR;
        DWORD * pdwWaitState = NULL;
        int iRet;
        const int MaxUnavailableResourceRetries = 10;
        int iEagains;
        pthread_condattr_t attrs;
        pthread_condattr_t *attrsPtr = nullptr;

        m_shridWaitAwakened = malloc(sizeof(DWORD));
        if (NULL == m_shridWaitAwakened)
        {
            ERROR("Fail allocating thread wait status shared object\n");
            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto IPrC_exit;
        }

        pdwWaitState = SharedIDToTypePointer(DWORD,
            m_shridWaitAwakened);

        _ASSERT_MSG(NULL != pdwWaitState,
            "Unable to map shared wait state: bad shared ID [shrid=%p]\n", (VOID*)m_shridWaitAwakened);

        VolatileStore<DWORD>(pdwWaitState, TWS_ACTIVE);
        m_tsThreadState = TS_STARTING;

#if HAVE_PTHREAD_CONDATTR_SETCLOCK
        attrsPtr = &attrs;
        iRet = pthread_condattr_init(&attrs);
        if (0 != iRet)
        {
            ERROR("Failed to initialize thread synchronization condition attribute "
                  "[error=%d (%s)]\n", iRet, strerror(iRet));
            if (ENOMEM == iRet)
            {
                palErr = ERROR_NOT_ENOUGH_MEMORY;
            }
            else
            {
                palErr = ERROR_INTERNAL_ERROR;
            }
            goto IPrC_exit;
        }

        // Ensure that the pthread_cond_timedwait will use CLOCK_MONOTONIC
        iRet = pthread_condattr_setclock(&attrs, CLOCK_MONOTONIC);
        if (0 != iRet)
        {
            ERROR("Failed set thread synchronization condition timed wait clock "
                  "[error=%d (%s)]\n", iRet, strerror(iRet));
            palErr = ERROR_INTERNAL_ERROR;
            pthread_condattr_destroy(&attrs);
            goto IPrC_exit;
        }
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK

        iEagains = 0;
    Mutex_retry:
        iRet = pthread_mutex_init(&m_tnwdNativeData.mutex, NULL);
        if (0 != iRet)
        {
            ERROR("Failed creating thread synchronization mutex [error=%d (%s)]\n", iRet, strerror(iRet));
            if (EAGAIN == iRet && MaxUnavailableResourceRetries >= ++iEagains)
            {
                poll(NULL, 0, std::min(100,10*iEagains));
                goto Mutex_retry;
            }
            else if (ENOMEM == iRet)
            {
                palErr = ERROR_NOT_ENOUGH_MEMORY;
            }
            else
            {
                palErr = ERROR_INTERNAL_ERROR;
            }

            goto IPrC_exit;
        }

        iEagains = 0;
    Cond_retry:

        iRet = pthread_cond_init(&m_tnwdNativeData.cond, attrsPtr);

        if (0 != iRet)
        {
            ERROR("Failed creating thread synchronization condition "
                  "[error=%d (%s)]\n", iRet, strerror(iRet));
            if (EAGAIN == iRet && MaxUnavailableResourceRetries >= ++iEagains)
            {
                poll(NULL, 0, std::min(100,10*iEagains));
                goto Cond_retry;
            }
            else if (ENOMEM == iRet)
            {
                palErr = ERROR_NOT_ENOUGH_MEMORY;
            }
            else
            {
                palErr = ERROR_INTERNAL_ERROR;
            }
            pthread_mutex_destroy(&m_tnwdNativeData.mutex);
            goto IPrC_exit;
        }

        m_tnwdNativeData.fInitialized = true;

    IPrC_exit:
        if (attrsPtr != nullptr)
        {
            pthread_condattr_destroy(attrsPtr);
        }
        if (NO_ERROR != palErr)
        {
            m_tsThreadState = TS_FAILED;
        }
        return palErr;
    }

    /*++
    Method:
      CThreadSynchronizationInfo::InitializePostCreate

    Part of CThreadSynchronizationInfo's initialization to be carried out
    after actual thread creation
    --*/
    PAL_ERROR CThreadSynchronizationInfo::InitializePostCreate(
        CPalThread *pthrCurrent,
        SIZE_T threadId,
        DWORD dwLwpId)
    {
        PAL_ERROR palErr = NO_ERROR;

        if (TS_FAILED == m_tsThreadState)
        {
            palErr = ERROR_INTERNAL_ERROR;
        }

        m_twiWaitInfo.pthrOwner = pthrCurrent;

        return palErr;
    }

#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING

    /*++
    Method:
      CThreadSynchronizationInfo::RunDeferredThreadConditionSignalings

    Carries out all the pending condition signalings for the current thread.
    --*/
    PAL_ERROR CThreadSynchronizationInfo::RunDeferredThreadConditionSignalings()
    {
        PAL_ERROR palErr = NO_ERROR;

        _ASSERTE(0 <= m_lPendingSignalingCount);

        if (0 < m_lPendingSignalingCount)
        {
            LONG lArrayPendingSignalingCount = std::min(PendingSignalingsArraySize, m_lPendingSignalingCount);
            LONG lIdx = 0;
            PAL_ERROR palTempErr;

            // Signal all the pending signalings from the array
            for (lIdx = 0; lIdx < lArrayPendingSignalingCount; lIdx++)
            {
                // Do the actual signaling
                palTempErr = CPalSynchronizationManager::SignalThreadCondition(
                    m_rgpthrPendingSignalings[lIdx]->synchronizationInfo.GetNativeData());
                if (NO_ERROR != palTempErr)
                {
                    palErr = palTempErr;
                }

                // Release the thread reference
                m_rgpthrPendingSignalings[lIdx]->ReleaseThreadReference();
            }

            // Signal any pending signalings from the array overflow list
            if (m_lPendingSignalingCount > PendingSignalingsArraySize)
            {
                PLIST_ENTRY pLink;
                DeferredSignalingListNode * pdsln;

                while (!IsListEmpty(&m_lePendingSignalingsOverflowList))
                {
                    // Remove a node from the head of the queue
                    // Note: no need to synchronize the access to this list since
                    // it is meant to be accessed only by the owner thread.
                    pLink = RemoveHeadList(&m_lePendingSignalingsOverflowList);
                    pdsln = CONTAINING_RECORD(pLink,
                                              DeferredSignalingListNode,
                                              Link);

                    // Do the actual signaling
                    palTempErr = CPalSynchronizationManager::SignalThreadCondition(
                        pdsln->pthrTarget->synchronizationInfo.GetNativeData());
                    if (NO_ERROR != palTempErr)
                    {
                        palErr = palTempErr;
                    }

                    // Release the thread reference
                    pdsln->pthrTarget->ReleaseThreadReference();

                    // Delete the node
                    delete pdsln;

                    lIdx += 1;
                }

                _ASSERTE(lIdx == m_lPendingSignalingCount);
            }

            // Reset the counter of pending signalings for this thread
            m_lPendingSignalingCount = 0;
        }

        return palErr;
    }

#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING


    /*++
    Method:
      CPalSynchronizationManager::InterlockedAwaken

    Tries to change the target wait status to 'active' in an interlocked fashion
    --*/
    bool CPalSynchronizationManager::InterlockedAwaken(
        DWORD *pWaitState)
    {
        return InterlockedCompareExchange((LONG *)pWaitState, TWS_ACTIVE, TWS_WAITING) == TWS_WAITING;
    }

    /*++
    Method:
      CPalSynchronizationManager::GetAbsoluteTimeout

    Converts a relative timeout to an absolute one.
    --*/
    PAL_ERROR CPalSynchronizationManager::GetAbsoluteTimeout(DWORD dwTimeout, struct timespec * ptsAbsTmo)
    {
        PAL_ERROR palErr = NO_ERROR;
        int iRet;

#if HAVE_PTHREAD_CONDATTR_SETCLOCK
        iRet = clock_gettime(CLOCK_MONOTONIC, ptsAbsTmo);
#elif HAVE_WORKING_CLOCK_GETTIME
        // Not every platform implements a (working) clock_gettime
        iRet = clock_gettime(CLOCK_REALTIME, ptsAbsTmo);
#elif HAVE_WORKING_GETTIMEOFDAY
        // Not every platform implements a (working) gettimeofday
        struct timeval tv;
        iRet = gettimeofday(&tv, NULL);
        if (0 == iRet)
        {
            ptsAbsTmo->tv_sec  = tv.tv_sec;
            ptsAbsTmo->tv_nsec = tv.tv_usec * tccMicroSecondsToNanoSeconds;
        }
#else
#ifdef DBI_COMPONENT_MONO
    return ERROR_INTERNAL_ERROR;
#else
    #error "Don't know how to get hi-res current time on this platform"
#endif
#endif // HAVE_WORKING_CLOCK_GETTIME, HAVE_WORKING_GETTIMEOFDAY
        if (0 == iRet)
        {
            ptsAbsTmo->tv_sec  += dwTimeout / tccSecondsToMilliSeconds;
            ptsAbsTmo->tv_nsec += (dwTimeout % tccSecondsToMilliSeconds) * tccMilliSecondsToNanoSeconds;
            while (ptsAbsTmo->tv_nsec >= tccSecondsToNanoSeconds)
            {
                ptsAbsTmo->tv_sec  += 1;
                ptsAbsTmo->tv_nsec -= tccSecondsToNanoSeconds;
            }
        }
        else
        {
            palErr = ERROR_INTERNAL_ERROR;
        }

        return palErr;
    }
}
