// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/**********************************************************************
svcworker.cpp -- logic for the runtime implementation of the native 
image service.

Overview:  the runtime implementation is accessed via a local COM
server implemented in ngen.exe.  That server is simply a stub that
loads the most recent runtime and calls into the actual implementation
in this file.  There are three entrypoints in mscorwks.dll that
are called by the local service in ngen.exe:

NGenWorkerRegisterServer -- called to register ngen.exe as the current
  COM server for CLSID_CorSvcWorker
NGenWorkerUnregisterServer -- unregister ngen.exe as the current COM
  server for CLSID_CorSvcWorker
NGenWorkerEmbedding() -- called when COM invoked the COM server with
  the "-Embedding" flag.  Implements the logic for registering the class
  factory for CLSID_CorSvcWorker and controlling the lifetime of the
  COM server.
**********************************************************************/

#include "common.h"

#ifdef FEATURE_FUSION
#include "binderngen.h"
#endif

#ifdef FEATURE_APPX
#include "AppXUtil.h"
#endif

ILocalServerLifetime *g_pLocalServerLifetime = NULL;

SvcLogger::SvcLogger()
    : pss(NULL),
      pCorSvcLogger(NULL)
{
}

inline void SvcLogger::CheckInit()
{
    if(pss == NULL)
    {
        StackSString* psstemp = new StackSString();
        StackSString* pssOrig = InterlockedCompareExchangeT(&pss, psstemp, NULL); 
        if(pssOrig)
            delete psstemp;  
    }        
}

SvcLogger::~SvcLogger()
{
    if (pCorSvcLogger)
    {
//        pCorSvcLogger->Release();
        pCorSvcLogger = NULL;
    }
    if (pss)
        delete pss;
}

void SvcLogger::ReleaseLogger()
{
    if (pCorSvcLogger)
    {
        pCorSvcLogger->Release();
        pCorSvcLogger = NULL;
    }
}

void SvcLogger::Printf(const CHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode() );
    }
}

void SvcLogger::SvcPrintf(const CHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    LogHelper(s);
}

void SvcLogger::Printf(const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode() );
    }
}

void SvcLogger::Printf(CorSvcLogLevel logLevel, const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s, logLevel);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode());
    }
}

void SvcLogger::SvcPrintf(const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    LogHelper(s);
}

void SvcLogger::Log(const WCHAR *message, CorSvcLogLevel logLevel)
{
    LogHelper(StackSString(message), logLevel);
}

void SvcLogger::LogHelper(SString s, CorSvcLogLevel logLevel)
{
    CheckInit(); 
    pss->Append(s);

    // Does s contain a newline?
    SString::Iterator i = pss->Begin();
    if (pss->FindASCII(i, "\n"))
    {
        if (pCorSvcLogger)
        {
            BSTRHolder bstrHolder(::SysAllocString(pss->GetUnicode()));
            // Can't use the IfFailThrow macro here because in checked
            // builds that macros will try to log an error message
            // that will recursively return to this method.
            HRESULT hr = pCorSvcLogger->Log(logLevel, bstrHolder);
            if (FAILED(hr))
                ThrowHR(hr);
        }
        pss->Clear();
    }
}

void SvcLogger::SetSvcLogger(ICorSvcLogger *pCorSvcLoggerArg)
{
    ReleaseLogger();
    this->pCorSvcLogger = pCorSvcLoggerArg;
    if (pCorSvcLoggerArg)
    {
        pCorSvcLogger->AddRef();
    }
}

BOOL SvcLogger::HasSvcLogger()
{
    return (this->pCorSvcLogger != NULL);
}

ICorSvcLogger* SvcLogger::GetSvcLogger()
{
    return pCorSvcLogger;
}

#ifndef FEATURE_CORECLR

void InitNGenOptions(NGenOptions *ngo,
                     NGenPrivateAttributes ngenPrivateAttributes,                     
                     OptimizationScenario optScenario = ScenarioDefault,
                     LPCWSTR lpszRepositoryDir = NULL, RepositoryFlags repositoryFlags = RepositoryDefault)
{
    ULONG_PTR pScenario = (ULONG_PTR) optScenario;

    ngo->dwSize = sizeof(NGenOptions);
        
    // V1
    //
    ngo->fDebug      = (pScenario & ScenarioDebug)                ? true : false;
    ngo->fDebugOpt   = false;
    ngo->fProf       = (pScenario & ScenarioProfile)              ? true : false;
    ngo->fSilent = false;
    ngo->lpszExecutableFileName = NULL;

    // V2 (Whidbey) 
    //
    ngo->fInstrument = (pScenario & ScenarioTuningDataCollection) ? true : false;
    ngo->fWholeProgram = false;
    ngo->fProfInfo   = (pScenario & ScenarioProfileInfo)          ? true : false;

    ngo->lpszRepositoryDir = lpszRepositoryDir;
    ngo->repositoryFlags = repositoryFlags;

    ngo->dtRequested = DT_NIL;
    ngo->lpszDebugDir = NULL;

    ngo->fNoInstall  = false;
    ngo->fEmitFixups = false;
    ngo->fFatHeaders = false;
    ngo->fVerbose    = false;
    
    // This should be a value from the StatOptions enumeration
    ngo->uStats = ngenPrivateAttributes.ZapStats;
    ngo->dtRequested = (ngenPrivateAttributes.Flags & DbgTypePdb) ? DT_PDB : DT_NIL;
    ngo->lpszDebugDir = ngenPrivateAttributes.DbgDir;


    // V4 
    //
    ngo->fNgenLastRetry = (pScenario & ScenarioNgenLastRetry)     ? true : false;

    // V4.5
    ngo->fAutoNGen = (pScenario & ScenarioAutoNGen)               ? true : false;

    // Blue
    ngo->fRepositoryOnly = (pScenario & ScenarioRepositoryOnly)   ? true : false;
}

#endif // !FEATURE_CORECLR

namespace
{
    SvcLogger *g_SvcLogger = NULL;
}

// As NGen is currently single-threaded, this function is intentionally not thread safe.
// If necessary, change it into an interlocked function.
SvcLogger *GetSvcLogger()
{
    if (g_SvcLogger == NULL)
    {
        g_SvcLogger = new SvcLogger();
    }
    return g_SvcLogger;
}

BOOL HasSvcLogger()
{
    if (g_SvcLogger != NULL)
    {
        return g_SvcLogger->HasSvcLogger();
    }
    return FALSE;
}

#ifdef CROSSGEN_COMPILE
void SetSvcLogger(ICorSvcLogger *pCorSvcLogger)
{
    GetSvcLogger()->SetSvcLogger(pCorSvcLogger);
}
#endif

#ifndef FEATURE_CORECLR

//*****************************************************************************
// ICorSvcDependencies is used to enumerate the dependencies of an
// IL image.  It is used by the native image service.
//*****************************************************************************[
class CCorSvcDependencies : public ICorSvcDependencies
{
public:
    CCorSvcDependencies()
    {
        _cRef = 0;
        zapper = NULL;
       
        g_pLocalServerLifetime->AddRefServerProcess();
    }

    ~CCorSvcDependencies() 
    {
        if (zapper != NULL)
        {
            delete zapper;
        }

        g_pLocalServerLifetime->ReleaseServerProcess();
    }

    void Initialize(BSTR pApplicationName, OptimizationScenario scenario)
    {
        NGenOptions opt = {0};
        NGenPrivateAttributesClass ngenPrivateAttributesClass;
        InitNGenOptions(&opt, ngenPrivateAttributesClass, scenario);
        opt.lpszExecutableFileName = pApplicationName;
        zapper = Zapper::NewZapper(&opt, true);
        zapper->CreateDependenciesLookupDomain();
    }

    STDMETHOD (GetAssemblyDependencies)(
        BSTR pAssemblyName, 
        SAFEARRAY **pDependencies,
        DWORD *assemblyNGenSetting,
        BSTR *pNativeImageIdentity,
        BSTR *pAssemblyDisplayName,
        SAFEARRAY **pDependencyLoadSetting,
        SAFEARRAY **pDependencyNGenSetting
    )
    {
        SO_NOT_MAINLINE_FUNCTION;
        
        _ASSERTE(zapper != NULL);
        _ASSERTE(pNativeImageIdentity);

        HRESULT hr = S_OK;
        EX_TRY
        {
            GUID nativeImageSign = INVALID_NGEN_SIGNATURE;
            zapper->ComputeDependencies(pAssemblyName, &nativeImageSign);
            
            BSTRHolder displayNameHolder(::SysAllocString(zapper->m_assemblyDependencies.GetDisplayName()));
               
            *pDependencies          = zapper->m_assemblyDependencies.GetSAFEARRAY();
            *assemblyNGenSetting    = zapper->m_assemblyDependencies.GetNGenHint();
            *pDependencyLoadSetting = zapper->m_assemblyDependencies.GetLoadHintSAFEARRAY();
            *pDependencyNGenSetting = zapper->m_assemblyDependencies.GetNGenHintSAFEARRAY();
        
            if (nativeImageSign != INVALID_NGEN_SIGNATURE)
            {
                WCHAR szGuid[64];
                if (GuidToLPWSTR(nativeImageSign, szGuid, sizeof(szGuid) / sizeof(WCHAR)) == 0)
                {
                    ThrowHR(E_UNEXPECTED);
                }
                *pNativeImageIdentity = ::SysAllocString(szGuid);
            }

            *pAssemblyDisplayName = displayNameHolder.Extract();
        }
        EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

    STDMETHODIMP_(ULONG)    AddRef()
    {                               
        return InterlockedIncrement (&_cRef);
    }

    STDMETHODIMP_(ULONG)    Release()
    {
        SO_NOT_MAINLINE_FUNCTION;
        
        ULONG lRet = InterlockedDecrement (&_cRef);
        if (!lRet)
            delete this;
        return lRet;
    }

    STDMETHODIMP QueryInterface(REFIID riid,void ** ppv)
    {
        SO_NOT_MAINLINE_FUNCTION;

        if (!ppv)
            return E_POINTER;

        if (IsEqualIID(riid, IID_IUnknown) ||
            IsEqualIID(riid, IID_ICorSvcDependencies))
        {
            *ppv = static_cast<ICorSvcDependencies*> (this);
            AddRef();
            return S_OK;
        }
        else
        {
            *ppv = NULL;
            return E_NOINTERFACE;
        }
    }

private:
    LONG _cRef;
    Zapper *zapper;
};

//*****************************************************************************
// CCorSvcCreatePdbWorker is used to load the CLR, initialize an appdomain,
// load the given assembly and create a PDB for it.
//*****************************************************************************
class CCorSvcCreatePdbWorker {

public:

    CCorSvcCreatePdbWorker()
        : m_pZapper(NULL)
    {
    }

    ~CCorSvcCreatePdbWorker()
    {
        if (m_pZapper)
            delete m_pZapper;
    }

    void Initialize(BSTR pAppBaseOrConfig, OptimizationScenario scenario)
    {
        _ASSERTE(m_pZapper == NULL);

        NGenOptions options = {0};
        NGenPrivateAttributesClass privateAttributesClass;

        InitNGenOptions(&options, privateAttributesClass, scenario);
        options.lpszExecutableFileName = pAppBaseOrConfig;
        m_pZapper = Zapper::NewZapper(&options, true);
        m_pZapper->CreateDependenciesLookupDomain();
    }


    HRESULT CreatePdb(BSTR pAssemblyName, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath)
    {
        SO_NOT_MAINLINE_FUNCTION;
        _ASSERTE(m_pZapper);

        HRESULT hr = S_OK;
        EX_TRY {

            m_pZapper->CreatePdb(pAssemblyName, pNativeImagePath, pPdbPath, pdbLines, pManagedPdbSearchPath);

        } EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

private:

    Zapper *m_pZapper;

};

#ifdef _DEBUG
    inline void DoFreeEnvironmentStrings(LPTCH lpszEnvironmentBlock)
    {
        WszFreeEnvironmentStrings(lpszEnvironmentBlock);
    }
    typedef Wrapper<LPTCH, DoNothing, DoFreeEnvironmentStrings> EnvHolder;
#endif //_DEBUG    

//*****************************************************************************
// ICorSvcWorker contains methods for generating native images and enumerating
// their dependencies.
//*****************************************************************************[  
class CCorSvcWorker : 
    public ICorSvcWorker3,
    public ICorSvcRepository,
    public ICorSvcSetPrivateAttributes,
#ifdef FEATURE_APPX
    public ICorSvcAppX,
#endif
    public ICorSvcPooledWorker
{
public:
    CCorSvcWorker() :
        _cRef(0),
        repositoryDir(NULL),
        repositoryFlags(RepositoryDefault),
        ngenPrivateAttributesClass()

#ifdef FEATURE_FUSION
        ,
        pAssemblyCache(NULL)
#endif
    {
        g_pLocalServerLifetime->AddRefServerProcess();
    }

    ~CCorSvcWorker() 
    {
#ifdef FEATURE_FUSION
        if (pAssemblyCache != NULL)
        {
            pAssemblyCache->Release();
            pAssemblyCache = NULL;
        }
#endif

        GetSvcLogger()->ReleaseLogger();

        g_pLocalServerLifetime->ReleaseServerProcess();
    }

    STDMETHOD (SetPriority)(
            /*[in]*/ SvcWorkerPriority priority
        )
    {
        HRESULT hr = E_FAIL;
        
        // Set ourselves to the priority 
        if (::SetPriorityClass(GetCurrentProcess(),  priority.dwPriorityClass) == FALSE)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto DONE;
        }

        hr = S_OK;
    DONE:
        return hr;            
    }
    
#ifdef _DEBUG
    void Debug_CheckPPLProcessStatus(
        LPCWSTR wszAssemblyName)
    {
        size_t cchAssemblyName = wcslen(wszAssemblyName);
        
        // Check if we are in PPL (Protected Process Lightweight) by checking existence of LOCALAPPDATA env. var. (it is not present in PPL)
        BOOL fIsProcessPPL = TRUE;
        EnvHolder pEnvironmentStrings(WszGetEnvironmentStrings());
        for (LPTCH pEnv = pEnvironmentStrings; ((pEnv != NULL) && (*pEnv != W('\0'))); pEnv += wcslen(pEnv) + 1)
        {
            static const WCHAR  const_wszLocalAppData[] = W("LOCALAPPDATA=");
            static const size_t const_cchLocalAppData = _countof(const_wszLocalAppData) - 1;
            if (_wcsnicmp(pEnv, const_wszLocalAppData, const_cchLocalAppData) == 0)
            {   // LOCALAPPDATA is never set in PPL process
                fIsProcessPPL = FALSE;
                break;
            }
        }
        
        // Semicolon-separated list of names that should assert a failure
        NewArrayHolder<WCHAR> wszAssertList = NULL;
        if (fIsProcessPPL)
        {   // If we are in PPL, we should assert for assemblies that are fobidden to be ngen'd in PPL
            CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NGenProtectedProcess_ForbiddenList, &wszAssertList);
        }
        else
        {   // If we are not in PPL, we should assert for assemblies that require to be ngen'd in PPL
            CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NGenProtectedProcess_RequiredList, &wszAssertList);
        }
        
        if ((wszAssertList != NULL) && (*wszAssertList != W('\0')))
        {
            LPCWSTR pAssertListName = wszAssertList;
            for (;;)
            {
                LPCWSTR pAssertListNameEnd = wcschr(pAssertListName, W(';'));
                size_t  cchAssertListName;
                if (pAssertListNameEnd == NULL)
                {   // There is not another semicolon
                    cchAssertListName = wcslen(pAssertListName);
                }
                else
                {
                    cchAssertListName = pAssertListNameEnd - pAssertListName;
                }

                if ((cchAssertListName > 0) && (cchAssertListName <= cchAssemblyName))
                {
                    // Check prefix or suffix of assembly name (which is either file name or assembly identity name)
                    if ((_wcsnicmp(wszAssemblyName, pAssertListName, cchAssertListName) == 0) || 
                        (_wcsnicmp(wszAssemblyName + cchAssemblyName - cchAssertListName, pAssertListName, cchAssertListName) == 0))
                    {
                        if (fIsProcessPPL)
                        {
                            _ASSERTE_MSG(FALSE, "Assembly that is in NGenProtectedProcess_ForbiddenList is ngen'd in PPL process!");
                        }
                        else
                        {
                            _ASSERTE_MSG(FALSE, "Assembly that is in NGenProtectedProcess_RequiredList is ngen'd in normal (non-PPL) process!");
                        }
                    }
                }

                if (pAssertListNameEnd == NULL)
                {   // There are no more names in the semicolon-separated list
                    break;
                }

                // Move to next item in the semicolon-separated list (skip also the semicolon)
                pAssertListName = pAssertListNameEnd + 1;
            }
        }
    } // Debug_CheckPPLProcessStatus
#endif //_DEBUG
    
    STDMETHOD (OptimizeAssembly)(
        BSTR pAssemblyName, 
        BSTR pApplicationName, 
        OptimizationScenario scenario,
        SAFEARRAY *loadAlwaysList,
        SAFEARRAY *loadSometimesList,
        SAFEARRAY *loadNeverList,
        BSTR *pNativeImageIdentity
        ) 
    {
        SO_NOT_MAINLINE_FUNCTION;
        
        INDEBUG(Debug_CheckPPLProcessStatus(pAssemblyName);)
        
        HRESULT hr = S_OK;
        EX_TRY
        {
#if defined(_DEBUG) || defined(ALLOW_LOCAL_WORKER)
            // Make sure optimize is called only once per process
            static int OptimizeCount = 0;
#ifdef ALLOW_LOCAL_WORKER
            if (OptimizeCount != 0)
            {
                GetSvcLogger()->Printf(W("You cannot call OptimizeAssembly twice.  If you are using COMPLUS_NgenLocalWorker, make sure you are only optimizing one assembly.\r\n"));
                ThrowHR(E_FAIL);
            }
#else // _DEBUG
            _ASSERTE(OptimizeCount == 0);
#endif
            OptimizeCount++;
#endif

            NGenOptions opt = {0};
            InitNGenOptions(&opt, ngenPrivateAttributesClass, scenario, repositoryDir, repositoryFlags);
            opt.lpszExecutableFileName = pApplicationName;

            GUID nativeImageSign;
            bool hasProfileData;

            hr = ZapperCompileWrapper(pAssemblyName, &opt, &nativeImageSign,
                                      loadAlwaysList, loadSometimesList, loadNeverList,
                                      true, &hasProfileData);
#if 0
            // Unfotunately we can't perform a retry here as the Zapper currently
            // allocates and initializes some things once CompilationDomain and
            // thus some of this stuff will leak from the failed complation.
            //
            if (FAILED(hr) && hasProfileData  && retryNgenFailures)
            {
                hr = ZapperCompileWrapper(pAssemblyName, &opt, &nativeImageSign,
                                          loadAlwaysList, loadSometimesList, loadNeverList,
                                          false, NULL);
                if (SUCCEEDED(hr))
                {
                    StackSString msg;
                    msg.Printf(W("The compile failed when the profile data was used and ")
                               W("the compile succeeded when the profile data was ignored."));
                    GetSvcLogger()->Log(msg, LogLevel_Info);
                }
            }
#endif
            IfFailThrow(hr);

            _ASSERTE(nativeImageSign != INVALID_NGEN_SIGNATURE || opt.fRepositoryOnly);

            _ASSERTE(pNativeImageIdentity);
            if (nativeImageSign != INVALID_NGEN_SIGNATURE)
            {
                WCHAR szGuid[64];
                if (GuidToLPWSTR(nativeImageSign, szGuid, sizeof(szGuid) / sizeof(WCHAR)) == 0)
                    ThrowHR(E_UNEXPECTED);
                *pNativeImageIdentity = ::SysAllocString(szGuid);
            }
            else
            {
                *pNativeImageIdentity = NULL;
            }
        }
        EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

    STDMETHOD (DeleteNativeImage)(
        BSTR pAssemblyName,
        BSTR pNativeImage
        ) 
    {
        // The caller must either specify both parameters, or specify neither.
        _ASSERTE((pAssemblyName == NULL && pNativeImage == NULL) ||
                 (pAssemblyName != NULL && pNativeImage != NULL));

        HRESULT hr = S_OK;
        EX_TRY
        {
            NGenOptions opt = {0};
            InitNGenOptions(&opt, ngenPrivateAttributesClass);
            NewHolder<Zapper> zapper = Zapper::NewZapper(&opt, true);
            _ASSERTE(zapper != NULL);

            GUID *pNativeImageMVID = NULL;
            GUID nativeImageMVID;

            if (pNativeImage)
            {
                StackSString nativeImageString(pNativeImage);
                StackScratchBuffer buffer;
                LPCSTR pstr = nativeImageString.GetANSI(buffer);
                IfFailThrow(LPCSTRToGuid((LPCSTR) pstr, &nativeImageMVID));
                pNativeImageMVID = &nativeImageMVID;
            }

#ifdef FEATURE_FUSION
            if (pAssemblyName != NULL && pNativeImageMVID != NULL)
            {
                // Deleting a specific native image.
                zapper->DeleteFusionCacheEntry(pAssemblyName, pNativeImageMVID);
            }
            else if (pAssemblyName != NULL || pNativeImageMVID != NULL)
            {
                hr = E_UNEXPECTED;
            }
            else
            {
                // Not deleting a specific native image.  Need to enumerate NIC.
                IfFailThrow(zapper->EnumerateFusionCache(NULL, false, true, NULL));
            }
#else //FEATURE_FUSION
            _ASSERTE(!"NYI");
#endif //FEATURE_FUSION
        }
        EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

    STDMETHOD (DisplayNativeImages)(BSTR pAssemblyName) 
    {
#ifdef FEATURE_FUSION
        HRESULT hr = S_OK;
        EX_TRY
        {
            NGenOptions opt = {0};
            InitNGenOptions(&opt, ngenPrivateAttributesClass);
            NewHolder<Zapper> zapper = Zapper::NewZapper(&opt, true);
            _ASSERTE(zapper != NULL);
        
            IfFailThrow(zapper->EnumerateFusionCache(pAssemblyName, true, false, NULL));
        }
        EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
#else //FEATURE_FUSION
        return E_NOTIMPL;
#endif //FEATURE_FUSION
    }

    STDMETHOD(GetCorSvcDependencies)(
        BSTR pApplicationName,
        OptimizationScenario scenario,
        ICorSvcDependencies **ppCorSvcDependencies
        )
    {
        SO_NOT_MAINLINE_FUNCTION;
        
        HRESULT hr = S_OK;
        EX_TRY
        {
            NewHolder<CCorSvcDependencies> pCorSvcDependencies(new CCorSvcDependencies());
            pCorSvcDependencies->Initialize(pApplicationName, scenario);
            IfFailThrow(pCorSvcDependencies->QueryInterface(IID_ICorSvcDependencies, (void **) ppCorSvcDependencies));
            pCorSvcDependencies.SuppressRelease();
        }
        EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

    STDMETHOD(Stop)() 
    {
        return S_OK;
    }

    STDMETHOD(CreatePdb)(__in BSTR pAssemblyName, 
                         __in BSTR pAppBaseOrConfig,
                         __in OptimizationScenario scenario,
                         __in BSTR pNativeImagePath,
                         __in BSTR pPdbPath)
    {
        return CreatePdb2(
            pAssemblyName, 
            pAppBaseOrConfig,
            scenario,
            pNativeImagePath,
            pPdbPath,
            FALSE,
            NULL);
    }

    STDMETHOD(CreatePdb2)(__in BSTR pAssemblyName, 
                         __in BSTR pAppBaseOrConfig,
                         __in OptimizationScenario scenario,
                         __in BSTR pNativeImagePath,
                         __in BSTR pPdbPath,
                         __in BOOL pdbLines,
                         __in BSTR pManagedPdbSearchPath)
    {
        SO_NOT_MAINLINE_FUNCTION;

        HRESULT hr = S_OK;

        EX_TRY {

            CCorSvcCreatePdbWorker worker;
            worker.Initialize(pAppBaseOrConfig, scenario);
            hr = worker.CreatePdb(pAssemblyName, pNativeImagePath, pPdbPath, pdbLines, pManagedPdbSearchPath);

        } EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);

        return hr;
    }

#ifdef FEATURE_APPX
    STDMETHOD(SetPackage)(__in BSTR pPackageFullName)
    {
        return AppX::SetCurrentPackageForNGen(pPackageFullName);
    }

    STDMETHOD(SetLocalAppDataDirectory)(__in BSTR pLocalAppDataDirectory)
    {
        return Clr::Util::SetLocalAppDataDirectory(pLocalAppDataDirectory);
    }
#endif

    STDMETHOD (SetRepository)(
        BSTR pRepositoryDir,
        RepositoryFlags flags
        )
    {
        _ASSERTE(repositoryFlags == RepositoryDefault);
        _ASSERTE(repositoryDir == NULL);

        repositoryDir = ::SysAllocString(pRepositoryDir);
        repositoryFlags = flags;

        return S_OK;
    }

    STDMETHOD (SetNGenPrivateAttributes)(
        NGenPrivateAttributes ngenPrivateAttributes
        )
    {
        _ASSERTE(ngenPrivateAttributesClass.Flags == 0);
        _ASSERTE(ngenPrivateAttributesClass.ZapStats == 0);

        ngenPrivateAttributesClass.Flags = ngenPrivateAttributes.Flags;
        ngenPrivateAttributesClass.ZapStats = ngenPrivateAttributes.ZapStats;
        
        if (ngenPrivateAttributes.DbgDir)
        {
            _ASSERTE(ngenPrivateAttributesClass.DbgDir == NULL);
            ngenPrivateAttributesClass.DbgDir = ::SysAllocString(ngenPrivateAttributes.DbgDir);
        }
        
        return S_OK;
    }

    STDMETHOD (CanReuseProcess)(
        OptimizationScenario scenario,
        ICorSvcLogger *pCorSvcLogger,
        BOOL *pCanContinue)
    {
        SO_NOT_MAINLINE_FUNCTION;

        HRESULT hr = S_OK;

        _ASSERTE(pCanContinue != NULL);
        *pCanContinue = FALSE;

        return hr;
    }

    static HRESULT CreateObject(REFIID riid, void **ppUnk)
    {
        HRESULT     hr;
        CCorSvcWorker *pCorSvcWorker = new (nothrow) CCorSvcWorker();

        if (pCorSvcWorker == 0)
            return (E_OUTOFMEMORY);

        hr = pCorSvcWorker->QueryInterface(riid, ppUnk);
        if (FAILED(hr))
            delete pCorSvcWorker;
        return (hr);
    }

    STDMETHODIMP_(ULONG)    AddRef()
    {                               
        return InterlockedIncrement (&_cRef);
    }

    STDMETHODIMP_(ULONG)    Release()
    {
        ULONG lRet = InterlockedDecrement (&_cRef);
        if (!lRet)
            delete this;
        return lRet;
    }

    STDMETHODIMP QueryInterface(REFIID riid,void ** ppv)
    {
        if (!ppv)
            return E_POINTER;

        if (IsEqualIID(riid, IID_IUnknown) ||
            IsEqualIID(riid, IID_ICorSvcWorker))
        {
            *ppv = static_cast<ICorSvcWorker*> (this);
            AddRef();
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_ICorSvcWorker2))
        {
            *ppv = static_cast<ICorSvcWorker2 *>(this);
            AddRef();
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_ICorSvcWorker3))
        {
            *ppv = static_cast<ICorSvcWorker3 *>(this);
            AddRef();
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_ICorSvcRepository))
        {
            *ppv = static_cast<ICorSvcRepository*> (this);
            AddRef();
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_ICorSvcSetPrivateAttributes)) 
        {
            *ppv = static_cast<ICorSvcSetPrivateAttributes *> (this);
            AddRef();
            return S_OK;
        }
#ifdef FEATURE_APPX
        else if (IsEqualIID(riid, IID_ICorSvcAppX)) 
        {
            *ppv = static_cast<ICorSvcAppX *> (this);
            AddRef();
            return S_OK;
        }
#endif
        else if (IsEqualIID(riid, IID_ICorSvcPooledWorker))
        {
            *ppv = static_cast<ICorSvcPooledWorker *> (this);
            AddRef();
            return S_OK;
        }
        else
        {
            *ppv = NULL;
            return E_NOINTERFACE;
        }
    }

private:
    HRESULT ZapperCompileWrapper(BSTR          pAssemblyName, 
                                 NGenOptions * pOpt,
                                 GUID *        pNativeImageSign,
                                 SAFEARRAY *   loadAlwaysList,
                                 SAFEARRAY *   loadSometimesList,
                                 SAFEARRAY *   loadNeverList,
                                 bool          useProfileData,
                                 bool *        pHasProfileData)
    {
        NewHolder<Zapper> zapper(Zapper::NewZapper(pOpt, true));

        *pNativeImageSign = INVALID_NGEN_SIGNATURE;

        // Push the load lists to the zapper
        zapper->SetLoadLists(loadAlwaysList, loadSometimesList, loadNeverList);
        if (useProfileData == false)
        {
            zapper->DontUseProfileData();
        }

        HRESULT hr = zapper->Compile(pAssemblyName, pNativeImageSign);

        if (pHasProfileData != NULL)
        {
            *pHasProfileData = zapper->HasProfileData();
        }

        if (!FAILED(hr) && (*pNativeImageSign == INVALID_NGEN_SIGNATURE) && !pOpt->fRepositoryOnly)
        {
            // Unfortunately we can get a passing HR when an EE exception was
            // thrown because the zapper EH logic can't get the correct HR
            // out of the EE exception.  This will be fixed, but for now we
            // should also return E_FAIL in that case.

            hr = E_FAIL;
        }

        return hr;
    }

private:
    LONG _cRef;

    BSTRHolder      repositoryDir;
    RepositoryFlags repositoryFlags;
 
    NGenPrivateAttributesClass ngenPrivateAttributesClass;

#ifdef FEATURE_FUSION
    IAssemblyCache *pAssemblyCache;
#endif // FEATURE_FUSION
};

STDAPI NGenCreateNGenWorker(ICorSvcWorker **pCorSvcWorker, ILocalServerLifetime *pLocalServerLifetime, ICorSvcLogger *pCorSvcLogger)
{

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    EX_TRY
    {
        _ASSERTE(pLocalServerLifetime);
        //_ASSERTE(g_pLocalServerLifetime == NULL);

        g_pLocalServerLifetime = pLocalServerLifetime;

        GetSvcLogger()->SetSvcLogger(pCorSvcLogger);
 
        IfFailThrow(CCorSvcWorker::CreateObject(IID_ICorSvcWorker, (void **) pCorSvcWorker));
    }
    EX_CATCH_HRESULT_AND_NGEN_CLEAN(hr);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // !FEATURE_CORECLR
