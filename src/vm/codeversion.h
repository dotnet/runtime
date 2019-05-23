// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CodeVersion.h
//
// ===========================================================================


#ifndef CODE_VERSION_H
#define CODE_VERSION_H

class NativeCodeVersion;
class ILCodeVersion;
typedef DWORD NativeCodeVersionId;

#ifdef FEATURE_CODE_VERSIONING
class NativeCodeVersionNode;
typedef DPTR(class NativeCodeVersionNode) PTR_NativeCodeVersionNode;
class NativeCodeVersionCollection;
class NativeCodeVersionIterator;
class ILCodeVersionNode;
typedef DPTR(class ILCodeVersionNode) PTR_ILCodeVersionNode;
class ILCodeVersionCollection;
class ILCodeVersionIterator;
class MethodDescVersioningState;
typedef DPTR(class MethodDescVersioningState) PTR_MethodDescVersioningState;

class ILCodeVersioningState;
typedef DPTR(class ILCodeVersioningState) PTR_ILCodeVersioningState;
class CodeVersionManager;
typedef DPTR(class CodeVersionManager) PTR_CodeVersionManager;

// This HRESULT is only used as a private implementation detail. Corerror.xml has a comment in it
//  reserving this value for our use but it doesn't appear in the public headers.
#define CORPROF_E_RUNTIME_SUSPEND_REQUIRED _HRESULT_TYPEDEF_(0x80131381L)

#endif




class NativeCodeVersion
{
#ifdef FEATURE_CODE_VERSIONING
    friend class MethodDescVersioningState;
    friend class ILCodeVersion;
#endif

public:
    NativeCodeVersion();
    NativeCodeVersion(const NativeCodeVersion & rhs);
#ifdef FEATURE_CODE_VERSIONING
    NativeCodeVersion(PTR_NativeCodeVersionNode pVersionNode);
#endif
    explicit NativeCodeVersion(PTR_MethodDesc pMethod);
    BOOL IsNull() const;
    PTR_MethodDesc GetMethodDesc() const;
    NativeCodeVersionId GetVersionId() const;
    BOOL IsDefaultVersion() const;
    PCODE GetNativeCode() const;
    ILCodeVersion GetILCodeVersion() const;
    ReJITID GetILCodeVersionId() const;
#ifndef DACCESS_COMPILE
    BOOL SetNativeCodeInterlocked(PCODE pCode, PCODE pExpected = NULL);
#endif
    enum OptimizationTier
    {
        OptimizationTier0,
        OptimizationTier1,
        OptimizationTierOptimized, // may do less optimizations than tier 1
    };
#ifdef FEATURE_TIERED_COMPILATION
    OptimizationTier GetOptimizationTier() const;
#ifndef DACCESS_COMPILE
    void SetOptimizationTier(OptimizationTier tier);
#endif
#endif // FEATURE_TIERED_COMPILATION
    bool operator==(const NativeCodeVersion & rhs) const;
    bool operator!=(const NativeCodeVersion & rhs) const;
#if defined(DACCESS_COMPILE) && defined(FEATURE_CODE_VERSIONING)
    // The DAC is privy to the backing node abstraction
    PTR_NativeCodeVersionNode AsNode() const;
#endif

private:

#ifndef FEATURE_CODE_VERSIONING
    MethodDesc* m_pMethodDesc;
#else // FEATURE_CODE_VERSIONING

#ifndef DACCESS_COMPILE
    NativeCodeVersionNode* AsNode() const;
    NativeCodeVersionNode* AsNode();
    void SetActiveChildFlag(BOOL isActive);
    MethodDescVersioningState* GetMethodDescVersioningState();
#endif

    BOOL IsActiveChildVersion() const;
    PTR_MethodDescVersioningState GetMethodDescVersioningState() const;

    enum StorageKind
    {
        Unknown,
        Explicit,
        Synthetic
    };

    StorageKind m_storageKind;
    union
    {
        PTR_NativeCodeVersionNode m_pVersionNode;
        struct
        {
            PTR_MethodDesc m_pMethodDesc;
        } m_synthetic;
    };
#endif // FEATURE_CODE_VERSIONING
};



#ifdef FEATURE_CODE_VERSIONING



class ILCodeVersion
{
    friend class NativeCodeVersionIterator;

public:
    ILCodeVersion();
    ILCodeVersion(const ILCodeVersion & ilCodeVersion);
    ILCodeVersion(PTR_ILCodeVersionNode pILCodeVersionNode);
    ILCodeVersion(PTR_Module pModule, mdMethodDef methodDef);

    bool operator==(const ILCodeVersion & rhs) const;
    bool operator!=(const ILCodeVersion & rhs) const;
    BOOL HasDefaultIL() const;
    BOOL IsNull() const;
    BOOL IsDefaultVersion() const;
    PTR_Module GetModule() const;
    mdMethodDef GetMethodDef() const;
    ReJITID GetVersionId() const;
    NativeCodeVersionCollection GetNativeCodeVersions(PTR_MethodDesc pClosedMethodDesc) const;
    NativeCodeVersion GetActiveNativeCodeVersion(PTR_MethodDesc pClosedMethodDesc) const;
    PTR_COR_ILMETHOD GetIL() const;
    PTR_COR_ILMETHOD GetILNoThrow() const;
    DWORD GetJitFlags() const;
    const InstrumentedILOffsetMapping* GetInstrumentedILMap() const;

#ifndef DACCESS_COMPILE
    void SetIL(COR_ILMETHOD* pIL);
    void SetJitFlags(DWORD flags);
    void SetInstrumentedILMap(SIZE_T cMap, COR_IL_MAP * rgMap);
    HRESULT AddNativeCodeVersion(MethodDesc* pClosedMethodDesc, NativeCodeVersion::OptimizationTier optimizationTier, NativeCodeVersion* pNativeCodeVersion);
    HRESULT GetOrCreateActiveNativeCodeVersion(MethodDesc* pClosedMethodDesc, NativeCodeVersion* pNativeCodeVersion);
    HRESULT SetActiveNativeCodeVersion(NativeCodeVersion activeNativeCodeVersion, BOOL fEESuspended);
#endif //DACCESS_COMPILE

    enum RejitFlags
    {
        // The profiler has requested a ReJit, so we've allocated stuff, but we haven't
        // called back to the profiler to get any info or indicate that the ReJit has
        // started. (This Info can be 'reused' for a new ReJit if the
        // profiler calls RequestRejit again before we transition to the next state.)
        kStateRequested = 0x00000000,

        // The CLR has initiated the call to the profiler's GetReJITParameters() callback
        // but it hasn't completed yet. At this point we have to assume the profiler has
        // commited to a specific IL body, even if the CLR doesn't know what it is yet.
        // If the profiler calls RequestRejit we need to allocate a new ILCodeVersion
        // and call GetReJITParameters() again.
        kStateGettingReJITParameters = 0x00000001,

        // We have asked the profiler about this method via ICorProfilerFunctionControl,
        // and have thus stored the IL and codegen flags the profiler specified.
        kStateActive = 0x00000002,

        kStateMask = 0x0000000F,

        // Indicates that the method being ReJITted is an inliner of the actual 
        // ReJIT request and we should not issue the GetReJITParameters for this 
        // method.
        kSuppressParams = 0x80000000
    };

    RejitFlags GetRejitState() const;
    BOOL GetEnableReJITCallback() const;
#ifndef DACCESS_COMPILE
    void SetRejitState(RejitFlags newState);
    void SetEnableReJITCallback(BOOL state);
#endif

#ifdef DACCESS_COMPILE
    // The DAC is privy to the backing node abstraction
    PTR_ILCodeVersionNode AsNode() const;
#endif

private:

#ifndef DACCESS_COMPILE
    PTR_ILCodeVersionNode AsNode();
    PTR_ILCodeVersionNode AsNode() const;
#endif

    enum StorageKind
    {
        Unknown,
        Explicit,
        Synthetic
    };

    StorageKind m_storageKind;
    union
    {
        PTR_ILCodeVersionNode m_pVersionNode;
        struct
        {
            PTR_Module m_pModule;
            mdMethodDef m_methodDef;
        } m_synthetic;
    };
};


class NativeCodeVersionNode
{
    friend NativeCodeVersionIterator;
    friend MethodDescVersioningState;
    friend ILCodeVersionNode;
public:
#ifndef DACCESS_COMPILE
    NativeCodeVersionNode(NativeCodeVersionId id, MethodDesc* pMethod, ReJITID parentId, NativeCodeVersion::OptimizationTier optimizationTier);
#endif
#ifdef DEBUG
    BOOL LockOwnedByCurrentThread() const;
#endif
    PTR_MethodDesc GetMethodDesc() const;
    NativeCodeVersionId GetVersionId() const;
    PCODE GetNativeCode() const;
    ReJITID GetILVersionId() const;
    ILCodeVersion GetILCodeVersion() const;
    BOOL IsActiveChildVersion() const;
#ifndef DACCESS_COMPILE
    BOOL SetNativeCodeInterlocked(PCODE pCode, PCODE pExpected);
    void SetActiveChildFlag(BOOL isActive);
#endif
#ifdef FEATURE_TIERED_COMPILATION
    NativeCodeVersion::OptimizationTier GetOptimizationTier() const;
#ifndef DACCESS_COMPILE
    void SetOptimizationTier(NativeCodeVersion::OptimizationTier tier);
#endif
#endif // FEATURE_TIERED_COMPILATION

private:
    //union - could save a little memory?
    //{
    PCODE m_pNativeCode;
    PTR_MethodDesc m_pMethodDesc;
    //};

    ReJITID m_parentId;
    PTR_NativeCodeVersionNode m_pNextMethodDescSibling;
    NativeCodeVersionId m_id;
#ifdef FEATURE_TIERED_COMPILATION
    NativeCodeVersion::OptimizationTier m_optTier;
#endif

    enum NativeCodeVersionNodeFlags
    {
        IsActiveChildFlag = 1
    };
    DWORD m_flags;
};

class NativeCodeVersionCollection
{
    friend class NativeCodeVersionIterator;
public:
    NativeCodeVersionCollection(PTR_MethodDesc pMethodDescFilter, ILCodeVersion ilCodeFilter);
    NativeCodeVersionIterator Begin();
    NativeCodeVersionIterator End();

private:
    PTR_MethodDesc m_pMethodDescFilter;
    ILCodeVersion m_ilCodeFilter;
};

class NativeCodeVersionIterator : public Enumerator<const NativeCodeVersion, NativeCodeVersionIterator>
{
    friend class Enumerator<const NativeCodeVersion, NativeCodeVersionIterator>;

public:
    NativeCodeVersionIterator(NativeCodeVersionCollection* pCollection);
    CHECK Check() const { CHECK_OK; }

protected:
    const NativeCodeVersion & Get() const;
    void First();
    void Next();
    bool Equal(const NativeCodeVersionIterator &i) const;

    CHECK DoCheck() const { CHECK_OK; }

private:
    enum IterationStage
    {
        Initial,
        ImplicitCodeVersion,
        LinkedList,
        End
    };
    IterationStage m_stage;
    NativeCodeVersionCollection* m_pCollection;
    PTR_NativeCodeVersionNode m_pLinkedListCur;
    NativeCodeVersion m_cur;
};

class ILCodeVersionNode
{
public:
    ILCodeVersionNode();
#ifndef DACCESS_COMPILE
    ILCodeVersionNode(Module* pModule, mdMethodDef methodDef, ReJITID id);
#endif
#ifdef DEBUG
    BOOL LockOwnedByCurrentThread() const;
#endif //DEBUG
    PTR_Module GetModule() const;
    mdMethodDef GetMethodDef() const;
    ReJITID GetVersionId() const;
    PTR_COR_ILMETHOD GetIL() const;
    DWORD GetJitFlags() const;
    const InstrumentedILOffsetMapping* GetInstrumentedILMap() const;
    ILCodeVersion::RejitFlags GetRejitState() const;
    BOOL GetEnableReJITCallback() const;
    PTR_ILCodeVersionNode GetNextILVersionNode() const;
#ifndef DACCESS_COMPILE
    void SetIL(COR_ILMETHOD* pIL);
    void SetJitFlags(DWORD flags);
    void SetInstrumentedILMap(SIZE_T cMap, COR_IL_MAP * rgMap);
    void SetRejitState(ILCodeVersion::RejitFlags newState);
    void SetEnableReJITCallback(BOOL state);
    void SetNextILVersionNode(ILCodeVersionNode* pNextVersionNode);
#endif

private:
    PTR_Module m_pModule;
    mdMethodDef m_methodDef;
    ReJITID m_rejitId;
    PTR_ILCodeVersionNode m_pNextILVersionNode;
    Volatile<ILCodeVersion::RejitFlags> m_rejitState;
    VolatilePtr<COR_ILMETHOD, PTR_COR_ILMETHOD> m_pIL;
    Volatile<DWORD> m_jitFlags;
    InstrumentedILOffsetMapping m_instrumentedILMap;
};

class ILCodeVersionCollection
{
    friend class ILCodeVersionIterator;

public:
    ILCodeVersionCollection(PTR_Module pModule, mdMethodDef methodDef);
    ILCodeVersionIterator Begin();
    ILCodeVersionIterator End();

private:
    PTR_Module m_pModule;
    mdMethodDef m_methodDef;
};

class ILCodeVersionIterator : public Enumerator<const ILCodeVersion, ILCodeVersionIterator>
{
    friend class Enumerator<const ILCodeVersion, ILCodeVersionIterator>;

public:
    ILCodeVersionIterator();
    ILCodeVersionIterator(const ILCodeVersionIterator & iter);
    ILCodeVersionIterator(ILCodeVersionCollection* pCollection);
    CHECK Check() const { CHECK_OK; }

protected:
    const ILCodeVersion & Get() const;
    void First();
    void Next();
    bool Equal(const ILCodeVersionIterator &i) const;

    CHECK DoCheck() const { CHECK_OK; }

private:
    enum IterationStage
    {
        Initial,
        ImplicitCodeVersion,
        LinkedList,
        End
    };
    IterationStage m_stage;
    ILCodeVersion m_cur;
    PTR_ILCodeVersionNode m_pLinkedListCur;
    ILCodeVersionCollection* m_pCollection;
};

class MethodDescVersioningState
{
public:
    // The size of the code used to jump stamp the prolog
#ifdef FEATURE_JUMPSTAMP
    static const size_t JumpStubSize =
#if defined(_X86_) || defined(_AMD64_)
        5;
#else
#error "Need to define size of jump-stamp for this platform"
#endif
#endif // FEATURE_JUMPSTAMP

    MethodDescVersioningState(PTR_MethodDesc pMethodDesc);
    PTR_MethodDesc GetMethodDesc() const;
    NativeCodeVersionId AllocateVersionId();
    PTR_NativeCodeVersionNode GetFirstVersionNode() const;

#ifndef DACCESS_COMPILE
#ifdef FEATURE_JUMPSTAMP
    HRESULT SyncJumpStamp(NativeCodeVersion nativeCodeVersion, BOOL fEESuspended);
    HRESULT UpdateJumpTarget(BOOL fEESuspended, PCODE pRejittedCode);
    HRESULT UndoJumpStampNativeCode(BOOL fEESuspended);
    HRESULT JumpStampNativeCode(PCODE pCode = NULL);
#endif // FEATURE_JUMPSTAMP
    void LinkNativeCodeVersionNode(NativeCodeVersionNode* pNativeCodeVersionNode);
#endif // DACCESS_COMPILE

#ifdef FEATURE_JUMPSTAMP
    enum JumpStampFlags
    {
        // There is no jump stamp in place on this method (Either because
        // there is no code at all, or there is code that hasn't been
        // overwritten with a jump)
        JumpStampNone = 0x0,

        // The method code has the jump stamp written in, and it points to the Prestub
        JumpStampToPrestub = 0x1,

        // The method code has the jump stamp written in, and it points to the currently
        // active code version
        JumpStampToActiveVersion = 0x2,
    };

    JumpStampFlags GetJumpStampState();
    void SetJumpStampState(JumpStampFlags newState);
#endif // FEATURE_JUMPSTAMP

    //read-write data for the default native code version
    BOOL IsDefaultVersionActiveChild() const;
#ifndef DACCESS_COMPILE
    void SetDefaultVersionActiveChildFlag(BOOL isActive);
#endif

private:
#if !defined(DACCESS_COMPILE) && defined(FEATURE_JUMPSTAMP)
    INDEBUG(BOOL CodeIsSaved();)
    HRESULT UpdateJumpStampHelper(BYTE* pbCode, INT64 i64OldValue, INT64 i64NewValue, BOOL fContentionPossible);
#endif
    PTR_MethodDesc m_pMethodDesc;

    enum MethodDescVersioningStateFlags
    {
        JumpStampMask = 0x3,
        IsDefaultVersionActiveChildFlag = 0x4
    };
    BYTE m_flags;
    NativeCodeVersionId m_nextId;
    PTR_NativeCodeVersionNode m_pFirstVersionNode;


    // The originally JITted code that was overwritten with the jmp stamp.
#ifdef FEATURE_JUMPSTAMP
    BYTE m_rgSavedCode[JumpStubSize];
#endif
};

class MethodDescVersioningStateHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<PTR_MethodDescVersioningState>>
{
public:
    typedef typename DefaultSHashTraits<PTR_MethodDescVersioningState>::element_t element_t;
    typedef typename DefaultSHashTraits<PTR_MethodDescVersioningState>::count_t count_t;

    typedef const PTR_MethodDesc key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->GetMethodDesc();
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)(size_t)dac_cast<TADDR>(k);
    }

    static element_t Null() { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_MethodDescVersioningState>(nullptr); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
};

typedef SHash<MethodDescVersioningStateHashTraits> MethodDescVersioningStateHash;

class ILCodeVersioningState
{
public:
    ILCodeVersioningState(PTR_Module pModule, mdMethodDef methodDef);
    ILCodeVersion GetActiveVersion() const;
    PTR_ILCodeVersionNode GetFirstVersionNode() const;
#ifndef DACCESS_COMPILE
    void SetActiveVersion(ILCodeVersion ilActiveCodeVersion);
    void LinkILCodeVersionNode(ILCodeVersionNode* pILCodeVersionNode);
#endif

    struct Key
    {
    public:
        Key();
        Key(PTR_Module pModule, mdMethodDef methodDef);
        size_t Hash() const;
        bool operator==(const Key & rhs) const;
    private:
        PTR_Module m_pModule;
        mdMethodDef m_methodDef;
    };

    Key GetKey() const;

private:
    ILCodeVersion m_activeVersion;
    PTR_ILCodeVersionNode m_pFirstVersionNode;
    PTR_Module m_pModule;
    mdMethodDef m_methodDef;
};

class ILCodeVersioningStateHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<PTR_ILCodeVersioningState>>
{
public:
    typedef typename DefaultSHashTraits<PTR_ILCodeVersioningState>::element_t element_t;
    typedef typename DefaultSHashTraits<PTR_ILCodeVersioningState>::count_t count_t;

    typedef const ILCodeVersioningState::Key key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->GetKey();
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)k.Hash();
    }

    static element_t Null() { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_ILCodeVersioningState>(nullptr); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
};

typedef SHash<ILCodeVersioningStateHashTraits> ILCodeVersioningStateHash;

class CodeVersionManager
{
    friend class ILCodeVersion;
    friend class PublishMethodHolder;
    friend class PublishMethodTableHolder;

public:
    CodeVersionManager();

    void PreInit();

    class TableLockHolder : public CrstHolder
    {
    public:
        TableLockHolder(CodeVersionManager * pCodeVersionManager);
    };
    //Using the holder is preferable, but in some cases the holder can't be used
#ifndef DACCESS_COMPILE
    void EnterLock();
    void LeaveLock();
#endif

#ifdef DEBUG
    BOOL LockOwnedByCurrentThread() const;
#endif

    DWORD GetNonDefaultILVersionCount();
    ILCodeVersionCollection GetILCodeVersions(PTR_MethodDesc pMethod);
    ILCodeVersionCollection GetILCodeVersions(PTR_Module pModule, mdMethodDef methodDef);
    ILCodeVersion GetActiveILCodeVersion(PTR_MethodDesc pMethod);
    ILCodeVersion GetActiveILCodeVersion(PTR_Module pModule, mdMethodDef methodDef);
    ILCodeVersion GetILCodeVersion(PTR_MethodDesc pMethod, ReJITID rejitId);
    NativeCodeVersionCollection GetNativeCodeVersions(PTR_MethodDesc pMethod) const;
    NativeCodeVersion GetNativeCodeVersion(PTR_MethodDesc pMethod, PCODE codeStartAddress) const;
    PTR_ILCodeVersioningState GetILCodeVersioningState(PTR_Module pModule, mdMethodDef methodDef) const;
    PTR_MethodDescVersioningState GetMethodDescVersioningState(PTR_MethodDesc pMethod) const;

#ifndef DACCESS_COMPILE
    struct CodePublishError
    {
        Module* pModule;
        mdMethodDef methodDef;
        MethodDesc* pMethodDesc;
        HRESULT hrStatus;
    };

    HRESULT AddILCodeVersion(Module* pModule, mdMethodDef methodDef, ReJITID rejitId, ILCodeVersion* pILCodeVersion);
    HRESULT AddNativeCodeVersion(ILCodeVersion ilCodeVersion, MethodDesc* pClosedMethodDesc, NativeCodeVersion::OptimizationTier optimizationTier, NativeCodeVersion* pNativeCodeVersion);
    HRESULT DoJumpStampIfNecessary(MethodDesc* pMD, PCODE pCode);
    PCODE PublishVersionableCodeIfNecessary(MethodDesc* pMethodDesc, BOOL fCanBackpatchPrestub);
    HRESULT PublishNativeCodeVersion(MethodDesc* pMethodDesc, NativeCodeVersion nativeCodeVersion, BOOL fEESuspended);
    HRESULT GetOrCreateMethodDescVersioningState(MethodDesc* pMethod, MethodDescVersioningState** ppMethodDescVersioningState);
    HRESULT GetOrCreateILCodeVersioningState(Module* pModule, mdMethodDef methodDef, ILCodeVersioningState** ppILCodeVersioningState);
    HRESULT SetActiveILCodeVersions(ILCodeVersion* pActiveVersions, DWORD cActiveVersions, BOOL fEESuspended, CDynArray<CodePublishError> * pPublishErrors);
    static HRESULT AddCodePublishError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus, CDynArray<CodePublishError> * pErrors);
    static HRESULT AddCodePublishError(NativeCodeVersion nativeCodeVersion, HRESULT hrStatus, CDynArray<CodePublishError> * pErrors);
    static void OnAppDomainExit(AppDomain* pAppDomain);
#endif

    static bool IsMethodSupported(PTR_MethodDesc pMethodDesc);

private:

#ifndef DACCESS_COMPILE
    static HRESULT EnumerateClosedMethodDescs(MethodDesc* pMD, CDynArray<MethodDesc*> * pClosedMethodDescs, CDynArray<CodePublishError> * pUnsupportedMethodErrors);
    static HRESULT EnumerateDomainClosedMethodDescs(
        AppDomain * pAppDomainToSearch,
        Module* pModuleContainingMethodDef,
        mdMethodDef methodDef,
        CDynArray<MethodDesc*> * pClosedMethodDescs,
        CDynArray<CodePublishError> * pUnsupportedMethodErrors);
    static HRESULT GetNonVersionableError(MethodDesc* pMD);
    void ReportCodePublishError(CodePublishError* pErrorRecord);
    void ReportCodePublishError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus);
#endif

    //Module,MethodDef -> ILCodeVersioningState
    ILCodeVersioningStateHash m_ilCodeVersioningStateMap;

    //closed MethodDesc -> MethodDescVersioningState
    MethodDescVersioningStateHash m_methodDescVersioningStateMap;

    CrstExplicitInit m_crstTable;
};

#endif // FEATURE_CODE_VERSIONING

//
// These holders are used by runtime code that is making new code
// available for execution, either by publishing jitted code
// or restoring NGEN code. It ensures the publishing is synchronized
// with rejit requests
//
class PublishMethodHolder
{
public:
#if !defined(FEATURE_CODE_VERSIONING) || defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    PublishMethodHolder(MethodDesc* pMethod, PCODE pCode) { }
#else
    PublishMethodHolder(MethodDesc* pMethod, PCODE pCode);
    ~PublishMethodHolder();
#endif

private:
#if defined(FEATURE_CODE_VERSIONING)
    MethodDesc * m_pMD;
    HRESULT m_hr;
#endif
};

class PublishMethodTableHolder
{
public:
#if !defined(FEATURE_CODE_VERSIONING) || defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    PublishMethodTableHolder(MethodTable* pMethodTable) { }
#else
    PublishMethodTableHolder(MethodTable* pMethodTable);
    ~PublishMethodTableHolder();
#endif

private:
#if defined(FEATURE_CODE_VERSIONING) && !defined(DACCESS_COMPILE)
    MethodTable* m_pMethodTable;
    CDynArray<CodeVersionManager::CodePublishError> m_errors;
#endif
};

#endif // CODE_VERSION_H
