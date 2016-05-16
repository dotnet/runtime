// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#ifdef FEATURE_FUSION
#include "fusion.h"
#endif
#include "arraylist.h"
#include "comreflectioncache.hpp"
#include "comutilnative.h"
#include "domainfile.h"
#include "objectlist.h"
#include "fptrstubs.h"
#include "ilstubcache.h"
#include "testhookmgr.h"
#ifdef FEATURE_VERSIONING
#include "../binder/inc/applicationcontext.hpp"
#endif // FEATURE_VERSIONING
#include "rejit.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "clrprivbinderwinrt.h"
#ifndef FEATURE_CORECLR
#include "clrprivbinderreflectiononlywinrt.h"
#include "clrprivtypecachereflectiononlywinrt.h"
#endif
#include "..\md\winmd\inc\adapter.h"
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP

#include "appxutil.h"

class BaseDomain;
class SystemDomain;
class SharedDomain;
class AppDomain;
class CompilationDomain;
class AppDomainEnum;
class AssemblySink;
class EEMarshalingData;
class Context;
class GlobalStringLiteralMap;
class StringLiteralMap;
struct SecurityContext;
class MngStdInterfacesInfo;
class DomainModule;
class DomainAssembly;
struct InteropMethodTableData;
class LoadLevelLimiter;
class UMEntryThunkCache;
class TypeEquivalenceHashTable;
class IApplicationSecurityDescriptor;
class StringArrayList;

typedef VPTR(IApplicationSecurityDescriptor) PTR_IApplicationSecurityDescriptor;

extern INT64 g_PauseTime;  // Total time in millisecond the CLR has been paused

#ifdef FEATURE_COMINTEROP
class ComCallWrapperCache;
struct SimpleComCallWrapper;

class RCWRefCache;

// This enum is used to specify whether user want COM or remoting
enum COMorRemotingFlag {
    COMorRemoting_NotInitialized = 0,
    COMorRemoting_COM            = 1, // COM will be used both cross-domain and cross-runtime
    COMorRemoting_Remoting       = 2, // Remoting will be used cross-domain; cross-runtime will use Remoting only if it looks like it's expected (default)
    COMorRemoting_LegacyMode     = 3  // Remoting will be used both cross-domain and cross-runtime
};

#endif // FEATURE_COMINTEROP

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4200) // Disable zero-sized array warning
#endif


GPTR_DECL(IdDispenser,       g_pModuleIndexDispenser);

// This enum is aligned to System.ExceptionCatcherType. 
enum ExceptionCatcher {
    ExceptionCatcher_ManagedCode = 0,
    ExceptionCatcher_AppDomainTransition = 1,
    ExceptionCatcher_COMInterop = 2,    
};

// We would like *ALLOCATECLASS_FLAG to AV (in order to catch errors), so don't change it
struct ClassInitFlags {
    enum
    {
        INITIALIZED_FLAG_BIT    = 0,
        INITIALIZED_FLAG        = 1<<INITIALIZED_FLAG_BIT,
        ERROR_FLAG_BIT          = 1,
        ERROR_FLAG              = 1<<ERROR_FLAG_BIT,
        ALLOCATECLASS_FLAG_BIT  = 2,                    // Bit to avoid racing for InstantiateStaticHandles
        ALLOCATECLASS_FLAG      = 1<<ALLOCATECLASS_FLAG_BIT,
        COLLECTIBLE_FLAG_BIT    = 3,
        COLLECTIBLE_FLAG        = 1<<COLLECTIBLE_FLAG_BIT
    };
};

struct DomainLocalModule
{
    friend class ClrDataAccess;
    friend class CheckAsmOffsets;
    friend struct ThreadLocalModule;

// After these macros complete, they may have returned an interior pointer into a gc object. This pointer will have been cast to a byte pointer
// It is critically important that no GC is allowed to occur before this pointer is used.
#define GET_DYNAMICENTRY_GCSTATICS_BASEPOINTER(pLoaderAllocator, dynamicClassInfoParam, pGCStatics) \
    {\
        DomainLocalModule::PTR_DynamicClassInfo dynamicClassInfo = dac_cast<DomainLocalModule::PTR_DynamicClassInfo>(dynamicClassInfoParam);\
        DomainLocalModule::PTR_DynamicEntry pDynamicEntry = dac_cast<DomainLocalModule::PTR_DynamicEntry>((DomainLocalModule::DynamicEntry*)dynamicClassInfo->m_pDynamicEntry.Load()); \
        if ((dynamicClassInfo->m_dwFlags) & ClassInitFlags::COLLECTIBLE_FLAG) \
        {\
            PTRARRAYREF objArray;\
            objArray = (PTRARRAYREF)pLoaderAllocator->GetHandleValueFastCannotFailType2( \
                                        (dac_cast<DomainLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hGCStatics);\
            *(pGCStatics) = dac_cast<PTR_BYTE>(PTR_READ(PTR_TO_TADDR(OBJECTREFToObject( objArray )) + offsetof(PtrArray, m_Array), objArray->GetNumComponents() * sizeof(void*))) ;\
        }\
        else\
        {\
            *(pGCStatics) = (dac_cast<DomainLocalModule::PTR_NormalDynamicEntry>(pDynamicEntry))->GetGCStaticsBasePointer();\
        }\
    }\

#define GET_DYNAMICENTRY_NONGCSTATICS_BASEPOINTER(pLoaderAllocator, dynamicClassInfoParam, pNonGCStatics) \
    {\
        DomainLocalModule::PTR_DynamicClassInfo dynamicClassInfo = dac_cast<DomainLocalModule::PTR_DynamicClassInfo>(dynamicClassInfoParam);\
        DomainLocalModule::PTR_DynamicEntry pDynamicEntry = dac_cast<DomainLocalModule::PTR_DynamicEntry>((DomainLocalModule::DynamicEntry*)(dynamicClassInfo)->m_pDynamicEntry.Load()); \
        if (((dynamicClassInfo)->m_dwFlags) & ClassInitFlags::COLLECTIBLE_FLAG) \
        {\
            if ((dac_cast<DomainLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hNonGCStatics != 0) \
            { \
                U1ARRAYREF objArray;\
                objArray = (U1ARRAYREF)pLoaderAllocator->GetHandleValueFastCannotFailType2( \
                                            (dac_cast<DomainLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hNonGCStatics);\
                *(pNonGCStatics) = dac_cast<PTR_BYTE>(PTR_READ( \
                        PTR_TO_TADDR(OBJECTREFToObject( objArray )) + sizeof(ArrayBase) - DomainLocalModule::DynamicEntry::GetOffsetOfDataBlob(), \
                            objArray->GetNumComponents() * (DWORD)objArray->GetComponentSize() + DomainLocalModule::DynamicEntry::GetOffsetOfDataBlob())); \
            } else (*pNonGCStatics) = NULL; \
        }\
        else\
        {\
            *(pNonGCStatics) = dac_cast<DomainLocalModule::PTR_NormalDynamicEntry>(pDynamicEntry)->GetNonGCStaticsBasePointer();\
        }\
    }\

    struct DynamicEntry
    {
        static DWORD GetOffsetOfDataBlob();
    };
    typedef DPTR(DynamicEntry) PTR_DynamicEntry;

    struct CollectibleDynamicEntry : public DynamicEntry
    {
        LOADERHANDLE    m_hGCStatics;
        LOADERHANDLE    m_hNonGCStatics;
    };
    typedef DPTR(CollectibleDynamicEntry) PTR_CollectibleDynamicEntry;

    struct NormalDynamicEntry : public DynamicEntry
    {
        PTR_OBJECTREF   m_pGCStatics;
#ifdef FEATURE_64BIT_ALIGNMENT
        // Padding to make m_pDataBlob aligned at MAX_PRIMITIVE_FIELD_SIZE
        // code:MethodTableBuilder::PlaceRegularStaticFields assumes that the start of the data blob is aligned 
        SIZE_T          m_padding;
#endif
        BYTE            m_pDataBlob[0];

        inline PTR_BYTE GetGCStaticsBasePointer()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            return dac_cast<PTR_BYTE>(m_pGCStatics);
        }
        inline PTR_BYTE GetNonGCStaticsBasePointer()
        {
            LIMITED_METHOD_CONTRACT
            SUPPORTS_DAC;
            return dac_cast<PTR_BYTE>(this);
        }
    };
    typedef DPTR(NormalDynamicEntry) PTR_NormalDynamicEntry;

    struct DynamicClassInfo
    {
        VolatilePtr<DynamicEntry, PTR_DynamicEntry>  m_pDynamicEntry;
        Volatile<DWORD>             m_dwFlags;
    };
    typedef DPTR(DynamicClassInfo) PTR_DynamicClassInfo;
    
    inline UMEntryThunk * GetADThunkTable()
    {
        LIMITED_METHOD_CONTRACT
        return m_pADThunkTable;
    }

    inline void SetADThunkTable(UMEntryThunk* pADThunkTable)
    {
        LIMITED_METHOD_CONTRACT
        InterlockedCompareExchangeT(m_pADThunkTable.GetPointer(), pADThunkTable, NULL);
    }

    // Note the difference between:
    // 
    //  GetPrecomputedNonGCStaticsBasePointer() and
    //  GetPrecomputedStaticsClassData()
    //
    //  GetPrecomputedNonGCStaticsBasePointer returns the pointer that should be added to field offsets to retrieve statics
    //  GetPrecomputedStaticsClassData returns a pointer to the first byte of the precomputed statics block
    inline TADDR GetPrecomputedNonGCStaticsBasePointer()
    {
        LIMITED_METHOD_CONTRACT
        return dac_cast<TADDR>(this);
    }

    inline PTR_BYTE GetPrecomputedStaticsClassData()
    {
        LIMITED_METHOD_CONTRACT
        return dac_cast<PTR_BYTE>(this) + offsetof(DomainLocalModule, m_pDataBlob);
    }

    static SIZE_T GetOffsetOfDataBlob() { return offsetof(DomainLocalModule, m_pDataBlob); }
    static SIZE_T GetOffsetOfGCStaticPointer() { return offsetof(DomainLocalModule, m_pGCStatics); }

    inline DomainFile* GetDomainFile()
    {
        LIMITED_METHOD_CONTRACT
        SUPPORTS_DAC;
        return m_pDomainFile;
    }

#ifndef DACCESS_COMPILE
    inline void        SetDomainFile(DomainFile* pDomainFile)
    {
        LIMITED_METHOD_CONTRACT
        m_pDomainFile = pDomainFile;
    }
#endif

    inline PTR_OBJECTREF  GetPrecomputedGCStaticsBasePointer()
    {
        LIMITED_METHOD_CONTRACT        
        return m_pGCStatics;
    }

    inline PTR_OBJECTREF * GetPrecomputedGCStaticsBasePointerAddress()
    {
        LIMITED_METHOD_CONTRACT        
        return &m_pGCStatics;
    }

    // Returns bytes so we can add offsets
    inline PTR_BYTE GetGCStaticsBasePointer(MethodTable * pMT)
    {
        WRAPPER_NO_CONTRACT
        SUPPORTS_DAC;

        if (pMT->IsDynamicStatics())
        {
            _ASSERTE(GetDomainFile()->GetModule() == pMT->GetModuleForStatics());
            return GetDynamicEntryGCStaticsBasePointer(pMT->GetModuleDynamicEntryID(), pMT->GetLoaderAllocator());
        }
        else
        {
            return dac_cast<PTR_BYTE>(m_pGCStatics);
        }
    }

    inline PTR_BYTE GetNonGCStaticsBasePointer(MethodTable * pMT)
    {
        WRAPPER_NO_CONTRACT
        SUPPORTS_DAC;

        if (pMT->IsDynamicStatics())
        {
            _ASSERTE(GetDomainFile()->GetModule() == pMT->GetModuleForStatics());
            return GetDynamicEntryNonGCStaticsBasePointer(pMT->GetModuleDynamicEntryID(), pMT->GetLoaderAllocator());
        }
        else
        {
            return dac_cast<PTR_BYTE>(this);
        }
    }

    inline DynamicClassInfo* GetDynamicClassInfo(DWORD n)
    {
        LIMITED_METHOD_CONTRACT
        SUPPORTS_DAC;
        _ASSERTE(m_pDynamicClassTable.Load() && m_aDynamicEntries > n);
        dac_cast<PTR_DynamicEntry>(m_pDynamicClassTable[n].m_pDynamicEntry.Load());

        return &m_pDynamicClassTable[n];
    }

    // These helpers can now return null, as the debugger may do queries on a type
    // before the calls to PopulateClass happen
    inline PTR_BYTE GetDynamicEntryGCStaticsBasePointer(DWORD n, PTR_LoaderAllocator pLoaderAllocator)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;


        if (n >= m_aDynamicEntries)
        {
            return NULL;
        }
        
        DynamicClassInfo* pClassInfo = GetDynamicClassInfo(n);
        if (!pClassInfo->m_pDynamicEntry)
        {
            return NULL;
        }

        PTR_BYTE retval = NULL;

        GET_DYNAMICENTRY_GCSTATICS_BASEPOINTER(pLoaderAllocator, pClassInfo, &retval);

        return retval;
    }

    inline PTR_BYTE GetDynamicEntryNonGCStaticsBasePointer(DWORD n, PTR_LoaderAllocator pLoaderAllocator)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        
        if (n >= m_aDynamicEntries)
        {
            return NULL;
        }
        
        DynamicClassInfo* pClassInfo = GetDynamicClassInfo(n);
        if (!pClassInfo->m_pDynamicEntry)
        {
            return NULL;
        }

        PTR_BYTE retval = NULL;

        GET_DYNAMICENTRY_NONGCSTATICS_BASEPOINTER(pLoaderAllocator, pClassInfo, &retval);

        return retval;
    }

    FORCEINLINE PTR_DynamicClassInfo GetDynamicClassInfoIfInitialized(DWORD n)
    {
        WRAPPER_NO_CONTRACT;

        // m_aDynamicEntries is set last, it needs to be checked first
        if (n >= m_aDynamicEntries)
        {
            return NULL;
        }

        _ASSERTE(m_pDynamicClassTable.Load() != NULL);
        PTR_DynamicClassInfo pDynamicClassInfo = (PTR_DynamicClassInfo)(m_pDynamicClassTable.Load() + n);

        // INITIALIZED_FLAG is set last, it needs to be checked first
        if ((pDynamicClassInfo->m_dwFlags & ClassInitFlags::INITIALIZED_FLAG) == 0)
        {
            return NULL;
        }

        PREFIX_ASSUME(pDynamicClassInfo != NULL);
        return pDynamicClassInfo;
    }

    // iClassIndex is slightly expensive to compute, so if we already know
    // it, we can use this helper
    inline BOOL IsClassInitialized(MethodTable* pMT, DWORD iClassIndex = (DWORD)-1)
    {
        WRAPPER_NO_CONTRACT;
        return (GetClassFlags(pMT, iClassIndex) & ClassInitFlags::INITIALIZED_FLAG) != 0;
    }

    inline BOOL IsPrecomputedClassInitialized(DWORD classID)
    {
        return GetPrecomputedStaticsClassData()[classID] & ClassInitFlags::INITIALIZED_FLAG;
    }
    
    inline BOOL IsClassAllocated(MethodTable* pMT, DWORD iClassIndex = (DWORD)-1)
    {
        WRAPPER_NO_CONTRACT;
        return (GetClassFlags(pMT, iClassIndex) & ClassInitFlags::ALLOCATECLASS_FLAG) != 0;
    }

    BOOL IsClassInitError(MethodTable* pMT, DWORD iClassIndex = (DWORD)-1)
    {
        WRAPPER_NO_CONTRACT;
        return (GetClassFlags(pMT, iClassIndex) & ClassInitFlags::ERROR_FLAG) != 0;
    }

    void    SetClassInitialized(MethodTable* pMT);
    void    SetClassInitError(MethodTable* pMT);

    void    EnsureDynamicClassIndex(DWORD dwID);

    void    AllocateDynamicClass(MethodTable *pMT);

    void    PopulateClass(MethodTable *pMT);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    static DWORD OffsetOfDataBlob()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(DomainLocalModule, m_pDataBlob);
    }

    FORCEINLINE MethodTable * GetMethodTableFromClassDomainID(DWORD dwClassDomainID)
    {
        DWORD rid = (DWORD)(dwClassDomainID) + 1;
        TypeHandle th = GetDomainFile()->GetModule()->LookupTypeDef(TokenFromRid(rid, mdtTypeDef));
        _ASSERTE(!th.IsNull());
        MethodTable * pMT = th.AsMethodTable();
        PREFIX_ASSUME(pMT != NULL);
        return pMT;
    }

private:
    friend void EmitFastGetSharedStaticBase(CPUSTUBLINKER *psl, CodeLabel *init, bool bCCtorCheck);

    void SetClassFlags(MethodTable* pMT, DWORD dwFlags);
    DWORD GetClassFlags(MethodTable* pMT, DWORD iClassIndex);

    PTR_DomainFile           m_pDomainFile;
    VolatilePtr<DynamicClassInfo, PTR_DynamicClassInfo> m_pDynamicClassTable;   // used for generics and reflection.emit in memory
    Volatile<SIZE_T>         m_aDynamicEntries;      // number of entries in dynamic table
    VolatilePtr<UMEntryThunk> m_pADThunkTable;
    PTR_OBJECTREF            m_pGCStatics;           // Handle to GC statics of the module

    // In addition to storing the ModuleIndex in the Module class, we also
    // keep a copy of the ModuleIndex in the DomainLocalModule class. This
    // allows the thread static JIT helpers to quickly convert a pointer to
    // a DomainLocalModule into a ModuleIndex.
    ModuleIndex             m_ModuleIndex;

    // Note that the static offset calculation in code:Module::BuildStaticsOffsets takes the offset m_pDataBlob
    // into consideration for alignment so we do not need any padding to ensure that the start of the data blob is aligned

    BYTE                     m_pDataBlob[0];         // First byte of the statics blob

    // Layout of m_pDataBlob is:
    //              ClassInit bytes (hold flags for cctor run, cctor error, etc)
    //              Non GC Statics

public:

    // The Module class need to be able to initialized ModuleIndex,
    // so for now I will make it a friend..
    friend class Module;

    FORCEINLINE ModuleIndex GetModuleIndex()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ModuleIndex;
    }

};  // struct DomainLocalModule


typedef DPTR(class DomainLocalBlock) PTR_DomainLocalBlock;
class DomainLocalBlock
{
    friend class ClrDataAccess;
    friend class CheckAsmOffsets;

private:
    PTR_AppDomain          m_pDomain;
    DPTR(PTR_DomainLocalModule) m_pModuleSlots;
    SIZE_T                 m_aModuleIndices;               // Module entries the shared block has allocated

public: // used by code generators
    static SIZE_T GetOffsetOfModuleSlotsPointer() { return offsetof(DomainLocalBlock, m_pModuleSlots);}

public:

#ifndef DACCESS_COMPILE
    DomainLocalBlock()
      : m_pDomain(NULL),  m_pModuleSlots(NULL), m_aModuleIndices(0) {}

    void    EnsureModuleIndex(ModuleIndex index);

    void Init(AppDomain *pDomain) { LIMITED_METHOD_CONTRACT; m_pDomain = pDomain; }
#endif

    void SetModuleSlot(ModuleIndex index, PTR_DomainLocalModule pLocalModule);

    FORCEINLINE PTR_DomainLocalModule GetModuleSlot(ModuleIndex index)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        _ASSERTE(index.m_dwIndex < m_aModuleIndices);
        return m_pModuleSlots[index.m_dwIndex];
    }

    inline PTR_DomainLocalModule GetModuleSlot(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;
        return GetModuleSlot(pMT->GetModuleForStatics()->GetModuleIndex());
    }

    DomainFile* TryGetDomainFile(ModuleIndex index)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        // the publishing of m_aModuleIndices and m_pModuleSlots is dependent
        // on the order of accesses; we must ensure that we read from m_aModuleIndices
        // before m_pModuleSlots.
        if (index.m_dwIndex < m_aModuleIndices)
        {
            MemoryBarrier();
            if (m_pModuleSlots[index.m_dwIndex])
            {
                return m_pModuleSlots[index.m_dwIndex]->GetDomainFile();
            }
        }

        return NULL;
    }

    DomainFile* GetDomainFile(SIZE_T ModuleID)
    {
        WRAPPER_NO_CONTRACT;
        ModuleIndex index = Module::IDToIndex(ModuleID);
        _ASSERTE(index.m_dwIndex < m_aModuleIndices);
        return m_pModuleSlots[index.m_dwIndex]->GetDomainFile();
    }

#ifndef DACCESS_COMPILE
    void SetDomainFile(ModuleIndex index, DomainFile* pDomainFile)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(index.m_dwIndex < m_aModuleIndices);
        m_pModuleSlots[index.m_dwIndex]->SetDomainFile(pDomainFile);
    }
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif


private:

    //
    // Low level routines to get & set class entries
    //

};

#ifdef _MSC_VER
#pragma warning(pop)
#endif


// The large heap handle bucket class is used to contain handles allocated
// from an array contained in the large heap.
class LargeHeapHandleBucket
{
public:
    // Constructor and desctructor.
    LargeHeapHandleBucket(LargeHeapHandleBucket *pNext, DWORD Size, BaseDomain *pDomain, BOOL bCrossAD = FALSE);
    ~LargeHeapHandleBucket();

    // This returns the next bucket.
    LargeHeapHandleBucket *GetNext()
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

private:
    LargeHeapHandleBucket *m_pNext;
    int m_ArraySize;
    int m_CurrentPos;
    int m_CurrentEmbeddedFreePos;
    OBJECTHANDLE m_hndHandleArray;
    OBJECTREF *m_pArrayDataPtr;
};



// The large heap handle table is used to allocate handles that are pointers
// to objects stored in an array in the large object heap.
class LargeHeapHandleTable
{
public:
    // Constructor and desctructor.
    LargeHeapHandleTable(BaseDomain *pDomain, DWORD InitialBucketSize);
    ~LargeHeapHandleTable();

    // Allocate handles from the large heap handle table.
    OBJECTREF* AllocateHandles(DWORD nRequested, BOOL bCrossAD = FALSE);

    // Release object handles allocated using AllocateHandles().
    void ReleaseHandles(OBJECTREF *pObjRef, DWORD nReleased);    

private:
    // The buckets of object handles.
    LargeHeapHandleBucket *m_pHead;

    // We need to know the containing domain so we know where to allocate handles
    BaseDomain *m_pDomain;

    // The size of the LargeHeapHandleBuckets.
    DWORD m_NextBucketSize;

    // for finding and re-using embedded free items in the list
    LargeHeapHandleBucket *m_pFreeSearchHint;
    DWORD m_cEmbeddedFree;   

#ifdef _DEBUG

    // these functions are present to enforce that there is a locking mechanism in place
    // for each LargeHeapHandleTable even though the code itself does not do the locking
    // you must tell the table which lock you intend to use and it will verify that it has
    // in fact been taken before performing any operations

public:
    void RegisterCrstDebug(CrstBase *pCrst)
    {
        LIMITED_METHOD_CONTRACT;

        // this function must be called exactly once
        _ASSERTE(pCrst != NULL);
        _ASSERTE(m_pCrstDebug == NULL);
        m_pCrstDebug = pCrst;
    }

private:
    // we will assert that this Crst is held before using the object
    CrstBase *m_pCrstDebug;

#endif
    
};

class LargeHeapHandleBlockHolder;
void LargeHeapHandleBlockHolder__StaticFree(LargeHeapHandleBlockHolder*);


class LargeHeapHandleBlockHolder:public Holder<LargeHeapHandleBlockHolder*,DoNothing,LargeHeapHandleBlockHolder__StaticFree>

{
    LargeHeapHandleTable* m_pTable;
    DWORD m_Count;
    OBJECTREF* m_Data;
public:
    FORCEINLINE LargeHeapHandleBlockHolder(LargeHeapHandleTable* pOwner, DWORD nCount)
    {
        WRAPPER_NO_CONTRACT;
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

FORCEINLINE  void LargeHeapHandleBlockHolder__StaticFree(LargeHeapHandleBlockHolder* pHolder)
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
// for creating assemblies in the the domain. The system domain only has one assembly, it
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

#define LOW_FREQUENCY_HEAP_RESERVE_SIZE        (3 * PAGE_SIZE)
#define LOW_FREQUENCY_HEAP_COMMIT_SIZE         (1 * PAGE_SIZE)

#define HIGH_FREQUENCY_HEAP_RESERVE_SIZE       (10 * PAGE_SIZE)
#define HIGH_FREQUENCY_HEAP_COMMIT_SIZE        (1 * PAGE_SIZE)

#define STUB_HEAP_RESERVE_SIZE                 (3 * PAGE_SIZE)
#define STUB_HEAP_COMMIT_SIZE                  (1 * PAGE_SIZE)

// --------------------------------------------------------------------------------
// PE File List lock - for creating list locks on PE files
// --------------------------------------------------------------------------------

class PEFileListLock : public ListLock
{
public:
#ifndef DACCESS_COMPILE
    ListLockEntry *FindFileLock(PEFile *pFile)
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
            if (((PEFile *)pEntry->m_pData)->Equals(pFile))
            {
                return pEntry;
            }
        }

        return NULL;
    }
#endif // DACCESS_COMPILE

    DEBUG_NOINLINE static void HolderEnter(PEFileListLock *pThis) PUB
    {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
        
        pThis->Enter();
    }

    DEBUG_NOINLINE static void HolderLeave(PEFileListLock *pThis) PUB
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
// a DomainFile is a file being loaded.  Files are loaded in layers to enable loading in the
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
// LoadLock level behavior, will prevent any deadlocks from occuring due to circular
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
    DomainFile              *m_pDomainFile;
    HRESULT                 m_cachedHR;
    ADID                    m_AppDomainId;

public:
    static FileLoadLock *Create(PEFileListLock *pLock, PEFile *pFile, DomainFile *pDomainFile);

    ~FileLoadLock();
    DomainFile *GetDomainFile();
    ADID GetAppDomainId();
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

    FileLoadLock(PEFileListLock *pLock, PEFile *pFile, DomainFile *pDomainFile);

    static void HolderLeave(FileLoadLock *pThis);

public:
    typedef Wrapper<FileLoadLock *, DoNothing, FileLoadLock::HolderLeave> Holder;

};

typedef FileLoadLock::Holder FileLoadLockHolder;

#ifndef DACCESS_COMPILE
    typedef ReleaseHolder<FileLoadLock> FileLoadLockRefHolder;
#endif // DACCESS_COMPILE


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
        m_previousLimit=GetThread()->GetLoadLevelLimiter();
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
    friend class AppDomainNative;

    VPTR_BASE_VTABLE_CLASS(BaseDomain)
    VPTR_UNIQUE(VPTR_UNIQUE_BaseDomain)

protected:
    // These 2 variables are only used on the AppDomain, but by placing them here
    // we reduce the cost of keeping the asmconstants file up to date.

    // The creation sequence number of this app domain (starting from 1)
    // This ID is generated by the code:SystemDomain::GetNewAppDomainId routine
    // The ID are recycled. 
    // 
    // see also code:ADID 
    ADID m_dwId;

    DomainLocalBlock    m_sDomainLocalBlock;

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
    void Terminate();

    // ID to uniquely identify this AppDomain - used by the AppDomain publishing
    // service (to publish the list of all appdomains present in the process),
    // which in turn is used by, for eg., the debugger (to decide which App-
    // Domain(s) to attach to).
    // This is also used by Remoting for routing cross-appDomain calls.
    ADID GetId (void)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return m_dwId;
    }
    
    virtual BOOL IsAppDomain()    { LIMITED_METHOD_DAC_CONTRACT; return FALSE; }
    virtual BOOL IsSharedDomain() { LIMITED_METHOD_DAC_CONTRACT; return FALSE; }

    inline BOOL IsDefaultDomain();  // defined later in this file
    virtual PTR_LoaderAllocator GetLoaderAllocator() = 0;
    virtual PTR_AppDomain AsAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        _ASSERTE(!"Not an AppDomain");
        return NULL;
    }

    
    // If one domain is the SharedDomain and one is an AppDomain then
    // return the AppDomain, i.e. return the domain with the shorter lifetime
    // of the two given domains.
    static PTR_BaseDomain ComputeBaseDomain(
         BaseDomain *pGenericDefinitionDomain,    // the domain that owns the generic type or method
         Instantiation classInst,                       // the type arguments to the type (if any)
         Instantiation methodInst = Instantiation());   // the type arguments to the method (if any)

    static PTR_BaseDomain ComputeBaseDomain(TypeKey * pTypeKey);

#ifdef FEATURE_COMINTEROP
    //****************************************************************************************
    //
    // This will look up interop data for a method table
    //

#ifndef DACCESS_COMPILE
    // Returns the data pointer if present, NULL otherwise
    InteropMethodTableData *LookupComInteropData(MethodTable *pMT)
    {
        // Take the lock
        CrstHolder holder(&m_InteropDataCrst);

        // Lookup
        InteropMethodTableData *pData = (InteropMethodTableData*) m_interopDataHash.LookupValue((UPTR) pMT, (LPVOID) NULL);

        // Not there...
        if (pData == (InteropMethodTableData*) INVALIDENTRY)
            return NULL;

        // Found it
        return pData;
    }

    // Returns TRUE if successfully inserted, FALSE if this would be a duplicate entry
    BOOL InsertComInteropData(MethodTable* pMT, InteropMethodTableData *pData)
    {
        // We don't keep track of this kind of information for interfaces
        _ASSERTE(!pMT->IsInterface());

        // Take the lock
        CrstHolder holder(&m_InteropDataCrst);

        // Check to see that it's not already in there
        InteropMethodTableData *pDupData = (InteropMethodTableData*) m_interopDataHash.LookupValue((UPTR) pMT, (LPVOID) NULL);
        if (pDupData != (InteropMethodTableData*) INVALIDENTRY)
            return FALSE;

        // Not in there, so insert
        m_interopDataHash.InsertValue((UPTR) pMT, (LPVOID) pData);

        // Success
        return TRUE;
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_COMINTEROP

    void SetDisableInterfaceCache()
    {
        m_fDisableInterfaceCache = TRUE;
    }
    BOOL GetDisableInterfaceCache()
    {
        return m_fDisableInterfaceCache;
    }

#ifdef FEATURE_COMINTEROP
    MngStdInterfacesInfo * GetMngStdInterfacesInfo()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pMngStdInterfacesInfo;
    }
    
    PTR_CLRPrivBinderWinRT GetWinRtBinder()
    {
        return m_pWinRtBinder;
    }
#endif // FEATURE_COMINTEROP

    //****************************************************************************************
    // This method returns marshaling data that the EE uses that is stored on a per app domain
    // basis.
    EEMarshalingData *GetMarshalingData();

    // Deletes marshaling data at shutdown (which contains cached factories that needs to be released)
    void DeleteMarshalingData();
    
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

    ListLock* GetJitLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_JITLock;
    }

    ListLock* GetILStubGenLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_ILStubGenLock;
    }

    STRINGREF *IsStringInterned(STRINGREF *pString);
    STRINGREF *GetOrInternString(STRINGREF *pString);

    virtual BOOL CanUnload()   { LIMITED_METHOD_CONTRACT; return FALSE; }    // can never unload BaseDomain

    // Returns an array of OBJECTREF* that can be used to store domain specific data.
    // Statics and reflection info (Types, MemberInfo,..) are stored this way
    // If ppLazyAllocate != 0, allocation will only take place if *ppLazyAllocate != 0 (and the allocation
    // will be properly serialized)
    OBJECTREF *AllocateObjRefPtrsInLargeTable(int nRequested, OBJECTREF** ppLazyAllocate = NULL, BOOL bCrossAD = FALSE);

#ifdef FEATURE_PREJIT
    // Ensures that the file for logging profile data is open (we only open it once)
    // return false on failure
    static BOOL EnsureNGenLogFileOpen();
#endif

    //****************************************************************************************
    // Handles

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE) // needs GetCurrentThreadHomeHeapNumber
    OBJECTHANDLE CreateTypedHandle(OBJECTREF object, int type)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateTypedHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object, type);
    }

    OBJECTHANDLE CreateHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL)
        return ::CreateHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateWeakHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateShortWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateShortWeakHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateLongWeakHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL)
        return ::CreateLongWeakHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateStrongHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateStrongHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreatePinningHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
#if CHECK_APP_DOMAIN_LEAKS     
        if(IsAppDomain())
            object->TryAssignAppDomain((AppDomain*)this,TRUE);
#endif
        return ::CreatePinningHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateSizedRefHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        OBJECTHANDLE h = ::CreateSizedRefHandle(
            m_hHandleTableBucket->pTable[GCHeap::IsServerHeap() ? (m_dwSizedRefHandles % m_iNumberOfProcessors) : GetCurrentThreadHomeHeapNumber()], 
            object);
        InterlockedIncrement((LONG*)&m_dwSizedRefHandles);
        return h;
    }

#ifdef FEATURE_COMINTEROP
    OBJECTHANDLE CreateRefcountedHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateRefcountedHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object);
    }

    OBJECTHANDLE CreateWinRTWeakHandle(OBJECTREF object, IWeakReference* pWinRTWeakReference)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        return ::CreateWinRTWeakHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object, pWinRTWeakReference);
    }
#endif // FEATURE_COMINTEROP

    OBJECTHANDLE CreateVariableHandle(OBJECTREF object, UINT type)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateVariableHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], object, type);
    }

    OBJECTHANDLE CreateDependentHandle(OBJECTREF primary, OBJECTREF secondary)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateDependentHandle(m_hHandleTableBucket->pTable[GetCurrentThreadHomeHeapNumber()], primary, secondary);
    }
#endif // DACCESS_COMPILE && !CROSSGEN_COMPILE

    BOOL ContainsOBJECTHANDLE(OBJECTHANDLE handle);

#ifdef FEATURE_FUSION
    IApplicationContext *GetFusionContext() {LIMITED_METHOD_CONTRACT;  return m_pFusionContext; }
#else
    IUnknown *GetFusionContext() {LIMITED_METHOD_CONTRACT;  return m_pFusionContext; }
    
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)    
    CLRPrivBinderCoreCLR *GetTPABinderContext() {LIMITED_METHOD_CONTRACT;  return m_pTPABinderContext; }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

#endif

    CrstExplicitInit * GetLoaderAllocatorReferencesLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_crstLoaderAllocatorReferences;
    }
    
protected:
    
    //****************************************************************************************
    // Helper method to initialize the large heap handle table.
    void InitLargeHeapHandleTable();

    //****************************************************************************************
    // Adds an assembly to the domain.
    void AddAssemblyNoLock(Assembly* assem);

    //****************************************************************************************
    //
    // Hash table that maps a MethodTable to COM Interop compatibility data.
    PtrHashMap          m_interopDataHash;

    // Critical sections & locks
    PEFileListLock   m_FileLoadLock;            // Protects the list of assemblies in the domain
    CrstExplicitInit m_DomainCrst;              // General Protection for the Domain
    CrstExplicitInit m_DomainCacheCrst;         // Protects the Assembly and Unmanaged caches
    CrstExplicitInit m_DomainLocalBlockCrst;
    CrstExplicitInit m_InteropDataCrst;         // Used for COM Interop compatiblilty
    // Used to protect the reference lists in the collectible loader allocators attached to this appdomain
    CrstExplicitInit m_crstLoaderAllocatorReferences;
    CrstExplicitInit m_WinRTFactoryCacheCrst;   // For WinRT factory cache
    
    //#AssemblyListLock
    // Used to protect the assembly list. Taken also by GC or debugger thread, therefore we have to avoid 
    // triggering GC while holding this lock (by switching the thread to GC_NOTRIGGER while it is held).
    CrstExplicitInit m_crstAssemblyList;
    BOOL             m_fDisableInterfaceCache;  // RCW COM interface cache
    ListLock         m_ClassInitLock;
    ListLock         m_JITLock;
    ListLock         m_ILStubGenLock;

    // Fusion context, used for adding assemblies to the is domain. It defines
    // fusion properties for finding assemblyies such as SharedBinPath,
    // PrivateBinPath, Application Directory, etc.
#ifdef FEATURE_FUSION    
    IApplicationContext* m_pFusionContext; // Binding context for the domain
#else
    IUnknown *m_pFusionContext; // Current binding context for the domain

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)    
    CLRPrivBinderCoreCLR *m_pTPABinderContext; // Reference to the binding context that holds TPA list details
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

#endif    

    HandleTableBucket *m_hHandleTableBucket;

    // The large heap handle table.
    LargeHeapHandleTable        *m_pLargeHeapHandleTable;

    // The large heap handle table critical section.
    CrstExplicitInit             m_LargeHeapHandleTableCrst;

    EEMarshalingData            *m_pMarshalingData;

#ifdef FEATURE_COMINTEROP
    // Information regarding the managed standard interfaces.
    MngStdInterfacesInfo        *m_pMngStdInterfacesInfo;
    
    // WinRT binder (only in classic = non-AppX; AppX has the WinRT binder inside code:CLRPrivBinderAppX)
    PTR_CLRPrivBinderWinRT m_pWinRtBinder;
#endif // FEATURE_COMINTEROP

    // Number of allocated slots for context local statics of this domain
    DWORD m_dwContextStatics;

    // Protects allocation of slot IDs for thread and context statics
    static CrstStatic   m_SpecialStaticsCrst;

public:
    // Lazily allocate offset for context static
    DWORD AllocateContextStaticsOffset(DWORD* pOffsetSlot);

public:
    // Only call this routine when you can guarantee there are no
    // loads in progress.
    void ClearFusionContext();

public:

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

    class CacheLockHolder : public CrstHolder
    {
    public:
        CacheLockHolder(BaseDomain *pD)
            : CrstHolder(&pD->m_DomainCacheCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
    friend class CacheLockHolder;

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
    class WinRTFactoryCacheLockHolder : public CrstHolder
    {
    public:
        WinRTFactoryCacheLockHolder(BaseDomain *pD)
            : CrstHolder(&pD->m_WinRTFactoryCacheCrst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
    friend class WinRTFactoryCacheLockHolder;

public:
    void InitVSD();
    RangeList *GetCollectibleVSDRanges() { return &m_collVSDRanges; }

private:
    TypeIDMap m_typeIDMap;
    // Range list for collectible types. Maps VSD PCODEs back to the VirtualCallStubManager they belong to
    LockedRangeList m_collVSDRanges;

public:
    UINT32 GetTypeID(PTR_MethodTable pMT);
    UINT32 LookupTypeID(PTR_MethodTable pMT);
    PTR_MethodTable LookupType(UINT32 id);

private:
    // I have yet to figure out an efficent way to get the number of handles 
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

    // Profiler rejit
private:
    ReJitManager m_reJitMgr;

public:
    ReJitManager * GetReJitManager() { return &m_reJitMgr; }

#ifdef DACCESS_COMPILE
public:
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif

};  // class BaseDomain

enum
{
    ATTACH_ASSEMBLY_LOAD = 0x1,
    ATTACH_MODULE_LOAD = 0x2,
    ATTACH_CLASS_LOAD = 0x4,

    ATTACH_ALL = 0x7
};

class ADUnloadSink
{
    
protected:
    ~ADUnloadSink();
    CLREvent m_UnloadCompleteEvent;
    HRESULT   m_UnloadResult;
    Volatile<LONG> m_cRef;
public:
    ADUnloadSink();
    void ReportUnloadResult (HRESULT hr, OBJECTREF* pException);
    void WaitUnloadCompletion();
    HRESULT GetUnloadResult() {LIMITED_METHOD_CONTRACT; return m_UnloadResult;};
    void Reset();
    ULONG AddRef();
    ULONG Release();
};


FORCEINLINE void ADUnloadSink__Release(ADUnloadSink* pADSink)
{
    WRAPPER_NO_CONTRACT;

    if (pADSink)
        pADSink->Release();
}

typedef Wrapper <ADUnloadSink*,DoNothing,ADUnloadSink__Release,NULL> ADUnloadSinkHolder;

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
///     kIncludeLoaded|kIncludeExecution|kIncludeIntrospection

enum AssemblyIterationFlags
{
    // load status flags
    kIncludeLoaded        = 0x00000001, // include assemblies that are already loaded
                                        // (m_level >= code:FILE_LOAD_DELIVER_EVENTS)
    kIncludeLoading       = 0x00000002, // include assemblies that are still in the process of loading
                                        // (all m_level values)
    kIncludeAvailableToProfilers
                          = 0x00000020, // include assemblies available to profilers
                                        // See comment at code:DomainFile::IsAvailableToProfilers

    // Execution / introspection flags
    kIncludeExecution     = 0x00000004, // include assemblies that are loaded for execution only
    kIncludeIntrospection = 0x00000008, // include assemblies that are loaded for introspection only
    
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
#ifdef FEATURE_LOADER_OPTIMIZATION
class SharedAssemblyLocator
{
public:
    enum
    {
        DOMAINASSEMBLY      = 1,
        PEASSEMBLY          = 2,
        PEASSEMBLYEXACT     = 3
    };
    DWORD GetType() {LIMITED_METHOD_CONTRACT; return m_type;};
#ifndef DACCESS_COMPILE
    DomainAssembly* GetDomainAssembly() {LIMITED_METHOD_CONTRACT; _ASSERTE(m_type==DOMAINASSEMBLY); return (DomainAssembly*)m_value;};
    PEAssembly* GetPEAssembly() {LIMITED_METHOD_CONTRACT; _ASSERTE(m_type==PEASSEMBLY||m_type==PEASSEMBLYEXACT); return (PEAssembly*)m_value;};
    SharedAssemblyLocator(DomainAssembly* pAssembly)
    {
        LIMITED_METHOD_CONTRACT;
        m_type=DOMAINASSEMBLY;
        m_value=pAssembly;
    }
    SharedAssemblyLocator(PEAssembly* pFile, DWORD type = PEASSEMBLY)
    {
        LIMITED_METHOD_CONTRACT;
        m_type = type;
        m_value = pFile;
    }
#endif // DACCESS_COMPILE

    DWORD Hash();
protected:
    DWORD m_type;
    LPVOID m_value;
#if FEATURE_VERSIONING    
    ULONG   m_uIdentityHash;
#endif
};
#endif // FEATURE_LOADER_OPTIMIZATION

//
// Stores binding information about failed assembly loads for DAC
//
struct FailedAssembly {
    SString displayName;
    SString location;
#ifdef FEATURE_FUSION    
    LOADCTX_TYPE context;
#endif
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
        location.Set(pSpec->GetCodeBase());
        error = ex->GetHR();

        // 
        // Determine the binding context assembly would have been in.
        // If the parent has been set, use its binding context.
        // If the parent hasn't been set but the code base has, use LoadFrom.
        // Otherwise, use the default.
        //
#ifdef FEATURE_FUSION        
        context = pSpec->GetParentIAssembly() ? pSpec->GetParentIAssembly()->GetFusionLoadContext() : LOADCTX_TYPE_LOADFROM;
#endif // FEATURE_FUSION
    }
};

#ifdef FEATURE_COMINTEROP

// Cache used by COM Interop
struct NameToTypeMapEntry
{
    // Host space representation of the key
    struct Key
    {
        LPCWSTR m_wzName;     // The type name or registry string representation of the GUID "{<guid>}"
        SIZE_T  m_cchName;    // wcslen(m_wzName) for faster hashtable lookup
    };
    struct DacKey
    {
        PTR_CWSTR m_wzName;   // The type name or registry string representation of the GUID "{<guid>}"
        SIZE_T    m_cchName;  // wcslen(m_wzName) for faster hashtable lookup
    } m_key;
    TypeHandle m_typeHandle;  // Using TypeHandle instead of MethodTable* to avoid losing information when sharing method tables.
    UINT m_nEpoch;            // tracks creation Epoch. This is incremented each time an external reader enumerate the cache
    BYTE m_bFlags;
};

typedef DPTR(NameToTypeMapEntry) PTR_NameToTypeMapEntry;

class NameToTypeMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits<NameToTypeMapEntry> >
{
public:
    typedef NameToTypeMapEntry::Key key_t;

    static const NameToTypeMapEntry Null() { NameToTypeMapEntry e; e.m_key.m_wzName = NULL; e.m_key.m_cchName = 0; return e; }
    static bool IsNull(const NameToTypeMapEntry &e) { return e.m_key.m_wzName == NULL; }
    static const key_t GetKey(const NameToTypeMapEntry &e) 
    {
        key_t key;
        key.m_wzName = (LPCWSTR)(e.m_key.m_wzName); // this cast brings the string over to the host, in a DAC build
        key.m_cchName = e.m_key.m_cchName;

        return key;
    }
    static count_t Hash(const key_t &key) { WRAPPER_NO_CONTRACT; return HashStringN(key.m_wzName, key.m_cchName); }
    
    static BOOL Equals(const key_t &lhs, const key_t &rhs)
    {
        WRAPPER_NO_CONTRACT;
        return (lhs.m_cchName == rhs.m_cchName) && memcmp(lhs.m_wzName, rhs.m_wzName, lhs.m_cchName * sizeof(WCHAR)) == 0;
    }
    
    void OnDestructPerEntryCleanupAction(const NameToTypeMapEntry& e)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(e.m_key.m_cchName == wcslen(e.m_key.m_wzName));
#ifndef DACCESS_COMPILE
        delete [] e.m_key.m_wzName;
#endif // DACCESS_COMPILE
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

typedef SHash<NameToTypeMapTraits> NameToTypeMapTable;

typedef DPTR(NameToTypeMapTable) PTR_NameToTypeMapTable;

struct WinRTFactoryCacheEntry
{
    typedef MethodTable *Key;           
    Key          key;                   // Type as KEY
    
    CtxEntry    *m_pCtxEntry;           // Context entry - used to verify whether the cache is a match
    OBJECTHANDLE m_ohFactoryObject;     // Handle to factory object
};

class WinRTFactoryCacheTraits : public DefaultSHashTraits<WinRTFactoryCacheEntry>
{
public:
    typedef WinRTFactoryCacheEntry::Key key_t;
    static const WinRTFactoryCacheEntry Null() { WinRTFactoryCacheEntry e; e.key = NULL; return e; }
    static bool IsNull(const WinRTFactoryCacheEntry &e) { return e.key == NULL; }
    static const WinRTFactoryCacheEntry::Key GetKey(const WinRTFactoryCacheEntry& e) { return e.key; }
    static count_t Hash(WinRTFactoryCacheEntry::Key key) { return (count_t)((size_t)key); }
    static BOOL Equals(WinRTFactoryCacheEntry::Key lhs, WinRTFactoryCacheEntry::Key rhs)
    { return lhs == rhs; }
    static const WinRTFactoryCacheEntry Deleted() { WinRTFactoryCacheEntry e; e.key = (MethodTable *)-1; return e; }
    static bool IsDeleted(const WinRTFactoryCacheEntry &e) { return e.key == (MethodTable *)-1; }

    static void OnDestructPerEntryCleanupAction(const WinRTFactoryCacheEntry& e);
    static const bool s_DestructPerEntryCleanupAction = true;
};

typedef SHash<WinRTFactoryCacheTraits> WinRTFactoryCache;

#endif // FEATURE_COMINTEROP

class AppDomainIterator;

const DWORD DefaultADID = 1;

template <class AppDomainType> class AppDomainCreationHolder;

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
    friend class ADUnloadSink;
    friend class SystemDomain;
    friend class AssemblySink;
    friend class AppDomainNative;
    friend class AssemblyNative;
    friend class AssemblySpec;
    friend class ClassLoader;
    friend class ThreadNative;
    friend class RCWCache;
    friend class ClrDataAccess;
    friend class CheckAsmOffsets;
    friend class AppDomainFromIDHolder;

    VPTR_VTABLE_CLASS(AppDomain, BaseDomain)

public:
#ifndef DACCESS_COMPILE
    AppDomain();
    virtual ~AppDomain();
#endif
    static void DoADUnloadWork();
    DomainAssembly* FindDomainAssembly(Assembly*);
    void EnterContext(Thread* pThread, Context* pCtx,ContextTransitionFrame *pFrame);

#ifndef DACCESS_COMPILE
    //-----------------------------------------------------------------------------------------------------------------
    // Convenience wrapper for ::GetAppDomain to provide better encapsulation.
    static AppDomain * GetCurrentDomain()
    { return ::GetAppDomain(); }
#endif //!DACCESS_COMPILE
    
    //-----------------------------------------------------------------------------------------------------------------
    // Initializes an AppDomain. (this functions is not called from the SystemDomain)
    void Init();

    // creates only unamaged part
    static void CreateUnmanagedObject(AppDomainCreationHolder<AppDomain>& result);
    inline void SetAppDomainManagerInfo(LPCWSTR szAssemblyName, LPCWSTR szTypeName, EInitializeNewDomainFlags dwInitializeDomainFlags);
    inline BOOL HasAppDomainManagerInfo();
    inline LPCWSTR GetAppDomainManagerAsm();
    inline LPCWSTR GetAppDomainManagerType();
    inline EInitializeNewDomainFlags GetAppDomainManagerInitializeNewDomainFlags();

#ifndef FEATURE_CORECLR
    inline BOOL AppDomainManagerSetFromConfig();
    Assembly *GetAppDomainManagerEntryAssembly();
    void ComputeTargetFrameworkName();
#endif // FEATURE_CORECLR

#if defined(FEATURE_CORECLR) && defined(FEATURE_COMINTEROP)
    HRESULT SetWinrtApplicationContext(SString &appLocalWinMD);
#endif // FEATURE_CORECLR && FEATURE_COMINTEROP

    BOOL CanReversePInvokeEnter();
    void SetReversePInvokeCannotEnter();
    bool MustForceTrivialWaitOperations();
    void SetForceTrivialWaitOperations();

    //****************************************************************************************
    //
    // Stop deletes all the assemblies but does not remove other resources like
    // the critical sections
    void Stop();

    // Gets rid of resources
    void Terminate();

#ifdef  FEATURE_PREJIT
    //assembly cleanup that requires suspended runtime
    void DeleteNativeCodeRanges();
#endif

    // final assembly cleanup
    void ShutdownAssemblies();
    void ShutdownFreeLoaderAllocators(BOOL bFromManagedCode);
    
    void ReleaseDomainBoundInfo();
    void ReleaseFiles();
    

    // Remove the Appdomain for the system and cleans up. This call should not be
    // called from shut down code.
    void CloseDomain();

    virtual BOOL IsAppDomain() { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
    virtual PTR_AppDomain AsAppDomain() { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_AppDomain>(this); }

#ifndef FEATURE_CORECLR
    void InitializeSorting(OBJECTREF* ppAppdomainSetup);
    void InitializeHashing(OBJECTREF* ppAppdomainSetup);
#endif

    OBJECTREF DoSetup(OBJECTREF* setupInfo);

    OBJECTREF GetExposedObject();
    OBJECTREF GetRawExposedObject() {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        if (m_ExposedObject) {
            return ObjectFromHandle(m_ExposedObject);
        }
        else {
            return NULL;
        }
    }

    OBJECTHANDLE GetRawExposedObjectHandleForDebugger() { LIMITED_METHOD_DAC_CONTRACT; return m_ExposedObject; }

#ifdef FEATURE_COMINTEROP
    HRESULT GetComIPForExposedObject(IUnknown **pComIP);

    MethodTable *GetRedirectedType(WinMDAdapter::RedirectedTypeIndex index);
#endif // FEATURE_COMINTEROP


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
#ifndef DACCESS_COMPILE
    private:
        // Can be called only from AppDomain shutdown code:AppDomain::ShutdownAssemblies.
        // Note: Does not lock the assembly list and does not AddRefs collectible assemblies.
        BOOL Next_UnsafeNoAddRef(DomainAssembly ** ppDomainAssembly);
#endif

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

#ifdef FEATURE_CORECLR
private:
    struct NativeImageDependenciesEntry
    {
        BaseAssemblySpec m_AssemblySpec;
        GUID m_guidMVID;
    };

    class NativeImageDependenciesTraits : public NoRemoveSHashTraits<DefaultSHashTraits<NativeImageDependenciesEntry *> >
    {
    public:
        typedef BaseAssemblySpec *key_t;
        static key_t GetKey(NativeImageDependenciesEntry * e) { return &(e->m_AssemblySpec); }

        static count_t Hash(key_t k)
        {
            return k->Hash();
        }

        static BOOL Equals(key_t lhs, key_t rhs)
        {
            return lhs->CompareEx(rhs);
        }
    };

    SHash<NativeImageDependenciesTraits> m_NativeImageDependencies;

public:
    void CheckForMismatchedNativeImages(AssemblySpec * pSpec, const GUID * pGuid);

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
    BOOL BindingByManifestFile();

    PathIterator IterateNativeDllSearchDirectories();
    void SetNativeDllSearchDirectories(LPCWSTR paths);
    BOOL HasNativeDllSearchDirectories();
    void ShutdownNativeDllSearchDirectories();
#endif // FEATURE_CORECLR

public:
    SIZE_T GetAssemblyCount()
    {
        WRAPPER_NO_CONTRACT;
        return m_Assemblies.GetCount(this);
    }

    CHECK CheckCanLoadTypes(Assembly *pAssembly);
    CHECK CheckCanExecuteManagedCode(MethodDesc* pMD);
    CHECK CheckLoading(DomainFile *pFile, FileLoadLevel level);

    FileLoadLevel GetDomainFileLoadLevel(DomainFile *pFile);
    BOOL IsLoading(DomainFile *pFile, FileLoadLevel level);
    static FileLoadLevel GetThreadFileLoadLevel();

    void LoadDomainFile(DomainFile *pFile,
                        FileLoadLevel targetLevel);

    enum FindAssemblyOptions
    {
        FindAssemblyOptions_None                    = 0x0,
        FindAssemblyOptions_IncludeFailedToLoad     = 0x1
    };

    DomainAssembly * FindAssembly(PEAssembly * pFile, FindAssemblyOptions options = FindAssemblyOptions_None) DAC_EMPTY_RET(NULL);

#ifdef FEATURE_MIXEDMODE
    // Finds only loaded modules, elevates level if needed
    Module* GetIJWModule(HMODULE hMod) DAC_EMPTY_RET(NULL);
    // Finds loading modules
    DomainFile* FindIJWDomainFile(HMODULE hMod, const SString &path) DAC_EMPTY_RET(NULL);
#endif //  FEATURE_MIXEDMODE

    Assembly *LoadAssembly(AssemblySpec* pIdentity,
                           PEAssembly *pFile,
                           FileLoadLevel targetLevel,
                           AssemblyLoadSecurity *pLoadSecurity = NULL);

    // this function does not provide caching, you must use LoadDomainAssembly
    // unless the call is guaranteed to succeed or you don't need the caching 
    // (e.g. if you will FailFast or tear down the AppDomain anyway)
    // The main point that you should not bypass caching if you might try to load the same file again, 
    // resulting in multiple DomainAssembly objects that share the same PEAssembly for ngen image 
    //which is violating our internal assumptions
    DomainAssembly *LoadDomainAssemblyInternal( AssemblySpec* pIdentity,
                                                PEAssembly *pFile,
                                                FileLoadLevel targetLevel,
                                                AssemblyLoadSecurity *pLoadSecurity = NULL);

    DomainAssembly *LoadDomainAssembly( AssemblySpec* pIdentity,
                                        PEAssembly *pFile,
                                        FileLoadLevel targetLevel,
                                        AssemblyLoadSecurity *pLoadSecurity = NULL);

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    DomainModule *LoadDomainModule(DomainAssembly *pAssembly,
                                   PEModule *pFile,
                                   FileLoadLevel targetLevel);
#endif 

    CHECK CheckValidModule(Module *pModule);
#ifdef FEATURE_LOADER_OPTIMIZATION    
    DomainFile *LoadDomainNeutralModuleDependency(Module *pModule, FileLoadLevel targetLevel);
#endif

#ifdef FEATURE_FUSION
    PEAssembly *BindExplicitAssembly(HMODULE hMod, BOOL bindable);
    Assembly *LoadExplicitAssembly(HMODULE hMod, BOOL bindable);
    void GetFileFromFusion(IAssembly *pIAssembly, LPCWSTR wszModuleName,
                           SString &path);
#endif
    // private:
    void LoadSystemAssemblies();

    DomainFile *LoadDomainFile(FileLoadLock *pLock,
                               FileLoadLevel targetLevel);

    void TryIncrementalLoad(DomainFile *pFile, FileLoadLevel workLevel, FileLoadLockHolder &lockHolder);

    Assembly *LoadAssemblyHelper(LPCWSTR wszAssembly,
                                 LPCWSTR wszCodeBase);

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
    void CacheStringsForDAC();

    BOOL AddFileToCache(AssemblySpec* pSpec, PEAssembly *pFile, BOOL fAllowFailure = FALSE);
    BOOL AddAssemblyToCache(AssemblySpec* pSpec, DomainAssembly *pAssembly);
    BOOL AddExceptionToCache(AssemblySpec* pSpec, Exception *ex);
    void AddUnmanagedImageToCache(LPCWSTR libraryName, HMODULE hMod);
    HMODULE FindUnmanagedImageInCache(LPCWSTR libraryName);
    //****************************************************************************************
    //
    // Adds an assembly to the domain.
    void AddAssembly(DomainAssembly * assem);
    void RemoveAssembly_Unlocked(DomainAssembly * pAsm);

    BOOL ContainsAssembly(Assembly * assem);

#ifdef FEATURE_LOADER_OPTIMIZATION    
    enum SharePolicy
    {
        // Attributes to control when to use domain neutral assemblies
        SHARE_POLICY_UNSPECIFIED,   // Use the current default policy (LoaderOptimization.NotSpecified)
        SHARE_POLICY_NEVER,         // Do not share anything, except the system assembly (LoaderOptimization.SingleDomain)
        SHARE_POLICY_ALWAYS,        // Share everything possible (LoaderOptimization.MultiDomain)
        SHARE_POLICY_GAC,           // Share only GAC-bound assemblies (LoaderOptimization.MultiDomainHost)

        SHARE_POLICY_COUNT,
        SHARE_POLICY_MASK = 0x3,

        // NOTE that previously defined was a bit 0x40 which might be set on this value
        // in custom attributes.
        SHARE_POLICY_DEFAULT = SHARE_POLICY_NEVER,
    };

    void SetSharePolicy(SharePolicy policy);
    SharePolicy GetSharePolicy();
    BOOL ReduceSharePolicyFromAlways();

    //****************************************************************************************
    // Determines if the image is to be loaded into the shared assembly or an individual
    // appdomains.
#ifndef FEATURE_CORECLR    
    BOOL ApplySharePolicy(DomainAssembly *pFile);
    BOOL ApplySharePolicyFlag(DomainAssembly *pFile);
#endif    
#endif // FEATURE_LOADER_OPTIMIZATION

    BOOL HasSetSecurityPolicy();

    FORCEINLINE IApplicationSecurityDescriptor* GetSecurityDescriptor()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return static_cast<IApplicationSecurityDescriptor*>(m_pSecDesc);
    }

    void CreateSecurityDescriptor();

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
    void ResetFriendlyName(BOOL fDebuggerCares = TRUE);

    //****************************************************************************************

    // This can be used to override the binding behavior of the appdomain.   It
    // is overridden in the compilation domain.  It is important that all
    // static binding goes through this path.
    virtual PEAssembly * BindAssemblySpec(
        AssemblySpec *pSpec,
        BOOL fThrowOnFileNotFound,
        BOOL fRaisePrebindEvents,
        StackCrawlMark *pCallerStackMark = NULL,
        AssemblyLoadSecurity *pLoadSecurity = NULL,
        BOOL fUseHostBinderIfAvailable = TRUE) DAC_EMPTY_RET(NULL);

    HRESULT BindAssemblySpecForHostedBinder(
        AssemblySpec *   pSpec, 
        IAssemblyName *  pAssemblyName, 
        ICLRPrivBinder * pBinder, 
        PEAssembly **    ppAssembly) DAC_EMPTY_RET(E_FAIL);
    
    HRESULT BindHostedPrivAssembly(
        PEAssembly *       pParentPEAssembly,
        ICLRPrivAssembly * pPrivAssembly, 
        IAssemblyName *    pAssemblyName, 
        PEAssembly **      ppAssembly, 
        BOOL               fIsIntrospectionOnly = FALSE) DAC_EMPTY_RET(S_OK);

#ifdef FEATURE_REFLECTION_ONLY_LOAD    
    virtual DomainAssembly *BindAssemblySpecForIntrospectionDependencies(AssemblySpec *pSpec) DAC_EMPTY_RET(NULL);
#endif

    PEAssembly *TryResolveAssembly(AssemblySpec *pSpec, BOOL fPreBind);

    // Store a successful binding into the cache.  This will keep the file from
    // being physically unmapped, as well as shortcutting future attempts to bind
    // the same spec throught the Cached entry point.
    //
    // Right now we only cache assembly binds for "probing" type
    // binding situations, basically when loading domain neutral assemblies or
    // zap files.
    //
    // <TODO>@todo: We may want to be more aggressive about this if
    // there are other situations where we are repeatedly binding the
    // same assembly specs, though.</TODO>
    //
    // Returns TRUE if stored
    //         FALSE if it's a duplicate (caller should clean up args)
    BOOL StoreBindAssemblySpecResult(AssemblySpec *pSpec,
                                     PEAssembly *pFile,
                                     BOOL clone = TRUE);

    BOOL StoreBindAssemblySpecError(AssemblySpec *pSpec,
                                    HRESULT hr,
                                    OBJECTREF *pThrowable,
                                    BOOL clone = TRUE);

    //****************************************************************************************
    //
#ifdef FEATURE_FUSION    
    static BOOL SetContextProperty(IApplicationContext* pFusionContext,
                                   LPCWSTR pProperty,
                                   OBJECTREF* obj);
#endif
    //****************************************************************************************
    //
    // Uses the first assembly to add an application base to the Context. This is done
    // in a lazy fashion so executables do not take the perf hit unless the load other
    // assemblies
#ifdef FEATURE_FUSION    
    LPWSTR GetDynamicDir();
#endif
#ifndef DACCESS_COMPILE
    void OnAssemblyLoad(Assembly *assem);
    void OnAssemblyLoadUnlocked(Assembly *assem);
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

    void SetSystemAssemblyLoadEventSent (BOOL fFlag);
    BOOL WasSystemAssemblyLoadEventSent (void);

#ifndef DACCESS_COMPILE
    OBJECTREF* AllocateStaticFieldObjRefPtrs(int nRequested, OBJECTREF** ppLazyAllocate = NULL)
    {
        WRAPPER_NO_CONTRACT;

        return AllocateObjRefPtrsInLargeTable(nRequested, ppLazyAllocate);
    }

    OBJECTREF* AllocateStaticFieldObjRefPtrsCrossDomain(int nRequested, OBJECTREF** ppLazyAllocate = NULL)
    {
        WRAPPER_NO_CONTRACT;

        return AllocateObjRefPtrsInLargeTable(nRequested, ppLazyAllocate, TRUE);
    }
#endif // DACCESS_COMPILE

    void              EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

    DomainLocalBlock *GetDomainLocalBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return &m_sDomainLocalBlock;
    }

    static SIZE_T GetOffsetOfModuleSlotsPointer()
    {
        WRAPPER_NO_CONTRACT;

        return offsetof(AppDomain,m_sDomainLocalBlock) + DomainLocalBlock::GetOffsetOfModuleSlotsPointer();
    }

    void SetupSharedStatics();

    ADUnloadSink* PrepareForWaitUnloadCompletion();

    //****************************************************************************************
    //
    // Create a quick lookup for classes loaded into this domain based on their GUID.
    //
    void InsertClassForCLSID(MethodTable* pMT, BOOL fForceInsert = FALSE);
    void InsertClassForCLSID(MethodTable* pMT, GUID *pGuid);

#ifdef FEATURE_COMINTEROP
private:
    void CacheTypeByNameWorker(const SString &ssClassName, const UINT vCacheVersion, TypeHandle typeHandle, BYTE flags, BOOL bReplaceExisting = FALSE);
    TypeHandle LookupTypeByNameWorker(const SString &ssClassName, UINT *pvCacheVersion, BYTE *pbFlags);
public:
    // Used by COM Interop for mapping WinRT runtime class names to real types.
    void CacheTypeByName(const SString &ssClassName, const UINT vCacheVersion, TypeHandle typeHandle, BYTE flags, BOOL bReplaceExisting = FALSE);
    TypeHandle LookupTypeByName(const SString &ssClassName, UINT *pvCacheVersion, BYTE *pbFlags);
    PTR_MethodTable LookupTypeByGuid(const GUID & guid);

#ifndef DACCESS_COMPILE
    inline BOOL CanCacheWinRTTypeByGuid(TypeHandle typeHandle)
    { 
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        // Only allow caching guid/types maps for types loaded during 
        // "normal" domain operation
        if (IsCompilationDomain() || (m_Stage < STAGE_OPEN))
            return FALSE;

        MethodTable *pMT = typeHandle.GetMethodTable();
        if (pMT != NULL)
        {
            // Don't cache mscorlib-internal declarations of WinRT types.
            if (pMT->GetModule()->IsSystem() && pMT->IsProjectedFromWinRT())
                return FALSE;

            // Don't cache redirected WinRT types.
            if (WinRTTypeNameConverter::IsRedirectedWinRTSourceType(pMT))
                return FALSE;
        }

        return TRUE;
    }
#endif // !DACCESS_COMPILE

    void CacheWinRTTypeByGuid(TypeHandle typeHandle);
    void GetCachedWinRTTypes(SArray<PTR_MethodTable> * pTypes, SArray<GUID> * pGuids, UINT minEpoch, UINT * pCurEpoch);

    // Used by COM Interop for caching WinRT factory objects.
    void CacheWinRTFactoryObject(MethodTable *pClassMT, OBJECTREF *refFactory, LPVOID lpCtxCookie);
    OBJECTREF LookupWinRTFactoryObject(MethodTable *pClassMT, LPVOID lpCtxCookie);
    void RemoveWinRTFactoryObjects(LPVOID pCtxCookie);

    MethodTable *LoadCOMClass(GUID clsid, BOOL bLoadRecord = FALSE, BOOL* pfAssemblyInReg = NULL);
    COMorRemotingFlag GetComOrRemotingFlag();
    BOOL GetPreferComInsteadOfManagedRemoting();
    OBJECTREF GetMissingObject();    // DispatchInfo will call function to retrieve the Missing.Value object.
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
    MethodTable* LookupClass(REFIID iid)
    {
        WRAPPER_NO_CONTRACT;

        MethodTable *pMT = (MethodTable*) m_clsidHash.LookupValue((UPTR) GetKeyFromGUID(&iid), (LPVOID)&iid);
        return (pMT == (MethodTable*) INVALIDENTRY
            ? NULL
            : pMT);
    }
#endif // DACCESS_COMPILE

    //<TODO>@todo get a better key</TODO>
    ULONG GetKeyFromGUID(const GUID *pguid)
    {
        LIMITED_METHOD_CONTRACT;

        return *(ULONG *) pguid;
    }

#ifdef FEATURE_COMINTEROP
    ComCallWrapperCache* GetComCallWrapperCache();
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

    RCWRefCache *GetRCWRefCache();

    void ResetComCallWrapperCache()
    {
        LIMITED_METHOD_CONTRACT;
        m_pComCallWrapperCache = NULL;
    }

    MethodTable* GetLicenseInteropHelperMethodTable();
#endif // FEATURE_COMINTEROP

    //****************************************************************************************
    // Get the proxy for this app domain
#ifdef FEATURE_REMOTING    
    OBJECTREF GetAppDomainProxy();
#endif

    ADIndex GetIndex()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_dwIndex;
    }

    TPIndex GetTPIndex()
    {
        LIMITED_METHOD_CONTRACT;
        return m_tpIndex;
    }

    void InitializeDomainContext(BOOL allowRedirects, LPCWSTR pwszPath, LPCWSTR pwszConfig);

#ifdef FEATURE_FUSION
    IApplicationContext *CreateFusionContext();
    void SetupLoaderOptimization(DWORD optimization);
#endif
#ifdef FEATURE_VERSIONING
    IUnknown *CreateFusionContext();
#endif // FEATURE_VERSIONING

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    void OverrideDefaultContextBinder(IUnknown *pOverrideBinder)
    {
        LIMITED_METHOD_CONTRACT;
        
        _ASSERTE(pOverrideBinder != NULL);
        pOverrideBinder->AddRef();
        m_pFusionContext->Release();
        m_pFusionContext = pOverrideBinder;
    }
    
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

#ifdef FEATURE_PREJIT
    CorCompileConfigFlags GetNativeConfigFlags();
#endif // FEATURE_PREJIT

    //****************************************************************************************
    // Create a domain context rooted at the fileName. The directory containing the file name
    // is the application base and the configuration file is the fileName appended with
    // .config. If no name is passed in then no domain is created.
    static AppDomain* CreateDomainContext(LPCWSTR fileName);

    // Sets up the current domain's fusion context based on the given exe file name
    // (app base & config file)
    void SetupExecutableFusionContext(LPCWSTR exePath);

    //****************************************************************************************
    // Manage a pool of asyncrhonous objects used to fetch assemblies.  When a sink is released
    // it places itself back on the pool list.  Only one object is kept in the pool.
#ifdef FEATURE_FUSION
    AssemblySink* AllocateAssemblySink(AssemblySpec* pSpec);
#endif
    void SetIsUserCreatedDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= USER_CREATED_DOMAIN;
    }

    BOOL IsUserCreatedDomain()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & USER_CREATED_DOMAIN);
    }

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

    void SetPassiveDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= PASSIVE_DOMAIN;
    }

    BOOL IsPassiveDomain()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & PASSIVE_DOMAIN);
    }

    void SetVerificationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= VERIFICATION_DOMAIN;
    }

    BOOL IsVerificationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & VERIFICATION_DOMAIN);
    }

    void SetIllegalVerificationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= ILLEGAL_VERIFICATION_DOMAIN;
    }

    BOOL IsIllegalVerificationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_dwFlags & ILLEGAL_VERIFICATION_DOMAIN);
    }

    void SetCompilationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= (PASSIVE_DOMAIN|COMPILATION_DOMAIN);
    }

    BOOL IsCompilationDomain();

    PTR_CompilationDomain ToCompilationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsCompilationDomain());
        return dac_cast<PTR_CompilationDomain>(this);
    }

    void SetCanUnload()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= APP_DOMAIN_CAN_BE_UNLOADED;
    }

    BOOL CanUnload()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return m_dwFlags & APP_DOMAIN_CAN_BE_UNLOADED;
    }

    void SetRemotingConfigured()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        FastInterlockOr((ULONG*)&m_dwFlags, REMOTING_CONFIGURED_FOR_DOMAIN);
    }

    BOOL IsRemotingConfigured()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return m_dwFlags & REMOTING_CONFIGURED_FOR_DOMAIN;
    }

    void SetOrphanedLocks()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        FastInterlockOr((ULONG*)&m_dwFlags, ORPHANED_LOCKS);
    }

    BOOL HasOrphanedLocks()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return m_dwFlags & ORPHANED_LOCKS;
    }

    // This function is used to relax asserts in the lock accounting.
    // It returns true if we are fine with hosed lock accounting in this domain.
    BOOL OkToIgnoreOrphanedLocks()
    {
        WRAPPER_NO_CONTRACT;
        return HasOrphanedLocks() && m_Stage >= STAGE_UNLOAD_REQUESTED;
    }

    static void ExceptionUnwind(Frame *pFrame);

#ifdef _DEBUG
    void TrackADThreadEnter(Thread *pThread, Frame *pFrame);
    void TrackADThreadExit(Thread *pThread, Frame *pFrame);
    void DumpADThreadTrack();
#endif

#ifndef DACCESS_COMPILE
    void ThreadEnter(Thread *pThread, Frame *pFrame)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
        if (LoggingOn(LF_APPDOMAIN, LL_INFO100))
            TrackADThreadEnter(pThread, pFrame);
        else
#endif
        {
            InterlockedIncrement((LONG*)&m_dwThreadEnterCount);
            LOG((LF_APPDOMAIN, LL_INFO1000, "AppDomain::ThreadEnter  %p to [%d] (%8.8x) %S count %d\n", 
                 pThread,GetId().m_dwId, this,
                 GetFriendlyNameForLogging(),GetThreadEnterCount()));
#if _DEBUG_AD_UNLOAD
            printf("AppDomain::ThreadEnter %p to [%d] (%8.8x) %S count %d\n",
                   pThread, GetId().m_dwId, this,
                   GetFriendlyNameForLogging(), GetThreadEnterCount());
#endif
        }
    }

    void ThreadExit(Thread *pThread, Frame *pFrame)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
        if (LoggingOn(LF_APPDOMAIN, LL_INFO100)) {
            TrackADThreadExit(pThread, pFrame);
        }
        else
#endif
        {
            LONG result;
            result = InterlockedDecrement((LONG*)&m_dwThreadEnterCount);
            _ASSERTE(result >= 0);
            LOG((LF_APPDOMAIN, LL_INFO1000, "AppDomain::ThreadExit from [%d] (%8.8x) %S count %d\n",
                 this, GetId().m_dwId,
                 GetFriendlyNameForLogging(), GetThreadEnterCount()));
#if _DEBUG_ADUNLOAD
            printf("AppDomain::ThreadExit %x from [%d] (%8.8x) %S count %d\n",
                   pThread->GetThreadId(), this, GetId().m_dwId,
                   GetFriendlyNameForLogging(), GetThreadEnterCount());
#endif
        }
    }
#endif // DACCESS_COMPILE

    ULONG GetThreadEnterCount()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwThreadEnterCount;
    }

    BOOL OnlyOneThreadLeft()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwThreadEnterCount==1 || m_dwThreadsStillInAppDomain ==1;
    }

    Context *GetDefaultContext()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDefaultContext;
    }

    BOOL CanLoadCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Stage >= STAGE_READYFORMANAGEDCODE && m_Stage < STAGE_CLOSED;        
    }

    void SetAnonymouslyHostedDynamicMethodsAssembly(DomainAssembly * pDomainAssembly)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pDomainAssembly != NULL);
        _ASSERTE(m_anonymouslyHostedDynamicMethodsAssembly == NULL);
        m_anonymouslyHostedDynamicMethodsAssembly = pDomainAssembly;
    }

    DomainAssembly * GetAnonymouslyHostedDynamicMethodsAssembly()
    {
        LIMITED_METHOD_CONTRACT;
        return m_anonymouslyHostedDynamicMethodsAssembly;
    }

    BOOL HasUnloadStarted()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Stage>=STAGE_EXITED;
    }
    static void RefTakerAcquire(AppDomain* pDomain)
    {
        WRAPPER_NO_CONTRACT;
        if(!pDomain)
            return;
        pDomain->AddRef();
#ifdef _DEBUG
        FastInterlockIncrement(&pDomain->m_dwRefTakers);
#endif
    }

    static void RefTakerRelease(AppDomain* pDomain)
    {
        WRAPPER_NO_CONTRACT;
        if(!pDomain)
            return;
#ifdef _DEBUG
        _ASSERTE(pDomain->m_dwRefTakers);
        FastInterlockDecrement(&pDomain->m_dwRefTakers);
#endif
        pDomain->Release();
    }

#ifdef _DEBUG 

    BOOL IsHeldByIterator()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwIterHolders>0;
    }

    BOOL IsHeldByRefTaker()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwRefTakers>0;
    }

    void IteratorRelease()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_dwIterHolders);
        FastInterlockDecrement(&m_dwIterHolders);
    }

      
    void IteratorAcquire()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockIncrement(&m_dwIterHolders);
    }
    
#endif    
    BOOL IsActive()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_Stage >= STAGE_ACTIVE && m_Stage < STAGE_CLOSED;
    }
    // Range for normal execution of code in the appdomain, currently used for
    // appdomain resource monitoring since we don't care to update resource usage
    // unless it's in these stages (as fields of AppDomain may not be valid if it's
    // not within these stages)
    BOOL IsUserActive()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_Stage >= STAGE_ACTIVE && m_Stage <= STAGE_OPEN;
    }
    BOOL IsValid()
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef DACCESS_COMPILE
        // We want to see all appdomains in SOS, even the about to be destructed ones.
        // There is no risk of races under DAC, so we will pretend to be unconditionally valid.
        return TRUE;
#else
        return m_Stage > STAGE_CREATING && m_Stage < STAGE_CLOSED;
#endif
    }

#ifdef _DEBUG
    BOOL IsBeingCreated()
    {
        LIMITED_METHOD_CONTRACT;

        return m_dwCreationHolders > 0;
    }

    void IncCreationCount()
    {
        LIMITED_METHOD_CONTRACT;

        FastInterlockIncrement(&m_dwCreationHolders);
        _ASSERTE(m_dwCreationHolders > 0);
    }

    void DecCreationCount()
    {
        LIMITED_METHOD_CONTRACT;

        FastInterlockDecrement(&m_dwCreationHolders);
        _ASSERTE(m_dwCreationHolders > -1);
    }
#endif
    BOOL IsRunningIn(Thread* pThread);

    BOOL IsUnloading()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_Stage > STAGE_UNLOAD_REQUESTED;
    }

    BOOL NotReadyForManagedCode()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Stage < STAGE_READYFORMANAGEDCODE;
    }

    void SetFinalized()
    {
        LIMITED_METHOD_CONTRACT;
        SetStage(STAGE_FINALIZED);
    }

    BOOL IsFinalizing()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Stage >= STAGE_FINALIZING;
    }

    BOOL IsFinalized()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Stage >= STAGE_FINALIZED;
    }

    BOOL NoAccessToHandleTable()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_Stage >= STAGE_HANDLETABLE_NOACCESS;
    }

    // Checks whether the given thread can enter the app domain
    BOOL CanThreadEnter(Thread *pThread);

    // Following two are needed for the Holder
    static void SetUnloadInProgress(AppDomain *pThis) PUB;
    static void SetUnloadComplete(AppDomain *pThis) PUB;
    // Predicates for GC asserts
    BOOL ShouldHaveFinalization()
    {
        LIMITED_METHOD_CONTRACT;

        return ((DWORD) m_Stage) < STAGE_COLLECTED;
    }
    BOOL ShouldHaveCode()
    {
        LIMITED_METHOD_CONTRACT;

        return ((DWORD) m_Stage) < STAGE_COLLECTED;
    }
    BOOL ShouldHaveRoots()
    {
        LIMITED_METHOD_CONTRACT;

        return ((DWORD) m_Stage) < STAGE_CLEARED;
    }
    BOOL ShouldHaveInstances()
    {
        LIMITED_METHOD_CONTRACT;

        return ((DWORD) m_Stage) < STAGE_COLLECTED;
    }


    static void RaiseExitProcessEvent();
    Assembly* RaiseResourceResolveEvent(DomainAssembly* pAssembly, LPCSTR szName);
    DomainAssembly* RaiseTypeResolveEventThrowing(DomainAssembly* pAssembly, LPCSTR szName, ASSEMBLYREF *pResultingAssemblyRef);
    Assembly* RaiseAssemblyResolveEvent(AssemblySpec *pSpec, BOOL fIntrospection, BOOL fPreBind);

private:
    CrstExplicitInit    m_ReflectionCrst;
    CrstExplicitInit    m_RefClassFactCrst;


    EEClassFactoryInfoHashTable *m_pRefClassFactHash;   // Hash table that maps a class factory info to a COM comp.
#ifdef FEATURE_COMINTEROP
    DispIDCache *m_pRefDispIDCache;
    COMorRemotingFlag m_COMorRemotingFlag;
    OBJECTHANDLE  m_hndMissing;     //Handle points to Missing.Value Object which is used for [Optional] arg scenario during IDispatch CCW Call

    MethodTable* m_rpCLRTypes[WinMDAdapter::RedirectedTypeIndex_Count];

    MethodTable* LoadRedirectedType(WinMDAdapter::RedirectedTypeIndex index, WinMDAdapter::FrameworkAssemblyIndex assembly);
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
    virtual PTR_LoaderAllocator GetLoaderAllocator();

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    #define ARM_ETW_ALLOC_THRESHOLD (4 * 1024 * 1024)
    // cache line size in ULONGLONG - 128 bytes which are 16 ULONGLONG's
    #define ARM_CACHE_LINE_SIZE_ULL 16

    inline ULONGLONG GetAllocBytes()
    {
        LIMITED_METHOD_CONTRACT;
        ULONGLONG ullTotalAllocBytes = 0;

        // Ensure that m_pullAllocBytes is non-null to avoid an AV in a race between GC and AD unload.
        // A race can occur when a new appdomain is created, but an OOM is thrown when allocating for m_pullAllocBytes, causing the AD unload.
        if(NULL != m_pullAllocBytes)
        {
            for (DWORD i = 0; i < m_dwNumHeaps; i++)
            {
                ullTotalAllocBytes += m_pullAllocBytes[i * ARM_CACHE_LINE_SIZE_ULL];
            }
        }
        return ullTotalAllocBytes;
    }

    void RecordAllocBytes(size_t allocatedBytes, DWORD dwHeapNumber)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(dwHeapNumber < m_dwNumHeaps);
        
        // Ensure that m_pullAllocBytes is non-null to avoid an AV in a race between GC and AD unload.
        // A race can occur when a new appdomain is created, but an OOM is thrown when allocating for m_pullAllocBytes, causing the AD unload.
        if(NULL != m_pullAllocBytes)
        {
            m_pullAllocBytes[dwHeapNumber * ARM_CACHE_LINE_SIZE_ULL] += allocatedBytes;
        }

        ULONGLONG ullTotalAllocBytes = GetAllocBytes();

        if ((ullTotalAllocBytes - m_ullLastEtwAllocBytes) >= ARM_ETW_ALLOC_THRESHOLD)
        {
            m_ullLastEtwAllocBytes = ullTotalAllocBytes;
            FireEtwAppDomainMemAllocated((ULONGLONG)this, ullTotalAllocBytes, GetClrInstanceId());
        }
    }

    inline ULONGLONG GetSurvivedBytes()
    {
        LIMITED_METHOD_CONTRACT;
        ULONGLONG ullTotalSurvivedBytes = 0;

        // Ensure that m_pullSurvivedBytes is non-null to avoid an AV in a race between GC and AD unload.
        // A race can occur when a new appdomain is created, but an OOM is thrown when allocating for m_pullSurvivedBytes, causing the AD unload.
        if(NULL != m_pullSurvivedBytes)
        {
            for (DWORD i = 0; i < m_dwNumHeaps; i++)
            {
                ullTotalSurvivedBytes += m_pullSurvivedBytes[i * ARM_CACHE_LINE_SIZE_ULL];
            }
        }
        return ullTotalSurvivedBytes;
    }

    void RecordSurvivedBytes(size_t promotedBytes, DWORD dwHeapNumber)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(dwHeapNumber < m_dwNumHeaps);
   
        // Ensure that m_pullSurvivedBytes is non-null to avoid an AV in a race between GC and AD unload.
        // A race can occur when a new appdomain is created, but an OOM is thrown when allocating for m_pullSurvivedBytes, causing the AD unload.
        if(NULL != m_pullSurvivedBytes)
        {
            m_pullSurvivedBytes[dwHeapNumber * ARM_CACHE_LINE_SIZE_ULL] += promotedBytes;
        }
    }

    inline void ResetSurvivedBytes()
    {
        LIMITED_METHOD_CONTRACT;
        
        // Ensure that m_pullSurvivedBytes is non-null to avoid an AV in a race between GC and AD unload.
        // A race can occur when a new appdomain is created, but an OOM is thrown when allocating for m_pullSurvivedBytes, causing the AD unload.
        if(NULL != m_pullSurvivedBytes)
        {
            for (DWORD i = 0; i < m_dwNumHeaps; i++)
            {
                m_pullSurvivedBytes[i * ARM_CACHE_LINE_SIZE_ULL] = 0;
            }
        }
    }

    // Return the total processor time (user and kernel) used by threads executing in this AppDomain so far.
    // The result is in 100ns units.
    ULONGLONG QueryProcessorUsage();

    // Add to the current count of processor time used by threads within this AppDomain. This API is called by
    // threads transitioning between AppDomains.
    void UpdateProcessorUsage(ULONGLONG ullAdditionalUsage);
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

private:
    static void RaiseOneExitProcessEvent_Wrapper(AppDomainIterator* pi);
    static void RaiseOneExitProcessEvent();
    size_t EstimateSize();
    EEClassFactoryInfoHashTable* SetupClassFactHash();
#ifdef FEATURE_COMINTEROP
    DispIDCache* SetupRefDispIDCache();
    COMorRemotingFlag GetPreferComInsteadOfManagedRemotingFromConfigFile();
#endif // FEATURE_COMINTEROP

    void InitializeDefaultDomainManager ();

#ifdef FEATURE_CLICKONCE
    void InitializeDefaultClickOnceDomain();
#endif // FEATURE_CLICKONCE

    void InitializeDefaultDomainSecurity();
public:
#ifdef FEATURE_CLICKONCE
    BOOL IsClickOnceAppDomain();
#endif // FEATURE_CLICKONCE

protected:
    BOOL PostBindResolveAssembly(AssemblySpec  *pPrePolicySpec,
                                 AssemblySpec  *pPostPolicySpec,
                                 HRESULT        hrBindResult,
                                 AssemblySpec **ppFailedSpec);

#ifdef FEATURE_COMINTEROP
public:
    void ReleaseRCWs(LPVOID pCtxCookie);
    void DetachRCWs();

protected:
#endif // FEATURE_COMINTEROP

    LPWSTR m_pwDynamicDir;

private:
    void RaiseLoadingAssemblyEvent(DomainAssembly* pAssembly);

    friend class DomainAssembly;

public:
    static void ProcessUnloadDomainEventOnFinalizeThread();
    static BOOL HasWorkForFinalizerThread()
    {
        LIMITED_METHOD_CONTRACT;
        return s_pAppDomainToRaiseUnloadEvent != NULL;
    }

private:
    static AppDomain* s_pAppDomainToRaiseUnloadEvent;
    static BOOL s_fProcessUnloadDomainEvent;

    void RaiseUnloadDomainEvent();
    static void RaiseUnloadDomainEvent_Wrapper(LPVOID /* AppDomain * */);

    BOOL RaiseUnhandledExceptionEvent(OBJECTREF *pSender, OBJECTREF *pThrowable, BOOL isTerminating);
    BOOL HasUnhandledExceptionEventHandler();
    BOOL RaiseUnhandledExceptionEventNoThrow(OBJECTREF *pSender, OBJECTREF *pThrowable, BOOL isTerminating);
    
    struct RaiseUnhandled_Args
    {
        AppDomain *pExceptionDomain;
        AppDomain *pTargetDomain;
        OBJECTREF *pSender;
        OBJECTREF *pThrowable;
        BOOL isTerminating;
        BOOL *pResult;
    };
    #ifndef FEATURE_CORECLR 
    static void RaiseUnhandledExceptionEvent_Wrapper(LPVOID /* RaiseUnhandled_Args * */);
    #endif


    static void AllowThreadEntrance(AppDomain *pApp);
    static void RestrictThreadEntrance(AppDomain *pApp);

    typedef Holder<AppDomain*,DoNothing<AppDomain*>,AppDomain::AllowThreadEntrance,NULL> RestrictEnterHolder;
    
    enum Stage {
        STAGE_CREATING,
        STAGE_READYFORMANAGEDCODE,
        STAGE_ACTIVE,
        STAGE_OPEN,
        STAGE_UNLOAD_REQUESTED,
        STAGE_EXITING,
        STAGE_EXITED,
        STAGE_FINALIZING,
        STAGE_FINALIZED,
        STAGE_HANDLETABLE_NOACCESS,
        STAGE_CLEARED,
        STAGE_COLLECTED,
        STAGE_CLOSED
    };
    void SetStage(Stage stage)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
        STRESS_LOG2(LF_APPDOMAIN, LL_INFO100,"Updating AD stage, ADID=%d, stage=%d\n",GetId().m_dwId,stage);
        TESTHOOKCALL(AppDomainStageChanged(GetId().m_dwId,m_Stage,stage));
        Stage lastStage=m_Stage;
        while (lastStage !=stage) 
            lastStage = (Stage)FastInterlockCompareExchange((LONG*)&m_Stage,stage,lastStage);
    };
    void Exit(BOOL fRunFinalizers, BOOL fAsyncExit);
    void Close();
    void ClearGCRoots();
    void ClearGCHandles();
    void HandleAsyncPinHandles();
    void UnwindThreads();
    // Return TRUE if EE is stopped
    // Return FALSE if more work is needed
    BOOL StopEEAndUnwindThreads(unsigned int retryCount, BOOL *pFMarkUnloadRequestThread);

    // Use Rude Abort to unload the domain.
    BOOL m_fRudeUnload;

    Thread *m_pUnloadRequestThread;
    ADUnloadSink*   m_ADUnloadSink;
    BOOL  m_bForceGCOnUnload;
    BOOL  m_bUnloadingFromUnloadEvent;
    AppDomainLoaderAllocator m_LoaderAllocator;

    // List of unloaded LoaderAllocators, protected by code:GetLoaderAllocatorReferencesLock (for now)
    LoaderAllocator * m_pDelayedLoaderAllocatorUnloadList;
    
public:
    
    // Register the loader allocator for deletion in code:ShutdownFreeLoaderAllocators.
    void RegisterLoaderAllocatorForDeletion(LoaderAllocator * pLoaderAllocator);
    
    AppDomain * m_pNextInDelayedUnloadList;
    
    void SetForceGCOnUnload(BOOL bSet)
    {
        m_bForceGCOnUnload=bSet;
    }

    void SetUnloadingFromUnloadEvent()
    {
        m_bUnloadingFromUnloadEvent=TRUE;
    }

    BOOL IsUnloadingFromUnloadEvent()
    {
        return m_bUnloadingFromUnloadEvent;
    }
    
    void SetRudeUnload()
    {
        LIMITED_METHOD_CONTRACT;

        m_fRudeUnload = TRUE;
    }

    BOOL IsRudeUnload()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fRudeUnload;
    }

    ADUnloadSink* GetADUnloadSink();
    ADUnloadSink* GetADUnloadSinkForUnload();
    void SetUnloadRequestThread(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;

        m_pUnloadRequestThread = pThread;
    }

    Thread *GetUnloadRequestThread()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pUnloadRequestThread;
    }

public:
    void SetGCRefPoint(int gccounter)
    {
        LIMITED_METHOD_CONTRACT;
        m_LoaderAllocator.SetGCRefPoint(gccounter);
    }
    int  GetGCRefPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return m_LoaderAllocator.GetGCRefPoint();
    }

    static USHORT GetOffsetOfId()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = offsetof(class AppDomain, m_dwId);
        _ASSERTE(FitsInI2(ofs));
        return (USHORT)ofs;
    }

    
    void AddMemoryPressure();
    void RemoveMemoryPressure();
    void Unload(BOOL fForceUnload);
    static HRESULT UnloadById(ADID Id, BOOL fSync, BOOL fExceptionsPassThrough=FALSE);
    static HRESULT UnloadWait(ADID Id, ADUnloadSink* pSink);
#ifdef FEATURE_TESTHOOKS        
    static HRESULT UnloadWaitNoCatch(ADID Id, ADUnloadSink* pSink);
#endif
    static void ResetUnloadRequestThread(ADID Id);

    void UnlinkClass(MethodTable *pMT);

    typedef Holder<AppDomain *, AppDomain::SetUnloadInProgress, AppDomain::SetUnloadComplete> UnloadHolder;
    Assembly *GetRootAssembly()
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
    SString         m_friendlyName;
    PTR_Assembly    m_pRootAssembly;

    // General purpose flags.
    DWORD           m_dwFlags;

    // When an application domain is created the ref count is artifically incremented
    // by one. For it to hit zero an explicit close must have happened.
    LONG        m_cRef;                    // Ref count.

    PTR_IApplicationSecurityDescriptor m_pSecDesc;  // Application Security Descriptor

    OBJECTHANDLE    m_ExposedObject;

#ifdef FEATURE_LOADER_OPTIMIZATION
    // Indicates where assemblies will be loaded for
    // this domain. By default all assemblies are loaded into the domain.
    // There are two additional settings, all
    // assemblies can be loaded into the shared domain or assemblies
    // that are strong named are loaded into the shared area.
    SharePolicy m_SharePolicy;
#endif

    IUnknown        *m_pComIPForExposedObject;

    // Hash table that maps a clsid to a type
    PtrHashMap          m_clsidHash;

#ifdef FEATURE_COMINTEROP
    // Hash table that maps WinRT class names to MethodTables.
    PTR_NameToTypeMapTable m_pNameToTypeMap;
    UINT                m_vNameToTypeMapVersion;

    UINT                m_nEpoch; // incremented each time m_pNameToTypeMap is enumerated

    // Hash table that remembers the last cached WinRT factory object per type per appdomain.
    WinRTFactoryCache   *m_pWinRTFactoryCache;

    // The wrapper cache for this domain - it has its own CCacheLineAllocator on a per domain basis
    // to allow the domain to go away and eventually kill the memory when all refs are gone
    ComCallWrapperCache *m_pComCallWrapperCache;
    
    // this cache stores the RCWs in this domain
    RCWCache *m_pRCWCache;

    // this cache stores the RCW -> CCW references in this domain
    RCWRefCache *m_pRCWRefCache;
    
    // The method table used for LicenseInteropHelper
    MethodTable*    m_pLicenseInteropHelperMT;
#endif // FEATURE_COMINTEROP

    AssemblySink*      m_pAsyncPool;  // asynchronous retrival object pool (only one is kept)

    // The index of this app domain among existing app domains (starting from 1)
    ADIndex m_dwIndex;

    // The thread-pool index of this app domain among existing app domains (starting from 1)
    TPIndex m_tpIndex;

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    ULONGLONG* m_pullAllocBytes;
    ULONGLONG* m_pullSurvivedBytes;
    DWORD m_dwNumHeaps;
    ULONGLONG m_ullLastEtwAllocBytes;
    // Total processor time (user and kernel) utilized by threads running in this AppDomain so far. May not
    // account for threads currently executing in the AppDomain until a call to QueryProcessorUsage() is
    // made.
    Volatile<ULONGLONG> m_ullTotalProcessorUsage;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

#ifdef _DEBUG
    struct ThreadTrackInfo;
    typedef CDynArray<ThreadTrackInfo *> ThreadTrackInfoList;
    ThreadTrackInfoList *m_pThreadTrackInfoList;
    DWORD m_TrackSpinLock;
#endif


    // IL stub cache with fabricated MethodTable parented by a random module in this AD.
    ILStubCache         m_ILStubCache;

    // U->M thunks created in this domain and not associated with a delegate.
    // The cache is keyed by MethodDesc pointers.
    UMEntryThunkCache *m_pUMEntryThunkCache;

    // The number of  times we have entered this AD
    ULONG m_dwThreadEnterCount;
    // The number of threads that have entered this AD, for ADU only
    ULONG m_dwThreadsStillInAppDomain;

    Volatile<Stage> m_Stage;

    // The default context for this domain
    Context *m_pDefaultContext;

    SString         m_applicationBase;
    SString         m_privateBinPaths;
    SString         m_configFile;

    ArrayList        m_failedAssemblies;

    DomainAssembly * m_anonymouslyHostedDynamicMethodsAssembly;

#ifdef _DEBUG
    Volatile<LONG> m_dwIterHolders;
    Volatile<LONG> m_dwRefTakers;
    Volatile<LONG> m_dwCreationHolders;
#endif

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

#ifdef FEATURE_FUSION
    void TurnOnBindingRedirects();
#endif
public:

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
private:
    Volatile<BOOL> m_fIsBindingModelLocked;
public:
    BOOL IsHostAssemblyResolverInUse();
    BOOL IsBindingModelLocked();
    BOOL LockBindingModel();
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    UMEntryThunkCache *GetUMEntryThunkCache();

    ILStubCache* GetILStubCache()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_ILStubCache;
    }

    static AppDomain* GetDomain(ILStubCache* pILStubCache)
    {
        return CONTAINING_RECORD(pILStubCache, AppDomain, m_ILStubCache);
    }

    enum {
        CONTEXT_INITIALIZED =               0x0001,
        USER_CREATED_DOMAIN =               0x0002, // created by call to AppDomain.CreateDomain
        ALLOCATEDCOM =                      0x0008,
        LOAD_SYSTEM_ASSEMBLY_EVENT_SENT =   0x0040,
        REMOTING_CONFIGURED_FOR_DOMAIN =    0x0100,
        COMPILATION_DOMAIN =                0x0400, // Are we ngenning?
        APP_DOMAIN_CAN_BE_UNLOADED =        0x0800, // if need extra bits, can derive this at runtime
        ORPHANED_LOCKS =                    0x1000, // Orphaned locks exist in this appdomain.
        PASSIVE_DOMAIN =                    0x2000, // Can we execute code in this AppDomain
        VERIFICATION_DOMAIN =               0x4000, // This is a verification domain
        ILLEGAL_VERIFICATION_DOMAIN =       0x8000, // This can't be a verification domain
        IGNORE_UNHANDLED_EXCEPTIONS =      0x10000, // AppDomain was created using the APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS flag
        ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP    =      0x20000, // AppDomain was created using the APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP flag
#ifdef FEATURE_CORECLR
        ENABLE_SKIP_PLAT_CHECKS         = 0x200000, // Skip various assembly checks (like platform check)
        ENABLE_ASSEMBLY_LOADFILE        = 0x400000, // Allow Assembly.LoadFile in CoreCLR
        DISABLE_TRANSPARENCY_ENFORCEMENT= 0x800000, // Disable enforcement of security transparency rules
#endif        
    };

    SecurityContext *m_pSecContext;

    AssemblySpecBindingCache  m_AssemblyCache;
    DomainAssemblyCache       m_UnmanagedCache;
    size_t                    m_MemoryPressure;

    SString m_AppDomainManagerAssembly;
    SString m_AppDomainManagerType;
    BOOL    m_fAppDomainManagerSetInConfig;
    EInitializeNewDomainFlags m_dwAppDomainManagerInitializeDomainFlags;

#ifdef FEATURE_CORECLR 
    ArrayList m_NativeDllSearchDirectories;
#endif
    BOOL m_ReversePInvokeCanEnter;
    bool m_ForceTrivialWaitOperations;
    // Section to support AD unload due to escalation
public:
    static void CreateADUnloadWorker();

    static void CreateADUnloadStartEvent();

    static DWORD WINAPI ADUnloadThreadStart(void *args);

    // Default is safe unload with test hook
    void EnableADUnloadWorker();

    // If called to handle stack overflow, we can not set event, since the thread has limit stack.
    void EnableADUnloadWorker(EEPolicy::AppDomainUnloadTypes type, BOOL fHasStack = TRUE);

    static void EnableADUnloadWorkerForThreadAbort();
    static void EnableADUnloadWorkerForFinalizer();
    static void EnableADUnloadWorkerForCollectedADCleanup();

    BOOL IsUnloadRequested()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_Stage == STAGE_UNLOAD_REQUESTED);
    }

#ifdef FEATURE_CORECLR
    BOOL IsImageFromTrustedPath(PEImage* pImage);
    BOOL IsImageFullyTrusted(PEImage* pImage);
#endif

#ifdef FEATURE_TYPEEQUIVALENCE
private:
    VolatilePtr<TypeEquivalenceHashTable> m_pTypeEquivalenceTable;
    CrstExplicitInit m_TypeEquivalenceCrst;
public:
    TypeEquivalenceHashTable * GetTypeEquivalenceCache();
#endif

    private:
    static void ADUnloadWorkerHelper(AppDomain *pDomain);
    static CLREvent * g_pUnloadStartEvent;

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

#ifdef FEATURE_COMINTEROP

private:
#ifdef FEATURE_REFLECTION_ONLY_LOAD
    // ReflectionOnly WinRT binder and its TypeCache (only in classic = non-AppX; the scenario is not supported in AppX)
    CLRPrivBinderReflectionOnlyWinRT *    m_pReflectionOnlyWinRtBinder;
    CLRPrivTypeCacheReflectionOnlyWinRT * m_pReflectionOnlyWinRtTypeCache;
#endif // FEATURE_REFLECTION_ONLY_LOAD

#endif //FEATURE_COMINTEROP

public:
#ifndef FEATURE_CORECLR
    BOOL m_bUseOsSorting;
    DWORD m_sortVersion;
    COMNlsCustomSortLibrary *m_pCustomSortLibrary;
#if _DEBUG
    BOOL m_bSortingInitialized;
#endif // _DEBUG
    COMNlsHashProvider *m_pNlsHashProvider;
#endif // !FEATURE_CORECLR

private:
    // This is the root-level default load context root binder. If null, then
    // the Fusion binder is used; otherwise this binder is used.
    ReleaseHolder<ICLRPrivBinder> m_pLoadContextHostBinder;

    // -------------------------
    // IMPORTANT!
    // The shared and designer context binders are ONLY to be used in tool
    // scenarios. There are known issues where use of these binders will
    // cause application crashes, and interesting behaviors.
    // -------------------------
    
    // This is the default designer shared context root binder.
    // This is used as the parent binder for ImmersiveDesignerContextBinders
    ReleaseHolder<ICLRPrivBinder> m_pSharedContextHostBinder;

    // This is the current context root binder.
    // Normally, this variable is immutable for appdomain lifetime, but in designer scenarios
    // it may be replaced by designer context binders
    Volatile<ICLRPrivBinder *>    m_pCurrentContextHostBinder;

public:
    // Returns the current hosted binder, or null if none available.
    inline
    ICLRPrivBinder * GetCurrentLoadContextHostBinder() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCurrentContextHostBinder;
    }

    // Returns the shared context binder, or null if none available.
    inline
    ICLRPrivBinder * GetSharedContextHostBinder() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSharedContextHostBinder;
    }

    // Returns the load context binder, or null if none available.
    inline
    ICLRPrivBinder * GetLoadContextHostBinder() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoadContextHostBinder;
    }

#ifndef DACCESS_COMPILE

    // This is only called from the ImmersiveDesignerContext code
    // It is protected with a managed monitor lock
    inline
    void SetSharedContextHostBinder(ICLRPrivBinder * pBinder)
    {
        LIMITED_METHOD_CONTRACT;
        pBinder->AddRef();
        m_pSharedContextHostBinder = pBinder;
    }

    // This is called from CorHost2's implementation of ICLRPrivRuntime::CreateAppDomain.
    // Should only be called during AppDomain creation.
    inline
    void SetLoadContextHostBinder(ICLRPrivBinder * pBinder)
    {
        LIMITED_METHOD_CONTRACT;
        pBinder->AddRef();
        m_pLoadContextHostBinder = m_pCurrentContextHostBinder = pBinder;
    }

    inline
    void SetCurrentContextHostBinder(ICLRPrivBinder * pBinder)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
        }
        CONTRACTL_END;

        LockHolder lh(this);

#ifdef FEATURE_COMINTEROP
        if (m_pNameToTypeMap != nullptr)
        {
            delete m_pNameToTypeMap;
            m_pNameToTypeMap = nullptr;
        }

        m_vNameToTypeMapVersion++;
#endif

        m_pCurrentContextHostBinder = pBinder;
    }

#endif // DACCESS_COMPILE

    // Indicates that a hosted binder is present.
    inline
    bool HasLoadContextHostBinder()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoadContextHostBinder != nullptr;
    }

    class ComInterfaceReleaseList
    {
        SArray<IUnknown *> m_objects;
    public:
        ~ComInterfaceReleaseList()
        {
            WRAPPER_NO_CONTRACT;

            for (COUNT_T i = 0; i < m_objects.GetCount(); i++)
            {
                IUnknown *pItf = *(m_objects.GetElements() + i);
                if (pItf != nullptr)
                    pItf->Release();
            }
        }

        // Append to the list of object to free. Only use under the AppDomain "LockHolder(pAppDomain)"
        void Append(IUnknown *pInterfaceToRelease)
        {
            WRAPPER_NO_CONTRACT;
            m_objects.Append(pInterfaceToRelease);
        }
    } AppDomainInterfaceReleaseList;

private:
    //-----------------------------------------------------------
    // Static ICLRPrivAssembly -> DomainAssembly mapping functions.
    // This map does not maintain a reference count to either key or value.
    // PEFile maintains a reference count on the ICLRPrivAssembly through its code:PEFile::m_pHostAssembly field.
    // It is removed from this hash table by code:DomainAssembly::~DomainAssembly.
    struct HostAssemblyHashTraits : public DefaultSHashTraits<PTR_DomainAssembly>
    {
    public:
        typedef PTR_ICLRPrivAssembly key_t;
        
        static key_t GetKey(element_t const & elem)
        {
            STATIC_CONTRACT_WRAPPER;
            return elem->GetFile()->GetHostAssembly();
        }
        
        static BOOL Equals(key_t key1, key_t key2) 
        {
            LIMITED_METHOD_CONTRACT;
            return dac_cast<TADDR>(key1) == dac_cast<TADDR>(key2);
        }
        
        static count_t Hash(key_t key)
        {
            STATIC_CONTRACT_LIMITED_METHOD;
            //return reinterpret_cast<count_t>(dac_cast<TADDR>(key));
            return (count_t)(dac_cast<TADDR>(key));
        }
        
        static const element_t Null() { return NULL; }
        static const element_t Deleted() { return (element_t)(TADDR)-1; }
        static bool IsNull(const element_t & e) { return e == NULL; }
        static bool IsDeleted(const element_t & e) { return dac_cast<TADDR>(e) == (TADDR)-1; }
    };

    struct OriginalFileHostAssemblyHashTraits : public HostAssemblyHashTraits
    {
    public:
        static key_t GetKey(element_t const & elem)
        {
            STATIC_CONTRACT_WRAPPER;
            return elem->GetOriginalFile()->GetHostAssembly();
        }
    };
    
    typedef SHash<HostAssemblyHashTraits> HostAssemblyMap;
    typedef SHash<OriginalFileHostAssemblyHashTraits> OriginalFileHostAssemblyMap;
    HostAssemblyMap   m_hostAssemblyMap;
    OriginalFileHostAssemblyMap   m_hostAssemblyMapForOrigFile;
    CrstExplicitInit  m_crstHostAssemblyMap;
    // Lock to serialize all Add operations (in addition to the "read-lock" above)
    CrstExplicitInit  m_crstHostAssemblyMapAdd;

public:
    // Returns DomainAssembly.
    PTR_DomainAssembly FindAssembly(PTR_ICLRPrivAssembly pHostAssembly);
    
#ifndef DACCESS_COMPILE
private:
    friend void DomainAssembly::Allocate();
    friend DomainAssembly::~DomainAssembly();

    // Called from DomainAssembly::Begin.
    void PublishHostedAssembly(
        DomainAssembly* pAssembly);

    // Called from DomainAssembly::UpdatePEFile.
    void UpdatePublishHostedAssembly(
        DomainAssembly* pAssembly,
        PTR_PEFile pFile);

    // Called from DomainAssembly::~DomainAssembly
    void UnPublishHostedAssembly(
        DomainAssembly* pAssembly);
#endif // DACCESS_COMPILE

#ifdef FEATURE_PREJIT
    friend void DomainFile::InsertIntoDomainFileWithNativeImageList();
    Volatile<DomainFile *> m_pDomainFileWithNativeImageList;
public:
    DomainFile *GetDomainFilesWithNativeImagesList()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDomainFileWithNativeImageList;
    }
#endif
};  // class AppDomain


// This holder is to be used to take a reference to make sure AppDomain* is still valid
// Please do not use if you are aleady ADU-safe
typedef Wrapper<AppDomain*,AppDomain::RefTakerAcquire,AppDomain::RefTakerRelease,NULL> AppDomainRefTaker;

// Just a ref holder
typedef ReleaseHolder<AppDomain> AppDomainRefHolder;

// This class provides a way to access AppDomain by ID
// without risking the appdomain getting invalid in the process
class AppDomainFromIDHolder
{
public:
    enum SyncType  
    {
        SyncType_GC,     // Prevents AD from being unloaded by forbidding GC for the lifetime of the object
        SyncType_ADLock  // Prevents AD from being unloaded by requiring ownership of DomainLock for the lifetime of the object
    };
protected:    
    AppDomain* m_pDomain;
#ifdef _DEBUG    
    BOOL       m_bAcquired;
    BOOL       m_bChecked;
    SyncType   m_type;
#endif
public:
    DEBUG_NOINLINE AppDomainFromIDHolder(ADID adId, BOOL bUnsafePoint, SyncType synctype=SyncType_GC);
    DEBUG_NOINLINE AppDomainFromIDHolder(SyncType synctype=SyncType_GC);
    DEBUG_NOINLINE ~AppDomainFromIDHolder();

	void* GetAddress() { return m_pDomain; } 	// Used to get an identfier for ETW
    void Assign(ADID adId, BOOL bUnsafePoint);
    void ThrowIfUnloaded();
    void Release();
    BOOL IsUnloaded() 
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        m_bChecked=TRUE; 
        if (m_pDomain==NULL)
        {
            // no need to enforce anything
            Release(); 
        }
#endif
        return m_pDomain==NULL;
    };
    AppDomain* operator->();
};  // class AppDomainFromIDHolder



typedef VPTR(class SystemDomain) PTR_SystemDomain;

class SystemDomain : public BaseDomain
{
    friend class AppDomainNative;
    friend class AppDomainIterator;
    friend class UnsafeAppDomainIterator;
    friend class ClrDataAccess;
    friend class AppDomainFromIDHolder;
    friend Frame *Thread::IsRunningIn(AppDomain* pDomain, int *count);

    VPTR_VTABLE_CLASS(SystemDomain, BaseDomain)
    VPTR_UNIQUE(VPTR_UNIQUE_SystemDomain)
    static AppDomain *GetAppDomainAtId(ADID indx);

public:  
    static PTR_LoaderAllocator GetGlobalLoaderAllocator();
    virtual PTR_LoaderAllocator GetLoaderAllocator() { WRAPPER_NO_CONTRACT; return GetGlobalLoaderAllocator(); }
    static AppDomain* GetAppDomainFromId(ADID indx,DWORD ADValidityKind)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        AppDomain* pRetVal;
        if (indx.m_dwId==DefaultADID)
            pRetVal= SystemDomain::System()->DefaultDomain();
        else
            pRetVal= GetAppDomainAtId(indx);
#ifdef _DEBUG
        // Only call CheckADValidity in DEBUG builds for non-NULL return values
        if (pRetVal != NULL)
            CheckADValidity(pRetVal, ADValidityKind);
#endif        
        return pRetVal;
    }
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
    void Terminate();
    static void LazyInitGlobalStringLiteralMap();

    //****************************************************************************************
    //
    // Load the base system classes, these classes are required before
    // any other classes are loaded
    void LoadBaseSystemClasses();

    AppDomain* DefaultDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pDefaultDomain;
    }

    // Notification when an assembly is loaded into the system domain
    void OnAssemblyLoad(Assembly *assem);

    //****************************************************************************************
    //
    // Global Static to get the one and only system domain
    static SystemDomain * System()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pSystemDomain;
    }

    static PEAssembly* SystemFile()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_pSystemDomain);
        return System()->m_pSystemFile;
    }

    static Assembly* SystemAssembly()
    {
        WRAPPER_NO_CONTRACT;

        return System()->m_pSystemAssembly;
    }

    static Module* SystemModule()
    {
        WRAPPER_NO_CONTRACT;

        return SystemAssembly()->GetManifestModule();
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
#endif // DACCESS_COMPILE

#ifndef FEATURE_CORECLR    
	static void ExecuteMainMethod(HMODULE hMod, __in_opt LPWSTR path = NULL);
#endif
    static void ActivateApplication(int *pReturnValue);

    static void InitializeDefaultDomain(BOOL allowRedirects, ICLRPrivBinder * pBinder = NULL);
    static void SetupDefaultDomain();
    static HRESULT SetupDefaultDomainNoThrow();

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)
    static Thread::ApartmentState GetEntryPointThreadAptState(IMDInternalImport* pScope, mdMethodDef mdMethod);
    static void SetThreadAptState(IMDInternalImport* pScope, Thread::ApartmentState state);
#endif
    static BOOL SetGlobalSharePolicyUsingAttribute(IMDInternalImport* pScope, mdMethodDef mdMethod);

#ifdef FEATURE_MIXEDMODE
    static HRESULT RunDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved);
#endif // FEATURE_MIXEDMODE

    //****************************************************************************************
    //
    // Use an already exising & inited Application Domain (e.g. a subclass).
    static void LoadDomain(AppDomain     *pDomain);

#ifndef DACCESS_COMPILE
    static void MakeUnloadable(AppDomain* pApp)
    {
        WRAPPER_NO_CONTRACT;
        System()->AddDomain(pApp);
        pApp->SetCanUnload();
    }
#endif // DACCESS_COMPILE

    //****************************************************************************************
    // Methods used to get the callers module and hence assembly and app domain.
    __declspec(deprecated("This method is deprecated, use the version that takes a StackCrawlMark instead"))
    static Module* GetCallersModule(int skip);
    static MethodDesc* GetCallersMethod(StackCrawlMark* stackMark, AppDomain **ppAppDomain = NULL);
    static MethodTable* GetCallersType(StackCrawlMark* stackMark, AppDomain **ppAppDomain = NULL);
    static Module* GetCallersModule(StackCrawlMark* stackMark, AppDomain **ppAppDomain = NULL);
    static Assembly* GetCallersAssembly(StackCrawlMark* stackMark, AppDomain **ppAppDomain = NULL);

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

    //****************************************************************************************
    // Helper function to remove a domain from the system
    BOOL RemoveDomain(AppDomain* pDomain); // Does not decrement the reference

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

    //****************************************************************************************
    // return the dev path
#ifdef FEATURE_FUSION    
    void GetDevpathW(__out_ecount_opt(1) LPWSTR* pPath, DWORD* pSize);
#endif

#ifndef DACCESS_COMPILE
    void IncrementNumAppDomains ()
    {
        LIMITED_METHOD_CONTRACT;

        s_dNumAppDomains++;
    }

    void DecrementNumAppDomains ()
    {
        LIMITED_METHOD_CONTRACT;

        s_dNumAppDomains--;
    }

    ULONG GetNumAppDomains ()
    {
        LIMITED_METHOD_CONTRACT;

        return s_dNumAppDomains;
    }
#endif // DACCESS_COMPILE

    //
    // AppDomains currently have both an index and an ID.  The
    // index is "densely" assigned; indices are reused as domains
    // are unloaded.  The Id's on the other hand, are not reclaimed
    // so may be sparse.
    //
    // Another important difference - it's OK to call GetAppDomainAtId for
    // an unloaded domain (it will return NULL), while GetAppDomainAtIndex
    // will assert if the domain is unloaded.
    //<TODO>
    // @todo:
    // I'm not really happy with this situation, but
    //  (a) we need an ID for a domain which will last the process lifetime for the
    //      remoting code.
    //  (b) we need a dense ID, for the handle table index.
    // So for now, I'm leaving both, but hopefully in the future we can come up
    // with something better.
    //</TODO>

    static ADIndex GetNewAppDomainIndex(AppDomain * pAppDomain);
    static void ReleaseAppDomainIndex(ADIndex indx);
    static PTR_AppDomain GetAppDomainAtIndex(ADIndex indx);
    static PTR_AppDomain TestGetAppDomainAtIndex(ADIndex indx);
    static DWORD GetCurrentAppDomainMaxIndex()
    {
        WRAPPER_NO_CONTRACT;

        ArrayListStatic* list = (ArrayListStatic *)&m_appDomainIndexList;
        PREFIX_ASSUME(list!=NULL);
        return list->GetCount();
    }

    static ADID GetNewAppDomainId(AppDomain *pAppDomain);
    static void ReleaseAppDomainId(ADID indx);
    
#ifndef DACCESS_COMPILE
    static ADID GetCurrentAppDomainMaxId() { ADID id; id.m_dwId=m_appDomainIdList.GetCount(); return id;}
#endif // DACCESS_COMPILE


#ifndef DACCESS_COMPILE
    DWORD RequireAppDomainCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDelayedUnloadList != 0 || m_pDelayedUnloadListOfLoaderAllocators != 0;
    }

    void AddToDelayedUnloadList(AppDomain* pDomain, BOOL bAsync)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        m_UnloadIsAsync = bAsync;
        
        CrstHolder lh(&m_DelayedUnloadCrst);
        pDomain->m_pNextInDelayedUnloadList=m_pDelayedUnloadList;
        m_pDelayedUnloadList=pDomain;
        if (m_UnloadIsAsync)
        {
            pDomain->AddRef();
            int iGCRefPoint=GCHeap::GetGCHeap()->CollectionCount(GCHeap::GetGCHeap()->GetMaxGeneration());
            if (GCHeap::GetGCHeap()->IsGCInProgress())
                iGCRefPoint++;
            pDomain->SetGCRefPoint(iGCRefPoint);
        }
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

        int iGCRefPoint=GCHeap::GetGCHeap()->CollectionCount(GCHeap::GetGCHeap()->GetMaxGeneration());
        if (GCHeap::GetGCHeap()->IsGCInProgress())
            iGCRefPoint++;
        pAllocator->SetGCRefPoint(iGCRefPoint);
    }

    void ClearCollectedDomains();
    void ProcessClearingDomains();
    void ProcessDelayedUnloadDomains();
    
    static void SetUnloadInProgress(AppDomain *pDomain)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_pAppDomainBeingUnloaded == NULL);
        m_pAppDomainBeingUnloaded = pDomain;
        m_dwIndexOfAppDomainBeingUnloaded = pDomain->GetIndex();
    }

    static void SetUnloadDomainCleared()
    {
        LIMITED_METHOD_CONTRACT;

        // about to delete, so clear this pointer so nobody uses it
        m_pAppDomainBeingUnloaded = NULL;
    }
    static void SetUnloadComplete()
    {
        LIMITED_METHOD_CONTRACT;

        // should have already cleared the AppDomain* prior to delete
        // either we succesfully unloaded and cleared or we failed and restored the ID
        _ASSERTE(m_pAppDomainBeingUnloaded == NULL && m_dwIndexOfAppDomainBeingUnloaded.m_dwIndex != 0
            || m_pAppDomainBeingUnloaded && SystemDomain::GetAppDomainAtId(m_pAppDomainBeingUnloaded->GetId()) != NULL);
        m_pAppDomainBeingUnloaded = NULL;
        m_pAppDomainUnloadingThread = NULL;
    }

    static AppDomain *AppDomainBeingUnloaded()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAppDomainBeingUnloaded;
    }

    static ADIndex IndexOfAppDomainBeingUnloaded()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwIndexOfAppDomainBeingUnloaded;
    }

    static void SetUnloadRequestingThread(Thread *pRequestingThread)
    {
        LIMITED_METHOD_CONTRACT;
        m_pAppDomainUnloadRequestingThread = pRequestingThread;
    }

    static Thread *GetUnloadRequestingThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAppDomainUnloadRequestingThread;
    }

    static void SetUnloadingThread(Thread *pUnloadingThread)
    {
        LIMITED_METHOD_CONTRACT;
        m_pAppDomainUnloadingThread = pUnloadingThread;
    }

    static Thread *GetUnloadingThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAppDomainUnloadingThread;
    }

    static void EnumAllStaticGCRefs(promote_func* fn, ScanContext* sc);

#endif // DACCESS_COMPILE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    // The *AD* methods are what we got from tracing through EE roots.
    // RecordTotalSurvivedBytes is the total promoted from a GC.
    static void ResetADSurvivedBytes();
    static ULONGLONG GetADSurvivedBytes();
    static void RecordTotalSurvivedBytes(size_t totalSurvivedBytes);
    static ULONGLONG GetTotalSurvivedBytes()
    {
        LIMITED_METHOD_CONTRACT;
        return m_totalSurvivedBytes;
    }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

    //****************************************************************************************
    // Routines to deal with the base library (currently mscorlib.dll)
    LPCWSTR BaseLibrary()
    {
        WRAPPER_NO_CONTRACT;

        return m_BaseLibrary;
    }

#ifndef DACCESS_COMPILE
    BOOL IsBaseLibrary(SString &path)
    {
        WRAPPER_NO_CONTRACT;

        // See if it is the installation path to mscorlib
        if (path.EqualsCaseInsensitive(m_BaseLibrary, PEImage::GetFileSystemLocale()))
            return TRUE;

        // Or, it might be the GAC location of mscorlib
        if (System()->SystemAssembly() != NULL
            && path.EqualsCaseInsensitive(System()->SystemAssembly()->GetManifestFile()->GetPath(),
                                          PEImage::GetFileSystemLocale()))
            return TRUE;

        return FALSE;
    }

    BOOL IsBaseLibrarySatellite(SString &path)
    {
        WRAPPER_NO_CONTRACT;

        // See if it is the installation path to mscorlib.resources
        SString s(SString::Ascii,g_psBaseLibrarySatelliteAssemblyName);
        if (path.EqualsCaseInsensitive(s, PEImage::GetFileSystemLocale()))
            return TRUE;

        // workaround!  Must implement some code to do this string comparison for
        // mscorlib.resources in a culture-specific directory in the GAC.

        /*
        // Or, it might be the GAC location of mscorlib.resources
        if (System()->SystemAssembly() != NULL
            && path.EqualsCaseInsensitive(System()->SystemAssembly()->GetManifestFile()->GetPath(),
                                          PEImage::GetFileSystemLocale()))
            return TRUE;
        */

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

    //****************************************************************************************
    // Helper function to create the single COM domain
    void CreateDefaultDomain();

    //****************************************************************************************
    // Helper function to add a domain to the global list
    void AddDomain(AppDomain* pDomain);

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

        m_pDefaultDomain = NULL;
        m_pDelayedUnloadList=NULL;
        m_pDelayedUnloadListOfLoaderAllocators=NULL;
        m_UnloadIsAsync = FALSE;

        m_GlobalAllocator.Init(this);
    }
#endif

    PTR_PEAssembly  m_pSystemFile;      // Single assembly (here for quicker reference);
    PTR_Assembly    m_pSystemAssembly;  // Single assembly (here for quicker reference);
    PTR_AppDomain   m_pDefaultDomain;   // Default domain for COM+ classes exposed through IClassFactory.

    GlobalLoaderAllocator m_GlobalAllocator;


    InlineSString<100>  m_BaseLibrary;

#ifdef FEATURE_VERSIONING

    InlineSString<100>  m_SystemDirectory;

#else

    LPCWSTR             m_SystemDirectory;

#endif

    LPWSTR      m_pwDevpath;
    DWORD       m_dwDevpath;
    BOOL        m_fDevpath;  // have we searched the environment

    // <TODO>@TODO: CTS, we can keep the com modules in a single assembly or in different assemblies.
    // We are currently using different assemblies but this is potentitially to slow...</TODO>

    // Global domain that every one uses
    SPTR_DECL(SystemDomain, m_pSystemDomain);

    AppDomain* m_pDelayedUnloadList;
    BOOL m_UnloadIsAsync;

    LoaderAllocator * m_pDelayedUnloadListOfLoaderAllocators;

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    // This is what gets promoted for the whole GC heap.
    static size_t m_totalSurvivedBytes;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

    SVAL_DECL(ArrayListStatic, m_appDomainIndexList);
#ifndef DACCESS_COMPILE
    static CrstStatic m_DelayedUnloadCrst;
    static CrstStatic       m_SystemDomainCrst;


    static ArrayListStatic  m_appDomainIdList;

    // only one ad can be unloaded at a time
    static AppDomain*   m_pAppDomainBeingUnloaded;
    // need this so can determine AD being unloaded after it has been deleted
    static ADIndex      m_dwIndexOfAppDomainBeingUnloaded;

    // if had to spin off a separate thread to do the unload, this is the original thread.
    // allows us to delay aborting it until it's the last one so that it can receive
    // notification of an unload failure
    static Thread *m_pAppDomainUnloadRequestingThread;

    // this is the thread doing the actual unload. He's allowed to enter the domain
    // even if have started unloading.
    static Thread *m_pAppDomainUnloadingThread;

    static GlobalStringLiteralMap *m_pGlobalStringLiteralMap;

    static ULONG       s_dNumAppDomains;  // Maintain a count of children app domains.

    static DWORD        m_dwLowestFreeIndex;
#endif // DACCESS_COMPILE

protected:

    // These flags let the correct native image of mscorlib to be loaded.
    // This is important for hardbinding to it

    SVAL_DECL(BOOL, s_fForceDebug);
    SVAL_DECL(BOOL, s_fForceProfiling);
    SVAL_DECL(BOOL, s_fForceInstrument);

public:
    static void     SetCompilationOverrides(BOOL fForceDebug,
                                            BOOL fForceProfiling,
                                            BOOL fForceInstrument);

    static void     GetCompilationOverrides(BOOL * fForceDebug,
                                            BOOL * fForceProfiling,
                                            BOOL * fForceInstrument);
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


//
// an UnsafeAppDomainIterator is used to iterate over all existing domains
//
// The iteration is guaranteed to include all domains that exist at the
// start & end of the iteration. This iterator is considered unsafe because it does not
// reference count the various appdomains, and can only be used when the runtime is stopped,
// or external synchronization is used. (and therefore no other thread may cause the appdomain list to change.)
//
class UnsafeAppDomainIterator
{
    friend class SystemDomain;
public:
    UnsafeAppDomainIterator(BOOL bOnlyActive)
    {
        m_bOnlyActive = bOnlyActive;
    }

    void Init()
    {
        LIMITED_METHOD_CONTRACT;
        SystemDomain* sysDomain = SystemDomain::System();
        if (sysDomain)
        {
            ArrayListStatic* list = &sysDomain->m_appDomainIndexList;
            PREFIX_ASSUME(list != NULL);
            m_i = list->Iterate();
        }
        else
        {
            m_i.SetEmpty();
        }

        m_pCurrent = NULL;
    }

    BOOL Next()
    {
        WRAPPER_NO_CONTRACT;

        while (m_i.Next())
        {
            m_pCurrent = dac_cast<PTR_AppDomain>(m_i.GetElement());
            if (m_pCurrent != NULL &&
                (m_bOnlyActive ?
                 m_pCurrent->IsActive() : m_pCurrent->IsValid()))
            {
                return TRUE;
            }
        }

        m_pCurrent = NULL;
        return FALSE;
    }

    AppDomain * GetDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pCurrent;
    }

  private:

    ArrayList::Iterator m_i;
    AppDomain *         m_pCurrent;
    BOOL                m_bOnlyActive;
};  // class UnsafeAppDomainIterator

//
// an AppDomainIterator is used to iterate over all existing domains.
//
// The iteration is guaranteed to include all domains that exist at the
// start & end of the iteration.  Any domains added or deleted during
// iteration may or may not be included.  The iterator also guarantees
// that the current iterated appdomain (GetDomain()) will not be deleted.
//

class AppDomainIterator : public UnsafeAppDomainIterator
{
    friend class SystemDomain;

  public:
    AppDomainIterator(BOOL bOnlyActive) : UnsafeAppDomainIterator(bOnlyActive)
    {
        WRAPPER_NO_CONTRACT;
        Init();
    }

    ~AppDomainIterator()
    {
        WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
        if (GetDomain() != NULL)
        {
#ifdef _DEBUG            
            GetDomain()->IteratorRelease();
#endif            
            GetDomain()->Release();
        }
#endif
    }

    BOOL Next()
    {
        WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
        if (GetDomain() != NULL)
        {
#ifdef _DEBUG            
            GetDomain()->IteratorRelease();
#endif            
            GetDomain()->Release();
        }

        SystemDomain::LockHolder lh;
#endif

        if (UnsafeAppDomainIterator::Next())
        {
#ifndef DACCESS_COMPILE
            GetDomain()->AddRef();
#ifdef _DEBUG            
            GetDomain()->IteratorAcquire();
#endif
#endif
            return TRUE;
        }

        return FALSE;
    }
};  // class AppDomainIterator

typedef VPTR(class SharedDomain) PTR_SharedDomain;

class SharedDomain : public BaseDomain
{
    VPTR_VTABLE_CLASS_AND_CTOR(SharedDomain, BaseDomain)

public:

    static void Attach();
    static void Detach();

    virtual BOOL IsSharedDomain() { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
    virtual PTR_LoaderAllocator GetLoaderAllocator() { WRAPPER_NO_CONTRACT; return SystemDomain::GetGlobalLoaderAllocator(); }

    virtual PTR_AppDomain AsAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        _ASSERTE(!"Not an AppDomain");
        return NULL;
    }

    static SharedDomain * GetDomain();

    void Init();
    void Terminate();

    // This will also set the tenured bit if and only if the add was successful,
    // and will make sure that the bit appears atomically set to all readers that
    // might be accessing the hash on another thread.
    MethodTable * FindIndexClass(SIZE_T index);

#ifdef FEATURE_LOADER_OPTIMIZATION
    void AddShareableAssembly(Assembly * pAssembly);

    class SharedAssemblyIterator
    {
        PtrHashMap::PtrIterator i;
        Assembly * m_pAssembly;

      public:
        SharedAssemblyIterator() :
          i(GetDomain() ? GetDomain()->m_assemblyMap.firstBucket() : NULL)
        { LIMITED_METHOD_DAC_CONTRACT; }

        BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            if (i.end())
                return FALSE;

            m_pAssembly = PTR_Assembly(dac_cast<TADDR>(i.GetValue()));
            ++i;
            return TRUE;
        }

        Assembly * GetAssembly()
        {
            LIMITED_METHOD_DAC_CONTRACT;

            return m_pAssembly;
        }

      private:
        friend class SharedDomain;
    };
    
    Assembly * FindShareableAssembly(SharedAssemblyLocator * pLocator);
    SIZE_T GetShareableAssemblyCount();
#endif //FEATURE_LOADER_OPTIMIZATION

private:
    friend class SharedAssemblyIterator;
    friend class SharedFileLockHolder;
    friend class ClrDataAccess;

#ifndef DACCESS_COMPILE
    void *operator new(size_t size, void *pInPlace);
    void operator delete(void *pMem);
#endif

    SPTR_DECL(SharedDomain, m_pSharedDomain);

#ifdef FEATURE_LOADER_OPTIMIZATION
    PEFileListLock          m_FileCreateLock;
    SIZE_T                  m_nextClassIndex;
    PtrHashMap              m_assemblyMap;
#endif
    
public:
#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif

#ifdef FEATURE_LOADER_OPTIMIZATION
    // Hash map comparison function`
    static BOOL CompareSharedAssembly(UPTR u1, UPTR u2);
#endif
};

#ifdef FEATURE_LOADER_OPTIMIZATION
class SharedFileLockHolderBase : protected HolderBase<PEFile *>
{
  protected:
    PEFileListLock      *m_pLock;
    ListLockEntry   *m_pLockElement;

    SharedFileLockHolderBase(PEFile *value)
      : HolderBase<PEFile *>(value)
    {
        LIMITED_METHOD_CONTRACT;

        m_pLock = NULL;
        m_pLockElement = NULL;
    }

#ifndef DACCESS_COMPILE
    void DoAcquire()
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_FAULT;

        PEFileListLockHolder lockHolder(m_pLock);

        m_pLockElement = m_pLock->FindFileLock(m_value);
        if (m_pLockElement == NULL)
        {
            m_pLockElement = new ListLockEntry(m_pLock, m_value);
            m_pLock->AddElement(m_pLockElement);
        }
        else
            m_pLockElement->AddRef();

        lockHolder.Release();

        m_pLockElement->Enter();
    }

    void DoRelease()
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_FORBID_FAULT;

        m_pLockElement->Leave();
        m_pLockElement->Release();
        m_pLockElement = NULL;
    }
#endif // DACCESS_COMPILE
};

class SharedFileLockHolder : public BaseHolder<PEFile *, SharedFileLockHolderBase>
{
  public:
    DEBUG_NOINLINE SharedFileLockHolder(SharedDomain *pDomain, PEFile *pFile, BOOL Take = TRUE)
      : BaseHolder<PEFile *, SharedFileLockHolderBase>(pFile, FALSE)
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_FAULT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

        m_pLock = &pDomain->m_FileCreateLock;
        if (Take)
            Acquire();
    }
};
#endif // FEATURE_LOADER_OPTIMIZATION

inline BOOL BaseDomain::IsDefaultDomain()
{ 
    LIMITED_METHOD_DAC_CONTRACT; 
    return (SystemDomain::System()->DefaultDomain() == this);
}

#include "comreflectioncache.inl"

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
// holds an extra reference so needs special Extract() and should not have SuppressRelease()
// Holders/Wrappers have nonvirtual methods so cannot use them as the base class
template <class AppDomainType>
class AppDomainCreationHolder 
{
private:
    // disable the copy ctor
    AppDomainCreationHolder(const AppDomainCreationHolder<AppDomainType>&) {}

protected:
    AppDomainType* m_pDomain;
    BOOL       m_bAcquired;
    void ReleaseAppDomainDuringCreation()
    {
        CONTRACTL
        {
            NOTHROW;
            WRAPPER(GC_TRIGGERS);
            PRECONDITION(m_bAcquired);
            PRECONDITION(CheckPointer(m_pDomain));
        }
        CONTRACTL_END;

        if (m_pDomain->NotReadyForManagedCode())
        {
            m_pDomain->Release();
        }
        else
        {
            STRESS_LOG2 (LF_APPDOMAIN, LL_INFO100, "Unload domain during creation [%d] %p\n", m_pDomain->GetId().m_dwId, m_pDomain);
            SystemDomain::MakeUnloadable(m_pDomain);
#ifdef _DEBUG
            DWORD hostTestADUnload = g_pConfig->GetHostTestADUnload();
            m_pDomain->EnableADUnloadWorker(hostTestADUnload != 2?EEPolicy::ADU_Safe:EEPolicy::ADU_Rude);
#else
            m_pDomain->EnableADUnloadWorker(EEPolicy::ADU_Safe);
#endif
        }
    };
    
public:
    AppDomainCreationHolder() 
    {
        m_pDomain=NULL;
        m_bAcquired=FALSE;
    };
    ~AppDomainCreationHolder()
    {
        if (m_bAcquired) 
        {
            Release();
        }
    };
    void Assign(AppDomainType* pDomain)
    {
        if(m_bAcquired)
            Release();
        m_pDomain=pDomain;
        if(m_pDomain)
        {
            AppDomain::RefTakerAcquire(m_pDomain);
#ifdef _DEBUG
            m_pDomain->IncCreationCount();
#endif // _DEBUG
        }
        m_bAcquired=TRUE;
    };
    
    void Release()
    {
        _ASSERTE(m_bAcquired);
        if(m_pDomain)
        {
#ifdef _DEBUG
            m_pDomain->DecCreationCount();
#endif // _DEBUG
            if(!m_pDomain->IsDefaultDomain())
                ReleaseAppDomainDuringCreation();
            AppDomain::RefTakerRelease(m_pDomain);
        };
        m_bAcquired=FALSE;
    };

    AppDomainType* Extract()
    {
        _ASSERTE(m_bAcquired);
        if(m_pDomain)
        {
#ifdef _DEBUG
            m_pDomain->DecCreationCount();
#endif // _DEBUG
            AppDomain::RefTakerRelease(m_pDomain);
        }
        m_bAcquired=FALSE;
        return m_pDomain;
    };

    AppDomainType* operator ->()
    {
        _ASSERTE(m_bAcquired);
        return m_pDomain;
    }

    operator AppDomainType*()
    {
        _ASSERTE(m_bAcquired);
        return m_pDomain;
    }

    void DoneCreating()
    {
        Extract();
    }
};
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

#endif
