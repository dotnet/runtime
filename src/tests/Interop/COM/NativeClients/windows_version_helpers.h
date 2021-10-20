// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "windows.h"

inline HRESULT is_windows_nano()
{
    OSVERSIONINFOEXW osVersionInfo;
    osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
#pragma warning(push)
#pragma warning(disable: 4996) // GetVersionExW is deprecated.
    if (!GetVersionExW((LPOSVERSIONINFOW)&osVersionInfo))
#pragma warning(pop)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    DWORD productKind;
    if (!GetProductInfo(
        osVersionInfo.dwMajorVersion,
        osVersionInfo.dwMinorVersion,
        osVersionInfo.wServicePackMajor,
        osVersionInfo.wServicePackMinor,
        &productKind))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (productKind == PRODUCT_IOTUAP)
    {
        return S_FALSE;
    }

    DWORD dataSize;
    DWORD type;
    if (!RegGetValueW(HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
        L"InstallationType",
        RRF_RT_REG_SZ,
        &type,
        nullptr,
        &dataSize)
        || type != REG_SZ)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    LPWSTR installationType = new WCHAR[dataSize + 1];

    if (!RegGetValueW(HKEY_LOCAL_MACHINE,
        L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
        L"InstallationType",
        RRF_RT_REG_SZ,
        &type,
        installationType,
        &dataSize)
        || type != REG_SZ)
    {
        delete[] installationType;
        return HRESULT_FROM_WIN32(GetLastError());
    }

    bool isNano = _wcsnicmp(L"Nano Server", installationType, dataSize + 1) == 0;
    delete[] installationType;
    return isNano ? S_OK : S_FALSE;
}
