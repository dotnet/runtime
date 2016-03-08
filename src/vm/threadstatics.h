// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
// statics use the DomainLocalBlock and DomainLocalModule structures to allocate space for statics each time
// a module is loaded in an AppDomain.
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

// Defines ObjectHandeList type
#include "specialstatics.h"


typedef DPTR(struct ThreadLocalModule) PTR_ThreadLocalModule;

struct ThreadLocalModule
{
    friend class ClrDataAccess;
    friend class CheckAsmOffsets; 
    friend struct ThreadLocalBlock;

    struct DynamicEntry
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
                SO_TOLERANT;
                SUPPORTS_DAC;
            }
            CONTRACTL_END;

            _ASSERTE(m_pGCStatics != NULL);

            return dac_cast<PTR_BYTE>((PTR_OBJECTREF)((PTRARRAYREF)ObjectFromHandle(m_pGCStatics))->GetDataPtr());
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
        static DWORD GetOffsetOfDataBlob()
        {
            LIMITED_METHOD_CONTRACT;
            return offsetof(DynamicEntry, m_pDataBlob);
        }
    };
    typedef DPTR(DynamicEntry) PTR_DynamicEntry;

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
            SO_TOLERANT;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        _ASSERTE(m_pGCStatics != NULL);

        return (PTR_OBJECTREF)((PTRARRAYREF)ObjectFromHandle(m_pGCStatics))->GetDataPtr();
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
            return GetDynamicEntryGCStaticsBasePointer(pMT->GetModuleDynamicEntryID());
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
            return GetDynamicEntryNonGCStaticsBasePointer(pMT->GetModuleDynamicEntryID());
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

    // These helpers can now return null, as the debugger may do queries on a type
    // before the calls to PopulateClass happen
    inline PTR_BYTE GetDynamicEntryGCStaticsBasePointer(DWORD n)
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
        
        DynamicEntry* pEntry = GetDynamicEntry(n);
        if (!pEntry)
        {
            return NULL;
        }

        return pEntry->GetGCStaticsBasePointer();
    }

    inline PTR_BYTE GetDynamicEntryNonGCStaticsBasePointer(DWORD n)
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

        DynamicEntry* pEntry = GetDynamicEntry(n);
        if (!pEntry)
        {
            return NULL;
        }

        return pEntry->GetNonGCStaticsBasePointer();
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
            SO_INTOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
    
        _ASSERTE(!IsClassInitialized(pMT));
        _ASSERTE(!IsClassInitError(pMT));
        
        SetClassFlags(pMT, ClassInitFlags::INITIALIZED_FLAG);
    }

    void SetClassAllocatedAndInitialized(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;
    
        _ASSERTE(!IsClassInitialized(pMT));
        _ASSERTE(!IsClassInitError(pMT));
    
        SetClassFlags(pMT, ClassInitFlags::ALLOCATECLASS_FLAG | ClassInitFlags::INITIALIZED_FLAG);
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



typedef DPTR(struct TLMTableEntry) PTR_TLMTableEntry;

struct TLMTableEntry
{
    PTR_ThreadLocalModule pTLM;
};


typedef DPTR(struct ThreadLocalBlock) PTR_ThreadLocalBlock;
typedef DPTR(PTR_ThreadLocalBlock) PTR_PTR_ThreadLocalBlock;

struct ThreadLocalBlock
{
    friend class ClrDataAccess;

private:
    PTR_TLMTableEntry   m_pTLMTable;     // Table of ThreadLocalModules
    SIZE_T              m_TLMTableSize;  // Current size of table

    // Each ThreadLocalBlock has its own ThreadStaticHandleTable. The ThreadStaticHandleTable works
    // by allocating Object arrays on the GC heap and keeping them alive with pinning handles.
    //
    // We use the ThreadStaticHandleTable to allocate space for GC thread statics. A GC thread
    // static is thread static that is either a reference type or a value type whose layout
    // contains a pointer to a reference type.

    ThreadStaticHandleTable * m_pThreadStaticHandleTable;

    // Need to keep a list of the pinning handles we've created
    // so they can be cleaned up when the thread dies
    ObjectHandleList          m_PinningHandleList;

public: 

#ifndef DACCESS_COMPILE
    void AddPinningHandleToList(OBJECTHANDLE oh);
    void FreePinningHandles();
    void AllocateThreadStaticHandles(Module * pModule, ThreadLocalModule * pThreadLocalModule);
    OBJECTHANDLE AllocateStaticFieldObjRefPtrs(int nRequested, OBJECTHANDLE* ppLazyAllocate = NULL);
    void InitThreadStaticHandleTable();

    void AllocateThreadStaticBoxes(MethodTable* pMT);
#endif

public: // used by code generators
    static SIZE_T GetOffsetOfModuleSlotsPointer() { return offsetof(ThreadLocalBlock, m_pTLMTable); }

public:

#ifndef DACCESS_COMPILE
    ThreadLocalBlock()
      : m_pTLMTable(NULL), m_TLMTableSize(0), m_pThreadStaticHandleTable(NULL) {}

    void    FreeTLM(SIZE_T i);

    void    FreeTable();

    void    EnsureModuleIndex(ModuleIndex index);

#endif

    void SetModuleSlot(ModuleIndex index, PTR_ThreadLocalModule pLocalModule);

    FORCEINLINE PTR_ThreadLocalModule GetTLMIfExists(ModuleIndex index)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        if (index.m_dwIndex >= m_TLMTableSize)
            return NULL;

        return m_pTLMTable[index.m_dwIndex].pTLM;
    }

    FORCEINLINE PTR_ThreadLocalModule GetTLMIfExists(MethodTable* pMT)
    {
        WRAPPER_NO_CONTRACT;
        ModuleIndex index = pMT->GetModuleForStatics()->GetModuleIndex();
        return GetTLMIfExists(index);
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};




class ThreadStatics
{
  public:

#ifndef DACCESS_COMPILE
    static PTR_ThreadLocalBlock AllocateTLB(PTR_Thread pThread, ADIndex index);
    static PTR_ThreadLocalModule AllocateTLM(Module * pModule);
    static PTR_ThreadLocalModule AllocateAndInitTLM(ModuleIndex index, PTR_ThreadLocalBlock pThreadLocalBlock, Module * pModule);

    static PTR_ThreadLocalModule GetTLM(ModuleIndex index, Module * pModule);
    static PTR_ThreadLocalModule GetTLM(MethodTable * pMT);
#endif
    static PTR_ThreadLocalBlock GetTLBIfExists(PTR_Thread pThread, ADIndex index);

#ifndef DACCESS_COMPILE
    // Grows the TLB table
    inline static void EnsureADIndex(PTR_Thread pThread, ADIndex index)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
        SIZE_T size = max(16, pThread->m_TLBTableSize);
        while (size <= index.m_dwIndex)
        {
            size *= 2;
        }

        // If this allocation fails, we will throw. If it succeeds,
        // then we are good to go
        PTR_ThreadLocalBlock * pNewTLBTable = (PTR_ThreadLocalBlock *)(void*)new PTR_ThreadLocalBlock [size];

        // Zero out the new TLB table
        memset(pNewTLBTable, 0, sizeof(PTR_ThreadLocalBlock) * size);

        if (pThread->m_pTLBTable != NULL)
        {
            memcpy(pNewTLBTable, pThread->m_pTLBTable, sizeof(PTR_ThreadLocalBlock) * pThread->m_TLBTableSize);
        }

        PTR_ThreadLocalBlock * pOldTLBTable = pThread->m_pTLBTable;

        pThread->m_pTLBTable = pNewTLBTable;
        pThread->m_TLBTableSize = size;

        delete pOldTLBTable;
    }

    FORCEINLINE static PTR_ThreadLocalBlock GetCurrentTLBIfExists()
    {
        // Get the current thread
        PTR_Thread pThread = GetThread();
    
        // If the current TLB pointer is NULL, search the TLB table
        if (pThread->m_pThreadLocalBlock == NULL)
        {
            ADIndex index = pThread->GetDomain()->GetIndex();
            pThread->m_pThreadLocalBlock = ThreadStatics::GetTLBIfExists(pThread, index);
        }

        return pThread->m_pThreadLocalBlock;
    }
#endif

    FORCEINLINE static PTR_ThreadLocalBlock GetCurrentTLBIfExists(PTR_Thread pThread, PTR_AppDomain pDomain)
    {
        SUPPORTS_DAC;

        // If the current TLB pointer is NULL, search the TLB table
        PTR_ThreadLocalBlock pTLB = pThread->m_pThreadLocalBlock;
        if (pTLB == NULL)
        {
            if (pDomain == NULL)
            {
                pDomain = pThread->GetDomain();
            }

            pTLB = ThreadStatics::GetTLBIfExists(pThread, pDomain->GetIndex());

            // Update the ThreadLocalBlock pointer,
            // but only on non-DAC builds
#ifndef DACCESS_COMPILE
            pThread->m_pThreadLocalBlock = pTLB;
#endif
        }

        return pTLB;
    }

#ifndef DACCESS_COMPILE
    FORCEINLINE static PTR_ThreadLocalBlock GetCurrentTLB()
    {
        // Get the current thread
        Thread * pThread = GetThread();
    
        // If the current TLB pointer is NULL, search the TLB table
        if (pThread->m_pThreadLocalBlock == NULL)
        {
            AppDomain * pDomain = pThread->GetDomain();
            pThread->m_pThreadLocalBlock = ThreadStatics::GetTLBIfExists(pThread, pDomain->GetIndex());
            if (pThread->m_pThreadLocalBlock == NULL)
            {
                // Allocate the new ThreadLocalBlock.
                // If the allocation fails this will throw.
                return ThreadStatics::AllocateTLB(pThread, pDomain->GetIndex());
            }
        }

        return pThread->m_pThreadLocalBlock;       
    }

    FORCEINLINE static PTR_ThreadLocalModule GetTLMIfExists(ModuleIndex index)
    {
        // Get the current ThreadLocalBlock
        PTR_ThreadLocalBlock pThreadLocalBlock = GetCurrentTLBIfExists();
        if (pThreadLocalBlock == NULL)
            return NULL;

        // Get the TLM from the ThreadLocalBlock's table
        return pThreadLocalBlock->GetTLMIfExists(index);
    }

    FORCEINLINE static PTR_ThreadLocalModule GetTLMIfExists(MethodTable * pMT)
    {
        // Get the current ThreadLocalBlock
        PTR_ThreadLocalBlock pThreadLocalBlock = GetCurrentTLBIfExists();
        if (pThreadLocalBlock == NULL)
            return NULL;

        // Get the TLM from the ThreadLocalBlock's table
        return pThreadLocalBlock->GetTLMIfExists(pMT);
    }
#endif

};


#endif
