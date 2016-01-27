// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




#include "common.h"

#include "clrhost.h"
#include "helpers.h"
#include "dbglog.h"
#include "adl.h"
#include "cacheutils.h"
#include "installlogger.h"
#include "actasm.h"
#include "naming.h"
#include "policy.h"
#include "bindhelpers.h"
#ifdef FEATURE_COMINTEROP
#include "appxutil.h"
#endif

#include "msi.h"

STDAPI InitializeNativeBinder();

#define DEVOVERRIDE_PATH							W(".local\\")
#define REG_KEY_IMAGE_FILE_EXECUTION_OPTIONS		W("Software\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options")
#define REG_VAL_DEVOVERRIDE_PATH					W("DevOverridePath")
#define REG_VAL_DEVOVERRIDE_ENABLE					W("DevOverrideEnable")
#define REG_VAL_FUSION_FORCE_UNIFICATION			W("OnlyUseLatestCLR") 
#define REG_VAL_USE_LEGACY_IDENTITY_FORMAT			W("UseLegacyIdentityFormat")
#define REG_VAL_ENABLE_MSI_IN_LOCAL_GAC				W("FusionEnableMSIInLocalGac")
#define REG_VAL_ENABLE_FORCED_FULL_CLOSURE_WALK     W("FusionEnableForcedFullClosureWalk")

// Registry / Environment variable
#define BINDING_CONFIGURATION                  W("BindingConfiguration")

extern HMODULE g_pMSCorEE;
extern BOOL g_bRunningOnNT6OrHigher;

WCHAR                         g_szWindowsDir[MAX_LONGPATH+1];
WCHAR                         g_FusionDllPath[MAX_LONGPATH+1];
HINSTANCE                     g_hInst = NULL;
HMODULE                       g_hMSCorEE = NULL;
DWORD                         g_dwLogInMemory;
DWORD                         g_dwLogLevel;
DWORD                         g_dwForceLog;
DWORD                         g_dwLogFailures;
DWORD                         g_dwLogResourceBinds;
BOOL                          g_bBinderLoggingNeeded;
DWORD                         g_dwDevOverrideEnable;
WORD                          g_wProcessorArchitecture;     // Default to 32 bit
BOOL                          g_fWow64Process;          // Wow64 Process
PEKIND                        g_peKindProcess;
List<CAssemblyDownload *>    *g_pDownloadList;
BOOL                          g_bLogToWininet;
WCHAR                         g_wzCustomLogPath[MAX_LONGPATH];
DWORD                         g_dwConfigForceUnification;
DWORD                         g_dwFileInUseRetryAttempts;
DWORD                         g_dwFileInUseMillisecondsBetweenRetries;
DWORD                         g_dwUseLegacyIdentityFormat;
DWORD                         g_dwConfigEnableMSIInLocalGac = 0;
DWORD                         g_dwConfigEnableForcedFullClosureWalk = 0;

CRITSEC_COOKIE                g_csInitClb;
CRITSEC_COOKIE                g_csConfigSettings;
CRITSEC_COOKIE                g_csSingleUse;
CRITSEC_COOKIE                g_csDownload;
CRITSEC_COOKIE                g_csBindLog;

// Defined in peimage.cpp
extern LCID                          g_lcid;

IIdentityAuthority           *g_pIdentityAuthority;
CIdentityCache               *g_pIdentityCache;

WCHAR                         g_wzEXEPath[MAX_LONGPATH+1];


#ifdef _DEBUG
BOOL                          g_bTagAssemblyNames;
#endif //_DEBUG

extern int SetOsFlag(void) ;

// MSI
DWORD g_dwDisableMSIPeek; 
typedef HRESULT (*pfnMsiProvideAssemblyW)(LPCWSTR wzAssemblyName, LPCWSTR szAppContext,
                                          DWORD dwInstallMode, DWORD dwUnused,
                                          LPWSTR lpPathBuf, DWORD *pcchPathBuf);
typedef INSTALLUILEVEL (*pfnMsiSetInternalUI)(INSTALLUILEVEL dwUILevel, HWND *phWnd);
typedef UINT (*pfnMsiInstallProductW)(LPCWSTR wzPackagePath, LPCWSTR wzCmdLine);

pfnMsiProvideAssemblyW     g_pfnMsiProvideAssemblyW;
pfnMsiSetInternalUI        g_pfnMsiSetInternalUI;
pfnMsiInstallProductW      g_pfnMsiInstallProductW;
BOOL                       g_bCheckedMSIPresent;
HMODULE                    g_hModMSI;

WCHAR g_wzLocalDevOverridePath[MAX_LONGPATH + 1];
WCHAR g_wzGlobalDevOverridePath[MAX_LONGPATH + 1];
DWORD g_dwDevOverrideFlags;

HRESULT GetScavengerQuotasFromReg(DWORD *pdwZapQuotaInGAC,
                                  DWORD *pdwDownloadQuotaAdmin,
                                  DWORD *pdwDownloadQuotaUser);

HRESULT SetupDevOverride(LPCWSTR pwzBindingConfigDevOverridePath);

static DWORD GetConfigDWORD(HKEY hkey, LPCWSTR wzName, DWORD dwDefault)
{
    LIMITED_METHOD_CONTRACT;
    DWORD   lResult;
    DWORD   dwSize;
    DWORD   dwType = REG_DWORD;
    DWORD   dwValue;

    if (hkey == 0) {
        return dwDefault;
    }

    dwSize = sizeof(DWORD);
    lResult = WszRegQueryValueEx(hkey, wzName, NULL,
                                &dwType, (LPBYTE)&dwValue, &dwSize);
    if (lResult != ERROR_SUCCESS || dwType != REG_DWORD) {
        return dwDefault;
    }

    return dwValue;
}

BOOL InitFusionCriticalSections()
{
    WRAPPER_NO_CONTRACT;
    BOOL fRet = FALSE;

    g_csInitClb = ClrCreateCriticalSection(CrstFusionClb, CRST_REENTRANCY);
    if (!g_csInitClb) {
        goto Exit;
    }

    g_csDownload = ClrCreateCriticalSection(CrstFusionDownload, CRST_REENTRANCY);
    if (!g_csDownload) {
        goto Exit;
    }

    g_csBindLog = ClrCreateCriticalSection(CrstFusionLog, CRST_DEFAULT);
    if (!g_csBindLog) {
        goto Exit;
    }

    g_csSingleUse = ClrCreateCriticalSection(CrstFusionSingleUse, CRST_DEFAULT);
    if (!g_csSingleUse) {
        goto Exit;
    }

    // <TODO> Get rid of this critical section</TODO>
    g_csConfigSettings = ClrCreateCriticalSection(CrstFusionConfigSettings, CRST_DEFAULT);
    if (!g_csConfigSettings) {
        goto Exit;
    }

    fRet = TRUE;
    
Exit:
    return fRet;
}

// ensure that the symbol will be exported properly
extern "C" HRESULT STDMETHODCALLTYPE InitializeFusion();

HRESULT STDMETHODCALLTYPE InitializeFusion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY;);
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT             hr = S_OK;
    static BOOL         bInitialized = FALSE;
    LPWSTR              pwzBindingConfigAssemblyStorePath = NULL;
    LPWSTR              pwzBindingConfigDevOverridePath = NULL;
    ReleaseHolder<CNodeFactory> pNodeFact;
    DWORD               dwSize = 0;
    BOOL                fExecutableIsKnown = FALSE;
    LPWSTR              pwzFileName = NULL;
    WCHAR               wzBindingConfigPath[MAX_LONGPATH + 1];

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    
    if (bInitialized) {
        hr = S_OK;
        goto Exit;
    }

    g_hInst = g_pMSCorEE;

    SetOsFlag();

    g_lcid = MAKELCID(LOCALE_INVARIANT, SORT_DEFAULT);

    WszGetModuleFileName(g_hInst, g_FusionDllPath, MAX_LONGPATH);

    if (!InitFusionCriticalSections()) {
        hr = E_OUTOFMEMORY;
        goto Exit;
    }

    g_wProcessorArchitecture = g_SystemInfo.wProcessorArchitecture;

    g_fWow64Process = RunningInWow64();

    if (IsProcess32()) {
        g_peKindProcess = (g_wProcessorArchitecture==PROCESSOR_ARCHITECTURE_INTEL)?peI386:peARM;
    }
    else {
        g_peKindProcess = (g_wProcessorArchitecture==PROCESSOR_ARCHITECTURE_IA64)?peIA64:peAMD64;
    }

    if (!g_pIdentityAuthority) {
        IIdentityAuthority                      *pAuth = NULL;

        hr = GetIdentityAuthority(&pAuth);
        if (FAILED(hr)) {
            goto Exit;
        }

        if (InterlockedCompareExchangeT(&g_pIdentityAuthority, pAuth, NULL) != NULL) {
            SAFE_RELEASE(pAuth);
        }
    }

    if (!g_pIdentityCache) {
        CIdentityCache              *pIdentityCache;
        
        hr = CIdentityCache::Create(&pIdentityCache);
        if (FAILED(hr)) {
            goto Exit;
        }
        
        if (InterlockedCompareExchangeT(&g_pIdentityCache, pIdentityCache, NULL) != NULL) {
            SAFEDELETE(pIdentityCache);
        }
    }

    DWORD           lResult;
    HKEY            hKey;

    lResult = FusionRegOpenKeyEx(HKEY_LOCAL_MACHINE, REG_KEY_FUSION_SETTINGS, 0, KEY_READ, &hKey);
    if (lResult != ERROR_SUCCESS) {
        hKey = 0;
    }
    
#define _GetConfigDWORD(name, default) GetConfigDWORD(hKey, name, default)

    // Get this executable's filename
    fExecutableIsKnown = WszGetModuleFileName(NULL, g_wzEXEPath, MAX_LONGPATH);
    if(!fExecutableIsKnown) {
        hr = StringCbCopy(g_wzEXEPath, sizeof(g_wzEXEPath), W("Unknown"));
        if (FAILED(hr)) {
            goto Exit;
        }
    }

    if (g_bRunningOnNT6OrHigher) {
        //
        // BINDINGCONFIGURATION
        //
        // Obtain the path to an override xml file via the registry for
        // this executable or from the environment variable
        //
        wzBindingConfigPath[0] = W('\0');

        // If there is a BindingConfiguration entry for this 
        // executable, then read it
        pwzFileName = PathFindFileName(g_wzEXEPath);
        if(fExecutableIsKnown && pwzFileName) {
            WCHAR       wzValue[MAX_LONGPATH + 1];
            HKEY        hKeyExeName = NULL;
            DWORD       dwType = REG_SZ;

            wzValue[0] = W('\0');

            // key name + '\' + filename + null
            if(lstrlenW(REG_KEY_IMAGE_FILE_EXECUTION_OPTIONS) + 1 + lstrlenW(pwzFileName) + 1 > MAX_PATH_FNAME) {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                goto Exit;
            }

            hr = StringCchPrintf(wzValue, MAX_PATH_FNAME, W("%ws\\%ws"), 
                REG_KEY_IMAGE_FILE_EXECUTION_OPTIONS, pwzFileName);
            if (FAILED(hr)) {
                goto Exit;
            }

            lResult = FusionRegOpenKeyEx(HKEY_LOCAL_MACHINE, wzValue, 0, KEY_READ, &hKeyExeName);
            if(lResult != ERROR_SUCCESS) {
                if(lResult != ERROR_FILE_NOT_FOUND) {
                    hr = HRESULT_FROM_WIN32(lResult);
                    goto Exit;
                }
            }
            else {  // Success
                dwSize = MAX_LONGPATH * sizeof(WCHAR);
                wzValue[0] = W('\0');

                lResult = WszRegQueryValueEx(hKeyExeName, BINDING_CONFIGURATION, NULL, &dwType, (LPBYTE)wzValue, &dwSize);
                RegCloseKey(hKeyExeName);
                if(lResult != ERROR_SUCCESS) {
                    if(lResult != ERROR_FILE_NOT_FOUND) {
                        hr = HRESULT_FROM_WIN32(lResult);
                        goto Exit;
                    }
                }
                else {  // Success
                    if(dwType != REG_SZ) {
                        hr = HRESULT_FROM_WIN32(ERROR_BADKEY);
                        goto Exit;
                    }

                    if(wzValue[0]) {
                        hr = StringCbCopy(wzBindingConfigPath, sizeof(wzBindingConfigPath), wzValue);
                        if (FAILED(hr)) {
                            goto Exit;
                        }
                    }
                }
            }
        }

        // If we didn't get a path from the registry,
        // try the ENV variable.
        if(!wzBindingConfigPath[0]) {
            dwSize = WszGetEnvironmentVariable(BINDING_CONFIGURATION, wzBindingConfigPath, MAX_LONGPATH);
            if(dwSize > MAX_LONGPATH) {
                hr = HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW);
                goto Exit;
            }
        }

        // We have a path to XML config override file
        if(wzBindingConfigPath[0]) {
            hr = ParseXML(&pNodeFact, wzBindingConfigPath, NULL, FALSE);
            if (FAILED(hr)) {
                goto Exit;
            }
        
            // Get AssemblyStore override path
            hr = pNodeFact->GetAssemblyStorePath(&pwzBindingConfigAssemblyStorePath);
            if(FAILED(hr)) {
                goto Exit;
            }
        
            // Get DevOverride path
            hr = pNodeFact->GetDevOverridePath(&pwzBindingConfigDevOverridePath);
            if(FAILED(hr)) {
                goto Exit;
            }
        }
    }

    //
    // end BindingConfiguration
    //

    hr = SetRootCachePath(pwzBindingConfigAssemblyStorePath);
    if(FAILED(hr)) {
        goto Exit;
    }

    GetScavengerQuotasFromReg(NULL, NULL, NULL);

#ifdef _DEBUG
    g_bTagAssemblyNames = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TagAssemblyNames);
#endif //_DEBUG

    // Machine level logging settings
    g_dwLogInMemory = _GetConfigDWORD(REG_VAL_FUSION_LOG_ENABLE, 0);
    g_dwLogLevel = _GetConfigDWORD(REG_VAL_FUSION_LOG_LEVEL, 1);
    g_dwForceLog = _GetConfigDWORD(REG_VAL_FUSION_LOG_FORCE, 0);
    g_dwLogFailures = _GetConfigDWORD(REG_VAL_FUSION_LOG_FAILURES, 0);
    g_dwLogResourceBinds = _GetConfigDWORD(REG_VAL_FUSION_LOG_RESOURCE_BINDS, 0);

    BOOL fusionMessagesAreEnabled = ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FusionMessageEvent);
    g_bBinderLoggingNeeded = !!(fusionMessagesAreEnabled || g_dwLogFailures || g_dwForceLog || g_dwLogResourceBinds || g_dwLogInMemory);

    g_dwConfigForceUnification = REGUTIL::GetConfigDWORD_DontUse_(REG_VAL_FUSION_FORCE_UNIFICATION, 0);

    // Settings to indicate how many times to retry opening a hard-locked file for reading, moving/renaming or deletion.
    // Used to configure CreateFileWithRetries and MoveFileWithRetries in fusion\Utils\Helpers.cpp
    // These are used mainly for GAC Installation where anti-virus and indexing programs can temporarily hard-lock files.
    // g_dwFileInUseRetryAttempts does not include the first attempt to open the file (hence set it to 0 to prevent retries).
    g_dwFileInUseRetryAttempts = _GetConfigDWORD(REG_VAL_FUSION_FILE_IN_USE_RETRY_ATTEMPTS, FILE_IN_USE_RETRY_ATTEMPTS);
    if (g_dwFileInUseRetryAttempts > MAX_FILE_IN_USE_RETRY_ATTEMPTS)
        g_dwFileInUseRetryAttempts = MAX_FILE_IN_USE_RETRY_ATTEMPTS;    
    g_dwFileInUseMillisecondsBetweenRetries = _GetConfigDWORD(REG_VAL_FUSION_FILE_IN_USE_MILLISECONDS_BETWEEN_RETRIES, FILE_IN_USE_MILLISECONDS_BETWEEN_RETRIES);
    if (g_dwFileInUseMillisecondsBetweenRetries > MAX_FILE_IN_USE_MILLISECONDS_BETWEEN_RETRIES)
        g_dwFileInUseMillisecondsBetweenRetries = MAX_FILE_IN_USE_MILLISECONDS_BETWEEN_RETRIES;    

    // This setting is only relevant in the unsupported case where we're running out of a local GAC
    // g_bUseDefaultStore is initialized in SetRootCachePath above
    if (!g_bUseDefaultStore)
    {
        g_dwConfigEnableMSIInLocalGac = REGUTIL::GetConfigDWORD_DontUse_(REG_VAL_ENABLE_MSI_IN_LOCAL_GAC, 0);
    }
    g_dwConfigEnableForcedFullClosureWalk = REGUTIL::GetConfigDWORD_DontUse_(REG_VAL_ENABLE_FORCED_FULL_CLOSURE_WALK, 0);

    g_dwUseLegacyIdentityFormat = AppX::IsAppXProcess() || _GetConfigDWORD(REG_VAL_USE_LEGACY_IDENTITY_FORMAT, 0);
    g_dwDisableMSIPeek = _GetConfigDWORD(W("DisableMSIPeek"), FALSE);
    g_dwDisableMSIPeek |= CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DisableMSIPeek);

    if (hKey) {
        RegCloseKey(hKey);
        hKey = NULL;
    }

    {
        WCHAR wzBuf[1];

        wzBuf[0] = W('\0');
        dwSize = WszGetEnvironmentVariable(W("USE_LEGACY_IDENTITY_FORMAT"), wzBuf, 1);
        if (dwSize == 1 && !FusionCompareString(wzBuf, W("1"))) {
            g_dwUseLegacyIdentityFormat = 1;
        }
    }

    if (IsLoggingNeeded()) {
        g_bLogToWininet = TRUE;
        dwSize = MAX_LONGPATH;
        DWORD dwAttr;
        BOOL  fExists;
        g_wzCustomLogPath[0] = W('\0');
        GetCustomLogPath(g_wzCustomLogPath, &dwSize);
        if (g_wzCustomLogPath[0] != W('\0')) {
            if(SUCCEEDED(CheckFileExistence(g_wzCustomLogPath, &fExists, &dwAttr)) &&
               fExists && (dwAttr & FILE_ATTRIBUTE_DIRECTORY)) {
                g_bLogToWininet = FALSE;
            }
        }
    }

    // make devoverride longhorn+ only
    if (g_bRunningOnNT6OrHigher) {
        hr = SetupDevOverride(pwzBindingConfigDevOverridePath);
        if (FAILED(hr)) {
            goto Exit;
        }
    }
    
    g_pDownloadList = new (nothrow) List<CAssemblyDownload *>;
    if (!g_pDownloadList) {
        hr = E_OUTOFMEMORY;
        goto Exit;
    }

    g_pAssemblyFingerprintCache = new (nothrow) CAssemblyFingerprintCache;
    if (!g_pAssemblyFingerprintCache) {
        hr = E_OUTOFMEMORY;
        goto Exit;
    }

    hr = g_pAssemblyFingerprintCache->Init();
    if (FAILED(hr))
    {
        goto Exit;
    }

    bInitialized = TRUE;

    hr = InitializeNativeBinder();
    if (FAILED(hr))
    {
        goto Exit;
    }

Exit:
    SAFEDELETEARRAY(pwzBindingConfigAssemblyStorePath);
    SAFEDELETEARRAY(pwzBindingConfigDevOverridePath);

    END_SO_INTOLERANT_CODE;
    
    return hr;
}

HRESULT SetupDevOverride(LPCWSTR pwzBindingConfigDevOverridePath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT         hr = S_OK;

 // g_wzLocalDevOverridePath[0] = W('\0');
 // g_wzGlobalDevOverridePath[0] = W('\0');
    g_dwDevOverrideFlags = 0;

    DWORD                                        dwFileAttr;
    DWORD                                        dwType;
    DWORD                                        dwSize;
    LONG                                         lResult = 0;
    HKEY                                         hKey = 0;

    lResult = FusionRegOpenKeyEx(HKEY_LOCAL_MACHINE, REG_KEY_IMAGE_FILE_EXECUTION_OPTIONS, 0, KEY_READ, &hKey);
    if (lResult != ERROR_SUCCESS) {
        hr = S_FALSE; 
        goto Exit;
    }

    g_dwDevOverrideEnable = _GetConfigDWORD(REG_VAL_DEVOVERRIDE_ENABLE, 0);

    if (g_dwDevOverrideEnable != 0) {

        // Check local dev path
        if (!WszGetModuleFileName(NULL, g_wzLocalDevOverridePath, MAX_LONGPATH)) {
            hr = HRESULT_FROM_GetLastError();
            goto Exit;
        }

        if (lstrlenW(g_wzLocalDevOverridePath) + lstrlenW(DEVOVERRIDE_PATH) <= MAX_LONGPATH) {
            // Only process .devoverride if the total path length <= MAX_LONGPATH

            hr = StringCbCat(g_wzLocalDevOverridePath, sizeof(g_wzLocalDevOverridePath), DEVOVERRIDE_PATH);
            if (FAILED(hr)) {
                goto Exit;
            }

            dwFileAttr = WszGetFileAttributes(g_wzLocalDevOverridePath);
            if ((dwFileAttr != INVALID_FILE_ATTRIBUTES) && (dwFileAttr & FILE_ATTRIBUTE_DIRECTORY)) {
                g_dwDevOverrideFlags |= DEVOVERRIDE_LOCAL;
            }
            else {
                // Clear the path just for goodness sake
                g_wzLocalDevOverridePath[0] = W('\0');
            }
        }

        // Check global dev path
        dwSize = sizeof(g_wzGlobalDevOverridePath);

        lResult = WszRegQueryValueEx(hKey, REG_VAL_DEVOVERRIDE_PATH, NULL, &dwType, (LPBYTE)g_wzGlobalDevOverridePath, &dwSize);
        if (lResult == ERROR_SUCCESS && lstrlenW(g_wzGlobalDevOverridePath) && dwType == REG_SZ) {
            
            dwFileAttr = WszGetFileAttributes(g_wzGlobalDevOverridePath);
            if ((dwFileAttr != INVALID_FILE_ATTRIBUTES) && (dwFileAttr & FILE_ATTRIBUTE_DIRECTORY)) {
                g_dwDevOverrideFlags |= DEVOVERRIDE_GLOBAL;
            }
            else {
                // Clear the path just for goodness sake
                g_wzGlobalDevOverridePath[0] = W('\0');
            }
        }

        // BINDING_CONFIGURATION Env check
        if(pwzBindingConfigDevOverridePath && pwzBindingConfigDevOverridePath[0]) {
            WCHAR       wzTempPath[MAX_LONGPATH + 1];
            BOOL        fExists = FALSE;
            WIN32_FILE_ATTRIBUTE_DATA fileInfo;

            wzTempPath[0] = W('\0');

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:25025)
#endif
            // usage of PathCanonicalize is safe if we ensure that
            // length of buffer specified by parameter1 is >= length of buffer specified by parameter2
            if(lstrlenW(pwzBindingConfigDevOverridePath) + 1 > COUNTOF(wzTempPath)) {
                hr = HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW);
                goto Exit;
            }

            if(!PathCanonicalize(wzTempPath, pwzBindingConfigDevOverridePath)) {
                hr = E_FAIL;
                goto Exit;
            }

#ifdef _PREFAST_
#pragma warning(pop)
#endif

            if(!WszGetFileAttributesEx(wzTempPath, GetFileExInfoStandard, &fileInfo)) {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Exit;
            }

            if(!(fileInfo.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
                hr = HRESULT_FROM_WIN32(ERROR_FILE_EXISTS);
                goto Exit;
            }
            
            hr = StringCbCopy(g_wzGlobalDevOverridePath, sizeof(g_wzGlobalDevOverridePath), wzTempPath);
            if (FAILED(hr)) {
                goto Exit;
            }

            g_dwDevOverrideFlags |= DEVOVERRIDE_GLOBAL;
        }

        if(g_dwDevOverrideFlags & DEVOVERRIDE_GLOBAL) {
            PathAddBackslashWrap(g_wzGlobalDevOverridePath, MAX_LONGPATH);
        }
    }

Exit:

    if (hKey) {
        RegCloseKey(hKey);
    }

    return hr;
}

