// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
#include "apithreadstress.h"
#include "eventtrace.h"
#include "comdelegate.h"
#include "siginfo.hpp"
#include "typekey.h"

#include "caparser.h"
#include "ecall.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#ifdef FEATURE_PREJIT
#include "corcompile.h"
#include "compile.h"
#endif // FEATURE_PREJIT

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#include "runtimecallablewrapper.h"
#include "mngstdinterfaces.h"
#include "olevariant.h"
#include "rcwrefcache.h"
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP

#include "typeequivalencehash.hpp"

#include "appdomain.inl"
#include "typeparse.h"
#include "threadpoolrequest.h"

#include "nativeoverlapped.h"

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "stringarraylist.h"

#include "../binder/inc/clrprivbindercoreclr.h"


#include "clrprivtypecachewinrt.h"

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
#ifdef FEATURE_PREJIT
SVAL_IMPL(BOOL, SystemDomain, s_fForceDebug);
SVAL_IMPL(BOOL, SystemDomain, s_fForceProfiling);
SVAL_IMPL(BOOL, SystemDomain, s_fForceInstrument);
#endif

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

ULONG               SystemDomain::s_dNumAppDomains = 0;

DWORD               SystemDomain::m_dwLowestFreeIndex        = 0;



// comparison function to be used for matching clsids in our clsid hash table
BOOL CompareCLSID(UPTR u1, UPTR u2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GUID *pguid = (GUID *)(u1 << 1);
    _ASSERTE(pguid != NULL);

    MethodTable *pMT= (MethodTable *)u2;
    _ASSERTE(pMT!= NULL);

    GUID guid;
    pMT->GetGuid(&guid, TRUE);
    if (!IsEqualIID(guid, *pguid))
        return FALSE;

    return TRUE;
}

#ifndef CROSSGEN_COMPILE
// Constructor for the LargeHeapHandleBucket class.
LargeHeapHandleBucket::LargeHeapHandleBucket(LargeHeapHandleBucket *pNext, DWORD Size, BaseDomain *pDomain)
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
    HandleArrayObj = (PTRARRAYREF)AllocateObjectArray(Size, g_pObjectClass, TRUE);

    // Retrieve the pointer to the data inside the array. This is legal since the array
    // is located in the large object heap and is guaranteed not to move.
    m_pArrayDataPtr = (OBJECTREF *)HandleArrayObj->GetDataPtr();

    // Store the array in a strong handle to keep it alive.
    m_hndHandleArray = pDomain->CreatePinningHandle((OBJECTREF)HandleArrayObj);
}


// Destructor for the LargeHeapHandleBucket class.
LargeHeapHandleBucket::~LargeHeapHandleBucket()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_hndHandleArray)
    {
        DestroyPinningHandle(m_hndHandleArray);
        m_hndHandleArray = NULL;
    }
}


// Allocate handles from the bucket.
OBJECTREF *LargeHeapHandleBucket::AllocateHandles(DWORD nRequested)
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
OBJECTREF *LargeHeapHandleBucket::TryAllocateEmbeddedFreeHandle()
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


// Maximum bucket size will be 64K on 32-bit and 128K on 64-bit. 
// We subtract out a small amount to leave room for the object
// header and length of the array.

#define MAX_BUCKETSIZE (16384 - 4)

// Constructor for the LargeHeapHandleTable class.
LargeHeapHandleTable::LargeHeapHandleTable(BaseDomain *pDomain, DWORD InitialBucketSize)
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


// Destructor for the LargeHeapHandleTable class.
LargeHeapHandleTable::~LargeHeapHandleTable()
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
        LargeHeapHandleBucket *pOld = m_pHead;
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
// There are two locations you can find a LargeHeapHandleTable
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
//        CrstHolder ch(&m_LargeHeapHandleTableCrst);
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
//     LargeHeapHandleBlockHolder constructor
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
OBJECTREF* LargeHeapHandleTable::AllocateHandles(DWORD nRequested)
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

        m_pHead = new LargeHeapHandleBucket(m_pHead, NewBucketSize, m_pDomain);

        m_NextBucketSize = min(m_NextBucketSize * 2, MAX_BUCKETSIZE);
    }

    return m_pHead->AllocateHandles(nRequested);
}

//*****************************************************************************
// Release object handles allocated using AllocateHandles().
void LargeHeapHandleTable::ReleaseHandles(OBJECTREF *pObjRef, DWORD nReleased)
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
    HandleArrayObj = (PTRARRAYREF)AllocateObjectArray(Size, g_pObjectClass, FALSE);

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

#endif // CROSSGEN_COMPILE


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

    m_fDisableInterfaceCache = FALSE;

    m_pFusionContext = NULL;
    m_pTPABinderContext = NULL;

    // Make sure the container is set to NULL so that it gets loaded when it is used.
    m_pLargeHeapHandleTable = NULL;

#ifndef CROSSGEN_COMPILE
    // Note that m_handleStore is overridden by app domains
    m_handleStore = GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore();
#else
    m_handleStore = NULL;
#endif

#ifdef FEATURE_COMINTEROP
    m_pMngStdInterfacesInfo = NULL;
    m_pWinRtBinder = NULL;
#endif
    m_FileLoadLock.PreInit();
    m_JITLock.PreInit();
    m_ClassInitLock.PreInit();
    m_ILStubGenLock.PreInit();

#ifdef FEATURE_CODE_VERSIONING
    m_codeVersionManager.PreInit();
#endif

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

    m_InteropDataCrst.Init(CrstInteropData, CRST_REENTRANCY);

    m_WinRTFactoryCacheCrst.Init(CrstWinRTFactoryCache, CRST_UNSAFE_COOPGC);

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

    // Large heap handle table CRST.
    m_LargeHeapHandleTableCrst.Init(CrstAppDomainHandleTable);

    m_crstLoaderAllocatorReferences.Init(CrstLoaderAllocatorReferences);
    // Has to switch thread to GC_NOTRIGGER while being held (see code:BaseDomain#AssemblyListLock)
    m_crstAssemblyList.Init(CrstAssemblyList, CrstFlags(
        CRST_GC_NOTRIGGER_WHEN_TAKEN | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));

#ifdef FEATURE_COMINTEROP
    // Allocate the managed standard interfaces information.
    m_pMngStdInterfacesInfo = new MngStdInterfacesInfo();
    
    {
        CLRPrivBinderWinRT::NamespaceResolutionKind fNamespaceResolutionKind = CLRPrivBinderWinRT::NamespaceResolutionKind_WindowsAPI;
        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DesignerNamespaceResolutionEnabled) != FALSE)
        {
            fNamespaceResolutionKind = CLRPrivBinderWinRT::NamespaceResolutionKind_DesignerResolveEvent;
        }
        CLRPrivTypeCacheWinRT * pWinRtTypeCache = CLRPrivTypeCacheWinRT::GetOrCreateTypeCache();
        m_pWinRtBinder = CLRPrivBinderWinRT::GetOrCreateBinder(pWinRtTypeCache, fNamespaceResolutionKind);
    }
#endif // FEATURE_COMINTEROP

    // Init the COM Interop data hash
    {
        LockOwner lock = {&m_InteropDataCrst, IsOwnerOfCrst};
        m_interopDataHash.Init(0, NULL, false, &lock);
    }

    m_dwSizedRefHandles = 0;
    if (!m_iNumberOfProcessors)
    {
        m_iNumberOfProcessors = GetCurrentProcessCpuCount();
    }
}

#undef LOADERHEAP_PROFILE_COUNTER

void BaseDomain::InitVSD()
{
    STANDARD_VM_CONTRACT;

    // This is a workaround for gcc, since it fails to successfully resolve
    // "TypeIDMap::STARTING_SHARED_DOMAIN_ID" when used within the ?: operator.
    UINT32 startingId;
    if (IsSharedDomain())
    {
        startingId = TypeIDMap::STARTING_SHARED_DOMAIN_ID;
    }
    else
    {
        startingId = TypeIDMap::STARTING_UNSHARED_DOMAIN_ID;
    }

    // By passing false as the last parameter, interfaces loaded in the
    // shared domain will not be given fat type ids if RequiresFatDispatchTokens
    // is set. This is correct, as the fat dispatch tokens are only needed to solve
    // uniqueness problems involving domain specific types.
    m_typeIDMap.Init(startingId, 2, !IsSharedDomain());

#ifndef CROSSGEN_COMPILE
    GetLoaderAllocator()->InitVirtualCallStubManager(this);
#endif
}

#ifndef CROSSGEN_COMPILE

void BaseDomain::ClearFusionContext()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if(m_pFusionContext) {
        m_pFusionContext->Release();
        m_pFusionContext = NULL;
    }
    if (m_pTPABinderContext) {
        m_pTPABinderContext->Release();
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

void AppDomain::ReleaseFiles()
{
    STANDARD_VM_CONTRACT;

    // Shutdown assemblies
    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded  | kIncludeExecution | kIncludeFailedToLoad | kIncludeLoading));
    CollectibleAssemblyHolder<DomainAssembly *> pAsm;

    while (i.Next(pAsm.This()))
    {
        if (pAsm->GetCurrentAssembly() == NULL)
        {
            // Might be domain neutral or not, but should have no live objects as it has not been
            // really loaded yet. Just reset it.
            _ASSERTE(FitsIn<DWORD>(i.GetIndex()));
            m_Assemblies.Set(this, static_cast<DWORD>(i.GetIndex()), NULL);
            delete pAsm.Extract();
        }
        else
        {
            pAsm->ReleaseFiles();
        }
    }
} // AppDomain::ReleaseFiles


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
        CrstHolder ch(&m_LargeHeapHandleTableCrst);
        GCX_COOP();

        if (ppLazyAllocate && *ppLazyAllocate)
        {
            // Allocation already happened
            return *ppLazyAllocate;
        }

        // Make sure the large heap handle table is initialized.
        if (!m_pLargeHeapHandleTable)
            InitLargeHeapHandleTable();

        // Allocate the handles.
        OBJECTREF* result = m_pLargeHeapHandleTable->AllocateHandles(nRequested);

        if (ppLazyAllocate)
        {
            *ppLazyAllocate = result;
        }

        return result;
    }
}
#endif // CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE

// Insert class in the hash table
void AppDomain::InsertClassForCLSID(MethodTable* pMT, BOOL fForceInsert /*=FALSE*/)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CVID cvid;

    // Ensure that registered classes are activated for allocation
    pMT->EnsureInstanceActive();

    // Note that it is possible for multiple classes to claim the same CLSID, and in such a
    // case it is arbitrary which one we will return for a future query for a given app domain.

    pMT->GetGuid(&cvid, fForceInsert);

    if (!IsEqualIID(cvid, GUID_NULL))
    {
        //<TODO>@todo get a better key</TODO>
        LPVOID val = (LPVOID)pMT;
        {
            LockHolder lh(this);

            if (LookupClass(cvid) != pMT)
            {
                m_clsidHash.InsertValue(GetKeyFromGUID(&cvid), val);
            }
        }
    }
}

void AppDomain::InsertClassForCLSID(MethodTable* pMT, GUID *pGuid)
{
    CONTRACT_VOID
    {
        NOTHROW;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACT_END;

    LPVOID val = (LPVOID)pMT;
    {
        LockHolder lh(this);

        CVID* cvid = pGuid;
        if (LookupClass(*cvid) != pMT)
        {
            m_clsidHash.InsertValue(GetKeyFromGUID(pGuid), val);
        }
    }

    RETURN;
}
#endif // DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
void AppDomain::CacheTypeByName(const SString &ssClassName, const UINT vCacheVersion, TypeHandle typeHandle, BYTE bFlags, BOOL bReplaceExisting /*= FALSE*/)
{
    WRAPPER_NO_CONTRACT;
    LockHolder lh(this);
    CacheTypeByNameWorker(ssClassName, vCacheVersion, typeHandle, bFlags, bReplaceExisting);
}

void AppDomain::CacheTypeByNameWorker(const SString &ssClassName, const UINT vCacheVersion, TypeHandle typeHandle, BYTE bFlags, BOOL bReplaceExisting /*= FALSE*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!typeHandle.IsNull());
    }
    CONTRACTL_END;

    NewArrayHolder<WCHAR> wzClassName(DuplicateStringThrowing(ssClassName.GetUnicode()));

    if (m_vNameToTypeMapVersion != vCacheVersion)
        return;

    if (m_pNameToTypeMap == nullptr)
    {
        m_pNameToTypeMap = new NameToTypeMapTable();
    }

    NameToTypeMapEntry e;
    e.m_key.m_wzName = wzClassName;
    e.m_key.m_cchName = ssClassName.GetCount();
    e.m_typeHandle = typeHandle;
    e.m_nEpoch = this->m_nEpoch;
    e.m_bFlags = bFlags;
    if (!bReplaceExisting)
        m_pNameToTypeMap->Add(e);
    else
        m_pNameToTypeMap->AddOrReplace(e);

    wzClassName.SuppressRelease();
}
#endif // DACCESS_COMPILE

TypeHandle AppDomain::LookupTypeByName(const SString &ssClassName, UINT* pvCacheVersion, BYTE *pbFlags)
{
    WRAPPER_NO_CONTRACT;
    LockHolder lh(this);
    return LookupTypeByNameWorker(ssClassName, pvCacheVersion, pbFlags);
}

TypeHandle AppDomain::LookupTypeByNameWorker(const SString &ssClassName, UINT* pvCacheVersion, BYTE *pbFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pbFlags, NULL_OK));
    }
    CONTRACTL_END;

    *pvCacheVersion = m_vNameToTypeMapVersion;

    if (m_pNameToTypeMap == nullptr)
        return TypeHandle();  // a null TypeHandle

    NameToTypeMapEntry::Key key;
    key.m_cchName = ssClassName.GetCount();
    key.m_wzName  = ssClassName.GetUnicode();

    const NameToTypeMapEntry * pEntry = m_pNameToTypeMap->LookupPtr(key);
    if (pEntry == NULL)
        return TypeHandle();  // a null TypeHandle

    if (pbFlags != NULL)
        *pbFlags = pEntry->m_bFlags;

    return pEntry->m_typeHandle;
}

PTR_MethodTable AppDomain::LookupTypeByGuid(const GUID & guid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    SString sGuid;
    {
        WCHAR wszGuid[64];
        GuidToLPWSTR(guid, wszGuid, _countof(wszGuid));
        sGuid.Append(wszGuid);
    }
    UINT ver;
    TypeHandle th = LookupTypeByName(sGuid, &ver, NULL);

    if (!th.IsNull())
    {
        _ASSERTE(!th.IsTypeDesc());
        return th.AsMethodTable();
    }

#ifdef FEATURE_PREJIT
    else
    {
        // Next look in each ngen'ed image in turn
        AssemblyIterator assemblyIterator = IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (assemblyIterator.Next(pDomainAssembly.This()))
        {
            CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetLoadedAssembly();

            DomainAssembly::ModuleIterator i = pDomainAssembly->IterateModules(kModIterIncludeLoaded);
            while (i.Next())
            {
                Module * pModule = i.GetLoadedModule();
                if (!pModule->HasNativeImage())
                    continue;
                _ASSERTE(!pModule->IsCollectible());
                PTR_MethodTable pMT = pModule->LookupTypeByGuid(guid);
                if (pMT != NULL)
                {
                    return pMT;
                }
            }
        }
    }
#endif // FEATURE_PREJIT
    return NULL;
}

#ifndef DACCESS_COMPILE
void AppDomain::CacheWinRTTypeByGuid(TypeHandle typeHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!typeHandle.IsTypeDesc());
        PRECONDITION(CanCacheWinRTTypeByGuid(typeHandle));
    }
    CONTRACTL_END;

    PTR_MethodTable pMT = typeHandle.AsMethodTable();

    GUID guid;
    if (pMT->GetGuidForWinRT(&guid))
    {
        SString sGuid;

        {
            WCHAR wszGuid[64];
            GuidToLPWSTR(guid, wszGuid, _countof(wszGuid));
            sGuid.Append(wszGuid);
        }

        BYTE bFlags = 0x80;
        TypeHandle th;
        UINT vCacheVersion;
        {
            LockHolder lh(this);
            th = LookupTypeByNameWorker(sGuid, &vCacheVersion, &bFlags);

            if (th.IsNull())
            {
                // no other entry with the same GUID exists in the cache
                CacheTypeByNameWorker(sGuid, vCacheVersion, typeHandle, bFlags);
            }
            else if (typeHandle.AsMethodTable() != th.AsMethodTable() && th.IsProjectedFromWinRT())
            {
                // If we found a native WinRT type cached with the same GUID, replace it.
                // Otherwise simply add the new mapping to the cache.
                CacheTypeByNameWorker(sGuid, vCacheVersion, typeHandle, bFlags, TRUE);
            }
        }
    }
}
#endif // DACCESS_COMPILE

void AppDomain::GetCachedWinRTTypes(
                        SArray<PTR_MethodTable> * pTypes, 
                        SArray<GUID> * pGuids, 
                        UINT minEpoch, 
                        UINT * pCurEpoch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LockHolder lh(this);

    for (auto it = m_pNameToTypeMap->Begin(), end = m_pNameToTypeMap->End(); 
            it != end; 
            ++it)
    {
        NameToTypeMapEntry entry = (NameToTypeMapEntry)(*it);
        TypeHandle th = entry.m_typeHandle;
        if (th.AsMethodTable() != NULL && 
            entry.m_key.m_wzName[0] == W('{') &&
            entry.m_nEpoch >= minEpoch)
        {
            _ASSERTE(!th.IsTypeDesc());
            PTR_MethodTable pMT = th.AsMethodTable();
            // we're parsing the GUID value from the cache, because projected types do not cache the 
            // COM GUID in their GetGuid() but rather the legacy GUID
            GUID iid;
            if (LPWSTRToGuid(&iid, entry.m_key.m_wzName, 38) && iid != GUID_NULL)
            {
                pTypes->Append(pMT);
                pGuids->Append(iid);
            }
        }
    }

#ifdef FEATURE_PREJIT
    // Next look in each ngen'ed image in turn
    AssemblyIterator assemblyIterator = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (assemblyIterator.Next(pDomainAssembly.This()))
    {
        CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetLoadedAssembly();

        DomainAssembly::ModuleIterator i = pDomainAssembly->IterateModules(kModIterIncludeLoaded);
        while (i.Next())
        {
            Module * pModule = i.GetLoadedModule();
            if (!pModule->HasNativeImage())
                continue;
            _ASSERTE(!pModule->IsCollectible());

            pModule->GetCachedWinRTTypes(pTypes, pGuids);
        }
    }
#endif // FEATURE_PREJIT

    if (pCurEpoch != NULL)
        *pCurEpoch = m_nEpoch;
    ++m_nEpoch;
}

#ifndef CROSSGEN_COMPILE
#ifndef DACCESS_COMPILE
// static
void WinRTFactoryCacheTraits::OnDestructPerEntryCleanupAction(const WinRTFactoryCacheEntry& e)
{
    WRAPPER_NO_CONTRACT;
    if (e.m_pCtxEntry != NULL)
    {
        e.m_pCtxEntry->Release();
    }
    // the AD is going away, no need to destroy the OBJECTHANDLE
}

void AppDomain::CacheWinRTFactoryObject(MethodTable *pClassMT, OBJECTREF *refFactory, LPVOID lpCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pClassMT));
    }
    CONTRACTL_END;

    CtxEntryHolder pNewCtxEntry;
    if (lpCtxCookie != NULL)
    {
        // We don't want to insert the context cookie in the cache because it's just an address
        // of an internal COM data structure which will be freed when the apartment is torn down.
        // What's worse, if another apartment is later created, its context cookie may have exactly
        // the same value leading to incorrect cache hits. We'll use our CtxEntry instead which
        // is ref-counted and keeps the COM data structure alive even after the apartment ceases
        // to exist.
        pNewCtxEntry = CtxEntryCache::GetCtxEntryCache()->FindCtxEntry(lpCtxCookie, GetThread());
    }

    WinRTFactoryCacheLockHolder lh(this);

    if (m_pWinRTFactoryCache == nullptr)
    {
        m_pWinRTFactoryCache = new WinRTFactoryCache();
    }

    WinRTFactoryCacheEntry *pEntry = const_cast<WinRTFactoryCacheEntry*>(m_pWinRTFactoryCache->LookupPtr(pClassMT));
    if (!pEntry)
    {
        //
        // No existing entry for this cache
        // Create a new one
        //
        WinRTFactoryCacheEntry e;

        OBJECTHANDLEHolder ohNewHandle(CreateHandle(*refFactory));

        e.key               = pClassMT;
        e.m_pCtxEntry       = pNewCtxEntry;
        e.m_ohFactoryObject = ohNewHandle;

        m_pWinRTFactoryCache->Add(e);
     
        // suppress release of the CtxEntry and handle after we successfully inserted the new entry
        pNewCtxEntry.SuppressRelease();
        ohNewHandle.SuppressRelease();
    }
    else
    {
        //
        // Existing entry
        //
        // release the old CtxEntry and update the entry
        CtxEntry *pTemp = pNewCtxEntry.Extract();
        pNewCtxEntry = pEntry->m_pCtxEntry;
        pEntry->m_pCtxEntry = pTemp;

        IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
        mgr->StoreObjectInHandle(pEntry->m_ohFactoryObject, OBJECTREFToObject(*refFactory));
    }
}

OBJECTREF AppDomain::LookupWinRTFactoryObject(MethodTable *pClassMT, LPVOID lpCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pClassMT));
        PRECONDITION(CheckPointer(m_pWinRTFactoryCache, NULL_OK));
    }
    CONTRACTL_END;


    if (m_pWinRTFactoryCache == nullptr)
        return NULL;
            
    //
    // Retrieve cached factory
    //
    WinRTFactoryCacheLockHolder lh(this);

    const WinRTFactoryCacheEntry *pEntry = m_pWinRTFactoryCache->LookupPtr(pClassMT);
    if (pEntry == NULL)
        return NULL;
    
    //
    // Ignore factories from a different context, unless lpCtxCookie == NULL, 
    // which means the factory is free-threaded
    // Note that we cannot touch the RCW to retrieve cookie at this point
    // because the RCW might belong to a STA thread and that STA thread might die
    // and take the RCW with it. Therefore we have to save cookie in this cache    
    //
    if (pEntry->m_pCtxEntry == NULL || pEntry->m_pCtxEntry->GetCtxCookie() == lpCtxCookie)
        return ObjectFromHandle(pEntry->m_ohFactoryObject);
    
    return NULL;
}

void AppDomain::RemoveWinRTFactoryObjects(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pWinRTFactoryCache == nullptr)
        return;

    // helper class for delayed CtxEntry cleanup
    class CtxEntryListReleaseHolder
    {
    public:
        CQuickArrayList<CtxEntry *> m_list;

        ~CtxEntryListReleaseHolder()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            for (SIZE_T i = 0; i < m_list.Size(); i++)
            {
                m_list[i]->Release();
            }
        }
    } ctxEntryListReleaseHolder;

    GCX_COOP();
    {
        WinRTFactoryCacheLockHolder lh(this);

        // Go through the hash table and remove items in the given context
        for (WinRTFactoryCache::Iterator it = m_pWinRTFactoryCache->Begin(); it != m_pWinRTFactoryCache->End(); it++)
        {
            if (it->m_pCtxEntry != NULL && it->m_pCtxEntry->GetCtxCookie() == pCtxCookie)
            {
                // Releasing the CtxEntry may trigger GC which we can't do under the lock so we push
                // it on our local list and release them all after we're done iterating the hashtable.
                ctxEntryListReleaseHolder.m_list.Push(it->m_pCtxEntry);

                DestroyHandle(it->m_ohFactoryObject);
                m_pWinRTFactoryCache->Remove(it);
            }
        }
    }
}

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
        FieldDesc *pValueFD = MscorlibBinder::GetField(FIELD__MISSING__VALUE);

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
#endif //CROSSGEN_COMPILE
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE

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

void BaseDomain::InitLargeHeapHandleTable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pLargeHeapHandleTable==NULL);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    m_pLargeHeapHandleTable = new LargeHeapHandleTable(this, STATIC_OBJECT_TABLE_BUCKET_SIZE);

#ifdef _DEBUG
    m_pLargeHeapHandleTable->RegisterCrstDebug(&m_LargeHeapHandleTableCrst);
#endif
}

#endif // CROSSGEN_COMPILE

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

#ifdef FEATURE_PREJIT
void    SystemDomain::SetCompilationOverrides(BOOL fForceDebug,
                                              BOOL fForceProfiling,
                                              BOOL fForceInstrument)
{
    LIMITED_METHOD_CONTRACT;
    s_fForceDebug = fForceDebug;
    s_fForceProfiling = fForceProfiling;
    s_fForceInstrument = fForceInstrument;
}
#endif

#endif //!DACCESS_COMPILE

#ifdef FEATURE_PREJIT
void    SystemDomain::GetCompilationOverrides(BOOL * fForceDebug,
                                              BOOL * fForceProfiling,
                                              BOOL * fForceInstrument)
{
    LIMITED_METHOD_DAC_CONTRACT;
    *fForceDebug = s_fForceDebug;
    *fForceProfiling = s_fForceProfiling;
    *fForceInstrument = s_fForceInstrument;
}
#endif

#ifndef DACCESS_COMPILE

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

#ifndef CROSSGEN_COMPILE
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

    PerAppDomainTPCountList::InitAppDomainIndexList();
#endif // CROSSGEN_COMPILE

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

#ifndef CROSSGEN_COMPILE

void SystemDomain::DetachBegin()
{
    WRAPPER_NO_CONTRACT;
    // Shut down the domain and its children (but don't deallocate anything just
    // yet).

    // TODO: we should really not running managed DLLMain during process detach.
    if (GetThread() == NULL)
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
        m_pSystemDomain->ClearFusionContext();
        AppDomain* pAppDomain = GetAppDomain();
        if (pAppDomain)
            pAppDomain->ClearFusionContext();
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

#ifdef FEATURE_PREJIT
    if (SystemModule()->HasNativeImage())
    {
        CORCOMPILE_EE_INFO_TABLE *pEEInfo = SystemModule()->GetNativeImage()->GetNativeEEInfoTable();
        pEEInfo->emptyString = (CORINFO_Object **)StringObject::GetEmptyStringRefPtr();
    }
#endif
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

    EXCEPTIONREF pBaseException = (EXCEPTIONREF)AllocateObject(g_pExceptionClass);
    pBaseException->SetHResult(COR_E_EXCEPTION);
    pBaseException->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedBaseException == NULL);
    g_pPreallocatedBaseException = CreateHandle(pBaseException);


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


    EXCEPTIONREF pRudeAbortException = (EXCEPTIONREF)AllocateObject(g_pThreadAbortExceptionClass);
    pRudeAbortException->SetHResult(COR_E_THREADABORTED);
    pRudeAbortException->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedRudeThreadAbortException == NULL);
    g_pPreallocatedRudeThreadAbortException = CreateHandle(pRudeAbortException);


    EXCEPTIONREF pAbortException = (EXCEPTIONREF)AllocateObject(g_pThreadAbortExceptionClass);
    pAbortException->SetHResult(COR_E_THREADABORTED);
    pAbortException->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedThreadAbortException == NULL);
    g_pPreallocatedThreadAbortException = CreateHandle( pAbortException );
}
#endif // CROSSGEN_COMPILE

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

#ifdef FEATURE_PREJIT
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapDisable) != 0)
        g_fAllowNativeImages = false;
#endif

    m_pSystemFile = NULL;
    m_pSystemAssembly = NULL;

    DWORD size = 0;


    // Get the install directory so we can find mscorlib
    hr = GetInternalSystemDirectory(NULL, &size);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        ThrowHR(hr);

    // GetInternalSystemDirectory returns a size, including the null!
    WCHAR *buffer = m_SystemDirectory.OpenUnicodeBuffer(size-1);
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

#ifndef CROSSGEN_COMPILE
        if (!NingenEnabled())
        {
            CreatePreallocatedExceptions();

            PreallocateSpecialObjects();
        }
#endif

        // Finish loading mscorlib now.
        m_pSystemAssembly->GetDomainAssembly()->EnsureActive();
    }

#ifdef _DEBUG
    BOOL fPause = EEConfig::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_PauseOnLoad, FALSE);

    while(fPause)
    {
        ClrSleepEx(20, TRUE);
    }
#endif // _DEBUG
}

#ifndef CROSSGEN_COMPILE
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
#endif // CROSSGEN_COMPILE

void SystemDomain::LoadBaseSystemClasses()
{
    STANDARD_VM_CONTRACT;

    ETWOnStartup(LdSysBases_V1, LdSysBasesEnd_V1);

    {
        m_pSystemFile = PEAssembly::OpenSystem(NULL);
    }
    // Only partially load the system assembly. Other parts of the code will want to access
    // the globals in this function before finishing the load.
    m_pSystemAssembly = DefaultDomain()->LoadDomainAssembly(NULL, m_pSystemFile, FILE_LOAD_POST_LOADLIBRARY)->GetCurrentAssembly();

    // Set up binder for mscorlib
    MscorlibBinder::AttachModule(m_pSystemAssembly->GetManifestModule());

    // Load Object
    g_pObjectClass = MscorlibBinder::GetClass(CLASS__OBJECT);

    // Now that ObjectClass is loaded, we can set up
    // the system for finalizers.  There is no point in deferring this, since we need
    // to know this before we allocate our first object.
    g_pObjectFinalizerMD = MscorlibBinder::GetMethod(METHOD__OBJECT__FINALIZE);


    g_pCanonMethodTableClass = MscorlibBinder::GetClass(CLASS____CANON);

    // NOTE: !!!IMPORTANT!!! ValueType and Enum MUST be loaded one immediately after
    //                       the other, because we have coded MethodTable::IsChildValueType
    //                       in such a way that it depends on this behaviour.
    // Load the ValueType class
    g_pValueTypeClass = MscorlibBinder::GetClass(CLASS__VALUE_TYPE);

    // Load the enum class
    g_pEnumClass = MscorlibBinder::GetClass(CLASS__ENUM);
    _ASSERTE(!g_pEnumClass->IsValueType());

    // Load System.RuntimeType
    g_pRuntimeTypeClass = MscorlibBinder::GetClass(CLASS__CLASS);
    _ASSERTE(g_pRuntimeTypeClass->IsFullyLoaded());

    // Load Array class
    g_pArrayClass = MscorlibBinder::GetClass(CLASS__ARRAY);

    // Calling a method on IList<T> for an array requires redirection to a method on
    // the SZArrayHelper class. Retrieving such methods means calling
    // GetActualImplementationForArrayGenericIListMethod, which calls FetchMethod for
    // the corresponding method on SZArrayHelper. This basically results in a class
    // load due to a method call, which the debugger cannot handle, so we pre-load
    // the SZArrayHelper class here.
    g_pSZArrayHelperClass = MscorlibBinder::GetClass(CLASS__SZARRAYHELPER);

    // Load ByReference class
    //
    // NOTE: ByReference<T> must be the first by-ref-like system type to be loaded,
    //       because MethodTable::ClassifyEightBytesWithManagedLayout depends on it.
    g_pByReferenceClass = MscorlibBinder::GetClass(CLASS__BYREFERENCE);

    // Load Nullable class
    g_pNullableClass = MscorlibBinder::GetClass(CLASS__NULLABLE);

    // Load the Object array class.
    g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT] = ClassLoader::LoadArrayTypeThrowing(TypeHandle(g_pObjectClass)).AsArray();

    // We have delayed allocation of mscorlib's static handles until we load the object class
    MscorlibBinder::GetModule()->AllocateRegularStaticHandles(DefaultDomain());

    g_TypedReferenceMT = MscorlibBinder::GetClass(CLASS__TYPED_REFERENCE);

    // Make sure all primitive types are loaded
    for (int et = ELEMENT_TYPE_VOID; et <= ELEMENT_TYPE_R8; et++)
        MscorlibBinder::LoadPrimitiveType((CorElementType)et);

    MscorlibBinder::LoadPrimitiveType(ELEMENT_TYPE_I);
    MscorlibBinder::LoadPrimitiveType(ELEMENT_TYPE_U);

    // unfortunately, the following cannot be delay loaded since the jit
    // uses it to compute method attributes within a function that cannot
    // handle Complus exception and the following call goes through a path
    // where a complus exception can be thrown. It is unfortunate, because
    // we know that the delegate class and multidelegate class are always
    // guaranteed to be found.
    g_pDelegateClass = MscorlibBinder::GetClass(CLASS__DELEGATE);
    g_pMulticastDelegateClass = MscorlibBinder::GetClass(CLASS__MULTICAST_DELEGATE);

    // used by IsImplicitInterfaceOfSZArray
    MscorlibBinder::GetClass(CLASS__IENUMERABLEGENERIC);
    MscorlibBinder::GetClass(CLASS__ICOLLECTIONGENERIC);
    MscorlibBinder::GetClass(CLASS__ILISTGENERIC);
    MscorlibBinder::GetClass(CLASS__IREADONLYCOLLECTIONGENERIC);
    MscorlibBinder::GetClass(CLASS__IREADONLYLISTGENERIC);

    // Load String
    g_pStringClass = MscorlibBinder::LoadPrimitiveType(ELEMENT_TYPE_STRING);

#ifdef FEATURE_UTF8STRING
    // Load Utf8String
    g_pUtf8StringClass = MscorlibBinder::GetClass(CLASS__UTF8_STRING);
#endif // FEATURE_UTF8STRING

    // Used by Buffer::BlockCopy
    g_pByteArrayMT = ClassLoader::LoadArrayTypeThrowing(
        TypeHandle(MscorlibBinder::GetElementType(ELEMENT_TYPE_U1))).AsArray()->GetMethodTable();

#ifndef CROSSGEN_COMPILE
    CrossLoaderAllocatorHashSetup::EnsureTypesLoaded();
#endif

#ifndef CROSSGEN_COMPILE
    ECall::PopulateManagedStringConstructors();
#endif // CROSSGEN_COMPILE

    g_pExceptionClass = MscorlibBinder::GetClass(CLASS__EXCEPTION);
    g_pOutOfMemoryExceptionClass = MscorlibBinder::GetException(kOutOfMemoryException);
    g_pStackOverflowExceptionClass = MscorlibBinder::GetException(kStackOverflowException);
    g_pExecutionEngineExceptionClass = MscorlibBinder::GetException(kExecutionEngineException);
    g_pThreadAbortExceptionClass = MscorlibBinder::GetException(kThreadAbortException);

    g_pThreadClass = MscorlibBinder::GetClass(CLASS__THREAD);

#ifdef FEATURE_COMINTEROP
    g_pBaseCOMObject = MscorlibBinder::GetClass(CLASS__COM_OBJECT);
    g_pBaseRuntimeClass = MscorlibBinder::GetClass(CLASS__RUNTIME_CLASS);
    
    MscorlibBinder::GetClass(CLASS__IDICTIONARYGENERIC);
    MscorlibBinder::GetClass(CLASS__IREADONLYDICTIONARYGENERIC);
    MscorlibBinder::GetClass(CLASS__ATTRIBUTE);
    MscorlibBinder::GetClass(CLASS__EVENT_HANDLERGENERIC);

    MscorlibBinder::GetClass(CLASS__IENUMERABLE);
    MscorlibBinder::GetClass(CLASS__ICOLLECTION);
    MscorlibBinder::GetClass(CLASS__ILIST);
    MscorlibBinder::GetClass(CLASS__IDISPOSABLE);

#ifdef _DEBUG
    WinRTInterfaceRedirector::VerifyRedirectedInterfaceStubs();
#endif // _DEBUG
#endif

#ifdef FEATURE_ICASTABLE
    g_pICastableInterface = MscorlibBinder::GetClass(CLASS__ICASTABLE);
#endif // FEATURE_ICASTABLE

    // Load a special marker method used to detect Constrained Execution Regions
    // at jit time.
    g_pExecuteBackoutCodeHelperMethod = MscorlibBinder::GetMethod(METHOD__RUNTIME_HELPERS__EXECUTE_BACKOUT_CODE_HELPER);

    // Make sure that FCall mapping for Monitor.Enter is initialized. We need it in case Monitor.Enter is used only as JIT helper. 
    // For more details, see comment in code:JITutil_MonEnterWorker around "__me = GetEEFuncEntryPointMacro(JIT_MonEnter)".
    ECall::GetFCallImpl(MscorlibBinder::GetMethod(METHOD__MONITOR__ENTER));

#ifdef PROFILING_SUPPORTED
    // Note that g_profControlBlock.fBaseSystemClassesLoaded must be set to TRUE only after
    // all base system classes are loaded.  Profilers are not allowed to call any type-loading
    // APIs until g_profControlBlock.fBaseSystemClassesLoaded is TRUE.  It is important that
    // all base system classes need to be loaded before profilers can trigger the type loading.
    g_profControlBlock.fBaseSystemClassesLoaded = TRUE;
#endif // PROFILING_SUPPORTED

#if defined(_DEBUG) && !defined(CROSSGEN_COMPILE)
    if (!NingenEnabled())
    {
        g_Mscorlib.Check();
    }
#endif

#if defined(HAVE_GCCOVER) && defined(FEATURE_PREJIT)
    if (GCStress<cfg_instr_ngen>::IsEnabled())
    {
        // Setting up gc coverage requires the base system classes
        //  to be initialized. So we have deferred it until now for mscorlib.
        Module *pModule = MscorlibBinder::GetModule();
        _ASSERTE(pModule->IsSystem());
        if(pModule->HasNativeImage())
        {
            SetupGcCoverageForNativeImage(pModule);
        }
    }
#endif // defined(HAVE_GCCOVER) && !defined(FEATURE_PREJIT)
}

/*static*/
void SystemDomain::LoadDomain(AppDomain *pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(System()));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    SystemDomain::System()->AddDomain(pDomain);
}

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)

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
    _ASSERTE(pThread);

    if(state == Thread::AS_InSTA)
    {
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InSTA, TRUE);
        _ASSERTE(pState == Thread::AS_InSTA);
    }
    else
    {
        // If an apartment state was not explicitly requested, default to MTA
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InMTA, TRUE);
        _ASSERTE(pState == Thread::AS_InMTA);
    }
}
#endif // defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)

// Helper function to load an assembly. This is called from LoadCOMClass.
/* static */

Assembly *AppDomain::LoadAssemblyHelper(LPCWSTR wszAssembly,
                                        LPCWSTR wszCodeBase)
{
    CONTRACT(Assembly *)
    {
        THROWS;
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(wszAssembly || wszCodeBase);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    AssemblySpec spec;
    if(wszAssembly) {
        #define MAKE_TRANSLATIONFAILED  { ThrowOutOfMemory(); }
        MAKE_UTF8PTR_FROMWIDE(szAssembly,wszAssembly);
        #undef  MAKE_TRANSLATIONFAILED
       
        IfFailThrow(spec.Init(szAssembly));
    }

    if (wszCodeBase) {
        spec.SetCodeBase(wszCodeBase);
    }
    RETURN spec.LoadAssembly(FILE_LOADED);
}

#if defined(FEATURE_CLASSIC_COMINTEROP) && !defined(CROSSGEN_COMPILE)

MethodTable *AppDomain::LoadCOMClass(GUID clsid,
                                     BOOL bLoadRecord/*=FALSE*/,
                                     BOOL* pfAssemblyInReg/*=NULL*/)
{
    // @CORESYSTODO: what to do here?
    // If implemented, this should handle checking that the type actually has the requested CLSID
    return NULL;
}

#endif // FEATURE_CLASSIC_COMINTEROP && !CROSSGEN_COMPILE


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

    // All Reflection Invocation methods are defined in mscorlib.dll
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
        CLASS__LAZY_INITIALIZER,
        CLASS__DYNAMICMETHOD,
        CLASS__DELEGATE,
        CLASS__MULTICAST_DELEGATE
    };

    static const BinderClassID genericReflectionInvocationTypes[] = {
        CLASS__LAZY
    };

    static mdTypeDef genericReflectionInvocationTypeDefs[NumItems(genericReflectionInvocationTypes)];

    static bool fInited = false;

    if (!VolatileLoad(&fInited))
    {
        // Make sure all types are loaded so that we can use faster GetExistingClass()
        for (unsigned i = 0; i < NumItems(reflectionInvocationTypes); i++)
        {
            MscorlibBinder::GetClass(reflectionInvocationTypes[i]);
        }

        // Make sure all types are loaded so that we can use faster GetExistingClass()
        for (unsigned i = 0; i < NumItems(genericReflectionInvocationTypes); i++)
        {
            genericReflectionInvocationTypeDefs[i] = MscorlibBinder::GetClass(genericReflectionInvocationTypes[i])->GetCl();
        }

        VolatileStore(&fInited, true);
    }

    if (pCaller->HasInstantiation())
    {
        // For generic types, pCaller will be an instantiated type and never equal to the type definition.
        // So we compare their TypeDef tokens instead.
        for (unsigned i = 0; i < NumItems(genericReflectionInvocationTypeDefs); i++)
        {
            if (pCaller->GetCl() == genericReflectionInvocationTypeDefs[i])
                return true;
        }
    }
    else
    {
        for (unsigned i = 0; i < NumItems(reflectionInvocationTypes); i++)
        {
            if (MscorlibBinder::GetExistingClass(reflectionInvocationTypes[i]) == pCaller)
                return true;
        }
    }

    return false;
}

#ifndef CROSSGEN_COMPILE
struct CallersDataWithStackMark
{
    StackCrawlMark* stackMark;
    BOOL foundMe;
    MethodDesc* pFoundMethod;
    MethodDesc* pPrevMethod;
    AppDomain*  pAppDomain;
};

/*static*/
MethodDesc* SystemDomain::GetCallersMethod(StackCrawlMark* stackMark,
                                           AppDomain **ppAppDomain/*=NULL*/)

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
        if (ppAppDomain)
            *ppAppDomain = cdata.pAppDomain;
        return cdata.pFoundMethod;
    } else
        return NULL;
}

/*static*/
MethodTable* SystemDomain::GetCallersType(StackCrawlMark* stackMark,
                                          AppDomain **ppAppDomain/*=NULL*/)

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
        if (ppAppDomain)
            *ppAppDomain = cdata.pAppDomain;
        return cdata.pFoundMethod->GetMethodTable();
    } else
        return NULL;
}

/*static*/
Module* SystemDomain::GetCallersModule(StackCrawlMark* stackMark,
                                       AppDomain **ppAppDomain/*=NULL*/)

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
        if (ppAppDomain)
            *ppAppDomain = cdata.pAppDomain;
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
Assembly* SystemDomain::GetCallersAssembly(StackCrawlMark *stackMark,
                                           AppDomain **ppAppDomain/*=NULL*/)
{
    WRAPPER_NO_CONTRACT;
    Module* mod = GetCallersModule(stackMark, ppAppDomain);
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
            pCaller->pAppDomain = pCf->GetAppDomain();
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
    // is only invoked for selected methods in mscorlib itself. So we're
    // reasonably sure we won't have any sensitive methods late bound invoked on
    // constructors, properties or events. This leaves being invoked via
    // MethodInfo, Type or Delegate (and depending on which invoke overload is
    // being used, several different reflection classes may be involved).

    g_IBCLogger.LogMethodDescAccess(pFunc);

    if (SystemDomain::IsReflectionInvocationMethod(pFunc))
        return SWA_CONTINUE;

    if (frame && frame->GetFrameType() == Frame::TYPE_MULTICAST)
    {
        // This must be either a secure delegate frame or a true multicast delegate invocation.

        _ASSERTE(pFunc->GetMethodTable()->IsDelegate());

        DELEGATEREF del = (DELEGATEREF)((SecureDelegateFrame*)frame)->GetThis(); // This can throw.

        if (COMDelegate::IsSecureDelegate(del))
        {
            if (del->IsWrapperDelegate())
            {
                // On ARM, we use secure delegate infrastructure to preserve R4 register.
                return SWA_CONTINUE;
            }
            // For a secure delegate frame, we should return the delegate creator instead
            // of the delegate method itself.
            pFunc = (MethodDesc*) del->GetMethodPtrAux();
        }
        else
        {
            _ASSERTE(COMDelegate::IsTrueMulticastDelegate(del));
            return SWA_CONTINUE;
        }
    }

    // Return the first non-reflection/remoting frame if no stack mark was
    // supplied.
    if (!pCaller->stackMark)
    {
        pCaller->pFoundMethod = pFunc;
        pCaller->pAppDomain = pCf->GetAppDomain();
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

    // If remoting is not available, we only set the caller if the crawlframe is from the same domain.
    // Why? Because if the callerdomain is different from current domain,
    // there have to be interop/native frames in between.
    // For example, in the CORECLR, if we find the caller to be in a different domain, then the 
    // call into reflection is due to an unmanaged call into mscorlib. For that
    // case, the caller really is an INTEROP method.
    // In general, if the caller is INTEROP, we set the caller/callerdomain to be NULL 
    // (To be precise: they are already NULL and we don't change them).
    if (pCf->GetAppDomain() == GetAppDomain())
    // We must either be looking for the caller, or the caller's caller when
    // we've already found the caller (we used a non-null value in pFoundMethod
    // simply as a flag, the correct method to return in both case is the
    // current method).
    {
        pCaller->pFoundMethod = pFunc;
        pCaller->pAppDomain = pCf->GetAppDomain();
    }

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
#endif // CROSSGEN_COMPILE

#ifdef CROSSGEN_COMPILE
// defined in compile.cpp
extern CompilationDomain * theDomain;
#endif

void AppDomain::Create()
{
    STANDARD_VM_CONTRACT;

#ifdef CROSSGEN_COMPILE
    AppDomainRefHolder pDomain(theDomain);
#else
    AppDomainRefHolder pDomain(new AppDomain());
#endif

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

void SystemDomain::AddDomain(AppDomain* pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer((pDomain)));
    }
    CONTRACTL_END;

    {
        LockHolder lh;

        _ASSERTE (pDomain->m_Stage != AppDomain::STAGE_CREATING);
        if (pDomain->m_Stage == AppDomain::STAGE_READYFORMANAGEDCODE ||
            pDomain->m_Stage == AppDomain::STAGE_ACTIVE)
        {
            pDomain->SetStage(AppDomain::STAGE_OPEN);
        }
    }

    // Note that if you add another path that can reach here without calling
    // PublishAppDomainAndInformDebugger, then you should go back & make sure
    // that PADAID gets called.  Right after this call, if not sooner.
    LOG((LF_CORDB, LL_INFO1000, "SD::AD:Would have added domain here! 0x%x\n",
        pDomain));
}

BOOL SystemDomain::RemoveDomain(AppDomain* pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDomain));
        PRECONDITION(!pDomain->IsDefaultDomain());    
    }
    CONTRACTL_END;

    // You can not remove the default domain.


    if (!pDomain->IsActive())
        return FALSE;

    pDomain->Release();

    return TRUE;
}


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
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        g_profControlBlock.pProfInterface->AppDomainCreationStarted((AppDomainID) System());
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID) System(), S_OK);
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        g_profControlBlock.pProfInterface->AppDomainCreationStarted((AppDomainID) System()->DefaultDomain());
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID) System()->DefaultDomain(), S_OK);
        END_PIN_PROFILER();
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
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        g_profControlBlock.pProfInterface->AppDomainShutdownStarted((AppDomainID) System());
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System());
        g_profControlBlock.pProfInterface->AppDomainShutdownFinished((AppDomainID) System(), S_OK);
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        g_profControlBlock.pProfInterface->AppDomainShutdownStarted((AppDomainID) System()->DefaultDomain());
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(System()->DefaultDomain());
        g_profControlBlock.pProfInterface->AppDomainShutdownFinished((AppDomainID) System()->DefaultDomain(), S_OK);
        END_PIN_PROFILER();
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
    m_pRCWRefCache = NULL;
    memset(m_rpCLRTypes, 0, sizeof(m_rpCLRTypes));
#endif // FEATURE_COMINTEROP

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
    m_dwRefTakers=0;
    m_dwCreationHolders=0;
#endif

#ifdef FEATURE_TYPEEQUIVALENCE
    m_pTypeEquivalenceTable = NULL;
#endif // FEATURE_TYPEEQUIVALENCE

#ifdef FEATURE_COMINTEROP
    m_pNameToTypeMap = NULL;
    m_vNameToTypeMapVersion = 0;
    m_nEpoch = 0;
    m_pWinRTFactoryCache = NULL;
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_PREJIT
    m_pDomainFileWithNativeImageList = NULL;
#endif

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

#ifndef CROSSGEN_COMPILE

    _ASSERTE(m_dwCreationHolders == 0);

    // release the TPIndex.  note that since TPIndex values are recycled the TPIndex
    // can only be released once all threads in the AppDomain have exited.
    if (GetTPIndex().m_dwIndex != 0)
        PerAppDomainTPCountList::ResetAppDomainIndex(GetTPIndex());

    m_AssemblyCache.Clear();

#ifdef FEATURE_COMINTEROP
    if (m_pNameToTypeMap != nullptr)
    {
        delete m_pNameToTypeMap;
        m_pNameToTypeMap = nullptr;
    }
    if (m_pWinRTFactoryCache != nullptr)
    {
        delete m_pWinRTFactoryCache;
        m_pWinRTFactoryCache = nullptr;
    }
#endif //FEATURE_COMINTEROP
    
#endif // CROSSGEN_COMPILE
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

#ifndef CROSSGEN_COMPILE
    //Allocate the threadpool entry before the appdomain id list. Otherwise,
    //the thread pool list will be out of sync if insertion of id in 
    //the appdomain fails. 
    m_tpIndex = PerAppDomainTPCountList::AddNewTPIndex();    
#endif // CROSSGEN_COMPILE

    BaseDomain::Init();

// Set up the binding caches
    m_AssemblyCache.Init(&m_DomainCacheCrst, GetHighFrequencyHeap());
    m_UnmanagedCache.InitializeTable(this, &m_DomainCacheCrst);

    m_MemoryPressure = 0;

#ifndef CROSSGEN_COMPILE

    // Default domain reuses the handletablemap that was created during EEStartup
    m_handleStore = GCHandleUtilities::GetGCHandleManager()->GetGlobalHandleStore();

    if (!m_handleStore)
    {
        COMPlusThrowOM();
    }

#endif // CROSSGEN_COMPILE

#ifdef FEATURE_TYPEEQUIVALENCE
    m_TypeEquivalenceCrst.Init(CrstTypeEquivalenceMap);
#endif

    m_ReflectionCrst.Init(CrstReflection, CRST_UNSAFE_ANYMODE);
    m_RefClassFactCrst.Init(CrstClassFactInfoHash);

    {
        LockOwner lock = {&m_DomainCrst, IsOwnerOfCrst};
        m_clsidHash.Init(0,&CompareCLSID,true, &lock); // init hash table
    }

    SetStage(STAGE_READYFORMANAGEDCODE);

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_TIERED_COMPILATION
    m_tieredCompilationManager.Init();
#endif
#endif // CROSSGEN_COMPILE
} // AppDomain::Init


/*********************************************************************/

BOOL AppDomain::IsCompilationDomain()
{
    LIMITED_METHOD_CONTRACT;

    BOOL isCompilationDomain = (m_dwFlags & COMPILATION_DOMAIN) != 0;
#ifdef FEATURE_PREJIT
    _ASSERTE(!isCompilationDomain || IsCompilationProcess());
#endif // FEATURE_PREJIT
    return isCompilationDomain;
}

#ifndef CROSSGEN_COMPILE

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

#endif // !CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP
MethodTable *AppDomain::GetRedirectedType(WinMDAdapter::RedirectedTypeIndex index)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we have the type loaded already, use that
    if (m_rpCLRTypes[index] != nullptr)
    {
        return m_rpCLRTypes[index];
    }

    WinMDAdapter::FrameworkAssemblyIndex frameworkAssemblyIndex;
    WinMDAdapter::GetRedirectedTypeInfo(index, nullptr, nullptr, nullptr, &frameworkAssemblyIndex, nullptr, nullptr);
    MethodTable * pMT = LoadRedirectedType(index, frameworkAssemblyIndex);
    m_rpCLRTypes[index] = pMT;
    return pMT;
}

MethodTable* AppDomain::LoadRedirectedType(WinMDAdapter::RedirectedTypeIndex index, WinMDAdapter::FrameworkAssemblyIndex assembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(index < WinMDAdapter::RedirectedTypeIndex_Count);
    }
    CONTRACTL_END;

    LPCSTR szClrNamespace;
    LPCSTR szClrName;
    LPCSTR szFullWinRTName;
    WinMDAdapter::FrameworkAssemblyIndex nFrameworkAssemblyIndex;

    WinMDAdapter::GetRedirectedTypeInfo(index, &szClrNamespace, &szClrName, &szFullWinRTName, &nFrameworkAssemblyIndex, nullptr, nullptr);

    _ASSERTE(nFrameworkAssemblyIndex >= WinMDAdapter::FrameworkAssembly_Mscorlib &&
             nFrameworkAssemblyIndex < WinMDAdapter::FrameworkAssembly_Count);

    if (assembly != nFrameworkAssemblyIndex)
    {
        // The framework type does not live in the assembly we were requested to load redirected types from
        return nullptr;
    }
    else if (nFrameworkAssemblyIndex == WinMDAdapter::FrameworkAssembly_Mscorlib)
    {
        return ClassLoader::LoadTypeByNameThrowing(MscorlibBinder::GetModule()->GetAssembly(),
                                                   szClrNamespace,
                                                   szClrName,
                                                   ClassLoader::ThrowIfNotFound,
                                                   ClassLoader::LoadTypes,
                                                   CLASS_LOAD_EXACTPARENTS).GetMethodTable();
    }
    else
    {
        LPCSTR pSimpleName;
        AssemblyMetaDataInternal context;
        const BYTE * pbKeyToken;
        DWORD cbKeyTokenLength;
        DWORD dwFlags;

        WinMDAdapter::GetExtraAssemblyRefProps(nFrameworkAssemblyIndex,
                                               &pSimpleName,
                                               &context,
                                               &pbKeyToken,
                                               &cbKeyTokenLength,
                                               &dwFlags);

        Assembly* pAssembly = AssemblySpec::LoadAssembly(pSimpleName,
                                                         &context,
                                                         pbKeyToken,
                                                         cbKeyTokenLength,
                                                         dwFlags);

        return ClassLoader::LoadTypeByNameThrowing(
            pAssembly,
            szClrNamespace,
            szClrName,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::LoadTypes,
            CLASS_LOAD_EXACTPARENTS).GetMethodTable();
    }
}
#endif //FEATURE_COMINTEROP

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
    "VERIFY_EXECUTION",                   // FILE_LOAD_VERIFY_EXECUTION
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

#ifndef CROSSGEN_COMPILE
// Thread stress
class LoadDomainAssemblyStress : APIThreadStress
{
public:
    AppDomain *pThis;
    AssemblySpec* pSpec;
    PEAssembly *pFile;
    FileLoadLevel targetLevel;

    LoadDomainAssemblyStress(AppDomain *pThis, AssemblySpec* pSpec, PEAssembly *pFile, FileLoadLevel targetLevel)
        : pThis(pThis), pSpec(pSpec), pFile(pFile), targetLevel(targetLevel) {LIMITED_METHOD_CONTRACT;}

    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        SetupThread();
        pThis->LoadDomainAssembly(pSpec, pFile, targetLevel);
    }
};
#endif // CROSSGEN_COMPILE

extern BOOL AreSameBinderInstance(ICLRPrivBinder *pBinderA, ICLRPrivBinder *pBinderB);

DomainAssembly* AppDomain::LoadDomainAssembly( AssemblySpec* pSpec,
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
        Exception* pEx=GET_EXCEPTION();
        if (!pEx->IsTransient())
        {
            // Setup the binder reference in AssemblySpec from the PEAssembly if one is not already set.
            ICLRPrivBinder* pCurrentBindingContext = pSpec->GetBindingContext();
            ICLRPrivBinder* pBindingContextFromPEAssembly = pFile->GetBindingContext();
            
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
                _ASSERTE(AreSameBinderInstance(pCurrentBindingContext, pBindingContextFromPEAssembly));
            }
#endif // _DEBUG            

            if (!EEFileLoadException::CheckType(pEx))
            {
                StackSString name;
                pSpec->GetFileOrDisplayName(0, name);
                pEx=new EEFileLoadException(name, pEx->GetHR(), NULL, pEx);
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

#ifndef CROSSGEN_COMPILE
    LoadDomainAssemblyStress ts (this, pIdentity, pFile, targetLevel);
#endif

    // Go into preemptive mode since this may take a while.
    GCX_PREEMP();

    // Check for existing fully loaded assembly, or for an assembly which has failed during the loading process.
    result = FindAssembly(pFile, FindAssemblyOptions_IncludeFailedToLoad);
    
    if (result == NULL)
    {
        LoaderAllocator *pLoaderAllocator = NULL;

#ifndef CROSSGEN_COMPILE
        ICLRPrivBinder *pFileBinder = pFile->GetBindingContext();
        if (pFileBinder != NULL)
        {
            // Assemblies loaded with AssemblyLoadContext need to use a different LoaderAllocator if
            // marked as collectible
            pFileBinder->GetLoaderAllocator((LPVOID*)&pLoaderAllocator);
        }
#endif // !CROSSGEN_COMPILE

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
        if (fileLock == NULL)
        {
            // Check again in case we were racing
            result = FindAssembly(pFile, FindAssemblyOptions_IncludeFailedToLoad);
            if (result == NULL)
            {
                // We are the first one in - create the DomainAssembly
                fileLock = FileLoadLock::Create(lock, pFile, pDomainAssembly);
                pDomainAssembly.SuppressRelease();
#ifndef CROSSGEN_COMPILE
                if (pDomainAssembly->IsCollectible())
                {
                    // We add the assembly to the LoaderAllocator only when we are sure that it can be added
                    // and won't be deleted in case of a concurrent load from the same ALC
                    ((AssemblyLoaderAllocator *)pLoaderAllocator)->AddDomainAssembly(pDomainAssembly);
                }
#endif // !CROSSGEN_COMPILE
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
    // Do not cache WindowsRuntime assemblies, they are cached in code:CLRPrivTypeCacheWinRT
    if ((pIdentity != NULL) && (pIdentity->CanUseWithBindingCache()) && (result->CanUseWithBindingCache()))
        GetAppDomain()->AddAssemblyToCache(pIdentity, result);
    
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

    // Thread stress
    APIThreadStress::SyncThreadStress();

    DomainFile *pFile = pLock->GetDomainFile();

    // Make sure we release the lock on exit
    FileLoadLockRefHolder lockRef(pLock);

    // We need to perform the early steps of loading mscorlib without a domain transition.  This is
    // important for bootstrapping purposes - we need to get mscorlib at least partially loaded
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
            // Thread stress
            APIThreadStress::SyncThreadStress();

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

static void NormalizeAssemblySpecForNativeDependencies(AssemblySpec * pSpec)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pSpec->IsStrongNamed() && pSpec->HasPublicKey())
    {
        pSpec->ConvertPublicKeyToToken();
    }

    //
    // CoreCLR binder unifies assembly versions. Ignore assembly version here to 
    // detect more types of potential mismatches.
    //
    AssemblyMetaDataInternal * pContext = pSpec->GetContext();
    pContext->usMajorVersion = (USHORT)-1;
    pContext->usMinorVersion = (USHORT)-1;
    pContext->usBuildNumber = (USHORT)-1;
    pContext->usRevisionNumber = (USHORT)-1;

    // Ignore the WinRT type while considering if two assemblies have the same identity.
    pSpec->SetWindowsRuntimeType(NULL, NULL);    
}

void AppDomain::CheckForMismatchedNativeImages(AssemblySpec * pSpec, const GUID * pGuid)
{
    STANDARD_VM_CONTRACT;

    //
    // The native images are ever used only for trusted images in CoreCLR.
    // We don't wish to open the IL file at runtime so we just forgo any
    // eager consistency checking. But we still want to prevent mistmatched 
    // NGen images from being used. We record all mappings between assembly 
    // names and MVID, and fail once we detect mismatch.
    //
    NormalizeAssemblySpecForNativeDependencies(pSpec);

    CrstHolder ch(&m_DomainCrst);

    const NativeImageDependenciesEntry * pEntry = m_NativeImageDependencies.Lookup(pSpec);

    if (pEntry != NULL)
    {
        if (*pGuid != pEntry->m_guidMVID)
        {
            SString msg;
            msg.Printf("ERROR: Native images generated against multiple versions of assembly %s. ", pSpec->GetName());
            WszOutputDebugString(msg.GetUnicode());
            COMPlusThrowNonLocalized(kFileLoadException, msg.GetUnicode());
        }
    }
    else
    {
        //
        // No entry yet - create one
        //
        NativeImageDependenciesEntry * pNewEntry = new NativeImageDependenciesEntry();
        pNewEntry->m_AssemblySpec.CopyFrom(pSpec);
        pNewEntry->m_AssemblySpec.CloneFields(AssemblySpec::ALL_OWNED);
        pNewEntry->m_guidMVID = *pGuid;
        m_NativeImageDependencies.Add(pNewEntry);
    }
}

BOOL AppDomain::RemoveNativeImageDependency(AssemblySpec * pSpec)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pSpec));
    }
    CONTRACTL_END;

    BOOL result = FALSE;
    NormalizeAssemblySpecForNativeDependencies(pSpec);

    CrstHolder ch(&m_DomainCrst);

    const NativeImageDependenciesEntry * pEntry = m_NativeImageDependencies.Lookup(pSpec);

    if (pEntry != NULL)
    {
        m_NativeImageDependencies.Remove(pSpec);
        delete pEntry;
        result = TRUE;
    }

    return result;
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

#ifndef CROSSGEN_COMPILE
    if (NingenEnabled())
        return;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: SetupSharedStatics()"));

    // don't do any work in init stage. If not init only do work in non-shared case if are default domain
    _ASSERTE(!g_fEEInit);

    // Because we are allocating/referencing objects, need to be in cooperative mode
    GCX_COOP();

    DomainLocalModule *pLocalModule = MscorlibBinder::GetModule()->GetDomainLocalModule();

    // This is a convenient place to initialize String.Empty.
    // It is treated as intrinsic by the JIT as so the static constructor would never run.
    // Leaving it uninitialized would confuse debuggers.

    // String should not have any static constructors.
    _ASSERTE(g_pStringClass->IsClassPreInited());

    FieldDesc * pEmptyStringFD = MscorlibBinder::GetField(FIELD__STRING__EMPTY);
    OBJECTREF* pEmptyStringHandle = (OBJECTREF*)
        ((TADDR)pLocalModule->GetPrecomputedGCStaticsBasePointer()+pEmptyStringFD->GetOffset());
    SetObjectReference( pEmptyStringHandle, StringObject::GetEmptyString());
#endif // CROSSGEN_COMPILE
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

static const AssemblyIterationFlags STANDARD_IJW_ITERATOR_FLAGS = 
    (AssemblyIterationFlags)(kIncludeLoaded | kIncludeLoading | kIncludeExecution | kExcludeCollectible);


void AppDomain::SetFriendlyName(LPCWSTR pwzFriendlyName, BOOL fDebuggerCares/*=TRUE*/)
{
    CONTRACTL
    {
        THROWS;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

#if _DEBUG
    // Handle NULL this pointer - this happens sometimes when printing log messages
    // but in general shouldn't occur in real code
    if (this == NULL)
        RETURN NULL;
#endif // _DEBUG

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
#if _DEBUG
    // Handle NULL this pointer - this happens sometimes when printing log messages
    // but in general shouldn't occur in real code
    if (this == NULL)
        RETURN NULL;
#endif // _DEBUG
    RETURN (m_friendlyName.IsEmpty() ?W(""):(LPCWSTR)m_friendlyName);
}

LPCWSTR AppDomain::GetFriendlyNameForDebugger()
{
    CONTRACT (LPCWSTR)
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
        // Hosted fusion binder makes an exception here, so we cannot assert.
        //PRECONDITION(pSpec->CanUseWithBindingCache());
        //PRECONDITION(pFile->CanUseWithBindingCache());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CrstHolder holder(&m_DomainCacheCrst);
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
        PRECONDITION(pSpec->CanUseWithBindingCache());
        PRECONDITION(pAssembly->CanUseWithBindingCache());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    
    CrstHolder holder(&m_DomainCacheCrst);
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
        PRECONDITION(pSpec->CanUseWithBindingCache());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    
    if (ex->IsTransient())
        return TRUE;

    CrstHolder holder(&m_DomainCacheCrst);
    // !!! suppress exceptions
    return m_AssemblyCache.StoreException(pSpec, ex);
}

void AppDomain::AddUnmanagedImageToCache(LPCWSTR libraryName, NATIVE_LIBRARY_HANDLE hMod)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(libraryName));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (libraryName)
    {
        AssemblySpec spec;
        spec.SetCodeBase(libraryName);
        m_UnmanagedCache.InsertEntry(&spec, hMod);
    }
    return ;
}


NATIVE_LIBRARY_HANDLE AppDomain::FindUnmanagedImageInCache(LPCWSTR libraryName)
{
    CONTRACT(NATIVE_LIBRARY_HANDLE)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(libraryName,NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;
    if(libraryName == NULL) RETURN NULL;

    AssemblySpec spec;
    spec.SetCodeBase(libraryName);
    RETURN (NATIVE_LIBRARY_HANDLE) m_UnmanagedCache.LookupEntry(&spec, 0);
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
    
    CrstHolder holder(&m_DomainCacheCrst);

    return m_AssemblyCache.RemoveAssembly(pAssembly);
}

BOOL AppDomain::IsCached(AssemblySpec *pSpec)
{
    WRAPPER_NO_CONTRACT;

    // Check to see if this fits our rather loose idea of a reference to mscorlib.
    // If so, don't use fusion to bind it - do it ourselves.
    if (pSpec->IsMscorlib())
        return TRUE;

    return m_AssemblyCache.Contains(pSpec);
}

void AppDomain::GetCacheAssemblyList(SetSHash<PTR_DomainAssembly>& assemblyList)
{
    CrstHolder holder(&m_DomainCacheCrst);
    m_AssemblyCache.GetAllAssemblies(assemblyList);
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

    // Check to see if this fits our rather loose idea of a reference to mscorlib.
    // If so, don't use fusion to bind it - do it ourselves.
    if (fThrow && pSpec->IsMscorlib())
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
        result = TryResolveAssembly(*ppFailedSpec);

        if (result != NULL && pPrePolicySpec->CanUseWithBindingCache() && result->CanUseWithBindingCache())
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
            if (*ppFailedSpec != pPrePolicySpec && pPostPolicySpec->CanUseWithBindingCache())
            {
                AddFileToCache(pPostPolicySpec, result, TRUE /* fAllowFailure */ );
            }
        }
    }

    return fFailure;
}

//----------------------------------------------------------------------------------------
// Helper class for hosted binder

class PEAssemblyAsPrivAssemblyInfo : public IUnknownCommon<ICLRPrivAssemblyInfo>
{
public:
    //------------------------------------------------------------------------------------
    // Ctor

    PEAssemblyAsPrivAssemblyInfo(PEAssembly *pPEAssembly)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_THROWS;

        if (pPEAssembly == nullptr)
            ThrowHR(E_UNEXPECTED);

        pPEAssembly->AddRef();
        m_pPEAssembly = pPEAssembly;
    }

    //------------------------------------------------------------------------------------
    // ICLRPrivAssemblyInfo methods

    //------------------------------------------------------------------------------------
    STDMETHOD(GetAssemblyName)(
        __in  DWORD cchBuffer,
        __out_opt LPDWORD pcchBuffer,
        __out_ecount_part_opt(cchBuffer, *pcchBuffer) LPWSTR wzBuffer)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;

        if ((cchBuffer == 0) != (wzBuffer == nullptr))
        {
            return E_INVALIDARG;
        }

        LPCUTF8 szName = m_pPEAssembly->GetSimpleName();

        bool bIsAscii;
        DWORD cchName;
        IfFailRet(FString::Utf8_Unicode_Length(szName, &bIsAscii, &cchName));

        if (cchBuffer < cchName + 1)
        {
            if (pcchBuffer != nullptr)
            {
                *pcchBuffer = cchName + 1;
            }
            return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }
        else
        {
            IfFailRet(FString::Utf8_Unicode(szName, bIsAscii, wzBuffer, cchName));
            if (pcchBuffer != nullptr)
            {
                *pcchBuffer = cchName;
            }
            return S_OK;
        }
    }

    //------------------------------------------------------------------------------------
    STDMETHOD(GetAssemblyVersion)(
        USHORT *pMajor,
        USHORT *pMinor,
        USHORT *pBuild,
        USHORT *pRevision)
    {
        WRAPPER_NO_CONTRACT;
        return m_pPEAssembly->GetVersion(pMajor, pMinor, pBuild, pRevision);
    }

    //------------------------------------------------------------------------------------
    STDMETHOD(GetAssemblyPublicKey)(
        DWORD cbBuffer,
        LPDWORD pcbBuffer,
        BYTE *pbBuffer)
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        STATIC_CONTRACT_CAN_TAKE_LOCK;

        VALIDATE_PTR_RET(pcbBuffer);
        VALIDATE_CONDITION((pbBuffer == nullptr) == (cbBuffer == 0), return E_INVALIDARG);

        HRESULT hr = S_OK;

        EX_TRY
        {
            // Note: PEAssembly::GetPublicKey will return bogus data pointer when *pcbBuffer == 0
            LPCVOID pbKey = m_pPEAssembly->GetPublicKey(pcbBuffer);

            if (*pcbBuffer != 0)
            {
                if (pbBuffer != nullptr && cbBuffer >= *pcbBuffer)
                {
                    memcpy(pbBuffer, pbKey, *pcbBuffer);
                    hr = S_OK;
                }
                else
                {
                    hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                }
            }
            else
            {
                hr = S_FALSE; // ==> No public key
            }
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

private:
    ReleaseHolder<PEAssembly> m_pPEAssembly;
};

//-----------------------------------------------------------------------------------------------------------------
HRESULT AppDomain::BindAssemblySpecForHostedBinder(
    AssemblySpec *   pSpec, 
    IAssemblyName *  pAssemblyName, 
    ICLRPrivBinder * pBinder, 
    PEAssembly **    ppAssembly)
{
    STANDARD_VM_CONTRACT;
    
    PRECONDITION(CheckPointer(pSpec));
    PRECONDITION(pSpec->GetAppDomain() == this);
    PRECONDITION(CheckPointer(ppAssembly));
    PRECONDITION(pSpec->GetCodeBase() == nullptr);

    HRESULT hr = S_OK;


    // The Fusion binder can throw (to preserve compat, since it will actually perform an assembly
    // load as part of it's bind), so we need to be careful here to catch any FileNotFoundException
    // objects if fThrowIfNotFound is false.
    ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;

    // We return HRESULTs here on failure instead of throwing as failures here are not necessarily indicative
    // of an actual application problem. Returning an error code is substantially faster than throwing, and
    // should be used when possible.
    IfFailRet(pBinder->BindAssemblyByName(pAssemblyName, &pPrivAssembly));

    IfFailRet(BindHostedPrivAssembly(nullptr, pPrivAssembly, pAssemblyName, ppAssembly));


    return S_OK;
}

//-----------------------------------------------------------------------------------------------------------------
HRESULT 
AppDomain::BindHostedPrivAssembly(
    PEAssembly *       pParentAssembly,
    ICLRPrivAssembly * pPrivAssembly, 
    IAssemblyName *    pAssemblyName, 
    PEAssembly **      ppAssembly)
{
    STANDARD_VM_CONTRACT;

    PRECONDITION(CheckPointer(pPrivAssembly));
    PRECONDITION(CheckPointer(ppAssembly));
    
    HRESULT hr = S_OK;
    
    *ppAssembly = nullptr;
    
    // See if result has been previously loaded.
    {
        DomainAssembly* pDomainAssembly = FindAssembly(pPrivAssembly);
        if (pDomainAssembly != nullptr)
        {
            *ppAssembly = clr::SafeAddRef(pDomainAssembly->GetFile());
        }
    }

    if (*ppAssembly != nullptr)
    {   // Already exists: return the assembly.
        return S_OK;
    }

    // Get the IL PEFile.
    PEImageHolder pPEImageIL;
    {
        // Does not already exist, so get the resource for the assembly and load it.
        DWORD dwImageType;
        ReleaseHolder<ICLRPrivResource> pIResourceIL;

        IfFailRet(pPrivAssembly->GetImageResource(ASSEMBLY_IMAGE_TYPE_IL, &dwImageType, &pIResourceIL));
        _ASSERTE(dwImageType == ASSEMBLY_IMAGE_TYPE_IL);

        pPEImageIL = PEImage::OpenImage(pIResourceIL, MDInternalImport_Default);
    }

    // See if an NI is available.
    DWORD dwAvailableImages;
    IfFailRet(pPrivAssembly->GetAvailableImageTypes(&dwAvailableImages));
    _ASSERTE(dwAvailableImages & ASSEMBLY_IMAGE_TYPE_IL); // Just double checking that IL bit is always set.

    // Get the NI PEFile if available.
    PEImageHolder pPEImageNI;
#ifdef FEATURE_PREJIT
    if (dwAvailableImages & ASSEMBLY_IMAGE_TYPE_NATIVE)
    {
        DWORD dwImageType;
        ReleaseHolder<ICLRPrivResource> pIResourceNI;

        IfFailRet(pPrivAssembly->GetImageResource(ASSEMBLY_IMAGE_TYPE_NATIVE, &dwImageType, &pIResourceNI));
        _ASSERTE(dwImageType == ASSEMBLY_IMAGE_TYPE_NATIVE || FAILED(hr));

        pPEImageNI = PEImage::OpenImage(pIResourceNI, MDInternalImport_TrustedNativeImage);
    }
#endif // FEATURE_PREJIT
    _ASSERTE(pPEImageIL != nullptr);
    
    // Create a PEAssembly using the IL and NI images.
    PEAssemblyHolder pPEAssembly = PEAssembly::Open(pParentAssembly, pPEImageIL, pPEImageNI, pPrivAssembly);

    // The result.    
    *ppAssembly = pPEAssembly.Extract();

    return S_OK;
} // AppDomain::BindHostedPrivAssembly

//---------------------------------------------------------------------------------------------------------------------
PEAssembly * AppDomain::BindAssemblySpec(
    AssemblySpec *         pSpec, 
    BOOL                   fThrowOnFileNotFound, 
    BOOL                   fUseHostBinderIfAvailable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    PRECONDITION(CheckPointer(pSpec));
    PRECONDITION(pSpec->GetAppDomain() == this);
    PRECONDITION(this==::GetAppDomain());

    GCX_PREEMP();

    BOOL fForceReThrow = FALSE;

#if defined(FEATURE_COMINTEROP)
    // Handle WinRT assemblies in the classic/hybrid scenario. If this is an AppX process,
    // then this case will be handled by the previous block as part of the full set of
    // available binding hosts.
    if (pSpec->IsContentType_WindowsRuntime())
    {
        HRESULT hr = S_OK;

        // Get the assembly display name.
        ReleaseHolder<IAssemblyName> pAssemblyName;

        IfFailThrow(pSpec->CreateFusionName(&pAssemblyName, TRUE, TRUE));


        PEAssemblyHolder pAssembly;

        EX_TRY
        {
            hr = BindAssemblySpecForHostedBinder(pSpec, pAssemblyName, m_pWinRtBinder, &pAssembly);
            if (FAILED(hr))
                goto EndTry2; // Goto end of try block.

            PTR_CLRPrivAssemblyWinRT assem = dac_cast<PTR_CLRPrivAssemblyWinRT>(pAssembly->GetHostAssembly());
            assem->SetFallbackBinder(pSpec->GetHostBinder());
EndTry2:;
        }
        // The combination of this conditional catch/ the following if statement which will throw reduces the count of exceptions 
        // thrown in scenarios where the exception does not escape the method. We cannot get rid of the try/catch block, as
        // there are cases within some of the clrpriv binder's which throw.
        // Note: In theory, FileNotFound should always come here as HRESULT, never as exception.
        EX_CATCH_HRESULT_IF(hr,
            !fThrowOnFileNotFound && Assembly::FileNotFound(hr))

        if (FAILED(hr) && (fThrowOnFileNotFound || !Assembly::FileNotFound(hr)))
        {
            if (Assembly::FileNotFound(hr))
            {
                _ASSERTE(fThrowOnFileNotFound);
                // Uses defaultScope
                EEFileLoadException::Throw(pSpec, hr);
            }

            // WinRT type bind failures
            _ASSERTE(pSpec->IsContentType_WindowsRuntime());
            if (hr == HRESULT_FROM_WIN32(APPMODEL_ERROR_NO_PACKAGE)) // Returned by RoResolveNamespace when using 3rd party WinRT types in classic process
            {
                if (fThrowOnFileNotFound)
                {   // Throw NotSupportedException (with custom message) wrapped by TypeLoadException to give user type name for diagnostics
                    // Note: TypeLoadException is equivalent of FileNotFound in WinRT world
                    EEMessageException ex(kNotSupportedException, IDS_EE_WINRT_THIRDPARTY_NOTSUPPORTED);
                    EX_THROW_WITH_INNER(EETypeLoadException, (pSpec->GetWinRtTypeNamespace(), pSpec->GetWinRtTypeClassName(), nullptr, nullptr, IDS_EE_WINRT_LOADFAILURE), &ex);
                }
            }
            else if ((hr == CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT) || // Returned e.g. for WinRT type name without namespace
                     (hr == COR_E_PLATFORMNOTSUPPORTED)) // Using WinRT on pre-Win8 OS
            {
                if (fThrowOnFileNotFound)
                {   // Throw ArgumentException/PlatformNotSupportedException wrapped by TypeLoadException to give user type name for diagnostics
                    // Note: TypeLoadException is equivalent of FileNotFound in WinRT world
                    EEMessageException ex(hr);
                    EX_THROW_WITH_INNER(EETypeLoadException, (pSpec->GetWinRtTypeNamespace(), pSpec->GetWinRtTypeClassName(), nullptr, nullptr, IDS_EE_WINRT_LOADFAILURE), &ex);
                }
            }
            else
            {
                IfFailThrow(hr);
            }
        }
        _ASSERTE((FAILED(hr) && !fThrowOnFileNotFound) || pAssembly != nullptr);

        return pAssembly.Extract();
    }
    else
#endif // FEATURE_COMINTEROP
    if (pSpec->HasUniqueIdentity())
    {
        HRESULT hrBindResult = S_OK;
        PEAssemblyHolder result;
        

        EX_TRY
        {
            if (!IsCached(pSpec))
            {

                {
                    bool fAddFileToCache = false;

                    // Use CoreClr's fusion alternative
                    CoreBindResult bindResult;

                    pSpec->Bind(this, FALSE /* fThrowOnFileNotFound */, &bindResult, FALSE /* fNgenExplicitBind */, FALSE /* fExplicitBindToNativeImage */);
                    hrBindResult = bindResult.GetHRBindResult();

                    if (bindResult.Found()) 
                    {
                        if (SystemDomain::SystemFile() && bindResult.IsMscorlib())
                        {
                            // Avoid rebinding to another copy of mscorlib
                            result = SystemDomain::SystemFile();
                            result.SuppressRelease(); // Didn't get a refcount
                        }
                        else
                        {
                            // IsSystem on the PEFile should be false, even for mscorlib satellites
                            result = PEAssembly::Open(&bindResult,
                                                      FALSE);
                        }
                        fAddFileToCache = true;
                        
                        // Setup the reference to the binder, which performed the bind, into the AssemblySpec
                        ICLRPrivBinder* pBinder = result->GetBindingContext();
                        _ASSERTE(pBinder != NULL);
                        pSpec->SetBindingContext(pBinder);
                    }


                    if (fAddFileToCache)
                    {


                        if (pSpec->CanUseWithBindingCache() && result->CanUseWithBindingCache())
                        {
                            // Failure to add simply means someone else beat us to it. In that case
                            // the FindCachedFile call below (after catch block) will update result
                            // to the cached value.
                            AddFileToCache(pSpec, result, TRUE /*fAllowFailure*/);
                        }
                    }
                    else
                    {
                        // Don't trigger the resolve event for the CoreLib satellite assembly. A misbehaving resolve event may
                        // return an assembly that does not match, and this can cause recursive resource lookups during error
                        // reporting. The CoreLib satellite assembly is loaded from relative locations based on the culture, see
                        // AssemblySpec::Bind().
                        if (!pSpec->IsMscorlibSatellite())
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
        if (pSpec->CanUseWithBindingCache() && (result== NULL || result->CanUseWithBindingCache()))
        {
            result = FindCachedFile(pSpec);

            if (result != NULL)
                result->AddRef();
        }

        return result.Extract();
    }
    else
    {
        // Unsupported content type
        if (fThrowOnFileNotFound)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        return nullptr;
    }
} // AppDomain::BindAssemblySpec



PEAssembly *AppDomain::TryResolveAssembly(AssemblySpec *pSpec)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = pSpec->ResolveAssemblyFile(this);
    }
    EX_HOOK
    {
        Exception *pEx = GET_EXCEPTION();

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


#ifndef CROSSGEN_COMPILE

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
        if (MscorlibBinder::GetField(FIELD__ASSEMBLYLOADCONTEXT__ASSEMBLY_LOAD)->GetStaticOBJECTREF() != NULL)
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

    OBJECTREF orDelegate = MscorlibBinder::GetField(FIELD__APPCONTEXT__UNHANDLED_EXCEPTION)->GetStaticOBJECTREF();
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

#endif // CROSSGEN_COMPILE

IUnknown *AppDomain::CreateFusionContext()
{
    CONTRACT(IUnknown *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (!m_pFusionContext)
    {
        ETWOnStartup (FusionAppCtx_V1, FusionAppCtxEnd_V1);
        CLRPrivBinderCoreCLR *pTPABinder = NULL;

        GCX_PREEMP();

        // Initialize the assembly binder for the default context loads for CoreCLR.
        IfFailThrow(CCoreCLRBinderHelper::DefaultBinderSetupContext(DefaultADID, &pTPABinder));
        m_pFusionContext = reinterpret_cast<IUnknown *>(pTPABinder);
        
        // By default, initial binding context setup for CoreCLR is also the TPABinding context
        (m_pTPABinderContext = pTPABinder)->AddRef();

    }

    RETURN m_pFusionContext;
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

void AppDomain::SetSystemAssemblyLoadEventSent(BOOL fFlag)
{
    LIMITED_METHOD_CONTRACT;
    if (fFlag == TRUE)
        m_dwFlags |= LOAD_SYSTEM_ASSEMBLY_EVENT_SENT;
    else
        m_dwFlags &= ~LOAD_SYSTEM_ASSEMBLY_EVENT_SENT;
}

BOOL AppDomain::WasSystemAssemblyLoadEventSent(void)
{
    LIMITED_METHOD_CONTRACT;
    return ((m_dwFlags & LOAD_SYSTEM_ASSEMBLY_EVENT_SENT) == 0) ? FALSE : TRUE;
}

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP

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

    RemoveWinRTFactoryObjects(pCtxCookie);
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
    _ASSERTE(pThread);

    LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::ExceptionUnwind: not first transition or abort\n"));
}

#endif // CROSSGEN_COMPILE

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

#ifndef CROSSGEN_COMPILE
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
#endif // CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE

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

#endif // CROSSGEN_COMPILE

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

    AppDomain::AssemblyIterator asmIterator = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (asmIterator.Next(pDomainAssembly.This()))
    {
        // @TODO: Review when DomainAssemblies get added.
        _ASSERTE(pDomainAssembly != NULL);
        pDomainAssembly->EnumStaticGCRefs(fn, sc);
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

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
// 
// Can be called only from AppDomain shutdown code:AppDomain::ShutdownAssemblies.
// Does not add-ref collectible assemblies (as the LoaderAllocator might not be reachable from the 
// DomainAssembly anymore).
// 
BOOL 
AppDomain::AssemblyIterator::Next_UnsafeNoAddRef(
    DomainAssembly ** ppDomainAssembly)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;
    
    // Make sure we are iterating all assemblies (see the only caller code:AppDomain::ShutdownAssemblies)
    _ASSERTE(m_assemblyIterationFlags == 
        (kIncludeLoaded | kIncludeLoading | kIncludeExecution | kIncludeFailedToLoad | kIncludeCollected));
    // It also means that we do not exclude anything
    _ASSERTE((m_assemblyIterationFlags & kExcludeCollectible) == 0);
    
    // We are on shutdown path, so lock shouldn't be neccessary, but all _Unlocked methods on AssemblyList 
    // have asserts that the lock is held, so why not to take it ...
    CrstHolder ch(m_pAppDomain->GetAssemblyListLock());
    
    while (m_Iterator.Next())
    {
        // Get element from the list/iterator (without adding reference to the assembly)
        *ppDomainAssembly = dac_cast<PTR_DomainAssembly>(m_Iterator.GetElement());
        if (*ppDomainAssembly == NULL)
        {
            continue;
        }
        
        return TRUE;
    }
    
    *ppDomainAssembly = NULL;
    return FALSE;
} // AppDomain::AssemblyIterator::Next_UnsafeNoAddRef


#endif //!DACCESS_COMPILE

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

// Returns S_OK if the assembly was successfully loaded
HRESULT RuntimeInvokeHostAssemblyResolver(INT_PTR pManagedAssemblyLoadContextToBindWithin, IAssemblyName *pIAssemblyName, CLRPrivBinderCoreCLR *pTPABinder, BINDER_SPACE::AssemblyName *pAssemblyName, ICLRPrivAssembly **ppLoadedAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(ppLoadedAssembly != NULL);
    }
    CONTRACTL_END;
    
    HRESULT hr = E_FAIL;

    // DevDiv #933506: Exceptions thrown during AssemblyLoadContext.Load should propagate
    // EX_TRY
    {
        // Switch to COOP mode since we are going to work with managed references
        GCX_COOP();
        
        struct 
        {
            ASSEMBLYNAMEREF oRefAssemblyName;
            ASSEMBLYREF oRefLoadedAssembly;
        } _gcRefs;
        
        ZeroMemory(&_gcRefs, sizeof(_gcRefs));
        
        GCPROTECT_BEGIN(_gcRefs);
        
        ICLRPrivAssembly *pAssemblyBindingContext = NULL;

        bool fInvokedForTPABinder = (pTPABinder == NULL)?true:false;
        
        // Prepare to invoke System.Runtime.Loader.AssemblyLoadContext.Resolve method.
        //
        // First, initialize an assembly spec for the requested assembly
        //
        AssemblySpec spec;
        hr = spec.Init(pIAssemblyName);
        if (SUCCEEDED(hr))
        {
            bool fResolvedAssembly = false;
            bool fResolvedAssemblyViaTPALoadContext = false;

            // Allocate an AssemblyName managed object
            _gcRefs.oRefAssemblyName = (ASSEMBLYNAMEREF) AllocateObject(MscorlibBinder::GetClass(CLASS__ASSEMBLY_NAME));
            
            // Initialize the AssemblyName object from the AssemblySpec
            spec.AssemblyNameInit(&_gcRefs.oRefAssemblyName, NULL);
                
            bool isSatelliteAssemblyRequest = !spec.IsNeutralCulture();

            if (!fInvokedForTPABinder)
            {
                // Step 2 (of CLRPrivBinderAssemblyLoadContext::BindUsingAssemblyName) - Invoke Load method
                // This is not invoked for TPA Binder since it always returns NULL.

                // Finally, setup arguments for invocation
                BinderMethodID idHAR_Resolve = METHOD__ASSEMBLYLOADCONTEXT__RESOLVE;
                MethodDescCallSite methLoadAssembly(idHAR_Resolve);
                
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

                // Step 3 (of CLRPrivBinderAssemblyLoadContext::BindUsingAssemblyName)
                if (!fResolvedAssembly && !isSatelliteAssemblyRequest)
                {
                    // If we could not resolve the assembly using Load method, then attempt fallback with TPA Binder.
                    // Since TPA binder cannot fallback to itself, this fallback does not happen for binds within TPA binder.
                    //
                    // Switch to pre-emp mode before calling into the binder
                    GCX_PREEMP();
                    ICLRPrivAssembly *pCoreCLRFoundAssembly = NULL;
                    hr = pTPABinder->BindAssemblyByName(pIAssemblyName, &pCoreCLRFoundAssembly);
                    if (SUCCEEDED(hr))
                    {
                        pAssemblyBindingContext = pCoreCLRFoundAssembly;
                        fResolvedAssembly = true;
                        fResolvedAssemblyViaTPALoadContext = true;
                    }
                }
            }

            if (!fResolvedAssembly && isSatelliteAssemblyRequest)
            {
                // Step 4 (of CLRPrivBinderAssemblyLoadContext::BindUsingAssemblyName)
                //
                // Attempt to resolve it using the ResolveSatelliteAssembly method.
                // Finally, setup arguments for invocation
                BinderMethodID idHAR_ResolveSatelitteAssembly = METHOD__ASSEMBLYLOADCONTEXT__RESOLVESATELLITEASSEMBLY;
                MethodDescCallSite methResolveSatelitteAssembly(idHAR_ResolveSatelitteAssembly);

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
            }

            if (!fResolvedAssembly)
            {
                // Step 5 (of CLRPrivBinderAssemblyLoadContext::BindUsingAssemblyName)
                //
                // If we couldnt resolve the assembly using TPA LoadContext as well, then
                // attempt to resolve it using the Resolving event.
                // Finally, setup arguments for invocation
                BinderMethodID idHAR_ResolveUsingEvent = METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUSINGEVENT;
                MethodDescCallSite methResolveUsingEvent(idHAR_ResolveUsingEvent);
                
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
            }
            
            if (fResolvedAssembly && !fResolvedAssemblyViaTPALoadContext)
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
                    if (pLoadedPEAssembly->HasHostAssembly() != true)
                    {
                        // Reflection emitted assemblies will not have a domain assembly.
                        fFailLoad = true;
                    }
                }
                
                // The loaded assembly's ICLRPrivAssembly* is saved as HostAssembly in PEAssembly
                if (fFailLoad)
                {
                    SString name;
                    spec.GetFileOrDisplayName(0, name);
                    COMPlusThrowHR(COR_E_INVALIDOPERATION, IDS_HOST_ASSEMBLY_RESOLVER_DYNAMICALLY_EMITTED_ASSEMBLIES_UNSUPPORTED, name);
                }
                
                // Is the assembly already bound using a binding context that will be incompatible?
                // An example is attempting to consume an assembly bound to WinRT binder.
                pAssemblyBindingContext = pLoadedPEAssembly->GetHostAssembly();
            }
            
            if (fResolvedAssembly)
            {
#ifdef FEATURE_COMINTEROP
                if (AreSameBinderInstance(pAssemblyBindingContext, GetAppDomain()->GetWinRtBinder()))
                {
                    // It is invalid to return an assembly bound to an incompatible binder
                    *ppLoadedAssembly = NULL;
                    SString name;
                    spec.GetFileOrDisplayName(0, name);
                    COMPlusThrowHR(COR_E_INVALIDOPERATION, IDS_HOST_ASSEMBLY_RESOLVER_INCOMPATIBLE_BINDING_CONTEXT, name);
                }
#endif // FEATURE_COMINTEROP

                // Get the ICLRPrivAssembly reference to return back to.
                *ppLoadedAssembly = clr::SafeAddRef(pAssemblyBindingContext);
                hr = S_OK;
            }
            else
            {
                hr = COR_E_FILENOTFOUND;
            }
        }
        
        GCPROTECT_END();
    }
    // EX_CATCH_HRESULT(hr);
    
    return hr;
    
}
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

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
    else
    {
        // In AppX processes, all PEAssemblies that are reach this stage should have host binders.
        _ASSERTE(!AppX::IsAppXProcess());
    }
}

#if defined(FEATURE_COMINTEROP)
HRESULT AppDomain::SetWinrtApplicationContext(LPCWSTR pwzAppLocalWinMD)
{
    STANDARD_VM_CONTRACT;
    
    _ASSERTE(WinRTSupported());
    _ASSERTE(m_pWinRtBinder != nullptr);

    _ASSERTE(GetTPABinderContext() != NULL);
    BINDER_SPACE::ApplicationContext *pApplicationContext = GetTPABinderContext()->GetAppContext();
    _ASSERTE(pApplicationContext != NULL);
    
    return m_pWinRtBinder->SetApplicationContext(pApplicationContext, pwzAppLocalWinMD);
}

#endif // FEATURE_COMINTEROP

#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------------------------------------
PTR_DomainAssembly AppDomain::FindAssembly(PTR_ICLRPrivAssembly pHostAssembly)
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

#if !defined(DACCESS_COMPILE) && defined(FEATURE_NATIVE_IMAGE_GENERATION)

void ZapperSetBindingPaths(ICorCompilationDomain *pDomain, SString &trustedPlatformAssemblies, SString &platformResourceRoots, SString &appPaths, SString &appNiPaths)
{
    CLRPrivBinderCoreCLR *pBinder = static_cast<CLRPrivBinderCoreCLR*>(((CompilationDomain *)pDomain)->GetFusionContext());
    _ASSERTE(pBinder != NULL);
    pBinder->SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, appNiPaths);
#ifdef FEATURE_COMINTEROP
    ((CompilationDomain*)pDomain)->SetWinrtApplicationContext(NULL);
#endif
}

#endif
