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
    void CombinePath(const SString<EncodingUnicode> &pathA,
                     const SString<EncodingUnicode> &pathB,
                     SString<EncodingUnicode> &combinedPath);

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB);

    BOOL IsFileNotFound(HRESULT hr);

    HRESULT GetNextPath(const SString<EncodingUnicode>& paths, SString<EncodingUnicode>::CIterator& startPos, SString<EncodingUnicode>& outPath);
    HRESULT GetNextTPAPath(const SString<EncodingUnicode>& paths, SString<EncodingUnicode>::CIterator& startPos, bool dllOnly, SString<EncodingUnicode>& outPath, SString<EncodingUnicode>& simpleName, bool& isNativeImage);
};

#endif
