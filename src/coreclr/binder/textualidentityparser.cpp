// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// TextualIdentityParser.cpp
//


//
// Implements the TextualIdentityParser class
//
// ============================================================

#include "textualidentityparser.hpp"
#include "assemblyidentity.hpp"

#include "ex.h"

#define TOHEX(a) ((a)>=10 ? W('a')+(a)-10 : W('0')+(a))
#define TOLOWER(a) (((a) >= W('A') && (a) <= W('Z')) ? (W('a') + (a - W('A'))) : (a))

namespace BINDER_SPACE
{
    namespace
    {
        const int iPublicKeyTokenLength = 8;

        const int iVersionMax = 65535;
        const int iVersionParts = 4;

        inline void BinToUnicodeHex(const BYTE *pSrc, UINT cSrc, _Out_writes_(2*cSrc) LPWSTR pDst)
        {
            UINT x;
            UINT y;

            for (x = 0, y = 0 ; x < cSrc; ++x)
            {
                UINT v;

                v = pSrc[x]>>4;
                pDst[y++] = (WCHAR)TOHEX(v);
                v = pSrc[x] & 0x0f;
                pDst[y++] = (WCHAR)TOHEX(v);
            }
        }

        const struct {
            LPCWSTR strValue;
            PEKIND enumValue;
        } wszKnownArchitectures[] = { { W("x86"), peI386 },
                                      { W("IA64"), peIA64 },
                                      { W("AMD64"), peAMD64 },
                                      { W("ARM"), peARM },
                                      { W("MSIL"), peMSIL } };

        LPCWSTR PeKindToString(PEKIND kProcessorArchitecture)
        {
            _ASSERTE(kProcessorArchitecture != peNone);

            for (int i = LENGTH_OF(wszKnownArchitectures); i--;)
            {
                if (wszKnownArchitectures[i].enumValue == kProcessorArchitecture)
                {
                    return wszKnownArchitectures[i].strValue;
                }
            }

            return NULL;
        }

        LPCWSTR ContentTypeToString(AssemblyContentType kContentType)
        {
            _ASSERTE(kContentType != AssemblyContentType_Default);

            if (kContentType == AssemblyContentType_WindowsRuntime)
            {
                return W("WindowsRuntime");
            }

            return NULL;
        }

        BOOL IsWhitespace(WCHAR wcChar)
        {
            return ((wcChar == L'\n') || (wcChar == L'\r') || (wcChar == L' ') || (wcChar == L'\t'));
        }
    };  // namespace (anonymous)

    /* static */
    HRESULT TextualIdentityParser::ToString(AssemblyIdentity *pAssemblyIdentity,
                                            DWORD             dwIdentityFlags,
                                            SString          &textualIdentity)
    {
        HRESULT hr = S_OK;

        IF_FALSE_GO(pAssemblyIdentity != NULL);

        EX_TRY
        {
            SmallStackSString tmpString;

            textualIdentity.Clear();

            if (pAssemblyIdentity->m_simpleName.IsEmpty())
            {
                goto Exit;
            }

            EscapeString(pAssemblyIdentity->m_simpleName, tmpString);
            textualIdentity.Append(tmpString);

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_VERSION))
            {
                tmpString.Clear();
                tmpString.Printf(W("%d.%d.%d.%d"),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetMajor(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetMinor(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetBuild(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetRevision());

                textualIdentity.Append(W(", Version="));
                textualIdentity.Append(tmpString);
            }

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_CULTURE))
            {
                textualIdentity.Append(W(", Culture="));
                if (pAssemblyIdentity->m_cultureOrLanguage.IsEmpty())
                {
                    textualIdentity.Append(W("neutral"));
                }
                else
                {
                    EscapeString(pAssemblyIdentity->m_cultureOrLanguage, tmpString);
                    textualIdentity.Append(tmpString);
                }
            }

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
            {
                textualIdentity.Append(W(", PublicKey="));
                tmpString.Clear();
                BlobToHex(pAssemblyIdentity->m_publicKeyOrTokenBLOB, tmpString);
                textualIdentity.Append(tmpString);
            }
            else if (AssemblyIdentity::Have(dwIdentityFlags,
                                            AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN))
            {
                textualIdentity.Append(W(", PublicKeyToken="));
                tmpString.Clear();
                BlobToHex(pAssemblyIdentity->m_publicKeyOrTokenBLOB, tmpString);
                textualIdentity.Append(tmpString);
            }
            else if (AssemblyIdentity::Have(dwIdentityFlags,
                                            AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL))
            {
                textualIdentity.Append(W(", PublicKeyToken=null"));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
            {
                textualIdentity.Append(W(", processorArchitecture="));
                textualIdentity.Append(PeKindToString(pAssemblyIdentity->m_kProcessorArchitecture));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE))
            {
                textualIdentity.Append(W(", Retargetable=Yes"));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
            {
                textualIdentity.Append(W(", ContentType="));
                textualIdentity.Append(ContentTypeToString(pAssemblyIdentity->m_kContentType));
            }

        }
        EX_CATCH_HRESULT(hr);

    Exit:
        return hr;
    }

    /* static */
    void TextualIdentityParser::BlobToHex(SBuffer &publicKeyOrTokenBLOB,
                                          SString &publicKeyOrToken)
    {
        UINT cbPublicKeyOrTokenBLOB = publicKeyOrTokenBLOB.GetSize();
        WCHAR *pwzpublicKeyOrToken =
            publicKeyOrToken.OpenUnicodeBuffer(cbPublicKeyOrTokenBLOB * 2);

        BinToUnicodeHex(publicKeyOrTokenBLOB, cbPublicKeyOrTokenBLOB, pwzpublicKeyOrToken);
        publicKeyOrToken.CloseBuffer(cbPublicKeyOrTokenBLOB * 2);
    }

    /* static */
    void TextualIdentityParser::EscapeString(SString &input,
                                             SString &result)
    {
        BOOL fNeedQuotes = FALSE;
        WCHAR wcQuoteCharacter = W('"');

        SmallStackSString tmpString;
        SString::Iterator cursor = input.Begin();
        SString::Iterator end = input.End() - 1;

        // Leading/Trailing white space require quotes
        if (IsWhitespace(cursor[0]) || IsWhitespace(end[0]))
        {
            fNeedQuotes = TRUE;
        }

        // Fusion textual identity compat: escape all non-quote characters even if quoted
        while (cursor <= end)
        {
            WCHAR wcCurrentChar = cursor[0];

            switch (wcCurrentChar)
            {
            case W('"'):
            case W('\''):
                if (fNeedQuotes && (wcQuoteCharacter != wcCurrentChar))
                {
                    tmpString.Append(wcCurrentChar);
                }
                else if (!fNeedQuotes)
                {
                    fNeedQuotes = TRUE;
                    wcQuoteCharacter = (wcCurrentChar == W('"') ? W('\'') : W('"'));
                    tmpString.Append(wcCurrentChar);
                }
                else
                {
                    tmpString.Append(W('\\'));
                    tmpString.Append(wcCurrentChar);
                }
                break;
            case W('='):
            case W(','):
            case W('\\'):
                tmpString.Append(W('\\'));
                tmpString.Append(wcCurrentChar);
                break;
            case 9:
                tmpString.Append(W("\\t"));
                break;
            case 10:
                tmpString.Append(W("\\n"));
                break;
            case 13:
                tmpString.Append(W("\\r"));
                break;
            default:
                tmpString.Append(wcCurrentChar);
                break;
            }

            cursor++;
        }

        if (fNeedQuotes)
        {
            result.Clear();
            result.Append(wcQuoteCharacter);
            result.Append(tmpString);
            result.Append(wcQuoteCharacter);
        }
        else
        {
            result.Set(tmpString);
        }
    }
};
