//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//=========================================================================

//
// ThreadPoolRequest.h
//

//
// This file contains definitions of classes needed to mainain per-appdomain
// thread pool work requests. This is needed as unmanaged and managed work 
// requests are allocted, managed and dispatched in drastically different ways.
// However, the scheduler need be aware of these differences, and it should 
// simply talk to a common interface for managing work request counts.
//
//=========================================================================

#ifndef _THREADPOOL_REQUEST_H
#define _THREADPOOL_REQUEST_H

#define TP_QUANTUM 2
#define UNUSED_THREADPOOL_INDEX (DWORD)-1

//--------------------------------------------------------------------------
//IPerAppDomainTPCount is an interface for implementing per-appdomain thread 
//pool state. It's implementation should include logic to maintain work-counts,
//notify thread pool class when work arrives or no work is left. Finally 
//there is logic to dipatch work items correctly in the right domain.
//
//Notes:
//This class was designed to support both the managed and unmanaged uses
//of thread pool. The unmananged part may directly be used through com 
//interfaces. The differences between the actual management of counts and 
//dispatching of work is quite different between the two. This interface 
//hides these differences to the thread scheduler implemented by the thread pool
//class.
//

class IPerAppDomainTPCount{  
public:
    virtual void ResetState() = 0;
    virtual BOOL IsRequestPending() = 0;

	//This functions marks the begining of requests queued for the domain. 
    //It needs to notify the scheduler of work-arrival among other things.
    virtual void SetAppDomainRequestsActive() = 0;

    //This functions marks the end of requests queued for this domain.
    virtual void ClearAppDomainRequestsActive(BOOL bADU = FALSE) = 0;

    //Clears the "active" flag if it was set, and returns whether it was set.
    virtual bool TakeActiveRequest() = 0;

    //Takes care of dispatching requests in the right domain.
    virtual void DispatchWorkItem(bool* foundWork, bool* wasNotRecalled) = 0;
    virtual void SetAppDomainId(ADID id) = 0;
    virtual void SetTPIndexUnused() = 0;
    virtual BOOL IsTPIndexUnused() = 0;
    virtual void SetTPIndex(TPIndex index) = 0; 
    virtual void SetAppDomainUnloading() = 0;
    virtual void ClearAppDomainUnloading() = 0;
};

typedef DPTR(IPerAppDomainTPCount) PTR_IPerAppDomainTPCount;

static const LONG ADUnloading = -1;

//--------------------------------------------------------------------------
//ManagedPerAppDomainTPCount maintains per-appdomain thread pool state. 
//This class maintains the count of per-appdomain work-items queued by
//ThreadPool.QueueUserWorkItem. It also dispatches threads in the appdomain
//correctly by setting up the right exception handling frames etc.
//
//Note: The counts are not accurate, and neither do they need to be. The
//actual work queue is in managed (implemented in threadpool.cs). This class
//just provides heuristics to the thread pool scheduler, along with 
//synchronization to indicate start/end of requests to the scheduler.
class ManagedPerAppDomainTPCount : public IPerAppDomainTPCount {
public:

    ManagedPerAppDomainTPCount(TPIndex index) {ResetState(); m_index = index;}

    inline void ResetState()
    {
        LIMITED_METHOD_CONTRACT;
        m_numRequestsPending = 0;
        m_id.m_dwId = 0;
    }
    
    inline BOOL IsRequestPending()
    {
        LIMITED_METHOD_CONTRACT;
        return m_numRequestsPending != ADUnloading && m_numRequestsPending > 0;
    }

    void SetAppDomainRequestsActive();
    void ClearAppDomainRequestsActive(BOOL bADU);
    bool TakeActiveRequest();

    inline void SetAppDomainId(ADID id)
    {
        LIMITED_METHOD_CONTRACT;
        //This function should be called during appdomain creation when no managed code
        //has started running yet. That implies, no requests should be pending
        //or dispatched to this structure yet.

        _ASSERTE(m_numRequestsPending != ADUnloading);
        _ASSERTE(m_id.m_dwId == 0);

        m_id = id;
    }

    inline void SetTPIndex(TPIndex index) 
    {
        LIMITED_METHOD_CONTRACT;
        //This function should be called during appdomain creation when no managed code
        //has started running yet. That implies, no requests should be pending
        //or dispatched to this structure yet.

        _ASSERTE(m_numRequestsPending != ADUnloading);
        _ASSERTE(m_id.m_dwId == 0);
        _ASSERTE(m_index.m_dwIndex == UNUSED_THREADPOOL_INDEX);

        m_index = index;
    }
    
    inline BOOL IsTPIndexUnused()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_index.m_dwIndex == UNUSED_THREADPOOL_INDEX)
        {
            //This function is called during appdomain creation, and no new appdomains can be
            //added removed at this time. So, make sure that the per-appdomain structures that 
            //have been cleared(reclaimed) don't have any pending requests to them.

            _ASSERTE(m_numRequestsPending != ADUnloading);
            _ASSERTE(m_id.m_dwId == 0);

            return TRUE;
        }

        return FALSE;
    }

    inline void SetTPIndexUnused()
    {
        WRAPPER_NO_CONTRACT;
        //This function should be called during appdomain unload when all threads have
        //succesfully exited the appdomain. That implies, no requests should be pending
        //or dispatched to this structure.

        _ASSERTE(m_id.m_dwId == 0);

        m_index.m_dwIndex = UNUSED_THREADPOOL_INDEX;
    }

    inline void SetAppDomainUnloading()
    {
        LIMITED_METHOD_CONTRACT;
        m_numRequestsPending = ADUnloading;
    }

    inline void ClearAppDomainUnloading();

    inline BOOL IsAppDomainUnloading()
    {
        return m_numRequestsPending.Load() == ADUnloading;
    }

    void DispatchWorkItem(bool* foundWork, bool* wasNotRecalled);

private:
    Volatile<LONG> m_numRequestsPending;
    ADID m_id;
    TPIndex m_index;
};

//--------------------------------------------------------------------------
//UnManagedPerAppDomainTPCount maintains the thread pool state/counts for 
//unmanaged work requests. From thread pool point of view we treat unmanaged 
//requests as a special "appdomain". This helps in scheduling policies, and
//follow same fairness policies as requests in other appdomains.
class UnManagedPerAppDomainTPCount : public IPerAppDomainTPCount {
public:

    UnManagedPerAppDomainTPCount() 
    {
        LIMITED_METHOD_CONTRACT;
        ResetState();
    }

    inline void InitResources()
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END;

    }    

    inline void CleanupResources()
    {
    }    

    inline void ResetState()
    {
        LIMITED_METHOD_CONTRACT;
        m_NumRequests = 0;
        m_outstandingThreadRequestCount = 0;
    }

    inline BOOL IsRequestPending()
    {
        LIMITED_METHOD_CONTRACT;
        return m_outstandingThreadRequestCount != 0 ? TRUE : FALSE;
    }

    void SetAppDomainRequestsActive();
    
    inline void ClearAppDomainRequestsActive(BOOL bADU)
    {
        LIMITED_METHOD_CONTRACT;
        m_outstandingThreadRequestCount = 0;
    }

    bool TakeActiveRequest();

    inline void SetAppDomainId(ADID id)
    {
    }

    void QueueUnmanagedWorkRequest(LPTHREAD_START_ROUTINE  function, PVOID context);
    PVOID DeQueueUnManagedWorkRequest(bool* lastOne);

    void DispatchWorkItem(bool* foundWork, bool* wasNotRecalled);

    inline void SetTPIndexUnused()
    {        
        WRAPPER_NO_CONTRACT;
	_ASSERT(FALSE);
    }

    inline BOOL IsTPIndexUnused()
    {
        WRAPPER_NO_CONTRACT;
	_ASSERT(FALSE);
        return FALSE; 
    }

    inline void SetTPIndex(TPIndex index) 
    {
        WRAPPER_NO_CONTRACT;
	_ASSERT(FALSE); 
    }   

    inline void SetAppDomainUnloading()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERT(FALSE);        
    }

    inline void ClearAppDomainUnloading()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERT(FALSE);        
    }

private:
    ULONG m_NumRequests;
    Volatile<LONG> m_outstandingThreadRequestCount;
    SpinLock m_lock;
};

//--------------------------------------------------------------------------
//PerAppDomainTPCountList maintains the collection of per-appdomain thread 
//pool states. Per appdomain counts are added to the list during appdomain
//creation inside the sdomain lock. The counts are reset during appdomain
//unload after all the threads have  
//This class maintains the count of per-appdomain work-items queued by
//ThreadPool.QueueUserWorkItem. It also dispatches threads in the appdomain
//correctly by setting up the right exception handling frames etc.
//
//Note: The counts are not accurate, and neither do they need to be. The
//actual work queue is in managed (implemented in threadpool.cs). This class
//just provides heuristics to the thread pool scheduler, along with 
//synchronization to indicate start/end of requests to the scheduler.
class PerAppDomainTPCountList{
public:
    static void InitAppDomainIndexList();    
    static void ResetAppDomainIndex(TPIndex index);
    static void ResetAppDomainTPCounts(TPIndex index);
    static bool AreRequestsPendingInAnyAppDomains();
    static LONG GetAppDomainIndexForThreadpoolDispatch();
    static void SetAppDomainId(TPIndex index, ADID id);
    static TPIndex AddNewTPIndex();
    static void SetAppDomainUnloading(TPIndex index)
    {
        WRAPPER_NO_CONTRACT;
        IPerAppDomainTPCount * pAdCount = dac_cast<PTR_IPerAppDomainTPCount> (s_appDomainIndexList.Get(index.m_dwIndex-1));
        _ASSERTE(pAdCount);
        pAdCount->SetAppDomainUnloading();
    }

    static void ClearAppDomainUnloading(TPIndex index)
    {
        WRAPPER_NO_CONTRACT;
        IPerAppDomainTPCount * pAdCount = dac_cast<PTR_IPerAppDomainTPCount> (s_appDomainIndexList.Get(index.m_dwIndex-1));
        _ASSERTE(pAdCount);
        pAdCount->ClearAppDomainUnloading();
    }

    typedef Holder<TPIndex, SetAppDomainUnloading, ClearAppDomainUnloading> AppDomainUnloadingHolder;
 
    inline static IPerAppDomainTPCount* GetPerAppdomainCount(TPIndex index)
    {
        return dac_cast<PTR_IPerAppDomainTPCount>(s_appDomainIndexList.Get(index.m_dwIndex-1));
    }

    inline static UnManagedPerAppDomainTPCount* GetUnmanagedTPCount()
    {
        return &s_unmanagedTPCount;
    }

private:
    static DWORD FindFirstFreeTpEntry();

    static UnManagedPerAppDomainTPCount s_unmanagedTPCount;

    //The list of all per-appdomain work-request counts.
    static ArrayListStatic s_appDomainIndexList;    

    static LONG s_ADHint;
};

#endif //_THREADPOOL_REQUEST_H

















