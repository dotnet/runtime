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

#include "utils.hpp"

#include "strongname.h"
#include "corpriv.h"

namespace BINDER_SPACE
{
    namespace
    {
        inline const WCHAR *GetPlatformPathSeparator()
        {
#ifdef PLATFORM_UNIX
            return W("/");
#else
            return W("\\");
#endif // PLATFORM_UNIX
        }
    }

    void MutateUrlToPath(SString &urlOrPath)
    {
        const SString fileUrlPrefix(SString::Literal, W("file://"));
        SString::Iterator i = urlOrPath.Begin();

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
    }

    void CombinePath(SString &pathA,
                     SString &pathB,
                     SString &combinedPath)
    {
        SString platformPathSeparator(SString::Literal, GetPlatformPathSeparator());
        combinedPath.Set(pathA);

        if (!combinedPath.EndsWith(platformPathSeparator))
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

        if (!StrongNameTokenFromPublicKey(const_cast<BYTE *>(pByteKey),
                                          dwKeyLen,
                                          &pByteToken,
                                          &dwTokenLen))
        {
            IF_FAIL_GO(StrongNameErrorInfo());
        }
        else
        {
            _ASSERTE(pByteToken != NULL);
            publicKeyTokenBLOB.Set(pByteToken, dwTokenLen);
            StrongNameFreeBuffer(pByteToken);
        }

    Exit:
        return hr;
    }

    BOOL IsFileNotFound(HRESULT hr)
    {
        return RuntimeFileNotFound(hr);
    }
};
