// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


#include "stdafx.h"

#include <strsafe.h>

#include "utilcode.h"
#include "holder.h"
#include "volatile.h"
#include "clr/fs.h"
#include "clr/str.h"

#include "appxutil.h"
#include "ex.h"

#include "shlwapi.h"    // Path manipulation APIs

#ifdef FEATURE_CORECLR

GVAL_IMPL(bool, g_fAppX);
INDEBUG(bool g_fIsAppXAsked;)

namespace AppX
{
#ifdef DACCESS_COMPILE
    bool DacIsAppXProcess()
    {
        return g_fAppX;
    }
#else

    // Returns true if host has deemed the process to be appx
    bool IsAppXProcess()
    {
        INDEBUG(g_fIsAppXAsked = true;)
        return g_fAppX;
    }


    void SetIsAppXProcess(bool value)
    {
        _ASSERTE(!g_fIsAppXAsked);
        g_fAppX = value;    
    }
#endif
};

#else // FEATURE_CORECLR

//---------------------------------------------------------------------------------------------
// Convenience values

#ifndef E_INSUFFICIENT_BUFFER
    #define E_INSUFFICIENT_BUFFER (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
#endif

#ifndef E_FILE_NOT_FOUND
    #define E_FILE_NOT_FOUND (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
#endif

//---------------------------------------------------------------------------------------------
using clr::str::IsNullOrEmpty;

//---------------------------------------------------------------------------------------------
typedef decltype(GetCurrentPackageId)   GetCurrentPackageId_t;
typedef decltype(GetCurrentPackageInfo) GetCurrentPackageInfo_t;
typedef decltype(GetCurrentPackagePath) GetCurrentPackagePath_t;
typedef decltype(OpenPackageInfoByFullName) OpenPackageInfoByFullName_t;
typedef decltype(ClosePackageInfo)          ClosePackageInfo_t;
typedef decltype(GetPackageInfo)            GetPackageInfo_t;

//---------------------------------------------------------------------------------------------
// Caches AppX ARI API-related information.
struct AppXRTInfo
{
    HMODULE                     m_hAppXRTMod;
    bool                        m_fIsAppXProcess;
    bool                        m_fIsAppXAdaptiveApp;
    bool                        m_fIsAppXNGen;

    GetCurrentPackageId_t *     m_pfnGetCurrentPackageId;
    GetCurrentPackageInfo_t *   m_pfnGetCurrentPackageInfo;
    GetCurrentPackagePath_t *   m_pfnGetCurrentPackagePath;

    NewArrayHolder<BYTE>        m_pbAppContainerInfo;
    DWORD                       m_cbAppContainerInfo;

    struct CurrentPackageInfo
    {
        UINT32        m_cbCurrentPackageInfo;
        PBYTE         m_pbCurrentPackageInfo;
        UINT32        m_nCount;

        CurrentPackageInfo(UINT32 cbPkgInfo, PBYTE pbPkgInfo, UINT32 nCount)
            : m_cbCurrentPackageInfo(cbPkgInfo)
            , m_pbCurrentPackageInfo(pbPkgInfo)
            , m_nCount(nCount)
        { LIMITED_METHOD_CONTRACT; }

        ~CurrentPackageInfo()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_pbCurrentPackageInfo != nullptr)
            {
                delete [] m_pbCurrentPackageInfo;
                m_pbCurrentPackageInfo = nullptr;
            }
        }
    };

    CurrentPackageInfo *        m_pCurrentPackageInfo;

    NewArrayHolder<WCHAR>       m_AdaptiveAppWinmetadataDir;

    AppXRTInfo() :
        m_hAppXRTMod(nullptr),
        m_fIsAppXProcess(false),
        m_fIsAppXAdaptiveApp(false),
        m_fIsAppXNGen(false),
        m_pfnGetCurrentPackageId(nullptr),
        m_pfnGetCurrentPackageInfo(nullptr),
        m_pfnGetCurrentPackagePath(nullptr),
        m_pbAppContainerInfo(nullptr),
        m_pCurrentPackageInfo(nullptr),
        m_AdaptiveAppWinmetadataDir(nullptr)
    { LIMITED_METHOD_CONTRACT; }

    ~AppXRTInfo()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pCurrentPackageInfo != nullptr)
        {
            delete m_pCurrentPackageInfo;
            m_pCurrentPackageInfo = nullptr;
        }

        if (m_hAppXRTMod != nullptr)
        {
            FreeLibrary(m_hAppXRTMod);
            m_hAppXRTMod = nullptr;
        }
    }
};  // struct AppXRTInfo

GPTR_IMPL(AppXRTInfo, g_pAppXRTInfo); // Relies on zero init static memory.

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------------
static
HRESULT GetAppContainerTokenInfoForProcess(
    DWORD pid,
    NewArrayHolder<BYTE>& pbAppContainerTokenInfo,
    DWORD* pcbAppContainerTokenInfo)
{
    PRECONDITION(CheckPointer(pcbAppContainerTokenInfo, NULL_OK));

    HRESULT hr = S_OK;

    pbAppContainerTokenInfo = nullptr;

    // In order to get the AppContainer SID we need to open the process token
    HandleHolder hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, pid);
    if (hProcess == NULL)
        return HRESULT_FROM_GetLastError();

    HandleHolder hToken;
    if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
        return HRESULT_FROM_GetLastError();

    // Query the process to see if it's inside an AppContainer
    ULONG isAppContainer = 0;
    DWORD actualLength = 0;
    if (!GetTokenInformation(hToken, static_cast<TOKEN_INFORMATION_CLASS>(TokenIsAppContainer), &isAppContainer, sizeof(isAppContainer), &actualLength))
        return HRESULT_FROM_GetLastError();

    _ASSERTE(actualLength > 0);

    // Not an AppContainer so bail
    if (!isAppContainer)
    {
        return S_FALSE;
    }

    // Now we need the AppContainer SID so first get the required buffer length
    actualLength = 0;
    VERIFY(!GetTokenInformation(hToken, static_cast<TOKEN_INFORMATION_CLASS>(TokenAppContainerSid), NULL, 0, &actualLength));
    hr = HRESULT_FROM_GetLastError();
    _ASSERTE(hr == E_INSUFFICIENT_BUFFER);

    // Something unexpected happened
    if (hr != E_INSUFFICIENT_BUFFER)
        return hr;

    // Now we know the length of the AppContainer SID so create a buffer and retrieve it
    pbAppContainerTokenInfo = new (nothrow) BYTE[actualLength];
    IfNullRet(pbAppContainerTokenInfo);

    if (!GetTokenInformation(hToken, static_cast<TOKEN_INFORMATION_CLASS>(TokenAppContainerSid), pbAppContainerTokenInfo.GetValue(), actualLength, &actualLength))
        return HRESULT_FROM_GetLastError();

    if (pcbAppContainerTokenInfo != nullptr)
        *pcbAppContainerTokenInfo = actualLength;

    return S_OK;
}

//---------------------------------------------------------------------------------------------
// Initializes a global AppXRTInfo structure if it has not already been initialized. Returns
// false only in the event of OOM; otherwise caches the result of ARI information in
// g_pAppXRTInfo. Thread safe.
static
HRESULT InitAppXRT()
{
    // This will be used for catastrophic errors.
    HRESULT hr = S_OK;

    if (VolatileLoad(&g_pAppXRTInfo) == nullptr)
    {
        NewHolder<AppXRTInfo> pAppXRTInfo = new (nothrow) AppXRTInfo();
        IfNullRet(pAppXRTInfo); // Catastrophic error.

        pAppXRTInfo->m_fIsAppXProcess = false;

        do
        {
            LPCWSTR wzAppXRTDll = W("api-ms-win-appmodel-runtime-l1-1-0.dll");
            // Does not use GetLoadWithAlteredSearchPathFlag() because that would cause infinite recursion.
            pAppXRTInfo->m_hAppXRTMod = WszLoadLibrary(wzAppXRTDll);
            if (pAppXRTInfo->m_hAppXRTMod == nullptr)
            {   // Error is catastrophic: can't find kernel32.dll?
                hr = HRESULT_FROM_GetLastError();
                break;
            }

            pAppXRTInfo->m_pfnGetCurrentPackageId = reinterpret_cast<GetCurrentPackageId_t *>(
                GetProcAddress(pAppXRTInfo->m_hAppXRTMod, "GetCurrentPackageId"));
            if (pAppXRTInfo->m_pfnGetCurrentPackageId == nullptr)
            {   // Error is non-catastrophic: could be running downlevel
                break;
            }

            pAppXRTInfo->m_pfnGetCurrentPackageInfo = reinterpret_cast<GetCurrentPackageInfo_t *>(
                GetProcAddress(pAppXRTInfo->m_hAppXRTMod, "GetCurrentPackageInfo"));
            if (pAppXRTInfo->m_pfnGetCurrentPackageInfo == nullptr)
            {   // Error is catastrophic: GetCurrentPackageId is available but not GetCurrentPackageInfo?
                hr = HRESULT_FROM_GetLastError();
                break;
            }

            pAppXRTInfo->m_pfnGetCurrentPackagePath = reinterpret_cast<GetCurrentPackagePath_t *>(
                GetProcAddress(pAppXRTInfo->m_hAppXRTMod, "GetCurrentPackagePath"));
            if (pAppXRTInfo->m_pfnGetCurrentPackagePath == nullptr)
            {   // Error is catastrophic: GetCurrentPackageInfo is available but not GetCurrentPackagePath?
                hr = HRESULT_FROM_GetLastError();
                break;
            }

            // Determine if this is an AppX process
            UINT32 cbBuffer = 0;
            LONG lRes = (*pAppXRTInfo->m_pfnGetCurrentPackageId)(&cbBuffer, nullptr);
            pAppXRTInfo->m_fIsAppXProcess = (lRes == ERROR_INSUFFICIENT_BUFFER);

            _ASSERTE(AppX::IsAppXSupported());

            hr = GetAppContainerTokenInfoForProcess(
                            GetCurrentProcessId(),
                            pAppXRTInfo->m_pbAppContainerInfo,
                            &pAppXRTInfo->m_cbAppContainerInfo);

            if (FAILED(hr))
            {
                if (pAppXRTInfo->m_fIsAppXProcess)
                {   // Error is catastrophic: running in true immersive process but no token info?
                }
                else
                {   // Error is non-catastrophic: reset HRESULT to S_OK.
                    hr = S_OK;
                }
                break;
            }
        }
        while (false);

        if (InterlockedCompareExchangeT<AppXRTInfo>(&g_pAppXRTInfo, pAppXRTInfo, nullptr) == nullptr)
        {
            pAppXRTInfo.SuppressRelease();
        }
    }

    return hr;
}

//---------------------------------------------------------------------------------------------
// Inline helper to check first an only init when required.
static inline HRESULT CheckInitAppXRT()
{
    return (VolatileLoad(&g_pAppXRTInfo) != nullptr) ? S_OK : InitAppXRT();
}

#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------------------------------
// Contains general helper methods for interacting with AppX functionality. This code will
// gracefully fail on downlevel OS by returning false from AppX::IsAppXProcess, so always
// call this API first to check before calling any of the others defined in this namespace.
//
// See http://windows/windows8/docs/Windows%208%20Feature%20Documents/Developer%20Experience%20(DEVX)/Apps%20Experience%20(APPX)/Modern%20Client/App%20Runtime%20Improvements%20API%20Developer%20Platform%20Spec.docm
// for more information.

namespace AppX
{
#ifdef DACCESS_COMPILE

    //-----------------------------------------------------------------------------------------
    // DAC-only IsAppXProcess. Returns false if g_pAppXRTInfo has not been initialized.
    bool DacIsAppXProcess()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (g_pAppXRTInfo != nullptr && g_pAppXRTInfo->m_fIsAppXProcess);
    }

#else // DACCESS_COMPILE

    //---------------------------------------------------------------------------------------------
    // cleans up resources allocated in InitAppXRT()
    void ShutDown()
    {
        if (VolatileLoad(&g_pAppXRTInfo) != nullptr)
        {
            delete g_pAppXRTInfo;
            g_pAppXRTInfo = nullptr;
        }
    }

    //-----------------------------------------------------------------------------------------
    // Returns true if the current process is immersive.
    // NOTE: a return value of true doesn't necessarily indicate that the process is a
    //       real Metro app, e.g. it could be an ngen process compiling an AppX assembly.
    bool IsAppXProcess()
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        if (FAILED(hr = CheckInitAppXRT()))
        {
            SetLastError(hr); // HRESULT_FROM_WIN32 is idempotent when error value is HRESULT.
            return false;
        }

        return g_pAppXRTInfo->m_fIsAppXProcess;
    }

    //-----------------------------------------------------------------------------------------
    // Returns true if the current process is immersive.
    // Only produces reliable results after IsAppXProcess is inititalized
    bool IsAppXProcess_Initialized_NoFault()
    {
        LIMITED_METHOD_CONTRACT;
        if (VolatileLoad(&g_pAppXRTInfo) == nullptr)
        {
            return false;
        }
        return g_pAppXRTInfo->m_fIsAppXProcess;
    }

    bool IsAppXNGen()
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoad(&g_pAppXRTInfo) != nullptr && g_pAppXRTInfo->m_fIsAppXNGen;
    }

    //-----------------------------------------------------------------------------------------
    HRESULT InitCurrentPackageInfoCache()
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        UINT32 cbBuffer = 0;
        hr = HRESULT_FROM_WIN32((*g_pAppXRTInfo->m_pfnGetCurrentPackageInfo)(PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, nullptr, nullptr));
        if (hr != E_INSUFFICIENT_BUFFER)
            return hr;

        NewArrayHolder<BYTE> pbBuffer(new (nothrow) BYTE[cbBuffer]);
        IfNullRet(pbBuffer);

        UINT32 nCount = 0;
        IfFailRet(HRESULT_FROM_WIN32((*g_pAppXRTInfo->m_pfnGetCurrentPackageInfo)(PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, pbBuffer, &nCount)));

        NewHolder<AppXRTInfo::CurrentPackageInfo> pPkgInfo(
            new (nothrow) AppXRTInfo::CurrentPackageInfo(cbBuffer, pbBuffer.Extract(), nCount));
        IfNullRet(pPkgInfo);

        if (InterlockedCompareExchangeT<AppXRTInfo::CurrentPackageInfo>(
                &g_pAppXRTInfo->m_pCurrentPackageInfo, pPkgInfo, nullptr) == nullptr)
        {
            pPkgInfo.SuppressRelease();
        }

        return S_OK;
    }

    //-----------------------------------------------------------------------------------------
    FORCEINLINE HRESULT CheckInitCurrentPackageInfoCache()
    {
        WRAPPER_NO_CONTRACT;
        PRECONDITION(IsAppXProcess());

        if (!IsAppXProcess())
            return E_UNEXPECTED;

        if (g_pAppXRTInfo->m_pCurrentPackageInfo == nullptr)
            return InitCurrentPackageInfoCache();
        else
            return S_OK;
    }

    //-----------------------------------------------------------------------------------------
    LPCWSTR GetHeadPackageMoniker()
    {
        STANDARD_VM_CONTRACT;

        IfFailThrow(CheckInitCurrentPackageInfoCache());
        return reinterpret_cast<PPACKAGE_INFO>(
            g_pAppXRTInfo->m_pCurrentPackageInfo->m_pbCurrentPackageInfo)->packageFullName;
    }

    //-----------------------------------------------------------------------------------------
    // Returns the current process' PACKAGE_ID in the provided buffer. See the ARI spec (above)
    // for more information.
    HRESULT GetCurrentPackageId(
        PUINT32 pBufferLength,
        PBYTE pBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        IfFailRet(CheckInitAppXRT());
        IfFailRet(HRESULT_FROM_WIN32((*g_pAppXRTInfo->m_pfnGetCurrentPackageId)(pBufferLength, pBuffer)));

        return S_OK;
    }

    //-----------------------------------------------------------------------------------------
    // Returns the current process' PACKAGE_INFO in the provided buffer. See the ARI spec
    // (above) for more information.
    HRESULT GetCurrentPackageInfo(
        UINT32 uiFlags,
        PUINT32 pcbBuffer,
        PBYTE pbBuffer,
        PUINT32 pnCount)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(IsAppXProcess());
        PRECONDITION(CheckPointer(pcbBuffer));

        HRESULT hr = S_OK;

        IfFailRet(CheckInitAppXRT());

        if (pcbBuffer == nullptr)
            return E_INVALIDARG;

        if (uiFlags == PACKAGE_FILTER_CLR_DEFAULT)
        {
            IfFailRet(CheckInitCurrentPackageInfoCache());

            DWORD cbBuffer = *pcbBuffer;
            *pcbBuffer = g_pAppXRTInfo->m_pCurrentPackageInfo->m_cbCurrentPackageInfo;
            if (pnCount != nullptr)
            {
                *pnCount = g_pAppXRTInfo->m_pCurrentPackageInfo->m_nCount;
            }

            if (pbBuffer == nullptr || cbBuffer < g_pAppXRTInfo->m_pCurrentPackageInfo->m_cbCurrentPackageInfo)
            {
                return E_INSUFFICIENT_BUFFER;
            }
            memcpy(pbBuffer, g_pAppXRTInfo->m_pCurrentPackageInfo->m_pbCurrentPackageInfo, g_pAppXRTInfo->m_pCurrentPackageInfo->m_cbCurrentPackageInfo);
        }
        else
        {
            IfFailRet(HRESULT_FROM_WIN32((*g_pAppXRTInfo->m_pfnGetCurrentPackageInfo)(uiFlags, pcbBuffer, pbBuffer, pnCount)));
        }

        return S_OK;
    }

    //-----------------------------------------------------------------------------------------
    bool IsAdaptiveApp()
    {
         LIMITED_METHOD_CONTRACT;
        
         HRESULT hr = S_OK;
         static bool cachedIsAdaptiveApp = false;
   
         if (!IsAppXProcess())
         {
             return false;
         }

         if (!cachedIsAdaptiveApp)
         {
             cachedIsAdaptiveApp = true;
             LPWSTR adaptiveAppWinmetaDataDir = NULL;
   
             if (SUCCEEDED(hr = AppX::GetWinMetadataDirForAdaptiveApps(&adaptiveAppWinmetaDataDir)))
             {
                 g_pAppXRTInfo->m_fIsAppXAdaptiveApp = clr::fs::Dir::Exists(adaptiveAppWinmetaDataDir);
                            
             }
             else
             {
                 SetLastError(hr); 
                 g_pAppXRTInfo->m_fIsAppXAdaptiveApp = false;
             }
                    
         }

         return g_pAppXRTInfo->m_fIsAppXAdaptiveApp;
    }
    
   
    //-----------------------------------------------------------------------------------------
    // length       : Upon success, contains the  the length of packagePath
    // [in/out]       
    //
    // packageRoot  : Upon success, contains the  full packagePath 
    // [out]          [ Note: The memory has to be preallocated for the above length]
    //
    HRESULT GetCurrentPackagePath(_Inout_ UINT32* length, _Out_opt_ PWSTR packageRoot)
    {
            PRECONDITION(IsAppXProcess());
            PRECONDITION(CheckPointer(length));

            HRESULT hr;
            IfFailRet(CheckInitAppXRT());
           
            IfFailRet(HRESULT_FROM_WIN32((*g_pAppXRTInfo->m_pfnGetCurrentPackagePath)(length, packageRoot)));
            return S_OK;
    }

    //-----------------------------------------------------------------------------------------
    // winMetadDataDir        : Upon success, contains the absolute path for the winmetadata directory for the adaptive app
    // [out]                    NOTE: The string the pointer points to is global memory and never should be modified
    //
    HRESULT GetWinMetadataDirForAdaptiveApps(_Out_ LPWSTR* winMetadDataDir)
    {

            PRECONDITION(IsAppXProcess());
            
            HRESULT hr;

            IfFailRet(CheckInitAppXRT());

            if (g_pAppXRTInfo->m_AdaptiveAppWinmetadataDir == nullptr)
            {
                LPCWSTR wzWinMetadataFolder=W("\\WinMetadata");
                NewArrayHolder<WCHAR> wzCompletePath;
                UINT32 length=0;

                hr = GetCurrentPackagePath(&length, NULL);
                if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                    return hr;

                NewArrayHolder<WCHAR> wzPath_holder = new (nothrow) WCHAR[length];  

                IfNullRet(wzPath_holder);
                IfFailRet(GetCurrentPackagePath(&length, wzPath_holder.GetValue()));

                DWORD cchFullPathBuf = length + (DWORD)wcslen(wzWinMetadataFolder) + 1;
                IfNullRet(wzCompletePath = new (nothrow) WCHAR[cchFullPathBuf]);
                IfFailRet(clr::fs::Path::Combine(wzPath_holder.GetValue(), wzWinMetadataFolder, &cchFullPathBuf, wzCompletePath.GetValue()));
                g_pAppXRTInfo->m_AdaptiveAppWinmetadataDir = wzCompletePath.Extract();
            }

            *winMetadDataDir = g_pAppXRTInfo->m_AdaptiveAppWinmetadataDir; 

            return S_OK;
    }

#if defined(FEATURE_APPX_BINDER)
    //-----------------------------------------------------------------------------------------
    // Iterates the current process' packages and returns the first package that contains a
    // file matching wzFileName.
    //
    // wzFileName   :  The file to look for in the current process' packages. If this is a
    // [in]           relative path, then it appends the path to the root of each package in
    //                sequence to see if the resulting path points to an existing file. If this
    //                is an absolute path, then for each package it determines if the package
    //                root is a prefix of wzFileName.
    // pcchPathName : Upon entry contains the length of the buffer pwzPathName, and upon return
    // [in/out]       contains either the number of characters written or the buffer size
    //                required.
    // pwzPathName  : Upon success, contains the absolute path for the first matching file.
    // [out]
    HRESULT FindFileInCurrentPackage(
        PCWSTR wzFileName,
        PUINT32 pcchPathName,
        PWSTR pwzPathName,
        UINT32 uiFlags,
        __in PCWSTR *rgwzAltPaths,
        __in UINT32 cAltPaths,
        FindFindInPackageFlags findInCurrentPackageFlags)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(IsAppXProcess());
        PRECONDITION(CheckPointer(wzFileName));
        PRECONDITION(CheckPointer(pcchPathName));
        PRECONDITION(CheckPointer(pwzPathName));

        HRESULT hr = S_OK;

        if (!IsAppXProcess())
            return E_UNEXPECTED;

        // PACKAGE_FILTER_ALL_LOADED is obsolete, and shouldn't be used.
        // We also don't currently handle the case where PACKAGE_FILTER_HEAD isn't set.
        _ASSERTE(uiFlags != PACKAGE_FILTER_ALL_LOADED && (uiFlags & PACKAGE_FILTER_HEAD) != 0);
        if (uiFlags == PACKAGE_FILTER_ALL_LOADED || (uiFlags & PACKAGE_FILTER_HEAD) == 0)
            return E_NOTIMPL;

        // File name must be non-null and relative
        if (IsNullOrEmpty(wzFileName) || pcchPathName == nullptr || pwzPathName == nullptr)
            return E_INVALIDARG;

        // If we've been provided a full path and the file doesn't actually exist
        // then we can immediately say that this function will fail with "file not found".
        bool fIsRelative = clr::fs::Path::IsRelative(wzFileName);
        if (!fIsRelative && !clr::fs::File::Exists(wzFileName))
            return E_FILE_NOT_FOUND;

        IfFailRet(CheckInitCurrentPackageInfoCache());

        DWORD const           cchFileName    = static_cast<DWORD>(wcslen(wzFileName));
        DWORD                 cchFullPathBuf = _MAX_PATH;
        NewArrayHolder<WCHAR> wzFullPathBuf  = new (nothrow) WCHAR[cchFullPathBuf];
        IfNullRet(wzFullPathBuf);

        auto FindFileInCurrentPackageHelper = [&](LPCWSTR wzPath) -> HRESULT
        {
            HRESULT hr = S_OK;

            if (!(findInCurrentPackageFlags & FindFindInPackageFlags_AllowLongFormatPath) && clr::fs::Path::HasLongFormatPrefix(wzPath))
                return COR_E_BAD_PATHNAME; // We can't handle long format paths.

            // If the path is relative, concatenate the package root and the file name and see
            // if the file exists.
            if (fIsRelative)
            {
                DWORD cchFullPath = cchFullPathBuf;
                hr = clr::fs::Path::Combine(wzPath, wzFileName, &cchFullPath, wzFullPathBuf);
                if (hr == E_INSUFFICIENT_BUFFER)
                {
                    IfNullRet(wzFullPathBuf = new (nothrow) WCHAR[cchFullPathBuf = (cchFullPath + 1)]);
                    hr = clr::fs::Path::Combine(wzPath, wzFileName, &cchFullPath, wzFullPathBuf);
                }
                IfFailRet(hr);

                if (!clr::fs::Path::IsValid(wzFullPathBuf, cchFullPath, !!(findInCurrentPackageFlags & FindFindInPackageFlags_AllowLongFormatPath)))
                    return COR_E_BAD_PATHNAME;

                if (clr::fs::File::Exists(wzFullPathBuf))
                {
                    DWORD cchPathName = *pcchPathName;
                    *pcchPathName = cchFullPath;
                    return StringCchCopy(pwzPathName, cchPathName, wzFullPathBuf);
                }
            }
            // If the path is absolute, see if the file name contains the pacakge root as a prefix
            // and if the file exists.
            else
            {
                DWORD cchPath = static_cast<DWORD>(wcslen(wzPath));

                // Determine if wzPath is a path prefix of wzFileName
                if (cchPath < cchFileName &&
                    _wcsnicmp(wzPath, wzFileName, cchPath) == 0 &&
                    (wzFileName[cchPath] == W('\\') || wzPath[cchPath-1] == W('\\'))) // Ensure wzPath is not just a prefix, but a path prefix
                {
                    if (clr::fs::File::Exists(wzFileName))
                    {
                        DWORD cchPathName = *pcchPathName;
                        *pcchPathName = cchFileName;
                        return StringCchCopy(pwzPathName, cchPathName, wzFileName);
                    }
                }    
            }

            return S_FALSE;
        }; // FindFileInCurrentPackageHelper

        if (!(findInCurrentPackageFlags & FindFindInPackageFlags_SkipCurrentPackageGraph))
        {
            PCPACKAGE_INFO pCurNode = reinterpret_cast<PPACKAGE_INFO>(
                g_pAppXRTInfo->m_pCurrentPackageInfo->m_pbCurrentPackageInfo);
            PCPACKAGE_INFO pEndNode = pCurNode + g_pAppXRTInfo->m_pCurrentPackageInfo->m_nCount;
            for (; pCurNode != pEndNode; ++pCurNode)
            {
                IfFailRet(FindFileInCurrentPackageHelper(pCurNode->path));

                if (hr == S_OK)
                {
                    return hr;
                }

                // End search if dependent packages should not be checked.
                if ((uiFlags & PACKAGE_FILTER_DIRECT) == 0)
                {
                    break;
                }
            }
        }

        // Process alternative paths
        for (UINT iAltPath = 0; iAltPath < cAltPaths; iAltPath++)
        {
            IfFailRet(FindFileInCurrentPackageHelper(rgwzAltPaths[iAltPath]));

            if (hr == S_OK)
            {
                return hr;
            }
        }

        return E_FILE_NOT_FOUND;
    }
#endif // FEATURE_APPX_BINDER

    //-----------------------------------------------------------------------------------------
    HRESULT GetAppContainerTokenInfoForProcess(
        DWORD dwPid,
        NewArrayHolder<BYTE>& pbAppContainerTokenInfo,
        DWORD* pcbAppContainerTokenInfo)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        pbAppContainerTokenInfo = nullptr;

        if (!IsAppXSupported())
        {
            return S_FALSE;
        }

        if (dwPid == GetCurrentProcessId())
        {
            if (FAILED(hr = CheckInitAppXRT()))
            {
                SetLastError(hr);
                return S_FALSE;
            }
            
            if (g_pAppXRTInfo->m_pbAppContainerInfo == nullptr)
            {
                return S_FALSE;
            }
            else
            {
                pbAppContainerTokenInfo = g_pAppXRTInfo->m_pbAppContainerInfo.GetValue();
                pbAppContainerTokenInfo.SuppressRelease(); // Caller does not need to free mem.
                if (pcbAppContainerTokenInfo != nullptr)
                {
                    *pcbAppContainerTokenInfo = g_pAppXRTInfo->m_cbAppContainerInfo;
                }
                return S_OK;
            }
        }
        else
        {
            return ::GetAppContainerTokenInfoForProcess(dwPid, pbAppContainerTokenInfo, pcbAppContainerTokenInfo);
        }
    }

    // Called during NGen to pretend that we're in a certain package.  Due to Windows restriction, we can't
    // start NGen worker processes in the right package environment (only in the right AppContainer).  So
    // NGen calls this function with a package name, to indicate that all AppX-related code should behave as
    // if the current process is running in that package.
    HRESULT SetCurrentPackageForNGen(__in PCWSTR pszPackageFullName)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        IfFailRet(CheckInitAppXRT());

        _ASSERTE(IsAppXSupported());
        _ASSERTE(g_pAppXRTInfo->m_hAppXRTMod != nullptr);

        HMODULE hAppXRTMod = g_pAppXRTInfo->m_hAppXRTMod;
        OpenPackageInfoByFullName_t *pfnOpenPackageInfoByFullName
            = reinterpret_cast<OpenPackageInfoByFullName_t*>(GetProcAddress(hAppXRTMod, "OpenPackageInfoByFullName"));
        _ASSERTE(pfnOpenPackageInfoByFullName != nullptr);
        if (pfnOpenPackageInfoByFullName == nullptr)
            return HRESULT_FROM_GetLastError();

        ClosePackageInfo_t *pfnClosePackageInfo
            = reinterpret_cast<ClosePackageInfo_t*>(GetProcAddress(hAppXRTMod, "ClosePackageInfo"));
        _ASSERTE(pfnClosePackageInfo != nullptr);
        if (pfnClosePackageInfo == nullptr)
            return HRESULT_FROM_GetLastError();

        GetPackageInfo_t *pfnGetPackageInfo
            = reinterpret_cast<GetPackageInfo_t*>(GetProcAddress(hAppXRTMod, "GetPackageInfo"));
        _ASSERTE(pfnGetPackageInfo != nullptr);
        if (pfnGetPackageInfo == nullptr)
            return HRESULT_FROM_GetLastError();

        PACKAGE_INFO_REFERENCE packageInfoReference;
        hr = HRESULT_FROM_WIN32(pfnOpenPackageInfoByFullName(pszPackageFullName, 0, &packageInfoReference));
        if (FAILED(hr))
            return hr;

        // Automatically close packageInfoReference before we return.
        class PackageInfoReferenceHolder
        {
        public:
            PackageInfoReferenceHolder(ClosePackageInfo_t *pfnClosePackageInfo, PACKAGE_INFO_REFERENCE &packageInfoReference)
                :m_pfnClosePackageInfo(pfnClosePackageInfo),
                 m_packageInfoReference(packageInfoReference)
            {
            }
            ~PackageInfoReferenceHolder() { m_pfnClosePackageInfo(m_packageInfoReference); }
        private:
            ClosePackageInfo_t *m_pfnClosePackageInfo;
            PACKAGE_INFO_REFERENCE &m_packageInfoReference;
        } pirh(pfnClosePackageInfo, packageInfoReference);

        UINT32 cbBuffer = 0;
        hr = HRESULT_FROM_WIN32(pfnGetPackageInfo(packageInfoReference, PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, nullptr, nullptr));
        if (hr != E_INSUFFICIENT_BUFFER)
            return hr;

        NewArrayHolder<BYTE> pbBuffer(new (nothrow) BYTE[cbBuffer]);
        IfNullRet(pbBuffer);

        UINT32 nCount = 0;
        IfFailRet(HRESULT_FROM_WIN32(pfnGetPackageInfo(packageInfoReference, PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, pbBuffer, &nCount)));

        NewHolder<AppXRTInfo::CurrentPackageInfo> pPkgInfo(
            new (nothrow) AppXRTInfo::CurrentPackageInfo(cbBuffer, pbBuffer.Extract(), nCount));
        IfNullRet(pPkgInfo);

        if (InterlockedCompareExchangeT<AppXRTInfo::CurrentPackageInfo>(
                &g_pAppXRTInfo->m_pCurrentPackageInfo, pPkgInfo, nullptr) == nullptr)
        {
            pPkgInfo.SuppressRelease();
        }

        g_pAppXRTInfo->m_fIsAppXProcess = true;
        g_pAppXRTInfo->m_fIsAppXNGen = true;

        return hr;
    }

#endif // DACCESS_COMPILE
} // namespace AppX


#endif // FEATURE_CORECLR
