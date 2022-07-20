// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// Utils.cpp
//


//
// Implements a bunch of binder auxilary functions
//
// ============================================================

#include "utils.hpp"

#include "strongnameinternal.h"
#include "corpriv.h"
#include "clr/fs/path.h"
using namespace clr::fs;

namespace BINDER_SPACE
{
    namespace
    {
        inline const WCHAR *GetPlatformPathSeparator()
        {
#ifdef TARGET_UNIX
            return W("/");
#else
            return W("\\");
#endif // TARGET_UNIX
        }
    }

    void CombinePath(const SString &pathA,
                     const SString &pathB,
                     SString &combinedPath)
    {
        SString platformPathSeparator(SString::Literal, GetPlatformPathSeparator());
        combinedPath.Set(pathA);

        if (!combinedPath.IsEmpty() && !combinedPath.EndsWith(platformPathSeparator))
        {
            combinedPath.Append(platformPathSeparator);
        }

        combinedPath.Append(pathB);
    }

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB)
    {
        HRESULT hr = S_OK;

        const BYTE *pByteKey = publicKeyBLOB;
        DWORD dwKeyLen = publicKeyBLOB.GetSize();
        BYTE *pByteToken = NULL;
        DWORD dwTokenLen = 0;

        IF_FAIL_GO(StrongNameTokenFromPublicKey(
            const_cast<BYTE*>(pByteKey),
            dwKeyLen,
            &pByteToken,
            &dwTokenLen));

        _ASSERTE(pByteToken != NULL);
        publicKeyTokenBLOB.Set(pByteToken, dwTokenLen);
        StrongNameFreeBuffer(pByteToken);

    Exit:
        return hr;
    }

    BOOL IsFileNotFound(HRESULT hr)
    {
        return RuntimeFileNotFound(hr);
    }

    HRESULT GetNextPath(const SString& paths, SString::CIterator& startPos, SString& outPath)
    {
        HRESULT hr = S_OK;

        bool wrappedWithQuotes = false;

        // Skip any leading spaces or path separators
        while (paths.Skip(startPos, W(' ')) || paths.Skip(startPos, PATH_SEPARATOR_CHAR_W)) {}

        if (startPos == paths.End())
        {
            // No more paths in the string and we just skipped over some white space
            outPath.Set(W(""));
            return S_FALSE;
        }

        // Support paths being wrapped with quotations
        if (paths.Skip(startPos, W('\"')))
        {
            wrappedWithQuotes = true;
        }

        SString::CIterator iEnd = startPos;      // Where current path ends
        SString::CIterator iNext;                // Where next path starts
        if (wrappedWithQuotes)
        {
            if (paths.Find(iEnd, W('\"')))
            {
                iNext = iEnd;
                // Find where the next path starts - there should be a path separator right after the closing quotation mark
                if (paths.Find(iNext, PATH_SEPARATOR_CHAR_W))
                {
                    iNext++;
                }
                else
                {
                    iNext = paths.End();
                }
            }
            else
            {
                // There was no terminating quotation mark - that's bad
                GO_WITH_HRESULT(E_INVALIDARG);
            }
        }
        else if (paths.Find(iEnd, PATH_SEPARATOR_CHAR_W))
        {
            iNext = iEnd + 1;
        }
        else
        {
            iNext = iEnd = paths.End();
        }

        // Skip any trailing spaces
        while (iEnd[-1] == W(' '))
        {
            iEnd--;
        }

        _ASSERTE(startPos < iEnd);

        outPath.Set(paths, startPos, iEnd);
        startPos = iNext;
    Exit:
        return hr;
    }

    HRESULT GetNextTPAPath(const SString& paths, SString::CIterator& startPos, bool dllOnly, SString& outPath, SString& simpleName, bool& isNativeImage)
    {
        HRESULT hr = S_OK;
        isNativeImage = false;

        HRESULT pathResult = S_OK;
        IF_FAIL_GO(pathResult = GetNextPath(paths, startPos, outPath));
        if (pathResult == S_FALSE)
        {
            return S_FALSE;
        }

        if (Path::IsRelative(outPath))
        {
            GO_WITH_HRESULT(E_INVALIDARG);
        }

        {
            // Find the beginning of the simple name
            SString::CIterator iSimpleNameStart = outPath.End();

            if (!outPath.FindBack(iSimpleNameStart, DIRECTORY_SEPARATOR_CHAR_W))
            {
                iSimpleNameStart = outPath.Begin();
            }
            else
            {
                // Advance past the directory separator to the first character of the file name
                iSimpleNameStart++;
            }

            if (iSimpleNameStart == outPath.End())
            {
                GO_WITH_HRESULT(E_INVALIDARG);
            }

            const SString sNiDll(SString::Literal, W(".ni.dll"));
            const SString sNiExe(SString::Literal, W(".ni.exe"));
            const SString sDll(SString::Literal, W(".dll"));
            const SString sExe(SString::Literal, W(".exe"));

            if (!dllOnly && (outPath.EndsWithCaseInsensitive(sNiDll) ||
                outPath.EndsWithCaseInsensitive(sNiExe)))
            {
                simpleName.Set(outPath, iSimpleNameStart, outPath.End() - 7);
                isNativeImage = true;
            }
            else if (outPath.EndsWithCaseInsensitive(sDll) ||
                (!dllOnly && outPath.EndsWithCaseInsensitive(sExe)))
            {
                simpleName.Set(outPath, iSimpleNameStart, outPath.End() - 4);
            }
            else
            {
                // Invalid filename
                GO_WITH_HRESULT(E_INVALIDARG);
            }
        }

    Exit:
        return hr;
    }

};
