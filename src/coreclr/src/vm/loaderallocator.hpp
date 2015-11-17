//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Header:  LoaderAllocator.hpp
** 

**
** Purpose: Implements collection of loader heaps
**
**
===========================================================*/

#ifndef __LoaderAllocator_h__
#define __LoaderAllocator_h__

class FuncPtrStubs;
#include "qcall.h"

#define VPTRU_LoaderAllocator 0x3200

enum LoaderAllocatorType
{
    LAT_Invalid,
    LAT_Global,
    LAT_AppDomain,
    LAT_Assembly
};

class LoaderAllocatorID
{

protected:
    LoaderAllocatorType m_type;
    union
    {
        AppDomain* m_pAppDomain;
        DomainAssembly* m_pDomainAssembly;
        void* m_pValue;
    };

    VOID * GetValue();

public:
    LoaderAllocatorID(LoaderAllocatorType laType=LAT_Invalid, VOID* value = 0)
    {
        m_type = laType;
        m_pValue = value;
    };
    VOID Init();
    VOID Init(AppDomain* pAppDomain);
    LoaderAllocatorType GetType();
    VOID SetDomainAssembly(DomainAssembly* pDomainAssembly);
    DomainAssembly* GetDomainAssembly();
    AppDomain* GetAppDomain();
    BOOL Equals(LoaderAllocatorID* pId);
    COUNT_T Hash();
    BOOL IsCollectible();
};

class StringLiteralMap;
class VirtualCallStubManager;
class ListLockEntry;

class LoaderAllocator
{
    VPTR_BASE_VTABLE_CLASS(LoaderAllocator)
    VPTR_UNIQUE(VPTRU_LoaderAllocator)
protected:    
   
    //****************************************************************************************
    // #LoaderAllocator Heaps
    // Heaps for allocating data that persists for the life of the AppDomain
    // Objects that are allocated frequently should be allocated into the HighFreq heap for
    // better page management
    BYTE *              m_InitialReservedMemForLoaderHeaps;
    BYTE                m_LowFreqHeapInstance[sizeof(LoaderHeap)];
    BYTE                m_HighFreqHeapInstance[sizeof(LoaderHeap)];
    BYTE                m_StubHeapInstance[sizeof(LoaderHeap)];
    BYTE                m_PrecodeHeapInstance[sizeof(CodeFragmentHeap)];
    PTR_LoaderHeap      m_pLowFrequencyHeap;
    PTR_LoaderHeap      m_pHighFrequencyHeap;
    PTR_LoaderHeap      m_pStubHeap; // stubs for PInvoke, remoting, etc
    PTR_CodeFragmentHeap m_pPrecodeHeap;
    PTR_LoaderHeap      m_pExecutableHeap;
#ifdef FEATURE_READYTORUN
    PTR_CodeFragmentHeap m_pDynamicHelpersHeap;
#endif
    //****************************************************************************************
    OBJECTHANDLE        m_hLoaderAllocatorObjectHandle;
    FuncPtrStubs *      m_pFuncPtrStubs; // for GetMultiCallableAddrOfCode()
    // The LoaderAllocator specific string literal map.
    StringLiteralMap   *m_pStringLiteralMap;
    CrstExplicitInit    m_crstLoaderAllocator;
    bool                m_fGCPressure;
    bool                m_fUnloaded;
    bool                m_fTerminated;
    bool                m_fMarked;
    int                 m_nGCCount;

    // Pre-allocated blocks of heap for collectible assemblies. Will be set to NULL as soon as it is 
    // used. See code in GetVSDHeapInitialBlock and GetCodeHeapInitialBlock
    BYTE *              m_pVSDHeapInitialAlloc;
    BYTE *              m_pCodeHeapInitialAlloc;

public:
    BYTE *GetVSDHeapInitialBlock(DWORD *pSize);
    BYTE *GetCodeHeapInitialBlock(const BYTE * loAddr, const BYTE * hiAddr, DWORD minimumSize, DWORD *pSize);

    BaseDomain *m_pDomain;

    // ExecutionManager caches
    void * m_pLastUsedCodeHeap;
    void * m_pLastUsedDynamicCodeHeap;
    void * m_pJumpStubCache;

    // LoaderAllocator GC Structures
    PTR_LoaderAllocator m_pLoaderAllocatorDestroyNext; // Used in LoaderAllocator GC process (during sweeping)
protected:
    void ClearMark();
    void Mark();
    bool Marked();

#ifdef FAT_DISPATCH_TOKENS
    struct DispatchTokenFatSHashTraits : public DefaultSHashTraits<DispatchTokenFat*>
    {
        typedef DispatchTokenFat* key_t;

        static key_t GetKey(element_t e)
            { return e; }

        static BOOL Equals(key_t k1, key_t k2)
            { return *k1 == *k2; }

        static count_t Hash(key_t k)
            { return (count_t)(size_t)*k; }
    };

    typedef SHash<DispatchTokenFatSHashTraits> FatTokenSet;
    SimpleRWLock *m_pFatTokenSetLock;
    FatTokenSet *m_pFatTokenSet;
#endif

#ifndef CROSSGEN_COMPILE
    VirtualCallStubManager *m_pVirtualCallStubManager;
#endif

private:
    typedef SHash<PtrSetSHashTraits<LoaderAllocator * > > LoaderAllocatorSet;

    LoaderAllocatorSet m_LoaderAllocatorReferences;
    Volatile<UINT32>   m_cReferences;
    // This will be set by code:LoaderAllocator::Destroy (from managed scout finalizer) and signalizes that 
    // the assembly was collected
    DomainAssembly * m_pDomainAssemblyToDelete;
    
    BOOL CheckAddReference_Unlocked(LoaderAllocator *pOtherLA);
    
    static UINT64 cLoaderAllocatorsCreated;
    UINT64 m_nLoaderAllocator;
    
    struct FailedTypeInitCleanupListItem
    {
        SLink m_Link;
        ListLockEntry *m_pListLockEntry;
        explicit FailedTypeInitCleanupListItem(ListLockEntry *pListLockEntry)
                :
            m_pListLockEntry(pListLockEntry)
        {
        }
    };

    SList<FailedTypeInitCleanupListItem> m_failedTypeInitCleanupList;

#ifndef DACCESS_COMPILE
    LOADERHANDLE AllocateHandle_Unlocked(OBJECTREF value);

public:
    // CleanupFailedTypeInit is called from AppDomain
    // This method accesses loader allocator state in a thread unsafe manner.
    // It expects to be called only from Terminate.
    void CleanupFailedTypeInit();
#endif //!DACCESS_COMPILE
    
    // Collect unreferenced assemblies, remove them from the assembly list and return their loader allocator 
    // list.
    static LoaderAllocator * GCLoaderAllocators_RemoveAssemblies(AppDomain * pAppDomain);
    
public:

    // 
    // The scheme for ensuring that LoaderAllocators are destructed correctly is substantially
    // complicated by the requirement that LoaderAllocators that are eligible for destruction
    // must be destroyed as a group due to issues where there may be ordering issues in destruction
    // of LoaderAllocators.
    // Thus, while there must be a complete web of references keeping the LoaderAllocator alive in
    // managed memory, we must also have an analogous web in native memory to manage the specific
    // ordering requirements.
    //
    // Thus we have an extra garbage collector here to manage the native web of LoaderAllocator references
    // Also, we have a reference count scheme so that LCG methods keep their associated LoaderAllocator
    // alive. LCG methods cannot be referenced by LoaderAllocators, so they do not need to participate
    // in the garbage collection scheme except by using AddRef/Release to adjust the root set of this
    // garbage collector.
    // 
    
    //#AssemblyPhases
    // The phases of unloadable assembly are:
    // 
    // 1. Managed LoaderAllocator is alive.
    //    - Assembly is visible to managed world, the managed scout is alive and was not finalized yet.
    //      Note that the fact that the managed scout is in the finalizer queue is not important as it can 
    //      (and in certain cases has to) ressurect itself.
    //    Detection:
    //        code:IsAlive ... TRUE
    //        code:IsManagedScoutAlive ... TRUE
    //        code:DomainAssembly::GetExposedAssemblyObject ... non-NULL (may need to allocate GC object)
    //        
    //        code:AddReferenceIfAlive ... TRUE (+ adds reference)
    // 
    // 2. Managed scout is alive, managed LoaderAllocator is collected.
    //    - All managed object related to this assembly (types, their instances, Assembly/AssemblyBuilder) 
    //      are dead and/or about to disappear and cannot be recreated anymore. We are just waiting for the 
    //      managed scout to run its finalizer.
    //    Detection:
    //        code:IsAlive ... TRUE
    //        code:IsManagedScoutAlive ... TRUE
    //        code:DomainAssembly::GetExposedAssemblyObject ... NULL (change from phase #1)
    //        
    //        code:AddReferenceIfAlive ... TRUE (+ adds reference)
    // 
    // 3. Native LoaderAllocator is alive, managed scout is collected.
    //    - The native LoaderAllocator can be kept alive via native reference with code:AddRef call, e.g.:
    //        * Reference from LCG method, 
    //        * Reference recieved from assembly iterator code:AppDomain::AssemblyIterator::Next and/or 
    //          held by code:CollectibleAssemblyHolder.
    //    - Other LoaderAllocator can have this LoaderAllocator in its reference list 
    //      (code:m_LoaderAllocatorReferences), but without call to code:AddRef.
    //    - LoaderAllocator cannot ever go back to phase #1 or #2, but it can skip this phase if there are 
    //      not any LCG method references keeping it alive at the time of manged scout finalization.
    //    Detection:
    //        code:IsAlive ... TRUE
    //        code:IsManagedScoutAlive ... FALSE (change from phase #2)
    //        code:DomainAssembly::GetExposedAssemblyObject ... NULL
    //        
    //        code:AddReferenceIfAlive ... TRUE (+ adds reference)
    // 
    // 4. LoaderAllocator is dead.
    //    - The managed scout was collected. No one holds a native reference with code:AddRef to this 
    //      LoaderAllocator.
    //    - Other LoaderAllocator can have this LoaderAllocator in its reference list 
    //      (code:m_LoaderAllocatorReferences), but without call to code:AddRef.
    //    - LoaderAllocator cannot ever become alive again (i.e. go back to phase #3, #2 or #1).
    //    Detection:
    //        code:IsAlive ... FALSE (change from phase #3, #2 and #1)
    //        
    //        code:AddReferenceIfAlive ... FALSE (change from phase #3, #2 and #1)
    // 
    
    void AddReference();
    // Adds reference if the native object is alive  - code:#AssemblyPhases.
    // Returns TRUE if the reference was added.
    BOOL AddReferenceIfAlive();
    BOOL Release();
    // Checks if the native object is alive - see code:#AssemblyPhases.
    BOOL IsAlive() { LIMITED_METHOD_DAC_CONTRACT; return (m_cReferences != (UINT32)0); }
    // Checks if managed scout is alive - see code:#AssemblyPhases.
    BOOL IsManagedScoutAlive()
    {
        return (m_pDomainAssemblyToDelete == NULL);
    }
    
    // Collect unreferenced assemblies, delete all their remaining resources.
    static void GCLoaderAllocators(AppDomain *pAppDomain);
    
    UINT64 GetCreationNumber() { LIMITED_METHOD_DAC_CONTRACT; return m_nLoaderAllocator; }

    // Ensure this LoaderAllocator has a reference to another LoaderAllocator
    BOOL EnsureReference(LoaderAllocator *pOtherLA);

    // Ensure this LoaderAllocator has a reference to every LoaderAllocator of the types
    // in an instantiation
    BOOL EnsureInstantiation(Module *pDefiningModule, Instantiation inst);

    // Given typeId and slotNumber, GetDispatchToken will return a DispatchToken
    // representing <typeId, slotNumber>. If the typeId is big enough, this
    // method will automatically allocate a DispatchTokenFat and encapsulate it
    // in the return value.
    DispatchToken GetDispatchToken(UINT32 typeId, UINT32 slotNumber);

    // Same as GetDispatchToken, but returns invalid DispatchToken  when the
    // value doesn't exist or a transient exception (OOM, stack overflow) is
    // encountered. To check if the token is valid, use DispatchToken::IsValid
    DispatchToken TryLookupDispatchToken(UINT32 typeId, UINT32 slotNumber);

    virtual LoaderAllocatorID* Id() =0;
    BOOL IsCollectible() { WRAPPER_NO_CONTRACT; return Id()->IsCollectible(); }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    PTR_LoaderHeap GetLowFrequencyHeap()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLowFrequencyHeap;
    }

    PTR_LoaderHeap GetHighFrequencyHeap()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHighFrequencyHeap;
    }

    PTR_LoaderHeap GetStubHeap()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pStubHeap;
    }

    PTR_CodeFragmentHeap GetPrecodeHeap()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pPrecodeHeap;
    }

    // The executable heap is intended to only be used by the global loader allocator.
    // It refers to executable memory that is not associated with a rangelist.
    PTR_LoaderHeap GetExecutableHeap()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pExecutableHeap;
    }

    PTR_CodeFragmentHeap GetDynamicHelpersHeap();

    FuncPtrStubs * GetFuncPtrStubs();

    FuncPtrStubs * GetFuncPtrStubsNoCreate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pFuncPtrStubs;
    }

    OBJECTHANDLE GetLoaderAllocatorObjectHandle()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hLoaderAllocatorObjectHandle;
    }

    LOADERALLOCATORREF GetExposedObject();

#ifndef DACCESS_COMPILE
    LOADERHANDLE AllocateHandle(OBJECTREF value);

    void SetHandleValue(LOADERHANDLE handle, OBJECTREF value);
    OBJECTREF CompareExchangeValueInHandle(LOADERHANDLE handle, OBJECTREF value, OBJECTREF compare);
    void ClearHandle(LOADERHANDLE handle);

    // The default implementation is a no-op. Only collectible loader allocators implement this method.
    virtual void RegisterHandleForCleanup(OBJECTHANDLE /* objHandle */) { }
    virtual void CleanupHandles() { }

    void RegisterFailedTypeInitForCleanup(ListLockEntry *pListLockEntry);
#endif // !defined(DACCESS_COMPILE)


    // This function is only safe to call if the handle is known to be a handle in a collectible
    // LoaderAllocator, and the handle is allocated, and the LoaderAllocator is also not collected.
    FORCEINLINE OBJECTREF GetHandleValueFastCannotFailType2(LOADERHANDLE handle);

    // These functions are designed to be used for maximum performance to access handle values
    // The GetHandleValueFast will handle the scenario where a loader allocator pointer does not
    // need to be acquired to do the handle lookup, and the GetHandleValueFastPhase2 handles
    // the scenario where the LoaderAllocator pointer is required.
    // Do not use these functions directly - use GET_LOADERHANDLE_VALUE_FAST macro instead.
    FORCEINLINE static BOOL GetHandleValueFast(LOADERHANDLE handle, OBJECTREF *pValue);
    FORCEINLINE BOOL GetHandleValueFastPhase2(LOADERHANDLE handle, OBJECTREF *pValue);

#define GET_LOADERHANDLE_VALUE_FAST(pLoaderAllocator, handle, pRetVal)              \
    do {                                                                            \
        LOADERHANDLE __handle__ = handle;                                           \
        if (!LoaderAllocator::GetHandleValueFast(__handle__, pRetVal) &&            \
            !pLoaderAllocator->GetHandleValueFastPhase2(__handle__, pRetVal))       \
        {                                                                           \
            *(pRetVal) = NULL;                                                      \
        }                                                                           \
    } while (0)

    OBJECTREF GetHandleValue(LOADERHANDLE handle);

    LoaderAllocator();
    virtual ~LoaderAllocator();
    BaseDomain *GetDomain() { LIMITED_METHOD_CONTRACT; return m_pDomain; }
    virtual BOOL CanUnload() = 0;
    BOOL IsDomainNeutral();
    void Init(BaseDomain *pDomain, BYTE *pExecutableHeapMemory = NULL);
    void Terminate();
    SIZE_T EstimateSize();

    void SetupManagedTracking(LOADERALLOCATORREF *pLoaderAllocatorKeepAlive);
    void ActivateManagedTracking();

    // Unloaded in this context means that there is no managed code running against this loader allocator.
    // This flag is used by debugger to filter out methods in modules that are being destructed.
    bool IsUnloaded() { LIMITED_METHOD_CONTRACT; return m_fUnloaded; }
    void SetIsUnloaded() { LIMITED_METHOD_CONTRACT; m_fUnloaded = true; }

    void SetGCRefPoint(int gccounter)
    {
        LIMITED_METHOD_CONTRACT;
        m_nGCCount=gccounter;
    }
    int GetGCRefPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nGCCount;
    }

    static BOOL QCALLTYPE Destroy(QCall::LoaderAllocatorHandle pLoaderAllocator);

    //****************************************************************************************
    // Methods to retrieve a pointer to the COM+ string STRINGREF for a string constant.
    // If the string is not currently in the hash table it will be added and if the
    // copy string flag is set then the string will be copied before it is inserted.
    STRINGREF *GetStringObjRefPtrFromUnicodeString(EEStringData *pStringData);
    void LazyInitStringLiteralMap();
    STRINGREF *IsStringInterned(STRINGREF *pString);
    STRINGREF *GetOrInternString(STRINGREF *pString);
    void CleanupStringLiteralMap();

    void InitVirtualCallStubManager(BaseDomain *pDomain, BOOL fCollectible = FALSE);
    void UninitVirtualCallStubManager();
#ifndef CROSSGEN_COMPILE
    inline VirtualCallStubManager *GetVirtualCallStubManager()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pVirtualCallStubManager;
    }
#endif
};  // class LoaderAllocator

typedef VPTR(LoaderAllocator) PTR_LoaderAllocator;

class GlobalLoaderAllocator : public LoaderAllocator
{
    VPTR_VTABLE_CLASS(GlobalLoaderAllocator, LoaderAllocator)
    VPTR_UNIQUE(VPTRU_LoaderAllocator+1);

    BYTE                m_ExecutableHeapInstance[sizeof(LoaderHeap)];

protected:
    LoaderAllocatorID m_Id;
    
public:
    void Init(BaseDomain *pDomain);
    GlobalLoaderAllocator() : m_Id(LAT_Global, (void*)1) { LIMITED_METHOD_CONTRACT;};
    virtual LoaderAllocatorID* Id();
    virtual BOOL CanUnload();
};

typedef VPTR(GlobalLoaderAllocator) PTR_GlobalLoaderAllocator;


class AppDomainLoaderAllocator : public LoaderAllocator
{
    VPTR_VTABLE_CLASS(AppDomainLoaderAllocator, LoaderAllocator)
    VPTR_UNIQUE(VPTRU_LoaderAllocator+2);

protected:
    LoaderAllocatorID m_Id;
public:    
    AppDomainLoaderAllocator() : m_Id(LAT_AppDomain) { LIMITED_METHOD_CONTRACT;};
    void Init(AppDomain *pAppDomain);
    virtual LoaderAllocatorID* Id();
    virtual BOOL CanUnload();
};

typedef VPTR(AppDomainLoaderAllocator) PTR_AppDomainLoaderAllocator;

class AssemblyLoaderAllocator : public LoaderAllocator
{
    VPTR_VTABLE_CLASS(AssemblyLoaderAllocator, LoaderAllocator)
    VPTR_UNIQUE(VPTRU_LoaderAllocator+3);

protected:
    LoaderAllocatorID m_Id;
public:    
    virtual LoaderAllocatorID* Id();
    AssemblyLoaderAllocator() : m_Id(LAT_Assembly) { LIMITED_METHOD_CONTRACT; }
    void Init(AppDomain *pAppDomain);
    virtual BOOL CanUnload();
    void SetDomainAssembly(DomainAssembly *pDomainAssembly) { WRAPPER_NO_CONTRACT; m_Id.SetDomainAssembly(pDomainAssembly); }

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    virtual void RegisterHandleForCleanup(OBJECTHANDLE objHandle);
    virtual void CleanupHandles();
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

private:
    struct HandleCleanupListItem
    {    
        SLink m_Link;
        OBJECTHANDLE m_handle;
        explicit HandleCleanupListItem(OBJECTHANDLE handle)
                :
            m_handle(handle)
        {
        }
    };
    
    SList<HandleCleanupListItem> m_handleCleanupList;
};

typedef VPTR(AssemblyLoaderAllocator) PTR_AssemblyLoaderAllocator;


#include "loaderallocator.inl"

#endif //  __LoaderAllocator_h__

