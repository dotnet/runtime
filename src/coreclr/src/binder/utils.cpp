// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Utils.cpp
//


//
// Implements a bunch of binder auxilary functions
//
// ============================================================

#define DISABLE_BINDER_DEBUG_LOGGING

#include "utils.hpp"

#include <shlwapi.h>

#include "strongname.h"
#include "corpriv.h"

namespace BINDER_SPACE
{
    namespace
    {
        inline BOOL IsPathSeparator(WCHAR wcChar)
        {
            // Invariant: Valid only for MutateUrlToPath treated pathes
            return (wcChar == W('\\'));
        }

        inline const WCHAR *GetPlatformPathSeparator()
        {
#ifdef PLATFORM_UNIX
            return W("/");
#else
            return W("\\");
#endif // PLATFORM_UNIX            
        }

        inline WCHAR ToPlatformPathSepator(WCHAR wcChar)
        {
#ifdef PLATFORM_UNIX
            if (IsPathSeparator(wcChar))
            {
                wcChar = W('/');
            }
#endif // PLATFORM_UNIX

            return wcChar;
        }

        inline BOOL IsDoublePathSeparator(SString::CIterator &cur)
        {
            return (IsPathSeparator(cur[0]) && IsPathSeparator(cur[1]));
        }

        bool NeedToRemoveDoubleAndNormalizePathSeparators(SString const &path)
        {
#ifdef PLATFORM_UNIX
            return true;
#else
            SString::CIterator begin = path.Begin();
            SString::CIterator end = path.End();
            SString::CIterator cur = path.Begin();

            while (cur < end)
            {
                if ((cur != begin) && ((cur + 2) < end) && IsDoublePathSeparator(cur))
                {
                    return true;
                }

                cur++;
            }

            return false;
#endif
        }

        void RemoveDoubleAndNormalizePathSeparators(SString &path)
        {
            BINDER_LOG_ENTER(W("Utils::RemoveDoubleAndNormalizePathSeparators"));

            SString::Iterator begin = path.Begin();
            SString::Iterator end = path.End();
            SString::Iterator cur = path.Begin();
            PathString resultPath;

            BINDER_LOG_STRING(W("path"), path);

            while (cur < end)
            {
                if ((cur != begin) && ((cur + 2) < end) && IsDoublePathSeparator(cur))
                {
                    // Skip the doublette
                    cur++;
                }

                resultPath.Append(ToPlatformPathSepator(cur[0]));
                cur++;
            }
            
            BINDER_LOG_STRING(W("resultPath"), resultPath);

            path.Set(resultPath);

            BINDER_LOG_LEAVE(W("Utils::RemoveDoubleAndNormalizePathSeparators"));
        }
    }

    HRESULT FileOrDirectoryExists(PathString &path)
    {
        HRESULT hr = S_FALSE;

        DWORD dwFileAttributes = WszGetFileAttributes(path.GetUnicode());
        if (dwFileAttributes == INVALID_FILE_ATTRIBUTES)
        {
            hr = HRESULT_FROM_GetLastError();

            if ((hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)) ||
                (hr == HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND)))
            {
                hr = S_FALSE;
            }
        }
        else
        {
            hr = S_TRUE;
        }

        return hr;
    }

    HRESULT FileOrDirectoryExistsLog(PathString &path)
    {
        HRESULT hr = S_FALSE;
        BINDER_LOG_ENTER(W("Utils::FileOrDirectoryExistsLog"));
        BINDER_LOG_STRING(W("path"), path);
        
        hr = FileOrDirectoryExists(path);
        
        BINDER_LOG_LEAVE_HR(W("Utils::FileOrDirectoryExistsLog"), hr);
        return hr;
    }

    void MutateUrlToPath(SString &urlOrPath)
    {
        BINDER_LOG_ENTER(W("Utils::MutateUrlToPath"));
        const SString fileUrlPrefix(SString::Literal, W("file://"));
        SString::Iterator i = urlOrPath.Begin();

        BINDER_LOG_STRING(W("URL"), urlOrPath);

        if (urlOrPath.MatchCaseInsensitive(i, fileUrlPrefix))
        {
            urlOrPath.Delete(i, fileUrlPrefix.GetCount());

            i = urlOrPath.Begin() + 1;
            if (i[0] ==  W(':'))
            {
                // CLR erroneously passes in file:// prepended to file paths,
                // so we can't tell the difference between UNC and local file.
                goto Exit;
            }

            i = urlOrPath.Begin();
#if !defined(PLATFORM_UNIX)
            if (i[0] == W('/'))
            {
                // Disk path file:///
                urlOrPath.Delete(i, 1);
            }
            else if (i[0] != W('\\'))
            {
                // UNC Path, re-insert "//" if not the wrong file://\\...
                urlOrPath.Insert(i, W("//"));
            }
#else
            // Unix doesn't have a distinction between local and network path
            _ASSERTE(i[0] == W('\\') || i[0] == W('/'));
#endif
        }

    Exit:
        while (urlOrPath.Find(i, W('/')))
        {
            urlOrPath.Replace(i, W('\\'));
        }

        BINDER_LOG_STRING(W("Path"), urlOrPath);
        BINDER_LOG_LEAVE(W("Utils::MutateUrlToPath"));
    }

    void MutatePathToUrl(SString &pathOrUrl)
    {
        BINDER_LOG_ENTER(W("Utils::MutatePathToUrl"));
        SString::Iterator i = pathOrUrl.Begin();

        BINDER_LOG_STRING(W("Path"), pathOrUrl);

#if !defined(PLATFORM_UNIX)
        // Network path \\server --> file://server
        // Disk path    c:\dir   --> file:///c:/dir
        if (i[0] == W('\\'))
        {
            const SString networkUrlPrefix(SString::Literal, W("file:"));

            // Network path
            pathOrUrl.Insert(i, networkUrlPrefix);
            pathOrUrl.Skip(i, networkUrlPrefix);
        }
        else
        {
            const SString diskPathUrlPrefix(SString::Literal, W("file:///"));

            // Disk path
            pathOrUrl.Insert(i, diskPathUrlPrefix);
            pathOrUrl.Skip(i, diskPathUrlPrefix);
        }
#else
        // Unix doesn't have a distinction between a network or a local path
        _ASSERTE(i[0] == W('\\') || i[0] == W('/'));
        const SString fileUrlPrefix(SString::Literal, W("file://"));

        pathOrUrl.Insert(i, fileUrlPrefix);
        pathOrUrl.Skip(i, fileUrlPrefix);
#endif

        while (pathOrUrl.Find(i, W('\\')))
        {
            pathOrUrl.Replace(i, W('/'));
        }

        BINDER_LOG_STRING(W("URL"), pathOrUrl);
        BINDER_LOG_LEAVE(W("Utils::MutatePathToUrl"));
    }

    void PlatformPath(SString &path)
    {
        BINDER_LOG_ENTER(W("Utils::PlatformPath"));
        BINDER_LOG_STRING(W("input path"), path);

        // Create platform representation
        MutateUrlToPath(path);
        if (NeedToRemoveDoubleAndNormalizePathSeparators(path))
            RemoveDoubleAndNormalizePathSeparators(path);

        BINDER_LOG_STRING(W("platform path"), path);

        BINDER_LOG_LEAVE(W("Utils::PlatformPath"));
    }

    void CombinePath(SString &pathA,
                     SString &pathB,
                     SString &combinedPath)
    {
        BINDER_LOG_ENTER(W("Utils::CombinePath"));

        BINDER_LOG_STRING(W("path A"), pathA);
        BINDER_LOG_STRING(W("path B"), pathB);

        SString platformPathSeparator(SString::Literal, GetPlatformPathSeparator());
        combinedPath.Set(pathA);
        
        if (!combinedPath.EndsWith(platformPathSeparator))
        {
            combinedPath.Append(platformPathSeparator);
        }
        
        combinedPath.Append(pathB);
        
        BINDER_LOG_LEAVE(W("Utils::CombinePath"));
    }

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("GetTokenFromPublicKey"));

        const BYTE *pByteKey = publicKeyBLOB;
        DWORD dwKeyLen = publicKeyBLOB.GetSize();
        BYTE *pByteToken = NULL;
        DWORD dwTokenLen = 0;

        if (!StrongNameTokenFromPublicKey(const_cast<BYTE *>(pByteKey),
                                          dwKeyLen,
                                          &pByteToken,
                                          &dwTokenLen))
        {
            BINDER_LOG(W("StrongNameTokenFromPublicKey failed!"));
            IF_FAIL_GO(StrongNameErrorInfo());
        }
        else
        {
            _ASSERTE(pByteToken != NULL);
            publicKeyTokenBLOB.Set(pByteToken, dwTokenLen);
            StrongNameFreeBuffer(pByteToken);
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("GetTokenFromPublicKey"), hr);
        return hr;
    }

    BOOL IsFileNotFound(HRESULT hr)
    {
        return RuntimeFileNotFound(hr);
    }
};
