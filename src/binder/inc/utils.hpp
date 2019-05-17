// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Utils.hpp
//


//
// Declares a bunch of binder auxilary functions
//
// ============================================================

#ifndef __BINDER_UTILS_HPP__
#define __BINDER_UTILS_HPP__

#include "bindertypes.hpp"

namespace BINDER_SPACE
{
    inline BOOL EqualsCaseInsensitive(SString &a, SString &b)
    {
        return a.EqualsCaseInsensitive(b);
    }

    inline ULONG HashCaseInsensitive(SString &string)
    {
        return string.HashCaseInsensitive();
    }

    HRESULT FileOrDirectoryExists(PathString &path);
    HRESULT FileOrDirectoryExistsLog(PathString &path);

    void MutateUrlToPath(SString &urlOrPath);
    void MutatePathToUrl(SString &pathOrUrl);

    // Mutates path
    void PlatformPath(SString &path);
    void CanonicalizePath(SString &path, BOOL fAppendPathSeparator = FALSE);

    // It is safe to use either A or B as CombinedPath.
    void CombinePath(SString &pathA,
                     SString &pathB,
                     SString &combinedPath);

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB);

    BOOL IsFileNotFound(HRESULT hr);
};

#endif
