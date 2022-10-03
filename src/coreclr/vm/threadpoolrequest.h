// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#include "util.hpp"

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

    //This functions marks the beginning of requests queued for the domain.
    //It needs to notify the scheduler of work-arrival among other things.
    virtual void SetAppDomainRequestsActive() = 0;

    //This functions marks the end of requests queued for this domain.
    virtual void ClearAppDomainRequestsActive() = 0;

    //Clears the "active" flag if it was set, and returns whether it was set.
    virtual bool TakeActiveRequest() = 0;

    //Takes care of dispatching requests in the right domain.
    virtual void DispatchWorkItem(bool* foundWork, bool* wasNotRecalled) = 0;
    virtual void SetTPIndexUnused() = 0;
    virtual BOOL IsTPIndexUnused() = 0;
    virtual void SetTPIndex(TPIndex index) = 0;
};

typedef DPTR(IPerAppDomainTPCount) PTR_IPerAppDomainTPCount;

#ifdef _MSC_VER
// Disable this warning - we intentionally want __declspec(align()) to insert padding for us
#pragma warning(disable: 4324) // structure was padded due to __declspec(align())
#endif

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
        VolatileStore(&m_numRequestsPending, (LONG)0);
    }

    inline BOOL IsRequestPending()
    {
        LIMITED_METHOD_CONTRACT;

        LONG count = VolatileLoad(&m_numRequestsPending);
        return count > 0;
    }

    void SetAppDomainRequestsActive();
    void ClearAppDomainRequestsActive();
    bool TakeActiveRequest();

    inline void SetTPIndex(TPIndex index)
    {
        LIMITED_METHOD_CONTRACT;
        //This function should be called during appdomain creation when no managed code
        //has started running yet. That implies, no requests should be pending
        //or dispatched to this structure yet.

        _ASSERTE(m_index.m_dwIndex == UNUSED_THREADPOOL_INDEX);

        m_index = index;
    }

    inline BOOL IsTPIndexUnused()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_index.m_dwIndex == UNUSED_THREADPOOL_INDEX)
        {
            return TRUE;
        }

        return FALSE;
    }

    inline void SetTPIndexUnused()
    {
        WRAPPER_NO_CONTRACT;
        m_index.m_dwIndex = UNUSED_THREADPOOL_INDEX;
    }

    void DispatchWorkItem(bool* foundWork, bool* wasNotRecalled);

private:
    TPIndex m_index;
    struct DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) {
        BYTE m_padding1[MAX_CACHE_LINE_SIZE - sizeof(LONG)];
        // Only use with VolatileLoad+VolatileStore+InterlockedCompareExchange
        LONG m_numRequestsPending;
        BYTE m_padding2[MAX_CACHE_LINE_SIZE];
    };
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

    inline void ResetState()
    {
        LIMITED_METHOD_CONTRACT;
        m_NumRequests = 0;
        VolatileStore(&m_outstandingThreadRequestCount, (LONG)0);
    }

    inline BOOL IsRequestPending()
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoad(&m_outstandingThreadRequestCount) != (LONG)0 ? TRUE : FALSE;
    }

    void SetAppDomainRequestsActive();

    inline void ClearAppDomainRequestsActive()
    {
        LIMITED_METHOD_CONTRACT;
        VolatileStore(&m_outstandingThreadRequestCount, (LONG)0);
    }

    bool TakeActiveRequest();

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

    inline ULONG GetNumRequests()
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoad(&m_NumRequests);
    }

private:
    SpinLock m_lock;
    ULONG m_NumRequests;
    struct DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) {
        BYTE m_padding1[MAX_CACHE_LINE_SIZE - sizeof(LONG)];
        // Only use with VolatileLoad+VolatileStore+InterlockedCompareExchange
        LONG m_outstandingThreadRequestCount;
        BYTE m_padding2[MAX_CACHE_LINE_SIZE];
    };
};

#ifdef _MSC_VER
#pragma warning(default: 4324)  // structure was padded due to __declspec(align())
#endif

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
    static bool AreRequestsPendingInAnyAppDomains();
    static LONG GetAppDomainIndexForThreadpoolDispatch();
    static TPIndex AddNewTPIndex();

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

    static BYTE s_padding[MAX_CACHE_LINE_SIZE - sizeof(LONG)];
    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static LONG s_ADHint;
    DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) static UnManagedPerAppDomainTPCount s_unmanagedTPCount;

    //The list of all per-appdomain work-request counts.
    static ArrayListStatic s_appDomainIndexList;
};

#endif //_THREADPOOL_REQUEST_H
