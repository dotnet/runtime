// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/synchobjects.hpp

Abstract:
    Header file for synchronization manager and controllers



--*/

#ifndef _SINCHOBJECTS_HPP_
#define _SINCHOBJECTS_HPP_

#include "corunix.hpp"
#include "threadinfo.hpp"
#include "mutex.hpp"
#include "shm.hpp"
#include "list.h"

#include <pthread.h>

#define SharedID SHMPTR
#define SharedPoolId ULONG_PTR
#define DefaultSharedPool ((ULONG_PTR)0)
#define NULLSharedID ((SHMPTR)NULL)
#define SharedIDToPointer(shID) SHMPTR_TO_TYPED_PTR(PVOID, shID)
#define SharedIDToTypePointer(TYPE,shID) SHMPTR_TO_TYPED_PTR(TYPE, shID)
#define RawSharedObjectAlloc(szSize, shPoolId) SHMalloc(szSize)
#define RawSharedObjectFree(shID) SHMfree(shID)
    
namespace CorUnix
{   
    DWORD InternalWaitForMultipleObjectsEx(
        CPalThread * pthrCurrent,
        DWORD nCount,
        CONST HANDLE *lpHandles,
        BOOL bWaitAll,
        DWORD dwMilliseconds,
        BOOL bAlertable);
    
    PAL_ERROR InternalSleepEx(
        CPalThread * pthrCurrent,
        DWORD dwMilliseconds,
        BOOL bAlertable);
        
    enum THREAD_STATE
    {
        TS_IDLE,
        TS_STARTING,
        TS_RUNNING,
        TS_FAILED,
        TS_DONE,
    };

    // forward declarations
    struct _ThreadWaitInfo;
    struct _WaitingThreadsListNode;
    class CSynchData;
    
    typedef struct _WaitingThreadsListNode * PWaitingThreadsListNode;
    typedef struct _OwnedObjectsListNode * POwnedObjectsListNode;
    typedef struct _ThreadApcInfoNode * PThreadApcInfoNode;
        
    typedef struct _ThreadWaitInfo
    {   
        WaitType wtWaitType;
        WaitDomain wdWaitDomain;
        LONG lObjCount;
        LONG lSharedObjCount;
        CPalThread * pthrOwner;        
        PWaitingThreadsListNode rgpWTLNodes[MAXIMUM_WAIT_OBJECTS];
        
        _ThreadWaitInfo() : wtWaitType(SingleObject), wdWaitDomain(LocalWait), 
                            lObjCount(0), lSharedObjCount(0), 
                            pthrOwner(NULL) {}
    } ThreadWaitInfo;

    typedef struct _ThreadNativeWaitData 
    {
        pthread_mutex_t     mutex;
        pthread_cond_t      cond;
        int                 iPred;
        DWORD               dwObjectIndex;
        ThreadWakeupReason  twrWakeupReason;
        bool                fInitialized;

        _ThreadNativeWaitData() :
            iPred(0), 
            dwObjectIndex(0), 
            twrWakeupReason(WaitSucceeded), 
            fInitialized(false)
        {
        }

        ~_ThreadNativeWaitData();
    } ThreadNativeWaitData;

    class CThreadSynchronizationInfo : public CThreadInfoInitializer
    {
        friend class CPalSynchronizationManager;
        friend class CSynchWaitController;

        THREAD_STATE           m_tsThreadState; 
        SharedID               m_shridWaitAwakened;
        Volatile<LONG>         m_lLocalSynchLockCount;
        Volatile<LONG>         m_lSharedSynchLockCount;
        LIST_ENTRY             m_leOwnedObjsList;

        CRITICAL_SECTION       m_ownedNamedMutexListLock;
        NamedMutexProcessData *m_ownedNamedMutexListHead;

        ThreadNativeWaitData   m_tnwdNativeData;
        ThreadWaitInfo         m_twiWaitInfo;

#ifdef SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        static const int       PendingSignalingsArraySize = 10;
        LONG                   m_lPendingSignalingCount;
        CPalThread *           m_rgpthrPendingSignalings[PendingSignalingsArraySize];
        LIST_ENTRY             m_lePendingSignalingsOverflowList;
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING

    public:

        CThreadSynchronizationInfo();
        virtual ~CThreadSynchronizationInfo();

        //
        // CThreadInfoInitializer methods
        //
        virtual PAL_ERROR InitializePreCreate(void);
        
        virtual PAL_ERROR InitializePostCreate(
            CPalThread *pthrCurrent,
            SIZE_T threadId,
            DWORD dwLwpId
            );

        THREAD_STATE GetThreadState(void)
        {
            return m_tsThreadState;
        };

        void SetThreadState(THREAD_STATE tsThreadState)
        {
            m_tsThreadState = tsThreadState;
        };

        ThreadNativeWaitData * GetNativeData()
        {
            return &m_tnwdNativeData;
        }

#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
        PAL_ERROR RunDeferredThreadConditionSignalings();
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
    
        // NOTE: the following methods provide non-synchronized access to 
        //       the list of owned objects for this thread. Any thread 
        //       accessing this list MUST own the appropriate 
        //       synchronization lock(s).
        void AddObjectToOwnedList(POwnedObjectsListNode pooln);
        void RemoveObjectFromOwnedList(POwnedObjectsListNode pooln);
        POwnedObjectsListNode RemoveFirstObjectFromOwnedList(void);

        void AddOwnedNamedMutex(NamedMutexProcessData *processData);
        void RemoveOwnedNamedMutex(NamedMutexProcessData *processData);
        NamedMutexProcessData *RemoveFirstOwnedNamedMutex();
        bool OwnsNamedMutex(NamedMutexProcessData *processData);

        // The following methods provide access to the native wait lock for 
        // those implementations that need a lock to protect the support for 
        // native thread blocking (e.g.: pthread conditions)
        void AcquireNativeWaitLock(void);
        void ReleaseNativeWaitLock(void);
        bool TryAcquireNativeWaitLock(void);
    };

    class CThreadApcInfo : public CThreadInfoInitializer
    {
        friend class CPalSynchronizationManager;

        PThreadApcInfoNode m_ptainHead;
        PThreadApcInfoNode m_ptainTail;
        
    public:
        CThreadApcInfo() :
            m_ptainHead(NULL),
            m_ptainTail(NULL)
        {
        }
    };
        
    class CPalSynchMgrController
    {
    public:
        static IPalSynchronizationManager * CreatePalSynchronizationManager();

        static PAL_ERROR StartWorker(CPalThread * pthrCurrent);

        static PAL_ERROR PrepareForShutdown(void);
        
        static PAL_ERROR Shutdown(CPalThread *pthrCurrent, bool fFullCleanup);
    };
}

#endif // _SINCHOBJECTS_HPP_

