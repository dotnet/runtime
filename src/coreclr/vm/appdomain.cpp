// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#include "appdomain.hpp"
#include "peimagelayout.inl"
#include "field.h"
#include "strongnameinternal.h"
#include "excep.h"
#include "eeconfig.h"
#include "gcheaputilities.h"
#include "eventtrace.h"
#include "assemblyname.hpp"
#include "eeprofinterfaces.h"
#include "dbginterface.h"
#ifndef DACCESS_COMPILE
#include "eedbginterfaceimpl.h"
#endif
#include "comdynamic.h"
#include "mlinfo.h"
#include "posterror.h"
#include "assemblynative.hpp"
#include "shimload.h"
#include "stringliteralmap.h"
#include "codeman.h"
#include "comcallablewrapper.h"
#include "eventtrace.h"
#include "comdelegate.h"
#include "siginfo.hpp"
#include "typekey.h"
#include "castcache.h"

#include "caparser.h"
#include "ecall.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "mngstdinterfaces.h"
#include "olevariant.h"
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP

#if defined(FEATURE_COMWRAPPERS)
#include "rcwrefcache.h"
#endif // FEATURE_COMWRAPPERS

#include "typeequivalencehash.hpp"

#include "appdomain.inl"
#include "typeparse.h"
#include "threadpoolrequest.h"

#include "nativeoverlapped.h"

#ifndef TARGET_UNIX
#include "dwreport.h"
#endif // !TARGET_UNIX

#include "stringarraylist.h"

#include "../binder/inc/bindertracing.h"
#include "../binder/inc/defaultassemblybinder.h"
#include "../binder/inc/assemblybindercommon.hpp"

// this file handles string conversion errors for itself
#undef  MAKE_TRANSLATIONFAILED

// Define these macro's to do strict validation for jit lock and class
// init entry leaks.  This defines determine if the asserts that
// verify for these leaks are defined or not.  These asserts can
// sometimes go off even if no entries have been leaked so this
// defines should be used with caution.
//
// If we are inside a .cctor when the application shut's down then the
// class init lock's head will be set and this will cause the assert
// to go off.
//
// If we are jitting a method when the application shut's down then
// the jit lock's head will be set causing the assert to go off.

//#define STRICT_CLSINITLOCK_ENTRY_LEAK_DETECTION

static const WCHAR DEFAULT_DOMAIN_FRIENDLY_NAME[] = W("DefaultDomain");
static const WCHAR OTHER_DOMAIN_FRIENDLY_NAME_PREFIX[] = W("Domain");

#define STATIC_OBJECT_TABLE_BUCKET_SIZE 1020

// Statics

SPTR_IMPL(AppDomain, AppDomain, m_pTheAppDomain);
SPTR_IMPL(SystemDomain, SystemDomain, m_pSystemDomain);

#ifndef DACCESS_COMPILE

// Base Domain Statics
CrstStatic          BaseDomain::m_SpecialStaticsCrst;

int                 BaseDomain::m_iNumberOfProcessors = 0;

// System Domain Statics
GlobalStringLiteralMap* SystemDomain::m_pGlobalStringLiteralMap = NULL;

DECLSPEC_ALIGN(16)
static BYTE         g_pSystemDomainMemory[sizeof(SystemDomain)];

CrstStatic          SystemDomain::m_SystemDomainCrst;
CrstStatic          SystemDomain::m_DelayedUnloadCrst;

DWORD               SystemDomain::m_dwLowestFreeIndex        = 0;

// Constructor for the PinnedHeapHandleBucket class.
PinnedHeapHandleBucket::PinnedHeapHandleBucket(PinnedHeapHandleBucket *pNext, DWORD Size, BaseDomain *pDomain)
: m_pNext(pNext)
, m_ArraySize(Size)
, m_CurrentPos(0)
, m_CurrentEmbeddedFreePos(0) // hint for where to start a search for an embedded free item
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDomain));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    PTRARRAYREF HandleArrayObj;

    // Allocate the array in the large object heap.
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
    HandleArrayObj = (PTRARRAYREF)AllocateObjectArray(Size, g_pObjectClass, /* bAllocateInPinnedHeap = */TRUE);

    // Retrieve the pointer to the data inside the array. This is legal since the array
    // is located in the large object heap and is guaranteed not to move.
    m_pArrayDataPtr = (OBJECTREF *)HandleArrayObj->GetDataPtr();

    // Store the array in a strong handle to keep it alive.
    m_hndHandleArray = pDomain->CreateStrongHandle((OBJECTREF)HandleArrayObj);
}


// Destructor for the PinnedHeapHandleBucket class.
PinnedHeapHandleBucket::~PinnedHeapHandleBucket()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_hndHandleArray)
    {
        DestroyStrongHandle(m_hndHandleArray);
        m_hndHandleArray = NULL;
    }
}


// Allocate handles from the bucket.
OBJECTREF *PinnedHeapHandleBucket::AllocateHandles(DWORD nRequested)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(nRequested > 0 && nRequested <= GetNumRemainingHandles());
    _ASSERTE(m_pArrayDataPtr == (OBJECTREF*)((PTRARRAYREF)ObjectFromHandle(m_hndHandleArray))->GetDataPtr());

    // Store the handles in the buffer that was passed in
    OBJECTREF* ret = &m_pArrayDataPtr[m_CurrentPos];
    m_CurrentPos += nRequested;

    return ret;
}

// look for a free item embedded in the table
OBJECTREF *PinnedHeapHandleBucket::TryAllocateEmbeddedFreeHandle()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF pPreallocatedSentinalObject = ObjectFromHandle(g_pPreallocatedSentinelObject);
    _ASSERTE(pPreallocatedSentinalObject  != NULL);

    for (int  i = m_CurrentEmbeddedFreePos; i < m_CurrentPos; i++)
    {
        if (m_pArrayDataPtr[i] == pPreallocatedSentinalObject)
        {
            m_CurrentEmbeddedFreePos = i;
            m_pArrayDataPtr[i] = NULL;
            return &m_pArrayDataPtr[i];
        }
    }

    // didn't find it (we don't bother wrapping around for a full search, it's not worth it to try that hard, we'll get it next time)

    m_CurrentEmbeddedFreePos = 0;
    return NULL;
}

// enumerate the handles in the bucket
void PinnedHeapHandleBucket::EnumStaticGCRefs(promote_func* fn, ScanContext* sc)
{
    for (int i = 0; i < m_CurrentPos; i++)
    {
        fn((Object**)&m_pArrayDataPtr[i], sc, 0);
    }
}


// Maximum bucket size will be 64K on 32-bit and 128K on 64-bit.
// We subtract out a small amount to leave room for the object
// header and length of the array.

#define MAX_BUCKETSIZE (16384 - 4)

// Constructor for the PinnedHeapHandleTable class.
PinnedHeapHandleTable::PinnedHeapHandleTable(BaseDomain *pDomain, DWORD InitialBucketSize)
: m_pHead(NULL)
, m_pDomain(pDomain)
, m_NextBucketSize(InitialBucketSize)
, m_pFreeSearchHint(NULL)
, m_cEmbeddedFree(0)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDomain));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_pCrstDebug = NULL;
#endif
}


// Destructor for the PinnedHeapHandleTable class.
PinnedHeapHandleTable::~PinnedHeapHandleTable()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Delete the buckets.
    while (m_pHead)
    {
        PinnedHeapHandleBucket *pOld = m_pHead;
        m_pHead = pOld->GetNext();
        delete pOld;
    }
}

//*****************************************************************************
//
// LOCKING RULES FOR AllocateHandles() and ReleaseHandles()  12/08/2004
//
//
// These functions are not protected by any locking in this location but rather the callers are
// assumed to be doing suitable locking  for the handle table.  The handle table itself is
// behaving rather like a thread-agnostic collection class -- it doesn't want to know
// much about the outside world and so it is just doing its job with no awareness of
// thread notions.
//
// The instance in question is
// There are two locations you can find a PinnedHeapHandleTable
// 1) there is one in every BaseDomain, it is used to keep track of the static members
//     in that domain
// 2) there is one in the System Domain that is used for the GlobalStringLiteralMap
//
// the one in (2) is not the same as the one that is in the BaseDomain object that corresponds
// to the SystemDomain -- that one is basically stilborn because the string literals don't go
// there and of course the System Domain has no code loaded into it -- only regular
// AppDomains (like Domain 0) actually execute code.  As a result handle tables are in
// practice used either for string literals or for static members but never for both.
// At least not at this writing.
//
// Now it's useful to consider what the locking discipline is for these classes.
//
// ---------
//
// First case: (easiest) is the statics members
//
// Each BaseDomain has its own critical section
//
// BaseDomain::AllocateObjRefPtrsInLargeTable takes a lock with
//        CrstHolder ch(&m_PinnedHeapHandleTableCrst);
//
// it does this before it calls AllocateHandles which suffices.  It does not call ReleaseHandles
// at any time (although ReleaseHandles may be called via AllocateHandles if the request
// doesn't fit in the current block, the remaining handles at the end of the block are released
// automatically as part of allocation/recycling)
//
// note: Recycled handles are only used during String Literal allocation because we only try
// to recycle handles if the allocation request is for exactly one handle.
//
// The handles in the BaseDomain handle table are released when the Domain is unloaded
// as the GC objects become rootless at that time.
//
// This dispenses with all of the Handle tables except the one that is used for string literals
//
// ---------
//
// Second case:  Allocation for use in a string literal
//
// AppDomainStringLiteralMap::GetStringLiteral
// leads to calls to
//     PinnedHeapHandleBlockHolder constructor
//     leads to calls to
//          m_Data = pOwner->AllocateHandles(nCount);
//
// before doing this  AppDomainStringLiteralMap::GetStringLiteral takes this lock
//
//    CrstHolder gch(&(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal));
//
// which is the lock for the hash table that it owns
//
// STRINGREF *AppDomainStringLiteralMap::GetInternedString
//
// has a similar call path and uses the same approach and  the same lock
// this covers all the paths which allocate
//
// ---------
//
// Third case:  Releases for use in a string literal entry
//
// CrstHolder gch(&(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal));
// taken in the AppDomainStringLiteralMap functions below protects the 3 ways that this can happen
//
// case 3a)
//
// AppDomainStringLiteralMap::GetStringLiteral() can call StringLiteralEntry::Release in some
// error cases, leading to the same stack as above
//
// case 3b)
//
// AppDomainStringLiteralMap::GetInternedString() can call StringLiteralEntry::Release in some
// error cases, leading to the same stack as above
//
// case 3c)
//
// The same code paths in 3b and 3c and also end up releasing if an exception is thrown
// during their processing.  Both these paths use a StringLiteralEntryHolder to assist in cleanup,
// the StaticRelease method of the StringLiteralEntry gets called, which in turn calls the
// Release method.


// Allocate handles from the large heap handle table.
OBJECTREF* PinnedHeapHandleTable::AllocateHandles(DWORD nRequested)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(nRequested > 0);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // SEE "LOCKING RULES FOR AllocateHandles() and ReleaseHandles()" above

    // the lock must be registered and already held by the caller per contract
#ifdef _DEBUG
    _ASSERTE(m_pCrstDebug != NULL);
    _ASSERTE(m_pCrstDebug->OwnedByCurrentThread());
#endif

    if (nRequested == 1 && m_cEmbeddedFree != 0)
    {
        // special casing singleton requests to look for slots that can be re-used

        // we need to do this because string literals are allocated one at a time and then sometimes
        // released.  we do not wish for the number of handles consumed by string literals to
        // increase forever as assemblies are loaded and unloaded

        if (m_pFreeSearchHint == NULL)
            m_pFreeSearchHint = m_pHead;

        while (m_pFreeSearchHint)
        {
            OBJECTREF* pObjRef = m_pFreeSearchHint->TryAllocateEmbeddedFreeHandle();
            if (pObjRef != NULL)
            {
                // the slot is to have been prepared with a null ready to go
                _ASSERTE(*pObjRef == NULL);
                m_cEmbeddedFree--;
                return pObjRef;
            }
            m_pFreeSearchHint = m_pFreeSearchHint->GetNext();
        }

        // the search doesn't wrap around so it's possible that we might have embedded free items
        // and not find them but that's ok, we'll get them on the next alloc... all we're trying to do
        // is to not have big leaks over time.
    }


    // Retrieve the remaining number of handles in the bucket.
    DWORD NumRemainingHandlesInBucket = (m_pHead != NULL) ? m_pHead->GetNumRemainingHandles() : 0;

    // create a new block if this request doesn't fit in the current block
    if (nRequested > NumRemainingHandlesInBucket)
    {
        if (m_pHead != NULL)
        {
            // mark the handles in that remaining region as available for re-use
            ReleaseHandles(m_pHead->CurrentPos(), NumRemainingHandlesInBucket);

            // mark what's left as having been used
            m_pHead->ConsumeRemaining();
        }

        // create a new bucket for this allocation

        // We need a block big enough to hold the requested handles
        DWORD NewBucketSize = max(m_NextBucketSize, nRequested);

        m_pHead = new PinnedHeapHandleBucket(m_pHead, NewBucketSize, m_pDomain);

        m_NextBucketSize = min(m_NextBucketSize * 2, MAX_BUCKETSIZE);
    }

    return m_pHead->AllocateHandles(nRequested);
}

//*****************************************************************************
// Release object handles allocated using AllocateHandles().
void PinnedHeapHandleTable::ReleaseHandles(OBJECTREF *pObjRef, DWORD nReleased)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObjRef));
    }
    CONTRACTL_END;

    // SEE "LOCKING RULES FOR AllocateHandles() and ReleaseHandles()" above

    // the lock must be registered and already held by the caller per contract
#ifdef _DEBUG
    _ASSERTE(m_pCrstDebug != NULL);
    _ASSERTE(m_pCrstDebug->OwnedByCurrentThread());
#endif

    OBJECTREF pPreallocatedSentinalObject = ObjectFromHandle(g_pPreallocatedSentinelObject);
    _ASSERTE(pPreallocatedSentinalObject  != NULL);


    // Add the released handles to the list of available handles.
    for (DWORD i = 0; i < nReleased; i++)
    {
        SetObjectReference(&pObjRef[i], pPreallocatedSentinalObject);
    }

    m_cEmbeddedFree += nReleased;
}

// enumerate the handles in the handle table
void PinnedHeapHandleTable::EnumStaticGCRefs(promote_func* fn, ScanContext* sc)
{
    for (PinnedHeapHandleBucket *pBucket = m_pHead; pBucket != nullptr; pBucket = pBucket->GetNext())
    {
        pBucket->EnumStaticGCRefs(fn, sc);
    }
}

// Constructor for the ThreadStaticHandleBucket class.
ThreadStaticHandleBucket::ThreadStaticHandleBucket(ThreadStaticHandleBucket *pNext, DWORD Size, BaseDomain *pDomain)
: m_pNext(pNext)
, m_ArraySize(Size)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDomain));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    PTRARRAYREF HandleArrayObj;

    // Allocate the array on the GC heap.
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
    HandleArrayObj = (PTRARRAYREF)AllocateObjectArray(Size, g_pObjectClass);

    // Store the array in a strong handle to keep it alive.
    m_hndHandleArray = pDomain->CreateStrongHandle((OBJECTREF)HandleArrayObj);
}

// Destructor for the ThreadStaticHandleBucket class.
ThreadStaticHandleBucket::~ThreadStaticHandleBucket()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_hndHandleArray)
    {
        DestroyStrongHandle(m_hndHandleArray);
        m_hndHandleArray = NULL;
    }
}

// Allocate handles from the bucket.
OBJECTHANDLE ThreadStaticHandleBucket::GetHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    return m_hndHandleArray;
}

// Constructor for the ThreadStaticHandleTable class.
ThreadStaticHandleTable::ThreadStaticHandleTable(BaseDomain *pDomain)
: m_pHead(NULL)
, m_pDomain(pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDomain));
    }
    CONTRACTL_END;
}

// Destructor for the ThreadStaticHandleTable class.
ThreadStaticHandleTable::~ThreadStaticHandleTable()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Delete the buckets.
    while (m_pHead)
    {
        ThreadStaticHandleBucket *pOld = m_pHead;
        m_pHead = pOld->GetNext();
        delete pOld;
    }
}

// Allocate handles from the large heap handle table.
OBJECTHANDLE ThreadStaticHandleTable::AllocateHandles(DWORD nRequested)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(nRequested > 0);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // create a new bucket for this allocation
    m_pHead = new ThreadStaticHandleBucket(m_pHead, nRequested, m_pDomain);

    return m_pHead->GetHandles();
}



//*****************************************************************************
// BaseDomain
//*****************************************************************************
void BaseDomain::Attach()
{
    m_SpecialStaticsCrst.Init(CrstSpecialStatics);
}

BaseDomain::BaseDomain()
{
    // initialize fields so the domain can be safely destructed
    // shouldn't call anything that can fail here - use ::Init instead
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_pTPABinderContext = NULL;

    // Make sure the container is set to NULL so that it gets loaded when it is used.
    m_pPinnedHeapHandleTable = NULL;

    // Note that m_handleStore is overridden by app domains
    m_handleStore = GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore();

#ifdef FEATURE_COMINTEROP
    m_pMngStdInterfacesInfo = NULL;
#endif
    m_FileLoadLock.PreInit();
    m_JITLock.PreInit();
    m_ClassInitLock.PreInit();
    m_ILStubGenLock.PreInit();
    m_NativeTypeLoadLock.PreInit();
} //BaseDomain::BaseDomain

//*****************************************************************************
void BaseDomain::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    //
    // Initialize the domain locks
    //

    if (this == reinterpret_cast<BaseDomain*>(&g_pSystemDomainMemory[0]))
        m_DomainCrst.Init(CrstSystemBaseDomain);
    else
        m_DomainCrst.Init(CrstBaseDomain);

    m_DomainCacheCrst.Init(CrstAppDomainCache);
    m_DomainLocalBlockCrst.Init(CrstDomainLocalBlock);

    // NOTE: CRST_UNSAFE_COOPGC prevents a GC mode switch to preemptive when entering this crst.
    // If you remove this flag, we will switch to preemptive mode when entering
    // m_FileLoadLock, which means all functions that enter it will become
    // GC_TRIGGERS.  (This includes all uses of PEFileListLockHolder, LoadLockHolder, etc.)  So be sure
    // to update the contracts if you remove this flag.
    m_FileLoadLock.Init(CrstAssemblyLoader,
                        CrstFlags(CRST_HOST_BREAKABLE), TRUE);

    //
    //   The JIT lock and the CCtor locks are at the same level (and marked as
    //   UNSAFE_SAME_LEVEL) because they are all part of the same deadlock detection mechanism. We
    //   see through cycles of JITting and .cctor execution and then explicitly allow the cycle to
    //   be broken by giving access to uninitialized classes.  If there is no cycle or if the cycle
    //   involves other locks that arent part of this special deadlock-breaking semantics, then
    //   we continue to block.
    //
    m_JITLock.Init(CrstJit, CrstFlags(CRST_REENTRANCY | CRST_UNSAFE_SAMELEVEL), TRUE);
    m_ClassInitLock.Init(CrstClassInit, CrstFlags(CRST_REENTRANCY | CRST_UNSAFE_SAMELEVEL), TRUE);

    m_ILStubGenLock.Init(CrstILStubGen, CrstFlags(CRST_REENTRANCY), TRUE);
    m_NativeTypeLoadLock.Init(CrstInteropData, CrstFlags(CRST_REENTRANCY), TRUE);

    // Pinned heap handle table CRST.
    m_PinnedHeapHandleTableCrst.Init(CrstAppDomainHandleTable);

    m_crstLoaderAllocatorReferences.Init(CrstLoaderAllocatorReferences);
    // Has to switch thread to GC_NOTRIGGER while being held (see code:BaseDomain#AssemblyListLock)
    m_crstAssemblyList.Init(CrstAssemblyList, CrstFlags(
        CRST_GC_NOTRIGGER_WHEN_TAKEN | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));

#ifdef FEATURE_COMINTEROP
    // Allocate the managed standard interfaces information.
    m_pMngStdInterfacesInfo = new MngStdInterfacesInfo();
#endif // FEATURE_COMINTEROP

    m_dwSizedRefHandles = 0;
    // For server GC this value indicates the number of GC heaps used in circular order to allocate sized
    // ref handles. It must not exceed the array size allocated by the handle table (see getNumberOfSlots
    // in objecthandle.cpp). We might want to use GetNumberOfHeaps if it were accessible here.
    m_iNumberOfProcessors = min(GetCurrentProcessCpuCount(), GetTotalProcessorCount());
}

#undef LOADERHEAP_PROFILE_COUNTER

void BaseDomain::InitVSD()
{
    STANDARD_VM_CONTRACT;

    UINT32 startingId = TypeIDMap::STARTING_UNSHARED_DOMAIN_ID;
    m_typeIDMap.Init(startingId, 2);

    GetLoaderAllocator()->InitVirtualCallStubManager(this);
}


void BaseDomain::ClearBinderContext()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_pTPABinderContext)
    {
        delete m_pTPABinderContext;
        m_pTPABinderContext = NULL;
    }
}

void AppDomain::ShutdownFreeLoaderAllocators()
{
    // If we're called from managed code (i.e. the finalizer thread) we take a lock in
    // LoaderAllocator::CleanupFailedTypeInit, which may throw. Otherwise we're called
    // from the app-domain shutdown path in which we can avoid taking the lock.
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder ch(GetLoaderAllocatorReferencesLock());

    // Shutdown the LoaderAllocators associated with collectible assemblies
    while (m_pDelayedLoaderAllocatorUnloadList != NULL)
    {
        LoaderAllocator * pCurrentLoaderAllocator = m_pDelayedLoaderAllocatorUnloadList;
        // Remove next loader allocator from the list
        m_pDelayedLoaderAllocatorUnloadList = m_pDelayedLoaderAllocatorUnloadList->m_pLoaderAllocatorDestroyNext;

        // For loader allocator finalization, we need to be careful about cleaning up per-appdomain allocations
        // and synchronizing with GC using delay unload list. We need to wait for next Gen2 GC to finish to ensure
        // that GC heap does not have any references to the MethodTables being unloaded.

        pCurrentLoaderAllocator->CleanupFailedTypeInit();

        pCurrentLoaderAllocator->CleanupHandles();

        GCX_COOP();
        SystemDomain::System()->AddToDelayedUnloadList(pCurrentLoaderAllocator);
    }
} // AppDomain::ShutdownFreeLoaderAllocators

//---------------------------------------------------------------------------------------
//
// Register the loader allocator for deletion in code:AppDomain::ShutdownFreeLoaderAllocators.
//
void AppDomain::RegisterLoaderAllocatorForDeletion(LoaderAllocator * pLoaderAllocator)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder ch(GetLoaderAllocatorReferencesLock());

    pLoaderAllocator->m_pLoaderAllocatorDestroyNext = m_pDelayedLoaderAllocatorUnloadList;
    m_pDelayedLoaderAllocatorUnloadList = pLoaderAllocator;
}

void AppDomain::SetNativeDllSearchDirectories(LPCWSTR wszNativeDllSearchDirectories)
{
    STANDARD_VM_CONTRACT;

    SString sDirectories(wszNativeDllSearchDirectories);

    if (sDirectories.GetCount() > 0)
    {
        SString::CIterator start = sDirectories.Begin();
        SString::CIterator itr = sDirectories.Begin();
        SString::CIterator end = sDirectories.End();
        SString qualifiedPath;

        while (itr != end)
        {
            start = itr;
            BOOL found = sDirectories.Find(itr, PATH_SEPARATOR_CHAR_W);
            if (!found)
            {
                itr = end;
            }

            SString qualifiedPath(sDirectories, start, itr);

            if (found)
            {
                itr++;
            }

            unsigned len = qualifiedPath.GetCount();

            if (len > 0)
            {
                if (qualifiedPath[len - 1] != DIRECTORY_SEPARATOR_CHAR_W)
                {
                    qualifiedPath.Append(DIRECTORY_SEPARATOR_CHAR_W);
                }

                NewHolder<SString> stringHolder(new SString(qualifiedPath));
                IfFailThrow(m_NativeDllSearchDirectories.Append(stringHolder.GetValue()));
                stringHolder.SuppressRelease();
            }
        }
    }
}

OBJECTREF* BaseDomain::AllocateObjRefPtrsInLargeTable(int nRequested, OBJECTREF** ppLazyAllocate)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION((nRequested > 0));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (ppLazyAllocate && *ppLazyAllocate)
    {
        // Allocation already happened
        return *ppLazyAllocate;
    }

    // Enter preemptive state, take the lock and go back to cooperative mode.
    {
        CrstHolder ch(&m_PinnedHeapHandleTableCrst);
        GCX_COOP();

        if (ppLazyAllocate && *ppLazyAllocate)
        {
            // Allocation already happened
            return *ppLazyAllocate;
        }

        // Make sure the large heap handle table is initialized.
        if (!m_pPinnedHeapHandleTable)
            InitPinnedHeapHandleTable();

        // Allocate the handles.
        OBJECTREF* result = m_pPinnedHeapHandleTable->AllocateHandles(nRequested);

        if (ppLazyAllocate)
        {
            *ppLazyAllocate = result;
        }

        return result;
    }
}

#endif // !DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
#ifndef DACCESS_COMPILE

OBJECTREF AppDomain::GetMissingObject()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!m_hndMissing)
    {
        // Get the field
        FieldDesc *pValueFD = CoreLibBinder::GetField(FIELD__MISSING__VALUE);

        pValueFD->CheckRunClassInitThrowing();

        // Retrieve the value static field and store it.
        OBJECTHANDLE hndMissing = CreateHandle(pValueFD->GetStaticOBJECTREF());

        if (FastInterlockCompareExchangePointer(&m_hndMissing, hndMissing, NULL) != NULL)
        {
            // Exchanged failed. The m_hndMissing did not equal NULL and was returned.
            DestroyHandle(hndMissing);
        }
    }

    return ObjectFromHandle(m_hndMissing);
}

#endif // DACCESS_COMPILE
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE


STRINGREF *BaseDomain::IsStringInterned(STRINGREF *pString)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pString));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    return GetLoaderAllocator()->IsStringInterned(pString);
}

STRINGREF *BaseDomain::GetOrInternString(STRINGREF *pString)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pString));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    return GetLoaderAllocator()->GetOrInternString(pString);
}

void BaseDomain::InitPinnedHeapHandleTable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pPinnedHeapHandleTable==NULL);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    m_pPinnedHeapHandleTable = new PinnedHeapHandleTable(this, STATIC_OBJECT_TABLE_BUCKET_SIZE);

#ifdef _DEBUG
    m_pPinnedHeapHandleTable->RegisterCrstDebug(&m_PinnedHeapHandleTableCrst);
#endif
}


//*****************************************************************************
//*****************************************************************************
//*****************************************************************************

void *SystemDomain::operator new(size_t size, void *pInPlace)
{
    LIMITED_METHOD_CONTRACT;
    return pInPlace;
}


void SystemDomain::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Do nothing - new() was in-place
}

void SystemDomain::Attach()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pSystemDomain == NULL);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Initialize stub managers
    PrecodeStubManager::Init();
    DelegateInvokeStubManager::Init();
    JumpStubStubManager::Init();
    RangeSectionStubManager::Init();
    ILStubManager::Init();
    InteropDispatchStubManager::Init();
    StubLinkStubManager::Init();
    ThunkHeapStubManager::Init();
    TailCallStubManager::Init();
#ifdef FEATURE_TIERED_COMPILATION
    CallCountingStubManager::Init();
#endif

    PerAppDomainTPCountList::InitAppDomainIndexList();

    m_SystemDomainCrst.Init(CrstSystemDomain, (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN));
    m_DelayedUnloadCrst.Init(CrstSystemDomainDelayedUnloadList, CRST_UNSAFE_COOPGC);

    // Initialize the ID dispenser that is used for domain neutral module IDs
    g_pModuleIndexDispenser = new IdDispenser();

    // Create the global SystemDomain and initialize it.
    m_pSystemDomain = new (&g_pSystemDomainMemory[0]) SystemDomain();
    // No way it can fail since g_pSystemDomainMemory is a static array.
    CONSISTENCY_CHECK(CheckPointer(m_pSystemDomain));

    LOG((LF_CLASSLOADER,
         LL_INFO10,
         "Created system domain at %p\n",
         m_pSystemDomain));

    // We need to initialize the memory pools etc. for the system domain.
    m_pSystemDomain->BaseDomain::Init(); // Setup the memory heaps

    // Create the one and only app domain
    AppDomain::Create();

    // Each domain gets its own ReJitManager, and ReJitManager has its own static
    // initialization to run
    ReJitManager::InitStatic();
}


void SystemDomain::DetachBegin()
{
    WRAPPER_NO_CONTRACT;
    // Shut down the domain and its children (but don't deallocate anything just
    // yet).

    // TODO: we should really not running managed DLLMain during process detach.
    if (GetThreadNULLOk() == NULL)
    {
        return;
    }

    if(m_pSystemDomain)
        m_pSystemDomain->Stop();
}

void SystemDomain::DetachEnd()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Shut down the domain and its children (but don't deallocate anything just
    // yet).
    if(m_pSystemDomain)
    {
        GCX_PREEMP();
        m_pSystemDomain->ClearBinderContext();
        AppDomain* pAppDomain = GetAppDomain();
        if (pAppDomain)
            pAppDomain->ClearBinderContext();
    }
}

void SystemDomain::Stop()
{
    WRAPPER_NO_CONTRACT;
    AppDomainIterator i(TRUE);

    while (i.Next())
        i.GetDomain()->Stop();
}

void SystemDomain::PreallocateSpecialObjects()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    _ASSERTE(g_pPreallocatedSentinelObject == NULL);

    OBJECTREF pPreallocatedSentinalObject = AllocateObject(g_pObjectClass);
    g_pPreallocatedSentinelObject = CreatePinningHandle( pPreallocatedSentinalObject );
}

void SystemDomain::CreatePreallocatedExceptions()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    EXCEPTIONREF pOutOfMemory = (EXCEPTIONREF)AllocateObject(g_pOutOfMemoryExceptionClass);
    pOutOfMemory->SetHResult(COR_E_OUTOFMEMORY);
    pOutOfMemory->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedOutOfMemoryException == NULL);
    g_pPreallocatedOutOfMemoryException = CreateHandle(pOutOfMemory);


    EXCEPTIONREF pStackOverflow = (EXCEPTIONREF)AllocateObject(g_pStackOverflowExceptionClass);
    pStackOverflow->SetHResult(COR_E_STACKOVERFLOW);
    pStackOverflow->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedStackOverflowException == NULL);
    g_pPreallocatedStackOverflowException = CreateHandle(pStackOverflow);


    EXCEPTIONREF pExecutionEngine = (EXCEPTIONREF)AllocateObject(g_pExecutionEngineExceptionClass);
    pExecutionEngine->SetHResult(COR_E_EXECUTIONENGINE);
    pExecutionEngine->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedExecutionEngineException == NULL);
    g_pPreallocatedExecutionEngineException = CreateHandle(pExecutionEngine);
}

void SystemDomain::Init()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

#ifdef _DEBUG
    LOG((
        LF_EEMEM,
        LL_INFO10,
        "sizeof(EEClass)     = %d\n"
        "sizeof(MethodTable) = %d\n"
        "sizeof(MethodDesc)= %d\n"
        "sizeof(FieldDesc)   = %d\n"
        "sizeof(Module)      = %d\n",
        sizeof(EEClass),
        sizeof(MethodTable),
        sizeof(MethodDesc),
        sizeof(FieldDesc),
        sizeof(Module)
        ));
#endif // _DEBUG

    // The base domain is initialized in SystemDomain::Attach()
    // to allow stub caches to use the memory pool. Do not
    // initialze it here!

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapDisable) != 0)
        g_fAllowNativeImages = false;

    m_pSystemFile = NULL;
    m_pSystemAssembly = NULL;

    DWORD size = 0;

    // Get the install directory so we can find CoreLib
    hr = GetInternalSystemDirectory(NULL, &size);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        ThrowHR(hr);

    // GetInternalSystemDirectory returns a size, including the null!
    WCHAR* buffer = m_SystemDirectory.OpenUnicodeBuffer(size - 1);
    IfFailThrow(GetInternalSystemDirectory(buffer, &size));
    m_SystemDirectory.CloseBuffer();
    m_SystemDirectory.Normalize();

    // At this point m_SystemDirectory should already be canonicalized
    m_BaseLibrary.Append(m_SystemDirectory);
    if (!m_BaseLibrary.EndsWith(DIRECTORY_SEPARATOR_CHAR_W))
    {
        m_BaseLibrary.Append(DIRECTORY_SEPARATOR_CHAR_W);
    }
    m_BaseLibrary.Append(g_pwBaseLibrary);
    m_BaseLibrary.Normalize();

    LoadBaseSystemClasses();

    {
        // We are about to start allocating objects, so we must be in cooperative mode.
        // However, many of the entrypoints to the system (DllGetClassObject and all
        // N/Direct exports) get called multiple times.  Sometimes they initialize the EE,
        // but generally they remain in preemptive mode.  So we really want to push/pop
        // the state here:
        GCX_COOP();

        CreatePreallocatedExceptions();
        PreallocateSpecialObjects();

        // Finish loading CoreLib now.
        m_pSystemAssembly->GetDomainAssembly()->EnsureActive();
    }

#ifdef _DEBUG
    BOOL fPause = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PauseOnLoad);

    while (fPause)
    {
        ClrSleepEx(20, TRUE);
    }
#endif // _DEBUG
}

void SystemDomain::LazyInitGlobalStringLiteralMap()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Allocate the global string literal map.
    NewHolder<GlobalStringLiteralMap> pGlobalStringLiteralMap(new GlobalStringLiteralMap());

    // Initialize the global string literal map.
    pGlobalStringLiteralMap->Init();

    if (InterlockedCompareExchangeT<GlobalStringLiteralMap *>(&m_pGlobalStringLiteralMap, pGlobalStringLiteralMap, NULL) == NULL)
    {
        pGlobalStringLiteralMap.SuppressRelease();
    }
}

/*static*/ void SystemDomain::EnumAllStaticGCRefs(promote_func* fn, ScanContext* sc)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACT_END;

    // We don't do a normal AppDomainIterator because we can't take the SystemDomain lock from
    // here.
    // We're only supposed to call this from a Server GC. We're walking here m_appDomainIdList
    // m_appDomainIdList will have an AppDomain* or will be NULL. So the only danger is if we
    // Fetch an AppDomain and then in some other thread the AppDomain is deleted.
    //
    // If the thread deleting the AppDomain (AppDomain::~AppDomain)was in Preemptive mode
    // while doing SystemDomain::EnumAllStaticGCRefs we will issue a GCX_COOP(), which will wait
    // for the GC to finish, so we are safe
    //
    // If the thread is in cooperative mode, it must have been suspended for the GC so a delete
    // can't happen.

    _ASSERTE(GCHeapUtilities::IsGCInProgress() &&
             GCHeapUtilities::IsServerHeap()   &&
             IsGCSpecialThread());

    SystemDomain* sysDomain = SystemDomain::System();
    if (sysDomain)
    {
        AppDomain* pAppDomain = ::GetAppDomain();
        if (pAppDomain && pAppDomain->IsActive())
        {
            pAppDomain->EnumStaticGCRefs(fn, sc);
        }
    }

    RETURN;
}

// Only called when EE is suspended.
DWORD SystemDomain::GetTotalNumSizedRefHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SystemDomain* sysDomain = SystemDomain::System();
    DWORD dwTotalNumSizedRefHandles = 0;
    if (sysDomain)
    {
        AppDomain* pAppDomain = ::GetAppDomain();
        if (pAppDomain && pAppDomain->IsActive())
        {
            dwTotalNumSizedRefHandles += pAppDomain->GetNumSizedRefHandles();
        }
    }

    return dwTotalNumSizedRefHandles;
}

void SystemDomain::LoadBaseSystemClasses()
{
    STANDARD_VM_CONTRACT;

    ETWOnStartup(LdSysBases_V1, LdSysBasesEnd_V1);

    {
        m_pSystemFile = PEAssembly::OpenSystem();
    }
    // Only partially load the system assembly. Other parts of the code will want to access
    // the globals in this function before finishing the load.
    m_pSystemAssembly = DefaultDomain()->LoadDomainAssembly(NULL, m_pSystemFile, FILE_LOAD_POST_LOADLIBRARY)->GetCurrentAssembly();

    // Set up binder for CoreLib
    CoreLibBinder::AttachModule(m_pSystemAssembly->GetManifestModule());

    // Load Object
    g_pObjectClass = CoreLibBinder::GetClass(CLASS__OBJECT);

    // Now that ObjectClass is loaded, we can set up
    // the system for finalizers.  There is no point in deferring this, since we need
    // to know this before we allocate our first object.
    g_pObjectFinalizerMD = CoreLibBinder::GetMethod(METHOD__OBJECT__FINALIZE);


    g_pCanonMethodTableClass = CoreLibBinder::GetClass(CLASS____CANON);

    // NOTE: !!!IMPORTANT!!! ValueType and Enum MUST be loaded one immediately after
    //                       the other, because we have coded MethodTable::IsChildValueType
    //                       in such a way that it depends on this behaviour.
    // Load the ValueType class
    g_pValueTypeClass = CoreLibBinder::GetClass(CLASS__VALUE_TYPE);

    // Load the enum class
    g_pEnumClass = CoreLibBinder::GetClass(CLASS__ENUM);
    _ASSERTE(!g_pEnumClass->IsValueType());

    // Load System.RuntimeType
    g_pRuntimeTypeClass = CoreLibBinder::GetClass(CLASS__CLASS);
    _ASSERTE(g_pRuntimeTypeClass->IsFullyLoaded());

    // Load Array class
    g_pArrayClass = CoreLibBinder::GetClass(CLASS__ARRAY);

    // Calling a method on IList<T> for an array requires redirection to a method on
    // the SZArrayHelper class. Retrieving such methods means calling
    // GetActualImplementationForArrayGenericIListMethod, which calls FetchMethod for
    // the corresponding method on SZArrayHelper. This basically results in a class
    // load due to a method call, which the debugger cannot handle, so we pre-load
    // the SZArrayHelper class here.
    g_pSZArrayHelperClass = CoreLibBinder::GetClass(CLASS__SZARRAYHELPER);

    // Load ByReference class
    //
    // NOTE: ByReference<T> must be the first by-ref-like system type to be loaded,
    //       because MethodTable::ClassifyEightBytesWithManagedLayout depends on it.
    g_pByReferenceClass = CoreLibBinder::GetClass(CLASS__BYREFERENCE);

    // Load Nullable class
    g_pNullableClass = CoreLibBinder::GetClass(CLASS__NULLABLE);

    // Load the Object array class.
    g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT] = ClassLoader::LoadArrayTypeThrowing(TypeHandle(g_pObjectClass));

    // We have delayed allocation of CoreLib's static handles until we load the object class
    CoreLibBinder::GetModule()->AllocateRegularStaticHandles(DefaultDomain());

    // Make sure all primitive types are loaded
    for (int et = ELEMENT_TYPE_VOID; et <= ELEMENT_TYPE_R8; et++)
        CoreLibBinder::LoadPrimitiveType((CorElementType)et);

    CoreLibBinder::LoadPrimitiveType(ELEMENT_TYPE_I);
    CoreLibBinder::LoadPrimitiveType(ELEMENT_TYPE_U);

    g_TypedReferenceMT = CoreLibBinder::GetClass(CLASS__TYPED_REFERENCE);

    // unfortunately, the following cannot be delay loaded since the jit
    // uses it to compute method attributes within a function that cannot
    // handle Complus exception and the following call goes through a path
    // where a complus exception can be thrown. It is unfortunate, because
    // we know that the delegate class and multidelegate class are always
    // guaranteed to be found.
    g_pDelegateClass = CoreLibBinder::GetClass(CLASS__DELEGATE);
    g_pMulticastDelegateClass = CoreLibBinder::GetClass(CLASS__MULTICAST_DELEGATE);

    CrossLoaderAllocatorHashSetup::EnsureTypesLoaded();

    // further loading of nonprimitive types may need casting support.
    // initialize cast cache here.
    CastCache::Initialize();
    ECall::PopulateManagedCastHelpers();

    // used by IsImplicitInterfaceOfSZArray
    CoreLibBinder::GetClass(CLASS__IENUMERABLEGENERIC);
    CoreLibBinder::GetClass(CLASS__ICOLLECTIONGENERIC);
    CoreLibBinder::GetClass(CLASS__ILISTGENERIC);
    CoreLibBinder::GetClass(CLASS__IREADONLYCOLLECTIONGENERIC);
    CoreLibBinder::GetClass(CLASS__IREADONLYLISTGENERIC);

    // Load String
    g_pStringClass = CoreLibBinder::LoadPrimitiveType(ELEMENT_TYPE_STRING);

    ECall::PopulateManagedStringConstructors();

    g_pExceptionClass = CoreLibBinder::GetClass(CLASS__EXCEPTION);
    g_pOutOfMemoryExceptionClass = CoreLibBinder::GetException(kOutOfMemoryException);
    g_pStackOverflowExceptionClass = CoreLibBinder::GetException(kStackOverflowException);
    g_pExecutionEngineExceptionClass = CoreLibBinder::GetException(kExecutionEngineException);
    g_pThreadAbortExceptionClass = CoreLibBinder::GetException(kThreadAbortException);

    g_pThreadClass = CoreLibBinder::GetClass(CLASS__THREAD);

#ifdef FEATURE_COMINTEROP
    if (g_pConfig->IsBuiltInCOMSupported())
    {
        g_pBaseCOMObject = CoreLibBinder::GetClass(CLASS__COM_OBJECT);
    }
    else
    {
        g_pBaseCOMObject = NULL;
    }
#endif

    g_pIDynamicInterfaceCastableInterface = CoreLibBinder::GetClass(CLASS__IDYNAMICINTERFACECASTABLE);

#ifdef FEATURE_ICASTABLE
    g_pICastableInterface = CoreLibBinder::GetClass(CLASS__ICASTABLE);
#endif // FEATURE_ICASTABLE

    // Make sure that FCall mapping for Monitor.Enter is initialized. We need it in case Monitor.Enter is used only as JIT helper.
    // For more details, see comment in code:JITutil_MonEnterWorker around "__me = GetEEFuncEntryPointMacro(JIT_MonEnter)".
    ECall::GetFCallImpl(CoreLibBinder::GetMethod(METHOD__MONITOR__ENTER));

#ifdef PROFILING_SUPPORTED
    // Note that g_profControlBlock.fBaseSystemClassesLoaded must be set to TRUE only after
    // all base system classes are loaded.  Profilers are not allowed to call any type-loading
    // APIs until g_profControlBlock.fBaseSystemClassesLoaded is TRUE.  It is important that
    // all base system classes need to be loaded before profilers can trigger the type loading.
    g_profControlBlock.fBaseSystemClassesLoaded = TRUE;
#endif // PROFILING_SUPPORTED

#if defined(_DEBUG)
    g_CoreLib.Check();
#endif
}

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT)

Thread::ApartmentState SystemDomain::GetEntryPointThreadAptState(IMDInternalImport* pScope, mdMethodDef mdMethod)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;
    IfFailThrow(hr = pScope->GetCustomAttributeByName(mdMethod,
                                                      DEFAULTDOMAIN_MTA_TYPE,
                                                      NULL,
                                                      NULL));
    BOOL fIsMTA = FALSE;
    if(hr == S_OK)
        fIsMTA = TRUE;

    IfFailThrow(hr = pScope->GetCustomAttributeByName(mdMethod,
                                                      DEFAULTDOMAIN_STA_TYPE,
                                                      NULL,
                                                      NULL));
    BOOL fIsSTA = FALSE;
    if (hr == S_OK)
        fIsSTA = TRUE;

    if (fIsSTA && fIsMTA)
        COMPlusThrowHR(COR_E_CUSTOMATTRIBUTEFORMAT);

    if (fIsSTA)
        return Thread::AS_InSTA;
    else if (fIsMTA)
        return Thread::AS_InMTA;

    return Thread::AS_Unknown;
}

void SystemDomain::SetThreadAptState (Thread::ApartmentState state)
{
    STANDARD_VM_CONTRACT;

    Thread* pThread = GetThread();
    if(state == Thread::AS_InSTA)
    {
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InSTA);
        _ASSERTE(pState == Thread::AS_InSTA);
    }
    else
    {
        // If an apartment state was not explicitly requested, default to MTA
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InMTA);
        _ASSERTE(pState == Thread::AS_InMTA);
    }
}
#endif // defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT)

/*static*/
bool SystemDomain::IsReflectionInvocationMethod(MethodDesc* pMeth)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable* pCaller = pMeth->GetMethodTable();

    // All Reflection Invocation methods are defined in CoreLib
    if (!pCaller->GetModule()->IsSystem())
        return false;

    /* List of types that should be skipped to identify true caller */
    static const BinderClassID reflectionInvocationTypes[] = {
        CLASS__METHOD,
        CLASS__METHOD_BASE,
        CLASS__METHOD_INFO,
        CLASS__CONSTRUCTOR,
        CLASS__CONSTRUCTOR_INFO,
        CLASS__CLASS,
        CLASS__TYPE_HANDLE,
        CLASS__METHOD_HANDLE,
        CLASS__FIELD_HANDLE,
        CLASS__TYPE,
        CLASS__FIELD,
        CLASS__RT_FIELD_INFO,
        CLASS__FIELD_INFO,
        CLASS__EVENT,
        CLASS__EVENT_INFO,
        CLASS__PROPERTY,
        CLASS__PROPERTY_INFO,
        CLASS__ACTIVATOR,
        CLASS__ARRAY,
        CLASS__ASSEMBLYBASE,
        CLASS__ASSEMBLY,
        CLASS__TYPE_DELEGATOR,
        CLASS__RUNTIME_HELPERS,
        CLASS__DYNAMICMETHOD,
        CLASS__DELEGATE,
        CLASS__MULTICAST_DELEGATE
    };

    static bool fInited = false;

    if (!VolatileLoad(&fInited))
    {
        // Make sure all types are loaded so that we can use faster GetExistingClass()
        for (unsigned i = 0; i < NumItems(reflectionInvocationTypes); i++)
        {
            CoreLibBinder::GetClass(reflectionInvocationTypes[i]);
        }

        VolatileStore(&fInited, true);
    }

    if (!pCaller->HasInstantiation())
    {
        for (unsigned i = 0; i < NumItems(reflectionInvocationTypes); i++)
        {
            if (CoreLibBinder::GetExistingClass(reflectionInvocationTypes[i]) == pCaller)
                return true;
        }
    }

    return false;
}

struct CallersDataWithStackMark
{
    StackCrawlMark* stackMark;
    BOOL foundMe;
    MethodDesc* pFoundMethod;
    MethodDesc* pPrevMethod;
};

/*static*/
MethodDesc* SystemDomain::GetCallersMethod(StackCrawlMark* stackMark)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_COOP();

    CallersDataWithStackMark cdata;
    ZeroMemory(&cdata, sizeof(CallersDataWithStackMark));
    cdata.stackMark = stackMark;

    GetThread()->StackWalkFrames(CallersMethodCallbackWithStackMark, &cdata, FUNCTIONSONLY | LIGHTUNWIND);

    if(cdata.pFoundMethod) {
        return cdata.pFoundMethod;
    } else
        return NULL;
}

/*static*/
MethodTable* SystemDomain::GetCallersType(StackCrawlMark* stackMark)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CallersDataWithStackMark cdata;
    ZeroMemory(&cdata, sizeof(CallersDataWithStackMark));
    cdata.stackMark = stackMark;

    GetThread()->StackWalkFrames(CallersMethodCallbackWithStackMark, &cdata, FUNCTIONSONLY | LIGHTUNWIND);

    if(cdata.pFoundMethod) {
        return cdata.pFoundMethod->GetMethodTable();
    } else
        return NULL;
}

/*static*/
Module* SystemDomain::GetCallersModule(StackCrawlMark* stackMark)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_COOP();

    CallersDataWithStackMark cdata;
    ZeroMemory(&cdata, sizeof(CallersDataWithStackMark));
    cdata.stackMark = stackMark;

    GetThread()->StackWalkFrames(CallersMethodCallbackWithStackMark, &cdata, FUNCTIONSONLY | LIGHTUNWIND);

    if(cdata.pFoundMethod) {
        return cdata.pFoundMethod->GetModule();
    } else
        return NULL;
}

struct CallersData
{
    int skip;
    MethodDesc* pMethod;
};

/*static*/
Assembly* SystemDomain::GetCallersAssembly(StackCrawlMark *stackMark)
{
    WRAPPER_NO_CONTRACT;
    Module* mod = GetCallersModule(stackMark);
    if (mod)
        return mod->GetAssembly();
    return NULL;
}

/*private static*/
StackWalkAction SystemDomain::CallersMethodCallbackWithStackMark(CrawlFrame* pCf, VOID* data)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;


    MethodDesc *pFunc = pCf->GetFunction();

    /* We asked to be called back only for functions */
    _ASSERTE(pFunc);

    CallersDataWithStackMark* pCaller = (CallersDataWithStackMark*) data;
    if (pCaller->stackMark)
    {
        if (!pCf->IsInCalleesFrames(pCaller->stackMark))
        {
            // save the current in case it is the one we want
            pCaller->pPrevMethod = pFunc;
            return SWA_CONTINUE;
        }

        // LookForMe stack crawl marks needn't worry about reflection or
        // remoting frames on the stack. Each frame above (newer than) the
        // target will be captured by the logic above. Once we transition to
        // finding the stack mark below the AofRA, we know that we hit the
        // target last time round and immediately exit with the cached result.

        if (*(pCaller->stackMark) == LookForMe)
        {
            pCaller->pFoundMethod = pCaller->pPrevMethod;
            return SWA_ABORT;
        }
    }

    // Skip reflection and remoting frames that could lie between a stack marked
    // method and its true caller (or that caller and its own caller). These
    // frames are infrastructure and logically transparent to the stack crawling
    // algorithm.

    // Skipping remoting frames. We always skip entire client to server spans
    // (though we see them in the order server then client during a stack crawl
    // obviously).

    // We spot the server dispatcher end because all calls are dispatched
    // through a single method: StackBuilderSink._PrivateProcessMessage.

    Frame* frame = pCf->GetFrame();
    _ASSERTE(pCf->IsFrameless() || frame);



    // Skipping reflection frames. We don't need to be quite as exhaustive here
    // as the security or reflection stack walking code since we know this logic
    // is only invoked for selected methods in CoreLib itself. So we're
    // reasonably sure we won't have any sensitive methods late bound invoked on
    // constructors, properties or events. This leaves being invoked via
    // MethodInfo, Type or Delegate (and depending on which invoke overload is
    // being used, several different reflection classes may be involved).

    g_IBCLogger.LogMethodDescAccess(pFunc);

    if (SystemDomain::IsReflectionInvocationMethod(pFunc))
        return SWA_CONTINUE;

    if (frame && frame->GetFrameType() == Frame::TYPE_MULTICAST)
    {
        // This must be either a multicast delegate invocation.

        _ASSERTE(pFunc->GetMethodTable()->IsDelegate());

        DELEGATEREF del = (DELEGATEREF)((MulticastFrame*)frame)->GetThis(); // This can throw.

        _ASSERTE(COMDelegate::IsTrueMulticastDelegate(del));
        return SWA_CONTINUE;
    }

    // Return the first non-reflection/remoting frame if no stack mark was
    // supplied.
    if (!pCaller->stackMark)
    {
        pCaller->pFoundMethod = pFunc;
        return SWA_ABORT;
    }

    // If we got here, we must already be in the frame containing the stack mark and we are not looking for "me".
    _ASSERTE(pCaller->stackMark &&
             pCf->IsInCalleesFrames(pCaller->stackMark) &&
             *(pCaller->stackMark) != LookForMe);

    // When looking for caller's caller, we delay returning results for another
    // round (the way this is structured, we will still be able to skip
    // reflection and remoting frames between the caller and the caller's
    // caller).

    if ((*(pCaller->stackMark) == LookForMyCallersCaller) &&
        (pCaller->pFoundMethod == NULL))
    {
        pCaller->pFoundMethod = pFunc;
        return SWA_CONTINUE;
    }

    pCaller->pFoundMethod = pFunc;

    return SWA_ABORT;
}

/*private static*/
StackWalkAction SystemDomain::CallersMethodCallback(CrawlFrame* pCf, VOID* data)
{
    LIMITED_METHOD_CONTRACT;
    MethodDesc *pFunc = pCf->GetFunction();

    /* We asked to be called back only for functions */
    _ASSERTE(pFunc);

    CallersData* pCaller = (CallersData*) data;
    if(pCaller->skip == 0) {
        pCaller->pMethod = pFunc;
        return SWA_ABORT;
    }
    else {
        pCaller->skip--;
        return SWA_CONTINUE;
    }

}


void AppDomain::Create()
{
    STANDARD_VM_CONTRACT;

    AppDomainRefHolder pDomain(new AppDomain());

    pDomain->Init();

    // allocate a Virtual Call Stub Manager for the default domain
    pDomain->InitVSD();

    pDomain->SetStage(AppDomain::STAGE_OPEN);
    pDomain.SuppressRelease();

    m_pTheAppDomain = pDomain;

    LOG((LF_CLASSLOADER | LF_CORDB,
         LL_INFO10,
         "Created the app domain at %p\n", m_pTheAppDomain));
}

#ifdef DEBUGGING_SUPPORTED

void SystemDomain::PublishAppDomainAndInformDebugger (AppDomain *pDomain)
{
    CONTRACTL
    {
        if(!g_fEEInit) {THROWS;} else {DISABLED(NOTHROW);};
        if(!g_fEEInit) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);};
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO100, "SD::PADAID: Adding 0x%x\n", pDomain));

    // Call the publisher API to add this appdomain entry to the list
    // The publisher will handle failures, so we don't care if this succeeds or fails.
    if (g_pDebugInterface != NULL)
    {
        g_pDebugInterface->AddAppDomainToIPC(pDomain);
    }
}

#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
void SystemDomain::NotifyProfilerStartup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        (&g_profControlBlock)->AppDomainCreationStarted((AppDomainID) System());
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        (&g_profControlBlock)->AppDomainCreationFinished((AppDomainID) System(), S_OK);
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        (&g_profControlBlock)->AppDomainCreationStarted((AppDomainID) System()->DefaultDomain());
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        (&g_profControlBlock)->AppDomainCreationFinished((AppDomainID) System()->DefaultDomain(), S_OK);
        END_PROFILER_CALLBACK();
    }
}

HRESULT SystemDomain::NotifyProfilerShutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        (&g_profControlBlock)->AppDomainShutdownStarted((AppDomainID) System());
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        (&g_profControlBlock)->AppDomainShutdownFinished((AppDomainID) System(), S_OK);
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        (&g_profControlBlock)->AppDomainShutdownStarted((AppDomainID) System()->DefaultDomain());
        END_PROFILER_CALLBACK();
    }

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        (&g_profControlBlock)->AppDomainShutdownFinished((AppDomainID) System()->DefaultDomain(), S_OK);
        END_PROFILER_CALLBACK();
    }
    return (S_OK);
}
#endif // PROFILING_SUPPORTED

AppDomain::AppDomain()
{
    // initialize fields so the appdomain can be safely destructed
    // shouldn't call anything that can fail here - use ::Init instead
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_cRef=1;

    m_pRootAssembly = NULL;

    m_dwFlags = 0;
#ifdef FEATURE_COMINTEROP
    m_pRCWCache = NULL;
#endif //FEATURE_COMINTEROP
#ifdef FEATURE_COMWRAPPERS
    m_pRCWRefCache = NULL;
#endif // FEATURE_COMWRAPPERS

    m_handleStore = NULL;

#ifdef _DEBUG
    m_Assemblies.Debug_SetAppDomain(this);
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    m_pRefDispIDCache = NULL;
    m_hndMissing = NULL;
#endif

    m_pRefClassFactHash = NULL;

    m_ForceTrivialWaitOperations = false;
    m_Stage=STAGE_CREATING;

#ifdef _DEBUG
    m_dwIterHolders=0;
#endif

#ifdef FEATURE_TYPEEQUIVALENCE
    m_pTypeEquivalenceTable = NULL;
#endif // FEATURE_TYPEEQUIVALENCE

} // AppDomain::AppDomain

AppDomain::~AppDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    // release the TPIndex.  note that since TPIndex values are recycled the TPIndex
    // can only be released once all threads in the AppDomain have exited.
    if (GetTPIndex().m_dwIndex != 0)
        PerAppDomainTPCountList::ResetAppDomainIndex(GetTPIndex());

    m_AssemblyCache.Clear();

}

//*****************************************************************************
//*****************************************************************************
//*****************************************************************************
void AppDomain::Init()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    m_pDelayedLoaderAllocatorUnloadList = NULL;

    SetStage( STAGE_CREATING);


    // The lock is taken also during stack walking (GC or profiler)
    //  - To prevent deadlock with GC thread, we cannot trigger GC while holding the lock
    //  - To prevent deadlock with profiler thread, we cannot allow thread suspension
    m_crstHostAssemblyMap.Init(
        CrstHostAssemblyMap,
        (CrstFlags)(CRST_GC_NOTRIGGER_WHEN_TAKEN
                    | CRST_DEBUGGER_THREAD
                    INDEBUG(| CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD)));
    m_crstHostAssemblyMapAdd.Init(CrstHostAssemblyMapAdd);

    //Allocate the threadpool entry before the appdomain id list. Otherwise,
    //the thread pool list will be out of sync if insertion of id in
    //the appdomain fails.
    m_tpIndex = PerAppDomainTPCountList::AddNewTPIndex();

    BaseDomain::Init();

    // Set up the binding caches
    m_AssemblyCache.Init(&m_DomainCacheCrst, GetHighFrequencyHeap());

    m_MemoryPressure = 0;


    // Default domain reuses the handletablemap that was created during EEStartup
    m_handleStore = GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore();

    if (!m_handleStore)
    {
        COMPlusThrowOM();
    }


#ifdef FEATURE_TYPEEQUIVALENCE
    m_TypeEquivalenceCrst.Init(CrstTypeEquivalenceMap);
#endif

    m_ReflectionCrst.Init(CrstReflection, CRST_UNSAFE_ANYMODE);
    m_RefClassFactCrst.Init(CrstClassFactInfoHash);

    SetStage(STAGE_READYFORMANAGEDCODE);


#ifdef FEATURE_TIERED_COMPILATION
    m_tieredCompilationManager.Init();
#endif

    m_nativeImageLoadCrst.Init(CrstNativeImageLoad);
} // AppDomain::Init

void AppDomain::Stop()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef FEATURE_MULTICOREJIT
    GetMulticoreJitManager().StopProfile(true);
#endif

    // Set the unloaded flag before notifying the debugger
    GetLoaderAllocator()->SetIsUnloaded();

#ifdef DEBUGGING_SUPPORTED
    if (IsDebuggerAttached())
        NotifyDebuggerUnload();

    if (NULL != g_pDebugInterface)
    {
        // Call the publisher API to delete this appdomain entry from the list
        CONTRACT_VIOLATION(ThrowsViolation);
        g_pDebugInterface->RemoveAppDomainFromIPC (this);
    }
#endif // DEBUGGING_SUPPORTED
}


#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE

void AppDomain::AddAssembly(DomainAssembly * assem)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    {
        CrstHolder ch(GetAssemblyListLock());

        // Attempt to find empty space in assemblies list
        DWORD asmCount = m_Assemblies.GetCount_Unlocked();
        for (DWORD i = 0; i < asmCount; ++i)
        {
            if (m_Assemblies.Get_UnlockedNoReference(i) == NULL)
            {
                m_Assemblies.Set_Unlocked(i, assem);
                return;
            }
        }

        // If empty space not found, simply add to end of list
        IfFailThrow(m_Assemblies.Append_Unlocked(assem));
    }
}

void AppDomain::RemoveAssembly(DomainAssembly * pAsm)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CrstHolder ch(GetAssemblyListLock());
    DWORD asmCount = m_Assemblies.GetCount_Unlocked();
    for (DWORD i = 0; i < asmCount; ++i)
    {
        if (m_Assemblies.Get_UnlockedNoReference(i) == pAsm)
        {
            m_Assemblies.Set_Unlocked(i, NULL);
            return;
        }
    }

    _ASSERTE(!"Unreachable");
}

BOOL AppDomain::ContainsAssembly(Assembly * assem)
{
    WRAPPER_NO_CONTRACT;
    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (i.Next(pDomainAssembly.This()))
    {
        CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetLoadedAssembly();
        if (pAssembly == assem)
            return TRUE;
    }

    return FALSE;
}

EEClassFactoryInfoHashTable* AppDomain::SetupClassFactHash()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CrstHolder ch(&m_ReflectionCrst);

    if (m_pRefClassFactHash == NULL)
    {
        AllocMemHolder<void> pCache(GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof (EEClassFactoryInfoHashTable))));
        EEClassFactoryInfoHashTable *tmp = new (pCache) EEClassFactoryInfoHashTable;
        LockOwner lock = {&m_RefClassFactCrst,IsOwnerOfCrst};
        if (!tmp->Init(20, &lock))
            COMPlusThrowOM();
        pCache.SuppressRelease();
        m_pRefClassFactHash = tmp;
    }

    return m_pRefClassFactHash;
}

#ifdef FEATURE_COMINTEROP
DispIDCache* AppDomain::SetupRefDispIDCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CrstHolder ch(&m_ReflectionCrst);

    if (m_pRefDispIDCache == NULL)
    {
        AllocMemHolder<void> pCache = GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof (DispIDCache)));

        DispIDCache *tmp = new (pCache) DispIDCache;
        tmp->Init();

        pCache.SuppressRelease();
        m_pRefDispIDCache = tmp;
    }

    return m_pRefDispIDCache;
}

#endif // FEATURE_COMINTEROP

FileLoadLock *FileLoadLock::Create(PEFileListLock *pLock, PEFile *pFile, DomainFile *pDomainFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pLock->HasLock());
        PRECONDITION(pLock->FindFileLock(pFile) == NULL);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    NewHolder<FileLoadLock> result(new FileLoadLock(pLock, pFile, pDomainFile));

    pLock->AddElement(result);
    result->AddRef(); // Add one ref on behalf of the ListLock's reference. The corresponding Release() happens in FileLoadLock::CompleteLoadLevel.
    return result.Extract();
}

FileLoadLock::~FileLoadLock()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    ((PEFile *) m_data)->Release();
}

DomainFile *FileLoadLock::GetDomainFile()
{
    LIMITED_METHOD_CONTRACT;
    return m_pDomainFile;
}

FileLoadLevel FileLoadLock::GetLoadLevel()
{
    LIMITED_METHOD_CONTRACT;
    return m_level;
}

// Acquire will return FALSE and not take the lock if the file
// has already been loaded to the target level.  Otherwise,
// it will return TRUE and take the lock.
//
// Note that the taker must release the lock via IncrementLoadLevel.

BOOL FileLoadLock::Acquire(FileLoadLevel targetLevel)
{
    WRAPPER_NO_CONTRACT;

    // If we are already loaded to the desired level, the lock is "free".
    if (m_level >= targetLevel)
        return FALSE;

    if (!DeadlockAwareEnter())
    {
        // We failed to get the lock due to a deadlock.
        return FALSE;
    }

    if (m_level >= targetLevel)
    {
        Leave();
        return FALSE;
    }

    return TRUE;
}

BOOL FileLoadLock::CanAcquire(FileLoadLevel targetLevel)
{
    // If we are already loaded to the desired level, the lock is "free".
    if (m_level >= targetLevel)
        return FALSE;

    return CanDeadlockAwareEnter();
}

#if !defined(DACCESS_COMPILE) && (defined(LOGGING) || defined(STRESS_LOG))
static const char *fileLoadLevelName[] =
{
    "CREATE",                             // FILE_LOAD_CREATE
    "BEGIN",                              // FILE_LOAD_BEGIN
    "FIND_NATIVE_IMAGE",                  // FILE_LOAD_FIND_NATIVE_IMAGE
    "VERIFY_NATIVE_IMAGE_DEPENDENCIES",   // FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES
    "ALLOCATE",                           // FILE_LOAD_ALLOCATE
    "ADD_DEPENDENCIES",                   // FILE_LOAD_ADD_DEPENDENCIES
    "PRE_LOADLIBRARY",                    // FILE_LOAD_PRE_LOADLIBRARY
    "LOADLIBRARY",                        // FILE_LOAD_LOADLIBRARY
    "POST_LOADLIBRARY",                   // FILE_LOAD_POST_LOADLIBRARY
    "EAGER_FIXUPS",                       // FILE_LOAD_EAGER_FIXUPS
    "DELIVER_EVENTS",                     // FILE_LOAD_DELIVER_EVENTS
    "VTABLE FIXUPS",                      // FILE_LOAD_VTABLE_FIXUPS
    "LOADED",                             // FILE_LOADED
    "ACTIVE",                             // FILE_ACTIVE
};
#endif // !DACCESS_COMPILE && (LOGGING || STRESS_LOG)

BOOL FileLoadLock::CompleteLoadLevel(FileLoadLevel level, BOOL success)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(HasLock());
    }
    CONTRACTL_END;

    // Increment may happen more than once if reentrancy occurs (e.g. LoadLibrary)
    if (level > m_level)
    {
        // Must complete each level in turn, unless we have an error
        CONSISTENCY_CHECK(m_pDomainFile->IsError() || (level == (m_level+1)));
        // Remove the lock from the list if the load is completed
        if (level >= FILE_ACTIVE)
        {
            {
                GCX_COOP();
                PEFileListLockHolder lock((PEFileListLock*)m_pList);

#if _DEBUG
                BOOL fDbgOnly_SuccessfulUnlink =
#endif
                    m_pList->Unlink(this);
                _ASSERTE(fDbgOnly_SuccessfulUnlink);

                m_pDomainFile->ClearLoading();

                CONSISTENCY_CHECK(m_dwRefCount >= 2); // Caller (LoadDomainFile) should have 1 refcount and m_pList should have another which was acquired in FileLoadLock::Create.

                m_level = (FileLoadLevel)level;

                // Dev11 bug 236344
                // In AppDomain::IsLoading, if the lock is taken on m_pList and then FindFileLock returns NULL,
                // we depend on the DomainFile's load level being up to date. Hence we must update the load
                // level while the m_pList lock is held.
                if (success)
                    m_pDomainFile->SetLoadLevel(level);
            }


            Release(); // Release m_pList's refcount on this lock, which was acquired in FileLoadLock::Create

        }
        else
        {
            m_level = (FileLoadLevel)level;

            if (success)
                m_pDomainFile->SetLoadLevel(level);
        }

#ifndef DACCESS_COMPILE
        switch(level)
        {
            case FILE_LOAD_ALLOCATE:
            case FILE_LOAD_ADD_DEPENDENCIES:
            case FILE_LOAD_DELIVER_EVENTS:
            case FILE_LOADED:
            case FILE_ACTIVE: // The timing of stress logs is not critical, so even for the FILE_ACTIVE stage we need not do it while the m_pList lock is held.
                STRESS_LOG3(LF_CLASSLOADER, LL_INFO100, "Completed Load Level %s for DomainFile %p - success = %i\n", fileLoadLevelName[level], m_pDomainFile, success);
                break;
            default:
                break;
        }
#endif

        return TRUE;
    }
    else
        return FALSE;
}

void FileLoadLock::SetError(Exception *ex)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(CheckPointer(ex));
        PRECONDITION(HasLock());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    m_cachedHR = ex->GetHR();

    LOG((LF_LOADER, LL_WARNING, "LOADER: %x:***%s*\t!!!Non-transient error 0x%x\n",
         m_pDomainFile->GetAppDomain(), m_pDomainFile->GetSimpleName(), m_cachedHR));

    m_pDomainFile->SetError(ex);

    CompleteLoadLevel(FILE_ACTIVE, FALSE);
}

void FileLoadLock::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockIncrement((LONG *) &m_dwRefCount);
}

UINT32 FileLoadLock::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LONG count = FastInterlockDecrement((LONG *) &m_dwRefCount);
    if (count == 0)
        delete this;

    return count;
}

FileLoadLock::FileLoadLock(PEFileListLock *pLock, PEFile *pFile, DomainFile *pDomainFile)
  : ListLockEntry(pLock, pFile, "File load lock"),
    m_level((FileLoadLevel) (FILE_LOAD_CREATE)),
    m_pDomainFile(pDomainFile),
    m_cachedHR(S_OK)
{
    WRAPPER_NO_CONTRACT;
    pFile->AddRef();
}

void FileLoadLock::HolderLeave(FileLoadLock *pThis)
{
    LIMITED_METHOD_CONTRACT;
    pThis->Leave();
}






//
// Assembly loading:
//
// Assembly loading is carefully layered to avoid deadlocks in the
// presence of circular loading dependencies.
// A LoadLevel is associated with each assembly as it is being loaded.  During the
// act of loading (abstractly, increasing its load level), its lock is
// held, and the current load level is stored on the thread.  Any
// recursive loads during that period are automatically restricted to
// only partially load the dependent assembly to the same level as the
// caller (or to one short of that level in the presence of a deadlock
// loop.)
//
// Each loading stage must be carfully constructed so that
// this constraint is expected and can be dealt with.
//
// Note that there is one case where this still doesn't handle recursion, and that is the
// security subsytem. The security system runs managed code, and thus must typically fully
// initialize assemblies of permission sets it is trying to use. (And of course, these may be used
// while those assemblies are initializing.)  This is dealt with in the historical manner - namely
// the security system passes in a special flag which says that it will deal with null return values
// in the case where a load cannot be safely completed due to such issues.
//

void AppDomain::LoadSystemAssemblies()
{
    STANDARD_VM_CONTRACT;

    // The only reason to make an assembly a "system assembly" is if the EE is caching
    // pointers to stuff in the assembly.  Because this is going on, we need to preserve
    // the invariant that the assembly is loaded into every app domain.
    //
    // Right now we have only one system assembly. We shouldn't need to add any more.

    LoadAssembly(NULL, SystemDomain::System()->SystemFile(), FILE_ACTIVE);
}

FileLoadLevel AppDomain::GetDomainFileLoadLevel(DomainFile *pFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    LoadLockHolder lock(this);

    FileLoadLock* pLockEntry = (FileLoadLock *) lock->FindFileLock(pFile->GetFile());

    if (pLockEntry == NULL)
        return pFile->GetLoadLevel();
    else
        return pLockEntry->GetLoadLevel();
}

// This checks if the thread has initiated (or completed) loading at the given level.  A false guarantees that
// (a) The current thread (or a thread blocking on the current thread) has not started loading the file
//      at the given level, and
// (b) No other thread had started loading the file at this level at the start of this function call.

// Note that another thread may start loading the file at that level in a race with the completion of
// this function.  However, the caller still has the guarantee that such a load started after this
// function was called (and e.g. any state in place before the function call will be seen by the other thread.)
//
// Conversely, a true guarantees that either the current thread has started the load step, or another
// thread has completed the load step.
//

BOOL AppDomain::IsLoading(DomainFile *pFile, FileLoadLevel level)
{
    // Cheap out
    if (pFile->GetLoadLevel() < level)
    {
        FileLoadLock *pLock = NULL;
        {
            LoadLockHolder lock(this);

            pLock = (FileLoadLock *) lock->FindFileLock(pFile->GetFile());

            if (pLock == NULL)
            {
                // No thread involved with loading
                return pFile->GetLoadLevel() >= level;
            }

            pLock->AddRef();
        }

        FileLoadLockRefHolder lockRef(pLock);

        if (pLock->Acquire(level))
        {
            // We got the lock - therefore no other thread has started this loading step yet.
            pLock->Leave();
            return FALSE;
        }

        // We didn't get the lock - either this thread is already doing the load,
        // or else the load has already finished.
    }
    return TRUE;
}

// CheckLoading is a weaker form of IsLoading, which will not block on
// other threads waiting for their status.  This is appropriate for asserts.
CHECK AppDomain::CheckLoading(DomainFile *pFile, FileLoadLevel level)
{
    // Cheap out
    if (pFile->GetLoadLevel() < level)
    {
        FileLoadLock *pLock = NULL;

        LoadLockHolder lock(this);

        pLock = (FileLoadLock *) lock->FindFileLock(pFile->GetFile());

        if (pLock != NULL
            && pLock->CanAcquire(level))
        {
            // We can get the lock - therefore no other thread has started this loading step yet.
            CHECK_FAILF(("Loading step %d has not been initiated yet", level));
        }

        // We didn't get the lock - either this thread is already doing the load,
        // or else the load has already finished.
    }

    CHECK_OK;
}

CHECK AppDomain::CheckCanLoadTypes(Assembly *pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    CHECK_MSG(CheckValidModule(pAssembly->GetManifestModule()),
              "Type loading can occur only when executing in the assembly's app domain");
    CHECK_OK;
}

CHECK AppDomain::CheckCanExecuteManagedCode(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Module* pModule=pMD->GetModule();

    CHECK_MSG(CheckValidModule(pModule),
              "Managed code can only run when executing in the module's app domain");

    if (!pMD->IsInterface() || pMD->IsStatic()) //interfaces require no activation for instance methods
    {
        //cctor could have been interupted by ADU
        CHECK_MSG(pModule->CheckActivated(),
              "Managed code can only run when its module has been activated in the current app domain");
    }

    CHECK_OK;
}

#endif // !DACCESS_COMPILE

void AppDomain::LoadDomainFile(DomainFile *pFile,
                               FileLoadLevel targetLevel)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM();); }
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Quick exit if finished
    if (pFile->GetLoadLevel() >= targetLevel)
        return;

    // Handle the error case
    pFile->ThrowIfError(targetLevel);


#ifndef DACCESS_COMPILE

    if (pFile->IsLoading())
    {
        GCX_PREEMP();

        // Load some more if appropriate
        LoadLockHolder lock(this);

        FileLoadLock* pLockEntry = (FileLoadLock *) lock->FindFileLock(pFile->GetFile());
        if (pLockEntry == NULL)
        {
            _ASSERTE (!pFile->IsLoading());
            return;
        }

        pLockEntry->AddRef();

        lock.Release();

        LoadDomainFile(pLockEntry, targetLevel);
    }

#else // DACCESS_COMPILE
    DacNotImpl();
#endif // DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

FileLoadLevel AppDomain::GetThreadFileLoadLevel()
{
    WRAPPER_NO_CONTRACT;
    if (GetThread()->GetLoadLevelLimiter() == NULL)
        return FILE_ACTIVE;
    else
        return (FileLoadLevel)(GetThread()->GetLoadLevelLimiter()->GetLoadLevel()-1);
}


Assembly *AppDomain::LoadAssembly(AssemblySpec* pIdentity,
                                  PEAssembly *pFile,
                                  FileLoadLevel targetLevel)
{
    CONTRACT(Assembly *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK)); // May be NULL in recursive load case
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DomainAssembly *pAssembly = LoadDomainAssembly(pIdentity, pFile, targetLevel);
    PREFIX_ASSUME(pAssembly != NULL);

    RETURN pAssembly->GetAssembly();
}

DomainAssembly* AppDomain::LoadDomainAssembly(AssemblySpec* pSpec,
                                              PEAssembly *pFile,
                                              FileLoadLevel targetLevel)
{
    STATIC_CONTRACT_THROWS;

    if (pSpec == nullptr)
    {
        // skip caching, since we don't have anything to base it on
        return LoadDomainAssemblyInternal(pSpec, pFile, targetLevel);
    }

    DomainAssembly* pRetVal = NULL;
    EX_TRY
    {
        pRetVal = LoadDomainAssemblyInternal(pSpec, pFile, targetLevel);
    }
    EX_HOOK
    {
        Exception* pEx = GET_EXCEPTION();
        if (!pEx->IsTransient())
        {
            // Setup the binder reference in AssemblySpec from the PEAssembly if one is not already set.
            AssemblyBinder* pCurrentBindingContext = pSpec->GetBindingContext();
            AssemblyBinder* pBindingContextFromPEAssembly = pFile->GetBindingContext();

            if (pCurrentBindingContext == NULL)
            {
                // Set the binding context we got from the PEAssembly if AssemblySpec does not
                // have that information
                _ASSERTE(pBindingContextFromPEAssembly != NULL);
                pSpec->SetBindingContext(pBindingContextFromPEAssembly);
            }
#if defined(_DEBUG)
            else
            {
                // Binding context in the spec should be the same as the binding context in the PEAssembly
                _ASSERTE(pCurrentBindingContext == pBindingContextFromPEAssembly);
            }
#endif // _DEBUG

            if (!EEFileLoadException::CheckType(pEx))
            {
                StackSString name;
                pSpec->GetFileOrDisplayName(0, name);
                pEx=new EEFileLoadException(name, pEx->GetHR(), pEx);
                AddExceptionToCache(pSpec, pEx);
                PAL_CPP_THROW(Exception *, pEx);
            }
            else
                AddExceptionToCache(pSpec, pEx);
        }
    }
    EX_END_HOOK;

    return pRetVal;
}


DomainAssembly *AppDomain::LoadDomainAssemblyInternal(AssemblySpec* pIdentity,
                                              PEAssembly *pFile,
                                              FileLoadLevel targetLevel)
{
    CONTRACT(DomainAssembly *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        PRECONDITION(pFile->IsSystem() || ::GetAppDomain()==this);
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->GetLoadLevel() >= GetThreadFileLoadLevel()
                      || RETVAL->GetLoadLevel() >= targetLevel);
        POSTCONDITION(RETVAL->CheckNoError(targetLevel));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;


    DomainAssembly * result;

    // Go into preemptive mode since this may take a while.
    GCX_PREEMP();

    // Check for existing fully loaded assembly, or for an assembly which has failed during the loading process.
    result = FindAssembly(pFile, FindAssemblyOptions_IncludeFailedToLoad);

    if (result == NULL)
    {
        LoaderAllocator *pLoaderAllocator = NULL;

        AssemblyBinder *pFileBinder = pFile->GetBindingContext();
        if (pFileBinder != NULL)
        {
            // Assemblies loaded with AssemblyLoadContext need to use a different LoaderAllocator if
            // marked as collectible
            pLoaderAllocator = pFileBinder->GetLoaderAllocator();
        }

        if (pLoaderAllocator == NULL)
        {
            pLoaderAllocator = this->GetLoaderAllocator();
        }

        // Allocate the DomainAssembly a bit early to avoid GC mode problems. We could potentially avoid
        // a rare redundant allocation by moving this closer to FileLoadLock::Create, but it's not worth it.
        NewHolder<DomainAssembly> pDomainAssembly = new DomainAssembly(this, pFile, pLoaderAllocator);

        LoadLockHolder lock(this);

        // Find the list lock entry
        FileLoadLock * fileLock = (FileLoadLock *)lock->FindFileLock(pFile);
        bool registerNewAssembly = false;
        if (fileLock == NULL)
        {
            // Check again in case we were racing
            result = FindAssembly(pFile, FindAssemblyOptions_IncludeFailedToLoad);
            if (result == NULL)
            {
                // We are the first one in - create the DomainAssembly
                registerNewAssembly = true;
                fileLock = FileLoadLock::Create(lock, pFile, pDomainAssembly);
                pDomainAssembly.SuppressRelease();
                if (pDomainAssembly->IsCollectible())
                {
                    // We add the assembly to the LoaderAllocator only when we are sure that it can be added
                    // and won't be deleted in case of a concurrent load from the same ALC
                    ((AssemblyLoaderAllocator *)pLoaderAllocator)->AddDomainAssembly(pDomainAssembly);
                }
            }
        }
        else
        {
            fileLock->AddRef();
        }

        lock.Release();

        if (result == NULL)
        {
            // We pass our ref on fileLock to LoadDomainFile to release.

            // Note that if we throw here, we will poison fileLock with an error condition,
            // so it will not be removed until app domain unload.  So there is no need
            // to release our ref count.
            result = (DomainAssembly *)LoadDomainFile(fileLock, targetLevel);
        }
        else
        {
            result->EnsureLoadLevel(targetLevel);
        }

        if (registerNewAssembly)
        {
            pFile->GetAssemblyLoadContext()->AddLoadedAssembly(pDomainAssembly->GetCurrentAssembly());
        }
    }
    else
        result->EnsureLoadLevel(targetLevel);

    // Malformed metadata may contain a Module reference to what is actually
    // an Assembly. In this case we need to throw an exception, since returning
    // a DomainModule as a DomainAssembly is a type safety violation.
    if (!result->IsAssembly())
    {
        ThrowHR(COR_E_ASSEMBLYEXPECTED);
    }

    // Cache result in all cases, since found pFile could be from a different AssemblyRef than pIdentity
    if (pIdentity == NULL)
    {
        AssemblySpec spec;
        spec.InitializeSpec(result->GetFile());
        GetAppDomain()->AddAssemblyToCache(&spec, result);
    }
    else
    {
        GetAppDomain()->AddAssemblyToCache(pIdentity, result);
    }

    RETURN result;
} // AppDomain::LoadDomainAssembly


struct LoadFileArgs
{
    FileLoadLock *pLock;
    FileLoadLevel targetLevel;
    DomainFile *result;
};

DomainFile *AppDomain::LoadDomainFile(FileLoadLock *pLock, FileLoadLevel targetLevel)
{
    CONTRACT(DomainFile *)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pLock));
        PRECONDITION(pLock->GetDomainFile()->GetAppDomain() == this);
        POSTCONDITION(RETVAL->GetLoadLevel() >= GetThreadFileLoadLevel()
                      || RETVAL->GetLoadLevel() >= targetLevel);
        POSTCONDITION(RETVAL->CheckNoError(targetLevel));
    }
    CONTRACT_END;

    DomainFile *pFile = pLock->GetDomainFile();

    // Make sure we release the lock on exit
    FileLoadLockRefHolder lockRef(pLock);

    // We need to perform the early steps of loading CoreLib without a domain transition.  This is
    // important for bootstrapping purposes - we need to get CoreLib at least partially loaded
    // into a domain before we can run serialization code to do the transition.
    //
    // Note that we cannot do this in general for all assemblies, because some of the security computations
    // require the managed exposed object, which must be created in the correct app domain.

    if (this != GetAppDomain()
        && pFile->GetFile()->IsSystem()
        && targetLevel > FILE_LOAD_ALLOCATE)
    {
        // Re-call the routine with a limited load level. This will cause the first part of the load to
        // get performed in the current app domain.

        pLock->AddRef();
        LoadDomainFile(pLock, targetLevel > FILE_LOAD_ALLOCATE ? FILE_LOAD_ALLOCATE : targetLevel);

        // Now continue on to complete the rest of the load, if any.
    }

    // Do a quick out check for the already loaded case.
    if (pLock->GetLoadLevel() >= targetLevel)
    {
        pFile->ThrowIfError(targetLevel);

        RETURN pFile;
    }

    // Initialize a loading queue.  This will hold any loads which are triggered recursively but
    // which cannot be immediately satisfied due to anti-deadlock constraints.

    // PendingLoadQueues are allocated on the stack during a load, and
    // shared with all nested loads on the same thread. (Note that we won't use
    // "candidate" if we are in a recursive load; that's OK since they are cheap to
    // construct.)
    FileLoadLevel immediateTargetLevel = targetLevel;
    {
        LoadLevelLimiter limit;
        limit.Activate();

        // We cannot set a target level higher than that allowed by the limiter currently.
        // This is because of anti-deadlock constraints.
        if (immediateTargetLevel > limit.GetLoadLevel())
            immediateTargetLevel = limit.GetLoadLevel();

        LOG((LF_LOADER, LL_INFO100, "LOADER: %x:***%s*\t>>>Load initiated, %s/%s\n",
             pFile->GetAppDomain(), pFile->GetSimpleName(),
             fileLoadLevelName[immediateTargetLevel], fileLoadLevelName[targetLevel]));

        // Now loop and do the load incrementally to the target level.
        if (pLock->GetLoadLevel() < immediateTargetLevel)
        {
            while (pLock->Acquire(immediateTargetLevel))
            {
                FileLoadLevel workLevel;
                {
                    FileLoadLockHolder fileLock(pLock);

                    // Work level is next step to do
                    workLevel = (FileLoadLevel)(fileLock->GetLoadLevel()+1);

                    // Set up the anti-deadlock constraint: we cannot safely recursively load any assemblies
                    // on this thread to a higher level than this assembly is being loaded now.
                    // Note that we do allow work at a parallel level; any deadlocks caused here will
                    // be resolved by the deadlock detection in the FileLoadLocks.
                    limit.SetLoadLevel(workLevel);

                    LOG((LF_LOADER,
                         (workLevel == FILE_LOAD_BEGIN
                          || workLevel == FILE_LOADED
                          || workLevel == FILE_ACTIVE)
                         ? LL_INFO10 : LL_INFO1000,
                         "LOADER: %p:***%s*\t   loading at level %s\n",
                         this, pFile->GetSimpleName(), fileLoadLevelName[workLevel]));

                    TryIncrementalLoad(pFile, workLevel, fileLock);
                }
            }

            if (pLock->GetLoadLevel() == immediateTargetLevel-1)
            {
                LOG((LF_LOADER, LL_INFO100, "LOADER: %x:***%s*\t<<<Load limited due to detected deadlock, %s\n",
                     pFile->GetAppDomain(), pFile->GetSimpleName(),
                     fileLoadLevelName[immediateTargetLevel-1]));
            }
        }

        LOG((LF_LOADER, LL_INFO100, "LOADER: %x:***%s*\t<<<Load completed, %s\n",
             pFile->GetAppDomain(), pFile->GetSimpleName(),
             fileLoadLevelName[pLock->GetLoadLevel()]));

    }

    // There may have been an error stored on the domain file by another thread, or from a previous load
    pFile->ThrowIfError(targetLevel);

    // There are two normal results from the above loop.
    //
    // 1. We succeeded in loading the file to the current thread's load level.
    // 2. We succeeded in loading the file to the current thread's load level - 1, due
    //      to deadlock condition with another thread loading the same assembly.
    //
    // Either of these are considered satisfactory results, as code inside a load must expect
    // a parial load result.
    //
    // However, if load level elevation has occurred, then it is possible for a deadlock to
    // prevent us from loading an assembly which was loading before the elevation at a radically
    // lower level.  In such a case, we throw an exception which transiently fails the current
    // load, since it is likely we have not satisfied the caller.
    // (An alternate, and possibly preferable, strategy here would be for all callers to explicitly
    // identify the minimum load level acceptable via CheckLoadDomainFile and throw from there.)

    pFile->RequireLoadLevel((FileLoadLevel)(immediateTargetLevel-1));


    RETURN pFile;
}

void AppDomain::TryIncrementalLoad(DomainFile *pFile, FileLoadLevel workLevel, FileLoadLockHolder &lockHolder)
{
    STANDARD_VM_CONTRACT;

    // This is factored out so we don't call EX_TRY in a loop (EX_TRY can _alloca)

    BOOL released = FALSE;
    FileLoadLock* pLoadLock = lockHolder.GetValue();

    EX_TRY
    {

        // Special case: for LoadLibrary, we cannot hold the lock during the
        // actual LoadLibrary call, because we might get a callback from _CorDllMain on any
        // other thread.  (Note that this requires DomainFile's LoadLibrary to be independently threadsafe.)

        if (workLevel == FILE_LOAD_LOADLIBRARY)
        {
            lockHolder.Release();
            released = TRUE;
        }

        // Do the work
        BOOL success = pFile->DoIncrementalLoad(workLevel);
        if (released)
        {
            // Reobtain lock to increment level. (Note that another thread may
            // have already done it which is OK.
            if (pLoadLock->Acquire(workLevel))
            {
                // note lockHolder.Acquire isn't wired up to actually take the lock
                lockHolder = pLoadLock;
                released = FALSE;
            }
        }

        if (!released)
        {
            // Complete the level.
            if (pLoadLock->CompleteLoadLevel(workLevel, success) &&
                pLoadLock->GetLoadLevel()==FILE_LOAD_DELIVER_EVENTS)
            {
                lockHolder.Release();
                released = TRUE;
                pFile->DeliverAsyncEvents();
            };
        }
    }
    EX_HOOK
    {
        Exception *pEx = GET_EXCEPTION();


        //We will cache this error and wire this load to forever fail,
        // unless the exception is transient or the file is loaded OK but just cannot execute
        if (!pEx->IsTransient() && !pFile->IsLoaded())
        {

            if (released)
            {
                // Reobtain lock to increment level. (Note that another thread may
                // have already done it which is OK.
                if (pLoadLock->Acquire(workLevel)) // note pLockHolder->Acquire isn't wired up to actually take the lock
                {
                    // note lockHolder.Acquire isn't wired up to actually take the lock
                    lockHolder = pLoadLock;
                    released = FALSE;
                }
            }

            if (!released)
            {
                // Report the error in the lock
                pLoadLock->SetError(pEx);
            }

            if (!EEFileLoadException::CheckType(pEx))
                EEFileLoadException::Throw(pFile->GetFile(), pEx->GetHR(), pEx);
        }

        // Otherwise, we simply abort this load, and can retry later on.
        // @todo cleanup: make sure that each level is restartable after an exception, and
        // leaves no bad side effects
    }
    EX_END_HOOK;
}

// Checks whether the module is valid to be in the given app domain (need not be yet loaded)
CHECK AppDomain::CheckValidModule(Module * pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pModule->GetDomainFile() != NULL)
        CHECK_OK;

    CHECK_OK;
}

void AppDomain::SetupSharedStatics()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: SetupSharedStatics()"));

    // don't do any work in init stage. If not init only do work in non-shared case if are default domain
    _ASSERTE(!g_fEEInit);

    // Because we are allocating/referencing objects, need to be in cooperative mode
    GCX_COOP();

    DomainLocalModule *pLocalModule = CoreLibBinder::GetModule()->GetDomainLocalModule();

    // This is a convenient place to initialize String.Empty.
    // It is treated as intrinsic by the JIT as so the static constructor would never run.
    // Leaving it uninitialized would confuse debuggers.

    // String should not have any static constructors.
    _ASSERTE(g_pStringClass->IsClassPreInited());

    FieldDesc * pEmptyStringFD = CoreLibBinder::GetField(FIELD__STRING__EMPTY);
    OBJECTREF* pEmptyStringHandle = (OBJECTREF*)
        ((TADDR)pLocalModule->GetPrecomputedGCStaticsBasePointer()+pEmptyStringFD->GetOffset());
    SetObjectReference( pEmptyStringHandle, StringObject::GetEmptyString());
}

DomainAssembly * AppDomain::FindAssembly(PEAssembly * pFile, FindAssemblyOptions options/* = FindAssemblyOptions_None*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    const bool includeFailedToLoad = (options & FindAssemblyOptions_IncludeFailedToLoad) != 0;

    if (pFile->HasHostAssembly())
    {
        DomainAssembly * pDA = FindAssembly(pFile->GetHostAssembly());
        if (pDA != nullptr && (pDA->IsLoaded() || (includeFailedToLoad && pDA->IsError())))
        {
            return pDA;
        }
        return nullptr;
    }

    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded |
        (includeFailedToLoad ? kIncludeFailedToLoad : 0) |
        kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (i.Next(pDomainAssembly.This()))
    {
        PEFile * pManifestFile = pDomainAssembly->GetFile();
        if (pManifestFile &&
            !pManifestFile->IsResource() &&
            pManifestFile->Equals(pFile))
        {
            // Caller already has PEAssembly, so we can give DomainAssembly away freely without AddRef
            return pDomainAssembly.Extract();
        }
    }
    return NULL;
}

void AppDomain::SetFriendlyName(LPCWSTR pwzFriendlyName, BOOL fDebuggerCares/*=TRUE*/)
{
    CONTRACTL
    {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Do all computations into a temporary until we're ensured of success
    SString tmpFriendlyName;


    if (pwzFriendlyName)
        tmpFriendlyName.Set(pwzFriendlyName);
    else
    {
        // If there is an assembly, try to get the name from it.
        // If no assembly, but if it's the DefaultDomain, then give it a name

        if (m_pRootAssembly)
        {
            tmpFriendlyName.SetUTF8(m_pRootAssembly->GetSimpleName());

            SString::Iterator i = tmpFriendlyName.End();
            if (tmpFriendlyName.FindBack(i, '.'))
                tmpFriendlyName.Truncate(i);
        }
        else
        {
            tmpFriendlyName.Set(DEFAULT_DOMAIN_FRIENDLY_NAME);
        }
    }

    tmpFriendlyName.Normalize();


    m_friendlyName = tmpFriendlyName;
    m_friendlyName.Normalize();

    if(g_pDebugInterface)
    {
        // update the name in the IPC publishing block
        if (SUCCEEDED(g_pDebugInterface->UpdateAppDomainEntryInIPC(this)))
        {
            // inform the attached debugger that the name of this appdomain has changed.
            if (IsDebuggerAttached() && fDebuggerCares)
                g_pDebugInterface->NameChangeEvent(this, NULL);
        }
    }
}

LPCWSTR AppDomain::GetFriendlyName(BOOL fDebuggerCares/*=TRUE*/)
{
    CONTRACT (LPCWSTR)
    {
        THROWS;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (m_friendlyName.IsEmpty())
        SetFriendlyName(NULL, fDebuggerCares);

    RETURN m_friendlyName;
}

LPCWSTR AppDomain::GetFriendlyNameForLogging()
{
    CONTRACT(LPCWSTR)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
    }
    CONTRACT_END;
    RETURN (m_friendlyName.IsEmpty() ?W(""):(LPCWSTR)m_friendlyName);
}

LPCWSTR AppDomain::GetFriendlyNameForDebugger()
{
    CONTRACT (LPCWSTR)
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;


    if (m_friendlyName.IsEmpty())
    {
        BOOL fSuccess = FALSE;

        EX_TRY
        {
            SetFriendlyName(NULL);

            fSuccess = TRUE;
        }
        EX_CATCH
        {
            // Gobble all exceptions.
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (!fSuccess)
        {
            RETURN W("");
        }
    }

    RETURN m_friendlyName;
}


#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE

PVOID AppDomain::GetFriendlyNameNoSet(bool* isUtf8)
{
    SUPPORTS_DAC;

    if (!m_friendlyName.IsEmpty())
    {
        *isUtf8 = false;
        return m_friendlyName.DacGetRawContent();
    }
    else if (m_pRootAssembly)
    {
        *isUtf8 = true;
        return (PVOID)m_pRootAssembly->GetSimpleName();
    }
    else if (dac_cast<TADDR>(this) ==
             dac_cast<TADDR>(SystemDomain::System()->DefaultDomain()))
    {
        *isUtf8 = false;
        return (PVOID)DEFAULT_DOMAIN_FRIENDLY_NAME;
    }
    else
    {
        return NULL;
    }
}

#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE

BOOL AppDomain::AddFileToCache(AssemblySpec* pSpec, PEAssembly *pFile, BOOL fAllowFailure)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSpec));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();
    DomainCacheCrstHolderForGCCoop holder(this);

    // !!! suppress exceptions
    if(!m_AssemblyCache.StoreFile(pSpec, pFile) && !fAllowFailure)
    {
        // TODO: Disabling the below assertion as currently we experience
        // inconsistency on resolving the Microsoft.Office.Interop.MSProject.dll
        // This causes below assertion to fire and crashes the VS. This issue
        // is being tracked with Dev10 Bug 658555. Brought back it when this bug
        // is fixed.
        // _ASSERTE(FALSE);

        EEFileLoadException::Throw(pSpec, FUSION_E_CACHEFILE_FAILED, NULL);
    }

    return TRUE;
}

BOOL AppDomain::AddAssemblyToCache(AssemblySpec* pSpec, DomainAssembly *pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSpec));
        PRECONDITION(CheckPointer(pAssembly));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();
    DomainCacheCrstHolderForGCCoop holder(this);

    // !!! suppress exceptions
    BOOL bRetVal = m_AssemblyCache.StoreAssembly(pSpec, pAssembly);
    return bRetVal;
}

BOOL AppDomain::AddExceptionToCache(AssemblySpec* pSpec, Exception *ex)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSpec));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (ex->IsTransient())
        return TRUE;

    GCX_PREEMP();
    DomainCacheCrstHolderForGCCoop holder(this);

    // !!! suppress exceptions
    return m_AssemblyCache.StoreException(pSpec, ex);
}

void AppDomain::AddUnmanagedImageToCache(LPCWSTR libraryName, NATIVE_LIBRARY_HANDLE hMod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(libraryName));
        PRECONDITION(CheckPointer(hMod));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DomainCacheCrstHolderForGCPreemp lock(this);

    const UnmanagedImageCacheEntry *existingEntry = m_unmanagedCache.LookupPtr(libraryName);
    if (existingEntry != NULL)
    {
        _ASSERTE(existingEntry->Handle == hMod);
        return;
    }

    size_t len = (wcslen(libraryName) + 1) * sizeof(WCHAR);
    AllocMemHolder<WCHAR> copiedName(GetLowFrequencyHeap()->AllocMem(S_SIZE_T(len)));
    memcpy(copiedName, libraryName, len);

    m_unmanagedCache.Add(UnmanagedImageCacheEntry{ copiedName, hMod });
    copiedName.SuppressRelease();
}

NATIVE_LIBRARY_HANDLE AppDomain::FindUnmanagedImageInCache(LPCWSTR libraryName)
{
    CONTRACT(NATIVE_LIBRARY_HANDLE)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(libraryName));
        POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DomainCacheCrstHolderForGCPreemp lock(this);

    const UnmanagedImageCacheEntry *existingEntry = m_unmanagedCache.LookupPtr(libraryName);
    if (existingEntry == NULL)
        RETURN NULL;

    RETURN existingEntry->Handle;
}

BOOL AppDomain::RemoveFileFromCache(PEAssembly *pFile)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pFile));
    }
    CONTRACTL_END;

    LoadLockHolder lock(this);
    FileLoadLock *fileLock = (FileLoadLock *)lock->FindFileLock(pFile);

    if (fileLock == NULL)
        return FALSE;

    VERIFY(lock->Unlink(fileLock));

    fileLock->Release();

    return TRUE;
}

BOOL AppDomain::RemoveAssemblyFromCache(DomainAssembly* pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();
    DomainCacheCrstHolderForGCCoop holder(this);

    return m_AssemblyCache.RemoveAssembly(pAssembly);
}

BOOL AppDomain::IsCached(AssemblySpec *pSpec)
{
    WRAPPER_NO_CONTRACT;

    // Check to see if this fits our rather loose idea of a reference to CoreLib.
    // If so, don't use fusion to bind it - do it ourselves.
    if (pSpec->IsCoreLib())
        return TRUE;

    return m_AssemblyCache.Contains(pSpec);
}

PEAssembly* AppDomain::FindCachedFile(AssemblySpec* pSpec, BOOL fThrow /*=TRUE*/)
{
    CONTRACTL
    {
        if (fThrow) {
            GC_TRIGGERS;
            THROWS;
        }
        else {
            GC_NOTRIGGER;
            NOTHROW;
        }
        MODE_ANY;
    }
    CONTRACTL_END;

    // Check to see if this fits our rather loose idea of a reference to CoreLib.
    // If so, don't use fusion to bind it - do it ourselves.
    if (fThrow && pSpec->IsCoreLib())
    {
        CONSISTENCY_CHECK(SystemDomain::System()->SystemAssembly() != NULL);
        PEAssembly *pFile = SystemDomain::System()->SystemFile();
        pFile->AddRef();
        return pFile;
    }

    return m_AssemblyCache.LookupFile(pSpec, fThrow);
}


BOOL AppDomain::PostBindResolveAssembly(AssemblySpec  *pPrePolicySpec,
                                        AssemblySpec  *pPostPolicySpec,
                                        HRESULT        hrBindResult,
                                        AssemblySpec **ppFailedSpec)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    PRECONDITION(CheckPointer(pPrePolicySpec));
    PRECONDITION(CheckPointer(pPostPolicySpec));
    PRECONDITION(CheckPointer(ppFailedSpec));

    BOOL fFailure = TRUE;
    *ppFailedSpec = pPrePolicySpec;

    PEAssemblyHolder result;

    if ((EEFileLoadException::GetFileLoadKind(hrBindResult) == kFileNotFoundException) ||
        (hrBindResult == FUSION_E_REF_DEF_MISMATCH) ||
        (hrBindResult == FUSION_E_INVALID_NAME))
    {
        result = TryResolveAssemblyUsingEvent(*ppFailedSpec);

        if (result != NULL)
        {
            fFailure = FALSE;

            // Given the post-policy resolve event construction of the CLR binder,
            // chained managed resolve events can race with each other, therefore we do allow
            // the adding of the result to fail. Checking for already chached specs
            // is not an option as it would introduce another race window.
            // The binder does a re-fetch of the
            // original binding spec and therefore will not cause inconsistency here.
            // For the purposes of the resolve event, failure to add to the cache still is a success.
            AddFileToCache(pPrePolicySpec, result, TRUE /* fAllowFailure */);
            if (*ppFailedSpec != pPrePolicySpec)
            {
                AddFileToCache(pPostPolicySpec, result, TRUE /* fAllowFailure */ );
            }
        }
    }

    return fFailure;
}

//---------------------------------------------------------------------------------------------------------------------
PEAssembly * AppDomain::BindAssemblySpec(
    AssemblySpec *         pSpec,
    BOOL                   fThrowOnFileNotFound)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    PRECONDITION(CheckPointer(pSpec));
    PRECONDITION(pSpec->GetAppDomain() == this);
    PRECONDITION(this==::GetAppDomain());

    GCX_PREEMP();

    BOOL fForceReThrow = FALSE;

    BinderTracing::AssemblyBindOperation bindOperation(pSpec);

    HRESULT hrBindResult = S_OK;
    PEAssemblyHolder result;

    bool isCached = false;
    EX_TRY
    {
        isCached = IsCached(pSpec);
        if (!isCached)
        {

            {
                // Use CoreClr's fusion alternative
                CoreBindResult bindResult;

                pSpec->Bind(this, FALSE /* fThrowOnFileNotFound */, &bindResult);
                hrBindResult = bindResult.GetHRBindResult();

                if (bindResult.Found())
                {
                    if (SystemDomain::SystemFile() && bindResult.IsCoreLib())
                    {
                        // Avoid rebinding to another copy of CoreLib
                        result = SystemDomain::SystemFile();
                        result.SuppressRelease(); // Didn't get a refcount
                    }
                    else
                    {
                        // IsSystem on the PEFile should be false, even for CoreLib satellites
                        result = PEAssembly::Open(&bindResult,
                                                    FALSE);
                    }

                    // Setup the reference to the binder, which performed the bind, into the AssemblySpec
                    AssemblyBinder* pBinder = result->GetBindingContext();
                    _ASSERTE(pBinder != NULL);
                    pSpec->SetBindingContext(pBinder);

                    // Failure to add simply means someone else beat us to it. In that case
                    // the FindCachedFile call below (after catch block) will update result
                    // to the cached value.
                    AddFileToCache(pSpec, result, TRUE /*fAllowFailure*/);
                }
                else
                {
                    // Don't trigger the resolve event for the CoreLib satellite assembly. A misbehaving resolve event may
                    // return an assembly that does not match, and this can cause recursive resource lookups during error
                    // reporting. The CoreLib satellite assembly is loaded from relative locations based on the culture, see
                    // AssemblySpec::Bind().
                    if (!pSpec->IsCoreLibSatellite())
                    {
                        // Trigger the resolve event also for non-throw situation.
                        AssemblySpec NewSpec(this);
                        AssemblySpec *pFailedSpec = NULL;

                        fForceReThrow = TRUE; // Managed resolve event handler can throw

                        BOOL fFailure = PostBindResolveAssembly(pSpec, &NewSpec, hrBindResult, &pFailedSpec);

                        if (fFailure && fThrowOnFileNotFound)
                        {
                            EEFileLoadException::Throw(pFailedSpec, COR_E_FILENOTFOUND, NULL);
                        }
                    }
                }
            }
        }
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        AssemblySpec NewSpec(this);
        AssemblySpec *pFailedSpec = NULL;

        // Let transient exceptions or managed resolve event handler exceptions propagate
        if (ex->IsTransient() || fForceReThrow)
        {
            EX_RETHROW;
        }

        {
            BOOL fFailure = PostBindResolveAssembly(pSpec, &NewSpec, ex->GetHR(), &pFailedSpec);
            if (fFailure)
            {
                BOOL bFileNotFoundException =
                    (EEFileLoadException::GetFileLoadKind(ex->GetHR()) == kFileNotFoundException);

                if (!bFileNotFoundException)
                {
                    fFailure = AddExceptionToCache(pFailedSpec, ex);
                } // else, fFailure stays TRUE
                // Effectively, fFailure == bFileNotFoundException || AddExceptionToCache(pFailedSpec, ex)

                // Only throw this exception if we are the first in the cache
                if (fFailure)
                {
                    // Store the failure information for DAC to read
                    if (IsDebuggerAttached()) {
                        FailedAssembly *pFailed = new FailedAssembly();
                        pFailed->Initialize(pFailedSpec, ex);
                        IfFailThrow(m_failedAssemblies.Append(pFailed));
                    }

                    if (!bFileNotFoundException || fThrowOnFileNotFound)
                    {
                        // V1.1 App-compatibility workaround. See VSW530166 if you want to whine about it.
                        //
                        // In Everett, if we failed to download an assembly because of a broken network cable,
                        // we returned a FileNotFoundException with a COR_E_FILENOTFOUND hr embedded inside
                        // (which would be exposed when marshaled to native.)
                        //
                        // In Whidbey, we now set the more appropriate INET_E_RESOURCE_NOT_FOUND hr. But
                        // the online/offline switch code in VSTO for Everett hardcoded a check for
                        // COR_E_FILENOTFOUND.
                        //
                        // So now, to keep that code from breaking, we have to remap INET_E_RESOURCE_NOT_FOUND
                        // back to COR_E_FILENOTFOUND. We're doing it here rather down in Fusion so as to affect
                        // the least number of callers.

                        if (ex->GetHR() == INET_E_RESOURCE_NOT_FOUND)
                        {
                            EEFileLoadException::Throw(pFailedSpec, COR_E_FILENOTFOUND, ex);
                        }

                        if (EEFileLoadException::CheckType(ex))
                        {
                            if (pFailedSpec == pSpec)
                            {
                                EX_RETHROW; //preserve the information
                            }
                            else
                            {
                                StackSString exceptionDisplayName, failedSpecDisplayName;

                                ((EEFileLoadException*)ex)->GetName(exceptionDisplayName);
                                pFailedSpec->GetFileOrDisplayName(0, failedSpecDisplayName);

                                if (exceptionDisplayName.CompareCaseInsensitive(failedSpecDisplayName) == 0)
                                {
                                    EX_RETHROW; // Throw the original exception. Otherwise, we'd throw an exception that contains the same message twice.
                                }
                            }
                        }

                        EEFileLoadException::Throw(pFailedSpec, ex->GetHR(), ex);
                    }

                }
            }
        }
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    // Now, if it's a cacheable bind we need to re-fetch the result from the cache, as we may have been racing with another
    // thread to store our result.  Note that we may throw from here, if there is a cached exception.
    // This will release the refcount of the current result holder (if any), and will replace
    // it with a non-addref'ed result
    result = FindCachedFile(pSpec);

    if (result != NULL)
        result->AddRef();

    bindOperation.SetResult(result.GetValue(), isCached);
    return result.Extract();
} // AppDomain::BindAssemblySpec



PEAssembly *AppDomain::TryResolveAssemblyUsingEvent(AssemblySpec *pSpec)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    // No assembly resolve on codebase binds
    if (pSpec->GetName() == nullptr)
        return nullptr;

    PEAssembly *result = nullptr;
    EX_TRY
    {
        Assembly *pAssembly = RaiseAssemblyResolveEvent(pSpec);
        if (pAssembly != nullptr)
        {
            PEAssembly *pFile = pAssembly->GetManifestFile();
            pFile->AddRef();
            result = pFile;
        }

        BinderTracing::ResolutionAttemptedOperation::TraceAppDomainAssemblyResolve(pSpec, result);
    }
    EX_HOOK
    {
        Exception *pEx = GET_EXCEPTION();
        BinderTracing::ResolutionAttemptedOperation::TraceAppDomainAssemblyResolve(pSpec, nullptr, pEx);
        if (!pEx->IsTransient())
        {
            AddExceptionToCache(pSpec, pEx);
            if (!EEFileLoadException::CheckType(pEx))
                EEFileLoadException::Throw(pSpec, pEx->GetHR(), pEx);
        }
    }
    EX_END_HOOK;

    return result;
}


ULONG AppDomain::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return InterlockedIncrement(&m_cRef);
}

ULONG AppDomain::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_cRef > 0);
    }
    CONTRACTL_END;

    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef)
    {
        _ASSERTE (m_Stage == STAGE_CREATING);
        delete this;
    }
    return (cRef);
}



void AppDomain::RaiseLoadingAssemblyEvent(DomainAssembly *pAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(this == GetAppDomain());
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pAssembly->GetFile()->IsSystem())
    {
        return;
    }

    GCX_COOP();
    FAULT_NOT_FATAL();
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    EX_TRY
    {
        if (CoreLibBinder::GetField(FIELD__ASSEMBLYLOADCONTEXT__ASSEMBLY_LOAD)->GetStaticOBJECTREF() != NULL)
        {
            struct _gc {
                OBJECTREF    orThis;
            } gc;
            ZeroMemory(&gc, sizeof(gc));

            ARG_SLOT args[1];
            GCPROTECT_BEGIN(gc);

            gc.orThis = pAssembly->GetExposedAssemblyObject();

            MethodDescCallSite onAssemblyLoad(METHOD__ASSEMBLYLOADCONTEXT__ON_ASSEMBLY_LOAD);

            // GetExposedAssemblyObject may cause a gc, so call this before filling args[0]
            args[0] = ObjToArgSlot(gc.orThis);

            onAssemblyLoad.Call(args);

            GCPROTECT_END();
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

BOOL AppDomain::OnUnhandledException(OBJECTREF *pThrowable, BOOL isTerminating/*=TRUE*/)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    BOOL retVal = FALSE;

    GCX_COOP();

    EX_TRY
    {
        retVal = GetAppDomain()->RaiseUnhandledExceptionEvent(pThrowable, isTerminating);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)  // Swallow any errors.

    return retVal;
}

void AppDomain::RaiseExitProcessEvent()
{
    if (!g_fEEStarted)
        return;

    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Only finalizer thread during shutdown can call this function.
    _ASSERTE ((g_fEEShutDown&ShutDown_Finalize1) && GetThread() == FinalizerThread::GetFinalizerThread());

    _ASSERTE (GetThread()->PreemptiveGCDisabled());

    MethodDescCallSite onProcessExit(METHOD__APPCONTEXT__ON_PROCESS_EXIT);
    onProcessExit.Call(NULL);
}

BOOL
AppDomain::RaiseUnhandledExceptionEvent(OBJECTREF *pThrowable, BOOL isTerminating)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    _ASSERTE(pThrowable != NULL && IsProtectedByGCFrame(pThrowable));

    _ASSERTE(this == GetThread()->GetDomain());

    OBJECTREF orDelegate = CoreLibBinder::GetField(FIELD__APPCONTEXT__UNHANDLED_EXCEPTION)->GetStaticOBJECTREF();
    if (orDelegate == NULL)
        return FALSE;

    struct _gc {
        OBJECTREF Delegate;
        OBJECTREF Sender;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    gc.Delegate = orDelegate;
    if (orDelegate != NULL)
    {
        DistributeUnhandledExceptionReliably(&gc.Delegate, &gc.Sender, pThrowable, isTerminating);
    }
    GCPROTECT_END();
    return TRUE;
}


DefaultAssemblyBinder *AppDomain::CreateBinderContext()
{
    CONTRACT(DefaultAssemblyBinder *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (!m_pTPABinderContext)
    {
        ETWOnStartup (FusionAppCtx_V1, FusionAppCtxEnd_V1);

        GCX_PREEMP();

        // Initialize the assembly binder for the default context loads for CoreCLR.
        IfFailThrow(BINDER_SPACE::AssemblyBinderCommon::DefaultBinderSetupContext(&m_pTPABinderContext));
    }

    RETURN m_pTPABinderContext;
}



//---------------------------------------------------------------------------------------
//
// AppDomain::IsDebuggerAttached - is a debugger attached to this process
//
// Arguments:
//    None
//
// Return Value:
//    TRUE if a debugger is attached to this process, FALSE otherwise.
//
// Notes:
//    This is identical to CORDebuggerAttached.  This exists idependantly for legacy reasons - we used to
//    support attaching to individual AppDomains.  This should probably go away eventually.
//

BOOL AppDomain::IsDebuggerAttached()
{
    LIMITED_METHOD_CONTRACT;

    if (CORDebuggerAttached())
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

#ifdef DEBUGGING_SUPPORTED

// This is called from the debugger to request notification events from
// Assemblies, Modules, Types in this appdomain.
BOOL AppDomain::NotifyDebuggerLoad(int flags, BOOL attaching)
{
    WRAPPER_NO_CONTRACT;
    BOOL result = FALSE;

    if (!attaching && !IsDebuggerAttached())
        return FALSE;

    AssemblyIterator i;

    // Attach to our assemblies
    LOG((LF_CORDB, LL_INFO100, "AD::NDA: Iterating assemblies\n"));
    i = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeLoading | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (i.Next(pDomainAssembly.This()))
    {
        result = (pDomainAssembly->NotifyDebuggerLoad(flags, attaching) ||
                  result);
    }

    return result;
}

void AppDomain::NotifyDebuggerUnload()
{
    WRAPPER_NO_CONTRACT;
    if (!IsDebuggerAttached())
        return;

    LOG((LF_CORDB, LL_INFO10, "AD::NDD domain %#08x %ls\n",
         this, GetFriendlyNameForLogging()));

    LOG((LF_CORDB, LL_INFO100, "AD::NDD: Interating domain bound assemblies\n"));
    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded |  kIncludeLoading  | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    // Detach from our assemblies
    while (i.Next(pDomainAssembly.This()))
    {
        LOG((LF_CORDB, LL_INFO100, "AD::NDD: Iterating assemblies\n"));
        pDomainAssembly->NotifyDebuggerUnload();
    }
}
#endif // DEBUGGING_SUPPORTED


#ifdef FEATURE_COMWRAPPERS

RCWRefCache *AppDomain::GetRCWRefCache()
{
    CONTRACT(RCWRefCache*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (!m_pRCWRefCache) {
        NewHolder<RCWRefCache> pRCWRefCache = new RCWRefCache(this);
        if (FastInterlockCompareExchangePointer(&m_pRCWRefCache, (RCWRefCache *)pRCWRefCache, NULL) == NULL)
        {
            pRCWRefCache.SuppressRelease();
        }
    }
    RETURN m_pRCWRefCache;
}
#endif // FEATURE_COMWRAPPERS

#ifdef FEATURE_COMINTEROP

RCWCache *AppDomain::CreateRCWCache()
{
    CONTRACT(RCWCache*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Initialize the global RCW cleanup list here as well. This is so that it
    // it guaranteed to exist if any RCW's are created, but it is not created
    // unconditionally.
    if (!g_pRCWCleanupList)
    {
        SystemDomain::LockHolder lh;

        if (!g_pRCWCleanupList)
            g_pRCWCleanupList = new RCWCleanupList();
    }
    _ASSERTE(g_pRCWCleanupList);

    {
        BaseDomain::LockHolder lh(this);

        if (!m_pRCWCache)
            m_pRCWCache = new RCWCache(this);
    }

    RETURN m_pRCWCache;
}

void AppDomain::ReleaseRCWs(LPVOID pCtxCookie)
{
    WRAPPER_NO_CONTRACT;
    if (m_pRCWCache)
        m_pRCWCache->ReleaseWrappersWorker(pCtxCookie);
}

void AppDomain::DetachRCWs()
{
    WRAPPER_NO_CONTRACT;
    if (m_pRCWCache)
        m_pRCWCache->DetachWrappersWorker();
}

#endif // FEATURE_COMINTEROP

void AppDomain::ExceptionUnwind(Frame *pFrame)
{
    CONTRACTL
    {
        DISABLED(GC_TRIGGERS);  // EEResourceException
        DISABLED(THROWS);   // EEResourceException
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::ExceptionUnwind for %8.8x\n", pFrame));
    Thread *pThread = GetThread();

    LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::ExceptionUnwind: not first transition or abort\n"));
}


#endif // !DACCESS_COMPILE

DWORD DomainLocalModule::GetClassFlags(MethodTable* pMT, DWORD iClassIndex /*=(DWORD)-1*/)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    {
        CONSISTENCY_CHECK(GetDomainFile()->GetModule() == pMT->GetModuleForStatics());
    }

    if (pMT->IsDynamicStatics())
    {
        _ASSERTE(!pMT->ContainsGenericVariables());
        DWORD dynamicClassID = pMT->GetModuleDynamicEntryID();
        if(m_aDynamicEntries <= dynamicClassID)
            return FALSE;
        return (m_pDynamicClassTable[dynamicClassID].m_dwFlags);
    }
    else
    {
        if (iClassIndex == (DWORD)-1)
            iClassIndex = pMT->GetClassIndex();
        return GetPrecomputedStaticsClassData()[iClassIndex];
    }
}

#ifndef DACCESS_COMPILE

void DomainLocalModule::SetClassInitialized(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BaseDomain::DomainLocalBlockLockHolder lh(GetDomainFile()->GetAppDomain());

    _ASSERTE(!IsClassInitialized(pMT));
    _ASSERTE(!IsClassInitError(pMT));

    SetClassFlags(pMT, ClassInitFlags::INITIALIZED_FLAG);
}

void DomainLocalModule::SetClassInitError(MethodTable* pMT)
{
    WRAPPER_NO_CONTRACT;

    BaseDomain::DomainLocalBlockLockHolder lh(GetDomainFile()->GetAppDomain());

    SetClassFlags(pMT, ClassInitFlags::ERROR_FLAG);
}

void DomainLocalModule::SetClassFlags(MethodTable* pMT, DWORD dwFlags)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(GetDomainFile()->GetModule() == pMT->GetModuleForStatics());
        // Assumes BaseDomain::DomainLocalBlockLockHolder is taken
        PRECONDITION(GetDomainFile()->GetAppDomain()->OwnDomainLocalBlockLock());
    } CONTRACTL_END;

    if (pMT->IsDynamicStatics())
    {
        _ASSERTE(!pMT->ContainsGenericVariables());
        DWORD dwID = pMT->GetModuleDynamicEntryID();
        EnsureDynamicClassIndex(dwID);
        m_pDynamicClassTable[dwID].m_dwFlags |= dwFlags;
    }
    else
    {
        GetPrecomputedStaticsClassData()[pMT->GetClassIndex()] |= dwFlags;
    }
}

void DomainLocalModule::EnsureDynamicClassIndex(DWORD dwID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        // Assumes BaseDomain::DomainLocalBlockLockHolder is taken
        PRECONDITION(GetDomainFile()->GetAppDomain()->OwnDomainLocalBlockLock());
    }
    CONTRACTL_END;

    if (dwID < m_aDynamicEntries)
    {
        _ASSERTE(m_pDynamicClassTable.Load() != NULL);
        return;
    }

    SIZE_T aDynamicEntries = max(16, m_aDynamicEntries.Load());
    while (aDynamicEntries <= dwID)
    {
        aDynamicEntries *= 2;
    }

    DynamicClassInfo* pNewDynamicClassTable;
    pNewDynamicClassTable = (DynamicClassInfo*)
        (void*)GetDomainFile()->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(
            S_SIZE_T(sizeof(DynamicClassInfo)) * S_SIZE_T(aDynamicEntries));

    memcpy(pNewDynamicClassTable, m_pDynamicClassTable, sizeof(DynamicClassInfo) * m_aDynamicEntries);

    // Note: Memory allocated on loader heap is zero filled
    // memset(pNewDynamicClassTable + m_aDynamicEntries, 0, (aDynamicEntries - m_aDynamicEntries) * sizeof(DynamicClassInfo));

    _ASSERTE(m_aDynamicEntries%2 == 0);

    // Commit new dynamic table. The lock-free helpers depend on the order.
    MemoryBarrier();
    m_pDynamicClassTable = pNewDynamicClassTable;
    MemoryBarrier();
    m_aDynamicEntries = aDynamicEntries;
}

void    DomainLocalModule::AllocateDynamicClass(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        // Assumes BaseDomain::DomainLocalBlockLockHolder is taken
        PRECONDITION(GetDomainFile()->GetAppDomain()->OwnDomainLocalBlockLock());
    }
    CONTRACTL_END;

    _ASSERTE(!pMT->ContainsGenericVariables());
    _ASSERTE(!pMT->IsSharedByGenericInstantiations());
    _ASSERTE(GetDomainFile()->GetModule() == pMT->GetModuleForStatics());
    _ASSERTE(pMT->IsDynamicStatics());

    DWORD dynamicEntryIDIndex = pMT->GetModuleDynamicEntryID();

    EnsureDynamicClassIndex(dynamicEntryIDIndex);

    _ASSERTE(m_aDynamicEntries > dynamicEntryIDIndex);

    EEClass *pClass = pMT->GetClass();

    DWORD dwStaticBytes = pClass->GetNonGCRegularStaticFieldBytes();
    DWORD dwNumHandleStatics = pClass->GetNumHandleRegularStatics();

    _ASSERTE(!IsClassAllocated(pMT));
    _ASSERTE(!IsClassInitialized(pMT));
    _ASSERTE(!IsClassInitError(pMT));

    DynamicEntry *pDynamicStatics = m_pDynamicClassTable[dynamicEntryIDIndex].m_pDynamicEntry;

    // We need this check because maybe a class had a cctor but no statics
    if (dwStaticBytes > 0 || dwNumHandleStatics > 0)
    {
        if (pDynamicStatics == NULL)
        {
            LoaderHeap * pLoaderAllocator = GetDomainFile()->GetLoaderAllocator()->GetHighFrequencyHeap();

            if (pMT->Collectible())
            {
                pDynamicStatics = (DynamicEntry*)(void*)pLoaderAllocator->AllocMem(S_SIZE_T(sizeof(CollectibleDynamicEntry)));
            }
            else
            {
                SIZE_T dynamicEntrySize = DynamicEntry::GetOffsetOfDataBlob() + dwStaticBytes;

#ifdef FEATURE_64BIT_ALIGNMENT
                // Allocate memory with extra alignment only if it is really necessary
                if (dwStaticBytes >= MAX_PRIMITIVE_FIELD_SIZE)
                {
                    static_assert_no_msg(sizeof(NormalDynamicEntry) % MAX_PRIMITIVE_FIELD_SIZE == 0);
                    pDynamicStatics = (DynamicEntry*)(void*)pLoaderAllocator->AllocAlignedMem(dynamicEntrySize, MAX_PRIMITIVE_FIELD_SIZE);
                }
                else
#endif
                    pDynamicStatics = (DynamicEntry*)(void*)pLoaderAllocator->AllocMem(S_SIZE_T(dynamicEntrySize));
            }

            // Note: Memory allocated on loader heap is zero filled

            m_pDynamicClassTable[dynamicEntryIDIndex].m_pDynamicEntry = pDynamicStatics;
        }

        if (pMT->Collectible() && (dwStaticBytes != 0))
        {
            GCX_COOP();
            OBJECTREF nongcStaticsArray = NULL;
            GCPROTECT_BEGIN(nongcStaticsArray);
#ifdef FEATURE_64BIT_ALIGNMENT
            // Allocate memory with extra alignment only if it is really necessary
            if (dwStaticBytes >= MAX_PRIMITIVE_FIELD_SIZE)
                nongcStaticsArray = AllocatePrimitiveArray(ELEMENT_TYPE_I8, (dwStaticBytes + (sizeof(CLR_I8)-1)) / (sizeof(CLR_I8)));
            else
#endif
                nongcStaticsArray = AllocatePrimitiveArray(ELEMENT_TYPE_U1, dwStaticBytes);
            ((CollectibleDynamicEntry *)pDynamicStatics)->m_hNonGCStatics = GetDomainFile()->GetModule()->GetLoaderAllocator()->AllocateHandle(nongcStaticsArray);
            GCPROTECT_END();
        }
        if (dwNumHandleStatics > 0)
        {
            if (!pMT->Collectible())
            {
                GetAppDomain()->AllocateStaticFieldObjRefPtrs(dwNumHandleStatics,
                                                              &((NormalDynamicEntry *)pDynamicStatics)->m_pGCStatics);
            }
            else
            {
                GCX_COOP();
                OBJECTREF gcStaticsArray = NULL;
                GCPROTECT_BEGIN(gcStaticsArray);
                gcStaticsArray = AllocateObjectArray(dwNumHandleStatics, g_pObjectClass);
                ((CollectibleDynamicEntry *)pDynamicStatics)->m_hGCStatics = GetDomainFile()->GetModule()->GetLoaderAllocator()->AllocateHandle(gcStaticsArray);
                GCPROTECT_END();
            }
        }
    }
}


void DomainLocalModule::PopulateClass(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!pMT->ContainsGenericVariables());

    // <todo> the only work actually done here for non-dynamics is the freezing related work.
    // See if we can eliminate this and make this a dynamic-only path </todo>
    DWORD iClassIndex = pMT->GetClassIndex();

    if (!IsClassAllocated(pMT, iClassIndex))
    {
        BaseDomain::DomainLocalBlockLockHolder lh(GetDomainFile()->GetAppDomain());

        if (!IsClassAllocated(pMT, iClassIndex))
        {
            // Allocate dynamic space if necessary
            if (pMT->IsDynamicStatics())
                AllocateDynamicClass(pMT);

            // determine flags to set on the statics block
            DWORD dwFlags = ClassInitFlags::ALLOCATECLASS_FLAG;

            if (!pMT->HasClassConstructor() && !pMT->HasBoxedRegularStatics())
            {
                _ASSERTE(!IsClassInitialized(pMT));
                _ASSERTE(!IsClassInitError(pMT));
                dwFlags |= ClassInitFlags::INITIALIZED_FLAG;
            }

            if (pMT->Collectible())
            {
                dwFlags |= ClassInitFlags::COLLECTIBLE_FLAG;
            }

            // Set all flags at the same time to avoid races
            SetClassFlags(pMT, dwFlags);
        }
    }

    return;
}


DomainAssembly* AppDomain::RaiseTypeResolveEventThrowing(DomainAssembly* pAssembly, LPCSTR szName, ASSEMBLYREF *pResultingAssemblyRef)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    DomainAssembly* pResolvedAssembly = NULL;
    _ASSERTE(strcmp(szName, g_AppDomainClassName));

    GCX_COOP();

    struct _gc {
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    if (pAssembly != NULL)
        gc.AssemblyRef = pAssembly->GetExposedAssemblyObject();

    MethodDescCallSite onTypeResolve(METHOD__ASSEMBLYLOADCONTEXT__ON_TYPE_RESOLVE);

    gc.str = StringObject::NewString(szName);
    ARG_SLOT args[2] =
    {
        ObjToArgSlot(gc.AssemblyRef),
        ObjToArgSlot(gc.str)
    };
    ASSEMBLYREF ResultingAssemblyRef = (ASSEMBLYREF) onTypeResolve.Call_RetOBJECTREF(args);

    if (ResultingAssemblyRef != NULL)
    {
        pResolvedAssembly = ResultingAssemblyRef->GetDomainAssembly();

        if (pResultingAssemblyRef)
            *pResultingAssemblyRef = ResultingAssemblyRef;
        else
        {
            if (pResolvedAssembly->IsCollectible())
            {
                COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
            }
        }
    }
    GCPROTECT_END();

    return pResolvedAssembly;
}


Assembly* AppDomain::RaiseResourceResolveEvent(DomainAssembly* pAssembly, LPCSTR szName)
{
    CONTRACT(Assembly*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    Assembly* pResolvedAssembly = NULL;

    GCX_COOP();

    struct _gc {
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    if (pAssembly != NULL)
        gc.AssemblyRef=pAssembly->GetExposedAssemblyObject();

    MethodDescCallSite onResourceResolve(METHOD__ASSEMBLYLOADCONTEXT__ON_RESOURCE_RESOLVE);
    gc.str = StringObject::NewString(szName);
    ARG_SLOT args[2] =
    {
        ObjToArgSlot(gc.AssemblyRef),
        ObjToArgSlot(gc.str)
    };
    ASSEMBLYREF ResultingAssemblyRef = (ASSEMBLYREF) onResourceResolve.Call_RetOBJECTREF(args);
    if (ResultingAssemblyRef != NULL)
    {
        pResolvedAssembly = ResultingAssemblyRef->GetAssembly();
        if (pResolvedAssembly->IsCollectible())
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleAssemblyResolve"));
        }
    }
    GCPROTECT_END();

    RETURN pResolvedAssembly;
}


Assembly *
AppDomain::RaiseAssemblyResolveEvent(
    AssemblySpec * pSpec)
{
    CONTRACT(Assembly*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    StackSString ssName;
    pSpec->GetFileOrDisplayName(0, ssName);

    // Elevate threads allowed loading level.  This allows the host to load an assembly even in a restricted
    // condition.  Note, however, that this exposes us to possible recursion failures, if the host tries to
    // load the assemblies currently being loaded.  (Such cases would then throw an exception.)

    OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    GCX_COOP();

    Assembly* pAssembly = NULL;

    struct _gc {
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    {
        if (pSpec->GetParentAssembly() != NULL)
        {
            gc.AssemblyRef=pSpec->GetParentAssembly()->GetExposedAssemblyObject();
        }

        MethodDescCallSite onAssemblyResolve(METHOD__ASSEMBLYLOADCONTEXT__ON_ASSEMBLY_RESOLVE);

        gc.str = StringObject::NewString(ssName);
        ARG_SLOT args[2] = {
            ObjToArgSlot(gc.AssemblyRef),
            ObjToArgSlot(gc.str)
        };

        ASSEMBLYREF ResultingAssemblyRef = (ASSEMBLYREF) onAssemblyResolve.Call_RetOBJECTREF(args);

        if (ResultingAssemblyRef != NULL)
        {
            pAssembly = ResultingAssemblyRef->GetAssembly();
            if (pAssembly->IsCollectible())
            {
                COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleAssemblyResolve"));
            }
        }
    }
    GCPROTECT_END();

    if (pAssembly != NULL)
    {
        // Check that the public key token matches the one specified in the spec
        // MatchPublicKeys throws as appropriate
        pSpec->MatchPublicKeys(pAssembly);
    }

    RETURN pAssembly;
} // AppDomain::RaiseAssemblyResolveEvent

enum WorkType
{
    WT_UnloadDomain = 0x1,
    WT_ThreadAbort = 0x2,
    WT_FinalizerThread = 0x4
};

static Volatile<DWORD> s_WorkType = 0;

void SystemDomain::ProcessDelayedUnloadLoaderAllocators()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int iGCRefPoint=GCHeapUtilities::GetGCHeap()->CollectionCount(GCHeapUtilities::GetGCHeap()->GetMaxGeneration());
    if (GCHeapUtilities::GetGCHeap()->IsConcurrentGCInProgress())
        iGCRefPoint--;

    LoaderAllocator * pAllocatorsToDelete = NULL;

    {
        CrstHolder lh(&m_DelayedUnloadCrst);

        LoaderAllocator ** ppAllocator=&m_pDelayedUnloadListOfLoaderAllocators;
        while (*ppAllocator!= NULL)
        {
            LoaderAllocator * pAllocator = *ppAllocator;
            if (0 < iGCRefPoint - pAllocator->GetGCRefPoint())
            {
                *ppAllocator = pAllocator->m_pLoaderAllocatorDestroyNext;

                pAllocator->m_pLoaderAllocatorDestroyNext = pAllocatorsToDelete;
                pAllocatorsToDelete = pAllocator;
            }
            else
            {
                ppAllocator = &pAllocator->m_pLoaderAllocatorDestroyNext;
            }
        }
    }

    // Delete collected loader allocators on the finalizer thread. We cannot offload it to appdomain unload thread because of
    // there is not guaranteed to be one, and it is not that expensive operation anyway.
    while (pAllocatorsToDelete != NULL)
    {
        LoaderAllocator * pAllocator = pAllocatorsToDelete;
        pAllocatorsToDelete = pAllocator->m_pLoaderAllocatorDestroyNext;
        delete pAllocator;
    }
}


void AppDomain::EnumStaticGCRefs(promote_func* fn, ScanContext* sc)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    _ASSERTE(GCHeapUtilities::IsGCInProgress() &&
             GCHeapUtilities::IsServerHeap()   &&
             IsGCSpecialThread());

    if (m_pPinnedHeapHandleTable != nullptr)
    {
        m_pPinnedHeapHandleTable->EnumStaticGCRefs(fn, sc);
    }

    RETURN;
}

#endif // !DACCESS_COMPILE

//------------------------------------------------------------------------
PTR_LoaderAllocator BaseDomain::GetLoaderAllocator()
{
    WRAPPER_NO_CONTRACT;
    return SystemDomain::GetGlobalLoaderAllocator(); // The one and only domain is not unloadable
}

//------------------------------------------------------------------------
UINT32 BaseDomain::GetTypeID(PTR_MethodTable pMT) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pMT->GetDomain() == this);
    } CONTRACTL_END;

    return m_typeIDMap.GetTypeID(pMT);
}

//------------------------------------------------------------------------
// Returns the ID of the type if found. If not found, returns INVALID_TYPE_ID
UINT32 BaseDomain::LookupTypeID(PTR_MethodTable pMT)
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(pMT->GetDomain() == this);
    } CONTRACTL_END;

    return m_typeIDMap.LookupTypeID(pMT);
}

//------------------------------------------------------------------------
PTR_MethodTable BaseDomain::LookupType(UINT32 id) {
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        CONSISTENCY_CHECK(id != TYPE_ID_THIS_CLASS);
    } CONTRACTL_END;

    PTR_MethodTable pMT = m_typeIDMap.LookupType(id);

    CONSISTENCY_CHECK(CheckPointer(pMT));
    CONSISTENCY_CHECK(pMT->IsInterface());
    return pMT;
}

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
void BaseDomain::RemoveTypesFromTypeIDMap(LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_typeIDMap.RemoveTypes(pLoaderAllocator);
}
#endif // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
BOOL
AppDomain::AssemblyIterator::Next(
    CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder)
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
        MODE_ANY;
    } CONTRACTL_END;

    CrstHolder ch(m_pAppDomain->GetAssemblyListLock());
    return Next_Unlocked(pDomainAssemblyHolder);
}

//---------------------------------------------------------------------------------------
//
// Note: Does not lock the assembly list, but locks collectible assemblies for adding references.
//
BOOL
AppDomain::AssemblyIterator::Next_Unlocked(
    CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

#ifndef DACCESS_COMPILE
    _ASSERTE(m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
#endif

    while (m_Iterator.Next())
    {
        // Get element from the list/iterator (without adding reference to the assembly)
        DomainAssembly * pDomainAssembly = dac_cast<PTR_DomainAssembly>(m_Iterator.GetElement());
        if (pDomainAssembly == NULL)
        {
            continue;
        }

        if (pDomainAssembly->IsError())
        {
            if (m_assemblyIterationFlags & kIncludeFailedToLoad)
            {
                *pDomainAssemblyHolder = pDomainAssembly;
                return TRUE;
            }
            continue; // reject
        }

        // First, reject DomainAssemblies whose load status is not to be included in
        // the enumeration

        if (pDomainAssembly->IsAvailableToProfilers() &&
            (m_assemblyIterationFlags & kIncludeAvailableToProfilers))
        {
            // The assembly has reached the state at which we would notify profilers,
            // and we're supposed to include such assemblies in the enumeration. So
            // don't reject it (i.e., noop here, and don't bother with the rest of
            // the load status checks). Check for this first, since
            // kIncludeAvailableToProfilers contains some loaded AND loading
            // assemblies.
        }
        else if (pDomainAssembly->IsLoaded())
        {
            // A loaded assembly
            if (!(m_assemblyIterationFlags & kIncludeLoaded))
            {
                continue; // reject
            }
        }
        else
        {
            // A loading assembly
            if (!(m_assemblyIterationFlags & kIncludeLoading))
            {
                continue; // reject
            }
        }

        // Next, reject DomainAssemblies whose execution status is
        // not to be included in the enumeration

        // execution assembly
        if (!(m_assemblyIterationFlags & kIncludeExecution))
        {
            continue; // reject
        }

        // Next, reject collectible assemblies
        if (pDomainAssembly->IsCollectible())
        {
            if (m_assemblyIterationFlags & kExcludeCollectible)
            {
                _ASSERTE(!(m_assemblyIterationFlags & kIncludeCollected));
                continue; // reject
            }

            // Un-tenured collectible assemblies should not be returned. (This can only happen in a brief
            // window during collectible assembly creation. No thread should need to have a pointer
            // to the just allocated DomainAssembly at this stage.)
            if (!pDomainAssembly->GetAssembly()->GetManifestModule()->IsTenured())
            {
                continue; // reject
            }

            if (pDomainAssembly->GetLoaderAllocator()->AddReferenceIfAlive())
            {   // The assembly is alive

                // Set the holder value (incl. increasing ref-count)
                *pDomainAssemblyHolder = pDomainAssembly;

                // Now release the reference we took in the if-condition
                pDomainAssembly->GetLoaderAllocator()->Release();
                return TRUE;
            }
            // The assembly is not alive anymore (and we didn't increase its ref-count in the
            // if-condition)

            if (!(m_assemblyIterationFlags & kIncludeCollected))
            {
                continue; // reject
            }
            // Set the holder value to assembly with 0 ref-count without increasing the ref-count (won't
            // call Release either)
            pDomainAssemblyHolder->Assign(pDomainAssembly, FALSE);
            return TRUE;
        }

        *pDomainAssemblyHolder = pDomainAssembly;
        return TRUE;
    }

    *pDomainAssemblyHolder = NULL;
    return FALSE;
} // AppDomain::AssemblyIterator::Next_Unlocked

#if !defined(DACCESS_COMPILE)

// Returns S_OK if the assembly was successfully loaded
HRESULT RuntimeInvokeHostAssemblyResolver(INT_PTR pManagedAssemblyLoadContextToBindWithin, BINDER_SPACE::AssemblyName *pAssemblyName, DefaultAssemblyBinder *pTPABinder, BINDER_SPACE::Assembly **ppLoadedAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pAssemblyName != NULL);
        PRECONDITION(ppLoadedAssembly != NULL);
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

    // Switch to COOP mode since we are going to work with managed references
    GCX_COOP();

    struct
    {
        ASSEMBLYNAMEREF oRefAssemblyName;
        ASSEMBLYREF oRefLoadedAssembly;
    } _gcRefs;

    ZeroMemory(&_gcRefs, sizeof(_gcRefs));

    GCPROTECT_BEGIN(_gcRefs);

    BINDER_SPACE::Assembly *pResolvedAssembly = NULL;

    bool fResolvedAssembly = false;
    BinderTracing::ResolutionAttemptedOperation tracer{pAssemblyName, 0 /*binderID*/, pManagedAssemblyLoadContextToBindWithin, hr};

    // Allocate an AssemblyName managed object
    _gcRefs.oRefAssemblyName = (ASSEMBLYNAMEREF) AllocateObject(CoreLibBinder::GetClass(CLASS__ASSEMBLY_NAME));

    // Initialize the AssemblyName object
    AssemblySpec::InitializeAssemblyNameRef(pAssemblyName, &_gcRefs.oRefAssemblyName);

    bool isSatelliteAssemblyRequest = !pAssemblyName->IsNeutralCulture();

    EX_TRY
    {
        if (pTPABinder != NULL)
        {
            // Step 2 (of CustomAssemblyBinder::BindAssemblyByName) - Invoke Load method
            // This is not invoked for TPA Binder since it always returns NULL.
            tracer.GoToStage(BinderTracing::ResolutionAttemptedOperation::Stage::AssemblyLoadContextLoad);

            // Finally, setup arguments for invocation
            MethodDescCallSite methLoadAssembly(METHOD__ASSEMBLYLOADCONTEXT__RESOLVE);

            // Setup the arguments for the call
            ARG_SLOT args[2] =
            {
                PtrToArgSlot(pManagedAssemblyLoadContextToBindWithin), // IntPtr for managed assembly load context instance
                ObjToArgSlot(_gcRefs.oRefAssemblyName), // AssemblyName instance
            };

            // Make the call
            _gcRefs.oRefLoadedAssembly = (ASSEMBLYREF) methLoadAssembly.Call_RetOBJECTREF(args);
            if (_gcRefs.oRefLoadedAssembly != NULL)
            {
                fResolvedAssembly = true;
            }

            hr = fResolvedAssembly ? S_OK : COR_E_FILENOTFOUND;

            // Step 3 (of CustomAssemblyBinder::BindAssemblyByName)
            if (!fResolvedAssembly && !isSatelliteAssemblyRequest)
            {
                tracer.GoToStage(BinderTracing::ResolutionAttemptedOperation::Stage::DefaultAssemblyLoadContextFallback);

                // If we could not resolve the assembly using Load method, then attempt fallback with TPA Binder.
                // Since TPA binder cannot fallback to itself, this fallback does not happen for binds within TPA binder.
                //
                // Switch to pre-emp mode before calling into the binder
                GCX_PREEMP();
                BINDER_SPACE::Assembly *pCoreCLRFoundAssembly = NULL;
                hr = pTPABinder->BindUsingAssemblyName(pAssemblyName, &pCoreCLRFoundAssembly);
                if (SUCCEEDED(hr))
                {
                    _ASSERTE(pCoreCLRFoundAssembly != NULL);
                    pResolvedAssembly = pCoreCLRFoundAssembly;
                    fResolvedAssembly = true;
                }
            }
        }

        if (!fResolvedAssembly && isSatelliteAssemblyRequest)
        {
            // Step 4 (of CustomAssemblyBinder::BindAssemblyByName)
            //
            // Attempt to resolve it using the ResolveSatelliteAssembly method.
            // Finally, setup arguments for invocation
            tracer.GoToStage(BinderTracing::ResolutionAttemptedOperation::Stage::ResolveSatelliteAssembly);

            MethodDescCallSite methResolveSatelitteAssembly(METHOD__ASSEMBLYLOADCONTEXT__RESOLVESATELLITEASSEMBLY);

            // Setup the arguments for the call
            ARG_SLOT args[2] =
            {
                PtrToArgSlot(pManagedAssemblyLoadContextToBindWithin), // IntPtr for managed assembly load context instance
                ObjToArgSlot(_gcRefs.oRefAssemblyName), // AssemblyName instance
            };

            // Make the call
            _gcRefs.oRefLoadedAssembly = (ASSEMBLYREF) methResolveSatelitteAssembly.Call_RetOBJECTREF(args);
            if (_gcRefs.oRefLoadedAssembly != NULL)
            {
                // Set the flag indicating we found the assembly
                fResolvedAssembly = true;
            }

            hr = fResolvedAssembly ? S_OK : COR_E_FILENOTFOUND;
        }

        if (!fResolvedAssembly)
        {
            // Step 5 (of CustomAssemblyBinder::BindAssemblyByName)
            //
            // If we couldn't resolve the assembly using TPA LoadContext as well, then
            // attempt to resolve it using the Resolving event.
            // Finally, setup arguments for invocation
            tracer.GoToStage(BinderTracing::ResolutionAttemptedOperation::Stage::AssemblyLoadContextResolvingEvent);

            MethodDescCallSite methResolveUsingEvent(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUSINGEVENT);

            // Setup the arguments for the call
            ARG_SLOT args[2] =
            {
                PtrToArgSlot(pManagedAssemblyLoadContextToBindWithin), // IntPtr for managed assembly load context instance
                ObjToArgSlot(_gcRefs.oRefAssemblyName), // AssemblyName instance
            };

            // Make the call
            _gcRefs.oRefLoadedAssembly = (ASSEMBLYREF) methResolveUsingEvent.Call_RetOBJECTREF(args);
            if (_gcRefs.oRefLoadedAssembly != NULL)
            {
                // Set the flag indicating we found the assembly
                fResolvedAssembly = true;
            }

            hr = fResolvedAssembly ? S_OK : COR_E_FILENOTFOUND;
        }

        if (fResolvedAssembly && pResolvedAssembly == NULL)
        {
            // If we are here, assembly was successfully resolved via Load or Resolving events.
            _ASSERTE(_gcRefs.oRefLoadedAssembly != NULL);

            // We were able to get the assembly loaded. Now, get its name since the host could have
            // performed the resolution using an assembly with different name.
            DomainAssembly *pDomainAssembly = _gcRefs.oRefLoadedAssembly->GetDomainAssembly();
            PEAssembly *pLoadedPEAssembly = NULL;
            bool fFailLoad = false;
            if (!pDomainAssembly)
            {
                // Reflection emitted assemblies will not have a domain assembly.
                fFailLoad = true;
            }
            else
            {
                pLoadedPEAssembly = pDomainAssembly->GetFile();
                if (!pLoadedPEAssembly->HasHostAssembly())
                {
                    // Reflection emitted assemblies will not have a domain assembly.
                    fFailLoad = true;
                }
            }

            // The loaded assembly's BINDER_SPACE::Assembly* is saved as HostAssembly in PEAssembly
            if (fFailLoad)
            {
                PathString name;
                pAssemblyName->GetDisplayName(name, BINDER_SPACE::AssemblyName::INCLUDE_ALL);
                COMPlusThrowHR(COR_E_INVALIDOPERATION, IDS_HOST_ASSEMBLY_RESOLVER_DYNAMICALLY_EMITTED_ASSEMBLIES_UNSUPPORTED, name);
            }

            pResolvedAssembly = pLoadedPEAssembly->GetHostAssembly();
        }

        if (fResolvedAssembly)
        {
            _ASSERTE(pResolvedAssembly != NULL);

            // Get the BINDER_SPACE::Assembly reference to return back to.
            *ppLoadedAssembly = clr::SafeAddRef(pResolvedAssembly);
            hr = S_OK;

            tracer.SetFoundAssembly(static_cast<BINDER_SPACE::Assembly *>(pResolvedAssembly));
        }
        else
        {
            hr = COR_E_FILENOTFOUND;
        }
    }
    EX_HOOK
    {
        Exception* ex = GET_EXCEPTION();
        tracer.SetException(ex);
    }
    EX_END_HOOK

    GCPROTECT_END();

    return hr;
}
#endif // !defined(DACCESS_COMPILE)

//approximate size of loader data
//maintained for each assembly
#define APPROX_LOADER_DATA_PER_ASSEMBLY 8196

size_t AppDomain::EstimateSize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    size_t retval = sizeof(AppDomain);
    retval += GetLoaderAllocator()->EstimateSize();
    //very rough estimate
    retval += GetAssemblyCount() * APPROX_LOADER_DATA_PER_ASSEMBLY;
    return retval;
}

#ifdef DACCESS_COMPILE

void
DomainLocalModule::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Enumerate the DomainLocalModule itself. DLMs are allocated to be larger than
    // sizeof(DomainLocalModule) to make room for ClassInit flags and non-GC statics.
    // "DAC_ENUM_DTHIS()" probably does not account for this, so we might not enumerate
    // all of the ClassInit flags and non-GC statics.
    // sizeof(DomainLocalModule) == 0x28
    DAC_ENUM_DTHIS();

    if (m_pDomainFile.IsValid())
    {
        m_pDomainFile->EnumMemoryRegions(flags);
    }

    if (m_pDynamicClassTable.Load().IsValid())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_pDynamicClassTable.Load()),
                            m_aDynamicEntries * sizeof(DynamicClassInfo));

        for (SIZE_T i = 0; i < m_aDynamicEntries; i++)
        {
            PTR_DynamicEntry entry = dac_cast<PTR_DynamicEntry>(m_pDynamicClassTable[i].m_pDynamicEntry.Load());
            if (entry.IsValid())
            {
                // sizeof(DomainLocalModule::DynamicEntry) == 8
                entry.EnumMem();
            }
        }
    }
}

void
BaseDomain::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                              bool enumThis)
{
    SUPPORTS_DAC;
    if (enumThis)
    {
        // This is wrong.  Don't do it.
        // BaseDomain cannot be instantiated.
        // The only thing this code can hope to accomplish is to potentially break
        // memory enumeration walking through the derived class if we
        // explicitly call the base class enum first.
//        DAC_ENUM_VTHIS();
    }

    EMEM_OUT(("MEM: %p BaseDomain\n", dac_cast<TADDR>(this)));
}

void
AppDomain::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                             bool enumThis)
{
    SUPPORTS_DAC;

    if (enumThis)
    {
        //sizeof(AppDomain) == 0xeb0
        DAC_ENUM_VTHIS();
    }
    BaseDomain::EnumMemoryRegions(flags, false);

    // We don't need AppDomain name in triage dumps.
    if (flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        m_friendlyName.EnumMemoryRegions(flags);
    }

    m_Assemblies.EnumMemoryRegions(flags);
    AssemblyIterator assem = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (assem.Next(pDomainAssembly.This()))
    {
        pDomainAssembly->EnumMemoryRegions(flags);
    }
}

void
SystemDomain::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                bool enumThis)
{
    SUPPORTS_DAC;
    if (enumThis)
    {
        DAC_ENUM_VTHIS();
    }
    BaseDomain::EnumMemoryRegions(flags, false);

    if (m_pSystemFile.IsValid())
    {
        m_pSystemFile->EnumMemoryRegions(flags);
    }
    if (m_pSystemAssembly.IsValid())
    {
        m_pSystemAssembly->EnumMemoryRegions(flags);
    }
    if (AppDomain::GetCurrentDomain())
    {
        AppDomain::GetCurrentDomain()->EnumMemoryRegions(flags, true);
    }
}

#endif //DACCESS_COMPILE


PTR_LoaderAllocator SystemDomain::GetGlobalLoaderAllocator()
{
    return PTR_LoaderAllocator(PTR_HOST_MEMBER_TADDR(SystemDomain,System(),m_GlobalAllocator));
}

#if defined(FEATURE_TYPEEQUIVALENCE)

#ifndef DACCESS_COMPILE
TypeEquivalenceHashTable * AppDomain::GetTypeEquivalenceCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the critical section all of the time in debug builds to ensure that it is safe to take
    // the critical section in the unusual times when it may actually be needed in retail builds
#ifdef _DEBUG
    CrstHolder ch(&m_TypeEquivalenceCrst);
#endif

    if (m_pTypeEquivalenceTable.Load() == NULL)
    {
#ifndef _DEBUG
        CrstHolder ch(&m_TypeEquivalenceCrst);
#endif
        if (m_pTypeEquivalenceTable.Load() == NULL)
        {
            m_pTypeEquivalenceTable = TypeEquivalenceHashTable::Create(this, /* bucket count */ 12, &m_TypeEquivalenceCrst);
        }
    }
    return m_pTypeEquivalenceTable;
}
#endif //!DACCESS_COMPILE

#endif //FEATURE_TYPEEQUIVALENCE

#if !defined(DACCESS_COMPILE)

//---------------------------------------------------------------------------------------------------------------------
void AppDomain::PublishHostedAssembly(
    DomainAssembly * pDomainAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    if (pDomainAssembly->GetFile()->HasHostAssembly())
    {
        // We have to serialize all Add operations
        CrstHolder lockAdd(&m_crstHostAssemblyMapAdd);
        _ASSERTE(m_hostAssemblyMap.Lookup(pDomainAssembly->GetFile()->GetHostAssembly()) == nullptr);

        // Wrapper for m_hostAssemblyMap.Add that avoids call out into host
        HostAssemblyMap::AddPhases addCall;

        // 1. Preallocate one element
        addCall.PreallocateForAdd(&m_hostAssemblyMap);
        {
            // 2. Take the reader lock which can be taken during stack walking
            // We cannot call out into host from ForbidSuspend region (i.e. no allocations/deallocations)
            ForbidSuspendThreadHolder suspend;
            {
                CrstHolder lock(&m_crstHostAssemblyMap);
                // 3. Add the element to the hash table (no call out into host)
                addCall.Add(pDomainAssembly);
            }
        }
        // 4. Cleanup the old memory (if any)
        addCall.DeleteOldTable();
    }
    else
    {
    }
}

//---------------------------------------------------------------------------------------------------------------------
void AppDomain::UpdatePublishHostedAssembly(
    DomainAssembly * pAssembly,
    PTR_PEFile       pFile)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END

    if (pAssembly->GetFile()->HasHostAssembly())
    {
        // We have to serialize all Add operations
        CrstHolder lockAdd(&m_crstHostAssemblyMapAdd);
        {
            // Wrapper for m_hostAssemblyMap.Add that avoids call out into host
            OriginalFileHostAssemblyMap::AddPhases addCall;
            bool fAddOrigFile = false;

            // For cases where the pefile is being updated
            // 1. Preallocate one element
            if (pFile != pAssembly->GetFile())
            {
                addCall.PreallocateForAdd(&m_hostAssemblyMapForOrigFile);
                fAddOrigFile = true;
            }

            {
                // We cannot call out into host from ForbidSuspend region (i.e. no allocations/deallocations)
                ForbidSuspendThreadHolder suspend;
                {
                    CrstHolder lock(&m_crstHostAssemblyMap);

                    // Remove from hash table.
                    _ASSERTE(m_hostAssemblyMap.Lookup(pAssembly->GetFile()->GetHostAssembly()) != nullptr);
                    m_hostAssemblyMap.Remove(pAssembly->GetFile()->GetHostAssembly());

                    // Update PEFile on DomainAssembly. (This may cause the key for the hash to change, which is why we need this function)
                    pAssembly->UpdatePEFileWorker(pFile);

                    _ASSERTE(fAddOrigFile == (pAssembly->GetOriginalFile() != pAssembly->GetFile()));
                    if (fAddOrigFile)
                    {
                        // Add to the orig file hash table if we might be in a case where we've cached the original pefile and not the final pe file (for use during GetAssemblyIfLoaded)
                        addCall.Add(pAssembly);
                    }

                    // Add back to the hashtable (the call to Remove above guarantees that we will not call into host for table reallocation)
                    _ASSERTE(m_hostAssemblyMap.Lookup(pAssembly->GetFile()->GetHostAssembly()) == nullptr);
                    m_hostAssemblyMap.Add(pAssembly);
                }
            }

            // 4. Cleanup the old memory (if any)
            if (fAddOrigFile)
                addCall.DeleteOldTable();
        }
    }
    else
    {

        pAssembly->UpdatePEFileWorker(pFile);
    }
}

//---------------------------------------------------------------------------------------------------------------------
void AppDomain::UnPublishHostedAssembly(
    DomainAssembly * pAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END

    if (pAssembly->GetFile()->HasHostAssembly())
    {
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_crstHostAssemblyMap);
            _ASSERTE(m_hostAssemblyMap.Lookup(pAssembly->GetFile()->GetHostAssembly()) != nullptr);
            m_hostAssemblyMap.Remove(pAssembly->GetFile()->GetHostAssembly());

            // We also have an entry in m_hostAssemblyMapForOrigFile. Handle that case.
            if (pAssembly->GetOriginalFile() != pAssembly->GetFile())
            {
                m_hostAssemblyMapForOrigFile.Remove(pAssembly->GetOriginalFile()->GetHostAssembly());
            }
        }
    }
}

#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------------------------------------
PTR_DomainAssembly AppDomain::FindAssembly(PTR_BINDER_SPACE_Assembly pHostAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (pHostAssembly == nullptr)
        return NULL;

    {
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_crstHostAssemblyMap);
            PTR_DomainAssembly returnValue = m_hostAssemblyMap.Lookup(pHostAssembly);
            if (returnValue == NULL)
            {
                // If not found in the m_hostAssemblyMap, look in the m_hostAssemblyMapForOrigFile
                // This is necessary as it may happen during in a second AppDomain that the PEFile
                // first discovered in the AppDomain may not be used by the DomainFile, but the CLRPrivBinderFusion
                // will in some cases find the pHostAssembly associated with this no longer used PEFile
                // instead of the PEFile that was finally decided upon.
                returnValue = m_hostAssemblyMapForOrigFile.Lookup(pHostAssembly);
            }

            return returnValue;
        }
    }
}

#ifndef DACCESS_COMPILE
// Return native image for a given composite image file name, NULL when not found.
PTR_NativeImage AppDomain::GetNativeImage(LPCUTF8 simpleFileName)
{
    CrstHolder ch(&m_nativeImageLoadCrst);
    PTR_NativeImage pExistingImage;
    if (m_nativeImageMap.Lookup(simpleFileName, &pExistingImage))
    {
        return pExistingImage;
    }
    return nullptr;
}

PTR_NativeImage AppDomain::SetNativeImage(LPCUTF8 simpleFileName, PTR_NativeImage pNativeImage)
{
    CrstHolder ch(&m_nativeImageLoadCrst);
    PTR_NativeImage pExistingImage;
    if (m_nativeImageMap.Lookup(simpleFileName, &pExistingImage))
    {
        return pExistingImage;
    }
    m_nativeImageMap.Add(simpleFileName, pNativeImage);
    return nullptr;
}
#endif//DACCESS_COMPILE
