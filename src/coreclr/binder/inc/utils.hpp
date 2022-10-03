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
    // It is safe to use either A or B as CombinedPath.
    void CombinePath(const SString &pathA,
                     const SString &pathB,
                     SString &combinedPath);

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB);

    BOOL IsFileNotFound(HRESULT hr);

    HRESULT GetNextPath(const SString& paths, SString::CIterator& startPos, SString& outPath);
    HRESULT GetNextTPAPath(const SString& paths, SString::CIterator& startPos, bool dllOnly, SString& outPath, SString& simpleName, bool& isNativeImage);
};

#endif
