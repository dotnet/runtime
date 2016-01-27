// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: registrywrapper.cpp
//

//
// Wrapper around Win32 Registry Functions allowing redirection of .Net 
// Framework root registry location
//
// Notes on Offline Ngen Implementation:
//
// This implementation redirects file accesses to the GAC, NIC, framework directory
// and registry accesses to root store and fusion.
// Essentially, if we open a file or reg key directly from the CLR, we redirect it
// into the mounted VHD specified in the COMPLUS config values.
//
// Terminology:
//  Host Machine - The machine running a copy of windows that mounts a VHD to 
//      compile the assemblies within.  This is the build machine in the build lab.
//
//  Target Machine - The VHD that gets mounted inside the host.  We compile
//      native images storing them inside the target.  This is the freshly build
//      copy of windows in the build lab.
//
// The OS itself pulls open all manner of registry keys and files as side-effects
// of our API calls. Here is a list of things the redirection implementation does
// not cover:
//
// - COM
//      We use COM in Ngen to create and communicate with worker processes.  In
//      order to marshal arguments between ngen and worker, mscoree.tlb is loaded.
//      The COM system from the loaded and running OS is used, which means the host
//      machine's mscoree.tlb gets loaded for marshalling arguments for the CLR
//      running on the target machine. In the next release (4.5) the ngen interfaces
//      don't expect to change for existing ngen operations.  If new functionality
//      is added, new interfaces would be added, but existing ones will not be
//      altered since we have a high compat bar with an inplace release.  mscoree.tlb
//      also contains interfaces for mscoree shim and gchost which again we expect to
//      remain compatible in this next product release.  In order to fix this, we will
//      need support from Windows for using the COM system on another copy of Windows.
// - Registry Accesses under
//      - HKLM\software[\Wow6432Node]\policies\microsoft : SQM, Cryptography, MUI, codeidentifiers, appcompat, RPC 
//      - HKLM\software[\Wow6432Node]\RPC,OLE,COM and under these keys
//      - HKLM\Software\Microsoft\Cryptography and under
//      - HKLM\Software\Microsoft\SQMClient
//      - HKLM\Software[\Wow6432Node]\Microsoft\Windows\Windows Error Reporting\WMR and under 
//
//      These locations are not accessed directly by the CLR, but looked up by Windows
//      as part of other API calls.  It is safer that we are accessing these
//      on the host machine since they correspond with the actively running copy of 
//      Windows.  If we could somehow redirect these to the target VM, we would have
//      Windows 7/2K8 OS looking up its config keys from a Win8 registry.  If Windows
//      has made incompatible changes here, such as moving the location or redefining
//      values, we would break.
//  - Accesses from C:\Windows\System32 and C:\Windows\Syswow64 and HKCU
//      HKCU does not contain any .Net Framework settings (Microsoft\.NETFramework
//      is empty).
//      There are various files accessed from C:\Windows\System32 and these are a
//      function of the OS loader.  We load an executable and it automatically
//      pulls in kernel32.dll, for example.  This should not be a problem for running
//      the CLR, since v4.5 will run on Win2K8, and for offline ngen compilation, we
//      will not be using the new Win8 APIs for AppX.  We had considered setting
//      the PATH to point into the target machine's System32 directory, but the
//      Windows team advised us that would break pretty quickly due to Api-sets
//      having their version numbers rev'd and the Win2k8 host machine not having
//      the ability to load them.
//
//
//*****************************************************************************
#include "stdafx.h"
#include "registrywrapper.h"
#include "clrconfig.h"
#include "strsafe.h"

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES) && !defined(FEATURE_CORECLR)

static LPCWSTR kRegistryRootKey = W("SOFTWARE\\");
static const HKEY kRegistryRootHive = HKEY_LOCAL_MACHINE;
static LPCWSTR kWow6432Node = W("Wow6432Node\\");
static LPCWSTR kMicrosoftDotNetFrameworkPolicy = W("Microsoft\\.NETFramework\\Policy");
static LPCWSTR wszHKLM = W("HKLM\\");
static WCHAR * g_registryRoot = NULL;
static volatile bool g_initialized = false;
static const WCHAR BACKSLASH_CHARACTER[] = W("\\");

//
// Called the first time we use ClrRegCreateKeyEx or ClrRegOpenKeyEx to determine if the registry redirection
// config value has been set.  If so, we parse it into g_registryRoot
// If the config string is mal-formed, we don't set the global variable.
//
HRESULT ParseRegistryRootConfigValue()
{
    if (!IsNgenOffline())
    {
        return ERROR_SUCCESS;
    }
    
    CLRConfigStringHolder configValue(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_RegistryRoot));
    // Since IsNgenOffline returned true, this better not be NULL
    if (configValue == NULL)
        return ERROR_BAD_ARGUMENTS;
    
    if (_wcsnicmp(configValue, wszHKLM, wcslen(wszHKLM)) != 0)
    {
        return ERROR_BAD_ARGUMENTS;
    }
    
    // The rest of the string is the location of the redirected registry key
    LPWSTR configValueKey = (LPWSTR)configValue;
    configValueKey = _wcsninc(configValueKey, wcslen(wszHKLM));
    
    size_t len = wcslen(configValueKey) + 1;
    
    bool appendBackslash = false;
    if (configValueKey[wcslen(configValueKey) - 1] != W('\\'))
    {
        appendBackslash = true;
        len += wcslen(BACKSLASH_CHARACTER);
    }
    g_registryRoot = new (nothrow) WCHAR[len];
    
    if (g_registryRoot == NULL)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
    
    wcscpy_s(g_registryRoot, len, configValueKey);
    if (appendBackslash)
    {
        StringCchCat(g_registryRoot, len, BACKSLASH_CHARACTER);
    }
    
    return ERROR_SUCCESS;
}

//
// This is our check that the target machine is 32/64bit
// If 32bit only, there will be no Wow6432Node key
// If 64bit, there will be a Wow6432Node key
//
HRESULT CheckWow6432NodeNetFxExists(bool * fResult)
{
    static bool bInitialized = false;
    static bool bNodeExists = false;
    HRESULT hr = ERROR_SUCCESS;
    
    if (!bInitialized)
    {
        size_t redirectedLength = wcslen(g_registryRoot) + wcslen(kWow6432Node) + wcslen(kMicrosoftDotNetFrameworkPolicy) + 1;
        LPWSTR lpRedirectedKey = new (nothrow) WCHAR[redirectedLength];
    
        if (lpRedirectedKey == NULL)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }
        
        wcscpy_s(lpRedirectedKey, redirectedLength, g_registryRoot);
        StringCchCat(lpRedirectedKey, redirectedLength, kWow6432Node);
        StringCchCat(lpRedirectedKey, redirectedLength, kMicrosoftDotNetFrameworkPolicy);
        
        HKEY hkey;
        hr = RegOpenKeyExW(HKEY_LOCAL_MACHINE, lpRedirectedKey, 0, KEY_READ, &hkey);
        bNodeExists = hr == ERROR_SUCCESS;
        
        if (hr == ERROR_FILE_NOT_FOUND)
        {
            hr = ERROR_SUCCESS;
        }
        
        if (hkey)
        {
            RegCloseKey(hkey);
        }
        bInitialized = true;
    }
    *fResult = bNodeExists;
    return hr;
}

HRESULT CheckUseWow6432Node(REGSAM samDesired, bool * fResult)
{
    HRESULT hr = ERROR_SUCCESS;
    bool fWow6432NodeExists = false;
    
    hr = CheckWow6432NodeNetFxExists(&fWow6432NodeExists);
    if (hr == ERROR_SUCCESS)
    {
        if (!fWow6432NodeExists)
        {
            *fResult = false;
        } else
        {
#if defined(_WIN64)
            *fResult = false;
#else
            *fResult = true;
#endif
            // If KEY_WOW64_64KEY or KEY_WOW64_32KEY are set, override
            // If both are set, the actual registry call will fail with ERROR_INVALID_PARAMETER
            // so no worries here
            if (samDesired & KEY_WOW64_64KEY)
            {
                *fResult = false;
            } else if (samDesired & KEY_WOW64_32KEY)
            {
                *fResult = true;
            }
        }
    }
    return hr;
}

//
// lpRedirectedKey is allocated and returned from this method.  Caller owns the new string and
// should release
//
__success(return == ERROR_SUCCESS)
HRESULT RedirectKey(REGSAM samDesired, HKEY hKey, LPCWSTR lpKey, __deref_out_z LPWSTR * lpRedirectedKey)
{
    if (hKey != kRegistryRootHive || _wcsnicmp(lpKey, kRegistryRootKey, wcslen(kRegistryRootKey)) != 0)
    {
        *lpRedirectedKey = NULL;
        return ERROR_SUCCESS;
    }
    
    LPCWSTR lpRootStrippedKey = lpKey + wcslen(kRegistryRootKey);
    size_t redirectedLength = wcslen(g_registryRoot) + wcslen(lpRootStrippedKey) + 1;
    
    bool bUseWow = false;
    HRESULT hr = CheckUseWow6432Node(samDesired, &bUseWow);
    
    if (hr != ERROR_SUCCESS)
    {
        return hr;
    }
    
    if (bUseWow)
    {
        redirectedLength += wcslen(kWow6432Node);
    }
    
    *lpRedirectedKey = new (nothrow) WCHAR[redirectedLength];
    
    if (*lpRedirectedKey == NULL)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
    
    wcscpy_s(*lpRedirectedKey, redirectedLength, g_registryRoot);
    if (bUseWow)
    {
        StringCchCat(*lpRedirectedKey, redirectedLength, kWow6432Node);
    }
    
    StringCchCat(*lpRedirectedKey, redirectedLength, lpRootStrippedKey);
    
    return ERROR_SUCCESS;
}

LONG ClrRegCreateKeyEx(
        HKEY hKey,
        LPCWSTR lpSubKey,
        DWORD Reserved,
        __in_opt LPWSTR lpClass,
        DWORD dwOptions,
        REGSAM samDesired,
        LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        PHKEY phkResult,
        LPDWORD lpdwDisposition
        )
{
    HRESULT hr;
    if (!g_initialized)
    {
        hr = ParseRegistryRootConfigValue();
        if (hr != ERROR_SUCCESS)
            return hr;
        g_initialized = true;
    }
    
    if (g_registryRoot != NULL)
    {
        NewArrayHolder<WCHAR> lpRedirectedKey(NULL);
        hr = RedirectKey(samDesired, hKey, lpSubKey, &lpRedirectedKey);
        // We don't want to touch the registry if we're low on memory and can't redirect properly.  It could lead
        // to use reading and writing to two separate registries and corrupting both in the process
        if (hr != ERROR_SUCCESS)
            return hr;
        LPCWSTR lpKeyToUse = lpRedirectedKey == NULL ? lpSubKey : lpRedirectedKey;
        return RegCreateKeyExW(hKey, lpKeyToUse, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);
    }
    return RegCreateKeyExW(hKey, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);
} // ClrRegCreateKeyEx

LONG ClrRegOpenKeyEx(
        HKEY hKey,
        LPCWSTR lpSubKey,
        DWORD ulOptions,
        REGSAM samDesired,
        PHKEY phkResult
        )
{
    HRESULT hr;
    if (!g_initialized)
    {
        hr = ParseRegistryRootConfigValue();
        if (hr != ERROR_SUCCESS)
            return hr;
        g_initialized = true;
    }
    
    if (g_registryRoot != NULL)
    {
        NewArrayHolder<WCHAR> lpRedirectedKey(NULL);
        hr = RedirectKey(samDesired, hKey, lpSubKey, &lpRedirectedKey);
        // We don't want to touch the registry if we're low on memory and can't redirect properly.  It could lead
        // to use reading and writing to two separate registries and corrupting both in the process
        if (hr != ERROR_SUCCESS)
            return hr;
        LPCWSTR lpKeyToUse = lpRedirectedKey == NULL ? lpSubKey : lpRedirectedKey;
        return RegOpenKeyExW(hKey, lpKeyToUse, ulOptions, samDesired, phkResult);
    }
    return RegOpenKeyExW(hKey, lpSubKey, ulOptions, samDesired, phkResult);
}

//
// Determine whether we're running Offline Ngen for Build Lab based on the settings of several Config Values.
//
bool IsNgenOffline()
{
    static volatile bool fInitialized = false;
    static volatile bool fNgenOffline = false;
    
    if (!fInitialized)
    {
        REGUTIL::CORConfigLevel kCorConfigLevel = static_cast<REGUTIL::CORConfigLevel>(REGUTIL::COR_CONFIG_ENV);
        CLRConfigStringHolder pwzConfigInstallRoot = REGUTIL::GetConfigString_DontUse_(CLRConfig::INTERNAL_InstallRoot,
                                                          TRUE /* Prepend Complus */,
                                                          kCorConfigLevel,
                                                          FALSE /* fUsePerfCache */);
        CLRConfigStringHolder pwzConfigNicPath(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_NicPath));
        CLRConfigStringHolder pwzRegistryRoot(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_RegistryRoot));
        CLRConfigStringHolder pwzConfigAssemblyPath(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_AssemblyPath));
        CLRConfigStringHolder pwzConfigAssemblyPath2(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_AssemblyPath2));
        if (pwzConfigInstallRoot != NULL && 
                pwzConfigNicPath != NULL &&
                pwzConfigAssemblyPath != NULL && 
                pwzConfigAssemblyPath2 != NULL &&
                pwzRegistryRoot != NULL)
        {
            fNgenOffline = true;
        }
        fInitialized = true;
    }
    
    return fNgenOffline;
}

#endif //!FEATURE_UTILCODE_NO_DEPENDENCIES && !FEATURE_CORECLR
