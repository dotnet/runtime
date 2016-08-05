// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"

#ifdef FEATURE_FUSION
#include "binderngen.h"
#endif

#ifndef FEATURE_MERGE_JIT_AND_ENGINE
#include "metahost.h"
#endif

#if defined(FEATURE_APPX) && !defined(FEATURE_CORECLR)
#include "AppXUtil.h"
#include "AssemblyUsageLogManager.h"
#endif

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
#include "coregen.h"
#endif

#include "clr/fs/dir.h"
#ifdef FEATURE_FUSION
#include "ngenparser.inl"
#endif

/* --------------------------------------------------------------------------- *
 * Error Macros
 * --------------------------------------------------------------------------- */

#ifdef ALLOW_LOCAL_WORKER
namespace
{

bool g_eeInitialized = false;
//these track the initialization state of the runtime in local worker mode.  If
//the flags change during subsequent calls to InitEE, then fail.
BOOL g_fForceDebug, g_fForceProfile, g_fForceInstrument;

}
#endif


#ifdef FEATURE_FUSION
extern "C" HRESULT STDMETHODCALLTYPE InitializeFusion();
#endif

#pragma warning(push)
#pragma warning(disable: 4995)
#include "shlwapi.h"
#pragma warning(pop)

extern const WCHAR g_pwBaseLibrary[];
extern bool g_fAllowNativeImages;
#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
bool g_fNGenMissingDependenciesOk;
#endif
bool g_fNGenWinMDResilient;
#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
extern int g_ningenState;
#endif

#ifdef FEATURE_READYTORUN_COMPILER
bool g_fReadyToRunCompilation;
#endif

#ifdef FEATURE_CORECLR
static bool s_fNGenNoMetaData;
#endif

// Event logging helper
void Zapper::ReportEventNGEN(WORD wType, DWORD dwEventID, LPCWSTR format, ...)
{
    SString s;
    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    SString message;
    message.Printf(W(".NET Runtime Optimization Service (%s) - %s"), VER_FILEVERSION_STR_L, s.GetUnicode());

    // Note: We are using the same event log source as the ngen service. This may become problem 
    // if we ever want to split the ngen service from the rest of the .NET Framework.
    ClrReportEvent(W(".NET Runtime Optimization Service"),
        wType,                  // event type 
        0,                      // category zero
        dwEventID,              // event identifier
        NULL,                   // no user security identifier
        message.GetUnicode());

    // Output the message to the logger as well.
    if (wType == EVENTLOG_WARNING_TYPE)
        Warning(W("%s\n"), s.GetUnicode());
}

#ifdef FEATURE_FUSION
static HRESULT GetAssemblyName(
    ICorCompileInfo * pCCI,
    CORINFO_ASSEMBLY_HANDLE hAssembly,
    SString & str,
    DWORD dwFlags)
{
    DWORD dwSize = 0;
    LPWSTR buffer = NULL;
    COUNT_T allocation = str.GetUnicodeAllocation();
    if (allocation > 0)
    {
        // pass in the buffer if we got one
        dwSize = allocation + 1;
        buffer = str.OpenUnicodeBuffer(allocation);
    }
    HRESULT hr = pCCI->GetAssemblyName(hAssembly, dwFlags, buffer, &dwSize);
    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        if (buffer != NULL) 
            str.CloseBuffer(0);
        buffer = str.OpenUnicodeBuffer(dwSize-1);
        hr = pCCI->GetAssemblyName(hAssembly, dwFlags, buffer, &dwSize);
    }
    if (buffer != NULL)
    {
        str.CloseBuffer((SUCCEEDED(hr) && dwSize >= 1) ? (dwSize-1) : 0);
    }

    return hr;
}
#endif // FEATURE_FUSION

/* --------------------------------------------------------------------------- *
 * Private fusion entry points
 * --------------------------------------------------------------------------- */

/* --------------------------------------------------------------------------- *
 * Public entry points for ngen
 * --------------------------------------------------------------------------- */
// For side by side issues, it's best to use the exported API calls to generate a
// Zapper Object instead of creating one on your own.

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)

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
        ngo.fSilent = false;
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

        ngo.fVerbose = false;
        ngo.uStats = false;

        ngo.fNgenLastRetry = false;

#ifdef FEATURE_CORECLR
        s_fNGenNoMetaData = (dwFlags & NGENWORKER_FLAGS_NO_METADATA) != 0;
#endif

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

#if defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        if (pwszCLRJITPath != nullptr)
            zap->SetCLRJITPath(pwszCLRJITPath);
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)

        zap->SetForceFullTrust(!!(dwFlags & NGENWORKER_FLAGS_FULLTRUSTDOMAIN));

        g_fNGenMissingDependenciesOk = !!(dwFlags & NGENWORKER_FLAGS_MISSINGDEPENDENCIESOK);

#ifdef FEATURE_WINMD_RESILIENT
        g_fNGenWinMDResilient = !!(dwFlags & NGENWORKER_FLAGS_WINMD_RESILIENT);
#endif

#ifdef FEATURE_READYTORUN_COMPILER
        g_fReadyToRunCompilation = !!(dwFlags & NGENWORKER_FLAGS_READYTORUN);
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
        NGenOptions ngo = {0};
        ngo.dwSize = sizeof(NGenOptions);

        zap = Zapper::NewZapper(&ngo);

#if defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
        zap->SetDontLoadJit();
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)

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

#if defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)
        if (pwzDiasymreaderPath != nullptr)
            zap->SetDiasymreaderPath(pwzDiasymreaderPath);
#endif // defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)

        // Avoid unnecessary security failures, since permissions are irrelevant when
        // generating NGEN PDBs
        zap->SetForceFullTrust(true);

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

#else // FEATURE_CORECLR || CROSSGEN_COMPILE

STDAPI LegacyNGenCreateZapper(HANDLE* hZapper, NGenOptions* opt)
{
    if (hZapper == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;
    Zapper* zap = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;


    EX_TRY
    {
        zap = Zapper::NewZapper(opt);
    }
    EX_CATCH_HRESULT(hr);

    END_ENTRYPOINT_NOTHROW;

    IfFailRet(hr);

    if (zap == NULL)
        return E_OUTOFMEMORY;

    zap->SetLegacyMode();

    *hZapper = (HANDLE)zap;

    return S_OK;
}// NGenCreateZapper

STDAPI LegacyNGenFreeZapper(HANDLE hZapper)
{
    if (hZapper == NULL || hZapper == INVALID_HANDLE_VALUE)
        return E_HANDLE;

    BEGIN_ENTRYPOINT_NOTHROW;

    Zapper *zapper = (Zapper*)hZapper;
    delete zapper;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}// NGenFreeZapper

#ifdef FEATURE_FUSION
STDAPI LegacyNGenTryEnumerateFusionCache(HANDLE hZapper, LPCWSTR assemblyName, bool fPrint, bool fDelete)
{

    HRESULT hr = S_OK;
    if (hZapper == NULL || hZapper == INVALID_HANDLE_VALUE)
        return E_HANDLE;
    
    BEGIN_ENTRYPOINT_NOTHROW;

    Zapper *zapper = (Zapper*)hZapper;
    hr =  zapper->TryEnumerateFusionCache(assemblyName, fPrint, fDelete);
    END_ENTRYPOINT_NOTHROW;

    return hr;

}// NGenTryEnumerateFusionCache
#endif //FEATURE_FUSION

STDAPI_(BOOL) LegacyNGenCompile(HANDLE hZapper, LPCWSTR path)
{
    CONTRACTL{
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (hZapper == NULL || hZapper == INVALID_HANDLE_VALUE)
        return FALSE;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_VOIDRET;

    Zapper *zapper = (Zapper*)hZapper;

    EX_TRY
    {
        hr = zapper->Compile(path);
    }
    EX_CATCH_HRESULT(hr);

    END_ENTRYPOINT_VOIDRET;

    return (hr == S_OK) ? TRUE : FALSE;
}// NGenCompile

#endif // FEATURE_CORECLR || CROSSGEN_COMPILE

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
  m_compilerFlags(CORJIT_FLG_RELOC | CORJIT_FLG_PREJIT),
  m_legacyMode(false)
#ifdef FEATURE_CORECLR
  ,m_fNoMetaData(s_fNGenNoMetaData)
#endif
{
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

    zo->m_compilerFlags = CORJIT_FLG_RELOC | CORJIT_FLG_PREJIT;
    zo->m_autodebug = true;

    if (pOptions->fDebug)
    {
        zo->m_compilerFlags |= CORJIT_FLG_DEBUG_INFO|CORJIT_FLG_DEBUG_CODE;
        zo->m_autodebug = false;
    }

    if (pOptions->fProf)
    {
        zo->m_compilerFlags |= CORJIT_FLG_PROF_ENTERLEAVE;
    }

#ifdef FEATURE_FUSION
    if (pOptions->lpszRepositoryDir != NULL && pOptions->lpszRepositoryDir[0] != '\0')
    {
        size_t buflen = wcslen(pOptions->lpszRepositoryDir) + 1;
        LPWSTR lpszDir = new WCHAR[buflen];
        wcscpy_s(lpszDir, buflen, pOptions->lpszRepositoryDir);
        zo->m_repositoryDir = lpszDir;
    }
    else
    {
        zo->m_repositoryDir = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_RepositoryDir);
    }

    if (pOptions->repositoryFlags != RepositoryDefault)
    {
        zo->m_repositoryFlags = pOptions->repositoryFlags;
    }
    else
    {
        zo->m_repositoryFlags = (RepositoryFlags)REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_RepositoryFlags, RepositoryDefault);
    }

    // The default location of the repository is "repository" folder under framework version directory
    if (zo->m_repositoryDir == NULL)
    {
        DWORD lgth = _MAX_PATH + 1;
        WCHAR wszRepositoryDir[_MAX_PATH + 1];
        IfFailThrow(GetInternalSystemDirectory(wszRepositoryDir, &lgth));

        wcscat_s(wszRepositoryDir, COUNTOF(wszRepositoryDir), W("repository"));

        size_t buflen = wcslen(wszRepositoryDir) + 1;
        LPWSTR lpszDir = new WCHAR[buflen];
        wcscpy_s(lpszDir, buflen, wszRepositoryDir);
        zo->m_repositoryDir = lpszDir;

        // Move the images by default
        if (zo->m_repositoryFlags == RepositoryDefault)
            zo->m_repositoryFlags = MoveFromRepository;
    }
#endif //FEATURE_FUSION

    if (pOptions->fInstrument)
        zo->m_compilerFlags |= CORJIT_FLG_BBINSTR;

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

#ifndef FEATURE_MERGE_JIT_AND_ENGINE

//
// Pointer to the activated CLR interface provided by the shim.
//
extern ICLRRuntimeInfo *g_pCLRRuntime;

#endif // FEATURE_MERGE_JIT_AND_ENGINE

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

#ifdef FEATURE_FUSION
    hr = InitializeFusion();
    _ASSERTE(SUCCEEDED(hr));
#endif

#if defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
    m_fDontLoadJit = false;
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)

    m_fForceFullTrust = false;
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

#if defined(FEATURE_CORECLR) || defined(FEATURE_MERGE_JIT_AND_ENGINE)
    // Note: FEATURE_MERGE_JIT_AND_ENGINE is defined for the Desktop crossgen compilation as well.
    //
    PathString CoreClrFolder;
    extern HINSTANCE g_hThisInst;

#if defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
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
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
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
#else
    hr = g_pCLRRuntime->LoadLibrary(pwzJitName, phJit);
#endif

    if (FAILED(hr))
    {
        Error(W("Unable to load Jit Compiler: %s, hr=0x%08x\r\n"), pwzJitName, hr);
        ThrowLastError();
    }

#if (defined(FEATURE_CORECLR) || !defined(SELF_NO_HOST)) && !defined(CROSSGEN_COMPILE)
    typedef void (__stdcall* pSxsJitStartup) (CoreClrCallbacks const & cccallbacks);
    pSxsJitStartup sxsJitStartupFn = (pSxsJitStartup) GetProcAddress(*phJit, "sxsJitStartup");
    if (sxsJitStartupFn == NULL)
    {
        Error(W("Unable to load Jit startup function: %s\r\n"), pwzJitName);
        ThrowLastError();
    }

    CoreClrCallbacks cccallbacks = GetClrCallbacks();
    (*sxsJitStartupFn) (cccallbacks);
#endif

    typedef void (__stdcall* pJitStartup)(ICorJitHost* host);
    pJitStartup jitStartupFn = (pJitStartup)GetProcAddress(*phJit, "jitStartup");
    if (jitStartupFn != nullptr)
    {
        jitStartupFn(JitHost::getJitHost());
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

#ifdef ALLOW_LOCAL_WORKER
    //if this is not NULL it means that we've already initialized the EE.
    //Do not do so again.  However, verify that it would get initialized
    //the same way.
    if (g_eeInitialized)
    {
        if (fForceDebug != g_fForceDebug ||
            fForceProfile != g_fForceProfile ||
            fForceInstrument != g_fForceInstrument)
        {
            Error(W("AllowLocalWorker does not support changing EE initialization state\r\n"));
            ThrowHR(E_FAIL);
        }
    }
    else
    {
#endif // ALLOW_LOCAL_WORKER
        IfFailThrow(m_pEECompileInfo->Startup(fForceDebug, fForceProfile, fForceInstrument));
#ifdef ALLOW_LOCAL_WORKER
        g_fForceDebug = fForceDebug;
        g_fForceProfile = fForceProfile;
        g_fForceInstrument = fForceInstrument;
        g_eeInitialized = true;
    }
#endif // ALLOW_LOCAL_WORKER

    m_pEEJitInfo = GetZapJitInfo();

    //
    // Get JIT interface
    //

#ifdef FEATURE_MERGE_JIT_AND_ENGINE
    jitStartup(JitHost::getJitHost());
    m_pJitCompiler = getJit();

    if (m_pJitCompiler == NULL)
    {
        Error(W("Unable to initialize Jit Compiler\r\n"));
        ThrowLastError();
    }
#else

    CorCompileRuntimeDlls ngenDllId;

#if !defined(FEATURE_CORECLR)    
    ngenDllId = NGEN_COMPILER_INFO;
#else // FEATURE_CORECLR
    ngenDllId = CROSSGEN_COMPILER_INFO;
#endif

    LPCWSTR pwzJitName = CorCompileGetRuntimeDllName(ngenDllId);
    LoadAndInitializeJITForNgen(pwzJitName, &m_hJitLib, &m_pJitCompiler);
    
#if defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    // For reasons related to servicing, and RyuJIT rollout on .NET 4.6 and beyond, we only use RyuJIT when the registry
    // value UseRyuJIT (type DWORD), under key HKLM\SOFTWARE\Microsoft\.NETFramework, is set to 1. Otherwise, we fall back
    // to JIT64.
    //
    // See the document "RyuJIT Compatibility Fallback Specification.docx" for details.
    //
    // Also see the code and comments in EEJitManager::LoadJIT().

    if (!UseRyuJit())        // Do we need to fall back to JIT64 for NGEN?
    {
        LPCWSTR pwzJitName = MAKEDLLNAME_W(L"compatjit");

        // Note: if the compatjit fails to load, we ignore it, and continue to use the main JIT for
        // everything. You can imagine a policy where if the user requests the compatjit, and we fail
        // to load it, that we fail noisily. We don't do that currently.
        ICorJitCompiler* fallbackICorJitCompiler;
        LoadAndInitializeJITForNgen(pwzJitName, &m_hJitLegacy, &fallbackICorJitCompiler);

        // Tell the main JIT to fall back to the "fallback" JIT compiler, in case some
        // obfuscator tries to directly call the main JIT's getJit() function.
        m_pJitCompiler->setRealJit(fallbackICorJitCompiler);
    }
#endif // defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
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

#ifdef FEATURE_FUSION
HRESULT Zapper::TryEnumerateFusionCache(LPCWSTR name, bool fPrint, bool fDelete)
{
    HRESULT hr = S_OK;

    EX_TRY
    {
        if (EnumerateFusionCache(name, fPrint, fDelete) == 0)
            hr = S_FALSE;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

#define MAX_ZAP_STRING_SIZE 4

int Zapper::EnumerateFusionCache(
        LPCWSTR name,
        bool fPrint, bool fDelete,
        CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    _ASSERTE(pNativeImageSig == NULL || (*pNativeImageSig) != INVALID_NGEN_SIGNATURE);

    int count = 0;

    NonVMComHolder<IAssemblyName> pName;

    //
    // Decide whether the name is a file or assembly name
    //

    DWORD attributes = -1;

    if (name != NULL)
        attributes = WszGetFileAttributes(name);

    if (attributes == -1)
    {
        IfFailThrow(CreateAssemblyNameObject(&pName, name,
                                             name == NULL ? 0 :
                                             CANOF_PARSE_DISPLAY_NAME, NULL));
    }
    else if (attributes & FILE_ATTRIBUTE_DIRECTORY)
    {
        ClrDirectoryEnumerator de(name);

        while (de.Next())
        {
            StackSString fullName;
            fullName.Set(name, W("\\"), de.GetFileName());

            if (de.GetFileAttributes() & FILE_ATTRIBUTE_DIRECTORY)
            {
                count += EnumerateFusionCache(fullName, fPrint, fDelete);
            }
            else 
            if (IsAssembly(fullName))
            {
                if (TryEnumerateFusionCache(fullName, fPrint, fDelete) == S_OK)
                    count++;
            }
        }
    }
    else
    {
        NonVMComHolder<IMetaDataAssemblyImport> pAssemblyImport;
        IfFailThrow(m_pMetaDataDispenser->OpenScope(name, ofRead,
                                                    IID_IMetaDataAssemblyImport,
                                                    (IUnknown**)&pAssemblyImport));

        pName = GetAssemblyFusionName(pAssemblyImport);
    }

    if (pName != NULL)
    {
        pName->SetProperty(ASM_NAME_CUSTOM, NULL, 0);

        NonVMComHolder<IAssemblyEnum> pEnum;
        HRESULT hr = CreateAssemblyEnum(&pEnum, NULL, pName, ASM_CACHE_ZAP, 0);
        IfFailThrow(hr);

        pName.Clear();

        if (hr == S_OK)
        {
            //
            // Scope the iteration by the zap set
            //

            LPCWSTR zapPrefix = m_pOpt->m_zapSet;
            size_t zapPrefixSize = (zapPrefix != NULL) ? wcslen(zapPrefix) : 0;

            for (;;)
            {
                NonVMComHolder<IApplicationContext> pContext;
                NonVMComHolder<IAssemblyName> pEntryName;

                if (pEnum->GetNextAssembly(&pContext, &pEntryName, 0) != S_OK)
                    break;

                //
                // Only consider assemblies which have the proper zap string
                // prefix.
                //
                if (zapPrefix != NULL)
                {
                    WCHAR zapString[MAX_ZAP_STRING_SIZE];
                    DWORD zapStringSize = sizeof(zapString);

                    hr = pEntryName->GetProperty(ASM_NAME_CUSTOM, (void*) zapString, &zapStringSize);
                    if (hr != S_OK
                        || wcslen(zapString) < zapPrefixSize
                        || wcsncmp(zapString, zapPrefix, zapPrefixSize) != 0)
                    {
                        continue;
                    }
                }

                count++;

                if (pNativeImageSig)
                {
                    // If we are looking for a specific native image,
                    // check that the signatures match.

                    CORCOMPILE_NGEN_SIGNATURE sign;
                    DWORD cbSign = sizeof(sign);

                    IfFailThrow(pEntryName->GetProperty(ASM_NAME_MVID, &sign, &cbSign));
                    _ASSERTE(cbSign == sizeof(sign));

                    if (cbSign != sizeof(sign) || *pNativeImageSig != sign)
                        continue;
                }

                if (fPrint)
                {
                    PrintFusionCacheEntry(LogLevel_Success, pEntryName);
                }

                if (fDelete)
                {
                    DeleteFusionCacheEntry(pEntryName);
                }
            }
        }

    }

    return count;
}

void Zapper::PrintFusionCacheEntry(CorSvcLogLevel logLevel, IAssemblyName *pName)
{
    StackSString ss;
    FusionBind::GetAssemblyNameDisplayName(pName, ss, 
        ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN);

    GetSvcLogger()->Printf(logLevel, W("%s"), ss.GetUnicode());

    // Get the custom string
    WCHAR zapString[MAX_ZAP_STRING_SIZE];
    DWORD zapStringSize = sizeof(zapString);

    HRESULT hr = pName->GetProperty(ASM_NAME_CUSTOM, (void*) zapString, &zapStringSize);
    IfFailThrow(hr);
    IfFailThrow((zapStringSize != 0) ? S_OK : E_FAIL);

    // Get the config mask

    DWORD mask = 0;
    DWORD maskSize = sizeof(mask);

    hr = pName->GetProperty(ASM_NAME_CONFIG_MASK, (void*) &mask, &maskSize);
    IfFailThrow(hr);

    // Pretty-print the custom string and the config mask

    if (hr == S_OK)
    {
        DWORD maskStringLength;
        IfFailThrow(GetNativeImageDescription(zapString, mask, NULL, &maskStringLength));

        CQuickWSTR buffer;
        buffer.ReSizeThrows(maskStringLength * sizeof(WCHAR));
        IfFailThrow(GetNativeImageDescription(zapString, mask, buffer.Ptr(), &maskStringLength));

        GetSvcLogger()->Printf(logLevel, W("%s"), buffer.Ptr());
    }

    GetSvcLogger()->Printf(logLevel, W("\n"));

    StackSString s;
    PrintAssemblyVersionInfo(pName, s);

    GetSvcLogger()->Log(s.GetUnicode(), LogLevel_Info);
}

void Zapper::DeleteFusionCacheEntry(IAssemblyName *pName)
{
    IfFailThrow(UninstallNativeAssembly(pName, GetSvcLogger()->GetSvcLogger()));
}

void Zapper::DeleteFusionCacheEntry(LPCWSTR assemblyName, CORCOMPILE_NGEN_SIGNATURE *pNativeImageSig)
{
    _ASSERTE(assemblyName != NULL && pNativeImageSig != NULL);
    _ASSERTE(*pNativeImageSig != INVALID_NGEN_SIGNATURE);
    // assemblyName must be a display name, not a file name.
    _ASSERTE(WszGetFileAttributes(assemblyName) == INVALID_FILE_ATTRIBUTES);

    NonVMComHolder<IAssemblyName> pEntryName;
    IfFailThrow(CreateAssemblyNameObject(&pEntryName, assemblyName, CANOF_PARSE_DISPLAY_NAME, NULL));

    // Native Binder requires ASM_NAME_CUSTOM (zapset) not to be NULL while deleting a native image.
    LPCWSTR zapSet = m_pOpt->m_zapSet != NULL ? m_pOpt->m_zapSet : W("");
    pEntryName->SetProperty(ASM_NAME_CUSTOM, zapSet, DWORD(sizeof(WCHAR) * (wcslen(zapSet) + 1)));

    pEntryName->SetProperty(ASM_NAME_MVID, pNativeImageSig, sizeof(*pNativeImageSig));

    DeleteFusionCacheEntry(pEntryName);
}

// @TODO: Use the default flags like CORCOMPILE_CONFIG_DEBUG_DEFAULT

__success(SUCCEEDED(return))
STDAPI GetNativeImageDescription(__in_z LPCWSTR customString,
                                 DWORD dwConfigMask,
                                 __inout_ecount(*pdwLength) LPWSTR pwzString,
                                 LPDWORD pdwLength)
{
    _ASSERTE(pdwLength);
    //_ASSERTE(pwzString == NULL || (pwzString[(*pdwLength) - 1], true));

    #define ZAP_STRING_DEBUG            W("<debug>")
    #define ZAP_STRING_PROFILING        W("<profiling>")
    #define ZAP_STRING_INSTRUMENTED     W("<instrumented>")

    StackSString ssBuff;

    if (customString[0] != W('\0'))
    {
        ssBuff.Append(W(" (set "));
        ssBuff.Append(customString);
        ssBuff.Append(W(')'));
    }

    if (dwConfigMask & CORCOMPILE_CONFIG_DEBUG)
        ssBuff.Append(W(" ") ZAP_STRING_DEBUG);

    if (dwConfigMask & CORCOMPILE_CONFIG_PROFILING)
        ssBuff.Append(W(" ") ZAP_STRING_PROFILING);

    if (dwConfigMask & CORCOMPILE_CONFIG_INSTRUMENTATION)
        ssBuff.Append(W(" ") ZAP_STRING_INSTRUMENTED);

    DWORD length = (DWORD) ssBuff.GetCount() + 1; // +1 for the null terminating character
    DWORD inputLength = *pdwLength;

    *pdwLength = length;
    if (pwzString)
    {
        wcsncpy_s(pwzString, inputLength, ssBuff.GetUnicode(), _TRUNCATE);

        if (length > inputLength)
            return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    return S_OK;
}
#endif // FEATURE_FUSION

//**********************************************************************
// Copy of vm\i386\cgenCpu.h
// @TODO : clean this up. There has to be a way of sharing this code

//**********************************************************************
// To be used with GetSpecificCpuInfo()
#ifdef _TARGET_X86_

BOOL Runtime_Test_For_SSE2();

#define CPU_X86_FAMILY(cpuType)     (((cpuType) & 0x0F00) >> 8)
#define CPU_X86_MODEL(cpuType)      (((cpuType) & 0x00F0) >> 4)
// Stepping is masked out by GetSpecificCpuInfo()
// #define CPU_X86_STEPPING(cpuType)   (((cpuType) & 0x000F)     )

#define CPU_X86_USE_CMOV(cpuFeat)   ((cpuFeat & 0x00008001) == 0x00008001)
#define CPU_X86_USE_SSE2(cpuFeat)  (((cpuFeat & 0x04000000) == 0x04000000) && Runtime_Test_For_SSE2())

// Values for CPU_X86_FAMILY(cpuType)
#define CPU_X86_486                 4
#define CPU_X86_PENTIUM             5
#define CPU_X86_PENTIUM_PRO         6
#define CPU_X86_PENTIUM_4           0xF

// Values for CPU_X86_MODEL(cpuType) for CPU_X86_PENTIUM_PRO
#define CPU_X86_MODEL_PENTIUM_PRO_BANIAS    9 // Pentium M (Mobile PPro with P4 feautres)

#endif

#ifdef FEATURE_FUSION
void Zapper::PrintDependencies(
        IMetaDataAssemblyImport * pAssemblyImport,
        CORCOMPILE_DEPENDENCY * pDependencies,
        COUNT_T cDependencies,
        SString &s)
{
    if (cDependencies == 0)
    {
        s.AppendPrintf("\tNo dependencies\n");
        return;
    }

    s.AppendASCII("\tDependencies:\n");

    CORCOMPILE_DEPENDENCY *pDepsEnd = pDependencies + cDependencies;
    for(CORCOMPILE_DEPENDENCY *pDeps = pDependencies; pDeps < pDepsEnd; pDeps++)
    {
        mdAssemblyRef assem = pDeps->dwAssemblyDef;
        if (assem == mdAssemblyRefNil)
            assem = pDeps->dwAssemblyRef;

        NonVMComHolder<IAssemblyName> pNameHolder =
                GetAssemblyRefFusionName(pAssemblyImport, assem);

        StackSString ss;
        FusionBind::GetAssemblyNameDisplayName(pNameHolder, ss, ASM_DISPLAYF_FULL);

        s.AppendPrintf(W("\t\t%s:\n"), ss.GetUnicode());

        if (pDeps->dwAssemblyDef == mdAssemblyRefNil)
        {
            s.AppendASCII("\t\t\t** Missing dependency assembly **\n");
            continue;
        }

        //
        // Dependency MVID/HASH
        //

        WCHAR szGuid[64];
        GuidToLPWSTR(pDeps->signAssemblyDef.mvid, szGuid, NumItems(szGuid));
        s.AppendPrintf(W("\t\t\tGuid:%s\n"), szGuid);

        if (pDeps->dwAssemblyRef != pDeps->dwAssemblyDef)
        {
            // If there is a redirect, display the original dependency version

            NonVMComHolder<IAssemblyName> pOrigName =
              GetAssemblyRefFusionName(pAssemblyImport, pDeps->dwAssemblyRef);

            StackSString ss;
            FusionBind::GetAssemblyNameDisplayName(pOrigName, ss, ASM_DISPLAYF_VERSION);

            s.AppendPrintf(W("\t\t\tOriginal Ref: %s\n"), ss.GetUnicode());
        }

        if (pDeps->signNativeImage != INVALID_NGEN_SIGNATURE)
        {
            GuidToLPWSTR(pDeps->signNativeImage, szGuid, NumItems(szGuid));
            s.AppendPrintf(W("\t\t\tHardbound Guid:%s\n"), szGuid);
        }
    }

    s.AppendASCII("\n");
}


BOOL Zapper::VerifyDependencies(
        IMDInternalImport * pAssemblyImport,
        CORCOMPILE_DEPENDENCY * pDependencies,
        COUNT_T cDependencies)
{
    CORCOMPILE_DEPENDENCY *pDepsEnd = pDependencies + cDependencies;
    for(CORCOMPILE_DEPENDENCY *pDeps = pDependencies; pDeps < pDepsEnd; pDeps++)
    {
        mdAssemblyRef assem = pDeps->dwAssemblyDef;

        // TODO: Better support for images with unresolved dependencies?
        if (assem == mdAssemblyRefNil)
            continue;

        CORINFO_ASSEMBLY_HANDLE hAssemblyRef;
        if (m_pEECompileInfo->LoadAssemblyRef(pAssemblyImport, assem, &hAssemblyRef) != S_OK)
            return FALSE;

        CORCOMPILE_VERSION_INFO sourceVersionInfo;
        IfFailThrow(m_pEECompileInfo->GetAssemblyVersionInfo(hAssemblyRef,
                                                            &sourceVersionInfo));

        // check the soft bound dependency
        CORCOMPILE_ASSEMBLY_SIGNATURE * pSign1 = &pDeps->signAssemblyDef;
        CORCOMPILE_ASSEMBLY_SIGNATURE * pSign2 = &sourceVersionInfo.sourceAssembly;

        if (   (pSign1->mvid != pSign2->mvid)
            || (pSign1->timeStamp != pSign2->timeStamp)
            || (pSign1->ilImageSize != pSign2->ilImageSize) )
        {
            return FALSE;
        }

        if (pDeps->signNativeImage != INVALID_NGEN_SIGNATURE)
        {
            // check the hardbound dependency
            CORCOMPILE_NGEN_SIGNATURE nativeImageSig;

            if (!CheckAssemblyUpToDate(hAssemblyRef, &nativeImageSig))
                return FALSE;

            if (pDeps->signNativeImage != nativeImageSig)
                return FALSE;
        }
    }

    return TRUE;
}

void Zapper::PrintAssemblyVersionInfo(IAssemblyName *pName, SString &s)
{
    //
    // Bind zap assembly to a path
    //

    WCHAR szGuid[64];
    WCHAR path[MAX_LONGPATH];
    DWORD cPath = MAX_LONGPATH;

    IfFailThrow(QueryNativeAssemblyInfo(pName, path, &cPath));

    // The LoadLibrary call fails occasionally in the lab due to sharing violation.  Retry when needed.
    const int SHARING_VIOLATION_RETRY_TIMES = 10;
    const DWORD SHARING_VIOLATION_RETRY_WAITING_TIME = 100;
    HModuleHolder hMod;
    DWORD err;
    for (int i = 0; i < SHARING_VIOLATION_RETRY_TIMES; i++)
    {
        hMod = ::WszLoadLibrary(path);
        if (! hMod.IsNull())
            break;
        // Save last error before ClrSleepEx overwrites it.
        err = GetLastError();
        ClrSleepEx(SHARING_VIOLATION_RETRY_WAITING_TIME, FALSE);
    }
    if (hMod.IsNull())
        ThrowWin32(err);

    PEDecoder pedecoder(hMod);

    CORCOMPILE_VERSION_INFO *pVersionInfo = pedecoder.GetNativeVersionInfo();

    COUNT_T cMeta;
    const void *pMeta = pedecoder.GetNativeManifestMetadata(&cMeta);

    NonVMComHolder<IMetaDataAssemblyImport> pAssemblyImport;
    IfFailThrow(m_pMetaDataDispenser->OpenScopeOnMemory(pMeta, cMeta, ofRead,
                                    IID_IMetaDataAssemblyImport,
                                    (IUnknown**)&pAssemblyImport));

    NonVMComHolder<IMetaDataImport> pImport;
    IfFailThrow(pAssemblyImport->QueryInterface(IID_IMetaDataImport,
                                                (void**)&pImport));

    //
    // Source MVID
    //

    GuidToLPWSTR(pVersionInfo->sourceAssembly.mvid, szGuid, NumItems(szGuid));
    s.AppendPrintf(W("\tSource MVID:\t%s\n"), szGuid);

    //
    // Signature of generated ngen image
    //

    GuidToLPWSTR(pVersionInfo->signature, szGuid, NumItems(szGuid));
    s.AppendPrintf(W("\tNGen GUID sign:\t%s\n"), szGuid);


    s.AppendASCII("\tOS:\t\t");
    switch (pVersionInfo->wOSPlatformID)
    {
    case VER_PLATFORM_WIN32_NT:
        s.AppendASCII("WinNT");
        break;
    default:
        s.AppendPrintf("<unknown> (%d)", pVersionInfo->wOSPlatformID);
        break;
    }
    s.AppendASCII("\n");

    //
    // Processor
    //

    s.AppendASCII("\tProcessor:\t");

    CORINFO_CPU cpuInfo = pVersionInfo->cpuInfo;

    switch (pVersionInfo->wMachine)
    {
    case IMAGE_FILE_MACHINE_I386:
    {
        s.AppendASCII("x86");
        DWORD cpuType = cpuInfo.dwCPUType;
#ifdef _TARGET_X86_

        //
        // Specific processor ID
        //

        switch (CPU_X86_FAMILY(cpuType))
        {
        case CPU_X86_486:
            s.AppendASCII("(486)");
            break;

        case CPU_X86_PENTIUM:
            s.AppendASCII("(Pentium)");
            break;

        case CPU_X86_PENTIUM_PRO:
            if(CPU_X86_MODEL(cpuType) == CPU_X86_MODEL_PENTIUM_PRO_BANIAS)
            {
                s.AppendASCII("(Pentium M)");
            }
            else
            {
                s.AppendASCII("(PentiumPro)");
            }
            break;

        case CPU_X86_PENTIUM_4:
            s.AppendASCII("(Pentium 4)");
            break;

        default:
            s.AppendPrintf("(Family %d, Model %d (%03x))",
                   CPU_X86_FAMILY(cpuType), CPU_X86_MODEL(cpuType), cpuType);
            break;
        }

        s.AppendPrintf(" (features: %08x)", cpuInfo.dwFeatures);

        break;

#endif // _TARGET_X86_
    }

    case IMAGE_FILE_MACHINE_IA64:
        s.AppendASCII("ia64");
        break;

    case IMAGE_FILE_MACHINE_AMD64:
        s.AppendASCII("amd64");
        break;

    case IMAGE_FILE_MACHINE_ARMNT:
        s.AppendASCII("arm");
        break;
    }
    s.AppendASCII("\n");

    //
    // EE version
    //

    s.AppendPrintf("\tRuntime:\t%d.%d.%.d.%d\n",
           pVersionInfo->wVersionMajor, pVersionInfo->wVersionMinor,
           pVersionInfo->wVersionBuildNumber, pVersionInfo->wVersionPrivateBuildNumber);

    s.AppendPrintf("\tclr.dll:\tTimeStamp=%08X, VirtualSize=%08X\n",
           pVersionInfo->runtimeDllInfo[CLR_INFO].timeStamp,
           pVersionInfo->runtimeDllInfo[CLR_INFO].virtualSize);

    //
    // Flags
    //

    s.AppendASCII("\tFlags:\t\t");
    if (pVersionInfo->wBuild == CORCOMPILE_BUILD_CHECKED)
    {
        s.AppendASCII("<checked> ");
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROFILING)
    {
        s.AppendASCII("<profiling> ");
    }
    else if ((pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_DEBUGGING))
    {
        s.AppendASCII("<debug> ");
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROF_INSTRUMENTING)
    {
        s.AppendASCII("<instrumenting> ");
    }
    s.AppendASCII("\n");

    //
    // Config
    //

    s.AppendASCII("\tScenarios:\t\t");

    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_DEBUG_NONE)
    {
        s.AppendASCII("<no debug info> ");
    }
    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_DEBUG)
    {
        s.AppendASCII("<debugger> ");
    }
    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_DEBUG_DEFAULT)
    {
        s.AppendASCII("<no debugger> ");
    }

    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_PROFILING_NONE)
    {
        s.AppendASCII("<no profiler> ");
    }
    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_PROFILING)
    {
        s.AppendASCII("<instrumenting profiler> ");
    }

    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_INSTRUMENTATION_NONE)
    {
        s.AppendASCII("<no instrumentation> ");
    }

    if (pVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_INSTRUMENTATION)
    {
        s.AppendASCII("<block instrumentation> ");
    }

    s.AppendASCII("\n");

    //
    // Native image file name
    //

    s.AppendPrintf(W("\tFile:\t\t%s\n"), path);

    //
    // Dependencies
    //

    COUNT_T cDependencies;
    CORCOMPILE_DEPENDENCY *pDependencies = pedecoder.GetNativeDependencies(&cDependencies);

    PrintDependencies(pAssemblyImport, pDependencies, cDependencies, s);
}

IAssemblyName *Zapper::GetAssemblyFusionName(IMetaDataAssemblyImport *pImport)
{
    IAssemblyName *pName;

    mdAssembly a;
    IfFailThrow(pImport->GetAssemblyFromScope(&a));

    ASSEMBLYMETADATA md = {0};
    LPWSTR szName;
    ULONG cbName = 0;
    const void *pbPublicKeyToken;
    ULONG cbPublicKeyToken;
    DWORD dwFlags;

    IfFailThrow(pImport->GetAssemblyProps(a,
                                          NULL, NULL, NULL,
                                          NULL, 0, &cbName,
                                          &md,
                                          NULL));

    szName = (LPWSTR) _alloca(cbName * sizeof(WCHAR));
    md.szLocale = (LPWSTR) _alloca(md.cbLocale * sizeof(WCHAR));
    md.rProcessor = (DWORD *) _alloca(md.ulProcessor * sizeof(DWORD));
    md.rOS = (OSINFO *) _alloca(md.ulOS * sizeof(OSINFO));

    IfFailThrow(pImport->GetAssemblyProps(a,
                                          &pbPublicKeyToken, &cbPublicKeyToken, NULL,
                                          szName, cbName, &cbName,
                                          &md,
                                          &dwFlags));

    IfFailThrow(CreateAssemblyNameObject(&pName, szName, 0, NULL));

    if (md.usMajorVersion != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_MAJOR_VERSION,
                                       &md.usMajorVersion,
                                       sizeof(USHORT)));
    if (md.usMinorVersion != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_MINOR_VERSION,
                                       &md.usMinorVersion,
                                       sizeof(USHORT)));
    if (md.usBuildNumber != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_BUILD_NUMBER,
                                       &md.usBuildNumber,
                                       sizeof(USHORT)));
    if (md.usRevisionNumber != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_REVISION_NUMBER,
                                       &md.usRevisionNumber,
                                       sizeof(USHORT)));
    if (md.ulProcessor > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_PROCESSOR_ID_ARRAY,
                                       &md.rProcessor,
                                       md.ulProcessor*sizeof(DWORD)));
    if (md.ulOS > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_OSINFO_ARRAY,
                                       &md.rOS,
                                       md.ulOS*sizeof(OSINFO)));
    if (md.cbLocale > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_CULTURE,
                                       md.szLocale,
                                       md.cbLocale*sizeof(WCHAR)));

    if (cbPublicKeyToken > 0)
    {
        if (!StrongNameTokenFromPublicKey((BYTE*)pbPublicKeyToken, cbPublicKeyToken,
                                          (BYTE**)&pbPublicKeyToken, &cbPublicKeyToken))
            IfFailThrow(StrongNameErrorInfo());

        IfFailThrow(pName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                                       (void*)pbPublicKeyToken,
                                       cbPublicKeyToken));

        StrongNameFreeBuffer((BYTE*)pbPublicKeyToken);
    }

    return pName;
}

IAssemblyName *Zapper::GetAssemblyRefFusionName(IMetaDataAssemblyImport *pImport,
                                                mdAssemblyRef ar)
{
    IAssemblyName *pName;

    ASSEMBLYMETADATA md = {0};
    LPWSTR szName;
    ULONG cbName = 0;
    const void *pbPublicKeyOrToken;
    ULONG cbPublicKeyOrToken;
    DWORD dwFlags;

    IfFailThrow(pImport->GetAssemblyRefProps(ar,
                                             NULL, NULL,
                                             NULL, 0, &cbName,
                                             &md,
                                             NULL, NULL,
                                             NULL));

    szName = (LPWSTR) _alloca(cbName * sizeof(WCHAR));
    md.szLocale = (LPWSTR) _alloca(md.cbLocale * sizeof(WCHAR));
    md.rProcessor = (DWORD *) _alloca(md.ulProcessor * sizeof(DWORD));
    md.rOS = (OSINFO *) _alloca(md.ulOS * sizeof(OSINFO));

    IfFailThrow(pImport->GetAssemblyRefProps(ar,
                                             &pbPublicKeyOrToken, &cbPublicKeyOrToken,
                                             szName, cbName, &cbName,
                                             &md,
                                             NULL, NULL,
                                             &dwFlags));

    IfFailThrow(CreateAssemblyNameObject(&pName, szName, 0, NULL));

    if (md.usMajorVersion != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_MAJOR_VERSION,
                                       &md.usMajorVersion,
                                       sizeof(USHORT)));
    if (md.usMinorVersion != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_MINOR_VERSION,
                                       &md.usMinorVersion,
                                       sizeof(USHORT)));
    if (md.usBuildNumber != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_BUILD_NUMBER,
                                       &md.usBuildNumber,
                                       sizeof(USHORT)));
    if (md.usRevisionNumber != -1)
        IfFailThrow(pName->SetProperty(ASM_NAME_REVISION_NUMBER,
                                       &md.usRevisionNumber,
                                       sizeof(USHORT)));
    if (md.ulProcessor > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_PROCESSOR_ID_ARRAY,
                                       &md.rProcessor,
                                       md.ulProcessor*sizeof(DWORD)));
    if (md.ulOS > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_OSINFO_ARRAY,
                                       &md.rOS,
                                       md.ulOS*sizeof(OSINFO)));
    if (md.cbLocale > 0)
        IfFailThrow(pName->SetProperty(ASM_NAME_CULTURE,
                                       md.szLocale,
                                       md.cbLocale*sizeof(WCHAR)));

    if (cbPublicKeyOrToken > 0)
    {
        if (dwFlags & afPublicKey)
            IfFailThrow(pName->SetProperty(ASM_NAME_PUBLIC_KEY,
                                           (void*)pbPublicKeyOrToken,
                                           cbPublicKeyOrToken));
        else
            IfFailThrow(pName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                                           (void*)pbPublicKeyOrToken,
                                           cbPublicKeyOrToken));
    }
    else
    {
        IfFailThrow(pName->SetProperty(ASM_NAME_NULL_PUBLIC_KEY_TOKEN,
                                       NULL, 0));
    }

    // See if the assemblyref is retargetable (ie, for a generic assembly).
    if (IsAfRetargetable(dwFlags))
    {
        BOOL bTrue = TRUE;
        IfFailThrow(pName->SetProperty(ASM_NAME_RETARGET,
                                       &bTrue,
                                       sizeof(bTrue)));
    }

    return pName;
}
#endif //FEATURE_FUSION

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

/*static*/ HRESULT Zapper::GenericDomainCallback(LPVOID pvArgs)
{
    STATIC_CONTRACT_ENTRY_POINT;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    EX_TRY
    {
        REMOVE_STACK_GUARD;
        ((DomainCallback *) pvArgs)->doCallback();
    }
    EX_CATCH_HRESULT(hr);

    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}

void Zapper::InvokeDomainCallback(DomainCallback *callback)
{
#ifdef CROSSGEN_COMPILE
    // Call the callback directly for better error messages (avoids exception swallowing and rethrow)
    callback->doCallback();
#else
    IfFailThrow(m_pEECompileInfo->MakeCrossDomainCallback(m_pDomain,
                                                          Zapper::GenericDomainCallback,
                                                          (LPVOID) callback));
#endif
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

#if defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
void ZapperSetPlatformAssembliesPaths(SString &platformAssembliesPaths);
#endif

#ifdef FEATURE_CORECLR
void ZapperSetBindingPaths(ICorCompilationDomain *pDomain, SString &trustedPlatformAssemblies, SString &platformResourceRoots, SString &appPaths, SString &appNiPaths);
#endif

void Zapper::CreateCompilationDomain()
{
#if defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    // Platform assemblies paths have to be set before appdomain is setup so that
    // mscorlib.dll can be loaded from them.
    ZapperSetPlatformAssembliesPaths(m_platformAssembliesPaths);
#endif

    BOOL fForceDebug = FALSE;
    if (!m_pOpt->m_autodebug)
        fForceDebug = (m_pOpt->m_compilerFlags & CORJIT_FLG_DEBUG_INFO) != 0;

    BOOL fForceProfile = (m_pOpt->m_compilerFlags & CORJIT_FLG_PROF_ENTERLEAVE) != 0;
    BOOL fForceInstrument = (m_pOpt->m_compilerFlags & CORJIT_FLG_BBINSTR) != 0;

    InitEE(fForceDebug, fForceProfile, fForceInstrument);

    // Make a compilation domain for the assembly.  Note that this will
    // collect the assembly dependencies for use in the version info, as
    // well as isolating the compilation code.

    IfFailThrow(m_pEECompileInfo->CreateDomain(&m_pDomain,
                                               CreateAssemblyEmitter(),
                                               fForceDebug,
                                               fForceProfile,
                                               fForceInstrument,
                                               m_fForceFullTrust));

#ifdef CROSSGEN_COMPILE
    IfFailThrow(m_pDomain->SetPlatformWinmdPaths(m_platformWinmdPaths));
#endif

#ifdef FEATURE_CORECLR
    // we support only TPA binding on CoreCLR

    if (!m_trustedPlatformAssemblies.IsEmpty())
    {
        // TPA-based binding
        // Add the trusted paths and apppath to the binding list
        ZapperSetBindingPaths(m_pDomain, m_trustedPlatformAssemblies, m_platformResourceRoots, m_appPaths, m_appNiPaths);
    }
#endif
}

void Zapper::CreateDependenciesLookupDomain()
{
    class Callback : public DomainCallback
    {
    public:
        Callback(Zapper *pZapper)
        {
            this->pZapper = pZapper;
        }

        virtual void doCallback()
        {
            pZapper->CreateDependenciesLookupDomainInCurrentDomain();
        }

        Zapper* pZapper;
    };


    CreateCompilationDomain();

    Callback callback(this);
    InvokeDomainCallback(&callback);
}

void Zapper::CreatePdb(BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath)
{
    class Callback : public DomainCallback {

    public:

        Callback(Zapper *pZapper, BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath)
            : m_pZapper(pZapper),
              m_pAssemblyPathOrName(pAssemblyPathOrName),
              m_pNativeImagePath(pNativeImagePath),
              m_pPdbPath(pPdbPath),
              m_pdbLines(pdbLines),
              m_pManagedPdbSearchPath(pManagedPdbSearchPath)
        {
        }
                
        virtual void doCallback()
        {
            m_pZapper->CreatePdbInCurrentDomain(m_pAssemblyPathOrName, m_pNativeImagePath, m_pPdbPath, m_pdbLines, m_pManagedPdbSearchPath);
        };

    private:

        Zapper *m_pZapper;
        BSTR m_pAssemblyPathOrName;
        BSTR m_pNativeImagePath;
        BSTR m_pPdbPath;
        BOOL m_pdbLines;
        BSTR m_pManagedPdbSearchPath;

    };

#ifdef CROSSGEN_COMPILE
    CreateCompilationDomain();
#endif
    _ASSERTE(m_pDomain);

    Callback callback(this, pAssemblyPathOrName, pNativeImagePath, pPdbPath, pdbLines, pManagedPdbSearchPath);
    InvokeDomainCallback(&callback);
}


void Zapper::CreatePdbInCurrentDomain(BSTR pAssemblyPathOrName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath)
{
    EX_TRY
    {
        CORINFO_ASSEMBLY_HANDLE hAssembly = NULL;

        m_pEECompileInfo->SetIsGeneratingNgenPDB(TRUE);

#ifdef FEATURE_FUSION
        ReleaseHolder<IAssemblyName> pAssemblyName(NULL);	
        HRESULT hr;

        if (pAssemblyPathOrName == NULL)
        {
            // No root was found, so we don't have an IL assembly path yet. Get IAssemblyName
            // from the aux file next to pNativeImagePath, and load from that IAssemblyName
            IfFailThrow(GetAssemblyNameFromNIPath(pNativeImagePath, &pAssemblyName));
            IfFailThrow(m_pEECompileInfo->LoadAssemblyByIAssemblyName(pAssemblyName, &hAssembly));
        }
        else
        {
            DWORD attributes = WszGetFileAttributes(pAssemblyPathOrName);
            if (attributes == INVALID_FILE_ATTRIBUTES || 
                ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
            {
                // pAssemblyPathOrName doesn't map to a valid file, so assume it's an
                // assembly name that we can parse and generate an IAssemblyName from, and
                // then load from the IAssemblyName

                IfFailThrow(m_pEECompileInfo->LoadAssemblyByName(pAssemblyPathOrName, &hAssembly));

            }
            else
            {
                // pAssemblyPathOrName DOES map to a valid file, so load it directly.
                IfFailThrow(m_pEECompileInfo->LoadAssemblyByPath(pAssemblyPathOrName, 
                    // fExplicitBindToNativeImage: On the phone, a path to the NI is specified
                    // explicitly (even with .ni. in the name). All other callers specify a path to
                    // the IL, and the NI is inferred, so this is normally FALSE in those other
                    // cases.
                    FALSE,
                    &hAssembly));
            }
        }

        // Now is a good time to make sure pNativeImagePath is the same native image
        // fusion loaded
        {
            WCHAR wzZapImagePath[MAX_LONGPATH] = {0};
            DWORD dwZapImagePathLength = MAX_LONGPATH;

            hr = E_FAIL;
            if (m_pEECompileInfo->CheckAssemblyZap(hAssembly, wzZapImagePath, &dwZapImagePathLength))
            {
                if (_wcsicmp(wzZapImagePath, pNativeImagePath) != 0)
                {
                    GetSvcLogger()->Printf(
                        W("Unable to load '%s'.  Please ensure that it is a native image and is up to date.  Perhaps you meant to specify this native image instead: '%s'\n"),
                        pNativeImagePath,
                        wzZapImagePath);

                    ThrowHR(E_FAIL);
                }
            }
        }
#else // FEATURE_FUSION
        IfFailThrow(m_pEECompileInfo->LoadAssemblyByPath(
            pAssemblyPathOrName, 

            // fExplicitBindToNativeImage: On the phone, a path to the NI is specified
            // explicitly (even with .ni. in the name). All other callers specify a path to
            // the IL, and the NI is inferred, so this is normally FALSE in those other
            // cases.
            TRUE, 

            &hAssembly));
#endif // FEATURE_FUSION


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
#if defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)
        if (m_DiasymreaderPath.GetCount() > 0)
        {
            pDiasymreaderPath = m_DiasymreaderPath.GetUnicode();
        }
#endif // defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)

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
#ifdef FEATURE_FUSION
    class Callback : public DomainCallback
    {
    public:
        Callback(Zapper *pZapper, LPCWSTR pAssemblyName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
        {
            this->pZapper         = pZapper;
            this->pAssemblyName   = pAssemblyName;
            this->pNativeImageSig = pNativeImageSig;
        }

        virtual void doCallback()
        {
            pZapper->ComputeDependenciesInCurrentDomain(pAssemblyName, pNativeImageSig);
        }

        Zapper* pZapper;
        LPCWSTR pAssemblyName;
        CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig;
    };

    _ASSERTE(m_pDomain);
    Callback callback(this, pAssemblyName, pNativeImageSig);
    InvokeDomainCallback(&callback);
#endif // FEATURE_FUSION
}

#ifdef FEATURE_FUSION

void Zapper::ComputeDependenciesInCurrentDomain(LPCWSTR pAssemblyString, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    //
    // Load the assembly.
    //
    // "string" may be a path or assembly display name.
    // To decide, we see if it is the name of a valid file.
    //

    CORINFO_ASSEMBLY_HANDLE hAssembly;
    HRESULT                 hr;

    DWORD attributes = WszGetFileAttributes(pAssemblyString);
    if (attributes == INVALID_FILE_ATTRIBUTES || ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
    {
        IfFailThrow(m_pEECompileInfo->LoadAssemblyByName(pAssemblyString,
                                                         &hAssembly));
    }
    else
    {
#if defined(FEATURE_APPX) && !defined(FEATURE_CORECLR)
        if (m_pOpt->m_fAutoNGen)
        {
            // Make sure we're not been spoofed into loading an assembly that might be unsafe to load.
            // Loading by path so we better be AppX or a WinMD
            StackSString s(pAssemblyString);
            SString literalWinMD(SString::Literal, W(".winmd"));
            BOOL isWinMD = (s.GetCount() > 6) && s.MatchCaseInsensitive(s.End() - 6, literalWinMD);
            if (!AppX::IsAppXProcess() && !isWinMD)
            {
                Error(W("Cannot load assembly %s for automatic NGen.\n"), pAssemblyString);
                ThrowHR(E_FAIL);
            }

            // Is AppX NGen disabled?
            if ((AssemblyUsageLogManager::GetUsageLogFlags() & AssemblyUsageLogManager::ASSEMBLY_USAGE_LOG_FLAGS_APPLOCALNGENDISABLED) != 0)
            {
                memset(pNativeImageSig, 0, sizeof(*pNativeImageSig));   // Fake NI signature to disable NGen.
                Warning(W("NGen disabled for this application.\n"));
                return;
            }
        }
#endif

        hr = m_pEECompileInfo->LoadAssemblyByPath(pAssemblyString,
                                                  FALSE,  // fExplicitBindToNativeImage
                                                  &hAssembly);
        if (hr == CLR_E_BIND_TYPE_NOT_FOUND)
        {   // This means we are ngen'ing WinMD file which does not have any public WinRT type - therefore cannot be ever used
            // Note: It's comming from call to code:GetFirstWinRTTypeDef
            
            // Let's not create the ngen image
            if (pNativeImageSig != NULL)
            {
                memset(pNativeImageSig, 0, sizeof(*pNativeImageSig));   // Fake NI signature to disable NGen.
            }
            Warning(W("NGen is not supported for empty WinMD files.\n"));
            return;
        }
        IfFailThrow(hr);
    }

#ifndef FEATURE_CORECLR
    if (m_pOpt->m_fAutoNGen && !m_pEECompileInfo->SupportsAutoNGen(hAssembly))
    {
        Error(W("Assembly %s does not support automatic NGen.\n"), pAssemblyString);
        ThrowHR(E_FAIL);
    }
#endif // FEATURE_CORECLR

    //
    // Check if we have a native image already, and if so get its GUID
    //

    WCHAR zapManifestPath[MAX_LONGPATH];
    DWORD cZapManifestPath = MAX_LONGPATH;
    if (pNativeImageSig &&
        m_pEECompileInfo->CheckAssemblyZap(hAssembly, zapManifestPath, &cZapManifestPath))
    {
        NonVMComHolder<INativeImageInstallInfo> pNIInstallInfo;

        IfFailThrow(GetAssemblyMDInternalImport(
            zapManifestPath,
            IID_INativeImageInstallInfo,
            (IUnknown **)&pNIInstallInfo));

        IfFailThrow(pNIInstallInfo->GetSignature(pNativeImageSig));
    }

    //
    // Set the display name for the assembly
    //

    StackSString ss;
    GetAssemblyName(m_pEECompileInfo, hAssembly, ss, ICorCompileInfo::GANF_Default);

    m_assemblyDependencies.SetDisplayName(ss.GetUnicode());

    ComputeAssemblyDependencies(hAssembly);
}

void Zapper::ComputeAssemblyDependencies(CORINFO_ASSEMBLY_HANDLE hAssembly)
{
    HRESULT hr = S_OK;
    NonVMComHolder<IMDInternalImport> pAssemblyImport = m_pEECompileInfo->GetAssemblyMetaDataImport(hAssembly);

    EX_TRY
    {
        //
        // Enumerate the dependencies
        //

        HENUMInternalHolder hEnum(pAssemblyImport);
        hEnum.EnumAllInit(mdtAssemblyRef);

        // Need to reinitialize the dependencies list, since we could have been called for other assemblies
        // belonging to the same root.  Zapper::ComputeAssemblyDependencies is first called for the root
        // assembly, then called for each hard dependencies, and called for each soft dependencies unless
        // /nodependencies switch is used.
        m_assemblyDependencies.Reinitialize();

        if (!CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenAllowMscorlibSoftbind))
        {
            // Force all assemblies (other than mscorlib itself) to hardbind to mscorlib.
            // This ensures that mscorlib is NGen'ed before all other assemblies.
            CORINFO_ASSEMBLY_HANDLE hAssemblyMscorlib = m_pEECompileInfo->GetModuleAssembly(m_pEECompileInfo->GetLoaderModuleForMscorlib());
            if (hAssembly != hAssemblyMscorlib)
            {
                StackSString ss;
                GetAssemblyName(m_pEECompileInfo, hAssemblyMscorlib, ss, ICorCompileInfo::GANF_Default);
    
                m_assemblyDependencies.Append(ss.GetUnicode(), LoadAlways, NGenDefault);
            }
        }

        mdAssembly token;
        mdMethodDef md;
        while (pAssemblyImport->EnumNext(&hEnum, &token))
        {
            CORINFO_ASSEMBLY_HANDLE hAssemblyRef;
            HRESULT hrLoad = m_pEECompileInfo->LoadAssemblyRef(pAssemblyImport, token, &hAssemblyRef);
            if (FAILED(hrLoad))
            {
                // Failed to load a dependency.  Print a warning and move to the next dependency.

                LPCSTR pszName;
                IfFailThrow(pAssemblyImport->GetAssemblyRefProps(token, NULL, NULL,
                                                         &pszName, NULL,
                                                         NULL, NULL, NULL));

                StackSString sName(SString::Utf8, pszName);

                StackSString hrMsg;
                GetHRMsg(hrLoad, hrMsg);

                Warning(W("Failed to load dependency %s of assembly %s because of the following error : %s\n"),
                        sName.GetUnicode(),
                        m_assemblyDependencies.GetDisplayName(),
                        hrMsg.GetUnicode());
                continue;
            }
            else if (hrLoad == S_OK)
            {
                _ASSERTE(hAssemblyRef != NULL);

                StackSString ss;
                GetAssemblyName(m_pEECompileInfo, hAssemblyRef, ss, ICorCompileInfo::GANF_Default);

                LoadHintEnum loadHint = LoadDefault;
                IfFailThrow(m_pEECompileInfo->GetLoadHint(hAssembly, hAssemblyRef, &loadHint));

                NGenHintEnum ngenHint = NGenDefault;
                // Not supported
                //IfFailThrow(m_pEECompileInfo->GetNGenHint(hAssemblyRef, &ngenHint));
                m_assemblyDependencies.Append(ss.GetUnicode(), loadHint, ngenHint);
            }
        }

#ifdef FEATURE_COMINTEROP
        HENUMInternalHolder hTypeRefEnum(pAssemblyImport);
        hTypeRefEnum.EnumAllInit(mdtTypeRef);

        mdTypeRef tkTypeRef;
        while(pAssemblyImport->EnumNext(&hTypeRefEnum, &tkTypeRef))
        {
            CORINFO_ASSEMBLY_HANDLE hAssemblyRef;
            HRESULT hrLoad = m_pEECompileInfo->LoadTypeRefWinRT(pAssemblyImport, tkTypeRef, &hAssemblyRef);
            if (FAILED(hrLoad))
            {
                // Failed to load a dependency.  Print a warning and move to the next dependency.
                LPCSTR psznamespace;
                LPCSTR pszname;
                pAssemblyImport->GetNameOfTypeRef(tkTypeRef, &psznamespace, &pszname);
                
                StackSString sName(SString::Utf8, pszname);
                StackSString sNamespace(SString::Utf8, psznamespace);
                StackSString hrMsg;
                GetHRMsg(hrLoad, hrMsg);

                Warning(W("Failed to load WinRT type dependency %s.%s of assembly %s because of the following error : %s\n"),
                        sNamespace.GetUnicode(),
                        sName.GetUnicode(),
                        m_assemblyDependencies.GetDisplayName(),
                        hrMsg.GetUnicode());
                continue;
            }
            else if (hrLoad == S_OK)
            {
                _ASSERTE(hAssemblyRef != NULL);
                StackSString ss;

                CORINFO_MODULE_HANDLE hModule = m_pEECompileInfo->GetAssemblyModule(hAssemblyRef);
                m_pEECompileInfo->GetModuleFileName(hModule, ss);

                LoadHintEnum loadHint = LoadDefault;
                IfFailThrow(m_pEECompileInfo->GetLoadHint(hAssembly, hAssemblyRef, &loadHint));

                NGenHintEnum ngenHint = NGenDefault;
                
                // Append verifies no duplicates
                m_assemblyDependencies.Append(ss.GetUnicode(), loadHint, ngenHint);
            }
        }
#endif
        //
        // Get the default NGen setting for the assembly
        //

        NGenHintEnum ngenHint = NGenDefault;
        // Not supported
        // IfFailThrow(m_pEECompileInfo->GetNGenHint(hAssembly, &ngenHint));
        m_assemblyDependencies.SetNGenHint(ngenHint);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
        RetailAssertIfExpectedClean();
    }
    EX_END_CATCH(SwallowAllExceptions);

    IfFailThrow(hr);

    HangWorker(W("NGenDependencyWorkerHang"), W("NGenDependencyWorkerInsideHang"));
}

void Zapper::HangWorker(LPCWSTR hangKey, LPCWSTR insideHangKey)
{
    if (REGUTIL::GetConfigDWORD_DontUse_(hangKey, 0) != 1)
    {
        return;
    }

    RegKeyHolder hKey;
    if (WszRegCreateKeyEx(HKEY_LOCAL_MACHINE, FRAMEWORK_REGISTRY_KEY_W, 0,
        NULL, 0, KEY_WRITE, NULL, &hKey, NULL) == ERROR_SUCCESS)
    {
        DWORD dwValue = 1;
        WszRegSetValueEx(hKey, insideHangKey, 0, REG_DWORD,
            reinterpret_cast<BYTE *>(&dwValue), sizeof(dwValue));
    }

    while (true)
    {
        ClrSleepEx(1000, FALSE);
    }
}
#endif // FEATURE_FUSION

//
// Compile a module by name
//

HRESULT Zapper::Compile(LPCWSTR string, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    class Callback : public DomainCallback
    {
    public:
        Callback(Zapper *pZapper, LPCWSTR pAssemblyName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
        {
            this->pZapper         = pZapper;
            this->pAssemblyName   = pAssemblyName;
            this->pNativeImageSig = pNativeImageSig;
        }

        virtual void doCallback()
        {
            pZapper->CompileInCurrentDomain(pAssemblyName, pNativeImageSig);
        }

        Zapper* pZapper;
        LPCWSTR pAssemblyName;
        CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig;
    };

    HRESULT hr = S_OK;

    bool fMscorlib = false;
    LPCWSTR fileName = PathFindFileName(string);
    if (fileName != NULL && SString::_wcsicmp(fileName, g_pwBaseLibrary) == 0)
    {
        fMscorlib = true;
    }
#ifndef FEATURE_CORECLR
    else
    if (_wcsnicmp(fileName, W("mscorlib"), 8) == 0 && (wcslen(fileName) == 8 || fileName[8] == W(',')))
    {
        fMscorlib = true;
    }
#endif

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    if (fMscorlib)
    {
        //
        // Enable ningen by default for mscorlib to get identical images between ngen and crossgen
        //
        g_ningenState = 1;
    }
#endif

#if defined(CROSSGEN_COMPILE) || defined(FEATURE_CORECLR)
    if (fMscorlib)
    {
        //
        // Disallow use of native image to force a new native image generation for mscorlib
        //
        g_fAllowNativeImages = false;
    }
#endif

    // the errors in CreateCompilationDomain are fatal - propogate them up
    CreateCompilationDomain();

    EX_TRY
    {
        Callback callback(this, string, pNativeImageSig);
        InvokeDomainCallback(&callback);
    }
    EX_CATCH
    {
        // Print the error message

        Error(W("Error compiling %s: "), string);
        PrintErrorMessage(CORZAP_LOGLEVEL_ERROR, GET_EXCEPTION());
        Error(W("\n"));

        hr = GET_EXCEPTION()->GetHR();
        RetailAssertIfExpectedClean();
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

#ifndef FEATURE_CORECLR
    // Set the hard binding list.  This needs to be done early, before we attempt to use any
    // softbound native images, in order to ensure NGen determinism.
    SetAssemblyHardBindList();
#endif // !FEATURE_CORECLR

#ifdef FEATURE_FUSION
    // Set the context info for the current domain
    SetContextInfo(string);
#endif

    //
    // Load the assembly.
    //
    // "string" may be a path or assembly display name.
    // To decide, we see if it is the name of a valid file.
    //

    _ASSERTE(m_hAssembly == NULL);

    //without fusion, this has to be a file name
#ifdef FEATURE_FUSION
    DWORD attributes = WszGetFileAttributes(string);
    if (attributes == INVALID_FILE_ATTRIBUTES || ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
    {
        IfFailThrow(m_pEECompileInfo->LoadAssemblyByName(string, &m_hAssembly));

        /* @TODO: If this is the first input, check if it is an EXE
           using m_pEECompileInfo->GetAssemblyCodeBase(), and print a
           warning that its better to use the EXE file name on the
           command-line instead of the full assembly name, so that we
           will pick the right runtime version from its config file
           (if present). We could make this work even for full assembly name,
           but that does not currently work, and this will not be an
           issue once we require only one platform-runtime version
           on the machine
        */
        StackSString codeBase;
        m_pEECompileInfo->GetAssemblyCodeBase(m_hAssembly, codeBase);
        if (codeBase.GetCount() > 4)
        {
            SString::Iterator i = codeBase.End() - 4;
            if (codeBase.MatchCaseInsensitive(i, SL(".exe")))
                Warning(W("Specify the input as a .EXE for ngen to pick up the config-file\n"));
        }
    }
    else
#endif //FEATURE_FUSION
    {
        IfFailThrow(m_pEECompileInfo->LoadAssemblyByPath(string, FALSE /* fExplicitBindToNativeImage */, &m_hAssembly));
    }

#ifdef FEATURE_FUSION
    //
    // Skip the compilation if the assembly is up to date
    //
    if (CheckAssemblyUpToDate(m_hAssembly, pNativeImageSig))
    {
        Info(W("Assembly %s is up to date.\n"), string);
        goto Exit;
    }

    //
    // Try to install native image from repository
    //
    if (TryToInstallFromRepository(m_hAssembly, pNativeImageSig))
    {
        Success(W("Installed native image for assembly %s from repository.\n"), string);
        goto Exit;
    }

    if (m_pOpt->m_fRepositoryOnly)
    {
        Info(W("Unable to find native image for assembly %s in repository, skipping.\n"), string);
        goto Exit;
    }

    //
    // Testing aid
    //
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NativeImageRequire) != 0)
    {
        Error(W("Failed to find native image for assembly %s in repository.\n"), string);
        ThrowHR(E_FAIL);
    }
#endif //FEATURE_FUSION

    //
    // Compile the assembly
    //
    CompileAssembly(pNativeImageSig);

    goto Exit; // Avoid warning about unreferenced label

Exit:
    END_ENTRYPOINT_VOIDRET;

    return;
}

#ifdef FEATURE_FUSION
BOOL Zapper::CheckAssemblyUpToDate(CORINFO_ASSEMBLY_HANDLE hAssembly, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    WCHAR zapManifestPath[MAX_LONGPATH];
    DWORD cZapManifestPath = MAX_LONGPATH;

    if (!m_pEECompileInfo->CheckAssemblyZap(
        hAssembly,
        zapManifestPath, &cZapManifestPath))
        return FALSE;

    if (pNativeImageSig)
    {
        NonVMComHolder<INativeImageInstallInfo> pNIInstallInfo;

        IfFailThrow(GetAssemblyMDInternalImport(
            zapManifestPath,
            IID_INativeImageInstallInfo,
            (IUnknown **)&pNIInstallInfo));

        IfFailThrow(pNIInstallInfo->GetSignature(pNativeImageSig));
    }

    return TRUE;
}

BOOL Zapper::TryToInstallFromRepository(CORINFO_ASSEMBLY_HANDLE hAssembly, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    BOOL fHitMismatchedVersion = FALSE;
    BOOL fHitMismatchedDependencies = FALSE;

    if (!m_pOpt->m_repositoryDir || (m_pOpt->m_repositoryFlags & IgnoreRepository) != 0)
    {
        // No repository
        return FALSE;
    }

    StackSString strSimpleName;
    GetAssemblyName(m_pEECompileInfo, hAssembly, strSimpleName, ICorCompileInfo::GANF_Simple);

    // First see if the NI is available in a folder named "NGen" under the CLR location.
    // This folder is used by CBS to store build lab generated NIs.  Moving files out of
    // this folder might confuse CBS, so we hard link NIs from this folder into the NIC.
    WCHAR wszNGenPath[MAX_LONGPATH];
    DWORD dwNGenPathLen = COUNTOF(wszNGenPath);
    IfFailThrow(GetInternalSystemDirectory(wszNGenPath, &dwNGenPathLen));

    wcscat_s(wszNGenPath, COUNTOF(wszNGenPath), W("NativeImages"));
    if (TryToInstallFromRepositoryDir(StackSString(wszNGenPath), strSimpleName,
                                      pNativeImageSig, &fHitMismatchedVersion, &fHitMismatchedDependencies, TRUE))
    {
        return TRUE;
    }

    // If we are moving files from repository, first try to look for the native image in the
    // top-level directory of the repository. This is designed for scenarios where it is convenient
    // to put all native images in a flat directory. We don't support this with CopyFromRepository
    // switch, since it is tricky to figure out which files to copy.
    if ((m_pOpt->m_repositoryFlags & MoveFromRepository) != 0 &&
        TryToInstallFromRepositoryDir(StackSString(m_pOpt->m_repositoryDir), strSimpleName,
                                      pNativeImageSig, &fHitMismatchedVersion, &fHitMismatchedDependencies))
    {
        // Try to remove the repository directory in case it has become empty.
        // (Note that attempt to remove non-empty directory fails)
        WszRemoveDirectory(m_pOpt->m_repositoryDir);
        return TRUE;
    }

    // Copied from fusion\asmcache\cacheUtils.cpp
    // TODO: Clean this up
#define MAX_ZAP_NAME_LENGTH     20
#define ZAP_ABBR_END_CHAR       W('#')
    StackSString strSubDirName;
    if (strSimpleName.GetCount() > MAX_ZAP_NAME_LENGTH)
    {
        strSubDirName.Set(strSimpleName.GetUnicode(), MAX_ZAP_NAME_LENGTH-1);
        strSubDirName.Append(ZAP_ABBR_END_CHAR);           
    }
    else
        strSubDirName.Set(strSimpleName);

    StackSString strRepositorySubDir(StackSString(m_pOpt->m_repositoryDir), SL(W("\\")), strSubDirName);

    DWORD attributes = WszGetFileAttributes(strRepositorySubDir);
    if (attributes == INVALID_FILE_ATTRIBUTES || ((attributes & FILE_ATTRIBUTE_DIRECTORY) == 0))
    {
       return FALSE;
    }

    ClrDirectoryEnumerator de(strRepositorySubDir);
   
    //
    // Try to find a matching native image
    //
    while (de.Next())
    {
        StackSString strNativeImageDir;
        strNativeImageDir.Set(strRepositorySubDir, SL("\\"), de.GetFileName());

        if (TryToInstallFromRepositoryDir(strNativeImageDir, strSimpleName,
                                          pNativeImageSig, &fHitMismatchedVersion, &fHitMismatchedDependencies))
        {
            if (m_pOpt->m_repositoryFlags & MoveFromRepository)
            {
                // Close the iterator so that the directory can be deleted below
                de.Close();

                // Try to remove empty directories that are not needed anymore.
                // (Note that attempt to remove non-empty directory fails)
                if (WszRemoveDirectory(strNativeImageDir))
                {
                    if (WszRemoveDirectory(strRepositorySubDir))
                    {
                        WszRemoveDirectory(m_pOpt->m_repositoryDir);
                    }
                }
            }

            return TRUE;
        }
    }

    if (fHitMismatchedVersion)
    {
        ReportEventNGEN(EVENTLOG_WARNING_TYPE, NGEN_REPOSITORY, W("Version or flavor did not match with repository: %s"), strSimpleName.GetUnicode());
    }

    if (fHitMismatchedDependencies)
    {
        ReportEventNGEN(EVENTLOG_WARNING_TYPE, NGEN_REPOSITORY, W("Dependencies did not match with repository: %s"), strSimpleName.GetUnicode());
    }

    return FALSE;
}

BOOL Zapper::TryToInstallFromRepositoryDir(
    SString &strNativeImageDir, SString &strSimpleName,
    CORCOMPILE_NGEN_SIGNATURE *pNativeImageSig, BOOL *pfHitMismatchedVersion, BOOL *pfHitMismatchedDependencies, BOOL useHardLink)
{
    StackSString strNativeImageName;
    StackSString strNativeImagePath;

    // probe for both .exe and .dll
    static const LPCWSTR c_Suffixes[] = { W(".dll"), W(".exe"), W(".ni.dll"), W(".ni.exe") };

    int suffix;
    for (suffix = 0; suffix < NumItems(c_Suffixes); suffix++)
    {
        strNativeImageName.Set(strSimpleName, SL(c_Suffixes[suffix]));
        strNativeImagePath.Set(strNativeImageDir, SL("\\"), strNativeImageName);

        if (WszGetFileAttributes(strNativeImagePath) != INVALID_FILE_ATTRIBUTES)
        {
            break;
        }
    }
    if (suffix == NumItems(c_Suffixes))
    {
        // No matching file
        return FALSE;
    }

    // Make sure the native image is unmapped before we try to install it
    {
        HModuleHolder hMod(::WszLoadLibrary(strNativeImagePath));
        if (hMod.IsNull())
        {
            // Corrupted image or something
            return FALSE;
        }

        PEDecoder pedecoder(hMod);

        if (!pedecoder.CheckNativeHeader())
        {
            // Corrupted image
            return FALSE;
        }

        class LoggableNativeImage : public LoggableAssembly
        {
            LPCWSTR m_lpszNativeImage;

        public:
            LoggableNativeImage(LPCWSTR lpszNativeImage)
                : m_lpszNativeImage(lpszNativeImage)
            {
            }

            virtual SString DisplayString() { return m_lpszNativeImage; }
#ifdef FEATURE_FUSION
            virtual IAssemblyName*  FusionAssemblyName() { return NULL; }
            virtual IFusionBindLog* FusionBindLog() { return NULL; }
#endif // FEATURE_FUSION
        }
        loggableNativeImage(strNativeImagePath);

        // Does the version info of the native image match what we are looking for?
        CORCOMPILE_VERSION_INFO *pVersionInfo = pedecoder.GetNativeVersionInfo();
        if (!RuntimeVerifyNativeImageVersion(pVersionInfo, &loggableNativeImage) ||
            !RuntimeVerifyNativeImageFlavor(pVersionInfo, &loggableNativeImage))
        {
            // Version info does not match
            *pfHitMismatchedVersion = TRUE;
            return FALSE;
        }

        COUNT_T cDependencies;
        CORCOMPILE_DEPENDENCY *pDependencies = pedecoder.GetNativeDependencies(&cDependencies);

        COUNT_T cMeta;
        const void *pMeta = pedecoder.GetNativeManifestMetadata(&cMeta);

        NonVMComHolder<IMDInternalImport> pAssemblyImport;

        IfFailThrow(GetMetaDataInternalInterface((void *) pMeta,
                                                 cMeta,
                                                 ofRead,
                                                 IID_IMDInternalImport,
                                                 (void **) &pAssemblyImport));

        if (!VerifyDependencies(pAssemblyImport, pDependencies, cDependencies))
        {
            // Dependencies does not match
            *pfHitMismatchedDependencies = TRUE;
            return FALSE;
        }
    }

    if (m_pOpt->m_repositoryFlags & MoveFromRepository && !useHardLink)
    {
        // Move files to save I/O bandwidth
        InstallFromRepository(strNativeImagePath.GetUnicode(), pNativeImageSig);
    }
    else
    {
        // Copy files
        CopyAndInstallFromRepository(strNativeImageDir.GetUnicode(), strNativeImageName.GetUnicode(), pNativeImageSig, useHardLink);
    }

    ReportEventNGEN(EVENTLOG_SUCCESS, NGEN_REPOSITORY, W("Installed from repository: %s"), strSimpleName.GetUnicode());
    return TRUE;
}

void Zapper::InstallFromRepository(LPCWSTR lpszNativeImage, 
                                   CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    //
    // Get the zap string.
    //
    HRESULT hr = S_OK;

    NonVMComHolder<IAssemblyName> pName;
    NonVMComHolder<IAssemblyLocation> pAssemblyLocation;

    ReleaseHolder<IBindContext> pBindCtx;
    if (FAILED(hr = m_pDomain->GetIBindContext(&pBindCtx)))
    {
        Error(W("Failed to get binding context.\n"));
        ThrowHR(hr);
    }

    HRESULT hrInstallCustomAssembly = InstallNativeAssembly(lpszNativeImage, INVALID_HANDLE_VALUE, m_pOpt->m_zapSet, pBindCtx, &pName, &pAssemblyLocation);
    if (FAILED(hrInstallCustomAssembly))
    {
        Error(W("Failed to install native image %s from repository.\n"), lpszNativeImage);
        ThrowHR(hrInstallCustomAssembly);
    }

    // TODO: It would be nice to verify that the native image works by calling CheckAssemblyUpToDate.
    // Unfortunately, it does not work since we have loaded the non-ngened image in this appdomain 
    // already in CompileInCurrentDomain. We would need to create a new appdomain for the verification. 

    //
    // Print a success message
    //
    if (!m_pOpt->m_silent)
    {
        PrintFusionCacheEntry(LogLevel_Info, pName);
    }

    WCHAR zapManifestPath[MAX_LONGPATH];
    DWORD cPath = MAX_LONGPATH;
    IfFailThrow(pAssemblyLocation->GetPath(zapManifestPath, &cPath));

    if (pNativeImageSig)
    {
        NonVMComHolder<INativeImageInstallInfo> pNIInstallInfo;

        IfFailThrow(GetAssemblyMDInternalImport(
            zapManifestPath,
            IID_INativeImageInstallInfo,
            (IUnknown **)&pNIInstallInfo));

        IfFailThrow(pNIInstallInfo->GetSignature(pNativeImageSig));
    }
}

void Zapper::CleanDirectory(LPCWSTR path)
{
    // Handle the case when we are given file instead of directory
    DWORD dwAttributes = WszGetFileAttributes(path);
    if (dwAttributes == INVALID_FILE_ATTRIBUTES)
    {
        // Directory does not exist
        return;
    }

    if (!(dwAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        if (dwAttributes & FILE_ATTRIBUTE_READONLY)
            WszSetFileAttributes(path, dwAttributes&~FILE_ATTRIBUTE_READONLY);

        if (!WszDeleteFile(path))
        {
            Warning(W("Cannot delete file %s\n"), path);
            ThrowLastError();
        }
        return;
    }

    {
        ClrDirectoryEnumerator de(path);

        while (de.Next())
        {
            StackSString fullName;
            fullName.Set(path, W("\\"), de.GetFileName());

            if (de.GetFileAttributes() & FILE_ATTRIBUTE_DIRECTORY)
            {
                CleanDirectory(fullName);
            }
            else
            {
                if (de.GetFileAttributes() & FILE_ATTRIBUTE_READONLY)
                    WszSetFileAttributes(fullName, de.GetFileAttributes()&~FILE_ATTRIBUTE_READONLY);

                if (!WszDeleteFile(fullName))
                {
                    Warning(W("Cannot delete file %s\n"), fullName.GetUnicode());
                    ThrowLastError();
                }
            }
        }
    }

    if (!WszRemoveDirectory(path))
    {
        Warning(W("Cannot remove directory %s\n"), path);
        ThrowLastError();
    }
}

void Zapper::TryCleanDirectory(LPCWSTR path)
{
    EX_TRY
    {
        CleanDirectory(path);
    }
    EX_SWALLOW_NONTERMINAL;
}

//------------------------------------------------------------------------------

// static
void Zapper::TryCleanDirectory(Zapper * pZapper)
{
    // @CONSIDER: If this fails, block for some time, and try again.
    // This will give more time for programs like Anti-virus software
    // to release the file handle.
    pZapper->TryCleanDirectory(pZapper->m_outputPath);
}

typedef Wrapper<Zapper*, DoNothing<Zapper*>, Zapper::TryCleanDirectory, NULL>
    TryCleanDirectoryHolder;

//------------------------------------------------------------------------------
// Sets Zapper::m_outputPath to the folder where we should create the
// ngen images.
//------------------------------------------------------------------------------

void Zapper::GetOutputFolder()
{
    /* We create a temporary folder in the NativeImageCache (NIC) instead of using
       WszGetTempPath(). This is because WszGetTempPath() is a private folder
       for the current user. Files created in there will have ACLs allowing
       accesses only to the current user. Later InstallCustomAssembly()
       will move the files to the NIC preserving the security attributes.
       Now other users cannot use the ngen images, which is bad.
    */
    WCHAR tempFolder[MAX_LONGPATH];
    DWORD tempFolderLen = NumItems(tempFolder);
    IfFailThrow(GetCachePath(ASM_CACHE_ZAP, tempFolder, &tempFolderLen));

    // Create the folder "NIC"

    IfFailThrow(clr::fs::Dir::CreateRecursively(tempFolder));

    // Create the folder "NIC\Temp"

    StackSString tempPath(tempFolder);
    tempPath += W("\\Temp");
    if (!WszCreateDirectory(tempPath, NULL))
    {
        if (GetLastError() != ERROR_ALREADY_EXISTS)
            ThrowLastError();
    }

    // Create the folder "NIC\Temp\P-N", where P is the current process ID, and NN is a serial number. 
    // Start with N=0.  If that directory name is already in use, clean up that directory (it can't be in
    // active use because process ID is unique), increment N, and try again.  Give up if N gets too large.
    for (DWORD n = 0; ; n++)
    {
        m_outputPath.Printf(W("%s\\%x-%x"), (LPCWSTR)tempPath, GetCurrentProcessId(), n);
        if (WszCreateDirectory(m_outputPath, NULL))
            break;

        if (GetLastError() != ERROR_ALREADY_EXISTS)
            ThrowLastError();

        TryCleanDirectory(m_outputPath);

        if (n >= 255)
        {
            Error(W("Unable to create working directory"));
            ThrowHR(E_FAIL);
        }
    }
}

//------------------------------------------------------------------------------

void Zapper::CopyAndInstallFromRepository(LPCWSTR lpszNativeImageDir,
                                          LPCWSTR lpszNativeImageName, 
                                          CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig,
                                          BOOL useHardLink)
{
    GetOutputFolder();

    // Note that we do not want to fail if we cannot clean up the TEMP files.
    // We have seen many issues where the Indexing service or AntiVirus software
    // open the temporary files, and seem to hold onto them. We have been
    // told that if ngen completes too fast, these other softwares may
    // not be able to process the file fast enough, and may close the file
    // sometime after we have tried to delete it.
    TryCleanDirectoryHolder outputPathHolder(this);

    StackSString strTempNativeImage;
    
    //local variable fixes gcc overload resolution.
    SString literalPathSep(SString::Literal, "\\");
    strTempNativeImage.Set(m_outputPath, literalPathSep, lpszNativeImageName);

    if (useHardLink)
    {
        // Don't support multi-module assemblies. The useHardLink flag is used in
        // scenarios where the source directory has multiple native images, and we
        // want to avoid the need to figure out which files are needed.
        StackSString strSource(lpszNativeImageDir, literalPathSep, lpszNativeImageName);

        // Try to create hard link first. If that fails, try again with copy.
        if (!WszCreateHardLink(strTempNativeImage.GetUnicode(), strSource.GetUnicode(), NULL) &&
            !WszCopyFile(strSource.GetUnicode(), strTempNativeImage.GetUnicode(), TRUE))
        {
            ThrowLastError();
        }
    }
    else
    {
        // Copy everything in the directory over. Blindly copying everything over
        // saves us from dealing with external modules.
        CopyDirectory(lpszNativeImageDir, m_outputPath);
    }

    InstallFromRepository(strTempNativeImage.GetUnicode(), pNativeImageSig);
}

//------------------------------------------------------------------------------

void Zapper::CopyDirectory(LPCWSTR srcPath, LPCWSTR dstPath)
{
    ClrDirectoryEnumerator de(srcPath);

    while (de.Next())
    {
        StackSString srcFile;
        SString literalPathSep(SString::Literal, "\\");
        srcFile.Set(srcPath, literalPathSep, de.GetFileName());
        
        StackSString dstFile;
        dstFile.Set(dstPath, literalPathSep, de.GetFileName());
        
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NGenCopyFromRepository_SetCachedSigningLevel) != 0)
        {
            // The user wants the destination file to be vouched for. It would be a security hole to copy the file
            // and then actually vouch for it. So we create a hard link instead. If the original file has the EA,
            // the new link will also see the EA as it points to the same physical file. Note that the argument
            // order is different between CreateHardLink and CopyFile.
            if (WszCreateHardLink(dstFile.GetUnicode(), srcFile.GetUnicode(), NULL))
            {
                continue;
            }

            // If creation of hard link failed, issue an warning and fall back to copying.
            HRESULT hr = HRESULT_FROM_WIN32(GetLastError());
            _ASSERTE(FAILED(hr));
            if (IsExeOrDllOrWinMD(srcFile.GetUnicode()))
            {   // Print the warning for executables for easier troubleshooting
                Warning(W("CreateHardLink failed with HRESULT 0x%08x for file %s\n"), hr, srcFile.GetUnicode());
            }
        }

        if (!WszCopyFile(srcFile.GetUnicode(), dstFile.GetUnicode(), TRUE))
            ThrowLastError();
    }
}
#endif // FEATURE_FUSION

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
    m_pOpt->m_compilerFlags &= ~(CORJIT_FLG_DEBUG_INFO
                                 | CORJIT_FLG_DEBUG_CODE
                                 | CORJIT_FLG_PROF_ENTERLEAVE
                                 | CORJIT_FLG_PROF_NO_PINVOKE_INLINE);

    // We track debug info all the time in the ngen image
    m_pOpt->m_compilerFlags |= CORJIT_FLG_DEBUG_INFO;

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_DEBUGGING)
    {
        m_pOpt->m_compilerFlags |= (CORJIT_FLG_DEBUG_INFO|
                                    CORJIT_FLG_DEBUG_CODE);
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROFILING)
    {
        m_pOpt->m_compilerFlags |= CORJIT_FLG_PROF_ENTERLEAVE | CORJIT_FLG_PROF_NO_PINVOKE_INLINE;
        m_pOpt->m_ngenProfileImage = true;
    }

    if (pVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_PROF_INSTRUMENTING)
        m_pOpt->m_compilerFlags |= CORJIT_FLG_BBINSTR;

#if defined(_TARGET_X86_)

    // @TODO: This is a copy of SetCpuInfo() in vm\codeman.cpp. Unify the implementaion

    switch (CPU_X86_FAMILY(pVersionInfo->cpuInfo.dwCPUType))
    {
    case CPU_X86_PENTIUM_4:
        m_pOpt->m_compilerFlags |= CORJIT_FLG_TARGET_P4;
        break;

    default:
        break;
    }

    if (CPU_X86_USE_CMOV(pVersionInfo->cpuInfo.dwFeatures))
    {
        m_pOpt->m_compilerFlags |= CORJIT_FLG_USE_CMOV |
                                   CORJIT_FLG_USE_FCOMI;
    }

    if (CPU_X86_USE_SSE2(pVersionInfo->cpuInfo.dwFeatures))
    {
        m_pOpt->m_compilerFlags |= CORJIT_FLG_USE_SSE2;
    }

#endif // _TARGET_X86_

    if (   (m_pOpt->m_compilerFlags & CORJIT_FLG_DEBUG_INFO)
        && (m_pOpt->m_compilerFlags & CORJIT_FLG_DEBUG_CODE)
        && (m_pOpt->m_compilerFlags & CORJIT_FLG_PROF_ENTERLEAVE))
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
#ifdef FEATURE_FUSION
    if (!FusionBind::VerifyBindingStringW(wszAssemblyName))
    {
        Error(W("Error: Assembly name \"%s\" contains path separator and/or extension.\n"), wszAssemblyName); // VLDTR_E_AS_BADNAME
        ThrowHR(HRESULT_FROM_WIN32(ERROR_INVALID_NAME));
    }
#endif //FEATURE_FUSION

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

#ifdef FEATURE_FUSION
    GetOutputFolder();

    // Note that we do not want to fail if we cannot clean up the TEMP files.
    // We have seen many issues where the Indexing service or AntiVirus software
    // open the temporary files, and seem to hold onto them. We have been
    // told that if ngen completes too fast, these other softwares may
    // not be able to process the file fast enough, and may close the file
    // sometime after we have tried to delete it.
    TryCleanDirectoryHolder outputPathHolder(this);
#else // FEATURE_FUSION
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
#endif // FEATURE_FUSION

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

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES 
        CompileNonManifestModules(hashAlgId, hFiles);
#endif // FEATURE_MULTIMODULE_ASSEMBLIES 

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
#ifdef FEATURE_FUSION
        strNativeImagePath.Set(m_outputPath, SL(DIRECTORY_SEPARATOR_STR_W), strAssemblyName, 
            pAssemblyModule->m_ModuleDecoder.IsDll() ? SL(W(".dll")) : SL(W(".exe")));
#else // FEATURE_FUSION
        strNativeImagePath = GetOutputFileName();

        if (strNativeImagePath.IsEmpty())
        {
            strNativeImagePath.Set(m_outputPath, SL(DIRECTORY_SEPARATOR_STR_W), strAssemblyName, 
                pAssemblyModule->m_ModuleDecoder.IsDll() ? SL(W(".ni.dll")) : SL(W(".ni.exe")));
        }
#endif // FEATURE_FUSION

        pAssemblyModule->SetPdbFileName(SString(strAssemblyName, SL(W(".ni.pdb"))));

        hFile = pAssemblyModule->SaveImage(strNativeImagePath.GetUnicode(), pNativeImageSig);
    }

    // Throw away the assembly if we have hit fatal error during compilation
    if (FAILED(g_hrFatalError))
        ThrowHR(g_hrFatalError);

#ifdef FEATURE_FUSION
    InstallCompiledAssembly(strAssemblyName.GetUnicode(), strNativeImagePath.GetUnicode(), hFile, hFiles);
    
    // 
    // Once we return from InstallCompiledAssembly, we're in a window where the native image file
    // has been placed into the NIC, but none of the Ngen rootstore data structures have been set
    // to indicate there is a native image for this assembly.  Therefore, we MUST return to the
    // Ngen process now without throwing an exception.
    // If you need to add code below here before returning it cannot throw an exception or return 
    // failure.  If it does, you must make sure the native image get uninstalled from disk before 
    // returning so we do not leak the NI file.
    // 
    return;

#else // FEATURE_FUSION

    // Close the file
    CloseHandle(hFile);
    for (SArray<HANDLE>::Iterator i = hFiles.Begin(); i != hFiles.End(); ++i)
    {
        CloseHandle(*i);
    }

    if (!m_pOpt->m_silent)
    {
        GetSvcLogger()->Printf(W("Native image %s generated successfully.\n"), strNativeImagePath.GetUnicode());
    }
#endif // FEATURE_FUSION

}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES 
void * Zapper::GetMapViewOfFile(
        HANDLE file,
        DWORD * pdwFileLen)
{
    //
    // Create a file-mapping to read the file contents
    //

    DWORD dwFileLen = SafeGetFileSize(file, 0);
    if (dwFileLen == INVALID_FILE_SIZE)
        ThrowHR(HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY));

    HandleHolder mapFile(WszCreateFileMapping(
                    file,                       // hFile
                    NULL,                       // lpAttributes
                    PAGE_READONLY,              // flProtect
                    0,                          // dwMaximumSizeHigh
                    0,                          // dwMaximumSizeLow
                    NULL));                      // lpName (of the mapping object)
    if (mapFile == NULL)
        ThrowLastError();

    MapViewHolder mapView(MapViewOfFile(mapFile,      // hFileMappingObject,
                                       FILE_MAP_READ,   // dwDesiredAccess,
                                       0,               // dwFileOffsetHigh,
                                       0,               // dwFileOffsetLow,
                                       0));              // dwNumberOfBytesToMap
    if (mapView == NULL)
        ThrowLastError();

    *pdwFileLen = dwFileLen;
    return mapView.Extract();
}

void Zapper::ComputeHashValue(HANDLE hFile, int iHashAlg,
                              BYTE **ppHashValue, DWORD *pcHashValue)
{
    
    DWORD dwFileLen;
    MapViewHolder mapView(GetMapViewOfFile(hFile, &dwFileLen));

    BYTE * pbBuffer = (BYTE *) mapView.GetValue();

    //
    // Hash the file
    //

    DWORD       dwCount = sizeof(DWORD);
    DWORD       cbHashValue = 0;
    NewArrayHolder<BYTE>  pbHashValue;

    HandleCSPHolder hProv;
    HandleHashHolder hHash;

    if ((!WszCryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT)) ||
        (!CryptCreateHash(hProv, iHashAlg, 0, 0, &hHash)) ||
        (!CryptHashData(hHash, pbBuffer, dwFileLen, 0)) ||
        (!CryptGetHashParam(hHash, HP_HASHSIZE, (BYTE *) &cbHashValue, &dwCount, 0)))
        ThrowLastError();

    pbHashValue = new BYTE[cbHashValue];

    if(!CryptGetHashParam(hHash, HP_HASHVAL, pbHashValue, &cbHashValue, 0))
        ThrowLastError();

    *ppHashValue = pbHashValue.Extract();
    *pcHashValue = cbHashValue;
}

void Zapper::CompileNonManifestModules(ULONG hashAlgId, SArray<HANDLE> &hFiles)
{
    //
    // Iterate all non-manifest modules in the assembly, and compile them
    //

    IMDInternalImport * pMDImport = m_pAssemblyImport;

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

        SString strFileName(SString::Utf8, szName);
        LPCWSTR wszFileName = strFileName.GetUnicode(); 

        //
        // We want to compile this file.
        //

        CORINFO_MODULE_HANDLE hModule;

        IfFailThrow(m_pEECompileInfo->LoadAssemblyModule(m_hAssembly,
                                                         tkFile, &hModule));

        SString strNativeImagePath;
        HANDLE hFile;

        {
            NewHolder<ZapImage> pModule;
            pModule = CompileModule(hModule, NULL);

            {
                SString strFileNameWithoutExt(strFileName);
                SString::CIterator fileNameIterator = strFileNameWithoutExt.End();
                if (!strFileNameWithoutExt.FindBack(fileNameIterator, '.'))
                    ThrowHR(E_FAIL);
                strFileNameWithoutExt.Truncate(fileNameIterator.ConstCast());

                pModule->SetPdbFileName(SString(strFileNameWithoutExt, SL(W(".ni.pdb"))));

                strNativeImagePath.Set(m_outputPath, SL(W("\\")), wszFileName);

                hFile = pModule->SaveImage(strNativeImagePath.GetUnicode(), NULL);
                hFiles.Append(hFile);
            }
        }

        {
            NewArrayHolder<BYTE> pbHashValue;
            DWORD cbHashValue;
            ComputeHashValue(hFile, hashAlgId, &pbHashValue, &cbHashValue);

            mdFile token;
            IfFailThrow(m_pAssemblyEmit->DefineFile(wszFileName, pbHashValue, cbHashValue, 0, &token));
        }
    }
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES 

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

    if (IsReadyToRunCompilation())
    {
        return module.Extract();
    }

    //
    // Link preloaded module.
    //

    Info(W("Linking preloaded input file %s\n"), module->m_pModuleFileName);

    module->LinkPreload();

    return module.Extract();
}

#ifdef FEATURE_FUSION
void Zapper::InstallCompiledAssembly(LPCWSTR szAssemblyName, LPCWSTR szNativeImagePath, HANDLE hFile, SArray<HANDLE> &hFiles)
{
    HRESULT hr = S_OK;

    if ((m_pOpt->m_repositoryFlags & CopyToRepository) && m_pOpt->m_repositoryDir)
    {
        //
        // Copy the native images back to repository. We require RepositoryDir itself to exists.
        //

        StackSString strSimpleName(szAssemblyName);
        StackSString strSubDirName;

        // Get subdirectory name from the assembly name
        if (strSimpleName.GetCount() > MAX_ZAP_NAME_LENGTH)
        {
            strSubDirName.Set(strSimpleName.GetUnicode(), MAX_ZAP_NAME_LENGTH-1);
            strSubDirName.Append(ZAP_ABBR_END_CHAR);           
        }
        else
            strSubDirName.Set(strSimpleName);

        StackSString destPath;
        destPath.Set(m_pOpt->m_repositoryDir, SL(W("\\")), strSubDirName);

        if (!WszCreateDirectory(destPath, NULL))
        {
            if (GetLastError() != ERROR_ALREADY_EXISTS)
                ThrowLastError();
        }

        // Get unique subdirectory name from native image sig
        GUID unique;
        IfFailThrow(CoCreateGuid(&unique));
        static_assert_no_msg(sizeof(unique) == 16);
        destPath.AppendPrintf(W("\\%08x%08x%08x%08x"), 
            ((LONG*)&unique)[0], ((LONG*)&unique)[1],
            ((LONG*)&unique)[2], ((LONG*)&unique)[3]);

        if (!WszCreateDirectory(destPath, NULL))
            ThrowLastError();

        CopyDirectory(m_outputPath, destPath);
    }

    NonVMComHolder<IAssemblyName> pName;
    NonVMComHolder<IAssemblyLocation> pAssemblyLocation;

    // If the NGenCompileWorkerHang key is set, we want to loop forever.  This helps testing
    // of termination of compilation workers on fast machines (where the compilation can finish
    // before the worker has been terminated).
    HangWorker(W("NGenCompileWorkerHang"), W("NGenCompileWorkerInsideHang"));

    ReleaseHolder<IBindContext> pBindCtx;
    if (FAILED(hr = m_pDomain->GetIBindContext(&pBindCtx)))
    {
        Error(W("Failed to get binding context.\n"));
        ThrowHR(hr);
    }
    if (FAILED(hr = InstallNativeAssembly(szNativeImagePath, hFile, m_pOpt->m_zapSet, pBindCtx, &pName, &pAssemblyLocation)))
    {
        Warning(W("Failed to install image to native image cache.\n"));
        ThrowHR(hr);
    }
    
    //
    // The native image is now installed in the NIC.  Any exception thrown before the end of this method
    // will result in the native image being leaked but the rootstore being cleaned up, orphaning the NI.
    //
    EX_TRY
    {
        // Ignore errors if they happen
        (void)m_pEECompileInfo->SetCachedSigningLevel(hFile, hFiles.GetElements(), hFiles.GetCount());
    
        CloseHandle(hFile);
        for (SArray<HANDLE>::Iterator i = hFiles.Begin(); i != hFiles.End(); ++i)
        {
            CloseHandle(*i);
        }

        //
        // Print a success message
        //

        if (!m_pOpt->m_silent)
        {
            PrintFusionCacheEntry(LogLevel_Info, pName);
        }
    }
    EX_CATCH
    {
        // Uninstall the native image and rethrow. Ignore all errors from UninstallNativeAssembly; we 
        // tried our best not to leak a native image and want to surface to original reason for 
        // failing anyway.
        (void)UninstallNativeAssembly(pName, GetSvcLogger()->GetSvcLogger());
        EX_RETHROW;
    }
    EX_END_CATCH_UNREACHABLE;
} // Zapper::InstallCompiledAssembly
#endif // FEATURE_FUSION

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

#if defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)
void Zapper::SetCLRJITPath(LPCWSTR pwszCLRJITPath)
{
    m_CLRJITPath.Set(pwszCLRJITPath);
}

void Zapper::SetDontLoadJit()
{
    m_fDontLoadJit = true;
}
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_MERGE_JIT_AND_ENGINE)

#if defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)
void Zapper::SetDiasymreaderPath(LPCWSTR pwzDiasymreaderPath)
{
    m_DiasymreaderPath.Set(pwzDiasymreaderPath);
}
#endif // defined(FEATURE_CORECLR) && !defined(NO_NGENPDB)

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)

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

void Zapper::SetForceFullTrust(bool val)
{
    m_fForceFullTrust = val;
}

#endif // FEATURE_CORECLR || CROSSGEN_COMPILE


void Zapper::SetOutputFilename(LPCWSTR pwzOutputFilename)
{
    m_outputFilename.Set(pwzOutputFilename);
}


SString Zapper::GetOutputFileName()
{
    return m_outputFilename;
}

void Zapper::SetLegacyMode()
{
    LIMITED_METHOD_CONTRACT;

    m_pOpt->m_legacyMode = true;
}

