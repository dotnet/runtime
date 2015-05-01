//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// codeman.cpp - a managment class for handling multiple code managers
//

//

#include "common.h"
#include "jitinterface.h"
#include "corjit.h"
#include "eetwain.h"
#include "eeconfig.h"
#include "excep.h"
#include "appdomain.hpp"
#include "codeman.h"
#include "nibblemapmacros.h"
#include "generics.h"
#include "dynamicmethod.h"
#include "eemessagebox.h"
#include "eventtrace.h"
#include "threadsuspend.h"

#include "exceptionhandling.h"

#include "rtlfunctions.h"

#include "jitperf.h"
#include "shimload.h"
#include "debuginfostore.h"
#include "strsafe.h"

#ifdef _WIN64
#define CHECK_DUPLICATED_STRUCT_LAYOUTS
#include "../debug/daccess/fntableaccess.h"
#endif // _WIN64

#define MAX_M_ALLOCATED         (16 * 1024)

// Default number of jump stubs in a jump stub block
#define DEFAULT_JUMPSTUBS_PER_BLOCK  32

SPTR_IMPL(EECodeManager, ExecutionManager, m_pDefaultCodeMan);

SPTR_IMPL(EEJitManager, ExecutionManager, m_pEEJitManager);
#ifdef FEATURE_PREJIT
SPTR_IMPL(NativeImageJitManager, ExecutionManager, m_pNativeImageJitManager);
#endif
#ifdef FEATURE_READYTORUN
SPTR_IMPL(ReadyToRunJitManager, ExecutionManager, m_pReadyToRunJitManager);
#endif

#ifndef DACCESS_COMPILE
Volatile<RangeSection *> ExecutionManager::m_CodeRangeList = NULL;
Volatile<LONG> ExecutionManager::m_dwReaderCount = 0;
Volatile<LONG> ExecutionManager::m_dwWriterLock = 0;
#else
SPTR_IMPL(RangeSection, ExecutionManager, m_CodeRangeList);
SVAL_IMPL(LONG, ExecutionManager, m_dwReaderCount);
SVAL_IMPL(LONG, ExecutionManager, m_dwWriterLock);
#endif

#ifndef DACCESS_COMPILE

CrstStatic ExecutionManager::m_JumpStubCrst;
CrstStatic ExecutionManager::m_RangeCrst;

#endif // DACCESS_COMPILE

#if defined(_TARGET_AMD64_) && !defined(DACCESS_COMPILE) // We don't do this on ARM just amd64

// Support for new style unwind information (to allow OS to stack crawl JIT compiled code).

typedef NTSTATUS (WINAPI* RtlAddGrowableFunctionTableFnPtr) (
        PVOID *DynamicTable, RUNTIME_FUNCTION* FunctionTable, ULONG EntryCount, 
        ULONG MaximumEntryCount, ULONG_PTR rangeStart, ULONG_PTR rangeEnd);
typedef VOID (WINAPI* RtlGrowFunctionTableFnPtr) (PVOID DynamicTable, ULONG NewEntryCount); 
typedef VOID (WINAPI* RtlDeleteGrowableFunctionTableFnPtr) (PVOID DynamicTable);

// OS entry points (only exist on Win8 and above)
static RtlAddGrowableFunctionTableFnPtr pRtlAddGrowableFunctionTable;
static RtlGrowFunctionTableFnPtr pRtlGrowFunctionTable;
static RtlDeleteGrowableFunctionTableFnPtr pRtlDeleteGrowableFunctionTable;
static Volatile<bool> RtlUnwindFtnsInited;

// statics for UnwindInfoTable 
Crst* UnwindInfoTable::s_pUnwindInfoTableLock = NULL;
Volatile<bool>      UnwindInfoTable::s_publishingActive = false;


#if _DEBUG
// Fake functions on Win7 checked build to excercize the code paths, they are no-ops
NTSTATUS WINAPI FakeRtlAddGrowableFunctionTable (
        PVOID *DynamicTable, RUNTIME_FUNCTION* FunctionTable, ULONG EntryCount, 
        ULONG MaximumEntryCount, ULONG_PTR rangeStart, ULONG_PTR rangeEnd) { *DynamicTable = (PVOID) 1; return 0; }
VOID WINAPI FakeRtlGrowFunctionTable (PVOID DynamicTable, ULONG NewEntryCount) { }
VOID WINAPI FakeRtlDeleteGrowableFunctionTable (PVOID DynamicTable) {}
#endif

/****************************************************************************/
// initialize the entry points for new win8 unwind info publishing functions.
// return true if the initialize is successful (the functions exist)

bool InitUnwindFtns()
{
    CONTRACTL {
        NOTHROW;
    } CONTRACTL_END;

#ifndef FEATURE_PAL
    if (!RtlUnwindFtnsInited)
    {
        HINSTANCE hNtdll = WszGetModuleHandle(W("ntdll.dll"));
        if (hNtdll != NULL) 
        {
            void* growFunctionTable = GetProcAddress(hNtdll, "RtlGrowFunctionTable");
            void* deleteGrowableFunctionTable = GetProcAddress(hNtdll, "RtlDeleteGrowableFunctionTable");
            void* addGrowableFunctionTable = GetProcAddress(hNtdll, "RtlAddGrowableFunctionTable");

            // All or nothing AddGroableFunctionTable is last (marker)
            if (growFunctionTable != NULL && 
                deleteGrowableFunctionTable != NULL && 
                addGrowableFunctionTable != NULL)
            {
                pRtlGrowFunctionTable = (RtlGrowFunctionTableFnPtr) growFunctionTable;
                pRtlDeleteGrowableFunctionTable = (RtlDeleteGrowableFunctionTableFnPtr) deleteGrowableFunctionTable;
                pRtlAddGrowableFunctionTable = (RtlAddGrowableFunctionTableFnPtr) addGrowableFunctionTable;
            } 
            // Don't call FreeLibrary(hNtdll) because GetModuleHandle did *NOT* increment the reference count!
        }
        else 
        {
#if _DEBUG
            pRtlGrowFunctionTable = FakeRtlGrowFunctionTable;
            pRtlDeleteGrowableFunctionTable = FakeRtlDeleteGrowableFunctionTable;
            pRtlAddGrowableFunctionTable = FakeRtlAddGrowableFunctionTable;
#endif
        }
        RtlUnwindFtnsInited = true;
    }
    return (pRtlAddGrowableFunctionTable != NULL);
#else // !FEATURE_PAL
    return false;
#endif // !FEATURE_PAL
}

/****************************************************************************/
UnwindInfoTable::UnwindInfoTable(ULONG_PTR rangeStart, ULONG_PTR rangeEnd, ULONG size)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(s_pUnwindInfoTableLock->OwnedByCurrentThread());
    _ASSERTE((rangeEnd - rangeStart) <= 0x7FFFFFFF);

    cTableCurCount = 0;
    cTableMaxCount = size;
    cDeletedEntries = 0;
    iRangeStart = rangeStart;
    iRangeEnd = rangeEnd;
    hHandle = NULL;
    pTable = new RUNTIME_FUNCTION[cTableMaxCount];
}

/****************************************************************************/
UnwindInfoTable::~UnwindInfoTable()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;   
    } CONTRACTL_END;
    _ASSERTE(s_publishingActive);

    // We do this lock free to because too many places still want no-trigger.   It should be OK
    // It would be cleaner if we could take the lock (we did not have to be GC_NOTRIGGER)
    UnRegister();
    delete[] pTable;
}

/*****************************************************************************/
void UnwindInfoTable::Register()
{
    _ASSERTE(s_pUnwindInfoTableLock->OwnedByCurrentThread());
    EX_TRY
    {
        hHandle = NULL;
        NTSTATUS ret = pRtlAddGrowableFunctionTable(&hHandle, pTable, cTableCurCount, cTableMaxCount, iRangeStart, iRangeEnd);    
        if (ret != STATUS_SUCCESS)  
        {
            _ASSERTE(!"Failed to publish UnwindInfo (ignorable)");
            hHandle = NULL;
            STRESS_LOG3(LF_JIT, LL_ERROR, "UnwindInfoTable::Register ERROR %x creating table [%p, %p]\n", ret, iRangeStart, iRangeEnd);
        }
        else  
        {
            STRESS_LOG3(LF_JIT, LL_INFO100, "UnwindInfoTable::Register Handle: %p [%p, %p]\n", hHandle, iRangeStart, iRangeEnd);
        }
    }
    EX_CATCH
    {
        hHandle = NULL;
        STRESS_LOG2(LF_JIT, LL_ERROR, "UnwindInfoTable::Register Exception while creating table [%p, %p]\n",
            iRangeStart, iRangeEnd);
        _ASSERTE(!"Failed to publish UnwindInfo (ignorable)");
    }
    EX_END_CATCH(SwallowAllExceptions)
}

/*****************************************************************************/
void UnwindInfoTable::UnRegister()
{
    PVOID handle = hHandle;
    hHandle = 0;
    if (handle != 0) 
    {
        STRESS_LOG3(LF_JIT, LL_INFO100, "UnwindInfoTable::UnRegister Handle: %p [%p, %p]\n", handle, iRangeStart, iRangeEnd);
        pRtlDeleteGrowableFunctionTable(handle);
    }
}

/*****************************************************************************/
// Add 'data' to the linked list whose head is pointed at by 'unwindInfoPtr'
//
/* static */ 
void UnwindInfoTable::AddToUnwindInfoTable(UnwindInfoTable** unwindInfoPtr, RUNTIME_FUNCTION* data,
                                          TADDR rangeStart, TADDR rangeEnd)    
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    _ASSERTE(data->BeginAddress <= RUNTIME_FUNCTION__EndAddress(data, rangeStart));
    _ASSERTE(RUNTIME_FUNCTION__EndAddress(data, rangeStart) <=  (rangeEnd-rangeStart));
    _ASSERTE(unwindInfoPtr != NULL);

    if (!s_publishingActive)
        return;

    CrstHolder ch(s_pUnwindInfoTableLock);

    UnwindInfoTable* unwindInfo = *unwindInfoPtr; 
    // was the original list null, If so lazy initialize. 
    if (unwindInfo == NULL) 
    {
        // We can choose the average method size estimate dynamically based on past experience
        // 128 is the estimated size of an average method, so we can accurately predict
        // how many RUNTIME_FUNCTION entries are in each chunk we allocate.

        ULONG size = (ULONG) ((rangeEnd - rangeStart) / 128) + 1;

        // To insure the test the growing logic in debug code make the size much smaller.  
        INDEBUG(size = size / 4 + 1);
        unwindInfo = (PTR_UnwindInfoTable)new UnwindInfoTable(rangeStart, rangeEnd, size);
        unwindInfo->Register();
        *unwindInfoPtr = unwindInfo;
    }
    _ASSERTE(unwindInfo != NULL);        // If new had failed, we would have thrown OOM
    _ASSERTE(unwindInfo->cTableCurCount <= unwindInfo->cTableMaxCount);
    _ASSERTE(unwindInfo->iRangeStart == rangeStart);
    _ASSERTE(unwindInfo->iRangeEnd == rangeEnd);

    // Means we had a failure publishing to the OS, in this case we give up
    if (unwindInfo->hHandle == NULL)
        return;

    // Check for the fast path: we are adding the the end of an UnwindInfoTable with space
    if (unwindInfo->cTableCurCount < unwindInfo->cTableMaxCount)
    {
        if (unwindInfo->cTableCurCount == 0 ||
            unwindInfo->pTable[unwindInfo->cTableCurCount-1].BeginAddress < data->BeginAddress)
        {
            // Yeah, we can simply add to the end of table and we are done!
            unwindInfo->pTable[unwindInfo->cTableCurCount] = *data;
            unwindInfo->cTableCurCount++;

            // Add to the function table
            pRtlGrowFunctionTable(unwindInfo->hHandle, unwindInfo->cTableCurCount);

            STRESS_LOG5(LF_JIT, LL_INFO1000, "AddToUnwindTable Handle: %p [%p, %p] ADDING 0x%xp TO END, now 0x%x entries\n",
                unwindInfo->hHandle, unwindInfo->iRangeStart, unwindInfo->iRangeEnd, 
                data->BeginAddress, unwindInfo->cTableCurCount);
            return;
        }
    }

    // OK we need to rellocate the table and reregister.  First figure out our 'desiredSpace'
    // We could imagine being much more efficient for 'bulk' updates, but we don't try
    // because we assume that this is rare and we want to keep the code simple

    int usedSpace = unwindInfo->cTableCurCount - unwindInfo->cDeletedEntries;
    int desiredSpace = usedSpace * 5 / 4 + 1;        // Increase by 20%
    // Be more aggresive if we used all of our space; 
    if (usedSpace == unwindInfo->cTableMaxCount)
        desiredSpace = usedSpace * 3 / 2 + 1;        // Increase by 50%

    STRESS_LOG7(LF_JIT, LL_INFO100, "AddToUnwindTable Handle: %p [%p, %p] SLOW Realloc Cnt 0x%x Max 0x%x NewMax 0x%x, Adding %x\n",
        unwindInfo->hHandle, unwindInfo->iRangeStart, unwindInfo->iRangeEnd, 
        unwindInfo->cTableCurCount, unwindInfo->cTableMaxCount, desiredSpace, data->BeginAddress);

    UnwindInfoTable* newTab = new UnwindInfoTable(unwindInfo->iRangeStart, unwindInfo->iRangeEnd, desiredSpace);

    // Copy in the entries, removing deleted entries and adding the new entry wherever it belongs
    int toIdx = 0;
    bool inserted = false;    // Have we inserted 'data' into the table
    for(ULONG fromIdx = 0; fromIdx < unwindInfo->cTableCurCount; fromIdx++)
    {
        if (!inserted && data->BeginAddress < unwindInfo->pTable[fromIdx].BeginAddress)
        {
            STRESS_LOG1(LF_JIT, LL_INFO100, "AddToUnwindTable Inserted at MID position 0x%x\n", toIdx);
            newTab->pTable[toIdx++] = *data;
            inserted = true;
        }
        if (unwindInfo->pTable[fromIdx].UnwindData != 0)	// A 'non-deleted' entry
            newTab->pTable[toIdx++] = unwindInfo->pTable[fromIdx];
    }
    if (!inserted) 
    {
        STRESS_LOG1(LF_JIT, LL_INFO100, "AddToUnwindTable Inserted at END position 0x%x\n", toIdx);
        newTab->pTable[toIdx++] = *data;
    }
    newTab->cTableCurCount = toIdx;
    STRESS_LOG2(LF_JIT, LL_INFO100, "AddToUnwindTable New size 0x%x max 0x%x\n",
        newTab->cTableCurCount, newTab->cTableMaxCount);
    _ASSERTE(newTab->cTableCurCount <= newTab->cTableMaxCount);

    // Unregister the old table
    *unwindInfoPtr = 0;
    unwindInfo->UnRegister();

    // Note that there is a short time when we are not publishing...

    // Register the new table
    newTab->Register();
    *unwindInfoPtr = newTab;
    
    delete unwindInfo;
}
        
/*****************************************************************************/
/* static */ void UnwindInfoTable::RemoveFromUnwindInfoTable(UnwindInfoTable** unwindInfoPtr, TADDR baseAddress, TADDR entryPoint)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;
    _ASSERTE(unwindInfoPtr != NULL);

    if (!s_publishingActive)
        return;
    CrstHolder ch(s_pUnwindInfoTableLock);

    UnwindInfoTable* unwindInfo = *unwindInfoPtr;
    if (unwindInfo != NULL)
    {
        DWORD relativeEntryPoint = (DWORD)(entryPoint - baseAddress);
        STRESS_LOG3(LF_JIT, LL_INFO100, "RemoveFromUnwindInfoTable Removing %p BaseAddress %p rel %x\n",
            entryPoint, baseAddress, relativeEntryPoint);
        for(ULONG i = 0; i < unwindInfo->cTableCurCount; i++)
        {
            if (unwindInfo->pTable[i].BeginAddress <= relativeEntryPoint && 
                relativeEntryPoint < RUNTIME_FUNCTION__EndAddress(&unwindInfo->pTable[i], unwindInfo->iRangeStart))
            {
                if (unwindInfo->pTable[i].UnwindData != 0)
                    unwindInfo->cDeletedEntries++;
                unwindInfo->pTable[i].UnwindData = 0;        // Mark the entry for deletion
                STRESS_LOG1(LF_JIT, LL_INFO100, "RemoveFromUnwindInfoTable Removed entry 0x%x\n", i);
                return;
            }
        }
    }
    STRESS_LOG2(LF_JIT, LL_WARNING, "RemoveFromUnwindInfoTable COULD NOT FIND %p BaseAddress %p\n",
        entryPoint, baseAddress);
}

/****************************************************************************/
// Publish the stack unwind data 'data' which is relative 'baseAddress' 
// to the operating system in a way ETW stack tracing can use.

/* static */ void UnwindInfoTable::PublishUnwindInfoForMethod(TADDR baseAddress, RUNTIME_FUNCTION* unwindInfo, int unwindInfoCount)
{
    STANDARD_VM_CONTRACT;
    if (!s_publishingActive)
        return;

    TADDR entry = baseAddress + unwindInfo->BeginAddress;
    RangeSection * pRS = ExecutionManager::FindCodeRange(entry, ExecutionManager::GetScanFlags());
    _ASSERTE(pRS != NULL);
    if (pRS != NULL)
    {
        for(int i = 0; i < unwindInfoCount; i++)
            AddToUnwindInfoTable(&pRS->pUnwindInfoTable, &unwindInfo[i], pRS->LowAddress, pRS->HighAddress);
    }
}
    
/*****************************************************************************/
/* static */ void UnwindInfoTable::UnpublishUnwindInfoForMethod(TADDR entryPoint)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;
    if (!s_publishingActive)
        return;

    RangeSection * pRS = ExecutionManager::FindCodeRange(entryPoint, ExecutionManager::GetScanFlags());
    _ASSERTE(pRS != NULL);
    if (pRS != NULL)
    {
        _ASSERTE(pRS->pjit->GetCodeType() == (miManaged | miIL));
        if (pRS->pjit->GetCodeType() == (miManaged | miIL))
        {
            // This cast is justified because only EEJitManager's have the code type above.
            EEJitManager* pJitMgr = (EEJitManager*)(pRS->pjit);
            CodeHeader * pHeader = pJitMgr->GetCodeHeaderFromStartAddress(entryPoint);
            for(ULONG i = 0; i < pHeader->GetNumberOfUnwindInfos(); i++)
                RemoveFromUnwindInfoTable(&pRS->pUnwindInfoTable, pRS->LowAddress, pRS->LowAddress + pHeader->GetUnwindInfo(i)->BeginAddress);
        }
    }
}

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
extern StubUnwindInfoHeapSegment *g_StubHeapSegments;
#endif // STUBLINKER_GENERATES_UNWIND_INFO

extern CrstStatic g_StubUnwindInfoHeapSegmentsCrst;
/*****************************************************************************/
// Publish all existing JIT compiled methods by iterating through the code heap
// Note that because we need to keep the entries in order we have to hold 
// s_pUnwindInfoTableLock so that all entries get inserted in the correct order.
// (we rely on heapIterator walking the methods in a heap section in order).

/* static */ void UnwindInfoTable::PublishUnwindInfoForExistingMethods() 
{
    STANDARD_VM_CONTRACT;
    {
        // CodeHeapIterator holds the m_CodeHeapCritSec, which insures code heaps don't get deallocated while being walked
        EEJitManager::CodeHeapIterator heapIterator(NULL, NULL);

        // Currently m_CodeHeapCritSec is given the CRST_UNSAFE_ANYMODE flag which allows it to be taken in a GC_NOTRIGGER
        // region but also disallows GC_TRIGGERS.  We need GC_TRIGGERS because we take annother lock.   Ideally we would
        // fix m_CodeHeapCritSec to not have the CRST_UNSAFE_ANYMODE flag, but I currently reached my threshold for fixing
        // contracts.
        CONTRACT_VIOLATION(GCViolation);

        while(heapIterator.Next())
        {
            MethodDesc *pMD = heapIterator.GetMethod();
            if(pMD)
            { 
                _ASSERTE(!pMD->IsZapped());

                PCODE methodEntry =(PCODE) heapIterator.GetMethodCode();
                RangeSection * pRS = ExecutionManager::FindCodeRange(methodEntry, ExecutionManager::GetScanFlags());
                _ASSERTE(pRS != NULL);
                _ASSERTE(pRS->pjit->GetCodeType() == (miManaged | miIL));
                if (pRS != NULL && pRS->pjit->GetCodeType() == (miManaged | miIL))
                {
                    // This cast is justified because only EEJitManager's have the code type above.
                    EEJitManager* pJitMgr = (EEJitManager*)(pRS->pjit);
                    CodeHeader * pHeader = pJitMgr->GetCodeHeaderFromStartAddress(methodEntry);
                    int unwindInfoCount = pHeader->GetNumberOfUnwindInfos();
                    for(int i = 0; i < unwindInfoCount; i++)
                        AddToUnwindInfoTable(&pRS->pUnwindInfoTable, pHeader->GetUnwindInfo(i), pRS->LowAddress, pRS->HighAddress);
                }
            }
        }
    }

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    // Enumerate all existing stubs
    CrstHolder crst(&g_StubUnwindInfoHeapSegmentsCrst);
    for (StubUnwindInfoHeapSegment* pStubHeapSegment = g_StubHeapSegments; pStubHeapSegment; pStubHeapSegment = pStubHeapSegment->pNext)
    {
        // The stubs are in reverse order, so we reverse them so they are in memory order
        CQuickArrayList<StubUnwindInfoHeader*> list;
        for (StubUnwindInfoHeader *pHeader = pStubHeapSegment->pUnwindHeaderList; pHeader; pHeader = pHeader->pNext)
            list.Push(pHeader);

        for(int i = (int) list.Size()-1; i >= 0; --i)
        {
            StubUnwindInfoHeader *pHeader = list[i];
            AddToUnwindInfoTable(&pStubHeapSegment->pUnwindInfoTable, &pHeader->FunctionEntry, 
                (TADDR) pStubHeapSegment->pbBaseAddress, (TADDR) pStubHeapSegment->pbBaseAddress + pStubHeapSegment->cbSegment);
        }
    }
#endif // STUBLINKER_GENERATES_UNWIND_INFO
}

/*****************************************************************************/
// turn on the publishing of unwind info.  Called when the ETW rundown provider
// is turned on. 

/* static */ void UnwindInfoTable::PublishUnwindInfo(bool publishExisting)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (s_publishingActive)
        return;

    // If we don't have the APIs we need, give up
    if (!InitUnwindFtns())
        return;

    EX_TRY
    {
        // Create the lock 
        Crst* newCrst = new Crst(CrstUnwindInfoTableLock);
        if (InterlockedCompareExchangeT(&s_pUnwindInfoTableLock, newCrst, NULL) == NULL)
        {
            s_publishingActive = true;
            if (publishExisting)
                PublishUnwindInfoForExistingMethods();
        }
        else 
            delete newCrst;    // we were in a race and failed, throw away the Crst we made.

    } EX_CATCH {
        STRESS_LOG1(LF_JIT, LL_ERROR, "Exception happened when doing unwind Info rundown. EIP of last AV = %p\n", g_LastAccessViolationEIP);
        _ASSERTE(!"Exception thrown while publishing 'catchup' ETW unwind information");
        s_publishingActive = false;     // Try to minimize damage.  
    } EX_END_CATCH(SwallowAllExceptions);
}

#endif // defined(_TARGET_AMD64_) && !defined(DACCESS_COMPILE)

/*-----------------------------------------------------------------------------
 This is a listing of which methods uses which synchronization mechanism
 in the EEJitManager.
//-----------------------------------------------------------------------------

Setters of EEJitManager::m_CodeHeapCritSec
-----------------------------------------------
allocCode
allocGCInfo
allocEHInfo
allocJumpStubBlock
ResolveEHClause
RemoveJitData
Unload
ReleaseReferenceToHeap
JitCodeToMethodInfo


Need EEJitManager::m_CodeHeapCritSec to be set
-----------------------------------------------
NewCodeHeap
allocCodeRaw
GetCodeHeapList
GetCodeHeap
RemoveCodeHeapFromDomainList
DeleteCodeHeap
AddRangeToJitHeapCache
DeleteJitHeapCache

*/


#if !defined(DACCESS_COMPILE)
EEJitManager::CodeHeapIterator::CodeHeapIterator(BaseDomain *pDomainFilter, LoaderAllocator *pLoaderAllocatorFilter)
    : m_lockHolder(&(ExecutionManager::GetEEJitManager()->m_CodeHeapCritSec)), m_Iterator(NULL, 0, NULL, 0)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pHeapList = NULL;
    m_pDomain = pDomainFilter;
    m_pLoaderAllocator = pLoaderAllocatorFilter;
    m_pHeapList = ExecutionManager::GetEEJitManager()->GetCodeHeapList();
    if(m_pHeapList)
        new (&m_Iterator) MethodSectionIterator((const void *)m_pHeapList->mapBase, (COUNT_T)m_pHeapList->maxCodeHeapSize, m_pHeapList->pHdrMap, (COUNT_T)HEAP2MAPSIZE(ROUND_UP_TO_PAGE(m_pHeapList->maxCodeHeapSize)));
};

EEJitManager::CodeHeapIterator::~CodeHeapIterator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
}

BOOL EEJitManager::CodeHeapIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(!m_pHeapList)
        return FALSE;

    while(1)
    {
        if(!m_Iterator.Next())
        {
            m_pHeapList = m_pHeapList->GetNext();
            if(!m_pHeapList)
                return FALSE;
            new (&m_Iterator) MethodSectionIterator((const void *)m_pHeapList->mapBase, (COUNT_T)m_pHeapList->maxCodeHeapSize, m_pHeapList->pHdrMap, (COUNT_T)HEAP2MAPSIZE(ROUND_UP_TO_PAGE(m_pHeapList->maxCodeHeapSize)));
        }
        else
        {
            BYTE * code = m_Iterator.GetMethodCode();
            CodeHeader * pHdr = (CodeHeader *)(code - sizeof(CodeHeader));
            m_pCurrent = !pHdr->IsStubCodeBlock() ? pHdr->GetMethodDesc() : NULL;
            if (m_pDomain && m_pCurrent)
            {
                BaseDomain *pCurrentBaseDomain = m_pCurrent->GetDomain();
                if(pCurrentBaseDomain != m_pDomain)
                    continue;
            }

            // LoaderAllocator filter
            if (m_pLoaderAllocator && m_pCurrent)
            {
                LoaderAllocator *pCurrentLoaderAllocator = m_pCurrent->GetLoaderAllocatorForCode();
                if(pCurrentLoaderAllocator != m_pLoaderAllocator)
                    continue;
            }

            return TRUE;
        }
    }
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// ReaderLockHolder::ReaderLockHolder takes the reader lock, checks for the writer lock 
// and either aborts if the writer lock is held, or yields until the writer lock is released,
// keeping the reader lock.  This is normally called in the constructor for the
// ReaderLockHolder.
// 
// The writer cannot be taken if there are any readers. The WriterLockHolder functions take the
// writer lock and check for any readers. If there are any, the WriterLockHolder functions
// release the writer and yield to wait for the readers to be done.

ExecutionManager::ReaderLockHolder::ReaderLockHolder(HostCallPreference hostCallPreference /*=AllowHostCalls*/)
{
    CONTRACTL {
        NOTHROW;
        if (hostCallPreference == AllowHostCalls) { HOST_CALLS; } else { HOST_NOCALLS; }
        GC_NOTRIGGER;
        SO_TOLERANT;
        CAN_TAKE_LOCK;
    } CONTRACTL_END;

    IncCantAllocCount();

    FastInterlockIncrement(&m_dwReaderCount);

    EE_LOCK_TAKEN(GetPtrForLockContract());

    if (VolatileLoad(&m_dwWriterLock) != 0)
    {
        if (hostCallPreference != AllowHostCalls)
        {
            // Rats, writer lock is held. Gotta bail. Since the reader count was already
            // incremented, we're technically still blocking writers at the moment. But
            // the holder who called us is about to call DecrementReader in its
            // destructor and unblock writers.
            return;
        }

        YIELD_WHILE ((VolatileLoad(&m_dwWriterLock) != 0));
    }
}

//---------------------------------------------------------------------------------------
//
// See code:ExecutionManager::ReaderLockHolder::ReaderLockHolder. This just decrements the reader count. 

ExecutionManager::ReaderLockHolder::~ReaderLockHolder()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    FastInterlockDecrement(&m_dwReaderCount);
    DecCantAllocCount();

    EE_LOCK_RELEASED(GetPtrForLockContract());
}

//---------------------------------------------------------------------------------------
//
// Returns whether the reader lock is acquired

BOOL ExecutionManager::ReaderLockHolder::Acquired()
{
    LIMITED_METHOD_CONTRACT;
    return VolatileLoad(&m_dwWriterLock) == 0;
}

ExecutionManager::WriterLockHolder::WriterLockHolder()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    } CONTRACTL_END;

    _ASSERTE(m_dwWriterLock == 0);

    // Signal to a debugger that this thread cannot stop now
    IncCantStopCount();

    IncCantAllocCount();

    DWORD dwSwitchCount = 0;
    while (TRUE)
    {
        // While this thread holds the writer lock, we must not try to suspend it
        // or allow a profiler to walk its stack
        Thread::IncForbidSuspendThread();

        FastInterlockIncrement(&m_dwWriterLock);
        if (m_dwReaderCount == 0)
            break;
        FastInterlockDecrement(&m_dwWriterLock);

        // Before we loop and retry, it's safe to suspend or hijack and inspect
        // this thread
        Thread::DecForbidSuspendThread();

        __SwitchToThread(0, ++dwSwitchCount);
    }
    EE_LOCK_TAKEN(GetPtrForLockContract());
}

ExecutionManager::WriterLockHolder::~WriterLockHolder()
{
    LIMITED_METHOD_CONTRACT;
    
    FastInterlockDecrement(&m_dwWriterLock);

    // Writer lock released, so it's safe again for this thread to be
    // suspended or have its stack walked by a profiler
    Thread::DecForbidSuspendThread();

    DecCantAllocCount();

    // Signal to a debugger that it's again safe to stop this thread
    DecCantStopCount();

    EE_LOCK_RELEASED(GetPtrForLockContract());
}

#else

// For DAC builds, we only care whether the writer lock is held.
// If it is, we will assume the locked data is in an inconsistent 
// state and throw. We never actually take the lock.
// Note: Throws
ExecutionManager::ReaderLockHolder::ReaderLockHolder(HostCallPreference hostCallPreference /*=AllowHostCalls*/)
{
    SUPPORTS_DAC;

    if (m_dwWriterLock != 0) 
    {
        ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED); 
    }
}

ExecutionManager::ReaderLockHolder::~ReaderLockHolder()
{
}

#endif // DACCESS_COMPILE

/*-----------------------------------------------------------------------------
 This is a listing of which methods uses which synchronization mechanism
 in the ExecutionManager
//-----------------------------------------------------------------------------

==============================================================================
ExecutionManger::ReaderLockHolder and ExecutionManger::WriterLockHolder
Protects the callers of ExecutionManager::GetRangeSection from heap deletions
while walking RangeSections.  You need to take a reader lock before reading the
values: m_CodeRangeList and hold it while walking the lists

Uses ReaderLockHolder (allows multiple reeaders with no writers)
-----------------------------------------
ExecutionManager::FindCodeRange
ExecutionManager::FindZapModule
ExecutionManager::EnumMemoryRegions

Uses WriterLockHolder (allows single writer and no readers)
-----------------------------------------
ExecutionManager::AddRangeHelper
ExecutionManager::DeleteRangeHelper

*/

//-----------------------------------------------------------------------------

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#define EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS
#endif

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS)
// The function fragments can be used in Hot/Cold splitting, expressing Large Functions or in 'ShrinkWrapping', which is 
// delaying saving and restoring some callee-saved registers later inside the body of the method.
// (It's assumed that JIT will not emit any ShrinkWrapping-style methods)
// For these cases multiple RUNTIME_FUNCTION entries (a.k.a function fragments) are used to define 
// all the regions of the function or funclet. And one of these function fragments cover the beginning of the function/funclet,
// including the prolog section and is referred as the 'Host Record'. 
// This function returns TRUE if the inspected RUNTIME_FUNCTION entry is NOT a host record

BOOL IsFunctionFragment(TADDR baseAddress, PTR_RUNTIME_FUNCTION pFunctionEntry)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE((pFunctionEntry->UnwindData & 3) == 0);   // The unwind data must be an RVA; we don't support packed unwind format
    DWORD unwindHeader = *(PTR_DWORD)(baseAddress + pFunctionEntry->UnwindData);
    _ASSERTE((0 == ((unwindHeader >> 18) & 3)) || !"unknown unwind data format, version != 0");
#if defined(_TARGET_ARM_)

    // On ARM, It's assumed that the prolog is always at the beginning of the function and cannot be split.
    // Given that, there are 4 possible ways to fragment a function:
    // 1. Prolog only: 
    // 2. Prolog and some epilogs: 
    // 3. Epilogs only: 
    // 4. No Prolog or epilog
    //
    // Function fragments describing 1 & 2 are host records, 3 & 4 are not.
    // for 3 & 4, the .xdata record's F bit is set to 1, marking clearly what is NOT a host record

    _ASSERTE((pFunctionEntry->BeginAddress & THUMB_CODE) == THUMB_CODE);   // Sanity check: it's a thumb address
    DWORD Fbit = (unwindHeader >> 22) & 0x1;    // F "fragment" bit
    return (Fbit == 1);
#elif defined(_TARGET_ARM64_)

    // ARM64 is a little bit more flexible, in the sense that it supports partial prologs. However only one of the 
    // prolog regions are allowed to alter SP and that's the Host Record. Partial prologs are used in ShrinkWrapping
    // scenarios which is not supported, hence we don't need to worry about them. discarding partial prologs
    // simplifies identifying a host record a lot.
    // 
    // 1. Prolog only: The host record. Epilog Count and E bit are all 0.
    // 2. Prolog and some epilogs: The host record with acompannying epilog-only records
    // 3. Epilogs only: First unwind code is Phantom prolog (Starting with an end_c, indicating an empty prolog)
    // 4. No prologs or epilogs: Epilog Count = 1 and Epilog Start Index points end_c. (as if it's case #2 with empty epilog codes) 
    //

    int EpilogCount = (int)(unwindHeader >> 22) & 0x1F;
    int CodeWords = unwindHeader >> 27;
    PTR_DWORD pUnwindCodes = (PTR_DWORD)(baseAddress + pFunctionEntry->UnwindData);
    if ((CodeWords == 0) && (EpilogCount == 0))
        pUnwindCodes++;
    BOOL Ebit = (unwindHeader >> 21) & 0x1;
    if (Ebit)
    {
        // EpilogCount is the index of the first unwind code that describes the one and only epilog
        // The unwind codes immediatelly follow the unwindHeader
        pUnwindCodes++;
    }
    else if (EpilogCount != 0)
    {
        // EpilogCount is the number of exception scopes defined right after the unwindHeader
        pUnwindCodes += EpilogCount+1;
    }
    else
    {
        return FALSE;
    }

    if ((*pUnwindCodes & 0xFF) == 0xE5) // Phantom prolog
        return TRUE;
        

#else
    PORTABILITY_ASSERT("IsFunctionFragnent - NYI on this platform");
#endif
    return FALSE;
}

#endif // EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS


#ifndef DACCESS_COMPILE

//**********************************************************************************
//  IJitManager
//**********************************************************************************
IJitManager::IJitManager()
{
    LIMITED_METHOD_CONTRACT;

    m_runtimeSupport   = ExecutionManager::GetDefaultCodeManager();
}

#endif // #ifndef DACCESS_COMPILE

// When we unload an appdomain, we need to make sure that any threads that are crawling through
// our heap or rangelist are out. For cooperative-mode threads, we know that they will have
// been stopped when we suspend the EE so they won't be touching an element that is about to be deleted.
// However for pre-emptive mode threads, they could be stalled right on top of the element we want
// to delete, so we need to apply the reader lock to them and wait for them to drain.
ExecutionManager::ScanFlag ExecutionManager::GetScanFlags()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    BEGIN_GETTHREAD_ALLOWED;

    Thread *pThread = GetThread();

    if (!pThread)
        return ScanNoReaderLock;

    // If this thread is hijacked by a profiler and crawling its own stack,
    // we do need to take the lock
    if (pThread->GetProfilerFilterContext() != NULL)
        return ScanReaderLock;
    
    if (pThread->PreemptiveGCDisabled() || (pThread == ThreadSuspend::GetSuspensionThread()))
        return ScanNoReaderLock;

    END_GETTHREAD_ALLOWED;

    return ScanReaderLock;
#else
    return ScanNoReaderLock;
#endif
}

#ifdef DACCESS_COMPILE

void IJitManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_VTHIS();
    if (m_runtimeSupport.IsValid())
    {
        m_runtimeSupport->EnumMemoryRegions(flags);
    }
}

#endif // #ifdef DACCESS_COMPILE

#if defined(WIN64EXCEPTIONS)

PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ SIZE_T * pSize)
{
    LIMITED_METHOD_CONTRACT;

#if defined(_TARGET_AMD64_)
    PTR_UNWIND_INFO pUnwindInfo(dac_cast<PTR_UNWIND_INFO>(moduleBase + RUNTIME_FUNCTION__GetUnwindInfoAddress(pRuntimeFunction)));

    *pSize = ALIGN_UP(offsetof(UNWIND_INFO, UnwindCode) +
        sizeof(UNWIND_CODE) * pUnwindInfo->CountOfUnwindCodes +
        sizeof(ULONG) /* personality routine is always present */,
            sizeof(DWORD));

    return pUnwindInfo;

#elif defined(_TARGET_ARM_)

    // if this function uses packed unwind data then at least one of the two least significant bits
    // will be non-zero.  if this is the case then there will be no xdata record to enumerate.
    _ASSERTE((pRuntimeFunction->UnwindData & 0x3) == 0);

    // compute the size of the unwind info
    PTR_TADDR xdata = dac_cast<PTR_TADDR>(pRuntimeFunction->UnwindData + moduleBase);

    ULONG epilogScopes = 0;
    ULONG unwindWords = 0;
    ULONG size = 0;

    if ((xdata[0] >> 23) != 0)
    {
        size = 4;
        epilogScopes = (xdata[0] >> 23) & 0x1f;
        unwindWords = (xdata[0] >> 28) & 0x0f;
    }
    else
    {
        size = 8;
        epilogScopes = xdata[1] & 0xffff;
        unwindWords = (xdata[1] >> 16) & 0xff;
    }

    if (!(xdata[0] & (1 << 21)))
        size += 4 * epilogScopes;

    size += 4 * unwindWords;

    _ASSERTE(xdata[0] & (1 << 20)); // personality routine should be always present
    size += 4;

    *pSize = size;
    return xdata;
#else
    PORTABILITY_ASSERT("GetUnwindDataBlob");
    return NULL;
#endif
}

// GetFuncletStartAddress returns the starting address of the function or funclet indicated by the EECodeInfo address.
TADDR IJitManager::GetFuncletStartAddress(EECodeInfo * pCodeInfo)
{
    PTR_RUNTIME_FUNCTION pFunctionEntry = pCodeInfo->GetFunctionEntry();

#ifdef _TARGET_AMD64_
    _ASSERTE((pFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0);
#endif

    TADDR baseAddress = pCodeInfo->GetModuleBase();
    TADDR funcletStartAddress = baseAddress + RUNTIME_FUNCTION__BeginAddress(pFunctionEntry);

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS)
    // Is the RUNTIME_FUNCTION a fragment? If so, we need to walk backwards until we find the first
    // non-fragment RUNTIME_FUNCTION, and use that one. This happens when we have very large functions
    // and multiple RUNTIME_FUNCTION entries per function or funclet. However, all but the first will
    // have the "F" bit set in the unwind data, indicating a fragment (with phantom prolog unwind codes).

    for (;;)
    {
        if (!IsFunctionFragment(baseAddress, pFunctionEntry))
        {
            // This is not a fragment; we're done
            break;
        }

        // We found a fragment. Walk backwards in the RUNTIME_FUNCTION array until we find a non-fragment.
        // We're guaranteed to find one, because we require that a fragment live in a function or funclet
        // that has a prolog, which will have non-fragment .xdata.
        --pFunctionEntry;

        funcletStartAddress = baseAddress + RUNTIME_FUNCTION__BeginAddress(pFunctionEntry);
    }
#endif // EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS

    return funcletStartAddress;
}

BOOL IJitManager::IsFunclet(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TADDR funcletStartAddress = GetFuncletStartAddress(pCodeInfo);
    TADDR methodStartAddress = pCodeInfo->GetStartAddress();

    return (funcletStartAddress != methodStartAddress);
}

BOOL IJitManager::IsFilterFunclet(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pCodeInfo->IsFunclet())
        return FALSE;

    TADDR funcletStartAddress = GetFuncletStartAddress(pCodeInfo);

    // This assumes no hot/cold splitting for funclets

    _ASSERTE(FitsInU4(pCodeInfo->GetCodeAddress() - funcletStartAddress));
    DWORD relOffsetWithinFunclet = static_cast<DWORD>(pCodeInfo->GetCodeAddress() - funcletStartAddress);

    _ASSERTE(pCodeInfo->GetRelOffset() >= relOffsetWithinFunclet);
    DWORD funcletStartOffset = pCodeInfo->GetRelOffset() - relOffsetWithinFunclet;

    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = InitializeEHEnumeration(pCodeInfo->GetMethodToken(), &pEnumState);
    _ASSERTE(EHCount > 0);

    EE_ILEXCEPTION_CLAUSE EHClause;
    for (ULONG i = 0; i < EHCount; i++)
    {
         GetNextEHClause(&pEnumState, &EHClause);

        // Duplicate clauses are always listed at the end, so when we hit a duplicate clause,
        // we have already visited all of the normal clauses.
        if (IsDuplicateClause(&EHClause))
        {
            break;
        }

        if (IsFilterHandler(&EHClause))
        {
            if (EHClause.FilterOffset == funcletStartOffset)
            {
                return true;
            }
        }
    }

    return false;
}

#else // WIN64EXCEPTIONS

FORCEINLINE PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ SIZE_T * pSize)
{
    *pSize = 0;
    return dac_cast<PTR_VOID>(pRuntimeFunction->UnwindData + moduleBase);
}

#endif // WIN64EXCEPTIONS


#ifndef CROSSGEN_COMPILE

#ifndef DACCESS_COMPILE

//**********************************************************************************
//  EEJitManager
//**********************************************************************************

EEJitManager::EEJitManager()
    :
    // CRST_DEBUGGER_THREAD - We take this lock on debugger thread during EnC add method, among other things
    // CRST_TAKEN_DURING_SHUTDOWN - We take this lock during shutdown if ETW is on (to do rundown)
    m_CodeHeapCritSec( CrstSingleUseLock,
                        CrstFlags(CRST_UNSAFE_ANYMODE|CRST_DEBUGGER_THREAD|CRST_TAKEN_DURING_SHUTDOWN)),
    m_EHClauseCritSec( CrstSingleUseLock )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_pCodeHeap = NULL;
    m_jit = NULL;
    m_JITCompiler      = NULL;
#ifdef _TARGET_AMD64_
    m_JITCompilerOther = NULL;
#endif
#ifdef ALLOW_SXS_JIT
    m_alternateJit     = NULL;
    m_AltJITCompiler   = NULL;
    m_AltJITRequired   = false;
#endif

    m_dwCPUCompileFlags = 0;

    m_cleanupList = NULL;
}

#if defined(_TARGET_AMD64_)
extern "C" DWORD __stdcall getcpuid(DWORD arg, unsigned char result[16]);
#endif // defined(_TARGET_AMD64_)

void EEJitManager::SetCpuInfo()
{
    LIMITED_METHOD_CONTRACT;

    //
    // NOTE: This function needs to be kept in sync with Zapper::CompileAssembly()
    //

    DWORD dwCPUCompileFlags = 0;

#if defined(_TARGET_X86_)
    // NOTE: if you're adding any flags here, you probably should also be doing it
    // for ngen (zapper.cpp)
    CORINFO_CPU cpuInfo;
    GetSpecificCpuInfo(&cpuInfo);

    switch (CPU_X86_FAMILY(cpuInfo.dwCPUType))
    {
    case CPU_X86_PENTIUM_4:
        dwCPUCompileFlags |= CORJIT_FLG_TARGET_P4;
        break;
    default:
        break;
    }

    if (CPU_X86_USE_CMOV(cpuInfo.dwFeatures))
    {
        dwCPUCompileFlags |= CORJIT_FLG_USE_CMOV |
                             CORJIT_FLG_USE_FCOMI;
    }

    if (CPU_X86_USE_SSE2(cpuInfo.dwFeatures))
    {
        dwCPUCompileFlags |= CORJIT_FLG_USE_SSE2;
    }
#elif defined(_TARGET_AMD64_)
    unsigned char buffer[16];
    DWORD maxCpuId = getcpuid(0, buffer);
    if (maxCpuId >= 0)
    {
        // getcpuid executes cpuid with eax set to its first argument, and ecx cleared.
        // It returns the resulting eax in buffer[0-3], ebx in buffer[4-7], ecx in buffer[8-11],
        // and edx in buffer[12-15].
        // We will set the following flags:
        // CORJIT_FLG_USE_SSE3_4 if the following feature bits are set (input EAX of 1)
        //    SSE3 - ECX bit 0     (buffer[8]  & 0x01)
        //    SSSE3 - ECX bit 9    (buffer[9]  & 0x02)
        //    SSE4.1 - ECX bit 19  (buffer[10] & 0x08)
        //    SSE4.2 - ECX bit 20  (buffer[10] & 0x10)
        // CORJIT_FLG_USE_AVX if the following feature bit is set (input EAX of 1):
        //    AVX - ECX bit 28     (buffer[11] & 0x10)
        // CORJIT_FLG_USE_AVX2 if the following feature bit is set (input EAX of 0x07 and input ECX of 0):
        //    AVX2 - EBX bit 5     (buffer[4]  & 0x20)
        // CORJIT_FLG_USE_AVX_512 is not currently set, but defined so that it can be used in future without
        // synchronously updating VM and JIT.
        (void) getcpuid(1, buffer);
        // If SSE2 is not enabled, there is no point in checking the rest.
        // SSE2 is bit 26 of EDX   (buffer[15] & 0x04)
        // TODO: Determine whether we should break out the various SSE options further.
        if ((buffer[15] & 0x04) != 0)               // SSE2
        {
            if (((buffer[8]  & 0x01) != 0) &&       // SSE3
                ((buffer[9]  & 0x02) != 0) &&       // SSSE3
                ((buffer[10] & 0x08) != 0) &&       // SSE4.1
                ((buffer[10] & 0x10) != 0))         // SSE4.2
            {
                dwCPUCompileFlags |= CORJIT_FLG_USE_SSE3_4;
            }
            if ((buffer[11] & 0x10) != 0)
            {
                dwCPUCompileFlags |= CORJIT_FLG_USE_AVX;
                if (maxCpuId >= 0x07)
                {
                    (void) getcpuid(0x07, buffer);
                    if ((buffer[4]  & 0x20) != 0)
                    {
                        dwCPUCompileFlags |= CORJIT_FLG_USE_AVX2;
                    }
                }
            }
            static ConfigDWORD fFeatureSIMD;
            if (fFeatureSIMD.val(CLRConfig::EXTERNAL_FeatureSIMD) != 0)
            {
                dwCPUCompileFlags |= CORJIT_FLG_FEATURE_SIMD;
            }
        }
    }
#endif // defined(_TARGET_AMD64_)

    m_dwCPUCompileFlags = dwCPUCompileFlags;
}

// Define some data that we can use to get a better idea of what happened when we get a Watson dump that indicates the JIT failed to load.
// This will be used and updated by the JIT loading and initialization functions, and the data written will get written into a Watson dump.

enum JIT_LOAD_JIT_ID
{
    JIT_LOAD_MAIN = 500,    // The "main" JIT. Normally, this is named "clrjit.dll". Start at a number that is somewhat uncommon (i.e., not zero or 1) to help distinguish from garbage, in process dumps.
    JIT_LOAD_LEGACY,        // The "legacy" JIT. Normally, this is named "compatjit.dll" (aka, JIT64). This only applies to AMD64.
    JIT_LOAD_ALTJIT         // An "altjit". By default, named "protojit.dll". Used both internally, as well as externally for JIT CTP builds.
};

enum JIT_LOAD_STATUS
{
    JIT_LOAD_STATUS_STARTING = 1001,                   // The JIT load process is starting. Start at a number that is somewhat uncommon (i.e., not zero or 1) to help distinguish from garbage, in process dumps.
    JIT_LOAD_STATUS_DONE_LOAD,                         // LoadLibrary of the JIT dll succeeded.
    JIT_LOAD_STATUS_DONE_GET_SXSJITSTARTUP,            // GetProcAddress for "sxsJitStartup" succeeded.
    JIT_LOAD_STATUS_DONE_CALL_SXSJITSTARTUP,           // Calling sxsJitStartup() succeeded.
    JIT_LOAD_STATUS_DONE_GET_GETJIT,                   // GetProcAddress for "getJit" succeeded.
    JIT_LOAD_STATUS_DONE_CALL_GETJIT,                  // Calling getJit() succeeded.
    JIT_LOAD_STATUS_DONE_CALL_GETVERSIONIDENTIFIER,    // Calling ICorJitCompiler::getVersionIdentifier() succeeded.
    JIT_LOAD_STATUS_DONE_VERSION_CHECK,                // The JIT-EE version identifier check succeeded.
    JIT_LOAD_STATUS_DONE,                              // The JIT load is complete, and successful.
};

struct JIT_LOAD_DATA
{
    JIT_LOAD_JIT_ID     jld_id;         // Which JIT are we currently loading?
    JIT_LOAD_STATUS     jld_status;     // The current load status of a JIT load attempt.
    HRESULT             jld_hr;         // If the JIT load fails, the last jld_status will be JIT_LOAD_STATUS_STARTING.
                                        //   In that case, this will contain the HRESULT returned by LoadLibrary.
                                        //   Otherwise, this will be S_OK (which is zero).
};

// Here's the global data for JIT load and initialization state.
JIT_LOAD_DATA g_JitLoadData;

// LoadAndInitializeJIT: load the JIT dll into the process, and initialize it (call the UtilCode initialization function,
// check the JIT-EE interface GUID, etc.)
//
// Parameters:
//
// pwzJitName        - The filename of the JIT .dll file to load. E.g., "altjit.dll".
// phJit             - On return, *phJit is the Windows module handle of the loaded JIT dll. It will be NULL if the load failed.
// ppICorJitCompiler - On return, *ppICorJitCompiler is the ICorJitCompiler* returned by the JIT's getJit() entrypoint.
//                     It is NULL if the JIT returns a NULL interface pointer, or if the JIT-EE interface GUID is mismatched.
//                     Note that if the given JIT is loaded, but the interface is mismatched, then *phJit will be legal and non-NULL
//                     even though *ppICorJitCompiler is NULL. This allows the caller to unload the JIT dll, if necessary
//                     (nobody does this today).
// pJitLoadData      - Pointer to a structure that we update as we load and initialize the JIT to indicate how far we've gotten. This
//                     is used to help understand problems we see with JIT loading that come in via Watson dumps. Since we don't throw
//                     an exception immediately upon failure, we can lose information about what the failure was if we don't store this
//                     information in a way that persists into a process dump.
// 

static void LoadAndInitializeJIT(LPCWSTR pwzJitName, OUT HINSTANCE* phJit, OUT ICorJitCompiler** ppICorJitCompiler, IN OUT JIT_LOAD_DATA* pJitLoadData)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(phJit != NULL);
    _ASSERTE(ppICorJitCompiler != NULL);
    _ASSERTE(pJitLoadData != NULL);

    pJitLoadData->jld_status = JIT_LOAD_STATUS_STARTING;
    pJitLoadData->jld_hr     = S_OK;

    *phJit = NULL;
    *ppICorJitCompiler = NULL;

    HRESULT hr = E_FAIL;

#ifdef FEATURE_MERGE_JIT_AND_ENGINE
    WCHAR CoreClrFolder[MAX_PATH + 1];
    extern HINSTANCE g_hThisInst;
    if (WszGetModuleFileName(g_hThisInst, CoreClrFolder, MAX_PATH))
    {
        WCHAR *filePtr = wcsrchr(CoreClrFolder, DIRECTORY_SEPARATOR_CHAR_W);
        if (filePtr)
        {
            filePtr[1] = W('\0');
            wcscat_s(CoreClrFolder, MAX_PATH, pwzJitName);
            *phJit = CLRLoadLibrary(CoreClrFolder);
            if (*phJit != NULL)
            {
                hr = S_OK;
            }
        }
    }
#else
    hr = g_pCLRRuntime->LoadLibrary(pwzJitName, phJit);
#endif

    if (SUCCEEDED(hr))
    {
        pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_LOAD;

        EX_TRY
        {
            typedef void (__stdcall* psxsJitStartup) (CoreClrCallbacks const &);
            psxsJitStartup sxsJitStartupFn = (psxsJitStartup) GetProcAddress(*phJit, "sxsJitStartup");

            if (sxsJitStartupFn)
            {
                pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_GET_SXSJITSTARTUP;

                CoreClrCallbacks cccallbacks = GetClrCallbacks();
                (*sxsJitStartupFn) (cccallbacks);

                pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_CALL_SXSJITSTARTUP;

                typedef ICorJitCompiler* (__stdcall* pGetJitFn)();
                pGetJitFn getJitFn = (pGetJitFn) GetProcAddress(*phJit, "getJit");

                if (getJitFn)
                {
                    pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_GET_GETJIT;

                    ICorJitCompiler* pICorJitCompiler = (*getJitFn)();
                    if (pICorJitCompiler != NULL)
                    {
                        pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_CALL_GETJIT;

                        GUID versionId;
                        memset(&versionId, 0, sizeof(GUID));
                        pICorJitCompiler->getVersionIdentifier(&versionId);

                        pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_CALL_GETVERSIONIDENTIFIER;

                        if (memcmp(&versionId, &JITEEVersionIdentifier, sizeof(GUID)) == 0)
                        {
                            pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_VERSION_CHECK;

                            // The JIT has loaded and passed the version identifier test, so publish the JIT interface to the caller.
                            *ppICorJitCompiler = pICorJitCompiler;

                            // The JIT is completely loaded and initialized now.
                            pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE;
                        }
                        else
                        {
                            // Mismatched version ID. Fail the load.
                            LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: mismatched JIT version identifier in %S\n", pwzJitName));
                        }
                    }
                    else
                    {
                        LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: failed to get ICorJitCompiler in %S\n", pwzJitName));
                    }
                }
                else
                {
                    LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: failed to find 'getJit' entrypoint in %S\n", pwzJitName));
                }
            }
            else
            {
                LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: failed to find 'sxsJitStartup' entrypoint in %S\n", pwzJitName));
            }
        }
        EX_CATCH
        {
            LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: caught an exception trying to initialize %S\n", pwzJitName));
        }
        EX_END_CATCH(SwallowAllExceptions)
    }
    else
    {
        pJitLoadData->jld_hr = hr;
        LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: failed to load %S, hr=0x%08x\n", pwzJitName, hr));
    }
}

#ifdef FEATURE_MERGE_JIT_AND_ENGINE
EXTERN_C ICorJitCompiler* __stdcall getJit();
#endif // FEATURE_MERGE_JIT_AND_ENGINE

// Set this to the result of LoadJIT as a courtesy to code:CorCompileGetRuntimeDll
extern HMODULE s_ngenCompilerDll;

BOOL EEJitManager::LoadJIT()
{
    STANDARD_VM_CONTRACT;

    // If the JIT is already loaded, don't take the lock.
    if (IsJitLoaded())
        return TRUE;

    // Abuse m_EHClauseCritSec to ensure that the JIT is loaded on one thread only 
    CrstHolder chRead(&m_EHClauseCritSec);

    // Did someone load the JIT before we got the lock?
    if (IsJitLoaded())
        return TRUE;

    SetCpuInfo();

    ICorJitCompiler* newJitCompiler = NULL;

#ifdef FEATURE_MERGE_JIT_AND_ENGINE

    typedef ICorJitCompiler* (__stdcall* pGetJitFn)();
    pGetJitFn getJitFn = (pGetJitFn) getJit;
    EX_TRY
    {
        newJitCompiler = (*getJitFn)();

        // We don't need to call getVersionIdentifier(), since the JIT is linked together with the VM.
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

#else // !FEATURE_MERGE_JIT_AND_ENGINE

    m_JITCompiler = NULL;
#ifdef _TARGET_AMD64_
    m_JITCompilerOther = NULL;
#endif

    g_JitLoadData.jld_id = JIT_LOAD_MAIN;
    LoadAndInitializeJIT(ExecutionManager::GetJitName(), &m_JITCompiler, &newJitCompiler, &g_JitLoadData);

    // Set as a courtesy to code:CorCompileGetRuntimeDll
    s_ngenCompilerDll = m_JITCompiler;
    
#if defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE)
    // If COMPLUS_UseLegacyJit=1, then we fall back to compatjit.dll.
    //
    // This fallback mechanism was introduced for Visual Studio "14" Preview, when JIT64 (the legacy JIT) was replaced with
    // RyuJIT. It was desired to provide a fallback mechanism in case comptibility problems (or other bugs)
    // were discovered by customers. Setting this COMPLUS variable to 1 does not affect NGEN: existing NGEN images continue
    // to be used, and all subsequent NGEN compilations continue to use the new JIT.
    //
    // If this is a compilation process, then we don't allow specifying a fallback JIT. This is a case where, when NGEN'ing,
    // we sometimes need to JIT some things (such as when we are NGEN'ing mscorlib). In that case, we want to use exactly
    // the same JIT as NGEN uses. And NGEN doesn't follow the COMPLUS_UseLegacyJit=1 switch -- it always uses clrjit.dll.
    //
    // Note that we always load and initialize the default JIT. This is to handle cases where obfuscators rely on
    // LoadLibrary("clrjit.dll") returning the module handle of the JIT, and then they call GetProcAddress("getJit") to get
    // the EE-JIT interface. They also do this without also calling sxsJitStartup()!
    //
    // In addition, for reasons related to servicing, we only use RyuJIT when the registry value UseRyuJIT (type DWORD), under
    // key HKLM\SOFTWARE\Microsoft\.NETFramework, is set to 1. Otherwise, we fall back to JIT64. Note that if this value
    // is set, we also must use JIT64 for all NGEN compilations as well.
    //
    // See the document "RyuJIT Compatibility Fallback Specification.docx" for details.

    bool fUseRyuJit = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_UseRyuJit) == 1); // uncached access, since this code is run no more than one time

    if ((!IsCompilationProcess() || !fUseRyuJit) &&     // Use RyuJIT for all NGEN, unless we're falling back to JIT64 for everything.
        (newJitCompiler != nullptr))    // the main JIT must successfully load before we try loading the fallback JIT
    {
        BOOL fUsingCompatJit = FALSE;

        if (!fUseRyuJit)
        {
            fUsingCompatJit = TRUE;
        }

        if (!fUsingCompatJit)
        {
            DWORD useLegacyJit = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_UseLegacyJit); // uncached access, since this code is run no more than one time
            if (useLegacyJit == 1)
            {
                fUsingCompatJit = TRUE;
            }
        }

#if defined(FEATURE_APPX_BINDER)
        if (!fUsingCompatJit)
        {
            // AppX applications don't have a .config file for per-app configuration. So, we allow the placement of a single
            // distinguished file, "UseLegacyJit.txt" in the root of the app's package to indicate that the app should fall
            // back to JIT64. This same file is also used to prevent this app from participating in AutoNgen.
            if (AppX::IsAppXProcess())
            {
                WCHAR szPathName[MAX_PATH];
                UINT32 cchPathName = MAX_PATH;
                if (AppX::FindFileInCurrentPackage(L"UseLegacyJit.txt", &cchPathName, szPathName, PACKAGE_FILTER_HEAD) == S_OK)
                {
                    fUsingCompatJit = TRUE;
                }
            }
        }
#endif // FEATURE_APPX_BINDER

        if (fUsingCompatJit)
        {
            // Now, load the compat jit and initialize it.

            LPWSTR pwzJitName = MAKEDLLNAME_W(L"compatjit");

            // Note: if the compatjit fails to load, we ignore it, and continue to use the main JIT for
            // everything. You can imagine a policy where if the user requests the compatjit, and we fail
            // to load it, that we fail noisily. We don't do that currently.
            ICorJitCompiler* fallbackICorJitCompiler;
            g_JitLoadData.jld_id = JIT_LOAD_LEGACY;
            LoadAndInitializeJIT(pwzJitName, &m_JITCompilerOther, &fallbackICorJitCompiler, &g_JitLoadData);
            if (fallbackICorJitCompiler != nullptr)
            {
                // Tell the main JIT to fall back to the "fallback" JIT compiler, in case some
                // obfuscator tries to directly call the main JIT's getJit() function.
                newJitCompiler->setRealJit(fallbackICorJitCompiler);
            }
        }
    }
#endif // defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE)

#endif // !FEATURE_MERGE_JIT_AND_ENGINE

#ifdef ALLOW_SXS_JIT

    // Do not load altjit.dll unless COMPLUS_AltJit is set.
    // Even if the main JIT fails to load, if the user asks for an altjit we try to load it.
    // This allows us to display load error messages for loading altjit.

    ICorJitCompiler* newAltJitCompiler = NULL;

    LPWSTR altJitConfig;
    IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AltJit, &altJitConfig));

    m_AltJITCompiler = NULL;

    if (altJitConfig != NULL)
    {
        // Load the altjit into the system.
        // Note: altJitName must be declared as a const otherwise assigning the string
        // constructed by MAKEDLLNAME_W() to altJitName will cause a build break on Unix.
        LPCWSTR altJitName;
        IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AltJitName, (LPWSTR*)&altJitName));

        if (altJitName == NULL)
        {
            altJitName = MAKEDLLNAME_W(W("protojit"));
        }

        g_JitLoadData.jld_id = JIT_LOAD_ALTJIT;
        LoadAndInitializeJIT(altJitName, &m_AltJITCompiler, &newAltJitCompiler, &g_JitLoadData);
    }

#endif // ALLOW_SXS_JIT 

    // Publish the compilers.

#ifdef ALLOW_SXS_JIT
    m_AltJITRequired = (altJitConfig != NULL);
    m_alternateJit = newAltJitCompiler;
#endif // ALLOW_SXS_JIT

    m_jit = newJitCompiler;

    // Failing to load the main JIT is a failure.
    // If the user requested an altjit and we failed to load an altjit, that is also a failure.
    // In either failure case, we'll rip down the VM (so no need to clean up (unload) either JIT that did load successfully.
    return IsJitLoaded();
}

#ifndef CROSSGEN_COMPILE
//**************************************************************************

CodeFragmentHeap::CodeFragmentHeap(LoaderAllocator * pAllocator, StubCodeBlockKind kind)
    : m_pAllocator(pAllocator), m_pFreeBlocks(NULL), m_kind(kind),
    // CRST_DEBUGGER_THREAD - We take this lock on debugger thread during EnC add meth
    m_CritSec(CrstCodeFragmentHeap, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD))
{
    WRAPPER_NO_CONTRACT;
}

void CodeFragmentHeap::AddBlock(VOID * pMem, size_t dwSize)
{
    LIMITED_METHOD_CONTRACT;
    FreeBlock * pBlock = (FreeBlock *)pMem;
    pBlock->m_pNext = m_pFreeBlocks;
    pBlock->m_dwSize = dwSize;
    m_pFreeBlocks = pBlock;
}

void CodeFragmentHeap::RemoveBlock(FreeBlock ** ppBlock)
{
    LIMITED_METHOD_CONTRACT;
    FreeBlock * pBlock = *ppBlock;
    *ppBlock = pBlock->m_pNext;
    ZeroMemory(pBlock, sizeof(FreeBlock));
}

TaggedMemAllocPtr CodeFragmentHeap::RealAllocAlignedMem(size_t  dwRequestedSize
                    ,unsigned  dwAlignment
#ifdef _DEBUG
                    ,__in __in_z const char *szFile
                    ,int  lineNum
#endif
                    )
{
    CrstHolder ch(&m_CritSec);

    dwRequestedSize = ALIGN_UP(dwRequestedSize, sizeof(TADDR));

    if (dwRequestedSize < sizeof(FreeBlock))
        dwRequestedSize = sizeof(FreeBlock);

    // We will try to batch up allocation of small blocks into one large allocation
#define SMALL_BLOCK_THRESHOLD 0x100
    SIZE_T nFreeSmallBlocks = 0;

    FreeBlock ** ppBestFit = NULL;
    FreeBlock ** ppFreeBlock = &m_pFreeBlocks;
    while (*ppFreeBlock != NULL)
    {
        FreeBlock * pFreeBlock = *ppFreeBlock;
        if (((BYTE *)pFreeBlock + pFreeBlock->m_dwSize) - (BYTE *)ALIGN_UP(pFreeBlock, dwAlignment) >= (SSIZE_T)dwRequestedSize)
        {
            if (ppBestFit == NULL || pFreeBlock->m_dwSize < (*ppBestFit)->m_dwSize)
                ppBestFit = ppFreeBlock;
        }
        else
        {
            if (pFreeBlock->m_dwSize < SMALL_BLOCK_THRESHOLD)
                nFreeSmallBlocks++;
        }
        ppFreeBlock = &(*ppFreeBlock)->m_pNext;
    }

    VOID * pMem;
    SIZE_T dwSize;
    if (ppBestFit != NULL)
    {
        pMem = *ppBestFit;
        dwSize = (*ppBestFit)->m_dwSize;

        RemoveBlock(ppBestFit);
    }
    else
    {
        dwSize = dwRequestedSize;
        if (dwSize < SMALL_BLOCK_THRESHOLD)
            dwSize = 4 * SMALL_BLOCK_THRESHOLD;
        pMem = ExecutionManager::GetEEJitManager()->allocCodeFragmentBlock(dwSize, dwAlignment, m_pAllocator, m_kind);
    }

    SIZE_T dwExtra = (BYTE *)ALIGN_UP(pMem, dwAlignment) - (BYTE *)pMem;
    _ASSERTE(dwSize >= dwExtra + dwRequestedSize);
    SIZE_T dwRemaining = dwSize - (dwExtra + dwRequestedSize);

    // Avoid accumulation of too many small blocks. The more small free blocks we have, the more picky we are going to be about adding new ones.
    if ((dwRemaining >= max(sizeof(FreeBlock), sizeof(StubPrecode)) + (SMALL_BLOCK_THRESHOLD / 0x10) * nFreeSmallBlocks) || (dwRemaining >= SMALL_BLOCK_THRESHOLD))
    {
        AddBlock((BYTE *)pMem + dwExtra + dwRequestedSize, dwRemaining);
        dwSize -= dwRemaining;
    }

    TaggedMemAllocPtr tmap;
    tmap.m_pMem             = pMem;
    tmap.m_dwRequestedSize  = dwSize;
    tmap.m_pHeap            = this;
    tmap.m_dwExtra          = dwExtra;
#ifdef _DEBUG
    tmap.m_szFile           = szFile;
    tmap.m_lineNum          = lineNum;
#endif
    return tmap;
}

void CodeFragmentHeap::RealBackoutMem(void *pMem
                    , size_t dwSize
#ifdef _DEBUG
                    , __in __in_z const char *szFile
                    , int lineNum
                    , __in __in_z const char *szAllocFile
                    , int allocLineNum
#endif
                    )
{
    CrstHolder ch(&m_CritSec);

    _ASSERTE(dwSize >= sizeof(FreeBlock));

    ZeroMemory((BYTE *)pMem, dwSize);

    //
    // Try to coalesce blocks if possible
    //
    FreeBlock ** ppFreeBlock = &m_pFreeBlocks;
    while (*ppFreeBlock != NULL)
    {
        FreeBlock * pFreeBlock = *ppFreeBlock;

        if ((BYTE *)pFreeBlock == (BYTE *)pMem + dwSize)
        {
            // pMem = pMem;
            dwSize += pFreeBlock->m_dwSize;
            RemoveBlock(ppFreeBlock);
            continue;
        }
        else
        if ((BYTE *)pFreeBlock + pFreeBlock->m_dwSize == (BYTE *)pMem)
        {
            pMem = pFreeBlock;
            dwSize += pFreeBlock->m_dwSize;
            RemoveBlock(ppFreeBlock);
            continue;
        }

        ppFreeBlock = &(*ppFreeBlock)->m_pNext;
    }

    AddBlock(pMem, dwSize);
}
#endif // CROSSGEN_COMPILE

//**************************************************************************

LoaderCodeHeap::LoaderCodeHeap(size_t * pPrivatePCLBytes)
    : m_LoaderHeap(pPrivatePCLBytes,
                   0,                       // RangeList *pRangeList
                   TRUE),                   // BOOL fMakeExecutable
    m_cbMinNextPad(0)
{
    WRAPPER_NO_CONTRACT;
}

void ThrowOutOfMemoryWithinRange()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    EX_THROW(EEMessageException, (kOutOfMemoryException, IDS_EE_OUT_OF_MEMORY_WITHIN_RANGE));
}

HeapList* LoaderCodeHeap::CreateCodeHeap(CodeHeapRequestInfo *pInfo, LoaderHeap *pJitMetaHeap)
{
    CONTRACT(HeapList *) {
        THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    size_t * pPrivatePCLBytes   = NULL;
    size_t   reserveSize        = pInfo->getReserveSize();
    size_t   initialRequestSize = pInfo->getRequestSize();
    const BYTE *   loAddr       = pInfo->m_loAddr;
    const BYTE *   hiAddr       = pInfo->m_hiAddr;

    // Make sure that what we are reserving will fix inside a DWORD
    if (reserveSize != (DWORD) reserveSize)
    {
        _ASSERTE(!"reserveSize does not fit in a DWORD");
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }

#ifdef ENABLE_PERF_COUNTERS
    pPrivatePCLBytes   = &(GetPerfCounters().m_Loading.cbLoaderHeapSize);
#endif

    LOG((LF_JIT, LL_INFO100,
         "Request new LoaderCodeHeap::CreateCodeHeap(%08x, %08x, for loader allocator" FMT_ADDR "in" FMT_ADDR ".." FMT_ADDR ")\n",
         (DWORD) reserveSize, (DWORD) initialRequestSize, DBG_ADDR(pInfo->m_pAllocator), DBG_ADDR(loAddr), DBG_ADDR(hiAddr)
                                ));

    NewHolder<LoaderCodeHeap> pCodeHeap(new LoaderCodeHeap(pPrivatePCLBytes));

    BYTE * pBaseAddr = NULL;
    DWORD dwSizeAcquiredFromInitialBlock = 0;

    pBaseAddr = (BYTE *)pInfo->m_pAllocator->GetCodeHeapInitialBlock(loAddr, hiAddr, (DWORD)initialRequestSize, &dwSizeAcquiredFromInitialBlock);
    if (pBaseAddr != NULL)
    {
        pCodeHeap->m_LoaderHeap.SetReservedRegion(pBaseAddr, dwSizeAcquiredFromInitialBlock, FALSE);
    }
    else
    {
        if (loAddr != NULL || hiAddr != NULL)
        {
            pBaseAddr = ClrVirtualAllocWithinRange(loAddr, hiAddr,
                                                   reserveSize, MEM_RESERVE, PAGE_NOACCESS);
            if (!pBaseAddr)
                ThrowOutOfMemoryWithinRange();
        }
        else
        {
            pBaseAddr = ClrVirtualAllocExecutable(reserveSize, MEM_RESERVE, PAGE_NOACCESS);
            if (!pBaseAddr)
                ThrowOutOfMemory();
        }
        pCodeHeap->m_LoaderHeap.SetReservedRegion(pBaseAddr, reserveSize, TRUE);
    }


    // this first allocation is critical as it sets up correctly the loader heap info
    HeapList *pHp = (HeapList*)pCodeHeap->m_LoaderHeap.AllocMem(sizeof(HeapList));

    pHp->pHeap = pCodeHeap;

    size_t heapSize = pCodeHeap->m_LoaderHeap.GetReservedBytesFree();
    size_t nibbleMapSize = HEAP2MAPSIZE(ROUND_UP_TO_PAGE(heapSize));

    pHp->startAddress    = (TADDR)pHp + sizeof(HeapList);

    pHp->endAddress      = pHp->startAddress;
    pHp->maxCodeHeapSize = heapSize;

    _ASSERTE(heapSize >= initialRequestSize);

    // We do not need to memset this memory, since ClrVirtualAlloc() guarantees that the memory is zero.
    // Furthermore, if we avoid writing to it, these pages don't come into our working set

    pHp->bFull           = false;
    pHp->bFullForJumpStubs = false;

    pHp->cBlocks         = 0;

    pHp->mapBase         = ROUND_DOWN_TO_PAGE(pHp->startAddress);  // round down to next lower page align
    pHp->pHdrMap         = (DWORD*)(void*)pJitMetaHeap->AllocMem(S_SIZE_T(nibbleMapSize));

    LOG((LF_JIT, LL_INFO100,
         "Created new CodeHeap(" FMT_ADDR ".." FMT_ADDR ")\n",
         DBG_ADDR(pHp->startAddress), DBG_ADDR(pHp->startAddress+pHp->maxCodeHeapSize)
         ));

#ifdef _WIN64
    emitJump(pHp->CLRPersonalityRoutine, (void *)ProcessCLRException);
#endif

    pCodeHeap.SuppressRelease();
    RETURN pHp;
}

void * LoaderCodeHeap::AllocMemForCode_NoThrow(size_t header, size_t size, DWORD alignment)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (m_cbMinNextPad > (SSIZE_T)header) header = m_cbMinNextPad;

    void * p = m_LoaderHeap.AllocMemForCode_NoThrow(header, size, alignment);
    if (p == NULL)
        return NULL;

    // If the next allocation would have started in the same nibble map entry, allocate extra space to prevent it from happening
    // Note that m_cbMinNextPad can be negative
    m_cbMinNextPad = ALIGN_UP((SIZE_T)p + 1, BYTES_PER_BUCKET) - ((SIZE_T)p + size);

    return p;
}

void CodeHeapRequestInfo::Init()
{ 
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION((m_hiAddr == 0) ||
                     ((m_loAddr < m_hiAddr) &&
                      ((m_loAddr + m_requestSize) < m_hiAddr)));
    } CONTRACTL_END;

    if (m_pAllocator == NULL)
        m_pAllocator = m_pMD->GetLoaderAllocatorForCode();
    m_isDynamicDomain = (m_pMD != NULL) ? m_pMD->IsLCGMethod() : false;
    m_isCollectible = m_pAllocator->IsCollectible() ? true : false;
}

#ifdef WIN64EXCEPTIONS

#ifdef _WIN64
extern "C" PRUNTIME_FUNCTION GetRuntimeFunctionCallback(IN ULONG64   ControlPc,
                                                        IN PVOID     Context)
#else
extern "C" PRUNTIME_FUNCTION GetRuntimeFunctionCallback(IN ULONG     ControlPc,
                                                        IN PVOID     Context)
#endif
{
    WRAPPER_NO_CONTRACT;

    PRUNTIME_FUNCTION prf = NULL;

    // We must preserve this so that GCStress=4 eh processing doesnt kill last error.
    BEGIN_PRESERVE_LAST_ERROR;

#ifdef ENABLE_CONTRACTS
    // See comment in code:Thread::SwitchIn and SwitchOut.
    Thread *pThread = GetThread();
    if (!(pThread && pThread->HasThreadStateNC(Thread::TSNC_InTaskSwitch)))
    {

    // Some 64-bit OOM tests use the hosting interface to re-enter the CLR via
    // RtlVirtualUnwind to track unique stacks at each failure point. RtlVirtualUnwind can
    // result in the EEJitManager taking a reader lock. This, in turn, results in a
    // CANNOT_TAKE_LOCK contract violation if a CANNOT_TAKE_LOCK function were on the stack
    // at the time. While it's theoretically possible for "real" hosts also to re-enter the
    // CLR via RtlVirtualUnwind, generally they don't, and we'd actually like to catch a real
    // host causing such a contract violation. Therefore, we'd like to suppress such contract
    // asserts when these OOM tests are running, but continue to enforce the contracts by
    // default. This function returns whether to suppress locking violations.
    CONDITIONAL_CONTRACT_VIOLATION(
        TakesLockViolation, 
        g_pConfig->SuppressLockViolationsOnReentryFromOS());
#endif // ENABLE_CONTRACTS

    EECodeInfo codeInfo((PCODE)ControlPc);
    if (codeInfo.IsValid())
        prf = codeInfo.GetFunctionEntry();

    LOG((LF_EH, LL_INFO1000000, "GetRuntimeFunctionCallback(%p) returned %p\n", ControlPc, prf));

#ifdef ENABLE_CONTRACTS
    }
#endif // ENABLE_CONTRACTS

    END_PRESERVE_LAST_ERROR;

    return  prf;
}
#endif // WIN64EXCEPTIONS

HeapList* EEJitManager::NewCodeHeap(CodeHeapRequestInfo *pInfo, DomainCodeHeapList *pADHeapList)
{
    CONTRACT(HeapList *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    size_t initialRequestSize = pInfo->getRequestSize();
    size_t minReserveSize = VIRTUAL_ALLOC_RESERVE_GRANULARITY; //     ( 64 KB)           

#ifdef _WIN64
    if (pInfo->m_hiAddr == 0)
    {
        if (pADHeapList->m_CodeHeapList.Count() > CODE_HEAP_SIZE_INCREASE_THRESHOLD)
        {
            minReserveSize *= 4; // Increase the code heap size to 256 KB for workloads with a lot of code.
        }

        // For non-DynamicDomains that don't have a loAddr/hiAddr range
        // we bump up the reserve size for the 64-bit platforms
        if (!pInfo->IsDynamicDomain())
        {
            minReserveSize *= 4; // CodeHeaps are larger on AMD64 (256 KB to 1024 KB)
        }
    }
#endif

    // <BUGNUM> VSW 433293 </BUGNUM>
    // SETUP_NEW_BLOCK reserves the first sizeof(LoaderHeapBlock) bytes for LoaderHeapBlock.
    // In other word, the first m_pAllocPtr starts at sizeof(LoaderHeapBlock) bytes 
    // after the allocated memory. Therefore, we need to take it into account.
    size_t requestAndHeadersSize = sizeof(LoaderHeapBlock) + sizeof(HeapList) + initialRequestSize;

    size_t reserveSize = requestAndHeadersSize;
    if (reserveSize < minReserveSize)
        reserveSize = minReserveSize;
    reserveSize = ALIGN_UP(reserveSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

    pInfo->setReserveSize(reserveSize);

    HeapList *pHp = NULL;

    DWORD flags = RangeSection::RANGE_SECTION_CODEHEAP;

    if (pInfo->IsDynamicDomain())
    {
        flags |= RangeSection::RANGE_SECTION_COLLECTIBLE;
        pHp = HostCodeHeap::CreateCodeHeap(pInfo, this);
    }
    else
    {
        LoaderHeap *pJitMetaHeap = pADHeapList->m_pAllocator->GetLowFrequencyHeap();

        if (pInfo->IsCollectible())
            flags |= RangeSection::RANGE_SECTION_COLLECTIBLE;

        pHp = LoaderCodeHeap::CreateCodeHeap(pInfo, pJitMetaHeap);
    }

    _ASSERTE (pHp != NULL);
    _ASSERTE (pHp->maxCodeHeapSize >= initialRequestSize);

    pHp->SetNext(GetCodeHeapList());

    EX_TRY
    {
        TADDR pStartRange = (TADDR) pHp;
        TADDR pEndRange = (TADDR) &((BYTE*)pHp->startAddress)[pHp->maxCodeHeapSize];

        ExecutionManager::AddCodeRange(pStartRange,
                                       pEndRange,
                                       this,
                                       (RangeSection::RangeSectionFlags)flags,
                                       pHp);
        //
        // add a table to cover each range in the range list
        //
        InstallEEFunctionTable(
                  (PVOID)pStartRange,   // this is just an ID that gets passed to RtlDeleteFunctionTable;
                  (PVOID)pStartRange,
                  (ULONG)((ULONG64)pEndRange - (ULONG64)pStartRange),
                  GetRuntimeFunctionCallback,
                  this,
                  DYNFNTABLE_JIT);
    }
    EX_CATCH
    {
        // If we failed to alloc memory in ExecutionManager::AddCodeRange()
        // then we will delete the LoaderHeap that we allocated

        // pHp is allocated in pHeap, so only need to delete the LoaderHeap itself
        delete pHp->pHeap;

        pHp = NULL;
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (pHp == NULL)
    {
        ThrowOutOfMemory();
    }

    m_pCodeHeap = pHp;

    HeapList **ppHeapList = pADHeapList->m_CodeHeapList.AppendThrowing();
    *ppHeapList = pHp;

    RETURN(pHp);
}

void* EEJitManager::allocCodeRaw(CodeHeapRequestInfo *pInfo,
                                 size_t header, size_t blockSize, unsigned align, 
                                 HeapList ** ppCodeHeap /* Writeback, Can be null */ )
{
    CONTRACT(void *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    pInfo->setRequestSize(header+blockSize+(align-1));

    // Initialize the writeback value to NULL if a non-NULL pointer was provided
    if (ppCodeHeap)
        *ppCodeHeap = NULL;

    void *      mem       = NULL;

    bool bForJumpStubs = (pInfo->m_loAddr != 0) || (pInfo->m_hiAddr != 0);
    bool bUseCachedDynamicCodeHeap = pInfo->IsDynamicDomain();

    HeapList * pCodeHeap;

    for (;;)
    {
        // Avoid going through the full list in the common case - try to use the most recently used codeheap
        if (bUseCachedDynamicCodeHeap)
        {
            pCodeHeap = (HeapList *)pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap;
            pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap = NULL;
        }
        else
        {
            pCodeHeap = (HeapList *)pInfo->m_pAllocator->m_pLastUsedCodeHeap;
            pInfo->m_pAllocator->m_pLastUsedCodeHeap = NULL;
        }


        // If we will use a cached code heap for jump stubs, ensure that the code heap meets the loAddr and highAddr constraint
        if (bForJumpStubs && pCodeHeap && !CanUseCodeHeap(pInfo, pCodeHeap))
        {
            pCodeHeap = NULL;
        }

        // If we don't have a cached code heap or can't use it, get a code heap
        if (pCodeHeap == NULL)
        {
            pCodeHeap = GetCodeHeap(pInfo);
            if (pCodeHeap == NULL)
                break;
        }

#ifdef _WIN64
        if (!bForJumpStubs)
        {
            //
            // Keep a small reserve at the end of the codeheap for jump stubs. It should reduce
            // chance that we won't be able allocate jump stub because of lack of suitable address space.
            //
            // It is not a perfect solution. Ideally, we would be able to either ensure that jump stub
            // allocation won't fail or handle jump stub allocation gracefully (see DevDiv #381823 and 
            // related bugs for details).
            //
            static int codeHeapReserveForJumpStubs = -1;

            if (codeHeapReserveForJumpStubs == -1)
                codeHeapReserveForJumpStubs = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CodeHeapReserveForJumpStubs);

            size_t reserveForJumpStubs = codeHeapReserveForJumpStubs * (pCodeHeap->maxCodeHeapSize / 100);

            size_t minReserveForJumpStubs = sizeof(CodeHeader) +
                sizeof(JumpStubBlockHeader) + (size_t) DEFAULT_JUMPSTUBS_PER_BLOCK * BACK_TO_BACK_JUMP_ALLOCATE_SIZE +
                CODE_SIZE_ALIGN + BYTES_PER_BUCKET;

            // Reserve only if the size can fit a cluster of jump stubs
            if (reserveForJumpStubs > minReserveForJumpStubs)
            {
                size_t occupiedSize = pCodeHeap->endAddress - pCodeHeap->startAddress;

                if (occupiedSize + pInfo->getRequestSize() + reserveForJumpStubs > pCodeHeap->maxCodeHeapSize)
                {
                    pCodeHeap->SetHeapFull();
                    continue;
                }
            }
        }
#endif

        mem = (pCodeHeap->pHeap)->AllocMemForCode_NoThrow(header, blockSize, align);
        if (mem != NULL)
            break;

        // The current heap couldn't handle our request. Mark it as full.
        if (bForJumpStubs)
            pCodeHeap->SetHeapFullForJumpStubs();
        else
            pCodeHeap->SetHeapFull();
    }

    if (mem == NULL)
    {
        // Let us create a new heap.

        DomainCodeHeapList *pList = GetCodeHeapList(pInfo->m_pMD, pInfo->m_pAllocator);
        if (pList == NULL)
        {
            // not found so need to create the first one
            pList = CreateCodeHeapList(pInfo);
            _ASSERTE(pList == GetCodeHeapList(pInfo->m_pMD, pInfo->m_pAllocator));
        }
        _ASSERTE(pList);

        pCodeHeap = NewCodeHeap(pInfo, pList);
        _ASSERTE(pCodeHeap);

        mem = (pCodeHeap->pHeap)->AllocMemForCode_NoThrow(header, blockSize, align);
        if (mem == NULL)
            ThrowOutOfMemory();
        _ASSERTE(mem);
    }
    
    if (bUseCachedDynamicCodeHeap)
    {
        pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap = pCodeHeap;
    }
    else
    {
        pInfo->m_pAllocator->m_pLastUsedCodeHeap = pCodeHeap;
    }


    // Record the pCodeHeap value into ppCodeHeap, if a non-NULL pointer was provided
    if (ppCodeHeap)
        *ppCodeHeap = pCodeHeap;

    _ASSERTE((TADDR)mem >= pCodeHeap->startAddress);

    if (((TADDR) mem)+blockSize > (TADDR)pCodeHeap->endAddress)
    {
        // Update the CodeHeap endAddress
        pCodeHeap->endAddress = (TADDR)mem+blockSize;
    }

    RETURN(mem);
}

CodeHeader* EEJitManager::allocCode(MethodDesc* pMD, size_t blockSize, CorJitAllocMemFlag flag
#ifdef WIN64EXCEPTIONS
                                    , UINT nUnwindInfos
                                    , TADDR * pModuleBase
#endif
                                    )
{
    CONTRACT(CodeHeader *) {
        THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    //
    // Alignment
    //

    unsigned alignment = CODE_SIZE_ALIGN;
    
    if ((flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN) != 0) 
    {
        alignment = max(alignment, 16);
    }
    
#if defined(_TARGET_X86_)
    // when not optimizing for code size, 8-byte align the method entry point, so that
    // the JIT can in turn 8-byte align the loop entry headers.
    // 
    // when ReJIT is enabled, 8-byte-align the method entry point so that we may use an
    // 8-byte interlocked operation to atomically poke the top most bytes (e.g., to
    // redirect the rejit jmp-stamp at the top of the method from the prestub to the
    // rejitted code, or to reinstate original code on a revert).
    else if ((g_pConfig->GenOptimizeType() != OPT_SIZE) ||
        ReJitManager::IsReJITEnabled())
    {
        alignment = max(alignment, 8);
    }
#endif

    //
    // Compute header layout
    //

    SIZE_T totalSize = blockSize;

#if defined(USE_INDIRECT_CODEHEADER)
    SIZE_T realHeaderSize = offsetof(RealCodeHeader, unwindInfos[0]) + (sizeof(RUNTIME_FUNCTION) * nUnwindInfos); 

    // if this is a LCG method then we will be allocating the RealCodeHeader
    // following the code so that the code block can be removed easily by 
    // the LCG code heap.
    if (pMD->IsLCGMethod())
    {
        totalSize = ALIGN_UP(totalSize, sizeof(void*)) + realHeaderSize;
        static_assert_no_msg(CODE_SIZE_ALIGN >= sizeof(void*));
    }
#endif  // USE_INDIRECT_CODEHEADER

    CodeHeader * pCodeHdr = NULL;

    CodeHeapRequestInfo requestInfo(pMD);

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        HeapList *pCodeHeap = NULL;

        TADDR pCode = (TADDR) allocCodeRaw(&requestInfo, sizeof(CodeHeader), totalSize, alignment, &pCodeHeap);

        _ASSERTE(pCodeHeap);

        if (pMD->IsLCGMethod())
        {
            pMD->AsDynamicMethodDesc()->GetLCGMethodResolver()->m_recordCodePointer = (void*) pCode;
        }

        _ASSERTE(IS_ALIGNED(pCode, alignment));

        JIT_PERF_UPDATE_X86_CODE_SIZE(totalSize);

        // Initialize the CodeHeader *BEFORE* we publish this code range via the nibble
        // map so that we don't have to harden readers against uninitialized data.
        // However because we hold the lock, this initialization should be fast and cheap!

        pCodeHdr = ((CodeHeader *)pCode) - 1;

#ifdef USE_INDIRECT_CODEHEADER
        if (pMD->IsLCGMethod())
        {
            pCodeHdr->SetRealCodeHeader((BYTE*)pCode + ALIGN_UP(blockSize, sizeof(void*)));
        }
        else
        {
            // TODO: think about the CodeHeap carrying around a RealCodeHeader chunking mechanism
            //
            // allocate the real header in the low frequency heap
            BYTE* pRealHeader = (BYTE*)(void*)pMD->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(realHeaderSize));
            pCodeHdr->SetRealCodeHeader(pRealHeader);
        }
#endif

        pCodeHdr->SetDebugInfo(NULL);
        pCodeHdr->SetEHInfo(NULL);
        pCodeHdr->SetGCInfo(NULL);
        pCodeHdr->SetMethodDesc(pMD);
#ifdef WIN64EXCEPTIONS
        pCodeHdr->SetNumberOfUnwindInfos(nUnwindInfos);
        *pModuleBase = (TADDR)pCodeHeap;
#endif

        NibbleMapSet(pCodeHeap, pCode, TRUE);
    }

    RETURN(pCodeHdr);
}

EEJitManager::DomainCodeHeapList *EEJitManager::GetCodeHeapList(MethodDesc *pMD, LoaderAllocator *pAllocator, BOOL fDynamicOnly)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    DomainCodeHeapList *pList = NULL;
    DomainCodeHeapList **ppList = NULL;
    int count = 0;

    // get the appropriate list of heaps
    // pMD is NULL for NGen modules during Module::LoadTokenTables
    if (fDynamicOnly || (pMD != NULL && pMD->IsLCGMethod()))
    {
        ppList = m_DynamicDomainCodeHeaps.Table();
        count = m_DynamicDomainCodeHeaps.Count();
    }
    else
    {
        ppList = m_DomainCodeHeaps.Table();
        count = m_DomainCodeHeaps.Count();
    }

    // this is a virtual call - pull it out of the loop
    BOOL fCanUnload = pAllocator->CanUnload();

    // look for a DomainCodeHeapList
    for (int i=0; i < count; i++)
    {
        if (ppList[i]->m_pAllocator == pAllocator ||
            (!fCanUnload && !ppList[i]->m_pAllocator->CanUnload()))
        {
            pList = ppList[i];
            break;
        }
    }
    return pList;
}

HeapList* EEJitManager::GetCodeHeap(CodeHeapRequestInfo *pInfo)
{
    CONTRACT(HeapList *) {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACT_END;

    HeapList *pResult = NULL;

    _ASSERTE(pInfo->m_pAllocator != NULL);

    // loop through the m_DomainCodeHeaps to find the AppDomain
    // if not found, then create it
    DomainCodeHeapList *pList = GetCodeHeapList(pInfo->m_pMD, pInfo->m_pAllocator);
    if (pList)
    {
        // Set pResult to the largest non-full HeapList
        // that also satisfies the [loAddr..hiAddr] constraint
        for (int i=0; i < pList->m_CodeHeapList.Count(); i++)
        {
            HeapList *pCurrent   = pList->m_CodeHeapList[i];

            // Validate that the code heap can be used for the current request
            if(CanUseCodeHeap(pInfo, pCurrent))
            {
                if (pResult == NULL)
                {
                    // pCurrent is the first (and possibly only) heap that would satistfy
                    pResult = pCurrent;
                }
                // We use the initial creation size as a discriminator (i.e largest heap)
                else if (pResult->maxCodeHeapSize < pCurrent->maxCodeHeapSize)
                {
                    pResult = pCurrent;
                }
            }
        }
    }

    RETURN (pResult);
}

bool EEJitManager::CanUseCodeHeap(CodeHeapRequestInfo *pInfo, HeapList *pCodeHeap)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    bool retVal = false;

    if ((pInfo->m_loAddr == 0) && (pInfo->m_hiAddr == 0))
    {
        if (!pCodeHeap->IsHeapFull())
        {
            // We have no constraint so this non empty heap will be able to satistfy our request
            if (pInfo->IsDynamicDomain())
            {
                retVal = true;
            }
            else
            {
                BYTE * lastAddr = (BYTE *) pCodeHeap->startAddress + pCodeHeap->maxCodeHeapSize;

                BYTE * loRequestAddr  = (BYTE *) pCodeHeap->endAddress;
                BYTE * hiRequestAddr  = loRequestAddr + pInfo->getRequestSize() + BYTES_PER_BUCKET;
                if (hiRequestAddr <= lastAddr)
                {
                    retVal = true;
                }
            }
        }
    }
    else
    {
        if (!pCodeHeap->IsHeapFullForJumpStubs())
        {
            // We also check to see if an allocation in this heap would satistfy
            // the [loAddr..hiAddr] requirement
            
            // Calculate the byte range that can ever be returned by
            // an allocation in this HeapList element
            //
            BYTE * firstAddr      = (BYTE *) pCodeHeap->startAddress;
            BYTE * lastAddr       = (BYTE *) pCodeHeap->startAddress + pCodeHeap->maxCodeHeapSize;

            _ASSERTE(pCodeHeap->startAddress <= pCodeHeap->endAddress);
            _ASSERTE(firstAddr <= lastAddr);
            
            if (pInfo->IsDynamicDomain())
            {
                // We check to see if every allocation in this heap
                // will satistfy the [loAddr..hiAddr] requirement.
                //
                // Dynamic domains use a free list allocator, 
                // thus we can receive any address in the range
                // when calling AllocMemory with a DynamicDomain
            
                // [firstaddr .. lastAddr] must be entirely within
                // [pInfo->m_loAddr .. pInfo->m_hiAddr]
                //
                if ((pInfo->m_loAddr <= firstAddr)   &&
                    (lastAddr        <= pInfo->m_hiAddr))
                {
                    // This heap will always satisfy our constraint
                    retVal = true;
                }
            }
            else // non-DynamicDomain
            {
                // Calculate the byte range that would be allocated for the
                // next allocation request into [loRequestAddr..hiRequestAddr]
                //
                BYTE * loRequestAddr  = (BYTE *) pCodeHeap->endAddress;
                BYTE * hiRequestAddr  = loRequestAddr + pInfo->getRequestSize() + BYTES_PER_BUCKET;
                _ASSERTE(loRequestAddr <= hiRequestAddr);

                // loRequestAddr and hiRequestAddr must be entirely within
                // [pInfo->m_loAddr .. pInfo->m_hiAddr]
                // additionally hiRequestAddr must also be less than
                // or equal to lastAddr
                //
                if ((pInfo->m_loAddr <= loRequestAddr)   &&
                    (hiRequestAddr   <= pInfo->m_hiAddr) &&
                    (hiRequestAddr   <= lastAddr))
                {
                   // This heap will be able to satistfy our constraint
                   retVal = true;
                }
            }
       }
   }

   return retVal; 
}

EEJitManager::DomainCodeHeapList * EEJitManager::CreateCodeHeapList(CodeHeapRequestInfo *pInfo)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    NewHolder<DomainCodeHeapList> pNewList(new DomainCodeHeapList());
    pNewList->m_pAllocator = pInfo->m_pAllocator;

    DomainCodeHeapList **ppList = NULL;
    if (pInfo->IsDynamicDomain())
        ppList = m_DynamicDomainCodeHeaps.AppendThrowing();
    else
        ppList = m_DomainCodeHeaps.AppendThrowing();
    *ppList = pNewList;

    return pNewList.Extract();
}

LoaderHeap *EEJitManager::GetJitMetaHeap(MethodDesc *pMD)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    LoaderAllocator *pAllocator = pMD->GetLoaderAllocator();
    _ASSERTE(pAllocator);

    return pAllocator->GetLowFrequencyHeap();
}

BYTE* EEJitManager::allocGCInfo(CodeHeader* pCodeHeader, DWORD blockSize, size_t * pAllocationSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    MethodDesc* pMD = pCodeHeader->GetMethodDesc();
    // sadly for light code gen I need the check in here. We should change GetJitMetaHeap
    if (pMD->IsLCGMethod()) 
    {
        CrstHolder ch(&m_CodeHeapCritSec);
        pCodeHeader->SetGCInfo((BYTE*)(void*)pMD->AsDynamicMethodDesc()->GetResolver()->GetJitMetaHeap()->New(blockSize));
    }
    else
    {
        pCodeHeader->SetGCInfo((BYTE*) (void*)GetJitMetaHeap(pMD)->AllocMem(S_SIZE_T(blockSize)));
    }
    _ASSERTE(pCodeHeader->GetGCInfo()); // AllocMem throws if there's not enough memory
    JIT_PERF_UPDATE_X86_CODE_SIZE(blockSize);

    * pAllocationSize = blockSize;  // Store the allocation size so we can backout later.
    
    return(pCodeHeader->GetGCInfo());
}

void* EEJitManager::allocEHInfoRaw(CodeHeader* pCodeHeader, DWORD blockSize, size_t * pAllocationSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    MethodDesc* pMD = pCodeHeader->GetMethodDesc();
    void * mem = NULL;

    // sadly for light code gen I need the check in here. We should change GetJitMetaHeap
    if (pMD->IsLCGMethod()) 
    {
        CrstHolder ch(&m_CodeHeapCritSec);
        mem = (void*)pMD->AsDynamicMethodDesc()->GetResolver()->GetJitMetaHeap()->New(blockSize);
    }
    else 
    {
        mem = (void*)GetJitMetaHeap(pMD)->AllocMem(S_SIZE_T(blockSize));
    }
    _ASSERTE(mem);   // AllocMem throws if there's not enough memory

    JIT_PERF_UPDATE_X86_CODE_SIZE(blockSize);

    * pAllocationSize = blockSize; // Store the allocation size so we can backout later.
    
    return(mem);
}


EE_ILEXCEPTION* EEJitManager::allocEHInfo(CodeHeader* pCodeHeader, unsigned numClauses, size_t * pAllocationSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // Note - pCodeHeader->phdrJitEHInfo - sizeof(size_t) contains the number of EH clauses

    DWORD temp =  EE_ILEXCEPTION::Size(numClauses);
    DWORD blockSize = 0;
    if (!ClrSafeInt<DWORD>::addition(temp, sizeof(size_t), blockSize))
        COMPlusThrowOM();

    BYTE *EHInfo = (BYTE*)allocEHInfoRaw(pCodeHeader, blockSize, pAllocationSize);

    pCodeHeader->SetEHInfo((EE_ILEXCEPTION*) (EHInfo + sizeof(size_t)));
    pCodeHeader->GetEHInfo()->Init(numClauses);
    *((size_t *)EHInfo) = numClauses;
    return(pCodeHeader->GetEHInfo());
}

JumpStubBlockHeader *  EEJitManager::allocJumpStubBlock(MethodDesc* pMD, DWORD numJumps, 
                                                        BYTE * loAddr, BYTE * hiAddr,
                                                        LoaderAllocator *pLoaderAllocator)
{
    CONTRACT(JumpStubBlockHeader *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(loAddr < hiAddr);
        PRECONDITION(pLoaderAllocator != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    _ASSERTE((sizeof(JumpStubBlockHeader) % CODE_SIZE_ALIGN) == 0);
    _ASSERTE(numJumps < MAX_M_ALLOCATED);

    size_t blockSize = sizeof(JumpStubBlockHeader) + (size_t) numJumps * BACK_TO_BACK_JUMP_ALLOCATE_SIZE;

    HeapList *pCodeHeap = NULL;
    CodeHeapRequestInfo    requestInfo(pMD, pLoaderAllocator, loAddr, hiAddr);

    TADDR                  mem;
    JumpStubBlockHeader *  pBlock;

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        mem = (TADDR) allocCodeRaw(&requestInfo, sizeof(TADDR), blockSize, CODE_SIZE_ALIGN, &pCodeHeap);

        // CodeHeader comes immediately before the block
        CodeHeader * pCodeHdr = (CodeHeader *) (mem - sizeof(CodeHeader));
        pCodeHdr->SetStubCodeBlockKind(STUB_CODE_BLOCK_JUMPSTUB);

        NibbleMapSet(pCodeHeap, mem, TRUE);

        pBlock = (JumpStubBlockHeader *)mem;

        _ASSERTE(IS_ALIGNED(pBlock, CODE_SIZE_ALIGN));
    
        JIT_PERF_UPDATE_X86_CODE_SIZE(blockSize);
    }

    pBlock->m_next            = NULL;
    pBlock->m_used            = 0;
    pBlock->m_allocated       = numJumps;
    if (pMD && pMD->IsLCGMethod())
        pBlock->SetHostCodeHeap(static_cast<HostCodeHeap*>(pCodeHeap->pHeap));
    else
        pBlock->SetLoaderAllocator(pLoaderAllocator);

    LOG((LF_JIT, LL_INFO1000, "Allocated new JumpStubBlockHeader for %d stubs at" FMT_ADDR " in loader allocator " FMT_ADDR "\n",
         numJumps, DBG_ADDR(pBlock) , DBG_ADDR(pLoaderAllocator) ));

    RETURN(pBlock);
}

void * EEJitManager::allocCodeFragmentBlock(size_t blockSize, unsigned alignment, LoaderAllocator *pLoaderAllocator, StubCodeBlockKind kind)
{
    CONTRACT(void *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(pLoaderAllocator != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    HeapList *pCodeHeap = NULL;
    CodeHeapRequestInfo    requestInfo(NULL, pLoaderAllocator, NULL, NULL);

    TADDR                  mem;

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        mem = (TADDR) allocCodeRaw(&requestInfo, sizeof(TADDR), blockSize, alignment, &pCodeHeap);

        // CodeHeader comes immediately before the block
        CodeHeader * pCodeHdr = (CodeHeader *) (mem - sizeof(CodeHeader));
        pCodeHdr->SetStubCodeBlockKind(kind);

        NibbleMapSet(pCodeHeap, (TADDR)mem, TRUE);
    }

    RETURN((void *)mem);
}

#endif // !DACCESS_COMPILE


PTR_VOID EEJitManager::GetGCInfo(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return GetCodeHeader(MethodToken)->GetGCInfo();
}

// creates an enumeration and returns the number of EH clauses
unsigned EEJitManager::InitializeEHEnumeration(const METHODTOKEN& MethodToken, EH_CLAUSE_ENUMERATOR* pEnumState)
{
    LIMITED_METHOD_CONTRACT;
    EE_ILEXCEPTION * EHInfo = GetCodeHeader(MethodToken)->GetEHInfo();

    pEnumState->iCurrentPos = 0;     // since the EH info is not compressed, the clause number is used to do the enumeration
    pEnumState->pExceptionClauseArray = NULL;

    if (!EHInfo)
        return 0;

    pEnumState->pExceptionClauseArray = dac_cast<TADDR>(EHInfo->EHClause(0));
    return *(dac_cast<PTR_unsigned>(dac_cast<TADDR>(EHInfo) - sizeof(size_t)));
}

PTR_EXCEPTION_CLAUSE_TOKEN EEJitManager::GetNextEHClause(EH_CLAUSE_ENUMERATOR* pEnumState,
                              EE_ILEXCEPTION_CLAUSE* pEHClauseOut)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    unsigned iCurrentPos = pEnumState->iCurrentPos;
    pEnumState->iCurrentPos++;

    EE_ILEXCEPTION_CLAUSE* pClause = &(dac_cast<PTR_EE_ILEXCEPTION_CLAUSE>(pEnumState->pExceptionClauseArray)[iCurrentPos]);
    *pEHClauseOut = *pClause;
    return dac_cast<PTR_EXCEPTION_CLAUSE_TOKEN>(pClause);
}

#ifndef DACCESS_COMPILE
TypeHandle EEJitManager::ResolveEHClause(EE_ILEXCEPTION_CLAUSE* pEHClause,
                                         CrawlFrame *pCf)
{
    // We don't want to use a runtime contract here since this codepath is used during
    // the processing of a hard SO. Contracts use a significant amount of stack
    // which we can't afford for those cases.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    _ASSERTE(NULL != pCf);
    _ASSERTE(NULL != pEHClause);
    _ASSERTE(IsTypedHandler(pEHClause));
    

    TypeHandle typeHnd = TypeHandle();
    mdToken typeTok = mdTokenNil;

    {
        CrstHolder chRead(&m_EHClauseCritSec);
        if (HasCachedTypeHandle(pEHClause))
        {
            typeHnd = TypeHandle::FromPtr(pEHClause->TypeHandle);
        }
        else
        {
            typeTok = pEHClause->ClassToken;
        }
    }

    if (!typeHnd.IsNull())
    {
        return typeHnd;
    }
    
    MethodDesc* pMD = pCf->GetFunction();
    Module* pModule = pMD->GetModule();
    PREFIX_ASSUME(pModule != NULL);

    SigTypeContext typeContext(pMD);
    VarKind k = hasNoVars;
    
    // In the vast majority of cases the code under the "if" below
    // will not be executed.
    //
    // First grab the representative instantiations.  For code
    // shared by multiple generic instantiations these are the
    // canonical (representative) instantiation.
    if (TypeFromToken(typeTok) == mdtTypeSpec)
    {
        PCCOR_SIGNATURE pSig;
        ULONG cSig;
        IfFailThrow(pModule->GetMDImport()->GetTypeSpecFromToken(typeTok, &pSig, &cSig));
        
        SigPointer psig(pSig, cSig);
        k = psig.IsPolyType(&typeContext);
        
        // Grab the active class and method instantiation.  This exact instantiation is only
        // needed in the corner case of "generic" exception catching in shared
        // generic code.  We don't need the exact instantiation if the token
        // doesn't contain E_T_VAR or E_T_MVAR.
        if ((k & hasSharableVarsMask) != 0)
        {
            Instantiation classInst;
            Instantiation methodInst;
            pCf->GetExactGenericInstantiations(&classInst, &methodInst);
            SigTypeContext::InitTypeContext(pMD,classInst, methodInst,&typeContext);
        }
    }

    typeHnd = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, typeTok, &typeContext, 
                                                          ClassLoader::ReturnNullIfNotFound);

    // If the type (pModule,typeTok) was not loaded or not
    // restored then the exception object won't have this type, because an
    // object of this type has not been allocated.    
    if (typeHnd.IsNull())
        return typeHnd;

    // We can cache any exception specification except:
    //   - If the type contains type variables in generic code,
    //     e.g. catch E<T> where T is a type variable.
    // We CANNOT cache E<T> in non-shared instantiations of generic code because
    // there is only one EHClause cache for the IL, shared across all instantiations.
    //
    if((k & hasAnyVarsMask) == 0)
    {
        CrstHolder chWrite(&m_EHClauseCritSec);
    
        // Note another thread might have beaten us to it ...
        if (!HasCachedTypeHandle(pEHClause))
        {
            // We should never cache a NULL typeHnd.
            _ASSERTE(!typeHnd.IsNull());
            pEHClause->TypeHandle = typeHnd.AsPtr();
            SetHasCachedTypeHandle(pEHClause);            
        }
        else
        {
            // If we raced in here with aother thread and got held up on the lock, then we just need to return the
            // type handle that the other thread put into the clause.
            // The typeHnd we found and the typeHnd the racing thread found should always be the same
            _ASSERTE(typeHnd.AsPtr() == pEHClause->TypeHandle);
            typeHnd = TypeHandle::FromPtr(pEHClause->TypeHandle);
        }
    }
    return typeHnd;
}

void EEJitManager::RemoveJitData (CodeHeader * pCHdr, size_t GCinfo_len, size_t EHinfo_len)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    MethodDesc* pMD = pCHdr->GetMethodDesc();

    if (pMD->IsLCGMethod()) {

        void * codeStart = (pCHdr + 1);

        {
            CrstHolder ch(&m_CodeHeapCritSec);

            LCGMethodResolver * pResolver = pMD->AsDynamicMethodDesc()->GetLCGMethodResolver();

            // Clear the pointer only if it matches what we are about to free. There may be cases where the JIT is reentered and
            // the method JITed multiple times.
            if (pResolver->m_recordCodePointer == codeStart)
                pResolver->m_recordCodePointer = NULL;
        }

#if defined(_TARGET_AMD64_)
        // Remove the unwind information (if applicable)
        UnwindInfoTable::UnpublishUnwindInfoForMethod((TADDR)codeStart);
#endif // defined(_TARGET_AMD64_)

        HostCodeHeap* pHeap = HostCodeHeap::GetCodeHeap((TADDR)codeStart);
        FreeCodeMemory(pHeap, codeStart);

        // We are leaking GCInfo and EHInfo. They will be freed once the dynamic method is destroyed.

        return;
    }

    {
        CrstHolder ch(&m_CodeHeapCritSec);

        HeapList *pHp = GetCodeHeapList();

        while (pHp && ((pHp->startAddress > (TADDR)pCHdr) ||
                        (pHp->endAddress < (TADDR)pCHdr + sizeof(CodeHeader))))
        {
            pHp = pHp->GetNext();
        }

        _ASSERTE(pHp && pHp->pHdrMap);
        _ASSERTE(pHp && pHp->cBlocks);

        // Better to just return than AV?
        if (pHp == NULL)
            return;

        NibbleMapSet(pHp, (TADDR)(pCHdr + 1), FALSE);
    }

    // Backout the GCInfo  
    if (GCinfo_len > 0) {
        GetJitMetaHeap(pMD)->BackoutMem(pCHdr->GetGCInfo(), GCinfo_len);
    }
    
    // Backout the EHInfo  
    BYTE *EHInfo = (BYTE *)pCHdr->GetEHInfo();
    if (EHInfo) {
        EHInfo -= sizeof(size_t);

        _ASSERTE(EHinfo_len>0);
        GetJitMetaHeap(pMD)->BackoutMem(EHInfo, EHinfo_len);
    }  

    // <TODO>
    // TODO: Although we have backout the GCInfo and EHInfo, we haven't actually backout the
    //       code buffer itself. As a result, we might leak the CodeHeap if jitting fails after
    //       the code buffer is allocated.
    // 
    //       However, it appears non-trival to fix this.
    //       Here are some of the reasons:
    //       (1) AllocCode calls in AllocCodeRaw to alloc code buffer in the CodeHeap. The exact size 
    //           of the code buffer is not known until the alignment is calculated deep on the stack.
    //       (2) AllocCodeRaw is called in 3 different places. We might need to remember the 
    //           information for these places.
    //       (3) AllocCodeRaw might create a new CodeHeap. We should remember exactly which 
    //           CodeHeap is used to allocate the code buffer.
    //
    //       Fortunately, this is not a severe leak since the CodeHeap will be reclaimed on appdomain unload.
    //
    // </TODO>
    return;
}

// appdomain is being unloaded, so delete any data associated with it. We have to do this in two stages.
// On the first stage, we remove the elements from the list. On the second stage, which occurs after a GC
// we know that only threads who were in preemptive mode prior to the GC could possibly still be looking
// at an element that is about to be deleted. All such threads are guarded with a reader count, so if the
// count is 0, we can safely delete, otherwise we must add to the cleanup list to be deleted later. We know
// there can only be one unload at a time, so we can use a single var to hold the unlinked, but not deleted,
// elements.
void EEJitManager::Unload(LoaderAllocator *pAllocator)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    CrstHolder ch(&m_CodeHeapCritSec);

    DomainCodeHeapList **ppList = m_DomainCodeHeaps.Table();
    int count = m_DomainCodeHeaps.Count();

    for (int i=0; i < count; i++) {
        if (ppList[i]->m_pAllocator== pAllocator) {
            DomainCodeHeapList *pList = ppList[i];
            m_DomainCodeHeaps.DeleteByIndex(i);

            // pHeapList is allocated in pHeap, so only need to delete the LoaderHeap itself
            count = pList->m_CodeHeapList.Count();
            for (i=0; i < count; i++) {
                HeapList *pHeapList = pList->m_CodeHeapList[i];
                DeleteCodeHeap(pHeapList);
            }

            // this is ok to do delete as anyone accessing the DomainCodeHeapList structure holds the critical section.
            delete pList;

            break;
        }
    }
    ppList = m_DynamicDomainCodeHeaps.Table();
    count = m_DynamicDomainCodeHeaps.Count();
    for (int i=0; i < count; i++) {
        if (ppList[i]->m_pAllocator== pAllocator) {
            DomainCodeHeapList *pList = ppList[i];
            m_DynamicDomainCodeHeaps.DeleteByIndex(i);

            // pHeapList is allocated in pHeap, so only need to delete the CodeHeap itself
            count = pList->m_CodeHeapList.Count();
            for (i=0; i < count; i++) {
                HeapList *pHeapList = pList->m_CodeHeapList[i];
                // m_DynamicDomainCodeHeaps should only contain HostCodeHeap.
                RemoveFromCleanupList(static_cast<HostCodeHeap*>(pHeapList->pHeap));
                DeleteCodeHeap(pHeapList);
            }

            // this is ok to do delete as anyone accessing the DomainCodeHeapList structure holds the critical section.
            delete pList;

            break;
        }
    }

    ResetCodeAllocHint();
}

EEJitManager::DomainCodeHeapList::DomainCodeHeapList()
{
    LIMITED_METHOD_CONTRACT;
    m_pAllocator = NULL;
}

EEJitManager::DomainCodeHeapList::~DomainCodeHeapList()
{
    LIMITED_METHOD_CONTRACT;
}

void EEJitManager::RemoveCodeHeapFromDomainList(CodeHeap *pHeap, LoaderAllocator *pAllocator)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    // get the AppDomain heap list for pAllocator in m_DynamicDomainCodeHeaps
    DomainCodeHeapList *pList = GetCodeHeapList(NULL, pAllocator, TRUE);

    // go through the heaps and find and remove pHeap
    int count = pList->m_CodeHeapList.Count();
    for (int i = 0; i < count; i++) {
        HeapList *pHeapList = pList->m_CodeHeapList[i];
        if (pHeapList->pHeap == pHeap) {
            // found the heap to remove. If this is the only heap we remove the whole DomainCodeHeapList
            // otherwise we just remove this heap
            if (count == 1) {
                m_DynamicDomainCodeHeaps.Delete(pList);
                delete pList;
            }
            else
                pList->m_CodeHeapList.Delete(i);

            // if this heaplist is cached in the loader allocator, we must clear it
            if (pAllocator->m_pLastUsedDynamicCodeHeap == ((void *) pHeapList))
            {
                pAllocator->m_pLastUsedDynamicCodeHeap = NULL;
            }

            break;
        }
    }
}

void EEJitManager::FreeCodeMemory(HostCodeHeap *pCodeHeap, void * codeStart)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_CodeHeapCritSec);

    // FreeCodeMemory is only supported on LCG methods,
    // so pCodeHeap can only be a HostCodeHeap.

    // clean up the NibbleMap
    NibbleMapSet(pCodeHeap->m_pHeapList, (TADDR)codeStart, FALSE);

    // The caller of this method doesn't call HostCodeHeap->FreeMemForCode
    // directly because the operation should be protected by m_CodeHeapCritSec.
    pCodeHeap->FreeMemForCode(codeStart);
}

void ExecutionManager::CleanupCodeHeaps()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE (g_fProcessDetach || (GCHeap::IsGCInProgress()  && ::IsGCThread()));

    GetEEJitManager()->CleanupCodeHeaps();
}

void EEJitManager::CleanupCodeHeaps()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE (g_fProcessDetach || (GCHeap::IsGCInProgress() && ::IsGCThread()));

    CrstHolder ch(&m_CodeHeapCritSec);

    if (m_cleanupList == NULL)
        return;

    HostCodeHeap *pHeap = m_cleanupList;
    m_cleanupList = NULL;

    while (pHeap)
    {
        HostCodeHeap *pNextHeap = pHeap->m_pNextHeapToRelease;

        DWORD allocCount = pHeap->m_AllocationCount;
        if (allocCount == 0)
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - Destryoing CodeHeap [0x%p, vt(0x%x)] - ref count 0\n", pHeap, *(size_t*)pHeap));
            RemoveCodeHeapFromDomainList(pHeap, pHeap->m_pAllocator);
            DeleteCodeHeap(pHeap->m_pHeapList);
        }
        else
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - Restoring CodeHeap [0x%p, vt(0x%x)] - ref count %d\n", pHeap, *(size_t*)pHeap, allocCount));
        }
        pHeap = pNextHeap;
    }
}

void EEJitManager::RemoveFromCleanupList(HostCodeHeap *pCodeHeap)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    HostCodeHeap *pHeap = m_cleanupList;
    HostCodeHeap *pPrevHeap = NULL;
    while (pHeap)
    {
        if (pHeap == pCodeHeap)
        {
            if (pPrevHeap)
            {
                // remove current heap from list
                pPrevHeap->m_pNextHeapToRelease = pHeap->m_pNextHeapToRelease;
            }
            else
            {
                m_cleanupList = pHeap->m_pNextHeapToRelease;
            }
            break;
        }
        pPrevHeap = pHeap;
        pHeap = pHeap->m_pNextHeapToRelease;
    }
}

void EEJitManager::AddToCleanupList(HostCodeHeap *pCodeHeap)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    // it may happen that the current heap count goes to 0 and later on, before it is destroyed, it gets reused 
    // for another dynamic method. 
    // It's then possible that the ref count reaches 0 multiple times. If so we simply don't add it again
    // Also on cleanup we check the the ref count is actually 0.
    HostCodeHeap *pHeap = m_cleanupList;
    while (pHeap)
    {
        if (pHeap == pCodeHeap)
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p, vt(0x%x)] - Already in list\n", pCodeHeap, *(size_t*)pCodeHeap));
            break;
        }
        pHeap = pHeap->m_pNextHeapToRelease;
    }
    if (pHeap == NULL)
    {
        pCodeHeap->m_pNextHeapToRelease = m_cleanupList;
        m_cleanupList = pCodeHeap;
        LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p, vt(0x%x)] - ref count %d - Adding to cleanup list\n", pCodeHeap, *(size_t*)pCodeHeap, pCodeHeap->m_AllocationCount));
    }
}

void EEJitManager::DeleteCodeHeap(HeapList *pHeapList)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    HeapList *pHp = GetCodeHeapList();
    if (pHp == pHeapList)
        m_pCodeHeap = pHp->GetNext();
    else
    {
        HeapList *pHpNext = pHp->GetNext();
        
        while (pHpNext != pHeapList)
        {
            pHp = pHpNext;
            _ASSERTE(pHp != NULL);  // should always find the HeapList
            pHpNext = pHp->GetNext();
        }
        pHp->SetNext(pHeapList->GetNext());
    }

    DeleteEEFunctionTable((PVOID)pHeapList);

    ExecutionManager::DeleteRange((TADDR)pHeapList);

    LOG((LF_JIT, LL_INFO100, "DeleteCodeHeap start" FMT_ADDR "end" FMT_ADDR "\n",
                              (const BYTE*)pHeapList->startAddress, 
                              (const BYTE*)pHeapList->endAddress     ));

    // pHeapList is allocated in pHeap, so only need to delete the CodeHeap itself
    // !!! For SoC, compiler inserts code to write a special cookie at pHeapList->pHeap after delete operator, at least for debug code.
    // !!! Since pHeapList is deleted at the same time as pHeap, this causes AV.
    // delete pHeapList->pHeap;
    CodeHeap* pHeap = pHeapList->pHeap;
    delete pHeap;
}

#endif // #ifndef DACCESS_COMPILE

static CodeHeader * GetCodeHeaderFromDebugInfoRequest(const DebugInfoRequest & request)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;
    
    TADDR address = (TADDR) request.GetStartAddress();
    _ASSERTE(address != NULL);

    CodeHeader * pHeader = dac_cast<PTR_CodeHeader>(address & ~3) - 1;
    _ASSERTE(pHeader != NULL);    

    return pHeader;
}

//-----------------------------------------------------------------------------
// Get vars from Jit Store
//-----------------------------------------------------------------------------
BOOL EEJitManager::GetBoundariesAndVars(
        const DebugInfoRequest & request,
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        OUT ULONG32 * pcMap, 
        OUT ICorDebugInfo::OffsetMapping **ppMap,
        OUT ULONG32 * pcVars, 
        OUT ICorDebugInfo::NativeVarInfo **ppVars)
{
    CONTRACTL {
        THROWS;       // on OOM.
        GC_NOTRIGGER; // getting vars shouldn't trigger
        SUPPORTS_DAC;
    } CONTRACTL_END;

    CodeHeader * pHdr = GetCodeHeaderFromDebugInfoRequest(request);
    _ASSERTE(pHdr != NULL);

    PTR_BYTE pDebugInfo = pHdr->GetDebugInfo();
    
    // No header created, which means no jit information is available.
    if (pDebugInfo == NULL)
        return FALSE;

    // Uncompress. This allocates memory and may throw.
    CompressDebugInfo::RestoreBoundariesAndVars(
        fpNew, pNewData, // allocators
        pDebugInfo,      // input
        pcMap, ppMap,
        pcVars, ppVars); // output

    return TRUE;
}

#ifdef DACCESS_COMPILE
void CodeHeader::EnumMemoryRegions(CLRDataEnumMemoryFlags flags, IJitManager* pJitMan)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;    

    DAC_ENUM_DTHIS();
    
#ifdef USE_INDIRECT_CODEHEADER
    this->pRealCodeHeader.EnumMem();
#endif // USE_INDIRECT_CODEHEADER

    if (this->GetDebugInfo() != NULL)
    {
        CompressDebugInfo::EnumMemoryRegions(flags, this->GetDebugInfo());
    }
}

//-----------------------------------------------------------------------------
// Enumerate for minidumps.
//-----------------------------------------------------------------------------
void EEJitManager::EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, MethodDesc * pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DebugInfoRequest request;
    PCODE addrCode = pMD->GetNativeCode();
    request.InitFromStartingAddr(pMD, addrCode);

    CodeHeader * pHeader = GetCodeHeaderFromDebugInfoRequest(request);

    pHeader->EnumMemoryRegions(flags, NULL);
}
#endif // DACCESS_COMPILE

PCODE EEJitManager::GetCodeAddressForRelOffset(const METHODTOKEN& MethodToken, DWORD relOffset)
{
    WRAPPER_NO_CONTRACT;

    CodeHeader * pHeader = GetCodeHeader(MethodToken);
    return pHeader->GetCodeStartAddress() + relOffset;
}

BOOL EEJitManager::JitCodeToMethodInfo(
        RangeSection * pRangeSection,
        PCODE currentPC,
        MethodDesc ** ppMethodDesc,
        EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    _ASSERTE(pRangeSection != NULL);

    TADDR start = dac_cast<PTR_EEJitManager>(pRangeSection->pjit)->FindMethodCode(pRangeSection, currentPC);
    if (start == NULL)
        return FALSE;

    CodeHeader * pCHdr = PTR_CodeHeader(start - sizeof(CodeHeader));
    if (pCHdr->IsStubCodeBlock())
        return FALSE;

    _ASSERTE(pCHdr->GetMethodDesc()->SanityCheck());

    if (pCodeInfo)
    {
        pCodeInfo->m_methodToken = METHODTOKEN(pRangeSection, dac_cast<TADDR>(pCHdr));

        // This can be counted on for Jitted code. For NGEN code in the case
        // where we have hot/cold splitting this isn't valid and we need to
        // take into account cold code.
        pCodeInfo->m_relOffset = (DWORD)(PCODEToPINSTR(currentPC) - pCHdr->GetCodeStartAddress());

#ifdef WIN64EXCEPTIONS
        // Computed lazily by code:EEJitManager::LazyGetFunctionEntry
        pCodeInfo->m_pFunctionEntry = NULL;
#endif
    }

    if (ppMethodDesc)
    {
        *ppMethodDesc = pCHdr->GetMethodDesc();
    }
    return TRUE;
}

StubCodeBlockKind EEJitManager::GetStubCodeBlockKind(RangeSection * pRangeSection, PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    TADDR start = dac_cast<PTR_EEJitManager>(pRangeSection->pjit)->FindMethodCode(pRangeSection, currentPC);
    if (start == NULL)
        return STUB_CODE_BLOCK_NOCODE;
    CodeHeader * pCHdr = PTR_CodeHeader(start - sizeof(CodeHeader));
    return pCHdr->IsStubCodeBlock() ? pCHdr->GetStubCodeBlockKind() : STUB_CODE_BLOCK_MANAGED;
}

TADDR EEJitManager::FindMethodCode(PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    RangeSection * pRS = ExecutionManager::FindCodeRange(currentPC, ExecutionManager::GetScanFlags());
    if (pRS == NULL || (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP) == 0)
        return STUB_CODE_BLOCK_NOCODE;
    return dac_cast<PTR_EEJitManager>(pRS->pjit)->FindMethodCode(pRS, currentPC);
}

// Finds the header corresponding to the code at offset "delta".
// Returns NULL if there is no header for the given "delta"

TADDR EEJitManager::FindMethodCode(RangeSection * pRangeSection, PCODE currentPC)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(pRangeSection != NULL);

    HeapList *pHp = dac_cast<PTR_HeapList>(pRangeSection->pHeapListOrZapModule);

    if ((currentPC < pHp->startAddress) ||
        (currentPC > pHp->endAddress))
    {
        return NULL;
    }

    TADDR base = pHp->mapBase;
    TADDR delta = currentPC - base;
    PTR_DWORD pMap = pHp->pHdrMap;
    PTR_DWORD pMapStart = pMap;

    DWORD tmp;

    size_t startPos = ADDR2POS(delta);  // align to 32byte buckets
                                        // ( == index into the array of nibbles)
    DWORD  offset   = ADDR2OFFS(delta); // this is the offset inside the bucket + 1

    _ASSERTE(offset == (offset & NIBBLE_MASK));

    pMap += (startPos >> LOG2_NIBBLES_PER_DWORD); // points to the proper DWORD of the map

    // get DWORD and shift down our nibble

    PREFIX_ASSUME(pMap != NULL);
    tmp = VolatileLoadWithoutBarrier<DWORD>(pMap) >> POS2SHIFTCOUNT(startPos);

    if ((tmp & NIBBLE_MASK) && ((tmp & NIBBLE_MASK) <= offset) )
    {
        return base + POSOFF2ADDR(startPos, tmp & NIBBLE_MASK);
    }

    // Is there a header in the remainder of the DWORD ?
    tmp = tmp >> NIBBLE_SIZE;

    if (tmp)
    {
        startPos--;
        while (!(tmp & NIBBLE_MASK))
        {
            tmp = tmp >> NIBBLE_SIZE;
            startPos--;
        }
        return base + POSOFF2ADDR(startPos, tmp & NIBBLE_MASK);
    }

    // We skipped the remainder of the DWORD,
    // so we must set startPos to the highest position of
    // previous DWORD, unless we are already on the first DWORD

    if (startPos < NIBBLES_PER_DWORD)
        return NULL;

    startPos = ((startPos >> LOG2_NIBBLES_PER_DWORD) << LOG2_NIBBLES_PER_DWORD) - 1;

    // Skip "headerless" DWORDS

    while (pMapStart < pMap && 0 == (tmp = VolatileLoadWithoutBarrier<DWORD>(--pMap)))
    {
        startPos -= NIBBLES_PER_DWORD;
    }

    // This helps to catch degenerate error cases. This relies on the fact that
    // startPos cannot ever be bigger than MAX_UINT
    if (((INT_PTR)startPos) < 0)
        return NULL;

    // Find the nibble with the header in the DWORD

    while (startPos && !(tmp & NIBBLE_MASK))
    {
        tmp = tmp >> NIBBLE_SIZE;
        startPos--;
    }

    if (startPos == 0 && tmp == 0)
        return NULL;

    return base + POSOFF2ADDR(startPos, tmp & NIBBLE_MASK);
}

#if !defined(DACCESS_COMPILE)
void EEJitManager::NibbleMapSet(HeapList * pHp, TADDR pCode, BOOL bSet) 
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // Currently all callers to this method ensure EEJitManager::m_CodeHeapCritSec
    // is held.
    _ASSERTE(m_CodeHeapCritSec.OwnedByCurrentThread());

    _ASSERTE(pCode >= pHp->mapBase);

    size_t delta = pCode - pHp->mapBase;

    size_t pos  = ADDR2POS(delta); 
    DWORD value = bSet?ADDR2OFFS(delta):0;

    DWORD index = (DWORD) (pos >> LOG2_NIBBLES_PER_DWORD);
    DWORD mask  = ~((DWORD) HIGHEST_NIBBLE_MASK >> ((pos & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE));

    value = value << POS2SHIFTCOUNT(pos);

    PTR_DWORD pMap = pHp->pHdrMap;

    // assert that we don't overwrite an existing offset
    // (it's a reset or it is empty)
    _ASSERTE(!value || !((*(pMap+index))& ~mask));

    // It is important for this update to be atomic. Synchronization would be required with FindMethodCode otherwise.
    *(pMap+index) = ((*(pMap+index))&mask)|value;

    pHp->cBlocks += (bSet ? 1 : -1);
}
#endif // !DACCESS_COMPILE

#if defined(WIN64EXCEPTIONS)
PTR_RUNTIME_FUNCTION EEJitManager::LazyGetFunctionEntry(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if (!pCodeInfo->IsValid())
    {
        return NULL;
    }

    CodeHeader * pHeader = GetCodeHeader(pCodeInfo->GetMethodToken());

    DWORD address = RUNTIME_FUNCTION__BeginAddress(pHeader->GetUnwindInfo(0)) + pCodeInfo->GetRelOffset();

    // We need the module base address to calculate the end address of a function from the functionEntry.
    // Thus, save it off right now.
    TADDR baseAddress = pCodeInfo->GetModuleBase();

    // NOTE: We could binary search here, if it would be helpful (e.g., large number of funclets)
    for (UINT iUnwindInfo = 0; iUnwindInfo < pHeader->GetNumberOfUnwindInfos(); iUnwindInfo++)
    {
        PTR_RUNTIME_FUNCTION pFunctionEntry = pHeader->GetUnwindInfo(iUnwindInfo);

        if (RUNTIME_FUNCTION__BeginAddress(pFunctionEntry) <= address && address < RUNTIME_FUNCTION__EndAddress(pFunctionEntry, baseAddress))
        {
            return pFunctionEntry;
        }
    }

    return NULL;
}

DWORD EEJitManager::GetFuncletStartOffsets(const METHODTOKEN& MethodToken, DWORD* pStartFuncletOffsets, DWORD dwLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeHeader * pCH = GetCodeHeader(MethodToken);
    TADDR moduleBase = JitTokenToModuleBase(MethodToken);

    _ASSERTE(pCH->GetNumberOfUnwindInfos() >= 1);

    DWORD parentBeginRva = RUNTIME_FUNCTION__BeginAddress(pCH->GetUnwindInfo(0));

    DWORD nFunclets = 0;
    for (COUNT_T iUnwindInfo = 1; iUnwindInfo < pCH->GetNumberOfUnwindInfos(); iUnwindInfo++)
    {
        PTR_RUNTIME_FUNCTION pFunctionEntry = pCH->GetUnwindInfo(iUnwindInfo);

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS)
        if (IsFunctionFragment(moduleBase, pFunctionEntry))
        {
            // This is a fragment (not the funclet beginning); skip it
            continue;
        }
#endif // EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS

        DWORD funcletBeginRva = RUNTIME_FUNCTION__BeginAddress(pFunctionEntry);
        DWORD relParentOffsetToFunclet = funcletBeginRva - parentBeginRva;

        if (nFunclets < dwLength)
            pStartFuncletOffsets[nFunclets] = relParentOffsetToFunclet;
        nFunclets++;
    }

    return nFunclets;
}

#if defined(DACCESS_COMPILE)
// This function is basically like RtlLookupFunctionEntry(), except that it works with DAC 
// to read the function entries out of process.  Also, it can only look up function entries
// inside mscorwks.dll, since DAC doesn't know anything about other unmanaged dll's.
void GetUnmanagedStackWalkInfo(IN  ULONG64   ControlPc,
                               OUT UINT_PTR* pModuleBase,
                               OUT UINT_PTR* pFuncEntry)
{
    WRAPPER_NO_CONTRACT;

    if (pModuleBase)
    {
        *pModuleBase = NULL;
    }

    if (pFuncEntry)
    {
        *pFuncEntry = NULL;
    }

    PEDecoder peDecoder(DacGlobalBase());

    SIZE_T baseAddr = dac_cast<TADDR>(peDecoder.GetBase());
    SIZE_T cbSize   = (SIZE_T)peDecoder.GetVirtualSize();

    // Check if the control PC is inside mscorwks.
    if ( (baseAddr <= ControlPc) && 
         (ControlPc < (baseAddr + cbSize))
       )
    {
        if (pModuleBase)
        {
            *pModuleBase = baseAddr;
        }

        if (pFuncEntry)
        {
            // Check if there is a static function table.
            COUNT_T cbSize = 0;
            TADDR   pExceptionDir = peDecoder.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_EXCEPTION, &cbSize);

            if (pExceptionDir != NULL)
            {
                // Do a binary search on the static function table of mscorwks.dll.
                HRESULT hr = E_FAIL;
                TADDR   taFuncEntry;
                T_RUNTIME_FUNCTION functionEntry;

                DWORD dwLow  = 0;
                DWORD dwHigh = cbSize / sizeof(RUNTIME_FUNCTION);
                DWORD dwMid  = 0;

                while (dwLow <= dwHigh)
                {
                    dwMid = (dwLow + dwHigh) >> 1;
                    taFuncEntry = pExceptionDir + dwMid * sizeof(RUNTIME_FUNCTION);
                    hr = DacReadAll(taFuncEntry, &functionEntry, sizeof(functionEntry), false);
                    if (FAILED(hr))
                    {
                        return;
                    }

                    if (ControlPc < baseAddr + functionEntry.BeginAddress)
                    {
                        dwHigh = dwMid - 1;
                    }
                    else if (ControlPc >= baseAddr + RUNTIME_FUNCTION__EndAddress(&functionEntry, baseAddr))
                    {
                        dwLow = dwMid + 1;
                    }
                    else
                    {
                        _ASSERTE(pFuncEntry);
                        *pFuncEntry = (UINT_PTR)(T_RUNTIME_FUNCTION*)PTR_RUNTIME_FUNCTION(taFuncEntry);
                        break;
                    }
                }

                if (dwLow > dwHigh)
                {
                    _ASSERTE(*pFuncEntry == NULL);
                }
            }
        }
    }
}
#endif // DACCESS_COMPILE

extern "C" void GetRuntimeStackWalkInfo(IN  ULONG64   ControlPc,
                                        OUT UINT_PTR* pModuleBase,
                                        OUT UINT_PTR* pFuncEntry)
{

    WRAPPER_NO_CONTRACT;

    BEGIN_PRESERVE_LAST_ERROR;

    BEGIN_ENTRYPOINT_VOIDRET;

    if (pModuleBase)
        *pModuleBase = NULL;
    if (pFuncEntry)
        *pFuncEntry = NULL;

    EECodeInfo codeInfo((PCODE)ControlPc);
    if (!codeInfo.IsValid())
    {
#if defined(DACCESS_COMPILE)
        GetUnmanagedStackWalkInfo(ControlPc, pModuleBase, pFuncEntry);
#endif // DACCESS_COMPILE
        goto Exit;
    }

    if (pModuleBase)
    {
        *pModuleBase = (UINT_PTR)codeInfo.GetModuleBase();
    }

    if (pFuncEntry)
    {
        *pFuncEntry = (UINT_PTR)(PT_RUNTIME_FUNCTION)codeInfo.GetFunctionEntry();
    }

Exit:
    END_ENTRYPOINT_VOIDRET;

    END_PRESERVE_LAST_ERROR;
}
#endif // WIN64EXCEPTIONS

#ifdef DACCESS_COMPILE

void EEJitManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    IJitManager::EnumMemoryRegions(flags);

    //
    // Save all of the code heaps.
    //

    HeapList* heap;

    for (heap = m_pCodeHeap; heap; heap = heap->GetNext())
    {
        DacEnumHostDPtrMem(heap);

        if (heap->pHeap.IsValid())
        {
            heap->pHeap->EnumMemoryRegions(flags);
        }

        DacEnumMemoryRegion(heap->startAddress, (ULONG32)
                            (heap->endAddress - heap->startAddress));

        if (heap->pHdrMap.IsValid())
        {
            ULONG32 nibbleMapSize = (ULONG32)
                HEAP2MAPSIZE(ROUND_UP_TO_PAGE(heap->maxCodeHeapSize));
            DacEnumMemoryRegion(dac_cast<TADDR>(heap->pHdrMap), nibbleMapSize);
        }
    }
}
#endif // #ifdef DACCESS_COMPILE

#endif // CROSSGEN_COMPILE


#ifndef DACCESS_COMPILE

//*******************************************************
// Execution Manager
//*******************************************************

// Init statics
void ExecutionManager::Init()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_JumpStubCrst.Init(CrstJumpStubCache, CrstFlags(CRST_UNSAFE_ANYMODE|CRST_DEBUGGER_THREAD));

    m_RangeCrst.Init(CrstExecuteManRangeLock, CRST_UNSAFE_ANYMODE);

    m_pDefaultCodeMan = new EECodeManager();

#ifndef CROSSGEN_COMPILE
    m_pEEJitManager = new EEJitManager();
#endif
#ifdef FEATURE_PREJIT
    m_pNativeImageJitManager = new NativeImageJitManager();
#endif

#ifdef FEATURE_READYTORUN
    m_pReadyToRunJitManager = new ReadyToRunJitManager();
#endif
}

#endif // #ifndef DACCESS_COMPILE

//**************************************************************************
RangeSection * 
ExecutionManager::FindCodeRange(PCODE currentPC, ScanFlag scanFlag)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if (currentPC == NULL)
        return NULL;

    if (scanFlag == ScanReaderLock)
        return FindCodeRangeWithLock(currentPC);

    return GetRangeSection(currentPC);
}

//**************************************************************************
NOINLINE // Make sure that the slow path with lock won't affect the fast path
RangeSection *
ExecutionManager::FindCodeRangeWithLock(PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    ReaderLockHolder rlh;
    return GetRangeSection(currentPC);
}

//**************************************************************************
MethodDesc * ExecutionManager::GetCodeMethodDesc(PCODE currentPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
    }
    CONTRACTL_END

    EECodeInfo codeInfo(currentPC);
    if (!codeInfo.IsValid())
        return NULL;
    return codeInfo.GetMethodDesc();
}

//**************************************************************************
BOOL ExecutionManager::IsManagedCode(PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    if (currentPC == NULL)
        return FALSE;

    if (GetScanFlags() == ScanReaderLock) 
        return IsManagedCodeWithLock(currentPC);

    return IsManagedCodeWorker(currentPC);
}

//**************************************************************************
NOINLINE // Make sure that the slow path with lock won't affect the fast path
BOOL ExecutionManager::IsManagedCodeWithLock(PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    ReaderLockHolder rlh;
    return IsManagedCodeWorker(currentPC);
}

//**************************************************************************
BOOL ExecutionManager::IsManagedCode(PCODE currentPC, HostCallPreference hostCallPreference /*=AllowHostCalls*/, BOOL *pfFailedReaderLock /*=NULL*/)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

#ifdef DACCESS_COMPILE
    return IsManagedCode(currentPC);
#else
    if (hostCallPreference == AllowHostCalls)
    {
        return IsManagedCode(currentPC);
    }

    ReaderLockHolder rlh(hostCallPreference);
    if (!rlh.Acquired())
    {
        _ASSERTE(pfFailedReaderLock != NULL);
        *pfFailedReaderLock = TRUE;
        return FALSE;
    }

    return IsManagedCodeWorker(currentPC);
#endif
}

//**************************************************************************
// Assumes that the ExecutionManager reader/writer lock is taken or that 
// it is safe not to take it.
BOOL ExecutionManager::IsManagedCodeWorker(PCODE currentPC)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    // This may get called for arbitrary code addresses. Note that the lock is
    // taken over the call to JitCodeToMethodInfo too so that nobody pulls out 
    // the range section from underneath us.

    RangeSection * pRS = GetRangeSection(currentPC);
    if (pRS == NULL)
        return FALSE;

    if (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP)
    {
#ifndef CROSSGEN_COMPILE
        // Typically if we find a Jit Manager we are inside a managed method
        // but on we could also be in a stub, so we check for that 
        // as well and we don't consider stub to be real managed code.
        TADDR start = dac_cast<PTR_EEJitManager>(pRS->pjit)->FindMethodCode(pRS, currentPC);
        if (start == NULL)
            return FALSE;
        CodeHeader * pCHdr = PTR_CodeHeader(start - sizeof(CodeHeader));
        if (!pCHdr->IsStubCodeBlock())
            return TRUE;
#endif
    }
#ifdef FEATURE_READYTORUN
    else
    if (pRS->flags & RangeSection::RANGE_SECTION_READYTORUN)
    {
        if (dac_cast<PTR_ReadyToRunJitManager>(pRS->pjit)->JitCodeToMethodInfo(pRS, currentPC, NULL, NULL))
            return TRUE;
    }
#endif
    else
    {
#ifdef FEATURE_PREJIT
        // Check that we are in the range with true managed code. We don't
        // consider jump stubs or precodes to be real managed code.

        Module * pModule = dac_cast<PTR_Module>(pRS->pHeapListOrZapModule);

        NGenLayoutInfo * pLayoutInfo = pModule->GetNGenLayoutInfo();

        if (pLayoutInfo->m_CodeSections[0].IsInRange(currentPC) ||
            pLayoutInfo->m_CodeSections[1].IsInRange(currentPC) ||
            pLayoutInfo->m_CodeSections[2].IsInRange(currentPC))
        {
            return TRUE;
        }
#endif
    }

    return FALSE;
}

#ifndef DACCESS_COMPILE

//**************************************************************************
// Clear the caches for all JITs loaded.
//
void ExecutionManager::ClearCaches( void )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    GetEEJitManager()->ClearCache();
}

//**************************************************************************
// Check if caches for any JITs loaded need to be cleaned
//
BOOL ExecutionManager::IsCacheCleanupRequired( void )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    return GetEEJitManager()->IsCacheCleanupRequired();
}

#ifndef FEATURE_MERGE_JIT_AND_ENGINE
/*********************************************************************/
// This static method returns the name of the jit dll
//
LPWSTR ExecutionManager::GetJitName()
{
    STANDARD_VM_CONTRACT;

    LPWSTR  pwzJitName;

    // Try to obtain a name for the jit library from the env. variable
    IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_JitName, &pwzJitName));

    if (NULL == pwzJitName)
    {
        pwzJitName = MAKEDLLNAME_W(W("clrjit"));
    }

    return pwzJitName;
}
#endif // FEATURE_MERGE_JIT_AND_ENGINE

#endif // #ifndef DACCESS_COMPILE

RangeSection* ExecutionManager::GetRangeSection(TADDR addr)
{
    CONTRACTL {
        NOTHROW;
        HOST_NOCALLS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    RangeSection * pHead = m_CodeRangeList;

    if (pHead == NULL)
    {
        return NULL;
    }

    RangeSection *pCurr = pHead;
    RangeSection *pLast = NULL;

#ifndef DACCESS_COMPILE
    RangeSection *pLastUsedRS = (pCurr != NULL) ? pCurr->pLastUsed : NULL;

    if (pLastUsedRS != NULL)
    {
        // positive case
        if ((addr >= pLastUsedRS->LowAddress) &&
            (addr <  pLastUsedRS->HighAddress)   )
        {
            return pLastUsedRS;
        }

        RangeSection * pNextAfterLastUsedRS = pLastUsedRS->pnext;

        // negative case
        if ((addr <  pLastUsedRS->LowAddress) &&
            (pNextAfterLastUsedRS == NULL || addr >= pNextAfterLastUsedRS->HighAddress))
        {
            return NULL;
        }
    }
#endif

    while (pCurr != NULL)
    {
        // See if addr is in [pCurr->LowAddress .. pCurr->HighAddress)
        if (pCurr->LowAddress <= addr)
        {
            // Since we are sorted, once pCurr->HighAddress is less than addr
            // then all subsequence ones will also be lower, so we are done.
            if (addr >= pCurr->HighAddress)
            {
                // we'll return NULL and put pLast into pLastUsed
                pCurr = NULL;
            }
            else
            {
                // addr must be in [pCurr->LowAddress .. pCurr->HighAddress)
                _ASSERTE((pCurr->LowAddress <= addr) && (addr < pCurr->HighAddress));

                // Found the matching RangeSection
                // we'll return pCurr and put it into pLastUsed
                pLast = pCurr;
            }

            break;
        }
        pLast = pCurr;
        pCurr = pCurr->pnext;
    }

#ifndef DACCESS_COMPILE
    // Cache pCurr as pLastUsed in the head node
    // Unless we are on an MP system with many cpus
    // where this sort of caching actually diminishes scaling during server GC
    // due to many processors writing to a common location
    if (g_SystemInfo.dwNumberOfProcessors < 4 || !GCHeap::IsServerHeap() || !GCHeap::IsGCInProgress())
        pHead->pLastUsed = pLast;
#endif

    return pCurr;
}

RangeSection* ExecutionManager::GetRangeSectionAndPrev(RangeSection *pHead, TADDR addr, RangeSection** ppPrev)
{
    WRAPPER_NO_CONTRACT;

    RangeSection *pCurr;
    RangeSection *pPrev;
    RangeSection *result = NULL;

    for (pPrev = NULL,  pCurr = pHead; 
         pCurr != NULL; 
         pPrev = pCurr, pCurr = pCurr->pnext)
    {
        // See if addr is in [pCurr->LowAddress .. pCurr->HighAddress)
        if (pCurr->LowAddress > addr)
            continue;

        if (addr >= pCurr->HighAddress)
            break;
        
        // addr must be in [pCurr->LowAddress .. pCurr->HighAddress)
        _ASSERTE((pCurr->LowAddress <= addr) && (addr < pCurr->HighAddress));
    
        // Found the matching RangeSection
        result = pCurr;

        // Write back pPrev to ppPrev if it is non-null
        if (ppPrev != NULL)
            *ppPrev = pPrev;

        break;
    }
    
    // If we failed to find a match write NULL to ppPrev if it is non-null
    if ((ppPrev != NULL) && (result == NULL))
    {
        *ppPrev = NULL;
    }

    return result;
}

/* static */
PTR_Module ExecutionManager::FindZapModule(TADDR currentData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
        STATIC_CONTRACT_HOST_CALLS;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    ReaderLockHolder rlh;

    RangeSection * pRS = GetRangeSection(currentData);
    if (pRS == NULL)
        return NULL;

    if (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP)
        return NULL;

#ifdef FEATURE_READYTORUN
    if (pRS->flags & RangeSection::RANGE_SECTION_READYTORUN)
        return NULL;
#endif

    return dac_cast<PTR_Module>(pRS->pHeapListOrZapModule);
}

/* static */
PTR_Module ExecutionManager::FindModuleForGCRefMap(TADDR currentData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    RangeSection * pRS = FindCodeRange(currentData, ExecutionManager::GetScanFlags());
    if (pRS == NULL)
        return NULL;

    if (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP)
        return NULL;

#ifdef FEATURE_READYTORUN
    // RANGE_SECTION_READYTORUN is intentionally not filtered out here
#endif

    return dac_cast<PTR_Module>(pRS->pHeapListOrZapModule);
}

#ifndef DACCESS_COMPILE

/* NGenMem depends on this entrypoint */
NOINLINE 
void ExecutionManager::AddCodeRange(TADDR          pStartRange,
                                    TADDR          pEndRange,
                                    IJitManager *  pJit,
                                    RangeSection::RangeSectionFlags flags,
                                    void *         pHp)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pJit));
        PRECONDITION(CheckPointer(pHp));
    } CONTRACTL_END;

    AddRangeHelper(pStartRange,
                   pEndRange,
                   pJit,
                   flags,
                   dac_cast<TADDR>(pHp));
}

#ifdef FEATURE_PREJIT

void ExecutionManager::AddNativeImageRange(TADDR StartRange, 
                                           SIZE_T Size, 
                                           Module * pModule)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
    } CONTRACTL_END;

    AddRangeHelper(StartRange,
                   StartRange + Size,
                   GetNativeImageJitManager(),
                   RangeSection::RANGE_SECTION_NONE,
                   dac_cast<TADDR>(pModule));
}
#endif

void ExecutionManager::AddRangeHelper(TADDR          pStartRange,
                                      TADDR          pEndRange,
                                      IJitManager *  pJit,
                                      RangeSection::RangeSectionFlags flags,
                                      TADDR          pHeapListOrZapModule)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        HOST_CALLS;
        PRECONDITION(pStartRange < pEndRange);
        PRECONDITION(pHeapListOrZapModule != NULL);
    } CONTRACTL_END;

    RangeSection *pnewrange = new RangeSection;

    _ASSERTE(pEndRange > pStartRange);

    pnewrange->LowAddress  = pStartRange;
    pnewrange->HighAddress = pEndRange;
    pnewrange->pjit        = pJit;
    pnewrange->pnext       = NULL;
    pnewrange->flags       = flags;
    pnewrange->pLastUsed   = NULL;
    pnewrange->pHeapListOrZapModule = pHeapListOrZapModule;
#if defined(_TARGET_AMD64_)
    pnewrange->pUnwindInfoTable = NULL;
#endif // defined(_TARGET_AMD64_)
    {
        CrstHolder ch(&m_RangeCrst); // Acquire the Crst before linking in a new RangeList

        RangeSection * current  = m_CodeRangeList;
        RangeSection * previous = NULL;

        if (current != NULL)
        {
            while (true)
            {
                // Sort addresses top down so that more recently created ranges
                // will populate the top of the list
                if (pnewrange->LowAddress > current->LowAddress)
                {
                    // Asserts if ranges are overlapping
                    _ASSERTE(pnewrange->LowAddress >= current->HighAddress);
                    pnewrange->pnext = current;

                    if (previous == NULL) // insert new head
                    {
                        m_CodeRangeList = pnewrange;
                    }
                    else
                    { // insert in the middle
                        previous->pnext  = pnewrange;
                    }
                    break;
                }

                RangeSection * next = current->pnext;
                if (next == NULL) // insert at end of list
                {
                    current->pnext = pnewrange;
                    break;
                }

                // Continue walking the RangeSection list
                previous = current;
                current  = next;
            }
        }
        else
        {
            m_CodeRangeList = pnewrange;
        }
    }
}

// Deletes a single range starting at pStartRange
void ExecutionManager::DeleteRange(TADDR pStartRange)
{
    CONTRACTL {
        NOTHROW; // If this becomes throwing, then revisit the queuing of deletes below.
        GC_NOTRIGGER;
    } CONTRACTL_END;

    RangeSection *pCurr = NULL;
    {
        // Acquire the Crst before unlinking a RangeList.
        // NOTE: The Crst must be acquired BEFORE we grab the writer lock, as the
        // writer lock forces us into a forbid suspend thread region, and it's illegal
        // to enter a Crst after the forbid suspend thread region is entered
        CrstHolder ch(&m_RangeCrst);

        // Acquire the WriterLock and prevent any readers from walking the RangeList.
        // This also forces us to enter a forbid suspend thread region, to prevent
        // hijacking profilers from grabbing this thread and walking it (the walk may
        // require the reader lock, which would cause a deadlock).
        WriterLockHolder wlh;

        RangeSection *pPrev = NULL;

        pCurr = GetRangeSectionAndPrev(m_CodeRangeList, pStartRange, &pPrev);

        // pCurr points at the Range that needs to be unlinked from the RangeList
        if (pCurr != NULL)
        {

            // If pPrev is NULL the the head of this list is to be deleted
            if (pPrev == NULL)
            {
                m_CodeRangeList = pCurr->pnext;          
            }
            else
            {
                _ASSERT(pPrev->pnext == pCurr);

                pPrev->pnext = pCurr->pnext;
            }

            // Clear the cache pLastUsed in the head node (if any)
            RangeSection * head = m_CodeRangeList;
            if (head != NULL)
            {
                head->pLastUsed = NULL;
            }

            //
            // Cannot delete pCurr here because we own the WriterLock and if this is
            // a hosted scenario then the hosting api callback cannot occur in a forbid
            // suspend region, which the writer lock is.
            //
        }
    }

    //
    // Now delete the node
    //
    if (pCurr != NULL)
    {
#if defined(_TARGET_AMD64_)
        if (pCurr->pUnwindInfoTable != 0)
            delete pCurr->pUnwindInfoTable;
#endif // defined(_TARGET_AMD64_)
        delete pCurr;
    }
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void ExecutionManager::EnumRangeList(RangeSection* list,
                                     CLRDataEnumMemoryFlags flags)
{
    while (list != NULL) 
    {
        // If we can't read the target memory, stop immediately so we don't work
        // with broken data.
        if (!DacEnumMemoryRegion(dac_cast<TADDR>(list), sizeof(*list)))
            break;

        if (list->pjit.IsValid())
        {
            list->pjit->EnumMemoryRegions(flags);
        }

        if (!(list->flags & RangeSection::RANGE_SECTION_CODEHEAP))
        {
            PTR_Module pModule = dac_cast<PTR_Module>(list->pHeapListOrZapModule);

            if (pModule.IsValid())
            {
                pModule->EnumMemoryRegions(flags, true);
            }
        }

        list = list->pnext;
#if defined (_DEBUG)
        // Test hook: when testing on debug builds, we want an easy way to test that the while
        // correctly terminates in the face of ridiculous stuff from the target.
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget) == 1)
        {
            // Force us to struggle on with something bad.
            if (list == NULL)
            {
                list = (RangeSection *)&flags;
            }
        }
#endif // (_DEBUG)

    }
}

void ExecutionManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    STATIC_CONTRACT_HOST_CALLS;

    ReaderLockHolder rlh;

    //
    // Report the global data portions.
    //

    m_CodeRangeList.EnumMem();
    m_pDefaultCodeMan.EnumMem();

    //
    // Walk structures and report.
    //

    if (m_CodeRangeList.IsValid())
    {
        EnumRangeList(m_CodeRangeList, flags);
    }
}
#endif // #ifdef DACCESS_COMPILE

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

void ExecutionManager::Unload(LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // a size of 0 is a signal to Nirvana to flush the entire cache 
    FlushInstructionCache(GetCurrentProcess(),0,0);

    /* StackwalkCacheEntry::EIP is an address into code. Since we are
    unloading the code, we need to invalidate the cache. Otherwise,
    its possible that another appdomain might generate code at the very
    same address, and we might incorrectly think that the old
    StackwalkCacheEntry corresponds to it. So flush the cache.
    */
    StackwalkCache::Invalidate(pLoaderAllocator);

    JumpStubCache * pJumpStubCache = (JumpStubCache *)pLoaderAllocator->m_pJumpStubCache;
    if (pJumpStubCache != NULL)
    {
        delete pJumpStubCache;
        pLoaderAllocator->m_pJumpStubCache = NULL;
    }

    GetEEJitManager()->Unload(pLoaderAllocator);
}

PCODE ExecutionManager::jumpStub(MethodDesc* pMD, PCODE target,
                                 BYTE * loAddr,   BYTE * hiAddr,
                                 LoaderAllocator *pLoaderAllocator)
{
    CONTRACT(PCODE) {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pLoaderAllocator != NULL || pMD != NULL);
        PRECONDITION(loAddr < hiAddr);
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    PCODE jumpStub = NULL;

    if (pLoaderAllocator == NULL)
        pLoaderAllocator = pMD->GetLoaderAllocatorForCode();
    _ASSERTE(pLoaderAllocator != NULL);

    CrstHolder ch(&m_JumpStubCrst);

    JumpStubCache * pJumpStubCache = (JumpStubCache *)pLoaderAllocator->m_pJumpStubCache;
    if (pJumpStubCache == NULL)
    {
        pJumpStubCache = new JumpStubCache();
        pLoaderAllocator->m_pJumpStubCache = pJumpStubCache;
    }

    for (JumpStubTable::KeyIterator i = pJumpStubCache->m_Table.Begin(target), 
        end = pJumpStubCache->m_Table.End(target); i != end; i++)
    {
        jumpStub = i->m_jumpStub;

        _ASSERTE(jumpStub != NULL);

        // Is the matching entry with the requested range?
        if (((TADDR)loAddr <= jumpStub) && (jumpStub <= (TADDR)hiAddr))
        {
            RETURN(jumpStub);
        }
    }

    // If we get here we need to create a new jump stub
    // add or change the jump stub table to point at the new one
    jumpStub = getNextJumpStub(pMD, target, loAddr, hiAddr, pLoaderAllocator);    // this statement can throw

    _ASSERTE(((TADDR)loAddr <= jumpStub) && (jumpStub <= (TADDR)hiAddr));

    LOG((LF_JIT, LL_INFO10000, "Add JumpStub to" FMT_ADDR "at" FMT_ADDR "\n",
            DBG_ADDR(target), DBG_ADDR(jumpStub) ));

    RETURN(jumpStub);
}

PCODE ExecutionManager::getNextJumpStub(MethodDesc* pMD, PCODE target,
                                        BYTE * loAddr, BYTE * hiAddr, LoaderAllocator *pLoaderAllocator)
{
    CONTRACT(PCODE) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(pLoaderAllocator != NULL);
        PRECONDITION(m_JumpStubCrst.OwnedByCurrentThread());
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    BYTE *                 jumpStub = NULL;
    bool                   isLCG    = pMD && pMD->IsLCGMethod();
    JumpStubBlockHeader ** ppHead   = isLCG ? &(pMD->AsDynamicMethodDesc()->GetLCGMethodResolver()->m_jumpStubBlock) : &(((JumpStubCache *)(pLoaderAllocator->m_pJumpStubCache))->m_pBlocks);
    JumpStubBlockHeader *  curBlock = *ppHead;
    
    while (curBlock)
    {
        _ASSERTE(pLoaderAllocator == (isLCG ? curBlock->GetHostCodeHeap()->GetAllocator() : curBlock->GetLoaderAllocator()));

        if (curBlock->m_used < curBlock->m_allocated)
        {
            jumpStub = (BYTE *) curBlock + sizeof(JumpStubBlockHeader) + ((size_t) curBlock->m_used * BACK_TO_BACK_JUMP_ALLOCATE_SIZE);

            if ((loAddr <= jumpStub) && (jumpStub <= hiAddr))
            {
                // We will update curBlock->m_used at "DONE"
                goto DONE;
            }
        }

        curBlock = curBlock->m_next;
    }

    // If we get here then we need to allocate a new JumpStubBlock

    // allocJumpStubBlock will allocate from the LoaderCodeHeap for normal methods and HostCodeHeap for LCG methods
    // this can throw an OM exception
    curBlock = ExecutionManager::GetEEJitManager()->allocJumpStubBlock(pMD, DEFAULT_JUMPSTUBS_PER_BLOCK, loAddr, hiAddr, pLoaderAllocator);

    jumpStub = (BYTE *) curBlock + sizeof(JumpStubBlockHeader) + ((size_t) curBlock->m_used * BACK_TO_BACK_JUMP_ALLOCATE_SIZE);

    _ASSERTE((loAddr <= jumpStub) && (jumpStub <= hiAddr));

    curBlock->m_next = *ppHead;
    *ppHead = curBlock;

DONE:

    _ASSERTE((curBlock->m_used < curBlock->m_allocated));

#ifdef _TARGET_ARM64_
    // 8-byte alignment is required on ARM64
    _ASSERTE(((UINT_PTR)jumpStub & 7) == 0);
#endif

    emitBackToBackJump(jumpStub, (void*) target);

    if (isLCG)
    {
        // always get a new jump stub for LCG method
        // We don't share jump stubs among different LCG methods so that the jump stubs used
        // by every LCG method can be cleaned up individually
        // There is not much benefit to share jump stubs within one LCG method anyway.
    }
    else
    {
        JumpStubCache * pJumpStubCache = (JumpStubCache *)pLoaderAllocator->m_pJumpStubCache;
        _ASSERTE(pJumpStubCache != NULL);

        JumpStubEntry entry;

        entry.m_target = target;
        entry.m_jumpStub = (PCODE)jumpStub;

        pJumpStubCache->m_Table.Add(entry);
    }

    curBlock->m_used++;

    RETURN((PCODE)jumpStub);
}
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

#ifdef FEATURE_PREJIT
//***************************************************************************************
//***************************************************************************************

#ifndef DACCESS_COMPILE

NativeImageJitManager::NativeImageJitManager()
{
    WRAPPER_NO_CONTRACT;
}

#endif // #ifndef DACCESS_COMPILE

PTR_VOID NativeImageJitManager::GetGCInfo(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    PTR_RUNTIME_FUNCTION pRuntimeFunction = dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader);
    TADDR baseAddress = JitTokenToModuleBase(MethodToken);

#ifndef DACCESS_COMPILE
    if (g_IBCLogger.InstrEnabled())
    {
        PTR_NGenLayoutInfo pNgenLayout = JitTokenToZapModule(MethodToken)->GetNGenLayoutInfo();
        PTR_MethodDesc pMD = NativeUnwindInfoLookupTable::GetMethodDesc(pNgenLayout, pRuntimeFunction, baseAddress);
        g_IBCLogger.LogMethodGCInfoAccess(pMD);
    }
#endif

    SIZE_T nUnwindDataSize;
    PTR_VOID pUnwindData = GetUnwindDataBlob(baseAddress, pRuntimeFunction, &nUnwindDataSize);

    // GCInfo immediatelly follows unwind data
    return dac_cast<PTR_BYTE>(pUnwindData) + nUnwindDataSize;
}

unsigned NativeImageJitManager::InitializeEHEnumeration(const METHODTOKEN& MethodToken, EH_CLAUSE_ENUMERATOR* pEnumState)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    NGenLayoutInfo * pNgenLayout = JitTokenToZapModule(MethodToken)->GetNGenLayoutInfo();

    //early out if the method doesn't have EH info bit set.
    if (!NativeUnwindInfoLookupTable::HasExceptionInfo(pNgenLayout, PTR_RUNTIME_FUNCTION(MethodToken.m_pCodeHeader)))
        return 0;

    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE pExceptionLookupTable = dac_cast<PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE>(pNgenLayout->m_ExceptionInfoLookupTable.StartAddress());
    _ASSERTE(pExceptionLookupTable != NULL);

    SIZE_T size = pNgenLayout->m_ExceptionInfoLookupTable.Size();
    COUNT_T numLookupTableEntries = (COUNT_T)(size / sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
    // at least 2 entries (1 valid entry + 1 sentinal entry)
    _ASSERTE(numLookupTableEntries >= 2);
    
    DWORD methodStartRVA = (DWORD)(JitTokenToStartAddress(MethodToken) - JitTokenToModuleBase(MethodToken));

    COUNT_T ehInfoSize = 0;
    DWORD exceptionInfoRVA = NativeExceptionInfoLookupTable::LookupExceptionInfoRVAForMethod(pExceptionLookupTable,
                                                                  numLookupTableEntries,
                                                                  methodStartRVA, 
                                                                  &ehInfoSize);
    if (exceptionInfoRVA == 0)
        return 0;

    pEnumState->iCurrentPos = 0;
    pEnumState->pExceptionClauseArray = JitTokenToModuleBase(MethodToken) + exceptionInfoRVA;

    return ehInfoSize / sizeof(CORCOMPILE_EXCEPTION_CLAUSE);
}

PTR_EXCEPTION_CLAUSE_TOKEN NativeImageJitManager::GetNextEHClause(EH_CLAUSE_ENUMERATOR* pEnumState,
                              EE_ILEXCEPTION_CLAUSE* pEHClauseOut)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    unsigned iCurrentPos = pEnumState->iCurrentPos;
    pEnumState->iCurrentPos++;

    CORCOMPILE_EXCEPTION_CLAUSE* pClause = &(dac_cast<PTR_CORCOMPILE_EXCEPTION_CLAUSE>(pEnumState->pExceptionClauseArray)[iCurrentPos]);

    // copy to the input parmeter, this is a nice abstraction for the future
    // if we want to compress the Clause encoding, we can do without affecting the call sites
    pEHClauseOut->TryStartPC = pClause->TryStartPC; 
    pEHClauseOut->TryEndPC = pClause->TryEndPC; 
    pEHClauseOut->HandlerStartPC = pClause->HandlerStartPC; 
    pEHClauseOut->HandlerEndPC = pClause->HandlerEndPC;
    pEHClauseOut->Flags = pClause->Flags;
    pEHClauseOut->FilterOffset = pClause->FilterOffset;

    return dac_cast<PTR_EXCEPTION_CLAUSE_TOKEN>(pClause);
}

#ifndef DACCESS_COMPILE

TypeHandle NativeImageJitManager::ResolveEHClause(EE_ILEXCEPTION_CLAUSE* pEHClause,
                                              CrawlFrame* pCf)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    _ASSERTE(NULL != pCf);
    _ASSERTE(NULL != pEHClause);
    _ASSERTE(IsTypedHandler(pEHClause));

    MethodDesc *pMD = PTR_MethodDesc(pCf->GetFunction());

    _ASSERTE(pMD != NULL);

    Module* pModule = pMD->GetModule();
    PREFIX_ASSUME(pModule != NULL);

    SigTypeContext typeContext(pMD);
    VarKind k = hasNoVars;    
            
    mdToken typeTok = pEHClause->ClassToken;

    // In the vast majority of cases the code under the "if" below
    // will not be executed.
    //
    // First grab the representative instantiations.  For code
    // shared by multiple generic instantiations these are the
    // canonical (representative) instantiation.
    if (TypeFromToken(typeTok) == mdtTypeSpec)
    {
        PCCOR_SIGNATURE pSig;
        ULONG cSig;
        IfFailThrow(pModule->GetMDImport()->GetTypeSpecFromToken(typeTok, &pSig, &cSig));
        
        SigPointer psig(pSig, cSig);
        k = psig.IsPolyType(&typeContext);

        // Grab the active class and method instantiation.  This exact instantiation is only
        // needed in the corner case of "generic" exception catching in shared
        // generic code.  We don't need the exact instantiation if the token
        // doesn't contain E_T_VAR or E_T_MVAR.        
        if ((k & hasSharableVarsMask) != 0)
        {
            Instantiation classInst;
            Instantiation methodInst;
            pCf->GetExactGenericInstantiations(&classInst,&methodInst);
            SigTypeContext::InitTypeContext(pMD,classInst, methodInst,&typeContext);
        }
    }

    return ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, typeTok, &typeContext, 
                                                          ClassLoader::ReturnNullIfNotFound);
}

#endif // #ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Ngen info manager
//-----------------------------------------------------------------------------
BOOL NativeImageJitManager::GetBoundariesAndVars(
        const DebugInfoRequest & request,
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        OUT ULONG32 * pcMap, 
        OUT ICorDebugInfo::OffsetMapping **ppMap,
        OUT ULONG32 * pcVars, 
        OUT ICorDebugInfo::NativeVarInfo **ppVars)
{
    CONTRACTL {
        THROWS;       // on OOM.
        GC_NOTRIGGER; // getting vars shouldn't trigger
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // We want the module that the code is instantiated in, not necessarily the one
    // that it was declared in. This only matters for ngen-generics.        
    MethodDesc * pMD = request.GetMD();
    Module * pModule = pMD->GetZapModule();
    PREFIX_ASSUME(pModule != NULL);

    PTR_BYTE pDebugInfo = pModule->GetNativeDebugInfo(pMD);

    // No header created, which means no jit information is available.
    if (pDebugInfo == NULL)
        return FALSE;

    // Uncompress. This allocates memory and may throw.
    CompressDebugInfo::RestoreBoundariesAndVars(
        fpNew, pNewData, // allocators
        pDebugInfo,      // input
        pcMap, ppMap,
        pcVars, ppVars); // output

    return TRUE;
}

#ifdef DACCESS_COMPILE
//
// Need to write out debug info
//
void NativeImageJitManager::EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, MethodDesc * pMD)
{
    SUPPORTS_DAC;

    Module * pModule = pMD->GetZapModule();
    PREFIX_ASSUME(pModule != NULL);
    PTR_BYTE pDebugInfo = pModule->GetNativeDebugInfo(pMD);

    if (pDebugInfo != NULL)
    {
        CompressDebugInfo::EnumMemoryRegions(flags, pDebugInfo);
    }
}
#endif

PCODE NativeImageJitManager::GetCodeAddressForRelOffset(const METHODTOKEN& MethodToken, DWORD relOffset)
{
    WRAPPER_NO_CONTRACT;

    MethodRegionInfo methodRegionInfo;
    JitTokenToMethodRegionInfo(MethodToken, &methodRegionInfo);

    if (relOffset < methodRegionInfo.hotSize)
        return methodRegionInfo.hotStartAddress + relOffset;

    SIZE_T coldOffset = relOffset - methodRegionInfo.hotSize;
    _ASSERTE(coldOffset < methodRegionInfo.coldSize);
    return methodRegionInfo.coldStartAddress + coldOffset;
}

BOOL NativeImageJitManager::JitCodeToMethodInfo(RangeSection * pRangeSection,
                                            PCODE        currentPC,
                                            MethodDesc** ppMethodDesc,
                                            EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    TADDR currentInstr = PCODEToPINSTR(currentPC);

    Module * pModule = dac_cast<PTR_Module>(pRangeSection->pHeapListOrZapModule);

    NGenLayoutInfo * pLayoutInfo = pModule->GetNGenLayoutInfo();
    DWORD iRange = 0;

    if (pLayoutInfo->m_CodeSections[0].IsInRange(currentInstr))
    {
        iRange = 0;
    }
    else
    if (pLayoutInfo->m_CodeSections[1].IsInRange(currentInstr))
    {
        iRange = 1;
    }
    else
    if (pLayoutInfo->m_CodeSections[2].IsInRange(currentInstr))
    {
        iRange = 2;
    }
    else
    {
        return FALSE;
    }

    TADDR ImageBase = pRangeSection->LowAddress;

    DWORD RelativePc = (DWORD)(currentInstr - ImageBase);

    PTR_RUNTIME_FUNCTION FunctionEntry;

    if (iRange == 2)
    {
        int ColdMethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(RelativePc,
                                                                               pLayoutInfo->m_pRuntimeFunctions[2],
                                                                               0,
                                                                               pLayoutInfo->m_nRuntimeFunctions[2] - 1);

        if (ColdMethodIndex < 0)
            return FALSE;

#ifdef WIN64EXCEPTIONS
        // Save the raw entry
        int RawColdMethodIndex = ColdMethodIndex;

        PTR_CORCOMPILE_COLD_METHOD_ENTRY pColdCodeMap = pLayoutInfo->m_ColdCodeMap;

        while (pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA == 0)
            ColdMethodIndex--;

        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(ImageBase + pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA);
#else
        DWORD ColdUnwindData = pLayoutInfo->m_pRuntimeFunctions[2][ColdMethodIndex].UnwindData;
        _ASSERTE((ColdUnwindData & RUNTIME_FUNCTION_INDIRECT) != 0);
        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(ImageBase + (ColdUnwindData & ~RUNTIME_FUNCTION_INDIRECT));
#endif

        if (ppMethodDesc)
        {
            DWORD methodDescRVA;

            COUNT_T iIndex = (COUNT_T)(FunctionEntry - pLayoutInfo->m_pRuntimeFunctions[0]);
            if (iIndex >= pLayoutInfo->m_nRuntimeFunctions[0])
            {
                iIndex = (COUNT_T)(FunctionEntry - pLayoutInfo->m_pRuntimeFunctions[1]);
                _ASSERTE(iIndex < pLayoutInfo->m_nRuntimeFunctions[1]);
                methodDescRVA = pLayoutInfo->m_MethodDescs[1][iIndex];
            }
            else
            {
                methodDescRVA = pLayoutInfo->m_MethodDescs[0][iIndex];
            }
            _ASSERTE(methodDescRVA != NULL);

            // Note that the MethodDesc does not have to be restored. (It happens when we are called
            // from SetupGcCoverageForNativeMethod.)
            *ppMethodDesc = PTR_MethodDesc((methodDescRVA & ~HAS_EXCEPTION_INFO_MASK) + ImageBase);
        }

        if (pCodeInfo)
        {
            PTR_RUNTIME_FUNCTION ColdFunctionTable = pLayoutInfo->m_pRuntimeFunctions[2];

            PTR_RUNTIME_FUNCTION ColdFunctionEntry =  ColdFunctionTable + ColdMethodIndex;
            DWORD coldCodeOffset = (DWORD)(RelativePc - RUNTIME_FUNCTION__BeginAddress(ColdFunctionEntry));
            pCodeInfo->m_relOffset = pLayoutInfo->m_ColdCodeMap[ColdMethodIndex].hotCodeSize + coldCodeOffset;

            // We are using RUNTIME_FUNCTION as METHODTOKEN
            pCodeInfo->m_methodToken = METHODTOKEN(pRangeSection, dac_cast<TADDR>(FunctionEntry));

#ifdef WIN64EXCEPTIONS
            PTR_RUNTIME_FUNCTION RawColdFunctionEntry = ColdFunctionTable + RawColdMethodIndex;
#ifdef _TARGET_AMD64_
            if ((RawColdFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) != 0)
            {
                RawColdFunctionEntry = PTR_RUNTIME_FUNCTION(ImageBase + (RawColdFunctionEntry->UnwindData & ~RUNTIME_FUNCTION_INDIRECT));
            }
#endif // _TARGET_AMD64_
            pCodeInfo->m_pFunctionEntry = RawColdFunctionEntry;
#endif
        }
    }
    else
    {
        PTR_DWORD pRuntimeFunctionLookupTable = dac_cast<PTR_DWORD>(pLayoutInfo->m_UnwindInfoLookupTable[iRange]);

        _ASSERTE(pRuntimeFunctionLookupTable != NULL);

        DWORD RelativeToCodeStart = (DWORD)(currentInstr - dac_cast<TADDR>(pLayoutInfo->m_CodeSections[iRange].StartAddress()));
        COUNT_T iStrideIndex = RelativeToCodeStart / RUNTIME_FUNCTION_LOOKUP_STRIDE;

        // The lookup table may not be big enough to cover the entire code range if there was padding inserted during NGen image layout.
        // The last entry is lookup table entry covers the rest of the code range in this case.
        if (iStrideIndex >= pLayoutInfo->m_UnwindInfoLookupTableEntryCount[iRange])
            iStrideIndex = pLayoutInfo->m_UnwindInfoLookupTableEntryCount[iRange] - 1;

        int Low = pRuntimeFunctionLookupTable[iStrideIndex];
        int High = pRuntimeFunctionLookupTable[iStrideIndex+1];

        PTR_RUNTIME_FUNCTION FunctionTable = pLayoutInfo->m_pRuntimeFunctions[iRange];
        PTR_DWORD pMethodDescs = pLayoutInfo->m_MethodDescs[iRange];

        int MethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(RelativePc,
                                                                               FunctionTable,
                                                                               Low,
                                                                               High);

        if (MethodIndex < 0)
            return FALSE;

#ifdef WIN64EXCEPTIONS
        // Save the raw entry
        PTR_RUNTIME_FUNCTION RawFunctionEntry = FunctionTable + MethodIndex;;

        // Skip funclets to get the method desc
        while (pMethodDescs[MethodIndex] == 0)
            MethodIndex--;
#endif

        FunctionEntry = FunctionTable + MethodIndex;

        if (ppMethodDesc)
        {
            DWORD methodDescRVA = pMethodDescs[MethodIndex];
            _ASSERTE(methodDescRVA != NULL);

            // Note that the MethodDesc does not have to be restored. (It happens when we are called
            // from SetupGcCoverageForNativeMethod.)
            *ppMethodDesc = PTR_MethodDesc((methodDescRVA & ~HAS_EXCEPTION_INFO_MASK) + ImageBase);

            // We are likely executing the code already or going to execute it soon. However, there are a few cases like
            // code:MethodTable::GetMethodDescForSlot where it is not the case. Log the code access here to avoid these
            // cases from touching cold code maps.
            g_IBCLogger.LogMethodCodeAccess(*ppMethodDesc);
        }

        //Get the function entry that corresponds to the real method desc.
        _ASSERTE(RelativePc >= RUNTIME_FUNCTION__BeginAddress(FunctionEntry));
    
        if (pCodeInfo)
        {
            pCodeInfo->m_relOffset = (DWORD)
                (RelativePc - RUNTIME_FUNCTION__BeginAddress(FunctionEntry));

            // We are using RUNTIME_FUNCTION as METHODTOKEN
            pCodeInfo->m_methodToken = METHODTOKEN(pRangeSection, dac_cast<TADDR>(FunctionEntry));

#ifdef WIN64EXCEPTIONS
            AMD64_ONLY(_ASSERTE((RawFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0));
            pCodeInfo->m_pFunctionEntry = RawFunctionEntry;
#endif
        }
    }

    return TRUE;
}

#if defined(WIN64EXCEPTIONS)
PTR_RUNTIME_FUNCTION NativeImageJitManager::LazyGetFunctionEntry(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!pCodeInfo->IsValid())
    {
        return NULL;
    }

    // code:NativeImageJitManager::JitCodeToMethodInfo computes PTR_RUNTIME_FUNCTION eagerly. This path is only 
    // reachable via EECodeInfo::GetMainFunctionInfo, and so we can just return the main entry.
    _ASSERTE(pCodeInfo->GetRelOffset() == 0);

    return dac_cast<PTR_RUNTIME_FUNCTION>(pCodeInfo->GetMethodToken().m_pCodeHeader);
}

TADDR NativeImageJitManager::GetFuncletStartAddress(EECodeInfo * pCodeInfo)
{
    LIMITED_METHOD_DAC_CONTRACT;

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    NGenLayoutInfo * pLayoutInfo = JitTokenToZapModule(pCodeInfo->GetMethodToken())->GetNGenLayoutInfo();

    if (pLayoutInfo->m_CodeSections[2].IsInRange(pCodeInfo->GetCodeAddress()))
    {
        // If the address is in the cold section, then we assume it is cold main function
        // code, NOT a funclet. So, don't do the backward walk: just return the start address
        // of the main function.
        // @ARMTODO: Handle hot/cold splitting with EH funclets
        return pCodeInfo->GetStartAddress();
    }
#endif

    return IJitManager::GetFuncletStartAddress(pCodeInfo);
}

static void GetFuncletStartOffsetsHelper(PCODE pCodeStart, SIZE_T size, SIZE_T ofsAdj,
                                         PTR_RUNTIME_FUNCTION pFunctionEntry, TADDR moduleBase,
                                         DWORD * pnFunclets, DWORD* pStartFuncletOffsets, DWORD dwLength)
{
    _ASSERTE(FitsInU4((pCodeStart + size) - moduleBase));
    DWORD endAddress = (DWORD)((pCodeStart + size) - moduleBase);

    // Entries are sorted and terminated by sentinel value (DWORD)-1
    for ( ; RUNTIME_FUNCTION__BeginAddress(pFunctionEntry) < endAddress; pFunctionEntry++)
    {
#ifdef _TARGET_AMD64_
        _ASSERTE((pFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0);
#endif

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS)
        if (IsFunctionFragment(moduleBase, pFunctionEntry))
        {
            // This is a fragment (not the funclet beginning); skip it
            continue;
        }
#endif // EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS

        if (*pnFunclets < dwLength)
        {
            TADDR funcletStartAddress = (moduleBase + RUNTIME_FUNCTION__BeginAddress(pFunctionEntry)) + ofsAdj;
            _ASSERTE(FitsInU4(funcletStartAddress - pCodeStart));
            pStartFuncletOffsets[*pnFunclets] = (DWORD)(funcletStartAddress - pCodeStart);
        }
        (*pnFunclets)++;
    }
}

DWORD NativeImageJitManager::GetFuncletStartOffsets(const METHODTOKEN& MethodToken, DWORD* pStartFuncletOffsets, DWORD dwLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PTR_RUNTIME_FUNCTION pFirstFuncletFunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader) + 1;
    
    TADDR moduleBase = JitTokenToModuleBase(MethodToken);
    DWORD nFunclets = 0;
    MethodRegionInfo regionInfo;
    JitTokenToMethodRegionInfo(MethodToken, &regionInfo);

    // pFirstFuncletFunctionEntry will work for ARM when passed to GetFuncletStartOffsetsHelper()
    // even if it is a fragment of the main body and not a RUNTIME_FUNCTION for the beginning
    // of the first hot funclet, because GetFuncletStartOffsetsHelper() will skip all the function
    // fragments until the first funclet, if any, is found.

    GetFuncletStartOffsetsHelper(regionInfo.hotStartAddress, regionInfo.hotSize, 0,
        pFirstFuncletFunctionEntry, moduleBase,
        &nFunclets, pStartFuncletOffsets, dwLength);

    // There are no funclets in cold section on ARM yet
    // @ARMTODO: support hot/cold splitting in functions with EH
#if !defined(_TARGET_ARM_) && !defined(_TARGET_ARM64_)
    if (regionInfo.coldSize != NULL)
    {
        NGenLayoutInfo * pLayoutInfo = JitTokenToZapModule(MethodToken)->GetNGenLayoutInfo();
    
        int iColdMethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(
                                                    (DWORD)(regionInfo.coldStartAddress - moduleBase),
                                                    pLayoutInfo->m_pRuntimeFunctions[2],
                                                    0,
                                                    pLayoutInfo->m_nRuntimeFunctions[2] - 1);

        PTR_RUNTIME_FUNCTION pFunctionEntry = pLayoutInfo->m_pRuntimeFunctions[2] + iColdMethodIndex;

        _ASSERTE(regionInfo.coldStartAddress == moduleBase + RUNTIME_FUNCTION__BeginAddress(pFunctionEntry));

#ifdef _TARGET_AMD64_
        // Skip cold part of the method body
        if ((pFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) != 0)
            pFunctionEntry++;
#endif

        GetFuncletStartOffsetsHelper(regionInfo.coldStartAddress, regionInfo.coldSize, regionInfo.hotSize,
            pFunctionEntry, moduleBase,
            &nFunclets, pStartFuncletOffsets, dwLength);
    }
#endif // !_TARGET_ARM_ && !_TARGET_ARM64

    return nFunclets;
}

BOOL NativeImageJitManager::IsFilterFunclet(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pCodeInfo->IsFunclet())
        return FALSE;

    //
    // The generic IsFilterFunclet implementation is touching exception handling tables.
    // It is bad for working set because of it is sometimes called during GC stackwalks.
    // The optimized version for native images does not touch exception handling tables.
    //

    NGenLayoutInfo * pLayoutInfo = JitTokenToZapModule(pCodeInfo->GetMethodToken())->GetNGenLayoutInfo();

    SIZE_T size;
    PTR_VOID pUnwindData = GetUnwindDataBlob(pCodeInfo->GetModuleBase(), pCodeInfo->GetFunctionEntry(), &size);
    _ASSERTE(pUnwindData != NULL);

    // Personality routine is always the last element of the unwind data
    DWORD rvaPersonalityRoutine = *(dac_cast<PTR_DWORD>(dac_cast<TADDR>(pUnwindData) + size) - 1);

    BOOL fRet = (pLayoutInfo->m_rvaFilterPersonalityRoutine == rvaPersonalityRoutine);

    // Verify that the optimized implementation is in sync with the slow implementation
    _ASSERTE(fRet == IJitManager::IsFilterFunclet(pCodeInfo));

    return fRet;
}

#endif  // WIN64EXCEPTIONS
 
StubCodeBlockKind NativeImageJitManager::GetStubCodeBlockKind(RangeSection * pRangeSection, PCODE currentPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    Module * pZapModule = dac_cast<PTR_Module>(pRangeSection->pHeapListOrZapModule);

    if (pZapModule->IsZappedPrecode(currentPC))
    {
        return STUB_CODE_BLOCK_PRECODE;
    }

    NGenLayoutInfo * pLayoutInfo = pZapModule->GetNGenLayoutInfo();
    _ASSERTE(pLayoutInfo != NULL);

    if (pLayoutInfo->m_JumpStubs.IsInRange(currentPC))
    {
        return STUB_CODE_BLOCK_JUMPSTUB;
    }

    if (pLayoutInfo->m_StubLinkStubs.IsInRange(currentPC))
    {
        return STUB_CODE_BLOCK_STUBLINK;
    }

    if (pLayoutInfo->m_VirtualMethodThunks.IsInRange(currentPC))
    {
        return STUB_CODE_BLOCK_VIRTUAL_METHOD_THUNK;
    }

    if (pLayoutInfo->m_ExternalMethodThunks.IsInRange(currentPC))
    {
        return STUB_CODE_BLOCK_EXTERNAL_METHOD_THUNK;
    }

    return STUB_CODE_BLOCK_UNKNOWN;
}

PTR_Module NativeImageJitManager::JitTokenToZapModule(const METHODTOKEN& MethodToken)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<PTR_Module>(MethodToken.m_pRangeSection->pHeapListOrZapModule);
}
void NativeImageJitManager::JitTokenToMethodRegionInfo(const METHODTOKEN& MethodToken, 
                                                   MethodRegionInfo * methodRegionInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    _ASSERTE(methodRegionInfo != NULL);

    //
    // Initialize methodRegionInfo assuming that the method is entirely hot.  This is the common
    // case (either binary is not procedure split or the current method is all hot).  We can
    // adjust these values later if necessary.
    //

    methodRegionInfo->hotStartAddress  = JitTokenToStartAddress(MethodToken);
    methodRegionInfo->hotSize          = GetCodeManager()->GetFunctionSize(GetGCInfo(MethodToken));
    methodRegionInfo->coldStartAddress = 0;
    methodRegionInfo->coldSize         = 0;

    RangeSection *rangeSection = MethodToken.m_pRangeSection;
    PREFIX_ASSUME(rangeSection != NULL);

    Module * pModule = dac_cast<PTR_Module>(rangeSection->pHeapListOrZapModule);

    NGenLayoutInfo * pLayoutInfo = pModule->GetNGenLayoutInfo();

    //
    // If this module is not procedure split, then we're done.
    //
    if (pLayoutInfo->m_CodeSections[2].Size() == 0)
        return;

    //
    // Perform a binary search in the cold range section until we find our method
    //

    TADDR ImageBase = rangeSection->LowAddress;

    int Low = 0;
    int High = pLayoutInfo->m_nRuntimeFunctions[2] - 1;

    PTR_RUNTIME_FUNCTION pRuntimeFunctionTable = pLayoutInfo->m_pRuntimeFunctions[2];
    PTR_CORCOMPILE_COLD_METHOD_ENTRY pColdCodeMap = pLayoutInfo->m_ColdCodeMap;

    while (Low <= High)
    {
        int Middle = Low + (High - Low) / 2;

        int ColdMethodIndex = Middle;

        PTR_RUNTIME_FUNCTION FunctionEntry;

#ifdef WIN64EXCEPTIONS
        while (pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA == 0)
            ColdMethodIndex--;

        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(ImageBase + pColdCodeMap[ColdMethodIndex].mainFunctionEntryRVA);
#else
        DWORD ColdUnwindData = pRuntimeFunctionTable[ColdMethodIndex].UnwindData;
        _ASSERTE((ColdUnwindData & RUNTIME_FUNCTION_INDIRECT) != 0);
        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(ImageBase + (ColdUnwindData & ~RUNTIME_FUNCTION_INDIRECT));
#endif

        if (FunctionEntry == dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader))
        {
            PTR_RUNTIME_FUNCTION ColdFunctionEntry = pRuntimeFunctionTable + ColdMethodIndex;

            methodRegionInfo->coldStartAddress = ImageBase + RUNTIME_FUNCTION__BeginAddress(ColdFunctionEntry);

            //
            // At this point methodRegionInfo->hotSize is set to the total size of
            // the method obtained from the GC info (we set that in the init code above).
            // Use that and coldHeader->hotCodeSize to compute the hot and cold code sizes.
            //

            ULONG hotCodeSize = pColdCodeMap[ColdMethodIndex].hotCodeSize;

            methodRegionInfo->coldSize         = methodRegionInfo->hotSize - hotCodeSize;
            methodRegionInfo->hotSize          = hotCodeSize;

            return;
        }
        else if (FunctionEntry < dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader))
        {
            Low = Middle + 1;
        }
        else
        {
            // Use ColdMethodIndex to take advantage of entries skipped while looking for method start
            High = ColdMethodIndex - 1;
        }
    }

    //
    // We didn't find it.  Therefore this method doesn't have a cold section.
    //

    return;
}

#ifdef DACCESS_COMPILE

void NativeImageJitManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    IJitManager::EnumMemoryRegions(flags);
}

#if defined(WIN64EXCEPTIONS)

// 
// To locate an entry in the function entry table (the program exceptions data directory), the debugger
// performs a binary search over the table.  This function reports the entries that are encountered in the
// binary search.
// 
// Parameters:
//   pRtf: The target function table entry to be located
//   pNativeLayout: A pointer to the loaded native layout for the module containing pRtf
//   
static void EnumRuntimeFunctionEntriesToFindEntry(PTR_RUNTIME_FUNCTION pRtf, PTR_PEImageLayout pNativeLayout)
{
    pRtf.EnumMem();

    if (pNativeLayout == NULL)
    {
        return;
    }

    IMAGE_DATA_DIRECTORY * pProgramExceptionsDirectory = pNativeLayout->GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXCEPTION);
    if (!pProgramExceptionsDirectory || 
        (pProgramExceptionsDirectory->Size == 0) ||
        (pProgramExceptionsDirectory->Size % sizeof(RUNTIME_FUNCTION) != 0))
    {
        // Program exceptions directory malformatted
        return;
    }

    PTR_BYTE moduleBase(pNativeLayout->GetBase());
    PTR_RUNTIME_FUNCTION firstFunctionEntry(moduleBase + pProgramExceptionsDirectory->VirtualAddress);

    if (pRtf < firstFunctionEntry ||
        ((dac_cast<TADDR>(pRtf) - dac_cast<TADDR>(firstFunctionEntry)) % sizeof(RUNTIME_FUNCTION) != 0))
    {
        // Program exceptions directory malformatted
        return;
    }
    
// Review conversion of size_t to ULONG.
#if defined(_MSC_VER)
#pragma warning(push)
#pragma warning(disable:4267)
#endif // defined(_MSC_VER)

    ULONG indexToLocate = pRtf - firstFunctionEntry;

#if defined(_MSC_VER)   
#pragma warning(pop)
#endif // defined(_MSC_VER)

    ULONG low = 0; // index in the function entry table of low end of search range
    ULONG high = (pProgramExceptionsDirectory->Size)/sizeof(RUNTIME_FUNCTION) - 1; // index of high end of search range
    ULONG mid = (low + high) /2; // index of entry to be compared

    if (indexToLocate > high)
    {
        return;
    }

    while (indexToLocate != mid)
    {
        PTR_RUNTIME_FUNCTION functionEntry = firstFunctionEntry + mid;
        functionEntry.EnumMem();
        if (indexToLocate > mid)
        {
            low = mid + 1;
        }
        else
        {
            high = mid - 1;
        }
        mid = (low + high) /2;
        _ASSERTE( low <= mid && mid <= high );
    }
}

//
// EnumMemoryRegionsForMethodUnwindInfo - enumerate the memory necessary to read the unwind info for the
// specified method.
// 
// Note that in theory, a dump generation library could save the unwind information itself without help
// from us, since it's stored in the image in the standard function table layout for Win64.  However, 
// dump-generation libraries assume that the image will be available at debug time, and if the image 
// isn't available then it is acceptable for stackwalking to break.  For ngen images (which are created 
// on the client), it usually isn't possible to have the image available at debug time, and so for minidumps
// we must explicitly ensure the unwind information is saved into the dump. 
// 
// Arguments:
//     flags - EnumMem flags
//     pMD   - MethodDesc for the method in question
//     
void NativeImageJitManager::EnumMemoryRegionsForMethodUnwindInfo(CLRDataEnumMemoryFlags flags, EECodeInfo * pCodeInfo)
{
    // Get the RUNTIME_FUNCTION entry for this method 
    PTR_RUNTIME_FUNCTION pRtf = pCodeInfo->GetFunctionEntry();

    if (pRtf==NULL)
    {
        return;
    }

    // Enumerate the function entry and other entries needed to locate it in the program exceptions directory
    Module * pModule = JitTokenToZapModule(pCodeInfo->GetMethodToken());
    EnumRuntimeFunctionEntriesToFindEntry(pRtf, pModule->GetFile()->GetLoadedNative());

    SIZE_T size;
    PTR_VOID pUnwindData = GetUnwindDataBlob(pCodeInfo->GetModuleBase(), pRtf, &size);
    if (pUnwindData != NULL)
        DacEnumMemoryRegion(PTR_TO_TADDR(pUnwindData), size); 
}

#endif //WIN64EXCEPTIONS
#endif // #ifdef DACCESS_COMPILE

// Return start of exception info for a method, or 0 if the method has no EH info
DWORD NativeExceptionInfoLookupTable::LookupExceptionInfoRVAForMethod(PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE pExceptionLookupTable,
                                                                              COUNT_T numLookupEntries,
                                                                              DWORD methodStartRVA,
                                                                              COUNT_T* pSize)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    _ASSERTE(pExceptionLookupTable != NULL);

    COUNT_T start = 0;
    COUNT_T end = numLookupEntries - 2;

    // The last entry in the lookup table (end-1) points to a sentinal entry.
    // The sentinal entry helps to determine the number of EH clauses for the last table entry.
    _ASSERTE(pExceptionLookupTable->ExceptionLookupEntry(numLookupEntries-1)->MethodStartRVA == (DWORD)-1);

    // Binary search the lookup table
    // Using linear search is faster once we get down to small number of entries.
    while (end - start > 10)
    {
        COUNT_T middle = start + (end - start) / 2;

        _ASSERTE(start < middle && middle < end);

        DWORD rva = pExceptionLookupTable->ExceptionLookupEntry(middle)->MethodStartRVA;

        if (methodStartRVA < rva)
        {
            end = middle - 1;
        }
        else 
        {
            start = middle;
        }
    }

    for (COUNT_T i = start; i <= end; ++i)
    {
        DWORD rva = pExceptionLookupTable->ExceptionLookupEntry(i)->MethodStartRVA;
        if (methodStartRVA  == rva)
        {
            CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY *pEntry = pExceptionLookupTable->ExceptionLookupEntry(i);

            //Get the count of EH Clause entries
            CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY * pNextEntry = pExceptionLookupTable->ExceptionLookupEntry(i + 1);
            *pSize = pNextEntry->ExceptionInfoRVA - pEntry->ExceptionInfoRVA;

            return pEntry->ExceptionInfoRVA;
        }
    }

    // Not found
    return 0;
}

int NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(DWORD RelativePc,
                                                           PTR_RUNTIME_FUNCTION pRuntimeFunctionTable,
                                                           int Low,
                                                           int High)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;


#ifdef _TARGET_ARM_
    RelativePc |= THUMB_CODE;
#endif 

    // Entries are sorted and terminated by sentinel value (DWORD)-1

    // Binary search the RUNTIME_FUNCTION table
    // Use linear search once we get down to a small number of elements
    // to avoid Binary search overhead.
    while (High - Low > 10) 
    {
       int Middle = Low + (High - Low) / 2;

       PTR_RUNTIME_FUNCTION pFunctionEntry = pRuntimeFunctionTable + Middle;
       if (RelativePc < pFunctionEntry->BeginAddress) 
       {
           High = Middle - 1;
       } 
       else 
       {
           Low = Middle;
       }
    }

    for (int i = Low; i <= High; ++i)
    {
        // This is safe because of entries are terminated by sentinel value (DWORD)-1
        PTR_RUNTIME_FUNCTION pNextFunctionEntry = pRuntimeFunctionTable + (i + 1);

        if (RelativePc < pNextFunctionEntry->BeginAddress)
        {
            PTR_RUNTIME_FUNCTION pFunctionEntry = pRuntimeFunctionTable + i;
            if (RelativePc >= pFunctionEntry->BeginAddress)
            {
                return i;
            }
            break;
        }
    }

    return -1;
}

BOOL NativeUnwindInfoLookupTable::HasExceptionInfo(NGenLayoutInfo * pNgenLayout, PTR_RUNTIME_FUNCTION pMainRuntimeFunction)
{
    LIMITED_METHOD_DAC_CONTRACT;
    DWORD methodDescRVA = NativeUnwindInfoLookupTable::GetMethodDescRVA(pNgenLayout, pMainRuntimeFunction);
    return (methodDescRVA & HAS_EXCEPTION_INFO_MASK);
}

PTR_MethodDesc NativeUnwindInfoLookupTable::GetMethodDesc(NGenLayoutInfo * pNgenLayout, PTR_RUNTIME_FUNCTION pMainRuntimeFunction, TADDR moduleBase)
{
    LIMITED_METHOD_DAC_CONTRACT;
    DWORD methodDescRVA = NativeUnwindInfoLookupTable::GetMethodDescRVA(pNgenLayout, pMainRuntimeFunction);
    return PTR_MethodDesc((methodDescRVA & ~HAS_EXCEPTION_INFO_MASK) + moduleBase);
}

DWORD NativeUnwindInfoLookupTable::GetMethodDescRVA(NGenLayoutInfo * pNgenLayout, PTR_RUNTIME_FUNCTION pMainRuntimeFunction)
{
    LIMITED_METHOD_DAC_CONTRACT;

    COUNT_T iIndex = (COUNT_T)(pMainRuntimeFunction - pNgenLayout->m_pRuntimeFunctions[0]);
    DWORD rva = 0;
    if (iIndex >= pNgenLayout->m_nRuntimeFunctions[0])
    {
        iIndex = (COUNT_T)(pMainRuntimeFunction - pNgenLayout->m_pRuntimeFunctions[1]);
        _ASSERTE(iIndex < pNgenLayout->m_nRuntimeFunctions[1]);
        rva = pNgenLayout->m_MethodDescs[1][iIndex];
    }
    else
    {
        rva = pNgenLayout->m_MethodDescs[0][iIndex];
    }
    _ASSERTE(rva != 0);

    return rva;
}

#endif // FEATURE_PREJIT

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------


// Nirvana Support

MethodDesc* __stdcall Nirvana_FindMethodDesc(PCODE ptr, BYTE*& hotStartAddress, size_t& hotSize, BYTE*& coldStartAddress, size_t & coldSize)
{
    EECodeInfo codeInfo(ptr);
    if (!codeInfo.IsValid())
        return NULL;

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    hotStartAddress  = (BYTE*)methodRegionInfo.hotStartAddress;
    hotSize          = methodRegionInfo.hotSize;
    coldStartAddress = (BYTE*)methodRegionInfo.coldStartAddress;
    coldSize         = methodRegionInfo.coldSize;

    return codeInfo.GetMethodDesc();
}


bool Nirvana_GetMethodInfo(MethodDesc * pMD, BYTE*& hotStartAddress, size_t& hotSize, BYTE*& coldStartAddress, size_t & coldSize)
{
    EECodeInfo codeInfo(pMD->GetNativeCode());
    if (!codeInfo.IsValid())
        return false;

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    hotStartAddress  = (BYTE*)methodRegionInfo.hotStartAddress;
    hotSize          = methodRegionInfo.hotSize;
    coldStartAddress = (BYTE*)methodRegionInfo.coldStartAddress;
    coldSize         = methodRegionInfo.coldSize;

    return true;
}


#include "sigformat.h"

__forceinline bool Nirvana_PrintMethodDescWorker(__in_ecount(iBuffer) char * szBuffer, size_t iBuffer, MethodDesc * pMD, const char * pSigString)
{
    if (iBuffer == 0) 
        return false;

    szBuffer[0] = '\0';
    pSigString = strchr(pSigString, ' ');

    if (pSigString == NULL)
        return false;

    ++pSigString;

    LPCUTF8 pNamespace;
    LPCUTF8 pClassName = pMD->GetMethodTable()->GetFullyQualifiedNameInfo(&pNamespace);

    if (pClassName == NULL)
        return false;

    if (*pNamespace != 0)
    {
        if(FAILED(StringCchPrintfA(szBuffer, iBuffer, "%s.%s.%s", pNamespace, pClassName, pSigString)))
            return false;
    }
    else
    {
        if(FAILED(StringCchPrintfA(szBuffer, iBuffer, "%s.%s", pClassName, pSigString)))
            return false;
    }

    _ASSERTE(szBuffer[0] != '\0');

    return true;
}

bool __stdcall Nirvana_PrintMethodDesc(__in_ecount(iBuffer) char * szBuffer, size_t iBuffer, MethodDesc * pMD)
{
    bool fResult = false;

    EX_TRY
    {
        NewHolder<SigFormat> pSig = new SigFormat(pMD, NULL, false);
        fResult = Nirvana_PrintMethodDescWorker(szBuffer, iBuffer, pMD, pSig->GetCString());
    }
    EX_CATCH
    {
        fResult = false;
    }
    EX_END_CATCH(SwallowAllExceptions)
    
    return fResult;
};


// Nirvana_Dummy() is a dummy function that is exported privately by ordinal only.
// The sole purpose of this function is to reference Nirvana_FindMethodDesc(),
// Nirvana_GetMethodInfo(), and Nirvana_PrintMethodDesc() so that they are not
// inlined or removed by the compiler or the linker.

DWORD __stdcall Nirvana_Dummy()
{
    LIMITED_METHOD_CONTRACT;
    void * funcs[] = { 
        (void*)Nirvana_FindMethodDesc,
        (void*)Nirvana_GetMethodInfo,
        (void*)Nirvana_PrintMethodDesc 
    };

    size_t n = sizeof(funcs) / sizeof(funcs[0]);

    size_t sum = 0;
    for (size_t i = 0; i < n; ++i)
        sum += (size_t)funcs[i];

    return (DWORD)sum;
}


#endif // #ifndef DACCESS_COMPILE


#ifdef FEATURE_PREJIT

MethodIterator::MethodIterator(PTR_Module pModule, MethodIteratorOptions mio)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Init(pModule, pModule->GetNativeImage(),  mio);
}

MethodIterator::MethodIterator(PTR_Module pModule, PEDecoder * pPEDecoder, MethodIteratorOptions mio)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Init(pModule, pPEDecoder,  mio);
}

void MethodIterator::Init(PTR_Module pModule, PEDecoder * pPEDecoder, MethodIteratorOptions mio)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_ModuleBase = dac_cast<TADDR>(pPEDecoder->GetBase());

    methodIteratorOptions = mio;

    m_pNgenLayout = pModule->GetNGenLayoutInfo();

    m_fHotMethodsDone = FALSE;
    m_CurrentRuntimeFunctionIndex = -1;
    m_CurrentColdRuntimeFunctionIndex = 0;
}

BOOL MethodIterator::Next()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_CurrentRuntimeFunctionIndex ++;

    if (!m_fHotMethodsDone)
    {
        //iterate the hot methods 
        if (methodIteratorOptions & Hot)
        {
#ifdef WIN64EXCEPTIONS
            //Skip to the next method.
            // skip over method fragments and funclets.
            while (m_CurrentRuntimeFunctionIndex < m_pNgenLayout->m_nRuntimeFunctions[0])
            {
                if (m_pNgenLayout->m_MethodDescs[0][m_CurrentRuntimeFunctionIndex] != 0)
                    return TRUE;
                m_CurrentRuntimeFunctionIndex++;
            }
#else
            if (m_CurrentRuntimeFunctionIndex < m_pNgenLayout->m_nRuntimeFunctions[0])
                return TRUE;
#endif
        }
        m_CurrentRuntimeFunctionIndex = 0;
        m_fHotMethodsDone = TRUE;
    }

    if (methodIteratorOptions & Unprofiled)
    {
#ifdef WIN64EXCEPTIONS
         //Skip to the next method.
        // skip over method fragments and funclets.
        while (m_CurrentRuntimeFunctionIndex < m_pNgenLayout->m_nRuntimeFunctions[1])
        {
            if (m_pNgenLayout->m_MethodDescs[1][m_CurrentRuntimeFunctionIndex] != 0)
                return TRUE;
            m_CurrentRuntimeFunctionIndex++;
        }
#else
        if (m_CurrentRuntimeFunctionIndex < m_pNgenLayout->m_nRuntimeFunctions[1])
            return TRUE;
#endif
    }

    return FALSE;
}

PTR_MethodDesc MethodIterator::GetMethodDesc()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    } 
    CONTRACTL_END;

    return NativeUnwindInfoLookupTable::GetMethodDesc(m_pNgenLayout, GetRuntimeFunction(), m_ModuleBase);
}

PTR_VOID MethodIterator::GetGCInfo()
{
    LIMITED_METHOD_CONTRACT;

    // get the gc info from the RT function
    SIZE_T size;
    PTR_VOID pUnwindData = GetUnwindDataBlob(m_ModuleBase, GetRuntimeFunction(), &size);
    return (PTR_VOID)((PTR_BYTE)pUnwindData + size);
}

TADDR MethodIterator::GetMethodStartAddress()
{
    LIMITED_METHOD_CONTRACT;

    return m_ModuleBase + RUNTIME_FUNCTION__BeginAddress(GetRuntimeFunction());
}

TADDR MethodIterator::GetMethodColdStartAddress()
{
    LIMITED_METHOD_CONTRACT;

    PTR_RUNTIME_FUNCTION CurrentFunctionEntry = GetRuntimeFunction();

    //
    // Catch up with hot code
    //
    for ( ; m_CurrentColdRuntimeFunctionIndex < m_pNgenLayout->m_nRuntimeFunctions[2]; m_CurrentColdRuntimeFunctionIndex++)
    {
        PTR_RUNTIME_FUNCTION ColdFunctionEntry = m_pNgenLayout->m_pRuntimeFunctions[2] + m_CurrentColdRuntimeFunctionIndex;

        PTR_RUNTIME_FUNCTION FunctionEntry;

#ifdef WIN64EXCEPTIONS
        DWORD MainFunctionEntryRVA = m_pNgenLayout->m_ColdCodeMap[m_CurrentColdRuntimeFunctionIndex].mainFunctionEntryRVA;

        if (MainFunctionEntryRVA == 0)
            continue;

        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(m_ModuleBase + MainFunctionEntryRVA);
#else
        DWORD ColdUnwindData = ColdFunctionEntry->UnwindData;
        _ASSERTE((ColdUnwindData & RUNTIME_FUNCTION_INDIRECT) != 0);
        FunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(m_ModuleBase + (ColdUnwindData & ~RUNTIME_FUNCTION_INDIRECT));
#endif

        if (CurrentFunctionEntry == FunctionEntry)
        {
            // we found a match
            return m_ModuleBase + RUNTIME_FUNCTION__BeginAddress(ColdFunctionEntry);
        }
        else
        if (CurrentFunctionEntry < FunctionEntry)
        {
            // method does not have cold code
            return NULL;
        }
    }

    return NULL;
}

PTR_RUNTIME_FUNCTION MethodIterator::GetRuntimeFunction()
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(m_CurrentRuntimeFunctionIndex >= 0);
    _ASSERTE(m_CurrentRuntimeFunctionIndex < (m_fHotMethodsDone ? m_pNgenLayout->m_nRuntimeFunctions[1] : m_pNgenLayout->m_nRuntimeFunctions[0]));
    return (m_fHotMethodsDone ? m_pNgenLayout->m_pRuntimeFunctions[1] : m_pNgenLayout->m_pRuntimeFunctions[0]) + m_CurrentRuntimeFunctionIndex;
}

ULONG MethodIterator::GetHotCodeSize()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(GetMethodColdStartAddress() != NULL);
    return m_pNgenLayout->m_ColdCodeMap[m_CurrentColdRuntimeFunctionIndex].hotCodeSize;
}

void MethodIterator::GetMethodRegionInfo(IJitManager::MethodRegionInfo *methodRegionInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    methodRegionInfo->hotStartAddress  = GetMethodStartAddress();
    methodRegionInfo->coldStartAddress = GetMethodColdStartAddress();

    methodRegionInfo->hotSize          = ExecutionManager::GetNativeImageJitManager()->GetCodeManager()->GetFunctionSize(GetGCInfo());
    methodRegionInfo->coldSize         = 0;

    if (methodRegionInfo->coldStartAddress != NULL)
    {
        //
        // At this point methodRegionInfo->hotSize is set to the total size of
        // the method obtained from the GC info (we set that in the init code above).
        // Use that and pCMH->hotCodeSize to compute the hot and cold code sizes.
        //

        ULONG hotCodeSize = GetHotCodeSize();

        methodRegionInfo->coldSize         = methodRegionInfo->hotSize - hotCodeSize;
        methodRegionInfo->hotSize          = hotCodeSize;
    }
}

#endif // FEATURE_PREJIT



#ifdef FEATURE_READYTORUN

//***************************************************************************************
//***************************************************************************************

#ifndef DACCESS_COMPILE

ReadyToRunJitManager::ReadyToRunJitManager()
{
    WRAPPER_NO_CONTRACT;
}

#endif // #ifndef DACCESS_COMPILE

ReadyToRunInfo * ReadyToRunJitManager::JitTokenToReadyToRunInfo(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return dac_cast<PTR_Module>(MethodToken.m_pRangeSection->pHeapListOrZapModule)->GetReadyToRunInfo();
}

PTR_RUNTIME_FUNCTION ReadyToRunJitManager::JitTokenToRuntimeFunction(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader);
}

TADDR ReadyToRunJitManager::JitTokenToStartAddress(const METHODTOKEN& MethodToken)
{ 
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return JitTokenToModuleBase(MethodToken) + 
        RUNTIME_FUNCTION__BeginAddress(dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader));
}

PTR_VOID ReadyToRunJitManager::GetGCInfo(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    PTR_RUNTIME_FUNCTION pRuntimeFunction = JitTokenToRuntimeFunction(MethodToken);
    TADDR baseAddress = JitTokenToModuleBase(MethodToken);

#ifndef DACCESS_COMPILE
    if (g_IBCLogger.InstrEnabled())
    {
        ReadyToRunInfo * pInfo = JitTokenToReadyToRunInfo(MethodToken);
        MethodDesc * pMD = pInfo->GetMethodDescForEntryPoint(JitTokenToStartAddress(MethodToken));
        g_IBCLogger.LogMethodGCInfoAccess(pMD);
    }
#endif

    SIZE_T nUnwindDataSize;
    PTR_VOID pUnwindData = GetUnwindDataBlob(baseAddress, pRuntimeFunction, &nUnwindDataSize);

    // GCInfo immediatelly follows unwind data
    return dac_cast<PTR_BYTE>(pUnwindData) + nUnwindDataSize;
}

unsigned ReadyToRunJitManager::InitializeEHEnumeration(const METHODTOKEN& MethodToken, EH_CLAUSE_ENUMERATOR* pEnumState)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    ReadyToRunInfo * pReadyToRunInfo = JitTokenToReadyToRunInfo(MethodToken);

    IMAGE_DATA_DIRECTORY * pExceptionInfoDir = pReadyToRunInfo->FindSection(READYTORUN_SECTION_EXCEPTION_INFO);
    if (pExceptionInfoDir == NULL)
        return 0;

    PEImageLayout * pLayout = pReadyToRunInfo->GetImage();

    PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE pExceptionLookupTable = dac_cast<PTR_CORCOMPILE_EXCEPTION_LOOKUP_TABLE>(pLayout->GetRvaData(pExceptionInfoDir->VirtualAddress));

    COUNT_T numLookupTableEntries = (COUNT_T)(pExceptionInfoDir->Size / sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
    // at least 2 entries (1 valid entry + 1 sentinal entry)
    _ASSERTE(numLookupTableEntries >= 2);

    DWORD methodStartRVA = (DWORD)(JitTokenToStartAddress(MethodToken) - JitTokenToModuleBase(MethodToken));

    COUNT_T ehInfoSize = 0;
    DWORD exceptionInfoRVA = NativeExceptionInfoLookupTable::LookupExceptionInfoRVAForMethod(pExceptionLookupTable,
                                                                  numLookupTableEntries,
                                                                  methodStartRVA, 
                                                                  &ehInfoSize);
    if (exceptionInfoRVA == 0)
        return 0;

    pEnumState->iCurrentPos = 0;
    pEnumState->pExceptionClauseArray = JitTokenToModuleBase(MethodToken) + exceptionInfoRVA;

    return ehInfoSize / sizeof(CORCOMPILE_EXCEPTION_CLAUSE);
}

PTR_EXCEPTION_CLAUSE_TOKEN ReadyToRunJitManager::GetNextEHClause(EH_CLAUSE_ENUMERATOR* pEnumState,
                              EE_ILEXCEPTION_CLAUSE* pEHClauseOut)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    unsigned iCurrentPos = pEnumState->iCurrentPos;
    pEnumState->iCurrentPos++;

    CORCOMPILE_EXCEPTION_CLAUSE* pClause = &(dac_cast<PTR_CORCOMPILE_EXCEPTION_CLAUSE>(pEnumState->pExceptionClauseArray)[iCurrentPos]);

    // copy to the input parmeter, this is a nice abstraction for the future
    // if we want to compress the Clause encoding, we can do without affecting the call sites
    pEHClauseOut->TryStartPC = pClause->TryStartPC; 
    pEHClauseOut->TryEndPC = pClause->TryEndPC; 
    pEHClauseOut->HandlerStartPC = pClause->HandlerStartPC; 
    pEHClauseOut->HandlerEndPC = pClause->HandlerEndPC;
    pEHClauseOut->Flags = pClause->Flags;
    pEHClauseOut->FilterOffset = pClause->FilterOffset;

    return dac_cast<PTR_EXCEPTION_CLAUSE_TOKEN>(pClause);
}

StubCodeBlockKind ReadyToRunJitManager::GetStubCodeBlockKind(RangeSection * pRangeSection, PCODE currentPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD rva = (DWORD)(currentPC - pRangeSection->LowAddress);

    ReadyToRunInfo * pReadyToRunInfo = dac_cast<PTR_Module>(pRangeSection->pHeapListOrZapModule)->GetReadyToRunInfo();

    IMAGE_DATA_DIRECTORY * pDelayLoadMethodCallThunksDir = pReadyToRunInfo->FindSection(READYTORUN_SECTION_DELAYLOAD_METHODCALL_THUNKS);
    if (pDelayLoadMethodCallThunksDir != NULL)
    {
        if (pDelayLoadMethodCallThunksDir->VirtualAddress <= rva 
                && rva < pDelayLoadMethodCallThunksDir->VirtualAddress + pDelayLoadMethodCallThunksDir->Size)
            return STUB_CODE_BLOCK_METHOD_CALL_THUNK;
    }

    return STUB_CODE_BLOCK_UNKNOWN;
}

#ifndef DACCESS_COMPILE

TypeHandle ReadyToRunJitManager::ResolveEHClause(EE_ILEXCEPTION_CLAUSE* pEHClause,
                                              CrawlFrame* pCf)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    _ASSERTE(NULL != pCf);
    _ASSERTE(NULL != pEHClause);
    _ASSERTE(IsTypedHandler(pEHClause));
    
    MethodDesc *pMD = PTR_MethodDesc(pCf->GetFunction());

    _ASSERTE(pMD != NULL);

    Module* pModule = pMD->GetModule();
    PREFIX_ASSUME(pModule != NULL);

    SigTypeContext typeContext(pMD);
    VarKind k = hasNoVars;

    mdToken typeTok = pEHClause->ClassToken;

    // In the vast majority of cases the code un der the "if" below
    // will not be executed.
    //
    // First grab the representative instantiations.  For code
    // shared by multiple generic instantiations these are the
    // canonical (representative) instantiation.
    if (TypeFromToken(typeTok) == mdtTypeSpec)
    {
        PCCOR_SIGNATURE pSig;
        ULONG cSig;
        IfFailThrow(pModule->GetMDImport()->GetTypeSpecFromToken(typeTok, &pSig, &cSig));
        
        SigPointer psig(pSig, cSig);
        k = psig.IsPolyType(&typeContext);

        // Grab the active class and method instantiation.  This exact instantiation is only
        // needed in the corner case of "generic" exception catching in shared
        // generic code.  We don't need the exact instantiation if the token
        // doesn't contain E_T_VAR or E_T_MVAR.        
        if ((k & hasSharableVarsMask) != 0)
        {
            Instantiation classInst;
            Instantiation methodInst;
            pCf->GetExactGenericInstantiations(&classInst,&methodInst);
            SigTypeContext::InitTypeContext(pMD,classInst, methodInst,&typeContext);
        }
    }

    return ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, typeTok, &typeContext, 
                                                          ClassLoader::ReturnNullIfNotFound);
}

#endif // #ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Ngen info manager
//-----------------------------------------------------------------------------
BOOL ReadyToRunJitManager::GetBoundariesAndVars(
        const DebugInfoRequest & request,
        IN FP_IDS_NEW fpNew, IN void * pNewData,
        OUT ULONG32 * pcMap, 
        OUT ICorDebugInfo::OffsetMapping **ppMap,
        OUT ULONG32 * pcVars, 
        OUT ICorDebugInfo::NativeVarInfo **ppVars)
{
    CONTRACTL {
        THROWS;       // on OOM.
        GC_NOTRIGGER; // getting vars shouldn't trigger
        SUPPORTS_DAC;
    } CONTRACTL_END;

    EECodeInfo codeInfo(request.GetStartAddress());
    if (!codeInfo.IsValid())
        return FALSE;

    ReadyToRunInfo * pReadyToRunInfo = JitTokenToReadyToRunInfo(codeInfo.GetMethodToken());
    PTR_RUNTIME_FUNCTION pRuntimeFunction = JitTokenToRuntimeFunction(codeInfo.GetMethodToken());

    PTR_BYTE pDebugInfo = pReadyToRunInfo->GetDebugInfo(pRuntimeFunction);
    if (pDebugInfo == NULL)
        return FALSE;

    // Uncompress. This allocates memory and may throw.
    CompressDebugInfo::RestoreBoundariesAndVars(
        fpNew, pNewData, // allocators
        pDebugInfo,      // input
        pcMap, ppMap,
        pcVars, ppVars); // output

    return TRUE;
}

#ifdef DACCESS_COMPILE
//
// Need to write out debug info
//
void ReadyToRunJitManager::EnumMemoryRegionsForMethodDebugInfo(CLRDataEnumMemoryFlags flags, MethodDesc * pMD)
{
    SUPPORTS_DAC;

    EECodeInfo codeInfo(pMD->GetNativeCode());
    if (!codeInfo.IsValid())
        return;

    ReadyToRunInfo * pReadyToRunInfo = JitTokenToReadyToRunInfo(codeInfo.GetMethodToken());
    PTR_RUNTIME_FUNCTION pRuntimeFunction = JitTokenToRuntimeFunction(codeInfo.GetMethodToken());

    PTR_BYTE pDebugInfo = pReadyToRunInfo->GetDebugInfo(pRuntimeFunction);
    if (pDebugInfo == NULL)
        return;

    CompressDebugInfo::EnumMemoryRegions(flags, pDebugInfo);
}
#endif

PCODE ReadyToRunJitManager::GetCodeAddressForRelOffset(const METHODTOKEN& MethodToken, DWORD relOffset)
{
    WRAPPER_NO_CONTRACT;

    MethodRegionInfo methodRegionInfo;
    JitTokenToMethodRegionInfo(MethodToken, &methodRegionInfo);

    if (relOffset < methodRegionInfo.hotSize)
        return methodRegionInfo.hotStartAddress + relOffset;

    SIZE_T coldOffset = relOffset - methodRegionInfo.hotSize;
    _ASSERTE(coldOffset < methodRegionInfo.coldSize);
    return methodRegionInfo.coldStartAddress + coldOffset;
}

BOOL ReadyToRunJitManager::JitCodeToMethodInfo(RangeSection * pRangeSection,
                                            PCODE currentPC,
                                            MethodDesc** ppMethodDesc,
                                            OUT EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // READYTORUN: FUTURE: Hot-cold spliting

    TADDR currentInstr = PCODEToPINSTR(currentPC);

    TADDR ImageBase = pRangeSection->LowAddress;

    DWORD RelativePc = (DWORD)(currentInstr - ImageBase);

    Module * pModule = dac_cast<PTR_Module>(pRangeSection->pHeapListOrZapModule);
    ReadyToRunInfo * pInfo = pModule->GetReadyToRunInfo();

    COUNT_T nRuntimeFunctions = pInfo->m_nRuntimeFunctions;
    PTR_RUNTIME_FUNCTION pRuntimeFunctions = pInfo->m_pRuntimeFunctions;

    int MethodIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod(RelativePc,
                                                                             pRuntimeFunctions,
                                                                             0,
                                                                             nRuntimeFunctions - 1);

    if (MethodIndex < 0)
        return FALSE;

#ifdef WIN64EXCEPTIONS
    // Save the raw entry
    PTR_RUNTIME_FUNCTION RawFunctionEntry = pRuntimeFunctions + MethodIndex;

    MethodDesc *pMethodDesc;
    while ((pMethodDesc = pInfo->GetMethodDescForEntryPoint(ImageBase + RUNTIME_FUNCTION__BeginAddress(pRuntimeFunctions + MethodIndex))) == NULL)
        MethodIndex--;
#endif

    PTR_RUNTIME_FUNCTION FunctionEntry = pRuntimeFunctions + MethodIndex;

    if (ppMethodDesc)
    {
#ifdef WIN64EXCEPTIONS
        *ppMethodDesc = pMethodDesc;
#else
        *ppMethodDesc = pInfo->GetMethodDescForEntryPoint(ImageBase + RUNTIME_FUNCTION__BeginAddress(FunctionEntry));
#endif
        _ASSERTE(*ppMethodDesc != NULL);
    }

    if (pCodeInfo)
    {
        pCodeInfo->m_relOffset = (DWORD)
            (RelativePc - RUNTIME_FUNCTION__BeginAddress(FunctionEntry));

        // We are using RUNTIME_FUNCTION as METHODTOKEN
        pCodeInfo->m_methodToken = METHODTOKEN(pRangeSection, dac_cast<TADDR>(FunctionEntry));

#ifdef WIN64EXCEPTIONS
        AMD64_ONLY(_ASSERTE((RawFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0));
        pCodeInfo->m_pFunctionEntry = RawFunctionEntry;
#endif
    }

    return TRUE;
}

#if defined(WIN64EXCEPTIONS)
PTR_RUNTIME_FUNCTION ReadyToRunJitManager::LazyGetFunctionEntry(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!pCodeInfo->IsValid())
    {
        return NULL;
    }

    // code:ReadyToRunJitManager::JitCodeToMethodInfo computes PTR_RUNTIME_FUNCTION eagerly. This path is only 
    // reachable via EECodeInfo::GetMainFunctionInfo, and so we can just return the main entry.
    _ASSERTE(pCodeInfo->GetRelOffset() == 0);

    return dac_cast<PTR_RUNTIME_FUNCTION>(pCodeInfo->GetMethodToken().m_pCodeHeader);
}

TADDR ReadyToRunJitManager::GetFuncletStartAddress(EECodeInfo * pCodeInfo)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // READYTORUN: FUTURE: Hot-cold spliting

    return IJitManager::GetFuncletStartAddress(pCodeInfo);
}

DWORD ReadyToRunJitManager::GetFuncletStartOffsets(const METHODTOKEN& MethodToken, DWORD* pStartFuncletOffsets, DWORD dwLength)
{
    PTR_RUNTIME_FUNCTION pFirstFuncletFunctionEntry = dac_cast<PTR_RUNTIME_FUNCTION>(MethodToken.m_pCodeHeader) + 1;
    
    TADDR moduleBase = JitTokenToModuleBase(MethodToken);
    DWORD nFunclets = 0;
    MethodRegionInfo regionInfo;
    JitTokenToMethodRegionInfo(MethodToken, &regionInfo);

    // pFirstFuncletFunctionEntry will work for ARM when passed to GetFuncletStartOffsetsHelper()
    // even if it is a fragment of the main body and not a RUNTIME_FUNCTION for the beginning
    // of the first hot funclet, because GetFuncletStartOffsetsHelper() will skip all the function
    // fragments until the first funclet, if any, is found.

    GetFuncletStartOffsetsHelper(regionInfo.hotStartAddress, regionInfo.hotSize, 0,
        pFirstFuncletFunctionEntry, moduleBase,
        &nFunclets, pStartFuncletOffsets, dwLength);

    // READYTORUN: FUTURE: Hot/cold splitting

    return nFunclets;
}

BOOL ReadyToRunJitManager::IsFilterFunclet(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pCodeInfo->IsFunclet())
        return FALSE;

    // Get address of the personality routine for the function being queried.
    SIZE_T size;
    PTR_VOID pUnwindData = GetUnwindDataBlob(pCodeInfo->GetModuleBase(), pCodeInfo->GetFunctionEntry(), &size);
    _ASSERTE(pUnwindData != NULL);

    // Personality routine is always the last element of the unwind data
    DWORD rvaPersonalityRoutine = *(dac_cast<PTR_DWORD>(dac_cast<TADDR>(pUnwindData) + size) - 1);

    // Get the personality routine for the first function in the module, which is guaranteed to be not a funclet.
    ReadyToRunInfo * pInfo = JitTokenToReadyToRunInfo(pCodeInfo->GetMethodToken());
    if (pInfo->m_nRuntimeFunctions == 0)
        return FALSE;

    PTR_VOID pFirstUnwindData = GetUnwindDataBlob(pCodeInfo->GetModuleBase(), pInfo->m_pRuntimeFunctions, &size);
    _ASSERTE(pFirstUnwindData != NULL);
    DWORD rvaFirstPersonalityRoutine = *(dac_cast<PTR_DWORD>(dac_cast<TADDR>(pFirstUnwindData) + size) - 1);

    // Compare the two personality routines. If they are different, then the current function is a filter funclet.
    BOOL fRet = (rvaPersonalityRoutine != rvaFirstPersonalityRoutine);

    // Verify that the optimized implementation is in sync with the slow implementation
    _ASSERTE(fRet == IJitManager::IsFilterFunclet(pCodeInfo));

    return fRet;
}

#endif  // WIN64EXCEPTIONS

void ReadyToRunJitManager::JitTokenToMethodRegionInfo(const METHODTOKEN& MethodToken, 
                                                   MethodRegionInfo * methodRegionInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
        PRECONDITION(methodRegionInfo != NULL);
    } CONTRACTL_END;

    // READYTORUN: FUTURE: Hot-cold spliting

    methodRegionInfo->hotStartAddress  = JitTokenToStartAddress(MethodToken);
    methodRegionInfo->hotSize          = GetCodeManager()->GetFunctionSize(GetGCInfo(MethodToken));
    methodRegionInfo->coldStartAddress = 0;
    methodRegionInfo->coldSize         = 0;
}

#ifdef DACCESS_COMPILE

void ReadyToRunJitManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    IJitManager::EnumMemoryRegions(flags);
}

#if defined(WIN64EXCEPTIONS)

//
// EnumMemoryRegionsForMethodUnwindInfo - enumerate the memory necessary to read the unwind info for the
// specified method.
// 
// Note that in theory, a dump generation library could save the unwind information itself without help
// from us, since it's stored in the image in the standard function table layout for Win64.  However, 
// dump-generation libraries assume that the image will be available at debug time, and if the image 
// isn't available then it is acceptable for stackwalking to break.  For ngen images (which are created 
// on the client), it usually isn't possible to have the image available at debug time, and so for minidumps
// we must explicitly ensure the unwind information is saved into the dump. 
// 
// Arguments:
//     flags - EnumMem flags
//     pMD   - MethodDesc for the method in question
//     
void ReadyToRunJitManager::EnumMemoryRegionsForMethodUnwindInfo(CLRDataEnumMemoryFlags flags, EECodeInfo * pCodeInfo)
{
    // Get the RUNTIME_FUNCTION entry for this method 
    PTR_RUNTIME_FUNCTION pRtf = pCodeInfo->GetFunctionEntry();

    if (pRtf==NULL)
    {
        return;
    }

    // Enumerate the function entry and other entries needed to locate it in the program exceptions directory
    ReadyToRunInfo * pReadyToRunInfo = JitTokenToReadyToRunInfo(pCodeInfo->GetMethodToken());
    EnumRuntimeFunctionEntriesToFindEntry(pRtf, pReadyToRunInfo->GetImage());

    SIZE_T size;
    PTR_VOID pUnwindData = GetUnwindDataBlob(pCodeInfo->GetModuleBase(), pRtf, &size);
    if (pUnwindData != NULL)
        DacEnumMemoryRegion(PTR_TO_TADDR(pUnwindData), size); 
}

#endif //WIN64EXCEPTIONS
#endif // #ifdef DACCESS_COMPILE

#endif
