// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _NIDUMP_H_
#define _NIDUMP_H_


#ifdef FEATURE_PREJIT
#include <daccess.h>

//some DPTR definitions that aren't elsewhere in the source
typedef DPTR(const COR_SIGNATURE) PTR_CCOR_SIGNATURE;
typedef DPTR(IMAGE_SECTION_HEADER) PTR_IMAGE_SECTION_HEADER;
typedef DPTR(struct CerRoot) PTR_CerRoot;
typedef DPTR(DictionaryEntry) PTR_DictionaryEntry;
typedef DPTR(GuidInfo) PTR_GuidInfo;
#if defined(FEATURE_COMINTEROP)
typedef DPTR(SparseVTableMap) PTR_SparseVTableMap;
#endif
#if defined(FEATURE_COMINTEROP)
typedef DPTR(ClassFactoryBase) PTR_ClassFactoryBase;
#endif
typedef DPTR(LayoutEEClass) PTR_LayoutEEClass;
typedef DPTR(ArrayClass) PTR_ArrayClass;
typedef DPTR(DelegateEEClass) PTR_DelegateEEClass;
typedef DPTR(UMThunkMarshInfo) PTR_UMThunkMarshInfo;
typedef DPTR(CORCOMPILE_DEPENDENCY) PTR_CORCOMPILE_DEPENDENCY;
typedef DPTR(struct ModuleCtorInfo) PTR_ModuleCtorInfo;
typedef DPTR(class EEImplMethodDesc) PTR_EEImplMethodDesc;
typedef DPTR(class EEClassLayoutInfo) PTR_EEClassLayoutInfo;
typedef DPTR(class FieldMarshaler) PTR_FieldMarshaler;
typedef DPTR(LPCUTF8) PTR_LPCUTF8;
typedef DPTR(struct STORAGESIGNATURE UNALIGNED) PTR_STORAGESIGNATURE;
typedef DPTR(struct STORAGEHEADER UNALIGNED) PTR_STORAGEHEADER;
typedef DPTR(struct STORAGESTREAM UNALIGNED) PTR_STORAGESTREAM;
typedef DPTR(ArrayMethodDesc) PTR_ArrayMethodDesc;


#if 0
template<typename PtrType>
class TokenHashMap : CClosedHash< Pair<DPTR(PtrType), mdToken> >
{
public:
    typedef DPTR(PtrType) Key;
    typedef mdTypeRef Data;
    typedef Pair<Key, Data> Entry;
    typedef CClosedHash< Entry > Parent;
    TokenHashMap(int buckets = 23) : Parent(buckets)
    {

    }
    ~TokenHashMap() { }

    void Add(const Key key, const Data data)
    {
        Entry * newEntry = Parent::Add((void*)PTR_HOST_TO_TADDR(key));
        newEntry->First() = key;
        newEntry->Second() = data;
    }

    Data Find(const Key key)
    {
        Entry * found = Parent::Find((void*)PTR_HOST_TO_TADDR(key));
        if( !found )
            return mdTokenNil;
        else
            return found->Second();
    }
    inline Key GetKey(Entry * entry) { return entry->First(); }
    Parent::ELEMENTSTATUS Status(Entry * entry)
    {
        if( entry->First() == 0xffffffff && entry->Second() == 0xffffffff )
            return Parent::DELETED;
        else if( entry->First() == 0x00000000 && entry->Second() == 0x00000000 )
            return Parent::FREE;
        else
            return Parent::USED;
    }
    void SetStatus(Entry * entry, Parent::ELEMENTSTATUS status)
    {
        switch(status)
        {
        case Parent::FREE:
            entry->First() = Key((TADDR)0x00000000);
            entry->Second() = 0x00000000;
            break;
        case Parent::DELETED:
            entry->First() = Key((TADDR)0xffffffff);
            entry->Second() = 0xffffffff;
            break;
        }
    }

    unsigned int Compare(const Entry * lhs, Entry * rhs)
    {
        return lhs->First() == rhs->First() && lhs->Second() == rhs->Second();
    }

    //parent methods
    unsigned int Hash(const void *pData)
    {
        return (int)(INT_PTR)pData;
    }
    unsigned int Compare(const void * p1, BYTE * p2)
    {
        return Compare((const Entry *) p1, (Entry*) p2);
    }
    Parent::ELEMENTSTATUS Status(BYTE * p){
        return Status((Entry*)p);
    }
    void SetStatus(BYTE * p, Parent::ELEMENTSTATUS status) {
        SetStatus((Entry*)p, status);
    }
    void * GetKey(BYTE *p) { return (void*)GetKey((Entry*)p); }
};
typedef TokenHashMap<EEClass> EEClassToTypeRefMap;
typedef TokenHashMap<MethodTable> MTToTypeRefMap;
#endif

class NativeImageDumper
{
public:
    //DPTR to private field needs to be a member of NativeImageDumper
#if defined(FEATURE_COMINTEROP)
    typedef DPTR(SparseVTableMap::Entry) PTR_SparseVTableMap_Entry;
#endif

    NativeImageDumper(PTR_VOID loadedBase, const WCHAR * const name,
                      IXCLRDataDisplay * display, IXCLRLibrarySupport *support,
                      IXCLRDisassemblySupport * dis);
    ~NativeImageDumper();

    //type dumping methods
    void DumpNativeImage();

    void ComputeMethodFixupHistogram( PTR_Module module );
    void DumpFixupTables( PTR_Module module);

    void WriteElementTypeHandle( const char * name, TypeHandle th );
    void DoWriteFieldFieldDesc( const char * name, unsigned offset,
                                unsigned fieldSize, PTR_FieldDesc fd );
    void DoWriteFieldMethodDesc( const char * name, unsigned offset,
                                 unsigned fieldSize, PTR_MethodDesc md );
    void DoWriteFieldTypeHandle( const char * name, unsigned offset,
                                 unsigned fieldSize, TypeHandle th );
    void DoWriteFieldMDToken( const char * name, unsigned offset,
                              unsigned fieldsize, mdToken token,
                              IMetaDataImport2 *pAssemblyImport = NULL);
    void DoWriteFieldMethodTable( const char * name, unsigned offset,
                                  unsigned fieldSize, PTR_MethodTable mt );
    //if fixup is a fixup, it writes the field as if it were a fixup (including
    //subelements) and returns true.  Otherwise, it returns false.
    BOOL DoWriteFieldAsFixup( const char * name, unsigned offset,
                              unsigned fieldSize, TADDR fixup );

    void WriteElementMethodTable( const char * name, PTR_MethodTable mt );
    void WriteElementMethodDesc( const char * name, PTR_MethodDesc md );

    void DoWriteFieldCorElementType( const char * name, unsigned offset,
                                     unsigned fieldSize, CorElementType type );
    void WriteElementMDToken( const char * name, mdToken token );

    void DoWriteFieldAsHex( const char * name, unsigned offset,
                            unsigned fieldSize, PTR_BYTE data,
                            unsigned dataLen );

    void DumpMethods(PTR_Module module);

    void DumpCompleteMethod(PTR_Module module, MethodIterator& mi);

    void DisassembleMethod(BYTE *method, SIZE_T size);

    void DumpModule( PTR_Module module );
    void DumpNative();
    void DumpNativeHeader();

    void DumpBaseRelocs();
    void DumpHelperTable();

    void DumpMethodFixups(PTR_Module module,
                          TADDR fixupList);

    void DumpTypes( PTR_Module module );

    void DumpMethodTable( PTR_MethodTable mt, const char * name,
                          PTR_Module module );

#ifndef STUB_DISPATCH_ALL
    void DumpMethodTableSlotChunk( TADDR slotChunk, COUNT_T size, bool );
#endif

    void DumpSlot( unsigned index, PCODE tgt );
    void DumpFieldDesc( PTR_FieldDesc fd, const char * name );
    void DumpEEClassForMethodTable( PTR_MethodTable mt );
    void DumpTypeDesc( PTR_TypeDesc td );

    void DumpMethodDesc( PTR_MethodDesc md, PTR_Module module );
    void DumpPrecode( PTR_Precode precode, PTR_Module module );







    //utility routines
    void AppendTokenName(mdToken token, SString& str);
    void AppendTokenName(mdToken token, SString& str, IMetaDataImport2 *pImport,
                         bool force = false);
    void PrintManifestTokenName(mdToken token, SString& str);
    void PrintManifestTokenName(mdToken token, SString& str,
                                IMetaDataAssemblyImport *pAssemblyImport,
                                bool force = false);
    void WriteElementsFixupBlob(PTR_CORCOMPILE_IMPORT_SECTION pSection, SIZE_T fixup);
    void WriteElementsFixupTargetAndName(RVA rva);
    void FixupBlobToString(RVA rva, SString& buf);

    void AppendToken(mdToken token, SString& buf);
    void AppendToken(mdToken token, SString& buf, IMetaDataImport2 *pImport);
    IMetaDataImport2* TypeToString(PTR_CCOR_SIGNATURE &sig, SString& buf);  // assumes pImport is m_import
    IMetaDataImport2* TypeToString(PTR_CCOR_SIGNATURE &sig, SString& buf,
                                   IMetaDataImport2 *pImport,
                                   IMetaDataImport2 *pOrigImport =NULL);
    void MethodTableToString( PTR_MethodTable mt, SString& buf );
    void TypeHandleToString( TypeHandle td, SString& buf );
    void TypeDescToString( PTR_TypeDesc td, SString& buf );
    void DictionaryToArgString( PTR_Dictionary dictionary, unsigned numArgs, SString& buf );

    void EntryPointToString( PCODE pEntryPoint, SString& buf );
    void MethodDescToString( PTR_MethodDesc md, SString& buf );
    void FieldDescToString( PTR_FieldDesc fd, SString& buf );
    //uses tok to generate a name if fd == NULL
    void FieldDescToString( PTR_FieldDesc fd, mdFieldDef tok, SString& buf );

#ifdef FEATURE_READYTORUN
private:
    READYTORUN_HEADER *			m_pReadyToRunHeader;

    PTR_RUNTIME_FUNCTION        m_pRuntimeFunctions;
    DWORD                       m_nRuntimeFunctions;

    NativeFormat::NativeReader  m_nativeReader;
    NativeFormat::NativeArray   m_methodDefEntryPoints;

    IMAGE_DATA_DIRECTORY * FindReadyToRunSection(ReadyToRunSectionType type);

public:
    void DumpReadyToRun();
    void DumpReadyToRunHeader();
    void DumpReadyToRunMethods();
    void DumpReadyToRunMethod(PCODE pEntryPoint, PTR_RUNTIME_FUNCTION pRuntimeFunction, SString& name);
#endif // FEATURE_READYTORUN

private:
    PEDecoder m_decoder;
    const WCHAR * const m_name;
    PTR_VOID m_baseAddress;
    SIZE_T m_imageSize;
    IXCLRDataDisplay * m_display;
    IXCLRLibrarySupport * m_librarySupport;

    bool isInRange(TADDR ptr)
    {
        return dac_cast<TADDR>(m_baseAddress) <= ptr
            && ptr < (dac_cast<TADDR>(m_baseAddress) + m_imageSize);
    }


    COUNT_T ** m_fixupHistogram;

    #define COUNT_HISTOGRAM_SIZE 16
    COUNT_T m_fixupCountHistogram[COUNT_HISTOGRAM_SIZE];
    COUNT_T m_fixupCount; //used to track above counts

    // Primary image metadata
    IMetaDataImport2 *m_import;
    IMetaDataAssemblyImport *m_assemblyImport;

    // Installation manifest metadata.  For native images this is metadata
    // copied from the IL image.
    IMetaDataImport2 *m_manifestImport;
    IMetaDataAssemblyImport *m_manifestAssemblyImport;

    //helper for ComputeMethodFixupHistogram
    BOOL HandleFixupForHistogram(PTR_CORCOMPILE_IMPORT_SECTION pSection, SIZE_T fixupIndex, SIZE_T *fixupCell, BOOL mayUsePrecompiledNDirectMethods = TRUE);

    //helper for DumpMethodFixups
    BOOL HandleFixupForMethodDump(PTR_CORCOMPILE_IMPORT_SECTION pSection, SIZE_T fixupIndex, SIZE_T *fixupCell, BOOL mayUsePrecompiledNDirectMethods = TRUE);

    // Dependencies

public:
    struct Dependency
    {
        CORCOMPILE_DEPENDENCY * entry;
        //CORINFO_ASSEMBLY_HANDLE assembly;

        TADDR pPreferredBase;
        TADDR pLoadedAddress;
        SIZE_T size;

        PTR_Module pModule;
        IMetaDataImport2 *pImport;
        TADDR pMetadataStartTarget;
        TADDR pMetadataStartHost;
        SIZE_T MetadataSize;
        bool fIsCoreLib;
        bool fIsHardbound;
        WCHAR name[128];
    };

    /* REVISIT_TODO Fri 12/09/2005
     * Perhaps the module and import should be in the dependency.  In order to
     * properly name tokens in modules w/o import entries.
     */
    struct Import
    {
        DWORD index;
        Dependency *dependency;
    };
private:

    Dependency *m_dependencies;
    COUNT_T m_numDependencies;
    Import *m_imports;
    COUNT_T m_numImports;
    CORCOMPILE_DEPENDENCY m_self;

    bool inline isSelf(const Dependency* dep) {
        return &m_dependencies[0] == dep;
    }


    void OpenMetadata();
    void WriteElementsMetadata( const char * elementName,
                                TADDR data, SIZE_T size );
    NativeImageDumper::Dependency*
        GetDependency(mdAssemblyRef token, IMetaDataAssemblyImport *pImport = NULL);
    NativeImageDumper::Import *OpenImport(int i);
    NativeImageDumper::Dependency * OpenDependency(int index);
    void TraceDumpImport(int idx, NativeImageDumper::Import * import);
    void TraceDumpDependency(int idx, NativeImageDumper::Dependency * dependency);
    mdAssemblyRef MapAssemblyRefToManifest(mdAssemblyRef token, IMetaDataAssemblyImport *pAssemblyImport);

    const Dependency * GetDependencyForFixup(RVA rva);
    const Dependency * GetDependencyForModule( PTR_Module module );
#if 0
    const Import * GetImportForPointer( TADDR ptr );
#endif
    const Dependency * GetDependencyForPointer( TADDR ptr );
#ifdef MANUAL_RELOCS
    template< typename T >
        inline T RemapPointerForReloc( T ptr );

    inline TADDR RemapTAddrForReloc( TADDR ptr );

    inline TADDR RemapTAddrForReloc( const NativeImageDumper::Dependency * d,
                                     TADDR ptr );

    template< typename T >
        inline T RemapPointerForReloc( const NativeImageDumper::Dependency * d,
                                       T ptr );
#endif


    // msdis support

#if 0
    static size_t TranslateFixupCallback(const DIS *, DIS::ADDR, size_t, WCHAR *, size_t, DWORDLONG *);
    static size_t TranslateRegrelCallback(const DIS *, DIS::REGA, DWORD, WCHAR *, size_t, DWORD *);
    static size_t TranslateConstCallback(const DIS *, DWORD, WCHAR *, size_t);
#endif
    IXCLRDisassemblySupport * m_dis;
    static SIZE_T __stdcall TranslateFixupCallback(IXCLRDisassemblySupport *dis,
                                                 CLRDATA_ADDRESS addr,
                                                 SIZE_T size, __out_ecount(nameSize) WCHAR *name,
                                                 SIZE_T nameSize,
                                                 DWORDLONG *offset);
    static SIZE_T __stdcall TranslateAddressCallback(IXCLRDisassemblySupport *dis,
                                                   CLRDATA_ADDRESS addr,
                                                   __out_ecount(nameSize) WCHAR *name, SIZE_T nameSize,
                                                   DWORDLONG *offset);
    size_t TranslateSymbol(IXCLRDisassemblySupport *dis,
                                          CLRDATA_ADDRESS addr, __out_ecount(nameSize) WCHAR *name,
                                          SIZE_T nameSize, DWORDLONG *offset);

    CLRDATA_ADDRESS m_currentAddress;
    bool m_currentIsAddress;

    //mscorwks sizes
    TADDR m_mscorwksBase;
    TADDR m_mscorwksPreferred;
    SIZE_T m_mscorwksSize;


    //internal type dumpers
    void DumpDictionaryEntry( const char * name, DictionaryEntryKind kind,
                              PTR_DictionaryEntry entry );
    void WriteFieldDictionaryLayout( const char * name, unsigned offset,
                                     unsigned fieldSize,
                                     PTR_DictionaryLayout layout,
                                     IMetaDataImport2 * import );


    IMAGE_SECTION_HEADER * FindSection( char const * name );


    //map traversal methods and helpers
    void IterateTypeDefToMTCallback(TADDR taddrTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateTypeRefToMTCallback(TADDR taddrTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateMethodDefToMDCallback(TADDR taddrTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateFieldDefToFDCallback(TADDR taddrTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateMemberRefToDescCallback(TADDR taddrTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateGenericParamToDescCallback(TADDR fdTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateFileReferencesCallback(TADDR moduleTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);
    void IterateManifestModules(TADDR moduleTarget, TADDR flags, PTR_LookupMapBase map, DWORD rid);

    void TraverseMap(PTR_LookupMapBase map, const char * name, unsigned offset,
                     unsigned fieldSize,
                     void(NativeImageDumper::*cb)(TADDR, TADDR, PTR_LookupMapBase, DWORD));

    template<typename HASH_CLASS, typename HASH_ENTRY_CLASS>
    void TraverseNgenHash(DPTR(HASH_CLASS) pTable, const char * name,
                          unsigned offset, unsigned fieldSize,
                          bool saveClasses,
                          void (NativeImageDumper::*DisplayEntryFunction)(void *, DPTR(HASH_ENTRY_CLASS), bool),
                          void *pContext);
    template<typename HASH_CLASS, typename HASH_ENTRY_CLASS>
    void TraverseNgenPersistedEntries(DPTR(HASH_CLASS) pTable,
                                      DPTR(typename HASH_CLASS::PersistedEntries) pEntries,
                                      bool saveClasses,
                                      void (NativeImageDumper::*DisplayEntryFunction)(void *, DPTR(HASH_ENTRY_CLASS), bool),
                                      void *pContext);

    void TraverseClassHashEntry(void *pContext, PTR_EEClassHashEntry pEntry, bool saveClasses);
    void TraverseClassHash(PTR_EEClassHashTable pTable, const char * name,
                           unsigned offset, unsigned fieldSize,
                           bool saveClasses);

#ifdef FEATURE_COMINTEROP
    void TraverseGuidToMethodTableEntry(void *pContext, PTR_GuidToMethodTableEntry pEntry, bool saveClasses);
    void TraverseGuidToMethodTableHash(PTR_GuidToMethodTableHashTable pTable, const char * name,
                                       unsigned offset, unsigned fieldSize, bool saveClasses);
#endif // FEATURE_COMINTEROP

    void TraverseMemberRefToDescHashEntry(void *pContext, PTR_MemberRefToDescHashEntry pEntry, bool saveClasses);

    void TraverseMemberRefToDescHash(PTR_MemberRefToDescHashTable pTable, const char * name,
                                       unsigned offset, unsigned fieldSize, bool saveClasses);


    void TraverseTypeHashEntry(void *pContext, PTR_EETypeHashEntry pEntry, bool saveClasses);
    void TraverseTypeHash(PTR_EETypeHashTable pTable, const char * name,
                          unsigned offset, unsigned fieldSize );

    void TraverseInstMethodHashEntry(void *pContext, PTR_InstMethodHashEntry pEntry, bool saveClasses);
    void TraverseInstMethodHash(PTR_InstMethodHashTable pTable,
                                const char * name, unsigned offset,
                                unsigned fieldSize, PTR_Module module);

    void TraverseStubMethodHashEntry(void *pContext, PTR_StubMethodHashEntry pEntry, bool saveClasses);
    void TraverseStubMethodHash(PTR_StubMethodHashTable pTable,
                                const char * name, unsigned offset,
                                unsigned fieldSize, PTR_Module module);

    void DoWriteFieldStr( PTR_BYTE ptr, const char * name, unsigned offset,
                          unsigned fieldSize );


    template<typename T>
    TADDR DPtrToPreferredAddr( T ptr );

    TADDR DPtrToPreferredAddr( TADDR tptr );

    void DumpAssemblySignature(CORCOMPILE_ASSEMBLY_SIGNATURE & assemblySignature);

    SIZE_T CountFields( PTR_MethodTable mt );
    mdToken ConvertToTypeDef( mdToken typeSpecOrRef, IMetaDataImport2* (&pImport) );
    SIZE_T CountDictionariesInClass( mdToken typeDefOrRef, IMetaDataImport2 * pImport );
    PTR_EEClass GetClassFromMT( PTR_MethodTable mt );
    PTR_MethodTable GetParent( PTR_MethodTable mt );

    const Dependency* GetDependencyFromFD( PTR_FieldDesc fd );
    const Dependency* GetDependencyFromMD( PTR_MethodDesc md );
    const Dependency* GetDependencyFromMT( PTR_MethodTable mt );

    CLRNativeImageDumpOptions m_dumpOptions;
    inline TADDR RvaToDisplay( SIZE_T rva );
    inline TADDR DataPtrToDisplay(TADDR ptr);
    inline int CheckOptions( CLRNativeImageDumpOptions opt );

    //support various LookupMap Iterators
    SArray<PTR_MethodTable> m_discoveredMTs;

    struct SlotChunk
    {
        TADDR addr;
        WORD nSlots;
        bool isRelative;

        inline bool operator==(const SlotChunk& sc) const
        {
            return (addr == sc.addr) && (nSlots == sc.nSlots) && (isRelative == sc.isRelative);
        }

        inline bool operator<(const SlotChunk& sc) const
        {
            if (addr < sc.addr)
            {
                return TRUE;
            }
            else if (addr > sc.addr)
            {
                return FALSE;
            }
            else
            {
                return nSlots < sc.nSlots;
            }
        }
    };

    SArray<SlotChunk> m_discoveredSlotChunks;
    //SArray<PTR_MethodDesc> m_discoveredMDs;
    //SArray<PTR_FieldDesc> m_discoveredFDs;
    SArray<PTR_MethodTable> m_discoveredClasses;
    SArray<PTR_TypeDesc> m_discoveredTypeDescs;

    typedef InlineSString<128> TempBuffer;

    /* XXX Mon 10/03/2005
     * When we encounter pointers from metadata they are already in the host
     * process because we read all of metadata in as one big block (since the
     * metadata api isn't dac-ized.  Map the metadata pointers back to good DAC
     * pointers for compatibility with certain sig parsing code.
     */
    TADDR m_MetadataStartHost;
    TADDR m_MetadataStartTarget;
    COUNT_T m_MetadataSize;

    //Support dumping IL.  The COR_ILMETHOD_DECODER is not DACized, so read the
    //whole IL section in, and translate RVAs into host pointers into the IL
    //section copy
    RVA m_ILSectionStart;
    BYTE * m_ILHostCopy;
#ifdef _DEBUG
    COUNT_T m_ILSectionSize;
#endif

    //This is true if we are hard bound to corelib.  This enables various forms of generics dumping and MT
    //dumping that require g_pObjectClass to be set.
    bool m_isCoreLibHardBound;

#if 0
    PTR_CCOR_SIGNATURE metadataToHostDAC( PCCOR_SIGNATURE pSig,
                                          IMetaDataImport2 * import );
#endif
    template<typename T>
        DPTR(T) metadataToHostDAC( T * pSig, IMetaDataImport2 * import);

    void DoDumpFieldStub( PTR_Stub stub, unsigned offset, unsigned fieldSize,
                          const char * name );
#ifdef FEATURE_COMINTEROP
    void DoDumpComPlusCallInfo( PTR_ComPlusCallInfo compluscall );
#endif // FEATURE_COMINTEROP

    SIZE_T m_sectionAlignment;
    inline SIZE_T GetSectionAlignment() const;

public:
    //this is the list of valid precode addresses for the current module.
    struct PrecodeRange
    {
        PrecodeRange( CorCompileSection section, TADDR start, SIZE_T size )
            : m_sectionType(section), m_rangeStart(start),
              m_rangeSize(size) { }
        CorCompileSection m_sectionType;
        TADDR m_rangeStart;
        SIZE_T m_rangeSize;
    };
private:
    bool isPrecode(TADDR maybePrecode);
    void FixupThunkToString(PTR_CORCOMPILE_IMPORT_SECTION pImportSection, TADDR thunkAddr, SString& buf);

#if 0
    MTToTypeRefMap m_mtToTypeRefMap;
    EEClassToTypeRefMap m_eeClassToTypeRefMap;
    void RecordTypeRef( mdTypeRef token, PTR_MethodTable mt );
    void RecordTypeRef( mdTypeRef token, PTR_EEClass clazz );
    mdTypeRef FindTypeRefForMT( PTR_MethodTable mt );
    mdTypeRef FindTypeRefForEEClass( PTR_EEClass clazz );
#endif


public:
    struct EnumMnemonics
    {
        EnumMnemonics( DWORD val, const WCHAR * m )
            : value(val), mask(val), mnemonic(m){ }
        EnumMnemonics( DWORD val, DWORD msk, const WCHAR * m )
            : value(val), mask(msk), mnemonic(m) { }
        DWORD value;
        DWORD mask;
        const WCHAR * mnemonic;
    };

    static EnumMnemonics s_ModulePersistedFlags[];
    static EnumMnemonics s_MDC[];
    static EnumMnemonics s_MDFlag2[];
    static EnumMnemonics s_TDFlags[];
    static EnumMnemonics s_SSMDExtendedFlags[];
    static EnumMnemonics s_IMDFlags[];
    static EnumMnemonics s_EECLIFlags[];
};
#include "nidump.inl"

#endif //FEATURE_PREJIT
#endif
