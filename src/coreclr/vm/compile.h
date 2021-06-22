// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: compile.h
//
// Interfaces and support for zap compiler and zap files
//

// ===========================================================================


/*

The preloader is used to serialize internal EE data structures in the
zapped image.  The object model looks like the following:

                    +--------------------+
                    |                    |
                    |    ZapperModule    |
                    |                    |
                    +--------------------+
                              |
                              *
                     ICorCompileDataStore           Zapper

           =====================================================

                     ICorCompilePreloader           EE
                              *
                              |
                    +--------------------+
                    |                    |
                    |    CEEPreloader    |
                    |                    |
                    +--------------------+
                              |
                              *
                     DataImage::IDataStore


                    +--------------------+
                    |                    |
                    |     DataImage      |
                    |                    |
                    +--------------------+

ZapperModule - Created by the zapper for each module.  It implements the
               ICorCompileDataStore interface that the preloader uses to
               allocate space for the EE data structures.  Currently it
               allocates space in a single PE section (though the DataImage
               has logic to further subdivide the space into subsections).

CEEPreloader - Created by ZapperModule in order to serialize EE
               data structures.  It implements two interfaces.
               ICorCompilePreloader is used by ZapperModule to inquire
               about the offsets of various EE data structures inside
               the preloader section.  DataImage::IDataStore is used
               by DataImage to manage the PE section memory, and the
               implementation in the CEEPreloader mostly forwards the calls
               to the zapper (ICorCompileDataStore).

DataImage    - Created by CEEPreloader to keep track of memory used by
               EE data structures.  Even though it uses only one PE
               section, it allows the EE to allocate memory in multiple
               subsections.  This is accomplished by splitting the work into
               three phases (there are comments in dataimage.h that explain
               this in detail).


The CEEPreloader is created when ZapperModule::Preload calls
m_zapper->m_pEECompileInfo->PreloadModule.  PreloadModule creates
the CEEPreloader and then calls its Preload method, which explicitely
loads all the EE objects into memory (Module::ExpandAll), and then
allocates space for them in the preloader section (Module::Save).

Each EE data structure that needs to be serialized implements a Save
method.  A Save method is required to:
1) Store all of its data (including strings and other buffers that it
   uses) in the preloader section.  This is accomplished by calling on
   one of the DataImage storage methods (such as DataImage::StoreStructure).
2) Call the Save method on the objects that it owns.  The interesting
   part of the hierarchy looks like:

   Module::Save
     MethodTable::Save (in profile order)
       EEClass::Save
         MethodDescChunk::Save (method desc chunks can be split into hot
                                and cold based on profile info)
           MethodDesc::Save

Note that while the architecture requires the data structures in the
preloader sections to look like their EE counterparts, it is possible
to work around that limitation by constructing multiple submappings of
these data structures.  Sometimes the submappings require a change to the actual
data (i.e. each method desc has information that tells you how far it is
from the MethodDescChunk, and that needs to change when reordering method
descs).  In such cases you create new copies of that memory and construct
a regular copying map for each of the new updated copies (DataImage::StoreStructure),
and a pointer update map for each of the original EE data structures
(DataImage::StoreStructureUsingSurrogate).  See MethodDescChunk::Save for
an example on how to do this.

Fixups:  once everything has been layout out in memory, the ZapperModule
calls CEEPreloader::Link to generate fixups for the data.  CEEPreloader::Link
calls Module::Fixup, which results in a data structure walk very similar to
that of Module::Save.  Each data structure calls one of the FixupPointerField
methods on the DataImage, which in turn forwards the call to
CEEPreloader::AddFixup, which forwards it to the zapper
(ZapperModule::AddFixup).

*/

#ifndef COMPILE_H_
#define COMPILE_H_

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

struct ZapperLoaderModuleTableKey {
    ZapperLoaderModuleTableKey(Module *pDefinitionModule,
        mdToken token,
        Instantiation classInst,
        Instantiation methodInst)
        : m_inst(classInst, methodInst)
    { WRAPPER_NO_CONTRACT;
      this->m_pDefinitionModule = pDefinitionModule;
      this->m_token = token;  }

    Module *m_pDefinitionModule;
    mdToken m_token;
    SigTypeContext m_inst;
} ;

struct ZapperLoaderModuleTableEntry {
    ZapperLoaderModuleTableEntry(): key(0,0,Instantiation(),Instantiation()) { WRAPPER_NO_CONTRACT; this->result = 0; }
    ZapperLoaderModuleTableEntry(const ZapperLoaderModuleTableKey &_key,Module *_result)
        : key(_key)
    { this->result = _result; }

    ZapperLoaderModuleTableKey key;
    Module *result;
} ;

class ZapperLoaderModuleTableTraits : public NoRemoveSHashTraits<DefaultSHashTraits<ZapperLoaderModuleTableEntry> >
{

public:
    typedef const ZapperLoaderModuleTableKey *key_t;
    static const ZapperLoaderModuleTableKey * GetKey(const ZapperLoaderModuleTableEntry &e) { return &e.key; }
    static count_t Hash(const ZapperLoaderModuleTableKey * k)
    {
        LIMITED_METHOD_CONTRACT;

        DWORD dwHash = 5381;

        dwHash = ((dwHash << 5) + dwHash) ^ (unsigned int)(SIZE_T)k->m_pDefinitionModule;
        dwHash = ((dwHash << 5) + dwHash) ^ (unsigned int)(SIZE_T)k->m_token;
        dwHash = ((dwHash << 5) + dwHash) ^ EEInstantiationHashTableHelper:: Hash(&k->m_inst);
        return dwHash;
    }

    static BOOL Equals(const ZapperLoaderModuleTableKey *e1, const ZapperLoaderModuleTableKey *e2)
    {
        WRAPPER_NO_CONTRACT;
        return e1->m_pDefinitionModule == e2->m_pDefinitionModule &&
            e1->m_token == e2->m_token &&
            SigTypeContext::Equal(&e1->m_inst, &e2->m_inst);
    }
    static const ZapperLoaderModuleTableEntry Null()
    { return ZapperLoaderModuleTableEntry(); }

    static bool IsNull(const ZapperLoaderModuleTableEntry &e)
    { LIMITED_METHOD_CONTRACT; return e.key.m_pDefinitionModule == 0 && e.key.m_token == 0 && e.key.m_inst.IsEmpty(); }

};


typedef  SHash<ZapperLoaderModuleTableTraits> ZapperLoaderModuleTable;

class CEECompileInfo : public ICorCompileInfo
{
  public:
    CEECompileInfo()
       : m_fGeneratingNgenPDB(FALSE)
    {
    }

    virtual ~CEECompileInfo()
    {
        WRAPPER_NO_CONTRACT;
    }

    HRESULT Startup(     BOOL                     fForceDebug,
                         BOOL                     fForceProfiling,
                         BOOL                     fForceInstrument);

    HRESULT CreateDomain(ICorCompilationDomain **ppDomain,
                         IMetaDataAssemblyEmit    *pEmitter,
                         BOOL                     fForceDebug,
                         BOOL                     fForceProfiling,
                         BOOL                     fForceInstrument);

    HRESULT DestroyDomain(ICorCompilationDomain   *pDomain);

    HRESULT LoadAssemblyByPath(LPCWSTR                  wzPath,
                               BOOL                     fExplicitBindToNativeImage,
                               CORINFO_ASSEMBLY_HANDLE *pHandle);

    BOOL IsInCurrentVersionBubble(CORINFO_MODULE_HANDLE hModule);

    HRESULT LoadAssemblyModule(CORINFO_ASSEMBLY_HANDLE assembly,
                               mdFile                  file,
                               CORINFO_MODULE_HANDLE   *pHandle);


    BOOL CheckAssemblyZap(
        CORINFO_ASSEMBLY_HANDLE assembly,
      __out_ecount_opt(*cAssemblyManifestModulePath)
        LPWSTR                  assemblyManifestModulePath,
        LPDWORD                 cAssemblyManifestModulePath);

    HRESULT SetCompilationTarget(CORINFO_ASSEMBLY_HANDLE     assembly,
                                 CORINFO_MODULE_HANDLE       module);

    IMDInternalImport * GetAssemblyMetaDataImport(CORINFO_ASSEMBLY_HANDLE scope);

    IMDInternalImport * GetModuleMetaDataImport(CORINFO_MODULE_HANDLE scope);

    CORINFO_MODULE_HANDLE GetAssemblyModule(CORINFO_ASSEMBLY_HANDLE module);

    CORINFO_ASSEMBLY_HANDLE GetModuleAssembly(CORINFO_MODULE_HANDLE module);

    PEDecoder * GetModuleDecoder(CORINFO_MODULE_HANDLE scope);

    void GetModuleFileName(CORINFO_MODULE_HANDLE module,
                           SString               &result);

    void EncodeModuleAsIndex( CORINFO_MODULE_HANDLE   fromHandle,
                              CORINFO_MODULE_HANDLE   handle,
                              DWORD                   *pIndex,
                              IMetaDataAssemblyEmit   *pAssemblyEmit);

    void EncodeClass(  CORINFO_MODULE_HANDLE   referencingModule,
                       CORINFO_CLASS_HANDLE    classHandle,
                       SigBuilder              *pSigBuilder,
                       LPVOID                  encodeContext,
                       ENCODEMODULE_CALLBACK   pfnEncodeModule);

    void EncodeMethod( CORINFO_MODULE_HANDLE   referencingModule,
                       CORINFO_METHOD_HANDLE   methHnd,
                       SigBuilder              *pSigBuilder,
                       LPVOID                  encodeContext,
                       ENCODEMODULE_CALLBACK   pfnEncodeModule,
                       CORINFO_RESOLVED_TOKEN  *pResolvedToken,
                       CORINFO_RESOLVED_TOKEN  *pConstrainedResolvedToken,
                       BOOL                    fEncodeUsingResolvedTokenSpecStreams);

    virtual mdToken TryEncodeMethodAsToken(CORINFO_METHOD_HANDLE handle,
                                           CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                           CORINFO_MODULE_HANDLE * referencingModule);

    virtual DWORD TryEncodeMethodSlot(CORINFO_METHOD_HANDLE handle);

    void EncodeField(  CORINFO_MODULE_HANDLE   referencingModule,
                       CORINFO_FIELD_HANDLE    handle,
                       SigBuilder              *pSigBuilder,
                       LPVOID                  encodeContext,
                       ENCODEMODULE_CALLBACK   pfnEncodeModule,
                       CORINFO_RESOLVED_TOKEN  *pResolvedToken,
                       BOOL                    fEncodeUsingResolvedTokenSpecStreams);

    // Encode generic dictionary signature
    virtual void EncodeGenericSignature(
            LPVOID signature,
            BOOL fMethod,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule);


    BOOL IsEmptyString(mdString token,
                       CORINFO_MODULE_HANDLE module);

    BOOL IsUnmanagedCallConvMethod(CORINFO_METHOD_HANDLE handle);

    BOOL IsUnmanagedCallersOnlyMethod(CORINFO_METHOD_HANDLE handle);

    BOOL IsCachingOfInliningHintsEnabled()
    {
        return m_fCachingOfInliningHintsEnabled;
    }

    void DisableCachingOfInliningHints()
    {
        m_fCachingOfInliningHintsEnabled = FALSE;
    }

    HRESULT GetTypeDef(   CORINFO_CLASS_HANDLE    classHandle,
                          mdTypeDef               *token);
    HRESULT GetMethodDef( CORINFO_METHOD_HANDLE   methodHandle,
                          mdMethodDef             *token);
    HRESULT GetFieldDef(  CORINFO_FIELD_HANDLE    fieldHandle,
                          mdFieldDef              *token);

    void SetAssemblyHardBindList(__in_ecount( cHardBindList )
                                 LPWSTR *pHardBindList,
                                 DWORD cHardBindList);

    CORINFO_MODULE_HANDLE GetLoaderModuleForCoreLib();
    CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableType(CORINFO_CLASS_HANDLE classHandle);
    CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableMethod(CORINFO_METHOD_HANDLE methodHandle);
    CORINFO_MODULE_HANDLE GetLoaderModuleForEmbeddableField(CORINFO_FIELD_HANDLE fieldHandle);

    ICorCompilePreloader * PreloadModule(CORINFO_MODULE_HANDLE   moduleHandle,
                                    ICorCompileDataStore    *pData,
                                    CorProfileData          *profileData);


    HRESULT GetLoadHint(CORINFO_ASSEMBLY_HANDLE   hAssembly,
                        CORINFO_ASSEMBLY_HANDLE hAssemblyDependency,
                        LoadHintEnum           *loadHint,
                        LoadHintEnum           *defaultLoadHint);

    HRESULT GetAssemblyVersionInfo(CORINFO_ASSEMBLY_HANDLE Handle,
                                    CORCOMPILE_VERSION_INFO *pInfo);

    void GetAssemblyCodeBase(CORINFO_ASSEMBLY_HANDLE hAssembly,
                             SString                &result);

    void GetCallRefMap(CORINFO_METHOD_HANDLE hMethod,
                       GCRefMapBuilder * pBuilder,
                       bool isDispatchCell);

    void CompressDebugInfo(
                                    IN ICorDebugInfo::OffsetMapping * pOffsetMapping,
                                    IN ULONG            iOffsetMapping,
                                    IN ICorDebugInfo::NativeVarInfo * pNativeVarInfo,
                                    IN ULONG            iNativeVarInfo,
                                    IN OUT SBuffer    * pDebugInfoBuffer);

    HRESULT SetVerboseLevel(
                                    IN  VerboseLevel        level);

    HRESULT GetBaseJitFlags(
            IN  CORINFO_METHOD_HANDLE    hMethod,
            OUT CORJIT_FLAGS            *pFlags);

    ICorJitHost* GetJitHost();

    void* GetStubSize(void *pStubAddress, DWORD *pSizeToCopy);

    HRESULT GetStubClone(void *pStub, BYTE *pBuffer, DWORD dwBufferSize);

    BOOL GetIsGeneratingNgenPDB();
    void SetIsGeneratingNgenPDB(BOOL fGeneratingNgenPDB);

#ifdef FEATURE_READYTORUN_COMPILER
    CORCOMPILE_FIXUP_BLOB_KIND GetFieldBaseOffset(
            CORINFO_CLASS_HANDLE classHnd,
            DWORD * pBaseOffset);

    BOOL NeedsTypeLayoutCheck(CORINFO_CLASS_HANDLE classHnd);
    void EncodeTypeLayout(CORINFO_CLASS_HANDLE classHandle, SigBuilder * pSigBuilder);

    BOOL AreAllClassesFullyLoaded(CORINFO_MODULE_HANDLE moduleHandle);

    int GetVersionResilientTypeHashCode(CORINFO_MODULE_HANDLE moduleHandle, mdToken token);

    int GetVersionResilientMethodHashCode(CORINFO_METHOD_HANDLE methodHandle);

    BOOL EnumMethodsForStub(CORINFO_METHOD_HANDLE hMethod, void** enumerator);
    BOOL EnumNextMethodForStub(void * enumerator, CORINFO_METHOD_HANDLE *hMethod);
    void EnumCloseForStubEnumerator(void *enumerator);
#endif

    BOOL HasCustomAttribute(CORINFO_METHOD_HANDLE method, LPCSTR customAttributeName);

    //--------------------------------------------------------------------
    // ZapperLoaderModules and the ZapperLoaderModuleTable
    //
    // When NGEN'ing we want to adjust the
    // places where some items (i.e. generic instantiations) are placed, in order to get some of them
    // placed into the module we are compiling.  However, the
    // results of ComputeLoaderModule must be stable for the duration
    // of an entire instance of the VM, i.e. for the duration of a compilation
    // process.  Thus each time we place an item into a non-standard LoaderModule we record
    // that fact.

    Module *LookupZapperLoaderModule(const ZapperLoaderModuleTableKey *pKey)
    {
        WRAPPER_NO_CONTRACT;
        const ZapperLoaderModuleTableEntry *pEntry = m_ZapperLoaderModuleTable.LookupPtr(pKey);
        if (pEntry)
            return pEntry->result;
        return NULL;
    }

    void RecordZapperLoaderModule(const ZapperLoaderModuleTableKey *pKey,
                                  Module *pZapperLoaderModuleTable)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        ZapperLoaderModuleTableEntry entry(*pKey, pZapperLoaderModuleTable);
        m_ZapperLoaderModuleTable.Add(entry);
    }

    ZapperLoaderModuleTable m_ZapperLoaderModuleTable;

private:
    BOOL m_fCachingOfInliningHintsEnabled;
    BOOL m_fGeneratingNgenPDB;
};

extern CEECompileInfo *g_pCEECompileInfo;

BOOL IsNgenPDBCompilationProcess();

//
// See comment at top of file for an explanation on the preloader
// architecture.
//

class CEEPreloader : public ICorCompilePreloader
{
  private:
    DataImage              *m_image;
    ICorCompileDataStore   *m_pData;

    class MethodSetTraits : public NoRemoveSHashTraits< DefaultSHashTraits<MethodDesc *> >
    {
    public:
        typedef MethodDesc *key_t;
        static MethodDesc * GetKey(MethodDesc *md) { return md; }
        static count_t Hash(MethodDesc *md) { return (count_t) (UINT_PTR) md; }
        static BOOL Equals(MethodDesc *md1, MethodDesc *md2)
        {
            return md1 == md2;
        }
    };

    class TypeSetTraits : public NoRemoveSHashTraits< DefaultSHashTraits<TypeHandle> >
    {
    public:
        typedef TypeHandle key_t;
        static const TypeHandle Null() { return TypeHandle(); }
        static bool IsNull(const TypeHandle &th) { return !!th.IsNull(); }
        static TypeHandle GetKey(TypeHandle th) { return th; }
        static count_t Hash(TypeHandle th) { return (count_t) th.AsTAddr(); }
        static BOOL Equals(TypeHandle th1, TypeHandle th2) { return th1 == th2; }
    };

    // Cached results of instantiations triage
    SHash<TypeSetTraits>    m_acceptedTypes;
    SHash<MethodSetTraits>  m_acceptedMethods;
    SHash<TypeSetTraits>    m_rejectedTypes;
    SHash<MethodSetTraits>  m_rejectedMethods;

#ifdef FEATURE_FULL_NGEN
    // Tentatively accepted instantiations
    InlineSArray<TypeHandle, 20>    m_speculativeTypes;
    BOOL                            m_fSpeculativeTriage;
    BOOL                            m_fDictionariesPopulated;
#endif

    struct CompileMethodEntry
    {
        MethodDesc * pMD;
#ifndef FEATURE_FULL_NGEN // Unreferenced methods
        bool fReferenced; // true when this method was referenced by other code
        bool fScheduled;  // true when this method was scheduled for compilation
#endif
    };

    class CompileMethodSetTraits : public NoRemoveSHashTraits< DefaultSHashTraits<CompileMethodEntry> >
    {
    public:
        typedef MethodDesc *key_t;
        static MethodDesc * GetKey(CompileMethodEntry e) { return e.pMD; }
        static count_t Hash(MethodDesc *md) { return (count_t) (UINT_PTR) md; }
        static BOOL Equals(MethodDesc *md1, MethodDesc *md2)
        {
            return md1 == md2;
        }
        static const CompileMethodEntry Null() { CompileMethodEntry e; e.pMD = NULL; return e; }
        static bool IsNull(const CompileMethodEntry &e) { return e.pMD == NULL; }
    };

    SHash<CompileMethodSetTraits> m_compileMethodsHash;

    // Array of methods that we need to compile.
    SArray<MethodDesc*> m_uncompiledMethods;

    int m_methodCompileLimit;

    void AppendUncompiledMethod(MethodDesc *pMD)
    {
        STANDARD_VM_CONTRACT;
        if (m_methodCompileLimit > 0)
        {
            m_uncompiledMethods.Append(pMD);
            m_methodCompileLimit--;
        }
    }

    struct DuplicateMethodEntry
    {
        MethodDesc * pMD;
        MethodDesc * pDuplicateMD;
    };

    class DuplicateMethodTraits : public NoRemoveSHashTraits< DefaultSHashTraits<DuplicateMethodEntry> >
    {
    public:
        typedef MethodDesc *key_t;
        static MethodDesc * GetKey(DuplicateMethodEntry e) { return e.pMD; }
        static count_t Hash(MethodDesc *md) { return (count_t) (UINT_PTR) md; }
        static BOOL Equals(MethodDesc *md1, MethodDesc *md2)
        {
            return md1 == md2;
        }
        static const DuplicateMethodEntry Null() { DuplicateMethodEntry e; e.pMD = NULL; return e; }
        static bool IsNull(const DuplicateMethodEntry &e) { return e.pMD == NULL; }
    };

    SHash<DuplicateMethodTraits> m_duplicateMethodsHash;

    MethodDesc * CompileMethodStubIfNeeded(
            MethodDesc *pMD,
            MethodDesc *pStubMD,
            ICorCompilePreloader::CORCOMPILE_CompileStubCallback pfnCallback,
            LPVOID pCallbackContext);

  public:
    CEEPreloader(Module *pModule,
                 ICorCompileDataStore *pData);
    virtual ~CEEPreloader();

    void Preload(CorProfileData * profileData);
    DataImage * GetDataImage() { LIMITED_METHOD_CONTRACT; return m_image; }
    ICorCompileDataStore * GetDataStore() { LIMITED_METHOD_CONTRACT; return m_pData; }

    //
    // ICorCompilerPreloader
    //

    DWORD MapMethodEntryPoint(CORINFO_METHOD_HANDLE handle);
    DWORD MapClassHandle(CORINFO_CLASS_HANDLE handle);
    DWORD MapMethodHandle(CORINFO_METHOD_HANDLE handle);
    DWORD MapFieldHandle(CORINFO_FIELD_HANDLE handle);
    DWORD MapAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE handle);
    DWORD MapGenericHandle(CORINFO_GENERIC_HANDLE handle);
    DWORD MapModuleIDHandle(CORINFO_MODULE_HANDLE handle);

    void AddMethodToTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE handle);
    void AddTypeToTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE handle);
    BOOL IsMethodInTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE handle);
    BOOL IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE handle);

    void MethodReferencedByCompiledCode(CORINFO_METHOD_HANDLE handle);

    BOOL IsUncompiledMethod(CORINFO_METHOD_HANDLE handle);
    BOOL ShouldSuppressGCTransition(CORINFO_METHOD_HANDLE handle);

private:
    void AddToUncompiledMethods(MethodDesc *pMethod, BOOL fForStubs);

    void ApplyTypeDependencyProductionsForType(TypeHandle t);
    void ApplyTypeDependencyForSZArrayHelper(MethodTable * pInterfaceMT, TypeHandle elemTypeHnd);

    friend class Module;
    void TriageTypeForZap(TypeHandle th, BOOL fAcceptIfNotSure, BOOL fExpandDependencies = TRUE);
    void TriageMethodForZap(MethodDesc* pMethod, BOOL fAcceptIfNotSure, BOOL fExpandDependencies = TRUE);

    void ExpandTypeDependencies(TypeHandle th);
    void ExpandMethodDependencies(MethodDesc * pMD);

    void TriageTypeSpecsFromSoftBoundModule(Module * pSoftBoundModule);
    void TriageTypeFromSoftBoundModule(TypeHandle th, Module * pSoftBoundModule);
    void TriageSpeculativeType(TypeHandle th);
    void TriageSpeculativeInstantiations();

    // Returns TRUE if new types or methods have been added by the triage
    BOOL TriageForZap(BOOL fAcceptIfNotSure, BOOL fExpandDependencies = TRUE);

public:
    CORINFO_METHOD_HANDLE NextUncompiledMethod();

    void PrePrepareMethodIfNecessary(CORINFO_METHOD_HANDLE hMethod);

    void GenerateMethodStubs(
            CORINFO_METHOD_HANDLE hMethod,
            bool                  fNgenProfileImage,
            CORCOMPILE_CompileStubCallback pfnCallback,
            LPVOID                pCallbackContext);

    bool IsDynamicMethod(CORINFO_METHOD_HANDLE hMethod);
    void SetMethodProfilingFlags(CORINFO_METHOD_HANDLE hMethod, DWORD flags);

    bool CanSkipMethodPreparation (
            CORINFO_METHOD_HANDLE   callerHnd,      /* IN  */
            CORINFO_METHOD_HANDLE   calleeHnd,      /* IN  */
            CorInfoIndirectCallReason *pReason = NULL,
            CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY);

    BOOL CanEmbedClassID     (CORINFO_CLASS_HANDLE    typeHandle);
    BOOL CanEmbedModuleID    (CORINFO_MODULE_HANDLE   moduleHandle);
    BOOL CanEmbedModuleHandle(CORINFO_MODULE_HANDLE   moduleHandle);
    BOOL CanEmbedClassHandle (CORINFO_CLASS_HANDLE    typeHandle);
    BOOL CanEmbedMethodHandle(CORINFO_METHOD_HANDLE   methodHandle,
                              CORINFO_METHOD_HANDLE   contextHandle);
    BOOL CanEmbedFieldHandle (CORINFO_FIELD_HANDLE    fieldHandle);

    BOOL CanPrerestoreEmbedClassHandle (CORINFO_CLASS_HANDLE  classHnd);
    BOOL CanPrerestoreEmbedMethodHandle(CORINFO_METHOD_HANDLE methodHnd);

    BOOL CanEmbedFunctionEntryPoint(CORINFO_METHOD_HANDLE   methodHandle,
                                    CORINFO_METHOD_HANDLE   contextHandle,
                                    CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY);

    BOOL DoesMethodNeedRestoringBeforePrestubIsRun(CORINFO_METHOD_HANDLE   methodHandle);

    BOOL CanSkipDependencyActivation(CORINFO_METHOD_HANDLE   context,
                                     CORINFO_MODULE_HANDLE   moduleFrom,
                                     CORINFO_MODULE_HANDLE   moduleTo);

    CORINFO_MODULE_HANDLE GetPreferredZapModuleForClassHandle(CORINFO_CLASS_HANDLE classHnd);

    void NoteDeduplicatedCode(CORINFO_METHOD_HANDLE method, CORINFO_METHOD_HANDLE duplicateMethod);

    CORINFO_METHOD_HANDLE LookupMethodDef(mdMethodDef token);
    bool GetMethodInfo(mdMethodDef token, CORINFO_METHOD_HANDLE ftnHnd, CORINFO_METHOD_INFO * methInfo);

    CorCompileILRegion GetILRegion(mdMethodDef token);

    CORINFO_METHOD_HANDLE FindMethodForProfileEntry(CORBBTPROF_BLOB_PARAM_SIG_ENTRY * profileBlobEntry);

    void ReportInlining(CORINFO_METHOD_HANDLE inliner, CORINFO_METHOD_HANDLE inlinee);

    void Link();
    void FixupRVAs();

    void SetRVAsForFields(IMetaDataEmit * pEmit);

    void GetRVAFieldData(mdFieldDef fd, PVOID * ppData, DWORD * pcbSize, DWORD * pcbAlignment);

    ULONG Release();

#ifdef FEATURE_READYTORUN_COMPILER
    void GetSerializedInlineTrackingMap(SBuffer* pBuffer);
#endif

    void Error(mdToken token, Exception * pException);
};


struct RefCache
{
    RefCache(Module *pModule)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
        }
        CONTRACTL_END


        m_pModule = pModule;

        {
            // HashMap::Init can throw due to OOM. Our ctor can't. Since this whole
            // thing is for use inside CEECompileInfo methods, it doesn't make sense to
            // use an exception model. Thus we probably have to move the hashmap init
            // calls out of the ctor so can catch these exceptions and translate them to
            // hresults.
            //
            CONTRACT_VIOLATION(ThrowsViolation|FaultViolation);

            m_sAssemblyRefMap.Init(FALSE,NULL);
        }
    }

    Module *m_pModule;

    HashMap m_sAssemblyRefMap;
};

struct AssemblySpecDefRefMapEntry {
    AssemblySpec * m_pDef;
    AssemblySpec * m_pRef;
};

class AssemblySpecDefRefMapTraits : public NoRemoveSHashTraits<DefaultSHashTraits<AssemblySpecDefRefMapEntry> >
{
public:
    typedef const AssemblySpec *key_t;
    static const AssemblySpec * GetKey(const AssemblySpecDefRefMapEntry &e) { return e.m_pDef; }

    static count_t Hash(const AssemblySpec * k)
    {
        return const_cast<AssemblySpec *>(k)->Hash();
    }

    static BOOL Equals(const AssemblySpec * lhs, const AssemblySpec * rhs)
    {
        return const_cast<AssemblySpec *>(lhs)->CompareEx(const_cast<AssemblySpec *>(rhs), AssemblySpec::ASC_DefinitionEquality);
    }

    static const AssemblySpecDefRefMapEntry Null() { AssemblySpecDefRefMapEntry e; e.m_pDef = NULL; return e; }
    static bool IsNull(const AssemblySpecDefRefMapEntry &e) { return e.m_pDef == NULL; }

    void OnDestructPerEntryCleanupAction(const AssemblySpecDefRefMapEntry& e)
    {
        WRAPPER_NO_CONTRACT;
        delete e.m_pDef;
        delete e.m_pRef;
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

typedef SHash<AssemblySpecDefRefMapTraits> AssemblySpecMapDefRefMapTable;

class CompilationDomain : public AppDomain,
                          public ICorCompilationDomain
{

 public:
    BOOL                    m_fForceDebug;
    BOOL                    m_fForceProfiling;
    BOOL                    m_fForceInstrument;

    // TODO:  During ngen, we need to determine whether we can call NeedsRestore
    // before the preloader has been initialized.  This is accomplished via this
    // method.  This code needs to be cleaned up.  See bug #284709 for background.
    BOOL canCallNeedsRestore() { return  (m_pTargetImage != NULL); };

    // DDB 175659: Make sure that canCallNeedsRestore() returns FALSE during compilation
    // domain shutdown.
    void setCannotCallNeedsRestore() { m_pTargetImage = NULL; }

  private:

    Assembly                *m_pTargetAssembly;     // Assembly being compiled
    Module                  *m_pTargetModule;       // Module currently being compiled. Needed for multi-module assemblies
    DataImage               *m_pTargetImage;        // Data image
    CEEPreloader            *m_pTargetPreloader;

    ReleaseHolder<IMetaDataAssemblyEmit>    m_pEmit;

    NewHolder<AssemblySpecHash>             m_pDependencyRefSpecs;

    AssemblySpecMapDefRefMapTable           m_dependencyDefRefMap;

    CORCOMPILE_DEPENDENCY   *m_pDependencies;
    USHORT                   m_cDependenciesCount, m_cDependenciesAlloc;

    CQuickArray<RefCache*> m_rRefCaches;

    HRESULT AddDependencyEntry(PEAssembly *pFile, mdAssemblyRef ref,mdAssemblyRef def);
    void ReleaseDependencyEmitter();


  public:

#ifndef DACCESS_COMPILE
    CompilationDomain(BOOL fForceDebug = FALSE,
                      BOOL fForceProfiling = FALSE,
                      BOOL fForceInstrument = FALSE);
    ~CompilationDomain();
#endif

    void Init();

    HRESULT AddDependency(AssemblySpec *pRefSpec, PEAssembly *pFile);

    AssemblySpec* FindAssemblyRefSpecForDefSpec(
        AssemblySpec* pDefSpec);

    PEAssembly *BindAssemblySpec(
        AssemblySpec *pSpec,
        BOOL fThrowOnFileNotFound) DAC_EMPTY_RET(NULL);

    BOOL CanEagerBindToZapFile(Module *targetModule, BOOL limitToHardBindList = TRUE);



    // Returns NULL on out-of-memory
    RefCache *GetRefCache(Module *pModule)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            INJECT_FAULT(return NULL;);
        }
        CONTRACTL_END

        unsigned uSize = (unsigned) m_rRefCaches.Size();
        for (unsigned i = 0; i < uSize; i++)
            if (m_rRefCaches[i]->m_pModule == pModule)
                return m_rRefCaches[i];

        // Add a new cache entry
        HRESULT hr;

        if (FAILED(hr = m_rRefCaches.ReSizeNoThrow(uSize + 1)))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            return NULL;
        }

        m_rRefCaches[uSize] = new (nothrow) RefCache(pModule);
        return m_rRefCaches[uSize];
    }

    void SetTarget(Assembly * pAssembly, Module *pModule);

    void SetTargetImage(DataImage * pImage, CEEPreloader * pPreloader);
    DataImage * GetTargetImage() { LIMITED_METHOD_CONTRACT; return m_pTargetImage; }

    Assembly * GetTargetAssembly()
        { LIMITED_METHOD_CONTRACT; return m_pTargetAssembly; }
    Module * GetTargetModule()
        { LIMITED_METHOD_CONTRACT; return m_pTargetModule; }

    // ICorCompilationDomain

    HRESULT SetContextInfo(LPCWSTR exePath, BOOL isExe) DAC_EMPTY_RET(E_FAIL);
    HRESULT GetDependencies(CORCOMPILE_DEPENDENCY **ppDependencies,
                            DWORD *cDependencies) DAC_EMPTY_RET(E_FAIL);

    void SetDependencyEmitter(IMetaDataAssemblyEmit *pEmitter);
};

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // COMPILE_H_
