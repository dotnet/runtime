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

    void CombinePath(const SString<EncodingUnicode> &pathA,
                     const SString<EncodingUnicode> &pathB,
                     SString<EncodingUnicode> &combinedPath)
    {
        SString<EncodingUnicode> platformPathSeparator(SharedData, GetPlatformPathSeparator());
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

    HRESULT GetNextPath(const SString<EncodingUnicode>& paths, SString<EncodingUnicode>::CIterator& startPos, SString<EncodingUnicode>& outPath)
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

        SString<EncodingUnicode>::CIterator iEnd = startPos;      // Where current path ends
        SString<EncodingUnicode>::CIterator iNext;                // Where next path starts
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

    HRESULT GetNextTPAPath(const SString<EncodingUnicode>& paths, SString<EncodingUnicode>::CIterator& startPos, bool dllOnly, SString<EncodingUnicode>& outPath, SString<EncodingUnicode>& simpleName, bool& isNativeImage)
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
            SString<EncodingUnicode>::CIterator iSimpleNameStart = outPath.End();

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

            const SString<EncodingUnicode> sNiDll(SharedData, W(".ni.dll"));
            const SString<EncodingUnicode> sNiExe(SharedData, W(".ni.exe"));
            const SString<EncodingUnicode> sDll(SharedData, W(".dll"));
            const SString<EncodingUnicode> sExe(SharedData, W(".exe"));

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
