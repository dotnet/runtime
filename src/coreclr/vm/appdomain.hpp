// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  AppDomain.cpp
**

**
** Purpose: Implements AppDomain (loader domain) architecture
**
**
===========================================================*/
#ifndef _APPDOMAIN_H
#define _APPDOMAIN_H

#include "eventtrace.h"
#include "assembly.hpp"
#include "clsload.hpp"
#include "eehash.h"
#include "arraylist.h"
#include "comreflectioncache.hpp"
#include "comutilnative.h"
#include "domainassembly.h"
#include "fptrstubs.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"
#include "rejit.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#include "tieredcompilation.h"

#include "codeversion.h"

class BaseDomain;
class SystemDomain;
class AppDomain;
class GlobalStringLiteralMap;
class StringLiteralMap;
class FrozenObjectHeapManager;
class MngStdInterfacesInfo;
class DomainAssembly;
class LoadLevelLimiter;
class TypeEquivalenceHashTable;

#ifdef FEATURE_COMINTEROP
class RCWCache;
#endif //FEATURE_COMINTEROP
#ifdef FEATURE_COMWRAPPERS
class RCWRefCache;
#endif // FEATURE_COMWRAPPERS

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4200) // Disable zero-sized array warning
#endif


#ifdef _MSC_VER
#pragma warning(pop)
#endif


// The pinned heap handle bucket class is used to contain handles allocated
// from an array contained in the pinned heap.
class PinnedHeapHandleBucket
{
public:
    // Constructor and desctructor.
    PinnedHeapHandleBucket(PinnedHeapHandleBucket *pNext, PTRARRAYREF pinnedHandleArrayObj, DWORD size, BaseDomain *pDomain);
    ~PinnedHeapHandleBucket();

    // This returns the next bucket.
    PinnedHeapHandleBucket *GetNext()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pNext;
    }

    // This returns the number of remaining handle slots.
    DWORD GetNumRemainingHandles()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ArraySize - m_CurrentPos;
    }

    void ConsumeRemaining()
    {
        LIMITED_METHOD_CONTRACT;

        m_CurrentPos = m_ArraySize;
    }

    OBJECTREF *TryAllocateEmbeddedFreeHandle();

    // Allocate handles from the bucket.
    OBJECTREF* AllocateHandles(DWORD nRequested);
    OBJECTREF* CurrentPos()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pArrayDataPtr + m_CurrentPos;
    }

    void EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

private:
    PinnedHeapHandleBucket *m_pNext;
    int m_ArraySize;
    int m_CurrentPos;
    int m_CurrentEmbeddedFreePos;
    OBJECTHANDLE m_hndHandleArray;
    OBJECTREF *m_pArrayDataPtr;
};



// The pinned heap handle table is used to allocate handles that are pointers
// to objects stored in an array in the pinned object heap.
class PinnedHeapHandleTable
{
public:
    // Constructor and desctructor.
    PinnedHeapHandleTable(BaseDomain *pDomain, DWORD InitialBucketSize);
    ~PinnedHeapHandleTable();

    // Allocate handles from the pinned heap handle table.
    OBJECTREF* AllocateHandles(DWORD nRequested);

    // Release object handles allocated using AllocateHandles().
    void ReleaseHandles(OBJECTREF *pObjRef, DWORD nReleased);

    void EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

private:
    void ReleaseHandlesLocked(OBJECTREF *pObjRef, DWORD nReleased);

    // The buckets of object handles.
    // synchronized by m_Crst
    PinnedHeapHandleBucket *m_pHead;

    // We need to know the containing domain so we know where to allocate handles
    BaseDomain *m_pDomain;

    // The size of the PinnedHeapHandleBucket.
    // synchronized by m_Crst
    DWORD m_NextBucketSize;

    // for finding and re-using embedded free items in the list
    // these fields are synchronized by m_Crst
    PinnedHeapHandleBucket *m_pFreeSearchHint;
    DWORD m_cEmbeddedFree;

    CrstExplicitInit m_Crst;
};

class PinnedHeapHandleBlockHolder;
void PinnedHeapHandleBlockHolder__StaticFree(PinnedHeapHandleBlockHolder*);


class PinnedHeapHandleBlockHolder:public Holder<PinnedHeapHandleBlockHolder*,DoNothing,PinnedHeapHandleBlockHolder__StaticFree>

{
    PinnedHeapHandleTable* m_pTable;
    DWORD m_Count;
    OBJECTREF* m_Data;
public:
    FORCEINLINE PinnedHeapHandleBlockHolder(PinnedHeapHandleTable* pOwner, DWORD nCount)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        m_Data = pOwner->AllocateHandles(nCount);
        m_Count=nCount;
        m_pTable=pOwner;
    };

    FORCEINLINE void FreeData()
    {
        WRAPPER_NO_CONTRACT;
        for (DWORD i=0;i< m_Count;i++)
            ClearObjectReference(m_Data+i);
        m_pTable->ReleaseHandles(m_Data, m_Count);
    };
    FORCEINLINE OBJECTREF* operator[] (DWORD idx)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(idx<m_Count);
        return &(m_Data[idx]);
    }
};

FORCEINLINE  void PinnedHeapHandleBlockHolder__StaticFree(PinnedHeapHandleBlockHolder* pHolder)
{
    WRAPPER_NO_CONTRACT;
    pHolder->FreeData();
};





// The large heap handle bucket class is used to contain handles allocated
// from an array contained in the large heap.
class ThreadStaticHandleBucket
{
public:
    // Constructor and desctructor.
    ThreadStaticHandleBucket(ThreadStaticHandleBucket *pNext, DWORD Size, BaseDomain *pDomain);
    ~ThreadStaticHandleBucket();

    // This returns the next bucket.
    ThreadStaticHandleBucket *GetNext()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pNext;
    }

    // Allocate handles from the bucket.
    OBJECTHANDLE GetHandles();

private:
    ThreadStaticHandleBucket *m_pNext;
    int m_ArraySize;
    OBJECTHANDLE m_hndHandleArray;
};


// The large heap handle table is used to allocate handles that are pointers
// to objects stored in an array in the large object heap.
class ThreadStaticHandleTable
{
public:
    // Constructor and desctructor.
    ThreadStaticHandleTable(BaseDomain *pDomain);
    ~ThreadStaticHandleTable();

    // Allocate handles from the large heap handle table.
    OBJECTHANDLE AllocateHandles(DWORD nRequested);

private:
    // The buckets of object handles.
    ThreadStaticHandleBucket *m_pHead;

    // We need to know the containing domain so we know where to allocate handles
    BaseDomain *m_pDomain;
};




//--------------------------------------------------------------------------------------
// Base class for domains. It provides an abstract way of finding the first assembly and
// for creating assemblies in the domain. The system domain only has one assembly, it
// contains the classes that are logically shared between domains. All other domains can
// have multiple assemblies. Iteration is done be getting the first assembly and then
// calling the Next() method on the assembly.
//
// The system domain should be as small as possible, it includes object, exceptions, etc.
// which are the basic classes required to load other assemblies. All other classes
// should be loaded into the domain. Of coarse there is a trade off between loading the
// same classes multiple times, requiring all domains to load certain assemblies (working
// set) and being able to specify specific versions.
//

#define LOW_FREQUENCY_HEAP_RESERVE_SIZE        (3 * GetOsPageSize())
#define LOW_FREQUENCY_HEAP_COMMIT_SIZE         (1 * GetOsPageSize())

#define HIGH_FREQUENCY_HEAP_RESERVE_SIZE       (10 * GetOsPageSize())
#define HIGH_FREQUENCY_HEAP_COMMIT_SIZE        (1 * GetOsPageSize())

#define STUB_HEAP_RESERVE_SIZE                 (3 * GetOsPageSize())
#define STUB_HEAP_COMMIT_SIZE                  (1 * GetOsPageSize())

// --------------------------------------------------------------------------------
// PE File List lock - for creating list locks on PE files
// --------------------------------------------------------------------------------

class PEFileListLock : public ListLock
{
public:
#ifndef DACCESS_COMPILE
    ListLockEntry *FindFileLock(PEAssembly *pPEAssembly)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_FORBID_FAULT;

        PRECONDITION(HasLock());

        ListLockEntry *pEntry;

        for (pEntry = m_pHead;
             pEntry != NULL;
             pEntry = pEntry->m_pNext)
        {
            if (((PEAssembly *)pEntry->m_data)->Equals(pPEAssembly))
            {
                return pEntry;
            }
        }

        return NULL;
    }
#endif // DACCESS_COMPILE

    DEBUG_NOINLINE static void HolderEnter(PEFileListLock *pThis)
    {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

        pThis->Enter();
    }

    DEBUG_NOINLINE static void HolderLeave(PEFileListLock *pThis)
    {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

        pThis->Leave();
    }

    typedef Wrapper<PEFileListLock*, PEFileListLock::HolderEnter, PEFileListLock::HolderLeave> Holder;
};

typedef PEFileListLock::Holder PEFileListLockHolder;

// Loading infrastructure:
//
// a DomainAssembly is a file being loaded.  Files are loaded in layers to enable loading in the
// presence of dependency loops.
//
// FileLoadLevel describes the various levels available.  These are implemented slightly
// differently for assemblies and modules, but the basic structure is the same.
//
// LoadLock and FileLoadLock form the ListLock data structures for files. The FileLoadLock
// is specialized in that it allows taking a lock at a particular level.  Basicall any
// thread may obtain the lock at a level at which the file has previously been loaded to, but
// only one thread may obtain the lock at its current level.
//
// The PendingLoadQueue is a per thread data structure which serves two purposes.  First, it
// holds a "load limit" which automatically restricts the level of recursive loads to be
// one less than the current load which is preceding.  This, together with the AppDomain
// LoadLock level behavior, will prevent any deadlocks from occurring due to circular
// dependencies.  (Note that it is important that the loading logic understands this restriction,
// and any given level of loading must deal with the fact that any recursive loads will be partially
// unfulfilled in a specific way.)
//
// The second function is to queue up any unfulfilled load requests for the thread.  These
// are then delivered immediately after the current load request is dealt with.

class FileLoadLock : public ListLockEntry
{
private:
    FileLoadLevel           m_level;
    DomainAssembly          *m_pDomainAssembly;
    HRESULT                 m_cachedHR;

public:
    static FileLoadLock *Create(PEFileListLock *pLock, PEAssembly *pPEAssembly, DomainAssembly *pDomainAssembly);

    ~FileLoadLock();
    DomainAssembly *GetDomainAssembly();
    FileLoadLevel GetLoadLevel();

    // CanAcquire will return FALSE if Acquire will definitely not take the lock due
    // to levels or deadlock.
    // (Note that there is a race exiting from the function, where Acquire may end
    // up not taking the lock anyway if another thread did work in the meantime.)
    BOOL CanAcquire(FileLoadLevel targetLevel);

    // Acquire will return FALSE and not take the lock if the file
    // has already been loaded to the target level.  Otherwise,
    // it will return TRUE and take the lock.
    //
    // Note that the taker must release the lock via IncrementLoadLevel.
    BOOL Acquire(FileLoadLevel targetLevel);

    // CompleteLoadLevel can be called after Acquire returns true
    // returns TRUE if it updated load level, FALSE if the level was set already
    BOOL CompleteLoadLevel(FileLoadLevel level, BOOL success);

    void SetError(Exception *ex);

    void AddRef();
    UINT32 Release() DAC_EMPTY_RET(0);

private:

    FileLoadLock(PEFileListLock *pLock, PEAssembly *pPEAssembly, DomainAssembly *pDomainAssembly);

    static void HolderLeave(FileLoadLock *pThis);

public:
    typedef Wrapper<FileLoadLock *, DoNothing, FileLoadLock::HolderLeave> Holder;

};

typedef FileLoadLock::Holder FileLoadLockHolder;

#ifndef DACCESS_COMPILE
    typedef ReleaseHolder<FileLoadLock> FileLoadLockRefHolder;
#endif // DACCESS_COMPILE

    typedef ListLockBase<NativeCodeVersion> JitListLock;
    typedef ListLockEntryBase<NativeCodeVersion> JitListLockEntry;


#ifdef _MSC_VER
#pragma warning(push)
#pragma warning (disable: 4324) //sometimes 64bit compilers complain about alignment
#endif
class LoadLevelLimiter
{
    FileLoadLevel                   m_currentLevel;
    LoadLevelLimiter* m_previousLimit;
    BOOL m_bActive;

public:

    LoadLevelLimiter()
      : m_currentLevel(FILE_ACTIVE),
      m_previousLimit(NULL),
      m_bActive(FALSE)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void Activate()
    {
        WRAPPER_NO_CONTRACT;
        m_previousLimit= GetThread()->GetLoadLevelLimiter();
        if(m_previousLimit)
            m_currentLevel=m_previousLimit->GetLoadLevel();
        GetThread()->SetLoadLevelLimiter(this);
        m_bActive=TRUE;
    }

    void Deactivate()
    {
        WRAPPER_NO_CONTRACT;
        if (m_bActive)
        {
            GetThread()->SetLoadLevelLimiter(m_previousLimit);
            m_bActive=FALSE;
        }
    }

    ~LoadLevelLimiter()
    {
        WRAPPER_NO_CONTRACT;

        // PendingLoadQueues are allocated on the stack during a load, and
        // shared with all nested loads on the same thread.

        // Make sure the thread pointer gets reset after the
        // top level queue goes out of scope.
        if(m_bActive)
        {
            Deactivate();
        }
    }

    FileLoadLevel GetLoadLevel()
    {
        LIMITED_METHOD_CONTRACT;
        return m_currentLevel;
    }

    void SetLoadLevel(FileLoadLevel level)
    {
        LIMITED_METHOD_CONTRACT;
        m_currentLevel = level;
    }
};
#ifdef _MSC_VER
#pragma warning (pop) //4324
#endif

#define OVERRIDE_LOAD_LEVEL_LIMIT(newLimit)                    \
    LoadLevelLimiter __newLimit;                                                    \
    __newLimit.Activate();                                                              \
    __newLimit.SetLoadLevel(newLimit);

// A BaseDomain much basic information in a code:AppDomain including
//
//    * code:#AppdomainHeaps - Heaps for any data structures that will be freed on appdomain unload
//
class BaseDomain
{
    friend class Assembly;
    friend class AssemblySpec;
    friend class AppDomain;

    VPTR_BASE_VTABLE_CLASS(BaseDomain)
    VPTR_UNIQUE(VPTR_UNIQUE_BaseDomain)

public:

    class AssemblyIterator;
    friend class AssemblyIterator;

    // Static initialization.
    static void Attach();

    //****************************************************************************************
    //
    // Initialization/shutdown routines for every instance of an BaseDomain.

    BaseDomain();
    virtual ~BaseDomain() {}
    void Init();
    void Stop();

    virtual BOOL IsAppDomain()    { LIMITED_METHOD_DAC_CONTRACT; return FALSE; }

    PTR_LoaderAllocator GetLoaderAllocator();
    virtual PTR_AppDomain AsAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(!"Not an AppDomain");
        return NULL;
    }

#ifdef FEATURE_COMINTEROP
    MngStdInterfacesInfo * GetMngStdInterfacesInfo()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pMngStdInterfacesInfo;
    }
#endif // FEATURE_COMINTEROP
#ifdef _DEBUG
    BOOL OwnDomainLocalBlockLock()
    {
        WRAPPER_NO_CONTRACT;

        return m_DomainLocalBlockCrst.OwnedByCurrentThread();
    }
#endif

    //****************************************************************************************
    // Get the class init lock. The method is limited to friends because inappropriate use
    // will cause deadlocks in the system
    ListLock*  GetClassInitLock()
    {
        LIMITED_METHOD_CONTRACT;

        return &m_ClassInitLock;
    }

    JitListLock* GetJitLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_JITLock;
    }

    ListLock* GetILStubGenLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_ILStubGenLock;
    }

    ListLock* GetNativeTypeLoadLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_NativeTypeLoadLock;
    }

    STRINGREF *IsStringInterned(STRINGREF *pString);
    STRINGREF *GetOrInternString(STRINGREF *pString);

    // Returns an array of OBJECTREF* that can be used to store domain specific data.
    // Statics and reflection info (Types, MemberInfo,..) are stored this way
    // If ppLazyAllocate != 0, allocation will only take place if *ppLazyAllocate != 0 (and the allocation
    // will be properly serialized)
    OBJECTREF *AllocateObjRefPtrsInLargeTable(int nRequested, OBJECTREF** ppLazyAllocate = NULL, MethodTable *pMTToFillWithStaticBoxes = NULL);

    //****************************************************************************************
    // Handles

#if !defined(DACCESS_COMPILE)
    IGCHandleStore* GetHandleStore()
    {
        LIMITED_METHOD_CONTRACT;
        return m_handleStore;
    }

    OBJECTHANDLE CreateTypedHandle(OBJECTREF object, HandleType type)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateHandleCommon(m_handleStore, object, type);
    }

    OBJECTHANDLE CreateHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL)
        return ::CreateHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateWeakHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateShortWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateShortWeakHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateLongWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL)
        return ::CreateLongWeakHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateStrongHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateStrongHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreatePinningHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreatePinningHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateSizedRefHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        OBJECTHANDLE h;
        if (GCHeapUtilities::IsServerHeap())
        {
            h = ::CreateSizedRefHandle(m_handleStore, object, m_dwSizedRefHandles % m_iNumberOfProcessors);
        }
        else
        {
            h = ::CreateSizedRefHandle(m_handleStore, object);
        }

        InterlockedIncrement((LONG*)&m_dwSizedRefHandles);
        return h;
    }

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    OBJECTHANDLE CreateRefcountedHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateRefcountedHandle(m_handleStore, object);
    }
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

    OBJECTHANDLE CreateDependentHandle(OBJECTREF primary, OBJECTREF secondary)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateDependentHandle(m_handleStore, primary, secondary);
    }

#endif // DACCESS_COMPILE

    DefaultAssemblyBinder *GetDefaultBinder() {LIMITED_METHOD_CONTRACT;  return m_pDefaultBinder; }

    CrstExplicitInit * GetLoaderAllocatorReferencesLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_crstLoaderAllocatorReferences;
    }

    CrstExplicitInit* GetStaticBoxInitLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_crstStaticBoxInitLock;
    }

    static CrstStatic* GetMethodTableExposedClassObjectLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_MethodTableExposedClassObjectCrst;
    }

    void AssertLoadLockHeld()
    {
        _ASSERTE(m_FileLoadLock.HasLock());
    }

protected:

    //****************************************************************************************
    // Helper method to initialize the large heap handle table.
    void InitPinnedHeapHandleTable();

    // Critical sections & locks
    PEFileListLock   m_FileLoadLock;            // Protects the list of assemblies in the domain
    CrstExplicitInit m_DomainCrst;              // General Protection for the Domain
    CrstExplicitInit m_DomainCacheCrst;         // Protects the Assembly and Unmanaged caches
    CrstExplicitInit m_DomainLocalBlockCrst;
    // Used to protect the reference lists in the collectible loader allocators attached to this appdomain
    CrstExplicitInit m_crstLoaderAllocatorReferences;
    CrstExplicitInit m_crstStaticBoxInitLock;

    //#AssemblyListLock
    // Used to protect the assembly list. Taken also by GC or debugger thread, therefore we have to avoid
    // triggering GC while holding this lock (by switching the thread to GC_NOTRIGGER while it is held).
    CrstExplicitInit m_crstAssemblyList;
    ListLock         m_ClassInitLock;
    JitListLock      m_JITLock;
    ListLock         m_ILStubGenLock;
    ListLock         m_NativeTypeLoadLock;

    DefaultAssemblyBinder *m_pDefaultBinder; // Reference to the binding context that holds TPA list details

    IGCHandleStore* m_handleStore;

    // The pinned heap handle table.
    PinnedHeapHandleTable       *m_pPinnedHeapHandleTable;

#ifdef FEATURE_COMINTEROP
    // Information regarding the managed standard interfaces.
    MngStdInterfacesInfo        *m_pMngStdInterfacesInfo;
#endif // FEATURE_COMINTEROP

    // Protects allocation of slot IDs for thread statics
    static CrstStatic m_MethodTableExposedClassObjectCrst;

public:
    // Only call this routine when you can guarantee there are no
    // loads in progress.
    void ClearBinderContext();

    //****************************************************************************************
    // Synchronization holders.

    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(BaseDomain *pD)
            : CrstHolder(&pD->m_DomainCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
    friend class LockHolder;

    // To be used when the thread will remain in preemptive GC mode while holding the lock
    class DomainCacheCrstHolderForGCPreemp : private CrstHolder
    {
    public:
        DomainCacheCrstHolderForGCPreemp(BaseDomain *pD)
            : CrstHolder(&pD->m_DomainCacheCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    // To be used when the thread may enter cooperative GC mode while holding the lock. The thread enters a
    // forbid-suspend-for-debugger region along with acquiring the lock, such that it would not suspend for the debugger while
    // holding the lock, as that may otherwise cause a FuncEval to deadlock when trying to acquire the lock.
    class DomainCacheCrstHolderForGCCoop : private CrstAndForbidSuspendForDebuggerHolder
    {
    public:
        DomainCacheCrstHolderForGCCoop(BaseDomain *pD)
            : CrstAndForbidSuspendForDebuggerHolder(&pD->m_DomainCacheCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    class DomainLocalBlockLockHolder : public CrstHolder
    {
    public:
        DomainLocalBlockLockHolder(BaseDomain *pD)
            : CrstHolder(&pD->m_DomainLocalBlockCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
    friend class DomainLocalBlockLockHolder;

    class LoadLockHolder :  public PEFileListLockHolder
    {
    public:
        LoadLockHolder(BaseDomain *pD, BOOL Take = TRUE)
          : PEFileListLockHolder(&pD->m_FileLoadLock, Take)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                CAN_TAKE_LOCK;
            }
            CONTRACTL_END;
        }
    };
    friend class LoadLockHolder;
public:
    void InitVSD();

private:
    TypeIDMap m_typeIDMap;

public:


    UINT32 GetTypeID(PTR_MethodTable pMT);
    UINT32 LookupTypeID(PTR_MethodTable pMT);
    PTR_MethodTable LookupType(UINT32 id);
#ifndef DACCESS_COMPILE
    void RemoveTypesFromTypeIDMap(LoaderAllocator* pLoaderAllocator);
#endif // DACCESS_COMPILE

private:
    // I have yet to figure out an efficient way to get the number of handles
    // of a particular type that's currently used by the process without
    // spending more time looking at the handle table code. We know that
    // our only customer (asp.net) in Dev10 is not going to create many of
    // these handles so I am taking a shortcut for now and keep the sizedref
    // handle count on the AD itself.
    DWORD m_dwSizedRefHandles;

    static int m_iNumberOfProcessors;

public:
    // Called by DestroySizedRefHandle
    void DecNumSizedRefHandles()
    {
        WRAPPER_NO_CONTRACT;
        LONG result;
        result = InterlockedDecrement((LONG*)&m_dwSizedRefHandles);
        _ASSERTE(result >= 0);
    }

    DWORD GetNumSizedRefHandles()
    {
        return m_dwSizedRefHandles;
    }

#ifdef FEATURE_CODE_VERSIONING
private:
    CodeVersionManager m_codeVersionManager;

public:
    CodeVersionManager* GetCodeVersionManager() { return &m_codeVersionManager; }
#endif //FEATURE_CODE_VERSIONING

#ifdef DACCESS_COMPILE
public:
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags, bool enumThis) = 0;
#endif

};  // class BaseDomain

enum
{
    ATTACH_ASSEMBLY_LOAD = 0x1,
    ATTACH_MODULE_LOAD = 0x2,
    ATTACH_CLASS_LOAD = 0x4,

    ATTACH_ALL = 0x7
};

// This filters the output of IterateAssemblies. This ought to be declared more locally
// but it would result in really verbose callsites.
//
// Assemblies can be categorized by their load status (loaded, loading, or loaded just
// enough that they would be made available to profilers)
// Independently, they can also be categorized as execution or introspection.
//
// An assembly will be included in the results of IterateAssemblies only if
// the appropriate bit is set for *both* characterizations.
//
// The flags can be combined so if you want all loaded assemblies, you must specify:
//
///     kIncludeLoaded|kIncludeExecution

enum AssemblyIterationFlags
{
    // load status flags
    kIncludeLoaded        = 0x00000001, // include assemblies that are already loaded
                                        // (m_level >= code:FILE_LOAD_DELIVER_EVENTS)
    kIncludeLoading       = 0x00000002, // include assemblies that are still in the process of loading
                                        // (all m_level values)
    kIncludeAvailableToProfilers
                          = 0x00000020, // include assemblies available to profilers
                                        // See comment at code:DomainAssembly::IsAvailableToProfilers

    // Execution / introspection flags
    kIncludeExecution     = 0x00000004, // include assemblies that are loaded for execution only

    kIncludeFailedToLoad  = 0x00000010, // include assemblies that failed to load

    // Collectible assemblies flags
    kExcludeCollectible   = 0x00000040, // Exclude all collectible assemblies
    kIncludeCollected     = 0x00000080,
        // Include assemblies which were collected and cannot be referenced anymore. Such assemblies are not
        // AddRef-ed. Any manipulation with them should be protected by code:GetAssemblyListLock.
        // Should be used only by code:LoaderAllocator::GCLoaderAllocators.

};  // enum AssemblyIterationFlags

//---------------------------------------------------------------------------------------
//
// Base class for holder code:CollectibleAssemblyHolder (see code:HolderBase).
// Manages AddRef/Release for collectible assemblies. It is no-op for 'normal' non-collectible assemblies.
//
// Each type of type parameter needs 2 methods implemented:
//  code:CollectibleAssemblyHolderBase::GetLoaderAllocator
//  code:CollectibleAssemblyHolderBase::IsCollectible
//
template<typename _Type>
class CollectibleAssemblyHolderBase
{
protected:
    _Type m_value;
public:
    CollectibleAssemblyHolderBase(const _Type & value = NULL)
    {
        LIMITED_METHOD_CONTRACT;
        m_value = value;
    }
    void DoAcquire()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        // We don't need to keep the assembly alive in DAC - see code:#CAH_DAC
#ifndef DACCESS_COMPILE
        if (this->IsCollectible(m_value))
        {
            LoaderAllocator * pLoaderAllocator = GetLoaderAllocator(m_value);
            pLoaderAllocator->AddReference();
        }
#endif //!DACCESS_COMPILE
    }
    void DoRelease()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

#ifndef DACCESS_COMPILE
        if (this->IsCollectible(m_value))
        {
            LoaderAllocator * pLoaderAllocator = GetLoaderAllocator(m_value);
            pLoaderAllocator->Release();
        }
#endif //!DACCESS_COMPILE
    }

private:
    LoaderAllocator * GetLoaderAllocator(DomainAssembly * pDomainAssembly)
    {
        WRAPPER_NO_CONTRACT;
        return pDomainAssembly->GetLoaderAllocator();
    }
    BOOL IsCollectible(DomainAssembly * pDomainAssembly)
    {
        WRAPPER_NO_CONTRACT;
        return pDomainAssembly->IsCollectible();
    }
    LoaderAllocator * GetLoaderAllocator(Assembly * pAssembly)
    {
        WRAPPER_NO_CONTRACT;
        return pAssembly->GetLoaderAllocator();
    }
    BOOL IsCollectible(Assembly * pAssembly)
    {
        WRAPPER_NO_CONTRACT;
        return pAssembly->IsCollectible();
    }
};  // class CollectibleAssemblyHolderBase<>

//---------------------------------------------------------------------------------------
//
// Holder of assembly reference which keeps collectible assembly alive while the holder is valid.
//
// Collectible assembly can be collected at any point when GC happens. Almost instantly all native data
// structures of the assembly (e.g. code:DomainAssembly, code:Assembly) could be deallocated.
// Therefore any usage of (collectible) assembly data structures from native world, has to prevent the
// deallocation by increasing ref-count on the assembly / associated loader allocator.
//
// #CAH_DAC
// In DAC we don't AddRef/Release as the assembly doesn't have to be kept alive: The process is stopped when
// DAC is used and therefore the assembly cannot just disappear.
//
template<typename _Type>
class CollectibleAssemblyHolder : public BaseWrapper<_Type, CollectibleAssemblyHolderBase<_Type> >
{
public:
    FORCEINLINE
    CollectibleAssemblyHolder(const _Type & value = NULL, BOOL fTake = TRUE)
        : BaseWrapper<_Type, CollectibleAssemblyHolderBase<_Type> >(value, fTake)
    {
        STATIC_CONTRACT_WRAPPER;
    }

    FORCEINLINE
    CollectibleAssemblyHolder &
    operator=(const _Type & value)
    {
        STATIC_CONTRACT_WRAPPER;
        BaseWrapper<_Type, CollectibleAssemblyHolderBase<_Type> >::operator=(value);
        return *this;
    }

    // Operator & is overloaded in parent, therefore we have to get to 'this' pointer explicitly.
    FORCEINLINE
    CollectibleAssemblyHolder<_Type> *
    This()
    {
        LIMITED_METHOD_CONTRACT;
        return this;
    }
};  // class CollectibleAssemblyHolder<>

//---------------------------------------------------------------------------------------
//
// Stores binding information about failed assembly loads for DAC
//
struct FailedAssembly {
    SString displayName;
    HRESULT error;

    void Initialize(AssemblySpec *pSpec, Exception *ex)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        displayName.SetASCII(pSpec->GetName());
        error = ex->GetHR();

        //
        // Determine the binding context assembly would have been in.
        // If the parent has been set, use its binding context.
        // If the parent hasn't been set but the code base has, use LoadFrom.
        // Otherwise, use the default.
        //
    }
};

const DWORD DefaultADID = 1;

// An Appdomain is the managed equivalent of a process.  It is an isolation unit (conceptually you don't
// have pointers directly from one appdomain to another, but rather go through remoting proxies).  It is
// also a unit of unloading.
//
// Threads are always running in the context of a particular AppDomain.  See
// file:threads.h#RuntimeThreadLocals for more details.
//
// see code:BaseDomain for much of the meat of a AppDomain (heaps locks, etc)
//     * code:AppDomain.m_Assemblies - is a list of code:Assembly in the appdomain
//
class AppDomain : public BaseDomain
{
    friend class SystemDomain;
    friend class AssemblyNative;
    friend class AssemblySpec;
    friend class ClassLoader;
    friend class ThreadNative;
    friend class ClrDataAccess;
    friend class CheckAsmOffsets;

    VPTR_VTABLE_CLASS(AppDomain, BaseDomain)

public:
#ifndef DACCESS_COMPILE
    AppDomain();
    virtual ~AppDomain();
    static void Create();
#endif

    //-----------------------------------------------------------------------------------------------------------------
    // Convenience wrapper for ::GetAppDomain to provide better encapsulation.
    static PTR_AppDomain GetCurrentDomain()
    { return m_pTheAppDomain; }

    //-----------------------------------------------------------------------------------------------------------------
    // Initializes an AppDomain. (this functions is not called from the SystemDomain)
    void Init();

    bool MustForceTrivialWaitOperations();
    void SetForceTrivialWaitOperations();

    //****************************************************************************************
    //
    // Stop deletes all the assemblies but does not remove other resources like
    // the critical sections
    void Stop();

    // final assembly cleanup
    void ShutdownFreeLoaderAllocators();

    virtual BOOL IsAppDomain() { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
    virtual PTR_AppDomain AsAppDomain() { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_AppDomain>(this); }

    OBJECTREF GetRawExposedObject() { LIMITED_METHOD_CONTRACT; return NULL; }
    OBJECTHANDLE GetRawExposedObjectHandleForDebugger() { LIMITED_METHOD_DAC_CONTRACT; return NULL; }

#ifndef DACCESS_COMPILE
    PTR_NativeImage GetNativeImage(LPCUTF8 compositeFileName);
    PTR_NativeImage SetNativeImage(LPCUTF8 compositeFileName, PTR_NativeImage pNativeImage);
#endif // DACCESS_COMPILE

    //****************************************************************************************

protected:
    // Multi-thread safe access to the list of assemblies
    class DomainAssemblyList
    {
    private:
        ArrayList m_array;
#ifdef _DEBUG
        AppDomain * dbg_m_pAppDomain;
    public:
        void Debug_SetAppDomain(AppDomain * pAppDomain)
        {
            dbg_m_pAppDomain = pAppDomain;
        }
#endif //_DEBUG
    public:
        bool IsEmpty()
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            } CONTRACTL_END;

            // This function can be reliably called without taking the lock, because the first assembly
            // added to the arraylist is non-collectible, and the ArrayList itself allows lockless read access
            return (m_array.GetCount() == 0);
        }
        void Clear(AppDomain * pAppDomain)
        {
            CONTRACTL {
                NOTHROW;
                WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain == pAppDomain);

            CrstHolder ch(pAppDomain->GetAssemblyListLock());
            m_array.Clear();
        }

        DWORD GetCount(AppDomain * pAppDomain)
        {
            CONTRACTL {
                NOTHROW;
                WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain == pAppDomain);

            CrstHolder ch(pAppDomain->GetAssemblyListLock());
            return GetCount_Unlocked();
        }
        DWORD GetCount_Unlocked()
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            } CONTRACTL_END;

#ifndef DACCESS_COMPILE
            _ASSERTE(dbg_m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
#endif
            // code:Append_Unlock guarantees that we do not have more than MAXDWORD items
            return m_array.GetCount();
        }

        void Get(AppDomain * pAppDomain, DWORD index, CollectibleAssemblyHolder<DomainAssembly *> * pAssemblyHolder)
        {
            CONTRACTL {
                NOTHROW;
                WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain == pAppDomain);

            CrstHolder ch(pAppDomain->GetAssemblyListLock());
            Get_Unlocked(index, pAssemblyHolder);
        }
        void Get_Unlocked(DWORD index, CollectibleAssemblyHolder<DomainAssembly *> * pAssemblyHolder)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
            *pAssemblyHolder = dac_cast<PTR_DomainAssembly>(m_array.Get(index));
        }
        // Doesn't lock the assembly list (caller has to hold the lock already).
        // Doesn't AddRef the returned assembly (if collectible).
        DomainAssembly * Get_UnlockedNoReference(DWORD index)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                SUPPORTS_DAC;
            } CONTRACTL_END;

#ifndef DACCESS_COMPILE
            _ASSERTE(dbg_m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
#endif
            return dac_cast<PTR_DomainAssembly>(m_array.Get(index));
        }

#ifndef DACCESS_COMPILE
        void Set(AppDomain * pAppDomain, DWORD index, DomainAssembly * pAssembly)
        {
            CONTRACTL {
                NOTHROW;
                WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain == pAppDomain);

            CrstHolder ch(pAppDomain->GetAssemblyListLock());
            return Set_Unlocked(index, pAssembly);
        }
        void Set_Unlocked(DWORD index, DomainAssembly * pAssembly)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
            m_array.Set(index, pAssembly);
        }

        HRESULT Append_Unlocked(DomainAssembly * pAssembly)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
            } CONTRACTL_END;

            _ASSERTE(dbg_m_pAppDomain->GetAssemblyListLock()->OwnedByCurrentThread());
            return m_array.Append(pAssembly);
        }
#else //DACCESS_COMPILE
        void
        EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
        {
            SUPPORTS_DAC;

            m_array.EnumMemoryRegions(flags);
        }
#endif // DACCESS_COMPILE

        // Should be used only by code:AssemblyIterator::Create
        ArrayList::Iterator GetArrayListIterator()
        {
            return m_array.Iterate();
        }
    };  // class DomainAssemblyList

    // Conceptually a list of code:Assembly structures, protected by lock code:GetAssemblyListLock
    DomainAssemblyList m_Assemblies;

public:
    // Note that this lock switches thread into GC_NOTRIGGER region as GC can take it too.
    CrstExplicitInit * GetAssemblyListLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_crstAssemblyList;
    }

public:
    class AssemblyIterator
    {
        // AppDomain context with the assembly list
        AppDomain *            m_pAppDomain;
        ArrayList::Iterator    m_Iterator;
        AssemblyIterationFlags m_assemblyIterationFlags;

    public:
        BOOL Next(CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder);
        // Note: Does not lock the assembly list, but AddRefs collectible assemblies.
        BOOL Next_Unlocked(CollectibleAssemblyHolder<DomainAssembly *> * pDomainAssemblyHolder);

    private:
        inline DWORD GetIndex()
        {
            LIMITED_METHOD_CONTRACT;
            return m_Iterator.GetIndex();
        }

    private:
        friend class AppDomain;
        // Cannot have constructor so this iterator can be used inside a union
        static AssemblyIterator Create(AppDomain * pAppDomain, AssemblyIterationFlags assemblyIterationFlags)
        {
            LIMITED_METHOD_CONTRACT;
            AssemblyIterator i;

            i.m_pAppDomain = pAppDomain;
            i.m_Iterator = pAppDomain->m_Assemblies.GetArrayListIterator();
            i.m_assemblyIterationFlags = assemblyIterationFlags;
            return i;
        }
    };  // class AssemblyIterator

    AssemblyIterator IterateAssembliesEx(AssemblyIterationFlags assemblyIterationFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return AssemblyIterator::Create(this, assemblyIterationFlags);
    }

public:
    class PathIterator
    {
        friend class AppDomain;

        ArrayList::Iterator m_i;

    public:
        BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            return m_i.Next();
        }

        SString* GetPath()
        {
            WRAPPER_NO_CONTRACT;
            return dac_cast<PTR_SString>(m_i.GetElement());
        }
    };

    PathIterator IterateNativeDllSearchDirectories();
    void SetNativeDllSearchDirectories(LPCWSTR paths);
    BOOL HasNativeDllSearchDirectories();

public:
    SIZE_T GetAssemblyCount()
    {
        WRAPPER_NO_CONTRACT;
        return m_Assemblies.GetCount(this);
    }

    CHECK CheckCanLoadTypes(Assembly *pAssembly);
    CHECK CheckCanExecuteManagedCode(MethodDesc* pMD);
    CHECK CheckLoading(DomainAssembly *pFile, FileLoadLevel level);

    BOOL IsLoading(DomainAssembly *pFile, FileLoadLevel level);
    static FileLoadLevel GetThreadFileLoadLevel();

    void LoadDomainAssembly(DomainAssembly *pFile,
                        FileLoadLevel targetLevel);

    enum FindAssemblyOptions
    {
        FindAssemblyOptions_None                    = 0x0,
        FindAssemblyOptions_IncludeFailedToLoad     = 0x1
    };

    DomainAssembly * FindAssembly(PEAssembly* pPEAssembly, FindAssemblyOptions options = FindAssemblyOptions_None) DAC_EMPTY_RET(NULL);


    Assembly *LoadAssembly(AssemblySpec* pIdentity,
                           PEAssembly *pPEAssembly,
                           FileLoadLevel targetLevel);

    // this function does not provide caching, you must use LoadDomainAssembly
    // unless the call is guaranteed to succeed or you don't need the caching
    // (e.g. if you will FailFast or tear down the AppDomain anyway)
    // The main point that you should not bypass caching if you might try to load the same file again,
    // resulting in multiple DomainAssembly objects that share the same PEAssembly for ngen image
    //which is violating our internal assumptions
    DomainAssembly *LoadDomainAssemblyInternal( AssemblySpec* pIdentity,
                                                PEAssembly *pPEAssembly,
                                                FileLoadLevel targetLevel);

    DomainAssembly *LoadDomainAssembly( AssemblySpec* pIdentity,
                                        PEAssembly *pPEAssembly,
                                        FileLoadLevel targetLevel);


    CHECK CheckValidModule(Module *pModule);

    // private:
    void LoadSystemAssemblies();

    DomainAssembly *LoadDomainAssembly(FileLoadLock *pLock,
                               FileLoadLevel targetLevel);

    void TryIncrementalLoad(DomainAssembly *pFile, FileLoadLevel workLevel, FileLoadLockHolder &lockHolder);

#ifndef DACCESS_COMPILE // needs AssemblySpec

    //****************************************************************************************
    // Returns and Inserts assemblies into a lookup cache based on the binding information
    // in the AssemblySpec. There can be many AssemblySpecs to a single assembly.
    DomainAssembly* FindCachedAssembly(AssemblySpec* pSpec, BOOL fThrow=TRUE)
    {
        WRAPPER_NO_CONTRACT;
        return m_AssemblyCache.LookupAssembly(pSpec, fThrow);
    }

    PEAssembly* FindCachedFile(AssemblySpec* pSpec, BOOL fThrow = TRUE);
    BOOL IsCached(AssemblySpec *pSpec);
#endif // DACCESS_COMPILE

    BOOL AddFileToCache(AssemblySpec* pSpec, PEAssembly *pPEAssembly);
    BOOL RemoveFileFromCache(PEAssembly *pPEAssembly);

    BOOL AddAssemblyToCache(AssemblySpec* pSpec, DomainAssembly *pAssembly);
    BOOL RemoveAssemblyFromCache(DomainAssembly* pAssembly);

    BOOL AddExceptionToCache(AssemblySpec* pSpec, Exception *ex);
    void AddUnmanagedImageToCache(LPCWSTR libraryName, NATIVE_LIBRARY_HANDLE hMod);
    NATIVE_LIBRARY_HANDLE FindUnmanagedImageInCache(LPCWSTR libraryName);
    //****************************************************************************************
    //
    // Adds or removes an assembly to the domain.
    void AddAssembly(DomainAssembly * assem);
    void RemoveAssembly(DomainAssembly * pAsm);

    BOOL ContainsAssembly(Assembly * assem);

    //****************************************************************************************
    //
    // Reference count. When an appdomain is first created the reference is bump
    // to one when it is added to the list of domains (see SystemDomain). An explicit
    // Removal from the list is necessary before it will be deleted.
    ULONG AddRef(void);
    ULONG Release(void) DAC_EMPTY_RET(0);

    //****************************************************************************************
    LPCWSTR GetFriendlyName(BOOL fDebuggerCares = TRUE);
    LPCWSTR GetFriendlyNameForDebugger();
    LPCWSTR GetFriendlyNameForLogging();
#ifdef DACCESS_COMPILE
    PVOID GetFriendlyNameNoSet(bool* isUtf8);
#endif
    void SetFriendlyName(LPCWSTR pwzFriendlyName, BOOL fDebuggerCares = TRUE);

    //****************************************************************************************

    // This can be used to override the binding behavior of the appdomain.   It
    // is overridden in the compilation domain.  It is important that all
    // static binding goes through this path.
    virtual PEAssembly * BindAssemblySpec(
        AssemblySpec *pSpec,
        BOOL fThrowOnFileNotFound) DAC_EMPTY_RET(NULL);

    //****************************************************************************************
    //
    //****************************************************************************************
    //
    // Uses the first assembly to add an application base to the Context. This is done
    // in a lazy fashion so executables do not take the perf hit unless the load other
    // assemblies
#ifndef DACCESS_COMPILE
    static BOOL OnUnhandledException(OBJECTREF *pThrowable, BOOL isTerminating = TRUE);

#endif

    // True iff a debugger is attached to the process (same as CORDebuggerAttached)
    BOOL IsDebuggerAttached (void);

#ifdef DEBUGGING_SUPPORTED
    // Notify debugger of all assemblies, modules, and possibly classes in this AppDomain
    BOOL NotifyDebuggerLoad(int flags, BOOL attaching);

    // Send unload notifications to the debugger for all assemblies, modules and classes in this AppDomain
    void NotifyDebuggerUnload();
#endif // DEBUGGING_SUPPORTED

#ifndef DACCESS_COMPILE
    OBJECTREF* AllocateStaticFieldObjRefPtrs(int nRequested, OBJECTREF** ppLazyAllocate = NULL)
    {
        WRAPPER_NO_CONTRACT;

        return AllocateObjRefPtrsInLargeTable(nRequested, ppLazyAllocate);
    }
#endif // DACCESS_COMPILE

    void              EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

    void SetupSharedStatics();

#ifdef FEATURE_COMINTEROP
public:
    OBJECTREF GetMissingObject();    // DispatchInfo will call function to retrieve the Missing.Value object.

    RCWCache *GetRCWCache()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pRCWCache)
            return m_pRCWCache;

        // By separating the cache creation from the common lookup, we
        // can keep the (x86) EH prolog/epilog off the path.
        return CreateRCWCache();
    }
private:
    RCWCache *CreateRCWCache();
public:
    RCWCache *GetRCWCacheNoCreate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pRCWCache;
    }

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
public:
    RCWRefCache *GetRCWRefCache();
#endif // FEATURE_COMWRAPPERS

    DefaultAssemblyBinder *CreateDefaultBinder();

    void SetIgnoreUnhandledExceptions()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= IGNORE_UNHANDLED_EXCEPTIONS;
    }

    BOOL IgnoreUnhandledExceptions()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & IGNORE_UNHANDLED_EXCEPTIONS);
    }

    static void ExceptionUnwind(Frame *pFrame);

    BOOL IsActive()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_Stage >= STAGE_ACTIVE;
    }

    BOOL IsValid()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef DACCESS_COMPILE
        // We want to see all appdomains in SOS, even the about to be destructed ones.
        // There is no risk of races under DAC, so we will pretend to be unconditionally valid.
        return TRUE;
#else
        return m_Stage > STAGE_CREATING;
#endif
    }

    static void RaiseExitProcessEvent();
    Assembly* RaiseResourceResolveEvent(DomainAssembly* pAssembly, LPCSTR szName);
    DomainAssembly* RaiseTypeResolveEventThrowing(DomainAssembly* pAssembly, LPCSTR szName, ASSEMBLYREF *pResultingAssemblyRef);
    Assembly* RaiseAssemblyResolveEvent(AssemblySpec *pSpec);

private:
    CrstExplicitInit    m_ReflectionCrst;
    CrstExplicitInit    m_RefClassFactCrst;


    EEClassFactoryInfoHashTable *m_pRefClassFactHash;   // Hash table that maps a class factory info to a COM comp.
#ifdef FEATURE_COMINTEROP
    DispIDCache *m_pRefDispIDCache;
    OBJECTHANDLE  m_hndMissing;     //Handle points to Missing.Value Object which is used for [Optional] arg scenario during IDispatch CCW Call
#endif // FEATURE_COMINTEROP

public:

    CrstBase *GetRefClassFactCrst()
    {
        LIMITED_METHOD_CONTRACT;

        return &m_RefClassFactCrst;
    }

#ifndef DACCESS_COMPILE
    EEClassFactoryInfoHashTable* GetClassFactHash()
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_FAULT;

        if (m_pRefClassFactHash != NULL) {
            return m_pRefClassFactHash;
        }

        return SetupClassFactHash();
    }
#endif // DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
    DispIDCache* GetRefDispIDCache()
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_FAULT;

        if (m_pRefDispIDCache != NULL) {
            return m_pRefDispIDCache;
        }

        return SetupRefDispIDCache();
    }
#endif // FEATURE_COMINTEROP

    PTR_LoaderHeap GetStubHeap();
    PTR_LoaderHeap GetLowFrequencyHeap();
    PTR_LoaderHeap GetHighFrequencyHeap();

private:
    size_t EstimateSize();
    EEClassFactoryInfoHashTable* SetupClassFactHash();
#ifdef FEATURE_COMINTEROP
    DispIDCache* SetupRefDispIDCache();
#endif // FEATURE_COMINTEROP

private:
    PEAssembly *TryResolveAssemblyUsingEvent(AssemblySpec *pSpec);
    BOOL PostBindResolveAssembly(AssemblySpec  *pPrePolicySpec,
                                 AssemblySpec  *pPostPolicySpec,
                                 HRESULT        hrBindResult,
                                 AssemblySpec **ppFailedSpec);

#ifdef FEATURE_COMINTEROP
public:
    void ReleaseRCWs(LPVOID pCtxCookie);
    void DetachRCWs();

#endif // FEATURE_COMINTEROP

private:
    void RaiseLoadingAssemblyEvent(DomainAssembly* pAssembly);

    friend class DomainAssembly;

private:
    BOOL RaiseUnhandledExceptionEvent(OBJECTREF *pThrowable, BOOL isTerminating);

    enum Stage {
        STAGE_CREATING,
        STAGE_READYFORMANAGEDCODE,
        STAGE_ACTIVE,
        STAGE_OPEN,
        // Don't delete the following *_DONOTUSE members and in case a new member needs to be added,
        // add it at the end. The reason is that debugger stuff has its own copy of this enum and
        // it can use the members that are marked as *_DONOTUSE here when debugging older version
        // of the runtime.
        STAGE_UNLOAD_REQUESTED_DONOTUSE,
        STAGE_EXITING_DONOTUSE,
        STAGE_EXITED_DONOTUSE,
        STAGE_FINALIZING_DONOTUSE,
        STAGE_FINALIZED_DONOTUSE,
        STAGE_HANDLETABLE_NOACCESS_DONOTUSE,
        STAGE_CLEARED_DONOTUSE,
        STAGE_COLLECTED_DONOTUSE,
        STAGE_CLOSED_DONOTUSE
    };
    void SetStage(Stage stage)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        STRESS_LOG1(LF_APPDOMAIN, LL_INFO100,"Updating AD stage, stage=%d\n",stage);
        Stage lastStage=m_Stage;
        while (lastStage !=stage)
            lastStage = (Stage)InterlockedCompareExchange((LONG*)&m_Stage,stage,lastStage);
    };

    // List of unloaded LoaderAllocators, protected by code:GetLoaderAllocatorReferencesLock (for now)
    LoaderAllocator * m_pDelayedLoaderAllocatorUnloadList;

public:

    // Register the loader allocator for deletion in code:ShutdownFreeLoaderAllocators.
    void RegisterLoaderAllocatorForDeletion(LoaderAllocator * pLoaderAllocator);

public:
    void SetGCRefPoint(int gccounter)
    {
        LIMITED_METHOD_CONTRACT;
        GetLoaderAllocator()->SetGCRefPoint(gccounter);
    }
    int  GetGCRefPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return GetLoaderAllocator()->GetGCRefPoint();
    }

    void AddMemoryPressure();
    void RemoveMemoryPressure();

    PTR_Assembly GetRootAssembly()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pRootAssembly;
    }

#ifndef DACCESS_COMPILE
    void SetRootAssembly(Assembly *pAssembly)
    {
        LIMITED_METHOD_CONTRACT;
        m_pRootAssembly = pAssembly;
    }
#endif

private:
    // The one and only AppDomain
    SPTR_DECL(AppDomain, m_pTheAppDomain);

    SString         m_friendlyName;
    PTR_Assembly    m_pRootAssembly;

    // General purpose flags.
    DWORD           m_dwFlags;

    // When an application domain is created the ref count is artificially incremented
    // by one. For it to hit zero an explicit close must have happened.
    LONG        m_cRef;                    // Ref count.

    // Map of loaded composite native images indexed by base load addresses
    CrstExplicitInit m_nativeImageLoadCrst;
    MapSHash<LPCUTF8, PTR_NativeImage, NativeImageIndexTraits> m_nativeImageMap;

#ifdef FEATURE_COMINTEROP
    // this cache stores the RCWs in this domain
    RCWCache *m_pRCWCache;
#endif //FEATURE_COMINTEROP
#ifdef FEATURE_COMWRAPPERS
    // this cache stores the RCW -> CCW references in this domain
    RCWRefCache *m_pRCWRefCache;
#endif // FEATURE_COMWRAPPERS

    Volatile<Stage> m_Stage;

    ArrayList        m_failedAssemblies;

    //
    // DAC iterator for failed assembly loads
    //
    class FailedAssemblyIterator
    {
        ArrayList::Iterator m_i;

      public:
        BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            return m_i.Next();
        }
        FailedAssembly *GetFailedAssembly()
        {
            WRAPPER_NO_CONTRACT;
            return dac_cast<PTR_FailedAssembly>(m_i.GetElement());
        }
        SIZE_T GetIndex()
        {
            WRAPPER_NO_CONTRACT;
            return m_i.GetIndex();
        }

      private:
        friend class AppDomain;
        // Cannot have constructor so this iterator can be used inside a union
        static FailedAssemblyIterator Create(AppDomain *pDomain)
        {
            WRAPPER_NO_CONTRACT;
            FailedAssemblyIterator i;

            i.m_i = pDomain->m_failedAssemblies.Iterate();
            return i;
        }
    };
    friend class FailedAssemblyIterator;

    FailedAssemblyIterator IterateFailedAssembliesEx()
    {
        WRAPPER_NO_CONTRACT;
        return FailedAssemblyIterator::Create(this);
    }

    //---------------------------------------------------------
    // Stub caches for Method stubs
    //---------------------------------------------------------

public:

    enum {
        CONTEXT_INITIALIZED =               0x0001,
        // unused =                         0x0400,
        IGNORE_UNHANDLED_EXCEPTIONS =      0x10000, // AppDomain was created using the APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS flag
    };

    AssemblySpecBindingCache  m_AssemblyCache;
    size_t                    m_MemoryPressure;

    ArrayList m_NativeDllSearchDirectories;
    bool m_ForceTrivialWaitOperations;

private:
    struct UnmanagedImageCacheEntry
    {
        LPCWSTR Name;
        NATIVE_LIBRARY_HANDLE Handle;
    };

    class UnmanagedImageCacheTraits : public NoRemoveSHashTraits<DefaultSHashTraits<UnmanagedImageCacheEntry>>
    {
    public:
        using key_t = LPCWSTR;
        static const key_t GetKey(_In_ const element_t& e) { return e.Name; }
        static count_t Hash(_In_ key_t key) { return HashString(key); }
        static bool Equals(_In_ key_t lhs, _In_ key_t rhs) { return u16_strcmp(lhs, rhs) == 0; }
        static bool IsNull(_In_ const element_t& e) { return e.Handle == NULL; }
        static const element_t Null() { return UnmanagedImageCacheEntry(); }
    };

    SHash<UnmanagedImageCacheTraits> m_unmanagedCache;

#ifdef FEATURE_TYPEEQUIVALENCE
private:
    VolatilePtr<TypeEquivalenceHashTable> m_pTypeEquivalenceTable;
    CrstExplicitInit m_TypeEquivalenceCrst;
public:
    TypeEquivalenceHashTable * GetTypeEquivalenceCache();
#endif

    private:

#ifdef DACCESS_COMPILE
public:
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif

#ifdef FEATURE_MULTICOREJIT

private:
    MulticoreJitManager m_MulticoreJitManager;

public:
    MulticoreJitManager & GetMulticoreJitManager()
    {
        LIMITED_METHOD_CONTRACT;

        return m_MulticoreJitManager;
    }

#endif

#if defined(FEATURE_TIERED_COMPILATION)

public:
    TieredCompilationManager * GetTieredCompilationManager()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_tieredCompilationManager;
    }

private:
    TieredCompilationManager m_tieredCompilationManager;

#endif
};  // class AppDomain

// Just a ref holder
typedef ReleaseHolder<AppDomain> AppDomainRefHolder;

typedef VPTR(class SystemDomain) PTR_SystemDomain;

class SystemDomain : public BaseDomain
{
    friend class ClrDataAccess;

    VPTR_VTABLE_CLASS(SystemDomain, BaseDomain)
    VPTR_UNIQUE(VPTR_UNIQUE_SystemDomain)

public:
    static PTR_LoaderAllocator GetGlobalLoaderAllocator();

    //****************************************************************************************
    //
    // To be run during the initial start up of the EE. This must be
    // performed prior to any class operations.
    static void Attach();

    //****************************************************************************************
    //
    // To be run during shutdown. This must be done after all operations
    // that require the use of system classes (i.e., exceptions).
    // DetachBegin stops all domains, while DetachEnd deallocates domain resources.
    static void DetachBegin();

    //****************************************************************************************
    //
    // To be run during shutdown. This must be done after all operations
    // that require the use of system classes (i.e., exceptions).
    // DetachBegin stops release resources held by systemdomain and the default domain.
    static void DetachEnd();

    //****************************************************************************************
    //
    // Initializes and shutdowns the single instance of the SystemDomain
    // in the EE
#ifndef DACCESS_COMPILE
    void *operator new(size_t size, void *pInPlace);
    void operator delete(void *pMem);
#endif
    void Init();
    void Stop();
    static void LazyInitGlobalStringLiteralMap();
    static void LazyInitFrozenObjectsHeap();

    //****************************************************************************************
    //
    // Load the base system classes, these classes are required before
    // any other classes are loaded
    void LoadBaseSystemClasses();

    AppDomain* DefaultDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return AppDomain::GetCurrentDomain();
    }

    //****************************************************************************************
    //
    // Global Static to get the one and only system domain
    static PTR_SystemDomain System()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pSystemDomain;
    }

    static PEAssembly* SystemPEAssembly()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_pSystemDomain);
        return System()->m_pSystemPEAssembly;
    }

    static Assembly* SystemAssembly()
    {
        WRAPPER_NO_CONTRACT;

        return System()->m_pSystemAssembly;
    }

    static Module* SystemModule()
    {
        WRAPPER_NO_CONTRACT;

        return SystemAssembly()->GetModule();
    }

    static BOOL IsSystemLoaded()
    {
        WRAPPER_NO_CONTRACT;

        return System()->m_pSystemAssembly != NULL;
    }

#ifndef DACCESS_COMPILE
    static GlobalStringLiteralMap *GetGlobalStringLiteralMap()
    {
        WRAPPER_NO_CONTRACT;

        if (m_pGlobalStringLiteralMap == NULL)
        {
            SystemDomain::LazyInitGlobalStringLiteralMap();
        }
        _ASSERTE(m_pGlobalStringLiteralMap);
        return m_pGlobalStringLiteralMap;
    }
    static GlobalStringLiteralMap *GetGlobalStringLiteralMapNoCreate()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_pGlobalStringLiteralMap);
        return m_pGlobalStringLiteralMap;
    }
    static FrozenObjectHeapManager* GetFrozenObjectHeapManager()
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        if (VolatileLoad(&m_FrozenObjectHeapManager) == nullptr)
        {
            LazyInitFrozenObjectsHeap();
        }
        return VolatileLoad(&m_FrozenObjectHeapManager);
    }
    static FrozenObjectHeapManager* GetFrozenObjectHeapManagerNoThrow()
    {
        LIMITED_METHOD_CONTRACT;

        return VolatileLoad(&m_FrozenObjectHeapManager);
    }
#endif // DACCESS_COMPILE

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT)
    static Thread::ApartmentState GetEntryPointThreadAptState(IMDInternalImport* pScope, mdMethodDef mdMethod);
    static void SetThreadAptState(Thread::ApartmentState state);
#endif

    //****************************************************************************************
    // Methods used to get the callers module and hence assembly and app domain.

    static Module* GetCallersModule(StackCrawlMark* stackMark);
    static Assembly* GetCallersAssembly(StackCrawlMark* stackMark);

    static bool IsReflectionInvocationMethod(MethodDesc* pMeth);

#ifndef DACCESS_COMPILE
    //****************************************************************************************
    // Returns the domain associated with the current context. (this can only be a child domain)
    static inline AppDomain * GetCurrentDomain()
    {
        WRAPPER_NO_CONTRACT;
        return ::GetAppDomain();
    }
#endif //!DACCESS_COMPILE

#ifdef DEBUGGING_SUPPORTED
    //****************************************************************************************
    // Debugger/Publisher helper function to indicate creation of new app domain to debugger
    // and publishing it in the IPC block
    static void PublishAppDomainAndInformDebugger (AppDomain *pDomain);
#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
    //****************************************************************************************
    // Tell profiler about system created domains which are created before the profiler is
    // actually activated.
    static void NotifyProfilerStartup();

    //****************************************************************************************
    // Tell profiler at shutdown that system created domains are going away.  They are not
    // torn down using the normal sequence.
    static HRESULT NotifyProfilerShutdown();
#endif // PROFILING_SUPPORTED

#ifndef DACCESS_COMPILE
    DWORD RequireAppDomainCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDelayedUnloadListOfLoaderAllocators != 0;
    }

    void AddToDelayedUnloadList(LoaderAllocator * pAllocator)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        CrstHolder lh(&m_DelayedUnloadCrst);
        pAllocator->m_pLoaderAllocatorDestroyNext=m_pDelayedUnloadListOfLoaderAllocators;
        m_pDelayedUnloadListOfLoaderAllocators=pAllocator;

        int iGCRefPoint=GCHeapUtilities::GetGCHeap()->CollectionCount(GCHeapUtilities::GetGCHeap()->GetMaxGeneration());
        if (GCHeapUtilities::IsGCInProgress())
            iGCRefPoint++;
        pAllocator->SetGCRefPoint(iGCRefPoint);
    }

    void ProcessDelayedUnloadLoaderAllocators();

    static void EnumAllStaticGCRefs(promote_func* fn, ScanContext* sc);

#endif // DACCESS_COMPILE

    //****************************************************************************************
    LPCWSTR BaseLibrary()
    {
        WRAPPER_NO_CONTRACT;

        return m_BaseLibrary;
    }

#ifndef DACCESS_COMPILE
    BOOL IsBaseLibrary(SString &path)
    {
        WRAPPER_NO_CONTRACT;

        // See if it is the installation path to CoreLib
        if (path.EqualsCaseInsensitive(m_BaseLibrary))
            return TRUE;

        // Or, it might be the location of CoreLib
        if (System()->SystemAssembly() != NULL
            && path.EqualsCaseInsensitive(System()->SystemAssembly()->GetPEAssembly()->GetPath()))
            return TRUE;

        return FALSE;
    }

    BOOL IsBaseLibrarySatellite(SString &path)
    {
        WRAPPER_NO_CONTRACT;

        // See if it is the installation path to corelib.resources
        SString s(SString::Ascii,g_psBaseLibrarySatelliteAssemblyName);
        if (path.EqualsCaseInsensitive(s))
            return TRUE;

        return FALSE;
    }
#endif // DACCESS_COMPILE

    // Return the system directory
    LPCWSTR SystemDirectory()
    {
        WRAPPER_NO_CONTRACT;

        return m_SystemDirectory;
    }

private:

    void CreatePreallocatedExceptions();

    void PreallocateSpecialObjects();

    //****************************************************************************************
    //
    static StackWalkAction CallersMethodCallback(CrawlFrame* pCrawlFrame, VOID* pClientData);
    static StackWalkAction CallersMethodCallbackWithStackMark(CrawlFrame* pCrawlFrame, VOID* pClientData);

#ifndef DACCESS_COMPILE
    // This class is not to be created through normal allocation.
    SystemDomain()
    {
        STANDARD_VM_CONTRACT;

        m_pDelayedUnloadListOfLoaderAllocators=NULL;

        m_GlobalAllocator.Init(this);
    }
#endif

    PTR_PEAssembly  m_pSystemPEAssembly;// Single assembly (here for quicker reference);
    PTR_Assembly    m_pSystemAssembly;  // Single assembly (here for quicker reference);

    GlobalLoaderAllocator m_GlobalAllocator;


    InlineSString<100>  m_BaseLibrary;

    InlineSString<100>  m_SystemDirectory;

    // <TODO>@TODO: CTS, we can keep the com modules in a single assembly or in different assemblies.
    // We are currently using different assemblies but this is potentitially to slow...</TODO>

    // Global domain that every one uses
    SPTR_DECL(SystemDomain, m_pSystemDomain);

    LoaderAllocator * m_pDelayedUnloadListOfLoaderAllocators;

#ifndef DACCESS_COMPILE
    static CrstStatic m_DelayedUnloadCrst;
    static CrstStatic       m_SystemDomainCrst;

    static GlobalStringLiteralMap *m_pGlobalStringLiteralMap;
    static FrozenObjectHeapManager *m_FrozenObjectHeapManager;
#endif // DACCESS_COMPILE

public:
    //****************************************************************************************
    //

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
inline static BOOL IsUnderDomainLock() { LIMITED_METHOD_CONTRACT; return m_SystemDomainCrst.OwnedByCurrentThread();};
#endif

    // This lock controls adding and removing domains from the system domain
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder()
            : CrstHolder(&m_SystemDomainCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
#endif // DACCESS_COMPILE

public:
    DWORD GetTotalNumSizedRefHandles();

#ifdef DACCESS_COMPILE
public:
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif

};  // class SystemDomain

#include "comreflectioncache.inl"

#endif
