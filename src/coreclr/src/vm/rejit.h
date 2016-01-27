// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ===========================================================================
// File: REJIT.H
//

// 
// REJIT.H defines the class and structures used to store info about rejitted
// methods.  See comment at top of rejit.cpp for more information on how
// rejit works.
// 
// ===========================================================================
#ifndef _REJIT_H_
#define _REJIT_H_

#include "common.h"
#include "contractimpl.h"
#include "shash.h"
#include "corprof.h"

struct ReJitInfo;
struct SharedReJitInfo;
class ReJitManager;
class MethodDesc;
class ClrDataAccess;

#ifdef FEATURE_REJIT

//---------------------------------------------------------------------------------------
// The CLR's implementation of ICorProfilerFunctionControl, which is passed
// to the profiler.  The profiler calls methods on this to specify the IL and
// codegen flags for a given rejit request.
// 
class ProfilerFunctionControl : public ICorProfilerFunctionControl
{
public:
    ProfilerFunctionControl(LoaderHeap * pHeap);
    ~ProfilerFunctionControl();

    // IUnknown functions
    virtual HRESULT __stdcall QueryInterface(REFIID id, void** pInterface);
    virtual ULONG __stdcall AddRef();
    virtual ULONG __stdcall Release();

    // ICorProfilerFunctionControl functions
    virtual HRESULT __stdcall SetCodegenFlags(DWORD flags);
    virtual HRESULT __stdcall SetILFunctionBody(ULONG cbNewILMethodHeader, LPCBYTE pbNewILMethodHeader);
    virtual HRESULT __stdcall SetILInstrumentedCodeMap(ULONG cILMapEntries, COR_IL_MAP * rgILMapEntries);

    // Accessors
    DWORD GetCodegenFlags();
    LPBYTE GetIL();
    ULONG GetInstrumentedMapEntryCount();
    COR_IL_MAP* GetInstrumentedMapEntries();


protected:
    Volatile<LONG> m_refCount;
    LoaderHeap * m_pHeap;
    DWORD m_dwCodegenFlags;
    ULONG m_cbIL;

    // This pointer will get copied into SharedReJitInfo::m_pbIL and owned there.
    LPBYTE m_pbIL;
    ULONG m_cInstrumentedMapEntries;
    COR_IL_MAP * m_rgInstrumentedMapEntries;
};

//---------------------------------------------------------------------------------------
// Helper base class used by the structures below to enforce that their
// pieces get allocated on the appropriate loader heaps
// 
struct LoaderHeapAllocatedRejitStructure
{
public:
    void * operator new (size_t size, LoaderHeap * pHeap, const NoThrow&);
    void * operator new (size_t size, LoaderHeap * pHeap);
};

//---------------------------------------------------------------------------------------
// One instance of this per rejit request for each mdMethodDef.  Contains IL and
// compilation flags.  This is used primarily as a structure, so most of its
// members are left public.
// 
struct SharedReJitInfo : public LoaderHeapAllocatedRejitStructure
{
private:
    // This determines what to use next as the value of the profiling API's ReJITID.
    static ReJITID s_GlobalReJitId;

public:
    // These represent the various states a SharedReJitInfo can be in.
    enum InternalFlags 
    {
        // The profiler has requested a ReJit, so we've allocated stuff, but we haven't
        // called back to the profiler to get any info or indicate that the ReJit has
        // started. (This Info can be 'reused' for a new ReJit if the
        // profiler calls RequestRejit again before we transition to the next state.)
        kStateRequested = 0x00000000,

        // The CLR has initiated the call to the profiler's GetReJITParameters() callback
        // but it hasn't completed yet. At this point we have to assume the profiler has
        // commited to a specific IL body, even if the CLR doesn't know what it is yet.
        // If the profiler calls RequestRejit we need to allocate a new SharedReJitInfo
        // and call GetReJITParameters() again.
        kStateGettingReJITParameters = 0x00000001,

        // We have asked the profiler about this method via ICorProfilerFunctionControl,
        // and have thus stored the IL and codegen flags the profiler specified. Can only
        // transition to kStateReverted from this state.
        kStateActive    = 0x00000002,

        // The methoddef has been reverted, but not freed yet. It (or its instantiations
        // for generics) *MAY* still be active on the stack someplace or have outstanding
        // memory references.
        kStateReverted  = 0x00000003,


        kStateMask      = 0x0000000F,
    };

    DWORD        m_dwInternalFlags;

    // Data
    LPBYTE       m_pbIL;
    DWORD        m_dwCodegenFlags;
    InstrumentedILOffsetMapping m_instrumentedILMap;

private:
    // This is the value of the profiling API's ReJITID for this particular
    // rejit request.
    const ReJITID m_reJitId;

    // Children
    ReJitInfo *  m_pInfoList;

public:
    // Constructor
    SharedReJitInfo();

    // Intentionally no destructor. SharedReJitInfo and its contents are
    // allocated on a loader heap, so SharedReJitInfo and its contents will be
    // freed when the AD is unloaded.

    // Read-Only Identifcation
    ReJITID GetId() { return m_reJitId; }

    void AddMethod(ReJitInfo * pInfo);

    void RemoveMethod(ReJitInfo * pInfo);

    ReJitInfo * GetMethods() { return m_pInfoList; }

    InternalFlags GetState();
};

//---------------------------------------------------------------------------------------
// One instance of this per rejit request for each MethodDesc*. One SharedReJitInfo
// corresponds to many ReJitInfos, as the SharedReJitInfo tracks the rejit request for
// the methodDef token whereas the ReJitInfo tracks the rejit request for each correspond
// MethodDesc* (instantiation). Points to actual generated code.
// 
// In the case of "pre-rejit" (see comment at top of rejit.cpp), a special "placeholder"
// instance of ReJitInfo is used to "remember" to jmp-stamp a not-yet-jitted-method once
// it finally gets jitted the first time.
// 
// Each ReJitManager contains a hash table of ReJitInfo instances, keyed by
// ReJitManager::m_key.
// 
// This is used primarily as a structure, so most of its members are left public.
//
struct ReJitInfo : public LoaderHeapAllocatedRejitStructure
{
public:
    // The size of the code used to jump stamp the prolog
    static const size_t JumpStubSize =
#if defined(_X86_) || defined(_AMD64_)
        5;
#else
#error "Need to define size of rejit jump-stamp for this platform"
        1;
#endif

    // Used by PtrSHash template as the key for this ReJitInfo.  For regular
    // ReJitInfos, the key is the MethodDesc*.  For placeholder ReJitInfos
    // (to facilitate pre-rejit), the key is (Module*, mdMethodDef).
    struct Key
    {
    public:
        enum
        {
            // The key has not yet had its values initialized
            kUninitialized          = 0x0,
            
            // The key represents a loaded MethodDesc, and is identified by the m_pMD
            // field
            kMethodDesc             = 0x1,
            
            // The key represents a "placeholder" ReJitInfo identified not by loaded
            // MethodDesc, but by the module and metadata token (m_pModule,
            // m_methodDef).
            kMetadataToken          = 0x2,
        };

        // Storage consists of a discriminated union between MethodDesc* or
        // (Module*, mdMethodDef), with the key type as the discriminator.
        union
        {
            TADDR m_pMD;
            TADDR m_pModule;
        };
        ULONG32             m_methodDef : 28;
        ULONG32             m_keyType   : 2;

        Key();
        Key(PTR_MethodDesc pMD);
        Key(PTR_Module pModule, mdMethodDef methodDef);
    };

    static COUNT_T Hash(Key key);

    enum InternalFlags 
    {
        // This ReJitInfo is either a placeholder (identified by module and
        // metadata token, rather than loaded MethodDesc) OR this ReJitInfo is
        // identified by a loaded MethodDesc that has been reverted OR not yet
        // been jump-stamped. In the last case, the time window where this
        // ReJitInfo would stay in kJumpNone is rather small, as
        // RequestReJIT() will immediately cause the originally JITted code to
        // be jump-stamped.
        kJumpNone               = 0x00000000,
        
        // This ReJitInfo is identified by a loaded MethodDesc that has been compiled and
        // jump-stamped, with the target being the prestub. The MethodDesc has not yet
        // been rejitted
        kJumpToPrestub          = 0x00000001,
        
        // This ReJitInfo is identified by a loaded MethodDesc that has been compiled AND
        // rejitted. The top of the originally JITted code has been jump-stamped, with
        // the target being the latest version of the rejitted code.
        kJumpToRejittedCode     = 0x00000002,

        kStateMask              = 0x0000000F,
    };

    Key                     m_key;
    DWORD                   m_dwInternalFlags;

    // The beginning of the rejitted code
    PCODE                   m_pCode;
    
    // The parent SharedReJitInfo, which manages the rejit request for all
    // instantiations.
    PTR_SharedReJitInfo const m_pShared;

    // My next sibling ReJitInfo for this rejit request (e.g., another
    // generic instantiation of the same method)
    PTR_ReJitInfo             m_pNext;
    
    // The originally JITted code that was overwritten with the jmp stamp.
    BYTE                    m_rgSavedCode[JumpStubSize];


    ReJitInfo(PTR_MethodDesc pMD, SharedReJitInfo * pShared);
    ReJitInfo(PTR_Module pModule, mdMethodDef methodDef, SharedReJitInfo * pShared);

    // Intentionally no destructor. ReJitInfo is allocated on a loader heap,
    // and will be freed (along with its associated SharedReJitInfo) when the
    // AD is unloaded.

    Key GetKey();
    PTR_MethodDesc GetMethodDesc();
    void GetModuleAndToken(Module ** ppModule, mdMethodDef * pMethodDef);
    void GetModuleAndTokenRegardlessOfKeyType(Module ** ppModule, mdMethodDef * pMethodDef);
    InternalFlags GetState();

    COR_ILMETHOD * GetIL();

    HRESULT JumpStampNativeCode(PCODE pCode = NULL);
    HRESULT UndoJumpStampNativeCode(BOOL fEESuspended);
    HRESULT UpdateJumpTarget(BOOL fEESuspended, PCODE pRejittedCode);
    HRESULT UpdateJumpStampHelper(BYTE* pbCode, INT64 i64OldValue, INT64 i64newValue, BOOL fContentionPossible);
    

protected:
    void CommonInit();
    INDEBUG(BOOL CodeIsSaved();)
};

//---------------------------------------------------------------------------------------
// Used by the SHash inside ReJitManager which maintains the set of ReJitInfo instances.
// 
class ReJitInfoTraits : public DefaultSHashTraits<PTR_ReJitInfo>
{
public:

    // explicitly declare local typedefs for these traits types, otherwise 
    // the compiler may get confused
    typedef DefaultSHashTraits<PTR_ReJitInfo> PARENT;
    typedef PARENT::element_t element_t;
    typedef PARENT::count_t count_t;

    typedef ReJitInfo::Key key_t;

    static key_t GetKey(const element_t &e);
    static BOOL Equals(key_t k1, key_t k2);
    static count_t Hash(key_t k);
    static bool IsNull(const element_t &e);
};

// RequestRejit and RequestRevert use these batches to accumulate ReJitInfos that need their
// jump stamps updated
class ReJitManager;
struct ReJitManagerJumpStampBatch
{
    ReJitManagerJumpStampBatch(ReJitManager * pReJitManager) : undoMethods(), preStubMethods()
    {
        LIMITED_METHOD_CONTRACT;
        this->pReJitManager = pReJitManager;
    }

    ReJitManager* pReJitManager;
    CDynArray<ReJitInfo *> undoMethods;
    CDynArray<ReJitInfo *> preStubMethods;
};

class ReJitManagerJumpStampBatchTraits : public DefaultSHashTraits<ReJitManagerJumpStampBatch *>
{
public:

    // explicitly declare local typedefs for these traits types, otherwise 
    // the compiler may get confused
    typedef DefaultSHashTraits<ReJitManagerJumpStampBatch *> PARENT;
    typedef PARENT::element_t element_t;
    typedef PARENT::count_t count_t;

    typedef ReJitManager * key_t;

    static key_t GetKey(const element_t &e)
    {
        return e->pReJitManager;
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        return (k1 == k2);
    }

    static count_t Hash(key_t k)
    {
        return (count_t)k;
    }

    static bool IsNull(const element_t &e)
    {
        return (e == NULL);
    }
};

struct ReJitReportErrorWorkItem
{
    Module* pModule;
    mdMethodDef methodDef;
    MethodDesc* pMethodDesc;
    HRESULT hrStatus;
};


#endif  // FEATURE_REJIT

//
// These holders are used by runtime code that is making new code
// available for execution, either by publishing jitted code
// or restoring NGEN code. It ensures the publishing is synchronized
// with rejit requests
//
class ReJitPublishMethodHolder
{
public:
#if !defined(FEATURE_REJIT) || defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    ReJitPublishMethodHolder(MethodDesc* pMethod, PCODE pCode) { }
#else
    ReJitPublishMethodHolder(MethodDesc* pMethod, PCODE pCode);
    ~ReJitPublishMethodHolder();
#endif

private:
#if defined(FEATURE_REJIT)
    MethodDesc * m_pMD;
    HRESULT m_hr;
#endif
};

class ReJitPublishMethodTableHolder
{
public:
#if !defined(FEATURE_REJIT) || defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    ReJitPublishMethodTableHolder(MethodTable* pMethodTable) { }
#else
    ReJitPublishMethodTableHolder(MethodTable* pMethodTable);
    ~ReJitPublishMethodTableHolder();
#endif

private:
#if defined(FEATURE_REJIT)
    MethodTable* m_pMethodTable;
    CDynArray<ReJitReportErrorWorkItem> m_errors;
#endif
};

//---------------------------------------------------------------------------------------
// The big honcho.  One of these per AppDomain, plus one for the
// SharedDomain.  Contains the hash table of ReJitInfo structures to manage
// every rejit and revert request for its owning domain.
// 
class ReJitManager
{
    friend class ClrDataAccess;
    friend class DacDbiInterfaceImpl;

    //I would have prefered to make these inner classes, but
    //then I can't friend them from crst easily.
    friend class ReJitPublishMethodHolder;
    friend class ReJitPublishMethodTableHolder;

private:

#ifdef FEATURE_REJIT

    // Hash table mapping MethodDesc* (or (ModuleID, mdMethodDef)) to its
    // ReJitInfos. One key may map to multiple ReJitInfos if there have been
    // multiple rejit requests made for the same MD. See
    // code:ReJitManager::ReJitManager#Invariants for more information.
    typedef SHash<ReJitInfoTraits> ReJitInfoHash;

    // One global crst (for the entire CLR instance) to synchronize
    // cross-ReJitManager operations, such as batch calls to RequestRejit and
    // RequestRevert (which modify multiple ReJitManager instances).
    static CrstStatic s_csGlobalRequest;

    // All The ReJitInfos (and their linked SharedReJitInfos) for this domain.
    ReJitInfoHash m_table;

    // The crst that synchronizes the data in m_table, including
    // adding/removing to m_table, as well as state changes made to
    // individual ReJitInfos & SharedReJitInfos in m_table.
    CrstExplicitInit    m_crstTable;

#endif //FEATURE_REJIT

public:
    // The ReJITManager takes care of grabbing its m_crstTable when necessary.  However,
    // for clients who need to do this explicitly (like ETW rundown), this holder may be
    // used.
    class TableLockHolder
#ifdef FEATURE_REJIT
        : public CrstHolder
#endif
    {
    public:
        TableLockHolder(ReJitManager * pReJitManager);
    };

    static void InitStatic();

    static BOOL IsReJITEnabled();

    static void OnAppDomainExit(AppDomain * pAppDomain);

    static HRESULT RequestReJIT(
        ULONG       cFunctions,
        ModuleID    rgModuleIDs[],
        mdMethodDef rgMethodDefs[]);
    
    static HRESULT RequestRevert(
        ULONG       cFunctions,
        ModuleID    rgModuleIDs[],
        mdMethodDef rgMethodDefs[],
        HRESULT     rgHrStatuses[]);

    static PCODE DoReJitIfNecessary(PTR_MethodDesc pMD);  // Invokes the jit, or returns previously rejitted code
    
    static void DoJumpStampForAssemblyIfNecessary(Assembly* pAssemblyToSearch);

    static DWORD GetCurrentReJitFlags(PTR_MethodDesc pMD);

    ReJitManager();

    void PreInit(BOOL fSharedDomain);

    ReJITID GetReJitId(PTR_MethodDesc pMD, PCODE pCodeStart);

    ReJITID GetReJitIdNoLock(PTR_MethodDesc pMD, PCODE pCodeStart);

    PCODE GetCodeStart(PTR_MethodDesc pMD, ReJITID reJitId);

    HRESULT GetReJITIDs(PTR_MethodDesc pMD, ULONG cReJitIds, ULONG * pcReJitIds, ReJITID reJitIds[]);

#ifdef FEATURE_REJIT


    INDEBUG(BOOL IsTableCrstOwnedByCurrentThread());

private:
    static HRESULT IsMethodSafeForReJit(PTR_MethodDesc pMD);
    static void ReportReJITError(ReJitReportErrorWorkItem* pErrorRecord);
    static void ReportReJITError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus);
    static HRESULT AddReJITError(ReJitInfo* pReJitInfo, HRESULT hrStatus, CDynArray<ReJitReportErrorWorkItem> * pErrors);
    static HRESULT AddReJITError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus, CDynArray<ReJitReportErrorWorkItem> * pErrors);
    HRESULT BatchUpdateJumpStamps(CDynArray<ReJitInfo *> * pUndoMethods, CDynArray<ReJitInfo *> * pPreStubMethods, CDynArray<ReJitReportErrorWorkItem> * pErrors);

    PCODE DoReJitIfNecessaryWorker(PTR_MethodDesc pMD);  // Invokes the jit, or returns previously rejitted code
    DWORD GetCurrentReJitFlagsWorker(PTR_MethodDesc pMD);

    HRESULT MarkAllInstantiationsForReJit(
        SharedReJitInfo * pSharedForAllGenericInstantiations,
        AppDomain * pAppDomainToSearch,
        PTR_Module pModuleContainingGenericDefinition,
        mdMethodDef methodDef,
        ReJitManagerJumpStampBatch* pJumpStampBatch,
        CDynArray<ReJitReportErrorWorkItem> * pRejitErrors);

    INDEBUG(BaseDomain * m_pDomain;)
        INDEBUG(void Dump(LPCSTR szIntroText);)
        INDEBUG(void AssertRestOfEntriesAreReverted(
        ReJitInfoHash::KeyIterator iter,
        ReJitInfoHash::KeyIterator end);)


    HRESULT DoJumpStampIfNecessary(MethodDesc* pMD, PCODE pCode);
    HRESULT MarkForReJit(PTR_MethodDesc pMD, SharedReJitInfo * pSharedToReuse, ReJitManagerJumpStampBatch* pJumpStampBatch, CDynArray<ReJitReportErrorWorkItem> * pRejitErrors, SharedReJitInfo ** ppSharedUsed);
    HRESULT MarkForReJit(PTR_Module pModule, mdMethodDef methodDef, ReJitManagerJumpStampBatch* pJumpStampBatch, CDynArray<ReJitReportErrorWorkItem> * pRejitErrors, SharedReJitInfo ** ppSharedUsed);
    HRESULT MarkForReJitHelper(
        PTR_MethodDesc pMD, 
        PTR_Module pModule, 
        mdMethodDef methodDef,
        SharedReJitInfo * pSharedToReuse,
        ReJitManagerJumpStampBatch* pJumpStampBatch,
        CDynArray<ReJitReportErrorWorkItem> * pRejitErrors,
        /* out */ SharedReJitInfo ** ppSharedUsed);
    HRESULT AddNewReJitInfo(
        PTR_MethodDesc pMD, 
        PTR_Module pModule,
        mdMethodDef methodDef,
        SharedReJitInfo * pShared,
        ReJitInfo ** ppInfo);
    HRESULT RequestRevertByToken(PTR_Module pModule, mdMethodDef methodDef);
    PTR_ReJitInfo FindReJitInfo(PTR_MethodDesc pMD, PCODE pCodeStart, ReJITID reJitId);
    PTR_ReJitInfo FindNonRevertedReJitInfo(PTR_Module pModule, mdMethodDef methodDef);
    PTR_ReJitInfo FindNonRevertedReJitInfo(PTR_MethodDesc pMD);
    PTR_ReJitInfo FindNonRevertedReJitInfoHelper(PTR_MethodDesc pMD, PTR_Module pModule, mdMethodDef methodDef);
    ReJitInfo* FindPreReJittedReJitInfo(ReJitInfoHash::KeyIterator beginIter, ReJitInfoHash::KeyIterator endIter);
    HRESULT Revert(SharedReJitInfo * pShared, ReJitManagerJumpStampBatch* pJumpStampBatch);
    PCODE   DoReJit(ReJitInfo * pInfo);
    ReJitInfoHash::KeyIterator GetBeginIterator(PTR_MethodDesc pMD);
    ReJitInfoHash::KeyIterator GetEndIterator(PTR_MethodDesc pMD);
    ReJitInfoHash::KeyIterator GetBeginIterator(PTR_Module pModule, mdMethodDef methodDef);
    ReJitInfoHash::KeyIterator GetEndIterator(PTR_Module pModule, mdMethodDef methodDef);
    void RemoveReJitInfosFromDomain(AppDomain * pAppDomain);

#endif // FEATURE_REJIT

};

#include "rejit.inl"

#endif // _REJIT_H_
