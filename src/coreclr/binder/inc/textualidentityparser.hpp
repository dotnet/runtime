// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// TextualIdentityParser.hpp
//


//
// Defines the TextualIdentityParser class
//
// ============================================================

#ifndef __BINDER__TEXTUAL_IDENTITY_PARSER_HPP__
#define __BINDER__TEXTUAL_IDENTITY_PARSER_HPP__

#include "bindertypes.hpp"

namespace BINDER_SPACE
{
    class AssemblyVersion;
    class AssemblyIdentity;

    class TextualIdentityParser
    {
    public:
        static HRESULT ToString(/* in */  AssemblyIdentity *pAssemblyIdentity,
                                /* in */  DWORD             dwIdentityFlags,
                                /* out */ SString<EncodingUnicode> &textualIdentity);

        static void BlobToHex(/* in */  SBuffer &publicKeyOrTokenBLOB,
                              /* out */ SString<EncodingUnicode> &publicKeyOrToken);

    protected:
        static void EscapeString(/* in */ SString<EncodingUnicode> &input,
                                 /* out*/ SString<EncodingUnicode> &result);
    };
};

#endif
