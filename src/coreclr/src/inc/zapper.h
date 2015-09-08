//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef ZAPPER_H_
#define ZAPPER_H_

#include <winwrap.h>
#include <windows.h>
#include <stdlib.h>
#include <objbase.h>
#include <stddef.h>
#include <float.h>
#include <limits.h>

#include "sarray.h"
#include "sstring.h"
#include "shash.h"
#include "utilcode.h"
#include "corjit.h"
#ifdef FEATURE_FUSION
#include "binderngen.h"
#endif
#include "corcompile.h"
#include "corhlprpriv.h"
#include "ngen.h"
#include "corbbtprof.h"
#include "pedecoder.h"
#include "mscorsvc.h"
#include "holderinst.h"
#include "corpriv.h"

// For side by side issues, it's best to use the exported API calls to generate a
// Zapper Object instead of creating one on your own.

STDAPI NGenCreateNGenWorker(ICorSvcWorker **pCorSvcWorker, ILocalServerLifetime *pLocalServerLifetime, ICorSvcLogger *pCorSvcLogger);
STDAPI NGenCreateZapper(HANDLE* hZapper, NGenOptions* opt);
STDAPI NGenFreeZapper(HANDLE hZapper);
STDAPI NGenTryEnumerateFusionCache(HANDLE hZapper, LPCWSTR assemblyName, bool fPrint, bool fDelete);
STDAPI_(BOOL) NGenCompile(HANDLE hZapper, LPCWSTR path);


/* --------------------------------------------------------------------------- *
 * Zapper classes
 * --------------------------------------------------------------------------- */

class ZapperOptions;
class ZapperAttributionStats;
class ZapImage;
class ZapInfo;

typedef enum CorZapLogLevel
{
        CORZAP_LOGLEVEL_ERROR,
        CORZAP_LOGLEVEL_WARNING,
        CORZAP_LOGLEVEL_SUCCESS,
        CORZAP_LOGLEVEL_INFO,
} CorZapLogLevel;

class Zapper
{
    friend class ZapImage;
    friend class ZapInfo;
    friend class ZapILMetaData;

 private:

    //
    // Interfaces
    //

    ICorDynamicInfo         *m_pEEJitInfo;
    ICorCompileInfo         *m_pEECompileInfo;
    ICorJitCompiler         *m_pJitCompiler;
    IMetaDataDispenserEx    *m_pMetaDataDispenser;
    HMODULE                  m_hJitLib;
#ifdef _TARGET_AMD64_
    HMODULE                  m_hJitLegacy;
#endif

#ifdef ALLOW_SXS_JIT_NGEN
    ICorJitCompiler         *m_alternateJit;
    HMODULE                  m_hAltJITCompiler;
#endif // ALLOW_SXS_JIT_NGEN

    //
    // Options
    //

    ZapperOptions *         m_pOpt;
    BOOL                    m_fFreeZapperOptions;

    SString                 m_exeName;      // If an EXE is specified

    bool                    m_fromDllHost;

    //
    // Current assembly info
    //

    ICorCompilationDomain  *m_pDomain;
    CORINFO_ASSEMBLY_HANDLE m_hAssembly;
    IMDInternalImport      *m_pAssemblyImport;

    WCHAR                   m_outputPath[MAX_LONGPATH]; // Temp folder for creating the output file

    IMetaDataAssemblyEmit  *m_pAssemblyEmit;
    IMetaDataAssemblyEmit  *CreateAssemblyEmitter();

    //
    //
    // Status info
    //

    BOOL                    m_failed;
    CorInfoRegionKind       m_currentRegionKind;

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
    SString                 m_platformAssembliesPaths;
    SString                 m_trustedPlatformAssemblies;
    SString                 m_platformResourceRoots;
    SString                 m_appPaths;
    SString                 m_appNiPaths;
    SString                 m_platformWinmdPaths;

#ifdef FEATURE_LEGACYNETCF
    bool                    m_appCompatWP8;    // Whether we're using quirks mode for binding with NetCF semantics.
#endif

#endif // FEATURE_CORECLR || CROSSGEN_COMPILE

    bool                    m_fForceFullTrust;

#ifdef MDIL
    bool                    m_fEmbedMDIL;
#endif

    SString                 m_outputFilename;  // output target when coregen is emitting a combined IL/MDIL file.
                                                   // (an empty string here (temporarily) indicates the use of the depecrated /createmdil sitch.)
  public:

    struct assemblyDependencies
    {
        struct DependencyEntry
        {
            BSTR bstr;
            LoadHintEnum loadHint;
            NGenHintEnum ngenHint;
        };

        SArray<DependencyEntry> dependencyArray;
        NGenHintEnum ngenHint;
        SString displayName;

        assemblyDependencies()
        {
            Initialize();
            ngenHint = NGenDefault;
        }

        ~assemblyDependencies()
        {
            Cleanup();
        }

        void Reinitialize()
        {
            Cleanup();
            Initialize();
        }

        void SetNGenHint(NGenHintEnum ngenHint)
        {
            this->ngenHint = ngenHint;
        }

        NGenHintEnum GetNGenHint()
        {
            return ngenHint;
        }

        void SetDisplayName(const WCHAR *pStr)
        {
            displayName.Set(pStr);
        }

        const WCHAR *GetDisplayName()
        {
            return displayName.GetUnicode();
        }

        void Append(const WCHAR *pStr, LoadHintEnum loadHint = LoadDefault, NGenHintEnum ngenHint = NGenDefault)
        {
            // Don't append string if it's a duplicate
            for (COUNT_T i = 0; i < dependencyArray.GetCount(); i++)
            {
                if (wcscmp(dependencyArray[i].bstr, pStr) == 0)
                    return;
            }

            BSTRHolder bstr(::SysAllocString(pStr));
            DependencyEntry dependencyEntry;
            dependencyEntry.bstr = bstr.GetValue();
            dependencyEntry.loadHint = loadHint;
            dependencyEntry.ngenHint = ngenHint;

            dependencyArray.Append(dependencyEntry);
            bstr.SuppressRelease();
        }

        SAFEARRAY *GetSAFEARRAY()
        {
            SafeArrayHolder pDependencies(SafeArrayCreateVector(VT_BSTR, 0, dependencyArray.GetCount()));
            if (pDependencies.GetValue() == NULL) ThrowLastError();

            for (COUNT_T i = 0; i < dependencyArray.GetCount(); i++)
            {
                LONG indices[1];
                indices[0] = (LONG) i;
                IfFailThrow(SafeArrayPutElement(pDependencies, indices, this->dependencyArray[i].bstr));
            }

            pDependencies.SuppressRelease();
            return pDependencies.GetValue();
        }

        SAFEARRAY *GetLoadHintSAFEARRAY()
        {
            SafeArrayHolder pSettings(SafeArrayCreateVector(VT_UI4, 0, dependencyArray.GetCount()));
            if (pSettings.GetValue() == NULL) ThrowLastError();

            for (COUNT_T i = 0; i < dependencyArray.GetCount(); i++)
            {
                LONG indices[1];
                indices[0] = (LONG) i;
                IfFailThrow(SafeArrayPutElement(pSettings, indices, &this->dependencyArray[i].loadHint));
            }

            pSettings.SuppressRelease();
            return pSettings.GetValue();
        }

        SAFEARRAY *GetNGenHintSAFEARRAY()
        {
            SafeArrayHolder pSettings(SafeArrayCreateVector(VT_UI4, 0, dependencyArray.GetCount()));
            if (pSettings.GetValue() == NULL) ThrowLastError();

            for (COUNT_T i = 0; i < dependencyArray.GetCount(); i++)
            {
                LONG indices[1];
                indices[0] = (LONG) i;
                IfFailThrow(SafeArrayPutElement(pSettings, indices, &this->dependencyArray[i].ngenHint));
            }

            pSettings.SuppressRelease();
            return pSettings.GetValue();
        }

    private:
        void Initialize()
        {
            dependencyArray.SetCount(0);
            // Should we reinitialize ngenHint to the default value as well?
        }

        void Cleanup()
        {
            for (COUNT_T i = 0; i < dependencyArray.GetCount(); i++)
            {
                ::SysFreeString(dependencyArray[i].bstr);
            }
        }
    } m_assemblyDependencies;

#ifndef FEATURE_CORECLR // No load lists on CoreCLR
    struct loadLists
    {
        loadLists() :
            m_loadAlwaysList(NULL),
            m_loadSometimesList(NULL),
            m_loadNeverList(NULL)
        {
        }

        SAFEARRAY *m_loadAlwaysList;
        SAFEARRAY *m_loadSometimesList;
        SAFEARRAY *m_loadNeverList;

        void SetLoadLists(SAFEARRAY *loadAlwaysList, SAFEARRAY *loadSometimesList, SAFEARRAY *loadNeverList)
        {
            m_loadAlwaysList    = loadAlwaysList;
            m_loadSometimesList = loadSometimesList;
            m_loadNeverList     = loadNeverList;
        }

    } m_loadLists;

    void SetLoadLists(SAFEARRAY *loadAlwaysList, SAFEARRAY *loadSometimesList, SAFEARRAY *loadNeverList)
    {
        m_loadLists.SetLoadLists(loadAlwaysList, loadSometimesList, loadNeverList);
    }

    void SetAssemblyHardBindList()
    {
        SAFEARRAY *loadAlwaysList = m_loadLists.m_loadAlwaysList;
        if (loadAlwaysList == NULL)
        {
            return;
        }

        LONG ubound = 0;
        IfFailThrow(SafeArrayGetUBound(loadAlwaysList, 1, &ubound));

        BSTR *pArrBstr = NULL;
        IfFailThrow(SafeArrayAccessData(loadAlwaysList, reinterpret_cast<void **>(&pArrBstr)));

        EX_TRY
        {
            _ASSERTE((ubound + 1) >= 0);
            m_pEECompileInfo->SetAssemblyHardBindList(reinterpret_cast<LPWSTR *>(pArrBstr), ubound + 1);
        }
        EX_CATCH
        {
            // If something went wrong, try to unlock the OLE array
            // Do not verify the outcome, as we can do nothing about it
            SafeArrayUnaccessData(loadAlwaysList);
            EX_RETHROW;
        }
        EX_END_CATCH_UNREACHABLE;

        IfFailThrow(SafeArrayUnaccessData(loadAlwaysList));
    }
#endif // !FEATURE_CORECLR

  public:

    Zapper(ZapperOptions *pOpt);
    Zapper(NGenOptions *pOpt, bool fromDllHost = false);

    void Init(ZapperOptions *pOpt, bool fFreeZapperOptions= false);

    static Zapper *NewZapper(NGenOptions *pOpt, bool fromDllHost = false)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;
        return new Zapper(pOpt, fromDllHost);
    }

    ~Zapper();

    // The arguments control which native image of mscorlib to use.
    // This matters for hardbinding.
#ifdef BINDER
    void InitEE(BOOL fForceDebug, BOOL fForceProfile, BOOL fForceInstrument, ICorCompileInfo *compileInfo, ICorDynamicInfo *dynamicInfo);
#else
    void InitEE(BOOL fForceDebug, BOOL fForceProfile, BOOL fForceInstrument);
    void LoadAndInitializeJITForNgen(LPCWSTR pwzJitName, OUT HINSTANCE* phJit, OUT ICorJitCompiler** ppICorJitCompiler);
#endif

#ifdef FEATURE_FUSION
    HRESULT TryEnumerateFusionCache(LPCWSTR assemblyName, bool fPrint, bool fDelete);
    int EnumerateFusionCache(LPCWSTR assemblyName, bool fPrint, bool fDelete,
            CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig = NULL);
    void PrintFusionCacheEntry(CorSvcLogLevel logLevel, IAssemblyName *pZapAssemblyName);
    void DeleteFusionCacheEntry(IAssemblyName *pZapAssemblyName);
    void DeleteFusionCacheEntry(LPCWSTR assemblyName, CORCOMPILE_NGEN_SIGNATURE *pNativeImageSig);

    void PrintDependencies(
            IMetaDataAssemblyImport * pAssemblyImport,
            CORCOMPILE_DEPENDENCY * pDependencies,
            COUNT_T cDependencies,
            SString &s);
    BOOL VerifyDependencies(
            IMDInternalImport * pAssemblyImport,
            CORCOMPILE_DEPENDENCY * pDependencies,
            COUNT_T cDependencies);

    void PrintAssemblyVersionInfo(IAssemblyName *pZapAssemblyName, SString &s);

    IAssemblyName *GetAssemblyFusionName(IMetaDataAssemblyImport *pImport);
    IAssemblyName *GetAssemblyRefFusionName(IMetaDataAssemblyImport *pImport,
                                            mdAssemblyRef ar);
#endif //FEATURE_FUSION

    BOOL IsAssembly(LPCWSTR path);

    void CreateDependenciesLookupDomain();
    void ComputeDependencies(LPCWSTR pAssemblyName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    void ComputeAssemblyDependencies(CORINFO_ASSEMBLY_HANDLE hAssembly);

    void CreatePdb(BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath);
    void CreatePdbInCurrentDomain(BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath);

    void DefineOutputAssembly(SString& strAssemblyName, ULONG * pHashAlgId);

    HRESULT Compile(LPCWSTR path, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig = NULL);
    void    DontUseProfileData();  
    bool    HasProfileData();     

    void CompileAssembly(CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    ZapImage * CompileModule(CORINFO_MODULE_HANDLE hModule,
                             IMetaDataAssemblyEmit *pEmit);
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES 
    void CompileNonManifestModules(ULONG hashAlgId, SArray<HANDLE> &hFiles);
    static void * GetMapViewOfFile(
                                HANDLE hFile,
                                DWORD * pdwFileLen);
    static void ComputeHashValue(HANDLE hFile, int hashAlg,
                                 BYTE **ppHashValue, DWORD *cbHashValue);
#endif // FEATURE_MULTIMODULE_ASSEMBLIES 
    void InstallCompiledAssembly(LPCWSTR szAssemblyName, LPCWSTR szNativeImagePath, HANDLE hFile, SArray<HANDLE> &hFiles);

    HRESULT GetExceptionHR();

    void Success(LPCWSTR format, ...);
    void Error(LPCWSTR format, ...);
    void Warning(LPCWSTR format, ...);
    void Info(LPCWSTR format, ...);
    void Print(CorZapLogLevel level, LPCWSTR format, ...);
    void Print(CorZapLogLevel level, LPCWSTR format, va_list args);
    void PrintErrorMessage(CorZapLogLevel level, Exception *ex);
    void PrintErrorMessage(CorZapLogLevel level, HRESULT hr);
    void ReportEventNGEN(WORD wType, DWORD dwEventID, LPCWSTR format, ...);

    BOOL            CheckAssemblyUpToDate(CORINFO_ASSEMBLY_HANDLE hAssembly, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    BOOL            TryToInstallFromRepository(CORINFO_ASSEMBLY_HANDLE hAssembly, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    BOOL            TryToInstallFromRepositoryDir(SString &strNativeImageDir,
                                                  SString &strSimpleName,
                                                  CORCOMPILE_NGEN_SIGNATURE *pNativeImageSig,
                                                  BOOL *pfHitMismatchedVersion,
                                                  BOOL *pfHitMismatchedDependencies,
                                                  BOOL useHardLink = FALSE);
    void            CopyAndInstallFromRepository(LPCWSTR lpszNativeImageDir, 
                                                 LPCWSTR lpszNativeImageName, 
                                                 CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig,
                                                 BOOL useHardLink = FALSE);
    void            InstallFromRepository(LPCWSTR lpszNativeImage, 
                                          CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);

    void            CopyDirectory(LPCWSTR srcPath, LPCWSTR dstPath);
    void            CleanDirectory(LPCWSTR path);
    void            TryCleanDirectory(LPCWSTR path);
    static void     TryCleanDirectory(Zapper * pZapper);

    void            GetOutputFolder();

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
    void SetPlatformAssembliesPaths(LPCWSTR pwzPlatformAssembliesPaths);
    void SetTrustedPlatformAssemblies(LPCWSTR pwzTrustedPlatformAssemblies);
    void SetPlatformResourceRoots(LPCWSTR pwzPlatformResourceRoots);
    void SetAppPaths(LPCWSTR pwzAppPaths);
    void SetAppNiPaths(LPCWSTR pwzAppNiPaths);
    void SetPlatformWinmdPaths(LPCWSTR pwzPlatformWinmdPaths);

#ifdef FEATURE_LEGACYNETCF
    void SetAppCompatWP8(bool val);
#endif

#ifdef MDIL
    void SetEmbedMDIL(bool val);
    void SetCompilerFlag(DWORD val);
#endif

    void SetForceFullTrust(bool val);
#endif // FEATURE_CORECLR || CROSSGEN_COMPILE

    void SetOutputFilename(LPCWSTR pwszOutputFilename);
    SString GetOutputFileName();

    void SetLegacyMode();

 private:

    void DestroyDomain();
    void CleanupAssembly();

    void CreateCompilationDomain();
    void SetContextInfo(LPCWSTR assemblyName = NULL);

    void InitializeCompilerFlags(CORCOMPILE_VERSION_INFO * pVersionInfo);

    // DomainCallback is subclassed by each method that would like to compile in a given domain
    class DomainCallback
    {
    public:
        virtual void doCallback() = NULL;
    };

    static HRESULT __stdcall GenericDomainCallback(LPVOID pvArgs);
    void InvokeDomainCallback(DomainCallback *callback);

    void CompileInCurrentDomain(__in LPCWSTR path, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    void ComputeDependenciesInCurrentDomain(LPCWSTR pAssemblyName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig);
    void CreateDependenciesLookupDomainInCurrentDomain();

    void HangWorker(LPCWSTR hangKey, LPCWSTR insideHangKey);
};

class ZapperOptions
{
  public:
    enum StatOptions
    {
        NO_STATS            = 0,
        DEFAULT_STATS       = 1,
        FIXUP_STATS         = DEFAULT_STATS << 1,
        CALL_STATS          = FIXUP_STATS << 1,
        ATTRIB_STATS        = CALL_STATS << 1,
        ALL_STATS           = ~NO_STATS,
    };

    LPWSTR      m_zapSet;               // Add to zap string. Use to get a private scope of ngen images, for debugging/testing

    LPCWSTR     m_repositoryDir;        // Directory with prebuilt native images
    RepositoryFlags m_repositoryFlags;  // Copy the native images back to NativeImagesDir

    bool        m_autodebug;            // Use the debug setting specified by the IL module

    MethodNamesList* m_onlyMethods;     // only methods to process
    MethodNamesList* m_excludeMethods;  // excluded these methods
    mdToken          m_onlyOneMethod;   // only compile method with matching token

    bool        m_silent;               // Dont spew any text output
    bool        m_verbose;              // Spew extra text ouput
    bool        m_ignoreErrors;         // Continue in the face of errors
    unsigned    m_statOptions;          // print statisitcs on number of methods, size of code ...
    bool        m_ngenProfileImage;     // ngening with "/prof"

                                        // Which optimizations should be done
    bool        m_ignoreProfileData;    // Don't use profile data
    bool        m_noProcedureSplitting; // Don't do procedure splitting

    bool        m_fHasAnyProfileData;   // true if we successfully loaded and used 
                                        //  any profile data when compiling this assembly

    bool        m_fPartialNGen;         // Generate partial NGen images using IBC data

    bool        m_fPartialNGenSet;      // m_fPartialNGen has been set through the environment

    bool        m_fAutoNGen;            // This is an automatic NGen request

    bool        m_fRepositoryOnly;      // Install from repository only, no real NGen

    bool        m_fNGenLastRetry;       // This is retry of the compilation

    unsigned    m_compilerFlags;

    bool       m_legacyMode;          // true if the zapper was invoked using legacy mode

#ifdef FEATURE_CORECLR
    bool        m_fNoMetaData;          // Do not copy metadata and IL to native image
#endif

    ZapperOptions();
    ~ZapperOptions();
};

struct NGenPrivateAttributesClass : public NGenPrivateAttributes
{
    NGenPrivateAttributesClass()
    {
        Flags    = 0;
        ZapStats = 0;
        DbgDir   = NULL;
    }

private:
    // Make sure that copies of this object aren't inadvertently created
    NGenPrivateAttributesClass(const NGenPrivateAttributesClass &init);
    const NGenPrivateAttributesClass &operator =(const NGenPrivateAttributesClass &rhs);

public:
    ~NGenPrivateAttributesClass()
    {
        if (DbgDir)
        {
            ::SysFreeString(DbgDir);
            DbgDir = NULL;
        }
    }
};

#endif // ZAPPER_H_

