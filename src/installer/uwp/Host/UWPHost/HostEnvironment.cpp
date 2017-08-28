//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// CoreCLR Host that is used in activating CoreCLR in 
// UWP apps' F5/development time scenarios
//

#include "windows.h"
#include "HostEnvironment.h"

// This should ideally come from WINDOWS SDK header but is missing in earlier versions
#ifndef PACKAGE_FILTER_IS_IN_RELATED_SET 
#define PACKAGE_FILTER_IS_IN_RELATED_SET 0x40000
#endif


HRESULT HostEnvironment::TryLoadCoreCLR() {

    m_coreCLRModule = ::LoadLibraryEx(coreCLRDll, NULL, 0);
    if (!m_coreCLRModule) {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Pin the module - CoreCLR.dll does not support being unloaded.
    // N.B.: HostEnvironment is not calling ::FreeLibrary on this loaded module since we're pinning it anyway.
    // If unloading CoreCLR is ever supported and the pinning below is to be removed please make sure that 
    // the ::FreeLibrary is called appropriately.
    HMODULE dummy_coreCLRModule;
    if (!::GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_PIN, coreCLRDll, &dummy_coreCLRModule)) 
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (!GetModuleFileNameW(m_coreCLRModule, m_coreCLRInstallDirectory, MAX_PATH))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }
    // Find out where the last backslash is, and trim the path to get the directory containing coreclr.dll
    wchar_t *lastBackSlash = wcsrchr(m_coreCLRInstallDirectory, L'\\');
    if (lastBackSlash != nullptr)
    {
        *(lastBackSlash+1) = L'\0';
    }
    else
    {
        memset(m_coreCLRInstallDirectory, 0, MAX_PATH * sizeof(wchar_t)); 
        return E_FAIL;
    }

    return S_OK;
}


HRESULT HostEnvironment::Initialize()
{
    HRESULT hr = S_OK;
    // 
    // Get the package root. 
    // This is also our check for the App to be an AppX app.
    // TODO: Not all AppX apps are UWP apps. Add validation for UWP apps.
    //
    UINT32 length = 0;
    LONG rc = GetCurrentPackagePath(&length, NULL);
    if (rc != ERROR_INSUFFICIENT_BUFFER)
    {
        return HRESULT_FROM_WIN32(rc);
    }

    m_packageRoot = (PWSTR) malloc(length * sizeof(*m_packageRoot));
    if (m_packageRoot == NULL)
    {
        return E_FAIL;
    }

    // Get the actual path to the package root from the path to the current module.
    rc = GetCurrentPackagePath(&length, m_packageRoot);
    IfFailWin32Ret(rc);

    m_fIsAppXPackage = true;

    //
    // Query the package full name
    //
    length = 0;
    rc = GetCurrentPackageFullName(&length, NULL);
    if (rc != ERROR_INSUFFICIENT_BUFFER)
    {
        return HRESULT_FROM_WIN32(rc);
    }

    m_currentPackageFullName = (PWSTR) malloc(length * sizeof(*m_currentPackageFullName));
    if (m_currentPackageFullName == NULL)
    {
        return HRESULT_FROM_WIN32(rc);
    }

    rc = GetCurrentPackageFullName(&length, m_currentPackageFullName);
    IfFailWin32Ret(rc);
    
    // Try to load CoreCLR
    IfFailRet(TryLoadCoreCLR());


    PACKAGE_INFO_REFERENCE packageInfoReference = {0};
    rc = OpenPackageInfoByFullName(m_currentPackageFullName, 0 , &packageInfoReference);
    IfFailWin32Ret(rc);

    length = 0;
    rc = GetPackageInfo(packageInfoReference, PACKAGE_FILTER_DIRECT|PACKAGE_FILTER_IS_IN_RELATED_SET, &length, NULL, &m_dependentPackagesCount);
    IfFalseWin32Goto(rc == ERROR_INSUFFICIENT_BUFFER, rc, ErrExit2);

    BYTE* buffer = (BYTE *) malloc(length);
    IfFalseGoto(buffer != NULL, E_FAIL, ErrExit2);

    m_dependentPackagesRootPaths = (wchar_t**) malloc(m_dependentPackagesCount * sizeof(wchar_t*));
    IfFalseGo(m_dependentPackagesRootPaths != NULL, E_FAIL);
    memset(m_dependentPackagesRootPaths, 0, m_dependentPackagesCount * sizeof(wchar_t*));

    rc = GetPackageInfo(packageInfoReference, PACKAGE_FILTER_DIRECT|PACKAGE_FILTER_IS_IN_RELATED_SET, &length, buffer, &m_dependentPackagesCount);
    IfFailWin32Go(rc);

    const PACKAGE_INFO *packageInfo = (PACKAGE_INFO *) buffer;
    for (UINT32 i=0; i<m_dependentPackagesCount; i++, ++packageInfo)
    {
        length = 0;
        rc = GetPackagePathByFullName(packageInfo->packageFullName, &length, NULL);
        IfFalseWin32Go(rc == ERROR_INSUFFICIENT_BUFFER, rc);
        
        // +1 below is to append a '\' at the end of the string if it's not already ending with one.
        m_dependentPackagesRootPaths[i] = (wchar_t*) malloc((length+1) * sizeof(wchar_t));
        IfFalseGo(m_dependentPackagesRootPaths[i] != NULL, E_FAIL);

        rc = GetPackagePathByFullName(packageInfo->packageFullName, &length, m_dependentPackagesRootPaths[i]);
        IfFailWin32Go(rc);

        // Note: length includes the null-terminator. So the last char is at index (length-2)
        wchar_t* lastBackslash = wcsrchr(m_dependentPackagesRootPaths[i], L'\\');
        if ((lastBackslash != nullptr) && ((UINT32)(lastBackslash - m_dependentPackagesRootPaths[i] + 1) != length - 2))
        {
            wcsncat_s(m_dependentPackagesRootPaths[i], length+1, L"\\", 1);
        }
    }


    return S_OK;

ErrExit:
    free(buffer);
ErrExit2:
    ClosePackageInfo(packageInfoReference);
    return hr;
}

HostEnvironment::HostEnvironment() 
    : m_CLRRuntimeHost(nullptr), m_coreCLRModule(NULL), m_fIsAppXPackage(false), 
      m_dependentPackagesRootPaths(NULL), m_dependentPackagesCount(0)
{
}

HostEnvironment::~HostEnvironment() 
{
    if (m_packageRoot)
    {
        free(m_packageRoot);
        m_packageRoot = NULL;
    }
    if (m_dependentPackagesCount > 0 && m_dependentPackagesRootPaths != NULL)
    {
        for (UINT32 i=0;i<m_dependentPackagesCount;i++)
        {
            if (m_dependentPackagesRootPaths[i] != NULL)
                free(m_dependentPackagesRootPaths[i]);
        }
        free(m_dependentPackagesRootPaths);
    }


    if (m_currentPackageFullName)
    {
        free(m_currentPackageFullName);
        m_currentPackageFullName = NULL;
    }
}

void HostEnvironment::InitializeTPAList(_In_reads_(tpaEntriesCount) wchar_t** tpaEntries, int tpaEntriesCount)
{
    wchar_t assemblyPath[MAX_PATH];
    wchar_t *targetPath = GetCoreCLRInstallPath();
    const size_t dirLength = wcslen(targetPath);
    
    for (int i= 0; i < tpaEntriesCount; i++)
    {
        wcscpy_s(assemblyPath, MAX_PATH, targetPath);

        wchar_t* const fileNameBuffer = assemblyPath + dirLength;
        const size_t fileNameBufferSize = MAX_PATH - dirLength;

        wcscat_s(assemblyPath, tpaEntries[i]);
        
        WIN32_FIND_DATA data;
        HANDLE findHandle = FindFirstFile(assemblyPath, &data);
        if (findHandle != INVALID_HANDLE_VALUE) {
            do {
                if (!(data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
                    wchar_t* fileName = data.cFileName;
                    const size_t fileLength = wcslen(data.cFileName);
                    
                    m_tpaList.Append(targetPath, dirLength);
                    m_tpaList.Append(fileName, fileLength);
                    m_tpaList.Append(L";", 1);
                }
            } while (0 != FindNextFile(findHandle, &data));

            FindClose(findHandle);
        }
    }
}

// Returns the ICLRRuntimeHost2 instance, loading it from CoreCLR.dll if necessary, or nullptr on failure.
ICLRRuntimeHost2* HostEnvironment::GetCLRRuntimeHost(HRESULT &hResult) {
    if (!m_CLRRuntimeHost) {

        if (!m_coreCLRModule) {
            hResult = E_FAIL;
            return nullptr;
        }

        FnGetCLRRuntimeHost pfnGetCLRRuntimeHost = 
            (FnGetCLRRuntimeHost)::GetProcAddress(m_coreCLRModule, "GetCLRRuntimeHost");
        if (!pfnGetCLRRuntimeHost) {
            hResult = E_INVALIDARG;
            return nullptr;
        }

        HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost2, (IUnknown**)&m_CLRRuntimeHost);
        if (FAILED(hr)) {
            hResult = hr;
            return nullptr;
        }
    }

    hResult = S_OK;
    return m_CLRRuntimeHost;
}



