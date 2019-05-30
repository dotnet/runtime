// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"

#include "coregen.h"

#include "clr/fs/dir.h"

#pragma warning(push)
#pragma warning(disable: 4995)
#include "shlwapi.h"
#pragma warning(pop)

extern const WCHAR g_pwBaseLibrary[];
extern bool g_fAllowNativeImages;
bool g_fNGenMissingDependenciesOk;
bool g_fNGenWinMDResilient;

#ifdef FEATURE_READYTORUN_COMPILER
bool g_fReadyToRunCompilation;
bool g_fLargeVersionBubble;
#endif

static bool s_fNGenNoMetaData;

/* --------------------------------------------------------------------------- *
 * Public entry points for ngen
 * --------------------------------------------------------------------------- */
// For side by side issues, it's best to use the exported API calls to generate a
// Zapper Object instead of creating one on your own.


STDAPI NGenWorker(LPCWSTR pwzFilename, DWORD dwFlags, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzOutputFilename=NULL, LPCWSTR pwzPlatformWinmdPaths=NULL, ICorSvcLogger *pLogger = NULL, LPCWSTR pwszCLRJITPath = nullptr)
{    
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    Zapper* zap = NULL;

    EX_TRY
    {
        NGenOptions ngo = {0};
        ngo.dwSize = sizeof(NGenOptions);
        
        // V1
        
        ngo.fDebug = false;
        ngo.fDebugOpt = false;
        ngo.fProf = false;
        ngo.fSilent = (dwFlags & NGENWORKER_FLAGS_SILENT) != 0;
        ngo.lpszExecutableFileName = pwzFilename;

        // V2 (Whidbey)

        ngo.fInstrument = !!(dwFlags & NGENWORKER_FLAGS_TUNING);
        ngo.fWholeProgram = false;
        ngo.fProfInfo = false;

        ngo.lpszRepositoryDir = NULL;
        ngo.repositoryFlags = RepositoryDefault;

        ngo.dtRequested = DT_NIL;
        ngo.lpszDebugDir = NULL;

        ngo.fNoInstall = false;

        ngo.fEmitFixups = false;
        ngo.fFatHeaders = false;

        ngo.fVerbose = (dwFlags & NGENWORKER_FLAGS_VERBOSE) != 0;
        ngo.uStats = false;

        ngo.fNgenLastRetry = false;

        s_fNGenNoMetaData = (dwFlags & NGENWORKER_FLAGS_NO_METADATA) != 0;

        zap = Zapper::NewZapper(&ngo);

        if (pwzOutputFilename)
            zap->SetOutputFilename(pwzOutputFilename);

        if (pwzPlatformAssembliesPaths != nullptr)
            zap->SetPlatformAssembliesPaths(pwzPlatformAssembliesPaths);

        if (pwzTrustedPlatformAssemblies != nullptr)
            zap->SetTrustedPlatformAssemblies(pwzTrustedPlatformAssemblies);

        if (pwzPlatformResourceRoots != nullptr)
            zap->SetPlatformResourceRoots(pwzPlatformResourceRoots);

        if (pwzAppPaths != nullptr)
            zap->SetAppPaths(pwzAppPaths);

        if (pwzPlatformWinmdPaths != nullptr)
            zap->SetPlatformWinmdPaths(pwzPlatformWinmdPaths);

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        if (pwszCLRJITPath != nullptr)
            zap->SetCLRJITPath(pwszCLRJITPath);
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

        g_fNGenMissingDependenciesOk = !!(dwFlags & NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK);

#ifdef FEATURE_WINMD_RESILIENT
        g_fNGenWinMDResilient = !!(dwFlags & NGENWORKER_FLAGS_WINMD_RESILIENT);
#endif

#ifdef FEATURE_READYTORUN_COMPILER
        g_fReadyToRunCompilation = !!(dwFlags & NGENWORKER_FLAGS_READYTORUN);
        g_fLargeVersionBubble = !!(dwFlags & NGENWORKER_FLAGS_LARGEVERSIONBUBBLE);
#endif

        if (pLogger != NULL)
        {
            GetSvcLogger()->SetSvcLogger(pLogger);
        }

        hr = zap->Compile(pwzFilename);
    }
    EX_CATCH_HRESULT(hr);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

STDAPI CreatePDBWorker(LPCWSTR pwzAssemblyPath, LPCWSTR pwzPlatformAssembliesPaths, LPCWSTR pwzTrustedPlatformAssemblies, LPCWSTR pwzPlatformResourceRoots, LPCWSTR pwzAppPaths, LPCWSTR pwzAppNiPaths, LPCWSTR pwzPdbPath, BOOL fGeneratePDBLinesInfo, LPCWSTR pwzManagedPdbSearchPath, LPCWSTR pwzPlatformWinmdPaths, LPCWSTR pwzDiasymreaderPath)
{    
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    Zapper* zap = NULL;

    EX_TRY
    {
        GetCompileInfo()->SetIsGeneratingNgenPDB(TRUE);

        NGenOptions ngo = {0};
        ngo.dwSize = sizeof(NGenOptions);

        zap = Zapper::NewZapper(&ngo);

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        zap->SetDontLoadJit();
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

        if (pwzPlatformAssembliesPaths != nullptr)
            zap->SetPlatformAssembliesPaths(pwzPlatformAssembliesPaths);

        if (pwzTrustedPlatformAssemblies != nullptr)
            zap->SetTrustedPlatformAssemblies(pwzTrustedPlatformAssemblies);

        if (pwzPlatformResourceRoots != nullptr)
            zap->SetPlatformResourceRoots(pwzPlatformResourceRoots);

        if (pwzAppPaths != nullptr)
            zap->SetAppPaths(pwzAppPaths);

        if (pwzAppNiPaths != nullptr)
            zap->SetAppNiPaths(pwzAppNiPaths);

        if (pwzPlatformWinmdPaths != nullptr)
            zap->SetPlatformWinmdPaths(pwzPlatformWinmdPaths);

#if !defined(NO_NGENPDB)
        if (pwzDiasymreaderPath != nullptr)
            zap->SetDiasymreaderPath(pwzDiasymreaderPath);
#endif // !defined(NO_NGENPDB)

        BSTRHolder strAssemblyPath(::SysAllocString(pwzAssemblyPath));
        BSTRHolder strPdbPath(::SysAllocString(pwzPdbPath));
        BSTRHolder strManagedPdbSearchPath(::SysAllocString(pwzManagedPdbSearchPath));

        // Zapper::CreatePdb is shared code for both desktop NGEN PDBs and coreclr
        // crossgen PDBs.
        zap->CreatePdb(
            strAssemblyPath, 

            // On desktop with fusion AND on the phone, the various binders always expect
            // the native image to be specified here.
            strAssemblyPath, 
            
            strPdbPath, 
            fGeneratePDBLinesInfo, 
            strManagedPdbSearchPath);
    }
    EX_CATCH_HRESULT(hr);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


/* --------------------------------------------------------------------------- *
 * Options class
 * --------------------------------------------------------------------------- */

ZapperOptions::ZapperOptions() :
  m_repositoryDir(NULL),
  m_repositoryFlags(RepositoryDefault),
  m_autodebug(false),
  m_onlyOneMethod(0),
  m_silent(true),
  m_verbose(false),
  m_ignoreErrors(true),
  m_statOptions(0),
  m_ngenProfileImage(false),
  m_ignoreProfileData(false),
  m_noProcedureSplitting(false),
  m_fHasAnyProfileData(false),
  m_fPartialNGen(false),
  m_fPartialNGenSet(false),
  m_fNGenLastRetry(false),
  m_compilerFlags(),
  m_fNoMetaData(s_fNGenNoMetaData)
{
    SetCompilerFlags();

    m_zapSet = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapSet);
    if (m_zapSet != NULL && wcslen(m_zapSet) > 3)
    {
        delete [] m_zapSet;
        m_zapSet = NULL;
    }

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_DisableIBC))
    {
        m_ignoreProfileData = true;
    }

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NoProcedureSplitting))
    {
        m_noProcedureSplitting = true;
    }

    DWORD partialNGen = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PartialNGen);
    if (partialNGen != (DWORD)(-1))
    {
        m_fPartialNGenSet = true;
        m_fPartialNGen = partialNGen != 0;
    }

#ifdef _DEBUG
    m_onlyOneMethod = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NGenOnlyOneMethod);
#endif
}

ZapperOptions::~ZapperOptions()
{
    if (m_zapSet != NULL)
        delete [] m_zapSet;

    if (m_repositoryDir != NULL)
        delete [] m_repositoryDir;
}

void ZapperOptions::SetCompilerFlags(void)
{
    m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_RELOC);
    m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PREJIT);

#if defined(_TARGET_ARM_)
# if defined(PLATFORM_UNIX)
    m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_RELATIVE_CODE_RELOCS);
# endif // defined(PLATFORM_UNIX)
#endif // defined(_TARGET_ARM_)
}

/* --------------------------------------------------------------------------- *
 * Zapper class
 * --------------------------------------------------------------------------- */

Zapper::Zapper(NGenOptions *pOptions, bool fromDllHost)
{
    ZapperOptions *zo = new ZapperOptions();

    // We can version NGenOptions by looking at the dwSize variable

    NGenOptions currentVersionOptions;
    memcpy(&currentVersionOptions, pOptions, pOptions->dwSize);

    if (pOptions->dwSize < sizeof(currentVersionOptions))
    {
        // Clear out the memory. This is redundant with the explicit
        // initializations below, but is meant to be a backup.

        ZeroMemory(PBYTE(&currentVersionOptions) + pOptions->dwSize,
                   sizeof(currentVersionOptions) - pOptions->dwSize);

        // Initialize all the fields added in the current version

        currentVersionOptions.fInstrument = false;
        currentVersionOptions.fWholeProgram = false;
        currentVersionOptions.fProfInfo = false;

        currentVersionOptions.dtRequested = DT_NIL;
        currentVersionOptions.lpszDebugDir = NULL;

        currentVersionOptions.lpszRepositoryDir = NULL;
        currentVersionOptions.repositoryFlags = RepositoryDefault;

        currentVersionOptions.fNoInstall = false;

        currentVersionOptions.fEmitFixups = false;
        currentVersionOptions.fFatHeaders = false;

        currentVersionOptions.fVerbose = false;
        currentVersionOptions.uStats = 0;

        currentVersionOptions.fNgenLastRetry = false;

        currentVersionOptions.fAutoNGen = false;

        currentVersionOptions.fRepositoryOnly = false;
    }

    pOptions = &currentVersionOptions;

    zo->m_compilerFlags.Reset();
    zo->SetCompilerFlags();
    zo->m_autodebug = true;

    if (pOptions->fDebug)
    {
        zo->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);
        zo->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
        zo->m_autodebug = false;
    }

    if (pOptions->fProf)
    {
        zo->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);
    }


    if (pOptions->fInstrument)
        zo->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR);

    zo->m_verbose = pOptions->fVerbose;
    zo->m_statOptions = pOptions->uStats;

    zo->m_silent = pOptions->fSilent;

    if (pOptions->lpszExecutableFileName != NULL)
    {
        m_exeName.Set(pOptions->lpszExecutableFileName);
    }

    if (pOptions->fNgenLastRetry)
    {
        zo->m_fNGenLastRetry = true;

        //
        // Things that we turn off when this is the last retry for ngen
        // 
        zo->m_ignoreProfileData = true;   // ignore any IBC profile data

#ifdef _TARGET_ARM_
        //
        // On ARM, we retry compilation for large images when we hit overflow of IMAGE_REL_BASED_THUMB_BRANCH24 relocations.
        // Disable procedure spliting for retry because of it depends on IMAGE_REL_BASED_THUMB_BRANCH24 relocations.
        // See related code in code:ZapInfo::getRelocTypeHint
        //
        zo->m_noProcedureSplitting = true;
#endif
    }

    zo->m_fAutoNGen = pOptions->fAutoNGen;

    zo->m_fRepositoryOnly = pOptions->fRepositoryOnly;

    m_fromDllHost = fromDllHost;

    Init(zo, true);
}


Zapper::Zapper(ZapperOptions *pOptions)
{
    Init(pOptions);
}

void Zapper::Init(ZapperOptions *pOptions, bool fFreeZapperOptions)
{
    m_pEEJitInfo = NULL;
    m_pEECompileInfo = NULL;
    m_pJitCompiler = NULL;
    m_pMetaDataDispenser = NULL;
    m_hJitLib = NULL;
#ifdef _TARGET_AMD64_
    m_hJitLegacy = NULL;
#endif

    m_pOpt = pOptions;

    m_pDomain = NULL;
    m_hAssembly = NULL;
    m_pAssemblyImport = NULL;
    m_failed = FALSE;
    m_currentRegionKind = CORINFO_REGION_NONE;

    m_pAssemblyEmit = NULL;
    m_fFreeZapperOptions = fFreeZapperOptions;

#ifdef LOGGING
    InitializeLogging();
#endif

    HRESULT hr;

    //
    // Get metadata dispenser interface
    //

    IfFailThrow(MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                                     IID_IMetaDataDispenserEx, (void **)&m_pMetaDataDispenser));

    //
    // Make sure we don't duplicate assembly refs and file refs
    //

    VARIANT opt;
    hr = m_pMetaDataDispenser->GetOption(MetaDataCheckDuplicatesFor, &opt);
    _ASSERTE(SUCCEEDED(hr));

    _ASSERTE(V_VT(&opt) == VT_UI4);
    V_UI4(&opt) |= MDDupAssemblyRef | MDDupFile;

    hr = m_pMetaDataDispenser->SetOption(MetaDataCheckDuplicatesFor, &opt);
    _ASSERTE(SUCCEEDED(hr));


#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    m_fDontLoadJit = false;
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
}

// LoadAndInitializeJITForNgen: load the JIT dll into the process, and initialize it (call the UtilCode initialization function,
// check the JIT-EE interface GUID, etc.). (This is similar to LoadAndInitializeJIT.)
//
// Parameters:
//
// pwzJitName        - The filename of the JIT .dll file to load. E.g., "altjit.dll".
// phJit             - On return, *phJit is the Windows module handle of the loaded JIT dll. It will be NULL if the load failed.
// ppICorJitCompiler - On return, *ppICorJitCompiler is the ICorJitCompiler* returned by the JIT's getJit() entrypoint.
//                     It is NULL if the JIT returns a NULL interface pointer, or if the JIT-EE interface GUID is mismatched.
//
// Note that both *phJit and *ppICorJitCompiler will be non-NULL on success. On failure, an exception is thrown.
void Zapper::LoadAndInitializeJITForNgen(LPCWSTR pwzJitName, OUT HINSTANCE* phJit, OUT ICorJitCompiler** ppICorJitCompiler)
{
    _ASSERTE(phJit != NULL);
    _ASSERTE(ppICorJitCompiler != NULL);

    *phJit = NULL;
    *ppICorJitCompiler = NULL;

    HRESULT hr = E_FAIL;

    // Note: FEATURE_MERGE_JIT_AND_ENGINE is defined for the Desktop crossgen compilation as well.
    //
    PathString CoreClrFolder;
    extern HINSTANCE g_hThisInst;

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    if (m_fDontLoadJit)
    {
        return;
    }

    if (m_CLRJITPath.GetCount() > 0)
    {
        // If we have been asked to load a specific JIT binary, load it.
        CoreClrFolder.Set(m_CLRJITPath);
        hr = S_OK;
    }
    else 
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    if (WszGetModuleFileName(g_hThisInst, CoreClrFolder))
    {
        hr = CopySystemDirectory(CoreClrFolder, CoreClrFolder);
        if (SUCCEEDED(hr))
        {
            CoreClrFolder.Append(pwzJitName);

        }
    }

    if (SUCCEEDED(hr))
    {
        *phJit = ::WszLoadLibrary(CoreClrFolder);
        if (*phJit == NULL)
        {
            hr = HRESULT_FROM_GetLastError();
        }
        else
        {
            hr = S_OK;
        }
    }

    if (FAILED(hr))
    {
        Error(W("Unable to load Jit Compiler: %s, hr=0x%08x\r\n"), pwzJitName, hr);
        ThrowLastError();
    }

    typedef void (__stdcall* pJitStartup)(ICorJitHost* host);
    pJitStartup jitStartupFn = (pJitStartup)GetProcAddress(*phJit, "jitStartup");
    if (jitStartupFn != nullptr)
    {
        jitStartupFn(m_pEECompileInfo->GetJitHost());
    }

    //get the appropriate compiler interface
    typedef ICorJitCompiler* (__stdcall* pGetJitFn)();
    pGetJitFn getJitFn = (pGetJitFn) GetProcAddress(*phJit, "getJit");
    if (getJitFn == NULL)
    {
        Error(W("Unable to load main Jit function: %s\r\n"), pwzJitName);
        ThrowLastError();
    }

    ICorJitCompiler* pICorJitCompiler = (*getJitFn)();
    if (pICorJitCompiler == NULL)
    {
        Error(W("Unable to initialize Jit Compiler: %s\r\n"), pwzJitName);
        ThrowLastError();
    }

    // Check the JIT version identifier.

    GUID versionId;
    memset(&versionId, 0, sizeof(GUID));
    pICorJitCompiler->getVersionIdentifier(&versionId);

    if (memcmp(&versionId, &JITEEVersionIdentifier, sizeof(GUID)) != 0)
    {
        // Mismatched version ID. Fail the load.
        Error(W("Jit Compiler has wrong version identifier\r\n"));
        ThrowHR(E_FAIL);
    }

    // The JIT has loaded and passed the version identifier test, so publish the JIT interface to the caller.
    *ppICorJitCompiler = pICorJitCompiler;
}

void Zapper::InitEE(BOOL fForceDebug, BOOL fForceProfile, BOOL fForceInstrument)
{
    if (m_pEECompileInfo != NULL)
        return;

#if defined(FEATURE_COMINTEROP)
    //
    // Initialize COM
    //

    if (!m_fromDllHost)
    {
        CoInitializeEx(NULL, COINIT_MULTITHREADED);
    }

#endif // FEATURE_COMINTEROP

    //
    // Get EE compiler interface and initialize the EE
    //

    m_pEECompileInfo = GetCompileInfo();

    if (m_pOpt->m_statOptions)
        IfFailThrow(m_pEECompileInfo->SetVerboseLevel (CORCOMPILE_STATS));

    if (m_pOpt->m_verbose)
        IfFailThrow(m_pEECompileInfo->SetVerboseLevel (CORCOMPILE_VERBOSE));

    IfFailThrow(m_pEECompileInfo->Startup(fForceDebug, fForceProfile, fForceInstrument));

    m_pEEJitInfo = GetZapJitInfo();

    //
    // Get JIT interface
    //

#ifdef FEATURE_MERGE_JIT_AND_ENGINE
    jitStartup(m_pEECompileInfo->GetJitHost());
    m_pJitCompiler = getJit();

    if (m_pJitCompiler == NULL)
    {
        Error(W("Unable to initialize Jit Compiler\r\n"));
        ThrowLastError();
    }
#else

    CorCompileRuntimeDlls ngenDllId;

    ngenDllId = CROSSGEN_COMPILER_INFO;

    LPCWSTR pwzJitName = CorCompileGetRuntimeDllName(ngenDllId);
    LoadAndInitializeJITForNgen(pwzJitName, &m_hJitLib, &m_pJitCompiler);
    
#endif // FEATURE_MERGE_JIT_AND_ENGINE

#ifdef ALLOW_SXS_JIT_NGEN

    // Do not load altjit unless COMPlus_AltJitNgen is set.
    LPWSTR altJit;
    HRESULT hr = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AltJitNgen, &altJit);
    if (FAILED(hr))
    {
        Error(W("Unable to load alternative Jit Compiler\r\n"));
        ThrowHR(hr);
    }

    m_hAltJITCompiler = NULL;
    m_alternateJit = NULL;

    if (altJit != NULL)
    {
        // Allow a second jit to be loaded into the system.
        // 
        LPCWSTR altName;
        hr = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_AltJitName, (LPWSTR*)&altName);
        if (FAILED(hr))
        {
            Error(W("Unable to load alternative Jit Compiler\r\n"));
            ThrowHR(hr);
        }

        if (altName == NULL)
        {
            altName = MAKEDLLNAME_W(W("protojit"));
        }

        LoadAndInitializeJITForNgen(altName, &m_hAltJITCompiler, &m_alternateJit);
    }
#endif // ALLOW_SXS_JIT_NGEN
}

Zapper::~Zapper()
{
    DestroyDomain();

    if (m_pMetaDataDispenser != NULL)
        m_pMetaDataDispenser->Release();

    if (m_fFreeZapperOptions)
    {
        if (m_pOpt != NULL)
            delete m_pOpt;
        m_pOpt = NULL;
    }

    if (m_pEECompileInfo != NULL)
    {
#ifndef FEATURE_MERGE_JIT_AND_ENGINE
#ifdef _TARGET_AMD64_
        if (m_hJitLegacy != NULL)
        {
            _ASSERTE(m_hJitLib != NULL);
            _ASSERTE(m_pJitCompiler != NULL);

            // We're unloading the fallback JIT dll, so clear the fallback shim. We do this even though
            // we'll unload the main JIT below, in case there are other places that have loaded the main JIT
            // but not the fallback JIT (note that LoadLibrary reference counts the number of loads that have been done).
            m_pJitCompiler->setRealJit(nullptr);

            FreeLibrary(m_hJitLegacy);
        }
#endif
        if (m_hJitLib != NULL)
            FreeLibrary(m_hJitLib);
#endif

#ifdef ALLOW_SXS_JIT_NGEN
        if (m_hAltJITCompiler != NULL)
            FreeLibrary(m_hAltJITCompiler);
#endif

#ifdef FEATURE_COMINTEROP
        if (!m_fromDllHost)
        {
            CoUninitialize();
        }
#endif // FEATURE_COMINTEROP
    }
}

void Zapper::DestroyDomain()
{
    CleanupAssembly();

    //
    // Shut down JIT compiler.
    //

    if (m_pJitCompiler != NULL)
    {
        m_pJitCompiler->ProcessShutdownWork(NULL);
    }

    //
    // Get rid of domain.
    //

    if (m_pDomain != NULL)
    {
        m_pEECompileInfo->DestroyDomain(m_pDomain);
        m_pDomain = NULL;
    }
}

void Zapper::CleanupAssembly()
{
    if (m_pAssemblyEmit != NULL)
    {
        m_pAssemblyEmit->Release();
        m_pAssemblyEmit = NULL;
    }

    if (m_pAssemblyImport != NULL)
    {
        m_pAssemblyImport->Release();
        m_pAssemblyImport = NULL;
    }
}


//**********************************************************************
// Copy of vm\i386\cgenCpu.h
// @TODO : clean this up. There has to be a way of sharing this code

//**********************************************************************
// To be used with GetSpecificCpuInfo()
#ifdef _TARGET_X86_

#define CPU_X86_FAMILY(cpuType)     (((cpuType) & 0x0F00) >> 8)
#define CPU_X86_MODEL(cpuType)      (((cpuType) & 0x00F0) >> 4)
// Stepping is masked out by GetSpecificCpuInfo()
// #define CPU_X86_STEPPING(cpuType)   (((cpuType) & 0x000F)     )

#define CPU_X86_USE_CMOV(cpuFeat)   ((cpuFeat & 0x00008001) == 0x00008001)
#define CPU_X86_USE_SSE2(cpuFeat)   ((cpuFeat & 0x04000000) == 0x04000000)

// Values for CPU_X86_FAMILY(cpuType)
#define CPU_X86_486                 4
#define CPU_X86_PENTIUM             5
#define CPU_X86_PENTIUM_PRO         6
#define CPU_X86_PENTIUM_4           0xF

// Values for CPU_X86_MODEL(cpuType) for CPU_X86_PENTIUM_PRO
#define CPU_X86_MODEL_PENTIUM_PRO_BANIAS    9 // Pentium M (Mobile PPro with P4 feautres)

#endif


BOOL Zapper::IsAssembly(LPCWSTR path)
{
    HandleHolder hFile = WszCreateFile(path,
                                     GENERIC_READ,
                                     FILE_SHARE_READ | FILE_SHARE_WRITE,
                                     NULL, OPEN_EXISTING, 0, NULL);

    if (hFile == INVALID_HANDLE_VALUE)
        return FALSE;

    IMAGE_DOS_HEADER dos;

    DWORD count;
    if (!ReadFile(hFile, &dos, sizeof(dos), &count, NULL)
        || count != sizeof(dos))
        return FALSE;

    if (dos.e_magic != IMAGE_DOS_SIGNATURE || dos.e_lfanew == 0)
        return FALSE;

    if( SetFilePointer(hFile, dos.e_lfanew, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
         return FALSE;

    IMAGE_NT_HEADERS nt;
    if (!ReadFile(hFile, &nt, sizeof(nt), &count, NULL)
        || count != sizeof(nt))
        return FALSE;

    if (nt.Signature != IMAGE_NT_SIGNATURE)
        return FALSE;

    IMAGE_DATA_DIRECTORY* pDataDirectory = NULL;

    // We accept pe32 or pe64 file formats

    if (nt.OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
    {
        if (nt.FileHeader.SizeOfOptionalHeader != sizeof(IMAGE_OPTIONAL_HEADER32))
            return FALSE;

        IMAGE_NT_HEADERS32* pNTHeader32 = (IMAGE_NT_HEADERS32*) &nt.OptionalHeader;
        pDataDirectory = &pNTHeader32->OptionalHeader.DataDirectory[0];
    }
    else if (nt.OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC)
    {
        if (nt.FileHeader.SizeOfOptionalHeader != sizeof(IMAGE_OPTIONAL_HEADER64))
            return FALSE;

        IMAGE_NT_HEADERS64* pNTHeader64 = (IMAGE_NT_HEADERS64*) &nt.OptionalHeader;
        pDataDirectory = &pNTHeader64->OptionalHeader.DataDirectory[0];
    }
    else
        return FALSE;

    if ((pDataDirectory[IMAGE_DIRECTORY_ENTRY_COMHEADER].VirtualAddress == 0) ||
        (pDataDirectory[IMAGE_DIRECTORY_ENTRY_COMHEADER].Size < sizeof(IMAGE_COR20_HEADER)))
    {
        return FALSE;
    }

    return TRUE;
}

void Zapper::SetContextInfo(LPCWSTR assemblyName)
{
    // A special case:  If we're compiling mscorlib, ignore m_exeName and don't set any context. 
    // There can only be one mscorlib in the runtime, independent of any context.  If we don't
    // check for mscorlib, and isExe == true, then CompilationDomain::SetContextInfo will call
    // into mscorlib and cause the resulting mscorlib.ni.dll to be slightly different (checked
    // build only).
    if (assemblyName != NULL && _wcsnicmp(assemblyName, CoreLibName_W, CoreLibNameLen) == 0 && (wcslen(assemblyName) == CoreLibNameLen || assemblyName[CoreLibNameLen] == W(',')))
    {
        return;
    }

    // Determine whether the root is an exe or a dll

    StackSString s(m_exeName);

    //These locals seem necessary for gcc do resolve the overload correctly
    SString literalExe(SString::Literal, W(".exe"));
    SString literalDll(SString::Literal, W(".dll"));
    BOOL isExe = (s.GetCount() > 4) && s.MatchCaseInsensitive(s.End() - 4, literalExe);
    BOOL isDll = (s.GetCount() > 4) && s.MatchCaseInsensitive(s.End() - 4, literalDll);

    DWORD attributes = WszGetFileAttributes(m_exeName);
    if (attributes != INVALID_FILE_ATTRIBUTES)
    {
        if (isExe)
        {
            // If it's an exe, set it as the config
            m_pDomain->SetContextInfo(m_exeName, TRUE);
        }
        else if ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
        {
            // If it's a directory, set it as the default app base
            m_pDomain->SetContextInfo(m_exeName, FALSE);
        }
        else if (isDll)
        {
            // If it's a dll, then set a default app base of the directory containing the dll
            SString::Iterator i = s.End();
            if (s.FindBack(i, W("\\")))
            {
                s.Truncate(i);
                m_pDomain->SetContextInfo(s.GetUnicode(), FALSE);
            }
        }
    }
}

void Zapper::CreateDependenciesLookupDomainInCurrentDomain()
{
    SetContextInfo();
}


void ZapperSetBindingPaths(ICorCompilationDomain *pDomain, SString &trustedPlatformAssemblies, SString &platformResourceRoots, SString &appPaths, SString &appNiPaths);

void Zapper::CreateCompilationDomain()
{

    BOOL fForceDebug = FALSE;
    if (!m_pOpt->m_autodebug)
        fForceDebug = m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);

    BOOL fForceProfile = m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);
    BOOL fForceInstrument = m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR);

    InitEE(fForceDebug, fForceProfile, fForceInstrument);

    // Make a compilation domain for the assembly.  Note that this will
    // collect the assembly dependencies for use in the version info, as
    // well as isolating the compilation code.

    IfFailThrow(m_pEECompileInfo->CreateDomain(&m_pDomain,
                                               CreateAssemblyEmitter(),
                                               fForceDebug,
                                               fForceProfile,
                                               fForceInstrument));

#ifdef CROSSGEN_COMPILE
    IfFailThrow(m_pDomain->SetPlatformWinmdPaths(m_platformWinmdPaths));
#endif

    // we support only TPA binding on CoreCLR

    if (!m_trustedPlatformAssemblies.IsEmpty())
    {
        // TPA-based binding
        // Add the trusted paths and apppath to the binding list
        ZapperSetBindingPaths(m_pDomain, m_trustedPlatformAssemblies, m_platformResourceRoots, m_appPaths, m_appNiPaths);
    }
}

void Zapper::CreateDependenciesLookupDomain()
{
    CreateCompilationDomain();
    CreateDependenciesLookupDomainInCurrentDomain();
}

void Zapper::CreatePdb(BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath)
{
#ifdef CROSSGEN_COMPILE
    CreateCompilationDomain();
#endif

    EX_TRY
    {
        CORINFO_ASSEMBLY_HANDLE hAssembly = NULL;

        IfFailThrow(m_pEECompileInfo->LoadAssemblyByPath(
            pAssemblyPathOrName, 

            // fExplicitBindToNativeImage: On the phone, a path to the NI is specified
            // explicitly (even with .ni. in the name). All other callers specify a path to
            // the IL, and the NI is inferred, so this is normally FALSE in those other
            // cases.
            TRUE, 

            &hAssembly));


        // Ensure all modules belonging to this assembly get loaded.  The CreatePdb() call
        // below will iterate through them via the ModuleIterator to generate PDB files for each
        // one, and the ModuleIterator assumes they've been loaded.
        IMDInternalImport * pMDImport = m_pEECompileInfo->GetAssemblyMetaDataImport(hAssembly);
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumAllInit(mdtFile);
        mdFile tkFile;
        while (pMDImport->EnumNext(&hEnum, &tkFile))
        {
            LPCSTR szName;
            DWORD flags;
            IfFailThrow(pMDImport->GetFileProps(tkFile, &szName, NULL, NULL, &flags));

            if (!IsFfContainsMetaData(flags))
                continue;

            CORINFO_MODULE_HANDLE hModule;
            IfFailThrow(m_pEECompileInfo->LoadAssemblyModule(hAssembly,
                tkFile, &hModule));
        }

        LPCWSTR pDiasymreaderPath = nullptr;
#if !defined(NO_NGENPDB)
        if (m_DiasymreaderPath.GetCount() > 0)
        {
            pDiasymreaderPath = m_DiasymreaderPath.GetUnicode();
        }
#endif //!defined(NO_NGENPDB)

        IfFailThrow(::CreatePdb(hAssembly, pNativeImagePath, pPdbPath, pdbLines, pManagedPdbSearchPath, pDiasymreaderPath));
    }
    EX_CATCH
    {
        // Print the error message

        Error(W("Error generating PDB for '%s': "), pNativeImagePath);
        PrintErrorMessage(CORZAP_LOGLEVEL_ERROR, GET_EXCEPTION());
        Error(W("\n"));
        EX_RETHROW;
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

void Zapper::ComputeDependencies(LPCWSTR pAssemblyName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
}


//
// Compile a module by name
//

HRESULT Zapper::Compile(LPCWSTR string, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    HRESULT hr = S_OK;

    bool fMscorlib = false;
    LPCWSTR fileName = PathFindFileName(string);
    if (fileName != NULL && SString::_wcsicmp(fileName, g_pwBaseLibrary) == 0)
    {
        fMscorlib = true;
    }


    if (fMscorlib)
    {
        //
        // Disallow use of native image to force a new native image generation for mscorlib
        //
        g_fAllowNativeImages = false;
    }

    // the errors in CreateCompilationDomain are fatal - propogate them up
    CreateCompilationDomain();

    EX_TRY
    {
        CompileInCurrentDomain(string, pNativeImageSig);
    }
    EX_CATCH
    {
        // Print the error message

        Error(W("Error compiling %s: "), string);
        PrintErrorMessage(CORZAP_LOGLEVEL_ERROR, GET_EXCEPTION());
        Error(W("\n"));

        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    // the errors in DestroyDomain are fatal - propogate them up
    DestroyDomain();

    return hr;
}

void Zapper::DontUseProfileData()
{
    // Call this before calling Compile()

    m_pOpt->m_ignoreProfileData = true;
}

bool Zapper::HasProfileData()
{
    // Only valid after calling Compile()
    return m_pOpt->m_fHasAnyProfileData;
}

// Helper function for Zapper::Compile(LPCWSTR string)
//

void Zapper::CompileInCurrentDomain(__in LPCWSTR string, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    STATIC_CONTRACT_ENTRY_POINT;
    
    BEGIN_ENTRYPOINT_VOIDRET;



    //
    // Load the assembly.
    //
    // "string" may be a path or assembly display name.
    // To decide, we see if it is the name of a valid file.
    //

    _ASSERTE(m_hAssembly == NULL);

    //without fusion, this has to be a file name
    {
        IfFailThrow(m_pEECompileInfo->LoadAssemblyByPath(string, FALSE /* fExplicitBindToNativeImage */, &m_hAssembly));
    }


    //
    // Compile the assembly
    //
    CompileAssembly(pNativeImageSig);

    goto Exit; // Avoid warning about unreferenced label

Exit:
    END_ENTRYPOINT_VOIDRET;

    return;
}


//------------------------------------------------------------------------------

// Is this is a valid file-system file name?

bool StringHasLegalFileNameChars(LPCWSTR assemblyName)
{
    if (wcschr(assemblyName, ':') || wcschr(assemblyName, '/') || wcschr(assemblyName, '\\') ||
        wcschr(assemblyName, '*') || wcschr(assemblyName, '?') || wcschr(assemblyName, '"') ||
        wcschr(assemblyName, '<') || wcschr(assemblyName, '>') || wcschr(assemblyName, '|'))
        return false;

    return true;
}

IMetaDataAssemblyEmit * Zapper::CreateAssemblyEmitter()
{
    _ASSERTE(!m_pAssemblyEmit);

    //
    // Make a manifest emitter for the zapped assembly.
    //

    // Hardwire the metadata version to be the current runtime version so that the ngen image
    // does not change when the directory runtime is installed in different directory (e.g. v2.0.x86chk vs. v2.0.80826).
    BSTRHolder strVersion(SysAllocString(W("v")VER_PRODUCTVERSION_NO_QFE_STR_L));
    VARIANT versionOption;
    V_VT(&versionOption) = VT_BSTR;
    V_BSTR(&versionOption) = strVersion;
    IfFailThrow(m_pMetaDataDispenser->SetOption(MetaDataRuntimeVersion, &versionOption));

    IfFailThrow(m_pMetaDataDispenser->DefineScope(CLSID_CorMetaDataRuntime,
                                                  0, IID_IMetaDataAssemblyEmit,
                                                  (IUnknown **) &m_pAssemblyEmit));

    return m_pAssemblyEmit;
}

void Zapper::InitializeCompilerFlags(CORCOMPILE_VERSION_INFO * pVersionInfo)
{
    m_pOpt->m_compilerFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);
    m_pOpt->m_compilerFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    m_pOpt->m_compilerFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);
    m_pOpt->m_compilerFlags.Clear(CORJIT_FLAGS::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE);

    // We track debug info all the time in the ngen image
    m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_DEBUGGING)
    {
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROFILING)
    {
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE);
        m_pOpt->m_ngenProfileImage = true;
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROF_INSTRUMENTING)
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR);

    // Set CORJIT_FLAG_MIN_OPT only if COMPlus_JitMinOpts == 1
    static ConfigDWORD g_jitMinOpts;
    if (g_jitMinOpts.val_DontUse_(CLRConfig::UNSUPPORTED_JITMinOpts, 0) == 1)
    {
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT);
    }

#if defined(_TARGET_X86_)

    // @TODO: This is a copy of SetCpuInfo() in vm\codeman.cpp. Unify the implementaion

    switch (CPU_X86_FAMILY(pVersionInfo->cpuInfo.dwCPUType))
    {
        case CPU_X86_PENTIUM_4:
            m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_TARGET_P4);
            break;

        default:
            break;
    }

    if (CPU_X86_USE_CMOV(pVersionInfo->cpuInfo.dwFeatures))
    {
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_CMOV);
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_FCOMI);
    }

    // .NET Core requires SSE2.
    m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE2);

#endif // _TARGET_X86_

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    // If we're compiling CoreLib, allow RyuJIT to generate SIMD code so that we can expand some
    // of the hardware intrinsics.
    if (m_pEECompileInfo->GetAssemblyModule(m_hAssembly) == m_pEECompileInfo->GetLoaderModuleForMscorlib())
    {
        m_pOpt->m_compilerFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_FEATURE_SIMD);
    }
#endif // defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

    if (   m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO)
        && m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE)
        && m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE))
    {
        //
        // We've decided not to support debugging + optimizations disabled + profiling to
        // cut down on the testing matrix, under the assertion that the combination isn't
        // particularly common or useful.  (It's still supported in normal jit mode though.)
        //

        Error(W("The CLR doesn't support precompiled code when using both debugging and profiling")
                W(" instrumentation.\n"));
        ThrowHR(E_NOTIMPL);
    }
}

void Zapper::DefineOutputAssembly(SString& strAssemblyName, ULONG * pHashAlgId)
{
    mdAssembly tkAssembly;
    IfFailThrow(m_pAssemblyImport->GetAssemblyFromScope(&tkAssembly));

    AssemblyMetaDataInternal internalMetadata;

    const void *pbPublicKey;
    ULONG cbPublicKey;
    ULONG hashAlgId;
    LPCSTR szAssemblyName;
    DWORD flags;

    IfFailThrow(m_pAssemblyImport->GetAssemblyProps(tkAssembly, &pbPublicKey, &cbPublicKey,
         &hashAlgId, &szAssemblyName, &internalMetadata, &flags));

    strAssemblyName.SetUTF8(szAssemblyName);

    ASSEMBLYMETADATA metadata;
    SString strLocale;

    metadata.usMajorVersion = internalMetadata.usMajorVersion;
    metadata.usMinorVersion = internalMetadata.usMinorVersion;
    metadata.usBuildNumber = internalMetadata.usBuildNumber;
    metadata.usRevisionNumber = internalMetadata.usRevisionNumber;
    if (internalMetadata.szLocale)
    {
        strLocale.SetUTF8(internalMetadata.szLocale);
        metadata.szLocale = (LPWSTR)strLocale.GetUnicode();
        metadata.cbLocale = strLocale.GetCount() + 1;
    }
    else
    {
        metadata.szLocale = NULL;
        metadata.cbLocale = 0;
    }
    metadata.rProcessor = internalMetadata.rProcessor;
    metadata.ulProcessor = internalMetadata.ulProcessor;
    metadata.rOS = internalMetadata.rOS;
    metadata.ulOS = internalMetadata.ulOS;

    LPCWSTR wszAssemblyName = strAssemblyName.GetUnicode();

    // If assembly name (and hence fileName) has invalid characters,
    // GenerateFile() will fail later on.
    // VerifyBindingString is a Runtime requirement, but StringHasLegalFileNameChars
    // is a ngen restriction.

    if (!StringHasLegalFileNameChars(wszAssemblyName))
    {
        Error(W("Error: Assembly name \"%s\" contains illegal (unsupported) file name characters.\n"), wszAssemblyName);
        ThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_NAME));
    }

#ifndef PLATFORM_UNIX
    //
    // We always need a hash since our assembly module is separate from the manifest.
    // Use MD5 by default.
    //

    if (hashAlgId == 0)
        hashAlgId = CALG_MD5;
#endif

    mdAssembly tkEmitAssembly;
    IfFailThrow(m_pAssemblyEmit->DefineAssembly(pbPublicKey, cbPublicKey, hashAlgId,
                                              wszAssemblyName, &metadata,
                                              flags, &tkEmitAssembly));

    *pHashAlgId = hashAlgId;
}

//
// This is the main worker function (at the assembly level).
//
// Zapper::CompileInCurrentDomain() ends up calling this function.

void Zapper::CompileAssembly(CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    _ASSERTE(m_pDomain != NULL); // This must be set up by this time.
    _ASSERTE(m_hAssembly != NULL);

    CORINFO_MODULE_HANDLE hAssemblyModule = m_pEECompileInfo->GetAssemblyModule(m_hAssembly);

    CORCOMPILE_VERSION_INFO versionInfo;

    IfFailThrow(m_pEECompileInfo->GetAssemblyVersionInfo(m_hAssembly,
                                                         &versionInfo));

    InitializeCompilerFlags(&versionInfo);

    //
    // Set up the output path.
    //

    //
    // If we don't have fusion, we just create the file right at the target.  No need to do an install.
    //
    {
        SString strAssemblyPath = GetOutputFileName();

        if (strAssemblyPath.IsEmpty())
        {
            m_pEECompileInfo->GetModuleFileName(hAssemblyModule, strAssemblyPath);
        }

        const WCHAR * assemblyPath = strAssemblyPath.GetUnicode();
        const WCHAR * pathend = wcsrchr( assemblyPath, DIRECTORY_SEPARATOR_CHAR_W );
        if( pathend )
        {
            m_outputPath.Set(assemblyPath, COUNT_T(pathend - assemblyPath));
        }
        else
        {
            m_outputPath.Set(W(".") DIRECTORY_SEPARATOR_STR_W);
        }
    }

    //
    // Get the manifest metadata.
    //

    m_pAssemblyImport = m_pEECompileInfo->GetAssemblyMetaDataImport(m_hAssembly);

    //
    // Copy the relevant assembly info to the zap manifest
    //

    SString strAssemblyName;
    ULONG hashAlgId;
    DefineOutputAssembly(strAssemblyName, &hashAlgId);

    SString strNativeImagePath;
    SString strNativeImageTempPath;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    StackSArray<HANDLE> hFiles;

    {
        //
        // Compile the manifest module (if manifest is not stand-alone).
        //

        NewHolder<ZapImage> pAssemblyModule;

        pAssemblyModule = CompileModule(hAssemblyModule, m_pAssemblyEmit);

        //
        // The loader (currently) requires all modules of an ngen-ed multi-module
        // assembly to be ngen-ed.
        //


        //
        // Record the version info
        //

        pAssemblyModule->SetVersionInfo(&versionInfo);

        //
        // Record Dependencies
        //

        if (!IsReadyToRunCompilation())
        {
            CORCOMPILE_DEPENDENCY *pDependencies;
            DWORD cDependencies;
            IfFailThrow(m_pDomain->GetDependencies(&pDependencies, &cDependencies));

            pAssemblyModule->SetDependencies(pDependencies, cDependencies);
        }

        // Write the main assembly module
        strNativeImagePath = GetOutputFileName();

        if (strNativeImagePath.IsEmpty())
        {
            strNativeImagePath.Set(m_outputPath, SL(DIRECTORY_SEPARATOR_STR_W), strAssemblyName, 
                pAssemblyModule->m_ModuleDecoder.IsDll() ? SL(W(".ni.dll")) : SL(W(".ni.exe")));
        }

        pAssemblyModule->SetPdbFileName(SString(strAssemblyName, SL(W(".ni.pdb"))));

        strNativeImageTempPath = strNativeImagePath;
        strNativeImageTempPath.Append(W(".tmp"));

        hFile = pAssemblyModule->SaveImage(strNativeImageTempPath.GetUnicode(), strNativeImagePath.GetUnicode(), pNativeImageSig);
    }

    // Throw away the assembly if we have hit fatal error during compilation
    if (FAILED(g_hrFatalError))
        ThrowHR(g_hrFatalError);


    // Close the file
    CloseHandle(hFile);
    for (SArray<HANDLE>::Iterator i = hFiles.Begin(); i != hFiles.End(); ++i)
    {
        CloseHandle(*i);
    }

    if (!WszMoveFileEx(strNativeImageTempPath.GetUnicode(), strNativeImagePath.GetUnicode(), MOVEFILE_REPLACE_EXISTING))
    {
        ThrowLastError();
    }

    if (!m_pOpt->m_silent)
    {
        GetSvcLogger()->Printf(W("Native image %s generated successfully.\n"), strNativeImagePath.GetUnicode());
    }

}


ZapImage * Zapper::CompileModule(CORINFO_MODULE_HANDLE hModule,
                                 IMetaDataAssemblyEmit *pAssemblyEmit)
{
    NewHolder<ZapImage> module;

    BYTE * pMemory = new BYTE[sizeof(ZapImage)];
    ZeroMemory(pMemory, sizeof(ZapImage));

    module = new (pMemory) ZapImage(this);

    // Finish initializing the ZapImage object
    // any calls that could throw an exception are done in ZapImage::Initialize()
    // 
    module->ZapWriter::Initialize();

    //
    // Set target
    //

    IfFailThrow(m_pEECompileInfo->SetCompilationTarget(m_hAssembly, hModule));

    //
    // Open input file
    //

    Info(W("Opening input file\n"));

    module->Open(hModule, pAssemblyEmit);

    //
    //  input module.
    //

    Info(W("Preloading input file %s\n"), module->m_pModuleFileName);

    module->Preload();

    //
    // Compile input module
    //

    Info(W("Compiling input file %s\n"), module->m_pModuleFileName);

    module->Compile();

    if (!IsReadyToRunCompilation())
    {
        //
        // Link preloaded module.
        //

        Info(W("Linking preloaded input file %s\n"), module->m_pModuleFileName);

        module->LinkPreload();
    }
    return module.Extract();
}


void Zapper::Success(LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);

    Print(CORZAP_LOGLEVEL_SUCCESS, format, args);

    va_end(args);
}

void Zapper::Error(LPCWSTR format, ...)
{
    // Preserve last error so we can still do ThrowLastError() after printing the message
    DWORD err = GetLastError();

    va_list args;
    va_start(args, format);

    Print(CORZAP_LOGLEVEL_ERROR, format, args);

    va_end(args);

    SetLastError(err);
}

void Zapper::Warning(LPCWSTR format, ...)
{
    // Preserve last error so we can still do ThrowLastError() after printing the message
    DWORD err = GetLastError();

    va_list args;
    va_start(args, format);

    Print(CORZAP_LOGLEVEL_WARNING, format, args);

    va_end(args);

    SetLastError(err);
}

void Zapper::Info(LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);

    Print(CORZAP_LOGLEVEL_INFO, format, args);

    va_end(args);
}

void Zapper::Print(CorZapLogLevel level, LPCWSTR format, ...)
{
    va_list args;
    va_start(args, format);

    Print(level, format, args);

    va_end(args);
}

void Zapper::Print(CorZapLogLevel level, LPCWSTR format, va_list args)
{
    const int BUFF_SIZE = 1024;
    WCHAR output[BUFF_SIZE];

    switch (level)
    {
    case CORZAP_LOGLEVEL_INFO:
        if (!m_pOpt->m_verbose)
            return;
        break;

    case CORZAP_LOGLEVEL_ERROR:
    case CORZAP_LOGLEVEL_WARNING:
        break;

    case CORZAP_LOGLEVEL_SUCCESS:
        if (m_pOpt->m_silent)
            return;
        break;
    }

    CorSvcLogLevel logLevel = (CorSvcLogLevel)0;
    switch (level)
    {
    case CORZAP_LOGLEVEL_INFO:
        logLevel = LogLevel_Info;
        break;

    case CORZAP_LOGLEVEL_ERROR:
        logLevel = LogLevel_Error;
        break;

    case CORZAP_LOGLEVEL_WARNING:
        logLevel = LogLevel_Warning;
        break;

    case CORZAP_LOGLEVEL_SUCCESS:
        logLevel = LogLevel_Success;
        break;
    }

    _vsnwprintf_s(output, BUFF_SIZE, BUFF_SIZE - 1, format, args);
    output[BUFF_SIZE-1] = 0;

    GetSvcLogger()->Log(output, logLevel);
}

void Zapper::PrintErrorMessage(CorZapLogLevel level, Exception *ex)
{
    StackSString message;
    ex->GetMessage(message);
    Print(level, W("%s"), message.GetUnicode());
}

void Zapper::PrintErrorMessage(CorZapLogLevel level, HRESULT hr)
{
    StackSString message;
    GetHRMsg(hr, message);
    Print(level, W("%s"), message.GetUnicode());
}

#if !defined(FEATURE_MERGE_JIT_AND_ENGINE)
void Zapper::SetCLRJITPath(LPCWSTR pwszCLRJITPath)
{
    m_CLRJITPath.Set(pwszCLRJITPath);
}

void Zapper::SetDontLoadJit()
{
    m_fDontLoadJit = true;
}
#endif // !defined(FEATURE_MERGE_JIT_AND_ENGINE)

#if !defined(NO_NGENPDB)
void Zapper::SetDiasymreaderPath(LPCWSTR pwzDiasymreaderPath)
{
    m_DiasymreaderPath.Set(pwzDiasymreaderPath);
}
#endif // !defined(NO_NGENPDB)


void Zapper::SetPlatformAssembliesPaths(LPCWSTR pwzPlatformAssembliesPaths)
{
    m_platformAssembliesPaths.Set(pwzPlatformAssembliesPaths);
}

void Zapper::SetTrustedPlatformAssemblies(LPCWSTR pwzTrustedPlatformAssemblies)
{
    m_trustedPlatformAssemblies.Set(pwzTrustedPlatformAssemblies);
}

void Zapper::SetPlatformResourceRoots(LPCWSTR pwzPlatformResourceRoots)
{
    m_platformResourceRoots.Set(pwzPlatformResourceRoots);
}

void Zapper::SetAppPaths(LPCWSTR pwzAppPaths)
{
    m_appPaths.Set(pwzAppPaths);
}

void Zapper::SetAppNiPaths(LPCWSTR pwzAppNiPaths)
{
    m_appNiPaths.Set(pwzAppNiPaths);
}

void Zapper::SetPlatformWinmdPaths(LPCWSTR pwzPlatformWinmdPaths)
{
    m_platformWinmdPaths.Set(pwzPlatformWinmdPaths);
}


void Zapper::SetOutputFilename(LPCWSTR pwzOutputFilename)
{
    m_outputFilename.Set(pwzOutputFilename);
}


SString Zapper::GetOutputFileName()
{
    return m_outputFilename;
}
