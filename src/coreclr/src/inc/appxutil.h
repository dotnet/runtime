// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


#pragma once

#ifdef FEATURE_APPX

#include "clrtypes.h"
#include "appmodel.h"
#include "fusionsetup.h"

#define PACKAGE_FILTER_CLR_DEFAULT (PACKAGE_FILTER_HEAD|PACKAGE_FILTER_DIRECT)


typedef PACKAGE_INFO *              PPACKAGE_INFO;
typedef PACKAGE_INFO const *        PCPACKAGE_INFO;

//---------------------------------------------------------------------------------------------
// Forward declarations
template <typename T>
class NewArrayHolder;
BOOL WinRTSupported();

#ifdef FEATURE_CORECLR

namespace AppX
{
    // Returns true if process is immersive (or if running in mockup environment).
    bool IsAppXProcess();

    // On CoreCLR, the host is in charge of determining whether the process is AppX or not.
    void SetIsAppXProcess(bool);

    inline bool IsAppXNGen()
    {
        WRAPPER_NO_CONTRACT;
        return false;
    }

#ifdef DACCESS_COMPILE
        bool DacIsAppXProcess();
#endif // DACCESS_COMPILE
};

#else // FEATURE_CORECLR

struct AppXRTInfo;
typedef DPTR(AppXRTInfo) PTR_AppXRTInfo;

//---------------------------------------------------------------------------------------------
namespace AppX
{
    // cleans up resources allocated in InitAppXRT()
    void ShutDown();

    // Returns true if process is immersive (or if running in mockup environment).
    // NOTE: a return value of true doesn't necessarily indicate that the process is a
    //       real Metro app, e.g. it could be an ngen process compiling an AppX assembly.
    bool IsAppXProcess();

#ifdef DACCESS_COMPILE
    bool DacIsAppXProcess();
#endif // DACCESS_COMPILE

    // Returns true if process is immersive (or if running in mockup environment).
    // Use only in NOFAULT regions when you are 100% sure that code:IsAppXProcess has been already called.
    // This function does not initialize (no faults).
    bool IsAppXProcess_Initialized_NoFault();

    // Returns true if process is NGen worker compiling an AppX assembly.
    bool IsAppXNGen();

    // Returns true if the host OS supports immersive apps.
    inline bool IsAppXSupported()
    { return WinRTSupported() != FALSE; }

    LPCWSTR GetHeadPackageMoniker();

    HRESULT GetCurrentPackageId(
        __inout PUINT32 pBufferLength,
        __out PBYTE pBuffer);

    HRESULT GetCurrentPackageInfo(
        __in UINT32 dwFlags,
        __inout PUINT32 pcbBuffer,
        __out PBYTE pbBuffer,
        __out PUINT32 nCount);

    bool IsAdaptiveApp();
    HRESULT GetCurrentPackageRoot(_Inout_ UINT32* length, _Out_opt_ PWSTR packageRoot);
    HRESULT GetWinMetadataDirForAdaptiveApps(_Out_ LPWSTR* winMetadDataDir);
#ifdef FEATURE_APPX_BINDER
    enum FindFindInPackageFlags
    {
        FindFindInPackageFlags_None = 0,
        FindFindInPackageFlags_AllowLongFormatPath = 1,
        FindFindInPackageFlags_SkipCurrentPackageGraph = 2, // Only search in alt path
    };

    // If the function succeeds, pcchPathName is set to the length of the string that is copied to the buffer,
    // in characters, including the terminating null character, and the function returns S_OK. If the buffer
    // is too small, pcchPathName is set to the length of the buffer required (in characters),
    // including the terminating null character, and the function returns ERROR_INSUFFICIENT_BUFFER.
    HRESULT FindFileInCurrentPackage(
        __in PCWSTR pszFileName,
        __inout PUINT32 pcchPathName,
        __out PWSTR pszPathName,
        __in UINT32 uiFlags = PACKAGE_FILTER_CLR_DEFAULT,
        __in PCWSTR *rgwzAltPaths = NULL,
        __in UINT32 cAltPaths = 0,
        FindFindInPackageFlags findInCurrentPackageFlags = FindFindInPackageFlags_None);
#endif // FEATURE_APPX_BINDER

    // Attempts to retrieve the AppContainer SID for the specified process.
    // For non-AppContainer processes the function will return S_FALSE and pAppContainerTokenInfo will be NULL.
    // For AppContainer processes the function will return S_OK and pAppContainerTokenInfo will contain data.
    // Note that there might be legitimate cases where this function fails (caller doesn't have permissions to
    // OpenProcess() for example) so any callers must account for such failures.
    // Use of NewArrayHolder permits method to reuse info for current process (dwPid == self) or to allocate
    // memory (dwPid != self). Cast the result to PTOKEN_APPCONTAINER_INFORMATION;
    HRESULT GetAppContainerTokenInfoForProcess(
        DWORD dwPid,
        NewArrayHolder<BYTE>& pbAppContainerTokenInfo, // Cast to PTOKEN_APPCONTAINER_INFORMATION on return.
        DWORD* pcbAppContainerTokenInfo = nullptr);

    // Called during NGen to pretend that we are in a certain package.
    HRESULT SetCurrentPackageForNGen(__in PCWSTR pszPackageFullName);
}

#endif // FEATURE_CORECLR

#else // FEATURE_APPX

namespace AppX
{
    inline bool IsAppXProcess()
    {
        return false;
    }
}

#endif // FEATURE_APPX
