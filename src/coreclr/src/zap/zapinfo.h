// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapInfo.h
//

//
// JIT-EE interface for zapping
// 
// ======================================================================================

#ifndef __ZAPINFO_H__
#define __ZAPINFO_H__

#include "zapcode.h"

class ZapInfo;
struct InlineContext;

// The compiled code often implicitly needs fixups for various subtle reasons.
// We only emit explict fixups while compiling the method, while collecting
// implicit fixups in the LoadTable. At the end of compiling, we expect
// many of the LoadTable entries to be subsumed by the explicit entries
// and will not need to be emitted.
// This is also used to detect duplicate explicit fixups for the same type.

template <typename HandleType>
class LoadTable
{
private:
    ZapImage            *m_pModule;

    struct LoadEntry
    {
        HandleType              handle;
        int                     order;      // -1 = fixed
    };

    static int __cdecl LoadEntryCmp(const void* a_, const void* b_)
    {
        return ((LoadEntry*)a_)->order - ((LoadEntry*)b_)->order;
    }

    class LoadEntryTraits : public NoRemoveSHashTraits< DefaultSHashTraits<LoadEntry> >
    {
    public:
        typedef typename NoRemoveSHashTraits<DefaultSHashTraits<LoadEntry> >::count_t count_t;
        typedef typename NoRemoveSHashTraits<DefaultSHashTraits<LoadEntry> >::element_t element_t;
        typedef HandleType key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.handle;
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; LoadEntry e; e.handle = NULL; e.order = 0; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return (e.handle == NULL); }
    };

    typedef SHash<LoadEntryTraits> LoadEntryHashTable;

    LoadEntryHashTable      m_entries;

public:
    LoadTable(ZapImage *pModule)
      : m_pModule(pModule)
    {
    }

    // fixed=TRUE if the caller can guarantee that type will be fixed up because
    // of some implicit fixup. In this case, we track 'handle' only to avoid
    // duplicates and will not actually emit an explicit fixup for 'handle'
    //
    // fixed=FALSE if the caller needs an explicit fixup. We will emit an
    // explicit fixup for 'handle' if there are no other implicit fixups.

    void Load(HandleType handle, BOOL fixed)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        const LoadEntry *result = m_entries.LookupPtr(handle);

        if (result != NULL)
        {
            if (fixed)
                ((LoadEntry*)result)->order = -1;
            return;
        }

        LoadEntry   newEntry;

        newEntry.handle = handle;
        newEntry.order = fixed ? -1 : m_entries.GetCount();

        m_entries.Add(newEntry);
    }

    void EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);
};

// Declare some specializations of EmitLoadFixups().
template<> void LoadTable<CORINFO_CLASS_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);
template<> void LoadTable<CORINFO_METHOD_HANDLE>::EmitLoadFixups(CORINFO_METHOD_HANDLE currentMethodHandle, ZapInfo * pZapInfo);


class ZapInfo
    : public ICorJitInfo
{
    friend class ZapImage;

    // Owning ZapImage
    ZapImage * m_pImage;

    Zapper * m_zapper;
    ICorDynamicInfo * m_pEEJitInfo;
    ICorCompileInfo * m_pEECompileInfo;

    // Current method being compiled; it is non-nil only for
    // method defs whose IL is in this module and (for generic code)
    // have <object> instantiation. It is also nil for IL_STUBs.
    mdMethodDef                 m_currentMethodToken;
    CORINFO_METHOD_HANDLE       m_currentMethodHandle;
    CORINFO_METHOD_INFO         m_currentMethodInfo;

    // m_currentMethodModule==m_hModule except for generic types/methods
    // defined in another assembly but instantiated in the current assembly.
    CORINFO_MODULE_HANDLE       m_currentMethodModule;

    unsigned                    m_currentMethodProfilingDataFlags;

    // Debug information reported by the JIT compiler for the current method
    ICorDebugInfo::NativeVarInfo *m_pNativeVarInfo;
    ULONG32                     m_iNativeVarInfo;
    ICorDebugInfo::OffsetMapping *m_pOffsetMapping;
    ULONG32                     m_iOffsetMapping;

    BYTE *                      m_pGCInfo;
    SIZE_T                      m_cbGCInfo;


    ZapBlobWithRelocs *         m_pCode;
    ZapBlobWithRelocs *         m_pColdCode;
    ZapBlobWithRelocs *         m_pROData;

#ifdef WIN64EXCEPTIONS
    // Unwind info of the main method body. It will get merged with GC info.
    BYTE *                      m_pMainUnwindInfo;
    ULONG                       m_cbMainUnwindInfo;

    ZapUnwindInfo *             m_pUnwindInfo;
    ZapUnwindInfo *             m_pUnwindInfoFragments;
#if defined(_TARGET_AMD64_)
    ZapUnwindInfo *             m_pChainedColdUnwindInfo;
#endif
#endif // WIN64EXCEPTIONS

    ZapExceptionInfo *          m_pExceptionInfo;

    ZapBlobWithRelocs *         m_pProfileData;

    ZapImport *                 m_pProfilingHandle;

    struct CodeRelocation : ZapReloc
    {
        ZapBlobWithRelocs * m_pNode;
    };

    SArray<CodeRelocation>   m_CodeRelocations;

    static int __cdecl CompareCodeRelocation(const void * a, const void * b);

    struct ImportEntry
    {
        ZapImport * pImport;
        bool fConditional; // Conditional imports are emitted only if they are actually referenced by the code.
    };

    class ImportTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ImportEntry> >
    {
    public:
        typedef ZapImport * key_t;

        static key_t GetKey(element_t e)
        { 
            LIMITED_METHOD_CONTRACT;
            return e.pImport;
        }
        static BOOL Equals(key_t k1, key_t k2) 
        { 
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k) 
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)(size_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; ImportEntry e; e.pImport = NULL; return e; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pImport == NULL; }
    };

    SHash<ImportTraits>         m_ImportSet;
    SArray<ZapImport *>         m_Imports;

    InlineSString<128>          m_currentMethodName;

    // Cache to reduce the number of entries in CORCOMPILE_LOAD_TABLE if it
    // is implied by some other fixup type
    LoadTable<CORINFO_CLASS_HANDLE>  m_ClassLoadTable;
    LoadTable<CORINFO_METHOD_HANDLE> m_MethodLoadTable;

    CORJIT_FLAGS m_jitFlags;

    void InitMethodName();

    CORJIT_FLAGS ComputeJitFlags(CORINFO_METHOD_HANDLE handle);

    ZapDebugInfo * EmitDebugInfo();
    ZapGCInfo * EmitGCInfo();
    ZapImport ** EmitFixupList();

    void PublishCompiledMethod();

    void EmitCodeRelocations();

    void ProcessReferences();

    BOOL CurrentMethodHasProfileData();

    void embedGenericSignature(CORINFO_LOOKUP * pLookup);

    PVOID embedDirectCall(CORINFO_METHOD_HANDLE ftn, 
                          CORINFO_ACCESS_FLAGS accessFlags,
                          BOOL fAllowThunk);

public:
    ZapInfo(ZapImage * pImage, mdMethodDef md, CORINFO_METHOD_HANDLE handle, CORINFO_MODULE_HANDLE module, unsigned methodProfilingDataFlags);
    ~ZapInfo();

#ifdef ALLOW_SXS_JIT_NGEN
    void ResetForJitRetry();
#endif // ALLOW_SXS_JIT_NGEN

    void CompileMethod();

    void AppendImport(ZapImport * pImport);
    void AppendConditionalImport(ZapImport * pImport);

    ULONG GetNumFixups();

    // ICorJitInfo

    IEEMemoryManager* getMemoryManager();

    virtual void allocMem (
            ULONG               hotCodeSize,    /* IN */
            ULONG               coldCodeSize,   /* IN */
            ULONG               roDataSize,     /* IN */
            ULONG               xcptnsCount,    /* IN */
            CorJitAllocMemFlag  flag,           /* IN */
            void **             hotCodeBlock,   /* OUT */
            void **             coldCodeBlock,  /* OUT */
            void **             roDataBlock     /* OUT */
            );

    void    reserveUnwindInfo(
            BOOL isFunclet,               /* IN */
            BOOL isColdCode,              /* IN */
            ULONG unwindSize              /* IN */
            );

    void    allocUnwindInfo (
            BYTE * pHotCode,              /* IN */
            BYTE * pColdCode,             /* IN */
            ULONG  startOffset,           /* IN */
            ULONG  endOffset,             /* IN */
            ULONG  unwindSize,            /* IN */
            BYTE * pUnwindBlock,          /* IN */
            CorJitFuncKind funcKind       /* IN */
            );

    void * allocGCInfo(size_t size);
    void yieldExecution();
    void setEHcount(unsigned cEH);
    void setEHinfo(unsigned EHnumber, const CORINFO_EH_CLAUSE *clause);

    int  canHandleException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    BOOL logMsg(unsigned level,  const char *fmt, va_list args);
    int  doAssert(const char* szFile, int iLine, const char* szExpr);
    void reportFatalError(CorJitResult result);

    HRESULT allocBBProfileBuffer (
            ULONG cBlock,
            ICorJitInfo::ProfileBuffer ** ppBlock);

    HRESULT getBBProfileData (
            CORINFO_METHOD_HANDLE ftnHnd,
            ULONG * size,
            ICorJitInfo::ProfileBuffer ** profileBuffer,
            ULONG * numRuns);

    DWORD getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes);

    bool runWithErrorTrap(void (*function)(void*), void* param);

    // ICorDynamicInfo

    DWORD getThreadTLSIndex(void **ppIndirection);
    const void * getInlinedCallFrameVptr(void **ppIndirection);
    LONG * getAddrOfCaptureThreadGlobal(void **ppIndirection);

    // get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*). 
    // Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
    CorInfoHelpFunc getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle);

    CORINFO_MODULE_HANDLE
      embedModuleHandle(CORINFO_MODULE_HANDLE handle,
                        void **ppIndirection);
    CORINFO_CLASS_HANDLE
      embedClassHandle(CORINFO_CLASS_HANDLE handle,
                       void **ppIndirection);
    CORINFO_FIELD_HANDLE
      embedFieldHandle(CORINFO_FIELD_HANDLE handle,
                       void **ppIndirection);
    CORINFO_METHOD_HANDLE
      embedMethodHandle(CORINFO_METHOD_HANDLE handle,
                        void **ppIndirection);
    void
      embedGenericHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                         BOOL                     fEmbedParent,
                         CORINFO_GENERICHANDLE_RESULT *pResult);

    CORINFO_LOOKUP_KIND
        getLocationOfThisType(CORINFO_METHOD_HANDLE context);



    void * getHelperFtn (CorInfoHelpFunc   ftnNum,
                                  void**            ppIndirection);

    void* getTailCallCopyArgsThunk (
                      CORINFO_SIG_INFO       *pSig,
                      CorInfoHelperTailCallSpecialHandling flags);

    void getFunctionEntryPoint(
                      CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
                      CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
                      CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY);

    void getFunctionFixedEntryPoint(
                      CORINFO_METHOD_HANDLE   ftn,
                      CORINFO_CONST_LOOKUP *  pResult);


    void * getMethodSync(CORINFO_METHOD_HANDLE ftn,
                         void **ppIndirection);
    void * getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method,
                                     void **ppIndirection);
    void * getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method,
                                    void **ppIndirection);
    void getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method,
                                   CORINFO_CONST_LOOKUP *pLookup);
    CORINFO_JUST_MY_CODE_HANDLE getJustMyCodeHandle(
                        CORINFO_METHOD_HANDLE method,
                        CORINFO_JUST_MY_CODE_HANDLE **ppIndirection);

    ZapImport * GetProfilingHandleImport();

    void GetProfilingHandle(
                    BOOL                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    BOOL                      *pbIndirectedHandles
                    );

    void getCallInfo(
                        // Token info
                        CORINFO_RESOLVED_TOKEN * pResolvedToken,
                        //Generics info
                        CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                        //Security info
                        CORINFO_METHOD_HANDLE   callerHandle,
                        //Jit info
                        CORINFO_CALLINFO_FLAGS  flags,
                        //out params
                        CORINFO_CALL_INFO       *pResult);

    BOOL canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                                   CORINFO_CLASS_HANDLE hInstanceType);


    BOOL isRIDClassDomainID(CORINFO_CLASS_HANDLE cls);

    unsigned getClassDomainID(CORINFO_CLASS_HANDLE cls,
                                        void **ppIndirection);

    void * getFieldAddress(CORINFO_FIELD_HANDLE field,
                                    void **ppIndirection);
    DWORD getFieldThreadLocalStoreID (CORINFO_FIELD_HANDLE field,
                                                void **ppIndirection);
    CORINFO_VARARGS_HANDLE getVarArgsHandle(CORINFO_SIG_INFO *sig,
                                                      void **ppIndirection);
    bool canGetVarArgsHandle(CORINFO_SIG_INFO *sig);

    InfoAccessType constructStringLiteral(CORINFO_MODULE_HANDLE module,
                                            unsigned metaTok, void **ppIndirection);

    InfoAccessType emptyStringLiteral(void **ppIndirection);

    void setOverride(ICorDynamicInfo *pOverride, CORINFO_METHOD_HANDLE currentMethod);

    void addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom, CORINFO_MODULE_HANDLE moduleTo);

    void recordCallSite(ULONG instrOffset, CORINFO_SIG_INFO *callSig, CORINFO_METHOD_HANDLE methodHandle);

    // Relocations

    void recordRelocation(void *location, void *target,
                                    WORD fRelocType, WORD slotNum, INT32 addlDelta);

    WORD getRelocTypeHint(void * target);

    void getModuleNativeEntryPointRange(void** pStart, void** pEnd);

    DWORD getExpectedTargetArchitecture();

    // ICorJitInfo delegate ctor optimization
    CORINFO_METHOD_HANDLE GetDelegateCtor(
                            CORINFO_METHOD_HANDLE   methHnd,
                            CORINFO_CLASS_HANDLE    clsHnd,
                            CORINFO_METHOD_HANDLE   targetMethodHnd,
                            DelegateCtorArgs *      pCtorData);

    void MethodCompileComplete(
                CORINFO_METHOD_HANDLE methHnd);

    // ICorStaticInfo

    void getEEInfo(CORINFO_EE_INFO *pEEInfoOut);

    LPCWSTR getJitTimeLogFilename();

    // ICorArgInfo

    CORINFO_ARG_LIST_HANDLE getArgNext(CORINFO_ARG_LIST_HANDLE args);
    CorInfoTypeWithMod getArgType(CORINFO_SIG_INFO* sig,
                                     CORINFO_ARG_LIST_HANDLE args,
                                     CORINFO_CLASS_HANDLE *vcTypeRet);
    CORINFO_CLASS_HANDLE getArgClass(CORINFO_SIG_INFO* sig,
                                               CORINFO_ARG_LIST_HANDLE args);
    CorInfoType getHFAType(CORINFO_CLASS_HANDLE hClass);

    // ICorDebugInfo

    void getBoundaries(CORINFO_METHOD_HANDLE ftn, unsigned int *cILOffsets,
                       DWORD **pILOffsets, ICorDebugInfo::BoundaryTypes *implicitBoundaries);
    void setBoundaries(CORINFO_METHOD_HANDLE ftn, ULONG32 cMap,
                       ICorDebugInfo::OffsetMapping *pMap);
    void getVars(CORINFO_METHOD_HANDLE ftn, ULONG32 *cVars,
                 ICorDebugInfo::ILVarInfo **vars, bool *extendOthers);
    void setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars,
                 ICorDebugInfo::NativeVarInfo*vars);
    void * allocateArray(ULONG cBytes);
    void freeArray(void *array);

    // ICorFieldInfo

    const char* getFieldName(CORINFO_FIELD_HANDLE ftn, const char **moduleName);
    CORINFO_CLASS_HANDLE getFieldClass(CORINFO_FIELD_HANDLE field);

    CorInfoType getFieldType(CORINFO_FIELD_HANDLE field,
                                       CORINFO_CLASS_HANDLE *structType,
                                       CORINFO_CLASS_HANDLE memberParent);

    unsigned getFieldOffset(CORINFO_FIELD_HANDLE field);

    bool isWriteBarrierHelperRequired(
                        CORINFO_FIELD_HANDLE    field);

    void getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                       CORINFO_METHOD_HANDLE  callerHandle,
                       CORINFO_ACCESS_FLAGS   flags,
                       CORINFO_FIELD_INFO    *pResult);

    bool isFieldStatic(CORINFO_FIELD_HANDLE fldHnd);

    // ICorClassInfo

    CorInfoType asCorInfoType(CORINFO_CLASS_HANDLE cls);
    const char* getClassName(CORINFO_CLASS_HANDLE cls);
    const char* getHelperName(CorInfoHelpFunc ftnNum);
    int appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf, int* pnBufLen,
                                  CORINFO_CLASS_HANDLE    cls,
                                  BOOL fNamespace,
                                  BOOL fFullInst,
                                  BOOL fAssembly);
    BOOL isValueClass(CORINFO_CLASS_HANDLE clsHnd);
    BOOL canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE clsHnd);
    DWORD getClassAttribs(CORINFO_CLASS_HANDLE cls);
    BOOL isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls);
    CORINFO_MODULE_HANDLE getClassModule(CORINFO_CLASS_HANDLE cls);
    CORINFO_ASSEMBLY_HANDLE getModuleAssembly(CORINFO_MODULE_HANDLE mod);
    const char* getAssemblyName(CORINFO_ASSEMBLY_HANDLE assem);
    void* LongLifetimeMalloc(size_t sz);
    void LongLifetimeFree(void* obj);
    size_t getClassModuleIdForStatics(CORINFO_CLASS_HANDLE cls, CORINFO_MODULE_HANDLE *pModule, void **ppIndirection);

    unsigned getClassSize(CORINFO_CLASS_HANDLE cls);
    unsigned getClassAlignmentRequirement(CORINFO_CLASS_HANDLE cls, BOOL fDoubleAlignHint);

    CORINFO_FIELD_HANDLE getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num);

    mdMethodDef getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod);
    BOOL checkMethodModifier(CORINFO_METHOD_HANDLE hMethod, LPCSTR modifier, BOOL fOptional);

    unsigned getClassGClayout(CORINFO_CLASS_HANDLE cls, BYTE *gcPtrs);

    bool getSystemVAmd64PassStructInRegisterDescriptor(
        /*IN*/  CORINFO_CLASS_HANDLE _structHnd,
        /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr);

    unsigned getClassNumInstanceFields(CORINFO_CLASS_HANDLE cls);

    CorInfoHelpFunc getNewHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_METHOD_HANDLE callerHandle);
    CorInfoHelpFunc getCastingHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, bool fThrowing);
    CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_HANDLE arrayCls);
    CorInfoHelpFunc getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd);
    CorInfoHelpFunc getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn);
    CORINFO_CLASS_HANDLE getTypeForBox(CORINFO_CLASS_HANDLE  cls);
    CorInfoHelpFunc getBoxHelper(CORINFO_CLASS_HANDLE cls);
    CorInfoHelpFunc getUnBoxHelper(CORINFO_CLASS_HANDLE cls);

    bool getReadyToRunHelper(
            CORINFO_RESOLVED_TOKEN *        pResolvedToken,
            CORINFO_LOOKUP_KIND *           pGenericLookupKind,
            CorInfoHelpFunc                 id,
            CORINFO_CONST_LOOKUP *          pLookup
            );

    void getReadyToRunDelegateCtorHelper(
            CORINFO_RESOLVED_TOKEN * pTargetMethod,
            CORINFO_CLASS_HANDLE     delegateType,
            CORINFO_LOOKUP *   pLookup
            );

    CorInfoInitClassResult initClass(
            CORINFO_FIELD_HANDLE    field,
            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONTEXT_HANDLE  context,
            BOOL                    speculative = FALSE);

    void classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE cls);
    void methodMustBeLoadedBeforeCodeIsRun(CORINFO_METHOD_HANDLE meth);
    CORINFO_METHOD_HANDLE mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE methHnd);
    CORINFO_CLASS_HANDLE getBuiltinClass(CorInfoClassId classId);
    CorInfoType getTypeForPrimitiveValueClass(CORINFO_CLASS_HANDLE cls);
    BOOL canCast(CORINFO_CLASS_HANDLE child, CORINFO_CLASS_HANDLE parent);
    BOOL areTypesEquivalent(CORINFO_CLASS_HANDLE cls1, CORINFO_CLASS_HANDLE cls2);
    CORINFO_CLASS_HANDLE mergeClasses(CORINFO_CLASS_HANDLE cls1,
                                CORINFO_CLASS_HANDLE cls2);
    BOOL shouldEnforceCallvirtRestriction(CORINFO_MODULE_HANDLE scope);
    CORINFO_CLASS_HANDLE getParentType(CORINFO_CLASS_HANDLE  cls);
    CorInfoType getChildType (CORINFO_CLASS_HANDLE       clsHnd,
                              CORINFO_CLASS_HANDLE       *clsRet);
    BOOL satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls);

    BOOL     isSDArray   (CORINFO_CLASS_HANDLE  cls);
    unsigned getArrayRank(CORINFO_CLASS_HANDLE  cls);
    void * getArrayInitializationData(CORINFO_FIELD_HANDLE field, DWORD size);
    CorInfoIsAccessAllowedResult canAccessClass(CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                CORINFO_METHOD_HANDLE   callerHandle,
                                                CORINFO_HELPER_DESC    *throwHelper);

    // ICorModuleInfo

    void resolveToken(CORINFO_RESOLVED_TOKEN * pResolvedToken);
    bool tryResolveToken(CORINFO_RESOLVED_TOKEN * pResolvedToken);

    void findSig(CORINFO_MODULE_HANDLE module, unsigned sigTOK,
                 CORINFO_CONTEXT_HANDLE context,
                 CORINFO_SIG_INFO *sig);
    void findCallSiteSig(CORINFO_MODULE_HANDLE module,
                                   unsigned methTOK,
                                   CORINFO_CONTEXT_HANDLE context,
                                   CORINFO_SIG_INFO *sig);
    CORINFO_CLASS_HANDLE getTokenTypeAsHandle(CORINFO_RESOLVED_TOKEN * pResolvedToken);
    size_t findNameOfToken(CORINFO_MODULE_HANDLE module,
                           unsigned metaTOK,
                           __out_ecount (FQNameCapacity) char * szFQName,
                           size_t FQNameCapacity);
    CorInfoCanSkipVerificationResult canSkipVerification (CORINFO_MODULE_HANDLE module);
    BOOL isValidToken(CORINFO_MODULE_HANDLE module,
                      unsigned metaTOK);
    BOOL isValidStringRef(CORINFO_MODULE_HANDLE module,
                          unsigned metaTOK);


    // ICorMethodInfo

    const char* getMethodName(CORINFO_METHOD_HANDLE ftn,
                                        const char **moduleName);
    unsigned getMethodHash(CORINFO_METHOD_HANDLE ftn);
    DWORD getMethodAttribs(CORINFO_METHOD_HANDLE ftn);
    void setMethodAttribs(CORINFO_METHOD_HANDLE ftn, CorInfoMethodRuntimeFlags attribs);

    void getMethodSig(CORINFO_METHOD_HANDLE ftn, CORINFO_SIG_INFO *sig, CORINFO_CLASS_HANDLE memberParent);

    bool getMethodInfo(CORINFO_METHOD_HANDLE ftn,
                       CORINFO_METHOD_INFO* info);

    CorInfoInline canInline(CORINFO_METHOD_HANDLE caller,
                            CORINFO_METHOD_HANDLE callee,
                            DWORD*                pRestrictions);

    void reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                 CORINFO_METHOD_HANDLE inlineeHnd,
                                 CorInfoInline inlineResult,
                                 const char * reason);

    CorInfoInstantiationVerification isInstantiationOfVerifiedGeneric(
            CORINFO_METHOD_HANDLE   method);

    void initConstraintsForVerification(CORINFO_METHOD_HANDLE method,
                                        BOOL *pfHasCircularClassConstraints,
                                        BOOL *pfHasCircularMethodConstraints);

    bool canTailCall(CORINFO_METHOD_HANDLE caller,
                     CORINFO_METHOD_HANDLE declaredCallee,
                     CORINFO_METHOD_HANDLE exactCallee,
                     bool fIsTailPrefix);

    void reportTailCallDecision (CORINFO_METHOD_HANDLE callerHnd,
                                 CORINFO_METHOD_HANDLE calleeHnd,
                                 bool fIsTailPrefix,
                                 CorInfoTailCall tailCallResult,
                                 const char * reason);

    CorInfoCanSkipVerificationResult canSkipMethodVerification (
            CORINFO_METHOD_HANDLE   callerHnd);
    
    void getEHinfo(CORINFO_METHOD_HANDLE ftn,
                             unsigned EHnumber, CORINFO_EH_CLAUSE* clause);
    CORINFO_CLASS_HANDLE getMethodClass(CORINFO_METHOD_HANDLE method);
    CORINFO_MODULE_HANDLE getMethodModule(CORINFO_METHOD_HANDLE method);

    void getMethodVTableOffset(CORINFO_METHOD_HANDLE method,
                               unsigned * pOffsetOfIndirection,
                               unsigned * pOffsetAfterIndirection,
                               unsigned * isRelative);

    CORINFO_METHOD_HANDLE resolveVirtualMethod(
        CORINFO_METHOD_HANDLE virtualMethod,
        CORINFO_CLASS_HANDLE implementingClass,
        CORINFO_CONTEXT_HANDLE ownerType
        );

    void expandRawHandleIntrinsic(
        CORINFO_RESOLVED_TOKEN *        pResolvedToken,
        CORINFO_GENERICHANDLE_RESULT *  pResult);

    CorInfoIntrinsics getIntrinsicID(CORINFO_METHOD_HANDLE method,
                                     bool * pMustExpand = NULL);
    bool isInSIMDModule(CORINFO_CLASS_HANDLE classHnd);
    CorInfoUnmanagedCallConv getUnmanagedCallConv(CORINFO_METHOD_HANDLE method);
    BOOL pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig);
    LPVOID GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig,
                                       void ** ppIndirecton);
    bool canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig);
    BOOL satisfiesMethodConstraints(CORINFO_CLASS_HANDLE parent,
                                              CORINFO_METHOD_HANDLE method);

    BOOL isCompatibleDelegate(CORINFO_CLASS_HANDLE objCls,
                              CORINFO_CLASS_HANDLE methodParentCls,
                              CORINFO_METHOD_HANDLE method,
                              CORINFO_CLASS_HANDLE delegateCls,
                              BOOL* pfIsOpenDelegate);

    void getGSCookie(GSCookie * pCookieVal, 
                     GSCookie** ppCookieVal);
    // ICorErrorInfo

    HRESULT GetErrorHRESULT(struct _EXCEPTION_POINTERS *pExceptionPointers);
    ULONG GetErrorMessage(__in_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength);
    int FilterException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    void HandleException(struct _EXCEPTION_POINTERS *pExceptionPointers);
    void ThrowExceptionForJitResult(HRESULT result);
    void ThrowExceptionForHelper(const CORINFO_HELPER_DESC * throwHelper);
};

#endif // __ZAPINFO_H__
