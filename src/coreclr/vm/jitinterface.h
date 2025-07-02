// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: JITinterface.H
//

// ===========================================================================


#ifndef JITINTERFACE_H
#define JITINTERFACE_H

#include "corjit.h"

#ifndef TARGET_UNIX
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((32*1024)-1)   // when generating JIT code
#else // !TARGET_UNIX
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((GetOsPageSize() / 2) - 1)
#endif // !TARGET_UNIX
#include "pgo.h"

enum StompWriteBarrierCompletionAction
{
    SWB_PASS = 0x0,
    SWB_ICACHE_FLUSH = 0x1,
    SWB_EE_RESTART = 0x2
};

enum SignatureKind
{
    SK_NOT_CALLSITE,
    SK_CALLSITE,
    SK_VIRTUAL_CALLSITE,
    SK_STATIC_VIRTUAL_CODEPOINTER_CALLSITE,
};

class Stub;
class MethodDesc;
class NativeCodeVersion;
class FieldDesc;
enum RuntimeExceptionKind;
class AwareLock;
class PtrArray;
#if defined(FEATURE_GDBJIT)
class CalledMethod;
#endif

#include "genericdict.h"

void FlushVirtualFunctionPointerCaches();

inline FieldDesc* GetField(CORINFO_FIELD_HANDLE fieldHandle)
{
    LIMITED_METHOD_CONTRACT;
    return (FieldDesc*) fieldHandle;
}

inline
bool SigInfoFlagsAreValid (CORINFO_SIG_INFO *sig)
{
    LIMITED_METHOD_CONTRACT;
    return !(sig->flags & ~(  CORINFO_SIGFLAG_IS_LOCAL_SIG
                            | CORINFO_SIGFLAG_IL_STUB
                            ));
}


void InitJITAllocationHelpers();

void InitJITWriteBarrierHelpers();

PCODE UnsafeJitFunction(PrepareCodeConfig* config,
                        COR_ILMETHOD_DECODER* header,
                        _Out_ bool* isTier0,
                        _Out_ bool* isInterpreterCode,
                        _Out_ ULONG* pSizeOfCode);

void getMethodInfoILMethodHeaderHelper(
    COR_ILMETHOD_DECODER* header,
    CORINFO_METHOD_INFO* methInfo
    );


BOOL LoadDynamicInfoEntry(Module *currentModule,
                          RVA fixupRva,
                          SIZE_T *entry,
                          BOOL mayUsePrecompiledNDirectMethods = TRUE);

// These must be implemented in assembly and generate a TransitionBlock then calling JIT_PatchpointWorkerWithPolicy in order to actually be used.
EXTERN_C FCDECL2(void, JIT_Patchpoint, int* counter, int ilOffset);
EXTERN_C FCDECL1(void, JIT_PatchpointForced, int ilOffset);

//
// JIT HELPER ALIASING FOR PORTABILITY.
//
// The portable helper is used if the platform does not provide optimized implementation.
//

EXTERN_C FCDECL0(void, JIT_PollGC);

#ifndef JIT_GetGCStaticBase
#define JIT_GetGCStaticBase NULL
#else
EXTERN_C FCDECL1(void*, JIT_GetGCStaticBase, DynamicStaticsInfo* pStaticsInfo);
#endif

#ifndef JIT_GetNonGCStaticBase
#define JIT_GetNonGCStaticBase NULL
#else
EXTERN_C FCDECL1(void*, JIT_GetNonGCStaticBase, DynamicStaticsInfo* pStaticsInfo);
#endif

#ifndef JIT_GetGCStaticBaseNoCtor
#define JIT_GetGCStaticBaseNoCtor JIT_GetGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetGCStaticBaseNoCtor, MethodTable *pMT);
EXTERN_C FCDECL1(void*, JIT_GetGCStaticBaseNoCtor_Portable, MethodTable *pMT);

#ifndef JIT_GetNonGCStaticBaseNoCtor
#define JIT_GetNonGCStaticBaseNoCtor JIT_GetNonGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetNonGCStaticBaseNoCtor, MethodTable *pMT);
EXTERN_C FCDECL1(void*, JIT_GetNonGCStaticBaseNoCtor_Portable, MethodTable *pMT);

#ifndef JIT_GetDynamicGCStaticBase
#define JIT_GetDynamicGCStaticBase NULL
#else
EXTERN_C FCDECL1(void*, JIT_GetDynamicGCStaticBase, DynamicStaticsInfo* pStaticsInfo);
#endif

#ifndef JIT_GetDynamicNonGCStaticBase
#define JIT_GetDynamicNonGCStaticBase NULL
#else
EXTERN_C FCDECL1(void*, JIT_GetDynamicNonGCStaticBase, DynamicStaticsInfo* pStaticsInfo);
#endif

#ifndef JIT_GetDynamicGCStaticBaseNoCtor
#define JIT_GetDynamicGCStaticBaseNoCtor JIT_GetDynamicGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetDynamicGCStaticBaseNoCtor, DynamicStaticsInfo* pStaticsInfo);
EXTERN_C FCDECL1(void*, JIT_GetDynamicGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pStaticsInfo);

#ifndef JIT_GetDynamicNonGCStaticBaseNoCtor
#define JIT_GetDynamicNonGCStaticBaseNoCtor JIT_GetDynamicNonGCStaticBaseNoCtor_Portable
#endif
EXTERN_C FCDECL1(void*, JIT_GetDynamicNonGCStaticBaseNoCtor, DynamicStaticsInfo* pStaticsInfo);
EXTERN_C FCDECL1(void*, JIT_GetDynamicNonGCStaticBaseNoCtor_Portable, DynamicStaticsInfo* pStaticsInfo);

EXTERN_C FCDECL1(Object*, RhpNewFast, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, RhpNewArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
EXTERN_C FCDECL2(Object*, RhNewString, CORINFO_CLASS_HANDLE typeHnd_, DWORD stringLength);

#if defined(FEATURE_64BIT_ALIGNMENT)
EXTERN_C FCDECL1(Object*, RhpNewFastAlign8, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL1(Object*, RhpNewFastMisalign, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
#endif

#if defined(TARGET_WINDOWS) && (defined(TARGET_AMD64) || defined(TARGET_X86))
EXTERN_C FCDECL1(Object*, RhpNewFast_UP, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, RhpNewArrayFast_UP, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast_UP, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
EXTERN_C FCDECL2(Object*, RhNewString_UP, CORINFO_CLASS_HANDLE typeHnd_, DWORD stringLength);
#endif

EXTERN_C FCDECL1(Object*, RhpNew, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);
EXTERN_C FCDECL1(Object*, RhpNewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C FCDECL2(Object*, RhpNewArrayMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size);

EXTERN_C FCDECL1(Object*, AllocateStringFast, DWORD stringLength);
EXTERN_C FCDECL1(Object*, AllocateStringSlow, DWORD stringLength);

EXTERN_C FCDECL2(void, JITutil_MonReliableEnter, Object* obj, BYTE* pbLockTaken);
EXTERN_C FCDECL3(void, JITutil_MonTryEnter, Object* obj, INT32 timeOut, BYTE* pbLockTaken);
EXTERN_C FCDECL2(void, JITutil_MonReliableContention, AwareLock* awarelock, BYTE* pbLockTaken);

EXTERN_C FCDECL1(void*, JIT_GetNonGCStaticBase_Helper, MethodTable *pMT);
EXTERN_C FCDECL1(void*, JIT_GetGCStaticBase_Helper, MethodTable *pMT);

EXTERN_C void DoJITFailFast ();
EXTERN_C FCDECL0(void, JIT_FailFast);

FCDECL0(int, JIT_GetCurrentManagedThreadId);

#if !defined(FEATURE_USE_ASM_GC_WRITE_BARRIERS) && defined(FEATURE_COUNT_GC_WRITE_BARRIERS)
// Extra argument for the classification of the checked barriers.
extern "C" FCDECL3(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref, CheckedWriteBarrierKinds kind);
#else
// Regular checked write barrier.
extern "C" FCDECL2(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref);

#ifdef TARGET_ARM64
#define RhpCheckedAssignRef RhpCheckedAssignRefArm64
#define RhpByRefAssignRef RhpByRefAssignRefArm64
#define RhpAssignRef RhpAssignRefArm64
#elif defined (TARGET_LOONGARCH64)
#define RhpAssignRef RhpAssignRefLoongArch64
#elif defined (TARGET_RISCV64)
#define RhpAssignRef RhpAssignRefRiscV64
#endif // TARGET_*

#endif // FEATURE_USE_ASM_GC_WRITE_BARRIERS && defined(FEATURE_COUNT_GC_WRITE_BARRIERS)

extern "C" FCDECL2(VOID, RhpCheckedAssignRef, Object **dst, Object *ref);
extern "C" FCDECL2(VOID, RhpByRefAssignRef, Object **dst, Object *ref);
extern "C" FCDECL2(VOID, RhpAssignRef, Object **dst, Object *ref);

extern "C" FCDECL2(VOID, JIT_WriteBarrier, Object **dst, Object *ref);
extern "C" FCDECL2(VOID, JIT_WriteBarrierEnsureNonHeapTarget, Object **dst, Object *ref);

// ARM64 JIT_WriteBarrier uses special ABI and thus is not callable directly
// Copied write barriers must be called at a different location
extern "C" FCDECL2(VOID, JIT_WriteBarrier_Callable, Object **dst, Object *ref);

#define WriteBarrier_Helper JIT_WriteBarrier_Callable

EXTERN_C FCDECL2_VV(INT64, JIT_LMul, INT64 val1, INT64 val2);

#ifndef HOST_64BIT
#ifdef TARGET_X86
// JIThelp.asm
EXTERN_C void STDCALL JIT_LLsh();
EXTERN_C void STDCALL JIT_LRsh();
EXTERN_C void STDCALL JIT_LRsz();
#else // TARGET_X86
EXTERN_C FCDECL2_VV(UINT64, JIT_LLsh, UINT64 num, int shift);
EXTERN_C FCDECL2_VV(INT64, JIT_LRsh, INT64 num, int shift);
EXTERN_C FCDECL2_VV(UINT64, JIT_LRsz, UINT64 num, int shift);
#endif // !TARGET_X86
#endif // !HOST_64BIT

#ifdef TARGET_X86

#define ENUM_X86_WRITE_BARRIER_REGISTERS() \
    X86_WRITE_BARRIER_REGISTER(EAX) \
    X86_WRITE_BARRIER_REGISTER(ECX) \
    X86_WRITE_BARRIER_REGISTER(EBX) \
    X86_WRITE_BARRIER_REGISTER(ESI) \
    X86_WRITE_BARRIER_REGISTER(EDI) \
    X86_WRITE_BARRIER_REGISTER(EBP)

extern "C"
{

// JIThelp.asm/JIThelp.s
#define X86_WRITE_BARRIER_REGISTER(reg) \
    void STDCALL JIT_DebugWriteBarrier##reg(); \
    void STDCALL JIT_WriteBarrier##reg(); \
    void FASTCALL RhpAssignRef##reg(Object**, Object*); \
    void FASTCALL RhpCheckedAssignRef##reg(Object**, Object*);

    ENUM_X86_WRITE_BARRIER_REGISTERS()
#undef X86_WRITE_BARRIER_REGISTER

    void STDCALL JIT_WriteBarrierGroup();
    void STDCALL JIT_WriteBarrierGroup_End();

    void STDCALL JIT_PatchedWriteBarrierGroup();
    void STDCALL JIT_PatchedWriteBarrierGroup_End();
}

void ValidateWriteBarrierHelpers();

#endif //TARGET_X86

extern "C"
{
#ifndef FEATURE_EH_FUNCLETS
    void STDCALL JIT_EndCatch();               // JIThelp.asm/JIThelp.s
#endif // FEATURE_EH_FUNCLETS

    void STDCALL JIT_ByRefWriteBarrier();      // JIThelp.asm/JIThelp.s

#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
    void STDCALL JIT_TailCall();                    // JIThelp.asm
#endif // defined(TARGET_X86) && !defined(UNIX_X86_ABI)

    void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle);
#if !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    // TODO: implement stack probing for other architectures https://github.com/dotnet/runtime/issues/13519
    void STDCALL JIT_StackProbe();
#endif // !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
};

/*********************************************************************/
/*********************************************************************/

// Transient data for a MethodDesc involved
// in the current JIT compilation.
struct TransientMethodDetails final
{
    MethodDesc* Method;
    COR_ILMETHOD_DECODER* Header;
    CORINFO_MODULE_HANDLE Scope;

    TransientMethodDetails() = default;
    TransientMethodDetails(MethodDesc* pMD, _In_opt_ COR_ILMETHOD_DECODER* header, CORINFO_MODULE_HANDLE scope);
    TransientMethodDetails(const TransientMethodDetails&) = delete;
    TransientMethodDetails(TransientMethodDetails&&);
    ~TransientMethodDetails();

    TransientMethodDetails& operator=(const TransientMethodDetails&) = delete;
    TransientMethodDetails& operator=(TransientMethodDetails&&);
};

class CEEInfo : public ICorJitInfo
{
    friend class CEEDynamicCodeInfo;

    void GetTypeContext(const CORINFO_SIG_INST* info, SigTypeContext* pTypeContext);
    MethodDesc* GetMethodFromContext(CORINFO_CONTEXT_HANDLE context);
    TypeHandle GetTypeFromContext(CORINFO_CONTEXT_HANDLE context);
    void GetTypeContext(CORINFO_CONTEXT_HANDLE context, SigTypeContext* pTypeContext);

    void HandleException(struct _EXCEPTION_POINTERS* pExceptionPointers);
public:
#include "icorjitinfoimpl_generated.h"
    uint32_t getClassAttribsInternal (CORINFO_CLASS_HANDLE cls);
    bool isObjectImmutableInteral(OBJECTREF obj);

    static unsigned getClassAlignmentRequirementStatic(TypeHandle clsHnd);

    static unsigned getClassGClayoutStatic(TypeHandle th, BYTE* gcPtrs);
    static CorInfoHelpFunc getNewHelperStatic(MethodTable * pMT, bool * pHasSideEffects);
    static CorInfoHelpFunc getNewArrHelperStatic(TypeHandle clsHnd);
    static CorInfoHelpFunc getCastingHelperStatic(TypeHandle clsHnd, bool fThrowing);

    // Returns that compilation flags that are shared between JIT and NGen
    static CORJIT_FLAGS GetBaseCompileFlags(MethodDesc * ftn);

    static CorInfoHelpFunc getSharedStaticsHelper(FieldDesc * pField, MethodTable * pFieldMT);

    static size_t findNameOfToken (Module* module, mdToken metaTOK,
                            _Out_writes_ (FQNameCapacity) char * szFQName, size_t FQNameCapacity);

    DWORD getMethodAttribsInternal (CORINFO_METHOD_HANDLE ftnHnd);

    bool resolveVirtualMethodHelper(CORINFO_DEVIRTUALIZATION_INFO * info);

    CORINFO_CLASS_HANDLE getDefaultComparerClassHelper(
        CORINFO_CLASS_HANDLE elemType
        );

    CORINFO_CLASS_HANDLE getDefaultEqualityComparerClassHelper(
        CORINFO_CLASS_HANDLE elemType
        );

    CORINFO_CLASS_HANDLE getSZArrayHelperEnumeratorClassHelper(
        CORINFO_CLASS_HANDLE elemType
        );


    CorInfoType getFieldTypeInternal (CORINFO_FIELD_HANDLE field, CORINFO_CLASS_HANDLE* structType = NULL,CORINFO_CLASS_HANDLE owner = NULL);

protected:
    void freeArrayInternal(void* array);

public:

    bool getTailCallHelpersInternal(
        CORINFO_RESOLVED_TOKEN* callToken,
        CORINFO_SIG_INFO* sig,
        CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
        CORINFO_TAILCALL_HELPERS* pResult);

    bool getStaticObjRefContent(OBJECTREF obj, uint8_t* buffer, bool ignoreMovableObjects);

    // This normalizes EE type information into the form expected by the JIT.
    //
    // If typeHnd contains exact type information, then *clsRet will contain
    // the normalized CORINFO_CLASS_HANDLE information on return.
    static CorInfoType asCorInfoType (CorElementType cet,
                                      TypeHandle typeHnd = TypeHandle() /* optional in */,
                                      CORINFO_CLASS_HANDLE *clsRet = NULL /* optional out */ );

    CEEInfo(MethodDesc * fd = NULL)
        : m_pJitHandles(nullptr)
        , m_pMethodBeingCompiled(fd)
        , m_transientDetails(NULL)
        , m_pThread(GetThreadNULLOk())
        , m_hMethodForSecurity_Key(NULL)
        , m_pMethodForSecurity_Value(NULL)
#if defined(FEATURE_GDBJIT)
        , m_pCalledMethods(NULL)
#endif
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual ~CEEInfo()
    {
        LIMITED_METHOD_CONTRACT;

#if !defined(DACCESS_COMPILE)
        // Free all handles used by JIT
        if (m_pJitHandles != nullptr)
        {
            OBJECTHANDLE* elements = m_pJitHandles->GetElements();
            unsigned count = m_pJitHandles->GetCount();
            for (unsigned i = 0; i < count; i++)
            {
                DestroyHandle(elements[i]);
            }
            delete m_pJitHandles;
        }

        delete m_transientDetails;
#endif
    }

    // Performs any work JIT-related work that should be performed at process shutdown.
    void JitProcessShutdownWork();

public:
    MethodDesc * GetMethodForSecurity(CORINFO_METHOD_HANDLE callerHandle);

    // Prepare the information about how to do a runtime lookup of the handle with shared
    // generic variables.
    void ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind entryKind,
                                                   CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                   CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken /* for ConstrainedMethodEntrySlot */,
                                                   MethodDesc * pTemplateMD /* for method-based slots */,
                                                   MethodDesc * pCallerMD,
                                                   CORINFO_LOOKUP *pResultLookup);

#if defined(FEATURE_GDBJIT)
    CalledMethod * GetCalledMethods() { return m_pCalledMethods; }
#endif

    // Add/Remove/Find transient method details.
    void AddTransientMethodDetails(TransientMethodDetails details);
    TransientMethodDetails RemoveTransientMethodDetails(MethodDesc* pMD);
    bool FindTransientMethodDetails(MethodDesc* pMD, TransientMethodDetails** details);

protected:
    COR_ILMETHOD_DECODER* getMethodInfoWorker(
        MethodDesc* ftn,
        COR_ILMETHOD_DECODER* header,
        CORINFO_METHOD_INFO* methInfo,
        CORINFO_CONTEXT_HANDLE exactContext = NULL);

protected:
    SArray<OBJECTHANDLE>*   m_pJitHandles;          // GC handles used by JIT
    MethodDesc*             m_pMethodBeingCompiled; // Top-level method being compiled
    SArray<TransientMethodDetails, FALSE>* m_transientDetails;   // Transient details for dynamic codegen scenarios.
    Thread *                m_pThread;              // Cached current thread for faster JIT-EE transitions
    CORJIT_FLAGS            m_jitFlags;

    CORINFO_METHOD_HANDLE getMethodBeingCompiled()
    {
        LIMITED_METHOD_CONTRACT;
        return (CORINFO_METHOD_HANDLE)m_pMethodBeingCompiled;
    }

    CORINFO_OBJECT_HANDLE getJitHandleForObject(OBJECTREF objref, bool knownFrozen = false);
    OBJECTREF getObjectFromJitHandle(CORINFO_OBJECT_HANDLE handle);

    // Cache of last GetMethodForSecurity() lookup
    CORINFO_METHOD_HANDLE   m_hMethodForSecurity_Key;
    MethodDesc *            m_pMethodForSecurity_Value;

#if defined(FEATURE_GDBJIT)
    CalledMethod *          m_pCalledMethods;
#endif

    void EnsureActive(TypeHandle th, MethodDesc * pMD = NULL);
};


/*********************************************************************/

class  EEJitManager;
class  InterpreterJitManager;
class  EECodeGenManager;
struct  HeapList;
struct CodeHeader;

class CEECodeGenInfo : public CEEInfo
{
public:
    // ICorJitInfo stuff
    CEECodeGenInfo(PrepareCodeConfig* config, MethodDesc* fd, COR_ILMETHOD_DECODER* header, EECodeGenManager* jm);

    ~CEECodeGenInfo()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        if (m_CodeHeaderRW != m_CodeHeader)
            freeArrayInternal(m_CodeHeaderRW);

        if (m_pOffsetMapping != NULL)
            freeArrayInternal(m_pOffsetMapping);

        if (m_pNativeVarInfo != NULL)
            freeArrayInternal(m_pNativeVarInfo);
    }

    virtual void ResetForJitRetry()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        if (m_CodeHeaderRW != m_CodeHeader)
            freeArrayInternal(m_CodeHeaderRW);

        m_CodeHeader = NULL;
        m_CodeHeaderRW = NULL;

        m_codeWriteBufferSize = 0;
        m_pRealCodeHeader = NULL;
        m_pCodeHeap = NULL;

        if (m_pOffsetMapping != NULL)
            freeArrayInternal(m_pOffsetMapping);

        if (m_pNativeVarInfo != NULL)
            freeArrayInternal(m_pNativeVarInfo);

        m_iOffsetMapping = 0;
        m_pOffsetMapping = NULL;
        m_iNativeVarInfo = 0;
        m_pNativeVarInfo = NULL;

        if (m_inlineTreeNodes != NULL)
            freeArrayInternal(m_inlineTreeNodes);
        if (m_richOffsetMappings != NULL)
            freeArrayInternal(m_richOffsetMappings);

        m_inlineTreeNodes = NULL;
        m_numInlineTreeNodes = 0;
        m_richOffsetMappings = NULL;
        m_numRichOffsetMappings = 0;
    }

    virtual void BackoutJitData(EECodeGenManager * jitMgr) = 0;

    // ICorDebugInfo stuff.
    void setBoundaries(CORINFO_METHOD_HANDLE ftn,
                       ULONG32 cMap, ICorDebugInfo::OffsetMapping *pMap) override final;
    void setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars,
                 ICorDebugInfo::NativeVarInfo *vars) override final;
    void CompressDebugInfo(PCODE nativeEntry);
    virtual void SetDebugInfo(PTR_BYTE pDebugInfo) = 0;

    virtual PatchpointInfo* GetPatchpointInfo()
    {
        return NULL;
    }

    virtual BOOL JitAgain()
    {
        return FALSE;
    }

    void reportRichMappings(
        ICorDebugInfo::InlineTreeNode*    inlineTreeNodes,
        uint32_t                          numInlineTreeNodes,
        ICorDebugInfo::RichOffsetMapping* mappings,
        uint32_t                          numMappings) override final;

    void reportMetadata(const char* key, const void* value, size_t length) override final;

    virtual void WriteCode(EECodeGenManager * jitMgr) = 0;

    void getHelperFtn(CorInfoHelpFunc         tnNum,                     /* IN  */
                      CORINFO_CONST_LOOKUP *  pNativeEntrypoint,         /* OUT */
                      CORINFO_METHOD_HANDLE * pMethodHandle) override;   /* OUT */
    static PCODE getHelperFtnStatic(CorInfoHelpFunc ftnNum);

    InfoAccessType constructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd, mdToken metaTok, void **ppValue) override;
    InfoAccessType emptyStringLiteral(void ** ppValue) override;
    CORINFO_CLASS_HANDLE getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE field, bool* pIsSpeculative) override;
    void* getMethodSync(CORINFO_METHOD_HANDLE ftnHnd, void **ppIndirection) override;

    CORINFO_METHOD_INFO* getMethodInfoInternal();
    CORJIT_FLAGS* getJitFlagsInternal();

protected:

    template <typename TCodeHeader>
    void setEHcountWorker(unsigned cEH);

    template<class TCodeHeader>
    void SetRealCodeHeader();

    template<class TCodeHeader>
    void NibbleMapSet();

    void getEHinfo(
                    CORINFO_METHOD_HANDLE ftn,              /* IN  */
                    unsigned      EHnumber,                 /* IN */
                    CORINFO_EH_CLAUSE* clause               /* OUT */
                  ) override final;

    void setEHinfoWorker(
                          EE_ILEXCEPTION* pEHInfo,
                          unsigned      EHnumber,
                          const CORINFO_EH_CLAUSE* clause
                        );

    EECodeGenManager*       m_jitManager;   // responsible for allocating memory
    void*                   m_CodeHeader;   // descriptor for JITTED code - read/execute address
    void*                   m_CodeHeaderRW; // descriptor for JITTED code - code write scratch buffer address
    size_t                  m_codeWriteBufferSize;
    BYTE*                   m_pRealCodeHeader;
    HeapList*               m_pCodeHeap;
    COR_ILMETHOD_DECODER*   m_ILHeader;     // the code header to use. This may have been generated due to dynamic IL generation.
    CORINFO_METHOD_INFO     m_MethodInfo;

#if defined(_DEBUG)
    ULONG                   m_codeSize;     // Code size requested via allocMem
#endif

    size_t                  m_GCinfo_len;   // Cached copy of GCinfo_len so we can backout in BackoutJitData()
    size_t                  m_EHinfo_len;   // Cached copy of EHinfo_len so we can backout in BackoutJitData()

    ULONG32                 m_iOffsetMapping;
    ICorDebugInfo::OffsetMapping * m_pOffsetMapping;

    ULONG32                 m_iNativeVarInfo;
    ICorDebugInfo::NativeVarInfo * m_pNativeVarInfo;

    ICorDebugInfo::InlineTreeNode    *m_inlineTreeNodes;
    ULONG32                           m_numInlineTreeNodes;
    ICorDebugInfo::RichOffsetMapping *m_richOffsetMappings;
    ULONG32                           m_numRichOffsetMappings;

    // The first time a call is made to CEEJitInfo::GetProfilingHandle() from this thread
    // for this method, these values are filled in.   Thereafter, these values are used
    // in lieu of calling into the base CEEInfo::GetProfilingHandle() again.  This protects the
    // profiler from duplicate calls to its FunctionIDMapper() callback.
    struct GetProfilingHandleCache
    {
        GetProfilingHandleCache() :
            m_bGphIsCacheValid(false),
            m_bGphHookFunction(false),
            m_pvGphProfilerHandle(NULL)
        {
            LIMITED_METHOD_CONTRACT;
        }

        bool                    m_bGphIsCacheValid : 1;        // Tells us whether below values are valid
        bool                    m_bGphHookFunction : 1;
        void*                   m_pvGphProfilerHandle;
    } m_gphCache;
};

// CEEJitInfo is the concrete implementation of callbacks that the EE must provide for the JIT to do its
// work.   See code:ICorJitInfo#JitToEEInterface for more on this interface.
class CEEJitInfo final : public CEECodeGenInfo
{
public:
    // ICorJitInfo stuff

    void allocMem (AllocMemArgs *pArgs) override;
    void * allocGCInfo(size_t  size) override;
    void setEHcount (unsigned cEH) override;
    void setEHinfo (
        unsigned      EHnumber,
        const CORINFO_EH_CLAUSE* clause
       ) override;

    void WriteCodeBytes();
    void WriteCode(EECodeGenManager * jitMgr) override;

    void reserveUnwindInfo(bool isFunclet, bool isColdCode, uint32_t unwindSize) override;

    void allocUnwindInfo (
            uint8_t * pHotCode,              /* IN */
            uint8_t * pColdCode,             /* IN */
            uint32_t  startOffset,           /* IN */
            uint32_t  endOffset,             /* IN */
            uint32_t  unwindSize,            /* IN */
            uint8_t * pUnwindBlock,          /* IN */
            CorJitFuncKind funcKind       /* IN */
            ) override;

    HRESULT allocPgoInstrumentationBySchema(
            CORINFO_METHOD_HANDLE ftnHnd, /* IN */
            PgoInstrumentationSchema* pSchema, /* IN/OUT */
            uint32_t countSchemaItems, /* IN */
            uint8_t** pInstrumentationData /* OUT */
            ) override;

    HRESULT getPgoInstrumentationResults(
            CORINFO_METHOD_HANDLE ftnHnd, /* IN */
            PgoInstrumentationSchema** pSchema, /* OUT */
            uint32_t* pCountSchemaItems, /* OUT */
            uint8_t**pInstrumentationData, /* OUT */
            PgoSource *pPgoSource, /* OUT */
            bool* pDynamicPgo /* OUT */
            ) override;

    void recordCallSite(
            uint32_t                     instrOffset,  /* IN */
            CORINFO_SIG_INFO *        callSig,      /* IN */
            CORINFO_METHOD_HANDLE     methodHandle  /* IN */
            ) override;

    void recordRelocation(
            void                    *location,
            void                    *locationRW,
            void                    *target,
            uint16_t                 fRelocType,
            int32_t                  addlDelta) override;

    uint16_t getRelocTypeHint(void * target) override;

    uint32_t getExpectedTargetArchitecture() override;

    void BackoutJitData(EECodeGenManager * jitMgr) override;
    void SetDebugInfo(PTR_BYTE pDebugInfo) override;

    void ResetForJitRetry() override
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        CEECodeGenInfo::ResetForJitRetry();

#ifdef FEATURE_ON_STACK_REPLACEMENT
        if (m_pPatchpointInfoFromJit != NULL)
            freeArrayInternal(m_pPatchpointInfoFromJit);

        m_pPatchpointInfoFromJit = NULL;
#endif

#ifdef FEATURE_EH_FUNCLETS
        m_moduleBase = (TADDR)0;
        m_totalUnwindSize = 0;
        m_usedUnwindSize = 0;
        m_theUnwindBlock = NULL;
        m_totalUnwindInfos = 0;
        m_usedUnwindInfos = 0;
#endif // FEATURE_EH_FUNCLETS
    }

#ifdef TARGET_AMD64
    void SetAllowRel32(BOOL fAllowRel32)
    {
        LIMITED_METHOD_CONTRACT;
        m_fAllowRel32 = fAllowRel32;
    }
#endif

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    void SetJumpStubOverflow(BOOL fJumpStubOverflow)
    {
        LIMITED_METHOD_CONTRACT;
        m_fJumpStubOverflow = fJumpStubOverflow;
    }

    BOOL IsJumpStubOverflow()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fJumpStubOverflow;
    }

    BOOL JitAgain() override
    {
        LIMITED_METHOD_CONTRACT;
        return m_fJumpStubOverflow;
    }

    size_t GetReserveForJumpStubs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_reserveForJumpStubs;
    }

    void SetReserveForJumpStubs(size_t value)
    {
        LIMITED_METHOD_CONTRACT;
        m_reserveForJumpStubs = value;
    }

    PatchpointInfo* GetPatchpointInfo() override
    {
#ifdef FEATURE_ON_STACK_REPLACEMENT
        return m_pPatchpointInfoFromJit;
#else
        return NULL;
#endif
    }

#else
    BOOL JitAgain() override
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    size_t GetReserveForJumpStubs()
    {
        LIMITED_METHOD_CONTRACT;
        return 0;
    }
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)

#ifdef FEATURE_ON_STACK_REPLACEMENT
    // Called by the runtime to supply patchpoint information to the jit.
    void SetOSRInfo(PatchpointInfo* patchpointInfo, unsigned ilOffset)
    {
        _ASSERTE(m_pPatchpointInfoFromRuntime == NULL);
        _ASSERTE(patchpointInfo != NULL);
        m_pPatchpointInfoFromRuntime = patchpointInfo;
        m_ilOffset = ilOffset;
    }
#endif

    void PublishFinalCodeAddress(PCODE addr);

    CEEJitInfo(PrepareCodeConfig* config, MethodDesc* fd, COR_ILMETHOD_DECODER* header,
               EECodeGenManager* jm)
        : CEECodeGenInfo(config, fd, header, jm)
#ifdef FEATURE_EH_FUNCLETS
        , m_moduleBase(0),
          m_totalUnwindSize(0),
          m_usedUnwindSize(0),
          m_theUnwindBlock(NULL),
          m_totalUnwindInfos(0),
          m_usedUnwindInfos(0)
#endif
#ifdef TARGET_AMD64
        , m_fAllowRel32(FALSE)
#endif
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
        , m_fJumpStubOverflow(FALSE),
          m_reserveForJumpStubs(0)
#endif
#ifdef FEATURE_ON_STACK_REPLACEMENT
        , m_pPatchpointInfoFromJit(NULL),
          m_pPatchpointInfoFromRuntime(NULL),
          m_ilOffset(0)
#endif
        , m_finalCodeAddressSlot(NULL)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;
    }

    ~CEEJitInfo()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

#ifdef FEATURE_ON_STACK_REPLACEMENT
        if (m_pPatchpointInfoFromJit != NULL)
            freeArrayInternal(m_pPatchpointInfoFromJit);
#endif
#ifdef FEATURE_PGO
        if (m_foundPgoData != NULL)
        {
            ComputedPgoData* current = m_foundPgoData;
            while (current != NULL)
            {
                ComputedPgoData* next = current->m_next;
                delete current;
                current = next;
            }
        }
#endif
    }

    // Override of CEEInfo::GetProfilingHandle.  The first time this is called for a
    // method desc, it calls through to CEEInfo::GetProfilingHandle and caches the
    // result in CEEJitInfo::GetProfilingHandleCache.  Thereafter, this wrapper regurgitates the cached values
    // rather than calling into CEEInfo::GetProfilingHandle each time.  This avoids
    // making duplicate calls into the profiler's FunctionIDMapper callback.
    void GetProfilingHandle(
                    bool                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    bool                      *pbIndirectedHandles
                    ) override;

    void setPatchpointInfo(PatchpointInfo* patchpointInfo) override;
    PatchpointInfo* getOSRInfo(unsigned* ilOffset) override;

    virtual CORINFO_METHOD_HANDLE getAsyncResumptionStub() override final;

protected :

#ifdef FEATURE_PGO
    // PGO data
    struct ComputedPgoData
    {
        ComputedPgoData(MethodDesc* pMD) : m_pMD(pMD) {}

        ComputedPgoData* m_next = nullptr;
        MethodDesc *m_pMD;
        NewArrayHolder<BYTE> m_allocatedData;
        PgoInstrumentationSchema* m_schema = nullptr;
        UINT32 m_cSchemaElems;
        BYTE *m_pInstrumentationData = nullptr;
        HRESULT m_hr = E_NOTIMPL;
        PgoSource m_pgoSource = PgoSource::Unknown;
    };
    ComputedPgoData*        m_foundPgoData = nullptr;
#endif


#ifdef FEATURE_EH_FUNCLETS
    TADDR                   m_moduleBase;       // Base for unwind Infos
    ULONG                   m_totalUnwindSize;  // Total reserved unwind space
    uint32_t                m_usedUnwindSize;   // used space in m_theUnwindBlock
    BYTE *                  m_theUnwindBlock;   // start of the unwind memory block
    ULONG                   m_totalUnwindInfos; // Number of RUNTIME_FUNCTION needed
    ULONG                   m_usedUnwindInfos;
#endif

#ifdef TARGET_AMD64
    BOOL                    m_fAllowRel32;      // Use 32-bit PC relative address modes
#endif
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    BOOL                    m_fJumpStubOverflow;   // Overflow while trying to alocate jump stub slot within PC relative branch region
                                                   // The code will need to be regenerated (with m_fRel32Allowed == FALSE for AMD64).
    size_t                  m_reserveForJumpStubs; // Space to reserve for jump stubs when allocating code
#endif

#ifdef FEATURE_ON_STACK_REPLACEMENT
    PatchpointInfo        * m_pPatchpointInfoFromJit;
    PatchpointInfo        * m_pPatchpointInfoFromRuntime;
    unsigned                m_ilOffset;
#endif
    PCODE* m_finalCodeAddressSlot;

};

#ifdef FEATURE_INTERPRETER
class CInterpreterJitInfo final : public CEECodeGenInfo
{
public:
    // ICorJitInfo stuff

    CInterpreterJitInfo(PrepareCodeConfig* config, MethodDesc* fd, COR_ILMETHOD_DECODER* header,
                        EECodeGenManager* jm)
        : CEECodeGenInfo(config, fd, header, jm)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;
    }

    void allocMem(AllocMemArgs *pArgs) override;
    void * allocGCInfo(size_t  size) override;
    void setEHcount (unsigned cEH) override;
    void setEHinfo (
        unsigned      EHnumber,
        const CORINFO_EH_CLAUSE* clause
       ) override;

    void WriteCodeBytes();
    void WriteCode(EECodeGenManager * jitMgr) override;

    void BackoutJitData(EECodeGenManager * jitMgr) override;
    void SetDebugInfo(PTR_BYTE pDebugInfo) override;

    LPVOID GetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig) override;
};
#endif // FEATURE_INTERPRETER

/*********************************************************************/
/*********************************************************************/

typedef struct {
    void * pfnHelper;
#ifdef _DEBUG
    const char* name;
#endif
} VMHELPDEF;

#if defined(DACCESS_COMPILE)

GARY_DECL(VMHELPDEF, hlpFuncTable, CORINFO_HELP_COUNT);

#else

extern "C" const VMHELPDEF hlpFuncTable[CORINFO_HELP_COUNT];

#endif

// enum for dynamically assigned helper calls
enum DynamicCorInfoHelpFunc {
#define JITHELPER(code, pfnHelper, binderId)
#define DYNAMICJITHELPER(code, pfnHelper, binderId) DYNAMIC_##code,
#include "jithelpers.h"
    DYNAMIC_CORINFO_HELP_COUNT
};

#ifdef _MSC_VER
// GCC complains about duplicate "extern". And it is not needed for the GCC build
extern "C"
#endif
GARY_DECL(VMHELPDEF, hlpDynamicFuncTable, DYNAMIC_CORINFO_HELP_COUNT);

#define SetJitHelperFunction(ftnNum, pFunc) _SetJitHelperFunction(DYNAMIC_##ftnNum, (void*)(pFunc))
void    _SetJitHelperFunction(DynamicCorInfoHelpFunc ftnNum, void * pFunc);

VMHELPDEF LoadDynamicJitHelper(DynamicCorInfoHelpFunc ftnNum, MethodDesc** methodDesc = NULL);
bool HasILBasedDynamicJitHelper(DynamicCorInfoHelpFunc ftnNum);
bool IndirectionAllowedForJitHelper(CorInfoHelpFunc ftnNum);

void *GenFastGetSharedStaticBase(bool bCheckCCtor);

#ifdef HAVE_GCCOVER
void SetupGcCoverage(NativeCodeVersion nativeCodeVersion, BYTE* nativeCode);
void SetupGcCoverageForNativeImage(Module* module);
BOOL OnGcCoverageInterrupt(PT_CONTEXT regs);
void DoGcStress (PT_CONTEXT regs, NativeCodeVersion nativeCodeVersion);
#endif //HAVE_GCCOVER

BOOL ObjIsInstanceOf(Object *pObject, TypeHandle toTypeHnd, BOOL throwCastException = FALSE);

class InlinedCallFrame;
EXTERN_C Thread * JIT_InitPInvokeFrame(InlinedCallFrame *pFrame);

#ifdef _DEBUG
extern LONG g_JitCount;
#endif

struct VirtualFunctionPointerArgs
{
    CORINFO_CLASS_HANDLE classHnd;
    CORINFO_METHOD_HANDLE methodHnd;
};

struct StaticFieldAddressArgs
{
    PCODE staticBaseHelper;
    TADDR arg0;
    SIZE_T offset;
};

struct GenericHandleArgs
{
    LPVOID signature;
    CORINFO_MODULE_HANDLE module;
    DWORD dictionaryIndexAndSlot;
};

CORJIT_FLAGS GetDebuggerCompileFlags(Module* pModule, CORJIT_FLAGS flags);

bool __stdcall TrackAllocationsEnabled();


extern Volatile<int64_t> g_cbILJitted;
extern Volatile<int64_t> g_cMethodsJitted;
extern Volatile<int64_t> g_c100nsTicksInJit;
extern thread_local int64_t t_cbILJittedForThread;
extern thread_local int64_t t_cMethodsJittedForThread;
extern thread_local int64_t t_c100nsTicksInJitForThread;

FCDECL1(INT64, GetCompiledILBytes, FC_BOOL_ARG currentThread);
FCDECL1(INT64, GetCompiledMethodCount, FC_BOOL_ARG currentThread);
FCDECL1(INT64, GetCompilationTimeInTicks, FC_BOOL_ARG currentThread);

#endif // JITINTERFACE_H
