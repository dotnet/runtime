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

const int CorUnix::CThreadSynchronizationInfo::PendingSignalingsArraySize;

// We use the synchronization manager's worker thread to handle
// process termination requests. It does so by calling the
// registered handler function.
PTERMINATION_REQUEST_HANDLER g_terminationRequestHandler = NULL;

// Set the handler for process termination requests.
VOID PALAPI PAL_SetTerminationRequestHandler(
    IN PTERMINATION_REQUEST_HANDLER terminationHandler)
{
    g_terminationRequestHandler = terminationHandler;
}

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
      CPalSynchMgrController::StartWorker

    Starts the Synchronization Manager's Worker Thread
    --*/
    PAL_ERROR CPalSynchMgrController::StartWorker(
        CPalThread * pthrCurrent)
    {
        return CPalSynchronizationManager::StartWorker(pthrCurrent);
    }

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
    CRITICAL_SECTION CPalSynchronizationManager::s_csSynchProcessLock;
    CRITICAL_SECTION CPalSynchronizationManager::s_csMonitoredProcessesLock;

    CPalSynchronizationManager::CPalSynchronizationManager()
        : m_dwWorkerThreadTid(0),
          m_pipoThread(NULL),
          m_pthrWorker(NULL),
          m_iProcessPipeRead(-1),
          m_iProcessPipeWrite(-1),
          m_pmplnMonitoredProcesses(NULL),
          m_lMonitoredProcessesCount(0),
          m_pmplnExitedNodes(NULL),
          m_cacheWaitCtrlrs(CtrlrsCacheMaxSize),
          m_cacheStateCtrlrs(CtrlrsCacheMaxSize),
          m_cacheSynchData(SynchDataCacheMaxSize),
          m_cacheSHRSynchData(SynchDataCacheMaxSize),
          m_cacheWTListNodes(WTListNodeCacheMaxSize),
          m_cacheSHRWTListNodes(WTListNodeCacheMaxSize),
          m_cacheThreadApcInfoNodes(ApcInfoNodeCacheMaxSize),
          m_cacheOwnedObjectsListNodes(OwnedObjectsListCacheMaxSize)
    {
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
        m_iKQueue = -1;
        // Initialize data to 0 and flags to EV_EOF
        EV_SET(&m_keProcessPipeEvent, 0, 0, EV_EOF, 0, 0, 0);
#endif // HAVE_KQUEUE
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
        bool fAlertable,
        bool fIsSleep,
        ThreadWakeupReason *ptwrWakeupReason,
        DWORD * pdwSignaledObject)
    {
        PAL_ERROR palErr = NO_ERROR;
        ThreadWakeupReason twrWakeupReason = WaitFailed;
        DWORD * pdwWaitState;
        DWORD dwWaitState = 0;
        DWORD dwSigObjIdx = 0;
        bool fRaceAlerted = false;
        bool fEarlyDeath = false;

        pdwWaitState = SharedIDToTypePointer(DWORD,
                pthrCurrent->synchronizationInfo.m_shridWaitAwakened);

        _ASSERT_MSG(NULL != pdwWaitState,
                    "Got NULL pdwWaitState from m_shridWaitAwakened=%p\n",
                    (VOID *)pthrCurrent->synchronizationInfo.m_shridWaitAwakened);

        if (fIsSleep)
        {
            // If fIsSleep is true we are being called by Sleep/SleepEx
            // and we need to switch the wait state to TWS_WAITING or
            // TWS_ALERTABLE (according to fAlertable)

            if (fAlertable)
            {
                // If we are in alertable mode we need to grab the lock to
                // make sure that no APC is queued right before the
                // InterlockedCompareExchange.
                // If there are APCs queued at this time, no native wakeup
                // will be posted, so we need to skip the native wait

                // Lock
                AcquireLocalSynchLock(pthrCurrent);
                AcquireSharedSynchLock(pthrCurrent);

                if (AreAPCsPending(pthrCurrent))
                {
                    // APCs have been queued when the thread wait status was
                    // still TWS_ACTIVE, therefore the queueing thread will not
                    // post any native wakeup: we need to skip the actual
                    // native wait
                    fRaceAlerted = true;
                }
            }

            if (!fRaceAlerted)
            {
                // Setting the thread in wait state
                dwWaitState = (DWORD)(fAlertable ? TWS_ALERTABLE : TWS_WAITING);

                TRACE("Switching my wait state [%p] from TWS_ACTIVE to %u [current *pdwWaitState=%u]\n",
                      pdwWaitState, dwWaitState, *pdwWaitState);

                dwWaitState = InterlockedCompareExchange((LONG *)pdwWaitState,
                                                         dwWaitState,
                                                         TWS_ACTIVE);

                if ((DWORD)TWS_ACTIVE != dwWaitState)
                {
                    if (fAlertable)
                    {
                        // Unlock
                        ReleaseSharedSynchLock(pthrCurrent);
                        ReleaseLocalSynchLock(pthrCurrent);
                    }

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

            if (fAlertable)
            {
                // Unlock
                ReleaseSharedSynchLock(pthrCurrent);
                ReleaseLocalSynchLock(pthrCurrent);
            }
        }

        if (fRaceAlerted)
        {
            twrWakeupReason = Alerted;
        }
        else
        {
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
        }

        if (WaitTimeout == twrWakeupReason)
        {
            // timeout reached. set wait state back to 'active'
            dwWaitState = (DWORD)(fAlertable ? TWS_ALERTABLE : TWS_WAITING);

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
                case TWS_ALERTABLE:
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
                // Note: UnRegisterWait will take care of grabbing the shared synch lock, if needed.
                UnRegisterWait(pthrCurrent, ptwiWaitInfo, false);

                // Unlock
                ReleaseLocalSynchLock(pthrCurrent);

                break;
            }
            case WaitSucceeded:
            case MutexAbandoned:
                *pdwSignaledObject = dwSigObjIdx;
                break;
            default:
                // 'Alerted' and 'WaitFailed' go through this case
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
            palErr = GetAbsoluteTimeout(dwTimeout, &tsAbsTmo, /*fPreferMonotonicClock*/ TRUE);
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

    TNW_exit:
        TRACE("ThreadNativeWait: returning %u [WakeupReason=%u]\n", palErr, *ptwrWakeupReason);
        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::AbandonObjectsOwnedByThread

    This method is called by a thread at thread-exit time to abandon
    any currently owned waitable object (mutexes). If pthrTarget is
    different from pthrCurrent, AbandonObjectsOwnedByThread assumes
    to be called whether by TerminateThread or at shutdown time. See
    comments below for more details
    --*/
    PAL_ERROR CPalSynchronizationManager::AbandonObjectsOwnedByThread(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget)
    {
        PAL_ERROR palErr = NO_ERROR;
        OwnedObjectsListNode * poolnItem;
        bool fSharedSynchLock = false;
        CThreadSynchronizationInfo * pSynchInfo = &pthrTarget->synchronizationInfo;
        CPalSynchronizationManager * pSynchManager = GetInstance();

        // The shared memory manager's process lock is acquired before calling into some PAL synchronization primitives that may
        // take the PAL synchronization manager's synch lock (acquired below). For example, when using a file lock
        // implementation for a named mutex (see NamedMutexProcessData::NamedMutexProcessData()), under the shared memory
        // manager's process lock, CreateMutex is called, which acquires the PAL synchronization manager's synch lock. The same
        // lock order needs to be maintained here to avoid a deadlock.
        bool abandonNamedMutexes = pSynchInfo->OwnsAnyNamedMutex();
        if (abandonNamedMutexes)
        {
            SharedMemoryManager::AcquireCreationDeletionProcessLock();
        }

        // Local lock
        AcquireLocalSynchLock(pthrCurrent);

        // Abandon owned objects
        while (NULL != (poolnItem = pSynchInfo->RemoveFirstObjectFromOwnedList()))
        {
            CSynchData * psdSynchData = poolnItem->pPalObjSynchData;

            _ASSERT_MSG(NULL != psdSynchData,
                        "NULL psdSynchData pointer in ownership list node\n");

            VALIDATEOBJECT(psdSynchData);

            TRACE("Abandoning object with SynchData at %p\n", psdSynchData);

            if (!fSharedSynchLock &&
                (SharedObject == psdSynchData->GetObjectDomain()))
            {
                AcquireSharedSynchLock(pthrCurrent);
                fSharedSynchLock = true;
            }

            // Reset ownership data
            psdSynchData->ResetOwnership();

            // Set abandoned status; in case there is a thread to be released:
            //  - if the thread is local, ReleaseFirstWaiter will reset the
            //    abandoned status
            //  - if the thread is remote, the remote worker thread will use
            //    the value and reset it
            psdSynchData->SetAbandoned(true);

            // Signal the object and trigger thread awakening
            psdSynchData->Signal(pthrCurrent, 1, false);

            // Release reference to to SynchData
            psdSynchData->Release(pthrCurrent);

            // Return node to the cache
            pSynchManager->m_cacheOwnedObjectsListNodes.Add(pthrCurrent, poolnItem);
        }

        if (abandonNamedMutexes)
        {
            // Abandon owned named mutexes
            while (true)
            {
                NamedMutexProcessData *processData = pSynchInfo->RemoveFirstOwnedNamedMutex();
                if (processData == nullptr)
                {
                    break;
                }
                processData->Abandon();
            }
        }

        if (pthrTarget != pthrCurrent)
        {
            // If the target thead is not the current one, we are being called
            // at shutdown time, right before the target thread is suspended,
            // or anyway the target thread is being terminated.
            // In this case we switch its wait state to TWS_EARLYDEATH so that,
            // if the thread is currently waiting/sleeping and it wakes up
            // before shutdown code manage to suspend it, it will be rerouted
            // to ThreadPrepareForShutdown (that will be done without holding
            // any internal lock, in a way to accommodate shutdown time thread
            // suspension).
            // At this time we also unregister the wait, so no dummy nodes are
            // left around on waiting objects.
            // The TWS_EARLYDEATH wait-state will also prevent the thread from
            // successfully registering for a possible new wait in the same
            // time window.
            LONG lTWState;
            DWORD * pdwWaitState;

            pdwWaitState = SharedIDToTypePointer(DWORD, pthrTarget->synchronizationInfo.m_shridWaitAwakened);
            lTWState = InterlockedExchange((LONG *)pdwWaitState, TWS_EARLYDEATH);

            if (( ((LONG)TWS_WAITING == lTWState) || ((LONG)TWS_ALERTABLE == lTWState) ) &&
                (0 < pSynchInfo->m_twiWaitInfo.lObjCount))
            {
                // Unregister the wait
                // Note: UnRegisterWait will take care of grabbing the shared synch lock, if needed.
                UnRegisterWait(pthrCurrent, &pSynchInfo->m_twiWaitInfo, fSharedSynchLock);
            }
        }

        // Unlock
        if (fSharedSynchLock)
        {
            ReleaseSharedSynchLock(pthrCurrent);
            fSharedSynchLock = false;
        }

        ReleaseLocalSynchLock(pthrCurrent);

        if (abandonNamedMutexes)
        {
            SharedMemoryManager::ReleaseCreationDeletionProcessLock();
        }

        DiscardAllPendingAPCs(pthrCurrent, pthrTarget);

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
        WaitDomain wdWaitDomain = LocalWait;
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

        for (uIdx=0; uIdx<dwObjectCount; uIdx++)
        {
            if (SharedObject == rgObjects[uIdx]->GetObjectDomain())
            {
                ++uSharedObjectCount;
            }

            if (uSharedObjectCount > 0 && uSharedObjectCount <= uIdx)
            {
                wdWaitDomain = MixedWait;
                break;
            }
        }

        if (dwObjectCount == uSharedObjectCount)
        {
            wdWaitDomain = SharedWait;
        }

        for (uIdx=0;uIdx<dwObjectCount;uIdx++)
        {
            void * pvSData;
            CSynchData * psdSynchData;
            ObjectDomain odObjectDomain = rgObjects[uIdx]->GetObjectDomain();

            palErr = rgObjects[uIdx]->GetObjectSynchData((void **)&pvSData);
            if (NO_ERROR != palErr)
            {
                break;
            }

            psdSynchData = (SharedObject == odObjectDomain) ? SharedIDToTypePointer(
                CSynchData, reinterpret_cast<SharedID>(pvSData)) :
                static_cast<CSynchData *>(pvSData);

            VALIDATEOBJECT(psdSynchData);

            potObjectType = rgObjects[uIdx]->GetObjectType();

            if (CSynchControllerBase::WaitController == ctCtrlrType)
            {
                Ctrlrs.pWaitCtrlrs[uIdx]->Init(pthrCurrent,
                                            ctCtrlrType,
                                            odObjectDomain,
                                            potObjectType,
                                            psdSynchData,
                                            wdWaitDomain);
            }
            else
            {
                Ctrlrs.pStateCtrlrs[uIdx]->Init(pthrCurrent,
                                             ctCtrlrType,
                                             odObjectDomain,
                                             potObjectType,
                                             psdSynchData,
                                             wdWaitDomain);
            }

            if (CSynchControllerBase::WaitController == ctCtrlrType &&
                otiProcess == potObjectType->GetId())
            {
                CProcProcessLocalData * pProcLocData;
                IDataLock * pDataLock;

                palErr = rgObjects[uIdx]->GetProcessLocalData(
                    pthrCurrent,
                    ReadLock,
                    &pDataLock,
                    (void **)&pProcLocData);

                if (NO_ERROR != palErr)
                {
                    // In case of failure here, bail out of the loop, but
                    // keep track (by incrementing the counter 'uIdx') of the
                    // fact that this controller has already being initialized
                    // and therefore need to be Release'd rather than just
                    // returned to the cache
                    uIdx++;
                    break;
                }

                Ctrlrs.pWaitCtrlrs[uIdx]->SetProcessData(rgObjects[uIdx], pProcLocData);
                pDataLock->ReleaseLock(pthrCurrent, false);
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
        ObjectDomain odObjectDomain,
        VOID **ppvSynchData)
    {
        PAL_ERROR palErr = NO_ERROR;
        CSynchData * psdSynchData = NULL;
        CPalThread * pthrCurrent = InternalGetCurrentThread();

        if (SharedObject == odObjectDomain)
        {
            SharedID shridSynchData = m_cacheSHRSynchData.Get(pthrCurrent);
            if (NULL == shridSynchData)
            {
                ERROR("Unable to allocate shared memory\n");
                return ERROR_NOT_ENOUGH_MEMORY;
            }

            psdSynchData = SharedIDToTypePointer(CSynchData, shridSynchData);

            VALIDATEOBJECT(psdSynchData);

            _ASSERT_MSG(NULL != psdSynchData, "Bad shared memory pointer\n");

            // Initialize waiting list pointers
            psdSynchData->SetWTLHeadShrPtr(NULL);
            psdSynchData->SetWTLTailShrPtr(NULL);

            // Store shared pointer to this object
            psdSynchData->SetSharedThis(shridSynchData);

            *ppvSynchData = reinterpret_cast<void *>(shridSynchData);
        }
        else
        {
            psdSynchData = m_cacheSynchData.Get(pthrCurrent);
            if (NULL == psdSynchData)
            {
                ERROR("Unable to allocate memory\n");
                return ERROR_NOT_ENOUGH_MEMORY;
            }

            // Initialize waiting list pointers
            psdSynchData->SetWTLHeadPtr(NULL);
            psdSynchData->SetWTLTailPtr(NULL);

            // Set shared this pointer to NULL
            psdSynchData->SetSharedThis(NULL);

            *ppvSynchData = static_cast<void *>(psdSynchData);
        }

        // Initialize object domain and object type;
        psdSynchData->SetObjectDomain(odObjectDomain);
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
        ObjectDomain odObjectDomain,
        VOID *pvSynchData)
    {
        CSynchData * psdSynchData;
        CPalThread * pthrCurrent = InternalGetCurrentThread();

        if (odObjectDomain == SharedObject)
        {
            psdSynchData = SharedIDToTypePointer(CSynchData,
                reinterpret_cast<SharedID>(pvSynchData));

            if (NULL == psdSynchData)
            {
                ASSERT("Bad shared memory pointer\n");
                return;
            }
        }
        else
        {
            psdSynchData = static_cast<CSynchData *>(pvSynchData);
        }

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
        ObjectDomain odObjectDomain,
        ISynchStateController **ppStateController)
    {
        PAL_ERROR palErr = NO_ERROR;
        CSynchStateController * pCtrlr =  NULL;
        WaitDomain wdWaitDomain = (SharedObject == odObjectDomain) ? SharedWait : LocalWait;
        CSynchData * psdSynchData;

        psdSynchData = (SharedObject == odObjectDomain) ? SharedIDToTypePointer(CSynchData, reinterpret_cast<SharedID>(pvSynchData))
                                                        : static_cast<CSynchData *>(pvSynchData);

        VALIDATEOBJECT(psdSynchData);

        pCtrlr = m_cacheStateCtrlrs.Get(pthrCurrent);
        if (NULL == pCtrlr)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        pCtrlr->Init(pthrCurrent,
                     CSynchControllerBase::StateController,
                     odObjectDomain,
                     potObjectType,
                     psdSynchData,
                     wdWaitDomain);

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
        ObjectDomain odObjectDomain,
        ISynchWaitController **ppWaitController)
    {
        CSynchWaitController * pCtrlr =  NULL;
        WaitDomain wdWaitDomain = (SharedObject == odObjectDomain) ? SharedWait : LocalWait;
        CSynchData * psdSynchData;

        psdSynchData = (SharedObject == odObjectDomain) ? SharedIDToTypePointer(
            CSynchData, reinterpret_cast<SharedID>(pvSynchData)) :
            static_cast<CSynchData *>(pvSynchData);

        VALIDATEOBJECT(psdSynchData);

        pCtrlr = m_cacheWaitCtrlrs.Get(pthrCurrent);
        if (NULL == pCtrlr)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        pCtrlr->Init(pthrCurrent,
                     CSynchControllerBase::WaitController,
                     odObjectDomain,
                     potObjectType,
                     psdSynchData,
                     wdWaitDomain);

        // Succeeded
        *ppWaitController = (ISynchWaitController *)pCtrlr;

        return NO_ERROR;
    }

    /*++
    Method:
      CPalSynchronizationManager::QueueUserAPC

    Internal implementation of QueueUserAPC
    --*/
    PAL_ERROR CPalSynchronizationManager::QueueUserAPC(CPalThread * pthrCurrent,
        CPalThread * pthrTarget,
        PAPCFUNC pfnAPC,
        ULONG_PTR uptrData)
    {
        PAL_ERROR palErr = NO_ERROR;
        ThreadApcInfoNode * ptainNode = NULL;
        DWORD dwWaitState;
        DWORD * pdwWaitState;
        ThreadWaitInfo * pTargetTWInfo = GetThreadWaitInfo(pthrTarget);
        bool fLocalSynchLock = false;
        bool fSharedSynchLock = false;
        bool fThreadLock = false;

        ptainNode = m_cacheThreadApcInfoNodes.Get(pthrCurrent);
        if (NULL == ptainNode)
        {
            ERROR("No memory for new APCs linked list entry\n");
            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto QUAPC_exit;
        }

        ptainNode->pfnAPC = pfnAPC;
        ptainNode->pAPCData = uptrData;
        ptainNode->pNext = NULL;

        AcquireLocalSynchLock(pthrCurrent);
        fLocalSynchLock = true;

        if (LocalWait != pTargetTWInfo->wdWaitDomain)
        {
            AcquireSharedSynchLock(pthrCurrent);
            fSharedSynchLock = true;
        }

        pthrTarget->Lock(pthrCurrent);
        fThreadLock = true;

        if (TS_DONE == pthrTarget->synchronizationInfo.GetThreadState())
        {
            ERROR("Thread %#x has terminated; can't queue an APC on it\n",
                  pthrTarget->GetThreadId());
            palErr = ERROR_INVALID_PARAMETER;
            goto QUAPC_exit;
        }
        pdwWaitState = SharedIDToTypePointer(DWORD,
            pthrTarget->synchronizationInfo.m_shridWaitAwakened);
        if (TWS_EARLYDEATH == VolatileLoad(pdwWaitState))
        {
            ERROR("Thread %#x is about to be suspended for process shutdwon, "
                  "can't queue an APC on it\n", pthrTarget->GetThreadId());
            palErr = ERROR_INVALID_PARAMETER;
            goto QUAPC_exit;
        }

        if (NULL == pthrTarget->apcInfo.m_ptainTail)
        {
            _ASSERT_MSG(NULL == pthrTarget->apcInfo.m_ptainHead, "Corrupted APC list\n");

            pthrTarget->apcInfo.m_ptainHead = ptainNode;
            pthrTarget->apcInfo.m_ptainTail = ptainNode;
        }
        else
        {
            pthrTarget->apcInfo.m_ptainTail->pNext = ptainNode;
            pthrTarget->apcInfo.m_ptainTail = ptainNode;
        }

        // Set ptainNode to NULL so it won't be readded to the cache
        ptainNode = NULL;

        TRACE("APC %p with parameter %p added to APC queue\n", pfnAPC, uptrData);

        dwWaitState = InterlockedCompareExchange((LONG *)pdwWaitState,
                                                 (LONG)TWS_ACTIVE,
                                                 (LONG)TWS_ALERTABLE);

        // Release thread lock
        pthrTarget->Unlock(pthrCurrent);
        fThreadLock = false;

        if (TWS_ALERTABLE == dwWaitState)
        {
            // Unregister the wait
            UnRegisterWait(pthrCurrent, pTargetTWInfo, fSharedSynchLock);

            // Wake up target thread
            palErr = WakeUpLocalThread(
                pthrCurrent,
                pthrTarget,
                Alerted,
                0);

            if (NO_ERROR != palErr)
            {
                ERROR("Failed to wakeup local thread %#x for dispatching APCs [err=%u]\n",
                    pthrTarget->GetThreadId(), palErr);
            }
        }

    QUAPC_exit:
        if (fThreadLock)
        {
            pthrTarget->Unlock(pthrCurrent);
        }

        if (fSharedSynchLock)
        {
            ReleaseSharedSynchLock(pthrCurrent);
        }

        if (fLocalSynchLock)
        {
            ReleaseLocalSynchLock(pthrCurrent);
        }

        if (ptainNode)
        {
            m_cacheThreadApcInfoNodes.Add(pthrCurrent, ptainNode);
        }

        return palErr;
    }

    /*++
    Method:
        CPalSynchronizationManager::SendTerminationRequestToWorkerThread

    Send a request to the worker thread to initiate process termination.
    --*/
    PAL_ERROR CPalSynchronizationManager::SendTerminationRequestToWorkerThread()
    {
        PAL_ERROR palErr = GetInstance()->WakeUpLocalWorkerThread(SynchWorkerCmdTerminationRequest);
        if (palErr != NO_ERROR)
        {
            ERROR("Failed to wake up worker thread [errno=%d {%s%}]\n",
                  errno, strerror(errno));
            palErr = ERROR_INTERNAL_ERROR;
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::AreAPCsPending

    Returns 'true' if there are APCs currently pending for the target
    thread (normally the current one)
    --*/
    bool CPalSynchronizationManager::AreAPCsPending(
        CPalThread * pthrTarget)
    {
        // No need to lock here
        return (NULL != pthrTarget->apcInfo.m_ptainHead);
    }

    /*++
    Method:
      CPalSynchronizationManager::DispatchPendingAPCs

    Executes any pending APC for the current thread
    --*/
    PAL_ERROR CPalSynchronizationManager::DispatchPendingAPCs(
        CPalThread * pthrCurrent)
    {
        ThreadApcInfoNode * ptainNode, * ptainLocalHead;
        int iAPCsCalled = 0;

        while (TRUE)
        {
            // Lock
            pthrCurrent->Lock(pthrCurrent);
            ptainLocalHead = pthrCurrent->apcInfo.m_ptainHead;
            if (ptainLocalHead)
            {
                pthrCurrent->apcInfo.m_ptainHead = NULL;
                pthrCurrent->apcInfo.m_ptainTail = NULL;
            }

            // Unlock
            pthrCurrent->Unlock(pthrCurrent);

            if (NULL == ptainLocalHead)
            {
                break;
            }

            while (ptainLocalHead)
            {
                ptainNode = ptainLocalHead;
                ptainLocalHead = ptainNode->pNext;

#if _ENABLE_DEBUG_MESSAGES_
                // reset ENTRY nesting level back to zero while
                // inside the callback ...
                int iOldLevel = DBG_change_entrylevel(0);
#endif /* _ENABLE_DEBUG_MESSAGES_ */

                TRACE("Calling APC %p with parameter %#x\n",
                      ptainNode->pfnAPC, ptainNode->pfnAPC);

                // Actual APC call
                ptainNode->pfnAPC(ptainNode->pAPCData);

#if _ENABLE_DEBUG_MESSAGES_
                // ... and set nesting level back to what it was
                DBG_change_entrylevel(iOldLevel);
#endif /* _ENABLE_DEBUG_MESSAGES_ */

                iAPCsCalled++;
                m_cacheThreadApcInfoNodes.Add(pthrCurrent, ptainNode);
            }
        }

        return (iAPCsCalled > 0) ? NO_ERROR : ERROR_NOT_FOUND;
    }

    /*++
    Method:
      CPalSynchronizationManager::DiscardAllPendingAPCs

    Discards any pending APC for the target pthrTarget thread
    --*/
    void CPalSynchronizationManager::DiscardAllPendingAPCs(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget)
    {
        ThreadApcInfoNode * ptainNode, * ptainLocalHead;

        // Lock
        pthrTarget->Lock(pthrCurrent);
        ptainLocalHead = pthrTarget->apcInfo.m_ptainHead;
        if (ptainLocalHead)
        {
            pthrTarget->apcInfo.m_ptainHead = NULL;
            pthrTarget->apcInfo.m_ptainTail = NULL;
        }

        // Unlock
        pthrTarget->Unlock(pthrCurrent);

        while (ptainLocalHead)
        {
            ptainNode = ptainLocalHead;
            ptainLocalHead = ptainNode->pNext;

            m_cacheThreadApcInfoNodes.Add(pthrCurrent, ptainNode);
        }
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

        InternalInitializeCriticalSection(&s_csSynchProcessLock);
        InternalInitializeCriticalSection(&s_csMonitoredProcessesLock);

        pSynchManager = InternalNew<CPalSynchronizationManager>();
        if (NULL == pSynchManager)
        {
            ERROR("Failed to allocate memory for Synchronization Manager");
            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto I_exit;
        }

        if (!pSynchManager->CreateProcessPipe())
        {
            ERROR("Unable to create process pipe \n");
            palErr = ERROR_OPEN_FAILED;
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
            if (NULL != pSynchManager)
            {
                pSynchManager->ShutdownProcessPipe();
            }

            s_pObjSynchMgr = NULL;
            g_pSynchronizationManager = NULL;
            InternalDelete(pSynchManager);
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::StartWorker

    Starts the Synchronization Manager's Worker Thread.
    Private method, it is called only by CPalSynchMgrController.
    --*/
    PAL_ERROR CPalSynchronizationManager::StartWorker(
        CPalThread * pthrCurrent)
    {
        PAL_ERROR palErr = NO_ERROR;
        CPalSynchronizationManager * pSynchManager = GetInstance();

        if ((NULL == pSynchManager) || ((LONG)SynchMgrStatusRunning != s_lInitStatus))
        {
            ERROR("Trying to to create worker thread in invalid state\n");
            return ERROR_INTERNAL_ERROR;
        }

        HANDLE hWorkerThread = NULL;
        SIZE_T osThreadId = 0;
        palErr = InternalCreateThread(pthrCurrent,
                                      NULL,
                                      0,
                                      &WorkerThread,
                                      (PVOID)pSynchManager,
                                      0,
                                      PalWorkerThread,
                                      &osThreadId,
                                      &hWorkerThread);

        if (NO_ERROR == palErr)
        {
            pSynchManager->m_dwWorkerThreadTid = (DWORD)osThreadId;
            palErr = InternalGetThreadDataFromHandle(pthrCurrent,
                                                     hWorkerThread,
                                                     &pSynchManager->m_pthrWorker,
                                                     &pSynchManager->m_pipoThread);
            if (NO_ERROR != palErr)
            {
                ERROR("Unable to get worker thread data\n");
            }
        }
        else
        {
            ERROR("Unable to create worker thread\n");
        }

        if (NULL != hWorkerThread)
        {
            CloseHandle(hWorkerThread);
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
        CPalSynchronizationManager * pSynchManager = GetInstance();
        CPalThread * pthrCurrent = InternalGetCurrentThread();
        int iRet;
        ThreadNativeWaitData * ptnwdWorkerThreadNativeData;
        struct timespec tsAbsTmo = { 0, 0 };

        LONG lInit = InterlockedCompareExchange(&s_lInitStatus,
            (LONG)SynchMgrStatusShuttingDown, (LONG)SynchMgrStatusRunning);

        if ((LONG)SynchMgrStatusRunning != lInit)
        {
            ASSERT("Unexpected initialization status found "
                   "in PrepareForShutdown [expected=%d current=%d]\n",
                   SynchMgrStatusRunning, lInit);
            // We intentionally not set s_lInitStatus to SynchMgrStatusError
            // cause this could interfere with a previous thread already
            // executing shutdown
            palErr = ERROR_INTERNAL_ERROR;
            goto PFS_exit;
        }

        // Discard process monitoring for process waits
        pSynchManager->DiscardMonitoredProcesses(pthrCurrent);

        if (NULL == pSynchManager->m_pipoThread)
        {
            // If m_pipoThread is NULL here, that means that StartWorker has
            // never been called. That may happen if PAL_Initialize fails
            // sometime after having called CreatePalSynchronizationManager,
            // but before calling StartWorker. Nothing else to do here.
            goto PFS_exit;
        }

        palErr = pSynchManager->WakeUpLocalWorkerThread(SynchWorkerCmdShutdown);
        if (NO_ERROR != palErr)
        {
            ERROR("Failed stopping worker thread [palErr=%u]\n", palErr);
            s_lInitStatus = SynchMgrStatusError;
            goto PFS_exit;
        }

        ptnwdWorkerThreadNativeData =
            &pSynchManager->m_pthrWorker->synchronizationInfo.m_tnwdNativeData;

        palErr = GetAbsoluteTimeout(WorkerThreadTerminationTimeout, &tsAbsTmo, /*fPreferMonotonicClock*/ TRUE);
        if (NO_ERROR != palErr)
        {
            ERROR("Failed to convert timeout to absolute timeout\n");
            s_lInitStatus = SynchMgrStatusError;
            goto PFS_exit;
        }

        // Using the worker thread's predicate/condition/mutex
        // to wait for worker thread to be done
        iRet = pthread_mutex_lock(&ptnwdWorkerThreadNativeData->mutex);
        if (0 != iRet)
        {
            // pthread calls might fail if the shutdown is called
            // from a signal handler. In this case just don't wait
            // for the worker thread
            ERROR("Cannot lock mutex [err=%d]\n", iRet);
            palErr = ERROR_INTERNAL_ERROR;
            s_lInitStatus = SynchMgrStatusError;
            goto PFS_exit;
        }

        while (FALSE == ptnwdWorkerThreadNativeData->iPred)
        {
            iRet = pthread_cond_timedwait(&ptnwdWorkerThreadNativeData->cond,
                                          &ptnwdWorkerThreadNativeData->mutex,
                                          &tsAbsTmo);
            if (0 != iRet)
            {
                if (ETIMEDOUT == iRet)
                {
                    WARN("Timed out waiting for worker thread to exit "
                         "(tmo=%u ms)\n", WorkerThreadTerminationTimeout);
                }
                else
                {
                    ERROR("pthread_cond_timedwait returned %d [errno=%d (%s)]\n",
                          iRet, errno, strerror(errno));
                }
                break;
            }
        }
        if (0 == iRet)
        {
            ptnwdWorkerThreadNativeData->iPred = FALSE;
        }
        iRet = pthread_mutex_unlock(&ptnwdWorkerThreadNativeData->mutex);
        if (0 != iRet)
        {
            ERROR("Cannot unlock mutex [err=%d]\n", iRet);
            palErr = ERROR_INTERNAL_ERROR;
            s_lInitStatus = SynchMgrStatusError;
            goto PFS_exit;
        }

    PFS_exit:
        if (NO_ERROR == palErr)
        {
            if (NULL != pSynchManager->m_pipoThread)
            {
                pSynchManager->m_pipoThread->ReleaseReference(pthrCurrent);

                // After this release both m_pipoThread and m_pthrWorker
                // are no longer valid
                pSynchManager->m_pipoThread = NULL;
                pSynchManager->m_pthrWorker = NULL;
            }

            // Ready for process shutdown
            s_lInitStatus = SynchMgrStatusReadyForProcessShutDown;
        }

        return palErr;
    }

    // Entry point routine for the thread that initiates process termination.
    DWORD PALAPI TerminationRequestHandlingRoutine(LPVOID pArg)
    {
        // Call the termination request handler if one is registered.
        if (g_terminationRequestHandler != NULL)
        {
            // The process will terminate normally by calling exit.
            // We use an exit code of '128 + signo'. This is a convention used in popular
            // shells to calculate an exit code when the process was terminated by a signal.
            // This is also used by the Process.ExitCode implementation.
            g_terminationRequestHandler(128 + SIGTERM);
        }

        return 0;
    }

    /*++
    Method:
      CPalSynchronizationManager::WorkerThread

    Synchronization Manager's Worker Thread
    --*/
    DWORD PALAPI CPalSynchronizationManager::WorkerThread(LPVOID pArg)
    {
        PAL_ERROR palErr;
        bool fShuttingDown = false;
        bool fWorkerIsDone = false;
        int iPollTimeout = INFTIM;
        SynchWorkerCmd swcCmd;
        ThreadWakeupReason twrWakeUpReason;
        SharedID shridMarshaledData;
        DWORD dwData;
        CPalSynchronizationManager * pSynchManager =
            reinterpret_cast<CPalSynchronizationManager*>(pArg);
        CPalThread * pthrWorker = InternalGetCurrentThread();

        while (!fWorkerIsDone)
        {
            LONG lProcessCount;

            palErr = pSynchManager->ReadCmdFromProcessPipe(iPollTimeout,
                                                           &swcCmd,
                                                           &shridMarshaledData,
                                                           &dwData);
            if (NO_ERROR != palErr)
            {
                ERROR("Received error %x from ReadCmdFromProcessPipe()\n",
                      palErr);
                continue;
            }
            switch (swcCmd)
            {
                case SynchWorkerCmdTerminationRequest:
                    // This worker thread is being asked to initiate process termination

                    HANDLE hTerminationRequestHandlingThread;
                    palErr = InternalCreateThread(pthrWorker,
                                      NULL,
                                      0,
                                      &TerminationRequestHandlingRoutine,
                                      NULL,
                                      0,
                                      PalWorkerThread,
                                      NULL,
                                      &hTerminationRequestHandlingThread);

                    if (NO_ERROR != palErr)
                    {
                        ERROR("Unable to create worker thread\n");
                    }

                    if (hTerminationRequestHandlingThread != NULL)
                    {
                        CloseHandle(hTerminationRequestHandlingThread);
                    }

                    break;
                case SynchWorkerCmdNop:
                    TRACE("Synch Worker: received SynchWorkerCmdNop\n");
                    if (fShuttingDown)
                    {
                        TRACE("Synch Worker: received a timeout when "
                              "fShuttingDown==true: worker is done, bailing "
                              "out from the loop\n");

                        // Whether WorkerThreadShuttingDownTimeout has elapsed
                        // or the last process with a descriptor opened for
                        // write on our process pipe, has just closed it,
                        // causing an EOF on the read fd (that can happen only
                        // at shutdown time since during normal run time we
                        // hold a fd opened for write within this process).
                        // In both the case it is time to go for the worker
                        // thread.
                        fWorkerIsDone = true;
                    }
                    else
                    {
                        lProcessCount = pSynchManager->DoMonitorProcesses(pthrWorker);
                        if (lProcessCount > 0)
                        {
                            iPollTimeout = WorkerThreadProcMonitoringTimeout;
                        }
                        else
                        {
                            iPollTimeout = INFTIM;
                        }
                    }
                    break;
                case SynchWorkerCmdRemoteSignal:
                {
                    // Note: this cannot be a wait all
                    WaitingThreadsListNode * pWLNode;
                    ThreadWaitInfo * ptwiWaitInfo;
                    DWORD dwObjIndex;
                    bool fSharedSynchLock = false;

                    // Lock
                    AcquireLocalSynchLock(pthrWorker);
                    AcquireSharedSynchLock(pthrWorker);
                    fSharedSynchLock = true;

                    pWLNode = SharedIDToTypePointer(WaitingThreadsListNode,
                                                    shridMarshaledData);

                    _ASSERT_MSG(NULL != pWLNode, "Received bad Shared ID %p\n",
                                shridMarshaledData);
                    _ASSERT_MSG(gPID == pWLNode->dwProcessId,
                                "Remote signal apparently sent to the wrong "
                                "process [target pid=%u current pid=%u]\n",
                                pWLNode->dwProcessId, gPID);
                    _ASSERT_MSG(0 == (WTLN_FLAG_WAIT_ALL & pWLNode->dwFlags),
                                "Wait all with remote awakening delegated "
                                "through SynchWorkerCmdRemoteSignal rather than "
                                "SynchWorkerCmdDelegatedObjectSignaling\n");


                    // Get the object index
                    dwObjIndex = pWLNode->dwObjIndex;

                    // Get the WaitInfo
                    ptwiWaitInfo = pWLNode->ptwiWaitInfo;

                    // Initialize the WakeUpReason to WaitSucceeded
                    twrWakeUpReason = WaitSucceeded;

                    CSynchData * psdSynchData =
                        SharedIDToTypePointer(CSynchData,
                                              pWLNode->ptrOwnerObjSynchData.shrid);

                    TRACE("Synch Worker: received REMOTE SIGNAL cmd "
                        "[WInfo=%p {Type=%u Domain=%u ObjCount=%d TgtThread=%x} "
                        "SynchData={shriId=%p p=%p} {SigCount=%d IsAbandoned=%d}\n",
                        ptwiWaitInfo, ptwiWaitInfo->wtWaitType, ptwiWaitInfo->wdWaitDomain,
                        ptwiWaitInfo->lObjCount, ptwiWaitInfo->pthrOwner->GetThreadId(),
                        (VOID *)pWLNode->ptrOwnerObjSynchData.shrid, psdSynchData,
                        psdSynchData->GetSignalCount(), psdSynchData->IsAbandoned());

                    if (CObjectType::OwnershipTracked ==
                        psdSynchData->GetObjectType()->GetOwnershipSemantics())
                    {
                        // Abandoned status is not propagated through process
                        // pipe: need to get it from the object itself before
                        // resetting the data by acquiring the object ownership
                        if (psdSynchData->IsAbandoned())
                        {
                            twrWakeUpReason = MutexAbandoned;
                        }

                        // Acquire ownership
                        palErr = psdSynchData->AssignOwnershipToThread(
                                    pthrWorker,
                                    ptwiWaitInfo->pthrOwner);
                        if (NO_ERROR != palErr)
                        {
                            ERROR("Synch Worker: AssignOwnershipToThread "
                                  "failed with error %u; ownership data on "
                                  "object with SynchData %p may be "
                                  "corrupted\n", palErr, psdSynchData);
                        }
                    }

                    // Unregister the wait
                    pSynchManager->UnRegisterWait(pthrWorker,
                                                  ptwiWaitInfo,
                                                  fSharedSynchLock);

                    // pWLNode is no longer valid after UnRegisterWait
                    pWLNode = NULL;

                    TRACE("Synch Worker: Waking up local thread %x "
                          "{WakeUpReason=%u ObjIndex=%u}\n",
                          ptwiWaitInfo->pthrOwner->GetThreadId(),
                          twrWakeUpReason, dwObjIndex);

                    // Wake up the target thread
                    palErr = WakeUpLocalThread(
                        pthrWorker,
                        ptwiWaitInfo->pthrOwner,
                        twrWakeUpReason,
                        dwObjIndex);
                    if (NO_ERROR != palErr)
                    {
                        ERROR("Synch Worker: Failed to wake up local thread "
                              "%#x while propagating remote signaling: "
                              "object signaling may be lost\n",
                              ptwiWaitInfo->pthrOwner->GetThreadId());
                    }

                    // Unlock
                    ReleaseSharedSynchLock(pthrWorker);
                    fSharedSynchLock = false;
                    ReleaseLocalSynchLock(pthrWorker);

                    break;
                }
                case SynchWorkerCmdDelegatedObjectSignaling:
                {
                    CSynchData * psdSynchData;

                    TRACE("Synch Worker: received "
                          "SynchWorkerCmdDelegatedObjectSignaling\n");

                    psdSynchData = SharedIDToTypePointer(CSynchData,
                                                       shridMarshaledData);

                    _ASSERT_MSG(NULL != psdSynchData, "Received bad Shared ID %p\n",
                                shridMarshaledData);
                    _ASSERT_MSG(0 < dwData && (DWORD)INT_MAX > dwData,
                                "Received remote signaling with invalid signal "
                                "count\n");

                    // Lock
                    AcquireLocalSynchLock(pthrWorker);
                    AcquireSharedSynchLock(pthrWorker);

                    TRACE("Synch Worker: received DELEGATED OBJECT SIGNALING "
                        "cmd [SynchData={shriId=%p p=%p} SigCount=%u] [Current obj SigCount=%d "
                        "IsAbandoned=%d]\n", (VOID *)shridMarshaledData,
                        psdSynchData, dwData, psdSynchData->GetSignalCount(),
                        psdSynchData->IsAbandoned());

                    psdSynchData->Signal(pthrWorker,
                                       psdSynchData->GetSignalCount() + dwData,
                                       true);

                    // Current SynchData has been AddRef'd by remote process in
                    // order to be marshaled to the current one, therefore at
                    // this point we need to release it
                    psdSynchData->Release(pthrWorker);

                    // Unlock
                    ReleaseSharedSynchLock(pthrWorker);
                    ReleaseLocalSynchLock(pthrWorker);

                    break;
                }
                case SynchWorkerCmdShutdown:
                    TRACE("Synch Worker: received SynchWorkerCmdShutdown\n");

                    // Shutdown the process pipe: this will cause the process
                    // pipe to be unlinked and its write-only file descriptor
                    // to be closed, so that when the last fd opened for write
                    // on the fifo (from another process) will be closed, we
                    // will receive an EOF on the read end (i.e. poll in
                    // ReadBytesFromProcessPipe will return 1 with no data to
                    // be read). That will allow the worker thread to process
                    // possible commands already successfully written to the
                    // pipe by some other process, before shutting down.
                    pSynchManager->ShutdownProcessPipe();

                    // Shutting down: this will cause the worker thread to
                    // fetch residual cmds from the process pipe until an
                    // EOF is converted to a SynchWorkerCmdNop or the
                    // WorkerThreadShuttingDownTimeout has elapsed without
                    // receiving any cmd.
                    fShuttingDown = true;

                    // Set the timeout to WorkerThreadShuttingDownTimeout
                    iPollTimeout = WorkerThreadShuttingDownTimeout;
                    break;
                default:
                    ASSERT("Synch Worker: Unknown worker cmd [swcWorkerCmd=%d]\n",
                           swcCmd);
                    break;
            }
        }

        int iRet;
        ThreadNativeWaitData * ptnwdWorkerThreadNativeData =
            &pthrWorker->synchronizationInfo.m_tnwdNativeData;

        // Using the worker thread's predicate/condition/mutex
        // (that normally are never used) to signal the shutting
        // down thread that the worker thread is done
        iRet = pthread_mutex_lock(&ptnwdWorkerThreadNativeData->mutex);
        _ASSERT_MSG(0 == iRet, "Cannot lock mutex [err=%d]\n", iRet);

        ptnwdWorkerThreadNativeData->iPred = TRUE;

        iRet = pthread_cond_signal(&ptnwdWorkerThreadNativeData->cond);
        if (0 != iRet)
        {
            ERROR ("pthread_cond_signal returned %d [errno=%d (%s)]\n",
                   iRet, errno, strerror(errno));
        }

        iRet = pthread_mutex_unlock(&ptnwdWorkerThreadNativeData->mutex);
        _ASSERT_MSG(0 == iRet, "Cannot lock mutex [err=%d]\n", iRet);

        // Sleep forever
        ThreadPrepareForShutdown();

        return 0;
    }

    /*++
    Method:
      CPalSynchronizationManager::ReadCmdFromProcessPipe

    Reads a worker thread cmd from the process pipe. If there is no data
    to be read on the pipe, it blocks until there is data available or the
    timeout expires.
    --*/
    PAL_ERROR CPalSynchronizationManager::ReadCmdFromProcessPipe(
        int iPollTimeout,
        SynchWorkerCmd * pswcWorkerCmd,
        SharedID * pshridMarshaledData,
        DWORD * pdwData)
    {
        int iRet;
        BYTE byVal;
        SynchWorkerCmd swcWorkerCmd = SynchWorkerCmdNop;

        _ASSERTE(NULL != pswcWorkerCmd);
        _ASSERTE(NULL != pshridMarshaledData);
        _ASSERTE(NULL != pdwData);

        iRet = ReadBytesFromProcessPipe(iPollTimeout, &byVal, sizeof(BYTE));

        if (0 > iRet)
        {
            ERROR("Failed polling the process pipe [ret=%d errno=%d (%s)]\n",
                  iRet, errno, strerror(errno));

            return ERROR_INTERNAL_ERROR;
        }

        if (iRet != 0)
        {
            _ASSERT_MSG(sizeof(BYTE) == iRet,
                        "Got %d bytes from process pipe while expecting for %d\n",
                        iRet, sizeof(BYTE));

            swcWorkerCmd = (SynchWorkerCmd)byVal;

            if (SynchWorkerCmdLast <= swcWorkerCmd)
            {
                ERROR("Got unknown worker command code %d from the process "
                       "pipe!\n", swcWorkerCmd);

                return ERROR_INTERNAL_ERROR;
            }

            _ASSERT_MSG(SynchWorkerCmdNop == swcWorkerCmd ||
                        SynchWorkerCmdRemoteSignal == swcWorkerCmd ||
                        SynchWorkerCmdDelegatedObjectSignaling == swcWorkerCmd ||
                        SynchWorkerCmdShutdown == swcWorkerCmd ||
                        SynchWorkerCmdTerminationRequest == swcWorkerCmd,
                        "Unknown worker command code %u\n", swcWorkerCmd);

            TRACE("Got cmd %u from process pipe\n", swcWorkerCmd);
        }

        if (SynchWorkerCmdRemoteSignal == swcWorkerCmd ||
            SynchWorkerCmdDelegatedObjectSignaling == swcWorkerCmd)
        {
            SharedID shridMarshaledId = NULL;

            TRACE("Received %s cmd\n",
                  (swcWorkerCmd == SynchWorkerCmdRemoteSignal) ?
                  "REMOTE SIGNAL" : "DELEGATED OBJECT SIGNALING" );

            iRet = ReadBytesFromProcessPipe(WorkerCmdCompletionTimeout,
                                            (BYTE *)&shridMarshaledId,
                                            sizeof(shridMarshaledId));
            if (sizeof(shridMarshaledId) != iRet)
            {
                ERROR("Unable to read marshaled Shared ID from the "
                      "process pipe [pipe=%d ret=%d errno=%d (%s)]\n",
                      m_iProcessPipeRead, iRet, errno, strerror(errno));

                return ERROR_INTERNAL_ERROR;
            }

            TRACE("Received marshaled shrid=%p\n", (VOID *)shridMarshaledId);

            *pshridMarshaledData = shridMarshaledId;
        }

        if (SynchWorkerCmdDelegatedObjectSignaling == swcWorkerCmd)
        {
            DWORD dwData;

            iRet = ReadBytesFromProcessPipe(WorkerCmdCompletionTimeout,
                                            (BYTE *)&dwData,
                                            sizeof(dwData));
            if (sizeof(dwData) != iRet)
            {
                ERROR("Unable to read signal count from the "
                      "process pipe [pipe=%d ret=%d errno=%d (%s)]\n",
                      m_iProcessPipeRead, iRet, errno, strerror(errno));

                return ERROR_INTERNAL_ERROR;
            }

            TRACE("Received signal count %u\n", dwData);

            *pdwData = dwData;
        }

        *pswcWorkerCmd = swcWorkerCmd;
        return NO_ERROR;
    }

    /*++
    Method:
      CPalSynchronizationManager::ReadBytesFromProcessPipe

    Reads the specified number of bytes from the process pipe. If there is
    no data to be read on the pipe, it blocks until there is data available
    or the timeout expires.
    --*/
    int CPalSynchronizationManager::ReadBytesFromProcessPipe(
        int iTimeout,
        BYTE * pRecvBuf,
        LONG iBytes)
    {
#if !HAVE_KQUEUE
        struct pollfd Poll;
#endif // !HAVE_KQUEUE
        int iRet = -1;
        int iConsecutiveEintrs = 0;
        LONG iBytesRead = 0;
        BYTE * pPos = pRecvBuf;
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
        struct kevent keChanges;
        struct timespec ts, *pts;
        int iNChanges;
#endif // HAVE_KQUEUE

        _ASSERTE(0 <= iBytes);

        do
        {
            while (TRUE)
            {
                int iErrno = 0;
#if HAVE_KQUEUE
#if HAVE_BROKEN_FIFO_KEVENT
#if HAVE_BROKEN_FIFO_SELECT
#error Found no way to wait on a FIFO.
#endif

                timeval *ptv;
                timeval tv;

                if (INFTIM == iTimeout)
                {
                    ptv = NULL;
                }
                else
                {
                    tv.tv_usec = (iTimeout % tccSecondsToMillieSeconds) *
                        tccMillieSecondsToMicroSeconds;
                    tv.tv_sec = iTimeout / tccSecondsToMillieSeconds;
                    ptv = &tv;
                }

                fd_set readfds;
                FD_ZERO(&readfds);
                FD_SET(m_iProcessPipeRead, &readfds);
                iRet = select(m_iProcessPipeRead + 1, &readfds, NULL, NULL, ptv);

#else // HAVE_BROKEN_FIFO_KEVENT

                // Note: FreeBSD needs to use kqueue/kevent support here, since on this
                // platform the EOF notification on FIFOs is not surfaced through poll,
                // and process pipe shutdown relies on this feature.
                // If a thread is polling a FIFO or a pipe for POLLIN, when the last
                // write descriptor for that pipe is closed, poll() is supposed to
                // return with a POLLIN event but no data to be read on the FIFO/pipe,
                // which means EOF.
                // On FreeBSD such feature works for pipes but it doesn't for FIFOs.
                // Using kevent the EOF is instead surfaced correctly.

                if (iBytes > m_keProcessPipeEvent.data)
                {
                    if (INFTIM == iTimeout)
                    {
                        pts = NULL;
                    }
                    else
                    {
                        ts.tv_nsec = (iTimeout % tccSecondsToMillieSeconds) *
                            tccMillieSecondsToNanoSeconds;
                        ts.tv_sec = iTimeout / tccSecondsToMillieSeconds;
                        pts = &ts;
                    }

                    if (0 != (EV_EOF & m_keProcessPipeEvent.flags))
                    {
                        TRACE("Refreshing kevent settings\n");
                        EV_SET(&keChanges, m_iProcessPipeRead, EVFILT_READ,
                               EV_ADD | EV_CLEAR, 0, 0, 0);
                        iNChanges = 1;
                    }
                    else
                    {
                        iNChanges = 0;
                    }

                    iRet = kevent(m_iKQueue, &keChanges, iNChanges,
                                  &m_keProcessPipeEvent, 1, pts);

                    if (0 < iRet)
                    {
                        _ASSERTE(1 == iRet);
                        _ASSERTE(EVFILT_READ == m_keProcessPipeEvent.filter);

                        if (EV_ERROR & m_keProcessPipeEvent.flags)
                        {
                            ERROR("EV_ERROR from kevent [ident=%d filter=%d flags=%x]\n", m_keProcessPipeEvent.ident, m_keProcessPipeEvent.filter, m_keProcessPipeEvent.flags);
                            iRet = -1;
                            iErrno = m_keProcessPipeEvent.data;
                            m_keProcessPipeEvent.data = 0;
                        }
                    }
                    else if (0 > iRet)
                    {
                        iErrno = errno;
                    }

                    TRACE("Woken up from kevent() with ret=%d flags=%#x data=%d "
                          "[iTimeout=%d]\n", iRet, m_keProcessPipeEvent.flags,
                          m_keProcessPipeEvent.data, iTimeout);
                }
                else
                {
                    // There is enough data already available in the buffer, just use that.
                    iRet = 1;
                }

#endif // HAVE_BROKEN_FIFO_KEVENT
#else // HAVE_KQUEUE

                Poll.fd = m_iProcessPipeRead;
                Poll.events = POLLIN;
                Poll.revents = 0;

                iRet = poll(&Poll, 1, iTimeout);

                TRACE("Woken up from poll() with ret=%d [iTimeout=%d]\n",
                       iRet, iTimeout);

                if (1 == iRet &&
                    ((POLLERR | POLLHUP | POLLNVAL) & Poll.revents))
                {
                    // During PAL shutdown the pipe gets closed and Poll.revents is set to POLLHUP
                    // (note: no other flags are set). We will also receive an EOF on from the read call.
                    // Please see the comment for SynchWorkerCmdShutdown in CPalSynchronizationManager::WorkerThread.
                    if (!PALIsShuttingDown() || (Poll.revents != POLLHUP))
                    {
                        ERROR("Unexpected revents=%x while polling pipe %d\n",
                            Poll.revents, Poll.fd);
                        iErrno = EINVAL;
                        iRet = -1;
                    }
                }
                else if (0 > iRet)
                {
                    iErrno = errno;
                }

#endif // HAVE_KQUEUE

                if (0 == iRet || 1 == iRet)
                {
                    // 0 == wait timed out
                    // 1 == FIFO has data available
                    break;
                }
                else
                {
                    if (1 < iRet)
                    {
                        // Unexpected iRet > 1
                        ASSERT("Unexpected return code %d from blocking poll/kevent call\n",
                                iRet);
                        goto RBFPP_exit;
                    }

                    if (EINTR != iErrno)
                    {
                        // Unexpected error
                        ASSERT("Unexpected error from blocking poll/kevent call: %d (%s)\n",
                               iErrno, strerror(iErrno));
                        goto RBFPP_exit;
                    }

                    iConsecutiveEintrs++;
                    TRACE("poll() failed with EINTR; re-polling\n");

                    if (iConsecutiveEintrs >= MaxWorkerConsecutiveEintrs)
                    {
                        if (iTimeout != INFTIM)
                        {
                            WARN("Receiving too many EINTRs; converting one of them "
                                 "to a timeout");
                            iRet = 0;
                            break;
                        }
                        else if (0 == (iConsecutiveEintrs % MaxWorkerConsecutiveEintrs))
                        {
                            WARN("Receiving too many EINTRs [%d so far]",
                                 iConsecutiveEintrs);
                        }
                    }
                }
            }

            if (0 == iRet)
            {
                // Time out
                break;
            }
            else
            {
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
                if (0 != (EV_EOF & m_keProcessPipeEvent.flags) && 0 == m_keProcessPipeEvent.data)
                {
                    // EOF
                    TRACE("Received an EOF on process pipe via kevent\n");
                    goto RBFPP_exit;
                }
#endif // HAVE_KQUEUE

                iRet = read(m_iProcessPipeRead, pPos, iBytes - iBytesRead);

                if (0 == iRet)
                {
                    // Poll returned 1 and read returned zero: this is an EOF,
                    // i.e. no other process has the pipe still open for write
                    TRACE("Received an EOF on process pipe via poll\n");
                    goto RBFPP_exit;
                }
                else if (0 > iRet)
                {
                    ERROR("Unable to read %d bytes from the process pipe "
                          "[pipe=%d ret=%d errno=%d (%s)]\n", iBytes - iBytesRead,
                          m_iProcessPipeRead, iRet, errno, strerror(errno));
                    goto RBFPP_exit;
                }

                TRACE("Read %d bytes from process pipe\n", iRet);

                iBytesRead += iRet;
                pPos += iRet;

#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
                // Update available data count
                m_keProcessPipeEvent.data -= iRet;
                _ASSERTE(0 <= m_keProcessPipeEvent.data);
#endif // HAVE_KQUEUE
            }
        } while(iBytesRead < iBytes);

    RBFPP_exit:
        return (iRet < 0) ? iRet : iBytesRead;
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
                InternalNew<DeferredSignalingListNode>();

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
      CPalSynchronizationManager::ReadBytesFromProcessPipe

    Wakes up a remote thead currently sleeping for a wait or a sleep
    by sending the appropriate cmd to the remote process' worker
    thread, which will take care to convert this command into a
    WakeUpLocalThread in the remote process
    --*/
    PAL_ERROR CPalSynchronizationManager::WakeUpRemoteThread(
        SharedID shridWLNode)
    {
        const int MsgSize = sizeof(BYTE) + sizeof(SharedID);
        PAL_ERROR palErr = NO_ERROR;
        BYTE rgSendBuf[MsgSize];
        BYTE * pbySrc, * pbyDst = rgSendBuf;
        WaitingThreadsListNode * pWLNode = SharedIDToTypePointer(WaitingThreadsListNode, shridWLNode);


        _ASSERT_MSG(NULL != pWLNode, "Bad shared wait list node identifier (%p)\n", (VOID*)shridWLNode);
        _ASSERT_MSG(gPID != pWLNode->dwProcessId, "WakeUpRemoteThread called on local thread\n");
        _ASSERT_MSG(NULL != shridWLNode, "NULL shared identifier\n");
        _ASSERT_MSG(MsgSize <= PIPE_BUF, "Message too long [MsgSize=%d PIPE_BUF=%d]\n", MsgSize, (int)PIPE_BUF);

        TRACE("Waking up remote thread {pid=%x, tid=%x} by sending cmd=%u and shridWLNode=%p over process pipe\n",
              pWLNode->dwProcessId, pWLNode->dwThreadId, SynchWorkerCmdRemoteSignal, (VOID *)shridWLNode);

        // Prepare the message
        // Cmd
        *pbyDst++ = (BYTE)(SynchWorkerCmdRemoteSignal & 0xFF);

        // WaitingThreadsListNode (not aligned, copy byte by byte)
        pbySrc = (BYTE *)&shridWLNode;
        for (int i = 0; i < (int)sizeof(SharedID); i++)
        {
            *pbyDst++ = *pbySrc++;
        }

        _ASSERT_MSG(pbyDst <= rgSendBuf + MsgSize + 1, "Buffer overrun");

        // Send the message
        palErr = SendMsgToRemoteWorker(pWLNode->dwProcessId, rgSendBuf, MsgSize);
        if (NO_ERROR != palErr)
        {
            ERROR("Failed sending message to remote worker in process %u\n", pWLNode->dwProcessId);
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::DelegateSignalingToRemoteProcess

    This method transfers an object signaling operation to a remote process,
    where it will be performed by the worker thread. Such delegation takes
    place when the currently processed thread (among those waiting on the
    signald object) lives in a different process as the signaling thread,
    and it is performing a wait all. In this case generally is not possible
    to find out whether or not the wait all is satisfied, therefore the
    signaling operation must be continued in the target process.
    --*/
    PAL_ERROR CPalSynchronizationManager::DelegateSignalingToRemoteProcess(
        CPalThread * pthrCurrent,
        DWORD dwTargetProcessId,
        SharedID shridSynchData)
    {
        const int MsgSize = sizeof(BYTE) + sizeof(SharedID) + sizeof(DWORD);
        int i;
        PAL_ERROR palErr = NO_ERROR;
        BYTE rgSendBuf[MsgSize];
        BYTE * pbySrc, * pbyDst = rgSendBuf;
        DWORD dwSigCount;
        CSynchData * psdSynchData =
            SharedIDToTypePointer(CSynchData, shridSynchData);

        _ASSERT_MSG(gPID != dwTargetProcessId, " called on local thread\n");
        _ASSERT_MSG(NULL != shridSynchData, "NULL shared identifier\n");
        _ASSERT_MSG(NULL != psdSynchData, "Bad shared SynchData identifier (%p)\n", (VOID*)shridSynchData);
        _ASSERT_MSG(MsgSize <= PIPE_BUF, "Message too long [MsgSize=%d PIPE_BUF=%d]\n", MsgSize, (int)PIPE_BUF);

        TRACE("Transfering wait all signaling to remote process pid=%x by sending cmd=%u and shridSynchData=%p over process pipe\n",
              dwTargetProcessId, SynchWorkerCmdDelegatedObjectSignaling, (VOID *)shridSynchData);

        dwSigCount = psdSynchData->GetSignalCount();

        // AddRef SynchData to be marshaled to remote process
        psdSynchData->AddRef();

        //
        // Prepare the message
        //

        // Cmd
        *pbyDst++ = (BYTE)(SynchWorkerCmdDelegatedObjectSignaling & 0xFF);

        // CSynchData (not aligned, copy byte by byte)
        pbySrc = (BYTE *)&shridSynchData;
        for (i=0; i<(int)sizeof(SharedID); i++)
        {
            *pbyDst++ = *pbySrc++;
        }

        // Signal Count (not aligned, copy byte by byte)
        pbySrc = (BYTE *)&dwSigCount;
        for (i=0; i<(int)sizeof(DWORD); i++)
        {
            *pbyDst++ = *pbySrc++;
        }

        _ASSERT_MSG(pbyDst <= rgSendBuf + MsgSize + 1, "Buffer overrun");

        // Send the message
        palErr = SendMsgToRemoteWorker(dwTargetProcessId, rgSendBuf, MsgSize);
        if (NO_ERROR != palErr)
        {
            TRACE("Failed sending message to remote worker in process %u\n", dwTargetProcessId);

            // Undo refcounting
            psdSynchData->Release(pthrCurrent);
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::SendMsgToRemoteWorker

    Sends a message (command + data) to a remote process's worker thread.
    --*/
    PAL_ERROR CPalSynchronizationManager::SendMsgToRemoteWorker(
        DWORD dwProcessId,
        BYTE * pMsg,
        int iMsgSize)
    {
#ifndef CORECLR
        PAL_ERROR palErr = NO_ERROR;
        int iProcessPipe, iBytesToWrite, iRetryCount;
        ssize_t sszRet;
        char strPipeFilename[MAX_PATH];
        BYTE * pPos = pMsg;
        bool fRet;
        CPalThread *pthrCurrent = InternalGetCurrentThread();

        _ASSERT_MSG(gPID != dwProcessId, "SendMsgToRemoteWorker called with local process as target process\n");

        fRet = GetProcessPipeName(strPipeFilename, MAX_PATH, dwProcessId);

        _ASSERT_MSG(fRet, "Failed to retrieve process pipe's name!\n");

        iProcessPipe = InternalOpen(strPipeFilename, O_WRONLY);
        if (-1 == iProcessPipe)
        {
            ERROR("Unable to open a process pipe to wake up a remote thread "
                  "[pid=%u errno=%d (%s) PipeFilename=%s]\n", dwProcessId,
                  errno, strerror(errno), strPipeFilename);
            palErr = ERROR_INTERNAL_ERROR;
            goto SMTRW_exit;
        }

        pPos = pMsg;
        iBytesToWrite = iMsgSize;
        while (0 < iBytesToWrite)
        {
            iRetryCount = 0;
            do
            {
                sszRet = write(iProcessPipe, pPos, iBytesToWrite);
            } while (-1 == sszRet &&
                     EAGAIN == errno &&
                     ++iRetryCount < MaxConsecutiveEagains &&
                     0 == sched_yield());

            if (0 >= sszRet)
            {
                ERROR("Error writing message to process pipe %d [target_pid=%u "
                      "bytes_to_write=%d bytes_written=%d ret=%d errno=%d (%s) "
                      "PipeFilename=%s]\n", iProcessPipe, dwProcessId, iMsgSize,
                      iMsgSize - iBytesToWrite, (int)sszRet, errno, strerror(errno),
                      strPipeFilename);
                palErr = ERROR_INTERNAL_ERROR;
                break;
            }
            iBytesToWrite -= (int)sszRet;
            pPos += sszRet;

            _ASSERT_MSG(0 == iBytesToWrite,
                        "Interleaved messages while writing to process pipe %d\n",
                        iProcessPipe);
        }

        // Close the opened pipe
        close(iProcessPipe);

    SMTRW_exit:
        return palErr;
#else // !CORECLR
        ASSERT("There should never be a reason to send a message to a remote worker\n");
        return ERROR_INTERNAL_ERROR;
#endif // !CORECLR
    }

    /*++
    Method:
      CPalSynchronizationManager::WakeUpLocalWorkerThread

    Wakes up the local worker thread by writing a 'nop' cmd to the
    process pipe.
    --*/
    PAL_ERROR CPalSynchronizationManager::WakeUpLocalWorkerThread(
        SynchWorkerCmd swcWorkerCmd)
    {
        PAL_ERROR palErr = NO_ERROR;

        _ASSERT_MSG((swcWorkerCmd & 0xFF) == swcWorkerCmd,
                    "Value too big for swcWorkerCmd\n");

        _ASSERT_MSG((SynchWorkerCmdNop == swcWorkerCmd) ||
                    (SynchWorkerCmdShutdown == swcWorkerCmd) ||
                    (SynchWorkerCmdTerminationRequest == swcWorkerCmd),
                    "WakeUpLocalWorkerThread supports only SynchWorkerCmdNop, SynchWorkerCmdShutdown, and SynchWorkerCmdTerminationRequest."
                    "[received cmd=%d]\n", swcWorkerCmd);

        BYTE byCmd = (BYTE)(swcWorkerCmd & 0xFF);

        TRACE("Waking up Synch Worker Thread for %u [byCmd=%u]\n",
                    swcWorkerCmd, (unsigned int)byCmd);

        // As long as we use pipes and we keep the message size
        // within PIPE_BUF, there's no need to lock here, since the
        // write is guaranteed not to be interleaved with/into other
        // writes of PIPE_BUF bytes or less.
        _ASSERT_MSG(sizeof(BYTE) <= PIPE_BUF, "Message too long\n");

        int iRetryCount = 0;
        ssize_t sszWritten;
        do
        {
            sszWritten = write(m_iProcessPipeWrite, &byCmd, sizeof(BYTE));
        } while (-1 == sszWritten &&
                 EAGAIN == errno &&
                 ++iRetryCount < MaxConsecutiveEagains &&
                 0 == sched_yield());

        if (sszWritten != sizeof(BYTE))
        {
            ERROR("Unable to write to the process pipe to wake up the "
                   "worker thread [errno=%d (%s)]\n", errno, strerror(errno));
            palErr = ERROR_INTERNAL_ERROR;
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
        ThreadWaitInfo * ptwiWaitInfo,
        bool fHaveSharedLock)
    {
        int i = 0;
        CSynchData * psdSynchData = NULL;
        bool fSharedSynchLock = false;

        if (!fHaveSharedLock && LocalWait != ptwiWaitInfo->wdWaitDomain)
        {
            AcquireSharedSynchLock(pthrCurrent);
            fSharedSynchLock = true;
        }

        TRACE("Unregistering wait for thread=%u [ObjCount=%d WaitType=%u WaitDomain=%u]\n",
              ptwiWaitInfo->pthrOwner->GetThreadId(),
              ptwiWaitInfo->lObjCount, ptwiWaitInfo->wtWaitType,
              ptwiWaitInfo->wdWaitDomain);

        for (i=0; i < ptwiWaitInfo->lObjCount; i++)
        {
            WaitingThreadsListNode * pwtlnItem = ptwiWaitInfo->rgpWTLNodes[i];

            VALIDATEOBJECT(pwtlnItem);

            if (pwtlnItem->dwFlags & WTLN_FLAG_OWNER_OBJECT_IS_SHARED)
            {
                // Shared object
                WaitingThreadsListNode * pwtlnItemNext, * pwtlnItemPrev;

                psdSynchData = SharedIDToTypePointer(CSynchData,
                    pwtlnItem->ptrOwnerObjSynchData.shrid);

                VALIDATEOBJECT(psdSynchData);

                pwtlnItemNext = SharedIDToTypePointer(WaitingThreadsListNode,
                    pwtlnItem->ptrNext.shrid);
                pwtlnItemPrev = SharedIDToTypePointer(WaitingThreadsListNode,
                    pwtlnItem->ptrPrev.shrid);
                if (pwtlnItemPrev)
                {
                    VALIDATEOBJECT(pwtlnItemPrev);
                    pwtlnItemPrev->ptrNext.shrid = pwtlnItem->ptrNext.shrid;
                }
                else
                {
                    psdSynchData->SetWTLHeadShrPtr(pwtlnItem->ptrNext.shrid);
                }

                if (pwtlnItemNext)
                {
                    VALIDATEOBJECT(pwtlnItemNext);
                    pwtlnItemNext->ptrPrev.shrid = pwtlnItem->ptrPrev.shrid;
                }
                else
                {
                    psdSynchData->SetWTLTailShrPtr(pwtlnItem->ptrPrev.shrid);
                }

                m_cacheSHRWTListNodes.Add(pthrCurrent, pwtlnItem->shridSHRThis);
            }
            else
            {
                // Local object
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
            }

            // Release the node's refcount on the synch data, and decerement
            // waiting thread count
            psdSynchData->DecrementWaitingThreadCount();
            psdSynchData->Release(pthrCurrent);
        }

        // Reset wait data in ThreadWaitInfo structure: it is enough
        // to reset lObjCount, lSharedObjCount and wdWaitDomain.
        ptwiWaitInfo->lObjCount       = 0;
        ptwiWaitInfo->lSharedObjCount = 0;
        ptwiWaitInfo->wdWaitDomain    = LocalWait;

        // Done
        if (fSharedSynchLock)
        {
            ReleaseSharedSynchLock(pthrCurrent);
        }

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

        _ASSERT_MSG(gPID == pwtlnNode->dwProcessId,
            "UnsignalRestOfLocalAwakeningWaitAll() called on a wait all with remote awakening");

        ThreadWaitInfo *ptwiWaitInfo = pwtlnNode->ptwiWaitInfo;

        int iObjCount = ptwiWaitInfo->lObjCount;
        for (int i = 0; i < iObjCount; i++)
        {
            WaitingThreadsListNode * pwtlnItem = ptwiWaitInfo->rgpWTLNodes[i];

            VALIDATEOBJECT(pwtlnItem);

            if (0 != (WTLN_FLAG_OWNER_OBJECT_IS_SHARED & pwtlnItem->dwFlags))
            {
                psdSynchDataItem = SharedIDToTypePointer(CSynchData, pwtlnItem->ptrOwnerObjSynchData.shrid);
            }
            else
            {
                psdSynchDataItem = pwtlnItem->ptrOwnerObjSynchData.ptr;
            }

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
      CPalSynchronizationManager::MarkWaitForDelegatedObjectSignalingInProgress

    Marks all the thread waiting list nodes involved in the current wait-all
    for "delegated object signaling in progress", so that this wait cannot be
    involved in another delegated object signaling that may happen while the
    current object singaling is being tranfered to the target process (while
    transfering it, synchronization locks are released in this process and later
    grabbed again in the target process; in this time window another thread
    could signal another object part of the same wait-all. In this case no
    signal delegation must take place.

    Note: this method must be called while holding the synchronization locks
          appropriate to the target object described by pwtlnNode (i.e. the
          local process synch lock if the target object is local, both local
          and shared one if the object is shared).
    --*/
    void CPalSynchronizationManager::MarkWaitForDelegatedObjectSignalingInProgress(
        CPalThread * pthrCurrent,
        WaitingThreadsListNode * pwtlnNode)
    {
        bool fSharedSynchLock = false;
        bool fTargetObjectIsShared = (0 != (WTLN_FLAG_OWNER_OBJECT_IS_SHARED & pwtlnNode->dwFlags));

        VALIDATEOBJECT(pwtlnNode);

        _ASSERT_MSG(gPID == pwtlnNode->dwProcessId,
            "MarkWaitForDelegatedObjectSignalingInProgress() called from the wrong process");

        ThreadWaitInfo *ptwiWaitInfo = pwtlnNode->ptwiWaitInfo;

        if (!fSharedSynchLock && !fTargetObjectIsShared &&
            LocalWait != ptwiWaitInfo->wdWaitDomain)
        {
            AcquireSharedSynchLock(pthrCurrent);
            fSharedSynchLock = true;
        }

        _ASSERT_MSG(MultipleObjectsWaitAll == ptwiWaitInfo->wtWaitType,
            "MarkWaitForDelegatedObjectSignalingInProgress() called on a normal (non wait-all) wait");

        // Unmark all nodes other than the target one
        int iTgtCount = ptwiWaitInfo->lObjCount;
        for (int i = 0; i < iTgtCount; i++)
        {
            VALIDATEOBJECT(ptwiWaitInfo->rgpWTLNodes[i]);
            ptwiWaitInfo->rgpWTLNodes[i]->dwFlags &= ~WTLN_FLAG_DELEGATED_OBJECT_SIGNALING_IN_PROGRESS;
        }

        // Mark the target node
        pwtlnNode->dwFlags |= WTLN_FLAG_DELEGATED_OBJECT_SIGNALING_IN_PROGRESS;

        // Done
        if (fSharedSynchLock)
        {
            ReleaseSharedSynchLock(pthrCurrent);
        }

        return;
    }

    /*++
    Method:
      CPalSynchronizationManager::UnmarkTWListForDelegatedObjectSignalingInProgress

    Resets the "delegated object signaling in progress" flags in all the
    nodes of the thread waitin list for the target waitable objects (represented
    by its SynchData)

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    void CPalSynchronizationManager::UnmarkTWListForDelegatedObjectSignalingInProgress(
        CSynchData * pTgtObjectSynchData)
    {
        bool fSharedObject = (SharedObject == pTgtObjectSynchData->GetObjectDomain());
        WaitingThreadsListNode * pwtlnNode;

        VALIDATEOBJECT(pTgtObjectSynchData);

        pwtlnNode =  fSharedObject ? SharedIDToTypePointer(WaitingThreadsListNode, pTgtObjectSynchData->GetWTLHeadShmPtr())
                                   : pTgtObjectSynchData->GetWTLHeadPtr();

        while (pwtlnNode)
        {
            VALIDATEOBJECT(pwtlnNode);

            pwtlnNode->dwFlags &= ~WTLN_FLAG_DELEGATED_OBJECT_SIGNALING_IN_PROGRESS;
            pwtlnNode = fSharedObject ? SharedIDToTypePointer(WaitingThreadsListNode, pwtlnNode->ptrNext.shrid)
                                      : pwtlnNode->ptrNext.ptr;
        }
    }

    /*++
    Method:
      CPalSynchronizationManager::RegisterProcessForMonitoring

    Registers the process object represented by the passed psdSynchData and
    pProcLocalData. The worker thread will monitor the actual process and,
    upon process termination, it will set the exit code in pProcLocalData,
    and it will signal the process object, by signaling its psdSynchData.
    --*/
    PAL_ERROR CPalSynchronizationManager::RegisterProcessForMonitoring(
        CPalThread * pthrCurrent,
        CSynchData *psdSynchData,
        IPalObject *pProcessObject,
        CProcProcessLocalData * pProcLocalData)
    {
        PAL_ERROR palErr = NO_ERROR;
        MonitoredProcessesListNode * pmpln;
        bool fWakeUpWorker = false;
        bool fMonitoredProcessesLock = false;

        VALIDATEOBJECT(psdSynchData);

        InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);

        fMonitoredProcessesLock = true;

        pmpln = m_pmplnMonitoredProcesses;
        while (pmpln)
        {
            if (psdSynchData == pmpln->psdSynchData)
            {
                _ASSERT_MSG(pmpln->dwPid == pProcLocalData->dwProcessId, "Invalid node in Monitored Processes List\n");
                break;
            }

            pmpln = pmpln->pNext;
        }

        if (pmpln)
        {
            pmpln->lRefCount++;
        }
        else
        {
            pmpln = InternalNew<MonitoredProcessesListNode>();
            if (NULL == pmpln)
            {
                ERROR("No memory to allocate MonitoredProcessesListNode structure\n");
                palErr = ERROR_NOT_ENOUGH_MEMORY;
                goto RPFM_exit;
            }

            pmpln->lRefCount      = 1;
            pmpln->dwPid          = pProcLocalData->dwProcessId;
            pmpln->dwExitCode     = 0;
            pmpln->pProcessObject = pProcessObject;
            pmpln->pProcessObject->AddReference();
            pmpln->pProcLocalData = pProcLocalData;

            // Acquire SynchData and AddRef it
            pmpln->psdSynchData = psdSynchData;
            psdSynchData->AddRef();

            pmpln->pNext = m_pmplnMonitoredProcesses;
            m_pmplnMonitoredProcesses = pmpln;
            m_lMonitoredProcessesCount++;

            fWakeUpWorker = true;
        }

        // Unlock
        InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        fMonitoredProcessesLock = false;

        if (fWakeUpWorker)
        {
            CPalSynchronizationManager * pSynchManager = GetInstance();

            palErr = pSynchManager->WakeUpLocalWorkerThread(SynchWorkerCmdNop);
            if (NO_ERROR != palErr)
            {
                ERROR("Failed waking up worker thread for process "
                      "monitoring registration [errno=%d {%s%}]\n",
                      errno, strerror(errno));
                palErr = ERROR_INTERNAL_ERROR;
            }
        }

    RPFM_exit:
        if (fMonitoredProcessesLock)
        {
            InternalLeaveCriticalSection(pthrCurrent,
                                         &s_csMonitoredProcessesLock);
        }

        return palErr;
    }

    /*++
    Method:
      CPalSynchronizationManager::UnRegisterProcessForMonitoring

    Unregisters a process object currently monitored by the worker thread
    (typically called if the wait timed out before the process exited, or
    if the wait was a normal (i.e. non wait-all) wait that involved othter
    objects, and another object has been signaled).
    --*/
    PAL_ERROR CPalSynchronizationManager::UnRegisterProcessForMonitoring(
        CPalThread * pthrCurrent,
        CSynchData *psdSynchData,
        DWORD dwPid)
    {
        PAL_ERROR palErr = NO_ERROR;
        MonitoredProcessesListNode * pmpln, * pmplnPrev = NULL;

        VALIDATEOBJECT(psdSynchData);

        InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);

        pmpln = m_pmplnMonitoredProcesses;
        while (pmpln)
        {
            if (psdSynchData == pmpln->psdSynchData)
            {
                _ASSERT_MSG(dwPid == pmpln->dwPid, "Invalid node in Monitored Processes List\n");
                break;
            }

            pmplnPrev = pmpln;
            pmpln = pmpln->pNext;
        }

        if (pmpln)
        {
            if (0 == --pmpln->lRefCount)
            {
                if (NULL != pmplnPrev)
                {
                    pmplnPrev->pNext = pmpln->pNext;
                }
                else
                {
                    m_pmplnMonitoredProcesses = pmpln->pNext;
                }

                m_lMonitoredProcessesCount--;
                pmpln->pProcessObject->ReleaseReference(pthrCurrent);
                pmpln->psdSynchData->Release(pthrCurrent);
                InternalDelete(pmpln);
            }
        }
        else
        {
            palErr = ERROR_NOT_FOUND;
        }

        InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        return palErr;
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
      CPalSynchronizationManager::DoMonitorProcesses

    This method is called by the worker thread to execute one step of
    monitoring for all the process currently registered for monitoring
    --*/
    LONG CPalSynchronizationManager::DoMonitorProcesses(
        CPalThread * pthrCurrent)
    {
        MonitoredProcessesListNode * pNode, * pPrev = NULL, * pNext;
        LONG lInitialNodeCount;
        LONG lRemovingCount = 0;
        bool fLocalSynchLock = false;
        bool fSharedSynchLock = false;
        bool fMonitoredProcessesLock = false;

        // Note: we first need to grab the monitored processes lock to walk
        //       the list of monitored processes, and then, if there is any
        //       which exited, to grab the synchronization lock(s) to signal
        //       the process object. Anyway we cannot grab the synchronization
        //       lock(s) while holding the monitored processes lock; that
        //       would cause deadlock, since RegisterProcessForMonitoring and
        //       UnRegisterProcessForMonitoring call stacks grab the locks
        //       in the opposite order. Grabbing the synch lock(s) first (and
        //       therefore all the times) would cause unacceptable contention
        //       (process monitoring is done in polling mode).
        //       Therefore we need to remove list nodes for processes that
        //       exited copying them to the exited array, while holding only
        //       the monitored processes lock, and then to signal them from that
        //       array holding synch lock(s) and monitored processes lock,
        //       acquired in this order. Holding again the monitored processes
        //       lock is needed in order to support object promotion.

        // Grab the monitored processes lock
        InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        fMonitoredProcessesLock = true;

        lInitialNodeCount = m_lMonitoredProcessesCount;

        pNode = m_pmplnMonitoredProcesses;
        while (pNode)
        {
            pNext = pNode->pNext;

            if (HasProcessExited(pNode->dwPid,
                                 &pNode->dwExitCode,
                                 &pNode->fIsActualExitCode))
            {
                TRACE("Process %u exited with return code %u\n",
                      pNode->dwPid,
                      pNode->fIsActualExitCode ? "actual" : "guessed",
                      pNode->dwExitCode);

                if (NULL != pPrev)
                {
                    pPrev->pNext = pNext;
                }
                else
                {
                    m_pmplnMonitoredProcesses = pNext;
                }

                m_lMonitoredProcessesCount--;

                // Insert in the list of nodes for exited processes
                pNode->pNext = m_pmplnExitedNodes;
                m_pmplnExitedNodes = pNode;
                lRemovingCount++;
            }
            else
            {
                pPrev = pNode;
            }

            // Go to the next
            pNode = pNext;
        }

        // Release the monitored processes lock
        InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        fMonitoredProcessesLock = false;

        if (lRemovingCount > 0)
        {
            // First grab the local synch lock
            AcquireLocalSynchLock(pthrCurrent);
            fLocalSynchLock = true;

            // Acquire the monitored processes lock
            InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
            fMonitoredProcessesLock = true;

            if (!fSharedSynchLock)
            {
                bool fSharedSynchLockIsNeeded = false;

                // See if the shared lock is needed
                pNode = m_pmplnExitedNodes;
                while (pNode)
                {
                    if (SharedObject == pNode->psdSynchData->GetObjectDomain())
                    {
                        fSharedSynchLockIsNeeded = true;
                        break;
                    }

                    pNode = pNode->pNext;
                }

                if (fSharedSynchLockIsNeeded)
                {
                    // Release the monitored processes lock
                    InternalLeaveCriticalSection(pthrCurrent,
                                                 &s_csMonitoredProcessesLock);
                    fMonitoredProcessesLock = false;

                    // Acquire the shared synch lock
                    AcquireSharedSynchLock(pthrCurrent);
                    fSharedSynchLock = true;

                    // Acquire again the monitored processes lock
                    InternalEnterCriticalSection(pthrCurrent,
                                                 &s_csMonitoredProcessesLock);
                    fMonitoredProcessesLock = true;
                }
            }

            // Start from the beginning of the exited processes list
            pNode = m_pmplnExitedNodes;

            // Invalidate the list
            m_pmplnExitedNodes = NULL;

            while (pNode)
            {
                pNext = pNode->pNext;

                TRACE("Process pid=%u exited with exitcode=%u\n",
                      pNode->dwPid, pNode->dwExitCode);

                // Store the exit code in the process local data
                if (pNode->fIsActualExitCode)
                {
                    pNode->pProcLocalData->dwExitCode = pNode->dwExitCode;
                }

                // Set process status to PS_DONE
                pNode->pProcLocalData->ps = PS_DONE;

                // Set signal count
                pNode->psdSynchData->SetSignalCount(1);

                // Releasing all local waiters
                //
                // We just called directly in CSynchData::SetSignalCount(), so
                // we need to take care of waking up waiting threads according
                // to the Process object semantics (i.e. every thread must be
                // awakend). Anyway if a process object is shared among two or
                // more processes and threads from different processes are
                // waiting on it, the object will be registered for monitoring
                // in each of the processes. As result its signal count will
                // be set to one more times (which is not a problem, given the
                // process object semantics) and each worker thread will wake
                // up waiting threads. Therefore we need to make sure that each
                // worker wakes up only threads in its own process: we do that
                // by calling ReleaseAllLocalWaiters
                pNode->psdSynchData->ReleaseAllLocalWaiters(pthrCurrent);

                // We are done with pProcLocalData, so we can release the process object
                pNode->pProcessObject->ReleaseReference(pthrCurrent);

                // Release the reference to the SynchData
                pNode->psdSynchData->Release(pthrCurrent);

                // Delete the node
                InternalDelete(pNode);

                // Go to the next
                pNode = pNext;
            }
        }

        if (fMonitoredProcessesLock)
        {
            InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        }

        if (fSharedSynchLock)
        {
            ReleaseSharedSynchLock(pthrCurrent);
        }

        if (fLocalSynchLock)
        {
            ReleaseLocalSynchLock(pthrCurrent);
        }

        return (lInitialNodeCount - lRemovingCount);
    }

    /*++
    Method:
      CPalSynchronizationManager::DiscardMonitoredProcesses

    This method is called at shutdown time to discard all the registration
    for the processes currently monitored by the worker thread.
    This method must be called at shutdown time, otherwise some shared memory
    may be leaked at process shutdown.
    --*/
    void CPalSynchronizationManager::DiscardMonitoredProcesses(
        CPalThread * pthrCurrent)
    {
        MonitoredProcessesListNode * pNode;

        // Grab the monitored processes lock
        InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);

        while (m_pmplnMonitoredProcesses)
        {
            pNode = m_pmplnMonitoredProcesses;
            m_pmplnMonitoredProcesses = pNode->pNext;
            pNode->pProcessObject->ReleaseReference(pthrCurrent);
            pNode->psdSynchData->Release(pthrCurrent);
            InternalDelete(pNode);
        }

        // Release the monitored processes lock
        InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
    }

    /*++
    Method:
      CPalSynchronizationManager::CreateProcessPipe

    Creates the process pipe for the current process
    --*/
    bool CPalSynchronizationManager::CreateProcessPipe()
    {
        bool fRet = true;
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
        int iKq = -1;
#endif // HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT

#ifndef CORECLR
        int iPipeRd = -1, iPipeWr = -1;
        char szPipeFilename[MAX_PATH];

        /* Create the blocking pipe */
        if (!GetProcessPipeName(szPipeFilename, MAX_PATH, gPID))
        {
            ERROR("couldn't get process pipe's name\n");
            szPipeFilename[0] = 0;
            fRet = false;
            goto CPP_exit;
        }

        /* create the pipe, with full access to the owner only */
        if (mkfifo(szPipeFilename, S_IRWXU) == -1)
        {
            if (errno == EEXIST)
            {
                /* Some how no one deleted the pipe, perhaps it was left behind
                from a crash?? Delete the pipe and try again. */
                if (-1 == unlink(szPipeFilename))
                {
                    ERROR( "Unable to delete the process pipe that was left behind.\n" );
                    fRet = false;
                    goto CPP_exit;
                }
                else
                {
                    if (mkfifo(szPipeFilename, S_IRWXU) == -1)
                    {
                        ERROR( "Still unable to create the process pipe...giving up!\n" );
                        fRet = false;
                        goto CPP_exit;
                    }
                }
            }
            else
            {
                ERROR( "Unable to create the process pipe.\n" );
                fRet = false;
                goto CPP_exit;
            }
        }

        iPipeRd = InternalOpen(szPipeFilename, O_RDONLY | O_NONBLOCK);
        if (iPipeRd == -1)
        {
            ERROR("Unable to open the process pipe for read\n");
            fRet = false;
            goto CPP_exit;
        }

        iPipeWr = InternalOpen(szPipeFilename, O_WRONLY | O_NONBLOCK);
        if (iPipeWr == -1)
        {
            ERROR("Unable to open the process pipe for write\n");
            fRet = false;
            goto CPP_exit;
        }
#else // !CORECLR
        int rgiPipe[] = { -1, -1 };
        int pipeRv =
#if HAVE_PIPE2
            pipe2(rgiPipe, O_CLOEXEC);
#else
            pipe(rgiPipe);
#endif // HAVE_PIPE2
        if (pipeRv == -1)
        {
            ERROR("Unable to create the process pipe\n");
            fRet = false;
            goto CPP_exit;
        }
#if !HAVE_PIPE2
        fcntl(rgiPipe[0], F_SETFD, FD_CLOEXEC); // make pipe non-inheritable, if possible
        fcntl(rgiPipe[1], F_SETFD, FD_CLOEXEC);
#endif // !HAVE_PIPE2
#endif // !CORECLR

#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
        iKq = kqueue();
        if (-1 == iKq)
        {
            ERROR("Failed to create kqueue associated to process pipe\n");
            fRet = false;
            goto CPP_exit;
        }
#endif // HAVE_KQUEUE

    CPP_exit:
        if (fRet)
        {
            // Succeeded
#ifndef CORECLR
            m_iProcessPipeRead = iPipeRd;
            m_iProcessPipeWrite = iPipeWr;
#else // !CORECLR
            m_iProcessPipeRead = rgiPipe[0];
            m_iProcessPipeWrite = rgiPipe[1];
#endif // !CORECLR
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
            m_iKQueue = iKq;
#endif // HAVE_KQUEUE
        }
        else
        {
#ifndef CORECLR
            // Failed
            if (0 != szPipeFilename[0])
            {
                unlink(szPipeFilename);
            }
            if (-1 != iPipeRd)
            {
                close(iPipeRd);
            }
            if (-1 != iPipeWr)
            {
                close(iPipeWr);
            }
#else // !CORECLR
            if (-1 != rgiPipe[0])
            {
                close(rgiPipe[0]);
                close(rgiPipe[1]);
            }
#endif // !CORECLR
#if HAVE_KQUEUE && !HAVE_BROKEN_FIFO_KEVENT
            if (-1 != iKq)
            {
                close(iKq);
            }
#endif // HAVE_KQUEUE
        }

        return fRet;
    }

    /*++
    Method:
      CPalSynchronizationManager::ShutdownProcessPipe

    Shuts down the process pipe and removes the fifo so that other processes
    can no longer open it. It also closes the local write end of the pipe (see
    comment below). From this moment on the worker thread will process any
    possible data already received in the pipe (but not yet consumed) and any
    data written by processes that still have a opened write end of this pipe;
    it will wait (with timeout) until the last remote process which has a write
    end opened closes it, and then it will yield to process shutdown.
    --*/
    PAL_ERROR CPalSynchronizationManager::ShutdownProcessPipe()
    {
        PAL_ERROR palErr = NO_ERROR;
#ifndef CORECLR
        char szPipeFilename[MAX_PATH];

        if (GetProcessPipeName(szPipeFilename, MAX_PATH, gPID))
        {
            if (unlink(szPipeFilename) == -1)
            {
                ERROR("Unable to unlink the pipe file name errno=%d (%s)\n",
                      errno, strerror(errno));
                palErr = ERROR_INTERNAL_ERROR;
                // go on anyway
            }
        }
        else
        {
            ERROR("Couldn't get the process pipe's name\n");
            palErr = ERROR_INTERNAL_ERROR;
            // go on anyway
        }
#endif // CORECLR

        if (-1 != m_iProcessPipeWrite)
        {
            // Closing the write end of the process pipe. When the last process
            // that still has a open write-fd on this pipe will close it, the
            // worker thread will receive an EOF; the worker thread will wait
            // for this EOF before shutting down, so to ensure to process any
            // possible data already written to the pipe by other processes
            // when the shutdown has been initiated in the current process.
            // Note: no need here to worry about platforms where close(pipe)
            // blocks on outstanding syscalls, since we are the only one using
            // this fd.
            TRACE("Closing the write end of process pipe\n");
            if (close(m_iProcessPipeWrite) == -1)
            {
                ERROR("Unable to close the write end of process pipe\n");
                palErr = ERROR_INTERNAL_ERROR;
            }

            m_iProcessPipeWrite = -1;
        }

        return palErr;
    }

#ifndef CORECLR
    /*++
    Method:
      CPalSynchronizationManager::GetProcessPipeName

    Returns the process pipe name for the target process (identified by its PID)
    --*/
    bool CPalSynchronizationManager::GetProcessPipeName(
        LPSTR pDest,
        int iDestSize,
        DWORD dwPid)
    {
        CHAR config_dir[MAX_PATH];
        int needed_size;

        _ASSERT_MSG(NULL != pDest, "Destination pointer is NULL!\n");
        _ASSERT_MSG(0 < iDestSize,"Invalid buffer size %d\n", iDestSize);

        if (!PALGetPalConfigDir(config_dir, MAX_PATH))
        {
            ASSERT("Unable to determine the PAL config directory.\n");
            pDest[0] = '\0';
            return false;
        }
        needed_size = snprintf(pDest, iDestSize, "%s/%s-%u", config_dir,
                               PROCESS_PIPE_NAME_PREFIX, dwPid);
        pDest[iDestSize-1] = 0;
        if(needed_size >= iDestSize)
        {
            ERROR("threadpipe name needs %d characters, buffer only has room for "
                  "%d\n", needed_size, iDestSize+1);
            return false;
        }
        return true;
    }
#endif // !CORECLR

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

    /*++
    Method:
      CPalSynchronizationManager::PromoteObjectSynchData

    Promotes an object's synchdata from local to shared
    --*/
    PAL_ERROR CPalSynchronizationManager::PromoteObjectSynchData(
        CPalThread *pthrCurrent,
        VOID *pvLocalSynchData,
        VOID **ppvSharedSynchData)
    {
        PAL_ERROR palError = NO_ERROR;
        CSynchData *psdLocal = reinterpret_cast<CSynchData *>(pvLocalSynchData);
        CSynchData *psdShared = NULL;
        SharedID shridSynchData = NULL;
        SharedID *rgshridWTLNodes = NULL;
        CObjectType *pot = NULL;
        ULONG ulcWaitingThreads;

        _ASSERTE(NULL != pthrCurrent);
        _ASSERTE(NULL != pvLocalSynchData);
        _ASSERTE(NULL != ppvSharedSynchData);
        _ASSERTE(ProcessLocalObject == psdLocal->GetObjectDomain());

#if _DEBUG

        //
        // TODO: Verify that the proper locks are held
        //
#endif

        //
        // Allocate shared memory CSynchData and map to local memory
        //

        shridSynchData = m_cacheSHRSynchData.Get(pthrCurrent);
        if (NULL == shridSynchData)
        {
            ERROR("Unable to allocate shared memory\n");
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto POSD_exit;
        }

        psdShared = SharedIDToTypePointer(CSynchData, shridSynchData);
        _ASSERTE(NULL != psdShared);

        //
        // Allocate shared memory WaitingThreadListNodes if there are
        // any threads currently waiting on this object
        //

        ulcWaitingThreads = psdLocal->GetWaitingThreadCount();
        if (0 < ulcWaitingThreads)
        {
            int i;

            rgshridWTLNodes = InternalNewArray<SharedID>(ulcWaitingThreads);
            if (NULL == rgshridWTLNodes)
            {
                palError = ERROR_OUTOFMEMORY;
                goto POSD_exit;
            }

            i = m_cacheSHRWTListNodes.Get(
                    pthrCurrent,
                    ulcWaitingThreads,
                    rgshridWTLNodes
                    );

            if (static_cast<ULONG>(i) != ulcWaitingThreads)
            {
                for (i -= 1; i >= 0; i -= 1)
                {
                    m_cacheSHRWTListNodes.Add(pthrCurrent, rgshridWTLNodes[i]);
                }

                palError = ERROR_OUTOFMEMORY;
                goto POSD_exit;
            }
        }

        //
        // If the synch data is for a process object we need to grab
        // the monitored process list lock here
        //

        pot = psdLocal->GetObjectType();
        _ASSERTE(NULL != pot);

        if (otiProcess == pot->GetId())
        {
            InternalEnterCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        }

        //
        // Copy pertinent CSynchData info to the shared memory version (and
        // initialize other members)
        //

        psdShared->SetSharedThis(shridSynchData);
        psdShared->SetObjectDomain(SharedObject);
        psdShared->SetObjectType(psdLocal->GetObjectType());
        psdShared->SetSignalCount(psdLocal->GetSignalCount());

#ifdef SYNCH_STATISTICS
        psdShared->SetStatContentionCount(psdLocal->GetStatContentionCount());
        psdShared->SetStatWaitCount(psdLocal->GetStatWaitCount());
#endif

        //
        // Rebuild the waiting thread list, and update the wait domain
        // for the waiting threads
        //

        psdShared->SetWTLHeadShrPtr(NULL);
        psdShared->SetWTLTailShrPtr(NULL);

        if (0 < ulcWaitingThreads)
        {
            WaitingThreadsListNode *pwtlnOld;
            WaitingThreadsListNode *pwtlnNew;
            int i = 0;

            for (pwtlnOld = psdLocal->GetWTLHeadPtr();
                 pwtlnOld != NULL;
                 pwtlnOld = pwtlnOld->ptrNext.ptr, i += 1)
            {
                pwtlnNew = SharedIDToTypePointer(
                    WaitingThreadsListNode,
                    rgshridWTLNodes[i]
                    );

                _ASSERTE(NULL != pwtlnNew);

                pwtlnNew->shridSHRThis = rgshridWTLNodes[i];
                pwtlnNew->ptrOwnerObjSynchData.shrid = shridSynchData;

                pwtlnNew->dwThreadId = pwtlnOld->dwThreadId;
                pwtlnNew->dwProcessId = pwtlnOld->dwProcessId;
                pwtlnNew->dwObjIndex = pwtlnOld->dwObjIndex;
                pwtlnNew->dwFlags = pwtlnOld->dwFlags | WTLN_FLAG_OWNER_OBJECT_IS_SHARED;
                pwtlnNew->shridWaitingState = pwtlnOld->shridWaitingState;
                pwtlnNew->ptwiWaitInfo = pwtlnOld->ptwiWaitInfo;

                psdShared->SharedWaiterEnqueue(rgshridWTLNodes[i], false);
                psdShared->AddRef();

                _ASSERTE(pwtlnOld = pwtlnOld->ptwiWaitInfo->rgpWTLNodes[pwtlnOld->dwObjIndex]);
                pwtlnNew->ptwiWaitInfo->rgpWTLNodes[pwtlnNew->dwObjIndex] = pwtlnNew;

                pwtlnNew->ptwiWaitInfo->lSharedObjCount += 1;
                if (pwtlnNew->ptwiWaitInfo->lSharedObjCount
                    == pwtlnNew->ptwiWaitInfo->lObjCount)
                {
                    pwtlnNew->ptwiWaitInfo->wdWaitDomain = SharedWait;
                }
                else
                {
                    _ASSERTE(pwtlnNew->ptwiWaitInfo->lSharedObjCount
                        < pwtlnNew->ptwiWaitInfo->lObjCount);

                    pwtlnNew->ptwiWaitInfo->wdWaitDomain = MixedWait;
                }
            }

            _ASSERTE(psdShared->GetWaitingThreadCount() == ulcWaitingThreads);
        }

        //
        // If the object tracks ownership and has a current owner update
        // the OwnedObjectsListNode to point to the shared memory synch
        // data
        //

        if (CObjectType::OwnershipTracked == pot->GetOwnershipSemantics())
        {
            OwnedObjectsListNode *pooln;

            pooln = psdLocal->GetOwnershipListNode();
            if (NULL != pooln)
            {
                pooln->pPalObjSynchData = psdShared;
                psdShared->SetOwnershipListNode(pooln);
                psdShared->AddRef();

                //
                // Copy over other ownership info.
                //

                psdShared->SetOwner(psdLocal->GetOwnerThread());
                psdShared->SetOwnershipCount(psdLocal->GetOwnershipCount());
                _ASSERTE(!psdShared->IsAbandoned());
            }
            else
            {
                _ASSERTE(0 == psdLocal->GetOwnershipCount());
                _ASSERTE(0 == psdShared->GetOwnershipCount());
                psdShared->SetAbandoned(psdLocal->IsAbandoned());
            }
        }

        //
        // If the synch data is for a process object update the monitored
        // process list nodes to point to the shared memory object data,
        // and release the monitored process list lock
        //

        if (otiProcess == pot->GetId())
        {
            MonitoredProcessesListNode *pmpn;

            pmpn = m_pmplnMonitoredProcesses;
            while (NULL != pmpn)
            {
                if (psdLocal == pmpn->psdSynchData)
                {
                    pmpn->psdSynchData = psdShared;
                    psdShared->AddRef();
                }

                pmpn = pmpn->pNext;
            }

            pmpn = m_pmplnExitedNodes;
            while (NULL != pmpn)
            {
                if (psdLocal == pmpn->psdSynchData)
                {
                    pmpn->psdSynchData = psdShared;
                    psdShared->AddRef();
                }

                pmpn = pmpn->pNext;
            }

            InternalLeaveCriticalSection(pthrCurrent, &s_csMonitoredProcessesLock);
        }

        *ppvSharedSynchData = reinterpret_cast<VOID*>(shridSynchData);

        //
        // Free the local memory items to caches
        //

        if (0 < ulcWaitingThreads)
        {
            WaitingThreadsListNode *pwtln;

            pwtln = psdLocal->GetWTLHeadPtr();
            while (NULL != pwtln)
            {
                WaitingThreadsListNode *pwtlnTemp;

                pwtlnTemp = pwtln;
                pwtln = pwtln->ptrNext.ptr;
                m_cacheWTListNodes.Add(pthrCurrent, pwtlnTemp);
            }
        }

        m_cacheSynchData.Add(pthrCurrent, psdLocal);

    POSD_exit:

        if (NULL != rgshridWTLNodes)
        {
            InternalDeleteArray(rgshridWTLNodes);
        }

        return palError;
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
            m_lLocalSynchLockCount(0),
            m_lSharedSynchLockCount(0),
            m_ownedNamedMutexListHead(nullptr)
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

#if HAVE_CLOCK_MONOTONIC && HAVE_PTHREAD_CONDATTR_SETCLOCK
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
#endif // HAVE_CLOCK_MONOTONIC && HAVE_PTHREAD_CONDATTR_SETCLOCK

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


    /*++
    Method:
      CThreadSynchronizationInfo::AddObjectToOwnedList

    Adds an object to the list of currently owned objects.
    --*/
    void CThreadSynchronizationInfo::AddObjectToOwnedList(POwnedObjectsListNode pooln)
    {
        InsertTailList(&m_leOwnedObjsList, &pooln->Link);
    }

    /*++
    Method:
      CThreadSynchronizationInfo::RemoveObjectFromOwnedList

    Removes an object from the list of currently owned objects.
    --*/
    void CThreadSynchronizationInfo::RemoveObjectFromOwnedList(POwnedObjectsListNode pooln)
    {
        RemoveEntryList(&pooln->Link);
    }

    /*++
    Method:
      CThreadSynchronizationInfo::RemoveFirstObjectFromOwnedList

    Removes the first object from the list of currently owned objects.
    --*/
    POwnedObjectsListNode CThreadSynchronizationInfo::RemoveFirstObjectFromOwnedList()
    {
        OwnedObjectsListNode * poolnItem;

        if (IsListEmpty(&m_leOwnedObjsList))
        {
            poolnItem = NULL;
        }
        else
        {
            PLIST_ENTRY pLink = RemoveHeadList(&m_leOwnedObjsList);
            poolnItem = CONTAINING_RECORD(pLink, OwnedObjectsListNode, Link);
        }

        return poolnItem;
    }

    void CThreadSynchronizationInfo::AddOwnedNamedMutex(NamedMutexProcessData *processData)
    {
        _ASSERTE(this == &GetCurrentPalThread()->synchronizationInfo);
        _ASSERTE(processData != nullptr);
        _ASSERTE(processData->IsLockOwnedByCurrentThread());
        _ASSERTE(processData->GetNextInThreadOwnedNamedMutexList() == nullptr);

        processData->SetNextInThreadOwnedNamedMutexList(m_ownedNamedMutexListHead);
        m_ownedNamedMutexListHead = processData;
    }

    void CThreadSynchronizationInfo::RemoveOwnedNamedMutex(NamedMutexProcessData *processData)
    {
        _ASSERTE(this == &GetCurrentPalThread()->synchronizationInfo);
        _ASSERTE(processData != nullptr);
        _ASSERTE(processData->IsLockOwnedByCurrentThread());

        if (m_ownedNamedMutexListHead == processData)
        {
            m_ownedNamedMutexListHead = processData->GetNextInThreadOwnedNamedMutexList();
            processData->SetNextInThreadOwnedNamedMutexList(nullptr);
        }
        else
        {
            bool found = false;
            for (NamedMutexProcessData
                    *previous = m_ownedNamedMutexListHead,
                    *current = previous->GetNextInThreadOwnedNamedMutexList();
                current != nullptr;
                previous = current, current = current->GetNextInThreadOwnedNamedMutexList())
            {
                if (current == processData)
                {
                    found = true;
                    previous->SetNextInThreadOwnedNamedMutexList(current->GetNextInThreadOwnedNamedMutexList());
                    current->SetNextInThreadOwnedNamedMutexList(nullptr);
                    break;
                }
            }
            _ASSERTE(found);
        }
    }

    NamedMutexProcessData *CThreadSynchronizationInfo::RemoveFirstOwnedNamedMutex()
    {
        _ASSERTE(this == &GetCurrentPalThread()->synchronizationInfo);

        NamedMutexProcessData *processData = m_ownedNamedMutexListHead;
        if (processData != nullptr)
        {
            _ASSERTE(processData->IsLockOwnedByCurrentThread());
            m_ownedNamedMutexListHead = processData->GetNextInThreadOwnedNamedMutexList();
            processData->SetNextInThreadOwnedNamedMutexList(nullptr);
        }
        return processData;
    }

    bool CThreadSynchronizationInfo::OwnsNamedMutex(NamedMutexProcessData *processData)
    {
        _ASSERTE(this == &GetCurrentPalThread()->synchronizationInfo);

        for (NamedMutexProcessData *current = m_ownedNamedMutexListHead;
            current != nullptr;
            current = current->GetNextInThreadOwnedNamedMutexList())
        {
            _ASSERTE(current->IsLockOwnedByCurrentThread());
            if (current == processData)
            {
                return true;
            }
        }

        return false;
    }

    bool CThreadSynchronizationInfo::OwnsAnyNamedMutex() const
    {
        _ASSERTE(this == &GetCurrentPalThread()->synchronizationInfo);
        return m_ownedNamedMutexListHead != nullptr;
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
                    InternalDelete(pdsln);

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
      CPalSynchronizationManager::HasProcessExited

    Tests whether or not a process has exited
    --*/
    bool CPalSynchronizationManager::HasProcessExited(
        DWORD dwPid,
        DWORD * pdwExitCode,
        bool * pfIsActualExitCode)
    {
        pid_t pidWaitRetval;
        int iStatus;
        bool fRet = false;

        TRACE("Looking for status of process; trying wait()\n");

        while(1)
        {
            /* try to get state of process, using non-blocking call */
            pidWaitRetval = waitpid(dwPid, &iStatus, WNOHANG);

            if ((DWORD)pidWaitRetval == dwPid)
            {
                /* success; get the exit code */
                if (WIFEXITED(iStatus))
                {
                    *pdwExitCode = WEXITSTATUS(iStatus);
                    *pfIsActualExitCode = true;
                    TRACE("Exit code was %d\n", *pdwExitCode);
                }
                else
                {
                    WARN("Process terminated without exiting; can't get exit "
                         "code. Assuming EXIT_FAILURE.\n");
                    *pfIsActualExitCode = true;
                    *pdwExitCode = EXIT_FAILURE;
                }

                fRet = true;
            }
            else if (0 == pidWaitRetval)
            {
                // The process is still running.
                TRACE("Process %#x is still active.\n", dwPid);
            }
            else
            {
                // A legitimate cause of failure is EINTR; if this happens we
                // have to try again. A second legitimate cause is ECHILD, which
                // happens if we're trying to retrieve the status of a currently-
                // running process that isn't a child of this process.
                if(EINTR == errno)
                {
                    TRACE("waitpid() failed with EINTR; re-waiting\n");
                    continue;
                }
                else if (ECHILD == errno)
                {
                    TRACE("waitpid() failed with ECHILD; calling kill instead\n");
                    if (kill(dwPid, 0) != 0)
                    {
                        if (ESRCH == errno)
                        {
                            WARN("kill() failed with ESRCH, i.e. target "
                                 "process exited and it wasn't a child, "
                                 "so can't get the exit code, assuming  "
                                 "it was 0.\n");
                            *pfIsActualExitCode = false;
                            *pdwExitCode = 0;
                        }
                        else
                        {
                            ERROR("kill(pid, 0) failed; errno is %d (%s)\n",
                                  errno, strerror(errno));
                            *pfIsActualExitCode = false;
                            *pdwExitCode = EXIT_FAILURE;
                        }

                        fRet = true;
                    }
                }
                else
                {
                    // Ignoring unexpected waitpid errno and assuming that
                    // the process is still running
                    ERROR("waitpid(pid=%u) failed with errno=%d (%s)\n",
                          dwPid, errno, strerror(errno));
                }
            }

            // Break out of the loop in all cases except EINTR.
            break;
        }

        return fRet;
    }

    /*++
    Method:
      CPalSynchronizationManager::InterlockedAwaken

    Tries to change the target wait status to 'active' in an interlocked fashion
    --*/
    bool CPalSynchronizationManager::InterlockedAwaken(
        DWORD *pWaitState,
        bool fAlertOnly)
    {
        DWORD dwPrevState;

        dwPrevState = InterlockedCompareExchange((LONG *)pWaitState, TWS_ACTIVE, TWS_ALERTABLE);
        if (TWS_ALERTABLE != dwPrevState)
        {
            if (fAlertOnly)
            {
                return false;
            }

            dwPrevState = InterlockedCompareExchange((LONG *)pWaitState, TWS_ACTIVE, TWS_WAITING);
            if (TWS_WAITING == dwPrevState)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    /*++
    Method:
      CPalSynchronizationManager::GetAbsoluteTimeout

    Converts a relative timeout to an absolute one.
    --*/
    PAL_ERROR CPalSynchronizationManager::GetAbsoluteTimeout(DWORD dwTimeout, struct timespec * ptsAbsTmo, BOOL fPreferMonotonicClock)
    {
        PAL_ERROR palErr = NO_ERROR;
        int iRet;

#if HAVE_CLOCK_MONOTONIC && HAVE_PTHREAD_CONDATTR_SETCLOCK
        if (fPreferMonotonicClock)
        {
            iRet = clock_gettime(CLOCK_MONOTONIC, ptsAbsTmo);
        }
        else
        {
#endif
#if HAVE_WORKING_CLOCK_GETTIME
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
#if HAVE_CLOCK_MONOTONIC && HAVE_PTHREAD_CONDATTR_SETCLOCK
        }
#endif
        if (0 == iRet)
        {
            ptsAbsTmo->tv_sec  += dwTimeout / tccSecondsToMillieSeconds;
            ptsAbsTmo->tv_nsec += (dwTimeout % tccSecondsToMillieSeconds) * tccMillieSecondsToNanoSeconds;
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
