// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    synchmanager.hpp

Abstract:
    Private header file for synchronization manager and 
    controllers implementation



--*/
#ifndef _SYNCHMANAGER_HPP_
#define _SYNCHMANAGER_HPP_

#include "pal/synchobjects.hpp"
#include "pal/synchcache.hpp"
#include "pal/cs.hpp"
#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/procobj.hpp"
#include "pal/init.h"
#include "pal/process.h"

#include <sys/types.h>
#include <unistd.h>
#if HAVE_KQUEUE
#include <sys/event.h>
#endif // HAVE_KQUEUE
#include "pal/dbgmsg.h"

#ifdef _DEBUG
// #define SYNCH_OBJECT_VALIDATION
// #define SYNCH_STATISTICS
#endif

#ifdef SYNCH_OBJECT_VALIDATION
#define VALIDATEOBJECT(obj) ((obj)->ValidateObject())
#else
#define VALIDATEOBJECT(obj)
#endif

namespace CorUnix
{
    const DWORD WTLN_FLAG_OWNER_OBJECT_IS_SHARED                 = 1<<0;
    const DWORD WTLN_FLAG_WAIT_ALL                               = 1<<1;
    const DWORD WTLN_FLAG_DELEGATED_OBJECT_SIGNALING_IN_PROGRESS = 1<<2;

#ifdef SYNCH_OBJECT_VALIDATION
    const DWORD HeadSignature  = 0x48454144;
    const DWORD TailSignature  = 0x5441494C;
    const DWORD EmptySignature = 0xBAADF00D;
#endif

    enum THREAD_WAIT_STATE
    {
        TWS_ACTIVE,
        TWS_WAITING,
        TWS_ALERTABLE,
        TWS_EARLYDEATH,
    };

    enum WaitCompletionState
    {
        WaitIsNotSatisfied,
        WaitIsSatisfied,
        WaitMayBeSatisfied
    };

    typedef union _SynchDataGenrPtr
    {
        SharedID shrid;
        CSynchData * ptr;
    } SynchDataGenrPtr;

    typedef union _WTLNodeGenrPtr
    {
        SharedID shrid;
        struct _WaitingThreadsListNode * ptr;
    } WTLNodeGenrPtr;

    typedef struct _WaitingThreadsListNode
    {
#ifdef SYNCH_OBJECT_VALIDATION
        DWORD dwDebugHeadSignature;
#endif
        WTLNodeGenrPtr ptrNext;
        WTLNodeGenrPtr ptrPrev;
        SharedID shridSHRThis;

        // Data
        DWORD dwThreadId;
        DWORD dwProcessId;
        DWORD dwObjIndex;
        DWORD dwFlags;

        // Pointers to related objects
        SharedID shridWaitingState; 
        SynchDataGenrPtr ptrOwnerObjSynchData;
        struct _ThreadWaitInfo * ptwiWaitInfo;  // valid only in the 
                                                // target process
#ifdef SYNCH_OBJECT_VALIDATION
        _WaitingThreadsListNode();
        ~_WaitingThreadsListNode();
        void ValidateObject(void);
        void ValidateEmptyObject(void);
        void InvalidateObject(void);

        DWORD dwDebugTailSignature;
#endif
    } WaitingThreadsListNode;

    typedef struct _DeferredSignalingListNode
    {
        LIST_ENTRY Link;
        CPalThread * pthrTarget;
    } DeferredSignalingListNode;

    typedef struct _OwnedObjectsListNode
    {
        LIST_ENTRY Link;
        CSynchData * pPalObjSynchData;
    } OwnedObjectsListNode;

    typedef struct _ThreadApcInfoNode
    {
        struct _ThreadApcInfoNode * pNext;
        PAPCFUNC pfnAPC;
        ULONG_PTR pAPCData;
    } ThreadApcInfoNode;

    class CPalSynchronizationManager; // fwd declaration
    class CProcProcessLocalData;      // fwd declaration

    class CSynchData
    {
#ifdef SYNCH_OBJECT_VALIDATION
        DWORD m_dwDebugHeadSignature;
#endif
        // NB: For perforformance purposes this class is supposed
        //     to have no virtual methods, and no destructor.

        WTLNodeGenrPtr  m_ptrWTLHead;
        WTLNodeGenrPtr  m_ptrWTLTail;
        ULONG m_ulcWaitingThreads;
        SharedID m_shridThis;
        ObjectDomain m_odObjectDomain; 
        PalObjectTypeId m_otiObjectTypeId;
        LONG  m_lRefCount;
        LONG  m_lSignalCount;

        // Ownership data
        LONG  m_lOwnershipCount;
        DWORD m_dwOwnerPid;
        DWORD m_dwOwnerTid; // used only by remote processes
                            // (thread ids may be recycled)
        CPalThread * m_pOwnerThread; // valid only on the target process
        OwnedObjectsListNode * m_poolnOwnedObjectListNode;
        bool m_fAbandoned;

#ifdef SYNCH_STATISTICS
        ULONG m_lStatWaitCount;
        ULONG m_lStatContentionCount;
#endif

    public:
        CSynchData() 
            : m_ulcWaitingThreads(0), m_shridThis(NULLSharedID), m_lRefCount(1),
              m_lSignalCount(0), m_lOwnershipCount(0), m_dwOwnerPid(0),
              m_dwOwnerTid(0), m_pOwnerThread(NULL), 
              m_poolnOwnedObjectListNode(NULL), m_fAbandoned(false)
        { 
            // m_ptrWTLHead, m_ptrWTLTail, m_odObjectDomain 
            // and m_otiObjectTypeId are initialized by
            // CPalSynchronizationManager::AllocateObjectSynchData
#ifdef SYNCH_STATISTICS
            m_lStatWaitCount = 0;
            m_lStatContentionCount = 0;
#endif
#ifdef SYNCH_OBJECT_VALIDATION
            ValidateEmptyObject();
            m_dwDebugHeadSignature = HeadSignature;;
            m_dwDebugTailSignature = TailSignature;
#endif
        }

        LONG AddRef()
        {
            return InterlockedIncrement(&m_lRefCount); 
        }

        LONG Release(CPalThread * pthrCurrent);

        bool CanWaiterWaitWithoutBlocking(
            CPalThread * pWaiterThread,
            bool * pfAbandoned);

        PAL_ERROR ReleaseWaiterWithoutBlocking(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget);

        void WaiterEnqueue(WaitingThreadsListNode * pwtlnNewNode);
        void SharedWaiterEnqueue(SharedID shridNewNode);

        // Object Domain accessor methods
        ObjectDomain GetObjectDomain(void)
        {
            return m_odObjectDomain;
        }
        void SetObjectDomain(ObjectDomain odObjectDomain)
        {
            m_odObjectDomain = odObjectDomain;
        }

        // Object Type accessor methods
        CObjectType * GetObjectType(void)
        {
            return CObjectType::GetObjectTypeById(m_otiObjectTypeId);
        }
        PalObjectTypeId GetObjectTypeId(void)
        {
            return m_otiObjectTypeId;
        }
        void SetObjectType(CObjectType * pot)
        {
            m_otiObjectTypeId = pot->GetId();
        }
        void SetObjectType(PalObjectTypeId oti)
        {
            m_otiObjectTypeId = oti;
        }

        // Object shared 'this' pointer accessor methods
        SharedID GetSharedThis (void)
        {
            return m_shridThis;
        }
        void SetSharedThis (SharedID shridThis)
        {
            m_shridThis = shridThis;
        }

        void Signal(
            CPalThread * pthrCurrent, 
            LONG lSignalCount, 
            bool fWorkerThread);

        bool ReleaseFirstWaiter(
            CPalThread * pthrCurrent,
            bool * pfDelegated,
            bool fWorkerThread);

        LONG ReleaseAllLocalWaiters(
            CPalThread * pthrCurrent);

        WaitCompletionState IsRestOfWaitAllSatisfied(
            WaitingThreadsListNode * pwtlnNode);

        // Object signal count accessor methods
        LONG GetSignalCount(void) 
        {
            _ASSERTE(m_lSignalCount >= 0);
            return m_lSignalCount; 
        }
        void SetSignalCount(LONG lSignalCount) 
        { 
            _ASSERTE(m_lSignalCount >= 0);
            _ASSERTE(lSignalCount >= 0);
            m_lSignalCount = lSignalCount; 
        }
        LONG DecrementSignalCount(void) 
        {
            _ASSERTE(m_lSignalCount > 0);
            return --m_lSignalCount; 
        }

        // Object ownership accessor methods
        void SetOwner(CPalThread * pOwnerThread);
        void ResetOwnership(void);
        PAL_ERROR AssignOwnershipToThread(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget);
        DWORD GetOwnerProcessID(void) 
        { 
            return m_dwOwnerPid; 
        }
        DWORD GetOwnerThreadID(void) 
        { 
            return m_dwOwnerTid; 
        }
        CPalThread * GetOwnerThread(void) 
        { 
            return m_pOwnerThread; 
        }
        OwnedObjectsListNode * GetOwnershipListNode(void)
        {
            return m_poolnOwnedObjectListNode;
        }
        void SetOwnershipListNode(OwnedObjectsListNode * pooln)
        {
            m_poolnOwnedObjectListNode = pooln;
        }

        // Object ownership count accessor methods
        LONG GetOwnershipCount(void) 
        { 
            return m_lOwnershipCount; 
        }
        void SetOwnershipCount(LONG lOwnershipCount) 
        { 
            m_lOwnershipCount = lOwnershipCount;
        }

        // Object abandoned flag accessor methods
        void SetAbandoned(bool fAbandoned) 
                                    { m_fAbandoned = fAbandoned; }
        bool IsAbandoned(void) { return m_fAbandoned; }

        void IncrementWaitingThreadCount(void)
        {
            m_ulcWaitingThreads += 1;
        }
        void DecrementWaitingThreadCount(void)
        {
            m_ulcWaitingThreads -= 1;
        }
        ULONG GetWaitingThreadCount(void)
        {
            return m_ulcWaitingThreads;
        }


#ifdef SYNCH_STATISTICS
        void IncrementStatWaitCount(void)
        {
            m_lStatWaitCount++;
        }
        LONG GetStatWaitCount(void)
        {
            return m_lStatWaitCount;
        }
        void IncrementStatContentionCount(void)
        {
            m_lStatContentionCount++;
        }
        LONG GetStatContentionCount(void)
        {
            return m_lStatContentionCount;
        }
#endif
        //
        // Wating threads list access methods
        //
        WaitingThreadsListNode * GetWTLHeadPtr(void) 
        { 
            return m_ptrWTLHead.ptr; 
        }
        WaitingThreadsListNode * GetWTLTailPtr(void) 
        { 
            return m_ptrWTLTail.ptr; 
        }
        SharedID GetWTLHeadShmPtr(void) 
        { 
            return m_ptrWTLHead.shrid; 
        }
        SharedID GetWTLTailShmPtr(void) 
        { 
            return m_ptrWTLTail.shrid; 
        }
        void SetWTLHeadPtr(WaitingThreadsListNode * p) 
        { 
            m_ptrWTLHead.ptr = p; 
        }
        void SetWTLTailPtr(WaitingThreadsListNode * p) 
        { 
            m_ptrWTLTail.ptr = p; 
        }
        void SetWTLHeadShrPtr(SharedID shrid) 
        { 
            m_ptrWTLHead.shrid = shrid; 
        }
        void SetWTLTailShrPtr(SharedID shrid) 
        { 
            m_ptrWTLTail.shrid = shrid; 
        }
#ifdef SYNCH_OBJECT_VALIDATION
        ~CSynchData();
        void ValidateObject(bool fDestructor = false);
        void ValidateEmptyObject(void);
        void InvalidateObject(void);

        DWORD m_dwDebugTailSignature;
#endif
    };

    
    class CSynchControllerBase
    {
        friend class CPalSynchronizationManager;

        // NB: For perforformance purposes this class is supposed
        //     to have no virtual methods, contructor and 
        //     destructor
    public:
        enum ControllerType { WaitController, StateController };

    protected:
        CPalThread * m_pthrOwner;
        ControllerType m_ctCtrlrType;
        ObjectDomain m_odObjectDomain;
        CObjectType * m_potObjectType;
        CSynchData * m_psdSynchData;
        WaitDomain m_wdWaitDomain;
             
        PAL_ERROR Init(
            CPalThread * pthrCurrent,
            ControllerType ctCtrlrType,
            ObjectDomain odObjectDomain,
            CObjectType *potObjectType,
            CSynchData * psdSynchData,
            WaitDomain wdWaitDomain);
        
        void Release(void);

        void SetSynchData(CSynchData * psdSynchData) 
        { 
            m_psdSynchData = psdSynchData; 
        }
        CSynchData * GetSynchData() 
        { 
            return m_psdSynchData; 
        }
    };
        
    class CSynchWaitController : public CSynchControllerBase, 
                                 public ISynchWaitController
    {
        // Per-object-type specific data
        //
        // Process (otiProcess)
        IPalObject *m_pProcessObject; // process that owns m_pProcLocalData, this is stored without a reference
        CProcProcessLocalData * m_pProcLocalData;
        
    public:
        CSynchWaitController() : m_pProcessObject(NULL), m_pProcLocalData(NULL) {}
        virtual ~CSynchWaitController() = default;
        
        //
        // ISynchWaitController methods
        //
        virtual PAL_ERROR CanThreadWaitWithoutBlocking(
            bool * pfCanWaitWithoutBlocking,
            bool * pfAbandoned);
        
        virtual PAL_ERROR ReleaseWaitingThreadWithoutBlocking(void);
        
        virtual PAL_ERROR RegisterWaitingThread(
            WaitType wtWaitType,
            DWORD dwIndex,
            bool fAlertable);

        virtual void ReleaseController(void);

        CProcProcessLocalData * GetProcessLocalData(void);

        void SetProcessData(IPalObject* pProcessObject, CProcProcessLocalData * pProcLocalData);
    };  

    class CSynchStateController : public CSynchControllerBase, 
                                  public ISynchStateController
    {
    public:
        // NB: For perforformance purposes this class is supposed
        //     to have no constructor
        virtual ~CSynchStateController() = default;

        //
        // ISynchStateController methods
        //
        virtual PAL_ERROR GetSignalCount(LONG *plSignalCount);
        virtual PAL_ERROR SetSignalCount(LONG lNewCount);
        virtual PAL_ERROR IncrementSignalCount(LONG lAmountToIncrement);
        virtual PAL_ERROR DecrementSignalCount(LONG lAmountToDecrement);
        virtual PAL_ERROR SetOwner(CPalThread *pNewOwningThread);
        virtual PAL_ERROR DecrementOwnershipCount(void);
        virtual void ReleaseController(void);
    };    
    
    class CPalSynchronizationManager : public IPalSynchronizationManager
    {
        friend class CPalSynchMgrController;
        template <class T> friend T *CorUnix::InternalNew();

    public:
        // types
        typedef CSynchCache<CSynchWaitController>      CSynchWaitControllerCache;
        typedef CSynchCache<CSynchStateController>     CSynchStateControllerCache;
        typedef CSynchCache<CSynchData>                CSynchDataCache;
        typedef CSHRSynchCache<CSynchData>             CSHRSynchDataCache;
        typedef CSynchCache<WaitingThreadsListNode>    CWaitingThreadsListNodeCache;
        typedef CSHRSynchCache<WaitingThreadsListNode> CSHRWaitingThreadsListNodeCache;
        typedef CSynchCache<ThreadApcInfoNode>         CThreadApcInfoNodeCache;
        typedef CSynchCache<OwnedObjectsListNode>      COwnedObjectsListNodeCache;

    private:
        // types
        enum InitStatus 
        { 
            SynchMgrStatusIdle, 
            SynchMgrStatusInitializing, 
            SynchMgrStatusRunning, 
            SynchMgrStatusShuttingDown,
            SynchMgrStatusReadyForProcessShutDown,
            SynchMgrStatusError 
        };  
        enum SynchWorkerCmd 
        { 
            SynchWorkerCmdNop,
            SynchWorkerCmdRemoteSignal,
            SynchWorkerCmdDelegatedObjectSignaling,
            SynchWorkerCmdShutdown,
            SynchWorkerCmdTerminationRequest,
            SynchWorkerCmdLast
        };

        typedef struct _MonitoredProcessesListNode
        {
            struct _MonitoredProcessesListNode * pNext;
            LONG lRefCount;
            CSynchData * psdSynchData;
            DWORD dwPid;
            DWORD dwExitCode;
            bool fIsActualExitCode;

            // Object that owns pProcLocalData. This is stored, with a reference, to 
            // ensure that pProcLocalData is not deleted.
            IPalObject *pProcessObject;
            CProcProcessLocalData * pProcLocalData;
        } MonitoredProcessesListNode;

        // constants
        static const int CtrlrsCacheMaxSize                = 256;
        static const int SynchDataCacheMaxSize             = 256;
        static const int WTListNodeCacheMaxSize            = 256;
        static const int ApcInfoNodeCacheMaxSize           = 32;
        static const int OwnedObjectsListCacheMaxSize      = 16;
        static const int MaxWorkerConsecutiveEintrs        = 128;
        static const int MaxConsecutiveEagains             = 128;
        static const int WorkerThreadProcMonitoringTimeout = 250;  // ms
        static const int WorkerThreadShuttingDownTimeout   = 1000; // ms
        static const int WorkerCmdCompletionTimeout        = 250;  // ms
        static const DWORD SecondNativeWaitTimeout         = INFINITE;
        static const DWORD WorkerThreadTerminationTimeout  = 2000; // ms

        // static members
        static CPalSynchronizationManager * s_pObjSynchMgr;        
        static Volatile<LONG>               s_lInitStatus;
        static CRITICAL_SECTION             s_csSynchProcessLock;
        static CRITICAL_SECTION             s_csMonitoredProcessesLock;
        
        // members        
        DWORD                           m_dwWorkerThreadTid;
        IPalObject *                    m_pipoThread;
        CPalThread *                    m_pthrWorker;
        int                             m_iProcessPipeRead;
        int                             m_iProcessPipeWrite;
#if HAVE_KQUEUE
        int                             m_iKQueue;
        struct kevent                   m_keProcessPipeEvent;
#endif // HAVE_KQUEUE

        MonitoredProcessesListNode *    m_pmplnMonitoredProcesses;
        LONG                            m_lMonitoredProcessesCount;
        MonitoredProcessesListNode *    m_pmplnExitedNodes;

        // caches
        CSynchWaitControllerCache       m_cacheWaitCtrlrs;
        CSynchStateControllerCache      m_cacheStateCtrlrs;
        CSynchDataCache                 m_cacheSynchData;
        CSHRSynchDataCache              m_cacheSHRSynchData;
        CWaitingThreadsListNodeCache    m_cacheWTListNodes;
        CSHRWaitingThreadsListNodeCache m_cacheSHRWTListNodes;
        CThreadApcInfoNodeCache         m_cacheThreadApcInfoNodes;
        COwnedObjectsListNodeCache      m_cacheOwnedObjectsListNodes;

        // static methods
        static PAL_ERROR Initialize();
        static DWORD PALAPI WorkerThread(LPVOID pArg);

    protected:
        CPalSynchronizationManager();

        PAL_ERROR GetSynchControllersForObjects(
            CPalThread *pthrCurrent,
            IPalObject *rgObjects[],
            DWORD dwObjectCount,
            void ** ppvControllers,
            CSynchControllerBase::ControllerType ctCtrlrType);

    private:
        static IPalSynchronizationManager * CreatePalSynchronizationManager();
        static PAL_ERROR StartWorker(CPalThread * pthrCurrent);
        static PAL_ERROR PrepareForShutdown(void);

    public:
        virtual ~CPalSynchronizationManager();

        static CPalSynchronizationManager * GetInstance(void)
        { 
            // No need here to check for NULL and in case create the 
            // singleton, since its creation is enforced by the PAL 
            // initialization code.
            return s_pObjSynchMgr; 
        }

        //
        // Inline utility methods
        //
        static void AcquireLocalSynchLock(CPalThread * pthrCurrent)
        { 
            _ASSERTE(0 <= pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount);
            
            if (1 == ++pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount)
            {
                InternalEnterCriticalSection(pthrCurrent, &s_csSynchProcessLock);
            }
        }
        static void ReleaseLocalSynchLock(CPalThread * pthrCurrent) 
        {
            _ASSERTE(0 < pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount);
            if (0 == --pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount)
            {
                InternalLeaveCriticalSection(pthrCurrent, &s_csSynchProcessLock);
                
#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
                pthrCurrent->synchronizationInfo.RunDeferredThreadConditionSignalings();
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
            }
        }
        static LONG ResetLocalSynchLock(CPalThread * pthrCurrent) 
        {
            LONG lRet = pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount;

            _ASSERTE(0 <= lRet);
            if (0 < lRet)
            {
                pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount = 0;
                InternalLeaveCriticalSection(pthrCurrent, &s_csSynchProcessLock);

#if SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
                pthrCurrent->synchronizationInfo.RunDeferredThreadConditionSignalings();
#endif // SYNCHMGR_SUSPENSION_SAFE_CONDITION_SIGNALING
            }
            return lRet;
        }
        static LONG GetLocalSynchLockCount(CPalThread * pthrCurrent) 
        {
            _ASSERTE(0 <= pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount);
            return pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount;
        }

        static void AcquireSharedSynchLock(CPalThread * pthrCurrent)
        {   
            _ASSERTE(0 <= pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount);
            _ASSERT_MSG(0 < pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount,
                "The local synch lock should be acquired before grabbing the "
                "shared one.\n");
            if (1 == ++pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount)
            {
                SHMLock();
            }
        } 
        static void ReleaseSharedSynchLock(CPalThread * pthrCurrent) 
        {
            _ASSERTE(0 < pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount);
            if (0 == --pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount)
            {
                _ASSERT_MSG(0 < pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount,
                    "Final release of the shared synch lock while not holding the "
                    "local one. Local synch lock should always be acquired first and "
                    "released last.\n");                
                SHMRelease();
            }                
        }
        static LONG ResetSharedSynchLock(CPalThread * pthrCurrent) 
        {   
            LONG lRet = pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount;

            _ASSERTE(0 <= lRet);
            _ASSERTE(0 == lRet || 
                     0 < pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount);
            if (0 < lRet)
            {
                pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount = 0;
                SHMRelease();
            }
            return lRet;
        }
        static LONG GetSharedSynchLockCount(CPalThread * pthrCurrent) 
        {
            _ASSERTE(0 <= pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount);
            _ASSERTE(0 == pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount || 
                     0 < pthrCurrent->synchronizationInfo.m_lLocalSynchLockCount);
            return pthrCurrent->synchronizationInfo.m_lSharedSynchLockCount;
        }

        CSynchWaitController * CacheGetWaitCtrlr(CPalThread * pthrCurrent)
        { 
            return m_cacheWaitCtrlrs.Get(pthrCurrent); 
        }
        int CacheGetWaitCtrlr(
            CPalThread * pthrCurrent, 
            int n, 
            CSynchWaitController * prgCtrlrs[])
        { 
            return m_cacheWaitCtrlrs.Get(pthrCurrent, n, prgCtrlrs); 
        }
        void CacheAddWaitCtrlr(
            CPalThread * pthrCurrent, 
            CSynchWaitController * pCtrlr) 
        { 
            m_cacheWaitCtrlrs.Add(pthrCurrent, pCtrlr); 
        }            
        CSynchStateController * CacheGetStateCtrlr(CPalThread * pthrCurrent)
        { 
            return m_cacheStateCtrlrs.Get(pthrCurrent); 
        }
        int CacheGetStateCtrlr(
            CPalThread * pthrCurrent,
            int n, 
            CSynchStateController * prgCtrlrs[]) 
        { 
            return m_cacheStateCtrlrs.Get(pthrCurrent, n, prgCtrlrs); 
        }
        void CacheAddStateCtrlr(
            CPalThread * pthrCurrent, 
            CSynchStateController * pCtrlr) 
        { 
            m_cacheStateCtrlrs.Add(pthrCurrent, pCtrlr); 
        }

        CSynchData * CacheGetLocalSynchData(CPalThread * pthrCurrent)            
        { 
            return m_cacheSynchData.Get(pthrCurrent); 
        }
        void CacheAddLocalSynchData(
            CPalThread * pthrCurrent,
            CSynchData * psdSynchData) 
        { 
            m_cacheSynchData.Add(pthrCurrent, psdSynchData); 
        }
        SharedID CacheGetSharedSynchData(CPalThread * pthrCurrent) 
        { 
            return m_cacheSHRSynchData.Get(pthrCurrent); 
        }
        void CacheAddSharedSynchData(
            CPalThread * pthrCurrent,
            SharedID shridSData) 
        { 
            m_cacheSHRSynchData.Add(pthrCurrent, shridSData); 
        }

        WaitingThreadsListNode * CacheGetLocalWTListNode(
            CPalThread * pthrCurrent)
        { 
            return m_cacheWTListNodes.Get(pthrCurrent); 
        }
        void CacheAddLocalWTListNode(
            CPalThread * pthrCurrent,
            WaitingThreadsListNode * pWTLNode)
        { 
            m_cacheWTListNodes.Add(pthrCurrent, pWTLNode); 
        }
        SharedID CacheGetSharedWTListNode(CPalThread * pthrCurrent) 
        { 
            return m_cacheSHRWTListNodes.Get(pthrCurrent); 
        }
        void CacheAddSharedWTListNode(
            CPalThread * pthrCurrent,
            SharedID shridWTLNode) 
        { 
            m_cacheSHRWTListNodes.Add(pthrCurrent, shridWTLNode); 
        }

        ThreadApcInfoNode * CacheGetApcInfoNodes(CPalThread * pthrCurrent) 
        { 
            return m_cacheThreadApcInfoNodes.Get(pthrCurrent); 
        }
        void CacheAddApcInfoNodes(
            CPalThread * pthrCurrent,
            ThreadApcInfoNode * pNode) 
        { 
            m_cacheThreadApcInfoNodes.Add(pthrCurrent, pNode); 
        }

        OwnedObjectsListNode * CacheGetOwnedObjsListNode(
            CPalThread * pthrCurrent)
        { 
            return m_cacheOwnedObjectsListNodes.Get(pthrCurrent); 
        }
        void CacheAddOwnedObjsListNode(
            CPalThread * pthrCurrent,
            OwnedObjectsListNode * pNode) 
        { 
            m_cacheOwnedObjectsListNodes.Add(pthrCurrent, pNode); 
        }


        //
        // IPalSynchronizationManager methods
        //
        virtual PAL_ERROR BlockThread(
            CPalThread *pthrCurrent,
            DWORD dwTimeout,
            bool fAlertable,
            bool fIsSleep,
            ThreadWakeupReason *ptwrWakeupReason,
            DWORD *pdwSignaledObject);

        virtual PAL_ERROR AbandonObjectsOwnedByThread(
            CPalThread *pthrCurrent,
            CPalThread *pthrTarget);

        virtual PAL_ERROR GetSynchWaitControllersForObjects(
            CPalThread *pthrCurrent,
            IPalObject *rgObjects[],
            DWORD dwObjectCount,
            ISynchWaitController *rgControllers[]);

        virtual PAL_ERROR GetSynchStateControllersForObjects(
            CPalThread *pthrCurrent,
            IPalObject *rgObjects[],
            DWORD dwObjectCount,
            ISynchStateController *rgControllers[]);

        virtual PAL_ERROR AllocateObjectSynchData(
            CObjectType *potObjectType,
            ObjectDomain odObjectDomain,
            VOID **ppvSynchData);

        virtual void FreeObjectSynchData(
            CObjectType *potObjectType,
            ObjectDomain odObjectDomain,
            VOID *pvSynchData);

        virtual PAL_ERROR PromoteObjectSynchData(
            CPalThread *pthrCurrent,
            VOID *pvLocalSynchData,
            VOID **ppvSharedSynchData);

        virtual PAL_ERROR CreateSynchStateController(
            CPalThread *pthrCurrent,
            CObjectType *potObjectType,
            VOID *pvSynchData,
            ObjectDomain odObjectDomain,
            ISynchStateController **ppStateController);

        virtual PAL_ERROR CreateSynchWaitController(
            CPalThread *pthrCurrent,
            CObjectType *potObjectType,
            VOID *pvSynchData,
            ObjectDomain odObjectDomain,
            ISynchWaitController **ppWaitController);

        virtual PAL_ERROR QueueUserAPC(
            CPalThread * pthrCurrent,
            CPalThread *pthrTarget,
            PAPCFUNC pfnAPC,
            ULONG_PTR uptrData);

        virtual PAL_ERROR SendTerminationRequestToWorkerThread();

        virtual bool AreAPCsPending(CPalThread * pthrTarget);

        virtual PAL_ERROR DispatchPendingAPCs(CPalThread * pthrCurrent);

        virtual void AcquireProcessLock(CPalThread *pthrCurrent);

        virtual void ReleaseProcessLock(CPalThread *pthrCurrent);

        //
        // Static helper methods
        //
    public:
        static PAL_ERROR WakeUpLocalThread(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget,
            ThreadWakeupReason twrWakeupReason,
            DWORD dwObjectIndex);

        static PAL_ERROR SignalThreadCondition(
            ThreadNativeWaitData * ptnwdNativeWaitData);

        static PAL_ERROR DeferThreadConditionSignaling(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget);

        static PAL_ERROR WakeUpRemoteThread(
            SharedID shridWLNode);

        static PAL_ERROR DelegateSignalingToRemoteProcess(
            CPalThread * pthrCurrent,
            DWORD dwTargetProcessId,
            SharedID shridSynchData);

        static PAL_ERROR SendMsgToRemoteWorker(
            DWORD dwProcessId,
            BYTE * pMsg,
            int iMsgSize);

        static ThreadWaitInfo * GetThreadWaitInfo(
            CPalThread * pthrCurrent);

        //
        // The following methods must be called only by a Sync*Controller or
        // while holding the required synchronization global locks
        //
        static void UnsignalRestOfLocalAwakeningWaitAll(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget,
            WaitingThreadsListNode * pwtlnNode,
            CSynchData * psdTgtObjectSynchData);

        static void MarkWaitForDelegatedObjectSignalingInProgress(
            CPalThread * pthrCurrent,
            WaitingThreadsListNode * pwtlnNode);

        static void UnmarkTWListForDelegatedObjectSignalingInProgress(
            CSynchData * pTgtObjectSynchData);

        static PAL_ERROR ThreadNativeWait(
            ThreadNativeWaitData * ptnwdNativeWaitData,
            DWORD dwTimeout,
            ThreadWakeupReason * ptwrWakeupReason,
            DWORD * pdwSignaledObject);

        static void ThreadPrepareForShutdown(void);

#ifndef CORECLR
        static bool GetProcessPipeName(
            LPSTR pDest, 
            int iDestSize,
            DWORD dwPid);
#endif // !CORECLR
        
        //
        // Non-static helper methods
        //
    private:
        LONG DoMonitorProcesses(CPalThread * pthrCurrent);

        void DiscardMonitoredProcesses(CPalThread * pthrCurrent);

        PAL_ERROR ReadCmdFromProcessPipe(
            int iPollTimeout,
            SynchWorkerCmd * pswcWorkerCmd,
            SharedID * pshridMarshaledData,
            DWORD * pdwData);

        PAL_ERROR WakeUpLocalWorkerThread(
            SynchWorkerCmd swcWorkerCmd);

        void DiscardAllPendingAPCs(
            CPalThread * pthrCurrent,
            CPalThread * pthrTarget);

        int ReadBytesFromProcessPipe(
            int iTimeout,
            BYTE * pRecvBuf,
            LONG lBytes);

        bool CreateProcessPipe();

        PAL_ERROR ShutdownProcessPipe();

    public:
        //
        // The following methods must be called only by a Sync*Controller or
        // while holding the required synchronization global locks
        //
        void UnRegisterWait(
            CPalThread * pthrCurrent,
            ThreadWaitInfo * ptwiWaitInfo,
            bool fHaveSharedLock);

        PAL_ERROR RegisterProcessForMonitoring(
            CPalThread * pthrCurrent,
            CSynchData *psdSynchData,
            IPalObject *pProcessObject,
            CProcProcessLocalData * pProcLocalData);

        PAL_ERROR UnRegisterProcessForMonitoring(
            CPalThread * pthrCurrent,
            CSynchData *psdSynchData,
            DWORD dwPid);

        //
        // Utility static methods, no lock required
        //
        static bool HasProcessExited(
            DWORD dwPid,
            DWORD * pdwExitCode,
            bool * pfIsActualExitCode);

        static bool InterlockedAwaken(
            DWORD *pWaitState,
            bool fAlertOnly);

        static PAL_ERROR GetAbsoluteTimeout(
            DWORD dwTimeout,
            struct timespec * ptsAbsTmo,
            BOOL fPreferMonotonicClock);
    };
}

#endif // _SYNCHMANAGER_HPP_
