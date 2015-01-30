//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// ZapImage.h
//

//
// NGEN-specific infrastructure for writing PE files.
//
// ======================================================================================

#ifndef __ZAPIMAGE_H__
#define __ZAPIMAGE_H__


#ifdef CLR_STANDALONE_BINDER
#include "nativedata.h"
#endif

class ZapMetaData;
class ZapILMetaData;
class ZapCorHeader;
class ZapNativeHeader;
class ZapVersionInfo;
class ZapDependencies;
class ZapCodeManagerEntry;

class ZapReadyToRunHeader;

class ZapInnerPtrTable;
class ZapMethodEntryPointTable;
class ZapWrapperTable;

class ZapBaseRelocs;

class ZapBlobWithRelocs;

//class ZapGCInfoTable;
#ifdef WIN64EXCEPTIONS
class ZapUnwindDataTable;
#endif

class ZapImportTable;
class ZapImportSectionsTable;
class ZapImportSectionSignatures;

class ZapVirtualSectionsTable;
class DataImage;

class ZapperStats;

#undef SAFERELEASE
#define SAFERELEASE(p) if ((p) != NULL) { IUnknown * _ = (p); (p) = NULL; _->Release();  };

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
#define DEFAULT_CODE_BUFFER_INIT 0xcc // breakpoint
#else
#define DEFAULT_CODE_BUFFER_INIT 0
#endif

#ifdef _WIN64
// Optimize for speed
#define DEFAULT_CODE_ALIGN  16
#else
// Optimize for size.
#define DEFAULT_CODE_ALIGN  4
#endif

#ifdef _TARGET_ARM_
#define MINIMUM_CODE_ALIGN 2
#else
#define MINIMUM_CODE_ALIGN 1
#endif

// Various zapper hashtables are preallocated based on the size of IL image to reduce amount of
// rehashing we have to do. Turn ZAP_HASHTABLE_TUNING on to collect data for the tuning of initial hashtable sizes.
// #define ZAP_HASHTABLE_TUNING

#ifdef ZAP_HASHTABLE_TUNING

#define PREALLOCATE_HASHTABLE(table, quotient, cbILImage) \
    PREALLOCATE_HASHTABLE_NOT_NEEDED(table, cbILImage)

#define PREALLOCATE_HASHTABLE_NOT_NEEDED(table, cbILImage) \
    do { \
        GetSvcLogger()->Printf(W("HashTable:\t%S\t%d\t%f\n"), #table, table.GetCount(), (double)table.GetCount() / (double)cbILImage); \
    } while (0)

#define PREALLOCATE_ARRAY(array, quotient, cbILImage)  \
    do { \
        GetSvcLogger()->Printf(W("Array:\t%S\t%d\t%f\n"), #array, array.GetCount(), (double)array.GetCount() / (double)cbILImage); \
    } while (0)

#else // ZAP_HASHTABLE_TUNING

#define PREALLOCATE_HASHTABLE(table, quotient, cbILImage)  \
    do { \
        COUNT_T nSize = (COUNT_T)(quotient * \
            ((double)table.s_density_factor_denominator / (double)table.s_density_factor_numerator) * \
            cbILImage); \
        if (nSize > table.s_minimum_allocation) \
            table.Reallocate(nSize); \
    } while (0)

#define PREALLOCATE_HASHTABLE_NOT_NEEDED(table, cbILImage)

#define PREALLOCATE_ARRAY(array, quotient, cbILImage)  \
    do { \
        COUNT_T nSize = (COUNT_T)(quotient * \
            cbILImage); \
        array.Preallocate(nSize); \
    } while (0)

#endif // ZAP_HASHTABLE_TUNING

//---------------------------------------------------------------------------------------
//
// ZapImportSectionType is enum describing import sections allocated in the image
//
enum ZapImportSectionType
{
    ZapImportSectionType_Handle,        // Unspecified handle
    ZapImportSectionType_TypeHandle,    // Type and method handles have to have their own section so we can restore them correctly
    ZapImportSectionType_MethodHandle,
#ifdef _TARGET_ARM_
    ZapImportSectionType_PCode,         // Code pointers have to be in a own section on ARM because of they are tagged differently
#endif
    ZapImportSectionType_StringHandle,  // String handles require special handling for interning
    ZapImportSectionType_Count,

    ZapImportSectionType_Hot = 0,       // We have two sets of the section - hot and cold
    ZapImportSectionType_Cold = ZapImportSectionType_Count,

    ZapImportSectionType_Eager = 2 * ZapImportSectionType_Count,    // And one section for eager loaded handles

    ZapImportSectionType_Total = 2 * ZapImportSectionType_Count + 1,
};

#include "zaprelocs.h"
#include "zapinfo.h"
#include "zapcode.h"

#ifdef MDIL
class ICompactLayoutWriter;
#endif

class ZapImage
    : public ZapWriter
    , public ICorCompileDataStore
{
    friend class Zapper;
    friend class ZapInfo;
    friend class ZapILMetaData;
    friend class ZapImportTable;
    friend class ZapCodeMethodDescs;
    friend class ZapColdCodeMap;
    friend class ZapReadyToRunHeader;

 private:

    Zapper          *m_zapper;

    //
    // Output module
    //

    LPWSTR          m_pOutputFileFullName;      // Name of the temp ngen file to generate (including the path)

    //
    // Make all virtual section pointers public for now. It should be cleaned up as we get more sophisticated layout
    // algorithm in place.
    //
public:
    ZapPhysicalSection * m_pTextSection;

    //
    // All virtual sections of the native image in alphabetical order
    //

    ZapVirtualSection * m_pBaseRelocsSection;
    ZapVirtualSection * m_pCodeSection;
    ZapVirtualSection * m_pColdCodeSection;
    ZapVirtualSection * m_pDebugSection;
    ZapVirtualSection * m_pDelayLoadInfoDelayListSectionEager;
    ZapVirtualSection * m_pDelayLoadInfoDelayListSectionCold;
    ZapVirtualSection * m_pDelayLoadInfoDelayListSectionHot;
    ZapVirtualSection * m_pDelayLoadInfoTableSection[ZapImportSectionType_Total];
    ZapVirtualSection * m_pStubsSection;
    ZapVirtualSection * m_pEETableSection;
    ZapVirtualSection * m_pExceptionSection;
    ZapVirtualSection * m_pGCSection;
    ZapVirtualSection * m_pHeaderSection;
    ZapVirtualSection * m_pHelperTableSection;
    ZapVirtualSection * m_pLazyHelperSection;
    ZapVirtualSection * m_pLazyMethodCallHelperSection;
    ZapVirtualSection * m_pHotCodeSection;
    ZapVirtualSection * m_pHotGCSection;
    ZapVirtualSection * m_pHotTouchedGCSection;
    ZapVirtualSection * m_pILMetaDataSection;
    ZapVirtualSection * m_pILSection;
    ZapVirtualSection * m_pImportTableSection;
    ZapVirtualSection * m_pInstrumentSection;
    ZapVirtualSection * m_pMetaDataSection;
    ZapVirtualSection * m_pReadOnlyDataSection;
    ZapVirtualSection * m_pResourcesSection;
    ZapVirtualSection * m_pWin32ResourceSection;
    ZapVirtualSection * m_pStubDispatchCellSection;
    ZapVirtualSection * m_pStubDispatchDataSection;
    ZapVirtualSection * m_pDynamicHelperCellSection;
    ZapVirtualSection * m_pDynamicHelperDataSection;
    ZapVirtualSection * m_pVirtualImportThunkSection;
    ZapVirtualSection * m_pExternalMethodThunkSection;
    ZapVirtualSection * m_pExternalMethodCellSection;
    ZapVirtualSection * m_pExternalMethodDataSection;
    ZapVirtualSection * m_pHotRuntimeFunctionSection;
    ZapVirtualSection * m_pRuntimeFunctionSection;
    ZapVirtualSection * m_pColdRuntimeFunctionSection;
    ZapVirtualSection * m_pHotCodeMethodDescsSection;
    ZapVirtualSection * m_pCodeMethodDescsSection;
    ZapVirtualSection * m_pHotRuntimeFunctionLookupSection;
    ZapVirtualSection * m_pRuntimeFunctionLookupSection;
    ZapVirtualSection * m_pColdCodeMapSection;
#if defined(WIN64EXCEPTIONS)
    ZapVirtualSection * m_pHotUnwindDataSection;
    ZapVirtualSection * m_pUnwindDataSection;
    ZapVirtualSection * m_pColdUnwindDataSection;
#endif // defined(WIN64EXCEPTIONS)

#ifdef MDIL
    ZapVirtualSection * m_pMDILSection;
#endif
    // Preloader sections
    ZapVirtualSection * m_pPreloadSections[CORCOMPILE_SECTION_COUNT];

    ZapExceptionInfoLookupTable*      m_pExceptionInfoLookupTable;
public:
    // TODO: Remove once all EE datastructures are converted to ZapNodes
    ICorCompilePreloader * m_pPreloader;
    DataImage * m_pDataImage;

public:
    // TODO: The stats should be removed once we have all information available in nidump
    ZapperStats                *m_stats;

private:
    IMetaDataAssemblyEmit      *m_pAssemblyEmit; // native image manifest
    ZapMetaData *               m_pAssemblyMetaData;
#ifdef CLR_STANDALONE_BINDER
    SArray<NativeManifestData>  m_pNativeManifestData;
public:
#ifdef BINDER
    static const ULONG          m_selfIndex = 0; // position of "self dependency" in m_pNativeManifestData
#else
    ULONG                       m_selfIndex;     // position of "self dependency" in m_pNativeManifestData
#endif // BINDER

private:
#endif

    ZapVersionInfo *            m_pVersionInfo;
    ZapDependencies *           m_pDependencies;

    SString                     m_pdbFileName;

    ZapCodeManagerEntry *       m_pCodeManagerEntry;

    ZapBlob *                   m_pEEInfoTable;

    //
    // Auxiliary tables
    //
    ZapImportTable *            m_pImportTable;

    ZapImportSectionsTable *    m_pImportSectionsTable;

    ZapInnerPtrTable *          m_pInnerPtrs;

    ZapMethodEntryPointTable *  m_pMethodEntryPoints;

    ZapWrapperTable *           m_pWrappers;

    ZapBaseRelocs *             m_pBaseRelocs;

    ULONGLONG                   m_NativeBaseAddress;

    ULONGLONG GetNativeBaseAddress()
    {
        return m_NativeBaseAddress;
    }

    void                        CalculateZapBaseAddress();

    // Preallocate hashtables to avoid rehashing
    void Preallocate();

    ZapGCInfoTable *            m_pGCInfoTable;

#ifdef WIN64EXCEPTIONS
    ZapUnwindDataTable *        m_pUnwindDataTable;
#endif

    ZapImportSectionSignatures * m_pDelayLoadInfoDataTable[ZapImportSectionType_Total];
    ZapImportSectionSignatures * m_pStubDispatchDataTable;
    ZapImportSectionSignatures * m_pExternalMethodDataTable;
    ZapImportSectionSignatures * m_pDynamicHelperDataTable;

    ZapVirtualSectionsTable *   m_pVirtualSectionsTable;

    ZapDebugInfoTable *         m_pDebugInfoTable;
#ifdef MDIL
    MdilDebugInfoTable *        m_pMdilDebugInfoTable;
#endif

    ZapILMetaData *             m_pILMetaData;

    ZapCorHeader *              m_pCorHeader;

    ZapNode *                   m_pResources;

    ZapNode *                   m_pNativeHeader;

    ZapBlob *                   m_pNGenPdbDebugData;

    ULONG                       m_totalHotCodeSize;
    ULONG                       m_totalColdCodeSize;

    ULONG                       m_totalCodeSizeInProfiledMethods;
    ULONG                       m_totalColdCodeSizeInProfiledMethods;

    //information to track the boundaries of the different subsections within
    //the hot section.
    COUNT_T                     m_iIBCMethod;
    COUNT_T                     m_iGenericsMethod;
    COUNT_T                     m_iUntrainedMethod;

    //
    // Input module
    //

    LPWSTR                      m_pModuleFileName; // file name of the module being compiled, including path
    CORINFO_MODULE_HANDLE       m_hModule;   // Module being compiled
    PEDecoder                   m_ModuleDecoder;
    IMDInternalImport *         m_pMDImport;
    bool                        m_fManifestModule;  // Is this the assembly-manifest-module
    bool                        m_fHaveProfileData;

    ZapNode **                  m_pHelperThunks; // Array of on demand allocated JIT helper thunks

    //
    // Profile source
    //

    BYTE *                      m_profileDataFile;
    BYTE *                      m_pRawProfileData;
    COUNT_T                     m_cRawProfileData;
    CorProfileData *            m_pCorProfileData;

    // ProfileData hash table
    struct ProfileDataHashEntry
    {
        mdMethodDef md;       // A copy of the method.token of the profile data
        DWORD       size;     // A copy of the size of the profile data
        ULONG       pos;
    };

    class ProfileDataHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ProfileDataHashEntry> >
    {
    public:
        typedef const mdMethodDef key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.md;
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return k1 == k2;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return (count_t)k;
        }

        static const element_t Null() { LIMITED_METHOD_CONTRACT; ProfileDataHashEntry e; e.pos = 0; e.size = 0; e.md = 0; return e; } // Assuming method profile data cannot start from position 0.
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pos == 0; }
    };
    typedef SHash<ProfileDataHashTraits> ProfileDataHashTable;

    ProfileDataHashTable profileDataHashTable;

#ifndef BINDER
    SArray<SString, FALSE> fileNotFoundErrorsTable;
    void FileNotFoundError(LPCWSTR pszMessage);
#endif // BINDER

public:
    struct ProfileDataSection
    {
        BYTE    *pData;
        DWORD    dataSize;
        DWORD    tableSize;
        CORBBTPROF_TOKEN_INFO *pTable;
    };

private:
    ProfileDataSection m_profileDataSections[SectionFormatCount];

    DWORD m_profileDataNumRuns;

    CorInfoRegionKind m_currentRegionKind;

    BOOL IsAssemblyBeingCompiled(CORINFO_MODULE_HANDLE module) {
        return ((module == m_hModule) ||
                (m_zapper->m_pEECompileInfo->GetModuleAssembly(module) == m_zapper->m_hAssembly));
    }

    class ZapMethodTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapMethodHeader *> >
    {
    public:
        typedef CORINFO_METHOD_HANDLE key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e->GetHandle();
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
    };

    typedef SHash<ZapMethodTraits> ZapMethodHashTable;

    ZapMethodHashTable m_CompiledMethods;

    SArray<ZapMethodHeader *> m_MethodCompilationOrder;

    SArray<ZapGCInfo *> m_PrioritizedGCInfo;

#ifndef FEATURE_FULL_NGEN
    class MethodCodeTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ZapMethodHeader *> >
    {
    public:
        typedef ZapMethodHeader * key_t;

        static FORCEINLINE key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e;
        }

        static BOOL Equals(key_t k1, key_t k2);
        static COUNT_T Hash(key_t k);

        static const element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
    };

    typedef SHash <MethodCodeTraits> ZapMethodCodeHashTable;

    ZapMethodCodeHashTable m_CodeDeduplicator;
#endif // FEATURE_FULL_NGEN

    struct ClassLayoutOrderEntry
    {
        CORINFO_CLASS_HANDLE m_cls;
        unsigned             m_order;

        ClassLayoutOrderEntry()
            : m_cls(0), m_order(0)
        {
        }

        ClassLayoutOrderEntry(CORINFO_CLASS_HANDLE cls, unsigned order)
            : m_cls(cls), m_order(order)
        {
        }
    };

    class ClassLayoutOrderTraits : public NoRemoveSHashTraits< DefaultSHashTraits<ClassLayoutOrderEntry> >
    {
    public:
        typedef CORINFO_CLASS_HANDLE key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return e.m_cls;
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
        static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t(0,0); }
        static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.m_cls == 0; }
    };

    typedef SHash<ClassLayoutOrderTraits> ClassLayoutOrderHashTable;

    ClassLayoutOrderHashTable m_ClassLayoutOrder;

    // See ComputeClassLayoutOrder for an explanation of these flags
    #define UNSEEN_CLASS_FLAG (0x80000000)
    #define METHOD_INDEX_FLAG (0x40000000)

    // The class layout order needs to be initialized with the first index
    // in m_MethodCompilationOrder of a method in the given class.
    inline void InitializeClassLayoutOrder(CORINFO_CLASS_HANDLE cls, unsigned order)
    {
        WRAPPER_NO_CONTRACT;

        if (!m_ClassLayoutOrder.LookupPtr(cls))
        {
            ClassLayoutOrderEntry entry(cls, order | UNSEEN_CLASS_FLAG | METHOD_INDEX_FLAG);
            m_ClassLayoutOrder.Add(entry);
        }
    }

public:
    inline unsigned LookupClassLayoutOrder(CORINFO_CLASS_HANDLE cls)
    {
        WRAPPER_NO_CONTRACT;

        const ClassLayoutOrderEntry *pEntry = m_ClassLayoutOrder.LookupPtr(cls);
        _ASSERTE(!pEntry || pEntry->m_order != 0);

        return pEntry ? pEntry->m_order : 0;
    }

private:

    //
    // The image layout algorithm
    //

    enum CodeType
    {
        ProfiledHot,
        ProfiledCold,
        Unprofiled
    };

    ZapVirtualSection * GetCodeSection(CodeType codeType);
    ZapVirtualSection * GetRuntimeFunctionSection(CodeType codeType);
    ZapVirtualSection * GetCodeMethodDescSection(CodeType codeType);
    ZapVirtualSection * GetUnwindInfoLookupSection(CodeType codeType);

#if defined(WIN64EXCEPTIONS)
    ZapVirtualSection * GetUnwindDataSection(CodeType codeType);
#endif

    void GetCodeCompilationRange(CodeType codeType, COUNT_T * start, COUNT_T * end);

    void OutputCode(CodeType codeType);
    void OutputCodeInfo(CodeType codeType);

    void OutputGCInfo();

    void OutputDebugInfo();
    void OutputProfileData();

    void OutputEntrypointsTableForReadyToRun();
    void OutputDebugInfoForReadyToRun();

    void CopyDebugDirEntry();
    void CopyWin32VersionResource();

    void OutputManifestMetadata();
    void OutputTables();

    // Assign RVAs to all ZapNodes
    void ComputeRVAs();

    HANDLE GenerateFile(LPCWSTR wszOutputFileName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);

    void PrintStats(LPCWSTR wszOutputFileName);

    bool m_fHasClassLayoutOrder;

    void ComputeClassLayoutOrder();
    void SortUnprofiledMethodsByClassLayoutOrder();

    HRESULT GetPdbFileNameFromModuleFilePath(__in_z const wchar_t *pwszModuleFilePath,
                                             __out_ecount(dwPdbFileNameBufferSize) char * pwszPdbFileName,
                                             DWORD dwPdbFileNameBufferSize);

#ifdef  MDIL
    void LoadMDILSection();

    void MethodCompileComplete_MDIL(BYTE *pCode, ULONG cCode);

    void Output_MDIL();
public:
    DWORD Write_MDIL(FILE *outputFile);
private:
    void EncodeGenericInstances_MDIL();

    void UnifyGenericInstances_MDIL(ZapInfo::MDILGenericMethodDesc *pMD);

    COUNT_T EncodeGenericInstance_MDIL(ZapInfo::MDILGenericMethodDesc *pMD);

    enum CodeKind
    {
        GENERIC_CODE,
        NON_GENERIC_CODE,
        CODE_KIND_COUNT,
    };

    SArray<BYTE>                m_codeBuffer[CODE_KIND_COUNT];  // the buffer holding the finished MDIL code
                                                                // for all compiled methods, including a method header
                                                                // indicating the length, and the exception tables
                                                                // the first four bytes are a magic DWORD, so offset 0 is invalid
                                                                // we wish to keep MDIL code possibly referenced from dependencies
                                                                // (currently only generic code) separate from MDIL code only used
                                                                // during translation of the module itself
    ULONG                       m_codeOffs[CODE_KIND_COUNT];    // code offset of the header of the current method

    SArray<ULONG>               m_mapMethodRidToOffs;   // offset in code buffer for each method def rid.
                                                        // 0 means no MDIL code available.
    static const ULONG GENERIC_METHOD_REF = 0x80000000; // hi bit on means this is a reference to a generic method descriptor
                                                        // that in turn points to the method bodies

    ULONG                       m_methodRidCount;       // number of valid entries in the m_mapMethodRidToOffs table

    ULONG                       m_mergedGenericSize;
    ULONG                       m_unmergedGenericSize;
    ULONG                       m_mergedGenericCount;
    ULONG                       m_unmergedGenericCount;

    ULONG                       m_stubMethodCount;

    ULONG                       m_assemblyName;
    ULONG                       m_locale;
    AssemblyMetaDataInternal    m_assemblyData;
    ULONG                       m_neutralResourceCultureNameLen;
    DWORD                       m_cultureName;
    USHORT                      m_neutralResourceFallbackLocation;

    SArray<ZapInfo::MDILGenericMethodDesc *> m_mapGenericMethodToDesc; // maps a method rid to a list of instances

    SArray<BYTE>                m_genericInstPool;      // where the flavor -> mdil body maps are stored

    BYTE *                      m_pMdilPESectionData;
    DWORD                       m_cbMdilPESectionData;
    struct ModuleZapImage
    {
        CORINFO_MODULE_HANDLE   m_module;
        ZapImage               *m_zapImage;
    };

    SArray<ModuleZapImage>      m_mdilImages;

    int CheckForUnmerged(ZapInfo::MDILGenericMethodDesc tab[], int last, ZapInfo::FlavorSet flavorsToMatch, __in_z WCHAR *message);

    ICompactLayoutWriter       *m_pICLW;

    SArray<BYTE>                m_stubBuffer;           // the buffer holding all IL stub descriptions
    SArray<BYTE>                m_stubAssocBuffer;      // the buffer with method -> IL stub associations

    SArray<BYTE>                m_debugInfoBuffer[CODE_KIND_COUNT]; // the buffer holding the debug info for all compiled methods
                                                                    // the first four bytes are a magic DWORD, so offset 0 is invalid
                                                                    // as for code, we want to keep information possibly referenced from dependencies
                                                                    // (currently only generic code) separate from information used only
                                                                    // during translation of the module itself so we keep two buffers
    SArray<ULONG>               m_mapMethodRidToDebug;  // offset in debug info buffer for each method def rid.
                                                        // before adjusting the offsets to account for the generic code buffer:
                                                        //     0xFFFFFFFF indicates no debug data and 0 is a valid offset
                                                        // after adjusting the offsets, aka what is written to file:
                                                        //     0xFFFFFFFF is valid (albeit never generated) and 0 indicates no debug data

public:

    Zapper *GetZapper()
    {
        return m_zapper;
    }

    // remember the assembly data
    void SetAssemblyNameAndLocale(ULONG assembly, ULONG locale, AssemblyMetaDataInternal *assemblyData) {
        m_assemblyName = assembly;
        m_locale = locale;
        m_assemblyData = *assemblyData;
    }

    // Set neutral resource culture information
    void SetNeutralResourceInfo(ULONG neutralResourceCultureNameLen, DWORD cultureName, USHORT neutralResourceFallbackLocation)
    {
        m_neutralResourceCultureNameLen = neutralResourceCultureNameLen;
        m_cultureName = cultureName;
        m_neutralResourceFallbackLocation = neutralResourceFallbackLocation;
    }

    // Should compact layout info be generated?
    bool DoCompactLayout()
    {
        return (m_zapper->m_pOpt->m_compilerFlags & CORJIT_FLG_MDIL) != 0;
    }

    // Flush a serialized representation of a type
    void FlushCompactLayoutData(mdToken typeToken, BYTE *pData, ULONG cData);

    // Flush the IL stub data
    void FlushStubData(BYTE *pStubSize, ULONG cStubSize,
                       BYTE *pStubData, ULONG cStubData,
                       BYTE *pStubAssocData, ULONG cStubAssocData);

    // Flush the user string pool
    void FlushUserStringPool(BYTE *pData, ULONG cData);

    // Flush the well known types table
    void FlushWellKnownTypes(DWORD *wellKnownTypesTable, SIZE_T count);

    struct  ExtModRef
    {
        enum ExtModRefFlags
        {
            NO_FLAGS            = 0x0000,
            IS_FROM_IL_METADATA = 0x0001,
            IS_EAGERLY_BOUND    = 0x0002,
            IS_MODULE_REF       = 0x0004,
            IS_LOCAL_MODULE     = 0x0008,
        };

        ULONG           name;       // offset of name in name pool
        ExtModRefFlags  flags;
    };

    SArray<ExtModRef>   m_extModRef;

    SArray<char>        m_namePool;

    struct  ExtTypeRef
    {
        ULONG   module  : 14;     // 16383 max modules to import from
        ULONG   ordinal : 18;     // 262143 max types within a module 
                                  // Just like metadata does today, I'm planning to use
                                  // wider tables when necessary
    };

#ifndef BINDER
    SArray<ExtTypeRef>  m_extTypeRef;

    struct ExtTypeRefExtend
    {
        ULONG resolutionScope;  // Rid in ExtTypeRef table of containing type (currently only used for nested types, all other type have a 0)
        ULONG name_space;       // offset of namespace in name pool
        ULONG name;             // offset of name in name pool
    };

    SArray<ExtTypeRefExtend>  m_extTypeRefExtend;
#endif // !BINDER

    struct ExtMemberRef
    {
        ULONG   typeRid : 15;       // 32767 max types to import
        ULONG   isTypeSpec : 1;
        ULONG   isField : 1;           // is this a field or a method?
        ULONG   ordinal : 15;          // 32767 max fields or methods in a type
                                       // Just like metadata does today, I'm planning to use
                                       // wider tables when necessary
    };

    SArray<ExtMemberRef> m_extMemberRef;

#ifndef BINDER
    struct ExtMemberRefExtend
    {
        ULONG name;             // offset of name in name pool
        ULONG signature;        // offset of signature.
    };

    SArray<ExtMemberRefExtend> m_extMemberRefExtend;
#endif // !BINDER

    SArray<ULONG>        m_typeSpecToOffs;
    SArray<ULONG>        m_methodSpecToOffs;
    SArray<ULONG>        m_signatureToOffs;
    SArray<BYTE>         m_compactLayoutBuffer;  // the buffer holding the finished compact layout data
                                                 // the first four bytes are a magic DWORD, so offset 0 is invalid

    void SetCompactLayoutWriter(ICompactLayoutWriter *pICLW)
    {
        m_pICLW = pICLW;
    }

    ICompactLayoutWriter *GetCompactLayoutWriter()
    {
        return m_pICLW;
    }

#ifdef BINDER
    void Output()
    {
        OutputCode(ProfiledHot);
        OutputCode(Unprofiled);
        OutputCode(ProfiledCold);

        OutputCodeInfo(ProfiledHot);
        OutputCodeInfo(ProfiledCold);  // actually both Unprofiled and ProfiledCold

        OutputGCInfo();
        OutputDebugInfo();
        OutputProfileData();
    }
#ifdef CLR_STANDALONE_BINDER
    void EmitMethodIL(mdToken methodDefToken);
    void EmitFieldRVA(mdToken fieldDefToken, RVA fieldRVA);
#endif
#endif

private:

    SArray<ULONG>               m_mapTypeRidToOffs;     // offset in compact layout buffer for each typedef rid
    ULONG                       m_compactLayoutOffs;    // offset of the current type in the compact layout data
    ULONG                       m_typeRidCount;         // number of valid entries in the m_mapTypeRidToOffs table
    SArray<BYTE>                m_userStringPool;       // this is the user string pool exactly like it is in meta data
    SArray<DWORD>               m_wellKnownTypesTable;  // this is an array of type def tokens for the well known types
#endif

public:
    ZapImage(Zapper *zapper);
    ~ZapImage();

    // ----------------------------------------------------------------------------------------------------------
    //
    // Utility function for converting ZapWriter * to ZapImage *. This cast should not be done directly by the code
    // so that the relationship between ZapWriter and ZapImage is abstracted away.
    //
    static ZapImage * GetImage(ZapWriter * pZapWriter)
    {
        return (ZapImage *)pZapWriter;
    }

    // ----------------------------------------------------------------------------------------------------------
    //
    // Add relocation record. This method is meant to be called from the Save method of custom ZapNodes right
    // before the given datastructure is written into the native image.
    //
    // Arguments:
    //    pSrc - the datastructure being written
    //    offset - offset of the relocation within the datastructure
    //    pTarget - target of the relocation
    //    targetOffset - adjusment of the target (usually 0)
    //    type - relocation type (IMAGE_REL_BASED_XXX enum, note that we have private additions to this enum:
    //              IMAGE_REL_BASED_PTR - architecture specific reloc of virtual address
    //              IMAGE_REL_BASED_ABSOLUTE_TAGGED - absolute stored in the middle 30-bits, used for fixups.
    //              IMAGE_REL_BASED_RELPTR - pointer stored as address relative delta
    //              IMAGE_REL_BASED_RELPTR32 - pointer stored as address relative 32-bit delta
    //
    void WriteReloc(PVOID pSrc, int offset, ZapNode * pTarget, int targetOffset, ZapRelocationType type);

    void Open(CORINFO_MODULE_HANDLE hModule, IMetaDataAssemblyEmit *pEmit);

    void InitializeSections();
    void InitializeSectionsForReadyToRun();

    // Wrapper of ZapWriter::NewVirtualSection that sets sectionType
    ZapVirtualSection * NewVirtualSection(ZapPhysicalSection * pPhysicalSection, DWORD sectionType /* ZapVirtualSectionType */, DWORD dwAlignment = 16, ZapVirtualSection * pInsertAfter = NULL)
    {
        ZapVirtualSection * pSection = ZapWriter::NewVirtualSection(pPhysicalSection, dwAlignment, pInsertAfter);
        pSection->SetSectionType(sectionType);
        return pSection;
    }

    void AllocateVirtualSections();

    HANDLE SaveImage(LPCWSTR wszOutputFileName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);

    void Preload();
    void LinkPreload();

#ifdef BINDER
    void SetNativeVersionResource(PVOID pvVersionResourceBlob, SIZE_T cbVersionResource);
#endif
    void SetVersionInfo(CORCOMPILE_VERSION_INFO * pVersionInfo);
    void SetDependencies(CORCOMPILE_DEPENDENCY *pDependencies, DWORD cDependencies);
    void SetPdbFileName(const SString &strFileName);

#ifdef CLR_STANDALONE_BINDER
    void AddNativeDependency (NativeManifestData * pNativeManifestData) {
        m_pNativeManifestData.Append(*pNativeManifestData);
    }
#endif

#ifdef WIN64EXCEPTIONS
    void SetRuntimeFunctionsDirectoryEntry();
#endif

    void SaveCorHeader();
    void SaveNativeHeader();
    void SaveCodeManagerEntry();

    void Compile();

    ZapMethodHeader * GetCompiledMethod(CORINFO_METHOD_HANDLE handle)
    {
        return m_CompiledMethods.Lookup(handle);
    }


    enum CompileStatus { LOOKUP_FAILED   = -2, COMPILE_FAILED   = -1,       // Failure
                         NOT_COMPILED    =  0, COMPILE_EXCLUDED =  1,       // Info
                         COMPILE_SUCCEED = 10, ALREADY_COMPILED = 11};      // Success

    static void __stdcall TryCompileMethodStub(LPVOID pContext, CORINFO_METHOD_HANDLE hStub, DWORD dwJitFlags);

    BOOL IsVTableGapMethod(mdMethodDef md);

    CompileStatus TryCompileMethodDef(mdMethodDef md, unsigned methodProfilingDataFlags);
    CompileStatus TryCompileInstantiatedMethod(CORINFO_METHOD_HANDLE handle, unsigned methodProfilingDataFlags);
    CompileStatus TryCompileMethodWorker(CORINFO_METHOD_HANDLE handle, mdMethodDef md, unsigned methodProfilingDataFlags);

    BOOL ShouldCompileMethodDef(mdMethodDef md);
    BOOL ShouldCompileInstantiatedMethod(CORINFO_METHOD_HANDLE handle);

    bool canIntraModuleDirectCall(CORINFO_METHOD_HANDLE  callerFtn,
                         CORINFO_METHOD_HANDLE  targetFtn,
                         CorInfoIndirectCallReason *pReason = NULL,
                         CORINFO_ACCESS_FLAGS   accessFlags = CORINFO_ACCESS_ANY);

    CORINFO_MODULE_HANDLE GetModuleHandle()
    {
        return m_hModule;
    }

    IMetaDataAssemblyEmit * GetAssemblyEmit()
    {
        return m_pAssemblyEmit;
    }

    ZapWrapperTable * GetWrappers()
    {
        return m_pWrappers;
    }

    ZapImportTable * GetImportTable()
    {
        return m_pImportTable;
    }

    ZapImportSectionsTable * GetImportSectionsTable()
    {
        return m_pImportSectionsTable;
    }

    ZapNode * GetEEInfoTable()
    {
        return m_pEEInfoTable;
    }

    ZapReadyToRunHeader * GetReadyToRunHeader()
    {
        _ASSERTE(IsReadyToRunCompilation());
        return (ZapReadyToRunHeader *)m_pNativeHeader;
    }

    ZapNode * GetInnerPtr(ZapNode * pNode, SSIZE_T offset);

    CorInfoRegionKind GetCurrentRegionKind()
    {
        return m_currentRegionKind;
    }

    //
    // Called from  ZapImportTable::PlaceBlob
    // to determine wheather to place the new signature Blob
    // into the HotImports or the ColdImports section.
    //
    // The Assert will fire if BeginRegion was not called
    // to setup the region
    //
    bool IsCurrentCodeRegionHot()
    {
        if (GetCurrentRegionKind() == CORINFO_REGION_HOT)
        {
            return true;
        }
        else if (GetCurrentRegionKind() == CORINFO_REGION_COLD)
        {
            return false;
        }
        _ASSERTE(!"unsupported RegionKind");
        return false;
    }

    //
    // Marks the start of a region where we want to place any
    // new signature Blobs into the Hot/Cold region
    //
    void BeginRegion(CorInfoRegionKind regionKind)
    {
        _ASSERTE(GetCurrentRegionKind() == CORINFO_REGION_NONE);
        m_currentRegionKind = regionKind;
    }

    //
    // Marks the end of a region and we no longer expect to
    // need any new signature Blobs
    //
    void EndRegion(CorInfoRegionKind regionKind)
    {
        _ASSERTE(GetCurrentRegionKind() == regionKind);
        m_currentRegionKind = CORINFO_REGION_NONE;
    }

    ICorCompilationDomain * GetDomain()
    {
        return m_zapper->m_pDomain;
    }

    ICorDynamicInfo * GetJitInfo()
    {
        return m_zapper->m_pEEJitInfo;
    }

    ICorCompileInfo * GetCompileInfo()
    {
        return m_zapper->m_pEECompileInfo;
    }

    ZapperOptions *    GetZapperOptions()
    {
        return m_zapper->m_pOpt;
    }

    ZapNode * GetHelperThunkIfExists(CorInfoHelpFunc ftnNum)
    {
        return m_pHelperThunks[ftnNum];
    }

    ZapNode * GetHelperThunk(CorInfoHelpFunc ftnNum);

    BOOL HasClassLayoutOrder()
    {
        return m_fHasClassLayoutOrder;
    }

    HRESULT PrintTokenDescription(CorZapLogLevel level, mdToken token);

    // ICorCompileDataStore

    // Returns ZapImage
    virtual ZapImage * GetZapImage();
    void Error(mdToken token, HRESULT error, LPCWSTR message);

    // Returns virtual section for EE datastructures
    ZapVirtualSection * GetSection(CorCompileSection section)
    {
        return m_pPreloadSections[section];
    }

    HRESULT LocateProfileData();
    HRESULT parseProfileData  ();
    HRESULT convertProfileDataFromV1();
    void RehydrateBasicBlockSection();
    void RehydrateTokenSection(int sectionFormat, unsigned int flagTable[255]);
    void RehydrateBlobStream();
    HRESULT RehydrateProfileData();
    HRESULT hashBBProfileData ();

    void              LoadProfileData();
    CorProfileData *  NewProfileData();
    CorProfileData *  GetProfileData();
    bool              CanConvertIbcData();

    CompileStatus     CompileProfileDataWorker(mdToken token, unsigned methodProfilingDataFlags);
    void              CompileProfileData();
#ifdef CLR_STANDALONE_BINDER
    PEDecoder *GetPEDecoder()
    {
        return &m_ModuleDecoder;
    }

    ZapMethodEntryPoint *GetMethodEntryPoint(CORINFO_METHOD_HANDLE handle, CORINFO_ACCESS_FLAGS accessFlags)
    {
        return m_pMethodEntryPoints->GetMethodEntryPoint(handle, accessFlags);
    }
#endif
};

class BinaryWriter
{
private:
    char *m_buffer;
    unsigned int m_length;
    unsigned int m_currentPosition;
    ZapHeap *m_heap;

private:
    // Make sure that the buffer is at least newLength bytes long;
    // expand it if necessary.
    void RequireLength(unsigned int newLength)
    {
        if (newLength <= m_length)
        {
            return;
        }

        if (newLength < (m_length * 3) / 2)
        {
            newLength = (m_length * 3) / 2;
        }

        char *newBuffer = new (m_heap) char[newLength];

        memcpy(newBuffer, m_buffer, m_length);

        m_length = newLength;
        m_buffer = newBuffer;
    }

public:
    BinaryWriter(unsigned int initialLength, ZapHeap *heap)
    {
        m_heap = heap;
        m_length = initialLength;
        m_buffer = new (m_heap) char[initialLength];
        m_currentPosition = 0;
    }

    template <typename T>
    void WriteAt(unsigned int position, const T &v)
    {
        RequireLength(position + sizeof(T));

        *(T *)(m_buffer + position) = v;
    }

    template <typename T>
    void Write(const T &v)
    {
        WriteAt<T>(m_currentPosition, v);
        m_currentPosition += sizeof(T);
    }

    void Write(const char *data, unsigned int length)
    {
        RequireLength(m_currentPosition + length);

        memcpy(m_buffer + m_currentPosition, data, length);
        m_currentPosition += length;
    }

    BYTE *GetBuffer()
    {
        return (BYTE *)m_buffer;
    }

    unsigned int GetWrittenSize()
    {
        return m_currentPosition;
    }
};

class ProfileReader
{
public:
    ProfileReader(void *buffer, ULONG length)
    {
        profileBuffer = (char *) buffer;
        bufferSize    = length;
        currentPos    = 0;
    }

    bool Seek(ULONG pos)
    {
        if (pos <= bufferSize)
        {
            currentPos = pos;
            return true;
        }
        else
        {
            _ASSERTE(!"ProfileReader:  attempt to seek out of bounds");
            return false;
        }
    }

    void *Read(ULONG size)
    {
        ULONG oldPos = currentPos;

        if (!Seek(currentPos + size))
        {
            return NULL;
        }

        return (void *)(profileBuffer + oldPos);
    }

    template <typename T> T Read()
    {
        T* pResult = (T*)Read(sizeof(T));

        if (!pResult)
        {
            ThrowHR(E_FAIL);
        }

        return *pResult;
    }

    // Read an integer a la BinaryReader.Read7BitEncodedInt.
    unsigned int Read7BitEncodedInt()
    {
        unsigned int result = 0;
        int shift = 0;
        unsigned char current = 0x80;

        while ((currentPos < bufferSize) &&
               (shift <= 28))
        {
            current = profileBuffer[currentPos++];
            result |= (current & 0x7f) << shift;
            shift += 7;

            if (!(current & 0x80))
            {
                return result;
            }
        }

        _ASSERTE(!"Improperly encoded value");
        ThrowHR(E_FAIL);
    }

    // Read a token given a 'memory' value--the last token of this type read
    // from the stream. The encoding takes advantage of the fact that two
    // adjacent tokens in the file are usually of the same type, and therefore
    // share a high byte. With the high byte removed the rest of the token can
    // be encoded more efficiently.
    mdToken ReadTokenWithMemory(mdToken &memory)
    {
        mdToken current;
        mdToken result;

        current = Read7BitEncodedInt();

        unsigned char highByte = ((current >> 24) & 0xff);

        if (highByte == 0)
        {
            result = current | (memory & 0xff000000);
        }
        else if (highByte == 0xff)
        {
            result = current & 0x00ffffff;
        }
        else
        {
            result = current;
        }

        memory = result;

        return result;
    }

    // Read a 32-bit flag value using a lookup table built while processing the
    // file. Flag values are represented by a one-byte index. If the index
    // hasn't occurred before in the file, it is followed by the four-byte flag
    // value it represents. The index 255 is used as an escape code--it is
    // always followed by a flag value.
    // flagTable must have 255 entries and they must all start as 0xFFFFFFFF.
    unsigned int ReadFlagWithLookup(unsigned int flagTable[255])
    {
        unsigned char index;
        unsigned int flags;

        index = Read<unsigned char>();

        if ((index < 255) && (flagTable[index] != 0xffffffff))
        {
            return flagTable[index];
        }

        flags = Read<unsigned int>();

        if (index < 255)
        {
            flagTable[index] = flags;
        }

        return flags;
    }

    ULONG GetCurrentPos()
    {
        _ASSERTE(currentPos <= bufferSize);
        return currentPos;
    }

private:
    char *profileBuffer;
    ULONG bufferSize;
    ULONG currentPos;
};

struct RSDS {
        DWORD magic;
        GUID  signature;
        DWORD age;
        char  path[MAX_PATH];
};

#define SEEK(pos)                                \
    if (!profileReader.Seek(pos)) return E_FAIL;

#define READ_SIZE(dst,type,size)                 \
    dst = (type *) profileReader.Read(size);     \
    if (!dst) return E_FAIL;

#define READ(dst,type)                           \
    READ_SIZE(dst,type,sizeof(type))

#endif // __ZAPIMAGE_H__
