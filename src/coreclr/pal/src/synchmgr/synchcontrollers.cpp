// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    synchcontrollers.cpp

Abstract:
    Implementation of Synchronization Controllers and related objects



--*/

#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(SYNC); // some headers have code with asserts, so do this first

#include "synchmanager.hpp"

#include <sys/types.h>
#include <sys/time.h>
#include <sys/stat.h>
#include <unistd.h>
#include <sched.h>
#include <errno.h>
#include <limits.h>

namespace CorUnix
{
#ifdef SYNCH_STATISTICS
    LONG g_rglStatWaitCount[ObjectTypeIdCount]       = { 0 };
    LONG g_rglStatContentionCount[ObjectTypeIdCount] = { 0 };
#endif // SYNCH_STATISTICS
    ////////////////////////////
    //                        //
    //  CSynchControllerBase  //
    //                        //
    ////////////////////////////

    /*++
    Method:
      CSynchControllerBase::Init

    Initializes a generic controller
    --*/
    PAL_ERROR CSynchControllerBase::Init(
        CPalThread * pthrCurrent,
        ControllerType ctCtrlrType,
        CObjectType *potObjectType,
        CSynchData * psdSynchData)
    {
        VALIDATEOBJECT(psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == pthrCurrent);

        // Initialize internal controller data
        m_pthrOwner      = pthrCurrent;
        m_ctCtrlrType    = ctCtrlrType;
        m_potObjectType  = potObjectType;
        m_psdSynchData   = psdSynchData;

        // Add reference to target synch data
        m_psdSynchData->AddRef();

        // Acquire lock implied by the controller
        CPalSynchronizationManager::AcquireLocalSynchLock(m_pthrOwner);

        return NO_ERROR;
    }

    /*++
    Method:
      CSynchControllerBase::Release

    Releases a generic controller a return it to the appropriate cache
    --*/
    void CSynchControllerBase::Release()
    {
        VALIDATEOBJECT(m_psdSynchData);

#ifdef _DEBUG
        ThreadWaitInfo * ptwiWaitInfo =
            CPalSynchronizationManager::GetThreadWaitInfo(m_pthrOwner);
#endif // _DEBUG

        CPalSynchronizationManager * pSynchManager =
            CPalSynchronizationManager::GetInstance();

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(ptwiWaitInfo->pthrOwner == m_pthrOwner);

        // Release reference to target synch data
        m_psdSynchData->Release(m_pthrOwner);

        // Release lock implied by the controller
        CPalSynchronizationManager::ReleaseLocalSynchLock(m_pthrOwner);

        // Return controller to the appropriate cache
        if (WaitController == m_ctCtrlrType)
        {
            // The cast here must be static_cast and not reinterpet_cast.
            // In fact in general static_cast<CSynchWaitController*>(this) is
            // equal to this-sizeof(void*), given that CSynchWaitController
            // has a virtual table, while CSynchControllerBase doesn't.
            pSynchManager->CacheAddWaitCtrlr(m_pthrOwner,
                static_cast<CSynchWaitController*>(this));
        }
        else
        {
            // The cast here must be static_cast and not reinterpet_cast
            pSynchManager->CacheAddStateCtrlr(m_pthrOwner,
                static_cast<CSynchStateController*>(this));
        }
    }

    ////////////////////////////
    //                        //
    //  CSynchWaitController  //
    //                        //
    ////////////////////////////

    /*++
    Method:
      CSynchWaitController::CanThreadWaitWithoutBlocking

    Returns whether or not the thread owning this controller can
    wait on the target object without blocking (i.e. the objet is
    signaled)
    --*/
    PAL_ERROR CSynchWaitController::CanThreadWaitWithoutBlocking(
        bool * pfCanWaitWithoutBlocking,
        bool * pfAbandoned)
    {
        VALIDATEOBJECT(m_psdSynchData);

        bool fRetVal = false;

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(NULL != pfCanWaitWithoutBlocking);
        _ASSERTE(NULL != pfAbandoned);

        fRetVal = m_psdSynchData->CanWaiterWaitWithoutBlocking(m_pthrOwner, pfAbandoned);

        if(!fRetVal && otiProcess == m_psdSynchData->GetObjectTypeId())
        {
            // Note: if the target object is a process, here we need to check
            // whether or not it has already exited. In fact, since currently
            // we do not monitor a process status as long as there is no
            // thread waiting on it, in general if the process already exited
            // the process object is likely not to be signaled yet, therefore
            // the above CanWaiterWaitWithoutBlocking call probably returned
            // false, and, without the check below, that would cause the
            // current thread to eventually go to sleep for a short time
            // (until the worker thread notifies that the waited process has
            // indeed exited), while it would not be necessary.
            // As side effect that would cause a WaitForSingleObject with zero
            // timeout to always return WAIT_TIMEOUT, even though the target
            // process already exited. WaitForSingleObject with zero timeout
            // is a common way to probe whether or not a process has already
            // exited, and it is supposed to return WAIT_OBJECT_0 if the
            // process exited, and WAIT_TIMEOUT if it is still active.
            // In order to support this feature we need to check at this time
            // whether or not the process has already exited.

            CProcProcessLocalData * pProcLocalData = GetProcessLocalData();
            DWORD dwExitCode = 0;
            bool fIsActualExitCode = false;

            _ASSERT_MSG(NULL != pProcLocalData,
                        "Process synch data pointer is missing\n");

            if (NULL != pProcLocalData &&
                CPalSynchronizationManager::HasProcessExited(pProcLocalData->dwProcessId,
                                                             &dwExitCode,
                                                             &fIsActualExitCode))
            {
                TRACE("Process pid=%u exited with %s exitcode=%u\n",
                      pProcLocalData->dwProcessId,
                      fIsActualExitCode ? "actual" : "guessed",
                      dwExitCode);

                // Store the exit code in the process local data
                if (fIsActualExitCode)
                {
                    pProcLocalData->dwExitCode = dwExitCode;
                }

                // Set process status to PS_DONE
                pProcLocalData->ps = PS_DONE;

                // Set signal count
                m_psdSynchData->SetSignalCount(1);

                // Releasing all local waiters
                // (see comments in DoMonitorProcesses)
                m_psdSynchData->ReleaseAllLocalWaiters(m_pthrOwner);

                fRetVal = true;
            }
        }

        *pfCanWaitWithoutBlocking = fRetVal;
        return NO_ERROR;
    }

    /*++
    Method:
      CSynchWaitController::ReleaseWaitingThreadWithoutBlocking

    Performs all the steps needed to be done by the controller's owner
    thread in order to wait on the target object without blocking
    (e.g. modifying the object signal count accordingly with its
    thread release semantics)
    This method should be called only after having received positive
    response from CanThreadWaitWithoutBlocking called on the same
    controller.
    --*/
    PAL_ERROR CSynchWaitController::ReleaseWaitingThreadWithoutBlocking()
    {
        VALIDATEOBJECT(m_psdSynchData);

        PAL_ERROR palErr = NO_ERROR;

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);

        palErr = m_psdSynchData->ReleaseWaiterWithoutBlocking(m_pthrOwner, m_pthrOwner);

#ifdef SYNCH_STATISTICS
        if (NO_ERROR == palErr)
        {
            m_psdSynchData->IncrementStatWaitCount();
        }
#endif
        return palErr;
    }

    /*++
    Method:
      CSynchWaitController::RegisterWaitingThread

    Registers the controller's owner thread for waiting on the target
    object
    --*/
    PAL_ERROR CSynchWaitController::RegisterWaitingThread(
        WaitType wtWaitType,
        DWORD dwIndex,
        bool fAlertable,
        bool fPrioritize)
    {
        VALIDATEOBJECT(m_psdSynchData);

        PAL_ERROR palErr = NO_ERROR;
        WaitingThreadsListNode * pwtlnNewNode = NULL;
        SharedID shridNewNode = NULL;
        ThreadWaitInfo * ptwiWaitInfo;
        DWORD * pdwWaitState;
        bool fEarlyDeath = false;
        bool fSynchDataRefd = false;
        CPalSynchronizationManager * pSynchManager =
            CPalSynchronizationManager::GetInstance();

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);

        ptwiWaitInfo = CPalSynchronizationManager::GetThreadWaitInfo(
            m_pthrOwner);

        _ASSERTE(ptwiWaitInfo->pthrOwner == m_pthrOwner);

        pdwWaitState = SharedIDToTypePointer(DWORD,
                m_pthrOwner->synchronizationInfo.m_shridWaitAwakened);

        pwtlnNewNode = pSynchManager->CacheGetLocalWTListNode(m_pthrOwner);

        if (!pwtlnNewNode)
        {
            ERROR("Out of memory\n");
            palErr = ERROR_NOT_ENOUGH_MEMORY;
            goto RWT_exit;
        }

        if (ptwiWaitInfo->lObjCount >= MAXIMUM_WAIT_OBJECTS)
        {
            ASSERT("Too many objects");
            palErr = ERROR_INTERNAL_ERROR;
            goto RWT_exit;
        }

        if (0 == ptwiWaitInfo->lObjCount)
        {
            ptwiWaitInfo->wtWaitType = wtWaitType;
        }

        pwtlnNewNode->ptwiWaitInfo       = ptwiWaitInfo;
        pwtlnNewNode->dwObjIndex         = dwIndex;
        pwtlnNewNode->dwThreadId         = m_pthrOwner->GetThreadId();
        pwtlnNewNode->dwFlags            = (MultipleObjectsWaitAll == wtWaitType) ?
                                            WTLN_FLAG_WAIT_ALL : 0;
        pwtlnNewNode->shridWaitingState  = m_pthrOwner->synchronizationInfo.m_shridWaitAwakened;
        pwtlnNewNode->ptrOwnerObjSynchData.ptr = m_psdSynchData;

        // AddRef the synch data (will be released in UnregisterWait)
        m_psdSynchData->AddRef();
        fSynchDataRefd = true;

        ptwiWaitInfo->rgpWTLNodes[ptwiWaitInfo->lObjCount] = pwtlnNewNode;

        if(otiProcess == m_psdSynchData->GetObjectTypeId())
        {
            CProcProcessLocalData * pProcLocalData = GetProcessLocalData();

            if (NULL == pProcLocalData)
            {
                // Process local data pointer not set in the controller.
                // This pointer is set in CSynchWaitController only when the
                // wait controller for the object is created by calling
                // GetSynchWaitControllersForObjects
                ASSERT("Process synch data pointer is missing\n");
                palErr = ERROR_INTERNAL_ERROR;
                goto RWT_exit;
            }

            palErr = pSynchManager->RegisterProcessForMonitoring(m_pthrOwner,
                                                                 m_psdSynchData,
                                                                 m_pProcessObject,
                                                                 pProcLocalData);
            if (NO_ERROR != palErr)
            {
                goto RWT_exit;
            }
        }

        if (0 == ptwiWaitInfo->lObjCount)
        {
            DWORD dwWaitState;

            // Setting the thread in wait state
            dwWaitState = (DWORD)(fAlertable ? TWS_ALERTABLE: TWS_WAITING);

            TRACE("Switching my wait state [%p] from TWS_ACTIVE to %u \n",
                  pdwWaitState, dwWaitState);

            dwWaitState = InterlockedCompareExchange(
                (LONG *)pdwWaitState, (LONG)dwWaitState, TWS_ACTIVE);
            if ((DWORD)TWS_ACTIVE != dwWaitState)
            {
                if ((DWORD)TWS_EARLYDEATH == dwWaitState)
                {
                    // Process is terminating, this thread will soon be
                    // suspended (by SuspendOtherThreads).
                    WARN("Thread is about to get suspended by "
                         "TerminateProcess\n");

                    fEarlyDeath = true;
                    palErr = WAIT_FAILED;
                }
                else
                {
                    ASSERT("Unexpected thread wait state %d\n", dwWaitState);
                    palErr = ERROR_INTERNAL_ERROR;
                }
                goto RWT_exit;
            }
        }

        // Add new node to queue
        m_psdSynchData->WaiterEnqueue(pwtlnNewNode, fPrioritize);

        // Succeeded: update object count
        ptwiWaitInfo->lObjCount++;

    RWT_exit:
        if (palErr != NO_ERROR)
        {
            // Unregister any partial wait registration
            pSynchManager->UnRegisterWait(m_pthrOwner, ptwiWaitInfo);

            if (fSynchDataRefd)
            {
                m_psdSynchData->Release(m_pthrOwner);
            }
            if (NULL != pwtlnNewNode)
            {
                pSynchManager->CacheAddLocalWTListNode(m_pthrOwner, pwtlnNewNode);
            }

            if (fEarlyDeath)
            {
                // Early death detected, i.e. the process is about to exit.
                // We need to completely release the synch lock(s) before
                // going to sleep
                LONG lLocalSynchLockCount;

                lLocalSynchLockCount = CPalSynchronizationManager::ResetLocalSynchLock(m_pthrOwner);

                _ASSERTE(0 < lLocalSynchLockCount);

                // Sleep for ever
                CPalSynchronizationManager::ThreadPrepareForShutdown();
            }
        }
#ifdef SYNCH_STATISTICS
        else
        {
            m_psdSynchData->IncrementStatWaitCount();
            m_psdSynchData->IncrementStatContentionCount();
        }
#endif
        return palErr;
    }

    /*++
    Method:
      CSynchWaitController::ReleaseController

    Releases the current controller
    --*/
    void CSynchWaitController::ReleaseController()
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);

        Release();
    }

    /*++
    Method:
      CSynchWaitController::GetProcessLocalData

    Accessor Get method for process local data of the target object
    --*/
    CProcProcessLocalData * CSynchWaitController::GetProcessLocalData()
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERT_MSG(NULL != m_pProcLocalData,
                    "Pointer to process local data not yet initialized\n");

        return m_pProcLocalData;
    }

    /*++
    Method:
      CSynchWaitController::SetProcessData

    Accessor Set method for process local data of the target object
    --*/
    void CSynchWaitController::SetProcessData(IPalObject* pProcessObject, CProcProcessLocalData * pProcLocalData)
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERT_MSG(m_pProcessObject == nullptr, "SetProcessData should not be called more than once");
        _ASSERT_MSG(pProcessObject != nullptr && pProcessObject->GetObjectType()->GetId() == otiProcess, "Invalid process object passed to SetProcessData");

        m_pProcessObject = pProcessObject;
        m_pProcLocalData = pProcLocalData;
    }

    /////////////////////////////
    //                         //
    //  CSynchStateController  //
    //                         //
    /////////////////////////////

    /*++
    Method:
      CSynchStateController::GetSignalCount

    Returns the current signal count of the target object
    --*/
    PAL_ERROR CSynchStateController::GetSignalCount(LONG *plSignalCount)
    {
        VALIDATEOBJECT(m_psdSynchData);

        PAL_ERROR palErr = NO_ERROR;
        LONG lCount = m_psdSynchData->GetSignalCount();

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(NULL != plSignalCount);
        _ASSERT_MSG(0 <= lCount,
                    "Internal error: negative signal count [signal count=%d]",
                    lCount);

        *plSignalCount = lCount;
        return palErr;
    }

    /*++
    Method:
      CSynchStateController::SetSignalCount

    Sets the signal count of the target object, possibly triggering
    waiting threads awakening.
    --*/
    PAL_ERROR CSynchStateController::SetSignalCount(LONG lNewCount)
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(lNewCount >= 0);

        m_psdSynchData->Signal(m_pthrOwner, lNewCount);

        return NO_ERROR;
    }

    /*++
    Method:
      CSynchStateController::IncrementSignalCount

    Increments the signal count of the target object, possibly triggering
    waiting threads awakening.
    --*/
    PAL_ERROR CSynchStateController::IncrementSignalCount(
        LONG lAmountToIncrement)
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(lAmountToIncrement > 0);

        LONG lOldCount = m_psdSynchData->GetSignalCount();
        LONG lNewCount = lOldCount + lAmountToIncrement;

        _ASSERT_MSG(lNewCount > lOldCount,
            "Signal count increment %d would make current signal count %d to "
            "wrap around\n", lAmountToIncrement, lOldCount);

        m_psdSynchData->Signal(m_pthrOwner, lNewCount);

        return NO_ERROR;
    }

    /*++
    Method:
      CSynchStateController::DecrementSignalCount

    Decrements the signal count of the target object.
    --*/
    PAL_ERROR CSynchStateController::DecrementSignalCount(
        LONG lAmountToDecrement)
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(lAmountToDecrement > 0);

        PAL_ERROR palErr = NO_ERROR;
        LONG lCount = m_psdSynchData->GetSignalCount();
        _ASSERTE(lAmountToDecrement <= lCount);

        m_psdSynchData->SetSignalCount(lCount - lAmountToDecrement);

        return palErr;
    }

    /*++
    Method:
      CSynchStateController::SetOwner

    Sets the owner of the target object and initializes the ownership
    count to 1 (for objects with tracked ownership).
    --*/
    PAL_ERROR CSynchStateController::SetOwner(CPalThread * pNewOwningThread)
    {
        VALIDATEOBJECT(m_psdSynchData);

        PAL_ERROR palErr = NO_ERROR;

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERTE(NULL != pNewOwningThread);
        _ASSERT_MSG(CObjectType::OwnershipTracked ==
                    m_potObjectType->GetOwnershipSemantics(),
                    "SetOwner called on an object without OwnershipTracked "
                    "semantics\n");

        if (0 != m_psdSynchData->GetOwnershipCount())
        {
            ASSERT("Ownership count should be zero at this time\n");
            palErr = ERROR_INTERNAL_ERROR;
            goto SO_exit;
        }

        palErr = m_psdSynchData->AssignOwnershipToThread(m_pthrOwner,
                                                       pNewOwningThread);

        _ASSERT_MSG(0 == m_psdSynchData->GetOwnershipCount() ||
                    0 == m_psdSynchData->GetSignalCount(),
                    "Conflicting values for SignalCount [%d] and "
                    "OwnershipCount [%d]\n",
                    m_psdSynchData->GetOwnershipCount(),
                    m_psdSynchData->GetSignalCount());

    SO_exit:
        return palErr;
    }

    /*++
    Method:
      CSynchStateController::DecrementOwnershipCount

    Decrements the ownership count of the target object possibly triggering
    waiting threads awakening (for objects with tracked ownership).
    --*/
    PAL_ERROR CSynchStateController::DecrementOwnershipCount()
    {
        VALIDATEOBJECT(m_psdSynchData);

        PAL_ERROR palErr = NO_ERROR;
        LONG lOwnershipCount = m_psdSynchData->GetOwnershipCount();

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);
        _ASSERT_MSG(CObjectType::OwnershipTracked ==
                    m_potObjectType->GetOwnershipSemantics(),
                    "Trying to decrement ownership count on an object with "
                    "ownership semantics other than OwnershipTracked\n");
        _ASSERT_MSG(0 <= lOwnershipCount,
                    "Operation would make ownership count negative - object "
                    "should be owned at this time [ownership count=%d]\n",
                    lOwnershipCount);

        if ( (1 > lOwnershipCount) ||
             (m_psdSynchData->GetOwnerProcessID() != gPID) ||
             (m_psdSynchData->GetOwnerThread() != m_pthrOwner) )
        {
            palErr = ERROR_NOT_OWNER;
            goto DOC_exit;
        }

        lOwnershipCount--;
        m_psdSynchData->SetOwnershipCount(lOwnershipCount);

        if (0 == lOwnershipCount)
        {
            CPalSynchronizationManager * pSynchManager =
                CPalSynchronizationManager::GetInstance();
            OwnedObjectsListNode * pooln =
                m_psdSynchData->GetOwnershipListNode();

            _ASSERT_MSG(NULL != pooln,
                        "Null ownership node pointer in SynchData with ownership "
                        "semantics\n");
            _ASSERT_MSG(m_psdSynchData == pooln->pPalObjSynchData,
                        "Corrupted ownership node\n");

            // Object has been released
            // Remove it from list of owned objs for current thread
            m_pthrOwner->synchronizationInfo.RemoveObjectFromOwnedList(pooln);

            // Release SynchData reference count implied by the ownership
            // list node
            m_psdSynchData->Release(m_pthrOwner);

            // Return node to the cache
            pSynchManager->CacheAddOwnedObjsListNode(m_pthrOwner, pooln);

            // Reset ownership
            m_psdSynchData->ResetOwnership();

            // Signal it and trigger waiter thread awakening
            m_psdSynchData->Signal(m_pthrOwner, 1);
        }

    DOC_exit:
        return palErr;
    }

    /*++
    Method:
      CSynchStateController::ReleaseController

    Releases the controller.
    --*/
    void CSynchStateController::ReleaseController(void)
    {
        VALIDATEOBJECT(m_psdSynchData);

        _ASSERTE(InternalGetCurrentThread() == m_pthrOwner);

        Release();
    }

    //////////////////
    //              //
    //  CSynchData  //
    //              //
    //////////////////

    /*++
    Method:
      CSynchData::Release

    Decremnt the reference count of the target synchdata and retrurns
    it to the appropriate cache if the reference count reaches zero.
    --*/
    LONG CSynchData::Release(CPalThread * pthrCurrent)
    {
        VALIDATEOBJECT(this);

        LONG lCount = InterlockedDecrement(&m_lRefCount);

        _ASSERT_MSG(0 <= lCount,
                    "CSynchData %p with negative reference count [%d]\n",
                    this, lCount);

        if (0 == lCount)
        {
            CPalSynchronizationManager * pSynchManager =
                CPalSynchronizationManager::GetInstance();

            _ASSERT_MSG((NULL == m_ptrWTLHead.ptr),
                        "Final Release on CSynchData with threads still in "
                        "the waiting list\n");

            TRACE("Disposing %s waitable object with SynchData @ "
                  "p=%p}\n",
                  "local",
                  this);


#ifdef SYNCH_STATISTICS
            LONG lStatWaitCount = GetStatWaitCount();
            LONG lStatContentionCount = GetStatContentionCount();
            LONG lCount, lNewCount;

            TRACE("Statistical data for SynchData of otiType=%u @ %p: WaitCount=%d "
                  "ContentionCount=%d\n", m_otiObjectTypeId, this, lStatWaitCount,
                  lStatContentionCount);

            do {
                lCount = g_rglStatWaitCount[m_otiObjectTypeId];
                lNewCount = lCount + lStatWaitCount;
                lNewCount = InterlockedCompareExchange(&(g_rglStatWaitCount[m_otiObjectTypeId]),
                                                       lNewCount, lCount);
            } while (lCount != lNewCount);

            lStatWaitCount = lNewCount;

            do {
                lCount = g_rglStatContentionCount[m_otiObjectTypeId];
                lNewCount = lCount + lStatContentionCount;
                lNewCount = InterlockedCompareExchange(&(g_rglStatContentionCount[m_otiObjectTypeId]),
                                                       lNewCount, lCount);
            } while (lCount != lNewCount);

            lStatContentionCount = lNewCount;

            TRACE("Total current statistical data for otiType=%u objects: WaitCount=%d "
                  "ContentionCount=%d\n", m_otiObjectTypeId, lStatWaitCount,
                  lStatContentionCount);
#endif // SYNCH_STATISTICS

            pSynchManager->CacheAddLocalSynchData(pthrCurrent, this);
        }

        return lCount;
    }

    /*++
    Method:
      CSynchData::ReleaseWaiterWithoutBlocking

    Performs all the steps needed to be done by the target thread in order
    to wait without blocking on the object associated with the current
    SynchData (e.g. modifying the object signal count accordingly with its
    thread release semantics)

    Note: this method must be called while holding the local process synch lock.
    --*/
    PAL_ERROR CSynchData::ReleaseWaiterWithoutBlocking(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget)
    {
        VALIDATEOBJECT(this);

        PAL_ERROR palErr = NO_ERROR;
        CObjectType * potObjectType = GetObjectType();
#ifdef _DEBUG
        CObjectType::SignalingSemantics ssSignalingSemantics =
            potObjectType->GetSignalingSemantics();
#endif // _DEBUG
        CObjectType::OwnershipSemantics osOwnershipSemantics =
            potObjectType->GetOwnershipSemantics();
        CObjectType::ThreadReleaseSemantics trsThreadReleaseSemantics =
            potObjectType->GetThreadReleaseSemantics();
        bool fReenteringObjWithOwnership = false;

        _ASSERT_MSG(CObjectType::SignalingNotApplicable != ssSignalingSemantics,
                    "Signaling not applicable");
        _ASSERT_MSG(CObjectType::ThreadReleaseNotApplicable !=
                    trsThreadReleaseSemantics,
                    "Thread releasing not applicable");
        _ASSERT_MSG(CObjectType::SingleTransitionObject != ssSignalingSemantics ||
                    (CObjectType::ThreadReleaseHasNoSideEffects ==
                     trsThreadReleaseSemantics &&
                     CObjectType::NoOwner == osOwnershipSemantics),
                    "Conflicting object synchronization attributes "
                    "[SignalingSemantics=%u OwnershipSemantics=%u "
                    "ThreadReleaseSemantics=%u]\n", ssSignalingSemantics,
                    osOwnershipSemantics, trsThreadReleaseSemantics);

        if (CObjectType::OwnershipTracked == osOwnershipSemantics &&
            0 < GetOwnershipCount())
        {
            // We are rentering an object with ownership: we need to skip
            // the object unsignaling
            fReenteringObjWithOwnership = true;
        }

        if (!fReenteringObjWithOwnership &&
            CObjectType::ThreadReleaseAltersSignalCount == trsThreadReleaseSemantics)
        {
            _ASSERT_MSG(0 < GetSignalCount(),
                        "Internal error: operation would make signal count "
                        "negative - object should be signaled at this time "
                        "[signal count=%d]", GetSignalCount());
            _ASSERT_MSG(CObjectType::OwnershipTracked != osOwnershipSemantics ||
                        1 == GetSignalCount(),
                        "Ownable objects cannot have signal count greater "
                        "than zero [current SignalCount=%d]\n",
                        GetSignalCount());

            // Unsignal the object
            DecrementSignalCount();
        }

        if (CObjectType::OwnershipTracked == osOwnershipSemantics)
        {
            _ASSERT_MSG(0 == GetOwnershipCount() || 0 == GetSignalCount(),
                        "OwnershipCount and SignalCount with conflicting "
                        "values\n");

            // Take ownership or increment ownership count.
            // We do this after the object unsignaling to minimize possibilities
            // of having both SignalCount and OwnershipCount greater than zero
            // (see comment in AssignOwnershipToThread)
            palErr = AssignOwnershipToThread(pthrCurrent, pthrTarget);

            if (NO_ERROR != palErr)
            {
                ERROR("AssignOwnershipToThread failed with error %u; "
                      "ownership data on object with SynchData {p=%p} "
                      "may be corrupted\n", palErr, this);
            }
        }

#ifdef SYNCH_STATISTICS
        if (NO_ERROR == palErr)
        {
            IncrementStatWaitCount();
        }
#endif
        return palErr;

    }

    /*++
    Method:
      CSynchData::CanWaiterWaitWithoutBlocking

    Returns whether or not the waiter thread can wait on the target object
    without blocking (i.e. the objet is signaled)

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    bool CSynchData::CanWaiterWaitWithoutBlocking(
        CPalThread * pWaiterThread,
        bool * pfAbandoned)
    {
        VALIDATEOBJECT(this);

        bool fRetVal = (0 < GetSignalCount());
        bool fAbandoned = false;
        bool fOwnershipTracked = (CObjectType::OwnershipTracked ==
                                  GetObjectType()->GetOwnershipSemantics());
        if (fRetVal)
        {
            // Object signaled: thread can wait without blocking
            if (fOwnershipTracked)
            {
                fAbandoned = IsAbandoned();
            }

            goto CWWWB_exit;
        }

        // Object not signaled: thread can wait without blocking only if the
        // object is an ownable one, and it is owned by the current thread
        if (fOwnershipTracked)
        {
            _ASSERT_MSG(0 < GetSignalCount() || 0 < GetOwnershipCount(),
                        "Objects with ownership must be either signaled or "
                        "owned by a thread\n");

            if ((GetOwnerProcessID() == gPID) &&
                (GetOwnerThread() == pWaiterThread) )
            {
                fRetVal = true;
                goto CWWWB_exit;
            }
        }

    CWWWB_exit:
        *pfAbandoned = fAbandoned;
        return fRetVal;
    }

    /*++
    Method:
      CSynchData::Signal

    Sets the signal count of the object owning the target SynchData,
    possibly triggering awakening of waiting threads.

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    void CSynchData::Signal(
        CPalThread * pthrCurrent,
        LONG lSignalCount)
    {
        VALIDATEOBJECT(this);

        bool fThreadReleased = false;
        bool fReleaseAltersSignalCount =
            (CObjectType::ThreadReleaseAltersSignalCount ==
                GetObjectType()->GetThreadReleaseSemantics());

        _ASSERTE(0 <= lSignalCount);

        // Preset the signal count to the new value, so that it can be used
        // by ReleaseFirstWaiter when delegating signaling to another process
        m_lSignalCount = lSignalCount;

        while (m_lSignalCount > 0)
        {
            fThreadReleased = ReleaseFirstWaiter(pthrCurrent);
            if (!fThreadReleased)
            {
                // No more threads to release: break out of the loop
                // keeping the current signal count
                break;
            }
            if (fReleaseAltersSignalCount)
            {
                // Adjust signal count
                m_lSignalCount--;
            }
        }

        _ASSERT_MSG(CObjectType::OwnershipTracked !=
                    GetObjectType()->GetOwnershipSemantics() ||
                    0 == GetOwnershipCount() || 0 == GetSignalCount(),
                    "Conflicting values for SignalCount [%d] and "
                    "OwnershipCount [%d]\n",
                    GetOwnershipCount(), GetSignalCount());

        _ASSERT_MSG(otiMutex != m_otiObjectTypeId || m_lSignalCount <= 1,
                    "Mutex with invalid singal count\n");

        return;
    }

    /*++
    Method:
      CSynchData::ReleaseFirstWaiter

    Releases the first thread from the front of the list of waiting threads
    whose wait is fully satisfied, possibly triggering remote awakening (if
    the target thread lives in a different process) or object signaling
    delegation (if the target thread lives in a different processing and it
    is blocked on a wait-all).

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    bool CSynchData::ReleaseFirstWaiter(
        CPalThread * pthrCurrent)
    {
        PAL_ERROR palErr = NO_ERROR;
        bool fThreadAwakened = false;
        DWORD * pdwWaitState;
        DWORD dwObjIdx;
        SharedID shridItem = NULL, shridNextItem = NULL;
        WaitingThreadsListNode * pwtlnItem, * pwtlnNextItem;
        CPalSynchronizationManager * pSynchManager =
            CPalSynchronizationManager::GetInstance();

        VALIDATEOBJECT(this);

        pwtlnItem = GetWTLHeadPtr();

        while (pwtlnItem)
        {
            VALIDATEOBJECT(pwtlnItem);

            WaitCompletionState wcsWaitCompletionState;
            bool fWaitAll = (0 != (WTLN_FLAG_WAIT_ALL & pwtlnItem->dwFlags));
            pdwWaitState = SharedIDToTypePointer(DWORD,
                pwtlnItem->shridWaitingState);

            pwtlnNextItem = pwtlnItem->ptrNext.ptr;

            if (fWaitAll)
            {
                // Wait All: we need to find out whether the wait is satisfied,
                // or it is not.
                wcsWaitCompletionState = IsRestOfWaitAllSatisfied(pwtlnItem);
            }
            else
            {
                // Normal Wait: the wait is satisfied by definition
                wcsWaitCompletionState = WaitIsSatisfied;
            }

            if (WaitIsSatisfied == wcsWaitCompletionState)
            {
                //
                // Target wait is satisfied
                //
                TRACE("Trying to switch wait state [%p] from WAIT/ALERTABLE "
                      "to ACTIVE for thread=%u\n",
                      pdwWaitState, pwtlnItem->dwThreadId);

                if (CPalSynchronizationManager::InterlockedAwaken(pdwWaitState, FALSE))
                {
                    TRACE("Succeeded switching wait state [%p] from WAIT/ALERTABLE "
                          "to TWS_ACTIVE for trhead=%u\n",
                          pdwWaitState, pwtlnItem->dwThreadId);

                    dwObjIdx = pwtlnItem->dwObjIndex;

                    ThreadWaitInfo * ptwiWaitInfo = pwtlnItem->ptwiWaitInfo;
                    bool fAbandoned = false;

                    if (CObjectType::OwnershipTracked ==
                        GetObjectType()->GetOwnershipSemantics())
                    {
                        // Get the abandoned status before resetting it by
                        // assigning ownership to target thread
                        fAbandoned = IsAbandoned();

                        // Assign ownership to target thread
                        // Note: This will cause both ownership count and
                        //       signal count to be greater than zero at the
                        //       same time; the signal count will be anyway
                        //       decremented immediately by the caller
                        //       CsynchData::Signal
                        palErr = AssignOwnershipToThread(pthrCurrent,
                                                            ptwiWaitInfo->pthrOwner);
                        if (NO_ERROR != palErr)
                        {
                            ERROR("Synch Worker: AssignOwnershipToThread "
                                    "failed with error %u; ownership data on "
                                    "object with SynchData %p may be "
                                    "corrupted\n", palErr, this);
                        }
                    }

                    if (fWaitAll)
                    {
                        // Wait all satisfied: unsignal other objects
                        // involved in the wait
                        CPalSynchronizationManager::UnsignalRestOfLocalAwakeningWaitAll(
                            pthrCurrent,
                            ptwiWaitInfo->pthrOwner,
                            pwtlnItem,
                            this);
                    }

                    TRACE("Unregistering wait for thread %u and waking it up "
                            "[pdwWaitState=%p]\n", pwtlnItem->dwThreadId,
                            pdwWaitState);

                    // Unregister the wait
                    pSynchManager->UnRegisterWait(pthrCurrent,
                                                    ptwiWaitInfo);

                    // After UnRegisterWait pwtlnItem is invalid
                    pwtlnItem = NULL;

                    palErr = CPalSynchronizationManager::WakeUpLocalThread(
                        pthrCurrent,
                        ptwiWaitInfo->pthrOwner,
                        fAbandoned ? MutexAbandoned : WaitSucceeded,
                        dwObjIdx);

                    if (NO_ERROR != palErr)
                    {
                        ERROR("Failed to wakeup local thread %#x: "
                                "object signaling may be "
                                "lost\n", ptwiWaitInfo->pthrOwner->GetThreadId());
                    }

                    // A thread has been awakened
                    fThreadAwakened = true;

                    // break out of the while loop
                    break;
                }
            }

            // Go to the next item
            shridItem = shridNextItem;
            pwtlnItem = pwtlnNextItem;
        }

        return fThreadAwakened;
    }

    /*++
    Method:
      CSynchData::Signal

    Releases all the threads waiting on this object and living in the current
    process.

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    LONG CSynchData::ReleaseAllLocalWaiters(
        CPalThread * pthrCurrent)
    {
        PAL_ERROR palErr = NO_ERROR;
        LONG lAwakenedCount = 0;
        DWORD * pdwWaitState;
        DWORD dwObjIdx;
        SharedID shridItem = NULL, shridNextItem = NULL;
        WaitingThreadsListNode * pwtlnItem, * pwtlnNextItem;
        CPalSynchronizationManager * pSynchManager =
            CPalSynchronizationManager::GetInstance();

        VALIDATEOBJECT(this);

        pwtlnItem = GetWTLHeadPtr();

        while (pwtlnItem)
        {
            VALIDATEOBJECT(pwtlnItem);

            bool fWaitAll = (0 != (WTLN_FLAG_WAIT_ALL & pwtlnItem->dwFlags));
            pdwWaitState = SharedIDToTypePointer(DWORD,
                pwtlnItem->shridWaitingState);

            pwtlnNextItem = pwtlnItem->ptrNext.ptr;

            // See note in similar spot in ReleaseFirstWaiter

            if(!fWaitAll || WaitIsSatisfied == IsRestOfWaitAllSatisfied(pwtlnItem))
            {
                //
                // Target wait is satisfied
                //
                TRACE("Trying to switch wait state [%p] from WAIT/ALERTABLE "
                      "to ACTIVE for thread=%u\n",
                      pdwWaitState, pwtlnItem->dwThreadId);

                if (CPalSynchronizationManager::InterlockedAwaken(pdwWaitState, FALSE))
                {
                    TRACE("Succeeded switching wait state [%p] from WAIT/ALERTABLE "
                          "to TWS_ACTIVE for trhead=%u\n",
                          pdwWaitState, pwtlnItem->dwThreadId);

                    dwObjIdx = pwtlnItem->dwObjIndex;

                    ThreadWaitInfo * ptwiWaitInfo = pwtlnItem->ptwiWaitInfo;
                    bool fAbandoned = false;

                    if (CObjectType::OwnershipTracked ==
                        GetObjectType()->GetOwnershipSemantics())
                    {
                        // Get the abandoned status before resetting it by
                        // assigning ownership to target thread
                        fAbandoned = IsAbandoned();

                        // Assign ownership to target thread
                        palErr = AssignOwnershipToThread(pthrCurrent,
                                                         ptwiWaitInfo->pthrOwner);
                        if (NO_ERROR != palErr)
                        {
                            ERROR("Synch Worker: AssignOwnershipToThread "
                                  "failed with error %u; ownership data on "
                                  "object with SynchData %p may be "
                                  "corrupted\n", palErr, this);
                        }
                    }

                    if (fWaitAll)
                    {
                        // Wait all satisfied: unsignal other objects
                        // involved in the wait
                        CPalSynchronizationManager::UnsignalRestOfLocalAwakeningWaitAll(
                                                   pthrCurrent,
                                                   ptwiWaitInfo->pthrOwner,
                                                   pwtlnItem,
                                                   this);
                    }

                    TRACE("Unregistering wait for thread %u and waking it up "
                          "[pdwWaitState=%p]\n", pwtlnItem->dwThreadId,
                          pdwWaitState);

                    // Unregister the wait
                    pSynchManager->UnRegisterWait(pthrCurrent,
                                                  ptwiWaitInfo);

                    // After UnRegisterWait pwtlnItem is invalid
                    pwtlnItem = NULL;

                    palErr = CPalSynchronizationManager::WakeUpLocalThread(
                        pthrCurrent,
                        ptwiWaitInfo->pthrOwner,
                        fAbandoned ? MutexAbandoned : WaitSucceeded,
                        dwObjIdx);

                    if (NO_ERROR != palErr)
                    {
                        ERROR("Failed to wakeup local thread %#x: "
                              "object signaling may be "
                              "lost\n", ptwiWaitInfo->pthrOwner->GetThreadId());
                    }
                    else
                    {
                        // A thread has been awakened
                        lAwakenedCount++;
                    }
                }
            }

            // Go to the next item
            shridItem = shridNextItem;
            pwtlnItem = pwtlnNextItem;
        }

        return lAwakenedCount;
    }

    /*++
    Method:
      CSynchData::IsRestOfWaitAllSatisfied

    Returns whether or not the current wait-all operation is fully satisfied,
    assuming the current target object as signaled (i.e. whether or not all the
    involved object, except the current one, are signaled).
    It returns:
     - WaitIsNotSatisfied if the wait-all is not fully satisfied.
     - WaitIsSatisfied if the wait-all is fully satisfied.

    Note: this method must be called while holding the synchronization locks
          appropriate to all the objects involved in the wait-all. If any
          of the objects is shared, the caller must own both local and
          shared synch locks; if no shared object is involved in the wait,
          only the local synch lock is needed.
    --*/
    WaitCompletionState CSynchData::IsRestOfWaitAllSatisfied(
        WaitingThreadsListNode * pwtlnNode)
    {
        int iSignaledOrOwnedObjCount = 0;
        int iTgtCount = 0;
        int i;
        WaitCompletionState wcsWaitCompletionState = WaitIsNotSatisfied;
        CSynchData * psdSynchDataItem = NULL;
        ThreadWaitInfo * ptwiWaitInfo = NULL;

        VALIDATEOBJECT(this);
        VALIDATEOBJECT(pwtlnNode);

        _ASSERT_MSG(0 != (WTLN_FLAG_WAIT_ALL & pwtlnNode->dwFlags),
                    "IsRestOfWaitAllSatisfied() called on a normal "
                    "(non wait all) wait");

        ///////////////////////////
        //
        // Local Thread Awakening
        //
        ///////////////////////////

        ptwiWaitInfo = pwtlnNode->ptwiWaitInfo;

        iTgtCount = ptwiWaitInfo->lObjCount;
        for (i=0; i < iTgtCount; i++)
        {
            WaitingThreadsListNode * pwtlnItem = ptwiWaitInfo->rgpWTLNodes[i];
            bool fRetVal;
            bool fIsAbandoned;

            VALIDATEOBJECT(pwtlnItem);

            psdSynchDataItem = pwtlnItem->ptrOwnerObjSynchData.ptr;

            VALIDATEOBJECT(psdSynchDataItem);

            if (pwtlnItem == pwtlnNode)
            {
                _ASSERT_MSG (this == psdSynchDataItem,
                             "pwtlnNode and pwtlnItem match, but this "
                             "and psdSynchDataItem don't\n");

                // The target object (the one related to pwtlnNode) is counted as
                // signaled/owned without checking it (also if it is not, as
                // it normally happens when this method is called)
                iSignaledOrOwnedObjCount++;
                continue;
            }

            fRetVal = psdSynchDataItem->CanWaiterWaitWithoutBlocking(
                ptwiWaitInfo->pthrOwner,
                &fIsAbandoned);

            if (fRetVal)
            {
                iSignaledOrOwnedObjCount++;
            }
            else
            {
                break;
            }
        }

        if (iSignaledOrOwnedObjCount < iTgtCount)
        {
            wcsWaitCompletionState = WaitIsNotSatisfied;
        }
        else
        {
            wcsWaitCompletionState = WaitIsSatisfied;
        }

        TRACE("IsRestOfWaitAllSatisfied() returning %u \n", wcsWaitCompletionState);

        return wcsWaitCompletionState;
    }


    /*++
    Method:
      CSynchData::SetOwner

    Blindly sets the thread whose CPalThread is passed as argument, as the
    owner of the current object.
    WARNING: this method discards any previous ownership data and does not
    update the list of the object owned by the owner thread.

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    void CSynchData::SetOwner(CPalThread * pOwnerThread)
    {
        VALIDATEOBJECT(this);

        m_dwOwnerPid   = gPID;
        m_dwOwnerTid   = pOwnerThread->GetThreadId();
        m_pOwnerThread = pOwnerThread;
    }

    /*++
    Method:
      CSynchData::ResetOwnership

    Resets current object's ownership data

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    void CSynchData::ResetOwnership()
    {
        VALIDATEOBJECT(this);

        m_lOwnershipCount          = 0;
        m_dwOwnerPid               = 0;
        m_dwOwnerTid               = 0;
        m_pOwnerThread             = NULL;
        m_poolnOwnedObjectListNode = NULL;
    }

    /*++
    Method:
      CSynchData::AssignOwnershipToThread

    Assigns thw ownership of the current object to the target thread, performing
    all the operations neede to mantain the correct status of ownership data,
    also handling recursive object ownership acquisition

    Note: this method must be called while holding the appropriate
          synchronization locks (the local process synch lock if the target
          object is local, both local and shared one if the object is shared).
    --*/
    PAL_ERROR CSynchData::AssignOwnershipToThread(
        CPalThread * pthrCurrent,
        CPalThread * pthrTarget)
    {
        // Note: when this method is called by ReleaseFirstWaiter there is
        //       a small time window in which both SignalCount and
        //       OwnershipCount can be greater than zero (which normally
        //       is illegal). Anyway that is fine since ReleaseFirstWaiter
        //       will restore the value right after, and such situation
        //       takes place while holding synchroniztion locks, so no
        //       other thread/process can access the object.

        PAL_ERROR palErr = NO_ERROR;

        _ASSERT_MSG(CObjectType::OwnershipTracked ==
                    GetObjectType()->GetOwnershipSemantics(),
                    "AssignOwnershipToThread called on a non-ownable "
                    "CSynchData [this=%p OwnershipSemantics=%u]\n", this,
                    GetObjectType()->GetOwnershipSemantics());


        if (0 < m_lOwnershipCount)
        {
            //
            // Object already owned, incrementing ownership count
            //
            _ASSERT_MSG(0 == GetSignalCount(),
                        "Conflicting OwnershipCount and SignalCount values\n");

            _ASSERT_MSG(pthrTarget == m_pOwnerThread && gPID == m_dwOwnerPid,
                        "Attempting to assign ownership of CSynchData %p to "
                        "thread {pid=%#x tid=%#x} while it is currently owned "
                        "by thread {pid=%#x tid=%#x}\n", this,
                        gPID, pthrTarget->GetThreadId(),
                        m_dwOwnerPid, m_pOwnerThread->GetThreadId());

            m_lOwnershipCount++;

            TRACE("Incrementing ownership count for object with "
                  "SynchData %p owned by thread %#x [new count=%d]\n",
                  this, pthrTarget->GetThreadId(), m_lOwnershipCount);
        }
        else
        {
            //
            // Acquiring currently not owned object
            //
            CPalSynchronizationManager * pSynchManager =
                CPalSynchronizationManager::GetInstance();
            OwnedObjectsListNode * pooln;

            pooln = pSynchManager->CacheGetOwnedObjsListNode(pthrCurrent);
            if (NULL == pooln)
            {
                ERROR("Out of memory while acquiring mutex ownership");
                // In this case we bail out. It will result in no
                // thread being awakend, which may cause deadlock,
                // but it is anyway better than corrupting the
                // ownership list
                palErr = ERROR_NOT_ENOUGH_MEMORY;
                goto AOTT_exit;
            }

            TRACE("Assigning ownable object with SynchData %p to "
                  "thread %#x\n",
                  this, pthrTarget->GetThreadId());

            // Set ownership data
            SetOwner(pthrTarget);
            SetOwnershipListNode(pooln);
            SetOwnershipCount(1);
            SetAbandoned(false);

            // Add object to list of owned objs for current thread
            pooln->pPalObjSynchData = this;
            AddRef();
            pthrTarget->synchronizationInfo.AddObjectToOwnedList(pooln);
        }

    AOTT_exit:
        return palErr;
    }

    /*++
    Method:
      CSynchData::WaiterEnqueue

    Adds the WaitingThreadsListNode passed as argument at the end of the
    list of WaitingThreadsListNode for the current object, representing
    the threads waiting on the current object. The target SynchData is
    assumed to be local to the current process

    Note: this method must be called while holding the local process
          synchronization lock.
    --*/
    void CSynchData::WaiterEnqueue(WaitingThreadsListNode * pwtlnNewNode, bool fPrioritize)
    {
        VALIDATEOBJECT(this);
        VALIDATEOBJECT(pwtlnNewNode);

        if (!fPrioritize)
        {
            // Enqueue normally to the end of the queue
            WaitingThreadsListNode * pwtlnCurrLast = m_ptrWTLTail.ptr;

            pwtlnNewNode->ptrNext.ptr = NULL;
            if (NULL == pwtlnCurrLast)
            {
                _ASSERT_MSG(NULL == m_ptrWTLHead.ptr,
                            "Corrupted waiting list on local CSynchData @ %p\n",
                            this);

                pwtlnNewNode->ptrPrev.ptr = NULL;
                m_ptrWTLHead.ptr = pwtlnNewNode;
                m_ptrWTLTail.ptr = pwtlnNewNode;
            }
            else
            {
                VALIDATEOBJECT(pwtlnCurrLast);

                pwtlnNewNode->ptrPrev.ptr = pwtlnCurrLast;
                pwtlnCurrLast->ptrNext.ptr = pwtlnNewNode;
                m_ptrWTLTail.ptr = pwtlnNewNode;
            }
        }
        else
        {
            // The wait is prioritized, enqueue to the beginning of the queue
            WaitingThreadsListNode * pwtlnCurrFirst = m_ptrWTLHead.ptr;

            pwtlnNewNode->ptrPrev.ptr = NULL;
            if (NULL == pwtlnCurrFirst)
            {
                _ASSERT_MSG(NULL == m_ptrWTLTail.ptr,
                            "Corrupted waiting list on local CSynchData @ %p\n",
                            this);

                pwtlnNewNode->ptrNext.ptr = NULL;
                m_ptrWTLHead.ptr = pwtlnNewNode;
                m_ptrWTLTail.ptr = pwtlnNewNode;
            }
            else
            {
                VALIDATEOBJECT(pwtlnCurrFirst);

                pwtlnNewNode->ptrNext.ptr = pwtlnCurrFirst;
                pwtlnCurrFirst->ptrPrev.ptr = pwtlnNewNode;
                m_ptrWTLHead.ptr = pwtlnNewNode;
            }
        }

        m_ulcWaitingThreads += 1;

        return;
    }

#ifdef SYNCH_OBJECT_VALIDATION
    CSynchData::~CSynchData()
    {
        ValidateObject(true);
        InvalidateObject();
    }
    /*++
    Method:
      CSynchData::ValidateObject

    Makes sure that the signature at the beginning and at the end of the
    current object are those of a currently alive object (i.e. the object
    has been constructed and does not appear to have been overwritten)
    --*/
    void CSynchData::ValidateObject(bool fDestructor)
    {
        TRACE("Verifying in-use CSynchData @ %p\n", this);
        _ASSERT_MSG(HeadSignature == m_dwDebugHeadSignature,
                    "CSynchData header signature corruption [p=%p]", this);
        _ASSERT_MSG(TailSignature == m_dwDebugTailSignature,
                    "CSynchData trailer signature corruption [p=%p]", this);
        _ASSERT_MSG((fDestructor && 0 == m_lRefCount) ||
                    (!fDestructor && 0 < m_lRefCount),
                    "CSynchData %p with NULL reference count\n", this);
    }
    /*++
    Method:
      CSynchData::ValidateEmptyObject

    Makes sure that the signature at the beginning and at the end of the
    current object are not those of a currently alive object (i.e. the
    object has not yet been constructed or it has alread been destructed)
    --*/
    void CSynchData::ValidateEmptyObject()
    {
        TRACE("Verifying empty CSynchData @ %p\n", this);
        _ASSERT_MSG(HeadSignature != m_dwDebugHeadSignature,
                    "CSynchData header previously signed [p=%p]", this);
        _ASSERT_MSG(TailSignature != m_dwDebugTailSignature,
                    "CSynchData trailer previously signed [p=%p]", this);
    }
    /*++
    Method:
      CSynchData::InvalidateObject

    Turns signatures from alive object to destructed object
    --*/
    void CSynchData::InvalidateObject()
    {
        TRACE("Invalidating CSynchData @ %p\n", this);
        m_dwDebugHeadSignature = EmptySignature;
        m_dwDebugTailSignature = EmptySignature;
    }
#endif // SYNCH_OBJECT_VALIDATION
}

