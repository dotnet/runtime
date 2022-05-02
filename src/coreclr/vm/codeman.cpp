// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// codeman.cpp - a managment class for handling multiple code managers
//

//

#include "common.h"
#include "jitinterface.h"
#include "corjit.h"
#include "jithost.h"
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

#include "shimload.h"
#include "debuginfostore.h"
#include "strsafe.h"

#include "configuration.h"

#ifdef HOST_64BIT
#define CHECK_DUPLICATED_STRUCT_LAYOUTS
#include "../debug/daccess/fntableaccess.h"
#endif // HOST_64BIT

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

// Default number of jump stubs in a jump stub block
#define DEFAULT_JUMPSTUBS_PER_BLOCK  32

SPTR_IMPL(EECodeManager, ExecutionManager, m_pDefaultCodeMan);

SPTR_IMPL(EEJitManager, ExecutionManager, m_pEEJitManager);
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

unsigned   ExecutionManager::m_normal_JumpStubLookup;
unsigned   ExecutionManager::m_normal_JumpStubUnique;
unsigned   ExecutionManager::m_normal_JumpStubBlockAllocCount;
unsigned   ExecutionManager::m_normal_JumpStubBlockFullCount;

unsigned   ExecutionManager::m_LCG_JumpStubLookup;
unsigned   ExecutionManager::m_LCG_JumpStubUnique;
unsigned   ExecutionManager::m_LCG_JumpStubBlockAllocCount;
unsigned   ExecutionManager::m_LCG_JumpStubBlockFullCount;

#endif // DACCESS_COMPILE

#if defined(TARGET_AMD64) && !defined(DACCESS_COMPILE) // We don't do this on ARM just amd64

// Support for new style unwind information (to allow OS to stack crawl JIT compiled code).

typedef NTSTATUS (WINAPI* RtlAddGrowableFunctionTableFnPtr) (
        PVOID *DynamicTable, PRUNTIME_FUNCTION FunctionTable, ULONG EntryCount,
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
        PVOID *DynamicTable, PT_RUNTIME_FUNCTION FunctionTable, ULONG EntryCount,
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

#ifndef TARGET_UNIX
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
#else // !TARGET_UNIX
    return false;
#endif // !TARGET_UNIX
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
    pTable = new T_RUNTIME_FUNCTION[cTableMaxCount];
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
void UnwindInfoTable::AddToUnwindInfoTable(UnwindInfoTable** unwindInfoPtr, PT_RUNTIME_FUNCTION data,
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

            STRESS_LOG5(LF_JIT, LL_INFO1000, "AddToUnwindTable Handle: %p [%p, %p] ADDING 0x%p TO END, now 0x%x entries\n",
                unwindInfo->hHandle, unwindInfo->iRangeStart, unwindInfo->iRangeEnd,
                data->BeginAddress, unwindInfo->cTableCurCount);
            return;
        }
    }

    // OK we need to rellocate the table and reregister.  First figure out our 'desiredSpace'
    // We could imagine being much more efficient for 'bulk' updates, but we don't try
    // because we assume that this is rare and we want to keep the code simple

    ULONG usedSpace = unwindInfo->cTableCurCount - unwindInfo->cDeletedEntries;
    ULONG desiredSpace = usedSpace * 5 / 4 + 1;        // Increase by 20%
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

/* static */ void UnwindInfoTable::PublishUnwindInfoForMethod(TADDR baseAddress, PT_RUNTIME_FUNCTION unwindInfo, int unwindInfoCount)
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
        EEJitManager::CodeHeapIterator heapIterator(NULL);

        // Currently m_CodeHeapCritSec is given the CRST_UNSAFE_ANYMODE flag which allows it to be taken in a GC_NOTRIGGER
        // region but also disallows GC_TRIGGERS.  We need GC_TRIGGERS because we take another lock.   Ideally we would
        // fix m_CodeHeapCritSec to not have the CRST_UNSAFE_ANYMODE flag, but I currently reached my threshold for fixing
        // contracts.
        CONTRACT_VIOLATION(GCViolation);

        while(heapIterator.Next())
        {
            MethodDesc *pMD = heapIterator.GetMethod();
            if(pMD)
            {
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

#endif // defined(TARGET_AMD64) && !defined(DACCESS_COMPILE)

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
RemoveCodeHeapFromDomainList
DeleteCodeHeap
AddRangeToJitHeapCache
DeleteJitHeapCache

*/


#if !defined(DACCESS_COMPILE)
EEJitManager::CodeHeapIterator::CodeHeapIterator(LoaderAllocator *pLoaderAllocatorFilter)
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

            // LoaderAllocator filter
            if (m_pLoaderAllocator && m_pCurrent)
            {
                LoaderAllocator *pCurrentLoaderAllocator = m_pCurrent->GetLoaderAllocator();
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

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
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
#if defined(TARGET_ARM)

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
#elif defined(TARGET_ARM64)

    // ARM64 is a little bit more flexible, in the sense that it supports partial prologs. However only one of the
    // prolog regions are allowed to alter SP and that's the Host Record. Partial prologs are used in ShrinkWrapping
    // scenarios which is not supported, hence we don't need to worry about them. discarding partial prologs
    // simplifies identifying a host record a lot.
    //
    // 1. Prolog only: The host record. Epilog Count and E bit are all 0.
    // 2. Prolog and some epilogs: The host record with accompanying epilog-only records
    // 3. Epilogs only: First unwind code is Phantom prolog (Starting with an end_c, indicating an empty prolog)
    // 4. No prologs or epilogs: First unwind code is Phantom prolog  (Starting with an end_c, indicating an empty prolog)
    //

    int EpilogCount = (int)(unwindHeader >> 22) & 0x1F;
    int CodeWords = unwindHeader >> 27;
    PTR_DWORD pUnwindCodes = (PTR_DWORD)(baseAddress + pFunctionEntry->UnwindData);
    // Skip header.
    pUnwindCodes++;

    // Skip extended header.
    if ((CodeWords == 0) && (EpilogCount == 0))
    {
        EpilogCount = (*pUnwindCodes) & 0xFFFF;
        pUnwindCodes++;
    }

    // Skip epilog scopes.
    BOOL Ebit = (unwindHeader >> 21) & 0x1;
    if (!Ebit && (EpilogCount != 0))
    {
        // EpilogCount is the number of exception scopes defined right after the unwindHeader
        pUnwindCodes += EpilogCount;
    }

    return ((*pUnwindCodes & 0xFF) == 0xE5);
#else
    PORTABILITY_ASSERT("IsFunctionFragnent - NYI on this platform");
#endif
}

// When we have fragmented unwind we usually want to refer to the
// unwind record that includes the prolog. We can find it by searching
// back in the sequence of unwind records.
PTR_RUNTIME_FUNCTION FindRootEntry(PTR_RUNTIME_FUNCTION pFunctionEntry, TADDR baseAddress)
{
    LIMITED_METHOD_DAC_CONTRACT;

    PTR_RUNTIME_FUNCTION pRootEntry = pFunctionEntry;

    if (pRootEntry != NULL)
    {
        // Walk backwards in the RUNTIME_FUNCTION array until we find a non-fragment.
        // We're guaranteed to find one, because we require that a fragment live in a function or funclet
        // that has a prolog, which will have non-fragment .xdata.
        for (;;)
        {
            if (!IsFunctionFragment(baseAddress, pRootEntry))
            {
                // This is not a fragment; we're done
                break;
            }

            --pRootEntry;
        }
    }

    return pRootEntry;
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
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

#if !defined(DACCESS_COMPILE)


    Thread *pThread = GetThreadNULLOk();

    if (!pThread)
        return ScanNoReaderLock;

    // If this thread is hijacked by a profiler and crawling its own stack,
    // we do need to take the lock
    if (pThread->GetProfilerFilterContext() != NULL)
        return ScanReaderLock;

    if (pThread->PreemptiveGCDisabled() || (pThread == ThreadSuspend::GetSuspensionThread()))
        return ScanNoReaderLock;



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

#if defined(FEATURE_EH_FUNCLETS)

PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ SIZE_T * pSize)
{
    LIMITED_METHOD_CONTRACT;

#if defined(TARGET_AMD64)
    PTR_UNWIND_INFO pUnwindInfo(dac_cast<PTR_UNWIND_INFO>(moduleBase + RUNTIME_FUNCTION__GetUnwindInfoAddress(pRuntimeFunction)));

    *pSize = ALIGN_UP(offsetof(UNWIND_INFO, UnwindCode) +
        sizeof(UNWIND_CODE) * pUnwindInfo->CountOfUnwindCodes +
        sizeof(ULONG) /* personality routine is always present */,
            sizeof(DWORD));

    return pUnwindInfo;

#elif defined(TARGET_X86)
    PTR_UNWIND_INFO pUnwindInfo(dac_cast<PTR_UNWIND_INFO>(moduleBase + RUNTIME_FUNCTION__GetUnwindInfoAddress(pRuntimeFunction)));

    *pSize = sizeof(UNWIND_INFO);

    return pUnwindInfo;

#elif defined(TARGET_ARM) || defined(TARGET_ARM64)

    // if this function uses packed unwind data then at least one of the two least significant bits
    // will be non-zero.  if this is the case then there will be no xdata record to enumerate.
    _ASSERTE((pRuntimeFunction->UnwindData & 0x3) == 0);

    // compute the size of the unwind info
    PTR_DWORD xdata = dac_cast<PTR_DWORD>(pRuntimeFunction->UnwindData + moduleBase);
    int size = 4;

#if defined(TARGET_ARM)
    // See https://docs.microsoft.com/en-us/cpp/build/arm-exception-handling
    int unwindWords = xdata[0] >> 28;
    int epilogScopes = (xdata[0] >> 23) & 0x1f;
#else
    // See https://docs.microsoft.com/en-us/cpp/build/arm64-exception-handling
    int unwindWords = xdata[0] >> 27;
    int epilogScopes = (xdata[0] >> 22) & 0x1f;
#endif

    if (unwindWords == 0 && epilogScopes == 0)
    {
        size += 4;
        unwindWords = (xdata[1] >> 16) & 0xff;
        epilogScopes = xdata[1] & 0xffff;
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

#ifdef TARGET_AMD64
    _ASSERTE((pFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0);
#endif

    TADDR baseAddress = pCodeInfo->GetModuleBase();

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS)
    pFunctionEntry = FindRootEntry(pFunctionEntry, baseAddress);
#endif // EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS

    TADDR funcletStartAddress = baseAddress + RUNTIME_FUNCTION__BeginAddress(pFunctionEntry);

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

#else // FEATURE_EH_FUNCLETS

PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ SIZE_T * pSize)
{
    *pSize = 0;
    return dac_cast<PTR_VOID>(pRuntimeFunction->UnwindData + moduleBase);
}

#endif // FEATURE_EH_FUNCLETS



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
    m_CPUCompileFlags(),
    m_JitLoadCritSec( CrstSingleUseLock )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_pCodeHeap = NULL;
    m_jit = NULL;
    m_JITCompiler      = NULL;
#ifdef TARGET_AMD64
    m_pEmergencyJumpStubReserveList = NULL;
#endif
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    m_JITCompilerOther = NULL;
#endif

#ifdef ALLOW_SXS_JIT
    m_alternateJit     = NULL;
    m_AltJITCompiler   = NULL;
    m_AltJITRequired   = false;
#endif

    m_cleanupList = NULL;
}

#if defined(TARGET_X86) || defined(TARGET_AMD64)

bool DoesOSSupportAVX()
{
    LIMITED_METHOD_CONTRACT;

#ifndef TARGET_UNIX
    // On Windows we have an api(GetEnabledXStateFeatures) to check if AVX is supported
    typedef DWORD64 (WINAPI *PGETENABLEDXSTATEFEATURES)();
    PGETENABLEDXSTATEFEATURES pfnGetEnabledXStateFeatures = NULL;

    HMODULE hMod = WszLoadLibraryEx(WINDOWS_KERNEL32_DLLNAME_W, NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if(hMod == NULL)
        return FALSE;

    pfnGetEnabledXStateFeatures = (PGETENABLEDXSTATEFEATURES)GetProcAddress(hMod, "GetEnabledXStateFeatures");

    if (pfnGetEnabledXStateFeatures == NULL)
    {
        return FALSE;
    }

    DWORD64 FeatureMask = pfnGetEnabledXStateFeatures();
    if ((FeatureMask & XSTATE_MASK_AVX) == 0)
    {
        return FALSE;
    }
#endif // !TARGET_UNIX

    return TRUE;
}

#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

#ifdef TARGET_ARM64
extern "C" DWORD64 __stdcall GetDataCacheZeroIDReg();
#endif

void EEJitManager::SetCpuInfo()
{
    LIMITED_METHOD_CONTRACT;

    //
    // NOTE: This function needs to be kept in sync with compSetProcesor() in jit\compiler.cpp
    //

    CORJIT_FLAGS CPUCompileFlags;

#if defined(TARGET_X86)
    CORINFO_CPU cpuInfo;
    GetSpecificCpuInfo(&cpuInfo);

    switch (CPU_X86_FAMILY(cpuInfo.dwCPUType))
    {
        case CPU_X86_PENTIUM_4:
            CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_TARGET_P4);
            break;

        default:
            break;
    }

    if (CPU_X86_USE_CMOV(cpuInfo.dwFeatures))
    {
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_CMOV);
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_FCOMI);
    }
#endif // TARGET_X86

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    CPUCompileFlags.Set(InstructionSet_X86Base);

    // NOTE: The below checks are based on the information reported by
    //   Intel® 64 and IA-32 Architectures Software Developer’s Manual. Volume 2
    //   and
    //   AMD64 Architecture Programmer’s Manual. Volume 3
    // For more information, please refer to the CPUID instruction in the respective manuals

    // We will set the following flags:
    //   CORJIT_FLAG_USE_SSE2 is required
    //      SSE       - EDX bit 25
    //      SSE2      - EDX bit 26
    //   CORJIT_FLAG_USE_AES
    //      CORJIT_FLAG_USE_SSE2
    //      AES       - ECX bit 25
    //   CORJIT_FLAG_USE_PCLMULQDQ
    //      CORJIT_FLAG_USE_SSE2
    //      PCLMULQDQ - ECX bit 1
    //   CORJIT_FLAG_USE_SSE3 if the following feature bits are set (input EAX of 1)
    //      CORJIT_FLAG_USE_SSE2
    //      SSE3      - ECX bit 0
    //   CORJIT_FLAG_USE_SSSE3 if the following feature bits are set (input EAX of 1)
    //      CORJIT_FLAG_USE_SSE3
    //      SSSE3     - ECX bit 9
    //   CORJIT_FLAG_USE_SSE41 if the following feature bits are set (input EAX of 1)
    //      CORJIT_FLAG_USE_SSSE3
    //      SSE4.1    - ECX bit 19
    //   CORJIT_FLAG_USE_SSE42 if the following feature bits are set (input EAX of 1)
    //      CORJIT_FLAG_USE_SSE41
    //      SSE4.2    - ECX bit 20
    //   CORJIT_FLAG_USE_POPCNT if the following feature bits are set (input EAX of 1)
    //      CORJIT_FLAG_USE_SSE42
    //      POPCNT    - ECX bit 23
    //   CORJIT_FLAG_USE_AVX if the following feature bits are set (input EAX of 1), and xmmYmmStateSupport returns 1:
    //      CORJIT_FLAG_USE_SSE42
    //      OSXSAVE   - ECX bit 27
    //      AVX       - ECX bit 28
    //      XGETBV    - XCR0[2:1]    11b
    //   CORJIT_FLAG_USE_FMA if the following feature bits are set (input EAX of 1), and xmmYmmStateSupport returns 1:
    //      CORJIT_FLAG_USE_AVX
    //      FMA       - ECX bit 12
    //   CORJIT_FLAG_USE_AVX2 if the following feature bit is set (input EAX of 0x07 and input ECX of 0):
    //      CORJIT_FLAG_USE_AVX
    //      AVX2      - EBX bit 5
    //   CORJIT_FLAG_USE_AVXVNNI if the following feature bit is set (input EAX of 0x07 and input ECX of 1):
    //      CORJIT_FLAG_USE_AVX2
    //      AVXVNNI   - EAX bit 4
    //   CORJIT_FLAG_USE_AVX_512 is not currently set, but defined so that it can be used in future without
    //   CORJIT_FLAG_USE_BMI1 if the following feature bit is set (input EAX of 0x07 and input ECX of 0):
    //      BMI1 - EBX bit 3
    //   CORJIT_FLAG_USE_BMI2 if the following feature bit is set (input EAX of 0x07 and input ECX of 0):
    //      BMI2 - EBX bit 8
    //   CORJIT_FLAG_USE_LZCNT if the following feature bits are set (input EAX of 80000001H)
    //      LZCNT - ECX bit 5
    // synchronously updating VM and JIT.

    int cpuidInfo[4];

    const int EAX = CPUID_EAX;
    const int EBX = CPUID_EBX;
    const int ECX = CPUID_ECX;
    const int EDX = CPUID_EDX;

    __cpuid(cpuidInfo, 0x00000000);
    uint32_t maxCpuId = static_cast<uint32_t>(cpuidInfo[EAX]);

    if (maxCpuId >= 1)
    {
        __cpuid(cpuidInfo, 0x00000001);

        if (((cpuidInfo[EDX] & (1 << 25)) != 0) && ((cpuidInfo[EDX] & (1 << 26)) != 0))                     // SSE & SSE2
        {
            CPUCompileFlags.Set(InstructionSet_SSE);
            CPUCompileFlags.Set(InstructionSet_SSE2);

            if ((cpuidInfo[ECX] & (1 << 25)) != 0)                                                          // AESNI
            {
                CPUCompileFlags.Set(InstructionSet_AES);
            }

            if ((cpuidInfo[ECX] & (1 << 1)) != 0)                                                           // PCLMULQDQ
            {
                CPUCompileFlags.Set(InstructionSet_PCLMULQDQ);
            }

            if ((cpuidInfo[ECX] & (1 << 0)) != 0)                                                           // SSE3
            {
                CPUCompileFlags.Set(InstructionSet_SSE3);

                if ((cpuidInfo[ECX] & (1 << 9)) != 0)                                                       // SSSE3
                {
                    CPUCompileFlags.Set(InstructionSet_SSSE3);

                    if ((cpuidInfo[ECX] & (1 << 19)) != 0)                                                  // SSE4.1
                    {
                        CPUCompileFlags.Set(InstructionSet_SSE41);

                        if ((cpuidInfo[ECX] & (1 << 20)) != 0)                                              // SSE4.2
                        {
                            CPUCompileFlags.Set(InstructionSet_SSE42);

                            if ((cpuidInfo[ECX] & (1 << 23)) != 0)                                          // POPCNT
                            {
                                CPUCompileFlags.Set(InstructionSet_POPCNT);
                            }

                            if (((cpuidInfo[ECX] & (1 << 27)) != 0) && ((cpuidInfo[ECX] & (1 << 28)) != 0)) // OSXSAVE & AVX
                            {
                                if(DoesOSSupportAVX() && (xmmYmmStateSupport() == 1))                       // XGETBV == 11
                                {
                                    CPUCompileFlags.Set(InstructionSet_AVX);

                                    if ((cpuidInfo[ECX] & (1 << 12)) != 0)                                  // FMA
                                    {
                                        CPUCompileFlags.Set(InstructionSet_FMA);
                                    }

                                    if (maxCpuId >= 0x07)
                                    {
                                        __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

                                        if ((cpuidInfo[EBX] & (1 << 5)) != 0)                               // AVX2
                                        {
                                            CPUCompileFlags.Set(InstructionSet_AVX2);

                                            __cpuidex(cpuidInfo, 0x00000007, 0x00000001);
                                            if ((cpuidInfo[EAX] & (1 << 4)) != 0)                           // AVX-VNNI
                                            {
                                                CPUCompileFlags.Set(InstructionSet_AVXVNNI);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SIMD16ByteOnly) != 0)
            {
                CPUCompileFlags.Clear(InstructionSet_AVX2);
            }
        }

        if (maxCpuId >= 0x07)
        {
            __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

            if ((cpuidInfo[EBX] & (1 << 3)) != 0)                                                           // BMI1
            {
                CPUCompileFlags.Set(InstructionSet_BMI1);
            }

            if ((cpuidInfo[EBX] & (1 << 8)) != 0)                                                           // BMI2
            {
                CPUCompileFlags.Set(InstructionSet_BMI2);
            }
        }
    }

    __cpuid(cpuidInfo, 0x80000000);
    uint32_t maxCpuIdEx = static_cast<uint32_t>(cpuidInfo[EAX]);

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, 0x80000001);

        if ((cpuidInfo[ECX] & (1 << 5)) != 0)                                                               // LZCNT
        {
            CPUCompileFlags.Set(InstructionSet_LZCNT);
        }
    }

    if (!CPUCompileFlags.IsSet(InstructionSet_SSE))
    {
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("SSE is not supported on the processor."));
    }
    if (!CPUCompileFlags.IsSet(InstructionSet_SSE2))
    {
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("SSE2 is not supported on the processor."));
    }
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

#if defined(TARGET_ARM64)
#if defined(TARGET_UNIX)
    PAL_GetJitCpuCapabilityFlags(&CPUCompileFlags);

    // For HOST_ARM64, if OS has exposed mechanism to detect CPU capabilities, make sure it has AdvSimd capability.
    // For other cases i.e. if !HOST_ARM64 but TARGET_ARM64 or HOST_ARM64 but OS doesn't expose way to detect
    // CPU capabilities, we always enable AdvSimd flags by default.
    //
    if (!CPUCompileFlags.IsSet(InstructionSet_AdvSimd))
    {
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("AdvSimd is not supported on the processor."));
    }
#elif defined(HOST_64BIT)
    // FP and SIMD support are enabled by default
    CPUCompileFlags.Set(InstructionSet_ArmBase);
    CPUCompileFlags.Set(InstructionSet_AdvSimd);

    // PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE (30)
    if (IsProcessorFeaturePresent(PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE))
    {
        CPUCompileFlags.Set(InstructionSet_Aes);
        CPUCompileFlags.Set(InstructionSet_Sha1);
        CPUCompileFlags.Set(InstructionSet_Sha256);
    }
    // PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE (31)
    if (IsProcessorFeaturePresent(PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE))
    {
        CPUCompileFlags.Set(InstructionSet_Crc32);
    }
#endif // HOST_64BIT
    if (GetDataCacheZeroIDReg() == 4)
    {
        // DCZID_EL0<4> (DZP) indicates whether use of DC ZVA instructions is permitted (0) or prohibited (1).
        // DCZID_EL0<3:0> (BS) specifies Log2 of the block size in words.
        //
        // We set the flag when the instruction is permitted and the block size is 64 bytes.
        CPUCompileFlags.Set(InstructionSet_Dczva);
    }
#endif // TARGET_ARM64

    // Now that we've queried the actual hardware support, we need to adjust what is actually supported based
    // on some externally available config switches that exist so users can test code for downlevel hardware.

#if defined(TARGET_AMD64) || defined(TARGET_X86)
    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableHWIntrinsic))
    {
        CPUCompileFlags.Clear(InstructionSet_X86Base);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableAES))
    {
        CPUCompileFlags.Clear(InstructionSet_AES);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableAVX))
    {
        CPUCompileFlags.Clear(InstructionSet_AVX);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableAVX2))
    {
        CPUCompileFlags.Clear(InstructionSet_AVX2);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableAVXVNNI))
    {
        CPUCompileFlags.Clear(InstructionSet_AVXVNNI);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableBMI1))
    {
        CPUCompileFlags.Clear(InstructionSet_BMI1);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableBMI2))
    {
        CPUCompileFlags.Clear(InstructionSet_BMI2);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableFMA))
    {
        CPUCompileFlags.Clear(InstructionSet_FMA);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableLZCNT))
    {
        CPUCompileFlags.Clear(InstructionSet_LZCNT);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnablePCLMULQDQ))
    {
        CPUCompileFlags.Clear(InstructionSet_PCLMULQDQ);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnablePOPCNT))
    {
        CPUCompileFlags.Clear(InstructionSet_POPCNT);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE))
    {
        CPUCompileFlags.Clear(InstructionSet_SSE);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE2))
    {
        CPUCompileFlags.Clear(InstructionSet_SSE2);
    }

    // We need to additionally check that EXTERNAL_EnableSSE3_4 is set, as that
    // is a prexisting config flag that controls the SSE3+ ISAs
    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE3) || !CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE3_4))
    {
        CPUCompileFlags.Clear(InstructionSet_SSE3);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE41))
    {
        CPUCompileFlags.Clear(InstructionSet_SSE41);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSE42))
    {
        CPUCompileFlags.Clear(InstructionSet_SSE42);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableSSSE3))
    {
        CPUCompileFlags.Clear(InstructionSet_SSSE3);
    }
#elif defined(TARGET_ARM64)
    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableHWIntrinsic))
    {
        CPUCompileFlags.Clear(InstructionSet_ArmBase);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64AdvSimd))
    {
        CPUCompileFlags.Clear(InstructionSet_AdvSimd);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Aes))
    {
        CPUCompileFlags.Clear(InstructionSet_Aes);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Atomics))
    {
        CPUCompileFlags.Clear(InstructionSet_Atomics);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Rcpc))
    {
        CPUCompileFlags.Clear(InstructionSet_Rcpc);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Crc32))
    {
        CPUCompileFlags.Clear(InstructionSet_Crc32);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Dczva))
    {
        CPUCompileFlags.Clear(InstructionSet_Dczva);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Dp))
    {
        CPUCompileFlags.Clear(InstructionSet_Dp);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Rdm))
    {
        CPUCompileFlags.Clear(InstructionSet_Rdm);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Sha1))
    {
        CPUCompileFlags.Clear(InstructionSet_Sha1);
    }

    if (!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableArm64Sha256))
    {
        CPUCompileFlags.Clear(InstructionSet_Sha256);
    }
#endif

    // These calls are very important as it ensures the flags are consistent with any
    // removals specified above. This includes removing corresponding 64-bit ISAs
    // and any other implications such as SSE2 depending on SSE or AdvSimd on ArmBase

    CPUCompileFlags.Set64BitInstructionSetVariants();
    CPUCompileFlags.EnsureValidInstructionSetSupport();

    m_CPUCompileFlags = CPUCompileFlags;
}

// Define some data that we can use to get a better idea of what happened when we get a Watson dump that indicates the JIT failed to load.
// This will be used and updated by the JIT loading and initialization functions, and the data written will get written into a Watson dump.

enum JIT_LOAD_JIT_ID
{
    JIT_LOAD_MAIN = 500,    // The "main" JIT. Normally, this is named "clrjit.dll". Start at a number that is somewhat uncommon (i.e., not zero or 1) to help distinguish from garbage, in process dumps.
    // 501 is JIT_LOAD_LEGACY on some platforms; please do not reuse this value.
    JIT_LOAD_ALTJIT = 502   // An "altjit". By default, named something like "clrjit_<targetos>_<target_arch>_<host_arch>.dll". Used both internally, as well as externally for JIT CTP builds.
};

enum JIT_LOAD_STATUS
{
    JIT_LOAD_STATUS_STARTING = 1001,                   // The JIT load process is starting. Start at a number that is somewhat uncommon (i.e., not zero or 1) to help distinguish from garbage, in process dumps.
    JIT_LOAD_STATUS_DONE_LOAD,                         // LoadLibrary of the JIT dll succeeded.
    JIT_LOAD_STATUS_DONE_GET_JITSTARTUP,               // GetProcAddress for "jitStartup" succeeded.
    JIT_LOAD_STATUS_DONE_CALL_JITSTARTUP,              // Calling jitStartup() succeeded.
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

//  Validate that the name used to load the JIT is just a simple file name
//  and does not contain something that could be used in a non-qualified path.
//  For example, using the string "..\..\..\myjit.dll" we might attempt to
//  load a JIT from the root of the drive.
//
//  The minimal set of characters that we must check for and exclude are:
//     '\\' - (backslash)
//     '/'  - (forward slash)
//     ':'  - (colon)
//
//  Returns false if we find any of these characters in 'pwzJitName'
//  Returns true if we reach the null terminator without encountering
//  any of these characters.
//
static bool ValidateJitName(LPCWSTR pwzJitName)
{
    LPCWSTR pCurChar = pwzJitName;
    wchar_t curChar;
    do {
        curChar = *pCurChar;
        if ((curChar == '\\') || (curChar == '/') || (curChar == ':'))
        {
            //  Return false if we find any of these character in 'pwzJitName'
            return false;
        }
        pCurChar++;
    } while (curChar != 0);

    //  Return true; we have reached the null terminator
    //
    return true;
}

CORINFO_OS getClrVmOs();

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
// targetOs          - Target OS for JIT
//

static void LoadAndInitializeJIT(LPCWSTR pwzJitName, OUT HINSTANCE* phJit, OUT ICorJitCompiler** ppICorJitCompiler, IN OUT JIT_LOAD_DATA* pJitLoadData, CORINFO_OS targetOs)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(phJit != NULL);
    _ASSERTE(ppICorJitCompiler != NULL);
    _ASSERTE(pJitLoadData != NULL);

    pJitLoadData->jld_status = JIT_LOAD_STATUS_STARTING;
    pJitLoadData->jld_hr     = S_OK;

    *phJit = NULL;
    *ppICorJitCompiler = NULL;

    if (pwzJitName == nullptr)
    {
        pJitLoadData->jld_hr = E_FAIL;
        LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: pwzJitName is null"));
        return;
    }

    HRESULT hr = E_FAIL;

    if (ValidateJitName(pwzJitName))
    {
        // Load JIT from next to CoreCLR binary
        PathString CoreClrFolderHolder;
        if (GetClrModulePathName(CoreClrFolderHolder) && !CoreClrFolderHolder.IsEmpty())
        {
            SString::Iterator iter = CoreClrFolderHolder.End();
            BOOL findSep = CoreClrFolderHolder.FindBack(iter, DIRECTORY_SEPARATOR_CHAR_W);
            if (findSep)
            {
                SString sJitName(pwzJitName);
                CoreClrFolderHolder.Replace(iter + 1, CoreClrFolderHolder.End() - (iter + 1), sJitName);

                *phJit = CLRLoadLibrary(CoreClrFolderHolder.GetUnicode());
                if (*phJit != NULL)
                {
                    hr = S_OK;
                }
            }
        }
    }
    else
    {
        LOG((LF_JIT, LL_FATALERROR, "LoadAndInitializeJIT: invalid characters in %S\n", pwzJitName));
    }

    if (SUCCEEDED(hr))
    {
        pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_LOAD;

        EX_TRY
        {
            typedef void (* pjitStartup)(ICorJitHost*);
            pjitStartup jitStartupFn = (pjitStartup) GetProcAddress(*phJit, "jitStartup");

            if (jitStartupFn)
            {
                pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_GET_JITSTARTUP;

                (*jitStartupFn)(JitHost::getJitHost());

                pJitLoadData->jld_status = JIT_LOAD_STATUS_DONE_CALL_JITSTARTUP;
            }

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

                        // Specify to the JIT that it is working with the OS that we are compiled against
                        pICorJitCompiler->setTargetOS(targetOs);

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
EXTERN_C void jitStartup(ICorJitHost* host);
EXTERN_C ICorJitCompiler* getJit();
#endif // FEATURE_MERGE_JIT_AND_ENGINE

BOOL EEJitManager::LoadJIT()
{
    STANDARD_VM_CONTRACT;

    // If the JIT is already loaded, don't take the lock.
    if (IsJitLoaded())
        return TRUE;

    // Use m_JitLoadCritSec to ensure that the JIT is loaded on one thread only
    CrstHolder chRead(&m_JitLoadCritSec);

    // Did someone load the JIT before we got the lock?
    if (IsJitLoaded())
        return TRUE;

    SetCpuInfo();

    ICorJitCompiler* newJitCompiler = NULL;

#ifdef FEATURE_MERGE_JIT_AND_ENGINE

    EX_TRY
    {
        jitStartup(JitHost::getJitHost());

        newJitCompiler = getJit();

        // We don't need to call getVersionIdentifier(), since the JIT is linked together with the VM.
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

#else // !FEATURE_MERGE_JIT_AND_ENGINE

    m_JITCompiler = NULL;
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    m_JITCompilerOther = NULL;
#endif

    g_JitLoadData.jld_id = JIT_LOAD_MAIN;
    LoadAndInitializeJIT(ExecutionManager::GetJitName(), &m_JITCompiler, &newJitCompiler, &g_JitLoadData, getClrVmOs());
#endif // !FEATURE_MERGE_JIT_AND_ENGINE

#ifdef ALLOW_SXS_JIT

    // Do not load altjit.dll unless COMPlus_AltJit is set.
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
#ifdef TARGET_WINDOWS
#ifdef TARGET_X86
            altJitName = MAKEDLLNAME_W(W("clrjit_win_x86_x86"));
#elif defined(TARGET_AMD64)
            altJitName = MAKEDLLNAME_W(W("clrjit_win_x64_x64"));
#endif
#else // TARGET_WINDOWS
#ifdef TARGET_X86
            altJitName = MAKEDLLNAME_W(W("clrjit_unix_x86_x86"));
#elif defined(TARGET_AMD64)
            altJitName = MAKEDLLNAME_W(W("clrjit_unix_x64_x64"));
#endif
#endif // TARGET_WINDOWS

#if defined(TARGET_ARM)
            altJitName = MAKEDLLNAME_W(W("clrjit_universal_arm_arm"));
#elif defined(TARGET_ARM64)
            altJitName = MAKEDLLNAME_W(W("clrjit_universal_arm64_arm64"));
#endif // TARGET_ARM
        }

        CORINFO_OS targetOs = getClrVmOs();
        LPWSTR altJitOsConfig;
        IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AltJitOs, &altJitOsConfig));
        if (altJitOsConfig != NULL)
        {
            // We have some inconsistency all over the place with osx vs macos, let's handle both here
            if ((_wcsicmp(altJitOsConfig, W("macos")) == 0) || (_wcsicmp(altJitOsConfig, W("osx")) == 0))
            {
                targetOs = CORINFO_MACOS;
            }
            else if ((_wcsicmp(altJitOsConfig, W("linux")) == 0) || (_wcsicmp(altJitOsConfig, W("unix")) == 0))
            {
                targetOs = CORINFO_UNIX;
            }
            else if (_wcsicmp(altJitOsConfig, W("windows")) == 0)
            {
                targetOs = CORINFO_WINNT;
            }
            else
            {
                _ASSERTE(!"Unknown AltJitOS, it has to be either Windows, Linux or macOS");
            }
        }
        g_JitLoadData.jld_id = JIT_LOAD_ALTJIT;
        LoadAndInitializeJIT(altJitName, &m_AltJITCompiler, &newAltJitCompiler, &g_JitLoadData, targetOs);
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
     CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The new "nothrow" below failure is handled in a non-fault way, so
    // make sure that callers with FORBID_FAULT can call this method without
    // firing the contract violation assert.
    PERMANENT_CONTRACT_VIOLATION(FaultViolation, ReasonContractInfrastructure);

    FreeBlock * pBlock = new (nothrow) FreeBlock;
    // In the OOM case we don't add the block to the list of free blocks
    // as we are in a FORBID_FAULT code path.
    if (pBlock != NULL)
    {
        pBlock->m_pNext = m_pFreeBlocks;
        pBlock->m_pBlock = pMem;
        pBlock->m_dwSize = dwSize;
        m_pFreeBlocks = pBlock;
    }
}

void CodeFragmentHeap::RemoveBlock(FreeBlock ** ppBlock)
{
    LIMITED_METHOD_CONTRACT;
    FreeBlock * pBlock = *ppBlock;
    *ppBlock = pBlock->m_pNext;
    delete pBlock;
}

CodeFragmentHeap::~CodeFragmentHeap()
{
    FreeBlock* pBlock = m_pFreeBlocks;
    while (pBlock != NULL)
    {
        FreeBlock *pNextBlock = pBlock->m_pNext;
        delete pBlock;
        pBlock = pNextBlock;
    }
}

TaggedMemAllocPtr CodeFragmentHeap::RealAllocAlignedMem(size_t  dwRequestedSize
                    ,unsigned  dwAlignment
#ifdef _DEBUG
                    ,_In_ _In_z_ const char *szFile
                    ,int  lineNum
#endif
                    )
{
    CrstHolder ch(&m_CritSec);

    dwRequestedSize = ALIGN_UP(dwRequestedSize, sizeof(TADDR));

    // We will try to batch up allocation of small blocks into one large allocation
#define SMALL_BLOCK_THRESHOLD 0x100
    SIZE_T nFreeSmallBlocks = 0;

    FreeBlock ** ppBestFit = NULL;
    FreeBlock ** ppFreeBlock = &m_pFreeBlocks;
    while (*ppFreeBlock != NULL)
    {
        FreeBlock * pFreeBlock = *ppFreeBlock;
        if (((BYTE *)pFreeBlock->m_pBlock + pFreeBlock->m_dwSize) - (BYTE *)ALIGN_UP(pFreeBlock->m_pBlock, dwAlignment) >= (SSIZE_T)dwRequestedSize)
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
        pMem = (*ppBestFit)->m_pBlock;
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
                    , _In_ _In_z_ const char *szFile
                    , int lineNum
                    , _In_ _In_z_ const char *szAllocFile
                    , int allocLineNum
#endif
                    )
{
    CrstHolder ch(&m_CritSec);

    {
        ExecutableWriterHolder<BYTE> memWriterHolder((BYTE*)pMem, dwSize);
        ZeroMemory(memWriterHolder.GetRW(), dwSize);
    }

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

//**************************************************************************

LoaderCodeHeap::LoaderCodeHeap()
    : m_LoaderHeap(NULL,                    // RangeList *pRangeList
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

    // Allow breaking into debugger or terminating the process when this exception occurs
    switch (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnOutOfMemoryWithinRange))
    {
    case 1:
        DebugBreak();
        break;
    case 2:
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_OUTOFMEMORY);
        break;
    default:
        break;
    }

    EX_THROW(EEMessageException, (kOutOfMemoryException, IDS_EE_OUT_OF_MEMORY_WITHIN_RANGE));
}

#ifdef TARGET_AMD64
BYTE * EEJitManager::AllocateFromEmergencyJumpStubReserve(const BYTE * loAddr, const BYTE * hiAddr, SIZE_T * pReserveSize)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
    } CONTRACTL_END;

    for (EmergencyJumpStubReserve ** ppPrev = &m_pEmergencyJumpStubReserveList; *ppPrev != NULL; ppPrev = &(*ppPrev)->m_pNext)
    {
        EmergencyJumpStubReserve * pList = *ppPrev;

        if (loAddr <= pList->m_ptr &&
            pList->m_ptr + pList->m_size < hiAddr)
        {
            *ppPrev = pList->m_pNext;

            BYTE * pBlock = pList->m_ptr;
            *pReserveSize = pList->m_size;

            delete pList;

            return pBlock;
        }
    }

    return NULL;
}

VOID EEJitManager::EnsureJumpStubReserve(BYTE * pImageBase, SIZE_T imageSize, SIZE_T reserveSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    CrstHolder ch(&m_CodeHeapCritSec);

    BYTE * loAddr = pImageBase + imageSize + INT32_MIN;
    if (loAddr > pImageBase) loAddr = NULL; // overflow

    BYTE * hiAddr = pImageBase + INT32_MAX;
    if (hiAddr < pImageBase) hiAddr = (BYTE *)UINT64_MAX; // overflow

    for (EmergencyJumpStubReserve * pList = m_pEmergencyJumpStubReserveList; pList != NULL; pList = pList->m_pNext)
    {
        if (loAddr <= pList->m_ptr &&
            pList->m_ptr + pList->m_size < hiAddr)
        {
            SIZE_T used = min(reserveSize, pList->m_free);
            pList->m_free -= used;

            reserveSize -= used;
            if (reserveSize == 0)
                return;
        }
    }

    // Try several different strategies - the most efficient one first
    int allocMode = 0;

    // Try to reserve at least 16MB at a time
    SIZE_T allocChunk = max(ALIGN_UP(reserveSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY), 16*1024*1024);

    while (reserveSize > 0)
    {
        NewHolder<EmergencyJumpStubReserve> pNewReserve(new EmergencyJumpStubReserve());

        for (;;)
        {
            BYTE * loAddrCurrent = loAddr;
            BYTE * hiAddrCurrent = hiAddr;

            switch (allocMode)
            {
            case 0:
                // First, try to allocate towards the center of the allowed range. It is more likely to
                // satisfy subsequent reservations.
                loAddrCurrent = loAddr + (hiAddr - loAddr) / 8;
                hiAddrCurrent = hiAddr - (hiAddr - loAddr) / 8;
                break;
            case 1:
                // Try the whole allowed range
                break;
            case 2:
                // If the large allocation failed, retry with small chunk size
                allocChunk = VIRTUAL_ALLOC_RESERVE_GRANULARITY;
                break;
            default:
                return; // Unable to allocate the reserve - give up
            }

            pNewReserve->m_ptr = (BYTE*)ExecutableAllocator::Instance()->ReserveWithinRange(allocChunk, loAddrCurrent, hiAddrCurrent);

            if (pNewReserve->m_ptr != NULL)
                break;

            // Retry with the next allocation strategy
            allocMode++;
        }

        SIZE_T used = min(allocChunk, reserveSize);
        reserveSize -= used;

        pNewReserve->m_size = allocChunk;
        pNewReserve->m_free = allocChunk - used;

        // Add it to the list
        pNewReserve->m_pNext = m_pEmergencyJumpStubReserveList;
        m_pEmergencyJumpStubReserveList = pNewReserve.Extract();
    }
}
#endif // TARGET_AMD64

static size_t GetDefaultReserveForJumpStubs(size_t codeHeapSize)
{
    LIMITED_METHOD_CONTRACT;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    //
    // Keep a small default reserve at the end of the codeheap for jump stubs. It should reduce
    // chance that we won't be able allocate jump stub because of lack of suitable address space.
    //
    static ConfigDWORD configCodeHeapReserveForJumpStubs;
    int percentReserveForJumpStubs = configCodeHeapReserveForJumpStubs.val(CLRConfig::INTERNAL_CodeHeapReserveForJumpStubs);

    size_t reserveForJumpStubs = percentReserveForJumpStubs * (codeHeapSize / 100);

    size_t minReserveForJumpStubs = sizeof(CodeHeader) +
        sizeof(JumpStubBlockHeader) + (size_t) DEFAULT_JUMPSTUBS_PER_BLOCK * BACK_TO_BACK_JUMP_ALLOCATE_SIZE +
        CODE_SIZE_ALIGN + BYTES_PER_BUCKET;

    return max(reserveForJumpStubs, minReserveForJumpStubs);
#else
    return 0;
#endif
}

HeapList* LoaderCodeHeap::CreateCodeHeap(CodeHeapRequestInfo *pInfo, LoaderHeap *pJitMetaHeap)
{
    CONTRACT(HeapList *) {
        THROWS;
        GC_NOTRIGGER;
        POSTCONDITION((RETVAL != NULL) || !pInfo->getThrowOnOutOfMemoryWithinRange());
    } CONTRACT_END;

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

    LOG((LF_JIT, LL_INFO100,
         "Request new LoaderCodeHeap::CreateCodeHeap(%08x, %08x, for loader allocator" FMT_ADDR "in" FMT_ADDR ".." FMT_ADDR ")\n",
         (DWORD) reserveSize, (DWORD) initialRequestSize, DBG_ADDR(pInfo->m_pAllocator), DBG_ADDR(loAddr), DBG_ADDR(hiAddr)
                                ));

    NewHolder<LoaderCodeHeap> pCodeHeap(new LoaderCodeHeap());

    BYTE * pBaseAddr = NULL;
    DWORD dwSizeAcquiredFromInitialBlock = 0;
    bool fAllocatedFromEmergencyJumpStubReserve = false;

    pBaseAddr = (BYTE *)pInfo->m_pAllocator->GetCodeHeapInitialBlock(loAddr, hiAddr, (DWORD)initialRequestSize, &dwSizeAcquiredFromInitialBlock);
    if (pBaseAddr != NULL)
    {
        pCodeHeap->m_LoaderHeap.SetReservedRegion(pBaseAddr, dwSizeAcquiredFromInitialBlock, FALSE);
    }
    else
    {
        if (loAddr != NULL || hiAddr != NULL)
        {
#ifdef _DEBUG
            // Always exercise the fallback path in the caller when forced relocs are turned on
            if (!pInfo->getThrowOnOutOfMemoryWithinRange() && PEDecoder::GetForceRelocs())
                RETURN NULL;
#endif
            pBaseAddr = (BYTE*)ExecutableAllocator::Instance()->ReserveWithinRange(reserveSize, loAddr, hiAddr);

            if (!pBaseAddr)
            {
                // Conserve emergency jump stub reserve until when it is really needed
                if (!pInfo->getThrowOnOutOfMemoryWithinRange())
                    RETURN NULL;
#ifdef TARGET_AMD64
                pBaseAddr = ExecutionManager::GetEEJitManager()->AllocateFromEmergencyJumpStubReserve(loAddr, hiAddr, &reserveSize);
                if (!pBaseAddr)
                    ThrowOutOfMemoryWithinRange();
                fAllocatedFromEmergencyJumpStubReserve = true;
#else
                ThrowOutOfMemoryWithinRange();
#endif // TARGET_AMD64
            }
        }
        else
        {
            pBaseAddr = (BYTE*)ExecutableAllocator::Instance()->Reserve(reserveSize);
            if (!pBaseAddr)
                ThrowOutOfMemory();
        }
        pCodeHeap->m_LoaderHeap.SetReservedRegion(pBaseAddr, reserveSize, TRUE);
    }


    // this first allocation is critical as it sets up correctly the loader heap info
    HeapList *pHp = new HeapList;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    pHp->CLRPersonalityRoutine = (BYTE *)pCodeHeap->m_LoaderHeap.AllocMem(JUMP_ALLOCATE_SIZE);
#else
    // Ensure that the heap has a reserved block of memory and so the GetReservedBytesFree()
    // and GetAllocPtr() calls below return nonzero values.
    pCodeHeap->m_LoaderHeap.ReservePages(1);
#endif

    pHp->pHeap = pCodeHeap;

    size_t heapSize = pCodeHeap->m_LoaderHeap.GetReservedBytesFree();
    size_t nibbleMapSize = HEAP2MAPSIZE(ROUND_UP_TO_PAGE(heapSize));

    pHp->startAddress = (TADDR)pCodeHeap->m_LoaderHeap.GetAllocPtr();

    pHp->endAddress      = pHp->startAddress;
    pHp->maxCodeHeapSize = heapSize;
    pHp->reserveForJumpStubs = fAllocatedFromEmergencyJumpStubReserve ? pHp->maxCodeHeapSize : GetDefaultReserveForJumpStubs(pHp->maxCodeHeapSize);

    _ASSERTE(heapSize >= initialRequestSize);

    // We do not need to memset this memory, since ClrVirtualAlloc() guarantees that the memory is zero.
    // Furthermore, if we avoid writing to it, these pages don't come into our working set

    pHp->mapBase         = ROUND_DOWN_TO_PAGE(pHp->startAddress);  // round down to next lower page align
    pHp->pHdrMap         = (DWORD*)(void*)pJitMetaHeap->AllocMem(S_SIZE_T(nibbleMapSize));

    LOG((LF_JIT, LL_INFO100,
         "Created new CodeHeap(" FMT_ADDR ".." FMT_ADDR ")\n",
         DBG_ADDR(pHp->startAddress), DBG_ADDR(pHp->startAddress+pHp->maxCodeHeapSize)
         ));

#ifdef TARGET_64BIT
    ExecutableWriterHolder<BYTE> personalityRoutineWriterHolder(pHp->CLRPersonalityRoutine, 12);
    emitJump(pHp->CLRPersonalityRoutine, personalityRoutineWriterHolder.GetRW(), (void *)ProcessCLRException);
#endif // TARGET_64BIT

    pCodeHeap.SuppressRelease();
    RETURN pHp;
}

void * LoaderCodeHeap::AllocMemForCode_NoThrow(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (m_cbMinNextPad > (SSIZE_T)header) header = m_cbMinNextPad;

    void * p = m_LoaderHeap.AllocMemForCode_NoThrow(header, size, alignment, reserveForJumpStubs);
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
        m_pAllocator = m_pMD->GetLoaderAllocator();
    m_isDynamicDomain = (m_pMD != NULL) && m_pMD->IsLCGMethod();
    m_isCollectible = m_pAllocator->IsCollectible();
    m_throwOnOutOfMemoryWithinRange = true;
}

#ifdef FEATURE_EH_FUNCLETS

#ifdef HOST_64BIT
extern "C" PT_RUNTIME_FUNCTION GetRuntimeFunctionCallback(IN ULONG64   ControlPc,
                                                        IN PVOID     Context)
#else
extern "C" PT_RUNTIME_FUNCTION GetRuntimeFunctionCallback(IN ULONG     ControlPc,
                                                        IN PVOID     Context)
#endif
{
    WRAPPER_NO_CONTRACT;

    PT_RUNTIME_FUNCTION prf = NULL;

    // We must preserve this so that GCStress=4 eh processing doesnt kill last error.
    BEGIN_PRESERVE_LAST_ERROR;

#ifdef ENABLE_CONTRACTS
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

    END_PRESERVE_LAST_ERROR;

    return  prf;
}
#endif // FEATURE_EH_FUNCLETS

HeapList* EEJitManager::NewCodeHeap(CodeHeapRequestInfo *pInfo, DomainCodeHeapList *pADHeapList)
{
    CONTRACT(HeapList *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
        POSTCONDITION((RETVAL != NULL) || !pInfo->getThrowOnOutOfMemoryWithinRange());
    } CONTRACT_END;

    size_t initialRequestSize = pInfo->getRequestSize();
    size_t minReserveSize = VIRTUAL_ALLOC_RESERVE_GRANULARITY; //     ( 64 KB)

#ifdef HOST_64BIT
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
            minReserveSize *= 8; // CodeHeaps are larger on AMD64 (256 KB to 2048 KB)
        }
    }
#endif

    size_t reserveSize = initialRequestSize;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    reserveSize += JUMP_ALLOCATE_SIZE;
#endif

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
    if (pHp == NULL)
    {
        _ASSERTE(!pInfo->getThrowOnOutOfMemoryWithinRange());
        RETURN(NULL);
    }

    _ASSERTE (pHp != NULL);
    _ASSERTE (pHp->maxCodeHeapSize >= initialRequestSize);

    pHp->SetNext(GetCodeHeapList());

    EX_TRY
    {
        TADDR pStartRange = pHp->GetModuleBase();
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

        delete pHp->pHeap;
        delete pHp;

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
                                 HeapList ** ppCodeHeap)
{
    CONTRACT(void *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_CodeHeapCritSec.OwnedByCurrentThread());
        POSTCONDITION((RETVAL != NULL) || !pInfo->getThrowOnOutOfMemoryWithinRange());
    } CONTRACT_END;

    pInfo->setRequestSize(header+blockSize+(align-1)+pInfo->getReserveForJumpStubs());

    void *      mem         = NULL;
    HeapList * pCodeHeap    = NULL;
    DomainCodeHeapList *pList = NULL;

    // Avoid going through the full list in the common case - try to use the most recently used codeheap
    if (pInfo->IsDynamicDomain())
    {
        pCodeHeap = (HeapList *)pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap;
        pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap = NULL;
    }
    else
    {
        pCodeHeap = (HeapList *)pInfo->m_pAllocator->m_pLastUsedCodeHeap;
        pInfo->m_pAllocator->m_pLastUsedCodeHeap = NULL;
    }

    // If we will use a cached code heap, ensure that the code heap meets the constraints
    if (pCodeHeap && CanUseCodeHeap(pInfo, pCodeHeap))
    {
        mem = (pCodeHeap->pHeap)->AllocMemForCode_NoThrow(header, blockSize, align, pInfo->getReserveForJumpStubs());
    }

    if (mem == NULL)
    {
        pList = GetCodeHeapList(pInfo, pInfo->m_pAllocator);
        if (pList != NULL)
        {
            for (int i = 0; i < pList->m_CodeHeapList.Count(); i++)
            {
                pCodeHeap = pList->m_CodeHeapList[i];

                // Validate that the code heap can be used for the current request
                if (CanUseCodeHeap(pInfo, pCodeHeap))
                {
                    mem = (pCodeHeap->pHeap)->AllocMemForCode_NoThrow(header, blockSize, align, pInfo->getReserveForJumpStubs());
                    if (mem != NULL)
                        break;
                }
            }
        }

        if (mem == NULL)
        {
            // Let us create a new heap.
            if (pList == NULL)
            {
                // not found so need to create the first one
                pList = CreateCodeHeapList(pInfo);
                _ASSERTE(pList == GetCodeHeapList(pInfo, pInfo->m_pAllocator));
            }
            _ASSERTE(pList);

            pCodeHeap = NewCodeHeap(pInfo, pList);
            if (pCodeHeap == NULL)
            {
                _ASSERTE(!pInfo->getThrowOnOutOfMemoryWithinRange());
                RETURN(NULL);
            }

            mem = (pCodeHeap->pHeap)->AllocMemForCode_NoThrow(header, blockSize, align, pInfo->getReserveForJumpStubs());
            if (mem == NULL)
                ThrowOutOfMemory();
            _ASSERTE(mem);
        }
    }

    if (pInfo->IsDynamicDomain())
    {
        pInfo->m_pAllocator->m_pLastUsedDynamicCodeHeap = pCodeHeap;
    }
    else
    {
        pInfo->m_pAllocator->m_pLastUsedCodeHeap = pCodeHeap;
    }

    // Record the pCodeHeap value into ppCodeHeap
    *ppCodeHeap = pCodeHeap;

    _ASSERTE((TADDR)mem >= pCodeHeap->startAddress);

    if (((TADDR) mem)+blockSize > (TADDR)pCodeHeap->endAddress)
    {
        // Update the CodeHeap endAddress
        pCodeHeap->endAddress = (TADDR)mem+blockSize;
    }

    RETURN(mem);
}

void EEJitManager::allocCode(MethodDesc* pMD, size_t blockSize, size_t reserveForJumpStubs, CorJitAllocMemFlag flag, CodeHeader** ppCodeHeader, CodeHeader** ppCodeHeaderRW,
                             size_t* pAllocatedSize, HeapList** ppCodeHeap
#ifdef USE_INDIRECT_CODEHEADER
                           , BYTE** ppRealHeader
#endif
#ifdef FEATURE_EH_FUNCLETS
                           , UINT nUnwindInfos
#endif
                           )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    //
    // Alignment
    //

    unsigned alignment = CODE_SIZE_ALIGN;

    if ((flag & CORJIT_ALLOCMEM_FLG_32BYTE_ALIGN) != 0)
    {
        alignment = max(alignment, 32);
    }
    else if ((flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN) != 0)
    {
        alignment = max(alignment, 16);
    }

#if defined(TARGET_X86)
    // when not optimizing for code size, 8-byte align the method entry point, so that
    // the JIT can in turn 8-byte align the loop entry headers.
    else if ((g_pConfig->GenOptimizeType() != OPT_SIZE))
    {
        alignment = max(alignment, 8);
    }
#endif

    //
    // Compute header layout
    //

    SIZE_T totalSize = blockSize;

    CodeHeader * pCodeHdr = NULL;
    CodeHeader * pCodeHdrRW = NULL;

    CodeHeapRequestInfo requestInfo(pMD);
#if defined(FEATURE_JIT_PITCHING)
    if (pMD && pMD->IsPitchable() && CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMethodSizeThreshold) < blockSize)
    {
        requestInfo.SetDynamicDomain();
    }
#endif
    requestInfo.setReserveForJumpStubs(reserveForJumpStubs);

#if defined(USE_INDIRECT_CODEHEADER)
    SIZE_T realHeaderSize = offsetof(RealCodeHeader, unwindInfos[0]) + (sizeof(T_RUNTIME_FUNCTION) * nUnwindInfos);

    // if this is a LCG method then we will be allocating the RealCodeHeader
    // following the code so that the code block can be removed easily by
    // the LCG code heap.
    if (requestInfo.IsDynamicDomain())
    {
        totalSize = ALIGN_UP(totalSize, sizeof(void*)) + realHeaderSize;
        static_assert_no_msg(CODE_SIZE_ALIGN >= sizeof(void*));
    }
#endif  // USE_INDIRECT_CODEHEADER

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        *ppCodeHeap = NULL;
        TADDR pCode = (TADDR) allocCodeRaw(&requestInfo, sizeof(CodeHeader), totalSize, alignment, ppCodeHeap);
        _ASSERTE(*ppCodeHeap);

        if (pMD->IsLCGMethod())
        {
            pMD->AsDynamicMethodDesc()->GetLCGMethodResolver()->m_recordCodePointer = (void*) pCode;
        }

        _ASSERTE(IS_ALIGNED(pCode, alignment));

        pCodeHdr = ((CodeHeader *)pCode) - 1;

        *pAllocatedSize = sizeof(CodeHeader) + totalSize;

        if (ExecutableAllocator::IsWXORXEnabled())
        {
            pCodeHdrRW = (CodeHeader *)new BYTE[*pAllocatedSize];
        }
        else
        {
            pCodeHdrRW = pCodeHdr;
        }

#ifdef USE_INDIRECT_CODEHEADER
        if (requestInfo.IsDynamicDomain())
        {
            // Set the real code header to the writeable mapping so that we can set its members via the CodeHeader methods below
            pCodeHdrRW->SetRealCodeHeader((BYTE *)(pCodeHdrRW + 1) + ALIGN_UP(blockSize, sizeof(void*)));
        }
        else
        {
            // TODO: think about the CodeHeap carrying around a RealCodeHeader chunking mechanism
            //
            // allocate the real header in the low frequency heap
            BYTE* pRealHeader = (BYTE*)(void*)pMD->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(realHeaderSize));
            pCodeHdrRW->SetRealCodeHeader(pRealHeader);
        }
#endif

        pCodeHdrRW->SetDebugInfo(NULL);
        pCodeHdrRW->SetEHInfo(NULL);
        pCodeHdrRW->SetGCInfo(NULL);
        pCodeHdrRW->SetMethodDesc(pMD);
#ifdef FEATURE_EH_FUNCLETS
        pCodeHdrRW->SetNumberOfUnwindInfos(nUnwindInfos);
#endif

#ifdef USE_INDIRECT_CODEHEADER
        if (requestInfo.IsDynamicDomain())
        {
            *ppRealHeader = (BYTE*)pCode + ALIGN_UP(blockSize, sizeof(void*));
        }
        else
        {
            *ppRealHeader = NULL;
        }
#endif // USE_INDIRECT_CODEHEADER
    }

    *ppCodeHeader = pCodeHdr;
    *ppCodeHeaderRW = pCodeHdrRW;
}

EEJitManager::DomainCodeHeapList *EEJitManager::GetCodeHeapList(CodeHeapRequestInfo *pInfo, LoaderAllocator *pAllocator, BOOL fDynamicOnly)
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
    if (fDynamicOnly || (pInfo != NULL && pInfo->IsDynamicDomain()))
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
        // We have no constraint so this non empty heap will be able to satisfy our request
        if (pInfo->IsDynamicDomain())
        {
            _ASSERTE(pCodeHeap->reserveForJumpStubs == 0);
            retVal = true;
        }
        else
        {
            BYTE * lastAddr = (BYTE *) pCodeHeap->startAddress + pCodeHeap->maxCodeHeapSize;

            BYTE * loRequestAddr  = (BYTE *) pCodeHeap->endAddress;
            BYTE * hiRequestAddr = loRequestAddr + pInfo->getRequestSize() + BYTES_PER_BUCKET;
            if (hiRequestAddr <= lastAddr - pCodeHeap->reserveForJumpStubs)
            {
                retVal = true;
            }
        }
    }
    else
    {
        // We also check to see if an allocation in this heap would satisfy
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
            _ASSERTE(pCodeHeap->reserveForJumpStubs == 0);

            // We check to see if every allocation in this heap
            // will satisfy the [loAddr..hiAddr] requirement.
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
            //
            if ((pInfo->m_loAddr <= loRequestAddr)   &&
                (hiRequestAddr   <= pInfo->m_hiAddr))
            {
                // Additionally hiRequestAddr must also be less than or equal to lastAddr.
                // If throwOnOutOfMemoryWithinRange is not set, conserve reserveForJumpStubs until when it is really needed.
                if (hiRequestAddr <= lastAddr - (pInfo->getThrowOnOutOfMemoryWithinRange() ? 0 : pCodeHeap->reserveForJumpStubs))
                {
                    // This heap will be able to satisfy our constraint
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
                                                        LoaderAllocator *pLoaderAllocator,
                                                        bool throwOnOutOfMemoryWithinRange)
{
    CONTRACT(JumpStubBlockHeader *) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(loAddr < hiAddr);
        PRECONDITION(pLoaderAllocator != NULL);
        POSTCONDITION((RETVAL != NULL) || !throwOnOutOfMemoryWithinRange);
    } CONTRACT_END;

    _ASSERTE((sizeof(JumpStubBlockHeader) % CODE_SIZE_ALIGN) == 0);

    size_t blockSize = sizeof(JumpStubBlockHeader) + (size_t) numJumps * BACK_TO_BACK_JUMP_ALLOCATE_SIZE;

    HeapList *pCodeHeap = NULL;
    CodeHeapRequestInfo    requestInfo(pMD, pLoaderAllocator, loAddr, hiAddr);
    requestInfo.setThrowOnOutOfMemoryWithinRange(throwOnOutOfMemoryWithinRange);

    TADDR                  mem;
    ExecutableWriterHolderNoLog<JumpStubBlockHeader> blockWriterHolder;

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        mem = (TADDR) allocCodeRaw(&requestInfo, sizeof(CodeHeader), blockSize, CODE_SIZE_ALIGN, &pCodeHeap);
        if (mem == NULL)
        {
            _ASSERTE(!throwOnOutOfMemoryWithinRange);
            RETURN(NULL);
        }

        // CodeHeader comes immediately before the block
        CodeHeader * pCodeHdr = (CodeHeader *) (mem - sizeof(CodeHeader));
        ExecutableWriterHolder<CodeHeader> codeHdrWriterHolder(pCodeHdr, sizeof(CodeHeader));
        codeHdrWriterHolder.GetRW()->SetStubCodeBlockKind(STUB_CODE_BLOCK_JUMPSTUB);

        NibbleMapSetUnlocked(pCodeHeap, mem, TRUE);

        blockWriterHolder.AssignExecutableWriterHolder((JumpStubBlockHeader *)mem, sizeof(JumpStubBlockHeader));

        _ASSERTE(IS_ALIGNED(blockWriterHolder.GetRW(), CODE_SIZE_ALIGN));
    }

    blockWriterHolder.GetRW()->m_next            = NULL;
    blockWriterHolder.GetRW()->m_used            = 0;
    blockWriterHolder.GetRW()->m_allocated       = numJumps;
    if (pMD && pMD->IsLCGMethod())
        blockWriterHolder.GetRW()->SetHostCodeHeap(static_cast<HostCodeHeap*>(pCodeHeap->pHeap));
    else
        blockWriterHolder.GetRW()->SetLoaderAllocator(pLoaderAllocator);

    LOG((LF_JIT, LL_INFO1000, "Allocated new JumpStubBlockHeader for %d stubs at" FMT_ADDR " in loader allocator " FMT_ADDR "\n",
         numJumps, DBG_ADDR(mem) , DBG_ADDR(pLoaderAllocator) ));

    RETURN((JumpStubBlockHeader*)mem);
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

#ifdef TARGET_AMD64
    // CodeFragments are pretty much always Precodes that may need to be patched with jump stubs at some point in future
    // We will assume the worst case that every FixupPrecode will need to be patched and reserve the jump stubs accordingly
    requestInfo.setReserveForJumpStubs((blockSize / 8) * JUMP_ALLOCATE_SIZE);
#endif

    TADDR                  mem;

    // Scope the lock
    {
        CrstHolder ch(&m_CodeHeapCritSec);

        mem = (TADDR) allocCodeRaw(&requestInfo, sizeof(CodeHeader), blockSize, alignment, &pCodeHeap);

        // CodeHeader comes immediately before the block
        CodeHeader * pCodeHdr = (CodeHeader *) (mem - sizeof(CodeHeader));
        ExecutableWriterHolder<CodeHeader> codeHdrWriterHolder(pCodeHdr, sizeof(CodeHeader));
        codeHdrWriterHolder.GetRW()->SetStubCodeBlockKind(kind);

        NibbleMapSetUnlocked(pCodeHeap, mem, TRUE);

        // Record the jump stub reservation
        pCodeHeap->reserveForJumpStubs += requestInfo.getReserveForJumpStubs();
    }

    RETURN((void *)mem);
}

#endif // !DACCESS_COMPILE


GCInfoToken EEJitManager::GetGCInfoToken(const METHODTOKEN& MethodToken)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // The JIT-ed code always has the current version of GCInfo
    return{ GetCodeHeader(MethodToken)->GetGCInfo(), GCINFO_VERSION };
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

    // CachedTypeHandle's are filled in at JIT time, and not cached when accessed multiple times
    if (HasCachedTypeHandle(pEHClause))
    {
        return TypeHandle::FromPtr(pEHClause->TypeHandle);
    }
    else
    {
        typeTok = pEHClause->ClassToken;
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

    return ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, typeTok, &typeContext,
                                                       ClassLoader::ReturnNullIfNotFound);
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

            // Clear the pointer only if it matches what we are about to free.
            // There can be cases where the JIT is reentered and we JITed the method multiple times.
            if (pResolver->m_recordCodePointer == codeStart)
                pResolver->m_recordCodePointer = NULL;
        }

#if defined(TARGET_AMD64)
        // Remove the unwind information (if applicable)
        UnwindInfoTable::UnpublishUnwindInfoForMethod((TADDR)codeStart);
#endif // defined(TARGET_AMD64)

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

        // Better to just return than AV?
        if (pHp == NULL)
            return;

        NibbleMapSetUnlocked(pHp, (TADDR)(pCHdr + 1), FALSE);
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

    ExecutableAllocator::ResetLazyPreferredRangeHint();
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
    NibbleMapSetUnlocked(pCodeHeap->m_pHeapList, (TADDR)codeStart, FALSE);

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

    _ASSERTE (g_fProcessDetach || (GCHeapUtilities::IsGCInProgress()  && ::IsGCThread()));

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

    _ASSERTE (g_fProcessDetach || (GCHeapUtilities::IsGCInProgress() && ::IsGCThread()));

	// Quick out, don't even take the lock if we have not cleanup to do.
	// This is important because ETW takes the CodeHeapLock when it is doing
	// rundown, and if there are many JIT compiled methods, this can take a while.
	// Because cleanup is called synchronously before a GC, this means GCs get
	// blocked while ETW is doing rundown.   By not taking the lock we avoid
	// this stall most of the time since cleanup is rare, and ETW rundown is rare
	// the likelihood of both is very very rare.
	if (m_cleanupList == NULL)
		return;

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

    DeleteEEFunctionTable((PVOID)pHeapList->GetModuleBase());

    ExecutionManager::DeleteRange((TADDR)pHeapList->GetModuleBase());

    LOG((LF_JIT, LL_INFO100, "DeleteCodeHeap start" FMT_ADDR "end" FMT_ADDR "\n",
                              (const BYTE*)pHeapList->startAddress,
                              (const BYTE*)pHeapList->endAddress     ));

    CodeHeap* pHeap = pHeapList->pHeap;
    delete pHeap;
    delete pHeapList;
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

#ifdef FEATURE_ON_STACK_REPLACEMENT
    BOOL hasFlagByte = TRUE;
#else
    BOOL hasFlagByte = FALSE;
#endif

    // Uncompress. This allocates memory and may throw.
    CompressDebugInfo::RestoreBoundariesAndVars(
        fpNew, pNewData, // allocators
        pDebugInfo,      // input
        pcMap, ppMap,    // output
        pcVars, ppVars,  // output
        hasFlagByte
    );

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

#ifdef FEATURE_ON_STACK_REPLACEMENT
    BOOL hasFlagByte = TRUE;
#else
    BOOL hasFlagByte = FALSE;
#endif

    if (this->GetDebugInfo() != NULL)
    {
        CompressDebugInfo::EnumMemoryRegions(flags, this->GetDebugInfo(), hasFlagByte);
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

#ifdef FEATURE_EH_FUNCLETS
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

    CrstHolder ch(&m_CodeHeapCritSec);
    NibbleMapSetUnlocked(pHp, pCode, bSet);
}

void EEJitManager::NibbleMapSetUnlocked(HeapList * pHp, TADDR pCode, BOOL bSet)
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
}
#endif // !DACCESS_COMPILE

#if defined(FEATURE_EH_FUNCLETS)
// Note: This returns the root unwind record (the one that describes the prolog)
// in cases where there is fragmented unwind.
PTR_RUNTIME_FUNCTION EEJitManager::LazyGetFunctionEntry(EECodeInfo * pCodeInfo)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
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

#if defined(EXCEPTION_DATA_SUPPORTS_FUNCTION_FRAGMENTS) && defined(TARGET_ARM64)
            // If we might have fragmented unwind, and we're on ARM64, make sure
            // to returning the root record, as the trailing records don't have
            // prolog unwind codes.
            pFunctionEntry = FindRootEntry(pFunctionEntry, baseAddress);
#endif

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
                DWORD dwHigh = cbSize / sizeof(T_RUNTIME_FUNCTION);
                DWORD dwMid  = 0;

                while (dwLow <= dwHigh)
                {
                    dwMid = (dwLow + dwHigh) >> 1;
                    taFuncEntry = pExceptionDir + dwMid * sizeof(T_RUNTIME_FUNCTION);
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
#ifdef _TARGET_AMD64_
                        // On amd64, match RtlLookupFunctionEntry behavior by resolving indirect function entries
                        // back to the associated owning function entry.
                        if ((functionEntry.UnwindData & RUNTIME_FUNCTION_INDIRECT) != 0)
                        {
                            DWORD dwRvaOfOwningFunctionEntry = (functionEntry.UnwindData & ~RUNTIME_FUNCTION_INDIRECT);
                            taFuncEntry = peDecoder.GetRvaData(dwRvaOfOwningFunctionEntry);
                            hr = DacReadAll(taFuncEntry, &functionEntry, sizeof(functionEntry), false);
                            if (FAILED(hr))
                            {
                                return;
                            }

                            _ASSERTE((functionEntry.UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0);
                        }
#endif // _TARGET_AMD64_

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
#endif // FEATURE_EH_FUNCLETS

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

    m_pEEJitManager = new EEJitManager();

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
        SUPPORTS_DAC;
    } CONTRACTL_END;

    ReaderLockHolder rlh;
    return GetRangeSection(currentPC);
}


//**************************************************************************
PCODE ExecutionManager::GetCodeStartAddress(PCODE currentPC)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(currentPC != NULL);

    EECodeInfo codeInfo(currentPC);
    if (!codeInfo.IsValid())
        return NULL;
    return PINSTRToPCODE(codeInfo.GetStartAddress());
}

//**************************************************************************
NativeCodeVersion ExecutionManager::GetNativeCodeVersion(PCODE currentPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    EECodeInfo codeInfo(currentPC);
    return codeInfo.IsValid() ? codeInfo.GetNativeCodeVersion() : NativeCodeVersion();
}

//**************************************************************************
MethodDesc * ExecutionManager::GetCodeMethodDesc(PCODE currentPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
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
    } CONTRACTL_END;

    // This may get called for arbitrary code addresses. Note that the lock is
    // taken over the call to JitCodeToMethodInfo too so that nobody pulls out
    // the range section from underneath us.

    RangeSection * pRS = GetRangeSection(currentPC);
    if (pRS == NULL)
        return FALSE;

    if (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP)
    {
        // Typically if we find a Jit Manager we are inside a managed method
        // but on we could also be in a stub, so we check for that
        // as well and we don't consider stub to be real managed code.
        TADDR start = dac_cast<PTR_EEJitManager>(pRS->pjit)->FindMethodCode(pRS, currentPC);
        if (start == NULL)
            return FALSE;
        CodeHeader * pCHdr = PTR_CodeHeader(start - sizeof(CodeHeader));
        if (!pCHdr->IsStubCodeBlock())
            return TRUE;
    }
#ifdef FEATURE_READYTORUN
    else
    if (pRS->flags & RangeSection::RANGE_SECTION_READYTORUN)
    {
        if (dac_cast<PTR_ReadyToRunJitManager>(pRS->pjit)->JitCodeToMethodInfo(pRS, currentPC, NULL, NULL))
            return TRUE;
    }
#endif

    return FALSE;
}

//**************************************************************************
// Assumes that it is safe not to take it the ExecutionManager reader/writer lock
BOOL ExecutionManager::IsReadyToRunCode(PCODE currentPC)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // This may get called for arbitrary code addresses. Note that the lock is
    // taken over the call to JitCodeToMethodInfo too so that nobody pulls out
    // the range section from underneath us.

#ifdef FEATURE_READYTORUN
    RangeSection * pRS = GetRangeSection(currentPC);
    if (pRS != NULL && (pRS->flags & RangeSection::RANGE_SECTION_READYTORUN))
    {
        if (dac_cast<PTR_ReadyToRunJitManager>(pRS->pjit)->JitCodeToMethodInfo(pRS, currentPC, NULL, NULL))
            return TRUE;
    }
#endif

    return FALSE;
}

#ifndef FEATURE_MERGE_JIT_AND_ENGINE
/*********************************************************************/
// This static method returns the name of the jit dll
//
LPCWSTR ExecutionManager::GetJitName()
{
    STANDARD_VM_CONTRACT;

    LPCWSTR  pwzJitName = NULL;

    // Try to obtain a name for the jit library from the env. variable
    IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_JitName, const_cast<LPWSTR *>(&pwzJitName)));

    if (NULL == pwzJitName)
    {
        pwzJitName = MAKEDLLNAME_W(W("clrjit"));
    }

    return pwzJitName;
}
#endif // !FEATURE_MERGE_JIT_AND_ENGINE

RangeSection* ExecutionManager::GetRangeSection(TADDR addr)
{
    CONTRACTL {
        NOTHROW;
        HOST_NOCALLS;
        GC_NOTRIGGER;
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
    if (g_SystemInfo.dwNumberOfProcessors < 4 || !GCHeapUtilities::IsServerHeap() || !GCHeapUtilities::IsGCInProgress())
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
PTR_Module ExecutionManager::FindReadyToRunModule(TADDR currentData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        STATIC_CONTRACT_HOST_CALLS;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef FEATURE_READYTORUN
    ReaderLockHolder rlh;

    RangeSection * pRS = GetRangeSection(currentData);
    if (pRS == NULL)
        return NULL;

    if (pRS->flags & RangeSection::RANGE_SECTION_CODEHEAP)
        return NULL;

    if (pRS->flags & RangeSection::RANGE_SECTION_READYTORUN)
        return dac_cast<PTR_Module>(pRS->pHeapListOrZapModule);;

    return NULL;
#else
    return NULL;
#endif
}


/* static */
PTR_Module ExecutionManager::FindModuleForGCRefMap(TADDR currentData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
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
#if defined(TARGET_AMD64)
    pnewrange->pUnwindInfoTable = NULL;
#endif // defined(TARGET_AMD64)
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
#if defined(TARGET_AMD64)
        if (pCurr->pUnwindInfoTable != 0)
            delete pCurr->pUnwindInfoTable;
#endif // defined(TARGET_AMD64)
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

#if !defined(DACCESS_COMPILE)

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

    JumpStubCache * pJumpStubCache = (JumpStubCache *) pLoaderAllocator->m_pJumpStubCache;
    if (pJumpStubCache != NULL)
    {
        delete pJumpStubCache;
        pLoaderAllocator->m_pJumpStubCache = NULL;
    }

    GetEEJitManager()->Unload(pLoaderAllocator);
}

// This method is used by the JIT and the runtime for PreStubs. It will return
// the address of a short jump thunk that will jump to the 'target' address.
// It is only needed when the target architecture has a perferred call instruction
// that doesn't actually span the full address space.  This is true for x64 where
// the preferred call instruction is a 32-bit pc-rel call instruction.
// (This is also true on ARM64, but it not true for x86)
//
// For these architectures, in JITed code and in the prestub, we encode direct calls
// using the preferred call instruction and we also try to insure that the Jitted
// code is within the 32-bit pc-rel range of clr.dll to allow direct JIT helper calls.
//
// When the call target is too far away to encode using the preferred call instruction.
// We will create a short code thunk that uncoditionally jumps to the target address.
// We call this jump thunk a "jumpStub" in the CLR code.
// We have the requirement that the "jumpStub" that we create on demand be usable by
// the preferred call instruction, this requires that on x64 the location in memory
// where we create the "jumpStub" be within the 32-bit pc-rel range of the call that
// needs it.
//
// The arguments to this method:
//  pMD    - the MethodDesc for the currenty managed method in Jitted code
//           or for the target method for a PreStub
//           It is required if calling from or to a dynamic method (LCG method)
//  target - The call target address (this is the address that was too far to encode)
//  loAddr
//  hiAddr - The range of the address that we must place the jumpStub in, so that it
//           can be used to encode the preferred call instruction.
//  pLoaderAllocator
//         - The Loader allocator to use for allocations, this can be null.
//           When it is null, then the pMD must be valid and is used to obtain
//           the allocator.
//
// This method will either locate and return an existing jumpStub thunk that can be
// reused for this request, because it meets all of the requirements necessary.
// Or it will allocate memory in the required region and create a new jumpStub that
// meets all of the requirements necessary.
//
// Note that for dynamic methods (LCG methods) we cannot share the jumpStubs between
// different methods. This is because we allow for the unloading (reclaiming) of
// individual dynamic methods. And we associate the jumpStub memory allocated with
// the dynamic method that requested the jumpStub.
//

PCODE ExecutionManager::jumpStub(MethodDesc* pMD, PCODE target,
                                 BYTE * loAddr,   BYTE * hiAddr,
                                 LoaderAllocator *pLoaderAllocator,
                                 bool throwOnOutOfMemoryWithinRange)
{
    CONTRACT(PCODE) {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pLoaderAllocator != NULL || pMD != NULL);
        PRECONDITION(loAddr < hiAddr);
        POSTCONDITION((RETVAL != NULL) || !throwOnOutOfMemoryWithinRange);
    } CONTRACT_END;

    PCODE jumpStub = NULL;

    if (pLoaderAllocator == NULL)
    {
        pLoaderAllocator = pMD->GetLoaderAllocator();
    }
    _ASSERTE(pLoaderAllocator != NULL);

    bool                 isLCG          = pMD && pMD->IsLCGMethod();
    LCGMethodResolver *  pResolver      = nullptr;
    JumpStubCache *      pJumpStubCache = (JumpStubCache *) pLoaderAllocator->m_pJumpStubCache;

    if (isLCG)
    {
        pResolver      = pMD->AsDynamicMethodDesc()->GetLCGMethodResolver();
        pJumpStubCache = pResolver->m_pJumpStubCache;
    }

    CrstHolder ch(&m_JumpStubCrst);
    if (pJumpStubCache == NULL)
    {
        pJumpStubCache = new JumpStubCache();
        if (isLCG)
        {
            pResolver->m_pJumpStubCache = pJumpStubCache;
        }
        else
        {
            pLoaderAllocator->m_pJumpStubCache = pJumpStubCache;
        }
    }

    if (isLCG)
    {
        // Increment counter of LCG jump stub lookup attempts
        m_LCG_JumpStubLookup++;
    }
    else
    {
        // Increment counter of normal jump stub lookup attempts
        m_normal_JumpStubLookup++;
    }

    // search for a matching jumpstub in the jumpStubCache
    //
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
    jumpStub = getNextJumpStub(pMD, target, loAddr, hiAddr, pLoaderAllocator, throwOnOutOfMemoryWithinRange); // this statement can throw
    if (jumpStub == NULL)
    {
        _ASSERTE(!throwOnOutOfMemoryWithinRange);
        RETURN(NULL);
    }

    _ASSERTE(((TADDR)loAddr <= jumpStub) && (jumpStub <= (TADDR)hiAddr));

    LOG((LF_JIT, LL_INFO10000, "Add JumpStub to" FMT_ADDR "at" FMT_ADDR "\n",
            DBG_ADDR(target), DBG_ADDR(jumpStub) ));

    RETURN(jumpStub);
}

PCODE ExecutionManager::getNextJumpStub(MethodDesc* pMD, PCODE target,
                                        BYTE * loAddr, BYTE * hiAddr,
                                        LoaderAllocator *pLoaderAllocator,
                                        bool throwOnOutOfMemoryWithinRange)
{
    CONTRACT(PCODE) {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(pLoaderAllocator != NULL);
        PRECONDITION(m_JumpStubCrst.OwnedByCurrentThread());
        POSTCONDITION((RETVAL != NULL) || !throwOnOutOfMemoryWithinRange);
    } CONTRACT_END;

    BYTE *           jumpStub       = NULL;
    BYTE *           jumpStubRW     = NULL;
    bool             isLCG          = pMD && pMD->IsLCGMethod();
    // For LCG we request a small block of 4 jumpstubs, because we can not share them
    // with any other methods and very frequently our method only needs one jump stub.
    // Using 4 gives a request size of (32 + 4*12) or 80 bytes.
    // Also note that request sizes are rounded up to a multiples of 16.
    // The request size is calculated into 'blockSize' in allocJumpStubBlock.
    // For x64 the value of BACK_TO_BACK_JUMP_ALLOCATE_SIZE is 12 bytes
    // and the sizeof(JumpStubBlockHeader) is 32.
    //
    DWORD            numJumpStubs   = isLCG ? 4 : DEFAULT_JUMPSTUBS_PER_BLOCK;
    JumpStubCache *  pJumpStubCache = (JumpStubCache *) pLoaderAllocator->m_pJumpStubCache;

    if (isLCG)
    {
        LCGMethodResolver *  pResolver;
        pResolver      = pMD->AsDynamicMethodDesc()->GetLCGMethodResolver();
        pJumpStubCache = pResolver->m_pJumpStubCache;
    }

    JumpStubBlockHeader ** ppHead   = &(pJumpStubCache->m_pBlocks);
    JumpStubBlockHeader *  curBlock = *ppHead;
    ExecutableWriterHolderNoLog<JumpStubBlockHeader> curBlockWriterHolder;

    // allocate a new jumpstub from 'curBlock' if it is not fully allocated
    //
    while (curBlock)
    {
        _ASSERTE(pLoaderAllocator == (isLCG ? curBlock->GetHostCodeHeap()->GetAllocator() : curBlock->GetLoaderAllocator()));

        if (curBlock->m_used < curBlock->m_allocated)
        {
            jumpStub = (BYTE *) curBlock + sizeof(JumpStubBlockHeader) + ((size_t) curBlock->m_used * BACK_TO_BACK_JUMP_ALLOCATE_SIZE);

            if ((loAddr <= jumpStub) && (jumpStub <= hiAddr))
            {
                // We will update curBlock->m_used at "DONE"
                size_t blockSize = sizeof(JumpStubBlockHeader) + (size_t) numJumpStubs * BACK_TO_BACK_JUMP_ALLOCATE_SIZE;
                curBlockWriterHolder.AssignExecutableWriterHolder(curBlock, blockSize);
                jumpStubRW = (BYTE *)((TADDR)jumpStub + (TADDR)curBlockWriterHolder.GetRW() - (TADDR)curBlock);
                goto DONE;
            }
        }
        curBlock = curBlock->m_next;
    }

    // If we get here then we need to allocate a new JumpStubBlock

    if (isLCG)
    {
#ifdef TARGET_AMD64
        // Note this these values are not requirements, instead we are
        // just confirming the values that are mentioned in the comments.
        _ASSERTE(BACK_TO_BACK_JUMP_ALLOCATE_SIZE == 12);
        _ASSERTE(sizeof(JumpStubBlockHeader) == 32);
#endif

        // Increment counter of LCG jump stub block allocations
        m_LCG_JumpStubBlockAllocCount++;
    }
    else
    {
        // Increment counter of normal jump stub block allocations
        m_normal_JumpStubBlockAllocCount++;
    }

    // allocJumpStubBlock will allocate from the LoaderCodeHeap for normal methods
    // and will allocate from a HostCodeHeap for LCG methods.
    //
    // note that this can throw an OOM exception

    curBlock = ExecutionManager::GetEEJitManager()->allocJumpStubBlock(pMD, numJumpStubs, loAddr, hiAddr, pLoaderAllocator, throwOnOutOfMemoryWithinRange);
    if (curBlock == NULL)
    {
        _ASSERTE(!throwOnOutOfMemoryWithinRange);
        RETURN(NULL);
    }

    curBlockWriterHolder.AssignExecutableWriterHolder(curBlock, sizeof(JumpStubBlockHeader) + ((size_t) (curBlock->m_used + 1) * BACK_TO_BACK_JUMP_ALLOCATE_SIZE));

    jumpStubRW = (BYTE *) curBlockWriterHolder.GetRW() + sizeof(JumpStubBlockHeader) + ((size_t) curBlock->m_used * BACK_TO_BACK_JUMP_ALLOCATE_SIZE);
    jumpStub = (BYTE *) curBlock + sizeof(JumpStubBlockHeader) + ((size_t) curBlock->m_used * BACK_TO_BACK_JUMP_ALLOCATE_SIZE);

    _ASSERTE((loAddr <= jumpStub) && (jumpStub <= hiAddr));

    curBlockWriterHolder.GetRW()->m_next = *ppHead;
    *ppHead = curBlock;

DONE:

    _ASSERTE((curBlock->m_used < curBlock->m_allocated));

#ifdef TARGET_ARM64
    // 8-byte alignment is required on ARM64
    _ASSERTE(((UINT_PTR)jumpStub & 7) == 0);
#endif

    emitBackToBackJump(jumpStub, jumpStubRW, (void*) target);

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "emitBackToBackJump", (PCODE)jumpStub, BACK_TO_BACK_JUMP_ALLOCATE_SIZE);
#endif

    // We always add the new jumpstub to the jumpStubCache
    //
    _ASSERTE(pJumpStubCache != NULL);

    JumpStubEntry entry;

    entry.m_target = target;
    entry.m_jumpStub = (PCODE)jumpStub;

    pJumpStubCache->m_Table.Add(entry);

    curBlockWriterHolder.GetRW()->m_used++;    // record that we have used up one more jumpStub in the block

    // Every time we create a new jumpStub thunk one of these counters is incremented
    if (isLCG)
    {
        // Increment counter of LCG unique jump stubs
        m_LCG_JumpStubUnique++;
    }
    else
    {
        // Increment counter of normal unique jump stubs
        m_normal_JumpStubUnique++;
    }

    // Is the 'curBlock' now completely full?
    if (curBlock->m_used == curBlock->m_allocated)
    {
        if (isLCG)
        {
            // Increment counter of LCG jump stub blocks that are full
            m_LCG_JumpStubBlockFullCount++;

            // Log this "LCG JumpStubBlock filled" along with the four counter values
            STRESS_LOG4(LF_JIT, LL_INFO1000, "LCG JumpStubBlock filled - (%u, %u, %u, %u)\n",
                        m_LCG_JumpStubLookup, m_LCG_JumpStubUnique,
                        m_LCG_JumpStubBlockAllocCount, m_LCG_JumpStubBlockFullCount);
        }
        else
        {
            // Increment counter of normal jump stub blocks that are full
            m_normal_JumpStubBlockFullCount++;

            // Log this "normal JumpStubBlock filled" along with the four counter values
            STRESS_LOG4(LF_JIT, LL_INFO1000, "Normal JumpStubBlock filled - (%u, %u, %u, %u)\n",
                        m_normal_JumpStubLookup, m_normal_JumpStubUnique,
                        m_normal_JumpStubBlockAllocCount, m_normal_JumpStubBlockFullCount);

            if ((m_LCG_JumpStubLookup > 0) && ((m_normal_JumpStubBlockFullCount % 5) == 1))
            {
                // Every 5 occurrence of the above we also
                // Log "LCG JumpStubBlock status" along with the four counter values
                STRESS_LOG4(LF_JIT, LL_INFO1000, "LCG JumpStubBlock status - (%u, %u, %u, %u)\n",
                            m_LCG_JumpStubLookup, m_LCG_JumpStubUnique,
                            m_LCG_JumpStubBlockAllocCount, m_LCG_JumpStubBlockFullCount);
            }
        }
    }

    RETURN((PCODE)jumpStub);
}
#endif // !DACCESS_COMPILE

static void GetFuncletStartOffsetsHelper(PCODE pCodeStart, SIZE_T size, SIZE_T ofsAdj,
    PTR_RUNTIME_FUNCTION pFunctionEntry, TADDR moduleBase,
    DWORD * pnFunclets, DWORD* pStartFuncletOffsets, DWORD dwLength)
{
    _ASSERTE(FitsInU4((pCodeStart + size) - moduleBase));
    DWORD endAddress = (DWORD)((pCodeStart + size) - moduleBase);

    // Entries are sorted and terminated by sentinel value (DWORD)-1
    for (; RUNTIME_FUNCTION__BeginAddress(pFunctionEntry) < endAddress; pFunctionEntry++)
    {
#ifdef TARGET_AMD64
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

#if defined(FEATURE_EH_FUNCLETS) && defined(DACCESS_COMPILE)

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
        (pProgramExceptionsDirectory->Size % sizeof(T_RUNTIME_FUNCTION) != 0))
    {
        // Program exceptions directory malformatted
        return;
    }

    PTR_BYTE moduleBase(pNativeLayout->GetBase());
    PTR_RUNTIME_FUNCTION firstFunctionEntry(moduleBase + pProgramExceptionsDirectory->VirtualAddress);

    if (pRtf < firstFunctionEntry ||
        ((dac_cast<TADDR>(pRtf) - dac_cast<TADDR>(firstFunctionEntry)) % sizeof(T_RUNTIME_FUNCTION) != 0))
    {
        // Program exceptions directory malformatted
        return;
    }

    UINT_PTR indexToLocate = pRtf - firstFunctionEntry;

    UINT_PTR low = 0; // index in the function entry table of low end of search range
    UINT_PTR high = (pProgramExceptionsDirectory->Size) / sizeof(T_RUNTIME_FUNCTION) - 1; // index of high end of search range
    UINT_PTR mid = (low + high) / 2; // index of entry to be compared

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
        mid = (low + high) / 2;
        _ASSERTE(low <= mid && mid <= high);
    }
}
#endif // FEATURE_EH_FUNCLETS

#if defined(FEATURE_READYTORUN)

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
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;


#ifdef TARGET_ARM
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

UINT32 ReadyToRunJitManager::JitTokenToGCInfoVersion(const METHODTOKEN& MethodToken)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    READYTORUN_HEADER * header = JitTokenToReadyToRunInfo(MethodToken)->GetReadyToRunHeader();

    return GCInfoToken::ReadyToRunVersionToGcInfoVersion(header->MajorVersion);
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

GCInfoToken ReadyToRunJitManager::GetGCInfoToken(const METHODTOKEN& MethodToken)
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
    PTR_BYTE gcInfo = dac_cast<PTR_BYTE>(pUnwindData) + nUnwindDataSize;
    UINT32 gcInfoVersion = JitTokenToGCInfoVersion(MethodToken);

    return{ gcInfo, gcInfoVersion };
}

unsigned ReadyToRunJitManager::InitializeEHEnumeration(const METHODTOKEN& MethodToken, EH_CLAUSE_ENUMERATOR* pEnumState)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    ReadyToRunInfo * pReadyToRunInfo = JitTokenToReadyToRunInfo(MethodToken);

    IMAGE_DATA_DIRECTORY * pExceptionInfoDir = pReadyToRunInfo->FindSection(ReadyToRunSectionType::ExceptionInfo);
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
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD rva = (DWORD)(currentPC - pRangeSection->LowAddress);

    PTR_ReadyToRunInfo pReadyToRunInfo = dac_cast<PTR_Module>(pRangeSection->pHeapListOrZapModule)->GetReadyToRunInfo();

    PTR_IMAGE_DATA_DIRECTORY pDelayLoadMethodCallThunksDir = pReadyToRunInfo->GetDelayMethodCallThunksSection();
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
        pcMap, ppMap,    // output
        pcVars, ppVars,  // output
        FALSE);          // no patchpoint info

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

    CompressDebugInfo::EnumMemoryRegions(flags, pDebugInfo, FALSE);
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
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // READYTORUN: FUTURE: Hot-cold spliting

    // If the address is in a thunk, return NULL.
    if (GetStubCodeBlockKind(pRangeSection, currentPC) != STUB_CODE_BLOCK_UNKNOWN)
    {
        return FALSE;
    }

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

    if (ppMethodDesc == NULL && pCodeInfo == NULL)
    {
        // Bail early if caller doesn't care about the MethodDesc or EECodeInfo.
        // Avoiding the method desc lookups below also prevents deadlocks when this
        // is called from IsManagedCode.
        return TRUE;
    }

#ifdef FEATURE_EH_FUNCLETS
    // Save the raw entry
    PTR_RUNTIME_FUNCTION RawFunctionEntry = pRuntimeFunctions + MethodIndex;

    MethodDesc *pMethodDesc;
    while ((pMethodDesc = pInfo->GetMethodDescForEntryPoint(ImageBase + RUNTIME_FUNCTION__BeginAddress(pRuntimeFunctions + MethodIndex))) == NULL)
        MethodIndex--;
#endif

    PTR_RUNTIME_FUNCTION FunctionEntry = pRuntimeFunctions + MethodIndex;

    if (ppMethodDesc)
    {
#ifdef FEATURE_EH_FUNCLETS
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

#ifdef FEATURE_EH_FUNCLETS
        AMD64_ONLY(_ASSERTE((RawFunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0));
        pCodeInfo->m_pFunctionEntry = RawFunctionEntry;
#endif
    }

    return TRUE;
}

#if defined(FEATURE_EH_FUNCLETS)
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

#endif  // FEATURE_EH_FUNCLETS

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
    methodRegionInfo->hotSize          = GetCodeManager()->GetFunctionSize(GetGCInfoToken(MethodToken));
    methodRegionInfo->coldStartAddress = 0;
    methodRegionInfo->coldSize         = 0;
}

#ifdef DACCESS_COMPILE

void ReadyToRunJitManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    IJitManager::EnumMemoryRegions(flags);
}

#if defined(FEATURE_EH_FUNCLETS)

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

#endif //FEATURE_EH_FUNCLETS
#endif // #ifdef DACCESS_COMPILE

#endif
