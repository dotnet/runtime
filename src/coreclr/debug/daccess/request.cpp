// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: request.cpp
//

//
// CorDataAccess::Request implementation.
//
//*****************************************************************************

#include "stdafx.h"

#include "typestring.h"
#include <gccover.h>
#include <virtualcallstub.h>

#ifdef FEATURE_COMINTEROP
#include <comcallablewrapper.h>
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
#include <interoplibinterface.h>
#include <interoplibabi.h>

typedef DPTR(InteropLibInterface::ExternalObjectContextBase) PTR_ExternalObjectContext;
typedef DPTR(InteropLib::ABI::ManagedObjectWrapperLayout) PTR_ManagedObjectWrapper;
#endif // FEATURE_COMWRAPPERS

#ifndef TARGET_UNIX
// It is unfortunate having to include this header just to get the definition of GenericModeBlock
#include <msodw.h>
#endif // TARGET_UNIX

// To include definition of IsThrowableThreadAbortException
#include <exstatecommon.h>

#include "rejit.h"
#include "request_common.h"

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

#if TARGET_64BIT
    Value = Value & ~7; // equivalent to Object::GetGCSafeMethodTable()
#else
    Value = Value & ~3; // equivalent to Object::GetGCSafeMethodTable()
#endif
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

BOOL DacValidateEEClass(PTR_EEClass pEEClass)
{
    // Verify things are right.
    // The EEClass method table pointer should match the method table.
    // TODO: Microsoft, need another test for validity, this one isn't always true anymore.
    BOOL retval = TRUE;
    EX_TRY
    {
        PTR_MethodTable pMethodTable = pEEClass->GetMethodTable();
        if (!pMethodTable)
        {
            // PREfix.
            retval = FALSE;
        }
        else if (pEEClass != pMethodTable->GetClass())
        {
            retval = FALSE;
        }
    }
    EX_CATCH
    {
        retval = FALSE; // Something is wrong
    }
    EX_END_CATCH(SwallowAllExceptions)
    return retval;

}

BOOL DacValidateMethodTable(PTR_MethodTable pMT, BOOL &bIsFree)
{
    bIsFree = FALSE;

    if ((pMT == NULL) || dac_cast<TADDR>(pMT) == (TADDR)-1)
    {
        return FALSE;
    }

    // Verify things are right.
    BOOL retval = FALSE;
    EX_TRY
    {
        if (HOST_CDADDR(pMT) == HOST_CDADDR(g_pFreeObjectMethodTable))
        {
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
    EX_CATCH
    {
        retval = FALSE; // Something is wrong
    }
    EX_END_CATCH(SwallowAllExceptions)
    return retval;

}

BOOL DacValidateMD(PTR_MethodDesc pMD)
{
    if ((pMD == NULL) || dac_cast<TADDR>(pMD) == (TADDR)-1)
    {
        return FALSE;
    }

    // Verify things are right.
    BOOL retval = TRUE;
    EX_TRY
    {
        PTR_MethodTable pMethodTable = pMD->GetMethodTable();

        // Standard fast check
        if ((pMethodTable == NULL) || dac_cast<TADDR>(pMethodTable) == (TADDR)-1)
        {
            retval = FALSE;
        }

        if (retval && !pMethodTable->ValidateWithPossibleAV())
        {
            retval = FALSE;
        }

        if (retval && (pMD->GetSlot() >= pMethodTable->GetNumVtableSlots() && !pMD->HasNonVtableSlot()))
        {
            retval = FALSE;
        }

        if (retval)
        {
            MethodDesc *pMDCheck = MethodDesc::GetMethodDescFromStubAddr(pMD->GetTemporaryEntryPoint(), TRUE);

            if (PTR_HOST_TO_TADDR(pMD) != PTR_HOST_TO_TADDR(pMDCheck))
            {
                retval = FALSE;
            }
        }

        if (retval && pMD->HasNativeCode() && !pMD->IsFCall())
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
    EX_CATCH
    {
        retval = FALSE; // Something is wrong
    }
    EX_END_CATCH(SwallowAllExceptions)
    return retval;
}

BOOL DacValidateMD(LPCVOID pMD)
{
    return DacValidateMD(dac_cast<PTR_MethodDesc>(pMD));
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
ClrDataAccess::GetHillClimbingLogEntry(CLRDATA_ADDRESS addr, struct DacpHillClimbingLogEntry* entry)
{
    return E_NOTIMPL;
}

HRESULT
ClrDataAccess::GetWorkRequestData(CLRDATA_ADDRESS addr, struct DacpWorkRequestData *workRequestData)
{
    return E_NOTIMPL;
}

HRESULT
ClrDataAccess::GetThreadpoolData(struct DacpThreadpoolData *threadpoolData)
{
    return E_NOTIMPL;
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
        threadStoreData->fHostConfig = FALSE;

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
    }
    else if (pNeeded)
    {
        *pNeeded = 1;
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

    PTR_MethodTable mTable = PTR_MethodTable(TO_TADDR(mt));
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
            CodeHeap *codeHeap = heapList->pHeap;
            codeHeaps[i] = DACGetHeapInfoForCodeHeap(codeHeap);
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

DacpJitCodeHeapInfo ClrDataAccess::DACGetHeapInfoForCodeHeap(CodeHeap *heapAddr)
{
    DacpJitCodeHeapInfo jitCodeHeapInfo;

    TADDR targetVtblPtrForHeapType = VPTR_HOST_VTABLE_TO_TADDR(*(LPVOID*)heapAddr);
    if (targetVtblPtrForHeapType == LoaderCodeHeap::VPtrTargetVTable())
    {
        LoaderCodeHeap *loaderCodeHeap = PTR_LoaderCodeHeap(PTR_HOST_TO_TADDR(heapAddr));
        jitCodeHeapInfo.codeHeapType = CODEHEAP_LOADER;
        jitCodeHeapInfo.LoaderHeap =
            TO_CDADDR(PTR_HOST_MEMBER_TADDR(LoaderCodeHeap, loaderCodeHeap, m_LoaderHeap));
    }
    else if (targetVtblPtrForHeapType == HostCodeHeap::VPtrTargetVTable())
    {
        HostCodeHeap *hostCodeHeap = PTR_HostCodeHeap(PTR_HOST_TO_TADDR(heapAddr));
        jitCodeHeapInfo.codeHeapType = CODEHEAP_HOST;
        jitCodeHeapInfo.HostData.baseAddr = PTR_CDADDR(hostCodeHeap->m_pBaseAddr);
        jitCodeHeapInfo.HostData.currentAddr = PTR_CDADDR(hostCodeHeap->m_pLastAvailableCommittedAddr);
    }
    else
    {
        jitCodeHeapInfo.codeHeapType = CODEHEAP_UNKNOWN;
    }

    return jitCodeHeapInfo;
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
ClrDataAccess::GetRegisterName(int regNum, unsigned int count, _Inout_updates_z_(count) WCHAR *buffer, unsigned int *pNeeded)
{
    if (!buffer && !pNeeded)
        return E_POINTER;

#ifdef TARGET_AMD64
    static const WCHAR *regs[] =
    {
        W("rax"), W("rcx"), W("rdx"), W("rbx"), W("rsp"), W("rbp"), W("rsi"), W("rdi"),
        W("r8"), W("r9"), W("r10"), W("r11"), W("r12"), W("r13"), W("r14"), W("r15"),
    };
#elif defined(TARGET_ARM)
    static const WCHAR *regs[] =
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
#elif defined(TARGET_ARM64)
    static const WCHAR *regs[] =
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
        W("X28"), W("Fp"),  W("Lr"),  W("Sp")
    };
#elif defined(TARGET_X86)
    static const WCHAR *regs[] =
    {
        W("eax"), W("ecx"), W("edx"), W("ebx"), W("esp"), W("ebp"), W("esi"), W("edi"),
    };
#elif defined(TARGET_LOONGARCH64)
    static const WCHAR *regs[] =
    {
        W("R0"), W("AT"), W("V0"), W("V1"),
        W("A0"), W("A1"), W("A2"), W("A3"),
        W("A4"), W("A5"), W("A6"), W("A7"),
        W("T0"), W("T1"), W("T2"), W("T3"),
        W("T8"), W("T9"), W("S0"), W("S1"),
        W("S2"), W("S3"), W("S4"), W("S5"),
        W("S6"), W("S7"), W("K0"), W("K1"),
        W("GP"), W("SP"), W("FP"), W("RA")
    };
#elif defined(TARGET_RISCV64)
    static const WCHAR *regs[] =
    {
        W("R0"), W("RA"), W("SP"), W("GP"),
        W("TP"), W("T0"), W("T1"), W("T2"),
        W("FP"), W("S1"), W("A0"), W("A1"),
        W("A2"), W("A3"), W("A4"), W("A5"),
        W("A6"), W("A7"), W("S2"), W("S3"),
        W("S4"), W("S5"), W("S6"), W("S7"),
        W("S8"), W("S9"), W("S10"), W("S11"),
        W("T3"), W("T4"), W("T5"), W("T6")
    };
#endif

    // Caller frame registers are encoded as "-(reg+1)".
    bool callerFrame = regNum < 0;
    if (callerFrame)
        regNum = -regNum-1;

    if ((unsigned int)regNum >= ARRAY_SIZE(regs))
        return E_UNEXPECTED;

    const WCHAR callerPrefix[] = W("caller.");
    // Include null terminator in prefixLen/regLen because wcscpy_s will fail otherwise
    unsigned int prefixLen = (unsigned int)ARRAY_SIZE(callerPrefix);
    unsigned int regLen = (unsigned int)u16_strlen(regs[regNum]) + 1;
    unsigned int needed = (callerFrame ? prefixLen - 1 : 0) + regLen;
    if (pNeeded)
        *pNeeded = needed;

    if (buffer)
    {
        WCHAR* curr = buffer;
        WCHAR* end = buffer + count;
        unsigned int destSize = count;
        if (curr < end && callerFrame)
        {
            unsigned int toCopy = prefixLen < destSize ? prefixLen : destSize;
            wcscpy_s(curr, toCopy, callerPrefix);
            // Point to null terminator
            toCopy--;
            curr += toCopy;
            destSize -= toCopy;
        }

        if (curr < end)
        {
            unsigned int toCopy = regLen < destSize ? regLen : destSize;
            wcscpy_s(curr, toCopy, regs[regNum]);
            // Point to null terminator
            toCopy--;
            curr += toCopy;
            destSize -= toCopy;
        }

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

    DacStackReferenceWalker *walker = new (nothrow) DacStackReferenceWalker(this, osThreadID, false);

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

    data->allocBytes = TO_CDADDR(thread->m_alloc_context.gc_alloc_context.alloc_bytes);
    data->allocBytesLoh = TO_CDADDR(thread->m_alloc_context.gc_alloc_context.alloc_bytes_uoh);

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
    if (GCHeapUtilities::IsServerHeap())
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
            DPTR(unused_generation) table = g_gcDacGlobals->generation_table;
            for (unsigned int i=0; i < *g_gcDacGlobals->max_gen + 2; i++)
            {
                dac_generation entry = GenerationTableIndex(table, i);
                data[0].allocData[i].allocBytes = (CLRDATA_ADDRESS)(ULONG_PTR) entry.allocation_context.alloc_bytes;
                data[0].allocData[i].allocBytesLoh = (CLRDATA_ADDRESS)(ULONG_PTR) entry.allocation_context.alloc_bytes_uoh;
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
    threadData->osThreadId = (DWORD)thread->m_OSThreadId;
    threadData->state = thread->m_State;
    threadData->preemptiveGCDisabled = thread->m_fPreemptiveGCDisabled;
    threadData->allocContextPtr = TO_CDADDR(thread->m_alloc_context.gc_alloc_context.alloc_ptr);
    threadData->allocContextLimit = TO_CDADDR(thread->m_alloc_context.gc_alloc_context.alloc_limit);

    threadData->fiberData = NULL;

    threadData->pFrame = PTR_CDADDR(thread->m_pFrame);
    threadData->context = PTR_CDADDR(thread->m_pDomain);
    threadData->domain = PTR_CDADDR(thread->m_pDomain);
    threadData->lockCount = (DWORD)-1;
#ifndef TARGET_UNIX
    threadData->teb = TO_CDADDR(thread->m_pTEB);
#else
    threadData->teb = NULL;
#endif
    threadData->lastThrownObjectHandle =
        TO_CDADDR(thread->m_LastThrownObjectHandle);
    threadData->nextThread =
        HOST_CDADDR(ThreadStore::s_pThreadStore->m_ThreadList.GetNext(thread));
#ifdef FEATURE_EH_FUNCLETS
    if (thread->m_ExceptionState.m_pCurrentTracker)
    {
        threadData->firstNestedException = PTR_HOST_TO_TADDR(
            thread->m_ExceptionState.m_pCurrentTracker->m_pPrevNestedInfo);
    }
#else
    threadData->firstNestedException = PTR_HOST_TO_TADDR(
        thread->m_ExceptionState.m_currentExInfo.m_pPrevNestedInfo);
#endif // FEATURE_EH_FUNCLETS

    SOSDacLeave();
    return hr;
}

#ifdef FEATURE_REJIT
void CopyNativeCodeVersionToReJitData(NativeCodeVersion nativeCodeVersion, NativeCodeVersion activeCodeVersion, DacpReJitData * pReJitData)
{
    pReJitData->rejitID = nativeCodeVersion.GetILCodeVersion().GetVersionId();
    pReJitData->NativeCodeAddr = nativeCodeVersion.GetNativeCode();

    if (nativeCodeVersion != activeCodeVersion)
    {
        pReJitData->flags = DacpReJitData::kReverted;
    }
    else
    {
        switch (nativeCodeVersion.GetILCodeVersion().GetRejitState())
        {
        default:
            _ASSERTE(!"Unknown SharedRejitInfo state.  DAC should be updated to understand this new state.");
            pReJitData->flags = DacpReJitData::kUnknown;
            break;

        case ILCodeVersion::kStateRequested:
            pReJitData->flags = DacpReJitData::kRequested;
            break;

        case ILCodeVersion::kStateActive:
            pReJitData->flags = DacpReJitData::kActive;
            break;
        }
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
        ZeroMemory(methodDescData, sizeof(DacpMethodDescData));
        if (rgRevertedRejitData != NULL)
            ZeroMemory(rgRevertedRejitData, sizeof(*rgRevertedRejitData) * cRevertedRejitVersions);
        if (pcNeededRevertedRejitData != NULL)
            *pcNeededRevertedRejitData = 0;

        NativeCodeVersion requestedNativeCodeVersion, activeNativeCodeVersion;
        if (ip != NULL)
        {
            requestedNativeCodeVersion = ExecutionManager::GetNativeCodeVersion(CLRDATA_ADDRESS_TO_TADDR(ip));
        }
        else
        {
#ifdef FEATURE_CODE_VERSIONING
            activeNativeCodeVersion = pMD->GetCodeVersionManager()->GetActiveILCodeVersion(pMD).GetActiveNativeCodeVersion(pMD);
#else
            activeNativeCodeVersion = NativeCodeVersion(pMD);
#endif
            requestedNativeCodeVersion = activeNativeCodeVersion;
        }

        methodDescData->requestedIP = ip;
        methodDescData->bIsDynamic = (pMD->IsLCGMethod()) ? TRUE : FALSE;
        methodDescData->wSlotNumber = pMD->GetSlot();
        if (!requestedNativeCodeVersion.IsNull() && requestedNativeCodeVersion.GetNativeCode() != NULL)
        {
            methodDescData->bHasNativeCode = TRUE;
            methodDescData->NativeCodeAddr = TO_CDADDR(PCODEToPINSTR(requestedNativeCodeVersion.GetNativeCode()));
        }
        else
        {
            methodDescData->bHasNativeCode = FALSE;
            methodDescData->NativeCodeAddr = (CLRDATA_ADDRESS)-1;
        }
        methodDescData->AddressOfNativeCodeSlot = pMD->HasNativeCodeSlot() ? TO_CDADDR(dac_cast<TADDR>(pMD->GetAddrOfNativeCodeSlot())) : NULL;
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
            CodeVersionManager *pCodeVersionManager = pMD->GetCodeVersionManager();

            // Current ReJitInfo
            if (activeNativeCodeVersion.IsNull())
            {
                ILCodeVersion activeILCodeVersion = pCodeVersionManager->GetActiveILCodeVersion(pMD);
                activeNativeCodeVersion = activeILCodeVersion.GetActiveNativeCodeVersion(pMD);
            }
            CopyNativeCodeVersionToReJitData(
                activeNativeCodeVersion,
                activeNativeCodeVersion,
                &methodDescData->rejitDataCurrent);

            // Requested ReJitInfo
            _ASSERTE(methodDescData->rejitDataRequested.rejitID == 0);
            if (ip != NULL && !requestedNativeCodeVersion.IsNull())
            {
                CopyNativeCodeVersionToReJitData(
                    requestedNativeCodeVersion,
                    activeNativeCodeVersion,
                    &methodDescData->rejitDataRequested);
            }

            // Total number of jitted rejit versions
            ULONG cJittedRejitVersions;
            if (SUCCEEDED(ReJitManager::GetReJITIDs(pMD, 0 /* cReJitIds */, &cJittedRejitVersions, NULL /* reJitIds */)))
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
                ReJITID *rgReJitIds = reJitIds.OpenRawBuffer(cRevertedRejitVersions + 1);
                if (rgReJitIds != NULL)
                {
                    hr = ReJitManager::GetReJITIDs(pMD, cRevertedRejitVersions + 1, &cReJitIds, rgReJitIds);
                    if (SUCCEEDED(hr))
                    {
                        // Go through rejitids.  For each reverted one, populate a entry in rgRevertedRejitData
                        reJitIds.CloseRawBuffer(cReJitIds);
                        ULONG iRejitDataReverted = 0;
                        ILCodeVersion activeVersion = pCodeVersionManager->GetActiveILCodeVersion(pMD);
                        for (COUNT_T i = 0;
                             (i < cReJitIds) && (iRejitDataReverted < cRevertedRejitVersions);
                             i++)
                        {
                            ILCodeVersion ilCodeVersion = pCodeVersionManager->GetILCodeVersion(pMD, reJitIds[i]);

                            if ((ilCodeVersion.IsNull()) ||
                                (ilCodeVersion == activeVersion))
                            {
                                continue;
                            }

                            NativeCodeVersion activeRejitChild = ilCodeVersion.GetActiveNativeCodeVersion(pMD);
                            CopyNativeCodeVersionToReJitData(
                                activeRejitChild,
                                activeNativeCodeVersion,
                                &rgRevertedRejitData[iRejitDataReverted]);
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
        hr = S_OK; // Failure to get rejitids is not fatal

#endif // FEATURE_REJIT

#ifdef HAVE_GCCOVER
        if (!requestedNativeCodeVersion.IsNull())
        {
            PTR_GCCoverageInfo gcCover = requestedNativeCodeVersion.GetGCCoverageInfo();
            if (gcCover != NULL)
            {
                // In certain minidumps, we won't save the gccover information.
                // (it would be unwise to do so, it is heavy and not a customer scenario).
                methodDescData->GCStressCodeCopy = HOST_CDADDR(gcCover) + offsetof(GCCoverageInfo, savedCode);
            }
        }
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
                        FieldDesc *pField = (&g_CoreLib)->GetField(FIELD__DYNAMICRESOLVER__DYNAMIC_METHOD);
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

HRESULT ClrDataAccess::GetTieredVersions(
    CLRDATA_ADDRESS methodDesc,
    int rejitId,
    struct DacpTieredVersionData *nativeCodeAddrs,
    int cNativeCodeAddrs,
    int *pcNativeCodeAddrs)
{
    if (methodDesc == 0 || cNativeCodeAddrs == 0 || pcNativeCodeAddrs == NULL)
    {
        return E_INVALIDARG;
    }

    *pcNativeCodeAddrs = 0;

    SOSDacEnter();

#ifdef FEATURE_REJIT
    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));

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
        CodeVersionManager *pCodeVersionManager = pMD->GetCodeVersionManager();
        ILCodeVersion ilCodeVersion = pCodeVersionManager->GetILCodeVersion(pMD, rejitId);

        if (ilCodeVersion.IsNull())
        {
            // Bad rejit ID
            hr = E_INVALIDARG;
            goto cleanup;
        }

        TADDR r2rImageBase = NULL;
        TADDR r2rImageEnd = NULL;
        {
            PTR_Module pModule = (PTR_Module)pMD->GetModule();
            if (pModule->IsReadyToRun())
            {
                PTR_PEImageLayout pImage = pModule->GetReadyToRunInfo()->GetImage();
                r2rImageBase = dac_cast<TADDR>(pImage->GetBase());
                r2rImageEnd = r2rImageBase + pImage->GetSize();
            }
        }

        NativeCodeVersionCollection nativeCodeVersions = ilCodeVersion.GetNativeCodeVersions(pMD);
        int count = 0;
        for (NativeCodeVersionIterator iter = nativeCodeVersions.Begin(); iter != nativeCodeVersions.End(); iter++)
        {
            TADDR pNativeCode = PCODEToPINSTR((*iter).GetNativeCode());
            nativeCodeAddrs[count].NativeCodeAddr = pNativeCode;
            PTR_NativeCodeVersionNode pNode = (*iter).AsNode();
            nativeCodeAddrs[count].NativeCodeVersionNodePtr = TO_CDADDR(PTR_TO_TADDR(pNode));

            if (r2rImageBase <= pNativeCode && pNativeCode < r2rImageEnd)
            {
                nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_ReadyToRun;
            }
            else if (pMD->IsEligibleForTieredCompilation())
            {
                switch ((*iter).GetOptimizationTier())
                {
                default:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_Unknown;
                    break;
                case NativeCodeVersion::OptimizationTier0:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_QuickJitted;
                    break;
                case NativeCodeVersion::OptimizationTier1:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_OptimizedTier1;
                    break;
                case NativeCodeVersion::OptimizationTier1OSR:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_OptimizedTier1OSR;
                    break;
                case NativeCodeVersion::OptimizationTierOptimized:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_Optimized;
                    break;
                case NativeCodeVersion::OptimizationTier0Instrumented:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_QuickJittedInstrumented;
                    break;
                case NativeCodeVersion::OptimizationTier1Instrumented:
                    nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_OptimizedTier1Instrumented;
                    break;
                }
            }
            else if (pMD->IsJitOptimizationDisabled())
            {
                nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_MinOptJitted;
            }
            else
            {
                nativeCodeAddrs[count].OptimizationTier = DacpTieredVersionData::OptimizationTier_Optimized;
            }

            ++count;

            if (count >= cNativeCodeAddrs)
            {
                hr = S_FALSE;
                break;
            }
        }

        *pcNativeCodeAddrs = count;
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions)

cleanup:
    ;
#endif // FEATURE_REJIT

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodDescTransparencyData(CLRDATA_ADDRESS methodDesc, struct DacpMethodDescTransparencyData *data)
{
    if (methodDesc == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));
    if (!DacValidateMD(pMD))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(data, sizeof(DacpMethodDescTransparencyData));
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
        size_t methodSize = codeInfo.GetCodeManager()->GetFunctionSize(codeInfo.GetGCInfoToken());
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
ClrDataAccess::GetMethodDescName(CLRDATA_ADDRESS methodDesc, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded)
{
    if (methodDesc == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    MethodDesc* pMD = PTR_MethodDesc(TO_TADDR(methodDesc));
    StackSString str;

    EX_TRY
    {
        TypeString::AppendMethodInternal(str, pMD, TypeString::FormatSignature|TypeString::FormatNamespace|TypeString::FormatFullInst);
    }
    EX_CATCH
    {
        hr = E_FAIL;
        if (pMD->IsDynamicMethod())
        {
            if (pMD->IsLCGMethod() || pMD->IsILStub())
            {
                // In heap dumps, trying to format the signature can fail
                // in certain cases.

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
                WCHAR path[MAX_LONGPATH];
                COUNT_T nChars = 0;
                if (pModule->GetPath().DacGetUnicode(ARRAY_SIZE(path), path, &nChars) &&
                    nChars > 0 && nChars <= ARRAY_SIZE(path))
                {
                    WCHAR* pFile = path + nChars - 1;
                    while ((pFile >= path) && (*pFile != DIRECTORY_SEPARATOR_CHAR_W))
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
    EX_END_CATCH(SwallowAllExceptions)

    if (SUCCEEDED(hr))
    {

        const WCHAR *val = str.GetUnicode();

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

    *domain = contextAddr; // Context is same as the AppDomain in CoreCLR

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetObjectStringData(CLRDATA_ADDRESS obj, unsigned int count, _Inout_updates_z_(count) WCHAR *stringData, unsigned int *pNeeded)
{
    if (obj == 0)
        return E_INVALIDARG;

    if ((stringData == 0 || count <= 0) && (pNeeded == NULL))
        return E_INVALIDARG;

    SOSDacEnter();

    TADDR mtTADDR = DACGetMethodTableFromObjectPointer(TO_TADDR(obj), m_pTarget);
    PTR_MethodTable mt = PTR_MethodTable(mtTADDR);

    // Object must be a string
    BOOL bFree = FALSE;
    if (!DacValidateMethodTable(mt, bFree))
        hr = E_INVALIDARG;
    else if (HOST_CDADDR(mt) != HOST_CDADDR(g_pStringClass))
        hr = E_INVALIDARG;

    if (SUCCEEDED(hr))
    {
        PTR_StringObject str(TO_TADDR(obj));
        ULONG32 needed = (ULONG32)str->GetStringLength() + 1;

        if (stringData && count > 0)
        {
            if (count > needed)
                count = needed;

            TADDR pszStr = TO_TADDR(obj)+offsetof(StringObject, m_FirstChar);
            hr = m_pTarget->ReadVirtual(pszStr, (PBYTE)stringData, count * sizeof(WCHAR), &needed);

            if (SUCCEEDED(hr))
                stringData[count - 1] = W('\0');
            else
                stringData[0] = W('\0');
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
ClrDataAccess::GetObjectClassName(CLRDATA_ADDRESS obj, unsigned int count, _Inout_updates_z_(count) WCHAR *className, unsigned int *pNeeded)
{
    if (obj == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    // Don't turn the Object into a pointer, it is too costly on
    // scans of the gc heap.
    PTR_MethodTable mt = NULL;
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
        PEAssembly *pPEAssembly = mt->GetModule()->GetPEAssembly();
        if (pPEAssembly->GetPEImage() == NULL)
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
            const WCHAR *val = s.GetUnicode();

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
    ModuleData->PEAssembly = HOST_CDADDR(pModule->GetPEAssembly());
    COUNT_T metadataSize = 0;
    if (!pModule->GetPEAssembly()->IsDynamic())
    {
        ModuleData->ilBase = (CLRDATA_ADDRESS)(ULONG_PTR) pModule->GetPEAssembly()->GetIJWBase();
    }

    ModuleData->metadataStart = (CLRDATA_ADDRESS)dac_cast<TADDR>(pModule->GetPEAssembly()->GetLoadedMetadata(&metadataSize));
    ModuleData->metadataSize = (SIZE_T) metadataSize;

    ModuleData->bIsReflection = pModule->IsReflection();
    ModuleData->bIsPEFile = pModule->IsPEFile();
    ModuleData->Assembly = HOST_CDADDR(pModule->GetAssembly());
    ModuleData->dwModuleID = pModule->GetModuleID();
    ModuleData->dwModuleIndex = pModule->GetModuleIndex().m_dwIndex;
    ModuleData->dwTransientFlags = pModule->m_dwTransientFlags;
    ModuleData->LoaderAllocator = HOST_CDADDR(pModule->m_loaderAllocator);
    ModuleData->ThunkHeap = HOST_CDADDR(pModule->m_pThunkHeap);

    EX_TRY
    {
        //
        // In minidump's case, these data structure is not avaiable.
        //
        ModuleData->TypeDefToMethodTableMap = PTR_CDADDR(pModule->m_TypeDefToMethodTableMap.pTable);
        ModuleData->TypeRefToMethodTableMap = PTR_CDADDR(pModule->m_TypeRefToMethodTableMap.pTable);
        ModuleData->MethodDefToDescMap = PTR_CDADDR(pModule->m_MethodDefToDescMap.pTable);
        ModuleData->FieldDefToDescMap = PTR_CDADDR(pModule->m_FieldDefToDescMap.pTable);
        ModuleData->MemberRefToDescMap = PTR_CDADDR(pModule->m_MemberRefMap.pTable);
        ModuleData->ManifestModuleReferencesMap = PTR_CDADDR(pModule->m_ManifestModuleReferencesMap.pTable);

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

    PTR_MethodTable pMT = PTR_MethodTable(TO_TADDR(mt));
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
            MTData->wNumInterfaces = (WORD)pMT->GetNumInterfaces();
            MTData->wNumMethods = pMT->GetNumMethods();
            MTData->wNumVtableSlots = pMT->GetNumVtableSlots();
            MTData->wNumVirtuals = pMT->GetNumVirtuals();
            MTData->cl = pMT->GetCl();
            MTData->dwAttrClass = pMT->GetAttrClass();
            MTData->bContainsPointers = pMT->ContainsPointers();
            MTData->bIsShared = FALSE;
            MTData->bIsDynamic = pMT->IsDynamicStatics();
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableName(CLRDATA_ADDRESS mt, unsigned int count, _Inout_updates_z_(count) WCHAR *mtName, unsigned int *pNeeded)
{
    if (mt == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    PTR_MethodTable pMT = PTR_MethodTable(TO_TADDR(mt));
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
        PEAssembly *pPEAssembly = pMT->GetModule()->GetPEAssembly();
        if (pPEAssembly->GetPEImage() == NULL)
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
            EX_TRY
            {
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

                TypeString::AppendType(s, TypeHandle(pMT), TypeString::FormatNamespace|TypeString::FormatFullInst);

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
            }
            EX_CATCH
            {
                if (!MdCacheGetEEName(dac_cast<TADDR>(pMT), s))
                {
                    EX_RETHROW;
                }
            }
            EX_END_CATCH(SwallowAllExceptions)
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

            if (s.IsEmpty())
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                const WCHAR *val = s.GetUnicode();

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
    FieldDescData->bIsContextLocal = FALSE;
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

    PTR_MethodTable pMT = PTR_MethodTable(TO_TADDR(mt));
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

        data->wContextStaticsSize = 0;
        data->wContextStaticOffset = 0;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetMethodTableCollectibleData(CLRDATA_ADDRESS mt, struct DacpMethodTableCollectibleData *data)
{
    if (mt == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    PTR_MethodTable pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!pMT || !DacValidateMethodTable(pMT, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        data->bCollectible = pMT->Collectible();
        if (data->bCollectible)
        {
            data->LoaderAllocatorObjectHandle = pMT->GetLoaderAllocatorObjectHandle();
        }
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

    PTR_MethodTable pMT = PTR_MethodTable(TO_TADDR(mt));
    BOOL bIsFree = FALSE;
    if (!DacValidateMethodTable(pMT, bIsFree))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        ZeroMemory(pTransparencyData, sizeof(DacpMethodTableTransparencyData));
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

    PTR_EEClass pClass = PTR_EEClass(TO_TADDR(eeClass));
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
ClrDataAccess::GetFrameName(CLRDATA_ADDRESS vtable, unsigned int count, _Inout_updates_z_(count) WCHAR *frameName, unsigned int *pNeeded)
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
        unsigned int len = (unsigned int)u16_strlen(pszName);

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
ClrDataAccess::GetPEFileName(CLRDATA_ADDRESS addr, unsigned int count, _Inout_updates_z_(count) WCHAR *fileName, unsigned int *pNeeded)
{
    if (addr == 0 || (fileName == NULL && pNeeded == NULL) || (fileName != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    PEAssembly* pPEAssembly = PTR_PEAssembly(TO_TADDR(addr));

    // Turn from bytes to wide characters
    if (!pPEAssembly->GetPath().IsEmpty())
    {
        if (!pPEAssembly->GetPath().DacGetUnicode(count, fileName, pNeeded))
            hr = E_FAIL;
    }
    else if (!pPEAssembly->IsDynamic())
    {
        StackSString displayName;
        pPEAssembly->GetDisplayName(displayName, 0);

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

    PEAssembly* pPEAssembly = PTR_PEAssembly(TO_TADDR(addr));

    // More fields later?
    if (!pPEAssembly->IsDynamic())
        *base = TO_CDADDR(pPEAssembly->GetIJWBase());
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
    PTR_MethodTable mt = NULL;
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

                TypeHandle thElem = mt->GetArrayElementTypeHandle();

                TypeHandle thCur  = thElem;
                while (thCur.IsArray())
                    thCur = thCur.GetArrayElementTypeHandle();

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

    AppDomain* appDomain = AppDomain::GetCurrentDomain();
    unsigned int i = 0;
    if (appDomain != NULL && i < count)
    {
        if (values)
            values[0] = HOST_CDADDR(appDomain);

        i = 1;
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
    adsData->sharedDomain = NULL;

    // Get an accurate count of appdomains.
    adsData->DomainCount = 0;
    if (AppDomain::GetCurrentDomain() != NULL)
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

        if (pBaseDomain->IsAppDomain())
        {
            AppDomain * pAppDomain = pBaseDomain->AsAppDomain();
            appdomainData->DomainLocalBlock = 0;
            appdomainData->pDomainLocalModules = 0;

            appdomainData->dwId = DefaultADID;
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
        if (pResult)
            *pResult = pAssembly->error;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyLocation(CLRDATA_ADDRESS assembly, unsigned int count,
                                         _Inout_updates_z_(count) WCHAR *location, unsigned int *pNeeded)
{
    if (assembly == NULL || (location == NULL && pNeeded == NULL) || (location != NULL && count == 0))
        return E_INVALIDARG;

    SOSDacEnter();
    FailedAssembly* pAssembly = PTR_FailedAssembly(TO_TADDR(assembly));

    if (pNeeded)
        *pNeeded = 1;

    if (location)
        location[0] = 0;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetFailedAssemblyDisplayName(CLRDATA_ADDRESS assembly, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded)
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
    if (pBaseDomain->IsAppDomain())
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
ClrDataAccess::GetAppDomainName(CLRDATA_ADDRESS addr, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded)
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
                                  _Inout_updates_z_(count) WCHAR *base, unsigned int *pNeeded)
{
    // Method is not supported on CoreCLR

    return E_FAIL;
}

HRESULT
ClrDataAccess::GetPrivateBinPaths(CLRDATA_ADDRESS appDomain, int count,
                                  _Inout_updates_z_(count) WCHAR *paths, unsigned int *pNeeded)
{
    // Method is not supported on CoreCLR

    return E_FAIL;
}

HRESULT
ClrDataAccess::GetAppDomainConfigFile(CLRDATA_ADDRESS appDomain, int count,
                                      _Inout_updates_z_(count) WCHAR *configFile, unsigned int *pNeeded)
{
    // Method is not supported on CoreCLR

    return E_FAIL;
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
    assemblyData->isDynamic = pAssembly->IsDynamic();
    assemblyData->ModuleCount = 0;
    assemblyData->isDomainNeutral = FALSE;

    if (pAssembly->GetModule())
    {
        assemblyData->ModuleCount++;
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetAssemblyName(CLRDATA_ADDRESS assembly, unsigned int count, _Inout_updates_z_(count) WCHAR *name, unsigned int *pNeeded)
{
    SOSDacEnter();
    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));

    if (name)
        name[0] = 0;

    if (!pAssembly->GetPEAssembly()->GetPath().IsEmpty())
    {
        if (!pAssembly->GetPEAssembly()->GetPath().DacGetUnicode(count, name, pNeeded))
            hr = E_FAIL;
        else if (name)
            name[count-1] = 0;
    }
    else if (!pAssembly->GetPEAssembly()->IsDynamic())
    {
        StackSString displayName;
        pAssembly->GetPEAssembly()->GetDisplayName(displayName, 0);

        const WCHAR *val = displayName.GetUnicode();

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
ClrDataAccess::GetAssemblyLocation(CLRDATA_ADDRESS assembly, int count, _Inout_updates_z_(count) WCHAR *location, unsigned int *pNeeded)
{
    if ((assembly == NULL) || (location == NULL && pNeeded == NULL) || (location != NULL && count == 0))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    Assembly* pAssembly = PTR_Assembly(TO_TADDR(assembly));

    // Turn from bytes to wide characters
    if (!pAssembly->GetPEAssembly()->GetPath().IsEmpty())
    {
        if (!pAssembly->GetPEAssembly()->GetPath().DacGetUnicode(count, location, pNeeded))
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
    if (modules)
    {
        if (pAssembly->GetModule() && count > 0)
            modules[0] = HOST_CDADDR(pAssembly->GetModule());
    }

    if (pNeeded)
        *pNeeded = 1;

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
    if (!GCHeapUtilities::IsServerHeap())
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
    // Make sure ClrDataAccess::ServerGCHeapDetails() is updated as well.
    if (detailsData == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    detailsData->heapAddr = NULL;

    detailsData->lowest_address = PTR_CDADDR(g_lowest_address);
    detailsData->highest_address = PTR_CDADDR(g_highest_address);
    if (IsBackgroundGCEnabled())
    {
        detailsData->current_c_gc_state = (CLRDATA_ADDRESS)*g_gcDacGlobals->current_c_gc_state;
        detailsData->mark_array = (CLRDATA_ADDRESS)*g_gcDacGlobals->mark_array;
        detailsData->next_sweep_obj = (CLRDATA_ADDRESS)*g_gcDacGlobals->next_sweep_obj;
        detailsData->background_saved_lowest_address = (CLRDATA_ADDRESS)*g_gcDacGlobals->background_saved_lowest_address;
        detailsData->background_saved_highest_address = (CLRDATA_ADDRESS)*g_gcDacGlobals->background_saved_highest_address;
    }
    else
    {
        detailsData->current_c_gc_state = 0;
        detailsData->mark_array = -1;
        detailsData->next_sweep_obj = 0;
        detailsData->background_saved_lowest_address = 0;
        detailsData->background_saved_highest_address = 0;
    }

    detailsData->alloc_allocated = (CLRDATA_ADDRESS)*g_gcDacGlobals->alloc_allocated;
    detailsData->ephemeral_heap_segment = (CLRDATA_ADDRESS)*g_gcDacGlobals->ephemeral_heap_segment;
    detailsData->card_table = PTR_CDADDR(g_card_table);

    if (IsRegionGCEnabled())
    {
        // with regions, we don't have these variables anymore
        // use special value -1 in saved_sweep_ephemeral_seg to signal the region case
        detailsData->saved_sweep_ephemeral_seg = (CLRDATA_ADDRESS)-1;
        detailsData->saved_sweep_ephemeral_start = 0;
    }
    else
    {
        if (IsBackgroundGCEnabled())
        {
            detailsData->saved_sweep_ephemeral_seg = (CLRDATA_ADDRESS)*g_gcDacGlobals->saved_sweep_ephemeral_seg;
            detailsData->saved_sweep_ephemeral_start = (CLRDATA_ADDRESS)*g_gcDacGlobals->saved_sweep_ephemeral_start;
        }
        else
        {
            detailsData->saved_sweep_ephemeral_seg = 0;
            detailsData->saved_sweep_ephemeral_start = 0;
        }
    }

    // get bounds for the different generations
    for (unsigned int i=0; i < DAC_NUMBERGENERATIONS; i++)
    {
        dac_generation generation = GenerationTableIndex(g_gcDacGlobals->generation_table, i);
        detailsData->generation_table[i].start_segment = (CLRDATA_ADDRESS) dac_cast<TADDR>(generation.start_segment);
        detailsData->generation_table[i].allocation_start = (CLRDATA_ADDRESS) generation.allocation_start;
        gc_alloc_context alloc_context = generation.allocation_context;
        detailsData->generation_table[i].allocContextPtr = (CLRDATA_ADDRESS)alloc_context.alloc_ptr;
        detailsData->generation_table[i].allocContextLimit = (CLRDATA_ADDRESS)alloc_context.alloc_limit;
    }

    if (g_gcDacGlobals->finalize_queue.IsValid())
    {
        DPTR(dac_finalize_queue) fq = Dereference(g_gcDacGlobals->finalize_queue);
        DPTR(uint8_t*) fillPointersTable = dac_cast<TADDR>(fq) + offsetof(dac_finalize_queue, m_FillPointers);
        for (unsigned int i = 0; i < DAC_NUMBERGENERATIONS + 3; i++)
        {
            detailsData->finalization_fill_pointers[i] = (CLRDATA_ADDRESS)*TableIndex(fillPointersTable, i, sizeof(uint8_t*));
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

    if (GCHeapUtilities::IsServerHeap())
    {
#if !defined(FEATURE_SVR_GC)
        _ASSERTE(0);
#else // !defined(FEATURE_SVR_GC)
        hr = GetServerHeapData(seg, heapSegment);
#endif //!defined(FEATURE_SVR_GC)
    }
    else
    {
        dac_heap_segment *pSegment = __DPtr<dac_heap_segment>(TO_TADDR(seg));
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

            if (seg == (CLRDATA_ADDRESS)*g_gcDacGlobals->ephemeral_heap_segment)
            {
                heapSegment->highAllocMark = (CLRDATA_ADDRESS)*g_gcDacGlobals->alloc_allocated;
            }
            else
            {
                heapSegment->highAllocMark = heapSegment->allocated;
            }
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
    if (GCHeapUtilities::IsServerHeap())
    {
#if !defined(FEATURE_SVR_GC)
        _ASSERTE(0);
#else // !defined(FEATURE_SVR_GC)
        unsigned int heapCount = GCHeapCount();
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

    // we need to check and see if g_heap_type
    // is GC_HEAP_INVALID, in which case we fail.
    ULONG32 gcHeapValue = g_heap_type;

    // GC_HEAP_TYPE has three possible values:
    //       GC_HEAP_INVALID = 0,
    //       GC_HEAP_WKS     = 1,
    //       GC_HEAP_SVR     = 2
    // If we get something other than that, we probably read the wrong location.
    _ASSERTE(gcHeapValue >= GC_HEAP_INVALID && gcHeapValue <= GC_HEAP_SVR);

    // we have GC_HEAP_INVALID if gcHeapValue == 0, so we're done - we haven't
    // initialized the heap yet.
    if (gcHeapValue == GC_HEAP_INVALID)
    {
        hr = E_FAIL;
        goto cleanup;
    }

    // Now we can get other important information about the heap
    // We can use GCHeapUtilities::IsServerHeap here because we have already validated
    // that the heap is in a valid state. We couldn't use it above, because IsServerHeap
    // asserts if the heap type is GC_HEAP_INVALID.
    gcheapData->g_max_generation = *g_gcDacGlobals->max_gen;
    gcheapData->bServerMode = GCHeapUtilities::IsServerHeap();
    gcheapData->bGcStructuresValid = *g_gcDacGlobals->gc_structures_invalid_cnt == 0;

    if (GCHeapUtilities::IsServerHeap())
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

cleanup:
    ;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetOOMStaticData(struct DacpOomData *oomData)
{
    if (oomData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    *oomData = {};

    if (!GCHeapUtilities::IsServerHeap())
    {
        oom_history* pOOMInfo = g_gcDacGlobals->oom_info;
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
    *data = {};

    if (!GCHeapUtilities::IsServerHeap())
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
ClrDataAccess::GetGCGlobalMechanisms(size_t* globalMechanisms)
{
#ifdef GC_CONFIG_DRIVEN
    if (globalMechanisms == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    memset(globalMechanisms, 0, (sizeof(size_t) * MAX_GLOBAL_GC_MECHANISMS_COUNT));

    for (int i = 0; i < MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
    {
        globalMechanisms[i] = g_gcDacGlobals->gc_global_mechanisms[i];
    }

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif //GC_CONFIG_DRIVEN
}

HRESULT
ClrDataAccess::GetGCInterestingInfoStaticData(struct DacpGCInterestingInfoData *data)
{
#ifdef GC_CONFIG_DRIVEN
    if (data == NULL)
        return E_INVALIDARG;

    static_assert_no_msg(DAC_NUMBERGENERATIONS == NUMBERGENERATIONS);
    static_assert_no_msg(DAC_NUM_GC_DATA_POINTS == NUM_GC_DATA_POINTS);
    static_assert_no_msg(DAC_MAX_COMPACT_REASONS_COUNT == MAX_COMPACT_REASONS_COUNT);
    static_assert_no_msg(DAC_MAX_EXPAND_MECHANISMS_COUNT == MAX_EXPAND_MECHANISMS_COUNT);
    static_assert_no_msg(DAC_MAX_GC_MECHANISM_BITS_COUNT == MAX_GC_MECHANISM_BITS_COUNT);

    SOSDacEnter();
    *data = {};

    if (g_heap_type != GC_HEAP_SVR)
    {
        for (int i = 0; i < NUM_GC_DATA_POINTS; i++)
            data->interestingDataPoints[i] = g_gcDacGlobals->interesting_data_per_heap[i];
        for (int i = 0; i < MAX_COMPACT_REASONS_COUNT; i++)
            data->compactReasons[i] = g_gcDacGlobals->compact_reasons_per_heap[i];
        for (int i = 0; i < MAX_EXPAND_MECHANISMS_COUNT; i++)
            data->expandMechanisms[i] = g_gcDacGlobals->expand_mechanisms_per_heap[i];
        for (int i = 0; i < MAX_GC_MECHANISM_BITS_COUNT; i++)
            data->bitMechanisms[i] = g_gcDacGlobals->interesting_mechanism_bits_per_heap[i];
    }
    else
    {
        hr = E_FAIL;
    }

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif //GC_CONFIG_DRIVEN
}

HRESULT
ClrDataAccess::GetGCInterestingInfoData(CLRDATA_ADDRESS interestingInfoAddr, struct DacpGCInterestingInfoData *data)
{
#ifdef GC_CONFIG_DRIVEN
    if (interestingInfoAddr == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    *data = {};

    if (!GCHeapUtilities::IsServerHeap())
        hr = E_FAIL; // doesn't make sense to call this on WKS mode

#ifdef FEATURE_SVR_GC
    else
        hr = ServerGCInterestingInfoData(interestingInfoAddr, data);
#else
    _ASSERTE_MSG(false, "IsServerHeap returned true but FEATURE_SVR_GC not defined");
    hr = E_NOTIMPL;
#endif //FEATURE_SVR_GC

    SOSDacLeave();
    return hr;
#else
    return E_NOTIMPL;
#endif //GC_CONFIG_DRIVEN
}

HRESULT
ClrDataAccess::GetHeapAnalyzeData(CLRDATA_ADDRESS addr, struct  DacpGcHeapAnalyzeData *data)
{
    if (addr == 0 || data == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    if (!GCHeapUtilities::IsServerHeap())
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

    analyzeData->internal_root_array = dac_cast<TADDR>(g_gcDacGlobals->internal_root_array);
    analyzeData->internal_root_array_index = *g_gcDacGlobals->internal_root_array_index;
    analyzeData->heap_analyze_success = *g_gcDacGlobals->heap_analyze_success;

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetUsefulGlobals(struct DacpUsefulGlobalsData *globalsData)
{
    if (globalsData == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    TypeHandle objArray = g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT];
    if (objArray != NULL)
        globalsData->ArrayMethodTable = HOST_CDADDR(objArray.AsMethodTable());
    else
        globalsData->ArrayMethodTable = 0;

    globalsData->StringMethodTable = HOST_CDADDR(g_pStringClass);
    globalsData->ObjectMethodTable = HOST_CDADDR(g_pObjectClass);
    globalsData->ExceptionMethodTable = HOST_CDADDR(g_pExceptionClass);
    globalsData->FreeMethodTable = HOST_CDADDR(g_pFreeObjectMethodTable);

    SOSDacLeave();
    return hr;
}


HRESULT
ClrDataAccess::GetNestedExceptionData(CLRDATA_ADDRESS exception, CLRDATA_ADDRESS *exceptionObject, CLRDATA_ADDRESS *nextNestedException)
{
    if (exception == 0 || exceptionObject == NULL || nextNestedException == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

#ifdef FEATURE_EH_FUNCLETS
    ExceptionTrackerBase *pExData = PTR_ExceptionTrackerBase(TO_TADDR(exception));
#else
    ExInfo *pExData = PTR_ExInfo(TO_TADDR(exception));
#endif // FEATURE_EH_FUNCLETS

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
    DomainLocalModule* pLocalModule = PTR_DomainLocalModule(pModule->GetDomainLocalModule());
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
ClrDataAccess::GetDomainLocalModuleDataFromAppDomain(CLRDATA_ADDRESS appDomainAddr, int moduleID, struct DacpDomainLocalModuleData *pLocalModuleData)
{
    // CoreCLR does not support multi-appdomain shared assembly loading. Thus, a non-pointer sized moduleID cannot exist.
    return E_INVALIDARG;
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
    PTR_ThreadLocalBlock pLocalBlock = ThreadStatics::GetCurrentTLB(pThread);
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

    SOSDacLeave();
    return hr;
}


HRESULT ClrDataAccess::GetHandleEnum(ISOSHandleEnum **ppHandleEnum)
{
    unsigned int types[] = {HNDTYPE_WEAK_SHORT, HNDTYPE_WEAK_LONG, HNDTYPE_STRONG, HNDTYPE_PINNED, HNDTYPE_DEPENDENT,
                            HNDTYPE_SIZEDREF,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS) || defined(FEATURE_OBJCMARSHAL)
                            HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS || FEATURE_OBJCMARSHAL
                            };

    return GetHandleEnumForTypes(types, ARRAY_SIZE(types), ppHandleEnum);
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

    unsigned int types[] = {HNDTYPE_WEAK_SHORT, HNDTYPE_WEAK_LONG, HNDTYPE_STRONG, HNDTYPE_PINNED, HNDTYPE_DEPENDENT,
                            HNDTYPE_SIZEDREF,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS) || defined(FEATURE_OBJCMARSHAL)
                            HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS || FEATURE_OBJCMARSHAL
                            };

    DacHandleWalker *walker = new DacHandleWalker();

    HRESULT hr = walker->Init(this, types, ARRAY_SIZE(types), gen);
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

static HRESULT TraverseLoaderHeapBlock(PTR_LoaderHeapBlock firstBlock, VISITHEAP pFunc)
{
    // If we are given a bad address, we may end up mis-interpreting random memory
    // as a loader heap.  We'll do three things to try to avoid this:
    //  1.  Put a cap on the number of heaps we enumerate at some sensible number.
    //  2.  If we detect the block is bad, return a failure HRESULT.  Callers of
    //      this function need to check the return before acting on data given
    //      by the callback.
    //  3.  If we hit an exception, we'll return a failing HRESULT as before.
    const int iterationMax = 8192;

    int i = 0;
    PTR_LoaderHeapBlock block = firstBlock;

    while (block != nullptr && i++ < iterationMax)
    {
        if (!block.IsValid())
            return E_POINTER;

        TADDR addr = PTR_TO_TADDR(block->pVirtualAddress);
        size_t size = block->dwVirtualSize;

        BOOL bCurrentBlock = (block == firstBlock);
        pFunc(addr, size, bCurrentBlock);

        block = block->pNext;

        // Ensure we only see the first block once and that we aren't looping
        // infinitely.
        if (block == firstBlock)
            return E_POINTER;
    }

    return i < iterationMax ? S_OK : S_FALSE;
}

HRESULT
ClrDataAccess::TraverseLoaderHeap(CLRDATA_ADDRESS loaderHeapAddr, VISITHEAP pFunc)
{
    if (loaderHeapAddr == 0 || pFunc == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    hr = TraverseLoaderHeapBlock(PTR_LoaderHeap(TO_TADDR(loaderHeapAddr))->m_pFirstBlock, pFunc);

    SOSDacLeave();
    return hr;
}



HRESULT
ClrDataAccess::TraverseLoaderHeap(CLRDATA_ADDRESS loaderHeapAddr, LoaderHeapKind kind, VISITHEAP pCallback)
{
    if (loaderHeapAddr == 0 || pCallback == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    switch (kind)
    {
        case LoaderHeapKindNormal:
            hr = TraverseLoaderHeapBlock(PTR_LoaderHeap(TO_TADDR(loaderHeapAddr))->m_pFirstBlock, pCallback);
            break;

        case LoaderHeapKindExplicitControl:
            hr = TraverseLoaderHeapBlock(PTR_ExplicitControlLoaderHeap(TO_TADDR(loaderHeapAddr))->m_pFirstBlock, pCallback);
            break;

        default:
            hr = E_NOTIMPL;
            break;
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
    VirtualCallStubManager *pVcsMgr = pBaseDomain->GetLoaderAllocator()->GetVirtualCallStubManager();
    if (!pVcsMgr)
    {
        hr = E_POINTER;
    }
    else
    {
        PTR_LoaderHeap pLoaderHeap = NULL;
        switch(heaptype)
        {
            case IndcellHeap:
                pLoaderHeap = pVcsMgr->indcell_heap;
                break;

            case CacheEntryHeap:
                pLoaderHeap = pVcsMgr->cache_entry_heap;
                break;

            default:
                hr = E_INVALIDARG;
        }

        if (SUCCEEDED(hr))
        {
            hr = TraverseLoaderHeapBlock(pLoaderHeap->m_pFirstBlock, pFunc);
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetDomainLoaderAllocator(CLRDATA_ADDRESS domainAddress, CLRDATA_ADDRESS *pLoaderAllocator)
{
    if (pLoaderAllocator == nullptr)
        return E_INVALIDARG;

    if (domainAddress == 0)
    {
        *pLoaderAllocator = 0;
        return S_FALSE;
    }

    SOSDacEnter();

    PTR_BaseDomain pDomain = PTR_BaseDomain(TO_TADDR(domainAddress));
    *pLoaderAllocator = pDomain != nullptr ? HOST_CDADDR(pDomain->GetLoaderAllocator()) : 0;

    SOSDacLeave();
    return hr;
}

// The ordering of these entries must match the order enumerated in GetLoaderAllocatorHeaps.
// This array isn't fixed, we can reorder/add/remove entries as long as the corresponding
// code in GetLoaderAllocatorHeaps is updated to match.
static const char *LoaderAllocatorLoaderHeapNames[] =
{
    "LowFrequencyHeap",
    "HighFrequencyHeap",
    "StubHeap",
    "ExecutableHeap",
    "FixupPrecodeHeap",
    "NewStubPrecodeHeap",
    "IndcellHeap",
    "CacheEntryHeap",
};


HRESULT ClrDataAccess::GetLoaderAllocatorHeaps(CLRDATA_ADDRESS loaderAllocatorAddress, int count, CLRDATA_ADDRESS *pLoaderHeaps, LoaderHeapKind *pKinds, int *pNeeded)
{
    if (loaderAllocatorAddress == 0)
        return E_INVALIDARG;

    SOSDacEnter();

    const int loaderHeapCount = ARRAY_SIZE(LoaderAllocatorLoaderHeapNames);
    PTR_LoaderAllocator pLoaderAllocator = PTR_LoaderAllocator(TO_TADDR(loaderAllocatorAddress));

    if (pNeeded)
        *pNeeded = loaderHeapCount;

    if (pLoaderHeaps)
    {
        if (count < loaderHeapCount)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            // Must match order of LoaderAllocatorLoaderHeapNames
            int i = 0;
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetLowFrequencyHeap());
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetHighFrequencyHeap());
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetStubHeap());
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetExecutableHeap());
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetFixupPrecodeHeap());
            pLoaderHeaps[i++] = HOST_CDADDR(pLoaderAllocator->GetNewStubPrecodeHeap());

            VirtualCallStubManager *pVcsMgr = pLoaderAllocator->GetVirtualCallStubManager();
            if (pVcsMgr == nullptr)
            {
                for (; i < min(count, loaderHeapCount); i++)
                    pLoaderHeaps[i] = 0;
            }
            else
            {
                pLoaderHeaps[i++] = HOST_CDADDR(pVcsMgr->indcell_heap);
                pLoaderHeaps[i++] = HOST_CDADDR(pVcsMgr->cache_entry_heap);
            }

            // All of the above are "LoaderHeap" and not the ExplicitControl version.
            for (int j = 0; j < i; j++)
                pKinds[j] = LoaderHeapKindNormal;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT
ClrDataAccess::GetLoaderAllocatorHeapNames(int count, const char **ppNames, int *pNeeded)
{
    SOSDacEnter();

    const int loaderHeapCount = ARRAY_SIZE(LoaderAllocatorLoaderHeapNames);
    if (pNeeded)
        *pNeeded = loaderHeapCount;

    if (ppNames)
        for (int i = 0; i < min(count, loaderHeapCount); i++)
            ppNames[i] = LoaderAllocatorLoaderHeapNames[i];

    if (count < loaderHeapCount)
        hr = S_FALSE;

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
    pSyncBlockData->bFree = TRUE;

    if (pSyncBlockData->SyncBlockCount > 0 && SBNumber <= pSyncBlockData->SyncBlockCount)
    {
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

                pSyncBlockData->MonitorHeld = pBlock->m_Monitor.GetMonitorHeldStateVolatile();
                pSyncBlockData->Recursion = pBlock->m_Monitor.GetRecursionLevel();
                pSyncBlockData->HoldingThread = HOST_CDADDR(pBlock->m_Monitor.GetHoldingThread());
                pSyncBlockData->appDomainPtr = PTR_HOST_TO_TADDR(AppDomain::GetCurrentDomain());

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
ClrDataAccess::GetJitHelperFunctionName(CLRDATA_ADDRESS ip, unsigned int count, _Inout_updates_z_(count) char *name, unsigned int *pNeeded)
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

#ifdef TARGET_AMD64
    SOSDacEnter();

    if (!GetAnyThunkTarget(ctx, targetIP, targetMD))
        hr = E_FAIL;

    SOSDacLeave();
    return hr;
#else
    return E_FAIL;
#endif // TARGET_AMD64
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

#ifdef FEATURE_SVR_GC
    // If server GC, skip enumeration
    if (g_gcDacGlobals->g_heaps != nullptr)
        return;
#endif

    Dereference(g_gcDacGlobals->ephemeral_heap_segment).EnumMem();
    g_gcDacGlobals->alloc_allocated.EnumMem();
    g_gcDacGlobals->gc_structures_invalid_cnt.EnumMem();
    Dereference(g_gcDacGlobals->finalize_queue).EnumMem();

    // Enumerate the entire generation table, which has variable size
    EnumGenerationTable(dac_cast<TADDR>(g_gcDacGlobals->generation_table));

    if (g_gcDacGlobals->generation_table.IsValid())
    {
        ULONG first = IsRegionGCEnabled() ? 0 : (*g_gcDacGlobals->max_gen);
        // enumerating the first to max + 2 gives you
        // the segment list for all the normal segments plus the pinned heap segment (max + 2)
        // this is the convention in the GC so it is repeated here
        for (ULONG i = first; i <= *g_gcDacGlobals->max_gen + 2; i++)
        {
            dac_generation gen = GenerationTableIndex(g_gcDacGlobals->generation_table, i);
            __DPtr<dac_heap_segment> seg = dac_cast<TADDR>(gen.start_segment);
            while (seg)
            {
                DacEnumMemoryRegion(dac_cast<TADDR>(seg), sizeof(dac_heap_segment));
                seg = seg->next;
            }
        }
    }
}

HRESULT
ClrDataAccess::GetClrWatsonBuckets(CLRDATA_ADDRESS thread, void *pGenericModeBlock)
{
#ifdef TARGET_UNIX
	// This API is not available under TARGET_UNIX
	return E_FAIL;
#else // TARGET_UNIX
    if (thread == 0 || pGenericModeBlock == NULL)
        return E_INVALIDARG;

    SOSDacEnter();

    Thread * pThread = PTR_Thread(TO_TADDR(thread));
    hr = GetClrWatsonBucketsWorker(pThread, reinterpret_cast<GenericModeBlock *>(pGenericModeBlock));

    SOSDacLeave();
    return hr;
#endif // TARGET_UNIX
}

#ifndef TARGET_UNIX

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
            U1ARRAYREF refWatsonBucketArray = ((EXCEPTIONREF)oThrowable)->GetWatsonBucketReference();
            if (refWatsonBucketArray != NULL)
            {
                // Get the watson buckets from the throwable for non-preallocated
                // exceptions
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

#endif // TARGET_UNIX

HRESULT ClrDataAccess::GetTLSIndex(ULONG *pIndex)
{
    if (pIndex == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    if (g_TlsIndex == TLS_OUT_OF_INDEXES)
    {
        *pIndex = 0;
        hr = S_FALSE;
    }
    else
    {
        *pIndex = g_TlsIndex;
    }

    SOSDacLeave();
    return hr;
}

#ifndef TARGET_UNIX
extern "C" IMAGE_DOS_HEADER __ImageBase;
#endif

HRESULT ClrDataAccess::GetDacModuleHandle(HMODULE *phModule)
{
    if(phModule == NULL)
        return E_INVALIDARG;

#ifndef TARGET_UNIX
    *phModule = (HMODULE)&__ImageBase;
    return S_OK;
#else
    //  hModule is not available under TARGET_UNIX
    return E_FAIL;
#endif
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
    rcwData->isAggregated = pRCW->IsURTAggregated();
    rcwData->isContained = pRCW->IsURTContained();
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

#ifdef TARGET_ARM
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

#ifdef FEATURE_COMWRAPPERS
BOOL ClrDataAccess::DACGetComWrappersCCWVTableQIAddress(CLRDATA_ADDRESS ccwPtr, TADDR *vTableAddress, TADDR *qiAddress)
{
    _ASSERTE(vTableAddress != NULL && qiAddress != NULL);

    HRESULT hr = S_OK;
    ULONG32 bytesRead = 0;
    TADDR ccw = CLRDATA_ADDRESS_TO_TADDR(ccwPtr);
    *vTableAddress = NULL;
    if (FAILED(m_pTarget->ReadVirtual(ccw, (PBYTE)vTableAddress, sizeof(TADDR), &bytesRead))
        || bytesRead != sizeof(TADDR)
        || vTableAddress == NULL)
    {
        return FALSE;
    }

    *qiAddress = NULL;
    if (FAILED(m_pTarget->ReadVirtual(*vTableAddress, (PBYTE)qiAddress, sizeof(TADDR), &bytesRead))
        || bytesRead != sizeof(TADDR)
        || qiAddress == NULL)
    {
        return FALSE;
    }


#ifdef TARGET_ARM
    // clear the THUMB bit on qiAddress before comparing with known vtable entry
    *qiAddress &= ~THUMB_CODE;
#endif

    return TRUE;
}

BOOL ClrDataAccess::DACIsComWrappersCCW(CLRDATA_ADDRESS ccwPtr)
{
    TADDR vTableAddress = NULL;
    TADDR qiAddress = NULL;
    if (!DACGetComWrappersCCWVTableQIAddress(ccwPtr, &vTableAddress, &qiAddress))
    {
        return FALSE;
    }

    return (qiAddress == GetEEFuncEntryPoint(ManagedObjectWrapper_QueryInterface)
        || qiAddress == GetEEFuncEntryPoint(TrackerTarget_QueryInterface));
}

TADDR ClrDataAccess::DACGetManagedObjectWrapperFromCCW(CLRDATA_ADDRESS ccwPtr)
{
    if (!DACIsComWrappersCCW(ccwPtr))
    {
        return NULL;
    }

    ULONG32 bytesRead = 0;
    TADDR managedObjectWrapperPtrPtr = ccwPtr & InteropLib::ABI::DispatchThisPtrMask;
    TADDR managedObjectWrapperPtr = 0;
    if (FAILED(m_pTarget->ReadVirtual(managedObjectWrapperPtrPtr, (PBYTE)&managedObjectWrapperPtr, sizeof(TADDR), &bytesRead))
        || bytesRead != sizeof(TADDR))
    {
        return NULL;
    }

    return managedObjectWrapperPtr;
}

HRESULT ClrDataAccess::DACTryGetComWrappersHandleFromCCW(CLRDATA_ADDRESS ccwPtr, OBJECTHANDLE* objHandle)
{
    HRESULT hr = E_FAIL;
    TADDR ccw, managedObjectWrapperPtr;
    ULONG32 bytesRead = 0;
    OBJECTHANDLE handle;

    if (ccwPtr == 0 || objHandle == NULL)
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    if (!DACIsComWrappersCCW(ccwPtr))
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    ccw = CLRDATA_ADDRESS_TO_TADDR(ccwPtr);

    // Return ManagedObjectWrapper as an OBJECTHANDLE. (The OBJECTHANDLE is guaranteed to live at offset 0).
    managedObjectWrapperPtr = DACGetManagedObjectWrapperFromCCW(ccwPtr);
    if (managedObjectWrapperPtr == NULL)
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    IfFailGo(m_pTarget->ReadVirtual(managedObjectWrapperPtr, (PBYTE)&handle, sizeof(OBJECTHANDLE), &bytesRead));
    if (bytesRead != sizeof(OBJECTHANDLE))
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    *objHandle = handle;

    return S_OK;

ErrExit: return hr;
}

HRESULT ClrDataAccess::DACTryGetComWrappersObjectFromCCW(CLRDATA_ADDRESS ccwPtr, OBJECTREF* objRef)
{
    HRESULT hr = E_FAIL;

    if (ccwPtr == 0 || objRef == NULL)
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    OBJECTHANDLE handle;
    if (DACTryGetComWrappersHandleFromCCW(ccwPtr, &handle) != S_OK)
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    *objRef = ObjectFromHandle(handle);

    return S_OK;

ErrExit: return hr;
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
    return S_OK;
#else
    return E_NOTIMPL;
#endif // FEATURE_COMINTEROP
}

HRESULT ClrDataAccess::GetClrNotification(CLRDATA_ADDRESS arguments[], int count, int *pNeeded)
{
    SOSDacEnter();

    *pNeeded = MAX_CLR_NOTIFICATION_ARGS;

    if (g_clrNotificationArguments[0] == NULL)
    {
        hr = E_FAIL;
    }
    else
    {
        for (int i = 0; i < count && i < MAX_CLR_NOTIFICATION_ARGS; i++)
        {
            arguments[i] = g_clrNotificationArguments[i];
        }
    }

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::GetPendingReJITID(CLRDATA_ADDRESS methodDesc, int *pRejitId)
{
    if (methodDesc == 0 || pRejitId == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    *pRejitId = -1;
    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));

    CodeVersionManager* pCodeVersionManager = pMD->GetCodeVersionManager();
    CodeVersionManager::LockHolder codeVersioningLockHolder;
    ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pMD);
    if (ilVersion.IsNull())
    {
        hr = E_INVALIDARG;
    }
    else if (ilVersion.GetRejitState() == ILCodeVersion::kStateRequested)
    {
        *pRejitId = (int)ilVersion.GetVersionId();
    }
    else
    {
        hr = S_FALSE;
    }

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::GetReJITInformation(CLRDATA_ADDRESS methodDesc, int rejitId, struct DacpReJitData2 *pReJitData)
{
    if (methodDesc == 0 || rejitId < 0 || pReJitData == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));

    CodeVersionManager* pCodeVersionManager = pMD->GetCodeVersionManager();
    CodeVersionManager::LockHolder codeVersioningLockHolder;
    ILCodeVersion ilVersion = pCodeVersionManager->GetILCodeVersion(pMD, rejitId);
    if (ilVersion.IsNull())
    {
        hr = E_INVALIDARG;
    }
    else
    {
        pReJitData->rejitID = rejitId;

        switch (ilVersion.GetRejitState())
        {
        default:
            _ASSERTE(!"Unknown SharedRejitInfo state.  DAC should be updated to understand this new state.");
            pReJitData->flags = DacpReJitData2::kUnknown;
            break;

        case ILCodeVersion::kStateRequested:
            pReJitData->flags = DacpReJitData2::kRequested;
            break;

        case ILCodeVersion::kStateActive:
            pReJitData->flags = DacpReJitData2::kActive;
            break;
        }

        pReJitData->il = TO_CDADDR(PTR_TO_TADDR(ilVersion.GetIL()));
        PTR_ILCodeVersionNode nodePtr = ilVersion.IsDefaultVersion() ? NULL : ilVersion.AsNode();
        pReJitData->ilCodeVersionNodePtr = TO_CDADDR(PTR_TO_TADDR(nodePtr));
    }

    SOSDacLeave();

    return hr;
}


HRESULT ClrDataAccess::GetProfilerModifiedILInformation(CLRDATA_ADDRESS methodDesc, struct DacpProfilerILData *pILData)
{
    if (methodDesc == 0 || pILData == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    pILData->type = DacpProfilerILData::Unmodified;
    pILData->rejitID = 0;
    pILData->il = NULL;
    PTR_MethodDesc pMD = PTR_MethodDesc(TO_TADDR(methodDesc));

    CodeVersionManager* pCodeVersionManager = pMD->GetCodeVersionManager();
    CodeVersionManager::LockHolder codeVersioningLockHolder;
    ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pMD);
    if (ilVersion.GetRejitState() != ILCodeVersion::kStateActive || !ilVersion.HasDefaultIL())
    {
        pILData->type = DacpProfilerILData::ReJITModified;
        pILData->rejitID = static_cast<ULONG>(pCodeVersionManager->GetActiveILCodeVersion(pMD).GetVersionId());
    }

    TADDR pDynamicIL = pMD->GetModule()->GetDynamicIL(pMD->GetMemberDef(), TRUE);
    if (pDynamicIL != NULL)
    {
        pILData->type = DacpProfilerILData::ILModified;
        pILData->il = (CLRDATA_ADDRESS)pDynamicIL;
    }

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::GetMethodsWithProfilerModifiedIL(CLRDATA_ADDRESS mod, CLRDATA_ADDRESS *methodDescs, int cMethodDescs, int *pcMethodDescs)
{
    if (mod == 0 || methodDescs == NULL || cMethodDescs == 0 || pcMethodDescs == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    *pcMethodDescs = 0;

    PTR_Module pModule = PTR_Module(TO_TADDR(mod));
    CodeVersionManager* pCodeVersionManager = pModule->GetCodeVersionManager();
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    LookupMap<PTR_MethodTable>::Iterator typeIter(&pModule->m_TypeDefToMethodTableMap);
    for (int i = 0; typeIter.Next(); i++)
    {
        if (*pcMethodDescs >= cMethodDescs)
        {
            break;
        }

        if (typeIter.GetElement())
        {
            MethodTable* pMT = typeIter.GetElement();
            for (MethodTable::IntroducedMethodIterator itMethods(pMT, FALSE); itMethods.IsValid(); itMethods.Next())
            {
                PTR_MethodDesc pMD = dac_cast<PTR_MethodDesc>(itMethods.GetMethodDesc());

                TADDR pDynamicIL = pModule->GetDynamicIL(pMD->GetMemberDef(), TRUE);
                ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pMD);
                if (ilVersion.GetRejitState() != ILCodeVersion::kStateActive || !ilVersion.HasDefaultIL() || pDynamicIL != NULL)
                {
                    methodDescs[*pcMethodDescs] = PTR_CDADDR(pMD);
                    ++(*pcMethodDescs);
                }

                if (*pcMethodDescs >= cMethodDescs)
                {
                    break;
                }
            }
        }
    }

    SOSDacLeave();

    return hr;
}

HRESULT ClrDataAccess::GetNumberGenerations(unsigned int *pGenerations)
{
    if (pGenerations == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    *pGenerations = (unsigned int)(g_gcDacGlobals->total_generation_count);

    SOSDacLeave();
    return S_OK;
}

HRESULT ClrDataAccess::GetGenerationTable(unsigned int cGenerations, struct DacpGenerationData *pGenerationData, unsigned int *pNeeded)
{
    if (cGenerations > 0 && pGenerationData == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    HRESULT hr = S_OK;
    unsigned int numGenerationTableEntries = (unsigned int)(g_gcDacGlobals->total_generation_count);
    if (pNeeded != NULL)
    {
        *pNeeded = numGenerationTableEntries;
    }

    if (cGenerations < numGenerationTableEntries)
    {
        hr = S_FALSE;
    }
    else
    {
        if (g_gcDacGlobals->generation_table.IsValid())
        {
            for (unsigned int i = 0; i < numGenerationTableEntries; i++)
            {
                dac_generation generation = GenerationTableIndex(g_gcDacGlobals->generation_table, i);
                pGenerationData[i].start_segment = (CLRDATA_ADDRESS) dac_cast<TADDR>(generation.start_segment);

                pGenerationData[i].allocation_start = (CLRDATA_ADDRESS) generation.allocation_start;

                gc_alloc_context alloc_context = generation.allocation_context;
                pGenerationData[i].allocContextPtr = (CLRDATA_ADDRESS)alloc_context.alloc_ptr;
                pGenerationData[i].allocContextLimit = (CLRDATA_ADDRESS)alloc_context.alloc_limit;
            }
        }
        else
        {
            hr = E_FAIL;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetFinalizationFillPointers(unsigned int cFillPointers, CLRDATA_ADDRESS *pFinalizationFillPointers, unsigned int *pNeeded)
{
    if (cFillPointers > 0 && pFinalizationFillPointers == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    HRESULT hr = S_OK;
    unsigned int numFillPointers = (unsigned int)(g_gcDacGlobals->total_generation_count + dac_finalize_queue::ExtraSegCount);
    if (pNeeded != NULL)
    {
        *pNeeded = numFillPointers;
    }

    if (cFillPointers < numFillPointers)
    {
        hr = S_FALSE;
    }
    else
    {
        if (g_gcDacGlobals->finalize_queue.IsValid())
        {
            DPTR(dac_finalize_queue) fq = Dereference(g_gcDacGlobals->finalize_queue);
            DPTR(uint8_t*) fillPointersTable = dac_cast<TADDR>(fq) + offsetof(dac_finalize_queue, m_FillPointers);
            for (unsigned int i = 0; i < numFillPointers; i++)
            {
                pFinalizationFillPointers[i] = (CLRDATA_ADDRESS)*TableIndex(fillPointersTable, i, sizeof(uint8_t*));
            }
        }
        else
        {
            hr = E_FAIL;
        }
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetGenerationTableSvr(CLRDATA_ADDRESS heapAddr, unsigned int cGenerations, struct DacpGenerationData *pGenerationData, unsigned int *pNeeded)
{
    if (heapAddr == NULL || (cGenerations > 0 && pGenerationData == NULL))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    HRESULT hr = S_OK;
#ifdef FEATURE_SVR_GC
    unsigned int numGenerationTableEntries = (unsigned int)(g_gcDacGlobals->total_generation_count);
    if (pNeeded != NULL)
    {
        *pNeeded = numGenerationTableEntries;
    }

    if (cGenerations < numGenerationTableEntries)
    {
        hr = S_FALSE;
    }
    else
    {
        TADDR heapAddress = TO_TADDR(heapAddr);

        if (heapAddress != 0)
        {
            for (unsigned int i = 0; i < numGenerationTableEntries; ++i)
            {
                dac_generation generation = ServerGenerationTableIndex(heapAddress, i);
                pGenerationData[i].start_segment = (CLRDATA_ADDRESS)dac_cast<TADDR>(generation.start_segment);
                pGenerationData[i].allocation_start = (CLRDATA_ADDRESS)(ULONG_PTR)generation.allocation_start;
                gc_alloc_context alloc_context = generation.allocation_context;
                pGenerationData[i].allocContextPtr = (CLRDATA_ADDRESS)(ULONG_PTR)alloc_context.alloc_ptr;
                pGenerationData[i].allocContextLimit = (CLRDATA_ADDRESS)(ULONG_PTR)alloc_context.alloc_limit;
            }
        }
        else
        {
            hr = E_FAIL;
        }
    }
#else
        hr = E_NOTIMPL;
#endif

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetFinalizationFillPointersSvr(CLRDATA_ADDRESS heapAddr, unsigned int cFillPointers, CLRDATA_ADDRESS *pFinalizationFillPointers, unsigned int *pNeeded)
{
    if (heapAddr == NULL || (cFillPointers > 0 && pFinalizationFillPointers == NULL))
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    HRESULT hr = S_OK;
#ifdef FEATURE_SVR_GC
    unsigned int numFillPointers = (unsigned int)(g_gcDacGlobals->total_generation_count + dac_finalize_queue::ExtraSegCount);
    if (pNeeded != NULL)
    {
        *pNeeded = numFillPointers;
    }

    if (cFillPointers < numFillPointers)
    {
        hr = S_FALSE;
    }
    else
    {
        TADDR heapAddress = TO_TADDR(heapAddr);
        if (heapAddress != 0)
        {
            dac_gc_heap heap = LoadGcHeapData(heapAddress);
            dac_gc_heap* pHeap = &heap;
            DPTR(dac_finalize_queue) fq = pHeap->finalize_queue;
            DPTR(uint8_t*) pFillPointerArray= dac_cast<TADDR>(fq) + offsetof(dac_finalize_queue, m_FillPointers);
            for (unsigned int i = 0; i < numFillPointers; ++i)
            {
                pFinalizationFillPointers[i] = (CLRDATA_ADDRESS) pFillPointerArray[i];
            }
        }
        else
        {
            hr = E_FAIL;
        }
    }
#else
        hr = E_NOTIMPL;
#endif

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetAssemblyLoadContext(CLRDATA_ADDRESS methodTable, CLRDATA_ADDRESS* assemblyLoadContext)
{
    if (methodTable == 0 || assemblyLoadContext == NULL)
        return E_INVALIDARG;

    SOSDacEnter();
    PTR_MethodTable pMT = PTR_MethodTable(CLRDATA_ADDRESS_TO_TADDR(methodTable));
    PTR_Module pModule = pMT->GetModule();

    PTR_PEAssembly pPEAssembly = pModule->GetPEAssembly();
    PTR_AssemblyBinder pBinder = pPEAssembly->GetAssemblyBinder();

    INT_PTR managedAssemblyLoadContextHandle = pBinder->GetManagedAssemblyLoadContext();

    TADDR managedAssemblyLoadContextAddr = 0;
    if (managedAssemblyLoadContextHandle != 0)
    {
        DacReadAll(managedAssemblyLoadContextHandle,&managedAssemblyLoadContextAddr,sizeof(TADDR),true);
    }

    *assemblyLoadContext = TO_CDADDR(managedAssemblyLoadContextAddr);

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetBreakingChangeVersion(int* pVersion)
{
    if (pVersion == nullptr)
        return E_INVALIDARG;

    *pVersion = SOS_BREAKING_CHANGE_VERSION;
    return S_OK;
}

HRESULT ClrDataAccess::GetObjectComWrappersData(CLRDATA_ADDRESS objAddr, CLRDATA_ADDRESS *rcw, unsigned int count, CLRDATA_ADDRESS *mowList, unsigned int *pNeeded)
{
#ifdef FEATURE_COMWRAPPERS
    if (objAddr == 0 )
    {
        return E_INVALIDARG;
    }

    if (count > 0 && mowList == NULL)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();
    if (pNeeded != NULL)
    {
        *pNeeded = 0;
    }

    if (rcw != NULL)
    {
        *rcw = 0;
    }

    PTR_SyncBlock pSyncBlk = PTR_Object(TO_TADDR(objAddr))->PassiveGetSyncBlock();
    if (pSyncBlk != NULL)
    {
        PTR_InteropSyncBlockInfo pInfo = pSyncBlk->GetInteropInfoNoCreate();
        if (pInfo != NULL)
        {
            if (rcw != NULL)
            {
                *rcw = TO_TADDR(pInfo->m_externalComObjectContext);
            }

            DPTR(NewHolder<ManagedObjectComWrapperByIdMap>) mapHolder(PTR_TO_MEMBER_TADDR(InteropSyncBlockInfo, pInfo, m_managedObjectComWrapperMap));
            DPTR(ManagedObjectComWrapperByIdMap *)ppMap(PTR_TO_MEMBER_TADDR(NewHolder<ManagedObjectComWrapperByIdMap>, mapHolder, m_value));
            DPTR(ManagedObjectComWrapperByIdMap) pMap(TO_TADDR(*ppMap));

            CQuickArrayList<CLRDATA_ADDRESS> comWrappers;
            if (pMap != NULL)
            {
                ManagedObjectComWrapperByIdMap::Iterator iter = pMap->Begin();
                while (iter != pMap->End())
                {
                    comWrappers.Push(TO_CDADDR(iter->Value()));
                    ++iter;

                }
            }

            if (pNeeded != NULL)
            {
                *pNeeded = (unsigned int)comWrappers.Size();
            }

            for (SIZE_T pos = 0; pos < comWrappers.Size(); ++pos)
            {
                if (pos >= count)
                {
                    hr = S_FALSE;
                    break;
                }

                mowList[pos] = comWrappers[pos];
            }
        }
        else
        {
            hr = S_FALSE;
        }
    }
    else
    {
        hr = S_FALSE;
    }

    SOSDacLeave();
    return hr;
#else // FEATURE_COMWRAPPERS
    return E_NOTIMPL;
#endif // FEATURE_COMWRAPPERS
}

HRESULT ClrDataAccess::IsComWrappersCCW(CLRDATA_ADDRESS ccw, BOOL *isComWrappersCCW)
{
#ifdef FEATURE_COMWRAPPERS
    if (ccw == 0)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    if (isComWrappersCCW != NULL)
    {
        TADDR managedObjectWrapperPtr = DACGetManagedObjectWrapperFromCCW(ccw);
        *isComWrappersCCW = managedObjectWrapperPtr != NULL;
        hr = *isComWrappersCCW ? S_OK : S_FALSE;
    }

    SOSDacLeave();
    return hr;
#else // FEATURE_COMWRAPPERS
    return E_NOTIMPL;
#endif // FEATURE_COMWRAPPERS
}

HRESULT ClrDataAccess::GetComWrappersCCWData(CLRDATA_ADDRESS ccw, CLRDATA_ADDRESS *managedObject, int *refCount)
{
#ifdef FEATURE_COMWRAPPERS
    if (ccw == 0)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    TADDR managedObjectWrapperPtr = DACGetManagedObjectWrapperFromCCW(ccw);
    if (managedObjectWrapperPtr != NULL)
    {
        PTR_ManagedObjectWrapper pMOW(managedObjectWrapperPtr);

        if (managedObject != NULL)
        {
            OBJECTREF managedObjectRef;
            if (SUCCEEDED(DACTryGetComWrappersObjectFromCCW(ccw, &managedObjectRef)))
            {
                *managedObject = PTR_HOST_TO_TADDR(managedObjectRef);
            }
            else
            {
                *managedObject = 0;
            }
        }

        if (refCount != NULL)
        {
            *refCount = (int)pMOW->RefCount;
        }
    }
    else
    {
        // Not a ComWrappers CCW
        hr = E_INVALIDARG;
    }

    SOSDacLeave();
    return hr;
#else // FEATURE_COMWRAPPERS
    return E_NOTIMPL;
#endif // FEATURE_COMWRAPPERS
}

HRESULT ClrDataAccess::IsComWrappersRCW(CLRDATA_ADDRESS rcw, BOOL *isComWrappersRCW)
{
#ifdef FEATURE_COMWRAPPERS
    if (rcw == 0)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    if (isComWrappersRCW != NULL)
    {
        PTR_ExternalObjectContext pRCW(TO_TADDR(rcw));
        BOOL stillValid = TRUE;
        if(pRCW->SyncBlockIndex >= SyncBlockCache::s_pSyncBlockCache->m_SyncTableSize)
        {
            stillValid = FALSE;
        }

        PTR_SyncBlock pSyncBlk = NULL;
        if (stillValid)
        {
            PTR_SyncTableEntry ste = PTR_SyncTableEntry(dac_cast<TADDR>(g_pSyncTable) + (sizeof(SyncTableEntry) * pRCW->SyncBlockIndex));
            pSyncBlk = ste->m_SyncBlock;
            if(pSyncBlk == NULL)
            {
                stillValid = FALSE;
            }
        }

        PTR_InteropSyncBlockInfo pInfo = NULL;
        if (stillValid)
        {
            pInfo = pSyncBlk->GetInteropInfoNoCreate();
            if(pInfo == NULL)
            {
                stillValid = FALSE;
            }
        }

        if (stillValid)
        {
            stillValid = TO_TADDR(pInfo->m_externalComObjectContext) == PTR_HOST_TO_TADDR(pRCW);
        }

        *isComWrappersRCW = stillValid;
        hr = *isComWrappersRCW ? S_OK : S_FALSE;
    }

    SOSDacLeave();
    return hr;
#else // FEATURE_COMWRAPPERS
    return E_NOTIMPL;
#endif // FEATURE_COMWRAPPERS
}

HRESULT ClrDataAccess::GetComWrappersRCWData(CLRDATA_ADDRESS rcw, CLRDATA_ADDRESS *identity)
{
#ifdef FEATURE_COMWRAPPERS
    if (rcw == 0)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();

    PTR_ExternalObjectContext pEOC(TO_TADDR(rcw));
    if (identity != NULL)
    {
        *identity = PTR_CDADDR(pEOC->Identity);
    }

    SOSDacLeave();
    return hr;
#else // FEATURE_COMWRAPPERS
    return E_NOTIMPL;
#endif // FEATURE_COMWRAPPERS
}

namespace
{
    BOOL TryReadTaggedMemoryState(
        CLRDATA_ADDRESS objAddr,
        ICorDebugDataTarget* target,
        CLRDATA_ADDRESS *taggedMemory = NULL,
        size_t *taggedMemorySizeInBytes = NULL)
    {
        BOOL hasTaggedMemory = FALSE;

#ifdef FEATURE_OBJCMARSHAL
        EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
        {
            PTR_SyncBlock pSyncBlk = DACGetSyncBlockFromObjectPointer(CLRDATA_ADDRESS_TO_TADDR(objAddr), target);
            if (pSyncBlk != NULL)
            {
                PTR_InteropSyncBlockInfo pInfo = pSyncBlk->GetInteropInfoNoCreate();
                if (pInfo != NULL)
                {
                    CLRDATA_ADDRESS taggedMemoryLocal = PTR_CDADDR(pInfo->GetTaggedMemory());
                    if (taggedMemoryLocal != NULL)
                    {
                        hasTaggedMemory = TRUE;
                        if (taggedMemory)
                            *taggedMemory = taggedMemoryLocal;

                        if (taggedMemorySizeInBytes)
                            *taggedMemorySizeInBytes = pInfo->GetTaggedMemorySizeInBytes();
                    }
                }
            }
        }
        EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY;
#endif // FEATURE_OBJCMARSHAL

        return hasTaggedMemory;
    }
}

HRESULT ClrDataAccess::IsTrackedType(
    CLRDATA_ADDRESS objAddr,
    BOOL *isTrackedType,
    BOOL *hasTaggedMemory)
{
    if (objAddr == 0
        || isTrackedType == NULL
        || hasTaggedMemory == NULL)
    {
        return E_INVALIDARG;
    }

    *isTrackedType = FALSE;
    *hasTaggedMemory = FALSE;

    SOSDacEnter();

    TADDR mtTADDR = DACGetMethodTableFromObjectPointer(CLRDATA_ADDRESS_TO_TADDR(objAddr), m_pTarget);
    if (mtTADDR==NULL)
        hr = E_INVALIDARG;

    BOOL bFree = FALSE;
    PTR_MethodTable mt = NULL;
    if (SUCCEEDED(hr))
    {
        mt = PTR_MethodTable(mtTADDR);
        if (!DacValidateMethodTable(mt, bFree))
            hr = E_INVALIDARG;
    }

    if (SUCCEEDED(hr))
    {
        *isTrackedType = mt->IsTrackedReferenceWithFinalizer();
        hr = *isTrackedType ? S_OK : S_FALSE;
        *hasTaggedMemory = TryReadTaggedMemoryState(objAddr, m_pTarget);
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetTaggedMemory(
    CLRDATA_ADDRESS objAddr,
    CLRDATA_ADDRESS *taggedMemory,
    size_t *taggedMemorySizeInBytes)
{
    if (objAddr == 0
        || taggedMemory == NULL
        || taggedMemorySizeInBytes == NULL)
    {
        return E_INVALIDARG;
    }

    *taggedMemory = NULL;
    *taggedMemorySizeInBytes = 0;

    SOSDacEnter();

    if (FALSE == TryReadTaggedMemoryState(objAddr, m_pTarget, taggedMemory, taggedMemorySizeInBytes))
    {
        hr = S_FALSE;
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetGlobalAllocationContext(
        CLRDATA_ADDRESS *allocPtr,
        CLRDATA_ADDRESS *allocLimit)
{
    if (allocPtr == nullptr || allocLimit == nullptr)
    {
        return E_INVALIDARG;
    }

    SOSDacEnter();
    *allocPtr = (CLRDATA_ADDRESS)(g_global_alloc_context->alloc_ptr);
    *allocLimit = (CLRDATA_ADDRESS)(g_global_alloc_context->alloc_limit);
    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetHandleTableMemoryRegions(ISOSMemoryEnum** ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    SOSDacEnter();

    DacHandleTableMemoryEnumerator* htEnum = new (nothrow) DacHandleTableMemoryEnumerator();
    if (htEnum)
    {
        hr = htEnum->Init();

        if (SUCCEEDED(hr))
            hr = htEnum->QueryInterface(__uuidof(ISOSMemoryEnum), (void**)ppEnum);

        if (FAILED(hr))
            delete htEnum;
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::GetGCBookkeepingMemoryRegions(ISOSMemoryEnum** ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    SOSDacEnter();

    DacGCBookkeepingEnumerator* bkEnum = new (nothrow) DacGCBookkeepingEnumerator();
    if (bkEnum)
    {
        hr = bkEnum->Init();

        if (SUCCEEDED(hr))
            hr = bkEnum->QueryInterface(__uuidof(ISOSMemoryEnum), (void**)ppEnum);

        if (FAILED(hr))
            delete bkEnum;
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }

    SOSDacLeave();
    return hr;
}


HRESULT ClrDataAccess::GetGCFreeRegions(ISOSMemoryEnum **ppEnum)
{
    if (!ppEnum)
        return E_POINTER;

    SOSDacEnter();

    DacFreeRegionEnumerator* frEnum = new (nothrow) DacFreeRegionEnumerator();
    if (frEnum)
    {
        hr = frEnum->Init();

        if (SUCCEEDED(hr))
            hr = frEnum->QueryInterface(__uuidof(ISOSMemoryEnum), (void**)ppEnum);

        if (FAILED(hr))
            delete frEnum;
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }

    SOSDacLeave();
    return hr;
}

HRESULT ClrDataAccess::LockedFlush()
{
    SOSDacEnter();

    Flush();

    SOSDacLeave();
    return hr;
}
