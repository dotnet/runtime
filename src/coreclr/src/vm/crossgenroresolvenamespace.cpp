// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+----------------------------------------------------------------------------
//

//
//  Adapted from Windows sources.  Modified to run on Windows version < Win8, so
//  that we can use this in CrossGen.
//

//
//-----------------------------------------------------------------------------

#include "common.h"
#include "crossgenroresolvenamespace.h"
#include "stringarraylist.h"

namespace Crossgen
{

#define WINDOWS_NAMESPACE W("Windows")
#define WINDOWS_NAMESPACE_PREFIX WINDOWS_NAMESPACE W(".")
#define WINMD_FILE_EXTENSION_L       W(".winmd")

StringArrayList* g_wszWindowsNamespaceDirectories;
StringArrayList* g_wszUserNamespaceDirectories;

BOOL
IsWindowsNamespace(const WCHAR * wszNamespace)
{
    LIMITED_METHOD_CONTRACT;

    if (wcsncmp(wszNamespace, WINDOWS_NAMESPACE_PREFIX, (_countof(WINDOWS_NAMESPACE_PREFIX) - 1)) == 0)
    {
        return TRUE;
    }
    else if (wcscmp(wszNamespace, WINDOWS_NAMESPACE) == 0)
    {
        return TRUE;
    }

    return FALSE;
}


BOOL
DoesFileExist(
    const WCHAR * wszFileName)
{
    LIMITED_METHOD_CONTRACT;

    BOOL  fFileExists = TRUE;
    DWORD dwFileAttributes;
    dwFileAttributes = GetFileAttributesW(wszFileName);

    if ((dwFileAttributes == INVALID_FILE_ATTRIBUTES) ||
        (dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        fFileExists = FALSE;
    }

    return fFileExists;
}


HRESULT
FindNamespaceFileInDirectory(
    const WCHAR * wszNamespace,
    const WCHAR * wszDirectory,
    DWORD *       pcMetadataFiles,
    SString **    ppMetadataFiles)
{
    LIMITED_METHOD_CONTRACT;

    if (wszDirectory == nullptr)
        return ERROR_NOT_SUPPORTED;

    WCHAR wszFilePath[MAX_LONGPATH + 1];
    wcscpy_s(
        wszFilePath,
        _countof(wszFilePath),
        wszDirectory);

    WCHAR * wszFirstFileNameChar = wszFilePath + wcslen(wszFilePath);

    // If there's no backslash, add one.
    if (*(wszFirstFileNameChar - 1) != '\\')
        *wszFirstFileNameChar++ = '\\';

    WCHAR wszRemainingNamespace[MAX_PATH_FNAME +1];
    wcscpy_s(
        wszRemainingNamespace,
        _countof(wszRemainingNamespace),
        wszNamespace);

    do
    {
        *wszFirstFileNameChar = W('\0');
        wcscat_s(
            wszFilePath,
            _countof(wszFilePath),
            wszRemainingNamespace);
        wcscat_s(
            wszFilePath,
            _countof(wszFilePath),
            WINMD_FILE_EXTENSION_L);

        if (DoesFileExist(wszFilePath))
        {
            *ppMetadataFiles = new SString(wszFilePath);
            *pcMetadataFiles = 1;
            return S_OK;
        }

        WCHAR * wszLastDotChar = wcsrchr(wszRemainingNamespace, W('.'));
        if (wszLastDotChar == nullptr)
        {
            *ppMetadataFiles = nullptr;
            *pcMetadataFiles = 0;
            return S_FALSE;
        }
        *wszLastDotChar = W('\0');
    } while (true);
}


__checkReturn
HRESULT WINAPI CrossgenRoResolveNamespace(
    const LPCWSTR   wszNamespace,
    DWORD *         pcMetadataFiles,
    SString **      ppMetadataFiles)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;

    if (IsWindowsNamespace(wszNamespace))
    {
        DWORD cAppPaths = g_wszWindowsNamespaceDirectories->GetCount();

        for (DWORD i = 0; i < cAppPaths; i++)
        {
            // Returns S_FALSE on file not found so we continue proving app directory graph
            IfFailRet(FindNamespaceFileInDirectory(
                wszNamespace,
                g_wszWindowsNamespaceDirectories->Get(i).GetUnicode(),
                pcMetadataFiles,
                ppMetadataFiles));

            if (hr == S_OK)
            {
                return hr;
            }
        }
    }
    else
    {
        DWORD cAppPaths = g_wszUserNamespaceDirectories->GetCount();

        for (DWORD i = 0; i < cAppPaths; i++)
        {
            // Returns S_FALSE on file not found so we continue proving app directory graph
            IfFailRet(FindNamespaceFileInDirectory(
                wszNamespace,
                g_wszUserNamespaceDirectories->Get(i).GetUnicode(),
                pcMetadataFiles,
                ppMetadataFiles));

            if (hr == S_OK)
            {
                return hr;
            }
        }
    }

    return hr;
} // RoResolveNamespace

void SetFirstPartyWinMDPaths(StringArrayList* saAppPaths)
{
    LIMITED_METHOD_CONTRACT;

    g_wszWindowsNamespaceDirectories = saAppPaths;
}

void SetAppPaths(StringArrayList* saAppPaths)
{
    LIMITED_METHOD_CONTRACT;

    g_wszUserNamespaceDirectories = saAppPaths;
}

}// Namespace Crossgen
