//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: request.cpp
// 

//
// CorDataAccess::Request implementation.
//
//*****************************************************************************

#include "stdafx.h"
#include <win32threadpool.h>

#include <gceewks.cpp>
#include <handletablepriv.h>
#include "typestring.h"
#include <gccover.h>
#include <virtualcallstub.h>
#ifdef FEATURE_COMINTEROP
#include <comcallablewrapper.h>
#endif // FEATURE_COMINTEROP

#ifndef FEATURE_PAL
// It is unfortunate having to include this header just to get the definition of GenericModeBlock
#include <msodw.h>
#endif // FEATURE_PAL

// To include definiton of IsThrowableThreadAbortException
#include <exstatecommon.h>

#include "rejit.h"


// GC headers define these to EE-specific stuff that we don't want.
#undef EnterCriticalSection
#undef LeaveCriticalSection

#define PTR_CDADDR(ptr)   TO_CDADDR(PTR_TO_TADDR(ptr))
#define HOST_CDADDR(host) TO_CDADDR(PTR_HOST_TO_TADDR(host))

#define SOSDacEnter()   \
    DAC_ENTER();        \
    HRESULT hr = S_OK;  \
    EX_TRY              \
    {

#define SOSDacLeave()   \
    }                   \
    EX_CATCH            \
    {                   \
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &hr)) \
        {               \
            EX_RETHROW; \
        }               \
    }                   \
    EX_END_CATCH(SwallowAllExceptions) \
    DAC_LEAVE();

// Use this when you don't want to instantiate an Object * in the host.
TADDR DACGetMethodTableFromObjectPointer(TADDR objAddr, ICorDebugDataTarget * target)
{
    ULONG32 returned = 0;
    TADDR Value = NULL;

    HRESULT hr = target->ReadVirtual(objAddr, (PBYTE)&Value, sizeof(TADDR), &returned);
    
    if ((hr != S_OK) || (returned != sizeof(TADDR)))
    {
        return NULL;
    }

    Value = Value & ~3; // equivalent to Object::GetGCSafeMethodTable()
    return Value;
}

// Use this when you don't want to instantiate an Object * in the host.
PTR_SyncBlock DACGetSyncBlockFromObjectPointer(TADDR objAddr, ICorDebugDataTarget * target)
{
    ULONG32 returned = 0;
    DWORD Value = NULL;

    HRESULT hr = target->ReadVirtual(objAddr - sizeof(DWORD), (PBYTE)&Value, sizeof(DWORD), &returned);
    
    if ((hr != S_OK) || (returned != sizeof(DWORD)))
    {
        return NULL;
    }

    if ((Value & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) != BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
        return NULL;
    Value &= MASK_SYNCBLOCKINDEX;

    PTR_SyncTableEntry ste = PTR_SyncTableEntry(dac_cast<TADDR>(g_pSyncTable) + (sizeof(SyncTableEntry) * Value));
    return ste->m_SyncBlock;
}

BOOL DacValidateEEClass(EEClass *pEEClass)
{
    // Verify things are right.
    // The EEClass method table pointer should match the method table.
    // TODO: Microsoft, need another test for validity, this one isn't always true anymore.
    BOOL retval = TRUE;
    PAL_CPP_TRY
    {
        MethodTable *pMethodTable = pEEClass->GetMethodTable();
        if (!pMethodTable)
        {
            // PREfix.
            return FALSE;
        }
        if (pEEClass != pMethodTable->GetClass())
        {
            retval = FALSE;
        }
    }
    PAL_CPP_CATCH_ALL
    {
        retval = FALSE; // Something is wrong
    }
    PAL_CPP_ENDTRY
    return retval;

}

BOOL DacValidateMethodTable(MethodTable *pMT, BOOL &bIsFree)
{
    // Verify things are right.
    BOOL retval = FALSE;
    PAL_CPP_TRY
    {
        bIsFree = FALSE;
        EEClass *pEEClass = pMT->GetClass();
        if (pEEClass==NULL)
        {
            // Okay to have a NULL EEClass if this is a free methodtable
            CLRDATA_ADDRESS MethTableAddr = HOST_CDADDR(pMT);
            CLRDATA_ADDRESS FreeObjMethTableAddr = HOST_CDADDR(g_pFreeObjectMethodTable);
            if (MethTableAddr != FreeObjMethTableAddr)
                goto BadMethodTable;

            bIsFree = TRUE;
        }
        else
        {
            // Standard fast check
            if (!pMT->ValidateWithPossibleAV())
                goto BadMethodTable;
 
            // In rare cases, we've seen the standard check above pass when it shouldn't.
            // Insert additional/ad-hoc tests below.

            // Metadata token should look valid for a class
            mdTypeDef td = pMT->GetCl();
            if (td != mdTokenNil && TypeFromToken(td) != mdtTypeDef)
                goto BadMethodTable;

            // BaseSize should always be greater than 0 for valid objects (unless it's an interface)
            // For strings, baseSize is not ptr-aligned
            if (!pMT->IsInterface() && !pMT->IsString())
            {
                if (pMT->GetBaseSize() == 0 || !IS_ALIGNED(pMT->GetBaseSize(), sizeof(void *)))
                    goto BadMethodTable;
            }
        }

        retval = TRUE;

BadMethodTable: ;
    }
    PAL_CPP_CATCH_ALL
    {
        retval = FALSE; // Something is wrong
    }
    PAL_CPP_ENDTRY
    return retval;

}

BOOL DacValidateMD(MethodDesc * pMD)
{
    if (pMD == NULL)
    {
        return FALSE;
    }

    // Verify things are right.
    BOOL retval = TRUE;
    PAL_CPP_TRY
    {
        MethodTable *pMethodTable = pMD->GetMethodTable();

        // Standard fast check
        if (!pMethodTable->ValidateWithPossibleAV())
        {
            retval = FALSE;
        }

        if (retval && (pMD->GetSlot() >= pMethodTable->GetNumVtableSlots() && !pMD->HasNonVtableSlot()))
        {
            retval = FALSE;
        }

        if (retval && pMD->HasTemporaryEntryPoint())
        {
            MethodDesc *pMDCheck = MethodDesc::GetMethodDescFromStubAddr(pMD->GetTemporaryEntryPoint(), TRUE);

            if (PTR_HOST_TO_TADDR(pMD) != PTR_HOST_TO_TADDR(pMDCheck))
            {
                retval = FALSE;
            }
        }

        if (retval && pMD->HasNativeCode())
        {
            PCODE jitCodeAddr = pMD->GetNativeCode();

            MethodDesc *pMDCheck = ExecutionManager::GetCodeMethodDesc(jitCodeAddr);
            if (pMDCheck)
            {
                // Check that the given MethodDesc matches the MethodDesc from
                // the CodeHeader
                if (PTR_HOST_TO_TADDR(pMD) != PTR_HOST_TO_TADDR(pMDCheck))
                {
                    retval = FALSE;
                }
            }
            else
            {
                retval = FALSE;
            }
        }
    }
    PAL_CPP_CATCH_ALL
    {
        retval = FALSE; // Something is wrong
    }
    PAL_CPP_ENDTRY
    return retval;
}

BOOL DacValidateMD(LPCVOID pMD)
{
    return DacValidateMD((MethodDesc *)pMD);
}

VOID GetJITMethodInfo (EECodeInfo * pCodeInfo, JITTypes *pJITType, CLRDATA_ADDRESS *pGCInfo)
{
    DWORD dwType = pCodeInfo->GetJitManager()->GetCodeType();
    if (IsMiIL(dwType))
    {
        *pJITType = TYPE_JIT;
    }
    else if (IsMiNative(dwType))
    {
        *pJITType = TYPE_PJIT;
    }
    else
    {
        *pJITType = TYPE_UNKNOWN;
    }

    *pGCInfo = (CLRDATA_ADDRESS)PTR_TO_TADDR(pCodeInfo->GetGCInfo());
}


HRESULT
ClrDataAccess::GetWorkRequestData(CLRDATA_ADDRESS addr, struct DacpWorkRequestData *workRequestData)
{
    if (addr == 0 || workRequestData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    WorkRequest *pRequest = PTR_WorkRequest(TO_TADDR(addr));
    workRequestData->Function = (TADDR)(pRequest->Function);
    workRequestData->Context = (TADDR)(pRequest->Context);
    workRequestData->NextWorkRequest = (TADDR)(pRequest->next);

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetHillClimbingLogEntry(CLRDATA_ADDRESS addr, struct DacpHillClimbingLogEntry *entry)
{
    if (addr == 0 || entry == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    HillClimbingLogEntry *pLogEntry = PTR_HillClimbingLogEntry(TO_TADDR(addr));
    entry->TickCount = pLogEntry->TickCount;
    entry->NewControlSetting = pLogEntry->NewControlSetting;
    entry->LastHistoryCount = pLogEntry->LastHistoryCount;
    entry->LastHistoryMean = pLogEntry->LastHistoryMean;
    entry->Transition = pLogEntry->Transition;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetThreadpoolData(struct DacpThreadpoolData *threadpoolData)
{
    if (threadpoolData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    threadpoolData->cpuUtilization = ThreadpoolMgr::cpuUtilization;
    threadpoolData->MinLimitTotalWorkerThreads = ThreadpoolMgr::MinLimitTotalWorkerThreads;
    threadpoolData->MaxLimitTotalWorkerThreads = ThreadpoolMgr::MaxLimitTotalWorkerThreads;

    //
    // Read ThreadpoolMgr::WorkerCounter
    //
    TADDR pCounter = DacGetTargetAddrForHostAddr(&ThreadpoolMgr::WorkerCounter,true);
    ThreadpoolMgr::ThreadCounter counter;
    DacReadAll(pCounter,&counter,sizeof(ThreadpoolMgr::ThreadCounter),true);
    ThreadpoolMgr::ThreadCounter::Counts counts = counter.counts;

    threadpoolData->NumWorkingWorkerThreads = counts.NumWorking;
    threadpoolData->NumIdleWorkerThreads = counts.NumActive - counts.NumWorking;
    threadpoolData->NumRetiredWorkerThreads = counts.NumRetired;

    threadpoolData->FirstUnmanagedWorkRequest = HOST_CDADDR(ThreadpoolMgr::WorkRequestHead);

    threadpoolData->HillClimbingLog = dac_cast<TADDR>(&HillClimbingLog);
    threadpoolData->HillClimbingLogFirstIndex = HillClimbingLogFirstIndex;
    threadpoolData->HillClimbingLogSize = HillClimbingLogSize;


    //
    // Read ThreadpoolMgr::CPThreadCounter
    //
    pCounter = DacGetTargetAddrForHostAddr(&ThreadpoolMgr::CPThreadCounter,true);
    DacReadAll(pCounter,&counter,sizeof(ThreadpoolMgr::ThreadCounter),true);
    counts = counter.counts;

    threadpoolData->NumCPThreads = (LONG)(counts.NumActive + counts.NumRetired);
    threadpoolData->NumFreeCPThreads = (LONG)(counts.NumActive - counts.NumWorking);
    threadpoolData->MaxFreeCPThreads  = ThreadpoolMgr::MaxFreeCPThreads;
    threadpoolData->NumRetiredCPThreads = (LONG)(counts.NumRetired);
    threadpoolData->MaxLimitTotalCPThreads = ThreadpoolMgr::MaxLimitTotalCPThreads;
    threadpoolData->CurrentLimitTotalCPThreads = (LONG)(counts.NumActive); //legacy: currently has no meaning
    threadpoolData->MinLimitTotalCPThreads = ThreadpoolMgr::MinLimitTotalCPThreads;

    TADDR pEntry = DacGetTargetAddrForHostAddr(&ThreadpoolMgr::TimerQueue,true);
    ThreadpoolMgr::LIST_ENTRY entry;
    DacReadAll(pEntry,&entry,sizeof(ThreadpoolMgr::LIST_ENTRY),true);
    TADDR node = (TADDR) entry.Flink;
    threadpoolData->NumTimers = 0;
    while (node && node != pEntry)
    {
        threadpoolData->NumTimers++;
        DacReadAll(node,&entry,sizeof(ThreadpoolMgr::LIST_ENTRY),true);
        node = (TADDR) entry.Flink;
    }
    
    threadpoolData->AsyncTimerCallbackCompletionFPtr = (CLRDATA_ADDRESS) GFN_TADDR(ThreadpoolMgr__AsyncTimerCallbackCompletion);
    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetThreadStoreData(struct DacpThreadStoreData *threadStoreData)
{
    SOSDacEnter();
    
    ThreadStore* threadStore = ThreadStore::s_pThreadStore;
    if (!threadStore)
    {
        hr = E_UNEXPECTED;
    }
    else
    {
        // initialize the fields of our local structure 
        threadStoreData->threadCount = threadStore->m_ThreadCount;
        threadStoreData->unstartedThreadCount = threadStore->m_UnstartedThreadCount;
        threadStoreData->backgroundThreadCount = threadStore->m_BackgroundThreadCount;
        threadStoreData->pendingThreadCount = threadStore->m_PendingThreadCount;
        threadStoreData->deadThreadCount = threadStore->m_DeadThreadCount;
        threadStoreData->fHostConfig = g_fHostConfig;

        // identify the "important" threads
        threadStoreData->firstThread = HOST_CDADDR(threadStore->m_ThreadList.GetHead());
        threadStoreData->finalizerThread = HOST_CDADDR(g_pFinalizerThread);
        threadStoreData->gcThread = HOST_CDADDR(g_pSuspensionThread);
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetStressLogAddress(CLRDATA_ADDRESS *stressLog)
{
    if (stressLog == NULL)
        return E_INVALIDARG;

#ifdef STRESS_LOG
    SOSDacEnter();
    if (g_pStressLog.IsValid())
        *stressLog = HOST_CDADDR(g_pStressLog);
    else
        hr = E_FAIL;

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif // STRESS_LOG
}

HRESULT
ClrDataAccess::GetJitManagerList(unsigned int count, struct DacpJitManagerInfo managers[], unsigned int *pNeeded)
{
    SOSDacEnter();

    if (managers)
    {
        if (count >= 1)
        {
            EEJitManager * managerPtr = ExecutionManager::GetEEJitManager();

            DacpJitManagerInfo *currentPtr = &managers[0];
            currentPtr->managerAddr = HOST_CDADDR(managerPtr);
            currentPtr->codeType = managerPtr->GetCodeType();

            EEJitManager *eeJitManager = PTR_EEJitManager(PTR_HOST_TO_TADDR(managerPtr));
            currentPtr->ptrHeapList = HOST_CDADDR(eeJitManager->m_pCodeHeap);
        }
#ifdef FEATURE_PREJIT
        if (count >= 2)
        {
            NativeImageJitManager * managerPtr = ExecutionManager::GetNativeImageJitManager();
            DacpJitManagerInfo *currentPtr = &managers[1];
            currentPtr->managerAddr = HOST_CDADDR(managerPtr);
            currentPtr->codeType = managerPtr->GetCodeType();
        }
#endif
    }
    else if (pNeeded)
    {
        *pNeeded = 1;
#ifdef FEATURE_PREJIT
        (*pNeeded)++;
#endif
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableSlot(CLRDATA_ADDRESS mt, unsigned int slot, CLRDATA_ADDRESS *value)
{
    if (mt == 0 || value == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodTable* mTable = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!DacValidateMethodTable(mTable, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else if (slot < mTable->GetNumVtableSlots())
    {
        // Now get the slot:
        *value = mTable->GetRestoredSlot(slot);
    }
    else
    {
        hr = E_INVALIDARG;
        MethodTable::IntroducedMethodIterator it(mTable);
        for (; it.IsValid() && FAILED(hr); it.Next())
        {
            MethodDesc * pMD = it.GetMethodDesc();
            if (pMD->GetSlot() == slot)
            {
                *value = pMD->GetMethodEntryPoint();
                hr = S_OK;
            }
        }
    }

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetCodeHeapList(CLRDATA_ADDRESS jitManager, unsigned int count, struct DacpJitCodeHeapInfo codeHeaps[], unsigned int *pNeeded)
{
    if (jitManager == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    EEJitManager *pJitManager = PTR_EEJitManager(TO_TADDR(jitManager));
    HeapList *heapList = pJitManager->m_pCodeHeap;
    
    if (codeHeaps)
    {
        unsigned int i = 0;
        while ((heapList != NULL) && (i < count))
        {
            // What type of CodeHeap pointer do we have?
            CodeHeap *codeHeap = heapList->pHeap;
            TADDR ourVTablePtr = VPTR_HOST_VTABLE_TO_TADDR(*(LPVOID*)codeHeap);
            if (ourVTablePtr == LoaderCodeHeap::VPtrTargetVTable())
            {
                LoaderCodeHeap *loaderCodeHeap = PTR_LoaderCodeHeap(PTR_HOST_TO_TADDR(codeHeap));
                codeHeaps[i].codeHeapType = CODEHEAP_LOADER;
                codeHeaps[i].LoaderHeap = 
                    TO_CDADDR(PTR_HOST_MEMBER_TADDR(LoaderCodeHeap, loaderCodeHeap, m_LoaderHeap));
            }
            else if (ourVTablePtr == HostCodeHeap::VPtrTargetVTable())
            {
                HostCodeHeap *hostCodeHeap = PTR_HostCodeHeap(PTR_HOST_TO_TADDR(codeHeap));
                codeHeaps[i].codeHeapType = CODEHEAP_HOST;
                codeHeaps[i].HostData.baseAddr = PTR_CDADDR(hostCodeHeap->m_pBaseAddr);
                codeHeaps[i].HostData.currentAddr = PTR_CDADDR(hostCodeHeap->m_pLastAvailableCommittedAddr);
            }
            else
            {
                codeHeaps[i].codeHeapType = CODEHEAP_UNKNOWN;
            }
            heapList = heapList->hpNext;
            i++;
        }

        if (pNeeded)
            *pNeeded = i;
    }
    else if (pNeeded)
    {
        int i = 0;
        while (heapList != NULL)
        {
            heapList = heapList->hpNext;
            i++;
        }

        *pNeeded = i;
    }
    else
    {
        hr = E_INVALIDARG;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetStackLimits(CLRDATA_ADDRESS threadPtr, CLRDATA_ADDRESS *lower, 
                              CLRDATA_ADDRESS *upper, CLRDATA_ADDRESS *fp)
{
    if (threadPtr == 0 || (lower == NULL && upper == NULL && fp == NULL))
        return E_INVALIDARG;
    
    SOSDacEnter();
    
    Thread * thread = PTR_Thread(TO_TADDR(threadPtr));
    
    if (lower)
        *lower = TO_CDADDR(thread->GetCachedStackBase().GetAddr());
        
    if (upper)
        *upper = TO_CDADDR(thread->GetCachedStackLimit().GetAddr());
        
    if (fp)
        *fp = PTR_HOST_MEMBER_TADDR(Thread, thread, m_pFrame);
    
    SOSDacLeave();
    
    return hr;
}

HRESULT
ClrDataAccess::GetRegisterName(int regNum, unsigned int count, __out_z __inout_ecount(count) wchar_t *buffer, unsigned int *pNeeded)
{
    if (!buffer && !pNeeded)
        return E_POINTER;

#ifdef _TARGET_AMD64_
    static const wchar_t *regs[] = 
    {
        W("rax"), W("rcx"), W("rdx"), W("rbx"), W("rsp"), W("rbp"), W("rsi"), W("rdi"),
        W("r8"), W("r9"), W("r10"), W("r11"), W("r12"), W("r13"), W("r14"), W("r15"),
    };
#elif defined(_TARGET_ARM_)
    static const wchar_t *regs[] = 
    {
        W("r0"),
        W("r1"),
        W("r2"),
        W("r3"),
        W("r4"),
        W("r5"), 
        W("r6"),
        W("r7"),
        W("r8"), W("r9"), W("r10"), W("r11"), W("r12"), W("sp"), W("lr")
    };
#elif defined(_TARGET_ARM64_)
    static const wchar_t *regs[] = 
    {
        W("X0"),
        W("X1"),
        W("X2"),
        W("X3"),
        W("X4"),
        W("X5"), 
        W("X6"),
        W("X7"),
        W("X8"),  W("X9"),  W("X10"), W("X11"), W("X12"), W("X13"), W("X14"), W("X15"), W("X16"), W("X17"), 
        W("X18"), W("X19"), W("X20"), W("X21"), W("X22"), W("X23"), W("X24"), W("X25"), W("X26"), W("X27"), 
        W("X28"), W("Fp"),  W("Sp"),  W("Lr")
    };
#elif defined(_TARGET_X86_)
    static const wchar_t *regs[] = 
    {
        W("eax"), W("ecx"), W("edx"), W("ebx"), W("esp"), W("ebp"), W("esi"), W("edi"),
    };
#endif

    // Caller frame registers are encoded as "-(reg+1)".
    bool callerFrame = regNum < 0;
    if (callerFrame)
        regNum = -regNum-1;
    
    if ((unsigned int)regNum >= _countof(regs))
        return E_UNEXPECTED;
    
    
    const wchar_t caller[] = W("caller.");
    unsigned int needed = (callerFrame?(unsigned int)wcslen(caller):0) + (unsigned int)wcslen(regs[regNum]) + 1;
    if (pNeeded)
        *pNeeded = needed;
    
    if (buffer)
    {
        _snwprintf_s(buffer, count, _TRUNCATE, W("%s%s"), callerFrame ? caller : W(""), regs[regNum]);
        if (count < needed)
            return S_FALSE;
    }
    
    return S_OK;
}

HRESULT 
ClrDataAccess::GetStackReferences(DWORD osThreadID, ISOSStackRefEnum **ppEnum)
{
    if (ppEnum == NULL)
        return E_POINTER;
    
    SOSDacEnter();
    
    DacStackReferenceWalker *walker = new (nothrow) DacStackReferenceWalker(this, osThreadID);
    
    if (walker == NULL)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        hr = walker->Init();
        
        if (SUCCEEDED(hr))
            hr = walker->QueryInterface(__uuidof(ISOSStackRefEnum), (void**)ppEnum);
        
        if (FAILED(hr))
        {
            delete walker;
            *ppEnum = NULL;
        }
    }
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetThreadFromThinlockID(UINT thinLockId, CLRDATA_ADDRESS *pThread)
{
    if (pThread == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Thread *thread = g_pThinLockThreadIdDispenser->IdToThread(thinLockId);
    *pThread = PTR_HOST_TO_TADDR(thread);

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetThreadAllocData(CLRDATA_ADDRESS addr, struct DacpAllocData *data)
{
    if (data == NULL)
        return E_POINTER;
        
    SOSDacEnter();
    
    Thread* thread = PTR_Thread(TO_TADDR(addr));
    
    data->allocBytes = TO_CDADDR(thread->m_alloc_context.alloc_bytes);
    data->allocBytesLoh = TO_CDADDR(thread->m_alloc_context.alloc_bytes_loh);
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetHeapAllocData(unsigned int count, struct DacpGenerationAllocData *data, unsigned int *pNeeded)
{
    if (data == 0 && pNeeded == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
#if defined(FEATURE_SVR_GC)
    if (GCHeap::IsServerHeap())
    {
        hr = GetServerAllocData(count, data, pNeeded);
    }
    else
#endif //FEATURE_SVR_GC
    {
        if (pNeeded)
            *pNeeded = 1;
    
        if (data && count >= 1)
        {
            for (int i=0;i<NUMBERGENERATIONS;i++)
            {
                data[0].allocData[i].allocBytes = (CLRDATA_ADDRESS)(ULONG_PTR) WKS::generation_table[i].allocation_context.alloc_bytes;
                data[0].allocData[i].allocBytesLoh = (CLRDATA_ADDRESS)(ULONG_PTR) WKS::generation_table[i].allocation_context.alloc_bytes_loh;
            }
        }
    }
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetThreadData(CLRDATA_ADDRESS threadAddr, struct DacpThreadData *threadData)
{
    SOSDacEnter();

    // marshal the Thread object from the target
    Thread* thread = PTR_Thread(TO_TADDR(threadAddr));

    // initialize our local copy from the marshaled target Thread instance
    ZeroMemory (threadData, sizeof(DacpThreadData));
    threadData->corThreadId = thread->m_ThreadId;
    threadData->osThreadId = thread->m_OSThreadId;
    threadData->state = thread->m_State;
    threadData->preemptiveGCDisabled = thread->m_fPreemptiveGCDisabled;
    threadData->allocContextPtr = TO_CDADDR(thread->m_alloc_context.alloc_ptr);
    threadData->allocContextLimit = TO_CDADDR(thread->m_alloc_context.alloc_limit);

    // @todo Microsoft: the following assignment is pointless--we're just getting the
    // target address of the m_pFiberData field of the Thread instance. Then we're going to
    // compute it again as the argument to ReadVirtual. Ultimately, we want the value of 
    // that field, not its address. We already have that value as part of thread (the 
    // marshaled Thread instance).This should just go away and we should simply have:
    // threadData->fiberData = TO_CDADDR(thread->m_pFiberData );
    // instead of the next 11 lines. 
    threadData->fiberData = (CLRDATA_ADDRESS)PTR_HOST_MEMBER_TADDR(Thread, thread, m_pFiberData);

    ULONG32 returned = 0;
    TADDR Value = NULL;
    HRESULT hr = m_pTarget->ReadVirtual(PTR_HOST_MEMBER_TADDR(Thread, thread, m_pFiberData),
                                        (PBYTE)&Value,
                                        sizeof(TADDR),
                                        &returned);
    
    if ((hr  == S_OK) && (returned == sizeof(TADDR)))
    {
        threadData->fiberData = (CLRDATA_ADDRESS) Value;
    }

    threadData->pFrame = PTR_CDADDR(thread->m_pFrame);
    threadData->context = PTR_CDADDR(thread->m_Context);
    threadData->domain = PTR_CDADDR(thread->m_pDomain);
    threadData->lockCount = thread->m_dwLockCount;
#ifndef FEATURE_PAL
    threadData->teb = TO_CDADDR(thread->m_pTEB);
#else
    threadData->teb = NULL;
#endif
    threadData->lastThrownObjectHandle =
        TO_CDADDR(thread->m_LastThrownObjectHandle);
    threadData->nextThread =
        HOST_CDADDR(ThreadStore::s_pThreadStore->m_ThreadList.GetNext(thread));
#ifdef WIN64EXCEPTIONS
    if (thread->m_ExceptionState.m_pCurrentTracker)
    {
        threadData->firstNestedException = PTR_HOST_TO_TADDR(
            thread->m_ExceptionState.m_pCurrentTracker->m_pPrevNestedInfo);
    }
#else
    threadData->firstNestedException = PTR_HOST_TO_TADDR(
        thread->m_ExceptionState.m_currentExInfo.m_pPrevNestedInfo);
#endif // _WIN64

    SOSDacLeave();
    return hr;
}

#ifdef FEATURE_REJIT
void CopyReJitInfoToReJitData(ReJitInfo * pReJitInfo, DacpReJitData * pReJitData)
{
    pReJitData->rejitID = pReJitInfo->m_pShared->GetId();
    pReJitData->NativeCodeAddr = pReJitInfo->m_pCode;

    switch (pReJitInfo->m_pShared->GetState())
    {
    default:
        _ASSERTE(!"Unknown SharedRejitInfo state.  DAC should be updated to understand this new state.");
        pReJitData->flags = DacpReJitData::kUnknown;
        break;

    case SharedReJitInfo::kStateRequested:
        pReJitData->flags = DacpReJitData::kRequested;
        break;

    case SharedReJitInfo::kStateActive:
        pReJitData->flags = DacpReJitData::kActive;
        break;

    case SharedReJitInfo::kStateReverted:
        pReJitData->flags = DacpReJitData::kReverted;
        break;
    }
}
#endif // FEATURE_REJIT



//---------------------------------------------------------------------------------------
//
// Given a method desc addr, this loads up DacpMethodDescData and multiple DacpReJitDatas
// with data on that method
//
// Arguments:
//      * methodDesc - MD to look up
//      * ip - IP address of interest (e.g., from an !ip2md call). This is used to ensure
//          the rejitted version corresponding to this IP is returned. May be NULL if you
//          don't care.
//      * methodDescData - [out] DacpMethodDescData to populate
//      * cRevertedRejitVersions - Number of entries allocated in rgRevertedRejitData
//          array
//      * rgRevertedRejitData - [out] Array of DacpReJitDatas to populate with rejitted
//          rejit version data
//      * pcNeededRevertedRejitData - [out] If cRevertedRejitVersions==0, the total
//          number of available rejit versions (including the current version) is
//          returned here. Else, the number of reverted rejit data actually fetched is
//          returned here.
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT ClrDataAccess::GetMethodDescData(
    CLRDATA_ADDRESS methodDesc, 
    CLRDATA_ADDRESS ip, 
    struct DacpMethodDescData *methodDescData, 
    ULONG cRevertedRejitVersions, 
    DacpReJitData * rgRevertedRejitData,
    ULONG * pcNeededRevertedRejitData)
{
    if (methodDesc == 0)
        return E_INVALIDARG;

    if ((cRevertedRejitVersions != 0) && (rgRevertedRejitData == NULL))
    {
        return E_INVALIDARG;
    }

    if ((rgRevertedRejitData != NULL) && (pcNeededRevertedRejitData == NULL))
    {
        // If you're asking for reverted rejit data, you'd better ask for the number of
        // elements we return
        return E_INVALIDARG;
    }

    SOSDacEnter();

    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));

    if (!DacValidateMD(pMD))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(methodDescData,sizeof(DacpMethodDescData));
        if (rgRevertedRejitData != NULL)
            ZeroMemory(rgRevertedRejitData, sizeof(*rgRevertedRejitData)*cRevertedRejitVersions);
        if (pcNeededRevertedRejitData != NULL)
            *pcNeededRevertedRejitData = 0;
    
        methodDescData->requestedIP = ip;
        methodDescData->bHasNativeCode = pMD->HasNativeCode();
        methodDescData->bIsDynamic = (pMD->IsLCGMethod()) ? TRUE : FALSE;
        methodDescData->wSlotNumber = pMD->GetSlot();
        if (pMD->HasNativeCode())
        {
            methodDescData->NativeCodeAddr = TO_CDADDR(pMD->GetNativeCode());
#ifdef DBG_TARGET_ARM
            methodDescData->NativeCodeAddr &= ~THUMB_CODE;
#endif
        }
        else
        {
            methodDescData->NativeCodeAddr = (CLRDATA_ADDRESS)-1;
        }
        methodDescData->AddressOfNativeCodeSlot = pMD->HasNativeCodeSlot() ?
            TO_CDADDR(pMD->GetAddrOfNativeCodeSlot()) : NULL;
        methodDescData->MDToken = pMD->GetMemberDef();
        methodDescData->MethodDescPtr = methodDesc;
        methodDescData->MethodTablePtr = HOST_CDADDR(pMD->GetMethodTable());
        methodDescData->ModulePtr = HOST_CDADDR(pMD->GetModule());

#ifdef FEATURE_REJIT

        // If rejit info is appropriate, get the following:
        //     * ReJitInfo for the current, active version of the method
        //     * ReJitInfo for the requested IP (for !ip2md and !u)
        //     * ReJitInfos for all reverted versions of the method (up to
        //         cRevertedRejitVersions)
        //         
        // Minidumps will not have all this rejit info, and failure to get rejit info
        // should not be fatal.  So enclose all rejit stuff in a try.

        EX_TRY
        {
            ReJitManager * pReJitMgr = pMD->GetReJitManager();

            // Current ReJitInfo
            ReJitInfo * pReJitInfoCurrent = pReJitMgr->FindNonRevertedReJitInfo(pMD);
            if (pReJitInfoCurrent != NULL)
            {
                CopyReJitInfoToReJitData(pReJitInfoCurrent, &methodDescData->rejitDataCurrent);
            }

            // Requested ReJitInfo
            _ASSERTE(methodDescData->rejitDataRequested.rejitID == 0);
            if (methodDescData->requestedIP != NULL)
            {
                ReJitInfo * pReJitInfoRequested = pReJitMgr->FindReJitInfo(
                    pMD, 
                    CLRDATA_ADDRESS_TO_TADDR(methodDescData->requestedIP),
                    NULL    /* reJitId */);

                if (pReJitInfoRequested != NULL)
                {
                    CopyReJitInfoToReJitData(pReJitInfoRequested, &methodDescData->rejitDataRequested);
                }
            }

            // Total number of jitted rejit versions
            ULONG cJittedRejitVersions;
            if (SUCCEEDED(pReJitMgr->GetReJITIDs(pMD, 0 /* cReJitIds */, &cJittedRejitVersions, NULL /* reJitIds */)))
            {
                methodDescData->cJittedRejitVersions = cJittedRejitVersions;
            }

            // Reverted ReJitInfos
            if (rgRevertedRejitData == NULL)
            {
                // No reverted rejit versions will be returned, but maybe caller wants a
                // count of all versions
                if (pcNeededRevertedRejitData != NULL)
                {
                    *pcNeededRevertedRejitData = methodDescData->cJittedRejitVersions;
                }
            }
            else
            {
                // Caller wants some reverted rejit versions.  Gather reverted rejit version data to return
                ULONG cReJitIds;
                StackSArray<ReJITID> reJitIds;

                // Prepare array to populate with rejitids.  "+ 1" because GetReJITIDs
                // returns all available rejitids, including the rejitid for the one non-reverted
                // current version.
                ReJITID * rgReJitIds = reJitIds.OpenRawBuffer(cRevertedRejitVersions + 1);
                if (rgReJitIds != NULL)
                {
                    hr = pReJitMgr->GetReJITIDs(pMD, cRevertedRejitVersions + 1, &cReJitIds, rgReJitIds);
                    if (SUCCEEDED(hr))
                    {
                        // Go through rejitids.  For each reverted one, populate a entry in rgRevertedRejitData
                        reJitIds.CloseRawBuffer(cReJitIds);
                        ULONG iRejitDataReverted = 0;
                        for (COUNT_T i=0; 
                            (i < cReJitIds) && (iRejitDataReverted < cRevertedRejitVersions);
                            i++)
                        {
                            ReJitInfo * pRejitInfo = pReJitMgr->FindReJitInfo(
                                pMD, 
                                NULL /* pCodeStart */,
                                reJitIds[i]);

                            if ((pRejitInfo == NULL) || 
                                (pRejitInfo->m_pShared->GetState() != SharedReJitInfo::kStateReverted))
                            {
                                continue;
                            }

                            CopyReJitInfoToReJitData(pRejitInfo, &rgRevertedRejitData[iRejitDataReverted]);
                            iRejitDataReverted++;
                        }
                        // pcNeededRevertedRejitData != NULL as per condition at top of function (cuz rgRevertedRejitData !=
                        // NULL).
                        *pcNeededRevertedRejitData = iRejitDataReverted;
                    }
                }
            }
        }
        EX_CATCH
        {
            if (pcNeededRevertedRejitData != NULL)
                *pcNeededRevertedRejitData = 0;
        }
        EX_END_CATCH(SwallowAllExceptions)
        hr = S_OK;      // Failure to get rejitids is not fatal
#endif // FEATURE_REJIT

#if defined(HAVE_GCCOVER)
        if (pMD->m_GcCover)
        {
            EX_TRY
            {
                // In certain minidumps, we won't save the gccover information.
                // (it would be unwise to do so, it is heavy and not a customer scenario).
                methodDescData->GCStressCodeCopy = HOST_CDADDR(pMD->m_GcCover) + offsetof(GCCoverageInfo, savedCode);
            }
            EX_CATCH
            {
                methodDescData->GCStressCodeCopy = 0;
            }
            EX_END_CATCH(SwallowAllExceptions)
        }
        else
#endif // HAVE_GCCOVER

        // Set this above Dario since you know how to tell if dynamic
        if (methodDescData->bIsDynamic)
        {
            DynamicMethodDesc *pDynamicMethod = PTR_DynamicMethodDesc(TO_TADDR(methodDesc));
            if (pDynamicMethod)
            {
                LCGMethodResolver *pResolver = pDynamicMethod->GetLCGMethodResolver();
                if (pResolver)
                {
                    OBJECTREF value = pResolver->GetManagedResolver();
                    if (value)
                    {
                        FieldDesc *pField = (&g_Mscorlib)->GetField(FIELD__DYNAMICRESOLVER__DYNAMIC_METHOD);
                        _ASSERTE(pField);
                        value = pField->GetRefValue(value);
                        if (value)
                        {
                            methodDescData->managedDynamicMethodObject = PTR_HOST_TO_TADDR(value);
                        }
                    }
                }
            }            
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescTransparencyData(CLRDATA_ADDRESS methodDesc, struct DacpMethodDescTransparencyData *data)
{
    if (methodDesc == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodDesc *pMD = PTR_MethodDesc(TO_TADDR(methodDesc));
    if (!DacValidateMD(pMD))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(data, sizeof(DacpMethodDescTransparencyData));

        if (pMD->HasCriticalTransparentInfo())
        {
            data->bHasCriticalTransparentInfo = pMD->HasCriticalTransparentInfo();
            data->bIsCritical = pMD->IsCritical();
            data->bIsTreatAsSafe = pMD->IsTreatAsSafe();
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetCodeHeaderData(CLRDATA_ADDRESS ip, struct DacpCodeHeaderData *codeHeaderData)
{
    if (ip == 0 || codeHeaderData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    EECodeInfo codeInfo(TO_TADDR(ip));

    if (!codeInfo.IsValid())
    {
        // We may be able to walk stubs to find a method desc if it's not a jitted method.
        MethodDesc *methodDescI = MethodTable::GetMethodDescForSlotAddress(TO_TADDR(ip));
        if (methodDescI == NULL)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            codeHeaderData->MethodDescPtr = HOST_CDADDR(methodDescI);
            codeHeaderData->JITType = TYPE_UNKNOWN;
            codeHeaderData->GCInfo = NULL;
            codeHeaderData->MethodStart = NULL;
            codeHeaderData->MethodSize = 0;
            codeHeaderData->ColdRegionStart = NULL;
        }
    }
    else
    {
        codeHeaderData->MethodDescPtr = HOST_CDADDR(codeInfo.GetMethodDesc());

        GetJITMethodInfo(&codeInfo, &codeHeaderData->JITType, &codeHeaderData->GCInfo);

        codeHeaderData->MethodStart = 
            (CLRDATA_ADDRESS) codeInfo.GetStartAddress();
        size_t methodSize = codeInfo.GetCodeManager()->GetFunctionSize(codeInfo.GetGCInfo());
        _ASSERTE(FitsIn<DWORD>(methodSize));
        codeHeaderData->MethodSize = static_cast<DWORD>(methodSize);

        IJitManager::MethodRegionInfo methodRegionInfo = {NULL, 0, NULL, 0};
        codeInfo.GetMethodRegionInfo(&methodRegionInfo);

        codeHeaderData->HotRegionSize = (DWORD) methodRegionInfo.hotSize;
        codeHeaderData->ColdRegionSize = (DWORD) methodRegionInfo.coldSize;
        codeHeaderData->ColdRegionStart = (CLRDATA_ADDRESS) methodRegionInfo.coldStartAddress;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescPtrFromFrame(CLRDATA_ADDRESS frameAddr, CLRDATA_ADDRESS * ppMD)
{
    if (frameAddr == 0 || ppMD == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Frame *pFrame = PTR_Frame(TO_TADDR(frameAddr));
    CLRDATA_ADDRESS methodDescAddr = HOST_CDADDR(pFrame->GetFunction());
    if ((methodDescAddr == NULL) || !DacValidateMD(PTR_MethodDesc(TO_TADDR(methodDescAddr))))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        *ppMD = methodDescAddr;
        hr = S_OK;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescPtrFromIP(CLRDATA_ADDRESS ip, CLRDATA_ADDRESS * ppMD)
{
    if (ip == 0 || ppMD == NULL)
        return E_INVALIDARG;
    
    SOSDacEnter();

    EECodeInfo codeInfo(TO_TADDR(ip));

    if (!codeInfo.IsValid())
    {
        hr = E_FAIL;
    }
    else
    {
        CLRDATA_ADDRESS pMD = HOST_CDADDR(codeInfo.GetMethodDesc());
        if ((pMD == NULL) || !DacValidateMD(PTR_MethodDesc(TO_TADDR(pMD))))
        {
            hr = E_INVALIDARG;
        }
        else
        {
            *ppMD = pMD;
            hr = S_OK;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescName(CLRDATA_ADDRESS methodDesc, unsigned int count, __out_z __inout_ecount(count) wchar_t *name, unsigned int *pNeeded)
{
    if (methodDesc == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodDesc* pMD = PTR_MethodDesc(TO_TADDR(methodDesc));
    StackSString str;

    PAL_CPP_TRY
    {
        TypeString::AppendMethodInternal(str, pMD, TypeString::FormatSignature|TypeString::FormatNamespace|TypeString::FormatFullInst);
    }
    PAL_CPP_CATCH_ALL
    {
        hr = E_FAIL;
        if (pMD->IsDynamicMethod())
        {
            if (pMD->IsLCGMethod() || pMD->IsILStub())
            {
                // In heap dumps, trying to format the signature can fail 
                // in certain cases because StoredSigMethodDesc::m_pSig points
                // to the IMAGE_MAPPED layout (in the PEImage::m_pLayouts array).
                // We save only the IMAGE_LOADED layout to the heap dump. Rather
                // than bloat the dump, we just drop the signature in these
                // cases.
                
                str.Clear();
                TypeString::AppendMethodInternal(str, pMD, TypeString::FormatNamespace|TypeString::FormatFullInst);
                hr = S_OK;
            }
        }
        else
        {
#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            if (MdCacheGetEEName(TO_TADDR(methodDesc), str))
            {
                hr = S_OK;
            }
            else
            {
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            str.Clear();
            Module* pModule = pMD->GetModule();
            if (pModule)
            {
                WCHAR path[MAX_PATH];
                COUNT_T nChars = 0;
                if (pModule->GetPath().DacGetUnicode(NumItems(path), path, &nChars) &&
                    nChars > 0 && nChars <= NumItems(path))
                {
                    WCHAR* pFile = path + nChars - 1;
                    while ((pFile >= path) && (*pFile != W('\\')))
                    {
                        pFile--;
                    }
                    pFile++;
                    if (*pFile)
                    {
                        str.Append(pFile);
                        str.Append(W("!Unknown"));
                        hr = S_OK;
                    }
                }
            }
#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            }
#endif
        }
    }
    PAL_CPP_ENDTRY

    if (SUCCEEDED(hr))
    {

        const wchar_t *val = str.GetUnicode();

        if (pNeeded)
            *pNeeded = str.GetCount() + 1;

        if (name && count)
        {
            wcsncpy_s(name, count, val, _TRUNCATE);
            name[count-1] = 0;
        }
    }
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetDomainFromContext(CLRDATA_ADDRESS contextAddr, CLRDATA_ADDRESS *domain)
{
    if (contextAddr == 0 || domain == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Context* context = PTR_Context(TO_TADDR(contextAddr));
    *domain = HOST_CDADDR(context->GetDomain());

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetObjectStringData(CLRDATA_ADDRESS obj, unsigned int count, __out_z __inout_ecount(count) wchar_t *stringData, unsigned int *pNeeded)
{
    if (obj == 0)
        return E_INVALIDARG;

    if ((stringData == 0 || count <= 0) && (pNeeded == NULL))
        return E_INVALIDARG;

    SOSDacEnter();

    TADDR mtTADDR = DACGetMethodTableFromObjectPointer(TO_TADDR(obj), m_pTarget);
    MethodTable *mt = PTR_MethodTable(mtTADDR);
    
    // Object must be a string
    BOOL bFree = FALSE;
    if (!DacValidateMethodTable(mt, bFree))
        hr = E_INVALIDARG;
    else if (HOST_CDADDR(mt) != HOST_CDADDR(g_pStringClass))
        hr = E_INVALIDARG;

    if (SUCCEEDED(hr))
    {
        PTR_StringObject str(TO_TADDR(obj));
        ULONG32 needed = (ULONG32)str->GetStringLength()+1;

        if (stringData && count > 0)
        {
            if (count > needed)
                count = needed;

            TADDR pszStr = TO_TADDR(obj)+offsetof(StringObject, m_Characters);
            hr = m_pTarget->ReadVirtual(pszStr, (PBYTE)stringData, count*sizeof(wchar_t), &needed);
        
            if (SUCCEEDED(hr))
                stringData[count-1] = 0;
            else
                stringData[0] = 0;
        }
        else
        {
            hr = E_INVALIDARG;
        }
        
        if (pNeeded)
            *pNeeded = needed;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetObjectClassName(CLRDATA_ADDRESS obj, unsigned int count, __out_z __inout_ecount(count) wchar_t *className, unsigned int *pNeeded)
{
    if (obj == 0)
        return E_INVALIDARG;
    
    SOSDacEnter();

    // Don't turn the Object into a pointer, it is too costly on
    // scans of the gc heap.
    MethodTable *mt = NULL;
    TADDR mtTADDR = DACGetMethodTableFromObjectPointer(CLRDATA_ADDRESS_TO_TADDR(obj), m_pTarget);
    if (mtTADDR != NULL)
        mt = PTR_MethodTable(mtTADDR);
    else
        hr = E_INVALIDARG;

    BOOL bFree = FALSE;
    if (SUCCEEDED(hr) && !DacValidateMethodTable(mt, bFree))
        hr = E_INVALIDARG;

    if (SUCCEEDED(hr))
    {
        // There is a case where metadata was unloaded and the AppendType call will fail.
        // This is when an AppDomain has been unloaded but not yet collected.
        PEFile *pPEFile = mt->GetModule()->GetFile();
        if (pPEFile->GetNativeImage() == NULL && pPEFile->GetILimage() == NULL)
        {
            if (pNeeded)
                *pNeeded = 16;

            if (className)
                wcsncpy_s(className, count, W("<Unloaded Type>"), _TRUNCATE);
        }
        else
        {
            StackSString s;
            TypeString::AppendType(s, TypeHandle(mt), TypeString::FormatNamespace|TypeString::FormatFullInst);
            const wchar_t *val = s.GetUnicode();

            if (pNeeded)
                *pNeeded = s.GetCount() + 1;

            if (className && count)
            {
                wcsncpy_s(className, count, val, _TRUNCATE);
                className[count-1] = 0;
            }
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescFromToken(CLRDATA_ADDRESS moduleAddr, mdToken token, CLRDATA_ADDRESS *methodDesc)
{
    if (moduleAddr == 0 || methodDesc == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Module* pModule = PTR_Module(TO_TADDR(moduleAddr));
    TypeHandle th;
    switch (TypeFromToken(token))
    {
        case mdtFieldDef:
            *methodDesc = HOST_CDADDR(pModule->LookupFieldDef(token));
            break;
        case mdtMethodDef:
            *methodDesc = HOST_CDADDR(pModule->LookupMethodDef(token));
            break;
        case mdtTypeDef:
            th = pModule->LookupTypeDef(token);
            *methodDesc = th.AsTAddr();
            break;
        case mdtTypeRef:
            th = pModule->LookupTypeRef(token);
            *methodDesc = th.AsTAddr();
            break;
        default:
            hr = E_INVALIDARG;
            break;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::TraverseModuleMap(ModuleMapType mmt, CLRDATA_ADDRESS moduleAddr, MODULEMAPTRAVERSE pCallback, LPVOID token)
{
    if (moduleAddr == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    Module* pModule = PTR_Module(TO_TADDR(moduleAddr));

    // We want to traverse these two tables, passing callback information
    switch (mmt)
    {
        case TYPEDEFTOMETHODTABLE:
            {
                LookupMap<PTR_MethodTable>::Iterator typeIter(&pModule->m_TypeDefToMethodTableMap);
                for (int i = 0; typeIter.Next(); i++)
                {
                    if (typeIter.GetElement())
                    {
                        MethodTable* pMT = typeIter.GetElement();
                        (pCallback)(i,PTR_HOST_TO_TADDR(pMT), token);
                    }
                }
            }
            break;
        case TYPEREFTOMETHODTABLE:
            {
                LookupMap<PTR_TypeRef>::Iterator typeIter(&pModule->m_TypeRefToMethodTableMap);
                for (int i = 0; typeIter.Next(); i++)
                {
                    if (typeIter.GetElement())
                    {
                        MethodTable* pMT = TypeHandle::FromTAddr(dac_cast<TADDR>(typeIter.GetElement())).GetMethodTable();
                        (pCallback)(i,PTR_HOST_TO_TADDR(pMT), token);
                    }
                }
            }
            break;
        default:
            hr = E_INVALIDARG;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetModule(CLRDATA_ADDRESS addr, IXCLRDataModule **mod)
{
    if (addr == 0 || mod == NULL)
        return E_INVALIDARG;
    
    SOSDacEnter();
    
    Module* pModule = PTR_Module(TO_TADDR(addr));
    *mod = new ClrDataModule(this, pModule);
    SOSDacLeave();
    
    return hr;
}

HRESULT
ClrDataAccess::GetModuleData(CLRDATA_ADDRESS addr, struct DacpModuleData *ModuleData)
{
    if (addr == 0 || ModuleData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Module* pModule = PTR_Module(TO_TADDR(addr));

    ZeroMemory(ModuleData,sizeof(DacpModuleData));
    ModuleData->Address = addr;
    ModuleData->File = HOST_CDADDR(pModule->GetFile());
    COUNT_T metadataSize = 0;
    if (pModule->GetFile()->HasNativeImage())
    {
        ModuleData->ilBase = (CLRDATA_ADDRESS)PTR_TO_TADDR(pModule->GetFile()->GetLoadedNative()->GetBase());
    }
    else 
    if (!pModule->GetFile()->IsDynamic())
    {
        ModuleData->ilBase = (CLRDATA_ADDRESS)(ULONG_PTR) pModule->GetFile()->GetIJWBase();
    }

    ModuleData->metadataStart = (CLRDATA_ADDRESS)dac_cast<TADDR>(pModule->GetFile()->GetLoadedMetadata(&metadataSize));
    ModuleData->metadataSize = (SIZE_T) metadataSize;

    ModuleData->bIsReflection = pModule->IsReflection();
    ModuleData->bIsPEFile = pModule->IsPEFile();
    ModuleData->Assembly = HOST_CDADDR(pModule->GetAssembly());
    ModuleData->dwModuleID = pModule->GetModuleID();
    ModuleData->dwModuleIndex = pModule->GetModuleIndex().m_dwIndex;
    ModuleData->dwTransientFlags = pModule->m_dwTransientFlags;

    EX_TRY
    {
        //
        // In minidump's case, these data structure is not avaiable.
        //
        ModuleData->TypeDefToMethodTableMap = PTR_CDADDR(pModule->m_TypeDefToMethodTableMap.pTable);
        ModuleData->TypeRefToMethodTableMap = PTR_CDADDR(pModule->m_TypeRefToMethodTableMap.pTable);
        ModuleData->MethodDefToDescMap = PTR_CDADDR(pModule->m_MethodDefToDescMap.pTable);
        ModuleData->FieldDefToDescMap = PTR_CDADDR(pModule->m_FieldDefToDescMap.pTable);
        ModuleData->MemberRefToDescMap = NULL;
        ModuleData->FileReferencesMap = PTR_CDADDR(pModule->m_FileReferencesMap.pTable);
        ModuleData->ManifestModuleReferencesMap = PTR_CDADDR(pModule->m_ManifestModuleReferencesMap.pTable);

#ifdef FEATURE_MIXEDMODE // IJW
        ModuleData->pThunkHeap = HOST_CDADDR(pModule->m_pThunkHeap);
#endif // FEATURE_MIXEDMODE // IJW
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetILForModule(CLRDATA_ADDRESS moduleAddr, DWORD rva, CLRDATA_ADDRESS *il)
{
    if (moduleAddr == 0 || il == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Module* pModule = PTR_Module(TO_TADDR(moduleAddr));
    *il = (TADDR)(CLRDATA_ADDRESS)pModule->GetIL(rva);

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableData(CLRDATA_ADDRESS mt, struct DacpMethodTableData *MTData)
{
    if (mt == 0 || MTData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodTable* pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!DacValidateMethodTable(pMT, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(MTData,sizeof(DacpMethodTableData));
        MTData->BaseSize = pMT->GetBaseSize();
        if(pMT->IsString())
            MTData->BaseSize -= sizeof(WCHAR);
        MTData->ComponentSize = (DWORD)pMT->GetComponentSize();
        MTData->bIsFree = bIsFree;
        if(!bIsFree)
        {
            MTData->Module = HOST_CDADDR(pMT->GetModule());
            MTData->Class = HOST_CDADDR(pMT->GetClass());
            MTData->ParentMethodTable = HOST_CDADDR(pMT->GetParentMethodTable());;
            MTData->wNumInterfaces = pMT->GetNumInterfaces();
            MTData->wNumMethods = pMT->GetNumMethods();
            MTData->wNumVtableSlots = pMT->GetNumVtableSlots();
            MTData->wNumVirtuals = pMT->GetNumVirtuals();
            MTData->cl = pMT->GetCl();
            MTData->dwAttrClass = pMT->GetAttrClass();
            MTData->bContainsPointers = pMT->ContainsPointers();
            MTData->bIsShared = (pMT->IsDomainNeutral() ? TRUE : FALSE); // flags & enum_flag_DomainNeutral
            MTData->bIsDynamic = pMT->IsDynamicStatics();
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableName(CLRDATA_ADDRESS mt, unsigned int count, __out_z __inout_ecount(count) wchar_t *mtName, unsigned int *pNeeded)
{
    if (mt == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodTable *pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL free = FALSE;
    
    if (mt == HOST_CDADDR(g_pFreeObjectMethodTable))
    {
        if (pNeeded)
            *pNeeded = 5;

        if (mtName && count)
            wcsncpy_s(mtName, count, W("Free"), _TRUNCATE);
    }
    else if (!DacValidateMethodTable(pMT, free))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        // There is a case where metadata was unloaded and the AppendType call will fail.
        // This is when an AppDomain has been unloaded but not yet collected.
        PEFile *pPEFile = pMT->GetModule()->GetFile();
        if (pPEFile->GetNativeImage() == NULL && pPEFile->GetILimage() == NULL)
        {
            if (pNeeded)
                *pNeeded = 16;

            if (mtName)
                wcsncpy_s(mtName, count, W("<Unloaded Type>"), _TRUNCATE);
        }
        else
        {
            StackSString s;
#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            PAL_CPP_TRY
            {
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

                TypeString::AppendType(s, TypeHandle(pMT), TypeString::FormatNamespace|TypeString::FormatFullInst);

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            }
            PAL_CPP_CATCH_ALL
            {
                if (!MdCacheGetEEName(dac_cast<TADDR>(pMT), s))
                {
                    PAL_CPP_RETHROW;
                }
            }
            PAL_CPP_ENDTRY
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

            if (s.IsEmpty())
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                const wchar_t *val = s.GetUnicode();

                if (pNeeded)
                    *pNeeded = s.GetCount() + 1;

                if (mtName && count)
                {
                    wcsncpy_s(mtName, count, val, _TRUNCATE);
                    mtName[count-1] = 0;
                }
            }
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFieldDescData(CLRDATA_ADDRESS addr, struct DacpFieldDescData *FieldDescData)
{
    if (addr == 0 || FieldDescData == NULL)
        return E_INVALIDARG;
    
    SOSDacEnter();
    FieldDesc* pFieldDesc = PTR_FieldDesc(TO_TADDR(addr));
    FieldDescData->Type = pFieldDesc->GetFieldType();
    FieldDescData->sigType = FieldDescData->Type;

    EX_TRY
    {
        // minidump case, we do not have the field's type's type handle!
        // Strike should be able to form name based on the metadata token in
        // the field desc. Find type is using look up map which is huge. We cannot
        // drag in this data structure in minidump's case.
        //
        TypeHandle th = pFieldDesc->LookupFieldTypeHandle();
        MethodTable *pMt = th.GetMethodTable();
        if (pMt)
        {
            FieldDescData->MTOfType = HOST_CDADDR(th.GetMethodTable());
        }
        else
        {
            FieldDescData->MTOfType = NULL;
        }
    }
    EX_CATCH
    {
        FieldDescData->MTOfType = NULL;
    }
    EX_END_CATCH(SwallowAllExceptions)

    // TODO: This is not currently useful, I need to get the module of the
    // type definition not that of the field description.

    // TODO: Is there an easier way to get this information?
    // I'm getting the typeDef of a (possibly unloaded) type.
    MetaSig tSig(pFieldDesc);
    tSig.NextArg();
    SigPointer sp1 = tSig.GetArgProps();
    CorElementType et;
    hr = sp1.GetElemType(&et); // throw away the value, we just need to walk past.

    if (SUCCEEDED(hr))
    {
        if (et == ELEMENT_TYPE_CLASS || et == ELEMENT_TYPE_VALUETYPE)   // any other follows token?
        {
            hr = sp1.GetToken(&(FieldDescData->TokenOfType));
        }
        else
        {
            // There is no encoded token of field type
            FieldDescData->TokenOfType = mdTypeDefNil;
            if (FieldDescData->MTOfType == NULL)
            {
                // If there is no encoded token (that is, it is primitive type) and no MethodTable for it, remember the
                // element_type from signature
                //
                FieldDescData->sigType = et;
            }
        }
    }

    FieldDescData->ModuleOfType = HOST_CDADDR(pFieldDesc->GetModule());
    FieldDescData->mb = pFieldDesc->GetMemberDef();
    FieldDescData->MTOfEnclosingClass = HOST_CDADDR(pFieldDesc->GetApproxEnclosingMethodTable());
    FieldDescData->dwOffset = pFieldDesc->GetOffset();
    FieldDescData->bIsThreadLocal = pFieldDesc->IsThreadStatic();
#ifdef FEATURE_REMOTING            
    FieldDescData->bIsContextLocal = pFieldDesc->IsContextStatic();;
#else
    FieldDescData->bIsContextLocal = FALSE;
#endif
    FieldDescData->bIsStatic = pFieldDesc->IsStatic();
    FieldDescData->NextField = HOST_CDADDR(PTR_FieldDesc(PTR_HOST_TO_TADDR(pFieldDesc) + sizeof(FieldDesc)));

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableFieldData(CLRDATA_ADDRESS mt, struct DacpMethodTableFieldData *data)
{
    if (mt == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodTable* pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!pMT || !DacValidateMethodTable(pMT, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        data->wNumInstanceFields = pMT->GetNumInstanceFields();
        data->wNumStaticFields = pMT->GetNumStaticFields();
        data->wNumThreadStaticFields = pMT->GetNumThreadStaticFields();

        data->FirstField = PTR_TO_TADDR(pMT->GetClass()->GetFieldDescList());

#ifdef FEATURE_REMOTING
        BOOL hasContextStatics = pMT->HasContextStatics();
    
        data->wContextStaticsSize = (hasContextStatics) ? pMT->GetContextStaticsSize() : 0;
        _ASSERTE(!hasContextStatics || FitsIn<WORD>(pMT->GetContextStaticsOffset()));
        data->wContextStaticOffset = (hasContextStatics) ? static_cast<WORD>(pMT->GetContextStaticsOffset()) : 0;
#else
        data->wContextStaticsSize = 0;
        data->wContextStaticOffset = 0;
#endif
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableTransparencyData(CLRDATA_ADDRESS mt, struct DacpMethodTableTransparencyData *pTransparencyData)
{
    if (mt == 0 || pTransparencyData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodTable *pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!DacValidateMethodTable(pMT, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(pTransparencyData, sizeof(DacpMethodTableTransparencyData));

        EEClass * pClass = pMT->GetClass();
        if (pClass->HasCriticalTransparentInfo())
        {
            pTransparencyData->bHasCriticalTransparentInfo = pClass->HasCriticalTransparentInfo();
            pTransparencyData->bIsCritical = pClass->IsCritical() || pClass->IsAllCritical();
            pTransparencyData->bIsTreatAsSafe = pClass->IsTreatAsSafe();
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT 
ClrDataAccess::GetMethodTableForEEClass(CLRDATA_ADDRESS eeClass, CLRDATA_ADDRESS *value)
{
    if (eeClass == 0 || value == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    EEClass * pClass = PTR_EEClass(TO_TADDR(eeClass));
    if (!DacValidateEEClass(pClass))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        *value = HOST_CDADDR(pClass->GetMethodTable());
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFrameName(CLRDATA_ADDRESS vtable, unsigned int count, __out_z __inout_ecount(count) wchar_t *frameName, unsigned int *pNeeded)
{
    if (vtable == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    PWSTR pszName = DacGetVtNameW(CLRDATA_ADDRESS_TO_TADDR(vtable));
    if (pszName == NULL)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        // Turn from bytes to wide characters
        unsigned int len = (unsigned int)wcslen(pszName);

        if (frameName)
        {
            wcsncpy_s(frameName, count, pszName, _TRUNCATE);
            
            if (pNeeded)
            {
                if (count < len)
                    *pNeeded = count - 1;
                else
                    *pNeeded = len;
            }
        }
        else if (pNeeded)
        {
            *pNeeded = len + 1;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetPEFileName(CLRDATA_ADDRESS addr, unsigned int count, __out_z __inout_ecount(count) wchar_t *fileName, unsigned int *pNeeded)
{
    if (addr == 0 || (fileName == NULL && pNeeded == NULL) || (fileName != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    PEFile* pPEFile = PTR_PEFile(TO_TADDR(addr));

    // Turn from bytes to wide characters
    if (!pPEFile->GetPath().IsEmpty())
    {
        if (!pPEFile->GetPath().DacGetUnicode(count, fileName, pNeeded))
            hr = E_FAIL;
    }
    else if (!pPEFile->IsDynamic())
    {
        PEAssembly *pAssembly = pPEFile->GetAssembly();
        StackSString displayName;
        pAssembly->GetDisplayName(displayName, 0);

        if (displayName.IsEmpty())
        {
            if (fileName)
                fileName[0] = 0;

            if (pNeeded)
                *pNeeded = 1;
        }
        else
        {
            unsigned int len = displayName.GetCount()+1;

            if (fileName)
            {
                wcsncpy_s(fileName, count, displayName.GetUnicode(), _TRUNCATE);

                if (count < len)
                    len = count;
            }

            if (pNeeded)
                *pNeeded = len;
        }
    }
    else
    {
        if (fileName && count)
            fileName[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetPEFileBase(CLRDATA_ADDRESS addr, CLRDATA_ADDRESS *base)
{
    if (addr == 0 || base == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    PEFile* pPEFile = PTR_PEFile(TO_TADDR(addr));

    // More fields later?
    if (pPEFile->HasNativeImage())
        *base = TO_CDADDR(PTR_TO_TADDR(pPEFile->GetLoadedNative()->GetBase()));
    else if (!pPEFile->IsDynamic())
        *base = TO_CDADDR(pPEFile->GetIJWBase());
    else
        *base = NULL;

    SOSDacLeave();
    return hr;
}

DWORD DACGetNumComponents(TADDR addr, ICorDebugDataTarget* target)
{
    // For an object pointer, this attempts to read the number of
    // array components.
    addr+=sizeof(size_t);
    ULONG32 returned = 0;
    DWORD Value = NULL;
    HRESULT hr = target->ReadVirtual(addr, (PBYTE)&Value, sizeof(DWORD), &returned);
    
    if ((hr != S_OK) || (returned != sizeof(DWORD)))
    {
        return 0;
    }
    return Value;
}

HRESULT
ClrDataAccess::GetObjectData(CLRDATA_ADDRESS addr, struct DacpObjectData *objectData)
{
    if (addr == 0 || objectData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    ZeroMemory (objectData, sizeof(DacpObjectData));
    TADDR mtTADDR = DACGetMethodTableFromObjectPointer(CLRDATA_ADDRESS_TO_TADDR(addr),m_pTarget);
    if (mtTADDR==NULL)
        hr = E_INVALIDARG;
    
    BOOL bFree = FALSE;
    MethodTable *mt = NULL;
    if (SUCCEEDED(hr))
    {
        mt = PTR_MethodTable(mtTADDR);
        if (!DacValidateMethodTable(mt, bFree))
            hr = E_INVALIDARG;
    }

    if (SUCCEEDED(hr))
    {
        objectData->MethodTable = HOST_CDADDR(mt);
        objectData->Size = mt->GetBaseSize();
        if (mt->GetComponentSize())
        {
            objectData->Size += (DACGetNumComponents(CLRDATA_ADDRESS_TO_TADDR(addr),m_pTarget) * mt->GetComponentSize());
            objectData->dwComponentSize = mt->GetComponentSize();
        }

        if (bFree)
        {
            objectData->ObjectType = OBJ_FREE;
        }
        else
        {
            if (objectData->MethodTable == HOST_CDADDR(g_pStringClass))
            {
                objectData->ObjectType = OBJ_STRING;
            }
            else if (objectData->MethodTable == HOST_CDADDR(g_pObjectClass))
            {
                objectData->ObjectType = OBJ_OBJECT;
            }
            else if (mt->IsArray())
            {
                objectData->ObjectType = OBJ_ARRAY;

                // For now, go ahead and instantiate array classes.
                // TODO: avoid instantiating even object Arrays in the host.
                // NOTE: This code is carefully written to deal with MethodTable fields
                //       in the array object having the mark bit set (because we may
                //       be in mark phase when this function is called).
                ArrayBase *pArrayObj = PTR_ArrayBase(TO_TADDR(addr));
                objectData->ElementType = mt->GetArrayElementType();

                TypeHandle thElem = mt->GetApproxArrayElementTypeHandle();

                TypeHandle thCur  = thElem;
                while (thCur.IsTypeDesc())
                    thCur = thCur.AsArray()->GetArrayElementTypeHandle();

                TADDR mtCurTADDR = thCur.AsTAddr();
                if (!DacValidateMethodTable(PTR_MethodTable(mtCurTADDR), bFree))
                {
                    hr = E_INVALIDARG;
                }
                else
                {
                    objectData->ElementTypeHandle = (CLRDATA_ADDRESS)(thElem.AsTAddr());
                    objectData->dwRank = mt->GetRank();
                    objectData->dwNumComponents = pArrayObj->GetNumComponents ();
                    objectData->ArrayDataPtr = PTR_CDADDR(pArrayObj->GetDataPtr (TRUE));
                    objectData->ArrayBoundsPtr = HOST_CDADDR(pArrayObj->GetBoundsPtr());
                    objectData->ArrayLowerBoundsPtr = HOST_CDADDR(pArrayObj->GetLowerBoundsPtr());
                }
            }
            else
            {
                objectData->ObjectType = OBJ_OTHER;
            }
        }
    }
    
#ifdef FEATURE_COMINTEROP
    if (SUCCEEDED(hr))
    {
        EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
        {
            PTR_SyncBlock pSyncBlk = DACGetSyncBlockFromObjectPointer(CLRDATA_ADDRESS_TO_TADDR(addr), m_pTarget);
            if (pSyncBlk != NULL)
            {
                // see if we have an RCW and/or CCW associated with this object
                PTR_InteropSyncBlockInfo pInfo = pSyncBlk->GetInteropInfoNoCreate();
                if (pInfo != NULL)
                {
                    objectData->RCW = TO_CDADDR(pInfo->DacGetRawRCW());
                    objectData->CCW = HOST_CDADDR(pInfo->GetCCW());
                }
            }
        }
        EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY;
    }
#endif // FEATURE_COMINTEROP

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::GetAppDomainList(unsigned int count, CLRDATA_ADDRESS values[], unsigned int *fetched)
{
    SOSDacEnter();

    AppDomainIterator ai(FALSE);
    unsigned int i = 0;
    while (ai.Next() && (i < count))
    {
        if (values)
            values[i] = HOST_CDADDR(ai.GetDomain());
        i++;
    }

    if (fetched)
        *fetched = i;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAppDomainStoreData(struct DacpAppDomainStoreData *adsData)
{
    SOSDacEnter();

    adsData->systemDomain = HOST_CDADDR(SystemDomain::System());
    adsData->sharedDomain = HOST_CDADDR(SharedDomain::GetDomain());

    // Get an accurate count of appdomains.
    adsData->DomainCount = 0;
    AppDomainIterator ai(FALSE);
    while (ai.Next())
        adsData->DomainCount++;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAppDomainData(CLRDATA_ADDRESS addr, struct DacpAppDomainData *appdomainData)
{
    SOSDacEnter();

    if (addr == 0)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        PTR_BaseDomain pBaseDomain = PTR_BaseDomain(TO_TADDR(addr));

        ZeroMemory(appdomainData, sizeof(DacpAppDomainData));
        appdomainData->AppDomainPtr = PTR_CDADDR(pBaseDomain);
        PTR_LoaderAllocator pLoaderAllocator = pBaseDomain->GetLoaderAllocator();
        appdomainData->pHighFrequencyHeap = HOST_CDADDR(pLoaderAllocator->GetHighFrequencyHeap());
        appdomainData->pLowFrequencyHeap = HOST_CDADDR(pLoaderAllocator->GetLowFrequencyHeap());
        appdomainData->pStubHeap = HOST_CDADDR(pLoaderAllocator->GetStubHeap());
        appdomainData->appDomainStage = STAGE_OPEN;

        if (pBaseDomain->IsSharedDomain())
        {
    #ifdef FEATURE_LOADER_OPTIMIZATION    
            SharedDomain::SharedAssemblyIterator i;
            while (i.Next())
            {
                appdomainData->AssemblyCount++;
            }
    #endif // FEATURE_LOADER_OPTIMIZATION        
        }
        else if (pBaseDomain->IsAppDomain())
        {
            AppDomain * pAppDomain = pBaseDomain->AsAppDomain();
            appdomainData->DomainLocalBlock = appdomainData->AppDomainPtr +
                offsetof(AppDomain, m_sDomainLocalBlock);
            appdomainData->pDomainLocalModules = PTR_CDADDR(pAppDomain->m_sDomainLocalBlock.m_pModuleSlots);

            appdomainData->dwId = pAppDomain->GetId().m_dwId;
            appdomainData->appDomainStage = (DacpAppDomainDataStage)pAppDomain->m_Stage.Load();
            if (pAppDomain->IsActive())
            {
                // The assembly list is not valid in a closed appdomain.
                AppDomain::AssemblyIterator i = pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(
                    kIncludeLoading | kIncludeLoaded | kIncludeExecution));
                CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
            
                while (i.Next(pDomainAssembly.This()))
                {
                    if (pDomainAssembly->IsLoaded())
                    {
                        appdomainData->AssemblyCount++;
                    }
                }

                AppDomain::FailedAssemblyIterator j = pAppDomain->IterateFailedAssembliesEx();
                while (j.Next())
                {
                    appdomainData->FailedAssemblyCount++;
                }
            }

            // MiniDumpNormal doesn't guarantee to dump the SecurityDescriptor, let it fail.
            EX_TRY
            {
                appdomainData->AppSecDesc = HOST_CDADDR(pAppDomain->GetSecurityDescriptor());
            }
            EX_CATCH
            {
                HRESULT hrExc = GET_EXCEPTION()->GetHR();
                if (hrExc != HRESULT_FROM_WIN32(ERROR_READ_FAULT)
                    && hrExc != CORDBG_E_READVIRTUAL_FAILURE)
                {
                    EX_RETHROW;
                }
            }
            EX_END_CATCH(SwallowAllExceptions)
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyData(CLRDATA_ADDRESS assembly, unsigned int *pContext, HRESULT *pResult)
{
    if (assembly == NULL || (pContext == NULL && pResult == NULL))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    FailedAssembly* pAssembly = PTR_FailedAssembly(TO_TADDR(assembly));
    if (!pAssembly)
    {
        hr = E_INVALIDARG;
    }
    else
    {
#ifdef FEATURE_FUSION
        if (pContext)
            *pContext = pAssembly->context;
#endif
        if (pResult)
            *pResult = pAssembly->error;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyLocation(CLRDATA_ADDRESS assembly, unsigned int count,
                                         __out_z __inout_ecount(count) wchar_t *location, unsigned int *pNeeded)
{
    if (assembly == NULL || (location == NULL && pNeeded == NULL) || (location != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    FailedAssembly* pAssembly = PTR_FailedAssembly(TO_TADDR(assembly));

    // Turn from bytes to wide characters
    if (!pAssembly->location.IsEmpty())
    {
        if (!pAssembly->location.DacGetUnicode(count, location, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (pNeeded)
            *pNeeded = 1;

        if (location)
            location[0] = 0;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyDisplayName(CLRDATA_ADDRESS assembly, unsigned int count, __out_z __inout_ecount(count) wchar_t *name, unsigned int *pNeeded)
{
    if (assembly == NULL || (name == NULL && pNeeded == NULL) || (name != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    FailedAssembly* pAssembly = PTR_FailedAssembly(TO_TADDR(assembly));

    if (!pAssembly->displayName.IsEmpty())
    {
        if (!pAssembly->displayName.DacGetUnicode(count, name, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (pNeeded)
            *pNeeded = 1;

        if (name)
            name[0] = 0;
    }

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetAssemblyList(CLRDATA_ADDRESS addr, int count, CLRDATA_ADDRESS values[], int *pNeeded)
{
    if (addr == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    BaseDomain* pBaseDomain = PTR_BaseDomain(TO_TADDR(addr));

    int n=0;
    if (pBaseDomain->IsSharedDomain())
    {
#ifdef FEATURE_LOADER_OPTIMIZATION    
        SharedDomain::SharedAssemblyIterator i;
        if (values)
        {
            while (i.Next() && n < count)
                values[n++] = HOST_CDADDR(i.GetAssembly());
        }
        else
        {
            while (i.Next())
                n++;
        }

        if (pNeeded)
            *pNeeded = n;
#else
        hr = E_UNEXPECTED;
#endif
    }
    else if (pBaseDomain->IsAppDomain())
    {
        AppDomain::AssemblyIterator i = pBaseDomain->AsAppDomain()->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoading | kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        
        if (values)
        {
            while (i.Next(pDomainAssembly.This()) && (n < count))
            {
                if (pDomainAssembly->IsLoaded())
                {
                    CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetAssembly();
                    // Note: DAC doesn't need to keep the assembly alive - see code:CollectibleAssemblyHolder#CAH_DAC
                    values[n++] = HOST_CDADDR(pAssembly.Extract());
                }
            }
        }
        else
        {
            while (i.Next(pDomainAssembly.This()))
                if (pDomainAssembly->IsLoaded())
                    n++;
        }

        if (pNeeded)
            *pNeeded = n;
    }
    else
    {
        // The only other type of BaseDomain is the SystemDomain, and we shouldn't be asking
        // for the assemblies in it.
        _ASSERTE(false);
        hr = E_INVALIDARG;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyList(CLRDATA_ADDRESS appDomain, int count,
                                     CLRDATA_ADDRESS values[], unsigned int *pNeeded)
{
    if ((appDomain == NULL) || (values == NULL && pNeeded == NULL))
    {
        return E_INVALIDARG;
    }
    
    SOSDacEnter();
    AppDomain* pAppDomain = PTR_AppDomain(TO_TADDR(appDomain));

    int n=0;
    AppDomain::FailedAssemblyIterator i = pAppDomain->IterateFailedAssembliesEx();
    while (i.Next() && n<=count)
    {
        if (values)
            values[n] = HOST_CDADDR(i.GetFailedAssembly());

        n++;
    }
    
    if (pNeeded)
        *pNeeded = n;
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAppDomainName(CLRDATA_ADDRESS addr, unsigned int count, __out_z __inout_ecount(count) wchar_t *name, unsigned int *pNeeded)
{
    SOSDacEnter();

    PTR_BaseDomain pBaseDomain = PTR_BaseDomain(TO_TADDR(addr));
    if (!pBaseDomain->IsAppDomain())
    {
        // Shared domain and SystemDomain don't have this field.
        if (pNeeded)
            *pNeeded = 1;
        if (name)
            name[0] = 0;
    }
    else
    {
        AppDomain* pAppDomain = pBaseDomain->AsAppDomain();

        if (!pAppDomain->m_friendlyName.IsEmpty())
        {
            if (!pAppDomain->m_friendlyName.DacGetUnicode(count, name, pNeeded))
            {
                hr =  E_FAIL;
            }
        }
        else
        {
            if (pNeeded)
                *pNeeded = 1;
            if (name)
                name[0] = 0;

            hr = S_OK;
        }
    }
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetApplicationBase(CLRDATA_ADDRESS appDomain, int count,
                                  __out_z __inout_ecount(count) wchar_t *base, unsigned int *pNeeded)
{
    if (appDomain == NULL || (base == NULL && pNeeded == NULL) || (base != NULL && count == 0))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();
    AppDomain* pAppDomain = PTR_AppDomain(TO_TADDR(appDomain));

    // Turn from bytes to wide characters
    if ((PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SharedDomain::GetDomain())) ||
        (PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SystemDomain::System())))
    {
        // Shared domain and SystemDomain don't have this field.
        if (base)
            base[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }

    if (!pAppDomain->m_applicationBase.IsEmpty())
    {
        if (!pAppDomain->m_applicationBase.
            DacGetUnicode(count, base, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (base)
            base[0] = 0;
        
        if (pNeeded)
            *pNeeded = 1;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetPrivateBinPaths(CLRDATA_ADDRESS appDomain, int count,
                                  __out_z __inout_ecount(count) wchar_t *paths, unsigned int *pNeeded)
{
    if (appDomain == NULL || (paths == NULL && pNeeded == NULL) || (paths != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    AppDomain* pAppDomain = PTR_AppDomain(TO_TADDR(appDomain));

    // Turn from bytes to wide characters
    if ((PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SharedDomain::GetDomain())) ||
        (PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SystemDomain::System())))
    {
        // Shared domain and SystemDomain don't have this field.
        if (pNeeded)
            *pNeeded = 1;

        if (paths)
            paths[0] = 0;

        hr = S_OK;
    }

    if (!pAppDomain->m_privateBinPaths.IsEmpty())
    {
        if (!pAppDomain->m_privateBinPaths.DacGetUnicode(count, paths, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (paths)
            paths[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAppDomainConfigFile(CLRDATA_ADDRESS appDomain, int count,
                                      __out_z __inout_ecount(count) wchar_t *configFile, unsigned int *pNeeded)
{
    if (appDomain == NULL || (configFile == NULL && pNeeded == NULL) || (configFile != NULL && count == 0))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();
    AppDomain* pAppDomain = PTR_AppDomain(TO_TADDR(appDomain));

    // Turn from bytes to wide characters

    if ((PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SharedDomain::GetDomain())) ||
        (PTR_BaseDomain(pAppDomain) == PTR_BaseDomain(SystemDomain::System())))
    {
        // Shared domain and SystemDomain don't have this field.
        if (configFile)
            configFile[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }

    if (!pAppDomain->m_configFile.IsEmpty())
    {
        if (!pAppDomain->m_configFile.DacGetUnicode(count, configFile, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (configFile)
            configFile[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAssemblyData(CLRDATA_ADDRESS cdBaseDomainPtr, CLRDATA_ADDRESS assembly, struct DacpAssemblyData *assemblyData)
{
    if (assembly == NULL && cdBaseDomainPtr == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));

    // Make sure conditionally-assigned fields like AssemblySecDesc, LoadContext, etc. are zeroed
    ZeroMemory(assemblyData, sizeof(DacpAssemblyData));

    // If the specified BaseDomain is an AppDomain, get a pointer to it
    AppDomain * pDomain = NULL;
    if (cdBaseDomainPtr != NULL)
    {
        assemblyData->BaseDomainPtr = cdBaseDomainPtr;
        PTR_BaseDomain baseDomain = PTR_BaseDomain(TO_TADDR(cdBaseDomainPtr));
        if( baseDomain->IsAppDomain() )
            pDomain = baseDomain->AsAppDomain();
    }

    assemblyData->AssemblyPtr = HOST_CDADDR(pAssembly);
    assemblyData->ClassLoader = HOST_CDADDR(pAssembly->GetLoader());
    assemblyData->ParentDomain = HOST_CDADDR(pAssembly->GetDomain());
    if (pDomain != NULL)
        assemblyData->AssemblySecDesc = HOST_CDADDR(pAssembly->GetSecurityDescriptor(pDomain));
    assemblyData->isDynamic = pAssembly->IsDynamic();
    assemblyData->ModuleCount = 0;
    assemblyData->isDomainNeutral = pAssembly->IsDomainNeutral();

    if (pAssembly->GetManifestFile())
    {
#ifdef FEATURE_FUSION    
        assemblyData->LoadContext = pAssembly->GetManifestFile()->GetLoadContext();
        assemblyData->dwLocationFlags = pAssembly->GetManifestFile()->GetLocationFlags();
#endif
        
    }

    ModuleIterator mi = pAssembly->IterateModules();
    while (mi.Next())
    {
        assemblyData->ModuleCount++;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAssemblyName(CLRDATA_ADDRESS assembly, unsigned int count, __out_z __inout_ecount(count) wchar_t *name, unsigned int *pNeeded)
{
    SOSDacEnter();
    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));

    if (name)
        name[0] = 0;

    if (!pAssembly->GetManifestFile()->GetPath().IsEmpty())
    {
        if (!pAssembly->GetManifestFile()->GetPath().DacGetUnicode(count, name, pNeeded))
            hr = E_FAIL;
        else if (name)
            name[count-1] = 0;
    }
    else if (!pAssembly->GetManifestFile()->IsDynamic())
    {
        StackSString displayName;
        pAssembly->GetManifestFile()->GetDisplayName(displayName, 0);
        
        const wchar_t *val = displayName.GetUnicode();
        
        if (pNeeded)
            *pNeeded = displayName.GetCount() + 1;
        
        if (name && count)
        {
            wcsncpy_s(name, count, val, _TRUNCATE);
            name[count-1] = 0;
        }
    }
    else
    {
        hr = E_FAIL;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAssemblyLocation(CLRDATA_ADDRESS assembly, int count, __out_z __inout_ecount(count) wchar_t *location, unsigned int *pNeeded)
{
    if ((assembly == NULL) || (location == NULL && pNeeded == NULL) || (location != NULL && count == 0))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));

    // Turn from bytes to wide characters
    if (!pAssembly->GetManifestFile()->GetPath().IsEmpty())
    {
        if (!pAssembly->GetManifestFile()->GetPath().
            DacGetUnicode(count, location, pNeeded))
        {
            hr = E_FAIL;
        }
    }
    else
    {
        if (location)
            location[0] = 0;

        if (pNeeded)
            *pNeeded = 1;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAssemblyModuleList(CLRDATA_ADDRESS assembly, unsigned int count, CLRDATA_ADDRESS modules[], unsigned int *pNeeded)
{
    if (assembly == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));
    ModuleIterator mi = pAssembly->IterateModules();
    unsigned int n = 0;
    if (modules)
    {
        while (mi.Next() && n < count)
            modules[n++] = HOST_CDADDR(mi.GetModule());
    }
    else
    {
        while (mi.Next())
            n++;
    }

    if (pNeeded)
        *pNeeded = n;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetGCHeapDetails(CLRDATA_ADDRESS heap, struct DacpGcHeapDetails *details)
{
    if (heap == 0 || details == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    // doesn't make sense to call this on WKS mode
    if (!GCHeap::IsServerHeap())
        hr = E_INVALIDARG;
    else
#ifdef FEATURE_SVR_GC
        hr = ServerGCHeapDetails(heap, details);
#else
        hr = E_NOTIMPL;
#endif

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetGCHeapStaticData(struct DacpGcHeapDetails *detailsData)
{
    if (detailsData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    detailsData->lowest_address = PTR_CDADDR(g_lowest_address);
    detailsData->highest_address = PTR_CDADDR(g_highest_address);
    detailsData->card_table = PTR_CDADDR(g_card_table);

    detailsData->heapAddr = NULL;

    detailsData->alloc_allocated = PTR_CDADDR(WKS::gc_heap::alloc_allocated);
    detailsData->ephemeral_heap_segment = PTR_CDADDR(WKS::gc_heap::ephemeral_heap_segment);

#ifdef BACKGROUND_GC
    detailsData->mark_array = PTR_CDADDR(WKS::gc_heap::mark_array);
    detailsData->current_c_gc_state = (CLRDATA_ADDRESS)(ULONG_PTR)WKS::gc_heap::current_c_gc_state;
    detailsData->next_sweep_obj = PTR_CDADDR(WKS::gc_heap::next_sweep_obj);
    detailsData->saved_sweep_ephemeral_seg = PTR_CDADDR(WKS::gc_heap::saved_sweep_ephemeral_seg);
    detailsData->saved_sweep_ephemeral_start = PTR_CDADDR(WKS::gc_heap::saved_sweep_ephemeral_start);
    detailsData->background_saved_lowest_address = PTR_CDADDR(WKS::gc_heap::background_saved_lowest_address);
    detailsData->background_saved_highest_address = PTR_CDADDR(WKS::gc_heap::background_saved_highest_address);
#endif //BACKGROUND_GC

    for (int i=0;i<NUMBERGENERATIONS;i++)
    {
        detailsData->generation_table[i].start_segment = (CLRDATA_ADDRESS)dac_cast<TADDR>(WKS::generation_table[i].start_segment);
        detailsData->generation_table[i].allocation_start = (CLRDATA_ADDRESS)(ULONG_PTR) WKS::generation_table[i].allocation_start;
        detailsData->generation_table[i].allocContextPtr = (CLRDATA_ADDRESS)(ULONG_PTR) WKS::generation_table[i].allocation_context.alloc_ptr;
        detailsData->generation_table[i].allocContextLimit = (CLRDATA_ADDRESS)(ULONG_PTR) WKS::generation_table[i].allocation_context.alloc_limit;
    }

    TADDR pFillPointerArray = TO_TADDR(WKS::gc_heap::finalize_queue.GetAddr()) + offsetof(WKS::CFinalize,m_FillPointers);
    for(int i=0;i<(NUMBERGENERATIONS+WKS::CFinalize::ExtraSegCount);i++)
    {
        ULONG32 returned = 0;
        size_t pValue;
        hr = m_pTarget->ReadVirtual(pFillPointerArray+(i*sizeof(size_t)), (PBYTE)&pValue, sizeof(size_t), &returned);
        if (SUCCEEDED(hr))
        {
            if (returned == sizeof(size_t))
                detailsData->finalization_fill_pointers[i] = (CLRDATA_ADDRESS) pValue;
            else
                hr = E_FAIL;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetHeapSegmentData(CLRDATA_ADDRESS seg, struct DacpHeapSegmentData *heapSegment)
{
    if (seg == 0 || heapSegment == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    if (GCHeap::IsServerHeap())
    {
#if !defined(FEATURE_SVR_GC)
        _ASSERTE(0);
#else // !defined(FEATURE_SVR_GC)
        hr = GetServerHeapData(seg, heapSegment);
#endif //!defined(FEATURE_SVR_GC)
    }
    else
    {
        WKS::heap_segment *pSegment = __DPtr<WKS::heap_segment>(TO_TADDR(seg));
        if (!pSegment)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            heapSegment->segmentAddr = seg;
            heapSegment->allocated = (CLRDATA_ADDRESS)(ULONG_PTR) pSegment->allocated;
            heapSegment->committed = (CLRDATA_ADDRESS)(ULONG_PTR) pSegment->committed;
            heapSegment->reserved = (CLRDATA_ADDRESS)(ULONG_PTR) pSegment->reserved;
            heapSegment->used = (CLRDATA_ADDRESS)(ULONG_PTR) pSegment->used;
            heapSegment->mem = (CLRDATA_ADDRESS)(ULONG_PTR) pSegment->mem;
            heapSegment->next = (CLRDATA_ADDRESS)dac_cast<TADDR>(pSegment->next);
            heapSegment->flags = pSegment->flags;
            heapSegment->gc_heap = NULL;
            heapSegment->background_allocated = (CLRDATA_ADDRESS)(ULONG_PTR)pSegment->background_allocated;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetGCHeapList(unsigned int count, CLRDATA_ADDRESS heaps[], unsigned int *pNeeded)
{
    SOSDacEnter();

    // make sure we called this in appropriate circumstances (i.e., we have multiple heaps)
    if (GCHeap::IsServerHeap())
    {
#if !defined(FEATURE_SVR_GC)
        _ASSERTE(0);
#else // !defined(FEATURE_SVR_GC)
        int heapCount = GCHeapCount();
        if (pNeeded)
            *pNeeded = heapCount;

        if (heaps)
        {
            // get the heap locations
            if (count == heapCount)
                hr = GetServerHeaps(heaps, m_pTarget);
            else
                hr = E_INVALIDARG;
        }
#endif // !defined(FEATURE_SVR_GC)
    }
    else
    {
        hr = E_FAIL; // doesn't make sense to call this on WKS mode
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetGCHeapData(struct DacpGcHeapData *gcheapData)
{
    if (gcheapData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    // Now get the heap type. The first data member of the GCHeap class is the GC_HEAP_TYPE, which has 
    // three possible values:
    //       GC_HEAP_INVALID = 0,
    //       GC_HEAP_WKS     = 1,
    //       GC_HEAP_SVR     = 2

    TADDR gcHeapLocation = g_pGCHeap.GetAddrRaw (); // get the starting address of the global GCHeap instance
    size_t gcHeapValue = 0;                         // this will hold the heap type
    ULONG32 returned = 0;

    // @todo Microsoft: we should probably be capturing the HRESULT from ReadVirtual. We could 
    // provide a more informative error message. E_FAIL is a wretchedly vague thing to return. 
    hr = m_pTarget->ReadVirtual(gcHeapLocation, (PBYTE)&gcHeapValue, sizeof(gcHeapValue), &returned);

    //@todo Microsoft: We have an enumerated type, we probably should use the symbolic name
    // we have GC_HEAP_INVALID if gcHeapValue == 0, so we're done
    if (SUCCEEDED(hr) && ((returned != sizeof(gcHeapValue)) || (gcHeapValue == 0)))
        hr = E_FAIL;

    if (SUCCEEDED(hr))
    {
        // Now we can get other important information about the heap
        gcheapData->g_max_generation = GCHeap::GetMaxGeneration();
        gcheapData->bServerMode = GCHeap::IsServerHeap();
        gcheapData->bGcStructuresValid = CNameSpace::GetGcRuntimeStructuresValid();
        if (GCHeap::IsServerHeap())
        {
#if !defined (FEATURE_SVR_GC)
            _ASSERTE(0);
            gcheapData->HeapCount = 1;
#else // !defined (FEATURE_SVR_GC)
            gcheapData->HeapCount = GCHeapCount();
#endif // !defined (FEATURE_SVR_GC)
        }
        else
        {
            gcheapData->HeapCount = 1;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetOOMStaticData(struct DacpOomData *oomData)
{
    if (oomData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    memset(oomData, 0, sizeof(DacpOomData));

    if (!GCHeap::IsServerHeap())
    {
        oom_history* pOOMInfo = &(WKS::gc_heap::oom_info);
        oomData->reason = pOOMInfo->reason;
        oomData->alloc_size = pOOMInfo->alloc_size;
        oomData->available_pagefile_mb = pOOMInfo->available_pagefile_mb;
        oomData->gc_index = pOOMInfo->gc_index;
        oomData->fgm = pOOMInfo->fgm;
        oomData->size = pOOMInfo->size;
        oomData->loh_p = pOOMInfo->loh_p;
    }
    else
    {
        hr = E_FAIL;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetOOMData(CLRDATA_ADDRESS oomAddr, struct DacpOomData *data)
{
    if (oomAddr == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    memset(data, 0, sizeof(DacpOomData));

    if (!GCHeap::IsServerHeap())
        hr = E_FAIL; // doesn't make sense to call this on WKS mode
    
#ifdef FEATURE_SVR_GC
    else
        hr = ServerOomData(oomAddr, data);
#else
    _ASSERTE_MSG(false, "IsServerHeap returned true but FEATURE_SVR_GC not defined");
    hr = E_NOTIMPL;
#endif //FEATURE_SVR_GC

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetHeapAnalyzeData(CLRDATA_ADDRESS addr, struct  DacpGcHeapAnalyzeData *data)
{
    if (addr == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    if (!GCHeap::IsServerHeap())
        hr = E_FAIL; // doesn't make sense to call this on WKS mode

#ifdef FEATURE_SVR_GC
    else
        hr = ServerGCHeapAnalyzeData(addr, data);
#else
    _ASSERTE_MSG(false, "IsServerHeap returned true but FEATURE_SVR_GC not defined");
    hr = E_NOTIMPL;
#endif //FEATURE_SVR_GC

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetHeapAnalyzeStaticData(struct DacpGcHeapAnalyzeData *analyzeData)
{
    if (analyzeData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    analyzeData->internal_root_array = PTR_CDADDR(WKS::gc_heap::internal_root_array);
    analyzeData->internal_root_array_index = (size_t) WKS::gc_heap::internal_root_array_index;
    analyzeData->heap_analyze_success = (BOOL) WKS::gc_heap::heap_analyze_success;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetUsefulGlobals(struct DacpUsefulGlobalsData *globalsData)
{
    if (globalsData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

// TODO - mikem 2/20/15 - ifdef temporary until the global pointer table is implemented for linux.
#ifndef FEATURE_PAL
    PTR_ArrayTypeDesc objArray = g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT];
    if (objArray)
        globalsData->ArrayMethodTable = HOST_CDADDR(objArray->GetMethodTable());
    else
        globalsData->ArrayMethodTable = 0;

    globalsData->StringMethodTable = HOST_CDADDR(g_pStringClass);
    globalsData->ObjectMethodTable = HOST_CDADDR(g_pObjectClass);
    globalsData->ExceptionMethodTable = HOST_CDADDR(g_pExceptionClass);
    globalsData->FreeMethodTable = HOST_CDADDR(g_pFreeObjectMethodTable);
#endif

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetNestedExceptionData(CLRDATA_ADDRESS exception, CLRDATA_ADDRESS *exceptionObject, CLRDATA_ADDRESS *nextNestedException)
{
    if (exception == 0 || exceptionObject == NULL || nextNestedException == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

#ifdef WIN64EXCEPTIONS
    ExceptionTracker *pExData = PTR_ExceptionTracker(TO_TADDR(exception));
#else
    ExInfo *pExData = PTR_ExInfo(TO_TADDR(exception));
#endif // _WIN64

    if (!pExData)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        *exceptionObject = TO_CDADDR(*PTR_TADDR(pExData->m_hThrowable));
        *nextNestedException = PTR_HOST_TO_TADDR(pExData->m_pPrevNestedInfo);
    }

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetDomainLocalModuleData(CLRDATA_ADDRESS addr, struct DacpDomainLocalModuleData *pLocalModuleData)
{
    if (addr == 0 || pLocalModuleData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    DomainLocalModule* pLocalModule = PTR_DomainLocalModule(TO_TADDR(addr));

    pLocalModuleData->pGCStaticDataStart    = TO_CDADDR(PTR_TO_TADDR(pLocalModule->GetPrecomputedGCStaticsBasePointer()));
    pLocalModuleData->pNonGCStaticDataStart = TO_CDADDR(pLocalModule->GetPrecomputedNonGCStaticsBasePointer());
    pLocalModuleData->pDynamicClassTable    = PTR_CDADDR(pLocalModule->m_pDynamicClassTable.Load());
    pLocalModuleData->pClassData            = (TADDR) (PTR_HOST_MEMBER_TADDR(DomainLocalModule, pLocalModule, m_pDataBlob));

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetDomainLocalModuleDataFromModule(CLRDATA_ADDRESS addr, struct DacpDomainLocalModuleData *pLocalModuleData)
{
    if (addr == 0 || pLocalModuleData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Module* pModule = PTR_Module(TO_TADDR(addr));
    if( pModule->GetAssembly()->IsDomainNeutral() )
    {
        // The module is loaded domain-neutral, then we need to know the specific AppDomain in order to
        // choose a DomainLocalModule instance.  Rather than try and guess an AppDomain (eg. based on
        // whatever the current debugger thread is in), we'll fail and force the debugger to explicitly use
        // a specific AppDomain.
        hr = E_INVALIDARG;
    }
    else
    {    
        DomainLocalModule* pLocalModule = PTR_DomainLocalModule(pModule->GetDomainLocalModule(NULL));
        if (!pLocalModule)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            pLocalModuleData->pGCStaticDataStart    = TO_CDADDR(PTR_TO_TADDR(pLocalModule->GetPrecomputedGCStaticsBasePointer()));
            pLocalModuleData->pNonGCStaticDataStart = TO_CDADDR(pLocalModule->GetPrecomputedNonGCStaticsBasePointer());
            pLocalModuleData->pDynamicClassTable    = PTR_CDADDR(pLocalModule->m_pDynamicClassTable.Load());
            pLocalModuleData->pClassData            = (TADDR) (PTR_HOST_MEMBER_TADDR(DomainLocalModule, pLocalModule, m_pDataBlob));
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetDomainLocalModuleDataFromAppDomain(CLRDATA_ADDRESS appDomainAddr, int moduleID, struct DacpDomainLocalModuleData *pLocalModuleData)
{
    if (appDomainAddr == 0 || moduleID < 0 || pLocalModuleData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    pLocalModuleData->appDomainAddr = appDomainAddr;
    pLocalModuleData->ModuleID = moduleID;

    AppDomain *pAppDomain = PTR_AppDomain(TO_TADDR(appDomainAddr));
    ModuleIndex index = Module::IDToIndex(moduleID);
    DomainLocalModule* pLocalModule = pAppDomain->GetDomainLocalBlock()->GetModuleSlot(index);
    if (!pLocalModule)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        pLocalModuleData->pGCStaticDataStart    = TO_CDADDR(PTR_TO_TADDR(pLocalModule->GetPrecomputedGCStaticsBasePointer()));
        pLocalModuleData->pNonGCStaticDataStart = TO_CDADDR(pLocalModule->GetPrecomputedNonGCStaticsBasePointer());
        pLocalModuleData->pDynamicClassTable    = PTR_CDADDR(pLocalModule->m_pDynamicClassTable.Load());
        pLocalModuleData->pClassData            = (TADDR) (PTR_HOST_MEMBER_TADDR(DomainLocalModule, pLocalModule, m_pDataBlob));
    }

    SOSDacLeave();
    return hr;
}




HRESULT
ClrDataAccess::GetThreadLocalModuleData(CLRDATA_ADDRESS thread, unsigned int index, struct DacpThreadLocalModuleData *pLocalModuleData)
{
    if (pLocalModuleData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    pLocalModuleData->threadAddr = thread;
    pLocalModuleData->ModuleIndex = index;
    
    PTR_Thread pThread = PTR_Thread(TO_TADDR(thread));
    PTR_ThreadLocalBlock pLocalBlock = ThreadStatics::GetCurrentTLBIfExists(pThread, NULL);
    if (!pLocalBlock)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        PTR_ThreadLocalModule pLocalModule = pLocalBlock->GetTLMIfExists(ModuleIndex(index));
        if (!pLocalModule)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            pLocalModuleData->pGCStaticDataStart    = TO_CDADDR(PTR_TO_TADDR(pLocalModule->GetPrecomputedGCStaticsBasePointer()));
            pLocalModuleData->pNonGCStaticDataStart = TO_CDADDR(pLocalModule->GetPrecomputedNonGCStaticsBasePointer());
            pLocalModuleData->pDynamicClassTable    = PTR_CDADDR(pLocalModule->m_pDynamicClassTable);
            pLocalModuleData->pClassData            = (TADDR) (PTR_HOST_MEMBER_TADDR(ThreadLocalModule, pLocalModule, m_pDataBlob));
        }
    }

    SOSDacLeave();
    return hr;
}


HRESULT ClrDataAccess::GetHandleEnum(ISOSHandleEnum **ppHandleEnum)
{
    unsigned int types[] = {HNDTYPE_WEAK_SHORT, HNDTYPE_WEAK_LONG, HNDTYPE_STRONG, HNDTYPE_PINNED, HNDTYPE_VARIABLE, HNDTYPE_DEPENDENT,
                            HNDTYPE_ASYNCPINNED, HNDTYPE_SIZEDREF,
#ifdef FEATURE_COMINTEROP
                            HNDTYPE_REFCOUNTED, HNDTYPE_WEAK_WINRT
#endif
                            };

    return GetHandleEnumForTypes(types, _countof(types), ppHandleEnum);
}

HRESULT ClrDataAccess::GetHandleEnumForTypes(unsigned int types[], unsigned int count, ISOSHandleEnum **ppHandleEnum)
{
    if (ppHandleEnum == 0)
        return E_POINTER;
    
    SOSDacEnter();
                    
    DacHandleWalker *walker = new DacHandleWalker();
    
    HRESULT hr = walker->Init(this, types, count);
    
    if (SUCCEEDED(hr))
        hr = walker->QueryInterface(__uuidof(ISOSHandleEnum), (void**)ppHandleEnum);
    
    if (FAILED(hr))
        delete walker;
    
    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetHandleEnumForGC(unsigned int gen, ISOSHandleEnum **ppHandleEnum)
{
    if (ppHandleEnum == 0)
        return E_POINTER;
    
    SOSDacEnter();
    
    unsigned int types[] = {HNDTYPE_WEAK_SHORT, HNDTYPE_WEAK_LONG, HNDTYPE_STRONG, HNDTYPE_PINNED, HNDTYPE_VARIABLE, HNDTYPE_DEPENDENT,
                            HNDTYPE_ASYNCPINNED, HNDTYPE_SIZEDREF,
#ifdef FEATURE_COMINTEROP
                            HNDTYPE_REFCOUNTED, HNDTYPE_WEAK_WINRT
#endif
                            };
                            
    DacHandleWalker *walker = new DacHandleWalker();
    
    HRESULT hr = walker->Init(this, types, _countof(types), gen);
    if (SUCCEEDED(hr))
        hr = walker->QueryInterface(__uuidof(ISOSHandleEnum), (void**)ppHandleEnum);
    
    if (FAILED(hr))
        delete walker;
    
    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::TraverseEHInfo(CLRDATA_ADDRESS ip, DUMPEHINFO pFunc, LPVOID token)
{
    if (ip == 0 || pFunc == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    
    EECodeInfo codeInfo(TO_TADDR(ip));
    if (!codeInfo.IsValid())
    {
        hr = E_INVALIDARG;
    }

    if (SUCCEEDED(hr))
    {
        EH_CLAUSE_ENUMERATOR    EnumState;
        EE_ILEXCEPTION_CLAUSE   EHClause;
        unsigned                EHCount;

        EHCount = codeInfo.GetJitManager()->InitializeEHEnumeration(codeInfo.GetMethodToken(), &EnumState);
        for (unsigned i = 0; i < EHCount; i++)
        {
            codeInfo.GetJitManager()->GetNextEHClause(&EnumState, &EHClause);

            DACEHInfo deh;
            ZeroMemory(&deh,sizeof(deh));

            if (IsFault(&EHClause))
            {
                deh.clauseType = EHFault;
            }
            else if (IsFinally(&EHClause))
            {
                deh.clauseType = EHFinally;
            }
            else if (IsFilterHandler(&EHClause))
            {
                deh.clauseType = EHFilter;
                deh.filterOffset = EHClause.FilterOffset;
            }
            else if (IsTypedHandler(&EHClause))
            {
                deh.clauseType = EHTyped;
                deh.isCatchAllHandler = (&EHClause.TypeHandle == (void*)(size_t)mdTypeRefNil);
            }
            else
            {
                deh.clauseType = EHUnknown;
            }

            if (HasCachedTypeHandle(&EHClause))
            {
                deh.mtCatch = TO_CDADDR(&EHClause.TypeHandle);
            }
            else if(!IsFaultOrFinally(&EHClause))
            {
                // the module of the token (whether a ref or def token) is the same as the module of the method containing the EH clause
                deh.moduleAddr = HOST_CDADDR(codeInfo.GetMethodDesc()->GetModule());
                deh.tokCatch = EHClause.ClassToken;
            }

            deh.tryStartOffset = EHClause.TryStartPC;
            deh.tryEndOffset = EHClause.TryEndPC;
            deh.handlerStartOffset = EHClause.HandlerStartPC;
            deh.handlerEndOffset = EHClause.HandlerEndPC;
            deh.isDuplicateClause = IsDuplicateClause(&EHClause);

            if (!(pFunc)(i, EHCount, &deh, token))
            {
                // User wants to stop the enumeration
                hr = E_ABORT;
                break;
            }
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::TraverseRCWCleanupList(CLRDATA_ADDRESS cleanupListPtr, VISITRCWFORCLEANUP pFunc, LPVOID token)
{
#ifdef FEATURE_COMINTEROP
    if (pFunc == 0)
        return E_INVALIDARG;

    SOSDacEnter();
    RCWCleanupList *pList = g_pRCWCleanupList;

    if (cleanupListPtr)
    {
        pList = PTR_RCWCleanupList(TO_TADDR(cleanupListPtr));
    }

    if (pList)
    {
        PTR_RCW pBucket = dac_cast<PTR_RCW>(TO_TADDR(pList->m_pFirstBucket));
        while (pBucket != NULL)
        {
            PTR_RCW pRCW = pBucket;
            Thread *pSTAThread = pRCW->GetSTAThread();
            LPVOID pCtxCookie  = pRCW->GetWrapperCtxCookie();
            BOOL bIsFreeThreaded = pRCW->IsFreeThreaded();
            
            while (pRCW)
            {
                (pFunc)(HOST_CDADDR(pRCW),(CLRDATA_ADDRESS)pCtxCookie, (CLRDATA_ADDRESS)(TADDR)pSTAThread, bIsFreeThreaded, token);
                pRCW = pRCW->m_pNextRCW;
            }
            pBucket = pBucket->m_pNextCleanupBucket;
        }
    }

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif // FEATURE_COMINTEROP
}

HRESULT
ClrDataAccess::TraverseLoaderHeap(CLRDATA_ADDRESS loaderHeapAddr, VISITHEAP pFunc)
{
    if (loaderHeapAddr == 0 || pFunc == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    LoaderHeap *pLoaderHeap = PTR_LoaderHeap(TO_TADDR(loaderHeapAddr));
    PTR_LoaderHeapBlock block = pLoaderHeap->m_pFirstBlock;
    while (block.IsValid())
    {
        TADDR addr = PTR_TO_TADDR(block->pVirtualAddress);
        size_t size = block->dwVirtualSize;

        BOOL bCurrentBlock = (block == pLoaderHeap->m_pCurBlock);
        
        pFunc(addr,size,bCurrentBlock);
        
        block = block->pNext;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::TraverseVirtCallStubHeap(CLRDATA_ADDRESS pAppDomain, VCSHeapType heaptype, VISITHEAP pFunc)
{
    if (pAppDomain == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    BaseDomain* pBaseDomain = PTR_BaseDomain(TO_TADDR(pAppDomain));
    VirtualCallStubManager *pVcsMgr = PTR_VirtualCallStubManager((TADDR)pBaseDomain->GetLoaderAllocator()->GetVirtualCallStubManager());
    if (!pVcsMgr)
    {
        hr = E_POINTER;
    }
    else
    {
        LoaderHeap *pLoaderHeap = NULL;
        switch(heaptype)
        {
            case IndcellHeap:
                pLoaderHeap = pVcsMgr->indcell_heap;
                break;
            case LookupHeap:
                pLoaderHeap = pVcsMgr->lookup_heap;
                break;
            case ResolveHeap:
                pLoaderHeap = pVcsMgr->resolve_heap;
                break;
            case DispatchHeap:
                pLoaderHeap = pVcsMgr->dispatch_heap;
                break;
            case CacheEntryHeap:
                pLoaderHeap = pVcsMgr->cache_entry_heap;
                break;
            default:
                hr = E_INVALIDARG;
        }

        if (SUCCEEDED(hr))
        {
            PTR_LoaderHeapBlock block = pLoaderHeap->m_pFirstBlock;
            while (block.IsValid())
            {
                TADDR addr = PTR_TO_TADDR(block->pVirtualAddress);
                size_t size = block->dwVirtualSize;

                BOOL bCurrentBlock = (block == pLoaderHeap->m_pCurBlock);
                pFunc(addr, size, bCurrentBlock);

                block = block->pNext;
            }
        }
    }

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetSyncBlockData(unsigned int SBNumber, struct DacpSyncBlockData *pSyncBlockData)
{
    if (pSyncBlockData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    ZeroMemory(pSyncBlockData,sizeof(DacpSyncBlockData));
    pSyncBlockData->SyncBlockCount = (SyncBlockCache::s_pSyncBlockCache->m_FreeSyncTableIndex) - 1;
    PTR_SyncTableEntry ste = PTR_SyncTableEntry(dac_cast<TADDR>(g_pSyncTable)+(sizeof(SyncTableEntry) * SBNumber));
    pSyncBlockData->bFree = ((dac_cast<TADDR>(ste->m_Object.Load())) & 1);

    if (pSyncBlockData->bFree == FALSE)
    {
        pSyncBlockData->Object = (CLRDATA_ADDRESS)dac_cast<TADDR>(ste->m_Object.Load());

        if (ste->m_SyncBlock != NULL)
        {
            SyncBlock *pBlock = PTR_SyncBlock(ste->m_SyncBlock);
            pSyncBlockData->SyncBlockPointer = HOST_CDADDR(pBlock);
#ifdef FEATURE_COMINTEROP
            if (pBlock->m_pInteropInfo)
            {
                pSyncBlockData->COMFlags |= (pBlock->m_pInteropInfo->DacGetRawRCW() != 0) ? SYNCBLOCKDATA_COMFLAGS_RCW : 0;
                pSyncBlockData->COMFlags |= (pBlock->m_pInteropInfo->GetCCW() != NULL) ? SYNCBLOCKDATA_COMFLAGS_CCW : 0;
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
                pSyncBlockData->COMFlags |= (pBlock->m_pInteropInfo->GetComClassFactory() != NULL) ? SYNCBLOCKDATA_COMFLAGS_CF : 0;
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
            }
#endif // FEATURE_COMINTEROP

            pSyncBlockData->MonitorHeld = pBlock->m_Monitor.m_MonitorHeld;
            pSyncBlockData->Recursion = pBlock->m_Monitor.m_Recursion;
            pSyncBlockData->HoldingThread = HOST_CDADDR(pBlock->m_Monitor.m_HoldingThread);

            if (pBlock->GetAppDomainIndex().m_dwIndex)
            {
                pSyncBlockData->appDomainPtr = PTR_HOST_TO_TADDR(
                        SystemDomain::TestGetAppDomainAtIndex(pBlock->GetAppDomainIndex()));
            }

            // TODO: Microsoft, implement the wait list
            pSyncBlockData->AdditionalThreadCount = 0;

            if (pBlock->m_Link.m_pNext != NULL)
            {
                PTR_SLink pLink = pBlock->m_Link.m_pNext;
                do
                {
                    pSyncBlockData->AdditionalThreadCount++;
                    pLink = pBlock->m_Link.m_pNext;
                }
                while ((pLink != NULL) &&
                    (pSyncBlockData->AdditionalThreadCount < 1000));
            }
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetSyncBlockCleanupData(CLRDATA_ADDRESS syncBlock, struct DacpSyncBlockCleanupData *syncBlockCData)
{
    if (syncBlock == 0 || syncBlockCData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    ZeroMemory (syncBlockCData, sizeof(DacpSyncBlockCleanupData));
    SyncBlock *pBlock = NULL;

    if (syncBlock == NULL && SyncBlockCache::s_pSyncBlockCache->m_pCleanupBlockList)
    {
        pBlock = (SyncBlock *) PTR_SyncBlock(
            PTR_HOST_TO_TADDR(SyncBlockCache::s_pSyncBlockCache->m_pCleanupBlockList) - offsetof(SyncBlock, m_Link));
    }
    else
    {
        pBlock = PTR_SyncBlock(TO_TADDR(syncBlock));
    }

    if (pBlock)
    {
        syncBlockCData->SyncBlockPointer = HOST_CDADDR(pBlock);
        if (pBlock->m_Link.m_pNext)
        {
            syncBlockCData->nextSyncBlock = (CLRDATA_ADDRESS)
                (PTR_HOST_TO_TADDR(pBlock->m_Link.m_pNext) - offsetof(SyncBlock, m_Link));
        }

#ifdef FEATURE_COMINTEROP
        if (pBlock->m_pInteropInfo->DacGetRawRCW())
            syncBlockCData->blockRCW = (CLRDATA_ADDRESS) pBlock->m_pInteropInfo->DacGetRawRCW();
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        if (pBlock->m_pInteropInfo->GetComClassFactory())
            syncBlockCData->blockClassFactory = (CLRDATA_ADDRESS) (TADDR) pBlock->m_pInteropInfo->GetComClassFactory();
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        if (pBlock->m_pInteropInfo->GetCCW())
            syncBlockCData->blockCCW = (CLRDATA_ADDRESS) dac_cast<TADDR>(pBlock->m_pInteropInfo->GetCCW());
#endif // FEATURE_COMINTEROP
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetJitHelperFunctionName(CLRDATA_ADDRESS ip, unsigned int count, __out_z __inout_ecount(count) char *name, unsigned int *pNeeded)
{
    SOSDacEnter();

    PCSTR pszHelperName = GetJitHelperName(TO_TADDR(ip));
    if (pszHelperName == NULL)
    {
        hr = E_INVALIDARG;
    }
    else
    {
        unsigned int len = (unsigned int)strlen(pszHelperName) + 1;

        if (pNeeded)
            *pNeeded = len;

        if (name)
        {
            if (count < len)
                hr = E_FAIL;
            else
                strcpy_s(name, count, pszHelperName);
        }
    }

    SOSDacLeave();
    return hr;
};

HRESULT
ClrDataAccess::GetJumpThunkTarget(T_CONTEXT *ctx, CLRDATA_ADDRESS *targetIP, CLRDATA_ADDRESS *targetMD)
{
    if (ctx == NULL || targetIP == NULL || targetMD == NULL)
        return E_INVALIDARG;
    
#ifdef _WIN64
    SOSDacEnter();
    
    if (!GetAnyThunkTarget(ctx, targetIP, targetMD))
        hr = E_FAIL;

    SOSDacLeave();
    return hr;
#else
    return E_FAIL;
#endif // _WIN64
}


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
STDMETHODIMP
ClrDataAccess::Request(IN ULONG32 reqCode,
                       IN ULONG32 inBufferSize,
                       IN BYTE* inBuffer,
                       IN ULONG32 outBufferSize,
                       OUT BYTE* outBuffer)
{
    HRESULT status;

    DAC_ENTER();

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 9;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), this, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void
ClrDataAccess::EnumWksGlobalMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WKS::gc_heap::ephemeral_heap_segment.EnumMem();
    WKS::gc_heap::alloc_allocated.EnumMem();
    WKS::gc_heap::finalize_queue.EnumMem();
    WKS::generation_table.EnumMem();
    WKS::gc_heap::oom_info.EnumMem();

    if (WKS::generation_table.IsValid())
    {
            // enumerating the generations from max (which is normally gen2) to max+1 gives you
            // the segment list for all the normal segements plus the large heap segment (max+1)
            // this is the convention in the GC so it is repeated here
            for (ULONG i = GCHeap::GetMaxGeneration(); i <= GCHeap::GetMaxGeneration()+1; i++)
            {
                __DPtr<WKS::heap_segment> seg = dac_cast<TADDR>(WKS::generation_table[i].start_segment);
                while (seg)
                {
                        DacEnumMemoryRegion(dac_cast<TADDR>(seg), sizeof(WKS::heap_segment));

                        seg = __DPtr<WKS::heap_segment>(dac_cast<TADDR>(seg->next));
                }
            }
    }
}

HRESULT
ClrDataAccess::GetClrWatsonBuckets(CLRDATA_ADDRESS thread, void *pGenericModeBlock)
{
#ifdef FEATURE_PAL
	// This API is not available under FEATURE_PAL
	return E_FAIL;
#else // FEATURE_PAL
    if (thread == 0 || pGenericModeBlock == NULL)
        return E_INVALIDARG;
    
    SOSDacEnter();
    
    Thread * pThread = PTR_Thread(TO_TADDR(thread));
    hr = GetClrWatsonBucketsWorker(pThread, reinterpret_cast<GenericModeBlock *>(pGenericModeBlock));

    SOSDacLeave();
    return hr;
#endif // FEATURE_PAL
}

#ifndef FEATURE_PAL

HRESULT ClrDataAccess::GetClrWatsonBucketsWorker(Thread * pThread, GenericModeBlock * pGM)
{
    if ((pThread == NULL) || (pGM == NULL))
    {
        return E_INVALIDARG;
    }

    // By default, there are no buckets
    PTR_VOID pBuckets = NULL;

    // Get the handle to the throwble
    OBJECTHANDLE ohThrowable = pThread->GetThrowableAsHandle();
    if (ohThrowable != NULL)
    {
	    // Get the object from handle and check if the throwable is preallocated or not
	    OBJECTREF oThrowable = ObjectFromHandle(ohThrowable);
        if (oThrowable != NULL)
        {
            // Does the throwable have buckets?
            if (((EXCEPTIONREF)oThrowable)->AreWatsonBucketsPresent())
            {
                // Get the watson buckets from the throwable for non-preallocated
                // exceptions
                U1ARRAYREF refWatsonBucketArray = ((EXCEPTIONREF)oThrowable)->GetWatsonBucketReference();
                pBuckets = dac_cast<PTR_VOID>(refWatsonBucketArray->GetDataPtr());
            }
            else
            {
                // This is a preallocated exception object - check if the UE Watson bucket tracker
                // has any bucket details
                pBuckets = pThread->GetExceptionState()->GetUEWatsonBucketTracker()->RetrieveWatsonBuckets();
                if (pBuckets == NULL)
                {
                    // Since the UE watson bucket tracker does not have them, look up the current
                    // exception tracker
                    if (pThread->GetExceptionState()->GetCurrentExceptionTracker() != NULL)
                    {
                        pBuckets = pThread->GetExceptionState()->GetCurrentExceptionTracker()->GetWatsonBucketTracker()->RetrieveWatsonBuckets();
                    }
                }
            }
        }
    }
    else 
    {
        // Debuger.Break doesn't have a throwable, but saves Watson buckets in EHWatsonBucketTracker.
        pBuckets = pThread->GetExceptionState()->GetUEWatsonBucketTracker()->RetrieveWatsonBuckets();
    }

    // If pBuckets is non-null, it is the address of a Watson GenericModeBlock in the target process.
    if (pBuckets != NULL)
    {
        ULONG32 returned = 0;
        HRESULT hr = m_pTarget->ReadVirtual(dac_cast<TADDR>(pBuckets), reinterpret_cast<BYTE *>(pGM), sizeof(*pGM), &returned);
        if (FAILED(hr))
        {
            hr = CORDBG_E_READVIRTUAL_FAILURE;
        }
        if (SUCCEEDED(hr) && (returned != sizeof(*pGM)))
        {
            hr = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
        }
        return hr;
    }
    else
    {
        // Buckets are not available
        return S_FALSE;
    }
}

#endif // FEATURE_PAL

HRESULT ClrDataAccess::GetTLSIndex(ULONG *pIndex)
{
    if (pIndex == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    if (CExecutionEngine::GetTlsIndex() == TLS_OUT_OF_INDEXES)
    {
        *pIndex = 0;
        hr = S_FALSE;
    }
    else
    {
        *pIndex = CExecutionEngine::GetTlsIndex();
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetDacModuleHandle(HMODULE *phModule)
{
    if(phModule == NULL)
        return E_INVALIDARG;
    *phModule = GetModuleInst();
    return S_OK;
}

HRESULT ClrDataAccess::GetRCWData(CLRDATA_ADDRESS addr, struct DacpRCWData *rcwData)
{
    if (addr == 0 || rcwData == NULL)
        return E_INVALIDARG;

#ifdef FEATURE_COMINTEROP
    SOSDacEnter();

    ZeroMemory (rcwData, sizeof(DacpRCWData));

    PTR_RCW pRCW = dac_cast<PTR_RCW>(CLRDATA_ADDRESS_TO_TADDR(addr));
    
    rcwData->identityPointer = TO_CDADDR(pRCW->m_pIdentity);
    rcwData->unknownPointer  = TO_CDADDR(pRCW->GetRawIUnknown_NoAddRef());
    rcwData->vtablePtr       = TO_CDADDR(pRCW->m_vtablePtr);
    rcwData->creatorThread   = TO_CDADDR(pRCW->m_pCreatorThread);
    rcwData->ctxCookie       = TO_CDADDR(pRCW->GetWrapperCtxCookie());
    rcwData->refCount        = pRCW->m_cbRefCount;

    rcwData->isJupiterObject = pRCW->IsJupiterObject();
    rcwData->supportsIInspectable = pRCW->SupportsIInspectable();
    rcwData->isAggregated = pRCW->IsURTAggregated();
    rcwData->isContained = pRCW->IsURTContained();
    rcwData->jupiterObject = TO_CDADDR(pRCW->GetJupiterObject());
    rcwData->isFreeThreaded = pRCW->IsFreeThreaded();
    rcwData->isDisconnected = pRCW->IsDisconnected();

    if (pRCW->m_SyncBlockIndex != 0)
    {
        PTR_SyncTableEntry ste = PTR_SyncTableEntry(dac_cast<TADDR>(g_pSyncTable) + (sizeof(SyncTableEntry) * pRCW->m_SyncBlockIndex));
        rcwData->managedObject = PTR_CDADDR(ste->m_Object.Load());    
    }
    
    // count the number of cached interface pointers
    rcwData->interfaceCount = 0;
    RCW::CachedInterfaceEntryIterator it = pRCW->IterateCachedInterfacePointers();
    while (it.Next())
    {
        if (it.GetEntry()->m_pUnknown.Load() != NULL)
            rcwData->interfaceCount++;
    }
    
    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif
}

HRESULT ClrDataAccess::GetRCWInterfaces(CLRDATA_ADDRESS rcw, unsigned int count, struct DacpCOMInterfacePointerData interfaces[], unsigned int *pNeeded)
{
    if (rcw == 0)
        return E_INVALIDARG;

#ifdef FEATURE_COMINTEROP

    SOSDacEnter();
    PTR_RCW pRCW = dac_cast<PTR_RCW>(CLRDATA_ADDRESS_TO_TADDR(rcw));
    if (interfaces == NULL)
    {
        if (pNeeded)
        {
            unsigned int c = 0;
            RCW::CachedInterfaceEntryIterator it = pRCW->IterateCachedInterfacePointers();
            while (it.Next())
            {
                if (it.GetEntry()->m_pUnknown.Load() != NULL)
                    c++;
            }

            *pNeeded = c;
        }
        else
        {
            hr = E_INVALIDARG;
        }
    }
    else
    {
        ZeroMemory(interfaces, sizeof(DacpCOMInterfacePointerData) * count);

        unsigned int itemIndex = 0;
        RCW::CachedInterfaceEntryIterator it = pRCW->IterateCachedInterfacePointers();
        while (it.Next())
        {
            InterfaceEntry *pEntry = it.GetEntry();
            if (pEntry->m_pUnknown.Load() != NULL)
            {
                if (itemIndex >= count)
                {
                    // the outBuffer is too small
                    hr = E_INVALIDARG;
                    break;
                }
                else
                {
                    interfaces[itemIndex].interfacePtr = TO_CDADDR(pEntry->m_pUnknown.Load());
                    interfaces[itemIndex].methodTable  = TO_CDADDR(pEntry->m_pMT.Load());
                    interfaces[itemIndex].comContext   = TO_CDADDR(it.GetCtxCookie());
                    itemIndex++;
                }
            }
        }

        if (SUCCEEDED(hr) && pNeeded)
            *pNeeded = itemIndex;
    }

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif
}

#ifdef FEATURE_COMINTEROP
PTR_ComCallWrapper ClrDataAccess::DACGetCCWFromAddress(CLRDATA_ADDRESS addr)
{
    PTR_ComCallWrapper pCCW = NULL;

    // first check whether the address is our COM IP
    TADDR pPtr = CLRDATA_ADDRESS_TO_TADDR(addr);

    ULONG32 returned = 0;
    if (m_pTarget->ReadVirtual(pPtr, (PBYTE)&pPtr, sizeof(TADDR), &returned) == S_OK &&
        returned == sizeof(TADDR))
    {
        // this should be the vtable pointer - dereference the 2nd slot
        if (m_pTarget->ReadVirtual(pPtr + sizeof(PBYTE) * TEAR_OFF_SLOT, (PBYTE)&pPtr, sizeof(TADDR), &returned) == S_OK &&
            returned == sizeof(TADDR))
        {

#ifdef DBG_TARGET_ARM
            // clear the THUMB bit on pPtr before comparing with known vtable entry
            pPtr &= ~THUMB_CODE;
#endif

            if (pPtr == GetEEFuncEntryPoint(TEAR_OFF_STANDARD))
            {
                // Points to ComCallWrapper
                PTR_IUnknown pUnk(CLRDATA_ADDRESS_TO_TADDR(addr));
                pCCW = ComCallWrapper::GetWrapperFromIP(pUnk);
            }
            else if (pPtr == GetEEFuncEntryPoint(TEAR_OFF_SIMPLE) || pPtr == GetEEFuncEntryPoint(TEAR_OFF_SIMPLE_INNER))
            {
                // Points to SimpleComCallWrapper
                PTR_IUnknown pUnk(CLRDATA_ADDRESS_TO_TADDR(addr));
                pCCW = SimpleComCallWrapper::GetWrapperFromIP(pUnk)->GetMainWrapper();               
            }
        }
    }

    if (pCCW == NULL)
    {
        // no luck interpreting the address as a COM interface pointer - it must be a CCW address
        pCCW = dac_cast<PTR_ComCallWrapper>(CLRDATA_ADDRESS_TO_TADDR(addr));
    }

    if (pCCW->IsLinked())
        pCCW = ComCallWrapper::GetStartWrapper(pCCW);

    return pCCW;
}

PTR_IUnknown ClrDataAccess::DACGetCOMIPFromCCW(PTR_ComCallWrapper pCCW, int vtableIndex)
{
    if (pCCW->m_rgpIPtr[vtableIndex] != NULL)
    {
        PTR_IUnknown pUnk = dac_cast<PTR_IUnknown>(dac_cast<TADDR>(pCCW) + offsetof(ComCallWrapper, m_rgpIPtr[vtableIndex]));

        PTR_ComMethodTable pCMT = ComMethodTable::ComMethodTableFromIP(pUnk);
        if (pCMT->IsLayoutComplete())
        {
            // return only fully laid out vtables
            return pUnk;
        }
    }
    return NULL;
}
#endif


HRESULT ClrDataAccess::GetCCWData(CLRDATA_ADDRESS ccw, struct DacpCCWData *ccwData)
{
    if (ccw == 0 || ccwData == NULL)
        return E_INVALIDARG;

#ifdef FEATURE_COMINTEROP
    SOSDacEnter();
    ZeroMemory (ccwData, sizeof(DacpCCWData));

    PTR_ComCallWrapper pCCW = DACGetCCWFromAddress(ccw);
    PTR_SimpleComCallWrapper pSimpleCCW = pCCW->GetSimpleWrapper();

    ccwData->outerIUnknown = TO_CDADDR(pSimpleCCW->m_pOuter);
    ccwData->refCount      = pSimpleCCW->GetRefCount();
    ccwData->isNeutered    = pSimpleCCW->IsNeutered();
    ccwData->ccwAddress    = TO_CDADDR(dac_cast<TADDR>(pCCW));
    
    ccwData->jupiterRefCount = pSimpleCCW->GetJupiterRefCount();
    ccwData->isPegged = pSimpleCCW->IsPegged();
    ccwData->isGlobalPegged = RCWWalker::IsGlobalPeggingOn();
    ccwData->hasStrongRef = pCCW->IsWrapperActive();
    ccwData->handle = pCCW->GetObjectHandle();
    ccwData->isExtendsCOMObject = pCCW->GetSimpleWrapper()->IsExtendsCOMObject();
    ccwData->isAggregated = pCCW->GetSimpleWrapper()->IsAggregated();

    if (pCCW->GetObjectHandle() != NULL)
        ccwData->managedObject = PTR_CDADDR(ObjectFromHandle(pCCW->GetObjectHandle()));

    // count the number of COM vtables
    ccwData->interfaceCount = 0;
    while (pCCW != NULL)
    {
        for (int i = 0; i < ComCallWrapper::NumVtablePtrs; i++)
        {
            if (DACGetCOMIPFromCCW(pCCW, i) != NULL)
                ccwData->interfaceCount++;
        }
        pCCW = ComCallWrapper::GetNext(pCCW);
    }
    
    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif
}

HRESULT ClrDataAccess::GetCCWInterfaces(CLRDATA_ADDRESS ccw, unsigned int count, struct DacpCOMInterfacePointerData interfaces[], unsigned int *pNeeded)
{
    if (ccw == 0)
        return E_INVALIDARG;

#ifdef FEATURE_COMINTEROP
    SOSDacEnter();
    PTR_ComCallWrapper pCCW = DACGetCCWFromAddress(ccw);

    if (interfaces == NULL)
    {
        if (pNeeded)
        {
            unsigned int c = 0;
            while (pCCW != NULL)
            {
                for (int i = 0; i < ComCallWrapper::NumVtablePtrs; i++)
                    if (DACGetCOMIPFromCCW(pCCW, i) != NULL)
                        c++;
                pCCW = ComCallWrapper::GetNext(pCCW);
            }

            *pNeeded = c;
        }
        else
        {
            hr = E_INVALIDARG;
        }
    }
    else
    {
        ZeroMemory(interfaces, sizeof(DacpCOMInterfacePointerData) * count);

        PTR_ComCallWrapperTemplate pCCWTemplate = pCCW->GetSimpleWrapper()->GetComCallWrapperTemplate();
        unsigned int itemIndex = 0;
        unsigned int wrapperOffset = 0;
        while (pCCW != NULL && SUCCEEDED(hr))
        {
            for (int i = 0; i < ComCallWrapper::NumVtablePtrs && SUCCEEDED(hr); i++)
            {
                PTR_IUnknown pUnk = DACGetCOMIPFromCCW(pCCW, i);
                if (pUnk != NULL)
                {
                    if (itemIndex >= count)
                    {
                        // the outBuffer is too small
                        hr = E_INVALIDARG;
                        break;
                    }

                    interfaces[itemIndex].interfacePtr = PTR_CDADDR(pUnk);

                    // if this is the first ComCallWrapper, the 0th vtable slots is special
                    if (wrapperOffset == 0 && i == ComCallWrapper::Slot_Basic)
                    {
                        // this is IDispatch/IUnknown
                        interfaces[itemIndex].methodTable = NULL;
                    }
                    else
                    {
                        // this slot represents the class interface or an interface implemented by the class
                        DWORD ifaceMapIndex = wrapperOffset + i - ComCallWrapper::Slot_FirstInterface;

                        PTR_ComMethodTable pCMT = ComMethodTable::ComMethodTableFromIP(pUnk);
                        interfaces[itemIndex].methodTable = PTR_CDADDR(pCMT->GetMethodTable());
                    }
                
                    itemIndex++;
                }
            }

            pCCW = ComCallWrapper::GetNext(pCCW);
            wrapperOffset += ComCallWrapper::NumVtablePtrs;
        }

        if (SUCCEEDED(hr) && pNeeded)
            *pNeeded = itemIndex;
    }

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif
}

HRESULT ClrDataAccess::GetObjectExceptionData(CLRDATA_ADDRESS objAddr, struct DacpExceptionObjectData *data)
{
    if (data == NULL)
        return E_POINTER;

    SOSDacEnter();

    PTR_ExceptionObject pObj = dac_cast<PTR_ExceptionObject>(TO_TADDR(objAddr));

    data->Message         = TO_CDADDR(dac_cast<TADDR>(pObj->GetMessage()));
    data->InnerException  = TO_CDADDR(dac_cast<TADDR>(pObj->GetInnerException()));
    data->StackTrace      = TO_CDADDR(dac_cast<TADDR>(pObj->GetStackTraceArrayObject()));
    data->WatsonBuckets   = TO_CDADDR(dac_cast<TADDR>(pObj->GetWatsonBucketReference()));
    data->StackTraceString = TO_CDADDR(dac_cast<TADDR>(pObj->GetStackTraceString()));
    data->RemoteStackTraceString = TO_CDADDR(dac_cast<TADDR>(pObj->GetRemoteStackTraceString()));
    data->HResult         = pObj->GetHResult();
    data->XCode           = pObj->GetXCode();

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::IsRCWDCOMProxy(CLRDATA_ADDRESS rcwAddr, BOOL* isDCOMProxy)
{
    if (isDCOMProxy == nullptr)
    {
        return E_POINTER;
    }

    *isDCOMProxy = FALSE;

#ifdef FEATURE_COMINTEROP
    SOSDacEnter();

    PTR_RCW pRCW = dac_cast<PTR_RCW>(CLRDATA_ADDRESS_TO_TADDR(rcwAddr));
    *isDCOMProxy = pRCW->IsDCOMProxy();

    SOSDacLeave();

    return S_OK;
#else
    return E_NOTIMPL;
#endif // FEATURE_COMINTEROP
}
