// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#ifndef _DYNAMICMETHOD_H_
#define _DYNAMICMETHOD_H_

#include "jitinterface.h"
#include "methodtable.h"
#include <daccess.h>

//---------------------------------------------------------------------------------------
//
// This links together a set of news and release in one object.
// The idea is to have a predefined size allocated up front and used by different calls to new.
// All the allocation will be released at the same time releaseing an instance of this class
// Here is how the object is laid out
// | ptr_to_next_chunk | size_left_in_chunk | data | ... | data
// This is not a particularly efficient allocator but it works well for a small number of allocation
// needed while jitting a method
//
class ChunkAllocator
{
private:
    #define CHUNK_SIZE 64

    BYTE *m_pData;

public:
    ChunkAllocator() : m_pData(NULL) {}

    ~ChunkAllocator();
    void* New(size_t size);
    void Delete();
};

//---------------------------------------------------------------------------------------
//
class DynamicResolver
{
public:
    // Keep in sync with dynamicIlGenerator.cs
    enum SecurityControlFlags
    {
        Default = 0,
        SkipVisibilityChecks = 0x1,
        RestrictedSkipVisibilityChecks = 0x2,
        HasCreationContext = 0x4,
        CanSkipCSEvaluation = 0x8,
    };


    // set up and clean up for jitting
    virtual void FreeCompileTimeState() = 0;
    virtual void GetJitContext(SecurityControlFlags * securityControlFlags,
                               TypeHandle *typeOwner) = 0;
    virtual ChunkAllocator* GetJitMetaHeap() = 0;

    //
    // code info data
    virtual BYTE * GetCodeInfo(
        unsigned *       pCodeSize,
        unsigned *       pStackSize,
        CorInfoOptions * pOptions,
        unsigned *       pEHSize) = 0;
    virtual SigPointer GetLocalSig() = 0;

#ifdef FEATURE_PGO
    virtual PgoManager* volatile* GetDynamicPgoManagerPointer() { return NULL; }
    PgoManager* GetDynamicPgoManager() { return NULL; }
#endif

    //
    // jit interface api
    virtual OBJECTHANDLE ConstructStringLiteral(mdToken metaTok) = 0;
    virtual BOOL IsValidStringRef(mdToken metaTok) = 0;
    virtual STRINGREF GetStringLiteral(mdToken metaTok) = 0;
    virtual void ResolveToken(mdToken token, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD) = 0;
    virtual SigPointer ResolveSignature(mdToken token) = 0;
    virtual SigPointer ResolveSignatureForVarArg(mdToken token) = 0;
    virtual void GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause) = 0;

    virtual MethodDesc * GetDynamicMethod() = 0;
};  // class DynamicResolver

//---------------------------------------------------------------------------------------
//
class StringLiteralEntry;

//---------------------------------------------------------------------------------------
//
struct DynamicStringLiteral
{
    DynamicStringLiteral *  m_pNext;
    StringLiteralEntry *    m_pEntry;
};

//---------------------------------------------------------------------------------------
//
// LCGMethodResolver
//
//  a jit resolver for managed dynamic methods
//
class LCGMethodResolver : public DynamicResolver
{
    friend class DynamicMethodDesc;
    friend class DynamicMethodTable;
    // review this to see whether the EEJitManageris the only thing to worry about
    friend class ExecutionManager;
    friend class EEJitManager;
    friend class HostCodeHeap;
    friend struct ExecutionManager::JumpStubCache;

public:
    void Destroy();

    void FreeCompileTimeState();
    void GetJitContext(SecurityControlFlags * securityControlFlags,
                       TypeHandle * typeOwner);
    ChunkAllocator* GetJitMetaHeap();

    BYTE* GetCodeInfo(unsigned *pCodeSize, unsigned *pStackSize, CorInfoOptions *pOptions, unsigned* pEHSize);
    SigPointer GetLocalSig();

    OBJECTHANDLE ConstructStringLiteral(mdToken metaTok);
    BOOL IsValidStringRef(mdToken metaTok);
    void ResolveToken(mdToken token, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD);
    SigPointer ResolveSignature(mdToken token);
    SigPointer ResolveSignatureForVarArg(mdToken token);
    void GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause);

    MethodDesc* GetDynamicMethod() { LIMITED_METHOD_CONTRACT; return m_pDynamicMethod; }
    OBJECTREF GetManagedResolver();
    void SetManagedResolver(OBJECTHANDLE obj) { LIMITED_METHOD_CONTRACT; m_managedResolver = obj; }
    void * GetRecordCodePointer()  { LIMITED_METHOD_CONTRACT; return m_recordCodePointer; }

    STRINGREF GetStringLiteral(mdToken metaTok);
    STRINGREF * GetOrInternString(STRINGREF *pString);
    void AddToUsedIndCellList(BYTE * indcell);
#ifdef FEATURE_PGO
    PgoManager* volatile* GetDynamicPgoManagerPointer() { return &m_pgoManager; }
    PgoManager* GetDynamicPgoManager() { return m_pgoManager; }
#endif

private:
    void RecycleIndCells();
    void Reset();

    struct IndCellList
    {
        BYTE * indcell;
        IndCellList * pNext;
    };

    DynamicMethodDesc* m_pDynamicMethod;
    OBJECTHANDLE m_managedResolver;
    BYTE *m_Code;
    DWORD m_CodeSize;
    SigPointer m_LocalSig;
    unsigned short m_StackSize;
    CorInfoOptions m_Options;
    unsigned m_EHSize;
    DynamicMethodTable *m_DynamicMethodTable;
    DynamicMethodDesc *m_next;
    void *m_recordCodePointer;
    ChunkAllocator m_jitMetaHeap;
    ChunkAllocator m_jitTempData;
    DynamicStringLiteral* m_DynamicStringLiterals;
    IndCellList * m_UsedIndCellList;    // list to keep track of all the indirection cells used by the jitted code
    ExecutionManager::JumpStubCache * m_pJumpStubCache;

#ifdef FEATURE_PGO
    Volatile<PgoManager*> m_pgoManager;
#endif // FEATURE_PGO
};  // class LCGMethodResolver

//---------------------------------------------------------------------------------------
//
// a DynamicMethodTable is used by the light code generation to lazily allocate methods.
// The methods in this MethodTable are not known up front and their signature is defined
// at runtime
//
class DynamicMethodTable
{
public:
#ifndef DACCESS_COMPILE
    static void CreateDynamicMethodTable(DynamicMethodTable **ppLocation, Module *pModule, AppDomain *pDomain);
#endif

private:
    CrstExplicitInit m_Crst;
    DynamicMethodDesc *m_DynamicMethodList;
    MethodTable *m_pMethodTable;
    Module *m_Module;
    AppDomain *m_pDomain;

    DynamicMethodTable() {WRAPPER_NO_CONTRACT;}

    class LockHolder : public CrstHolder
    {
      public:
        LockHolder(DynamicMethodTable *pDynMT)
            : CrstHolder(&pDynMT->m_Crst)
        {
            WRAPPER_NO_CONTRACT;
        }
    };
    friend class LockHolder;

#ifndef DACCESS_COMPILE
    void MakeMethodTable(AllocMemTracker *pamTracker);
    void AddMethodsToList();

public:
    void Destroy();
    DynamicMethodDesc* GetDynamicMethod(BYTE *psig, DWORD sigSize, PTR_CUTF8 name);
    void LinkMethod(DynamicMethodDesc *pMethod);

#endif

#ifdef _DEBUG
public:
    DWORD m_Used;
#endif

};  // class DynamicMethodTable


//---------------------------------------------------------------------------------------
//
#define HOST_CODEHEAP_SIZE_ALIGN 64

//---------------------------------------------------------------------------------------
//
// Implementation of the CodeHeap for DynamicMethods.
// This CodeHeap uses the host interface VirtualAlloc/Free and allows
// for reclamation of generated code
// (Check the base class - CodeHeap in codeman.h - for comments on the functions)
//
class HostCodeHeap : CodeHeap
{
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#else
    friend class EEJitManager;
#endif

    VPTR_VTABLE_CLASS(HostCodeHeap, CodeHeap)

private:
    // pointer back to jit manager info
    PTR_HeapList m_pHeapList;
    PTR_EEJitManager m_pJitManager;
    // basic allocation data
    PTR_BYTE m_pBaseAddr;
    PTR_BYTE m_pLastAvailableCommittedAddr;
    size_t m_TotalBytesAvailable;
    size_t m_ApproximateLargestBlock;
    // Heap ref count
    DWORD m_AllocationCount;

    // data to track free list and pointers into this heap
    // - on an used block this struct has got a pointer back to the CodeHeap, size and start of aligned allocation
    // - on an unused block (free block) this tracks the size of the block and the pointer to the next non contiguos free block
    struct TrackAllocation {
        union {
            HostCodeHeap *pHeap;
            TrackAllocation *pNext;
        };
        size_t size;
    };
    TrackAllocation *m_pFreeList;

    // used for cleanup. Keep track of the next potential heap to release. Normally NULL
    HostCodeHeap *m_pNextHeapToRelease;
    LoaderAllocator*m_pAllocator;

public:
    static HeapList* CreateCodeHeap(CodeHeapRequestInfo *pInfo, EEJitManager *pJitManager);

private:
    HostCodeHeap(EEJitManager *pJitManager);
    HeapList* InitializeHeapList(CodeHeapRequestInfo *pInfo);
    TrackAllocation* AllocFromFreeList(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs);
    void AddToFreeList(TrackAllocation *pBlockToInsert, TrackAllocation *pBlockToInsertRW);

    TrackAllocation* AllocMemory_NoThrow(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs);

public:
    // Space for header is reserved immediately before. It is not included in size.
    virtual void* AllocMemForCode_NoThrow(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs) DAC_EMPTY_RET(NULL);

    virtual ~HostCodeHeap() DAC_EMPTY();

    LoaderAllocator* GetAllocator() { return m_pAllocator; }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    static TrackAllocation * GetTrackAllocation(TADDR codeStart);
    static HostCodeHeap* GetCodeHeap(TADDR codeStart);

    void DestroyCodeHeap();

protected:
    friend class DynamicMethodDesc;
    friend class LCGMethodResolver;

    void FreeMemForCode(void * codeStart);

#if defined(FEATURE_JIT_PITCHING)
public:
    PTR_EEJitManager GetJitManager() { return m_pJitManager; }
#endif

}; // class HostCodeHeap

//---------------------------------------------------------------------------------------
//
#include "ilstubresolver.h"

inline MethodDesc* GetMethod(CORINFO_METHOD_HANDLE methodHandle)
{
    LIMITED_METHOD_CONTRACT;
    return (MethodDesc*) methodHandle;
}

#ifndef DACCESS_COMPILE

#define CORINFO_MODULE_HANDLE_TYPE_MASK 1

enum CORINFO_MODULE_HANDLE_TYPES
{
    CORINFO_NORMAL_MODULE  = 0,
    CORINFO_DYNAMIC_MODULE,
};

inline bool IsDynamicScope(CORINFO_MODULE_HANDLE module)
{
    LIMITED_METHOD_CONTRACT;
    return (CORINFO_DYNAMIC_MODULE == (((size_t)module) & CORINFO_MODULE_HANDLE_TYPE_MASK));
}

inline CORINFO_MODULE_HANDLE MakeDynamicScope(DynamicResolver* pResolver)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(0 == (((size_t)pResolver) & CORINFO_MODULE_HANDLE_TYPE_MASK));
    return (CORINFO_MODULE_HANDLE)(((size_t)pResolver) | CORINFO_DYNAMIC_MODULE);
}

inline DynamicResolver* GetDynamicResolver(CORINFO_MODULE_HANDLE module)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsDynamicScope(module));
    return (DynamicResolver*)(((size_t)module) & ~((size_t)CORINFO_MODULE_HANDLE_TYPE_MASK));
}

inline Module* GetModule(CORINFO_MODULE_HANDLE scope)
{
    WRAPPER_NO_CONTRACT;

    if (IsDynamicScope(scope))
    {
        return GetDynamicResolver(scope)->GetDynamicMethod()->GetModule();
    }
    else
    {
        return((Module*)scope);
    }
}

inline CORINFO_MODULE_HANDLE GetScopeHandle(Module* module)
{
    LIMITED_METHOD_CONTRACT;
    return(CORINFO_MODULE_HANDLE(module));
}

inline bool IsDynamicMethodHandle(CORINFO_METHOD_HANDLE method)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(NULL != GetMethod(method));
    return GetMethod(method)->IsDynamicMethod();
}

#endif // DACCESS_COMPILE

#endif // _DYNAMICMETHOD_H_
