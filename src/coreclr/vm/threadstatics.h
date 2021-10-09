// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ThreadStatics.h
//

//
//
// Classes can contain instance fields and statics fields. In addition to regular statics, .NET offers
// several types of special statics. In IL, thread static fields are marked with the ThreadStaticAttribute,
// distinguishing them from regular statics and other types of special statics. A thread static field is
// not shared between threads. Each executing thread has a separate instance of the field, and independently
// sets and gets values for that field.
//
// This implementation of thread statics closely parallels the implementation for regular statics. Regular
// statics use the DomainLocalModule structure to allocate space for statics.
//

//

#ifndef __threadstatics_h__
#define __threadstatics_h__

#include "vars.hpp"
#include "util.hpp"

#include "appdomain.hpp"
#include "field.h"
#include "methodtable.h"
#include "threads.h"
#include "spinlock.h"

// Defines ObjectHandeList type
#include "specialstatics.h"


typedef DPTR(struct ThreadLocalModule) PTR_ThreadLocalModule;

struct ThreadLocalModule
{
    friend class ClrDataAccess;
    friend class CheckAsmOffsets;
    friend struct ThreadLocalBlock;

    // After these macros complete, they may have returned an interior pointer into a gc object. This pointer will have been cast to a byte pointer
    // It is critically important that no GC is allowed to occur before this pointer is used.
#define GET_DYNAMICENTRY_GCTHREADSTATICS_BASEPOINTER(pLoaderAllocator, dynamicClassInfoParam, pGCStatics) \
    {\
        ThreadLocalModule::PTR_DynamicClassInfo dynamicClassInfo = dac_cast<ThreadLocalModule::PTR_DynamicClassInfo>(dynamicClassInfoParam);\
        ThreadLocalModule::PTR_DynamicEntry pDynamicEntry = dac_cast<ThreadLocalModule::PTR_DynamicEntry>((ThreadLocalModule::DynamicEntry*)dynamicClassInfo->m_pDynamicEntry); \
        if ((dynamicClassInfo->m_dwFlags) & ClassInitFlags::COLLECTIBLE_FLAG) \
        {\
            PTRARRAYREF objArray;\
            objArray = (PTRARRAYREF)pLoaderAllocator->GetHandleValueFastCannotFailType2( \
                                        (dac_cast<ThreadLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hGCStatics);\
            *(pGCStatics) = dac_cast<PTR_BYTE>(PTR_READ(PTR_TO_TADDR(OBJECTREFToObject( objArray )) + offsetof(PtrArray, m_Array), objArray->GetNumComponents() * sizeof(void*))) ;\
        }\
        else\
        {\
            *(pGCStatics) = (dac_cast<ThreadLocalModule::PTR_NormalDynamicEntry>(pDynamicEntry))->GetGCStaticsBasePointer();\
        }\
    }\

#define GET_DYNAMICENTRY_NONGCTHREADSTATICS_BASEPOINTER(pLoaderAllocator, dynamicClassInfoParam, pNonGCStatics) \
    {\
        ThreadLocalModule::PTR_DynamicClassInfo dynamicClassInfo = dac_cast<ThreadLocalModule::PTR_DynamicClassInfo>(dynamicClassInfoParam);\
        ThreadLocalModule::PTR_DynamicEntry pDynamicEntry = dac_cast<ThreadLocalModule::PTR_DynamicEntry>((ThreadLocalModule::DynamicEntry*)(dynamicClassInfo)->m_pDynamicEntry); \
        if (((dynamicClassInfo)->m_dwFlags) & ClassInitFlags::COLLECTIBLE_FLAG) \
        {\
            if ((dac_cast<ThreadLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hNonGCStatics != 0) \
            { \
                U1ARRAYREF objArray;\
                objArray = (U1ARRAYREF)pLoaderAllocator->GetHandleValueFastCannotFailType2( \
                                            (dac_cast<ThreadLocalModule::PTR_CollectibleDynamicEntry>(pDynamicEntry))->m_hNonGCStatics);\
                *(pNonGCStatics) = dac_cast<PTR_BYTE>(PTR_READ( \
                        PTR_TO_TADDR(OBJECTREFToObject( objArray )) + sizeof(ArrayBase) - ThreadLocalModule::DynamicEntry::GetOffsetOfDataBlob(), \
                            objArray->GetNumComponents() * (DWORD)objArray->GetComponentSize() + ThreadLocalModule::DynamicEntry::GetOffsetOfDataBlob())); \
            } else (*pNonGCStatics) = NULL; \
        }\
        else\
        {\
            *(pNonGCStatics) = dac_cast<ThreadLocalModule::PTR_NormalDynamicEntry>(pDynamicEntry)->GetNonGCStaticsBasePointer();\
        }\
    }\

    struct DynamicEntry
    {
        static DWORD GetOffsetOfDataBlob();
    };
    typedef DPTR(DynamicEntry) PTR_DynamicEntry;

    struct CollectibleDynamicEntry : public DynamicEntry
    {
        LOADERHANDLE        m_hGCStatics;
        LOADERHANDLE        m_hNonGCStatics;
        PTR_LoaderAllocator m_pLoaderAllocator;
    };
    typedef DPTR(CollectibleDynamicEntry) PTR_CollectibleDynamicEntry;

    struct NormalDynamicEntry : public DynamicEntry
    {
        OBJECTHANDLE    m_pGCStatics;
#ifdef FEATURE_64BIT_ALIGNMENT
        // Padding to make m_pDataBlob aligned at MAX_PRIMITIVE_FIELD_SIZE.
        // code:MethodTableBuilder::PlaceThreadStaticFields assumes that the start of the data blob is aligned
        SIZE_T          m_padding;
#endif
        BYTE            m_pDataBlob[0];

        inline PTR_BYTE GetGCStaticsBasePointer()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                SUPPORTS_DAC;
            }
            CONTRACTL_END;

            _ASSERTE(m_pGCStatics != NULL);

            return dac_cast<PTR_BYTE>(((PTRARRAYREF)ObjectFromHandle(m_pGCStatics))->GetDataPtr());
        }
        inline PTR_BYTE GetGCStaticsBaseHandle()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            return dac_cast<PTR_BYTE>(m_pGCStatics);
        }
        inline PTR_BYTE GetNonGCStaticsBasePointer()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            return dac_cast<PTR_BYTE>(this);
        }
    };
    typedef DPTR(NormalDynamicEntry) PTR_NormalDynamicEntry;

    struct DynamicClassInfo
    {
        PTR_DynamicEntry  m_pDynamicEntry;
        DWORD             m_dwFlags;
    };
    typedef DPTR(DynamicClassInfo) PTR_DynamicClassInfo;

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

    static SIZE_T GetOffsetOfDataBlob() { return offsetof(ThreadLocalModule, m_pDataBlob); }
    static SIZE_T GetOffsetOfGCStaticHandle() { return offsetof(ThreadLocalModule, m_pGCStatics); }

    inline PTR_OBJECTREF GetPrecomputedGCStaticsBasePointer()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        _ASSERTE(m_pGCStatics != NULL);

        return ((PTRARRAYREF)ObjectFromHandle(m_pGCStatics))->GetDataPtr();
    }

    inline OBJECTHANDLE GetPrecomputedGCStaticsBaseHandle()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return m_pGCStatics;
    }

    inline OBJECTHANDLE * GetPrecomputedGCStaticsBaseHandleAddress()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return &m_pGCStatics;
    }

    // Returns bytes so we can add offsets
    inline PTR_BYTE GetGCStaticsBasePointer(MethodTable * pMT)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        if (pMT->IsDynamicStatics())
        {
            return GetDynamicEntryGCStaticsBasePointer(pMT->GetModuleDynamicEntryID(), pMT->GetLoaderAllocator());
        }
        else
        {
            return dac_cast<PTR_BYTE>(GetPrecomputedGCStaticsBasePointer());
        }
    }

    inline PTR_BYTE GetNonGCStaticsBasePointer(MethodTable * pMT)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        if (pMT->IsDynamicStatics())
        {
            return GetDynamicEntryNonGCStaticsBasePointer(pMT->GetModuleDynamicEntryID(), pMT->GetLoaderAllocator());
        }
        else
        {
            return dac_cast<PTR_BYTE>(this);
        }
    }

    inline DynamicEntry* GetDynamicEntry(DWORD n)
    {
        LIMITED_METHOD_CONTRACT
        SUPPORTS_DAC;
        _ASSERTE(m_pDynamicClassTable && m_aDynamicEntries > n);
        DynamicEntry* pEntry = m_pDynamicClassTable[n].m_pDynamicEntry;

        return pEntry;
    }

    inline DynamicClassInfo* GetDynamicClassInfo(DWORD n)
    {
        LIMITED_METHOD_CONTRACT
        SUPPORTS_DAC;
        _ASSERTE(m_pDynamicClassTable && m_aDynamicEntries > n);
        dac_cast<PTR_DynamicEntry>(m_pDynamicClassTable[n].m_pDynamicEntry);

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
            MODE_ANY;
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

        GET_DYNAMICENTRY_GCTHREADSTATICS_BASEPOINTER(pLoaderAllocator, pClassInfo, &retval);

        return retval;
    }

    inline PTR_BYTE GetDynamicEntryNonGCStaticsBasePointer(DWORD n, PTR_LoaderAllocator pLoaderAllocator)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
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

        GET_DYNAMICENTRY_NONGCTHREADSTATICS_BASEPOINTER(pLoaderAllocator, pClassInfo, &retval);

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

        _ASSERTE(m_pDynamicClassTable != NULL);
        PTR_DynamicClassInfo pDynamicClassInfo = (PTR_DynamicClassInfo)(m_pDynamicClassTable + n);

        // ClassInitFlags::INITIALIZED_FLAG is set last, it needs to be checked first
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

    void SetClassInitialized(MethodTable* pMT)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        _ASSERTE(!IsClassInitialized(pMT));
        _ASSERTE(!IsClassInitError(pMT));

        SetClassFlags(pMT, ClassInitFlags::INITIALIZED_FLAG);
    }

    void SetClassAllocated(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;

        SetClassFlags(pMT, ClassInitFlags::ALLOCATECLASS_FLAG);
    }

    void SetClassInitError(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;

        SetClassFlags(pMT, ClassInitFlags::ERROR_FLAG);
    }

#ifndef DACCESS_COMPILE

    void    EnsureDynamicClassIndex(DWORD dwID);

    void    AllocateDynamicClass(MethodTable *pMT);

    void    PopulateClass(MethodTable *pMT);

#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    static DWORD OffsetOfDataBlob()
    {
        LIMITED_METHOD_CONTRACT;
        return offsetof(ThreadLocalModule, m_pDataBlob);
    }

private:

    void SetClassFlags(MethodTable* pMT, DWORD dwFlags);

    DWORD GetClassFlags(MethodTable* pMT, DWORD iClassIndex);


    PTR_DynamicClassInfo     m_pDynamicClassTable;   // used for generics and reflection.emit in memory
    SIZE_T                   m_aDynamicEntries;      // number of entries in dynamic table
    OBJECTHANDLE             m_pGCStatics;           // Handle to GC statics of the module

    // Note that the static offset calculation in code:Module::BuildStaticsOffsets takes the offset m_pDataBlob
    // into consideration so we do not need any padding to ensure that the start of the data blob is aligned

    BYTE                     m_pDataBlob[0];         // First byte of the statics blob

    // Layout of m_pDataBlob is:
    //              ClassInit bytes (hold flags for cctor run, cctor error, etc)
    //              Non GC Statics

public:
    inline PTR_BYTE GetPrecomputedStaticsClassData()
    {
        LIMITED_METHOD_CONTRACT
        return dac_cast<PTR_BYTE>(this) + offsetof(ThreadLocalModule, m_pDataBlob);
    }

    inline BOOL IsPrecomputedClassInitialized(DWORD classID)
    {
        return GetPrecomputedStaticsClassData()[classID] & ClassInitFlags::INITIALIZED_FLAG;
    }

    void* operator new(size_t) = delete;

    struct ParentModule { PTR_Module pModule; };

    void* operator new(size_t baseSize, ParentModule parentModule)
    {
        size_t size = parentModule.pModule->GetThreadLocalModuleSize();

        _ASSERTE(size >= baseSize);
        _ASSERTE(size >= ThreadLocalModule::OffsetOfDataBlob());

        return ::operator new(size);
    }

#ifndef DACCESS_COMPILE

    FORCEINLINE void EnsureClassAllocated(MethodTable * pMT)
    {
        _ASSERTE(this != NULL);

        // Check if the class needs to be allocated
        if (!IsClassAllocated(pMT))
            PopulateClass(pMT);

        // If PopulateClass() does not throw, then we are guaranteed
        // that the class has been allocated
        _ASSERTE(IsClassAllocated(pMT));
    }

    FORCEINLINE void CheckRunClassInitThrowing(MethodTable * pMT)
    {
        _ASSERTE(this != NULL);

        // Check if the class has been marked as inited in the ThreadLocalModule
        if (!IsClassInitialized(pMT))
        {
            // Ensure that the class has been allocated
            EnsureClassAllocated(pMT);

            // Check if the class has been marked as inited in the DomainLocalModule,
            // if not we must call CheckRunClassInitThrowing()
            if (!pMT->IsClassInited())
                pMT->CheckRunClassInitThrowing();

            // We cannot mark the class as inited in the TLM until it has been marked
            // as inited in the DLM. MethodTable::CheckRunClassInitThrowing() can return
            // before the class constructor has finished running (because of recursion),
            // so we actually need to check if the class has been marked as inited in the
            // DLM before marking it as inited in the TLM.
            if (pMT->IsClassInited())
                SetClassInitialized(pMT);
        }
    }

#endif
};  // struct ThreadLocalModule


#define OFFSETOF__ThreadLocalModule__m_pDataBlob               (3 * TARGET_POINTER_SIZE /* m_pDynamicClassTable + m_aDynamicEntries + m_pGCStatics */)
#ifdef FEATURE_64BIT_ALIGNMENT
#define OFFSETOF__ThreadLocalModule__DynamicEntry__m_pDataBlob (TARGET_POINTER_SIZE /* m_pGCStatics */ + TARGET_POINTER_SIZE /* m_padding */)
#else
#define OFFSETOF__ThreadLocalModule__DynamicEntry__m_pDataBlob TARGET_POINTER_SIZE /* m_pGCStatics */
#endif

typedef DPTR(struct TLMTableEntry) PTR_TLMTableEntry;

struct TLMTableEntry
{
    PTR_ThreadLocalModule pTLM;
};


typedef DPTR(struct ThreadLocalBlock) PTR_ThreadLocalBlock;
typedef DPTR(PTR_ThreadLocalBlock) PTR_PTR_ThreadLocalBlock;

class ThreadStatics
{
  public:

#ifndef DACCESS_COMPILE
    static PTR_ThreadLocalModule AllocateTLM(Module * pModule);
    static PTR_ThreadLocalModule AllocateAndInitTLM(ModuleIndex index, PTR_ThreadLocalBlock pThreadLocalBlock, Module * pModule);

    static PTR_ThreadLocalModule GetTLM(ModuleIndex index, Module * pModule);
    static PTR_ThreadLocalModule GetTLM(MethodTable * pMT);
#endif

    FORCEINLINE static PTR_ThreadLocalBlock GetCurrentTLB(PTR_Thread pThread)
    {
        SUPPORTS_DAC;

        return dac_cast<PTR_ThreadLocalBlock>(PTR_TO_MEMBER_TADDR(Thread, pThread, m_ThreadLocalBlock));
    }

#ifndef DACCESS_COMPILE
    FORCEINLINE static ThreadLocalBlock* GetCurrentTLB()
    {
        // Get the current thread
        Thread * pThread = GetThread();
        return &pThread->m_ThreadLocalBlock;
    }

    FORCEINLINE static ThreadLocalModule* GetTLMIfExists(ModuleIndex index)
    {
        // Get the current ThreadLocalBlock
        PTR_ThreadLocalBlock pThreadLocalBlock = GetCurrentTLB();

        // Get the TLM from the ThreadLocalBlock's table
        return pThreadLocalBlock->GetTLMIfExists(index);
    }

    FORCEINLINE static ThreadLocalModule* GetTLMIfExists(MethodTable * pMT)
    {
        // Get the current ThreadLocalBlock
        ThreadLocalBlock* pThreadLocalBlock = GetCurrentTLB();

        // Get the TLM from the ThreadLocalBlock's table
        return pThreadLocalBlock->GetTLMIfExists(pMT);
    }
#endif

};

/* static */
inline DWORD ThreadLocalModule::DynamicEntry::GetOffsetOfDataBlob()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(DWORD(offsetof(NormalDynamicEntry, m_pDataBlob)) == offsetof(NormalDynamicEntry, m_pDataBlob));
    return (DWORD)offsetof(NormalDynamicEntry, m_pDataBlob);
}

#endif
