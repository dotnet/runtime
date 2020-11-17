// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

    void MutateUrlToPath(SString &urlOrPath);

    // It is safe to use either A or B as CombinedPath.
    void CombinePath(SString &pathA,
                     SString &pathB,
                     SString &combinedPath);

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB);

    BOOL IsFileNotFound(HRESULT hr);

    HRESULT GetNextPath(SString& paths, SString::Iterator& startPos, SString& outPath);
    HRESULT GetNextTPAPath(SString& paths, SString::Iterator& startPos, bool dllOnly, SString& outPath, SString& simpleName, bool& isNativeImage);
};

#endif
