// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//=========================================================================

//
// ThreadPoolRequest.cpp
//

//
//
//=========================================================================

#include "common.h"
#include "comdelegate.h"
#include "comthreadpool.h"
#include "threadpoolrequest.h"
#include "win32threadpool.h"
#include "class.h"
#include "object.h"
#include "field.h"
#include "excep.h"
#include "eeconfig.h"
#include "corhost.h"
#include "nativeoverlapped.h"
#include "appdomain.inl"

BYTE PerAppDomainTPCountList::s_padding[MAX_CACHE_LINE_SIZE - sizeof(LONG)];
// Make this point to unmanaged TP in case, no appdomains have initialized yet.
// Cacheline aligned, hot variable
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) LONG PerAppDomainTPCountList::s_ADHint = -1;

// Move out of from preceeding variables' cache line
DECLSPEC_ALIGN(MAX_CACHE_LINE_SIZE) UnManagedPerAppDomainTPCount PerAppDomainTPCountList::s_unmanagedTPCount;
//The list of all per-appdomain work-request counts.
ArrayListStatic PerAppDomainTPCountList::s_appDomainIndexList;


//---------------------------------------------------------------------------
//ResetAppDomainIndex: Resets the  AppDomain ID  and the  per-appdomain
//                     thread pool counts
//
//Arguments:
//index - The index into the s_appDomainIndexList for the AppDomain we're
//        trying to clear (the AD being unloaded)
//
//Assumptions:
//This function needs to be called from the AD unload thread after all domain
//bound objects have been finalized when it's safe to recycle  the TPIndex.
//ClearAppDomainRequestsActive can be called from this function because no
// managed code is running (If managed code is running, this function needs
//to be called under a managed per-appdomain lock).
//
void PerAppDomainTPCountList::ResetAppDomainIndex(TPIndex index)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(index.m_dwIndex == TPIndex().m_dwIndex);
}

FORCEINLINE void ReleaseWorkRequest(WorkRequest *workRequest) { ThreadpoolMgr::RecycleMemory( workRequest, ThreadpoolMgr::MEMTYPE_WorkRequest ); }
typedef Wrapper< WorkRequest *, DoNothing<WorkRequest *>, ReleaseWorkRequest > WorkRequestHolder;
