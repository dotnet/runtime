// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    SString                 m_outputPath; // Temp folder for creating the output file

    IMetaDataAssemblyEmit  *m_pAssemblyEmit;
    IMetaDataAssemblyEmit  *CreateAssemblyEmitter();

    //
    //
    // Status info
    //

    BOOL                    m_failed;
    CorInfoRegionKind       m_currentRegionKind;

    SString                 m_platformAssembliesPaths;
    SString                 m_trustedPlatformAssemblies;
    SString                 m_platformResourceRoots;
    SString                 m_appPaths;
    SString                 m_appNiPaths;
    SString                 m_platformWinmdPaths;

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    SString                 m_CLRJITPath;
    bool                    m_fDontLoadJit;
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
#if !defined(NO_NGENPDB)
    SString                 m_DiasymreaderPath;
#endif // !defined(NO_NGENPDB)
    bool                    m_fForceFullTrust;

    SString                 m_outputFilename;

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
    void InitEE(BOOL fForceDebug, BOOL fForceProfile, BOOL fForceInstrument);
    void LoadAndInitializeJITForNgen(LPCWSTR pwzJitName, OUT HINSTANCE* phJit, OUT ICorJitCompiler** ppICorJitCompiler);


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

    void SetPlatformAssembliesPaths(LPCWSTR pwzPlatformAssembliesPaths);
    void SetTrustedPlatformAssemblies(LPCWSTR pwzTrustedPlatformAssemblies);
    void SetPlatformResourceRoots(LPCWSTR pwzPlatformResourceRoots);
    void SetAppPaths(LPCWSTR pwzAppPaths);
    void SetAppNiPaths(LPCWSTR pwzAppNiPaths);
    void SetPlatformWinmdPaths(LPCWSTR pwzPlatformWinmdPaths);
    void SetForceFullTrust(bool val);

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    void SetCLRJITPath(LPCWSTR pwszCLRJITPath);
    void SetDontLoadJit();
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

#if !defined(NO_NGENPDB)
    void SetDiasymreaderPath(LPCWSTR pwzDiasymreaderPath);
#endif // !defined(NO_NGENPDB)

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

    CORJIT_FLAGS m_compilerFlags;

    bool       m_legacyMode;          // true if the zapper was invoked using legacy mode

    bool        m_fNoMetaData;          // Do not copy metadata and IL to native image

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

