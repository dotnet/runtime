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
#include "arraylist.h"
#include "comreflectioncache.hpp"
#include "comutilnative.h"
#include "domainfile.h"
#include "objectlist.h"
#include "fptrstubs.h"
#include "testhookmgr.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"
#include "../binder/inc/applicationcontext.hpp"
#include "rejit.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "clrprivbinderwinrt.h"
#include "..\md\winmd\inc\adapter.h"
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP

#include "appxutil.h"

#include "tieredcompilation.h"

#include "codeversion.h"

class BaseDomain;
class SystemDomain;
class AppDomain;
class CompilationDomain;
class AppDomainEnum;
class AssemblySink;
class EEMarshalingData;
class GlobalStringLiteralMap;
class StringLiteralMap;
class MngStdInterfacesInfo;
class DomainModule;
class DomainAssembly;
struct InteropMethodTableData;
class LoadLevelLimiter;
class TypeEquivalenceHashTable;
class StringArrayList;

#ifdef FEATURE_COMINTEROP
class ComCallWrapperCache;
struct SimpleComCallWrapper;
class RCWRefCache;
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

#define OFFSETOF__DomainLocalModule__m_pDataBlob_                    (6 * TARGET_POINTER_SIZE)
#ifdef FEATURE_64BIT_ALIGNMENT
#define OFFSETOF__DomainLocalModule__NormalDynamicEntry__m_pDataBlob (TARGET_POINTER_SIZE /* m_pGCStatics */ + TARGET_POINTER_SIZE /* m_padding */)
#else
#define OFFSETOF__DomainLocalModule__NormalDynamicEntry__m_pDataBlob TARGET_POINTER_SIZE /* m_pGCStatics */
#endif

#ifdef _MSC_VER
#pragma warning(pop)
#endif


// The large heap handle bucket class is used to contain handles allocated
// from an array contained in the large heap.
class LargeHeapHandleBucket
{
public:
    // Constructor and desctructor.
    LargeHeapHandleBucket(LargeHeapHandleBucket *pNext, DWORD Size, BaseDomain *pDomain);
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
    OBJECTREF* AllocateHandles(DWORD nRequested);

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
            if (((PEFile *)pEntry->m_data)->Equals(pFile))
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

public:
    static FileLoadLock *Create(PEFileListLock *pLock, PEFile *pFile, DomainFile *pDomainFile);

    ~FileLoadLock();
    DomainFile *GetDomainFile();
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

    BOOL IsSharedDomain() { LIMITED_METHOD_DAC_CONTRACT; return FALSE; }
    BOOL IsDefaultDomain() { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }

    PTR_LoaderAllocator GetLoaderAllocator();
    virtual PTR_AppDomain AsAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(!"Not an AppDomain");
        return NULL;
    }

#ifdef FEATURE_COMINTEROP
    //****************************************************************************************
    //
    // This will look up interop data for a method table
    //

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

    STRINGREF *IsStringInterned(STRINGREF *pString);
    STRINGREF *GetOrInternString(STRINGREF *pString);

    // Returns an array of OBJECTREF* that can be used to store domain specific data.
    // Statics and reflection info (Types, MemberInfo,..) are stored this way
    // If ppLazyAllocate != 0, allocation will only take place if *ppLazyAllocate != 0 (and the allocation
    // will be properly serialized)
    OBJECTREF *AllocateObjRefPtrsInLargeTable(int nRequested, OBJECTREF** ppLazyAllocate = NULL);

#ifdef FEATURE_PREJIT
    // Ensures that the file for logging profile data is open (we only open it once)
    // return false on failure
    static BOOL EnsureNGenLogFileOpen();
#endif

    //****************************************************************************************
    // Handles

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
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

#ifdef FEATURE_COMINTEROP
    OBJECTHANDLE CreateRefcountedHandle(OBJECTREF object)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateRefcountedHandle(m_handleStore, object);
    }

    OBJECTHANDLE CreateWinRTWeakHandle(OBJECTREF object, IWeakReference* pWinRTWeakReference)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateWinRTWeakHandle(m_handleStore, object, pWinRTWeakReference);
    }
#endif // FEATURE_COMINTEROP

    OBJECTHANDLE CreateVariableHandle(OBJECTREF object, UINT type)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateVariableHandle(m_handleStore, object, type);
    }

    OBJECTHANDLE CreateDependentHandle(OBJECTREF primary, OBJECTREF secondary)
    {
        WRAPPER_NO_CONTRACT;
        return ::CreateDependentHandle(m_handleStore, primary, secondary);
    }

#endif // DACCESS_COMPILE && !CROSSGEN_COMPILE

    IUnknown *GetFusionContext() {LIMITED_METHOD_CONTRACT;  return m_pFusionContext; }
    
    CLRPrivBinderCoreCLR *GetTPABinderContext() {LIMITED_METHOD_CONTRACT;  return m_pTPABinderContext; }


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
    JitListLock      m_JITLock;
    ListLock         m_ILStubGenLock;

    // Fusion context, used for adding assemblies to the is domain. It defines
    // fusion properties for finding assemblyies such as SharedBinPath,
    // PrivateBinPath, Application Directory, etc.
    IUnknown *m_pFusionContext; // Current binding context for the domain

    CLRPrivBinderCoreCLR *m_pTPABinderContext; // Reference to the binding context that holds TPA list details

    IGCHandleStore* m_handleStore;

    // The large heap handle table.
    LargeHeapHandleTable        *m_pLargeHeapHandleTable;

    // The large heap handle table critical section.
    CrstExplicitInit             m_LargeHeapHandleTableCrst;

#ifdef FEATURE_COMINTEROP
    // Information regarding the managed standard interfaces.
    MngStdInterfacesInfo        *m_pMngStdInterfacesInfo;
    
    // WinRT binder
    PTR_CLRPrivBinderWinRT m_pWinRtBinder;
#endif // FEATURE_COMINTEROP

    // Protects allocation of slot IDs for thread statics
    static CrstStatic   m_SpecialStaticsCrst;

public:
    // Only call this routine when you can guarantee there are no
    // loads in progress.
    void ClearFusionContext();

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
#ifndef DACCESS_COMPILE
    void RemoveTypesFromTypeIDMap(LoaderAllocator* pLoaderAllocator);
#endif // DACCESS_COMPILE

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

#ifdef FEATURE_CODE_VERSIONING
private:
    CodeVersionManager m_codeVersionManager;

public:
    CodeVersionManager* GetCodeVersionManager() { return &m_codeVersionManager; }
#endif //FEATURE_CODE_VERSIONING

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
                                        // See comment at code:DomainFile::IsAvailableToProfilers

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
    SString location;
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
    friend class AssemblySink;
    friend class AppDomainNative;
    friend class AssemblyNative;
    friend class AssemblySpec;
    friend class ClassLoader;
    friend class ThreadNative;
    friend class RCWCache;
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

#if defined(FEATURE_COMINTEROP)
    HRESULT SetWinrtApplicationContext(LPCWSTR pwzAppLocalWinMD);
#endif // FEATURE_COMINTEROP

    bool MustForceTrivialWaitOperations();
    void SetForceTrivialWaitOperations();

    //****************************************************************************************
    //
    // Stop deletes all the assemblies but does not remove other resources like
    // the critical sections
    void Stop();

    // final assembly cleanup
    void ShutdownFreeLoaderAllocators();
    
    void ReleaseFiles();
    
    virtual BOOL IsAppDomain() { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
    virtual PTR_AppDomain AsAppDomain() { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_AppDomain>(this); }

    OBJECTREF GetRawExposedObject() { LIMITED_METHOD_CONTRACT; return NULL; }
    OBJECTHANDLE GetRawExposedObjectHandleForDebugger() { LIMITED_METHOD_DAC_CONTRACT; return NULL; }

#ifdef FEATURE_COMINTEROP
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

private:
    struct NativeImageDependenciesEntry
    {
        BaseAssemblySpec m_AssemblySpec;
        GUID m_guidMVID;
    };

    class NativeImageDependenciesTraits : public DeleteElementsOnDestructSHashTraits<DefaultSHashTraits<NativeImageDependenciesEntry *> >
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
    BOOL RemoveNativeImageDependency(AssemblySpec* pSpec);

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


    Assembly *LoadAssembly(AssemblySpec* pIdentity,
                           PEAssembly *pFile,
                           FileLoadLevel targetLevel);

    // this function does not provide caching, you must use LoadDomainAssembly
    // unless the call is guaranteed to succeed or you don't need the caching 
    // (e.g. if you will FailFast or tear down the AppDomain anyway)
    // The main point that you should not bypass caching if you might try to load the same file again, 
    // resulting in multiple DomainAssembly objects that share the same PEAssembly for ngen image 
    //which is violating our internal assumptions
    DomainAssembly *LoadDomainAssemblyInternal( AssemblySpec* pIdentity,
                                                PEAssembly *pFile,
                                                FileLoadLevel targetLevel);

    DomainAssembly *LoadDomainAssembly( AssemblySpec* pIdentity,
                                        PEAssembly *pFile,
                                        FileLoadLevel targetLevel);


    CHECK CheckValidModule(Module *pModule);

    // private:
    void LoadSystemAssemblies();

    DomainFile *LoadDomainFile(FileLoadLock *pLock,
                               FileLoadLevel targetLevel);

    void TryIncrementalLoad(DomainFile *pFile, FileLoadLevel workLevel, FileLoadLockHolder &lockHolder);

    Assembly *LoadAssemblyHelper(LPCWSTR wszAssembly,
                                 LPCWSTR wszCodeBase);

#ifndef DACCESS_COMPILE // needs AssemblySpec

    void GetCacheAssemblyList(SetSHash<PTR_DomainAssembly>& assemblyList);

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
    BOOL RemoveFileFromCache(PEAssembly *pFile);

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
        BOOL fThrowOnFileNotFound,
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
        PEAssembly **      ppAssembly) DAC_EMPTY_RET(S_OK);


    PEAssembly *TryResolveAssembly(AssemblySpec *pSpec);

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
    //****************************************************************************************
    //
    // Uses the first assembly to add an application base to the Context. This is done
    // in a lazy fashion so executables do not take the perf hit unless the load other
    // assemblies
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
#endif // DACCESS_COMPILE

    void              EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

    void SetupSharedStatics();

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
#endif // FEATURE_COMINTEROP

    //****************************************************************************************
    // Get the proxy for this app domain

    TPIndex GetTPIndex()
    {
        LIMITED_METHOD_CONTRACT;
        return m_tpIndex;
    }

    IUnknown *CreateFusionContext();

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

    void SetCompilationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        m_dwFlags |= COMPILATION_DOMAIN;
    }

    BOOL IsCompilationDomain();

    PTR_CompilationDomain ToCompilationDomain()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsCompilationDomain());
        return dac_cast<PTR_CompilationDomain>(this);
    }

    static void ExceptionUnwind(Frame *pFrame);

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

        return m_Stage >= STAGE_ACTIVE;
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
        return m_Stage > STAGE_CREATING;
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
    BOOL NotReadyForManagedCode()
    {
        LIMITED_METHOD_CONTRACT;

        return m_Stage < STAGE_READYFORMANAGEDCODE;
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

private:
    size_t EstimateSize();
    EEClassFactoryInfoHashTable* SetupClassFactHash();
#ifdef FEATURE_COMINTEROP
    DispIDCache* SetupRefDispIDCache();
#endif // FEATURE_COMINTEROP

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
        TESTHOOKCALL(AppDomainStageChanged(DefaultADID,m_Stage,stage));
        Stage lastStage=m_Stage;
        while (lastStage !=stage) 
            lastStage = (Stage)FastInterlockCompareExchange((LONG*)&m_Stage,stage,lastStage);
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

    void UnlinkClass(MethodTable *pMT);

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
    // The one and only AppDomain
    SPTR_DECL(AppDomain, m_pTheAppDomain);

    SString         m_friendlyName;
    PTR_Assembly    m_pRootAssembly;

    // General purpose flags.
    DWORD           m_dwFlags;

    // When an application domain is created the ref count is artifically incremented
    // by one. For it to hit zero an explicit close must have happened.
    LONG        m_cRef;                    // Ref count.

    // Hash table that maps a clsid to a type
    PtrHashMap          m_clsidHash;

#ifdef FEATURE_COMINTEROP
    // Hash table that maps WinRT class names to MethodTables.
    PTR_NameToTypeMapTable m_pNameToTypeMap;
    UINT                m_vNameToTypeMapVersion;

    UINT                m_nEpoch; // incremented each time m_pNameToTypeMap is enumerated

    // Hash table that remembers the last cached WinRT factory object per type per appdomain.
    WinRTFactoryCache   *m_pWinRTFactoryCache;

    // this cache stores the RCWs in this domain
    RCWCache *m_pRCWCache;

    // this cache stores the RCW -> CCW references in this domain
    RCWRefCache *m_pRCWRefCache;
#endif // FEATURE_COMINTEROP

    // The thread-pool index of this app domain among existing app domains (starting from 1)
    TPIndex m_tpIndex;

    Volatile<Stage> m_Stage;

    ArrayList        m_failedAssemblies;

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

public:

    enum {
        CONTEXT_INITIALIZED =               0x0001,
        LOAD_SYSTEM_ASSEMBLY_EVENT_SENT =   0x0040,
        COMPILATION_DOMAIN =                0x0400, // Are we ngenning?
        IGNORE_UNHANDLED_EXCEPTIONS =      0x10000, // AppDomain was created using the APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS flag
    };

    AssemblySpecBindingCache  m_AssemblyCache;
    DomainAssemblyCache       m_UnmanagedCache;
    size_t                    m_MemoryPressure;

    ArrayList m_NativeDllSearchDirectories;
    bool m_ForceTrivialWaitOperations;

public:

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

#ifdef FEATURE_COMINTEROP

private:

#endif //FEATURE_COMINTEROP

public:

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
        
        static element_t Null() { return NULL; }
        static element_t Deleted() { return (element_t)(TADDR)-1; }
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

typedef VPTR(class SystemDomain) PTR_SystemDomain;

class SystemDomain : public BaseDomain
{
    friend class AppDomainNative;
    friend class AppDomainIterator;
    friend class UnsafeAppDomainIterator;
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

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(CROSSGEN_COMPILE)
    static Thread::ApartmentState GetEntryPointThreadAptState(IMDInternalImport* pScope, mdMethodDef mdMethod);
    static void SetThreadAptState(Thread::ApartmentState state);
#endif

    //****************************************************************************************
    //
    // Use an already exising & inited Application Domain (e.g. a subclass).
    static void LoadDomain(AppDomain     *pDomain);

    //****************************************************************************************
    // Methods used to get the callers module and hence assembly and app domain.

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
        if (path.EqualsCaseInsensitive(m_BaseLibrary))
            return TRUE;

        // Or, it might be the GAC location of mscorlib
        if (System()->SystemAssembly() != NULL
            && path.EqualsCaseInsensitive(System()->SystemAssembly()->GetManifestFile()->GetPath()))
            return TRUE;

        return FALSE;
    }

    BOOL IsBaseLibrarySatellite(SString &path)
    {
        WRAPPER_NO_CONTRACT;

        // See if it is the installation path to mscorlib.resources
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

        m_pDelayedUnloadListOfLoaderAllocators=NULL;

        m_GlobalAllocator.Init(this);
    }
#endif

    PTR_PEAssembly  m_pSystemFile;      // Single assembly (here for quicker reference);
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
// In CoreCLR, this iterator doesn't use a list as there is at most 1 AppDomain, and instead will find the only AppDomain, or not.
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
        m_iterationCount = 0;
        m_pCurrent = NULL;
    }

    BOOL Next()
    {
        WRAPPER_NO_CONTRACT;

        if (m_iterationCount == 0)
        {
            m_iterationCount++;
            m_pCurrent = AppDomain::GetCurrentDomain();
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

    int                 m_iterationCount;
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

#include "comreflectioncache.inl"

#endif
