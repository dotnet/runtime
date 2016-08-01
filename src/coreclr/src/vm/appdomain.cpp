// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"

#include "appdomain.hpp"
#include "peimagelayout.inl"
#include "field.h"
#include "security.h"
#include "strongnameinternal.h"
#include "excep.h"
#include "eeconfig.h"
#include "gc.h"
#include "eventtrace.h"
#ifdef FEATURE_FUSION
#include "assemblysink.h"
#include "fusion.h"
#include "fusionbind.h"
#include "fusionlogging.h"
#endif
#include "perfcounters.h"
#include "assemblyname.hpp"
#include "eeprofinterfaces.h"
#include "dbginterface.h"
#ifndef DACCESS_COMPILE
#include "eedbginterfaceimpl.h"
#endif
#include "comdynamic.h"
#include "mlinfo.h"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif
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
#ifdef FEATURE_REMOTING
#include "appdomainhelper.h"
#include "objectclone.h"
#endif
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
#include "sxshelpers.h"
#include "runtimecallablewrapper.h"
#include "mngstdinterfaces.h"
#include "olevariant.h"
#include "rcwrefcache.h"
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_TYPEEQUIVALENCE
#include "typeequivalencehash.hpp"
#endif

#include "listlock.inl"
#include "appdomain.inl"
#include "typeparse.h"
#include "mdaassistants.h"
#include "stackcompressor.h"
#ifdef FEATURE_REMOTING
#include "mscorcfg.h"
#include "appdomainconfigfactory.hpp"
#include "crossdomaincalls.h"
#endif
#include "threadpoolrequest.h"

#include "nativeoverlapped.h"

#include "compatibilityflags.h"

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "stringarraylist.h"

#ifdef FEATURE_VERSIONING
#include "../binder/inc/clrprivbindercoreclr.h"
#endif

#if defined(FEATURE_APPX_BINDER)
#include "appxutil.h"
#include "clrprivbinderappx.h"
#endif

#include "clrprivtypecachewinrt.h"

#ifndef FEATURE_CORECLR
#include "nlsinfo.h"
#endif

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
#pragma warning(push)
#pragma warning(disable:4324) 
#include "marvin32.h"
#pragma warning(pop)
#endif

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

#define MAX_URL_LENGTH                  2084 // same as INTERNET_MAX_URL_LENGTH

//#define _DEBUG_ADUNLOAD 1

HRESULT RunDllMain(MethodDesc *pMD, HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved); // clsload.cpp





// Statics

SPTR_IMPL(SystemDomain, SystemDomain, m_pSystemDomain);
SVAL_IMPL(ArrayListStatic, SystemDomain, m_appDomainIndexList);
SPTR_IMPL(SharedDomain, SharedDomain, m_pSharedDomain);
SVAL_IMPL(BOOL, SystemDomain, s_fForceDebug);
SVAL_IMPL(BOOL, SystemDomain, s_fForceProfiling);
SVAL_IMPL(BOOL, SystemDomain, s_fForceInstrument);

#ifndef DACCESS_COMPILE

// Base Domain Statics
CrstStatic          BaseDomain::m_SpecialStaticsCrst;

int                 BaseDomain::m_iNumberOfProcessors = 0;

// Shared Domain Statics
static BYTE         g_pSharedDomainMemory[sizeof(SharedDomain)];

// System Domain Statics
GlobalStringLiteralMap* SystemDomain::m_pGlobalStringLiteralMap = NULL;

static BYTE         g_pSystemDomainMemory[sizeof(SystemDomain)];

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
size_t              SystemDomain::m_totalSurvivedBytes = 0;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

CrstStatic          SystemDomain::m_SystemDomainCrst;
CrstStatic          SystemDomain::m_DelayedUnloadCrst;

ULONG               SystemDomain::s_dNumAppDomains = 0;

AppDomain *         SystemDomain::m_pAppDomainBeingUnloaded = NULL;
ADIndex             SystemDomain::m_dwIndexOfAppDomainBeingUnloaded;
Thread            *SystemDomain::m_pAppDomainUnloadRequestingThread = 0;
Thread            *SystemDomain::m_pAppDomainUnloadingThread = 0;

ArrayListStatic     SystemDomain::m_appDomainIdList;

DWORD               SystemDomain::m_dwLowestFreeIndex        = 0;



// comparison function to be used for matching clsids in our clsid hash table
BOOL CompareCLSID(UPTR u1, UPTR u2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
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
LargeHeapHandleBucket::LargeHeapHandleBucket(LargeHeapHandleBucket *pNext, DWORD Size, BaseDomain *pDomain, BOOL bCrossAD)
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
    if (!bCrossAD) 
    {
        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
        HandleArrayObj = (PTRARRAYREF)AllocateObjectArray(Size, g_pObjectClass, TRUE);
    }
    else 
    {
        // During AD creation we don't want to assign the handle array to the currently running AD but
        // to the AD being created.  Ensure that AllocateArrayEx doesn't set the AD and then set it here.
        AppDomain *pAD = pDomain->AsAppDomain();
        _ASSERTE(pAD);
        _ASSERTE(pAD->IsBeingCreated());

        OBJECTREF array;
        {
            OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
            array = AllocateArrayEx(
                ClassLoader::LoadArrayTypeThrowing(g_pObjectClass),
                (INT32 *)(&Size),
                1,
                TRUE
                DEBUG_ARG(TRUE));
        }

        array->SetAppDomain(pAD);

        HandleArrayObj = (PTRARRAYREF)array;
    }

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
// taken in the AppDomainStringLiteralMap functions below protects the 4 ways that this can happen
//
// case 3a)
//
// in an appdomain unload case
//
// AppDomainStringLiteralMap::~AppDomainStringLiteralMap() takes the lock then
// leads to calls to
//     StringLiteralEntry::Release
//    which leads to
//        SystemDomain::GetGlobalStringLiteralMapNoCreate()->RemoveStringLiteralEntry(this)
//        which leads to
//            m_LargeHeapHandleTable.ReleaseHandles((OBJECTREF*)pObjRef, 1);
//
// case 3b)
//
// AppDomainStringLiteralMap::GetStringLiteral() can call StringLiteralEntry::Release in some
// error cases, leading to the same stack as above
//
// case 3c)
//
// AppDomainStringLiteralMap::GetInternedString() can call StringLiteralEntry::Release in some
// error cases, leading to the same stack as above
//
// case 3d)
//
// The same code paths in 3b and 3c and also end up releasing if an exception is thrown
// during their processing.  Both these paths use a StringLiteralEntryHolder to assist in cleanup,
// the StaticRelease method of the StringLiteralEntry gets called, which in turn calls the
// Release method.


// Allocate handles from the large heap handle table.
OBJECTREF* LargeHeapHandleTable::AllocateHandles(DWORD nRequested, BOOL bCrossAD)
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

        m_pHead = new LargeHeapHandleBucket(m_pHead, NewBucketSize, m_pDomain, bCrossAD);

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
        SetObjectReference(&pObjRef[i], pPreallocatedSentinalObject, NULL);
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
#ifdef  FEATURE_RANDOMIZED_STRING_HASHING
#ifdef FEATURE_CORECLR
    // Randomized string hashing is on by default for String.GetHashCode in coreclr.
    COMNlsHashProvider::s_NlsHashProvider.SetUseRandomHashing((CorHost2::GetStartupFlags() & STARTUP_DISABLE_RANDOMIZED_STRING_HASHING) == 0);
#endif // FEATURE_CORECLR
#endif // FEATURE_RANDOMIZED_STRING_HASHING
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
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)  
    m_pTPABinderContext = NULL;
#endif

    // Make sure the container is set to NULL so that it gets loaded when it is used.
    m_pLargeHeapHandleTable = NULL;

#ifndef CROSSGEN_COMPILE
    // Note that m_hHandleTableBucket is overridden by app domains
    m_hHandleTableBucket = g_HandleTableMap.pBuckets[0];
#else
    m_hHandleTableBucket = NULL;
#endif

    m_pMarshalingData = NULL;

    m_dwContextStatics = 0;
#ifdef FEATURE_COMINTEROP
    m_pMngStdInterfacesInfo = NULL;
    m_pWinRtBinder = NULL;
#endif
    m_FileLoadLock.PreInit();
    m_JITLock.PreInit();
    m_ClassInitLock.PreInit();
    m_ILStubGenLock.PreInit();

#ifdef FEATURE_REJIT
    m_reJitMgr.PreInit(this == (BaseDomain *) g_pSharedDomainMemory);
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

    if (this == reinterpret_cast<BaseDomain*>(&g_pSharedDomainMemory[0]))
        m_DomainCrst.Init(CrstSharedBaseDomain);
    else if (this == reinterpret_cast<BaseDomain*>(&g_pSystemDomainMemory[0]))
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

    // Initialize the EE marshaling data to NULL.
    m_pMarshalingData = NULL;

#ifdef FEATURE_COMINTEROP
    // Allocate the managed standard interfaces information.
    m_pMngStdInterfacesInfo = new MngStdInterfacesInfo();
    
#if defined(FEATURE_APPX_BINDER)
    if (!AppX::IsAppXProcess())
#endif
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

#ifndef CROSSGEN_COMPILE
//*****************************************************************************
void BaseDomain::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_crstLoaderAllocatorReferences.Destroy();
    m_DomainCrst.Destroy();
    m_DomainCacheCrst.Destroy();
    m_DomainLocalBlockCrst.Destroy();
    m_InteropDataCrst.Destroy();

    ListLockEntry* pElement;

    // All the threads that are in this domain had better be stopped by this
    // point.
    //
    // We might be jitting or running a .cctor so we need to empty that queue.
    pElement = m_JITLock.Pop(TRUE);
    while (pElement)
    {
#ifdef STRICT_JITLOCK_ENTRY_LEAK_DETECTION
        _ASSERTE ((m_JITLock.m_pHead->m_dwRefCount == 1
            && m_JITLock.m_pHead->m_hrResultCode == E_FAIL) ||
            dbg_fDrasticShutdown || g_fInControlC);
#endif // STRICT_JITLOCK_ENTRY_LEAK_DETECTION
        delete(pElement);
        pElement = m_JITLock.Pop(TRUE);

    }
    m_JITLock.Destroy();

    pElement = m_ClassInitLock.Pop(TRUE);
    while (pElement)
    {
#ifdef STRICT_CLSINITLOCK_ENTRY_LEAK_DETECTION
        _ASSERTE (dbg_fDrasticShutdown || g_fInControlC);
#endif
        delete(pElement);
        pElement = m_ClassInitLock.Pop(TRUE);
    }
    m_ClassInitLock.Destroy();

    FileLoadLock* pFileElement;
    pFileElement = (FileLoadLock*) m_FileLoadLock.Pop(TRUE);
    while (pFileElement)
    {
#ifdef STRICT_CLSINITLOCK_ENTRY_LEAK_DETECTION
        _ASSERTE (dbg_fDrasticShutdown || g_fInControlC);
#endif
        pFileElement->Release();
        pFileElement = (FileLoadLock*) m_FileLoadLock.Pop(TRUE);
    }
    m_FileLoadLock.Destroy();

    pElement = m_ILStubGenLock.Pop(TRUE);
    while (pElement)
    {
#ifdef STRICT_JITLOCK_ENTRY_LEAK_DETECTION
        _ASSERTE ((m_ILStubGenLock.m_pHead->m_dwRefCount == 1
            && m_ILStubGenLock.m_pHead->m_hrResultCode == E_FAIL) ||
            dbg_fDrasticShutdown || g_fInControlC);
#endif // STRICT_JITLOCK_ENTRY_LEAK_DETECTION
        delete(pElement);
        pElement = m_ILStubGenLock.Pop(TRUE);
    }
    m_ILStubGenLock.Destroy();

    m_LargeHeapHandleTableCrst.Destroy();

    if (m_pLargeHeapHandleTable != NULL)
    {
        delete m_pLargeHeapHandleTable;
        m_pLargeHeapHandleTable = NULL;
    }

    if (!IsAppDomain())
    {
        // Kind of a workaround - during unloading, we need to have an EE halt
        // around deleting this stuff. So it gets deleted in AppDomain::Terminate()
        // for those things (because there is a convenient place there.)
        GetLoaderAllocator()->CleanupStringLiteralMap();
    }

#ifdef FEATURE_COMINTEROP
    if (m_pMngStdInterfacesInfo)
    {
        delete m_pMngStdInterfacesInfo;
        m_pMngStdInterfacesInfo = NULL;
    }
    
    if (m_pWinRtBinder != NULL)
    {
        m_pWinRtBinder->Release();
    }
#endif // FEATURE_COMINTEROP

    ClearFusionContext();

    m_dwSizedRefHandles = 0;
}
#endif // CROSSGEN_COMPILE

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
BOOL BaseDomain::ContainsOBJECTHANDLE(OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return Ref_ContainHandle(m_hHandleTableBucket,handle);
}

DWORD BaseDomain::AllocateContextStaticsOffset(DWORD* pOffsetSlot)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_SpecialStaticsCrst);

    DWORD dwOffset = *pOffsetSlot;

    if (dwOffset == (DWORD)-1)
    {
        // Allocate the slot
        dwOffset = m_dwContextStatics++;
        *pOffsetSlot = dwOffset;
    }

    return dwOffset;
}

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
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)  
    if (m_pTPABinderContext) {
        m_pTPABinderContext->Release();
        m_pTPABinderContext = NULL;
    }
#endif
}

#ifdef  FEATURE_PREJIT
void AppDomain::DeleteNativeCodeRanges()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Fast path to skip using the assembly iterator when the appdomain has not yet completely been initialized 
    // and yet we are destroying it.  (This is the case if we OOM during AppDomain creation.)
    if (m_Assemblies.IsEmpty())
        return;

    // Shutdown assemblies
    AssemblyIterator i = IterateAssembliesEx( (AssemblyIterationFlags)(kIncludeLoaded | kIncludeLoading | kIncludeExecution | kIncludeIntrospection | kIncludeFailedToLoad) );
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (i.Next(pDomainAssembly.This()))
    {
        Assembly * assembly = pDomainAssembly->m_pAssembly;
        if ((assembly != NULL) && !assembly->IsDomainNeutral())
            assembly->DeleteNativeCodeRanges();
    }
}
#endif

void AppDomain::ShutdownAssemblies()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Fast path to skip using the assembly iterator when the appdomain has not yet completely been initialized 
    // and yet we are destroying it.  (This is the case if we OOM during AppDomain creation.)
    if (m_Assemblies.IsEmpty())
        return;

    // Shutdown assemblies
    // has two stages because Terminate needs info from the Assembly's dependencies

    // Stage 1: call code:Assembly::Terminate
    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded | kIncludeLoading | kIncludeExecution | kIncludeIntrospection | kIncludeFailedToLoad | kIncludeCollected));
    DomainAssembly * pDomainAssembly = NULL;

    while (i.Next_UnsafeNoAddRef(&pDomainAssembly))
    {
        // Note: cannot use DomainAssembly::GetAssembly() here as it asserts that the assembly has been
        // loaded to at least the FILE_LOAD_ALLOCATE level. Since domain shutdown can take place
        // asynchronously this property cannot be guaranteed. Access the m_pAssembly field directly instead.
        Assembly * assembly = pDomainAssembly->m_pAssembly;
        if (assembly && !assembly->IsDomainNeutral())
            assembly->Terminate();
    }
    
    // Stage 2: Clear the list of assemblies
    i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded | kIncludeLoading | kIncludeExecution | kIncludeIntrospection | kIncludeFailedToLoad | kIncludeCollected));
    while (i.Next_UnsafeNoAddRef(&pDomainAssembly))
    {
        // We are in shutdown path, no one else can get to the list anymore
        delete pDomainAssembly;
    }
    m_Assemblies.Clear(this);
    
    // Stage 2: Clear the loader allocators registered for deletion from code:Assembly:Terminate calls in 
    // stage 1
    // Note: It is not clear to me why we cannot delete the loader allocator from within 
    // code:DomainAssembly::~DomainAssembly
    ShutdownFreeLoaderAllocators(FALSE);
} // AppDomain::ShutdownAssemblies

void AppDomain::ShutdownFreeLoaderAllocators(BOOL bFromManagedCode)
{
    // If we're called from managed code (i.e. the finalizer thread) we take a lock in
    // LoaderAllocator::CleanupFailedTypeInit, which may throw. Otherwise we're called
    // from the app-domain shutdown path in which we can avoid taking the lock.
    CONTRACTL
    {
        GC_TRIGGERS;
        if (bFromManagedCode) THROWS; else NOTHROW;
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

        if (bFromManagedCode)
        {
            // For loader allocator finalization, we need to be careful about cleaning up per-appdomain allocations
            // and synchronizing with GC using delay unload list. We need to wait for next Gen2 GC to finish to ensure
            // that GC heap does not have any references to the MethodTables being unloaded.

            pCurrentLoaderAllocator->CleanupFailedTypeInit();

            pCurrentLoaderAllocator->CleanupHandles();

            GCX_COOP();
            SystemDomain::System()->AddToDelayedUnloadList(pCurrentLoaderAllocator);
        }
        else
        {
            // For appdomain unload, delete the loader allocator right away
            delete pCurrentLoaderAllocator;
        }
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

#ifdef FEATURE_CORECLR
void AppDomain::ShutdownNativeDllSearchDirectories()
{
    LIMITED_METHOD_CONTRACT;
    // Shutdown assemblies
    PathIterator i = IterateNativeDllSearchDirectories();

    while (i.Next())
    {
        delete i.GetPath();
    }

    m_NativeDllSearchDirectories.Clear();
}
#endif

void AppDomain::ReleaseDomainBoundInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;;
    // Shutdown assemblies
    m_AssemblyCache.OnAppDomainUnload();

    AssemblyIterator i = IterateAssembliesEx( (AssemblyIterationFlags)(kIncludeFailedToLoad) );
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    
    while (i.Next(pDomainAssembly.This()))
    {
       pDomainAssembly->ReleaseManagedData();
    }
}

void AppDomain::ReleaseFiles()
{
    STANDARD_VM_CONTRACT;

    // Shutdown assemblies
    AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded  | kIncludeExecution | kIncludeIntrospection | kIncludeFailedToLoad | kIncludeLoading));
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
            if (!pAsm->GetCurrentAssembly()->IsDomainNeutral())
                pAsm->ReleaseFiles();
        }
    }
} // AppDomain::ReleaseFiles


OBJECTREF* BaseDomain::AllocateObjRefPtrsInLargeTable(int nRequested, OBJECTREF** ppLazyAllocate, BOOL bCrossAD)
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
        OBJECTREF* result = m_pLargeHeapHandleTable->AllocateHandles(nRequested, bCrossAD);

        if (ppLazyAllocate)
        {
            *ppLazyAllocate = result;
        }

        return result;
    }
}
#endif // CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE

/*static*/
PTR_BaseDomain BaseDomain::ComputeBaseDomain(
    BaseDomain * pGenericDefinitionDomain,   // the domain that owns the generic type or method
    Instantiation classInst,                // the type arguments to the type (if any)
    Instantiation methodInst)               // the type arguments to the method (if any)
{
    CONTRACT(PTR_BaseDomain)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_END

    if (pGenericDefinitionDomain && pGenericDefinitionDomain->IsAppDomain())
        RETURN PTR_BaseDomain(pGenericDefinitionDomain);

    for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
    {
        PTR_BaseDomain pArgDomain = classInst[i].GetDomain();
        if (pArgDomain->IsAppDomain())
            RETURN pArgDomain;
    }

    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        PTR_BaseDomain pArgDomain = methodInst[i].GetDomain();
        if (pArgDomain->IsAppDomain())
            RETURN pArgDomain;
    }
    RETURN (pGenericDefinitionDomain ? 
            PTR_BaseDomain(pGenericDefinitionDomain) : 
            PTR_BaseDomain(SystemDomain::System()));
}

PTR_BaseDomain BaseDomain::ComputeBaseDomain(TypeKey * pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    if (pKey->GetKind() == ELEMENT_TYPE_CLASS)
        return BaseDomain::ComputeBaseDomain(pKey->GetModule()->GetDomain(),
                                             pKey->GetInstantiation());
    else if (pKey->GetKind() != ELEMENT_TYPE_FNPTR)
        return pKey->GetElementType().GetDomain();
    else
        return BaseDomain::ComputeBaseDomain(NULL,Instantiation(pKey->GetRetAndArgTypes(), pKey->GetNumArgs()+1));
}





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

        HndAssignHandle(pEntry->m_ohFactoryObject, *refFactory);
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

EEMarshalingData *BaseDomain::GetMarshalingData()
{
    CONTRACT (EEMarshalingData*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(m_pMarshalingData));
    }
    CONTRACT_END;

    if (!m_pMarshalingData)
    {
        // Take the lock
        CrstHolder holder(&m_InteropDataCrst);

        if (!m_pMarshalingData)
        {
            LoaderHeap* pHeap = GetLoaderAllocator()->GetLowFrequencyHeap();
            m_pMarshalingData = new (pHeap) EEMarshalingData(this, pHeap, &m_DomainCrst);
        }
    }

    RETURN m_pMarshalingData;
}

void BaseDomain::DeleteMarshalingData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We are in shutdown - no need to take any lock
    if (m_pMarshalingData)
    {
        delete m_pMarshalingData;
        m_pMarshalingData = NULL;
    }
}

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

#ifdef FEATURE_COMINTEROP
MethodTable* AppDomain::GetLicenseInteropHelperMethodTable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if(m_pLicenseInteropHelperMT == NULL)
    {
        // Do this work outside of the lock so we don't have an unbreakable lock condition

        TypeHandle licenseMgrTypeHnd;
        MethodDescCallSite  loadLM(METHOD__MARSHAL__LOAD_LICENSE_MANAGER);

        licenseMgrTypeHnd = (MethodTable*) loadLM.Call_RetLPVOID((ARG_SLOT*)NULL);

        //
        // Look up this method by name, because the type is actually declared in System.dll.  <TODO>@todo: why?</TODO>
        //

        MethodDesc *pGetLIHMD = MemberLoader::FindMethod(licenseMgrTypeHnd.AsMethodTable(),
                "GetLicenseInteropHelperType", &gsig_SM_Void_RetIntPtr);
        _ASSERTE(pGetLIHMD);

        TypeHandle lihTypeHnd;

        MethodDescCallSite getLIH(pGetLIHMD);
        lihTypeHnd = (MethodTable*) getLIH.Call_RetLPVOID((ARG_SLOT*)NULL);

        BaseDomain::LockHolder lh(this);

        if(m_pLicenseInteropHelperMT == NULL)
            m_pLicenseInteropHelperMT = lihTypeHnd.AsMethodTable();
    }
    return m_pLicenseInteropHelperMT;
}

COMorRemotingFlag AppDomain::GetComOrRemotingFlag()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // 0. check if the value is already been set
    if (m_COMorRemotingFlag != COMorRemoting_NotInitialized)
        return m_COMorRemotingFlag;

    // 1. check whether the process is AppX
    if (AppX::IsAppXProcess())
    {
        // do not use Remoting in AppX
        m_COMorRemotingFlag = COMorRemoting_COM;
        return m_COMorRemotingFlag;
    }

    // 2. check the xml file
    m_COMorRemotingFlag = GetPreferComInsteadOfManagedRemotingFromConfigFile();
    if (m_COMorRemotingFlag != COMorRemoting_NotInitialized)
    {
        return m_COMorRemotingFlag;
    }

    // 3. check the global setting
    if (NULL != g_pConfig && g_pConfig->ComInsteadOfManagedRemoting())
    {
        m_COMorRemotingFlag = COMorRemoting_COM;
    }
    else
    {
        m_COMorRemotingFlag = COMorRemoting_Remoting;
    }

    return m_COMorRemotingFlag;
}

BOOL AppDomain::GetPreferComInsteadOfManagedRemoting()
{
    WRAPPER_NO_CONTRACT;

    return (GetComOrRemotingFlag() == COMorRemoting_COM);
}

STDAPI GetXMLObjectEx(IXMLParser **ppv);

COMorRemotingFlag AppDomain::GetPreferComInsteadOfManagedRemotingFromConfigFile()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_REMOTING
    COMorRemotingFlag res = COMorRemoting_NotInitialized;
    NonVMComHolder<IXMLParser>         pIXMLParser(NULL);
    NonVMComHolder<IStream>            pFile(NULL);
    NonVMComHolder<AppDomainConfigFactory>    factory(NULL); 

    EX_TRY
    {
        HRESULT hr;
        CQuickBytes qb;
    
        // get config file URL which is a combination of app base and config file name
        IfFailGo(m_pFusionContext->PrefetchAppConfigFile());

        LPWSTR wzConfigFileUrl = (LPWSTR)qb.AllocThrows(MAX_URL_LENGTH * sizeof(WCHAR));
        DWORD dwSize = static_cast<DWORD>(qb.Size());

        IfFailGo(m_pFusionContext->Get(ACTAG_APP_CFG_LOCAL_FILEPATH, wzConfigFileUrl, &dwSize, 0));

        IfFailGo(CreateConfigStream(wzConfigFileUrl, &pFile));
        
        IfFailGo(GetXMLObjectEx(&pIXMLParser));

        factory = new (nothrow) AppDomainConfigFactory();
        
        if (!factory) { 
            goto ErrExit;
        }
        factory->AddRef(); // RefCount = 1 


        IfFailGo(pIXMLParser->SetInput(pFile)); // filestream's RefCount=2

        IfFailGo(pIXMLParser->SetFactory(factory)); // factory's RefCount=2

        IfFailGo(pIXMLParser->Run(-1));

        res = factory->GetCOMorRemotingFlag();
ErrExit: ;

    }
    EX_CATCH
    {
        ;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return res;
#else // FEATURE_REMOTING
    return COMorRemoting_COM;
#endif // FEATURE_REMOTING
}
#endif // FEATURE_COMINTEROP

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


void    SystemDomain::SetCompilationOverrides(BOOL fForceDebug,
                                              BOOL fForceProfiling,
                                              BOOL fForceInstrument)
{
    LIMITED_METHOD_CONTRACT;
    s_fForceDebug = fForceDebug;
    s_fForceProfiling = fForceProfiling;
    s_fForceInstrument = fForceInstrument;
}

#endif //!DACCESS_COMPILE

void    SystemDomain::GetCompilationOverrides(BOOL * fForceDebug,
                                              BOOL * fForceProfiling,
                                              BOOL * fForceInstrument)
{
    LIMITED_METHOD_DAC_CONTRACT;
    *fForceDebug = s_fForceDebug;
    *fForceProfiling = s_fForceProfiling;
    *fForceInstrument = s_fForceInstrument;
}

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

    m_appDomainIndexList.Init();
    m_appDomainIdList.Init();

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

    // Create the default domain
    m_pSystemDomain->CreateDefaultDomain();
    SharedDomain::Attach();

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
        if (m_pSystemDomain->m_pDefaultDomain)
            m_pSystemDomain->m_pDefaultDomain->ClearFusionContext();
    }
}

void SystemDomain::Stop()
{
    WRAPPER_NO_CONTRACT;
    AppDomainIterator i(TRUE);

    while (i.Next())
        if (i.GetDomain()->m_Stage < AppDomain::STAGE_CLEARED)
            i.GetDomain()->Stop();
}


void SystemDomain::Terminate() // bNotifyProfiler is ignored
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This ignores the refences and terminates the appdomains
    AppDomainIterator i(FALSE);

    while (i.Next())
    {
        delete i.GetDomain();
        // Keep the iterator from Releasing the current domain
        i.m_pCurrent = NULL;
    }

    if (m_pSystemFile != NULL) {
        m_pSystemFile->Release();
        m_pSystemFile = NULL;
    }

    m_pSystemAssembly = NULL;

    if(m_pwDevpath) {
        delete[] m_pwDevpath;
        m_pwDevpath = NULL;
    }
    m_dwDevpath = 0;
    m_fDevpath = FALSE;

    if (m_pGlobalStringLiteralMap) {
        delete m_pGlobalStringLiteralMap;
        m_pGlobalStringLiteralMap = NULL;
    }


    SharedDomain::Detach();

    BaseDomain::Terminate();

#ifdef FEATURE_COMINTEROP
    if (g_pRCWCleanupList != NULL)
        delete g_pRCWCleanupList;
#endif // FEATURE_COMINTEROP
    m_GlobalAllocator.Terminate();
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
#if CHECK_APP_DOMAIN_LEAKS
    pPreallocatedSentinalObject->SetSyncBlockAppDomainAgile();
#endif
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
#if CHECK_APP_DOMAIN_LEAKS
    pRudeAbortException->SetSyncBlockAppDomainAgile();
#endif
    pRudeAbortException->SetHResult(COR_E_THREADABORTED);
    pRudeAbortException->SetXCode(EXCEPTION_COMPLUS);
    _ASSERTE(g_pPreallocatedRudeThreadAbortException == NULL);
    g_pPreallocatedRudeThreadAbortException = CreateHandle(pRudeAbortException);


    EXCEPTIONREF pAbortException = (EXCEPTIONREF)AllocateObject(g_pThreadAbortExceptionClass);
#if CHECK_APP_DOMAIN_LEAKS
    pAbortException->SetSyncBlockAppDomainAgile();
#endif
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

#ifndef CROSSGEN_COMPILE
#ifdef _DEBUG
    Context     *curCtx = GetCurrentContext();
#endif
    _ASSERTE(curCtx);
    _ASSERTE(curCtx->GetDomain() != NULL);
#endif

#ifdef _DEBUG
    g_fVerifierOff = g_pConfig->IsVerifierOff();
#endif

#ifdef FEATURE_PREJIT
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapDisable) != 0)
        g_fAllowNativeImages = false;
#endif

    m_pSystemFile = NULL;
    m_pSystemAssembly = NULL;

    DWORD size = 0;

#ifdef FEATURE_VERSIONING

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

#else

    m_SystemDirectory = GetInternalSystemDirectory(&size);

#endif // FEATURE_VERSIONING

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
#ifdef FEATURE_FUSION
        // disable fusion log for m_pSystemFile, because m_pSystemFile will get reused
        m_pSystemFile->DisableFusionLogging();
#endif
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

void AppDomain::CreateADUnloadStartEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    g_pUnloadStartEvent = new CLREvent();
    g_pUnloadStartEvent->CreateAutoEvent(FALSE);
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

    _ASSERTE(GCHeap::IsGCInProgress() &&
             GCHeap::IsServerHeap()   &&
             IsGCSpecialThread());

    SystemDomain* sysDomain = SystemDomain::System();
    if (sysDomain)
    {
        DWORD i;
        DWORD count = (DWORD) m_appDomainIdList.GetCount();
        for (i = 0 ; i < count ; i++)
        {
            AppDomain* pAppDomain = (AppDomain *)m_appDomainIdList.Get(i);
            if (pAppDomain && pAppDomain->IsActive() && !pAppDomain->IsUnloading())
            {
#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
                if (g_fEnableARM)
                {
                    sc->pCurrentDomain = pAppDomain;
                }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING
                pAppDomain->EnumStaticGCRefs(fn, sc);
            }
        }
    }

    RETURN;
}

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
void SystemDomain::ResetADSurvivedBytes()
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    _ASSERTE(GCHeap::IsGCInProgress());

    SystemDomain* sysDomain = SystemDomain::System();
    if (sysDomain)
    {
        DWORD i;
        DWORD count = (DWORD) m_appDomainIdList.GetCount();
        for (i = 0 ; i < count ; i++)
        {
            AppDomain* pAppDomain = (AppDomain *)m_appDomainIdList.Get(i);
            if (pAppDomain && pAppDomain->IsUserActive())
            {
                pAppDomain->ResetSurvivedBytes();
            }
        }
    }

    RETURN;
}

ULONGLONG SystemDomain::GetADSurvivedBytes()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SystemDomain* sysDomain = SystemDomain::System();
    ULONGLONG ullTotalADSurvived = 0;
    if (sysDomain)
    {
        DWORD i;
        DWORD count = (DWORD) m_appDomainIdList.GetCount();
        for (i = 0 ; i < count ; i++)
        {
            AppDomain* pAppDomain = (AppDomain *)m_appDomainIdList.Get(i);
            if (pAppDomain && pAppDomain->IsUserActive())
            {
                ULONGLONG ullSurvived = pAppDomain->GetSurvivedBytes();
                ullTotalADSurvived += ullSurvived;
            }
        }
    }

    return ullTotalADSurvived;
}

void SystemDomain::RecordTotalSurvivedBytes(size_t totalSurvivedBytes)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    m_totalSurvivedBytes = totalSurvivedBytes;

    SystemDomain* sysDomain = SystemDomain::System();
    if (sysDomain)
    {
        DWORD i;
        DWORD count = (DWORD) m_appDomainIdList.GetCount();
        for (i = 0 ; i < count ; i++)
        {
            AppDomain* pAppDomain = (AppDomain *)m_appDomainIdList.Get(i);
            if (pAppDomain && pAppDomain->IsUserActive())
            {
                FireEtwAppDomainMemSurvived((ULONGLONG)pAppDomain, pAppDomain->GetSurvivedBytes(), totalSurvivedBytes, GetClrInstanceId());
            }
        }
    }

    RETURN;
}
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

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
        DWORD i;
        DWORD count = (DWORD) m_appDomainIdList.GetCount();
        for (i = 0 ; i < count ; i++)
        {
            AppDomain* pAppDomain = (AppDomain *)m_appDomainIdList.Get(i);
            if (pAppDomain && pAppDomain->IsActive() && !pAppDomain->IsUnloading())
            {
                dwTotalNumSizedRefHandles += pAppDomain->GetNumSizedRefHandles();
            }
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
#ifdef FEATURE_FUSION        
        ETWOnStartup (FusionAppCtx_V1, FusionAppCtxEnd_V1);
        // Setup fusion context for the system domain - this is used for binding mscorlib.
        IfFailThrow(FusionBind::SetupFusionContext(m_SystemDirectory, NULL, &m_pFusionContext));

        m_pSystemFile = PEAssembly::OpenSystem(m_pFusionContext);
#else
        m_pSystemFile = PEAssembly::OpenSystem(NULL);
#endif // FEATURE_FUSION
    }
    // Only partially load the system assembly. Other parts of the code will want to access
    // the globals in this function before finishing the load.
    m_pSystemAssembly = DefaultDomain()->LoadDomainAssembly(NULL, m_pSystemFile, FILE_LOAD_POST_LOADLIBRARY, NULL)->GetCurrentAssembly();

    // Set up binder for mscorlib
    MscorlibBinder::AttachModule(m_pSystemAssembly->GetManifestModule());

    // Load Object
    g_pObjectClass = MscorlibBinder::GetClass(CLASS__OBJECT);

    // get the Object::.ctor method desc so we can special-case it
    g_pObjectCtorMD = MscorlibBinder::GetMethod(METHOD__OBJECT__CTOR);

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
    // We need to load this after ValueType and Enum because RuntimeType now
    // contains an enum field (m_invocationFlags). Otherwise INVOCATION_FLAGS
    // would be treated as a reference type and clr!SigPointer::GetTypeHandleThrowing
    // throws an exception.
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
    _ASSERTE(g_pStringClass->GetBaseSize() == ObjSizeOf(StringObject)+sizeof(WCHAR));
    _ASSERTE(g_pStringClass->GetComponentSize() == 2);

    // Used by Buffer::BlockCopy
    g_pByteArrayMT = ClassLoader::LoadArrayTypeThrowing(
        TypeHandle(MscorlibBinder::GetElementType(ELEMENT_TYPE_U1))).AsArray()->GetMethodTable();

#ifndef CROSSGEN_COMPILE
    ECall::PopulateManagedStringConstructors();

    if (CLRIoCompletionHosted())
    {
        g_pOverlappedDataClass = MscorlibBinder::GetClass(CLASS__OVERLAPPEDDATA);
        _ASSERTE (g_pOverlappedDataClass);
        if (CorHost2::GetHostOverlappedExtensionSize() != 0)
        {
            // Overlapped may have an extension if a host hosts IO completion subsystem
            DWORD instanceFieldBytes = g_pOverlappedDataClass->GetNumInstanceFieldBytes() + CorHost2::GetHostOverlappedExtensionSize();
            _ASSERTE (instanceFieldBytes + ObjSizeOf(Object) >= MIN_OBJECT_SIZE);
            DWORD baseSize = (DWORD) (instanceFieldBytes + ObjSizeOf(Object));
            baseSize = (baseSize + ALLOC_ALIGN_CONSTANT) & ~ALLOC_ALIGN_CONSTANT;  // m_BaseSize must be aligned
            DWORD adjustSize = baseSize - g_pOverlappedDataClass->GetBaseSize();
            CGCDesc* map = CGCDesc::GetCGCDescFromMT(g_pOverlappedDataClass);
            CGCDescSeries * cur = map->GetHighestSeries();
            _ASSERTE ((SSIZE_T)map->GetNumSeries() == 1);
            cur->SetSeriesSize(cur->GetSeriesSize() - adjustSize);
            g_pOverlappedDataClass->SetBaseSize(baseSize);
        }
    }
#endif // CROSSGEN_COMPILE

    g_pExceptionClass = MscorlibBinder::GetClass(CLASS__EXCEPTION);
    g_pOutOfMemoryExceptionClass = MscorlibBinder::GetException(kOutOfMemoryException);
    g_pStackOverflowExceptionClass = MscorlibBinder::GetException(kStackOverflowException);
    g_pExecutionEngineExceptionClass = MscorlibBinder::GetException(kExecutionEngineException);
    g_pThreadAbortExceptionClass = MscorlibBinder::GetException(kThreadAbortException);

    // Used for determining whether a class has a critical finalizer
    // To determine whether a class has a critical finalizer, we
    // currently will simply see if it's parent class has a critical
    // finalizer. To introduce a class with a critical finalizer,
    // we'll explicitly load CriticalFinalizerObject and set the bit
    // here.
    g_pCriticalFinalizerObjectClass = MscorlibBinder::GetClass(CLASS__CRITICAL_FINALIZER_OBJECT);
    _ASSERTE(g_pCriticalFinalizerObjectClass->HasCriticalFinalizer());

    // used by gc to handle predefined agility checking
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
    g_pPrepareConstrainedRegionsMethod = MscorlibBinder::GetMethod(METHOD__RUNTIME_HELPERS__PREPARE_CONSTRAINED_REGIONS);
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

    pDomain->SetCanUnload();    // by default can unload any domain
    SystemDomain::System()->AddDomain(pDomain);
}

ADIndex SystemDomain::GetNewAppDomainIndex(AppDomain *pAppDomain)
{
    STANDARD_VM_CONTRACT;

    DWORD count = m_appDomainIndexList.GetCount();
    DWORD i;

#ifdef _DEBUG
    if (count < 2000)
    {
        // So that we can keep AD index inside object header.
        // We do not want to create syncblock unless needed.
        i = count;
    }
    else
    {
#endif // _DEBUG
        //
        // Look for an unused index.  Note that in a checked build,
        // we never reuse indexes - this makes it easier to tell
        // when we are looking at a stale app domain.
        //

        i = m_appDomainIndexList.FindElement(m_dwLowestFreeIndex, NULL);
        if (i == (DWORD) ArrayList::NOT_FOUND)
            i = count;
        m_dwLowestFreeIndex = i+1;
#ifdef _DEBUG
        if (m_dwLowestFreeIndex >= 2000)
        {
            m_dwLowestFreeIndex = 0;
        }
    }
#endif // _DEBUG

    if (i == count)
        IfFailThrow(m_appDomainIndexList.Append(pAppDomain));
    else
        m_appDomainIndexList.Set(i, pAppDomain);

    _ASSERTE(i < m_appDomainIndexList.GetCount());

    // Note that index 0 means domain agile.
    return ADIndex(i+1);
}

void SystemDomain::ReleaseAppDomainIndex(ADIndex index)
{
    WRAPPER_NO_CONTRACT;
    SystemDomain::LockHolder lh;
    // Note that index 0 means domain agile.
    index.m_dwIndex--;

    _ASSERTE(m_appDomainIndexList.Get(index.m_dwIndex) != NULL);

    m_appDomainIndexList.Set(index.m_dwIndex, NULL);

#ifndef _DEBUG
    if (index.m_dwIndex < m_dwLowestFreeIndex)
        m_dwLowestFreeIndex = index.m_dwIndex;
#endif // !_DEBUG
}

#endif // !DACCESS_COMPILE

PTR_AppDomain SystemDomain::GetAppDomainAtIndex(ADIndex index)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(index.m_dwIndex != 0);

    PTR_AppDomain pAppDomain = TestGetAppDomainAtIndex(index);

    _ASSERTE(pAppDomain || !"Attempt to access unloaded app domain");

    return pAppDomain;
}

PTR_AppDomain SystemDomain::TestGetAppDomainAtIndex(ADIndex index)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(index.m_dwIndex != 0);
    index.m_dwIndex--;

#ifndef DACCESS_COMPILE
    _ASSERTE(index.m_dwIndex < (DWORD)m_appDomainIndexList.GetCount());
    AppDomain *pAppDomain = (AppDomain*) m_appDomainIndexList.Get(index.m_dwIndex);
#else // DACCESS_COMPILE
    PTR_ArrayListStatic pList = &m_appDomainIndexList;
    AppDomain *pAppDomain = dac_cast<PTR_AppDomain>(pList->Get(index.m_dwIndex));
#endif // DACCESS_COMPILE
    return PTR_AppDomain(pAppDomain);
}

#ifndef DACCESS_COMPILE

// See also code:SystemDomain::ReleaseAppDomainId
ADID SystemDomain::GetNewAppDomainId(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DWORD i = m_appDomainIdList.GetCount();

    IfFailThrow(m_appDomainIdList.Append(pAppDomain));

    _ASSERTE(i < m_appDomainIdList.GetCount());

    return ADID(i+1);
}

AppDomain *SystemDomain::GetAppDomainAtId(ADID index)
{
    CONTRACTL
    {
#ifdef _DEBUG
        if (!SystemDomain::IsUnderDomainLock() && !IsGCThread()) { MODE_COOPERATIVE;} else { DISABLED(MODE_ANY);}
#endif
        GC_NOTRIGGER;
        SO_TOLERANT;
        NOTHROW;
    }
    CONTRACTL_END;

    if(index.m_dwId == 0)
        return NULL;
    DWORD requestedID = index.m_dwId - 1;

    if(requestedID  >= (DWORD)m_appDomainIdList.GetCount())
        return NULL;

    AppDomain * result = (AppDomain *)m_appDomainIdList.Get(requestedID);

#ifndef CROSSGEN_COMPILE
    if(result==NULL && GetThread() == FinalizerThread::GetFinalizerThread() &&
        SystemDomain::System()->AppDomainBeingUnloaded()!=NULL &&
        SystemDomain::System()->AppDomainBeingUnloaded()->GetId()==index)
        result=SystemDomain::System()->AppDomainBeingUnloaded();
    // If the current thread can't enter the AppDomain, then don't return it.
    if (!result || !result->CanThreadEnter(GetThread()))
        return NULL;
#endif // CROSSGEN_COMPILE

    return result;
}

// Releases an appdomain index.   Note that today we have code that depends on these
// indexes not being recycled, so we don't actually shrink m_appDomainIdList, but 
// simply zero out an entry.   THus we 'leak' the memory associated the slot in
// m_appDomainIdList.  
// 
// TODO make this a sparse structure so that we avoid that leak.  
//
void SystemDomain::ReleaseAppDomainId(ADID index)
{
    LIMITED_METHOD_CONTRACT;
    index.m_dwId--;

    _ASSERTE(index.m_dwId < (DWORD)m_appDomainIdList.GetCount());

    m_appDomainIdList.Set(index.m_dwId, NULL);
}

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)

#ifdef _DEBUG
int g_fMainThreadApartmentStateSet = 0;
#endif

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

void SystemDomain::SetThreadAptState (IMDInternalImport* pScope, Thread::ApartmentState state)
{
    STANDARD_VM_CONTRACT;

    BOOL fIsLegacy = FALSE;

    // Check for legacy behavior regarding COM Apartment state of the main thread.

#define METAMODEL_MAJOR_VER_WITH_NEW_BEHAVIOR 2
#define METAMODEL_MINOR_VER_WITH_NEW_BEHAVIOR 0

    LPCSTR pVer;
    IfFailThrow(pScope->GetVersionString(&pVer));
    
    // Does this look like a version?
    if (pVer != NULL)
    {
        // Is it 'vN.' where N is a digit?
        if ((pVer[0] == 'v' || pVer[0] == 'V') &&
            IS_DIGIT(pVer[1]) &&
            (pVer[2] == '.') )
        {
            // Looks like a version.  Is it lesser than v2.0 major version where we start using new behavior?
            fIsLegacy = DIGIT_TO_INT(pVer[1]) < METAMODEL_MAJOR_VER_WITH_NEW_BEHAVIOR;
        }
    }

    if (!fIsLegacy && g_pConfig != NULL)
    {
        fIsLegacy = g_pConfig->LegacyApartmentInitPolicy();
    }


    Thread* pThread = GetThread();
    _ASSERTE(pThread);

    if(state == Thread::AS_InSTA)
    {
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InSTA, TRUE);
        _ASSERTE(pState == Thread::AS_InSTA);
    }
    else if ((state == Thread::AS_InMTA) || (!fIsLegacy))
    {
        // If either MTAThreadAttribute is specified or (if no attribute is specified and we are not
        // running in legacy mode), then
        // we will set the apartment state to MTA. The reason for this is to ensure the apartment
        // state is consistent and reliably set. Without this, the apartment state for the main
        // thread would be undefined and would actually be dependent on if the assembly was
        // ngen'd, which other type were loaded, etc.
        Thread::ApartmentState pState = pThread->SetApartment(Thread::AS_InMTA, TRUE);
        _ASSERTE(pState == Thread::AS_InMTA);
    }

#ifdef _DEBUG
    g_fMainThreadApartmentStateSet++;
#endif
}
#endif // defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)

// Looks in all the modules for the DefaultDomain attribute
// The order is assembly and then the modules. It is first
// come, first serve.
BOOL SystemDomain::SetGlobalSharePolicyUsingAttribute(IMDInternalImport* pScope, mdMethodDef mdMethod)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_FUSION
    HRESULT hr;

    //
    // Check to see if the assembly has the LoaderOptimization attribute set.
    //

    DWORD cbVal;
    BYTE *pVal;
    IfFailThrow(hr = pScope->GetCustomAttributeByName(mdMethod,
                                                      DEFAULTDOMAIN_LOADEROPTIMIZATION_TYPE,
                                                      (const void**)&pVal, &cbVal));

    if (hr == S_OK) {
        CustomAttributeParser cap(pVal, cbVal);
        IfFailThrow(cap.SkipProlog());

        UINT8 u1;
        IfFailThrow(cap.GetU1(&u1));

        g_dwGlobalSharePolicy = u1 & AppDomain::SHARE_POLICY_MASK;

        return TRUE;
    }
#endif    

    return FALSE;
}

void SystemDomain::SetupDefaultDomain()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;


    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    AppDomain *pDomain;
    pDomain = pThread->GetDomain();
    _ASSERTE(pDomain);

    GCX_COOP();

    ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(),ADV_DEFAULTAD)
    {
        // Push this frame around loading the main assembly to ensure the
        // debugger can properly recgonize any managed code that gets run
        // as "class initializaion" code.
        FrameWithCookie<DebuggerClassInitMarkFrame> __dcimf;

        {
            GCX_PREEMP();
            InitializeDefaultDomain(TRUE);
        }

        __dcimf.Pop();
    }
    END_DOMAIN_TRANSITION;

    RETURN;
}

HRESULT SystemDomain::SetupDefaultDomainNoThrow()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        SystemDomain::SetupDefaultDomain();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

#ifdef _DEBUG
int g_fInitializingInitialAD = 0;
#endif

// This routine completes the initialization of the default domaine.
// After this call mananged code can be executed.
void SystemDomain::InitializeDefaultDomain(
    BOOL allowRedirects,
    ICLRPrivBinder * pBinder)
{
    STANDARD_VM_CONTRACT;

    WCHAR* pwsConfig = NULL;
    WCHAR* pwsPath = NULL;

    ETWOnStartup (InitDefaultDomain_V1, InitDefaultDomainEnd_V1);

#if defined(FEATURE_FUSION) // SxS
    // Determine the application base and the configuration file name
    CQuickWSTR sPathName;
    CQuickWSTR sConfigName;

    SIZE_T  dwSize;
    HRESULT hr = GetConfigFileFromWin32Manifest(sConfigName.Ptr(),
                                                sConfigName.MaxSize(),
                                                &dwSize);
    if(FAILED(hr))
    {
        if(hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            sConfigName.ReSizeThrows(dwSize);
            hr = GetConfigFileFromWin32Manifest(sConfigName.Ptr(),
                                                sConfigName.MaxSize(),
                                                &dwSize);
        }
        IfFailThrow(hr);
    }
    else
        sConfigName.ReSizeThrows(dwSize);

    hr = GetApplicationPathFromWin32Manifest(sPathName.Ptr(),
                                             sPathName.MaxSize(),
                                             &dwSize);
    if(FAILED(hr))
    {
        if(hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            sPathName.ReSizeThrows(dwSize);
            hr = GetApplicationPathFromWin32Manifest(sPathName.Ptr(),
                                                     sPathName.MaxSize(),
                                                     &dwSize);
        }
        IfFailThrow(hr);
    }
    else
        sPathName.ReSizeThrows(dwSize);

    pwsConfig = (sConfigName.Size() > 0 ? sConfigName.Ptr() : NULL);
    pwsPath = (sPathName.Size() > 0 ? sPathName.Ptr() : NULL);
#endif // defined(FEATURE_FUSION) // SxS

    // Setup the default AppDomain.

#ifdef _DEBUG
    g_fInitializingInitialAD++;
#endif

    AppDomain* pDefaultDomain = SystemDomain::System()->DefaultDomain();

    if (pBinder != nullptr)
    {
        pDefaultDomain->SetLoadContextHostBinder(pBinder);
    }
    #ifdef FEATURE_APPX_BINDER
        else if (AppX::IsAppXProcess())
        {
            CLRPrivBinderAppX * pAppXBinder = CLRPrivBinderAppX::GetOrCreateBinder();
            pDefaultDomain->SetLoadContextHostBinder(pAppXBinder);
        }
    #endif

    {
        GCX_COOP();

#ifndef CROSSGEN_COMPILE
        if (!NingenEnabled())
        {
#ifndef FEATURE_CORECLR
            pDefaultDomain->InitializeHashing(NULL);
            pDefaultDomain->InitializeSorting(NULL);
#endif // FEATURE_CORECLR
        }
#endif // CROSSGEN_COMPILE

        pDefaultDomain->InitializeDomainContext(allowRedirects, pwsPath, pwsConfig);

#ifndef CROSSGEN_COMPILE
        if (!NingenEnabled())
        {
#ifdef FEATURE_CLICKONCE
            pDefaultDomain->InitializeDefaultClickOnceDomain();
#endif // FEATURE_CLICKONCE
    
            if (!IsSingleAppDomain())
            {
                pDefaultDomain->InitializeDefaultDomainManager();
                pDefaultDomain->InitializeDefaultDomainSecurity();
            }
        }
#endif // CROSSGEN_COMPILE
    }

    // DefaultDomain Load event
    ETW::LoaderLog::DomainLoad(pDefaultDomain);

#ifdef _DEBUG
    g_fInitializingInitialAD--;
#endif

    TESTHOOKCALL(RuntimeStarted(RTS_DEFAULTADREADY));
}



#ifndef CROSSGEN_COMPILE

#ifdef _DEBUG
Volatile<LONG> g_fInExecuteMainMethod = 0;
#endif

#ifndef FEATURE_CORECLR
void SystemDomain::ExecuteMainMethod(HMODULE hMod, __in_opt LPWSTR path /*=NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(hMod, NULL_OK));
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef _DEBUG
    CounterHolder counter(&g_fInExecuteMainMethod);
#endif

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    GCX_COOP();

    //
    // There is no EH protecting this transition!
    // This is generically ok in this method because if we throw out of here, it becomes unhandled anyway.
    //
    FrameWithCookie<ContextTransitionFrame> frame;
    pThread->EnterContextRestricted(SystemDomain::System()->DefaultDomain()->GetDefaultContext(), &frame);
    _ASSERTE(pThread->GetDomain());

    AppDomain *pDomain = GetAppDomain();
    _ASSERTE(pDomain);

    // Push this frame around loading the main assembly to ensure the
    // debugger can properly recognize any managed code that gets run
    // as "class initializaion" code.
    FrameWithCookie<DebuggerClassInitMarkFrame> __dcimf;
    {
        GCX_PREEMP();

        PEImageHolder pTempImage(PEImage::LoadImage(hMod));

        PEFileHolder pTempFile(PEFile::Open(pTempImage.Extract()));

        // Check for CustomAttributes - Set up the DefaultDomain and the main thread
        // Note that this has to be done before ExplicitBind() as it
        // affects the bind
        mdToken tkEntryPoint = pTempFile->GetEntryPointToken();
        // <TODO>@TODO: What if the entrypoint is in another file of the assembly?</TODO>
        ReleaseHolder<IMDInternalImport> scope(pTempFile->GetMDImportWithRef());
        // In theory, we should have a valid executable image and scope should never be NULL, but we've been  
        // getting Watson failures for AVs here due to ISVs modifying image headers and some new OS loader 
        // checks (see Dev10# 718530 and Windows 7# 615596)
        if (scope == NULL)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }

#ifdef FEATURE_COMINTEROP
        Thread::ApartmentState state = Thread::AS_Unknown;        

        if((!IsNilToken(tkEntryPoint)) && (TypeFromToken(tkEntryPoint) == mdtMethodDef)) {
            if (scope->IsValidToken(tkEntryPoint))
                state = SystemDomain::GetEntryPointThreadAptState(scope, tkEntryPoint);
            else
                ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        // If the entry point has an explicit thread apartment state, set it
        // before running the AppDomainManager initialization code.
        if (state == Thread::AS_InSTA || state == Thread::AS_InMTA)
            SystemDomain::SetThreadAptState(scope, state);
#endif // FEATURE_COMINTEROP

        BOOL fSetGlobalSharePolicyUsingAttribute = FALSE;

        if((!IsNilToken(tkEntryPoint)) && (TypeFromToken(tkEntryPoint) == mdtMethodDef))
        {
            // The global share policy needs to be set before initializing default domain 
            // so that it is in place for loading of appdomain manager.
            fSetGlobalSharePolicyUsingAttribute = SystemDomain::SetGlobalSharePolicyUsingAttribute(scope, tkEntryPoint);
        }

        // This can potentially run managed code.
        InitializeDefaultDomain(FALSE);

#ifdef FEATURE_COMINTEROP
        // If we haven't set an explicit thread apartment state, set it after the
        // AppDomainManager has got a chance to go set it in InitializeNewDomain.
        if (state != Thread::AS_InSTA && state != Thread::AS_InMTA)
            SystemDomain::SetThreadAptState(scope, state);
#endif // FEATURE_COMINTEROP

        if (fSetGlobalSharePolicyUsingAttribute)
            SystemDomain::System()->DefaultDomain()->SetupLoaderOptimization(g_dwGlobalSharePolicy);

        NewHolder<IPEFileSecurityDescriptor> pSecDesc(Security::CreatePEFileSecurityDescriptor(pDomain, pTempFile));

        {
            GCX_COOP();
            pSecDesc->Resolve();
            if (pSecDesc->AllowBindingRedirects())
                pDomain->TurnOnBindingRedirects();
        }

        PEAssemblyHolder pFile(pDomain->BindExplicitAssembly(hMod, TRUE));

        pDomain->m_pRootAssembly = GetAppDomain()->LoadAssembly(NULL, pFile, FILE_ACTIVE);

        {
            GCX_COOP();

            // Reuse the evidence that was generated for the PEFile for the assembly so we don't have to
            // regenerate evidence of the same type again if it is requested later.
            pDomain->m_pRootAssembly->GetSecurityDescriptor()->SetEvidenceFromPEFile(pSecDesc);
        }

        // If the AppDomainManager for the default domain was specified in the application config file then
        // we require that the assembly be trusted in order to set the manager
        if (pDomain->HasAppDomainManagerInfo() && pDomain->AppDomainManagerSetFromConfig())
        {
            Assembly *pEntryAssembly = pDomain->GetAppDomainManagerEntryAssembly();
            if (!pEntryAssembly->GetSecurityDescriptor()->AllowApplicationSpecifiedAppDomainManager())
            {
                COMPlusThrow(kTypeLoadException, IDS_E_UNTRUSTED_APPDOMAIN_MANAGER);
            }
        }

        if (CorCommandLine::m_pwszAppFullName == NULL) {
            StackSString friendlyName;
            StackSString assemblyPath = pFile->GetPath();
            SString::Iterator i = assemblyPath.End();

            if (PEAssembly::FindLastPathSeparator(assemblyPath, i)) {
                i++;
                friendlyName.Set(assemblyPath, i, assemblyPath.End());
            }
            else
                friendlyName.Set(assemblyPath);

            pDomain->SetFriendlyName(friendlyName, TRUE);
        }
    }
    __dcimf.Pop();

    {
        GCX_PREEMP();

        LOG((LF_CLASSLOADER | LF_CORDB,
             LL_INFO10,
             "Created domain for an executable at %p\n",
             (pDomain->m_pRootAssembly ? pDomain->m_pRootAssembly->Parent() : NULL)));
        TESTHOOKCALL(RuntimeStarted(RTS_CALLINGENTRYPOINT));

#ifdef FEATURE_MULTICOREJIT
        pDomain->GetMulticoreJitManager().AutoStartProfile(pDomain);
#endif

        pDomain->m_pRootAssembly->ExecuteMainMethod(NULL, TRUE /* waitForOtherThreads */);
    }

    pThread->ReturnToContext(&frame);

#ifdef FEATURE_TESTHOOKS
    TESTHOOKCALL(LeftAppDomain(DefaultADID));
#endif
}
#endif //!FEATURE_CORECLR

#ifdef FEATURE_CLICKONCE
void SystemDomain::ActivateApplication(int *pReturnValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF   orThis;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCX_COOP();
    GCPROTECT_BEGIN(gc);

    gc.orThis = SystemDomain::System()->DefaultDomain()->GetExposedObject();

    MethodDescCallSite activateApp(METHOD__APP_DOMAIN__ACTIVATE_APPLICATION, &gc.orThis);

    ARG_SLOT args[] = {
        ObjToArgSlot(gc.orThis),
    };
    int retval = activateApp.Call_RetI4(args);
    if (pReturnValue)
        *pReturnValue = retval;

    GCPROTECT_END();
}
#endif // FEATURE_CLICKONCE

#ifdef FEATURE_MIXEDMODE
static HRESULT RunDllMainHelper(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved, Thread* pThread, bool bReenablePreemptive)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_FAULT;

    MethodDesc  *pMD;
    AppDomain   *pDomain;
    Module      *pModule;
    HRESULT     hr = S_FALSE;           // Assume no entry point.

    // Setup the thread state to cooperative to run managed code.

    // Get the old domain from the thread.  Legacy dll entry points must always
    // be run from the default domain.
    //
    // We cannot support legacy dlls getting loaded into all domains!!
    EX_TRY
    {
        ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(),ADV_DEFAULTAD)
        {
            pDomain = pThread->GetDomain();

            // The module needs to be in the current list if you are coming here.
            pModule = pDomain->GetIJWModule(hInst);
            if (!pModule)
                goto ErrExit;

            // See if there even is an entry point.
            pMD = pModule->GetDllEntryPoint();
            if (!pMD)
                goto ErrExit;

            // We're actually going to run some managed code.  There may be a customer
            // debug probe enabled, that prevents execution in the loader lock.
            CanRunManagedCode(hInst);

            {
                // Enter cooperative mode
                GCX_COOP_NO_DTOR();
            }

            // Run through the helper which will do exception handling for us.
            hr = ::RunDllMain(pMD, hInst, dwReason, lpReserved);

            {
                // Update thread state for the case where we are returning to unmanaged code.
                GCX_MAYBE_PREEMP_NO_DTOR(bReenablePreemptive);
            }

ErrExit: ;
        // does not throw exception
        }
        END_DOMAIN_TRANSITION;

    }
    EX_CATCH
    {
        hr = GetExceptionHResult(GET_THROWABLE());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return (hr);
}

//*****************************************************************************
// This guy will set up the proper thread state, look for the module given
// the hinstance, and then run the entry point if there is one.
//*****************************************************************************
HRESULT SystemDomain::RunDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{

    CONTRACTL
    {
        NOTHROW;
        if (GetThread() && !lpReserved) {MODE_PREEMPTIVE;} else {DISABLED(MODE_PREEMPTIVE);};
        if(GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);};
    }
    CONTRACTL_END;


    Thread      *pThread = NULL;
    BOOL        fEnterCoop = FALSE;
    HRESULT     hr = S_FALSE;           // Assume no entry point.

    pThread = GetThread();
    if ((!pThread && (dwReason == DLL_PROCESS_DETACH || dwReason == DLL_THREAD_DETACH)) ||
        g_fEEShutDown)
        return S_OK;

    // ExitProcess is called while a thread is doing GC.
    if (dwReason == DLL_PROCESS_DETACH && GCHeap::IsGCInProgress())
        return S_OK;

    // ExitProcess is called on a thread that we don't know about
    if (dwReason == DLL_PROCESS_DETACH && GetThread() == NULL)
        return S_OK;

    // Need to setup the thread since this might be the first time the EE has
    // seen it if the thread was created in unmanaged code and this is a thread
    // attach event.
    if (pThread)
        fEnterCoop = pThread->PreemptiveGCDisabled();
    else {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
            return hr;
    }

    return RunDllMainHelper(hInst, dwReason, lpReserved, pThread, !fEnterCoop);
}
#endif //  FEATURE_MIXEDMODE

#endif // CROSSGEN_COMPILE



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

#ifdef FEATURE_CORECLR
MethodTable *AppDomain::LoadCOMClass(GUID clsid,
                                     BOOL bLoadRecord/*=FALSE*/,
                                     BOOL* pfAssemblyInReg/*=NULL*/)
{
    // @CORESYSTODO: what to do here?
    return NULL;
}
#else // FEATURE_CORECLR

static BOOL IsSameRuntimeVersion(ICLRRuntimeInfo *pInfo1, ICLRRuntimeInfo *pInfo2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    WCHAR wszVersion1[_MAX_PATH]; 
    WCHAR wszVersion2[_MAX_PATH];
    DWORD cchVersion;

    cchVersion = COUNTOF(wszVersion1);
    IfFailThrow(pInfo1->GetVersionString(wszVersion1, &cchVersion));

    cchVersion = COUNTOF(wszVersion2);
    IfFailThrow(pInfo2->GetVersionString(wszVersion2, &cchVersion));

    return SString::_wcsicmp(wszVersion1, wszVersion2) == 0;
}

MethodTable *AppDomain::LoadCOMClass(GUID clsid,
                                     BOOL bLoadRecord/*=FALSE*/,
                                     BOOL* pfAssemblyInReg/*=NULL*/)
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;


    MethodTable* pMT = NULL;

    NewArrayHolder<WCHAR>  wszClassName = NULL;
    NewArrayHolder<WCHAR>  wszAssemblyString = NULL;
    NewArrayHolder<WCHAR>  wszCodeBaseString = NULL;

    DWORD   cbAssembly = 0;
    DWORD   cbCodeBase = 0;
    Assembly *pAssembly = NULL;
    BOOL    fFromRegistry = FALSE;
    BOOL    fRegFreePIA = FALSE;

    HRESULT hr = S_OK;

    if (pfAssemblyInReg != NULL)
        *pfAssemblyInReg = FALSE;

    // with sxs.dll help
    hr = FindShimInfoFromWin32(clsid, bLoadRecord, NULL, NULL, &wszClassName, &wszAssemblyString, &fRegFreePIA);

    if(FAILED(hr))
    {
        hr = FindShimInfoFromRegistry(clsid, bLoadRecord, VER_ASSEMBLYMAJORVERSION, VER_ASSEMBLYMINORVERSION,
                                      &wszClassName, &wszAssemblyString, &wszCodeBaseString);
        if (FAILED(hr))
            RETURN NULL;

        fFromRegistry = TRUE;
    }

    // Skip the GetRuntimeForManagedCOMObject check for value types since they cannot be activated and are
    // always used for wrapping existing instances coming from COM.
    if (!bLoadRecord)
    {
        // We will load the assembly only if it is a PIA or if unmanaged activation would load the currently running
        // runtime. Otherwise we return NULL which will result in using the default System.__ComObject type.

        // the type is a PIA type if mscoree.dll is not its inproc server dll or it was specified as <clrSurrogate> in the manifest
        BOOL fPIA = (fFromRegistry ? !Clr::Util::Com::CLSIDHasMscoreeAsInprocServer32(clsid) : fRegFreePIA);
        if (!fPIA)
        {
            // this isn't a PIA, so we must determine which runtime it would load
            ReleaseHolder<ICLRRuntimeHostInternal> pRuntimeHostInternal;
            IfFailThrow(g_pCLRRuntime->GetInterface(CLSID_CLRRuntimeHostInternal,
                                                    IID_ICLRRuntimeHostInternal,
                                                    &pRuntimeHostInternal));

            // we call the shim to see which runtime would this be activated in
            ReleaseHolder<ICLRRuntimeInfo> pRuntimeInfo;
            if (FAILED(pRuntimeHostInternal->GetRuntimeForManagedCOMObject(clsid, IID_ICLRRuntimeInfo, &pRuntimeInfo)))
            {
                // the requested runtime is not loadable - don't load the assembly
                RETURN NULL;
            }

            if (!IsSameRuntimeVersion(g_pCLRRuntime, pRuntimeInfo))
            {
                // the requested runtime is different from this runtime - don't load the assembly
                RETURN NULL;
            }
        }
    }

    if (pfAssemblyInReg != NULL)
        *pfAssemblyInReg = TRUE;

    if (wszAssemblyString != NULL) {
        pAssembly = LoadAssemblyHelper(wszAssemblyString, wszCodeBaseString);
        pMT = TypeName::GetTypeFromAssembly(wszClassName, pAssembly).GetMethodTable();
        if (!pMT)
            goto ErrExit;
    }

    if (pMT == NULL) {
    ErrExit:
        // Convert the GUID to its string representation.
        WCHAR szClsid[64];
        if (GuidToLPWSTR(clsid, szClsid, NumItems(szClsid)) == 0)
            szClsid[0] = 0;

        // Throw an exception indicating we failed to load the type with
        // the requested CLSID.
        COMPlusThrow(kTypeLoadException, IDS_CLASSLOAD_NOCLSIDREG, szClsid);
    }

    RETURN pMT;
}

#endif // FEATURE_CORECLR

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
#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
        CLASS__ITYPE,
        CLASS__IASSEMBLY,
        CLASS__IMETHODBASE,
        CLASS__IMETHODINFO,
        CLASS__ICONSTRUCTORINFO,
        CLASS__IFIELDINFO,
        CLASS__IPROPERTYINFO,
        CLASS__IEVENTINFO,
        CLASS__IAPPDOMAIN,
#endif // FEATURE_COMINTEROP && !FEATURE_CORECLR
        CLASS__LAZY_INITIALIZER,
        CLASS__DYNAMICMETHOD,
        CLASS__DELEGATE,
        CLASS__MULTICAST_DELEGATE
    };

    static const BinderClassID genericReflectionInvocationTypes[] = {
        CLASS__LAZY_HELPERS,
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

        MscorlibBinder::GetClass(CLASS__APP_DOMAIN);

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

        // AppDomain is an example of a type that is both used in the implementation of
        // reflection, and also a type that contains methods that are clients of reflection
        // (i.e., they instigate their own CreateInstance). Skip all AppDomain frames that
        // are NOT known clients of reflection. NOTE: The ever-increasing complexity of this
        // exclusion list is a sign that we need a better way--this is error-prone and
        // unmaintainable as more changes are made to BCL types.
        if ((pCaller == MscorlibBinder::GetExistingClass(CLASS__APP_DOMAIN))
            && (pMeth != MscorlibBinder::GetMethod(METHOD__APP_DOMAIN__CREATE_APP_DOMAIN_MANAGER)) // This uses reflection to create an AppDomainManager
    #ifdef FEATURE_CLICKONCE
            && (pMeth != MscorlibBinder::GetMethod(METHOD__APP_DOMAIN__ACTIVATE_APPLICATION)) // This uses reflection to create an ActivationContext
    #endif
            )
        {
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
#ifdef FEATURE_REMOTING    
    BOOL skippingRemoting;
#endif
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

/*static*/
Module* SystemDomain::GetCallersModule(int skip)
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

    CallersData cdata;
    ZeroMemory(&cdata, sizeof(CallersData));
    cdata.skip = skip;

    StackWalkFunctions(GetThread(), CallersMethodCallback, &cdata);

    if(cdata.pMethod)
        return cdata.pMethod->GetModule();
    else
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
        SO_INTOLERANT;
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

#ifdef FEATURE_REMOTING
    if (pFunc == MscorlibBinder::GetMethod(METHOD__STACK_BUILDER_SINK__PRIVATE_PROCESS_MESSAGE))
    {
        _ASSERTE(!pCaller->skippingRemoting);
        pCaller->skippingRemoting = true;
        return SWA_CONTINUE;
    }
    // And we spot the client end because there's a transparent proxy transition
    // frame pushed.
    if (frame && frame->GetFrameType() == Frame::TYPE_TP_METHOD_FRAME)
    {
        pCaller->skippingRemoting = false;
        return SWA_CONTINUE;
    }

    // Skip any frames into between the server and client remoting endpoints.
    if (pCaller->skippingRemoting)
        return SWA_CONTINUE;
#endif


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

#ifndef FEATURE_REMOTING
    // If remoting is not available, we only set the caller if the crawlframe is from the same domain.
    // Why? Because if the callerdomain is different from current domain,
    // there have to be interop/native frames in between.
    // For example, in the CORECLR, if we find the caller to be in a different domain, then the 
    // call into reflection is due to an unmanaged call into mscorlib. For that
    // case, the caller really is an INTEROP method.
    // In general, if the caller is INTEROP, we set the caller/callerdomain to be NULL 
    // (To be precise: they are already NULL and we don't change them).
    if (pCf->GetAppDomain() == GetAppDomain())
#endif // FEATURE_REMOTING
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
    STATIC_CONTRACT_SO_TOLERANT;
    MethodDesc *pFunc = pCf->GetFunction();

    /* We asked to be called back only for functions */
    _ASSERTE(pFunc);

    // Ignore intercepted frames
    if(pFunc->IsInterceptedForDeclSecurity())
        return SWA_CONTINUE;

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

void SystemDomain::CreateDefaultDomain()
{
    STANDARD_VM_CONTRACT;

#ifdef CROSSGEN_COMPILE
    AppDomainRefHolder pDomain(theDomain);
#else
    AppDomainRefHolder pDomain(new AppDomain());
#endif

    SystemDomain::LockHolder lh;
    pDomain->Init();

    Security::SetDefaultAppDomainProperty(pDomain->GetSecurityDescriptor());

    // need to make this assignment here since we'll be releasing
    // the lock before calling AddDomain. So any other thread
    // grabbing this lock after we release it will find that
    // the COM Domain has already been created
    m_pDefaultDomain = pDomain;
    _ASSERTE (pDomain->GetId().m_dwId == DefaultADID);

    // allocate a Virtual Call Stub Manager for the default domain
    m_pDefaultDomain->InitVSD();

    pDomain->SetStage(AppDomain::STAGE_OPEN);
    pDomain.SuppressRelease();

    LOG((LF_CLASSLOADER | LF_CORDB,
         LL_INFO10,
         "Created default domain at %p\n", m_pDefaultDomain));
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
            IncrementNumAppDomains(); // Maintain a count of app domains added to the list.
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

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(SharedDomain::GetDomain());
        g_profControlBlock.pProfInterface->AppDomainCreationStarted((AppDomainID) SharedDomain::GetDomain());
        END_PIN_PROFILER();
    }

    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        _ASSERTE(SharedDomain::GetDomain());
        g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID) SharedDomain::GetDomain(), S_OK);
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

#ifdef FEATURE_FUSION
static HRESULT GetVersionPath(HKEY root, __in LPWSTR key, __out LPWSTR* pDevpath, DWORD* pdwDevpath)
{
    CONTRACTL
    {
        MODE_PREEMPTIVE;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    DWORD rtn;
    RegKeyHolder versionKey;
    rtn = WszRegOpenKeyEx(root, key, 0, KEY_READ, &versionKey);
    if(rtn == ERROR_SUCCESS) {
        DWORD type;
        DWORD cbDevpath;
        if(WszRegQueryValueEx(versionKey, W("devpath"), 0, &type, (LPBYTE) NULL, &cbDevpath) == ERROR_SUCCESS && type == REG_SZ) {
            *pDevpath = (LPWSTR) new (nothrow) BYTE[cbDevpath];
            if(*pDevpath == NULL)
                return E_OUTOFMEMORY;
            else {
                rtn = WszRegQueryValueEx(versionKey, W("devpath"), 0, &type, (LPBYTE) *pDevpath, &cbDevpath);
                if ((rtn == ERROR_SUCCESS) && (type == REG_SZ))
                    *pdwDevpath = (DWORD) wcslen(*pDevpath);
            }
        }
        else
            return REGDB_E_INVALIDVALUE;
    }

    return HRESULT_FROM_WIN32(rtn);
}

// Get the developers path from the environment. This can only be set through the environment and
// cannot be added through configuration files, registry etc. This would make it to easy for
// developers to deploy apps that are not side by side. The environment variable should only
// be used on developers machines where exact matching to versions makes build and testing to
// difficult.
void SystemDomain::GetDevpathW(__out_ecount_opt(1) LPWSTR* pDevpath, DWORD* pdwDevpath)
{
   CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();

    if(g_pConfig->DeveloperInstallation() && m_fDevpath == FALSE) {

        LockHolder lh;

        if(m_fDevpath == FALSE) {
            DWORD dwPath = 0;
            PathString m_pwDevpathholder; 
            dwPath = WszGetEnvironmentVariable(APPENV_DEVPATH, m_pwDevpathholder);
            if(dwPath) {
                m_pwDevpath = m_pwDevpathholder.GetCopyOfUnicodeString();
            }
            else {
                RegKeyHolder userKey;
                RegKeyHolder machineKey;

                WCHAR pVersion[MAX_PATH_FNAME];
                DWORD dwVersion = MAX_PATH_FNAME;
                HRESULT hr = S_OK;
                hr = FusionBind::GetVersion(pVersion, &dwVersion);
                if(SUCCEEDED(hr)) {
                    LONG rslt;
                    rslt = WszRegOpenKeyEx(HKEY_CURRENT_USER, FRAMEWORK_REGISTRY_KEY_W,0,KEY_READ, &userKey);
                    hr = HRESULT_FROM_WIN32(rslt);
                    if (SUCCEEDED(hr)) {
                        hr = GetVersionPath(userKey, pVersion, &m_pwDevpath, &m_dwDevpath);
                    }

                    if (FAILED(hr) && WszRegOpenKeyEx(HKEY_LOCAL_MACHINE, FRAMEWORK_REGISTRY_KEY_W,0,KEY_READ, &machineKey) == ERROR_SUCCESS) {
                        hr = GetVersionPath(machineKey, pVersion, &m_pwDevpath, &m_dwDevpath);
                    }
                }
                if (Assembly::FileNotFound(hr))
                    hr = S_FALSE;
                else
                    IfFailThrow(hr);
            }

            m_fDevpath = TRUE;
        }
        // lh out of scope here
    }

    if(pDevpath) *pDevpath = m_pwDevpath;
    if(pdwDevpath) *pdwDevpath = m_dwDevpath;
    return;
}
#endif // FEATURE_FUSION

#ifdef _DEBUG
struct AppDomain::ThreadTrackInfo {
    Thread *pThread;
    CDynArray<Frame *> frameStack;
};
#endif // _DEBUG

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
    m_pNextInDelayedUnloadList = NULL;
    m_pSecContext = NULL;
    m_fRudeUnload = FALSE;
    m_pUnloadRequestThread = NULL;
    m_ADUnloadSink=NULL;

#ifndef FEATURE_CORECLR
    m_bUseOsSorting = RunningOnWin8();
    m_sortVersion = DEFAULT_SORT_VERSION;
    m_pCustomSortLibrary = NULL;
#if _DEBUG
    m_bSortingInitialized = FALSE;
#endif // _DEBUG
    m_pNlsHashProvider = NULL;
#endif //!FEATURE_CORECLR

    // Initialize Shared state. Assemblies are loaded
    // into each domain by default.
#ifdef FEATURE_LOADER_OPTIMIZATION    
    m_SharePolicy = SHARE_POLICY_UNSPECIFIED;
#endif

    m_pRootAssembly = NULL;

    m_pwDynamicDir = NULL;

    m_dwFlags = 0;
    m_pSecDesc = NULL;
    m_pDefaultContext = NULL;
#ifdef FEATURE_COMINTEROP
    m_pComCallWrapperCache = NULL;
    m_pRCWCache = NULL;
    m_pRCWRefCache = NULL;
    m_pLicenseInteropHelperMT = NULL;
    m_COMorRemotingFlag = COMorRemoting_NotInitialized;
    memset(m_rpCLRTypes, 0, sizeof(m_rpCLRTypes));
#endif // FEATURE_COMINTEROP

    m_pUMEntryThunkCache = NULL;

    m_pAsyncPool = NULL;
    m_hHandleTableBucket = NULL;

    m_ExposedObject = NULL;
    m_pComIPForExposedObject = NULL;

 #ifdef _DEBUG
    m_pThreadTrackInfoList = NULL;
    m_TrackSpinLock = 0;
    m_Assemblies.Debug_SetAppDomain(this);
#endif // _DEBUG

    m_dwThreadEnterCount = 0;
    m_dwThreadsStillInAppDomain = (ULONG)-1;

    m_pSecDesc = NULL;
    m_hHandleTableBucket=NULL;

    m_ExposedObject = NULL;

#ifdef FEATURE_COMINTEROP
    m_pRefDispIDCache = NULL;
    m_hndMissing = NULL;
#endif

    m_pRefClassFactHash = NULL;
    m_anonymouslyHostedDynamicMethodsAssembly = NULL;

    m_ReversePInvokeCanEnter=TRUE;
    m_ForceTrivialWaitOperations = false;
    m_Stage=STAGE_CREATING;

    m_bForceGCOnUnload=FALSE;
    m_bUnloadingFromUnloadEvent=FALSE;
#ifdef _DEBUG
    m_dwIterHolders=0;
    m_dwRefTakers=0;
    m_dwCreationHolders=0;
#endif
    
#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    m_ullTotalProcessorUsage = 0;
    m_pullAllocBytes = NULL;
    m_pullSurvivedBytes = NULL;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

#ifdef FEATURE_TYPEEQUIVALENCE
    m_pTypeEquivalenceTable = NULL;
#endif // FEATURE_TYPEEQUIVALENCE

#ifdef FEATURE_COMINTEROP
#ifdef FEATURE_REFLECTION_ONLY_LOAD
    m_pReflectionOnlyWinRtBinder = NULL;
    m_pReflectionOnlyWinRtTypeCache = NULL;
#endif // FEATURE_REFLECTION_ONLY_LOAD
    m_pNameToTypeMap = NULL;
    m_vNameToTypeMapVersion = 0;
    m_nEpoch = 0;
    m_pWinRTFactoryCache = NULL;
#endif // FEATURE_COMINTEROP

    m_fAppDomainManagerSetInConfig = FALSE;
    m_dwAppDomainManagerInitializeDomainFlags = eInitializeNewDomainFlags_None;

#ifdef FEATURE_PREJIT
    m_pDomainFileWithNativeImageList = NULL;
#endif

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    m_fIsBindingModelLocked.Store(FALSE);
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

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

    if (m_dwId.m_dwId!=0)
        SystemDomain::ReleaseAppDomainId(m_dwId);

    m_AssemblyCache.Clear();

    if (m_ADUnloadSink)
        m_ADUnloadSink->Release();

    if (m_pSecContext)
        delete m_pSecContext;

    if(!g_fEEInit)
        Terminate();

#ifndef FEATURE_CORECLR
    if (m_pCustomSortLibrary)
        delete m_pCustomSortLibrary;

    if (m_pNlsHashProvider)
        delete m_pNlsHashProvider;
#endif


#ifdef FEATURE_REMOTING
    if (!g_fEEInit)
    {
        GCX_COOP();         // See SystemDomain::EnumAllStaticGCRefs if you are removing this
        CrossDomainTypeMap::FlushStaleEntries();
        CrossDomainFieldMap::FlushStaleEntries();
    }
#endif //  FEATURE_REMOTING

#ifdef FEATURE_COMINTEROP
#ifdef FEATURE_REFLECTION_ONLY_LOAD
    if (m_pReflectionOnlyWinRtBinder != NULL)
    {
        m_pReflectionOnlyWinRtBinder->Release();
    }
    if (m_pReflectionOnlyWinRtTypeCache != NULL)
    {
        m_pReflectionOnlyWinRtTypeCache->Release();
    }
#endif // FEATURE_REFLECTION_ONLY_LOAD
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
    
#ifdef _DEBUG
    // If we were tracking thread AD transitions, cleanup the list on shutdown
    if (m_pThreadTrackInfoList)
    {
        while (m_pThreadTrackInfoList->Count() > 0)
        {
            // Get the very last element
            ThreadTrackInfo *pElem = *(m_pThreadTrackInfoList->Get(m_pThreadTrackInfoList->Count() - 1));
            _ASSERTE(pElem);

            // Free the memory
            delete pElem;

            // Remove pointer entry from the list
            m_pThreadTrackInfoList->Delete(m_pThreadTrackInfoList->Count() - 1);
        }

        // Now delete the list itself
        delete m_pThreadTrackInfoList;
        m_pThreadTrackInfoList = NULL;
    }
#endif // _DEBUG

#endif // CROSSGEN_COMPILE
}

//*****************************************************************************
//*****************************************************************************
//*****************************************************************************
#ifdef _DEBUG
#include "handletablepriv.h"
#endif



void AppDomain::Init()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(SystemDomain::IsUnderDomainLock());
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

    m_dwId = SystemDomain::GetNewAppDomainId(this);

    m_LoaderAllocator.Init(this);

#ifndef CROSSGEN_COMPILE
    //Allocate the threadpool entry before the appdomin id list. Otherwise,
    //the thread pool list will be out of sync if insertion of id in 
    //the appdomain fails. 
    m_tpIndex = PerAppDomainTPCountList::AddNewTPIndex();    
#endif // CROSSGEN_COMPILE

    m_dwIndex = SystemDomain::GetNewAppDomainIndex(this);

#ifndef CROSSGEN_COMPILE
    PerAppDomainTPCountList::SetAppDomainId(m_tpIndex, m_dwId);

    m_ADUnloadSink=new ADUnloadSink();
#endif

    BaseDomain::Init();

    // Set up the IL stub cache
    m_ILStubCache.Init(GetLoaderAllocator()->GetHighFrequencyHeap());

    m_pSecContext = new SecurityContext (GetLowFrequencyHeap());

// Set up the binding caches
    m_AssemblyCache.Init(&m_DomainCacheCrst, GetHighFrequencyHeap());
    m_UnmanagedCache.InitializeTable(this, &m_DomainCacheCrst);

    m_MemoryPressure = 0;

    m_sDomainLocalBlock.Init(this);

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    // NOTE: it's important that we initialize ARM data structures before calling
    // Ref_CreateHandleTableBucket, this is because AD::Init() can race with GC
    // and once we add ourselves to the handle table map the GC can start walking
    // our handles and calling AD::RecordSurvivedBytes() which touches ARM data.
    if (GCHeap::IsServerHeap())
        m_dwNumHeaps = CPUGroupInfo::CanEnableGCCPUGroups() ?
                           CPUGroupInfo::GetNumActiveProcessors() :
                           GetCurrentProcessCpuCount();
    else
        m_dwNumHeaps = 1;
    m_pullAllocBytes = new ULONGLONG [m_dwNumHeaps * ARM_CACHE_LINE_SIZE_ULL];
    m_pullSurvivedBytes = new ULONGLONG [m_dwNumHeaps * ARM_CACHE_LINE_SIZE_ULL];
    for (DWORD i = 0; i < m_dwNumHeaps; i++)
    {
        m_pullAllocBytes[i * ARM_CACHE_LINE_SIZE_ULL] = 0;
        m_pullSurvivedBytes[i * ARM_CACHE_LINE_SIZE_ULL] = 0;
    }
    m_ullLastEtwAllocBytes = 0;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

    // Default domain reuses the handletablemap that was created during EEStartup since
    // default domain cannot be unloaded.
    if (GetId().m_dwId == DefaultADID)
    {
        m_hHandleTableBucket = g_HandleTableMap.pBuckets[0];
    }
    else
    {
        m_hHandleTableBucket = Ref_CreateHandleTableBucket(m_dwIndex);
    }

#ifdef _DEBUG
    if (((HandleTable *)(m_hHandleTableBucket->pTable[0]))->uADIndex != m_dwIndex)
        _ASSERTE (!"AD index mismatch");
#endif // _DEBUG

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

    CreateSecurityDescriptor();
    SetStage(STAGE_READYFORMANAGEDCODE);

#ifndef CROSSGEN_COMPILE
    m_pDefaultContext = new Context(this);

    m_ExposedObject = CreateHandle(NULL);

    // Create the Application Security Descriptor

    COUNTER_ONLY(GetPerfCounters().m_Loading.cAppDomains++);

#ifdef FEATURE_COMINTEROP
    if (!AppX::IsAppXProcess())
    {
#ifdef FEATURE_REFLECTION_ONLY_LOAD
        m_pReflectionOnlyWinRtTypeCache = clr::SafeAddRef(new CLRPrivTypeCacheReflectionOnlyWinRT());
        m_pReflectionOnlyWinRtBinder = clr::SafeAddRef(new CLRPrivBinderReflectionOnlyWinRT(m_pReflectionOnlyWinRtTypeCache));
#endif
    }
#ifdef FEATURE_APPX_BINDER
    else if (g_fEEStarted && !IsDefaultDomain())
    {   // Non-default domain in an AppX process. This exists only for designers and we'd better be in dev mode.
        _ASSERTE(IsCompilationProcess() || AppX::IsAppXDesignMode());

        // Inherit AppX binder from default domain.
        SetLoadContextHostBinder(SystemDomain::System()->DefaultDomain()->GetLoadContextHostBinder());

        // Note: LoadFrom, LoadFile, Load(byte[], ...), ReflectionOnlyLoad, LoadWithPartialName,
        /// etc. are not supported and are actively blocked.
    }
#endif //FEATURE_APPX_BINDER
#endif //FEATURE_COMINTEROP

#endif // CROSSGEN_COMPILE
} // AppDomain::Init


/*********************************************************************/

BOOL AppDomain::IsCompilationDomain()
{
    LIMITED_METHOD_CONTRACT;

    BOOL isCompilationDomain = (m_dwFlags & COMPILATION_DOMAIN) != 0;
#ifdef FEATURE_PREJIT
    _ASSERTE(!isCompilationDomain ||
             (IsCompilationProcess() && IsPassiveDomain()));
#endif // FEATURE_PREJIT
    return isCompilationDomain;
}

#ifndef CROSSGEN_COMPILE

extern int g_fADUnloadWorkerOK;

// Notes:
//   This helper will send the AppDomain creation notifications for profiler / debugger.
//   If it throws, its backout code will also send a notification.
//   If it succeeds, then we still need to send a AppDomainCreateFinished notification.
void AppDomain::CreateUnmanagedObject(AppDomainCreationHolder<AppDomain>& pDomain)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GCX_PREEMP();

    pDomain.Assign(new AppDomain());
    if (g_fADUnloadWorkerOK<0)
    {
        AppDomain::CreateADUnloadWorker();
    }

    //@todo: B#25921
    // We addref Appdomain object here and notify a profiler that appdomain 
    // creation has started, then return to managed code which will  call 
    // the function that releases the appdomain and notifies a profiler that we finished
    // creating the appdomain. If an exception is raised while we're in that managed code
    // we will leak memory and the profiler will not be notified about the failure

#ifdef PROFILING_SUPPORTED
    // Signal profile if present.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        g_profControlBlock.pProfInterface->AppDomainCreationStarted((AppDomainID) (AppDomain*) pDomain);
        END_PIN_PROFILER();
    }
    EX_TRY
#endif // PROFILING_SUPPORTED
    {
        {
            SystemDomain::LockHolder lh;
            pDomain->Init(); 
            // allocate a Virtual Call Stub Manager for this domain
            pDomain->InitVSD();
        }

        pDomain->SetCanUnload();    // by default can unload any domain
        
        #ifdef DEBUGGING_SUPPORTED    
        // Notify the debugger here, before the thread transitions into the 
        // AD to finish the setup, and before any assemblies are loaded into it.
        SystemDomain::PublishAppDomainAndInformDebugger(pDomain);
        #endif // DEBUGGING_SUPPORTED

        STRESS_LOG2 (LF_APPDOMAIN, LL_INFO100, "Create domain [%d] %p\n", pDomain->GetId().m_dwId, (AppDomain*)pDomain);
        pDomain->LoadSystemAssemblies();
        pDomain->SetupSharedStatics();

        pDomain->SetStage(AppDomain::STAGE_ACTIVE);    
    }        
#ifdef PROFILING_SUPPORTED
    EX_HOOK
    {
        // Need the first assembly loaded in to get any data on an app domain.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
            g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID)(AppDomain*) pDomain, GET_EXCEPTION()->GetHR());
            END_PIN_PROFILER();
        }
    }
    EX_END_HOOK;

    // On success, caller must still send the AppDomainCreationFinished notification.
#endif // PROFILING_SUPPORTED
}

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
#endif // DEBUGGING_SUPPORTED

    m_pRootAssembly = NULL; // This assembly is in the assembly list;

    if (m_pSecDesc != NULL)
    {
        delete m_pSecDesc;
        m_pSecDesc = NULL;
    }

#ifdef DEBUGGING_SUPPORTED
    if (NULL != g_pDebugInterface)
    {
        // Call the publisher API to delete this appdomain entry from the list
        CONTRACT_VIOLATION(ThrowsViolation);
        g_pDebugInterface->RemoveAppDomainFromIPC (this);
    }
#endif // DEBUGGING_SUPPORTED
}

void AppDomain::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();


    _ASSERTE(m_dwThreadEnterCount == 0 || IsDefaultDomain());

    if (m_pComIPForExposedObject)
    {
        m_pComIPForExposedObject->Release();
        m_pComIPForExposedObject = NULL;
    }

    delete m_pDefaultContext;
    m_pDefaultContext = NULL;

    if (m_pUMEntryThunkCache)
    {
        delete m_pUMEntryThunkCache;
        m_pUMEntryThunkCache = NULL;
    }

#ifdef FEATURE_COMINTEROP
    if (m_pRCWCache)
    {
        delete m_pRCWCache;
        m_pRCWCache = NULL;
    }

    if (m_pRCWRefCache)
    {
        delete m_pRCWRefCache;
        m_pRCWRefCache = NULL;
    }
    
    if (m_pComCallWrapperCache)
    {
        m_pComCallWrapperCache->Neuter();
        m_pComCallWrapperCache->Release();
    }

    // if the above released the wrapper cache, then it will call back and reset our
    // m_pComCallWrapperCache to null. If not null, then need to set it's domain pointer to
    // null.
    if (! m_pComCallWrapperCache)
    {
        LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::Terminate ComCallWrapperCache released\n"));
    }
#ifdef _DEBUG
    else
    {
        m_pComCallWrapperCache = NULL;
        LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::Terminate ComCallWrapperCache not released\n"));
    }
#endif // _DEBUG

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_FUSION
    if(m_pAsyncPool != NULL)
    {
        delete m_pAsyncPool;
        m_pAsyncPool = NULL;
    }
#endif

    if (!IsAtProcessExit())
    {
        // if we're not shutting down everything then clean up the string literals associated
        // with this appdomain -- note that is no longer needs to happen while suspended
        // because the appropriate locks are taken in the GlobalStringLiteralMap
        // this is important as this locks have a higher lock number than does the
        // thread-store lock which is taken when we suspend.
        GetLoaderAllocator()->CleanupStringLiteralMap();

        // Suspend the EE to do some clean up that can only occur
        // while no threads are running.
        GCX_COOP (); // SuspendEE may require current thread to be in Coop mode
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN);
    }

    // Note that this must be performed before restarting the EE. It will clean
    // the cache and prevent others from using stale cache entries.
    //@TODO: Would be nice to get this back to BaseDomain, but need larger fix for that.
    // NOTE: Must have the runtime suspended to unlink managers
    // NOTE: May be NULL due to OOM during initialization. Can skip in that case.
    GetLoaderAllocator()->UninitVirtualCallStubManager();
    MethodTable::ClearMethodDataCache();
    ClearJitGenericHandleCache(this);

    // @TODO s_TPMethodTableCrst prevents us from from keeping the whole
    // assembly shutdown logic here. See if we can do better in the next milestone
#ifdef  FEATURE_PREJIT
    DeleteNativeCodeRanges();
#endif

    if (!IsAtProcessExit())
    {
        // Resume the EE.
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }

    ShutdownAssemblies();
#ifdef FEATURE_CORECLR    
    ShutdownNativeDllSearchDirectories();
#endif

    if (m_pRefClassFactHash)
    {
        m_pRefClassFactHash->Destroy();
        // storage for m_pRefClassFactHash itself is allocated on the loader heap
    }

#ifdef FEATURE_TYPEEQUIVALENCE
    m_TypeEquivalenceCrst.Destroy();
#endif

    m_ReflectionCrst.Destroy();
    m_RefClassFactCrst.Destroy();

    m_LoaderAllocator.Terminate();

    BaseDomain::Terminate();

#ifdef _DEBUG
    if (m_hHandleTableBucket &&
        m_hHandleTableBucket->pTable &&
        ((HandleTable *)(m_hHandleTableBucket->pTable[0]))->uADIndex != m_dwIndex)
        _ASSERTE (!"AD index mismatch");
#endif // _DEBUG

    if (m_hHandleTableBucket) {
        Ref_DestroyHandleTableBucket(m_hHandleTableBucket);
        m_hHandleTableBucket = NULL;
    }

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    if (m_pullAllocBytes)
    {
        delete [] m_pullAllocBytes;
    }
    if (m_pullSurvivedBytes)
    {
        delete [] m_pullSurvivedBytes;
    }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

    if(m_dwIndex.m_dwIndex != 0)
        SystemDomain::ReleaseAppDomainIndex(m_dwIndex);
} // AppDomain::Terminate

void AppDomain::CloseDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    BOOL bADRemoved=FALSE;;

    AddRef();  // Hold a reference
    AppDomainRefHolder AdHolder(this);
    {
        SystemDomain::LockHolder lh;

        SystemDomain::System()->DecrementNumAppDomains(); // Maintain a count of app domains added to the list.
        bADRemoved = SystemDomain::System()->RemoveDomain(this);
    }

    if(bADRemoved)
        Stop();
}

/*********************************************************************/

struct GetExposedObject_Args
{
    AppDomain *pDomain;
    OBJECTREF *ref;
};

static void GetExposedObject_Wrapper(LPVOID ptr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    GetExposedObject_Args *args = (GetExposedObject_Args *) ptr;
    *(args->ref) = args->pDomain->GetExposedObject();
}


OBJECTREF AppDomain::GetExposedObject()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    OBJECTREF ref = GetRawExposedObject();
    if (ref == NULL)
    {
        APPDOMAINREF obj = NULL;

        Thread *pThread = GetThread();
        if (pThread->GetDomain() != this)
        {
            GCPROTECT_BEGIN(ref);
            GetExposedObject_Args args = {this, &ref};
            // call through DoCallBack with a domain transition
            pThread->DoADCallBack(this,GetExposedObject_Wrapper, &args,ADV_CREATING|ADV_RUNNINGIN);
            GCPROTECT_END();
            return ref;
        }
        MethodTable *pMT = MscorlibBinder::GetClass(CLASS__APP_DOMAIN);

        // Create the module object
        obj = (APPDOMAINREF) AllocateObject(pMT);
        obj->SetDomain(this);

        if(StoreFirstObjectInHandle(m_ExposedObject, (OBJECTREF) obj) == FALSE) {
            obj = (APPDOMAINREF) GetRawExposedObject();
            _ASSERTE(obj);
        }

        return (OBJECTREF) obj;
    }

    return ref;
}

#ifndef FEATURE_CORECLR
void AppDomain::InitializeSorting(OBJECTREF* ppAppdomainSetup)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(ppAppdomainSetup == NULL || IsProtectedByGCFrame(ppAppdomainSetup));
    }
    CONTRACTL_END;

    DWORD sortVersionFromConfig = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CompatSortNLSVersion);

    if(sortVersionFromConfig != 0)
    {
        m_bUseOsSorting = FALSE;
        m_sortVersion = sortVersionFromConfig;
    }

    if(ppAppdomainSetup != NULL)
    {
        APPDOMAINSETUPREF adSetup = (APPDOMAINSETUPREF) *ppAppdomainSetup;
        APPDOMAINSORTINGSETUPINFOREF sortingSetup = adSetup->GetAppDomainSortingSetupInfo();

        if(sortingSetup != NULL)
        {
            if(sortingSetup->UseV2LegacySorting() || sortingSetup->UseV4LegacySorting())
            {        

                m_bUseOsSorting = FALSE;
    
                if(sortingSetup->UseV2LegacySorting())
                {           
                    m_sortVersion = SORT_VERSION_WHIDBEY;
                }

                if(sortingSetup->UseV4LegacySorting())
                {
                    m_sortVersion = SORT_VERSION_V4;
                }
            }
            else if(sortingSetup->GetPFNIsNLSDefinedString() != NULL 
                    && sortingSetup->GetPFNCompareStringEx() != NULL 
                    && sortingSetup->GetPFNLCMapStringEx() != NULL 
                    && sortingSetup->GetPFNFindNLSStringEx() != NULL 
                    && sortingSetup->GetPFNCompareStringOrdinal() != NULL 
                    && sortingSetup->GetPFNGetNLSVersionEx() != NULL
                    && sortingSetup->GetPFNFindStringOrdinal() != NULL)
            {
                m_pCustomSortLibrary = new COMNlsCustomSortLibrary;    
                m_pCustomSortLibrary->pIsNLSDefinedString = (PFN_IS_NLS_DEFINED_STRING) sortingSetup->GetPFNIsNLSDefinedString();
                m_pCustomSortLibrary->pCompareStringEx = (PFN_COMPARE_STRING_EX) sortingSetup->GetPFNCompareStringEx();
                m_pCustomSortLibrary->pLCMapStringEx = (PFN_LC_MAP_STRING_EX) sortingSetup->GetPFNLCMapStringEx();
                m_pCustomSortLibrary->pFindNLSStringEx = (PFN_FIND_NLS_STRING_EX) sortingSetup->GetPFNFindNLSStringEx();
                m_pCustomSortLibrary->pCompareStringOrdinal = (PFN_COMPARE_STRING_ORDINAL) sortingSetup->GetPFNCompareStringOrdinal();
                m_pCustomSortLibrary->pGetNLSVersionEx = (PFN_GET_NLS_VERSION_EX) sortingSetup->GetPFNGetNLSVersionEx();
                m_pCustomSortLibrary->pFindStringOrdinal = (PFN_FIND_STRING_ORDINAL) sortingSetup->GetPFNFindStringOrdinal();
            }
        }
    }

    if(m_bUseOsSorting == FALSE && m_sortVersion == DEFAULT_SORT_VERSION)
    {
        // If we are using the legacy sorting dlls, the default version for sorting is SORT_VERSION_V4.  Note that
        // we don't expect this to change in the future (even when V5 or V6 of the runtime comes out).
        m_sortVersion = SORT_VERSION_V4;
    }

    if(RunningOnWin8() && m_bUseOsSorting == FALSE)
    {
        // We need to ensure that the versioned sort DLL could load so we don't crash later.  This ensures we have
        // the same behavior as Windows 7, where even if we couldn't load the correct versioned sort dll, we would
        // provide the default sorting behavior.
        INT_PTR sortOrigin;
        if(COMNlsInfo::InternalInitVersionedSortHandle(W(""), &sortOrigin, m_sortVersion) == NULL)
        {
            LOG((LF_APPDOMAIN, LL_WARNING, "AppDomain::InitializeSorting failed to load legacy sort DLL for AppDomain.\n"));
            // We couldn't load a sort DLL.  Fall back to default sorting using the OS.
            m_bUseOsSorting = TRUE;
            m_sortVersion = DEFAULT_SORT_VERSION;
        }        
    }

#if _DEBUG
    m_bSortingInitialized = TRUE;
#endif
}
#endif

#ifndef FEATURE_CORECLR
void AppDomain::InitializeHashing(OBJECTREF* ppAppdomainSetup)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(ppAppdomainSetup == NULL || IsProtectedByGCFrame(ppAppdomainSetup));
    }
    CONTRACTL_END;
 
    m_pNlsHashProvider = new COMNlsHashProvider;

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    BOOL fUseRandomizedHashing = (BOOL) CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_UseRandomizedStringHashAlgorithm);

    if(ppAppdomainSetup != NULL)
    {
        APPDOMAINSETUPREF adSetup = (APPDOMAINSETUPREF) *ppAppdomainSetup;
        fUseRandomizedHashing |= adSetup->UseRandomizedStringHashing();
    }

    m_pNlsHashProvider->SetUseRandomHashing(fUseRandomizedHashing);
#endif // FEATURE_RANDOMIZED_STRING_HASHING
}
#endif // FEATURE_CORECLR

OBJECTREF AppDomain::DoSetup(OBJECTREF* setupInfo)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    ADID adid=GetAppDomain()->GetId();

    OBJECTREF retval=NULL;
    GCPROTECT_BEGIN(retval);    

    ENTER_DOMAIN_PTR(this,ADV_CREATING);

    MethodDescCallSite setup(METHOD__APP_DOMAIN__SETUP);

    ARG_SLOT args[1];

    args[0]=ObjToArgSlot(*setupInfo);

    OBJECTREF activator;
    activator=setup.Call_RetOBJECTREF(args);
#ifdef FEATURE_REMOTING
    if (activator != NULL)
    {
        GCPROTECT_BEGIN(activator);
        retval=AppDomainHelper::CrossContextCopyTo(adid,&activator);
        GCPROTECT_END();
    }
#else
    _ASSERTE(activator==NULL);
#endif
    
#if defined(FEATURE_MULTICOREJIT)
    // Disable AutoStartProfile in default domain from this code path.
    // It's called from SystemDomain::ExecuteMainMethod for normal program, not needed for SL and Asp.Net
    if (! IsDefaultDomain())
    {
        GCX_PREEMP();

        GetMulticoreJitManager().AutoStartProfile(this);
    }
#endif

    END_DOMAIN_TRANSITION;
    GCPROTECT_END();
    return retval;
}

#endif // !CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP
#ifndef CROSSGEN_COMPILE
HRESULT AppDomain::GetComIPForExposedObject(IUnknown **pComIP)
{
    // Assumption: This function is called for AppDomain's that the current
    //             thread is in or has entered, or the AppDomain is kept alive.
    //
    // Assumption: This function can now throw.  The caller is responsible for any
    //             BEGIN_EXTERNAL_ENTRYPOINT, EX_TRY, or other
    //             techniques to convert to a COM HRESULT protocol.
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    Thread *pThread = GetThread();
    if (m_pComIPForExposedObject)
    {
        GCX_PREEMP_THREAD_EXISTS(pThread);
        m_pComIPForExposedObject->AddRef();
        *pComIP = m_pComIPForExposedObject;
        return S_OK;
    }

    IUnknown* punk = NULL;

    OBJECTREF ref = NULL;
    GCPROTECT_BEGIN(ref);

    EnsureComStarted();

    ENTER_DOMAIN_PTR(this,ADV_DEFAULTAD)
    {
        ref = GetExposedObject();
        punk = GetComIPFromObjectRef(&ref);
        if (FastInterlockCompareExchangePointer(&m_pComIPForExposedObject, punk, NULL) == NULL)
        {
            GCX_PREEMP_THREAD_EXISTS(pThread);
            m_pComIPForExposedObject->AddRef();
        }
    }
    END_DOMAIN_TRANSITION;

    GCPROTECT_END();

    if(SUCCEEDED(hr))
    {
        *pComIP = m_pComIPForExposedObject;
    }

    return hr;
}
#endif //#ifndef CROSSGEN_COMPILE

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

void AppDomain::CreateSecurityDescriptor()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_pSecDesc == NULL);

    m_pSecDesc = Security::CreateApplicationSecurityDescriptor(this);
}

bool IsPlatformAssembly(LPCSTR szName, DomainAssembly *pDomainAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(szName));
        PRECONDITION(CheckPointer(pDomainAssembly));
    }
    CONTRACTL_END;

    PEAssembly *pPEAssembly = pDomainAssembly->GetFile();

    if (strcmp(szName, pPEAssembly->GetSimpleName()) != 0)
    {
        return false;
    }
    
    DWORD cbPublicKey;
    const BYTE *pbPublicKey = static_cast<const BYTE *>(pPEAssembly->GetPublicKey(&cbPublicKey));
    if (pbPublicKey == nullptr)
    {
        return false;
    }

#ifdef FEATURE_CORECLR
    return StrongNameIsSilverlightPlatformKey(pbPublicKey, cbPublicKey);
#else
    return StrongNameIsEcmaKey(pbPublicKey, cbPublicKey);
#endif
}

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

void AppDomain::RemoveAssembly_Unlocked(DomainAssembly * pAsm)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    
    _ASSERTE(GetAssemblyListLock()->OwnedByCurrentThread());
    
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
        kIncludeLoaded | 
        (assem->IsIntrospectionOnly() ? kIncludeIntrospection : kIncludeExecution)));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (i.Next(pDomainAssembly.This()))
    {
        CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetLoadedAssembly();
        if (pAssembly == assem)
            return TRUE;
    }

    return FALSE;
}

BOOL AppDomain::HasSetSecurityPolicy()
{
    CONTRACT(BOOL)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    GCX_COOP();

    if (NingenEnabled())
    {
        return FALSE;
    }
    RETURN ((APPDOMAINREF)GetExposedObject())->HasSetPolicy();
}

#if defined (FEATURE_LOADER_OPTIMIZATION) && !defined(FEATURE_CORECLR)
// Returns true if the user has declared the desire to load an 
// assembly domain-neutral.  This is either by specifying System.LoaderOptimizationAttribute
// on the entry routine or the host has set this loader-optimization flag.  
BOOL AppDomain::ApplySharePolicy(DomainAssembly *pFile)
{
    CONTRACT(BOOL)
    {
        PRECONDITION(CheckPointer(pFile));
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (!pFile->GetFile()->IsShareable())
        RETURN FALSE;

    if (ApplySharePolicyFlag(pFile))
        RETURN TRUE;

    RETURN FALSE;
}

BOOL AppDomain::ApplySharePolicyFlag(DomainAssembly *pFile)
{
    CONTRACT(BOOL)
    {
        PRECONDITION(CheckPointer(pFile));
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    switch(GetSharePolicy()) {
    case SHARE_POLICY_ALWAYS:
        RETURN (!pFile->MayHaveUnknownDependencies());

    case SHARE_POLICY_GAC:
        RETURN (pFile->IsClosedInGAC());

    case SHARE_POLICY_NEVER:
        RETURN pFile->IsSystem();

    default:
        UNREACHABLE_MSG("Unknown share policy");
    }
}
#endif // FEATURE_LOADER_OPTIMIZATION

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
    ((PEFile *) m_pData)->Release();
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

ADID FileLoadLock::GetAppDomainId()
{
    LIMITED_METHOD_CONTRACT;
    return m_AppDomainId;
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
    "VTABLE FIXUPS",                      // FILE_LOAD_VTABLE_FIXUPS
    "DELIVER_EVENTS",                     // FILE_LOAD_DELIVER_EVENTS
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
                STRESS_LOG4(LF_CLASSLOADER, LL_INFO100, "Completed Load Level %s for DomainFile %p in AD %i - success = %i\n", fileLoadLevelName[level], m_pDomainFile, m_AppDomainId.m_dwId, success);
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
    m_cachedHR(S_OK),
    m_AppDomainId(pDomainFile->GetAppDomain()->GetId())
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
        CHECK_MSG(HasUnloadStarted() || pModule->CheckActivated(),
              "Managed code can only run when its module has been activated in the current app domain");
    }

    CHECK_MSG(!IsPassiveDomain() || pModule->CanExecuteCode(),
              "Executing managed code from an unsafe assembly in a Passive AppDomain");

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
                                  FileLoadLevel targetLevel,
                                  AssemblyLoadSecurity *pLoadSecurity /* = NULL */)
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

    DomainAssembly *pAssembly = LoadDomainAssembly(pIdentity, pFile, targetLevel, pLoadSecurity);
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
    AssemblyLoadSecurity *pLoadSecurity;
    FileLoadLevel targetLevel;

    LoadDomainAssemblyStress(AppDomain *pThis, AssemblySpec* pSpec, PEAssembly *pFile, FileLoadLevel targetLevel, AssemblyLoadSecurity *pLoadSecurity)
        : pThis(pThis), pSpec(pSpec), pFile(pFile), pLoadSecurity(pLoadSecurity), targetLevel(targetLevel) {LIMITED_METHOD_CONTRACT;}

    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_INTOLERANT;
        SetupThread();
        pThis->LoadDomainAssembly(pSpec, pFile, targetLevel, pLoadSecurity);
    }
};
#endif // CROSSGEN_COMPILE

extern BOOL AreSameBinderInstance(ICLRPrivBinder *pBinderA, ICLRPrivBinder *pBinderB);

DomainAssembly* AppDomain::LoadDomainAssembly( AssemblySpec* pSpec,
                                                PEAssembly *pFile, 
                                                FileLoadLevel targetLevel,
                                                AssemblyLoadSecurity *pLoadSecurity /* = NULL */)
{
    STATIC_CONTRACT_THROWS;

    if (pSpec == nullptr)
    {
        // skip caching, since we don't have anything to base it on
        return LoadDomainAssemblyInternal(pSpec, pFile, targetLevel, pLoadSecurity);
    }

    DomainAssembly* pRetVal = NULL;
    EX_TRY
    {
        pRetVal = LoadDomainAssemblyInternal(pSpec, pFile, targetLevel, pLoadSecurity);
    }
    EX_HOOK
    {
        Exception* pEx=GET_EXCEPTION();
        if (!pEx->IsTransient())
        {
#if defined(FEATURE_CORECLR)
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
#endif // defined(FEATURE_CORECLR)

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
                                              FileLoadLevel targetLevel,
                                              AssemblyLoadSecurity *pLoadSecurity /* = NULL */)
{
    CONTRACT(DomainAssembly *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        PRECONDITION(CheckPointer(pLoadSecurity, NULL_OK));
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
    LoadDomainAssemblyStress ts (this, pIdentity, pFile, targetLevel, pLoadSecurity);
#endif

    // Go into preemptive mode since this may take a while.
    GCX_PREEMP();

    // Check for existing fully loaded assembly, or for an assembly which has failed during the loading process.
    result = FindAssembly(pFile, FindAssemblyOptions_IncludeFailedToLoad);
    
    if (result == NULL)
    {
        // Allocate the DomainAssembly a bit early to avoid GC mode problems. We could potentially avoid
        // a rare redundant allocation by moving this closer to FileLoadLock::Create, but it's not worth it.

        NewHolder<DomainAssembly> pDomainAssembly;
        pDomainAssembly = new DomainAssembly(this, pFile, pLoadSecurity, this->GetLoaderAllocator());

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

#ifdef  FEATURE_MULTIMODULE_ASSEMBLIES

#ifndef CROSSGEN_COMPILE
// Thread stress
class LoadDomainModuleStress : APIThreadStress
{
public:
    AppDomain *pThis;
    DomainAssembly *pAssembly;
    PEModule *pFile;
    FileLoadLevel targetLevel;

    LoadDomainModuleStress(AppDomain *pThis, DomainAssembly *pAssembly, PEModule *pFile, FileLoadLevel targetLevel)
        : pThis(pThis), pAssembly(pAssembly), pFile(pFile), targetLevel(targetLevel) {LIMITED_METHOD_CONTRACT;}

    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_INTOLERANT;
        SetupThread();
        pThis->LoadDomainModule(pAssembly, pFile, targetLevel);
    }
};
#endif // CROSSGEN_COMPILE

DomainModule *AppDomain::LoadDomainModule(DomainAssembly *pAssembly, PEModule *pFile,
                                          FileLoadLevel targetLevel)
{
    CONTRACT(DomainModule *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pFile));
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->GetLoadLevel() >= GetThreadFileLoadLevel()
                      || RETVAL->GetLoadLevel() >= targetLevel);
        POSTCONDITION(RETVAL->CheckNoError(targetLevel));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    GCX_PREEMP();

#ifndef CROSSGEN_COMPILE
    // Thread stress
    LoadDomainModuleStress ts (this, pAssembly, pFile, targetLevel);
#endif

    // Check for existing fully loaded assembly
    DomainModule *result = pAssembly->FindModule(pFile);
    if (result == NULL)
    {
        LoadLockHolder lock(this);

        // Check again in case we were racing
        result = pAssembly->FindModule(pFile);
        if (result == NULL)
        {
            // Find the list lock entry
            FileLoadLock *fileLock = (FileLoadLock *) lock->FindFileLock(pFile);
            if (fileLock == NULL)
            {
                // We are the first one in - create the DomainModule
                NewHolder<DomainModule> pDomainModule(new DomainModule(this, pAssembly, pFile));
                fileLock = FileLoadLock::Create(lock, pFile, pDomainModule);
                pDomainModule.SuppressRelease();
            }
            else
                fileLock->AddRef();

            lock.Release();

            // We pass our ref on fileLock to LoadDomainFile to release.

            // Note that if we throw here, we will poison fileLock with an error condition,
            // so it will not be removed until app domain unload.  So there is no need
            // to release our ref count.

            result = (DomainModule *) LoadDomainFile(fileLock, targetLevel);
        }
        else
        {
            lock.Release();
            result->EnsureLoadLevel(targetLevel);
        }

    }
    else
        result->EnsureLoadLevel(targetLevel);

    // Malformed metadata may contain an Assembly reference to what is actually
    // a Module. In this case we need to throw an exception, since returning a
    // DomainAssembly as a DomainModule is a type safety violation.
    if (result->IsAssembly())
    {
        ThrowHR(COR_E_ASSEMBLY_NOT_EXPECTED);
    }

    RETURN result;
}
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES

struct LoadFileArgs
{
    FileLoadLock *pLock;
    FileLoadLevel targetLevel;
    DomainFile *result;
};

#ifndef CROSSGEN_COMPILE
static void LoadDomainFile_Wrapper(void *ptr)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_INTOLERANT;
    GCX_PREEMP();
    LoadFileArgs *args = (LoadFileArgs *) ptr;
    args->result = GetAppDomain()->LoadDomainFile(args->pLock, args->targetLevel);
}
#endif // !CROSSGEN_COMPILE

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


    if(!CanLoadCode())
        COMPlusThrow(kAppDomainUnloadedException);

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

#ifndef CROSSGEN_COMPILE
    // Make sure we are in the right domain.  Many of the load operations require the target domain
    // to be the current app domain, most notably anything involving managed code or managed object
    // creation.
    if (this != GetAppDomain()
        && (!pFile->GetFile()->IsSystem() || targetLevel > FILE_LOAD_ALLOCATE))
    {
        // Transition to the correct app domain and perform the load there.
        GCX_COOP();

        // we will release the lock in the other app domain
        lockRef.SuppressRelease();

        if(!CanLoadCode() || GetDefaultContext() ==NULL)
            COMPlusThrow(kAppDomainUnloadedException);
        LoadFileArgs args = {pLock, targetLevel, NULL};
        GetThread()->DoADCallBack(this, LoadDomainFile_Wrapper, (void *) &args, ADV_CREATING);

        RETURN args.result;
    }
#endif // CROSSGEN_COMPILE

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
                TESTHOOKCALL(CompletedFileLoadLevel(GetId().m_dwId,pFile,workLevel));
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
#ifndef FEATURE_CORECLR
        // Event Tracing for Windows is used to log data for performance and functional testing purposes.
        // The events below are used to measure the performance of two steps in the assembly loader, namely assembly initialization and delivering events.
        StackSString ETWAssemblySimpleName;
        if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD))
        {
            LPCUTF8 simpleName = pFile->GetSimpleName();
            ETWAssemblySimpleName.AppendUTF8(simpleName ? simpleName : "NULL"); // Gather data used by ETW events later in this function.
        }
#endif // FEATURE_CORECLR

        // Special case: for LoadLibrary, we cannot hold the lock during the
        // actual LoadLibrary call, because we might get a callback from _CorDllMain on any
        // other thread.  (Note that this requires DomainFile's LoadLibrary to be independently threadsafe.)

        if (workLevel == FILE_LOAD_LOADLIBRARY)
        {
            lockHolder.Release();
            released = TRUE;
        }
#ifndef FEATURE_CORECLR
        else if (workLevel == FILE_LOAD_DELIVER_EVENTS)
        {
            FireEtwLoaderDeliverEventsPhaseStart(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, NULL, ETWAssemblySimpleName, GetClrInstanceId());
        }
#endif // FEATURE_CORECLR

        // Do the work
        TESTHOOKCALL(NextFileLoadLevel(GetId().m_dwId,pFile,workLevel));
#ifndef FEATURE_CORECLR
        if (workLevel == FILE_LOAD_ALLOCATE)
        {
            FireEtwLoaderAssemblyInitPhaseStart(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, NULL, ETWAssemblySimpleName, GetClrInstanceId());
        }                                                                             
#endif // FEATURE_CORECLR
        BOOL success = pFile->DoIncrementalLoad(workLevel);
#ifndef FEATURE_CORECLR
        if (workLevel == FILE_LOAD_ALLOCATE)
        {
            FireEtwLoaderAssemblyInitPhaseEnd(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, NULL, ETWAssemblySimpleName, GetClrInstanceId());
       }
#endif // FEATURE_CORECLR
        TESTHOOKCALL(CompletingFileLoadLevel(GetId().m_dwId,pFile,workLevel));
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
#ifndef FEATURE_CORECLR
                FireEtwLoaderDeliverEventsPhaseEnd(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, NULL, ETWAssemblySimpleName, GetClrInstanceId());
#endif // FEATURE_CORECLR
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

    if (pModule->FindDomainFile(this) != NULL)
        CHECK_OK;

    CCHECK_START
    {
        Assembly * pAssembly = pModule->GetAssembly();

        CCHECK(pAssembly->IsDomainNeutral());
#ifdef FEATURE_LOADER_OPTIMIZATION        
        Assembly * pSharedAssembly = NULL;
        _ASSERTE(this == ::GetAppDomain());
        {
            SharedAssemblyLocator locator(pAssembly->GetManifestFile());
            pSharedAssembly = SharedDomain::GetDomain()->FindShareableAssembly(&locator);
        }

        CCHECK(pAssembly == pSharedAssembly);
#endif         
    }
    CCHECK_END;

    CHECK_OK;
}

#ifdef FEATURE_LOADER_OPTIMIZATION
// Loads an existing Module into an AppDomain
// WARNING: this can only be done in a very limited scenario - the Module must be an unloaded domain neutral
// dependency in the app domain in question.  Normal code should not call this!
DomainFile *AppDomain::LoadDomainNeutralModuleDependency(Module *pModule, FileLoadLevel targetLevel)
{
    CONTRACT(DomainFile *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(::GetAppDomain()==this);
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckValidModule(pModule));
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->GetModule() == pModule);
    }
    CONTRACT_END;

    DomainFile *pDomainFile = pModule->FindDomainFile(this);

    STRESS_LOG3(LF_CLASSLOADER, LL_INFO100,"LDNMD: DomainFile %p for module %p in AppDomain %i\n",pDomainFile,pModule,GetId().m_dwId);

    if (pDomainFile == NULL)
    {
        GCX_PREEMP();

        Assembly *pAssembly = pModule->GetAssembly();

        DomainAssembly *pDomainAssembly = pAssembly->FindDomainAssembly(this);
        if (pDomainAssembly == NULL)
        {
            AssemblySpec spec(this);
            spec.InitializeSpec(pAssembly->GetManifestFile());

            pDomainAssembly = spec.LoadDomainAssembly(targetLevel);
        }
        else
        {
            //if the domain assembly already exists, we need to load it to the target level
            pDomainAssembly->EnsureLoadLevel (targetLevel);
        }

        if(pAssembly != pDomainAssembly->GetAssembly())
        {
            ThrowHR(SECURITY_E_INCOMPATIBLE_SHARE);
        }

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES        
        if (pModule == pAssembly->GetManifestModule())
            pDomainFile = pDomainAssembly;
        else
        {
            pDomainFile = LoadDomainModule(pDomainAssembly, (PEModule*) pModule->GetFile(), targetLevel);
            STRESS_LOG4(LF_CLASSLOADER, LL_INFO100,"LDNMD:  DF: for %p[%p/%p] is %p",
                        pModule,pDomainAssembly,pModule->GetFile(),pDomainFile);
        }
#else
        _ASSERTE (pModule == pAssembly->GetManifestModule());
        pDomainFile = pDomainAssembly;
#endif
    }
    else
    {
        // If the DomainFile already exists, we need to load it to the target level.
        pDomainFile->EnsureLoadLevel (targetLevel);
    }

    RETURN pDomainFile;
}

void AppDomain::SetSharePolicy(SharePolicy policy)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if ((int)policy > SHARE_POLICY_COUNT)
        COMPlusThrow(kArgumentException,W("Argument_InvalidValue"));

    // We cannot make all code domain neutral and still provide complete compatibility with regard
    // to using custom security policy and assembly evidence.
    //
    // In particular, if you try to do either of the above AFTER loading a domain neutral assembly
    // out of the GAC, we will now throw an exception.  The remedy would be to either not use SHARE_POLICY_ALWAYS
    // (change LoaderOptimizationMultiDomain to LoaderOptimizationMultiDomainHost), or change the loading order
    // in the app domain to do the policy set or evidence load earlier (which BTW will have the effect of
    // automatically using MDH rather than MD, for the same result.)
    //
    // We include a compatibility flag here to preserve old functionality if necessary - this has the effect
    // of never using SHARE_POLICY_ALWAYS.
    if (policy == SHARE_POLICY_ALWAYS &&
        (HasSetSecurityPolicy()
         || GetCompatibilityFlag(compatOnlyGACDomainNeutral)))
    {
        // Never share assemblies not in the GAC
        policy = SHARE_POLICY_GAC;
    }

    if (policy != m_SharePolicy)
    {

#ifdef FEATURE_PREJIT

#ifdef FEATURE_FUSION
        GCX_PREEMP();

        // Update the native image config flags
        FusionBind::SetApplicationContextDWORDProperty(m_pFusionContext, ACTAG_ZAP_CONFIG_FLAGS,
                                                       PEFile::GetNativeImageConfigFlags());
#endif //FEATURE_FUSION

#endif // FEATURE_PREJIT

        m_SharePolicy = policy;
    }

    return;
}

#ifdef FEATURE_FUSION
BOOL AppDomain::ReduceSharePolicyFromAlways()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // We may have already committed to always sharing - this is the case if
    // we have already loaded non-GAC-bound assemblies as domain neutral.

    if (GetSharePolicy() == SHARE_POLICY_ALWAYS)
    {
        AppDomain::AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeLoading | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

        // If we have loaded any non-GAC assemblies, we cannot set app domain policy as we have
        // already committed to the process-wide policy.

        while (i.Next(pDomainAssembly.This()))
        {
            if (pDomainAssembly->GetAssembly() && 
                pDomainAssembly->GetAssembly()->IsDomainNeutral() &&
                !pDomainAssembly->IsClosedInGAC())
            {
                // This assembly has been loaded domain neutral because of SHARE_POLICY_ALWAYS. We
                // can't reverse that decision now, so we have to fail the sharing policy change.
                return FALSE;
            }
        }

        // We haven't loaded any non-GAC assemblies yet - scale back to SHARE_POLICY_GAC so
        // future non-GAC assemblies won't be loaded as domain neutral.
        SetSharePolicy(SHARE_POLICY_GAC);
    }

    return TRUE;
}
#endif // FEATURE_FUSION

AppDomain::SharePolicy AppDomain::GetSharePolicy()
{
    LIMITED_METHOD_CONTRACT;
    // If the policy has been explicitly set for
    // the domain, use that.
    SharePolicy policy = m_SharePolicy;

    // Pick up the a specified config policy
    if (policy == SHARE_POLICY_UNSPECIFIED)
        policy = (SharePolicy) g_pConfig->DefaultSharePolicy();

    // Next, honor a host's request for global policy.
    if (policy == SHARE_POLICY_UNSPECIFIED)
        policy = (SharePolicy) g_dwGlobalSharePolicy;

    // If all else fails, use the hardwired default policy.
    if (policy == SHARE_POLICY_UNSPECIFIED)
        policy = SHARE_POLICY_DEFAULT;

    return policy;
}
#endif // FEATURE_LOADER_OPTIMIZATION


#ifdef FEATURE_CORECLR
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
        AllocMemTracker amTracker;
        AllocMemTracker *pamTracker = &amTracker;

        NativeImageDependenciesEntry * pNewEntry = 
            new (pamTracker->Track(GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(NativeImageDependenciesEntry)))))
                NativeImageDependenciesEntry();

        pNewEntry->m_AssemblySpec.CopyFrom(pSpec);
        pNewEntry->m_AssemblySpec.CloneFieldsToLoaderHeap(AssemblySpec::ALL_OWNED, GetLowFrequencyHeap(), pamTracker);

        pNewEntry->m_guidMVID = *pGuid;

        m_NativeImageDependencies.Add(pNewEntry);
        amTracker.SuppressRelease();
    }
}
#endif // FEATURE_CORECLR


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

    static OBJECTHANDLE hSharedStaticsHandle = NULL;

    if (hSharedStaticsHandle == NULL) {
        // Note that there is no race here since the default domain is always set up first
        _ASSERTE(IsDefaultDomain());

        MethodTable *pMT = MscorlibBinder::GetClass(CLASS__SHARED_STATICS);
        _ASSERTE(pMT->IsClassPreInited());

        hSharedStaticsHandle = CreateGlobalHandle(AllocateObject(pMT));
    }

    DomainLocalModule *pLocalModule;

    if (IsSingleAppDomain())
    {
        pLocalModule = MscorlibBinder::GetModule()->GetDomainLocalModule();
    }
    else
    {
        pLocalModule = GetDomainLocalBlock()->GetModuleSlot(
            MscorlibBinder::GetModule()->GetModuleIndex());
    }

    FieldDesc *pFD = MscorlibBinder::GetField(FIELD__SHARED_STATICS__SHARED_STATICS);

    OBJECTREF* pHandle = (OBJECTREF*)
        ((TADDR)pLocalModule->GetPrecomputedGCStaticsBasePointer()+pFD->GetOffset());
    SetObjectReference( pHandle, ObjectFromHandle(hSharedStaticsHandle), this );

    // This is a convenient place to initialize String.Empty.
    // It is treated as intrinsic by the JIT as so the static constructor would never run.
    // Leaving it uninitialized would confuse debuggers.

    // String should not have any static constructors.
    _ASSERTE(g_pStringClass->IsClassPreInited());

    FieldDesc * pEmptyStringFD = MscorlibBinder::GetField(FIELD__STRING__EMPTY);
    OBJECTREF* pEmptyStringHandle = (OBJECTREF*)
        ((TADDR)pLocalModule->GetPrecomputedGCStaticsBasePointer()+pEmptyStringFD->GetOffset());
    SetObjectReference( pEmptyStringHandle, StringObject::GetEmptyString(), this );
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
        (pFile->IsIntrospectionOnly() ? kIncludeIntrospection : kIncludeExecution)));
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

#ifdef FEATURE_MIXEDMODE
Module * AppDomain::GetIJWModule(HMODULE hMod)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    AssemblyIterator i = IterateAssembliesEx(STANDARD_IJW_ITERATOR_FLAGS);
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (i.Next(pDomainAssembly.This()))
    {
        _ASSERTE(!pDomainAssembly->IsCollectible());
        DomainFile * result = pDomainAssembly->FindIJWModule(hMod);

        if (result == NULL)
            continue;
        result->EnsureAllocated();
        return result->GetLoadedModule();
    }

    return NULL;
}

DomainFile * AppDomain::FindIJWDomainFile(HMODULE hMod, const SString & path)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    AssemblyIterator i = IterateAssembliesEx(STANDARD_IJW_ITERATOR_FLAGS);
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    
    while (i.Next(pDomainAssembly.This()))
    {
        _ASSERTE(!pDomainAssembly->IsCollectible());
        if (pDomainAssembly->GetCurrentAssembly() == NULL)
            continue;

        DomainFile * result = pDomainAssembly->GetCurrentAssembly()->FindIJWDomainFile(hMod, path);

        if (result != NULL)
            return result;
    }

    return NULL;
}
#endif //  FEATURE_MIXEDMODE

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
            if (IsDefaultDomain())
                tmpFriendlyName.Set(DEFAULT_DOMAIN_FRIENDLY_NAME);

            // This is for the profiler - if they call GetFriendlyName on an AppdomainCreateStarted
            // event, then we want to give them a temporary name they can use.
            else if (GetId().m_dwId != 0)
            {
                tmpFriendlyName.Clear();
                tmpFriendlyName.Printf(W("%s %d"), OTHER_DOMAIN_FRIENDLY_NAME_PREFIX, GetId().m_dwId);
            }
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

void AppDomain::ResetFriendlyName(BOOL fDebuggerCares/*=TRUE*/)
{
    WRAPPER_NO_CONTRACT;
    SetFriendlyName(NULL, fDebuggerCares);
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
    CONTRACT(LPWSTR)
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

void AppDomain::CacheStringsForDAC()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // If the application base, private bin paths, and configuration file are
    // available, cache them so DAC can read them out of memory
    //
#ifdef FEATURE_FUSION    
    if (m_pFusionContext)
    {
        CQuickBytes qb;
        LPWSTR ssz = (LPWSTR) qb.AllocThrows(MAX_URL_LENGTH * sizeof(WCHAR));

        DWORD dwSize;

        // application base
        ssz[0] = '\0';
        dwSize = MAX_URL_LENGTH * sizeof(WCHAR);
        m_pFusionContext->Get(ACTAG_APP_BASE_URL, ssz, &dwSize, 0);
        m_applicationBase.Set(ssz);

        // private bin paths
        ssz[0] = '\0';
        dwSize = MAX_URL_LENGTH * sizeof(WCHAR);
        m_pFusionContext->Get(ACTAG_APP_PRIVATE_BINPATH, ssz, &dwSize, 0);
        m_privateBinPaths.Set(ssz);

        // configuration file
        ssz[0] = '\0';
        dwSize = MAX_URL_LENGTH * sizeof(WCHAR);
        m_pFusionContext->Get(ACTAG_APP_CONFIG_FILE, ssz, &dwSize, 0);
        m_configFile.Set(ssz);
    }
#endif // FEATURE_FUSION    
}

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
#ifdef FEATURE_FUSION
    // check for context propagation
    if (bRetVal && pSpec->GetParentLoadContext() == LOADCTX_TYPE_LOADFROM && pAssembly->GetFile()->GetLoadContext() == LOADCTX_TYPE_DEFAULT)
    {
        // LoadFrom propagation occurred, store it in a way reachable by Load() (the "post-policy" one)
        AssemblySpec loadSpec;
        loadSpec.CopyFrom(pSpec);
        loadSpec.SetParentAssembly(NULL);
        bRetVal = m_AssemblyCache.StoreAssembly(&loadSpec, pAssembly);
    }
#endif
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

void AppDomain::AddUnmanagedImageToCache(LPCWSTR libraryName, HMODULE hMod)
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


HMODULE AppDomain::FindUnmanagedImageInCache(LPCWSTR libraryName)
{
    CONTRACT(HMODULE)
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
    RETURN (HMODULE) m_UnmanagedCache.LookupEntry(&spec, 0);
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

#ifdef FEATURE_FUSION
    // Fusion policy could have been applied,
    // so failed assembly could be not exactly what we ordered

    IAssemblyName *pIPostPolicyName = pPrePolicySpec->GetNameAfterPolicy();

    // Get post-policy assembly name
    if (pIPostPolicyName != NULL)
    {
        pPostPolicySpec->InitializeSpec(pIPostPolicyName,
                                        NULL,
                                        pPrePolicySpec->IsIntrospectionOnly());
        pPrePolicySpec->ReleaseNameAfterPolicy();

        if (!pPostPolicySpec->CompareEx(pPrePolicySpec))
        {
            *ppFailedSpec = pPostPolicySpec;
        }
    }
#endif //FEATURE_FUSION

    PEAssemblyHolder result;

    if ((EEFileLoadException::GetFileLoadKind(hrBindResult) == kFileNotFoundException) ||
        (hrBindResult == FUSION_E_REF_DEF_MISMATCH) ||
        (hrBindResult == FUSION_E_INVALID_NAME))
    {
        result = TryResolveAssembly(*ppFailedSpec, FALSE /* fPreBind */);

        if (result != NULL && pPrePolicySpec->CanUseWithBindingCache() && result->CanUseWithBindingCache())
        {
            fFailure = FALSE;

            // Given the post-policy resolve event construction of the CLR binder,
            // chained managed resolve events can race with each other, therefore we do allow
            // the adding of the result to fail. Checking for already chached specs
            // is not an option as it would introduce another race window.
            // The binder does a re-fetch of the
            // orignal binding spec and therefore will not cause inconsistency here.
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
            IfFailRet(FString::Utf8_Unicode(szName, bIsAscii, wzBuffer, cchBuffer));
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
static HRESULT VerifyBindHelper(
    ICLRPrivAssembly *pPrivAssembly,
    IAssemblyName *pAssemblyName,
    PEAssembly *pPEAssembly)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    HRESULT hr = S_OK;
    // Create an ICLRPrivAssemblyInfo to call to ICLRPrivAssembly::VerifyBind
    NewHolder<PEAssemblyAsPrivAssemblyInfo> pPrivAssemblyInfoImpl = new PEAssemblyAsPrivAssemblyInfo(pPEAssembly);
    ReleaseHolder<ICLRPrivAssemblyInfo> pPrivAssemblyInfo;
    IfFailRet(pPrivAssemblyInfoImpl->QueryInterface(__uuidof(ICLRPrivAssemblyInfo), (LPVOID *)&pPrivAssemblyInfo));
    pPrivAssemblyInfoImpl.SuppressRelease();

    // Call VerifyBind to give the host a chance to reject the bind based on assembly image contents.
    IfFailRet(pPrivAssembly->VerifyBind(pAssemblyName, pPrivAssembly, pPrivAssemblyInfo));

    return hr;
}

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

#ifdef FEATURE_FUSION
    StackSString wszAssemblyName;

    if (fusion::logging::LoggingEnabled())
    {   // Don't perform computation if logging is not enabled.
        FusionBind::GetAssemblyNameDisplayName(pAssemblyName, wszAssemblyName, ASM_DISPLAYF_FULL);
    }

    // Fire ETW Start event.
    FireEtwBindingPhaseStart(
        GetId().m_dwId, LOADCTX_TYPE_HOSTED, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable,
        pSpec->m_wszCodeBase, wszAssemblyName.GetUnicode(), GetClrInstanceId());
#endif

    // The Fusion binder can throw (to preserve compat, since it will actually perform an assembly
    // load as part of it's bind), so we need to be careful here to catch any FileNotFoundException
    // objects if fThrowIfNotFound is false.
    ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;

    // We return HRESULTs here on failure instead of throwing as failures here are not necessarily indicative
    // of an actual application problem. Returning an error code is substantially faster than throwing, and
    // should be used when possible.
    IfFailRet(pBinder->BindAssemblyByName(pAssemblyName, &pPrivAssembly));

    IfFailRet(BindHostedPrivAssembly(nullptr, pPrivAssembly, pAssemblyName, ppAssembly));

#ifdef FEATURE_FUSION
    // Fire ETW End event.
    FireEtwBindingPhaseEnd(
        GetId().m_dwId, LOADCTX_TYPE_HOSTED, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable,
        pSpec->m_wszCodeBase, wszAssemblyName.GetUnicode(), GetClrInstanceId());

 #endif

    return S_OK;
}

//-----------------------------------------------------------------------------------------------------------------
HRESULT 
AppDomain::BindHostedPrivAssembly(
    PEAssembly *       pParentAssembly,
    ICLRPrivAssembly * pPrivAssembly, 
    IAssemblyName *    pAssemblyName, 
    PEAssembly **      ppAssembly, 
    BOOL               fIsIntrospectionOnly) // = FALSE
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
    {   // Already exists: ask the binder to verify and return the assembly.
        return VerifyBindHelper(pPrivAssembly, pAssemblyName, *ppAssembly);
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
    if (dwAvailableImages & ASSEMBLY_IMAGE_TYPE_NATIVE)
    {
        DWORD dwImageType;
        ReleaseHolder<ICLRPrivResource> pIResourceNI;

        IfFailRet(pPrivAssembly->GetImageResource(ASSEMBLY_IMAGE_TYPE_NATIVE, &dwImageType, &pIResourceNI));
        _ASSERTE(dwImageType == ASSEMBLY_IMAGE_TYPE_NATIVE || FAILED(hr));

        pPEImageNI = PEImage::OpenImage(pIResourceNI, MDInternalImport_TrustedNativeImage);
    }
    _ASSERTE(pPEImageIL != nullptr);
    
    // Create a PEAssembly using the IL and NI images.
    PEAssemblyHolder pPEAssembly = PEAssembly::Open(pParentAssembly, pPEImageIL, pPEImageNI, pPrivAssembly, fIsIntrospectionOnly);

#ifdef FEATURE_FUSION
    // Ensure that the assembly found can be loaded for execution in the process.
    if (!fIsIntrospectionOnly)
        IfFailRet(RuntimeIsValidAssemblyOnThisPlatform_CheckProcessorArchitecture(pPEAssembly->GetFusionProcessorArchitecture(), FALSE));
#endif

    // Ask the binder to verify.
    IfFailRet(VerifyBindHelper(pPrivAssembly, pAssemblyName, pPEAssembly));

    // The result.    
    *ppAssembly = pPEAssembly.Extract();

    return S_OK;
} // AppDomain::BindHostedPrivAssembly

//---------------------------------------------------------------------------------------------------------------------
PEAssembly * AppDomain::BindAssemblySpec(
    AssemblySpec *         pSpec, 
    BOOL                   fThrowOnFileNotFound, 
    BOOL                   fRaisePrebindEvents, 
    StackCrawlMark *       pCallerStackMark, 
    AssemblyLoadSecurity * pLoadSecurity,
    BOOL                   fUseHostBinderIfAvailable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    PRECONDITION(CheckPointer(pSpec));
    PRECONDITION(pSpec->GetAppDomain() == this);
    PRECONDITION(this==::GetAppDomain());

    GCX_PREEMP();

    BOOL fForceReThrow = FALSE;

#if defined(FEATURE_APPX_BINDER)
    //
    // If there is a host binder available and this is an unparented bind within the
    // default load context, then the bind will be delegated to the domain-wide host
    // binder. If there is a parent assembly, then a bind will occur only if it has
    // an associated ICLRPrivAssembly to serve as the binder.
    //
    // fUseHostBinderIfAvailable can be false if this method is called by
    // CLRPrivBinderFusion::BindAssemblyByName, which explicitly indicates that it
    // wants to use the fusion binder.
    //

    if (AppX::IsAppXProcess() &&
        fUseHostBinderIfAvailable &&
        (
         ( pSpec->HasParentAssembly()
           ? // Parent assembly is hosted
             pSpec->GetParentAssembly()->GetFile()->HasHostAssembly()
           : // Non-parented default context bind
             ( HasLoadContextHostBinder() &&
               !pSpec->IsIntrospectionOnly() 
             )
         ) ||
         (pSpec->GetHostBinder() != nullptr)
         )
       )
    {
        HRESULT hr = S_OK;

        if (pSpec->GetCodeBase() != nullptr)
        {   // LoadFrom is not supported in AppX (we should never even get here)
            IfFailThrow(E_INVALIDARG);
        }
        
        // Get the assembly display name.
        ReleaseHolder<IAssemblyName> pAssemblyName;
        IfFailThrow(pSpec->CreateFusionName(&pAssemblyName, TRUE, TRUE));
        
        // Create new binding scope for fusion logging.
        fusion::logging::BindingScope defaultScope(pAssemblyName, FUSION_BIND_LOG_CATEGORY_DEFAULT);
        
        PEAssemblyHolder pAssembly;
        EX_TRY
        {
            // If there is a specified binder, then it is used.
            // Otherwise if there exist a parent assembly, then it provides the binding context
            // Otherwise the domain's root-level binder is used.
            ICLRPrivBinder * pBinder = nullptr;

            if (pSpec->GetHostBinder() != nullptr)
            {
                pBinder = pSpec->GetHostBinder();
            }
            else
            {
                PEAssembly * pParentAssembly =
                    (pSpec->GetParentAssembly() == nullptr) ? nullptr : pSpec->GetParentAssembly()->GetFile();

                if ((pParentAssembly != nullptr) && (pParentAssembly->HasHostAssembly()))
                {
                    BOOL fMustUseOriginalLoadContextBinder = FALSE;
                    if (pSpec->IsContentType_WindowsRuntime())
                    {
                        // Ugly, but we need to handle Framework assemblies that contain WinRT type references,
                        // and the Fusion binder won't resolve these in AppX processes. The shareable flag is currently
                        // a reasonable proxy for these cases. (It also catches first party WinMD files, but depedencies
                        // from those can also be resolved by the original load context binder).
                        // TODO! Update the fusion binder to resolve WinMD references correctly.
                        IfFailThrow(pParentAssembly->GetHostAssembly()->IsShareable(&fMustUseOriginalLoadContextBinder));
                    }

                    if (fMustUseOriginalLoadContextBinder)
                    {
                        pBinder = GetLoadContextHostBinder();
                    }
                    else
                    {
                        pBinder = pParentAssembly->GetHostAssembly();
                    }
                }
                else
                {
                    pBinder = GetCurrentLoadContextHostBinder();
                }
            }
            _ASSERTE(pBinder != nullptr);
            
            hr = BindAssemblySpecForHostedBinder(pSpec, pAssemblyName, pBinder, &pAssembly);
            if (FAILED(hr))
            {
                goto EndTry1;
            }
EndTry1:;
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
                EEFileLoadException::Throw(pSpec, fusion::logging::GetCurrentFusionBindLog(), hr);
            }
            if ((hr == CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT) && pSpec->IsContentType_WindowsRuntime())
            {   // Error returned e.g. for WinRT type name without namespace
                if (fThrowOnFileNotFound)
                {   // Throw ArgumentException (with the HRESULT) wrapped by TypeLoadException to give user type name for diagnostics
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

        _ASSERTE((pAssembly != nullptr) || (FAILED(hr) && !fThrowOnFileNotFound));
        return pAssembly.Extract();
    }
    else
#endif // FEATURE_APPX_BINDER
#if defined(FEATURE_COMINTEROP)
    // Handle WinRT assemblies in the classic/hybrid scenario. If this is an AppX process,
    // then this case will be handled by the previous block as part of the full set of
    // available binding hosts.
#ifndef FEATURE_APPX_BINDER
    if (pSpec->IsContentType_WindowsRuntime())
#else
    if (!AppX::IsAppXProcess() && pSpec->IsContentType_WindowsRuntime())
#endif
    {
        HRESULT hr = S_OK;

        // Get the assembly display name.
        ReleaseHolder<IAssemblyName> pAssemblyName;

        IfFailThrow(pSpec->CreateFusionName(&pAssemblyName, TRUE, TRUE));

#ifdef FEATURE_FUSION
        // Create new binding scope for fusion logging.
        fusion::logging::BindingScope defaultScope(pAssemblyName, FUSION_BIND_LOG_CATEGORY_DEFAULT);
#endif

        PEAssemblyHolder pAssembly;

        EX_TRY
        {
            hr = BindAssemblySpecForHostedBinder(pSpec, pAssemblyName, m_pWinRtBinder, &pAssembly);
            if (FAILED(hr))
                goto EndTry2; // Goto end of try block.
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
#ifdef FEATURE_FUSION
                EEFileLoadException::Throw(pSpec, fusion::logging::GetCurrentFusionBindLog(), hr);
#else
                EEFileLoadException::Throw(pSpec, hr);
#endif // FEATURE_FUSION
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
        
#if defined(FEATURE_COMINTEROP) && defined(FEATURE_REFLECTION_ONLY_LOAD)
        // We want to keep this holder around to avoid closing and remapping the file again - calls to Fusion further down will open the file again
        ReleaseHolder<IMetaDataAssemblyImport> pMetaDataAssemblyImport;
        
        // Special case ReflectionOnlyLoadFrom on .winmd (WinRT) assemblies
        if (pSpec->IsIntrospectionOnly() && (pSpec->m_wszCodeBase != NULL))
        {   // This is a LoadFrom request - we need to find out if it is .winmd file or classic managed assembly
            HRESULT hr = S_OK;
            
            StackSString sPath(pSpec->GetCodeBase());
            PEAssembly::UrlToPath(sPath);
            
            // Open MetaData of the file
            hr = GetAssemblyMDInternalImportEx(
                sPath, 
                IID_IMetaDataAssemblyImport, 
                MDInternalImport_Default, 
                (IUnknown **)&pMetaDataAssemblyImport);
            if (SUCCEEDED(hr))
            {
                DWORD dwAssemblyFlags = 0;
                hr = pMetaDataAssemblyImport->GetAssemblyProps(
                    TokenFromRid(1, mdtAssembly), 
                    nullptr,    // ppbPublicKey
                    nullptr,    // pcbPublicKey
                    nullptr,    // pulHashAlgId
                    nullptr,    // szName
                    0,          // cchName
                    nullptr,    // pchName
                    nullptr,    // pMetaData
                    &dwAssemblyFlags);
                if (SUCCEEDED(hr) && IsAfContentType_WindowsRuntime(dwAssemblyFlags))
                {   // It is .winmd file
                    _ASSERTE(!AppX::IsAppXProcess());
                    
                    ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;
                    ReleaseHolder<PEAssembly> pAssembly;

                    hr = m_pReflectionOnlyWinRtBinder->BindAssemblyExplicit(sPath, &pPrivAssembly);
                    if (SUCCEEDED(hr))
                    {
                        hr = BindHostedPrivAssembly(nullptr, pPrivAssembly, nullptr, &pAssembly, TRUE);
                        _ASSERTE(FAILED(hr) || (pAssembly != nullptr));
                    }
                    if (FAILED(hr))
                    {
                        if (fThrowOnFileNotFound)
                        {
                            ThrowHR(hr);
                        }
                        return nullptr;
                    }
                    return pAssembly.Extract();
                }
            }
        }
#endif //FEATURE_COMINTEROP && FEATURE_REFLECTION_ONLY_LOAD

        EX_TRY
        {
            if (!IsCached(pSpec))
            {

#ifdef FEATURE_FUSION
                if (fRaisePrebindEvents
                    && (result = TryResolveAssembly(pSpec, TRUE /*fPreBind*/)) != NULL
                    && result->CanUseWithBindingCache())
                {
                    // Failure to add simply means someone else beat us to it. In that case
                    // the FindCachedFile call below (after catch block) will update result
                    // to the cached value.
                    AddFileToCache(pSpec, result, TRUE /*fAllowFailure*/);
                }
                else
#endif
                {
                    bool fAddFileToCache = false;

                    BOOL fIsWellKnown = FALSE;

#ifdef FEATURE_FUSION
                    SafeComHolderPreemp<IAssembly> pIAssembly;
                    SafeComHolderPreemp<IBindResult> pNativeFusionAssembly;
                    SafeComHolderPreemp<IHostAssembly> pIHostAssembly;
                    SafeComHolderPreemp<IFusionBindLog> pFusionLog;

                    // Event Tracing for Windows is used to log data for performance and functional testing purposes.
                    // The events below are used to measure the performance of assembly binding as a whole.
                    FireEtwBindingPhaseStart(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, pSpec->m_wszCodeBase, NULL, GetClrInstanceId());
                    fIsWellKnown = pSpec->FindAssemblyFile(this,
                                                           fThrowOnFileNotFound,
                                                           &pIAssembly,
                                                           &pIHostAssembly,
                                                           &pNativeFusionAssembly,
                                                           &pFusionLog,
                                                           &hrBindResult,
                                                           pCallerStackMark,
                                                           pLoadSecurity);
                    FireEtwBindingPhaseEnd(GetId().m_dwId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, pSpec->m_wszCodeBase, NULL, GetClrInstanceId());
                    if (pIAssembly || pIHostAssembly)
                    {

                        if (fIsWellKnown &&
                            m_pRootAssembly &&
                            pIAssembly == m_pRootAssembly->GetFusionAssembly())
                        {
                            // This is a shortcut to avoid opening another copy of the process exe.
                            // In fact, we have other similar cases where we've called
                            // ExplicitBind() rather than normal binding, which aren't covered here.

                            // <TODO>@todo: It would be nice to populate the cache with those assemblies
                            // to avoid getting in this situation.</TODO>

                            result = m_pRootAssembly->GetManifestFile();
                            result.SuppressRelease(); // Didn't get a refcount
                        }
                        else
                        {
                            BOOL isSystemAssembly = pSpec->IsMscorlib(); // can use SystemDomain::m_pSystemAssembly 
                            BOOL isIntrospectionOnly = pSpec->IsIntrospectionOnly();
                            if (pIAssembly)
                                result = PEAssembly::Open(pIAssembly, pNativeFusionAssembly, pFusionLog,
                                                          isSystemAssembly, isIntrospectionOnly);
                            else
                                result = PEAssembly::Open(pIHostAssembly, isSystemAssembly,
                                                          isIntrospectionOnly);
                        }
                        fAddFileToCache = true;
                    }
                    else if (!fIsWellKnown)
                    {
                        // Trigger the resolve event also for non-throw situation.
                        // However, this code path will behave as if the resolve handler has thrown,
                        // that is, not trigger an MDA.
                        _ASSERTE(fThrowOnFileNotFound == FALSE);

                        AssemblySpec NewSpec(this);
                        AssemblySpec *pFailedSpec = NULL;

                        fForceReThrow = TRUE; // Managed resolve event handler can throw

                        // Purposly ignore return value
                        PostBindResolveAssembly(pSpec, &NewSpec, hrBindResult, &pFailedSpec);
                    }
#else //!FEATURE_FUSION
                    // Use CoreClr's fusion alternative
                    CoreBindResult bindResult;

                    pSpec->Bind(this, fThrowOnFileNotFound, &bindResult, FALSE /* fNgenExplicitBind */, FALSE /* fExplicitBindToNativeImage */, pCallerStackMark);
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
                                                      FALSE, pSpec->IsIntrospectionOnly());
                        }
                        fAddFileToCache = true;
                        
#if defined(FEATURE_CORECLR)                        
                        // Setup the reference to the binder, which performed the bind, into the AssemblySpec
                        ICLRPrivBinder* pBinder = result->GetBindingContext();
                        _ASSERTE(pBinder != NULL);
                        pSpec->SetBindingContext(pBinder);
#endif // defined(FEATURE_CORECLR)
                    }

#endif //!FEATURE_FUSION

                    if (fAddFileToCache)
                    {

#ifdef FEATURE_REFLECTION_ONLY_LOAD
                        // <TODO> PERF: This doesn't scale... </TODO>
                        if (pSpec->IsIntrospectionOnly() && (pSpec->GetCodeBase() != NULL))
                        {
                            IAssemblyName * pIAssemblyName = result->GetFusionAssemblyName();

                            AppDomain::AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
                                kIncludeLoaded | kIncludeIntrospection));
                            CollectibleAssemblyHolder<DomainAssembly *> pCachedDomainAssembly;
                            while (i.Next(pCachedDomainAssembly.This()))
                            {
                                IAssemblyName * pCachedAssemblyName = pCachedDomainAssembly->GetAssembly()->GetFusionAssemblyName();
                                if (pCachedAssemblyName->IsEqual(pIAssemblyName, ASM_CMPF_IL_ALL) == S_OK)
                                {
                                    if (!pCachedDomainAssembly->GetAssembly()->GetManifestModule()->GetFile()->Equals(result))
                                    {
                                        COMPlusThrow(kFileLoadException, IDS_EE_REFLECTIONONLY_LOADFROM, pSpec->GetCodeBase());
                                    }
                                }
                            }
                        }
#endif //FEATURE_REFLECTION_ONLY_LOAD

                        if (pSpec->CanUseWithBindingCache() && result->CanUseWithBindingCache())
                        {
                            // Failure to add simply means someone else beat us to it. In that case
                            // the FindCachedFile call below (after catch block) will update result
                            // to the cached value.
                            AddFileToCache(pSpec, result, TRUE /*fAllowFailure*/);
                        }
                    }
                    else if (!fIsWellKnown)
                    {
                        // Trigger the resolve event also for non-throw situation.
                        // However, this code path will behave as if the resolve handler has thrown,
                        // that is, not trigger an MDA.
                        _ASSERTE(fThrowOnFileNotFound == FALSE);

                        AssemblySpec NewSpec(this);
                        AssemblySpec *pFailedSpec = NULL;

                        fForceReThrow = TRUE; // Managed resolve event handler can throw

                        // Purposly ignore return value
                        PostBindResolveAssembly(pSpec, &NewSpec, hrBindResult, &pFailedSpec);
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
                // This is not executed for SO exceptions so we need to disable the backout
                // stack validation to prevent false violations from being reported.
                DISABLE_BACKOUT_STACK_VALIDATION;

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
                        //
                        // If the BindingFailure MDA is enabled, trigger one for this failure
                        // Note: TryResolveAssembly() can also throw if an AssemblyResolve event subscriber throws
                        //       and the MDA isn't sent in this case (or for transient failure cases)
                        //
#ifdef MDA_SUPPORTED
                        MdaBindingFailure* pProbe = MDA_GET_ASSISTANT(BindingFailure);
                        if (pProbe)
                        {
                            // Transition to cooperative GC mode before using any OBJECTREFs.
                            GCX_COOP();

                            OBJECTREF exceptionObj = GET_THROWABLE();
                            GCPROTECT_BEGIN(exceptionObj)
                            {
                                pProbe->BindFailed(pFailedSpec, &exceptionObj);
                            }
                            GCPROTECT_END();
                        }
#endif

                        // In the same cases as for the MDA, store the failure information for DAC to read
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


#ifdef FEATURE_REFLECTION_ONLY_LOAD
DomainAssembly * 
AppDomain::BindAssemblySpecForIntrospectionDependencies(
    AssemblySpec * pSpec)
{
    STANDARD_VM_CONTRACT;
    
    PRECONDITION(CheckPointer(pSpec));
    PRECONDITION(pSpec->GetAppDomain() == this);
    PRECONDITION(pSpec->IsIntrospectionOnly());
    PRECONDITION(this == ::GetAppDomain());
    
    PEAssemblyHolder result;
    HRESULT hr;
    
    if (!pSpec->HasUniqueIdentity())
    {
        if (!pSpec->HasBindableIdentity())
        {
            COMPlusThrowHR(E_UNEXPECTED);
        }
        
        // In classic (non-AppX), this is initilized by AppDomain constructor
        _ASSERTE(m_pReflectionOnlyWinRtBinder != NULL);
        
        ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;
        hr = m_pReflectionOnlyWinRtBinder->BindWinRtType(
            pSpec->GetWinRtTypeNamespace(), 
            pSpec->GetWinRtTypeClassName(), 
            pSpec->GetParentAssembly(), 
            &pPrivAssembly);
        if (FAILED(hr))
        {
            if (hr == CLR_E_BIND_TYPE_NOT_FOUND)
            {   // We could not find the type - throw TypeLoadException to give user type name for diagnostics
                EX_THROW(EETypeLoadException, (pSpec->GetWinRtTypeNamespace(), pSpec->GetWinRtTypeClassName(), nullptr, nullptr, IDS_EE_REFLECTIONONLY_WINRT_LOADFAILURE));
            }
            if (!Exception::IsTransient(hr))
            {   // Throw the HRESULT as exception wrapped by TypeLoadException to give user type name for diagnostics
                EEMessageException ex(hr);
                EX_THROW_WITH_INNER(EETypeLoadException, (pSpec->GetWinRtTypeNamespace(), pSpec->GetWinRtTypeClassName(), nullptr, nullptr, IDS_EE_REFLECTIONONLY_WINRT_LOADFAILURE), &ex);
            }
            IfFailThrow(hr);
        }
        
        IfFailThrow(BindHostedPrivAssembly(nullptr, pPrivAssembly, nullptr, &result, TRUE));
        _ASSERTE(result != nullptr);
        return LoadDomainAssembly(pSpec, result, FILE_LOADED);
    }
    
    EX_TRY
    {
        if (!IsCached(pSpec))
        {
            result = TryResolveAssembly(pSpec, TRUE /*fPreBind*/);
            if (result != NULL && result->CanUseWithBindingCache())
            {
                // Failure to add simply means someone else beat us to it. In that case
                // the FindCachedFile call below (after catch block) will update result
                // to the cached value.
                AddFileToCache(pSpec, result, TRUE /*fAllowFailure*/);
            }
        }
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();
        AssemblySpec NewSpec(this);
        AssemblySpec *pFailedSpec = NULL;

        // Let transient exceptions propagate
        if (ex->IsTransient())
        {
            EX_RETHROW;
        }

        // Non-"file not found" exception also propagate
        BOOL fFailure = PostBindResolveAssembly(pSpec, &NewSpec, ex->GetHR(), &pFailedSpec);
        if(fFailure)
        {
            if (AddExceptionToCache(pFailedSpec, ex))
            {
                if ((pFailedSpec == pSpec) && EEFileLoadException::CheckType(ex))
                {
                    EX_RETHROW; //preserve the information
                }
                else
                    EEFileLoadException::Throw(pFailedSpec, ex->GetHR(), ex);
            }
        }
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    result = FindCachedFile(pSpec);
    result.SuppressRelease();


    if (result)
    {
        // It was either already in the spec cache or the prebind event returned a result.
        return LoadDomainAssembly(pSpec, result, FILE_LOADED);
    }


    // Otherwise, look in the list of assemblies already loaded for reflectiononly.
    IAssemblyName * ptmp = NULL;
    hr = pSpec->CreateFusionName(&ptmp);
    if (FAILED(hr))
    {
        COMPlusThrowHR(hr);
    }
    SafeComHolder<IAssemblyName> pIAssemblyName(ptmp);

    // Note: We do not support introspection-only collectible assemblies (yet)
    AppDomain::AssemblyIterator i = IterateAssembliesEx((AssemblyIterationFlags)(
        kIncludeLoaded | kIncludeIntrospection | kExcludeCollectible));
    CollectibleAssemblyHolder<DomainAssembly *> pCachedDomainAssembly;
    
    while (i.Next(pCachedDomainAssembly.This()))
    {
        _ASSERTE(!pCachedDomainAssembly->IsCollectible());
        IAssemblyName * pCachedAssemblyName = pCachedDomainAssembly->GetAssembly()->GetFusionAssemblyName();
        if (pCachedAssemblyName->IsEqual(pIAssemblyName, ASM_CMPF_IL_ALL) == S_OK)
        {
            return pCachedDomainAssembly;
        }
    }
    // If not found in that list, it is an ERROR. Yes, this is by design.
    StackSString name;
    pSpec->GetFileOrDisplayName(0, name);
    COMPlusThrow(kFileLoadException, IDS_EE_REFLECTIONONLY_LOADFAILURE,name);
} // AppDomain::BindAssemblySpecForIntrospectionDependencies
#endif // FEATURE_REFLECTION_ONLY_LOAD

PEAssembly *AppDomain::TryResolveAssembly(AssemblySpec *pSpec, BOOL fPreBind)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = pSpec->ResolveAssemblyFile(this, fPreBind);
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

#ifdef FEATURE_FUSION
void AppDomain::GetFileFromFusion(IAssembly *pIAssembly, LPCWSTR wszModuleName,
                                  SString &path)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    SafeComHolder<IAssemblyModuleImport> pImport;
    IfFailThrow(pIAssembly->GetModuleByName(wszModuleName, &pImport));

    if (!pImport->IsAvailable()) {
        AssemblySink* pSink = AllocateAssemblySink(NULL);
        SafeComHolder<IAssemblyBindSink> sinkholder(pSink);
        SafeComHolder<IAssemblyModuleImport> pResult;

        IfFailThrow(FusionBind::RemoteLoadModule(GetFusionContext(),
                                                 pImport,
                                                 pSink,
                                                 &pResult));
        pResult->AddRef();
        pImport.Assign(pResult);
    }

    DWORD dwPath = 0;
    pImport->GetModulePath(NULL, &dwPath);

    LPWSTR buffer = path.OpenUnicodeBuffer(dwPath-1);
    IfFailThrow(pImport->GetModulePath(buffer, &dwPath));
    path.CloseBuffer();
}

PEAssembly *AppDomain::BindExplicitAssembly(HMODULE hMod, BOOL bindable)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(hMod));
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    SafeComHolder<IAssembly> pFusionAssembly;
    SafeComHolder<IBindResult> pNativeFusionAssembly;
    SafeComHolder<IFusionBindLog> pFusionLog;

    StackSString path;
    PEImage::GetPathFromDll(hMod, path);

    HRESULT hr = ExplicitBind(path, GetFusionContext(),
                              bindable ? EXPLICITBIND_FLAGS_EXE : EXPLICITBIND_FLAGS_NON_BINDABLE,
                              NULL, &pFusionAssembly, &pNativeFusionAssembly,&pFusionLog);
    if (FAILED(hr))
        EEFileLoadException::Throw(path, hr);

    RETURN PEAssembly::OpenHMODULE(hMod, pFusionAssembly,pNativeFusionAssembly, pFusionLog, FALSE);
}

Assembly *AppDomain::LoadExplicitAssembly(HMODULE hMod, BOOL bindable)
{
    CONTRACT(Assembly *)
    {
        PRECONDITION(CheckPointer(hMod));
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    PEAssemblyHolder pFile(BindExplicitAssembly(hMod, bindable));

    RETURN LoadAssembly(NULL, pFile, FILE_ACTIVE);
}
#endif // FEATURE_FUSION

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
        _ASSERTE (m_Stage == STAGE_CREATING || m_Stage == STAGE_CLOSED);
        ADID adid=GetId();
        delete this;
        TESTHOOKCALL(AppDomainDestroyed(adid.m_dwId));
    }
    return (cRef);
}

#ifdef FEATURE_FUSION
AssemblySink* AppDomain::AllocateAssemblySink(AssemblySpec* pSpec)
{
    CONTRACT(AssemblySink *)
    {
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    AssemblySink* ret = FastInterlockExchangePointer(&m_pAsyncPool, NULL);

    if(ret == NULL)
        ret = new AssemblySink(this);
    else
        ret->AddRef();
    ret->SetAssemblySpec(pSpec);
    RETURN ret;
}
#endif

AppDomain* AppDomain::s_pAppDomainToRaiseUnloadEvent;
BOOL AppDomain::s_fProcessUnloadDomainEvent = FALSE;

#ifndef CROSSGEN_COMPILE

void AppDomain::RaiseUnloadDomainEvent_Wrapper(LPVOID ptr)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    AppDomain* pDomain = (AppDomain *) ptr;
    pDomain->RaiseUnloadDomainEvent();
}

void AppDomain::ProcessUnloadDomainEventOnFinalizeThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    Thread *pThread = GetThread();
    _ASSERTE (pThread && IsFinalizerThread());

    // if we are not unloading domain now, do not process the event
    if (SystemDomain::AppDomainBeingUnloaded() == NULL)
    {
        s_pAppDomainToRaiseUnloadEvent->SetStage(STAGE_UNLOAD_REQUESTED);
        s_pAppDomainToRaiseUnloadEvent->EnableADUnloadWorker(
            s_pAppDomainToRaiseUnloadEvent->IsRudeUnload()?EEPolicy::ADU_Rude:EEPolicy::ADU_Safe);
        FastInterlockExchangePointer(&s_pAppDomainToRaiseUnloadEvent, NULL);
        return;
    }
    FastInterlockExchange((LONG*)&s_fProcessUnloadDomainEvent, TRUE);
    AppDomain::EnableADUnloadWorkerForFinalizer();
    pThread->SetThreadStateNC(Thread::TSNC_RaiseUnloadEvent);
    s_pAppDomainToRaiseUnloadEvent->RaiseUnloadDomainEvent();
    pThread->ResetThreadStateNC(Thread::TSNC_RaiseUnloadEvent);
    s_pAppDomainToRaiseUnloadEvent->EnableADUnloadWorker(
        s_pAppDomainToRaiseUnloadEvent->IsRudeUnload()?EEPolicy::ADU_Rude:EEPolicy::ADU_Safe);
    FastInterlockExchangePointer(&s_pAppDomainToRaiseUnloadEvent, NULL);
    FastInterlockExchange((LONG*)&s_fProcessUnloadDomainEvent, FALSE);

    if (pThread->IsAbortRequested())
    {
        pThread->UnmarkThreadForAbort(Thread::TAR_Thread);
    }
}

void AppDomain::RaiseUnloadDomainEvent()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    EX_TRY
    {
        Thread *pThread = GetThread();
        if (this != pThread->GetDomain())
        {
            pThread->DoADCallBack(this, AppDomain::RaiseUnloadDomainEvent_Wrapper, this,ADV_FINALIZER|ADV_COMPILATION);
        }
        else
        {
            struct _gc
            {
                APPDOMAINREF Domain;
                OBJECTREF    Delegate;
            } gc;
            ZeroMemory(&gc, sizeof(gc));

            GCPROTECT_BEGIN(gc);
            gc.Domain = (APPDOMAINREF) GetRawExposedObject();
            if (gc.Domain != NULL)
            {
                gc.Delegate = gc.Domain->m_pDomainUnloadEventHandler;
                if (gc.Delegate != NULL)
                    DistributeEventReliably(&gc.Delegate, (OBJECTREF *) &gc.Domain);
            }
            GCPROTECT_END();
        }
    }
    EX_CATCH
    {
        //@TODO call a MDA here
    }
    EX_END_CATCH(SwallowAllExceptions);
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

    GCX_COOP();
    FAULT_NOT_FATAL();
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    EX_TRY
    {
        struct _gc {
            APPDOMAINREF AppDomainRef;
            OBJECTREF    orThis;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        if ((gc.AppDomainRef = (APPDOMAINREF) GetRawExposedObject()) != NULL) {
            if (gc.AppDomainRef->m_pAssemblyEventHandler != NULL)
            {
                ARG_SLOT args[2];
                GCPROTECT_BEGIN(gc);

                gc.orThis = pAssembly->GetExposedAssemblyObject();

                MethodDescCallSite  onAssemblyLoad(METHOD__APP_DOMAIN__ON_ASSEMBLY_LOAD, &gc.orThis);

                // GetExposedAssemblyObject may cause a gc, so call this before filling args[0]
                args[1] = ObjToArgSlot(gc.orThis);
                args[0] = ObjToArgSlot(gc.AppDomainRef);

                onAssemblyLoad.Call(args);

                GCPROTECT_END();
            }
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

    BOOL retVal= FALSE;

    GCX_COOP();

    // The Everett behavior was to send the unhandled exception event only to the Default
    // AppDomain (since that's the only place that exceptions actually went unhandled).
    //
    // During Whidbey development, we broadcast the event to all AppDomains in the process.
    //
    // But the official shipping Whidbey behavior is that the unhandled exception event is
    // sent to the Default AppDomain and to whatever AppDomain the exception went unhandled
    // in.  To achieve this, we declare the exception to be unhandled *BEFORE* we marshal
    // it back to the Default AppDomain at the base of the Finalizer, threadpool and managed
    // threads.
    //
    // The rationale for sending the event to the Default AppDomain as well as the one the
    // exception went unhandled in is:
    //
    // 1)  This is compatible with the pre-Whidbey behavior, where only the Default AppDomain
    //     received the notification.
    //
    // 2)  This is convenient for hosts, which don't want to bother injecting listeners into
    //     every single AppDomain.

    AppDomain *pAppDomain = GetAppDomain();
    OBJECTREF orSender = 0;

    GCPROTECT_BEGIN(orSender);

    orSender = pAppDomain->GetRawExposedObject();

    retVal = pAppDomain->RaiseUnhandledExceptionEventNoThrow(&orSender, pThrowable, isTerminating);
#ifndef FEATURE_CORECLR    
// CoreCLR#520: 
// To make this work correctly we need the changes for coreclr 473
    if (pAppDomain != SystemDomain::System()->DefaultDomain())
        retVal |= SystemDomain::System()->DefaultDomain()->RaiseUnhandledExceptionEventNoThrow
                        (&orSender, pThrowable, isTerminating);
#endif    

    GCPROTECT_END();

    return retVal;
}


// Move outside of the AppDomain iteration, to avoid issues with the GC Frames being outside
// the domain transition.  This is a chronic issue that causes us to report roots for an AppDomain
// after we have left it.  This causes problems with AppDomain unloading that we only find
// with stress coverage..
void AppDomain::RaiseOneExitProcessEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct _gc
    {
        APPDOMAINREF Domain;
        OBJECTREF    Delegate;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    EX_TRY {

        GCPROTECT_BEGIN(gc);
        gc.Domain = (APPDOMAINREF) SystemDomain::GetCurrentDomain()->GetRawExposedObject();
        if (gc.Domain != NULL)
        {
            gc.Delegate = gc.Domain->m_pProcessExitEventHandler;
            if (gc.Delegate != NULL)
                DistributeEventReliably(&gc.Delegate, (OBJECTREF *) &gc.Domain);
        }
        GCPROTECT_END();

    } EX_CATCH {
    } EX_END_CATCH(SwallowAllExceptions);
}

// Local wrapper used in AppDomain::RaiseExitProcessEvent,
// introduced solely to avoid stack overflow because of _alloca in the loop.
// It's just factored out body of the loop, but it has to be a member method of AppDomain,
// because it calls private RaiseOneExitProcessEvent
/*static*/ void AppDomain::RaiseOneExitProcessEvent_Wrapper(AppDomainIterator* pi)
{

    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    EX_TRY {
        ENTER_DOMAIN_PTR(pi->GetDomain(),ADV_ITERATOR)
        AppDomain::RaiseOneExitProcessEvent();
        END_DOMAIN_TRANSITION;
    } EX_CATCH {
    } EX_END_CATCH(SwallowAllExceptions);
}

static LONG s_ProcessedExitProcessEventCount = 0;

LONG GetProcessedExitProcessEventCount()
{
    LIMITED_METHOD_CONTRACT;
    return s_ProcessedExitProcessEventCount;
}

void AppDomain::RaiseExitProcessEvent()
{
    if (!g_fEEStarted)
        return;

    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    // Only finalizer thread during shutdown can call this function.
    _ASSERTE ((g_fEEShutDown&ShutDown_Finalize1) && GetThread() == FinalizerThread::GetFinalizerThread());

    _ASSERTE (GetThread()->PreemptiveGCDisabled());

    _ASSERTE (GetThread()->GetDomain()->IsDefaultDomain());

    AppDomainIterator i(TRUE);
    while (i.Next())
    {
        RaiseOneExitProcessEvent_Wrapper(&i);
        FastInterlockIncrement(&s_ProcessedExitProcessEventCount);
    }
}

#ifndef FEATURE_CORECLR
void AppDomain::RaiseUnhandledExceptionEvent_Wrapper(LPVOID ptr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        SO_INTOLERANT;
    }
    CONTRACTL_END;
    AppDomain::RaiseUnhandled_Args *args = (AppDomain::RaiseUnhandled_Args *) ptr;

    struct _gc {
        OBJECTREF orThrowable;
        OBJECTREF orSender;
    } gc;

    ZeroMemory(&gc, sizeof(gc));

    _ASSERTE(args->pTargetDomain == GetAppDomain());
    GCPROTECT_BEGIN(gc);
    EX_TRY
    {
        SetObjectReference(&gc.orThrowable,
                           AppDomainHelper::CrossContextCopyFrom(args->pExceptionDomain,
                                                                 args->pThrowable),
                           args->pTargetDomain);

        SetObjectReference(&gc.orSender,
                           AppDomainHelper::CrossContextCopyFrom(args->pExceptionDomain,
                                                                 args->pSender),
                           args->pTargetDomain);
    }
    EX_CATCH
    {
        SetObjectReference(&gc.orThrowable, GET_THROWABLE(), args->pTargetDomain);
        SetObjectReference(&gc.orSender, GetAppDomain()->GetRawExposedObject(), args->pTargetDomain);
    }
    EX_END_CATCH(SwallowAllExceptions)
    *(args->pResult) = args->pTargetDomain->RaiseUnhandledExceptionEvent(&gc.orSender,
                                                                         &gc.orThrowable,
                                                                         args->isTerminating);
    GCPROTECT_END();

}
#endif //!FEATURE_CORECLR        

BOOL
AppDomain::RaiseUnhandledExceptionEventNoThrow(OBJECTREF *pSender, OBJECTREF *pThrowable, BOOL isTerminating)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    BOOL bRetVal=FALSE;

    EX_TRY
    {
        bRetVal = RaiseUnhandledExceptionEvent(pSender, pThrowable, isTerminating);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)  // Swallow any errors.
    return bRetVal;

}

BOOL
AppDomain::HasUnhandledExceptionEventHandler()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER; //essential
        NOTHROW;
    }
    CONTRACTL_END;
    if (!CanThreadEnter(GetThread()))
        return FALSE;
    if (GetRawExposedObject()==NULL)
        return FALSE;
    return (((APPDOMAINREF)GetRawExposedObject())->m_pUnhandledExceptionEventHandler!=NULL);
}

BOOL
AppDomain::RaiseUnhandledExceptionEvent(OBJECTREF *pSender, OBJECTREF *pThrowable, BOOL isTerminating)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!HasUnhandledExceptionEventHandler())
        return FALSE;

    BOOL result = FALSE;

    _ASSERTE(pThrowable != NULL && IsProtectedByGCFrame(pThrowable));
    _ASSERTE(pSender    != NULL && IsProtectedByGCFrame(pSender));

#ifndef FEATURE_CORECLR
    Thread *pThread = GetThread();
    if (this != pThread->GetDomain())
    {
        RaiseUnhandled_Args args = {pThread->GetDomain(), this, pSender, pThrowable, isTerminating, &result};
        // call through DoCallBack with a domain transition
        pThread->DoADCallBack(this, AppDomain::RaiseUnhandledExceptionEvent_Wrapper, &args, ADV_DEFAULTAD);
        return result;
    }
#else
    _ASSERTE(this == GetThread()->GetDomain());
#endif


    OBJECTREF orDelegate = NULL;

    GCPROTECT_BEGIN(orDelegate);

    APPDOMAINREF orAD = (APPDOMAINREF) GetAppDomain()->GetRawExposedObject();

    if (orAD != NULL)
    {
        orDelegate = orAD->m_pUnhandledExceptionEventHandler;
        if (orDelegate != NULL)
        {
            result = TRUE;
            DistributeUnhandledExceptionReliably(&orDelegate, pSender, pThrowable, isTerminating);
        }
    }
    GCPROTECT_END();
    return result;
}


#ifndef FEATURE_CORECLR
// Create a domain based on a string name
AppDomain* AppDomain::CreateDomainContext(LPCWSTR fileName)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if(fileName == NULL) return NULL;

    AppDomain* pDomain = NULL;

    MethodDescCallSite valCreateDomain(METHOD__APP_DOMAIN__VAL_CREATE_DOMAIN);

    STRINGREF pFilePath = NULL;
    GCPROTECT_BEGIN(pFilePath);
    pFilePath = StringObject::NewString(fileName);

    ARG_SLOT args[1] =
    {
        ObjToArgSlot(pFilePath),
    };

    APPDOMAINREF pDom = (APPDOMAINREF) valCreateDomain.Call_RetOBJECTREF(args);
    if(pDom != NULL)
    {
        Context* pContext = Context::GetExecutionContext(pDom);
        if(pContext)
        {
            pDomain = pContext->GetDomain();
        }
    }
    GCPROTECT_END();

    return pDomain;
}
#endif // !FEATURE_CORECLR

#endif // CROSSGEN_COMPILE

// You must be in the correct context before calling this
// routine. Therefore, it is only good for initializing the
// default domain.
void AppDomain::InitializeDomainContext(BOOL allowRedirects,
                                        LPCWSTR pwszPath,
                                        LPCWSTR pwszConfig)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (NingenEnabled())
    {
#ifdef FEATURE_FUSION   
        CreateFusionContext();
#endif // FEATURE_FUSION

#ifdef FEATURE_VERSIONING
        CreateFusionContext();
#endif // FEATURE_VERSIONING

        return;
    }

#ifndef CROSSGEN_COMPILE
    struct _gc {
        STRINGREF pFilePath;
        STRINGREF pConfig;
        OBJECTREF ref;
        PTRARRAYREF propertyNames;
        PTRARRAYREF propertyValues;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    if(pwszPath)
    {
        gc.pFilePath = StringObject::NewString(pwszPath);
    }

    if(pwszConfig)
    {
        gc.pConfig = StringObject::NewString(pwszConfig);
    }

#ifndef FEATURE_CORECLR
    StringArrayList *pPropertyNames;
    StringArrayList *pPropertyValues;
    CorHost2::GetDefaultAppDomainProperties(&pPropertyNames, &pPropertyValues);

    _ASSERTE(pPropertyNames->GetCount() == pPropertyValues->GetCount());

    if (pPropertyNames->GetCount() > 0)
    {
        gc.propertyNames = (PTRARRAYREF)AllocateObjectArray(pPropertyNames->GetCount(), g_pStringClass);
        gc.propertyValues = (PTRARRAYREF)AllocateObjectArray(pPropertyValues->GetCount(), g_pStringClass);

        for (DWORD i = 0; i < pPropertyNames->GetCount(); ++i)
        {
            STRINGREF propertyName = StringObject::NewString(pPropertyNames->Get(i));
            gc.propertyNames->SetAt(i, propertyName);

            STRINGREF propertyValue = StringObject::NewString(pPropertyValues->Get(i));
            gc.propertyValues->SetAt(i, propertyValue);
        }
    }
#endif // !FEATURE_CORECLR

    if ((gc.ref = GetExposedObject()) != NULL)
    {
        MethodDescCallSite setupDomain(METHOD__APP_DOMAIN__SETUP_DOMAIN);

        ARG_SLOT args[] =
        {
            ObjToArgSlot(gc.ref),
            BoolToArgSlot(allowRedirects),
            ObjToArgSlot(gc.pFilePath),
            ObjToArgSlot(gc.pConfig),
            ObjToArgSlot(gc.propertyNames),
            ObjToArgSlot(gc.propertyValues)
        };
        setupDomain.Call(args);
    }
    GCPROTECT_END();

    CacheStringsForDAC();
#endif // CROSSGEN_COMPILE
}

#ifdef FEATURE_FUSION

void AppDomain::SetupLoaderOptimization(DWORD optimization)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    if ((GetExposedObject()) != NULL)
    {
        MethodDescCallSite setupLoaderOptimization(METHOD__APP_DOMAIN__SETUP_LOADER_OPTIMIZATION);

        ARG_SLOT args[2] =
        {
            ObjToArgSlot(GetExposedObject()),
            optimization
        };
        setupLoaderOptimization.Call(args);
    }
}

// The fusion context should only be null when appdomain is being setup
// and there should be no reason to protect the creation.
IApplicationContext *AppDomain::CreateFusionContext()
{
    CONTRACT(IApplicationContext *)
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (m_pFusionContext == NULL)
    {
        ETWOnStartup (FusionAppCtx_V1, FusionAppCtxEnd_V1);

        GCX_PREEMP();

        SafeComHolderPreemp<IApplicationContext> pFusionContext;
        
        IfFailThrow(FusionBind::CreateFusionContext(NULL, &pFusionContext));
        
#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
        CLRPrivBinderWinRT * pWinRtBinder;
        if (AppX::IsAppXProcess())
        {   // Note: Fusion binder is used in AppX to bind .NET Fx assemblies - some of them depend on .winmd files (e.g. System.Runtime.WindowsRuntime.dll)
            CLRPrivBinderAppX * pAppXBinder = CLRPrivBinderAppX::GetOrCreateBinder();
            pWinRtBinder = pAppXBinder->GetWinRtBinder();
        }
        else
        {
            pWinRtBinder = m_pWinRtBinder;
        }
        _ASSERTE(pWinRtBinder != nullptr);
        
        IfFailThrow(SetApplicationContext_WinRTBinder(
            pFusionContext, 
            static_cast<IBindContext *>(pWinRtBinder)));
#endif

#ifdef FEATURE_PREJIT
        if (NGENImagesAllowed())
        {
            // Set the native image settings so fusion will bind native images
            SString zapString(g_pConfig->ZapSet());
            FusionBind::SetApplicationContextStringProperty(pFusionContext, ACTAG_ZAP_STRING, zapString);
            FusionBind::SetApplicationContextDWORDProperty(pFusionContext, ACTAG_ZAP_CONFIG_FLAGS,
                                                            PEFile::GetNativeImageConfigFlags());
        }
#endif // FEATURE_PREJIT

        pFusionContext.SuppressRelease();
        m_pFusionContext = pFusionContext;

        DWORD dwId = m_dwId.m_dwId;
        IfFailThrow(m_pFusionContext->Set(ACTAG_APP_DOMAIN_ID, &dwId, sizeof(DWORD), 0));

        if (HasLoadContextHostBinder())
            FusionBind::SetApplicationContextDWORDProperty(pFusionContext, ACTAG_FX_ONLY,1);

    }

    RETURN m_pFusionContext;
}

void AppDomain::TurnOnBindingRedirects()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;


    if ((GetExposedObject()) != NULL)
    {
        MethodDescCallSite turnOnBindingRedirects(METHOD__APP_DOMAIN__TURN_ON_BINDING_REDIRECTS);
        ARG_SLOT args[1] =
        {
            ObjToArgSlot(GetExposedObject()),
        };
        turnOnBindingRedirects.Call(args);
    }

    IfFailThrow(m_pFusionContext->Set(ACTAG_DISALLOW_APP_BINDING_REDIRECTS,
                                      NULL,
                                      0,
                                      0));
}

void AppDomain::SetupExecutableFusionContext(LPCWSTR exePath)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(GetAppDomain() == this);
    }
    CONTRACTL_END;

    GCX_COOP();

    struct _gc {
        STRINGREF pFilePath;
        OBJECTREF ref;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    gc.pFilePath = StringObject::NewString(exePath);

    if ((gc.ref = GetExposedObject()) != NULL)
    {
        MethodDescCallSite setDomainContext(METHOD__APP_DOMAIN__SET_DOMAIN_CONTEXT, &gc.ref);
        ARG_SLOT args[2] =
        {
            ObjToArgSlot(gc.ref),
            ObjToArgSlot(gc.pFilePath),
        };
        setDomainContext.Call(args);
    }

    GCPROTECT_END();

}

BOOL AppDomain::SetContextProperty(IApplicationContext* pFusionContext,
                                   LPCWSTR pProperty, OBJECTREF* obj)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (GetAppDomain()->HasLoadContextHostBinder())
        COMPlusThrow(kNotSupportedException);


    if(obj) {
        if ((*obj) != NULL){
            MethodTable* pMT = (*obj)->GetMethodTable();
            DWORD lgth;

            if(MscorlibBinder::IsClass(pMT, CLASS__STRING)) {

                lgth = (ObjectToSTRINGREF(*(StringObject**)obj))->GetStringLength();
                CQuickBytes qb;
                LPWSTR wszValue = (LPWSTR) qb.AllocThrows((lgth+1)*sizeof(WCHAR));
                memcpy(wszValue, (ObjectToSTRINGREF(*(StringObject**)obj))->GetBuffer(), lgth*sizeof(WCHAR));
                if(lgth > 0 && wszValue[lgth-1] == '/')
                    lgth--;
                wszValue[lgth] = W('\0');

                LOG((LF_LOADER,
                     LL_INFO10,
                     "Set: %S: *%S*.\n",
                     pProperty, wszValue));

                IfFailThrow(pFusionContext->Set(pProperty,
                                                wszValue,
                                                (lgth+1) * sizeof(WCHAR),
                                                0));
            }
            else {
                // Pin byte array for loading
                Wrapper<OBJECTHANDLE, DoNothing, DestroyPinningHandle> handle(
            GetAppDomain()->CreatePinningHandle(*obj));

                const BYTE *pbArray = ((U1ARRAYREF)(*obj))->GetDirectConstPointerToNonObjectElements();
                DWORD cbArray = (*obj)->GetNumComponents();

                IfFailThrow(pFusionContext->Set(pProperty,
                                                (LPVOID) pbArray,
                                                cbArray,
                                                0));
            }
        }
        else { // Un-set the property
            IfFailThrow(pFusionContext->Set(pProperty,
                                                NULL,
                                                0,
                                                0));
        }
    }

    return TRUE;
}
#endif // FEATURE_FUSION

#ifdef FEATURE_VERSIONING
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
        IfFailThrow(CCoreCLRBinderHelper::DefaultBinderSetupContext(GetId().m_dwId, &pTPABinder));
        m_pFusionContext = reinterpret_cast<IUnknown *>(pTPABinder);
        
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)  
        // By default, initial binding context setup for CoreCLR is also the TPABinding context
        (m_pTPABinderContext = pTPABinder)->AddRef();
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    }

    RETURN m_pFusionContext;
}
#endif // FEATURE_VERSIONING

#ifdef FEATURE_FUSION
LPWSTR AppDomain::GetDynamicDir()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pwDynamicDir == NULL) {

        BaseDomain::LockHolder lh(this);

        if(m_pwDynamicDir == NULL) {
            IApplicationContext* pFusionContext = GetFusionContext();
            _ASSERTE(pFusionContext);

            HRESULT hr = S_OK;
            DWORD dwSize = 0;
            hr = pFusionContext->GetDynamicDirectory(NULL, &dwSize);
            AllocMemHolder<WCHAR> tempDynamicDir;

            if(hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
                tempDynamicDir = GetLowFrequencyHeap()->AllocMem(S_SIZE_T(dwSize) * S_SIZE_T(sizeof(WCHAR)));
                hr = pFusionContext->GetDynamicDirectory(tempDynamicDir, &dwSize);
            }
            if(hr==HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
                return NULL;
            IfFailThrow(hr);

            tempDynamicDir.SuppressRelease();
            m_pwDynamicDir = tempDynamicDir;
        }
        // lh out of scope here
    }

    return m_pwDynamicDir;;
}
#endif //FEATURE_FUSION


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

    LOG((LF_CORDB, LL_INFO10, "AD::NDD domain [%d] %#08x %ls\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));

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
// U->M thunks created in this domain and not associated with a delegate.
UMEntryThunkCache *AppDomain::GetUMEntryThunkCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!m_pUMEntryThunkCache)
    {
        UMEntryThunkCache *pUMEntryThunkCache = new UMEntryThunkCache(this);

        if (FastInterlockCompareExchangePointer(&m_pUMEntryThunkCache, pUMEntryThunkCache, NULL) != NULL)
        {
            // some thread swooped in and set the field
            delete pUMEntryThunkCache;
        }
    }
    _ASSERTE(m_pUMEntryThunkCache);
    return m_pUMEntryThunkCache;
}

#ifdef FEATURE_COMINTEROP

ComCallWrapperCache *AppDomain::GetComCallWrapperCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (! m_pComCallWrapperCache)
    {
        BaseDomain::LockHolder lh(this);

        if (! m_pComCallWrapperCache)
            m_pComCallWrapperCache = ComCallWrapperCache::Create(this);
    }
    _ASSERTE(m_pComCallWrapperCache);
    return m_pComCallWrapperCache;
}

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

BOOL AppDomain::CanThreadEnter(Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    if (m_Stage < STAGE_EXITED)
        return TRUE;

    if (pThread == SystemDomain::System()->GetUnloadingThread())
        return m_Stage < STAGE_FINALIZING;
    if (pThread == FinalizerThread::GetFinalizerThread())
        return m_Stage < STAGE_FINALIZED;

    return FALSE;
}

void AppDomain::AllowThreadEntrance(AppDomain * pApp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pApp));
    }
    CONTRACTL_END;

    if (pApp->GetUnloadRequestThread() == NULL)
    {
        // This is asynchonous unload, either by a host, or by AppDomain.Unload from AD unload event.
        if (!pApp->IsUnloadingFromUnloadEvent())
        {
            pApp->SetStage(STAGE_UNLOAD_REQUESTED);
            pApp->EnableADUnloadWorker(
                 pApp->IsRudeUnload()?EEPolicy::ADU_Rude:EEPolicy::ADU_Safe);
            return;
        }
    }

    SystemDomain::LockHolder lh; // we don't want to reopen appdomain if other thread can be preparing to unload it

#ifdef FEATURE_COMINTEROP
    if (pApp->m_pComCallWrapperCache)
        pApp->m_pComCallWrapperCache->ResetDomainIsUnloading();
#endif // FEATURE_COMINTEROP

    pApp->SetStage(STAGE_OPEN);
}

void AppDomain::RestrictThreadEntrance(AppDomain * pApp)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        DISABLED(GC_TRIGGERS);
        MODE_ANY;
        DISABLED(FORBID_FAULT);
        PRECONDITION(CheckPointer(pApp));
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    // Set the flag on our CCW cache so stubs won't enter
    if (pApp->m_pComCallWrapperCache)
        pApp->m_pComCallWrapperCache->SetDomainIsUnloading();
#endif // FEATURE_COMINTEROP

    SystemDomain::LockHolder lh; // we don't want to reopen appdomain if other thread can be preparing to unload it
    // Release our ID so remoting and thread pool won't enter
    pApp->SetStage(STAGE_EXITED);
};

void AppDomain::Exit(BOOL fRunFinalizers, BOOL fAsyncExit)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Exiting domain [%d] %#08x %ls\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));

    RestrictEnterHolder RestrictEnter(this);

    {
        SystemDomain::LockHolder lh; // we don't want to close appdomain if other thread can be preparing to unload it
        SetStage(STAGE_EXITING);  // Note that we're trying to exit
    }

    // Raise the event indicating the domain is being unloaded.
    if (GetDefaultContext())
    {
        FastInterlockExchangePointer(&s_pAppDomainToRaiseUnloadEvent, this);

        DWORD timeout = GetEEPolicy()->GetTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload);
        //if (timeout == INFINITE)
        //{
        //    timeout = 20000; // 20 seconds
        //}
        DWORD timeoutForFinalizer = GetEEPolicy()->GetTimeout(OPR_FinalizerRun);
        ULONGLONG curTime = CLRGetTickCount64();
        ULONGLONG endTime = 0;
        if (timeout != INFINITE)
        {
            endTime = curTime + timeout;
            // We will try to kill AD unload event if it takes too long, and then we move on to the next registered caller.
            timeout /= 5;
        }

        while (s_pAppDomainToRaiseUnloadEvent != NULL)
        {
            FinalizerThread::FinalizerThreadWait(s_fProcessUnloadDomainEvent?timeout:timeoutForFinalizer);
            if (endTime != 0 && s_pAppDomainToRaiseUnloadEvent != NULL)
            {
                if (CLRGetTickCount64() >= endTime)
                {
                    SString sThreadId;
                    sThreadId.Printf(W("%x"), FinalizerThread::GetFinalizerThread()->GetThreadId());
                    COMPlusThrow(kCannotUnloadAppDomainException,
                                 IDS_EE_ADUNLOAD_CANT_UNWIND_THREAD,
                                 sThreadId);
                }
            }
        }
    }

    //
    // Set up blocks so no threads can enter except for the finalizer and the thread
    // doing the unload.
    //

    RestrictThreadEntrance(this);

    // Cause existing threads to abort out of this domain.  This should ensure all
    // normal threads are outside the domain, and we've already ensured that no new threads
    // can enter.

    PerAppDomainTPCountList::AppDomainUnloadingHolder tpAdUnloadHolder(GetTPIndex());


    if (!NingenEnabled())
    {
        UnwindThreads();
    }
    
    TESTHOOKCALL(UnwoundThreads(GetId().m_dwId)) ;    
    ProcessEventForHost(Event_DomainUnload, (PVOID)(UINT_PTR)GetId().m_dwId);

    RestrictEnter.SuppressRelease(); //after this point we don't guarantee appdomain consistency
#ifdef PROFILING_SUPPORTED
    // Signal profile if present.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AppDomainShutdownStarted((AppDomainID) this);
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED
    COUNTER_ONLY(GetPerfCounters().m_Loading.cAppDomains--);
    COUNTER_ONLY(GetPerfCounters().m_Loading.cAppDomainsUnloaded++);

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Domain [%d] %#08x %ls is exited.\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));

    ReJitManager::OnAppDomainExit(this);

    // Send ETW events for this domain's unload and potentially iterate through this
    // domain's modules & assemblies to send events for their unloads as well.  This
    // needs to occur before STAGE_FINALIZED (to ensure everything is there), so we do
    // this before any finalization occurs at all.
    ETW::LoaderLog::DomainUnload(this);

    //
    // Spin running finalizers until we flush them all.  We need to make multiple passes
    // in case the finalizers create more finalizable objects.  This is important to clear
    // the finalizable objects as roots, as well as to actually execute the finalizers. This
    // will only finalize instances instances of types that aren't potentially agile becuase we can't
    // risk finalizing agile objects. So we will be left with instances of potentially agile types
    // in handles or statics.
    //
    // <TODO>@todo: Need to ensure this will terminate in a reasonable amount of time.  Eventually
    // we should probably start passing FALSE for fRunFinalizers. Also I'm not sure we
    // guarantee that FinalizerThreadWait will ever terminate in general.</TODO>
    //

    SetStage(STAGE_FINALIZING);

    // Flush finalizers now.
    FinalizerThread::UnloadAppDomain(this, fRunFinalizers);
    
    DWORD timeout = GetEEPolicy()->GetTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload);
    ULONGLONG startTime = CLRGetTickCount64();
    ULONGLONG elapsedTime = 0;
    DWORD finalizerWait = 0;

    while (FinalizerThread::GetUnloadingAppDomain() != NULL)
    {

        if (timeout != INFINITE)
        {
            elapsedTime = CLRGetTickCount64() - startTime;
        }
        if (timeout > elapsedTime)
        {
            finalizerWait = timeout - static_cast<DWORD>(elapsedTime);
        }
        FinalizerThread::FinalizerThreadWait(finalizerWait); //will set stage to finalized
        if (timeout != INFINITE && FinalizerThread::GetUnloadingAppDomain() != NULL)
        {
            elapsedTime = CLRGetTickCount64() - startTime;
            if (timeout <= elapsedTime)
            {
                SetRudeUnload();
                // TODO: Consider escalation from RudeAppDomain
                timeout = INFINITE;
            }
        }
    }

    tpAdUnloadHolder.SuppressRelease();
    PerAppDomainTPCountList::ResetAppDomainTPCounts(GetTPIndex());

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Domain [%d] %#08x %ls is finalized.\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));


    AppDomainRefHolder This(this);
    AddRef();           // Hold a reference so CloseDomain won't delete us yet
    CloseDomain();      // Remove ourself from the list of app domains

    // This needs to be done prior to destroying the handle tables below.
    ReleaseDomainBoundInfo();

    //
    // It should be impossible to run non-mscorlib code in this domain now.
    // Cleanup all of our roots except the handles. We do this to allow as many
    // finalizers as possible to run correctly. If we delete the handles, they
    // can't run.
    //
    if (!NingenEnabled())
    {
#ifdef FEATURE_REMOTING
        EX_TRY
        {
            ADID domainId = GetId();
            MethodDescCallSite  domainUnloaded(METHOD__REMOTING_SERVICES__DOMAIN_UNLOADED);
    
            ARG_SLOT args[1];
            args[0] = domainId.m_dwId;
            domainUnloaded.Call(args);
        }
        EX_CATCH
        {
            //we don't care if it fails
        }
        EX_END_CATCH(SwallowAllExceptions);
#endif //  FEATURE_REMOTING
    }

    ClearGCRoots();
    ClearGCHandles();

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Domain [%d] %#08x %ls is cleared.\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));

    if (fAsyncExit && fRunFinalizers)
    {
        GCX_PREEMP();
        m_AssemblyCache.Clear();
        ClearFusionContext();
        ReleaseFiles();
        if (!NingenEnabled())
        {
            AddMemoryPressure();
        }
    }
    SystemDomain::System()->AddToDelayedUnloadList(this, fAsyncExit);
    SystemDomain::SetUnloadDomainCleared();
    if (m_dwId.m_dwId!=0)
        SystemDomain::ReleaseAppDomainId(m_dwId);
#ifdef PROFILING_SUPPORTED
    // Always signal profile if present, even when failed.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AppDomainShutdownFinished((AppDomainID) this, S_OK);
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

}

void AppDomain::Close()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Domain [%d] %#08x %ls is collected.\n",
         GetId().m_dwId, this, GetFriendlyNameForLogging()));


#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        // at this point shouldn't have any non-agile objects in the heap because we finalized all the non-agile ones.
        SyncBlockCache::GetSyncBlockCache()->CheckForUnloadedInstances(GetIndex());
#endif // CHECK_APP_DOMAIN_LEAKS
    {
        GCX_PREEMP();
        RemoveMemoryPressure();
    }
    _ASSERTE(m_cRef>0); //should be alive at this point otherwise iterator can revive us and crash
    {
        SystemDomain::LockHolder lh;    // Avoid races with AppDomainIterator
        SetStage(STAGE_CLOSED);
    }

    // CONSIDER: move releasing remoting cache from managed code to here.
}


void AppDomain::ResetUnloadRequestThread(ADID Id)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        PRECONDITION(!IsADUnloadHelperThread());
    }
    CONTRACTL_END;

    GCX_COOP();
    AppDomainFromIDHolder ad(Id, TRUE);
    if(!ad.IsUnloaded() && ad->m_Stage < STAGE_UNLOAD_REQUESTED)
    {
        Thread *pThread = ad->GetUnloadRequestThread();
        if(pThread==GetThread())
        {
            ad->m_dwThreadsStillInAppDomain=(ULONG)-1;

            if(pThread)
            {
                if (pThread->GetUnloadBoundaryFrame() && pThread->IsBeingAbortedForADUnload())
                {
                    pThread->UnmarkThreadForAbort(Thread::TAR_ADUnload);
                }
                ad->GetUnloadRequestThread()->ResetUnloadBoundaryFrame();
                pThread->ResetBeginAbortedForADUnload();
            }
            
            ad->SetUnloadRequestThread(NULL);
        }
    }
}


int g_fADUnloadWorkerOK = -1;

HRESULT AppDomain::UnloadById(ADID dwId, BOOL fSync,BOOL fExceptionsPassThrough)
{
    CONTRACTL
    {
        if(fExceptionsPassThrough) {THROWS;} else {NOTHROW;}
        MODE_ANY;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_TRIGGERS);}
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (dwId==(ADID)DefaultADID)
        return COR_E_CANNOTUNLOADAPPDOMAIN;

    Thread *pThread = GetThread();

    // Finalizer thread can not wait until AD unload is done,
    // because AD unload is going to wait for Finalizer Thread.
    if (fSync && pThread == FinalizerThread::GetFinalizerThread() && 
        !pThread->HasThreadStateNC(Thread::TSNC_RaiseUnloadEvent))
        return COR_E_CANNOTUNLOADAPPDOMAIN;


    // AD unload helper thread should have been created.
    _ASSERTE (g_fADUnloadWorkerOK == 1);

    _ASSERTE (!IsADUnloadHelperThread());

    BOOL fIsRaisingUnloadEvent = (pThread != NULL && pThread->HasThreadStateNC(Thread::TSNC_RaiseUnloadEvent));

    if (fIsRaisingUnloadEvent)
    {
        AppDomainFromIDHolder pApp(dwId, TRUE, AppDomainFromIDHolder::SyncType_GC);

        if (pApp.IsUnloaded() || ! pApp->CanLoadCode() || pApp->GetId().m_dwId == 0)
            return COR_E_APPDOMAINUNLOADED;

        pApp->EnableADUnloadWorker();

        return S_FALSE;
    }


    ADUnloadSinkHolder pSink;

    {
        SystemDomain::LockHolder ulh;

        AppDomainFromIDHolder pApp(dwId, TRUE, AppDomainFromIDHolder::SyncType_ADLock);

        if (pApp.IsUnloaded() || ! pApp->CanLoadCode() || pApp->GetId().m_dwId == 0)
            return COR_E_APPDOMAINUNLOADED;

        if (g_fADUnloadWorkerOK != 1)
        {
            _ASSERTE(FALSE);
            return E_UNEXPECTED;
        }

        if (!fSync)
        {
            pApp->EnableADUnloadWorker();
            return S_OK;
        }

        pSink = pApp->PrepareForWaitUnloadCompletion();

        pApp->EnableADUnloadWorker();

        // release the holders - we don't care anymore if the appdomain is gone
    }

#ifdef FEATURE_TESTHOOKS        
    if (fExceptionsPassThrough)
    {
        CONTRACT_VIOLATION(FaultViolation);
        return UnloadWaitNoCatch(dwId,pSink);
    }
#endif            

    return UnloadWait(dwId,pSink);
}

HRESULT AppDomain::UnloadWait(ADID Id, ADUnloadSink * pSink)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_TRIGGERS);}
    }
    CONTRACTL_END;
    
    HRESULT hr=S_OK;
    EX_TRY
    {
        // IF you ever try to change this to something not using events, please address the fact that
        // AppDomain::StopEEAndUnwindThreads relies on that events are used.

        pSink->WaitUnloadCompletion();
    }
    EX_CATCH_HRESULT(hr);

    if (SUCCEEDED(hr))
        hr=pSink->GetUnloadResult();

    if (FAILED(hr))
    {
        ResetUnloadRequestThread(Id);
    }
    return hr;
}

#ifdef FEATURE_TESTHOOKS        
HRESULT AppDomain::UnloadWaitNoCatch(ADID Id, ADUnloadSink * pSink)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;    

    Holder<ADID, DoNothing<ADID>, AppDomain::ResetUnloadRequestThread> resetUnloadHolder(Id);

    // IF you ever try to change this to something not using events, please address the fact that
    // AppDomain::StopEEAndUnwindThreads relies on that events are used.
    pSink->WaitUnloadCompletion();

    HRESULT hr = pSink->GetUnloadResult();

    if (SUCCEEDED(hr))
        resetUnloadHolder.SuppressRelease();

    return hr;
}
#endif

void AppDomain::Unload(BOOL fForceUnload)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef FEATURE_MULTICOREJIT

    // Avoid profiling file is partially written in ASP.net scenarios, call it earlier
    GetMulticoreJitManager().StopProfile(true);

#endif

    Thread *pThread = GetThread();


    if (! fForceUnload && !g_pConfig->AppDomainUnload())
        return;

    EPolicyAction action;
    EClrOperation operation;
    if (!IsRudeUnload())
    {
        operation = OPR_AppDomainUnload;
    }
    else
    {
        operation = OPR_AppDomainRudeUnload;
    }
    action = GetEEPolicy()->GetDefaultAction(operation,NULL);
    GetEEPolicy()->NotifyHostOnDefaultAction(operation,action);

    switch (action)
    {
    case eUnloadAppDomain:
        break;
    case eRudeUnloadAppDomain:
        SetRudeUnload();
        break;
    case eExitProcess:
    case eFastExitProcess:
    case eRudeExitProcess:
    case eDisableRuntime:
        EEPolicy::HandleExitProcessFromEscalation(action, HOST_E_EXITPROCESS_ADUNLOAD);
        _ASSERTE (!"Should not get here");
        break;
    default:
        break;
    }

#if (defined(_DEBUG) || defined(BREAK_ON_UNLOAD) || defined(AD_LOG_MEMORY) || defined(AD_SNAPSHOT))
    static int unloadCount = 0;
#endif

#ifdef AD_LOG_MEMORY
    {
        GCX_PREEMP();
        static int logMemory = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ADLogMemory);
        typedef void (__cdecl *LogItFcn) ( int );
        static LogItFcn pLogIt = NULL;

        if (logMemory && ! pLogIt)
        {
            HMODULE hMod = CLRLoadLibrary(W("mpdh.dll"));
            if (hMod)
            {
                pLogIt = (LogItFcn)GetProcAddress(hMod, "logIt");
                if (pLogIt)
                {
                    pLogIt(9999);
                    pLogIt(9999);
                }
            }
        }
    }
#endif // AD_LOG_MEMORY

    if (IsDefaultDomain() && !IsSingleAppDomain())
        COMPlusThrow(kCannotUnloadAppDomainException, IDS_EE_ADUNLOAD_DEFAULT);

    _ASSERTE(CanUnload());

    if (pThread == FinalizerThread::GetFinalizerThread() || GetUnloadRequestThread() == FinalizerThread::GetFinalizerThread())
        COMPlusThrow(kCannotUnloadAppDomainException, IDS_EE_ADUNLOAD_IN_FINALIZER);

    _ASSERTE(! SystemDomain::AppDomainBeingUnloaded());

    // should not be running in this AD because unload spawned thread in default domain
    if (!NingenEnabled())
    {
        _ASSERTE(!pThread->IsRunningIn(this, NULL));
    }


#ifdef APPDOMAIN_STATE
    _ASSERTE_ALL_BUILDS("clr/src/VM/AppDomain.cpp", pThread->GetDomain()->IsDefaultDomain());
#endif

    LOG((LF_APPDOMAIN | LF_CORDB, LL_INFO10, "AppDomain::Unloading domain [%d] %#08x %ls\n", GetId().m_dwId, this, GetFriendlyName()));

    STRESS_LOG3 (LF_APPDOMAIN, LL_INFO100, "Unload domain [%d, %d] %p\n", GetId().m_dwId, GetIndex().m_dwIndex, this);

    UnloadHolder hold(this);

    SystemDomain::System()->SetUnloadRequestingThread(GetUnloadRequestThread());
    SystemDomain::System()->SetUnloadingThread(pThread);


#ifdef _DEBUG
    static int dumpSB = -1;

    if (dumpSB == -1)
        dumpSB = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ADDumpSB);

    if (dumpSB > 1)
    {
        LogSpewAlways("Starting unload %3.3d\n", unloadCount);
        DumpSyncBlockCache();
    }
#endif // _DEBUG

    BOOL bForceGC=m_bForceGCOnUnload;

#ifdef AD_LOG_MEMORY
    if (pLogIt)
        bForceGC=TRUE;
#endif // AD_LOG_MEMORY

#ifdef AD_SNAPSHOT
    static int takeSnapShot = -1;

    if (takeSnapShot == -1)
        takeSnapShot = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ADTakeSnapShot);

    if (takeSnapShot)
        bForceGC=TRUE;
#endif // AD_SNAPSHOT

#ifdef _DEBUG
    if (dumpSB > 0)
        bForceGC=TRUE;
#endif // _DEBUG
    static int cfgForceGC = -1;

    if (cfgForceGC == -1)
        cfgForceGC =!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ADULazyMemoryRelease);

    bForceGC=bForceGC||cfgForceGC;
    AppDomainRefHolder This(this);
    AddRef();

    // Do the actual unloading
    {
        // We do not want other threads to abort the current one.
        ThreadPreventAsyncHolder preventAsync;
        Exit(TRUE, !bForceGC);
    }
    if(bForceGC)
    {
        GCHeap::GetGCHeap()->GarbageCollect();
        FinalizerThread::FinalizerThreadWait();
        SetStage(STAGE_COLLECTED);
        Close(); //NOTHROW!
    }

#ifdef AD_LOG_MEMORY
    if (pLogIt)
    {
        GCX_PREEMP();
        pLogIt(unloadCount);
    }
#endif // AD_LOG_MEMORY

#ifdef AD_SNAPSHOT
    if (takeSnapShot)
    {
        char buffer[1024];
        sprintf(buffer, "vadump -p %d -o > vadump.%d", GetCurrentProcessId(), unloadCount);
        system(buffer);
        sprintf(buffer, "umdh -p:%d -d -i:1 -f:umdh.%d", GetCurrentProcessId(), unloadCount);
        system(buffer);
        int takeDHSnapShot = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ADTakeDHSnapShot);
        if (takeDHSnapShot)
        {
            sprintf(buffer, "dh -p %d -s -g -h -b -f dh.%d", GetCurrentProcessId(), unloadCount);
            system(buffer);
        }
    }
#endif // AD_SNAPSHOT

#ifdef _DEBUG
    if (dumpSB > 0)
    {
        // do extra finalizer wait to remove any leftover sb entries
        FinalizerThread::FinalizerThreadWait();
        GCHeap::GetGCHeap()->GarbageCollect();
        FinalizerThread::FinalizerThreadWait();
        LogSpewAlways("Done unload %3.3d\n", unloadCount);
        DumpSyncBlockCache();
        ShutdownLogging();
        WCHAR buffer[128];
        swprintf_s(buffer, NumItems(buffer), W("DumpSB.%d"), unloadCount);
        _ASSERTE(WszMoveFileEx(W("COMPLUS.LOG"), buffer, MOVEFILE_REPLACE_EXISTING));
        // this will open a new file
        InitLogging();
    }
#endif // _DEBUG
}

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
#if _DEBUG_ADUNLOAD
    printf("%x AppDomain::ExceptionUnwind for %8.8p\n", GetThread()->GetThreadId(), pFrame);
#endif
    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    if (! pThread->ShouldChangeAbortToUnload(pFrame))
    {
        LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::ExceptionUnwind: not first transition or abort\n"));
        return;
    }

    LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::ExceptionUnwind: changing to unload\n"));

    GCX_COOP();
    OBJECTREF throwable = NULL;
    EEResourceException e(kAppDomainUnloadedException, W("Remoting_AppDomainUnloaded_ThreadUnwound"));
    throwable = e.GetThrowable();

    // reset the exception to an AppDomainUnloadedException
    if (throwable != NULL)
    {
        GetThread()->SafeSetThrowables(throwable);
    }
}

BOOL AppDomain::StopEEAndUnwindThreads(unsigned int retryCount, BOOL *pFMarkUnloadRequestThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    Thread *pThread = NULL;
    DWORD nThreadsNeedMoreWork=0;
    if (retryCount != (unsigned int)-1 && retryCount < g_pConfig->AppDomainUnloadRetryCount())
    {
        Thread *pCurThread = GetThread();
        if (pCurThread->CatchAtSafePoint())
            pCurThread->PulseGCMode();

        {
            // We know which thread is not in the domain now.  We just need to
            // work on those threads.  We do not need to suspend the runtime.
            ThreadStoreLockHolder tsl;

            while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
            {
                if (pThread == pCurThread)
                {
                    continue;
                }

                if (pThread == FinalizerThread::GetFinalizerThread())
                {
                    continue;
                }

                if (pThread->GetUnloadBoundaryFrame() == NULL)
                {
                    continue;
                }

                // A thread may have UnloadBoundaryFrame set if
                // 1. Being unloaded by AD unload helper thread
                // 2. Escalation from OOM or SO triggers AD unload
                // Here we only need to work on threads that are in the domain.  If we work on other threads,
                // those threads may be stucked in a finally, and we will not be able to escalate for them,
                // therefore AD unload is blocked.
                if (pThread->IsBeingAbortedForADUnload() ||
                    pThread == SystemDomain::System()->GetUnloadRequestingThread())
                {
                    nThreadsNeedMoreWork++;
                }

                if (!(IsRudeUnload() ||
                      (pThread != SystemDomain::System()->GetUnloadRequestingThread() || OnlyOneThreadLeft())))
                {
                    continue;
                }

                if ((pThread == SystemDomain::System()->GetUnloadRequestingThread()) && *pFMarkUnloadRequestThread)
                {
                    // Mark thread for abortion only once; later on interrupt only
                    *pFMarkUnloadRequestThread = FALSE;
                    pThread->SetAbortRequest(m_fRudeUnload? EEPolicy::TA_Rude : EEPolicy::TA_V1Compatible);
                }
                else
                {
                    if (pThread->m_State & Thread::TS_Interruptible)
                    {
                        pThread->UserInterrupt(Thread::TI_Abort);
                    }
                }

                if (pThread->PreemptiveGCDisabledOther())
                {
        #if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
                    Thread::SuspendThreadResult str = pThread->SuspendThread();
                    if (str == Thread::STR_Success)
                    {
                        if (pThread->PreemptiveGCDisabledOther() &&
                            (!pThread->IsAbortInitiated() || pThread->IsRudeAbort()))
                        {
                            pThread->HandleJITCaseForAbort();
                        }
                        pThread->ResumeThread();
                    }
        #endif
                }
            }
        } // ThreadStoreLockHolder

        if (nThreadsNeedMoreWork && CLRTaskHosted())
        {
            // In case a thread is the domain is blocked due to its scheduler being
            // occupied by another thread.
            Thread::ThreadAbortWatchDog();
        }
        m_dwThreadsStillInAppDomain=nThreadsNeedMoreWork;
        return !nThreadsNeedMoreWork;
    }

    // For now piggyback on the GC's suspend EE mechanism
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN);
#ifdef _DEBUG
    // <TODO>@todo: what to do with any threads that didn't stop?</TODO>
    _ASSERTE(ThreadStore::s_pThreadStore->DbgBackgroundThreadCount() > 0);
#endif // _DEBUG

    int totalADCount = 0;
    int finalizerADCount = 0;
    pThread = NULL;

    RuntimeExceptionKind reKind = kLastException;
    UINT resId = 0;
    SmallStackSString ssThreadId;

    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        // we already checked that we're not running in the unload domain
        if (pThread == GetThread())
        {
            continue;
        }

#ifdef _DEBUG
        void PrintStackTraceWithADToLog(Thread *pThread);
        if (LoggingOn(LF_APPDOMAIN, LL_INFO100)) {
            LOG((LF_APPDOMAIN, LL_INFO100, "\nStackTrace for %x\n", pThread->GetThreadId()));
            PrintStackTraceWithADToLog(pThread);
        }
#endif // _DEBUG
        int count = 0;
        Frame *pFrame = pThread->GetFirstTransitionInto(this, &count);
        if (! pFrame) {
            _ASSERTE(count == 0);
            if (pThread->IsBeingAbortedForADUnload())
            {
                pThread->ResetBeginAbortedForADUnload();
            }
            continue;
        }

        if (pThread != FinalizerThread::GetFinalizerThread())
        {
            totalADCount += count;
            nThreadsNeedMoreWork++;
            pThread->SetUnloadBoundaryFrame(pFrame);
        }
        else
        {
            finalizerADCount = count;
        }

        // don't setup the exception info for the unloading thread unless it's the last one in
        if (retryCount != ((unsigned int) -1) && retryCount > g_pConfig->AppDomainUnloadRetryCount() && reKind == kLastException &&
            (pThread != SystemDomain::System()->GetUnloadRequestingThread() || OnlyOneThreadLeft()))
        {
#ifdef AD_BREAK_ON_CANNOT_UNLOAD
            static int breakOnCannotUnload = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ADBreakOnCannotUnload);
            if (breakOnCannotUnload)
                _ASSERTE(!"Cannot unload AD");
#endif // AD_BREAK_ON_CANNOT_UNLOAD
            reKind = kCannotUnloadAppDomainException;
            resId = IDS_EE_ADUNLOAD_CANT_UNWIND_THREAD;
            ssThreadId.Printf(W("%x"), pThread->GetThreadId());
            STRESS_LOG2(LF_APPDOMAIN, LL_INFO10, "AppDomain::UnwindThreads cannot stop thread %x with %d transitions\n", pThread->GetThreadId(), count);
            // don't break out of this early or the assert totalADCount == (int)m_dwThreadEnterCount below will fire
            // it's better to chew a little extra time here and make sure our counts are consistent
        }
        // only abort the thread requesting the unload if it's the last one in, that way it will get
        // notification that the unload failed for some other thread not being aborted. And don't abort
        // the finalizer thread - let it finish it's work as it's allowed to be in there. If it won't finish,
        // then we will eventually get a CannotUnloadException on it.

        if (pThread != FinalizerThread::GetFinalizerThread() &&
            // If the domain is rudely unloaded, we will unwind the requesting thread out
            // Rude unload is going to succeed, or escalated to disable runtime or higher.
            (IsRudeUnload() ||
             (pThread != SystemDomain::System()->GetUnloadRequestingThread() || OnlyOneThreadLeft())
            )
           )
        {

            STRESS_LOG2(LF_APPDOMAIN, LL_INFO100, "AppDomain::UnwindThreads stopping %x with %d transitions\n", pThread->GetThreadId(), count);
            LOG((LF_APPDOMAIN, LL_INFO100, "AppDomain::UnwindThreads stopping %x with %d transitions\n", pThread->GetThreadId(), count));
#if _DEBUG_ADUNLOAD
            printf("AppDomain::UnwindThreads %x stopping %x with first frame %8.8p\n", GetThread()->GetThreadId(), pThread->GetThreadId(), pFrame);
#endif
            if (pThread == SystemDomain::System()->GetUnloadRequestingThread())
            {
                // Mark thread for abortion only once; later on interrupt only
                *pFMarkUnloadRequestThread = FALSE;
            }
            pThread->SetAbortRequest(m_fRudeUnload? EEPolicy::TA_Rude : EEPolicy::TA_V1Compatible);
        }
        TESTHOOKCALL(UnwindingThreads(GetId().m_dwId)) ;
    }
    _ASSERTE(totalADCount + finalizerADCount == (int)m_dwThreadEnterCount);

    //@TODO: This is intended to catch a stress bug. Remove when no longer needed.
    if (totalADCount + finalizerADCount != (int)m_dwThreadEnterCount)
        FreeBuildDebugBreak();

    // if our count did get messed up, set it to whatever count we actually found in the domain to avoid looping
    // or other problems related to incorrect count. This is very much a bug if this happens - a thread should always
    // exit the domain gracefully.
    // m_dwThreadEnterCount = totalADCount;

    if (reKind != kLastException)
    {
        pThread = NULL;
        while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
        {
            if (pThread->IsBeingAbortedForADUnload())
            {
                pThread->ResetBeginAbortedForADUnload();
            }
        }
    }

    // CommonTripThread will handle the abort for any threads that we've marked
    ThreadSuspend::RestartEE(FALSE, TRUE);
    if (reKind != kLastException)
        COMPlusThrow(reKind, resId, ssThreadId.GetUnicode());

    _ASSERTE((totalADCount==0 && nThreadsNeedMoreWork==0) ||(totalADCount!=0 && nThreadsNeedMoreWork!=0));
    
    m_dwThreadsStillInAppDomain=nThreadsNeedMoreWork;
    return (totalADCount == 0);
}

void AppDomain::UnwindThreads()
{
    // This function should guarantee appdomain
    // consistency even if it fails. Everything that is going
    // to make the appdomain impossible to reenter
    // should be factored out

    // <TODO>@todo: need real synchronization here!!!</TODO>
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    int retryCount = -1;
    m_dwThreadsStillInAppDomain=(ULONG)-1;
    ULONGLONG startTime = CLRGetTickCount64();

    if (GetEEPolicy()->GetDefaultAction(OPR_AppDomainUnload, NULL) == eRudeUnloadAppDomain &&
        !IsRudeUnload())
    {
        GetEEPolicy()->NotifyHostOnDefaultAction(OPR_AppDomainUnload, eRudeUnloadAppDomain);
        SetRudeUnload();
    }

    // Force threads to go through slow path during AD unload.
    TSSuspendHolder shTrap;

    BOOL fCurrentUnloadMode = IsRudeUnload();
    BOOL fMarkUnloadRequestThread = TRUE;

    // now wait for all the threads running in our AD to get out
    do
    {
        DWORD timeout = GetEEPolicy()->GetTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload);
        EPolicyAction action = GetEEPolicy()->GetActionOnTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload, NULL);
        if (timeout != INFINITE && action > eUnloadAppDomain) {
            // Escalation policy specified.
            ULONGLONG curTime = CLRGetTickCount64();
            ULONGLONG elapseTime = curTime - startTime;
            if (elapseTime > timeout)
            {
                // Escalate
                switch (action)
                {
                case eRudeUnloadAppDomain:
                    GetEEPolicy()->NotifyHostOnTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload, action);
                    SetRudeUnload();
                    STRESS_LOG1(LF_APPDOMAIN, LL_INFO100,"Escalating to RADU, adid=%d",GetId().m_dwId);
                    break;
                case eExitProcess:
                case eFastExitProcess:
                case eRudeExitProcess:
                case eDisableRuntime:
                    GetEEPolicy()->NotifyHostOnTimeout(m_fRudeUnload?OPR_AppDomainRudeUnload : OPR_AppDomainUnload, action);
                    EEPolicy::HandleExitProcessFromEscalation(action, HOST_E_EXITPROCESS_TIMEOUT);
                    _ASSERTE (!"Should not reach here");
                    break;
                default:
                    break;
                }
            }
        }
#ifdef _DEBUG
        if (LoggingOn(LF_APPDOMAIN, LL_INFO100))
            DumpADThreadTrack();
#endif // _DEBUG
        BOOL fNextUnloadMode = IsRudeUnload();
        if (fCurrentUnloadMode != fNextUnloadMode)
        {
            // We have changed from normal unload to rude unload.  We need to mark the thread
            // with RudeAbort, but we can only do this safely if the runtime is suspended.
            fCurrentUnloadMode = fNextUnloadMode;
            retryCount = -1;
        }
        if (StopEEAndUnwindThreads(retryCount, &fMarkUnloadRequestThread))
            break;
        if (timeout != INFINITE)
        {
            // Turn off the timeout used by AD.
            retryCount = 1;
        }
        else
        {
            // GCStress takes a long time to unwind, due to expensive creation of
            // a threadabort exception.
            if (!GCStress<cfg_any>::IsEnabled())
                ++retryCount;
            LOG((LF_APPDOMAIN, LL_INFO10, "AppDomain::UnwindThreads iteration %d waiting on thread count %d\n", retryCount, m_dwThreadEnterCount));
#if _DEBUG_ADUNLOAD
            printf("AppDomain::UnwindThreads iteration %d waiting on thread count %d\n", retryCount, m_dwThreadEnterCount);
#endif
        }

        if (m_dwThreadEnterCount != 0)
        {
#ifdef _DEBUG
            GetThread()->UserSleep(20);
#else // !_DEBUG
            GetThread()->UserSleep(10);
#endif // !_DEBUG
        }
    }
    while (TRUE) ;
}

void AppDomain::ClearGCHandles()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        NOTHROW;
    }
    CONTRACTL_END;

    SetStage(STAGE_HANDLETABLE_NOACCESS);

    GCHeap::GetGCHeap()->WaitUntilConcurrentGCComplete();

    // Keep async pin handles alive by moving them to default domain
    HandleAsyncPinHandles();

    // Remove our handle table as a source of GC roots
    HandleTableBucket *pBucket = m_hHandleTableBucket;

#ifdef _DEBUG
    if (((HandleTable *)(pBucket->pTable[0]))->uADIndex != m_dwIndex)
        _ASSERTE (!"AD index mismatch");
#endif // _DEBUG

    Ref_RemoveHandleTableBucket(pBucket);
}

// When an AD is unloaded, we will release all objects in this AD.
// If a future asynchronous operation, like io completion port function,
// we need to keep the memory space fixed so that the gc heap is not corrupted.
void AppDomain::HandleAsyncPinHandles()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        NOTHROW;
    }
    CONTRACTL_END;

    HandleTableBucket *pBucket = m_hHandleTableBucket;
    // IO completion port picks IO job using FIFO.  Here is how we know which AsyncPinHandle can be freed.
    // 1. We mark all non-pending AsyncPinHandle with READYTOCLEAN.
    // 2. We queue a dump Overlapped to the IO completion as a marker.
    // 3. When the Overlapped is picked up by completion port, we wait until all previous IO jobs are processed.
    // 4. Then we can delete all AsyncPinHandle marked with READYTOCLEAN.
    HandleTableBucket *pBucketInDefault = SystemDomain::System()->DefaultDomain()->m_hHandleTableBucket;
    Ref_RelocateAsyncPinHandles(pBucket, pBucketInDefault);

    OverlappedDataObject::RequestCleanup();
}

void AppDomain::ClearGCRoots()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        NOTHROW;
    }
    CONTRACTL_END;

    Thread *pThread = NULL;
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN);

    // Tell the JIT managers to delete any entries in their structures. All the cooperative mode threads are stopped at
    // this point, so only need to synchronize the preemptive mode threads.
    ExecutionManager::Unload(GetLoaderAllocator());

    while ((pThread = ThreadStore::GetAllThreadList(pThread, 0, 0)) != NULL)
    {
        // Delete the thread local static store
        pThread->DeleteThreadStaticData(this);

#ifdef FEATURE_LEAK_CULTURE_INFO
        pThread->ResetCultureForDomain(GetId());
#endif // FEATURE_LEAK_CULTURE_INFO

        // <TODO>@TODO: A pre-allocated AppDomainUnloaded exception might be better.</TODO>
        if (m_hHandleTableBucket->Contains(pThread->m_LastThrownObjectHandle))
        {
            // Never delete a handle to a preallocated exception object.
            if (!CLRException::IsPreallocatedExceptionHandle(pThread->m_LastThrownObjectHandle))
            {
                DestroyHandle(pThread->m_LastThrownObjectHandle);
            }

            pThread->m_LastThrownObjectHandle = NULL;
        }

        // Clear out the exceptions objects held by a thread.
        pThread->GetExceptionState()->ClearThrowablesForUnload(m_hHandleTableBucket);
    }

    //delete them while we still have the runtime suspended
    // This must be deleted before the loader heaps are deleted.
    if (m_pMarshalingData != NULL)
    {
        delete m_pMarshalingData;
        m_pMarshalingData = NULL;
    }

    if (m_pLargeHeapHandleTable != NULL)
    {
        delete m_pLargeHeapHandleTable;
        m_pLargeHeapHandleTable = NULL;
    }

    ThreadSuspend::RestartEE(FALSE, TRUE);
}

#ifdef _DEBUG

void AppDomain::TrackADThreadEnter(Thread *pThread, Frame *pFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // REENTRANT
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(pFrame != (Frame*)(size_t) INVALID_POINTER_CD);
    }
    CONTRACTL_END;

    while (FastInterlockCompareExchange((LONG*)&m_TrackSpinLock, 1, 0) != 0)
        ;
    if (m_pThreadTrackInfoList == NULL)
        m_pThreadTrackInfoList = new (nothrow) ThreadTrackInfoList;
    // If we don't assert here, we will AV in the for loop below
    _ASSERTE(m_pThreadTrackInfoList);

    ThreadTrackInfoList *pTrackList= m_pThreadTrackInfoList;

    ThreadTrackInfo *pTrack = NULL;
    int i;
    for (i=0; i < pTrackList->Count(); i++) {
        if ((*(pTrackList->Get(i)))->pThread == pThread) {
            pTrack = *(pTrackList->Get(i));
            break;
        }
    }
    if (! pTrack) {
        pTrack = new (nothrow) ThreadTrackInfo;
        // If we don't assert here, we will AV in the for loop below.
        _ASSERTE(pTrack);
        pTrack->pThread = pThread;
        ThreadTrackInfo **pSlot = pTrackList->Append();
        *pSlot = pTrack;
    }

    InterlockedIncrement((LONG*)&m_dwThreadEnterCount);
    Frame **pSlot;
    if (pTrack)
    {
        pSlot = pTrack->frameStack.Insert(0);
        *pSlot = pFrame;
    }
    int totThreads = 0;
    for (i=0; i < pTrackList->Count(); i++)
        totThreads += (*(pTrackList->Get(i)))->frameStack.Count();
    _ASSERTE(totThreads == (int)m_dwThreadEnterCount);

    InterlockedExchange((LONG*)&m_TrackSpinLock, 0);
}


void AppDomain::TrackADThreadExit(Thread *pThread, Frame *pFrame)
{
    CONTRACTL
    {
        if (GetThread()) {MODE_COOPERATIVE;}
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    while (FastInterlockCompareExchange((LONG*)&m_TrackSpinLock, 1, 0) != 0)
        ;
    ThreadTrackInfoList *pTrackList= m_pThreadTrackInfoList;
    _ASSERTE(pTrackList);
    ThreadTrackInfo *pTrack = NULL;
    int i;
    for (i=0; i < pTrackList->Count(); i++)
    {
        if ((*(pTrackList->Get(i)))->pThread == pThread)
        {
            pTrack = *(pTrackList->Get(i));
            break;
        }
    }
    _ASSERTE(pTrack);
    _ASSERTE(*(pTrack->frameStack.Get(0)) == pFrame);
    pTrack->frameStack.Delete(0);
    InterlockedDecrement((LONG*)&m_dwThreadEnterCount);

    int totThreads = 0;
    for (i=0; i < pTrackList->Count(); i++)
        totThreads += (*(pTrackList->Get(i)))->frameStack.Count();
    _ASSERTE(totThreads == (int)m_dwThreadEnterCount);

    InterlockedExchange((LONG*)&m_TrackSpinLock, 0);
}

void AppDomain::DumpADThreadTrack()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    while (FastInterlockCompareExchange((LONG*)&m_TrackSpinLock, 1, 0) != 0)
        ;
    ThreadTrackInfoList *pTrackList= m_pThreadTrackInfoList;
    if (!pTrackList)
        goto end;

    {
        LOG((LF_APPDOMAIN, LL_INFO10000, "\nThread dump of %d threads for [%d] %#08x %S\n",
             m_dwThreadEnterCount, GetId().m_dwId, this, GetFriendlyNameForLogging()));
        int totThreads = 0;
        for (int i=0; i < pTrackList->Count(); i++)
        {
            ThreadTrackInfo *pTrack = *(pTrackList->Get(i));
            if (pTrack->frameStack.Count()==0)
                continue;
            LOG((LF_APPDOMAIN, LL_INFO100, "  ADEnterCount for %x is %d\n", pTrack->pThread->GetThreadId(), pTrack->frameStack.Count()));
            totThreads += pTrack->frameStack.Count();
            for (int j=0; j < pTrack->frameStack.Count(); j++)
                LOG((LF_APPDOMAIN, LL_INFO100, "      frame %8.8x\n", *(pTrack->frameStack.Get(j))));
        }
        _ASSERTE(totThreads == (int)m_dwThreadEnterCount);
    }
end:
    InterlockedExchange((LONG*)&m_TrackSpinLock, 0);
}
#endif // _DEBUG

#ifdef FEATURE_REMOTING
OBJECTREF AppDomain::GetAppDomainProxy()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    OBJECTREF orProxy = CRemotingServices::CreateProxyForDomain(this);

    _ASSERTE(orProxy->IsTransparentProxy());

    return orProxy;
}
#endif

#endif // CROSSGEN_COMPILE

void *SharedDomain::operator new(size_t size, void *pInPlace)
{
    LIMITED_METHOD_CONTRACT;
    return pInPlace;
}

void SharedDomain::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Do nothing - new() was in-place
}


void SharedDomain::Attach()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Create the global SharedDomain and initialize it.
    m_pSharedDomain = new (&g_pSharedDomainMemory[0]) SharedDomain();
    SystemDomain::GetGlobalLoaderAllocator()->m_pDomain = m_pSharedDomain;
    // This cannot fail since g_pSharedDomainMemory is a static array.
    CONSISTENCY_CHECK(CheckPointer(m_pSharedDomain));

    LOG((LF_CLASSLOADER,
         LL_INFO10,
         "Created shared domain at %p\n",
         m_pSharedDomain));

    // We need to initialize the memory pools etc. for the system domain.
    m_pSharedDomain->Init(); // Setup the memory heaps

    // allocate a Virtual Call Stub Manager for the shared domain
    m_pSharedDomain->InitVSD();
}

#ifndef CROSSGEN_COMPILE
void SharedDomain::Detach()
{
    if (m_pSharedDomain)
    {
        m_pSharedDomain->Terminate();
        delete m_pSharedDomain;
        m_pSharedDomain = NULL;
    }
}
#endif // CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE

SharedDomain *SharedDomain::GetDomain()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return m_pSharedDomain;
}

#ifndef DACCESS_COMPILE

#define INITIAL_ASSEMBLY_MAP_SIZE 17
void SharedDomain::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    BaseDomain::Init();

#ifdef FEATURE_LOADER_OPTIMIZATION
    m_FileCreateLock.Init(CrstSharedAssemblyCreate, CRST_DEFAULT,TRUE);

    LockOwner lock = { &m_DomainCrst, IsOwnerOfCrst };
    m_assemblyMap.Init(INITIAL_ASSEMBLY_MAP_SIZE, CompareSharedAssembly, TRUE, &lock);
#endif // FEATURE_LOADER_OPTIMIZATION 

    ETW::LoaderLog::DomainLoad(this);
}

#ifndef CROSSGEN_COMPILE
void SharedDomain::Terminate()
{
    // make sure we delete the StringLiteralMap before unloading
    // the asemblies since the string literal map entries can
    // point to metadata string literals.
    GetLoaderAllocator()->CleanupStringLiteralMap();

#ifdef FEATURE_LOADER_OPTIMIZATION    
    PtrHashMap::PtrIterator i = m_assemblyMap.begin();

    while (!i.end())
    {
        Assembly *pAssembly = (Assembly*) i.GetValue();
        delete pAssembly;
        ++i;
    }

    ListLockEntry* pElement;
    pElement = m_FileCreateLock.Pop(TRUE);
    while (pElement)
    {
#ifdef STRICT_CLSINITLOCK_ENTRY_LEAK_DETECTION
        _ASSERTE (dbg_fDrasticShutdown || g_fInControlC);
#endif
        delete(pElement);
        pElement = (FileLoadLock*) m_FileCreateLock.Pop(TRUE);
    }
    m_FileCreateLock.Destroy();
#endif // FEATURE_LOADER_OPTIMIZATION    
    BaseDomain::Terminate();
}
#endif // CROSSGEN_COMPILE



#ifdef FEATURE_LOADER_OPTIMIZATION

BOOL SharedDomain::CompareSharedAssembly(UPTR u1, UPTR u2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This is the input to the lookup
    SharedAssemblyLocator *pLocator = (SharedAssemblyLocator *) (u1<<1);

    // This is the value stored in the table
    Assembly *pAssembly = (Assembly *) u2;
    if (pLocator->GetType()==SharedAssemblyLocator::DOMAINASSEMBLY)
    {
        if (!pAssembly->GetManifestFile()->Equals(pLocator->GetDomainAssembly()->GetFile()))
            return FALSE;

        return pAssembly->CanBeShared(pLocator->GetDomainAssembly());
    }
    else
    if (pLocator->GetType()==SharedAssemblyLocator::PEASSEMBLY)
        return pAssembly->GetManifestFile()->Equals(pLocator->GetPEAssembly());
    else
    if (pLocator->GetType()==SharedAssemblyLocator::PEASSEMBLYEXACT)
        return pAssembly->GetManifestFile() == pLocator->GetPEAssembly();
     _ASSERTE(!"Unexpected type of assembly locator");
    return FALSE;
}

DWORD SharedAssemblyLocator::Hash()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_type==DOMAINASSEMBLY)
        return GetDomainAssembly()->HashIdentity();
    if (m_type==PEASSEMBLY||m_type==PEASSEMBLYEXACT)
        return GetPEAssembly()->HashIdentity();
     _ASSERTE(!"Unexpected type of assembly locator");
     return 0;
}

Assembly * SharedDomain::FindShareableAssembly(SharedAssemblyLocator * pLocator)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    Assembly * match= (Assembly *) m_assemblyMap.LookupValue(pLocator->Hash(), pLocator);
    if (match != (Assembly *) INVALIDENTRY)
        return match;
    else
        return NULL;
}

SIZE_T SharedDomain::GetShareableAssemblyCount()
{
    LIMITED_METHOD_CONTRACT;

    return m_assemblyMap.GetCount();
}

void SharedDomain::AddShareableAssembly(Assembly * pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // We have a lock on the file. There should be no races to add the same assembly.

    {
        LockHolder holder(this);

        EX_TRY
        {
            pAssembly->SetIsTenured();
            m_assemblyMap.InsertValue(pAssembly->HashIdentity(), pAssembly);
        }
        EX_HOOK
        {
            // There was an error adding the assembly to the assembly hash (probably an OOM),
            // so we need to unset the tenured bit so that correct cleanup can happen.
            pAssembly->UnsetIsTenured();
        }
        EX_END_HOOK
    }

    LOG((LF_CODESHARING,
         LL_INFO100,
         "Successfully added shareable assembly \"%s\".\n",
         pAssembly->GetManifestFile()->GetSimpleName()));
}

#endif // FEATURE_LOADER_OPTIMIZATION
#endif // !DACCESS_COMPILE

DWORD DomainLocalModule::GetClassFlags(MethodTable* pMT, DWORD iClassIndex /*=(DWORD)-1*/)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    {   // SO tolerance exception for debug-only assertion.
        CONTRACT_VIOLATION(SOToleranceViolation);
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

void DomainLocalBlock::EnsureModuleIndex(ModuleIndex index)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        // Assumes BaseDomain::DomainLocalBlockLockHolder is taken
        PRECONDITION(m_pDomain->OwnDomainLocalBlockLock());
    }
    CONTRACTL_END;

    if (m_aModuleIndices > index.m_dwIndex)
    {
        _ASSERTE(m_pModuleSlots != NULL);
        return;
    }

    SIZE_T aModuleIndices = max(16, m_aModuleIndices);
    while (aModuleIndices <= index.m_dwIndex)
    {
        aModuleIndices *= 2;
    }

    PTR_DomainLocalModule* pNewModuleSlots = (PTR_DomainLocalModule*) (void*)m_pDomain->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(PTR_DomainLocalModule)) * S_SIZE_T(aModuleIndices));

    memcpy(pNewModuleSlots, m_pModuleSlots, sizeof(SIZE_T)*m_aModuleIndices);

    // Note: Memory allocated on loader heap is zero filled
    // memset(pNewModuleSlots + m_aModuleIndices, 0 , (aModuleIndices - m_aModuleIndices)*sizeof(PTR_DomainLocalModule) );

    // Commit new table. The lock-free helpers depend on the order.
    MemoryBarrier();
    m_pModuleSlots = pNewModuleSlots;
    MemoryBarrier();
    m_aModuleIndices = aModuleIndices;

}

void DomainLocalBlock::SetModuleSlot(ModuleIndex index, PTR_DomainLocalModule pLocalModule)
{
    // Need to synchronize with table growth in this domain
    BaseDomain::DomainLocalBlockLockHolder lh(m_pDomain);

    EnsureModuleIndex(index);

    _ASSERTE(index.m_dwIndex < m_aModuleIndices);

    // We would like this assert here, unfortunately, loading a module in this appdomain can fail
    // after here  and we will keep the module around and reuse the slot when we retry (if
    // the failure happened due to a transient error, such as OOM). In that case the slot wont
    // be null.
    //_ASSERTE(m_pModuleSlots[index.m_dwIndex] == 0);

    m_pModuleSlots[index.m_dwIndex] = pLocalModule;
}

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
        OBJECTREF AppDomainRef;
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    if ((gc.AppDomainRef = GetRawExposedObject()) != NULL)
    {
        if (pAssembly != NULL)
            gc.AssemblyRef = pAssembly->GetExposedAssemblyObject();

        MethodDescCallSite onTypeResolve(METHOD__APP_DOMAIN__ON_TYPE_RESOLVE, &gc.AppDomainRef);

        gc.str = StringObject::NewString(szName);
        ARG_SLOT args[3] =
        {
            ObjToArgSlot(gc.AppDomainRef),
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
        OBJECTREF AppDomainRef;
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    if ((gc.AppDomainRef = GetRawExposedObject()) != NULL)
    {
        if (pAssembly != NULL)
            gc.AssemblyRef=pAssembly->GetExposedAssemblyObject();

        MethodDescCallSite onResourceResolve(METHOD__APP_DOMAIN__ON_RESOURCE_RESOLVE, &gc.AppDomainRef);
        gc.str = StringObject::NewString(szName);
        ARG_SLOT args[3] =
        {
            ObjToArgSlot(gc.AppDomainRef),
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
    }
    GCPROTECT_END();

    RETURN pResolvedAssembly;
}


Assembly * 
AppDomain::RaiseAssemblyResolveEvent(
    AssemblySpec * pSpec, 
    BOOL           fIntrospection, 
    BOOL           fPreBind)
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

    BinderMethodID methodId;
    StackSString ssName;
    pSpec->GetFileOrDisplayName(0, ssName);
    
#ifdef FEATURE_REFLECTION_ONLY_LOAD    
    if ( (!fIntrospection) && (!fPreBind) )
    {
        methodId = METHOD__APP_DOMAIN__ON_ASSEMBLY_RESOLVE;  // post-bind execution event (the classic V1.0 event)
    }
    else if ((!fIntrospection) && fPreBind)
    {
        RETURN NULL; // There is currently no prebind execution resolve event
    }
    else if (fIntrospection && !fPreBind)
    {
        RETURN NULL; // There is currently no post-bind introspection resolve event
    }
    else
    {
        _ASSERTE( fIntrospection && fPreBind );
        methodId = METHOD__APP_DOMAIN__ON_REFLECTION_ONLY_ASSEMBLY_RESOLVE; // event for introspection assemblies
    }
#else // FEATURE_REFLECTION_ONLY_LOAD
    if (!fPreBind) 
    {
        methodId = METHOD__APP_DOMAIN__ON_ASSEMBLY_RESOLVE;  // post-bind execution event (the classic V1.0 event)
    }
    else
    {
        RETURN NULL;
    }
        
#endif // FEATURE_REFLECTION_ONLY_LOAD

    // Elevate threads allowed loading level.  This allows the host to load an assembly even in a restricted
    // condition.  Note, however, that this exposes us to possible recursion failures, if the host tries to
    // load the assemblies currently being loaded.  (Such cases would then throw an exception.)

    OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    GCX_COOP();

    Assembly* pAssembly = NULL;

    struct _gc {
        OBJECTREF AppDomainRef;
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    if ((gc.AppDomainRef = GetRawExposedObject()) != NULL)
    {
        if (pSpec->GetParentAssembly() != NULL)
        {
            if ( pSpec->IsIntrospectionOnly() 
#ifdef FEATURE_FUSION
                    || pSpec->GetParentLoadContext() == LOADCTX_TYPE_UNKNOWN
#endif
                )
            {
                gc.AssemblyRef=pSpec->GetParentAssembly()->GetExposedAssemblyObject();
            }
        }
        MethodDescCallSite onAssemblyResolve(methodId, &gc.AppDomainRef);

        gc.str = StringObject::NewString(ssName);
        ARG_SLOT args[3] = {
            ObjToArgSlot(gc.AppDomainRef),
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
        if  ((!(pAssembly->IsIntrospectionOnly())) != (!fIntrospection))
        {
            // Cannot return an introspection assembly from an execution callback or vice-versa
            COMPlusThrow(kFileLoadException, pAssembly->IsIntrospectionOnly() ? IDS_CLASSLOAD_ASSEMBLY_RESOLVE_RETURNED_INTROSPECTION : IDS_CLASSLOAD_ASSEMBLY_RESOLVE_RETURNED_EXECUTION);
        }

        // Check that the public key token matches the one specified in the spec
        // MatchPublicKeys throws as appropriate
        pSpec->MatchPublicKeys(pAssembly);
    }

    RETURN pAssembly;
} // AppDomain::RaiseAssemblyResolveEvent

#ifndef FEATURE_CORECLR

//---------------------------------------------------------------------------------------
//
// Ask the AppDomainManager for the entry assembly of the application
//
// Note:
//   Most AppDomainManagers will fall back on the root assembly for the domain, so we need
//   to make sure this is set before we call through to the AppDomainManager itself.
//

Assembly *AppDomain::GetAppDomainManagerEntryAssembly()
{
    CONTRACT(Assembly *)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(HasAppDomainManagerInfo());
        PRECONDITION(CheckPointer(m_pRootAssembly));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    GCX_COOP();

    Assembly *pEntryAssembly = NULL;

    struct
    {
        APPDOMAINREF    orDomain;
        OBJECTREF       orAppDomainManager;
        ASSEMBLYREF     orEntryAssembly;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.orDomain = static_cast<APPDOMAINREF>(GetExposedObject());
    gc.orAppDomainManager = gc.orDomain->GetAppDomainManager();
    _ASSERTE(gc.orAppDomainManager != NULL);

    MethodDescCallSite getEntryAssembly(METHOD__APPDOMAIN_MANAGER__GET_ENTRY_ASSEMBLY, &gc.orAppDomainManager);
    ARG_SLOT argThis = ObjToArgSlot(gc.orAppDomainManager);
    gc.orEntryAssembly = static_cast<ASSEMBLYREF>(getEntryAssembly.Call_RetOBJECTREF(&argThis));

    if (gc.orEntryAssembly != NULL)
    {
        pEntryAssembly = gc.orEntryAssembly->GetAssembly();
    }

    GCPROTECT_END();

    // If the AppDomainManager did not return an entry assembly, we'll assume the default assembly
    if (pEntryAssembly == NULL)
    {
        pEntryAssembly = m_pRootAssembly;
    }

    RETURN(pEntryAssembly);
}

#endif // !FEATURE_CORECLR

//---------------------------------------------------------------------------------------
//
// Determine the type of AppDomainManager to use for the default AppDomain
//
// Notes:
//   v2.0 of the CLR used environment variables APPDOMAIN_MANAGER_ASM and APPDOMAIN_MANAGER_TYPE to set the
//   domain manager. For compatibility these are still supported, along with appDomainManagerAsm and
//   appDomainManagerType config file switches. If the config switches are supplied, the entry point must be
//   fully trusted.  
//

void AppDomain::InitializeDefaultDomainManager()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(GetId().m_dwId == DefaultADID);
        PRECONDITION(!HasAppDomainManagerInfo());
    }
    CONTRACTL_END;

    //
    // The AppDomainManager for the default domain can be specified by:
    //  1. Native hosting API
    //  2. Application config file if the application is fully trusted
    //  3. Environment variables
    //


    if (CorHost2::HasAppDomainManagerInfo())
    {
        SetAppDomainManagerInfo(CorHost2::GetAppDomainManagerAsm(),
                                CorHost2::GetAppDomainManagerType(),
                                CorHost2::GetAppDomainManagerInitializeNewDomainFlags());
        m_fAppDomainManagerSetInConfig = FALSE;

        LOG((LF_APPDOMAIN, LL_INFO10, "Setting default AppDomainManager '%S', '%S' from hosting API.\n", GetAppDomainManagerAsm(), GetAppDomainManagerType()));
    }
#ifndef FEATURE_CORECLR
    else
    {
        CLRConfigStringHolder wszConfigAppDomainManagerAssembly(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AppDomainManagerAsm));
        CLRConfigStringHolder wszConfigAppDomainManagerType(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AppDomainManagerType));

        if (wszConfigAppDomainManagerAssembly != NULL &&
            wszConfigAppDomainManagerType != NULL)
        {
            SetAppDomainManagerInfo(wszConfigAppDomainManagerAssembly,
                                    wszConfigAppDomainManagerType,
                                    eInitializeNewDomainFlags_None);
            m_fAppDomainManagerSetInConfig = TRUE;

            LOG((LF_APPDOMAIN, LL_INFO10, "Setting default AppDomainManager '%S', '%S' from application config file.\n", GetAppDomainManagerAsm(), GetAppDomainManagerType()));
        }
        else
        {
            CLRConfigStringHolder wszEnvironmentAppDomainManagerAssembly(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_LEGACY_APPDOMAIN_MANAGER_ASM));
            CLRConfigStringHolder wszEnvironmentAppDomainManagerType(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_LEGACY_APPDOMAIN_MANAGER_TYPE));

            if (wszEnvironmentAppDomainManagerAssembly != NULL &&
                wszEnvironmentAppDomainManagerType != NULL)
            {
                SetAppDomainManagerInfo(wszEnvironmentAppDomainManagerAssembly,
                                        wszEnvironmentAppDomainManagerType,
                                        eInitializeNewDomainFlags_None);
                m_fAppDomainManagerSetInConfig = FALSE;

                LOG((LF_APPDOMAIN, LL_INFO10, "Setting default AppDomainManager '%S', '%S' from environment variables.\n", GetAppDomainManagerAsm(), GetAppDomainManagerType()));

                // Reset the environmetn variables so that child processes do not inherit our domain manager
                // by default.
                WszSetEnvironmentVariable(CLRConfig::EXTERNAL_LEGACY_APPDOMAIN_MANAGER_ASM.name, NULL);
                WszSetEnvironmentVariable(CLRConfig::EXTERNAL_LEGACY_APPDOMAIN_MANAGER_TYPE.name, NULL);
            }
        }
    }
#endif // !FEATURE_CORECLR

    // If we found an AppDomain manager to use, create and initialize it
    // Otherwise, initialize the config flags.
    if (HasAppDomainManagerInfo())
    {
        // If the initialization flags promise that the domain manager isn't going to modify security, then do a
        // pre-resolution of the domain now so that we can do some basic verification of the state later.  We
        // don't care about the actual result now, just that the resolution took place to compare against later.
        if (GetAppDomainManagerInitializeNewDomainFlags() & eInitializeNewDomainFlags_NoSecurityChanges)
        {
            BOOL fIsFullyTrusted;
            BOOL fIsHomogeneous;
            GetSecurityDescriptor()->PreResolve(&fIsFullyTrusted, &fIsHomogeneous);
        }

        OBJECTREF orThis = GetExposedObject();
        GCPROTECT_BEGIN(orThis);

        MethodDescCallSite createDomainManager(METHOD__APP_DOMAIN__CREATE_APP_DOMAIN_MANAGER);
        ARG_SLOT args[] =
        {
            ObjToArgSlot(orThis)
        };

        createDomainManager.Call(args);

        GCPROTECT_END();
    }
    else
    {
        OBJECTREF orThis = GetExposedObject();
        GCPROTECT_BEGIN(orThis);

        MethodDescCallSite initCompatFlags(METHOD__APP_DOMAIN__INITIALIZE_COMPATIBILITY_FLAGS);
        ARG_SLOT args[] =
        {
            ObjToArgSlot(orThis)
        };

        initCompatFlags.Call(args);

        GCPROTECT_END();
    }
}

#ifdef FEATURE_CLICKONCE

//---------------------------------------------------------------------------------------
//
// If we are launching a ClickOnce application, setup the default domain with the deails
// of the application.
//

void AppDomain::InitializeDefaultClickOnceDomain()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(GetId().m_dwId == DefaultADID);
    }
    CONTRACTL_END;

    //
    // If the CLR is being started to run a ClickOnce application, then capture the information about the
    // application to setup the default domain wtih.
    //

    if (CorCommandLine::m_pwszAppFullName != NULL)
    {
        struct
        {
            OBJECTREF   orThis;
            STRINGREF   orAppFullName;
            PTRARRAYREF orManifestPathsArray;
            PTRARRAYREF orActivationDataArray;
        }
        gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.orAppFullName = StringObject::NewString(CorCommandLine::m_pwszAppFullName);

        // If specific manifests have been pointed at, make a note of them
        if (CorCommandLine::m_dwManifestPaths > 0)
        {
            _ASSERTE(CorCommandLine::m_ppwszManifestPaths != NULL);

            gc.orManifestPathsArray = static_cast<PTRARRAYREF>(AllocateObjectArray(CorCommandLine::m_dwManifestPaths, g_pStringClass));
            for (DWORD i = 0; i < CorCommandLine::m_dwManifestPaths; ++i)
            {
                STRINGREF str = StringObject::NewString(CorCommandLine::m_ppwszManifestPaths[i]);
                gc.orManifestPathsArray->SetAt(i, str);
            }
        }

        // Check for any activation parameters to pass to the ClickOnce application
        if (CorCommandLine::m_dwActivationData > 0)
        {
            _ASSERTE(CorCommandLine::m_ppwszActivationData != NULL);

            gc.orActivationDataArray = static_cast<PTRARRAYREF>(AllocateObjectArray(CorCommandLine::m_dwActivationData, g_pStringClass));
            for (DWORD i = 0; i < CorCommandLine::m_dwActivationData; ++i)
            {
                STRINGREF str = StringObject::NewString(CorCommandLine::m_ppwszActivationData[i]);
                gc.orActivationDataArray->SetAt(i, str);
            }
        }

        gc.orThis = GetExposedObject();

        MethodDescCallSite setupDefaultClickOnceDomain(METHOD__APP_DOMAIN__SETUP_DEFAULT_CLICKONCE_DOMAIN);
        ARG_SLOT args[] =
        {
            ObjToArgSlot(gc.orThis),
            ObjToArgSlot(gc.orAppFullName),
            ObjToArgSlot(gc.orManifestPathsArray),
            ObjToArgSlot(gc.orActivationDataArray),
        };
        setupDefaultClickOnceDomain.Call(args);

        GCPROTECT_END();
    }
}

BOOL AppDomain::IsClickOnceAppDomain()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    return ((APPDOMAINREF)GetExposedObject())->HasActivationContext();
}

#endif // FEATURE_CLICKONCE

//---------------------------------------------------------------------------------------
//
// Intialize the security settings in the default AppDomain.
//

void AppDomain::InitializeDefaultDomainSecurity()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(GetId().m_dwId == DefaultADID);
    }
    CONTRACTL_END;

    OBJECTREF orThis = GetExposedObject();
    GCPROTECT_BEGIN(orThis);

    MethodDescCallSite initializeSecurity(METHOD__APP_DOMAIN__INITIALIZE_DOMAIN_SECURITY);
    ARG_SLOT args[] =
    {
        ObjToArgSlot(orThis),
        ObjToArgSlot(NULL),
        ObjToArgSlot(NULL),
        static_cast<ARG_SLOT>(FALSE),
        ObjToArgSlot(NULL),
        static_cast<ARG_SLOT>(FALSE)
    };

    initializeSecurity.Call(args);

    GCPROTECT_END();
}

CLREvent * AppDomain::g_pUnloadStartEvent;

void AppDomain::CreateADUnloadWorker()
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_CORECLR
    // Do not create adUnload thread if there is only default domain
    if(IsSingleAppDomain())
        return;
#endif

Retry:
    BOOL fCreator = FALSE;
    if (FastInterlockCompareExchange((LONG *)&g_fADUnloadWorkerOK,-2,-1)==-1)  //we're first
    {
#ifdef _TARGET_X86_  // use the smallest possible stack on X86 
        DWORD stackSize = 128 * 1024;
#else
        DWORD stackSize = 512 * 1024; // leave X64 unchanged since we have plenty of VM
#endif
        Thread *pThread = SetupUnstartedThread();
        if (pThread->CreateNewThread(stackSize, ADUnloadThreadStart, pThread))
        {
            fCreator = TRUE;
            DWORD dwRet;
            dwRet = pThread->StartThread();

            // When running under a user mode native debugger there is a race
            // between the moment we've created the thread (in CreateNewThread) and 
            // the moment we resume it (in StartThread); the debugger may receive 
            // the "ct" (create thread) notification, and it will attempt to 
            // suspend/resume all threads in the process.  Now imagine the debugger
            // resumes this thread first, and only later does it try to resume the
            // newly created thread (the ADU worker thread).  In these conditions our
            // call to ResumeThread may come before the debugger's call to ResumeThread
            // actually causing dwRet to equal 2.
            // We cannot use IsDebuggerPresent() in the condition below because the 
            // debugger may have been detached between the time it got the notification
            // and the moment we execute the test below.
            _ASSERTE(dwRet == 1 || dwRet == 2);
        }
        else
        {
            pThread->DecExternalCount(FALSE);
            FastInterlockExchange((LONG *)&g_fADUnloadWorkerOK, -1);
            ThrowOutOfMemory();
        }
    }

    YIELD_WHILE (g_fADUnloadWorkerOK == -2);

    if (g_fADUnloadWorkerOK == -1) {
        if (fCreator)
        {
            ThrowOutOfMemory();
        }
        else
        {
            goto Retry;
        }
    }
}

/*static*/ void AppDomain::ADUnloadWorkerHelper(AppDomain *pDomain)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    ADUnloadSink* pADUnloadSink=pDomain->GetADUnloadSinkForUnload();
    HRESULT hr=S_OK;

    EX_TRY
    {
        pDomain->Unload(FALSE);
    }
    EX_CATCH_HRESULT(hr);

    if(pADUnloadSink)
    {
        SystemDomain::LockHolder lh;
        pADUnloadSink->ReportUnloadResult(hr,NULL);
        pADUnloadSink->Release();
    }
}

void AppDomain::DoADUnloadWork()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DWORD i = 1;
    while (TRUE) {

        AppDomain *pDomainToUnload = NULL;

        {
            // Take the lock so that no domain can be added or removed from the system domain
            SystemDomain::LockHolder lh;

            DWORD numDomain = SystemDomain::GetCurrentAppDomainMaxIndex();
            for (; i <= numDomain; i ++) {
                AppDomain * pDomain = SystemDomain::TestGetAppDomainAtIndex(ADIndex(i));
                //
                // @todo: We used to also select a domain if pDomain->IsUnload() returned true. But that causes
                // problems when we've failed to completely unload the AD in the past. If we've reached the CLEARED
                // stage, for instance, then there will be no default context and AppDomain::Exit() will simply crash.
                //
                if (pDomain && pDomain->IsUnloadRequested())
                {
                    pDomainToUnload = pDomain;
                    i ++;
                    break;
                }
            }
        }

        if (!pDomainToUnload) {
            break;
        }

        // We are the only thread that can unload domains so no one else can delete the appdomain
        ADUnloadWorkerHelper(pDomainToUnload);            
    }
}

static void DoADUnloadWorkHelper()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    EX_TRY {
        AppDomain::DoADUnloadWork();
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

ULONGLONG g_ObjFinalizeStartTime = 0;
Volatile<BOOL> g_FinalizerIsRunning = FALSE;
Volatile<ULONG> g_FinalizerLoopCount = 0;

ULONGLONG GetObjFinalizeStartTime()
{
    LIMITED_METHOD_CONTRACT;
    return g_ObjFinalizeStartTime;
}

void FinalizerThreadAbortOnTimeout()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_GC_TRIGGERS;

    {
        // If finalizer thread is blocked because scheduler is running another task,
        // or it is waiting for another thread, we first see if we get finalizer thread
        // running again.
        Thread::ThreadAbortWatchDog();
    }

    EX_TRY
    {
        Thread *pFinalizerThread = FinalizerThread::GetFinalizerThread();
        EPolicyAction action = GetEEPolicy()->GetActionOnTimeout(OPR_FinalizerRun, pFinalizerThread);
        switch (action)
        {
        case eAbortThread:
            GetEEPolicy()->NotifyHostOnTimeout(OPR_FinalizerRun, action);
            pFinalizerThread->UserAbort(Thread::TAR_Thread,
                                        EEPolicy::TA_Safe,
                                        INFINITE,
                                        Thread::UAC_FinalizerTimeout);
            break;
        case eRudeAbortThread:
            GetEEPolicy()->NotifyHostOnTimeout(OPR_FinalizerRun, action);
            pFinalizerThread->UserAbort(Thread::TAR_Thread,
                                        EEPolicy::TA_Rude,
                                        INFINITE,
                                        Thread::UAC_FinalizerTimeout);
            break;
        case eUnloadAppDomain:
            {
                AppDomain *pDomain = pFinalizerThread->GetDomain();
                pFinalizerThread->UserAbort(Thread::TAR_Thread,
                                            EEPolicy::TA_Safe,
                                            INFINITE,
                                            Thread::UAC_FinalizerTimeout);
                if (!pDomain->IsDefaultDomain())
                {
                    GetEEPolicy()->NotifyHostOnTimeout(OPR_FinalizerRun, action);
                    pDomain->EnableADUnloadWorker(EEPolicy::ADU_Safe);
                }
            }
            break;
        case eRudeUnloadAppDomain:
            {
                AppDomain *pDomain = pFinalizerThread->GetDomain();
                pFinalizerThread->UserAbort(Thread::TAR_Thread,
                                            EEPolicy::TA_Rude,
                                            INFINITE,
                                            Thread::UAC_FinalizerTimeout);
                if (!pDomain->IsDefaultDomain())
                {
                    GetEEPolicy()->NotifyHostOnTimeout(OPR_FinalizerRun, action);
                    pDomain->EnableADUnloadWorker(EEPolicy::ADU_Rude);
                }
            }
            break;
        case eExitProcess:
        case eFastExitProcess:
        case eRudeExitProcess:
        case eDisableRuntime:
            GetEEPolicy()->NotifyHostOnTimeout(OPR_FinalizerRun, action);
            EEPolicy::HandleExitProcessFromEscalation(action, HOST_E_EXITPROCESS_TIMEOUT);
            _ASSERTE (!"Should not get here");
            break;
        default:
            break;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

enum WorkType
{
    WT_UnloadDomain = 0x1,
    WT_ThreadAbort = 0x2,
    WT_FinalizerThread = 0x4,
    WT_ClearCollectedDomains=0x8
};

static Volatile<DWORD> s_WorkType = 0;


DWORD WINAPI AppDomain::ADUnloadThreadStart(void *args)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);

        // This function will always be at the very bottom of the stack. The only
        // user code it calls is the AppDomainUnload notifications which we will
        // not be hardenning for Whidbey.
        //
        ENTRY_POINT;
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_NOTHROW;

    ClrFlsSetThreadType (ThreadType_ADUnloadHelper);

    Thread *pThread = (Thread*)args;
    bool fOK = (pThread->HasStarted() != 0);

    {
        GCX_MAYBE_PREEMP(fOK);

        if (fOK)
        {
            EX_TRY
            {
                if (CLRTaskHosted())
                {
                    // ADUnload helper thread is critical.  We do not want it to share scheduler
                    // with other tasks.
                    pThread->LeaveRuntime(0);
                }
            }
            EX_CATCH
            {
                fOK = false;
            }
            EX_END_CATCH(SwallowAllExceptions);
        }

        _ASSERTE (g_fADUnloadWorkerOK == -2);

        FastInterlockExchange((LONG *)&g_fADUnloadWorkerOK,fOK?1:-1);

        if (!fOK)
        {
            DestroyThread(pThread);
            goto Exit;
        }

        pThread->SetBackground(TRUE);

        pThread->SetThreadStateNC(Thread::TSNC_ADUnloadHelper);

        while (TRUE) {
            DWORD TAtimeout = INFINITE;
            ULONGLONG endTime = Thread::GetNextSelfAbortEndTime();
            ULONGLONG curTime = CLRGetTickCount64();
            if (endTime <= curTime) {
                TAtimeout = 5;
            }
            else
            {
                ULONGLONG diff = endTime - curTime;
                if (diff < MAXULONG)
                {
                    TAtimeout = (DWORD)diff;
                }
            }
            ULONGLONG finalizeStartTime = GetObjFinalizeStartTime();
            DWORD finalizeTimeout = INFINITE;
            DWORD finalizeTimeoutSetting = GetEEPolicy()->GetTimeout(OPR_FinalizerRun);
            if (finalizeTimeoutSetting != INFINITE && g_FinalizerIsRunning)
            {
                if (finalizeStartTime == 0)
                {
                    finalizeTimeout = finalizeTimeoutSetting;
                }
                else
                {
                    endTime = finalizeStartTime + finalizeTimeoutSetting;
                    if (endTime <= curTime) {
                        finalizeTimeout = 0;
                    }
                    else
                    {
                        ULONGLONG diff = endTime - curTime;
                        if (diff < MAXULONG)
                        {
                            finalizeTimeout = (DWORD)diff;
                        }
                    }
                }
            }

            if (AppDomain::HasWorkForFinalizerThread())
            {
                if (finalizeTimeout > finalizeTimeoutSetting)
                {
                    finalizeTimeout = finalizeTimeoutSetting;
                }
            }

            DWORD timeout = INFINITE;
            if (finalizeTimeout <= TAtimeout)
            {
                timeout = finalizeTimeout;
            }
            else
            {
                timeout = TAtimeout;
            }

            if (timeout != 0)
            {
                LOG((LF_APPDOMAIN, LL_INFO10, "Waiting to start unload\n"));
                g_pUnloadStartEvent->Wait(timeout,FALSE);
            }

            if (finalizeTimeout != INFINITE || (s_WorkType & WT_FinalizerThread) != 0)
            {
                STRESS_LOG0(LF_ALWAYS, LL_ALWAYS, "ADUnloadThreadStart work for Finalizer thread\n");
                FastInterlockAnd(&s_WorkType, ~WT_FinalizerThread);
                // only watch finalizer thread is finalizer method or unloadevent is being processed
                if (GetObjFinalizeStartTime() == finalizeStartTime && finalizeStartTime != 0 && g_FinalizerIsRunning)
                {
                    if (CLRGetTickCount64() >= finalizeStartTime+finalizeTimeoutSetting)
                    {
                        GCX_COOP();
                        FinalizerThreadAbortOnTimeout();
                    }
                }
                if (s_fProcessUnloadDomainEvent && g_FinalizerIsRunning)
                {
                    GCX_COOP();
                    FinalizerThreadAbortOnTimeout();
                }
            }

            if (TAtimeout != INFINITE || (s_WorkType & WT_ThreadAbort) != 0)
            {
                STRESS_LOG0(LF_ALWAYS, LL_ALWAYS, "ADUnloadThreadStart work for thread abort\n");
                FastInterlockAnd(&s_WorkType, ~WT_ThreadAbort);
                GCX_COOP();
                Thread::ThreadAbortWatchDog();
            }

            if ((s_WorkType & WT_UnloadDomain) != 0 && !AppDomain::HasWorkForFinalizerThread())
            {
                STRESS_LOG0(LF_ALWAYS, LL_ALWAYS, "ADUnloadThreadStart work for AD unload\n");
                FastInterlockAnd(&s_WorkType, ~WT_UnloadDomain);
                GCX_COOP();
                DoADUnloadWorkHelper();
            }

            if ((s_WorkType & WT_ClearCollectedDomains) != 0)
            {
                STRESS_LOG0(LF_ALWAYS, LL_ALWAYS, "ADUnloadThreadStart work for AD cleanup\n");
                FastInterlockAnd(&s_WorkType, ~WT_ClearCollectedDomains);
                GCX_COOP();
                SystemDomain::System()->ClearCollectedDomains();
            }

        }
Exit:;
    }

    END_ENTRYPOINT_NOTHROW;

    return 0;
}

void AppDomain::EnableADUnloadWorker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT; // Called during a SO
    }
    CONTRACTL_END;

    EEPolicy::AppDomainUnloadTypes type = EEPolicy::ADU_Safe;

#ifdef _DEBUG
    DWORD hostTestADUnload = g_pConfig->GetHostTestADUnload();
    if (hostTestADUnload == 2) {
        type = EEPolicy::ADU_Rude;
    }
#endif // _DEBUG

    EnableADUnloadWorker(type);
}

void AppDomain::EnableADUnloadWorker(EEPolicy::AppDomainUnloadTypes type, BOOL fHasStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT; // Called during a SO
    }
    CONTRACTL_END;

    FastInterlockOr (&s_WorkType, WT_UnloadDomain);

    LONG stage = m_Stage;
    static_assert_no_msg(sizeof(m_Stage) == sizeof(int));

    _ASSERTE(!IsDefaultDomain());

    // Mark unload requested.
    if (type == EEPolicy::ADU_Rude) {
        SetRudeUnload();
    }
    while (stage < STAGE_UNLOAD_REQUESTED) {
        stage = FastInterlockCompareExchange((LONG*)&m_Stage,STAGE_UNLOAD_REQUESTED,stage);
    }

    if (!fHasStack)
    {
        // Can not call Set due to limited stack.
        return;
    }
    LOG((LF_APPDOMAIN, LL_INFO10, "Enabling unload worker\n"));
    g_pUnloadStartEvent->Set();
}

void AppDomain::EnableADUnloadWorkerForThreadAbort()
{
    LIMITED_METHOD_CONTRACT;
    STRESS_LOG0(LF_ALWAYS, LL_ALWAYS, "Enabling unload worker for thread abort\n");
    LOG((LF_APPDOMAIN, LL_INFO10, "Enabling unload worker for thread abort\n"));
    FastInterlockOr (&s_WorkType, WT_ThreadAbort);
    g_pUnloadStartEvent->Set();
}


void AppDomain::EnableADUnloadWorkerForFinalizer()
{
    LIMITED_METHOD_CONTRACT;
    if (GetEEPolicy()->GetTimeout(OPR_FinalizerRun) != INFINITE)
    {
        LOG((LF_APPDOMAIN, LL_INFO10, "Enabling unload worker for Finalizer Thread\n"));
        FastInterlockOr (&s_WorkType, WT_FinalizerThread);
        g_pUnloadStartEvent->Set();
    }
}

void AppDomain::EnableADUnloadWorkerForCollectedADCleanup()
{
    LIMITED_METHOD_CONTRACT;
    LOG((LF_APPDOMAIN, LL_INFO10, "Enabling unload worker for collected domains\n"));
    FastInterlockOr (&s_WorkType, WT_ClearCollectedDomains);
    g_pUnloadStartEvent->Set();
}


void SystemDomain::ClearCollectedDomains()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
        
    AppDomain* pDomainsToClear=NULL;
    {
        CrstHolder lh(&m_DelayedUnloadCrst); 
        for (AppDomain** ppDomain=&m_pDelayedUnloadList;(*ppDomain)!=NULL; )
        {
            if ((*ppDomain)->m_Stage==AppDomain::STAGE_COLLECTED)
            {
                AppDomain* pAppDomain=*ppDomain;
                *ppDomain=(*ppDomain)->m_pNextInDelayedUnloadList;
                pAppDomain->m_pNextInDelayedUnloadList=pDomainsToClear;
                pDomainsToClear=pAppDomain;
            }
            else
                ppDomain=&((*ppDomain)->m_pNextInDelayedUnloadList);
        }
    }
        
    for (AppDomain* pDomain=pDomainsToClear;pDomain!=NULL;)
    {
        AppDomain* pNext=pDomain->m_pNextInDelayedUnloadList;
        pDomain->Close(); //NOTHROW!
        pDomain->Release();
        pDomain=pNext;
    }
}
 
void SystemDomain::ProcessClearingDomains()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;           
    }
    CONTRACTL_END;
    CrstHolder lh(&m_DelayedUnloadCrst); 

    for (AppDomain** ppDomain=&m_pDelayedUnloadList;(*ppDomain)!=NULL; )
    {
        if ((*ppDomain)->m_Stage==AppDomain::STAGE_HANDLETABLE_NOACCESS)
        {
            AppDomain* pAppDomain=*ppDomain;
            pAppDomain->SetStage(AppDomain::STAGE_CLEARED);
        }
        ppDomain=&((*ppDomain)->m_pNextInDelayedUnloadList);
    }
        
    if (!m_UnloadIsAsync)
    {
        // For synchronous mode, we are now done with the list.
        m_pDelayedUnloadList = NULL;    
    }
}

void SystemDomain::ProcessDelayedUnloadDomains()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;    

    int iGCRefPoint=GCHeap::GetGCHeap()->CollectionCount(GCHeap::GetGCHeap()->GetMaxGeneration());
    if (GCHeap::GetGCHeap()->IsConcurrentGCInProgress())
        iGCRefPoint--;

    BOOL bAppDomainToCleanup = FALSE;
    LoaderAllocator * pAllocatorsToDelete = NULL;

    {
        CrstHolder lh(&m_DelayedUnloadCrst); 

        for (AppDomain* pDomain=m_pDelayedUnloadList; pDomain!=NULL; pDomain=pDomain->m_pNextInDelayedUnloadList)
        {
            if (pDomain->m_Stage==AppDomain::STAGE_CLEARED)
            {
                // Compare with 0 to handle overflows gracefully
                if (0 < iGCRefPoint - pDomain->GetGCRefPoint())
                {
                    bAppDomainToCleanup=TRUE;
                    pDomain->SetStage(AppDomain::STAGE_COLLECTED);
                }
            }
        }

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

    if (bAppDomainToCleanup)
        AppDomain::EnableADUnloadWorkerForCollectedADCleanup();

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

AppDomainFromIDHolder::AppDomainFromIDHolder(ADID adId, BOOL bUnsafePoint, SyncType synctype)
{
    WRAPPER_NO_CONTRACT;
    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
#ifdef _DEBUG
    m_bAcquired=false;   
    m_bChecked=false;
    m_type=synctype;
    
#endif
    Assign(adId, bUnsafePoint);
}

AppDomainFromIDHolder::AppDomainFromIDHolder(SyncType synctype)
{
    LIMITED_METHOD_CONTRACT;
    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
    m_pDomain=NULL;
#ifdef _DEBUG
    m_bAcquired=false;
    m_bChecked=false;
    m_type=synctype;
#endif
}

#ifndef CROSSGEN_COMPILE
void ADUnloadSink::ReportUnloadResult (HRESULT hr, OBJECTREF* pException)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(m_UnloadCompleteEvent.IsValid());
    }
    CONTRACTL_END;

    //pException is unused;
    m_UnloadResult=hr;
    m_UnloadCompleteEvent.Set();
};

void ADUnloadSink::WaitUnloadCompletion()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(m_UnloadCompleteEvent.IsValid());
    }
    CONTRACTL_END;

    CONTRACT_VIOLATION(FaultViolation);
    m_UnloadCompleteEvent.WaitEx(INFINITE, (WaitMode)(WaitMode_Alertable | WaitMode_ADUnload));
};

ADUnloadSink* AppDomain::PrepareForWaitUnloadCompletion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(SystemDomain::IsUnderDomainLock());
        FORBID_FAULT;
    }
    CONTRACTL_END;

    ADUnloadSink* pADSink=GetADUnloadSink();
    PREFIX_ASSUME(pADSink!=NULL);
    if (m_Stage < AppDomain::STAGE_UNLOAD_REQUESTED) //we're first
    {
        pADSink->Reset();
        SetUnloadRequestThread(GetThread());
    }
    return pADSink;
};

ADUnloadSink::ADUnloadSink()
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    m_cRef=1;
    m_UnloadCompleteEvent.CreateManualEvent(FALSE);
    m_UnloadResult=S_OK;
};

ADUnloadSink::~ADUnloadSink()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_UnloadCompleteEvent.CloseEvent();

};


ULONG ADUnloadSink::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return InterlockedIncrement(&m_cRef);
};

ULONG ADUnloadSink::Release()
{
    LIMITED_METHOD_CONTRACT;
    ULONG ulRef = InterlockedDecrement(&m_cRef);
    if (ulRef == 0)
    {
        delete this;
        return 0;
    }
    return ulRef;
};

void ADUnloadSink::Reset()
{
    LIMITED_METHOD_CONTRACT;
    m_UnloadResult=S_OK;
    m_UnloadCompleteEvent.Reset();
}

ADUnloadSink* AppDomain::GetADUnloadSink()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(SystemDomain::IsUnderDomainLock());
    if(m_ADUnloadSink)
        m_ADUnloadSink->AddRef();
    return m_ADUnloadSink;
};

ADUnloadSink* AppDomain::GetADUnloadSinkForUnload()
{
    // unload thread only. Doesn't need to have AD lock
    LIMITED_METHOD_CONTRACT;
    if(m_ADUnloadSink)
        m_ADUnloadSink->AddRef();
    return m_ADUnloadSink;
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

    _ASSERTE(GCHeap::IsGCInProgress() &&
             GCHeap::IsServerHeap()   &&
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
        SO_TOLERANT;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(pMT->GetDomain() == this);
    } CONTRACTL_END;

    return m_typeIDMap.LookupTypeID(pMT);
}

//------------------------------------------------------------------------
PTR_MethodTable BaseDomain::LookupType(UINT32 id) {
    CONTRACTL {
        NOTHROW;
        SO_TOLERANT;
        WRAPPER(GC_TRIGGERS);
        CONSISTENCY_CHECK(id != TYPE_ID_THIS_CLASS);
    } CONTRACTL_END;

    PTR_MethodTable pMT = m_typeIDMap.LookupType(id);
    if (pMT == NULL && !IsSharedDomain()) {
        pMT = SharedDomain::GetDomain()->LookupType(id);
    }

    CONSISTENCY_CHECK(CheckPointer(pMT));
    CONSISTENCY_CHECK(pMT->IsInterface());
    return pMT;
}

#ifndef DACCESS_COMPILE

#ifndef FEATURE_CORECLR
//------------------------------------------------------------------------
DWORD* SetupCompatibilityFlags()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    LPCWSTR buf;
    bool return_null = true;

    FAULT_NOT_FATAL(); // we can simply give up

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(SetLastError(COR_E_STACKOVERFLOW); return NULL;)
    InlineSString<4> bufString;
    
    if (WszGetEnvironmentVariable(W("UnsupportedCompatSwitchesEnabled"), bufString) != 0)
    {
        buf = bufString.GetUnicode();
        if (buf[0] != '1' || buf[1] != '\0')
        {
            return_null = true;
        }
        else
        {
            return_null = false;
        }

    }
    END_SO_INTOLERANT_CODE

    if (return_null)
        return NULL;

    static const LPCWSTR rgFlagNames[] = {
#define COMPATFLAGDEF(name) TEXT(#name),
#include "compatibilityflagsdef.h"
    };

    int size = (compatCount+31) / 32;
    DWORD* pFlags = new (nothrow) DWORD[size];
    if (pFlags == NULL)
        return NULL;
    ZeroMemory(pFlags, size * sizeof(DWORD));

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(SetLastError(COR_E_STACKOVERFLOW); return NULL;)
    InlineSString<4> bufEnvString;
    for (int i = 0; i < COUNTOF(rgFlagNames); i++)
    {
        if (WszGetEnvironmentVariable(rgFlagNames[i], bufEnvString) == 0)
            continue;

        buf = bufEnvString.GetUnicode();
        if (buf[0] != '1' || buf[1] != '\0')
            continue;

        pFlags[i / 32] |= 1 << (i % 32);
    }
    END_SO_INTOLERANT_CODE
    
    return pFlags;
}

//------------------------------------------------------------------------
static VolatilePtr<DWORD> g_pCompatibilityFlags = (DWORD*)(-1);

DWORD* GetGlobalCompatibilityFlags()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    if (g_pCompatibilityFlags == (DWORD*)(-1))
    {
        DWORD *pCompatibilityFlags = SetupCompatibilityFlags();

        if (FastInterlockCompareExchangePointer(g_pCompatibilityFlags.GetPointer(), pCompatibilityFlags, reinterpret_cast<DWORD *>(-1)) != (VOID*)(-1))
        {
            delete [] pCompatibilityFlags;
        }
    }

    return g_pCompatibilityFlags;
}
#endif // !FEATURE_CORECLR

//------------------------------------------------------------------------
BOOL GetCompatibilityFlag(CompatibilityFlag flag)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

#ifndef FEATURE_CORECLR
    DWORD *pFlags = GetGlobalCompatibilityFlags();

    if (pFlags != NULL)
        return (pFlags[flag / 32] & (1 << (flag % 32))) ? TRUE : FALSE;
    else
        return FALSE;
#else // !FEATURE_CORECLR
    return FALSE;
#endif // !FEATURE_CORECLR
}
#endif // !DACCESS_COMPILE

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

        // Next, reject DomainAssemblies whose execution / introspection status is
        // not to be included in the enumeration
        
        if (pDomainAssembly->IsIntrospectionOnly())
        {
            // introspection assembly
            if (!(m_assemblyIterationFlags & kIncludeIntrospection))
            {
                continue; // reject
            }
        }
        else
        {
            // execution assembly
            if (!(m_assemblyIterationFlags & kIncludeExecution))
            {
                continue; // reject
            }
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
        (kIncludeLoaded | kIncludeLoading | kIncludeExecution | kIncludeIntrospection | kIncludeFailedToLoad | kIncludeCollected));
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

#ifdef FEATURE_CORECLR

//---------------------------------------------------------------------------------------
// 
BOOL AppDomain::IsImageFromTrustedPath(PEImage* pPEImage)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(CheckPointer(pPEImage));
    }
    CONTRACTL_END;

    BOOL fIsInGAC = FALSE;
    const SString &sImagePath = pPEImage->GetPath();

    if (!sImagePath.IsEmpty())
    {
        // If we're not in a sandboxed domain, everything is full trust all the time
        if (GetSecurityDescriptor()->IsFullyTrusted())
        {
            return TRUE;
        }
        
        fIsInGAC = GetTPABinderContext()->IsInTpaList(sImagePath);
    }

    return fIsInGAC;
}

BOOL AppDomain::IsImageFullyTrusted(PEImage* pPEImage)
{
    WRAPPER_NO_CONTRACT;
    return IsImageFromTrustedPath(pPEImage);
}

#endif //FEATURE_CORECLR

#endif //!DACCESS_COMPILE

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

// Returns a BOOL indicating if the binding model has been locked for the AppDomain
BOOL AppDomain::IsBindingModelLocked()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    return m_fIsBindingModelLocked.Load();
}

// Marks the binding model locked for AppDomain
BOOL AppDomain::LockBindingModel()
{
    LIMITED_METHOD_CONTRACT;
    
    BOOL fDidWeLockBindingModel = FALSE;
    
    if (InterlockedCompareExchangeT<BOOL>(&m_fIsBindingModelLocked, TRUE, FALSE) == FALSE)
    {
        fDidWeLockBindingModel = TRUE;
    }
    
    return fDidWeLockBindingModel;
}

BOOL AppDomain::IsHostAssemblyResolverInUse()
{
    LIMITED_METHOD_CONTRACT;
    
    return (GetFusionContext() != GetTPABinderContext());
}

// Helper used by the assembly binder to check if the specified AppDomain can use apppath assembly resolver
BOOL RuntimeCanUseAppPathAssemblyResolver(DWORD adid)
{
    CONTRACTL
    {
        NOTHROW; // Cannot throw since it is invoked by the Binder that expects to get a hresult
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ADID id(adid);

    // We need to be in COOP mode to get the AppDomain*
    GCX_COOP();
        
    AppDomain *pTargetDomain = SystemDomain::GetAppDomainFromId(id, ADV_CURRENTAD);
    _ASSERTE(pTargetDomain != NULL);

    pTargetDomain->LockBindingModel();
        
    return !pTargetDomain->IsHostAssemblyResolverInUse();
}

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
                if (!fResolvedAssembly)
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
            
            if (!fResolvedAssembly)
            {
                // Step 4 (of CLRPrivBinderAssemblyLoadContext::BindUsingAssemblyName)
                //
                // If we couldnt resolve the assembly using TPA LoadContext as well, then
                // attempt to resolve it using the Resolving event.
                // Finally, setup arguments for invocation
                BinderMethodID idHAR_ResolveUsingEvent = METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUSINGEVENT;
                MethodDescCallSite methLoadAssembly(idHAR_ResolveUsingEvent);
                
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
        
        GCPROTECT_END();
    }
    // EX_CATCH_HRESULT(hr);
    
    return hr;
    
}
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

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
DomainLocalBlock::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    // Block is contained in AppDomain, don't enum this.

    if (m_pModuleSlots.IsValid())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_pModuleSlots),
                            m_aModuleIndices * sizeof(TADDR));

        for (SIZE_T i = 0; i < m_aModuleIndices; i++)
        {
            PTR_DomainLocalModule domMod = m_pModuleSlots[i];
            if (domMod.IsValid())
            {
                domMod->EnumMemoryRegions(flags);
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
    AssemblyIterator assem = IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution | kIncludeIntrospection));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    
    while (assem.Next(pDomainAssembly.This()))
    {
        pDomainAssembly->EnumMemoryRegions(flags);
    }

    m_sDomainLocalBlock.EnumMemoryRegions(flags);

    m_LoaderAllocator.EnumMemoryRegions(flags);
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
    if (m_pDefaultDomain.IsValid())
    {
        m_pDefaultDomain->EnumMemoryRegions(flags, true);
    }

    m_appDomainIndexList.EnumMem();
    (&m_appDomainIndexList)->EnumMemoryRegions(flags);
}

void
SharedDomain::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                bool enumThis)
{
    SUPPORTS_DAC;
    if (enumThis)
    {
        DAC_ENUM_VTHIS();
    }
    BaseDomain::EnumMemoryRegions(flags, false);
#ifdef FEATURE_LOADER_OPTIMIZATION
    m_assemblyMap.EnumMemoryRegions(flags);
    SharedAssemblyIterator assem;
    while (assem.Next())
    {
        assem.GetAssembly()->EnumMemoryRegions(flags);
    }
#endif    
}

#endif //DACCESS_COMPILE


PTR_LoaderAllocator SystemDomain::GetGlobalLoaderAllocator()
{
    return PTR_LoaderAllocator(PTR_HOST_MEMBER_TADDR(SystemDomain,System(),m_GlobalAllocator));
}

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING

#ifndef CROSSGEN_COMPILE
// Return the total processor time (user and kernel) used by threads executing in this AppDomain so far. The
// result is in 100ns units.
ULONGLONG AppDomain::QueryProcessorUsage()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    Thread *pThread = NULL;

    // Need to update our accumulated processor time count with current values from each thread that is
    // currently executing in this domain.

    // Take the thread store lock while we enumerate threads.
    ThreadStoreLockHolder tsl;
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        // Skip unstarted and dead threads and those that are currently executing in a different AppDomain.
        if (pThread->IsUnstarted() || pThread->IsDead() || pThread->GetDomain(INDEBUG(TRUE)) != this)
            continue;

        // Add the amount of time spent by the thread in the AppDomain since the last time we asked (calling
        // Thread::QueryThreadProcessorUsage() will reset the thread's counter).
        UpdateProcessorUsage(pThread->QueryThreadProcessorUsage());
    }
#endif // !DACCESS_COMPILE

    // Return the updated total.
    return m_ullTotalProcessorUsage;
}

// Add to the current count of processor time used by threads within this AppDomain. This API is called by
// threads transitioning between AppDomains.
void AppDomain::UpdateProcessorUsage(ULONGLONG ullAdditionalUsage)
{
    LIMITED_METHOD_CONTRACT;

    // Need to be careful to synchronize here, multiple threads could be racing to update this count.
    ULONGLONG ullOldValue;
    ULONGLONG ullNewValue;
    do
    {
        ullOldValue = m_ullTotalProcessorUsage;
        ullNewValue = ullOldValue + ullAdditionalUsage;
    } while (InterlockedCompareExchange64((LONGLONG*)&m_ullTotalProcessorUsage,
                                          (LONGLONG)ullNewValue,
                                          (LONGLONG)ullOldValue) != (LONGLONG)ullOldValue);
}
#endif // CROSSGEN_COMPILE

#endif // FEATURE_APPDOMAIN_RESOURCE_MONITORING

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
            m_pTypeEquivalenceTable = TypeEquivalenceHashTable::Create(this, 12, &m_TypeEquivalenceCrst);
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
#ifdef FEATURE_APPX_BINDER
        // In AppX processes, all PEAssemblies that are reach this stage should have host binders.
        _ASSERTE(!AppX::IsAppXProcess());
#endif
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
#ifdef FEATURE_APPX_BINDER
        // In AppX processes, all PEAssemblies that are reach this stage should have host binders.
        _ASSERTE(!AppX::IsAppXProcess());
#endif

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

#if defined(FEATURE_CORECLR) && defined(FEATURE_COMINTEROP)
HRESULT AppDomain::SetWinrtApplicationContext(SString &appLocalWinMD)
{
    STANDARD_VM_CONTRACT;
    
    _ASSERTE(WinRTSupported());
    _ASSERTE(m_pWinRtBinder != nullptr);

    _ASSERTE(GetTPABinderContext() != NULL);
    BINDER_SPACE::ApplicationContext *pApplicationContext = GetTPABinderContext()->GetAppContext();
    _ASSERTE(pApplicationContext != NULL);
    
    return m_pWinRtBinder->SetApplicationContext(pApplicationContext, appLocalWinMD);
}

#endif // FEATURE_CORECLR && FEATURE_COMINTEROP

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

#if !defined(DACCESS_COMPILE) && defined(FEATURE_CORECLR) && defined(FEATURE_NATIVE_IMAGE_GENERATION)

void ZapperSetBindingPaths(ICorCompilationDomain *pDomain, SString &trustedPlatformAssemblies, SString &platformResourceRoots, SString &appPaths, SString &appNiPaths)
{
    CLRPrivBinderCoreCLR *pBinder = static_cast<CLRPrivBinderCoreCLR*>(((CompilationDomain *)pDomain)->GetFusionContext());
    _ASSERTE(pBinder != NULL);
    pBinder->SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, appNiPaths);
#ifdef FEATURE_COMINTEROP
    SString emptString;
    ((CompilationDomain*)pDomain)->SetWinrtApplicationContext(emptString);
#endif
}

#endif

#if defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
bool IsSingleAppDomain()
{
    STARTUP_FLAGS flags = CorHost2::GetStartupFlags();
    if(flags & STARTUP_SINGLE_APPDOMAIN)
        return TRUE;
    else
        return FALSE;
}
#else
bool IsSingleAppDomain()
{
    return FALSE;
}
#endif
